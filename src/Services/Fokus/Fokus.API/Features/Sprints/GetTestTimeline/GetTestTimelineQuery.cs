namespace Fokus.API.Features.Sprints.GetTestTimeline;

public class GetTestTimelineRequest
{
    public int SprintId { get; set; }
    public string? SubTeam { get; set; }
}

public class GetTestTimelineRequestValidator : Validator<GetTestTimelineRequest>
{
    public GetTestTimelineRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public sealed record TestTimelineResponse(
    bool HasQaData,
    bool IsXrayEnabled,
    DateTime SprintStartDate,
    DateTime SprintEndDate,
    int PlanningWindowDays,
    IReadOnlyList<BurnupDayEntryDto> BurnupData,
    IReadOnlyList<ScopeChangeDayEntryDto> ScopeChangeOverlay,
    TestingCrunchDto TestingCrunch,
    PostSprintTestingDto PostSprintTesting,
    UntestedAtCloseDto UntestedAtClose,
    DevToTestGapDto DevToTestGap);

public sealed record BurnupDayEntryDto(
    int DayNumber,
    DateTime CalendarDate,
    bool IsWithinSprint,
    int CumulativePass,
    int CumulativeFail,
    int CumulativeTotal,
    int DailyPass,
    int DailyFail);

public sealed record ScopeChangeDayEntryDto(
    int DayNumber,
    DateTime CalendarDate,
    decimal AddedSp,
    decimal RemovedSp,
    decimal NetSp);

public sealed record TestingCrunchDto(
    bool IsCrunchFlagged,
    decimal? CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount,
    IReadOnlyList<CrunchTicketEntryDto> CrunchTickets);

public sealed record PostSprintTestingDto(
    bool HasPostSprintTesting,
    decimal? PostSprintPercentage,
    int PostSprintRunCount,
    int TotalRunCount,
    IReadOnlyList<PostSprintTicketEntryDto> PostSprintTickets);

public sealed record UntestedAtCloseDto(
    bool HasUntestedAtClose,
    int UntestedAtCloseCount,
    IReadOnlyList<UntestedTicketEntryDto> UntestedAtCloseTickets);

public sealed record DevToTestGapDto(
    decimal? MedianGapDays,
    decimal? MedianGapDelta,
    string? MedianGapDirection,
    IReadOnlyList<GapTicketEntryDto> GapTickets);

public sealed record CrunchTicketEntryDto(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    int LateRunCount);

public sealed record PostSprintTicketEntryDto(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    int PostSprintRunCount);

public sealed record UntestedTicketEntryDto(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    DateTime DevDoneDate);

public sealed record GapTicketEntryDto(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    DateTime DevDoneDate,
    DateTime FirstTestDate,
    decimal GapDays);
