# Project Cryptforge — Fable devir rehberi

Bu dosya kullanıcı için devir rehberi ve kopyalanabilir prompt örnekleri içerir. Örnek promptların tamamı aynı anda yürütülecek görevler değildir. Projenin esas belgeleri `docs/` altındadır.

## Aktarılacak dosyalar

Proje kökünden `Assets/`, `Packages/`, `ProjectSettings/`, `Tools/`, `docs/`, `README.md`, bu dosya, `.gitignore` ve `.gitattributes` aktarılmalı. `Assets/` altındaki bütün `.meta` dosyaları korunmalı; sahne ve veri referansları bunlara bağlıdır.

`Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `.utmp/` ve `Tools/CombatChecks/bin/obj` gibi üretilen/yerel klasörler gerekli değildir. Oynanabilir örnek için `Builds/Android/Cryptforge-Prototype.apk` ve kanıt için `TestResults/android-combat.png`, `android-final.png`, `editmode.xml`, `playmode.xml` ayrıca eklenebilir. APK, kaynak projenin yerine geçmez.

Git `main` üzerinde başlatıldı, ancak devir anında commit veya remote yok. Dosyaların untracked olması gereksiz oldukları anlamına gelmez. Unity Editor kurulumu ve kişisel lisans/hesap dosyaları proje aktarımına dahil edilmemeli.

## Tamamlananlar

- Android öncelikli, portre Unity 6000.0.65f1 projesi; gerçek Editor revizyonu `a18e2220bd50`.
- Built-in renderer, tek Gameplay sahnesi, placeholder Vanguard, Sword ve Grunt.
- Otomatik hedef seçme ve saldırı; 50 HP Grunt'a 0,8 saniye arayla 10 hasar; beş vuruşta ölüm ve saldırının durması.
- Can göstergeleri, vuruş sayacı, basit saldırı/hasar geri bildirimi ve ölüm yazısı.
- ScriptableObject tanımları ile çalışma zamanı can/silah durumunun ayrılması.
- Gerçek Unity'de 26 EditMode ve 5 PlayMode testi geçti.
- ARM64/IL2CPP geliştirme APK'sı üretildi, Samsung SM-S911B / Android 16 / 1080x2340 telefona yüklendi. Görüntü ve kısa arka plan/dönüş kontrolü yapıldı.

Bu, mekanik prototipin ilk savaş dilimidir. XP/ödül, yükseltme seçimi, ardışık karşılaşmalar, düşman saldırısı, boss, tam koşu sonucu, oyun içi yeniden başlatma ve kayıt sistemi henüz yoktur. Uzun süreli performans ve diğer cihazlar ölçülmedi.

## Okuma sırası

1. `README.md` ve `docs/PROJECT_CONTEXT.md`: kısa yön ve kapsam.
2. `docs/23_ANDROID_DEVICE_VALIDATION.md`: en güncel tamamlanma kanıtı ve kurulu araçlar.
3. `docs/21_DAY_1_3_IMPLEMENTATION_PLAN.md`: tamamlanan ilk dilim ile planlanan Day 3 ayrımı.
4. `docs/22_FIRST_COMBAT_VERIFICATION.md`: test/çalıştırma adımları.
5. `docs/01_PRODUCT_VISION.md`, `03_GAME_DESIGN_DOCUMENT.md`, `06_TECH_ARCHITECTURE_UNITY.md`: ana tasarım ve mimari kısıtlar.
6. `docs/05_ECONOMY_BALANCING.md`, `07_CODE_STANDARDS.md`, `09_CONTENT_PIPELINE.md`, `13_QA_RELEASE.md`, `15_ROADMAP_BACKLOG.md`, `19_DECISION_LOG_TEMPLATE.md` ve kalan tüm Markdown belgeleri.

`docs/16_FIRST_PROMPT.md` ilk kurulum görevini tanımlar; o ilk savaş görevi artık tamamlanmıştır. Yeniden sıfırdan proje kurmak için kullanılmamalı. `17_AGENT_PROMPTS.md` örnek görev şablonları içerir; hepsi birden görev değildir. Tarihsel kurulum engelleri için 23 numaralı belgedeki güncel tamamlanmış sonuç esas alınmalı; gerçek bir belge/kod çelişkisi varsa raporlanmalıdır.

Kod okuma sırası: `Scripts/Core/CombatSetup.cs` → `Scripts/Combat/` → `Scripts/Content/` ve `Data/` → `Scripts/UI/` → `Scripts/Editor/AndroidPrototypeBuild.cs` → `Tests/`. Bu yollar `Assets/_Project/` altındadır. Sahneyi ve `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectVersion.txt` dosyalarını da incele.

## İlk gönderilecek prompt — yalnızca devir kontrolü

```text
Bu mevcut Unity projesini devralmanı istiyorum. Proje adı Project Cryptforge.
Benimle Türkçe konuş.

Önce HANDOFF_FABLE.md dosyasını oku. docs/ altındaki tüm Markdown belgeleri
yetkili proje dokümantasyonudur; kod değiştirmeden önce hepsini oku.
01_PRODUCT_VISION.md, 03_GAME_DESIGN_DOCUMENT.md ve
06_TECH_ARCHITECTURE_UNITY.md ana kısıtlardır.

