#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$rimFireRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$rimFireTarget=Join-Path $rimFireRoot 'Assets/_Project/Tests/PlayMode/FoundryRimFireCapture.cs'
$rimFireResults=Join-Path $rimFireRoot 'TestResults/foundry-rim-fire.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$rimFireRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $rimFireTarget) -or (Test-Path "$rimFireTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $rimFireHead=(git -C $rimFireRoot rev-parse HEAD).Trim()
    if($rimFireHead -ne '38cad41f79f1f9a7227f0d35f00993f481a94046'){throw 'Before capture requires the recorded baseline HEAD.'}
    $rimFireDirty=git -C $rimFireRoot status --porcelain --untracked-files=all -- Assets
    if($rimFireDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$rimFireRoot/TestResults/foundry-rim-fire-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryRimFireCapture.cs" -Destination $rimFireTarget
    Set-Content -LiteralPath "$rimFireTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $rimFireResults){Remove-Item -LiteralPath $rimFireResults}
    $rimFireProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$rimFireRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryRimFireCapture','-testResults',"`"$rimFireResults`"",'-logFile',"`"$rimFireRoot/TestResults/foundry-rim-fire.log`"") -PassThru -WindowStyle Hidden
    $rimFireProcess.WaitForExit()
    if($rimFireProcess.ExitCode -ne 0 -or !(Test-Path $rimFireResults)){throw 'Layout capture failed; see TestResults/foundry-rim-fire.log.'}
    $rimFireRun=([xml](Get-Content -LiteralPath $rimFireResults)).'test-run'
    if($rimFireRun.result -ne 'Passed'){throw "Layout capture failed: $($rimFireRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($rimFireFile in Get-ChildItem "$rimFireRoot/ArtDirection/2026-10-21/foundry-rim-fire-01/$Phase" -Filter '*.bmp'){
        $rimFireBitmap=[System.Drawing.Bitmap]::new($rimFireFile.FullName)
        try{$rimFireBitmap.Save([IO.Path]::ChangeExtension($rimFireFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$rimFireBitmap.Dispose()}
        Remove-Item -LiteralPath $rimFireFile.FullName
    }
    Write-Output "Foundry rim fire actual Unity capture: $($rimFireRun.passed) passed."
} finally {
    if(Test-Path $rimFireTarget){Remove-Item -LiteralPath $rimFireTarget}
    if(Test-Path "$rimFireTarget.meta"){Remove-Item -LiteralPath "$rimFireTarget.meta"}
}
