#requires -Version 7
<#
Builds the release AAB for Google Play (docs/61) with Unity in batch mode, then writes its report. Close any Editor that
has this project open first. The development APK stays Tools/Run-UnityTests.ps1's.

  pwsh -File Tools/Build-Release.ps1                          # Builds/Release/Cryptforge-<name>-<code>[-debugkey].aab + report
  pwsh -File Tools/Build-Release.ps1 -BumpVersion             # versionCode + 1 in Tools/Release/version.txt first (undone on failure)
  pwsh -File Tools/Build-Release.ps1 -AskPasswords            # asks (masked) for the upload key's two passwords, for this run only
  pwsh -File Tools/Build-Release.ps1 -PackageName com.x.y     # a trial package; the permanent one is chosen with the owner
  pwsh -File Tools/Build-Release.ps1 -Clean                   # rebuilds the player data and IL2CPP from scratch
  pwsh -File Tools/Build-Release.ps1 -ReportOnly [-Aab <path>] # the report again, without Unity (default: the last build's AAB)

Signing: with none of CRYPTFORGE_KEYSTORE_PATH, CRYPTFORGE_KEYSTORE_PASS, CRYPTFORGE_KEY_ALIAS, CRYPTFORGE_KEY_PASS set,
the AAB keeps Unity's debug key, its name ends in -debugkey and the report says "NOT FOR PLAY: debug key". With all four
set, the build signs with the upload key through jarsigner, which reads the passwords from the environment by name. This
script never prints, logs or writes a password.

