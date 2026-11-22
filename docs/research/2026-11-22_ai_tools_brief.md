# Araştırma brifi — Cryptforge'un animasyon, efekt, karakter ve boss üretimi için yapay zekâ araçları (2026-11-22)

> Bu brifi ChatGPT Deep Research'e (ya da benzer bir araca) olduğu gibi verebilirsin. Sonucu bana getirdiğinde, seçtiğimiz
> araçları üretim hattına bağlarım. Cevapların **Kasım 2026 itibarıyla güncel** olması önemli; eski bilgi işe yaramaz.

## Bağlam: oyun ve şu anki üretim hattı

- **Oyun:** Cryptforge, Android için dikey (portrait) bir roguelite. Kahraman kalabalık düşmanlarla savaşır (ekranda 30-40
  düşman). Üç kat ve her katın sonunda bir boss var. Hedef cihaz Samsung Galaxy S23; düşük donanımlı telefonlarda da akıcı
  kalmalı.
- **Motor:** Unity 6000.0 (Unity 6 LTS), Built-in Render Pipeline, eski uGUI arayüzü.
- **Görsel yapı (hibrit):**
  - Ortamlar düşük poligonlu 3D mesh ve boyalı dokular. Kat başına bir vertex bütçesi var (~174 bin).
  - Karakterler ve düşmanlar önceden render edilmiş sprite kareleri:
    - Vanguard elle boyanmış karelerden oluşuyor; ana çizim bir görsel üretme aracından.
    - Ilya, Blender'da script ile modellenip render edilmiş "3D-toon" kareler.
    - Düşmanlar sprite setleri.
  - Efektler Unity'nin parçacık sistemi, kodla kurulan LineRenderer'lar ve birkaç flipbook doku.
- **Ton:** "dark fantasy + dark cute". Kül, yorgun makineler, sönmüş güneş; üzgün küçük metal ruhlar. Lore ayrıntıları
  docs/44'te.
- **Üretimi kim yapıyor:** tek kişilik bir geliştirici (ben) ve bir yapay zekâ kod ajanı (Claude). Ajan Python ve Blender
  scriptleri çalıştırabiliyor, Unity'yi batch modda kullanabiliyor ve bir aracın API'si ya da komut satırı (CLI) varsa onu
  da kullanabiliyor. Bu yüzden **API'si, CLI'si ya da yerelde çalışan modeli olan araçlar** bizim için çok daha değerli.
- **Bilgisayar:** Windows 11. Ekran kartı: **[SEN DOLDUR: GPU modeli ve VRAM]**. Bu bilgi, yerel modellerin (Stable
  Diffusion/Flux türü, yerel 3D üretimi) çalışıp çalışmayacağını belirler.

## Asıl ihtiyaç

1. **Karakter animasyonları.** Her kahramanın bekleme, koşma, saldırı (üç silah yuvası için ayrı küçük hareketler), darbe
   alma, ölüm, seviye atlama ve yetenek kullanma kareleri. Yaklaşık 8 yönde ya da bizim kamera açımızda, 6 kahramana kadar.
2. **Düşman animasyonları.** Yürüme, darbe alınca sarsılma ve ölüm; ekranda 30-40 düşman olduğu için hafif olmalı.
3. **Boss'lar.** Her katın kendine özgü bir boss'u ve bu boss'ların hazırlanma (telegraph), saldırı, ikinci faz ve ölüm
   animasyonları. Mevcut boss'lar: Forge Warden ve Glass Regent; sırada Rootmother Myrr, Choirmaster Nine, The Starved
   Astronomer ve The Last Bell var.
4. **Efektler (VFX).** Kılıç savurma, asa halkası, hançer çizgileri, yanma, buz, zehir, kalkan ve mühür; seviye atlama ve
   sandık açma patlamaları. Mobilde hafif olmalı: flipbook dokular ya da sade parçacıklar.
5. **Karakter tutarlılığı.** Aynı karakterin onlarca karesinin ve farklı açılarının birbirini tutması. Görsel üretimdeki en
   büyük sorun bu.
6. **İsteğe bağlı:** ses efektleri ve müzik, Türkçe/İngilizce seslendirme (boss replikleri), mağaza görselleri.

## Araştırılacak sorular

Her kategori için, Kasım 2026 itibarıyla en iyi **3-5 aracı** karşılaştır.

**A. Tutarlı karakter görselleri ve sprite sheet üretimi**
- Bir referans çizimden aynı karakterin farklı açılarını ve pozlarını tutarlı üreten araçlar hangileri?
- Doğrudan sprite sheet veya animasyon karesi üretebilenler var mı? Kalite gerçekte nasıl? (Örnek oyunlar, kullanıcı
  yorumları.)
- Yerelde çalışan seçenekler (ControlNet, LoRA ile karakter eğitimi vb.) ve bunların GPU gereksinimi nedir?

