namespace Fokus.API.Features.Analytics.GetDeveloperQuality;

[HttpGet("/api/analytics/developer-quality")]
[Tags("Analytics")]
public class GetDeveloperQualityEndpoint(
    DeveloperQualityInputs developerQualityInputs,
    DeveloperQualityCalculator developerQualityCalculator)
    : Endpoint<GetDeveloperQualityRequest, DeveloperQualityResponse>
{
    public override async Task HandleAsync(GetDeveloperQualityRequest req, CancellationToken ct)
    {
        var source = await developerQualityInputs.LoadAsync(req.SprintId, req.Last, ct);
        if (source is null)
        {
            await Send.OkAsync(new DeveloperQualityResponse { HasQaData = false, Sprints = [], Developers = [] }, ct);
            return;
        }

        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        var result = developerQualityCalculator.ComputeDeveloperQuality(
            source.AllLoadedSprints,
            source.TargetSprintIds,
            source.ActiveDevelopers,
            source.TesBySprintId,
            source.StatusTransitions,
            source.Settings,
            subTeam,
            source.IsSingleSprint,
            source.PriorSprint,
            source.PriorSprintTes,
            source.SparklineWindow,
            source.SparklineTesBySprintId,
            source.StreakWindow,
            source.StreakTesBySprintId);

        await Send.OkAsync(result.ToDeveloperQualityResponse(), ct);
    }
}
