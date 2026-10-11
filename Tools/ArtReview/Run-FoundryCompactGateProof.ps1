#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$islandsRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$islandsTarget=Join-Path $islandsRoot 'Assets/_Project/Tests/PlayMode/FoundryCompactGateCapture.cs'
$islandsResults=Join-Path $islandsRoot 'TestResults/foundry-compact-gate.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$islandsRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $islandsTarget) -or (Test-Path "$islandsTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$islandsRoot/TestResults/foundry-compact-gate-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCompactGateCapture.cs" -Destination $islandsTarget
    Set-Content -LiteralPath "$islandsTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $islandsResults){Remove-Item -LiteralPath $islandsResults}
    $islandsProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$islandsRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCompactGateCapture','-testResults',"`"$islandsResults`"",'-logFile',"`"$islandsRoot/TestResults/foundry-compact-gate.log`"") -PassThru -WindowStyle Hidden
    $islandsProcess.WaitForExit()
    if($islandsProcess.ExitCode -ne 0 -or !(Test-Path $islandsResults)){throw 'Layout capture failed; see TestResults/foundry-compact-gate.log.'}
    $islandsRun=([xml](Get-Content -LiteralPath $islandsResults)).'test-run'
    if($islandsRun.result -ne 'Passed'){throw "Layout capture failed: $($islandsRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($islandsFile in Get-ChildItem "$islandsRoot/ArtDirection/2026-10-11/foundry-compact-gate-01/$Phase" -Filter '*.bmp'){
        $islandsBitmap=[System.Drawing.Bitmap]::new($islandsFile.FullName)
        try{$islandsBitmap.Save([IO.Path]::ChangeExtension($islandsFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$islandsBitmap.Dispose()}
        Remove-Item -LiteralPath $islandsFile.FullName
    }
    Write-Output "Layout actual Unity capture: $($islandsRun.passed) passed."
} finally {
    if(Test-Path $islandsTarget){Remove-Item -LiteralPath $islandsTarget}
    if(Test-Path "$islandsTarget.meta"){Remove-Item -LiteralPath "$islandsTarget.meta"}
}
