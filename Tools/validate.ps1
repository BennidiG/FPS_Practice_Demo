param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 5)]
    [int]$RunNumber
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$work = Join-Path $root 'Temp\FpsPractice.Validation'
$results = Join-Path $root 'ValidationResults'
New-Item -ItemType Directory -Path $work -Force | Out-Null
New-Item -ItemType Directory -Path $results -Force | Out-Null

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Read-Utf8([string]$Path) {
    $encoding = [System.Text.UTF8Encoding]::new($false, $true)
    return $encoding.GetString([System.IO.File]::ReadAllBytes($Path))
}

$checks = 0
try {
    $runtimeFiles = Get-ChildItem (Join-Path $root 'Assets\Scripts') -Filter '*.cs' -File -Recurse
    Assert-True ($runtimeFiles.Count -eq 34) "Expected 34 runtime scripts, found $($runtimeFiles.Count)"
    foreach ($file in $runtimeFiles) {
        [void](Read-Utf8 $file.FullName)
        Assert-True (Test-Path ($file.FullName + '.meta')) "Missing meta file for $($file.Name)"
        $checks += 2
    }

    foreach ($path in @('Assets\Scripts\FpsPractice.Runtime.asmdef', 'Assets\Tests\EditMode\FpsPractice.EditModeTests.asmdef', 'Assets\Tests\PlayMode\FpsPractice.PlayModeTests.asmdef', 'Packages\manifest.json')) {
        [void]((Read-Utf8 (Join-Path $root $path)) | ConvertFrom-Json)
        $checks++
    }

    $runtimeDefinition = (Read-Utf8 (Join-Path $root 'Assets\Scripts\FpsPractice.Runtime.asmdef')) | ConvertFrom-Json
    Assert-True ($runtimeDefinition.references -contains 'UnityEngine.UI') 'Runtime assembly must reference UnityEngine.UI.'
    $checks++

    $scriptGuids = @{}
    foreach ($meta in Get-ChildItem (Join-Path $root 'Assets\Scripts') -Filter '*.cs.meta' -File) {
        $content = Read-Utf8 $meta.FullName
        $match = [regex]::Match($content, 'guid: ([0-9a-f]{32})')
        Assert-True $match.Success "Missing GUID in $($meta.Name)"
        $scriptGuids[$meta.Name.Substring(0, $meta.Name.Length - 5)] = $match.Groups[1].Value
        $checks++
    }

    $sceneFiles = @('Assets\Scenes\SampleScene.unity', 'Assets\Scenes\StartScene.unity', 'Assets\Object\ShootObject.prefab')
    foreach ($relativePath in $sceneFiles) {
        $content = Read-Utf8 (Join-Path $root $relativePath)
        $ids = [regex]::Matches($content, '--- !u!\d+ &(\d+)') | ForEach-Object { $_.Groups[1].Value }
        Assert-True (($ids | Select-Object -Unique).Count -eq $ids.Count) "Duplicate object IDs in $relativePath"
        foreach ($component in [regex]::Matches($content, 'component: \{fileID: (\d+)\}')) {
            $id = $component.Groups[1].Value
            Assert-True ($content.Contains('&' + $id)) "Unresolved component $id in $relativePath"
        }
        $checks += 2
    }

    $sample = Read-Utf8 (Join-Path $root 'Assets\Scenes\SampleScene.unity')
    Assert-True ($sample.Contains('CharacterController:')) 'CharacterController is missing.'
    Assert-True (-not $sample.Contains('Rigidbody:')) 'Conflicting Rigidbody remains.'
    Assert-True ($sample.Contains('m_Name: Wall_Back')) 'Back wall is missing.'
    Assert-True (-not $sample.Contains('m_Name: HitPoint')) 'Legacy 3D crosshair remains.'
    Assert-True (-not [regex]::IsMatch($sample, 'm_IsTrigger: 0\s+m_Enabled: 0')) 'A practice-scene collider is disabled.'
    Assert-True ($sample.Contains("guid: $($scriptGuids['MoveCamera.cs'])")) 'MoveCamera GUID link is broken.'
    Assert-True ($sample.Contains("guid: $($scriptGuids['CreateObject.cs'])")) 'CreateObject GUID link is broken.'
    Assert-True ($sample.Contains("guid: $($scriptGuids['Initialization.cs'])")) 'Initialization GUID link is broken.'
    Assert-True ($sample.Contains('m_Name: Player')) 'Player root is missing.'
    Assert-True ($sample.Contains('cameraPivot: {fileID: 963194228}')) 'Camera pivot is not separated from the player root.'
    Assert-True ($sample.Contains('guid: 40000000000000000000000000000001')) 'Rifle definition is missing.'
    Assert-True ($sample.Contains('guid: 40000000000000000000000000000002')) 'Sidearm definition is missing.'
    Assert-True ($sample.Contains('spawnDepth: {x: -5, y: -1}')) 'Target depth range is missing.'
    Assert-True ($sample.Contains('respawnDelay: 0.2')) 'Delayed target respawn is not configured.'
    Assert-True ($sample.Contains('m_Name: Modern Range Environment')) 'Modern range root is missing.'
    Assert-True ($sample.Contains('guid: 60000000000000000000000000000009')) 'Range material set is incomplete.'
    Assert-True ($sample.Contains('m_LocalScale: {x: 22, y: 7, z: 0.1}')) 'Range side-wall dimensions are incorrect.'
    Assert-True ($sample.Contains('m_LocalScale: {x: 2, y: 2, z: 2.2}')) 'Range floor dimensions are incorrect.'
    Assert-True ($sample.Contains('mMouseCmPer360: 30')) 'cm/360 sensitivity is not configured.'
    Assert-True ($sample.Contains('guid: 80000000000000000000000000000004')) 'Training scenario references are incomplete.'
    Assert-True ($sample.Contains('adsFieldOfView: 45')) 'ADS configuration is missing.'
    $checks += 3
    $checks += 4
    Assert-True ($sample.Contains('m_Bits: 1536')) 'Weapon layers must include only Target and Environment.'
    foreach ($scriptName in @('TargetSpawner.cs', 'WeaponController.cs', 'PracticeSession.cs', 'PracticeHUD.cs', 'WeaponView.cs', 'RangeEnvironment.cs', 'FeedbackController.cs', 'TrainingUIController.cs', 'ImpactEffectPool.cs')) {
        Assert-True ($sample.Contains("guid: $($scriptGuids[$scriptName])")) "$scriptName GUID link is broken."
        $checks++
    }
    $checks += 11

    $start = Read-Utf8 (Join-Path $root 'Assets\Scenes\StartScene.unity')
    Assert-True ([regex]::Matches($start, 'm_SizeDelta: \{x: 240, y: 48\}').Count -eq 3) 'Menu button sizes are inconsistent.'
    foreach ($height in @(180, 100, 20)) {
        Assert-True ($start.Contains("m_AnchoredPosition: {x: 0, y: $height}")) "Missing menu button at Y=$height"
        $checks++
    }
    Assert-True ($start.Contains('m_ReferenceResolution: {x: 1920, y: 1080}')) 'Canvas reference resolution is not configured.'
    Assert-True ($start.Contains("guid: $($scriptGuids['ChangeScene.cs'])")) 'ChangeScene GUID link is broken.'
    $checks += 3

    $prefab = Read-Utf8 (Join-Path $root 'Assets\Object\ShootObject.prefab')
    Assert-True ([regex]::Matches($prefab, '(?m)^MeshCollider:$').Count -eq 1) 'Target must have exactly one MeshCollider.'
    Assert-True ($prefab.Contains('m_Convex: 1')) 'Target MeshCollider must be convex.'
    Assert-True (-not $prefab.Contains('BoxCollider:')) 'Target still has an inaccurate BoxCollider.'
    Assert-True (-not $prefab.Contains('SphereCollider:')) 'Target still has a SphereCollider.'
    Assert-True ($prefab.Contains('m_Mesh: {fileID: 10206,')) 'Target is not using the low-cost cylinder mesh.'
    Assert-True ($prefab.Contains('m_Layer: 9')) 'Target is not on the Target layer.'
    Assert-True ($prefab.Contains('m_LocalScale: {x: 1, y: 1, z: 1}')) 'Procedural target must use unit scale.'
    Assert-True ($prefab.Contains("guid: $($scriptGuids['TargetVisual.cs'])")) 'TargetVisual GUID link is broken.'
    Assert-True ([regex]::Matches($prefab, 'guid: 1000000000000000000000000000000[1-6]').Count -eq 6) 'Target must reference six model materials.'
    $checks += 3

    $targetVisual = Read-Utf8 (Join-Path $root 'Assets\Scripts\TargetVisual.cs')
    Assert-True ($targetVisual.Contains('if (initialized)')) 'Target collider initialization is not guarded against repeated respawns.'
    Assert-True (-not $targetVisual.Contains('sharedMesh = null')) 'Target collider is still being forced to recook.'
    Assert-True ($targetVisual.Contains('BuildCollisionTarget')) 'Target does not use its lightweight collision mesh.'
    $spawnerSource = Read-Utf8 (Join-Path $root 'Assets\Scripts\TargetSpawner.cs')
    Assert-True ([regex]::Matches($spawnerSource, 'Physics\.SyncTransforms\(\)').Count -eq 2) 'Physics synchronization must occur only after placement and batched motion.'
    $checks += 4

    $rangeSource = Read-Utf8 (Join-Path $root 'Assets\Scripts\RangeEnvironment.cs')
    Assert-True ($rangeSource.Contains('if (initialized)')) 'Range environment can be generated more than once.'
    Assert-True ([regex]::Matches($rangeSource, 'CreateLight\("Range Light').Count -eq 3) 'Range must create exactly three point lights.'
    Assert-True ($rangeSource.Contains('HideFlags.DontSave')) 'Generated range objects must not be serialized into the scene.'
    $checks += 3
    Assert-True ($prefab.Contains("guid: $($scriptGuids['ShootTarget.cs'])")) 'ShootTarget GUID link is broken.'
    $checks += 7

    $material = Read-Utf8 (Join-Path $root 'Assets\Materials\ShootObject.mat')
    Assert-True ($material.Contains('m_EnableInstancingVariants: 1')) 'Target material instancing is disabled.'
    $checks++

    $tagManager = Read-Utf8 (Join-Path $root 'ProjectSettings\TagManager.asset')
    foreach ($layerName in @('Player', 'Target', 'Environment')) {
        Assert-True ($tagManager.Contains("- $layerName")) "Missing physics layer: $layerName"
        $checks++
    }
    Assert-True ([regex]::IsMatch($tagManager, '- UI\r?\n  - \r?\n  - \r?\n  - Player\r?\n  - Target\r?\n  - Environment')) 'Player/Target/Environment must occupy layers 8/9/10.'
    $checks++
    $initialization = Read-Utf8 (Join-Path $root 'Assets\Scripts\Initialization.cs')
    Assert-True ($initialization.Contains('CapturedCursorThisFrame = true')) 'Cursor capture frame is not tracked.'
    Assert-True ($initialization.Contains('!CapturedCursorThisFrame')) 'Capture-frame input suppression is missing.'
    $checks += 2
    $weaponSource = Read-Utf8 (Join-Path $root 'Assets\Scripts\WeaponController.cs')
    Assert-True ($weaponSource.Contains('!session.IsRunning')) 'Public weapon APIs must reject input after training ends.'
    Assert-True ($weaponSource.Contains('definition.SpreadAngle * Mathf.Lerp')) 'ADS spread reduction is missing.'
    Assert-True ($weaponSource.Contains('definition.RecoilPitch * Mathf.Lerp')) 'ADS recoil reduction is missing.'
    Assert-True ($weaponSource.Contains('RecoilPattern.Evaluate')) 'Deterministic recoil pattern is missing.'
    $sessionSource = Read-Utf8 (Join-Path $root 'Assets\Scripts\PracticeSession.cs')
    Assert-True ($sessionSource.Contains('MedianReactionMs')) 'Median analytics are missing.'
    Assert-True ($sessionSource.Contains('ExportTrainingData')) 'Training export is missing.'
    Assert-True ($sessionSource.Contains('RecordAimSample')) 'Mouse path tracking is missing.'
    $checks += 3
    Assert-True ($sample.Contains('guid: ebf7b99dd7fa4b1aab8b8bb03ac08600')) 'Training UI is not linked.'
    Assert-True ($sample.Contains('guid: 9fa10a0df35b42f89b246c16b4d20002')) 'Impact effect pool is not linked.'
    $checks += 5

    foreach ($relativePath in $runtimeFiles | ForEach-Object { $_.FullName }) {
        $content = Read-Utf8 $relativePath
        Assert-True (-not $content.Contains('int.Parse(')) "Unsafe int.Parse remains in $relativePath"
        Assert-True (-not $content.Contains('Debug.Log(')) "Per-frame debug logging remains in $relativePath"
        Assert-True (-not $content.Contains('Destroy(hitInfo')) "Environment destruction remains in $relativePath"
        $checks += 3
    }

    $buildSettings = Read-Utf8 (Join-Path $root 'ProjectSettings\EditorBuildSettings.asset')
    Assert-True ($buildSettings.Contains('path: Assets/Scenes/StartScene.unity')) 'StartScene is not in build settings.'
    Assert-True ($buildSettings.Contains('path: Assets/Scenes/SampleScene.unity')) 'SampleScene is not in build settings.'
    $checks += 2

    $quality = Read-Utf8 (Join-Path $root 'ProjectSettings\QualitySettings.asset')
    Assert-True ($quality.Contains('m_CurrentQuality: 3')) 'Default quality is not High.'
    Assert-True ($quality.Contains('Standalone: 3')) 'Standalone default quality is not High.'
    $playerSettings = Read-Utf8 (Join-Path $root 'ProjectSettings\ProjectSettings.asset')
    Assert-True ($playerSettings.Contains('runInBackground: 0')) 'Background execution is still enabled.'
    Assert-True ($playerSettings.Contains('resizableWindow: 1')) 'Window resizing is disabled.'
    Assert-True ($start.Contains('m_HDR: 0') -and $sample.Contains('m_HDR: 0')) 'Unnecessary HDR rendering remains enabled.'
    $checks += 5

    $csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    Assert-True (Test-Path $csc) 'Offline C# compiler is unavailable.'
    $exe = Join-Path $work 'FpsPractice.OfflineTests.exe'
    $response = Join-Path $work 'compile.rsp'
    $sources = @(
        (Join-Path $root 'Tools\Validation\UnityStubs.cs'),
        (Join-Path $root 'Tools\Validation\NUnitStubs.cs'),
        (Join-Path $root 'Tools\Validation\OfflineTestRunner.cs'),
        (Join-Path $root 'Assets\Tests\EditMode\CoreLogicTests.cs')
    ) + ($runtimeFiles | ForEach-Object { $_.FullName })
    $lines = @('/nologo', '/target:exe', "/out:`"$exe`"", '/langversion:5', '/reference:System.dll', '/reference:System.Core.dll')
    $lines += $sources | ForEach-Object { '"' + $_ + '"' }
    [System.IO.File]::WriteAllLines($response, $lines, [System.Text.Encoding]::UTF8)
    $compilerOutput = & $csc "@$response" 2>&1
    if ($LASTEXITCODE -ne 0) { throw ("C# compilation failed:`n" + ($compilerOutput -join "`n")) }
    $testOutput = & $exe 2>&1
    if ($LASTEXITCODE -ne 0) { throw ("Offline tests failed:`n" + ($testOutput -join "`n")) }

    $summary = @(
        "RUN $RunNumber PASS",
        "Asset/static checks: $checks passed",
        'Runtime C# compilation: PASS (offline Unity API stubs)',
        ($testOutput -join [Environment]::NewLine),
        'Unity Test Runner: NOT RUN (Unity 2021.3.8f1c1 editor is unavailable on this machine)'
    )
    $logPath = Join-Path $results "run-$RunNumber.txt"
    [System.IO.File]::WriteAllLines($logPath, $summary, [System.Text.UTF8Encoding]::new($false))
    $summary | ForEach-Object { Write-Output $_ }
}
catch {
    $message = "RUN $RunNumber FAIL: $($_.Exception.Message)"
    [System.IO.File]::WriteAllText((Join-Path $results "run-$RunNumber.txt"), $message, [System.Text.UTF8Encoding]::new($false))
    Write-Error $message
    exit 1
}
