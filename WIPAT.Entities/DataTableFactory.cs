using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.BO;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Entities;
using WIPAT.Entities.Enum;
using WIPAT.Entities.ExcelTemplateDefinitions;

namespace WIPAT.Entities
{
    public class DataTableFactory
    {
        #region read excel to datatable

        public Response<DataTable> ReadExcelToDataTable(string filePath, string sheetName, List<string> columnsToRead = null)
        {
            var response = new Response<DataTable>();
            var table = new DataTable();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[sheetName];
                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        return new Response<DataTable> { Success = false, Message = $"Sheet {sheetName} not found or empty." };
                    }

                    var columnMapping = new Dictionary<string, int>();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        string header = worksheet.Cells[1, col].Text.Trim();
                        if (!string.IsNullOrEmpty(header))
                        {
                            columnMapping[header] = col;
                        }
                    }

                    var targetColumns = columnsToRead ?? columnMapping.Keys.ToList();

                    foreach (var colName in targetColumns)
                    {
                        if (columnMapping.ContainsKey(colName))
                        {
                            table.Columns.Add(colName);
                        }
                        else
                        {
                            return new Response<DataTable> { Success = false, Message = $"Column '{colName}' not found in file." };
                        }
                    }

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var newRow = table.NewRow();
                        bool rowHasData = false;

                        foreach (var colName in targetColumns)
                        {
                            int colIndex = columnMapping[colName];
                            string value = worksheet.Cells[row, colIndex].Text.Trim();
                            newRow[colName] = value;

                            if (!string.IsNullOrEmpty(value))
                            {
                                rowHasData = true;
                            }
                        }

