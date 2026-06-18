using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using WIPAT.Entities.ExcelTemplateDefinitions;

namespace WIPAT.Entities
{
    public class DataTableFactory
    {

        public  DataTable CreateProblemItemsDataTable(List<string> missingItems, List<string> deactivatedItems, string filePath, string month, string year)
        {
            DataTable dt = new DataTable("ProblemItems");

            dt.Columns.Add("Casin", typeof(string));
            dt.Columns.Add("Month", typeof(string));
            dt.Columns.Add("Year", typeof(string));
            dt.Columns.Add("FileName", typeof(string));
            dt.Columns.Add("Reason", typeof(string));

            string fileName = Path.GetFileName(filePath);

            if (deactivatedItems != null)
            {
                foreach (var casin in deactivatedItems)
                {
                    dt.Rows.Add(casin, month, year, fileName, "Deactivated/Invalid");
                }
            }

            if (missingItems != null)
            {
                foreach (var casin in missingItems)
                {
                    dt.Rows.Add(casin, month, year, fileName, "Missing");
                }
            }

            return dt;
        }

        public (DataTable Table, string ErrorMessage) CreateForecastBulkInsertTable(DataTable rawData, int masterId, Dictionary<string, (int Id, int ItemStatus)> catalogueLookup)
        {
            DataTable bulkTable = new DataTable();

            // 1. Define Columns
            bulkTable.Columns.Add("ItemCatalogueId", typeof(int));
            bulkTable.Columns.Add("CASIN", typeof(string));
            bulkTable.Columns.Add("RequestedQuantity", typeof(int));
            bulkTable.Columns.Add("CommitmentPeriod", typeof(string));
            bulkTable.Columns.Add("PODate", typeof(DateTime));
            bulkTable.Columns.Add("Month", typeof(string));
            bulkTable.Columns.Add("Year", typeof(string));
            bulkTable.Columns.Add("POForecastMasterId", typeof(int));
            bulkTable.Columns.Add("ItemStatus", typeof(int));

            // 2. Populate Rows
            foreach (DataRow row in rawData.Rows)
            {
                var casinValue = row["CASIN"].ToString();
                var newRow = bulkTable.NewRow();

                if (catalogueLookup.TryGetValue(casinValue, out var catInfo))
                {
                    newRow["ItemCatalogueId"] = catInfo.Id;
                    newRow["ItemStatus"] = catInfo.ItemStatus;
                }
                else
                {
                    // Break out immediately if a CASIN is invalid
                    return (null, $"Import failed: The CASIN '{casinValue}' does not exist in the Item Catalogue. Please register it before uploading.");
                }

                newRow["CASIN"] = casinValue;
                newRow["RequestedQuantity"] = int.TryParse(row["RequestedQuantity"].ToString(), out int qty) ? qty : 0;
                newRow["CommitmentPeriod"] = row["CommitmentPeriod"].ToString();
                newRow["PODate"] = DateTime.TryParse(row["PO Date"].ToString(), out DateTime poDate) ? poDate : DateTime.MinValue;
                newRow["Month"] = row["Month"].ToString();
                newRow["Year"] = row["Year"].ToString();
                newRow["POForecastMasterId"] = masterId;

                bulkTable.Rows.Add(newRow);
            }

            return (bulkTable, null); // Return the fully populated table
        }

        public DataTable CreateProcessedForecastTable(DataTable rawTable,List<string> requiredColumns,Dictionary<string, ItemCatalogue> allDbItems)
        {
            var processedTable = new DataTable();

            // 1. Setup Columns
            foreach (var col in requiredColumns) processedTable.Columns.Add(col);
            processedTable.Columns.Add("Month");
            processedTable.Columns.Add("Year");
            processedTable.Columns.Add("ItemStatus", typeof(int));

            // 2. Populate Rows
            foreach (DataRow row in rawTable.Rows)
            {
                string casin = row[MasterColumnCatalogue.Casin.Name].ToString().Trim();
                if (string.IsNullOrEmpty(casin)) continue;

                var newRow = processedTable.NewRow();

                // Copy required columns
                foreach (var col in requiredColumns) newRow[col] = row[col];

                // Handle Dates
                if (DateTime.TryParse(row[MasterColumnCatalogue.PODate.Name].ToString(), out DateTime poDate))
                {
                    newRow["Month"] = poDate.ToString("MMMM");
                    newRow["Year"] = poDate.Year.ToString();
                }
                else
                {
                    newRow["Month"] = "Invalid Date";
                    newRow["Year"] = "";
                }

                // Handle Item Status
                if (allDbItems.TryGetValue(casin, out var dbItem))
                {
                    newRow["ItemStatus"] = dbItem.ItemStatus;
                }
                else
                {
                    newRow["ItemStatus"] = (int)CatalogueItemStatus.Invalid;
                }

                processedTable.Rows.Add(newRow);
            }

            return processedTable;
        }

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

