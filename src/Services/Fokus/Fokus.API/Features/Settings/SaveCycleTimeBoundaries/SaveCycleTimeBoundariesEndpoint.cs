namespace Fokus.API.Features.Settings.SaveCycleTimeBoundaries;

[HttpPut("/api/settings/cycle-time-boundaries")]
[Tags("Settings")]
[Authorize(Policy = AccessPolicies.Admins)]
public class SaveCycleTimeBoundariesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveCycleTimeBoundariesCommand, SaveCycleTimeBoundariesResponse>
{
    public override async Task HandleAsync(SaveCycleTimeBoundariesCommand req, CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);

        // Build ordered stage list: workflow stages + done statuses
        var orderedStages = new List<string>();
        orderedStages.AddRange(settings.WorkflowStages);
        orderedStages.AddRange(settings.DoneStatuses);

        var startIndex = orderedStages.IndexOf(req.StartStage);
        var endIndex = orderedStages.IndexOf(req.EndStage);

        if (startIndex < 0)
            throw new BadRequestException("StartStage does not match any configured workflow stage or done status.");

        if (endIndex < 0)
            throw new BadRequestException("EndStage does not match any configured workflow stage or done status.");

        if (startIndex >= endIndex)
            throw new BadRequestException("StartStage must precede EndStage in the configured order.");

        settings.CycleTimeStartStage = req.StartStage;
        settings.CycleTimeEndStage = req.EndStage;
        await repository.SaveAsync(settings, ct);

        await Send.OkAsync(new SaveCycleTimeBoundariesResponse(settings.CycleTimeStartStage, settings.CycleTimeEndStage), ct);
    }
}
