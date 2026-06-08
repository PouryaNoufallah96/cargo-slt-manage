namespace SLT.Services._Treasury.DTOs.Updates
{
    public class OverviewUpdate
    {
        // Upcoming-maturity window in days. Default 30; null = no bound.
        public int? Limit { get; set; } = 30;
    }
}
