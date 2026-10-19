#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$gateCrownRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateCrownTarget=Join-Path $gateCrownRoot 'Assets/_Project/Tests/PlayMode/FoundryGateCrownCapture.cs'
$gateCrownResults=Join-Path $gateCrownRoot 'TestResults/foundry-gate-crown.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gateCrownRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $gateCrownTarget) -or (Test-Path "$gateCrownTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $gateCrownHead=(git -C $gateCrownRoot rev-parse HEAD).Trim()
    if($gateCrownHead -ne '3ce262627236c71165377404f674bd5cdaa2b933'){throw 'Before capture requires the recorded baseline HEAD.'}
    $gateCrownDirty=git -C $gateCrownRoot status --porcelain --untracked-files=all -- Assets
    if($gateCrownDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$gateCrownRoot/TestResults/foundry-gate-crown-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryGateCrownCapture.cs" -Destination $gateCrownTarget
    Set-Content -LiteralPath "$gateCrownTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $gateCrownResults){Remove-Item -LiteralPath $gateCrownResults}
    $gateCrownProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$gateCrownRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryGateCrownCapture','-testResults',"`"$gateCrownResults`"",'-logFile',"`"$gateCrownRoot/TestResults/foundry-gate-crown.log`"") -PassThru -WindowStyle Hidden
    $gateCrownProcess.WaitForExit()
    if($gateCrownProcess.ExitCode -ne 0 -or !(Test-Path $gateCrownResults)){throw 'Layout capture failed; see TestResults/foundry-gate-crown.log.'}
    $gateCrownRun=([xml](Get-Content -LiteralPath $gateCrownResults)).'test-run'
    if($gateCrownRun.result -ne 'Passed'){throw "Layout capture failed: $($gateCrownRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($gateCrownFile in Get-ChildItem "$gateCrownRoot/ArtDirection/2026-10-19/foundry-gate-crown-01/$Phase" -Filter '*.bmp'){
        $gateCrownBitmap=[System.Drawing.Bitmap]::new($gateCrownFile.FullName)
        try{$gateCrownBitmap.Save([IO.Path]::ChangeExtension($gateCrownFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$gateCrownBitmap.Dispose()}
        Remove-Item -LiteralPath $gateCrownFile.FullName
    }
    Write-Output "Foundry gate crown actual Unity capture: $($gateCrownRun.passed) passed."
} finally {
    if(Test-Path $gateCrownTarget){Remove-Item -LiteralPath $gateCrownTarget}
    if(Test-Path "$gateCrownTarget.meta"){Remove-Item -LiteralPath "$gateCrownTarget.meta"}
}
