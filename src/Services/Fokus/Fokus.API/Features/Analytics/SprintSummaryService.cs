namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record SprintInfo(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    DateTime SyncedAt);

public record HealthScoreResult(
    decimal CompositeScore,
    string CompositeRag,
    decimal CompletionSubScore,
    string CompletionRag,
    decimal DisruptionSubScore,
    string DisruptionRag,
    decimal CarryOverSubScore,
    string CarryOverRag);

public record SparklinePoint(string SprintName, decimal Value);

public record MetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity,
    List<SparklinePoint> Sparkline);

public record MetricsResult(
    MetricCard SpCompleted,
    MetricCard CompletionRate,
    MetricCard DisruptionRate,
    MetricCard CarryOverRate);

public record EpicProgress(
    string EpicName,
    decimal SpCompletedThisSprint,
    decimal TotalSp,
    decimal DoneSp,
    decimal CompletionPercentage);

public record DeveloperSummary(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    decimal SpCompleted);

public record ZombieTicket(string TicketKey, string Summary, int SprintCount);

public record MidSprintDisruption(decimal TotalSp, int TicketCount);

public record FlagsResult(
    List<ZombieTicket> ZombieTickets,
    MidSprintDisruption? MidSprintDisruption,
    List<string> ZeroSpDevelopers,
    bool HasAnyFlags);

public record SprintSummaryResponse(
    SprintInfo? Sprint,
    HealthScoreResult? HealthScore,
    MetricsResult? Metrics,
    List<EpicProgress> TopEpics,
    List<DeveloperSummary> Leaderboard,
    FlagsResult Flags);

// --- Service ---

