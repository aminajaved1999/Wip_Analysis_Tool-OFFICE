using System;
using System.Collections.Generic;

namespace WIPAT.Entities.ExcelTemplateDefinitions
{
    #region 1. ENUMS (System Vocabulary)

    public enum ImportExcelFileType
    {
        AddNewItemsToCatalogue,
        UpdateExistingCatalogue,
        ForecastFile,
        OrderFile,
        EditWipFile
    }

    public enum ExportExcelFileType
    {
        ExportFinalCalculatedWip,
        ExportItemsCatalogue,
        ExportWip
    }

    /// <summary>
    /// Dedicated enum for internal memory tables, system processing grids, or UI grids.
    /// Separates internal processing schemas from physical file export schemas.
    /// </summary>
    public enum DataTableTemplateType
    {
        WorkingWipCalculationGrid,
        ForecastBulkInsertTable,
        ProblemItemsTable,
        InvalidItemTable,
        InvalidStockTable,
        ValidStockTable,
        ItemCatalogueDataTable,
        ForecastUIDataTable,
        OrderUIDataTable,       // <-- Added for Order UI Grids
        OrderBulkInsertTable    // <-- NEW: Added for Order Bulk Inserts
    }

    public enum ExcelDataType
    {
        String,
        Int,
        Decimal,
        DateTime,
        Boolean
    }

    #endregion

    #region 2. CORE MODELS

    /// <summary>
    /// Represents the absolute definition of a column across the entire system.
    /// </summary>
    public class ColumnDefinition
    {
        public string Name { get; private set; }
        public ExcelDataType DataType { get; private set; }

        public ColumnDefinition(string name, ExcelDataType dataType)
        {
            Name = name;
            DataType = dataType;
        }
    }

    /// <summary>
    /// Represents how a column behaves inside a specific Excel template or DataTable.
    /// </summary>
    public class ColumnRule
    {
        public ColumnDefinition Definition { get; private set; }
        public bool IsHeaderRequired { get; private set; }
        public bool IsValueRequired { get; private set; }

        public ColumnRule(ColumnDefinition definition, bool isHeaderRequired = true, bool isValueRequired = true)
        {
            Definition = definition;
            IsHeaderRequired = isHeaderRequired;
            IsValueRequired = isValueRequired;
        }
    }

    #endregion

    #region 3. MASTER CATALOGUE 

    /// <summary>
    /// Every possible column your system could ever care about goes here.
    /// </summary>
    public static class MasterColumnCatalogue
    {
        // --- Standard / Shared ---
        public static readonly ColumnDefinition Casin = new ColumnDefinition("CASIN", ExcelDataType.String);
        public static readonly ColumnDefinition Model = new ColumnDefinition("Model", ExcelDataType.String);
        public static readonly ColumnDefinition Description = new ColumnDefinition("Description", ExcelDataType.String);
        public static readonly ColumnDefinition ColorName = new ColumnDefinition("ColorName", ExcelDataType.String);
        public static readonly ColumnDefinition Size = new ColumnDefinition("Size", ExcelDataType.String);
        public static readonly ColumnDefinition PCPK = new ColumnDefinition("PC/PK", ExcelDataType.Int);
        public static readonly ColumnDefinition CasePackQty = new ColumnDefinition("CasePackQty", ExcelDataType.Int);
        public static readonly ColumnDefinition OpeningStock = new ColumnDefinition("OpeningStock", ExcelDataType.Int);
        public static readonly ColumnDefinition Notes = new ColumnDefinition("Notes", ExcelDataType.String);
        public static readonly ColumnDefinition ItemStatus = new ColumnDefinition("ItemStatus", ExcelDataType.String);

        // --- Forecast Specific ---
        public static readonly ColumnDefinition RequestedQuantity = new ColumnDefinition("RequestedQuantity", ExcelDataType.Int);
        public static readonly ColumnDefinition CommitmentPeriod = new ColumnDefinition("CommitmentPeriod", ExcelDataType.Int);
        public static readonly ColumnDefinition PODate = new ColumnDefinition("PO Date", ExcelDataType.DateTime);
        public static readonly ColumnDefinition ProjectionMonth = new ColumnDefinition("ProjectionMonth", ExcelDataType.Int);
        public static readonly ColumnDefinition ProjectionYear = new ColumnDefinition("ProjectionYear", ExcelDataType.Int);

