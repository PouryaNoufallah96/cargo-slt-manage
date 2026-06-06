using SLT.Services._Treasury.DTOs.Results;
using SLT.Services._Treasury.DTOs.Updates;

namespace SLT.Services._Treasury
{
    public interface ITreasuryReportService
    {

        // dashboard
        Task<TreasuryOverviewResult> GetOverviewAsync();

        // plan durations (filter lookup)
        Task<PlanTypeResult> GetPlanTypesAsync();

        // maturity calendar
        Task<MaturityCalendarResult> GetMaturityCalendarAsync(MaturityCalendarUpdate update);

        // active contracts
        Task<ActiveContractsResult> GetActiveContractsAsync(ActiveContractsUpdate update);

        // wallet
        Task<WalletAnalysisResult> GetWalletAnalysisAsync(WalletAnalysisUpdate update);

        // distribution
        Task<ContractDistributionResult> GetContractDistributionAsync(ContractDistributionUpdate update);

        // historical
        Task<HistoricalPerformanceResult> GetHistoricalPerformanceAsync(HistoricalPerformanceUpdate update);

        // obligations
        Task<FutureObligationsResult> GetFutureObligationsAsync(FutureObligationsUpdate update);

        // composition
        Task<AssetCompositionResult> GetAssetCompositionAsync(AssetCompositionUpdate update);
    }
}
