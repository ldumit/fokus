namespace Fokus.API.Features.Analytics.GetSprintSummary;

[HttpGet("/api/analytics/sprint-summary")]
[Tags("Analytics")]
public class GetSprintSummaryEndpoint(
    SprintSummaryInputs sprintSummaryInputs,
    SprintSummaryCalculator sprintSummaryCalculator,
    QaMetricsCalculator qaMetricsCalculator)
    : Endpoint<GetSprintSummaryRequest, GetSprintSummaryResponse>
{
    public override async Task HandleAsync(GetSprintSummaryRequest req, CancellationToken ct)
    {
        var source = await sprintSummaryInputs.LoadAsync(req.SprintId, ct);
        if (source is null)
        {
            await Send.OkAsync(new GetSprintSummaryResponse(null, null, null, [], [], new FlagsDto([], null, [], false)), ct);
            return;
        }

        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;
        var selectedSprint = source.SelectedSprint;

        // Cross-cutting exclusion for the selected sprint
        var transitionIndex = StatusTransitionIndex.Build(source.StatusTransitions);
        var excludedIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
            selectedSprint, source.AllDevelopers, source.CapacityRecords, transitionIndex, source.Settings);
        var filteredActiveDevelopers = source.ActiveDevelopers.Where(d => !excludedIds.Contains(d.Id)).ToList();

        var quality = ComputeQuality(source, subTeam);

        // Exclusion applied inside the calculator via excludedIds
        var result = sprintSummaryCalculator.ComputeSummary(
            selectedSprint, source.WindowSprints, filteredActiveDevelopers, source.Settings, subTeam, source.AllEpicTickets,
            source.CapacityLookup, source.AllDevelopers, source.StatusTransitions, source.ZombieSprintCounts, excludedIds,
            quality.QualitySubScore, quality.QualityBreakdown, quality.HasQaData, quality.TestingCrunch);

        await Send.OkAsync(result.ToGetSprintSummaryResponse(), ct);
    }

    private sealed record SprintQuality(
        decimal? QualitySubScore,
        QualityBreakdownResult? QualityBreakdown,
        TestingCrunchFlag? TestingCrunch,
        bool HasQaData);

    // Quality sub-score and testing crunch when Xray is enabled (BR19)
    private SprintQuality ComputeQuality(SprintSummarySource source, string? subTeam)
    {
        if (source.SprintTes is null)
            return new SprintQuality(null, null, null, false);

        var selectedSprint = source.SelectedSprint;
        var qaResult = qaMetricsCalculator.ComputeQaMetrics(
            source.SprintTes,
            selectedSprint.Memberships.ToList(),
            source.StatusTransitions,
            selectedSprint,
            priorSprint: null,
            priorSprintTEs: null,
            priorMemberships: null,
            sparklineWindow: [],
            sparklineTEsBySprintId: [],
            sparklineMembershipsBySprintId: [],
            source.Settings,
            subTeam);

        var qualityBreakdown = new QualityBreakdownResult(
            qaResult.QualityBreakdown.CoverageScore,
            qaResult.QualityBreakdown.CoverageWeight,
            qaResult.QualityBreakdown.PassRateScore,
            qaResult.QualityBreakdown.PassRateWeight);

        var testingCrunch = TestTimelineCalculator.ComputeCrunchFlag(
            source.SprintTes,
            selectedSprint.Memberships.ToList(),
            selectedSprint,
            subTeam);

        return new SprintQuality(qaResult.QualitySubScore, qualityBreakdown, testingCrunch, HasQaData: true);
    }
}
