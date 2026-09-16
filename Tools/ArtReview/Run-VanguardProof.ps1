#requires -Version 7
# Capture the actual imported runtime hero. -Reimport first rebuilds the existing asset set from retained masters.
param([switch]$Reimport, [string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path.Replace('\','/')
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$proofRoot/Temp/UnityLockfile")) { throw 'Close the Unity Editor before capture.' }
function Invoke-VanguardUnity([string[]]$Arguments) {
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $Arguments -PassThru -WindowStyle Hidden
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity failed: $($process.ExitCode). Inspect TestResults/vanguard-*.log." }
}
function Install-TemporarySource([string]$Source, [string]$Target) {
    if ((Test-Path $Target) -or (Test-Path "$Target.meta")) { throw "Temporary source already exists: $Target" }
    Copy-Item -LiteralPath $Source -Destination $Target
    Set-Content -LiteralPath "$Target.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
}
Push-Location $proofRoot
try {
    if ($Reimport) {
        & "$PSScriptRoot/Convert-CharacterProof.ps1" -Mode Source -Keyposes -Integration
        $importTarget = "$proofRoot/Assets/_Project/Scripts/Editor/BuildVanguardAssets.cs"
        Install-TemporarySource "$PSScriptRoot/BuildVanguardAssets.cs" $importTarget
        try {
            Invoke-VanguardUnity @('-batchmode','-quit','-projectPath',"`"$proofRoot`"",'-executeMethod','Cryptforge.Editor.BuildVanguardAssets.Build','-logFile',"`"$proofRoot/TestResults/vanguard-import.log`"")
        } finally { Remove-Item -LiteralPath $importTarget,"$importTarget.meta" }
    }
    $captureTarget = "$proofRoot/Assets/_Project/Tests/PlayMode/VanguardRuntimeCapture.cs"
    Install-TemporarySource "$PSScriptRoot/VanguardRuntimeCapture.cs" $captureTarget
    try {
        dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed.' }
        dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Compile failed.' }
        $result = "$proofRoot/TestResults/vanguard-capture.xml"
        if (Test-Path $result) { Remove-Item -LiteralPath $result }
        Invoke-VanguardUnity @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','VanguardRuntimeCapture','-testResults',"`"$result`"",'-logFile',"`"$proofRoot/TestResults/vanguard-capture.log`"")
        $run = ([xml](Get-Content -LiteralPath $result)).'test-run'
        if ($run.result -ne 'Passed') { throw 'Vanguard capture assertions failed.' }
        Add-Type -AssemblyName System.Drawing
        Get-ChildItem -LiteralPath "$proofRoot/ArtDirection/2026-09-17/vanguard-runtime-01" -Filter '*.bmp' | ForEach-Object {
            $capture = [System.Drawing.Image]::FromFile($_.FullName)
            try { $capture.Save([System.IO.Path]::ChangeExtension($_.FullName,'.png'),[System.Drawing.Imaging.ImageFormat]::Png) }
            finally { $capture.Dispose() }
            Remove-Item -LiteralPath $_.FullName
        }
        Write-Output "Runtime capture: $($run.passed) passed. No APK built or installed."
    } finally { Remove-Item -LiteralPath $captureTarget,"$captureTarget.meta" }
} finally { Pop-Location }
