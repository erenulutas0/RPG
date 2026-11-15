#requires -Version 7
# Thorn Post's Cinder Bloom at its maul's reach (docs/52, the lead's decisions 2026-11-15): runs ThornPostCapture in the
# Editor's PlayMode (copied into Tests/PlayMode for the run and removed after), then converts its BMPs to PNG. Output:
# ArtDirection/2026-11-15/thorn-post-01/ - <side>-<closed|opening|open>-<close|portrait>.png and overlap.txt.
param([string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$thornRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$thornTarget=Join-Path $thornRoot 'Assets/_Project/Tests/PlayMode/ThornPostCapture.cs'
$thornResults=Join-Path $thornRoot 'TestResults/thorn-post.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$thornRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $thornTarget) -or (Test-Path "$thornTarget.meta")){throw 'Temporary Thorn Post capture exists; inspect first.'}
New-Item -ItemType Directory -Force "$thornRoot/TestResults" | Out-Null
try {
    Copy-Item -LiteralPath "$PSScriptRoot/ThornPostCapture.cs" -Destination $thornTarget
    Set-Content -LiteralPath "$thornTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $thornResults){Remove-Item -LiteralPath $thornResults}
    $thornProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$thornRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','ThornPostCapture','-testResults',"`"$thornResults`"",'-logFile',"`"$thornRoot/TestResults/thorn-post.log`"") -PassThru -WindowStyle Hidden
    $thornProcess.WaitForExit()
    if(!(Test-Path $thornResults)){throw 'Thorn Post capture failed; see TestResults/thorn-post.log.'}
    $thornRun=([xml](Get-Content -LiteralPath $thornResults)).'test-run'
    if($thornRun.result -ne 'Passed'){throw "Thorn Post capture failed: $($thornRun.result); see TestResults/thorn-post.log."}
    Add-Type -AssemblyName System.Drawing
    foreach($thornFile in Get-ChildItem "$thornRoot/ArtDirection/2026-11-15/thorn-post-01" -Filter '*.bmp'){
        $thornBitmap=[System.Drawing.Bitmap]::new($thornFile.FullName)
        try{$thornBitmap.Save([IO.Path]::ChangeExtension($thornFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$thornBitmap.Dispose()}
        Remove-Item -LiteralPath $thornFile.FullName
    }
    Write-Output "Thorn Post capture: $($thornRun.passed) passed. Output: $thornRoot/ArtDirection/2026-11-15/thorn-post-01 (overlap.txt beside the images)"
} finally {
    if(Test-Path $thornTarget){Remove-Item -LiteralPath $thornTarget}
    if(Test-Path "$thornTarget.meta"){Remove-Item -LiteralPath "$thornTarget.meta"}
}
