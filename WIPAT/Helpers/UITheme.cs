using System;
using System.Drawing;
using System.Windows.Forms;

namespace WIPAT.Helpers
{
    // The strict button taxonomy
    public enum AppButtonStyle
    {
        SignIn,
        Upload,
        Calculate,
        ApproveAndSave,
        ExportToExcel,
        LoadDbData,
        PreviewAdd,
        PreviewUpdate,
        Refresh,
        Search,
        Danger,
        Secondary
    }

    public static class UITheme
    {
        #region --- GLOBAL COLOR PALETTE (DESIGN TOKENS) ---

        // =========================================================
        // TIER 1: BASE PALETTE (Primitive Colors)
        // Strictly private. Define the raw RGB values only once here.
        // =========================================================
        private static readonly Color BrandMidnight = Color.FromArgb(0, 0, 64);
        private static readonly Color BrandGhostWhite = Color.FromArgb(245, 246, 250);
        private static readonly Color PureWhite = Color.White;
        private static readonly Color PureBlack = Color.Black;
        private static readonly Color DimGrayBase = Color.DimGray;
        private static readonly Color LightGrayBorder = Color.FromArgb(220, 220, 220);

        private static readonly Color PrimaryBlue = Color.FromArgb(0, 123, 255);
        private static readonly Color PrimaryBlueHover = Color.FromArgb(51, 153, 255);
        private static readonly Color SecondaryBlue = Color.FromArgb(0, 122, 204);
        private static readonly Color SecondaryBlueHover = Color.FromArgb(51, 163, 235);

        private static readonly Color SuccessGreen = Color.FromArgb(46, 125, 50);
        private static readonly Color SuccessGreenHover = Color.FromArgb(67, 160, 71);
        private static readonly Color ExcelGreen = Color.FromArgb(29, 111, 66);
        private static readonly Color ExcelGreenHover = Color.FromArgb(46, 139, 87);

        private static readonly Color GoldenYellow = Color.FromArgb(212, 175, 55);
        private static readonly Color GoldenYellowHover = Color.FromArgb(230, 195, 85);

        private static readonly Color BrightAzure = Color.FromArgb(2, 136, 209);
        private static readonly Color BrightAzureHover = Color.FromArgb(3, 169, 244);

        private static readonly Color MagentaBase = Color.FromArgb(194, 24, 91);
        private static readonly Color MagentaHover = Color.FromArgb(233, 30, 99);

        private static readonly Color DarkSlateGray = Color.FromArgb(69, 90, 100);
        private static readonly Color DarkSlateGrayHover = Color.FromArgb(84, 110, 122);

        private static readonly Color CrimsonRed = Color.FromArgb(220, 53, 69);
        private static readonly Color CrimsonRedHover = Color.FromArgb(200, 35, 51);
        private static readonly Color SoftRedTint = Color.FromArgb(255, 245, 245);
        private static readonly Color DarkRedTextBase = Color.FromArgb(114, 28, 36);

        private static readonly Color StandardGray = Color.FromArgb(108, 117, 125);
        private static readonly Color StandardGrayHover = Color.FromArgb(134, 142, 150);

        private static readonly Color WarningAmber = Color.FromArgb(255, 193, 7);

        private static readonly Color OxfordBlue = Color.FromArgb(15, 35, 70);
        private static readonly Color AliceBlue = Color.FromArgb(220, 235, 252);


        // =========================================================
        // TIER 2: SEMANTIC TOKENS (Functional Colors)
        // Publicly accessible. Map application logic to base colors.
        // =========================================================

        // 1. App Backgrounds
        public static readonly Color MainColor = BrandMidnight;
        public static readonly Color BackgroundCanvas = BrandGhostWhite;
        public static readonly Color SurfaceWhite = PureWhite;

        // 2. Action Colors (Base States)
        public static readonly Color ButtonTextColor = PureWhite;
        public static readonly Color TextSecondaryColor = DimGrayBase;

        public static readonly Color Upload_Color = PrimaryBlue;
        public static readonly Color Calculate_Color = SecondaryBlue;
        public static readonly Color Success_Color = SuccessGreen;
        public static readonly Color Excel_Color = ExcelGreen;

        public static readonly Color LoadDb_Color = GoldenYellow;
        public static readonly Color Refresh_Color = GoldenYellow; // Shared base

        public static readonly Color PreviewAdd_Color = BrightAzure;
        public static readonly Color PreviewUpdate_Color = MagentaBase;
        public static readonly Color Search_Color = DarkSlateGray;
        public static readonly Color Danger_Color = CrimsonRed;
        public static readonly Color Secondary_Color = StandardGray;
        public static readonly Color WarningColor = WarningAmber;