        public DataTable BuildForecastDataTable(IEnumerable<ForecastDetail> details)
        {
            DataTable table = new DataTable();

            // 1. Retrieve the ForecastFile template configuration
            var templateRules = FileTemplateFactory.GetImportTemplate(ImportExcelFileType.ForecastFile);

            var excludedColumns = new[] { MasterColumnCatalogue.ProjectionMonth.Name, MasterColumnCatalogue.ProjectionYear.Name };

            var allowedColumns = templateRules.Where(r => !excludedColumns.Contains(r.Definition.Name));

            // 2. Dynamically build columns based on the template
            foreach (var rule in allowedColumns)
            {
                table.Columns.Add(rule.Definition.Name, rule.Definition.DataType.ToDotNetType());
            }

            // 3. Populate Rows safely
            foreach (var d in details)
            {
                DataRow row = table.NewRow();

                // Map values utilizing the MasterColumnCatalogue
                row[MasterColumnCatalogue.Casin.Name] = d.CASIN ?? (object)DBNull.Value;
                row[MasterColumnCatalogue.RequestedQuantity.Name] = d.RequestedQuantity;
                row[MasterColumnCatalogue.CommitmentPeriod.Name] = d.CommitmentPeriod;
                row[MasterColumnCatalogue.PODate.Name] = d.PODate;
                row[MasterColumnCatalogue.MonthString.Name] = d.Month;
                row[MasterColumnCatalogue.Year.Name] = d.Year;
                row[MasterColumnCatalogue.ItemStatus.Name] = ((CatalogueItemStatus)d.ItemStatus).ToString();

                table.Rows.Add(row);
            }

            return table;
        }


        #region Helper Methods for Invalid Casins
        public DataTable CreateInvalidItemDataTable(List<string> selectedAsins, int createdById)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Casin", typeof(string));
            dt.Columns.Add("Model", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("ColorName", typeof(string));
            dt.Columns.Add("Size", typeof(string));
            dt.Columns.Add("PCPK", typeof(string));
            dt.Columns.Add("CasePackQty", typeof(int));
            dt.Columns.Add("CreatedAt", typeof(DateTime));
            dt.Columns.Add("CreatedById", typeof(int));
            dt.Columns.Add("Notes", typeof(string));

            dt.Columns.Add("ItemStatus", typeof(int));

            foreach (var asin in selectedAsins)
            {
                var row = dt.NewRow();
                row["Casin"] = asin;
                row["Model"] = DBNull.Value;
                row["Description"] = DBNull.Value;
                row["ColorName"] = DBNull.Value;
                row["Size"] = DBNull.Value;
                row["PCPK"] = DBNull.Value;
                row["CasePackQty"] = 0;
                row["CreatedAt"] = DateTime.UtcNow;
                row["CreatedById"] = createdById;
                row["Notes"] = DBNull.Value;

                // Set status to Invalid (2)
                row["ItemStatus"] = 2;

                dt.Rows.Add(row);
            }
            return dt;
        }

        public DataTable CreateInvalidStockDataTable(List<string> selectedAsins, int createdById)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Casin", typeof(string));
            dt.Columns.Add("ItemCatalogueId", typeof(int));
            dt.Columns.Add("OpeningStock", typeof(int));
            dt.Columns.Add("CreatedAt", typeof(DateTime));
            dt.Columns.Add("CreatedById", typeof(int));

            foreach (var asin in selectedAsins)
            {
                var row = dt.NewRow();
                row["Casin"] = asin;
                row["ItemCatalogueId"] = DBNull.Value;
                row["OpeningStock"] = 0;
                row["CreatedAt"] = DateTime.UtcNow;
                row["CreatedById"] = createdById;

                dt.Rows.Add(row);
            }
            return dt;
        }
        #endregion

    }
}
