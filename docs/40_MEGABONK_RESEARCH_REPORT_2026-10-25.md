# CRYPTFORGE — Megabonk Araştırması, Oyun Tasarımı ve Balance Raporu

> **Tarih:** 25 Ekim 2026  
> **Sürüm:** 1.0 — Birleştirilmiş Claude Code handoff belgesi  
> **Proje:** Cryptforge · Unity 6 · Android · Dikey ekran · İzometrik · Otomatik saldırı  
> **Dil:** Türkçe; DPS, TTK, scaling, proc, cooldown, build, item, tome vb. teknik terimler İngilizce.  
> **Amaç:** Önceki kapsamlı araştırma/tasarım çalışmasını tek bir dosyada toplamak; uygulanabilir sistem tasarımı ve ölçülebilir başlangıç denge parametreleri sağlamak.

## Okumadan önce: kaynak ve sayı statüleri

Bu belgedeki bilgiler üç düzeyde yorumlanmalıdır:

- **[MEGABONK—KAYNAKLI]**: Araştırmada belirtilen resmî sayfalar, geliştirici patch notları veya topluluk analizlerinden derlenmiş mekanikler. Kaynakların sürümleri farklı olabilir; oyun içi güncel doğrulama gerekebilir.
- **[MEGABONK—DOĞRULANMAMIŞ]**: Topluluk çıkarımı, kod analizi, eski patch verisi veya çelişkili iddia. Kesin oyun gerçeği olarak kabul edilmemeli.
- **[CRYPTFORGE—ÖNERİ]**: Bu rapor için önerilen formül, hedef, parametre, yetenek, TTK, unlock ve kabul kriteri. **Henüz Cryptforge telemetrisiyle ispatlanmış veya test edilmiş değerler değildir.**

**Mühendislik ilkesi:** Önerileri doğrudan production balance olarak sabitleme. Hepsini configurable hale getir; baseline karşılaştırması, deterministic seed simülasyonu, cihaz profili ve gerçek oyuncu testleriyle doğrula. Cryptforge'u Megabonk'un isimlerini, görsellerini ve karakterlerini kopyalamadan geliştir.

---

# 1. Yönetici özeti

**Ana tavsiye:** Cryptforge'un kat ve Foundry kimliğini koru; yalnızca odalara girilince tetiklenen, 5 saniye süren küçük düşman dalgaları modelini kaldır. Oyunu tek büyük Foundry haritasında **sürekli combat + keşif + node objective + Forge + Elite + Boss + Extract** içeren bir *hybrid arena-survivor* yapısına taşı.

## 1.1. Mevcut durum / ölçülen sorun

- Unity 6 Android, portrait izometrik, tek parmak hareket, auto attack.
- Tek karakter: **Vanguard**. Silahlar: **Staff (AoE), Daggers (kritik/hız), Sword**. Aktif: **Forge Burst**. Kalıcı relic örnekleri: **Second Wind**, **Counterweight**.
- Her kat: 3 combat odası + Forge + Elite + Forge Warden boss; harita bir büyük Foundry; yürüyerek node tetikleme; finalde swarm ve erken Extract.
- Level-up: üç kart. Dokuz stat: damage, attack speed, HP, range, crit, luck, armor, movement, stamina. Kart tavanında Evolution; her Evolution +%20 Difficulty. Shrine: bir kart için +%10 Difficulty. Chest: book çıkabilir.
- XP ve gold elle toplanır; pickup radius 1 birim. Lava pit 7 saniyede bir patlar. Hareket stamina tüketir.
- Meta: koşu gold'u ile weapon/relic açılır.
- **Problem:** Kat 1,5–2 dakika; hedef 4–6 dakika. Dalgalar yaklaşık 5 saniye, boss tehdit oluşturmuyor; büyük horde, build explosion, run variation eksik.

## 1.2. En önemli 10 tasarım kararı

| # | Karar | İlk ölçülebilir hedef |
|---|---|---|
| 1 | Kat/oda iskeletini koruyup sürekli combat'a geç | 3 kat; 4–6 dk/kat |
| 2 | Düşmansız yürüme/bekleme oranını azalt | Combat active time >= %75 |
| 3 | Mekanik kimliği farklı 6 karakter tasarla | Skill dependence 1–5 |
| 4 | Weapon ve Tome upgrade sistemlerini ayır | 3 weapon + 3 tome slot |
| 5 | Build sinerjisini tag temelli kur | Burn, Bleed, Shock, Guard, Heat |
| 6 | Dört rarity seviyesi; sınırlı Luck etkisi | Green/Orange/Red/Gold; normalleştirilmiş odds |
| 7 | Kalabalık algısını mobil frame budget ile yönet | Normal 50–90, yoğun 120 görünür/aktif hedef |
| 8 | Evolution'ın otomatik Difficulty cezasını kaldır | Risk, Overforge ile isteğe bağlı |
| 9 | Final Swarm + Extract kararını derinleştir | Erken çıkış veya daha büyük ödül |
| 10 | Dengeyi telemetri/simülasyonla doğrula | TTK, effective DPS, survival, win rate, FPS |

**Hedef güç deneyimi:** İlk 90 saniyede ilk güçlenme; 4–6. dakikada belirgin synergy; 9–12. dakikada kalabalık temizlemede dramatik sıçrama; finalde güçlü ama positioning hatasına karşı kırılgan oyun.

**Önemli teknoloji ilkesi:** Ekranda çok düşman hissettirmek, hepsinin tam maliyetli AI, fizik ve animasyonla işlenmesini gerektirmez. Simulation detail ve visual density ayrı bütçelere sahip olmalı.

---

# 2. Megabonk araştırması — oyun gerçekleri ve dersler

> Bu bölüm Cryptforge tasarımı için karşılaştırmalı kaynak araştırmasıdır. Kaynaklar ve linkler Bölüm 18'de. Resmî kaynakları wiki/topluluk analizlerine göre önceliklendir. Bazı mevcut wiki'lerde eski patch değerleri korunmuş olabilir.

## 2.1. Kaynak güvenilirliği ve sürüm notu

| Kaynak | Güven seviyesi | En uygun kullanım |
|---|---|---|
| Geliştirici Steam announcements/patch notes | Yüksek | Balancing değişimleri ve değişiklik tarihi |
| Steam mağazası | Yüksek | Oyunun çekirdek iddiaları |
| Sürüme bağlı kod/tersine mühendislik | Orta-yüksek | Algoritma ve değerler; sürüm kontrolü gerekli |
| Fan wiki / veri siteleri | Orta | İçerik envanteri, unlock, stat listesi |
| Reddit/Steam yorumları | Niteliksel | Algılanan eğlence, meta, eleştiri |
| Tekil video/rehber | Düşük-orta | Görülen davranış; kesin küresel mekanik değil |

Önceki araştırmada erişilen geliştirici duyurularında **v1.0.69 (4 Şubat 2026)** referans alındı. Bu bir *araştırma anı notudur*; en son sürümün mutlak garantisi değildir. Karakter sayısı için Steam'de 20 ifadesi bulunurken topluluk kaynakları Roberto sonrası 21 karakter bildiriyor. Tarihsel rehberlerde Luck Tome +%7 / XP Tome +%9 verilirken sonraki patch notlarında Luck +%8 / XP +%7 açıklanıyor. Benzer şekilde temel weapon/tome slot kapasitesi progression ve ek açılabilir özelliklerle değişebiliyor.

## 2.2. Core loop, run süresi, stage, boss ve final swarm

**[MEGABONK—KAYNAKLI]** Oyuncu 3D, rastgele harita üzerinde auto weapon ile düşman öldürür; XP toplar, silah/tome yükseltir, chest ve shrine etkileşimleri arar; giderek zorlaşan swarm'larla karşılaşır, Boss Portal ile boss'a gider. Forest, Desert, Graveyard gibi haritalar ve farklı Tier progression tarif edilmektedir.

Topluluk açıklamalarındaki yaklaşık stage içi olaylar:

| Zaman | Olay / belirsizlik |
|---|---|
| Başlangıç | Keşif, XP, weapon/tome, loot |
| ~3. dakika | İlk miniboss olarak bildiriliyor |
| ~4. dakika | Swarm baskısı |
| ~7. dakika | İkinci swarm |
| ~8. dakika | İkinci miniboss |
| Stage sonu | Final Swarm baskısı |
| Boss Portal etkinleşince | Stage boss savaşı |

Bazı kaynaklarda stage süreleri 10/9/8 dakika, bazılarında standart 10 dakika gösterilir. **[MEGABONK—DOĞRULANMAMIŞ] Tüm harita ve Tier kombinasyonları için kesin zaman çizelgesi doğrulanmış değildir.**

Boss'lar Tier ilerleyişine bağlı; bazı ileri encounter'larda silahlar geçici olarak kısıtlanabiliyor. Tasarım dersi: boss salt DPS kontrolü değil, oyuncunun hazırlığını ve mekanik ustalığını ölçen encounter olmalı.

## 2.3. Megabonk karakterleri ve farklılıkları

Skill sütunu **Cryptforge rapor yazarının niteliksel 1–5 yorumu**, oyunun resmî derecelendirmesi değildir.

| Karakter | Başlangıç weapon | Pasif/kimlik özeti | Tahmini skill |
|---|---|---|---:|
| Fox | Firestaff | Seviye başına Luck (+%1,5 olarak bildiriliyor) | 1 |
| Sir Oofie | Sword | Seviye başına Armor (+%1) | 1 |
| Calcium | Bone | Momentum/hareketten damage | 4 |
| Megachad | Aura | Flex savunması; başarıyla permanent damage | 2 |
| Ogre | Axe | Seviye başına Damage (+%1,5) | 1 |
| CL4NK | Revolver | Level başına Crit Chance (+%1), düşük HP | 3 |
| Athena | Aegis | Thorns / düşük HP sinerjisi | 3 |
| Robinette | Bow | Biriken gold ile damage | 2 |
| Monke | Bananarang | Duvara tırmanma, Max HP/level | 2 |
| Tony McZoom | Wireless Dagger | Hareket + electricity, pickup range | 3 |
| Bush | Sniper Rifle | Marked target burst, Crit Damage | 4 |
| Birdo | Tornado | Havada süzülme, airborne damage | 4 |
| Noelle | Frostwalker | Size / frozen enemy synergy | 2 |
| Amog | Poison Flask | Poison explosion chains | 2 |
| Spaceman | Black Hole | Büyüyen XP Gain, düşme damage yok | 2 |
| Bandit | Dexecutioner | Seviye başına Attack Speed (+%1) | 2 |
| Ninja | Katana | Evasion sonrası execute | 4 |
| Vlad | Blood Magic | Lifesteal | 2 |
| Sir Chadwell | Corrupted Sword | Seviye başına Difficulty (+%1) | 5 |
| Dicehead | Dice | Level-up'ta rastgele bonus stat | 3 |
| Roberto | Scythe | Periyodik ücretsiz chest | 2 |

**[MEGABONK—DOĞRULANMAMIŞ]** Tam pasif sayıları patch-dependent; özellikle Calcium, Noelle, Fox, Amog ve Roberto'yu güncel oyun/build üzerinden tekrar kontrol etmek gerekir.

