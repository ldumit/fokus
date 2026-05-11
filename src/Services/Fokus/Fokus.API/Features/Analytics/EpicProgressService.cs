namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record EpicProgressSummaryMetrics(
    int ActiveEpicCount,
    int CompletedEpicCount,
    decimal AverageCompletionPercentage);

public record EpicProgressTicketEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    string? AssigneeDisplayName,
    bool IsDone);

public record EpicProgressEntry(
    string EpicKey,
    string EpicName,
    int TotalTickets,
    int DoneTickets,
    int RemainingTickets,
    decimal TicketCompletionPercentage,
    decimal TotalSp,
    decimal DoneSp,
    decimal RemainingSp,
    decimal? ImputedSp,
    decimal? AdjustedTotalSp,
    decimal? SpCompletionPercentage,
    int UnestimatedTicketCount,
    decimal? Velocity,
    decimal? ProjectedSprintsRemaining,
    string? ProjectionConfidence,
    int ActiveSprintCount,
    bool IsCompleted,
    List<EpicProgressTicketEntry> Tickets);

public record EpicProgressUnlinkedWork(
    int TicketCount,
    decimal TotalSp);

public record EpicProgressResponse(
    EpicProgressSummaryMetrics SummaryMetrics,
    List<EpicProgressEntry> Epics,
    EpicProgressUnlinkedWork UnlinkedWork);

// --- Service ---

public class EpicProgressService
{
    public EpicProgressResponse ComputeEpicProgress(
        List<Ticket> epicTickets,
        List<SprintMembership> allClosedMemberships,
        List<Ticket> unlinkedTickets,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        List<Sprint> closedSprints,
        string? subTeam)
    {
        var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);
        var defaultSpPerBug = settings.DefaultSpPerBug;

        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        // Build sprint date lookup for velocity transition checks
        var sprintDateLookup = closedSprints.ToDictionary(s => s.Id, s => (s.StartDate, s.EndDate));

        // BR13: sub-team filtering
        var filteredEpicTickets = FilterTickets(epicTickets, subTeam);
        var filteredUnlinkedTickets = FilterTickets(unlinkedTickets, subTeam);
        var filteredMemberships = FilterMemberships(allClosedMemberships, subTeam);

        // Group filtered tickets by EpicKey
        var epicGroups = filteredEpicTickets
            .GroupBy(t => t.EpicKey!)
            .ToList();

        var epicEntries = new List<EpicProgressEntry>();

