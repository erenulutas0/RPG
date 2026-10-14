#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$materialStudyRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$materialStudyTarget=Join-Path $materialStudyRoot 'Assets/_Project/Tests/PlayMode/FoundryMaterialStudyCapture.cs'
$materialStudyResults=Join-Path $materialStudyRoot 'TestResults/foundry-material-study.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$materialStudyRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $materialStudyTarget) -or (Test-Path "$materialStudyTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $materialStudyHead=(git -C $materialStudyRoot rev-parse HEAD).Trim()
    if($materialStudyHead -ne '66b83f556d764d13704c2efd94a4f9e2bc04dc1d'){throw 'Before capture requires the recorded baseline HEAD.'}
    $materialStudyDirty=git -C $materialStudyRoot status --porcelain --untracked-files=all -- Assets
    if($materialStudyDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$materialStudyRoot/TestResults/foundry-material-study-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryMaterialStudyCapture.cs" -Destination $materialStudyTarget
    Set-Content -LiteralPath "$materialStudyTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $materialStudyResults){Remove-Item -LiteralPath $materialStudyResults}
    $materialStudyProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$materialStudyRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryMaterialStudyCapture','-testResults',"`"$materialStudyResults`"",'-logFile',"`"$materialStudyRoot/TestResults/foundry-material-study.log`"") -PassThru -WindowStyle Hidden
    $materialStudyProcess.WaitForExit()
    if($materialStudyProcess.ExitCode -ne 0 -or !(Test-Path $materialStudyResults)){throw 'Layout capture failed; see TestResults/foundry-material-study.log.'}
    $materialStudyRun=([xml](Get-Content -LiteralPath $materialStudyResults)).'test-run'
    if($materialStudyRun.result -ne 'Passed'){throw "Layout capture failed: $($materialStudyRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($materialStudyFile in Get-ChildItem "$materialStudyRoot/ArtDirection/2026-10-14/foundry-material-study-01/$Phase" -Filter '*.bmp'){
        $materialStudyBitmap=[System.Drawing.Bitmap]::new($materialStudyFile.FullName)
        try{$materialStudyBitmap.Save([IO.Path]::ChangeExtension($materialStudyFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$materialStudyBitmap.Dispose()}
        Remove-Item -LiteralPath $materialStudyFile.FullName
    }
    Write-Output "Material study actual Unity capture: $($materialStudyRun.passed) passed."
} finally {
    if(Test-Path $materialStudyTarget){Remove-Item -LiteralPath $materialStudyTarget}
    if(Test-Path "$materialStudyTarget.meta"){Remove-Item -LiteralPath "$materialStudyTarget.meta"}
}
