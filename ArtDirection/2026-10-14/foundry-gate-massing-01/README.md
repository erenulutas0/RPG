# Foundry gate massing

Verified 2026-10-14. Baseline `0e44e8fe3c04a53ec958556131ffa323fb4dede7`.

The complete shared outer gate-bay bake now has a taller raked lower mass, shorter blind upper opening, wider stone cheeks and a deeper recess. The native Blender source retains the original external footprint, crown contacts, projection, lighting and 0.62 material gain; its internal shoulder rises from 2.85 to 3.65, so the opaque contour is intentionally different. The upper curtain side also replaces its uniform grid with a broad recessed field, raked haunches and continuous bearing. Actual images show the outer bays as the main visual gain; the curtain is a subordinate construction and geometry simplification because inner piers hide most of it in the gate close views. Its near side remains visible in the full-map view. Existing door, header, roof, walking layout, camera, actor depth and gameplay remain unchanged. This is an incremental gate improvement; reference fidelity is unfinished.

Region 36 renderers / 171888 vertices versus 172340: -452. Distant 3572 is included and unchanged. Ceilings remain 174000 / 3600, leaving 2112 / 28. The gate-bay bake replaces its existing resource at the same loaded 512x1024 ASTC6 / 11-mip / non-readable budget. The two existing cards retain their material, projection and depth owners. No additional runtime texture layer, shader, renderer batch or updater. Static counts do not establish device performance.

## Verification

438 .NET / 693 EditMode / 160 PlayMode = 1,291 accepted suite executions, zero failures/skips. Focused Foundry 291/291 passed separately. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 636 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,382,273 bytes; SHA256 5C13765B77191B699EC464261CA376FC4646020704A021E6278C5E5E7AB966F2; NOT INSTALLED.

Three helpers worked in disjoint scopes: a versioned native Blender outer-bay replacement, upper-curtain construction and matching geometry tests, and independent source/image review. Root sealed 21 fresh Before views at 0e44e8f before any runtime edit, imported the direct Blender PNG while preserving its importer, checked identical external projection/footprint fields, and coordinated compile, captures, full suites, gallery and Android build. The complete outer-bay source is editable native geometry and procedural stone; no bitmap retouching was used. Two additional EditMode cases verify the broad exposed side recess, exact bearing/face contacts, no front overpaint, original envelope/front and nonpositive finished geometry delta. The first focused run passed 291/291, including both new cases. Matched After capture passed on its first run. Source review found no actionable geometry issue. Independent image review restricted acceptance to a major outer-bay form change and subordinate, partly occluded curtain simplification; no runtime revision was required. The final Asset freeze is checked again after the complete suites and build. Gallery initial asynchronous image loads were rechecked after completion; all 43 controls passed. No phone operation was made.

## Evidence boundary

Twenty-one matched actual Unity views use identical camera/actor staging, paused combat and hidden HUD. Reference is concept art.

This is a bounded gate massing improvement, not reference parity or a new layout. Approach portrait crops most upper architecture; full-map gain is localized to the gate. Most curtain work is occluded by existing inner piers, so its visual contribution is small. Native inner-pier form and material disparity remain unfinished. Map bounds, camera, navigation, equipment footprints, gameplay and encounters remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved (ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md). No phone control, installation or save/telemetry change. Device motion, HUD, shimmer and midrange performance remain unverified. The earlier concrete restart/install decision remains pending; later deployment must inspect current state and preserve current original saves/telemetry, never restore historical snapshots.

## Reproduction

Run with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryGateMassingProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before pins exact baseline HEAD and unchanged Assets. The editable native Blender source, procedural materials, projection and contact measurements are in masonry-bay/ and Tools/ArtReview/BuildBlenderGateButtressMassing.py.
