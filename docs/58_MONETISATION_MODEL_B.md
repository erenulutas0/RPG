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
   `gear_economy.py` models it with and without the x1.5 bonus before release, on the tiers' payouts (section 4a).
2. **The return rate:** the share of videos followed by a new run.
3. **The purchase rate** of the skip.

If the free player progresses and returns, the next thing sold is content: a new region or a gameplay expansion. Power
is never sold.

## 4a. Modelled (2026-11-19)

Item 1 before release: docs/54's economy model with the video's bonus and the difficulty tiers' payouts.

    python Tools/Economy/gear_economy.py --video
      # --watch-rates 0,30,60,100 --multiplier 150 --min-gold 10 --from-run 2 --video-seconds 30
      # --policies forged,matched --cohorts careful,first-card,clumsy --multipliers 125,150,175,200
      # seed 58, 2,000 players a cell; 2 min 14 s for every table below

The script's default output (docs/54's tables) is unchanged, byte for byte.

### Method

- **Income.** Each run is one of the player type's 200 measured runs (`income-2026-11-16.csv`: `GearEconomyResearch`,
  seeds 1-100 a hero, both heroes, typical hands). Its banked gold, floors cleared, elites and bosses give the loot under
  the gold's rule (docs/54 section 9). Its measured minutes give the hours.
- **Spending.** docs/54's expected habit, the one behind its headline table. Boosters are used as in docs/54 section 5,
  a quarter of all gold (the bonus included) is set aside for efsun, and four items are worked one at a time to +9 after
  every run.
- **The bonus.** It follows `RewardOfferRule` exactly:
  - the run's banked gold x 50 / 100, rounded up to whole gold;
  - from the second run;
  - only when the run banked at least 10;
  - gold only: never materials, boosters or items.

  A player with a watch rate of w % takes each offered video with chance w, run by run. 100 % also stands for the skip
  owner, who gets the same gold. Each video taken adds 30 s to the hours, so a skip owner's hours are the 100 % row's
  less half a minute a video.
- **Tier policies.**
  - **Stays on Forged.**
  - **Moves up when matched.** After a run, the player moves up one tier when both hold:
    - this profile has walked out of the Descent on its current tier (the unlock chain, docs/54 section 21);
    - its gear power has reached the next tier's matched set: Tempered +14.7 % (a +5 set), Inferno +30.9 % (+9),
      Abyssal +41.9 % (+9 with good efsun).
- **A tier's run** is the player type's measured run with the tier's rewards (`TIER_GOLD`, `TIER_REWARD`):
  - gold x1.25 / x1.53 / x2.00, the measured ratio of a careful run's banked gold (498 / 620.5 / 760.5 / 998, seeds
    5201-8400);
  - materials and booster chances x1.25 / x1.5 / x2, a fraction made whole by one draw, as `LootRule` does;
  - items at Forged's chance.
- **Paired.** Each player has its own seeded streams: the runs drawn, their loot, the Anvil and the watch. So the watch
  rates and policies play the same runs and strikes, and a difference between two rows is the bonus's, not noise.
- **Player types.** Careful, typical and clumsy at their measured completion: 96.5 %, 87.5 % and 32.5 % of the
  income file's runs. Their runs last 14.1, 13.3 and 7.2 minutes. "Typical" is the **first-card** cohort. The income file
  has no cohort named "typical", since in this project the word names the hands every cohort plays with. First-card
  kites and steps in as careful does but takes the first card offered (docs/52 2.3). docs/56's bar gates it beside
  careful and clumsy.

### Assumptions, stated plainly

1. **Gear never feeds back into income** (docs/54 section 9). A geared clumsy player completes more runs (docs/54
   section 20), so its rows are pessimistic, for the free player and the watcher alike.
2. **A tier's run is the Forged run with the tier's rewards.** docs/54 section 21 measured the matched set's
   completion on its tier 0.3 to 2.9 points above the ungeared cohort's on Forged, and its length within 0.1 minute.
3. **Gear power counts main stats only.** The model rolls no efsun lines.
   - Worked weapon first, the +14.7 % for Tempered is reached at weapon +9 with armour +5. Real lines would move a
     player up a little sooner.
   - Inferno's +30.9 % is the +9 set itself, and Abyssal's needs good efsun. So on the road to a +9 set the matched
     player plays Forged, then Tempered.
4. **A player moves up only when matched.** A player who moves up with less gear is not modelled: its completion on an
   unmatched tier was never measured.
