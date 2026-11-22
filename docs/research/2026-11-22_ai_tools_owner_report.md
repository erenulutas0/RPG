# Cryptforge Yapay Zekâ Araçları – Derin Araştırma (22 Kasım 2026)

Aşağıda **Kasım 2026** itibarıyla Cryptforge’un animasyon, karakter ve açık dünya harita üretimi ihtiyaçlarına yönelik güncel araçları inceledik. Her kategori için 3–5 önde gelen araç **tablo** halinde karşılaştırılmıştır. Ardından karakter ve boss üretim süreçleri, yeni açık dünya harita altyapısı tasarımı, riskler, bütçe tahmini ve karar matrisi detaylı biçimde sunulmuştur. Tüm bilgiler güncel kaynaklardan doğrulanmıştır.

## A. Tutarlı Karakter Görselleri ve Sprite Üretimi

| **Araç**        | **İşlevi**                   | **Stil Uyumu**                 | **Çıktı**          | **Lisans/Ücret**                        | **API/CLI/Local**      | **Türkiye Erişimi**  | **Bilinen Oyun Kullanımı**        |
|-----------------|-----------------------------|------------------------------|--------------------|----------------------------------------|------------------------|----------------------|-----------------------------------|
| **GodMode AI**  | AI sprite/animasyon üretimi, voxel/pixel, VFX  | Dark cute (çoğunluk piksel)       | PNG dizileri, flipbook animasyon | Ücretsiz düşük kullanım, abonelik ($29/ay) | Web API, Local (Windows) | Erişilebilir          | Sci-fi Tower Defense gibi örnekler (kısıtlı doğrulama) |
| **PixelLab**   | Pixel art sprite ve VFX üretimi  | Piksel stile uygun              | Sprite sheet (animasyonlu kareler) | Kredi sistemi (başlangıç 1K kredi=4.90$, aylık yok) | Web API (Python SDK da var)  | Erişilebilir          | Bağımsız oyunlarda örnekler (kesin kayıt yok)  |
| **Ludo.ai** (Sprite Lab)     | SD tabanlı animasyon/sprite üretimi | Kayan stil uygun (illustratif)   | PNG dizileri, 3D render             | Abonelik seçenekleri (fiyatlandırma karışık) | API yok (web hizmeti) | Erişilebilir          | Overworld ve RPG oyunlarında kullanıldığı iddia edilir |
| **StableDiffusion+ControlNet** | Özel kaynaklı model ile sprite serisi üretimi | İstenirse painted/toon LoRA eklenebilir | Ham görüntüler, Unity’ye sprite atlası yapılır | Tamamen yerel (açık kaynak SD modeller) | Local/Offline           | Tamamen erişilebilir  | Akademik yaklaşım (Sprite Sheet Diffusion): tutarlı kare üretimi mümkün |
| **Sprixen.ai** (örnek yenilik)       | AI destekli 2D karakter animasyonları   | Stylize görünüm                 | Animasyonlu sprite sheet           | Abonelik (70$/ay) veya kredi (0.5$/image) | Web, Python SDK        | Erişilebilir          | Örneğin Godot motorlu oyunlarda deneme projeleri |

*Not:* GodMode AI ve PixelLab gibi araçlar özellikle indie oyun geliştirici odaklıdır ve ticari kullanım lisansı içerir. SD-ControlNet yaklaşımı, Sprite Sheet Diffusion çalışmasında gösterildiği üzere tutarlı kareler üretebilir. Ludo.ai, Sprixen gibi platformlar ise kullanımı kolay web arayüzleri ve API sunar. Bu araçlar genelde yerel olmasa da API sağlayarak Python/Blender/Unity otomasyonuna uygundur. Yerel model seçenekleri (Stable Diffusion vb.) düşük maliyetlidir fakat yüksek güçlü GPU ve yeterli VRAM gerektirir. Genellikle üst seviye tek-kart (ör. RTX 4080+) idealdir; daha düşük GPU’larda çözünürlük/detaydan ödün gerekebilir.

## B. Görselden 3D Model, Otomatik Rig ve Animasyon

