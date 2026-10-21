#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$floorValueRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$floorValueTarget=Join-Path $floorValueRoot 'Assets/_Project/Tests/PlayMode/FoundryFloorValueCapture.cs'
$floorValueResults=Join-Path $floorValueRoot 'TestResults/foundry-floor-value.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$floorValueRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $floorValueTarget) -or (Test-Path "$floorValueTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $floorValueHead=(git -C $floorValueRoot rev-parse HEAD).Trim()
    if($floorValueHead -ne 'dbcbe70c244c33d7d7cf7f90a64173284537cffc'){throw 'Before capture requires the recorded baseline HEAD.'}
    $floorValueDirty=git -C $floorValueRoot status --porcelain --untracked-files=all -- Assets
    if($floorValueDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$floorValueRoot/TestResults/foundry-floor-value-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryFloorValueCapture.cs" -Destination $floorValueTarget
    Set-Content -LiteralPath "$floorValueTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $floorValueResults){Remove-Item -LiteralPath $floorValueResults}
    $floorValueProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$floorValueRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryFloorValueCapture','-testResults',"`"$floorValueResults`"",'-logFile',"`"$floorValueRoot/TestResults/foundry-floor-value.log`"") -PassThru -WindowStyle Hidden
    $floorValueProcess.WaitForExit()
    if($floorValueProcess.ExitCode -ne 0 -or !(Test-Path $floorValueResults)){throw 'Layout capture failed; see TestResults/foundry-floor-value.log.'}
    $floorValueRun=([xml](Get-Content -LiteralPath $floorValueResults)).'test-run'
    if($floorValueRun.result -ne 'Passed'){throw "Layout capture failed: $($floorValueRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($floorValueFile in Get-ChildItem "$floorValueRoot/ArtDirection/2026-10-21/foundry-floor-value-01/$Phase" -Filter '*.bmp'){
        $floorValueBitmap=[System.Drawing.Bitmap]::new($floorValueFile.FullName)
        try{$floorValueBitmap.Save([IO.Path]::ChangeExtension($floorValueFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$floorValueBitmap.Dispose()}
        Remove-Item -LiteralPath $floorValueFile.FullName
    }
    Write-Output "Foundry floor value actual Unity capture: $($floorValueRun.passed) passed."
} finally {
    if(Test-Path $floorValueTarget){Remove-Item -LiteralPath $floorValueTarget}
    if(Test-Path "$floorValueTarget.meta"){Remove-Item -LiteralPath "$floorValueTarget.meta"}
}
