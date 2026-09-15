#requires -Version 7
<#
Runs the Unity test suites in batch mode and, when every requested suite passes, builds the development APK.
The APK is never built on a failing suite. Close any Editor that has this project open first.

  pwsh -File Tools/Run-UnityTests.ps1                          # EditMode, PlayMode, then the APK
  pwsh -File Tools/Run-UnityTests.ps1 -SkipApk                 # the suites only
  pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -Filter ArenaWalkTests -SkipApk
  pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -SkipApk

Results land in TestResults/ (ignored by Git): <platform>.xml and .log, android-build.log, and the APK under
Builds/Android/. A Unity exit code of 198 means the license lapsed (docs/23). Run this outside restricted sandboxes;
the Editor needs its AppData caches. Never pipe this script through Tee-Object: an aborted run can leave the log locked
and a Unity process running; check for a stray Unity process and Temp/UnityLockfile before running it again.
#>
param(
  [ValidateSet('Both', 'EditMode', 'PlayMode')] [string] $Platform = 'Both',
  [string] $Filter = '',
  [switch] $SkipApk,
  [string] $UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path.Replace('\', '/')
$out = "$root/TestResults"
New-Item -ItemType Directory -Force $out | Out-Null
if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'A Unity process is already running; close it (or wait for it) first.' }
if (Test-Path "$root/Temp/UnityLockfile") { throw 'Temp/UnityLockfile exists: an Editor has the project open, or a run was killed. Remove it only when no Unity process is running.' }

$platforms = if ($Platform -eq 'Both') { @('EditMode', 'PlayMode') } else { @($Platform) }
$allPassed = $true
foreach ($p in $platforms) {
  $name = $p.ToLower() + $(if ($Filter) { '-filter' } else { '' })
  Remove-Item -LiteralPath "$out/$name.xml" -Force -ErrorAction SilentlyContinue
  $arguments = @('-batchmode', '-nographics', '-projectPath', $root, '-runTests', '-testPlatform', $p, '-testResults', "$out/$name.xml", '-logFile', "$out/$name.log")
  if ($Filter) { $arguments += @('-testFilter', $Filter) }
  $started = Get-Date
  $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -Wait -PassThru -NoNewWindow
  Write-Output "$p exit: $($process.ExitCode) after $([int]((Get-Date) - $started).TotalSeconds) s"
  if (Test-Path "$out/$name.xml") {
    $run = ([xml](Get-Content "$out/$name.xml")).'test-run'
    Write-Output "${p}: result=$($run.result) total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped) duration=$($run.duration)"
    foreach ($case in $run.GetElementsByTagName('test-case')) {
      if ($case.result -ne 'Passed') { Write-Output "  FAIL $($case.name): $($case.failure.message.InnerText)" }
    }
    if ($run.result -ne 'Passed') { $allPassed = $false }
  } else {
    Write-Output "${p}: no results file"
    Select-String -Path "$out/$name.log" -Pattern 'error CS|Exception|No valid Unity Editor license' | Select-Object -First 10 | ForEach-Object { Write-Output "  $($_.Line)" }
    $allPassed = $false
  }
}

if ($SkipApk) { Write-Output '--- APK build skipped on request'; exit ($allPassed ? 0 : 1) }
if (-not $allPassed) { Write-Output '--- a suite failed; build skipped'; exit 1 }
if ($Platform -ne 'Both' -or $Filter) { Write-Output '--- only part of the suites ran; build skipped (run without -Platform and -Filter to build)'; exit 0 }

Write-Output '--- both suites passed; building APK'
$process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode', '-quit', '-projectPath', $root, '-buildTarget', 'Android', '-executeMethod', 'Cryptforge.Editor.AndroidPrototypeBuild.Build', '-logFile', "$out/android-build.log") -Wait -PassThru -NoNewWindow
Write-Output "Build exit: $($process.ExitCode)"
Select-String -Path "$out/android-build.log" -Pattern 'Android prototype built|BuildFailedException|error CS' | Select-Object -Last 3 | ForEach-Object { Write-Output $_.Line }
$apk = "$root/Builds/Android/Cryptforge-Prototype.apk"
if (Test-Path $apk) {
  $file = Get-Item $apk
  Write-Output "APK: $($file.Length) bytes, $($file.LastWriteTime), SHA-256 $((Get-FileHash $apk -Algorithm SHA256).Hash)"
}
exit $process.ExitCode
