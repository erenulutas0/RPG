#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$gateStoneworkRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateStoneworkTarget=Join-Path $gateStoneworkRoot 'Assets/_Project/Tests/PlayMode/FoundryGateStoneworkCapture.cs'
$gateStoneworkResults=Join-Path $gateStoneworkRoot 'TestResults/foundry-gate-stonework.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gateStoneworkRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $gateStoneworkTarget) -or (Test-Path "$gateStoneworkTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $gateStoneworkHead=(git -C $gateStoneworkRoot rev-parse HEAD).Trim()
    if($gateStoneworkHead -ne 'e0bd846e4ba7ecc0f1ec3b8ca6da70847bcfb1d3'){throw 'Before capture requires the recorded baseline HEAD.'}
    $gateStoneworkDirty=git -C $gateStoneworkRoot status --porcelain --untracked-files=all -- Assets
    if($gateStoneworkDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$gateStoneworkRoot/TestResults/foundry-gate-stonework-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryGateStoneworkCapture.cs" -Destination $gateStoneworkTarget
    Set-Content -LiteralPath "$gateStoneworkTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $gateStoneworkResults){Remove-Item -LiteralPath $gateStoneworkResults}
    $gateStoneworkProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$gateStoneworkRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryGateStoneworkCapture','-testResults',"`"$gateStoneworkResults`"",'-logFile',"`"$gateStoneworkRoot/TestResults/foundry-gate-stonework.log`"") -PassThru -WindowStyle Hidden
    $gateStoneworkProcess.WaitForExit()
    if($gateStoneworkProcess.ExitCode -ne 0 -or !(Test-Path $gateStoneworkResults)){throw 'Layout capture failed; see TestResults/foundry-gate-stonework.log.'}
    $gateStoneworkRun=([xml](Get-Content -LiteralPath $gateStoneworkResults)).'test-run'
    if($gateStoneworkRun.result -ne 'Passed'){throw "Layout capture failed: $($gateStoneworkRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($gateStoneworkFile in Get-ChildItem "$gateStoneworkRoot/ArtDirection/2026-10-14/foundry-gate-stonework-01/$Phase" -Filter '*.bmp'){
        $gateStoneworkBitmap=[System.Drawing.Bitmap]::new($gateStoneworkFile.FullName)
        try{$gateStoneworkBitmap.Save([IO.Path]::ChangeExtension($gateStoneworkFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$gateStoneworkBitmap.Dispose()}
        Remove-Item -LiteralPath $gateStoneworkFile.FullName
    }
    Write-Output "Gate stonework actual Unity capture: $($gateStoneworkRun.passed) passed."
} finally {
    if(Test-Path $gateStoneworkTarget){Remove-Item -LiteralPath $gateStoneworkTarget}
    if(Test-Path "$gateStoneworkTarget.meta"){Remove-Item -LiteralPath "$gateStoneworkTarget.meta"}
}
