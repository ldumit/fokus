using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetSprintSummary;

public class GetSprintSummaryRequest
{
    public int? SprintId { get; set; }
    public string? SubTeam { get; set; }
}

public class GetSprintSummaryRequestValidator : Validator<GetSprintSummaryRequest>
{
    public GetSprintSummaryRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .When(x => x.SprintId.HasValue)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public sealed record GetSprintSummaryResponse(
    SprintInfoDto? Sprint,
    HealthScoreDto? HealthScore,
    MetricsDto? Metrics,
    IReadOnlyList<EpicProgressDto> TopEpics,
    IReadOnlyList<DeveloperSummaryDto> Leaderboard,
    FlagsDto Flags);

public sealed record SprintInfoDto(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    DateTime SyncedAt);

public sealed record HealthScoreDto(
    decimal CompositeScore,
    string CompositeRag,
    decimal CompletionSubScore,
    string CompletionRag,
    decimal DisruptionSubScore,
    string DisruptionRag,
    decimal CarryOverSubScore,
    string CarryOverRag)
{
    public decimal? QualitySubScore { get; init; }
    public string? QualityRag { get; init; }
    public QualityBreakdownDto? QualityBreakdown { get; init; }
}

public sealed record MetricsDto(
    MetricCardDto SpCompleted,
    MetricCardDto CompletionRate,
    MetricCardDto ScopeDisruptionRate,
    MetricCardDto BugDisruptionRate,
    MetricCardDto CarryOverRate,
    decimal BugSpCompleted);

public sealed record EpicProgressDto(
    string EpicName,
    decimal SpCompletedThisSprint,
    decimal TotalSp,
    decimal DoneSp,
    decimal CompletionPercentage);

public sealed record DeveloperSummaryDto(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    decimal SpCompleted,
    decimal FeatureSp,
    decimal BugSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public sealed record FlagsDto(
    IReadOnlyList<ZombieTicketDto> ZombieTickets,
    MidSprintDisruptionDto? MidSprintDisruption,
    IReadOnlyList<string> ZeroSpDevelopers,
    bool HasAnyFlags)
{
    public TestingCrunchFlagDto? TestingCrunch { get; init; }
}

public sealed record QualityBreakdownDto(
    decimal CoverageScore,
    int CoverageWeight,
    decimal PassRateScore,
    int PassRateWeight);

public sealed record ZombieTicketDto(
    string TicketKey,
    string Summary,
    int SprintCount,
    string? AssigneeName);

public sealed record MidSprintDisruptionDto(
    decimal TotalSp,
    int TicketCount);

public sealed record TestingCrunchFlagDto(
    decimal CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount);