Dersler:
- Sir Oofie ve Ogre gibi görece düşük execution gerektiren karakterler onboarding kolaylığı sağlar.
- Calcium'un momentum, Robinette'in gold, Dicehead'in randomness sistemi aynı kartlara farklı değer kazandırır.
- Sir Chadwell ve CL4NK risk-getiri dengesini değiştirir.
- Cryptforge burada ayrışabilir: karaktere özel aktif yetenek mastery, yalnızca başlangıç stat farkı değil.

## 2.4. Megabonk silahları ve slot sistemi

**[MEGABONK—KAYNAKLI / SÜRÜME BAĞLI]** Topluluk listeleri yaklaşık 30 weapon içerir; standart ilerlemede 2'den 4'e çıkabilen weapon kapasitesi bildirilir. Silahlar otomatik saldırır, kendi stat upgrade havuzları vardır. Örnekler:

| Silah | Davranış | Scaling |
|---|---|---|
| Sword | Geniş yakın slash | Damage, Size, Projectile Count, Knockback |
| Firestaff | Patlayan fireball | AoE, projectile |
| Lightning Staff | Lightning chain | Count, Bounces, Size |
| Aura | Yakın çevre damage | Size, Damage |
| Flamewalker | Hareketle fire trail | Duration, Size, Damage |
| Revolver | Çoklu bullet | Crit, Bounces |
| Sniper Rifle | Delici yüksek damage | Single-target, Size |
| Katana | Yakın dövüş | Crit / melee |
| Poison Flask | Poison uygulayan projectile | DoT / chain |
| Black Hole | Pull / area denial | Size, Duration, Damage |
| Wireless Dagger | Tracking daggers | Count, Bounces |
| Frostwalker | Freeze + damage | Size, Duration |

Weapon-specific raw damage artırımı ile character-wide tome multiplier **farklı aşamalarda** uygulanır. Eski v1.0.7 ölçümlerinde Sword Common Damage upgrade +2 base damage, Damage Tome +0.08x global damage biçiminde raporlanmıştır; bu sayıların güncel patch eşdeğeri olduğuna dair garanti yoktur.

## 2.5. Megabonk tomes

**[MEGABONK—KAYNAKLI / SÜRÜME BAĞLI]** Standart progression'da tome slotları 2→3→4 açılır; ek beşinci slot hakkında topluluk kayıtları bulunur. 23 tome olarak raporlanan liste:

- Offensive: Damage, Cooldown, Quantity, Size, Projectile, Precision.
- Projectile / control: Duration, Knockback.
- Defensive: HP, Regen, Shield, Armor, Evasion, Bloody, Thorns.
- Mobility / resource: Agility, Attraction, XP.
- Economy / risk: Luck, Gold, Silver, Cursed, Chaos.

Tarihsel Common upgrade örnekleri:

| Tome | Raporlanan artış | Not |
|---|---|---|
| Damage | +0.08x character damage | Tarihsel ölçüm |
| Cooldown | +%7,5 Attack Speed | Tarihsel ölçüm |
| Quantity | +1 projectile | Silahla ilişkili |
| Size | +%10 | Tarihsel ölçüm |
| Precision | +7 pp Crit Chance | Tarihsel ölçüm |
| HP | +25 Max HP | Tarihsel ölçüm |
| Armor | +%12 Armor statı | Tarihsel ölçüm |
| Attraction | +%75 pickup range | Tarihsel ölçüm |
| Cursed | +%3,5 Difficulty | Tarihsel ölçüm |
| Luck | +%8 | v1.0.49 sonrası patch iddiası |
| XP | +%7 | v1.0.12 sonrası patch iddiası |

Bir tome'un değeri, tek başına verdiği sayının yanında **kaç silahı ve kaç proc/attack davranışını etkilediğine** bağlıdır. Etkisiz tome teklifleri build diversity'yi azaltır.

## 2.6. Megabonk rarity ve Luck

Megabonk rarity olarak beş kademe kullanır: Common, Uncommon, Rare, Epic, Legendary. Tarihsel weapon/tome ölçüm çarpanları:

| Rarity | Tarihsel upgrade multiplier |
|---|---:|
| Common | 1.00x |
| Uncommon | 1.20x |
| Rare | 1.40x |
| Epic | 1.60x |
| Legendary | 2.00x |

**[MEGABONK—DOĞRULANMAMIŞ]** Resmî olarak doğrulanmış, bütün rarity roll türlerini kapsayan tek Luck formülü bulunmadı. Topluluk kod analizindeki yaklaşık alternatif:

```text
S = 1.5 * ln(1 + L/100)
W_i = b_i * 1.5^(-(3-i)*S)
P_i = W_i / Σ_j W_j
```

Bu analiz belirli rarity grupları için `b = [70, 15, 6, 1.5]` gibi temel ağırlıklar bildirir. Bunun *tüm item, weapon ve tome* rarity sistemlerine aynen uygulandığı varsayılmamalı. Cryptforge için Bölüm 7'de **bağımsız**, daha kolay test edilebilir dört rarity modeli öneriliyor.

## 2.7. Harita etkileşimleri ve meta unlock

| Megabonk mekanik örneği | İşlevi | Cryptforge'a ders |
|---|---|---|
| Charge Shrine | Riskli bölgede durup şarj et, stat al | Konumsal risk |
| Greed / Curse | Güç/ödül karşılığında daha yüksek tehdit | İsteğe bağlı risk |
| Chest | Gold vererek item edin | Ekonomik seçim |
| Key | Ücretsiz chest olasılığına katkı | Economy synergy, diminishing returns |
| Shady Guy | Harita satıcısı | Alternatif loot kaynağı |
| Microwave | Item dönüşümü / kopyalama | Build optimizasyonu |
| Boss Portal | Keşfedilip aktive edilir | Oyuncunun tempo kontrolü |
| Quest / Unlock | Yeni kalıcı içerik | Uzun vadeli yönlendirme |

Charge Shrine'dan ayrılmak progression'ı bozabilir; belli Key etkilerinin diminishing returns kullandığı topluluk analizlerinde açıklanır. Bazı unlock'lar boss/weapon/quest başarısından sonra Silver ile alınır.

## 2.8. Spawn ve Difficulty

**[MEGABONK—KAYNAKLI]** Difficulty genel olarak enemy HP, damage, hareket ve spawn baskısını etkiliyor; artan spawn dolaylı olarak daha fazla XP üretebilir.

**[MEGABONK—DOĞRULANMAMIŞ / SÜRÜM BAĞIMLI]** Topluluk kod analizi Stage 1, %200 Difficulty, ilk 90 saniye koşulunda hızlı kill ile yaklaşık 275 spawn; hiç kill yoksa 89 spawn raporlar. Normal aşamalarda ~550 canlı düşman limiti ve Final Swarm için zamanla değişen ghost limitleri (ör. 400→300) bildirilmiştir. Bunları Cryptforge için doğrulanmış teknik hedef sanma.

Ana ders: *kill-reactive director* — iyi temizleyen oyuncu daha çok spawn talep edebilir; yavaş temizleyeni sınırsız düşmanla boğma. Tüm düşmanlar için resmî ve eksiksiz HP/damage/speed scaling formülü **doğrulanmamıştır**.

## 2.9. Topluluk övgüleri ve eleştiriler

Steam'de güçlü pozitif oyuncu algısı bulunuyor; yorum oranları zamanla değişebilir. Tek bir pozitif skor mekanik bazlı kalite ölçümü değildir.

Övülen taraflar: farklı karakter hissi, beklenmedik build kombinasyonları, sinerji optimizasyonu, yüksek rarity ve riskli güç patlamaları. Eleştirilen taraflar: belirli ekonomi/item kombinasyonlarının dominant olması, optimal koşular için restart baskısı, bazı silahların görece değersiz görülmesi, geç oyunda projectile/proc kaynaklı performans. Geliştiricinin balance ve performans patch'leri de bu risklerin varlığına işaret eder.

**Ders:** Yüksek varyanslı *eğlence* serbest; oyuncunun rastgele kötü teklif yüzünden hiçbir makul seçeneğinin kalmaması istenmez.

## 2.10. Tür karşılaştırması

| Oyun | Ayırıcı yapı | Cryptforge dersi |
|---|---|---|
| Megabonk | 3D keşif + auto weapons + loot/shrine/boss | Combat ve keşfi birleştir |
| Vampire Survivors | Zaman baskısı + silah evrimleri + sürü | Basit kontrol, güçlü scaling |
| Brotato | 20–90 sn wave + shop + 6 weapon | Okunabilir kısa pacing ve economy |
| Halls of Torment | Dark fantasy, yaklaşık 30 dk run + quests | Atmosfer ve objective progression |
| 20 Minutes Till Dawn | Manuel aiming, ability/build synergy | Skill expression ve weapon identity |

**Cryptforge farklılaşması:** Foundry/Heat/Overforge/extraction, karaktere bağlı aktif ability mastery, mobil portrait'de optimize kalabalık.

---
# 3. Cryptforge koşu yapısı ve combat director

## 3.1. Seçenek karşılaştırması

| Kriter | A: Kat + altı ayrı oda / kısa wave | B: Açık Foundry + timer / sürekli combat |
|---|---|---|
| Var olan sistemi koruma | Çok iyi | Orta |
| Sürekli swarm baskısı | Ek değişiklik gerekir | Yapısal avantaj |
| Mobil oturum yönetimi | Kolay | Süre sınırıyla kolay |
| Keşif | Bölgesel | Daha güçlü |
| Boss pacing | Deterministic | Oyuncu daha çok karar verir |
| Build ilerleme | Oda araları kesebilir | Daha akıcı |
| Simülasyon / QA | Daha deterministik | Daha fazla senaryo |
| Ana risk | Combat kopuk kalır | Amaçsız dolaşma |

**[CRYPTFORGE—ÖNERİ] Seçim: B combat modeli + A progression iskeleti.** Altı nokta yine haritada dağılsın, ama düşman yalnızca salonun içinde ortaya çıkmasın. Harita traversal süresince spawn çalışsın. Combat Node'lar ayrıca görev / encounter özelliği açsın.

Altı aktivite:

1. Combat Node I — 45 saniyelik objective veya özel enemy composition.
2. Combat Node II — farklı enemy/arena objective.
3. Combat Node III — farklı enemy/arena objective.
4. Forge Node — kısa reward / weapon / tome / evolution kararı.
5. Elite Node — isteğe bağlı yüksek risk + özel reward.
6. Boss Gate — üç Combat Node bittikten sonra açılır; Forge ve Elite opsiyonel.

Boss kapısının açılması timer bitişine kilitlenmesin: hızlı oyuncu boss'a erken girsin; ekstra loot isteyen devam edebilsin.

## 3.2. Süre hedefleri

| Aşama | Hedef | Kimlik |
|---|---:|---|
| Kat 1 | 4–5 dk | Build temeli / ikinci silah |
| Kat 2 | 4–5 dk | Elite / ana synergy |
| Kat 3 | 4–5 dk | Evolution / güçlü boss |
| Final Swarm | 45–90 sn tipik | Extract veya artan risk |
| Tam normal run | 13–16 dk | Standart oturum |
| Kat 1 sonrası extract | 4–6 dk | Kısa oturum |
| Kat 2 sonrası extract | 8–11 dk | Orta oturum |

