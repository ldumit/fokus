using Fokus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fokus.Persistence.Repositories;

public class DeveloperRepository(FokusDbContext db)
{
    public async Task<Developer?> GetByIdAsync(string accountId, CancellationToken ct = default) =>
        await db.Developers.SingleOrDefaultAsync(d => d.AccountId == accountId, ct);

    public async Task<List<Developer>> GetAllAsync(CancellationToken ct = default) =>
        await db.Developers.OrderBy(d => d.DisplayName).ToListAsync(ct);

    public void Add(Developer developer) => db.Developers.Add(developer);

    public void Update(Developer developer) => db.Developers.Update(developer);

    public async Task UpsertAsync(Developer developer, CancellationToken ct = default)
    {
        var existing = await db.Developers.SingleOrDefaultAsync(d => d.AccountId == developer.AccountId, ct);
        if (existing is null)
        {
            db.Developers.Add(developer);
        }
        else
        {
            existing.DisplayName = developer.DisplayName;
            existing.AvatarUrl = developer.AvatarUrl;
            existing.IsActive = developer.IsActive;
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
