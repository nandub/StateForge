[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$TaskName = 'StateForge Maintenance Host',
    [Parameter(Mandatory = $true)]
    [string]$RootPath,
    [int]$FrequencyMinutes = 15,
    [string]$LogPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $invokeScript = Join-Path -Path $repoRoot -ChildPath 'scripts\Invoke-StateForgeMaintenanceHost.ps1'
    $resolvedRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)

    $argument = '-NoProfile -ExecutionPolicy Bypass -File "' + $invokeScript + '" -RootPath "' + $resolvedRoot + '" -Once'

    if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
        $resolvedLog = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($LogPath)
        $argument += ' -LogPath "' + $resolvedLog + '"'
    }

    if ($PSCmdlet.ShouldProcess($TaskName, 'Register StateForge maintenance scheduled task')) {
        $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $argument
        $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).Date.AddMinutes(5) -RepetitionInterval (New-TimeSpan -Minutes $FrequencyMinutes) -RepetitionDuration (New-TimeSpan -Days 3650)
        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
        Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Force | Out-Null
    }

    [PSCustomObject]@{
        TaskName = $TaskName
        RootPath = $resolvedRoot
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
