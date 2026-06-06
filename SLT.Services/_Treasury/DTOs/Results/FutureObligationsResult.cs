namespace SLT.Services._Treasury.DTOs.Results
{
    public class FutureObligationsResult
    {
        public DateTime ReportAsOfMoment { get; set; }

        public DateTime FromInclusive { get; set; }
        public DateTime ToExclusive { get; set; }

        public List<AssetObligation> DueNowOverdue { get; set; } = [];

        public List<AssetObligation> FutureWithinHorizon { get; set; } = [];

        // Per asset: DueNowOverdue + FutureWithinHorizon (principal, profit, total each).
        public List<AssetObligation> TotalRequired { get; set; } = [];

        public string SummaryText { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public class AssetObligation
    {
        public string Asset { get; set; }

        public decimal PrincipalObligation { get; set; }

        public decimal ProfitObligation { get; set; }

        public decimal TotalObligation { get; set; }
    }
}
