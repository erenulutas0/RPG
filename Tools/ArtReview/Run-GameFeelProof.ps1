#requires -Version 7
# Isolated, graphics-enabled capture of the game-feel pass of 2026-11-22 (docs/43 section 1, GameFeelCapture.cs): the
# slot cues (a Sword's flourish, a Staff's pulse, the Daggers' flick, alone and among real strikes), the hero's recoil,
# level pop, picked weapon and death into ash, an enemy's flinch from either side, its dissolve and a crowd's, a horde
# newcomer rising out of embers, the Forge Warden's tell, phase two and fall, and the Glass Regent's feet ring, cast and
# shatter - each framed on its subject at 540x540. No authored import, scene save, APK or device operation. Frames land
# in ArtDirection/2026-11-22/game-feel-01 as PNG named by section and frame (slot-flourish-003.png), with frames.txt
# beside them (untracked).
#   pwsh -File Tools/ArtReview/Run-GameFeelProof.ps1
# -Only captures some parts: hero, enemy, horde, warden, regent (comma separated).
#   pwsh -File Tools/ArtReview/Run-GameFeelProof.ps1 -Only warden,regent
param([string]$Only = '', [string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofTarget = Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/GameFeelCapture.cs'
$proofMeta = "$proofTarget.meta"
$proofResults = Join-Path $proofRoot 'TestResults/game-feel.xml'
$proofLog = Join-Path $proofRoot 'TestResults/game-feel.log'
$proofFrames = Join-Path $proofRoot 'ArtDirection/2026-11-22/game-feel-01'
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path (Join-Path $proofRoot 'Temp/UnityLockfile'))) { throw 'Unity is in use; coordinate before capturing.' }
if ((Test-Path $proofTarget) -or (Test-Path $proofMeta)) { throw 'Temporary proof files already exist; inspect before retrying.' }
Push-Location $proofRoot
try {
    New-Item -ItemType Directory -Force (Join-Path $proofRoot 'TestResults') | Out-Null
    Copy-Item -LiteralPath "$PSScriptRoot/GameFeelCapture.cs" -Destination $proofTarget
    Set-Content -LiteralPath $proofMeta -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
    dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed.' }
    dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Compile check failed.' }
    if (Test-Path $proofResults) { Remove-Item -LiteralPath $proofResults }
    # A BMP here is only ever left by a run that stopped before converting; this run's frames are the ones to keep.
    if (Test-Path $proofFrames) { Get-ChildItem -LiteralPath $proofFrames -Filter '*.bmp' -File | Remove-Item }
    $env:GAME_FEEL_ONLY = $Only
    $proofProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','Cryptforge.Tests.GameFeelCapture','-testResults',"`"$proofResults`"",'-logFile',"`"$proofLog`"") -PassThru -WindowStyle Hidden
    $proofProcess.WaitForExit()
    if (!(Test-Path $proofResults)) { throw "Unity capture failed; inspect $proofLog." }
    $proofRun = ([xml](Get-Content -LiteralPath $proofResults)).'test-run'
    if ($proofProcess.ExitCode -ne 0 -or $proofRun.result -ne 'Passed') {
        $proofFailure = ($proofRun.GetElementsByTagName('test-case') | Where-Object { $_.result -ne 'Passed' } | Select-Object -First 1).failure.message.InnerText
        throw "Capture failed ($($proofRun.result), exit $($proofProcess.ExitCode)): $proofFailure Frames taken before it stay as BMP in $proofFrames; inspect $proofLog."
    }
    # The lossless format bridge of Convert-CharacterProof.ps1 -Mode Renders: each rendered BMP saved beside itself as a
    # PNG, nothing resized, composited or graded, then removed.
    Add-Type -AssemblyName System.Drawing
    foreach ($proofFile in Get-ChildItem -LiteralPath $proofFrames -Filter '*.bmp' -File) {
        $proofBitmap = [System.Drawing.Bitmap]::new($proofFile.FullName)
        try { $proofBitmap.Save([System.IO.Path]::ChangeExtension($proofFile.FullName, 'png'), [System.Drawing.Imaging.ImageFormat]::Png) }
        finally { $proofBitmap.Dispose() }
        Remove-Item -LiteralPath $proofFile.FullName
    }
    $proofCount = (Get-ChildItem -LiteralPath $proofFrames -Filter '*.png' -File).Count
    Write-Output "Game feel capture: $($proofRun.passed) passed; $proofCount frames in $proofFrames (frames.txt lists them); no APK built or installed."
} finally {
    # Only these exact temporary files belong to this runner.
    if (Test-Path $proofTarget) { Remove-Item -LiteralPath $proofTarget }
    if (Test-Path $proofMeta) { Remove-Item -LiteralPath $proofMeta }
    Remove-Item Env:GAME_FEEL_ONLY -ErrorAction SilentlyContinue
    Pop-Location
}
