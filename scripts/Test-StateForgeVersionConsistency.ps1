<#
.SYNOPSIS
Validates StateForge project version consistency.

.DESCRIPTION
Ensures every src project file uses the expected StateForge package version.

.PARAMETER ExpectedVersion
Expected project version. Defaults to 0.30.2.

.EXAMPLE
.\scripts\Test-StateForgeVersionConsistency.ps1

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$ExpectedVersion = '0.30.2'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $projects = Get-ChildItem -Path '.\src' -Recurse -Filter '*.csproj'
    $mismatches = @()

    foreach ($project in $projects) {
        $text = Get-Content -LiteralPath $project.FullName -Raw

        if ($text -notmatch "<Version>$([regex]::Escape($ExpectedVersion))</Version>") {
            $mismatches += $project.FullName
        }
    }

    if ($mismatches.Count -gt 0) {
        throw "Project version mismatch: $($mismatches -join ', ')"
    }

    [PSCustomObject]@{
        ExpectedVersion = $ExpectedVersion
        ProjectCount    = $projects.Count
        Mismatches      = 0
        Success         = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
