#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$courtWorkyardRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$courtWorkyardTarget=Join-Path $courtWorkyardRoot 'Assets/_Project/Tests/PlayMode/FoundryCourtWorkyardCapture.cs'
$courtWorkyardResults=Join-Path $courtWorkyardRoot 'TestResults/foundry-court-workyard.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$courtWorkyardRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $courtWorkyardTarget) -or (Test-Path "$courtWorkyardTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $courtWorkyardHead=(git -C $courtWorkyardRoot rev-parse HEAD).Trim()
    if($courtWorkyardHead -ne 'c4f66e6db17a3ad74b240f35c414d2bdbaf3ebab'){throw 'Before capture requires the recorded baseline HEAD.'}
    $courtWorkyardDirty=git -C $courtWorkyardRoot status --porcelain --untracked-files=all -- Assets
    if($courtWorkyardDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$courtWorkyardRoot/TestResults/foundry-court-workyard-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCourtWorkyardCapture.cs" -Destination $courtWorkyardTarget
    Set-Content -LiteralPath "$courtWorkyardTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $courtWorkyardResults){Remove-Item -LiteralPath $courtWorkyardResults}
    $courtWorkyardProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$courtWorkyardRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCourtWorkyardCapture','-testResults',"`"$courtWorkyardResults`"",'-logFile',"`"$courtWorkyardRoot/TestResults/foundry-court-workyard.log`"") -PassThru -WindowStyle Hidden
    $courtWorkyardProcess.WaitForExit()
    if($courtWorkyardProcess.ExitCode -ne 0 -or !(Test-Path $courtWorkyardResults)){throw 'Layout capture failed; see TestResults/foundry-court-workyard.log.'}
    $courtWorkyardRun=([xml](Get-Content -LiteralPath $courtWorkyardResults)).'test-run'
    if($courtWorkyardRun.result -ne 'Passed'){throw "Layout capture failed: $($courtWorkyardRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($courtWorkyardFile in Get-ChildItem "$courtWorkyardRoot/ArtDirection/2026-10-12/foundry-court-workyard-01/$Phase" -Filter '*.bmp'){
        $courtWorkyardBitmap=[System.Drawing.Bitmap]::new($courtWorkyardFile.FullName)
        try{$courtWorkyardBitmap.Save([IO.Path]::ChangeExtension($courtWorkyardFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$courtWorkyardBitmap.Dispose()}
        Remove-Item -LiteralPath $courtWorkyardFile.FullName
    }
    Write-Output "Court and workyard actual Unity capture: $($courtWorkyardRun.passed) passed."
} finally {
    if(Test-Path $courtWorkyardTarget){Remove-Item -LiteralPath $courtWorkyardTarget}
    if(Test-Path "$courtWorkyardTarget.meta"){Remove-Item -LiteralPath "$courtWorkyardTarget.meta"}
}
