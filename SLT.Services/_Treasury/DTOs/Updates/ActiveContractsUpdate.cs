using Utilities.DTOs;

namespace SLT.Services._Treasury.DTOs.Updates
{
    public class ActiveContractsUpdate
    {
        public string Wallet { get; set; }

        public string ContractId { get; set; }

        public TreasuryAssetFilter Asset { get; set; } = TreasuryAssetFilter.All;

        public int? PlanType { get; set; }

        // Date ranges are from-inclusive / to-exclusive (>= from, < to).
        public DateTime? StartFrom { get; set; }
        public DateTime? StartTo { get; set; }

        public DateTime? EndFrom { get; set; }
        public DateTime? EndTo { get; set; }

        // Open = all Active (within-term + matured-unredeemed). Finished only when asked.
        public ActiveContractsStatusFilter Status { get; set; } = ActiveContractsStatusFilter.Open;

        public Pagination Pagination { get; set; } = new();
    }

    public enum ActiveContractsStatusFilter { Open, WithinTerm, MaturedUnredeemed, Finished, All }
}
