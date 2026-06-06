using SLT.Domain.Collections;

namespace SLT.Services._Treasury.Calculators
{
    public interface IObligationCalculator
    {
        StakeObligation Calculate(Stake stake);
    }

    public class StakeObligation
    {
        // RemainingPrincipalOwed = TokenAmount. Profit fields are full-term contractual, not accrued-to-date.
        public decimal RemainingPrincipalOwed { get; set; }
        public decimal ExpectedFullTermProfit { get; set; }
        public decimal PaidProfitTotal { get; set; }
        public decimal RemainingProfitOwed { get; set; }
        public decimal OpenObligation { get; set; }
        public bool ProfitAnomaly { get; set; }
    }
}
