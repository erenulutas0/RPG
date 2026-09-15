# Cryptforge — compact HUD kit 02

Date: 2026-09-15. Status: art-only component continuation of the approved A prominence + B quiet floor + C choice-card direction. The owner explicitly confirmed that Ultracode is still executing telemetry and asked Astra to continue in art files. Consequently this round does not edit runtime code, Assets, shared project docs, tests, build configuration or the phone.

## Deliverables

| File | Native size | Format | Visual role |
|---|---|---|---|
| UI_Icon_Gold_v1.png | 1254x1254 | RGB, opaque | Single coin with anvil stamp; one currency counter |
| UI_Icon_Counterweight_v1.png | 1254x1254 | RGBA | Heavy brass counterweight; retaliatory relic |
| UI_Icon_SecondWind_v1.png | 1254x1254 | RGB, opaque | Blue heart and breath ribbon; low-health recovery relic |
| UI_Bezel_Ability_v1.png | 1254x1254 | RGBA | Circular blue-steel/brass rim, transparent centre and exterior |

index.html assembles these masters with kit01's damage/speed/burst icons and shared panel. It contains a compact progress/coin/pause row, two upgrade badges plus the single equipped relic, grouped HP/XP and a layered ability-button study. The gameplay background is a CSS-cropped copy of the earlier A illustration for context only; it does not reproduce the current game or implement the approved quieter floor. The gold/relic/state controls and pause button affect only this local preview. PROMPTS.md preserves exact submitted prompts and original source names.

## Semantics and visual choices

- Currency uses a round coin; Counterweight uses a wide trapezoid. Their shapes remain distinct without relying on gold colour alone.
- Counterweight's badge is a trigger count (illustrative4), not a stack count or multiplier. The source definition says it strikes back after an enemy hit.
- Second Wind heals once at low health. The heart/ribbon depicts recovery, not resurrection or an extra life. Ready uses a check; spent uses a muted icon and a cross, with a text explanation. Preview translations are drafts.
- Only one equipped relic is shown at a time. Swapping the preview selector does not equip anything in the real profile.
- Damage/speed badge counts are examples. The attack-speed motif stays a gauntlet, never a boot implying movement speed.
- Ability layers: opaque navy well and glyph, transparent bezel, independently driven progress ring, then live cooldown numeral. The ring is not baked into the bezel. The preview's4/8s is a static fixture, not a working cooldown simulation.
- No text or numeric values are baked into PNGs. Progress pips, pause bars, status marks and text are lightweight UI shapes/text in the preview, intended for the existing uGUI system later.

## Asset inspection

Inspected every generated output. No clipped icon silhouette, text, skull or bone motif in the selected masters. Saved original pixels without resizing or raster editing. Native dimensions/format were read from the actual PNG files.

Counterweight: 32-bit ARGB; corner alpha0, centre alpha253. It contains partial alpha in the artwork, so compare it over both dark floor and UI backing before import acceptance. Do not assume every interior pixel is opaque.

Bezel: 32-bit ARGB; corner alpha0 and centre alpha0. The centre reveals the separately placed Forge Burst image in the browser. Gold and Second Wind are24-bit RGB with opaque backgrounds; keep them in deliberate navy icon wells. Do not chroma-key dark colours to manufacture transparency.

Source resolution is not shipping resolution, not a fixed pixel grid, and not a change to the authoritative 32PPU character contract. Edge cleanup, final export size/compression, sampler choice and Unity import remain pending. The bezel source has much finer clusters than the small icons; inspect its edge at final button size rather than forcing one shared world PPU.

## Browser verification

Used the in-app browser with the local preview at127.0.0.1:8341/ui-kit-02/. At360 CSS px, the six room pips, floor label, six-digit gold999999 and44px pause control fit without overlap. Switched to spent Second Wind, checked its muted icon and cross, tapped it and observed the matching explanation. Inspected the grouped lower panel and the4-second numeral through the transparent bezel, with the progress arc outside the frame. Values are illustrative and not user analytics. A final visual check uses A's closer artwork after the initial background context used B.

This is browser layout evidence only. No Unity/.NET tests, APK install, camera/device validation, telemetry modification, profile input, commit or push was performed in this round. No additional assistant was started. Preserve Ultracode's in-progress changes.

## Later integration boundary

The owner has already approved the combined direction; do not ask for A/B selection again. Once the telemetry writer releases the project/build/device, integrate one small UI slice at a time using the existing views and current character sprites. Begin with kit01's choice cards and icons. Then compact HUD and body-bound camera tests. Follow docs/25's full test/build gate for runtime changes, and record actual evidence in21/22/23 then. Do not mark these art files as an implemented gameplay slice.

## SHA-256 of saved masters

| Master | SHA-256 |
|---|---|
| Ability bezel | 8B981ACFD36A04ACE277A56CA4356BB90AF6CA7D075907277CE665A21E15E7D1 |
| Counterweight | 414447A02A546DCB65239324FADB6CFF7FD4CF579E628EC81FAB12735FAE2331 |
| Gold | 8D4F1743A1020CAB6463697A9CA8698352231848301583ACA0BF97B6812DBC25 |
| Second Wind | 0706CE5BA041FDE5D297197BE44130EC06B531F3D281CFDDBFE96F5BD64C4186 |
