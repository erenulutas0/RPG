# Painted foundation masonry (P3) — 2026-10-04

Builtin image generation, untouched master and exact prompt in ../painted-surfaces-02-masters/P3-raw.png and P3-prompt.txt. New versioned FoundryMasonryV2 texture on the existing shared masonry material, Repeat, 1024 POT, mipmapped ASTC6x6. Original texture/GUID retained. Broad, chipped violet-charcoal basalt courses replace the navy small-brick surface. Geometry and colour headroom unchanged.

Clutter samples the same clean UV patch (.624,.867); its measured 41x41 source-texel mean is now (.2504,.2188,.2432), replacing the old texture's mean, preserving wood and cloth colours. Matched actual Unity captures include overview, bridge, gate, foundation and workshop; graphics capture 1/1. No conspicuous repeat discontinuity at gameplay scale; close-up block edges remain stylized. Static budget 17 renderers / 73,244 vertices. Compile 0 warnings/errors. Full gate results are recorded below.

Android importer observed the actual texture as 1024x1024 ASTC_6x6, 11 mips. The build tool now reports actual Android-imported dimensions/format in android-build.log, alongside packed asset sizes, so requested settings are not mistaken for shipped compression.

## 2026-10-04 - Painted foundation masonry (P3)

Integrated FoundryMasonryV2, preserving geometry and the original asset/GUID. Recalibrated the sampled masonry texel mean used by clutter so wood/cloth tones remain correct. Matched Unity proof: ArtDirection/2026-10-04/painted-masonry-01. Graphics 1/1; compile 0 warnings/errors; EditMode 410/410; PlayMode 134/134; Android exit 0; integrity 527/1099/568. Static 17 renderers / 73,244 vertices. Android build log now explicitly reports actual target textures: both P1-b and P3 are 1024x1024 ASTC_6x6, 11 mips. APK 32,374,835 bytes SHA256 7C72EA9421300E14655AAB8FE6EB9D8CB4D42818521C9CA497812127CA57F423. Built, not installed. USB phone no longer appears in adb devices; owner was asked to reconnect while desktop work continues. No push.