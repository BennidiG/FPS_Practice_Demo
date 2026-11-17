$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$work = Join-Path $root 'Temp\FpsPractice.Performance'
$results = Join-Path $root 'ValidationResults'
New-Item -ItemType Directory -Path $work -Force | Out-Null
New-Item -ItemType Directory -Path $results -Force | Out-Null
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw 'Offline C# compiler is unavailable.' }

$exe = Join-Path $work 'FpsPractice.PerformanceBenchmark.exe'
$response = Join-Path $work 'compile.rsp'
$sources = Get-ChildItem (Join-Path $root 'Assets\Scripts') -Filter '*.cs' -File -Recurse | ForEach-Object { $_.FullName }
$sources += Join-Path $root 'Tools\Validation\UnityStubs.cs'
$sources += Join-Path $root 'Tools\Validation\PerformanceBenchmark.cs'
$lines = @('/nologo', '/target:exe', "/out:`"$exe`"", '/langversion:5', '/reference:System.dll', '/reference:System.Core.dll')
$lines += $sources | ForEach-Object { '"' + $_ + '"' }
[System.IO.File]::WriteAllLines($response, $lines, [System.Text.Encoding]::UTF8)
$output = & $csc "@$response" 2>&1
if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
$output = & $exe 2>&1
if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
$text = $output -join [Environment]::NewLine
[System.IO.File]::WriteAllText((Join-Path $results 'performance-benchmark.txt'), $text, [System.Text.UTF8Encoding]::new($false))
Write-Output $text
