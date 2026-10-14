using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Developers;


public record SprintBreakdown(
    int SprintId,
    decimal SpAssigned,
    decimal SpCompleted,
    decimal CompletionPercent,
    int TicketsDone,
    int TicketsCarriedOver,
    int CapacityPercent,
    decimal? RollingAverageSpCompleted,
    decimal? SpAssignedDelta,
    decimal? SpCompletedDelta,
    decimal? CompletionPercentDelta,
    int? TicketsDoneDelta,
    int? TicketsCarriedOverDelta,
    string? SpAssignedDeltaDirection,
    string? SpCompletedDeltaDirection,
    string? CompletionPercentDeltaDirection,
    string? TicketsDoneDeltaDirection,
    string? TicketsCarriedOverDeltaDirection,
    string? SpAssignedDeltaPolarity,
    string? SpCompletedDeltaPolarity,
    string? CompletionPercentDeltaPolarity,
    string? TicketsDoneDeltaPolarity,
    string? TicketsCarriedOverDeltaPolarity);

public record DeveloperThroughputEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    List<SprintBreakdown> SprintBreakdowns);

public record DeveloperThroughputResult(
    List<SprintRef> Sprints,
    List<DeveloperThroughputEntry> Developers);
