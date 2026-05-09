namespace Fokus.API.Features.Sprints.GetClosedSprints;

[AllowAnonymous]
[HttpGet("/api/sprints/closed")]
[Tags("Sprints")]
public class GetClosedSprintsEndpoint(SprintRepository sprintRepository)
    : EndpointWithoutRequest<List<ClosedSprintItem>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var sprints = await sprintRepository.GetClosedSprintsAsync(ct);

        var response = sprints.Select(s => new ClosedSprintItem
        {
            Id = s.Id,
            Name = s.Name,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            State = s.State.ToString()
        }).ToList();

        await SendOkAsync(response, ct);
    }
}
