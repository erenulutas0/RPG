# Cryptforge art sprint 01 — generation record

Date: 2026-09-15. Tool: built-in image generation. All prompts below are exact submitted text. References were visually inspected before use. Original generated files remain in C:/Users/pc/.codex/generated_images/01a095bd-7c87-7c33-83a1-013aa1757c47/.

Selected images are native 853x1844 PNG concepts, not 1080x2340 production assets or Unity captures. No resampling or manual raster editing was performed.

## Gameplay A — initial composition

Inputs: TestResults/art-sprint-01/current-01.png (hero identity), ArtDirection/2026-09-14/mockups/cosmic-v1.png (mood only).
Output: gameplay-a-medium-v1.png; source exec-29e5e406-0cb7-497b-84af-1d952ab61130.png.

```text
Create one polished but implementable portrait mobile gameplay UI/art-direction mockup for original game Cryptforge. Target canvas 1080x2340, 9:19.5, full bleed without phone frame or outside captions. Use input 1 (real prototype screenshot) as the STRICT HERO IDENTITY reference: small blocky pixel-art knight, blue steel plate, dark face under helmet, short blue plume, royal-blue cape, brown belt, thin sword in right hand and small round shield left, rear three-quarter facing upper-right. Preserve these features; improve clarity without inventing an unrelated hero. Input 2 is MOOD AND PALETTE ONLY: astral foundry, dark indigo, restrained brass and ember, silver constructs. It is NOT the camera or rendering reference. Keep coarse coherent pixel clusters, crisp stepped silhouettes, minimal gradients. Game sprites should plausibly originate from 32PPU pixel sprites, not smooth painted miniature sculptures.

This is a moving-camera close section of a much larger isometric arena, not an entire diamond island floating as a centrepiece. Camera A medium-wide: hero body visually about 180px wide x248px tall on a 1080px canvas, feet around x540,y1260. Flat arena floor continues beyond left and right edges, and almost all playfield; only a small upper corner rim reveals quiet deep space. Low-contrast charcoal violet tiles (base #2D2B41, #3A3852; seams only a little darker), large restful tile fields, no heavy black grid, no brass cross under hero. Very occasional subdued brass inlay. Background remains darker/quieter than entities.

Show exactly ONE blue knight and SEVEN enemies around him, spatially separated with clear walkable lanes: FOUR small ember mites, TWO stocky molten slag brutes, ONE larger quicksilver guardian with smooth plated helmet and violet visor, no skull face. Suggested positions as fraction of canvas: guardian .72,.32, brutes .25,.39 and .74,.63; mites .45,.28 .78,.46 .25,.58 .46,.67. Keep ALL bodies completely visible with comfortable screen margins. One closed brown-and-brass chest at .24,.73, a modest warm loot glint. Knight at .5,.53, small blue sword arc hitting one nearby mite, only two modest damage numbers ('10' and '6') and two small coin sparks. No giant area ring, no cursor. Characters belong naturally to floor with tiny tight shadow ellipses.

Compact precise HUD, clean readable pixel typography, thin brass edging instead of large ornament:
- top safe margin about70px, compact strip from y80 to190: left 'F1' and six small room pips (first two lit), right one gold icon and '120', small pause button far right. No game title.
- Second compact row y200 to300: exactly three small square/round medallions, sword badge '2', boot badge '1', counterweight badge '4'. Tiny subdued label 'ROOM 2 / 6'. No unreadable paragraphs, no global boss bar because these are a composition stress test pack.
- Keep y330 through1860 largely unobstructed arena.
- Bottom ergonomic HUD inside safe area y1970..2240: one restrained dark metal panel on lower left/centre, label 'VANGUARD', red HP bar '84 / 100', thinner blue XP bar and 'LV 3'. A single large circular blue forge-burst ability button on lower right (diameter180px, x920,y2130) with clear white-blue angular anvil/burst glyph and subtle ready outer ring. No second active button. Keep bottom90px clear.
- Gesture control is drag-anywhere, so no permanent joystick in this static mockup.

Overall: restrained handcrafted pixel-art game UI, uncluttered contrast hierarchy: hero/threat/loot first, floor second, space last. Authentic in-game legibility more important than illustrative ornament. No skulls, skeletons, bones, graves, imitation of any franchise, watermark, mockup caption, logo, text outside UI. This is Camera A; do not put A or camera numbers on the canvas.
```

## Gameplay B — rejected first variation

Input: original generated A.
Source exec-728f73c1-268d-49f3-92a2-ca01c505db1b.png. Rejected: changed arena architecture and shifted the hero too far down; original retained in generated_images, not selected into this package.

