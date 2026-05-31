<#
.SYNOPSIS
Validates the StateForge solution file.

.DESCRIPTION
Checks the solution file for duplicate project display names and duplicate project paths before dotnet restore/build.

.EXAMPLE
.\scripts\Test-StateForgeSolution.ps1

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
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $solutionPath = Join-Path -Path $repoRoot -ChildPath 'StateForge.sln'

    if (-not (Test-Path -LiteralPath $solutionPath)) {
        throw "Solution file not found: $solutionPath"
    }

    $projectNames = New-Object System.Collections.Generic.List[string]
    $projectPaths = New-Object System.Collections.Generic.List[string]

    foreach ($line in Get-Content -LiteralPath $solutionPath) {
        if ($line -match '^Project\("[^"]+"\)\s+=\s+"([^"]+)",\s+"([^"]+)"') {
            $projectNames.Add($Matches[1])
            $projectPaths.Add($Matches[2])
        }
    }

    $duplicateNames = $projectNames |
        Group-Object |
        Where-Object { $_.Count -gt 1 } |
        Select-Object -ExpandProperty Name

    $duplicatePaths = $projectPaths |
        Group-Object |
        Where-Object { $_.Count -gt 1 } |
        Select-Object -ExpandProperty Name

    if ($duplicateNames) {
        throw "Duplicate project name(s) in solution: $($duplicateNames -join ', ')"
    }

    if ($duplicatePaths) {
        throw "Duplicate project path(s) in solution: $($duplicatePaths -join ', ')"
    }

    [PSCustomObject]@{
        Solution     = $solutionPath
        ProjectCount = $projectNames.Count
        Success      = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
