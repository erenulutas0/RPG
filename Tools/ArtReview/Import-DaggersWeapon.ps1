#requires -Version 7
# Technical fixed-registration import; preserves source PNG and existing asset GUIDs.
param([string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$staffRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$staffTemp=Join-Path $staffRoot 'Assets/_Project/Scripts/Editor/BuildDaggerAsset.cs'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$staffRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $staffTemp) -or (Test-Path "$staffTemp.meta")){throw 'Temporary importer already exists; inspect first.'}
Push-Location $staffRoot
try {
    & "$PSScriptRoot/Convert-CharacterProof.ps1" -Mode Source -DaggersWeapon
    Copy-Item -LiteralPath "$PSScriptRoot/BuildDaggerAsset.cs" -Destination $staffTemp
    Set-Content -LiteralPath "$staffTemp.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
    if($LASTEXITCODE -ne 0){throw 'Compile failed.'}
    $staffProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-quit','-projectPath',"`"$staffRoot`"",'-executeMethod','Cryptforge.Editor.BuildDaggerAsset.Build','-logFile',"`"$staffRoot/TestResults/daggers-import.log`"") -PassThru -WindowStyle Hidden
    $staffProcess.WaitForExit()
    if($staffProcess.ExitCode -ne 0){throw 'Staff import failed; inspect log.'}
} finally {
    if(Test-Path $staffTemp){Remove-Item -LiteralPath $staffTemp}
    if(Test-Path "$staffTemp.meta"){Remove-Item -LiteralPath "$staffTemp.meta"}
    Pop-Location
}