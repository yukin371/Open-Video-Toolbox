[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$hooksDirectory = Join-Path $repoRoot '.githooks'
$requiredHookHeaders = [ordered]@{
    '_shared.sh'  = '#!/usr/bin/env bash'
    'pre-commit'  = '#!/usr/bin/env bash'
    'commit-msg'  = '#!/usr/bin/env bash'
    'pre-push'    = '#!/usr/bin/env bash'
}

$hasError = $false

function Write-Ok {
    param([string]$Message)
    Write-Host "[OK] $Message"
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[XX] $Message"
    $script:hasError = $true
}

function Get-ConfiguredHooksPath {
    $configuredPath = git -C $repoRoot config --get core.hooksPath 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return $configuredPath
}

Write-Host "Repository root : $repoRoot"
Write-Host "Hooks directory : $hooksDirectory"

if (Test-Path $hooksDirectory -PathType Container) {
    Write-Ok 'Hooks directory exists.'
}
else {
    Write-Fail 'Hooks directory is missing.'
}

foreach ($entry in $requiredHookHeaders.GetEnumerator()) {
    $hookPath = Join-Path $hooksDirectory $entry.Key
    if (-not (Test-Path $hookPath -PathType Leaf)) {
        Write-Fail "Required hook file is missing: $hookPath"
        continue
    }

    $firstLine = Get-Content $hookPath -TotalCount 1
    if ($firstLine -ne $entry.Value) {
        Write-Fail "Hook header mismatch: $hookPath"
        continue
    }

    Write-Ok "Hook file is present: $hookPath"
}

$configuredPath = Get-ConfiguredHooksPath
if ($configuredPath -eq '.githooks') {
    Write-Ok 'core.hooksPath is set to .githooks.'
}
else {
    $displayPath = if ([string]::IsNullOrWhiteSpace($configuredPath)) { '<unset>' } else { $configuredPath }
    Write-Fail "core.hooksPath is not configured correctly. Current value: $displayPath"
}

if ($hasError) {
    exit 1
}

Write-Ok 'Git hooks setup check passed.'
