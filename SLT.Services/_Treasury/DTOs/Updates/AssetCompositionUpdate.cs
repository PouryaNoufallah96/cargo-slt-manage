namespace SLT.Services._Treasury.DTOs.Updates
{
    public class AssetCompositionUpdate
    {
        // No asset filter — whole-program view only. Plan narrows both asset and plan sections.
        public int? PlanType { get; set; }
    }
}
