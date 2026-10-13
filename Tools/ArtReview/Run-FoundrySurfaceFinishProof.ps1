#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$surfaceFinishRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$surfaceFinishTarget=Join-Path $surfaceFinishRoot 'Assets/_Project/Tests/PlayMode/FoundrySurfaceFinishCapture.cs'
$surfaceFinishResults=Join-Path $surfaceFinishRoot 'TestResults/foundry-surface-finish.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$surfaceFinishRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $surfaceFinishTarget) -or (Test-Path "$surfaceFinishTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $surfaceFinishHead=(git -C $surfaceFinishRoot rev-parse HEAD).Trim()
    if($surfaceFinishHead -ne '525d7594e7c25cdfe1da8e2b3162ab587918dbaf'){throw 'Before capture requires the recorded baseline HEAD.'}
    $surfaceFinishDirty=git -C $surfaceFinishRoot status --porcelain --untracked-files=all -- Assets
    if($surfaceFinishDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$surfaceFinishRoot/TestResults/foundry-surface-finish-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundrySurfaceFinishCapture.cs" -Destination $surfaceFinishTarget
    Set-Content -LiteralPath "$surfaceFinishTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $surfaceFinishResults){Remove-Item -LiteralPath $surfaceFinishResults}
    $surfaceFinishProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$surfaceFinishRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundrySurfaceFinishCapture','-testResults',"`"$surfaceFinishResults`"",'-logFile',"`"$surfaceFinishRoot/TestResults/foundry-surface-finish.log`"") -PassThru -WindowStyle Hidden
    $surfaceFinishProcess.WaitForExit()
    if($surfaceFinishProcess.ExitCode -ne 0 -or !(Test-Path $surfaceFinishResults)){throw 'Layout capture failed; see TestResults/foundry-surface-finish.log.'}
    $surfaceFinishRun=([xml](Get-Content -LiteralPath $surfaceFinishResults)).'test-run'
    if($surfaceFinishRun.result -ne 'Passed'){throw "Layout capture failed: $($surfaceFinishRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($surfaceFinishFile in Get-ChildItem "$surfaceFinishRoot/ArtDirection/2026-10-13/foundry-surface-finish-01/$Phase" -Filter '*.bmp'){
        $surfaceFinishBitmap=[System.Drawing.Bitmap]::new($surfaceFinishFile.FullName)
        try{$surfaceFinishBitmap.Save([IO.Path]::ChangeExtension($surfaceFinishFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$surfaceFinishBitmap.Dispose()}
        Remove-Item -LiteralPath $surfaceFinishFile.FullName
    }
    Write-Output "Surface finish actual Unity capture: $($surfaceFinishRun.passed) passed."
} finally {
    if(Test-Path $surfaceFinishTarget){Remove-Item -LiteralPath $surfaceFinishTarget}
    if(Test-Path "$surfaceFinishTarget.meta"){Remove-Item -LiteralPath "$surfaceFinishTarget.meta"}
}
