using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetDeveloperThroughput;

public static class GetDeveloperThroughputExtensions
{
    public static GetDeveloperThroughputResponse ToGetDeveloperThroughputResponse(this DeveloperThroughputResult developerThroughput) =>
        new(
            developerThroughput.Sprints.Select(SprintRefExtensions.ToSprintRefDto).ToList(),
            developerThroughput.Developers.Select(ToDeveloperThroughputEntryDto).ToList());

    public static DeveloperThroughputEntryDto ToDeveloperThroughputEntryDto(this DeveloperThroughputEntry entry) =>
        new(
            AccountId: entry.AccountId,
            DisplayName: entry.DisplayName,
            SubTeam: entry.SubTeam,
            AvatarUrl: entry.AvatarUrl,
            entry.SprintBreakdowns.Select(ToSprintBreakdownDto).ToList());

    public static SprintBreakdownDto ToSprintBreakdownDto(this SprintBreakdown sprintBreakdown) =>
        new(
            sprintBreakdown.SprintId,
            SpAssigned: sprintBreakdown.SpAssigned,
            SpCompleted: sprintBreakdown.SpCompleted,
            CompletionPercent: sprintBreakdown.CompletionPercent,
            TicketsDone: sprintBreakdown.TicketsDone,
            TicketsCarriedOver: sprintBreakdown.TicketsCarriedOver,
            CapacityPercent: sprintBreakdown.CapacityPercent,
            RollingAverageSpCompleted: sprintBreakdown.RollingAverageSpCompleted,
            SpAssignedDelta: sprintBreakdown.SpAssignedDelta,
            SpCompletedDelta: sprintBreakdown.SpCompletedDelta,
            CompletionPercentDelta: sprintBreakdown.CompletionPercentDelta,
            TicketsDoneDelta: sprintBreakdown.TicketsDoneDelta,
            TicketsCarriedOverDelta: sprintBreakdown.TicketsCarriedOverDelta,
            SpAssignedDeltaDirection: sprintBreakdown.SpAssignedDeltaDirection,
            SpCompletedDeltaDirection: sprintBreakdown.SpCompletedDeltaDirection,
            CompletionPercentDeltaDirection: sprintBreakdown.CompletionPercentDeltaDirection,
            TicketsDoneDeltaDirection: sprintBreakdown.TicketsDoneDeltaDirection,
            TicketsCarriedOverDeltaDirection: sprintBreakdown.TicketsCarriedOverDeltaDirection,
            SpAssignedDeltaPolarity: sprintBreakdown.SpAssignedDeltaPolarity,
            SpCompletedDeltaPolarity: sprintBreakdown.SpCompletedDeltaPolarity,
            CompletionPercentDeltaPolarity: sprintBreakdown.CompletionPercentDeltaPolarity,
            TicketsDoneDeltaPolarity: sprintBreakdown.TicketsDoneDeltaPolarity,
            TicketsCarriedOverDeltaPolarity: sprintBreakdown.TicketsCarriedOverDeltaPolarity);
}
