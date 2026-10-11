# Verification — workyard identity

- Baseline: 10d5e013b04dcedbed0945a1bbe232bf998488df. Before captures reused unchanged and hash-checked; provenance-sha256.txt records image and unchanged source-import identities.
- Final pure .NET: 409 passed, 0 failed/skipped.
- Final UnityCompileCheck: 0 warnings, 0 errors. Initial compiler check caught unsupported Is.AnyOf in the lightweight NUnit shim; equivalent explicit rank assertion fixes this without changing test intent.
- EditMode: 446 passed, 0 failed/skipped; Unity exit0.
- PlayMode: 154 passed, 0 failed/skipped; Unity exit0. Region coverage retains exact image-card geometry, lower/tall band counts, owner/depth/UV/footprint/height bounds, no cross-owner triangles, renderer ceiling, mesh/material cleanup and borrowed texture lifetime.
- Total: 1,009 distinct suite cases, all passed. No new test methods; existing region contract was strengthened.
- Graphics: two successful after captures; first visually rejected, final accepted by root and independent reviewer. Baseline XML is historical, not another execution.
- Final region: 32 renderers, 118,709 vertices (baseline118,537, +172/about0.15%). Resource count only, not a device performance result.
- Both refreshed resources: 512x512 ASTC6x6, 10 mipmaps, non-readable. Existing metadata/GUIDs unchanged.
- Static: 585 unique asset/folder GUIDs, 1099 package GUIDs, 568 scene objects/components; valid references/JSON.
- Final graphics/EditMode/PlayMode/build logs: no C# error/warning or Shader error matches.
- Android build exit0. Actual packaged APK: 31,210,207 bytes, timestamp2026-10-11 12:51:39 Europe/Istanbul. SHA256 A2F326D1743A401D508E6AAAE7C58C2167D4E97ED525E0F6356CB2D657B32DD7. The Unity build-report byte count is not the final packaged APK size.
- Browser: actual gallery opened, view selector/Before/After/reference toggle verified, displayed images loaded. These are actual Unity views with matched cameras, not phone captures.

NOT INSTALLED. No ADB action/fresh phone observation. Earlier concrete owner-run restart/install decision remains pending. Historical package021b95f and Room2/74HP/8unbankedgold must not be treated as current state. On authorization inspect fresh, back up current original files, install/hash-check latest APK, and preserve current saves/telemetry. Never restore stale saves. Accumulated device/crowd/midrange/first-ramp acceptance remains open.

Reproduce: dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release; dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo; pwsh -NoProfile -File Tools/ArtReview/Run-FoundryWorkyardIdentityProof.ps1 -Phase after; pwsh -NoProfile -File Tools/Run-UnityTests.ps1; pwsh -NoProfile -File Tools/Verify-Project.ps1. Unity operations run sequentially with no concurrent Assets mutation.