using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Sprints;

// --- Multi-sprint ---

public record BugRatioTeamTrendEntry(
    int SprintId,
    decimal BugRatioPercent,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp);

public record BugRatioTeamMetrics(
    decimal BugRatioPercent,
    decimal TotalBugSp,
    decimal TotalNonBugSp,
    decimal TotalCompletedSp,
    List<BugRatioTeamTrendEntry> PerSprintTrend);

public record BugRatioIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal SpTotal);

public record BugRatioAlertStatus(bool IsActive, int ConsecutiveSprintCount, int ThresholdPercent);

public record BugRatioDeveloperSprintBreakdown(
    int SprintId,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount);

public record BugRatioDeveloperEntry(
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
    List<BugRatioDeveloperSprintBreakdown> SprintBreakdowns,
    BugRatioAlertStatus Alert);

public record BugRatioMultiSprintResult(
    List<SprintRef> Sprints,
    BugRatioTeamMetrics TeamMetrics,
    List<BugRatioIssueTypeEntry> IssueTypeBreakdown,
    List<BugRatioDeveloperEntry> Developers);

// --- Single-sprint ---

public record BugRatioMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record BugRatioTeamSingleMetrics(
    BugRatioMetricCard BugRatioPercent,
    BugRatioMetricCard BugSp,
    BugRatioMetricCard NonBugSp);

public record BugRatioDeveloperDelta(
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

public record BugRatioDeveloperSingleEntry(
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
    BugRatioDeveloperDelta? Delta,
    BugRatioAlertStatus Alert);

public record BugRatioSingleSprintResult(
    SprintRef Sprint,
    BugRatioTeamSingleMetrics TeamMetrics,
    List<BugRatioIssueTypeEntry> IssueTypeBreakdown,
    List<BugRatioDeveloperSingleEntry> Developers);
