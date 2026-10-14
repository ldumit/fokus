using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetCarryOver;

public static class GetCarryOverExtensions
{
    public static CarryOverMultiSprintDto ToCarryOverMultiSprintDto(this CarryOverMultiSprintResult multiSprint) =>
        new(
            multiSprint.Sprints.Select(SprintRefExtensions.ToSprintRefDto).ToList(),
            multiSprint.SummaryMetrics.ToCarryOverSummaryMetricsDto(),
            multiSprint.PerSprintData.Select(ToCarryOverPerSprintDataDto).ToList(),
            multiSprint.IssueTypeBreakdown.Select(ToCarryOverIssueTypeEntryDto).ToList(),
            multiSprint.ZombieTickets.Select(ToCarryOverZombieSummaryDto).ToList());

    public static CarryOverSingleSprintDto ToCarryOverSingleSprintDto(this CarryOverSingleSprintResult singleSprint) =>
        new(
            singleSprint.Sprint.ToSprintRefDto(),
            singleSprint.Metrics.ToCarryOverSingleSprintMetricsDto(),
            singleSprint.StatusDistribution.Select(ToCarryOverStatusDistributionEntryDto).ToList(),
            singleSprint.IssueTypeBreakdown.Select(ToCarryOverIssueTypeEntryDto).ToList(),
            singleSprint.CarryOverDestination?.ToCarryOverDestinationDto(),
            singleSprint.Tickets.Select(ToCarryOverTicketEntryDto).ToList(),
            singleSprint.ZombieTrajectories.Select(ToZombieTrajectoryEntryDto).ToList());

    public static CarryOverSummaryMetricsDto ToCarryOverSummaryMetricsDto(this CarryOverSummaryMetrics summaryMetrics) =>
        new(
            AverageCarryOverRate: summaryMetrics.AverageCarryOverRate,
            AverageCarryOverSp: summaryMetrics.AverageCarryOverSp,
            summaryMetrics.TotalZombieTickets);

    public static CarryOverPerSprintDataDto ToCarryOverPerSprintDataDto(this CarryOverPerSprintData perSprintData) =>
        new(
            perSprintData.SprintId,
            perSprintData.CarryOverSp,
            perSprintData.CarryOverTicketCount,
            CarryOverRate: perSprintData.CarryOverRate,
            TotalScopeSp: perSprintData.TotalScopeSp,
            perSprintData.StatusDistribution.Select(ToCarryOverStatusDistributionEntryDto).ToList());

    public static CarryOverIssueTypeEntryDto ToCarryOverIssueTypeEntryDto(this CarryOverIssueTypeEntry issueTypeEntry) =>
        new(
            issueTypeEntry.IssueType,
            issueTypeEntry.TicketCount,
            SpTotal: issueTypeEntry.SpTotal,
            Percentage: issueTypeEntry.Percentage);

    public static CarryOverZombieSummaryDto ToCarryOverZombieSummaryDto(this CarryOverZombieSummary zombieSummary) =>
        new(
            TicketKey: zombieSummary.TicketKey,
            Summary: zombieSummary.Summary,
            IssueType: zombieSummary.IssueType,
            CurrentStatus: zombieSummary.CurrentStatus,
            zombieSummary.StoryPoints,
            zombieSummary.SprintCount);

    public static CarryOverSingleSprintMetricsDto ToCarryOverSingleSprintMetricsDto(this CarryOverSingleSprintMetrics singleSprintMetrics) =>
        new(
            CarryOverRate: singleSprintMetrics.CarryOverRate.ToScopeMetricCardDto(),
            CarryOverSp: singleSprintMetrics.CarryOverSp.ToScopeMetricCardDto(),
            CarryOverTicketCount: singleSprintMetrics.CarryOverTicketCount.ToScopeMetricCardDto());

    public static CarryOverStatusDistributionEntryDto ToCarryOverStatusDistributionEntryDto(this CarryOverStatusDistributionEntry statusDistributionEntry) =>
        new(
            statusDistributionEntry.StageName,
            statusDistributionEntry.TicketCount,
            SpTotal: statusDistributionEntry.SpTotal,
            Percentage: statusDistributionEntry.Percentage);

    public static CarryOverDestinationDto ToCarryOverDestinationDto(this CarryOverDestination destination) =>
        new(
            destination.PriorSprintId,
            destination.PriorSprintName,
            destination.PriorCarryOverCount,
            destination.PriorCarryOverSp,
            Completed: destination.Completed.ToCarryOverDestinationBucketDto(),
            CarriedAgain: destination.CarriedAgain.ToCarryOverDestinationBucketDto(),
            Removed: destination.Removed.ToCarryOverDestinationBucketDto(),
            Dropped: destination.Dropped.ToCarryOverDestinationBucketDto());

    public static CarryOverTicketEntryDto ToCarryOverTicketEntryDto(this CarryOverTicketEntry ticketEntry) =>
        new(
            TicketKey: ticketEntry.TicketKey,
            Summary: ticketEntry.Summary,
            IssueType: ticketEntry.IssueType,
            ticketEntry.StoryPoints,
            FinalStatus: ticketEntry.FinalStatus,
            WorkflowStage: ticketEntry.WorkflowStage,
            ticketEntry.SprintCount,
            IsZombie: ticketEntry.IsZombie,
            IsExcluded: ticketEntry.IsExcluded);

    public static ZombieTrajectoryEntryDto ToZombieTrajectoryEntryDto(this ZombieTrajectoryEntry zombieTrajectoryEntry) =>
        new(
            TicketKey: zombieTrajectoryEntry.TicketKey,
            Summary: zombieTrajectoryEntry.Summary,
            IssueType: zombieTrajectoryEntry.IssueType,
            zombieTrajectoryEntry.StoryPoints,
            zombieTrajectoryEntry.CurrentStatus,
            zombieTrajectoryEntry.SprintCount,
            zombieTrajectoryEntry.Sprints.Select(ToZombieTrajectorySprintEntryDto).ToList());

    public static CarryOverDestinationBucketDto ToCarryOverDestinationBucketDto(this CarryOverDestinationBucket destinationBucket) =>
        new(
            destinationBucket.Count,
            destinationBucket.Sp);

    public static ZombieTrajectorySprintEntryDto ToZombieTrajectorySprintEntryDto(this ZombieTrajectorySprintEntry zombieTrajectorySprintEntry) =>
        new(
            zombieTrajectorySprintEntry.SprintId,
            SprintName: zombieTrajectorySprintEntry.SprintName,
            FinalStatus: zombieTrajectorySprintEntry.FinalStatus);
}