        foreach (var group in epicGroups)
        {
            var epicKey = group.Key;
            var tickets = group.ToList();

            // Epic name: first ticket's EpicName ?? EpicKey (BR22)
            var epicName = tickets.First().EpicName ?? epicKey;

            // Done vs remaining (BR2)
            var doneTickets = tickets.Where(t => completedStatuses.Contains(t.CurrentStatus)).ToList();
            var remainingTickets = tickets.Where(t => !completedStatuses.Contains(t.CurrentStatus)).ToList();

            var totalTickets = tickets.Count;
            var doneCount = doneTickets.Count;
            var remainingCount = remainingTickets.Count;
            var ticketCompletionPct = totalTickets > 0
                ? Math.Round((decimal)doneCount / totalTickets * 100, 1)
                : 0m;

            // SP sums from estimated tickets (BR15)
            var totalSp = tickets
                .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

            var doneSp = doneTickets
                .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

            var remainingSp = remainingTickets
                .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

            // Unestimated ticket count (BR15)
            var unestimatedCount = tickets.Count(t => GetEffectiveTicketSp(t, defaultSpPerBug) == null);

            // Imputation (BR4, BR5, BR6): apply only to remaining unestimated tickets
            var estimatedTickets = tickets.Where(t => GetEffectiveTicketSp(t, defaultSpPerBug).HasValue).ToList();
            decimal? imputedSp = null;
            decimal? adjustedTotalSp = null;
            decimal? spCompletionPct = null;

            if (estimatedTickets.Count > 0)
            {
                var avgSp = estimatedTickets.Average(t => GetEffectiveTicketSp(t, defaultSpPerBug)!.Value);
                var remainingUnestimatedCount = remainingTickets.Count(t => GetEffectiveTicketSp(t, defaultSpPerBug) == null);
                imputedSp = remainingUnestimatedCount > 0
                    ? Math.Round(avgSp * remainingUnestimatedCount, 1)
                    : 0m;
                adjustedTotalSp = Math.Round(totalSp + imputedSp.Value, 1);

                // SP completion % (BR3, BR19)
                spCompletionPct = adjustedTotalSp.Value > 0
                    ? Math.Round(doneSp / adjustedTotalSp.Value * 100, 1)
                    : 0m;
            }

            // Velocity (BR7, BR8, BR16, BR24): from closed sprint memberships for this epic
            var epicMemberships = filteredMemberships
                .Where(sm => sm.Ticket?.EpicKey == epicKey)
                .ToList();

            var transitionsByTicket = statusTransitions
                .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // Group by SprintId, sum completed SP per sprint (transition-based — spec BR16 velocity)
            var sprintSpCompleted = epicMemberships
                .GroupBy(sm => sm.SprintId)
                .Select(g =>
                {
                    var sprintId = g.Key;
                    var sprintStartDate = g.First().Sprint?.StartDate ?? DateTime.MinValue;
                    var (sprintStart, sprintEnd) = sprintDateLookup.TryGetValue(sprintId, out var dates)
                        ? dates
                        : (sprintStartDate, sprintStartDate);

                    var completedSp = g
                        .Where(sm =>
                        {
                            if (sm.RemovedAt != null) return false;
                            var ticketTransitions = transitionsByTicket.GetValueOrDefault(sm.TicketId, []);
                            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                                sm.TicketId, ticketTransitions, sprintStart, sprintEnd, orderedStages, endIndex);
                            return isCompleted;
                        })
                        .Sum(sm => sm.GetEffectiveSp(defaultSpPerBug) ?? 0m);

                    return new { SprintId = sprintId, SprintStartDate = sprintStartDate, CompletedSp = completedSp };
                })
                .Where(x => x.CompletedSp > 0) // BR8: skip zero-progress sprints
                .OrderByDescending(x => x.SprintStartDate)
                .Take(3) // rolling 3-sprint window
                .ToList();

            decimal? velocity = null;
            if (sprintSpCompleted.Count > 0)
            {
                velocity = Math.Round(sprintSpCompleted.Average(x => x.CompletedSp), 1);
            }

            // Projection (BR9, BR10)
            decimal? projectedSprintsRemaining = null;
            string? projectionConfidence = null;

            if (velocity.HasValue && velocity.Value > 0)
            {
                var remainingWork = remainingSp + (imputedSp ?? 0m);
                projectedSprintsRemaining = Math.Round(remainingWork / velocity.Value, 1);

                // Confidence (BR10): low when fewer than 3 data points
                if (sprintSpCompleted.Count < 3)
                {
                    projectionConfidence = "low";
                }
            }

            // Active sprint count (BR17): distinct sprints where this epic had at least one ticket membership
            var activeSprintCount = epicMemberships
                .Select(sm => sm.SprintId)
                .Distinct()
                .Count();

            // Is completed (BR11): all tickets have done status
            var isCompleted = tickets.All(t => completedStatuses.Contains(t.CurrentStatus));

            // Ticket list — sort: remaining first (by current status for grouping), then done (BR spec Flow 2 step 3)
            var ticketEntries = remainingTickets
                .OrderBy(t => t.CurrentStatus)
                .Concat(doneTickets.OrderBy(t => t.CurrentStatus))
                .Select(t => new EpicProgressTicketEntry(
                    t.Id,
                    t.Summary,
                    t.IssueType,
                    t.StoryPoints,
                    t.CurrentStatus,
                    t.Assignee?.DisplayName,
                    completedStatuses.Contains(t.CurrentStatus)))
                .ToList();

