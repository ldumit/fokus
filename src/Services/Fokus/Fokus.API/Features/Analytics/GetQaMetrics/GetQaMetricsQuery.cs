using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetQaMetrics;

public class GetQaMetricsRequest
{
    public int SprintId { get; set; }
    public string? SubTeam { get; set; }
}

public class GetQaMetricsRequestValidator : Validator<GetQaMetricsRequest>
{
    public GetQaMetricsRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public class QaMetricsResponse
{
    public bool HasQaData { get; set; }
    public MetricCardDto? CoverageRate { get; set; }
    public MetricCardDto? ExecutionRate { get; set; }
    public MetricCardDto? PassRate { get; set; }
    public int BugsFound { get; set; }
    public decimal QualitySubScore { get; set; }
    public QualityBreakdownResponse? QualityBreakdown { get; set; }
    public int UntestedCount { get; set; }
    public int FailingCount { get; set; }
}

public class QualityBreakdownResponse
{
    public decimal CoverageScore { get; set; }
    public int CoverageWeight { get; set; }
    public decimal PassRateScore { get; set; }
    public int PassRateWeight { get; set; }
}
