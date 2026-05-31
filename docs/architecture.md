# Architecture

## Overview

```text
ASP.NET Framework / ASP.NET Core
        |
        v
StateForge Provider or Adapter
        |
        v
StateForge.FileStore
        |
        v
File System
```

## Components

| Component | Purpose |
|---|---|
| `StateForge.Core` | Shared models and contracts |
| `StateForge.FileStore` | Persistent file-backed storage engine |
| `StateForge.AspNet` | Classic ASP.NET SessionState provider |
| `StateForge.AspNetCore` | ASP.NET Core `IDistributedCache` adapter |
| `StateForge.Tools` | Diagnostics, stats, health, cleanup, key generation |
| `StateForge.SmokeTests` | Core feature validation |
| `StateForge.Benchmarks` | Performance testing |
| `StateForge.FarmTests` | Multi-node simulation |
| `StateForge.ResilienceTests` | Crash and stale-lock recovery |
| `StateForge.AspNetHarness` | ASP.NET provider lifecycle without IIS |
| `StateForge.KestrelHarness` | ASP.NET Core HTTP harness |
| `StateForge.KestrelClientTest` | HTTP test client |

## Storage Layout

```text
RootPath\
├── sessions\
├── temp\
├── backups\
└── quarantine\
```

## Write Flow

```text
payload -> compression -> encryption -> temp file -> atomic replace
```

## Read Flow

```text
session file -> decrypt -> decompress -> payload
```

## Diagnostics

`diag` counts files. `list` and `stats` deserialize records. AES records require the AES key.
