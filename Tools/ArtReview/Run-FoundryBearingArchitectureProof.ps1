#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$bearingArchitectureRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$bearingArchitectureTarget=Join-Path $bearingArchitectureRoot 'Assets/_Project/Tests/PlayMode/FoundryBearingArchitectureCapture.cs'
$bearingArchitectureResults=Join-Path $bearingArchitectureRoot 'TestResults/foundry-bearing-architecture.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$bearingArchitectureRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $bearingArchitectureTarget) -or (Test-Path "$bearingArchitectureTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $bearingArchitectureHead=(git -C $bearingArchitectureRoot rev-parse HEAD).Trim()
    if($bearingArchitectureHead -ne 'cf511c38f325f43ec3f9d9005ed5b638c1bc0db1'){throw 'Before capture requires the recorded baseline HEAD.'}
    $bearingArchitectureDirty=git -C $bearingArchitectureRoot status --porcelain --untracked-files=all -- Assets
    if($bearingArchitectureDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$bearingArchitectureRoot/TestResults/foundry-bearing-architecture-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryBearingArchitectureCapture.cs" -Destination $bearingArchitectureTarget
    Set-Content -LiteralPath "$bearingArchitectureTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $bearingArchitectureResults){Remove-Item -LiteralPath $bearingArchitectureResults}
    $bearingArchitectureProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$bearingArchitectureRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryBearingArchitectureCapture','-testResults',"`"$bearingArchitectureResults`"",'-logFile',"`"$bearingArchitectureRoot/TestResults/foundry-bearing-architecture.log`"") -PassThru -WindowStyle Hidden
    $bearingArchitectureProcess.WaitForExit()
    if($bearingArchitectureProcess.ExitCode -ne 0 -or !(Test-Path $bearingArchitectureResults)){throw 'Layout capture failed; see TestResults/foundry-bearing-architecture.log.'}
    $bearingArchitectureRun=([xml](Get-Content -LiteralPath $bearingArchitectureResults)).'test-run'
    if($bearingArchitectureRun.result -ne 'Passed'){throw "Layout capture failed: $($bearingArchitectureRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($bearingArchitectureFile in Get-ChildItem "$bearingArchitectureRoot/ArtDirection/2026-10-14/foundry-bearing-architecture-01/$Phase" -Filter '*.bmp'){
        $bearingArchitectureBitmap=[System.Drawing.Bitmap]::new($bearingArchitectureFile.FullName)
        try{$bearingArchitectureBitmap.Save([IO.Path]::ChangeExtension($bearingArchitectureFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$bearingArchitectureBitmap.Dispose()}
        Remove-Item -LiteralPath $bearingArchitectureFile.FullName
    }
    Write-Output "Bearing architecture actual Unity capture: $($bearingArchitectureRun.passed) passed."
} finally {
    if(Test-Path $bearingArchitectureTarget){Remove-Item -LiteralPath $bearingArchitectureTarget}
    if(Test-Path "$bearingArchitectureTarget.meta"){Remove-Item -LiteralPath "$bearingArchitectureTarget.meta"}
}