**Örnek 300 saniyelik kat bütçesi:** 3 node × 45 sn = 135 sn; keşif/traversal 55 sn; Forge/loot 20 sn; Elite 35 sn; Boss 55 sn. Savaş, bu segmentlerin çoğunda arka planda devam edeceği için combat active time yaklaşık 225–260 saniye olabilir. Bu da **%75–87** demektir; gerçek ölçüm şarttır.

## 3.3. Director durumları

| Kat yüzdesi | State | Baskı |
|---|---|---|
| %0–25 | Ramp | Basit düşmanlar, ilk seçim |
| %25–60 | Horde | Normal spawn, seyrek ranged, keşif |
| %60–85 | Pressure | Runner/ranged/elite kombinasyonu |
| %85–100 veya Gate tetiklenince | Boss | Kontrollü minion + boss mechanics |

Boss girişinde bütün düşmanları aniden despawn ederek tehlikeyi sıfırlama; ama aktif düşman limitini, boss telegraph okunabilirliğini ve hesap yükünü koru.

---

# 4. Cryptforge karakter tasarımı — altı karakter

**[CRYPTFORGE—ÖNERİ]** Skill dependence: 1 = auto-hit ile normal farm yapılabilir; 5 = aktif yetenekler iyi kullanılmadan ileri seviyede başarı zor. Her karakterin güçlü ve zayıf yanları oyuncunun seçtiği zorluk/composition'a göre gerçek anlamda ayrışmalı.

## 4.1. Karakter tasarım tablosu

| Karakter | Rol | Pasif | Aktifler (CD) | Başlangıç silahı | Skill | Güçlü / zayıf | Parladığı koşul |
|---|---|---|---|---|---:|---|---|
| **Vanguard — Iron Wall** | Balanced/tank, onboarding | Combat içinde her 6 sn +%4 Armor stack, max 5; hit alınca 1 stack kayıp | **Forge Burst** (18 sn): 2.5 m cone AoE; **Iron Resolve** (28 sn): 3 sn %35 damage reduction | Sword | 1 | Güvenli auto-hit; düşük single-target burst | Apprentice/Forged, yeni oyuncu |
| **Cinder Arcanist — Flame Architect** | Burn/AoE chain | Burn'lü hedef ölünce max 4 yakına 1 Burn stack aktar; recursive sınır | **Crucible Vent** (14 sn): Burn tüketip explosion; **Flame Sigil** (25 sn): 5 sn alan | Staff | 3 | Yüksek density; fire-resistant elite/az hedefte zayıf | Horde, orta-ileri |
| **Riftblade — Edge Dancer** | Movement/Crit/Bleed burst | 2.5 m hareket sonrası Crit, +%60 Bleed damage, 1.2 sn internal CD | **Phase Dash** (9 sn): kısa invuln dash; **Sever** (18 sn): marked execute | Daggers | 4 | Elite/boss güçlü; durağan ve yoğun yakın baskıda kırılgan | Skilled, yüksek difficulty |
| **Bastion — Living Furnace** | Guard/retaliation | Önlenen 20 damage başına 12 retaliation, max 2 proc/sn | **Molten Guard** (20 sn): 4 sn shield; **Groundbreak** (24 sn): stun/knockback cone | Iron Halo | 2 | Melee sürü güvenliği; ranged boss'a karşı zayıf | Melee yoğun mod |
| **Voltwright — Coil Master** | Shock/chain tech | Shock→3 sn Conductive; zincirleme isabette max 3 kez +%10 sonraki hedef damage | **Overclock** (17 sn): 5 sn +%35 Attack Speed; **Coil Anchor** (27 sn): 6 sn chain relay | Arc Conduit | 3 | Dağınık gruplar; yalnız boss'a chain değer kaybı | Horde/ranged kompozisyon |
| **Sanguine — Blood Forged** | High risk HP/burst | Eksik her %10 HP için +%5 damage, cap +%40 | **Blood Pact** (12 sn): current HP'nin %12'sini öde, 5 sn +%55 damage; **Crimson Recall** (22 sn): son 4 sn direct damage'ın %8'ini heal, cap max HP'nin %25'i | Sanguine Sickle | 5 | Çok yüksek skill reward; yanlış timing ölümcül | Skilled, yüksek difficulty |

**UI notu:** Tek parmakla sürükleyerek hareket ederken iki ayrı yetenek butonuna kolay erişim garanti değildir. İlk testte tek ana ability butonu; ikinci yeteneği otomatik tetik, kısa/uzun basma veya context-aware seçenek olarak dene. Long-press'in hareketi kesip kesmediğini test et.

## 4.2. Beceriye bağımlılık metriği

```text
R_skill = sustained_DPS_good_active_use / sustained_DPS_auto_only
```

Bu oran aynı enemy composition, aynı build ve aynı seed penceresinde ölçülmeli.

| Karakter | Auto-only davranışı | İyi active kullanımının hedef DPS etkisi |
|---|---|---:|
| Vanguard | Normal run mümkün | 1.05–1.15x |
| Bastion | Çok durumda yeterli | 1.15–1.30x |
| Cinder Arcanist | Horde farm mümkün | 1.35–1.60x |
| Voltwright | Genel combat mümkün | 1.30–1.55x |
| Riftblade | Boss'ta zorlanmalı | 1.60–2.00x |
| Sanguine | İleri aşamada zorlanmalı | 1.80–2.30x |

Bunlar tek yeteneğin anlık burst multiplier'ı değil, **karşılaştırılabilir combat penceresinde sustainable performans oranlarıdır**. Skilled bot ve insan playtest'inde doğrulanacak.

---

# 5. Cryptforge silah sistemi

## 5.1. Slot ve level kuralları

- Başlangıç 1 weapon; **3 weapon slot** üst sınırı.
- Ayrı **3 tome slot**.
- Her weapon **Lv1–8** arasında gelişir.
- Lv5'ten itibaren ilgili Tome/Relic koşulu ile Evolution teklif edilebilir; Lv8 normal upgrade tavanı.
- Weapon-specific upgrade seçeneklerinden **bir tanesi** level başına gelir; aşağıdaki damage + size + status satırları her seviyede birlikte verilmez.
- Weapon slot dolunca yeni weapon tekliflerini filtrele; owned upgrade'lere ağırlık aktar.

## 5.2. 11 weapon tasarımı

Damage birimi = nominal HP/hit; period = saniye/attack. Tüm rakamlar **başlangıç tuning önerisi**.

| Weapon | Base hit / period | Common +Damage upgrade | Alternatif weapon-specific upgrade | Ana tag / synergy |
|---|---|---|---|---|
| Sword | 18 / 0.80 sn | +3 | +%12 Size, +%10 Knockback | Armor, Guard |
| Staff | 24 / 1.10 sn | +4 | +%12 Radius, +%15 Burn | Burn, Size |
| Daggers | 7 / 0.30 sn | +1.2 | +5 pp Crit, +%15 Bleed | Crit, Tempo |
| Iron Halo | 11 / 0.65 sn | +2 | +%12 Radius, +%8 Guard efficiency | Guard, Size |
| Arc Conduit | 16 / 0.85 sn | +2.5 | +1 Bounce, +%8 Shock | Shock, Tempo |
| Magma Mortar | 34 / 1.80 sn | +6 | +%15 Blast Radius, +%18 Burn | Burn, AoE |
| Cinder Saw | 5 / 0.20 sn | +0.8 | +%10 Duration, +%8 Bleed | Tempo, Bleed |
| Forge Bow | 25 / 1.20 sn | +4.5 | +7 pp Crit, +1 Pierce | Crit, Single Target |
| Gravity Anvil | 42 / 2.00 sn | +7 | +%12 Size, +%10 Knockback | CC, Area |
| Sanguine Sickle | 21 / 1.00 sn | +3.5 | +%12 Bleed, +%5 Lifesteal efficiency | Bleed, HP |
| Ember Mines | 28 / 1.50 sn | +5 | +%15 Area, +%12 Duration | Area, Burn |

## 5.3. Damage ve Attack Speed formülleri

Weapon-specific base damage artışları önce toplanır, global additive damage bucket sonra uygulanır:

```text
D_hit = (d0 + Σ_j (Δd_j * m_rj)) * (1 + G_D) * M_tag * M_conditional

DPS_raw = D_hit * (1 + AS_bonus) / base_attack_period_seconds
```

- `d0`: HP/hit baz hasar.
- `Δd_j`: ilgili weapon Damage upgrade'inin common artışı.
- `m_rj`: upgrade rarity multiplier.
- `G_D`: toplam global Damage bonusu, ondalık (örn. +%24 → 0.24).
- `M_tag`: elemental/status synergy multiplier.
- `M_conditional`: Crit/skill/target state koşullu çarpan.
- `AS_bonus`: additive Attack Speed oranı; weapon-specific ve generic modifier stacking kuralını config ile tanımla.

**Örnek:** Sword d0=18; iki yeşil +3 ve bir kırmızı +3×1.5 Damage upgrade; global +%24:

```text
D_hit = (18 + 3 + 3 + 3*1.5) * 1.24 = 35.34 HP/hit
DPS_raw = 35.34 * 1.20 / 0.80 = 53.01 HP/s
```

Bu **raw focused DPS**; miss, downtime, Crit variance, defense ve overkill dahil değil.

## 5.4. Evolution örnekleri

| Koşul | Evolution | Davranış farkı |
|---|---|---|
| Staff Lv5 + Ember/Burn Tome Lv2 | Cinder Nova | 3 Burn stack'li hedef ölünce küçük explosion |
| Daggers Lv5 + Precision Lv2 | Rift Echo | Her üçüncü Crit yakına echo hit |
| Sword Lv5 + Bastion Lv2 | Anvil Arc | Yakındaki düşman sayısına bağlı kısa aralıklı shockwave |
| Arc Conduit Lv5 + Conduct Lv2 | Storm Circuit | Conductive üzerinden chain extra bounce |
| Sanguine Sickle Lv5 + Vitality Lv2 | Blood Furnace | Düşük HP'de verimli ama capped lifesteal |

Evolution sadece +Damage olmamalı; targeting, coverage, proc ve playstyle'ı değiştirmeli. Evolution **otomatik +%20 Difficulty** yüklememeli. Riskli ekstra upgrade için **Overforge** sun.

---

# 6. Cryptforge Tome sistemi

## 6.1. Tome rarity tablosu

Her değer **bir tome upgrade'inin verdiği ilave miktardır**; rarity multiplier genel olarak 1.00/1.25/1.50/2.00. pp = yüzde puan.

