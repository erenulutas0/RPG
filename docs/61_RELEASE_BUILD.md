# 61 - The release build (runbook, 2026-11-19)

The release pipeline of docs/59 section 3 item 4: an Android App Bundle (AAB) for Google Play, built beside the prototype's
development APK and never instead of it. This document is the runbook: how the owner makes the upload key, how Play App
Signing uses it, how versions are numbered, what the report checks, and what the closed test still needs.

| Piece | Where |
|---|---|
| The build (Unity Editor, batch) | `Assets/_Project/Scripts/Editor/AndroidReleaseBuild.cs`, menu *Cryptforge > Build Android Release (AAB)* |
| Its pure rules (version file, signing environment, settings plan, AAB manifest, JAR signature) | `Assets/_Project/Scripts/Editor/Release/` |
| The wrapper and the report | `Tools/Build-Release.ps1` |
| The version | `Tools/Release/version.txt` (committed) |
| The tests | `ReleaseBuildTests` (EditMode and `Tools/CombatChecks`), `ReleaseBuildSettingsTests` (EditMode only) |
| Outputs | `Builds/Release/` (ignored by Git, as are `*.aab`, `*.keystore` and `*.jks`) |

## 1. What the build makes

- **An AAB**, not an APK, and **not a development build**: `BuildOptions.None`, `EditorUserBuildSettings.development = false`,
  `androidBuildType = Release`, so Gradle runs its release build type.
- **IL2CPP** (Release compiler configuration), **arm64-v8a only**, portrait.
- **targetSdk pinned to 36** and **minSdk 26** for the build: the project's "automatic" target would follow the highest
  installed SDK, and a missing SDK could lower it.
- **The version** from `Tools/Release/version.txt` (section 4). The build bumps nothing.
- **The package** `com.cryptforge.prototype`, behind one constant (`ReleaseSettings.DefaultPackageName`) and an optional
  `-PackageName` for a trial build. **The permanent name is chosen with the owner before the first upload: Play never
  lets it change afterwards** (docs/59 section 2).
- **Signed with the upload key** when the four `CRYPTFORGE_*` variables are set (section 2), otherwise with Unity's debug
  key: the file is then named `...-debugkey.aab` and the report says **NOT FOR PLAY: debug key**.

**ProjectSettings/ is unchanged on disk after a build.** Every PlayerSettings and EditorUserBuildSettings value the build
changes is in one table (`AndroidReleaseBuild.Settings`, 14 entries, the same names as `ReleaseSettings.Names`). The
build saves the whole table, applies the plan only through it, and restores it in a `finally` block; nothing calls
`AssetDatabase.SaveAssets`. The EditMode suite holds the table to the plan's list and round-trips every entry. The
wrapper also hashes every file under `ProjectSettings/` before and after the Unity run; should one differ all the same, it
puts the file back from a copy taken before the run and the report shows a **WARN** naming it.

**Why Unity never sees the upload key.** Unity 6 writes a custom keystore's passwords in plain text into `Library/Bee`:
the build program's input data (`Player*-inputdata.json`, field `KeystorePassword`) and the generated
`launcher/build.gradle` (`storePassword '...'`, the template in `Unity.Android.Gradle.dll`), both checked on 2026-11-19.
So the build keeps `useCustomKeystore = false`. Unity signs with its debug key, then `AabSigner`:

1. checks the upload key before the build: `keytool -list ... -storepass:env CRYPTFORGE_KEYSTORE_PASS`, which fails in
   seconds on a wrong password or alias;
2. removes the debug signature (`META-INF/MANIFEST.MF` and the `.SF`/`.RSA` pair; every other entry keeps its bytes);
3. signs with `jarsigner -sigalg SHA256withRSA -digestalg SHA-256 ... -storepass:env CRYPTFORGE_KEYSTORE_PASS
   -keypass:env CRYPTFORGE_KEY_PASS`, the JAR signature Play expects on a bundle;
4. verifies it (`jarsigner -verify`) and checks that the signed file's certificate is the upload key's.

The passwords stay in the environment and reach `keytool` and `jarsigner` by variable name. No password is in a command
line, a file, the Unity log or the report.

## 2. The upload key (the owner, once)

The owner makes the key, keeps it and its password **outside the repository**, with a backup. Nobody else creates a
keystore, and no agent ever sees the password.

1. Pick a folder outside `E:\MobileGame`, for example `D:\CryptforgeKeys\`.
2. Run Unity's own `keytool` (no Java install needed). It asks for the password twice; type it there, never on the command
   line, so it stays out of the shell's history:

   ```powershell
   & 'E:/Unity/Editors/6000.0.65f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe' -genkeypair -v `
     -keystore 'D:/CryptforgeKeys/cryptforge-upload.jks' -storetype PKCS12 `
     -alias cryptforge-upload -keyalg RSA -keysize 4096 -validity 10000 `
     -dname 'CN=Cryptforge upload key, C=TR'
   ```

   - RSA 4096, valid about 27 years; the pipeline signs with SHA256withRSA, so keep the key RSA.
   - A PKCS12 keystore has one password for the store and the key: `CRYPTFORGE_KEY_PASS` is the same value.
   - The certificate's name is seen by Google only (Play signs the installed app with its own key, section 3), so
     `-dname` may stay this plain.
