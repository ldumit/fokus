namespace Fokus.API.Features.Analytics.GetQaMetrics;

[HttpGet("/api/sprints/{sprintId}/qa-metrics")]
[Tags("Analytics")]
public class GetQaMetricsEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    TicketRepository ticketRepository,
    QaMetricsService qaMetricsService)
    : Endpoint<GetQaMetricsRequest, QaMetricsResponse>
{
    public override async Task HandleAsync(GetQaMetricsRequest req, CancellationToken ct)
    {
        // 1. Load settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 2. Load all closed sprints (lightweight)
        var closedSprints = await sprintRepository.GetClosedSprintsAsync(ct);

        // 3. Find the requested sprint
        var sprintInfo = closedSprints.FirstOrDefault(s => s.Id == req.SprintId);
        if (sprintInfo is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // 4. If Xray disabled, return hasQaData=false with empty metrics
        if (!settings.XrayEnabled)
        {
            await SendOkAsync(BuildEmptyResponse(), ct);
            return;
        }

        // 5. Build sparkline window: up to 4 sprints ending at selected (ascending)
        var ascending = closedSprints.OrderBy(s => s.StartDate).ToList();
        var selectedIndex = ascending.FindIndex(s => s.Id == req.SprintId);
        var windowIds = ascending
            .Take(selectedIndex + 1)
            .TakeLast(4)
            .Select(s => s.Id)
            .ToList();

        // 6. Bulk load sprints with memberships for the window
        var windowSprints = await sprintRepository.GetSprintsWithMembershipsAsync(windowIds, ct);
        var selectedSprint = windowSprints.First(s => s.Id == req.SprintId);

        // 7. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 8. Load status transitions for window sprint tickets
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(windowIds, ct);

        // 9. Load TEs for the selected sprint
        var sprintTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(req.SprintId, ct);

        // 10. Determine prior sprint and prior data
        var sortedWindow = windowSprints.OrderBy(s => s.StartDate).ToList();
        var selectedWindowIndex = sortedWindow.FindIndex(s => s.Id == req.SprintId);
        var priorSprint = selectedWindowIndex > 0 ? sortedWindow[selectedWindowIndex - 1] : null;

        List<TestExecution>? priorTEs = null;
        List<SprintMembership>? priorMemberships = null;
        if (priorSprint is not null)
        {
            priorTEs = await testExecutionRepository.GetTestExecutionsForSprintAsync(priorSprint.Id, ct);
            priorMemberships = priorSprint.Memberships.ToList();
        }

        // 11. Build sparkline TE and membership lookups for the window
        var sparklineWindow = sortedWindow.Take(selectedWindowIndex + 1).TakeLast(4).ToList();

        var sparklineTEsBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprint in sparklineWindow)
        {
            sparklineTEsBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
        }

        var sparklineMembershipsBySprintId = sparklineWindow
            .ToDictionary(s => s.Id, s => s.Memberships.ToList());

        // 12. Compute QA metrics
        var result = qaMetricsService.ComputeQaMetrics(
            sprintTEs,
            selectedSprint.Memberships.ToList(),
            statusTransitions,
            selectedSprint,
            priorSprint,
            priorTEs,
            priorMemberships,
            sparklineWindow,
            sparklineTEsBySprintId,
            sparklineMembershipsBySprintId,
            settings,
            subTeam);

        await SendOkAsync(MapResponse(result), ct);
    }

    private static QaMetricsResponse BuildEmptyResponse() => new()
    {
        HasQaData = false,
        CoverageRate = null,
        ExecutionRate = null,
        PassRate = null,
        BugsFound = 0,
        QualitySubScore = 0,
        QualityBreakdown = null,
        UntestedCount = 0,
        FailingCount = 0
    };

    private static QaMetricsResponse MapResponse(QaMetricsResult result) => new()
    {
        HasQaData = result.HasQaData,
        CoverageRate = result.CoverageRate,
        ExecutionRate = result.ExecutionRate,
        PassRate = result.PassRate,
        BugsFound = result.BugsFound,
        QualitySubScore = result.QualitySubScore,
        QualityBreakdown = result.QualityBreakdown is null ? null : new QualityBreakdownResponse
        {
            CoverageScore = result.QualityBreakdown.CoverageScore,
            CoverageWeight = result.QualityBreakdown.CoverageWeight,
            PassRateScore = result.QualityBreakdown.PassRateScore,
            PassRateWeight = result.QualityBreakdown.PassRateWeight
        },
        UntestedCount = result.UntestedCount,
        FailingCount = result.FailingCount
    };
}
