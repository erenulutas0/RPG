#requires -Version 7
param([ValidateSet('before','after')][string]$Phase='after',[string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$goldNetworkRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$goldNetworkTarget=Join-Path $goldNetworkRoot 'Assets/_Project/Tests/PlayMode/FoundryGoldNetworkCapture.cs'
$goldNetworkResults=Join-Path $goldNetworkRoot 'TestResults/foundry-gold-network.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$goldNetworkRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
if((Test-Path $goldNetworkTarget) -or (Test-Path "$goldNetworkTarget.meta")){throw 'Temporary layout capture exists; inspect first.'}
if($Phase -eq 'before'){
    $goldNetworkHead=(git -C $goldNetworkRoot rev-parse HEAD).Trim()
    if($goldNetworkHead -ne 'c5a44a8e82296c7e627019623f959ba8eee11e89'){throw 'Before capture requires the recorded baseline HEAD.'}
    $goldNetworkDirty=git -C $goldNetworkRoot status --porcelain --untracked-files=all -- Assets
    if($goldNetworkDirty){throw 'Before capture requires unchanged runtime Assets.'}
}
Set-Content "$goldNetworkRoot/TestResults/foundry-gold-network-phase.txt" $Phase
try {
    Copy-Item -LiteralPath "$PSScriptRoot/FoundryGoldNetworkCapture.cs" -Destination $goldNetworkTarget
    Set-Content -LiteralPath "$goldNetworkTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
    if(Test-Path $goldNetworkResults){Remove-Item -LiteralPath $goldNetworkResults}
    $goldNetworkProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-buildTarget','Android','-projectPath',"`"$goldNetworkRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','FoundryGoldNetworkCapture','-testResults',"`"$goldNetworkResults`"",'-logFile',"`"$goldNetworkRoot/TestResults/foundry-gold-network.log`"") -PassThru -WindowStyle Hidden
    $goldNetworkProcess.WaitForExit()
    if($goldNetworkProcess.ExitCode -ne 0 -or !(Test-Path $goldNetworkResults)){throw 'Layout capture failed; see TestResults/foundry-gold-network.log.'}
    $goldNetworkRun=([xml](Get-Content -LiteralPath $goldNetworkResults)).'test-run'
    if($goldNetworkRun.result -ne 'Passed'){throw "Layout capture failed: $($goldNetworkRun.result)"}
    Add-Type -AssemblyName System.Drawing
    foreach($goldNetworkFile in Get-ChildItem "$goldNetworkRoot/ArtDirection/2026-10-21/foundry-gold-network-01/$Phase" -Filter '*.bmp'){
        $goldNetworkBitmap=[System.Drawing.Bitmap]::new($goldNetworkFile.FullName)
        try{$goldNetworkBitmap.Save([IO.Path]::ChangeExtension($goldNetworkFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$goldNetworkBitmap.Dispose()}
        Remove-Item -LiteralPath $goldNetworkFile.FullName
    }
    Write-Output "Foundry gold network actual Unity capture: $($goldNetworkRun.passed) passed."
} finally {
    if(Test-Path $goldNetworkTarget){Remove-Item -LiteralPath $goldNetworkTarget}
    if(Test-Path "$goldNetworkTarget.meta"){Remove-Item -LiteralPath "$goldNetworkTarget.meta"}
}
