namespace Fokus.API.Features.Analytics.Shared;

public static class DayBreakdownEntryExtensions
{
    public static DayBreakdownEntryDto ToDayBreakdownEntryDto(this DayBreakdownEntry dayBreakdownEntry) =>
        new(
            dayBreakdownEntry.Day,
            dayBreakdownEntry.Date,
            dayBreakdownEntry.CompletedTickets.Select(ToCompletedTicketEntryDto).ToList(),
            CumulativeSp: dayBreakdownEntry.CumulativeSp,
            ExpectedCumulativeSp: dayBreakdownEntry.ExpectedCumulativeSp);

    public static CompletedTicketEntryDto ToCompletedTicketEntryDto(this CompletedTicketEntry completedTicketEntry) =>
        new(
            Key: completedTicketEntry.Key,
            Summary: completedTicketEntry.Summary,
            completedTicketEntry.StoryPoints,
            completedTicketEntry.IssueType);
}
