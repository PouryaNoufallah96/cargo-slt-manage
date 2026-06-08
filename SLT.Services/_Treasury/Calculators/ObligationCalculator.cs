using SLT.Domain.Collections;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Treasury.Calculators
{
    public class ObligationCalculator : IObligationCalculator, IScopedDependency
    {
        public StakeObligation Calculate(Stake stake)
        {
            // expectedFullTermProfit = EachMonthProfit * MonthDuration
            // paidProfitTotal        = TotalProfitWithdrawn + TotalProfitOfAmountWithdrawn
            // remainingProfitOwed    = max(0, expected - paid); < 0 => ProfitAnomaly, clamp to 0
            // openObligation         = TokenAmount + remainingProfitOwed
            var expectedFullTermProfit = stake.EachMonthProfit * stake.MonthDuration;
            var paidProfitTotal = stake.TotalProfitWithdrawn + stake.TotalProfitOfAmountWithdrawn;

            var rawRemainingProfit = expectedFullTermProfit - paidProfitTotal;
            var profitAnomaly = rawRemainingProfit < 0;
            var remainingProfitOwed = profitAnomaly ? 0 : rawRemainingProfit;

            var remainingPrincipalOwed = stake.TokenAmount;
            var openObligation = remainingPrincipalOwed + remainingProfitOwed;

            return new StakeObligation
            {
                RemainingPrincipalOwed = remainingPrincipalOwed,
                ExpectedFullTermProfit = expectedFullTermProfit,
                PaidProfitTotal = paidProfitTotal,
                RemainingProfitOwed = remainingProfitOwed,
                OpenObligation = openObligation,
                ProfitAnomaly = profitAnomaly
            };
        }
    }
}
