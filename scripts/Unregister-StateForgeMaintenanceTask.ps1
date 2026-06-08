[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$TaskName = 'StateForge Maintenance Host'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    if ($PSCmdlet.ShouldProcess($TaskName, 'Unregister StateForge maintenance scheduled task')) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction Stop
    }

    [PSCustomObject]@{
        TaskName = $TaskName
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
