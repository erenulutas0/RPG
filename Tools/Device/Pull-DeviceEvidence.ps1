#requires -Version 7
<#
Pulls what the V0.1 gate's two device rows read - the game's telemetry and the Unity log's frame lines - from the phone
into TestResults/device-<yyyyMMdd-HHmmss>/ and prints what it found. The batch report then reads every such folder
(Tools/CombatChecks/DevicePull.cs, FOUNDRY_DEVICE): "Floor time (the owner's play)" from the owner's runs on the
installed APK, "Frame time on the device" from FrameTimeProbe's [Perf] windows set against the telemetry (docs/23,
2026-11-11).

  pwsh -File Tools/Device/Pull-DeviceEvidence.ps1             # pull, print what was found
  pwsh -File Tools/Device/Pull-DeviceEvidence.ps1 -Report     # and run the batch report, print the gate's two rows
  pwsh -File Tools/Device/Pull-DeviceEvidence.ps1 -Serial R5CW...   # with more than one phone connected
  pwsh -File Tools/Device/Pull-DeviceEvidence.ps1 -FromFolder <dir> -Report   # offline: no adb at all; <dir> is read
                                                      # as if pulled (telemetry/, development/, logcat*.txt, pull.txt
                                                      # where there is one) and -Report reads it alone (FOUNDRY_DEVICE)

Pull-only: it never taps, launches, stops, installs, pushes, deletes, clears the log or changes a setting, and takes no
screenshot, so it needs no focus check. Everything it runs on the phone reads:
  adb devices                                         which phone is connected
  adb shell getprop ro.product.model / ro.build.version.release
  adb shell dumpsys package <package>                 the installed APK's version and lastUpdateTime (its install)
  adb shell pm path <package>; sha256sum <base.apk>   the installed APK's SHA-256: the build the runs are read on
  adb shell date                                      the phone's clock and UTC offset, to read the install time
  adb shell pidof <package>                           whether the game is running (its last seconds may be unwritten)
  adb shell ls <files>/telemetry, <files>/development
  adb pull <files>/telemetry                          RunTelemetry's events.jsonl and its rotated events.N.jsonl
  adb pull <files>/development                        the development flags, a few bytes each: which probes were on
  adb logcat -g                                       the log's buffer size (how many minutes of lines it keeps)
  adb logcat -d -v time -v epoch -s Unity:V           the Unity log as it stands (-d reads and exits, nothing is
                                                      cleared): FrameTimeProbe's [Perf] and RenderStatsProbe's
                                                      [RenderStats] lines, which exist only there, never in a file
<files> is /sdcard/Android/data/<package>/files, Application.persistentDataPath. The owner's profile is not pulled, and
nothing on the phone is written. The folder is new every time, so nothing here is overwritten or needs a backup.

