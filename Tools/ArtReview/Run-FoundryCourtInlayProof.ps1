#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$courtInlayRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$courtInlayTarget=Join-Path $courtInlayRoot 'Assets/_Project/Tests/PlayMode/FoundryCourtInlayCapture.cs'
$courtInlayResults=Join-Path $courtInlayRoot 'TestResults/foundry-court-inlay.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$courtInlayRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $courtInlayTarget) -or (Test-Path "$courtInlayTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $courtInlayHead=(git -C $courtInlayRoot rev-parse HEAD).Trim()
    if($courtInlayHead -ne '94a925f20ee4874d575206b354d47b6a0ef2c754'){throw 'Before capture requires the recorded baseline HEAD.'}
    $courtInlayDirty=git -C $courtInlayRoot status --porcelain --untracked-files=all -- Assets
    if($courtInlayDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$courtInlayRoot/TestResults/foundry-court-inlay-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCourtInlayCapture.cs" -Destination $courtInlayTarget
    Set-Content -LiteralPath "$courtInlayTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $courtInlayResults){Remove-Item -LiteralPath $courtInlayResults}
    $courtInlayProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$courtInlayRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCourtInlayCapture','-testResults',"`"$courtInlayResults`"",'-logFile',"`"$courtInlayRoot/TestResults/foundry-court-inlay.log`"") -PassThru -WindowStyle Hidden
    $courtInlayProcess.WaitForExit()
    if($courtInlayProcess.ExitCode -ne 0 -or !(Test-Path $courtInlayResults)){throw 'Layout capture failed; see TestResults/foundry-court-inlay.log.'}
    $courtInlayRun=([xml](Get-Content -LiteralPath $courtInlayResults)).'test-run'
    if($courtInlayRun.result -ne 'Passed'){throw "Layout capture failed: $($courtInlayRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($courtInlayFile in Get-ChildItem "$courtInlayRoot/ArtDirection/2026-10-20/foundry-court-inlay-01/$Phase" -Filter '*.bmp'){
        $courtInlayBitmap=[System.Drawing.Bitmap]::new($courtInlayFile.FullName)
        try{$courtInlayBitmap.Save([IO.Path]::ChangeExtension($courtInlayFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$courtInlayBitmap.Dispose()}
        Remove-Item -LiteralPath $courtInlayFile.FullName
    }
    Write-Output "Foundry court inlay actual Unity capture: $($courtInlayRun.passed) passed."
} finally {
    if(Test-Path $courtInlayTarget){Remove-Item -LiteralPath $courtInlayTarget}
    if(Test-Path "$courtInlayTarget.meta"){Remove-Item -LiteralPath "$courtInlayTarget.meta"}
}
