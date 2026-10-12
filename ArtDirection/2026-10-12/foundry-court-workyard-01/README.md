# Foundry court paving and workyard

2026-10-12. Baseline `c4f66e6db17a3ad74b240f35c414d2bdbaf3ebab`.

The centre court and its two approaches now replace the ordinary paving within three exact, disjoint footprints. All old floor layers are cut out, including the bridge portion, while source grain and sampled lighting are retained at each clipped boundary. The radius5.05 medallion at (-3,-5) contains an octagonal keystone, eight fitted fan stones and twelve broad radial outer stones. Full-width4.18 approach fields use broad transverse flags and alternating fitted pairs. Mortar, dressed shoulders and faces partition each replacement; they do not cover another complete coplanar bed. Existing brass dimensions, map outline, camera and walking geometry stay fixed.

The western crane gains a grounded native cast-iron machinery base on its original scenic deck. A sloped rear casing, joined cheeks and header surround an exposed cable drum. The active baked-crane path and fallback both receive the same base; the existing mast card, upper pivot, hoist chain and load retain their anchors. One existing iron batch and the original depth owner are reused. No runtime texture, material, renderer, per-frame updater, gameplay, encounter, scene or save change is included.

Three helpers divided court paving ownership, live crane machinery and independent review. Root captured sixteen fresh Before images on unchanged baseline c4f66e6 before authorizing runtime edits. All paired cameras and actor positions remain fixed. Root alone runs Unity and builds Android after source freeze; resource counts and scene captures are not physical-device observations. The new court removes underlying regular and bridge-floor triangles inside the authored paving domains before emitting their replacement. The crane housing is connected to the active baked-crane path, rather than changing only a missing-resource fallback.

Initial geometry preflight retained three failures. Actual triangle witnesses showed the two new court ownership checks sampled a shared half-unit lattice edge; their starting angle was corrected while retaining strict one-owner, area and tolerance assertions. The crane failure was real: a far bearing and deep header overpainted most of the drum. Far/near order and a physically seated raised soffit corrected the opening without reducing the visibility threshold. A later newly introduced floor budget compared the complete replacement with a raw base that omitted the old medallion and shoulders. It was replaced by separate clipping-overhead and inherited component bounds; the unchanged whole-region budget is checked on the actual scene. First actual capture measured32 renderers/161850 vertices against160781, and four merged floor/gate probes each had exactly one paving owner.

The first actual crane finish was rejected visually despite passing geometry checks: clamped UVs collapsed upper faces onto one texture row, creating corrugated streaks. The local casting sampler is corrected before final acceptance, with a new UV-area regression. Initial failure XMLs, triangle diagnostics, first-capture images and review notes remain in validation.

Region 32 renderers/161850 vertices versus 160781 (+1069); distant geometry 3572 versus 3572. No new runtime texture, material, renderer or per-frame updater. Resource counts are not physical-device frame-time measurements.

## Verification

438 .NET / 552 EditMode / 159 PlayMode = 1,149 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 608 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,291,532 bytes; SHA256 D3B7D53094316D3B89BA50C515D78A5BA7194ACC7FDC889D2E959F1DACE26804; NOT INSTALLED.

Reference fidelity remains unfinished. This package changes visual mesh composition inside the accepted walking outline; camera, gameplay and encounter data stay unchanged. Existing immortal-crowd waypoint congestion remains unresolved; evidence remains in ../foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before any later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCourtWorkyardProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Sixteen paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