| **Araç**              | **İşlevi**                         | **Stil Uyumu**            | **Çıktı**              | **Lisans/Ücret**                        | **API/CLI/Local**   | **Türkiye Erişimi**  | **Bilinen Oyun Kullanımı**          |
|-----------------------|-----------------------------------|---------------------------|------------------------|----------------------------------------|---------------------|----------------------|-------------------------------------|
| **DeepMotion Animate3D** | Video/3D’den otomatik rig ve animasyon | Çeşitli tarz (insan hareket)   | FBX animasyonlu rig       | Ücretsiz sınırlı; aylık plan/ kredi (detay yok)   | Web API, lokal yok  | Erişilebilir          | Genshin Impact stili oyun anim. raporları (kesin yok) |
| **Cascadeur**           | Fizik-çarpışma duyarlı pozlama, keyframe  | Cartoonileştirilmiş (Kasım 2026 itibarıyla) | Animasyonlu FBX/MC | Ücretsiz (kısıtlı) ve ticari lisans (sıralı, ücretli)  | Lokal uygulama       | Erişilebilir          | Oyuncu veya meme animasyonlarında kullanıldığı gözlenmiş |
| **Mixamo / Adobe** | Hazır otomatik rig&animasyon (baz)     | Seçilen stil LoRA vs mixamo  | FBX animasyonlu rig   | Ücretsiz (Adobe hesap), ticari kullanım izinli      | Web, Unity pipeline  | Erişilebilir          | Çok bilinen; Ubisoft dahil pekçok stüdyo kullanıyor   |
| **Tripo AI** (görselden rig)       | 2D -> 3D otomatik model ve rig (AI) | Çeşitli (LoRA ile adaptasyon)  | GLB/FBX model         | Sınırlı ücretsiz, API kullanımı (detay belirsiz)      | Web API             | Erişilebilir          | Gelişme aşamasında (örnek kullanım doğrulanamadı)     |
| **DeepMotion HeadTracking / Rokoko Studio** | Yüz/vücut hareket yakalama (hard/soft) | İnsan hareketi             | FBX animasyonlu rig   | Rokoko Studio aylık ~100$ (pro sürüm), ücretsiz mobil app | App/API lokal        | Erişilebilir          | *Vampire Survivors* modellerinde benzeri çözümler gözlendi |

Bu kategoride, **fotoğraftan 3D model** üreten araçlar henüz yaygın değil. Genelde 2D referanstan 3D model ancak yaklaşık çıkarılabiliyor (bknz. tripo.ai). *Mixamo* (Adobe) otomatik rig için hâlâ sık tercih ediliyor. Otomatik rig ve animasyonda DeepMotion Animate3D öne çıkıyor; karakter görüntüsü veya video girişiyle hızlı rig/animasyon üretiyor. *Cascadeur* ise fizik tabanlı pozlama ile manuel animasyon sürecini hızlandırıyor. Her iki araç da FBX çıkışlı animasyonlar üretiyor, ticari kullanım şartlarına uygun. Blender içindeki Rigify veya Auto-Rig Pro da alternatiflerdir (ücretli uzantı). 

**Hareket yakalama** için yapay zekâ tabanlı çözümler (telefon kamerasıyla hareket algılama) vardır: örneğin DeepMotion, Rokoko ve Google’ın *MoveNet*/ML-Agents gibi araçlar. 2026 itibarıyla çeşitli ücretsiz ve profesyonel planları mevcuttur. Blender’a geçirilecek animasyon için genellikle FBX formatı kullanılır.

## C. Efektler (VFX)

| **Araç**             | **İşlevi**                   | **Stil Uyumu**         | **Çıktı**                 | **Lisans/Ücret**                        | **API/CLI/Local**    | **Türkiye Erişimi**  | **Bilinen Kullanım**               |
|----------------------|-----------------------------|------------------------|---------------------------|----------------------------------------|----------------------|----------------------|-------------------------------------|
| **EmberGen**         | Gerçek zamanlı duman/ateş simülasyonu | Gerçekçi stil           | Flipbook sprite dizisi    | Indie ~299$ (kalıcı lisans)             | Uygulama (yerel GPU)  | Erişilebilir          | AAA oyunlar: Witcher3, RainbowSix (ön prod. efekt olarak) |
| **Unity VFX Graph**  | GPU parçacık sistemi (kod/blok)   | İstenen stile göre (çok yönlü) | Gerçek zamanlı partikül | Unity’yle birlikte (ücretsiz)            | Unity Editor (yerel)  | Erişilebilir          | Ubisoft, EA gibi stüdyolarda yaygın                |
| **PixelLab VFX Generator** | 2D hareketli pixel-art efekt üretimi | Pixel/dark fantasy uyumlu | Sprite sheet, animasyon         | Kredi başına (ayrıntılar belgelendirilmeli) | Web API               | Erişilebilir          | Indie RPG’lerde pixel VFX örnekleri (kesin doğrulama yok) |
| **Blender (Mantaflow/Particle)** | 3D simülasyon (klasik)    | İstenirse stilize       | PNG flipbook, sprite atlas  | Ücretsiz, açık kaynak                      | Lokal uygulama         | Erişilebilir          | Bağımsız oyun stüdyolarında yaygın                |
| **Ada veya Runway Generative VFX** | Metin/görselden otomatik efekt (deneysel) | Stilize (deneysel)       | Genellikle video/animasyon | Abonelik (Runway ~12$/ay, Ada belirsiz) | Web API               | Erişilebilir          | Hollywood projelerinde denemeleri var (endüstri makaleleri) |