The report checks the AAB itself: its size, its ABIs (arm64-v8a only), every arm64 library's LOAD segment alignment
(llvm-readelf -lW, 0x4000 or more for 16 KB pages), its protobuf manifest (package, versionName, versionCode, minSdk 26,
targetSdk 36, not debuggable), its signature, and that ProjectSettings/ is byte for byte as before the build. Outputs and
the Unity log land in Builds/Release/ (ignored by Git). A Unity exit code of 198 means the license lapsed (docs/23).
#>
param(
  [switch] $BumpVersion,
  [string] $PackageName = '',
  [switch] $Clean,
  [switch] $AskPasswords,
  [switch] $ReportOnly,
  [string] $Aab = '',
  [string] $UnityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
# The report's numbers in one format whatever the system's language (a Turkish Windows writes 1.234,5).
[Globalization.CultureInfo]::CurrentCulture = [Globalization.CultureInfo]::InvariantCulture
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path.Replace('\', '/')
$androidPlayer = (Join-Path (Split-Path $UnityEditor -Parent) 'Data/PlaybackEngines/AndroidPlayer').Replace('\', '/')
$readelf = "$androidPlayer/NDK/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-readelf.exe"
$jdkBin = "$androidPlayer/OpenJDK/bin"
$outDir = "$root/Builds/Release"
$versionPath = "$root/Tools/Release/version.txt"
$summaryPath = "$root/Builds/Release/last-build.txt"
$mergedManifest = "$root/Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/intermediates/merged_manifest/release/processReleaseMainManifest/AndroidManifest.xml"
$androidNs = 'http://schemas.android.com/apk/res/android'
$english = @('-J-Duser.language=en', '-J-Duser.country=US')
foreach ($tool in @($readelf, "$jdkBin/keytool.exe", "$jdkBin/jarsigner.exe")) {
  if (-not (Test-Path $tool)) { throw "Missing $tool (the Android module of Unity $UnityEditor)." }
}
New-Item -ItemType Directory -Force $outDir | Out-Null

# The version file's parser and the AAB's manifest reader, compiled from the sources Unity builds.
if (-not ('Cryptforge.Editor.Release.ReleaseVersion' -as [type])) {
  Add-Type -Path "$root/Assets/_Project/Scripts/Editor/Release/ReleaseRules.cs", "$root/Assets/_Project/Scripts/Editor/Release/AabManifest.cs"
}
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

function Get-SettingsHashes([string] $folder) {
  $hashes = @{}
  Get-ChildItem $folder -Recurse -File | ForEach-Object {
    $hashes[[IO.Path]::GetRelativePath($folder, $_.FullName)] = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
  }
  return $hashes
}

# --- the build ---
$settingsResult = 'not checked: no build in this run (-ReportOnly)'
$settingsPass = $true
$unityExit = $null
if ($ReportOnly) {
  if ($BumpVersion -or $Clean -or $AskPasswords) { throw '-ReportOnly builds nothing: drop -BumpVersion, -Clean and -AskPasswords.' }
} else {
  if ($Aab) { throw '-Aab is for -ReportOnly; a build writes its own AAB.' }
  if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'A Unity process is already running; close it (or wait for it) first.' }
  if (Test-Path "$root/Temp/UnityLockfile") { throw 'Temp/UnityLockfile exists: an Editor has the project open, or a run was killed. Remove it only when no Unity process is running.' }
  if ($AskPasswords -and (-not $env:CRYPTFORGE_KEYSTORE_PATH -or -not $env:CRYPTFORGE_KEY_ALIAS)) {
    throw '-AskPasswords asks for the two passwords only: set CRYPTFORGE_KEYSTORE_PATH and CRYPTFORGE_KEY_ALIAS first (docs/61 section 2).'
  }

  $versionBefore = [IO.File]::ReadAllText($versionPath)
  if ($BumpVersion) {
    [IO.File]::WriteAllText($versionPath, [Cryptforge.Editor.Release.ReleaseVersion]::Bump($versionBefore))
  }
  $version = [Cryptforge.Editor.Release.ReleaseVersion]::Parse([IO.File]::ReadAllText($versionPath))
  Write-Output "--- release build $version$(if ($BumpVersion) { ' (bumped)' })"

  # ProjectSettings/ before the build: hashes, and a copy to put back should Unity save a changed settings asset.
  $settingsBackup = Join-Path ([IO.Path]::GetTempPath()) ('cryptforge-projectsettings-' + [Guid]::NewGuid().ToString('N'))
  Copy-Item "$root/ProjectSettings" $settingsBackup -Recurse
  $settingsBefore = Get-SettingsHashes "$root/ProjectSettings"

  $log = "$outDir/Cryptforge-$($version.Name)-$($version.Code).unity.log"
  $arguments = @('-batchmode', '-quit', '-projectPath', $root, '-buildTarget', 'Android',
    '-executeMethod', 'Cryptforge.Editor.AndroidReleaseBuild.Build', '-logFile', $log)
  if ($PackageName) { $arguments += @('-packageName', $PackageName) }
  if ($Clean) { $arguments += '-cleanBuild' }
  $askedPasswords = $false
  try {
    if ($AskPasswords) {
      # Into this process's environment only, which the Unity process inherits; removed below whatever happens.
      $store = Read-Host 'Upload keystore password' -AsSecureString
      $key = Read-Host 'Upload key password (Enter: the same as the keystore''s)' -AsSecureString
      $env:CRYPTFORGE_KEYSTORE_PASS = ConvertFrom-SecureString $store -AsPlainText
      $env:CRYPTFORGE_KEY_PASS = if ($key.Length -gt 0) { ConvertFrom-SecureString $key -AsPlainText } else { $env:CRYPTFORGE_KEYSTORE_PASS }
      Remove-Variable store, key
      $askedPasswords = $true
    }
    $started = Get-Date
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -PassThru -WindowStyle Hidden
    # Wait for the Editor itself: -Wait also waits for long-lived licensing/helper descendants on Windows.
    $process.WaitForExit()
    $unityExit = $process.ExitCode
    Write-Output "Unity exit: $unityExit after $([int]((Get-Date) - $started).TotalSeconds) s; log $log"
  } finally {
    if ($askedPasswords) { Remove-Item Env:CRYPTFORGE_KEYSTORE_PASS, Env:CRYPTFORGE_KEY_PASS -ErrorAction SilentlyContinue }
  }

  # ProjectSettings/ after: the build restores every setting it changed and saves no settings asset. Should a file differ
  # all the same, it is put back from the copy and the report says which.
  $settingsAfter = Get-SettingsHashes "$root/ProjectSettings"
  $differing = @(@($settingsBefore.Keys) + @($settingsAfter.Keys) | Sort-Object -Unique | Where-Object { $settingsBefore[$_] -ne $settingsAfter[$_] })
  if ($differing.Count -eq 0) {
    $settingsResult = 'unchanged (SHA-256 of every file before and after the build)'
  } else {
    foreach ($relative in $differing) {
      $saved = Join-Path $settingsBackup $relative
      $current = Join-Path "$root/ProjectSettings" $relative
      if (Test-Path $saved) { Copy-Item $saved $current -Force } else { Remove-Item $current -Force }
    }
    $settingsPass = $false
    $settingsResult = "CHANGED by the build and put back from the copy taken before it: $($differing -join ', ')"
  }
  Remove-Item $settingsBackup -Recurse -Force
  Write-Output "ProjectSettings: $settingsResult"

  if ($unityExit -ne 0) {
    if ($BumpVersion) { [IO.File]::WriteAllText($versionPath, $versionBefore); Write-Output 'versionCode bump undone: nothing was built.' }
    Write-Output '--- the release build failed'
    if (Test-Path $log) {
      Select-String -Path $log -Pattern 'BuildFailedException|ReleaseConfigException|error CS|Exception:|Settings not restored|No valid Unity Editor license' |
        Select-Object -Last 12 | ForEach-Object { Write-Output "  $($_.Line)" }
    }
    exit 1
  }
}

# --- the report ---
$summary = @{}
if (Test-Path $summaryPath) { $summary = [Cryptforge.Editor.Release.ReleaseSummary]::Read([IO.File]::ReadAllText($summaryPath)) }
if (-not $Aab) {
  if (-not $summary.ContainsKey('aab')) { throw "No AAB to report on: $summaryPath is missing; build first or pass -Aab." }
  $Aab = $summary['aab']
}
$Aab = (Resolve-Path $Aab).Path.Replace('\', '/')
$isLastBuild = $summary.ContainsKey('aab') -and ($summary['aab'] -eq $Aab)
if ($isLastBuild) {
  $expected = [Cryptforge.Editor.Release.ReleaseVersion]::new($summary['versionName'], [int]$summary['versionCode'])
  $expectedPackage = $summary['package']
} else {
  $expected = [Cryptforge.Editor.Release.ReleaseVersion]::Parse([IO.File]::ReadAllText($versionPath))
  $expectedPackage = if ($PackageName) { $PackageName } else { [Cryptforge.Editor.Release.ReleaseSettings]::DefaultPackageName }
}

$checks = [Collections.Generic.List[object]]::new()
function Add-Check([string] $name, [bool] $pass, [string] $detail, [switch] $WarnOnly) {
  $status = if ($pass) { 'PASS' } elseif ($WarnOnly) { 'WARN' } else { 'FAIL' }
  $checks.Add([pscustomobject]@{ Name = $name; Status = $status; Detail = $detail })
}
function Test-Same([string] $name, $actual, $wanted) {
  Add-Check $name ("$actual" -ceq "$wanted") "$(if ($null -eq $actual) { 'missing' } else { $actual })$(if ("$actual" -cne "$wanted") { ", expected $wanted" })"
}

$file = Get-Item $Aab
$sha256 = (Get-FileHash $Aab -Algorithm SHA256).Hash
$work = Join-Path $outDir ('inspect-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $work | Out-Null
$libraryRows = [Collections.Generic.List[string]]::new()
$moduleRows = [Collections.Generic.List[string]]::new()
try {
  $zip = [IO.Compression.ZipFile]::OpenRead($Aab)
  try {
    [string[]] $names = @($zip.Entries | ForEach-Object { $_.FullName })

    # The manifest Play reads, from the AAB.
    $entry = $zip.GetEntry([Cryptforge.Editor.Release.AabManifest]::EntryName)
    if (-not $entry) { throw "$Aab has no $([Cryptforge.Editor.Release.AabManifest]::EntryName): not an Android App Bundle." }
    $bytes = [IO.MemoryStream]::new()
    $stream = $entry.Open(); $stream.CopyTo($bytes); $stream.Dispose()
    $manifest = [Cryptforge.Editor.Release.AabManifest]::Parse($bytes.ToArray())
    Test-Same 'package' $manifest.Package $expectedPackage
    Test-Same 'versionName' $manifest.VersionName $expected.Name
    Test-Same 'versionCode' $manifest.VersionCode $expected.Code
    Test-Same 'minSdkVersion' $manifest.MinSdk ([Cryptforge.Editor.Release.ReleaseSettings]::MinSdk)
    Test-Same 'targetSdkVersion' $manifest.TargetSdk ([Cryptforge.Editor.Release.ReleaseSettings]::TargetSdk)
    Add-Check 'not debuggable' (-not $manifest.Debuggable) $(if ($manifest.Debuggable) { 'android:debuggable="true": Play refuses it' } else { 'no android:debuggable' })

    # The ABIs and every arm64 library's LOAD segments (16 KB pages need 0x4000 or more).
    $abis = [Cryptforge.Editor.Release.AabLayout]::Abis($names)
    Test-Same 'ABIs' ($abis -join ',') 'arm64-v8a'
    $libraries = [Cryptforge.Editor.Release.AabLayout]::NativeLibraries($names, 'arm64-v8a')
    $aligned = $libraries.Count -gt 0
    foreach ($library in $libraries) {
      $libraryEntry = $zip.GetEntry($library)
      $target = Join-Path $work ([IO.Path]::GetFileName($library))
      [IO.Compression.ZipFileExtensions]::ExtractToFile($libraryEntry, $target, $true)
      $headers = & $readelf -lW $target 2>&1
      $aligns = @($headers | Where-Object { "$_" -match '^\s*LOAD\s' } | ForEach-Object { [Convert]::ToInt64((("$_").Trim() -split '\s+')[-1], 16) })
      [long] $minimum = if ($aligns.Count -gt 0) { ($aligns | Measure-Object -Minimum).Minimum } else { 0 }
      $ok = $aligns.Count -gt 0 -and $minimum -ge 0x4000
      if (-not $ok) { $aligned = $false }
      $libraryRows.Add(('  {0,-28} {1,12:N0} bytes  {2} LOAD, min align 0x{3:X}  {4}' -f [IO.Path]::GetFileName($library), $libraryEntry.Length, $aligns.Count, $minimum, $(if ($ok) { 'ok' } else { 'BELOW 0x4000' })))
    }
    Add-Check '16 KB alignment' $aligned "$($libraries.Count) arm64-v8a libraries, every LOAD segment aligned to 0x4000 or more"

    # Where the bytes are: the base module's folders, uncompressed and as stored in the bundle.
    $baseCompressed = 0
    $zip.Entries | Where-Object { $_.FullName.StartsWith('base/') } | Group-Object { $_.FullName.Split('/')[1] } | ForEach-Object {
      $raw = ($_.Group | Measure-Object Length -Sum).Sum
      $packed = ($_.Group | Measure-Object CompressedLength -Sum).Sum
      $baseCompressed += $packed
      $moduleRows.Add(('  base/{0,-14} {1,12:N0} bytes, {2,12:N0} compressed, {3} entries' -f $_.Name, $raw, $packed, $_.Count))
    }
    # Play's limit on the compressed download of the base module (with install-time modules) is 200 MB.
    Add-Check 'base module under 200 MB' ($baseCompressed -lt 200MB) ('{0:N0} bytes compressed in the bundle (bundletool get-size total gives Play''s own figure)' -f $baseCompressed)
  } finally {
    $zip.Dispose()
  }
} finally {
  Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

# The signature: which certificate, and whether it verifies.
$printcert = (& "$jdkBin/keytool.exe" @english -printcert -jarfile $Aab 2>&1 | Out-String)
$owner = if ($printcert -match 'Owner:\s*([^\r\n]+)') { $Matches[1].Trim() } else { $null }
$signerSha = if ($printcert -match 'SHA-?256\)?:\s*((?:[0-9A-F]{2}:){31}[0-9A-F]{2})') { $Matches[1] } else { $null }
$verify = (& "$jdkBin/jarsigner.exe" @english -verify $Aab 2>&1 | Out-String)
Add-Check 'signature verifies' ($verify -match 'jar verified') $(if ($verify -match 'jar verified') { 'jarsigner -verify: jar verified' } else { ($verify.Trim() -split "`n")[0] })
$debugKey = (-not $owner) -or ($owner -match 'CN=Android Debug')
$signingLine = if (-not $owner) { 'NOT FOR PLAY: unsigned' } elseif ($debugKey) { "NOT FOR PLAY: debug key ($owner)" } else { "upload key: $owner" }

# Cross-check: the release manifest Gradle merged for the last build (removed before each build, so it is this build's).
$crossCheck = 'skipped: the AAB is not the last build''s'
if ($isLastBuild) {
  if (Test-Path $mergedManifest) {
    $xml = [xml](Get-Content $mergedManifest -Raw)
    $sdk = $xml.manifest.SelectSingleNode('uses-sdk')
    $gradle = @($xml.manifest.GetAttribute('package'), $xml.manifest.GetAttribute('versionName', $androidNs), $xml.manifest.GetAttribute('versionCode', $androidNs),
      $(if ($sdk) { $sdk.GetAttribute('minSdkVersion', $androidNs) }), $(if ($sdk) { $sdk.GetAttribute('targetSdkVersion', $androidNs) })) -join ' '
    $bundle = @($manifest.Package, $manifest.VersionName, $manifest.VersionCode, $manifest.MinSdk, $manifest.TargetSdk) -join ' '
    Add-Check 'Gradle merged manifest agrees' ($gradle -ceq $bundle) "Library/Bee release merged manifest: $gradle"
    $crossCheck = $null
  } else {
    $crossCheck = "skipped: no $mergedManifest"
  }
}

$commit = (git -C $root rev-parse --short HEAD 2>$null)
$dirty = @(git -C $root status --porcelain 2>$null).Count
$unityVersion = (Select-String -Path "$root/ProjectSettings/ProjectVersion.txt" -Pattern '^m_EditorVersion:\s*(.+)$').Matches[0].Groups[1].Value
# Put back already, so the AAB and the checkout are both as they should be; a warning, because the in-memory restore did
# not hold and the pipeline wants a look.
if (-not $settingsPass) { Add-Check 'ProjectSettings unchanged' $false $settingsResult -WarnOnly }
$allPass = @($checks | Where-Object { $_.Status -eq 'FAIL' }).Count -eq 0
$warnings = @($checks | Where-Object { $_.Status -eq 'WARN' }).Count

$report = [Collections.Generic.List[string]]::new()
$report.Add('Cryptforge release build report (docs/61)')
if ($debugKey) { $report.Add("*** $signingLine ***") }
$report.Add("Written     $([DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss', [Globalization.CultureInfo]::InvariantCulture)) UTC")
$report.Add("Commit      $commit$(if ($dirty -gt 0) { " with $dirty uncommitted path(s)" } else { ', clean' }); Unity $unityVersion")
$report.Add("AAB         $Aab")
$report.Add(('Size        {0:N0} bytes ({1:N1} MB); SHA-256 {2}' -f $file.Length, ($file.Length / 1MB), $sha256))
$report.Add("Signing     $signingLine")
if ($signerSha) { $report.Add("Signer      SHA-256 $signerSha") }
if ($isLastBuild) {
  $report.Add("Build       $($summary['signing_note']); $($summary['settings_restored']) settings changed and restored; $($summary['unity_seconds']) s in Unity$(if ($summary['clean_build'] -eq 'true') { ', clean' })")
}
$report.Add("Settings    ProjectSettings/ $settingsResult")
$report.Add("Manifest    $($manifest.Package) $($manifest.VersionName) ($($manifest.VersionCode)), minSdk $($manifest.MinSdk), targetSdk $($manifest.TargetSdk)")
$report.Add("Permissions $(if ($manifest.Permissions.Count -gt 0) { $manifest.Permissions -join ', ' } else { 'none' })")
if ($crossCheck) { $report.Add("Cross-check $crossCheck") }
$report.Add('')
$report.Add('Checks')
foreach ($check in $checks) { $report.Add(('  {0}  {1,-30} {2}' -f $check.Status, $check.Name, $check.Detail)) }
$report.Add('')
$report.Add('Native libraries (base/lib/arm64-v8a, llvm-readelf -lW)')
foreach ($row in $libraryRows) { $report.Add($row) }
$report.Add('')
$report.Add('Base module')
foreach ($row in $moduleRows) { $report.Add($row) }
$report.Add('')
$report.Add('Not run: bundletool get-size total (Play''s download size per device) and bundletool build-apks (an install from the')
$report.Add('bundle). bundletool is not installed; downloading it is the owner''s call (docs/61 section 6).')
$report.Add('')
$report.Add("Result      $(if ($allPass) { 'PASS' } else { 'FAIL' })$(if ($warnings -gt 0) { " with $warnings warning(s)" })$(if ($debugKey) { " - $signingLine" })")

$reportPath = [IO.Path]::ChangeExtension($Aab, '.report.txt')
[IO.File]::WriteAllText($reportPath, ($report -join "`n") + "`n")
$report | ForEach-Object { Write-Output $_ }
Write-Output "Report: $reportPath"
exit $(if ($allPass) { 0 } else { 1 })
