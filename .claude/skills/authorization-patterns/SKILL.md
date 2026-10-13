---
name: authorization-patterns
description: Endpoint authorization for a role-based domain — the two-layer role and resource gate (a closed role enum surfaced as string constants, both layers attached by one role-authorization extension), the authentication-only read-model exception, per-service resource access checks, the identity stamp for each endpoint style, and JWT validation; plus when to reach for ASP.NET Core policy-based authorization instead and a minimal single-principal shape. This repo's form (Google cookie sign-in, the AccessRole vocabulary, one role-only admin policy) is stated first. Use when gating an endpoint by role, adding a resource access check, choosing between the role and resource gate and a policy, stamping the acting user onto a command, or adding a role. Does not cover OAuth or OIDC sign-in middleware, client-side route guards, or 401 response interception.
user-invocable: true
---

# Authorization Patterns

Authorization is **declared on the endpoint**, never checked inside the logic. A write endpoint passes two gates: a
**role gate** (is the caller one of these roles?) and a **resource gate** (may this caller act on *this* order?).
A read model takes **authentication only**. The acting user is stamped onto the command by one framework hook, and
every service validates the caller the same way. The samples in Phases 1–5 use an ordering domain — orders, clerks,
managers; this repo's own form comes first.

## Assumes

- **A closed role vocabulary** every service shares — an enum surfaced as string constants. A repo with one service
  keeps it in that service; with several, in the lowest project they all reference.
- **The gate's helpers** — a named policy per role set at minimum; the role-authorization extension, its requirement
  and the access handler where the resource layer is used, all sampled below.
- **Each write-side service answers access from its own data** — a local table of who acts on what — never a
  synchronous call to another service.
