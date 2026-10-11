param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDirectory = "artifacts/desktop-publish/$RuntimeIdentifier"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "..\src\OpenVideoToolbox.Desktop\OpenVideoToolbox.Desktop.csproj"
$output = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\$OutputDirectory"))

dotnet publish $project `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained false `
    --output $output `
    --nologo

$desktopExe = Join-Path $output "OpenVideoToolbox.Desktop.exe"
if (-not (Test-Path $desktopExe)) {
    throw "Desktop publish output is missing: $desktopExe"
}

$depsJson = Join-Path $output "OpenVideoToolbox.Desktop.deps.json"
if (-not (Test-Path $depsJson)) {
    throw "Desktop publish output is missing dependency metadata: $depsJson"
}

Write-Output "Desktop publish smoke passed: $desktopExe"
