using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities.Enum
{
    public enum ForecastExcelColumns
    {
        [Description("C-ASIN")]
        CASIN,

        [Description("Requested Quantity")]
        Requested_Quantity,

        [Description("Commitment period")]
        Commitment_Period,

        [Description("PO date")]
        PO_Date,

        ProjectionMonth,
        ProjectionYear
    }
}
