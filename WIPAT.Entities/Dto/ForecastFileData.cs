using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WIPAT.Entities.Dto
{
    public class ForecastFileData
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DataTable ForecastViewTable { get; set; }
        public DataTable ForecastCompleteTable { get; set; }
        public string ProjectionMonth { get; set; }
        public string ProjectionYear { get; set; }
        public string ForecastFor { get; set; }
        public bool IsWipAlreadyCalculated { get; set; }
        public bool IsContinueWithInactiveItems { get; set; }
        public List<string> MissingItems { get; set; } = new List<string>();
        public List<string> DeactivatedItems { get; set; } = new List<string>();
        public DataTable ProblemItemsTable { get; set; } = new DataTable();

        public ForecastMaster Forecast { get; set; }

        // NEW: UI Components
        public DataGridView BoundGrid { get; set; }
        public Label BoundLabel { get; set; }

        // Optional: UI Binder Method
        public void BindToUI()
        {
            if (BoundGrid != null)
            {
                BoundGrid.DataSource = ForecastViewTable;
                BoundGrid.Visible = true;
            }

            if (BoundLabel != null)
            {
                BoundLabel.Text = $"{ProjectionMonth} {ProjectionYear}";
                BoundLabel.Visible = true;
            }
        }
    }
}
