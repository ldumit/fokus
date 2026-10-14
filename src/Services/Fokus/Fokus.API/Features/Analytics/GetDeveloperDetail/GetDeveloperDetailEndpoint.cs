using Jira.RestApi;
using Microsoft.Extensions.Options;

namespace Fokus.API.Features.Analytics.GetDeveloperDetail;

[HttpGet("/api/analytics/developer-detail/{accountId}")]
[Tags("Analytics")]
public class GetDeveloperDetailEndpoint(
    DeveloperDetailInputs developerDetailInputs,
    DeveloperDetailCalculator developerDetailCalculator,
    IOptions<JiraOptions> jiraOptions,
    TimeProvider timeProvider)
    : Endpoint<GetDeveloperDetailRequest, GetDeveloperDetailResponse>
{
    public override async Task HandleAsync(GetDeveloperDetailRequest req, CancellationToken ct)
    {
        var source = await developerDetailInputs.LoadAsync(req.AccountId, ct);
        var settings = source.Settings;

        // Excluded developer check: 0% capacity AND 0 completed in all closed sprints
        var transitionIndex = StatusTransitionIndex.Build(source.StatusTransitions);
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var hasAnyActivity = source.ClosedSprints.Any(sprint =>
        {
            var effectiveCapacity = source.CapacityLookup.TryGetValue(req.AccountId, out var sprintMap) &&
                                    sprintMap.TryGetValue(sprint.Id, out var cap)
                ? cap
                : source.Developer.DefaultCapacityPercent;

            if (effectiveCapacity > 0) return true;

            return sprint.Memberships.Any(m =>
            {
                if (m.Ticket?.AssigneeId != req.AccountId) return false;
                if (m.RemovedAt != null) return false;
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);
                return isCompleted;
            });
        });

        if (!hasAnyActivity)
            throw new NotFoundException($"Developer {req.AccountId} has no activity in closed sprints.");

        var active = await developerDetailInputs.LoadActiveSprintAsync(source.AnalyticsSprints, ct);
        var statusTransitions = active.ActiveSprint is null
            ? source.StatusTransitions
            : [..source.StatusTransitions, ..active.StatusTransitions];

        var result = developerDetailCalculator.ComputeDetail(
            req.AccountId,
            source.Developer,
            source.ClosedSprints,
            active.ActiveSprint,
            source.CapacityLookup,
            active.CapacityRecords,
            settings,
            statusTransitions,
            req.Last ?? 10,
            settings.BugRatioTarget,
            jiraOptions.Value.InstanceUrl,
            timeProvider.GetUtcNow().UtcDateTime.Date);

        await Send.OkAsync(result.ToGetDeveloperDetailResponse(), ct);
    }
}
