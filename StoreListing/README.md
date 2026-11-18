# Store listing graphics (drafts, 2026-11-19)

Drafts of the Google Play graphics for the Turkish release path (docs/59), built from the game's own art by
`Tools/ArtReview/build_store_graphics.py`. They live outside `Assets/`, so nothing here ships in the APK. Nothing is
final until the owner approves it.

## Files

| File | Size | Play requirement it meets |
|---|---|---|
| `graphics/icon-512.png` | 512 x 512, 32-bit PNG (opaque), ~300 KB | The hi-res app icon: 512 x 512 PNG, at most 1 MB. Full bleed and square, because Play rounds the corners and adds the shadow itself. No text. |
| `graphics/adaptive-foreground-432.png` | 432 x 432, RGBA | The adaptive launcher icon's foreground layer: 108 dp at xxxhdpi. All of its content sits inside the central 66 dp safe circle (264 px across), so no launcher mask (circle, squircle, rounded square, teardrop) cuts it. |
| `graphics/adaptive-background-432.png` | 432 x 432, RGB | The adaptive icon's background layer: opaque, full 108 dp. |
| `graphics/feature-graphic-1024x500.png` | 1024 x 500, 24-bit PNG (no alpha), ~570 KB | The feature graphic: 1024 x 500, JPEG or 24-bit PNG, no alpha. The heroes stand well inside the edges, which some placements crop. The middle, where a promo video's play button would sit, is the Foundry's court. |

The review sheets are in `ArtDirection/2026-11-19/store-graphics-01/` (untracked):
- `icon-review.png`: the adaptive icon under four launcher masks at 192, 96 and 48, the layers with the safe circle drawn on, and the Play icon at 256, 96 and 48;
- `feature-review.png`: the feature graphic at 1024, 512 and 320 wide.

## Sources

- **The Vanguard:** its painted front three-quarter master, `ArtDirection/2026-09-17/vanguard-directions-01/front-idle-v1.png`. This is the master the in-game front sprites and puppet are cut from. The folder's README records it as an unchanged output of the built-in image-generation tool (2026-09-17), and the exact prompts are in that folder's `generation.json`. In other words, the store icon and feature graphic show generated art, as the game itself does.
- **Ilya:** rendered from the game's own Blender model by `Tools/ArtReview/BuildBlenderArcanist.py`. The script's docstring has the exact command. It uses the south-west idle pose, frame 2, at 4x. No generated imagery is involved.
- **The Foundry:** the runtime overview capture `ArtDirection/2026-11-12/foundry-trolley-crown-01/after/overview.png`.

The `ArtDirection/` folder is not in git. Rebuilding these drafts needs the main checkout's copy of those files, plus the Ilya render under `TestResults/`.

## What the owner should judge

1. **The icon's subject: the Vanguard alone.** It shows the helmet and shoulders over the foundry's ember glow. At 48 px it still reads as a blue knight's helmet on orange, but it says "knight" more than "forge roguelite", and Ilya does not appear. The alternatives are both heroes (too small at 48 px), or a forge emblem such as an anvil and flame.
2. **The painted Vanguard beside the 3D-toon Ilya.** In the feature graphic the two styles sit side by side: a glossy painted master and a flatter toon render. The difference shows more here than in the game, where both are small. If it bothers you, Ilya would need a painted master of its own.
3. **No logo yet.** There is no title or wordmark on the feature graphic or the icon. Play allows text on the feature graphic, so a logo could go in the top middle once one exists.
4. **Provenance.** The Vanguard master is image-generation output (see Sources). Decide whether you are comfortable with that in the store listing.
5. **The app is not wired to any icon.** `ProjectSettings/ProjectSettings.asset` has no icon today: `m_BuildTargetIcons` is empty and all 18 Android slots have no textures, so the APK ships Unity's default icon. Wiring these layers in means importing them under `Assets/` and filling the Android adaptive, round and legacy slots in PlayerSettings. That is a ProjectSettings change, and it waits for your OK.
6. **The name.** `productName` is still "Project Cryptforge" (and the application id is `com.cryptforge.prototype`). That name is what appears under the launcher icon, so it needs your decision before the closed test.
