using Fokus.API.Features.Analytics;

namespace Fokus.API.Features.Sprints.GetTestTimeline;

[HttpGet("/api/sprints/{sprintId}/test-timeline")]
[Tags("Sprints")]
public class GetTestTimelineEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    TicketRepository ticketRepository,
    TestTimelineService testTimelineService)
    : Endpoint<GetTestTimelineRequest, TestTimelineResponse>
{
    public override async Task HandleAsync(GetTestTimelineRequest req, CancellationToken ct)
    {
        // 1. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 2. Load all closed sprints (lightweight, ascending)
        var closedSprints = await sprintRepository.GetClosedSprintsAsync(ct);
        var ascending = closedSprints.OrderBy(s => s.StartDate).ToList();

        // 3. Find requested sprint — 404 if not found
        var sprintInfo = ascending.FirstOrDefault(s => s.Id == req.SprintId);
        if (sprintInfo is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // 4. Xray disabled — return hasQaData=false
        if (!settings.XrayEnabled)
        {
            await SendOkAsync(BuildEmptyResponse(sprintInfo, settings), ct);
            return;
        }

        // 5. Load sprint with memberships
        var sprintLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([sprintInfo.Id], ct);
        var sprint = sprintLoaded[0];

        // 6. Load TEs for the sprint
        var sprintTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);

        // 7. No TEs → hasQaData=false
        var hasAnyRuns = sprintTEs.Any(te => te.TestRuns.Any());
        if (!hasAnyRuns)
        {
            await SendOkAsync(BuildEmptyResponse(sprintInfo, settings), ct);
            return;
        }

        // 8. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 9. Load status transitions for sprint tickets
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync([sprint.Id], ct);

        // 10. Resolve prior sprint for delta computation
        var selectedIndex = ascending.FindIndex(s => s.Id == sprint.Id);
        Sprint? priorSprint = null;
        List<TestExecution>? priorTEs = null;
        List<SprintMembership>? priorMemberships = null;

        if (selectedIndex > 0)
        {
            var priorInfo = ascending[selectedIndex - 1];
            var priorLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([priorInfo.Id], ct);
            priorSprint = priorLoaded[0];
            priorTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(priorSprint.Id, ct);
            priorMemberships = priorSprint.Memberships.ToList();
        }

        // 11. Compute and return
        var result = testTimelineService.ComputeTimeline(
            sprintTEs,
            sprint.Memberships.ToList(),
            statusTransitions,
            sprint,
            priorSprint,
            priorTEs,
            priorMemberships,
            settings,
            subTeam);

        await SendOkAsync(result, ct);
    }

    private static TestTimelineResponse BuildEmptyResponse(Sprint sprint, AppSettings settings) =>
        new(
            HasQaData: false,
            SprintStartDate: sprint.StartDate,
            SprintEndDate: sprint.EndDate,
            PlanningWindowDays: settings.PlanningWindowDays,
            BurnupData: [],
            ScopeChangeOverlay: [],
            TestingCrunch: new TestingCrunchResult(false, null, 0, 0, []),
            PostSprintTesting: new PostSprintTestingResult(false, null, 0, 0, []),
            UntestedAtClose: new UntestedAtCloseResult(false, 0, []),
            DevToTestGap: new DevToTestGapResult(null, null, null, []));
}
