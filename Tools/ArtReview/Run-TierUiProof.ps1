#requires -Version 7
# Isolated, graphics-enabled capture of the difficulty tier row on the hero and map choice (docs/54 phase 6, section 21;
# TierUiCapture.cs), drawn at 1080x2340 on a tr-TR culture: a fresh profile (Forged lit, the tiers above it locked with
# their hints), a geared profile that completed Forged's Descent (Tempered open), Tempered chosen (the deeper maps locked on
# Tempered, the line on the tier with the hero's gear), the Tempered run's HUD floor line and its result title, and every
# tier open with Abyssal chosen. No authored import, scene save, APK or device operation. Frames land in
# ArtDirection/2026-11-17/tier-ui-01 as PNG, with frames.txt beside them (untracked).
#   pwsh -File Tools/ArtReview/Run-TierUiProof.ps1
# -Language tr draws the game in Turkish (docs/60 pass 2) into tier-ui-01-tr beside the English frames:
#   pwsh -File Tools/ArtReview/Run-TierUiProof.ps1 -Language tr
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe', [ValidateSet('en', 'tr')][string]$Language = 'en')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofTarget = Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/TierUiCapture.cs'
$proofMeta = "$proofTarget.meta"
$proofResults = Join-Path $proofRoot 'TestResults/tier-ui.xml'
$proofFrames = Join-Path $proofRoot ('ArtDirection/2026-11-17/tier-ui-01' + $(if ($Language -eq 'tr') { '-tr' } else { '' }))
# The capture reads its language from the Unity process's environment (LocalisationRuntime.CaptureLanguageVariable).
$proofLanguageBefore = $env:CRYPTFORGE_LANGUAGE
$env:CRYPTFORGE_LANGUAGE = $Language
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path (Join-Path $proofRoot 'Temp/UnityLockfile'))) { throw 'Unity is in use; coordinate before capturing.' }
if ((Test-Path $proofTarget) -or (Test-Path $proofMeta)) { throw 'Temporary proof files already exist; inspect before retrying.' }
Push-Location $proofRoot
try {
    New-Item -ItemType Directory -Force (Join-Path $proofRoot 'TestResults') | Out-Null
    Copy-Item -LiteralPath "$PSScriptRoot/TierUiCapture.cs" -Destination $proofTarget
    Set-Content -LiteralPath $proofMeta -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
    dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed.' }
    dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Compile check failed.' }
    if (Test-Path $proofResults) { Remove-Item -LiteralPath $proofResults }
    # A BMP here is only ever left by a run that stopped before converting; this run's frames are the ones to keep.
    if (Test-Path $proofFrames) { Get-ChildItem -LiteralPath $proofFrames -Filter '*.bmp' -File | Remove-Item }
    $proofProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','Cryptforge.Tests.TierUiCapture','-testResults',"`"$proofResults`"",'-logFile',"`"$proofRoot/TestResults/tier-ui.log`"") -PassThru -WindowStyle Hidden
    $proofProcess.WaitForExit()
    if (!(Test-Path $proofResults)) { throw 'Unity capture failed; inspect TestResults/tier-ui.log.' }
    $proofRun = ([xml](Get-Content -LiteralPath $proofResults)).'test-run'
    if ($proofProcess.ExitCode -ne 0 -or $proofRun.result -ne 'Passed') {
        $proofFailure = ($proofRun.GetElementsByTagName('test-case') | Where-Object { $_.result -ne 'Passed' } | Select-Object -First 1).failure.message.InnerText
        throw "Capture failed ($($proofRun.result), exit $($proofProcess.ExitCode)): $proofFailure Frames taken before it stay as BMP in $proofFrames; inspect TestResults/tier-ui.log."
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
    Write-Output "Tier UI capture: $($proofRun.passed) passed; frames in $proofFrames; no APK built or installed."
} finally {
    # Only these exact temporary files belong to this runner.
    if (Test-Path $proofTarget) { Remove-Item -LiteralPath $proofTarget }
    if (Test-Path $proofMeta) { Remove-Item -LiteralPath $proofMeta }
    $env:CRYPTFORGE_LANGUAGE = $proofLanguageBefore
    Pop-Location
}
