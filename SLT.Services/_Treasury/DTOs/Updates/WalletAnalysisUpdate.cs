namespace SLT.Services._Treasury.DTOs.Updates
{
    public class WalletAnalysisUpdate
    {
        // Optional. Set → single-wallet deep-dive (matched case-insensitively against Stake.WalletAddress).
        // Empty → leaderboard mode (top wallets per asset).
        public string Wallet { get; set; }

        // Wallet mode: IsTopN cutoff. Leaderboard mode: wallets returned per asset (capped at 100).
        public int TopRankThreshold { get; set; } = 10;
    }
}
