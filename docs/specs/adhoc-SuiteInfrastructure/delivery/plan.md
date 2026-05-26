# Suite Infrastructure: gRPC Contracts + MassTransit In-Memory

**Feature Spec:** None (ad-hoc infrastructure, driven by `docs/proposals/suite-integration-architecture.md`)

## Context

The SprintRituals monorepo (`D:\src\sprint-rituals\`) has Identity + Fokus composed in a single Host but no inter-module communication layer. The proposal mandates gRPC (protobuf-net.Grpc, code-first) for synchronous cross-module calls and MassTransit (in-memory) for integration events. This plan delivers the communication infrastructure — contracts, server implementation, and event publishing — so Reflekt can consume them when ported.

## Scope

**In scope:**
- New `Blocks.Contracts` BuildingBlock — gRPC service interfaces + integration event DTOs
- gRPC server implementation in Fokus.API (anonymized sprint data)
- Kestrel h2c endpoint for gRPC
- MassTransit in-memory transport registration
- `SprintSynced` integration event published after sync

**Out of scope:**
- Reflekt port, gRPC client registration (comes with Reflekt)
- `HealthScoreCalculated` event (v3 per proposal)
- `FokusSprintSnapshot` projection (Reflekt consumer responsibility)
- Frontend changes

## Domain Model Changes

None — no new aggregates or entities. gRPC exposes existing data through an anonymization boundary.

## Data Model Changes

None — no new tables or migrations.

## Skill Mapping

| Step | Skill | Disposition |
|------|-------|-------------|
| 1 — Blocks.Contracts project | `create-building-blocks-package` | Scaffold csproj + GlobalUsings |
| 2 — gRPC service contracts | `create-grpc-contract` | Interface + data contract patterns |
| 3 — SprintSynced event | `add-integration-event` | Contract portion only (no consumer) |
| 4 — gRPC server implementation | `create-grpc-contract` | Server implementation section |
| 5 — Host gRPC wiring | `service-registration` | Host composition, MapGrpcService |
| 6 — MassTransit in-memory | `service-registration` | Host-level transport registration |
| 7 — Publish SprintSynced | `add-integration-event` | Publisher portion (domain event handler) |
| 8 — Solution + docs update | None | Manual |

## Implementation Steps

### Step 1: Create Blocks.Contracts project

Create a new BuildingBlock for app-specific cross-service contracts (gRPC interfaces + integration events).

**Files to create:**
- `src/BuildingBlocks/Blocks.Contracts/Blocks.Contracts.csproj`
- `src/BuildingBlocks/Blocks.Contracts/GlobalUsings.cs`

**csproj:** net10.0 class library, packages: `System.ServiceModel.Primitives`, `System.Runtime.Serialization.Primitives`

**Skill:** `create-building-blocks-package`

**Acceptance:** Project builds. No protobuf-net dependency in contracts (attributes only).

---

### Step 2: Define gRPC service contracts and data contracts

Define the two gRPC service interfaces and their request/response data contracts per the proposal's privacy boundary — team-level metrics only, no per-developer data.

**Files to create:**
- `src/BuildingBlocks/Blocks.Contracts/Grpc/ISprintAnalyticsService.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/ISprintCatalogService.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintRequest.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/TeamRequest.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/AnonymizedSprintSummary.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/DisruptionMetrics.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/CarryOverHistory.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintInfo.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintCatalogResponse.cs`

**Contract surface:**

```
ISprintAnalyticsService [ServiceContract]:
  GetSprintSummaryAsync(SprintRequest) → AnonymizedSprintSummary
  GetDisruptionMetricsAsync(SprintRequest) → DisruptionMetrics
  GetCarryOverHistoryAsync(TeamRequest) → CarryOverHistory

ISprintCatalogService [ServiceContract]:
  GetAvailableSprintsAsync(TeamRequest) → SprintCatalogResponse
```

**AnonymizedSprintSummary:** CompletionRate, DisruptionRate (combined scope+bug), CarryOverRate, HealthScore (composite 0-100), HealthRag (string), SpCompleted, ActiveSp, TopScopeAdditions (list: TicketKey, Summary, StoryPoints).

**DisruptionMetrics:** ScopeDisruptionRate, BugDisruptionRate, AddedSp, BugAddedSp, MidSprintAdditionCount.

**CarryOverHistory:** CarryOverRate (current sprint), CarryOverSp, ZombieTicketCount, SprintHistory (list: SprintName, CarryOverRate — last 4 sprints).

**SprintInfo:** SprintId, Name, StartDate, EndDate, State, Goal.

**SprintCatalogResponse:** List of SprintInfo.

**Privacy rule:** No developer names, no per-developer metrics, no assignee data crosses this boundary.

**Skill:** `create-grpc-contract` — contract section

**Acceptance:** Contracts compile. All types use `[DataContract]`/`[DataMember]`. Service interfaces use `[ServiceContract]`/`[OperationContract]`.

---

### Step 3: Define SprintSynced integration event

**Files to create:**
- `src/BuildingBlocks/Blocks.Contracts/IntegrationEvents/SprintSynced.cs`

**Shape:** `record SprintSynced(int SprintId, string SprintName, int BoardId, DateTime SyncedAt, string State)`

Plain record — no attributes needed. MassTransit uses convention-based serialization.

**Skill:** `add-integration-event` — contract portion

**Acceptance:** Record compiles. No MassTransit package dependency in contracts project (POCOs only).

---

### Step 4: Implement gRPC servers in Fokus.API

Implement both gRPC service interfaces, delegating to existing Fokus services and mapping to anonymized contracts.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`
- `src/Services/Fokus/Fokus.API/Grpc/SprintCatalogGrpcService.cs`

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — add `protobuf-net.Grpc.AspNetCore` package + `Blocks.Contracts` project reference
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — register gRPC services in `AddApiServices`

**Implementation:**
- `SprintAnalyticsGrpcService` injects `SprintSummaryService`, `ScopeChangeService`, `CarryOverService`, `SprintRepository`, `AppSettingsRepository`
- `SprintCatalogGrpcService` injects `SprintRepository`
- Maps internal rich response DTOs → slim anonymized data contracts (strip developer data, flatten to team-level metrics)

**Skill:** `create-grpc-contract` — server implementation section

**Acceptance:** Both services implement their interface. No per-developer data in any response. DI registration compiles.

---

### Step 5: Wire gRPC in Host

Configure Kestrel for h2c (HTTP/2 cleartext) on a second port and map gRPC services.

**Files to modify:**
- `src/Host/Host.csproj` — add `protobuf-net.Grpc.AspNetCore` package
- `src/Host/Program.cs` — add `AddCodeFirstGrpc()`, map both gRPC services after FastEndpoints
- `src/Host/appsettings.json` — add Kestrel endpoints section (HTTP 5000 + gRPC 5001)
- `src/Host/appsettings.Development.json` — dev gRPC port

**Kestrel config (appsettings.json):**
```json
"Kestrel": {
  "Endpoints": {
    "Http": { "Url": "http://localhost:5000", "Protocols": "Http1AndHttp2" },
    "Grpc": { "Url": "http://localhost:5001", "Protocols": "Http2" }
  }
}
```

**Program.cs additions:**
```csharp
builder.Services.AddCodeFirstGrpc();
// after UseFastEndpoints + MapEndpoints:
app.MapGrpcService<SprintAnalyticsGrpcService>();
app.MapGrpcService<SprintCatalogGrpcService>();
```

**Skill:** `service-registration` — Host composition section

**Acceptance:** Host builds. Kestrel starts on both ports (visible in startup logs).

---

### Step 6: Wire MassTransit in-memory in Host

Add MassTransit with in-memory transport. No consumers yet — just bus infrastructure.

**Files to modify:**
- `src/Host/Host.csproj` — add `MassTransit` package
- `src/Host/Program.cs` — add `AddMassTransit(x => x.UsingInMemory(...))` before module registrations
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — add `MassTransit.Abstractions` package (for `IPublishEndpoint`)

**Skill:** `service-registration` — Host-level registration

**Acceptance:** Host builds. MassTransit bus started log visible at startup.

---

### Step 7: Publish SprintSynced from Fokus sync operations

After a successful sprint sync, publish the integration event via `IPublishEndpoint`.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — inject `IPublishEndpoint`, publish `SprintSynced` after SaveChanges succeeds

**Publish location:** End of sync method, after EF SaveChanges. One event per sprint synced.

**Skill:** `add-integration-event` — publisher section

**Acceptance:** Build passes. Sprint sync publishes `SprintSynced` (observable via MassTransit Debug logging). Event carries correct SprintId, Name, BoardId, SyncedAt, State.

---

### Step 8: Register in solution + update CLAUDE.md

**Files to modify:**
- `SprintRituals.slnx` — add `Blocks.Contracts` project
- `CLAUDE.md` — update BuildingBlocks list (add Blocks.Contracts), update tech stack (gRPC and MassTransit no longer "future use")

**Acceptance:** `dotnet build src/SprintRituals.slnx` — zero errors, 12 projects (was 11).

## Testing Strategy

1. **Build:** `dotnet build src/SprintRituals.slnx` — 0 errors
2. **Runtime:** Start Host, verify dual-port Kestrel startup in logs (5000 HTTP + 5001 gRPC)
3. **MassTransit:** Verify bus started log entry
4. **gRPC:** Call `GetAvailableSprintsAsync` via grpcurl or integration test — verify response
5. **SprintSynced:** Trigger a sync, observe MassTransit publish log at Debug level
6. **Privacy boundary:** Review all DataContract types — confirm no developer names, no per-developer metrics

## Open Questions

None — all decisions resolved by the proposal.
