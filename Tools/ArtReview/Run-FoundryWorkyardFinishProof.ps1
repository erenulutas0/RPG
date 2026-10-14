#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$workyardFinishRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workyardFinishTarget=Join-Path $workyardFinishRoot 'Assets/_Project/Tests/PlayMode/FoundryWorkyardFinishCapture.cs'
$workyardFinishResults=Join-Path $workyardFinishRoot 'TestResults/foundry-workyard-finish.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$workyardFinishRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $workyardFinishTarget) -or (Test-Path "$workyardFinishTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $workyardFinishHead=(git -C $workyardFinishRoot rev-parse HEAD).Trim()
    if($workyardFinishHead -ne 'd4424447705aa85bf078ed5fb46ab11c92555b91'){throw 'Before capture requires the recorded baseline HEAD.'}
    $workyardFinishDirty=git -C $workyardFinishRoot status --porcelain --untracked-files=all -- Assets
    if($workyardFinishDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$workyardFinishRoot/TestResults/foundry-workyard-finish-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryWorkyardFinishCapture.cs" -Destination $workyardFinishTarget
    Set-Content -LiteralPath "$workyardFinishTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $workyardFinishResults){Remove-Item -LiteralPath $workyardFinishResults}
    $workyardFinishProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$workyardFinishRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryWorkyardFinishCapture','-testResults',"`"$workyardFinishResults`"",'-logFile',"`"$workyardFinishRoot/TestResults/foundry-workyard-finish.log`"") -PassThru -WindowStyle Hidden
    $workyardFinishProcess.WaitForExit()
    if($workyardFinishProcess.ExitCode -ne 0 -or !(Test-Path $workyardFinishResults)){throw 'Layout capture failed; see TestResults/foundry-workyard-finish.log.'}
    $workyardFinishRun=([xml](Get-Content -LiteralPath $workyardFinishResults)).'test-run'
    if($workyardFinishRun.result -ne 'Passed'){throw "Layout capture failed: $($workyardFinishRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($workyardFinishFile in Get-ChildItem "$workyardFinishRoot/ArtDirection/2026-10-14/foundry-workyard-finish-01/$Phase" -Filter '*.bmp'){
        $workyardFinishBitmap=[System.Drawing.Bitmap]::new($workyardFinishFile.FullName)
        try{$workyardFinishBitmap.Save([IO.Path]::ChangeExtension($workyardFinishFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$workyardFinishBitmap.Dispose()}
        Remove-Item -LiteralPath $workyardFinishFile.FullName
    }
    Write-Output "Workyard finish actual Unity capture: $($workyardFinishRun.passed) passed."
} finally {
    if(Test-Path $workyardFinishTarget){Remove-Item -LiteralPath $workyardFinishTarget}
    if(Test-Path "$workyardFinishTarget.meta"){Remove-Item -LiteralPath "$workyardFinishTarget.meta"}
}
