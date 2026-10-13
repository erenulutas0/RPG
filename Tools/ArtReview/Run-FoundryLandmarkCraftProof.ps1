#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$landmarkCraftRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$landmarkCraftTarget=Join-Path $landmarkCraftRoot 'Assets/_Project/Tests/PlayMode/FoundryLandmarkCraftCapture.cs'
$landmarkCraftResults=Join-Path $landmarkCraftRoot 'TestResults/foundry-landmark-craft.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$landmarkCraftRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $landmarkCraftTarget) -or (Test-Path "$landmarkCraftTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $landmarkCraftHead=(git -C $landmarkCraftRoot rev-parse HEAD).Trim()
    if($landmarkCraftHead -ne 'f0ce77095ba814938f24f588a8a8173323699d9f'){throw 'Before capture requires the recorded baseline HEAD.'}
    $landmarkCraftDirty=git -C $landmarkCraftRoot status --porcelain --untracked-files=all -- Assets
    if($landmarkCraftDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$landmarkCraftRoot/TestResults/foundry-landmark-craft-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryLandmarkCraftCapture.cs" -Destination $landmarkCraftTarget
    Set-Content -LiteralPath "$landmarkCraftTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $landmarkCraftResults){Remove-Item -LiteralPath $landmarkCraftResults}
    $landmarkCraftProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$landmarkCraftRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryLandmarkCraftCapture','-testResults',"`"$landmarkCraftResults`"",'-logFile',"`"$landmarkCraftRoot/TestResults/foundry-landmark-craft.log`"") -PassThru -WindowStyle Hidden
    $landmarkCraftProcess.WaitForExit()
    if($landmarkCraftProcess.ExitCode -ne 0 -or !(Test-Path $landmarkCraftResults)){throw 'Layout capture failed; see TestResults/foundry-landmark-craft.log.'}
    $landmarkCraftRun=([xml](Get-Content -LiteralPath $landmarkCraftResults)).'test-run'
    if($landmarkCraftRun.result -ne 'Passed'){throw "Layout capture failed: $($landmarkCraftRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($landmarkCraftFile in Get-ChildItem "$landmarkCraftRoot/ArtDirection/2026-10-13/foundry-landmark-craft-01/$Phase" -Filter '*.bmp'){
        $landmarkCraftBitmap=[System.Drawing.Bitmap]::new($landmarkCraftFile.FullName)
        try{$landmarkCraftBitmap.Save([IO.Path]::ChangeExtension($landmarkCraftFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$landmarkCraftBitmap.Dispose()}
        Remove-Item -LiteralPath $landmarkCraftFile.FullName
    }
    Write-Output "Landmark craft actual Unity capture: $($landmarkCraftRun.passed) passed."
} finally {
    if(Test-Path $landmarkCraftTarget){Remove-Item -LiteralPath $landmarkCraftTarget}
    if(Test-Path "$landmarkCraftTarget.meta"){Remove-Item -LiteralPath "$landmarkCraftTarget.meta"}
}