3. **Back it up now:** the `.jks` file in two places (for example an encrypted USB stick and the password manager's
   file attachment), and the password in the password manager. Losing the upload key is recoverable (section 3) but takes
   days; the repository never holds either.
4. Note the key's SHA-256 fingerprint, shown by the command above (or by
   `keytool -list -v -keystore D:/CryptforgeKeys/cryptforge-upload.jks -alias cryptforge-upload`). The report prints the
   signed AAB's fingerprint, and Play Console shows the upload certificate's; all three must match.

**For each build session** (PowerShell; nothing is stored, `setx` is not used):

```powershell
$env:CRYPTFORGE_KEYSTORE_PATH = 'D:/CryptforgeKeys/cryptforge-upload.jks'
$env:CRYPTFORGE_KEY_ALIAS = 'cryptforge-upload'
pwsh -File Tools/Build-Release.ps1 -AskPasswords -BumpVersion
```

`-AskPasswords` asks for both passwords (masked; Enter at the second reuses the first), puts them into the build's own
environment and removes them when Unity exits. Setting `$env:CRYPTFORGE_KEYSTORE_PASS` and `$env:CRYPTFORGE_KEY_PASS`
by hand works too; then `Remove-Item Env:CRYPTFORGE_KEYSTORE_PASS, Env:CRYPTFORGE_KEY_PASS` afterwards. Set all four or
none: two or three set is an error that names the missing ones, never a quiet debug-key build.

## 3. Play App Signing

Every new app on Play uses Play App Signing. There are two keys:

- the **app signing key** is Google's: Play signs every APK it delivers with it. At the first release, accept *Google
  generates and protects the key* (the default). It never leaves Google and nobody here has to guard it.
- the **upload key** is ours (section 2): it only proves to Play that an upload comes from us. Play registers its
  certificate from the first AAB uploaded.

Consequences:

- **A lost or leaked upload key is not the end of the app:** the account owner asks for an *upload key reset* in Play
  Console (*Test and release > App integrity > App signing*), sends a new certificate made with `keytool -export -rfc`, and
  waits for Google (typically a few days). Avoid it all the same: backups (section 2, step 3).
- An SDK that asks for the app's SHA-1 or SHA-256 (Play Games, Firebase, Google Sign-In) needs the **app signing
  certificate** from *App integrity*, not the upload key's.
- A debug-key AAB is refused by Play once the upload certificate is registered, and should never be the first upload:
  Play would take the debug certificate as the upload key. The `-debugkey` name and the report's stamp are there for this.

## 4. The versionCode scheme

`Tools/Release/version.txt` holds two keys, and only those (an unknown or repeated key is an error, so a typo cannot keep
an old value in force):

```
versionName=0.1.0
versionCode=1
```

- **versionCode is a counter:** one higher for every AAB that may be uploaded to Play, on any track. Play keeps every
  code it has seen, even from a discarded draft, so a code is **never reused and never lowered**. Gaps are harmless.
  Ceiling 2,100,000,000. Not derived from versionName: a hotfix of the same name still needs a new code.
- `pwsh -File Tools/Build-Release.ps1 -BumpVersion` raises it by one before the build and **undoes the bump when the build
  fails** (nothing was built, so nothing can have been uploaded). The build itself bumps nothing; local trial builds
  without `-BumpVersion` reuse the current code.
- **Commit the bumped file** with the release candidate (`release: 0.1.0 (2)`), so the code an uploaded AAB carries is in
  Git.
- **versionName** is MAJOR.MINOR.PATCH, edited by hand: 0.x through the closed test, 1.0.0 at the Turkish release, PATCH
  for fixes, MINOR for content.

## 5. The report

`Tools/Build-Release.ps1` runs Unity in batch, then reads **the AAB itself**, the file that is uploaded, and writes
`Builds/Release/Cryptforge-<name>-<code>[-debugkey].report.txt` (also printed). Exit code 0 when no check fails.

| Check | How |
|---|---|
| package, versionName, versionCode | the bundle's `base/manifest/AndroidManifest.xml` (aapt2's protobuf XML, read by `AabManifest`; `aapt2 dump` cannot read a bundle's manifest), against the build's plan |
| minSdk 26, targetSdk 36 | the same manifest |
| not debuggable | the same manifest: Play refuses a debuggable app |
| ABIs = arm64-v8a | the bundle's `base/lib/*/` folders |
| 16 KB alignment | every `base/lib/arm64-v8a/*.so`, extracted and read with the NDK's `llvm-readelf -lW`: each LOAD segment's alignment must be 0x4000 or more |
| base module under 200 MB | the compressed size of `base/` in the bundle, an approximation of Play's limit on the install-time download |
| signature verifies | `jarsigner -verify`; the signer's owner and SHA-256 from `keytool -printcert -jarfile` |
| Gradle's merged manifest agrees | `Library/Bee/.../merged_manifest/release/processReleaseMainManifest/AndroidManifest.xml`, removed before each build so it is this build's; a cross-check only, skipped when absent |
| ProjectSettings unchanged | SHA-256 of every file before and after (WARN, put back, when not) |

