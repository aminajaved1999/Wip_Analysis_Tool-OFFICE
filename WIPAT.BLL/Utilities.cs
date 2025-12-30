using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.BLL
{
    public static class AllColumnNames
    {
        // Column constants
        public const string CAsin = "C-ASIN";
        public const string Model = "Model";
        public const string Description = "Description";
        public const string ColorName = "Color Name";
        public const string Size = "Size";
        public const string PCPK = "PC/PK";
        public const string CasePackQty = "Case Pack Qty";
        public const string OpeningStock = "OpeningStock";
        public const string ItemCatalogueId = "ItemCatalogueId";
        public const string CreatedAt = "CreatedAt";
        public const string CreatedById = "CreatedById";

        // Expose all as an array
        public static IEnumerable<string> ExcelColumnNames => new[] { CAsin, Model, Description, ColorName, Size, PCPK, CasePackQty, OpeningStock };

        public static IEnumerable<string> CatalogueTableColumns => ExcelColumnNames.Concat(new[] { CreatedAt, CreatedById });

        public static IEnumerable<string> StockTableColumns => new[] { ItemCatalogueId, CAsin, OpeningStock, CreatedAt, CreatedById };

        // This method determines the type based on the column name
        public static Type GetColumnType(string columnName)
        {
            if (columnName == ItemCatalogueId || columnName == CreatedById || columnName == OpeningStock)
            {
                return typeof(int);

            }
            else if (columnName == CreatedAt)
            {
                return typeof(DateTime);
            }
            else
            {
                return typeof(string);
            }

            throw new ArgumentException($"Unknown column: {columnName}");
        }

        // get Excel column indexes
        public static Dictionary<string, int> ExcelColumnIndexes
        {
            get
            {
                return ExcelColumnNames
                    .Select((columnName, index) => new { columnName, index = index + 1 }) // Add 1 to the index
                    .ToDictionary(x => x.columnName, x => x.index);
            }
        }

    }
}
