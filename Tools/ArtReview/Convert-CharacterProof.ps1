# Lossless format bridge only: unchanged PNG channels -> Unity RGBA staging; rendered BMP -> PNG evidence.
# No resizing, painting, compositing, alpha replacement or colour grading happens here.
param([ValidateSet('Source','Renders')][string]$Mode = 'Source')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$conversionRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if ($Mode -eq 'Source') {
    $rawFolder = Join-Path $conversionRoot 'TestResults/character-proof-source'
    New-Item -ItemType Directory -Force -Path $rawFolder | Out-Null
    foreach ($name in @('vanguard-body-v2.png','cinder-mite-v1.png')) {
        $bitmap = [System.Drawing.Bitmap]::new((Join-Path $conversionRoot "ArtDirection/2026-09-16/character-proof-01/$name"))
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
    $renders = Join-Path $conversionRoot 'ArtDirection/2026-09-16/character-unity-01'
    foreach ($file in Get-ChildItem -LiteralPath $renders -Filter '*.bmp' -File) {
        $bitmap = [System.Drawing.Bitmap]::new($file.FullName)
        try { $bitmap.Save([System.IO.Path]::ChangeExtension($file.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png) }
        finally { $bitmap.Dispose() }
        Remove-Item -LiteralPath $file.FullName
    }
    Write-Output 'Encoded Unity RGB renders as PNG without resizing.'
}
