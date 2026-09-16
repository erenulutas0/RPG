# Vanguard four-direction prototype — 2026-09-17

This slice adds a painted front three-quarter body with eight poses, two revised rear passing poses, and four runtime diagonal facings. The two left facings mirror their matching right-facing presentation subtree. It is not an eight-direction painted sheet or final foot-locked animation.

## Sources and import

Ten new PNG masters are unchanged outputs of the built-in image generation tool. `generation.json` retains exact prompts, edit references and original output paths; `source-sha256.txt` records workspace hashes. The front idle is derived from the approved rear Vanguard; all other front poses use that front idle as their edit reference. Rear passing edits use the preceding runtime masters. No Python image painting or background replacement was used.

Rear assets retain the previous fixed crop and 128x176 registration. Front assets use a single fixed source crop `(224,89,816,1152)`, output 128x180, 128 PPU, pivot `(370/816,16/1152)`. Source coordinate `(594,1225)` maps to the floor origin. The slightly taller canvas retains the front boots without changing the approximate drawing scale. Hand anchors are recorded per pose in `Tools/ArtReview/BuildVanguardAssets.cs`; the 1-pixel source-width difference is not auto-fitted per frame. Bilinear, no mipmaps/read-write, full-rect, clamp and Android ASTC 4x4 remain Vanguard-specific settings. Existing GUIDs and gameplay scene remain unchanged.

Imported front/rear bodies, silhouettes and equipment are shared borrowed assets. `VanguardArtSet` supports an optional complete front set; the old rear-only fallback remains valid. Neither scene unloading nor mirroring creates/destroys imported textures.

## Runtime contract

`VanguardAnimator` reads actual floor displacement. Travel chooses visual facing when not in a strike/recovery; each direction axis has a 0.15 normalized dead band and idle retains the previous facing. Only the body/equipment/flash subtree mirrors: the hero root, movement and floor shadow do not. Cardinal directions retain the previous value of the other axis. This is deliberately a four-diagonal approximation.

When stationary, anticipation faces an in-range target and the real `Struck` event supplies the actual hit target, even if killed. Moving heroes keep travel facing. Strike/recovery hold the facing until completion. Damage still precedes the immediate strike pose; the view neither changes targeting nor ticks/applies combat. Pause freezes body motion. Existing unscaled attack nudge/flash behaviour remains.

The sword draws behind the rear body, letting the gauntlet hide its grip; the front sword/buckler draw in front with pose-specific anchors and a lower outward idle angle. This improves attachment but is not individually painted finger occlusion. Staff/Daggers retain their existing procedural equipment and attack feedback.

## Review and reproduction

`pwsh -File Tools/ArtReview/Run-VanguardProof.ps1 -Directions -Reimport` rebuilds the imported set and captures the actual runtime hero in all four facings with all three loadouts using isolated test profiles. Omit `-Reimport` to capture only. Close Unity first. The fixture is temporarily staged, then removed; the gallery uses Unity render PNGs, not phone recordings. Its timed frame playback is a review aid, not the exact runtime cadence.

`VanguardAnimationTests` covers walking, pause, release, actual attack timing, all four facings, cardinal dead band, lethal-target facing, mirrored front flash, death/reload and borrowed sprite lifetime. The full parity suite remains required before device installation. See latest Docs/21–23 for measured gate/device results.

## Remaining limitations

Mirroring also reverses the light direction and apparent weapon handedness. Passing poses improve the support foot but do not enforce a world-space planted foot; cape/arm continuity and attack nudge remain visible. Sword finger occlusion and dedicated front Staff/Dagger poses need polish. Enemies/effects are still procedural, and this slice is not a new crowd-performance or mid-range-device benchmark.

## Verified build and device

.NET 272/272, clean compile, targeted PlayMode 6/6, graphics capture 1/1, full EditMode 284/284 and PlayMode 89/89 passed. Android build exited 0; integrity checked 333 GUIDs and 555 scene objects/components. Installed S23 APK SHA-256 matched `C793A3F11F58AB82049892BB939D9CEC606893D3ABCBEC2974FAA0F8173A329A`. All four Sword facings were captured during live drags. `device-front-left.png` and `device-rear-left.png` are unchanged phone captures; all other rendered comparison frames are Unity Editor captures. The preinstall/final stored profile remained identical (revision 111, 8007 gold, Sword/Second Wind). The phone was left on the Cinder Walk 2/6 level-4 choice; read Docs/23 before touching it again. No new performance benchmark was attempted.
