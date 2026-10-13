# Foundry surface finish

2026-10-13. Baseline `525d7594e7c25cdfe1da8e2b3162ab587918dbaf`.

The active paving surface now uses versioned FoundryPavingV6, a calmer jointless slate diffuse generated with the built-in image_gen tool. The selected PNG is copied byte-for-byte into the project; prompt and provenance are retained in texture-prompt.txt and paving-source-integration.json. Existing geometry still owns stone boundaries, chipped outlines, joints, metal inlays and authored forge light. The material tint, UV span, shader and Android import contract are unchanged.

Gate header sidewalls use broad dressed stones partitioning the original faces into mortar, arrises and stone interiors. Coping, closed niche returns and caps use bounded mineral UVs; their original exterior envelope and bearings are preserved. A restrained local crown/bearing treatment multiplies the existing light rather than replacing it. MasonryV4 remains the existing five-course atlas; no global wall texture replacement is made.

This is a combined surface-quality pass, not reference parity. The walkable outline, gameplay camera, encounters, collision, progression and shaders are unchanged. Further authored variation in landmark stone and machinery remains open.

Three helpers handled material binding/provenance, bounded gate surface work and independent visual acceptance. Root captured sixteen fresh Before views at unchanged HEAD 525d7594e7c25cdfe1da8e2b3162ab587918dbaf before authorizing runtime edits. Only root ran Unity. Source peer review and independent actual-image review were separate from implementation.

The first full EditMode preflight passed 567 of 569. Two new UV-area assertions caught projected slivers from mathematically zero-area mortar strips: boundary endpoints had moved only along an edge, but the old condition still emitted a quad. A focused diagnostic rerun recorded the actual world/UV coordinates and approximately 6e-7 apparent world areas. Production geometry now emits boundary mortar only with positive perpendicular inset. No test threshold was relaxed. Both failed reports are retained. The repaired 569-case preflight passed, followed by the final full verification workflow.

Five added EditMode cases cover three actual header proportions for face ownership and area, noncollapsed bounded cap/recess UVs, and localized contact shading preserving source RGB light ratios. Existing movement, resource, scene and court ownership coverage remains in the full suites. The selected PNG is unmodified built-in imagegen output; original V5 remains available as a versioned sibling. Assets and pre-Unity evidence were hashed before the final capture and complete suites/APK build.

The captures preserve camera and actor positions. They establish visual composition, resource imports and geometry budgets, not device performance or interactive acceptance. No ADB operation or device installation was performed.

Region 32 renderers/159454 vertices versus 158653 (+801); distant geometry 3572 versus 3572. One versioned paving texture replaces the existing active texture; import format and maximum resolution are preserved. No new material, renderer or per-frame updater. Resource counts are not physical-device frame-time measurements.

## Verification

438 .NET / 569 EditMode / 159 PlayMode = 1,166 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 612 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,300,736 bytes; SHA256 D8C3D2B0E284A57D74DBD9CB5EB3707DA934805C4966C8C851CE3E07765369CD; NOT INSTALLED.

Reference fidelity remains unfinished. This package changes the active paving diffuse and bounded gate surfaces inside the accepted walking outline; camera, gameplay and encounter data stay unchanged. Existing immortal-crowd waypoint congestion remains unresolved; evidence remains in ../../2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before any later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundrySurfaceFinishProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Sixteen paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
