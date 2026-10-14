#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$courtCohesionRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$courtCohesionTarget=Join-Path $courtCohesionRoot 'Assets/_Project/Tests/PlayMode/FoundryCourtCohesionCapture.cs'
$courtCohesionResults=Join-Path $courtCohesionRoot 'TestResults/foundry-court-cohesion.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$courtCohesionRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $courtCohesionTarget) -or (Test-Path "$courtCohesionTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $courtCohesionHead=(git -C $courtCohesionRoot rev-parse HEAD).Trim()
    if($courtCohesionHead -ne '722b3b23a18a3df2af89751da70ae81193ece511'){throw 'Before capture requires the recorded baseline HEAD.'}
    $courtCohesionDirty=git -C $courtCohesionRoot status --porcelain --untracked-files=all -- Assets
    if($courtCohesionDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$courtCohesionRoot/TestResults/foundry-court-cohesion-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCourtCohesionCapture.cs" -Destination $courtCohesionTarget
    Set-Content -LiteralPath "$courtCohesionTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $courtCohesionResults){Remove-Item -LiteralPath $courtCohesionResults}
    $courtCohesionProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$courtCohesionRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCourtCohesionCapture','-testResults',"`"$courtCohesionResults`"",'-logFile',"`"$courtCohesionRoot/TestResults/foundry-court-cohesion.log`"") -PassThru -WindowStyle Hidden
    $courtCohesionProcess.WaitForExit()
    if($courtCohesionProcess.ExitCode -ne 0 -or !(Test-Path $courtCohesionResults)){throw 'Layout capture failed; see TestResults/foundry-court-cohesion.log.'}
    $courtCohesionRun=([xml](Get-Content -LiteralPath $courtCohesionResults)).'test-run'
    if($courtCohesionRun.result -ne 'Passed'){throw "Layout capture failed: $($courtCohesionRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($courtCohesionFile in Get-ChildItem "$courtCohesionRoot/ArtDirection/2026-10-14/foundry-court-cohesion-01/$Phase" -Filter '*.bmp'){
        $courtCohesionBitmap=[System.Drawing.Bitmap]::new($courtCohesionFile.FullName)
        try{$courtCohesionBitmap.Save([IO.Path]::ChangeExtension($courtCohesionFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$courtCohesionBitmap.Dispose()}
        Remove-Item -LiteralPath $courtCohesionFile.FullName
    }
    Write-Output "Court cohesion actual Unity capture: $($courtCohesionRun.passed) passed."
} finally {
    if(Test-Path $courtCohesionTarget){Remove-Item -LiteralPath $courtCohesionTarget}
    if(Test-Path "$courtCohesionTarget.meta"){Remove-Item -LiteralPath "$courtCohesionTarget.meta"}
}
