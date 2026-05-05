---
name: create-grpc-contract
description: Creates a gRPC code-first contract, server implementation, and client registration. Use when adding service-to-service synchronous communication.
---

# Create gRPC Contract

Creates a complete gRPC code-first contract with server and client. No .proto files — pure C# with `protobuf-net.Grpc`.

## Steps

1. **Create the contract** — follow `workflows/Contract.md`
2. **Create the server implementation** — follow `workflows/Server.md`
3. **Register the client** in consuming services — follow `workflows/Client.md`
4. **Verify the build:** `dotnet build`

## Arguments

Pass the service contract name: `/create-grpc-contract ArticleQueryService`

## Existing Contracts

| Contract | Host Service | Port | Location |
|----------|-------------|------|----------|
| IPersonService | Auth | 4401 | `Articles.Grpc.Contracts/Auth/PersonContracts.cs` |
| IJournalService | Journals | 4402 | `Articles.Grpc.Contracts/Journals/JournalContracts.cs` |

## Port Conventions

gRPC uses the same ports as HTTP. Services: 4401-4406 (HTTP), 4451-4456 (HTTPS).
