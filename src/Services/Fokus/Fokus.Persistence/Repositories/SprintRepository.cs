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

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
