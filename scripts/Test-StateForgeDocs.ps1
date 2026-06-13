<#
.SYNOPSIS
Validates StateForge consolidated documentation.

.DESCRIPTION
Checks that the v0.28.x consolidated documentation model exists and that legacy docs are not present.

.EXAMPLE
.\scripts\Test-StateForgeDocs.ps1

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $requiredDocs = @(
        'docs\README.md',
        'docs\01-getting-started.md',
        'docs\02-architecture.md',
        'docs\03-disaster-recovery.md',
        'docs\04-observability.md',
        'docs\05-testing.md',
        'docs\06-solution-layout.md',
        'docs\07-roadmap.md',
        'docs\08-api-reference.md',
        'docs\09-release-history.md',
        'docs\10-contributing.md',
        'docs\11-script-reference.md',
        'docs\13-runbooks.md',
        'docs\12-production-readiness.md'
    )

    $forbiddenDocs = @(
        'docs\architecture',
        'docs\development',
        'docs\disaster-recovery',
        'docs\observability',
        'docs\user-guide',
        'docs\DOCUMENTATION-CONSOLIDATION-PLAN.md',
        'docs\release-hardening.md'
    )

    $missing = @()
    foreach ($doc in $requiredDocs) {
        if (-not (Test-Path -LiteralPath $doc)) {
            $missing += $doc
        }
    }

    $forbidden = @()
    foreach ($doc in $forbiddenDocs) {
        if (Test-Path -LiteralPath $doc) {
            $forbidden += $doc
        }
    }

    if ($missing.Count -gt 0) {
        throw "Missing documentation files: $($missing -join ', ')"
    }

    if ($forbidden.Count -gt 0) {
        throw "Forbidden legacy documentation paths still exist: $($forbidden -join ', ')"
    }

    [PSCustomObject]@{
        RequiredDocs  = $requiredDocs.Count
        MissingDocs   = $missing.Count
        ForbiddenDocs = $forbidden.Count
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
