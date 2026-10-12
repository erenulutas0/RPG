#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$materialIntegrationRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$materialIntegrationTarget=Join-Path $materialIntegrationRoot 'Assets/_Project/Tests/PlayMode/FoundryMaterialIntegrationCapture.cs'
$materialIntegrationResults=Join-Path $materialIntegrationRoot 'TestResults/foundry-material-integration.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$materialIntegrationRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $materialIntegrationTarget) -or (Test-Path "$materialIntegrationTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $materialIntegrationHead=(git -C $materialIntegrationRoot rev-parse HEAD).Trim()
    if($materialIntegrationHead -ne '3d854256d88863793b9094a71e6017c32ef82a3f'){throw 'Before capture requires the recorded baseline HEAD.'}
    $materialIntegrationDirty=git -C $materialIntegrationRoot status --porcelain --untracked-files=all -- Assets
    if($materialIntegrationDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$materialIntegrationRoot/TestResults/foundry-material-integration-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryMaterialIntegrationCapture.cs" -Destination $materialIntegrationTarget
    Set-Content -LiteralPath "$materialIntegrationTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $materialIntegrationResults){Remove-Item -LiteralPath $materialIntegrationResults}
    $materialIntegrationProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$materialIntegrationRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryMaterialIntegrationCapture','-testResults',"`"$materialIntegrationResults`"",'-logFile',"`"$materialIntegrationRoot/TestResults/foundry-material-integration.log`"") -PassThru -WindowStyle Hidden
    $materialIntegrationProcess.WaitForExit()
    if($materialIntegrationProcess.ExitCode -ne 0 -or !(Test-Path $materialIntegrationResults)){throw 'Layout capture failed; see TestResults/foundry-material-integration.log.'}
    $materialIntegrationRun=([xml](Get-Content -LiteralPath $materialIntegrationResults)).'test-run'
    if($materialIntegrationRun.result -ne 'Passed'){throw "Layout capture failed: $($materialIntegrationRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($materialIntegrationFile in Get-ChildItem "$materialIntegrationRoot/ArtDirection/2026-10-12/foundry-material-integration-01/$Phase" -Filter '*.bmp'){
        $materialIntegrationBitmap=[System.Drawing.Bitmap]::new($materialIntegrationFile.FullName)
        try{$materialIntegrationBitmap.Save([IO.Path]::ChangeExtension($materialIntegrationFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$materialIntegrationBitmap.Dispose()}
        Remove-Item -LiteralPath $materialIntegrationFile.FullName
    }
    Write-Output "Material integration actual Unity capture: $($materialIntegrationRun.passed) passed."
} finally {
    if(Test-Path $materialIntegrationTarget){Remove-Item -LiteralPath $materialIntegrationTarget}
    if(Test-Path "$materialIntegrationTarget.meta"){Remove-Item -LiteralPath "$materialIntegrationTarget.meta"}
}
