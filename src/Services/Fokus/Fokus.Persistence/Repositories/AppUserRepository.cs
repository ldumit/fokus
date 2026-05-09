namespace Fokus.Persistence.Repositories;

public class AppUserRepository(FokusDbContext db)
{
    public async Task<AppUser?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default) =>
        await db.AppUsers.SingleOrDefaultAsync(u => u.GoogleId == googleId, ct);

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await db.AppUsers.SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);

    public async Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.AppUsers.SingleOrDefaultAsync(u => u.Id == id, ct);

    public async Task<List<AppUser>> GetAllAsync(CancellationToken ct = default) =>
        await db.AppUsers.OrderBy(u => u.DisplayName).ToListAsync(ct);

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await db.AppUsers.CountAsync(ct);

    public async Task<int> CountActiveAdminsAsync(CancellationToken ct = default) =>
        await db.AppUsers.CountAsync(u => u.Role == "Admin" && u.IsActive, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default) =>
        await db.AppUsers.AddAsync(user, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