Also listed: size and SHA-256 of the AAB, each library's size, the base module by folder, the manifest's permissions (for
the data-safety form, docs/59 item 6), the commit and whether the tree was clean.

**Not run yet, optional later checks** (bundletool is a download, and downloads are the owner's call):

- `bundletool get-size total --apks=...` gives Play's own download size per device configuration;
- `bundletool build-apks --connected-device` then `install-apks` installs the bundle on the S23 exactly as Play would split
  it (the closest local test of the AAB).

`-ReportOnly` writes the report again without Unity; `-ReportOnly -Aab <path>` inspects any AAB.

## 6. The command the lead runs (main checkout)

No Editor may have `E:\MobileGame` open and no other Unity batch run may be in flight (the wrapper refuses otherwise).
First the new EditMode tests, then the build:

```powershell
cd E:\MobileGame
pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -Filter "Cryptforge.Tests.ReleaseBuildTests;Cryptforge.Tests.ReleaseBuildSettingsTests" -SkipApk
pwsh -File Tools/Build-Release.ps1
```

With no `CRYPTFORGE_*` variable set this makes `Builds/Release/Cryptforge-0.1.0-1-debugkey.aab` and its report, stamped
NOT FOR PLAY: that is the expected result until the owner's upload key exists. Then:

- `git status ProjectSettings` must be clean, and the report's Settings line must say *unchanged*;
- every check PASS; the alignment rows list libunity, libil2cpp, libmain, libswappywrapper and libc++_shared, as
  docs/59 section 2 measured on the APK;
- note the AAB's size beside the APK's 60,957,067 bytes of docs/23.

Expect the first release build to take longer than a prototype build: IL2CPP's Release configuration has its own cache,
and switching back to the development APK afterwards rebuilds the Development one. `-Clean` rebuilds everything when a
build looks stale. The Unity log is `Builds/Release/Cryptforge-<name>-<code>.unity.log`; exit 198 means the license
lapsed (docs/23).

## 7. What the closed test still needs

From docs/59 sections 3 and 4, in the order they block the first upload:

1. **The permanent package name**, chosen with the owner, then `ReleaseSettings.DefaultPackageName` changed in one place.
2. **The owner's Play Console account** (personal, verified on an Android 10+ phone) and the app created in it with that
   name.
3. **The upload key** (section 2) and a build with it: no NOT FOR PLAY stamp, every check PASS, versionCode bumped and
   committed.
4. **The Play Console forms:** store listing (Turkish first), content rating (IARC), data safety (the report's permissions
   and the SDK list), target audience, ads declaration, the privacy policy URL (the site is decided later, docs/59
   decision 4) and the reviewer's path to the reward offer (docs/59 item 6).
5. **The SDKs** (docs/59 item 5) when the owner's accounts exist: after each, a release build and its report again, for
   the 16 KB rows and the size.
6. **Native debug symbols:** Play Console warns when a bundle with native code has none, and a crash in the closed test is
   then hard to read. Unity 6 can produce them (`UnityEditor.Android.UserBuildSettings.DebugSymbols`, a `symbols.zip`
   to upload or symbols inside the bundle); decide before the first upload. The pipeline does not change it yet.
7. **The closed track:** at least 12 testers opted in for 14 continuous days (docs/59 section 4); Play's pre-launch report
   runs on every upload and is worth reading.

## The first run (2026-11-23, the lead, in the main checkout)

The command was `pwsh -File Tools/Build-Release.ps1`, run with no CRYPTFORGE_* variables set. It produced
`Builds/Release/Cryptforge-0.1.0-1-debugkey.aab`, stamped **NOT FOR PLAY: debug key**.

- **Size:** 57,309,047 bytes (54.7 MB); the base module, compressed, is 57,261,131 bytes, under the 200 MB limit.
- **Manifest:** com.cryptforge.prototype 0.1.0 (1), minSdk 26, targetSdk 36.
- **16 KB alignment:** every LOAD segment of the five arm64-v8a libraries is aligned to 0x4000 or more (libunity,
  libil2cpp, libmain, libswappywrapper, libc++_shared).
- **Signature:** `jarsigner -verify` reports "jar verified" (debug key).
- **Settings:** 14 changed and restored. ProjectSettings/ was unchanged on disk afterwards; `git status` was clean.
- **Time:** 975 s in Unity, the first IL2CPP release build.
- **Not run:** bundletool's size and APK checks. They need a download, which is the owner's call.

