#requires -Version 7
param([string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$gloamRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gloamTarget=Join-Path $gloamRoot 'Assets/_Project/Tests/PlayMode/GloamMapCapture.cs'
$gloamResults=Join-Path $gloamRoot 'TestResults/gloam-map.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gloamRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $gloamTarget) -or (Test-Path "$gloamTarget.meta")){throw 'Temporary gloam capture exists; inspect first.'}
New-Item -ItemType Directory -Force "$gloamRoot/TestResults" | Out-Null
try {
    Copy-Item -LiteralPath "$PSScriptRoot/GloamMapCapture.cs" -Destination $gloamTarget
    Set-Content -LiteralPath "$gloamTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $gloamResults){Remove-Item -LiteralPath $gloamResults}
    $gloamProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$gloamRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','GloamMapCapture','-testResults',"`"$gloamResults`"",'-logFile',"`"$gloamRoot/TestResults/gloam-map.log`"") -PassThru -WindowStyle Hidden
    $gloamProcess.WaitForExit()
    if(!(Test-Path $gloamResults)){throw 'Gloam capture failed; see TestResults/gloam-map.log.'}
    $gloamRun=([xml](Get-Content -LiteralPath $gloamResults)).'test-run'
    if($gloamRun.result -ne 'Passed'){throw "Gloam capture failed: $($gloamRun.result); see TestResults/gloam-map.log."}
    Add-Type -AssemblyName System.Drawing
    foreach($gloamFile in Get-ChildItem "$gloamRoot/ArtDirection/2026-11-12/gloam-map-01" -Recurse -Filter '*.bmp'){
        $gloamBitmap=[System.Drawing.Bitmap]::new($gloamFile.FullName)
        try{$gloamBitmap.Save([IO.Path]::ChangeExtension($gloamFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$gloamBitmap.Dispose()}
        Remove-Item -LiteralPath $gloamFile.FullName
    }
    Write-Output "Gloam map capture: $($gloamRun.passed) passed. Output: $gloamRoot/ArtDirection/2026-11-12/gloam-map-01/{floor1,floor2,floor3}"
} finally {
    if(Test-Path $gloamTarget){Remove-Item -LiteralPath $gloamTarget}
    if(Test-Path "$gloamTarget.meta"){Remove-Item -LiteralPath "$gloamTarget.meta"}
}
