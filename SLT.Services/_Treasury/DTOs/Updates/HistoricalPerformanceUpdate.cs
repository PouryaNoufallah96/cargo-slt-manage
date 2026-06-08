using SLT.Services._Treasury.Calculators;
using Utilities.DTOs;

namespace SLT.Services._Treasury.DTOs.Updates
{
    public class HistoricalPerformanceUpdate
    {
        public CalendarGrouping Grouping { get; set; } = CalendarGrouping.Monthly;

        // From-inclusive / to-exclusive.
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public TreasuryAssetFilter Asset { get; set; } = TreasuryAssetFilter.All;

        public Pagination Pagination { get; set; } = new();
    }
}
