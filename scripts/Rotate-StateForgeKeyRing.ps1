<#
.SYNOPSIS
Rotates a StateForge AES key ring.

.DESCRIPTION
Adds a new AES-256 key to the key ring and makes it the current key.

.PARAMETER RingFile
Path to the key ring JSON file.

.PARAMETER NewKeyId
Optional new key identifier.

.PARAMETER RetirePrevious
Marks the previous current key as retired.

.EXAMPLE
.\scripts\Rotate-StateForgeKeyRing.ps1 -RingFile .\stateforge-keyring.json -NewKeyId key-002

.EXAMPLE
.\scripts\Rotate-StateForgeKeyRing.ps1 -RingFile .\stateforge-keyring.json -NewKeyId key-002 -RetirePrevious -WhatIf

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$RingFile,

    [Parameter()]
    [string]$NewKeyId,

    [Parameter()]
    [switch]$RetirePrevious
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $toolProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'
    $resolvedRingFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RingFile)

    if ($PSCmdlet.ShouldProcess($resolvedRingFile, 'Rotate StateForge AES key ring')) {
        $arguments = @('run', '--project', $toolProject, '--', 'keyring-rotate', '--ring', $resolvedRingFile)

        if (-not [string]::IsNullOrWhiteSpace($NewKeyId)) {
            $arguments += '--new-key-id'
            $arguments += $NewKeyId
        }

        if ($RetirePrevious.IsPresent) {
            $arguments += '--retire-previous'
        }

        & dotnet @arguments

        if ($LASTEXITCODE -ne 0) {
            throw "StateForge key ring rotation failed with exit code $LASTEXITCODE."
        }
    }

    [PSCustomObject]@{
        RingFile = $resolvedRingFile
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
