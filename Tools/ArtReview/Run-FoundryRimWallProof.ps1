#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$rimWallRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$rimWallTarget=Join-Path $rimWallRoot 'Assets/_Project/Tests/PlayMode/FoundryRimWallCapture.cs'
$rimWallResults=Join-Path $rimWallRoot 'TestResults/foundry-rim-wall.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$rimWallRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $rimWallTarget) -or (Test-Path "$rimWallTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $rimWallHead=(git -C $rimWallRoot rev-parse HEAD).Trim()
    if($rimWallHead -ne 'e53eaee5c514c8b74fd287a49f6008ca33a062b4'){throw 'Before capture requires the recorded baseline HEAD.'}
    $rimWallDirty=git -C $rimWallRoot status --porcelain --untracked-files=all -- Assets
    if($rimWallDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$rimWallRoot/TestResults/foundry-rim-wall-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryRimWallCapture.cs" -Destination $rimWallTarget
    Set-Content -LiteralPath "$rimWallTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $rimWallResults){Remove-Item -LiteralPath $rimWallResults}
    $rimWallProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$rimWallRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryRimWallCapture','-testResults',"`"$rimWallResults`"",'-logFile',"`"$rimWallRoot/TestResults/foundry-rim-wall.log`"") -PassThru -WindowStyle Hidden
    $rimWallProcess.WaitForExit()
    if($rimWallProcess.ExitCode -ne 0 -or !(Test-Path $rimWallResults)){throw 'Layout capture failed; see TestResults/foundry-rim-wall.log.'}
    $rimWallRun=([xml](Get-Content -LiteralPath $rimWallResults)).'test-run'
    if($rimWallRun.result -ne 'Passed'){throw "Layout capture failed: $($rimWallRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($rimWallFile in Get-ChildItem "$rimWallRoot/ArtDirection/2026-10-21/foundry-rim-wall-01/$Phase" -Filter '*.bmp'){
        $rimWallBitmap=[System.Drawing.Bitmap]::new($rimWallFile.FullName)
        try{$rimWallBitmap.Save([IO.Path]::ChangeExtension($rimWallFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$rimWallBitmap.Dispose()}
        Remove-Item -LiteralPath $rimWallFile.FullName
    }
    Write-Output "Foundry rim wall actual Unity capture: $($rimWallRun.passed) passed."
} finally {
    if(Test-Path $rimWallTarget){Remove-Item -LiteralPath $rimWallTarget}
    if(Test-Path "$rimWallTarget.meta"){Remove-Item -LiteralPath "$rimWallTarget.meta"}
}
