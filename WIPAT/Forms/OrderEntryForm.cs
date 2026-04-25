using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Entities;
using WIPAT.Entities.Enum;
using WIPAT.Helpers;
using WIPAT.Entities.BO;

namespace WIPAT
{
    public partial class OrderEntryForm : Form
    {
        #region Dependencies & Fields

        private readonly WipSession _session;
        private readonly IOrderManager _orderManager;
        private readonly IExcelService _excelService;
        private readonly BusyOverlayHelper _busyHelper;

        // State variables
        private OrderFileResponse _currentPreviewData;
        private OrderMasterDto _currentMasterDto;
        private string _uploadedFileName;

        #endregion

        #region Constructor & Initialization

        public OrderEntryForm(WipSession session, IOrderManager orderManager, IExcelService excelService)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _orderManager = orderManager ?? throw new ArgumentNullException(nameof(orderManager));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));

            InitializeComponent();

            _busyHelper = new BusyOverlayHelper(this, progressBarTop, (msg, type) => SetStatus(msg, type));
            ApplyTheme();
        }

        private void OrderEntryForm_Load(object sender, EventArgs e)
        {
            this.ActiveControl = null;
            ResetFormState();
        }

        #endregion

        #region Event Handlers

        // ---------------------------------------------------------
        // BUTTON 1: PREVIEW
        // ---------------------------------------------------------
        private async void btnPreview_Click(object sender, EventArgs e)
        {
            // 1. Basic UI Validation
            if (string.IsNullOrWhiteSpace(txtDocNo.Text))
            {
                MessageBox.Show("Please enter a Document Number.", "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. File Selection
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Files|*.xlsx;*.xls" })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;

                _uploadedFileName = Path.GetFileName(ofd.FileName);

                _currentMasterDto = new OrderMasterDto
                {
                    DocType = OrderDocType.Actual.ToString(),
                    DocNo = txtDocNo.Text.Trim(),
                    FileName = _uploadedFileName,
                    Month = null,
                    Year = null
                };

                btnFillKill.Enabled = false;
                btnFillKill.BackColor = Color.Gray;

                await RunPreviewProcess(ofd.FileName);
            }
        }

        // ---------------------------------------------------------
        // BUTTON 2: SAVE
        // ---------------------------------------------------------
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (_currentPreviewData == null || _currentPreviewData.ValidOrderItems.Count == 0) return;

            // Safety check: Ensure Month/Year were successfully extracted
            if (string.IsNullOrEmpty(_currentMasterDto.Month) || string.IsNullOrEmpty(_currentMasterDto.Year))
            {
                MessageBox.Show("Cannot Save: Month or Year could not be determined from the Excel file.",
                                "Data Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to save {_currentPreviewData.ValidOrderItems.Count} items for {_currentMasterDto.Month} {_currentMasterDto.Year}?",
                                "Confirm Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _busyHelper.ShowBusy("Saving Order...");
            btnSave.Enabled = false;
            btnPreview.Enabled = false;
            ToggleInputs(false);

            try
            {
                var result = await Task.Run(() => _orderManager.ConfirmOrderAsync(_currentMasterDto, _currentPreviewData.ValidOrderItems, _session));

                if (result.Success)
                {
                    MessageBox.Show("Order Saved Successfully! You may now proceed to Fill & Kill.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Success State
                    btnPreview.Enabled = false;
                    btnPreview.BackColor = Color.Gray;
                    btnSave.Enabled = false;
                    btnSave.BackColor = Color.Gray;

                    btnFillKill.Enabled = true;
                    btnFillKill.BackColor = Color.DarkOrange;

                    ToggleInputs(false);
                    SetStatus("Order Saved. Pending Fill & Kill...", StatusType.Warning);
                }
                else
                {
                    // Failure State
                    MessageBox.Show($"Save Failed: {result.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnSave.Enabled = true;
                    btnSave.BackColor = Color.ForestGreen;
                    btnPreview.Enabled = true;
                    btnPreview.BackColor = Color.DodgerBlue;
                    ToggleInputs(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical Error: {ex.Message}");
                btnSave.Enabled = true;
                btnSave.BackColor = Color.ForestGreen;
                btnPreview.Enabled = true;
                ToggleInputs(true);
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }

        // ---------------------------------------------------------
        // BUTTON 3: FILL & KILL
        // ---------------------------------------------------------
        private async void btnFillKill_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to generate the 'Ship' Order records?",
                                "Confirm Fill & Kill", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _busyHelper.ShowBusy("Saving Ship Order...");
            btnFillKill.Enabled = false;

            try
            {
                // Rely on the Month/Year already stored in _currentMasterDto from the Preview step
                var shipMasterDto = new OrderMasterDto
                {
                    Month = _currentMasterDto.Month,
                    Year = _currentMasterDto.Year,
                    DocType = OrderDocType.Ship.ToString(),
                    DocNo = _currentMasterDto.DocNo,
                    FileName = _currentMasterDto.FileName
                };

                var itemsToSave = new List<ValidOrder>(_currentPreviewData.ValidOrderItems);
                var result = await Task.Run(() => _orderManager.RunFillAndKillAsync(shipMasterDto, itemsToSave, _session));

                if (result.Success)
                {
                    MessageBox.Show("Fill & Kill (Ship Order) Created Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetFormState();
                }
                else
                {
                    MessageBox.Show($"Fill & Kill Failed: {result.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnFillKill.Enabled = true;
                    btnFillKill.BackColor = Color.DarkOrange;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnFillKill.Enabled = true;
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }

        private void btnExportErrors_Click(object sender, EventArgs e)
        {
            if (dgvInvalid.Rows.Count == 0)
            {
                MessageBox.Show("No error records to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Workbook|*.xlsx";
                sfd.Title = "Save Error Report";
                sfd.FileName = $"Error_Log_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _excelService.ExportGridToExcel(dgvInvalid, sfd.FileName, "Invalid Items");
                        MessageBox.Show("Error report exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Export Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion

        #region Core Processing Logic

        private async Task RunPreviewProcess(string filePath)
        {
            _busyHelper.ShowBusy("Validating File...");
            btnPreview.Enabled = false;
            btnSave.Enabled = false;

            // Reset Grids
            dgvInvalid.DataSource = null;
            dgvValid.DataSource = null;
            splitGridContainer.Panel2Collapsed = true;

            try
            {
                // 1. Validate and Parse
                var result = await Task.Run(() => _orderManager.ValidateOrderAsync(filePath, _currentMasterDto));

                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _currentPreviewData = result.Data;

                // 2. EXTRACT MONTH/YEAR FROM EXCEL DATA
                string extractedMonth = null;
                string extractedYear = null;

                if (_currentPreviewData.ValidOrderItems != null && _currentPreviewData.ValidOrderItems.Any())
                {
                    // Take the Month/Year from the first valid item in the list
                    var firstItem = _currentPreviewData.ValidOrderItems.First();
                    extractedMonth = firstItem.Month;
                    extractedYear = firstItem.Year;

                    // IMPORTANT: Update the Master DTO so the 'Save' button check passes later
                    _currentMasterDto.Month = extractedMonth;
                    _currentMasterDto.Year = extractedYear;

                    // 3. Bind Data to Grids
                    splitGridContainer.Visible = true;
                    splitGridContainer.Panel2Collapsed = false;

                    dgvValid.DataSource = _currentPreviewData.ValidOrderItems;
                    dgvInvalid.DataSource = _currentPreviewData.InvalidOrderItems;

                    if (dgvValid.Columns.Count > 0) dgvValid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    if (dgvInvalid.Columns.Count > 0)
                    {
                        dgvInvalid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        dgvInvalid.ColumnHeadersDefaultCellStyle.BackColor = Color.LightPink;
                    }

                    // Now passing the actual values instead of null
                    UpdateUIStateBasedOnValidation(extractedMonth, extractedYear);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
                btnPreview.Enabled = true;
            }
        }

        private void UpdateUIStateBasedOnValidation(string month, string year)
        {
            int invalidCount = _currentPreviewData.InvalidOrderItems.Count;
            int validCount = _currentPreviewData.ValidOrderItems.Count;

            lblValidTitle.Text = $"Valid Items ({validCount})";
            lblInvalidTitle.Text = $"Invalid Items ({invalidCount})";

            if (invalidCount > 0)
            {
                SetStatus($"Found {invalidCount} invalid items.", StatusType.Warning);
                btnSave.Enabled = false;
                btnSave.BackColor = Color.Gray;
                MessageBox.Show("File contains invalid items.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (validCount == 0)
            {
                SetStatus("File is empty.", StatusType.Error);
                btnSave.Enabled = false;
                btnSave.BackColor = Color.Gray;
            }
            else if (string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year))
            {
                // Items valid, but Date could not be read
                SetStatus("Valid items found, but Month/Year missing.", StatusType.Error);
                btnSave.Enabled = false;
                btnSave.BackColor = Color.Gray;
                MessageBox.Show("Could not determine Month or Year from the file.", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                // SUCCESS
                // We show the detected date in the Status Bar since the textbox is gone
                SetStatus($"Ready. Period: {month} {year}", StatusType.Success);
                btnSave.Enabled = true;
                btnSave.BackColor = Color.ForestGreen;
            }
        }

        private void ResetFormState()
        {
            txtDocNo.Text = "";

            _currentPreviewData = null;
            _currentMasterDto = null;

            dgvValid.DataSource = null;
            dgvInvalid.DataSource = null;
            splitGridContainer.Panel2Collapsed = true;

            btnPreview.Enabled = true;
            btnPreview.BackColor = Color.DodgerBlue;

            btnSave.Enabled = false;
            btnSave.BackColor = Color.Gray;

            btnFillKill.Enabled = false;
            btnFillKill.BackColor = Color.Gray;

            ToggleInputs(true);
            SetStatus("Ready", StatusType.Success);
        }

        #endregion

        #region Helpers & Styling

        private void ToggleInputs(bool isEnabled)
        {
            txtDocNo.Enabled = isEnabled;
            txtDocNo.BackColor = isEnabled ? Color.WhiteSmoke : Color.LightGray;
        }

        private void ApplyTheme()
        {
            this.BackColor = UITheme.BackgroundCanvas;
            pnlHeaderCard.BackColor = UITheme.SurfaceWhite;
            pnlGridCard.BackColor = UITheme.SurfaceWhite;

            UITheme.ApplyButtonTheme(btnPreview);

            lblHeaderTitle.ForeColor = Color.DimGray;
            lblStatus.ForeColor = Color.DimGray;

            UITheme.StyleGrid(dgvValid, true);
            UITheme.StyleGrid(dgvInvalid, false);
        }

        private void SetStatus(string msg, StatusType type)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => SetStatus(msg, type)));
                return;
            }
            lblStatus.Text = msg;
            lblStatus.ForeColor = type == StatusType.Error ? Color.FromArgb(220, 53, 69) :
                                  type == StatusType.Warning ? Color.DarkOrange :
                                  Color.FromArgb(40, 167, 69);
        }

        #endregion
    }
}