# Claude handoff — Project Cryptforge / foundry art, 2026-10-14

Prepared at the owner's request after the current visual package. This is a repository handoff for continuation in Claude; it is not a message sent to Claude or authorization to operate the phone. See the verified closure and open queue below.

## 1. Read this first

Continue the existing game; do not rewrite it. The owner speaks Turkish. Use Turkish for user communication and English for code, documentation and commit messages. Make meaningful, reviewable progress, keep the owner informed, and distinguish what was built, tested, visually inspected and actually checked on a phone. Do not hide failures or call planned features complete.

The immediate priority is reference-led foundry environment quality. The owner has repeatedly prioritized the approved map's shape, major architecture and complete working equipment over tiny ornaments. The current track must preserve gameplay, camera framing, movement/navigation, encounters, balance, energy deferral and saves unless a separate task explicitly changes that scope.

Authoritative reading order:

1. `Docs/01_PRODUCT_VISION.md`, `Docs/03_GAME_DESIGN_DOCUMENT.md`, `Docs/06_TECH_ARCHITECTURE_UNITY.md` for intent and architecture.
2. The **newest dated sections at the end** of `Docs/38_HANDOFF_2026-09-30_ART.md`, then `08_ART_AI_PIPELINE.md`, `15_ROADMAP_BACKLOG.md`, `19_DECISION_LOG_TEMPLATE.md`, `21_DAY_1_3_IMPLEMENTATION_PLAN.md`, `22_FIRST_COMBAT_VERIFICATION.md`, and `23_ANDROID_DEVICE_VALIDATION.md`.
3. Current proof package README, `validation/independent-image-review.md`, `validation/next-visual-priorities.md`, source freeze, image hashes and actual Before/After images.
4. Standing rules in `Docs/25_HANDOFF_2026-09-15.md` sections 1 and 6, while treating its old status paragraphs and counts as historical.

**Historical-document trap:** the beginning of Docs38 still describes September 30, says nothing was committed, lists 17 renderers and proposes textures that have since shipped. The top-level README still describes procedural placeholder art and an old enemy capacity. Neither is the current visual state. Many old unchecked roadmap items were subsequently completed; read dated closure records before reopening them.

## 2. Repository and final verification boundary

Repository `E:\MobileGame`, GitHub `erenulutas0/RPG`, branch **main**. The verified implementation commit is **`0869689a6c8a6834319aee6f90eabab4906e5e66`** and was pushed to `origin/main`; equality was checked before this document was written. This handoff is a following documentation-only commit. Resolve its revision with `git log -1 -- docs/39_HANDOFF_2026-10-14_VISUAL_TO_CLAUDE.md`; the final user response reports the published final HEAD. At finalization there were no tracked changes; unrelated historical untracked ArtDirection evidence was preserved, not globally staged. Do not delete or assume ownership of those files.

**Latest completed package: `ArtDirection/2026-10-14/foundry-inner-piers-01/`.** Fresh Before was sealed at preceding accepted commit `e6f681792a53d9c3e3204291e67d51738f2c1abd` (`foundry-gate-massing-01`). That is the comparison baseline, not the latest implementation.

| Evidence | Final accepted result |
| --- | --- |
| Pure .NET | 438 passed, zero failed/skipped |
| Isolated compile | 0 warnings / 0 errors |
| Full Unity EditMode | 699 passed |
| Full Unity PlayMode | 160 passed |
| Full suite total | **1,297**; focused tests/captures are separate, not counted twice |
| Focused Foundry | 297/297 passed separately |
| Graphics | 21 matched actual Unity views per phase; Android target, identical cameras/actor staging, paused combat, hidden HUD |
| Visual review | Lead and independent **ACCEPT** for bounded inner-pier form/finish; no reference-parity claim |
| Gallery | 43 Before/After/reference controls checked, all images loaded |
| Static integrity | 639 project asset/folder GUIDs; 1,099 package GUIDs; 568 scene objects/components; resolved references and valid JSON |
| Region budget | **36 renderers / 171,884 vertices**, four below preceding baseline; includes unchanged 3,572 distant |
| Ceilings / headroom | 174,000 region / 3,600 distant; headroom 2,116 / 28 |
| Resources | Existing atlas/materials, no new bitmap, batch, shader or updater; outer-bay bake stays 512x1024 ASTC6, 11 mips, non-readable |
| Android build | Exit 0; **36,387,202 bytes** |
| APK SHA-256 | `C6EDF8C6975B336866423D848649FC3CC812F860E88697936A0F324E2A2839FC` |
| Deployment | **NOT INSTALLED**; no phone acceptance, frame-time, save or telemetry claim |

