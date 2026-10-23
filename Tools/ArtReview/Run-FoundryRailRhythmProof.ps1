#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$railRhythmRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$railRhythmTarget=Join-Path $railRhythmRoot 'Assets/_Project/Tests/PlayMode/FoundryRailRhythmCapture.cs'
$railRhythmResults=Join-Path $railRhythmRoot 'TestResults/foundry-rail-rhythm.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$railRhythmRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $railRhythmTarget) -or (Test-Path "$railRhythmTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $railRhythmHead=(git -C $railRhythmRoot rev-parse HEAD).Trim()
    if($railRhythmHead -ne '56be5dc66e1eb7d3aa67389102ec6c2a32ca92c6'){throw 'Before capture requires the recorded baseline HEAD.'}
    $railRhythmDirty=git -C $railRhythmRoot status --porcelain --untracked-files=all -- Assets
    if($railRhythmDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$railRhythmRoot/TestResults/foundry-rail-rhythm-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryRailRhythmCapture.cs" -Destination $railRhythmTarget
    Set-Content -LiteralPath "$railRhythmTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $railRhythmResults){Remove-Item -LiteralPath $railRhythmResults}
    $railRhythmProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$railRhythmRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryRailRhythmCapture','-testResults',"`"$railRhythmResults`"",'-logFile',"`"$railRhythmRoot/TestResults/foundry-rail-rhythm.log`"") -PassThru -WindowStyle Hidden
    $railRhythmProcess.WaitForExit()
    if($railRhythmProcess.ExitCode -ne 0 -or !(Test-Path $railRhythmResults)){throw 'Layout capture failed; see TestResults/foundry-rail-rhythm.log.'}
    $railRhythmRun=([xml](Get-Content -LiteralPath $railRhythmResults)).'test-run'
    if($railRhythmRun.result -ne 'Passed'){throw "Layout capture failed: $($railRhythmRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($railRhythmFile in Get-ChildItem "$railRhythmRoot/ArtDirection/2026-10-23/foundry-rail-rhythm-01/$Phase" -Filter '*.bmp'){
        $railRhythmBitmap=[System.Drawing.Bitmap]::new($railRhythmFile.FullName)
        try{$railRhythmBitmap.Save([IO.Path]::ChangeExtension($railRhythmFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$railRhythmBitmap.Dispose()}
        Remove-Item -LiteralPath $railRhythmFile.FullName
    }
    Write-Output "Foundry rail rhythm actual Unity capture: $($railRhythmRun.passed) passed."
} finally {
    if(Test-Path $railRhythmTarget){Remove-Item -LiteralPath $railRhythmTarget}
    if(Test-Path "$railRhythmTarget.meta"){Remove-Item -LiteralPath "$railRhythmTarget.meta"}
}
