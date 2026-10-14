namespace Fokus.API.Features.Analytics.GetQaTrends;

[HttpGet("/api/analytics/qa-trends")]
[Tags("Analytics")]
public class GetQaTrendsEndpoint(
    QaTrendsInputs qaTrendsInputs,
    BugRatioCalculator bugRatioCalculator,
    QaTrendsCalculator qaTrendsCalculator)
    : Endpoint<GetQaTrendsRequest, GetQaTrendsResponse>
{
    public override async Task HandleAsync(GetQaTrendsRequest req, CancellationToken ct)
    {
        var source = await qaTrendsInputs.LoadAsync(req.Last, ct);
        if (source is null)
        {
            await Send.OkAsync(new GetQaTrendsResponse(
                HasQaData: false,
                Sprints: [],
                QualityTrends: [],
                TestingVolume: [],
                DefectCorrelation: null), ct);
            return;
        }

        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        var bugRatioResult = bugRatioCalculator.ComputeMultiSprint(
            source.BugRatioTargetSprints, source.AllClosedSprints, source.ActiveDevelopers,
            source.Settings, source.BugRatioTransitions, subTeam);

        var bugRatioBySprintId = bugRatioResult.TeamMetrics.PerSprintTrend
            .ToDictionary(t => t.SprintId, t => t.BugRatioPercent);

        var result = qaTrendsCalculator.ComputeTrends(
            source.TargetSprints,
            source.TesBySprintId,
            source.MembershipsBySprintId,
            bugRatioBySprintId,
            source.StatusTransitions,
            source.Settings,
            subTeam);

        await Send.OkAsync(result.ToGetQaTrendsResponse(), ct);
    }
}
