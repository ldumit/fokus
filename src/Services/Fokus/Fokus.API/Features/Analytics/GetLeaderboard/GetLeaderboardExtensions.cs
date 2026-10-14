using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetLeaderboard;

public static class GetLeaderboardExtensions
{
    public static LeaderboardMultiSprintDto ToLeaderboardMultiSprintDto(this LeaderboardMultiSprintResult multiSprint) =>
        new(
            multiSprint.Sprints.Select(SprintRefExtensions.ToSprintRefDto).ToList(),
            multiSprint.Developers.Select(ToLeaderboardDeveloperEntryDto).ToList());

    public static LeaderboardSingleSprintDto ToLeaderboardSingleSprintDto(this LeaderboardSingleSprintResult singleSprint) =>
        new(
            singleSprint.Sprint.ToSprintRefDto(),
            singleSprint.Developers.Select(ToLeaderboardDeveloperSingleEntryDto).ToList());

    public static LeaderboardDeveloperEntryDto ToLeaderboardDeveloperEntryDto(this LeaderboardDeveloperEntry developerEntry) =>
        new(
            AccountId: developerEntry.AccountId,
            DisplayName: developerEntry.DisplayName,
            SubTeam: developerEntry.SubTeam,
            AvatarUrl: developerEntry.AvatarUrl,
            FeatureSp: developerEntry.FeatureSp,
            BugSp: developerEntry.BugSp,
            TotalSp: developerEntry.TotalSp,
            FeatureTickets: developerEntry.FeatureTickets,
            BugTickets: developerEntry.BugTickets,
            TotalTickets: developerEntry.TotalTickets,
            CapacityPercent: developerEntry.CapacityPercent,
            developerEntry.SprintBreakdowns.Select(ToLeaderboardDeveloperSprintBreakdownDto).ToList());

    public static LeaderboardDeveloperSingleEntryDto ToLeaderboardDeveloperSingleEntryDto(this LeaderboardDeveloperSingleEntry developerSingleEntry) =>
        new(
            AccountId: developerSingleEntry.AccountId,
            DisplayName: developerSingleEntry.DisplayName,
            SubTeam: developerSingleEntry.SubTeam,
            AvatarUrl: developerSingleEntry.AvatarUrl,
            FeatureSp: developerSingleEntry.FeatureSp,
            BugSp: developerSingleEntry.BugSp,
            TotalSp: developerSingleEntry.TotalSp,
            FeatureTickets: developerSingleEntry.FeatureTickets,
            BugTickets: developerSingleEntry.BugTickets,
            TotalTickets: developerSingleEntry.TotalTickets,
            CapacityPercent: developerSingleEntry.CapacityPercent,
            developerSingleEntry.Delta?.ToLeaderboardDeveloperDeltaDto());

    public static LeaderboardDeveloperSprintBreakdownDto ToLeaderboardDeveloperSprintBreakdownDto(this LeaderboardDeveloperSprintBreakdown developerSprintBreakdown) =>
        new(
            developerSprintBreakdown.SprintId,
            FeatureSp: developerSprintBreakdown.FeatureSp,
            BugSp: developerSprintBreakdown.BugSp,
            TotalSp: developerSprintBreakdown.TotalSp,
            FeatureTickets: developerSprintBreakdown.FeatureTickets,
            BugTickets: developerSprintBreakdown.BugTickets,
            CapacityPercent: developerSprintBreakdown.CapacityPercent);

    public static LeaderboardDeveloperDeltaDto ToLeaderboardDeveloperDeltaDto(this LeaderboardDeveloperDelta developerDelta) =>
        new(
            developerDelta.FeatureSpDelta,
            FeatureSpDeltaDirection: developerDelta.FeatureSpDeltaDirection,
            FeatureSpDeltaPolarity: developerDelta.FeatureSpDeltaPolarity,
            developerDelta.BugSpDelta,
            BugSpDeltaDirection: developerDelta.BugSpDeltaDirection,
            BugSpDeltaPolarity: developerDelta.BugSpDeltaPolarity,
            developerDelta.TotalSpDelta,
            TotalSpDeltaDirection: developerDelta.TotalSpDeltaDirection,
            TotalSpDeltaPolarity: developerDelta.TotalSpDeltaPolarity,
            developerDelta.FeatureTicketsDelta,
            FeatureTicketsDeltaDirection: developerDelta.FeatureTicketsDeltaDirection,
            FeatureTicketsDeltaPolarity: developerDelta.FeatureTicketsDeltaPolarity,
            developerDelta.BugTicketsDelta,
            BugTicketsDeltaDirection: developerDelta.BugTicketsDeltaDirection,
            BugTicketsDeltaPolarity: developerDelta.BugTicketsDeltaPolarity);
}
