#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$forecourtLayoutRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$forecourtLayoutTarget=Join-Path $forecourtLayoutRoot 'Assets/_Project/Tests/PlayMode/FoundryForecourtLayoutCapture.cs'
$forecourtLayoutResults=Join-Path $forecourtLayoutRoot 'TestResults/foundry-forecourt-layout.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$forecourtLayoutRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $forecourtLayoutTarget) -or (Test-Path "$forecourtLayoutTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $forecourtLayoutHead=(git -C $forecourtLayoutRoot rev-parse HEAD).Trim()
    if($forecourtLayoutHead -ne '4c86f7ae3dff5ab56e2f852080226bc696f41bd8'){throw 'Before capture requires the recorded baseline HEAD.'}
    $forecourtLayoutDirty=git -C $forecourtLayoutRoot status --porcelain --untracked-files=all -- Assets
    if($forecourtLayoutDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$forecourtLayoutRoot/TestResults/foundry-forecourt-layout-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryForecourtLayoutCapture.cs" -Destination $forecourtLayoutTarget
    Set-Content -LiteralPath "$forecourtLayoutTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $forecourtLayoutResults){Remove-Item -LiteralPath $forecourtLayoutResults}
    $forecourtLayoutProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$forecourtLayoutRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryForecourtLayoutCapture','-testResults',"`"$forecourtLayoutResults`"",'-logFile',"`"$forecourtLayoutRoot/TestResults/foundry-forecourt-layout.log`"") -PassThru -WindowStyle Hidden
    $forecourtLayoutProcess.WaitForExit()
    if($forecourtLayoutProcess.ExitCode -ne 0 -or !(Test-Path $forecourtLayoutResults)){throw 'Layout capture failed; see TestResults/foundry-forecourt-layout.log.'}
    $forecourtLayoutRun=([xml](Get-Content -LiteralPath $forecourtLayoutResults)).'test-run'
    if($forecourtLayoutRun.result -ne 'Passed'){throw "Layout capture failed: $($forecourtLayoutRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($forecourtLayoutFile in Get-ChildItem "$forecourtLayoutRoot/ArtDirection/2026-10-12/foundry-forecourt-layout-01/$Phase" -Filter '*.bmp'){
        $forecourtLayoutBitmap=[System.Drawing.Bitmap]::new($forecourtLayoutFile.FullName)
        try{$forecourtLayoutBitmap.Save([IO.Path]::ChangeExtension($forecourtLayoutFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$forecourtLayoutBitmap.Dispose()}
        Remove-Item -LiteralPath $forecourtLayoutFile.FullName
    }
    Write-Output "Forecourt layout actual Unity capture: $($forecourtLayoutRun.passed) passed."
} finally {
    if(Test-Path $forecourtLayoutTarget){Remove-Item -LiteralPath $forecourtLayoutTarget}
    if(Test-Path "$forecourtLayoutTarget.meta"){Remove-Item -LiteralPath "$forecourtLayoutTarget.meta"}
}
