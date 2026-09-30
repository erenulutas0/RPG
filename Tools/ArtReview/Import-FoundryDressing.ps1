#requires -Version 7
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$surfaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$surfaceTemp = Join-Path $surfaceRoot 'Assets/_Project/Scripts/Editor/BuildFoundryDressingAssets.cs'
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$surfaceRoot/Temp/UnityLockfile")) { throw 'Unity is in use.' }
if ((Test-Path $surfaceTemp) -or (Test-Path "$surfaceTemp.meta")) { throw 'Temporary foundry surface importer exists; inspect first.' }
try {
    Copy-Item -LiteralPath "$PSScriptRoot/BuildFoundryDressingAssets.cs" -Destination $surfaceTemp
    Set-Content -LiteralPath "$surfaceTemp.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    $surfaceProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-quit','-projectPath',"`"$surfaceRoot`"",'-executeMethod','Cryptforge.Editor.BuildFoundryDressingAssets.Build','-logFile',"`"$surfaceRoot/TestResults/foundry-dressing-import.log`"") -PassThru -WindowStyle Hidden
    $surfaceProcess.WaitForExit()
    if ($surfaceProcess.ExitCode -ne 0) { throw "Foundry surface import failed: $($surfaceProcess.ExitCode). See TestResults/foundry-dressing-import.log." }
} finally {
    if (Test-Path $surfaceTemp) { Remove-Item -LiteralPath $surfaceTemp }
    if (Test-Path "$surfaceTemp.meta") { Remove-Item -LiteralPath "$surfaceTemp.meta" }
}
