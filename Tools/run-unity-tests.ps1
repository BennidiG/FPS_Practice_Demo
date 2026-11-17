param(
    [Parameter(Mandatory = $true)]
    [string]$UnityPath
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$UnityPath = (Resolve-Path $UnityPath).Path
if ([System.IO.Path]::GetFileName($UnityPath) -ne 'Unity.exe') {
    throw 'UnityPath must point to Unity.exe.'
}

$results = Join-Path $root 'ValidationResults'
New-Item -ItemType Directory -Path $results -Force | Out-Null
foreach ($platform in @('EditMode', 'PlayMode')) {
    $xml = Join-Path $results "$platform-results.xml"
    $log = Join-Path $results "$platform-unity.log"
    Write-Output "Running Unity $platform tests..."
    & $UnityPath -batchmode -quit -projectPath $root -runTests -testPlatform $platform -testResults $xml -logFile $log
    if ($LASTEXITCODE -ne 0) {
        throw "Unity $platform tests exited with code $LASTEXITCODE. See $log"
    }
    if (-not (Test-Path $xml)) {
        throw "Unity $platform tests did not produce $xml"
    }

    [xml]$report = [System.IO.File]::ReadAllText($xml)
    $failed = [int]$report.'test-run'.failed
    if ($failed -ne 0 -or $report.'test-run'.result -ne 'Passed') {
        throw "Unity $platform tests failed. See $xml and $log"
    }
    Write-Output "$platform PASS: total=$($report.'test-run'.total), passed=$($report.'test-run'.passed)"
}
