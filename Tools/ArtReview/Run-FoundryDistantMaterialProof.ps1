#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$distantRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$distantTarget=Join-Path $distantRoot 'Assets/_Project/Tests/PlayMode/FoundryDistantMaterialCapture.cs'
$distantResults=Join-Path $distantRoot 'TestResults/foundry-distant-material.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$distantRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $distantTarget) -or (Test-Path "$distantTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$distantRoot/TestResults/foundry-distant-material-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryDistantMaterialCapture.cs" -Destination $distantTarget
    Set-Content -LiteralPath "$distantTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $distantResults){Remove-Item -LiteralPath $distantResults}
    $distantProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$distantRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryDistantMaterialCapture','-testResults',"`"$distantResults`"",'-logFile',"`"$distantRoot/TestResults/foundry-distant-material.log`"") -PassThru -WindowStyle Hidden
    $distantProcess.WaitForExit()
    if($distantProcess.ExitCode -ne 0 -or !(Test-Path $distantResults)){throw 'Layout capture failed; see TestResults/foundry-distant-material.log.'}
    $distantRun=([xml](Get-Content -LiteralPath $distantResults)).'test-run'
    if($distantRun.result -ne 'Passed'){throw "Layout capture failed: $($distantRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($distantFile in Get-ChildItem "$distantRoot/ArtDirection/2026-10-11/foundry-distant-material-01/$Phase" -Filter '*.bmp'){
        $distantBitmap=[System.Drawing.Bitmap]::new($distantFile.FullName)
        try{$distantBitmap.Save([IO.Path]::ChangeExtension($distantFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$distantBitmap.Dispose()}
        Remove-Item -LiteralPath $distantFile.FullName
    }
    Write-Output "Layout actual Unity capture: $($distantRun.passed) passed."
} finally {
    if(Test-Path $distantTarget){Remove-Item -LiteralPath $distantTarget}
    if(Test-Path "$distantTarget.meta"){Remove-Item -LiteralPath "$distantTarget.meta"}
}
