using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.CapacityResolver;
using static Fokus.Domain.Analytics.Shared.IssueTypeChecker;
using static Fokus.Domain.Analytics.Shared.MetricCardFactory;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;

namespace Fokus.Domain.Analytics.Sprints;

public class SprintSummaryCalculator
{
    public SprintSummaryResult ComputeSummary(
        Sprint selectedSprint,
        List<Sprint> windowSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        string? subTeam,
        List<Ticket> allEpicTickets,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers,
        List<StatusTransition> statusTransitions,
        IReadOnlyDictionary<string, int> zombieSprintCounts,
        HashSet<string>? excludedDeveloperIds = null,
        decimal? qualitySubScore = null,
        QualityBreakdownResult? qualityBreakdown = null,
        bool hasQaData = false,
        TestingCrunchFlag? testingCrunch = null)
    {
        // C2: sub-team filtering + cross-cutting exclusion
        var selectedMemberships = FilterMemberships(selectedSprint.Memberships, subTeam, excludedDeveloperIds);
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);
        var defaultSpPerBug = settings.DefaultSpPerBug;

        // Resolve transition-based boundaries once for this request
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var transitionIndex = StatusTransitionIndex.Build(statusTransitions);

        // Identify the prior sprint from sorted window (ascending by StartDate)
        var sortedWindow = windowSprints.OrderBy(s => s.StartDate).ToList();

        var selectedIndex = sortedWindow.FindIndex(s => s.Id == selectedSprint.Id);
        var priorSprint = selectedIndex > 0 ? sortedWindow[selectedIndex - 1] : null;

        // Core metrics for selected sprint
        var selected = ComputeMetrics(
            selectedMemberships, transitionIndex, selectedSprint, orderedStages, startIndex, endIndex, settings);

        // Delta metrics (C1 pattern)
        SprintMetrics? prior = priorSprint is not null
            ? ComputeMetrics(
                FilterMemberships(priorSprint.Memberships, subTeam, excludedDeveloperIds),
                transitionIndex, priorSprint, orderedStages, startIndex, endIndex, settings)
            : null;

        // Health score — uses combined DisruptionRate (scope + bug) for continuity
        var healthScore = ComputeHealthScore(selected, settings.HealthThresholds, settings.HealthWeights,
            qualitySubScore, qualityBreakdown, settings.QualityHealthWeight, hasQaData);

        // Sparklines — window of up to 4 sprints ending at selected
        var sparklineWindow = sortedWindow.Take(selectedIndex + 1).TakeLast(4).ToList();

        var metrics = BuildMetrics(selected, prior,
            new SparklineSource(sparklineWindow, windowSprints, subTeam, transitionIndex, settings, excludedDeveloperIds));

        // Top epics
        var topEpics = ComputeTopEpics(
            selectedMemberships, transitionIndex, selectedSprint.StartDate, selectedSprint.EndDate,
            windowSprints, subTeam, completedStatuses, allEpicTickets, defaultSpPerBug, orderedStages, endIndex);

        // Leaderboard
        var leaderboard = ComputeLeaderboard(
            filteredDevelopers, selectedMemberships, transitionIndex, selectedSprint.StartDate, selectedSprint.EndDate,
            orderedStages, endIndex, settings.ExcludedFromScopeStatuses, defaultSpPerBug, capacityLookup, allDevelopers,
            selectedSprint.Id);

        // Flags
        var flags = ComputeFlags(
            selectedSprint, selectedMemberships, transitionIndex, windowSprints, subTeam, filteredDevelopers,
            orderedStages, endIndex, settings.ExcludedFromScopeStatuses, defaultSpPerBug, settings.PlanningWindowDays,
            zombieSprintCounts, excludedDeveloperIds, testingCrunch);

