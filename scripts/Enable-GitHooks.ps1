[CmdletBinding()]
param(
    [switch]$VerifyOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$hooksDirectory = Join-Path $repoRoot '.githooks'

function Format-Value {
    param(
        [AllowNull()]
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return '<unset>'
    }

    return $Value.Trim()
}

function Write-StatusLine {
    param(
        [string]$Label,
        [string]$Value
    )

    Write-Host ("{0,-16}: {1}" -f $Label, $Value)
}

function Get-ConfiguredHooksPath {
    $configuredPath = git -C $repoRoot config --get core.hooksPath 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return $configuredPath
}

if (-not (Test-Path $hooksDirectory -PathType Container)) {
    throw "Git hooks directory '$hooksDirectory' was not found."
}

$requiredHookPaths = @(
    (Join-Path $hooksDirectory '_shared.sh')
    (Join-Path $hooksDirectory 'pre-commit')
    (Join-Path $hooksDirectory 'commit-msg')
    (Join-Path $hooksDirectory 'pre-push')
)

foreach ($hookPath in $requiredHookPaths) {
    if (-not (Test-Path $hookPath -PathType Leaf)) {
        throw "Required hook '$hookPath' was not found."
    }
}

$configuredPath = Get-ConfiguredHooksPath
$formattedConfiguredPath = Format-Value $configuredPath

Write-StatusLine 'Repository root' $repoRoot
Write-StatusLine 'Hooks directory' $hooksDirectory
Write-StatusLine 'Configured path' $formattedConfiguredPath

if ($VerifyOnly) {
    if ($configuredPath -eq '.githooks') {
        Write-Host 'Git hooks are enabled via core.hooksPath=.githooks'
        exit 0
    }

    Write-Error "Git hooks are not enabled for this repository. Current core.hooksPath: '$formattedConfiguredPath'"
    exit 1
}

Write-Host 'Installing Git hooks configuration...'
git -C $repoRoot config --local core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to set core.hooksPath to .githooks.'
}

$verifiedPath = Get-ConfiguredHooksPath
if ($LASTEXITCODE -ne 0 -or $verifiedPath -ne '.githooks') {
    throw "core.hooksPath verification failed. Current value: '$verifiedPath'"
}

Write-Host 'Git hooks enabled.'
Write-StatusLine 'Repository root' $repoRoot
Write-StatusLine 'Hooks directory' $hooksDirectory
Write-StatusLine 'Configured path' $verifiedPath
Write-StatusLine 'Required hooks' ($requiredHookPaths.Count.ToString())
