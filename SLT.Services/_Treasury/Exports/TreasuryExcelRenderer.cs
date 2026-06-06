using ClosedXML.Excel;
using SLT.Services._Treasury.DTOs.Results;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Treasury.Exports
{
    public class TreasuryExcelRenderer : ITreasuryExcelRenderer, IScopedDependency
    {
        public byte[] RenderOverview(TreasuryOverviewResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new TreasuryOverviewResult();
            if (meta == null)
                meta = new ExportMetadata();

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Overview");
                var row = 1;

                row = WriteHeaderBlock(sheet, row, meta);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Per-Asset Obligations");
                row = WriteTable(
                    sheet,
                    row,
                    ["Asset", "Open Principal", "Remaining Profit Owed", "Open Obligation"],
                    BuildPerAssetRows(model.PerAsset));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Active Contracts");
                row = WriteTable(
                    sheet,
                    row,
                    ["Within Term", "Matured Unredeemed", "Total"],
                    [
                        [
                            model.ActiveContracts == null ? 0 : model.ActiveContracts.WithinTerm,
                            model.ActiveContracts == null ? 0 : model.ActiveContracts.MaturedUnredeemed,
                            model.ActiveContracts == null ? 0 : model.ActiveContracts.Total
                        ]
                    ]);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Active Users");
                row = WriteLabelValue(sheet, row, "Distinct active wallets", model.ActiveUsers);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Nearest Maturities");
                row = WriteTable(
                    sheet,
                    row,
                    ["Stake Reference", "Asset", "Wallet Address", "End Moment (UTC)", "Remaining Principal"],
                    BuildMaturityRows(model.NearestMaturities));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Summary");
                row = WriteLabelValue(sheet, row, "Summary", Text(model.SummaryText));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Warnings");
                row = WriteWarnings(sheet, row, model.Warnings);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] RenderMaturityCalendar(MaturityCalendarResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new MaturityCalendarResult();
            if (meta == null)
                meta = new ExportMetadata();

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Maturity Calendar");
                var row = 1;

                row = WriteHeaderBlock(sheet, row, meta);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Resolved Window");
                row = WriteLabelValue(sheet, row, "From (inclusive, UTC)", model.FromInclusive.ToString("u"));
                row = WriteLabelValue(sheet, row, "To (exclusive, UTC)", model.ToExclusive.ToString("u"));
                row += 1;

                row = WriteMaturityBucket(sheet, row, "Due Now / Overdue", model.DueNowOverdue);
                row += 1;
                row = WriteMaturityBucket(sheet, row, "Future Maturing", model.FutureMaturing);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Summary");
                row = WriteLabelValue(sheet, row, "Summary", Text(model.SummaryText));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Warnings");
                row = WriteWarnings(sheet, row, model.Warnings);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] RenderFutureObligations(FutureObligationsResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new FutureObligationsResult();
            if (meta == null)
                meta = new ExportMetadata();

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Future Obligations");
                var row = 1;

                row = WriteHeaderBlock(sheet, row, meta);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Resolved Window");
                row = WriteLabelValue(sheet, row, "From (inclusive, UTC)", model.FromInclusive.ToString("u"));
                row = WriteLabelValue(sheet, row, "To (exclusive, UTC)", model.ToExclusive.ToString("u"));
                row += 1;

                row = WriteObligationBucket(sheet, row, "Due Now / Overdue", model.DueNowOverdue);
                row += 1;
                row = WriteObligationBucket(sheet, row, "Future Within Horizon", model.FutureWithinHorizon);
                row += 1;
                row = WriteObligationBucket(sheet, row, "Total Required", model.TotalRequired);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Summary");
                row = WriteLabelValue(sheet, row, "Summary", Text(model.SummaryText));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Warnings");
                row = WriteWarnings(sheet, row, model.Warnings);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] RenderActiveContracts(ActiveContractsResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new ActiveContractsResult();
            if (meta == null)
                meta = new ExportMetadata();

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Active Contracts");
                var row = 1;

                row = WriteHeaderBlock(sheet, row, meta);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Totals");
                row = WriteLabelValue(sheet, row, "Total contracts (all pages)", model.TotalCount);
                row = WriteLabelValue(sheet, row, "Page count", model.PageCount);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Contracts (this page)");
                row = WriteTable(
                    sheet,
                    row,
                    ["Wallet Address", "Stake Reference", "Asset", "Principal", "Plan (months)", "Start (UTC)", "End (UTC)", "Profit Received", "Remaining Profit Owed", "Status"],
                    BuildActiveContractRows(model.Data));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Summary");
                row = WriteLabelValue(sheet, row, "Summary", Text(model.SummaryText));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Warnings");
                row = WriteWarnings(sheet, row, model.Warnings);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] RenderWalletAnalysis(WalletAnalysisResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new WalletAnalysisResult();
            if (meta == null)
                meta = new ExportMetadata();

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Wallet Analysis");
                var row = 1;

                row = WriteHeaderBlock(sheet, row, meta);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Wallet");
                row = WriteLabelValue(sheet, row, "Wallet", Text(model.Wallet));
                row = WriteLabelValue(sheet, row, "Report as-of (UTC)", model.ReportAsOfMoment.ToString("u"));
                row = WriteLabelValue(sheet, row, "Registered contracts", model.RegisteredContractCount);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Counts");
                row = WriteTable(
                    sheet,
                    row,
                    ["Within Term", "Matured Unredeemed", "Active Open", "Finished"],
                    [
                        [
                            model.WithinTermCount,
                            model.MaturedUnredeemedCount,
                            model.ActiveOpenCount,
                            model.FinishedCount
                        ]
                    ]);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Entered By Asset");
                row = WriteTable(
                    sheet,
                    row,
                    ["Asset", "Contract Count", "Total Entered Principal", "Total Profit Received"],
                    BuildWalletEnteredRows(model.EnteredByAsset));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Nearest Maturity");
                if (model.NearestMaturity == null)
                {
                    sheet.Cell(row, 1).Value = "None open";
                    row += 1;
                }
                else
                {
                    row = WriteTable(
                        sheet,
                        row,
                        ["Stake Reference", "Asset", "End Moment (UTC)", "Remaining Principal"],
                        [
                            [
                                Text(model.NearestMaturity.StakeReference),
                                Text(model.NearestMaturity.Asset),
                                model.NearestMaturity.EndMoment.ToString("u"),
                                model.NearestMaturity.RemainingPrincipal
                            ]
                        ]);
                }
                row += 1;

                row = WriteSectionTitle(sheet, row, "Whale Ranking");
                row = WriteTable(
                    sheet,
                    row,
                    ["Asset", "Rank", "Wallets Considered", "Is Top-N"],
                    BuildWalletRankRows(model.Ranking));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Summary");
                row = WriteLabelValue(sheet, row, "Summary", Text(model.SummaryText));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Warnings");
                row = WriteWarnings(sheet, row, model.Warnings);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] RenderContractDistribution(ContractDistributionResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new ContractDistributionResult();
            if (meta == null)
                meta = new ExportMetadata();

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Contract Distribution");
                var row = 1;

                row = WriteHeaderBlock(sheet, row, meta);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Valuation");
                row = WriteLabelValue(sheet, row, "Method", model.ValuationMethod.ToString());
                row = WriteLabelValue(sheet, row, "Valuation complete", model.ValuationComplete);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Plan Distribution");
                row = WriteTable(
                    sheet,
                    row,
                    ["Plan (months)", "Contract Count", "Value-Weighted Share %"],
                    BuildPlanDistributionRows(model.Plans));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Per-Plan Open Principal By Asset");
                row = WriteTable(
                    sheet,
                    row,
                    ["Plan (months)", "Asset", "Open Principal"],
                    BuildPlanDistributionAssetRows(model.Plans));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Summary");
                row = WriteLabelValue(sheet, row, "Summary", Text(model.SummaryText));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Warnings");
                row = WriteWarnings(sheet, row, model.Warnings);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] RenderHistoricalPerformance(HistoricalPerformanceResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new HistoricalPerformanceResult();
            if (meta == null)
                meta = new ExportMetadata();

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Historical Performance");
                var row = 1;

                row = WriteHeaderBlock(sheet, row, meta);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Totals");
                row = WriteLabelValue(sheet, row, "Total periods (all pages)", model.TotalCount);
                row = WriteLabelValue(sheet, row, "Page count", model.PageCount);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Periods (this page)");
                row = WriteTable(
                    sheet,
                    row,
                    ["Period", "From (UTC)", "To (UTC)", "New Contracts", "Asset", "Attracted Capital", "Profit Paid", "Finished Contracts"],
                    BuildHistoricalPerformanceRows(model.Data));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Summary");
                row = WriteLabelValue(sheet, row, "Summary", Text(model.SummaryText));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Warnings");
                row = WriteWarnings(sheet, row, model.Warnings);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] RenderAssetComposition(AssetCompositionResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new AssetCompositionResult();
            if (meta == null)
                meta = new ExportMetadata();

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Asset Composition");
                var row = 1;

                row = WriteHeaderBlock(sheet, row, meta);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Valuation");
                row = WriteLabelValue(sheet, row, "Method", model.ValuationMethod.ToString());
                row = WriteLabelValue(sheet, row, "Valuation complete", model.ValuationComplete);
                row += 1;

                row = WriteSectionTitle(sheet, row, "Composition By Asset");
                row = WriteTable(
                    sheet,
                    row,
                    ["Asset", "Native Open Obligation", "Valued Obligation (USD)", "Share %"],
                    BuildAssetObligationShareRows(model.ByAsset));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Plan Composition");
                row = WriteTable(
                    sheet,
                    row,
                    ["Plan (months)", "Contract Count", "Valued Obligation (USD)", "Share %"],
                    BuildPlanObligationShareRows(model.Plans));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Per-Plan Open Obligation By Asset");
                row = WriteTable(
                    sheet,
                    row,
                    ["Plan (months)", "Asset", "Native Open Obligation"],
                    BuildPlanObligationAssetRows(model.Plans));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Summary");
                row = WriteLabelValue(sheet, row, "Summary", Text(model.SummaryText));
                row += 1;

                row = WriteSectionTitle(sheet, row, "Warnings");
                row = WriteWarnings(sheet, row, model.Warnings);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        #region Layout

        private static int WriteHeaderBlock(IXLWorksheet sheet, int row, ExportMetadata meta)
        {
            var titleCell = sheet.Cell(row, 1);
            titleCell.Value = Text(meta.ReportTitle);
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;
            row += 1;

            sheet.Cell(row, 1).Value = "Generated (UTC):";
            sheet.Cell(row, 2).Value = meta.GeneratedAtUtc.ToString("u");
            row += 1;

            sheet.Cell(row, 1).Value = "Filters:";
            sheet.Cell(row, 2).Value = Text(meta.AppliedFilters);
            row += 1;

            if (!string.IsNullOrEmpty(meta.ValuationNote))
            {
                sheet.Cell(row, 1).Value = "Valuation:";
                sheet.Cell(row, 2).Value = Text(meta.ValuationNote);
                row += 1;
            }

            return row;
        }

        private static int WriteSectionTitle(IXLWorksheet sheet, int row, string title)
        {
            var cell = sheet.Cell(row, 1);
            cell.Value = Text(title);
            cell.Style.Font.Bold = true;
            return row + 1;
        }

        private static int WriteTable(IXLWorksheet sheet, int row, string[] headers, IEnumerable<object[]> rows)
        {
            for (var col = 0; col < headers.Length; col++)
            {
                var cell = sheet.Cell(row, col + 1);
                cell.Value = Text(headers[col]);
                cell.Style.Font.Bold = true;
            }
            row += 1;

            foreach (var dataRow in rows)
            {
                for (var col = 0; col < dataRow.Length; col++)
                    WriteCell(sheet.Cell(row, col + 1), dataRow[col]);
                row += 1;
            }

            return row;
        }

        private static int WriteLabelValue(IXLWorksheet sheet, int row, string label, object value)
        {
            sheet.Cell(row, 1).Value = Text(label);
            WriteCell(sheet.Cell(row, 2), value);
            return row + 1;
        }

        private static int WriteWarnings(IXLWorksheet sheet, int row, List<string> warnings)
        {
            if (warnings == null || warnings.Count == 0)
            {
                sheet.Cell(row, 1).Value = "None";
                return row + 1;
            }

            foreach (var warning in warnings)
            {
                sheet.Cell(row, 1).Value = Text(warning);
                row += 1;
            }

            return row;
        }

        private static int WriteMaturityBucket(IXLWorksheet sheet, int row, string title, MaturityCalendarBucket bucket)
        {
            row = WriteSectionTitle(sheet, row, title);

            row = WriteTable(
                sheet,
                row,
                ["Asset", "Count", "Principal To Return", "Profit To Pay"],
                BuildMaturityAssetRows(bucket == null ? null : bucket.PerAsset));

            row = WriteTable(
                sheet,
                row,
                ["Plan (months)", "Asset", "Count", "Principal To Return", "Profit To Pay"],
                BuildMaturityPlanRows(bucket == null ? null : bucket.ByPlan));

            return row;
        }

        private static int WriteObligationBucket(IXLWorksheet sheet, int row, string title, List<AssetObligation> obligations)
        {
            row = WriteSectionTitle(sheet, row, title);
            row = WriteTable(
                sheet,
                row,
                ["Asset", "Principal Obligation", "Profit Obligation", "Total Obligation"],
                BuildObligationRows(obligations));
            return row;
        }

        #endregion

        #region Rows

        private static IEnumerable<object[]> BuildPerAssetRows(List<TreasuryAssetObligationSummary> perAsset)
        {
            if (perAsset == null)
                yield break;

            foreach (var asset in perAsset)
            {
                if (asset == null)
                    continue;

                yield return
                [
                    Text(asset.Asset),
                    asset.OpenPrincipal,
                    asset.RemainingProfitOwed,
                    asset.OpenObligation
                ];
            }
        }

        private static IEnumerable<object[]> BuildMaturityRows(List<TreasuryUpcomingMaturity> maturities)
        {
            if (maturities == null)
                yield break;

            foreach (var maturity in maturities)
            {
                if (maturity == null)
                    continue;

                yield return
                [
                    Text(maturity.StakeReference),
                    Text(maturity.Asset),
                    Text(maturity.WalletAddress),
                    maturity.EndMoment.ToString("u"),
                    maturity.RemainingPrincipal
                ];
            }
        }

        private static IEnumerable<object[]> BuildMaturityAssetRows(List<MaturityAssetTotals> perAsset)
        {
            if (perAsset == null)
                yield break;

            foreach (var asset in perAsset)
            {
                if (asset == null)
                    continue;

                yield return
                [
                    Text(asset.Asset),
                    asset.Count,
                    asset.PrincipalToReturn,
                    asset.ProfitToPay
                ];
            }
        }

        private static IEnumerable<object[]> BuildMaturityPlanRows(List<MaturityPlanBreakdown> byPlan)
        {
            if (byPlan == null)
                yield break;

            foreach (var plan in byPlan)
            {
                if (plan == null || plan.PerAsset == null)
                    continue;

                foreach (var asset in plan.PerAsset)
                {
                    if (asset == null)
                        continue;

                    yield return
                    [
                        plan.PlanType,
                        Text(asset.Asset),
                        asset.Count,
                        asset.PrincipalToReturn,
                        asset.ProfitToPay
                    ];
                }
            }
        }

        private static IEnumerable<object[]> BuildObligationRows(List<AssetObligation> obligations)
        {
            if (obligations == null)
                yield break;

            foreach (var obligation in obligations)
            {
                if (obligation == null)
                    continue;

                yield return
                [
                    Text(obligation.Asset),
                    obligation.PrincipalObligation,
                    obligation.ProfitObligation,
                    obligation.TotalObligation
                ];
            }
        }

        private static IEnumerable<object[]> BuildActiveContractRows(List<ActiveContractRow> contracts)
        {
            if (contracts == null)
                yield break;

            foreach (var contract in contracts)
            {
                if (contract == null)
                    continue;

                yield return
                [
                    Text(contract.WalletAddress),
                    Text(contract.StakeReference),
                    Text(contract.Asset),
                    contract.Principal,
                    contract.PlanType,
                    contract.StartMoment.ToString("u"),
                    contract.EndMoment.ToString("u"),
                    contract.ProfitReceived,
                    contract.RemainingProfitOwed,
                    contract.Status.ToString()
                ];
            }
        }

        private static IEnumerable<object[]> BuildWalletEnteredRows(List<WalletAssetEntered> entered)
        {
            if (entered == null)
                yield break;

            foreach (var asset in entered)
            {
                if (asset == null)
                    continue;

                yield return
                [
                    Text(asset.Asset),
                    asset.ContractCount,
                    asset.TotalEnteredPrincipal,
                    asset.TotalProfitReceived
                ];
            }
        }

        private static IEnumerable<object[]> BuildWalletRankRows(List<WalletAssetRank> ranking)
        {
            if (ranking == null)
                yield break;

            foreach (var rank in ranking)
            {
                if (rank == null)
                    continue;

                yield return
                [
                    Text(rank.Asset),
                    rank.Rank,
                    rank.TotalWalletsConsidered,
                    rank.IsTopN
                ];
            }
        }

        private static IEnumerable<object[]> BuildPlanDistributionRows(List<PlanDistribution> plans)
        {
            if (plans == null)
                yield break;

            foreach (var plan in plans)
            {
                if (plan == null)
                    continue;

                yield return
                [
                    plan.PlanType,
                    plan.ContractCount,
                    plan.ValueWeightedSharePercent
                ];
            }
        }

        private static IEnumerable<object[]> BuildPlanDistributionAssetRows(List<PlanDistribution> plans)
        {
            if (plans == null)
                yield break;

            foreach (var plan in plans)
            {
                if (plan == null || plan.ByAsset == null)
                    continue;

                foreach (var asset in plan.ByAsset)
                {
                    if (asset == null)
                        continue;

                    yield return
                    [
                        plan.PlanType,
                        Text(asset.Asset),
                        asset.OpenPrincipal
                    ];
                }
            }
        }

        private static IEnumerable<object[]> BuildHistoricalPerformanceRows(List<PeriodPerformance> periods)
        {
            if (periods == null)
                yield break;

            foreach (var period in periods)
            {
                if (period == null)
                    continue;

                var assets = new List<string>();
                var attracted = new Dictionary<string, decimal>();
                var profitPaid = new Dictionary<string, decimal>();

                CollectPeriodAssetAmounts(period.AttractedByAsset, assets, attracted);
                CollectPeriodAssetAmounts(period.ProfitPaidByAsset, assets, profitPaid);

                if (assets.Count == 0)
                {
                    yield return
                    [
                        Text(period.Label),
                        period.FromInclusive.ToString("u"),
                        period.ToExclusive.ToString("u"),
                        period.NewContractCount,
                        string.Empty,
                        0m,
                        0m,
                        period.FinishedContractCount
                    ];
                    continue;
                }

                foreach (var asset in assets)
                {
                    yield return
                    [
                        Text(period.Label),
                        period.FromInclusive.ToString("u"),
                        period.ToExclusive.ToString("u"),
                        period.NewContractCount,
                        Text(asset),
                        attracted.TryGetValue(asset, out var a) ? a : 0m,
                        profitPaid.TryGetValue(asset, out var p) ? p : 0m,
                        period.FinishedContractCount
                    ];
                }
            }
        }

        private static void CollectPeriodAssetAmounts(List<PeriodAssetAmount> source, List<string> assetOrder, Dictionary<string, decimal> amounts)
        {
            if (source == null)
                return;

            foreach (var entry in source)
            {
                if (entry == null)
                    continue;

                var key = Text(entry.Asset);
                if (!amounts.ContainsKey(key))
                {
                    amounts[key] = entry.Amount;
                    assetOrder.Add(key);
                }
                else
                {
                    amounts[key] = amounts[key] + entry.Amount;
                }
            }
        }

        private static IEnumerable<object[]> BuildAssetObligationShareRows(List<AssetObligationShare> byAsset)
        {
            if (byAsset == null)
                yield break;

            foreach (var asset in byAsset)
            {
                if (asset == null)
                    continue;

                yield return
                [
                    Text(asset.Asset),
                    asset.NativeOpenObligation,
                    asset.ValuedObligationUsd,
                    asset.SharePercent
                ];
            }
        }

        private static IEnumerable<object[]> BuildPlanObligationShareRows(List<PlanObligationShare> plans)
        {
            if (plans == null)
                yield break;

            foreach (var plan in plans)
            {
                if (plan == null)
                    continue;

                yield return
                [
                    plan.PlanType,
                    plan.ContractCount,
                    plan.ValuedObligationUsd,
                    plan.SharePercent
                ];
            }
        }

        private static IEnumerable<object[]> BuildPlanObligationAssetRows(List<PlanObligationShare> plans)
        {
            if (plans == null)
                yield break;

            foreach (var plan in plans)
            {
                if (plan == null || plan.ByAsset == null)
                    continue;

                foreach (var asset in plan.ByAsset)
                {
                    if (asset == null)
                        continue;

                    yield return
                    [
                        plan.PlanType,
                        Text(asset.Asset),
                        asset.NativeOpenObligation
                    ];
                }
            }
        }

        #endregion

        #region Cells

        private static void WriteCell(IXLCell cell, object value)
        {
            if (value == null)
            {
                cell.Value = string.Empty;
                return;
            }

            switch (value)
            {
                case decimal decimalValue:
                    cell.Value = decimalValue;
                    break;
                case int intValue:
                    cell.Value = intValue;
                    break;
                case long longValue:
                    cell.Value = longValue;
                    break;
                case bool boolValue:
                    cell.Value = boolValue;
                    break;
                case string stringValue:
                    cell.Value = stringValue;
                    break;
                default:
                    cell.Value = value.ToString();
                    break;
            }
        }

        private static string Text(string value) => value ?? string.Empty;

        #endregion
    }
}
