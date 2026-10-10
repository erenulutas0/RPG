# Workshop material coherence

## Status

Matched **before and final after** Unity captures are available. Independent visual review accepts the corrected workshop with the limits below. Full tests passed:409 .NET,432 EditMode,151 PlayMode (992total). Three earlier focused region tests passed separately. Android build passed; physical-device acceptance remains pending. See validation/verification.md for exact checks, APK hash and limits. No installed-build or performance claim is made here.

## Intent and reference

Replace the remaining visibly procedural crane and metal stock surfaces with a coherent authored treatment. The approved [foundry concept](../../2026-09-23/foundry-layout-01/foundry-v1.png) remains the visual target. The current workshop is a peripheral group of exterior service shelves; this package does not claim to reproduce the reference's complete working-yard composition.

The acceptance target is visible material and shape quality: rounded pipe mouths, convincing formed metal, readable machinery and coherent lighting. More rivets or a changed vertex count alone do not demonstrate an improvement. Keep the crane's lattice openings, grounded base, upper pivot and chain-to-basket connection legible.

## Spatial and painter contracts

- Stock shelf: design u[-24,-19], v[18.5,22.4], existing stock height1.6. Rear hoist retains explicit height3.6 ownership of that shelf without becoming a new landmark for unrelated meshes.
- Crane foundation: u[-33,-30], v[11,15], existing depth-landmark height8.6. Foot is WestProject(-25.5,13)+1.1; jib tip is WestProject(-26,20)+7.8; load remains3.4 below the tip.
- Implemented source-to-actual sorting order: shelf cap-13 ->-12; hoist-12.5 ->-11; metal stock-12 ->-10; crane-11.5 ->-9; live wood/chain hardware-11 ->-8. The stock and crane silhouettes remain separated from the hoist. Northwest chamfer lantern masonry alone now uses source-12/actual-10 behind the crane; its iron and flame remain unchanged. A single baked card cannot independently interleave its contents with neighbouring objects, so actual image review remains necessary alongside depth assertions.
- Preserve the current footprints, walking area, movement/camera, progression and saves. Any future raised workyard machinery requires its own collision/navigation contract.

## Evidence

`before/` contains actual source-state Unity captures, including the added crane detail. `overview.png` uses the established overview camera; `full-map.png` uses a separate wider camera. Portraits retain production follow width with staged actors and hidden HUD. These are not physical-phone screenshots.

The gallery compares the approved reference with actual before/after overview, stock, crane, workshop and walkable-portrait captures. Before images are snapshots of earlier source, not reconstructed by an output-phase switch.

Actual combined region: **26 ->29 renderers;118,902 ->116,178 vertices** (-2,724). Two authored cards replace procedural geometry, and one localized lantern-masonry batch fixes painter ordering. Fewer vertices do not establish a performance gain. The texture field in `runtime-budget.txt` describes the inherited, unchanged FoundryShortBoiler asset, not the new stock or crane textures. Actual after texture records confirm stock512ASTC6x6/10mips/non-readable, crane1024ASTC6x6/11mips/non-readable and retained hoist512ASTC6x6/10mips/non-readable. Final full cleanup/build verification remains coordinator-owned.

## Review limits

The final after stock-detail and portrait show clearly rounder pipe mouths, convincing grouped tubes and more coherent coil/basin/tool materials. The crane's lattice, lower housing and pivot are more readable, and its flange material is darker than the first after iteration. The large pivot still reads as a bright accent, so this is not a claim of perfectly matched reference shading. Barrel/basin and crate/tool silhouettes remain understandable in the reviewed images.

Independent review caught a real ordering regression in the first after capture: northwest chamfer lantern masonry covered the middle mast. The coordinator identified the exact post; a localized rear rank now restores the visible mast while retaining the lantern iron/flame. Final crane-detail and matched portrait confirm the corrected continuous lower housing, separated foreground post and intact stock/hoist relationship. This corrected after state is the one shown in the gallery.

The full-map gain is small because this group occupies a limited peripheral area. These replacements improve nearby material quality, not the reference's complete composition. Full reference fidelity, device acceptance, sustained crowd/midrange performance and the previously recorded first-ramp hitch remain open.
