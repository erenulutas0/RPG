#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$postBrassRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$postBrassTarget=Join-Path $postBrassRoot 'Assets/_Project/Tests/PlayMode/FoundryPostBrassCapture.cs'
$postBrassResults=Join-Path $postBrassRoot 'TestResults/foundry-post-brass.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$postBrassRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $postBrassTarget) -or (Test-Path "$postBrassTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $postBrassHead=(git -C $postBrassRoot rev-parse HEAD).Trim()
    if($postBrassHead -ne '3ab2d074ac8622920aadd867518f91ed6d4c20e5'){throw 'Before capture requires the recorded baseline HEAD.'}
    $postBrassDirty=git -C $postBrassRoot status --porcelain --untracked-files=all -- Assets
    if($postBrassDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$postBrassRoot/TestResults/foundry-post-brass-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryPostBrassCapture.cs" -Destination $postBrassTarget
    Set-Content -LiteralPath "$postBrassTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $postBrassResults){Remove-Item -LiteralPath $postBrassResults}
    $postBrassProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$postBrassRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryPostBrassCapture','-testResults',"`"$postBrassResults`"",'-logFile',"`"$postBrassRoot/TestResults/foundry-post-brass.log`"") -PassThru -WindowStyle Hidden
    $postBrassProcess.WaitForExit()
    if($postBrassProcess.ExitCode -ne 0 -or !(Test-Path $postBrassResults)){throw 'Layout capture failed; see TestResults/foundry-post-brass.log.'}
    $postBrassRun=([xml](Get-Content -LiteralPath $postBrassResults)).'test-run'
    if($postBrassRun.result -ne 'Passed'){throw "Layout capture failed: $($postBrassRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($postBrassFile in Get-ChildItem "$postBrassRoot/ArtDirection/2026-10-21/foundry-post-brass-01/$Phase" -Filter '*.bmp'){
        $postBrassBitmap=[System.Drawing.Bitmap]::new($postBrassFile.FullName)
        try{$postBrassBitmap.Save([IO.Path]::ChangeExtension($postBrassFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$postBrassBitmap.Dispose()}
        Remove-Item -LiteralPath $postBrassFile.FullName
    }
    Write-Output "Foundry post brass actual Unity capture: $($postBrassRun.passed) passed."
} finally {
    if(Test-Path $postBrassTarget){Remove-Item -LiteralPath $postBrassTarget}
    if(Test-Path "$postBrassTarget.meta"){Remove-Item -LiteralPath "$postBrassTarget.meta"}
}
