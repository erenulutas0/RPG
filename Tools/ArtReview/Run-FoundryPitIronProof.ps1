#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$pitIronRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$pitIronTarget=Join-Path $pitIronRoot 'Assets/_Project/Tests/PlayMode/FoundryPitIronCapture.cs'
$pitIronResults=Join-Path $pitIronRoot 'TestResults/foundry-pit-iron.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$pitIronRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $pitIronTarget) -or (Test-Path "$pitIronTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $pitIronHead=(git -C $pitIronRoot rev-parse HEAD).Trim()
    if($pitIronHead -ne 'dc1f2eee74f83b015eabce429040b0c2f93fe43f'){throw 'Before capture requires the recorded baseline HEAD.'}
    $pitIronDirty=git -C $pitIronRoot status --porcelain --untracked-files=all -- Assets
    if($pitIronDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$pitIronRoot/TestResults/foundry-pit-iron-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryPitIronCapture.cs" -Destination $pitIronTarget
    Set-Content -LiteralPath "$pitIronTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $pitIronResults){Remove-Item -LiteralPath $pitIronResults}
    $pitIronProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$pitIronRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryPitIronCapture','-testResults',"`"$pitIronResults`"",'-logFile',"`"$pitIronRoot/TestResults/foundry-pit-iron.log`"") -PassThru -WindowStyle Hidden
    $pitIronProcess.WaitForExit()
    if($pitIronProcess.ExitCode -ne 0 -or !(Test-Path $pitIronResults)){throw 'Layout capture failed; see TestResults/foundry-pit-iron.log.'}
    $pitIronRun=([xml](Get-Content -LiteralPath $pitIronResults)).'test-run'
    if($pitIronRun.result -ne 'Passed'){throw "Layout capture failed: $($pitIronRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($pitIronFile in Get-ChildItem "$pitIronRoot/ArtDirection/2026-10-23/foundry-pit-iron-01/$Phase" -Filter '*.bmp'){
        $pitIronBitmap=[System.Drawing.Bitmap]::new($pitIronFile.FullName)
        try{$pitIronBitmap.Save([IO.Path]::ChangeExtension($pitIronFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$pitIronBitmap.Dispose()}
        Remove-Item -LiteralPath $pitIronFile.FullName
    }
    Write-Output "Foundry pit iron actual Unity capture: $($pitIronRun.passed) passed."
} finally {
    if(Test-Path $pitIronTarget){Remove-Item -LiteralPath $pitIronTarget}
    if(Test-Path "$pitIronTarget.meta"){Remove-Item -LiteralPath "$pitIronTarget.meta"}
}
