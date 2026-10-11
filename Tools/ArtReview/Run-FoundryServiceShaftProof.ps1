#requires -Version 7
param([ValidateSet('after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$serviceShaftRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$serviceShaftTarget=Join-Path $serviceShaftRoot 'Assets/_Project/Tests/PlayMode/FoundryServiceShaftCapture.cs'
$serviceShaftResults=Join-Path $serviceShaftRoot 'TestResults/foundry-service-shafts.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$serviceShaftRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $serviceShaftTarget) -or (Test-Path "$serviceShaftTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$serviceShaftRoot/TestResults/foundry-service-shafts-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryServiceShaftCapture.cs" -Destination $serviceShaftTarget
    Set-Content -LiteralPath "$serviceShaftTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $serviceShaftResults){Remove-Item -LiteralPath $serviceShaftResults}
    $serviceShaftProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$serviceShaftRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryServiceShaftCapture','-testResults',"`"$serviceShaftResults`"",'-logFile',"`"$serviceShaftRoot/TestResults/foundry-service-shafts.log`"") -PassThru -WindowStyle Hidden
    $serviceShaftProcess.WaitForExit()
    if($serviceShaftProcess.ExitCode -ne 0 -or !(Test-Path $serviceShaftResults)){throw 'Layout capture failed; see TestResults/foundry-service-shafts.log.'}
    $serviceShaftRun=([xml](Get-Content -LiteralPath $serviceShaftResults)).'test-run'
    if($serviceShaftRun.result -ne 'Passed'){throw "Layout capture failed: $($serviceShaftRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($serviceShaftFile in Get-ChildItem "$serviceShaftRoot/ArtDirection/2026-10-11/foundry-service-shafts-01/$Phase" -Filter '*.bmp'){
        $serviceShaftBitmap=[System.Drawing.Bitmap]::new($serviceShaftFile.FullName)
        try{$serviceShaftBitmap.Save([IO.Path]::ChangeExtension($serviceShaftFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$serviceShaftBitmap.Dispose()}
        Remove-Item -LiteralPath $serviceShaftFile.FullName
    }
    Write-Output "Service shaft actual Unity capture: $($serviceShaftRun.passed) passed."
} finally {
    if(Test-Path $serviceShaftTarget){Remove-Item -LiteralPath $serviceShaftTarget}
    if(Test-Path "$serviceShaftTarget.meta"){Remove-Item -LiteralPath "$serviceShaftTarget.meta"}
}
