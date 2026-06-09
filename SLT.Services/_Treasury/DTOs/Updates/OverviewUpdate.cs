namespace SLT.Services._Treasury.DTOs.Updates
{
    public class OverviewUpdate
    {
        // Upcoming-maturity window in days. Absent or null both default to 30.
        public int? Limit { get; set; } = 30;
    }
}
