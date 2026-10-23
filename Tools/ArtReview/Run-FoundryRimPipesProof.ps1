#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$rimPipesRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$rimPipesTarget=Join-Path $rimPipesRoot 'Assets/_Project/Tests/PlayMode/FoundryRimPipesCapture.cs'
$rimPipesResults=Join-Path $rimPipesRoot 'TestResults/foundry-rim-pipes.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$rimPipesRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $rimPipesTarget) -or (Test-Path "$rimPipesTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $rimPipesHead=(git -C $rimPipesRoot rev-parse HEAD).Trim()
    if($rimPipesHead -ne '6d4ce2c4c578d78dc0aa671f228bed1471c0be0a'){throw 'Before capture requires the recorded baseline HEAD.'}
    $rimPipesDirty=git -C $rimPipesRoot status --porcelain --untracked-files=all -- Assets
    if($rimPipesDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$rimPipesRoot/TestResults/foundry-rim-pipes-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryRimPipesCapture.cs" -Destination $rimPipesTarget
    Set-Content -LiteralPath "$rimPipesTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $rimPipesResults){Remove-Item -LiteralPath $rimPipesResults}
    $rimPipesProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$rimPipesRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryRimPipesCapture','-testResults',"`"$rimPipesResults`"",'-logFile',"`"$rimPipesRoot/TestResults/foundry-rim-pipes.log`"") -PassThru -WindowStyle Hidden
    $rimPipesProcess.WaitForExit()
    if($rimPipesProcess.ExitCode -ne 0 -or !(Test-Path $rimPipesResults)){throw 'Layout capture failed; see TestResults/foundry-rim-pipes.log.'}
    $rimPipesRun=([xml](Get-Content -LiteralPath $rimPipesResults)).'test-run'
    if($rimPipesRun.result -ne 'Passed'){throw "Layout capture failed: $($rimPipesRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($rimPipesFile in Get-ChildItem "$rimPipesRoot/ArtDirection/2026-10-23/foundry-rim-pipes-01/$Phase" -Filter '*.bmp'){
        $rimPipesBitmap=[System.Drawing.Bitmap]::new($rimPipesFile.FullName)
        try{$rimPipesBitmap.Save([IO.Path]::ChangeExtension($rimPipesFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$rimPipesBitmap.Dispose()}
        Remove-Item -LiteralPath $rimPipesFile.FullName
    }
    Write-Output "Foundry rim pipes actual Unity capture: $($rimPipesRun.passed) passed."
} finally {
    if(Test-Path $rimPipesTarget){Remove-Item -LiteralPath $rimPipesTarget}
    if(Test-Path "$rimPipesTarget.meta"){Remove-Item -LiteralPath "$rimPipesTarget.meta"}
}
