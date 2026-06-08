using SLT.Services._Treasury.DTOs.Results;

namespace SLT.Services._Treasury.Exports
{
    public interface ITreasuryExcelRenderer
    {
        byte[] RenderOverview(TreasuryOverviewResult model, ExportMetadata meta);

        byte[] RenderMaturityCalendar(MaturityCalendarResult model, ExportMetadata meta);

        byte[] RenderFutureObligations(FutureObligationsResult model, ExportMetadata meta);

        byte[] RenderActiveContracts(ActiveContractsResult model, ExportMetadata meta);

        byte[] RenderWalletAnalysis(WalletAnalysisResult model, ExportMetadata meta);

        byte[] RenderContractDistribution(ContractDistributionResult model, ExportMetadata meta);

        byte[] RenderHistoricalPerformance(HistoricalPerformanceResult model, ExportMetadata meta);

        byte[] RenderAssetComposition(AssetCompositionResult model, ExportMetadata meta);
    }
}
