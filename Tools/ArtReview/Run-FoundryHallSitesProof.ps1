#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$hallSitesRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$hallSitesTarget=Join-Path $hallSitesRoot 'Assets/_Project/Tests/PlayMode/FoundryHallSitesCapture.cs'
$hallSitesResults=Join-Path $hallSitesRoot 'TestResults/foundry-hall-sites.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$hallSitesRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $hallSitesTarget) -or (Test-Path "$hallSitesTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$hallSitesRoot/TestResults/foundry-hall-sites-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryHallSitesCapture.cs" -Destination $hallSitesTarget
    Set-Content -LiteralPath "$hallSitesTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $hallSitesResults){Remove-Item -LiteralPath $hallSitesResults}
    $hallSitesProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$hallSitesRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryHallSitesCapture','-testResults',"`"$hallSitesResults`"",'-logFile',"`"$hallSitesRoot/TestResults/foundry-hall-sites.log`"") -PassThru -WindowStyle Hidden
    $hallSitesProcess.WaitForExit()
    if($hallSitesProcess.ExitCode -ne 0 -or !(Test-Path $hallSitesResults)){throw 'Layout capture failed; see TestResults/foundry-hall-sites.log.'}
    $hallSitesRun=([xml](Get-Content -LiteralPath $hallSitesResults)).'test-run'
    if($hallSitesRun.result -ne 'Passed'){throw "Layout capture failed: $($hallSitesRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($hallSitesFile in Get-ChildItem "$hallSitesRoot/ArtDirection/2026-10-23/foundry-hall-sites-01" -Filter '*.bmp'){
        $hallSitesBitmap=[System.Drawing.Bitmap]::new($hallSitesFile.FullName)
        try{$hallSitesBitmap.Save([IO.Path]::ChangeExtension($hallSitesFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$hallSitesBitmap.Dispose()}
        Remove-Item -LiteralPath $hallSitesFile.FullName
    }
    Write-Output "Foundry hall sites actual Unity capture: $($hallSitesRun.passed) passed."
} finally {
    if(Test-Path $hallSitesTarget){Remove-Item -LiteralPath $hallSitesTarget}
    if(Test-Path "$hallSitesTarget.meta"){Remove-Item -LiteralPath "$hallSitesTarget.meta"}
}
