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
        [Description("CASIN")]
        CASIN,

        [Description("RequestedQuantity")]
        Requested_Quantity,

        [Description("CommitmentPeriod")]
        Commitment_Period,

        [Description("PO Date")]
        PO_Date,

        ProjectionMonth,
        ProjectionYear
    }
}
