using Fokus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fokus.Persistence.Repositories;

public class SprintRepository(FokusDbContext db)
{
    public IQueryable<Sprint> Query() =>
        db.Sprints.Include(s => s.Memberships);

    public async Task<Sprint?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await Query().SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<Sprint>> GetAllAsync(CancellationToken ct = default) =>
        await db.Sprints.OrderByDescending(s => s.EndDate).ToListAsync(ct);

    public void Add(Sprint sprint) => db.Sprints.Add(sprint);

    public void Update(Sprint sprint) => db.Sprints.Update(sprint);

    public async Task UpsertAsync(Sprint sprint, CancellationToken ct = default)
    {
        var existing = await db.Sprints.SingleOrDefaultAsync(s => s.Id == sprint.Id, ct);
        if (existing is null)
        {
            db.Sprints.Add(sprint);
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
        var existing = await db.SprintMemberships
            .Where(sm => sm.SprintId == sprintId)
            .ToListAsync(ct);

        db.SprintMemberships.RemoveRange(existing);
        db.SprintMemberships.AddRange(memberships);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
