namespace SLT.Services._Treasury.DTOs.Results
{
    public class MaturityCalendarResult
    {
        public DateTime ReportAsOfMoment { get; set; }

        public DateTime FromInclusive { get; set; }
        public DateTime ToExclusive { get; set; }

        // Matured but not redeemed — shown outside the selected horizon.
        public MaturityCalendarBucket DueNowOverdue { get; set; } = new MaturityCalendarBucket();

        // Still within term and EndMoment falls in [FromInclusive, ToExclusive).
        public MaturityCalendarBucket FutureMaturing { get; set; } = new MaturityCalendarBucket();

        public string SummaryText { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public class MaturityCalendarBucket
    {
        public List<MaturityAssetTotals> PerAsset { get; set; } = [];

        public List<MaturityPlanBreakdown> ByPlan { get; set; } = [];
    }

    public class MaturityAssetTotals
    {
        public string Asset { get; set; }
        public int Count { get; set; }

        public decimal PrincipalToReturn { get; set; }

        public decimal ProfitToPay { get; set; }
    }

    public class MaturityPlanBreakdown
    {
        public int PlanType { get; set; }
        public List<MaturityAssetTotals> PerAsset { get; set; } = [];
    }
}
