using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Developers;


public record DeveloperProgressAlert(
    string AccountId,
    string DisplayName,
    string? AvatarUrl,
    decimal GapSp,
    decimal GapDays,
    decimal? GapDelta,
    string Direction);

public record CompletedTicketEntry(string Key, string Summary, decimal? StoryPoints, string IssueType);

public record DayBreakdownEntry(int Day, DateTime Date, List<CompletedTicketEntry> CompletedTickets, decimal CumulativeSp, decimal ExpectedCumulativeSp);

public record StalledTicketEntry(string Key, string Summary, string CurrentStatus, string IssueType, decimal? StoryPoints, int DaysSinceLastTransition);

public record DeveloperProgressEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    List<StalledTicketEntry> StalledTickets,
    List<DayBreakdownEntry> DailyBreakdown);

public record DeveloperProgressResult(
    bool HasActiveSprint,
    SprintRef? Sprint,
    int CurrentDay,
    int TotalDays,
    bool IsGracePeriod,
    List<DeveloperProgressAlert> Alerts,
    List<DeveloperProgressEntry> Developers);
