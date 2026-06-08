namespace SLT.Services._Treasury.DTOs.Results
{
    public class HistoricalPerformanceResult
    {
        public DateTime ReportAsOfMoment { get; set; }

        public List<PeriodPerformance> Data { get; set; } = [];

        // Period count (not stake/withdrawal row count).
        public long TotalCount { get; set; }
        public int PageCount { get; set; }

        public string SummaryText { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public class PeriodPerformance
    {
        public string Label { get; set; }
        public DateTime FromInclusive { get; set; }
        public DateTime ToExclusive { get; set; }

        // Stake.StartMoment in this bucket.
        public int NewContractCount { get; set; }
        public List<PeriodAssetAmount> AttractedByAsset { get; set; } = [];

        // Withdrawal.CreatedMoment, State=Success — both withdrawal types.
        public List<PeriodAssetAmount> ProfitPaidByAsset { get; set; } = [];

        // Distinct StakeWithdrawal successes in the bucket (= completions).
        public int FinishedContractCount { get; set; }
    }

    public class PeriodAssetAmount
    {
        public string Asset { get; set; }
        public decimal Amount { get; set; }
    }
}
