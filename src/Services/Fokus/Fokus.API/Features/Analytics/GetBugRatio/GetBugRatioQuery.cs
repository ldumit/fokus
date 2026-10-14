using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetBugRatio;

public class GetBugRatioRequest
{
    public int? SprintId { get; set; }
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetBugRatioRequestValidator : Validator<GetBugRatioRequest>
{
    public GetBugRatioRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .When(x => x.SprintId.HasValue)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be at least 1.");

        RuleFor(x => x)
            .Must(x => !(x.SprintId.HasValue && x.Last.HasValue))
            .WithMessage("SprintId and Last cannot both be provided.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public record BugRatioResponse(
    string Mode,
    BugRatioMultiSprintDto? MultiSprint,
    BugRatioSingleSprintDto? SingleSprint);

public sealed record BugRatioMultiSprintDto(
    IReadOnlyList<SprintRefDto> Sprints,
    BugRatioTeamMetricsDto TeamMetrics,
    IReadOnlyList<BugRatioIssueTypeEntryDto> IssueTypeBreakdown,
    IReadOnlyList<BugRatioDeveloperEntryDto> Developers);

public sealed record BugRatioSingleSprintDto(
    SprintRefDto Sprint,
    BugRatioTeamSingleMetricsDto TeamMetrics,
    IReadOnlyList<BugRatioIssueTypeEntryDto> IssueTypeBreakdown,
    IReadOnlyList<BugRatioDeveloperSingleEntryDto> Developers);

public sealed record BugRatioTeamMetricsDto(
    decimal BugRatioPercent,
    decimal TotalBugSp,
    decimal TotalNonBugSp,
    decimal TotalCompletedSp,
    IReadOnlyList<BugRatioTeamTrendEntryDto> PerSprintTrend);

public sealed record BugRatioIssueTypeEntryDto(
    string IssueType,
    int TicketCount,
    decimal SpTotal);

public sealed record BugRatioDeveloperEntryDto(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount,
    IReadOnlyList<BugRatioDeveloperSprintBreakdownDto> SprintBreakdowns,
    BugRatioAlertStatusDto Alert);

public sealed record BugRatioTeamSingleMetricsDto(
    BugRatioMetricCardDto BugRatioPercent,
    BugRatioMetricCardDto BugSp,
    BugRatioMetricCardDto NonBugSp);

public sealed record BugRatioDeveloperSingleEntryDto(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount,
    BugRatioDeveloperDeltaDto? Delta,
    BugRatioAlertStatusDto Alert);

public sealed record BugRatioTeamTrendEntryDto(
    int SprintId,
    decimal BugRatioPercent,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp);

public sealed record BugRatioDeveloperSprintBreakdownDto(
    int SprintId,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount);

public sealed record BugRatioAlertStatusDto(
    bool IsActive,
    int ConsecutiveSprintCount,
    int ThresholdPercent);

public sealed record BugRatioMetricCardDto(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public sealed record BugRatioDeveloperDeltaDto(
    decimal BugSpDelta,
    string BugSpDeltaDirection,
    string BugSpDeltaPolarity,
    decimal NonBugSpDelta,
    string NonBugSpDeltaDirection,
    string NonBugSpDeltaPolarity,
    decimal BugRatioPercentDelta,
    string BugRatioPercentDeltaDirection,
    string BugRatioPercentDeltaPolarity,
    int BugTicketCountDelta,
    string BugTicketCountDeltaDirection,
    string BugTicketCountDeltaPolarity,
    int NonBugTicketCountDelta,
    string NonBugTicketCountDeltaDirection,
    string NonBugTicketCountDeltaPolarity);
