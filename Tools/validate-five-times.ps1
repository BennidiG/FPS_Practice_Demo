$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
for ($run = 1; $run -le 5; $run++) {
    Write-Output "===== VALIDATION $run/5 ====="
    & (Join-Path $PSScriptRoot 'validate.ps1') -RunNumber $run
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
Write-Output 'ALL FIVE VALIDATION RUNS PASSED (offline checks; Unity editor tests still require Unity 2021.3.8f1c1).'