5. **The watch rate is fixed** and independent from run to run. A real player's rate may drift.
6. **Hours count only the runs' own minutes**, plus 30 s a video. Menus, the Anvil and loading are not counted, and they
   cost the clumsy player more, since it plays twice as many runs.
7. **A calendar** is read at an hour of play a day: about 4 careful runs, 8 clumsy runs.

### What a run pays (mean; the video's bonus is what a taken video adds)

| Type | Completed | Minutes | Gold: Forged / Tempered / Inferno / Abyssal | Video bonus: F / T / I / A | Spores, Forged / Abyssal |
|---|---|---|---|---|---|
| careful | 96.5 % | 14.1 | 496 / 621 / 759 / 992 | +249 / +310 / +380 / +497 | 2.89 / 5.78 |
| typical (first-card) | 87.5 % | 13.3 | 473 / 592 / 722 / 945 | +236 / +295 / +361 / +472 | 2.63 / 5.30 |
| clumsy | 32.5 % | 7.2 | 224 / 280 / 346 / 448 | +112 / +140 / +172 / +224 | 0.97 / 1.97 |

A careful Forged run pays 35 gold a minute of play. Its video pays 249 in half a minute: about 14 times the run's rate.
For the clumsy player it is 7 times.

### Runs and hours to +6 and +9, one item, then the full set

P50 runs (hours of play, videos included).

| Type | Tiers | Watch | One +6 | One +9 | Set +6 | Set +9 | Set +9, P90 | First Tempered run | Videos to set +9 |
|---|---|---|---|---|---|---|---|---|---|
| careful | stays on Forged | 0 % | 4 (0.9 h) | **32 (7.5 h)** | 103 (24.2 h) | **135 (31.7 h)** | 176 | - | 0 |
| careful | stays on Forged | 30 % | 3 (0.7 h) | 28 (6.7 h) | 90 (21.3 h) | 118 (27.9 h) | 154 | - | 35 |
| careful | stays on Forged | 60 % | 3 (0.7 h) | 25 (6.0 h) | 80 (19.2 h) | 104 (25.1 h) | 137 | - | 62 |
| careful | stays on Forged | 100 % | 3 (0.7 h) | 22 (5.4 h) | 70 (16.9 h) | 91 (22.1 h) | 118 | - | 90 |
| careful | moves up when matched | 0 % | 4 (0.9 h) | **32 (7.5 h)** | 90 (21.1 h) | **115 (27.0 h)** | 151 | 35 | 0 |
| careful | moves up when matched | 30 % | 3 (0.7 h) | 28 (6.7 h) | 78 (18.5 h) | 100 (23.8 h) | 132 | 31 | 30 |
| careful | moves up when matched | 60 % | 3 (0.7 h) | 25 (6.0 h) | 70 (16.7 h) | 89 (21.4 h) | 117 | 28 | 53 |
| careful | moves up when matched | 100 % | 3 (0.7 h) | 22 (5.4 h) | 61 (14.8 h) | 78 (18.9 h) | 102 | 24 | 77 |
| typical | stays on Forged | 0 % | 4 (0.9 h) | **34 (7.5 h)** | 108 (24.0 h) | **143 (31.6 h)** | 186 | - | 0 |
| typical | stays on Forged | 30 % | 4 (0.8 h) | 30 (6.7 h) | 95 (21.2 h) | 124 (27.8 h) | 162 | - | 36 |
| typical | stays on Forged | 60 % | 3 (0.7 h) | 27 (6.0 h) | 84 (19.0 h) | 110 (24.9 h) | 144 | - | 65 |
| typical | stays on Forged | 100 % | 3 (0.7 h) | 23 (5.3 h) | 73 (16.8 h) | 96 (22.0 h) | 124 | - | 94 |
| typical | moves up when matched | 0 % | 4 (0.9 h) | **34 (7.5 h)** | 95 (21.0 h) | **122 (26.8 h)** | 160 | 37 | 0 |
| typical | moves up when matched | 30 % | 4 (0.8 h) | 30 (6.7 h) | 82 (18.5 h) | 106 (23.7 h) | 139 | 33 | 31 |
| typical | moves up when matched | 60 % | 3 (0.7 h) | 27 (6.0 h) | 73 (16.6 h) | 94 (21.3 h) | 123 | 29 | 56 |
| typical | moves up when matched | 100 % | 3 (0.7 h) | 23 (5.3 h) | 64 (14.7 h) | 82 (18.8 h) | 106 | 26 | 81 |
| clumsy | stays on Forged | 0 % | 8 (1.0 h) | **71 (8.4 h)** | 229 (27.5 h) | **300 (36.0 h)** | 396 | - | 0 |
| clumsy | stays on Forged | 30 % | 8 (0.9 h) | 62 (7.6 h) | 200 (24.5 h) | 262 (32.0 h) | 348 | - | 77 |
| clumsy | stays on Forged | 60 % | 7 (0.9 h) | 55 (6.9 h) | 177 (22.1 h) | 231 (28.9 h) | 308 | - | 137 |
| clumsy | stays on Forged | 100 % | 6 (0.8 h) | 48 (6.2 h) | 155 (19.7 h) | 201 (25.7 h) | 269 | - | 199 |
| clumsy | moves up when matched | 0 % | 8 (1.0 h) | **71 (8.4 h)** | 199 (23.9 h) | **256 (30.7 h)** | 340 | 76 | 0 |
| clumsy | moves up when matched | 30 % | 8 (0.9 h) | 62 (7.6 h) | 175 (21.3 h) | 223 (27.3 h) | 298 | 67 | 66 |
| clumsy | moves up when matched | 60 % | 7 (0.9 h) | 55 (6.9 h) | 155 (19.3 h) | 198 (24.6 h) | 265 | 60 | 117 |
| clumsy | moves up when matched | 100 % | 6 (0.8 h) | 48 (6.2 h) | 134 (17.2 h) | 172 (22.0 h) | 229 | 53 | 171 |

