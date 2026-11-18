# 58 - Monetisation for the first release: model B (decision, 2026-11-18)

The owner chose this model on 2026-11-18, after researching nine games of the genre and the Turkish market. The report is
`docs/research/2026-11-18_monetisation_and_arena_owner_report.md` (in Turkish). This document narrows `docs/11` (the
earlier, broader strategy) to the first release; where they differ, this one holds.

## 1. The model

- **Free to download.**
- **One rewarded video, offered in one place: the result screen**, after the run is saved. It is optional and pays a
  limited bonus: the run's gold **x1.5**, a starting value that is measured before it ships (section 4). A "No" goes
  straight on, and nothing nags.
- **One product: "Skip reward videos" (non-consumable, about 99.99 TL).** The buyer gets exactly the same bonus, as
  often, without the video. It is the only thing the store sells in the first release.
- **Never sold:** gold, floor materials, boosters (Forge Seal, Master's Hammer, Efsun Scroll, Binding Rune), efsun,
  items, basma chances, retries, or protection. Everything that makes power is earned by playing.
- **Not in the first release:** interstitials, a revive video, currency packs, starter packs, a battle pass, gacha or
  any paid random item, and subscriptions.

Why: a free start reaches the first players. The first month then measures whether the run, the basma and the efsun are
loved, without the balance of a paid currency. The arena's fairness never meets bought power (docs/55 section 9). No
server-owned inventory is needed.

## 2. What the game already does right (the report's warnings, checked against docs/54)

- **A bad-luck streak is bounded.** Each failure adds pity, and a hard ceiling makes the next strike hold: +9 holds after
  10 failures in a row, a mean of 3.96 attempts. The report's "a guarantee for everyone" is docs/54's ceiling.
- **The chance on the button is the chance used**, built up underneath (base + the anvil's memory + a Seal). The memory
  bar shows how many strikes until the next one holds.
- **Efsun keeps the old lines or the new ones**, at the player's choice, so a reroll can only help.
- **The boosters drop in play only**, and stay so: a paid chance booster is the report's highest-risk item.

## 3. Requirements on the build

1. **Save before any video.** The run's result and rewards are written to the profile before a video opens. A crash,
   a call or the app being killed during the video costs at most the bonus, never the run. (A store complaint about
   Vampire Survivors' revive video is exactly this.)
2. **The bonus is credited once.** A completed-reward callback credits the bonus to the saved result, idempotently
   (keyed by the run id). A failed or skipped video credits nothing and says so plainly.
3. **The entitlement.** On start, Play Billing queries the owned products and unlocks the skip. The purchase is
   acknowledged after it completes (Google refunds unacknowledged purchases after three days). The entitlement is
   cached locally for offline play, apart from the profile's save. Restoring a purchase is separate from restoring a save.
4. **No video before the player understands the game:** the offer appears from the second completed run.
5. **Abstractions first, SDKs later.** `IRewardedVideo` and `IStoreEntitlements` interfaces, an offline fake for the
   editor, tests and the capture tools. The ad network and Unity IAP packages come in when the owner has opened the
   AdMob (or chosen network) and Play Console accounts. That is a Packages change, made then, at the owner's request.
6. **Telemetry:**
   - `reward_offer` (run id, bonus)
   - `reward_result` (watched / skipped / failed / skip-owned, credited)
   - `store_purchase` (product, result)
   - and, from the existing events, the next run's start after a video

## 4. What the first month measures (the owner's report, section "Son tavsiye")

1. **Runs to +6 and +9 for a player who never watches.** The free player must progress at a natural pace.
   `gear_economy.py` models it with and without the x1.5 bonus before release, on the tiers' payouts.
2. **The return rate:** the share of videos followed by a new run.
3. **The purchase rate** of the skip.

If the free player progresses and returns, the next thing sold is content: a new region or a gameplay expansion. Power
is never sold.

## 5. Owner's tasks (accounts, money and law are the owner's)

- A Google Play Console account. A new personal account must run a closed test with testers before production access;
  the current numbers are in the owner's second research.
- An ad network account (AdMob or the chosen one), and its payment and tax details.
- A privacy policy page, the Play data-safety form and the ad SDK's consent (UMP) setup. The KVKK notice is needed even
  without our own server, because the ad SDK processes data.
- The tax side of receiving store and ad income in Turkey (see the second research brief).

## 5a. The network and the counters (2026-11-18, from the owner's second report; docs/59)

- **AdMob alone at first** (one rewarded unit, Google's UMP; ads requested only when `CanRequestAds()`), mediation
  only after our own Turkish numbers argue for it. AdMob pays from 200 TL (TRY reporting), around the 21st.
- **Counters kept apart** (the report's section 3): requests, matches (match rate = matched / eligible requests), shows
  (show rate = shows / matches), rewards, and confirmed revenue; and the player-side availability = voluntary watch
  attempts with an ad ready / all voluntary watch attempts. A player who says "No" with an ad ready is not missing demand.
  Owners of the skip product are left out of the fill counts.
- **The offer needs a ready ad.** With none ready, the button says so plainly and the run's normal reward stands; the
  run always completes without ads (and offline).
- There is **no verified Turkish rewarded-video eCPM**; the first report's 50 TL was an example. The first 2-4 weeks
  measure it.

## 6. Order

1. The abstractions, the result-screen offer and the save-before-video flow, behind an offline fake (now; no packages).
   Built as a scaffold: section 7.
2. `gear_economy.py` with the tiers' payouts and the x1.5 bonus.
3. SDK integration, when the owner's accounts exist.
4. The closed test on Play, then the Turkish release.

## 7. Built (scaffold), 2026-11-19

Step 1 of section 6 and docs/59 section 3 items 2 and 3, as **pure code behind interfaces with offline fakes**: no SDK,
no package, no view, no scene, no asset. Nothing existing was edited but `Tools/CombatChecks/CombatChecks.csproj`
(the new files' lines). RunResultView, RunHud, MapChoiceView, RunTelemetry, RunTelemetryContext, the profile and every
profile save file are untouched (the difficulty tiers rewrite them); the wiring pass (7.4) comes after the tiers merge.
Nothing of the simulation was touched, so the default batch is unmoved by construction.

### 7.1 What is built

| Where | What |
|---|---|
| `Scripts/Monetisation/IRewardedVideo.cs` | `IRewardedVideo` (`IsReady`, `Load(onLoaded)`, `Show(onResult)`) and `RewardedVideoResult` (Completed, Skipped, Failed) |
| `Scripts/Monetisation/IStoreEntitlements.cs` | `IStoreEntitlements` (`QueryOwned`, `Purchase`, `Acknowledge`), `StoreQueryResult`, `OwnedProduct`, `StorePurchaseResult` (Purchased, Pending, Cancelled, Failed, AlreadyOwned), `StoreProducts.SkipRewardVideos` = `"skip_reward_videos"` (**a placeholder**: the real id is made in the Play Console and can never change after the first upload) |
| `Scripts/Monetisation/IAdConsent.cs` | `IAdConsent` (`CanRequestAds`, `Gather(onCompleted)`), UMP's shape |
| `Scripts/Monetisation/Fake*.cs` | `FakeRewardedVideo`, `FakeStoreEntitlements`, `FakeAdConsent`: scriptable for the Editor, the tests and the captures - queued outcomes (`ScriptLoads`, `ScriptShows`, `ScriptPurchases`), defaults, `Deferred` callbacks completed by hand (a video kept on screen, or never finished: the app killed), `ReportTwice` (a misbehaving SDK), `Online = false`, `SetOwned` and `Revoke` (a purchase elsewhere, a refund), call counts |
| `Scripts/Monetisation/RewardOfferRule.cs` | the rule, pure: `BonusGold`, `Evaluate` to a `RewardOfferState`, `IsOffered`, `IsShown` |
| `Scripts/Monetisation/RewardOfferCoordinator.cs` | `RewardOfferCoordinator` and its `RewardOfferContext`: the result screen's offer from open to credit (7.2) |
| `Scripts/Monetisation/RewardLedger.cs`, `RewardLedgerStore.cs` | the ledger (completed runs, credited run ids) and its file, `rewards.json` |
| `Scripts/Monetisation/EntitlementCache.cs`, `EntitlementService.cs` | the skip's cache, `entitlements.json`, and the service over the store |
| `Scripts/Monetisation/AdCounters.cs` | section 5a's counters for one launch |
| `Scripts/Monetisation/MonetisationTelemetry.cs` | the field builders (7.3) and `RewardResultKind` |
| `Scripts/Monetisation/FlatJson.cs`, `SmallSaveFile.cs` (internal) | the two small files' JSON (ArenaSaveJson's strict reader, forgiving fields) and their write (ArenaSaveStore's temp, backup, newest revision) |
| `Scripts/Analytics/IAnalytics.cs`, `LocalFileAnalytics.cs`, `AnalyticsFanOut.cs` | the analytics seam (docs/59 3.3), 7.5 |
| `Scripts/Analytics/SessionClock.cs` | the play sessions: a background trip to a video is not a new session (7.5) |
| `Tests/EditMode/RewardOfferTests.cs`, `RewardLedgerTests.cs`, `EntitlementTests.cs`, `AnalyticsSeamTests.cs`, `SessionClockTests.cs` | 33 tests, in Unity's EditMode suite and in every plain `dotnet test` of `Tools/CombatChecks` |

All of it is in `Project.Gameplay` (namespace `Cryptforge.Monetisation`, and `Cryptforge.Analytics` for the seam) with
no `UnityEngine`: `Tools/CombatChecks`, which has no Unity, compiles every file. The fakes ship in the game assembly so
the captures and a development build can use them until the SDKs come.

### 7.2 Decisions

- **The bonus: the run's banked gold x1.5, rounded up to whole gold.** `BonusGold(gold) = ceil(gold x 50 / 100)`, in
  integers only (`(gold x 50 + 99) / 100`, in 64 bits): 15 banked gives +8 (23 in all, where 22.5 is exact), 1 gives +1,
  0 gives 0. Rounding up means the total is never below what "x1.5" promises. The multiplier is a percentage
  (`RewardOfferRule.MultiplierPercent = 150`, a context field) so section 4's measurement can move it without a float.
  "The run's gold" is `RunState.GoldBanked`, what the run put in the profile after a defeat's loss; the gold an item
  melted for in a full bag is not the run's gold and is not multiplied.
- **A completed run is one that reached its result screen** (a victory, an extraction or a defeat; a run left by a
  restart or a closed app is not). The count lives in the ledger, not the profile, so the profile and its save version
  stay untouched: a new ledger's first screen is `TooEarly`, its second offers. A lost ledger restarts the count, which
  costs one offer. Opening the same run id again counts nothing.
- **The states**, in the rule's order: `TooEarly` (fewer than 2), `NotSaved`, `NoGold` (a run that banked nothing: x1.5
  adds nothing), `Resolved` (credited, its video ended, or declined), then `Skip` for an owner (**even with a video ready**:
  they paid not to watch), `Video` with one ready, else `NoAd`. `Showing` while the video is up, `None` with no screen.
- **`NoAd` is shown, plainly, and can be tapped**: "no video right now", the run's reward standing. A tap asks for a
  video (if consent allows) and counts as one voluntary attempt without a video ready - once per screen, so tapping
  again while nothing comes is not more demand. When a video arrives the button becomes `Video` and a tap is an attempt
  with one ready. That is what makes 5a's availability measurable.
- **Save before the video**, enforced: the view passes a `resultSaved` check; `Video` and `Skip` exist only while it is
  true, and `Take` reads the state at the tap, the last moment before the app leaves. The offer's telemetry is flushed
  before the video opens.
- **Credited once per run id, ledger first.** The ledger records the credit and is written, then the gold is credited
  (`creditGold`, the scene's `PlayerProfile.Deposit`, whose `Changed` saves the profile). An app killed between the two
  loses the bonus and never doubles it; one killed during the video loses only the bonus (the run was saved). A second
  report of the same video is ignored, and the ledger refuses a run id it holds. A ledger that cannot be written
  (`LastPersistError`) does not stop the credit: the player watched, and the ledger in memory still refuses a second.
- **One show per run.** A skipped or failed video ends the offer (`Resolved`, `LastResult` says which, for the view's
  plain words). No retry and no second offer: nothing nags (section 1). Leaving an open offer ends it as `declined`;
  leaving after an unanswered ask as `no_ad`.
- **The ledger keeps the 32 newest credited run ids** (`MaxCreditedRunIds`) and lifetime totals. A run id is the
  telemetry's (the launch's GUID and an ordinal), never offered again after its screen, so the window only has to
  outlast a double report.
- **The coordinator lives for the launch** (its counters are the launch's); the credit target is passed by each
  screen's `Open`, because CombatSetup reads the profile again with every scene. A video still up when another screen
  opens credits its own run's profile; no second video is asked for while one is up.
- **Requests**: `Prefetch` asks only with `CanRequestAds`, never for an owner, and not while a video is ready, loading or
  showing. A screen that could offer but has none asks as it opens; a finished video asks for the next one.
- **The entitlement**: the cache answers from the first frame, offline. A store that answers is the truth: its list
  replaces the cache (a refund or another account takes the skip away) and every unacknowledged purchase is acknowledged;
  a store that cannot answer leaves the cache. A purchase grants on Purchased and AlreadyOwned (a restore), is written,
  then acknowledged; a failed acknowledgement is retried by the next start's query (Google refunds after three days). A
  Pending purchase grants nothing until a later query sees it complete. A rooted phone could edit the cache; accepted,
  since the skip sells no power and there is no server (section 1).
- **The files**: `rewards.json` and `entitlements.json` beside `profile.json` and `arena.json` (the wiring passes
  `ProfileLocation.Resolve()`), each with its `.tmp` and `.bak`, UTF-8 without a byte-order mark, every number and the
  date (`yyyy-MM-ddTHH:mm:ssZ`, to the second) through `CultureInfo.InvariantCulture` - the same bytes under tr-TR and
  under a culture with another calendar, tested. A newer version is refused, never guessed at; the profile never reads
  them and its recovery never touches them.
- **The return rate** (section 4.2) needs no new field: a `reward_result` with `watched` followed by a `run_start` in
  the same session is a return.

### 7.3 The telemetry (data only; the coordinator and the service emit through `IAnalytics`)

| Event | Fields, in order | When |
|---|---|---|
| `reward_offer` | `offer` (`video` / `skip`), `run_gold`, `bonus_gold`, `multiplier_pct`, `completed_runs` | once per run, the first time the offer can be taken; under the run's id (`EmitForRun`), since the run's own events are over by the result screen |
| `reward_result` | `result` (`watched`, `skipped`, `failed`, `skip_owned`, `declined`, `no_ad`), `credited`, `bonus_gold` (credited, else 0) | once per run whose offer was offered or asked for; under the run's id |
| `store_purchase` | `product`, `result` (`purchased`, `pending`, `cancelled`, `failed`, `already_owned`) | each purchase's end |
| `ad_counters` | `requests`, `matches`, `shows`, `show_failures`, `rewards`, `attempts`, `attempts_ready` | with each `reward_result`, outside any run: the launch's cumulative counts, so a session's last line holds its totals and a lost line costs only its moment |

Match rate = `matches / requests`, show rate = `shows / matches`, availability = `attempts_ready / attempts` (the
reader's; `AdCounters` has them for tests). A request is eligible by construction (none without consent); owners of
the skip make no request and no attempt, so they are outside every count. Confirmed revenue is AdMob's report.

### 7.4 The wiring pass (after the tiers merge)

**A launch-long holder** (new, Unity side, beside `TelemetryRuntime` and in its shape: statics reset in
`SubsystemRegistration`): `MonetisationRuntime` holding, for the launch, the `EntitlementService` (over a
`FakeStoreEntitlements` until Unity IAP, the cache at `ProfileLocation.Resolve()`, `Refresh()` at boot), the
`RewardLedgerStore` and its ledger, a `SessionClock` (started with the telemetry session), and the
`RewardOfferCoordinator` with `Video = FakeRewardedVideo` and `Consent = FakeAdConsent(true)` until AdMob and UMP,
`OwnsSkip = () => service.OwnsSkip`, `SaveLedger = store.Save`, `Analytics = new LocalFileAnalytics(session)`,
`Sessions = clock`. At boot: `consent.Gather(() => offer.Prefetch())`.

**CombatSetup** (not on the tiers' list, but its run end is): a `ResultSaved` flag. Subscribe `Run.Ended` right after
`Bank = new RunBank(...)` (so it runs after the bank's deposit and before any view) with a handler that saves the
profile once more and sets `ResultSaved` only when `_profileStore.Save` returned without an exception; reset it at
`RestartRun`. Call `MonetisationRuntime.Offer.Prefetch()` as each run begins. Today `SaveProfile` swallows an IO
failure, and a run that banked nothing triggers no save at all, so neither can stand for "saved".

**RunTelemetry**: one getter, `public string RunId => _runId;` - the recorder's run id, kept after `run_end`, which
clears the session's own before the result screen opens. The bridge exposes it. (A scene without telemetry falls back to
a fresh `Guid.NewGuid().ToString("N")` per run.)

**RunResultView**, in `Show()` after the labels:

    RewardOfferState state = offer.Open(runId, run.GoldBanked, () => _setup.ResultSaved, _setup.Profile.Deposit);

then draw one button from `offer.Refresh()`: `Video` "watch a video: +{bonus} gold", `Skip` "+{bonus} gold"
(the owner's), `NoAd` "no video right now" (shown, tappable), any other state hidden. The button calls `offer.Take()`;
`offer.Changed` redraws it and the gold label (`GoldBanked + bonus` once `LastCredited`), and `LastResult` gives the
plain words: watched or skip_owned "+{bonus} gold added", skipped "the video was closed early: no bonus", failed "the
video could not play: no bonus". `OnRestart` and `OnDestroy` call `offer.Close()`. The input delay already guarding the
two buttons guards this one. The strings go through `Loc` with their Turkish (docs/60). Where the skip's own purchase
button lives (`service.Purchase(StoreProducts.SkipRewardVideos)`) is question 1 below.

**The profile**: nothing new. `PlayerProfile.Deposit` takes the bonus; no field, no save version, no migration.

**The session clock**: a hidden `DontDestroyOnLoad` object made by the holder forwards `OnApplicationPause` to
`SessionClock.Backgrounded` / `Foregrounded` with the telemetry clock's `Seconds` for every scene (the bridge's
`OnApplicationPause` lives only with a gameplay scene); a resume with `NewSession` writes a `play_session` line
(ordinal, the ended session's length).

**At SDK integration** (docs/59 3.5, a Packages change at the owner's request): an AdMob adapter for `IRewardedVideo`
(`RewardedAd.Load`, `Show` with the earned-reward callback as Completed and a close without it as Skipped,
`MobileAds.RaiseAdEventsOnUnityMainThread = true`), a UMP adapter for `IAdConsent`, a Unity IAP adapter for
`IStoreEntitlements` (PBL 8+), and a GameAnalytics sink implementing `IAnalytics` that maps the game's events to its
own (`run_start`/`run_end` to progression, `currency_earned`/`currency_spent` to resource, `store_purchase` to business,
`reward_result` to an ad event with `RewardOfferCoordinator.Placement`). GameAnalytics' manual session handling is
turned on and driven by `SessionClock`: `Backgrounded` true calls `EndSession`, `Foregrounded` with `NewSession` calls
`StartSession`. For every existing event (RunTelemetry's included) to reach that sink, the smallest change is an
`event Action<TelemetryRecord> Recorded` on `TelemetrySession`, raised as each line is stamped, with the sink subscribed
to it; the other way, RunTelemetry emitting through `IAnalytics` (`AnalyticsFanOut` of the file and the backend),
touches each of its emits.

### 7.5 The analytics seam

`IAnalytics` (`Emit`, `EmitForRun`, `Flush`) is the game's event sink. `LocalFileAnalytics` is the existing telemetry
writer behind it, unchanged: every call is `TelemetrySession`'s own, so a line written through the seam is byte for byte
the line the session writes directly (tested). `AnalyticsFanOut` hands each event to every sink in order; a sink that
throws does not cost the others the event, and the first exception is rethrown after.

`SessionClock` models docs/59 3.3's "background trips to a video must not count as a new session". GameAnalytics'
default (its Unity configuration page) ends a session when the app goes to the background and starts one when it
returns, so a rewarded video's full-screen activity would split every session it is shown in. The clock keeps that rule
for an ordinary trip (`DefaultGraceSeconds = 0`: the session ends as the app leaves; a grace can be given) and exempts a
trip that began with a video on screen (`VideoOpened` / `VideoClosed`, called by the coordinator) for up to
`DefaultVideoTripSeconds = 300`; a longer one ends the session after all, as of the moment the app left. The video's
own time counts in the session. `TelemetrySession`'s id stays the launch's.

### 7.6 Checks

    dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~RewardOfferTests|FullyQualifiedName~RewardLedgerTests|FullyQualifiedName~EntitlementTests|FullyQualifiedName~AnalyticsSeamTests|FullyQualifiedName~SessionClockTests"
    pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -Filter "RewardOfferTests|RewardLedgerTests|EntitlementTests|AnalyticsSeamTests|SessionClockTests" -SkipApk
    pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -Filter "CultureSafetyTests|TelemetryJsonTests|TelemetryStoreTests|TelemetryLogTests|TelemetrySessionTests|RunTelemetryTests" -SkipApk

The second is the new tests in Unity (its own NUnit, and the test assembly apart from the game's, so no test reaches an
`internal`); the third, the telemetry and culture tests beside them, unchanged.

### 7.7 Questions

1. **Where is the skip sold?** A small "skip reward videos" link under the result screen's offer (seen exactly where the
   video is), a line in the pause overlay's settings, or both. The price string comes from the store at integration.
2. **A failed video** (no fault of the player's) ends the offer like a skipped one. Should it allow one retry instead?
3. **A run that banked 1 gold** is offered +1. Should the offer need a minimum (say 10 gold) to be worth a video?