        // --- Order Specific ---
        public static readonly ColumnDefinition Quantity = new ColumnDefinition("Quantity", ExcelDataType.Int);
        public static readonly ColumnDefinition MonthInteger = new ColumnDefinition("Month", ExcelDataType.Int);
        public static readonly ColumnDefinition Year = new ColumnDefinition("Year", ExcelDataType.Int);

        // --- Export Specific ---
        public static readonly ColumnDefinition IsActive = new ColumnDefinition("IsActive", ExcelDataType.Boolean);
        public static readonly ColumnDefinition MonthString = new ColumnDefinition("Month", ExcelDataType.String);
        public static readonly ColumnDefinition WipQuantity = new ColumnDefinition("WIP Quantity", ExcelDataType.Int);
        public static readonly ColumnDefinition IssuedMonth = new ColumnDefinition("Issued Month", ExcelDataType.String);
        public static readonly ColumnDefinition IssuedYear = new ColumnDefinition("Issued Year", ExcelDataType.Int);
        public static readonly ColumnDefinition CasePack = new ColumnDefinition("CasePack", ExcelDataType.Int);
        public static readonly ColumnDefinition WipType = new ColumnDefinition("WIP Type", ExcelDataType.String);
        public static readonly ColumnDefinition CalculatedBy = new ColumnDefinition("Calculated By", ExcelDataType.String);

        // --- Internal DataTables / Working WIP Grid Specific ---
        public static readonly ColumnDefinition ActualOrder = new ColumnDefinition("Actual_Order", ExcelDataType.Int);
        public static readonly ColumnDefinition InitialStock = new ColumnDefinition("Initial_Stock", ExcelDataType.Int);
        public static readonly ColumnDefinition Stock = new ColumnDefinition("Stock", ExcelDataType.Int);
        public static readonly ColumnDefinition Arriving133Percent = new ColumnDefinition("Arriving_133%", ExcelDataType.Decimal);
        public static readonly ColumnDefinition GrossRequirement = new ColumnDefinition("grossRequirement", ExcelDataType.Decimal);

        public static readonly ColumnDefinition ReviewWip = new ColumnDefinition("Review_Wip", ExcelDataType.Int);
        public static readonly ColumnDefinition MoqWip = new ColumnDefinition("MOQ_Wip", ExcelDataType.Int);
        public static readonly ColumnDefinition MOQ = new ColumnDefinition("MOQ", ExcelDataType.Int);
        public static readonly ColumnDefinition CasePackWip = new ColumnDefinition("CasePack_Wip", ExcelDataType.Int);

        public static readonly ColumnDefinition Delta = new ColumnDefinition("Delta", ExcelDataType.Int);
        public static readonly ColumnDefinition StockLayman = new ColumnDefinition("Stock_Layman", ExcelDataType.String);

        public static readonly ColumnDefinition RequestedQuantityPrev = new ColumnDefinition("Requested_Quantity_Prev", ExcelDataType.Int);
        public static readonly ColumnDefinition WipPrev = new ColumnDefinition("Wip_Prev", ExcelDataType.Int);
        public static readonly ColumnDefinition RequestedQuantityCurr = new ColumnDefinition("Requested_Quantity_Curr", ExcelDataType.Int);
        public static readonly ColumnDefinition CommitmentPeriodCurr = new ColumnDefinition("CommitmentPeriod_Curr", ExcelDataType.String);

        // --- Database / Bulk Insert Specific ---
        public static readonly ColumnDefinition ItemCatalogueId = new ColumnDefinition("ItemCatalogueId", ExcelDataType.Int);
        public static readonly ColumnDefinition POForecastMasterId = new ColumnDefinition("POForecastMasterId", ExcelDataType.Int);
        public static readonly ColumnDefinition POOrderMasterId = new ColumnDefinition("POOrderMasterId", ExcelDataType.Int); // <-- NEW: Added for Order Bulk Insert

        // Exact spelling/type mappings required by bulkTable in CreateForecastBulkInsertTable
        public static readonly ColumnDefinition ItemStatusInt = new ColumnDefinition("ItemStatus", ExcelDataType.Int);

        // --- Problem Items Specific ---
        public static readonly ColumnDefinition FileName = new ColumnDefinition("FileName", ExcelDataType.String);
        public static readonly ColumnDefinition Reason = new ColumnDefinition("Reason", ExcelDataType.String);