- **The free careful player on Forged lands on docs/54 section 9 exactly:** one +9 in 32 runs, the set in 135. The
  streams here are different, so this is a check of the model.
- **Every matched player reaches Tempered**: all 2,000 of each type do, on every watch rate.
- **Moving up when matched** saves the free player about 15 % of the road to a set: 135 -> 115 runs careful, 300 -> 256
  clumsy. Nothing before one +9 changes, because the move comes after it.
- **In hours, the three types are close.** A free player needs about 7.5-8.4 h for one +9 and 27-36 h for the set. The
  clumsy player plays twice the runs, but each is half as long. Menus between runs are not counted (assumption 6).

### How much sooner than the free player

The free player is the 0 % row. Each cell is the share of P50 runs saved (runs, hours) on the same tier policy.
One +6 shows only the runs, since its P50 is 3-8 runs.

| Type | Tiers | Watch | One +6 | One +9 | Set +6 | Set +9 |
|---|---|---|---|---|---|---|
| careful | stays on Forged | 30 % | 1 run (0.2 h) | 12 % (4 runs, 0.8 h) | 13 % (13 runs, 2.9 h) | 13 % (17 runs, 3.8 h) |
| careful | stays on Forged | 60 % | 1 run (0.2 h) | 22 % (7 runs, 1.5 h) | 22 % (23 runs, 5.1 h) | 23 % (31 runs, 6.6 h) |
| careful | stays on Forged | 100 % | 1 run (0.2 h) | **31 % (10 runs, 2.2 h)** | 32 % (33 runs, 7.3 h) | **33 % (44 runs, 9.6 h)** |
| careful | moves up when matched | 30 % | 1 run (0.2 h) | 12 % (4 runs, 0.8 h) | 13 % (12 runs, 2.6 h) | 13 % (15 runs, 3.2 h) |
| careful | moves up when matched | 60 % | 1 run (0.2 h) | 22 % (7 runs, 1.5 h) | 22 % (20 runs, 4.4 h) | 23 % (26 runs, 5.6 h) |
| careful | moves up when matched | 100 % | 1 run (0.2 h) | **31 % (10 runs, 2.2 h)** | 32 % (29 runs, 6.3 h) | **32 % (37 runs, 8.1 h)** |
| typical | stays on Forged | 30 % | 0 (0.1 h) | 12 % (4 runs, 0.8 h) | 12 % (13 runs, 2.8 h) | 13 % (19 runs, 3.8 h) |
| typical | stays on Forged | 60 % | 1 run (0.2 h) | 21 % (7 runs, 1.5 h) | 22 % (24 runs, 5.0 h) | 23 % (33 runs, 6.7 h) |
| typical | stays on Forged | 100 % | 1 run (0.2 h) | **32 % (11 runs, 2.1 h)** | 32 % (35 runs, 7.2 h) | **33 % (47 runs, 9.6 h)** |
| typical | moves up when matched | 30 % | 0 (0.1 h) | 12 % (4 runs, 0.8 h) | 14 % (13 runs, 2.4 h) | 13 % (16 runs, 3.1 h) |
| typical | moves up when matched | 60 % | 1 run (0.2 h) | 21 % (7 runs, 1.5 h) | 23 % (22 runs, 4.4 h) | 23 % (28 runs, 5.6 h) |
| typical | moves up when matched | 100 % | 1 run (0.2 h) | **32 % (11 runs, 2.1 h)** | 33 % (31 runs, 6.3 h) | **33 % (40 runs, 8.1 h)** |
| clumsy | stays on Forged | 30 % | 0 (0.1 h) | 13 % (9 runs, 0.8 h) | 13 % (29 runs, 3.0 h) | 13 % (38 runs, 4.0 h) |
| clumsy | stays on Forged | 60 % | 1 run (0.1 h) | 23 % (16 runs, 1.5 h) | 23 % (52 runs, 5.4 h) | 23 % (69 runs, 7.1 h) |
| clumsy | stays on Forged | 100 % | 2 runs (0.2 h) | **32 % (23 runs, 2.2 h)** | 32 % (74 runs, 7.8 h) | **33 % (99 runs, 10.2 h)** |
| clumsy | moves up when matched | 30 % | 0 (0.1 h) | 13 % (9 runs, 0.8 h) | 12 % (24 runs, 2.6 h) | 13 % (33 runs, 3.4 h) |
| clumsy | moves up when matched | 60 % | 1 run (0.1 h) | 23 % (16 runs, 1.5 h) | 22 % (44 runs, 4.7 h) | 23 % (58 runs, 6.1 h) |
| clumsy | moves up when matched | 100 % | 2 runs (0.2 h) | **32 % (23 runs, 2.2 h)** | 33 % (65 runs, 6.7 h) | **33 % (84 runs, 8.7 h)** |

