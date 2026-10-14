using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetQaTrends;

public class GetQaTrendsRequest
{
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetQaTrendsRequestValidator : Validator<GetQaTrendsRequest>
{
    public GetQaTrendsRequestValidator()
    {
        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be 0 (all) or a positive number.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public sealed record GetQaTrendsResponse(
    bool HasQaData,
    IReadOnlyList<SprintRefDto> Sprints,
    IReadOnlyList<QualityTrendEntryDto> QualityTrends,
    IReadOnlyList<TestingVolumeEntryDto> TestingVolume,
    DefectCorrelationDto? DefectCorrelation);

public sealed record QualityTrendEntryDto(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal PassRate,
    decimal ExecutionRate,
    string CoverageRag,
    string PassRateRag,
    string ExecutionRag);

public sealed record TestingVolumeEntryDto(
    int SprintId,
    string SprintName,
    int TeCount,
    int BugsFound);

public sealed record DefectCorrelationDto(
    IReadOnlyList<DefectCorrelationDataPointDto> DataPoints,
    decimal? PearsonR,
    int DataPointCount);

public sealed record DefectCorrelationDataPointDto(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal? NextSprintBugRatio,
    string? NextSprintName);
