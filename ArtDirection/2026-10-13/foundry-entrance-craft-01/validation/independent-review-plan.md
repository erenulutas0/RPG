# Independent entrance-craft review plan - 2026-10-13

Evidence reviewed: approved foundry-layout-01/foundry-v1.png; the preceding pit-craft package's full-map, court-detail, bridge-detail and forecourt-detail PNGs; the latest authoritative handoff sections in docs/38_HANDOFF_2026-09-30_ART.md; current CourtSurface, CourtPaving, View bridge/deck generation, PavingWear and AshlarFlagFace source. Read-only review; no Assets, Unity, ADB or git changes.

## Visible objective

The reference uses a connected entrance composition that leads through the court towards the large gate. The current entrance has two narrow brass lines on the bridge, then a separate rotated field of large stones before the medallion. In the forecourt view these read as independent pieces rather than a fitted transition. A single tapering stone field from the real bridge threshold to the existing route cap is a meaningful improvement. It should organize the existing entry rather than add another decorative strip beside it.

The existing V6 floor already has a calm material response and clear actors. Further stone wear should make selected surfaces feel worked and weathered without reinstating noisy aggregate, repeated cracks or independently randomized pale/dark tiles.

## Geometry and ownership acceptance

- The bridge transition must replace all older flush floor layers inside its exact footprint. Existing bridge generation emits a bed and multiple stone layers; subtracting only the top stone or adding a new overlay is insufficient.
- Keep the existing circle and two route replacement domains, the real bridge/court seam, walkable bounds and ramp projection contract. The new domain must meet the existing rotated route cap exactly and avoid overlapping it.
- New mortar, bevel and stone interiors must partition the replacement surface. No doubled parallel paving field, hidden full-face bed or decorative duplicate under a new route.
- Inspect narrow clipped flags and the tapered-end corners after insetting. Nonzero footprint alone does not prove a valid inner polygon: guard against crossed inset lines, reversed inner triangles, slivers and pinholes.
- Verify physical area and off-edge sample ownership through both joins, not a proxy such as the number of flags. Keep inherited route and whole-region budgets; the new bridge domain has its separately agreed fixed bound.
- Preserve world-continuous grain and FloorLight sampling. Interpolated clipped colours should not be lit a second time. The transition should not become an unexplained bright or dark rectangle under the same local light.

## Wear and material acceptance

Current AshlarFlagFace returns immediately after ContouredFlagFace for most new quarry flags. Old rectangular fracture/corner/incision helpers therefore do not prove that a proposed change is active on the dominant paving. Confirm actual emitted contour vertices carry the new selected wear.

Do not accept a helper-only wear peak that is never sampled in the mesh. Measure or inspect the emitted result at the intended edge, with most of the stone interior and most neighbouring stones remaining quiet. Maintain the existing light colour relationships; a dust or abrasion coefficient must not replace the forge-light field.

At portrait scale, the hero, small enemy, large enemy, chest, health bars and attack guide must remain more salient than the stone treatment. The medallion and established brass hierarchy should retain their meaning without another bold parallel outline. At full-map scale the improvement should read as a coherent entrance, not merely extra microscopic detail.

## Required matched visual checks

- bridge-detail: the field begins at the actual threshold and retains the bridge's width, edge clearance and statue/lantern contacts.
- forecourt-detail: the taper meets the existing rotated route without an overlapping cap, seam gap or abrupt material patch.
- court-detail and approach-0-0: medallion ownership, actor readability and the quiet material hierarchy remain intact.
- approach--8--12: the walking view sees continuous stone craft through the bridge/court transition.
- full-map: entrance composition becomes clearer without changing silhouette or creating a second competing route.

The art review does not establish navigation correctness, full-HUD interaction, motion aliasing, device compression or physical frame time. Those remain separate from static image acceptance.

Guidance was sent to both implementation agents before runtime edits. Gate_surface confirmed the agreed single tapered replacement with broad transverse courses and unchanged brass, walking geometry and inherited budgets.