        // 2.1 _ Hover Colors (The "Pop" Effect)
        public static readonly Color Upload_Hover_Color = PrimaryBlueHover;
        public static readonly Color Calculate_Hover_Color = SecondaryBlueHover;
        public static readonly Color Success_Hover_Color = SuccessGreenHover;
        public static readonly Color Excel_Hover_Color = ExcelGreenHover;

        public static readonly Color LoadDb_Hover_Color = GoldenYellowHover;
        public static readonly Color Refresh_Hover_Color = GoldenYellowHover; // Shared base

        public static readonly Color PreviewAdd_Hover_Color = BrightAzureHover;
        public static readonly Color PreviewUpdate_Hover_Color = MagentaHover;
        public static readonly Color Search_Hover_Color = DarkSlateGrayHover;
        public static readonly Color Danger_Hover_Color = CrimsonRedHover;
        public static readonly Color Secondary_Hover_Color = StandardGrayHover;

        // 3. Grid Colors (Standard)
        public static readonly Color GridBorder = LightGrayBorder;
        public static readonly Color GridHeaderNavy = OxfordBlue;
        public static readonly Color GridHeaderText = PureWhite;
        public static readonly Color GridRowText = PureBlack;
        public static readonly Color GridSelectionBlue = AliceBlue;
        public static readonly Color GridSelectionText = PureBlack;

        // 4. Grid Colors (Error / Invalid)
        public static readonly Color GridErrorHeader = CrimsonRed; // Shared base with Danger_Color
        public static readonly Color GridErrorRowBg = SoftRedTint;
        public static readonly Color GridErrorRowText = DarkRedTextBase;
        public static readonly Color GridErrorSelection = CrimsonRed; // Shared base
        public static readonly Color GridErrorSelectionText = PureWhite;

        #endregion


        #region --- BUTTON STYLING ---

        public static void StyleButton(Button btn, AppButtonStyle style)
        {
            if (btn == null) return;

            // Universal Button DNA
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.ForeColor = ButtonTextColor;
            btn.Margin = new Padding(0, 0, 10, 0);

            // Apply Specific Identity and Hover Effects
            switch (style)
            {
                case AppButtonStyle.SignIn:
                    ApplyButtonInteraction(btn, MainColor, Upload_Hover_Color);
                    break;
                case AppButtonStyle.Upload:
                    ApplyButtonInteraction(btn, Upload_Color, Upload_Hover_Color);
                    break;
                case AppButtonStyle.Calculate:
                    ApplyButtonInteraction(btn, Calculate_Color, Calculate_Hover_Color);
                    break;
                case AppButtonStyle.ApproveAndSave:
                    ApplyButtonInteraction(btn, Success_Color, Success_Hover_Color);
                    break;
                case AppButtonStyle.ExportToExcel:
                    ApplyButtonInteraction(btn, Excel_Color, Excel_Hover_Color);
                    break;
                case AppButtonStyle.LoadDbData:
                    ApplyButtonInteraction(btn, LoadDb_Color, LoadDb_Hover_Color);
                    break;
                case AppButtonStyle.Refresh:
                    ApplyButtonInteraction(btn, Refresh_Color, Refresh_Hover_Color);
                    break;
                case AppButtonStyle.PreviewAdd:
                    ApplyButtonInteraction(btn, PreviewAdd_Color, PreviewAdd_Hover_Color);
                    break;
                case AppButtonStyle.PreviewUpdate:
                    ApplyButtonInteraction(btn, PreviewUpdate_Color, PreviewUpdate_Hover_Color);
                    break;
                case AppButtonStyle.Search:
                    ApplyButtonInteraction(btn, Search_Color, Search_Hover_Color);
                    break;
                case AppButtonStyle.Danger:
                    ApplyButtonInteraction(btn, Danger_Color, Danger_Hover_Color);
                    break;
                case AppButtonStyle.Secondary:
                    ApplyButtonInteraction(btn, Secondary_Color, Secondary_Hover_Color);
                    break;
            }
        }

        // Centralized helper to create the seamless hover & click transitions
        private static void ApplyButtonInteraction(Button btn, Color baseColor, Color hoverColor)
        {
            btn.BackColor = baseColor;
            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            btn.FlatAppearance.MouseDownBackColor = baseColor;
        }

        #endregion --- BUTTON STYLING ---


        #region --- GRID STYLING ---

