using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Sprints;

public record ScopeChangeSummaryMetrics(
    decimal AverageDisruptionRate,
    decimal? AverageDisruptionRateDelta,
    string? AverageDisruptionRateDeltaDirection,
    decimal AverageNetScopeChange,
    int TotalBugsAdded);

public record ScopeChangePerSprintData(
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

public record ClassificationEntry(
    string Category,
    int TicketCount,
    decimal? SpTotal,
    decimal Percentage);

public record ScopeChangeMultiSprintResult(
    List<SprintRef> Sprints,
    ScopeChangeSummaryMetrics SummaryMetrics,
    List<ScopeChangePerSprintData> PerSprintData,
    List<ClassificationEntry> ClassificationBreakdown);

public record ScopeMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record ScopeChangeSingleSprintMetrics(
    ScopeMetricCard CommittedSpActive,
    ScopeMetricCard CommittedSpTotal,
    ScopeMetricCard AddedSp,
    ScopeMetricCard RemovedSp,
    ScopeMetricCard NetScopeChange,
    ScopeMetricCard DisruptionRate,
    ScopeMetricCard BugCount);

public record BurnupDataPoint(
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

public record ScopeChangeEvent(
    DateTime Date,
    int SprintDayNumber,
    string TicketKey,
    string TicketSummary,
    decimal? StoryPoints,
    string IssueType,
    string Action,
    string? Category,
    bool IsExcluded);

public record BugTimeInProgress(
    string TicketKey,
    string TicketSummary,
    decimal TimeInActiveDays,
    string CurrentStatus);

public record ScopeChangeSingleSprintResult(
    SprintRef Sprint,
    ScopeChangeSingleSprintMetrics Metrics,
    List<BurnupDataPoint> BurnupData,
    List<ClassificationEntry> ClassificationBreakdown,
    List<ScopeChangeEvent> Events,
    List<BugTimeInProgress> BugTimeInProgress);
