#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$yardBraziersRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$yardBraziersTarget=Join-Path $yardBraziersRoot 'Assets/_Project/Tests/PlayMode/FoundryYardBraziersCapture.cs'
$yardBraziersResults=Join-Path $yardBraziersRoot 'TestResults/foundry-yard-braziers.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$yardBraziersRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $yardBraziersTarget) -or (Test-Path "$yardBraziersTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $yardBraziersHead=(git -C $yardBraziersRoot rev-parse HEAD).Trim()
    if($yardBraziersHead -ne '3ea949d432c4aa86e431f2ebbf10e921c3ad38b7'){throw 'Before capture requires the recorded baseline HEAD.'}
    $yardBraziersDirty=git -C $yardBraziersRoot status --porcelain --untracked-files=all -- Assets
    if($yardBraziersDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$yardBraziersRoot/TestResults/foundry-yard-braziers-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryYardBraziersCapture.cs" -Destination $yardBraziersTarget
    Set-Content -LiteralPath "$yardBraziersTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $yardBraziersResults){Remove-Item -LiteralPath $yardBraziersResults}
    $yardBraziersProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$yardBraziersRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryYardBraziersCapture','-testResults',"`"$yardBraziersResults`"",'-logFile',"`"$yardBraziersRoot/TestResults/foundry-yard-braziers.log`"") -PassThru -WindowStyle Hidden
    $yardBraziersProcess.WaitForExit()
    if($yardBraziersProcess.ExitCode -ne 0 -or !(Test-Path $yardBraziersResults)){throw 'Layout capture failed; see TestResults/foundry-yard-braziers.log.'}
    $yardBraziersRun=([xml](Get-Content -LiteralPath $yardBraziersResults)).'test-run'
    if($yardBraziersRun.result -ne 'Passed'){throw "Layout capture failed: $($yardBraziersRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($yardBraziersFile in Get-ChildItem "$yardBraziersRoot/ArtDirection/2026-10-21/foundry-yard-braziers-01/$Phase" -Filter '*.bmp'){
        $yardBraziersBitmap=[System.Drawing.Bitmap]::new($yardBraziersFile.FullName)
        try{$yardBraziersBitmap.Save([IO.Path]::ChangeExtension($yardBraziersFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$yardBraziersBitmap.Dispose()}
        Remove-Item -LiteralPath $yardBraziersFile.FullName
    }
    Write-Output "Foundry yard braziers actual Unity capture: $($yardBraziersRun.passed) passed."
} finally {
    if(Test-Path $yardBraziersTarget){Remove-Item -LiteralPath $yardBraziersTarget}
    if(Test-Path "$yardBraziersTarget.meta"){Remove-Item -LiteralPath "$yardBraziersTarget.meta"}
}
