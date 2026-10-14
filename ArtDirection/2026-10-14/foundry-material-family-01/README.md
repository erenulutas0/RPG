# Foundry material family

Verified 2026-10-14. Baseline `d4a3ecbab81759159633e27d65533c67d1a30f27`.

Three coordinated improvements: a versioned V8 paving bitmap replaces long shared streaks with restrained short mineral grain, with a neutral material gain calibrated to preserve the preceding mean value; the complete native moulding station gains oak flasks, real recessed ingot/bearing cavities, raked cast supports and properly seated clamp screws while recovering 395 vertices; exposed near-island and primary bridge bearings gain a more neutral value hierarchy while sheltered returns and voids retain their existing tones. The original walking layout, continuous paving UVs, exact flag ownership, forge-light sampling, material batches and actor depth are retained. Actual portrait benefit is clearest at the moulding station and across the floor; full-map equipment gain remains modest. Regular native stone and the mismatch with softer baked equipment remain unfinished reference gaps.

Region 36 renderers / 172340 vertices versus 172735: -395. Distant 3572 is included and unchanged. Ceilings remain 174000 / 3600, leaving 1660 / 28. V8 replaces the one paving texture at the same loaded 1024x1024 ASTC6 / 11-mip / non-readable budget; V7 remains versioned source history. No additional runtime texture layer, material, shader, renderer batch or updater. Static counts do not establish device performance.

## Verification

438 .NET / 691 EditMode / 160 PlayMode = 1,289 accepted suite executions, zero failures/skips. Focused Foundry 289/289 passed separately. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 636 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 626F66A93A70DA7981C8DC53A6E0DE140DBD54A9D341A15B9297932C969602D0; NOT INSTALLED.

Root integrated the built-in imagegen V8 paving replacement, neutral brightness calibration, loaded importer assertions, matched capture, gallery and verification. Two implementation helpers shared the native moulding station and exposed fortress-bearing family; a third reviewed source, source bitmap statistics and actual images independently. The bench test work was delegated to the fortress helper after its runtime scope froze, with disjoint file ownership.

Source review caught a clamp screw that rested beyond the flat oak cap. Its axis was moved 0.08 design units onto the complete cap with 0.01 margin on each side, without geometry cost. The new bench test verifies actual flat-cap and screw-foot vertices. An initial isolated compile found a generic test collection accessor typed as a scalar; repaired before Unity. First focused run passed 288/289: the new fortress colour oracle selected the first underlying mortar triangle instead of the last visible course. Reversing the test sampling order preserves the original baseline expected RGB and strict 1e-6 tolerance; runtime was unchanged. Original failed XML/source and compile diagnostics are retained in rejected-tests. Baseline metadata also records an inherited diagnostic-label correction; sealed Before images remain unchanged.

Final frozen Assets are required to remain unchanged through accepted After capture, full suites and Android build. Physical-device acceptance remains pending.

## Evidence boundary

Twenty-one matched actual Unity views use identical camera/actor staging, paused combat and hidden HUD. Reference is concept art.

This is an incremental material/equipment improvement, not reference parity or a new layout. Map bounds, camera, navigation, equipment footprints, gameplay and encounters remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved (ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md). No phone control, installation or save/telemetry change. Device motion, HUD, shimmer and midrange performance remain unverified. The earlier concrete restart/install decision remains pending; later deployment must inspect current state and preserve current original saves/telemetry, never restore historical snapshots.

## Reproduction

Run with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryMaterialFamilyProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before pins exact baseline HEAD and unchanged Assets. See validation/baseline-label.md for a retained inherited text-label error; sealed images and metadata identify the real baseline. Image-generation prompt and provenance are in source/.