Implementation completed in this package:

- `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateInnerPiers.cs`: complete base/shaft/head family, with higher raked haunch, shorter real recess and one consolidated capital. Original footing, inner returns, door, iron collar contacts and 7.25 terminal seat remain fixed. The -.18 shaft inset intentionally flares back to the old upper face planes.
- `FoundryRegionGatePierFinish.cs`: .11 family-local clean atlas field, preserving source lighting. `AppendGateStone` copies already finished static geometry and offsets its indices.
- `FoundryRegionArchitecture.cs`: finish header, piers, returns and cartouche separately, then preserve their original painter order and existing depth registration. Neighbours keep .075. No changed map, camera, navigation, encounters or balance.
- `Assets/_Project/Tests/EditMode/FoundryGateInnerPierTests.cs`: six near/far cases for actual bearings, fixed collar contact, exposed recesses, source-light/coverage preservation, finish isolation and appended mesh order. Each new Asset has fresh metadata.
- `Tools/ArtReview/FoundryInnerPiersCapture.cs` and `Run-FoundryInnerPiersProof.ps1`: versioned matched capture, gallery and durable validation evidence. Docs08/15/19/21/22/23/38 contain the dated closure.

The lead and three helpers completed geometry, family material integration, tests and independent review in disjoint scopes. The first passing focused run became superseded when the freeze check detected a late test-fixture correction. Its XML and old freeze remain as labelled evidence. Compile, all-Assets freeze, focused tests and final capture were rerun before full suites/build; no stale pass is counted. See `validation/work-record.txt` and `final-workflow.md`.

Final source-freeze SHA-256: `966457d1edc8ddaed78ea52f51dde673f23bce4019924ff636b71062e19fdcbe`. Exact image hashes are in `validation/capture-sha256.json` and independently recorded in `independent-image-inputs.json`. The raw baseline seal is `validation/baseline.json`. Existing package-local `.gitattributes` preserves exact capture line endings; the staged Before file bytes were checked against the seal before committing.

`Builds/Android/Cryptforge-Prototype.apk` and `TestResults/` are ignored and overwritten by later runs. Package records preserve the relevant evidence. This APK hash identifies this completed build only, not whatever appears at that path after future work.

## 3. What the project is, and what exists

Original product intent: a mobile-first portrait incremental auto-battler RPG, one-thumb movement, automatic attacks, meaningful build choices, fast reward cadence and an expandable data/behavior content model. The chosen identity is an arcane guild descending into an ancient foundry. The early skeleton/crypt identity was excluded to avoid copying the reference game's theme. No multiplayer, PvP, gacha, backend, ads or IAP is part of this prototype scope.

Implemented foundations include Vanguard movement and automatic combat, Sword/Staff/Daggers behavior, enemy packs, Forge Burst, XP and upgrade choices, chests, six-room Descent floor templates, forge/elite/boss encounters, Extract/Descend risk, result/restart/pause flows, local banked progression, weapon/relic unlocks, versioned local saves and bounded local telemetry. Painted actor/equipment/UI assets and cached ownership have replaced much of the original procedural placeholder presentation. The final swarm, clock-driven ladder, upgrade sittings, walked exit and visible XP/gold ground pickups also already exist (`SwarmDirector`, `SwarmRule`, `GroundDrops`, `GroundDropView`, `ExitSealView`; Docs21 S6c/S6d and later reward closures). The live pickup radius is 1.0; ground drops apply outside the swarm, while swarm kills award XP/gold immediately. These facts do not imply a finished roster, final animation, final balance or complete region gameplay.

