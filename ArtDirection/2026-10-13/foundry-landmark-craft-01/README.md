# Foundry landmark craft

Started 2026-10-13; verified 2026-10-14. Baseline `f0ce77095ba814938f24f588a8a8173323699d9f`.

The existing pointed gate bay frames now recede into a shallow splayed reveal. Their inner arrises consume one quarter of the existing niche depth, and connected returns shorten to meet the same stations. Backing, outer frame, caps, bearing contacts, door and tower envelopes remain fixed. A bounded directional scalar across the existing splay vertices darkens the receding far jamb and head while retaining the outer bearing and sill illumination. It preserves local forge-light RGB relationships. The changed geometry replaces the old flat frame; it adds no decorative layer or vertices.

The working apron gains local material use beyond the trolley loading end and the stock bench, plus a restrained scrubbed receiving threshold. Existing emitted stone samples receive a bounded neutral scalar, preserving world grain, forge-light channel ratios, geometry, UVs and triangle ownership. The open transfer centre and unused repair stone remain neutral. No texture, material, renderer or per-frame updater is added.

This package changes no map bounds, camera, walking geometry, gameplay, encounters, progression or saves. It is a local landmark/material improvement, not completion of the approved reference.

Two helpers owned disjoint gate-recess and service-apron source/test files. A third independently reviewed source and actual Before/After views, with crossed source reviews by the implementation helpers. Root alone runs Unity and Android. Twenty fresh Before views were captured with clean Assets at f0ce77095ba814938f24f588a8a8173323699d9f before editing. Camera and actor setup stays identical between phases; the stock and casting closeups are included in both. The cloned budget report retained a stale short baseline label; baseline-label-note.md records the corrected descriptive label while preserving the original report and sealed baseline hashes.

Seven added EditMode cases cover the two production gate profiles and actual apron material samples. Gate checks retain shared splay/return stations, unchanged backing, one frame-face owner, visible backing, noncollapsed mineral UVs and inherited vertex bounds. Apron checks require referenced nondegenerate rendered triangles, exact geometry/UV identity, shared world-position treatment across parents, original light hue and neutral unused paving.

Actual scene captures pause combat and hide the HUD. They verify local visual composition and measured resource counts, not physical-device motion or performance. No ADB, installation or phone-save operation is included.

The first captured gate splay was rejected by root and the independent reviewer because the geometric recession alone remained visually flat in the unlit renderer. Original images and source hashes remain in validation/first-capture. The final pass adds bounded direction-dependent shading to the same frame pieces and extends the existing two new gate cases with referenced colour/geometry checks; case count is unchanged. Apron runtime is unchanged from the first capture. Final graphics are recaptured from the newly frozen source.

The first full verification passed592/593 EditMode and159/159 PlayMode, so the APK was skipped. The new backing-centre witness was behind the same jamb in the original flat geometry. Two independent reconstructions established that this was an invalid visibility precondition, not new overpainting. The test-only repair measures the independently clipped projected rear/throat intersection: full backing coverage, no later overpaint, and containment of the old visible domain at the same strict area tolerance. The complete targeted gate class then passed22/22. Runtime files remain byte-identical to the accepted directional image pass; only FoundryGateRecessTests.cs changed. The failed XMLs/source/captures and diagnosis are retained in first-gate-check and backing-visibility-diagnosis.md. Final images and full suites are regenerated from the new source freeze.

Region 32 renderers/162361 vertices versus 162361 (+0); distant geometry 3572 versus 3572. No new runtime texture, material, renderer or per-frame updater. Resource counts are not physical-device frame-time measurements.

## Verification

438 .NET / 593 EditMode / 159 PlayMode = 1,190 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 613 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,308,109 bytes; SHA256 D5D31767220EADA46B523957EB97267FFCE5D68805698694D2CC91E5FB2C9793; NOT INSTALLED.

Reference fidelity remains unfinished. This package refines splayed gate stone reveals and local use on the existing working apron; camera, gameplay and encounter data stay unchanged. Existing immortal-crowd waypoint congestion remains unresolved; evidence remains in ../../2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before any later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryLandmarkCraftProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Twenty paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
