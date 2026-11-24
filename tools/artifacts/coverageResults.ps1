$RepoRoot = Resolve-Path "$PSScriptRoot\..\.."

$ArtifactStagingFolder = & "$PSScriptRoot/../Get-ArtifactsStagingDirectory.ps1"
$directTestLogs = Join-Path $ArtifactStagingFolder test_logs
$testLogsPrefix = [IO.Path]::GetFullPath($directTestLogs).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
$coverageFilesUnderRoot = @(Get-ChildItem "$RepoRoot/*.cobertura.xml" -Recurse | Where-Object {
    $_.FullName -notmatch '[\\/]In[\\/]' -and
    !$_.FullName.StartsWith($testLogsPrefix, [StringComparison]::OrdinalIgnoreCase)
})

# Test logs can contain both a report and an identical copy in a TRX attachment.
$coverageFilesUnderArtifacts = @()
if (Test-Path $directTestLogs) {
    $reports = @(Get-ChildItem "$directTestLogs/*.cobertura.xml" -Recurse)
    $standaloneReports = @($reports | Where-Object { $_.FullName -notmatch '[\\/]In[\\/]' })
    $coverageFilesUnderArtifacts += $standaloneReports
    foreach ($attachment in @($reports | Where-Object { $_.FullName -match '[\\/]In[\\/]' })) {
        $attachmentRun = ($attachment.FullName.Substring($testLogsPrefix.Length) -split '[\\/]')[0]
        $matchingReports = @($standaloneReports | Where-Object {
            $reportRun = ($_.FullName.Substring($testLogsPrefix.Length) -split '[\\/]')[0]
            $_.Name -eq $attachment.Name -and $reportRun -eq $attachmentRun
        })
        if ($matchingReports.Count -gt 0) {
            $attachmentContent = [IO.File]::ReadAllText($attachment.FullName).Replace("$RepoRoot", '{reporoot}').TrimEnd("`r", "`n")
            if (@($matchingReports | Where-Object {
                [IO.File]::ReadAllText($_.FullName).Replace("$RepoRoot", '{reporoot}').TrimEnd("`r", "`n") -ceq $attachmentContent
            }).Count -gt 0) {
                continue
            }
        }

        $coverageFilesUnderArtifacts += $attachment
    }
}

# Prepare code coverage reports for merging on another machine
Write-Host "Substituting $repoRoot with `"{reporoot}`""
@($coverageFilesUnderRoot + $coverageFilesUnderArtifacts) |? { $_ }|% {
    $content = Get-Content -LiteralPath $_ |% { $_ -Replace [regex]::Escape($repoRoot), "{reporoot}" }
    Set-Content -LiteralPath $_ -Value $content -Encoding UTF8
}

if (!((Test-Path $RepoRoot\bin) -and (Test-Path $RepoRoot\obj))) { return }

@{
    $directTestLogs = $coverageFilesUnderArtifacts;
    $RepoRoot = (
        $coverageFilesUnderRoot +
        (Get-ChildItem "$RepoRoot\obj\*.cs" -Recurse)
    );
}
