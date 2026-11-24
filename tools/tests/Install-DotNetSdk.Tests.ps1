#!/usr/bin/env pwsh

$ErrorActionPreference = 'Stop'
$target = Join-Path $PSScriptRoot '..\Install-DotNetSdk.ps1'
$tokens = $null
$errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($target, [ref]$tokens, [ref]$errors)
if ($errors.Count) {
    throw 'SDK installation script has syntax errors.'
}

# Load only the presence checks so these tests cannot download or install anything.
foreach ($name in @('Test-DotNetRuntimeInstalled', 'Test-DotNetSdkInstalled')) {
    $function = $ast.Find({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true)
    . ([scriptblock]::Create($function.Extent.Text))
}

$directory = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
try {
    New-Item -ItemType Directory -Path (Join-Path $directory 'sdk\10.0.401') -Force | Out-Null
    foreach ($runtime in @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App', 'Microsoft.AspNetCore.App')) {
        New-Item -ItemType Directory -Path (Join-Path $directory "shared\$runtime\8.0.0") -Force | Out-Null
    }

    foreach ($Upgrade in @($false, $true)) {
        $expected = !$Upgrade
        foreach ($runtime in @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App', 'Microsoft.AspNetCore.App')) {
            if ((Test-DotNetRuntimeInstalled $directory $runtime '8.0') -ne $expected) {
                throw "Unexpected runtime presence check for $runtime with Upgrade=$Upgrade."
            }
        }

        if ((Test-DotNetSdkInstalled $directory '10.0.401' $null) -ne $expected -or
            (Test-DotNetSdkInstalled $directory $null '10.0') -ne $expected) {
            throw "Unexpected SDK presence check with Upgrade=$Upgrade."
        }
    }

    $Upgrade = $false
    if (Test-DotNetRuntimeInstalled $directory Microsoft.NETCore.App '9.0') {
        throw 'A missing runtime channel must not be treated as installed.'
    }

    if (Test-DotNetSdkInstalled $directory '10.0.402' $null) {
        throw 'A missing SDK version must not be treated as installed.'
    }

    $init = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\..\init.ps1') -Raw
    if ($init -notmatch 'Install-DotNetSdk\.ps1".*-Upgrade:\$UpgradePrerequisites') {
        throw 'init.ps1 must forward UpgradePrerequisites to the SDK installer.'
    }

    Write-Host 'SDK installation presence-check tests passed.'
}
finally {
    Remove-Item -LiteralPath $directory -Recurse -Force
}
