# Run SoundSelf Test Runner (EditMode) tests via Unity batchmode.
# Usage (from repo root):
#   .\Tools\run-editmode-tests.ps1
#   .\Tools\run-editmode-tests.ps1 -TestFilter "Block3PolicyEditModeTests"
#
# Optional env: UNITY_EDITOR_PATH = full path to Unity.exe

param(
    [string]$TestFilter = "Block3PolicyEditModeTests",
    [string]$ResultsDir = "TestResults"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$VersionFile = Join-Path $ProjectRoot "ProjectSettings\ProjectVersion.txt"
if (-not (Test-Path $VersionFile)) {
    throw "ProjectVersion.txt not found at $VersionFile"
}

$versionLine = Get-Content $VersionFile | Where-Object { $_ -match '^m_EditorVersion:\s*(\S+)' } | Select-Object -First 1
if (-not $versionLine) { throw "Could not read Unity version from ProjectVersion.txt" }
$unityVersion = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()

$unityExe = $env:UNITY_EDITOR_PATH
if (-not $unityExe) {
    $hubPath = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
    if (Test-Path $hubPath) { $unityExe = $hubPath }
}
if (-not $unityExe -or -not (Test-Path $unityExe)) {
    throw @"
Unity.exe not found. Set UNITY_EDITOR_PATH to your Editor\Unity.exe
  (project expects $unityVersion).
"@
}

$resultsPath = Join-Path $ProjectRoot $ResultsDir
New-Item -ItemType Directory -Force -Path $resultsPath | Out-Null
$xmlOut = Join-Path $resultsPath "editmode-results.xml"
$logOut = Join-Path $resultsPath "editmode-run.log"

# Do not pass -quit with -runTests: Unity can shut down after project load before the Test Runner runs.
$testArgs = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectRoot,
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", $xmlOut,
    "-logFile", $logOut
)
if ($TestFilter) {
    $testArgs += @("-testFilter", $TestFilter)
}

Write-Host "Unity: $unityExe"
Write-Host "Filter: $TestFilter"
Write-Host "Results: $xmlOut"

& $unityExe @testArgs
$exit = $LASTEXITCODE
if ($exit -ne 0) {
    Write-Host "Unity exited with code $exit. See $logOut"
    exit $exit
}

if (-not (Test-Path $xmlOut)) {
    Write-Error "No results XML at $xmlOut - tests may not have run (see $logOut)."
    exit 1
}

[xml]$xml = Get-Content $xmlOut
$root = $xml."test-run"
if (-not $root) {
    Write-Error "Malformed results XML at $xmlOut"
    exit 1
}

$passed = [int]$root.passed
$failed = [int]$root.failed
$total = [int]$root.total
Write-Host "EditMode: $passed/$total passed, $failed failed (result=$($root.result))."
if ($failed -gt 0) { exit 1 }
Write-Host "EditMode tests finished OK."