| Tome | Yeşil | Turuncu | Kırmızı | Altın |
|---|---:|---:|---:|---:|
| Force — Global Damage | +%8 | +%10 | +%12 | +%16 |
| Tempo — Attack Speed | +%6 | +%7.5 | +%9 | +%12 |
| Reach — AoE Radius | +%8 | +%10 | +%12 | +%16 |
| Precision — Crit Chance | +4 pp | +5 pp | +6 pp | +8 pp |
| Rupture — Crit Damage | +%12 | +%15 | +%18 | +%24 |
| Ember — Burn Damage | +%10 | +%12.5 | +%15 | +%20 |
| Conduct — Shock proc chance | +6 pp | +7.5 pp | +9 pp | +12 pp |
| Magnet — Pickup Radius | +0.50 m | +0.625 m | +0.75 m | +1.00 m |
| Fortune — Luck | +6 | +7.5 | +9 | +12 |
| Bastion — Armor Rating | +8 | +10 | +12 | +16 |
| Vitality — Max HP | +12 | +15 | +18 | +24 |
| Knowledge — XP Gain | +%6 | +%7.5 | +%9 | +%12 |

## 6.2. Kurallar

1. Tome Lv1 alındığı anda bonus uygular.
2. Maksimum **3 farklı tome**; aynı tome sonraki tekliflerde level yükseltir, yeni slot kullanmaz.
3. Normal tome seviyesi en çok **Lv8**.
4. Slotlar dolduysa yalnızca owned tome upgrade teklifleri uygun kalır; kapalı kategori ağırlıkları yeniden normalize edilir.
5. Max-level tome, normal teklif havuzundan kalkar.
6. Mevcut silahlara hiç etki etmeyen tome'un ağırlığı azaltılır; gelecekte synergy kurulabilmesi için tamamen yasaklanmaz.
7. Aynı stat bucket içi yüzde bonuslar **additive**, ayrı sistem çarpanları kontrollü **multiplicative**.

Örnek: Force +%8 ve +%12 → toplam global Damage `1 + 0.08 + 0.12 = 1.20x` (ayrı 1.08 × 1.12 değil).

## 6.3. Sinerji örnekleri

- **Staff + Ember → Cinder Nova:** Hedefler öldükçe Burn patlamaları; yoğun düşük-HP horde için güçlü.
- **Daggers + Precision → Rift Echo:** Crit rhythm ve yüksek focused DPS, kontrollü duplicate attack.
- **Arc Conduit + Conduct → Storm Circuit:** Chain ile çoklu hedef coverage büyür.
- **Sword + Bastion → Anvil Arc:** Defense/close-range mob density'ye göre shockwave.
- **Sanguine Sickle + Vitality → Blood Furnace:** Riskli düşük-HP sustain; healing cap korunur.

Ana ders: oyuncu synergy bulduğunda yalnız hit damage değil **effective coverage**, **target distribution** ve **kill rate** büyüsün.

---

# 7. Cryptforge rarity ve Luck

## 7.1. Base rarity olasılıkları

| Rarity | Kod index | Temel odds | Upgrade stat çarpanı |
|---|---:|---:|---:|
| Yeşil / Green | 0 | %60 | 1.00x |
| Turuncu / Orange | 1 | %25 | 1.25x |
| Kırmızı / Red | 2 | %12 | 1.50x |
| Altın / Gold | 3 | %3 | 2.00x |

Bu dört renk Cryptforge için öneridir; Megabonk'un beş rarity'siyle karıştırma.

## 7.2. Luck ağırlık kaydırma formülü

```text
p0 = [0.60, 0.25, 0.12, 0.03]
i ∈ {0,1,2,3} // rarity index
L = Luck puanı (0, 10, 25, 50 ...)

w_i(L) = p0_i * (1 + L/25)^(0.4*i)
P_i(L) = w_i(L) / Σ_j w_j(L)
```

**Birim:** `L=25` 25 Luck puanı; yüzde formatı gerekiyorsa UI'da ayır. Toplam olasılık her zaman 1; negatif odds yok; artış azalan marjinal getiriye sahiptir.

## 7.3. Test tablosu

| Luck | Green | Orange | Red | Gold |
|---|---:|---:|---:|---:|
| 0 | %60.00 | %25.00 | %12.00 | %3.00 |
| 10 | %55.15 | %26.29 | %14.44 | %4.13 |
| 25 | %49.68 | %27.31 | %17.30 | %5.71 |
| 50 | %43.19 | %27.93 | %20.80 | %8.07 |

Örnek Luck 25:

```text
w0 = 0.60
w1 = 0.25 * 2^0.4 ≈ 0.330
w2 = 0.12 * 2^0.8 ≈ 0.209
w3 = 0.03 * 2^1.2 ≈ 0.0689
P_gold ≈ 5.71%
```

Üç kartın rarity roll'ları **bağımsız** ise en az bir Red veya Gold ihtimali:

```text
P(>=1 Red/Gold) = 1 - (1 - P_Red - P_Gold)^3
```

| Luck | >=1 Red/Gold (3 kartta) |
|---|---:|
| 0 | %38.6 |
| 10 | %46.0 |
| 25 | %54.4 |
| 50 | %64.0 |

Pity, guarantee, bağımlı kart seçimi vb. eklendiğinde bu formülün varsayımı değişir.

## 7.4. Ortalama rarity kalitesi

```text
E[m_rarity] = Σ_i P_i(L) * m_i
```

| Luck | Beklenen rarity stat çarpanı |
|---|---:|
| 0 | 1.1525x |
| 10 | 1.1792x |
| 25 | 1.2118x |
| 50 | 1.2546x |

Luck 0→50 geçişi upgrade başına beklenen rarity çarpanını ~%8.9 artırır. **Bu sayı toplam run DPS'inde otomatik +%8.9 anlamına gelmez.** Sadece rarity teklif kalitesi beklentisidir.

## 7.5. Luck zorunlu olmasın

- Luck, doğrudan Crit/proc/DPS yerine yalnız rarity kalitesini etkilesin.
- Luck Tome seçmenin **fırsat maliyeti** olsun: o teklifte damage/utility seçilmiyor.
- 50 üzerindeki Luck için soft cap önerisi: `L_effective = 50 + 0.25 * max(0, L - 50)`.
- İlk 2 dakikada yeni silah bulamayana **kategori garantisi** tanımlanabilir; rarity garantisi değildir.
- Luck almadan bütün karakterler iyi build ile kazanabilmeli.
- Automated test: `Luck-pick` / `no-Luck` eşleştirmelerini seed, karakter, difficulty ve bot düzeyinde yap.

---
# 8. Matematiksel denge çerçevesi — DPS, TTK, HP, spawn, pressure

**Dört temel bütçe:** (1) focused single-target DPS, (2) effective horde DPS, (3) survivability/hit budget, (4) pressure/spawn + composition. Birini diğerinin vekili olarak kullanma.

## 8.1. Median oyuncu DPS hedef eğrisi

Referans karakter, uygun ama aşırı optimize edilmemiş build, standart Forged zorluğu için:

```text
D_focus(t) = 22 * exp(0.22 * t)
t: koşu süresi [dakika]
D_focus: sustained focused DPS [HP/s]
```

| Dakika | P50 focused DPS | Başlangıca oran |
|---|---:|---:|
| 0 | 22 | 1.0x |
| 2.5 | 38 | 1.7x |
| 5 | 66 | 3.0x |
| 7.5 | 115 | 5.2x |
| 10 | 199 | 9.0x |
| 12.5 | 344 | 15.6x |
| 15 | 597 | 27.1x |

Büyüyen güç eğrisi tasarım amacıdır; üretimde benchmark değildir. P10/P90 build ayrışma hedefi:

| Zaman | P10/P50 | P90/P50 |
|---|---:|---:|
| 2 dk | 0.80x | 1.20x |
| 5 dk | 0.70x | 1.50x |
| 10 dk | 0.65x | 1.80x |
| 15 dk | 0.55x | 2.20x |

Bu varyansı **aynı player-skill cohort/difficulty ve eşdeğer meta state** içinde değerlendir. Güçlü kombinasyonlar geç oyunda öne çıkmalı, kötü RNG erken oyunda otomatik ölüm üretmemeli.

## 8.2. Enemy HP ve attack damage eğrisi

Normal Grunt için:

```text
HP_grunt(t) = 24 * exp(0.15*t)                  // HP
D_raw_enemy(t) = 18 * (1 + 0.06*t)              // raw HP / successful normal hit
TTK_approx(t) = HP_grunt(t) / D_focus(t)         // saniye
```

| Dakika | Grunt HP | Hit başına raw damage | Sürekli focused DPS ile yaklaşık TTK |
|---|---:|---:|---:|
| 0 | 24 | 18.0 | 1.09 sn |
| 2.5 | 35 | 20.7 | 0.92 sn |
| 5 | 51 | 23.4 | 0.77 sn |
| 7.5 | 74 | 26.1 | 0.65 sn |
| 10 | 108 | 28.8 | 0.54 sn |
| 12.5 | 157 | 31.5 | 0.45 sn |
| 15 | 228 | 34.2 | 0.38 sn |

**Önemli:** Bu TTK aritmetik yaklaşımdır; attack cadence, first-hit delay, crit, projectile miss, status tick, armor ve target switching dahil değildir. Gerçek observed TTK simülasyondan gelmeli.

Enemy HP artsa da normal düşman TTK'sının azalması hata değil, amaçlanan **power fantasy**. Baskıyı HP'yi sonsuza dek artırarak değil, density/attack pattern/elite composition ile sürdür.

## 8.3. Spawn rate ve enemy cap

```text
λ_base(t) = 2 + 0.25*t + 0.06*t^2   // enemies / second
```

| Dakika | Base spawn/sn | Hedef görünür enemy |
|---|---:|---:|
| 0 | 2.00 | 10–20 |
| 5 | 4.75 | 40–60 |
| 10 | 10.50 | 60–85 |
| 15 | 19.25 | 80–110 |

**Durağan yaklaşık kontrol:** `E[active enemy] ≈ spawn_rate × mean_alive_duration_seconds`. Örnek 10. dk, 10.5 spawn/sn, mean alive 7 sn → ~74 canlı enemy. Gerçek koşu non-stationary; spawn konumu, target clearing ve kiting etkiler.

Kill-reactive multiplier önerisi:

```text
K = last_5_sec_kills
S = last_5_sec_spawn_count
kill_to_spawn_ratio = (S == 0 ? 0 : K/S)
R_clear = 0.9 + 0.5*clamp(kill_to_spawn_ratio, 0, 1)
λ_requested = λ_base(t) * M_difficulty * R_clear
```

**Cap:** Normal/horde prototype `N_active <= 120` (cihaz profiline göre daha düşük olabilir). Cap dolunca yeni spawn yok. Biriken spawn credit `~1–1.5 saniyelik` budget ile sınırlandırılmalı, açıkta kalan talep birikip ani yüzlerce spawn'a dönüşmemeli.

**Açıklama:** `visibleEnemies`, `activeEnemies`, `simulatedFullAIEnemies`, `renderedLODEnemies` ayrı metriklerdir. Bu sayıları tek bir değişken olarak loglama.

## 8.4. Effective Horde DPS

Bir silahın nominal DPS'i bütün sürüye etkisini açıklamaz. AoE, bounce, uptime ve overkill dahil:

```text
DPS_horde = Σ_weapon (DPS_weapon_raw * H_weapon * U_weapon * (1 - O_weapon))
```

- `DPS_weapon_raw`: tek hedefe nominal damage throughput [HP/s].
- `H_weapon`: attack başına **eşdeğer farklı hedefe ulaşım** katsayısı.
- `U_weapon`: attack ve hedef bulabilme uptime [0,1].
- `O_weapon`: ölen hedefe gereksiz damage (overkill) oranı [0,1].