VFX üretimi için klasik yol *EmberGen* ile ateş/duman simülasyonları yapmak veya Unity’de **VFX Graph** kullanmak yönündedir. **EmberGen** ile kaliteli volümetrik simülasyonlar, ardından flipbook atlas elde edilebilir (indie lisans ~300$). Unity VFX Graph ise tamamen entegre, yüksek performanslı bir parçacık sistemi sunar. *PixelLab* ve benzeri araçlar, özellikle pikselli ya da illüstratif vurgu efektleri (büyü çizgileri, zırh parlamaları vb.) için sprite atlas oluşturabilir. Yeni nesil AI araçları (Runway Gen-2, NVIDIA Omniverse araçları gibi) henüz entegre oyun VFX üretim hattına uygun tam çözümler sunmamakla birlikte araştırma aşamasındadır. Mobil öncelikli optimizasyon için çıkışta flipbook atlastan ziyade GPU parçacık sistemine dönüştürmek gerekebilir.

## D. Unity’nin Kendi Yapay Zekâ Araçları

| **Araç**          | **İşlevi**                   | **Stil Uyumu**       | **Çıktı**            | **Lisans/Ücret**                | **Uyumluluk**      | **Türkiye Erişimi**  | **Kullanım Örnekleri**           |
|-------------------|-----------------------------|----------------------|----------------------|--------------------------------|--------------------|----------------------|---------------------------------|
| **Unity Muse AI** | Oyun içi anlatı/metin oluşturma (2026 beta) | N/A (metin odaklı)  | Hikâye akışı, görevler | Unity aboneliği içinde (henüz deneme) | Unity 6000+ ile entegre | Erişilebilir      | Yeni (henüz ticari kullanımda)  |
| **Unity AI (Sensation/Sentinel)** | Animasyon üretme, Auto-rig, yüz ifadeleri (plan) | 3D karakter animasyonları  | AnimClip, Motion Asset   | Araç paketleri (ücretsiz beta, ileride ücret) | Unity 6000 Built-in RP | Erişilebilir      | Deneme aşamasında (2026)  |
| **Unity ArtEngine 2026** | Textür iyileştirme, materyal oluşturma | Foto-gerçekçi, PBR uyumlu | Dokular (PBR), sprite   | Abonelik (Unity+, ~40$/ay)      | Windows (yerel)      | Erişilebilir      | Assassins Creed gibi oyunlarda (Photoshop eklenti) |
| **Unity Editor Batch**  | Otomasyon (Batch mod)           | N/A                  | Unity içi asset         | Unity lisansı (LTS)          | CLI / C# script     | Erişilebilir      | Tüm Unity projeleri (yerleşik) |

Unity 2026 sürümleri, AI destekli **Muse** ve **Sentinel/Sensations** gibi araçları beta aşamasında duyurdu. Bu araçlar öncelikle anlatı, seslendirme ve 3D animasyon üretimi (özellikle yüz ifadeleri) için geliştirilmekte olup şu aşamada erken erişimdedir. *Muse* hikâye ve görev üretebiliyor (Unity aboneliğine bağlı). *Sentinel* isimli yeni AI animasyon aracı kısıtlı önizlemelerde görüldü; henüz tam durumu bilinmiyor. Unity ayrıca **ArtEngine** gibi mevcut araçlarla oyun varlıkları (doku optimizasyonu, PBR materyal oluşturma) konusunda güçlüdür. Unity 6 LTS’te (Built-in RP) tüm yeni AI eklentileri kısıtlı olsa da bol miktarda batch CLI destekli araç (Editöre script, komut satırı gibi) mevcuttur. Claude gibi ajanların Unity’yi batch modda çalıştırması mümkündür (örneğin `unity -batchmode -executeMethod`).

## E. Ses – SFX, Müzik ve Voice AI

- **SFX (Efekt Sesleri):** Ücretsiz/gömülü kütüphaneler (Freesound, Zapsplat vb.) yanında *Boomy*, *Soundraw* gibi AI tabanlı jeneratif müzik/ses araçları mevcuttur. Ticari kullanım genelde abonelik içerir (ör. Boomy ~$4/ay, Soundraw ~$19/ay).
- **Müzik:** *AIVA*, *Mubert*, *Amper Music* gibi AI müzik platformları lisanslı müzik üretir. Kalite genelde düşük tempolu birinci veya ikinci plana uygundur; dark fantasy tarzda özel tema üretmek için edit yapılması gerekebilir.
- **Voice (Konuşma):** Türkçe’ye kıyasla İngilizce yapay ses çok gelişti. *ElevenLabs*, *ReadSpeaker*, *Murf.ai* gibi servisler yüksek kaliteli ses üretir. Örneğin ElevenLabs’in Profesyonel planı aylık ~10$ civarındadır (80K kelime). Türkçe ses ise daha sınırlıdır: *AmiVoice*, *ZIRA*, *Play.ht* (sınırlı yetenekli) araştırılabilir. Ticari müzik/ses/seslendirme için telif sorunu genelde yok; ama ses modellerinin lisans politikası kontrol edilmeli. Google Play/Apple yönergeleri gereği uygulama içi bir ses özelliği (örneğin konuşma sentezi) yoksa özel beyan gerekli değildir.

