using System;

namespace itpayroll.Utilities
{
    public enum PeriodType
    {
        Today,
        ThisWeek,
        ThisMonth,
        LastMonth,
        ThisYear,
        Custom
    }

    public static class PeriodHelper
    {
        public static (DateTime? From, DateTime? To) GetDateRange(string period, string customFrom, string customTo)
        {
            return period switch
            {
                "Today" => (DateTime.Today, DateTime.Today),
                "ThisWeek" => GetWeekRange(DateTime.Today),
                "ThisMonth" => GetMonthRange(DateTime.Today),
                "LastMonth" => GetLastMonthRange(),
                "ThisYear" => (new DateTime(DateTime.Today.Year, 1, 1), 
                                 new DateTime(DateTime.Today.Year, 12, 31)),
                "Custom" => (
                    string.IsNullOrEmpty(customFrom) ? null : DateTime.Parse(customFrom),
                    string.IsNullOrEmpty(customTo) ? null : DateTime.Parse(customTo)
                ),
                _ => (null, null) // No filter
            };
        }

        private static (DateTime, DateTime) GetWeekRange(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime monday = date.AddDays(-1 * diff).Date;
            return (monday, monday.AddDays(6));
        }

        private static (DateTime, DateTime) GetMonthRange(DateTime date)
        {
            return (new DateTime(date.Year, date.Month, 1),
                    new DateTime(date.Year, date.Month, 
                           DateTime.DaysInMonth(date.Year, date.Month)));
        }

        private static (DateTime, DateTime) GetLastMonthRange()
        {
            DateTime today = DateTime.Today;
            DateTime lastMonth = today.AddMonths(-1);
            return (new DateTime(lastMonth.Year, lastMonth.Month, 1),
                    new DateTime(lastMonth.Year, lastMonth.Month, 
                           DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month)));
        }
    }
}
