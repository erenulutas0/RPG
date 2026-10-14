#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$gateMasonryRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateMasonryTarget=Join-Path $gateMasonryRoot 'Assets/_Project/Tests/PlayMode/FoundryGateMasonryCapture.cs'
$gateMasonryResults=Join-Path $gateMasonryRoot 'TestResults/foundry-gate-masonry.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gateMasonryRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $gateMasonryTarget) -or (Test-Path "$gateMasonryTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $gateMasonryHead=(git -C $gateMasonryRoot rev-parse HEAD).Trim()
    if($gateMasonryHead -ne 'd34c56115fc0a771f648baf3601ab740a10b871d'){throw 'Before capture requires the recorded baseline HEAD.'}
    $gateMasonryDirty=git -C $gateMasonryRoot status --porcelain --untracked-files=all -- Assets
    if($gateMasonryDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$gateMasonryRoot/TestResults/foundry-gate-masonry-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryGateMasonryCapture.cs" -Destination $gateMasonryTarget
    Set-Content -LiteralPath "$gateMasonryTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $gateMasonryResults){Remove-Item -LiteralPath $gateMasonryResults}
    $gateMasonryProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$gateMasonryRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryGateMasonryCapture','-testResults',"`"$gateMasonryResults`"",'-logFile',"`"$gateMasonryRoot/TestResults/foundry-gate-masonry.log`"") -PassThru -WindowStyle Hidden
    $gateMasonryProcess.WaitForExit()
    if($gateMasonryProcess.ExitCode -ne 0 -or !(Test-Path $gateMasonryResults)){throw 'Layout capture failed; see TestResults/foundry-gate-masonry.log.'}
    $gateMasonryRun=([xml](Get-Content -LiteralPath $gateMasonryResults)).'test-run'
    if($gateMasonryRun.result -ne 'Passed'){throw "Layout capture failed: $($gateMasonryRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($gateMasonryFile in Get-ChildItem "$gateMasonryRoot/ArtDirection/2026-10-14/foundry-gate-masonry-01/$Phase" -Filter '*.bmp'){
        $gateMasonryBitmap=[System.Drawing.Bitmap]::new($gateMasonryFile.FullName)
        try{$gateMasonryBitmap.Save([IO.Path]::ChangeExtension($gateMasonryFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$gateMasonryBitmap.Dispose()}
        Remove-Item -LiteralPath $gateMasonryFile.FullName
    }
    Write-Output "Gate masonry actual Unity capture: $($gateMasonryRun.passed) passed."
} finally {
    if(Test-Path $gateMasonryTarget){Remove-Item -LiteralPath $gateMasonryTarget}
    if(Test-Path "$gateMasonryTarget.meta"){Remove-Item -LiteralPath "$gateMasonryTarget.meta"}
}
