# Character sampling and equipment proof in Unity — 2026-09-16

This folder contains **actual Unity renders** of the preceding original hero/Mite masters over the current Gameplay scene. It is an isolated visual review, not a shipped character replacement. No authored scene, runtime class, import metadata, project setting, package lock, profile or phone application was changed.

## Findings and proposed next contract

The blue hero and ember Mites read in the real furnace scene at the candidate heights. The painted source loses its intended surface structure at 32 PPU; 64 PPU is a middle option; 128 PPU with bilinear display is the preferred **candidate for further character production**, preserving the helmet, boots and stone planes without enlarging the actors. Point-filtered 128 PPU remains available in the review. This compares sampled versions of a painted master, not hand-authored pixel art: it cannot prove that a carefully redrawn 32-PPU sprite would be inferior.

This is not a global PPU/filtering change. Environment and current weapon art remain 32 PPU. In the close views, the old jagged sword, broad staff and shield do not match the new body's material treatment. Before importing production characters, redraw one complete weapon loadout in the same family and prove its hand occlusion. The current master is a closed-fist body with no separate foreground fingers; a correctly located grip can still appear pasted over the hand.

The old left-hand anchor is too low for this pose. Candidate anchors use source coordinates (848,794) for the right hand and (345,596) for the left, in top-left PNG coordinates. Normalization uses crop bottom-centre (594,1213) and 1.375/1123 world units per source pixel. These are measured attachment candidates, not approved animation anchors. The foot pivot remains crop bottom-centre; the asymmetrical stance still needs a planted-foot walk proof.

## What was rendered

| Candidate | Hero texture | Mite texture | Display filter |
|---|---:|---:|---|
| 32 PPU | 24×44 | 16×16 | Point |
| 64 PPU | 48×88 | 33×32 | Point |
| 128 PPU | 96×176 | 66×64 | Point / Bilinear |

All variants have hero height **1.375 world units** and Mite height **0.5**. At a 1080-wide render and camera width 9 these are 165/60 source pixels. Rounded integer widths cause a small aspect approximation, not a change in height. Sources are the unchanged PNGs in `../character-proof-01/`; their prompts and hashes remain there. No new image generation was used in this slice.

Unity samples the alpha crop from a mipmapped, trilinear source texture into each candidate texture with `Graphics.Blit`. Candidate textures have no mipmaps. This is a GPU sampling experiment, not the final TextureImporter/atlas/compression pipeline. The pair at 128 PPU has 84,480 base RGBA32 texel bytes, excluding all weapons, other frames, objects and staging masters; this arithmetic is **not a device memory or performance measurement**.

- `density-*-room.png`: 1080×2340, width-9 camera, real floor/backdrop/HUD and eight staged Mites.
- `density-*-detail.png`: 600×600 with a five-unit camera width, retaining 120 pixels/world unit, over the same floor. HUD hidden for inspection.
- `loadout-0/1/2-rest.png`: candidate Sword+shield, Staff and Daggers attachment using existing procedural weapon sprites.
- `anchors-legacy.png` / `anchors-candidate.png`: the same new body and Sword loadout with old versus measured attachment positions.
- `motion-00..11.png`: rigid horizontal translation and a sword sweep; the grip follows its parent with zero measured transform error. **Not a walk cycle or a completed attack animation.** The feet never change painted pose, and no new hit/death/facing frames exist.
- `sampling.csv`, `attachment-check.txt`: captured dimensions and attachment diagnostic.

The scene is paused after startup under `TestProfile` isolation. The original hero/enemies are hidden; replacements have contact patches and depth ordering but no live combat, enemy health bars or AI. UI is the real scene's initial Sword state. Safe-area insets are simulated. The close Mites demonstrate projected depth overlap, not a replay of Opus's movement tests. No mobile GPU, texture compression, sustained movement or phone performance claim is made.

## Reproduction and verification

Run from PowerShell with the Unity Editor closed:

```powershell
pwsh -File Tools/ArtReview/Run-CharacterProof.ps1
```

The driver stages one temporary PlayMode fixture and fresh metadata, runs the existing .NET and compile checks, then runs the single graphics-enabled `CharacterProofCapture` test. A `finally` removes those exact temporary files. TestProfile protects the Editor's real profile and is ended by UnityTearDown. The test asserts fixed world height and weapon attachment during twelve diagnostic frames. Its source is kept under `Tools/ArtReview`, outside game assemblies, for reproduction.

This project omits Unity ImageConversion. The initial capture compile failed with `CS1061` for `LoadImage`/`EncodeToPNG`; the corrected bridge decodes original PNG channels into a temporary bottom-up RGBA stream and losslessly encodes Unity's RGB BMP captures to PNG. No resizing or painting occurs in that bridge. A subsequent mipmap-loading attempt failed with `LoadRawTextureData: not enough data provided`; the final fixture fills mip level zero with `SetPixelData` before generating mipmaps. These were review-tool failures, not gameplay regressions.

Results/logs: `TestResults/character-proof.xml` and `.log` (ignored). The review is served at `http://127.0.0.1:8341/character-unity-01/` by the existing loopback ArtDirection server. Production animation, equipment art, import/cache/hit-flash integration and device acceptance remain separate work. No APK was built or installed in this slice; latest device evidence remains the spacing build in docs/23.

**Final verification:** .NET 272/272; compile 0 warnings/errors; graphics-enabled capture 1/1 passed; integrity 293 unique asset/folder GUIDs and 555 scene objects/components after temporary-source cleanup. No full EditMode/PlayMode rerun: authored game files are unchanged. APK on disk still hashes to `24F1FD800362ACBDFB518C8F86D78367E3068FDF11271BF1C071F1A3476CABDA`. Browser density/loadout/frame controls and playback reviewed at desktop and narrow viewport sizes; no missing images or horizontal page overflow observed. Original concept master hashes remain unchanged.
