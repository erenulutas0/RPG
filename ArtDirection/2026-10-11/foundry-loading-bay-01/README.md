# Foundry western loading bay

2026-10-11. Baseline 3044b83cdcb213bd72a7281a8b50de007c3a8660.

## Result

Western service shelves and crane footing replace solid faces with recessed coursed infill, connected tapered corbels and continuous bearing courses; shelf decks gain bounded iron channel edges. Casting cantilevers are tapered within their existing attachment volumes. Three native Blender renders replace the existing crane, receiving stock station and casting station images: rough cast crane channels/yokes/bearing cases, a loaded receiving cradle with six staggered hollow pipes, and a real annular mould cavity/runner with a supported pouring vessel. Existing card framing, anchors, GUIDs, import settings and depth owners remain. Camera, map/navigation, scene, encounters, gameplay and saves are unchanged.

Three helpers authored support geometry/tests, crane source, and stock/casting sources. Root integrated all three resources, reviewed the source diffs and both source renders and actual Unity images, and alone ran Unity. Helpers reached the account usage limit during the requested cross-review, so independent cross-review did not finish and is not claimed. Eight matched baseline 3044b83 views were copied from tracked files and Git-object/SHA256 verified; three close equipment views and two local portraits are explicitly after-only. The source-before directory retains all three verified baseline resource PNGs. Initial graphics import found a new test referencing internal FoundrySurface across the Unity assembly boundary; the test now invokes the existing private FoundryRegionView.Project through reflection. The initial diagnostic is retained. This was an import failure, not a completed graphics capture. Five new EditMode cases validate actual emitted geometry against projected original support prisms, unchanged deck corners, scenic footprint interiors, below-floor separation, finite UVs/indices and bounded vertex cost.

Root actual-image review accepts better connected loading architecture and clearer receiving/casting identities. Live crane chain/sheave/load contact remains seated in the inspected images. The existing foreground lamp pole crosses the mast and the large northwest shaft still dominates some close views. Local gains are visible; whole-map gain is modest and full reference material/structural richness remains unfinished. Region remains 32 renderers; 162568->165392 vertices (+2824, 1.7%). Distant2496 unchanged. Existing crane import1024 ASTC6x6/11mips and stock/casting512 ASTC6x6/10mips remain non-readable; no extra runtime texture, material or updater. Native Blender mesh counts are authoring counts, not runtime GPU geometry. These resource counts are not phone-performance evidence.

No ADB, installation or fresh phone observation this turn. The earlier concrete install/restart decision remains pending. Before eventual deployment inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Device circulation/crowd/HUD/motion/shimmer/midrange acceptance and regional encounter/boss progression remain open.

## Verification

425 .NET / 488 EditMode / 159 PlayMode = 1,072 distinct suite cases, zero failures/skips. Isolated compile zero warnings/errors. One successful Android-target graphics capture (1/1). PASS: 594 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,242,956 bytes; SHA256 1130DFD56C5AAFEBBE3CA034661F370AF86C03D9E9E4C10CDA5B0FBE9283E728. NOT INSTALLED.

XML/TRX, provenance, the initial import diagnostic and root acceptance are retained in validation. Graphics cases are separate from the suite total. The browser gallery compares eight matched cameras and labels five additional views after-only. Captures pause encounters, hide the HUD and use staged production actors. They are actual Unity views, not phone screenshots or crowded-combat measurements. The approved reference is concept art.

## Reproduction

With Unity closed, from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryLoadingBayProof.ps1
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The capture launcher temporarily copies FoundryLoadingBayCapture.cs into PlayMode and removes the temporary script/meta in finally. Historical before images are copied, never recreated from changed source. Source generators are Tools/ArtReview/BuildBlenderCrane.py, BuildBlenderStockStation.py and BuildBlenderCastingBench.py; native blends, transparent renders and projection checks are in ../loading-bay-assets-01/. See their READMEs for Blender reproduction. Original source/evidence directories remain unchanged.

## Next work

Next coherent art package: reduce the visual dominance of the northwest service shaft through joined shaft/head/side-return architecture and local material hierarchy, reviewed against the entire reference and the casting/crane portraits. Preserve the existing scenic envelope, circulation and camera; do not add another row of small trim or grow every wall mesh. Keep full-map scale and foreground/background separation as acceptance criteria. Physical-device acceptance remains separate.
