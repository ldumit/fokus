---
name: persistence-patterns
description: EF Core persistence — repository pattern, entity configurations, DbContext setup, seed data, interceptors. Loaded when working with database entities or persistence infrastructure.
user-invocable: false
---

# Persistence Patterns (EF Core)

## Repository Pattern (3-Tier)

### Tier 1: Interface + Base
**Files:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/IRepository.cs`
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/Repository.cs`

`IRepository<TEntity, TKey>` — comprehensive interface: CRUD, batch, query, save.
`RepositoryBase<TContext, TEntity, TKey>` — generic EF implementation with smart defaults.

Key interface methods: `Query()` (returns `IQueryable`), `AddAsync()`, `SaveChangesAsync()`, `DeleteByIdAsync()` (raw SQL for required-property entities).
Additional `RepositoryBase` methods (not on `IRepository`): `UpsertAsync()`, `FindByIdAsync()`, `TableName`.

### Tier 2: Service-Level Repository
Each service defines a thin concrete `Repository<TEntity>` binding to its DbContext:
```csharp
public class Repository<TEntity>(SubmissionDbContext dbContext)
    : RepositoryBase<SubmissionDbContext, TEntity>(dbContext)
    where TEntity : class, IEntity<int>;
```

### Tier 3: Domain-Specific Repository
Custom queries with eager loading:
```csharp
public class ArticleRepository(SubmissionDbContext dbContext) : Repository<Article>(dbContext)
{
    public override IQueryable<Article> Query()
    {
        return base.Entity
            .Include(e => e.Actors)
                .ThenInclude(e => e.Person)
            .Include(e => e.Assets);
    }

    public async Task<Article?> GetFullArticleByIdAsync(int id, CancellationToken ct = default)
    {
        return await Query()
            .Include(e => e.Journal)
            .Include(e => e.SubmittedBy)
            .SingleOrDefaultAsync(e => e.Id == id, ct);
    }
}
```

### Extension: FindByIdOrThrowAsync
**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs`

Shared extension — do NOT reimplement per service:
```csharp
await repository.FindByIdOrThrowAsync(id);
```

### Repository Registration Variants

| Variant | How | Services |
|---------|-----|----------|
| Auto (assembly scan) | `AddDerivedTypesOf(typeof(Repository<>))` | Submission, Review |
| Manual | `AddScoped<ArticleRepository>()` per repo | Production, ArticleHub, Auth |

## Entity Configuration

### Base: EntityConfiguration<T>
**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs`

- `HasKey(e => e.Id)` + auto-calls `builder.SeedFromJsonFile()`
- `HasGeneratedId` virtual (default `true`) — override to `false` for natural keys

### Audited: AuditedEntityConfiguration<T>
**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/AuditedEntityConfiguration.cs`

Extends `EntityConfiguration<T>`. Three opt-in override points:
- `HasGeneratedId` — `ValueGeneratedOnAdd()` vs `ValueGeneratedNever()`
- `DefaultDateSql` — default `"GETUTCDATE()"`, override `"NOW() AT TIME ZONE 'UTC'"` for PostgreSQL
- `HasConcurrencyToken` — default `false`, set `true` for opt-in `RowVersion` shadow property

```csharp
public class ArticleEntityConfiguration : AuditedEntityConfiguration<Article>
{
    protected override bool HasConcurrencyToken => true;

    public override void Configure(EntityTypeBuilder<Article> builder)
    {
        base.Configure(builder);
        // custom configuration...
    }
}
```

### Value Object Mapping
Two approaches used in codebase:
- `OwnsOne` (Auth): `builder.OwnsOne(p => p.Email, ...)`
- `ComplexProperty` (Review): `builder.ComplexProperty(a => a.Email, ...)`

## DbContext Setup

**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/ApplicationDbContext.cs`

- `ApplicationDbContext<TDbContext>` base with in-memory cache helpers (`GetAllCached`, `GetByIdCached`)

**Variants:**
- `ApplicationDbContext<T>` — most services
- `IdentityDbContext<User, Role, int>` — Auth (required by ASP.NET Identity)

**Database engine variants:**
- SQL Server: `UseSqlServer(connectionString)` — Auth, Submission, Review, Production
- PostgreSQL: `UseNpgsql(connectionString)` — ArticleHub

## Seed Data (Dual Pattern)

### Master Data (schema-deployed via migrations)
**Files:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs`

Convention: drop a `Data/Master/{EntityName}.json` file → `EntityConfiguration` base auto-calls `builder.SeedFromJsonFile()` → becomes EF `HasData` → included in migrations. Zero code changes needed.

### Test/Dev Data (runtime, behind IsDevelopment guard)
**Files:**
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.Seed.cs`
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Seeding/ManualGenerateIdScope.cs`
- `src/Services/{Service}/*/Data/Test/Seed.cs` per service

Pattern per service:
```csharp
public static class Seed
{
    public static void SeedTestData(this IServiceProvider services)
    {
        services.SeedTestData<SubmissionDbContext>(context =>
        {
            context.SeedFromJsonFile<Person>();
            context.SeedFromJsonFile<Journal>();
            context.SeedFromJsonFile<Article>();
        });
    }
}
```

- Idempotent: checks `if (context.Set<T>().Any()) return`
- `ManualGenerateIdScope` wraps `SET IDENTITY_INSERT ON/OFF` when JSON has explicit IDs
- JSON file naming: `typeof(T).Name` must match filename exactly

**IMPORTANT:** JSON files must be marked in `.csproj`:
```xml
<ItemGroup>
  <None Update="Data\**\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Seed Variants

| Variant | Mechanism | Services |
|---------|-----------|----------|
| EF JSON | `context.SeedFromJsonFile<T>()` | Submission, Review, Production |
| Identity custom | `UserManager.CreateAsync()` (can't bypass Identity) | Auth |
| Redis JSON | `provider.SeedFromJson<T>(redisDb)` | Journals |

### Wiring in Program.cs
```csharp
app.Migrate<SubmissionDbContext>();
if (app.Environment.IsDevelopment())
    app.Services.SeedTestData();
```

## Interceptors

Registered in `Persistence/DependencyInjection.cs`:
```csharp
services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
```

See `domain-patterns` skill for interceptor variant details (standard vs transactional).
