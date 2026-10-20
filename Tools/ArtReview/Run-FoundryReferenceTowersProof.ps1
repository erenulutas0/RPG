#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$refTowersRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$refTowersTarget=Join-Path $refTowersRoot 'Assets/_Project/Tests/PlayMode/FoundryReferenceTowersCapture.cs'
$refTowersResults=Join-Path $refTowersRoot 'TestResults/foundry-reference-towers.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$refTowersRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $refTowersTarget) -or (Test-Path "$refTowersTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $refTowersHead=(git -C $refTowersRoot rev-parse HEAD).Trim()
    if($refTowersHead -ne 'c021c9a980b6cfcb6674c8558ba7f14a1df0fa70'){throw 'Before capture requires the recorded baseline HEAD.'}
    $refTowersDirty=git -C $refTowersRoot status --porcelain --untracked-files=all -- Assets
    if($refTowersDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$refTowersRoot/TestResults/foundry-reference-towers-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryReferenceTowersCapture.cs" -Destination $refTowersTarget
    Set-Content -LiteralPath "$refTowersTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $refTowersResults){Remove-Item -LiteralPath $refTowersResults}
    $refTowersProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$refTowersRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryReferenceTowersCapture','-testResults',"`"$refTowersResults`"",'-logFile',"`"$refTowersRoot/TestResults/foundry-reference-towers.log`"") -PassThru -WindowStyle Hidden
    $refTowersProcess.WaitForExit()
    if($refTowersProcess.ExitCode -ne 0 -or !(Test-Path $refTowersResults)){throw 'Layout capture failed; see TestResults/foundry-reference-towers.log.'}
    $refTowersRun=([xml](Get-Content -LiteralPath $refTowersResults)).'test-run'
    if($refTowersRun.result -ne 'Passed'){throw "Layout capture failed: $($refTowersRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($refTowersFile in Get-ChildItem "$refTowersRoot/ArtDirection/2026-10-20/foundry-reference-towers-01/$Phase" -Filter '*.bmp'){
        $refTowersBitmap=[System.Drawing.Bitmap]::new($refTowersFile.FullName)
        try{$refTowersBitmap.Save([IO.Path]::ChangeExtension($refTowersFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$refTowersBitmap.Dispose()}
        Remove-Item -LiteralPath $refTowersFile.FullName
    }
    Write-Output "Foundry reference towers actual Unity capture: $($refTowersRun.passed) passed."
} finally {
    if(Test-Path $refTowersTarget){Remove-Item -LiteralPath $refTowersTarget}
    if(Test-Path "$refTowersTarget.meta"){Remove-Item -LiteralPath "$refTowersTarget.meta"}
}
