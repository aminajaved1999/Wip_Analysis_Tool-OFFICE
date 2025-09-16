using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WIPAT.DAL;
using WIPAT.Entities;
using WIPAT.Entities.Enum;

namespace WIPAT
{
    public partial class MainForm : Form
    {
        private Label[] steps;

        private readonly Color StepDefaultBack = Color.FromArgb(0, 0, 64);
        private readonly Color StepHoverBack = Color.FromArgb(20, 20, 100);
        private readonly Color StepActiveBack = Color.DodgerBlue;

        private readonly Color StepDefaultFore = Color.LightGray;
        private readonly Color StepActiveFore = Color.White;

        private Form activeForm = null;
        private ItemsRepository itemsRepository;

        private readonly WipSession _session = new WipSession();
        private readonly ToolTip _stepperTip = new ToolTip();

        // Track whether Step 2 (Calculate WIP) has finished successfully
        private bool _step2Done = false;

        // NEW: cache child forms so UI/data state persists across navigation
        private readonly Dictionary<Type, Form> _forms = new Dictionary<Type, Form>();

        public MainForm()
        {
            InitializeComponent();
            itemsRepository = new ItemsRepository();

            // If you add more steps later, extend this array and the switch below.
            steps = new[] { step1, step2, step3 };

            foreach (var step in steps)
            {
                step.Click += Step_Click;
                step.MouseEnter += Step_MouseEnter;
                step.MouseLeave += Step_MouseLeave;
            }

            stepperPanel.BackColor = StepDefaultBack;

            // Start at Step 1 (Upload)
            SetActiveStep(1);
            ShowForm(() => new UploadForm(_session, SetStatus));

            // Initial gating (locks Step 2 & 3 until Step 1 is ready; Step 3 until Step 2 is done)
            UpdateStepperEnabledState();

            // Ensure all cached children get disposed when app exits
            this.FormClosing += MainForm_FormClosing;
        }

        #region Child form caching / navigation

        private T GetOrCreate<T>(Func<T> factory) where T : Form
        {
            if (_forms.TryGetValue(typeof(T), out var existing) && !existing.IsDisposed)
                return (T)existing;

            var created = factory();
            created.TopLevel = false;
            created.FormBorderStyle = FormBorderStyle.None;
            created.Dock = DockStyle.Fill;

            _forms[typeof(T)] = created;
            return created;
        }

        /// <summary>
        /// Show a cached child form inside mainPanel without closing/disposing others.
        /// </summary>
        private void ShowForm<T>(Func<T> factory) where T : Form
        {
            var form = GetOrCreate(factory);

            if (activeForm != null && !activeForm.IsDisposed)
                activeForm.Hide(); // keep state

            activeForm = form;

            if (!mainPanel.Controls.Contains(form))
                mainPanel.Controls.Add(form);

            form.BringToFront();
            form.Show();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Dispose cached children on app exit to free resources
            foreach (var kv in _forms.ToList())
            {
                if (kv.Value != null && !kv.Value.IsDisposed)
                    kv.Value.Dispose();
            }
            _forms.Clear();
        }

        #endregion

        #region Step UI Management

