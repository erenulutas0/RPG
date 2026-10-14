#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$materialFamilyRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$materialFamilyTarget=Join-Path $materialFamilyRoot 'Assets/_Project/Tests/PlayMode/FoundryMaterialFamilyCapture.cs'
$materialFamilyResults=Join-Path $materialFamilyRoot 'TestResults/foundry-material-family.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$materialFamilyRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $materialFamilyTarget) -or (Test-Path "$materialFamilyTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $materialFamilyHead=(git -C $materialFamilyRoot rev-parse HEAD).Trim()
    if($materialFamilyHead -ne 'd4a3ecbab81759159633e27d65533c67d1a30f27'){throw 'Before capture requires the recorded baseline HEAD.'}
    $materialFamilyDirty=git -C $materialFamilyRoot status --porcelain --untracked-files=all -- Assets
    if($materialFamilyDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$materialFamilyRoot/TestResults/foundry-material-family-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryMaterialFamilyCapture.cs" -Destination $materialFamilyTarget
    Set-Content -LiteralPath "$materialFamilyTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $materialFamilyResults){Remove-Item -LiteralPath $materialFamilyResults}
    $materialFamilyProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$materialFamilyRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryMaterialFamilyCapture','-testResults',"`"$materialFamilyResults`"",'-logFile',"`"$materialFamilyRoot/TestResults/foundry-material-family.log`"") -PassThru -WindowStyle Hidden
    $materialFamilyProcess.WaitForExit()
    if($materialFamilyProcess.ExitCode -ne 0 -or !(Test-Path $materialFamilyResults)){throw 'Layout capture failed; see TestResults/foundry-material-family.log.'}
    $materialFamilyRun=([xml](Get-Content -LiteralPath $materialFamilyResults)).'test-run'
    if($materialFamilyRun.result -ne 'Passed'){throw "Layout capture failed: $($materialFamilyRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($materialFamilyFile in Get-ChildItem "$materialFamilyRoot/ArtDirection/2026-10-14/foundry-material-family-01/$Phase" -Filter '*.bmp'){
        $materialFamilyBitmap=[System.Drawing.Bitmap]::new($materialFamilyFile.FullName)
        try{$materialFamilyBitmap.Save([IO.Path]::ChangeExtension($materialFamilyFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$materialFamilyBitmap.Dispose()}
        Remove-Item -LiteralPath $materialFamilyFile.FullName
    }
    Write-Output "Foundry material family actual Unity capture: $($materialFamilyRun.passed) passed."
} finally {
    if(Test-Path $materialFamilyTarget){Remove-Item -LiteralPath $materialFamilyTarget}
    if(Test-Path "$materialFamilyTarget.meta"){Remove-Item -LiteralPath "$materialFamilyTarget.meta"}
}
