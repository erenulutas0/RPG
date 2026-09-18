# Foundry chest concept — 2026-09-18

## Transparent sprite candidates

Built-in image_gen edits; original output bytes retained. Closed uses `chest-states-v2.png`; opening and open each use `chest-closed-v1.png`. These are candidate poses, not a validated animation.

### Closed
```text
Edit target: provided three-state foundry chest board. Produce ONLY the left CLOSED chest as a standalone game sprite on a genuinely transparent RGBA background. Preserve its design, three-quarter camera angle, brass bands, four feet, charcoal iron panels and diamond ember emblem fixed below the seam on the FRONT BODY. Remove all other objects, ground, shadows and background entirely, do not draw a checkerboard. Simplify fine scratches to broad clean painted surfaces suited to a 90-pixel-wide mobile sprite. Keep restrained amber light in the seam and emblem; no external glow or particles. Square 1024x1024 canvas, whole chest centered horizontally, occupying approximately x=192..832 and y=320..880, reserve empty transparent space above for future opening lid. Do not enlarge to fill canvas. This is the closed master, no labels, no text, no scenery.
```

### Open / spent
```text
Edit target: transparent closed chest master. Make the SAME chest FULLY OPEN AND SPENT. Preserve canvas dimensions and exact lower-body pixel position, scale, camera, feet, brass bands, panel shape, lighting. Do NOT recenter or resize the object. Rotate only the lid backward around its rear hinge to a roughly 100 degree open angle, using empty space above the chest. Show a dark empty interior. Extinguish amber seam and the diamond emblem mounted on the front lower body, keeping the emblem geometry and position identical. No glow, no loot, no particles. Keep genuinely transparent RGBA background; no floor/shadow, no checkerboard. Clean painted game sprite, no words. Exact unchanged base registration is crucial for animation.
```

### Opening
```text
Edit target: transparent closed chest master. Make the SAME chest partway OPEN, rear hinged lid raised approximately 40 degrees, a restrained warm golden light INSIDE the opening. Preserve canvas dimensions and exact lower-body pixel position, scale, camera, feet, brass bands, front diamond ember emblem and lower panel shape. Do NOT recenter or resize the object. Rotate only lid around rear hinge using empty space above. Diamond emblem stays fixed to lower FRONT BODY, never attach it to moving lid. No light beam, no particles, no coins, no exterior halo. Simplified clean painted mobile game sprite. Truly transparent RGBA background, no floor or shadow or checkerboard or text. Exact unchanged lower-box registration matters more than detail.
```

## Continuity correction v2

Input: `chest-states-v1.png` (edit target). Built-in image_gen.

```text
Use case: precise-object-edit. Edit this Cryptforge three-state chest concept board. Preserve all three chest positions, scales, camera, lower body geometry, feet, broad brass trim and painted material style. Correct one continuity error: the diamond emblem is permanently mounted on the FRONT OF THE LOWER BOX, centered on that front face just below its rim, in ALL THREE states. On the left, move the lit diamond off the lid seam onto the lower front panel. In the middle, remove the diamond ornament hanging from the raised lid and replace the plain brass latch on the lower box with exactly the same lit diamond emblem as the left chest. The raised lid must have only the simple brass central band. On the right keep the dark unlit diamond mounted on the lower front box, matching location and size. Keep middle warm reward light compact inside chest. Also replace the smoky background glow with a uniform flat dark indigo #151522 background, retaining only subtle contact shadows. No labels or text. This is still a concept board, not a sprite sheet.
```

Built-in image_gen; new generation, no input image. Reviewed the actual platform runtime capture before writing this prompt. Concept only, not an imported animation sheet.

## Three-state board v1

```text
Use case: stylized-concept.
Asset type: original Cryptforge mobile game treasure chest state concept board, not a production sprite sheet.
Create a wide 1536x1024 art-direction image showing exactly three views of the SAME small treasure chest, evenly spaced left to right: CLOSED, HALF OPEN during reward flash, FULLY OPEN AND SPENT. No text or labels in the image.
Chest design: compact squat rectangular ancient foundry strongbox, blue charcoal iron panels, broad worn warm brass corner bands, a single small diamond shaped amber ember latch, four low square feet, beveled blocky lid with rear hinge. No wood, no skulls, no spikes, no eyes, no monster features. Premium hand-painted stylized game prop with broad readable surfaces and crisp clean silhouette, restrained highlights, matching a blue-steel armored hero and violet basalt foundry floor. Not photorealistic, not voxel art, not chunky low-resolution pixels.
Camera: fixed three-quarter view from above, see top, front and right side, ground-plane diagonals approximately 2:1 isometric. Exactly same camera, chest scale, base position relative to each cell and unchanged lower box across states; only the hinged lid and internal light change. All bases share one horizontal baseline, ample padding around and above every chest. Lid stays attached at rear hinge, rises back rather than swelling the base.
Closed: subtle narrow warm seam and small latch, clearly interactable but no fire plume.
Half open: rear-hinged lid about 40 degrees, compact warm pale-gold flash confined to opening, no particles outside silhouette, no coins flying.
Fully open: lid about 100 degrees, dark empty interior, spent latch and extinguished internal light, remains recognizable as the same prop.
Background: uniform deep neutral indigo #151522 with subtle contact shadows only. No arena, no scenery, no frames, no UI, no lettering, no watermark. Equal presentation importance for all three states. Lighting from upper left with cool soft fill. Favor silhouettes readable at only 90 pixels wide. This is one coherent three-state concept board, not three different designs.
```
