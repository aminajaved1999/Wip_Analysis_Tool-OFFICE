using System.Collections.Generic;
using System.Linq;

namespace WIPAT.BLL.Strategies
{
    public interface IWipStrategyFactory
    {
        IWipCalculationStrategy GetStrategy(string strategyName);
    }

    public class WipStrategyFactory : IWipStrategyFactory
    {
        private readonly IEnumerable<IWipCalculationStrategy> _strategies;

        public WipStrategyFactory(IEnumerable<IWipCalculationStrategy> strategies)
        {
            _strategies = strategies;
        }

        public IWipCalculationStrategy GetStrategy(string strategyName)
        {
            return _strategies.FirstOrDefault(s => s.StrategyName == strategyName);
        }
    }
}