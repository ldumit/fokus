namespace Fokus.API.Features.Developers.SetCapacity;

[HttpPut("/api/developers/{accountId}/capacity")]
[Tags("Developers")]
[Authorize(Policy = AccessPolicies.Admins)]
public class SetCapacityEndpoint(DeveloperRepository developerRepository)
    : Endpoint<SetCapacityRequest, SetCapacityResponse>
{
    public override async Task HandleAsync(SetCapacityRequest req, CancellationToken ct)
    {
        (await developerRepository.GetByIdAsync(req.AccountId, ct))
            .OrThrowNotFound($"Developer {req.AccountId} was not found.");

        await developerRepository.UpsertCapacityAsync(req.AccountId, req.SprintId, req.CapacityPercent, ct);
        await developerRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new SetCapacityResponse
        {
            DeveloperAccountId = req.AccountId,
            SprintId = req.SprintId,
            CapacityPercent = req.CapacityPercent
        }, ct);
    }
}
