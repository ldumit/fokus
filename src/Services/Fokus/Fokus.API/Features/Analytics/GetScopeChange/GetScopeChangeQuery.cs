using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetScopeChange;

public class GetScopeChangeRequest
{
    public int? SprintId { get; set; }
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetScopeChangeRequestValidator : Validator<GetScopeChangeRequest>
{
    public GetScopeChangeRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .When(x => x.SprintId.HasValue)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be at least 0 (0 means all sprints).");

        RuleFor(x => x)
            .Must(x => !(x.SprintId.HasValue && x.Last.HasValue))
            .WithMessage("SprintId and Last cannot both be provided.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public record ScopeChangeResponse(
    string Mode,
    ScopeChangeMultiSprintDto? MultiSprint,
    ScopeChangeSingleSprintDto? SingleSprint);

public sealed record ScopeChangeMultiSprintDto(
    IReadOnlyList<SprintRefDto> Sprints,
    ScopeChangeSummaryMetricsDto SummaryMetrics,
    IReadOnlyList<ScopeChangePerSprintDataDto> PerSprintData,
    IReadOnlyList<ClassificationEntryDto> ClassificationBreakdown);

public sealed record ScopeChangeSingleSprintDto(
    SprintRefDto Sprint,
    ScopeChangeSingleSprintMetricsDto Metrics,
    IReadOnlyList<BurnupDataPointDto> BurnupData,
    IReadOnlyList<ClassificationEntryDto> ClassificationBreakdown,
    IReadOnlyList<ScopeChangeEventDto> Events,
    IReadOnlyList<BugTimeInProgressDto> BugTimeInProgress);

public sealed record ScopeChangeSummaryMetricsDto(
    decimal AverageDisruptionRate,
    decimal? AverageDisruptionRateDelta,
    string? AverageDisruptionRateDeltaDirection,
    decimal AverageNetScopeChange,
    int TotalBugsAdded);

public sealed record ScopeChangePerSprintDataDto(
    int SprintId,
    decimal CommittedSpActive,
    decimal CommittedSpTotal,
    decimal AddedSp,
    decimal RemovedSp,
    decimal NetScopeChange,
    decimal CompletedSp,
    decimal DisruptionRate,
    int BugCount,
    decimal BugSpCompleted);

public sealed record ClassificationEntryDto(
    string Category,
    int TicketCount,
    decimal? SpTotal,
    decimal Percentage);

public sealed record ScopeChangeSingleSprintMetricsDto(
    ScopeMetricCardDto CommittedSpActive,
    ScopeMetricCardDto CommittedSpTotal,
    ScopeMetricCardDto AddedSp,
    ScopeMetricCardDto RemovedSp,
    ScopeMetricCardDto NetScopeChange,
    ScopeMetricCardDto DisruptionRate,
    ScopeMetricCardDto BugCount);

public sealed record BurnupDataPointDto(
    int DayNumber,
    DateTime Date,
    decimal TotalScopeSp,
    decimal CompletedSp,
    string Phase,
    decimal BugSp,
    int TotalScopeTickets,
    int CompletedTickets,
    int BugTickets,
    decimal CommittedTotalSp,
    int CommittedTotalTickets);

public sealed record ScopeChangeEventDto(
    DateTime Date,
    int SprintDayNumber,
    string TicketKey,
    string TicketSummary,
    decimal? StoryPoints,
    string IssueType,
    string Action,
    string? Category,
    bool IsExcluded);

public sealed record BugTimeInProgressDto(
    string TicketKey,
    string TicketSummary,
    decimal TimeInActiveDays,
    string CurrentStatus);
