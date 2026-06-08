namespace SLT.Services._Treasury.Exports
{
    public class ExportMetadata
    {
        public string ReportTitle { get; set; }

        public string AppliedFilters { get; set; }

        public DateTime GeneratedAtUtc { get; set; }

        // Null on reports with no deposit-time USD weighting (e.g. dashboard).
        public string ValuationNote { get; set; }
    }
}
