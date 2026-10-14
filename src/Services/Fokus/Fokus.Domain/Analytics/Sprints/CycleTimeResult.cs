using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Sprints;

// --- Shared types ---

public record CycleTimeMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record StageBreakdownEntry(string StageName, decimal DurationDays);

public record CycleTimeScatterPoint(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    DateTime CompletionDate,
    List<StageBreakdownEntry> StageBreakdown,
    int ReworkCount);

public record StageFunnelEntry(string StageName, decimal AverageDurationDays, decimal Percentage);

public record CycleTimeIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal MedianCycleTime,
    decimal P85CycleTime);

public record CycleTimeDeveloperEntry(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    string DominantStage);

public record CycleTimeOutlierEntry(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    List<StageBreakdownEntry> StageBreakdown,
    int ReworkCount);

public record CycleTimeBoundaries(string StartStage, string EndStage);

// --- Single-sprint ---

public record CycleTimeSingleSprintResult(
    SprintRef? Sprint,
    List<CycleTimeMetricCard>? MetricCards,
    List<CycleTimeScatterPoint> ScatterPlot,
    List<StageFunnelEntry> StageFunnel,
    List<CycleTimeIssueTypeEntry> IssueTypeBreakdown,
    List<CycleTimeDeveloperEntry> DeveloperBreakdown,
    List<CycleTimeOutlierEntry> Outliers,
    CycleTimeBoundaries Boundaries);

// --- Multi-sprint ---

public record CycleTimeTrendEntry(
    int SprintId,
    string SprintName,
    decimal P85CycleTime,
    decimal MedianCycleTime,
    int TicketsCompleted);

public record CycleTimeSprintSummaryEntry(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    int OutlierCount);

public record CycleTimeMultiSprintResult(
    List<SprintRef> Sprints,
    List<CycleTimeMetricCard>? MetricCards,
    List<CycleTimeTrendEntry> Trend,
    List<StageFunnelEntry> StageFunnel,
    List<CycleTimeSprintSummaryEntry> SprintSummaries,
    CycleTimeBoundaries Boundaries);
