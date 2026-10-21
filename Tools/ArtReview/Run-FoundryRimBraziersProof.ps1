#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$rimBraziersRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$rimBraziersTarget=Join-Path $rimBraziersRoot 'Assets/_Project/Tests/PlayMode/FoundryRimBraziersCapture.cs'
$rimBraziersResults=Join-Path $rimBraziersRoot 'TestResults/foundry-rim-braziers.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$rimBraziersRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $rimBraziersTarget) -or (Test-Path "$rimBraziersTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $rimBraziersHead=(git -C $rimBraziersRoot rev-parse HEAD).Trim()
    if($rimBraziersHead -ne '15a5574c4479d3c7d1b60b2befa3772f8d9b6c9f'){throw 'Before capture requires the recorded baseline HEAD.'}
    $rimBraziersDirty=git -C $rimBraziersRoot status --porcelain --untracked-files=all -- Assets
    if($rimBraziersDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$rimBraziersRoot/TestResults/foundry-rim-braziers-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryRimBraziersCapture.cs" -Destination $rimBraziersTarget
    Set-Content -LiteralPath "$rimBraziersTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $rimBraziersResults){Remove-Item -LiteralPath $rimBraziersResults}
    $rimBraziersProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$rimBraziersRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryRimBraziersCapture','-testResults',"`"$rimBraziersResults`"",'-logFile',"`"$rimBraziersRoot/TestResults/foundry-rim-braziers.log`"") -PassThru -WindowStyle Hidden
    $rimBraziersProcess.WaitForExit()
    if($rimBraziersProcess.ExitCode -ne 0 -or !(Test-Path $rimBraziersResults)){throw 'Layout capture failed; see TestResults/foundry-rim-braziers.log.'}
    $rimBraziersRun=([xml](Get-Content -LiteralPath $rimBraziersResults)).'test-run'
    if($rimBraziersRun.result -ne 'Passed'){throw "Layout capture failed: $($rimBraziersRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($rimBraziersFile in Get-ChildItem "$rimBraziersRoot/ArtDirection/2026-10-21/foundry-rim-braziers-01/$Phase" -Filter '*.bmp'){
        $rimBraziersBitmap=[System.Drawing.Bitmap]::new($rimBraziersFile.FullName)
        try{$rimBraziersBitmap.Save([IO.Path]::ChangeExtension($rimBraziersFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$rimBraziersBitmap.Dispose()}
        Remove-Item -LiteralPath $rimBraziersFile.FullName
    }
    Write-Output "Foundry rim braziers actual Unity capture: $($rimBraziersRun.passed) passed."
} finally {
    if(Test-Path $rimBraziersTarget){Remove-Item -LiteralPath $rimBraziersTarget}
    if(Test-Path "$rimBraziersTarget.meta"){Remove-Item -LiteralPath "$rimBraziersTarget.meta"}
}