            epicEntries.Add(new EpicProgressEntry(
                epicKey,
                epicName,
                totalTickets,
                doneCount,
                remainingCount,
                ticketCompletionPct,
                Math.Round(totalSp, 1),
                Math.Round(doneSp, 1),
                Math.Round(remainingSp, 1),
                imputedSp,
                adjustedTotalSp,
                spCompletionPct,
                unestimatedCount,
                velocity,
                projectedSprintsRemaining,
                projectionConfidence,
                activeSprintCount,
                isCompleted,
                ticketEntries));
        }

        // BR12: sort — active epics by SP completion % asc (null last), then completed epics by name asc
        var activeEpics = epicEntries
            .Where(e => !e.IsCompleted)
            .OrderBy(e => e.SpCompletionPercentage.HasValue ? 0 : 1)
            .ThenBy(e => e.SpCompletionPercentage ?? e.TicketCompletionPercentage)
            .ToList();

        var completedEpics = epicEntries
            .Where(e => e.IsCompleted)
            .OrderBy(e => e.EpicName)
            .ToList();

        var sortedEpics = activeEpics.Concat(completedEpics).ToList();

        // Summary metrics
        var activeEpicCount = activeEpics.Count;
        var completedEpicCount = completedEpics.Count;

        // Average completion: weighted by adjustedTotalSp (BR spec AC)
        decimal averageCompletionPct;
        var totalAdjustedSp = activeEpics
            .Where(e => e.AdjustedTotalSp.HasValue)
            .Sum(e => e.AdjustedTotalSp!.Value);

        if (totalAdjustedSp > 0)
        {
            var weightedSum = activeEpics
                .Where(e => e.AdjustedTotalSp.HasValue && e.SpCompletionPercentage.HasValue)
                .Sum(e => e.SpCompletionPercentage!.Value * e.AdjustedTotalSp!.Value);
            averageCompletionPct = Math.Round(weightedSum / totalAdjustedSp, 1);
        }
        else if (activeEpics.Count > 0)
        {
            // All active epics have null adjustedTotalSp — fall back to ticket-count average
            averageCompletionPct = Math.Round(
                activeEpics.Average(e => e.TicketCompletionPercentage), 1);
        }
        else
        {
            averageCompletionPct = 0m;
        }

        var summaryMetrics = new EpicProgressSummaryMetrics(
            activeEpicCount,
            completedEpicCount,
            averageCompletionPct);

        // Unlinked work (BR14)
        var unlinkedCount = filteredUnlinkedTickets.Count;
        var unlinkedTotalSp = filteredUnlinkedTickets
            .Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m);

        var unlinkedWork = new EpicProgressUnlinkedWork(
            unlinkedCount,
            Math.Round(unlinkedTotalSp, 1));

        return new EpicProgressResponse(summaryMetrics, sortedEpics, unlinkedWork);
    }

    // --- Effective SP for Ticket entities (mirrors SprintMembership.GetEffectiveSp) ---

    private static decimal? GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)
    {
        if (t.StoryPoints.HasValue && t.StoryPoints.Value > 0)
            return t.StoryPoints;
        if (t.IssueType == "Bug" && defaultSpPerBug > 0)
            return (decimal)defaultSpPerBug;
        return null;
    }

    // --- Sub-team filtering (BR13) ---

    private static List<Ticket> FilterTickets(List<Ticket> tickets, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return tickets;

        return tickets
            .Where(t => t.Assignee?.SubTeam == subTeam)
            .ToList();
    }

    private static List<SprintMembership> FilterMemberships(
        List<SprintMembership> memberships,
        string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return memberships;

        return memberships
            .Where(sm => sm.Ticket?.Assignee?.SubTeam == subTeam)
            .ToList();
    }
}