        public static void StyleGrid(DataGridView grid, bool isValid = true)
        {
            if (grid == null) return;

            // 1. Base Grid Styling
            grid.BackgroundColor = SurfaceWhite;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = GridBorder;

            // 2. Clean Layout
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // 3. Header Dimensions & Padding
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            // Lock height
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 45;

            // Add padding so text isn't pressed against the border
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 0, 0, 0);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // 4. Row Dimensions
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            grid.RowTemplate.Height = 35;

            // 5. Apply Contextual Theme using Global Colors
            if (isValid)
            {
                // The sleek "Enterprise Navy" look
                grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderNavy;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderText;

                grid.DefaultCellStyle.BackColor = SurfaceWhite;
                grid.DefaultCellStyle.ForeColor = GridRowText;
                grid.DefaultCellStyle.SelectionBackColor = GridSelectionBlue;
                grid.DefaultCellStyle.SelectionForeColor = GridSelectionText;
            }
            else
            {
                // The "Crimson Red" Error look
                grid.ColumnHeadersDefaultCellStyle.BackColor = GridErrorHeader;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderText;

                grid.DefaultCellStyle.BackColor = SurfaceWhite;
                grid.DefaultCellStyle.ForeColor = GridErrorRowText;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
                grid.DefaultCellStyle.SelectionForeColor = PureBlack;
            }
        }

        #endregion --- GRID STYLING ---

        #region --- GRID SUMMARY WIDGET ---

        /// <summary>
        /// Reads a standard DataGridView to count active, inactive, and invalid items,
        /// and dynamically updates and hides/shows the provided summary labels.
        /// </summary>
        public static void UpdateGridSummaryCounts(DataGridView grid, Label lblTotal, Label lblActive = null, Label lblInactive = null, Label lblInvalid = null)
        {
            if (grid == null) return;

            int activeCount = 0;
            int inactiveCount = 0;
            int invalidCount = 0;
            int totalCount = 0;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                totalCount++;

                bool isActive = true; // Default to active if the grid is missing the column
                bool isInvalid = false;

                // 1. Check for Invalid status
                string statusColName = grid.Columns.Contains("ItemStatus") ? "ItemStatus" :
                                       grid.Columns.Contains("Item Status") ? "Item Status" : string.Empty;

                if (!string.IsNullOrEmpty(statusColName) && row.Cells[statusColName].Value != null)
                {
                    string statusText = row.Cells[statusColName].Value.ToString();
                    if (statusText.Equals("Invalid", StringComparison.OrdinalIgnoreCase))
                    {
                        isInvalid = true;
                        isActive = false;
                    }
                    else if (statusText.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = false;
                    }
                }

                // 2. Check for Active status (only if not already marked invalid)
                if (!isInvalid)
                {
                    string activeColName = grid.Columns.Contains("isActive") ? "isActive" :
                                           grid.Columns.Contains("IsActive") ? "IsActive" : string.Empty;

                    if (!string.IsNullOrEmpty(activeColName))
                    {
                        var val = row.Cells[activeColName].Value;
                        if (val is bool b)
                            isActive = b;
                        else if (val != null && (val.ToString() == "1" || val.ToString().Equals("true", StringComparison.OrdinalIgnoreCase)))
                            isActive = true;
                        else
                            isActive = false;
                    }
                }

                // 3. Tally counts
                if (isInvalid)
                    invalidCount++;
                else if (isActive)
                    activeCount++;
                else
                    inactiveCount++;
            }

            // 4. Update UI Text and Visibility dynamically
            if (lblTotal != null)
            {
                lblTotal.Text = $"Total Items: {totalCount}";
                lblTotal.Visible = true; // Always show total count
            }

            if (lblActive != null)
            {
                lblActive.Text = $"Active: {activeCount}";
                lblActive.Visible = activeCount > 0; // Show only if count > 0
            }

            if (lblInactive != null)
            {
                lblInactive.Text = $"Deactivated: {inactiveCount}";
                lblInactive.Visible = inactiveCount > 0; // Show only if count > 0
            }

            if (lblInvalid != null)
            {
                lblInvalid.Text = $"Invalid: {invalidCount}";
                lblInvalid.Visible = invalidCount > 0; // Show only if count > 0
            }
        }

        #endregion --- GRID SUMMARY WIDGET ---

        public static void SetFormIcon(Form form)
        {
            if (form == null) { return; }

            try
            {
                form.Icon = Properties.Resources.icon;
            }
            catch (Exception)
            {
                // Safe fallback
            }
        }
    }
}