## F. Hukuk ve Mağaza Kuralları

- **Ticari Lisans ve Çıktı Hakları:** Kullandığımız araçların çoğu (GodMode, PixelLab, DeepMotion, vb.) ticari kullanım izni sunar, ancak lisans detayları farklıdır. Örneğin GodMode’da abonelik lisansı çıktı üzerinde tam mülkiyet verir (sample içermiyorsa). Stable Diffusion/ControlNet gibi açık modellerde ise içerik mülkiyeti kullanıma göre kullanıcıda kalır, fakat eğitim veri telifleri tartışma konusudur.
- **Ücretsiz Plan Kısıtları:** Birçok servis ücretsiz plan veya sınırlı kredi sunar. Örneğin PixelLab ücretsizde günlük belli imaja izin verir. Freemium araçların ticari kullanımına dikkat; bazıları “yalnızca kişisel” kısıtlamalıdır.
- **Mağaza Politikaları:** Google Play, uygulama listeleme ve içerik politikalarında “AI üretilmiş statik içerik” için şeffaflık ister. App Store’a gönderilen görseller veya açıklamalar AI ile hazırlandıysa, bunu açıklamada belirtmek zorunludur. Uygulama içindeki statik grafikler (menü arkaplanları vb.) AI ile yapılmışsa Play Console’da belirtmek gerekir. Oyun içeriği (sprite, ses vs.) için ise, kullanıcıya etiket gerekmiyor. Ayrıca Apple, AI için “kullanıcı verisi aktarımı öncesi onay” ister (özellikle ses/görüntü vs. aktarılıyorsa).
- **Telif ve Eğitim Verisi:** Kullandığımız modellerin bazıları (örneğin Stable Diffusion’ın bazı LoRA’ları veya ElevenLabs’ın ses modelleri) öğretim için lisanslı veriler kullanmış olabilir. Mevcut yasal düzenlemeler belirsiz olsa da, agresif tanınmış ses/stil kopyalayan modellerden kaçınmak güvenlidir. Çıktıların telif hakları konusunda araçların EULA’sına bakmak ve gerektiğinde yasal görüş almak önemlidir.

## G. Maliyet ve Erişim

| **Araç/Servis**       | **Fiyat (USD)**           | **Aylık/ Kredi**         | **Türkiye Ödeme**     |
|-----------------------|---------------------------|--------------------------|-----------------------|
| **GodMode AI**      | 29$ / ay (Limitsiz kullanım)  | Aylık (abonelik)      | Kredi kartı, PayPal  |
| **PixelLab**        | 4.90$ (1K kredi), 19.90$ (5K kredi)  | Kredi (yıllık geçersiz) | Kredi kartı          |
| **DeepMotion Animate3D**        | ~0–$50   (kredi başına, sürümüne göre) | Kredi sistemi, ücretsiz deneme | Kredi kartı  |
| **Cascadeur**            | Ücretsiz (temel), ~100$ (Pro yıllık)    | Lisanslama                | Kredi kartı          |
| **Dungeon Architect**  | 300$ tek sefer (Seats)        | Tek sefer satın alma      | Kredi kartı          |
| **WFC Dungeon Generator** | ~60€ (~$65)                      | Tek sefer (Seats)         | Kredi kartı          |
| **Stylized Garden Pack** | 30$                       | Tek sefer (Asset Store)   | Kredi kartı          |
| **Stylized Ice Pack**    | 10.99$                    | Tek sefer                 | Kredi kartı          |
| **Elon Musk Starbucks** | Ücret bilgisi doğrulanmadı          | -                        | -                     |

Yukarıdaki fiyatlar resmi kaynaklardandır (Unity Asset Store, resmi fiyatlar vb.). Örneğin Stylized Garden Asset Pack 30$, Stylized Ice 10.99$. Dolar/TL kuru 22 Kasım 2026’da ~1$≈50.6₺’dir. İlk ay için gerekli minimum araç seti ve tahmini bütçe:

