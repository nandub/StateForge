<#
.SYNOPSIS
Regenerates StateForge.sln with one unique project entry per known project.

.DESCRIPTION
Rewrites StateForge.sln from the known StateForge project list to remove duplicate solution entries.

.EXAMPLE
.\scripts\Repair-StateForgeSolution.ps1 -WhatIf

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $solutionPath = Join-Path -Path $repoRoot -ChildPath 'StateForge.sln'

    $projectType = '{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}'

    $projects = @(
        @{ Name = 'StateForge.Core'; Path = 'src\StateForge.Core\StateForge.Core.csproj'; Guid = '{3E8B1DF8-0B52-4C9A-AC57-CC9752F6B1C8}' },
        @{ Name = 'StateForge.FileStore'; Path = 'src\StateForge.FileStore\StateForge.FileStore.csproj'; Guid = '{CAAE5B22-AB66-40E4-98AA-A8E4028E7153}' },
        @{ Name = 'StateForge.AspNet'; Path = 'src\StateForge.AspNet\StateForge.AspNet.csproj'; Guid = '{8B1692C7-2692-41D2-97AD-0404B1C1389E}' },
        @{ Name = 'StateForge.AspNetCore'; Path = 'src\StateForge.AspNetCore\StateForge.AspNetCore.csproj'; Guid = '{B8D3B15A-AC33-4D10-A0DF-98E45AB8A9A4}' },
        @{ Name = 'StateForge.Maintenance'; Path = 'src\StateForge.Maintenance\StateForge.Maintenance.csproj'; Guid = '{E7C1EF3D-E41F-45A5-B58F-41C65A2381EF}' },
        @{ Name = 'StateForge.Security'; Path = 'src\StateForge.Security\StateForge.Security.csproj'; Guid = '{A766266C-2853-4BDF-93EC-78710C630C9E}' },
        @{ Name = 'StateForge.Telemetry'; Path = 'src\StateForge.Telemetry\StateForge.Telemetry.csproj'; Guid = '{6B4BA7C2-458F-4BB0-8CA7-985BD536E047}' },
        @{ Name = 'StateForge.Telemetry.AspNetCore'; Path = 'src\StateForge.Telemetry.AspNetCore\StateForge.Telemetry.AspNetCore.csproj'; Guid = '{2F552839-D073-456B-80BB-3DB72907809B}' },
        @{ Name = 'StateForge.Tools'; Path = 'src\StateForge.Tools\StateForge.Tools.csproj'; Guid = '{3F4B7F90-11C8-41AA-A2C9-7A7B70133D02}' },
        @{ Name = 'StateForge.SmokeTests'; Path = 'src\StateForge.SmokeTests\StateForge.SmokeTests.csproj'; Guid = '{6DD3D236-7DF9-4A8C-95F8-18F41D96CE61}' },
        @{ Name = 'StateForge.Benchmarks'; Path = 'src\StateForge.Benchmarks\StateForge.Benchmarks.csproj'; Guid = '{87DD84E3-B92E-44EE-8E86-5AE4F3526821}' },
        @{ Name = 'StateForge.FarmTests'; Path = 'src\StateForge.FarmTests\StateForge.FarmTests.csproj'; Guid = '{F0878148-83D3-4C8D-A564-9E569D22BB41}' },
        @{ Name = 'StateForge.ResilienceTests'; Path = 'src\StateForge.ResilienceTests\StateForge.ResilienceTests.csproj'; Guid = '{7A3F9C37-94A1-4743-A8DB-564CA2632C4F}' },
        @{ Name = 'StateForge.AspNetHarness'; Path = 'src\StateForge.AspNetHarness\StateForge.AspNetHarness.csproj'; Guid = '{EA3E214E-BC15-4B03-A62F-419FB48A004D}' },
        @{ Name = 'StateForge.KestrelHarness'; Path = 'src\StateForge.KestrelHarness\StateForge.KestrelHarness.csproj'; Guid = '{5FA39EB2-3ACB-46AE-A973-A3A1E4C1A04C}' },
        @{ Name = 'StateForge.KestrelClientTest'; Path = 'src\StateForge.KestrelClientTest\StateForge.KestrelClientTest.csproj'; Guid = '{9543AE54-B23B-4754-88CD-F78E508C3727}' },
        @{ Name = 'StateForge.FileStore.Tests'; Path = 'tests\StateForge.FileStore.Tests\StateForge.FileStore.Tests.csproj'; Guid = '{7921F885-D1CC-4F77-A707-A38964BA1A13}' }
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('Microsoft Visual Studio Solution File, Format Version 12.00')
    $lines.Add('# Visual Studio Version 17')
    $lines.Add('VisualStudioVersion = 17.0.31903.59')
    $lines.Add('MinimumVisualStudioVersion = 10.0.40219.1')

    foreach ($project in $projects) {
        $projectFile = Join-Path -Path $repoRoot -ChildPath $project.Path

        if (Test-Path -LiteralPath $projectFile) {
            $lines.Add(('Project("{0}") = "{1}", "{2}", "{3}"' -f $projectType, $project.Name, $project.Path, $project.Guid))
            $lines.Add('EndProject')
        }
    }

    $lines.Add('Global')
    $lines.Add('    GlobalSection(SolutionConfigurationPlatforms) = preSolution')
    $lines.Add('        Debug|Any CPU = Debug|Any CPU')
    $lines.Add('        Release|Any CPU = Release|Any CPU')
    $lines.Add('    EndGlobalSection')
    $lines.Add('    GlobalSection(ProjectConfigurationPlatforms) = postSolution')

    foreach ($project in $projects) {
        $projectFile = Join-Path -Path $repoRoot -ChildPath $project.Path

        if (Test-Path -LiteralPath $projectFile) {
            foreach ($configuration in @('Debug', 'Release')) {
                $lines.Add(('        {0}.{1}|Any CPU.ActiveCfg = {1}|Any CPU' -f $project.Guid, $configuration))
                $lines.Add(('        {0}.{1}|Any CPU.Build.0 = {1}|Any CPU' -f $project.Guid, $configuration))
            }
        }
    }

    $lines.Add('    EndGlobalSection')
    $lines.Add('EndGlobal')

    if ($PSCmdlet.ShouldProcess($solutionPath, 'Regenerate StateForge solution')) {
        Set-Content -LiteralPath $solutionPath -Value $lines -Encoding UTF8
    }

    [PSCustomObject]@{
        Solution = $solutionPath
        Projects = $projects.Count
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
