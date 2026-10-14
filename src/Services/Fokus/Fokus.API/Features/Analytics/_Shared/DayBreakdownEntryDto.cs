namespace Fokus.API.Features.Analytics.Shared;

public sealed record DayBreakdownEntryDto(
    int Day,
    DateTime Date,
    IReadOnlyList<CompletedTicketEntryDto> CompletedTickets,
    decimal CumulativeSp,
    decimal ExpectedCumulativeSp);

public sealed record CompletedTicketEntryDto(
    string Key,
    string Summary,
    decimal? StoryPoints,
    string IssueType);
