# Court paving topology — source review

The fresh `before/court-detail.png` shows ordinary square joints crossing the ceremonial ring and the thin processional courses. The active V5 paving bitmap contains mineral grain, not these square joints. The old builders emitted ordinary beds, bevels and tops before appending another full bed and dressed stones at the same height. This package replaces that geometry ownership instead of shifting depth or adding another tint.

## Changed ownership and stone layout

- `FoundryRegionCourtPaving.cs` subtracts three disjoint convex domains from every existing floor triangle: the unchanged 48-sided circle at (-3,-5), radius 5.05, and two complete 4.18-wide processional rectangles on the existing entrance/gate axes. The rectangles begin 5.21 units from the centre, outside the circle, and retain their previous endpoints.
- Source triangles crossing a domain are split. Source UVs and already sampled colours are interpolated; outside triangles retain their original data. Unreferenced removed vertices are discarded. Most triangles use an allocation-free AABB/separating-plane path; only intersecting triangles allocate clipping polygons.
- `FoundryRegionFinish.cs` replaces the inner grid with an octagonal keystone and eight fitted fan stones. Twelve broad annular stones retain all 48 original perimeter segments. Each annular stone uses four convex pieces with no joints or bevels on internal subdivision edges, avoiding a concave fan across the hole.
- `FoundryRegionCourtSurface.cs` fills both entire processional fields with broad crosswise flags (courses no longer than 3.2 units) and alternate fitted pairs. Mortar, shoulders and face partition each stone. No full mortar bed remains underneath these faces.
- Root owns the single `FoundryRegionView.Build` hook immediately before the two ceremonial floor builders.

The existing brass, medallion centre/radii, floor material, continuous 5.7 world grain, half-unit light sampling, alpha 1, flush projection, gameplay shape, camera and renderer ownership are unchanged. Each fitted face shares exact common design points between neighbouring clipped half-unit cells, within one fixed tone/mineral family; position, UV and sampled colour are therefore identical. Separate mortar, shoulder and face calls keep their distinct tones. Only referenced points are emitted. The whole path remains ordinary walkable floor; this is not a new gameplay boundary.

## Validation prepared

`FoundryCourtCompassTests` adds three cases: exact total area and one owner across fitted medallion samples; subtraction of two crossing source layers with affine UV/colour preservation; and the actual production court plus bridge build with old geometry absent, one replacement owner, no retained unused clipped vertices and a bounded vertex increase. Existing path coverage now checks the centre as well as both shoulders; walking-union triangle edge sampling remains. Previous 3,200/6,500/5,000 inner-field/ring/path vertex guards are retained. `FoundryCourtDressedStoneTests` retains its two existing cases but calls the authoritative fitted flag partition after removal of the retired `CourtDressedStone` helper.

`dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore -v quiet` passes with zero warnings and errors. This only confirms compilation. Unity test execution, actual After captures, final region cost and device installation are root-owned and pending; the source changes alone do not prove visual acceptance or reference parity.