The phone keeps about three files of 256 KB of telemetry, a long run or two each: pull after every two or three of the
owner's runs; the report reads every pull together, a line two pulls share once.
#>
param(
  [string] $Serial,
  [string] $Package = 'com.cryptforge.prototype',
  # The folder to pull into, absolute or within TestResults; by default TestResults/device-<yyyyMMdd-HHmmss>.
  [string] $Out,
  # adb itself; by default $env:ADB, adb on the PATH, the Android SDK's or the pinned Unity Editor's.
  [string] $Adb,
  # Offline: read this folder as if it had been pulled, with no adb call at all (a dry run, or a pull copied by hand).
  [string] $FromFolder,
  [switch] $Report
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$results = Join-Path $root 'TestResults'
$files = "/sdcard/Android/data/$Package/files"
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

function Find-Adb {
  if ($Adb) {
    if (Test-Path -LiteralPath $Adb) { return (Resolve-Path -LiteralPath $Adb).Path }
    throw "No adb at -Adb $Adb"
  }
  if ($env:ADB -and (Test-Path -LiteralPath $env:ADB)) { return $env:ADB }
  $onPath = Get-Command adb -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($onPath) { return $onPath.Source }
  foreach ($candidate in @("$env:LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe",
      'E:/Unity/Editors/6000.0.65f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe')) {
    if (Test-Path -LiteralPath $candidate) { return $candidate }
  }
  return $null
}

# One adb call: its standard output as lines, its error output apart, its exit code. Never throws on a failed call.
function Invoke-Adb([string[]] $Arguments) {
  $errors = [System.Collections.Generic.List[string]]::new()
  $ErrorActionPreference = 'Continue'
  $lines = @(& $script:adbPath @Arguments 2>&1 | ForEach-Object {
      if ($_ -is [System.Management.Automation.ErrorRecord]) { $errors.Add($_.ToString()) } else { "$_" }
    })
  [pscustomobject]@{ Lines = $lines; Errors = $errors; Code = $LASTEXITCODE }
}

function Invoke-Phone([string[]] $Arguments) { Invoke-Adb (@('-s', $script:serial) + $Arguments) }

function First-Line($call) {
  $line = @($call.Lines | Where-Object { $_.Trim().Length -gt 0 }) | Select-Object -First 1
  if ($null -eq $line) { return $null }
  return $line.Trim()
}

# adb writes UTF-8; the console's code page would mangle anything past ASCII in the log.
$consoleEncoding = [Console]::OutputEncoding
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
try {
 if ($FromFolder) {
  # Offline: nothing is run on a phone; the folder's own files stand for the pull, pull.txt's values where it has one.
  if (-not (Test-Path -LiteralPath $FromFolder -PathType Container)) { throw "No folder at -FromFolder $FromFolder" }
  $folder = (Resolve-Path -LiteralPath $FromFolder).Path
  $values = @{}
  $manifestPath = Join-Path $folder 'pull.txt'
  if (Test-Path -LiteralPath $manifestPath) {
    foreach ($line in Get-Content -LiteralPath $manifestPath) {
      if ($line -match '^\s*#' -or $line -notmatch '^(?<key>[^:]+):(?<value>.*)$') { continue }
      $value = $Matches.value.Trim()
      if ($value.Length -gt 0 -and $value -ne '?') { $values[$Matches.key.Trim()] = $value }
    }
  }
  $script:serial = if ($values.serial) { $values.serial } else { 'no phone' }
  $model = if ($values.model) { $values.model } else { 'a folder' }
  $android = $values.android
  $versionName = if ($values.version_name) { $values.version_name } else { '?' }
  $versionCode = if ($values.version_code) { $values.version_code } else { '?' }
  $sha = $values.apk_sha256
  $installedUtc = $values.installed_utc
  $lastUpdate = $values.installed_device
  $offset = $values.device_utc_offset
  $buffer = $values.logcat_buffer
  $running = $values.game_running -eq 'yes'
  $logFormat = 'from the folder'
  $logLines = @(Get-ChildItem -LiteralPath $folder -Filter 'logcat*.txt' -File | Sort-Object Name | ForEach-Object { Get-Content -LiteralPath $_.FullName })
  $telemetry = @(Get-ChildItem -LiteralPath (Join-Path $folder 'telemetry') -Filter '*.jsonl' -File -ErrorAction SilentlyContinue | Sort-Object Name)
  $flags = @(Get-ChildItem -LiteralPath (Join-Path $folder 'development') -File -ErrorAction SilentlyContinue | Sort-Object Name)
 } else {
  $script:adbPath = Find-Adb
  if (-not $script:adbPath) {
    Write-Output 'adb was not found: install the Android SDK platform-tools, put adb on the PATH, or pass -Adb <path>. Nothing was pulled.'
    exit 1
  }

  # Which phone. Nothing is pulled unless exactly one is ready, or -Serial names a ready one.
  $listed = Invoke-Adb @('devices')
  $phones = @(foreach ($line in $listed.Lines) {
      if ($line -match '^(\*|List of devices)' -or $line.Trim().Length -eq 0) { continue }
      if ($line -match '^(?<serial>\S+)\s+(?<state>\S+)') { [pscustomobject]@{ Serial = $Matches.serial; State = $Matches.state } }
    })
  $ready = @($phones | Where-Object { $_.State -eq 'device' -and (-not $Serial -or $_.Serial -eq $Serial) })
  if ($ready.Count -eq 0) {
    Write-Output ('No phone to pull from: adb devices lists ' + $(if ($phones.Count -eq 0) { 'none.' } else { 'none ready:' }))
    foreach ($phone in $phones) {
      $hint = switch ($phone.State) {
        'unauthorized' { ' - unlock the phone and allow USB debugging on its prompt' }
        'offline' { ' - unplug and plug the cable again' }
        default { '' }
      }
      Write-Output "  $($phone.Serial): $($phone.State)$hint"
    }
    if ($Serial) { Write-Output "  (-Serial $Serial was asked for)" }
    Write-Output 'Connect the S23 by USB, unlock it and allow USB debugging, then run this again. Nothing was pulled.'
    exit 1
  }
  if ($ready.Count -gt 1) {
    Write-Output "Several phones are ready ($(($ready | ForEach-Object Serial) -join ', ')): name one with -Serial. Nothing was pulled."
    exit 1
  }
  $script:serial = $ready[0].Serial

  # The folder: new every time.
  $pulledUtc = [DateTime]::UtcNow
  $name = if ($Out) { $Out } else { 'device-' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss', $invariant) }
  $folder = if ([System.IO.Path]::IsPathRooted($name)) { $name } else { Join-Path $results $name }
  if (Test-Path -LiteralPath $folder) { throw "$folder exists already: nothing is pulled over an earlier pull." }
  New-Item -ItemType Directory -Force $folder | Out-Null

  # The phone, the APK and the phone's clock.
  $model = First-Line (Invoke-Phone @('shell', 'getprop', 'ro.product.model'))
  $android = First-Line (Invoke-Phone @('shell', 'getprop', 'ro.build.version.release'))
  $dump = (Invoke-Phone @('shell', 'dumpsys', 'package', $Package)).Lines -join "`n"
  $versionName = if ($dump -match 'versionName=(\S+)') { $Matches[1] } else { '?' }
  $versionCode = if ($dump -match 'versionCode=(\d+)') { $Matches[1] } else { '?' }
  $lastUpdate = if ($dump -match 'lastUpdateTime=(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})') { $Matches[1] } else { $null }
  $offset = First-Line (Invoke-Phone @('shell', 'date', '+%z'))
  $deviceClock = First-Line (Invoke-Phone @('shell', 'date', '-u', '+%Y-%m-%dT%H:%M:%SZ'))
  $installedUtc = $null
  if ($lastUpdate -and $offset -match '^(?<sign>[+-])(?<hours>\d{2}):?(?<minutes>\d{2})$') {
    $span = [TimeSpan]::new([int]$Matches.hours, [int]$Matches.minutes, 0)
    if ($Matches.sign -eq '-') { $span = $span.Negate() }
    $local = [DateTime]::ParseExact($lastUpdate, 'yyyy-MM-dd HH:mm:ss', $invariant)
    $installedUtc = ($local - $span).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", $invariant)
  }
  $apk = $null
  foreach ($line in (Invoke-Phone @('shell', 'pm', 'path', $Package)).Lines) {
    if ($line -match '^package:(?<path>\S*base\.apk)\s*$') { $apk = $Matches.path; break }
  }
  $sha = $null
  if ($apk) {
    $hash = First-Line (Invoke-Phone @('shell', 'sha256sum', $apk))
    if ($hash -match '^(?<sha>[0-9a-fA-F]{64})\b') { $sha = $Matches.sha.ToLowerInvariant() }
  }
  $running = (First-Line (Invoke-Phone @('shell', 'pidof', $Package))) -match '^\d+'

  # The telemetry and the development flags.
  $pulled = @()
  foreach ($part in @('telemetry', 'development')) {
    $listing = Invoke-Phone @('shell', 'ls', "$files/$part")
    if ($listing.Code -ne 0) { continue }
    $pull = Invoke-Phone @('pull', "$files/$part", $folder)
    if ($pull.Code -ne 0) { throw "adb pull $files/$part failed: $(($pull.Errors + $pull.Lines) -join ' ')" }
    $pulled += $part
  }
  $telemetry = @(Get-ChildItem -LiteralPath (Join-Path $folder 'telemetry') -Filter '*.jsonl' -File -ErrorAction SilentlyContinue | Sort-Object Name)
  $flags = @(Get-ChildItem -LiteralPath (Join-Path $folder 'development') -File -ErrorAction SilentlyContinue | Sort-Object Name)

  # The Unity log as it stands: -d reads and exits. Epoch seconds, so its times need no zone; plain -v time on a phone
  # whose logcat refuses that, its times then the phone's local ones at its offset.
  $buffer = ((Invoke-Phone @('logcat', '-g')).Lines | Where-Object { $_ -match '^main' } | Select-Object -First 1)
  $logFormat = '-v time -v epoch'
  $logTimes = 'seconds since 1970'
  $log = Invoke-Phone @('logcat', '-d', '-v', 'time', '-v', 'epoch', '-s', 'Unity:V')
  if ($log.Code -ne 0) {
    $logFormat = '-v time'
    $logTimes = "the phone's local time at logcat_offset"
    $log = Invoke-Phone @('logcat', '-d', '-v', 'time', '-s', 'Unity:V')
  }
  $logFile = Join-Path $folder 'logcat-unity.txt'
  Set-Content -LiteralPath $logFile -Value $log.Lines -Encoding utf8NoBOM

  $manifest = @(
    '# Tools/Device/Pull-DeviceEvidence.ps1: what it pulled and read off the phone; Tools/CombatChecks/DevicePull.cs reads it.'
    "pulled_utc: $($pulledUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", $invariant))"
    "serial: $script:serial"
    "model: $(if ($model) { $model } else { '?' })"
    "android: $(if ($android) { $android } else { '?' })"
    "package: $Package"
    "version_name: $versionName"
    "version_code: $versionCode"
    "apk_sha256: $(if ($sha) { $sha } else { '?' })"
    "installed_device: $(if ($lastUpdate) { $lastUpdate } else { '?' })"
    "device_utc_offset: $(if ($offset) { $offset } else { '?' })"
    "installed_utc: $(if ($installedUtc) { $installedUtc } else { '?' })"
    "device_clock_utc: $(if ($deviceClock) { $deviceClock } else { '?' })"
    "game_running: $(if ($running) { 'yes' } else { 'no' })"
    "development_flags: $(if ($flags.Count -gt 0) { ($flags | ForEach-Object Name) -join ', ' } else { 'none' })"
    "telemetry_files: $(if ($telemetry.Count -gt 0) { ($telemetry | ForEach-Object { "$($_.Name) $($_.Length)" }) -join ', ' } else { 'none' })"
    "logcat: logcat-unity.txt (adb logcat -d $logFormat -s Unity:V), $($log.Lines.Count) lines"
    "logcat_times: $logTimes"
    "logcat_offset: $(if ($logFormat -eq '-v time' -and $offset) { $offset } else { '+0000' })"
    "logcat_buffer: $(if ($buffer) { $buffer.Trim() } else { '?' })"
  )
  Set-Content -LiteralPath (Join-Path $folder 'pull.txt') -Value $manifest -Encoding utf8NoBOM
  $logLines = $log.Lines
 }

  # What was found: a count by eye; the batch report reads it properly. A file the game holds open (FileTelemetryStore's
  # hold) reads the same; a run with no run_end is still on, or the app was killed in it.
  $lines = @(foreach ($file in ($telemetry | Sort-Object Name -Descending)) { Get-Content -LiteralPath $file.FullName })
  $heroes = @{}
  $ends = @{}
  $causes = @{}
  $probes = @{}
  $floors = @{}
  $sessions = @{}
  foreach ($entry in $lines) {
    if ($entry -match '"session":"(?<session>[^"]+)"') { $sessions[$Matches.session] = 1 }
    if ($entry -match '"event":"run_start".*?"hero_id":"(?<hero>[^"]+)"') { $heroes[$Matches.hero] = 1 + [int]$heroes[$Matches.hero] }
    elseif ($entry -match '"event":"run_end".*?"result":"(?<result>[^"]+)"') {
      $ends[$Matches.result] = 1 + [int]$ends[$Matches.result]
      if ($Matches.result -eq 'defeat' -and $entry -match '"cause":"(?<cause>[^"]+)"') { $causes[$Matches.cause] = 1 + [int]$causes[$Matches.cause] }
    }
    elseif ($entry -match '"event":"run_interrupted"') { $ends['interrupted'] = 1 + [int]$ends['interrupted'] }
    elseif ($entry -match '"event":"floor_complete".*?"floor_index":(?<floor>\d+)') { $floors[[int]$Matches.floor] = 1 + [int]$floors[[int]$Matches.floor] }
    if ($entry -match '"run":"(?<run>[^"]+)".*"hero_max_hp":(?<max>\d+)' -and [double]$Matches.max -ge 1000000) { $probes[$Matches.run] = 1 }
  }
  $times = @($lines | ForEach-Object { if ($_ -match '"t":"(?<t>[^"]+)"') { $Matches.t } } | Sort-Object)
  $perf = @($logLines | Where-Object { $_ -match '\[Perf\] window ' })
  $render = @($logLines | Where-Object { $_ -match '\[RenderStats\] t=' })
  $slow = @($logLines | Where-Object { $_ -match '\[Perf\] long frame ' })
  # The ground projection's one-time work: with a prewarm it is the load's (expected, once a run); without one it ran
  # at a ramp's first overlap, the stall the pre-warm is there to remove (docs/23, 2026-11-12 runbook).
  $ground = @($logLines | Where-Object { $_ -match '\[Perf\] ground projection: ' })
  $groundRamp = @($ground | Where-Object { $_ -match 'prewarm 0\.00 ms' })

  Write-Output "$(if ($FromFolder) { 'Read offline (no adb) from' } else { 'Pulled from' }) $model ($script:serial, Android $android) $(if ($FromFolder) { 'in' } else { 'into' }) $folder"
  Write-Output ("  APK: $Package $versionName ($versionCode), SHA-256 $(if ($sha) { $sha } else { 'not read' }), installed " +
    $(if ($installedUtc) { "$installedUtc (the phone's $lastUpdate at $offset)" } else { 'at a time not read: every run will be read' }))
  if ($telemetry.Count -eq 0) {
    Write-Output "  telemetry: none on the phone ($files/telemetry): no session since the game's data was cleared"
  } else {
    Write-Output "  telemetry: $(($telemetry | ForEach-Object { "$($_.Name) $($_.Length) B" }) -join ', '); $($lines.Count) lines, $($times[0]) to $($times[-1])"
    Write-Output ("    $($sessions.Count) sessions; runs begun: $(if ($heroes.Count) { ($heroes.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ', ' } else { 'none' }); " +
      "ended: $(if ($ends.Count) { ($ends.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ', ' } else { 'none' }); " +
      "floors cleared: $(if ($floors.Count) { ($floors.GetEnumerator() | Sort-Object Name | ForEach-Object { "floor $($_.Name) $($_.Value)" }) -join ', ' } else { 'none' })")
    if ($causes.Count -gt 0) { Write-Output "    deaths by run_end's cause: $(($causes.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ', ')" }
    if ($probes.Count -gt 0) { Write-Output "    the density probe's runs (its hero at a million health): $($probes.Count), left out of the owner's floors" }
  }
  Write-Output "  development flags: $(if ($flags.Count) { ($flags | ForEach-Object { "$($_.Name) ($((Get-Content -LiteralPath $_.FullName -TotalCount 1) -join ''))" }) -join ', ' } else { 'none' })"
  Write-Output "  log: $($logLines.Count) Unity lines ($logFormat; buffer $(if ($buffer) { $buffer.Trim() } else { 'not read' })): $($perf.Count) [Perf] windows, $($render.Count) [RenderStats] windows, $($slow.Count) long frames, $($ground.Count) ground projection lines ($($groundRamp.Count) at a ramp)"
  foreach ($line in $groundRamp) { Write-Output "  note: the ground projection did one-time work at a ramp (the pre-warm missed it): $($line.Substring($line.IndexOf('[Perf]')))" }

  if ($running) { Write-Output '  note: the game is running - its last seconds of telemetry may be unwritten, and a run still on is read as unfinished.' }
  if ($telemetry | Where-Object Name -eq 'events.2.jsonl') { Write-Output "  note: the phone's telemetry is at its three files: the oldest lines rotate away from here on. Pull after every two or three runs; every pull is read together." }
  if ($perf.Count -eq 0) { Write-Output "  note: no [Perf] window - the installed APK is not a development build, or the log no longer holds the session (pull right after it)." }
  foreach ($flag in $flags) {
    switch ($flag.Name) {
      'autopilot.txt' { Write-Output '  note: development/autopilot.txt is on the phone - the autopilot''s runs read as the owner''s.' }
      'horde-probe.txt' { Write-Output '  note: development/horde-probe.txt is on the phone - every run pads its first wave and gives the hero a million health; remove it (with the owner''s OK) before the owner plays.' }
      'floor-clock.txt' { Write-Output '  note: development/floor-clock.txt is on the phone - floors start their clock ahead, and their time is not the owner''s.' }
    }
  }
  if ($null -eq $sha -or $null -eq $installedUtc) { Write-Output '  note: the installed APK was not fully read (see pull.txt).' }

  if (-not $Report) {
    Write-Output ''
    Write-Output 'Next: the batch report reads every TestResults/device-* pull (FOUNDRY_DEVICE names others, or none):'
    Write-Output '  $env:FOUNDRY_LEVERS=0; $env:FOUNDRY_COHORTS=1; $env:FOUNDRY_FLOORS=2'
    Write-Output '  dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch"'
    Write-Output '  (or run this script with -Report)'
    exit 0
  }
} finally {
  [Console]::OutputEncoding = $consoleEncoding
}

