#requires -Version 7
# Isolated, graphics-enabled capture of P0's arena screen (docs/55 section 11, ArenaViewCapture.cs): the game opened over
# the map choice on a seeded profile with gear (AnvilFixtures) and an arena save of a fixed seed, on a tr-TR culture, drawn
# at 1080x2340: the build, the training ladder, the open arena's three offers, a strike landing, a skill, a round's end
# and the result. No authored import, scene save, APK or device operation. Frames land in
# ArtDirection/2026-11-19/arena-view-01 as PNG named by their language (en-01-build.png), with frames-en.txt beside them
# (untracked).
#   pwsh -File Tools/ArtReview/Run-ArenaProof.ps1
# -Language tr draws the game in Turkish (docs/60) into the same folder (tr-01-build.png, frames-tr.txt):
#   pwsh -File Tools/ArtReview/Run-ArenaProof.ps1 -Language tr
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe', [ValidateSet('en', 'tr')][string]$Language = 'en')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofTarget = Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/ArenaViewCapture.cs'
$proofMeta = "$proofTarget.meta"
$proofResults = Join-Path $proofRoot "TestResults/arena-view-$Language.xml"
$proofLog = Join-Path $proofRoot "TestResults/arena-view-$Language.log"
$proofFrames = Join-Path $proofRoot 'ArtDirection/2026-11-19/arena-view-01'
# The capture reads its language from the Unity process's environment (LocalisationRuntime.CaptureLanguageVariable).
$proofLanguageBefore = $env:CRYPTFORGE_LANGUAGE
$env:CRYPTFORGE_LANGUAGE = $Language
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path (Join-Path $proofRoot 'Temp/UnityLockfile'))) { throw 'Unity is in use; coordinate before capturing.' }
if ((Test-Path $proofTarget) -or (Test-Path $proofMeta)) { throw 'Temporary proof files already exist; inspect before retrying.' }
Push-Location $proofRoot
try {
    New-Item -ItemType Directory -Force (Join-Path $proofRoot 'TestResults') | Out-Null
    Copy-Item -LiteralPath "$PSScriptRoot/ArenaViewCapture.cs" -Destination $proofTarget
    Set-Content -LiteralPath $proofMeta -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
    dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed.' }
    dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Compile check failed.' }
    if (Test-Path $proofResults) { Remove-Item -LiteralPath $proofResults }
    # A BMP of this language here is only ever left by a run that stopped before converting; this run's frames are the ones to keep.
    if (Test-Path $proofFrames) { Get-ChildItem -LiteralPath $proofFrames -Filter "$Language-*.bmp" -File | Remove-Item }
    $proofProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','Cryptforge.Tests.ArenaViewCapture','-testResults',"`"$proofResults`"",'-logFile',"`"$proofLog`"") -PassThru -WindowStyle Hidden
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
    foreach ($proofFile in Get-ChildItem -LiteralPath $proofFrames -Filter "$Language-*.bmp" -File) {
        $proofBitmap = [System.Drawing.Bitmap]::new($proofFile.FullName)
        try { $proofBitmap.Save([System.IO.Path]::ChangeExtension($proofFile.FullName, 'png'), [System.Drawing.Imaging.ImageFormat]::Png) }
        finally { $proofBitmap.Dispose() }
        Remove-Item -LiteralPath $proofFile.FullName
    }
    Get-ChildItem -LiteralPath $proofFrames -File -Filter "$Language-*" | ForEach-Object { Write-Output "  $($_.Name)" }
    Write-Output "Arena view capture ($Language): $($proofRun.passed) passed; frames in $proofFrames; no APK built or installed."
} finally {
    # Only these exact temporary files belong to this runner.
    if (Test-Path $proofTarget) { Remove-Item -LiteralPath $proofTarget }
    if (Test-Path $proofMeta) { Remove-Item -LiteralPath $proofMeta }
    $env:CRYPTFORGE_LANGUAGE = $proofLanguageBefore
    Pop-Location
}
