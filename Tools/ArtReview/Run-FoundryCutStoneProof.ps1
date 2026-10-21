#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$cutStoneRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$cutStoneTarget=Join-Path $cutStoneRoot 'Assets/_Project/Tests/PlayMode/FoundryCutStoneCapture.cs'
$cutStoneResults=Join-Path $cutStoneRoot 'TestResults/foundry-cut-stone.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$cutStoneRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $cutStoneTarget) -or (Test-Path "$cutStoneTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $cutStoneHead=(git -C $cutStoneRoot rev-parse HEAD).Trim()
    if($cutStoneHead -ne 'f053cddc3490238a8226cfc57298964d1cdcfe64'){throw 'Before capture requires the recorded baseline HEAD.'}
    $cutStoneDirty=git -C $cutStoneRoot status --porcelain --untracked-files=all -- Assets
    if($cutStoneDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$cutStoneRoot/TestResults/foundry-cut-stone-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCutStoneCapture.cs" -Destination $cutStoneTarget
    Set-Content -LiteralPath "$cutStoneTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $cutStoneResults){Remove-Item -LiteralPath $cutStoneResults}
    $cutStoneProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$cutStoneRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCutStoneCapture','-testResults',"`"$cutStoneResults`"",'-logFile',"`"$cutStoneRoot/TestResults/foundry-cut-stone.log`"") -PassThru -WindowStyle Hidden
    $cutStoneProcess.WaitForExit()
    if($cutStoneProcess.ExitCode -ne 0 -or !(Test-Path $cutStoneResults)){throw 'Layout capture failed; see TestResults/foundry-cut-stone.log.'}
    $cutStoneRun=([xml](Get-Content -LiteralPath $cutStoneResults)).'test-run'
    if($cutStoneRun.result -ne 'Passed'){throw "Layout capture failed: $($cutStoneRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($cutStoneFile in Get-ChildItem "$cutStoneRoot/ArtDirection/2026-10-21/foundry-cut-stone-01/$Phase" -Filter '*.bmp'){
        $cutStoneBitmap=[System.Drawing.Bitmap]::new($cutStoneFile.FullName)
        try{$cutStoneBitmap.Save([IO.Path]::ChangeExtension($cutStoneFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$cutStoneBitmap.Dispose()}
        Remove-Item -LiteralPath $cutStoneFile.FullName
    }
    Write-Output "Foundry cut stone actual Unity capture: $($cutStoneRun.passed) passed."
} finally {
    if(Test-Path $cutStoneTarget){Remove-Item -LiteralPath $cutStoneTarget}
    if(Test-Path "$cutStoneTarget.meta"){Remove-Item -LiteralPath "$cutStoneTarget.meta"}
}
