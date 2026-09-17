#requires -Version 7
# Capture the real enemy prefab and pooled strikes. -Reimport rebuilds the shared sets from retained masters.
param([switch]$Reimport, [string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path.Replace('\','/')
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$proofRoot/Temp/UnityLockfile")) { throw 'Close the Unity Editor before capture.' }
function Invoke-GruntUnity([string[]]$Arguments) {
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $Arguments -PassThru -WindowStyle Hidden
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity failed: $($process.ExitCode). Inspect TestResults/grunt-*.log." }
}
function Install-TemporarySource([string]$Source, [string]$Target) {
    if ((Test-Path $Target) -or (Test-Path "$Target.meta")) { throw "Temporary source already exists: $Target" }
    Copy-Item -LiteralPath $Source -Destination $Target
    Set-Content -LiteralPath "$Target.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
}
Push-Location $proofRoot
try {
    if ($Reimport) {
        & "$PSScriptRoot/Convert-CharacterProof.ps1" -Mode Source -GruntPolish
        $importTarget = "$proofRoot/Assets/_Project/Scripts/Editor/BuildGruntAssets.cs"
        Install-TemporarySource "$PSScriptRoot/BuildGruntAssets.cs" $importTarget
        try {
            Invoke-GruntUnity @('-batchmode','-quit','-projectPath',"`"$proofRoot`"",'-executeMethod','Cryptforge.Editor.BuildGruntAssets.Build','-logFile',"`"$proofRoot/TestResults/grunt-import.log`"")
        } finally { Remove-Item -LiteralPath $importTarget,"$importTarget.meta" }
    }
    $captureClass = 'GruntRuntimeCapture'
    $captureFolder = 'grunt-polish-01'
    $captureTarget = "$proofRoot/Assets/_Project/Tests/PlayMode/$captureClass.cs"
    Install-TemporarySource "$PSScriptRoot/$captureClass.cs" $captureTarget
    try {
        dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed.' }
        dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Compile failed.' }
        $result = "$proofRoot/TestResults/grunt-capture.xml"
        if (Test-Path $result) { Remove-Item -LiteralPath $result }
        Invoke-GruntUnity @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter',$captureClass,'-testResults',"`"$result`"",'-logFile',"`"$proofRoot/TestResults/grunt-capture.log`"")
        $run = ([xml](Get-Content -LiteralPath $result)).'test-run'
        if ($run.result -ne 'Passed') { throw 'Grunt capture assertions failed.' }
        Add-Type -AssemblyName System.Drawing
        Get-ChildItem -LiteralPath "$proofRoot/ArtDirection/2026-09-17/$captureFolder" -Filter '*.bmp' | ForEach-Object {
            $capture = [System.Drawing.Image]::FromFile($_.FullName)
            try { $capture.Save([System.IO.Path]::ChangeExtension($_.FullName,'.png'),[System.Drawing.Imaging.ImageFormat]::Png) }
            finally { $capture.Dispose() }
            Remove-Item -LiteralPath $_.FullName
        }
        Write-Output "Runtime capture: $($run.passed) passed. No APK built or installed."
    } finally { Remove-Item -LiteralPath $captureTarget,"$captureTarget.meta" }
} finally { Pop-Location }
