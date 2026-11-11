#requires -Version 7
# Isolated, graphics-enabled capture of the level-up panel's cards on their tiers' item grounds
# (ChoiceRarityRuntimeCapture.cs): a foundry run on seed 260 drawn at 1080x2340, a frame for each offer that brings a tier
# not yet shown until common, rare, epic and gold have all been on screen, then two staged frames of one real three-card
# offer with its tiers set (common, rare, epic; gold, epic, rare), so all four stand side by side; then the hall, a
# two-card offer holding Tempered Edge or Quickened Grip staged on epic and gold (hall-legacy-*.png). No authored import,
# scene save, APK or device operation. Frames land in ArtDirection/2026-11-12/choice-rarity-01/runtime as PNG, with
# cards.txt beside them.
#   pwsh -File Tools/ArtReview/Run-ChoiceRarityProof.ps1
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofTarget = Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/ChoiceRarityRuntimeCapture.cs'
$proofMeta = "$proofTarget.meta"
$proofResults = Join-Path $proofRoot 'TestResults/choice-rarity-runtime.xml'
$proofFrames = Join-Path $proofRoot 'ArtDirection/2026-11-12/choice-rarity-01/runtime'
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path (Join-Path $proofRoot 'Temp/UnityLockfile'))) { throw 'Unity is in use; coordinate before capturing.' }
if ((Test-Path $proofTarget) -or (Test-Path $proofMeta)) { throw 'Temporary proof files already exist; inspect before retrying.' }
Push-Location $proofRoot
try {
    New-Item -ItemType Directory -Force (Join-Path $proofRoot 'TestResults') | Out-Null
    Copy-Item -LiteralPath "$PSScriptRoot/ChoiceRarityRuntimeCapture.cs" -Destination $proofTarget
    Set-Content -LiteralPath $proofMeta -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
    dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed.' }
    dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Compile check failed.' }
    if (Test-Path $proofResults) { Remove-Item -LiteralPath $proofResults }
    # A BMP here is only ever left by a run that stopped before converting; this run's frames are the ones to keep.
    if (Test-Path $proofFrames) { Get-ChildItem -LiteralPath $proofFrames -Filter '*.bmp' -File | Remove-Item }
    $proofProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','Cryptforge.Tests.ChoiceRarityRuntimeCapture','-testResults',"`"$proofResults`"",'-logFile',"`"$proofRoot/TestResults/choice-rarity-runtime.log`"") -PassThru -WindowStyle Hidden
    $proofProcess.WaitForExit()
    if (!(Test-Path $proofResults)) { throw 'Unity capture failed; inspect TestResults/choice-rarity-runtime.log.' }
    $proofRun = ([xml](Get-Content -LiteralPath $proofResults)).'test-run'
    if ($proofProcess.ExitCode -ne 0 -or $proofRun.result -ne 'Passed') {
        $proofFailure = ($proofRun.GetElementsByTagName('test-case') | Where-Object { $_.result -ne 'Passed' } | Select-Object -First 1).failure.message.InnerText
        throw "Capture failed ($($proofRun.result), exit $($proofProcess.ExitCode)): $proofFailure Frames taken before it stay as BMP in $proofFrames; inspect TestResults/choice-rarity-runtime.log."
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
    Get-ChildItem -LiteralPath $proofFrames -File | ForEach-Object { Write-Output "  $($_.Name)" }
    Write-Output "Choice rarity capture: $($proofRun.passed) passed; frames in $proofFrames; no APK built or installed."
} finally {
    # Only these exact temporary files belong to this runner.
    if (Test-Path $proofTarget) { Remove-Item -LiteralPath $proofTarget }
    if (Test-Path $proofMeta) { Remove-Item -LiteralPath $proofMeta }
    Pop-Location
}
