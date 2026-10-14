# Independent source review — grouped gate depth

Reviewed the frozen candidate on 2026-10-14, read-only. Scope: the new gate masonry integration and PlayMode test; enclosure fallback and teardown; authored Blender generator/projection metadata; importer; profiled header shoulders and six focused cases. No Unity, build, device or source mutation was performed by this reviewer.

## Verdict

Initial isolated source inspection found no construction/lifetime blocker, but actual integration exposed a painter-order defect described below. The final scoped pipe-order repair, calibrated scene colour and refreshed merged-scene images have now been independently reviewed: **no remaining source blocker found**, subject to the parent's complete regression/build gate. This package introduces a fixed-view baked replacement with new resources; it must not be described as only tint changes, resource-free, or real-time volumetric masonry.

## Projection and physical placement

The Blender camera direction `(-12,-12,sqrt(96))` gives the required 30-degree elevation: screen X is `(u-v)/sqrt(2)`, and screen Y is `(u+v)/(2*sqrt(2)) + z` after the authored vertical compensation `1/cos(30)`. The 10-unit vertical orthographic field, 1:2 aspect, and 3.75-unit target correspond to the Unity card offsets X `[-2.5,2.5]`, Y `[-1.25,8.75]`.

The bake omits the raised landing. `GateMasonryCard` adds `Project` at the original footprint centre, applying the landing lift once. The identical unmirrored orientation is appropriate for the near and far translated footprints. Foundations still emit separately; only the original above-deck outer-buttress body is replaced when the resource exists.

Independent read-only pixel checks on the source RGBA image found:

- Master 1024 x 2048; all four corner alpha values zero.
- Alpha >127 bounds `(82,184)` through exclusive `(942,2007)`.
- Model-projected continuous bounds approximately `(81.898,184.236)` through `(942.102,2007.051)`, matching within raster antialiasing.
- Every one of the eight original footing polygon stations has opaque image pixels within a two-pixel neighbourhood of its independently calculated projection.

This confirms field placement and outer-foot occupancy. The final generator additionally retains 142 expected tier-ring, frame, throat and backing stations and searches the evaluated model vertex set for each, asserting maximum distance below 1e-6. Final metadata reports maximum error zero. I reopened this source/metadata after the generator helper confirmed its freeze; the final PNG SHA256 is `B3C88B15F4CD709E259DD732DE4DFFC943419B78F74EF41022E3316945B074A8`. These checks support contact preservation, while joined foundation seams, reveal exposure and near/far occlusion still need integrated images.

## Runtime ownership and lifetime

The imported texture is borrowed through `Resources.Load`. The scene owns one source material, one cached shared cutaway variant and two generated quad meshes. The existing `MassDepth(towerFoot,7.35)` scope tags each replacement; `Create` carries that owner into the emitted card. Texture orientation is not mirrored. The initial candidate kept original authored ranks -14/-11, mapping to actual -14/-8, but that was insufficient after separating material batches; see the integration finding below.

The fallback emits the old native body only when the resource is absent, preventing the normal path from accumulating both bodies. The source material is destroyed in the region teardown; the existing visibility owner destroys its variant, and generated meshes remain in the normal owned-mesh list. The imported texture is not destroyed. The added material entails one additional focus-vector update in the existing visibility loop, plus its rendered colour/depth work. No new per-frame component is introduced.

The new PlayMode case checks the real scene's two cards, ranks, UVs, dimensions, mip count, nonreadability, shared variant, depth owner, no attached updater components, and unload disposal/borrowed texture survival. It is useful integration coverage. Quad coordinates alone cannot prove the visible content of the PNG; the alpha/projection check and real capture serve that separate purpose. Existing native outer-buttress tests now exercise the fallback and are not production baked-surface coverage.

## Header shoulder construction

Only the two flanking shoulder calls change. The new roof lowers eaves by 0.12 inside the old envelope, keeps the ridge maximum and bottom contacts, and joins the end gable/side surfaces to the pitched roof. Three broad rows and their separately emitted mortar partition the roof; the old flat cap is not retained underneath. Shared mineral mapping is applied once after the whole shoulder. The lower cornice, centre backing/lock, corbels and iron contacts retain their existing paths.

The six focused cases inspect actual emitted stations/envelope, production inclusion, lock exclusion, roof area and interior ownership, mortar ownership, noncollapsed UVs and restrained surface values. They provide more than simple vertex-count checks. No assertion relaxation was found in this added scope. The parent reports the initial local-name compilation error was corrected; current focused Foundry Edit 264/264 and isolated compile succeeded. This reviewer did not execute those checks.

## Import and resource tradeoff

