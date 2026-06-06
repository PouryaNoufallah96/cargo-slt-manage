namespace SLT.Services._Treasury.DTOs.Results
{
    public class WalletAnalysisResult
    {
        public string Wallet { get; set; }

        public DateTime ReportAsOfMoment { get; set; }

        public int RegisteredContractCount { get; set; }

        public List<WalletAssetEntered> EnteredByAsset { get; set; } = [];

        public int WithinTermCount { get; set; }
        public int MaturedUnredeemedCount { get; set; }

        public int ActiveOpenCount { get; set; }

        public int FinishedCount { get; set; }

        // Soonest open contract only; null if wallet has none open.
        public WalletUpcomingMaturity NearestMaturity { get; set; }

        public List<WalletAssetRank> Ranking { get; set; } = [];

        public string SummaryText { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public class WalletAssetEntered
    {
        public string Asset { get; set; }

        public int ContractCount { get; set; }

        // Sum of StartAmount across all this wallet's contracts in the asset (incl. Finished).
        public decimal TotalEnteredPrincipal { get; set; }

        // Both profit withdrawal buckets combined.
        public decimal TotalProfitReceived { get; set; }
    }

    public class WalletUpcomingMaturity
    {
        public string StakeReference { get; set; }
        public string Asset { get; set; }
        public DateTime EndMoment { get; set; }

        public decimal RemainingPrincipal { get; set; }
    }

    public class WalletAssetRank
    {
        public string Asset { get; set; }

        // 1 = highest entered principal in this asset.
        public int Rank { get; set; }

        public int TotalWalletsConsidered { get; set; }

        // Rank <= TopRankThreshold from the request.
        public bool IsTopN { get; set; }
    }
}
