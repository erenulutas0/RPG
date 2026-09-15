# Cryptforge — art sprint 01

Date: 2026-09-15. Status: owner approved the combined direction — A's character prominence, B's quieter floor and C's upgrade-card hierarchy. Approval recorded in docs/19 and docs/08. Exact camera width and production assets still require in-engine validation. No runtime integration or sprite replacement is delivered by this package.

## Selected outputs

| File | Purpose | Native size |
|---|---|---|
| gameplay-a-medium-v1.png | Closer combat composition; preferred starting point for character prominence | 853x1844 |
| gameplay-b-wide-v2.png | Wider composition; more travel space, smaller characters | 853x1844 |
| upgrade-choice-v1.png | Two existing upgrades in one opaque, themed modal | 853x1844 |

Open index.html for a side-by-side comparison. These are AI-generated concepts, not final pixel assets, screenshots of an installed build or calibrated Unity camera renders. Their aspect ratio approximates the target 1080x2340. Native files are preserved without stretching. PROMPTS.md records every generation, reference role and rejected iteration.

## Direction and recommendation

Keep the accepted astral foundry: blue-steel hero, ember creatures, silver guardian, charcoal-violet floor and restrained brass. Preserve existing character sprites during the first UI integration. The generated drawings retain broad character identity but do not reproduce the current procedural sprites pixel for pixel and must not silently replace them.

Prefer A's character prominence with B's quieter floor treatment. A makes the player and individual threats more readable; B provides more planning space but reduces the hero's presence. The first B was rejected because it rebuilt the island and displaced the hero. Corrected B is a better composition study but still shifts architecture and hero position; use it to discuss scale, never as proof of an exact 6-to-7.5-unit camera change.

The next real comparison should render the same paused scene at both candidate widths with the same hero anchor, pack positions, safe area and HUD. Choose a camera after that device test. Camera range should not compensate for unreadable sprites or change weapon reach.

## Visual hierarchy

- Top: floor/room progression, one gold counter and pause; compact weapon/upgrade/relic badges below. Remove the large game title and permanent debug-like stat paragraphs from combat.
- Centre: hero, readable threat silhouettes and one chest objective. Keep the playfield open for drag steering. No permanent joystick is proposed.
- Bottom: grouped HP/XP and one active ability. Its ready state should read through silhouette, rim and brightness, not colour alone.
- Floor: reduce internal seam contrast and scratches locally in PlatformArt. Keep rim/void separation clear. Do not change shared palette entries without checking all consumers.
- Level-up: one opaque panel, equal-priority full-card choices and a dimmed background. No old HUD text in the gaps. Retain existing upgrade effects and selection flow.

The combat scene is a seven-enemy composition stress case (four mites, two brutes, one guardian), not an actual Room 2 wave or new spawn prescription. HUD numbers and badge stacks are illustrative display fixtures, not captured runtime state. Bind actual definitions and state during integration; never implement a new buff merely because an icon appears here. The boot in the gameplay concept represents attack speed; use the gauntlet/speed motif from the choice card for the production badge to avoid implying movement speed.

The concept omits the persistent range ring to assess clutter. Changing its real visibility/timing remains a separate view decision; Forge Burst remains hero-centred, with unchanged radius, damage and cooldown. Show a boss bar only in an appropriate boss encounter, not the normal HUD fixture pictured here.

## Layout intent — target phone 1080x2340

Coordinates below are implementation targets, not measurements of the generated pixels. Anchor to the actual Safe Area and convert through the existing 1080x1920 CanvasScaler; do not paste physical-pixel coordinates directly into RectTransforms.

| Region | Initial target in physical screen pixels | Behaviour |
|---|---|---|
| Upper clearance | About 70, enlarged for actual safe inset | No control under cutout/status inset |
| Progress/gold/pause row | y80–190, horizontal padding 40 | Pause touch target at least 96x96; reserve six room pips |
| Build badges | y200–310 | Only occupied slots; actual stack badges; no tooltip paragraphs in combat |
| Unobstructed arena | Approximately y330–1950 | Camera anchor derives from actual top/bottom HUD bounds |
| HP/XP group | Approximately y2040–2220, x50–780 | Red health, distinct thinner blue XP; live state values |
| Ability | About 190x190 centred near x930,y2120 | Actual button raycast area matches artwork; ready/cooldown/disabled states |
| Bottom clearance | At least 90 plus actual inset where necessary | Do not overlap gesture navigation |
| Choice overlay | Anchored in lower-middle safe area | Two equal large cards, 28–40 px gap; fit text/localization on shorter screens |

On 9:16, keep readable touch targets and reflow vertical gaps; do not squeeze the same illustration into the shorter screen. Decorative frames should become reusable sliced UI parts, with live legacy Text over them, not text baked into textures.

## Camera experiment and unchanged asset contract

