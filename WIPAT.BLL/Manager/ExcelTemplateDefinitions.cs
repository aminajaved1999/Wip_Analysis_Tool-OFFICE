using System;
using System.Collections.Generic;

namespace WIPAT.BLL.Manager.ExcelTemplateDefinitions
{
    #region 1. ENUMS (System Vocabulary)

    public enum ExcelFileType
    {
        AddNewItemsToCatalogue,
        UpdateExistingCatalogue,
        ForecastFile,
        OrderFile
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
    /// Represents how a column behaves inside a specific Excel template.
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

        // Forecast Specific
        public static readonly ColumnDefinition RequestedQuantity = new ColumnDefinition("RequestedQuantity", ExcelDataType.Int);
        public static readonly ColumnDefinition CommitmentPeriod = new ColumnDefinition("CommitmentPeriod", ExcelDataType.Int);
        public static readonly ColumnDefinition PODate = new ColumnDefinition("PO date", ExcelDataType.DateTime);
        public static readonly ColumnDefinition ProjectionMonth = new ColumnDefinition("ProjectionMonth", ExcelDataType.Int);
        public static readonly ColumnDefinition ProjectionYear = new ColumnDefinition("ProjectionYear", ExcelDataType.Int);

        // Order Specific
        public static readonly ColumnDefinition Quantity = new ColumnDefinition("Quantity", ExcelDataType.Int);
        public static readonly ColumnDefinition Month = new ColumnDefinition("Month", ExcelDataType.Int);
        public static readonly ColumnDefinition Year = new ColumnDefinition("Year", ExcelDataType.Int);
    }

    #endregion

    #region 4. TEMPLATE FACTORY

    /// <summary>
    /// Defines WHICH columns belong to WHICH file, and how strict they are.
    /// </summary>
    public static class FileTemplateFactory
    {
        public static IReadOnlyList<ColumnRule> GetTemplate(ExcelFileType fileType)
        {
            switch (fileType)
            {
                case ExcelFileType.AddNewItemsToCatalogue:
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

                case ExcelFileType.UpdateExistingCatalogue:
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

                case ExcelFileType.ForecastFile:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.RequestedQuantity),
                        new ColumnRule(MasterColumnCatalogue.CommitmentPeriod),
                        new ColumnRule(MasterColumnCatalogue.PODate),
                        new ColumnRule(MasterColumnCatalogue.ProjectionMonth),
                        new ColumnRule(MasterColumnCatalogue.ProjectionYear)
                    }.AsReadOnly();

                case ExcelFileType.OrderFile:
                    return new List<ColumnRule>
                    {
                        new ColumnRule(MasterColumnCatalogue.Casin),
                        new ColumnRule(MasterColumnCatalogue.Quantity),
                        new ColumnRule(MasterColumnCatalogue.Month),
                        new ColumnRule(MasterColumnCatalogue.Year)
                    }.AsReadOnly();

                default:
                    throw new ArgumentException("No template configured for: " + fileType);
            }
        }
    }

    #endregion
}