# Integrated ground rewards — 2026-09-22

The Descent now displays each GroundDrops.Drop at its recorded floor position. The existing one-unit pickup rule and immediate experience/gold payment are unchanged. One painted cyan crystal/brass-coin sprite represents a combined reward; a pooled view plays a short collection flight after the authoritative Collecting event. Ground rewards are no longer invisible. Swarm instant rewards and chest effects remain as before; the old Descent kill coin burst is suppressed so it does not compete with the collectible cue.

## Source and import

Original `../reward-proof-01/reward-v1.png` is unchanged (SHA-256 `2556ee74e8f5a0a44524bf4dfa9b9ddc241a08a5b898f21bc7c8ec08320575f5`). Exact built-in image_gen prompt is in the preceding study. No new image generation occurred in this integration.

`Tools/ArtReview/Import-GroundReward.ps1` temporarily stages `BuildGroundRewardAsset.cs` in the Editor assembly. Unity decodes the source and samples its entire canvas uniformly to 96x96; no independent alpha fitting, repainting or trimming. It writes `Assets/_Project/Art/Props/GroundRewards/Reward_ExperienceGold.tga` and preserves existing asset GUIDs on rerun. Import is 128 PPU, bilinear, sRGB, clamp, no mips, non-readable, ASTC4x4 on Android. Calculated compressed pixel payload is 9,216 bytes, excluding texture/sprite/renderer overhead. Pivot is (636/1254,132/1254); view scale is .45/(96/128*835/1254), preserving the proposed .45 visible-unit width. One new CombatSetup sprite reference is the only authored scene change.

## Behaviour and ownership

GroundDropView is attached by CombatSetup after GroundDrops and telemetry exist and before encounter initialization. It observes Dropped, Collecting and Changed; it neither moves the hero nor awards/collects rewards. Value-identical/coincident entries remain ordered and separate. On re-enable it reconciles the current drop list. It creates sixteen pooled roots initially, grows when necessary without overwriting resting rewards, and reuses returned objects. No sprite/texture generation per normal kill or collection, no LINQ/temporary collections in the update loop.

Each root has a SortingGroup at actor/prop order1; a child body preserves the fixed ground pivot. A separate borrowed contact shadow follows the established ground/rim-fitting system. A flight moves the root on the floor for sorting while the body has a small cosmetic hop, shrink and late fade. The duration is .22s when the real hero is within DropRule reach, .42s otherwise; this is distance-based presentation, not a fabricated gameplay sweep flag. It tracks the live hero endpoint. No extra aggregate flash was added, to keep simultaneous collection quiet.

Flights use scaled delta time, freezing during player pause and choice panels. XP/gold are already paid before the animation finishes, including a same-frame level-up; the view never blocks a choice or checkpoint. Changing floors cancels old cosmetic flights; ending a run clears visuals without collecting unpaid drops. Disable unsubscribes/clears; re-enable hydrates; scene destruction drops subscriptions and destroys only owned fallback art. Imported reward and cached contact-shadow assets are borrowed.

The generated combined sprite is used only when both XP and gold are positive. Single-currency exceptions use tiny existing-style procedural XP/coin fallbacks created lazily once per view; zero-value entries draw no symbol. Those fallback designs are provisional. The normal combined master remains the painted path; it is not regenerated on reload.

## Verification and evidence

Four new GroundDropPresentationTests cover real lethal-damage events and immediate once-only collection, 40 identical stacked drops without loss, disable/re-enable reconciliation and pool reuse, a real upgrade choice freezing/resuming flight, death without paying remaining rewards, zero/single-currency presentation, imported texture contract, reload ownership and removal of destroyed subscriptions.

The existing ChestPresentationTests seed is pinned to one without a first-room book. Docs/21 records the prior flaky 85-vs-100 HP assertion: a Vital Surge book could legitimately add HP to the heal. This isolates the original chest heal/opening contract; gameplay/chest randomness is unchanged.

`Tools/ArtReview/Run-GroundRewardProof.ps1` temporarily stages GroundRewardCapture for a graphics-enabled PlayMode run using an isolated TestProfile. The fixture chooses the existing density proof floor/seed42, disables movement/attacks for controlled staging, kills one real enemy through Health, and adds seventeen sample drops through the real GroundDrops model. The normal imported view runs throughout; no sprite or transform substitution is used. Depth stress adds two near-foot drops. Collection calls the actual model methods. Captures cover a real kill, 18 rewards, 1080x2340 and 1080x1920 framing, front/back depth, local flight, sweep paused by a choice, resumed sweep and empty pool. The large lethal hit is fixture damage, not a balance change. These are controlled Unity renders, not a natural run or a phone recording.

Final gate: .NET **380/380**, compile **0 warnings/errors**, EditMode **395/395**, PlayMode **124/124**, targeted presentation classes **6/6**, final graphics capture **1/1**, static integrity **482 project GUIDs / 1099 package GUIDs / 568 scene objects/components**. Full PlayMode includes existing scene/simulation parity. Eight final Unity PNGs are saved here; ground/depth/flight were visually reviewed. The browser gallery's eight selectors/images and 390px layout passed headless Chromium checks.

