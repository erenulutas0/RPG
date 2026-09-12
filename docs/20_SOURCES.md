# Research Sources

Accessed / compiled: 2026-09-12.

## Reference game

### Bones and Coins — Steam
https://store.steampowered.com/app/4134650/Bones_and_Coins/

Used for:
- official genre description,
- content scope,
- release/reference facts,
- current review snapshot.

### Bones and Coins Demo — Steam
https://store.steampowered.com/app/4213470/Bones_and_Coins_Demo/

Used for:
- demo positioning and genre tags.

## Mobile benchmarks

### GameAnalytics — 2026 Mobile & PC Gaming Benchmarks
https://www.gameanalytics.com/cn/reports/2026-mobile-pc-gaming-benchmarks

Key reported 2025 mobile observations:
- median D1 around 22%,
- top 25% D1 just above 30%,
- top 10% around 40%,
- median D7 just under 4%,
- top 10% D7 around 11–12%.
Regional Europe values are documented in `02_MARKET_RESEARCH.md`.

These benchmarks should be treated as context, not a universal genre-specific target.

## Unity architecture / 2D pipeline

### Unity 6.1 ScriptableObject manual
https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html

### Unity 6 Addressables manual
https://docs.unity3d.com/ja/6000.0/Manual/com.unity.addressables.html

### Unity 6 2D Aseprite Importer
https://docs.unity3d.com/kr/6000.0/Manual/com.unity.2d.aseprite.html

### Unity 6 2D Pixel Perfect
https://docs.unity3d.com/kr/6000.0/Manual/com.unity.2d.pixel-perfect.html

### Unity mobile optimization / object pooling background
https://docs.unity3d.com/kr/2018.3/Manual/MobileOptimizationPracticalScriptingOptimizations.html

Some Unity optimization pages are older documentation, so use them for durable principles (profiling, allocation, pooling), not device-specific numeric assumptions.

## Analytics

### Firebase Analytics
https://firebase.google.com/docs/analytics

Firebase documentation states Unity support and reporting for up to 500 distinct events.

## Monetization

### Unity — Rewarded ad systems
https://unity.com/blog/rewarded-ad-systems

### Unity — Monetization strategy
https://docs.unity.com/en-us/grow/ads/monetization-strategy

### Unity LevelPlay best practices
https://docs.unity.com/en-us/grow/levelplay/platform/best-practices/recommended

Used for:
- opt-in rewarded ads,
- retention/engagement/ARPDAU measurement,
- A/B testing,
- modern mediation/bidding guidance.

## Google Play

### Payments policy
https://support.google.com/googleplay/android-developer/answer/9858738

### Testing requirements for new personal accounts
https://support.google.com/googleplay/android-developer/answer/14151465

### Store listing experiments
https://support.google.com/googleplay/android-developer/answer/12053285

Used for:
- billing/randomized item requirements,
- 12 testers / 14 days requirement for applicable new personal accounts,
- store listing A/B testing.

## Apple

### App Review Guidelines
https://developer.apple.com/app-store/review/guidelines/

### Product Page Optimization
https://developer.apple.com/app-store/product-page-optimization/

### Custom Product Pages
https://developer.apple.com/app-store/custom-product-pages/

Used for:
- randomized item odds disclosure,
- store page testing,
- audience-specific product pages.

## Round 2 additions (2026-09-12)

Reference post-launch, mobile comparables, and 2026 market context. The full annotated list with what each source was used for is in `24_RESEARCH_ROUND_2_REFERENCE_AND_FUTURE.md`.

- Bones and Coins Wiki: https://bones-and-coins.wiki/
- Bones and Coins Steam reviews (JSON API): https://store.steampowered.com/appreviews/4134650?json=1&filter=all&language=english
- Wanderer review: https://playwanderer.online/game-reviews/bones-and-coins
- InsertCoins review: https://insertcoins.press/en/articles/bones-and-coins-test
- Game Developer, Finding the Fun: Archero: https://www.gamedeveloper.com/design/finding-the-fun-archero-part-1---gameplay
- Mobile Game Report, roguelite and idle RPG analyses 2026: https://www.mobilegamereport.com/
- Deconstructor of Fun, Legend of Mushroom: https://www.deconstructoroffun.com/blog/2024/4/15/the-magic-of-legend-of-mushroom
- Sensor Tower, State of Mobile 2026: https://sensortower.com/blog/state-of-mobile-2026
- Playio, retention benchmarks 2026: https://blog.playio.co/d1-d7-d30-retention-benchmarks-2026
- Unity 6 optimization guides: https://unity.com/blog/unity-6-game-optimization-guides

## Research discipline

Policies, SDKs and store requirements change. Re-check official documentation immediately before:
- monetization integration,
- closed/production release,
- privacy declarations,
- store submission.
