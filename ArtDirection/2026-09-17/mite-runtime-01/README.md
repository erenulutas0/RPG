# Cinder Mite runtime and foundry strikes — 2026-09-17

This slice imports the first enemy alongside Vanguard: front/rear idle, two walking contacts and strike, mirrored for the opposite horizontal direction; a short damage compression; two pooled collapse/ember poses. The sword arc and shared impact spark use four imported frames each. Combat data, attacks, rewards, spacing and progression are unchanged.

## Source and registration

Generated with the built-in imagegen tool, using the existing original `ArtDirection/2026-09-16/character-proof-01/cinder-mite-v1.png` as identity reference. Its unchanged copy is `mite-front-idle.png`. Exact generation requests and original saved locations are in `generation.json`; the replacement front step is in `generation-correction.json`. `SHA256SUMS.txt` identifies every retained source PNG. Rear movement references the generated rear idle. The two FX sheets are original generations without reference images.

The first front step A pointed its head the wrong way and is retained as a rejected variant, `mite-front-step-a.png`; **only `mite-front-step-a-v2.png` is imported**. Raw source dimensions vary by one pixel (1176/1177 x 1337); registration is fixed in source coordinates, never fit to each alpha box. Both effect sheets are 1254 x 1254 with four 627 x 627 cells, read left-to-right, top-to-bottom.

| Imported asset | Output / PPU | Registration |
| --- | --- | --- |
| Eight Mite body/flash pairs | 80 x 68 / 128 | Source crop x80, top240, 1056 x 896; normalized pivot (508/1056, 32/896). |
| Two death poses | 80 x 68 / 128 | Same crop and pivot as the living actor. |
| Four slash and four spark frames | 128 x 128 / 128 | Fixed sheet cells and centre pivot. |

All sprites are bilinear, clamp, full-rect, no mipmaps, unreadable at runtime, Android ASTC 4x4. The source bridge preserves RGBA channels; Unity downsamples/crops on the GPU and emits TGA imports plus alpha-matched white silhouettes. Sources are not painted or retouched by script.

## Runtime ownership and timing

`CinderMiteArtSet` is assigned only to the existing CinderMite prefab. Its roots, targeting, health, spacing, damage and rewards are unchanged; the existing presentation nudge is reduced to .04 units. `EnemyLookView` borrows imported sprites, advances its two walk contacts by floor displacement (.55 units per cycle), retains facing at rest and holds the strike for .14 seconds after an actual attack. Right-facing presentation is mirrored, never the actor root. Health bars are initialized independently of procedural enemy generation.

`FoundryStrikeArtSet` is the sole new scene reference. Slash rotation follows the actual hit vector; sparks land at the existing body anchor. Imported frames never enter the procedural sprite destruction list. Death uses the existing 24-slot effect pool, lasts .4 seconds and fades for the last .16 seconds. Rewards and enemy count update immediately; the corpse survives removal of the original actor. Scaled-time pause freezes these effects. Saturation replaces the oldest pool slot, so some death/coin effects can be truncated during many simultaneous hits.

## Reproduce and review

Run `pwsh -File Tools/ArtReview/Run-MiteProof.ps1 -Reimport` with the Editor closed to rebuild from retained masters and capture the real scene; omit `-Reimport` for capture only. Temporary Editor/test scripts are removed in `finally`. The command runs the .NET and compile checks; it does not build an APK. Five `MiteAnimationTests` cover sharing/reload, four facings/travel/pause, actual strike/hit silhouette, immediate death/XP plus corpse lifetime, and pooled strike rotation/tint reset. The full project gate remains mandatory before a phone install.

`runtime-*.png` are actual Unity captures. Walking captures move the real actor root, attacks use real `AttackController`/`Health` events, deaths use real pooled effects. The ten-actor proof capture lets the authored development pack walk in while attacks are disabled. Safe insets are simulated at 1080 x 2340. The browser gallery replays captures at an inspection speed; it is not the game's animation clock or a phone benchmark.

## Limits

Two walk contacts are enough for this small enemy prototype, not final production animation. There is no separate rear death sequence; the compact collapse is shared. Mirroring also mirrors lighting. Grunts and other large enemies, chest, platform and remaining effects still show the earlier procedural style. The health bars are deliberately unchanged and visually large above a small Mite. World-space foot locking, Vanguard grip occlusion, Staff/Daggers matching art and final enemy/boss production remain separate work.

Final suite/build and physical-device evidence are recorded in the newest sections of Docs/21–23. Do not infer sustained performance or a middle-tier device result from this art review.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, targeted Mite **5/5**, art/HUD **9/9**, graphics capture **1/1**, full EditMode **284/284**, PlayMode **94/94**, Android exit **0**, integrity **367 GUIDs / 555 scene objects/components**. APK **24,739,942 bytes**, SHA-256 **`23EDF4B3D2FA6DC853A4B579254D5200D5483009D1B4B1A948EB6EDCC56BEED1`**. Physical-device review and preserved progress are recorded in the newest Docs/23 entry.

Physical review: one ten-enemy Daggers proof victory on the S23, with a southwest drag, visible hit feedback and collapsed remains. `device-crowd.png` and `device-kiting.png` are unchanged phone captures. The proof flag was removed; normal Ember Hall returned. Progress was preserved, adding only the run's 10 banked gold (8588 -> 8598). Sword's new arc is verified in Unity, not in this physical Daggers run. Full timing, profile hashes and evidence paths are in Docs/23.
