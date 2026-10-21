#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$chainsRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$chainsTarget=Join-Path $chainsRoot 'Assets/_Project/Tests/PlayMode/FoundryChainsCapture.cs'
$chainsResults=Join-Path $chainsRoot 'TestResults/foundry-chains.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$chainsRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $chainsTarget) -or (Test-Path "$chainsTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $chainsHead=(git -C $chainsRoot rev-parse HEAD).Trim()
    if($chainsHead -ne 'fd4de4f3db895cee2efe9ae5914814672399accd'){throw 'Before capture requires the recorded baseline HEAD.'}
    $chainsDirty=git -C $chainsRoot status --porcelain --untracked-files=all -- Assets
    if($chainsDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$chainsRoot/TestResults/foundry-chains-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryChainsCapture.cs" -Destination $chainsTarget
    Set-Content -LiteralPath "$chainsTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $chainsResults){Remove-Item -LiteralPath $chainsResults}
    $chainsProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$chainsRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryChainsCapture','-testResults',"`"$chainsResults`"",'-logFile',"`"$chainsRoot/TestResults/foundry-chains.log`"") -PassThru -WindowStyle Hidden
    $chainsProcess.WaitForExit()
    if($chainsProcess.ExitCode -ne 0 -or !(Test-Path $chainsResults)){throw 'Layout capture failed; see TestResults/foundry-chains.log.'}
    $chainsRun=([xml](Get-Content -LiteralPath $chainsResults)).'test-run'
    if($chainsRun.result -ne 'Passed'){throw "Layout capture failed: $($chainsRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($chainsFile in Get-ChildItem "$chainsRoot/ArtDirection/2026-10-21/foundry-chains-01/$Phase" -Filter '*.bmp'){
        $chainsBitmap=[System.Drawing.Bitmap]::new($chainsFile.FullName)
        try{$chainsBitmap.Save([IO.Path]::ChangeExtension($chainsFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$chainsBitmap.Dispose()}
        Remove-Item -LiteralPath $chainsFile.FullName
    }
    Write-Output "Foundry chains actual Unity capture: $($chainsRun.passed) passed."
} finally {
    if(Test-Path $chainsTarget){Remove-Item -LiteralPath $chainsTarget}
    if(Test-Path "$chainsTarget.meta"){Remove-Item -LiteralPath "$chainsTarget.meta"}
}
