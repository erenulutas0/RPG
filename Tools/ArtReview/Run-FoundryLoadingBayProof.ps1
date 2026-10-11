#requires -Version 7
param([ValidateSet('after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$loadingBayRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$loadingBayTarget=Join-Path $loadingBayRoot 'Assets/_Project/Tests/PlayMode/FoundryLoadingBayCapture.cs'
$loadingBayResults=Join-Path $loadingBayRoot 'TestResults/foundry-loading-bay.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$loadingBayRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $loadingBayTarget) -or (Test-Path "$loadingBayTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$loadingBayRoot/TestResults/foundry-loading-bay-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryLoadingBayCapture.cs" -Destination $loadingBayTarget
    Set-Content -LiteralPath "$loadingBayTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $loadingBayResults){Remove-Item -LiteralPath $loadingBayResults}
    $loadingBayProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$loadingBayRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryLoadingBayCapture','-testResults',"`"$loadingBayResults`"",'-logFile',"`"$loadingBayRoot/TestResults/foundry-loading-bay.log`"") -PassThru -WindowStyle Hidden
    $loadingBayProcess.WaitForExit()
    if($loadingBayProcess.ExitCode -ne 0 -or !(Test-Path $loadingBayResults)){throw 'Layout capture failed; see TestResults/foundry-loading-bay.log.'}
    $loadingBayRun=([xml](Get-Content -LiteralPath $loadingBayResults)).'test-run'
    if($loadingBayRun.result -ne 'Passed'){throw "Layout capture failed: $($loadingBayRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($loadingBayFile in Get-ChildItem "$loadingBayRoot/ArtDirection/2026-10-11/foundry-loading-bay-01/$Phase" -Filter '*.bmp'){
        $loadingBayBitmap=[System.Drawing.Bitmap]::new($loadingBayFile.FullName)
        try{$loadingBayBitmap.Save([IO.Path]::ChangeExtension($loadingBayFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$loadingBayBitmap.Dispose()}
        Remove-Item -LiteralPath $loadingBayFile.FullName
    }
    Write-Output "Layout actual Unity capture: $($loadingBayRun.passed) passed."
} finally {
    if(Test-Path $loadingBayTarget){Remove-Item -LiteralPath $loadingBayTarget}
    if(Test-Path "$loadingBayTarget.meta"){Remove-Item -LiteralPath "$loadingBayTarget.meta"}
}