        return new SprintSummaryResult(ToSprintInfo(selectedSprint), healthScore, metrics, topEpics, leaderboard, flags);
    }

    private static SprintInfo ToSprintInfo(Sprint sprint) =>
        new(
            sprint.Id,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            (int)(sprint.EndDate - sprint.StartDate).TotalDays,
            sprint.SyncedAt);

    private static MetricsResult BuildMetrics(SprintMetrics selected, SprintMetrics? prior, SparklineSource sparklines) =>
        new(
            SpCompleted: BuildMetricCard(
                "SP Completed",
                selected.FeatureCompleted,
                $"{selected.FeatureCompleted:0.#}",
                prior is not null ? selected.FeatureCompleted - prior.FeatureCompleted : null,
                "neutral",
                BuildSparkline(sparklines, (m, t, s, cfg) => ComputeSpCompleted(m, t, s, cfg))),
            CompletionRate: BuildMetricCard(
                "Completion %",
                selected.CompletionRate,
                $"{selected.CompletionRate:0.#}%",
                prior is not null ? selected.CompletionRate - prior.CompletionRate : null,
                "positive-up",
                BuildSparkline(sparklines, (m, t, s, cfg) => ComputeCompletionRate(m, t, s, cfg))),
            ScopeDisruptionRate: BuildMetricCard(
                "Scope Disruption Rate",
                selected.ScopeDisruptionRate,
                $"{selected.ScopeDisruptionRate:0.#}%",
                prior is not null ? selected.ScopeDisruptionRate - prior.ScopeDisruptionRate : null,
                "positive-down",
                BuildSparkline(sparklines, (m, t, s, cfg) => ComputeScopeDisruptionRate(m, t, s, cfg))),
            BugDisruptionRate: BuildMetricCard(
                "Bug Disruption Rate",
                selected.BugDisruptionRate,
                $"{selected.BugDisruptionRate:0.#}%",
                prior is not null ? selected.BugDisruptionRate - prior.BugDisruptionRate : null,
                "positive-down",
                BuildSparkline(sparklines, (m, t, s, cfg) => ComputeBugDisruptionRate(m, t, s, cfg))),
            CarryOverRate: BuildMetricCard(
                "Carry-Over Rate",
                selected.CarryOverRate,
                $"{selected.CarryOverRate:0.#}%",
                prior is not null ? selected.CarryOverRate - prior.CarryOverRate : null,
                "positive-down",
                BuildSparkline(sparklines, (m, t, s, cfg) => ComputeCarryOverRate(m, t, s, cfg))),
            BugSpCompleted: selected.BugSpCompleted);

    // --- Core metrics computation (transition-based, feature-only per spec BR9, BR21) ---

    private record SprintMetrics(
        decimal ActiveSp,
        decimal CompletedSp,
        decimal AddedSp,
        decimal CarryOverSp,
        decimal CompletionRate,
        decimal DisruptionRate,
        decimal ScopeDisruptionRate,
        decimal BugDisruptionRate,
        decimal CarryOverRate,
        decimal FeatureCompleted,
        decimal BugSpCompleted);

    private static bool IsExcluded(SprintMembership m, List<string> excludedStatuses) =>
        excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase);

    private static SprintMetrics ComputeMetrics(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        AppSettings settings)
    {
        var sprintStart = sprint.StartDate;
        var sprintEnd = sprint.EndDate;
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;
        var planningCutoff = sprintStart.AddDays(settings.PlanningWindowDays);

        var activeSp = 0m;
        var completedSp = 0m;
        var addedSp = 0m;
        var carryOverSp = 0m;
        var bugAddedSp = 0m;
        var bugSpCompleted = 0m;

        foreach (var m in memberships)
        {
            if (m.RemovedAt != null) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;
            if (IsExcluded(m, excludedStatuses)) continue;

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, startIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, endIndex);
            var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, planningCutoff);

            if (IsBug(m))
            {
                if (isAdded) bugAddedSp += sp.Value;
                if (isCompleted) bugSpCompleted += sp.Value;
            }
            else
            {
                if (isStarted) activeSp += sp.Value;
                if (isCompleted) completedSp += sp.Value;
                if (isAdded) addedSp += sp.Value;
                if (TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted)) carryOverSp += sp.Value;
            }
        }

        // Spec BR21 formulas
        var completionRate = activeSp > 0 ? completedSp / activeSp * 100 : 0;
        var scopeDisruptionRate = activeSp > 0 ? addedSp / activeSp * 100 : 0;
        var bugDisruptionRate = activeSp > 0 ? bugAddedSp / activeSp * 100 : 0;
        var disruptionRate = scopeDisruptionRate + bugDisruptionRate;
        var carryOverRate = activeSp > 0 ? carryOverSp / activeSp * 100 : 0;

        return new SprintMetrics(activeSp, completedSp, addedSp, carryOverSp, completionRate, disruptionRate, scopeDisruptionRate, bugDisruptionRate, carryOverRate, completedSp, bugSpCompleted);
    }

    // --- Sparkline helpers (new signature: memberships, transitions, sprint, settings) ---

    private static decimal ComputeSpCompleted(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        return memberships
            .Where(m => m.RemovedAt == null && !IsBug(m) && !IsExcluded(m, excludedStatuses) && m.GetEffectiveSp(defaultSpPerBug).HasValue)
            .Sum(m =>
            {
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);
                return isCompleted ? m.GetEffectiveSp(defaultSpPerBug)!.Value : 0m;
            });
    }

    private static decimal ComputeCompletionRate(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        var activeSp = 0m;
        var completedSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);

            if (isStarted) activeSp += sp.Value;
            if (isCompleted) completedSp += sp.Value;
        }

        return activeSp > 0 ? completedSp / activeSp * 100 : 0;
    }

    private static decimal ComputeScopeDisruptionRate(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;
        var planningCutoff = sprint.StartDate.AddDays(settings.PlanningWindowDays);

        var activeSp = 0m;
        var addedSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, planningCutoff);

            if (isStarted) activeSp += sp.Value;
            if (isAdded) addedSp += sp.Value;
        }

        return activeSp > 0 ? addedSp / activeSp * 100 : 0;
    }

    private static decimal ComputeBugDisruptionRate(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;
        var planningCutoff = sprint.StartDate.AddDays(settings.PlanningWindowDays);

        // activeSp = feature active (denominator per spec BR21)
        var activeSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            if (isStarted) activeSp += sp.Value;
        }

        var bugAddedSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || !IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, planningCutoff);
            if (isAdded) bugAddedSp += sp.Value;
        }

        return activeSp > 0 ? bugAddedSp / activeSp * 100 : 0;
    }

    private static decimal ComputeCarryOverRate(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        AppSettings settings)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        var activeSp = 0m;
        var carryOverSp = 0m;
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || IsBug(m) || IsExcluded(m, excludedStatuses)) continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, startIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, transitionIndex, sprint.StartDate, sprint.EndDate, orderedStages, endIndex);

            if (isStarted) activeSp += sp.Value;
            if (TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted)) carryOverSp += sp.Value;
        }

        return activeSp > 0 ? carryOverSp / activeSp * 100 : 0;
    }

    // --- Health score computation (BR2, BR3, BR4, BR16-BR19) ---

    private static HealthScoreResult ComputeHealthScore(
        SprintMetrics metrics,
        HealthThresholdConfig thresholds,
        HealthWeightConfig weights,
        decimal? qualitySubScore = null,
        QualityBreakdownResult? qualityBreakdown = null,
        int qualityWeight = 0,
        bool hasQaData = false)
    {
        var completionScore = ScoreHigherIsBetter(metrics.CompletionRate, thresholds.CompletionGreen, thresholds.CompletionAmber);
        var disruptionScore = ScoreLowerIsBetter(metrics.DisruptionRate, thresholds.DisruptionGreen, thresholds.DisruptionAmber);
        var carryOverScore = ScoreLowerIsBetter(metrics.CarryOverRate, thresholds.CarryOverGreen, thresholds.CarryOverAmber);

        var deliverySum = weights.Completion + weights.Disruption + weights.CarryOver;

        // BR18 three-way branching:
        // - hasQaData && qualityWeight > 0: include quality in composite
        // - hasQaData && qualityWeight == 0 (observation mode): quality computed but not in composite
        // - !hasQaData: quality null, delivery-only composite
        decimal totalWeight;
        decimal compositeNumerator;

        if (hasQaData && qualityWeight > 0 && qualitySubScore.HasValue)
        {
            totalWeight = deliverySum + qualityWeight;
            compositeNumerator = completionScore * weights.Completion
                + disruptionScore * weights.Disruption
                + carryOverScore * weights.CarryOver
                + qualitySubScore.Value * qualityWeight;
        }
        else
        {
            totalWeight = deliverySum;
            compositeNumerator = completionScore * weights.Completion
                + disruptionScore * weights.Disruption
                + carryOverScore * weights.CarryOver;
        }

        var composite = totalWeight > 0 ? compositeNumerator / totalWeight : 0;

        composite = Math.Round(composite, 1);
        completionScore = Math.Round(completionScore, 1);
        disruptionScore = Math.Round(disruptionScore, 1);
        carryOverScore = Math.Round(carryOverScore, 1);

        // Quality fields: non-null whenever hasQaData (even observation mode), null when !hasQaData
        decimal? roundedQualitySubScore = null;
        string? qualityRag = null;
        if (hasQaData && qualitySubScore.HasValue)
        {
            roundedQualitySubScore = Math.Round(qualitySubScore.Value, 1);
            qualityRag = HealthScoreCalculator.CompositeRag(qualitySubScore.Value);
        }

        return new HealthScoreResult(
            composite, CompositeRag(composite),
            completionScore, MetricRag(metrics.CompletionRate, thresholds.CompletionGreen, thresholds.CompletionAmber, higherIsBetter: true),
            disruptionScore, MetricRag(metrics.DisruptionRate, thresholds.DisruptionGreen, thresholds.DisruptionAmber, higherIsBetter: false),
            carryOverScore, MetricRag(metrics.CarryOverRate, thresholds.CarryOverGreen, thresholds.CarryOverAmber, higherIsBetter: false))
        {
            QualitySubScore = roundedQualitySubScore,
            QualityRag = qualityRag,
            QualityBreakdown = hasQaData ? qualityBreakdown : null
        };
    }

    private static decimal ScoreHigherIsBetter(decimal value, decimal green, decimal amber) =>
        HealthScoreCalculator.ScoreHigherIsBetter(value, green, amber);

    private static decimal ScoreLowerIsBetter(decimal value, decimal green, decimal amber) =>
        HealthScoreCalculator.ScoreLowerIsBetter(value, green, amber);

    private static string CompositeRag(decimal score) =>
        HealthScoreCalculator.CompositeRag(score);

    private static string MetricRag(decimal value, decimal green, decimal amber, bool higherIsBetter) =>
        HealthScoreCalculator.MetricRag(value, green, amber, higherIsBetter);

    // --- Sparkline (refactored: carries transitions, sprint object, and settings) ---

    private static List<SparklinePoint> BuildSparkline(
        SparklineSource source,
        Func<List<SprintMembership>, StatusTransitionIndex, Sprint, AppSettings, decimal> valueSelector)
    {
        return source.Window.Select(s =>
        {
            var sprintData = source.AllWindowSprints.First(ws => ws.Id == s.Id);
            var memberships = FilterMemberships(sprintData.Memberships, source.SubTeam, source.ExcludedDeveloperIds);
            return new SparklinePoint(s.Name, Math.Round(valueSelector(memberships, source.TransitionIndex, sprintData, source.Settings), 1));
        }).ToList();
    }

    private sealed record SparklineSource(
        List<Sprint> Window,
        List<Sprint> AllWindowSprints,
        string? SubTeam,
        StatusTransitionIndex TransitionIndex,
        AppSettings Settings,
        HashSet<string>? ExcludedDeveloperIds);

    // --- Top epics (dual mode per spec BR16: sprint attribution = transition-based, epic progress = position-based) ---

    private static List<EpicProgress> ComputeTopEpics(
        List<SprintMembership> selectedMemberships,
        StatusTransitionIndex transitionIndex,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<Sprint> allSprints,
        string? subTeam,
        HashSet<string> completedStatuses,
        List<Ticket> allEpicTickets,
        int defaultSpPerBug,
        List<string> orderedStages,
        int endIndex)
    {
        // Sprint attribution: transition-based (endIndex boundary check)
        // Epic progress: position-based CurrentStatus (completedStatuses) — spec BR16 dual mode

        // Step 1: Group by EpicKey, sum SP for tickets with a qualifying completion transition during this sprint
        var thisSprintByEpic = selectedMemberships
            .Where(m => m.Ticket?.EpicKey != null && m.RemovedAt == null && m.GetEffectiveSp(defaultSpPerBug).HasValue)
            .Where(m =>
            {
                // Transition-based: first transition to endStage (or beyond) within sprint window
                var hasCompletion = transitionIndex.ForTicket(m.TicketId)
                    .Any(t => t.Timestamp >= sprintStart
                              && t.Timestamp <= sprintEnd
                              && TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= endIndex);
                return hasCompletion;
            })
            .GroupBy(m => m.Ticket!.EpicKey!)
            .Select(g => new
            {
                EpicKey = g.Key,
                EpicName = g.First().Ticket!.EpicName ?? g.Key,
                SpCompletedThisSprint = g.Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value)
            })
            .OrderByDescending(e => e.SpCompletedThisSprint)
            .Take(3)
            .ToList();

        // Step 2 (BR16): for each top epic, compute overall completion from ALL tickets with that epic key
        // using ticket current status — position-based, not sprint attribution
        var filteredEpicTickets = string.IsNullOrWhiteSpace(subTeam)
            ? allEpicTickets
            : allEpicTickets.Where(t => t.Assignee?.SubTeam == subTeam).ToList();

        return thisSprintByEpic.Select(e =>
        {
            var epicTickets = filteredEpicTickets
                .Where(t => t.EpicKey == e.EpicKey && t.GetEffectiveSp(defaultSpPerBug) != null)
                .ToList();

            var doneSp = epicTickets
                .Where(t => completedStatuses.Contains(t.CurrentStatus))
                .Sum(t => t.GetEffectiveSp(defaultSpPerBug) ?? 0m);

            var totalSp = epicTickets.Sum(t => t.GetEffectiveSp(defaultSpPerBug) ?? 0m);
            var completionPct = totalSp > 0 ? Math.Round(doneSp / totalSp * 100, 1) : 0;

            return new EpicProgress(
                e.EpicName,
                Math.Round(e.SpCompletedThisSprint, 1),
                Math.Round(totalSp, 1),
                Math.Round(doneSp, 1),
                completionPct);
        }).ToList();
    }

    // --- Leaderboard (transition-based completion) ---

    private static List<DeveloperSummary> ComputeLeaderboard(
        List<Developer> developers,
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers,
        int sprintId)
    {
        // Transition-based completed memberships
        var completed = memberships
            .Where(m => m.RemovedAt == null
                        && !excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)
                        && m.Ticket?.AssigneeId != null
                        && TransitionAttributionChecker.IsCompletedInSprint(
                            m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, endIndex).IsCompleted)
            .ToList();

        var completedByDev = completed
            .GroupBy(m => m.Ticket!.AssigneeId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        return developers
            .Select(d =>
            {
                var devCompleted = completedByDev.GetValueOrDefault(d.Id, new List<SprintMembership>());
                var featureSp = devCompleted.Where(m => m.Ticket?.IssueType != "Bug").Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                var bugSp = devCompleted.Where(m => m.Ticket?.IssueType == "Bug").Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                var featureTickets = devCompleted.Count(m => m.Ticket?.IssueType != "Bug");
                var bugTickets = devCompleted.Count(m => m.Ticket?.IssueType == "Bug");
                var totalSp = featureSp + bugSp;
                var capacityPercent = GetCapacity(capacityLookup, d.Id, sprintId, allDevelopers);
                return new DeveloperSummary(
                    d.DisplayName,
                    d.AvatarUrl,
                    d.SubTeam,
                    Math.Round(totalSp, 1),
                    Math.Round(featureSp, 1),
                    Math.Round(bugSp, 1),
                    featureTickets,
                    bugTickets,
                    capacityPercent);
            })
            .OrderByDescending(d => d.SpCompleted)
            .ThenBy(d => d.DisplayName)
            .ToList();
    }

    // --- Flags ---

    private static FlagsResult ComputeFlags(
        Sprint selectedSprint,
        List<SprintMembership> selectedMemberships,
        StatusTransitionIndex transitionIndex,
        List<Sprint> allSprints,
        string? subTeam,
        List<Developer> filteredDevelopers,
        List<string> orderedStages,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug,
        int planningWindowDays,
        IReadOnlyDictionary<string, int> zombieSprintCounts,
        HashSet<string>? excludedDeveloperIds = null,
        TestingCrunchFlag? testingCrunch = null)
    {
        // Zombie tickets (BR16): window tickets in 3+ analytics sprints across the whole history
        var zombies = allSprints
            .SelectMany(s => FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds))
            .GroupBy(m => m.TicketId)
            .Where(g => zombieSprintCounts.ContainsKey(g.Key))
            .Select(g =>
            {
                var first = g.First();
                return new ZombieTicket(
                    first.TicketId,
                    first.Ticket?.Summary ?? first.TicketId,
                    zombieSprintCounts[g.Key],
                    first.Ticket?.Assignee?.DisplayName);
            })
            .OrderByDescending(z => z.SprintCount)
            .ToList();

        // Mid-sprint disruption: added after planning window closes, not removed (uses planningWindowDays per spec)
        var disruptionCutoff = selectedSprint.StartDate.AddDays(planningWindowDays);
        var disruptedMemberships = selectedMemberships
            .Where(m => m.AddedAt > disruptionCutoff && m.RemovedAt == null)
            .ToList();

        MidSprintDisruption? midSprintDisruption = disruptedMemberships.Count > 0
            ? new MidSprintDisruption(
                Math.Round(disruptedMemberships.Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue).Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value), 1),
                disruptedMemberships.Count)
            : null;

        // Zero-SP developers (BR18): active devs who had work assigned but completed 0 SP (transition-based)
        var completedByDev = selectedMemberships
            .Where(m => m.RemovedAt == null
                        && m.GetEffectiveSp(defaultSpPerBug).HasValue
                        && !excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)
                        && m.Ticket?.AssigneeId != null
                        && TransitionAttributionChecker.IsCompletedInSprint(
                            m.TicketId, transitionIndex,
                            selectedSprint.StartDate, selectedSprint.EndDate,
                            orderedStages, endIndex).IsCompleted)
            .GroupBy(m => m.Ticket!.AssigneeId!)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value));

        var assigneesInSprint = selectedMemberships
            .Where(m => m.RemovedAt == null && m.Ticket?.AssigneeId != null)
            .Select(m => m.Ticket!.AssigneeId!)
            .ToHashSet();

        var zeroSpDevs = filteredDevelopers
            .Where(d => assigneesInSprint.Contains(d.Id) && completedByDev.GetValueOrDefault(d.Id, 0) == 0)
            .Select(d => d.DisplayName)
            .OrderBy(n => n)
            .ToList();

        var hasFlags = zombies.Count > 0 || midSprintDisruption is not null || zeroSpDevs.Count > 0 || testingCrunch is not null;

        return new FlagsResult(zombies, midSprintDisruption, zeroSpDevs, hasFlags)
        {
            TestingCrunch = testingCrunch
        };
    }
}
