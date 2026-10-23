#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$railShrinesRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$railShrinesTarget=Join-Path $railShrinesRoot 'Assets/_Project/Tests/PlayMode/FoundryRailShrinesCapture.cs'
$railShrinesResults=Join-Path $railShrinesRoot 'TestResults/foundry-rail-shrines.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$railShrinesRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $railShrinesTarget) -or (Test-Path "$railShrinesTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $railShrinesHead=(git -C $railShrinesRoot rev-parse HEAD).Trim()
    if($railShrinesHead -ne '5ccf3f7d286f647bb50fc2c0e0552f99b7e575b3'){throw 'Before capture requires the recorded baseline HEAD.'}
    $railShrinesDirty=git -C $railShrinesRoot status --porcelain --untracked-files=all -- Assets
    if($railShrinesDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$railShrinesRoot/TestResults/foundry-rail-shrines-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryRailShrinesCapture.cs" -Destination $railShrinesTarget
    Set-Content -LiteralPath "$railShrinesTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $railShrinesResults){Remove-Item -LiteralPath $railShrinesResults}
    $railShrinesProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$railShrinesRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryRailShrinesCapture','-testResults',"`"$railShrinesResults`"",'-logFile',"`"$railShrinesRoot/TestResults/foundry-rail-shrines.log`"") -PassThru -WindowStyle Hidden
    $railShrinesProcess.WaitForExit()
    if($railShrinesProcess.ExitCode -ne 0 -or !(Test-Path $railShrinesResults)){throw 'Layout capture failed; see TestResults/foundry-rail-shrines.log.'}
    $railShrinesRun=([xml](Get-Content -LiteralPath $railShrinesResults)).'test-run'
    if($railShrinesRun.result -ne 'Passed'){throw "Layout capture failed: $($railShrinesRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($railShrinesFile in Get-ChildItem "$railShrinesRoot/ArtDirection/2026-10-23/foundry-rail-shrines-01/$Phase" -Filter '*.bmp'){
        $railShrinesBitmap=[System.Drawing.Bitmap]::new($railShrinesFile.FullName)
        try{$railShrinesBitmap.Save([IO.Path]::ChangeExtension($railShrinesFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$railShrinesBitmap.Dispose()}
        Remove-Item -LiteralPath $railShrinesFile.FullName
    }
    Write-Output "Foundry rail shrines actual Unity capture: $($railShrinesRun.passed) passed."
} finally {
    if(Test-Path $railShrinesTarget){Remove-Item -LiteralPath $railShrinesTarget}
    if(Test-Path "$railShrinesTarget.meta"){Remove-Item -LiteralPath "$railShrinesTarget.meta"}
}
