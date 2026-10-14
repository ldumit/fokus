namespace Fokus.Domain.Analytics.Sprints;

public record EpicProgressSummaryMetrics(
    int ActiveEpicCount,
    int CompletedEpicCount,
    decimal AverageCompletionPercentage);

public record TestRunSummary(
    int Passed,
    int Failed,
    int Todo,
    int Executing,
    int Aborted);

public record EpicProgressTicketEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    string? AssigneeDisplayName,
    bool IsDone,
    string? TestStatus,
    decimal? TestPassRate,
    int? TestBugsFound,
    TestRunSummary? TestRunSummary);

public record EpicProgressEntry(
    string EpicKey,
    string EpicName,
    int TotalTickets,
    int DoneTickets,
    int RemainingTickets,
    decimal TicketCompletionPercentage,
    decimal TotalSp,
    decimal DoneSp,
    decimal RemainingSp,
    decimal? ImputedSp,
    decimal? AdjustedTotalSp,
    decimal? SpCompletionPercentage,
    int UnestimatedTicketCount,
    decimal? Velocity,
    decimal? ProjectedSprintsRemaining,
    string? ProjectionConfidence,
    int ActiveSprintCount,
    DateOnly? StartedDate,
    DateOnly? LastWorkDate,
    bool IsCompleted,
    List<EpicProgressTicketEntry> Tickets,
    decimal? CoverageRate,
    decimal? PassRate,
    int BugsFound,
    int FeatureTicketCount,
    int CoveredTicketCount,
    string? CoverageRag,
    string? PassRateRag);

public record EpicProgressUnlinkedWork(
    int TicketCount,
    decimal TotalSp);

public record EpicProgressResult(
    EpicProgressSummaryMetrics SummaryMetrics,
    List<EpicProgressEntry> Epics,
    EpicProgressUnlinkedWork UnlinkedWork,
    bool HasQaData,
    decimal? AverageTestCoverage);

// --- QA data parameter type ---

public record EpicQaData(
    Dictionary<string, List<string>> TestsLinksByTicket,
    Dictionary<string, List<string>> BlocksLinksByTicket,
    Dictionary<string, List<TestRun>> RunsByTeId);
