$v = & "packages\Nerdbank.GitVersioning.1.1.2-rc\tools\Get-Version.ps1"
Write-Output "##vso[task.setvariable variable=GitBuildNumber;]$v"
