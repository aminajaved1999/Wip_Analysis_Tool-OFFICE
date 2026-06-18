using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
