# Monetization Strategy

## Principle

**Do not monetize the prototype. Design hooks, not pressure.**

The game must first be worth returning to.

## Recommended model

Hybrid:
- rewarded ads,
- remove-ads/value IAP,
- starter/value packs,
- cosmetics later.

Avoid paid randomized loot in MVP.

## Rewarded ads

Unity's current guidance emphasizes opt-in rewarded ads and notes that these placements can work well when the game has currency, boosts or consumables. Unity also recommends measuring retention, engagement and ARPDAU and A/B testing placements.

Potential placements:
- multiply end-run gold,
- optional revive once,
- bonus offline reward,
- optional reroll of upgrade choices.

### Rules
- never force the ad,
- reward must be explicit,
- cap/pacing,
- no ad before player understands gameplay,
- measure whether ads damage session flow.

## Interstitials

Recommendation for this project:
- do not include in first monetized test,
- consider only after strong retention and carefully between natural breaks,
- rewarded-first is safer for experience.

## IAP candidates

### Remove Ads
If interstitial/banner ever exists.
Rewarded ads may remain optional unless product says otherwise.

### Starter Pack
- fixed contents,
- clear value,
- no randomized outcome.

### Cosmetic Pack
- hero skins,
- weapon effects,
- UI themes.

### Convenience
Use cautiously:
- currency pack,
- unlock acceleration.

Avoid pay-to-win if long-term competitive features are introduced.

## Loot boxes / randomized purchases

Both Apple and Google require odds disclosure for purchased randomized virtual items.

Simplest MVP decision:
**do not sell randomized loot.**

## Economy guardrails

Never make:
- normal play intentionally miserable,
- ad watching the dominant source of resources,
- paid currency necessary to experiment with builds.

## Monetization rollout

1. no monetization prototype,
2. rewarded hooks disabled behind config,
3. soft-launch test with 1–2 rewarded placements,
4. add IAP only after retention baseline,
5. test price/value by region carefully.

## Ad technology note

Unity states that from April 1, 2026 direct integration via its legacy Advertisement package may see reduced performance and recommends mediation/LevelPlay bidding for best results.

So if using Unity Ads, prefer a modern mediation/bidding integration rather than starting a new project on legacy direct integration.