public class SprintSummaryService
{
    public SprintSummaryResponse ComputeSummary(
        Sprint selectedSprint,
        List<Sprint> windowSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        string? subTeam)
    {
        // C2: sub-team filtering
        var selectedMemberships = FilterMemberships(selectedSprint.Memberships, subTeam);
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var doneStatuses = settings.DoneStatuses;
        var thresholds = settings.HealthThresholds;
        var weights = settings.HealthWeights;

        // Identify the prior sprint from sorted window (ascending by StartDate)
        var sortedWindow = windowSprints
            .OrderBy(s => s.StartDate)
            .ToList();

        var selectedIndex = sortedWindow.FindIndex(s => s.Id == selectedSprint.Id);
        var priorSprint = selectedIndex > 0 ? sortedWindow[selectedIndex - 1] : null;
        var priorMemberships = priorSprint is not null
            ? FilterMemberships(priorSprint.Memberships, subTeam)
            : null;

        // Core metrics for selected sprint
        var selected = ComputeMetrics(selectedMemberships, doneStatuses);

        // Delta metrics (C1 pattern)
        SprintMetrics? prior = priorMemberships is not null
            ? ComputeMetrics(priorMemberships, doneStatuses)
            : null;

        // Health score
        var healthScore = ComputeHealthScore(selected, thresholds, weights);

        // Sparklines — window of up to 4 sprints ending at selected
        var sparklineWindow = sortedWindow
            .Take(selectedIndex + 1)
            .TakeLast(4)
            .ToList();

        var completionSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, doneStatuses, m => ComputeCompletionRate(m, doneStatuses));
        var spCompletedSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, doneStatuses, m => ComputeSpCompleted(m, doneStatuses));
        var disruptionSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, doneStatuses, m => ComputeDisruptionRate(m));
        var carryOverSparkline = BuildSparkline(sparklineWindow, windowSprints, subTeam, doneStatuses, m => ComputeCarryOverRate(m, doneStatuses));

        // Build metric cards
        var metrics = new MetricsResult(
            SpCompleted: BuildMetricCard(
                "SP Completed",
                selected.SpCompleted,
                $"{selected.SpCompleted:0.#}",
                prior is not null ? selected.SpCompleted - prior.SpCompleted : null,
                "neutral",
                spCompletedSparkline),
            CompletionRate: BuildMetricCard(
                "Completion %",
                selected.CompletionRate,
                $"{selected.CompletionRate:0.#}%",
                prior is not null ? selected.CompletionRate - prior.CompletionRate : null,
                "positive-up",
                completionSparkline),
            DisruptionRate: BuildMetricCard(
                "Disruption Rate",
                selected.DisruptionRate,
                $"{selected.DisruptionRate:0.#}%",
                prior is not null ? selected.DisruptionRate - prior.DisruptionRate : null,
                "positive-down",
                disruptionSparkline),
            CarryOverRate: BuildMetricCard(
                "Carry-Over Rate",
                selected.CarryOverRate,
                $"{selected.CarryOverRate:0.#}%",
                prior is not null ? selected.CarryOverRate - prior.CarryOverRate : null,
                "positive-down",
                carryOverSparkline));

        // Top epics
        var topEpics = ComputeTopEpics(selectedMemberships, windowSprints, subTeam, doneStatuses);

        // Leaderboard
        var leaderboard = ComputeLeaderboard(filteredDevelopers, selectedMemberships, doneStatuses);

        // Flags
        var flags = ComputeFlags(selectedSprint, selectedMemberships, windowSprints, subTeam, filteredDevelopers, doneStatuses);

        var sprintInfo = new SprintInfo(
            selectedSprint.Id,
            selectedSprint.Name,
            selectedSprint.StartDate,
            selectedSprint.EndDate,
            (int)(selectedSprint.EndDate - selectedSprint.StartDate).TotalDays,
            selectedSprint.SyncedAt);

        return new SprintSummaryResponse(sprintInfo, healthScore, metrics, topEpics, leaderboard, flags);
    }

    // --- Sub-team filtering (C2) ---

    private static List<SprintMembership> FilterMemberships(
        IReadOnlyList<SprintMembership> memberships,
        string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return memberships.ToList();

        return memberships
            .Where(m => m.Ticket?.Assignee?.SubTeam == subTeam)
            .ToList();
    }

    private static List<Developer> FilterDevelopers(List<Developer> developers, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return developers;

        return developers.Where(d => d.SubTeam == subTeam).ToList();
    }

    // --- Core metrics computation ---

    private record SprintMetrics(
        decimal SpCommitted,
        decimal SpCompleted,
        decimal SpAdded,
        decimal SpCarryOver,
        decimal CompletionRate,
        decimal DisruptionRate,
        decimal CarryOverRate);

    private static SprintMetrics ComputeMetrics(
        List<SprintMembership> memberships,
        List<string> doneStatuses)
    {
        var committed = memberships
            .Where(m => m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue)
            .Sum(m => m.StoryPoints!.Value);

        var completed = ComputeSpCompleted(memberships, doneStatuses);

        var added = memberships
            .Where(m => !m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue)
            .Sum(m => m.StoryPoints!.Value);

        var carryOver = memberships
            .Where(m => m.RemovedAt == null && m.StoryPoints.HasValue && !doneStatuses.Contains(m.FinalStatus))
            .Sum(m => m.StoryPoints!.Value);

        var completionRate = committed > 0 ? completed / committed * 100 : 0;
        var disruptionRate = committed > 0 ? added / committed * 100 : 0;
        var denominator = committed + added;
        var carryOverRate = denominator > 0 ? carryOver / denominator * 100 : 0;

        return new SprintMetrics(committed, completed, added, carryOver, completionRate, disruptionRate, carryOverRate);
    }

    private static decimal ComputeSpCompleted(List<SprintMembership> memberships, List<string> doneStatuses) =>
        memberships
            .Where(m => doneStatuses.Contains(m.FinalStatus) && m.RemovedAt == null && m.StoryPoints.HasValue)
            .Sum(m => m.StoryPoints!.Value);

    private static decimal ComputeCompletionRate(List<SprintMembership> memberships, List<string> doneStatuses)
    {
        var metrics = ComputeMetrics(memberships, doneStatuses);
        return metrics.CompletionRate;
    }

    private static decimal ComputeDisruptionRate(List<SprintMembership> memberships)
    {
        var committed = memberships
            .Where(m => m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue)
            .Sum(m => m.StoryPoints!.Value);
        var added = memberships
            .Where(m => !m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue)
            .Sum(m => m.StoryPoints!.Value);
        return committed > 0 ? added / committed * 100 : 0;
    }

    private static decimal ComputeCarryOverRate(List<SprintMembership> memberships, List<string> doneStatuses)
    {
        var metrics = ComputeMetrics(memberships, doneStatuses);
        return metrics.CarryOverRate;
    }

    // --- Health score computation (BR2, BR3, BR4) ---

    private static HealthScoreResult ComputeHealthScore(
        SprintMetrics metrics,
        HealthThresholdConfig thresholds,
        HealthWeightConfig weights)
    {
        var completionScore = ScoreHigherIsBetter(metrics.CompletionRate, thresholds.CompletionGreen, thresholds.CompletionAmber);
        var disruptionScore = ScoreLowerIsBetter(metrics.DisruptionRate, thresholds.DisruptionGreen, thresholds.DisruptionAmber);
        var carryOverScore = ScoreLowerIsBetter(metrics.CarryOverRate, thresholds.CarryOverGreen, thresholds.CarryOverAmber);

        var totalWeight = weights.Completion + weights.Disruption + weights.CarryOver;
        var composite = totalWeight > 0
            ? (completionScore * weights.Completion + disruptionScore * weights.Disruption + carryOverScore * weights.CarryOver) / totalWeight
            : 0;

        composite = Math.Round(composite, 1);
        completionScore = Math.Round(completionScore, 1);
        disruptionScore = Math.Round(disruptionScore, 1);
        carryOverScore = Math.Round(carryOverScore, 1);

        return new HealthScoreResult(
            composite, CompositeRag(composite),
            completionScore, MetricRag(metrics.CompletionRate, thresholds.CompletionGreen, thresholds.CompletionAmber, higherIsBetter: true),
            disruptionScore, MetricRag(metrics.DisruptionRate, thresholds.DisruptionGreen, thresholds.DisruptionAmber, higherIsBetter: false),
            carryOverScore, MetricRag(metrics.CarryOverRate, thresholds.CarryOverGreen, thresholds.CarryOverAmber, higherIsBetter: false));
    }

    // Higher is better: >= green -> 100; [amber, green) -> linear 50-99; < amber -> linear 0-49
    private static decimal ScoreHigherIsBetter(decimal value, decimal green, decimal amber)
    {
        if (value >= green) return 100;
        if (value >= amber)
        {
            var range = green - amber;
            if (range == 0) return 50;
            return 50 + (value - amber) / range * 49;
        }
        // below amber: linear 0-49, where 0=0 and amber=49
        if (amber == 0) return 0;
        return Math.Max(0, value / amber * 49);
    }

    // Lower is better: <= green -> 100; (green, amber] -> linear 99-50; > amber -> linear 49-0, hitting 0 at 2x amber
    private static decimal ScoreLowerIsBetter(decimal value, decimal green, decimal amber)
    {
        if (value <= green) return 100;
        if (value <= amber)
        {
            var range = amber - green;
            if (range == 0) return 50;
            return 99 - (value - green) / range * 49;
        }
        // above amber: linear 49-0, where amber=49 and 2*amber=0
        var cap = amber * 2;
        if (cap <= amber) return 0;
        return Math.Max(0, 49 - (value - amber) / (cap - amber) * 49);
    }

    private static string CompositeRag(decimal score) =>
        score >= 75 ? "green" : score >= 40 ? "amber" : "red";

    private static string MetricRag(decimal value, decimal green, decimal amber, bool higherIsBetter)
    {
        if (higherIsBetter)
            return value >= green ? "green" : value >= amber ? "amber" : "red";
        else
            return value <= green ? "green" : value <= amber ? "amber" : "red";
    }

    // --- Sparkline ---

    private static List<SparklinePoint> BuildSparkline(
        List<Sprint> window,
        List<Sprint> allWindowSprints,
        string? subTeam,
        List<string> doneStatuses,
        Func<List<SprintMembership>, decimal> valueSelector)
    {
        return window.Select(s =>
        {
            var sprintData = allWindowSprints.First(ws => ws.Id == s.Id);
            var memberships = FilterMemberships(sprintData.Memberships, subTeam);
            return new SparklinePoint(s.Name, Math.Round(valueSelector(memberships), 1));
        }).ToList();
    }

    // --- Metric card builder ---

    private static MetricCard BuildMetricCard(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity,
        List<SparklinePoint> sparkline)
    {
        string? direction = null;
        string? deltaPolarity = null;

        if (delta.HasValue)
        {
            direction = delta.Value > 0 ? "up" : delta.Value < 0 ? "down" : "flat";

            deltaPolarity = polarity switch
            {
                "positive-up" => delta.Value > 0 ? "positive" : delta.Value < 0 ? "negative" : "neutral",
                "positive-down" => delta.Value < 0 ? "positive" : delta.Value > 0 ? "negative" : "neutral",
                _ => "neutral"
            };
        }

        return new MetricCard(name, Math.Round(value, 1), displayValue, delta.HasValue ? Math.Round(delta.Value, 1) : null, direction, deltaPolarity, sparkline);
    }

    // --- Top epics ---

    private static List<EpicProgress> ComputeTopEpics(
        List<SprintMembership> selectedMemberships,
        List<Sprint> allSprints,
        string? subTeam,
        List<string> doneStatuses)
    {
        // Group by EpicKey in selected sprint, sum completed SP
        var thisSprintByEpic = selectedMemberships
            .Where(m => m.Ticket?.EpicKey != null && m.RemovedAt == null && m.StoryPoints.HasValue && doneStatuses.Contains(m.FinalStatus))
            .GroupBy(m => m.Ticket.EpicKey!)
            .Select(g => new
            {
                EpicKey = g.Key,
                EpicName = g.First().Ticket.EpicName ?? g.Key,
                SpCompletedThisSprint = g.Sum(m => m.StoryPoints!.Value)
            })
            .OrderByDescending(e => e.SpCompletedThisSprint)
            .Take(3)
            .ToList();

        // For each epic, compute overall progress across all provided sprints
        var allMembershipsFlat = allSprints
            .SelectMany(s => FilterMemberships(s.Memberships, subTeam))
            .ToList();

        return thisSprintByEpic.Select(e =>
        {
            var epicMemberships = allMembershipsFlat
                .Where(m => m.Ticket?.EpicKey == e.EpicKey && m.RemovedAt == null && m.StoryPoints.HasValue)
                .ToList();

            var doneSp = epicMemberships
                .Where(m => doneStatuses.Contains(m.FinalStatus))
                .Sum(m => m.StoryPoints!.Value);

            var totalSp = epicMemberships.Sum(m => m.StoryPoints!.Value);
            var completionPct = totalSp > 0 ? Math.Round(doneSp / totalSp * 100, 1) : 0;

            return new EpicProgress(
                e.EpicName,
                Math.Round(e.SpCompletedThisSprint, 1),
                Math.Round(totalSp, 1),
                Math.Round(doneSp, 1),
                completionPct);
        }).ToList();
    }

    // --- Leaderboard ---

    private static List<DeveloperSummary> ComputeLeaderboard(
        List<Developer> developers,
        List<SprintMembership> memberships,
        List<string> doneStatuses)
    {
        var completedByDev = memberships
            .Where(m => m.RemovedAt == null && m.StoryPoints.HasValue && doneStatuses.Contains(m.FinalStatus) && m.Ticket?.AssigneeId != null)
            .GroupBy(m => m.Ticket.AssigneeId!)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.StoryPoints!.Value));

        return developers
            .Select(d => new DeveloperSummary(
                d.DisplayName,
                d.AvatarUrl,
                d.SubTeam,
                Math.Round(completedByDev.GetValueOrDefault(d.Id, 0), 1)))
            .OrderByDescending(d => d.SpCompleted)
            .ThenBy(d => d.DisplayName)
            .ToList();
    }

    // --- Flags ---

    private static FlagsResult ComputeFlags(
        Sprint selectedSprint,
        List<SprintMembership> selectedMemberships,
        List<Sprint> allSprints,
        string? subTeam,
        List<Developer> filteredDevelopers,
        List<string> doneStatuses)
    {
        // Zombie tickets (BR16): tickets appearing in 3+ sprints
        var allMembershipsFlat = allSprints
            .SelectMany(s => FilterMemberships(s.Memberships, subTeam))
            .ToList();

        var zombies = allMembershipsFlat
            .GroupBy(m => m.TicketId)
            .Where(g => g.Select(m => m.SprintId).Distinct().Count() >= 3)
            .Select(g =>
            {
                var first = g.First();
                return new ZombieTicket(
                    first.TicketId,
                    first.Ticket?.Summary ?? first.TicketId,
                    g.Select(m => m.SprintId).Distinct().Count());
            })
            .OrderByDescending(z => z.SprintCount)
            .ToList();

        // Mid-sprint disruption (BR17): added > 2 days after sprint start, not removed
        var disruptionCutoff = selectedSprint.StartDate.AddDays(2);
        var disruptedMemberships = selectedMemberships
            .Where(m => m.AddedAt > disruptionCutoff && m.RemovedAt == null)
            .ToList();

        MidSprintDisruption? midSprintDisruption = disruptedMemberships.Count > 0
            ? new MidSprintDisruption(
                Math.Round(disruptedMemberships.Where(m => m.StoryPoints.HasValue).Sum(m => m.StoryPoints!.Value), 1),
                disruptedMemberships.Count)
            : null;

        // Zero-SP developers (BR18): active devs who had work assigned but completed 0 SP
        var completedByDev = selectedMemberships
            .Where(m => m.RemovedAt == null && m.StoryPoints.HasValue && doneStatuses.Contains(m.FinalStatus) && m.Ticket?.AssigneeId != null)
            .GroupBy(m => m.Ticket.AssigneeId!)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.StoryPoints!.Value));

        var assigneesInSprint = selectedMemberships
            .Where(m => m.RemovedAt == null && m.Ticket?.AssigneeId != null)
            .Select(m => m.Ticket.AssigneeId!)
            .ToHashSet();

        var zeroSpDevs = filteredDevelopers
            .Where(d => assigneesInSprint.Contains(d.Id) && completedByDev.GetValueOrDefault(d.Id, 0) == 0)
            .Select(d => d.DisplayName)
            .OrderBy(n => n)
            .ToList();

        var hasFlags = zombies.Count > 0 || midSprintDisruption is not null || zeroSpDevs.Count > 0;

        return new FlagsResult(zombies, midSprintDisruption, zeroSpDevs, hasFlags);
    }
}
