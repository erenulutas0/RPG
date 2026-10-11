#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$courtPitGateRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$courtPitGateTarget=Join-Path $courtPitGateRoot 'Assets/_Project/Tests/PlayMode/FoundryCourtPitGateCapture.cs'
$courtPitGateResults=Join-Path $courtPitGateRoot 'TestResults/foundry-court-pit-gate.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$courtPitGateRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $courtPitGateTarget) -or (Test-Path "$courtPitGateTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $courtPitGateHead=(git -C $courtPitGateRoot rev-parse HEAD).Trim()
    if($courtPitGateHead -ne '66e68d34409f00441f1048ed2740bb809c4a93e0'){throw 'Before capture requires the recorded baseline HEAD.'}
    $courtPitGateDirty=git -C $courtPitGateRoot status --porcelain --untracked-files=all -- Assets
    if($courtPitGateDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$courtPitGateRoot/TestResults/foundry-court-pit-gate-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCourtPitGateCapture.cs" -Destination $courtPitGateTarget
    Set-Content -LiteralPath "$courtPitGateTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $courtPitGateResults){Remove-Item -LiteralPath $courtPitGateResults}
    $courtPitGateProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$courtPitGateRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCourtPitGateCapture','-testResults',"`"$courtPitGateResults`"",'-logFile',"`"$courtPitGateRoot/TestResults/foundry-court-pit-gate.log`"") -PassThru -WindowStyle Hidden
    $courtPitGateProcess.WaitForExit()
    if($courtPitGateProcess.ExitCode -ne 0 -or !(Test-Path $courtPitGateResults)){throw 'Layout capture failed; see TestResults/foundry-court-pit-gate.log.'}
    $courtPitGateRun=([xml](Get-Content -LiteralPath $courtPitGateResults)).'test-run'
    if($courtPitGateRun.result -ne 'Passed'){throw "Layout capture failed: $($courtPitGateRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($courtPitGateFile in Get-ChildItem "$courtPitGateRoot/ArtDirection/2026-10-11/foundry-court-pit-gate-01/$Phase" -Filter '*.bmp'){
        $courtPitGateBitmap=[System.Drawing.Bitmap]::new($courtPitGateFile.FullName)
        try{$courtPitGateBitmap.Save([IO.Path]::ChangeExtension($courtPitGateFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$courtPitGateBitmap.Dispose()}
        Remove-Item -LiteralPath $courtPitGateFile.FullName
    }
    Write-Output "Court pit gate actual Unity capture: $($courtPitGateRun.passed) passed."
} finally {
    if(Test-Path $courtPitGateTarget){Remove-Item -LiteralPath $courtPitGateTarget}
    if(Test-Path "$courtPitGateTarget.meta"){Remove-Item -LiteralPath "$courtPitGateTarget.meta"}
}
