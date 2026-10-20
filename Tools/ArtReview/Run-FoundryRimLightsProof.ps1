#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$rimLightsRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$rimLightsTarget=Join-Path $rimLightsRoot 'Assets/_Project/Tests/PlayMode/FoundryRimLightsCapture.cs'
$rimLightsResults=Join-Path $rimLightsRoot 'TestResults/foundry-rim-lights.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$rimLightsRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $rimLightsTarget) -or (Test-Path "$rimLightsTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $rimLightsHead=(git -C $rimLightsRoot rev-parse HEAD).Trim()
    if($rimLightsHead -ne '2c3c4082181d5ac301698e0c8501bc8c98c99e80'){throw 'Before capture requires the recorded baseline HEAD.'}
    $rimLightsDirty=git -C $rimLightsRoot status --porcelain --untracked-files=all -- Assets
    if($rimLightsDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$rimLightsRoot/TestResults/foundry-rim-lights-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryRimLightsCapture.cs" -Destination $rimLightsTarget
    Set-Content -LiteralPath "$rimLightsTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $rimLightsResults){Remove-Item -LiteralPath $rimLightsResults}
    $rimLightsProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$rimLightsRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryRimLightsCapture','-testResults',"`"$rimLightsResults`"",'-logFile',"`"$rimLightsRoot/TestResults/foundry-rim-lights.log`"") -PassThru -WindowStyle Hidden
    $rimLightsProcess.WaitForExit()
    if($rimLightsProcess.ExitCode -ne 0 -or !(Test-Path $rimLightsResults)){throw 'Layout capture failed; see TestResults/foundry-rim-lights.log.'}
    $rimLightsRun=([xml](Get-Content -LiteralPath $rimLightsResults)).'test-run'
    if($rimLightsRun.result -ne 'Passed'){throw "Layout capture failed: $($rimLightsRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($rimLightsFile in Get-ChildItem "$rimLightsRoot/ArtDirection/2026-10-20/foundry-rim-lights-01/$Phase" -Filter '*.bmp'){
        $rimLightsBitmap=[System.Drawing.Bitmap]::new($rimLightsFile.FullName)
        try{$rimLightsBitmap.Save([IO.Path]::ChangeExtension($rimLightsFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$rimLightsBitmap.Dispose()}
        Remove-Item -LiteralPath $rimLightsFile.FullName
    }
    Write-Output "Foundry rim lights actual Unity capture: $($rimLightsRun.passed) passed."
} finally {
    if(Test-Path $rimLightsTarget){Remove-Item -LiteralPath $rimLightsTarget}
    if(Test-Path "$rimLightsTarget.meta"){Remove-Item -LiteralPath "$rimLightsTarget.meta"}
}
