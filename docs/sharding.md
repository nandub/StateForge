# Directory Sharding

StateForge v0.19.1 formalizes directory sharding.

## Purpose

Large session stores should avoid placing every `.stfg` file directly under one directory.

StateForge supports hash-based shard directories using `ShardDepth`.

Example with `ShardDepth = 1`:

```text
sessions\
  A1\
    A1F3...stfg
```

Example with `ShardDepth = 2`:

```text
sessions\
  A1\
    F3\
      A1F3...stfg
```

## Compatibility

v0.19.1 adds transparent candidate-path reads:

1. current configured shard depth
2. legacy depth 0
3. depth 1
4. depth 2

This supports rolling upgrades and migration windows.

## Remove Behavior

Deletes now attempt all candidate paths so legacy and sharded duplicates can be cleaned up safely.

## Validation

```powershell
.\scripts\Test-StateForgeShardingImplementation.ps1
.\scripts\Invoke-StateForgeShardingMigration.ps1
```

## Recommended Default

Use:

```text
ShardDepth = 1
```

Use `ShardDepth = 2` only for very large stores or high-churn farms.
