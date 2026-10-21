#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$doorwayRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$doorwayTarget=Join-Path $doorwayRoot 'Assets/_Project/Tests/PlayMode/FoundryDoorwayCapture.cs'
$doorwayResults=Join-Path $doorwayRoot 'TestResults/foundry-doorway.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$doorwayRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $doorwayTarget) -or (Test-Path "$doorwayTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $doorwayHead=(git -C $doorwayRoot rev-parse HEAD).Trim()
    if($doorwayHead -ne '92a8901647eb0df96228b26046cde96a0dcd270c'){throw 'Before capture requires the recorded baseline HEAD.'}
    $doorwayDirty=git -C $doorwayRoot status --porcelain --untracked-files=all -- Assets
    if($doorwayDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$doorwayRoot/TestResults/foundry-doorway-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryDoorwayCapture.cs" -Destination $doorwayTarget
    Set-Content -LiteralPath "$doorwayTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $doorwayResults){Remove-Item -LiteralPath $doorwayResults}
    $doorwayProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$doorwayRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryDoorwayCapture','-testResults',"`"$doorwayResults`"",'-logFile',"`"$doorwayRoot/TestResults/foundry-doorway.log`"") -PassThru -WindowStyle Hidden
    $doorwayProcess.WaitForExit()
    if($doorwayProcess.ExitCode -ne 0 -or !(Test-Path $doorwayResults)){throw 'Layout capture failed; see TestResults/foundry-doorway.log.'}
    $doorwayRun=([xml](Get-Content -LiteralPath $doorwayResults)).'test-run'
    if($doorwayRun.result -ne 'Passed'){throw "Layout capture failed: $($doorwayRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($doorwayFile in Get-ChildItem "$doorwayRoot/ArtDirection/2026-10-21/foundry-doorway-01/$Phase" -Filter '*.bmp'){
        $doorwayBitmap=[System.Drawing.Bitmap]::new($doorwayFile.FullName)
        try{$doorwayBitmap.Save([IO.Path]::ChangeExtension($doorwayFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$doorwayBitmap.Dispose()}
        Remove-Item -LiteralPath $doorwayFile.FullName
    }
    Write-Output "Foundry doorway actual Unity capture: $($doorwayRun.passed) passed."
} finally {
    if(Test-Path $doorwayTarget){Remove-Item -LiteralPath $doorwayTarget}
    if(Test-Path "$doorwayTarget.meta"){Remove-Item -LiteralPath "$doorwayTarget.meta"}
}
