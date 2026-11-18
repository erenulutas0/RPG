# 60 - Localisation: Turkish and English (pass 1, 2026-11-18)

The owner decided on 2026-11-18 that the game ships in Turkish and English (docs/59 decision 1): the game opens in the
phone's language and the language can be changed in the settings. This document is the guide: how the localisation
works, the glossary the owner reviews, the Turkish style rules, how to add a string, the culture rules for data, and
what pass 2 does once the difficulty tiers have merged.

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
- **The toggle** (`LanguageToggleView`) stands on the pause overlay under Resume: "Language: English" / "Dil: Türkçe";
  a tap switches and stores the choice. The open views write their words again at once (`Loc.Changed`:
  `LocalisedLabel` for fixed words; the forge, the Anvil, the hero chooser, the danger readout, the floor clock and the
  catch-up button refresh themselves). Text a scene made as it loaded - the level-up cards' names, the forge's cards,
  the views not yet converted (section 7) - follows on the next load, which is the next run.
- **Numbers shown to the player** are written in the chosen language's format on purpose: `Loc.Numbers` is the
  invariant culture for English ("12,345", "2.5") and a fixed Turkish format for Turkish ("12.345", "2,5"), built in
  code, never read from the phone. **Upper case for display** goes through `Loc.Upper`, which dots the Turkish capital
  i ("İSTİLA") and keeps rich-text tags.
- **Code:** the pure core is `Assets/_Project/Scripts/Localisation/` (`Loc`, `LocTable`, `LocFormat`, `LocText`,
  `RichText`, `ContentKeys`, `Language`); the Unity layer is its `Unity/` folder (`LocalisationRuntime`: boot, culture,
  table, choice; `LocalisedLabel`). Heroes are read through `Content/HeroText` (HeroDefinition keeps its fields).

## 2. Keys

| Text | Key | Example |
|---|---|---|
| A view's words | `<area>.<thing>` | `anvil.strike.hold`, `settings.language`, `hud.tap_to_continue` |
| The game's own words (PrototypeTextDefinition) | `prototype.<field in snake_case>` | `prototype.pause_title` |
| A content asset's text | `<id>.name`, `<id>.description` | `enemy_forge_warden.name`, `weapon_daggers.description` (the forge card) |
| A hero's | `<id>.name`, `.subtitle`, `.role`, `.unlock_hint` | `hero_cinder_arcanist.unlock_hint` |
| A room (authored inline in its floor, no id) | `room.<English name as snake_case>.name` | "Captain's Post" is `room.captains_post.name` |
| The gear's and the loot's words | `gear.*`, `loot.*` | `gear.stat.crit_chance`, `loot.items` |

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
| run (one Descent) | sefer | "Sefere çık", "seferi bitir"; "koşu" reads as jogging |
| Descent | İniş (pass 2, the map choice) | |
| Extract / Climb (the checkpoint) | Çık / Tırman | Two short verbs; the title names both: "Durak: Çık ya da Tırman" |
| checkpoint | durak | |
| floor / room / wave | kat / oda / dalga | |
| gate / exit / next hall | kapı / çıkış / sonraki salon | |
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

**Proper names stay as they are:** Cryptforge, Ilya Kest, Vanguard (the lead, 2026-11-18). **Cinder Arcanist is kept
as a proper name in pass 1** for symmetry with Vanguard; if the owner prefers it translated, "Kül Büyücüsü" goes in its
`tr` cell.

**Descriptive names are translated:** enemies and bosses (Forge Warden → Ocak Bekçisi, Glass Regent → Cam Naibi, Mirror
Sentry → Ayna Nöbetçisi, Pollen Wraith → Polen Hortlağı, Grunt → Piyade, Grunt Captain → Piyade Kaptanı, Runner →
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
   English `{0:0%}` is Turkish `{0:%0}`. The placeholder itself (its index or name) never changes.
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

## 7. Pass 2 (after the difficulty tiers merge)

Pass 1 left `MapChoiceView`, `RunHud`, `RunResultView` and `TierText` untouched (the tiers branch rewrites them), and
`CombatSetup`. Their words that come from PrototypeTextDefinition and from the content are already Turkish; their own
literals, their hero words and their number formats are pass 2's:

| File | English | Proposed Turkish |
|---|---|---|
| MapChoiceView | Choose your map | Haritanı seç |
| | Choose your hero and map | Kahramanını ve haritanı seç |
| | Selected | Seçildi |
| | Tap to choose | Seçmek için dokun |
| | Locked | Kilitli |
| | Weapon: {0} | Silah: {0} |
| | {0}\n<size=26>{1}</size> | (the same) |
| | Lava-lit halls where every Descent begins. The Forge Warden keeps the gate. | Her inişin başladığı, lavla aydınlanan salonlar. Kapıyı Ocak Bekçisi tutar. |
| | Frozen vaults of quicksilver. Mirror Sentries turn your blows back on you. | Donmuş cıva mahzenleri. Ayna Nöbetçileri darbelerini sana geri çevirir. |
| | A dusk garden grown over the ash. The last floor before the Final Swarm. | Küllerin üstünde büyümüş bir alacakaranlık bahçesi. Son Sürü'den önceki son kat. |
| | Floor {0}. Start at level {1}: choose {1} level-ups first. | Kat {0}. {1}. seviyeden başla: önce {1} seviye seç. |
| | Floor {0}. A fresh hero, the whole Descent ahead. | Kat {0}. Taze bir kahraman, önünde koca bir İniş. |
| RunResultView | nothing (the loot line beside a loss) | hiçbir şey |
| | {0} pieces (see the Bag) | {0} eşya (Çanta'ya bak) |

Also in pass 2: the hero's words in these views through `HeroText` (name, subtitle, role, unlock hint); their
`string.Format(CultureInfo.InvariantCulture, ...)` calls to `Loc.Numbers`; `RunHud`'s compact gold to a grouped number;
`LocalisedLabel` on `RunResultView`'s two buttons; the language toggle on the map choice's screen (the game's start);
and every string `TierText` and the tiers add.

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
