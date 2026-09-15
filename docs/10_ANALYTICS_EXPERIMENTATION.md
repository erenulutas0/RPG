# Analytics & Experimentation

Status 2026-09-15: no analytics SDK is integrated, and nothing leaves the device. A local event log (`21`, schema 1) writes this document's run, room, floor, boss, upgrade, checkpoint and currency events to `telemetry/events.jsonl` beside the profile. The implemented names are `run_start`, `room_start`, `room_complete`, `floor_complete`, `extract_choice`, `boss_start`, `boss_end`, `run_end`, `upgrade_offered`, `upgrade_selected`, `currency_earned` and `currency_spent`. It adds `forge_selected`, `first_kill`, `chest_opened`, `ability_used`, `run_interrupted`, app background/foreground and a drop report. It also separates active combat time from choice, pause and background time. Onboarding, hero unlocks, meta upgrades and monetization events are not logged because those features do not exist. `session_end` is not logged, because a mobile app is not reliably told it ends. A provider below is still a later decision.

## Recommended stack

For a solo Unity game, Firebase Analytics is a practical baseline:
- supports Unity,
- free analytics tier,
- up to 500 distinct events supported by Firebase Analytics documentation.

Crash reporting can be added separately.

## North-star hierarchy

Do not optimize revenue before retention.

Priority:
1. technical stability,
2. onboarding completion,
3. first-session engagement,
4. D1/D7 retention,
5. content progression,
6. monetization,
7. LTV/ROAS.

## Minimum event schema

### Session
- `session_start`
- `session_end` if reliable/meaningful

### Onboarding
- `tutorial_step`
  - step_id
- `tutorial_complete`

### Run
- `run_start`
  - hero_id
  - weapon_id
- `room_start`
  - floor_index
  - room_index
- `room_complete`
  - floor_index
  - room_index
- `floor_complete`
  - floor_index
- `extract_choice`
  - choice (extract / descend)
  - floor_index
- `boss_start`
- `boss_end`
- `run_end`
  - result
  - cause (extracted / killed_by:<enemy_id>)
  - duration_sec
  - floors_completed
  - rooms_completed

### Upgrade
- `upgrade_offered`
- `upgrade_selected`
  - upgrade_id
  - choice_slot
  - run_level

### Progression
- `weapon_unlocked`
- `hero_unlocked`
- `meta_upgrade`

### Economy
- `currency_earned`
- `currency_spent`
  - source/sink
  - amount

### Monetization later
- `rewarded_offer_shown`
- `rewarded_started`
- `rewarded_completed`
- `iap_viewed`
- `iap_purchase`

Do not create separate event names for every weapon.

## Funnels

### First session
install/open
→ gameplay start
→ first kill
→ first upgrade
→ first room clear
→ first boss
→ run complete
→ second run start

### Economy
run reward
→ progression screen
→ spend
→ next run

## Metrics

- tutorial completion %
- time to first kill
- time to first upgrade
- first-run completion
- second-run-start rate
- average sessions/day
- session length
- D1/D7/D30
- boss fail rates
- upgrade pick rates
- hero/weapon win rates
- economy source/sink ratio

## Experiment discipline

Change one major hypothesis at a time.

Experiment record:
- hypothesis,
- control,
- treatment,
- target metric,
- guardrail metric,
- duration/sample,
- result,
- decision.

## Example

Hypothesis:
Showing 3 upgrade choices instead of 2 increases build ownership.

Primary:
- second-run rate.

Guardrails:
- choice screen time,
- run completion,
- crash rate.

Do not ship experiments with no decision rule.
