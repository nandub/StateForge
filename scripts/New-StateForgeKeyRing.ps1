<#
.SYNOPSIS
Creates a new StateForge AES key ring file.

.DESCRIPTION
Creates a new JSON key ring with one current AES-256 key.

.PARAMETER OutFile
Path to the key ring JSON file.

.PARAMETER KeyId
Optional key identifier.

.EXAMPLE
.\scripts\New-StateForgeKeyRing.ps1 -OutFile .\stateforge-keyring.json -KeyId key-001

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
    [string]$OutFile,

    [Parameter()]
    [string]$KeyId
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $toolProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'
    $resolvedOutFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutFile)

    if ($PSCmdlet.ShouldProcess($resolvedOutFile, 'Create StateForge AES key ring')) {
        $args = @('run', '--project', $toolProject, '--', 'keyring-create', '--out', $resolvedOutFile)

        if (-not [string]::IsNullOrWhiteSpace($KeyId)) {
            $args += '--key-id'
            $args += $KeyId
        }

        & dotnet @args

        if ($LASTEXITCODE -ne 0) {
            throw "StateForge key ring creation failed with exit code $LASTEXITCODE."
        }
    }

    [PSCustomObject]@{
        OutFile = $resolvedOutFile
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