Android build exited **0**. APK **24,752,438 bytes**, SHA-256 **`9DD2A9A758C95DCB88F91E4204B0207D61E30389787AF674414A0805B19A0F28`**. The build log explicitly records `Rebuilding Data files because Assets/_Project/Scenes/Gameplay/Gameplay.unity is dirty (hash)`: this was not the previous scripts-only build. ZIP inspection found GroundDropView in the IL2CPP metadata and Reward_ExperienceGold in packaged shared assets. ProjectSettings and Packages/packages-lock.json remain unchanged; source master hash is unchanged. No commit/push in this slice.

## Limits and next step

The view has no off-screen reward arrows, pickup sound, persistent reward count or automatic magnetic collection. A small drop can legitimately hide behind a nearer actor, and simultaneous drops at exactly the same position visually overlap. They remain separate collectible values. Density captures are staged, not a performance benchmark. Single-currency fallback artwork can be improved later. Final exit seal and finale HUD are separate slices described in the preceding VISUAL_PLAN.md.

Phone availability was requested but not answered during this slice. `adb devices -l` enumerated S23 RFCW20W2WFX; **no install, launch, input, screen capture, profile/telemetry access or device-state change** was performed. The APK is ready for a coordinated install/review. The old September19 phone/profile state is not assumed current; physical visual acceptance and performance remain unverified.

## Device installation follow-up — 2026-09-22

Owner requested continuation. The connected S23 was dozing behind keyguard, so no launch/input/capture was performed; an unlock request was sent. A fresh backup of the application's external files was saved to ignored `TestResults/reward-device-20260922-233551/before/files`. Installed with `adb install --no-streaming -r`; result Success. Installed base.apk SHA-256 matches the built `9DD2A9A758C95DCB88F91E4204B0207D61E30389787AF674414A0805B19A0F28`.

Fresh profile: revision186, gold26321, default Sword (empty equippedWeaponId), Second Wind, both purchasable weapons/relics owned, deepestFloorCleared3. Primary SHA-256 `40F8FA69CEA9B53ED1D6FB924D70A3D708C7800E2A6B1501C57D9CA1B872D2DE`; backup `E79CC1322A981A2A1B97D0BA295DBBF279374149AC0BA7682EA0B6C99085B00B`. All nine backed-up files were byte-identical after installation, including both profile files and three telemetry files. The existing `development/start-floor.txt.off` is inactive and untouched; no active start-floor file was present. No save restoration, flag change, purchase or loadout change.

This supersedes the not-installed paragraph above. **Installed, but physical visual acceptance still pending:** phone remains locked/dozing and the unlock request is unanswered. Next: fresh awake/unlocked check, launch normal Gameplay, then live reward-drop/pickup/choice-pause review. No game suites repeated for this device-only operation.

## First live reward inspection — 2026-09-22

Owner unlocked the S23. Awake/unlocked launcher state was checked, then Cryptforge was launched with am start. Guarded captures (game focus, awake, keyguard false before and after) show visible ground rewards in normal Cinder Walk2/6. First capture: level0,XP0/10,0run gold,57HP. Important art gap: CinderMite is an authored XP-only enemy (XP1/gold0), so its frequent drops use the provisional pixel crystal. The source plan incorrectly treated single-currency drops as rare. The combined painted master does not solve that common case; make a matching XP-only sprite before treating reward art as visually complete.

By the next interaction the run had advanced to room5/6,level6,XP90/112,51run gold,84/115HP. Those choices/movements were not driven or observed by the agent. Asked whether the owner was playing to avoid mixed inputs; no controlled pickup or timing claim is made. One guarded pause tap left the game paused. Saved original captures in reward-runtime-01/device/first-ground-rewards.png and review-paused.png. Primary profile remained hash40F8FA69CEA9B53ED1D6FB924D70A3D708C7800E2A6B1501C57D9CA1B872D2DE; no restore/loadout/flag change. A clean controlled pickup/flight/choice-pause review remains pending coordination. No performance claim.

## Controlled phone follow-up — 2026-09-22

Owner explicitly handed over the idle phone. Guarded capture confirmed the previous paused room5/6 state. Recorded 25 seconds, resumed through the visible Resume button and performed one downward drag. The XP-only drop disappeared on approach and XP increased90->91 while gold stayed51. Frame samples show its short approach/disappearance, but actor overlap prevents an exact flight-duration assessment. A combined painted cyan/brass drop is clearly visible beside the hero after the remaining room5 enemy dies and room6 begins. The normal boss then died automatically; a level-up panel appeared with gold126, level7, XP121/136 and44/115HP. No upgrade was chosen; the phone is left safely waiting at that panel. Choice frames remain visually stationary; this recording does not isolate a visible in-flight reward behind the panel, so the precise pause/resume flight contract remains supported by Unity tests, not independently proven on-device.

Evidence: device/controlled-pickup.png, device/combined-drop.png, device/controlled-choice.png. Full original recording and sampled contact sheets remain under ignored TestResults/reward-device-20260922-233551/live. Primary profile hash remains40F8FA69CEA9B53ED1D6FB924D70A3D708C7800E2A6B1501C57D9CA1B872D2DE (revision186/gold26321). No save edits/restoration, active development flag, purchase or loadout change. No performance claim. No code changes or repeat build/tests in this device-only follow-up.

Device smoke check now confirms visible drops and proximity collection with payment; reward art remains incomplete because the frequent XP-only fallback still needs matching painted artwork. Next art priority: XP-only crystal, followed by the exit seal. Inspect fresh device state before further inputs.
