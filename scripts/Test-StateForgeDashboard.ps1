<#
.SYNOPSIS
Tests the StateForge dashboard command.

.DESCRIPTION
Runs the StateForge.Tools dashboard command against a temporary store.

.EXAMPLE
.\scripts\Test-StateForgeDashboard.ps1

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
    $root = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath 'StateForgeDashboardTest'

    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }

    New-Item -Path $root -ItemType Directory -Force | Out-Null

    $toolProject = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'
    $output = & dotnet run --project $toolProject --configuration Release -- dashboard --root $root

    if ($LASTEXITCODE -ne 0) {
        throw "Dashboard command failed with exit code $LASTEXITCODE."
    }

    $text = ($output | Out-String)

    if ($text -notmatch 'StateForge Dashboard') {
        throw "Dashboard header was not found."
    }

    if ($text -notmatch 'Health') {
        throw "Dashboard health section was not found."
    }

    [PSCustomObject]@{
        RootPath = $root
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