                        if (rowHasData)
                        {
                            table.Rows.Add(newRow);
                        }
                    }

                    response.Success = true;
                    response.Data = table;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error reading Excel: {ex.Message}";
                return response;
            }
        }

        #endregion read excel to datatable

        #region Forecast Related DataTables

        // 1. Database Entity -> UI Table
        public Response<DataTable> BuildForecastDataTable(IEnumerable<ForecastDetail> details)
        {
            var response = new Response<DataTable>();
            try
            {
                // Generate base table using standard Forecast template
                DataTable table = GenerateEmptyTable(DataTableTemplateType.ForecastUIDataTable);

                foreach (var d in details)
                {
                    DataRow row = table.NewRow();
                    row[MasterColumnCatalogue.Casin.Name] = d.CASIN ?? (object)DBNull.Value;
                    row[MasterColumnCatalogue.RequestedQuantity.Name] = d.RequestedQuantity;
                    row[MasterColumnCatalogue.CommitmentPeriod.Name] = d.CommitmentPeriod;
                    row[MasterColumnCatalogue.PODate.Name] = d.PODate;
                    row[MasterColumnCatalogue.MonthString.Name] = d.Month;
                    row[MasterColumnCatalogue.Year.Name] = d.Year;
                    row[MasterColumnCatalogue.ItemStatus.Name] = (int)d.ItemStatus;
                    table.Rows.Add(row);
                }

                response.Success = true;
                response.Data = table;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error building forecast data table: {ex.Message}";
            }
            return response;
        }

        // 2. Raw Excel -> Processed UI Table
        public Response<DataTable> CreateProcessedForecastTable(DataTable rawTable, List<string> requiredColumns, Dictionary<string, ItemCatalogue> allDbItems)
        {
            var response = new Response<DataTable>();
            try
            {
                var processedTable = new DataTable();
                foreach (var col in requiredColumns) processedTable.Columns.Add(col);
                processedTable.Columns.Add("Month");
                processedTable.Columns.Add("Year");
                processedTable.Columns.Add("ItemStatus", typeof(int));

                foreach (DataRow row in rawTable.Rows)
                {
                    string casin = row[MasterColumnCatalogue.Casin.Name].ToString().Trim();
                    if (string.IsNullOrEmpty(casin)) continue;

                    var newRow = processedTable.NewRow();
                    foreach (var col in requiredColumns) newRow[col] = row[col];

                    if (DateTime.TryParse(row[MasterColumnCatalogue.PODate.Name].ToString(), out DateTime poDate))
                    {
                        newRow["Month"] = poDate.ToString("MMMM");
                        newRow["Year"] = poDate.Year.ToString();
                    }

                    newRow["ItemStatus"] = allDbItems.TryGetValue(casin, out var dbItem)
                        ? dbItem.ItemStatus
                        : (int)CatalogueItemStatus.Invalid;

                    processedTable.Rows.Add(newRow);
                }

                response.Success = true;
                response.Data = processedTable;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error processing forecast table: {ex.Message}";
            }
            return response;
        }

        // 3. UI Table -> DB Bulk Insert Table
        public Response<DataTable> CreateForecastBulkInsertTable(DataTable rawData, int masterId, Dictionary<string, (int Id, int ItemStatus, string Model)> catalogueLookup, int loggedinUserId)
        {
            var response = new Response<DataTable>();
            try
            {
                DataTable bulkTable = GenerateEmptyTable(DataTableTemplateType.ForecastBulkInsertTable);
                DateTime now = DateTime.Now;

                foreach (DataRow row in rawData.Rows)
                {
                    var casinValue = row[MasterColumnCatalogue.Casin.Name]?.ToString();

                    if (!catalogueLookup.TryGetValue(casinValue, out var catInfo))
                    {
                        response.Success = false;
                        response.Message = $"Import failed: The CASIN '{casinValue}' does not exist in the Item Catalogue. Please register it before uploading.";
                        return response;
                    }

                    var newRow = bulkTable.NewRow();
                    newRow[MasterColumnCatalogue.ItemCatalogueId.Name] = catInfo.Id;
                    newRow[MasterColumnCatalogue.ItemStatusInt.Name] = catInfo.ItemStatus;
                    newRow[MasterColumnCatalogue.Model.Name] = catInfo.Model;
                    newRow[MasterColumnCatalogue.Casin.Name] = casinValue;
                    newRow[MasterColumnCatalogue.RequestedQuantity.Name] = int.TryParse(row[MasterColumnCatalogue.RequestedQuantity.Name]?.ToString(), out int qty) ? qty : 0;
                    newRow[MasterColumnCatalogue.CommitmentPeriod.Name] = int.TryParse(row[MasterColumnCatalogue.CommitmentPeriod.Name]?.ToString(), out int cp) ? cp : 0;
                    newRow[MasterColumnCatalogue.PODate.Name] = DateTime.TryParse(row[MasterColumnCatalogue.PODate.Name]?.ToString(), out DateTime poDate) ? poDate : DateTime.MinValue;
                    newRow[MasterColumnCatalogue.MonthString.Name] = row[MasterColumnCatalogue.MonthString.Name]?.ToString();
                    newRow[MasterColumnCatalogue.Year.Name] = int.TryParse(row[MasterColumnCatalogue.Year.Name]?.ToString(), out int yr) ? yr : 0;
                    newRow[MasterColumnCatalogue.POForecastMasterId.Name] = masterId;
                    newRow[MasterColumnCatalogue.CreatedById.Name] = loggedinUserId;
                    newRow[MasterColumnCatalogue.CreatedAt.Name] = now;

                    bulkTable.Rows.Add(newRow);
                }

                response.Success = true;
                response.Data = bulkTable;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error creating bulk insert table: {ex.Message}";
            }
            return response;
        }
        #endregion

        #region Order Related DataTables

        // 1. Database Entity -> UI Table
        public Response<DataTable> BuildOrderUIDataTable(IEnumerable<ActualOrder> orders)
        {
            var response = new Response<DataTable>();
            try
            {
                DataTable table = GenerateEmptyTable(DataTableTemplateType.OrderUIDataTable);

                foreach (var o in orders)
                {
                    var catalogue = o.ItemCatalogue;

                    DataRow uiRow = table.NewRow();
                    uiRow[MasterColumnCatalogue.ItemCatalogueId.Name] = o.ItemCatalogueId;
                    uiRow[MasterColumnCatalogue.Casin.Name] = catalogue?.Casin;
                    uiRow[MasterColumnCatalogue.Quantity.Name] = o.Quantity;
                    uiRow[MasterColumnCatalogue.MonthString.Name] = o.Month;
                    uiRow[MasterColumnCatalogue.Year.Name] = o.Year;
                    uiRow[MasterColumnCatalogue.ItemStatusInt.Name] = catalogue != null ? catalogue.ItemStatus : (int)CatalogueItemStatus.Invalid;

                    table.Rows.Add(uiRow);
                }

                response.Success = true;
                response.Data = table;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error building order UI data table: {ex.Message}";
            }
            return response;
        }

        // 2. Raw Excel -> Processed UI Table
        public Response<DataTable> CreateProcessedOrderDataTable(DataTable rawData, Dictionary<string, ItemCatalogue> allDbItems, int loggedInUserId)
        {
            var response = new Response<DataTable>();
            try
            {
                var processedTable = GenerateEmptyTable(DataTableTemplateType.OrderBulkInsertTable);
                foreach (DataRow row in rawData.Rows)
                {
                    string casin = row[MasterColumnCatalogue.Casin.Name].ToString().Trim();
                    DataRow uiRow = processedTable.NewRow();

                    uiRow[MasterColumnCatalogue.Casin.Name] = casin;
                    uiRow[MasterColumnCatalogue.Quantity.Name] = row[MasterColumnCatalogue.Quantity.Name];

                    // --- 1. Month Validation ---
                    string rawMonth = row[MasterColumnCatalogue.MonthString.Name]?.ToString();

                    if (int.TryParse(rawMonth, out int monthNumber) && monthNumber >= 1 && monthNumber <= 12)
                    {
                        uiRow[MasterColumnCatalogue.MonthString.Name] = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(monthNumber);
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = $"Validation Error: Invalid month value '{rawMonth}' found for CASIN '{casin}'. Expected a number between 1 and 12.";
                        return response;
                    }

                    uiRow[MasterColumnCatalogue.Year.Name] = row[MasterColumnCatalogue.Year.Name];
                    uiRow[MasterColumnCatalogue.CreatedById.Name] = loggedInUserId;
                    uiRow[MasterColumnCatalogue.CreatedAt.Name] = DateTime.Now;

                    // --- 2. Item Status Validation ---
                    if (allDbItems.TryGetValue(casin, out var dbItem))
                    {
                        uiRow[MasterColumnCatalogue.ItemStatusInt.Name] = dbItem.ItemStatus;
                        uiRow[MasterColumnCatalogue.ItemCatalogueId.Name] = dbItem.Id;
                    }
                    else
                    {
                        // Return an error immediately if the CASIN is not found in the DB dictionary
                        response.Success = false;
                        response.Message = $"Validation Error: CASIN '{casin}' is invalid or was not found in the item catalogue.";
                        return response;
                    }

                    processedTable.Rows.Add(uiRow);
                }

                response.Success = true;
                response.Data = processedTable;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error processing order data table: {ex.Message}";
            }
            return response;
        }

        #endregion

        #region stock related datatable

        public Response<DataTable> CreateInvalidStockDataTable(List<string> selectedAsins, int createdById)
        {
            var response = new Response<DataTable>();
            try
            {
                DataTable dt = new DataTable();

                // 1. Build schema dynamically from the template definitions
                var template = FileTemplateFactory.GetDataTableTemplate(DataTableTemplateType.InvalidStockTable);
                foreach (var rule in template)
                {
                    dt.Columns.Add(rule.Definition.Name, rule.Definition.DataType.ToDotNetType());
                }

                // 2. Populate rows using the Master Catalogue references to avoid hardcoded strings
                foreach (var asin in selectedAsins)
                {
                    var row = dt.NewRow();

                    row[MasterColumnCatalogue.Casin.Name] = asin;
                    row[MasterColumnCatalogue.ItemCatalogueId.Name] = DBNull.Value;
                    row[MasterColumnCatalogue.OpeningStock.Name] = 0;
                    row[MasterColumnCatalogue.CreatedAt.Name] = DateTime.Now;
                    row[MasterColumnCatalogue.CreatedById.Name] = createdById;

                    dt.Rows.Add(row);
                }

                response.Success = true;
                response.Data = dt;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error creating invalid stock data table: {ex.Message}";
            }
            return response;
        }

        public async Task<Response<DataTable>> GetStockDataTableFromExcel(string filePath, string requiredWorkSheetName, int loggedInUserId, bool isUpdate = false)
        {
            var response = new Response<DataTable>();
            try
            {
                // 1. Get the template definitions and filter based on the IsUpdate flag
                var templateRules = FileTemplateFactory.GetDataTableTemplate(DataTableTemplateType.ValidStockTable).ToList();

                if (isUpdate)
                {
                    templateRules.RemoveAll(r => r.Definition.Name == MasterColumnCatalogue.CreatedAt.Name ||
                                                 r.Definition.Name == MasterColumnCatalogue.CreatedById.Name);
                }
                else
                {
                    templateRules.RemoveAll(r => r.Definition.Name == MasterColumnCatalogue.UpdatedAt.Name ||
                                                 r.Definition.Name == MasterColumnCatalogue.UpdatedById.Name);
                }

                #region 2. Add columns to DataTable
                DataTable dt = new DataTable();

                foreach (var rule in templateRules)
                {
                    Type columnType = rule.Definition.DataType.ToDotNetType();
                    dt.Columns.Add(rule.Definition.Name, columnType);
                }
                #endregion

                var fileInfo = new FileInfo(filePath);

                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        response.Success = false;
                        response.Message = "The workbook does not contain any worksheets.";
                        return response;
                    }

                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];

                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "Worksheet is not valid or has been disposed.";
                        response.Data = null;
                        return response;
                    }

                    // 3. Dynamically map Excel columns by reading the header row (Row 1)
                    Dictionary<string, int> excelHeaderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                    {
                        string headerText = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(headerText))
                        {
                            excelHeaderMap[headerText] = col;
                        }
                    }

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        DataRow dr = dt.NewRow();

                        foreach (var rule in templateRules)
                        {
                            string colName = rule.Definition.Name;

                            // 4. Handle System / Audit fields
                            if (colName == MasterColumnCatalogue.CreatedAt.Name || colName == MasterColumnCatalogue.UpdatedAt.Name)
                            {
                                dr[colName] = DateTime.Now;
                            }
                            else if (colName == MasterColumnCatalogue.CreatedById.Name || colName == MasterColumnCatalogue.UpdatedById.Name)
                            {
                                dr[colName] = loggedInUserId;
                            }
                            else if (colName == MasterColumnCatalogue.ItemCatalogueId.Name)
                            {
                                dr[colName] = 0;
                            }
                            else
                            {
                                // 5. Retrieve value dynamically based on mapped Excel column index
                                string cellValue = string.Empty;
                                if (excelHeaderMap.TryGetValue(colName, out int colIndex))
                                {
                                    cellValue = worksheet.Cells[row, colIndex].Text;
                                }

                                var drRes = MapColumnValues(colName, cellValue, dr, row, loggedInUserId);
                                if (!drRes.Success)
                                {
                                    response.Success = drRes.Success;
                                    response.Message = drRes.Message;
                                    return response;
                                }

                                dr = drRes.Data;
                            }
                        }

                        dt.Rows.Add(dr);
                    }

                    response.Success = true;
                    response.Message = "Initial stock data loaded successfully.";
                    response.Data = dt;
                }

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error reading initial stock Excel file: {ex.Message}";
                response.Data = null;
            }

            return response;
        }
        #endregion stock related datatable

        #region itemcatalogue related datatable

        public Response<DataTable> CreateInvalidItemDataTable(List<string> selectedAsins, int createdById)
        {
            var response = new Response<DataTable>();
            try
            {
                DataTable dt = new DataTable();

                // 1. Build schema dynamically from the template definitions
                var template = FileTemplateFactory.GetDataTableTemplate(DataTableTemplateType.InvalidItemTable);
                foreach (var rule in template)
                {
                    dt.Columns.Add(rule.Definition.Name, rule.Definition.DataType.ToDotNetType());
                }

                // 2. Populate rows using the Master Catalogue references to avoid hardcoded strings
                foreach (var asin in selectedAsins)
                {
                    var row = dt.NewRow();

                    row[MasterColumnCatalogue.Casin.Name] = asin;
                    row[MasterColumnCatalogue.Model.Name] = DBNull.Value;
                    row[MasterColumnCatalogue.Description.Name] = DBNull.Value;
                    row[MasterColumnCatalogue.ColorName.Name] = DBNull.Value;
                    row[MasterColumnCatalogue.Size.Name] = DBNull.Value;
                    row[MasterColumnCatalogue.PCPK.Name] = DBNull.Value;
                    row[MasterColumnCatalogue.CasePackQty.Name] = 0;
                    row[MasterColumnCatalogue.CreatedAt.Name] = DateTime.Now;
                    row[MasterColumnCatalogue.CreatedById.Name] = createdById;
                    row[MasterColumnCatalogue.Notes.Name] = DBNull.Value;
                    row[MasterColumnCatalogue.ItemStatusInt.Name] = (int)CatalogueItemStatus.Invalid;

                    dt.Rows.Add(row);
                }

                response.Success = true;
                response.Data = dt;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error creating invalid item data table: {ex.Message}";
            }
            return response;
        }

        public async Task<Response<DataTable>> GetItemCataloguesDataTableFromExcel(string filePath, string requiredWorkSheetName, int loggedInUserId, bool isUpdate = false)
        {
            var response = new Response<DataTable>();

            try
            {
                #region 1. Get Template Rule and Build  DataTable Schema

                // Get exact rules for DataTable schema based on Insert vs Update
                var templateRules = FileTemplateFactory.GetDataTableTemplate(DataTableTemplateType.ItemCatalogueDataTable, isUpdate);

                //Build Dynamic DataTable Schema
                DataTable dt = new DataTable();

                foreach (var rule in templateRules)
                {
                    dt.Columns.Add(
                        rule.Definition.Name,
                        rule.Definition.DataType.ToDotNetType());
                }

                #endregion

                var fileInfo = new FileInfo(filePath);

                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
                {
                    #region 2. Worksheet Validation

                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        response.Success = false;
                        response.Message = "The workbook does not contain any worksheets.";
                        return response;
                    }

                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];

                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "Worksheet is not valid, disposed, or entirely empty.";
                        return response;
                    }

                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    #endregion

                    #region 3. Build Excel Header Column Mapping

                    Dictionary<string, int> actualColumnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    for (int col = 1; col <= colCount; col++)
                    {
                        string headerValue = worksheet.Cells[1, col].Value?.ToString();

                        if (!string.IsNullOrWhiteSpace(headerValue))
                        {
                            string strippedHeader = headerValue.Replace(" ", "").ToLower();
                            actualColumnIndexes[strippedHeader] = col;
                        }
                    }

                    #endregion

                    #region 4. Read and Map Excel Rows to DataTable

                    for (int row = 2; row <= rowCount; row++)
                    {
                        DataRow dr = dt.NewRow();

                        foreach (var rule in templateRules)
                        {
                            string colName = rule.Definition.Name;
                            string searchKey = colName.Replace(" ", "").ToLower();

                            #region 5.1 Audit Columns Handling

                            if (colName == MasterColumnCatalogue.CreatedAt.Name || colName == MasterColumnCatalogue.UpdatedAt.Name)
                            {
                                dr[colName] = DateTime.Now;
                                continue;
                            }

                            if (colName == MasterColumnCatalogue.CreatedById.Name || colName == MasterColumnCatalogue.UpdatedById.Name)
                            {
                                dr[colName] = loggedInUserId;
                                continue;
                            }

                            #endregion

                            #region 5.2 Column Existence Check

                            if (!actualColumnIndexes.ContainsKey(searchKey))
                            {
                                dr[colName] = DBNull.Value;
                                continue;
                            }

                            int colIndex = actualColumnIndexes[searchKey];
                            string cellValue = worksheet.Cells[row, colIndex].Value?.ToString()?.Trim();

                            #endregion

                            #region 5.3 & 5.4 Data Mapping (Enum & Standard Types)

                            try
                            {
                                if (string.IsNullOrWhiteSpace(cellValue))
                                {
                                    dr[colName] = DBNull.Value;
                                }
                                else
                                {
                                    if (colName.Equals(MasterColumnCatalogue.ItemStatus.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (System.Enum.TryParse<CatalogueItemStatus>(cellValue, true, out var status))
                                        {
                                            dr[colName] = (int)status;
                                        }
                                        else
                                        {
                                            dr[colName] = 0;
                                        }
                                    }
                                    else
                                    {
                                        Type targetType = dt.Columns[colName].DataType;
                                        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                                        dr[colName] = Convert.ChangeType(cellValue, underlyingType);
                                    }
                                }
                            }
                            catch (FormatException)
                            {
                                response.Success = false;
                                response.Message = $"Upload Failed: Row {row} contains invalid data format in '{colName}'.";
                                return response;
                            }
                            catch (Exception ex)
                            {
                                response.Success = false;
                                response.Message = $"Upload Failed: Error at Row {row}, Column '{colName}'. Details: {ex.Message}";
                                return response;
                            }

                            #endregion
                        }

                        dt.Rows.Add(dr);
                    }

                    #endregion

                    #region 6. Success Response

                    response.Success = true;
                    response.Message = "Items Catalogue Data read successfully.";
                    response.Data = dt;
                    return response;

                    #endregion
                }
            }
            catch (Exception ex)
            {
                #region 7. Global Exception Handling

                response.Success = false;
                response.Message = $"Error reading Items Catalogue Data from Excel file: {ex.Message}";
                response.Data = null;
                return response;

                #endregion
            }
        }
        #endregion itemcatalogue related datatable

        #region error grid table

        public Response<DataTable> CreateProblemItemsDataTable(List<string> missingItems, List<string> deactivatedItems, string filePath)
        {
            var response = new Response<DataTable>();
            try
            {
                DataTable dt = new DataTable("ProblemItems");

                // 1. Define Columns Dynamically using the Template Factory
                var columnRules = FileTemplateFactory.GetDataTableTemplate(DataTableTemplateType.ProblemItemsTable);
                foreach (var rule in columnRules)
                {
                    dt.Columns.Add(rule.Definition.Name, rule.Definition.DataType.ToDotNetType());
                }

                string fileName = Path.GetFileName(filePath);

                // 2. Insert rows sequentially (relies on the column order in GetDataTableTemplate matching these parameters)
                if (deactivatedItems != null)
                {
                    foreach (var casin in deactivatedItems)
                    {
                        dt.Rows.Add(casin, fileName, "Deactivated/Invalid");
                    }
                }

                if (missingItems != null)
                {
                    foreach (var casin in missingItems)
                    {
                        dt.Rows.Add(casin, fileName, "Missing");
                    }
                }

                response.Success = true;
                response.Data = dt;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error creating problem items data table: {ex.Message}";
            }
            return response;
        }

        #endregion error grid table

        #region helpers 

        private Response<DataRow> MapColumnValues(string column, string cellValue, DataRow dr, int row, int loggedInUserId)
        {
            var response = new Response<DataRow>();

            try
            {
                bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);

                if (column == AllColumnNames.CasePackQty)
                {
                    if (!IsEmpty(cellValue))
                    {
                        var parseRes = TryParseIntegerColumn(column, cellValue, row, dr);
                        if (!parseRes.Success)
                        {
                            response.Success = parseRes.Success;
                            response.Message = parseRes.Message;
                            return response;
                        }
                    }

                }
                else if (column == AllColumnNames.PCPK || column == AllColumnNames.OpeningStock)
                {
                    var parseRes = TryParseIntegerColumn(column, cellValue, row, dr);
                    if (!parseRes.Success)
                    {
                        response.Success = parseRes.Success;
                        response.Message = parseRes.Message;
                        return response;
                    }
                }
                else if (column == AllColumnNames.CreatedById)
                {
                    dr[column] = loggedInUserId;
                }
                else if (column == AllColumnNames.CreatedAt)
                {
                    dr[column] = DateTime.Now;
                }
                else if (column == AllColumnNames.CAsin)
                {
                    dr[column] = cellValue;
                }
                else if (column == AllColumnNames.ItemCatalogueId)
                {
                    dr[column] = 0;
                }
                else
                {
                    dr[column] = cellValue;
                }

                response.Success = true;
                response.Message = "Column values mapped successfully.";
                response.Data = dr;
            }
            catch (Exception ex)
            {
                response.Message = $"An error occurred while mapping column '{column}' for row {row}: {ex.Message}";
                response.Success = false;
            }

            return response;
        }

        private Response<DataRow> TryParseIntegerColumn(string column, string cellValue, int row, DataRow dr)
        {
            var response = new Response<DataRow>();

            try
            {
                if (int.TryParse(cellValue, out int parsedValue))
                {
                    dr[column] = parsedValue;
                    response.Success = true;
                    response.Message = "Success";
                    response.Data = dr;
                    return response;
                }
                else
                {
                    response.Success = false;
                    response.Message = $"Invalid {column} value at row {row}. Could not parse '{cellValue}' as an integer.";
                    response.Data = null;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An unexpected error occurred while processing the {column} value at row {row}: {ex.Message}";
                response.Data = null;
                return response;
            }
        }

        private DataTable GenerateEmptyTable(DataTableTemplateType templateType, bool isUpdate = false)
        {
            var dt = new DataTable();
            var rules = FileTemplateFactory.GetDataTableTemplate(templateType, isUpdate);

            foreach (var rule in rules)
            {
               
                dt.Columns.Add(rule.Definition.Name, rule.Definition.DataType.ToDotNetType());
            }
            return dt;
        }

        #endregion helpers   
    }
}