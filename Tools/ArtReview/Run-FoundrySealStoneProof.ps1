#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$sealStoneRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sealStoneTarget=Join-Path $sealStoneRoot 'Assets/_Project/Tests/PlayMode/FoundrySealStoneCapture.cs'
$sealStoneResults=Join-Path $sealStoneRoot 'TestResults/foundry-seal-stone.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$sealStoneRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $sealStoneTarget) -or (Test-Path "$sealStoneTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $sealStoneHead=(git -C $sealStoneRoot rev-parse HEAD).Trim()
    if($sealStoneHead -ne '6fc8a7d68f18e5bb027a98fc060ca148f634f94d'){throw 'Before capture requires the recorded baseline HEAD.'}
    $sealStoneDirty=git -C $sealStoneRoot status --porcelain --untracked-files=all -- Assets
    if($sealStoneDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$sealStoneRoot/TestResults/foundry-seal-stone-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundrySealStoneCapture.cs" -Destination $sealStoneTarget
    Set-Content -LiteralPath "$sealStoneTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $sealStoneResults){Remove-Item -LiteralPath $sealStoneResults}
    $sealStoneProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$sealStoneRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundrySealStoneCapture','-testResults',"`"$sealStoneResults`"",'-logFile',"`"$sealStoneRoot/TestResults/foundry-seal-stone.log`"") -PassThru -WindowStyle Hidden
    $sealStoneProcess.WaitForExit()
    if($sealStoneProcess.ExitCode -ne 0 -or !(Test-Path $sealStoneResults)){throw 'Layout capture failed; see TestResults/foundry-seal-stone.log.'}
    $sealStoneRun=([xml](Get-Content -LiteralPath $sealStoneResults)).'test-run'
    if($sealStoneRun.result -ne 'Passed'){throw "Layout capture failed: $($sealStoneRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($sealStoneFile in Get-ChildItem "$sealStoneRoot/ArtDirection/2026-10-22/foundry-seal-stone-01/$Phase" -Filter '*.bmp'){
        $sealStoneBitmap=[System.Drawing.Bitmap]::new($sealStoneFile.FullName)
        try{$sealStoneBitmap.Save([IO.Path]::ChangeExtension($sealStoneFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$sealStoneBitmap.Dispose()}
        Remove-Item -LiteralPath $sealStoneFile.FullName
    }
    Write-Output "Foundry seal stone actual Unity capture: $($sealStoneRun.passed) passed."
} finally {
    if(Test-Path $sealStoneTarget){Remove-Item -LiteralPath $sealStoneTarget}
    if(Test-Path "$sealStoneTarget.meta"){Remove-Item -LiteralPath "$sealStoneTarget.meta"}
}
