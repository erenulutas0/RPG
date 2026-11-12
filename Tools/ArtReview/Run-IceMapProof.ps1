#requires -Version 7
param([string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$iceRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$iceTarget=Join-Path $iceRoot 'Assets/_Project/Tests/PlayMode/IceMapCapture.cs'
$iceResults=Join-Path $iceRoot 'TestResults/ice-map.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$iceRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $iceTarget) -or (Test-Path "$iceTarget.meta")){throw 'Temporary ice capture exists; inspect first.'}
New-Item -ItemType Directory -Force "$iceRoot/TestResults" | Out-Null
try {
    Copy-Item -LiteralPath "$PSScriptRoot/IceMapCapture.cs" -Destination $iceTarget
    Set-Content -LiteralPath "$iceTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $iceResults){Remove-Item -LiteralPath $iceResults}
    $iceProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$iceRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','IceMapCapture','-testResults',"`"$iceResults`"",'-logFile',"`"$iceRoot/TestResults/ice-map.log`"") -PassThru -WindowStyle Hidden
    $iceProcess.WaitForExit()
    if(!(Test-Path $iceResults)){throw 'Ice capture failed; see TestResults/ice-map.log.'}
    $iceRun=([xml](Get-Content -LiteralPath $iceResults)).'test-run'
    if($iceRun.result -ne 'Passed'){throw "Ice capture failed: $($iceRun.result); see TestResults/ice-map.log."}
    Add-Type -AssemblyName System.Drawing
    foreach($iceFile in Get-ChildItem "$iceRoot/ArtDirection/2026-11-12/ice-map-01" -Recurse -Filter '*.bmp'){
        $iceBitmap=[System.Drawing.Bitmap]::new($iceFile.FullName)
        try{$iceBitmap.Save([IO.Path]::ChangeExtension($iceFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$iceBitmap.Dispose()}
        Remove-Item -LiteralPath $iceFile.FullName
    }
    Write-Output "Ice map capture: $($iceRun.passed) passed. Output: $iceRoot/ArtDirection/2026-11-12/ice-map-01/{floor1,floor2}"
} finally {
    if(Test-Path $iceTarget){Remove-Item -LiteralPath $iceTarget}
    if(Test-Path "$iceTarget.meta"){Remove-Item -LiteralPath "$iceTarget.meta"}
}
