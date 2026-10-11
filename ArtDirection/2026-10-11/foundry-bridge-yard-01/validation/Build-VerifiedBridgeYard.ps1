$ErrorActionPreference='Stop'
$bridgeRoot='E:/MobileGame'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$bridgeRoot/Temp/UnityLockfile")){throw 'Unity is in use.'}
foreach($suite in @('editmode','playmode')) {
    $run=([xml](Get-Content "$bridgeRoot/TestResults/$suite.xml")).'test-run'
    if($run.result -ne 'Passed' -or [int]$run.failed -ne 0 -or [int]$run.skipped -ne 0){throw "Suite failed: $suite"}
}
[xml]$pure=Get-Content "$bridgeRoot/TestResults/bridge-yard.trx"
$counters=$pure.SelectSingleNode("//*[local-name()='Counters']")
if([int]$counters.passed -ne 425 -or [int]$counters.failed -ne 0){throw 'Pure suite failed'}
$started=Get-Date
$process=Start-Process -FilePath 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath',$bridgeRoot,'-buildTarget','Android','-executeMethod','Cryptforge.Editor.AndroidPrototypeBuild.Build','-logFile',"$bridgeRoot/TestResults/android-build.log") -PassThru -WindowStyle Hidden
$process.WaitForExit()
if($process.ExitCode -ne 0){throw "Build failed: $($process.ExitCode)"}
$log=Get-Content -Raw "$bridgeRoot/TestResults/android-build.log"
if($log -notmatch 'Android prototype built' -or $log -match 'BuildFailedException|error CS\d|Shader error'){throw 'Build diagnostics failed'}
$apk=Get-Item "$bridgeRoot/Builds/Android/Cryptforge-Prototype.apk"
if($apk.LastWriteTime -lt $started){throw 'APK not rebuilt'}
Write-Output "Build exit:0; APK:$($apk.Length) bytes; SHA256:$((Get-FileHash -LiteralPath $apk.FullName -Algorithm SHA256).Hash)"