Örnek: raw DPS 100, H=4, U=.75, O=.15 → `100*4*.75*.85=255 HP/s` horde throughput.

**Sayım uyarısı:** Crit, proc, Burn, chain damage bazı weapon DPS toplamlarının içinde sayılıyorsa ayrıca eklenmemeli. `damage source ID`, `trigger source`, `originating weapon`, `proc chain depth` telemetrisiyle double count önle.

10. dakikada 199 focused DPS / 108 Grunt HP → ~1.84 kill/s *yalnız tek hedefte*. Çoklu hedef silahlarıyla toplam kills/s çok daha yüksek olabilir.

## 8.5. Enemy archetype tasarımı

| Enemy | Normal Grunt HP çarpanı | Damage çarpanı | Davranış |
|---|---:|---:|---|
| Grunt | 1.00x | 1.00x | Kütle baskısı |
| Runner | 0.65x | 0.80x | Hızlı flank |
| Bulwark | 3.00x | 1.20x | Kiting / yönlendirme |
| Ember Caster | 1.40x | 1.30x | Telegraphed ranged |
| Disruptor | 1.80x | 0.90x | Slow / area denial |
| Elite | Ayrı HP bütçesi | 1.5–1.8x | Özel mekanik |
| Boss | Ayrı faz bütçesi | 1.8–2.5x | Telegraph, positioning, skill |

## 8.6. Enemy tipine göre TTK hedefi

| Hedef | Beklenen combat TTK |
|---|---:|
| Grunt, erken oyun | 0.8–1.3 sn |
| Grunt, geç oyun | 0.3–0.7 sn |
| Runner | 0.4–0.9 sn |
| Bulwark | 2–4 sn |
| Ember Caster | 1–2.5 sn |
| Elite | 7–14 sn sustained damage exposure |
| Stage boss | 40–70 sn **elapsed encounter** |

**Önemli ayrım:** Bir elite'ın yalnızca ateş altında kaldığı anlar ile spawn→ölüm toplamı farklıdır. Boss fight sürelerinde elapsed / active damage window açıkça ayrı ölçülmeli.

## 8.7. Lv1 weapon TTK tablosu

Normal 24 HP Grunt, continuous single-target DPS yaklaşımı, crit/targeting dışarıda:

| Weapon | Nominal focused DPS | Yaklaşık TTK |
|---|---:|---:|
| Sword | 22.5 | 1.07 sn |
| Staff | 21.8 | 1.10 sn |
| Daggers | 23.3 | 1.03 sn |
| Iron Halo | 16.9 | 1.42 sn |
| Arc Conduit | 18.8 | 1.28 sn |
| Magma Mortar | 18.9 | 1.27 sn |
| Cinder Saw | 25.0 | 0.96 sn |
| Forge Bow | 20.8 | 1.15 sn |
| Gravity Anvil | 21.0 | 1.14 sn |
| Sanguine Sickle | 21.0 | 1.14 sn |
| Ember Mines | 18.7 | 1.29 sn |

Örn. 34 damage Magma Mortar 24 HP enemy'yi tek hitte öldürebilir ama 1.80 sn saldırı periyodundan dolayı ilk isabete kadar gecikme farklıdır. Continuous TTK ile discrete time-to-first-kill aynı şey değildir.

## 8.8. Karaktere göre TTK (uygun build ve ability)

| Karakter | Early Grunt | Elite | Boss |
|---|---:|---:|---:|
| Vanguard | 1.0–1.3 sn | 9–13 sn | 45–65 sn |
| Cinder Arcanist | 0.9–1.3 sn | 9–14 sn | 50–70 sn |
| Riftblade | 0.9–1.2 sn | 6–10 sn | 35–55 sn |
| Bastion | 1.2–1.6 sn | 11–16 sn | 55–75 sn |
| Voltwright | 1.0–1.4 sn | 9–14 sn | 45–70 sn |
| Sanguine | 1.1–1.5 sn | 5–9 sn | 30–55 sn |

**Farklılık gerekli:** Sanguine yüksek boss DPS karşılığında düşük hit budget ve timing riski taşır; Bastion daha yavaş ama güvenli. Her karakterin boss'u aynı saniyede kesmesi hedef değil.

## 8.9. Boss HP budget ve encounter tasarımı

```text
HP_boss = D_focus(t) * boss_damage_uptime * desired_damageable_seconds
```

Örnek t=5 dk, D_focus≈66 HP/s, boss uptime=.65, 35–45 damageable seconds:

```text
HP_boss ≈ 66 * .65 * (35..45) ≈ 1502..1931 HP
```

| Boss | Koşu zamanı | Başlangıç HP hedefi | Elapsed fight hedefi |
|---|---:|---:|---:|
| Forge Warden I | ~5 dk | 1,500–1,900 | 45–60 sn |
| Forge Warden II | ~10 dk | 4,000–5,800 | 45–65 sn |
| Final Forge Warden | ~14 dk | 9,000–13,000 | 45–70 sn |

Boss phase önerisi:

1. Telegraphed cone + lava lanes → positioning dersi.
2. Kısa vulnerability windows → saldırı zamanı ve skill penceresi.
3. Minion reinforcement + Forge Core hedefi → pressure, target priority.

Başlangıç telegraph süresi **0.8–1.2 sn**; düşük seviye cihazdaki input/visual clarity testleriyle ayarlanacak. Daha çok HP = daha tehditkâr boss değildir; zorlayıcı attack pattern şarttır.

## 8.10. Armor, damage alınca yaşam ve hit budget

```text
D_taken = D_raw * 100/(100 + ArmorRating)
H_nonlethal = max(0, ceil(PlayerHP/D_taken) - 1)
```

Örnek HP=120, ArmorRating=20, raw attack damage=30:

```text
D_taken = 30 * 100/120 = 25 HP
4 hit sonra HP=20; 5. hit lethal
H_nonlethal = ceil(120/25)-1 = 4
```

| Archetype | Ölmeden dayanabildiği normal hit hedefi |
|---|---:|
| Tank | 7–10 |
| Balanced | 4–6 |
| Glass cannon | 2–4 |

Bu hit budget boss heavy attacks için değil. Bir frame içinde birden fazla enemy contact hit ile anında ölüm oluşmamalı: global on-hit i-frame **0.45–0.60 sn** aralığını test et.

## 8.11. Crit / proc kuralları

Crit beklenen damage:

```text
M_crit = 1 + critChance * (critDamageMultiplier - 1)
```

Örnek CritChance=.30, CritDamageMultiplier=1.75 → `1 + .30*(.75) = 1.225x` expected damage.

Weapon proc coefficient:

```text
P_effective_proc = min(1, P_base_proc * weapon_proc_coefficient)
```

Örnek base proc .20, ultra-hızlı silah coefficient .35 → hit başına .07 / %7. Attack frequency arttıkça tüm silahların sınırsız on-hit proc üretmesini önle.

| Mekanik | İlk guardrail önerisi |
|---|---|
| Attack Speed | ~2.5x soft operating ceiling |
| Crit Chance | %65 sonrası diminishing returns; %80 hard cap |
| AoE Radius | 1.6x soft, 2.0x hard cap |
| Chain explosion | En çok 2 ek nesil |
| Proc recursion | Kaynaklara göre açıkça izinli/yasaklı |
| Aynı hedefe repeat hit | Weapon-specific; ilk test min 0.2 sn |
| Retaliation | <=2/s |
| Lifesteal | Per proc veya per-second heal cap |

Cap'leri gizleme; stat ekranında oyuncuya tooltip göster. **Proc graph cyclic dependency** olup olmadığını statik test ve runtime guard ile doğrula.

---

# 9. XP ekonomisi, teklif havuzu ve balance simulation

## 9.1. Hedef level pacing

| Zaman | Oyuncu level hedefi |
|---|---:|
| 1 dk | 3–4 |
| 5 dk | 11–13 |
| 10 dk | 22–25 |
| 15 dk | 35–40 |

Başlangıç XP maliyeti:

```text
XP_next(n) = 10 + 2*n + 0.12*n*n
n: current player level
XP_next: bir sonraki level için gereken XP birimi
```

| Level n | Gerekli XP |
|---|---:|
| 1 | 12.12 |
| 10 | 42 |
| 20 | 98 |
| 30 | 178 |
| 40 | 282 |

Bu tek başına pacing garantisi değildir. Düşman başı XP, kill rate, XP pickup, event reward ve Knowledge Tome etkileşimleri aynı simülasyonda ölçülmeli.

**İki telemetry metriği:** `spawnedXP` ve `collectedXP`; oran `xpCollectionRatio = collectedXP / spawnedXP`.

## 9.2. Offer category ağırlıkları

| Offer category | 0–2 dk | 2+ dk |
|---|---:|---:|
| New Weapon | %27 | %15 |
| Owned Weapon Upgrade | %28 | %35 |
| New Tome | %25 | %15 |
| Owned Tome Upgrade | %10 | %25 |
| General Stat / Relic-related | %10 | %10 |
| TOPLAM | %100 | %100 |

Seçilemeyen kategori `eligible=false` ise ağırlığı uygun seçeneklere dağıt. Üç teklif kartında aynı ID tekrarını önle veya açık bir repeat policy uygula.

Synergy-aware basit ağırlık:

```text
W_offer = W_base * (1 + 0.20 * min(matching_tags, 2))
```

Bu şekilde aynı tag sayısında +%20 veya +%40 teklif ağırlığı gelir, hard guarantee oluşmaz. Üç kartın da aynı build ekseninden gelmemesi için 1 wildcard/diversity slot test edilebilir.

### Önerilen deterministik offer pipeline

```text
1. Tüm içerikler ve mevcut run state'ten eligible pool oluştur.
2. Slot doluluğu, max level, banished ve exclusive koşullarını filtrele.
3. Early-game guarantee / pity kontrol et.
4. Offer category seç (weighted).
5. Tag synergy ağırlığı uygula.
6. Üç farklı kart ID'si seç (policy net olmalı).
7. Luck formülüyle rarity roll yap.
8. Rarity stat multiplier'ını uygula.
9. Seçim ekranında upgrade'in gerçek build etkisini göster.
10. seed, RNG state, candidate weights, final offers ve seçimi logla.
```

**Rarity RNG ve item category RNG ayrıdır.** Luck, yeni weapon teklifini manipüle etmek zorunda değildir.

## 9.3. Build diversity kabul kriterleri

| Metrik | Başlangıç acceptance |
|---|---|
| 5. dakikada >=2 weapon + >=2 tome | >=%85 run |
| 10. dakikada >=1 anlamlı synergy | >=%80 run |
| 15. dakikada >=1 evolution | >=%75 tamamlanan run |
| İlk 10 dk sıfır etkili kart | <%5 teklif |
| En çok seçilen weapon'ın conditional pick share'i | <%40 (eligible teklifler içinde) |
| Her karakterde >=3 viable build family | Evet |
| Tek build'in her karakterde tartışmasız meta olması | Hayır |

