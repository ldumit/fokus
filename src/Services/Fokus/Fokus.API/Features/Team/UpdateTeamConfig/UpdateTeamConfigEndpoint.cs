namespace Fokus.API.Features.Team.UpdateTeamConfig;

[HttpPut("/api/developers/{accountId}/team-config")]
[Tags("Team")]
[Authorize(Policy = AccessPolicies.Admins)]
public class UpdateTeamConfigEndpoint(DeveloperRepository developerRepository)
    : Endpoint<UpdateTeamConfigCommand, UpdateTeamConfigResponse>
{
    public override async Task HandleAsync(UpdateTeamConfigCommand req, CancellationToken ct)
    {
        var developer = (await developerRepository.UpdateTeamConfigAsync(
            req.AccountId,
            req.Role,
            req.DefaultCapacityPercent,
            req.SubTeam,
            req.SubTeamProvided ?? false,
            req.IsActive,
            ct))
            .OrThrowNotFound($"Developer {req.AccountId} was not found.");

        await developerRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new UpdateTeamConfigResponse
        {
            AccountId = developer.Id,
            DisplayName = developer.DisplayName,
            AvatarUrl = developer.AvatarUrl,
            Role = developer.Role,
            DefaultCapacityPercent = developer.DefaultCapacityPercent,
            SubTeam = developer.SubTeam,
            IsActive = developer.IsActive
        }, ct);
    }
}
