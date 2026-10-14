#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$innerPiersRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$innerPiersTarget=Join-Path $innerPiersRoot 'Assets/_Project/Tests/PlayMode/FoundryInnerPiersCapture.cs'
$innerPiersResults=Join-Path $innerPiersRoot 'TestResults/foundry-inner-piers.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$innerPiersRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $innerPiersTarget) -or (Test-Path "$innerPiersTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $innerPiersHead=(git -C $innerPiersRoot rev-parse HEAD).Trim()
    if($innerPiersHead -ne 'e6f681792a53d9c3e3204291e67d51738f2c1abd'){throw 'Before capture requires the recorded baseline HEAD.'}
    $innerPiersDirty=git -C $innerPiersRoot status --porcelain --untracked-files=all -- Assets
    if($innerPiersDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$innerPiersRoot/TestResults/foundry-inner-piers-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryInnerPiersCapture.cs" -Destination $innerPiersTarget
    Set-Content -LiteralPath "$innerPiersTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $innerPiersResults){Remove-Item -LiteralPath $innerPiersResults}
    $innerPiersProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$innerPiersRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryInnerPiersCapture','-testResults',"`"$innerPiersResults`"",'-logFile',"`"$innerPiersRoot/TestResults/foundry-inner-piers.log`"") -PassThru -WindowStyle Hidden
    $innerPiersProcess.WaitForExit()
    if($innerPiersProcess.ExitCode -ne 0 -or !(Test-Path $innerPiersResults)){throw 'Layout capture failed; see TestResults/foundry-inner-piers.log.'}
    $innerPiersRun=([xml](Get-Content -LiteralPath $innerPiersResults)).'test-run'
    if($innerPiersRun.result -ne 'Passed'){throw "Layout capture failed: $($innerPiersRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($innerPiersFile in Get-ChildItem "$innerPiersRoot/ArtDirection/2026-10-14/foundry-inner-piers-01/$Phase" -Filter '*.bmp'){
        $innerPiersBitmap=[System.Drawing.Bitmap]::new($innerPiersFile.FullName)
        try{$innerPiersBitmap.Save([IO.Path]::ChangeExtension($innerPiersFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$innerPiersBitmap.Dispose()}
        Remove-Item -LiteralPath $innerPiersFile.FullName
    }
    Write-Output "Foundry inner piers actual Unity capture: $($innerPiersRun.passed) passed."
} finally {
    if(Test-Path $innerPiersTarget){Remove-Item -LiteralPath $innerPiersTarget}
    if(Test-Path "$innerPiersTarget.meta"){Remove-Item -LiteralPath "$innerPiersTarget.meta"}
}
