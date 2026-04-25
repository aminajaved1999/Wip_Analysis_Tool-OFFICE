using System;

namespace WIPAT.BLL.Strategies
{
    public class PercentageStrategy : IWipCalculationStrategy
    {
        public string StrategyName => "Percentage";

        public int Calculate(WipCalculationContext context)
        {
            if (!context.Percentage.HasValue) return 0;

            // Logic moved from CalculateMediumCapacityWip
            double requiredQty = context.Demand + ((context.Percentage.Value / 100.0) * context.Demand);
            double shortfall = requiredQty - context.InitialStock;

            var roundedShortfall = (shortfall - Math.Floor(shortfall) < 0.5)
                                    ? Math.Floor(shortfall)
                                    : Math.Ceiling(shortfall);

            return (int)roundedShortfall > 0 ? (int)roundedShortfall : 0;
        }
    }
}