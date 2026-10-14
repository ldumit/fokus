namespace Fokus.API.Features.Analytics.GetQaWorkload;

[HttpGet("/api/analytics/qa-workload")]
[Tags("Analytics")]
public class GetQaWorkloadEndpoint(
    QaWorkloadInputs qaWorkloadInputs,
    QaWorkloadCalculator qaWorkloadCalculator)
    : Endpoint<GetQaWorkloadRequest, QaWorkloadResponseDto>
{
    public override async Task HandleAsync(GetQaWorkloadRequest req, CancellationToken ct)
    {
        var scope = await qaWorkloadInputs.LoadScopeAsync(req.SprintId, ct);
        if (scope is null)
        {
            await Send.OkAsync(new QaWorkloadResponseDto { HasQaData = false, Mode = "multi" }, ct);
            return;
        }

        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        if (scope.RequestedSprint is not null)
        {
            await SendSingleSprintAsync(scope, scope.RequestedSprint, subTeam, ct);
        }
        else
        {
            await SendMultiSprintAsync(scope, req.Last ?? 5, subTeam, ct);
        }
    }

    private async Task SendMultiSprintAsync(QaWorkloadScope scope, int last, string? subTeam, CancellationToken ct)
    {
        var source = await qaWorkloadInputs.LoadMultiSprintAsync(scope, last, ct);
        if (source is null)
        {
            await Send.OkAsync(new QaWorkloadResponseDto { HasQaData = false, Mode = "multi" }, ct);
            return;
        }

        var result = qaWorkloadCalculator.ComputeMultiSprint(
            source.TargetSprints,
            source.TesBySprintId,
            scope.AllDevelopers,
            source.AllClosedSprints,
            source.AllClosedTesBySprintId,
            scope.Settings,
            subTeam);

        await Send.OkAsync(result.ToQaWorkloadResponseDto(), ct);
    }

    private async Task SendSingleSprintAsync(QaWorkloadScope scope, Sprint targetSprintInfo, string? subTeam, CancellationToken ct)
    {
        var source = await qaWorkloadInputs.LoadSingleSprintAsync(scope, targetSprintInfo, ct);
        if (source is null)
        {
            await Send.OkAsync(new QaWorkloadResponseDto { HasQaData = false, Mode = "single" }, ct);
            return;
        }

        var result = qaWorkloadCalculator.ComputeSingleSprint(
            source.TargetSprint,
            source.TargetTes,
            source.PriorSprint,
            source.PriorTes,
            source.SparklineWindow,
            source.SparklineTesBySprintId,
            source.AllClosedSprints,
            source.AllClosedTesBySprintId,
            scope.AllDevelopers,
            scope.Settings,
            subTeam);

        await Send.OkAsync(result.ToQaWorkloadResponseDto(), ct);
    }
}
