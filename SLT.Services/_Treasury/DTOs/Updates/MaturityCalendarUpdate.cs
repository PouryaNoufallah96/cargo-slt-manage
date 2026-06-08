using SLT.Services._Treasury.Calculators;

namespace SLT.Services._Treasury.DTOs.Updates
{
    public class MaturityCalendarUpdate
    {
        // Default Next30Days — enum zero is Next7Days.
        public MaturityHorizon Horizon { get; set; } = MaturityHorizon.Next30Days;

        // Only when Horizon == Custom. Window is [from, to).
        public DateTime? CustomFrom { get; set; }
        public DateTime? CustomTo { get; set; }

        public TreasuryAssetFilter Asset { get; set; } = TreasuryAssetFilter.All;

        public int? PlanType { get; set; }
    }
}
