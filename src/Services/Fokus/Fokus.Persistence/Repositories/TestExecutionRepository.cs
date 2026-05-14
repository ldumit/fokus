using Blocks.EntityFrameworkCore.Repositories;

namespace Fokus.Persistence.Repositories;

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