At screen width 1080, screenPixelsPerWorldUnit = 1080 / visibleWidth. Texture pixels are 32 per world unit. Full sprite-rectangle sizes below are analytical targets; occupied artwork may be narrower and weapons can extend beyond the body.

| Candidate | Pixels/world unit | Pixels/texel | 32x44 hero rectangle | 49x56 guardian rectangle |
|---|---:|---:|---:|---:|
| Current 4.6 | 234.78 | 7.34 | 234.78x322.83 | 359.51x410.87 |
| Test 6.0 | 180 | 5.625 | 180x247.5 | 275.625x315 |
| Test 7.5 | 144 | 4.5 | 144x198 | 220.5x252 |

These computed values are not achieved measurements of the generated images. The 6/7.5 candidates are proposals, not approved tuning. Use full body, weapon and health-bar bounds plus a margin when testing clipping. Current boss stopping distance 1.6 plus half body width 49/64 is about 2.366 units, already beyond the current 2.3-unit half view.

Authoritative import contract remains docs/25: 32 PPU, point filtering, bottom-centre body pivots; hero32x44; Grunt31x30, Runner21x32, Tank45x36, Captain35x40, Warden49x56, Mite15x16; chest24x22. Hero weapons stay separate. No sprite-sheet production or resolution change has been approved. Generated gradients, shading and outlines need grid/palette cleanup before becoming real assets.

## Integration handoff after owner selects the direction

One writer owns the scene and affected UI files at a time. Keep existing character drawings for this slice.

1. Compact HUD and grouped lower panel: RunHud.cs, Gameplay.unity Safe Area objects, PrototypeText.asset. Preserve pause and ability interaction, room flow and culture-aware formatting.
2. Floor treatment: PlatformArt.cs only where possible. Preserve ArenaGeometry and collision/rim rules. The generated raised walls, towers, loose rocks and floor beyond walls are illustrative artefacts, not approved new obstacles or map geometry. Actual rim should remain low enough not to hide combatants.
3. Camera: FollowFraming.cs / ArenaCameraFollow.cs; test 6 and 7.5 on one identical scene fixture with body-bound margins and unchanged combat rules.
4. Choices: RunChoiceView.cs / existing Upgrade Canvas; opaque panel plus dim backdrop, two full-card hit areas, hidden/occluded old HUD, no reroll/rarity feature. Background gameplay remains paused in place; the generated card picture happens to occlude/remove characters and does not prescribe despawning them.
5. Ability visuals: AbilityButtonView.cs / AbilityArt.cs; preserve runtime cooldown. Create ready/cooldown/disabled visual states as a subsequent asset set once the shape is selected.

Relevant checks: PlatformArtTests, FollowFramingTests, ArenaViewTests, AbilityFlowTests, existing choice/flow assertions, EveryButtonSitsOnACanvasThatReceivesTouches and EveryButtonLabelSitsInsideItsButton. Verify long localized strings and Turkish numeric formatting. Update docs/23's old colour-based choice detection if the cards change. Run the full docs/25 test/build gate for real integration.

Phone acceptance for integration: centre and rim, guardian on both sides, chest visibility, drag steering, button presses/drag exclusion, choice/pause transitions and short/tall safe-area layouts. Check focus/awake/keyguard before every input/capture and after each capture. Record actual suite counts and APK hash only after running them.

## Review evidence and remaining limitations

- Current reference: TestResults/art-sprint-01/current-01.png, captured from com.cryptforge.prototype on authorized S23, 1080x2340. Focus/awake/keyguard gate passed before capture and again before pulling it. It shows the existing level-up modal with a faint old status line between cards.
- All three selected generated outputs were visually inspected. Both combat concepts contain one hero, seven enemies and one chest with no clipped character bodies. Choice labels and the two existing effect strings are readable and correct; no old status text bleeds between cards.
- Native PNG dimensions and SHA-256 were checked after copying. A: 7EF018332CBC5CADBFDEA5E9D96BCE51FEA8409EBF12593F7A4629BCCF077EAD; B: 06F35CA81547042744B67E705D7CBB79BB39312060E4B10088EE64A87A2BFBC0; choices: 53C99F335C5454F524B591DABFBBAAA5F4F66D2706043A7B078B32DCAFD74139.
- Generated architecture, pixel grid, camera anchors and exact sprite sizes are not production-accurate. Do not trace generated walls into the current collision model. Short-screen layout, accessibility contrast and final icon/font sizes require real UI/device validation.
- No gameplay/Assets/ProjectSettings/package/meta files changed. No APK built or installed, profile reset, commit or push in this art round. No Unity/.NET tests rerun: output consists of concepts, a gallery and documentation, with no compiled/runtime change.

Owner selected the recommended combination after this review. Next: separable HUD/card components in ../ui-kit-01, then an actual in-engine comparison before creating a full character roster. Ultracode is executing the independent telemetry prompt; do not duplicate or interrupt that slice.
