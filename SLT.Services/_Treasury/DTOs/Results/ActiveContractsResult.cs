using SLT.Services._Treasury.Calculators;

namespace SLT.Services._Treasury.DTOs.Results
{
    public class ActiveContractsResult
    {
        public List<ActiveContractRow> Data { get; set; } = [];

        public long TotalCount { get; set; }

        public int PageCount { get; set; }

        public string SummaryText { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public class ActiveContractRow
    {
        public string WalletAddress { get; set; }
        public string StakeReference { get; set; }
        public string Asset { get; set; }

        // Remaining principal (= TokenAmount), not original StartAmount.
        public decimal Principal { get; set; }

        public int PlanType { get; set; }
        public DateTime StartMoment { get; set; }
        public DateTime EndMoment { get; set; }

        public decimal ProfitReceived { get; set; }

        public decimal RemainingProfitOwed { get; set; }

        // Derived from State + EndMoment — not the raw Stake.State.
        public TreasuryReportingStatus Status { get; set; }
    }
}
