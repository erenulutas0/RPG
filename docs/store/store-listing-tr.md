# Store listing, Turkish (draft, 2026-11-19)

The Turkish Play listing, the default for the Turkish release (docs/59). The copy follows docs/60: "sen" and the
imperative, the glossary (sefer, Örs, basma, efsun, eşya, Ocak, Kalıntı, the floors' and enemies' Turkish names, the
proper names Cryptforge, Vanguard, Ilya Kest and Cinder Arcanist kept), no plural after a number, the dotted capitals,
a decimal comma ("1,5"). Counts are Unicode characters, line breaks included, measured with Python's `len` on the exact
text of each block; Play's limits apply to full- and half-width characters alike
([store listing limits](https://support.google.com/googleplay/android-developer/answer/9859152): name 30, short
description 80, full description 4,000).

Google's metadata rules that shaped the copy
([preview assets](https://support.google.com/googleplay/android-developer/answer/9866151)): no "en iyi", "#1", "yeni",
rankings, prices or "hemen indir"; no capitals for emphasis; no emojis or special characters in the short description;
no keyword stuffing; no competitors' names (the owner's report also says so).

## 1. App name (30 at most)

| Option | Count | Note |
|---|---|---|
| **Cryptforge** (recommended) | 10 | The proper name (docs/60). Clean, and the same in both languages |
| Cryptforge: Sürü ve Örs | 23 | Adds the two halves of the game; "sürü" is a search word |

## 2. Short description (80 at most)

The owner's report drafted "Tek elle sürülere karşı dayan, koşu arasında silahını +9'a geliştir." The game is
**two-handed** (docs/57 section 12: left thumb moves, right thumb presses), "koşu" is "sefer" in the glossary, and the
Anvil strikes every piece, not only the weapon. Corrected:

| Option | Count |
|---|---|
| **Sürülere karşı dayan, seferler arasında eşyanı +9'a kadar bas. Eşyan kırılmaz.** (recommended) | 78 |
| Sürülere karşı dayan, ganimeti topla, eşyanı kırılma korkusu olmadan +9'a bas. | 78 |
| Dikey ekranda sürülere karşı dayan, seferler arasında eşyanı +9'a kadar bas. | 76 |

## 3. Full description (4,000 at most)

**Counts:** the base text below is **2,121** characters. Two paragraphs are added only if their feature ships in the
release: the skills paragraph (+117) and the arena paragraph (+157). With both: **2,395**.

### 3.1 Base text

```text
Cryptforge, dikey ekranda oynanan, tek oyunculu bir aksiyon roguelite oyunu. Ocağın derinliklerinde sürüler üstüne gelir: sen dayanır, güçlenir, ganimetini sağ salim çıkarmaya çalışırsın. Sonra eşyanı Örs'te +9'a kadar basarsın. Eşyan asla kırılmaz.

Sefer
- Kahramanın kendi saldırır. Sol başparmağınla yönünü seçer, sağ başparmağınla yeteneğini kullanırsın. Oyun iki elle oynanır.
- Dökümhane'nin lavla aydınlanan salonlarından başla, Cıva Mahzenleri'nden ve Loş Bahçeler'den geç, sonunda Son Sürü'yle yüzleş.
- Her seviye atlayışında kartlar arasından güçlendirmeni seç: yeni silahlar, kitaplar, evrimler. Kılıç, Asa ve Hançerler ile kendi yapını kur.
- Katların kapısını bosslar tutar; aralarında Ocak Bekçisi ve Cam Naibi var.
- Durakta karar ver: altınını kasaya koyup çık ya da bir sonraki kata tırman. Düşersen o katın altınının bir kısmı gider; düşen eşyalar ise her zaman seninle kalır.

Örs, basma ve efsun
- Bosslardan, elitlerden ve çıkıştan eşya düşer: silah, zırh, tılsım, yüzük. Sıradan'dan Altın'a dört nadirlik.
- Eşyanı Örs'te +9'a kadar bas. Başarı şansı düğmenin üstünde yazar.
- Basma tutmazsa eşyan kırılmaz, seviyesi düşmez. +3'e kadar her basma tutar.
- Her tutmayan basma örsün hafızasını doldurur. Hafıza dolunca sıradaki basma kesin tutar.
- Efsunları yenile: yenileri eskilerin yanında görünür. Eskiyi tut ya da yeniyi al; sen seçene kadar hiçbir efsun kaybolmaz.
- Eşyaları erit, seviyeyi başka bir eşyaya aktar, Kalıntı Ocağı'nda kalıntı döv.

Kahramanlar
- Vanguard ön safı tutar. Ocak Patlaması yakındaki her düşmanı savurur.
- Ilya Kest, Cinder Arcanist: her vuruşu bir düşmanı tutuşturur. Pota Ağzı yananları patlatır.

Adil oyun
- Oyun ücretsiz.
- İstersen sefer sonunda kısa bir video izler, o seferin altınını 1,5 katına çıkarırsın. İzlemezsen hiçbir şey kaybetmezsin. Başka reklam yok.
- Mağazada tek ürün var: Ödül videolarını atla. Aynı bonusu video izlemeden verir.
- Güç satılmaz. Altın, malzeme, güçlendirici, efsun ve eşya yalnızca oynayarak kazanılır.
- Hesap gerekmez. İnternetsiz, çevrimdışı da oynanır; video bonusu yalnızca bağlantı varken sunulur.
- Türkçe ve İngilizce.
```

### 3.2 Only if hero skills 2-3 ship (docs/57 phases 3-4) - add as the last line of "Kahramanlar" (+117)

```text
- Her kahramanın üç yeteneği var. İkincisi 2. katı, üçüncüsü 3. katı ilk kez temizleyince açılır ve Ocak'ta dövülür.
```

With it, line one of "Sefer" can say "yeteneklerini" (plural) instead of "yeteneğini" (+3).

### 3.3 Only if P0's arena has a screen in the release (docs/55 section 9) - add before "Adil oyun", after a blank line (+157)

```text
Arena
- Kahramanını, kendi eski kahramanlarına ve oyunun ürettiği rakiplere karşı otomatik düellolarda dene. Arena telefonunda oynanır; çevrimiçi değildir.
```

### 3.4 What each claim rests on (re-check if the game changes before release)

| Claim | Source |
|---|---|
| Two-handed; the hero attacks on their own | docs/57 section 12 answer 4; `prototype.wave_format` "combat is automatic" |
| The floors' order | `MapChoiceView` texts (docs/60 section 7): the Foundry "where every Descent begins", the Gardens "the last floor before the Final Swarm" |
| A death costs part of the floor's gold; items are always kept | docs/54 ("death loses half of the dying floor's gold", "items are always kept on a death"); `anvil.bag.empty` |
| +3 always holds; the memory makes a strike certain; a miss keeps the level | `anvil.breakdown.safe`, `anvil.memory.*`, `anvil.breakdown.miss`; docs/58 section 2 |
| The chance is shown | `anvil.chance_caption`; docs/58 section 2 ("the chance on the button is the chance used") |
| The efsun choice | `anvil.efsun.note_pending`; docs/58 section 2 |
| Four rarities, four slots | `gear.rarity.*`, `gear.slot.*` |
| x1,5, one product, nothing that makes power sold, no other ads | docs/58 section 1 |
| Plays offline | docs/58 section 5a ("the run always completes without ads (and offline)") |
| Turkish and English | docs/59 decision 1 |

## 4. The in-app product (Play Console > Monetise > One-time products)

| Field | Text | Count |
|---|---|---|
| Name | Ödül videolarını atla | 21 |
| Description | Sefer sonu bonusunu (seferin altınının 1,5 katı) video izlemeden al. Bir kez alınır; aynı Google hesabıyla yeni cihazda da geri gelir. Güç vermez. | 146 |

The product id, the price (about 99,99 TL, docs/58) and the console's own length limits are set in Play Console.

## 5. Screenshots

**Specs** ([preview assets](https://support.google.com/googleplay/android-developer/answer/9866151)): 2 to 8 phone
screenshots; JPEG or 24-bit PNG, no alpha; up to 8 MB each. To be eligible for Play's game recommendations: **at least
three portrait 9:16 screenshots of at least 1080 x 1920** showing the game itself. So every frame here is
**1080 x 1920**, real play, no device frame, no store badge.

**How to take them:**

- Take them from a **release (non-development) build**, or the Unity "Development Build" mark shows in the corner.
- The S23 screen is 1080 x 2340. Either crop 420 px (check the HUD's top and bottom rows survive) or capture at
  1080 x 1920 from the Editor's Game view or the capture tools, which take `-Language tr` (docs/60:
  `Run-AnvilUiProof`, `Run-ChoiceRarityProof`, `Run-RoomPipsRuntimeProof`).
- One caption per frame, on a band above or below the play area, never over the action. Captions are short and claim
  only what the frame shows.

**The order** (the first three as docs/59 section 3 item 6 and the owner's report set them; the first frames carry
the page):

| # | Frame | What it shows | Caption (count) |
|---|---|---|---|
| 1 | **Real vertical combat** | The Vanguard in the Foundry with a crowd closing in, hits and damage numbers, the HUD, the skill button under the right thumb's spot. Busy but readable | Sürüye karşı dayan (18) |
| 2 | **"Your item never breaks"** | The Anvil right after a miss: "Tutmadı: +7 korundu / Örs hatırlıyor: +1 hafıza", the item still there, the memory bar | Eşyan asla kırılmaz (19) |
| 3 | **The efsun choice** | The Efsun tab with old and new lines side by side and "Eskiyi tut" / "Yeniyi al" | Efsunu seç: eskisi mi, yenisi mi? (33) |
| 4 | The level-up choice | The cards with rarities, a new weapon or an evolution among them | Her seviyede yapını kur (23) |
| 5 | A boss | The Glass Regent or the Forge Warden mid-fight | Kapıyı tutan bossu devir (24) |
| 6 | The checkpoint | "Durak: Çık ya da Tırman" with the gold at stake | Çık ya da tırman (16) |
| 7 | The heroes | The hero choice: the Vanguard and Ilya | İki kahraman, iki oyun tarzı (28) |
| 8 | The arena (if it ships) or the map choice | A duel, or the three floors and the Final Swarm | Arenada kahramanını dene (24) / Üç kat ve Son Sürü (18) |

Every frame is in Turkish for this listing (the English listing takes English frames, store-listing-en.md).

**Preview video (optional, recommended for games):** 15-30 seconds, real play from the first second (Play asks for
the core experience within the first 10 seconds and at least 80 % real experience; only the first 30 seconds autoplay).
A YouTube URL, public or unlisted, embeddable, not age-restricted, ads off. Suggested cut: horde (8 s), a boss blow
(4 s), a level-up pick (3 s), the Anvil's miss then a held +9 (6 s), the efsun choice (4 s), the title.

## 6. Feature graphic

**Specs:** 1024 x 500, JPEG or 24-bit PNG without alpha. Keep the key visual and the name in the centre, away from the
edges Play may crop; avoid a pure white, black or dark grey background (it melts into the store's own); alt text of
140 characters at most.

**Brief:** the game's own painted look (docs/08's astral foundry: dark stone and brass, a violet void, lava light).
Left of centre, the Vanguard in blue steel, sword raised, facing right; behind him, out of focus, a crowd of slag and
lava golems with ember eyes. Right of centre, the Anvil with a weapon on it glowing "+9", sparks rising. The word
**Cryptforge** in the centre-top band. No tagline, or one of two or three words if the owner wants one (the same in
both listings or translated). No skulls, no blood (docs/08), no "#1", no price, nothing the game does not show.
Background: the violet void with lava light, not near-black. One file serves both languages if it has no tagline.

**Alt text (113):** Lavla aydınlanan bir dökümhanede taş golem sürüsüne karşı duran bir şövalye; yanında örste parlayan +9 bir kılıç.

## 7. Keywords

Play has no keyword field; it reads the name and both descriptions. These are the words Turkish players search for in
this genre, and where the copy already carries them:

| Keyword | In the copy |
|---|---|
| roguelite | full description, line 1 |
| aksiyon | full description, line 1 |
| sürü | short description; full description |
| boss | full description |
| ganimet | full description |
| eşya, +9, basma, örs | short and full description |
| efsun | full description |
| dikey ekran | full description, line 1 |
| tek oyunculu | full description, line 1 |
| çevrimdışı, internetsiz | full description, "Adil oyun" |
| kahraman, silah, kılıç, asa, hançer | full description |
| RPG, roguelike, zindan, hayatta kalma | not in the copy; add one naturally only if search data shows it matters |

Do not add competitors' names (Archero, Survivor.io, Vampire Survivors, Metin2) anywhere: Play's metadata policy and
the owner's report both rule it out.

## 8. Category, tags and contact details

- **App or game:** Game. **Category: Action** (the genre's usual home; "Role Playing" is the alternative if the
  owner wants the gear progression to lead).
- **Tags:** up to Play Console's limit, from its own list, the closest to: roguelike, action RPG, fantasy, offline,
  single player (check what the list offers when the account exists).
- **Email (required):** {{CONTACT_EMAIL}}
- **Website:** {{WEBSITE_URL}} (AdMob reads `app-ads.txt` at its root)
- **Phone:** optional; leave empty unless the owner wants it public.
- **Privacy policy:** {{POLICY_URL}}
- **Developer name and address:** {{DEVELOPER_NAME}}; a personal account that earns money shows its full address,
  {{POSTAL_ADDRESS}}, on the store page (docs/59 decision 3).
