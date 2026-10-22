#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$keepersRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$keepersTarget=Join-Path $keepersRoot 'Assets/_Project/Tests/PlayMode/FoundryKeepersCapture.cs'
$keepersResults=Join-Path $keepersRoot 'TestResults/foundry-keepers.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$keepersRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $keepersTarget) -or (Test-Path "$keepersTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $keepersHead=(git -C $keepersRoot rev-parse HEAD).Trim()
    if($keepersHead -ne 'b4662882f76552ed4c5423b90c24e516251511ef'){throw 'Before capture requires the recorded baseline HEAD.'}
    $keepersDirty=git -C $keepersRoot status --porcelain --untracked-files=all -- Assets
    if($keepersDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$keepersRoot/TestResults/foundry-keepers-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryKeepersCapture.cs" -Destination $keepersTarget
    Set-Content -LiteralPath "$keepersTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $keepersResults){Remove-Item -LiteralPath $keepersResults}
    $keepersProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$keepersRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryKeepersCapture','-testResults',"`"$keepersResults`"",'-logFile',"`"$keepersRoot/TestResults/foundry-keepers.log`"") -PassThru -WindowStyle Hidden
    $keepersProcess.WaitForExit()
    if($keepersProcess.ExitCode -ne 0 -or !(Test-Path $keepersResults)){throw 'Layout capture failed; see TestResults/foundry-keepers.log.'}
    $keepersRun=([xml](Get-Content -LiteralPath $keepersResults)).'test-run'
    if($keepersRun.result -ne 'Passed'){throw "Layout capture failed: $($keepersRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($keepersFile in Get-ChildItem "$keepersRoot/ArtDirection/2026-10-22/foundry-keepers-01/$Phase" -Filter '*.bmp'){
        $keepersBitmap=[System.Drawing.Bitmap]::new($keepersFile.FullName)
        try{$keepersBitmap.Save([IO.Path]::ChangeExtension($keepersFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$keepersBitmap.Dispose()}
        Remove-Item -LiteralPath $keepersFile.FullName
    }
    Write-Output "Foundry bridge keepers actual Unity capture: $($keepersRun.passed) passed."
} finally {
    if(Test-Path $keepersTarget){Remove-Item -LiteralPath $keepersTarget}
    if(Test-Path "$keepersTarget.meta"){Remove-Item -LiteralPath "$keepersTarget.meta"}
}
