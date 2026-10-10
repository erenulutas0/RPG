# Casting workbay — actual Unity integration

Baseline: `b1b690c`. An original native Blender casting station adds a compact production scene behind the north pit loop: hand screw press, open moulds, removed mould half, cooled billets and hand tools. This is a fixed-view rendered prop integrated into the existing Unity scene, not a new 3D gameplay system.

The new masonry shelf occupies design `u[-18,-13], v[19.1,22.4]`, top height `.18`. Two below-deck supports connect it to the court wall. Equipment occupies measured `u[-17.56,-13.42], v[19.425,22.07], height[.18,2.47]`. The shelf clears the existing finial and remains outside the walking union. The north rail is retained; there is no implied new walkable route or invisible obstacle. Existing map bounds, camera, collision, combat, energy deferral, progression and saves are unchanged.

## Implementation

`FoundryRegionCastingBench.cs` adds one card material, one explicit non-landmark depth footprint and a bounded procedural fallback. The imported texture is borrowed from Resources; the region owns its source material/mesh and the existing visibility layer owns its cutaway variant. Card rank -10 remains behind the foreground rail/finial rank -8 and ahead of its shelf. Static supports reuse existing wall batches. The source render is 1024 RGBA, imported at 512 with Android ASTC6x6, mipmaps, clamp and no CPU readback.

Editable source and exact render projection: `../casting-bench-01/`. Reproduce with `Tools/ArtReview/BuildBlenderCastingBench.py` and Blender 4.5.9 LTS. Original modeled geometry and procedural materials only; no downloaded model or generated bitmap editing. Runtime uses the baked card, not the 10,040-vertex authoring mesh.

## Evidence

Before images were captured from the clean baseline, after images from the integrated source. Matching overview, stock, full-map and portrait cameras support comparison. `after/casting-detail.png` and `after/casting-portrait.png` are additional views without matching baselines. Portraits use production follow width with staged actors and hidden HUD; these are actual Unity renders, not phone captures.

Root and independent visual review accepted shelf contact, front rail/finial overlap and workshop coherence. The region measures 31 to 32 renderers and 115,042 to 115,106 vertices (+64). The source mesh is not loaded at runtime. Main improvement is local workshop identity; the whole-map effect is modest. Three bright similar moulds remain cleaner than nearby iron and can receive later restrained wear/variation. This does not block the accepted integration.

PASS: 409 pure .NET, 446 EditMode and 154 PlayMode cases (1,009 total; no failures or skips). Two focused casting cases also passed and are included in the full suite, not counted twice. Compile check: 0 warnings/errors. One baseline and two after graphics passes; the second adds the complete-workbay portrait. Before/final-after XML retained. Final static integrity and APK identity are recorded in `validation/verification.md`.

## Device and limits

Not installed. No ADB action or fresh device observation in this pass. The earlier concrete owner-run restart/install decision remains pending. Last recorded device package/run state is historical. On approval, perform fresh preflight/backup, install the latest accumulated package, verify its hash, check the map and normal combat, preserve current original saves/telemetry and remove only newly introduced diagnostic flags. Never restore stale saves.

The workbay increases local workshop specificity; it does not complete reference fidelity or establish phone performance. Architectural richness, coarse distant silhouettes, crowded combat readability, first-ramp hitch and physical-device/midrange acceptance remain open. Region encounter/boss progression remains deferred under the owner’s art-first direction.

## Reproduction

With Unity closed, run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryWorkbayProof.ps1 -Phase after`. Phase selects the output directory and does not reconstruct historical source. The temporary capture script is installed for the run and removed in `finally`. Root alone owns Unity/ADB; helper assets must be released before the run.