        // --- Invalid / Valid DataTables Specific ---
        public static readonly ColumnDefinition CreatedAt = new ColumnDefinition("CreatedAt", ExcelDataType.DateTime);
        public static readonly ColumnDefinition CreatedById = new ColumnDefinition("CreatedById", ExcelDataType.Int);
        public static readonly ColumnDefinition UpdatedAt = new ColumnDefinition("UpdatedAt", ExcelDataType.DateTime);
        public static readonly ColumnDefinition UpdatedById = new ColumnDefinition("UpdatedById", ExcelDataType.Int);

        //
        public static readonly ColumnDefinition UserWipQuantity = new ColumnDefinition("WipQuantity", ExcelDataType.Int);
    }

    #endregion

    #region 4. TEMPLATE FACTORY

    /// <summary>
    /// Defines WHICH columns belong to WHICH file or grid, and how strict they are.
    /// </summary>
    public static class FileTemplateFactory
    {
        public static IReadOnlyList<ColumnRule> GetImportTemplate(ImportExcelFileType fileType)
        {
            switch (fileType)
            {
                case ImportExcelFileType.AddNewItemsToCatalogue:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Model),
                        new ColumnRule(MasterColumnCatalogue.Description),
                        new ColumnRule(MasterColumnCatalogue.ColorName),
                        new ColumnRule(MasterColumnCatalogue.Size),
                        new ColumnRule(MasterColumnCatalogue.PCPK),
                        new ColumnRule(MasterColumnCatalogue.CasePackQty),
                        new ColumnRule(MasterColumnCatalogue.OpeningStock),
                        new ColumnRule(MasterColumnCatalogue.Notes, true, false)
                    }.AsReadOnly();