        private void Step_Click(object sender, EventArgs e)
        {
            var clickedStep = sender as Label;
            if (clickedStep == null) return;

            int index = Array.IndexOf(steps, clickedStep) + 1;
            statusLabel.Text = $"Step {index}: {clickedStep.Text}";

            switch (index)
            {
                case 1:
                    SetActiveStep(1);
                    ShowForm(() => new UploadForm(_session, SetStatus));
                    break;

                case 2:
                    if (!IsReadyForCalc(out var missingMsgFor2))
                    {
                        SetStatus(string.IsNullOrWhiteSpace(missingMsgFor2) ? "Complete Step 1 first." : missingMsgFor2, StatusType.Warning);
                        SetActiveStep(1);
                        ShowForm(() => new UploadForm(_session, SetStatus));
                        return;
                    }

                    SetActiveStep(2);
                    ShowForm(() =>
                    {
                        var calc = new CalculateWIPForm(_session, SetStatus);
                        // Only add once per instance
                        calc.CalculationCompleted -= OnStep2Completed;
                        calc.CalculationCompleted += OnStep2Completed;
                        return calc;
                    });
                    break;

                case 3:
                    var step1Ready = IsReadyForCalc(out var missingMsgFor3);
                    if (!step1Ready)
                    {
                        SetStatus(string.IsNullOrWhiteSpace(missingMsgFor3) ? "Complete Step 1 first." : missingMsgFor3, StatusType.Warning);
                        SetActiveStep(1);
                        ShowForm(() => new UploadForm(_session, SetStatus));
                        return;
                    }

                    if (!_step2Done)
                    {
                        SetStatus("Complete Step 2 (Calculate WIP) before proceeding to Export.", StatusType.Warning);
                        SetActiveStep(2);
                        ShowForm(() =>
                        {
                            var calc2 = new CalculateWIPForm(_session, SetStatus);
                            calc2.CalculationCompleted -= OnStep2Completed;
                            calc2.CalculationCompleted += OnStep2Completed;
                            return calc2;
                        });
                        return;
                    }

                    SetActiveStep(3);
                    ShowForm(() => new ExportForm(_session, SetStatus));
                    break;
            }
        }

        private void Step_MouseEnter(object sender, EventArgs e)
        {
            var step = sender as Label;
            if (step == null || !step.Enabled) return;
            if (step.BackColor != StepActiveBack)
                step.BackColor = StepHoverBack;
        }

        private void Step_MouseLeave(object sender, EventArgs e)
        {
            var step = sender as Label;
            if (step == null || !step.Enabled) return;
            if (step.BackColor != StepActiveBack)
                step.BackColor = StepDefaultBack;
        }

