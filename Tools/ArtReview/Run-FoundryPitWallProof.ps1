#requires -Version 7
param([ValidateSet('after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$pitWallRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$pitWallTarget=Join-Path $pitWallRoot 'Assets/_Project/Tests/PlayMode/FoundryPitWallCapture.cs'
$pitWallResults=Join-Path $pitWallRoot 'TestResults/foundry-pit-wall.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$pitWallRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $pitWallTarget) -or (Test-Path "$pitWallTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$pitWallRoot/TestResults/foundry-pit-wall-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryPitWallCapture.cs" -Destination $pitWallTarget
    Set-Content -LiteralPath "$pitWallTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $pitWallResults){Remove-Item -LiteralPath $pitWallResults}
    $pitWallProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$pitWallRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryPitWallCapture','-testResults',"`"$pitWallResults`"",'-logFile',"`"$pitWallRoot/TestResults/foundry-pit-wall.log`"") -PassThru -WindowStyle Hidden
    $pitWallProcess.WaitForExit()
    if($pitWallProcess.ExitCode -ne 0 -or !(Test-Path $pitWallResults)){throw 'Layout capture failed; see TestResults/foundry-pit-wall.log.'}
    $pitWallRun=([xml](Get-Content -LiteralPath $pitWallResults)).'test-run'
    if($pitWallRun.result -ne 'Passed'){throw "Layout capture failed: $($pitWallRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($pitWallFile in Get-ChildItem "$pitWallRoot/ArtDirection/2026-10-11/foundry-pit-wall-01/$Phase" -Filter '*.bmp'){
        $pitWallBitmap=[System.Drawing.Bitmap]::new($pitWallFile.FullName)
        try{$pitWallBitmap.Save([IO.Path]::ChangeExtension($pitWallFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$pitWallBitmap.Dispose()}
        Remove-Item -LiteralPath $pitWallFile.FullName
    }
    Write-Output "Layout actual Unity capture: $($pitWallRun.passed) passed."
} finally {
    if(Test-Path $pitWallTarget){Remove-Item -LiteralPath $pitWallTarget}
    if(Test-Path "$pitWallTarget.meta"){Remove-Item -LiteralPath "$pitWallTarget.meta"}
}
