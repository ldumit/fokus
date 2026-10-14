namespace Fokus.API.Features.Analytics.GetEpicProgress;

public class GetEpicProgressRequest
{
    public string? SubTeam { get; set; }
}

public class GetEpicProgressRequestValidator : Validator<GetEpicProgressRequest>
{
    public GetEpicProgressRequestValidator()
    {
        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public sealed record GetEpicProgressResponse(
    EpicProgressSummaryMetricsDto SummaryMetrics,
    IReadOnlyList<EpicProgressEntryDto> Epics,
    EpicProgressUnlinkedWorkDto UnlinkedWork,
    bool HasQaData,
    decimal? AverageTestCoverage);

public sealed record EpicProgressSummaryMetricsDto(
    int ActiveEpicCount,
    int CompletedEpicCount,
    decimal AverageCompletionPercentage);

public sealed record EpicProgressEntryDto(
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
    IReadOnlyList<EpicProgressTicketEntryDto> Tickets,
    decimal? CoverageRate,
    decimal? PassRate,
    int BugsFound,
    int FeatureTicketCount,
    int CoveredTicketCount,
    string? CoverageRag,
    string? PassRateRag);

public sealed record EpicProgressUnlinkedWorkDto(
    int TicketCount,
    decimal TotalSp);

public sealed record EpicProgressTicketEntryDto(
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
    TestRunSummaryDto? TestRunSummary);

public sealed record TestRunSummaryDto(
    int Passed,
    int Failed,
    int Todo,
    int Executing,
    int Aborted);
