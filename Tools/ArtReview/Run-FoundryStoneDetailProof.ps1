#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$stoneDetailRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$stoneDetailTarget=Join-Path $stoneDetailRoot 'Assets/_Project/Tests/PlayMode/FoundryStoneDetailCapture.cs'
$stoneDetailResults=Join-Path $stoneDetailRoot 'TestResults/foundry-stone-detail.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$stoneDetailRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $stoneDetailTarget) -or (Test-Path "$stoneDetailTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $stoneDetailHead=(git -C $stoneDetailRoot rev-parse HEAD).Trim()
    if($stoneDetailHead -ne 'd9f33320bac6eb9fe9c5873e163337025f9ad747'){throw 'Before capture requires the recorded baseline HEAD.'}
    $stoneDetailDirty=git -C $stoneDetailRoot status --porcelain --untracked-files=all -- Assets
    if($stoneDetailDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$stoneDetailRoot/TestResults/foundry-stone-detail-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryStoneDetailCapture.cs" -Destination $stoneDetailTarget
    Set-Content -LiteralPath "$stoneDetailTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $stoneDetailResults){Remove-Item -LiteralPath $stoneDetailResults}
    $stoneDetailProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$stoneDetailRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryStoneDetailCapture','-testResults',"`"$stoneDetailResults`"",'-logFile',"`"$stoneDetailRoot/TestResults/foundry-stone-detail.log`"") -PassThru -WindowStyle Hidden
    $stoneDetailProcess.WaitForExit()
    if($stoneDetailProcess.ExitCode -ne 0 -or !(Test-Path $stoneDetailResults)){throw 'Layout capture failed; see TestResults/foundry-stone-detail.log.'}
    $stoneDetailRun=([xml](Get-Content -LiteralPath $stoneDetailResults)).'test-run'
    if($stoneDetailRun.result -ne 'Passed'){throw "Layout capture failed: $($stoneDetailRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($stoneDetailFile in Get-ChildItem "$stoneDetailRoot/ArtDirection/2026-10-14/foundry-stone-detail-01/$Phase" -Filter '*.bmp'){
        $stoneDetailBitmap=[System.Drawing.Bitmap]::new($stoneDetailFile.FullName)
        try{$stoneDetailBitmap.Save([IO.Path]::ChangeExtension($stoneDetailFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$stoneDetailBitmap.Dispose()}
        Remove-Item -LiteralPath $stoneDetailFile.FullName
    }
    Write-Output "Stone detail actual Unity capture: $($stoneDetailRun.passed) passed."
} finally {
    if(Test-Path $stoneDetailTarget){Remove-Item -LiteralPath $stoneDetailTarget}
    if(Test-Path "$stoneDetailTarget.meta"){Remove-Item -LiteralPath "$stoneDetailTarget.meta"}
}
