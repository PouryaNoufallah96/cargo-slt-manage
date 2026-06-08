using SLT.Services._Treasury.Calculators;

namespace SLT.Services._Treasury.DTOs.Results
{
    public class ContractDistributionResult
    {
        public DateTime ReportAsOfMoment { get; set; }

        public ValuationMethod ValuationMethod { get; set; } = ValuationMethod.DepositTimeTokenPrice;

        public string ValuationNote { get; set; }

        public List<PlanDistribution> Plans { get; set; } = [];

        // False when any open stake had no usable TokenPrice — native amounts still correct, % may be incomplete.
        public bool ValuationComplete { get; set; } = true;

        public string SummaryText { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public class PlanDistribution
    {
        public int PlanType { get; set; }

        public int ContractCount { get; set; }

        public List<PlanAssetCapital> ByAsset { get; set; } = [];

        // This plan's deposit-time USD share of all open principal (LUSD + GOLDGR combined).
        public decimal ValueWeightedSharePercent { get; set; }
    }

    public class PlanAssetCapital
    {
        public string Asset { get; set; }

        public decimal OpenPrincipal { get; set; }
    }
}
