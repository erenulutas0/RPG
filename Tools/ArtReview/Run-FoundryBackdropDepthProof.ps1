#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$backdropRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$backdropTarget=Join-Path $backdropRoot 'Assets/_Project/Tests/PlayMode/FoundryBackdropDepthCapture.cs'
$backdropResults=Join-Path $backdropRoot 'TestResults/foundry-backdrop-depth.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$backdropRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $backdropTarget) -or (Test-Path "$backdropTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$backdropRoot/TestResults/foundry-backdrop-depth-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryBackdropDepthCapture.cs" -Destination $backdropTarget
    Set-Content -LiteralPath "$backdropTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $backdropResults){Remove-Item -LiteralPath $backdropResults}
    $backdropProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$backdropRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryBackdropDepthCapture','-testResults',"`"$backdropResults`"",'-logFile',"`"$backdropRoot/TestResults/foundry-backdrop-depth.log`"") -PassThru -WindowStyle Hidden
    $backdropProcess.WaitForExit()
    if($backdropProcess.ExitCode -ne 0 -or !(Test-Path $backdropResults)){throw 'Layout capture failed; see TestResults/foundry-backdrop-depth.log.'}
    $backdropRun=([xml](Get-Content -LiteralPath $backdropResults)).'test-run'
    if($backdropRun.result -ne 'Passed'){throw "Layout capture failed: $($backdropRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($backdropFile in Get-ChildItem "$backdropRoot/ArtDirection/2026-10-11/foundry-backdrop-depth-01/$Phase" -Filter '*.bmp'){
        $backdropBitmap=[System.Drawing.Bitmap]::new($backdropFile.FullName)
        try{$backdropBitmap.Save([IO.Path]::ChangeExtension($backdropFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$backdropBitmap.Dispose()}
        Remove-Item -LiteralPath $backdropFile.FullName
    }
    Write-Output "Layout actual Unity capture: $($backdropRun.passed) passed."
} finally {
    if(Test-Path $backdropTarget){Remove-Item -LiteralPath $backdropTarget}
    if(Test-Path "$backdropTarget.meta"){Remove-Item -LiteralPath "$backdropTarget.meta"}
}
