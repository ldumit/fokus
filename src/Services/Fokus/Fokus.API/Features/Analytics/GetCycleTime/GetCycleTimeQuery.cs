using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetCycleTime;

public class GetCycleTimeRequest
{
    public int? SprintId { get; set; }
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetCycleTimeRequestValidator : Validator<GetCycleTimeRequest>
{
    public GetCycleTimeRequestValidator()
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

public record CycleTimeResponse(
    string Mode,
    CycleTimeSingleSprintDto? SingleSprint,
    CycleTimeMultiSprintDto? MultiSprint);

public sealed record CycleTimeSingleSprintDto(
    SprintRefDto? Sprint,
    IReadOnlyList<CycleTimeMetricCardDto>? MetricCards,
    IReadOnlyList<CycleTimeScatterPointDto> ScatterPlot,
    IReadOnlyList<StageFunnelEntryDto> StageFunnel,
    IReadOnlyList<CycleTimeIssueTypeEntryDto> IssueTypeBreakdown,
    IReadOnlyList<CycleTimeDeveloperEntryDto> DeveloperBreakdown,
    IReadOnlyList<CycleTimeOutlierEntryDto> Outliers,
    CycleTimeBoundariesDto Boundaries);

public sealed record CycleTimeMultiSprintDto(
    IReadOnlyList<SprintRefDto> Sprints,
    IReadOnlyList<CycleTimeMetricCardDto>? MetricCards,
    IReadOnlyList<CycleTimeTrendEntryDto> Trend,
    IReadOnlyList<StageFunnelEntryDto> StageFunnel,
    IReadOnlyList<CycleTimeSprintSummaryEntryDto> SprintSummaries,
    CycleTimeBoundariesDto Boundaries);

public sealed record CycleTimeMetricCardDto(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public sealed record CycleTimeScatterPointDto(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    DateTime CompletionDate,
    IReadOnlyList<StageBreakdownEntryDto> StageBreakdown,
    int ReworkCount);

public sealed record StageFunnelEntryDto(
    string StageName,
    decimal AverageDurationDays,
    decimal Percentage);

public sealed record CycleTimeIssueTypeEntryDto(
    string IssueType,
    int TicketCount,
    decimal MedianCycleTime,
    decimal P85CycleTime);

public sealed record CycleTimeDeveloperEntryDto(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    string DominantStage);

public sealed record CycleTimeOutlierEntryDto(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    IReadOnlyList<StageBreakdownEntryDto> StageBreakdown,
    int ReworkCount);

public sealed record CycleTimeBoundariesDto(
    string StartStage,
    string EndStage);

public sealed record CycleTimeTrendEntryDto(
    int SprintId,
    string SprintName,
    decimal P85CycleTime,
    decimal MedianCycleTime,
    int TicketsCompleted);

public sealed record CycleTimeSprintSummaryEntryDto(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    int OutlierCount);

public sealed record StageBreakdownEntryDto(
    string StageName,
    decimal DurationDays);
