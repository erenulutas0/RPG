#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$apronStoneRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$apronStoneTarget=Join-Path $apronStoneRoot 'Assets/_Project/Tests/PlayMode/FoundryApronStoneCapture.cs'
$apronStoneResults=Join-Path $apronStoneRoot 'TestResults/foundry-apron-stone.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$apronStoneRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $apronStoneTarget) -or (Test-Path "$apronStoneTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $apronStoneHead=(git -C $apronStoneRoot rev-parse HEAD).Trim()
    if($apronStoneHead -ne '4fa884fe540e1e86c15415eee8b30beb29735046'){throw 'Before capture requires the recorded baseline HEAD.'}
    $apronStoneDirty=git -C $apronStoneRoot status --porcelain --untracked-files=all -- Assets
    if($apronStoneDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$apronStoneRoot/TestResults/foundry-apron-stone-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryApronStoneCapture.cs" -Destination $apronStoneTarget
    Set-Content -LiteralPath "$apronStoneTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $apronStoneResults){Remove-Item -LiteralPath $apronStoneResults}
    $apronStoneProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$apronStoneRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryApronStoneCapture','-testResults',"`"$apronStoneResults`"",'-logFile',"`"$apronStoneRoot/TestResults/foundry-apron-stone.log`"") -PassThru -WindowStyle Hidden
    $apronStoneProcess.WaitForExit()
    if($apronStoneProcess.ExitCode -ne 0 -or !(Test-Path $apronStoneResults)){throw 'Layout capture failed; see TestResults/foundry-apron-stone.log.'}
    $apronStoneRun=([xml](Get-Content -LiteralPath $apronStoneResults)).'test-run'
    if($apronStoneRun.result -ne 'Passed'){throw "Layout capture failed: $($apronStoneRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($apronStoneFile in Get-ChildItem "$apronStoneRoot/ArtDirection/2026-10-23/foundry-apron-stone-01/$Phase" -Filter '*.bmp'){
        $apronStoneBitmap=[System.Drawing.Bitmap]::new($apronStoneFile.FullName)
        try{$apronStoneBitmap.Save([IO.Path]::ChangeExtension($apronStoneFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$apronStoneBitmap.Dispose()}
        Remove-Item -LiteralPath $apronStoneFile.FullName
    }
    Write-Output "Foundry apron stone actual Unity capture: $($apronStoneRun.passed) passed."
} finally {
    if(Test-Path $apronStoneTarget){Remove-Item -LiteralPath $apronStoneTarget}
    if(Test-Path "$apronStoneTarget.meta"){Remove-Item -LiteralPath "$apronStoneTarget.meta"}
}
