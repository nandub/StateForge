<#
.SYNOPSIS
Formats local StateForge paths for user-facing script output.

.DESCRIPTION
Keeps script execution on the resolved repository path while allowing output to
prefer a stable display path. Set STATEFORGE_DISPLAY_ROOT to force a specific
display prefix, or let the helper prefer an equivalent C: path when one exists.

.NOTES
Compatible with Windows PowerShell 5.1.
#>

function Get-StateForgeDisplayRoot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($env:STATEFORGE_DISPLAY_ROOT)) {
        return $env:STATEFORGE_DISPLAY_ROOT.TrimEnd('\')
    }

    $normalizedRoot = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\')
    $rootPath = [System.IO.Path]::GetPathRoot($normalizedRoot)

    if (-not [string]::IsNullOrWhiteSpace($rootPath) -and
        $rootPath.Length -ge 2 -and
        -not $rootPath.StartsWith('C:', [System.StringComparison]::OrdinalIgnoreCase)) {
        $candidateRoot = 'C:' + $normalizedRoot.Substring(2)
        if (Test-StateForgeDisplayRootCandidate -CandidateRoot $candidateRoot) {
            return $candidateRoot.TrimEnd('\')
        }
    }

    return $normalizedRoot
}

function Test-StateForgeDisplayRootCandidate {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$CandidateRoot
    )

    if ([string]::IsNullOrWhiteSpace($CandidateRoot)) {
        return $false
    }

    try {
        return (Test-Path -LiteralPath (Join-Path -Path $CandidateRoot -ChildPath 'StateForge.sln')) -and
            (Test-Path -LiteralPath (Join-Path -Path $CandidateRoot -ChildPath 'scripts\Test-StateForge.ps1'))
    }
    catch {
        return $false
    }
}

function ConvertTo-StateForgeDisplayPath {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $Path
    }

    $actualRoot = $RepositoryRoot.TrimEnd('\')
    $displayRoot = Get-StateForgeDisplayRoot -RepositoryRoot $RepositoryRoot

    if ($Path.StartsWith($actualRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $displayRoot + $Path.Substring($actualRoot.Length)
    }

    return $Path
}
