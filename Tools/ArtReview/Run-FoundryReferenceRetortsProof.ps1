#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$refRetortsRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$refRetortsTarget=Join-Path $refRetortsRoot 'Assets/_Project/Tests/PlayMode/FoundryReferenceRetortsCapture.cs'
$refRetortsResults=Join-Path $refRetortsRoot 'TestResults/foundry-reference-retorts.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$refRetortsRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $refRetortsTarget) -or (Test-Path "$refRetortsTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $refRetortsHead=(git -C $refRetortsRoot rev-parse HEAD).Trim()
    if($refRetortsHead -ne 'a8eae771f3c66297fab9fe8d8408a0709280e87b'){throw 'Before capture requires the recorded baseline HEAD.'}
    $refRetortsDirty=git -C $refRetortsRoot status --porcelain --untracked-files=all -- Assets
    if($refRetortsDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$refRetortsRoot/TestResults/foundry-reference-retorts-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryReferenceRetortsCapture.cs" -Destination $refRetortsTarget
    Set-Content -LiteralPath "$refRetortsTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $refRetortsResults){Remove-Item -LiteralPath $refRetortsResults}
    $refRetortsProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$refRetortsRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryReferenceRetortsCapture','-testResults',"`"$refRetortsResults`"",'-logFile',"`"$refRetortsRoot/TestResults/foundry-reference-retorts.log`"") -PassThru -WindowStyle Hidden
    $refRetortsProcess.WaitForExit()
    if($refRetortsProcess.ExitCode -ne 0 -or !(Test-Path $refRetortsResults)){throw 'Layout capture failed; see TestResults/foundry-reference-retorts.log.'}
    $refRetortsRun=([xml](Get-Content -LiteralPath $refRetortsResults)).'test-run'
    if($refRetortsRun.result -ne 'Passed'){throw "Layout capture failed: $($refRetortsRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($refRetortsFile in Get-ChildItem "$refRetortsRoot/ArtDirection/2026-10-20/foundry-reference-retorts-01/$Phase" -Filter '*.bmp'){
        $refRetortsBitmap=[System.Drawing.Bitmap]::new($refRetortsFile.FullName)
        try{$refRetortsBitmap.Save([IO.Path]::ChangeExtension($refRetortsFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$refRetortsBitmap.Dispose()}
        Remove-Item -LiteralPath $refRetortsFile.FullName
    }
    Write-Output "Foundry reference retorts actual Unity capture: $($refRetortsRun.passed) passed."
} finally {
    if(Test-Path $refRetortsTarget){Remove-Item -LiteralPath $refRetortsTarget}
    if(Test-Path "$refRetortsTarget.meta"){Remove-Item -LiteralPath "$refRetortsTarget.meta"}
}
