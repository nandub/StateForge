<#
.SYNOPSIS
Runs a local StateForge farm simulation.

.DESCRIPTION
Simulates multiple application nodes using the same StateForge root path and AES key.

.PARAMETER RootPath
StateForge test root.

.PARAMETER AesKeyBase64
AES key. If omitted, the harness uses a deterministic test key.

.PARAMETER Keep
Keep test files.

.EXAMPLE
$key = dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key
.\scripts\Invoke-StateForgeFarmTest.ps1 -RootPath ..\StateForgeFarm -AesKeyBase64 $key -Keep

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
    [string]$RootPath,

    [Parameter()]
    [string]$AesKeyBase64,

    [Parameter()]
    [switch]$Keep
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.FarmTests\StateForge.FarmTests.csproj'

    $arguments = @('run', '--project', $projectPath, '--configuration', 'Release', '--')

    if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
        $arguments += '--root'
        $arguments += $RootPath
    }

    if (-not [string]::IsNullOrWhiteSpace($AesKeyBase64)) {
        $arguments += '--aes-key'
        $arguments += $AesKeyBase64
    }

    if ($Keep.IsPresent) {
        $arguments += '--keep'
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge farm test failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project  = $projectPath
        RootPath = $RootPath
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
