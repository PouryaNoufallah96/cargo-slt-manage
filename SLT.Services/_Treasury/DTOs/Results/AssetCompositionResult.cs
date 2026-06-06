using SLT.Services._Treasury.Calculators;

namespace SLT.Services._Treasury.DTOs.Results
{
    public class AssetCompositionResult
    {
        public DateTime ReportAsOfMoment { get; set; }

        public ValuationMethod ValuationMethod { get; set; } = ValuationMethod.DepositTimeTokenPrice;

        public string ValuationNote { get; set; }

        // False when any open stake had no usable TokenPrice — native amounts still correct, % may be incomplete.
        public bool ValuationComplete { get; set; } = true;

        public List<AssetObligationShare> ByAsset { get; set; } = [];

        public List<PlanObligationShare> Plans { get; set; } = [];

        public string SummaryText { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public class AssetObligationShare
    {
        public string Asset { get; set; }

        // Native units — includes every open stake for this asset.
        public decimal NativeOpenObligation { get; set; }

        // Deposit-time USD; stakes with bad TokenPrice are left out of this and SharePercent.
        public decimal ValuedObligationUsd { get; set; }

        public decimal SharePercent { get; set; }
    }

    public class PlanObligationShare
    {
        public int PlanType { get; set; }

        public int ContractCount { get; set; }

        public decimal ValuedObligationUsd { get; set; }

        public decimal SharePercent { get; set; }

        public List<PlanAssetObligation> ByAsset { get; set; } = [];
    }

    public class PlanAssetObligation
    {
        public string Asset { get; set; }

        public decimal NativeOpenObligation { get; set; }
    }
}
