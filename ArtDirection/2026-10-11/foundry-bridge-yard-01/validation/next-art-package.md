# Next reference-led art package

Read-only recommendation after bridge/workyard image review, 2026-10-11. Not implemented or accepted as completed work.

1. Unify the western loading bay. Crane, stock and casting stations currently read as separate clean blocks behind a continuous rail. Give existing shelf facades supported loading ledges, broad recessed faces, bearing corbels and attached iron edge beams. Inspect FoundryRegionWorkshop.cs, FoundryRegionCastingBench.cs and crane footing in FoundryRegionDressing.cs. Preserve shelf footprints, rail/navigation boundary and equipment deck heights.
2. Give the crane convincing cast-metal mass. Rework Tools/ArtReview/BuildBlenderCrane.py and ArtDirection/2026-10-10/crane-01 source around fuller bearing/root housings, coherent cast sections and broad roughness variation. Retain FoundryRegionBakedCrane.cs mast/jib-tip/sheave anchors, 1024 bake frame and depth owner; do not add miniature truss noise.
3. Distinguish receiving and casting. Stage a visibly loaded receiving rack and active casting setup with broad cavity/pattern contrasts and asymmetric stock. Inspect BuildBlenderStockStation.py, BuildBlenderCastingBench.py and workyard-clusters-01 authoring evidence. Retain stock [2,18.5,7,22.4]/1.6 and casting [-18,19.1,-13,22.4]/2.5 authoring envelopes, translated via established runtime anchors.

Tall northwest shafts behind these stations belong to FoundryRegionDistantWorks.cs. Their visual dominance is a separate composition issue, not actual shelf support geometry.

Evaluate whole-map reference similarity and actor-height portraits, not only isolated props. Current package resource growth and pending device acceptance require attention before continuing to add geometry.
