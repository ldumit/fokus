# Skill Backlog

## Skills Created

### extract-endpoint-types
- **Status:** Created
- **Type:** Gap
- **Source:** SyncRefactoring lessons
- **Description:** Splits endpoint files into endpoint + command/query sibling files (request, response, DTOs, validator)
- **Date:** 2026-05-08

### extract-feature-service
- **Status:** Created
- **Type:** Gap
- **Source:** SyncRefactoring lessons
- **Description:** Extracts shared handler logic into focused operation services named after the operation
- **Date:** 2026-05-08

### create-building-blocks-package
- **Status:** Created
- **Type:** Gap
- **Source:** ExtractJiraModule lessons
- **Description:** Scaffolds a new Blocks.{Name} shared library in BuildingBlocks with csproj and solution registration
- **Date:** 2026-05-08

## Skills Fixed

### persistence-patterns
- **Status:** Fixed
- **Type:** Fix
- **Source:** SkillAlignment lessons
- **Description:** Added warning that GetAllCached/GetByIdCached use ref params incompatible with async methods
- **Date:** 2026-05-08

### persistence-patterns
- **Status:** Fixed
- **Type:** Fix
- **Source:** SkillAlignment lessons
- **Description:** Added note that AuditedEntityConfiguration's HasDefaultValueSql requires Microsoft.EntityFrameworkCore.Relational package
- **Date:** 2026-05-08

### create-module
- **Status:** Fixed
- **Type:** Fix
- **Source:** ExtractJiraModule lessons
- **Description:** Added shared dependencies guidance to Component scaffold workflow for contracts that reference external types
- **Date:** 2026-05-08

## Skills Needed

### extract-feature-service (analytics variant)
- **Status:** Needed
- **Type:** Gap
- **Source:** SprintSummaryCard lessons
- **Description:** Existing skill focuses on extracting from endpoints. Analytics computation services are created fresh (not extracted) but follow the same naming/placement conventions. Skill should cover the "create fresh computation service" variant.
- **Date:** 2026-05-09

### create-standalone-service
- **Status:** Needed
- **Type:** Gap
- **Source:** Scaffolding lessons
- **Description:** Existing `create-service` assumes shared BuildingBlocks exist. For greenfield/standalone projects without shared infrastructure, a variant skill would avoid referencing nonexistent packages. Covers standalone csproj setup with direct NuGet packages, SQLite-first configuration.
- **Date:** 2026-05-09

## Vue Skills Created

### vue-patterns
- **Status:** Created
- **Type:** New
- **Source:** Learner consolidation (KIMJINWOO4, alexanderop, aliarghyani)
- **Description:** Vue 3 ecosystem — script setup macros, reactivity, composables, project conventions
- **Date:** 2026-05-09

### vue-component-architecture
- **Status:** Created
- **Type:** New
- **Source:** Learner consolidation (KIMJINWOO4, alexanderop)
- **Description:** Component level hierarchy (L0-L4), split decisions, composable lifecycle, store boundaries
- **Date:** 2026-05-09

### pinia-patterns
- **Status:** Created
- **Type:** New
- **Source:** Learner consolidation (aliarghyani, alexanderop)
- **Description:** Setup stores, storeToRefs, store vs view state, async patterns, composition
- **Date:** 2026-05-09

### tailwind-theme
- **Status:** Created
- **Type:** New
- **Source:** Learner consolidation (aliarghyani)
- **Description:** Tailwind CSS v4 theme patterns — design tokens, @theme directive, dark mode, component styling
- **Date:** 2026-05-09

### create-vue-feature
- **Status:** Created
- **Type:** New
- **Source:** Learner consolidation
- **Description:** Workflow skill — 8 steps to create a frontend feature slice (types, API, store, components, view, route, nav, build)
- **Date:** 2026-05-09

### frontend-review
- **Status:** Created
- **Type:** New
- **Source:** Learner consolidation (AppShell, SprintSummaryCard, WorkflowAutoDetection reviewer lessons)
- **Description:** 8-stage frontend review checklist — accessibility, reactive state, Tailwind tokens, component architecture, gap analysis
- **Date:** 2026-05-09