**B. Görselden 3D modele, otomatik iskelet ve animasyon (Blender'da render almak için)**
- Bir çizimden kullanılabilir bir 3D model (GLB/FBX) çıkaran araçlar hangileri?
  - Topoloji ve poligon sayısı nasıl?
  - Dokular ne kadar iyi?
  - Toon/boyalı stile uyuyor mu?
- Otomatik iskelet (auto-rig) ve hazır animasyon kütüphaneleri hangileri?
  - Mixamo'nun 2026'daki durumu nedir?
  - Alternatifleri neler?
- Videodan ya da metinden hareket (AI motion / mocap) üreten araçlar hangileri? Blender'a aktarılabiliyor mu?

**C. Efektler (VFX)**
- Ateş, duman, büyü ve vuruş efektlerini flipbook (sprite sheet) olarak üreten araçlar hangileri?
  - Simülasyon tabanlı olanlar (EmberGen benzeri).
  - Yapay zekâ tabanlı olanlar.
- Unity Built-in RP'de mobil için hafif efekt üretmenin 2026'daki en iyi yolu nedir?

**D. Unity'nin kendi yapay zekâ araçları**
- Unity AI'ın (eski adıyla Muse/Sentis) Kasım 2026'daki durumu nedir?
  - Unity 6000.0 ve Built-in RP ile ne kadarı çalışıyor?
  - Ücretli mi?
- Sprite, animasyon ve doku üretiminde ne işe yarıyor?

**E. Ses**
- Ses efekti, müzik ve seslendirme için ticari kullanıma izin veren yapay zekâ araçları hangileri?
- Türkçe seslendirme kalitesi nasıl?

**F. Hukuk ve mağaza kuralları**
- Her aracın **ticari kullanım lisansı** nedir?
  - Üretilen içeriğin sahibi kim?
  - Ücretsiz planda ticari kullanım yasak mı?
- Google Play'in yapay zekâ ile üretilmiş içerik politikası nedir? Mağaza sayfasında belirtmek gerekir mi?
- Eğitim verisi tartışmaları açısından hangi araçlar daha "temiz" sayılıyor (lisanslı veriyle eğitilmiş olanlar)?

**G. Maliyet ve erişim**
- Her aracın aylık fiyatı, kredi sistemi ve Türkiye'den ödeme yapılıp yapılamadığı nedir?
- API fiyatları nedir?

**H. Ajanla otomasyon**
- Hangi araçların resmî API'si ya da CLI'si var?
  - Bir kod ajanının script ile toplu üretim yapmasına uygunlar mı?
  - Kullanım koşulları otomasyona izin veriyor mu?
- Hangileri tamamen yerelde çalışabiliyor?

**I. Büyük, Megabonk tarzı açık dünya haritası üretimi (en önemli yeni başlık)**

Planımız şu: her katın önünde büyük bir dünya olacak. Oyuncu bu dünyada:
- koşar, rampalardan ve sıçrama pedlerinden zıplar;
- sunakları "capture" eder (içinde durup doldurur);
- dolaşan elitleri ve mini boss'ları keser;
- süre dolunca Final Swarm'dan sağ çıkar.

Ardından bir kapıdan şu anki el yapımı odalara geçer; bu odalar boss odası olur.

- **Unity Asset Store ve benzeri mağazalar:** stilize, düşük poligonlu, dark fantasy modüler ortam paketleri hangileri?
  (Dökümhane/endüstriyel, buz/ayna mahzeni, kül üstünde bahçe temalarına uyanlar.)
  - Mobil performansları nasıl (poligon, draw call, doku boyutu)?
  - Lisansları ne diyor?
  - Fiyatları ne kadar?
- **Prosedürel harita araçları:** Unity'de büyük, rastgele ama okunur haritalar kurmak için araçlar ve paketler hangileri?
  (Modüler parça yerleştirme, yol ağı, yükselti ve rampa üretimi.)
- **Yapay zekâ ile ortam üretimi:** metinden ya da görselden 3D ortam, prop, doku ve tileable doku üreten araçlar hangileri?
  Bunların mobil için kullanılabilirliği nedir?
- **Megabonk'un haritaları nasıl kurulmuş:**
  - Boyut ve oynama süresi.
  - Sunak çeşitleri.
  - Zıplama ve yükselti kullanımı.
  - Boss kapısının nasıl açıldığı.
  - Haritalar el yapımı mı, prosedürel mi?
  - Geliştiricinin röportajları ve devlog'ları varsa bunlar.

## Çıktı formatı

1. **Her kategori için bir tablo.** Sütunlar:
   - araç;
   - ne yapıyor;
   - bizim stilimize (dark-cute, boyalı/toon) uygunluğu;
   - çıktı formatı;
   - ticari lisans;
   - fiyat;
   - API / CLI / yerel;
   - Türkiye'den erişim;
   - bilinen bir oyunda kullanımı.
2. **Önerilen üretim hattı.** Örnek olarak tek bir karakterin (ör. Riftblade / Nix Vane) referans çiziminden oyun içi
   koşma, saldırı ve ölüm karelerine kadar adım adım hangi araçla ne yapılacağı.
3. **Bir boss için aynı hat** (ör. Rootmother Myrr: kök/bahçe temalı, büyük boyutlu, iki fazlı).
4. **Riskler.** Tutarlılık, lisans, maliyetin büyümesi, aracın kapanma riski.
5. **İlk ay için önerilen en küçük araç seti ve bütçesi** (aylık TL/USD).

**Kaynak kuralı:** Her iddia için kaynak linki ver ve tarihini yaz. Resmî dokümantasyon, fiyat sayfaları ve lisans
metinleri öncelikli; reklam yazılarını ayrıca işaretle.
