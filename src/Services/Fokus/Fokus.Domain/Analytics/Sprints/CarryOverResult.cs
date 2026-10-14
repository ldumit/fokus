using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Sprints;

// --- Multi-sprint ---

public record CarryOverSummaryMetrics(
    decimal AverageCarryOverRate,
    decimal AverageCarryOverSp,
    int TotalZombieTickets);

public record CarryOverStatusDistributionEntry(
    string StageName,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public record CarryOverPerSprintData(
    int SprintId,
    decimal CarryOverSp,
    int CarryOverTicketCount,
    decimal CarryOverRate,
    decimal TotalScopeSp,
    List<CarryOverStatusDistributionEntry> StatusDistribution);

public record CarryOverIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public record CarryOverZombieSummary(
    string TicketKey,
    string Summary,
    string IssueType,
    string CurrentStatus,
    decimal? StoryPoints,
    int SprintCount);

public record CarryOverMultiSprintResult(
    List<SprintRef> Sprints,
    CarryOverSummaryMetrics SummaryMetrics,
    List<CarryOverPerSprintData> PerSprintData,
    List<CarryOverIssueTypeEntry> IssueTypeBreakdown,
    List<CarryOverZombieSummary> ZombieTickets);

// --- Single-sprint ---

public record CarryOverSingleSprintMetrics(
    ScopeMetricCard CarryOverRate,
    ScopeMetricCard CarryOverSp,
    ScopeMetricCard CarryOverTicketCount);

public record CarryOverDestinationBucket(int Count, decimal Sp);

public record CarryOverDestination(
    int PriorSprintId,
    string PriorSprintName,
    int PriorCarryOverCount,
    decimal PriorCarryOverSp,
    CarryOverDestinationBucket Completed,
    CarryOverDestinationBucket CarriedAgain,
    CarryOverDestinationBucket Removed,
    CarryOverDestinationBucket Dropped);

public record CarryOverTicketEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string FinalStatus,
    string WorkflowStage,
    int SprintCount,
    bool IsZombie,
    bool IsExcluded);

public record ZombieTrajectorySprintEntry(int SprintId, string SprintName, string FinalStatus);

public record ZombieTrajectoryEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    int SprintCount,
    List<ZombieTrajectorySprintEntry> Sprints);

public record CarryOverSingleSprintResult(
    SprintRef Sprint,
    CarryOverSingleSprintMetrics Metrics,
    List<CarryOverStatusDistributionEntry> StatusDistribution,
    List<CarryOverIssueTypeEntry> IssueTypeBreakdown,
    CarryOverDestination? CarryOverDestination,
    List<CarryOverTicketEntry> Tickets,
    List<ZombieTrajectoryEntry> ZombieTrajectories);
