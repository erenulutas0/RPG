# Daggers source study — 2026-09-19

One original dagger master generated with built-in image_gen (exact prompt in PROMPTS.md), copied unchanged to dagger-v1.png. Use the same source for a matched pair rather than independently generated left/right weapons that drift. Short broad silver/blue-steel blade, restrained brass guard and indigo grip match Vanguard and remain distinct from the long Sword and violet Staff. No new combat, crit or upgrade behaviour.

Canvas 1254x1254 RGBA, transparent corner. Alpha>16 bounds inclusive (451,21)-(803,1221); alpha>128 (452,22)-(803,1221). Faint alpha<=16 extends nonzero bounds to (41,19)-(1193,1223); do not use naive alpha-auto-fit. No crop, alpha threshold, recolour or resampling was applied to the master. Provisional source grip (628,975) requires actual Unity registration.

The browser uses the unchanged hero-reference.png from staff-weapon-01, itself a lossless format conversion of the imported Vanguard front idle. Two DOM images align with approximate front hand points, source heights42-80px/default64, outward angle0-65/default35. This is a proportion sketch, not a finger mask or runtime animation. Displayed dimensions include transparent canvas. Mirroring repeats lighting; it is not independent facing art.

Runtime inspected: HeroArt currently creates mirrored 12x12 diagonal dagger sprites. VanguardAnimator assigns two independent hand anchors and an existing .12-unit upward thrust with neutral body during attack. The current sword/staff front knuckle overlay does not cover the second hand. Next: fixed-registration Unity sampling at phone size, decide per-hand angle/depth and finger coverage, retain immediate damage and movement budget, then shared imports/pause/reload tests and coordinated phone review. Do not replace one sprite while leaving the other procedural.

No authored Assets, C# runtime, scene, APK, profile or phone changes in this source slice. Latest installed Staff APK remains 4A73EE71112FE26005D97A9AD4AE0E986C9A6EF4AD7F8E4ED434D1AB86EF2E42; preceding S23 record is historical, not re-inspected here.

Verification: static integrity443/568 passed. Browser source and two-hand placement inspected; size56/angle45/mirror controls and original64/35 verified. No runtime suite rerun because this slice changes art-source/docs only.