                case ImportExcelFileType.UpdateExistingCatalogue:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Model),
                        new ColumnRule(MasterColumnCatalogue.Description),
                        new ColumnRule(MasterColumnCatalogue.ColorName),
                        new ColumnRule(MasterColumnCatalogue.Size),
                        new ColumnRule(MasterColumnCatalogue.PCPK),
                        new ColumnRule(MasterColumnCatalogue.CasePackQty),
                        new ColumnRule(MasterColumnCatalogue.OpeningStock),
                        new ColumnRule(MasterColumnCatalogue.Notes, true, false),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus)
                    }.AsReadOnly();

                case ImportExcelFileType.ForecastFile:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.RequestedQuantity),
                        new ColumnRule(MasterColumnCatalogue.CommitmentPeriod),
                        new ColumnRule(MasterColumnCatalogue.PODate),
                        new ColumnRule(MasterColumnCatalogue.ProjectionMonth),
                        new ColumnRule(MasterColumnCatalogue.ProjectionYear),

                        new ColumnRule(MasterColumnCatalogue.MonthString, false, false),
                        new ColumnRule(MasterColumnCatalogue.Year, false, false),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus, false, false)
                    }.AsReadOnly();

                case ImportExcelFileType.OrderFile:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Quantity),
                        new ColumnRule(MasterColumnCatalogue.MonthInteger),
                        new ColumnRule(MasterColumnCatalogue.Year)
                    }.AsReadOnly();
                case ImportExcelFileType.EditWipFile:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.WipQuantity)
                    }.AsReadOnly();

                default:
                    throw new ArgumentException("No template configured for: " + fileType);
            }
        }

        public static IReadOnlyList<ColumnRule> GetExportTemplate(ExportExcelFileType fileType)
        {
            switch (fileType)
            {
                case ExportExcelFileType.ExportFinalCalculatedWip:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.IsActive),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus),
                        new ColumnRule(MasterColumnCatalogue.MonthString),
                        new ColumnRule(MasterColumnCatalogue.Year),
                        new ColumnRule(MasterColumnCatalogue.WipQuantity),
                        new ColumnRule(MasterColumnCatalogue.CommitmentPeriod),
                        new ColumnRule(MasterColumnCatalogue.IssuedMonth),
                        new ColumnRule(MasterColumnCatalogue.IssuedYear),
                        new ColumnRule(MasterColumnCatalogue.CasePack),
                        new ColumnRule(MasterColumnCatalogue.WipType),
                        new ColumnRule(MasterColumnCatalogue.CalculatedBy)
                    }.AsReadOnly();

                case ExportExcelFileType.ExportItemsCatalogue:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Model),
                        new ColumnRule(MasterColumnCatalogue.Description),
                        new ColumnRule(MasterColumnCatalogue.ColorName),
                        new ColumnRule(MasterColumnCatalogue.Size),
                        new ColumnRule(MasterColumnCatalogue.PCPK),
                        new ColumnRule(MasterColumnCatalogue.CasePackQty),
                        new ColumnRule(MasterColumnCatalogue.OpeningStock),
                        new ColumnRule(MasterColumnCatalogue.Notes),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus)
                    }.AsReadOnly();

                case ExportExcelFileType.ExportWip:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.WipQuantity),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus)
                    }.AsReadOnly();

                default:
                    throw new ArgumentException("No export template configured for: " + fileType);
            }
        }

        /// <summary>
        /// Retrieves column rules specifically for internal memory DataTables or UI Grids.
        /// Added isUpdate parameter to dynamically generate schemas based on insert vs update operations.
        /// </summary>
        public static IReadOnlyList<ColumnRule> GetDataTableTemplate(DataTableTemplateType gridType, bool isUpdate = false)
        {
            switch (gridType)
            {
                case DataTableTemplateType.WorkingWipCalculationGrid:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus),
                        new ColumnRule(MasterColumnCatalogue.MonthString),
                        new ColumnRule(MasterColumnCatalogue.Year),
                        new ColumnRule(MasterColumnCatalogue.PODate),
                        new ColumnRule(MasterColumnCatalogue.RequestedQuantityPrev),
                        new ColumnRule(MasterColumnCatalogue.WipPrev),
                        new ColumnRule(MasterColumnCatalogue.RequestedQuantityCurr),
                        new ColumnRule(MasterColumnCatalogue.Arriving133Percent, false, false),
                        new ColumnRule(MasterColumnCatalogue.CommitmentPeriodCurr),
                        new ColumnRule(MasterColumnCatalogue.ActualOrder, false, false),
                        new ColumnRule(MasterColumnCatalogue.InitialStock),
                        new ColumnRule(MasterColumnCatalogue.Stock),
                        new ColumnRule(MasterColumnCatalogue.Delta, false, false),
                        new ColumnRule(MasterColumnCatalogue.StockLayman, false, false),
                        new ColumnRule(MasterColumnCatalogue.GrossRequirement, false, false),
                        new ColumnRule(MasterColumnCatalogue.ReviewWip),
                        new ColumnRule(MasterColumnCatalogue.MoqWip, false, false),
                        new ColumnRule(MasterColumnCatalogue.MOQ, false, false),
                        new ColumnRule(MasterColumnCatalogue.CasePackWip, false, false),
                        new ColumnRule(MasterColumnCatalogue.CasePack, false, false)
                    }.AsReadOnly();

                case DataTableTemplateType.ForecastBulkInsertTable:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.ItemCatalogueId),
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.RequestedQuantity),
                        new ColumnRule(MasterColumnCatalogue.CommitmentPeriod),
                        new ColumnRule(MasterColumnCatalogue.PODate),
                        new ColumnRule(MasterColumnCatalogue.MonthString),
                        new ColumnRule(MasterColumnCatalogue.Year),
                        new ColumnRule(MasterColumnCatalogue.POForecastMasterId),
                        new ColumnRule(MasterColumnCatalogue.ItemStatusInt),
                        new ColumnRule(MasterColumnCatalogue.Model,true, false),
                        new ColumnRule(MasterColumnCatalogue.CreatedById,true, false),
                        new ColumnRule(MasterColumnCatalogue.CreatedAt,true, false),
                    }.AsReadOnly();

                case DataTableTemplateType.OrderBulkInsertTable: // <-- NEW: ORDER BULK INSERT DEFINITION
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.ItemCatalogueId),
                        new ColumnRule(MasterColumnCatalogue.POOrderMasterId),
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Quantity),
                        new ColumnRule(MasterColumnCatalogue.MonthString), 
                        new ColumnRule(MasterColumnCatalogue.Year),
                        new ColumnRule(MasterColumnCatalogue.ItemStatusInt),
                        new ColumnRule(MasterColumnCatalogue.FileName),
                        new ColumnRule(MasterColumnCatalogue.CreatedById, true, false),
                        new ColumnRule(MasterColumnCatalogue.CreatedAt, true, false)
                    }.AsReadOnly();

                case DataTableTemplateType.ProblemItemsTable:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.FileName),
                        new ColumnRule(MasterColumnCatalogue.Reason)
                    }.AsReadOnly();

                case DataTableTemplateType.InvalidItemTable:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Model, true, false),
                        new ColumnRule(MasterColumnCatalogue.Description, true, false),
                        new ColumnRule(MasterColumnCatalogue.ColorName, true, false),
                        new ColumnRule(MasterColumnCatalogue.Size, true, false),
                        new ColumnRule(MasterColumnCatalogue.PCPK, true, false),
                        new ColumnRule(MasterColumnCatalogue.CasePackQty),
                        new ColumnRule(MasterColumnCatalogue.CreatedAt),
                        new ColumnRule(MasterColumnCatalogue.CreatedById),
                        new ColumnRule(MasterColumnCatalogue.Notes, true, false),
                        new ColumnRule(MasterColumnCatalogue.ItemStatusInt)
                    }.AsReadOnly();

                case DataTableTemplateType.InvalidStockTable:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.ItemCatalogueId, true, false),
                        new ColumnRule(MasterColumnCatalogue.OpeningStock),
                        new ColumnRule(MasterColumnCatalogue.CreatedAt),
                        new ColumnRule(MasterColumnCatalogue.CreatedById)
                    }.AsReadOnly();

                case DataTableTemplateType.ValidStockTable:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.OpeningStock),
                        new ColumnRule(MasterColumnCatalogue.ItemCatalogueId, false, false),
                        new ColumnRule(MasterColumnCatalogue.CreatedAt, false, false),
                        new ColumnRule(MasterColumnCatalogue.CreatedById, false, false),
                        new ColumnRule(MasterColumnCatalogue.UpdatedAt, false, false),
                        new ColumnRule(MasterColumnCatalogue.UpdatedById, false, false)
                    }.AsReadOnly();

                case DataTableTemplateType.ForecastUIDataTable:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.RequestedQuantity),
                        new ColumnRule(MasterColumnCatalogue.CommitmentPeriod),
                        new ColumnRule(MasterColumnCatalogue.PODate),
                        new ColumnRule(MasterColumnCatalogue.MonthString),
                        new ColumnRule(MasterColumnCatalogue.Year),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus),
                    }.AsReadOnly();

                case DataTableTemplateType.OrderUIDataTable:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.ItemCatalogueId),
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Quantity),
                        new ColumnRule(MasterColumnCatalogue.MonthString),
                        new ColumnRule(MasterColumnCatalogue.Year),
                        new ColumnRule(MasterColumnCatalogue.ItemStatusInt)
                    }.AsReadOnly();

                // --- ItemCatalogueDataTable with dynamic rules based on Insert/Update ---
                case DataTableTemplateType.ItemCatalogueDataTable:
                    var catalogueColumns = new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Model),
                        new ColumnRule(MasterColumnCatalogue.Description),
                        new ColumnRule(MasterColumnCatalogue.ColorName),
                        new ColumnRule(MasterColumnCatalogue.Size),
                        new ColumnRule(MasterColumnCatalogue.PCPK),
                        new ColumnRule(MasterColumnCatalogue.CasePackQty),
                        new ColumnRule(MasterColumnCatalogue.OpeningStock),
                        new ColumnRule(MasterColumnCatalogue.Notes, true, false),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus)
                    };

                    if (isUpdate)
                    {
                        catalogueColumns.Add(new ColumnRule(MasterColumnCatalogue.UpdatedAt));
                        catalogueColumns.Add(new ColumnRule(MasterColumnCatalogue.UpdatedById));
                    }
                    else
                    {
                        catalogueColumns.Add(new ColumnRule(MasterColumnCatalogue.CreatedAt));
                        catalogueColumns.Add(new ColumnRule(MasterColumnCatalogue.CreatedById));
                    }

                    return catalogueColumns.AsReadOnly();

                default:
                    throw new ArgumentException("No DataTable template configured for: " + gridType);
            }
        }
    }

    #endregion

    /// <summary>
    /// Extension methods for ExcelDataType to provide global mapping functions.
    /// </summary>
    public static class ExcelDataTypeExtensions
    {
        public static Type ToDotNetType(this ExcelDataType dataType)
        {
            switch (dataType)
            {
                case ExcelDataType.String: return typeof(string);
                case ExcelDataType.Int: return typeof(int);
                case ExcelDataType.Decimal: return typeof(double);
                case ExcelDataType.DateTime: return typeof(DateTime);
                case ExcelDataType.Boolean: return typeof(bool);
                default: return typeof(string);
            }
        }
    }
}