using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Developers;


public record DeveloperDetailInfo(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    string Role,
    int DefaultCapacityPercent);

public record SprintTrendEntry(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    DateTime EndDate,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    decimal AssignedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal? RollingAverageSp,
    decimal BugPercent);

public record WorkAllocationSummary(
    decimal AverageBugPercent,
    int SprintsAboveTarget,
    int TotalSprints);

public record CurrentSprintDetail(
    SprintRef Sprint,
    int CurrentDay,
    int TotalDays,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal CompletionPercent,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    List<DayBreakdownEntry> DailyBreakdown,
    List<TicketDetailEntry> Tickets);

public record TicketDetailEntry(
    string Key,
    string Summary,
    string CurrentStatus,
    decimal? StoryPoints,
    string IssueType,
    int DaysInCurrentStatus,
    string State,
    bool IsStalled);

public record DeveloperDetailResult(
    DeveloperDetailInfo Developer,
    List<SprintTrendEntry> SprintTrends,
    WorkAllocationSummary WorkAllocation,
    CurrentSprintDetail? CurrentSprint,
    decimal BugRatioTarget,
    string JiraInstanceUrl);
