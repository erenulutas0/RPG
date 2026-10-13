#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$pitCraftRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$pitCraftTarget=Join-Path $pitCraftRoot 'Assets/_Project/Tests/PlayMode/FoundryPitCraftCapture.cs'
$pitCraftResults=Join-Path $pitCraftRoot 'TestResults/foundry-pit-craft.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$pitCraftRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $pitCraftTarget) -or (Test-Path "$pitCraftTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $pitCraftHead=(git -C $pitCraftRoot rev-parse HEAD).Trim()
    if($pitCraftHead -ne 'e4f0611af0181de94ba83659a3a43b25e4937a37'){throw 'Before capture requires the recorded baseline HEAD.'}
    $pitCraftDirty=git -C $pitCraftRoot status --porcelain --untracked-files=all -- Assets
    if($pitCraftDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$pitCraftRoot/TestResults/foundry-pit-craft-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryPitCraftCapture.cs" -Destination $pitCraftTarget
    Set-Content -LiteralPath "$pitCraftTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $pitCraftResults){Remove-Item -LiteralPath $pitCraftResults}
    $pitCraftProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$pitCraftRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryPitCraftCapture','-testResults',"`"$pitCraftResults`"",'-logFile',"`"$pitCraftRoot/TestResults/foundry-pit-craft.log`"") -PassThru -WindowStyle Hidden
    $pitCraftProcess.WaitForExit()
    if($pitCraftProcess.ExitCode -ne 0 -or !(Test-Path $pitCraftResults)){throw 'Layout capture failed; see TestResults/foundry-pit-craft.log.'}
    $pitCraftRun=([xml](Get-Content -LiteralPath $pitCraftResults)).'test-run'
    if($pitCraftRun.result -ne 'Passed'){throw "Layout capture failed: $($pitCraftRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($pitCraftFile in Get-ChildItem "$pitCraftRoot/ArtDirection/2026-10-13/foundry-pit-craft-01/$Phase" -Filter '*.bmp'){
        $pitCraftBitmap=[System.Drawing.Bitmap]::new($pitCraftFile.FullName)
        try{$pitCraftBitmap.Save([IO.Path]::ChangeExtension($pitCraftFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$pitCraftBitmap.Dispose()}
        Remove-Item -LiteralPath $pitCraftFile.FullName
    }
    Write-Output "Pit craft actual Unity capture: $($pitCraftRun.passed) passed."
} finally {
    if(Test-Path $pitCraftTarget){Remove-Item -LiteralPath $pitCraftTarget}
    if(Test-Path "$pitCraftTarget.meta"){Remove-Item -LiteralPath "$pitCraftTarget.meta"}
}
