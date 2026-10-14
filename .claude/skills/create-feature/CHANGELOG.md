# create-feature — Changelog

## [Unreleased]
- Added Required Reading, Anti-patterns sections to SKILL.md (adhoc-PipelineCraftImprovements)
- Added Inputs/Outputs/Gate headers to all 6 workflow files (adhoc-PipelineCraftImprovements)
- `workflows/EndpointFastEndpoints.md`: admin-only endpoints use the named policy (`[Authorize(Policy = AccessPolicies.{PolicyName})]`, `Policies(...)`) instead of `Roles`; replies use FastEndpoints 8's `Send.*` API (`Send.OkAsync`, `Send.NoContentAsync`, `Send.ResponseAsync`) (F38-DotnetSkillAlignment)
- Whole folder re-based on the .NET plugin's text with this repo's form stated in a `This Repo's Form` section: endpoint-owned replies, hand-written `{Operation}Extensions.cs` maps (the mapping-library workflow is gone), Application capability classes and Domain calculators (F39-ServiceLayerSplit)

## [1.0.0] — 2026-05-24
- Baseline version (reconstructed from git history — last known modification: 27e1934 [Claude-Code] Skills cleanup)
- Variant-aware workflow split: FastEndpoints, Carter, Minimal APIs
- Workflow files: EndpointFastEndpoints.md, EndpointCarter.md, EndpointMinimalApi.md, Handler.md, Validator.md, Mappings.md
- Feature folder structure documented per framework variant
