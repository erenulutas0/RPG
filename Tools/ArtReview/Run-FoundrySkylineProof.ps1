#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$rewardRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$rewardTarget=Join-Path $rewardRoot 'Assets/_Project/Tests/PlayMode/FoundrySkylineCapture.cs'
$rewardResults=Join-Path $rewardRoot 'TestResults/foundry-skyline.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$rewardRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $rewardTarget) -or (Test-Path "$rewardTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$rewardRoot/TestResults/foundry-skyline-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundrySkylineCapture.cs" -Destination $rewardTarget
    Set-Content -LiteralPath "$rewardTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $rewardResults){Remove-Item -LiteralPath $rewardResults}
    $rewardProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$rewardRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundrySkylineCapture','-testResults',"`"$rewardResults`"",'-logFile',"`"$rewardRoot/TestResults/foundry-skyline.log`"") -PassThru -WindowStyle Hidden
    $rewardProcess.WaitForExit()
    if($rewardProcess.ExitCode -ne 0 -or !(Test-Path $rewardResults)){throw 'Layout capture failed; see TestResults/foundry-skyline.log.'}
    $rewardRun=([xml](Get-Content -LiteralPath $rewardResults)).'test-run'
    if($rewardRun.result -ne 'Passed'){throw "Layout capture failed: $($rewardRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($rewardFile in Get-ChildItem "$rewardRoot/ArtDirection/2026-10-10/foundry-skyline-01/$Phase" -Filter '*.bmp'){
        $rewardBitmap=[System.Drawing.Bitmap]::new($rewardFile.FullName)
        try{$rewardBitmap.Save([IO.Path]::ChangeExtension($rewardFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$rewardBitmap.Dispose()}
        Remove-Item -LiteralPath $rewardFile.FullName
    }
    Write-Output "Layout actual Unity capture: $($rewardRun.passed) passed."
} finally {
    if(Test-Path $rewardTarget){Remove-Item -LiteralPath $rewardTarget}
    if(Test-Path "$rewardTarget.meta"){Remove-Item -LiteralPath "$rewardTarget.meta"}
}
