# Art sprint 01 — Unity integration

The owner approved A's character prominence, B's quieter floor and C's opaque card hierarchy. The telemetry worker completed commit `127f4de` before this integration began. This slice changes presentation only; current combatants, movement, rewards, upgrades, telemetry and save rules remain in place.

## Implemented presentation

- The combat HUD has a two-line floor/room readout, six room pips, one gold counter, the equipped weapon name, actual upgrade-stack badges and the equipped relic. The boss readout is visible only in boss rooms. Detailed weapon, relic, target, wave and at-risk-gold text is available while paused.
- HP and XP share a lower panel, with red health and blue experience. The ability uses a circularly masked imported glyph, a separate bezel, the existing radial cooldown and a whole-second countdown. Imported sprites are not destroyed by the view; the procedural fallback remains available when no sprite is assigned.
- Upgrade cards use the reusable forged panel and live text. Icons resolve through the active offer's stable upgrade ID. Forge/checkpoint options and unknown future upgrades show no unrelated icon. The existing input delay, choice priority and pause rules remain unchanged.
- `PlatformArt` has local charcoal-violet tile/grout colours, less speckling and unlit inlays. The rim, geometry, faces, actors and global palette are unchanged.
- The initial scene camera is 6 world units wide. Bounds checks cover 6 and 7.5 at 1080x1920 and 1080x2340, including the guardian's full body/bar rectangle and horizontal follow-lag allowance. Those checks are analytical, not a paired phone screenshot comparison.

## Imported sources

Eight original PNG masters from `ui-kit-01` and `ui-kit-02` are copied unchanged to `Assets/_Project/Art/UI/`. Their generation prompts and master hashes remain in those kits. No generated gameplay mockup or new character art was imported.

UI imports use full-rectangle sprites, 100 UI pixels per unit, clamp, no mipmaps and bilinear sampling. The seven square images have a 256 maximum import size; the 2172x724 panel uses a 4096 limit to preserve its native dimensions. The panel's 128-pixel borders are rendered with a pixels-per-unit multiplier of 7, approximately 18 UI units per corner. These raster UI masters are not grid-cleaned world sprites and do not change the world's 32 PPU/point-filter contract. Default uncompressed imports prioritise this first device review over shipping size optimisation.

The Counterweight and bezel sources retain their real alpha; the other icons retain their opaque navy backgrounds. No chroma-keying or synthetic transparency was applied.

## Verification and handoff

Runtime scripts: `RunHud`, `RunChoiceView`, `AbilityButtonView`, `PlatformArt`; authored scene: `Gameplay.unity`; text data: `PrototypeText.asset`. Two one-shot Editor migrations authored the scene/imports and corrected the first device review's blue health bar and visible square icon corners, then were removed. `TestResults/ApplyArtSprint.cs` and `TestResults/PolishArtSprint.cs` are local migration records, not runtime dependencies.

`ArtHudFlowTests` adds checks for pause details/live gold, icon identity after a stack cap changes offer order, actual raycasting through a card icon, imported-sprite lifetime across restart and guardian bounds at the two candidate widths. Existing flow, telemetry, parity, touch-canvas and text-bound tests remain required.

Final suite results, installed APK hash and actual phone findings are recorded in docs/21, docs/22 and docs/23 after verification. Browser mockup checks are not device evidence. Remaining work includes walk/facing animation, new content icons, sound, a representative mid-range device and shipping texture optimisation.
