using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.BLL.Strategies
{
    public interface IWipCalculationStrategy
    {
        int Calculate(WipCalculationContext context);
        string StrategyName { get; }
    }
}