namespace Fokus.Domain.Analytics.Shared;

public static class TestCoverageCalculator
{
    /// <summary>
    /// Active scope: non-removed, non-excluded feature tickets with effective SP that started in the sprint.
    /// </summary>
    public static HashSet<string> GetActiveFeatureTicketKeys(
        List<SprintMembership> memberships,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null) continue;
            if (m.Ticket?.IssueType == "Bug") continue;
            if (excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)) continue;
            if (!m.GetEffectiveSp(defaultSpPerBug).HasValue) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            if (TransitionAttributionChecker.IsStartedInSprint(ticketTransitions, sprintStart, sprintEnd, orderedStages, startIndex))
                result.Add(m.TicketId);
        }
        return result;
    }

    /// <summary>
    /// Builds a map from ticketKey -> list of TE IDs (Tests links only, from non-cancelled TEs).
    /// </summary>
    public static Dictionary<string, List<string>> BuildTestsTesByTicket(List<TestExecution> nonCancelledTEs)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var te in nonCancelledTEs)
        {
            foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Tests))
            {
                if (!result.TryGetValue(link.TicketKey, out var list))
                {
                    list = [];
                    result[link.TicketKey] = list;
                }
                if (!list.Contains(te.Id))
                    list.Add(te.Id);
            }
        }
        return result;
    }

    public static Dictionary<string, List<TestRun>> BuildRunsByTe(List<TestExecution> nonCancelledTEs) =>
        nonCancelledTEs.ToDictionary(te => te.Id, te => te.TestRuns.ToList());

    /// <summary>
    /// Coverage rate (BR1): feature tickets in active scope with at least one non-cancelled TE via Tests link.
    /// Sub-task inheritance (BR9): if a ticket has no own TE links, check its parent's links.
    /// </summary>
    public static (HashSet<string> CoveredKeys, decimal Rate) ComputeCoverageRate(
        HashSet<string> activeTicketKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        List<SprintMembership> memberships)
    {
        if (activeTicketKeys.Count == 0)
            return ([], 0);

        // Build parentKey lookup from memberships
        var parentByTicket = memberships
            .Where(m => m.Ticket?.ParentTicketKey != null)
            .ToDictionary(m => m.TicketId, m => m.Ticket!.ParentTicketKey!, StringComparer.OrdinalIgnoreCase);

        var coveredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in activeTicketKeys)
        {
            // Own links
            if (testsTesByTicket.ContainsKey(key) && testsTesByTicket[key].Count > 0)
            {
                coveredKeys.Add(key);
                continue;
            }
            // BR9: inherit from parent
            if (parentByTicket.TryGetValue(key, out var parentKey) &&
                !string.IsNullOrEmpty(parentKey) &&
                testsTesByTicket.TryGetValue(parentKey, out var parentTeIds) &&
                parentTeIds.Count > 0)
            {
                coveredKeys.Add(key);
            }
        }

        var rate = (decimal)coveredKeys.Count / activeTicketKeys.Count * 100;
        return (coveredKeys, Math.Round(rate, 1));
    }

    /// <summary>
    /// Execution rate (BR2): covered tickets where at least one linked TE has a Pass or Fail run.
    /// </summary>
    public static (HashSet<string> ExecutedKeys, decimal Rate) ComputeExecutionRate(
        HashSet<string> coveredKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        Dictionary<string, List<TestRun>> runsByTe)
    {
        if (coveredKeys.Count == 0)
            return ([], 0);

        var executedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in coveredKeys)
        {
            var teIds = testsTesByTicket.GetValueOrDefault(key, []);
            var hasExecutedRun = teIds.Any(teId =>
            {
                var runs = runsByTe.GetValueOrDefault(teId, []);
                return runs.Any(r => r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail);
            });
            if (hasExecutedRun)
                executedKeys.Add(key);
        }

        var rate = (decimal)executedKeys.Count / coveredKeys.Count * 100;
        return (executedKeys, Math.Round(rate, 1));
    }

    /// <summary>
    /// Pass rate (BR3): pass runs / (pass + fail runs) across all non-cancelled TEs.
    /// </summary>
    public static decimal ComputePassRate(List<TestExecution> nonCancelledTEs)
    {
        var passCount = 0;
        var totalCount = 0;
        foreach (var te in nonCancelledTEs)
        {
            foreach (var run in te.TestRuns)
            {
                if (run.Status == TestRunStatus.Pass) { passCount++; totalCount++; }
                else if (run.Status == TestRunStatus.Fail) { totalCount++; }
            }
        }
        return totalCount > 0 ? Math.Round((decimal)passCount / totalCount * 100, 1) : 0;
    }

    /// <summary>
    /// Bugs found (BR4): unique bug ticket keys linked via Blocks links from non-cancelled TEs.
    /// </summary>
    public static int ComputeBugsFound(List<TestExecution> nonCancelledTEs)
    {
        var bugKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var te in nonCancelledTEs)
        {
            foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Blocks))
            {
                if (link.Ticket?.IssueType == "Bug")
                    bugKeys.Add(link.TicketKey);
            }
        }
        return bugKeys.Count;
    }
}
