using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Treasury.Calculators
{
    public class DepositTimeValuator : IDepositTimeValuator, IScopedDependency
    {
        public string ValuationLabel => "deposit-time token price";

        // Uses Stake.TokenPrice at deposit time — not a live market price.
        // Returns false when price is missing/zero so callers can flag incomplete % math.
        public bool TryValueUsd(decimal amount, decimal tokenPrice, out decimal valueUsd)
        {
            if (tokenPrice > 0)
            {
                valueUsd = amount * tokenPrice;
                return true;
            }

            valueUsd = 0m;
            return false;
        }
    }
}
