#requires -Version 7
# docs/62 P0a's Outworks probe captured in the actual foundry scene (OutworksCapture.cs, development/outworks.txt on): the
# anvils and their arrows, an anvil charging and captured, the Keeper's lantern and arrow, a pad's arc mid-flight and its
# landing, the Bell's swarm (and in Turkish), the open stair, its hold and the first hall after it. Frames go to
# ArtDirection/2026-11-22/outworks-p0a/ (untracked: review them, do not commit them). Close Unity first.
#
#   pwsh Tools/ArtReview/Run-OutworksProof.ps1
param([string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$outworksRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outworksTarget=Join-Path $outworksRoot 'Assets/_Project/Tests/PlayMode/OutworksCapture.cs'
$outworksResults=Join-Path $outworksRoot 'TestResults/outworks-p0a.xml'
$outworksFrames=Join-Path $outworksRoot 'ArtDirection/2026-11-22/outworks-p0a'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$outworksRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $outworksTarget) -or (Test-Path "$outworksTarget.meta")){throw 'Temporary Outworks capture exists; inspect first.'}
New-Item -ItemType Directory -Force -Path (Join-Path $outworksRoot 'TestResults') | Out-Null
New-Item -ItemType Directory -Force -Path $outworksFrames | Out-Null
try {
    Copy-Item -LiteralPath "$PSScriptRoot/OutworksCapture.cs" -Destination $outworksTarget
    Set-Content -LiteralPath "$outworksTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $outworksResults){Remove-Item -LiteralPath $outworksResults}
    $outworksProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$outworksRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','OutworksCapture','-testResults',"`"$outworksResults`"",'-logFile',"`"$outworksRoot/TestResults/outworks-p0a.log`"") -PassThru -WindowStyle Hidden
    $outworksProcess.WaitForExit()
    if($outworksProcess.ExitCode -ne 0 -or !(Test-Path $outworksResults)){throw 'Outworks capture failed; see TestResults/outworks-p0a.log.'}
    $outworksRun=([xml](Get-Content -LiteralPath $outworksResults)).'test-run'
    if($outworksRun.result -ne 'Passed'){throw "Outworks capture failed: $($outworksRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($outworksFile in Get-ChildItem $outworksFrames -Filter '*.bmp'){
        $outworksBitmap=[System.Drawing.Bitmap]::new($outworksFile.FullName)
        try{$outworksBitmap.Save([IO.Path]::ChangeExtension($outworksFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$outworksBitmap.Dispose()}
        Remove-Item -LiteralPath $outworksFile.FullName
    }
    Write-Output "Outworks P0a capture: $($outworksRun.passed) passed; frames in ArtDirection/2026-11-22/outworks-p0a."
} finally {
    if(Test-Path $outworksTarget){Remove-Item -LiteralPath $outworksTarget}
    if(Test-Path "$outworksTarget.meta"){Remove-Item -LiteralPath "$outworksTarget.meta"}
}
