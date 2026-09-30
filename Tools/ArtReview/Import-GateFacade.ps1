#requires -Version 7
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$rewardRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$rewardTemp = Join-Path $rewardRoot 'Assets/_Project/Scripts/Editor/BuildGateFacadeAsset.cs'
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$rewardRoot/Temp/UnityLockfile")) { throw 'Unity is in use.' }
if ((Test-Path $rewardTemp) -or (Test-Path "$rewardTemp.meta")) { throw 'Temporary reward importer exists; inspect first.' }
try {
    Copy-Item -LiteralPath "$PSScriptRoot/BuildGateFacadeAsset.cs" -Destination $rewardTemp
    Set-Content -LiteralPath "$rewardTemp.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    $rewardProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-quit','-projectPath',"`"$rewardRoot`"",'-executeMethod','Cryptforge.Editor.BuildGateFacadeAsset.Build','-logFile',"`"$rewardRoot/TestResults/gate-facade-import.log`"") -PassThru -WindowStyle Hidden
    $rewardProcess.WaitForExit()
    if ($rewardProcess.ExitCode -ne 0) { throw "Reward import failed: $($rewardProcess.ExitCode). See TestResults/gate-facade-import.log." }
} finally {
    if (Test-Path $rewardTemp) { Remove-Item -LiteralPath $rewardTemp }
    if (Test-Path "$rewardTemp.meta") { Remove-Item -LiteralPath "$rewardTemp.meta" }
}