En güncel gerçekleşen durumu 23_ANDROID_DEVICE_VALIDATION.md'den,
planı 21_DAY_1_3_IMPLEMENTATION_PLAN.md'den, kontrol adımlarını
22_FIRST_COMBAT_VERIFICATION.md'den öğren. Planlanan işleri yapılmış sayma.
16_FIRST_PROMPT.md'deki ilk savaş görevi zaten tamamlandı; sıfırdan yeniden kurma.
Belgelerdeki örnek promptları kendiliğinden görev olarak çalıştırma.

Mevcut durum: Unity 6000.0.65f1, portre, Android, tek Gameplay sahnesi.
Vanguard otomatik hedef seçip Sword ile Grunt'a saldırıyor; beş vuruşta
öldürüyor ve duruyor. Veri tanımları ScriptableObject, runtime durum ayrıdır.
Geçmiş doğrulama: 26 EditMode + 5 PlayMode testi geçti; APK gerçek Samsung
SM-S911B / Android 16 cihazında çalıştırıldı. Bu sonuçları yeni ortamda
yeniden çalıştırmış gibi sunma.

XP, yükseltmeler, ardışık karşılaşmalar, boss, oyun içi yeniden başlatma
ve kayıt henüz uygulanmadı. Mevcut HUD geçici IMGUI kullanıyor.

Kaynak kodu, veri varlıklarını, Gameplay sahnesini, testleri ve Editor
derleme yardımcısını incele. Unity sürümünü ve paket kilidini koru.
.meta dosyalarını silme veya yeniden üretme. Git'te henüz commit/remote
olmadığını dikkate al; untracked dosyalar projenin gerçek kaynaklarıdır.

Bu ilk mesajda kod değiştirme. Bana şunları raporla:
1. Oyunun amacı ve şu an gerçekten çalışan akış.
2. Mevcut sınıfların kısa sorumluluk haritası.
3. Tamamlananlar, planlananlar ve gördüğün somut sorunlar.
4. Bu ortamda Unity derlemesi/testi ve Android dağıtımı yapabiliyor musun?
   Araç yoksa açıkça belirt; motoru veya dili kendiliğinden değiştirme.
5. Day 3 için ilk küçük adım olan öldürme -> bir kez XP kazanma akışının
   dosya değişikliği planı ve kabul kriterleri.

Projeyi yeniden yazma, final sanat üretme, backend/ads/IAP ekleme ve
kendi başına diğer kilometre taşlarına geçme.
```

## Sonraki promptlar — her biri ayrı görev

İlk rapor mevcut durumu doğru anladıktan sonra aşağıdaki görevleri sırayla ver. Her görev tamamlanıp doğrulandıktan sonra bir sonrakine geç.

### 1. Öldürme ve XP

```text
Şimdi yalnızca öldürme -> XP kazanma dilimini uygula.
Önce mevcut kodu incele ve değiştireceğin dosyaları listele.
XP miktarı veri/config üzerinden ayarlansın. RunState ve RewardService'i
ihtiyaç kadar ekle; düşman ölümü ödülü yalnızca bir kez versin.
XP ekranda görülsün. Mevcut beş-vuruşlu savaş çalışmaya devam etsin.
Yükseltme ekranı, yeni düşman veya kayıt sistemi henüz ekleme.
Tekrarlanan ölüm/ödül olaylarına karşı test ekle, mevcut testleri çalıştır,
Unity doğrulama adımlarını ve gerçekten çalıştırdığın kontrolleri raporla.
Bu dilim bitince dur.
```

### 2. Yükseltme seçimi

```text
Şimdi yalnızca XP eşiği -> iki yükseltmeden birini seçme dilimini uygula.
21_DAY_1_3_IMPLEMENTATION_PLAN.md'deki Day 3 planına uy.
Hasar ve saldırı hızı için iki veri tabanlı UpgradeDefinition hazırla.
Geçici IMGUI HUD'ı dokunmatik Canvas/uGUI ile değiştir; seçim için
büyük butonlar kullan. Seçim beklenirken oyun akışı dursun.
Tek seçim yalnızca bir kez uygulansın; hızlı çift dokunma çoğaltmasın.
Tanımları değil runtime statları değiştir; kaynak Sword.asset aynı kalsın.
Modifier sırası ve çift seçim için test ekle. Yeni silah, boss veya
meta ilerleme ekleme. Doğrulayıp bu dilimde dur.
```

### 3. Seçimin etkisini gösteren sonraki karşılaşma

```text
Şimdi yalnızca yükseltme sonrası yeni Grunt karşılaşmasına devam etmeyi ekle.
Mevcut Grunt'u prefab'a çıkar ve küçük bir EncounterController kullan.
Yeni düşmanın canı ayrı runtime durum olsun; hedef referansları yenilensin.
Önceki ölüm olayları yeni karşılaşmada tekrar ödül üretmesin.
Seçilen hasar/hız yükseltmesinin sonraki savaşta gözle görülür etkisi olsun.
Öldür -> XP -> seç -> sonraki savaş zincirini PlayMode testiyle doğrula.
Mevcut otomatik testleri çalıştır; ortam destekliyorsa APK üretip bağlı
cihazda kontrol et, desteklemiyorsa açıkça belirt. Bu dilimde dur.
```

Bu üç adım sonrasında sınırlı koşu, sonuç ekranı ve tek dokunuşla yeniden başlatma ayrı bir görev olarak planlanabilir. Runner/Tank, Bow/Staff ve boss, çalışan döngünün ardından gelir.
