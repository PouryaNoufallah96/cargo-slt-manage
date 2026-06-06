namespace SLT.Services._Treasury.DTOs.Updates
{
    public class ContractDistributionUpdate
    {
        public TreasuryAssetFilter Asset { get; set; } = TreasuryAssetFilter.All;

        public int? PlanType { get; set; }
    }
}
