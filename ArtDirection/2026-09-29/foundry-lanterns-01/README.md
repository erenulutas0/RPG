# Foundry perimeter lanterns - 2026-09-29

Four existing exterior posts receive enclosed iron lanterns instead of their stone tips: (-18.5,-6), (2,-14.5), (-14,18.5), (10,18.5). Their original feet and walkable bounds remain unchanged. Each lamp has a supported foot, two visible hot panes, cage bars and a hipped metal roof. Existing ember texture is sampled unchanged. Static floor lighting includes four matching warm sources.

The first roof/cap mapping was too dark; switched these parts to complete-face clamped UVs and explicit roof edges. Initial -11 panes added one renderer and failed the existing <=17 static renderer test. Instead of changing the test, panes are now split into inset windows around the cage bars and share the existing -10 flame batch. No new material asset or texture.

Files: FoundryRegionSite.cs and FoundryRegionFinish.cs. No realtime lights, particles, per-frame work, gameplay, collision, camera, scene or save changes. No agents used. These are static contained flames.

## Evidence

Final graphics-enabled five-waypoint Unity capture passed 1/1; actual closeup reviewed. Before imagery is the preceding court finish. Final static budget: 17 renderers / 57,560 vertices, versus 17 / 57,432 (+128). Existing atmosphere remains separately 1 / 780. Compile check before cap UV refinement passed 0 warnings/errors; integrity 515/1099/568 passed. The first full gate passed 405 EditMode and 129/130 PlayMode; the renderer-limit failure was corrected as described above and the full gate restarted. Final results follow below.

No phone installation or device performance benchmark in this turn. Reference-image fidelity remains in progress.

## 2026-09-29 - Exterior post lanterns and warm floor pools

Four existing exterior post tips now carry enclosed iron lanterns with two visible window faces, opaque cage bars, supported caps and hipped roofs. Four matching static floor-light sources added. FoundryRegionSite and FoundryRegionFinish only; existing texture reused without editing. No gameplay, walking/collision, scene, camera, save, per-frame light or particle changes. No agents used.

First full gate caught a renderer regression: 18 static groups exceeded the existing <=17 test. Kept the test unchanged; split panes into inset windows around bars and reused the existing flame batch. Final actual Unity capture1/1 passed with 17renderers/57,560vertices (+128 from prior), atmosphere1/780 unchanged. Final closeup and overview reviewed; cap UVs corrected after dark first render. Final full gate EditMode405/405, PlayMode130/130, Android exit0. Earlier integrity515/1099/568 and compile0warnings/errors passed. Proof ArtDirection/2026-09-29/foundry-lanterns-01.

APK actual39,340,606bytes, localtime2026-09-29 19:36:46, SHA2561C4A1D885F27C7A5B2DFDA74FF16B990D24BC0193887DD27229C34C3EECA6688. READY NOT INSTALLED. Includes previous uninstalled art passes. Last verified installed remains18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations, save edits, new device performance claim, commit or push. Reference-image quality remains in progress.
## 2026-09-29 - Combined art installed and reviewed on S23

Owner confirmed phone free and authorized installation/review. Fresh backup TestResults/foundry-lanterns-install-20260929/before. adb install-r succeeded; installed base.apk SHA256 matches1C4A1D885F27C7A5B2DFDA74FF16B990D24BC0193887DD27229C34C3EECA6688. All7 profile/backup/development/telemetry files byte-identical immediately after install. Supersedes prior not-installed status for included art passes.

Physical review: center stone ring visible in combat; guarded southeast/northeast movement reached exterior lantern, warm paving, masonry and boiler/pipe details (07-lantern.png). Initial startup run ended in Slag Gate defeat,0gold banked; restarted through Try again. Left PAUSED in Ember Hall1/6,wave3/3,86HP,level0,XP3/10,0runGold,Staff/SecondWind. No upgrade, equipment purchase, save edit or restoration.

After review profile/backup,both development flags and rotated telemetry remain byte-identical. Profile revision215/32776gold/Staff/SecondWind/deepest3 unchanged. Current telemetry retained full preinstall prefix and appended3919bytes. Current-process log0matches for FATAL EXCEPTION,NullReferenceException,MissingReferenceException,InvalidOperationException. Short install/movement/art smoke only, not sustained performance or exhaustive traversal. Inputs/captures gated on focus,awake,no-keyguard; captures rechecked. Device image copied unchanged to ArtDirection/2026-09-29/foundry-lanterns-01/device-lantern.png. No code/build changes,commit,push. Reinspect fresh before further phone input.