Build identity için canonical summary üret: karakter, sırasız weapon IDs + levels, tome IDs + levels, evolution, top damage sources, dominant status tags ve ability use. Sadece aynı kombinasyonun farklı sırayla seçilmesini ayrı build sayma.

## 9.4. Bot cohort'ları

| Bot | Davranış modeli | Test amacı |
|---|---|---|
| Auto Farmer | Hareket eder, aktif skill yok, basit upgrade | Accessibility / Skill 1 |
| Casual | XP toplar, basit dodge, makul kart seçer | Tipik kullanıcı |
| Skilled | Telegraph okur, cooldown/route yönetir | Skill ceiling ve adalet |
| Optimizer | Route ve build'i yüksek performansla seçer | Exploit, baskın meta |

İlk pilot: `character × difficulty × bot` başına **2.000 deterministic seed**. Denge finaline yaklaşırken **10.000 seed**. Bağımsız 10k Bernoulli koşusunda %50 win rate için yaklaşık %95 CI yarı genişliği ±1 yüzde puandır; aynı seed'lerle paired tests ve otokorelasyon dikkat ister.

Aynı seed altında bot policy karşılaştırmalarını eşleştir; RNG stream'lerini `spawn`, `loot`, `rarity`, `enemy behavior` için mümkünse ayır. Bir build seçimi değiştiğinde bütün random sequence'ın kayması A/B yorumunu zorlaştırabilir.

## 9.5. Telemetry önerisi

En az şu olayları logla: `run_started`, `director_state_changed`, `combat_sample`, `offer_generated`, `offer_selected`, `level_up`, `boss_started`, `boss_ended`, `player_hit`, `player_died`, `extract_selected`, `run_ended`, `performance_sample`.

Örnek **sentetik** 10 saniyelik aggregate payload:

```json
{
  "event": "combat_sample",
  "schemaVersion": 1,
  "runId": "local-run-id",
  "seed": 48127,
  "gameTimeSec": 602,
  "characterId": "vanguard",
  "difficultyId": 1,
  "playerLevel": 24,
  "focusDps10s": 192.4,
  "hordeDps10s": 835.1,
  "killsPerSec10s": 7.8,
  "spawnedPerSec10s": 10.5,
  "activeEnemies": 74,
  "visibleEnemies": 61,
  "hp": 104,
  "maxHp": 130,
  "attackSkillUses": 18,
  "collectedXpRatio": 0.62,
  "cpuFrameMsP95": 18.4,
  "gpuFrameMsP95": 20.1
}
```

Bu değerler gerçek Cryptforge sonucu değildir. Her hit'i remote analytics'e göndermek yerine combat damage source'larını lokal agregasyonla 5–10 saniyelik sample'a dönüştür; deterministic replay için lokal event trace opsiyonel olsun.

## 9.6. Balance quality gates

| Metric | İlk acceptance |
|---|---|
| İlk combat | <=10 sn |
| Combat uptime | >=%75 |
| Kat süreleri | P50 4–6 dk |
| Boss'un 5 sn'de ölmesi | Normal cohort'ta sıfıra yakın |
| Boss elapsed | P50 45–65 sn |
| Normal enemy observed TTK | Zaman dilimi target band'e yakın |
| Sıradan enemy multi-hit instakill | Sıfıra yakın |
| Luck Tome almadan viable run | Her karakter |
| Infinite proc loop | 0 |
| Aşırı tek-source DPS anomali | P99 inceleme ve kök neden |
| Character fairness | Skill-adjusted win rate sapması çoğunlukla ±10–15 pp |
| FPS ve thermal | Cihaz cohort'una göre |

**Tuning kök neden sırası:** Hit frequency → attack uptime → targeting → collision → overkill → tome scaling → karakter etkileşimi → boss/horde ayrımı → en son raw damage buff/nerf.

---
# 10. Difficulty, elite, boss ve Final Swarm

## 10.1. Difficulty modları

| Mod | Enemy HP | Enemy Damage | Spawn multiplier | Hedef |
|---|---:|---:|---:|---|
| Apprentice | 0.85x | 0.80x | 0.85x | Öğrenme |
| Forged | 1.00x | 1.00x | 1.00x | Standart |
| Tempered | 1.20x | 1.15x | 1.10x | Tecrübeli |
| Inferno | 1.45x | 1.30x | 1.18x | Zorlayıcı |
| Abyssal | 1.80x | 1.50x | 1.25x | Yüksek beceri |

Çarpanlar normal enemy curve'e uygulanır; boss HP encounter-specific balance. Üst modlarda salt HP yerine yeni attack combinations, daha sert ancak okunabilir telegraph ve daha zor positioning olmalı.

## 10.2. Hedef win rate (beceri cohort'u tanımlanmalı)

| Difficulty | Hedef win rate |
|---|---:|
| Apprentice | %65–80 |
| Forged | %40–60 |
| Tempered | %25–40 |
| Inferno | %10–25 |
| Abyssal | %5–15 |

Bunlar evrensel piyasa benchmark'ı değil, **ilk tasarım cohort hedefleri**. Acemi/ustalaşmış oyuncular için farklılık ölç. Sadece bütün kullanıcıları bir arada ortalamak yanıltıcıdır.

Karakter adaleti:

```text
ΔWR_character = WR(character, skilled, fixed difficulty, comparable meta)
              - median(WR(all characters, skilled, same setting))
```

Başlangıçta çoğu mod için ±10–15 percentage point sapma değerlendirilebilir; challenge character'ın erken öğrenme kaybı kabul edilebilir, maksimum ustalıkta sistematik kayıp kabul edilmez.

## 10.3. Elite tasarımı

Elite sadece yüksek HP'li Grunt olmamalı. En az bir özgün saldırı kimliği taşısın:

- **Area denial:** güvenli konumların kısa süre değişmesi.
- **Target prioritization:** oyuncuya tehlikeli düşmanı önce öldürme nedeni.
- **Build counterplay:** ör. geçici AoE resistance ama complete immunity değil.

İlk prototipte aynı anda en çok 2 elite, en ileri modda 3 değerlendirilebilir. Active ability'leri tamamen susturup oyuncuyu mekaniksiz bırakma.

## 10.4. Final Swarm ve Extract

| Final zamanı | Pressure / ödül olayı |
|---|---|
| 0 sn | Stage boss ölür, Extraction Gate açılır |
| 15 sn | Horde density artar |
| 30 sn | Ranged enemy pressure artar |
| 45 sn | İlk bonus Extract ödül eşiği |
| 60 sn | Elite reinforcement |
| 90 sn | İkinci ödül eşiği |
| 120 sn+ | Endless survival, artan tehdit, sınırlı ödül |

Meta reward multiplier örneği:

| Extract tercihi | Meta reward |
|---|---:|
| Kat 3 boss sonrası hemen çıkış | 1.00x |
| Final 45 sn | 1.15x |
| Final 90 sn | 1.30x |
| Final 120 sn | 1.40x |
| 120 sn sonrası | Diminishing returns / cap |

Bu tasarım 10 saat Final Swarm'da farming yapmayı zorunlu hâle getiren economy exploit'ini engeller. Normal oyuncu için hedef kararı **45–90 saniyede çıkış**, endless isteyen için ileride ayrı leaderboard mode.

---

# 11. Run Item, Permanent Relic ve Heat/Overforge

## 11.1. İlerleme katmanları

| Kaynak | Nasıl bulunur | Rol |
|---|---|---|
| Weapon | Level-up / Forge | Damage kaynağı |
| Tome | Level-up / Shrine | Global stat/scaling |
| Run Item | Chest/Elite/keşif | Davranışsal pasif ve proc |
| Permanent Relic | Meta progression/loadout | Sınırlı kalıcı sidegrade |

Run Item ile Permanent Relic'i aynı havuzda karıştırma: koşu başında sabit meta gücü, rastgele build keşfini boğabilir.

## 11.2. Sekiz örnek Run Item

| Item | Önerilen etki | En iyi synergy |
|---|---|---|
| Ember Gear | Burn kill sonrası %15 küçük explosion chance | Staff / Mortar |
| Conductive Nail | Shock'lu hedefe +%12 chain damage | Arc Conduit |
| Split Crucible | Her 6 AoE hit sonrası mini projectile | Staff / Anvil |
| Blood Valve | Bleed tick'lerinin %5'ini capped heal'e çevir | Daggers / Sanguine |
| Iron Memory | Block sonrası 3 sn +%12 Damage | Vanguard / Bastion |
| Chain Grease | Crit sonrası 2 sn +%10 Attack Speed; max 2 stack | Daggers / Bow |
| Scavenger Coil | 40 kill sonrası yakın XP'yi çek | XP / Pickup |
| Forge Catalyst | Seçilmiş evolution'ın status davranışını güçlendir | Evolution build |

Her item data schema'sında en az `source`, `trigger`, `procCoefficient`, `internalCooldown`, `stackRule`, `cap`, `damageTags` tanımla.

## 11.3. Heat / Overforge

**Farklılaşma mekanizması:** Foundry lore ve gameplay birbirine bağlansın. Ancak ilk vertical slice'ta tamamen yeni global Heat barı ve karmaşık resource metası ekleme.

Başlangıç modeli:

- Normal Forge → beklenen standart upgrade; Difficulty cezası yok.
- **Overforge** seçeneği → +1 özel upgrade veya ekstra item seçimi.
- Karşılığı → bu katta enemy pressure +%10.
- Üç Overforge → Heat-themalı nadir Relic teklifine erişim.
- Artan baskıyı stat ekranında açık göster; gizli difficulty artırma kullanma.

İleride Heat-specific character/weapon/items açılabilir. Önce opsiyonel riskin oyun açısından gerçekten anlamlı karar üretip üretmediğini ölç.

---

# 12. Mevcut Cryptforge sistemlerine karar tablosu

| Mevcut sistem | Karar | Değişiklik |
|---|---|---|
| Tek büyük Foundry | KORU | Continuous enemies + meaningful nodes |
| İzometrik portrait camera | KORU | Görüş alanı + readability test |
| Tek parmak hareket | KORU | Active skill UX deneyleri |
| Auto attack | KORU | Targeting, hit rate ve feedback geliştir |
| 3 kısa combat room | DEĞİŞTİR | Sürekli combat + 45 sn objectives |
| Forge room | GELİŞTİR | Weapon/tome/evolution/item kararları |
| Elite room | GELİŞTİR | Opsiyonel yüksek risk/ödül |
| Forge Warden | YENİDEN TASARLA | 45–70 sn, telegraph ve phase |
| Final Swarm/Extract | KORU + GENİŞLET | Reward thresholds, cap |
| Staff/Daggers/Sword | KORU | Weapon tuning referansı |
| Forge Burst | KORU | Vanguard'a özel aktif |
| 9 generic stat cards | DÖNÜŞTÜR | Weapon/Tome/General stat kategori |
| Evolution +%20 Difficulty | KALDIR | Opsiyonel Overforge |
| Shrine +%10 Difficulty | DEĞİŞTİR | Konumsal risk ve bilinçli pressure kabulü |
| Pickup radius 1 birim | ARTIR/TEST | 1.8–2.5 m başlangıç deney |
| Stamina ile normal hareket | KALDIR/SINIRLA | Sadece dash/sprint/special maneuver |
| Lava pit 7 sn | KORU + GELİŞTİR | Telegraph, enemy damage, Burn, CC abuse guard |
| Permanent Relics | KORU | Sınırlı loadout ve sidegrade |
| Gold ile unlock | KORU + GÖREV | Quest-gated content |

