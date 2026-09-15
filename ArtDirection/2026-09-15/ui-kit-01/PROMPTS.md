# Cryptforge UI kit 01 — exact generation record

Date: 2026-09-15. Built-in image generation; no CLI/API fallback. Outputs are opaque RGB masters. All reference images were visually inspected. Generated originals remain under C:/Users/pc/.codex/generated_images/01a095bd-7c87-7c33-83a1-013aa1757c47/. Selected masters were copied unchanged into this directory.

## Blank panel v1 (superseded)

exec-a165b4ae-2c1c-477c-892e-b023b65b9d3e.png; too much interior texture.

```text
Use case: ui-mockup.
Asset type: one standalone reusable blank rectangular game UI panel texture for Cryptforge, NOT a full screen.
Input image: approved upgrade-screen reference, STYLE ONLY. Derive the card material and restrained brass edge from its two option cards.
Primary request: generate a single horizontal 3:1 panel, target 1536x512 pixels. The panel fills the entire rectangular image, front-on orthographic, all four borders fully visible and symmetrical. Opaque dark navy interior #101522, very subtle forged-steel texture. Two narrow aged-brass bevel lines near the perimeter (#AC7243, #F8BA50 tiny highlights) with clean dark outer stroke. Corners modestly chamfered inside the rectangular silhouette. Outer rectangle background is same near-black as the panel, so no external backdrop or shadow is needed.
Designed for nine-slice UI: keep all decoration within the outer 48 pixels; top/bottom horizontal edges straight and consistent in thickness; no motifs in the central area; centre mostly flat and calm to stretch cleanly. Keep roughly 90% of the image available for later icon/text overlays.
Style: crisp restrained pixel-inspired edge steps, broad controlled colour planes, matching the reference's dark foundry material. Far less noise than a painted texture.
Text: NONE. No letters, numbers, labels, icons, symbols, sword, anvil, gauntlet, card dividers or mockup scene. No game background, watermark, multiple panels, ornamental flourishes, skulls or bones. A usable blank UI skin, not a poster of a skin.
```

## Damage icon

exec-7e5cbbaf-cd02-4a49-9f43-914da6a00378.png -> UI_Icon_TemperedEdge_v1.png

```text
Use case: ui-mockup.
Asset type: standalone square UI upgrade icon for original Cryptforge, text-free master.
Input: approved upgrade-card screen, style and SWORD ICON reference only.
Create ONE square 1024x1024 image. No interface, card, screen, border or frame. Opaque uniform near-black navy background #0D1220, edge to edge, no floor, no transparency checkerboard.
Subject: a single broad blue-steel sword on a clear diagonal from lower-left hilt to upper-right blade tip. Blade pale cyan bevel #9BEBFE, mid blue #718CCF and deep blue #354A9C; short understated brass crossguard and navy wrapped grip. One small warm amber sharpening stroke beside the blade, at most three tiny sparks. Not a second sword. This represents increased DAMAGE, not fire damage.
Composition: silhouette within central76% of canvas; generous padding; sword weight and diagonal match the reference's Tempered Edge icon. Keep square icon equally readable when shown at64px and160px. Large clear block shapes, crisp stepped pixel-inspired edges, only two or three shade planes on metal; no microtexture, fog, bloom or scattered magic.
Text: NONE. No letters, numerals, + sign, upgrade title, logo, badge, border, skull or bones. Match reference's approved dark foundry/blue steel identity. One icon only.
```

## Attack-speed icon

exec-19474232-d1b8-4300-8e95-f3ecf0c20497.png -> UI_Icon_QuickenedGrip_v1.png

```text
Use case: ui-mockup.
Asset type: standalone square UI upgrade icon for original Cryptforge, text-free master.
Input1 approved upgrade screen: GAUNTLET motif reference only. Input2 sword icon: matching rendering scale, background, colour planes and padding reference.
Generate ONE square icon, matching input2 square canvas and style. Opaque near-black navy background #0D1220. No frame or border, no interface.
Subject: ONE blue-steel armoured gauntlet clenched into a fist, pointing from wrist at lower-left toward fingers at upper-right. Chunky readable knuckle plates, short open cuff with a dark interior. Pale-cyan highlight #9BEBFE, midblue #718CCF, darkblue #354A9C and navy outline. Exactly TWO short cyan directional speed streaks alongside/below the fist, neither lightning nor a second hand. No flame or orange effects. This means attack speed, not movement speed.
Composition: central76% silhouette, same visual mass and generous padding as sword reference. Readable at64px and160px. Large crisp stepped pixel-inspired blocks and simple planar shading; avoid detailed individual rivets or realistic skin. Shape must unmistakably read as a gauntlet, not a boot, torso or weapon.
Text: NONE. No numerals, badges, labels, letters, plus signs, skulls, bones, background scene, card, watermark or extra icons.
```

## Forge Burst icon

exec-02c8c4cb-0fd4-413e-9c6e-754acb5ecb27.png -> UI_Icon_ForgeBurst_v1.png

```text
Use case: ui-mockup.
Asset type: one standalone ability symbol master for original game Cryptforge.
Input1 approved gameplay mockup: reference for bottom-right forge-burst ANVIL motif only. Input2 sword icon: reference for matching pixel-inspired drawing style and navy background.
Generate one square image, matching input2 canvas. An extremely readable WHITE-CYAN ANVIL, front-on symmetrical silhouette: flat broad top with one horn at left and a squared stepped tail at right, narrow waist, stable two-foot base. Above it exactly THREE short angular burst rays, one tall central, two shorter side rays. Keep rays part of one compact emblem. Palette #9BEBFE #4973CE and near-white centre; dark blue outline, discreet blue edge glow only. This is the hero-centred Forge Burst ability, not a lightning spell.
Uniform opaque dark navy #0D1220 background. Icon inside central70% of canvas with generous padding. No circular bezel or external frame, no outer progress/cooldown ring (those are independent runtime UI layers), no dark cooldown overlay. No text, numbers, labels, logo, fire, coins, swords, skulls, bones or second icon.
Bold broad shapes and crisp stepped edges visible at48px; minimal inner facets, no microtexture or scattered magical particles. Standalone graphic suitable for later use inside a round ability button. Do not reproduce the full screenshot or HUD.
```

## Blank panel v2

exec-28d06c20-c18a-4bef-a9ef-f94e5f5b1406.png -> UI_Panel_ForgedBlank_v2.png

```text
Use case: precise-object-edit.
Edit this standalone blank horizontal Cryptforge UI panel texture. Keep exact canvas dimensions and every existing brass bevel, black outline and chamfered corner in place. Change ONLY the interior large navy region inside the inner bevel: replace ALL diagonal tile seams, scratches, mottling, gradients and microtexture with one perfectly flat solid opaque #0D1220 navy colour. This centre must be visually uniform to permit clean nine-slice stretching. Keep the aged-brass texture strictly on the narrow metal BORDER. Outer corners/background remain near-black. No text or icons, no transparency, no other changes. This is a reusable scalable blank UI component, not a stone wall or decorative illustration.
```


