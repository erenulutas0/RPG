#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$gateCohesionRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateCohesionTarget=Join-Path $gateCohesionRoot 'Assets/_Project/Tests/PlayMode/FoundryGateCohesionCapture.cs'
$gateCohesionResults=Join-Path $gateCohesionRoot 'TestResults/foundry-gate-cohesion.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gateCohesionRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $gateCohesionTarget) -or (Test-Path "$gateCohesionTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $gateCohesionHead=(git -C $gateCohesionRoot rev-parse HEAD).Trim()
    if($gateCohesionHead -ne '3474e356e0535e145f94e824eadaeda57231875f'){throw 'Before capture requires the recorded baseline HEAD.'}
    $gateCohesionDirty=git -C $gateCohesionRoot status --porcelain --untracked-files=all -- Assets
    if($gateCohesionDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$gateCohesionRoot/TestResults/foundry-gate-cohesion-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryGateCohesionCapture.cs" -Destination $gateCohesionTarget
    Set-Content -LiteralPath "$gateCohesionTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $gateCohesionResults){Remove-Item -LiteralPath $gateCohesionResults}
    $gateCohesionProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$gateCohesionRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryGateCohesionCapture','-testResults',"`"$gateCohesionResults`"",'-logFile',"`"$gateCohesionRoot/TestResults/foundry-gate-cohesion.log`"") -PassThru -WindowStyle Hidden
    $gateCohesionProcess.WaitForExit()
    if($gateCohesionProcess.ExitCode -ne 0 -or !(Test-Path $gateCohesionResults)){throw 'Layout capture failed; see TestResults/foundry-gate-cohesion.log.'}
    $gateCohesionRun=([xml](Get-Content -LiteralPath $gateCohesionResults)).'test-run'
    if($gateCohesionRun.result -ne 'Passed'){throw "Layout capture failed: $($gateCohesionRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($gateCohesionFile in Get-ChildItem "$gateCohesionRoot/ArtDirection/2026-10-14/foundry-gate-cohesion-01/$Phase" -Filter '*.bmp'){
        $gateCohesionBitmap=[System.Drawing.Bitmap]::new($gateCohesionFile.FullName)
        try{$gateCohesionBitmap.Save([IO.Path]::ChangeExtension($gateCohesionFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$gateCohesionBitmap.Dispose()}
        Remove-Item -LiteralPath $gateCohesionFile.FullName
    }
    Write-Output "Gate masonry cohesion actual Unity capture: $($gateCohesionRun.passed) passed."
} finally {
    if(Test-Path $gateCohesionTarget){Remove-Item -LiteralPath $gateCohesionTarget}
    if(Test-Path "$gateCohesionTarget.meta"){Remove-Item -LiteralPath "$gateCohesionTarget.meta"}
}
