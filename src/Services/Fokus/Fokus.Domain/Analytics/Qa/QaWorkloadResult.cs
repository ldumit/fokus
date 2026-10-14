using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Qa;


// --- Multi-sprint records ---

public record QaWorkloadTeamMetrics(
    int TotalTes,
    int TotalRunsCompleted,
    int PassCount,
    int FailCount,
    decimal TeamPassRate);

public record QaWorkloadSprintBreakdown(
    int SprintId,
    string SprintName,
    int TesOwned,
    int RunsCompleted,
    int PassCount,
    int FailCount,
    decimal PassRate,
    int StoriesCovered,
    int BugsFound);

public record WorkloadAlert(bool IsActive, int ConsecutiveSprintCount, int ThresholdPercent);

public record QaWorkloadEntry(
    string? AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    int TesOwned,
    int RunsCompleted,
    int PassCount,
    int FailCount,
    decimal PassRate,
    int StoriesCovered,
    int BugsFound,
    List<QaWorkloadSprintBreakdown> SprintBreakdowns,
    WorkloadAlert WorkloadAlert);

public record QaWorkloadMultiSprintResult(
    List<SprintRef> Sprints,
    QaWorkloadTeamMetrics TeamMetrics,
    List<QaWorkloadEntry> Developers);

// --- Single-sprint records ---

public record QaWorkloadSingleTeamMetrics(
    MetricCard TotalTes,
    MetricCard TotalRunsCompleted,
    MetricCard TeamPassRate);

public record QaWorkloadSingleEntry(
    string? AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    int TesOwned,
    int RunsCompleted,
    int PassCount,
    int FailCount,
    decimal PassRate,
    int StoriesCovered,
    int BugsFound,
    decimal? TesOwnedDelta,
    string? TesOwnedDirection,
    string? TesOwnedPolarity,
    decimal? RunsCompletedDelta,
    string? RunsCompletedDirection,
    string? RunsCompletedPolarity,
    decimal? PassCountDelta,
    string? PassCountDirection,
    string? PassCountPolarity,
    decimal? FailCountDelta,
    string? FailCountDirection,
    string? FailCountPolarity,
    decimal? PassRateDelta,
    string? PassRateDirection,
    string? PassRatePolarity,
    decimal? StoriesCoveredDelta,
    string? StoriesCoveredDirection,
    string? StoriesCoveredPolarity,
    decimal? BugsFoundDelta,
    string? BugsFoundDirection,
    string? BugsFoundPolarity,
    WorkloadAlert WorkloadAlert);

public record QaWorkloadSingleSprintResult(
    SprintRef Sprint,
    QaWorkloadSingleTeamMetrics TeamMetrics,
    List<QaWorkloadSingleEntry> Developers);
