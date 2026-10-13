namespace Fokus.API.Features.Auth.RevokeInvitation;

[HttpDelete("/api/invitations/{id}")]
[Tags("Invitations")]
[Authorize(Policy = AccessPolicies.Admins)]
public class RevokeInvitationEndpoint(InvitationRepository invitationRepository)
    : Endpoint<RevokeInvitationRequest>
{
    public override async Task HandleAsync(RevokeInvitationRequest req, CancellationToken ct)
    {
        var invitation = (await invitationRepository.GetByIdAsync(req.Id, ct))
            .OrThrowNotFound($"Invitation {req.Id} was not found.");

        invitation.Revoke();
        await invitationRepository.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
