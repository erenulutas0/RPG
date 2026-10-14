#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$sceneCohesionRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sceneCohesionTarget=Join-Path $sceneCohesionRoot 'Assets/_Project/Tests/PlayMode/FoundrySceneCohesionCapture.cs'
$sceneCohesionResults=Join-Path $sceneCohesionRoot 'TestResults/foundry-scene-cohesion.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$sceneCohesionRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $sceneCohesionTarget) -or (Test-Path "$sceneCohesionTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $sceneCohesionHead=(git -C $sceneCohesionRoot rev-parse HEAD).Trim()
    if($sceneCohesionHead -ne '8c09763e06076233d82f931684a43ecaa71e360b'){throw 'Before capture requires the recorded baseline HEAD.'}
    $sceneCohesionDirty=git -C $sceneCohesionRoot status --porcelain --untracked-files=all -- Assets
    if($sceneCohesionDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$sceneCohesionRoot/TestResults/foundry-scene-cohesion-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundrySceneCohesionCapture.cs" -Destination $sceneCohesionTarget
    Set-Content -LiteralPath "$sceneCohesionTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $sceneCohesionResults){Remove-Item -LiteralPath $sceneCohesionResults}
    $sceneCohesionProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$sceneCohesionRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundrySceneCohesionCapture','-testResults',"`"$sceneCohesionResults`"",'-logFile',"`"$sceneCohesionRoot/TestResults/foundry-scene-cohesion.log`"") -PassThru -WindowStyle Hidden
    $sceneCohesionProcess.WaitForExit()
    if($sceneCohesionProcess.ExitCode -ne 0 -or !(Test-Path $sceneCohesionResults)){throw 'Layout capture failed; see TestResults/foundry-scene-cohesion.log.'}
    $sceneCohesionRun=([xml](Get-Content -LiteralPath $sceneCohesionResults)).'test-run'
    if($sceneCohesionRun.result -ne 'Passed'){throw "Layout capture failed: $($sceneCohesionRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($sceneCohesionFile in Get-ChildItem "$sceneCohesionRoot/ArtDirection/2026-10-14/foundry-scene-cohesion-01/$Phase" -Filter '*.bmp'){
        $sceneCohesionBitmap=[System.Drawing.Bitmap]::new($sceneCohesionFile.FullName)
        try{$sceneCohesionBitmap.Save([IO.Path]::ChangeExtension($sceneCohesionFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$sceneCohesionBitmap.Dispose()}
        Remove-Item -LiteralPath $sceneCohesionFile.FullName
    }
    Write-Output "Foundry scene cohesion actual Unity capture: $($sceneCohesionRun.passed) passed."
} finally {
    if(Test-Path $sceneCohesionTarget){Remove-Item -LiteralPath $sceneCohesionTarget}
    if(Test-Path "$sceneCohesionTarget.meta"){Remove-Item -LiteralPath "$sceneCohesionTarget.meta"}
}