```text
Edit the supplied portrait Cryptforge gameplay mockup into camera comparison variant B. Keep canvas and EVERY HUD element, wording, position, scale and values identical. Keep the same exact seven enemies (four ember mites, two slag brutes, one silver visor guardian), same blue knight identity, same single chest, same health state and blue sword attack snapshot. Only change world camera scale: zoom the entire arena and ALL world objects out by 20 percent around the HERO FOOT ANCHOR (approximately x.50, y.496 of the supplied canvas). Thus hero stays at the same screen position but hero/enemies/chest/floor tiles become 80 percent of their former width and height; their relative positions converge toward that same anchor. Expand the newly visible arena around them coherently. Maintain isometric perspective and coherent floor height; there should be no walkable floor outside the island edge. No character may be clipped. No extra enemies, hero copies, loot, health bars, icons or wording.

Make the floor slightly quieter: reduce tiny stone scratches and flecks and tile seam contrast; preserve the existing charcoal-violet palette and rare brass inlays. Keep the cosmos subordinate, quieter than combat, occupying only space genuinely beyond the platform. No large decorative towers obstructing floor. This is a controlled wider-camera alternative, not a new scene. All UI exactly the same size and location as input, no new captions or labels. Crisp pixel-art-inspired rendering with stepped edges, retain blue cape/plume/round shield/sword, no skulls or skeletons.
```

## Gameplay B — selected corrected variation

Input: original generated A.
Output: gameplay-b-wide-v2.png; source exec-8944bc43-e147-444a-90be-0b2b44c88172.png. Residual geometry/anchor drift remains; artistic scale comparison only, not a calibrated camera experiment.

```text
Produce a tightly controlled wider-camera EDIT of this exact Cryptforge portrait gameplay image. The input canvas is 853x1844. Keep exactly that canvas and all HUD absolutely unchanged, including top strip, three buff medallions, gold120, pause, bottom HP84/100 LV3 and ready ability button. Preserve the exact scene layout and architecture from the input, no redesigned island or new interior walls or extra floor motifs.

Zoom WORLD ONLY outward modestly (linear world artwork scale 0.8), around the hero FOOT PIVOT at (425,910) in this 853x1844 image. Hero feet MUST REMAIN at x425,y910 after zoom; do not move hero toward bottom. The seven enemies, floor tiles, chest and architecture all scale with this same transform. Keep same four small ember mites, two molten brutes, one silver golem, same shadows, same attack and coin snapshot, same blue knight pose and identity. Hero width/height reduced20%, guardian width/height reduced20%, every other world element likewise. Scene details not identity redesign.

Fill extra revealed world with continuous quiet charcoal-violet arena flooring. The broad arena extends beyond both side edges. Do NOT fit the entire diamond island on screen; no full island outline. Do NOT add walls across the playable space. Where a genuine boundary appears preserve the original rim style and place dark cosmic space below its exterior; never draw same-height playable tiles beyond a vertical exterior face. Retain some dark nebula in distant upper-right but minimize decorative towers. Keep the exact same restrained palette and pixel-inspired edge treatment as input. Do not introduce captions, labels or extra text. No skeletons or skulls. This is the same location and moment through a slightly wider lens, with UI anchored to screen.
```

## Upgrade choice

Input: original generated A.
Output: upgrade-choice-v1.png; source exec-57d62ca6-4ea6-4521-a1eb-db0dc6a9eed5.png.

```text
Create the LEVEL-UP CHOICE overlay state for this exact Cryptforge portrait mobile gameplay mockup. Preserve its 853x1844 portrait canvas, underlying blue-knight astral-foundry scene, charcoal-violet stone/brass palette and crisp pixel-inspired styling. Dim the entire underlying gameplay and hide its HUD while modal is open: no old status paragraphs, buff medallions, gold, HP or ability control visible behind/between cards. Background scene remains faintly recognizable in the UPPER HALF; do not add characters, loot or enemies.

Place one cohesive opaque navy-black forged-metal choice panel from x45 to808, y900 to1720, with restrained bronze edging and modest warm forge glow at the top central anvil emblem. It is an accessible one-handed selection screen, not an ornate fantasy book. Clear title centred 'LEVEL UP', subtitle 'Choose one upgrade'. Inside, exactly TWO large vertically stacked full-width selectable cards, with generous padding and at least28px vertical separation:
Top card (approximately x80 y1090 w693 h230): square icon at left with a crisp blue steel sword and a small amber sharpened-edge spark. Text block right, two lines only: 'Tempered Edge' and '+50% damage per hit'.
Bottom card (approximately x80 y1355 w693 h230): square icon left with a crisp blue gauntlet and two short cyan speed strokes (no boot). Text right: 'Quickened Grip' and '+50% attack speed'.
Both cards equal visual priority, thin brass outline, dark indigo filled body, large cream title, smaller pale-blue description. Do not put action buttons inside the cards: entire card is the tap target. No selected state, no invented rarity, cost, rarity stars, stack count or reroll/skip feature. Use exactly these English strings and spell them correctly. Large readable text without flourished fonts, no tiny microtext. Maintain bottom safe-area clearance. Only the modal text is visible. No big game title, no extra slogans, no skeleton or skull glyphs. This is one game screenshot, not a presentation slide.
```


