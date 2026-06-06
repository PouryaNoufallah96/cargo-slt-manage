namespace SLT.Services._Treasury.DTOs.Updates
{
    public class WalletAnalysisUpdate
    {
        // Matched case-insensitively against Stake.WalletAddress.
        public string Wallet { get; set; }

        // Used for IsTopN in the per-asset whale ranking.
        public int TopRankThreshold { get; set; } = 10;
    }
}
