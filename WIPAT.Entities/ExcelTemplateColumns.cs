using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public static class ExcelTemplateColumns
    {
        // Exact headers as they appear in Excel
        public const string Casin = "CASIN";
        public const string Model = "Model";
        public const string Description = "Description";
        public const string ColorName = "ColorName";
        public const string Size = "Size";
        public const string PCPK = "PC/PK";
        public const string CasePackQty = "Case Pack Qty";
        public const string OpeningStock = "OpeningStock";
        public const string Notes = "Notes";
        public const string ItemStatus = "ItemStatus";

        /// <summary>
        /// Required columns for IMPORTING NEW items.
        /// </summary>
        public static readonly IReadOnlyList<string> ImportRequiredColumns = new List<string>
        {
            Casin, Model, Description, ColorName, Size, PCPK, CasePackQty, OpeningStock, Notes
        }.AsReadOnly();

        /// <summary>
        /// Required columns for UPDATING EXISTING items.
        /// (Same as import, but explicitly adds ItemStatus)
        /// </summary>
        public static readonly IReadOnlyList<string> UpdateRequiredColumns = new List<string>
        {
            Casin, Model, Description, ColorName, Size, PCPK, CasePackQty, OpeningStock, Notes, ItemStatus
        }.AsReadOnly();
    }
}