# Foundry mass and perimeter rhythm

Baseline `c532902`. Before captures are reused unchanged from `foundry-portal-parapet-01/after`; all new after views use the same cameras and staged actors. Target: `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

## Implemented together

FoundryRegionDistantWorks.cs replaces the three remaining generic shaft crowns with sloping shoulders, narrower necks, splayed bearing ribs and single, twin or offset exhausts. Their world anchors, lower fade, atmospheric material, quiet fire openings and sorting ranks remain. The two previously refined arched north furnace towers are unchanged. Existing masses are replaced rather than adding another scenery layer.

FoundryRegionMassing.cs gives wide exterior rail spans (at least 1.95) a recessed central masonry panel with two iron openings over a 0.28 sill. Short spans have an open iron bay over a 0.18 sill. Physical reveals, shoulders and a lower ledge support the panel. Existing posts and span bounds determine the grouping; no random noise is introduced. Rail top remains 0.72; original footprint, coping overhang, depth ownership, posts and light anchors remain. Pit and bridge keep their separate open balustrades.

No new texture, material, renderer or per-frame component. Camera, walkable map/ramp, combat, scene and saves unchanged. Three helpers split distant works, parapets and independent review; root alone ran Unity.

## Actual visual review

Root and independent reviewer accepted matched full-map, overview, west-pit/workyard and gate approach portraits. Panel shoulders meet the coping and sill, boiler returns remain continuous and the hero/range ring stay legible. Nearer distant crowns have different silhouettes without new visible floating bases or island sorting artifacts. This is sampled Unity visual acceptance, not physical-device or crowd acceptance.

Whole-map improvement is modest: the dominant painted side buildings remain much larger and richer than the changed background shafts. Regular post positions remain. The reference's workyard composition and individuality of its boilers are stronger priorities than another small parapet pass. The existing rail across the gate is unchanged and remains a future gate-transition design issue.

Measured region: 32 renderers unchanged; 117,629 -> 118,537 vertices (+908/about 0.77%). Distant masonry 2,244 plus fire144 = 2,388 vertices, below the unchanged 2,500 limit. Resource counts are not performance measurements.

## Verification and deployment

See `validation/verification.md` for final suites, APK and deployment status. One new after graphics capture; before XML is historical evidence, not another execution. The browser compares actual Unity captures and the approved concept. Portraits have production camera width and hidden HUD; they are not phone screenshots.

No ADB operation or fresh device observation in this slice. Earlier concrete owner-run restart/install decision remains pending. Deployment requires fresh state inspection/current-files backup and latest APK hash verification; preserve current original saves/telemetry, never restore stale snapshots. Device/crowd/midrange/first-ramp acceptance remains open. Regional encounter/boss progression stays deferred by art-first direction.

## Reproduce

With Unity closed: `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryMassRhythmProof.ps1 -Phase after`. Phase selects output directory only; it does not revert source. Temporary Assets capture source/meta are removed in finally.