The foundry is an explorable **development region preview** (`development/region-preview.txt = foundry` in Editor/development builds). It uses the existing combat/progression systems; the impressive gate, crane, boilers and workyard do not by themselves implement local encounter sites, a functional production system, gate travel or separate boss-hall progression. Regional encounter/boss progression remains deferred under the art-first direction. Do not resume an old proposed Opus/Claude route task just because a historic handoff mentions it.

`HeroStamina.CurrentRulesEnabled` currently returns `false`. Energy spending/slowdown, MOVE HUD and energy offers were deliberately deferred on September 23. Explicit historical stamina experiments remain in code/tests; their existence is not permission to enable them.

Technical stack is pinned: **Unity 6000.0.65f1**, C#, built-in renderer, legacy uGUI `Text`, legacy `Input`. Editor executable: `E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe`. Do not change engine/render pipeline/input/UI stack or introduce packages as incidental cleanup. Preserve `Packages/packages-lock.json`, ProjectSettings and existing `.meta` GUIDs. New Assets require fresh metadata.

Current assemblies include `Project.Gameplay`, `Project.Editor`, `Project.EditModeTests`, `Project.PlayModeTests` and `Project.TestSupport`; do not mistake the architecture doc's proposed assembly list for a migration instruction. Pure logic is exercised by `Tools/CombatChecks`; Unity lifecycle/rendering and loaded asset contracts require real Unity tests. ScriptableObjects are definitions and asset-owned materials, not mutable profile state. Scene-generated meshes/material variants need explicit ownership and unload cleanup; borrowed textures/material assets must survive reload.

## 4. Approved target, progress and honest quality gap

Approved visual/layout reference: **`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`**. It is concept art, not a runtime screenshot or a mechanically exact blueprint. The owner later prioritized its actual broad composition over the earlier diagram. Fixed camera and walking scale are part of the comparison contract; zooming a new image to make the map look richer is not an improvement.

The implemented layout has the broad court, large furnace pit to the left, south arrival bridge, northeast gate/raised terrace, peripheral industrial furnaces, western crane/working equipment, fortress undersides, chains, lanterns and ceremonial brass. Real layout work expanded and redistributed traversable space, connected bridge/court geometry, preserved voids and moved complete assemblies with their collision/depth/light anchors. It was not achieved only by adding decorative objects.

Current `FoundryMap` design-space anchors at the accepted baseline:

- Court: `West=-32`, `South=-18`, `North=18`, `East=14`, with diagonal clipped corners.
- Pit: `[-16,-4] x [2,14]`; protected hole remains.
- Gate offset `-4`; landing U 24..28; causeway and gate move together.
- Bridge: U+V -66..-22, U-V within ±5. Render seam clipping and navigation overlap have different purposes; do not remove robust overlap to hide a visual seam.
- Two raised work islands have shared navigation-blocked footprints in `FoundryMap`; their paving still exists beneath them. Flush freight rails/plates are crossable scenery. A new raised prop cannot silently occupy the walking field without matching navigation.