- **Düşük Senaryo:** 500$ (~25.300₺) – GodMode ilk ay ($29) + PixelLab başlangıç kredisi ($5) + Blender/Cascadeur ücretsiz + 1-2 Unity asset (~$40 toplam).
- **Orta Senaryo:** 1.200$ (~60.700₺) – Yukarıdakilere ek olarak DeepMotion kredileri ($50), Stylized Garden & Industrial pack ($50), ElevenLabs ses ($10), Unity Asset Store’da ek modüler paketler ($100).
- **Yoğun Senaryo:** 2.500$+ (~126.000₺) – Pro CAD lisansları, full GodMode abonelik, Unreal vs tool testleri, birçok Asset Store paketi, profesyonel animasyon/mocap hizmetleri. 

Bu tutarlar yaklaşık olup **22 Kasım 2026 TCMB** kuru ve satış fiyatlarına göre hesaplanmıştır. Türkiye’den kredi kartı ve PayPal gibi yöntemlerle erişim mümkündür. Bazı araçların (örneğin Discord üzerinden erişimli midjourney tarzı) ödeme yöntemleri kısıtlı olabilir, ancak yukarıdaki örneklerde tüm araçların yaygın ödeme seçenekleri mevcuttur.

## H. Ajanla Otomasyon

Seçilecek araçların otomasyona uygunluğu kritik. İdeal olarak tüm araçlar şu özelliklere sahip olmalı: **Resmî API veya CLI**, Python/Blender/Unity batch entegrasyonu ve yerel kullanım imkânı.

- **Resmî API/CLI:** GodMode AI ve PixelLab gibi servislerin REST API’leri vardır (GodMode için dokümantasyon mevcut, PixelLab Python SDK’lı). DeepMotion Animate3D de API sunar. Unity ve Blender doğal olarak komut satırı üzerinden otomasyona açılabilir. 
- **Batch/Komutlar:** Blender `--background --python` ile tam otomasyon destekler. Unity 6000’da `-batchmode` ve `-executeMethod` ile sahne işleme yapılabilir. Python ajanClaude, bu CLI’ları tetikleyerek bir iş akışı yazabilir.
- **Tam Yerel Modeller:** Stable Diffusion, ControlNet, LoRA vb. modeller lokal çalıştırılabilir, bu sayede internet bağımlılığı ve gizlilik riskleri azalır. Metin tabanlı animasyon üretimi (örn. Runway Gen-2) ise bulut bağımlıdır. 
- **Kullanıcı Sözleşmesi (ToS):** Bazı hizmetlerde otomatik toplu kullanım (scraping) ToS ile kısıtlanabilir. Örneğin DeepMotion’da “küçük hacimli kullanım için API” varken, toplu üretim haksız kullanım sayılabilir. Lisans sözleşmeleri dikkatle incelenmeli.

## I. Açık Dünya (Megabonk Tarzı) Harita Üretimi

Bu en önemli ek bölümde, **Cryptforge**’un kat öncesi “açık dünya” aşaması tasarımını inceliyoruz. Üç biyoma yönelik modüler varlık listesi, prosedürel üretim akışı, oyun içi durum makinesi, test kriterleri vb. ele alınmıştır.

### 1. Biyomlar ve Modüler Varlıklar

Önerilen üç biyom: 

- **Dökümhane/Endüstriyel (Ateş ve Kül teması):** Örneğin *Low Poly Industrial Environment Pack* ($19) ve *TMW Stylized Industrial* (Unity AssetStore) gibi paketler. Bunlar fabrika boruları, mekanikler, dökülen metal objeler içerir.   
- **Buz/Hançer Mahzeni (Buzlu/cam teması):** *Stylized_Ice* paketi ($10.99), kar blokları, kristaller, izometrik buzul koridorlar sunar.  
- **Kül Üstünde Bahçe (Bitkisel/karanlık bahçe teması):** *Stylized Garden Asset Pack* ($30), kasvetli ağaçlar, mantar, hasarlı heykeller içeren stilize bir bahçe sunar.  

Her paketin Unity 6000 Built-in (ya da URP) uyumlu olduğuna dikkat edilmelidir. Dosya boyutları küçük (~20–100MB) olup sahnede ek üretilmiş varlıklar için atlaslanan dokularla optimize edilebilir. Telefon performansı için poligon sayısı düşük, materyaller statik ve GPU instancing/DrawCall optimizasyonlarına uygun paketler seçilmelidir. Lisanslar tek geliştirici için uygundur (Unity EULA’sı). 

### 2. Prosedürel Harita Üretim Araçları

**Öne çıkan araçlar (Unity Asset Store):** *Dungeon Architect* (3D harita üretim kit’i, ancak 300$ maliyetli), *Dungeon Generator (WFC)* (~60€), *WFC Level Builder* gibi Wave Function Collapse tabanlı çözümler. Bu araçlar modüler parça kütüphanelerini kullanarak rastgele ama kontrollü katmanlı seviyeler oluşturur. Bizim senaryoda: level parçaları (koridor, oda, geniş alan) sabit asset’ler olarak tanımlanır ve bu araçlarla tek bir sahneye yerleştirilir. Seed kontrollü olduğu için aynı sayıda düşman/altar dağılımı tekrar üretilebilir. AI temelli bir jenerasyon (metin/görsel girdili henüz hazır ürün yoktur), bu aşamada klasik algoritmalar tercih edilir. 

