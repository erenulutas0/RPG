#requires -Version 7
# Reproducible, graphics-enabled art capture. Does not build/install an APK or change authored scene/assets.
param([string]$UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe', [switch]$Keyposes)
$ErrorActionPreference = 'Stop'
$proofRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$proofSource = Join-Path $PSScriptRoot 'CharacterProofCapture.cs'
$proofTarget = Join-Path $proofRoot 'Assets/_Project/Tests/PlayMode/CharacterProofCapture.cs'
$proofMeta = "$proofTarget.meta"
$proofName = if($Keyposes){'hero-keyposes'}else{'character-proof'}
$proofFilter = if($Keyposes){'CharacterProofCapture.RenderHeroEquipmentKeyposes'}else{'CharacterProofCapture.RenderResolutionLoadoutsAndAttachmentMotion'}
$proofResults = Join-Path $proofRoot "TestResults/$proofName.xml"
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path (Join-Path $proofRoot 'Temp/UnityLockfile'))) { throw 'Close the Unity Editor before capture.' }
if ((Test-Path $proofTarget) -or (Test-Path $proofMeta)) { throw 'Temporary capture source/meta already exists; inspect it before retrying.' }
Push-Location $proofRoot
try {
    & (Join-Path $PSScriptRoot 'Convert-CharacterProof.ps1') -Mode Source -Keyposes:$Keyposes
    Copy-Item -LiteralPath $proofSource -Destination $proofTarget
    Set-Content -LiteralPath $proofMeta -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))" -Encoding utf8
    dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Pure tests failed. Restore dependencies separately if needed.' }
    dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Unity compile check failed.' }
    if (Test-Path $proofResults) { Remove-Item -LiteralPath $proofResults }
    $proofProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$proofRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter',$proofFilter,'-testResults',"`"$proofResults`"",'-logFile',"`"$(Join-Path $proofRoot "TestResults/$proofName.log")`"") -PassThru -WindowStyle Hidden
    $proofProcess.WaitForExit()
    if ($proofProcess.ExitCode -ne 0 -or !(Test-Path $proofResults)) { throw "Unity capture did not finish successfully; inspect TestResults/$proofName.log." }
    $proofRun = ([xml](Get-Content -LiteralPath $proofResults)).'test-run'
    if ($proofRun.result -ne 'Passed') { throw "Capture assertions failed: $($proofRun.result)" }
    & (Join-Path $PSScriptRoot 'Convert-CharacterProof.ps1') -Mode Renders -Keyposes:$Keyposes
    Write-Output "$proofName capture: $($proofRun.passed) passed."
} finally {
    # Exact files created by this script; no recursive deletion or derived directory cleanup.
    if (Test-Path $proofTarget) { Remove-Item -LiteralPath $proofTarget }
    if (Test-Path $proofMeta) { Remove-Item -LiteralPath $proofMeta }
    Pop-Location
}
