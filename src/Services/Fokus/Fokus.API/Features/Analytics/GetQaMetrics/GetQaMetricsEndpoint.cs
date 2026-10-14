namespace Fokus.API.Features.Analytics.GetQaMetrics;

[HttpGet("/api/sprints/{sprintId}/qa-metrics")]
[Tags("Analytics")]
public class GetQaMetricsEndpoint(
    QaMetricsInputs qaMetricsInputs,
    QaMetricsCalculator qaMetricsCalculator)
    : Endpoint<GetQaMetricsRequest, QaMetricsResponse>
{
    public override async Task HandleAsync(GetQaMetricsRequest req, CancellationToken ct)
    {
        var source = await qaMetricsInputs.LoadAsync(req.SprintId, ct);
        if (source is null)
        {
            await Send.OkAsync(BuildEmptyResponse(), ct);
            return;
        }

        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        var result = qaMetricsCalculator.ComputeQaMetrics(
            source.SprintTes,
            source.SelectedSprint.Memberships.ToList(),
            source.StatusTransitions,
            source.SelectedSprint,
            source.PriorSprint,
            source.PriorTes,
            source.PriorMemberships,
            source.SparklineWindow,
            source.SparklineTesBySprintId,
            source.SparklineMembershipsBySprintId,
            source.Settings,
            subTeam);

        await Send.OkAsync(result.ToQaMetricsResponse(), ct);
    }

    private static QaMetricsResponse BuildEmptyResponse() => new()
    {
        HasQaData = false,
        CoverageRate = null,
        ExecutionRate = null,
        PassRate = null,
        BugsFound = 0,
        QualitySubScore = 0,
        QualityBreakdown = null,
        UntestedCount = 0,
        FailingCount = 0
    };
}
