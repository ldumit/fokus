using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Sprints;

public record SprintInfo(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    DateTime SyncedAt);

public record QualityBreakdownResult(
    decimal CoverageScore,
    int CoverageWeight,
    decimal PassRateScore,
    int PassRateWeight);

public record HealthScoreResult(
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
    public QualityBreakdownResult? QualityBreakdown { get; init; }
}

public record MetricsResult(
    MetricCard SpCompleted,
    MetricCard CompletionRate,
    MetricCard ScopeDisruptionRate,
    MetricCard BugDisruptionRate,
    MetricCard CarryOverRate,
    decimal BugSpCompleted);

public record EpicProgress(
    string EpicName,
    decimal SpCompletedThisSprint,
    decimal TotalSp,
    decimal DoneSp,
    decimal CompletionPercentage);

public record DeveloperSummary(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    decimal SpCompleted,
    decimal FeatureSp,
    decimal BugSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public record ZombieTicket(string TicketKey, string Summary, int SprintCount, string? AssigneeName);

public record MidSprintDisruption(decimal TotalSp, int TicketCount);

public record FlagsResult(
    List<ZombieTicket> ZombieTickets,
    MidSprintDisruption? MidSprintDisruption,
    List<string> ZeroSpDevelopers,
    bool HasAnyFlags)
{
    public TestingCrunchFlag? TestingCrunch { get; init; }
}

public record SprintSummaryResult(
    SprintInfo? Sprint,
    HealthScoreResult? HealthScore,
    MetricsResult? Metrics,
    List<EpicProgress> TopEpics,
    List<DeveloperSummary> Leaderboard,
    FlagsResult Flags);
