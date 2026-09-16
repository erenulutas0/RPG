# Stone, coping and contact pass — 2026-09-16

This is a bounded runtime material pass on the existing platform, after `../furnace-layer-01/`. It is not a new illustrated environment kit or a final character-art milestone.

## Changes and provenance

All new artwork is original deterministic C# drawing in `PlatformArt` and `ContactShadowArt`. No generated raster was edited or imported for this slice. The cavern remains the unchanged source documented in the preceding folder.

- The same 16-by-16 slab layout gains restrained upper bevels, lower lips, broad mineral patches and sparse chips. Internal detail stays cool and low contrast; no bright lava cracks are added to walkable floor.
- Flush coping stones sit inside the original diamond. Narrow brass outer rails, wider clamps and dark joints give the rim a construction rhythm. These are painted material details, not raised barriers or new collision.
- One 32-by-16 translucent ellipse at 32 PPU is shared by the hero and all six enemy looks. Each renderer stays at the actor root, below bodies and above platform lights, independently of body nudges/flash. It hides on death. Near a rim its whole rectangle contracts inside the walkable diamond, avoiding a patch on the face or void. This is a contact cue, not physically cast lighting.
- Surface/overlay dimensions, platform geometry, camera, actors, animations, encounters and HUD are unchanged. Corner lanterns, sidewall masonry and chest art still use their earlier prototype drawing. Chests do not receive the new actor shadow.

The shared shadow adds one sprite/texture, **2,048 RGBA32 texel bytes**, excluding native overhead. `ArenaSpriteCache` owns it until session reset/application quit; actor views never destroy it. Moving an actor updates its shadow transform without regenerating pixels.

## Review images

`room-0-*`, `room-1-*`, `room-2-*` are actual Unity camera renders of the centre, far tip and right tip at 1080x2340 and 1080x1920. Matching filenames in `../furnace-layer-01/` provide the preceding material baseline. `far-unbounded-2340.png` is a diagnostic with the arena camera constraint temporarily disabled, not the delivered camera behaviour.

Captures use a temporary PlayMode fixture, simulated top/bottom safe insets, an isolated test profile and ten frozen visual enemies (two Grunts, eight Mites) at real `EntrySides` positions. Staged enemies have the runtime contact-shadow component but no combat/health/bar controllers. Some lie outside the viewport or overlap at these entry positions. Hero, platform and HUD are from Gameplay. These are layout renders, not evidence of live ten-enemy combat or physical short-screen validation.

Unity RGB output was written as BMP and converted losslessly to PNG with Windows System.Drawing. Temporary capture code was removed from Assets before the full test/build gate and retained only under ignored `TestResults/StoneCaptureTemporary.cs`.

The pass improves ground contact and slab/rim structure at the current pixel scale. It does not close the fidelity gap to the painted reference; a coherent hero/Mite production proof and later prop/sidewall polish remain. Device evidence and final suite/build identifiers are recorded in docs/21–23.