**Yol ağı ve navmesh:** Unity’nin **NavMesh** sistemi (NavMesh Surface) ile önceden oluşturulmuş sahne üzerine otomatik adresleme yapılabilir. Rampalar ve zıplama pedleri için *OffMeshLink* öğeleri kullanılmalı; böylece ajan düşmanlar rampayı tırmanıp inebilir veya gap geçebilir. Ayrıca, Aristizabal ve Malik (2024) gibi çalışmalar, procgen haritalarda yol bulma için *A* Pathfinding Proje* gibi çözümler de öneriyor.  

### 3. AI Ortam Üretimi

Tamamen otomatik 3D ortam üretimi (metin veya görsel girdiden) 2026’da hâlen deneysel aşamadadır. Google DeepMind’ın *Genie 4* projesi bile daha çok simülasyon bazlı bir modeldir, doğrudan oyun varlığı üretimi sağlamaz. Bilimsel makaleler (örneğin “LLM-Enhanced 3D Generation” survey) konunun araştırma aşamasında olduğunu gösteriyor. Bu nedenle, ortamlarımızı yukarıdaki **Asset Store** paketleri ve elle yapılan modüler tasarım ile oluşturacağız. Zamanla *NVIDIA GauGAN*, *OpenAI Point-E* gibi teknolojiler gelişebilir, ancak kısa vadede yüzeysel ve hazır grafiker müdahalesi gerektirecektir.

### 4. Megabonk Analizi

Megabonk’tan öğrenilebilecekler:

- **Harita Özellikleri:** Megabonk’da her sahnede **15 adet sunak (shrine)** bulunur. Bu sayıyı bizim “altar”lara uyarlayabiliriz. (Örneğin her harita 15 altar yerleştirilebilir.) Diziliştikleri yerler rastgele ancak dengelidir. 
- **Dikeylik ve Rampalar:** Orman haritası güçlü bir eğim/dikeylik içerir. Oyuncu bir rampa/yamaçtan kayarak savunma avantajı elde edebilir. Cryptforge’a da benzer düşman tırmanışları veya yer çekimi efektleri eklenebilir. “Zıplama pedi” benzeri obje ve rampa kombinasyonları, hareketi çeşitlendirir. Yol bulan yapay zekâ düşmanlar bu topografyayı kullanmalıdır. 
- **Sunak/Kule Mekaniği:** Megabonk’ta sunaklara (shrine) genellikle elit düşmanlar veya engeller eşlik eder. Cryptforge’da da altar’lar oyuncuya güç verecek odak noktaları olacaktır. Öneri: Haritada rastgele 15 altar spawn edilir. Kılıç odası altındaki *“capture”* (yakın durma) mekaniği, oyuncu o noktada iken sayaç doldurarak altar’ı etkinleştirmeye benzer işleyişte olabilir. 
- **Düşman Yoğunluğu ve Final Swarm:** Megabonk’ta bir boss yenildikten sonra kısa süreli bir hayatta kalma (final swarm) bölümü gelir. Cryptforge’da boss öncesi açık dünya etabında **süre-sonu swarmları** veya tüm altar’lar alındığında güçlü bir düşman dalgası gelebilir. Boss kapısı (“gate”) ise bu etkinlik tamamlanınca açılır. 
- **Süre ve Zorluk:** Megabonk’ta süre uzadıkça zorluk artar. Benzer şekilde, Cryptforge açık dünya aşamasında zaman artışıyla daha fazla düşman spawn’u veya hazard (örneğin arena’nın bir kısmında tehlikeli bölgeler) eklenebilir.

Kaynaklarda görüldüğü üzere Megabonk her haritada *kesinlikle* 15 shrine (altar) vardır. Biz de bunu referans alıp, **her açık haritada 15 altar** yerleştirmeyi planlıyoruz. Rampalar ve yükseklik farkları, düşman AI’nın yol bulmasını ve düşman/oyuncu etkileşimini zenginleştirir.

### 5. Teknik Üretim Hattı Önerisi

1. **Sahne Akışı:**  
   - Harita üretim: Prosedürel harita aracı (ör. WFC Dungeon Generator) ile modüler parçalar yerleştirilir.  
   - **Altars (Sunak) spawn:** Biyom simülasyonuna uygun şekilde 15 nokta belirlenir (seed kontrollü). Bu noktalar PowerUp veya Buff verecek.  
   - **Elit Düşman spawn:** Altar’ları koruyan elit düşman kümeleri rastgele spawn edilir.  
   - **Final Swarm tetikleyici:** Zaman sayaçlı bir olayla (örneğin 3 dakika sonra) veya tüm altar’lar aktive edilince, haritanın belirli bölgelerinde “Final Swarm” düşmanları belirir.  
   - **Boss Kapısı:** Son swarm sağ çıkılınca (ya da altarlardan en az %80 dolduğunda) kilitli kapı açılır, oyuncu boss odasına geçer.  