- **The watcher's lead is the multiplier's arithmetic.** A 100 % watcher gets 1.5 times the gold, so it needs 1/1.5 of
  the runs: a third fewer, from one +9 on, for every type and on both policies.
- **Gold sets the pace, and the video buys only gold.** No floor material ever binds: even the clumsy watcher's Spores
  keep up.
- **The lead is linear in the watch rate:** about 13 % at 30 % and 23 % at 60 %.
- **Before one +6 it is a run or two at most**, because the first rungs are cheap (docs/54: +1..+3 cost 250 gold in all).

### The longest stretch between two levels gained

On the road to a +9 set: runs, P50 / P90 (hours, P50).

| Type | Tiers | 0 % | 30 % | 60 % | 100 % |
|---|---|---|---|---|---|
| careful | stays on Forged | 32 / 53 (7.6 h) | 28 / 47 (6.7 h) | 25 / 42 (6.0 h) | 21 / 36 (5.2 h) |
| careful | moves up when matched | 27 / 47 (6.2 h) | 24 / 41 (5.6 h) | 21 / 36 (5.0 h) | 18 / 31 (4.3 h) |
| typical | stays on Forged | 34 / 57 (7.6 h) | 30 / 49 (6.7 h) | 26 / 44 (5.9 h) | 23 / 38 (5.2 h) |
| typical | moves up when matched | 29 / 49 (6.2 h) | 26 / 43 (5.6 h) | 23 / 38 (5.0 h) | 19 / 33 (4.3 h) |
| clumsy | stays on Forged | 73 / 122 (8.6 h) | 63 / 107 (7.6 h) | 56 / 95 (6.9 h) | 48 / 82 (6.1 h) |
| clumsy | moves up when matched | 62 / 106 (7.1 h) | 55 / 92 (6.4 h) | 48 / 81 (5.8 h) | 41 / 71 (5.1 h) |

