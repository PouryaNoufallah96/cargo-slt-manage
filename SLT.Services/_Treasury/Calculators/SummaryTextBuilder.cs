using System.Globalization;
using System.Text;
using SLT.Services._Treasury.DTOs.Results;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Treasury.Calculators
{
    public class SummaryTextBuilder : ISummaryTextBuilder, IScopedDependency
    {
        public string BuildOverviewSummary(TreasuryOverviewResult overview)
        {
            if (overview == null)
                return "No treasury data available.";

            var contracts = overview.ActiveContracts ?? new TreasuryActiveContractsSummary();

            if (contracts.Total == 0)
                return "No open treasury contracts as of "
                     + overview.ReportAsOfMoment.ToString("u", CultureInfo.InvariantCulture)
                     + ".";

            var totalOpenObligation = overview.PerAsset == null
                ? 0m
                : overview.PerAsset.Sum(a => a.OpenObligation);

            var builder = new StringBuilder();
            builder.Append(contracts.Total);
            builder.Append(" open treasury contract");
            builder.Append(contracts.Total == 1 ? "" : "s");
            builder.Append(" (");
            builder.Append(contracts.WithinTerm);
            builder.Append(" within term, ");
            builder.Append(contracts.MaturedUnredeemed);
            builder.Append(" matured/unredeemed) across ");
            builder.Append(overview.ActiveUsers);
            builder.Append(overview.ActiveUsers == 1 ? " wallet" : " wallets");
            builder.Append(", with a total open obligation of ");
            builder.Append(totalOpenObligation.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" across LUSD and GOLDGR as of ");
            builder.Append(overview.ReportAsOfMoment.ToString("u", CultureInfo.InvariantCulture));
            builder.Append('.');

            return builder.ToString();
        }

        public string BuildMaturityCalendarSummary(MaturityCalendarResult calendar)
        {
            if (calendar == null)
                return "No treasury maturity data available.";

            var future = calendar.FutureMaturing ?? new MaturityCalendarBucket();
            var due = calendar.DueNowOverdue ?? new MaturityCalendarBucket();

            var futureContracts = future.PerAsset == null ? 0 : future.PerAsset.Sum(a => a.Count);
            var dueContracts = due.PerAsset == null ? 0 : due.PerAsset.Sum(a => a.Count);

            if (futureContracts == 0 && dueContracts == 0)
            {
                return "No treasury contracts mature by "
                     + calendar.ToExclusive.ToString("u", CultureInfo.InvariantCulture)
                     + " and none are currently due/overdue (as of "
                     + calendar.ReportAsOfMoment.ToString("u", CultureInfo.InvariantCulture)
                     + ").";
            }

            var lusd = AmountToPrepare(future, TreasuryAssets.Lusd);
            var goldgr = AmountToPrepare(future, TreasuryAssets.Goldgr);

            var builder = new StringBuilder();
            builder.Append("By ");
            builder.Append(calendar.ToExclusive.ToString("u", CultureInfo.InvariantCulture));
            builder.Append(", prepare ~");
            builder.Append(lusd.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" LUSD and ");
            builder.Append(goldgr.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" GOLDGR for repayment (");
            builder.Append(futureContracts);
            builder.Append(futureContracts == 1 ? " contract" : " contracts");
            builder.Append(" maturing; ");
            builder.Append(dueContracts);
            builder.Append(" already due/overdue).");

            return builder.ToString();
        }

        public string BuildActiveContractsSummary(ActiveContractsResult result)
        {
            if (result == null || result.TotalCount == 0)
                return "No treasury contracts match the filters.";

            var pageCount = result.PageCount < 1 ? 1 : result.PageCount;

            var builder = new StringBuilder();
            builder.Append(result.TotalCount);
            builder.Append(" open treasury contract");
            builder.Append(result.TotalCount == 1 ? "" : "s");
            builder.Append(" match the filters across ");
            builder.Append(pageCount);
            builder.Append(pageCount == 1 ? " page." : " pages.");

            return builder.ToString();
        }

        public string BuildWalletAnalysisSummary(WalletAnalysisResult result)
        {
            if (result == null)
                return "No treasury data available.";

            if (result.Mode == WalletAnalysisMode.Leaderboard)
                return BuildLeaderboardSummary(result);

            var wallet = string.IsNullOrEmpty(result.Wallet) ? "(unknown)" : result.Wallet;

            if (result.RegisteredContractCount == 0)
            {
                return "No treasury contracts for wallet "
                     + wallet
                     + " as of "
                     + result.ReportAsOfMoment.ToString("u", CultureInfo.InvariantCulture)
                     + ".";
            }

            var lusd = EnteredFor(result, TreasuryAssets.Lusd);
            var goldgr = EnteredFor(result, TreasuryAssets.Goldgr);

            var builder = new StringBuilder();
            builder.Append("Wallet ");
            builder.Append(wallet);
            builder.Append(" has ");
            builder.Append(result.RegisteredContractCount);
            builder.Append(result.RegisteredContractCount == 1 ? " registered contract" : " registered contracts");
            builder.Append(" (");
            builder.Append(result.ActiveOpenCount);
            builder.Append(" open: ");
            builder.Append(result.WithinTermCount);
            builder.Append(" within term, ");
            builder.Append(result.MaturedUnredeemedCount);
            builder.Append(" matured/unredeemed; ");
            builder.Append(result.FinishedCount);
            builder.Append(" finished), entered ");
            builder.Append(lusd.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" LUSD and ");
            builder.Append(goldgr.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" GOLDGR as of ");
            builder.Append(result.ReportAsOfMoment.ToString("u", CultureInfo.InvariantCulture));
            builder.Append('.');

            return builder.ToString();
        }

        private static string BuildLeaderboardSummary(WalletAnalysisResult result)
        {
            var assets = result.TopWallets == null
                ? new List<WalletLeaderboardAsset>()
                : result.TopWallets.Where(a => a != null && a.Rows != null && a.Rows.Count > 0).ToList();

            if (assets.Count == 0)
                return "No treasury wallets to rank as of "
                     + result.ReportAsOfMoment.ToString("u", CultureInfo.InvariantCulture)
                     + ".";

            var parts = new List<string>();
            foreach (var asset in assets)
            {
                var top = asset.Rows[0];
                parts.Add(asset.Asset
                    + " led by " + (string.IsNullOrEmpty(top.WalletAddress) ? "(unknown)" : top.WalletAddress)
                    + " (" + top.EnteredPrincipal.ToString("0.########", CultureInfo.InvariantCulture) + ")");
            }

            return "Top wallets per asset by entered principal as of "
                 + result.ReportAsOfMoment.ToString("u", CultureInfo.InvariantCulture)
                 + ": " + string.Join("; ", parts) + ".";
        }

        public string BuildContractDistributionSummary(ContractDistributionResult result)
        {
            if (result == null || result.Plans == null || result.Plans.Count == 0)
                return "No open treasury contracts to distribute across plan durations.";

            var totalContracts = result.Plans.Sum(p => p.ContractCount);
            if (totalContracts == 0)
                return "No open treasury contracts to distribute across plan durations.";

            var ranked = result.Plans
                .OrderByDescending(p => p.ValueWeightedSharePercent)
                .ThenBy(p => p.PlanType)
                .ToList();

            var builder = new StringBuilder();
            builder.Append(totalContracts);
            builder.Append(" open treasury contract");
            builder.Append(totalContracts == 1 ? "" : "s");
            builder.Append(" across ");
            builder.Append(result.Plans.Count);
            builder.Append(result.Plans.Count == 1 ? " plan duration" : " plan durations");

            if (ranked.Count == 1)
            {
                builder.Append("; capital sits entirely in the ");
                builder.Append(ranked[0].PlanType);
                builder.Append("-month plan");
            }
            else
            {
                builder.Append("; most capital is concentrated in the ");
                builder.Append(ranked[0].PlanType);
                builder.Append("- and ");
                builder.Append(ranked[1].PlanType);
                builder.Append("-month plans");
            }

            builder.Append(" (value-weighted at deposit-time price)");

            if (!result.ValuationComplete)
                builder.Append("; some contracts had no deposit-time price, so the value-weighted shares are incomplete");

            builder.Append('.');

            return builder.ToString();
        }

        public string BuildHistoricalPerformanceSummary(HistoricalPerformanceResult result)
        {
            if (result == null || result.Data == null || result.Data.Count == 0)
                return "No treasury performance data available.";

            var newContracts = result.Data.Sum(p => p.NewContractCount);
            var finishedContracts = result.Data.Sum(p => p.FinishedContractCount);

            var attractedLusd = SumPeriodAsset(result.Data, p => p.AttractedByAsset, TreasuryAssets.Lusd);
            var attractedGoldgr = SumPeriodAsset(result.Data, p => p.AttractedByAsset, TreasuryAssets.Goldgr);
            var profitLusd = SumPeriodAsset(result.Data, p => p.ProfitPaidByAsset, TreasuryAssets.Lusd);
            var profitGoldgr = SumPeriodAsset(result.Data, p => p.ProfitPaidByAsset, TreasuryAssets.Goldgr);

            if (newContracts == 0 && finishedContracts == 0
                && attractedLusd == 0m && attractedGoldgr == 0m
                && profitLusd == 0m && profitGoldgr == 0m)
            {
                return "No treasury activity between "
                     + result.Data[0].FromInclusive.ToString("u", CultureInfo.InvariantCulture)
                     + " and "
                     + result.Data[^1].ToExclusive.ToString("u", CultureInfo.InvariantCulture)
                     + " across "
                     + result.Data.Count
                     + (result.Data.Count == 1 ? " period." : " periods.");
            }

            var builder = new StringBuilder();
            builder.Append("Between ");
            builder.Append(result.Data[0].FromInclusive.ToString("u", CultureInfo.InvariantCulture));
            builder.Append(" and ");
            builder.Append(result.Data[^1].ToExclusive.ToString("u", CultureInfo.InvariantCulture));
            builder.Append(" (");
            builder.Append(result.Data.Count);
            builder.Append(result.Data.Count == 1 ? " period): " : " periods): ");
            builder.Append(newContracts);
            builder.Append(newContracts == 1 ? " new contract attracting " : " new contracts attracting ");
            builder.Append(attractedLusd.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" LUSD and ");
            builder.Append(attractedGoldgr.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" GOLDGR; ");
            builder.Append(profitLusd.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" LUSD and ");
            builder.Append(profitGoldgr.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" GOLDGR profit paid; ");
            builder.Append(finishedContracts);
            builder.Append(finishedContracts == 1 ? " contract finished." : " contracts finished.");

            return builder.ToString();
        }

        public string BuildFutureObligationsSummary(FutureObligationsResult result)
        {
            if (result == null)
                return "No treasury obligation data available.";

            var total = result.TotalRequired ?? [];
            var due = result.DueNowOverdue ?? [];

            var totalRequired = total.Sum(a => a.TotalObligation);

            if (totalRequired == 0m)
            {
                return "No treasury obligations fall due by "
                     + result.ToExclusive.ToString("u", CultureInfo.InvariantCulture)
                     + " (as of "
                     + result.ReportAsOfMoment.ToString("u", CultureInfo.InvariantCulture)
                     + ").";
            }

            var lusd = ObligationFor(total, TreasuryAssets.Lusd);
            var goldgr = ObligationFor(total, TreasuryAssets.Goldgr);
            var dueTotal = due.Sum(a => a.TotalObligation);

            var builder = new StringBuilder();
            builder.Append("If no new contracts are created, the program must pay ");
            builder.Append(lusd.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" LUSD and ");
            builder.Append(goldgr.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" GOLDGR within this horizon (by ");
            builder.Append(result.ToExclusive.ToString("u", CultureInfo.InvariantCulture));
            builder.Append("), of which ");
            builder.Append(dueTotal.ToString("0.########", CultureInfo.InvariantCulture));
            builder.Append(" is already due now/overdue.");

            return builder.ToString();
        }

        public string BuildAssetCompositionSummary(AssetCompositionResult result)
        {
            if (result == null || result.Plans == null || result.Plans.Count == 0)
                return "No open treasury contracts to compose by asset or plan.";

            var totalContracts = result.Plans.Sum(p => p.ContractCount);
            if (totalContracts == 0)
                return "No open treasury contracts to compose by asset or plan.";

            var rankedPlans = result.Plans
                .OrderByDescending(p => p.SharePercent)
                .ThenBy(p => p.PlanType)
                .ToList();

            var builder = new StringBuilder();

            var totalValuedUsd = result.ByAsset == null ? 0m : result.ByAsset.Sum(a => a.ValuedObligationUsd);

            if (totalValuedUsd == 0m)
            {
                builder.Append(totalContracts);
                builder.Append(" open treasury contract");
                builder.Append(totalContracts == 1 ? "" : "s");
                builder.Append(" across ");
                builder.Append(result.Plans.Count);
                builder.Append(result.Plans.Count == 1 ? " plan duration" : " plan durations");
                builder.Append("; obligation-weighted composition is unavailable (no deposit-time prices)");
                builder.Append('.');
                return builder.ToString();
            }

            var dominantAsset = result.ByAsset
                .OrderByDescending(a => a.SharePercent)
                .First();

            builder.Append(dominantAsset.SharePercent.ToString("0.##", CultureInfo.InvariantCulture));
            builder.Append("% of valued obligations are based on ");
            builder.Append(dominantAsset.Asset);

            if (rankedPlans.Count == 1)
            {
                builder.Append(", concentrated in the ");
                builder.Append(rankedPlans[0].PlanType);
                builder.Append("-month plan");
            }
            else
            {
                builder.Append(", concentrated in the ");
                builder.Append(rankedPlans[0].PlanType);
                builder.Append("- and ");
                builder.Append(rankedPlans[1].PlanType);
                builder.Append("-month plans");
            }

            builder.Append(" (deposit-time price)");

            if (!result.ValuationComplete)
                builder.Append("; some open contracts had no deposit-time price, so the shares are incomplete");

            builder.Append('.');

            return builder.ToString();
        }

        private static decimal ObligationFor(List<AssetObligation> obligations, string assetSymbol)
        {
            if (obligations == null)
                return 0m;

            return obligations
                .Where(a => string.Equals(a.Asset, assetSymbol, StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.TotalObligation);
        }

        private static decimal SumPeriodAsset(
            List<PeriodPerformance> periods,
            Func<PeriodPerformance, List<PeriodAssetAmount>> selector,
            string assetSymbol)
        {
            var total = 0m;
            foreach (var period in periods)
            {
                var amounts = selector(period);
                if (amounts == null)
                    continue;

                total += amounts
                    .Where(a => string.Equals(a.Asset, assetSymbol, StringComparison.OrdinalIgnoreCase))
                    .Sum(a => a.Amount);
            }

            return total;
        }

        private static decimal EnteredFor(WalletAnalysisResult result, string assetSymbol)
        {
            if (result?.EnteredByAsset == null)
                return 0m;

            return result.EnteredByAsset
                .Where(a => string.Equals(a.Asset, assetSymbol, StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.TotalEnteredPrincipal);
        }

        private static decimal AmountToPrepare(MaturityCalendarBucket bucket, string assetSymbol)
        {
            if (bucket?.PerAsset == null)
                return 0m;

            return bucket.PerAsset
                .Where(a => string.Equals(a.Asset, assetSymbol, StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.PrincipalToReturn + a.ProfitToPay);
        }
    }
}
