#requires -Version 7
<#
Runs the explicit PlayMode perf probe (Assets/_Project/Tests/PlayMode/PerfProbe.cs) in a batch Editor and prints its
summary. Unlike Tools/Run-UnityTests.ps1 it keeps the graphics device, because draw calls, batches and SetPass calls
are only counted when something renders: the probe then renders the gameplay camera into a 1080x2340 target each
frame. Close any Editor that has this project open first; nothing else may run Unity meanwhile. About 5 minutes for
both heroes and both phases.

  pwsh -File Tools/Run-PerfProbe.ps1               # graphics: CPU, GC, render counts and the census
  pwsh -File Tools/Run-PerfProbe.ps1 -NoGraphics   # CPU, GC and the census only, as Run-UnityTests.ps1 would run it

Results land in TestResults/ (ignored by Git): perf-probe.txt (the summary), perf-probe-frames.tsv (every frame),
perf-probe.xml and perf-probe.log. Knobs, one value per line, in TestResults/: perf-probe-heroes.txt (hero ids,
"vanguard" for the default hero), perf-probe-phases.txt (horde, warden), perf-probe-seconds.txt, perf-probe-seed.txt,
perf-probe-floors.txt ("1,2": each phase on floor 1 and again with floor 2's Ice dress; "1,2,3,4": every change of the
four-floor Descent - 2 Ice, 3 Gloam (the garden prepared while the ice plays), 4 the finale's foundry; floor 1 alone by
default). With a later floor, the summary ends in "compare <hero>/<phase>/floor n <metric>: floor 1 | floor n | change"
lines, also written to perf-probe-floors-compare.txt.
No APK is built and nothing touches a device.
#>
param(
  [switch] $NoGraphics,
  [string] $UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path.Replace('\', '/')
$out = "$root/TestResults"
New-Item -ItemType Directory -Force $out | Out-Null
if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'A Unity process is already running; close it (or wait for it) first.' }
if (Test-Path "$root/Temp/UnityLockfile") { throw 'Temp/UnityLockfile exists: an Editor has the project open, or a run was killed. Remove it only when no Unity process is running.' }

Remove-Item -LiteralPath "$out/perf-probe.xml", "$out/perf-probe.txt" -Force -ErrorAction SilentlyContinue
$arguments = @('-batchmode')
if ($NoGraphics) { $arguments += '-nographics' }
$arguments += @('-projectPath', $root, '-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'Cryptforge.Tests.PerfProbe',
  '-testResults', "$out/perf-probe.xml", '-logFile', "$out/perf-probe.log")
$started = Get-Date
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -PassThru -WindowStyle Hidden
# Wait for the Editor itself: -Wait also waits for long-lived licensing/helper descendants on Windows.
$process.WaitForExit()
Write-Output "PerfProbe exit: $($process.ExitCode) after $([int]((Get-Date) - $started).TotalSeconds) s (graphics: $(-not $NoGraphics))"
if (Test-Path "$out/perf-probe.xml") {
  $run = ([xml](Get-Content "$out/perf-probe.xml")).'test-run'
  Write-Output "PerfProbe: result=$($run.result) total=$($run.total) passed=$($run.passed) failed=$($run.failed)"
  foreach ($case in $run.GetElementsByTagName('test-case')) {
    if ($case.result -ne 'Passed') { Write-Output "  FAIL $($case.name): $($case.failure.message.InnerText)" }
  }
} else {
  Write-Output 'PerfProbe: no results file'
  Select-String -Path "$out/perf-probe.log" -Pattern 'error CS|Exception|No valid Unity Editor license' | Select-Object -First 10 | ForEach-Object { Write-Output "  $($_.Line)" }
}
if (Test-Path "$out/perf-probe.txt") { Get-Content "$out/perf-probe.txt" } else { Write-Output 'no TestResults/perf-probe.txt was written' }
exit $process.ExitCode