Recent accepted sequence (see each package's README and actual images):

| Package, all under `ArtDirection/2026-10-14/` | What was actually gained |
| --- | --- |
| `foundry-gate-depth-01` | Native Blender outer-bay bake with real source niches, two runtime cards; pitched native header shoulders; explicit pipe/card painter integration. The first overpaint and pale-bay candidates were rejected and repaired. |
| `foundry-gate-cohesion-01` | Fitted native inner jambs without duplicate backing, real curtain bays/returns, pitched coping, clean continuous gate-local mineral mapping. A dark atlas drip candidate was rejected. |
| `foundry-scene-cohesion-01` | Localized floor heat separate from wall/iron radii, finer paving grain/patina, supported billets/open flasks, cleaner exposed fortress/bridge surfaces. Storage improvement was modest and partly occluded. |
| `foundry-material-family-01` | V8 paving replaces long mineral streaks at preserved mean value; a complete native moulding station gains oak flasks, true sand cavities, raked supports and seated clamp screws; broad near-island bearings gain restrained value hierarchy. Recovered 395 vertices. |
| `foundry-gate-massing-01` | Active outer bays gain higher raked haunches, shorter/deeper blind recesses and wider cheeks. Curtain side construction becomes simpler, with a small exposed improvement and 452 fewer finished vertices. |
| `foundry-inner-piers-01` | Complete native inner piers gain substantial lower haunches, shorter open shafts and single capitals; a family-local .11 field refines grain while neighbours retain .075. Complete finished region recovers four vertices. The remaining native/baked material gap is explicitly retained. |

The reference is still not matched. Native stone is regular and often more sharply grained than the smoother baked equipment. The accepted inner-pier construction is stronger now, but crown transitions and workyard equipment families remain simpler than the reference. The workyard still has less authored grouping and variety; the large quiet floor serves gameplay but does not recreate all concept detail. Some architecture is largely occluded at the real camera, so source complexity alone is a poor target.

The gate-massing review accepted the outer bays as the main visual gain. It explicitly did **not** accept a claim of a second prominent curtain upgrade: the far field is almost entirely hidden by native inner piers in gate closeups. The near side has a small coherent visible field in full-map/overview. Source alpha-only visibility estimates missed the native foreground occlusion. Do not spend the next budget on more hidden curtain detailing.

`approach-20-1` crops the crown and most upper gate architecture. Use gate-detail/crown and full-map for complete gate form; use approach-20-1 for lower contact, character, navigation and portal regression. The complete moulding station is most legible in `yard-detail` and `approach--16-15`; its full-map gain is necessarily smaller.

## 5. Runtime art map and source pitfalls

Most environment implementation is a partial `FoundryRegionView` under `Assets/_Project/Scripts/UI/Arena/`. It builds grouped static meshes, sometimes substitutes authored cards, and keeps floor/depth/actor presentation tied to shared geometry.

| Area | Main source and contracts |
| --- | --- |
| Map / movement | `Scripts/Art/FoundryMap.cs` is shared by floor, clearance, spawning and pursuit. Do not independently reshape visual boundaries. |
| Raised projection | `FoundrySurface.cs`: X=(U-V)/sqrt(2), Y=(U+V)/(2sqrt(2))+Height(U). Ramp U25..27 reaches 1.25. Use its projection/inverse; a flat inverse on raised/elevated points drifts. |
| Floor | `FoundryRegionView`, `FoundryRegionFinish`, paving/contour helpers. Preserve exact single-owner flag/bridge/court replacement, continuous UVs, protected holes, floor-only light and quiet combat contrast. `PavingGrainSpan=4.3` at this baseline. |
| Active paving material | `Art/Props/Foundry/MAT_FoundryPaving.mat` through `Art/Environment/Platform/PlatformMaterials.asset` / `PlatformMaterialSet.FoundryPaving`. Current texture is `FoundryPavingV8.png`, loaded 1024² ASTC6, 11 mips, non-readable. V7 remains history. |
| Gate portal / inner piers | `FoundryRegionView.BuildGate`, `FoundryRegionArchitecture.BuildGateArchitecture`, `FoundryRegionGateRecesses`, `GateHeader`, `GateCornice`, `GateSurface`. `FoundryRegionGateInnerPiers.cs` owns `BuildGateInnerPier`; `FoundryRegionGatePierFinish.cs` owns `GateInnerPierGrain` (.11) and `AppendGateStone`. Preserve exact door opening, inner returns, 7.25 inner crown envelope, lower/fire/iron contacts and actor depth. |
| Active outer gate bays | `FoundryRegionGateEnclosure` calls `BuildBakedGateButtress`; `FoundryRegionGateMasonry.cs` loads `Resources/FoundryGateMasonry.png`. **`BuildGateOuterButtress` / `FoundryRegionGateButtress` is fallback-only while the texture loads.** Editing it alone has no production visual effect. |
| Curtain | `FoundryRegionGateCurtain.cs`: real front bays and separate upper side field, returned back faces and pitched coping. No full old wall under a recess. Native pier occlusion must be judged in actual scene images. |
| Workyard | `FoundryRegionProductionYard.cs` owns active native trolley/moulding islands. `FoundryRegionMouldingBench.cs` owns useful cavities/support/frame details; `WorkyardStock` owns added supplies. The adjacent `FoundryRegionCastingBench` prefers `BuildBakedCastingBench()` and returns before its native fallback. Do not confuse moulding and casting stations. |
| Baked equipment | `BakedBoiler`, `BakedCrane`, `BakedStockStation`, `BakedStockHoist`, `Reservoir`, `Salvage`, `CastingBench` load their named Resources assets. Trace the actual call branch before editing a native fallback; some foundations/housings are shared active geometry. |
| Lower stone / distant works | Fortress/Undercroft/bridge helpers and `FoundryRegionDistantWorks`. Keep recessed returns, bridge openings, shaft/abyss voids and chain anchors protected; distant cost has a separate ceiling. |
| Visibility | `FoundryRegionDepth`, `FoundryVisibilityView`, `GroundedSorting`, `FoundryCutaway.shader`, `FoundryActorXray.shader`. Painter rank still matters; the color pass does not gain reliable ordering from geometric z alone. Preserve actor apply/restore and scene-owned material lifetime. |

Concrete traps worth retaining:

- The active gate bake uses two **unmirrored four-vertex cards**, the original 5 x 10 projected field, physical footprints and depth owners, and 0.62 RGB gain. Reserved half-ranks put the cards after their curtains. The south feed body/collars/elbow must stay strictly in front of the near opaque card. Equal-ranked separate materials previously overpainted niches.
- The gate-massing source is `masonry-bay/gate-masonry.blend` plus `Tools/ArtReview/BuildBlenderGateButtressMassing.py`, procedural stone, projection/contact JSON and direct source PNG. Source image is 1024 x 2048; runtime stays 512 x 1024 ASTC6/11 mips. The internal shoulder rose 2.85→3.65; external bounds/crown contacts stayed fixed, but the local opaque contour changed. “Same silhouette pixels” would be false.
- A Blender source render is fixed-view baked lighting, not realtime volumetric geometry. Preserve bake/Unity projection; Unity supplies the 1.25 landing rise once. Do not bake it twice.
- `GateHouseGrain` tessellates at construction time. Count **final emitted geometry after finish**, not source quad counts. The clean gate atlas patch is U .04.. .21, V .44.. .564. Shared header/returns/cartouche use .075; the inner-pier family now uses .11; the earlier wider patch exposed a dark drip. Shared finish edits can change unrelated faces and budgets.
- `BuildGateArchitecture` finishes separate builders and appends header / pier / return / pier / return / cartouche in original painter order. Do not finish the combined builder again: that would overwrite the .11 family field and alter final subdivision. The shaft inset of -.18 is an intentional flare returning to the old upper planes, not an accidental negative width. The inherited iron collar remains at 5.43; the head bearing remains at 7.25.
- Historical builders are not automatically safe to rerun. For example, `Tools/ArtReview/BuildFoundryPavingAsset.cs` still copies the old V2 source and assigns material gain .8. Running it blindly would regress the current V8 binding/calibration. Inspect inputs, outputs and current imported asset GUID bindings first.
- Preserve neutral V8 mean-value calibration; increasing brightness to make a texture look “better” can damage the floor/actor relationship. The accepted material-family source comparison and actual images document the decision.
- Test failures can be real defects or wrong oracles. Previous floor ownership failure came from a degenerate, out-of-bounds triangle; the oracle now rejects zero area and bounding misses before strict crosses. A fortress color oracle needed the **last visible** triangle, not the first hidden mortar. Keep strict tolerances and retained failed evidence; never skip or loosen a test to secure green output.
- Check apparent overpaint with exact original-image pixels before changing runtime. An earlier keeper-overpaint suspicion was disproved; the reviewer withdrew it rather than inventing a fix.

## 6. Budgets, workflow and review gates

Accepted gate-massing baseline is **36 region renderers / 171,888 region vertices**. The **3,572 distant vertices are included in that region total**, not added again. Fixed ceilings are **174,000 region / 3,600 distant**; remaining headroom is **2,112 / 28**. These are ceilings, not a target to consume. Current accepted inner-pier result is **36 / 171,884**, including the same 3,572 distant; headroom is **2,116 / 28**. The four-vertex reduction is not a meaningful performance claim.

The gate texture remains 512 x 1024 ASTC6, 11 mips, non-readable; loaded Editor texture memory 630,880 bytes, two inherited card batches and one shared cutaway variant. Texture payload, Editor allocation, APK size, draw calls and phone frame time are different measures. Fewer vertices do not prove faster rendering. No current midrange-device result exists.

Use bounded complete families with visible camera impact, preferably replacing/simplifying existing work. For raster creation/editing, use the available built-in image-generation tool and retain untouched output/prompt/source evidence. For the recent gate bake, edit native Blender geometry/materials and render again; do not secretly paint a raster to fake source construction. If the next tool environment lacks a required capability, disclose that and keep a concrete reviewable proposal rather than claiming it ran.

Validation sequence for a source change:

1. Read the latest records and inspect actual reference/Before images. Choose the production-active scope; do not spend time polishing fallback-only or invisible surfaces.
2. Seal fresh matched Before at the true baseline before runtime edits. Capture scripts refuse a wrong HEAD/dirty Assets for this reason. Never overwrite accepted Before with the candidate.
3. If using helpers, assign whole disjoint files. Only the lead runs Unity, captures and eventual device operations. Isolate helper compile output directories. Freeze Assets before a Unity run and do not edit them during capture/tests/build.
4. Run pure tests, isolated compile, then meaningful focused geometry/import/lifetime checks. Preserve failed XML/source and diagnose before continuing. Changes to test expectations must follow a justified production contract, not weakened ownership/tolerance assertions.
5. Take the complete actual Android-target Unity After set with identical cameras/staging. Inspect full-map, relevant native detail and gameplay portrait views against both Before and concept. Record explicit bounded ACCEPT/REJECT. Test pass alone does not approve art.
6. With the final source frozen, run complete EditMode and PlayMode, build only after both pass, run static integrity, verify the source freeze again, and record build hash/size. The freeze includes test sources and metadata as well as runtime files. If any frozen Asset changes after a capture or suite, the matching freeze claim is invalid and the affected evidence must be regenerated. This package retains a superseded first passing focused run after a test-fixture correction; it was not carried forward as final acceptance.
7. Retain package README, source/projection/prompt data, image hashes, test evidence, rejected candidates and browser gallery. Gallery image loads are asynchronous; recheck after completion. Update Docs08/15/19/21/22/23/38 with exact scope and limits. Commit/push only per the current owner's/session authorization and actual final state; do not reset unrelated work.

Commands from repository root, with Unity closed. The capture command below is for reproducing this package only: it writes its fixed After directory. For new work, create a new versioned package/capture launcher pinned to the new true baseline; never overwrite accepted Before/After evidence with a later candidate.

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/Run-UnityTests.ps1 -Platform EditMode -Filter Foundry -SkipApk
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryInnerPiersProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

To view the evidence, open the package index.html or use the existing local gallery at http://127.0.0.1:8342/2026-10-14/foundry-inner-piers-01/. If the server is absent, serve ArtDirection locally on 127.0.0.1 port 8342; do not publish it externally.

The capture launcher temporarily adds/removes a PlayMode fixture under Assets and runs with graphics and Android target. Normal batch tests use `-nographics`; visual capture must not. One Unity process at a time; inspect process/lock ownership instead of deleting an active `Temp/UnityLockfile`. Do not pipe the Unity runner through `Tee-Object`. Unity exit 198 is a license problem, not a passed test. The isolated compile check and static integrity do not prove import/render/runtime behavior.

## 7. What is still open

1. **Re-establish the verified baseline before new edits.** The inner-pier package is closed by the evidence in section 2. Read its actual images and independent review; retain the exact portal/crown/depth/material contracts and create fresh Before evidence for the next candidate. Do not reopen already completed gate work from historical unchecked roadmap entries.
2. **Reference-level material and equipment integration.** The independent final review recommends the active trolley and its complete load/frame/wheel assembly first; the moulding bench already gained cavities/support in the previous package. Match neighboring authored equipment while retaining supported loads and open understructure. A second bounded study should address the broad exposed crown/header material mismatch; more hidden curtain detail is not a priority. Do not fill walkways or add hidden fasteners. Current suggestions are in inner-piers `validation/next-visual-priorities.md`; inspect its actual images before choosing the next family.
3. **Phone visual acceptance and performance.** Motion, HUD overlap, shimmer, cutaway readability in a crowded real run, cold first-ramp hitch and midrange frame time remain unverified for current packages. Old S23 evidence is a historical flagship measurement, not acceptance of this much larger current scene.
4. **Known crowd congestion.** `ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md` retains a stationary immortal ten-enemy failure: at hero (-10,-15), three enemies can queue near (-23,-23), 15.12–16.04 units away. The bounds-only A/B also demonstrates older queues. The walking-hero integration passes swept floor and separation but does not prove universal convergence. Do not count the deliberately failed diagnostics as passing acceptance. Fixing pursuit is a separate gameplay slice.
5. **Regional progression and wider product work.** Local encounter sites, actual gate/boss travel, broader hero/content/behavior synergies, final animation/crowd readability, balance decisions and player validation remain roadmap work, not completed by environment art. Consult current decisions before resuming them. No backend/ads/IAP scope is implied.

## 8. Phone boundary — do not erase it during handoff

**The restart/install decision remains pending. This document does not authorize installation, force-stop, input, save changes or diagnostic flag changes.** No phone operation was made in the recent packages. The last dated active-run report (Room 2, 74 HP, 8 unbanked gold) is old and must not be treated as the current screen. The last recorded installed workshop package is likewise historical; inspect current state only as permitted by the next task.

If the owner later authorizes deployment, first perform a fresh read-only preflight and preserve the **current original** profile, backup, development flags and telemetry. Do not restore historic snapshots over newer progress. Before every permitted tap/swipe/screenshot, and after a capture, verify game focus, awake display and no keyguard. Stop if focus changes or USB disconnects. Explain that installing over an open run ends it and loses unbanked progress; resolve the concrete run decision before doing that. Launch with the explicit Unity activity, not `monkey`. Preserve telemetry, remove only newly introduced diagnostic flags, verify installed APK hash and current profile/backup hashes, and record what was actually seen. These are requirements for a later authorized operation, not permission to operate now.

## 9. Concise copy/paste prompt for Claude

```text
Continue Project Cryptforge at E:\MobileGame. First read docs/39_HANDOFF_2026-10-14_VISUAL_TO_CLAUDE.md in full, then the primary Docs01/03/06, newest dated ends of Docs08/15/19/21/22/23/38, and ArtDirection/2026-10-14/foundry-inner-piers-01 README/reviews and actual Before/After images. Speak Turkish to me; write code/docs in English.

The latest visual package closes the complete native inner-pier shape and family-local stone finish. Current region is 36 renderers / 171884 vertices, including 3572 distant, with unchanged ceilings 174000/3600. The reference remains ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png; fidelity is unfinished. Follow the handoff's verified results and open queue, not historical status at the beginning of old docs.

Keep Unity 6000.0.65f1, built-in renderer, legacy uGUI/Input, gameplay/camera/navigation/saves and energy deferral unchanged for this art task. Confirm the active production path before editing: several native helpers are fallback-only. First inspect the full map and real portrait views, then complete one substantial visible equipment/material family from the latest priorities. Preserve useful cavities, load contacts, walkways and cutaway/depth ownership. Use disjoint file ownership for helpers and a single Unity/test/device owner. Take a fresh versioned baseline, freeze all Assets including tests/metas, validate meaningful focused tests, matched native Unity captures, full suites and an APK hash. Never overwrite an accepted proof with a later candidate. Report real visual gains and remaining gaps; do not infer phone performance from screenshots or vertex counts.

The phone restart/install decision remains pending; do not install, force-stop, input or change device data until it is resolved separately. Preserve current saves/telemetry and unrelated repository work. Read the known crowd-congestion limitation; do not silently include gameplay changes in the art package. Continue with the next visible family after baseline review and keep the owner informed.
```
