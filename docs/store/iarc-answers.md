# Play Console: the content rating questionnaire (IARC), answered (draft, 2026-11-19)

For Play Console > Policy > App content > Content rating. The questionnaire's exact wording is only visible inside
Play Console and changes over time, so this file answers the **facts** each section asks about, with the evidence; the
owner picks the matching option. Google: "Misrepresentation of your app's content may result in removal or
suspension"; ads are not rated, but an ad must not be "significantly more mature" than the app
([content ratings](https://support.google.com/googleplay/android-developer/answer/9898843)). A new questionnaire is
due whenever a change would alter an answer (section 4).

What was checked for this file: every enemy, hero, floor, item and card in `Assets/_Project/Data` (83 assets), the
string table `Assets/_Project/Resources/Localisation/Strings.txt` (every word the player reads, in both languages), the
characters' and effects' sprites under `Assets/_Project/Art/Characters` and `Assets/_Project/Art/Effects`, and docs/08's
art rule ("no skulls or skeletal faces on enemies or glyphs").

## 1. The game in one paragraph (for the "describe your app" field, if asked)

A single-player, portrait, isometric action roguelite. A human hero (a knight or a mage) walks through fantasy floors
while a crowd of creatures closes in; the hero attacks automatically, the player steers with one thumb and fires
skills with the other. The enemies are golems of stone, lava and bark, a lava mite, a mirror sentry, a pale floating
wraith and the Glass Regent (a faceless, robed figure of glass). Between runs the player upgrades gear at an anvil with
a shown chance of success, paid with gold earned in play. No chat, no accounts, no online play; one optional rewarded
video and one paid item, "Skip reward videos".

## 2. Answers

**Category: Game.** (The other categories - social or communication, reference/news/education, entertainment,
consumer store or streaming, utility or productivity - do not fit.)

### Violence

| Fact asked about | Answer | Evidence |
|---|---|---|
| Does the game contain violence? | **Yes** | Combat is the game: Sword, Staff (fire) and Daggers; enemies strike the hero |
| Against whom | **Fantasy creatures, none of them human.** The player's heroes are human-looking (an armoured knight, a robed mage) and are struck by the enemies; the player never attacks a human | Enemies: Grunt / Grunt Captain / Tank / Runner (stone-and-lava golems), Cinder Mite (a lump of lava rock), Cinder Bloom (a bark golem), Forge Warden (an armoured golem with a hammer), Mirror Sentry (a mirror on legs), Pollen Wraith (a pale drifting mote), Glass Regent (a faceless figure of glass in a robe, humanoid in outline). Sprites: `Art/Characters/*` |
| Realism | **Not realistic.** Small stylised figures seen from above, painted look, numbers float over hits | `Art/Effects`: sparks, slash arcs, smoke puffs, damage digits, rings |
| Blood, gore, dismemberment | **None** | No blood sprite or particle exists; a defeated enemy topples or crumbles and fades in a smoke puff (`death-collapse`, `fall-topple` frames) |
| Burning | **Yes, stylised:** the mage's hits set enemies alight (flames on the creature, a burn number); a lava pit can erupt under the hero ("burnt by the pit's eruption") | `hero_cinder_arcanist.role`, `prototype.result_pit_cause_format` |
| The hero's defeat | **Text only:** the run ends with "Defeated - Vanguard fell to a Grunt in Ember Hall"; the heroes have no death frames in the current build, and no body is shown | `prototype.result_defeat_*`; `Art/Characters/Vanguard`, `CinderArcanist` |
| Violence against animals | **No** (no animals) | |
| Violence rewarded | **Yes**, as in any action game: defeated enemies drop experience and gold | |

### Fear and horror

**No.** No horror theme, jump scares, gore or threatening imagery; docs/08 forbids skulls and skeletal faces. The
names "Pollen Wraith" (Polen Hortlağı) and "Cursed Gold" (Lanetli Altın, a floor rule: +20 % enemy damage, +50 % gold)
are flavour; the wraith is a small pale mote. If the questionnaire asks about "scenes that may frighten young
children", the honest answer is no; the owner may answer yes out of caution (it would not raise the expected rating
above 7, section 3).

### Sexuality and nudity

**No.** None of any kind; the characters are armoured or robed.

### Language

**No** profanity, slurs, crude or sexual language in either language (the full string table was read).

### Controlled substances

**No** drugs, alcohol or tobacco, shown, named or implied.

### Crude humour

**No.**

### Gambling

| Fact asked about | Answer | Evidence |
|---|---|---|
| Real-money gambling | **No** | |
| Simulated gambling (casino games, betting, slot machines) | **No.** The Anvil's basma (upgrade strike) has a chance of success shown on the button; it is paid with gold and materials earned in play, never with money; a missed strike keeps the item and its level, and every miss fills the "anvil's memory" until a strike is certain. No casino imagery, no betting, no wagering of anything bought | docs/54; `anvil.*` strings; docs/58 section 1 (boosters, gold, materials never sold) |
| Paid random items (loot boxes, gacha) | **No.** Drops in a run are random but cannot be bought; the one paid item is fixed and always does the same thing | docs/58 section 1 |

### Other content

Discrimination, hate, real-world violence, real people: **No.**

### Interactive elements (the "miscellaneous" questions)

| Fact asked about | Answer | Why |
|---|---|---|
| Can users interact or communicate (chat, messages, voice, shared content)? | **No** | No chat, no friends, no user-generated content. The first release's arena is local: rivals generated on the phone and the player's own past heroes (docs/55 section 9, "the release order") |
| Does the app share the user's current physical location with other users? | **No** | No location feature |
| Does the app allow purchase of digital goods? / In-game purchases | **Yes** | One non-consumable, "Skip reward videos" (docs/58). Not random, not time-limited, no virtual currency |
| Does the app share personal information with third parties? (asked by some versions) | **Yes** | The ad SDK sends device identifiers, IP address and interactions to Google, which uses them as an independent controller (data-safety-answers.md, the "Shared" column) |
| Unrestricted internet access (a browser or search) | **No** | No web view or browser. A tap on an ad's own link may open the phone's browser; that is the ad, not a browser in the app |
| Does the app contain ads? | Declared separately in **App content > Ads: Yes** | Not part of the rating |

## 3. Expected rating, and why

| Authority (region) | Expected | Why | Plausible alternative |
|---|---|---|---|
| **PEGI** (Europe; Google Play lists **Turkey** under PEGI) | **PEGI 7**, descriptor Violence; In-game purchases | PEGI 7 accepts "very mild forms of violence (implied, non-detailed, or non-realistic violence)"; ours is non-realistic and non-detailed, against fantasy creatures, with no blood | **PEGI 12** if the answers about the human heroes being struck (and the Glass Regent's human outline) weigh as "non-realistic violence towards human-like characters", which PEGI places at 12 |
| **ESRB** (Americas) | **Everyone 10+**, Fantasy Violence; In-Game Purchases | Constant combat against fantasy creatures | **Everyone**, Mild Fantasy Violence |
| **USK** (Germany) | **6** | Abstract, non-decisive violent imagery | 12 |
| **IARC generic** (countries without an authority) | **7+** | Mild, non-realistic violence | 12+ |

Interactive labels that should appear: In-game purchases; possibly "Shares info" (or its local equivalent) from the
ad SDK.

**PEGI's interactive-risk criteria** (for games submitted from June 2026, per PEGI's
[announcement of 12 March 2026](https://pegi.info/news/pegi-expands-age-rating-criteria-interactive-risk-categories))
raise none of these minimums for us:

- paid random items (PEGI 16): none;
- time- or quantity-limited offers (PEGI 12): none;
- rewards for coming back such as daily quests (PEGI 7): none today;
- punishing absence (PEGI 12): none;
- unrestricted communication (PEGI 18): none;
- NFTs or blockchain (PEGI 18): none.

That announcement does not say when or how the criteria reach IARC ratings on mobile stores, so this is a reading,
not a promise.

The rating is not the target audience: PEGI 7 says the content suits 7-year-olds, while the target audience (who the
game is made for) is a separate declaration with its own rules (data-safety-answers.md section 4; README question 6).

## 4. Changes that need a new questionnaire

| Change | Likely effect |
|---|---|
| Any paid random item (a chest, a pack, a paid reroll with random results) | PEGI 16 under the June 2026 criteria; the paid odds must also be shown before purchase (Google Play's payments policy) |
| A limited-time or limited-quantity offer, a countdown on a product | PEGI 12 minimum |
| Daily rewards or quests | PEGI 7 minimum (no change for us); a penalty for missing days: PEGI 12 |
| Online ranked arena against other players' heroes (docs/55 P1-P2), with no chat | Re-answer the interaction questions; asynchronous play with no communication should not change the content rating |
| Any chat or player names shown to others | Re-answer; unrestricted chat is PEGI 18 |
| Blood, a gorier death, a human enemy | Re-answer the violence section; likely 12 or more |
| Sanguine / "Blood Pact" (docs/40, docs/44: a future hero whose skills spend health) | Re-check the violence and blood answers when it is built |

## Sources (read 2026-11-19)

- Google Play, content ratings for developers: <https://support.google.com/googleplay/android-developer/answer/9898843>
- Google Play, rating authorities by region (Turkey under PEGI; IARC generic elsewhere): <https://support.google.com/googleplay/android-developer/answer/188189>
- Google Play, what each rating means (ESRB, PEGI, USK, IARC generic): <https://support.google.com/googleplay/answer/6209544>
- PEGI, what the labels mean: <https://pegi.info/what-do-the-labels-mean>
- PEGI, interactive risk criteria (12 March 2026): <https://pegi.info/news/pegi-expands-age-rating-criteria-interactive-risk-categories>
- The owner's release report, section 1 ("IARC ve hedef kitle"): `docs/research/2026-11-18_release_path_owner_report.md`
