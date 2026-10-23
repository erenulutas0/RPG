#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$bridgeStoneRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$bridgeStoneTarget=Join-Path $bridgeStoneRoot 'Assets/_Project/Tests/PlayMode/FoundryBridgeStoneCapture.cs'
$bridgeStoneResults=Join-Path $bridgeStoneRoot 'TestResults/foundry-bridge-stone.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$bridgeStoneRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $bridgeStoneTarget) -or (Test-Path "$bridgeStoneTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $bridgeStoneHead=(git -C $bridgeStoneRoot rev-parse HEAD).Trim()
    if($bridgeStoneHead -ne '884776d3920ac84f8af2a7e3ac390021526f860c'){throw 'Before capture requires the recorded baseline HEAD.'}
    $bridgeStoneDirty=git -C $bridgeStoneRoot status --porcelain --untracked-files=all -- Assets
    if($bridgeStoneDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$bridgeStoneRoot/TestResults/foundry-bridge-stone-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryBridgeStoneCapture.cs" -Destination $bridgeStoneTarget
    Set-Content -LiteralPath "$bridgeStoneTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $bridgeStoneResults){Remove-Item -LiteralPath $bridgeStoneResults}
    $bridgeStoneProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$bridgeStoneRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryBridgeStoneCapture','-testResults',"`"$bridgeStoneResults`"",'-logFile',"`"$bridgeStoneRoot/TestResults/foundry-bridge-stone.log`"") -PassThru -WindowStyle Hidden
    $bridgeStoneProcess.WaitForExit()
    if($bridgeStoneProcess.ExitCode -ne 0 -or !(Test-Path $bridgeStoneResults)){throw 'Layout capture failed; see TestResults/foundry-bridge-stone.log.'}
    $bridgeStoneRun=([xml](Get-Content -LiteralPath $bridgeStoneResults)).'test-run'
    if($bridgeStoneRun.result -ne 'Passed'){throw "Layout capture failed: $($bridgeStoneRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($bridgeStoneFile in Get-ChildItem "$bridgeStoneRoot/ArtDirection/2026-10-23/foundry-bridge-stone-01/$Phase" -Filter '*.bmp'){
        $bridgeStoneBitmap=[System.Drawing.Bitmap]::new($bridgeStoneFile.FullName)
        try{$bridgeStoneBitmap.Save([IO.Path]::ChangeExtension($bridgeStoneFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$bridgeStoneBitmap.Dispose()}
        Remove-Item -LiteralPath $bridgeStoneFile.FullName
    }
    Write-Output "Foundry bridge stone actual Unity capture: $($bridgeStoneRun.passed) passed."
} finally {
    if(Test-Path $bridgeStoneTarget){Remove-Item -LiteralPath $bridgeStoneTarget}
    if(Test-Path "$bridgeStoneTarget.meta"){Remove-Item -LiteralPath "$bridgeStoneTarget.meta"}
}