The longest stretch is always a +9 climb.
- **It is docs/54's +9 price, not the video's.** A 2,400-gold strike is 6.5 free careful runs of basma gold (372 a run
  after efsun's quarter), and 4.3 runs for a full watcher.
- **The free player meets it with or without the video.** The video shortens it by the same third.
- **Strikes keep coming during it, so it is not a wall:** about one every 6.5 careful runs, each with the memory bar
  rising. It is, though, the longest the free player goes without a "+1", about a week at an hour a day. The first
  month should read it (below).

### Other multipliers

The matched policy, P50 runs, and how much sooner than the free player.

| Multiplier | careful 100 %: set +9 (weeks at an hour a day) | careful, sooner at 30 % / 100 % | clumsy, sooner at 30 % / 100 % | Free player's pace against a full watcher's |
|---|---|---|---|---|
| none (the free player) | 115 (3.9) | - | - | - |
| x1.25 | 92 (3.2) | 7 % / 20 % | 7 % / 20 % | 0.80 |
| **x1.5** | **78 (2.7)** | **13 % / 32 %** | **13 % / 33 %** | **0.68** |
| x1.75 | 67 (2.3) | 17 % / 42 % | 18 % / 42 % | 0.58 |
| x2 | 59 (2.0) | 23 % / 49 % | 22 % / 49 % | 0.51 |

Typical tracks careful within two points at every multiplier. At 100 % the share saved stays close to 1 - 1/multiplier up to
x2, so materials stay out of the way there too. At x2 the clumsy watcher's Spores (1.0-1.2 a run against about 141 for
the set) are near binding.

### Judgement and recommendation

**Keep x1.5 for the first release, unchanged.**

1. **The free player has no wall.** The 0 % rows are docs/54's road unchanged, because the video adds and takes
   nothing away.
   - **Careful and typical:** one +6 in 4 runs (under an hour), one +9 in 32-34 runs (7.5 h, about a week at an hour a
     day), the set in 115-143 runs (27-32 h, about 4-4.5 weeks).
   - **Clumsy:** one +9 in 71 runs (8.4 h), the set in 256-300 runs (31-36 h, 4.5-5 weeks), pessimistically
     (assumption 1).
   - **The tiers help the free player** as much as anyone: 15 % off the set.
   - **Nothing is gated by the video.** Materials gate depth, and the tiers open by play. The video cannot buy +7 for a
     player who has not reached the Vaults.
2. **The video is worth it.** It pays half a run's gold for 30 s, 7-14 times the run's own rate a minute. A player who
   always takes it reaches one +9 and the set a third sooner (8-10 h of play saved on the set). A player who takes one
   in three gains 13 %.
3. **It is not mandatory.** At x1.5 the free player moves at two thirds of a full watcher's pace. The full watcher's set
   still takes 2.7-3.2 weeks at an hour a day, so docs/54's "weeks" holds for the most engaged players.
4. **Why not x2.** It puts the free player at half pace: skipping the video then costs as much play as the video gives,
   which is when a video starts to feel required. It also halves the long road the sink was built for (59 careful runs,
   2 weeks). And the skip product grants every bonus, so the multiplier is also the speed the product sells: x2 would
   make the one purchase a double-speed purchase, which leans on "power is never sold".
5. **Why not x1.25.** At x1.25 the video is still clearly worth its 30 s (7 times the run's rate), but the full watcher
   saves only a fifth. The free player gains nothing from a lower multiplier: its road is the same at every value.

### What the first month's real numbers should check

1. **The income.** `run_end`'s banked gold and completion by tier against the income table above (careful 496 / 621 /
   759 / 992), and the mix of players: how many play like careful, typical and clumsy.
2. **The watch rate.** Per offer (`reward_result` `watched` / `reward_offer`), and per profile: the spread of profiles'
   rates against the model's 0 / 30 / 60 / 100 %, and the skip's purchase rate.
3. **The never-watchers' pace.** Their runs to the first +6 and +9 (`anvil_attempt`'s `success` with `from_level` 5 and
   8, counted in runs) against the 0 % rows: P50 4 and 32 careful. If one +9 lands later than docs/54's P90 (56 careful
   runs, 122 clumsy), the free road is slower than modelled.
4. **The +9 stretch.** The share of never-watchers who stop playing during a +8 or +9 climb, against watchers at the
   same level and against earlier stretches. A drop there is the wall the model cannot see. Its knob is docs/54's
   `GOLD` at +8 and +9 (for everyone), not the video.
5. **The return rate** (item 2): new runs after a watched video against after a "No". If watchers return less, the
   video interrupts the session.
6. **The tier moves.** `run_start`'s `tier` and `gear_power`: do players move up at the matched power (the model:
   Tempered at run 35 for a free careful player), earlier with less gear (not modelled), or never?
