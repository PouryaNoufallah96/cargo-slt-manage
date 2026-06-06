namespace SLT.Services._Treasury
{
    public enum TreasuryAsset { LUSD, GOLDGR }

    public enum TreasuryAssetFilter { All, LUSD, GOLDGR }

    public static class TreasuryAssets
    {
        public const string Lusd = "LUSD";
        public const string Goldgr = "GOLDGR";

        // Reports only cover these two symbols — everything else is ignored server-side.
        public static readonly string[] UniverseSymbols = [Goldgr, Lusd];

        // Stable display order; per-asset sections are seeded from this even when zero.
        public static readonly TreasuryAsset[] Universe = [TreasuryAsset.LUSD, TreasuryAsset.GOLDGR];

        public static string SymbolOf(TreasuryAsset asset)
            => asset == TreasuryAsset.GOLDGR ? Goldgr : Lusd;
    }
}
