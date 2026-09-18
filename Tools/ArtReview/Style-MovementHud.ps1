#requires -Version 7
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$target = "$root/Assets/_Project/Scripts/Editor/StyleMovementHud.cs"
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$root/Temp/UnityLockfile")) { throw 'Unity is in use.' }
if ((Test-Path $target) -or (Test-Path "$target.meta")) { throw 'Temporary importer already exists.' }
Push-Location $root
try {

    Copy-Item -LiteralPath "$PSScriptRoot/StyleMovementHud.cs" -Destination $target
    Set-Content "$target.meta" "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    $p = Start-Process -FilePath 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath',"`"$root`"",'-executeMethod','Cryptforge.Editor.StyleMovementHud.Apply','-logFile',"`"$root/TestResults/movement-hud-style.log`"") -PassThru -WindowStyle Hidden
    $p.WaitForExit()
    if ($p.ExitCode -ne 0) { throw "Movement HUD styling failed: $($p.ExitCode)" }
} finally {
    if (Test-Path $target) { Remove-Item -LiteralPath $target }
    if (Test-Path "$target.meta") { Remove-Item -LiteralPath "$target.meta" }
    Pop-Location
}
