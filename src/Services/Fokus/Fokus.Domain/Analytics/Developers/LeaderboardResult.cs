using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Developers;


public record LeaderboardDeveloperSprintBreakdown(
    int SprintId,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public record LeaderboardDeveloperEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int TotalTickets,
    int CapacityPercent,
    List<LeaderboardDeveloperSprintBreakdown> SprintBreakdowns);

public record LeaderboardMultiSprintResult(
    List<SprintRef> Sprints,
    List<LeaderboardDeveloperEntry> Developers);

public record LeaderboardDeveloperDelta(
    decimal FeatureSpDelta,
    string FeatureSpDeltaDirection,
    string FeatureSpDeltaPolarity,
    decimal BugSpDelta,
    string BugSpDeltaDirection,
    string BugSpDeltaPolarity,
    decimal TotalSpDelta,
    string TotalSpDeltaDirection,
    string TotalSpDeltaPolarity,
    int FeatureTicketsDelta,
    string FeatureTicketsDeltaDirection,
    string FeatureTicketsDeltaPolarity,
    int BugTicketsDelta,
    string BugTicketsDeltaDirection,
    string BugTicketsDeltaPolarity);

public record LeaderboardDeveloperSingleEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int TotalTickets,
    int CapacityPercent,
    LeaderboardDeveloperDelta? Delta);

public record LeaderboardSingleSprintResult(
    SprintRef Sprint,
    List<LeaderboardDeveloperSingleEntry> Developers);
