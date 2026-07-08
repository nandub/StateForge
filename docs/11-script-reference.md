# Script Reference

StateForge uses two consolidated script entry points and keeps parameter-rich operational scripts dedicated.

## Preferred Entry Points

| Script | Purpose |
|---|---|
| `Build-StateForge.ps1` | Build validation |
| `Test-StateForge.ps1` | Suite-based validation runner |
| `Invoke-StateForge.ps1` | Convenience command runner |
| `Build-StateForgePackages.ps1` | Package creation |
| `Build-StateForgeApiDocs.ps1` | DocFX conceptual and generated API site |

## Validation Runner

Use `Test-StateForge.ps1` for validation suites:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Docs
.\scripts\Test-StateForge.ps1 -Suite Version
.\scripts\Test-StateForge.ps1 -Suite Layout
.\scripts\Test-StateForge.ps1 -Suite ApiDocs
.\scripts\Test-StateForge.ps1 -Suite ApiCompatibility
.\scripts\Test-StateForge.ps1 -Suite UpgradeCompatibility
.\scripts\Test-StateForge.ps1 -Suite Security
.\scripts\Test-StateForge.ps1 -Suite Samples
.\scripts\Test-StateForge.ps1 -Suite Snapshots
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
.\scripts\Test-StateForge.ps1 -Suite Quorum
.\scripts\Test-StateForge.ps1 -Suite Witness
.\scripts\Test-StateForge.ps1 -Suite SplitBrain
.\scripts\Test-StateForge.ps1 -Suite MultiSite
.\scripts\Test-StateForge.ps1 -Suite Remote
.\scripts\Test-StateForge.ps1 -Suite Deployment
.\scripts\Test-StateForge.ps1 -Suite Packages
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

## Performance Baselines

```powershell
.\scripts\Test-StateForge.ps1 -Suite Performance
.\scripts\Invoke-StateForgePerformanceBaseline.ps1 -Profile All
.\scripts\Invoke-StateForgePerformanceBaseline.ps1 -Profile All -UpdateBaseline
```

The runner writes candidates to `artifacts\performance` by default. `-UpdateBaseline` is the explicit
review action that writes small, medium, and large CSV/JSON references to the tracked
`performance-baselines` directory. `Compare-StateForgeBenchmark.ps1` fails when a scenario is missing,
throughput falls below 15 percent of reference, or P95 exceeds eight times reference plus 25 ms.

`Test-StateForgeArtifactDependencies.ps1` guards this split so ignored `artifacts` content cannot become
a required clean-clone input.

## Soak Testing

```powershell
.\scripts\Test-StateForge.ps1 -Suite Soak
.\scripts\Invoke-StateForgeSoakTest.ps1 -DurationSeconds 3600 -MaxOperations 100000 -FinalReplication -FinalSnapshot
```

`Invoke-StateForgeSoakTest.ps1` runs a configurable long-duration workload and writes `soak.json` and
`soak.csv` under `artifacts\soak` by default. Use `FinalReplication` and `FinalSnapshot` for quiescent
release evidence. Use `CleanupInterval`, `ReplicationInterval`, and `SnapshotInterval` to include
operational maintenance work during active writes. `Test-StateForgeSoak.ps1` executes a short
validation profile for release gating.


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

## Docker and Kubernetes

```powershell
.\scripts\Test-StateForge.ps1 -Suite Deployment
.\scripts\Test-StateForgeDeployment.ps1
docker build --tag stateforge-kestrel:1.0.1 .
kubectl apply -k .\deploy\k8s
```

## Remote Store

```powershell
.\scripts\Test-StateForge.ps1 -Suite Remote
.\scripts\Test-StateForgeRemote.ps1
```

The default remote suite builds the remote projects and runs endpoint validation. For full remote-host
TLS coverage, run the integration category directly:

```powershell
dotnet test .\tests\StateForge.Remote.Tests\StateForge.Remote.Tests.csproj `
  --configuration Release `
  --filter TestCategory=Integration
```

## NuGet Packages and SourceLink

```powershell
.\scripts\Test-StateForge.ps1 -Suite Packages
.\scripts\Test-StateForgePackages.ps1
.\scripts\Test-StateForgePackageArtifacts.ps1 -PackagePath .\artifacts\nuget
.\scripts\Test-StateForgePackageInstall.ps1 -PackagePath .\artifacts\nuget
```

`Build-StateForgePackages.ps1` emits `.nupkg` and `.snupkg` files with deterministic portable PDBs,
SourceLink mappings, and the exact repository commit in package metadata.

## Public API Compatibility

```powershell
.\scripts\Test-StateForge.ps1 -Suite ApiCompatibility
.\scripts\Test-StateForgeApiCompatibility.ps1
```

The default mode compares all thirteen package assemblies with `api-baselines`. For a reviewed intentional
change, regenerate the baselines explicitly:

```powershell
.\scripts\Test-StateForgeApiCompatibility.ps1 -UpdateBaseline
```

## Generated API Documentation

```powershell
.\scripts\Build-StateForgeApiDocs.ps1
.\scripts\Test-StateForge.ps1 -Suite ApiDocs
```

DocFX extracts public API metadata from all thirteen package projects and writes the site to
`artifacts\docfx\site`. The local tool manifest pins the DocFX version used by development and validation.

## Rolling Upgrade Compatibility

```powershell
.\scripts\Test-StateForge.ps1 -Suite UpgradeCompatibility
.\scripts\Test-StateForgeUpgradeCompatibility.ps1
```

The suite validates the supported same-shard STFG1 rolling-upgrade path, post-drain sharding migration,
legacy replication and snapshot restore, and unsupported downgrade boundaries.

## Security Validation

```powershell
.\scripts\Test-StateForge.ps1 -Suite Security
.\scripts\Test-StateForgeSecurity.ps1
```

The suite validates authenticated AES records, tamper and wrong-key rejection, legacy AES compatibility,
bounded decompression, and atomic validated key-ring persistence.

## Sample Validation

```powershell
.\scripts\Test-StateForge.ps1 -Suite Samples
.\scripts\Test-StateForgeSamples.ps1
```

The suite builds the FileStore, ASP.NET Core, and cloud-native samples, executes FileStore persistence
checks, and validates the Web Forms configuration and all sample READMEs.

Dashboard replica health uses semicolon-separated `name=path` entries:

```powershell
dotnet run --project .\src\StateForge.Tools -- dashboard --root C:\stateforge `
  --replicas "west=C:\replicas\west;east=C:\replicas\east" `
  --replica-stale-seconds 300
```
