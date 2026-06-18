using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public static class AllColumnNames
    {
        // Column constants
        public const string CAsin = "CASIN";
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

        public const string UpdatedAt = "UpdatedAt";
        public const string UpdatedById = "UpdatedById";

        public const string Notes = "Notes";
        public const string IsActive = "IsActive";
        public const string ItemStatus = "ItemStatus";

        public const string OrderQty = "OrderQty";          // Add this
        public const string ProductionQty = "ProductionQty"; // Add this


        // Expose all as an array (Included IsActive here)
        public static IEnumerable<string> ExcelColumnNames => new[] { CAsin, Model, Description, ColorName, Size, PCPK, CasePackQty, OpeningStock, Notes, IsActive };

        public static IEnumerable<string> CatalogueTableColumns => ExcelColumnNames.Concat(new[] { CreatedAt, CreatedById });

        public static IEnumerable<string> StockTableColumns => new[] { ItemCatalogueId, CAsin, OpeningStock, CreatedAt, CreatedById };

        // This method determines the type based on the column name
        public static Type GetColumnType(string columnName)
        {
            // NEW: Added UpdatedById
            if (columnName == ItemCatalogueId || columnName == CreatedById || columnName == UpdatedById || columnName == OpeningStock)
            {
                return typeof(int);
            }
            // NEW: Added UpdatedAt
            else if (columnName == CreatedAt || columnName == UpdatedAt)
            {
                return typeof(DateTime);
            }
            else
            {
                // IsActive is registered as string to accept flexible values (TRUE, FALSE, 1, 0, Yes, No)
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