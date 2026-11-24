#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs tests as they are run in cloud test runs.
.PARAMETER Configuration
    The configuration within which to run tests
.PARAMETER Agent
    The name of the agent. This is used in preparing test run titles.
.PARAMETER PublishResults
    A switch to publish results to Azure Pipelines.
.PARAMETER x86
    A switch to run the tests in an x86 process.
.PARAMETER dotnet32
    The path to a 32-bit dotnet executable to use.
.PARAMETER NoCoverage
    A switch to skip code coverage collection.
#>
[CmdletBinding()]
Param(
    [string]$Configuration='Debug',
    [string]$Agent='Local',
    [switch]$PublishResults,
    [switch]$x86,
    [string]$dotnet32,
    [switch]$NoCoverage
)

$RepoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$ArtifactStagingFolder = & "$PSScriptRoot/Get-ArtifactsStagingDirectory.ps1"
$OnCI = ($env:CI -or $env:TF_BUILD)

$dotnet = 'dotnet'
if ($x86) {
  $x86RunTitleSuffix = ", x86"
  if ($dotnet32) {
    $dotnet = $dotnet32
  } else {
    $dotnet32Possibilities = "$PSScriptRoot\../obj/tools/x86/.dotnet/dotnet.exe", "$env:AGENT_TOOLSDIRECTORY/x86/dotnet/dotnet.exe", "${env:ProgramFiles(x86)}\dotnet\dotnet.exe"
    $dotnet32Matches = $dotnet32Possibilities |? { Test-Path $_ }
    if ($dotnet32Matches) {
      $dotnet = Resolve-Path @($dotnet32Matches)[0]
      Write-Host "Running tests using `"$dotnet`"" -ForegroundColor DarkGray
    } else {
      Write-Error "Unable to find 32-bit dotnet.exe"
      exit 1
    }
  }
}

$runId = [guid]::NewGuid().ToString('N')
$testLogs = Join-Path (Join-Path $ArtifactStagingFolder test_logs) $runId
$buildLogs = Join-Path $ArtifactStagingFolder build_logs
New-Item $testLogs -ItemType Directory -Force | Out-Null
New-Item $buildLogs -ItemType Directory -Force | Out-Null

$extraArgs = @()
$failedTests = 0

$testDiagLog = Join-Path $testLogs diag.log
$coverageArgs = @()
if (-not $NoCoverage) {
    $coverageArgs = @(
        '--collect'
        'Code Coverage;Format=cobertura'
        '--settings'
        "$PSScriptRoot/test.runsettings"
    )
}

# The solution contains both VSTest and MTP projects; their runner arguments are incompatible.
$v2Project = Join-Path $RepoRoot 'test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.Tests.csproj'
$v2BinLog = "-bl:$(Join-Path $buildLogs "test-v2-$runId.binlog")"
& $dotnet test $v2Project `
    --no-build `
    -c $Configuration `
    --filter "TestCategory!=FailsInCloudTest" `
    --blame-hang-timeout 60s `
    --blame-crash `
    $v2BinLog `
    --diag "$testDiagLog;TraceLevel=info" `
    --results-directory $testLogs `
    --logger trx `
    @coverageArgs
if ($LASTEXITCODE -ne 0) { $failedTests += 1 }

if ($OnCI) { $extraArgs += '--no-progress' }
$mtpArgs = @(
    '--filter-not-trait'
    'TestCategory=FailsInCloudTest'
    '--diagnostic'
    '--diagnostic-output-directory'
    $testLogs
    '--diagnostic-verbosity'
    'Information'
    '--results-directory'
    $testLogs
    '--report-trx'
    '--hangdump'
    '--hangdump-timeout'
    '5m'
    '--crashdump'
    '--crashdump-type'
    'Heap'
    '--crash-report-if-supported'
)
if (-not $NoCoverage) {
    $mtpArgs += @(
        '--coverage'
        '--coverage-output-format'
        'cobertura'
        '--coverage-settings'
        "$PSScriptRoot/test.runsettings"
    )
}

$v3Project = Join-Path $RepoRoot 'test\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests\Microsoft.VisualStudio.Sdk.TestFramework.Xunit.v3.Tests.csproj'
$v3BinLog = "-bl:$(Join-Path $buildLogs "test-v3-$runId.binlog")"
& $dotnet test $v3Project `
    --no-build `
    -c $Configuration `
    $v3BinLog `
    -- `
    @mtpArgs `
    @extraArgs
if ($LASTEXITCODE -ne 0) { $failedTests += 1 }

$trxFiles = Get-ChildItem -Recurse -Path $testLogs\*.trx
$unknownCounter = 0
$trxFiles |% {
  New-Item $testLogs -ItemType Directory -Force | Out-Null
  if (!($_.FullName.StartsWith($testLogs, [StringComparison]::OrdinalIgnoreCase))) {
    Copy-Item $_ -Destination $testLogs
  }

  if ($PublishResults) {
    $x = [xml](Get-Content -LiteralPath $_)
    $runTitle = $null
    if ($x.TestRun.TestDefinitions -and $x.TestRun.TestDefinitions.GetElementsByTagName('UnitTest')) {
      $storage = $x.TestRun.TestDefinitions.GetElementsByTagName('UnitTest')[0].storage -replace '\\','/'
      if ($storage -match '/(?<tfm>net[^/]+)/(?:(?<rid>[^/]+)/)?(?<lib>[^/]+)\.(dll|exe)$') {
        if ($matches.rid) {
          $runTitle = "$($matches.lib) ($($matches.tfm), $($matches.rid), $Agent)"
        } else {
          $runTitle = "$($matches.lib) ($($matches.tfm)$x86RunTitleSuffix, $Agent)"
        }
      }
    }
    if (!$runTitle) {
      $unknownCounter += 1;
      $runTitle = "unknown$unknownCounter ($Agent$x86RunTitleSuffix)";
    }

    Write-Host "##vso[results.publish type=VSTest;runTitle=$runTitle;publishRunAttachments=true;resultFiles=$_;failTaskOnFailedTests=true;testRunSystem=VSTS - PTR;]"
  }
}

if ($failedTests -ne 0) {
    exit $failedTests
}
