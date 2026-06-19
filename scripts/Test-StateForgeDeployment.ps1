<#
.SYNOPSIS
Validates StateForge Docker and Kubernetes deployment assets.

.DESCRIPTION
Checks image version, non-root execution, storage paths, health probes, security settings,
resource requests, safe encryption defaults, and required Kubernetes resources.

.EXAMPLE
.\scripts\Test-StateForgeDeployment.ps1

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
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $requiredFiles = @(
        'Dockerfile',
        '.dockerignore',
        'deploy\k8s\configmap.yaml',
        'deploy\k8s\deployment.yaml',
        'deploy\k8s\hpa.yaml',
        'deploy\k8s\kustomization.yaml',
        'deploy\k8s\pvc.yaml',
        'deploy\k8s\secret.yaml',
        'deploy\k8s\service.yaml'
    )

    foreach ($requiredFile in $requiredFiles) {
        $path = Join-Path -Path $repoRoot -ChildPath $requiredFile
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Missing deployment asset: $requiredFile"
        }
    }

    $dockerfile = Get-Content -LiteralPath (Join-Path $repoRoot 'Dockerfile') -Raw
    $deployment = Get-Content -LiteralPath (Join-Path $repoRoot 'deploy\k8s\deployment.yaml') -Raw
    $configMap = Get-Content -LiteralPath (Join-Path $repoRoot 'deploy\k8s\configmap.yaml') -Raw
    $service = Get-Content -LiteralPath (Join-Path $repoRoot 'deploy\k8s\service.yaml') -Raw
    $pvc = Get-Content -LiteralPath (Join-Path $repoRoot 'deploy\k8s\pvc.yaml') -Raw
    $secret = Get-Content -LiteralPath (Join-Path $repoRoot 'deploy\k8s\secret.yaml') -Raw

    if ($dockerfile -notmatch 'USER app' -or
        $dockerfile -notmatch 'STATEFORGE_ROOT_PATH=/data/stateforge' -or
        $dockerfile -notmatch 'STATEFORGE_ENABLE_DEMO_ENDPOINTS=false') {
        throw 'Dockerfile must run non-root, use the persistent root, and disable demo endpoints.'
    }

    if ($deployment -notmatch 'image:\s*stateforge-kestrel:0\.35\.0' -or
        $deployment -notmatch 'runAsNonRoot:\s*true' -or
        $deployment -notmatch 'fsGroup:\s*1654' -or
        $deployment -notmatch 'startupProbe:' -or
        $deployment -notmatch 'readinessProbe:' -or
        $deployment -notmatch 'resources:') {
        throw 'Kubernetes deployment must use v0.35.0, non-root storage access, probes, and resources.'
    }

    if ($configMap -notmatch 'STATEFORGE_ROOT_PATH:\s*"/data/stateforge"' -or
        $configMap -notmatch 'STATEFORGE_SNAPSHOT_PATH:\s*"/data/stateforge/' -or
        $configMap -notmatch 'STATEFORGE_ENCRYPTION:\s*"false"' -or
        $configMap -notmatch 'STATEFORGE_ENABLE_DEMO_ENDPOINTS:\s*"false"') {
        throw 'Kubernetes configuration must align storage paths and use safe default exposure/encryption.'
    }

    if ($service -notmatch 'targetPort:\s*http' -or
        $pvc -notmatch 'ReadWriteMany' -or
        $secret -match 'REPLACE_WITH_BASE64_AES_KEY') {
        throw 'Kubernetes service, shared PVC, or secret template is stale.'
    }

    $kubectl = Get-Command kubectl -ErrorAction SilentlyContinue
    if ($null -ne $kubectl) {
        $kustomizeOutput = & $kubectl.Source kustomize (Join-Path $repoRoot 'deploy\k8s') 2>&1
        if ($LASTEXITCODE -ne 0) {
            $kustomizeText = ($kustomizeOutput | Out-String)
            if ($kustomizeText -match 'evalsymlink failure' -or $kustomizeText -match 'Access is denied') {
                Write-Warning 'kubectl kustomize could not access the local repository path; manifest file and content checks passed, so kustomize validation was skipped for this environment.'
            }
            else {
                throw "kubectl kustomize failed with exit code $LASTEXITCODE."
            }
        }
    }

    [PSCustomObject]@{
        Version       = '0.35.0'
        RequiredFiles = $requiredFiles.Count
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
