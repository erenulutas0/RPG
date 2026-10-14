#requires -Version 7
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$gateMasonryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateMasonryTemp = Join-Path $gateMasonryRoot 'Assets/_Project/Scripts/Editor/BuildGateMasonryAsset.cs'
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$gateMasonryRoot/Temp/UnityLockfile")) { throw 'Unity is in use.' }
if ((Test-Path $gateMasonryTemp) -or (Test-Path "$gateMasonryTemp.meta")) { throw 'Temporary gateMasonry importer exists; inspect first.' }
try {
    Copy-Item -LiteralPath "$PSScriptRoot/BuildGateMasonryAsset.cs" -Destination $gateMasonryTemp
    Set-Content -LiteralPath "$gateMasonryTemp.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    $gateMasonryProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-quit','-buildTarget','Android','-projectPath',"`"$gateMasonryRoot`"",'-executeMethod','Cryptforge.Editor.BuildGateMasonryAsset.Build','-logFile',"`"$gateMasonryRoot/TestResults/gate-masonry-import.log`"") -PassThru -WindowStyle Hidden
    $gateMasonryProcess.WaitForExit()
    if ($gateMasonryProcess.ExitCode -ne 0) { throw "Gate masonry import failed: $($gateMasonryProcess.ExitCode). See TestResults/gate-masonry-import.log." }
} finally {
    if (Test-Path $gateMasonryTemp) { Remove-Item -LiteralPath $gateMasonryTemp }
    if (Test-Path "$gateMasonryTemp.meta") { Remove-Item -LiteralPath "$gateMasonryTemp.meta" }
}
