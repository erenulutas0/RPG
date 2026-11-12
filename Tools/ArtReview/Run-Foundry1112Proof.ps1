#requires -Version 7
# 2026-11-12 foundry trolley (V1) and gate header/crown (V2) captures.
# Before: run in the main checkout at 5647d3ac with unchanged Assets. After: run on the changed branch.
# -Package writes into another 2026-11-12 package folder (default foundry-trolley-crown-01).
param([ValidateSet('before','after')][string]$Phase='after',[ValidatePattern('^[a-z0-9-]+$')][string]$Package='foundry-trolley-crown-01',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$proofRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofTarget=Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/Foundry1112Capture.cs'
$proofResults=Join-Path $proofRoot 'TestResults/foundry-1112.xml'
$proofOutput=Join-Path $proofRoot "ArtDirection/2026-11-12/$Package/$Phase"
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$proofRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $proofTarget) -or (Test-Path "$proofTarget.meta")){throw 'Temporary capture exists; inspect first.'}
if($Phase -eq 'before'){
    $proofHead=(git -C $proofRoot rev-parse HEAD).Trim()
    if($proofHead -ne '5647d3ac101daafde173714792e0094e00d8cd35'){throw 'Before capture requires the recorded baseline HEAD 5647d3ac.'}
    $proofDirty=git -C $proofRoot status --porcelain --untracked-files=all -- Assets
    if($proofDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
New-Item -ItemType Directory -Force (Join-Path $proofRoot 'TestResults') | Out-Null
Set-Content "$proofRoot/TestResults/foundry-1112-phase.txt" @($Phase,$Package)
try {
    Copy-Item -LiteralPath "$PSScriptRoot/Foundry1112Capture.cs" -Destination $proofTarget
    Set-Content -LiteralPath "$proofTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $proofResults){Remove-Item -LiteralPath $proofResults}
    $proofProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','Foundry1112Capture','-testResults',"`"$proofResults`"",'-logFile',"`"$proofRoot/TestResults/foundry-1112.log`"") -PassThru -WindowStyle Hidden
    $proofProcess.WaitForExit()
    if($proofProcess.ExitCode -ne 0 -or !(Test-Path $proofResults)){throw "Capture failed (exit $($proofProcess.ExitCode)); see TestResults/foundry-1112.log."}
    $proofRun=([xml](Get-Content -LiteralPath $proofResults)).'test-run'
    if($proofRun.result -ne 'Passed'){throw "Capture failed: $($proofRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($proofFile in Get-ChildItem $proofOutput -Filter '*.bmp'){
        $proofBitmap=[System.Drawing.Bitmap]::new($proofFile.FullName)
        try{$proofBitmap.Save([IO.Path]::ChangeExtension($proofFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$proofBitmap.Dispose()}
        Remove-Item -LiteralPath $proofFile.FullName
    }
    Write-Output "Foundry 1112 $Phase capture: $($proofRun.passed) passed; $((Get-ChildItem $proofOutput -Filter '*.png').Count) PNGs in $proofOutput"
    Get-Content (Join-Path $proofOutput 'runtime-budget.txt') -TotalCount 1
} finally {
    if(Test-Path $proofTarget){Remove-Item -LiteralPath $proofTarget}
    if(Test-Path "$proofTarget.meta"){Remove-Item -LiteralPath "$proofTarget.meta"}
}
