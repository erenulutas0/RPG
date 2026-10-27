#requires -Version 7
# Isolated, graphics-enabled capture of the painted roster in the game (RosterRuntimeCapture.cs); no authored import,
# scene save, APK or device operation. Frames land in TestResults/roster-runtime as BMP and, converted, in -Out as PNG.
#   pwsh -File Tools/ArtReview/Run-RosterRuntimeProof.ps1 [-Out <folder>]
param([string]$Out = '', [string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofTarget = Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/RosterRuntimeCapture.cs'
$proofMeta = "$proofTarget.meta"
$proofResults = Join-Path $proofRoot 'TestResults/roster-runtime.xml'
$frames = Join-Path $proofRoot 'TestResults/roster-runtime'
if (!$Out) { $Out = Join-Path $frames 'png' }
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path (Join-Path $proofRoot 'Temp/UnityLockfile'))) { throw 'Unity is in use; coordinate before capturing.' }
if ((Test-Path $proofTarget) -or (Test-Path $proofMeta)) { throw 'Temporary proof files already exist; inspect before retrying.' }
Push-Location $proofRoot
try {
    Copy-Item -LiteralPath "$PSScriptRoot/RosterRuntimeCapture.cs" -Destination $proofTarget
    Set-Content -LiteralPath $proofMeta -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
    if (Test-Path $proofResults) { Remove-Item -LiteralPath $proofResults }
    if (Test-Path $frames) { Remove-Item -LiteralPath $frames -Recurse }
    $proofProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','Cryptforge.Tests.RosterRuntimeCapture','-testResults',"`"$proofResults`"",'-logFile',"`"$proofRoot/TestResults/roster-runtime.log`"") -PassThru -WindowStyle Hidden
    $proofProcess.WaitForExit()
    if (!(Test-Path $proofResults)) { throw 'Unity capture failed; inspect TestResults/roster-runtime.log.' }
    $proofRun = ([xml](Get-Content -LiteralPath $proofResults)).'test-run'
    if ($proofRun.result -ne 'Passed') { throw "Capture assertions failed: $($proofRun.result)" }
} finally {
    # Only these exact temporary files belong to this runner.
    if (Test-Path $proofTarget) { Remove-Item -LiteralPath $proofTarget }
    if (Test-Path $proofMeta) { Remove-Item -LiteralPath $proofMeta }
    Pop-Location
}
New-Item -ItemType Directory -Force $Out | Out-Null
python -c "import glob,os,sys; from PIL import Image; [Image.open(f).convert('RGB').save(os.path.join(sys.argv[2], os.path.basename(f)[:-4] + '.png')) for f in glob.glob(os.path.join(sys.argv[1], '*.bmp'))]" $frames $Out
if ($LASTEXITCODE -ne 0) { throw 'PNG conversion failed.' }
Write-Output "Roster capture: $($proofRun.passed) passed; frames in $Out; no APK built or installed."
