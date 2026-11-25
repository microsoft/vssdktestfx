#!/usr/bin/env pwsh

$ErrorActionPreference = 'Stop'
$directory = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
$oldStagingDirectory = $env:BUILD_ARTIFACTSTAGINGDIRECTORY
try {
    $tools = Join-Path $directory 'tools'
    $artifactScripts = Join-Path $tools 'artifacts'
    New-Item -ItemType Directory -Path $artifactScripts, (Join-Path $directory 'bin'), (Join-Path $directory 'obj') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\artifacts\coverageResults.ps1') -Destination $artifactScripts
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\Get-ArtifactsStagingDirectory.ps1') -Destination $tools

    $env:BUILD_ARTIFACTSTAGINGDIRECTORY = Join-Path $directory 'obj\_artifacts'
    $logs = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY 'test_logs'
    $files = @{
        'run1\report.cobertura.xml' = "same $directory"
        'run1\result\In\host\report.cobertura.xml' = "same $directory"
        'run1\result\In\host\unique.cobertura.xml' = 'unique'
        'run1\result\In\other\report.cobertura.xml' = 'different'
        'run2\result\In\host\report.cobertura.xml' = "same $directory"
    }
    foreach ($entry in $files.GetEnumerator()) {
        $path = Join-Path $logs $entry.Key
        New-Item -ItemType Directory -Path (Split-Path $path) -Force | Out-Null
        Set-Content -LiteralPath $path -Value $entry.Value
    }

    foreach ($iteration in 1..2) {
        $result = & (Join-Path $artifactScripts 'coverageResults.ps1')
        $reports = @($result[$logs])
        $rootEntry = @($result.GetEnumerator() | Where-Object { $_.Key.ToString() -eq $directory })
        $rootReports = if ($rootEntry.Count -eq 1) { @($rootEntry[0].Value) } else { @() }
        if ($reports.Count -ne 4 -or $rootEntry.Count -ne 1 -or $rootReports.Count -ne 0) {
            throw "Expected four reports from test logs and none from the root; got $($reports.Count), $($rootReports.Count), and $($rootEntry.Count) root entries."
        }

        foreach ($relativePath in @('run1\report.cobertura.xml', 'run1\result\In\host\unique.cobertura.xml', 'run1\result\In\other\report.cobertura.xml', 'run2\result\In\host\report.cobertura.xml')) {
            if ($reports.FullName -notcontains (Join-Path $logs $relativePath)) {
                throw "Coverage report was not collected: $relativePath"
            }
        }
    }

    Write-Host 'Coverage collection tests passed.'
}
finally {
    $env:BUILD_ARTIFACTSTAGINGDIRECTORY = $oldStagingDirectory
    if (Test-Path -LiteralPath $directory) {
        Remove-Item -LiteralPath $directory -Recurse -Force
    }
}
