#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$gateDoorRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateDoorTarget=Join-Path $gateDoorRoot 'Assets/_Project/Tests/PlayMode/FoundryGateDoorCapture.cs'
$gateDoorResults=Join-Path $gateDoorRoot 'TestResults/foundry-gate-door.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gateDoorRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $gateDoorTarget) -or (Test-Path "$gateDoorTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $gateDoorHead=(git -C $gateDoorRoot rev-parse HEAD).Trim()
    if($gateDoorHead -ne '66a99ba8128cf2a5c64766fb35e84d4bff0b7376'){throw 'Before capture requires the recorded baseline HEAD.'}
    $gateDoorDirty=git -C $gateDoorRoot status --porcelain --untracked-files=all -- Assets
    if($gateDoorDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$gateDoorRoot/TestResults/foundry-gate-door-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryGateDoorCapture.cs" -Destination $gateDoorTarget
    Set-Content -LiteralPath "$gateDoorTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $gateDoorResults){Remove-Item -LiteralPath $gateDoorResults}
    $gateDoorProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$gateDoorRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryGateDoorCapture','-testResults',"`"$gateDoorResults`"",'-logFile',"`"$gateDoorRoot/TestResults/foundry-gate-door.log`"") -PassThru -WindowStyle Hidden
    $gateDoorProcess.WaitForExit()
    if($gateDoorProcess.ExitCode -ne 0 -or !(Test-Path $gateDoorResults)){throw 'Layout capture failed; see TestResults/foundry-gate-door.log.'}
    $gateDoorRun=([xml](Get-Content -LiteralPath $gateDoorResults)).'test-run'
    if($gateDoorRun.result -ne 'Passed'){throw "Layout capture failed: $($gateDoorRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($gateDoorFile in Get-ChildItem "$gateDoorRoot/ArtDirection/2026-10-20/foundry-gate-door-01/$Phase" -Filter '*.bmp'){
        $gateDoorBitmap=[System.Drawing.Bitmap]::new($gateDoorFile.FullName)
        try{$gateDoorBitmap.Save([IO.Path]::ChangeExtension($gateDoorFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$gateDoorBitmap.Dispose()}
        Remove-Item -LiteralPath $gateDoorFile.FullName
    }
    Write-Output "Foundry gate door actual Unity capture: $($gateDoorRun.passed) passed."
} finally {
    if(Test-Path $gateDoorTarget){Remove-Item -LiteralPath $gateDoorTarget}
    if(Test-Path "$gateDoorTarget.meta"){Remove-Item -LiteralPath "$gateDoorTarget.meta"}
}
