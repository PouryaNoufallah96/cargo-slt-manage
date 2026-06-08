using SLT.Domain.Collections;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Treasury.Calculators
{
    public class StakeReportingStatusResolver : IStakeReportingStatusResolver, IScopedDependency
    {
        public TreasuryReportingStatus Resolve(StakeState state, DateTime endMoment, DateTime reportAsOfMoment)
        {
            if (state == StakeState.Finished)
                return TreasuryReportingStatus.Finished;

            // Active only here — NotRegistered is filtered out upstream.
            if (endMoment > reportAsOfMoment)
                return TreasuryReportingStatus.WithinTerm;

            return TreasuryReportingStatus.MaturedUnredeemed;
        }
    }
}
