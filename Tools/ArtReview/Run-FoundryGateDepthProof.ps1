#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$gateDepthRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateDepthTarget=Join-Path $gateDepthRoot 'Assets/_Project/Tests/PlayMode/FoundryGateDepthCapture.cs'
$gateDepthResults=Join-Path $gateDepthRoot 'TestResults/foundry-gate-depth.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gateDepthRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $gateDepthTarget) -or (Test-Path "$gateDepthTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $gateDepthHead=(git -C $gateDepthRoot rev-parse HEAD).Trim()
    if($gateDepthHead -ne '94243b77ed59bb1c801ca3c7facc2fa11ecda2cb'){throw 'Before capture requires the recorded baseline HEAD.'}
    $gateDepthDirty=git -C $gateDepthRoot status --porcelain --untracked-files=all -- Assets
    if($gateDepthDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$gateDepthRoot/TestResults/foundry-gate-depth-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryGateDepthCapture.cs" -Destination $gateDepthTarget
    Set-Content -LiteralPath "$gateDepthTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $gateDepthResults){Remove-Item -LiteralPath $gateDepthResults}
    $gateDepthProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$gateDepthRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryGateDepthCapture','-testResults',"`"$gateDepthResults`"",'-logFile',"`"$gateDepthRoot/TestResults/foundry-gate-depth.log`"") -PassThru -WindowStyle Hidden
    $gateDepthProcess.WaitForExit()
    if($gateDepthProcess.ExitCode -ne 0 -or !(Test-Path $gateDepthResults)){throw 'Layout capture failed; see TestResults/foundry-gate-depth.log.'}
    $gateDepthRun=([xml](Get-Content -LiteralPath $gateDepthResults)).'test-run'
    if($gateDepthRun.result -ne 'Passed'){throw "Layout capture failed: $($gateDepthRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($gateDepthFile in Get-ChildItem "$gateDepthRoot/ArtDirection/2026-10-14/foundry-gate-depth-01/$Phase" -Filter '*.bmp'){
        $gateDepthBitmap=[System.Drawing.Bitmap]::new($gateDepthFile.FullName)
        try{$gateDepthBitmap.Save([IO.Path]::ChangeExtension($gateDepthFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$gateDepthBitmap.Dispose()}
        Remove-Item -LiteralPath $gateDepthFile.FullName
    }
    Write-Output "Gate masonry depth actual Unity capture: $($gateDepthRun.passed) passed."
} finally {
    if(Test-Path $gateDepthTarget){Remove-Item -LiteralPath $gateDepthTarget}
    if(Test-Path "$gateDepthTarget.meta"){Remove-Item -LiteralPath "$gateDepthTarget.meta"}
}
