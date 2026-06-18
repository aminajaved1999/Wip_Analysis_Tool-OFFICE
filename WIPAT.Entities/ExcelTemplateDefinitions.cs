using System;
using System.Collections.Generic;
using System.Configuration;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace WIPAT.Entities.ExcelTemplateDefinitions
{
    #region 1. ENUMS (System Vocabulary)

    public enum ImportExcelFileType
    {
        AddNewItemsToCatalogue,
        UpdateExistingCatalogue,
        ForecastFile,
        OrderFile
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
        WorkingWipCalculationGrid
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
        // Core calculation columns
        public static readonly ColumnDefinition ActualOrder = new ColumnDefinition("Actual_Order", ExcelDataType.Int);
        public static readonly ColumnDefinition InitialStock = new ColumnDefinition("Initial_Stock", ExcelDataType.Int);
        public static readonly ColumnDefinition Stock = new ColumnDefinition("Stock", ExcelDataType.Int);
        public static readonly ColumnDefinition Arriving133Percent = new ColumnDefinition("Arriving_133%", ExcelDataType.Decimal);
        public static readonly ColumnDefinition GrossRequirement = new ColumnDefinition("grossRequirement", ExcelDataType.Decimal);

        // WIP Result columns
        public static readonly ColumnDefinition ReviewWip = new ColumnDefinition("Review_Wip", ExcelDataType.Int);
        public static readonly ColumnDefinition MoqWip = new ColumnDefinition("MOQ_Wip", ExcelDataType.Int);
        public static readonly ColumnDefinition MOQ = new ColumnDefinition("MOQ", ExcelDataType.Int);
        public static readonly ColumnDefinition CasePackWip = new ColumnDefinition("CasePack_Wip", ExcelDataType.Int);

        // Layman specific columns
        public static readonly ColumnDefinition Delta = new ColumnDefinition("Delta", ExcelDataType.Int);
        public static readonly ColumnDefinition StockLayman = new ColumnDefinition("Stock_Layman", ExcelDataType.String);

        // Base definitions for Dynamic Columns (Used as template identifiers, renamed at runtime)
        public static readonly ColumnDefinition RequestedQuantityPrev = new ColumnDefinition("Requested_Quantity_Prev", ExcelDataType.Int);
        public static readonly ColumnDefinition WipPrev = new ColumnDefinition("Wip_Prev", ExcelDataType.Int);
        public static readonly ColumnDefinition RequestedQuantityCurr = new ColumnDefinition("Requested_Quantity_Curr", ExcelDataType.Int);
        public static readonly ColumnDefinition CommitmentPeriodCurr = new ColumnDefinition("CommitmentPeriod_Curr", ExcelDataType.String);
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

                        // Optional columns for downstream processing
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
        /// </summary>
        public static IReadOnlyList<ColumnRule> GetDataTableTemplate(DataTableTemplateType gridType)
        {
            switch (gridType)
            {
                case DataTableTemplateType.WorkingWipCalculationGrid:
                    return new List<ColumnRule>
                    {
                        // Standard Identity
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.ItemStatus),
                        new ColumnRule(MasterColumnCatalogue.MonthString),
                        new ColumnRule(MasterColumnCatalogue.Year),
                        new ColumnRule(MasterColumnCatalogue.PODate),

                        // Dynamic Previous/Current Month Data
                        new ColumnRule(MasterColumnCatalogue.RequestedQuantityPrev),
                        new ColumnRule(MasterColumnCatalogue.WipPrev),
                        new ColumnRule(MasterColumnCatalogue.RequestedQuantityCurr),
                        new ColumnRule(MasterColumnCatalogue.Arriving133Percent, false, false),
                        new ColumnRule(MasterColumnCatalogue.CommitmentPeriodCurr),

                        // Stock and Orders
                        new ColumnRule(MasterColumnCatalogue.ActualOrder, false, false),
                        new ColumnRule(MasterColumnCatalogue.InitialStock),
                        new ColumnRule(MasterColumnCatalogue.Stock),

                        // Conditional Layman Formula Columns
                        new ColumnRule(MasterColumnCatalogue.Delta, false, false),
                        new ColumnRule(MasterColumnCatalogue.StockLayman, false, false),
                        
                        // Calculated WIP and Requirements
                        new ColumnRule(MasterColumnCatalogue.GrossRequirement, false, false),
                        new ColumnRule(MasterColumnCatalogue.ReviewWip),

                        // Conditional MOQ/CasePack Constraints
                        new ColumnRule(MasterColumnCatalogue.MoqWip, false, false),
                        new ColumnRule(MasterColumnCatalogue.MOQ, false, false),
                        new ColumnRule(MasterColumnCatalogue.CasePackWip, false, false),
                        new ColumnRule(MasterColumnCatalogue.CasePack, false, false)
                    }.AsReadOnly();

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