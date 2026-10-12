#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$ceremonialArchitectureRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$ceremonialArchitectureTarget=Join-Path $ceremonialArchitectureRoot 'Assets/_Project/Tests/PlayMode/FoundryCeremonialArchitectureCapture.cs'
$ceremonialArchitectureResults=Join-Path $ceremonialArchitectureRoot 'TestResults/foundry-ceremonial-architecture.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$ceremonialArchitectureRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $ceremonialArchitectureTarget) -or (Test-Path "$ceremonialArchitectureTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $ceremonialArchitectureHead=(git -C $ceremonialArchitectureRoot rev-parse HEAD).Trim()
    if($ceremonialArchitectureHead -ne '7c0f057eb71ccc54c2141ad1b1c1215a34ade7cc'){throw 'Before capture requires the recorded baseline HEAD.'}
    $ceremonialArchitectureDirty=git -C $ceremonialArchitectureRoot status --porcelain --untracked-files=all -- Assets
    if($ceremonialArchitectureDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$ceremonialArchitectureRoot/TestResults/foundry-ceremonial-architecture-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCeremonialArchitectureCapture.cs" -Destination $ceremonialArchitectureTarget
    Set-Content -LiteralPath "$ceremonialArchitectureTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $ceremonialArchitectureResults){Remove-Item -LiteralPath $ceremonialArchitectureResults}
    $ceremonialArchitectureProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$ceremonialArchitectureRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCeremonialArchitectureCapture','-testResults',"`"$ceremonialArchitectureResults`"",'-logFile',"`"$ceremonialArchitectureRoot/TestResults/foundry-ceremonial-architecture.log`"") -PassThru -WindowStyle Hidden
    $ceremonialArchitectureProcess.WaitForExit()
    if($ceremonialArchitectureProcess.ExitCode -ne 0 -or !(Test-Path $ceremonialArchitectureResults)){throw 'Layout capture failed; see TestResults/foundry-ceremonial-architecture.log.'}
    $ceremonialArchitectureRun=([xml](Get-Content -LiteralPath $ceremonialArchitectureResults)).'test-run'
    if($ceremonialArchitectureRun.result -ne 'Passed'){throw "Layout capture failed: $($ceremonialArchitectureRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($ceremonialArchitectureFile in Get-ChildItem "$ceremonialArchitectureRoot/ArtDirection/2026-10-12/foundry-ceremonial-architecture-01/$Phase" -Filter '*.bmp'){
        $ceremonialArchitectureBitmap=[System.Drawing.Bitmap]::new($ceremonialArchitectureFile.FullName)
        try{$ceremonialArchitectureBitmap.Save([IO.Path]::ChangeExtension($ceremonialArchitectureFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$ceremonialArchitectureBitmap.Dispose()}
        Remove-Item -LiteralPath $ceremonialArchitectureFile.FullName
    }
    Write-Output "Ceremonial architecture actual Unity capture: $($ceremonialArchitectureRun.passed) passed."
} finally {
    if(Test-Path $ceremonialArchitectureTarget){Remove-Item -LiteralPath $ceremonialArchitectureTarget}
    if(Test-Path "$ceremonialArchitectureTarget.meta"){Remove-Item -LiteralPath "$ceremonialArchitectureTarget.meta"}
}
