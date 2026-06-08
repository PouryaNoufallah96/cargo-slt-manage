namespace SLT.Services._Treasury.DTOs.Results
{
    public class PlanTypeResult
    {
        public List<PlanTypeOption> PlanTypes { get; set; } = [];
    }

    public class PlanTypeOption
    {
        // Plan length in months; send as the planType filter.
        public int Key { get; set; }

        public string Label { get; set; }
    }
}
