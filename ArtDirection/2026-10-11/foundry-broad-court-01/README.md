# Foundry broad court — actual Unity proof

Baseline: `b5bcbed4e59ba99937978a18cb2b96f434b12294`. Eleven before views and five diagnostics were copied unchanged from `foundry-workyard-islands-01/after`; source/destination SHA-256 checks are retained in `validation/provenance-sha256.txt`. These are baseline observations, not new execution.

## Combined scope

The owner explicitly requested a physically wider reference-led map and grouped visual work. FoundryMap.West changes from -30 to -36. Main-court u span grows 44 to 50 units (+13.6%); west-rim-to-pit strip grows 14 to 20 (+42.9%). The unchanged v span is32. This adds192 square design units before equipment footprints; the existing corner-cut areas and pit are retained. Pit, bridge, northeast gate/causeway, production camera scale and scene remain unchanged. This broadens the industrial wing, not every dimension of the reference.

WestAssemblyOffset automatically moves the western crane, salvage, stock, exterior walls, lamps and shared depth ownership six units west. New WorkyardShift moves interior production equipment half that distance, retaining circulation on both sides. Turntable centre becomes(-27,7); trolley bounds[-29,-4,-25,-1.8]; mould bounds[-24.8,11.8,-21.6,14]. All rail panels, pivot and north/south tracks follow the centre. The western loading endpoint follows West+.35=-35.65, extending the connection to the relocated salvage edge rather than leaving a gap. Casting shelf remains in its existing northern position.

A second visual slice completes connected stepped billet bands/buckles, through axles/bearings and wheel ribs. A third refines the mould bench with a warm three-plank surface, supported screw clamps, asymmetric flask dog and attached handles. Native meshes use the same material and existing iron batch. Everything remains inside shared footprints, with maximum height1.54 below the1.55 depth owner. No new texture, material instance, renderer or updater.

## Movement and floor verification

The shared map bounds continue to drive physical paving, sidewalls, hero movement, spawning and pursuit. SurfaceContains stays separate from obstacle-aware Contains/Segment: paving under equipment is continuous. Derived pursuit nodes move with the two footprints. Existing western chamfer tests now target the new actual boundary, and the old cut locations are checked as newly paved floor. Packing tests derive their half-cell grid from production bounds (136x64), preserving exact coverage. Baked crane/stock depth assertions follow WestAssemblyOffset.

Four additional pure cases verify real width, new western floor, four waves of ten separated entries, and routes both directions between new points and court/gate/bridge anchors. Existing scene checks verify actual paving/blocked-footprint contacts. Enemy counts, attacks and encounter data do not change; extra space can change perceived density and escape opportunities, so this is not a balance acceptance.

## Capture contract

Eleven fixed baseline cameras remain matched: overview, full-map, yard-detail, workshop-detail, crane-detail, expanded--27-6, expanded--26-15, casting-portrait, islands-yard-wide, islands-trolley-portrait, islands-mould-portrait. Camera position/size are not changed to manufacture the apparent width.

Six after-only captures inspect the new area: broad-yard (centre -27,6; size12), broad--34-0, broad--34-6, broad--32-12, broad--27--6 and broad--23-10. The latter five use the production portrait framing with placed hero and hidden HUD. These are actual Unity captures, not phone screenshots. Images were not postprocessed.

The runner selects Android target and records actual imported texture format, distant material and mesh budget. One actual graphics execution passed. Phase selects output only; it does not restore source:

```powershell
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryBroadCourtProof.ps1 -Phase after
```

Root alone runs Unity. Three helpers split shared width/navigation/tests, equipment geometry and independent art review; root integrates rail placement, captures, full validation and documentation.

## Review and delivery

Root accepts the real broadened working wing and connected equipment from whole-map, yard and new-strip portraits. Resource counts:32 region renderers retained;120677 to128573 vertices (+7896/about6.54%) for the extra floor/perimeter and manufactured detail. Distant works remain2496 vertices. Counts are not a device performance measurement. Native equipment and architectural proportions/material richness remain below the approved reference. Independent art review and exact build/test evidence are recorded under validation.

No ADB action or fresh phone observation this turn. APK delivery is distinct from installation; the earlier concrete owner-run restart/install decision remains pending. Before any later device test inspect fresh, back up current original files, and preserve current saves/telemetry; never restore stale snapshots. Device movement/crowd/midrange/first-ramp/shimmer acceptance remains open. New encounters, bosses and region progression remain deferred under the art-first direction.

## Test-oracle correction

The first full gate stopped APK production on a false positive in the scene floor-coverage helper. A near-degenerate clipped triangle, with a bounding box far from the queried removed corner, returned float barycentrics(0,1,0). The diagnostic preserves the triangle and point. The helper now rejects points outside triangle bounds and computes orientations in double precision; the expected empty corner remains false and production geometry is unchanged. A synthetic sliver/real-floor regression protects the query. Failed, diagnostic, focused and final XML are retained; repeated runs are not counted as additional distinct tests.
