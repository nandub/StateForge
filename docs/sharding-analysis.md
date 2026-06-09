# Sharding Analysis

StateForge v0.18.2 adds sharding analysis helpers.

## Purpose

The sharding analyzer reports:

- directory count
- total file count
- minimum files per directory
- maximum files per directory
- average files per directory
- whether the store appears sharded
- warnings for large or unsharded directories

## Validate

```powershell
.\scripts\Test-StateForgeSharding.ps1
```
