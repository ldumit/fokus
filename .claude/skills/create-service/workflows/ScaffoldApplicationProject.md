# Scaffold Application Project

**Conditional step.** Only run this workflow if the CLAUDE.md shared-state axis is **Y** — the service's logic outgrows its endpoints (root `CLAUDE.md`): loading that spans many repositories, a class around another system, cross-feature state (state machines, seeders), or a Features/ layer driven by MediatR. Pure rules and calculators are not a reason: they live in `.Domain`.

If shared-state = N, skip this workflow entirely. The service has no `.Application` project, and `.API` references `.Persistence` directly.

## Reference exemplars

- **MediatR services (Carter / Minimal APIs):** `src/Services/{Svc}/{Svc}.Application/` — full Features/{Aggregate}/{Feature} layout
- **FastEndpoints (no Features, no mediator):** `src/Services/Fokus/Fokus.Application/` — injected capability classes, one folder per capability (`Analytics/{Sprints,Developers,Qa}/` loaders, `Sync/`, `Xray/`); the endpoint stays the coordinator

## `{Name}.Application/GlobalUsings.cs`

**Reference:** `src/Services/{Svc}/{Svc}.Application/GlobalUsings.cs`

```csharp
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
global using Blocks.Core;
global using {Name}.Domain;
global using {Name}.Persistence;
```

Add MediatR usings (`global using MediatR;`) only for MediatR framework branches.

Add integration event usings (`global using {ProjectName}.Integration.Contracts;`, `global using MassTransit;`) only if events are published or consumed from this layer.

## `{Name}.Application/DependencyInjection.cs`

**Reference:** `src/Services/{Svc}/{Svc}.Application/DependencyInjection.cs`

```csharp
namespace {Name}.Application;

public static class DependencyInjection
{
    public static IServiceCollection Add{Name}Application(this IServiceCollection services, IConfiguration configuration)
    {
        // MediatR (only for Carter + MediatR or Minimal APIs + MediatR):
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DependencyInjection>());

        // Mappings (AutoMapper, Mapperly, or hand-written — check exemplar):
        // services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // State machines — registered by create-aggregate later

        return services;
    }
}
```

## Folder branches

### MediatR framework branch (Carter + MediatR, Minimal APIs + MediatR)

Keep all four folders:
- `{Name}.Application/Features/` — `create-feature` adds feature folders here
- `{Name}.Application/Dtos/`
- `{Name}.Application/StateMachines/`
- `{Name}.Application/Mappings/`

### FastEndpoints framework branch (no mediator)

Omit `Features/` — FastEndpoints collapses the feature slice into the endpoint class in `.API/Features/`. Create no folder up front: each capability gets its own folder when its first class lands (Fokus: `Analytics/{Area}/`, `Sync/`, `Xray/`). Add `StateMachines/` only if the service has a state machine. No `Mappings/` or `Dtos/`: an endpoint's reply types and its hand-written `To…()` mapping live beside the endpoint in `.API/Features/` (`create-feature`).

## What goes in `.Application` vs `.API/Features`

**In `.Application` only (FastEndpoints):**
- Injected capability classes, named for their job and never `…Service` unless they wrap another system: loaders that read many repositories for one endpoint (`{Operation}Inputs`), classes around another system (`…SyncService`)
- State machines (Stateless library)
- Seeders that depend on application services

**In `.Domain`, not `.Application`:** pure calculators, resolvers and checkers over loaded entities (`domain-service`).

**In `.Application` only (MediatR branch):**
- Cross-feature reusable DTOs
- State machines (Stateless library)
- AutoMapper / Mapperly profiles
- Shared enums or constants used by multiple features
- Seeders that depend on application services

**In `.API/Features/{Aggregate}/{Feature}`:**
- Commands and command handlers (MediatR branch) OR endpoint classes (FastEndpoints branch)
- Feature-local validators
- Feature-local DTOs that aren't reused

## After this step

The `.Application` project compiles as an empty shell with just DI wire-up. Next: `ScaffoldInfrastructure.md`.
