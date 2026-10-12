#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$courtHierarchyRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$courtHierarchyTarget=Join-Path $courtHierarchyRoot 'Assets/_Project/Tests/PlayMode/FoundryCourtHierarchyCapture.cs'
$courtHierarchyResults=Join-Path $courtHierarchyRoot 'TestResults/foundry-court-hierarchy.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$courtHierarchyRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $courtHierarchyTarget) -or (Test-Path "$courtHierarchyTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $courtHierarchyHead=(git -C $courtHierarchyRoot rev-parse HEAD).Trim()
    if($courtHierarchyHead -ne '2c2fa7c6e7c832febfa3930b8469c6fe69461762'){throw 'Before capture requires the recorded baseline HEAD.'}
    $courtHierarchyDirty=git -C $courtHierarchyRoot status --porcelain --untracked-files=all -- Assets
    if($courtHierarchyDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$courtHierarchyRoot/TestResults/foundry-court-hierarchy-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCourtHierarchyCapture.cs" -Destination $courtHierarchyTarget
    Set-Content -LiteralPath "$courtHierarchyTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $courtHierarchyResults){Remove-Item -LiteralPath $courtHierarchyResults}
    $courtHierarchyProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$courtHierarchyRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCourtHierarchyCapture','-testResults',"`"$courtHierarchyResults`"",'-logFile',"`"$courtHierarchyRoot/TestResults/foundry-court-hierarchy.log`"") -PassThru -WindowStyle Hidden
    $courtHierarchyProcess.WaitForExit()
    if($courtHierarchyProcess.ExitCode -ne 0 -or !(Test-Path $courtHierarchyResults)){throw 'Layout capture failed; see TestResults/foundry-court-hierarchy.log.'}
    $courtHierarchyRun=([xml](Get-Content -LiteralPath $courtHierarchyResults)).'test-run'
    if($courtHierarchyRun.result -ne 'Passed'){throw "Layout capture failed: $($courtHierarchyRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($courtHierarchyFile in Get-ChildItem "$courtHierarchyRoot/ArtDirection/2026-10-12/foundry-court-hierarchy-01/$Phase" -Filter '*.bmp'){
        $courtHierarchyBitmap=[System.Drawing.Bitmap]::new($courtHierarchyFile.FullName)
        try{$courtHierarchyBitmap.Save([IO.Path]::ChangeExtension($courtHierarchyFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$courtHierarchyBitmap.Dispose()}
        Remove-Item -LiteralPath $courtHierarchyFile.FullName
    }
    Write-Output "Court hierarchy actual Unity capture: $($courtHierarchyRun.passed) passed."
} finally {
    if(Test-Path $courtHierarchyTarget){Remove-Item -LiteralPath $courtHierarchyTarget}
    if(Test-Path "$courtHierarchyTarget.meta"){Remove-Item -LiteralPath "$courtHierarchyTarget.meta"}
}
