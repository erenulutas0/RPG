#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$entranceCraftRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$entranceCraftTarget=Join-Path $entranceCraftRoot 'Assets/_Project/Tests/PlayMode/FoundryEntranceCraftCapture.cs'
$entranceCraftResults=Join-Path $entranceCraftRoot 'TestResults/foundry-entrance-craft.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$entranceCraftRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $entranceCraftTarget) -or (Test-Path "$entranceCraftTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $entranceCraftHead=(git -C $entranceCraftRoot rev-parse HEAD).Trim()
    if($entranceCraftHead -ne '7518a0f4807a84b0de0a48447993cbbbf60292d4'){throw 'Before capture requires the recorded baseline HEAD.'}
    $entranceCraftDirty=git -C $entranceCraftRoot status --porcelain --untracked-files=all -- Assets
    if($entranceCraftDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$entranceCraftRoot/TestResults/foundry-entrance-craft-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryEntranceCraftCapture.cs" -Destination $entranceCraftTarget
    Set-Content -LiteralPath "$entranceCraftTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $entranceCraftResults){Remove-Item -LiteralPath $entranceCraftResults}
    $entranceCraftProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$entranceCraftRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryEntranceCraftCapture','-testResults',"`"$entranceCraftResults`"",'-logFile',"`"$entranceCraftRoot/TestResults/foundry-entrance-craft.log`"") -PassThru -WindowStyle Hidden
    $entranceCraftProcess.WaitForExit()
    if($entranceCraftProcess.ExitCode -ne 0 -or !(Test-Path $entranceCraftResults)){throw 'Layout capture failed; see TestResults/foundry-entrance-craft.log.'}
    $entranceCraftRun=([xml](Get-Content -LiteralPath $entranceCraftResults)).'test-run'
    if($entranceCraftRun.result -ne 'Passed'){throw "Layout capture failed: $($entranceCraftRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($entranceCraftFile in Get-ChildItem "$entranceCraftRoot/ArtDirection/2026-10-13/foundry-entrance-craft-01/$Phase" -Filter '*.bmp'){
        $entranceCraftBitmap=[System.Drawing.Bitmap]::new($entranceCraftFile.FullName)
        try{$entranceCraftBitmap.Save([IO.Path]::ChangeExtension($entranceCraftFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$entranceCraftBitmap.Dispose()}
        Remove-Item -LiteralPath $entranceCraftFile.FullName
    }
    Write-Output "Entrance craft actual Unity capture: $($entranceCraftRun.passed) passed."
} finally {
    if(Test-Path $entranceCraftTarget){Remove-Item -LiteralPath $entranceCraftTarget}
    if(Test-Path "$entranceCraftTarget.meta"){Remove-Item -LiteralPath "$entranceCraftTarget.meta"}
}
