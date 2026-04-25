using System.Linq;

namespace WIPAT.BLL.Strategies
{
    public class SystemStrategy : IWipCalculationStrategy
    {
        public string StrategyName => "System";

        public int Calculate(WipCalculationContext context)
        {
            // Logic moved from CalculateLowCapacityWip
            int targetMonthDemand = context.ForecastData.FirstOrDefault(f => f.CommitmentPeriod == context.TargetPeriod)?.RequestedQuantity ?? 0;
            int nextMonthDemand = context.ForecastData.FirstOrDefault(f => f.CommitmentPeriod == (context.TargetPeriod + 1))?.RequestedQuantity ?? 0;

            if (context.CurrentPeriod == context.TargetPeriod)
            {
                return targetMonthDemand > context.InitialStock
                    ? targetMonthDemand - context.InitialStock
                    : 0;
            }
            else
            {
                // Note: The logic implied initialStock became 0 after the first hit, 
                // but strictly following your code logic for non-target months:
                return nextMonthDemand > context.InitialStock
                    ? nextMonthDemand - context.InitialStock
                    : (context.InitialStock == 0 ? context.Demand : 0);
            }
        }
    }
}