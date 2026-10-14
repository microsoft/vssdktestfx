#!/usr/bin/env pwsh

$ErrorActionPreference = 'Stop'
$workflow = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\..\.github\workflows\docs_validate.yml') -Raw
$pullRequestJob = [regex]::Match($workflow, '(?ms)^  build:\r?\n(?<job>.*?)(?=^  build-trusted:)')
if (!$pullRequestJob.Success -or $pullRequestJob.Groups['job'].Value -match 'id-token:\s*write|azure/login') {
    throw 'Fork pull-request documentation validation must not have OIDC permissions or Azure login.'
}

$forkCondition = "if: github.event_name == 'pull_request' && github.event.pull_request.head.repo.full_name != github.repository"
$trustedCondition = "if: github.event_name != 'pull_request' || github.event.pull_request.head.repo.full_name == github.repository"
if (!$pullRequestJob.Groups['job'].Value.Contains($forkCondition) -or
    !$workflow.Contains($trustedCondition)) {
    throw 'Same-repository PRs must use authenticated validation while fork PRs use anonymous validation.'
}

$trustedJob = [regex]::Match($workflow, '(?ms)^  build-trusted:\r?\n(?<job>.*)$').Groups['job'].Value
if ($trustedJob -notmatch 'id-token:\s*write' -or $trustedJob -notmatch 'uses: azure/login@') {
    throw 'Same-repository PR validation must have OIDC permission and Azure login.'
}

$credentialMatches = [regex]::Matches($workflow, '(?ms)      - name: Configure Azure Artifacts credentials\r?\n.*?        run: \|\r?\n(?<script>.*?)(?=      - name:)')
$match = $credentialMatches | Select-Object -Last 1
if (!$match.Success) {
    throw 'Credential setup step not found.'
}

$setup = [scriptblock]::Create(($match.Groups['script'].Value -replace '(?m)^          ', ''))
$directory = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
$oldEnv = $env:GITHUB_ENV
New-Item -ItemType Directory -Path $directory | Out-Null

function az {
    $global:LASTEXITCODE = 0
    'test-access-token'
}

Push-Location $directory
try {
    $env:GITHUB_ENV = Join-Path $directory 'github-env'
    @'
<configuration>
  <packageSources>
    <clear />
    <add key="msft_consumption" value="https://pkgs.dev.azure.com/azure-public/vside/_packaging/msft_consumption/nuget/v3/index.json" />
    <add key="msft_consumption_public" value="https://pkgs.dev.azure.com/azure-public/vside/_packaging/msft_consumption_public/nuget/v3/index.json" />
    <add key="legacy" value="https://example.pkgs.visualstudio.com/_packaging/feed/nuget/v3/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
'@ | Set-Content -LiteralPath nuget.config

    $output = & $setup
    if ($output -ne '::add-mask::test-access-token') {
        throw 'Token was not masked.'
    }

    $lines = @(Get-Content -LiteralPath $env:GITHUB_ENV)
    if ($lines.Count -ne 3) {
        throw 'Expected credentials for three Azure Artifacts sources only.'
    }

    foreach ($name in @('msft_consumption', 'msft_consumption_public', 'legacy')) {
        if ($lines -cnotcontains "NuGetPackageSourceCredentials_${name}=Username=ado;Password=test-access-token;ValidAuthenticationTypes=Basic") {
            throw "Credentials missing for '$name': $($lines -join ', ')."
        }
    }

    '<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath nuget.config
    $failed = $false
    try {
        & $setup | Out-Null
    }
    catch {
        if ($_.Exception.Message -ne 'No Azure Artifacts package sources were found in nuget.config.') {
            throw
        }

        $failed = $true
    }

    if (!$failed) {
        throw 'Missing feed configuration must fail explicitly.'
    }

    Write-Host 'Docs feed credential tests passed.'
}
finally {
    Pop-Location
    $env:GITHUB_ENV = $oldEnv
    Remove-Item -LiteralPath (Join-Path $directory 'nuget.config'), (Join-Path $directory 'github-env') -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $directory -Force
}
