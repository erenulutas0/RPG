#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$pitStacksRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$pitStacksTarget=Join-Path $pitStacksRoot 'Assets/_Project/Tests/PlayMode/FoundryPitStacksCapture.cs'
$pitStacksResults=Join-Path $pitStacksRoot 'TestResults/foundry-pit-stacks.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$pitStacksRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $pitStacksTarget) -or (Test-Path "$pitStacksTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $pitStacksHead=(git -C $pitStacksRoot rev-parse HEAD).Trim()
    if($pitStacksHead -ne '7769e7482a1de97a797a233242eb87532b999964'){throw 'Before capture requires the recorded baseline HEAD.'}
    $pitStacksDirty=git -C $pitStacksRoot status --porcelain --untracked-files=all -- Assets
    if($pitStacksDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$pitStacksRoot/TestResults/foundry-pit-stacks-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryPitStacksCapture.cs" -Destination $pitStacksTarget
    Set-Content -LiteralPath "$pitStacksTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $pitStacksResults){Remove-Item -LiteralPath $pitStacksResults}
    $pitStacksProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$pitStacksRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryPitStacksCapture','-testResults',"`"$pitStacksResults`"",'-logFile',"`"$pitStacksRoot/TestResults/foundry-pit-stacks.log`"") -PassThru -WindowStyle Hidden
    $pitStacksProcess.WaitForExit()
    if($pitStacksProcess.ExitCode -ne 0 -or !(Test-Path $pitStacksResults)){throw 'Layout capture failed; see TestResults/foundry-pit-stacks.log.'}
    $pitStacksRun=([xml](Get-Content -LiteralPath $pitStacksResults)).'test-run'
    if($pitStacksRun.result -ne 'Passed'){throw "Layout capture failed: $($pitStacksRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($pitStacksFile in Get-ChildItem "$pitStacksRoot/ArtDirection/2026-10-23/foundry-pit-stacks-01/$Phase" -Filter '*.bmp'){
        $pitStacksBitmap=[System.Drawing.Bitmap]::new($pitStacksFile.FullName)
        try{$pitStacksBitmap.Save([IO.Path]::ChangeExtension($pitStacksFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$pitStacksBitmap.Dispose()}
        Remove-Item -LiteralPath $pitStacksFile.FullName
    }
    Write-Output "Foundry pit stacks actual Unity capture: $($pitStacksRun.passed) passed."
} finally {
    if(Test-Path $pitStacksTarget){Remove-Item -LiteralPath $pitStacksTarget}
    if(Test-Path "$pitStacksTarget.meta"){Remove-Item -LiteralPath "$pitStacksTarget.meta"}
}
