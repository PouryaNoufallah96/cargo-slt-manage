namespace SLT.Services._Treasury.DTOs.Results
{
    public class TreasuryOverviewResult
    {
        public DateTime ReportAsOfMoment { get; set; }

        // Always LUSD + GOLDGR, even when zero.
        public List<TreasuryAssetObligationSummary> PerAsset { get; set; } = [];

        public TreasuryActiveContractsSummary ActiveContracts { get; set; } = new TreasuryActiveContractsSummary();

        // Distinct wallets with at least one open stake (case-insensitive).
        public int ActiveUsers { get; set; }

        // Open maturities within the limit window, soonest first.
        public List<TreasuryUpcomingMaturity> NearestMaturities { get; set; } = [];

        public string SummaryText { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public class TreasuryAssetObligationSummary
    {
        public string Asset { get; set; }

        public decimal OpenPrincipal { get; set; }

        public decimal RemainingProfitOwed { get; set; }

        // OpenPrincipal + RemainingProfitOwed.
        public decimal OpenObligation { get; set; }
    }

    public class TreasuryActiveContractsSummary
    {
        public int WithinTerm { get; set; }
        public int MaturedUnredeemed { get; set; }
        public int Total { get; set; }
    }

    public class TreasuryUpcomingMaturity
    {
        public string StakeReference { get; set; }
        public string Asset { get; set; }
        public string WalletAddress { get; set; }
        public DateTime EndMoment { get; set; }
        public decimal RemainingPrincipal { get; set; }
    }
}
