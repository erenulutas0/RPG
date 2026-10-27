#requires -Version 7
# Records the hero's runtime motion for review (HeroMotionCapture.cs): installs the fixture into the PlayMode tests for
# one graphics-enabled Unity run, removes it again, then builds the review GIFs (build_motion_gifs.py).
#   pwsh -File Tools/ArtReview/Run-HeroMotionCapture.ps1 [-Only vanguard-puppet,arcanist] [-Out <folder>]
param([string]$Only = '', [string]$Out = '', [string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path.Replace('\','/')
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$root/Temp/UnityLockfile")) { throw 'Close the Unity Editor before capture.' }
$target = "$root/Assets/_Project/Tests/PlayMode/HeroMotionCapture.cs"
if ((Test-Path $target) -or (Test-Path "$target.meta")) { throw "Temporary fixture already installed: $target" }
Copy-Item -LiteralPath "$PSScriptRoot/HeroMotionCapture.cs" -Destination $target
Set-Content -LiteralPath "$target.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
$result = "$root/TestResults/hero-motion.xml"
try {
    if (Test-Path $result) { Remove-Item -LiteralPath $result }
    $env:HERO_MOTION_ONLY = $Only
    $process = Start-Process -FilePath $UnityEditor -PassThru -WindowStyle Hidden -ArgumentList @(
        '-batchmode', '-projectPath', "`"$root`"", '-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'Cryptforge.Tests.HeroMotionCapture',
        '-testResults', "`"$result`"", '-logFile', "`"$root/TestResults/hero-motion.log`"")
    $process.WaitForExit()
    $run = ([xml](Get-Content -LiteralPath $result)).'test-run'
    Write-Output "Capture: $($run.result) ($($run.passed) passed, $($run.failed) failed)"
    if ($run.result -ne 'Passed') { throw 'Hero capture failed; inspect TestResults/hero-motion.log.' }
} finally {
    Remove-Item -LiteralPath $target, "$target.meta" -ErrorAction SilentlyContinue
    Remove-Item Env:HERO_MOTION_ONLY -ErrorAction SilentlyContinue
}
$gifArgs = @("$PSScriptRoot/build_motion_gifs.py", "$root/TestResults/hero-motion")
if ($Out) { $gifArgs += $Out }
python @gifArgs
if ($LASTEXITCODE -ne 0) { throw 'GIF build failed.' }
