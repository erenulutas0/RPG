#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$trolleyRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$trolleyTarget=Join-Path $trolleyRoot 'Assets/_Project/Tests/PlayMode/FoundryTrolleyCapture.cs'
$trolleyResults=Join-Path $trolleyRoot 'TestResults/foundry-trolley.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$trolleyRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $trolleyTarget) -or (Test-Path "$trolleyTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $trolleyHead=(git -C $trolleyRoot rev-parse HEAD).Trim()
    if($trolleyHead -ne '4e761e475826eac52fd866608de7eabf846b44b9'){throw 'Before capture requires the recorded baseline HEAD.'}
    $trolleyDirty=git -C $trolleyRoot status --porcelain --untracked-files=all -- Assets
    if($trolleyDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$trolleyRoot/TestResults/foundry-trolley-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryTrolleyCapture.cs" -Destination $trolleyTarget
    Set-Content -LiteralPath "$trolleyTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $trolleyResults){Remove-Item -LiteralPath $trolleyResults}
    $trolleyProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$trolleyRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryTrolleyCapture','-testResults',"`"$trolleyResults`"",'-logFile',"`"$trolleyRoot/TestResults/foundry-trolley.log`"") -PassThru -WindowStyle Hidden
    $trolleyProcess.WaitForExit()
    if($trolleyProcess.ExitCode -ne 0 -or !(Test-Path $trolleyResults)){throw 'Layout capture failed; see TestResults/foundry-trolley.log.'}
    $trolleyRun=([xml](Get-Content -LiteralPath $trolleyResults)).'test-run'
    if($trolleyRun.result -ne 'Passed'){throw "Layout capture failed: $($trolleyRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($trolleyFile in Get-ChildItem "$trolleyRoot/ArtDirection/2026-10-19/foundry-trolley-01/$Phase" -Filter '*.bmp'){
        $trolleyBitmap=[System.Drawing.Bitmap]::new($trolleyFile.FullName)
        try{$trolleyBitmap.Save([IO.Path]::ChangeExtension($trolleyFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$trolleyBitmap.Dispose()}
        Remove-Item -LiteralPath $trolleyFile.FullName
    }
    Write-Output "Foundry trolley actual Unity capture: $($trolleyRun.passed) passed."
} finally {
    if(Test-Path $trolleyTarget){Remove-Item -LiteralPath $trolleyTarget}
    if(Test-Path "$trolleyTarget.meta"){Remove-Item -LiteralPath "$trolleyTarget.meta"}
}
