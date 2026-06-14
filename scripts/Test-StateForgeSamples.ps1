<#
.SYNOPSIS
Builds and validates all StateForge samples.

.DESCRIPTION
Builds SDK-style samples, runs direct FileStore persistence checks, and validates the ASP.NET Framework
configuration and per-sample documentation.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$sampleRoot = $null
$sampleProcesses = @()
$previousRootPath = $env:STATEFORGE_ROOT_PATH
$previousAesKey = $env:STATEFORGE_AES_KEY_BASE64
$previousDataProtectionPath = $env:STATEFORGE_DATA_PROTECTION_PATH

function Get-FreeTcpPort {
    [CmdletBinding()]
    param()

    $listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Wait-ForHttp {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri
    )

    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        try {
            return Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    throw "Timed out waiting for sample endpoint: $Uri"
}

function Convert-ResponseContent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [object]$Content
    )

    if ($Content -is [byte[]]) {
        return [System.Text.Encoding]::UTF8.GetString($Content)
    }

    return [string]$Content
}

try {
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $samplesRoot = Join-Path -Path $repoRoot -ChildPath 'samples'
    $sampleRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ('StateForgeSamples-' + [Guid]::NewGuid().ToString('N'))

    $projects = @(
        'samples\StateForge.SampleFileStore\StateForge.SampleFileStore.csproj',
        'samples\StateForge.SampleAspNetCore\StateForge.SampleAspNetCore.csproj',
        'samples\StateForge.SampleCloudNative\StateForge.SampleCloudNative.csproj'
    )

    foreach ($project in $projects) {
        $projectPath = Join-Path -Path $repoRoot -ChildPath $project
        & dotnet build $projectPath --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Sample build failed: $project"
        }
    }

    $fileStoreProject = Join-Path -Path $repoRoot -ChildPath 'samples\StateForge.SampleFileStore\StateForge.SampleFileStore.csproj'
    $firstRun = & dotnet run --project $fileStoreProject --configuration Release --no-build -- demo --root $sampleRoot
    if ($LASTEXITCODE -ne 0 -or ($firstRun -join [Environment]::NewLine) -notmatch 'Counter:\s*1') {
        throw 'FileStore sample did not create the expected first counter value.'
    }

    $secondRun = & dotnet run --project $fileStoreProject --configuration Release --no-build -- demo --root $sampleRoot
    if ($LASTEXITCODE -ne 0 -or ($secondRun -join [Environment]::NewLine) -notmatch 'Counter:\s*2') {
        throw 'FileStore sample did not persist the counter across processes.'
    }

    $stats = & dotnet run --project $fileStoreProject --configuration Release --no-build -- stats --root $sampleRoot
    if ($LASTEXITCODE -ne 0 -or ($stats -join [Environment]::NewLine) -notmatch 'sessions=1') {
        throw 'FileStore sample statistics did not report the persisted record.'
    }

    $env:STATEFORGE_AES_KEY_BASE64 = $null

    $aspNetPort = Get-FreeTcpPort
    $aspNetUrl = "http://127.0.0.1:$aspNetPort"
    $env:STATEFORGE_ROOT_PATH = Join-Path -Path $sampleRoot -ChildPath 'aspnetcore'
    $env:STATEFORGE_DATA_PROTECTION_PATH = Join-Path -Path $sampleRoot -ChildPath 'data-protection'
    $aspNetDll = Join-Path -Path $repoRoot -ChildPath 'samples\StateForge.SampleAspNetCore\bin\Release\net8.0\StateForge.SampleAspNetCore.dll'
    $aspNetProcess = Start-Process -FilePath 'dotnet' -ArgumentList @($aspNetDll, '--urls', $aspNetUrl) -PassThru -WindowStyle Hidden
    $sampleProcesses += $aspNetProcess
    Wait-ForHttp -Uri $aspNetUrl | Out-Null

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $firstSessionResponse = Invoke-WebRequest -Uri $aspNetUrl -WebSession $session -UseBasicParsing
    $secondSessionResponse = Invoke-WebRequest -Uri $aspNetUrl -WebSession $session -UseBasicParsing
    $firstSessionContent = Convert-ResponseContent -Content $firstSessionResponse.Content
    $secondSessionContent = Convert-ResponseContent -Content $secondSessionResponse.Content
    if ($firstSessionContent -notmatch 'counter:\s*1' -or
        $secondSessionContent -notmatch 'counter:\s*2') {
        throw 'ASP.NET Core sample did not persist and increment the session counter.'
    }

    Stop-Process -Id $aspNetProcess.Id -Force
    $sampleProcesses = @($sampleProcesses | Where-Object { $_.Id -ne $aspNetProcess.Id })

    $cloudPort = Get-FreeTcpPort
    $cloudUrl = "http://127.0.0.1:$cloudPort"
    $env:STATEFORGE_ROOT_PATH = Join-Path -Path $sampleRoot -ChildPath 'cloud-native'
    $cloudDll = Join-Path -Path $repoRoot -ChildPath 'samples\StateForge.SampleCloudNative\bin\Release\net8.0\StateForge.SampleCloudNative.dll'
    $cloudProcess = Start-Process -FilePath 'dotnet' -ArgumentList @($cloudDll, '--urls', $cloudUrl) -PassThru -WindowStyle Hidden
    $sampleProcesses += $cloudProcess
    Wait-ForHttp -Uri ($cloudUrl + '/livez') | Out-Null

    Invoke-RestMethod -Method Put -Uri ($cloudUrl + '/cache/example') -ContentType 'application/json' -Body '{"value":"hello"}' | Out-Null
    $cloudValue = Invoke-RestMethod -Uri ($cloudUrl + '/cache/example')
    $cloudReady = Invoke-RestMethod -Uri ($cloudUrl + '/readyz')
    $cloudMetrics = Invoke-RestMethod -Uri ($cloudUrl + '/stateforge/metrics')

    if ($cloudValue.value -ne 'hello' -or
        -not $cloudReady.ready -or
        $cloudMetrics.writes -lt 1 -or
        $cloudMetrics.reads -lt 1) {
        throw 'Cloud-native sample cache, readiness, or telemetry validation failed.'
    }

    Stop-Process -Id $cloudProcess.Id -Force
    $sampleProcesses = @($sampleProcesses | Where-Object { $_.Id -ne $cloudProcess.Id })

    $sampleDirectories = Get-ChildItem -LiteralPath $samplesRoot -Directory
    foreach ($sampleDirectory in $sampleDirectories) {
        $readme = Join-Path -Path $sampleDirectory.FullName -ChildPath 'README.md'
        if (-not (Test-Path -LiteralPath $readme)) {
            throw "Sample folder is missing README.md: $($sampleDirectory.Name)"
        }
    }

    $webConfigPath = Join-Path -Path $samplesRoot -ChildPath 'StateForge.SampleWebFramework\Web.config'
    $webConfig = Get-Content -LiteralPath $webConfigPath -Raw
    if ($webConfig -match 'D:\\StateForge' -or
        $webConfig -notmatch 'enableEncryption="false"' -or
        $webConfig -notmatch 'protectionMode="none"') {
        throw 'ASP.NET Framework sample must use a portable root and safe encryption defaults.'
    }

    [PSCustomObject]@{
        ProjectCount   = $projects.Count
        SampleFolders = $sampleDirectories.Count
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
finally {
    foreach ($sampleProcess in $sampleProcesses) {
        if ($null -ne $sampleProcess -and -not $sampleProcess.HasExited) {
            Stop-Process -Id $sampleProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }

    $env:STATEFORGE_ROOT_PATH = $previousRootPath
    $env:STATEFORGE_AES_KEY_BASE64 = $previousAesKey
    $env:STATEFORGE_DATA_PROTECTION_PATH = $previousDataProtectionPath

    if (-not [string]::IsNullOrWhiteSpace($sampleRoot) -and (Test-Path -LiteralPath $sampleRoot)) {
        Remove-Item -LiteralPath $sampleRoot -Recurse -Force
    }
}
