#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$altarsRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$altarsTarget=Join-Path $altarsRoot 'Assets/_Project/Tests/PlayMode/FoundryAltarsCapture.cs'
$altarsResults=Join-Path $altarsRoot 'TestResults/foundry-altars.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$altarsRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $altarsTarget) -or (Test-Path "$altarsTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
Set-Content "$altarsRoot/TestResults/foundry-altars-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryAltarsCapture.cs" -Destination $altarsTarget
    Set-Content -LiteralPath "$altarsTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $altarsResults){Remove-Item -LiteralPath $altarsResults}
    $altarsProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$altarsRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryAltarsCapture','-testResults',"`"$altarsResults`"",'-logFile',"`"$altarsRoot/TestResults/foundry-altars.log`"") -PassThru -WindowStyle Hidden
    $altarsProcess.WaitForExit()
    if($altarsProcess.ExitCode -ne 0 -or !(Test-Path $altarsResults)){throw 'Layout capture failed; see TestResults/foundry-altars.log.'}
    $altarsRun=([xml](Get-Content -LiteralPath $altarsResults)).'test-run'
    if($altarsRun.result -ne 'Passed'){throw "Layout capture failed: $($altarsRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($altarsFile in Get-ChildItem "$altarsRoot/ArtDirection/2026-10-23/foundry-altars-01" -Filter '*.bmp'){
        $altarsBitmap=[System.Drawing.Bitmap]::new($altarsFile.FullName)
        try{$altarsBitmap.Save([IO.Path]::ChangeExtension($altarsFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$altarsBitmap.Dispose()}
        Remove-Item -LiteralPath $altarsFile.FullName
    }
    Write-Output "Foundry altars actual Unity capture: $($altarsRun.passed) passed."
} finally {
    if(Test-Path $altarsTarget){Remove-Item -LiteralPath $altarsTarget}
    if(Test-Path "$altarsTarget.meta"){Remove-Item -LiteralPath "$altarsTarget.meta"}
}
