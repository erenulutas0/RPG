# Foundry forecourt layout

2026-10-12. Baseline `4c86f7ae3dff5ab56e2f852080226bc696f41bd8`.

The main court now assigns four design units from the western working strip to the entrance forecourt: West -36 to -32 and South -14 to -18. The bounding rectangle changes from 50 by 32 to 46 by 36; the strip below the fixed furnace pit grows from 16 to 20 units, while the western strip decreases from 20 to 16. These are design coordinates, not measurements extracted from the concept. The projected outer span and comparison camera stay unchanged. Pit, gate landing and bridge endpoints retain their anchors; more of the same bridge now meets the expanded court.

Perimeter masonry, southern bastions, finials, lantern pools and exterior chains follow the new boundary. The two bridge lantern rows and three below-deck supports fit the shorter exposed approach. The south brass guide and the stone packer's forced joints share one position. The long hanging chain uses an actually emitted, unscreened major-pier socket with deterministic tie-breaking. Western equipment and interior work islands use their existing derived offsets. No scene, encounter, balance, profile, camera or runtime texture change is included; the geometry change can affect encounter spacing and travel, so it is not balance acceptance.

Three helpers divided shared-map geometry, boundary architecture and independent review. Root took sixteen fresh Before captures on unchanged baseline 4c86f7a before authorizing any runtime edit. The comparison uses fixed cameras and scale, including a fixed crane camera even though its assembly moves. Root updated actual rendered-floor probes for removed west ground, the gained southern band, both bridge overlaps and their outside wings. Actual scene checks also require the emitted south pier plate and attached hanging chain. Source review caught the moved brass guide still disagreeing with packed-stone joints; both now share ForecourtGuideV. Unity and Android verification are run only by root on frozen Assets.

The first After launch caught test-only CS0117 errors: the separate Unity test assembly could not directly reference internal bridge arrays. The new test now uses the same reflection access as its other geometry observations; no production visibility or assertion was weakened. Diagnostics are retained and the final source was re-frozen before rerunning Unity.

The additional stationary immortal-crowd end premise failed. A retained bounds-only A/B using unchanged movement code reproduces distant waypoint queues in the old layout too. Diagnostic cases remain separate failed evidence; no production solver was changed and no pre-existing test was weakened or ignored. A separate real HeroMotion plus ten-body PackMotion traversal checks every swept path and spacing over 3600 frames and finishes with all ten across the join. See crowd-diagnostic.md for exact scope, counterexamples and reproduction.

Region 32 renderers/161298 vertices versus 164217 (-2919); distant geometry 3572 versus 3572. No new runtime texture, material, renderer or per-frame updater. Resource counts are not physical-device frame-time measurements.

## Verification

438 .NET / 535 EditMode / 159 PlayMode = 1,132 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 605 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,264,048 bytes; SHA256 3BE71A740AE61ADA3816815C0D05FCCFAE20E408D1F9AA7AFDD6D20BFA688CFB; NOT INSTALLED.

Reference fidelity remains unfinished. The forecourt distribution changes but the projected outer span and camera remain fixed. Existing immortal-crowd waypoint congestion remains unresolved; see crowd-diagnostic.md for baseline/candidate evidence. Moving-hero pack checks do not establish general crowd convergence or balance acceptance. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before any later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryForecourtLayoutProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Sixteen paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
