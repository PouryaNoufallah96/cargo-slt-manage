using SLT.Services._Treasury.DTOs.Updates;
using Utilities.DTOs;

namespace SLT.Services._Treasury.Exports
{
    public static class ExportFilterText
    {
        public static string For(OverviewUpdate update)
        {
            if (update == null)
                return "None";

            var parts = new List<string>();
            parts.Add("Limit=" + (update.Limit ?? 30) + "d");
            return Join(parts);
        }

        public static string For(MaturityCalendarUpdate update)
        {
            if (update == null)
                return "None";

            var parts = new List<string>();
            parts.Add("Horizon=" + update.Horizon);
            AppendCustomWindow(parts, update.CustomFrom, update.CustomTo);
            AppendAsset(parts, update.Asset);
            AppendPlan(parts, update.PlanType);
            return Join(parts);
        }

        public static string For(FutureObligationsUpdate update)
        {
            if (update == null)
                return "None";

            var parts = new List<string>();
            parts.Add("Horizon=" + update.Horizon);
            AppendCustomWindow(parts, update.CustomFrom, update.CustomTo);
            AppendAsset(parts, update.Asset);
            AppendPlan(parts, update.PlanType);
            return Join(parts);
        }

        public static string For(ActiveContractsUpdate update)
        {
            if (update == null)
                return "None";

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(update.Wallet))
                parts.Add("Wallet=" + update.Wallet);
            if (!string.IsNullOrEmpty(update.ContractId))
                parts.Add("ContractId=" + update.ContractId);
            AppendAsset(parts, update.Asset);
            AppendPlan(parts, update.PlanType);
            AppendRange(parts, "Start", update.StartFrom, update.StartTo);
            AppendRange(parts, "End", update.EndFrom, update.EndTo);
            parts.Add("Status=" + update.Status);
            AppendPagination(parts, update.Pagination);
            return Join(parts);
        }

        public static string For(WalletAnalysisUpdate update)
        {
            if (update == null)
                return "None";

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(update.Wallet))
                parts.Add("Wallet=" + update.Wallet);
            parts.Add("TopRankThreshold=" + update.TopRankThreshold);
            return Join(parts);
        }

        public static string For(ContractDistributionUpdate update)
        {
            if (update == null)
                return "None";

            var parts = new List<string>();
            AppendAsset(parts, update.Asset);
            AppendPlan(parts, update.PlanType);
            return Join(parts);
        }

        public static string For(HistoricalPerformanceUpdate update)
        {
            if (update == null)
                return "None";

            var parts = new List<string>();
            parts.Add("Grouping=" + update.Grouping);
            parts.Add("From=" + update.From.ToString("u"));
            parts.Add("To=" + update.To.ToString("u"));
            AppendAsset(parts, update.Asset);
            AppendPagination(parts, update.Pagination);
            return Join(parts);
        }

        public static string For(AssetCompositionUpdate update)
        {
            if (update == null)
                return "None";

            var parts = new List<string>();
            AppendPlan(parts, update.PlanType);
            return Join(parts);
        }

        private static void AppendAsset(List<string> parts, TreasuryAssetFilter asset)
        {
            if (asset != TreasuryAssetFilter.All)
                parts.Add("Asset=" + asset);
        }

        private static void AppendPlan(List<string> parts, int? planType)
        {
            if (planType.HasValue)
                parts.Add("Plan=" + planType.Value + "mo");
        }

        private static void AppendCustomWindow(List<string> parts, DateTime? from, DateTime? to)
        {
            if (from.HasValue)
                parts.Add("CustomFrom=" + from.Value.ToString("u"));
            if (to.HasValue)
                parts.Add("CustomTo=" + to.Value.ToString("u"));
        }

        private static void AppendRange(List<string> parts, string label, DateTime? from, DateTime? to)
        {
            if (from.HasValue)
                parts.Add(label + "From=" + from.Value.ToString("u"));
            if (to.HasValue)
                parts.Add(label + "To=" + to.Value.ToString("u"));
        }

        private static void AppendPagination(List<string> parts, Pagination pagination)
        {
            if (pagination == null)
                return;
            parts.Add("Page=" + pagination.Page);
            parts.Add("Size=" + pagination.Size);
        }

        private static string Join(List<string> parts)
        {
            if (parts == null || parts.Count == 0)
                return "None";
            return string.Join("; ", parts);
        }
    }
}
