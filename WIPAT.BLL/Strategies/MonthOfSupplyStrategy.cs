using System.Linq;

namespace WIPAT.BLL.Strategies
{
    public class MonthOfSupplyStrategy : IWipCalculationStrategy
    {
        public string StrategyName => "MonthOfSupply";

        public int Calculate(WipCalculationContext context)
        {
            // Logic moved from CalculateHighCapacityWip
            int nextPeriodDemand = context.ForecastData
                .FirstOrDefault(f => f.CommitmentPeriod == context.TargetPeriod + 1)?.RequestedQuantity ?? 0;

            int value = (context.Demand + nextPeriodDemand) - context.InitialStock;

            if (value > 0)
            {
                if (context.CurrentPeriod == context.TargetPeriod) return value;
                if (context.CurrentPeriod == context.TargetPeriod + 1) return 0;
                return context.Demand;
            }
            else
            {
                if (context.CurrentPeriod == context.TargetPeriod || context.CurrentPeriod == context.TargetPeriod + 1) return 0;
                return context.Demand;
            }
        }
    }
}