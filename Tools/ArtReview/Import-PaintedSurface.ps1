#requires -Version 7
param([Parameter(Mandatory)][ValidateSet('P1b','P3','P3b','P4','P5','P6')][string]$Surface)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskTemp=Join-Path $taskRoot 'Assets/_Project/Scripts/Editor/BuildPaintedSurfaceAsset.cs'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$taskRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $taskTemp) -or (Test-Path "$taskTemp.meta")){throw 'Temporary importer exists; inspect first.'}
$taskPrevious=$env:CRYPTFORGE_SURFACE
try {
    $env:CRYPTFORGE_SURFACE=$Surface
    Copy-Item "$PSScriptRoot/BuildPaintedSurfaceAsset.cs" $taskTemp
    Set-Content "$taskTemp.meta" "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    $taskProcess=Start-Process 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath',"`"$taskRoot`"",'-executeMethod','Cryptforge.Editor.BuildPaintedSurfaceAsset.Build','-logFile',"`"$taskRoot/TestResults/painted-$Surface-import.log`"") -PassThru -WindowStyle Hidden
    $taskProcess.WaitForExit()
    if($taskProcess.ExitCode -ne 0){throw "Import failed: $($taskProcess.ExitCode)"}
} finally {
    $env:CRYPTFORGE_SURFACE=$taskPrevious
    if(Test-Path $taskTemp){Remove-Item -LiteralPath $taskTemp}
    if(Test-Path "$taskTemp.meta"){Remove-Item -LiteralPath "$taskTemp.meta"}
}