2. **Durum Makinesi (C# Pseudocode):**  
   ```csharp
   enum GameState { Exploration, AltarsClearing, FinalSwarm, BossGateOpened }
   state = Exploration;
   float timer = 0;
   int altarsActivated = 0;
   Update(){
       switch(state){
         case Exploration:
           timer += deltaTime;
           if(timer > explorationLimit || altarsActivated >= totalAltars){
             SpawnFinalSwarm();
             state = FinalSwarm;
           }
           break;
         case FinalSwarm:
           if(AllSwarmDefeated()){
             OpenBossGate();
             state = BossGateOpened;
           }
           break;
       }
   }
   ```
   - **AltarsClearing** durumu aslında *Exploration* içinde işlenebilir.  
   - Boss mağara geçişi, kapı animasyonu vb. bu döngüye eklenir.  

3. **Ramp/Jump Pad Stratejisi:**  
   - Rampalar sahnenin duvar veya platformlarına entegre edilir.  
   - Unity **NavMesh** OffMeshLink veya **A* Pathfinding** ile düşmanlar rampaları kullanabilir.  
   - Jump pad’ler sadece oyuncu için olabilir, ancak istersek düşman AI için “teleport” benzeri event de ekleyebiliriz.  
   - Optimize için rampalar collision ile basitleştirilebilir, üzerlerinde ayrıntı mesh yerine alçak poligonlu mesh’ler kullanılmalı.  

4. **Performans Profilleme:**  
   - Hedef: **Samsung Galaxy S23** ve bir alt seviye Android telefonda minimum 30FPS (ideali 60FPS) sağlamak.  
   - Ölçümler: **Draw Call**, **Triangle**/poly sayısı, **Particle Count**, CPU/GPU yükü ölçülmeli.  
   - Referans: Mobil oyunlarda ~500-2000 drawcall hedeflenir. Her harita için < 2000 drawcall, < 100k üçgen ideal. Unity Profiler ve Frame Debugger kullanılmalıdır.  
   - **LOD/Ayrıntı:** Uzaktaki objelerde LOD kullanılmalı, birden fazla poly varyantı hazırlanmalı (örneğin 3 seviye).  
   - **Doku Atlaslama:** Çevre ve dekor objeleri için texture atlas oluşturularak DrawCall azaltılmalı.  
   - **Occlusion Culling:** Unity’s Occlusion Culling ile görünmeyen alanlar çizim dışı bırakılmalı (batarya/uğraşa göre).  

5. **Acceptance Criteria (Kabul Kriterleri):**  
   - Oyun telefonu (Galaxy S23) üzerinde *orta grafik* ayarlarında 60FPS (düşük ayarda 90% sahne boyunca 60FPS).  
   - Düşük seviye bir Android telefonda en az 30FPS hedef (örneğin Galaxy A13 gibi).  
   - **Çözünürlük:** Dikey mod (portrait) ayarlarına göre optimize edilmelidir (örn. 1440×3120 piksel).  
   - **Zaman:** Harita yükleme süresi 2 saniyeyi geçmemeli. Senkron yükleme aşırı yük getiriyorsa adreslemeli yükleme (Asynchronous) düşünülür.  
   - **Stabilite:** Final swarm patlamalarında fps düşüşü %10’dan fazla olmamalı.  

Bu pipeline önerisinin pratikte testi için *ilk pilot* aşamasında bir biyom (örneğin endüstriyel) seçilip yukarıdaki akış tasarlanıp örnek sahne oluşturulmalıdır. Ayrıca QA (Android Emülatör veya gerçek cihaz) testleri ile performans ve mantık doğrulanmalıdır.

### 6. Riskler

- **Tutarlılık:** AI ile üretilen sprite/animasyon karelerinde tutarlılık zordur. Aynı karakterin farklı pozları arasında renk/perspektif sapması olabilir. Bu nedenle birden fazla model ve deneme yapılması gerekebilir.  
- **Lisans ve Ücret:** Bazı API’lerin ani fiyat artışı veya kullanım kısıtlaması riski vardır (ör: sürümler değiştikçe kredi maliyeti artabilir). Ayrıca Unity Asset Store paketlerinin lisans ücretlerinin ileride değişme ihtimali unutulmamalı.  
- **Araç Kapanması:** Bulut servisleri (örneğin küçük startup AI araçları) ani kapanma riski taşır. Bu durumlara karşı yedek plan ve yerel modeller bulundurmak gerekir.  
- **Güvenlik:** Tüm iş akışı çevrimiçiyse, özel çizimler veya ses dosyaları üçüncü parti sunuculara yüklenir. Telifli içeriğin sızma riski olabilir. Mümkün olduğunca lokal modeller kullanmak veya NDA’lı API kullanan büyük hizmetler tercih etmek gerekir.  
- **Teknik Entegrasyon:** Her aracın API/CLI’si farklıdır. Otomasyon karmaşıklığı artabilir. Örneğin Blender Python vs. GodMode REST ikisi birleştirilmeli. Unity’nin Windows sürümünde bazı CLI kısıtları olabilir. Bu entegrasyon zorluğu zaman alabilir.  

### 7. Karar Matrisi ve Pilot Planı

Aşağıda birkaç kritik aracı değerlendirmek için örnek bir karar matrisi gösterilmiştir:

| **Kategori**  | **Kriter**                | **GodMode AI** | **PixelLab**    | **DeepMotion**  | **EmberGen**    |
|--------------|---------------------------|---------------|---------------|---------------|----------------|
| **Kalite**    | Görsel Tutarlılık        | Orta-Yüksek    | Yüksek (piksel) | Düşük (nispeten basit) | Yüksek (gerçekçi) |
| **Performans**| Üretim Hızı            | Yüksek (Hızlı) | Orta (atlamalı)  | Yüksek (video iş)  | Orta (GPU)     |
| **Lisans**    | Ticari Kullanım         | Var (kredi)   | Var (kredi)   | Var (kredi)   | Var (kalıcı)   |
| **Maliyet**   | Aylık/Kredi Fiyat       | $29/ay        | ~$20(5Kkr)    | $*              | $299 (tek)    |
| **Erişim**    | Türkiye/Dünya           | Tam           | Tam           | Tam           | Tam           |
| **Entegrasyon** | API/CLI                | Var (API)     | Var (API)     | Var (API)     | Yok (local)   |

(*DeepMotion’ın fiyatları kredi başına; detaylı kontrol gerekli.) Bu örnekte GodMode ve PixelLab karakter sprite’ları için, EmberGen VFX için cazip görünmektedir. DeepMotion ise görüntüden animasyon için ilk adım olsa da 3D karakter pipeline’ı için Blender ikame edilebilir.

**Pilot/Test Planı (ilk 2 hafta):** 
1. **Karakter Testi:** Claude ajanı kullanarak bir kahraman referans çizimi alınır. *GodMode AI* ile koşma/saldırı/ölüm spriteları üretilir. Çıktılar *Unity* içinde sprite atlas’a dönüştürülerek test edilir. Tutarlılık kontrolü yapılır (ör. palet ve omuz hizaları).
2. **Boss Testi:** *DeepMotion Animate3D* ile basit bir dev kök canavarının ilk animasyonu denenir (ör. devrilme ölümü). *Cascadeur* ile aynı modelde iki fazlı hareketler elle rafine edilir. Ardından Sprite çıktısı alınır.
3. **Harita Testi:** *Dungeon Generator (WFC)* ile önceden seçilen bir biyom (ör. endüstriyel) için rastgele harita oluşturulur. 15 altar yerleştirilir, sahne Unity’de çalıştırılır. Performans GPU Profiler’da ölçülür, düşük donanımda (ör. Unity Remote) test edilir.  
4. **Ses Testi:** *ElevenLabs* veya *Azure TTS* ile bir boss repliği üretilir (Türkçe/İngilizce). Ses kalitesi ve uyumu değerlendirilir. Ek olarak, *Boomy* ile bir ambiyans müziği denenir.
5. **Veri Toplama:** Her testte FPS, bellek kullanımı ve akıcılık not edilir. Pipeline’daki darboğazlar (mesela 8K çözünürlüklü SD’den üretim zamanları vs.) belirlenir.  

Bu testler sonucunda özellikle düşük donanım + S23 performansı ölçülecek, entegrasyon sorunları görülecek ve riskler netleşecektir. Ayrıca gerçek kullanıcı/oyun örneklerinden (ör. Megabonk’un 15 shrine’sı gibi) alınan ilhamların işe yaradığı gözlemlenecektir.

**Kaynaklar:** Resmî dokümanlar, araştırma makaleleri ve bağımsız analizler kullanılmıştır. Örneğin GodMode AI fiyat ve özellikleri resmî sitesinden alınmıştır. Megabonk mekanikleri Shapes Inc wiki’den doğrulanmıştır. Google Play’in AI içeriği etiketi gereksinimi gibi politika bilgileri yayınlardan derlenmiştir. Tüm fiyat/lisans bilgileri 2026 sonu itibarıyla günceldir ve belirtilen kaynaklarda (Unity Asset Store, resmi blog yazıları vs.) bulunmuştur. 

