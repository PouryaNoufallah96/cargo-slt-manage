using SLT.Services._Treasury.DTOs.Results;

namespace SLT.Services._Treasury.Calculators
{
    public interface ISummaryTextBuilder
    {
        string BuildOverviewSummary(TreasuryOverviewResult dashboard);

        string BuildMaturityCalendarSummary(MaturityCalendarResult calendar);

        string BuildActiveContractsSummary(ActiveContractsResult result);

        string BuildWalletAnalysisSummary(WalletAnalysisResult result);

        string BuildContractDistributionSummary(ContractDistributionResult result);

        string BuildHistoricalPerformanceSummary(HistoricalPerformanceResult result);

        string BuildFutureObligationsSummary(FutureObligationsResult result);

        string BuildAssetCompositionSummary(AssetCompositionResult result);
    }
}
