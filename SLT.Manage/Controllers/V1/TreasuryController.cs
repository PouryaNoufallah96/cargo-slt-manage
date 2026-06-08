using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SLT.Services._Treasury;
using SLT.Services._Treasury.DTOs.Results;
using SLT.Services._Treasury.DTOs.Updates;
using SLT.Services._Treasury.Exports;
using Swashbuckle.AspNetCore.Annotations;
using Utilities.Api;
using Utilities.Attributes;
using Utilities.Filters;
using Utilities.Permissions;

namespace SLT.Manage.Controllers.V1
{

    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TreasuryController(ITreasuryReportService _treasuryReportService, ITreasuryExcelRenderer _excelRenderer, ITreasuryPdfRenderer _pdfRenderer) : ApiBaseController
    {

        #region Overview

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get treasury overview",
            Description = "Returns the treasury overview with per-asset obligations, active contract counts and upcoming maturities.",
            Tags = ["Treasury - Overview"]
        )]
        public async Task<TreasuryOverviewResult> Overview([FromBody] OverviewUpdate update)
            => await _treasuryReportService.GetOverviewAsync(update);

        #endregion

        #region Plan Type

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get available plan types",
            Description = "Returns the distinct plan lengths (in months) present in the data, ascending — use to populate the planType filter.",
            Tags = ["Treasury - Plan Type"]
        )]
        public async Task<PlanTypeResult> PlanType()
            => await _treasuryReportService.GetPlanTypesAsync();

        #endregion

        #region Overview Export

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export the treasury overview as Excel (.xlsx)",
            Description = "Exports the treasury overview as an Excel file.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryOverviewExcel([FromBody] OverviewUpdate update)
        {
            var model = await _treasuryReportService.GetOverviewAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Overview",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _excelRenderer.RenderOverview(model, meta);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "treasury-overview.xlsx");
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export the treasury overview as PDF (.pdf)",
            Description = "Exports the treasury overview as a PDF file.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryOverviewPdf([FromBody] OverviewUpdate update)
        {
            var model = await _treasuryReportService.GetOverviewAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Overview",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _pdfRenderer.RenderOverview(model, meta);
            return File(bytes, "application/pdf", "treasury-overview.pdf");
        }

        #endregion

        #region Maturity Calendar

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get treasury contract maturity calendar",
            Description = "Returns open contracts maturing in the selected horizon, grouped by asset and plan.",
            Tags = ["Treasury - Maturity Calendar"]
        )]
        public async Task<MaturityCalendarResult> GetTreasuryMaturityCalendar([FromBody] MaturityCalendarUpdate update)
            => await _treasuryReportService.GetMaturityCalendarAsync(update);

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export the treasury maturity calendar as Excel (.xlsx)",
            Description = "Exports the maturity calendar report as Excel.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryMaturityCalendarExcel([FromBody] MaturityCalendarUpdate update)
        {
            var model = await _treasuryReportService.GetMaturityCalendarAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Contract Maturity Calendar",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _excelRenderer.RenderMaturityCalendar(model, meta);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "treasury-maturity-calendar.xlsx");
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export the treasury maturity calendar as PDF (.pdf)",
            Description = "Exports the maturity calendar report as PDF.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryMaturityCalendarPdf([FromBody] MaturityCalendarUpdate update)
        {
            var model = await _treasuryReportService.GetMaturityCalendarAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Contract Maturity Calendar",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _pdfRenderer.RenderMaturityCalendar(model, meta);
            return File(bytes, "application/pdf", "treasury-maturity-calendar.pdf");
        }

        #endregion

        #region Active Contracts

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get active treasury contracts",
            Description = "Returns a paginated list of open treasury contracts with filters.",
            Tags = ["Treasury - Active Contracts"]
        )]
        public async Task<ActiveContractsResult> GetTreasuryActiveContracts([FromBody] ActiveContractsUpdate update)
            => await _treasuryReportService.GetActiveContractsAsync(update);

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export active treasury contracts as Excel (.xlsx)",
            Description = "Exports the active contracts page as Excel.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryActiveContractsExcel([FromBody] ActiveContractsUpdate update)
        {
            var model = await _treasuryReportService.GetActiveContractsAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Active Contracts",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _excelRenderer.RenderActiveContracts(model, meta);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "treasury-active-contracts.xlsx");
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export active treasury contracts as PDF (.pdf)",
            Description = "Exports the active contracts page as PDF.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryActiveContractsPdf([FromBody] ActiveContractsUpdate update)
        {
            var model = await _treasuryReportService.GetActiveContractsAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Active Contracts",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _pdfRenderer.RenderActiveContracts(model, meta);
            return File(bytes, "application/pdf", "treasury-active-contracts.pdf");
        }

        #endregion

        #region Wallet Analysis

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 100)]
        [SwaggerOperation(
            Summary = "Get wallet treasury analysis",
            Description = "Returns per-wallet treasury activity and optional whale ranking.",
            Tags = ["Treasury - Wallet Analysis"]
        )]
        public async Task<WalletAnalysisResult> GetTreasuryWalletAnalysis([FromBody] WalletAnalysisUpdate update)
            => await _treasuryReportService.GetWalletAnalysisAsync(update);

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export wallet treasury analysis as Excel (.xlsx)",
            Description = "Exports wallet analysis as Excel.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryWalletAnalysisExcel([FromBody] WalletAnalysisUpdate update)
        {
            var model = await _treasuryReportService.GetWalletAnalysisAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Wallet Analysis",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _excelRenderer.RenderWalletAnalysis(model, meta);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "treasury-wallet-analysis.xlsx");
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export wallet treasury analysis as PDF (.pdf)",
            Description = "Exports wallet analysis as PDF.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryWalletAnalysisPdf([FromBody] WalletAnalysisUpdate update)
        {
            var model = await _treasuryReportService.GetWalletAnalysisAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Wallet Analysis",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _pdfRenderer.RenderWalletAnalysis(model, meta);
            return File(bytes, "application/pdf", "treasury-wallet-analysis.pdf");
        }

        #endregion

        #region Contract Distribution

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get treasury contract distribution",
            Description = "Returns how open capital is spread across plan durations.",
            Tags = ["Treasury - Contract Distribution"]
        )]
        public async Task<ContractDistributionResult> GetTreasuryContractDistribution([FromBody] ContractDistributionUpdate update)
            => await _treasuryReportService.GetContractDistributionAsync(update);

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export treasury contract distribution as Excel (.xlsx)",
            Description = "Exports contract distribution as Excel.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryContractDistributionExcel([FromBody] ContractDistributionUpdate update)
        {
            var model = await _treasuryReportService.GetContractDistributionAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Contract Distribution",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = model.ValuationNote
            };
            var bytes = _excelRenderer.RenderContractDistribution(model, meta);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "treasury-contract-distribution.xlsx");
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export treasury contract distribution as PDF (.pdf)",
            Description = "Exports contract distribution as PDF.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryContractDistributionPdf([FromBody] ContractDistributionUpdate update)
        {
            var model = await _treasuryReportService.GetContractDistributionAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Contract Distribution",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = model.ValuationNote
            };
            var bytes = _pdfRenderer.RenderContractDistribution(model, meta);
            return File(bytes, "application/pdf", "treasury-contract-distribution.pdf");
        }

        #endregion

        #region Historical Performance

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get treasury historical performance",
            Description = "Returns historical treasury inflows and payouts grouped by period.",
            Tags = ["Treasury - Historical Performance"]
        )]
        public async Task<HistoricalPerformanceResult> GetTreasuryHistoricalPerformance([FromBody] HistoricalPerformanceUpdate update)
            => await _treasuryReportService.GetHistoricalPerformanceAsync(update);

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export treasury historical performance as Excel (.xlsx)",
            Description = "Exports historical performance as Excel.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryHistoricalPerformanceExcel([FromBody] HistoricalPerformanceUpdate update)
        {
            var model = await _treasuryReportService.GetHistoricalPerformanceAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Historical Performance",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _excelRenderer.RenderHistoricalPerformance(model, meta);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "treasury-historical-performance.xlsx");
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export treasury historical performance as PDF (.pdf)",
            Description = "Exports historical performance as PDF.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryHistoricalPerformancePdf([FromBody] HistoricalPerformanceUpdate update)
        {
            var model = await _treasuryReportService.GetHistoricalPerformanceAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Historical Performance",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _pdfRenderer.RenderHistoricalPerformance(model, meta);
            return File(bytes, "application/pdf", "treasury-historical-performance.pdf");
        }

        #endregion

        #region Future Obligations

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get treasury future obligations",
            Description = "Returns principal and profit obligations due within the selected horizon.",
            Tags = ["Treasury - Future Obligations"]
        )]
        public async Task<FutureObligationsResult> GetTreasuryFutureObligations([FromBody] FutureObligationsUpdate update)
            => await _treasuryReportService.GetFutureObligationsAsync(update);

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export treasury future obligations as Excel (.xlsx)",
            Description = "Exports future obligations as Excel.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryFutureObligationsExcel([FromBody] FutureObligationsUpdate update)
        {
            var model = await _treasuryReportService.GetFutureObligationsAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Future Obligations",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _excelRenderer.RenderFutureObligations(model, meta);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "treasury-future-obligations.xlsx");
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export treasury future obligations as PDF (.pdf)",
            Description = "Exports future obligations as PDF.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryFutureObligationsPdf([FromBody] FutureObligationsUpdate update)
        {
            var model = await _treasuryReportService.GetFutureObligationsAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Future Obligations",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = null
            };
            var bytes = _pdfRenderer.RenderFutureObligations(model, meta);
            return File(bytes, "application/pdf", "treasury-future-obligations.pdf");
        }

        #endregion

        #region Asset Composition

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get treasury asset composition",
            Description = "Returns obligation concentration by asset and plan.",
            Tags = ["Treasury - Asset Composition"]
        )]
        public async Task<AssetCompositionResult> GetTreasuryAssetComposition([FromBody] AssetCompositionUpdate update)
            => await _treasuryReportService.GetAssetCompositionAsync(update);

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export treasury asset composition as Excel (.xlsx)",
            Description = "Exports asset composition as Excel.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryAssetCompositionExcel([FromBody] AssetCompositionUpdate update)
        {
            var model = await _treasuryReportService.GetAssetCompositionAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Asset Composition",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = model.ValuationNote
            };
            var bytes = _excelRenderer.RenderAssetComposition(model, meta);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "treasury-asset-composition.xlsx");
        }

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Export treasury asset composition as PDF (.pdf)",
            Description = "Exports asset composition as PDF.",
            Tags = ["Treasury - Export"]
        )]
        public async Task<IActionResult> ExportTreasuryAssetCompositionPdf([FromBody] AssetCompositionUpdate update)
        {
            var model = await _treasuryReportService.GetAssetCompositionAsync(update);
            var meta = new ExportMetadata
            {
                ReportTitle = "Treasury Asset Composition",
                AppliedFilters = ExportFilterText.For(update),
                GeneratedAtUtc = DateTime.UtcNow,
                ValuationNote = model.ValuationNote
            };
            var bytes = _pdfRenderer.RenderAssetComposition(model, meta);
            return File(bytes, "application/pdf", "treasury-asset-composition.pdf");
        }

        #endregion
    }


}