## 12.1. Stamina

Continuous kiting ile normal movement-stamina çatışır. Ücretsiz normal yürüyüş; stamina yalnız dash, sprint veya karaktere özgü manevra için. Bütün build'leri yanlış konumlanma yerine normal hareket edememe yüzünden cezalandırma.

## 12.2. Lava pit

Her 7 saniyelik patlama için başlangıç test parametreleri:

- **1.2 sn önce belirgin telegraph**.
- Oyuncuya damage, düşmana damage/Burn.
- Sonrasında **1.5 sn temporary hazard**.
- Environmental kill'ler damage attribution ve XP üretiminde açık policy kullansın.
- Düşmanları sonsuza kadar sabitleyip limitsiz farm yaptırma; stun/root duration, CC resistance, reposition test et.

## 12.3. XP pickup

Öncelikli A/B: 1.0 m ↔ 2.2 m. İlk hedef doğal combat rotasında XP toplama oranı %55–75. Uzakta biriken küçük shard'ları cluster/merge et; her shard'ın full physics objesi olmasın. Magnet Tome sayesinde ciddi ilerleme hissi gelmeli, fakat Magnet olmadan oyun oynanamaz kalmamalı.

---

# 13. Meta progression ve unlock görevleri

## 13.1. Karakter açma

| Karakter | Örnek unlock condition |
|---|---|
| Vanguard | Varsayılan açık |
| Cinder Arcanist | Toplam 1,000 Burn uygulama + gold satın alımı |
| Bastion | Toplam 2,000 damage mitigate/block |
| Voltwright | Tek koşuda 150 Shock chain |
| Riftblade | Elite'ı 30 sn altında kes + Kat 2 extract |
| Sanguine | <%25 HP ile boss yen |

Bu quest count'lar **balancing adayıdır**; gerçek completion distribution'a bakıp ayarla. Unlock achievement, oynanış sistemi öğretmeli; yalnızca büyük kill grind olmamalı.

## 13.2. Weapon unlock örnekleri

- **Arc Conduit:** 500 Shock kill.
- **Magma Mortar:** Lava ile 200 düşman öldürme.
- **Iron Halo:** Vanguard ile Forge Warden boss yen.
- **Sanguine Sickle:** Sanguine karakterini aç.
- **Gravity Anvil:** Bir Elite'a tek hitte 500 damage.

## 13.3. Meta ekonomisinin sınırı

Üç tier önerilir:

1. **Content Unlock:** Character, Weapon, Tome, Item.
2. **Utility Unlock:** Birkaç reroll/banish hakkı, Forge seçeneği.
3. **Permanent Sidegrade Relic:** Avantaj karşılığında başka sınırlı dezavantaj.

Kalıcı başlangıç power bonusları küçük kalsın (örnek toplam +%10 HP veya +%5 Damage); aksi hâlde karakter dengesi, meta seviyesine göre yapay sapar. Her balance deneyinde `accountMetaPower`, `unlockedContent`, `relicLoadout` logla.

---

# 14. Unity 6 / Android mobil performans ve UX

## 14.1. Cihaz profili hedefleri

**[CRYPTFORGE—ÖNERİ]** Aşağıdakiler gerçek benchmark değil; ölçüm için ilk çerçeve:

| Sınıf | Sustained FPS | Normal görünür/aktif enemy budget |
|---|---|---:|
| Low-end Android | 30 | 50–70 |
| Mid-range Android | 30; opsiyonel 60 | 80–100 |
| High-end Android | 60 | 100–120 |

15 dakikalık thermal test şart. Güçlü cihazda kısa benchmark'ta 60 FPS görmek sustained 15 dakika 60 FPS garanti etmez.

## 14.2. Teknik performans taktikleri

- Enemy, projectile, damage number ve XP için **object pooling**.
- Aynı mesh/material grupları için **instanced rendering**.
- Yakın hedef araması / AoE için **spatial hashing/grid**, uygun koşullarda overlap query optimizasyonu.
- Her enemy pahalı AI update'ini her frame yapmak yerine **staggered updates**.
- Çok sayıda basit enemy için NavMesh yerine maliyet/kaliteye göre **cheap steering**.
- Crit/proc zincirlerinden bağımsız **VFX emission cap**; kritik boss telegraph VFX'lerini asla gizleme.
- **Damage numbers aggregation** ve uzak animasyonlarda reduced update frequency.
- `activeEnemies`, `visibleEnemies`, `LOD-tier`, `CPU`, `GPU`, `GC alloc`, `P95/P99 frametime` ayrı kaydedilsin.

## 14.3. Quality gates

| Metric | İlk hedef |
|---|---|
| 30 FPS modu | P95 frametime <=33.3 ms |
| 60 FPS modu | P95 frametime <=16.7 ms |
| >250 ms freeze | Sıfıra yakın |
| 15 dk thermal | Ağır thermal throttling olmadan kabul edilir performans |
| Enemy steady-state GC | Minimal allocation |
| Boss telegraph readability | 120 enemy VFX altında testle doğrulanmalı |

**Not:** FPS P95 eşiklerinin sağlanması tek başına %100 frame teslim garantisi değildir; P99, jank count ve thermal ayrıca ölç. Cihaz yavaşsa ilk olarak VFX/LOD/animation maliyetini azalt; gameplay challenge'ı sessizce değiştirme.

## 14.4. Tek parmak/portrait UX

- Hareket her zaman erişilebilir ana control olmalı.
- İlk skill büyük, başparmak erişimine uygun buton; ikinci skill otomatik veya context action olabilir.
- Level-up üç kart tek elde okunabilir boyutta ve net sayısal delta ile görünmeli.
- Attack feedback ve enemy telegraph, kalabalık ortasında ayırt edilebilir olmalı.
- Skill 1 karakterlerinin auto-hit farm yapabilmesi beginner UX avantajı.
- Device DPI, safe area, farklı aspect ratio ve parmak occlusion test edilmeli.

---

# 15. Aşamalı uygulama yol haritası ve Vertical Slice

## 15.1. İlk oynanabilir hedef: V0.1

**Tek Foundry katı, yaklaşık 5 dakika, Vanguard, sürekli spawn, tek boss.** Yalnız aşağıdaki scope:

| İçerik | V0.1 scope |
|---|---|
| Character | Vanguard |
| Weapons | Sword, Staff, Daggers |
| Tomes | Force, Tempo, Reach, Precision |
| Enemy | Grunt, Runner, Bulwark |
| Elite | 1 |
| Boss | Forge Warden |
| Progression | Level-up, 3-card offers, ilk synergy |
| Run | Tek kat + optional extract |
| Technical | Deterministic seed, combat/performance telemetry |

**V0.1 başarı soruları:**

1. Oyuncu düşmansız kısa dalgalarla mı, yoksa kesintisiz baskıyla mı oynuyor?
2. Upgrade sonrası yalnız number mı artıyor, yoksa kill pattern da değişiyor mu?
3. Boss positional/ability karar gerektiriyor mu?

Bu üçüne yanıt alınmadan çok karakterli genişletmeye geçme.

## 15.2. Fazlar

| Faz | Oynanabilir teslimat | Ana kabul ölçümü |
|---|---|---|
| **0. Horde Prototype** | Tek zone, 1 character, 3 weapon, 4 tome, boss, continuous spawn | Combat uptime, TTK, FPS |
| **1. Balance Foundation** | 4 rarity, Luck, tag-aware offer, ilk Evolution, enemy roles | DPS P10/P50/P90, dead offers |
| **2. Full Run** | 3 kat, 6 node, Forge, elite, final swarm, extract | 13–16 dk; boss duration |
| **3. Character Expansion** | 6 characters, 11 weapons, 12 tomes | Mastery / win rate |
| **4. Meta** | Quests, unlock, permanent sidegrades | Unlock pacing, retention proxy |
| **5. Mobile Polish** | Thermal/LOD/readability and devices | P95/P99, freezes, memory |

Her fazdan sonra regression simulation çalıştır; son eklenen silahın mevcut frame-time ve build balance'ı bozmaması gerekir.

## 15.3. Öncelikli A/B deneyleri

| Konu | A | B | Metrik |
|---|---|---|---|
| Combat pacing | Kısa ayrı wave | Continuous director | Active combat%, run continuation |
| XP pickup | 1.0 m | 2.2 m | Pickup ratio, level pacing |
| Stamina | Normal yürüyüş harcar | Yalnız dash harcar | Death causes, time spent moving |
| Evolution | +%20 auto Difficulty | Optional Overforge | Pick rate, win rate |
| Rarity | Flat odds | Luck weighted | Luck pick share, win rate |
| Boss | Mevcut HP/pattern | DPS-budget + telegraph | Boss TTK, player hits |

Önce deterministic simulator, sonra insan playtest'i. Botlar oyuncunun görsel karmaşayı, yanlış basışı ve boss telegraph okunabilirliğini tam temsil etmez.

---

# 16. Risk kaydı

| Risk | Erken sinyal | Mitigation |
|---|---|---|
| Exponential proc damage | P99/P50 DPS aşırı yükseliyor | Internal cooldown, proc coefficient, recursion cap |
| Kötü RNG softlock | P10 TTK aşırı uzun, usable offers yok | Early weapon guarantee, ağırlık tuning |
| Luck zorunlu | Luck pick / win rate aşırı yüksek | Diminishing returns, opportunity cost |
| Dominant character | Skilled-adjusted win rate farkı | Pasif/ability conditions tuning |
| Skill-1 ile bütün yüksek modlar kolay | Ability-off bot high win rate | Boss-specific mechanics |
| FPS düşüşü | Active enemy/VFX ile frame-time ilişki | Pooling, stagger, LOD, VFX cap |
| Koşular uzuyor | Median normal run >20 dk | Stage/objective cadence, extract |
| Combat yine kopuk | Combat active <75% | Director states/route |
| Endless meta farming | Currency/minute limit üstü | Reward cap/diminishing returns |
| Yeni oyuncu hemen ölüyor | İlk 60 sn death spike | Ramp / tutorial / first upgrade |
| Auto-target yanlış | Miss/downtime/overkill | Targeting, collision, range |
| Zor unlock yüzünden tıkanma | Median unlock session sayısı yüksek | Quest / alternatif şart |

Her güçlü build nerf gerektirmez. Hedef *nadir güçlü koşuların var olmasıdır*. Nerf için esas sinyaller: boss mechanics atlanıyor, infinite proc, FPS çöküyor, tüm viable build'ler tek bir kombinasyonda birleşiyor, meta progression anlamsızlaşıyor.

---

# 17. Claude Code'a uygulama handoff'u

> **Talimat:** Bu belgeyi doğrudan koda çevirmeden önce mevcut Cryptforge Unity repository'sinin sistemlerini incele ve buradaki tasarım önerilerini gerçek architecture/classes/assets ile eşleştir. Bu dosyadaki sayıları proposal olarak işle, production'ın gerçek durumunu önce ölç. Var olan component'leri gereksiz yeniden yazma.

