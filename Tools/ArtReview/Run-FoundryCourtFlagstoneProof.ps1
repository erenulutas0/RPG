#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$courtFlagstoneRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$courtFlagstoneTarget=Join-Path $courtFlagstoneRoot 'Assets/_Project/Tests/PlayMode/FoundryCourtFlagstoneCapture.cs'
$courtFlagstoneResults=Join-Path $courtFlagstoneRoot 'TestResults/foundry-court-flagstone.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$courtFlagstoneRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $courtFlagstoneTarget) -or (Test-Path "$courtFlagstoneTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $courtFlagstoneHead=(git -C $courtFlagstoneRoot rev-parse HEAD).Trim()
    if($courtFlagstoneHead -ne 'aa1f3904d506270118b0be31eda5ead741fa1560'){throw 'Before capture requires the recorded baseline HEAD.'}
    $courtFlagstoneDirty=git -C $courtFlagstoneRoot status --porcelain --untracked-files=all -- Assets
    if($courtFlagstoneDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$courtFlagstoneRoot/TestResults/foundry-court-flagstone-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCourtFlagstoneCapture.cs" -Destination $courtFlagstoneTarget
    Set-Content -LiteralPath "$courtFlagstoneTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $courtFlagstoneResults){Remove-Item -LiteralPath $courtFlagstoneResults}
    $courtFlagstoneProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$courtFlagstoneRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCourtFlagstoneCapture','-testResults',"`"$courtFlagstoneResults`"",'-logFile',"`"$courtFlagstoneRoot/TestResults/foundry-court-flagstone.log`"") -PassThru -WindowStyle Hidden
    $courtFlagstoneProcess.WaitForExit()
    if($courtFlagstoneProcess.ExitCode -ne 0 -or !(Test-Path $courtFlagstoneResults)){throw 'Layout capture failed; see TestResults/foundry-court-flagstone.log.'}
    $courtFlagstoneRun=([xml](Get-Content -LiteralPath $courtFlagstoneResults)).'test-run'
    if($courtFlagstoneRun.result -ne 'Passed'){throw "Layout capture failed: $($courtFlagstoneRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($courtFlagstoneFile in Get-ChildItem "$courtFlagstoneRoot/ArtDirection/2026-10-20/foundry-court-flagstone-01/$Phase" -Filter '*.bmp'){
        $courtFlagstoneBitmap=[System.Drawing.Bitmap]::new($courtFlagstoneFile.FullName)
        try{$courtFlagstoneBitmap.Save([IO.Path]::ChangeExtension($courtFlagstoneFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$courtFlagstoneBitmap.Dispose()}
        Remove-Item -LiteralPath $courtFlagstoneFile.FullName
    }
    Write-Output "Foundry court flagstone actual Unity capture: $($courtFlagstoneRun.passed) passed."
} finally {
    if(Test-Path $courtFlagstoneTarget){Remove-Item -LiteralPath $courtFlagstoneTarget}
    if(Test-Path "$courtFlagstoneTarget.meta"){Remove-Item -LiteralPath "$courtFlagstoneTarget.meta"}
}
