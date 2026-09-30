# Foundry perimeter study — 2026-09-23

Isolated Unity graphics fixture, derived from FoundryLayoutCapture. Two 3x3 visual furnace annexes outside passage x=12, and six low stone buttresses outside square/salvage edges. Existing Furnace sprite and platform materials borrowed unchanged. Walking-cell proposal remains 952 cells; annexes are decorative, not approved navigable terrain. No production Assets, scene, gameplay or collision changes.

Initial capture exposed below-floor annex walls painting over the neighbouring floor. Corrected by separating below-floor masonry at order -16 from raised masonry -14 and caps -13 (flat annex caps -15). This is fixed static placement, not a complete actor/prop depth system. Annexes reduced from 4x4 to 3x3 for less oversized supports. Furnace scale stays .72.

Five actual Unity portrait captures at 9-unit camera width plus overview. Hero is repositioned, not navigating. No HUD, combat, device performance or traversability claims. Temporary meshes are disposed; shared materials/sprite remain borrowed. Reproduce with Tools/ArtReview/Run-FoundryPerimeterProof.ps1.

Gate V3 background cleanup again failed visually; preserved in boss-gate-01, not imported or used here. Next: improve perimeter material joins/visual identity, clean gate source and registration. Opus remains on hold. Phone unchanged.

Validation: final Unity graphics1/1, static integrity489 unique GUIDs/1099 package GUIDs/568 scene objects; browser five selectors, images loaded, no390px overflow or script errors. Final passage capture inspected. Floor-to-annex coping seam remains visible and needs an authored join; do not call this a finished environment kit. No full-suite/APK/device run.
