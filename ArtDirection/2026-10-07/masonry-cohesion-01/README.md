# Masonry cohesion V4 — 2026-10-07

## Intent and asset

Replace repeated diagonal fracture facets in the shared masonry albedo with quieter dressed-stone faces. Preserve the five-course staggered bond, dark grey-violet palette, narrow joints and restrained edge wear. The purpose is to join the gate's painted door and surrounding procedural masonry visually, not to add geometry or brighten the scene globally.

New source: Assets/_Project/Art/Props/Foundry/FoundryMasonryV4.png, generated with built-in image_gen using V3 as the edit target. Full prompt and output/import notes are in prompt.txt. Original V3 and its GUID are untouched. MAT_FoundryMasonry now references the new versioned texture; no other material properties changed. Source RGB means: V3 (69.0,65.4,70.9), V4 (68.5,64.3,69.5). No raster postprocessing was performed.

Import retains the existing texture contract: non-readable, mipmapped, repeat, trilinear, Android max1024 ASTC6x6 with POT scaling. Removed inherited unused sprite-sheet metadata from this new Texture2D asset. Existing asset GUIDs preserved; V4 owns a fresh GUID.

## Visual review

Same-camera Unity before/after gate detail, gameplay portrait, bridge, pit and overview captured. Large diagonal fractures are reduced on the gate towers and parapets; block courses still read. The shared material also changes bridge and foundry masonry, so acceptance includes those areas. Static count remains 17 renderers / 80,344 vertices; no scene, camera, lighting, layout, collision or gameplay edits.

Graphics capture passed (1). Full suites/build and device review passed; closure below. This is a material improvement; faceted geometry and the difference from the concept remain. Do not describe the reference as achieved.

## Closure

Final EditMode 410/410, PlayMode 143/143, Android exit0. Integrity537/1099/568. APK 59FCABA6D557886D68D2E0E1230CB2D8C1076F2C3FC776A48F18E67D203C0A2E (35,934,327 bytes) installed on S23, base.apk SHA256 matched. Actual gate reveal and natural handback to Staff combat captured and inspected. No managed exception/crash pattern in current-process log. This is a short visual smoke check, not a new FPS/dense-wave/midrange benchmark.

Fresh backup TestResults/masonry-device-20261007. Profile and backup plus all original development files hash-identical. No diagnostic flags added; original arrival opt-in kept, app stopped, no profile restore. Old V3 remains for comparison/reversal. No push.
