#requires -Version 7
param([string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$rewardRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$rewardTarget=Join-Path $rewardRoot 'Assets/_Project/Tests/PlayMode/RegionRuntimeCapture.cs'
$rewardResults=Join-Path $rewardRoot 'TestResults/region-runtime.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$rewardRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $rewardTarget) -or (Test-Path "$rewardTarget.meta")){throw 'Temporary reward capture exists; inspect first.'}
$exploreScene=Join-Path $rewardRoot 'Assets/_Project/Scenes/Gameplay/ExplorationProof.unity'
if((Test-Path $exploreScene) -or (Test-Path "$exploreScene.meta")){throw 'Exploration scene exists; inspect first.'}
try {
    Copy-Item -LiteralPath "$PSScriptRoot/RegionRuntimeCapture.cs" -Destination $rewardTarget
    Set-Content -LiteralPath "$rewardTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $rewardResults){Remove-Item -LiteralPath $rewardResults}
    $rewardProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$rewardRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','RegionRuntimeCapture','-testResults',"`"$rewardResults`"",'-logFile',"`"$rewardRoot/TestResults/region-runtime.log`"") -PassThru -WindowStyle Hidden
    $rewardProcess.WaitForExit()
    if($rewardProcess.ExitCode -ne 0 -or !(Test-Path $rewardResults)){throw 'Reward capture failed; see TestResults/region-runtime.log.'}
    $rewardRun=([xml](Get-Content -LiteralPath $rewardResults)).'test-run'
    if($rewardRun.result -ne 'Passed'){throw "Reward capture failed: $($rewardRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($rewardFile in Get-ChildItem "$rewardRoot/ArtDirection/2026-09-23/region-runtime-01" -Filter '*.bmp'){
        $rewardBitmap=[System.Drawing.Bitmap]::new($rewardFile.FullName)
        try{$rewardBitmap.Save([IO.Path]::ChangeExtension($rewardFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$rewardBitmap.Dispose()}
        Remove-Item -LiteralPath $rewardFile.FullName
    }
    Write-Output "Reward actual Unity capture: $($rewardRun.passed) passed."
} finally {
    if(Test-Path $exploreScene){Remove-Item -LiteralPath $exploreScene}
    if(Test-Path "$exploreScene.meta"){Remove-Item -LiteralPath "$exploreScene.meta"}
    if(Test-Path $rewardTarget){Remove-Item -LiteralPath $rewardTarget}
    if(Test-Path "$rewardTarget.meta"){Remove-Item -LiteralPath "$rewardTarget.meta"}
}


