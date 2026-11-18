# 59 - The path to the Turkish release (plan, 2026-11-18)

The owner researched the release path for a Turkish individual developer: Play Console, tax, the ad network, the
first players and retention benchmarks. The report is `docs/research/2026-11-18_release_path_owner_report.md` (in
Turkish). This document records the decisions taken on it, what the build already meets, and the order of work.
Monetisation itself is docs/58; the arena's release order is docs/55 section 9.

## 1. Decisions (2026-11-18)

| # | Decision | By |
|---|---|---|
| 1 | **Turkish and English** at release. The game opens in the phone's language (Turkish when the system language is Turkish, English otherwise), and the language can be changed in the settings | owner |
| 2 | **GameAnalytics** measures retention (D1/D7/D30), sessions and the game's own events. The report's benchmark table comes from the same tool, so our cohorts compare directly | owner |
| 3 | The **personal Play Console account opens near the end**, when a release candidate exists. The owner knows that a personal account that earns money shows its full address on the store page | owner |
| 4 | The **developer site** (privacy policy, support, `app-ads.txt`) is decided later | owner |
| 5 | **AdMob alone** at first: one rewarded unit and Google's UMP consent. Mediation (LevelPlay, MAX) only after 2-4 weeks of our own Turkish numbers show a second network fills what AdMob misses | lead, from the report |

## 2. What the build already meets (measured 2026-11-18 on the APK of main 99f8d780)

| Requirement (Nov 2026) | Ours | Status |
|---|---|---|
| targetSdkVersion 36+ for new apps (from 31 Aug 2026) | target "automatic" = the highest installed, android-36; `aapt2` reads `targetSdkVersion:'36'`; compileSdk 36 | **met**; pin it to 36 explicitly in the release build so a missing SDK cannot lower it |
| ARM64, IL2CPP | IL2CPP, `arm64-v8a` only; minSdk 26 | **met** |
| 16 KB memory pages | every native library's LOAD segments align to 0x4000 (`llvm-readelf -lW`: libunity, libil2cpp, libmain, libswappywrapper, libc++_shared) | **met**; re-check on the release AAB and after each SDK is added |
| An AAB, a release (non-development) build, Play App Signing, an upload key | the prototype pipeline builds a development APK (`AndroidPrototypeBuild`, `buildAppBundle = false`) | **to build**: a release pipeline beside the prototype one |
| Play Billing Library 8+ for new uploads | no IAP package yet | **at integration**: the Unity IAP version must bundle PBL 8+ |
| A permanent package name | `com.cryptforge.prototype` | **to choose with the owner** before the first upload; it can never change after |

## 3. Engineering work before the closed test

1. **Localisation, TR + EN** (decision 1):
   - a string table, a lookup, the system-language default and a settings toggle stored apart from the save;
   - every UI string moved into the table;
   - a Turkish glossary for the game's terms. Basma and efsun are already Turkish; the owner reviews the Turkish column;
   - font coverage for ş ğ ı İ ç ö ü;
   - **culture safety**: saves, telemetry and parsers use the invariant culture whatever the phone's locale (Turkish writes "1,5", and "i".ToUpper() gives "İ"); numbers shown to the player use the chosen language's format on purpose.
2. **Monetisation scaffolding** (docs/58 section 6): `IRewardedVideo`, `IStoreEntitlements` and a consent gate (`CanRequestAds`). Offline fakes stand in for each. Also the result-screen offer and save-before-video.
3. **The analytics seam:** an `IAnalytics` sink behind the existing telemetry. It keeps the local file log and later gains the GameAnalytics backend. The report's minimum events:
   - first open, run start and end, first death, first basma, the second run;
   - the reward offer, the player's choice and the reward credited;
   - purchases.
   Background trips to a video must not count as a new session.
4. **The release build pipeline:** AAB, release configuration, versionCode scheme, the upload keystore, a pinned target SDK, and the 16 KB and size checks (`bundletool get-size total`) in its report.
5. **SDK integration**, once the owner's accounts exist: the Google Mobile Ads Unity plugin with UMP, Unity IAP (PBL 8+), GameAnalytics. These are Packages changes, made then at the owner's request. Each is measured for size and checked for 16 KB.
6. **Store and policy texts**, drafted by the lead for the owner to publish:
   - the privacy policy (TR and EN, naming AdMob, Google UMP and GameAnalytics), with the KVKK disclosure kept apart from consent;
   - the data-safety answers from the real SDK list, and the IARC answers;
   - the store listing in Turkish. The screenshots show real vertical combat, then "your item never breaks", then the efsun choice. Note: the report's draft line says "one-handed", but the game is two-handed (docs/57 section 12).
   - the reviewer's path to the reward offer.

   **Drafted 2026-11-19 in `docs/store/`** (its README lists the placeholders the owner fills, the open legal
   questions and what the build must settle first).

## 4. The owner's tasks, in order (from the report's section 6, timed to "near the end")

| When | Task |
|---|---|
| A week before the release candidate | Recruit **18-24 testers** with different phones (friends and university clubs, Turkish developer forums, players of the genre) |
| At the release candidate | Open the **personal Play Console account**: $25, ID, address, then device verification on an Android 10+ phone. Choose the package name with the lead |
| Same week | Apply for the **GVK 20/B certificate** (Dijital Vergi Dairesi). Open or designate the dedicated bank account. Notify its IBAN to the tax office within a month |
| Before any real income | Set up the **Play payments profile** and the **AdMob publisher account**: bank, tax forms (W-8BEN if asked), identity and PIN steps. **Ask an SMMM whether AdMob's direct payments fall under 20/B**: the report found no official source that names AdMob, and asks for a written opinion if it matters |
| Closed test | The testers opt in. **At least 12 must stay opted in for 14 continuous days.** Keep a record of bugs found and fixed, because the production-access request asks for it |
| After 14 days | Request production access (usually 7 days or less), then submit the release; AdMob runs its own app review |

## 5. Soft-launch gates (the report's decision rules, adopted)

- **Trial targets** (ours, not genre averages):
  - D1 30-35 %, D7 10-12 %, D30 4-6 %;
  - sessions of 6-10 minutes, 2-4 sessions a day.
  - For reference, GameAnalytics' October 2026 medians: D1 20.8 %, D7 3.8 %, D30 0.9 %; the top quarter: 28.3 / 6.9 / 2.0 %.
- **The closed test (12-24 people) reads behaviour and bugs, not market fit.** One tester is 8.3 points.
- **The soft launch:** 300-500 new, voluntary first opens in Turkey, with friends, paid and organic traffic kept apart.
  - **D1 < 25 %:** fix the first minute, the controls, loading and the first death first.
  - **D1 ≥ 30 % but D7 < 8 %:** fix the first basma, the next item goal and the second session before adding anything.
  - **D7 ≥ 10 % and a mature D30 ≥ 4 %, in two cohorts:** widen the Turkish test.
- D30 is read only after a cohort's full 30 days. Retention is cohorted by first-open day on Europe/Istanbul days.

## 6. Order

1. **Now (game work):**
   - finish the tiers, the Regent and hero skills phases 1-4;
   - P0's local arena;
   - the monetisation scaffolding and the analytics seam;
   - localisation;
   - the V0.1 device row on the owner's S23.
2. **The release candidate:** the release pipeline, the policy and store drafts, and the owner's account steps (section 4).
3. **SDKs:** integrated and measured, then tested with test ads and license testers.
4. **The closed test:** at least 14 days. Then production access and the Turkish release.

The report puts this at **4-7 weeks** from a playable release candidate, plus a month for the first D30.
