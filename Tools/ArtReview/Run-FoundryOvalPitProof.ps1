#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$ovalPitRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$ovalPitTarget=Join-Path $ovalPitRoot 'Assets/_Project/Tests/PlayMode/FoundryOvalPitCapture.cs'
$ovalPitResults=Join-Path $ovalPitRoot 'TestResults/foundry-oval-pit.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$ovalPitRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $ovalPitTarget) -or (Test-Path "$ovalPitTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $ovalPitHead=(git -C $ovalPitRoot rev-parse HEAD).Trim()
    if($ovalPitHead -ne '1b91b67ea0b680f4e0001c3bfadf71912d55ba63'){throw 'Before capture requires the recorded baseline HEAD.'}
    $ovalPitDirty=git -C $ovalPitRoot status --porcelain --untracked-files=all -- Assets
    if($ovalPitDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$ovalPitRoot/TestResults/foundry-oval-pit-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryOvalPitCapture.cs" -Destination $ovalPitTarget
    Set-Content -LiteralPath "$ovalPitTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $ovalPitResults){Remove-Item -LiteralPath $ovalPitResults}
    $ovalPitProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$ovalPitRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryOvalPitCapture','-testResults',"`"$ovalPitResults`"",'-logFile',"`"$ovalPitRoot/TestResults/foundry-oval-pit.log`"") -PassThru -WindowStyle Hidden
    $ovalPitProcess.WaitForExit()
    if($ovalPitProcess.ExitCode -ne 0 -or !(Test-Path $ovalPitResults)){throw 'Layout capture failed; see TestResults/foundry-oval-pit.log.'}
    $ovalPitRun=([xml](Get-Content -LiteralPath $ovalPitResults)).'test-run'
    if($ovalPitRun.result -ne 'Passed'){throw "Layout capture failed: $($ovalPitRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($ovalPitFile in Get-ChildItem "$ovalPitRoot/ArtDirection/2026-10-21/foundry-oval-pit-01/$Phase" -Filter '*.bmp'){
        $ovalPitBitmap=[System.Drawing.Bitmap]::new($ovalPitFile.FullName)
        try{$ovalPitBitmap.Save([IO.Path]::ChangeExtension($ovalPitFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$ovalPitBitmap.Dispose()}
        Remove-Item -LiteralPath $ovalPitFile.FullName
    }
    Write-Output "Foundry oval pit actual Unity capture: $($ovalPitRun.passed) passed."
} finally {
    if(Test-Path $ovalPitTarget){Remove-Item -LiteralPath $ovalPitTarget}
    if(Test-Path "$ovalPitTarget.meta"){Remove-Item -LiteralPath "$ovalPitTarget.meta"}
}
