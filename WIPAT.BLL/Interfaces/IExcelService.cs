using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.BLL.Interfaces
{
    public interface IExcelService
    {
       
        #region  validate excel file
        Task<Response<string>> ValidateItemCatalogueExcelFile(string filePath);
        Task<Response<bool>> ValidateExcelFile(string filePath, string fileType, string requiredWorkSheetName, List<string> requiredExcelColumns,
            string requiredMonth = null,
            string requiredYear = null
            );
        Response<bool> ValidateColumns(string filePath, string sheetName, List<string> requiredColumns);
        #endregion  validate excel file

        #region Read Excel
        Response<(string Month, string Year)> PeekForecastProjectionDate(string filePath, string sheetName);

        Response<DataTable> ReadExcelToDataTable(string filePath, string sheetName, List<string> columnsToRead = null);
        //Task<Response<List<DataTable>>> ReadCatalogDataTableFromExcel(string filePath);
        Task<Response<List<DataTable>>> ReadCatalogDataTableFromExcel(string filePath, bool isUpdate = false);
        Response<List<WipDetail>> ReadEditWipExcel(string filePath);
        #endregion read excel

        #region Get Datatable
        Task<Response<DataTable>> GetItemCataloguesDataTableFromExcel(string filePath, string requiredWorkSheetName, bool isUpdate = false);
        Task<Response<DataTable>> GetStockDataTableFromExcel(string filePath, string requiredWorkSheetName, bool isUpdate = false);
        #endregion Get Datatable

        #region Export to Excel
        void ExportWipDataToExcel<T>(List<T> data, string filePath, string sheetName);

        Response<string> ExportGridToExcel(DataGridView grid, string filePath, string sheetName);

        #endregion Export to Excel

        #region helpers
        string GetEnumValue(Enum value);

        #endregion helpers
    }
}