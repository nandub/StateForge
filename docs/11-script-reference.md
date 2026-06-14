# Script Reference

StateForge uses two consolidated script entry points and keeps parameter-rich operational scripts dedicated.

## Preferred Entry Points

| Script | Purpose |
|---|---|
| `Build-StateForge.ps1` | Build validation |
| `Test-StateForge.ps1` | Suite-based validation runner |
| `Invoke-StateForge.ps1` | Convenience command runner |
| `Build-StateForgePackages.ps1` | Package creation |

## Validation Runner

Use `Test-StateForge.ps1` for validation suites:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Docs
.\scripts\Test-StateForge.ps1 -Suite Version
.\scripts\Test-StateForge.ps1 -Suite Layout
.\scripts\Test-StateForge.ps1 -Suite Snapshots
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
.\scripts\Test-StateForge.ps1 -Suite Quorum
.\scripts\Test-StateForge.ps1 -Suite Witness
.\scripts\Test-StateForge.ps1 -Suite SplitBrain
.\scripts\Test-StateForge.ps1 -Suite MultiSite
.\scripts\Test-StateForge.ps1 -Suite Release
```

## Convenience Command Runner

`Invoke-StateForge.ps1` is intentionally limited to lower-parameter convenience commands:

```powershell
.\scripts\Invoke-StateForge.ps1 -Command BuildPackages
.\scripts\Invoke-StateForge.ps1 -Command RunSmokeTest
.\scripts\Invoke-StateForge.ps1 -Command RunBenchmark
.\scripts\Invoke-StateForge.ps1 -Command TestNuGetSources
.\scripts\Invoke-StateForge.ps1 -Command RepairSolution
```

## Dedicated Operational Scripts

Keep these as direct scripts because they expose meaningful required parameters:

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1
.\scripts\Start-StateForgeReplicationHost.ps1
.\scripts\New-StateForgeIncrementalSnapshot.ps1
.\scripts\New-StateForgeSnapshot.ps1
.\scripts\Register-StateForgeMaintenanceTask.ps1
.\scripts\Unregister-StateForgeMaintenanceTask.ps1
.\scripts\Rotate-StateForgeKeyRing.ps1
.\scripts\Get-StateForgeSession.ps1
.\scripts\Install-StateForgeStore.ps1
```

This keeps PowerShell parameter binding, prompts, validation, and help text visible to the operator.

## Policy

- Use `Test-StateForge.ps1` for validation.
- Use `Invoke-StateForge.ps1` only for convenience commands.
- Keep parameter-heavy operational scripts dedicated.
- Do not hide required operational parameters behind a generic dispatcher.


## Production Readiness

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
```

See `docs\12-production-readiness.md` and `docs\13-runbooks.md`.


## Replica Catch-Up

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForgeReplicaCatchUp.ps1
```

See `docs\14-replica-catch-up.md`.

## Replica Monitoring

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
.\scripts\Test-StateForgeReplicaMonitoring.ps1
```

## Quorum

```powershell
.\scripts\Test-StateForge.ps1 -Suite Quorum
.\scripts\Test-StateForgeQuorum.ps1
```

## Witness Nodes

```powershell
.\scripts\Test-StateForge.ps1 -Suite Witness
.\scripts\Test-StateForgeWitness.ps1
```

## Split-Brain Prevention

```powershell
.\scripts\Test-StateForge.ps1 -Suite SplitBrain
.\scripts\Test-StateForgeSplitBrain.ps1
```

## Multi-Site Disaster Recovery

```powershell
.\scripts\Test-StateForge.ps1 -Suite MultiSite
.\scripts\Test-StateForgeMultiSite.ps1
```

Dashboard replica health uses semicolon-separated `name=path` entries:

```powershell
dotnet run --project .\src\StateForge.Tools -- dashboard --root C:\stateforge `
  --replicas "west=C:\replicas\west;east=C:\replicas\east" `
  --replica-stale-seconds 300
```