# -Report: the batch report on the pulls, the levers off and the cohorts on both floors unless the environment says
# otherwise, then the gate's summary and its two device rows.
foreach ($setting in @(@('FOUNDRY_LEVERS', '0'), @('FOUNDRY_COHORTS', '1'), @('FOUNDRY_FLOORS', '2'))) {
  if (-not [Environment]::GetEnvironmentVariable($setting[0])) { Set-Item -Path "env:$($setting[0])" -Value $setting[1] }
}
# Offline, the folder read is the one named, alone: never the real pulls in TestResults.
if ($FromFolder -and -not $env:FOUNDRY_DEVICE) { $env:FOUNDRY_DEVICE = $folder }
Write-Output ''
Write-Output "Running the batch report (FOUNDRY_LEVERS=$env:FOUNDRY_LEVERS FOUNDRY_COHORTS=$env:FOUNDRY_COHORTS FOUNDRY_FLOORS=$env:FOUNDRY_FLOORS), a few minutes..."
& dotnet test (Join-Path $root 'Tools/CombatChecks') -c Release -p:NuGetAudit=false --filter 'FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch'
$code = $LASTEXITCODE
$gate = Join-Path $(if ($env:FOUNDRY_OUT) { $env:FOUNDRY_OUT } else { $results }) 'foundry-gate.md'
if (Test-Path -LiteralPath $gate) {
  Write-Output ''
  Get-Content -LiteralPath $gate | Where-Object { $_ -match '^\*\*Where V0\.1 stands|^Device: |^\| (Floor time \(the owner''s play\)|Frame time on the device) \|' }
}
exit $code
