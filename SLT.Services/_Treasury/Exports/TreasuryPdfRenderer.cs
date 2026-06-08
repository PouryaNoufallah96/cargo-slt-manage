using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using SLT.Services._Treasury.DTOs.Results;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Treasury.Exports
{
    public class TreasuryPdfRenderer : ITreasuryPdfRenderer, IScopedDependency
    {
        private const string DefaultFontName = "TreasuryDefault";

        private static readonly bool _fontsInitialized = InitFonts();

        private static bool InitFonts()
        {
            if (GlobalFontSettings.FontResolver == null)
                GlobalFontSettings.FontResolver = new TreasuryFontResolver();
            return true;
        }

        public byte[] RenderOverview(TreasuryOverviewResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new TreasuryOverviewResult();
            if (meta == null)
                meta = new ExportMetadata();

            var doc = NewDocument();
            var section = NewLandscapeSection(doc);

            WriteHeaderBlock(section, meta);

            WriteSectionTitle(section, "Per-Asset Obligations");
            WriteTable(
                section,
                ["Asset", "Open Principal", "Remaining Profit Owed", "Open Obligation"],
                BuildPerAssetRows(model.PerAsset));

            WriteSectionTitle(section, "Active Contracts");
            WriteTable(
                section,
                ["Within Term", "Matured Unredeemed", "Total"],
                [
                    [
                        Num(model.ActiveContracts == null ? 0 : model.ActiveContracts.WithinTerm),
                        Num(model.ActiveContracts == null ? 0 : model.ActiveContracts.MaturedUnredeemed),
                        Num(model.ActiveContracts == null ? 0 : model.ActiveContracts.Total)
                    ]
                ]);

            WriteSectionTitle(section, "Active Users");
            WriteLabelValue(section, "Distinct active wallets", Num(model.ActiveUsers));

            WriteSectionTitle(section, "Nearest Maturities");
            WriteTable(
                section,
                ["Stake Reference", "Asset", "Wallet Address", "End Moment (UTC)", "Remaining Principal"],
                BuildMaturityRows(model.NearestMaturities));

            WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);

            return Render(doc);
        }

        public byte[] RenderMaturityCalendar(MaturityCalendarResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new MaturityCalendarResult();
            if (meta == null)
                meta = new ExportMetadata();

            var doc = NewDocument();
            var section = NewLandscapeSection(doc);

            WriteHeaderBlock(section, meta);

            WriteSectionTitle(section, "Resolved Window");
            WriteLabelValue(section, "From (inclusive, UTC)", Moment(model.FromInclusive));
            WriteLabelValue(section, "To (exclusive, UTC)", Moment(model.ToExclusive));

            WriteMaturityBucket(section, "Due Now / Overdue", model.DueNowOverdue);
            WriteMaturityBucket(section, "Future Maturing", model.FutureMaturing);

            WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);

            return Render(doc);
        }

        public byte[] RenderFutureObligations(FutureObligationsResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new FutureObligationsResult();
            if (meta == null)
                meta = new ExportMetadata();

            var doc = NewDocument();
            var section = NewLandscapeSection(doc);

            WriteHeaderBlock(section, meta);

            WriteSectionTitle(section, "Resolved Window");
            WriteLabelValue(section, "From (inclusive, UTC)", Moment(model.FromInclusive));
            WriteLabelValue(section, "To (exclusive, UTC)", Moment(model.ToExclusive));

            WriteObligationBucket(section, "Due Now / Overdue", model.DueNowOverdue);
            WriteObligationBucket(section, "Future Within Horizon", model.FutureWithinHorizon);
            WriteObligationBucket(section, "Total Required", model.TotalRequired);

            WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);

            return Render(doc);
        }

        public byte[] RenderActiveContracts(ActiveContractsResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new ActiveContractsResult();
            if (meta == null)
                meta = new ExportMetadata();

            var doc = NewDocument();
            var section = NewLandscapeSection(doc);

            WriteHeaderBlock(section, meta);

            WriteSectionTitle(section, "Totals");
            WriteLabelValue(section, "Total contracts (all pages)", Num(model.TotalCount));
            WriteLabelValue(section, "Page count", Num(model.PageCount));

            WriteSectionTitle(section, "Contracts (this page)");
            WriteTable(
                section,
                ["Wallet Address", "Stake Reference", "Asset", "Principal", "Plan (months)", "Start (UTC)", "End (UTC)", "Profit Received", "Remaining Profit Owed", "Status"],
                BuildActiveContractRows(model.Data));

            WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);

            return Render(doc);
        }

        public byte[] RenderWalletAnalysis(WalletAnalysisResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new WalletAnalysisResult();
            if (meta == null)
                meta = new ExportMetadata();

            var doc = NewDocument();
            var section = NewLandscapeSection(doc);

            WriteHeaderBlock(section, meta);

            if (model.Mode == WalletAnalysisMode.Leaderboard)
            {
                foreach (var asset in model.TopWallets ?? [])
                {
                    WriteSectionTitle(section, "Top Wallets — " + Text(asset == null ? null : asset.Asset));
                    WriteTable(
                        section,
                        ["Rank", "Wallet Address", "Entered Principal", "Contract Count"],
                        BuildLeaderboardRows(asset == null ? null : asset.Rows));
                }

                WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);
                return Render(doc);
            }

            WriteSectionTitle(section, "Wallet");
            WriteLabelValue(section, "Wallet", Text(model.Wallet));
            WriteLabelValue(section, "Report as-of (UTC)", Moment(model.ReportAsOfMoment));
            WriteLabelValue(section, "Registered contracts", Num(model.RegisteredContractCount));

            WriteSectionTitle(section, "Counts");
            WriteTable(
                section,
                ["Within Term", "Matured Unredeemed", "Active Open", "Finished"],
                [
                    [
                        Num(model.WithinTermCount),
                        Num(model.MaturedUnredeemedCount),
                        Num(model.ActiveOpenCount),
                        Num(model.FinishedCount)
                    ]
                ]);

            WriteSectionTitle(section, "Entered By Asset");
            WriteTable(
                section,
                ["Asset", "Contract Count", "Total Entered Principal", "Total Profit Received"],
                BuildWalletEnteredRows(model.EnteredByAsset));

            WriteSectionTitle(section, "Nearest Maturity");
            if (model.NearestMaturity == null)
            {
                section.AddParagraph("None open");
            }
            else
            {
                WriteTable(
                    section,
                    ["Stake Reference", "Asset", "End Moment (UTC)", "Remaining Principal"],
                    [
                        [
                            Text(model.NearestMaturity.StakeReference),
                            Text(model.NearestMaturity.Asset),
                            Moment(model.NearestMaturity.EndMoment),
                            Num(model.NearestMaturity.RemainingPrincipal)
                        ]
                    ]);
            }

            WriteSectionTitle(section, "Whale Ranking");
            WriteTable(
                section,
                ["Asset", "Rank", "Wallets Considered", "Is Top-N"],
                BuildWalletRankRows(model.Ranking));

            WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);

            return Render(doc);
        }

        public byte[] RenderContractDistribution(ContractDistributionResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new ContractDistributionResult();
            if (meta == null)
                meta = new ExportMetadata();

            var doc = NewDocument();
            var section = NewLandscapeSection(doc);

            WriteHeaderBlock(section, meta);

            WriteSectionTitle(section, "Valuation");
            WriteLabelValue(section, "Method", Text(model.ValuationMethod.ToString()));
            WriteLabelValue(section, "Valuation complete", Bool(model.ValuationComplete));

            WriteSectionTitle(section, "Plan Distribution");
            WriteTable(
                section,
                ["Plan (months)", "Contract Count", "Value-Weighted Share %"],
                BuildPlanDistributionRows(model.Plans));

            WriteSectionTitle(section, "Per-Plan Open Principal By Asset");
            WriteTable(
                section,
                ["Plan (months)", "Asset", "Open Principal"],
                BuildPlanDistributionAssetRows(model.Plans));

            WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);

            return Render(doc);
        }

        public byte[] RenderHistoricalPerformance(HistoricalPerformanceResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new HistoricalPerformanceResult();
            if (meta == null)
                meta = new ExportMetadata();

            var doc = NewDocument();
            var section = NewLandscapeSection(doc);

            WriteHeaderBlock(section, meta);

            WriteSectionTitle(section, "Totals");
            WriteLabelValue(section, "Total periods (all pages)", Num(model.TotalCount));
            WriteLabelValue(section, "Page count", Num(model.PageCount));

            WriteSectionTitle(section, "Periods (this page)");
            WriteTable(
                section,
                ["Period", "From (UTC)", "To (UTC)", "New Contracts", "Asset", "Attracted Capital", "Profit Paid", "Finished Contracts"],
                BuildHistoricalPerformanceRows(model.Data));

            WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);

            return Render(doc);
        }

        public byte[] RenderAssetComposition(AssetCompositionResult model, ExportMetadata meta)
        {
            if (model == null)
                model = new AssetCompositionResult();
            if (meta == null)
                meta = new ExportMetadata();

            var doc = NewDocument();
            var section = NewLandscapeSection(doc);

            WriteHeaderBlock(section, meta);

            WriteSectionTitle(section, "Valuation");
            WriteLabelValue(section, "Method", Text(model.ValuationMethod.ToString()));
            WriteLabelValue(section, "Valuation complete", Bool(model.ValuationComplete));

            WriteSectionTitle(section, "Composition By Asset");
            WriteTable(
                section,
                ["Asset", "Native Open Obligation", "Valued Obligation (USD)", "Share %"],
                BuildAssetObligationShareRows(model.ByAsset));

            WriteSectionTitle(section, "Plan Composition");
            WriteTable(
                section,
                ["Plan (months)", "Contract Count", "Valued Obligation (USD)", "Share %"],
                BuildPlanObligationShareRows(model.Plans));

            WriteSectionTitle(section, "Per-Plan Open Obligation By Asset");
            WriteTable(
                section,
                ["Plan (months)", "Asset", "Native Open Obligation"],
                BuildPlanObligationAssetRows(model.Plans));

            WriteSummaryAndWarnings(section, model.SummaryText, model.Warnings);

            return Render(doc);
        }

        #region Document

        private static Document NewDocument()
        {
            _ = _fontsInitialized;

            var doc = new Document();
            doc.Styles["Normal"].Font.Name = DefaultFontName;
            doc.Styles["Normal"].Font.Size = 9;

            return doc;
        }

        private static Section NewLandscapeSection(Document doc)
        {
            var section = doc.AddSection();

            var pageSetup = doc.DefaultPageSetup.Clone();
            pageSetup.Orientation = Orientation.Landscape;
            pageSetup.LeftMargin = Unit.FromCentimeter(1.5);
            pageSetup.RightMargin = Unit.FromCentimeter(1.5);
            pageSetup.TopMargin = Unit.FromCentimeter(1.5);
            pageSetup.BottomMargin = Unit.FromCentimeter(1.5);
            section.PageSetup = pageSetup;

            return section;
        }

        private static byte[] Render(Document doc)
        {
            var renderer = new PdfDocumentRenderer { Document = doc };
            renderer.RenderDocument();

            using (var stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream, false);
                return stream.ToArray();
            }
        }

        #endregion

        #region Layout

        private static void WriteHeaderBlock(Section section, ExportMetadata meta)
        {
            var title = section.AddParagraph(Text(meta.ReportTitle));
            title.Format.Font.Bold = true;
            title.Format.Font.Size = 16;
            title.Format.SpaceAfter = Unit.FromPoint(6);

            section.AddParagraph("Generated (UTC): " + meta.GeneratedAtUtc.ToString("u"));
            section.AddParagraph("Filters: " + Text(meta.AppliedFilters));

            if (!string.IsNullOrEmpty(meta.ValuationNote))
                section.AddParagraph("Valuation: " + Text(meta.ValuationNote));
        }

        private static void WriteSectionTitle(Section section, string titleText)
        {
            var heading = section.AddParagraph(Text(titleText));
            heading.Format.Font.Bold = true;
            heading.Format.Font.Size = 11;
            heading.Format.SpaceBefore = Unit.FromPoint(10);
            heading.Format.SpaceAfter = Unit.FromPoint(2);
        }

        private static void WriteLabelValue(Section section, string label, string value)
        {
            var paragraph = section.AddParagraph();
            var labelText = paragraph.AddFormattedText(Text(label) + ": ");
            labelText.Bold = true;
            paragraph.AddText(Text(value));
        }

        private static void WriteTable(Section section, string[] headers, IEnumerable<string[]> rows)
        {
            var columnCount = headers == null ? 0 : headers.Length;
            if (columnCount == 0)
                return;

            var table = section.AddTable();
            table.Borders.Width = 0.5;
            table.Format.SpaceAfter = Unit.FromPoint(4);

            var columnWidth = Unit.FromCentimeter(25.7 / columnCount);
            for (var col = 0; col < columnCount; col++)
                table.AddColumn(columnWidth);

            var headerRow = table.AddRow();
            for (var col = 0; col < columnCount; col++)
            {
                var paragraph = headerRow.Cells[col].AddParagraph(Text(headers[col]));
                paragraph.Format.Font.Bold = true;
            }

            foreach (var dataRow in rows)
            {
                if (dataRow == null)
                    continue;

                var row = table.AddRow();
                for (var col = 0; col < columnCount; col++)
                {
                    var value = col < dataRow.Length ? dataRow[col] : string.Empty;
                    row.Cells[col].AddParagraph(Text(value));
                }
            }
        }

        private static void WriteSummaryAndWarnings(Section section, string summaryText, List<string> warnings)
        {
            WriteSectionTitle(section, "Summary");
            section.AddParagraph(Text(summaryText));

            WriteSectionTitle(section, "Warnings");
            if (warnings == null || warnings.Count == 0)
            {
                section.AddParagraph("None");
                return;
            }

            foreach (var warning in warnings)
                section.AddParagraph(Text(warning));
        }

        private static void WriteMaturityBucket(Section section, string title, MaturityCalendarBucket bucket)
        {
            WriteSectionTitle(section, title);

            WriteTable(
                section,
                ["Asset", "Count", "Principal To Return", "Profit To Pay"],
                BuildMaturityAssetRows(bucket == null ? null : bucket.PerAsset));

            WriteTable(
                section,
                ["Plan (months)", "Asset", "Count", "Principal To Return", "Profit To Pay"],
                BuildMaturityPlanRows(bucket == null ? null : bucket.ByPlan));
        }

        private static void WriteObligationBucket(Section section, string title, List<AssetObligation> obligations)
        {
            WriteSectionTitle(section, title);
            WriteTable(
                section,
                ["Asset", "Principal Obligation", "Profit Obligation", "Total Obligation"],
                BuildObligationRows(obligations));
        }

        #endregion

        #region Rows

        private static IEnumerable<string[]> BuildPerAssetRows(List<TreasuryAssetObligationSummary> perAsset)
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
                    Num(asset.OpenPrincipal),
                    Num(asset.RemainingProfitOwed),
                    Num(asset.OpenObligation)
                ];
            }
        }

        private static IEnumerable<string[]> BuildMaturityRows(List<TreasuryUpcomingMaturity> maturities)
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
                    Moment(maturity.EndMoment),
                    Num(maturity.RemainingPrincipal)
                ];
            }
        }

        private static IEnumerable<string[]> BuildMaturityAssetRows(List<MaturityAssetTotals> perAsset)
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
                    Num(asset.Count),
                    Num(asset.PrincipalToReturn),
                    Num(asset.ProfitToPay)
                ];
            }
        }

        private static IEnumerable<string[]> BuildMaturityPlanRows(List<MaturityPlanBreakdown> byPlan)
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
                        Num(plan.PlanType),
                        Text(asset.Asset),
                        Num(asset.Count),
                        Num(asset.PrincipalToReturn),
                        Num(asset.ProfitToPay)
                    ];
                }
            }
        }

        private static IEnumerable<string[]> BuildObligationRows(List<AssetObligation> obligations)
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
                    Num(obligation.PrincipalObligation),
                    Num(obligation.ProfitObligation),
                    Num(obligation.TotalObligation)
                ];
            }
        }

        private static IEnumerable<string[]> BuildActiveContractRows(List<ActiveContractRow> contracts)
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
                    Num(contract.Principal),
                    Num(contract.PlanType),
                    Moment(contract.StartMoment),
                    Moment(contract.EndMoment),
                    Num(contract.ProfitReceived),
                    Num(contract.RemainingProfitOwed),
                    Text(contract.Status.ToString())
                ];
            }
        }

        private static IEnumerable<string[]> BuildWalletEnteredRows(List<WalletAssetEntered> entered)
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
                    Num(asset.ContractCount),
                    Num(asset.TotalEnteredPrincipal),
                    Num(asset.TotalProfitReceived)
                ];
            }
        }

        private static IEnumerable<string[]> BuildWalletRankRows(List<WalletAssetRank> ranking)
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
                    Num(rank.Rank),
                    Num(rank.TotalWalletsConsidered),
                    Bool(rank.IsTopN)
                ];
            }
        }

        private static IEnumerable<string[]> BuildLeaderboardRows(List<WalletLeaderboardRow> rows)
        {
            if (rows == null)
                yield break;

            foreach (var row in rows)
            {
                if (row == null)
                    continue;

                yield return
                [
                    Num(row.Rank),
                    Text(row.WalletAddress),
                    Num(row.EnteredPrincipal),
                    Num(row.ContractCount)
                ];
            }
        }

        private static IEnumerable<string[]> BuildPlanDistributionRows(List<PlanDistribution> plans)
        {
            if (plans == null)
                yield break;

            foreach (var plan in plans)
            {
                if (plan == null)
                    continue;

                yield return
                [
                    Num(plan.PlanType),
                    Num(plan.ContractCount),
                    Num(plan.ValueWeightedSharePercent)
                ];
            }
        }

        private static IEnumerable<string[]> BuildPlanDistributionAssetRows(List<PlanDistribution> plans)
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
                        Num(plan.PlanType),
                        Text(asset.Asset),
                        Num(asset.OpenPrincipal)
                    ];
                }
            }
        }

        private static IEnumerable<string[]> BuildHistoricalPerformanceRows(List<PeriodPerformance> periods)
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
                        Moment(period.FromInclusive),
                        Moment(period.ToExclusive),
                        Num(period.NewContractCount),
                        string.Empty,
                        Num(0m),
                        Num(0m),
                        Num(period.FinishedContractCount)
                    ];
                    continue;
                }

                foreach (var asset in assets)
                {
                    yield return
                    [
                        Text(period.Label),
                        Moment(period.FromInclusive),
                        Moment(period.ToExclusive),
                        Num(period.NewContractCount),
                        Text(asset),
                        Num(attracted.TryGetValue(asset, out var a) ? a : 0m),
                        Num(profitPaid.TryGetValue(asset, out var p) ? p : 0m),
                        Num(period.FinishedContractCount)
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

        private static IEnumerable<string[]> BuildAssetObligationShareRows(List<AssetObligationShare> byAsset)
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
                    Num(asset.NativeOpenObligation),
                    Num(asset.ValuedObligationUsd),
                    Num(asset.SharePercent)
                ];
            }
        }

        private static IEnumerable<string[]> BuildPlanObligationShareRows(List<PlanObligationShare> plans)
        {
            if (plans == null)
                yield break;

            foreach (var plan in plans)
            {
                if (plan == null)
                    continue;

                yield return
                [
                    Num(plan.PlanType),
                    Num(plan.ContractCount),
                    Num(plan.ValuedObligationUsd),
                    Num(plan.SharePercent)
                ];
            }
        }

        private static IEnumerable<string[]> BuildPlanObligationAssetRows(List<PlanObligationShare> plans)
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
                        Num(plan.PlanType),
                        Text(asset.Asset),
                        Num(asset.NativeOpenObligation)
                    ];
                }
            }
        }

        #endregion

        #region Helpers

        private static string Text(string value) => value ?? string.Empty;

        private static string Num(decimal value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static string Num(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static string Bool(bool value) => value ? "True" : "False";

        private static string Moment(DateTime value) => value.ToString("u");

        #endregion
    }

    internal sealed class TreasuryFontResolver : IFontResolver
    {
        private static readonly byte[] _regular = Load();

        public byte[] GetFont(string faceName) => _regular;

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            => new FontResolverInfo("TreasuryDefault");

        private static byte[] Load()
        {
            string[] candidates =
            {
                "/System/Library/Fonts/Supplemental/Arial.ttf", "/Library/Fonts/Arial.ttf",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                "/usr/share/fonts/dejavu/DejaVuSans.ttf",
            };

            foreach (var path in candidates)
                if (System.IO.File.Exists(path))
                    return System.IO.File.ReadAllBytes(path);

            throw new System.IO.FileNotFoundException(
                "No system TrueType font found for PDF rendering (install a font, e.g. dejavu/liberation, in the deploy image).");
        }
    }
}
