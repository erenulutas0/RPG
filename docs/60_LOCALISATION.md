# 60 - Localisation: Turkish and English (pass 1, 2026-11-18; pass 2, 2026-11-19; the Ascent, 2026-11-22)

The owner decided on 2026-11-18 that the game ships in Turkish and English (docs/59 decision 1): the game opens in the
phone's language and the language can be changed in the settings. This document is the guide: how the localisation
works, the glossary the owner reviews, the Turkish style rules, how to add a string, the culture rules for data, what
pass 2 did once the difficulty tiers had merged (section 7), and the run's name turned from the Descent to the Ascent
(section 10).

## 1. How it works

- **The table** is one tab-separated file, `Assets/_Project/Resources/Localisation/Strings.txt`, with the columns `key`,
  `en`, `tr` and `note` under a header row. Lines starting with `#` group it into sections. A line break inside a cell
  is written `\n`. It opens in a text editor, or in LibreOffice/Excel as tab-separated text (keep it UTF-8). **The
  owner reviews the `tr` column**; the `note` says where a string shows and what each placeholder is.
- **English stays at its source.** The code names each key beside its English
  (`Loc.Text("anvil.title", "The Anvil")`), and a content asset's text is its own field, looked up by a key made from
  its id (`EnemyDefinition.DisplayName` reads `enemy_forge_warden.name` with the asset's "Forge Warden" as the English).
  The table's `en` column is a copy of that English for the reviewer; the tests keep it equal to its source. So the
  English the game shows never depends on the table: in English it is byte for byte what it was before.
- **The lookup** (`Cryptforge.Localisation.Loc`): in Turkish, the table's Turkish, then the English given, then the
  table's English, then the key itself. A fallback is logged once per key. An empty English is an empty text in both
  languages.
- **Formatting** is plural-free, with indexed (`{0}`, `{1:0.0}`, as `string.Format` reads them) or named placeholders
  (`Loc.Format("anvil.need_gold", "Need {gold} more gold", ("gold", 300))`). A Turkish template that cannot be filled
  falls back to the English and says so; it never throws.
- **The language choice:** the first session takes the phone's system language (`Application.systemLanguage`: Turkish
  gives Turkish, anything else English). The player's choice is stored in PlayerPrefs (`cryptforge.language`, `en` or
  `tr`), apart from the profile save, whose version is untouched. In the Editor the system language is not asked (the
  Editor starts in English unless a choice was stored), and a batch run (the test runner, the captures) always starts
  in English; a capture names its language with `-Language tr`. The test suites pin English for their run.
- **The toggle** (`LanguageToggleView`) stands on the pause overlay under Resume and, since pass 2, in the map choice's
  title band at the game's start: "Language: English" / "Dil: Türkçe"; a tap switches and stores the choice. The open
  views write their words again at once (`Loc.Changed`: `LocalisedLabel` for fixed words; the forge, the Anvil, the hero
  chooser, the danger readout, the floor clock, the catch-up button, the map choice and the HUD refresh themselves).
  Text a scene made as it loaded - the level-up cards' names, the forge's cards - follows on the next load, which is the
  next run. A run started from the map choice after a switch loads the scene again first (`CombatSetup.SceneLanguage`),
  so the whole run speaks the language chosen.
- **Numbers shown to the player** are written in the chosen language's format on purpose: `Loc.Numbers` is the
  invariant culture for English ("12,345", "2.5") and a fixed Turkish format for Turkish ("12.345", "2,5"), built in
  code, never read from the phone. **Upper case for display** goes through `Loc.Upper`, which dots the Turkish capital
  i ("İSTİLA") and keeps rich-text tags.
- **Code:** the pure core is `Assets/_Project/Scripts/Localisation/` (`Loc`, `LocTable`, `LocFormat`, `LocText`,
  `RichText`, `ContentKeys`, `Language`); the Unity layer is its `Unity/` folder (`LocalisationRuntime`: boot, culture,
  table, choice; `LocalisedLabel`). Heroes are read through `Content/HeroText` (HeroDefinition keeps its fields), the
  difficulty tiers through `UI/TierText` (DifficultyTiers keeps the English the save, telemetry and the simulation's
  reports write; `TierTextTests` hold the two equal in English).

## 2. Keys