## 17.1. Önerilen ilk mühendislik sırası

1. **Baseline kaydet:** Mevcut 1.5–2 dakikalık katlar, wave TTK, boss encounter, hero HP curve, level pacing, FPS, spawned/collected XP, stamina ve shrine kullanımı. Eski mechanic'leri silmeden `baseline_before_rework.json` gibi measurement çıktı üret.
2. **Config-driven balance katmanı:** Weapon/tome/rarity/enemy/stage/difficulty parametrelerini data-driven oluştur (mevcut projeye uygun ScriptableObject/JSON yaklaşımını incele). Magic number'ları azalt.
3. **Director + spawn:** Sürekli spawn, state machine, active enemy cap, spawn-credit cap, kill-reactive scaling. Sandbox'da tek kat.
4. **Three-card offer pipeline:** Uygunluk, weapon/tome ayrımı, slot, max level, category weights, Luck roll, deterministic seed.
5. **Damage attribution:** Focused/Horde DPS, effect tags, proc guard, targeted hit count, overkill, player hit logging.
6. **V0.1 boss:** 45–60 sn hedef, telegraph, minion pressure ve geçerli extract.
7. **Simulation regression:** Seed batch ve report; P10/P50/P90, observed TTK, deaths, run duration, FPS.
8. **A/B toggles:** Kısa wave ↔ continuous; stamina; radius; evolution penalty.
9. **Playtest:** Düşük/orta cihaz, 5/15 dk endurance; yalnız bot sonuçlarıyla karar verme.
10. **Expansion:** Ardından diğer karakterler, item'lar, tome kataloğu ve meta.

## 17.2. İlk PR / görev kırılımı

```text
PR-01: Instrumentation (no balance changes)
  - Combat sample + run timeline + boss/death/upgrade events
  - Performance sample (CPU/GPU frame, active/visible enemy)
  - Deterministic seed exposed to local run/session
  - Before/after baseline snapshot

PR-02: Continuous Horde Director behind feature flag
  - Stage director states
  - Spawn cap and spawn-credit cap
  - Enemy role ratios, telegraph readability
  - Regression tests for active cap / no burst spawn

PR-03: Weapon/Tome category separation
  - New/owned offer category rules
  - 3 weapon + 3 tome slots, Lv8 cap
  - Filter and weighted offer unit tests
  - Compatible with current level-up UI

PR-04: Rarity/Luck subsystem
  - Four weights, formula normalization, soft cap
  - Seeded RNG and sampling tests
  - Level-up card representation and delta tooltips

PR-05: Boss pacing / Evolution / Extract
  - DPS-budget boss initial HP + mechanics
  - Remove automatic evolution difficulty penalty
  - Introduce optional Overforge as experiment
  - Final swarm timing + extract rewards

PR-06: Balancing harness
  - Four bot policies
  - 2k-10k seed runner (as feasible)
  - Markdown/CSV/JSON report: P10/P50/P90, TTK, win rate
  - Versioned config + seed replay + quality gates
```

**Scope koruması:** Bu bir örnek görev planıdır. Gerçek kodu incelemeden sınıf adı, dosya yolu veya Unity component API'si uydurma. İlk PR gerçek repo mimarisine göre küçük ve güvenli olmalı. Existing telemetry/simulation altyapısı varsa onu genişlet, ikinci paralel sistem icat etme.

## 17.3. Raporlardan üretilecek minimum tablolar

- `time_bucket_summary`: 30 sn aralıkları; P10/P50/P90 player DPS, target HP, enemy density, XP, FPS.
- `character_difficulty_matrix`: bot×character×difficulty; win rate, average extract time, boss TTK, deaths.
- `weapon_effectiveness`: damage attribution, boss/horde efficiency, proc count, hit ratio, overkill.
- `rarity_sampling`: Luck=0/10/25/50 için observed vs expected rarity frequency.
- `offer_health`: dead choices, duplicates, early weapon/tome availability, synergy incidence.
- `mobile_performance`: device tier × active enemies × VFX load × frame-time percentiles.

## 17.4. Matematiksel unit/property test checklist

- Rarity probabilities `sum(P_i)=1` (numeric epsilon içinde), her `P_i>=0`.
- Luck arttığında Gold odds azalmıyor; iyi tanımlanmış soft cap davranışı.
- 3 kart probability formülü yalnız independence varsayımıyla kıyaslanıyor.
- Max weapon/tome slot aşılmıyor; level cap aşılamıyor.
- Offer pool'da ineligible item yok; boş kategori weighted draw crash üretmiyor.
- Damage modifiers aynı bucket'ta additive; farklı bucket'larda kasıtlı multiplicative.
- Proc recursion ve per-target repeated hit sınırlı.
- Director active enemy cap, spawn debt cap ve `S=0` division guard sağlam.
- ArmorRating=0 olduğunda damage değişmiyor; nonlethal hit formula tam HP multiple edge case'lerinde doğru.
- XP curve artan, negatif değil, UI ile telemetry aynı units.
- Deterministic run aynı config/seed/bot policy altında tekrar üretilebilir.

## 17.5. Asıl karar kapısı

**V0.1'i geçerli saymak için:** Tek Vanguard/Foundry senaryosunda 4–6 dk oynanış, combat uptime ≥%75, üç silahın farklı rolü, en az bir belirgin synergy, güvenilir 45–60 sn civarı Warden ve seçilebilir Extract; bunların yanında hedef cihazda kabul edilebilir frame-time. Bu sağlanmadan 6 karaktere genişleme gerekmiyor.

---

# 18. Kaynakça, bağlantılar ve doğrulama sınırları

## 18.1. Resmî / yüksek öncelikli kaynaklar

1. **Megabonk — Steam mağazası**: https://store.steampowered.com/app/3405340/Megabonk/  
   Kullanım: core gameplay, auto attacks, stage/loot/upgrade tanımı, oyuncu yorumlarına erişim.
2. **Megabonk — Developer announcements / patch notes**: https://steamcommunity.com/app/3405340/announcements/  
   Kullanım: patch bazlı Luck, XP, character, final swarm, performance ve item adjustments.
3. **Brotato — Developer factsheet**: https://blobfishgames.notion.site/Brotato-cb88a14520ba4703ae9aed4ab438427c  
   Kullanım: short-wave/6-weapon/store karşılaştırması.
4. **20 Minutes Till Dawn — Steam**: https://store.steampowered.com/app/1966900/20_Minutes_Till_Dawn/  
   Kullanım: aim + ability/build kıyası.
5. **Halls of Torment — Godot showcase**: https://godotengine.org/showcase/halls-of-torment/  
   Kullanım: Dark-fantasy survival/quest structure karşılaştırması.
6. **Unity 6 — Graphics performance profiling**: https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling  
   Kullanım: profiling ve performans planı.
7. **Android / Unity Adaptive Performance**: https://developer.android.com/games/engines/unity/unity-adpf  
   Kullanım: CPU/GPU/thermal aware mobil optimizasyon.

## 18.2. Topluluk, veri listeleri ve analizler

8. **SurvivorsDB — Megabonk Characters**: https://survivorsdb.com/megabonk/characters  
   Kullanım: character lineup, passive ve starting weapon; patch kontrolü gerekli.
9. **SurvivorsDB — Weapons**: https://survivorsdb.com/megabonk/weapons  
   Kullanım: weapon list/stat identities.
10. **SurvivorsDB — Tomes**: https://survivorsdb.com/megabonk/tomes  
    Kullanım: tome list/history; stat değişikliklerinde patch notları öncelikli.
11. **Megabonk Stats / Luck & Difficulty**: https://megabonk.org/guides/stats/  
    Kullanım: topluluk formül çıkarımları; **resmî doğrulama değil**.
12. **Megabonk Info — Difficulty**: https://www.megabonkinfo.org/player-stats/difficulty  
    Kullanım: spawn cap ve kill-reactive spawn analizi; **sürüm bağımlı topluluk analizi**.
13. **Megabonk research repository**: https://github.com/lukeod/megabonk_research  
    Kullanım: reverse-engineering / upgrade ve rarity araştırması; sürüm, kapsam ve methodology doğrulanmalı.
14. **Weapon and Tome Upgrade Guide (Steam)**: https://steamcommunity.com/sharedfiles/filedetails/?id=3576905235  
    Kullanım: tarihsel weapon/tome değerleri; güncel sürüm yerine kullanma.
15. **Reddit — late-game / RNG eleştirisi**: https://www.reddit.com/r/MegabonkOfficial/comments/1uugici/actually_playing_megabonk/  
    Kullanım: niteliksel topluluk görüşü, mekanik kanıtı değil.
16. **Reddit — weapon tier list meta tartışması**: https://www.reddit.com/r/MegabonkOfficial/comments/1q4pv9v/megabonk_weapon_tierlist_v1049/  
    Kullanım: weapon viability ve oyuncu algısı; öznel ve patch-dependent.

> **Kaynak notu:** URL'ler önceki araştırma raporunda verilen referansların toplanmış hâlidir. Bu dosyayı oluşturma adımında tüm bağlantılara yeniden erişim ve link sağlık kontrolü yapılmadı. Bazı fan kaynakları zamanla değişebilir veya erişilemez olabilir. **Açıklamalar her bir sayfadan birebir alıntı değildir; sentezdir.** Claude, doğrulanmamış/patch-dependent iddiaları production design'ın zorunlu girdisi kabul etmemeli.

## 18.3. Özellikle doğrulanmamış bulgular

- Megabonk için **tek bir resmî** Luck→rarity odds formülü.
- Bütün güncel weapon ve tome Common upgrade değerleri.
- Her Tier ve haritanın değişmez kesin zaman çizelgesi.
- Megabonk enemy HP/damage/speed için tüm stage/time formülleri.
- Karakterlere göre resmî ölçülmüş skill ceiling, TTK veya win rate.
- Tüm fan wikilerdeki slot, item stacking ve character passive rakamlarının güncel sürüm doğruluğu.
- Topluluk kod analizlerinde belirtilen 550/400/300 gibi enemy cap sayılarının tüm oyun versiyonları için geçerliliği.

---

# SONUÇ — Tasarım pusulası

**Cryptforge: Foundry katlarını keşfederken sürekli sürü baskısı altında hareket edilen, weapon+tome sinerjileriyle kill pattern'i değişen, aktif yetenek yönetimi karaktere göre zorlaşan ve büyük güç karşılığında isteğe bağlı risk alınabilen mobil roguelite.**

Ürün başarısını ilk anda içerik sayısıyla ölçme:

- **1. dakika:** Sürekli baskı var, XP toplama ve konumlanma gerekiyor.
- **3. dakika:** İlk anlamlı weapon+tome synergy kuruldu; kill pattern değişti.
- **5. dakika:** Sürü temizleme güçlendi ama boss telegraph'ını yanlış okursan ölebilirsin.

**İlk üç mühendislik önceliği:** (1) continuous combat director, (2) weapon/tome ayrımı ve synergy, (3) DPS/TTK/survival/performance simülasyonu. Sonraki sistemler bunların üzerine eklenmeli.

**Belge sonu.**
