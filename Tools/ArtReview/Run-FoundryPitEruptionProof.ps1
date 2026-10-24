#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$pitEruptionRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$pitEruptionTarget=Join-Path $pitEruptionRoot 'Assets/_Project/Tests/PlayMode/FoundryPitEruptionCapture.cs'
$pitEruptionResults=Join-Path $pitEruptionRoot 'TestResults/foundry-pit-eruption.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$pitEruptionRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $pitEruptionTarget) -or (Test-Path "$pitEruptionTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$pitEruptionRoot/TestResults/foundry-pit-eruption-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryPitEruptionCapture.cs" -Destination $pitEruptionTarget
    Set-Content -LiteralPath "$pitEruptionTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $pitEruptionResults){Remove-Item -LiteralPath $pitEruptionResults}
    $pitEruptionProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$pitEruptionRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryPitEruptionCapture','-testResults',"`"$pitEruptionResults`"",'-logFile',"`"$pitEruptionRoot/TestResults/foundry-pit-eruption.log`"") -PassThru -WindowStyle Hidden
    $pitEruptionProcess.WaitForExit()
    if($pitEruptionProcess.ExitCode -ne 0 -or !(Test-Path $pitEruptionResults)){throw 'Layout capture failed; see TestResults/foundry-pit-eruption.log.'}
    $pitEruptionRun=([xml](Get-Content -LiteralPath $pitEruptionResults)).'test-run'
    if($pitEruptionRun.result -ne 'Passed'){throw "Layout capture failed: $($pitEruptionRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($pitEruptionFile in Get-ChildItem "$pitEruptionRoot/ArtDirection/2026-10-24/foundry-pit-eruption-01" -Filter '*.bmp'){
        $pitEruptionBitmap=[System.Drawing.Bitmap]::new($pitEruptionFile.FullName)
        try{$pitEruptionBitmap.Save([IO.Path]::ChangeExtension($pitEruptionFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$pitEruptionBitmap.Dispose()}
        Remove-Item -LiteralPath $pitEruptionFile.FullName
    }
    Write-Output "Foundry pit eruption actual Unity capture: $($pitEruptionRun.passed) passed."
} finally {
    if(Test-Path $pitEruptionTarget){Remove-Item -LiteralPath $pitEruptionTarget}
    if(Test-Path "$pitEruptionTarget.meta"){Remove-Item -LiteralPath "$pitEruptionTarget.meta"}
}
