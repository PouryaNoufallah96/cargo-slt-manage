namespace SLT.Services._Treasury.Calculators
{
    public interface IPeriodBucketer
    {
        DateWindow ResolveMaturityWindow(MaturityHorizon horizon, DateTime? customFrom, DateTime? customTo, DateTime reportAsOfMoment);

        List<CalendarPeriod> ResolveCalendarPeriods(CalendarGrouping grouping, DateTime fromInclusive, DateTime toExclusive);
    }

    public enum MaturityHorizon { Next7Days, Next30Days, Next90Days, Next180Days, Next365Days, Custom, Next24Months }

    public enum CalendarGrouping { Monthly, Quarterly, Yearly, Custom }

    public class CalendarPeriod
    {
        public string Label { get; set; }
        public DateTime FromInclusive { get; set; }
        public DateTime ToExclusive { get; set; } // half-open: >= From, < To
    }

    public class DateWindow
    {
        public DateTime FromInclusive { get; set; }
        public DateTime ToExclusive { get; set; } // half-open: >= From, < To
    }
}
