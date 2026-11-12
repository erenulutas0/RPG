#requires -Version 7
# 2026-11-12 foundry court processional ways (foundry-court-ways-02).
# Before: run in the main checkout at 61df2510 or later with unchanged Assets. After: run on the changed branch.
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$proofRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofTarget=Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/FoundryCourtWaysCapture.cs'
$proofResults=Join-Path $proofRoot 'TestResults/foundry-court-ways.xml'
$proofOutput=Join-Path $proofRoot "ArtDirection/2026-11-12/foundry-court-ways-02/$Phase"
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$proofRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $proofTarget) -or (Test-Path "$proofTarget.meta")){throw 'Temporary capture exists; inspect first.'}
if($Phase -eq 'before'){
    git -C $proofRoot merge-base --is-ancestor 61df2510663cffcfa6e9a6ad4e8161055a002ccd HEAD
    if($LASTEXITCODE -ne 0){throw 'Before capture requires main at 61df2510 or later.'}
    $proofDirty=git -C $proofRoot status --porcelain --untracked-files=all -- Assets
    if($proofDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
New-Item -ItemType Directory -Force (Join-Path $proofRoot 'TestResults') | Out-Null
Set-Content "$proofRoot/TestResults/foundry-court-ways-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryCourtWaysCapture.cs" -Destination $proofTarget
    Set-Content -LiteralPath "$proofTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $proofResults){Remove-Item -LiteralPath $proofResults}
    $proofProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryCourtWaysCapture','-testResults',"`"$proofResults`"",'-logFile',"`"$proofRoot/TestResults/foundry-court-ways.log`"") -PassThru -WindowStyle Hidden
    $proofProcess.WaitForExit()
    if($proofProcess.ExitCode -ne 0 -or !(Test-Path $proofResults)){throw "Capture failed (exit $($proofProcess.ExitCode)); see TestResults/foundry-court-ways.log."}
    $proofRun=([xml](Get-Content -LiteralPath $proofResults)).'test-run'
    if($proofRun.result -ne 'Passed'){throw "Capture failed: $($proofRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($proofFile in Get-ChildItem $proofOutput -Filter '*.bmp'){
        $proofBitmap=[System.Drawing.Bitmap]::new($proofFile.FullName)
        try{$proofBitmap.Save([IO.Path]::ChangeExtension($proofFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$proofBitmap.Dispose()}
        Remove-Item -LiteralPath $proofFile.FullName
    }
    Write-Output "Foundry court ways $Phase capture: $($proofRun.passed) passed; $((Get-ChildItem $proofOutput -Filter '*.png').Count) PNGs in $proofOutput"
    Get-Content (Join-Path $proofOutput 'runtime-budget.txt') -TotalCount 1
} finally {
    if(Test-Path $proofTarget){Remove-Item -LiteralPath $proofTarget}
    if(Test-Path "$proofTarget.meta"){Remove-Item -LiteralPath "$proofTarget.meta"}
}
