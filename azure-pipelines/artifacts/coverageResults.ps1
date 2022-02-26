$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")

if (!((Test-Path $RepoRoot\bin) -and (Test-Path $RepoRoot\obj))) { return }

@{
    $RepoRoot = (
        @(Get-ChildItem "$RepoRoot\bin\coverage.*cobertura.xml" -Recurse) +
        (Get-ChildItem "$RepoRoot\obj\*.cs" -Recurse)
    );
}
