#requires -Version 7
param([ValidateSet('after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$bridgeYardRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$bridgeYardTarget=Join-Path $bridgeYardRoot 'Assets/_Project/Tests/PlayMode/FoundryBridgeYardCapture.cs'
$bridgeYardResults=Join-Path $bridgeYardRoot 'TestResults/foundry-bridge-yard.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$bridgeYardRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $bridgeYardTarget) -or (Test-Path "$bridgeYardTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$bridgeYardRoot/TestResults/foundry-bridge-yard-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryBridgeYardCapture.cs" -Destination $bridgeYardTarget
    Set-Content -LiteralPath "$bridgeYardTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $bridgeYardResults){Remove-Item -LiteralPath $bridgeYardResults}
    $bridgeYardProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$bridgeYardRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryBridgeYardCapture','-testResults',"`"$bridgeYardResults`"",'-logFile',"`"$bridgeYardRoot/TestResults/foundry-bridge-yard.log`"") -PassThru -WindowStyle Hidden
    $bridgeYardProcess.WaitForExit()
    if($bridgeYardProcess.ExitCode -ne 0 -or !(Test-Path $bridgeYardResults)){throw 'Layout capture failed; see TestResults/foundry-bridge-yard.log.'}
    $bridgeYardRun=([xml](Get-Content -LiteralPath $bridgeYardResults)).'test-run'
    if($bridgeYardRun.result -ne 'Passed'){throw "Layout capture failed: $($bridgeYardRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($bridgeYardFile in Get-ChildItem "$bridgeYardRoot/ArtDirection/2026-10-11/foundry-bridge-yard-01/$Phase" -Filter '*.bmp'){
        $bridgeYardBitmap=[System.Drawing.Bitmap]::new($bridgeYardFile.FullName)
        try{$bridgeYardBitmap.Save([IO.Path]::ChangeExtension($bridgeYardFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$bridgeYardBitmap.Dispose()}
        Remove-Item -LiteralPath $bridgeYardFile.FullName
    }
    Write-Output "Layout actual Unity capture: $($bridgeYardRun.passed) passed."
} finally {
    if(Test-Path $bridgeYardTarget){Remove-Item -LiteralPath $bridgeYardTarget}
    if(Test-Path "$bridgeYardTarget.meta"){Remove-Item -LiteralPath "$bridgeYardTarget.meta"}
}
