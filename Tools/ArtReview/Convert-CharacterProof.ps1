# Lossless format bridge only: unchanged PNG channels -> Unity RGBA staging; rendered BMP -> PNG evidence.
# No resizing, painting, compositing, alpha replacement or colour grading happens here.
param([ValidateSet('Source','Renders')][string]$Mode = 'Source', [switch]$Keyposes, [switch]$Integration, [switch]$Directions, [switch]$Mite, [switch]$GruntMotion, [switch]$GruntPolish, [switch]$PlatformMaterial, [switch]$Chest, [switch]$ChestRuntime, [switch]$MovementHud, [switch]$AbilityRing, [switch]$AbilityRingRuntime, [switch]$AbilityPulse, [switch]$StaffSplash)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$conversionRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if ($Mode -eq 'Source') {
    $rawFolder = Join-Path $conversionRoot $(if($GruntMotion){'TestResults/grunt-motion-source'}else{'TestResults/character-proof-source'})
    if ($GruntPolish) { $rawFolder = Join-Path $conversionRoot 'TestResults/grunt-polish-source' }
    if ($PlatformMaterial) { $rawFolder = Join-Path $conversionRoot 'TestResults/platform-material-source' }
    New-Item -ItemType Directory -Force -Path $rawFolder | Out-Null
    $sourceFiles = @('character-proof-01/vanguard-body-v2.png','character-proof-01/cinder-mite-v1.png')
    if ($Keyposes) { $sourceFiles += @('hero-motion-01/sword-v1.png','hero-motion-01/shield-v1.png','hero-motion-01/walk-a-v1.png','hero-motion-01/walk-b-v2.png','hero-motion-01/attack-windup-v1.png') }
    if ($Integration) { $sourceFiles += @('../2026-09-17/vanguard-runtime-01/pass-a-v1.png','../2026-09-17/vanguard-runtime-01/pass-b-v1.png','../2026-09-17/vanguard-runtime-01/strike-v1.png','../2026-09-17/vanguard-runtime-01/recover-v1.png') }
    if ($Directions) { $sourceFiles += Get-ChildItem -LiteralPath "$conversionRoot/ArtDirection/2026-09-17/vanguard-directions-01" -Filter '*.png' | Where-Object { $_.Name -match '^(front-|rear-passing-)' } | ForEach-Object { '../2026-09-17/vanguard-directions-01/' + $_.Name } }
    if ($Mite) { $sourceFiles += Get-ChildItem -LiteralPath "$conversionRoot/ArtDirection/2026-09-17/mite-runtime-01" -Filter '*.png' | Where-Object { $_.Name -match '^(mite-|slash-sheet|spark-sheet)' } | ForEach-Object { '../2026-09-17/mite-runtime-01/' + $_.Name } }
    if ($GruntMotion) { $sourceFiles = @('../2026-09-17/grunt-motion-01/front-sheet-v2.png','../2026-09-17/grunt-motion-01/rear-sheet-v1.png') }
    if ($GruntPolish) { $sourceFiles = @('../2026-09-17/grunt-motion-01/front-sheet-v2.png','../2026-09-17/grunt-polish-01/rear-sheet-v2.png','../2026-09-17/grunt-polish-01/passing-v2.png','../2026-09-17/grunt-polish-01/death-v1.png') }
    if ($PlatformMaterial) { $sourceFiles = @('../2026-09-17/platform-material-01/floor-v1.png','../2026-09-17/platform-material-01/floor-v2.png','../2026-09-17/platform-material-01/coping-v1.png','../2026-09-17/platform-material-01/wall-v1.png') }
    if ($Chest) { $rawFolder = Join-Path $conversionRoot 'TestResults/chest-source'; New-Item -ItemType Directory -Force -Path $rawFolder | Out-Null; $sourceFiles = @('../2026-09-18/chest-proof-01/chest-closed-v1.png','../2026-09-18/chest-proof-01/chest-opening-v1.png','../2026-09-18/chest-proof-01/chest-open-v1.png') }
    foreach ($relative in $sourceFiles) {
        $name = Split-Path $relative -Leaf
        $bitmap = [System.Drawing.Bitmap]::new((Join-Path $conversionRoot "ArtDirection/2026-09-16/$relative"))
        $stream = [System.IO.File]::Create((Join-Path $rawFolder "$name.rgba"))
        $writer = [System.IO.BinaryWriter]::new($stream)
        try {
            $writer.Write([int]$bitmap.Width); $writer.Write([int]$bitmap.Height)
            $rect = [System.Drawing.Rectangle]::new(0,0,$bitmap.Width,$bitmap.Height)
            $bits = $bitmap.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                $row = [byte[]]::new($bitmap.Width*4)
                for ($rowY=$bitmap.Height-1;$rowY -ge 0;$rowY--) {
                    [System.Runtime.InteropServices.Marshal]::Copy([IntPtr]::Add($bits.Scan0,$rowY*$bits.Stride),$row,0,$row.Length)
                    for ($col=0;$col -lt $row.Length;$col+=4) { $channel=$row[$col]; $row[$col]=$row[$col+2]; $row[$col+2]=$channel }
                    $writer.Write($row)
                }
            } finally { $bitmap.UnlockBits($bits) }
        } finally { $writer.Dispose();$bitmap.Dispose() }
        Write-Output "Staged unchanged RGBA channels: $name"
    }
} else {
    $renders = Join-Path $conversionRoot $(if($Keyposes){'ArtDirection/2026-09-16/hero-motion-01'}else{'ArtDirection/2026-09-16/character-unity-01'})
    if ($GruntMotion) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-17/grunt-motion-01' }
    if ($GruntPolish) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-17/grunt-polish-01' }
    if ($PlatformMaterial) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-17/platform-material-01' }
    if ($Chest) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-18/chest-unity-01' }
    if ($ChestRuntime) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-18/chest-runtime-01' }
    if ($MovementHud) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-19/movement-hud-01' }
    if ($AbilityRing) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-19/ability-ring-01' }
    if ($AbilityRingRuntime) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-19/ability-ring-runtime-01' }
    if ($AbilityPulse) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-19/ability-pulse-01' }
    if ($StaffSplash) { $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-19/staff-splash-01' }
    foreach ($file in Get-ChildItem -LiteralPath $renders -Filter '*.bmp' -File) {
        $bitmap = [System.Drawing.Bitmap]::new($file.FullName)
        try { $bitmap.Save([System.IO.Path]::ChangeExtension($file.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png) }
        finally { $bitmap.Dispose() }
        Remove-Item -LiteralPath $file.FullName
    }
    Write-Output 'Encoded Unity RGB renders as PNG without resizing.'
}
