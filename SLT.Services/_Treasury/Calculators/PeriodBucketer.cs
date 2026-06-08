using System.Globalization;
using Utilities.Exceptions.Common;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Treasury.Calculators
{
    public class PeriodBucketer : IPeriodBucketer, IScopedDependency
    {
        public DateWindow ResolveMaturityWindow(MaturityHorizon horizon, DateTime? customFrom, DateTime? customTo, DateTime reportAsOfMoment)
        {
            // Both custom bounds present → use them, even without Horizon=Custom (the date picker omits Horizon).
            if (customFrom.HasValue && customTo.HasValue)
            {
                if (customFrom.Value >= customTo.Value)
                    throw new BadRequestException("CustomFrom must be strictly earlier than CustomTo.");

                return new DateWindow
                {
                    FromInclusive = customFrom.Value,
                    ToExclusive = customTo.Value
                };
            }

            if (horizon == MaturityHorizon.Custom)
                throw new BadRequestException("A custom maturity horizon requires both CustomFrom and CustomTo.");

            // 24 months = AddMonths(24), not 730 days
            if (horizon == MaturityHorizon.Next24Months)
            {
                return new DateWindow
                {
                    FromInclusive = reportAsOfMoment,
                    ToExclusive = reportAsOfMoment.AddMonths(24)
                };
            }

            var days = DaysFor(horizon);
            return new DateWindow
            {
                FromInclusive = reportAsOfMoment,
                ToExclusive = reportAsOfMoment.AddDays(days)
            };
        }

        private static int DaysFor(MaturityHorizon horizon)
            => horizon switch
            {
                MaturityHorizon.Next7Days => 7,
                MaturityHorizon.Next30Days => 30,
                MaturityHorizon.Next90Days => 90,
                MaturityHorizon.Next180Days => 180,
                MaturityHorizon.Next365Days => 365,
                _ => throw new BadRequestException("Unsupported maturity horizon.")
            };

        public List<CalendarPeriod> ResolveCalendarPeriods(CalendarGrouping grouping, DateTime fromInclusive, DateTime toExclusive)
        {
            if (fromInclusive >= toExclusive)
                throw new BadRequestException("From must be strictly earlier than To.");

            if (grouping == CalendarGrouping.Custom)
            {
                return new List<CalendarPeriod>
                {
                    new CalendarPeriod
                    {
                        Label = BuildCustomLabel(fromInclusive, toExclusive),
                        FromInclusive = fromInclusive,
                        ToExclusive = toExclusive
                    }
                };
            }

            // Buckets are half-open [from, to) and cover the whole range without gaps.
            var periods = new List<CalendarPeriod>();
            var cursor = fromInclusive;
            while (cursor < toExclusive)
            {
                var nextBoundary = NextBoundary(grouping, cursor);
                var end = nextBoundary < toExclusive ? nextBoundary : toExclusive;

                periods.Add(new CalendarPeriod
                {
                    Label = LabelFor(grouping, cursor),
                    FromInclusive = cursor,
                    ToExclusive = end
                });

                cursor = end;
            }

            return periods;
        }

        private static DateTime NextBoundary(CalendarGrouping grouping, DateTime cursor)
            => grouping switch
            {
                CalendarGrouping.Monthly =>
                    new DateTime(cursor.Year, cursor.Month, 1, 0, 0, 0, cursor.Kind).AddMonths(1),
                CalendarGrouping.Quarterly =>
                    new DateTime(cursor.Year, QuarterStartMonth(cursor.Month), 1, 0, 0, 0, cursor.Kind).AddMonths(3),
                CalendarGrouping.Yearly =>
                    new DateTime(cursor.Year + 1, 1, 1, 0, 0, 0, cursor.Kind),
                _ => throw new BadRequestException("Unsupported calendar grouping.")
            };

        private static string LabelFor(CalendarGrouping grouping, DateTime cursor)
            => grouping switch
            {
                CalendarGrouping.Monthly =>
                    cursor.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                CalendarGrouping.Quarterly =>
                    string.Format(CultureInfo.InvariantCulture, "{0:D4}-Q{1}", cursor.Year, (cursor.Month - 1) / 3 + 1),
                CalendarGrouping.Yearly =>
                    cursor.ToString("yyyy", CultureInfo.InvariantCulture),
                _ => throw new BadRequestException("Unsupported calendar grouping.")
            };

        private static int QuarterStartMonth(int month) => (month - 1) / 3 * 3 + 1;

        private static string BuildCustomLabel(DateTime fromInclusive, DateTime toExclusive)
            => string.Format(
                CultureInfo.InvariantCulture,
                "{0} to {1}",
                fromInclusive.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                toExclusive.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
