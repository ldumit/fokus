using Blocks.EntityFrameworkCore.Repositories;

namespace Fokus.Persistence.Repositories;

public record TicketCoverageInfo(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    string? ParentTicketKey,
    string IssueType,
    bool HasCoverage,
    int FailedRunCount,
    int TotalRunCount);

public class TestExecutionRepository(FokusDbContext db)
    : RepositoryBase<FokusDbContext, TestExecution, string>(db)
{
    public override IQueryable<TestExecution> Query() =>
        Entity
            .Include(t => t.Links)
            .Include(t => t.TestRuns);

    public async Task<List<TestExecution>> GetByIssueIdsAsync(List<string> issueIds, CancellationToken ct = default) =>
        await Query()
            .Where(t => issueIds.Contains(t.Id))
            .ToListAsync(ct);

    public new async Task UpsertAsync(TestExecution testExecution, CancellationToken ct = default)
    {
        var existing = Entity.Local.SingleOrDefault(t => t.Id == testExecution.Id)
            ?? await Entity.SingleOrDefaultAsync(t => t.Id == testExecution.Id, ct);

        if (existing is null)
        {
            Entity.Add(testExecution);
        }
        else
        {
            existing.IssueKey = testExecution.IssueKey;
            existing.Summary = testExecution.Summary;
            existing.Status = testExecution.Status;
            existing.AssigneeId = testExecution.AssigneeId;
            existing.CreatedDate = testExecution.CreatedDate;
        }
    }

    public async Task ReplaceLinksAsync(string testExecutionIssueId, List<TestExecutionLink> links, CancellationToken ct = default)
    {
        var existing = await DbContext.TestExecutionLinks
            .Where(l => l.TestExecutionIssueId == testExecutionIssueId)
            .ToListAsync(ct);

        DbContext.TestExecutionLinks.RemoveRange(existing);
        DbContext.TestExecutionLinks.AddRange(links);
    }

    public async Task ReplaceTestRunsAsync(string testExecutionIssueId, List<TestRun> runs, CancellationToken ct = default)
    {
        var existing = await DbContext.TestRuns
            .Where(r => r.TestExecutionIssueId == testExecutionIssueId)
            .ToListAsync(ct);

        DbContext.TestRuns.RemoveRange(existing);
        DbContext.TestRuns.AddRange(runs);
    }

    /// <summary>
    /// Returns TestExecutions (with Links and TestRuns) attributed to the given sprint per BR8.
    /// A TE belongs to a sprint when any of its TestExecutionLinks point to a ticket with a SprintMembership
    /// for that sprintId, AND sprintId is the maximum SprintId across all its linked tickets' memberships.
    /// Cancelled TEs (IsCancelled = true, i.e. Status == "Cancelled") are excluded per BR7.
    /// </summary>
    public async Task<List<TestExecution>> GetTestExecutionsForSprintAsync(int sprintId, CancellationToken ct = default)
    {
        // Find all TE IDs linked to this sprint
        var teIdsInSprint = await DbContext.TestExecutionLinks
            .Where(l => DbContext.SprintMemberships.Any(sm => sm.SprintId == sprintId && sm.TicketId == l.TicketKey))
            .Select(l => l.TestExecutionIssueId)
            .Distinct()
            .ToListAsync(ct);

        if (teIdsInSprint.Count == 0)
            return [];

        // For each TE, find the maximum SprintId across all its linked tickets' memberships
        // Only include TEs where sprintId == maxSprintId (BR8 tiebreaker)
        var teIdsForSprint = await DbContext.TestExecutionLinks
            .Where(l => teIdsInSprint.Contains(l.TestExecutionIssueId))
            .Join(DbContext.SprintMemberships,
                l => l.TicketKey,
                sm => sm.TicketId,
                (l, sm) => new { l.TestExecutionIssueId, sm.SprintId })
            .GroupBy(x => x.TestExecutionIssueId)
            .Where(g => g.Max(x => x.SprintId) == sprintId)
            .Select(g => g.Key)
            .ToListAsync(ct);

        if (teIdsForSprint.Count == 0)
            return [];

        // "Cancelled" matches TestExecution.IsCancelled computed property (Status == "Cancelled").
        // The raw string is required here because EF Core cannot translate the computed property to SQL.
        return await Entity
            .Where(te => teIdsForSprint.Contains(te.Id) && te.Status != "Cancelled")
            .Include(te => te.Links)
                .ThenInclude(l => l.Ticket)
            .Include(te => te.TestRuns)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns feature tickets in the sprint's active scope along with coverage and failure data.
    /// Active scope: not removed, not bug, not excluded status, has effective SP, and IsStartedInSprint.
    /// Coverage and failure data is computed using non-cancelled TEs linked via Tests links.
    /// Sub-task coverage inheritance (BR9): sub-tasks without own TE links inherit from their parent ticket.
    /// </summary>
    public async Task<List<TicketCoverageInfo>> GetFeatureTicketsWithCoverageAsync(
        int sprintId,
        List<string> excludedStatuses,
        List<string> orderedStages,
        int startIndex,
        DateTime sprintStart,
        DateTime sprintEnd,
        int defaultSpPerBug,
        CancellationToken ct = default)
    {
        // Load sprint memberships with full ticket and assignee data
        var memberships = await DbContext.SprintMemberships
            .Where(sm => sm.SprintId == sprintId && sm.RemovedAt == null)
            .Include(sm => sm.Ticket)
                .ThenInclude(t => t!.Assignee)
            .ToListAsync(ct);

        // Filter to feature tickets with effective SP, not excluded status
        var featureMemberships = memberships
            .Where(sm =>
                sm.Ticket != null &&
                sm.Ticket.IssueType != "Bug" &&
                !excludedStatuses.Contains(sm.FinalStatus, StringComparer.OrdinalIgnoreCase) &&
                GetEffectiveSp(sm, defaultSpPerBug) != null)
            .ToList();

        if (featureMemberships.Count == 0)
            return [];

        var ticketIds = featureMemberships.Select(sm => sm.TicketId).ToList();

        // Load status transitions for these tickets to apply IsStartedInSprint filter
        var transitions = await DbContext.StatusTransitions
            .Where(st => ticketIds.Contains(st.TicketId))
            .ToListAsync(ct);

        // Apply IsStartedInSprint filter (replicate logic inline — TransitionAttributionChecker is in API layer)
        var startedMemberships = featureMemberships
            .Where(sm =>
            {
                if (startIndex < 0) return false;
                var ticketTransitions = transitions
                    .Where(t => t.TicketId == sm.TicketId)
                    .ToList();
                return ticketTransitions.Any(t =>
                    t.Timestamp >= sprintStart &&
                    t.Timestamp <= sprintEnd &&
                    GetStageIndex(t.ToStatus, orderedStages) >= startIndex);
            })
            .ToList();

        if (startedMemberships.Count == 0)
            return [];

        var startedTicketIds = startedMemberships.Select(sm => sm.TicketId).ToList();

        // Load all non-cancelled TE links for these tickets (Tests links for coverage)
        var teLinks = await DbContext.TestExecutionLinks
            .Where(l => startedTicketIds.Contains(l.TicketKey))
            .Join(DbContext.TestExecutions.Where(te => te.Status != "Cancelled"),
                l => l.TestExecutionIssueId,
                te => te.Id,
                (l, te) => new { l.TicketKey, l.TestExecutionIssueId, l.LinkType })
            .ToListAsync(ct);

        // Load test runs for these TEs
        var teIdsWithLinks = teLinks.Select(l => l.TestExecutionIssueId).Distinct().ToList();
        var testRuns = teIdsWithLinks.Count > 0
            ? await DbContext.TestRuns
                .Where(r => teIdsWithLinks.Contains(r.TestExecutionIssueId))
                .ToListAsync(ct)
            : [];

        // Build coverage and run count per ticket
        var testsLinksByTicket = teLinks
            .Where(l => l.LinkType == TestExecutionLinkType.Tests)
            .GroupBy(l => l.TicketKey)
            .ToDictionary(g => g.Key, g => g.Select(l => l.TestExecutionIssueId).Distinct().ToList());

        var runsByTe = testRuns
            .GroupBy(r => r.TestExecutionIssueId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return startedMemberships.Select(sm =>
        {
            var ticketKey = sm.TicketId;
            var ticket = sm.Ticket!;

            // Get TE IDs for this ticket (own links)
            var ownTeIds = testsLinksByTicket.GetValueOrDefault(ticketKey, []);

            // BR9: if no own links, check parent ticket's links
            var effectiveTeIds = ownTeIds.Count > 0
                ? ownTeIds
                : (!string.IsNullOrEmpty(ticket.ParentTicketKey)
                    ? testsLinksByTicket.GetValueOrDefault(ticket.ParentTicketKey, [])
                    : []);

            var hasCoverage = effectiveTeIds.Count > 0;

            // Compute run counts across all effective TEs
            var failedRunCount = 0;
            var totalRunCount = 0;
            foreach (var teId in effectiveTeIds)
            {
                var runs = runsByTe.GetValueOrDefault(teId, []);
                failedRunCount += runs.Count(r => r.Status == TestRunStatus.Fail);
                totalRunCount += runs.Count(r => r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail);
            }

            return new TicketCoverageInfo(
                TicketKey: ticketKey,
                Summary: ticket.Summary,
                AssigneeName: ticket.Assignee?.DisplayName,
                StoryPoints: sm.StoryPoints,
                ParentTicketKey: ticket.ParentTicketKey,
                IssueType: ticket.IssueType,
                HasCoverage: hasCoverage,
                FailedRunCount: failedRunCount,
                TotalRunCount: totalRunCount);
        }).ToList();
    }

    private static decimal? GetEffectiveSp(SprintMembership sm, int defaultSpPerBug)
    {
        if (sm.StoryPoints.HasValue && sm.StoryPoints.Value > 0)
            return sm.StoryPoints;
        if (sm.Ticket?.IssueType == "Bug" && defaultSpPerBug > 0)
            return (decimal)defaultSpPerBug;
        return null;
    }

    private static int GetStageIndex(string status, List<string> orderedStages) =>
        orderedStages.FindIndex(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns the set of sprint IDs (from the provided list) that have at least one non-cancelled
    /// TestExecution attributed to that sprint per BR8 (max-sprint-id tiebreaker).
    /// Mirrors the semantics of GetTestExecutionsForSprintAsync so that sprint inclusion/exclusion
    /// decisions are consistent with TE loading.
    /// </summary>
    public async Task<HashSet<int>> GetSprintIdsWithQaDataAsync(List<int> sprintIds, CancellationToken ct = default)
    {
        if (sprintIds.Count == 0)
            return [];

        // Step 1: find all TE IDs linked to any ticket in the candidate sprints
        var teIdsInCandidates = await DbContext.TestExecutionLinks
            .Where(l => DbContext.SprintMemberships.Any(sm => sprintIds.Contains(sm.SprintId) && sm.TicketId == l.TicketKey))
            .Select(l => l.TestExecutionIssueId)
            .Distinct()
            .ToListAsync(ct);

        if (teIdsInCandidates.Count == 0)
            return [];

        // Step 2: for each TE, apply BR8 tiebreaker — the TE belongs to the sprint with the
        // maximum SprintId across all its linked tickets' memberships.
        // Only include TEs whose max sprint is in the candidate list and is not cancelled.
        var result = await DbContext.TestExecutionLinks
            .Where(l => teIdsInCandidates.Contains(l.TestExecutionIssueId))
            .Join(DbContext.SprintMemberships,
                l => l.TicketKey,
                sm => sm.TicketId,
                (l, sm) => new { l.TestExecutionIssueId, sm.SprintId })
            .GroupBy(x => x.TestExecutionIssueId)
            .Where(g => sprintIds.Contains(g.Max(x => x.SprintId)))
            .Join(DbContext.TestExecutions.Where(te => te.Status != "Cancelled"),
                g => g.Key,
                te => te.Id,
                (g, te) => g.Max(x => x.SprintId))
            .Distinct()
            .ToListAsync(ct);

        return [.. result];
    }

    /// <summary>
    /// Returns TE links and test runs for the given ticket keys, used for epic-level QA metric computation.
    /// Only non-cancelled TEs are included (Status != "Cancelled").
    /// Returns:
    ///   testsLinksByTicket  — Tests links grouped by ticket key (coverage)
    ///   blocksLinksByTicket — Blocks links grouped by ticket key (bugs found)
    ///   runsByTeId          — TestRuns grouped by TE issue ID
    /// </summary>
    public async Task<(
        Dictionary<string, List<string>> TestsLinksByTicket,
        Dictionary<string, List<string>> BlocksLinksByTicket,
        Dictionary<string, List<TestRun>> RunsByTeId)>
        GetTestExecutionDataForTicketsAsync(List<string> ticketKeys, CancellationToken ct = default)
    {
        if (ticketKeys.Count == 0)
            return ([], [], []);

        // Load all non-cancelled TE links for these tickets (both Tests and Blocks link types)
        var teLinks = await DbContext.TestExecutionLinks
            .Where(l => ticketKeys.Contains(l.TicketKey))
            .Join(DbContext.TestExecutions.Where(te => te.Status != "Cancelled"),
                l => l.TestExecutionIssueId,
                te => te.Id,
                (l, te) => new { l.TicketKey, l.TestExecutionIssueId, l.LinkType })
            .ToListAsync(ct);

        // Load test runs for all involved TEs in one query
        var teIds = teLinks.Select(l => l.TestExecutionIssueId).Distinct().ToList();
        var testRuns = teIds.Count > 0
            ? await DbContext.TestRuns
                .Where(r => teIds.Contains(r.TestExecutionIssueId))
                .ToListAsync(ct)
            : new List<TestRun>();

        var testsLinksByTicket = teLinks
            .Where(l => l.LinkType == TestExecutionLinkType.Tests)
            .GroupBy(l => l.TicketKey)
            .ToDictionary(g => g.Key, g => g.Select(l => l.TestExecutionIssueId).Distinct().ToList());

        var blocksLinksByTicket = teLinks
            .Where(l => l.LinkType == TestExecutionLinkType.Blocks)
            .GroupBy(l => l.TicketKey)
            .ToDictionary(g => g.Key, g => g.Select(l => l.TestExecutionIssueId).Distinct().ToList());

        var runsByTeId = testRuns
            .GroupBy(r => r.TestExecutionIssueId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return (testsLinksByTicket, blocksLinksByTicket, runsByTeId);
    }

    public async Task UpsertTestSetAsync(TestSet testSet, CancellationToken ct = default)
    {
        var existing = DbContext.TestSets.Local.SingleOrDefault(t => t.Id == testSet.Id)
            ?? await DbContext.TestSets.SingleOrDefaultAsync(t => t.Id == testSet.Id, ct);

        if (existing is null)
        {
            DbContext.TestSets.Add(testSet);
        }
        else
        {
            existing.IssueKey = testSet.IssueKey;
            existing.Summary = testSet.Summary;
            existing.AssigneeId = testSet.AssigneeId;
            existing.Status = testSet.Status;
        }
    }
}
