#requires -Version 7
# Temporary graphics fixture only. Never saves a scene or builds/installs an APK.
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path.Replace('\','/')
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$proofRoot/Temp/UnityLockfile")) { throw 'Close Unity before capture.' }
$captureTarget = "$proofRoot/Assets/_Project/Tests/PlayMode/PlatformMaterialCapture.cs"
if ((Test-Path $captureTarget) -or (Test-Path "$captureTarget.meta")) { throw 'Temporary capture source already exists.' }
Push-Location $proofRoot
try {
    & "$PSScriptRoot/Convert-CharacterProof.ps1" -Mode Source -PlatformMaterial
    Copy-Item -LiteralPath "$PSScriptRoot/PlatformMaterialCapture.cs" -Destination $captureTarget
    Set-Content -LiteralPath "$captureTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
    try {
        dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed.' }
        dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Compile failed.' }
        $result = "$proofRoot/TestResults/platform-material-capture.xml"
        if (Test-Path $result) { Remove-Item -LiteralPath $result }
        $process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','PlatformMaterialCapture','-testResults',"`"$result`"",'-logFile',"`"$proofRoot/TestResults/platform-material-capture.log`"") -PassThru -WindowStyle Hidden
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "Unity capture exit $($process.ExitCode)." }
        $run = ([xml](Get-Content -LiteralPath $result)).'test-run'
        if ($run.result -ne 'Passed') { throw 'Platform capture failed; inspect its XML/log.' }
        & "$PSScriptRoot/Convert-CharacterProof.ps1" -Mode Renders -PlatformMaterial
        Write-Output "Platform graphics proof: $($run.passed) passed. No runtime imports, APK or device changes."
    } finally { Remove-Item -LiteralPath $captureTarget,"$captureTarget.meta" }
} finally { Pop-Location }
