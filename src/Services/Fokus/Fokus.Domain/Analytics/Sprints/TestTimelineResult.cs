namespace Fokus.Domain.Analytics.Sprints;

public sealed record TestTimelineResult(
    IReadOnlyList<BurnupDayEntry> BurnupData,
    IReadOnlyList<ScopeChangeDayEntry> ScopeChangeOverlay,
    TestingCrunchResult TestingCrunch,
    PostSprintTestingResult PostSprintTesting,
    UntestedAtCloseResult UntestedAtClose,
    DevToTestGapResult DevToTestGap);

public sealed record BurnupDayEntry(
    int DayNumber,
    DateTime CalendarDate,
    bool IsWithinSprint,
    int CumulativePass,
    int CumulativeFail,
    int CumulativeTotal,
    int DailyPass,
    int DailyFail);

public sealed record ScopeChangeDayEntry(
    int DayNumber,
    DateTime CalendarDate,
    decimal AddedSp,
    decimal RemovedSp,
    decimal NetSp);

public sealed record TestingCrunchResult(
    bool IsCrunchFlagged,
    decimal? CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount,
    IReadOnlyList<CrunchTicketEntry> CrunchTickets);

public sealed record CrunchTicketEntry(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    int LateRunCount);

public sealed record PostSprintTestingResult(
    bool HasPostSprintTesting,
    decimal? PostSprintPercentage,
    int PostSprintRunCount,
    int TotalRunCount,
    IReadOnlyList<PostSprintTicketEntry> PostSprintTickets);

public sealed record PostSprintTicketEntry(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    int PostSprintRunCount);

public sealed record UntestedAtCloseResult(
    bool HasUntestedAtClose,
    int UntestedAtCloseCount,
    IReadOnlyList<UntestedTicketEntry> UntestedAtCloseTickets);

public sealed record UntestedTicketEntry(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    DateTime DevDoneDate);

public sealed record DevToTestGapResult(
    decimal? MedianGapDays,
    decimal? MedianGapDelta,
    string? MedianGapDirection,
    IReadOnlyList<GapTicketEntry> GapTickets);

public sealed record GapTicketEntry(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    DateTime DevDoneDate,
    DateTime FirstTestDate,
    decimal GapDays);
