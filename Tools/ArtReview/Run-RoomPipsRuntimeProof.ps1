#requires -Version 7
# Isolated, graphics-enabled capture of the HUD's room-pip row on the foundry (RoomPipsRuntimeCapture.cs): floor 1's
# start, room 6, the Warden's room and floor 2, each at 1080x2400 and 1080x1920 with a crop of the top band. No authored
# import, scene save, APK or device operation. Frames land in ArtDirection/2026-11-04/room-pips-runtime-01 as PNG, with
# pip-rects.txt beside them.
#   pwsh -File Tools/ArtReview/Run-RoomPipsRuntimeProof.ps1
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofTarget = Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/RoomPipsRuntimeCapture.cs'
$proofMeta = "$proofTarget.meta"
$proofResults = Join-Path $proofRoot 'TestResults/room-pips-runtime.xml'
$proofFrames = Join-Path $proofRoot 'ArtDirection/2026-11-04/room-pips-runtime-01'
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path (Join-Path $proofRoot 'Temp/UnityLockfile'))) { throw 'Unity is in use; coordinate before capturing.' }
if ((Test-Path $proofTarget) -or (Test-Path $proofMeta)) { throw 'Temporary proof files already exist; inspect before retrying.' }
Push-Location $proofRoot
try {
    New-Item -ItemType Directory -Force (Join-Path $proofRoot 'TestResults') | Out-Null
    Copy-Item -LiteralPath "$PSScriptRoot/RoomPipsRuntimeCapture.cs" -Destination $proofTarget
    Set-Content -LiteralPath $proofMeta -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
    dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed.' }
    dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Compile check failed.' }
    if (Test-Path $proofResults) { Remove-Item -LiteralPath $proofResults }
    $proofProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','Cryptforge.Tests.RoomPipsRuntimeCapture','-testResults',"`"$proofResults`"",'-logFile',"`"$proofRoot/TestResults/room-pips-runtime.log`"") -PassThru -WindowStyle Hidden
    $proofProcess.WaitForExit()
    if (!(Test-Path $proofResults)) { throw 'Unity capture failed; inspect TestResults/room-pips-runtime.log.' }
    $proofRun = ([xml](Get-Content -LiteralPath $proofResults)).'test-run'
    if ($proofProcess.ExitCode -ne 0 -or $proofRun.result -ne 'Passed') {
        $proofFailure = ($proofRun.GetElementsByTagName('test-case') | Where-Object { $_.result -ne 'Passed' } | Select-Object -First 1).failure.message.InnerText
        throw "Capture failed ($($proofRun.result), exit $($proofProcess.ExitCode)): $proofFailure Frames taken before it stay as BMP in $proofFrames; inspect TestResults/room-pips-runtime.log."
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
    Write-Output "Room pip capture: $($proofRun.passed) passed; frames in $proofFrames; no APK built or installed."
} finally {
    # Only these exact temporary files belong to this runner.
    if (Test-Path $proofTarget) { Remove-Item -LiteralPath $proofTarget }
    if (Test-Path $proofMeta) { Remove-Item -LiteralPath $proofMeta }
    Pop-Location
}
