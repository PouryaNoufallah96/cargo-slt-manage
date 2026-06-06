using SLT.Domain.Collections;

namespace SLT.Services._Treasury.Calculators
{
    public interface IStakeReportingStatusResolver
    {
        TreasuryReportingStatus Resolve(StakeState state, DateTime endMoment, DateTime reportAsOfMoment);
    }

    // Active stakes only — Finished from stored State; Active split by EndMoment vs report time.
    public enum TreasuryReportingStatus { WithinTerm, MaturedUnredeemed, Finished }
}
