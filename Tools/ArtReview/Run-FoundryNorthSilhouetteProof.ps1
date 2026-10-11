#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$northRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$northTarget=Join-Path $northRoot 'Assets/_Project/Tests/PlayMode/FoundryNorthSilhouetteCapture.cs'
$northResults=Join-Path $northRoot 'TestResults/foundry-north-silhouette.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$northRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $northTarget) -or (Test-Path "$northTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$northRoot/TestResults/foundry-north-silhouette-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryNorthSilhouetteCapture.cs" -Destination $northTarget
    Set-Content -LiteralPath "$northTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $northResults){Remove-Item -LiteralPath $northResults}
    $northProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$northRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryNorthSilhouetteCapture','-testResults',"`"$northResults`"",'-logFile',"`"$northRoot/TestResults/foundry-north-silhouette.log`"") -PassThru -WindowStyle Hidden
    $northProcess.WaitForExit()
    if($northProcess.ExitCode -ne 0 -or !(Test-Path $northResults)){throw 'Layout capture failed; see TestResults/foundry-north-silhouette.log.'}
    $northRun=([xml](Get-Content -LiteralPath $northResults)).'test-run'
    if($northRun.result -ne 'Passed'){throw "Layout capture failed: $($northRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($northFile in Get-ChildItem "$northRoot/ArtDirection/2026-10-11/foundry-north-silhouette-01/$Phase" -Filter '*.bmp'){
        $northBitmap=[System.Drawing.Bitmap]::new($northFile.FullName)
        try{$northBitmap.Save([IO.Path]::ChangeExtension($northFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$northBitmap.Dispose()}
        Remove-Item -LiteralPath $northFile.FullName
    }
    Write-Output "Layout actual Unity capture: $($northRun.passed) passed."
} finally {
    if(Test-Path $northTarget){Remove-Item -LiteralPath $northTarget}
    if(Test-Path "$northTarget.meta"){Remove-Item -LiteralPath "$northTarget.meta"}
}
