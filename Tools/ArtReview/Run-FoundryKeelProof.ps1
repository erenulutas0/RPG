#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$keelRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$keelTarget=Join-Path $keelRoot 'Assets/_Project/Tests/PlayMode/FoundryKeelCapture.cs'
$keelResults=Join-Path $keelRoot 'TestResults/foundry-keel.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$keelRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $keelTarget) -or (Test-Path "$keelTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $keelHead=(git -C $keelRoot rev-parse HEAD).Trim()
    if($keelHead -ne '84cbd2112e4058869965a7bdbda777fd83f390ac'){throw 'Before capture requires the recorded baseline HEAD.'}
    $keelDirty=git -C $keelRoot status --porcelain --untracked-files=all -- Assets
    if($keelDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$keelRoot/TestResults/foundry-keel-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryKeelCapture.cs" -Destination $keelTarget
    Set-Content -LiteralPath "$keelTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $keelResults){Remove-Item -LiteralPath $keelResults}
    $keelProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$keelRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryKeelCapture','-testResults',"`"$keelResults`"",'-logFile',"`"$keelRoot/TestResults/foundry-keel.log`"") -PassThru -WindowStyle Hidden
    $keelProcess.WaitForExit()
    if($keelProcess.ExitCode -ne 0 -or !(Test-Path $keelResults)){throw 'Layout capture failed; see TestResults/foundry-keel.log.'}
    $keelRun=([xml](Get-Content -LiteralPath $keelResults)).'test-run'
    if($keelRun.result -ne 'Passed'){throw "Layout capture failed: $($keelRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($keelFile in Get-ChildItem "$keelRoot/ArtDirection/2026-10-22/foundry-keel-01/$Phase" -Filter '*.bmp'){
        $keelBitmap=[System.Drawing.Bitmap]::new($keelFile.FullName)
        try{$keelBitmap.Save([IO.Path]::ChangeExtension($keelFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$keelBitmap.Dispose()}
        Remove-Item -LiteralPath $keelFile.FullName
    }
    Write-Output "Foundry keel actual Unity capture: $($keelRun.passed) passed."
} finally {
    if(Test-Path $keelTarget){Remove-Item -LiteralPath $keelTarget}
    if(Test-Path "$keelTarget.meta"){Remove-Item -LiteralPath "$keelTarget.meta"}
}
