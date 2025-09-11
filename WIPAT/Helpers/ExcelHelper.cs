using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.Helpers
{
    public class ExcelHelper
    {




        /////////////////////////
        public static int GetCommitmentPeriod()
        {
            string setting = ConfigurationManager.AppSettings["CommitmentPeriod"];

            if (string.IsNullOrEmpty(setting))
            {
                throw new ConfigurationErrorsException("AppSetting 'CommitmentPeriod' is missing or empty.");
            }

            if (!int.TryParse(setting, out int parsedValue))
            {
                throw new ConfigurationErrorsException($"AppSetting 'CommitmentPeriod' is not a valid integer: '{setting}'.");
            }

            return parsedValue;
        }
    }
}
