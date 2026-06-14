# AGENTS.md

Guidance for Codex, Claude, and other coding agents working in the StateForge repository.

## Project Summary

StateForge is a resilient, file-backed session and state platform for .NET applications.

Implemented areas include:

- File-backed session/state storage
- ASP.NET Framework and ASP.NET Core providers
- STFG/STFG2 format support
- encryption and key-ring support
- maintenance and health tooling
- observability and Prometheus metrics
- sharding
- replication
- replication manifests
- snapshot services
- incremental snapshots
- replica promotion
- automatic failover
- replica catch-up and resynchronization
- replica lag monitoring
- production-readiness validation

## Current Version

Current repository version target: **0.35.0**

When making release changes, keep project versions and validation defaults aligned.

## PowerShell Requirements

All PowerShell scripts must remain compatible with **Windows PowerShell 5.1**.

Do not use:

- PowerShell 7-only features
- ternary operator `?:`
- null-coalescing operator `??`
- null-conditional operators `?.` or `?[]`
- `ForEach-Object -Parallel`

Use:

- `[CmdletBinding()]`
- `Set-StrictMode -Version 2.0`
- `$ErrorActionPreference = 'Stop'`
- explicit parameter validation
- clear error messages
- paths that work from the repository root

## .NET Requirements

The repo uses a mix of:

- `netstandard2.0`
- `net481`
- `net8.0`

Keep target frameworks consistent with nearby projects. Avoid new dependencies unless necessary.

## Preferred Validation

Minimum for most changes:

```powershell
.\scripts\Build-StateForge.ps1
.\scripts\Test-StateForge.ps1 -Suite Source
.\scripts\Test-StateForge.ps1 -Suite Production
```

Docs-only changes:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Docs
.\scripts\Test-StateForge.ps1 -Suite Source
```

Replication/catch-up changes:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Replication
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForge.ps1 -Suite Production
```

Snapshot/failover changes:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Snapshots
.\scripts\Test-StateForge.ps1 -Suite Recovery
.\scripts\Test-StateForge.ps1 -Suite Production
```

## Script Policy

Use these primary entry points:

```powershell
.\scripts\Build-StateForge.ps1
.\scripts\Test-StateForge.ps1
.\scripts\Invoke-StateForge.ps1
.\scripts\Build-StateForgePackages.ps1
```

Keep parameter-heavy operational scripts dedicated so PowerShell parameter help and validation remain visible:

- `Invoke-StateForgeMaintenanceHost.ps1`
- `Start-StateForgeReplicationHost.ps1`
- `New-StateForgeSnapshot.ps1`
- `New-StateForgeIncrementalSnapshot.ps1`
- `Register-StateForgeMaintenanceTask.ps1`
- `Unregister-StateForgeMaintenanceTask.ps1`
- `Rotate-StateForgeKeyRing.ps1`
- `Get-StateForgeSession.ps1`
- `Install-StateForgeStore.ps1`

Do not hide required operational parameters behind `Invoke-StateForge.ps1`.

## Documentation Policy

Documentation is consolidated. Prefer updating existing docs instead of adding many feature-specific files.

Core docs:

- `docs/01-getting-started.md`
- `docs/02-architecture.md`
- `docs/03-disaster-recovery.md`
- `docs/04-observability.md`
- `docs/05-testing.md`
- `docs/06-solution-layout.md`
- `docs/07-roadmap.md`
- `docs/08-api-reference.md`
- `docs/09-release-history.md`
- `docs/10-contributing.md`
- `docs/11-script-reference.md`
- `docs/12-production-readiness.md`
- `docs/13-runbooks.md`
- `docs/14-replica-catch-up.md`

If adding a new doc, update:

- `docs/README.md`
- `scripts\Test-StateForgeDocs.ps1`
- `scripts\Build-StateForge.ps1`
- `scripts\Test-StateForgeLayout.ps1`
- `scripts\Test-StateForgeSource.ps1`, if source guards are appropriate

## Replica Catch-Up Notes

Replica catch-up uses SHA256 content hashing for changed-file detection.

Do not regress to:

- file length only
- timestamp only
- `LastWriteUtc` comparison

The deterministic test fixture in `StateForge.ReplicaCatchUpTests` must remain independent of `StateForgeFileStore` path generation.

## Current Roadmap

Next recommended milestone:

```text
v1.0.0 — Production Release
```

Expected areas:

- security and performance review
- long-duration soak tests
- Production suite validation

Later milestones:

```text
Post-1.0 roadmap to be defined after production release.
```

## Safety Rules

Do not:

- delete validation scripts without replacing coverage
- remove Production suite checks
- reintroduce interactive prompts in Production or Release suites
- use timestamp-only file comparison for replica catch-up
- make parameter-heavy operations dispatcher-only
- expand docs back into many small overlapping files
- change public APIs without updating `docs/08-api-reference.md`
- update API baselines without reviewing and documenting the public surface change
- change store-format or sharding compatibility without updating the upgrade suite and migration guide
- change version numbers inconsistently

## Definition of Done

A change is done when:

- code builds
- relevant suite passes
- docs are updated
- version metadata is consistent
- source/layout/doc guards are updated when needed
- changelog includes the change
- no new interactive prompts are introduced into Production or Release validation