| Text | Key | Example |
|---|---|---|
| A view's words | `<area>.<thing>` | `anvil.strike.hold`, `settings.language`, `hud.tap_to_continue` |
| The game's own words (PrototypeTextDefinition) | `prototype.<field in snake_case>` | `prototype.pause_title` |
| A content asset's text | `<id>.name`, `<id>.description` | `enemy_forge_warden.name`, `weapon_daggers.description` (the forge card) |
| A hero's | `<id>.name`, `.subtitle`, `.role`, `.unlock_hint` | `hero_cinder_arcanist.unlock_hint` |
| A room (authored inline in its floor, no id) | `room.<English name as snake_case>.name` | "Captain's Post" is `room.captains_post.name` |
| The gear's and the loot's words | `gear.*`, `loot.*` | `gear.stat.crit_chance`, `loot.items` |
| A difficulty tier's name (its save id) and the tiers' words | `tier.<id>`, `tier.*` | `tier.tempered`, `tier.unlock_hint`, `tier.detail` |
| The map choice's and the result's own words | `map.*`, `result.*` | `map.flavour_2`, `result.loot_pieces` |

Development copies (`Data/Development`) and the enemies' own weapons are on no screen and have no rows; they fall back
to English.

## 3. Glossary (for the owner's review)

| English | Turkish | Why |
|---|---|---|
| basma (the Anvil's upgrade strike) | **basma** | Already the Turkish game word; "+8 bas", "basma tuttu / tutmadı" |
| efsun (a piece's lines) | **efsun** | Already the Turkish game word; a line is "efsun", the lines "efsunlar" |
| Anvil | Örs | The smith's anvil |
| strike held / missed | basma tuttu / tutmadı | The players' own words for a +N attempt |
| Anvil's memory (pity) | örsün hafızası | Keeps the English image; "hafıza" is plain Turkish |
| reroll | yenile / yenileme | Short on a button |
| inherit (move a level) | aktar | "Move" on the button is "aktar" too |
| melt | erit | |
| equip / equipped | kuşan / kuşanıldı | The RPG word |
| Bag | Çanta | |
| Forge (the place, the room, the tab) | Ocak | The smith's forge; also in Ocak Bekçisi, Ocak Mührü, Ocak Nabzı |
| Relic / Relic Forge | Kalıntı / Kalıntı Ocağı | The common RPG translation of "relic" |
| to forge (buy) a relic or weapon | dövmek ("60 altına döv") | Metal is forged: "dövülmeye hazır" |
| run (one Ascent) | sefer | "Sefere çık", "seferi bitir"; "koşu" reads as jogging |
| the Ascent (the whole climb, floor 1 to the Final Swarm) | **Tırmanış** | Since 2026-11-22 (section 10): the hero climbs the tower (docs/44 section 1), so the Descent / İniş of passes 1-2 became the Ascent / Tırmanış, the word the victory title already used ("Ascent complete" / "Tırmanış tamamlandı"). A proper noun in the game, so a suffix takes an apostrophe: "Tırmanış'ı tamamla", "Her Tırmanış'ın başladığı" |
| climb (on to the next floor) | tırman | Since 2026-11-22: a player's text never says "descend", "deeper" or "inmek"; a map above floor 1 is "a higher start" in the notes. The code's names (DescentStart, DescentSimulation, telemetry's "descend") stay |
| Extract / Climb (the checkpoint) | Çık / Tırman | Two short verbs; the title names both: "Durak: Çık ya da Tırman" |
| checkpoint | durak | |
| floor / room / wave | kat / oda / dalga | |
| gate / exit / next hall | kapı / çıkış / sonraki salon | |
| Outworks (docs/62's world before a floor's sanctum) | Dış Avlular | 2026-11-22, a working name (docs/62 section 11): the outer yards round the floor's heart. No P0a text shows it yet |
| Tithe Anvil (the capture shrine) | Öşür Örsü | 2026-11-22, working name: öşür is the old tithe, the tenth a harvest owed, as docs/62 section 1.4's lore has it. "ÖŞÜR ÖRSÜ - Halkanın içinde dur"; ÖRS on its edge arrow |
| Keeper (the roaming mini-boss) | Muhafız | 2026-11-22, working name: not Bekçi, which is the Warden's (Ocak Bekçisi). MUHAFIZ on its edge arrow; "Muhafızın anahtarıyla" (a common noun, no apostrophe) |
| the Bell | Çan | 2026-11-22, working name: "Çanın çalmasına 1:05", "ÇAN ÇALDI - Merdivene ulaş" |
| Ascent Stair / Stair Key | Tırmanış Merdiveni / Merdiven Anahtarı | 2026-11-22, working names. On screen the short MERDİVEN (the seal and the arrow): the full name would run half again the English |
| overrun | İSTİLA | The horde overruns the floor |
| danger | tehlike | |
| level / level up | seviye / seviye atladın | "Sv" when short ("Sv 2 -> 3") |
| XP | XP | Turkish players say "XP" ("XP kasmak"); "TP" (tecrübe puanı) is the formal option |
| HP (health) | Can | "120 / 150 Can"; "Azami can" for max health |
| upgrade (a level-up card) | güçlendirme | |
| tome | kitap | "YENİ KİTAP" |
| evolution | evrim | |
| crit | kritik | |
| elite | elit | The game word in Turkish |
| boss | boss | The game word in Turkish ("bosslardan") |
| armour / amulet / ring / weapon | zırh / tılsım / yüzük / silah | |
| Common / Rare / Epic / Gold | Sıradan / Nadir / Destansı / Altın | |
| Ember / Cinder / Ash | Kor / Kül / Kül | Ember is the glowing coal (Kor Parçası, Kor Salonu); Cinder and Ash are ash |
| Quicksilver | Cıva | |
| Gloam | Loş | Dim, dusk: Loş Bahçeler, Loş Sporu |
| Forge Seal / Master's Hammer / Efsun Scroll / Binding Rune | Ocak Mührü / Usta Çekici / Efsun Parşömeni / Bağlama Rünü | |
| pts (crit and armour points) | puan | |
| s (seconds) | sn | "4,5 sn", "her 0,80 sn'de" |
| difficulty tier ("on Forged") | zorluk ("Dövülmüş zorluğunda") | Pass 2. The suffix goes on "zorluk", never on the tier's name |
| Forged (tier 1) | **Dövülmüş** | Pass 2. Forged metal, as the glossary's "dövmek"; the tier line keeps the pun: "Tırmanış ilk dövüldüğü gibi" |
| Tempered (tier 2) | **Su Verilmiş** | Pass 2. Tempered steel is "su verilmiş çelik", the smith's step after forging. Not "Tavlanmış": "tavlamak" also means to con or sweet-talk someone, and "tavlama" is annealing, which softens the metal |
| Inferno (tier 3) | **Cehennem** | Pass 2 |
| Abyssal (tier 4) | **Uçurum** | Pass 2. The abyss ("aralarında uçurum var"), a noun beside Cehennem; "Dipsiz" (bottomless) is the alternative |
| Rewards x1.25 (a tier's) | Ödül x1,25 | Pass 2. Singular and short on the card |
| item luck | eşya şansı | Pass 2 |
| gear (a hero's gear power) | ekipman ("Ekipmanın: +%14,7") | Pass 2 |
| built for (the gear a tier is built for) | önerilen ("Önerilen ekipman +%15") | Pass 2. The "recommended power" of Turkish games |
| a +5 set / a +9 set | +5 set / +9 set | Pass 2. Turkish players say "set" |
| map (a starting floor) | harita ("Haritanı seç") | Pass 2 |
| Selected / Tap to choose / Locked | Seçildi / Seçmek için dokun / Kilitli | Pass 2 |
| choose N level-ups (a higher start) | N seviye seç | Pass 2, as the catch-up's own button says it ("{count} seviyeyi otomatik seç") |
| pieces (the result's bare loot count) | eşya ("12 eşya (Çanta'ya bak)") | Pass 2 |
| rewarded video / bonus (the result screen's offer) | ödüllü video / bonus ("Video izle: +60 altın", "Video oynatılamadı: bonus yok") | 2026-11-23, docs/58 7.8. "Bonus" is the Turkish game word |
| Skip reward videos (the store's one product) | Ödüllü videoları atla; owned: "Ödüllü videolar atlanıyor"; the owner's button "Videosuz al: +60 altın" (take it without a video) | 2026-11-23, docs/58 7.8 |
| skill (a hero's skills 1-3, docs/57) | yetenek ("Kahraman Yetenekleri", "Yetenekler: 2/3 hazır") | The Skills tab's word since docs/57 phase 4; since 2026-11-25 also the forge's skills header, the result's line and the first-run toast (docs/57 14.9). "{0}/{1}" in Turkish where English says "{0} of {1}": no suffix rides on a number |
| a skill opens (its floor's first clear) | yetenek açılır ("Yeni yetenek açıldı: Demir İrade. Kalıntı Ocağı'nda döv") | 2026-11-25, the result screen's line. The suffix goes on the fixed name with its apostrophe ("Kalıntı Ocağı'nda"), never on the skill's placeholder |
| the stick (the movement joystick) | kol ("kolun yönünde", "kolun yönüne hücum et") | Shield Rush's description and its toast |
| tap when ... (a skill's moment, the first-run toast) | ... dokun ("darbeler gelince dokun", "bossun darbesi inmeden hemen önce dokun") | 2026-11-25, docs/57 5.2. Lower case: the hint follows the skill's name ("Demir İrade: darbeler gelince dokun") |

**Proper names stay as they are:** Cryptforge, Ilya Kest, Vanguard (the lead, 2026-11-18). **Cinder Arcanist is kept
as a proper name in pass 1** for symmetry with Vanguard; if the owner prefers it translated, "Kül Büyücüsü" goes in its
`tr` cell.

**Descriptive names are translated:** enemies and bosses (Forge Warden → Ocak Bekçisi, Glass Regent → Cam Naibi,
Rootmother Myrr → Kök Ana Myrr - her name Myrr kept, as a proper name - Mirror Sentry → Ayna Nöbetçisi, Pollen Wraith → Polen Hortlağı, Grunt → Piyade, Grunt Captain → Piyade Kaptanı, Runner →
Koşucu, Cinder Mite → Kül Akarı, Cinder Bloom → Kül Çiçeği; Tank stays Tank, a game word), floors (The Foundry →
Dökümhane, Quicksilver Vaults → Cıva Mahzenleri, Gloam Gardens → Loş Bahçeler, The Final Swarm → Son Sürü), rooms,
items, weapons (Sword → Kılıç, Staff → Asa, Daggers → Hançerler), cards, tomes, relics and the abilities (Forge Burst →
Ocak Patlaması, Crucible Vent → Pota Ağzı).

## 4. Turkish style rules

1. **Talk to the player as "sen"**, in the imperative: "Seç", "Dokun", "Kuşan", "Sefere çık".
2. **Never put a suffix on a placeholder.** Turkish suffixes follow the word's vowels ("Kılıç'ı", "Asa'yı"), and a
   placeholder can be any name. Give the suffix to a fixed word after it - "{1} katını temizledi", "{item} eşyasının" -
   or use a postposition or the passive: "{2} içinde bir {1} karşısında düştü", "{1} yenildi". A suffix on a fixed word
   is fine ("+3'e kadar", "sn'de").
3. **The percent sign goes before the number:** "%25", "+%2,5". A format that multiplies by 100 moves its sign too:
   English `{0:0%}` is Turkish `{0:%0}`. A signed number keeps its sign in the template so the percent can stand between
   them: English `{1:+0.#}%` is Turkish `+%{1:0.#}` ("+%14,7", pass 2's tier line). The placeholder itself (its index or
   name) never changes.
4. **Never type a separator.** Decimal commas and thousands dots come from `Loc.Numbers`; a format keeps its pattern
   (`{1:0.0}`, `#,0`).
5. **No plural after a number:** "3 oda", "2 eşya". Where English needs one and another form, the code has two keys
   ("1 item" / "{count} items"), never a plural rule.
6. **Capitals keep their dots:** "SONRAKİ SALON", "YENİ SİLAH", "İSTİLA". An ordinal is a number and a dot: "1. kat".
7. **Fit:** Turkish runs 20-30 % longer. Keep a button within the English length plus a third; prefer the short word
   (kayıp over kaybedilen, Çık over Çıkış yap). TurkishScreensTests check that every label fits at its smallest
   best-fit size.
8. **Keep tags and placeholders:** `<size=34>`, `<color=...>` and every placeholder appear in the Turkish exactly as in
   the English, in any order.

## 5. How to add a string

- **In code:** write `Loc.Text("area.thing", "English")`, or `Loc.Format("area.thing", "English {name}", ("name", value))`,
  or `new LocText("area.thing", "English")` in a list. The English is a single literal. Then add a row to
  `Strings.txt`: the key, the same English (`\n` for a line break), the Turkish and a note. A view's fixed words go
  through `LocalisedLabel.Bind(label, () => ...)` so they follow a language change; a view showing live text refreshes
  on `Loc.Changed`.
- **In a content asset:** a new enemy, item, card, floor or room needs a row for `<id>.name` (and `<id>.description`
  where it has one) with the asset's English. A new definition type reads its text through
  `Loc.Text(ContentKeys.Key(_id, ContentKeys.Name), _displayName)`.
- **A new PrototypeTextDefinition field:** its getter is `Loc.Text("prototype.<field>", _field)` and it gets a row.
- **Numbers on screen** go through `Loc.Numbers` (`string.Format(Loc.Numbers, ...)`, `GearText.Gold`), never the
  invariant culture and never the thread's.
- **The checks** (`LocalisationTableTests`, also in Tools/CombatChecks; `LocalisationAssetTests`, EditMode) fail on: a
  row without English or Turkish; a key the code names that the table lacks; English that differs from the code's
  literal or the asset's field; a row nothing names (asset-derived keys count as named); placeholders that differ
  between English and Turkish; rich-text tags that are unbalanced or differ; different spacing at either end.

## 6. Culture safety (data never follows the phone)

A Turkish phone writes 1.5 as "1,5" and upper-cases "i" to "İ" in its own culture. The save, telemetry and the device
log lines are data and must read the same on every phone:

- `LocalisationRuntime` pins the invariant culture for the main thread and every later thread at boot
  (`RuntimeInitializeOnLoadMethod`, before anything loads). In the Editor the Editor's own culture comes back when play
  stops.
- Every data writer also names the invariant culture itself. The audit of 2026-11-18 found the data paths already
  explicit, with three implicit spots now made explicit: the telemetry build's `weapon_levels` and `tome_levels`
  (`StringBuilder.Append(int)`), and `development/horde-probe.txt`'s parse. The save goes through `JsonUtility`
  (native, culture-free) and holds only integers and ids; `gear_levels` and the reroll key were already invariant;
  the [Perf] and [RenderStats] lines are written with `FormattableString.Invariant` or digit by digit; Tools/Device and
  `DevicePull` parse with the invariant culture and culture-invariant regular expressions.
- `CultureSafetyTests` save a profile under the invariant culture, tr-TR and a culture with a Unicode minus sign and
  assert the bytes are identical; `RunTelemetryTests.TheLogIsByteIdenticalWhateverTheThreadsCulture` does the same for
  a full category run's log with gear.
- Upper and lower case for data use `ToUpperInvariant`/`ToLowerInvariant` (all of them already do); for display,
  `Loc.Upper`.

## 7. Pass 2 (done 2026-11-19)

Pass 1 left `MapChoiceView`, `RunHud`, `RunResultView` and `TierText` untouched (the tiers branch rewrote them), and
`CombatSetup`'s checkpoint numbers. Pass 2 converted them once the tiers had merged; English is byte for byte as before
(one deliberate exception below), and the 26 new rows sit in one block at the table's end ("Localisation pass 2").

**What moved into the table** (the four lines that named the Descent / İniş read as they do since 2026-11-22: the
Ascent / Tırmanış, section 10):

| Where | Keys | English | Turkish |
|---|---|---|---|
| TierText (the tier row, the HUD's floor line, the pause and result titles) | `tier.forged` ... `tier.abyssal` | Forged, Tempered, Inferno, Abyssal | Dövülmüş, Su Verilmiş, Cehennem, Uçurum |
| | `tier.unlock_hint` | Complete the Ascent on {0} | Tırmanış'ı {0} zorluğunda tamamla |
| | `tier.reward` | x{0:0.##} | x{0:0.##} ("x1,25") |
| MapChoiceView, the tier row | `tier.card_reward` | Rewards {0} | Ödül {0} |
| | `tier.detail_forged` | {0}: the Ascent as it was first forged; rewards x1\nBuilt for no gear. Your gear: {1:+0.#}% | {0}: Tırmanış ilk dövüldüğü gibi; ödül x1\nEkipman gerekmez. Ekipmanın: +%{1:0.#} |
| | `tier.detail` | {0}: foes {1:+0.#}% health, {2:+0.#}% damage; rewards {3}; item luck +{4:0}\nBuilt for gear {5:+0}% ({6}). Your gear: {7:+0.#}% | {0}: düşman canı +%{1:0.#}, hasarı +%{2:0.#}; ödül {3}; eşya şansı +{4:0}\nÖnerilen ekipman +%{5:0} ({6}). Ekipmanın: +%{7:0.#} |
| | `tier.set_tempered`, `_inferno`, `_abyssal` | a +5 set, a +9 set, +9 with good efsun | +5 set, +9 set, iyi efsunlu +9 |
| MapChoiceView | `map.title`, `map.title_with_heroes` | Choose your map / Choose your hero and map | Haritanı seç / Kahramanını ve haritanı seç |
| | `map.selected`, `map.tap_to_choose`, `map.locked` | Selected / Tap to choose / Locked | Seçildi / Seçmek için dokun / Kilitli |
| | `map.hero_weapon`, `map.hero_name` | Weapon: {0} / {0}\n<size=26>{1}</size> | Silah: {0} / (the same) |
| | `map.flavour_1` ... `_3` | the three maps' lines | Her Tırmanış'ın başladığı, lavla aydınlanan salonlar. Kapıyı Ocak Bekçisi tutar. / Donmuş cıva mahzenleri. Ayna Nöbetçileri darbelerini sana geri çevirir. / Küllerin üstünde büyümüş bir alacakaranlık bahçesi. Son Sürü'den önceki son kat. |
| | `map.start_catch_up`, `map.start_fresh` | Floor {0}. Start at level {1}: choose {1} level-ups first. / Floor {0}. A fresh hero, the whole Ascent ahead. | Kat {0}. {1}. seviyeden başla: önce {1} seviye seç. / Kat {0}. Taze bir kahraman, önünde koca bir Tırmanış. |
| RunResultView, the loot line | `result.loot_nothing`, `result.loot_pieces` | nothing / {0} pieces (see the Bag) | hiçbir şey / {0} eşya (Çanta'ya bak) |

The tiers added no content asset (no new `<id>` rows); the four hero skills' rows came with the skills (3f45838b).

**And beside the rows:**

- **Hero words** in the map choice (name, subtitle, role, unlock hint), the HUD's health line and the result's cause
  line go through `HeroText`.
- **Numbers:** every `string.Format(CultureInfo.InvariantCulture, ...)` of these views, and `CombatSetup.CheckpointOptions`'
  (Extract and Climb), now formats with `Loc.Numbers`. The tier line's numbers moved from pre-formatted strings into the
  template's own formats, so the Turkish can put the percent sign between the sign and the number ("+%8", "+%14,7").
- **The HUD's compact gold** is grouped in the language's format, as the forge's gold is (`GearText.Gold`): "1,250" in
  English, "1.250" in Turkish. **The one English change of pass 2**: a run's gold of 1,000 or more now reads "1,250"
  where it read "1250"; below 1,000 it is as before (no English test or capture reaches 1,000).
- **Labels that follow a switch:** the result's two buttons and the pause readout's title (with its tier) are bound with
  `LocalisedLabel`; the HUD writes its lines again on `Loc.Changed` (the pause toggle shows the readout); the map choice
  writes every card again.
- **The language toggle on the map choice** (the lead's decision: both homes). It stands in the title's band at its right
  (260 x 84), the title taking the band's left, so the panel keeps its 1900 units and its 99 % fit on a 16:9 phone. It
  waits out the panel's input delay and goes grey once a map is tapped. A map tapped after a switch reloads the scene
  (`CombatSetup.SceneLanguage`, `StartOnMap`), as another hero or tier does, so the level-up cards and the forge made as
  the scene loaded speak the chosen language too; switching back before the tap changes nothing.
- **Captures:** `Run-TierUiProof.ps1 -Language tr` draws the tier row, the HUD's floor line and the result's title in
  Turkish into `tier-ui-01-tr`.

**Tests:** `TierTextTests` (EditMode: TierText's English equals DifficultyTiers'; the tiers', the tier line's and the map
lines' English byte for byte as the tiers branch wrote them; the Turkish with "+%14,7", "x1,25" and no fallback);
`TurkishTierScreensTests` (PlayMode: the map choice with every tier, Abyssal locked, a locked hero and locked maps; the
HUD, pause and result of an Inferno run; every label fitting at its smallest best-fit size on the 1760 and 2232 safe
areas; the toggle on the map choice switching at once, storing the choice and reloading for the run); the table's own
checks cover the new keys.

**Noticed, not changed:** the stamina bar's word "HAREKET" (pass 1, `hud.stamina_label`) is wider than its 94-unit label
at 22 (Arial measures it about 104) and has no best fit; the bar is off today (`HeroStamina.CurrentRulesEnabled`), so
nothing shows it, but when it turns on the word wants a shorter Turkish ("ADIM"?) or a wider label.

## 8. Fonts

Every label uses Unity's built-in `LegacyRuntime.ttf`: the 54 scene Text components, the labels built in code (which copy
a scene label's font or fall back to it), the world TextMesh labels (exit seal, altar) and the gate arrival's IMGUI hint.
It is a dynamic font with no glyphs of its own (its asset names "Arial" and embeds no font data): it draws from the
system's fonts - Arial in the Windows Editor, Roboto/Noto on Android - all of which carry ş ğ ı İ ç ö ü Ş Ğ Ç Ö Ü. There
is no custom, bitmap or TextMesh Pro font in the project; the painted damage numbers are digits only. `TextPrewarm`
warms the glyphs of the language the scene loads in. The one thing to watch is metrics: the phone draws with Roboto, so
the Turkish fits measured in the Editor want a look on the S23 too.

## 9. Open questions for the owner

1. Hero names: keep "Cinder Arcanist" (and "Vanguard") as names, or "Kül Büyücüsü"?
2. "XP" or "TP"? "Can" for HP?
3. "Sefer" for a run ("Sefere çık", "seferi bitir")?
4. "Kalıntı" for relic, "Tılsım" for amulet, "Ocak" for forge?
5. The percent sign before the number ("%25") everywhere, including the Anvil's big chance?

Pass 2's (2026-11-19):

6. The tiers: Dövülmüş, **Su Verilmiş** (rather than the suggested Tavlanmış, whose "tavlamak" also means to con someone),
   Cehennem, **Uçurum** - or "Dipsiz" (bottomless) for the deepest?
7. "{0} zorluğunda" for "on Forged" ("Tırmanış'ı Dövülmüş zorluğunda tamamla"), or "kademe" ("Dövülmüş kademesinde")?
8. The tier line: "Önerilen ekipman +%15 (+5 set). Ekipmanın: +%14,7" - "önerilen" for "built for", "set" for a set of
   gear?
9. "Ödül x1,25" on the tier card: singular "Ödül", the multiplier's "x" and the decimal comma?
10. A higher start says "önce 31 seviye seç", as the catch-up's button says "seviyeyi otomatik seç"; "güçlendirme seç"
    (the level-up card's own word) is the alternative for both.

2026-11-22's (section 10):

11. "Tırmanış" for the Ascent, as the victory title already says it ("Tırmanış tamamlandı")? "Yükseliş" is the
    alternative, but it also reads as a rise in rank or prices.

2026-11-23's (the rewarded offer, docs/58 7.8):

12. The skip's owner's button says "Videosuz al: +60 altın" (take it without a video) where the English says "Skip owned:
    +60 gold"; the pause overlay's entry, once bought, "Ödüllü videolar atlanıyor". Literal alternatives: "Atlama sende:
    +60 altın", "Ödüllü videoları atla: satın alındı" (a third longer than the English).

## 10. The Ascent (2026-11-22)

**The decision.** The fiction is a climb up the tower: the hero always climbs (docs/44 section 1, "oyuncu aşağı inmez;
hep yukarı tırmanır"; the owner decided it on 2026-10-20 and confirmed it on 2026-11-22). The player's text still
called the run "the Descent" / "İniş" in four lines. They now say **the Ascent** / **Tırmanış**.

**Why "the Ascent" and not "the Climb":**

- The victory title already says it: "Ascent complete" / "Tırmanış tamamlandı" (`prototype.result_victory_title`), so
  the run's name and the title that ends it now agree.
- "Climb" is the checkpoint's verb ("Checkpoint: extract or climb?", the card "Climb", "GATE - Stand still to climb");
  a noun "the Climb" beside the button "Climb" would read as the button's name.
- A name, as the Descent was, one letter shorter in English (no fit moves), and it echoes the lore's Ascendant Kiln
  (docs/44).

**Turkish.** "Tırmanış" is the noun of the checkpoint's own "Tırman". It stays a proper noun, so a suffix takes an
apostrophe and follows its last vowel (ı, back and unrounded) and its last letter (ş, voiceless): **Tırmanış'ı**
(accusative), **Tırmanış'ın** (genitive), Tırmanış'a, Tırmanış'ta, Tırmanış'tan. Style rule 2 still holds: the suffix
rides on the fixed word, never on a placeholder.

| Key | English (was) | English | Turkish (was) | Turkish |
|---|---|---|---|---|
| `tier.unlock_hint` | Complete the Descent on {0} | Complete the Ascent on {0} | İniş'i {0} zorluğunda tamamla | Tırmanış'ı {0} zorluğunda tamamla |
| `tier.detail_forged` | {0}: the Descent as it was first forged; rewards x1\n... | {0}: the Ascent as it was first forged; rewards x1\n... | {0}: İniş ilk dövüldüğü gibi; ödül x1\n... | {0}: Tırmanış ilk dövüldüğü gibi; ödül x1\n... |
| `map.flavour_1` | Lava-lit halls where every Descent begins. The Forge Warden keeps the gate. | Lava-lit halls where every Ascent begins. The Forge Warden keeps the gate. | Her İniş'in başladığı, lavla aydınlanan salonlar. Kapıyı Ocak Bekçisi tutar. | Her Tırmanış'ın başladığı, lavla aydınlanan salonlar. Kapıyı Ocak Bekçisi tutar. |
| `map.start_fresh` | Floor {0}. A fresh hero, the whole Descent ahead. | Floor {0}. A fresh hero, the whole Ascent ahead. | Kat {0}. Taze bir kahraman, önünde koca bir İniş. | Kat {0}. Taze bir kahraman, önünde koca bir Tırmanış. |

No other player's text named a descent: the checkpoint already said "Climb" / "Tırman", the gate "Stand still to
climb", the forge "Highest floor cleared", the arena ladder "as you climb". "The Deep Forge" (a floor 2 room) and "Deep
Lungs" (a card) are names of their own, not the direction. The table's notes now say "a higher start" for a map above
floor 1 (the catch-up's notes) and "highest floor cleared" (the forge's), as the English already did.

**What stays.** Every key (`prototype.descend_name` and its kin), the code's names (`DescentStart`, `DescentSimulation`,
`RecordDescentCompleted`, `FOUNDRY_*`), the save's fields and the telemetry's events and values ("descend",
`floor_descend`), the research reports and the dated docs. `DifficultyTiers.UnlockHintFormat`, the English reference
`TierTextTests` hold TierText to, says "Complete the Ascent on {0}" too; nothing writes it to the save or telemetry.

**The store drafts** say it too: the listings' first paragraph reads "As you climb floor by floor" / "Kat kat yukarı
tırmanırken" where it read "Deep in the forge" / "Ocağın derinliklerinde" (docs/store; the counts there are updated).

**Fit.** The longest Turkish line that changed is a locked tier's state, "Kilitli: Tırmanış'ı Su Verilmiş zorluğunda
tamamla", four letters longer than before: in its 196 x 84 label it takes three lines at 22 and two at its smallest
best-fit size, 14 (Arial's metrics); `TurkishTierScreensTests` check it in the Editor.

**Tests.** `DifficultyTierTests` and `TierTextTests` (EditMode), `TierChoiceFlowTests` and `TurkishTierScreensTests`
(PlayMode) assert the new English and Turkish; `TurkishTierScreensTests`' list of English that must not show in Turkish
names "Ascent". `LocalisationTableTests` hold the table's English equal to the code's.

## 11. The bosses' barks (2026-11-23)

docs/44 section 1.8's spawn, telegraph and death lines for the two gate bosses that ship (docs/64 stage 1), shown as a
short plate over the boss (`BossBark`, `BossBarkView`). Six rows under `bark.<boss>.<moment>`, the English docs/44's
word for word. The Turkish keeps each line as short as its English or shorter, in the boss's own voice, and talks to the
hero as "sen" (style rule 1):

| Key | English | Turkish | Why |
|---|---|---|---|
| `bark.forge_warden.spawn` | Shift's not over. | Vardiya bitmedi. | The foreman's word: the Warden sealed a shift in (docs/44 1.5, year -7) |
| `bark.forge_warden.telegraph` | Stand clear of the mark. | İşaretten uzak dur. | An order; "işaret" is the mark on the floor |
| `bark.forge_warden.death` | Door... was never locked for you. | Kapı... sana hiç kilitli değildi. | The pause kept where the English has it |
| `bark.glass_regent.spawn` | Which one of you is original? | Hanginiz asıl? | "Asıl": the original against its copy; two words, as cold as the archivist |
| `bark.glass_regent.telegraph` | Hold still. I need your face. | Kıpırdama. Yüzün lazım. | "Lazım" over "ihtiyacım var": shorter and colder |
| `bark.glass_regent.death` | I kept the wrong one... | Yanlış olanı sakladım... | |

**Fit.** The plate is at most 640 of the HUD's 1080 canvas units wide; a line sits at 34 and fits down to 24, on one line
or two. Every line takes one line at 34: measured in Arial italic (PIL, Windows' `ariali.ttf`), the longest Turkish (the
Warden's death line) runs about 430 units and the longest English 494, inside the plate's 588 between its paddings;
Myrr's longest (docs/64, "Something... is growing underneath.") 543.
`BossBarkFlowTests.EveryLineFitsItsPlateInTurkish` lays every line in Turkish and checks it with `TurkishScreensTests`'
fit check; the phone draws Roboto, so the S23 look is the owner's (section 8).

Rootmother Myrr's three lines got their rows with her scene build (2026-11-25, docs/64 section 16), the Turkish docs/64
section 1's drafts as the lead answered them (section 13, answer 7), with her name's row:

| Key | English | Turkish | Why |
|---|---|---|---|
| `enemy_rootmother_myrr.name` | Rootmother Myrr | Kök Ana Myrr | "Kök Ana", the root mother; Myrr is her name and stays (for the owner's review) |
| `bark.rootmother_myrr.spawn` | Your tomorrow can still take root. | Yarının hâlâ kök salabilir. | She would keep the hero's future as she kept the others: by planting it |
| `bark.rootmother_myrr.telegraph` | Be still. Be planted. | Kımıldama. Kök sal. | "Kımıldama", not the Regent's "Kıpırdama": the two telegraphs open with different words |
| `bark.rootmother_myrr.death` | Something... is growing underneath. | Altta... bir şey büyüyor. | The pause kept; the floor's open question |

Her death line is the longest English of the nine, still one line inside the plate's 588 between its paddings (about 543
in Arial italic, above); `BossBarkFlowTests.EveryLineFitsItsPlateInTurkish` reads every line of
`BossBark.Lines`, hers among them. The room's row, `room.wardens_grove.name` ("Warden's Grove" / "Bekçinin Korusu"),
stays as it is until the owner's review of this table: the lead's answer 6 renames it then (docs/64 section 16).
