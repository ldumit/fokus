using Blocks.EntityFrameworkCore.Repositories;

namespace Fokus.Persistence.Repositories;

public class SprintRepository(FokusDbContext db)
    : RepositoryBase<FokusDbContext, Sprint, int>(db)
{
    public override IQueryable<Sprint> Query() =>
        Entity.Include(s => s.Memberships);

    public async Task<Sprint?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await Query().SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<Sprint>> GetAllAsync(CancellationToken ct = default) =>
        await Entity.OrderByDescending(s => s.EndDate).ToListAsync(ct);

    public new async Task UpsertAsync(Sprint sprint, CancellationToken ct = default)
    {
        var existing = await Entity.SingleOrDefaultAsync(s => s.Id == sprint.Id, ct);
        if (existing is null)
        {
            Entity.Add(sprint);
        }
        else
        {
            existing.Name = sprint.Name;
            existing.StartDate = sprint.StartDate;
            existing.EndDate = sprint.EndDate;
            existing.BoardId = sprint.BoardId;
            existing.BoardName = sprint.BoardName;
            existing.State = sprint.State;
            existing.SyncedAt = sprint.SyncedAt;
        }
    }

    public async Task UpsertMembershipsAsync(int sprintId, List<SprintMembership> memberships, CancellationToken ct = default)
    {
        var existing = await DbContext.SprintMemberships
            .Where(sm => sm.SprintId == sprintId)
            .ToListAsync(ct);

        DbContext.SprintMemberships.RemoveRange(existing);
        DbContext.SprintMemberships.AddRange(memberships);
    }
}
