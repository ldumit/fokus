namespace Fokus.Domain.Analytics.Shared;

public static class BusinessDayCalculator
{
    public static int CountBusinessDays(DateTime from, DateTime to)
    {
        var count = 0;
        var current = from.Date.AddDays(1); // exclude the transition day itself
        while (current <= to.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                count++;
            current = current.AddDays(1);
        }
        return count;
    }
}
