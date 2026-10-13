#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$stoneCraftRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$stoneCraftTarget=Join-Path $stoneCraftRoot 'Assets/_Project/Tests/PlayMode/FoundryStoneCraftCapture.cs'
$stoneCraftResults=Join-Path $stoneCraftRoot 'TestResults/foundry-stone-craft.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$stoneCraftRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $stoneCraftTarget) -or (Test-Path "$stoneCraftTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $stoneCraftHead=(git -C $stoneCraftRoot rev-parse HEAD).Trim()
    if($stoneCraftHead -ne 'a8261795d2b4ff947c19e830b46cc92ef8baad23'){throw 'Before capture requires the recorded baseline HEAD.'}
    $stoneCraftDirty=git -C $stoneCraftRoot status --porcelain --untracked-files=all -- Assets
    if($stoneCraftDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$stoneCraftRoot/TestResults/foundry-stone-craft-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryStoneCraftCapture.cs" -Destination $stoneCraftTarget
    Set-Content -LiteralPath "$stoneCraftTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $stoneCraftResults){Remove-Item -LiteralPath $stoneCraftResults}
    $stoneCraftProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$stoneCraftRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryStoneCraftCapture','-testResults',"`"$stoneCraftResults`"",'-logFile',"`"$stoneCraftRoot/TestResults/foundry-stone-craft.log`"") -PassThru -WindowStyle Hidden
    $stoneCraftProcess.WaitForExit()
    if($stoneCraftProcess.ExitCode -ne 0 -or !(Test-Path $stoneCraftResults)){throw 'Layout capture failed; see TestResults/foundry-stone-craft.log.'}
    $stoneCraftRun=([xml](Get-Content -LiteralPath $stoneCraftResults)).'test-run'
    if($stoneCraftRun.result -ne 'Passed'){throw "Layout capture failed: $($stoneCraftRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($stoneCraftFile in Get-ChildItem "$stoneCraftRoot/ArtDirection/2026-10-13/foundry-stone-craft-01/$Phase" -Filter '*.bmp'){
        $stoneCraftBitmap=[System.Drawing.Bitmap]::new($stoneCraftFile.FullName)
        try{$stoneCraftBitmap.Save([IO.Path]::ChangeExtension($stoneCraftFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$stoneCraftBitmap.Dispose()}
        Remove-Item -LiteralPath $stoneCraftFile.FullName
    }
    Write-Output "Stone craft actual Unity capture: $($stoneCraftRun.passed) passed."
} finally {
    if(Test-Path $stoneCraftTarget){Remove-Item -LiteralPath $stoneCraftTarget}
    if(Test-Path "$stoneCraftTarget.meta"){Remove-Item -LiteralPath "$stoneCraftTarget.meta"}
}
