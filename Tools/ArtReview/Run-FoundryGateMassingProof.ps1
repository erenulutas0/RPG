#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$gateMassingRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateMassingTarget=Join-Path $gateMassingRoot 'Assets/_Project/Tests/PlayMode/FoundryGateMassingCapture.cs'
$gateMassingResults=Join-Path $gateMassingRoot 'TestResults/foundry-gate-massing.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gateMassingRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $gateMassingTarget) -or (Test-Path "$gateMassingTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $gateMassingHead=(git -C $gateMassingRoot rev-parse HEAD).Trim()
    if($gateMassingHead -ne '0e44e8fe3c04a53ec958556131ffa323fb4dede7'){throw 'Before capture requires the recorded baseline HEAD.'}
    $gateMassingDirty=git -C $gateMassingRoot status --porcelain --untracked-files=all -- Assets
    if($gateMassingDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$gateMassingRoot/TestResults/foundry-gate-massing-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryGateMassingCapture.cs" -Destination $gateMassingTarget
    Set-Content -LiteralPath "$gateMassingTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $gateMassingResults){Remove-Item -LiteralPath $gateMassingResults}
    $gateMassingProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$gateMassingRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryGateMassingCapture','-testResults',"`"$gateMassingResults`"",'-logFile',"`"$gateMassingRoot/TestResults/foundry-gate-massing.log`"") -PassThru -WindowStyle Hidden
    $gateMassingProcess.WaitForExit()
    if($gateMassingProcess.ExitCode -ne 0 -or !(Test-Path $gateMassingResults)){throw 'Layout capture failed; see TestResults/foundry-gate-massing.log.'}
    $gateMassingRun=([xml](Get-Content -LiteralPath $gateMassingResults)).'test-run'
    if($gateMassingRun.result -ne 'Passed'){throw "Layout capture failed: $($gateMassingRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($gateMassingFile in Get-ChildItem "$gateMassingRoot/ArtDirection/2026-10-14/foundry-gate-massing-01/$Phase" -Filter '*.bmp'){
        $gateMassingBitmap=[System.Drawing.Bitmap]::new($gateMassingFile.FullName)
        try{$gateMassingBitmap.Save([IO.Path]::ChangeExtension($gateMassingFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$gateMassingBitmap.Dispose()}
        Remove-Item -LiteralPath $gateMassingFile.FullName
    }
    Write-Output "Foundry gate massing actual Unity capture: $($gateMassingRun.passed) passed."
} finally {
    if(Test-Path $gateMassingTarget){Remove-Item -LiteralPath $gateMassingTarget}
    if(Test-Path "$gateMassingTarget.meta"){Remove-Item -LiteralPath "$gateMassingTarget.meta"}
}
