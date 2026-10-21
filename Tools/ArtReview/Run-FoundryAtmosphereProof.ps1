#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$atmosphereRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$atmosphereTarget=Join-Path $atmosphereRoot 'Assets/_Project/Tests/PlayMode/FoundryAtmosphereCapture.cs'
$atmosphereResults=Join-Path $atmosphereRoot 'TestResults/foundry-atmosphere.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$atmosphereRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $atmosphereTarget) -or (Test-Path "$atmosphereTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $atmosphereHead=(git -C $atmosphereRoot rev-parse HEAD).Trim()
    if($atmosphereHead -ne 'f682cdda0d8698e2e85d70adf87e6aa27c26dce5'){throw 'Before capture requires the recorded baseline HEAD.'}
    $atmosphereDirty=git -C $atmosphereRoot status --porcelain --untracked-files=all -- Assets
    if($atmosphereDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$atmosphereRoot/TestResults/foundry-atmosphere-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryAtmosphereCapture.cs" -Destination $atmosphereTarget
    Set-Content -LiteralPath "$atmosphereTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $atmosphereResults){Remove-Item -LiteralPath $atmosphereResults}
    $atmosphereProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$atmosphereRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryAtmosphereCapture','-testResults',"`"$atmosphereResults`"",'-logFile',"`"$atmosphereRoot/TestResults/foundry-atmosphere.log`"") -PassThru -WindowStyle Hidden
    $atmosphereProcess.WaitForExit()
    if($atmosphereProcess.ExitCode -ne 0 -or !(Test-Path $atmosphereResults)){throw 'Layout capture failed; see TestResults/foundry-atmosphere.log.'}
    $atmosphereRun=([xml](Get-Content -LiteralPath $atmosphereResults)).'test-run'
    if($atmosphereRun.result -ne 'Passed'){throw "Layout capture failed: $($atmosphereRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($atmosphereFile in Get-ChildItem "$atmosphereRoot/ArtDirection/2026-10-21/foundry-atmosphere-01/$Phase" -Filter '*.bmp'){
        $atmosphereBitmap=[System.Drawing.Bitmap]::new($atmosphereFile.FullName)
        try{$atmosphereBitmap.Save([IO.Path]::ChangeExtension($atmosphereFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$atmosphereBitmap.Dispose()}
        Remove-Item -LiteralPath $atmosphereFile.FullName
    }
    Write-Output "Foundry atmosphere actual Unity capture: $($atmosphereRun.passed) passed."
} finally {
    if(Test-Path $atmosphereTarget){Remove-Item -LiteralPath $atmosphereTarget}
    if(Test-Path "$atmosphereTarget.meta"){Remove-Item -LiteralPath "$atmosphereTarget.meta"}
}