The importer uses sRGB RGBA, Clamp, Trilinear, mipmaps, nonreadable data and Android ASTC 6x6, reducing the 1024 x 2048 master to a 512 x 1024 runtime texture. Independently summing 16-byte ASTC blocks over all eleven mip levels gives **314,992 bytes** of compressed texel payload, excluding Unity/material/driver overhead. The two cards add transparent fragment area despite replacing substantial native vertex work. Record actual renderers, vertices and materials from the final capture rather than assuming a frame-time improvement from lower vertices.

The importer uses a temporary Editor script with guarded ownership and literal-path cleanup. Reusing the borrowed texture and existing shared cutaway pipeline is consistent with the project's other baked scenery. Directional neutral light is intentionally baked and cannot dynamically match every local flame; actual scene colour agreement remains an art acceptance question.

## Integration findings and required correction

The first native capture showed the old curtain wall painting over almost all of each new niche. `FoundryCutaway` keeps colour painter order with `ZTest Always`; preserving `MassDepth` cannot solve structure-to-structure colour ordering. Moving the bay from the shared wall mesh to a different material removed the old within-batch triangle ordering. Equal sorting order is not a guarantee of the previous assembly order. The parent retained this rejected evidence under `validation/first-integration`.

The second candidate moved cards by +0.5 authored order, to actual far -13 / near -7, and restored the niche pixels. The image nevertheless remains too pale relative to the central portal/native masonry; the parent is calibrating RGB only. This second capture is not accepted merely because the new geometry is visible.

The near -7 layer is not exclusive: existing gate inlays and boiler feeds are authored -10.5 and also map to -7. Inlays appear disjoint in the reviewed current views, so moving only those would not fix the concrete overlap. The south gate feed actually crosses opaque near-bay pixels: its centre at design `(28,-5.388135)`, height 1, maps to master pixel `(373,1659)`, alpha 255; samples across u=27 to 29.3 are also opaque. The reviewer recommended moving only the two south gate-feed segments, their collars and elbow to authored -10 / actual -6, using an optional order argument so unrelated pipework remains unchanged. At this stage the possible additional material/order batch still required measurement. No global rank-map change was recommended; final correction results follow.

### Final correction review

Root implemented the optional `MassPipe` order, defaulting to its prior -10.5. Only the south pair and elbow pass -10; the same argument reaches body and collar geometry. Other feed geometry, depth owners, dimensions and the global rank mapping remain unchanged. The final PlayMode case searches actual higher-ranked iron triangles covering the independently selected opaque witness, rather than merely asserting a new order constant. Gate inlays remain untouched.

The possible extra batch in the recommendation above did **not** materialize: capture confirms reuse of an existing FoundryIron/-10 batch. Final totals are **36 renderers / 164672 region vertices**, versus 34 / 167208 at baseline: +2 renderers and -2536 vertices. Distant geometry remains 3572, included in the region total. The master remains unchanged; scene RGB gain is .62 with alpha 1 on source and variant, also covered by the integration test. Fresh source and images were reopened after these changes. The corrected image verdict is in `independent-image-review.md`; no full-suite result is asserted here.

## Evidence boundary

Fresh merged-scene images, full suites and conditional APK build are separate parent-owned gates. No phone interaction or current physical-device acceptance is established here; preserve the standing device-state/save/telemetry boundary.

## Post-capture resource-budget review — 2026-10-14 17:21 local

The first full gate was not green: EditMode passed 666/666, but PlayMode passed 159/160. The existing region integration test counted the newly imported gate cards among its uncategorized batches and failed with 25 against the old ceiling of 24. Its subsequent total-renderer ceiling of 34 also needed an explicit change for the measured 36-renderer candidate. The new gate-specific PlayMode case passed. The failed XML and source freeze remain under `rejected-budget`.

I independently read the revised resource assertions. The test now identifies the borrowed gate texture, requires exactly two matching batches with four vertices each, retains the old uncategorized/native ceiling of 24, and explicitly changes the total ceiling from 34 to 36. This is an acceptable, bounded allowance for the two documented card renderers; it is a genuine resource-budget increase, not a performance saving. It does not remove the existing native-batch guard. The dedicated gate integration case supplies the more detailed ranks, material, depth, lifetime and overlap assertions.

Comparing the rejected and current source-freeze asset maps found only `Assets/_Project/Tests/PlayMode/FoundryRegionTests.cs` changed. Runtime source and imported assets are unchanged. The post-repair capture completed at 17:12:44; the independently reopened gate and full-map images retain the accepted result. Parent reports EditMode 666/666 has passed again; the final PlayMode/build gate was still pending at this note. No new source blocker was found.
