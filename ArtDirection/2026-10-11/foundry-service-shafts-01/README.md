# Foundry service shafts

2026-10-11. Baseline4ac6665aa72e061ab03c86efcb83f2b86df51e5e.

## Result

Two existing northwest distant furnace shafts receive narrower necks, joined dressed shoulders, real recessed neck panels with side returns and hollow exhaust crowns. The original leaning buttress heads move inward to meet the narrowed neck. The mouths have low throats, inner walls and separate annular rims instead of solid roof caps. Existing lower furnace body/fire, other distant variants, atmospheric fade, materials, sorting, camera, navigation, gameplay and saves remain unchanged.

Three helpers split runtime architecture, new regression tests/source review, and comparison tooling/independent review. Root alone ran Unity. Nine baseline4ac6665 camera views were copied from tracked files and Git-object/SHA256 verified; one new service-shaft close view is explicitly after-only. Actual captures pause combat and hide HUD; they are not phone screenshots. Initial EditMode found five failures in the new tests: raw intermediate Color scaling was incorrectly required to satisfy final alpha bounds, and the combined test omitted the production arch-alpha reset. The test setup was corrected; complete rendered builders still require finite alpha in [0,1]. Runtime was unchanged after graphics capture. Initial failed XML and diagnosis are retained, followed by a focused retest and complete EditMode pass. PlayMode completed against the same final runtime. See the saved source and image reviews for acceptance and limitations.

Region32 renderers/166468 vertices versus165392 (+1076); distant geometry2496->3572. The combined distant vertex ceiling is intentionally revised from2500 to3600 for two complete hollow crowns/recessed necks; all existing lifetime, material, rank, fade and bounds assertions stay intact. This is a measured resource-budget decision, not a frame-time result. No new texture, material instance or per-frame updater.

The local shaft silhouettes and construction are improved; full reference fidelity is unfinished. The broad dark sloped shoulders read more like pyramidal exhaust hoods, a visible material simplification; lower shaft bodies remain plain. Whole-map improvement is modest. Device movement/crowd/HUD/shimmer/midrange acceptance remains open. No ADB or fresh phone observation. The earlier concrete install/restart decision remains pending. Future deployment must inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Region encounter/boss progression remains deferred by the art-first direction.

## Verification

425 .NET / 497 EditMode / 159 PlayMode = 1,081 distinct suite cases, zero failures/skips. Final isolated compile zero warnings/errors; Android-target graphics capture passed. PASS: 595 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit0 with no C# or shader diagnostics. APK 36,248,250 bytes, SHA256 B8225B648A17854B3EB00D07B5A7CF4EDFC0B86DC32D080EB2353157B32C8DC7; NOT INSTALLED.

Before images are immutable historical evidence. Matched views: full-map, broad-yard, skyline-detail, expanded--26-15, casting-portrait, crane-detail, stock-detail, overview and expanded--27-6. New service-shaft-detail is after-only: Project(-5,39)+(0,-14.5), orthographic size12.5,1200-square. The approved reference is concept art. Graphics cases are separate from the suite total. Validation retains XML/TRX, provenance, reviews, build hash and final compile/static output.

## Reproduction

With Unity closed, from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryServiceShaftProof.ps1
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The proof launcher temporarily places its capture script/meta in the PlayMode assembly and removes them in finally. Only after images are captured from the changed source.

## Next work

Next substantial visual review: compare the whole court, pit and gate against the reference at matched scale, then prioritize the largest remaining material/structural mismatch rather than another isolated trim pass. Keep the complete new loading area and service shafts together for the next authorized phone review.