- **A claims reader** for the identity stamp — a narrow interface over the caller's claims, so a class that needs the
  caller never takes `HttpContext`. This repo has none yet (§ This Repo's Form).

#### Look First

Before building anything a skill relies on — a guard helper, the central error mapper, a settings binder, an
aggregate or repository base, a save interceptor, an event publisher — look at what the repo already has: the shared
libraries under `src/BuildingBlocks/Blocks.*` (guards in `Blocks.Core`, the error family in `Blocks.Exceptions`, the
entity and aggregate bases in `Blocks.Domain`, the repository base and save interceptor in `Blocks.EntityFrameworkCore`,
the error middleware in `Blocks.AspNetCore`, the event publisher in `Blocks.FastEndpoints`), the module contracts
(`src/Modules/Jira/Jira.Contracts`, `src/Modules/Xray/Xray.Contracts`), and the packages in
`src/Directory.Packages.props`. Use what you find. Only otherwise build it once, in the shared place: the lowest
project every user of it already references; where the users share no project, a new `Blocks.*` project each of them
references (`create-building-blocks-package`), with no reference cycle. Never a copy beside each user.

## This Repo's Form

Fokus is one service used by one team. The caller signs in with Google; there is no per-resource ownership. So it
uses the role layer only, through one named policy. What applies here, and which general rules it departs from:

- **Sign-in: Google OAuth with a cookie,** configured in `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs`
  (`AddCookie` + `AddGoogle`). At sign-in the caller's `AppUser.Role` becomes a `ClaimTypes.Role` claim. An
  unauthenticated call gets 401 and a forbidden one 403 (the cookie's `OnRedirectToLogin` and
  `OnRedirectToAccessDenied` set the status instead of redirecting). **There is no JWT, so Phase 5 does not apply.**
- **The vocabulary:** the enum `AccessRole` (`Admin`, `Manager`) and its string constants `AccessRoles`, derived
  with `nameof`, in `src/Services/Fokus/Fokus.Domain/Auth/`. The names differ from Phase 1's `Role`/`Roles` on
  purpose: `Role.Admin` inside `AppUser` or `Invitation` binds to their `string Role` property, and `Roles.Admin`
  inside an endpoint binds to FastEndpoints' inherited `Roles(params string[])` method. An access role is not a
  developer's team role (`Developer.Role`). `AppUser.Role` and `Invitation.Role` stay `string`, written with the
  constants. A role name is written as an `AccessRoles` constant everywhere — never as a literal.
- **The gate: one named policy for admins,** `AccessPolicies.Admins` — a static class of `nameof` constants in
  `src/Services/Fokus/Fokus.API/Auth/AccessPolicies.cs`, registered once in `AuthConfiguration.cs` with
  `RequireRole(AccessRoles.Admin)`. Every admin-only endpoint carries `[Authorize(Policy = AccessPolicies.Admins)]`.
  Every other endpoint takes authentication only (FastEndpoints' default); the two anonymous ones say
  `AllowAnonymous` (sign-in and invitation validation).
- **Carved out here — binding rule 2 and Phase 2's last sentence.** "A bare role check never gates a write" and "a
  role-only admin write is not this case" do not apply: with one team and no per-resource ownership, a resource
  check would always pass, so admin writes are gated by role only. **Phase 3 does not apply** for the same reason.
- **Carved out here — the authentication-only read rule, for two reads.** The user list (`Auth/GetUsers`) and the
  invitation list (`Auth/GetInvitations`) are reads that stay admin-only, because they expose other people's
  accounts.
- **A target user's role is a business rule, not a caller check.** `Auth/UpdateRole` and `Auth/UpdateStatus` compare
  the *target* user's role (the last active admin cannot be demoted or deactivated). That comparison stays in the
  endpoint, written with an `AccessRoles` constant; binding rule 1 is about the caller.
- **The active-user check** is a global FastEndpoints pre-processor,
  `src/Services/Fokus/Fokus.API/Auth/ActiveUserPreProcessor.cs`: a signed-in caller whose account is gone or
  deactivated is rejected by throwing `UnauthorizedException` ("Account not found." / "Your account has been
  deactivated."), which the central mapper answers with 401 (`error-handling`).
- **Not adopted yet — the identity stamp (binding rule 4, Phase 4).** Phase 4 stays the rule. This repo has no stamp
  hook and no claims reader yet, so the two endpoints that need the caller read `ClaimTypes.NameIdentifier` from
  `HttpContext.User` directly: `Auth/CreateInvitation/CreateInvitationEndpoint.cs` and
  `Auth/GetMe/GetMeEndpoint.cs` (both under `src/Services/Fokus/Fokus.API/Features/`). Adding the hook is open work.

## Choosing your authorization model

The phases below teach one model — a stacked role and resource gate. Pick the model first:

- **Two-layer role and resource gate (Phases 1–5).** When the question is *"which roles, and does the caller act on
  this resource?"* — a closed role vocabulary plus per-resource ownership.
- **Policy-based authorization.** When the decision is a claim or attribute predicate — "has a verified-email
  claim", "is over a spend threshold" — use ASP.NET Core's named policies:
  `AddAuthorizationBuilder().AddPolicy(name, policy => …)` with an `IAuthorizationRequirement` and its handler,
  attached by `.RequireAuthorization(name)`. The role and resource gate is a policy too; do not build its machinery
  where one claim policy expresses the rule.

### Minimal shape — single principal

A service with one principal and no role vocabulary yet authenticates only — `.RequireAuthorization()` plus the
service's sign-in validation — and stamps the caller's id. Promote to the two-layer gate when a second role appears;
that is the three-edit recipe of Phase 1, not a rewrite.

## Binding rules

1. **No access check in the logic.** The gate is declared on the endpoint, with constants; a handler, an endpoint
   body or a domain method never inspects the caller's roles, never compares the caller with an owner, and never
   writes a 403 by hand.
2. **A write takes both layers.** Role-gated writes go through the role-authorization extension, which attaches the
   role gate and the resource requirement together. A bare role check or a bare `RequireAuthorization()` never
   gates a write. (Carved out in this repo — § This Repo's Form.)
3. **Resource access is answered from local data only.**
4. **Provenance is stamped, never trusted.** The acting user and the time are set by the framework hook, never read
   from the request body.

## Phase 1 — Role vocabulary

One closed enum, surfaced as string constants derived with `nameof`, so the two can never drift:

```csharp file=UserRole.cs
public enum UserRole
{
    Customer = 1,
    Clerk = 2,
    Manager = 3,
    Admin = 4
}
```

```csharp file=Role.cs
public static class Role
{
    public const string Customer = nameof(UserRole.Customer);
    public const string Clerk = nameof(UserRole.Clerk);
    public const string Manager = nameof(UserRole.Manager);
    public const string Admin = nameof(UserRole.Admin);
}
```

In this repo the pair is `AccessRole` / `AccessRoles` (§ This Repo's Form):

```csharp
public enum AccessRole
{
    Admin = 1,
    Manager = 2
}

public static class AccessRoles
{
    public const string Admin = nameof(AccessRole.Admin);
    public const string Manager = nameof(AccessRole.Manager);
}
```

**To add a role — three edits, in order:** the enum member, with a number never used before; the matching
constant as `nameof(…)` of the new member; the constant at the endpoint gate (in this repo, the policy that admits
it). The role name appears nowhere else.

## Phase 2 — Endpoint gate

One policy shape carries both layers: the role check, and the requirement the resource gate (Phase 3) answers, both
built from the same role set. The extension attaches it to a route, or registers it under a name.

```csharp file=AuthorizationExtensions.cs
public static class AuthorizationExtensions
{
    public static TBuilder RequireRoleAuthorization<TBuilder>(this TBuilder builder, params string[] roles)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(policy => policy.RequireRoleAndResourceAccess(roles));

    public static AuthorizationBuilder AddResourceAccessPolicy(this AuthorizationBuilder builder, string name, params string[] roles) =>
        builder.AddPolicy(name, policy => policy.RequireRoleAndResourceAccess(roles));

    private static AuthorizationPolicyBuilder RequireRoleAndResourceAccess(this AuthorizationPolicyBuilder policy, string[] roles) =>
        policy.RequireRole(roles).AddRequirements(new ResourceAccessRequirement(roles));
}
```

```csharp file=ResourceAccessRequirement.cs
public class ResourceAccessRequirement(IEnumerable<string> allowedRoles) : IAuthorizationRequirement
{
    public IReadOnlySet<string> AllowedRoles { get; } = allowedRoles.ToHashSet();
}
```

Endpoint style decides how it is attached:

- **Mediator style (minimal or Carter):** the fluent call on the route —
  `.RequireRoleAuthorization(Role.Clerk, Role.Manager)`.
- **FastEndpoints:** the named policy for the endpoint's role set in the authorization attribute —
  `[Authorize(Policy = Policy.Clerks)]`. Each role set an endpoint admits is one policy, registered once at start-up
  with its roles, so the role check and the resource requirement always carry the same set. A role set nobody uses
  yet gets its policy in the change that first uses it.

```csharp file=Policy.cs
public static class Policy
{
    public const string Clerks = nameof(Clerks);
    public const string ClerksAndManagers = nameof(ClerksAndManagers);
}
```

```csharp
services.AddAuthorizationBuilder()
    .AddResourceAccessPolicy(Policy.Clerks, Role.Clerk)
    .AddResourceAccessPolicy(Policy.ClerksAndManagers, Role.Clerk, Role.Manager);
```

In this repo the policy is role-only, so it is a plain `AddPolicy` (§ This Repo's Form):

```csharp
public static class AccessPolicies
{
    public const string Admins = nameof(Admins);
}

services.AddAuthorizationBuilder()
    .AddPolicy(AccessPolicies.Admins, policy => policy.RequireRole(AccessRoles.Admin));
```

```csharp
[HttpPut("/api/settings/board")]
[Authorize(Policy = AccessPolicies.Admins)]
public class SaveBoardEndpoint(...) : Endpoint<...>
```

**The one single-layer case: authentication-only read models.** A read-only view across many resources (a search,
a timeline) takes `.RequireAuthorization()` — any authenticated caller — because it is a read model, not a write
on one resource. Writes get role and resource; read models get authentication only. A role-only admin write is not
this case. (This repo departs from the last sentence, and keeps two reads admin-only — § This Repo's Form.)

## Phase 3 — Resource check

Not used in this repo (§ This Repo's Form); it is the pattern for a service whose callers act on resources they own.

The handler narrows the caller's roles to the set the requirement carries — the roles the endpoint admits, in both
styles — then asks the service's checker whether the caller reaches the resource named in the route. So a caller
holding Clerk and Manager on a Clerk-only endpoint reaches the checker as a Clerk: the Manager bypass applies only
where the endpoint admits managers. A requirement with no roles admits nobody.

```csharp file=ResourceAccessHandler.cs
using System.Security.Claims;

public class ResourceAccessHandler(IResourceAccessChecker _accessChecker)
    : AuthorizationHandler<ResourceAccessRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext
            || !int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return;

        var roles = context.User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(requirement.AllowedRoles.Contains)
            .Select(role => Enum.TryParse<UserRole>(role, out var parsed) ? parsed : (UserRole?)null)
            .OfType<UserRole>()
            .ToHashSet();

        if (roles.Count == 0)
            return;

        var resourceId = int.TryParse(httpContext.GetRouteValue(_accessChecker.RouteKey)?.ToString(), out var id)
            ? id
            : (int?)null;

        if (await _accessChecker.HasAccessAsync(resourceId, userId, roles, httpContext.RequestAborted))
            context.Succeed(requirement);
    }
}
```

The checker is the service's own: the handler is shared, and every write-side service supplies its implementation —
which is why it has an interface.

```csharp file=IResourceAccessChecker.cs
public interface IResourceAccessChecker
{
    string RouteKey { get; }

    Task<bool> HasAccessAsync(int? resourceId, int userId, IReadOnlySet<UserRole> roles, CancellationToken ct = default);
}
```

Every checker follows the same decision tree, with a bypass role set per service:

1. No resource id in the route → **true** (the endpoint is not resource-specific).
2. The caller holds the service's bypass role → **true**.
3. Else → is the caller an actor on this resource, by the service's own table?

```csharp file=OrderAccessChecker.cs
public class OrderAccessChecker(OrdersDbContext _dbContext) : IResourceAccessChecker
{
    public string RouteKey => "orderId";

    public async Task<bool> HasAccessAsync(int? resourceId, int userId, IReadOnlySet<UserRole> roles, CancellationToken ct = default)
    {
        if (resourceId is null || roles.Contains(UserRole.Manager))
            return true;

        return await _dbContext.OrderActors
            .AnyAsync(actor => actor.OrderId == resourceId && actor.UserId == userId, ct);
    }
}
```

Register the checker and the handler, scoped, in the service's registration:
`services.AddScoped<IResourceAccessChecker, OrderAccessChecker>()` and
`services.AddScoped<IAuthorizationHandler, ResourceAccessHandler>()`.

## Phase 4 — Identity stamping — pick by framework

Not adopted yet in this repo; it stays the rule (§ This Repo's Form).

The acting user's id and the time are stamped onto every command that carries an action (an `IAction` request —
`create-feature` owns the request shapes) by one hook per endpoint style. Endpoints never read the caller; a class
that needs the caller takes the narrow claims reader (`IClaimsProvider`), never the concrete holder or
`HttpContext`. The time comes from the injected clock.

| Endpoint style | Hook | Registered as |
|---|---|---|
| A mediator (Carter or minimal APIs) | `StampActionBehavior` | the first pipeline behaviour, before validation |
| FastEndpoints | `StampActionPreProcessor` | a global pre-processor |
| Adaptation outside the two styles: minimal APIs with no mediator | `StampActionFilter` | an endpoint filter on the route group |

```csharp file=StampActionBehavior.cs
public class StampActionBehavior<TRequest, TResponse>(IClaimsProvider _claimsProvider, TimeProvider _timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAction
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (_claimsProvider.TryGetUserId() is { } userId)
            request.ActingUserId = userId;

        request.ActedOn = _timeProvider.GetUtcNow();

        return await next();
    }
}
```

```csharp file=StampActionFilter.cs
public class StampActionFilter(IClaimsProvider _claimsProvider, TimeProvider _timeProvider) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var action in context.Arguments.OfType<IAction>())
        {
            if (_claimsProvider.TryGetUserId() is { } userId)
                action.ActingUserId = userId;

            action.ActedOn = _timeProvider.GetUtcNow();
        }

        return await next(context);
    }
}
```

```csharp file=StampActionPreProcessor.cs
public class StampActionPreProcessor : IGlobalPreProcessor
{
    public Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        if (context.Request is IAction action)
        {
            action.ActingUserId = context.HttpContext.Resolve<IClaimsProvider>().GetUserId();
            action.ActedOn = context.HttpContext.Resolve<TimeProvider>().GetUtcNow();
        }

        return Task.CompletedTask;
    }
}
```

The presence guard differs on purpose: the behaviour and the filter use `TryGetUserId()` and leave the id unset
for an anonymous request; the pre-processor calls `GetUserId()`, which throws when the claim is missing — use the
throwing form only where every request is authenticated. A handler may set the acting user itself only when it
holds an id the hook cannot know; that is the exception, never the rule.

## Phase 5 — JWT validation

Not used in this repo: the caller signs in with Google and is carried by a cookie (§ This Repo's Form).

Every service that takes bearer tokens validates them the same way, through one registration: issuer and signing key
are the trust boundary, the audience check is off for a system with one audience, and roles map from
`ClaimTypes.Role`, which the resource gate reads. The token settings are bound and validated at start-up like any
other settings (`service-registration` § Options Validation).

```csharp file=JwtAuthenticationRegistration.cs
using System.Security.Claims;
using System.Text;

public static class JwtAuthenticationRegistration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Value.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Secret)),
                ValidateAudience = false,
                RequireExpirationTime = true,
                RoleClaimType = ClaimTypes.Role
            });

        return services;
    }
}
```

## What this skill does NOT do

- **Issue or sign tokens** — that is the sign-in service's flow; this skill only validates a token.
- **Fill the actors table** a checker reads — integration-event consumers do (`add-integration-event`).
- **Implement the claims reader** — a small class over `IHttpContextAccessor`, built once where it is first needed.
- **Define the command base** — `create-feature` carries the request shapes; it is used here.
- **Tenant-scoped authorization** — the access check has no tenant dimension.
- **OAuth or OIDC sign-in middleware** — the interactive flow that mints the token or the cookie (here,
  `AuthConfiguration.cs`).
- **Client-side route guards, and 401 or 403 interception** — front-end concerns.
