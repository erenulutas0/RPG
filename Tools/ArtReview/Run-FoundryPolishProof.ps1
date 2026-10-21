#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$polishRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$polishTarget=Join-Path $polishRoot 'Assets/_Project/Tests/PlayMode/FoundryPolishCapture.cs'
$polishResults=Join-Path $polishRoot 'TestResults/foundry-polish.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$polishRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $polishTarget) -or (Test-Path "$polishTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $polishHead=(git -C $polishRoot rev-parse HEAD).Trim()
    if($polishHead -ne 'fd06b2f4a0917007957626fccbbc5be994213f92'){throw 'Before capture requires the recorded baseline HEAD.'}
    $polishDirty=git -C $polishRoot status --porcelain --untracked-files=all -- Assets
    if($polishDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$polishRoot/TestResults/foundry-polish-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryPolishCapture.cs" -Destination $polishTarget
    Set-Content -LiteralPath "$polishTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $polishResults){Remove-Item -LiteralPath $polishResults}
    $polishProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$polishRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryPolishCapture','-testResults',"`"$polishResults`"",'-logFile',"`"$polishRoot/TestResults/foundry-polish.log`"") -PassThru -WindowStyle Hidden
    $polishProcess.WaitForExit()
    if($polishProcess.ExitCode -ne 0 -or !(Test-Path $polishResults)){throw 'Layout capture failed; see TestResults/foundry-polish.log.'}
    $polishRun=([xml](Get-Content -LiteralPath $polishResults)).'test-run'
    if($polishRun.result -ne 'Passed'){throw "Layout capture failed: $($polishRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($polishFile in Get-ChildItem "$polishRoot/ArtDirection/2026-10-21/foundry-polish-01/$Phase" -Filter '*.bmp'){
        $polishBitmap=[System.Drawing.Bitmap]::new($polishFile.FullName)
        try{$polishBitmap.Save([IO.Path]::ChangeExtension($polishFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$polishBitmap.Dispose()}
        Remove-Item -LiteralPath $polishFile.FullName
    }
    Write-Output "Foundry polish actual Unity capture: $($polishRun.passed) passed."
} finally {
    if(Test-Path $polishTarget){Remove-Item -LiteralPath $polishTarget}
    if(Test-Path "$polishTarget.meta"){Remove-Item -LiteralPath "$polishTarget.meta"}
}
