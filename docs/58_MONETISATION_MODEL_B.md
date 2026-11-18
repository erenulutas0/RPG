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

## 6. Order

1. The abstractions, the result-screen offer and the save-before-video flow, behind an offline fake (now; no packages).
2. `gear_economy.py` with the tiers' payouts and the x1.5 bonus.
3. SDK integration, when the owner's accounts exist.
4. The closed test on Play, then the Turkish release.
