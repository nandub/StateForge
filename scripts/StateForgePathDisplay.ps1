<#
.SYNOPSIS
Formats local StateForge paths for user-facing script output.

.DESCRIPTION
Keeps script execution on the resolved repository path while allowing output to
prefer a stable display path. This is useful when C:\Users\ferna\development is
a junction to S:\Users\ferna\development but command output should remain in
the C: path form.

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

    $normalizedRoot = $RepositoryRoot.TrimEnd('\')
    $mappedPrefix = 'S:\Users\ferna\development'
    if ($normalizedRoot.StartsWith($mappedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return 'C:\Users\ferna\development' + $normalizedRoot.Substring($mappedPrefix.Length)
    }

    return $normalizedRoot
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

    $mappedPrefix = 'S:\Users\ferna\development'
    if ($Path.StartsWith($mappedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return 'C:\Users\ferna\development' + $Path.Substring($mappedPrefix.Length)
    }

    return $Path
}
