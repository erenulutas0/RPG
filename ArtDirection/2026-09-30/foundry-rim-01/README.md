# Foundry rim trim: the "vent slot" row — 2026-09-30

## The bug

Every rim of the court showed a dashed row of dark bars that read as vent slots (point-1, point-3, gate-detail;
listed as rank 4 in `../painted-surfaces-01/PROMPTS.md`). Two causes:

- **Mapping.** Flush decal strips (`Strip`, the layout coping trim) used local quad UVs that start at the corner of
  `ENV_Platform_Coping.png`. A 0.8-long strip sampled only its bottom-left corner: a brass bar, a black frame and a
  panel edge. The same corner was stretched onto every brass inlay.
- **Layout.** The court borders were one 0.8 strip per edge cell, set 0.32 in, with 0.2 gaps between cells: dark
  dashes parallel to every rim.

## What changed

- `FoundryRegionView.cs`: the coping texture's clean regions are named (six stone panel interiors, inset past the
  bevels, and the top brass rail clear of its rivets). `Strip` takes a `CopingTrim` (Stone or Brass) and a seed;
  `Builder.MapLast` maps a quad onto one region. Stone runs along a panel's tall axis, with the panel chosen per cell.
- Brass inlays, the compass inlay lines, the seal's forge mark and the gate wear strips map onto the brass rail; the
  compass channels, bridge joints, court borders and layout coping trim map onto stone panels.
- `FoundryRegionSite.cs` `BuildCourtBorders`: the dashes became a continuous kerb course along every open edge, one
  kerb stone per cell (centre 0.2 in, 0.3 wide, tone 0.92).

No geometry beyond the continuous kerb; same batches and renderers (17 / 54,168 vertices, unchanged). No gameplay,
collision, balance, save or telemetry change; no new art.

## Evidence

Actual Unity captures in `after/`, matched against `../foundry-xray-01/after/` (same code except this package).

## Limits

- The kerb course is subtle: it is the existing coping stone, darker than the paving. The painted trim sheet (S4 in
  `PROMPTS.md`) is still the real fix for the rim; this only removes the artefact.
- The parapet is still low and thin; that needs the trim sheet's band A and a taller rise.
- The court seal's own quads still use the old local UVs (small, low-alpha; not part of the slot row).