7. **The habits the model assumes.** The share of gold spent on efsun (the model's quarter) and unspent gold, and runs
   and hours a day (the calendar's hour).

A different multiplier needs these numbers first. If never-watchers progress (items 3 and 4) and return at the
watchers' rate, x1.5 stands. If watchers take nearly every offer and never-watchers fall behind the model, look at the
road's prices before the bonus.

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
2. `gear_economy.py` with the tiers' payouts and the x1.5 bonus. Modelled: section 4a (x1.5 kept).
3. SDK integration, when the owner's accounts exist.
4. The closed test on Play, then the Turkish release.

## 7. Built (scaffold), 2026-11-19

Step 1 of section 6 and docs/59 section 3 items 2 and 3, as **pure code behind interfaces with offline fakes**: no SDK,
no package, no view, no scene, no asset. Nothing existing was edited but `Tools/CombatChecks/CombatChecks.csproj`
(the new files' lines). RunResultView, RunHud, MapChoiceView, RunTelemetry, RunTelemetryContext, the profile and every
profile save file are untouched (the difficulty tiers rewrite them); the wiring pass (7.4) comes after the tiers merge.
Nothing of the simulation was touched, so the default batch is unmoved by construction. **Wired into the game on
2026-11-23, still over the offline fakes: 7.8.**

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
| `Tests/EditMode/RewardOfferTests.cs`, `RewardLedgerTests.cs`, `EntitlementTests.cs`, `AnalyticsSeamTests.cs`, `SessionClockTests.cs` | 35 tests, in Unity's EditMode suite and in every plain `dotnet test` of `Tools/CombatChecks` |

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
- **A minimum of 10 banked gold** (the lead, 2026-11-19: question 3). Below `RewardOfferRule.MinimumRunGold = 10`, a
  constant beside `MultiplierPercent` and a context field like it, the offer is not shown (`NoGold`), not even to an
  owner of the skip: a +1 to +4 offer is never worth a video. 10 banked gives +5.
- **A completed run is one that reached its result screen** (a victory, an extraction or a defeat; a run left by a
  restart or a closed app is not). The count lives in the ledger, not the profile, so the profile and its save version
  stay untouched: a new ledger's first screen is `TooEarly`, its second offers. A lost ledger restarts the count, which
  costs one offer. Opening the same run id again counts nothing.
- **The states**, in the rule's order: `TooEarly` (fewer than 2), `NotSaved`, `NoGold` (less than the minimum banked),
  `Resolved` (credited, skipped, failed twice, or declined), then `Skip` for an owner (**even with a video ready**:
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
- **A skipped video ends the offer; a failed one allows one retry** (the lead, 2026-11-19: question 2). Skipping is the
  player's choice, so the offer is `Resolved` at once and nothing nags (section 1). A failure (no fill at the show, the
  network, a show error) is not the player's fault: `LastResult` says `Failed`, the next video is asked for at once, and
  the same screen offers it again (`Video`, or `NoAd` until one comes; `RetriesAfterAFailure = 1`). A second failure
  ends the offer. Leaving an open offer ends it as `declined`; leaving after an unanswered ask as `no_ad`; leaving with
  the retry untaken as `failed`. Each run still writes one `reward_result`, its final outcome; `ad_counters` counts every
  failed show.
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

Done on 2026-11-23 as planned here, with the details and the few departures in 7.8.

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
plain words: watched or skip_owned "+{bonus} gold added", skipped "the video was closed early: no bonus", failed with
the offer still shown "the video could not play: try once more", failed and over "the video could not play: no bonus".
`OnRestart` and `OnDestroy` call `offer.Close()`. The input delay already guarding the two buttons guards this one. The
strings go through `Loc` with their Turkish (docs/60).

**The skip's purchase** (`service.Purchase(StoreProducts.SkipRewardVideos)`, question 1 answered): in both places, low
key - a small link under the result screen's offer (not shown to an owner), and an entry in the settings: the pause
overlay for now, the map choice screen after localisation pass 2. Both come with this wiring pass.

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

### 7.7 Questions, answered by the lead (2026-11-19)

1. **Where is the skip sold?** Both places, low key: a small link under the result screen's offer, and an entry in the
   settings (the pause overlay now, the map choice screen after localisation pass 2). With the wiring pass (7.4); the
   price string comes from the store at integration.
2. **A failed video** allows one retry on the same screen; a second failure ends the offer; a skipped video still ends
   it. Built (7.2).
3. **A minimum:** the offer needs at least 10 banked gold, so a +1 offer never shows. Built: `MinimumRunGold`, beside
   `MultiplierPercent`, overridable in the context (7.2).

### 7.8 Wired (2026-11-23)

7.4's pass, **over the offline fakes only**: no SDK, no package, no ProjectSettings change, no scene or prefab edited (every
new part is built in code). The simulation reads none of it, and the default batch (`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0
FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_FLOORS=2`) wrote the same bytes before and after: the five CSV and
Markdown tables byte-identical, and `foundry-batch.md` apart only in its run's timestamp and its "Wall s" column (the
batch's own wall-clock seconds).

| Where | What |
|---|---|
| `Scripts/Monetisation/Unity/MonetisationRuntime.cs` | 7.4's launch-long holder, one per profile folder like `TelemetryRuntime` (statics reset in `SubsystemRegistration`): `EntitlementService` over `FakeStoreEntitlements` (`entitlements.json` at `ProfileLocation.Resolve()`, `Refresh()` at boot), `RewardLedgerStore` and its ledger, a `SessionClock` started with the telemetry session, the `RewardOfferCoordinator` (`FakeRewardedVideo`, `FakeAdConsent(true)`, `OwnsSkip` from the service, `SaveLedger = store.Save`, `LocalFileAnalytics(session)`); `consent.Gather(() => offer.Prefetch())` at boot; `BeginRun` (the development switch, then `Prefetch`), `BuySkip`, `Flush`, `Changed` |
| `Scripts/Monetisation/Unity/PlaySessionForwarder.cs` | the hidden `DontDestroyOnLoad` object, one a launch, forwarding `OnApplicationPause` to the clock; a return that starts a new session writes `play_session` (`ordinal`, `ended_sec`) |
| `Scripts/Core/CombatSetup.cs` | `ResultSaved`: `Run.Ended` subscribed right after `Bank = new RunBank(...)` saves the profile once more and sets it only when that write returned without an exception; `RestartRun` clears it. `RunId`: telemetry's, else one `Guid.NewGuid().ToString("N")` a run. `BeginRun`: `run_start` and `MonetisationRuntime.BeginRun()`, wherever the run's telemetry began (at load, or when the map choice starts the run) |
| `Scripts/Analytics/RunTelemetry.cs`, `Unity/RunTelemetryBridge.cs` | `RunId => _runId`, through the bridge |
| `Scripts/Monetisation/RewardOfferCoordinator.cs` | the one addition to the scaffold: `LastResultRunId`, the run `LastResult` belongs to (`Open` keeps the last run's result, so a screen must tell it from its own); one EditMode test |
| `Scripts/UI/RunResultView.cs`, `RunResultView.Offer.cs` | the offer on the result screen (below) |
| `Scripts/UI/SkipVideosButton.cs`, `RewardOfferText.cs` | the skip's link and settings entry; the offer's and the store's words |
| `Scripts/UI/PauseView.cs` | the skip's entry above the language toggle |
| `Scripts/Core/DevelopmentStart.cs` | `development/ads.txt` (below) |
| `Resources/Localisation/Strings.txt` | 12 rows, `result.offer_*` and `store.*`, with their Turkish (docs/60 sections 3 and 9) |
| `Tests/PlayMode/RewardOfferFlowTests.cs` | 12 PlayMode tests (below); `RelicForgeFlowTests` reads the saved profile now that a run's end writes one |
| `Tools/ArtReview/Run-RewardOfferProof.ps1`, `RewardOfferCapture.cs` | the capture |

**The result screen.** `Show()`, after its labels, opens the offer: `offer.Open(setup.RunId, run.GoldBanked, () =>
setup.ResultSaved, setup.Profile.Deposit)`. One button is drawn from `offer.Refresh()`: Video "Watch a video: +{bonus}
gold", Skip "Skip owned: +{bonus} gold" (the owner's: never a video, even with one ready), NoAd "No video right now"
(shown and tappable: a tap asks for a video, and counts as one attempt a screen); any other state shows none. A tap is `offer.Take()`. `Changed`
(the offer's, the entitlement's and the language's) draws the button again, the gold label (`GoldBanked + bonus` once
credited) and the forge's hint (the profile's gold now holds the bonus). `LastResult`, for this run only
(`LastResultRunId`), gives the plain words: "+{bonus} gold added", "The video was closed early: no bonus", "The video could
not play: try once more" (the retry, the button still shown) and "The video could not play: no bonus". Restart and the
view's destruction call `offer.Close()` - only for the screen's own run - and flush the lines it wrote. The 0.5 s input
delay that guards Restart and the Forge guards the offer and the link.

**The layout.** The scene lays the ledger out for the summary alone (title, cause, progress, gold, divider, build, the
forge's hint, Restart, Forge on a 984 x 1370 surface). When the offer shows as the screen opens, a 180-unit row opens
under the gold label: everything whose middle stands above the gold label's foot rises 90 units (the title's painted plate
and crest with it), everything below sinks 90, and the surface grows 180, so the ledger stays centred: 1550 units of the
shortest safe area's 1760, 105 to spare at each end. The row holds the button (760 x 100, the forge's secondary plate) 12
units under the gold, and under it a 52-unit line for the link or, during a retry, the words; once the offer is over the
words stand in the button's place. The row stays for the rest of the screen, so nothing jumps; a screen that opens
TooEarly, NotSaved or NoGold keeps the scene's layout exactly.

**The skip.** `SkipVideosButton`, built in code like `LanguageToggleView`: a bare line of text under the offer (shown while
the offer stands, never to an owner, and not while the retry's words take its line), and an entry on the forge's
secondary plate on the pause overlay, 24 units above the language toggle and as wide, under the hint. A tap is
`MonetisationRuntime.BuySkip()` (`EntitlementService.Purchase(StoreProducts.SkipRewardVideos)`); with the fake store it is
bought at once, cached in `entitlements.json`, acknowledged and logged (`store_purchase`). The words: "Skip reward videos",
"Opening the store..." while a purchase is open, "Purchase pending", "The store did not answer: try later" (a cancelled
one says nothing), and on the pause overlay "Skip reward videos: owned" (disabled) once bought. A purchase on the result
screen turns its Video into Skip in the same row. The fake store's account starts as the cache says: otherwise the boot's
query would answer "nothing owned" and take back a skip bought in an earlier launch.

**The development switch.** `development/ads.txt` in the profile folder (the Editor and a development build only, as every
development file): `completed` (also no file), `skipped`, `failed` (every show fails: the retry, then the end) or `noad`
(no video ever fills; a ready one is let go). Read at boot and again as each run begins.

**Nothing logs in normal play.** Only an unreadable `rewards.json` or `entitlements.json` is reported, as a warning, as the
profile's own recovery is.

**Checks.** The gates: `dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false` (Başarılı, 1120) and the compile
check (0 errors). In Unity:

    pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -Filter "RewardOfferFlowTests|RelicForgeFlowTests|RunResultTests|TurkishScreensTests|TurkishTierScreensTests|PaintedScreensFlowTests|ArtHudFlowTests|TelemetryFlowTests|TierChoiceFlowTests" -SkipApk
    pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -Filter "RewardOfferTests|RewardLedgerTests|EntitlementTests|AnalyticsSeamTests|SessionClockTests|LocalisationTableTests|LocalisationAssetTests" -SkipApk

`RewardOfferFlowTests`: the offer from the second completed run (through a real restart); the 10-gold minimum; no offer
while `ResultSaved` is false (a folder where the profile's temp file goes fails every save); a watched video credited once
(the fake reporting twice), saved, and shown; the skip bought from the link and credited without a video; an owner from
the cache, and the pause overlay saying owned; the pause overlay selling it; a failed video's retry crediting;
`ads.txt` failed and noad; leaving records `declined`; and the screen with its offer fitting both safe areas in English and,
state by state, in Turkish (every label at its smallest size, the row inside the ledger and clear of the gold and the
divider). The capture (graphics, 1080 x 2340 on a tr-TR culture): Video, Skip, NoAd, credited, the failed video's retry and
the pause entry, in English and Turkish, into `ArtDirection/2026-11-23/reward-offer-01/` (untracked):

    pwsh -File Tools/ArtReview/Run-RewardOfferProof.ps1

**Left for the lead and the owner.**

1. The map choice screen's settings: 7.4 placed a second skip entry there "after localisation pass 2", which is done; its
   title band holds the language toggle. Not added in this pass (the task named the pause overlay); a question.
2. The price on the link and the entry comes from the store at SDK integration; the fake has none, so none is shown.
3. `Skip owned: +{bonus} gold` reads "Videosuz al" in Turkish (docs/60 question 12).
4. `play_session` is written only on a return that starts a new session; the launch's first session is `session_start`.