        private void SetActiveStep(int stepNumber)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                if (i == stepNumber - 1)
                {
                    steps[i].BackColor = StepActiveBack;
                    steps[i].ForeColor = StepActiveFore;
                }
                else
                {
                    steps[i].BackColor = StepDefaultBack;
                    steps[i].ForeColor = steps[i].Enabled ? StepDefaultFore : Color.Gray;
                }
            }
        }

        /// <summary>
        /// Enable/disable steps based on readiness.
        /// Step 2: only if Step 1 ready.
        /// Step 3: only if Step 1 ready AND Step 2 done.
        /// </summary>
        public void UpdateStepperEnabledState()
        {
            bool step1Ready = IsReadyForCalc(out var missingMsg);
            bool step2Ready = _step2Done;

            if (!step1Ready)
                SetStatus(string.IsNullOrWhiteSpace(missingMsg) ? "Complete Step 1 first." : missingMsg, StatusType.Warning);

            // Step 2 enabled only if Step 1 ready
            step2.Enabled = step1Ready;
            step2.Cursor = step1Ready ? Cursors.Hand : Cursors.No;
            _stepperTip.SetToolTip(step2, step1Ready
                ? "Proceed to Calculate WIP"
                : "Complete Step 1 first (Forecast ×2, Stock, Orders, Prev, Curr, metadata...).");

            // Step 3 enabled only if Step 1 ready AND Step 2 done
            bool step3Enabled = step1Ready && step2Ready;
            step3.Enabled = step3Enabled;
            step3.Cursor = step3Enabled ? Cursors.Hand : Cursors.No;
            _stepperTip.SetToolTip(step3, step3Enabled
                ? "Proceed to Export"
                : (step1Ready ? "Run Calculate WIP in Step 2 first." : "Complete Step 1 first."));

            // Visually dim disabled steps (unless active)
            if (!step1Ready && step2.BackColor != StepActiveBack)
            {
                step2.BackColor = StepDefaultBack;
                step2.ForeColor = Color.Gray;
            }
            else if (step1Ready && step2.BackColor != StepActiveBack)
            {
                step2.ForeColor = StepDefaultFore;
            }

            if (!step3Enabled && step3.BackColor != StepActiveBack)
            {
                step3.BackColor = StepDefaultBack;
                step3.ForeColor = Color.Gray;
            }
            else if (step3Enabled && step3.BackColor != StepActiveBack)
            {
                step3.ForeColor = StepDefaultFore;
            }
        }

        /// <summary>
        /// Minimal requirement to enter Step 2 (what you already defined).
        /// </summary>
        private bool IsReadyForCalc(out string message)
        {
            var missing = new List<string>();

            if (_session == null)
            {
                message = "Internal error: session is null.";
                return false;
            }

            // Forecasts
            if (_session.ForecastFiles == null || _session.ForecastFiles.Count == 0)
                missing.Add("Forecast files (none loaded)");
            else if (_session.ForecastFiles.Count < 2)
                missing.Add($"At least 2 forecast files (currently {_session.ForecastFiles.Count})");

            // Optional but useful (kept as a required check per your current logic)
            if (_session.AsinList == null || _session.AsinList.Count == 0)
                missing.Add("ASIN list (not extracted from forecasts)");

            // Stock / Orders
            if (_session.Stock == null)
                missing.Add("Stock file");
            if (_session.Orders == null)
                missing.Add("Order file");

            // Forecast masters
            if (_session.Prev == null)
                missing.Add("Previous forecast (Prev)");
            if (_session.Curr == null)
                missing.Add("Current forecast (Curr)");

            // Metadata / selections
            //if (string.IsNullOrWhiteSpace(_session.WipType))
            //    missing.Add("WIP Type (Analyst / Layman / LaymanFormula)");
            //if (string.IsNullOrWhiteSpace(_session.TargetMonth))
            //    missing.Add("Target month (e.g., \"August 2025\")");
            //if (string.IsNullOrWhiteSpace(_session.CurrentMonth))
            //    missing.Add("Current month (e.g., \"July 2025\")");
            //if (string.IsNullOrWhiteSpace(_session.CurrentMonthWithYear))
            //    missing.Add("Current month with year (e.g., \"July 2025\")");

            // CommitmentPeriod (used during export filtering)
            if (_session.CommitmentPeriod <= 0)
                missing.Add("Commitment period");

            if (missing.Count > 0)
            {
                message = "Please Provide Following:\n• " + string.Join("\n• ", missing);
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// Status bar helper (colors + text).
        /// </summary>
        public void SetStatus(string message, StatusType statusType)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(message, statusType)));
                return;
            }

            statusLabel.Text = message;

            switch (statusType)
            {
                case StatusType.Success:
                    statusLabel.BackColor = Color.Green;
                    statusLabel.ForeColor = Color.White;
                    break;
                case StatusType.Error:
                    statusLabel.BackColor = Color.Red;
                    statusLabel.ForeColor = Color.White;
                    break;
                case StatusType.Reset:
                    statusLabel.Text = string.Empty;
                    statusLabel.BackColor = Color.Transparent;
                    statusLabel.ForeColor = Color.Black;
                    break;
                case StatusType.Warning:
                    statusLabel.BackColor = Color.Yellow;
                    statusLabel.ForeColor = Color.Black;
                    break;
                case StatusType.Transparent:
                    statusLabel.BackColor = Color.Transparent;
                    statusLabel.ForeColor = Color.Black;
                    break;
            }
        }

        /// <summary>
        /// Called by CalculateWIPForm when the calculation is finished.
        /// Unlocks Step 3.
        /// </summary>
        private void OnStep2Completed()
        {
            _step2Done = true;
            UpdateStepperEnabledState();
            SetStatus("WIP calculation completed. You can proceed to Export.", StatusType.Success);
        }

        /// <summary>
        /// If user changes Step 1 inputs (re-uploads, changes months, etc.), call this
        /// from UploadForm to relock Step 3 until Step 2 is rerun.
        /// </summary>
        public void ResetStep2Progress()
        {
            _step2Done = false;
            UpdateStepperEnabledState();
        }

        #endregion Step UI Management

        #region Menu Items (Catalogue display)

        private async void ItemsCatalogueMenuItem_Click(object sender, EventArgs e)
        {
            var itemsCatalogue = await itemsRepository.GetItemCatalogues();

            if (!itemsCatalogue.Success)
            {
                MessageBox.Show("Failed to load item catalogue:\n" + itemsCatalogue.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var data = itemsCatalogue.Data;
            _session.ItemCatalogue = itemsCatalogue.Data;

            var form = new Form
            {
                Text = "Item Catalogue",
                Width = 800,
                Height = 500,
                StartPosition = FormStartPosition.CenterParent
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToOrderColumns = false,
                DataSource = data
            };

            form.Controls.Add(dgv);
            form.ShowDialog();
        }

        #endregion

        #region ADD ITEMS (Excel import → DB)

        private void addItemsToCatalogueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new Form
            {
                Text = "Import Item Catalogue",
                Width = 800,
                Height = 500,
                StartPosition = FormStartPosition.CenterParent
            };

            var btnSelectFile = new Button
            {
                Text = "Select Excel File",
                Dock = DockStyle.Top,
                Height = 40
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var btnSave = new Button
            {
                Text = "Save to Database",
                Dock = DockStyle.Bottom,
                Height = 40,
                Enabled = false
            };

            form.Controls.Add(dgv);
            form.Controls.Add(btnSave);
            form.Controls.Add(btnSelectFile);

            DataTable importedData = null;

            // File selection + load
            btnSelectFile.Click += (s, ev) =>
            {
                try
                {
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Filter = "Excel Files|*.xlsx;*.xls",
                        Title = "Select Excel File"
                    };

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = ofd.FileName;

                        importedData = GetDataTableFromExcel(filePath);

                        dgv.DataSource = importedData;
                        btnSave.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Save to DB
            btnSave.Click += (s, ev) =>
            {
                try
                {
                    if (importedData != null && importedData.Rows.Count > 0)
                    {
                        BulkInsertToSQL(importedData);
                        MessageBox.Show("Data imported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form.Close();
                    }
                    else
                    {
                        MessageBox.Show("No data to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            form.ShowDialog();
        }

        // Convert Excel → DataTable
        private DataTable GetDataTableFromExcel(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Required for EPPlus

            DataTable dt = new DataTable();

            // Define columns to match your DB (skip Id because it's Identity)
            dt.Columns.Add("Casin", typeof(string));
            dt.Columns.Add("Model", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("ColorName", typeof(string));
            dt.Columns.Add("Size", typeof(string));
            dt.Columns.Add("PCPK", typeof(string));
            dt.Columns.Add("MOQ", typeof(int));              // keep null
            dt.Columns.Add("CasePackQty", typeof(int));
            dt.Columns.Add("CreatedAt", typeof(DateTime));

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var ws = package.Workbook.Worksheets[0]; // First sheet
                int rowCount = ws.Dimension.Rows;

                // Assumes first row is header → start from row 2
                for (int row = 2; row <= rowCount; row++)
                {
                    DataRow dr = dt.NewRow();
                    dr["Casin"] = ws.Cells[row, 1].Text;        // C-ASIN
                    dr["Model"] = ws.Cells[row, 2].Text;        // Model
                    dr["Description"] = ws.Cells[row, 3].Text;  // Description
                    dr["ColorName"] = ws.Cells[row, 4].Text;    // Color Name
                    dr["Size"] = ws.Cells[row, 5].Text;         // Size
                    dr["PCPK"] = ws.Cells[row, 6].Text;         // PC/PK

                    // MOQ → null
                    dr["MOQ"] = DBNull.Value;

                    // CasePackQty → int
                    dr["CasePackQty"] = int.TryParse(ws.Cells[row, 7].Text, out int casePack) ? casePack : 0;

                    // CreatedAt = now
                    dr["CreatedAt"] = DateTime.Now;

                    dt.Rows.Add(dr);
                }
            }
            return dt;
        }

        // Bulk Insert → SQL
        private void BulkInsertToSQL(DataTable dt)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["dbContext"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";

                    // Column mappings (Excel → DB)
                    bulkCopy.ColumnMappings.Add("Casin", "Casin");
                    bulkCopy.ColumnMappings.Add("Model", "Model");
                    bulkCopy.ColumnMappings.Add("Description", "Description");
                    bulkCopy.ColumnMappings.Add("ColorName", "ColorName");
                    bulkCopy.ColumnMappings.Add("Size", "Size");
                    bulkCopy.ColumnMappings.Add("PCPK", "PCPK");
                    bulkCopy.ColumnMappings.Add("MOQ", "MOQ");
                    bulkCopy.ColumnMappings.Add("CasePackQty", "CasePackQty");
                    bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");

                    bulkCopy.WriteToServer(dt);
                }
            }
        }

        #endregion
    }
}
