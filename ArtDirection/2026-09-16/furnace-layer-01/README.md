# First furnace layer and room-bound framing

## Implemented slice

One imported dim cavern plate replaces the active scene's star field and floating background props. The walkable floor, rim, underside, light animation, actors, HUD, weapon reach and collision geometry remain their existing runtime layers. The new plate contains no playable objects or UI. The source is in `Assets/_Project/Art/Environment`; provenance and exact prompt are in `PROMPT.md`.

The width-9 camera now favours the room interior near a rim. It first fits the projected room bounds within the usable HUD band, then reserves at least 2.6 world units on each side of the hero, 3.1 above and 1.4 below on the tested portrait sizes. Smaller windows proportionally reduce those reserves. Room bounds are a soft composition constraint: protecting the hero and nearby threat silhouettes takes precedence over hiding every patch of scenery. Interior horizontal follow and its exponential smoothing are retained. Movement space and speed do not change.

At the staged far-rim position (floor y=8.5, world y=4.25), the 2340-tall screen's band centre moves from 4.25 to about 0.88 world y; at height 1920 it becomes about 2.63. This brings more escape floor into view. The whole 18-unit-wide platform still cannot fit inside width 9; a boss-specific composition remains future work.

## Art and import contract

The original 1024 x 1536 image is imported as one full-rect sprite, centre pivot, 64 PPU, mipmaps off, CPU readability off, clamp wrapping, Android ASTC 6x6, maximum dimension 2048. Bilinear filtering is intentional for this painted distant layer; the procedural actors and floor remain point-filtered at 32 PPU. This is a background-specific exception, not a new character-art contract. Android compressed texels are approximately 0.67 MiB, excluding native overhead.

The view uses aspect-preserving cover scaling with 8% overscan and bounded 4% camera parallax. Part of each side of the source is cropped on tall phones; there is no stretched image or exposed edge. The plate is asset-owned and survives scene reload without a CPU copy or runtime painting. When no imported plate/camera is assigned, the previously tested cached procedural void remains available. The active scene skips generating that unused sprite set. A small existing sky mesh stays underneath as a fallback.

## Review images

- `room-0-2340.png`, `room-0-1920.png`: centre.
- `room-1-2340.png`, `room-1-1920.png`: far rim, bounded camera.
- `room-2-2340.png`, `room-2-1920.png`: right rim, bounded camera.
- `far-unbounded-2340.png`: same far-rim staging and cavern, previous unlimited follow behaviour.

These are real offscreen Unity renders, **not phone screenshots or combat tests**. Ten static visual actors use the development pack's two-Grunt/eight-Mite mix and real `EntrySides.Place` positions, without AI, health bars or combat. Some spawn positions intentionally fall outside the viewport; the figures do not promise ten enemies always fit on screen. The hero is placed at the centre/far/right points without walking there. HUD canvases are temporarily camera-space with assumed top/bottom safe insets of 100/76 pixels. The capture fixture passed 1/1 and was removed from Assets before the full suite/APK; local copy and original BMPs remain in ignored `TestResults/`. PNGs are lossless format conversions of those render captures.

The cavern adds depth without bright lava behind enemy silhouettes. The existing coarse floor and actors visibly differ from the painted background; this first layer does not complete the reference's material quality, character animation or boss composition. Next environment-art work should address quieter stone material variation, structured brass edge pieces and contact shadows at the tested scale, then a small hero/enemy production proof. No additional density, seeded offers or Forge Pulse is included.

Full automated and device results belong to the latest entries in docs/21–23.

Verified gate: 260 .NET, 272 EditMode and 79 PlayMode cases passed; clean compile/build/integrity. S23 APK SHA-256 `3704897005DA235B5C23DD70761DD2D784387489D5A896091F9241735CCB6F6F`. Live far-rim and right-corner movement kept the hero visible, a warm proof restart retained both layers, and the phone returned to Ember Hall 1/6. Actual device captures are under ignored `TestResults/device-furnace-01/`: `03-far-drag.png`, `06-live-rim.png`, `07-side-drag.png`, `09-warm-combat.png`, `11-normal-floor.png`. The short-screen renders remain simulated; a physical short/mid-range device is still untested.
