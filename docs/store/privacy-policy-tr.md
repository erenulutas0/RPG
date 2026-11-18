<!--
TASLAK - sahibin yayımlaması için (docs/59 bölüm 3 madde 6). Hukuki görüş değildir.
Yayımlamadan önce: her {{YER_TUTUCU}} doldurulur, her [[KONTROL: ...]] ve [[EĞER ...]] çözülüp işareti silinir,
metin {{POLICY_URL}} adresinde HTML sayfası olarak yayımlanır (Google Play etkin, herkese açık, bölge kısıtı olmayan
bir adres ister, PDF olmaz) ve hem Play kaydından hem oyunun içinden bağlantı verilir. İçinde "{{" ya da "[[" kalan
hiçbir şey yayına çıkmaz. İngilizce metin (privacy-policy-en.md) aynı politikadır; ikisi birlikte güncellenir.
KVKK aydınlatma metni ayrı bir belgedir (kvkk-aydinlatma-tr.md); bu politika onun yerine geçmez.
-->

# Cryptforge Gizlilik Politikası

**Yürürlük tarihi:** {{EFFECTIVE_DATE}}

Bu gizlilik politikası, {{DEVELOPER_NAME}} ("biz") tarafından geliştirilen Android oyunu **Cryptforge**'u oynarken
seninle ilgili bilgilere ne olduğunu anlatır. Oyunun kendisini ve içine yerleştirilmiş hizmetleri kapsar.

## Kısaca

- **Hesap yok.** Oyun adını, e-postanı, telefon numaranı ya da bir şifre sormaz; giriş ekranı yoktur.
- **Kendi sunucumuz yok.** İlerlemen (altının, eşyaların, kahramanların) **telefonunda** kaydedilir ve bize ulaşmaz.
- **Sohbet yok, oluşturduğun hiçbir şey paylaşılmaz.** Bu sürümdeki arena telefonunda, oyunun ürettiği rakiplere karşı
  oynanır; çevrimiçi oyun yoktur.
- Telefonundan çıkan veriyi **üç dış hizmet** işler:
  - **Google AdMob** isteğe bağlı ödül videosunu gösterir;
  - **Google'ın Kullanıcı Mesajlaşma Platformu (UMP)**, yasanın gerektirdiği yerlerde reklam tercihlerini sorar;
  - **GameAnalytics** oyunun nasıl oynandığını sayar, böylece oyunu iyileştiririz.
- **Satın almalar** **Google Play** üzerinden yapılır. Kart bilgilerini hiçbir zaman görmeyiz.
- **Oyun internetsiz çalışır.** İnternet olmadan her şeyi oynayabilirsin; yalnızca ödül videosu bağlantı ister.

## 1. Sorumlu kim

Veri sorumlusu {{DEVELOPER_NAME}}, {{POSTAL_ADDRESS}}. Gizlilikle ilgili her soru ve talep için **{{CONTACT_EMAIL}}**
adresine yaz.

## 2. Telefonunda kalanlar

Oyun aşağıdaki dosyaları telefonunda, uygulamanın kendi depolama alanında tutar. Oyun bunları ne bize ne de başka
birine gönderir:

- **Kaydın:** altının, malzemelerin, + seviyeleri ve efsunlarıyla eşyaların, açtığın kahramanlar, silahlar ve
  kalıntılar, katlardaki ilerlemen.
- **Arena kaydı:** arena rakiplerin, sonuçların ve sıralamadaki yerin.
- **Ayarların:** seçtiğin dil.
- **Satın almanın kopyası:** "Ödül videolarını atla" ürününe sahip olup olmadığının bir kopyası; oyun bunu internetsiz de
  bilsin diye. Satın almanın asıl kaydını Google Play tutar (bölüm 3.4).
- **Küçük bir oyun günlüğü:** hataları bulmak için son seferlerinin kaydı. Telefonda kalır; büyüdükçe en eski kısmı
  silinir (en fazla yaklaşık 768 KB). [[KONTROL: bu cümle yalnızca sürüm derlemesi yerel günlüğü tutuyorsa kalır
  (docs/59 bölüm 3.3).]]

**Hepsini silmek için** oyunu kaldır ya da telefonunun ayarlarından oyunun depolama alanını temizle (Ayarlar >
Uygulamalar > Cryptforge > Depolama). Silinen bir kaydı geri getiremeyiz, çünkü elimizde hiçbir zaman kopyası olmadı.

**Android'in kendi yedeği.** Telefonunun ayarlarında yedeklemeyi açtıysan Android, cihaz yedeğinin parçası olarak oyunun
dosyalarını Google hesabına kopyalayabilir. Bu yedek, Google hesabının ayarlarına bağlı bir Google hizmetidir; biz onu
göremeyiz. [[KONTROL: bu paragraf yalnızca sürüm derlemesi yedeklemeye izin veriyorsa kalır. Prototipin manifesti
`android:allowBackup` belirtmiyor; Android'in varsayılanı izin vermektir.]]

## 3. Oyunun içindeki hizmetler

### 3.1 Google AdMob (Google Mobile Ads SDK) - ödül videosu

**Ne için.** Bir sefer bittiğinde, ikinci seferinden itibaren sonuç ekranı kısa bir video önerebilir: izlersen seferin
altını 1,5 katına çıkar. Tamamen isteğe bağlıdır; "Hayır" demek başka hiçbir şeyi değiştirmez. Oyunda başka reklam
yoktur: ne afiş (banner) ne de ekranlar arasında reklam.

**Neyi işler.** Google'ın bu SDK için yayımladığı açıklama, reklam, analiz ve sahtekârlığı önleme amacıyla otomatik
olarak toplanıp paylaşılan şu verileri sayar:

- **IP adresin**; genel konumunu tahmin etmek için kullanılabilir;
- **uygulamayla etkileşimler**: uygulamanın açılması, dokunuşlar, video izlemeleri gibi;
- **tanılama bilgileri**: uygulamanın ve SDK'nın açılış süresi, donma oranı, enerji kullanımı gibi;
- **cihaz tanımlayıcıları**: Android reklam kimliği, uygulama seti kimliği (app set ID) ve varsa cihazda oturum açmış
  hesaplara bağlı diğer tanımlayıcılar.

**Kişiselleştirilmiş ya da değil.** Yasanın rızanı gerektirdiği yerlerde (örneğin Avrupa Ekonomik Alanı, Birleşik
Krallık ve İsviçre) önce Google'ın rıza mesajı sorar (bölüm 3.2); reklam ancak bu adım izin verdikten sonra istenir.
[[KONTROL: Türkiye'deki oyuncuya ne gösterileceği - README'deki "Türkiye'de reklam rızası" açık sorusuna bak. Seçenekler:
rıza mesajını her yerde göstermek, yukarıdaki bölgelerin dışındaki oyuncuya kişiselleştirilmemiş reklam istemek ya da
kişiselleştirilmiş reklamı başka bir hukuki sebebe dayandırmak. Bu paragraf seçime göre yeniden yazılır.]]

**Kim karar verir.** Google bu veriyi kendi şartları altında bağımsız bir veri sorumlusu olarak kullanır. Google'ın,
hizmetlerini kullanan uygulamalardan gelen bilgileri nasıl kullandığı: <https://policies.google.com/technologies/partner-sites>;
Google'ın gizlilik politikası: <https://policies.google.com/privacy>.

**Senin elindekiler.** Reklam kimliğini telefonunun ayarlarından sıfırlayabilir ya da silebilirsin (telefona göre
Ayarlar > Gizlilik > Reklamlar ya da Ayarlar > Google > Reklamlar). Reklam tercihlerini oyunda istediğin zaman
değiştirebilirsin: [[KONTROL: oyundaki gizlilik seçeneklerinin yolu, örneğin Duraklat > Gizlilik seçenekleri]].

[[KONTROL: reklam SDK'sının oyun açılırken mi yoksa yalnızca bir video önerilmek üzereyken mi başladığı, ve "Ödül
videolarını atla" sahipleri için hiç başlayıp başlamadığı. Açılışta başlıyorsa şu cümle eklenir: "Hiç video izlemesen
bile SDK oyun açılırken başlayabilir."]]

### 3.2 Google Kullanıcı Mesajlaşma Platformu (UMP) - reklam tercihlerin

UMP, Google'ın reklam rızası sorma aracıdır. Oyun açılırken, bulunduğun yerde bir rıza mesajı gerekip gerekmediğini
öğrenmek için Google'a bağlanır ve gerekiyorsa mesajı gösterir. Cevabın telefonunda saklanır ve reklam SDK'sına iletilir.
UMP, yukarıdaki Google politikalarına tabi bir Google hizmetidir.

### 3.3 GameAnalytics - oyunun nasıl oynandığı

**Ne için.** Oyuncuların oyunu nasıl kullandığını görmek için: örneğin kaç kişinin ertesi gün döndüğü, seferlerin nerede
bittiği, oturumların ne kadar sürdüğü, ödül teklifinin kabul edilip edilmediği. Böylece işlemeyeni düzeltir, oyunu
dengeleriz.

**Neyi işler.**

- kurulumun için üretilen **rastgele bir tanımlayıcı**; oyuncuları kim olduklarını bilmeden saymamızı sağlar
  [[KONTROL: ve eklenen SDK sürümünün topladığı cihaz tanımlayıcıları (reklam kimliği, app set ID) varsa onlar]];
- **cihazınla ilgili teknik bilgiler**: model, işletim sistemi sürümü, oyunun sürümü, dil;
- **IP adresin**: GameAnalytics bununla **yaklaşık konumunu** (ülke, bölge, şehir) çıkarır, sonra yalnızca kısaltılmış
  hâlini güvenlik amacıyla en fazla otuz gün tutar;
- **oyundaki olaylar**: oturumlar; başlayan ve biten seferler, nereye kadar gittikleri ve nasıl bittikleri; ilk ölüm ve
  Örs'teki ilk basma; ödül teklifi ve verdiğin cevap; satın almalar (ürün, kart bilgisi değil)
  [[KONTROL: GameAnalytics'e gönderilecek olayların son listesi]];
- oyun bir hatayla karşılaştığında **hata raporları** [[KONTROL: yalnızca SDK'nın hata raporlaması açıksa]].

**Kim karar verir.** GameAnalytics bu veriyi **bizim adımıza** (veri işleyen olarak) işler; neyin toplanacağına biz
karar veririz. GameAnalytics, veriyi seni tanımak, sana ulaşmak ya da sana reklam göstermek için kullanmadığını ve
satmadığını belirtir. Biz veriyi GameAnalytics panelinde, adını bilmeden görürüz. GameAnalytics ApS Danimarka'dadır;
veriyi Amerika Birleşik Devletleri'nde Amazon Web Services ve Google Cloud üzerinde barındırır. Gizlilik bildirimi:
<https://www.gameanalytics.com/trust/privacy-notice>.

[[KONTROL: analizin yalnızca rızadan sonra mı çalışacağı (GameAnalytics, "gerektiği yerde" rıza almayı geliştiriciye
bırakır) - README'ye bak.]]

### 3.4 Google Play ve Unity IAP - tek satın alma

Oyun tek bir şey satar: **"Ödül videolarını atla"**. Bir kez alınır; aynı 1,5 kat altın bonusunu video izlemeden verir.
Seni güçlendiren hiçbir şey satılmaz.

- Ödemeyi Google hesabın ve Google Play şartları altında **Google Play** yapar. Kart ya da banka bilgilerini hiçbir
  zaman görmeyiz. Oyun, ürünü açmak için satın almanın bir onayını (ürün, sipariş numarası, zaman) alır ve açılışta
  Google Play'e hangi ürünlere sahip olduğunu sorar; böylece aynı Google hesabıyla yeni bir telefonda satın alman geri
  gelir. (**Kaydın** bu yolla geri gelmez; bölüm 2'ye bak.)
- **Unity IAP** (Unity Technologies), oyunun içinde Google Play ile konuşan koddur. Unity'nin bu ürün için yayımladığı
  açıklama, satın almanın çalışması ve analiz için Unity'ye gönderilen ve paylaşılmayan şu verileri sayar: yaklaşık
  konum (kısa süreli işlenir), tanımlayıcılar, satın alma geçmişi, çökme kayıtları ve performans tanılamaları, cihaz
  tanımlayıcıları. Unity'nin gizlilik politikası: <https://unity.com/legal/privacy-policy>. [[KONTROL: eklenen Unity IAP
  sürümüne göre; yukarıdaki açıklama Unity IAP 5.4 ve sonrası içindir.]]

## 4. Bu veriyi neden ve hangi dayanakla kullanıyoruz

| Amaç | Veri | Dayanak |
|---|---|---|
| İstediğin ödül videosunu göstermek ve bonusu bir kez vermek | reklam SDK'sı verisi (3.1) | [[KONTROL: hukuki sebep, bir hukukçuyla]] |
| Kabul ettiysen kişiselleştirilmiş reklam | reklam SDK'sı verisi (3.1) | açık rızan; oyunda geri alabilirsin |
| Reklam sahtekârlığını önlemek | reklam SDK'sı verisi (3.1) | [[KONTROL: hukuki sebep, bir hukukçuyla]] |
| Oyunu anlamak ve iyileştirmek | GameAnalytics verisi (3.3) | [[KONTROL: hukuki sebep - meşru menfaat mi açık rıza mı - bir hukukçuyla]] |
| "Ödül videolarını atla"yı satmak ve geri yüklemek | satın alma verisi (3.4) | satın aldığını sağlamak [[KONTROL: bir hukukçuyla]] |

Türkiye'deki oyuncular için **KVKK aydınlatma metni** aynı bilgileri Türk hukukunun istediği biçimde verir:
[[KONTROL: yayımlanan KVKK sayfasının bağlantısı]].

## 5. Veri nereye gider

Google, GameAnalytics ve Unity veriyi Türkiye dışında da işler; Amerika Birleşik Devletleri ve Avrupa Birliği dahil.
[[KONTROL: her biri için KVKK 9. madde kapsamında dayanılan aktarım güvencesi (AEA'daki oyuncular için GDPR) - bir
hukukçuyla; README'ye bak.]]

## 6. Ne kadar saklanır

- **Telefonunda:** oyunu ya da depolama alanını silene kadar.
- **GameAnalytics:** olay düzeyindeki veri en fazla on iki ay; kısaltılmış IP adresleri en fazla otuz gün; oyuncu
  düzeyinde ve günlük toplu veri en fazla bir yıl; GameAnalytics daha erken silebilir. (Gizlilik bildiriminin
  9 Kasım 2026 tarihli 4.3 sürümünden.)
- **Google ve Unity:** kendi politikalarında yazdığı kadar (bağlantılar yukarıda).
- **Bize yazdığın e-postalar:** cevap vermek için gerektiği sürece, sonra silinir [[KONTROL: bir süre, örneğin bir yıl]].

## 7. Güvenlik

Oyun bizim sunucumuza hiçbir şey göndermez, çünkü öyle bir sunucu yok. Yukarıdaki hizmetler, kendi belgelerinde
belirttikleri gibi, gönderdikleri veriyi aktarım sırasında şifreler (TLS/HTTPS). Telefondaki kaydın, her uygulamanın
dosyaları gibi Android'in uygulama depolamasıyla korunur.

## 8. Hakların

Hakkında veri işleyip işlemediğimizi; işliyorsak ne olduğunu; neden işlendiğini ve bu amaca uygun kullanılıp
kullanılmadığını; yurt içinde ya da yurt dışında kimlere aktarıldığını öğrenebilir; düzeltilmesini ya da silinmesini ve
bunun aktarıldığı kişilere bildirilmesini isteyebilir; yalnızca otomatik sistemlerle analiz edilerek aleyhine çıkan bir
sonuca itiraz edebilir; hukuka aykırı işleme yüzünden uğradığın zararın giderilmesini talep edebilirsin. Türkiye'deki
oyuncuların bu hakları 6698 sayılı Kişisel Verilerin Korunması Kanunu'nun (KVKK) 11. maddesindedir; nasıl
kullanılacağını KVKK aydınlatma metni anlatır. Avrupa Ekonomik Alanı, Birleşik Krallık ya da İsviçre'deki oyuncuların
ayrıca GDPR'den doğan hakları (erişim, düzeltme, silme, kısıtlama, itiraz, taşınabilirlik, rızayı geri alma) ve kendi
veri koruma otoritesine şikâyet hakkı vardır. [[KONTROL: GDPR cümlesi yalnızca oyun oralarda dağıtılırsa kalır.]]

**Nasıl istenir:** {{CONTACT_EMAIL}} adresine yaz. Elimizde bir hesap ya da isim olmadığı için verini bulmak üzere
yardımın gerekebilir: [[KONTROL: oyuncunun analiz verisini nasıl tanıtacağı - örneğin oyunun ayarlarında gösterilen bir
oyuncu kimliği - README'ye bak]]. En geç otuz gün içinde, ücretsiz cevap veririz.

**Reklamlar:** reklam kimliğini telefonunun ayarlarından sıfırlayabilir ya da silebilir, reklam tercihlerini oyunda
değiştirebilirsin.

## 9. Çocuklar

Cryptforge 13 yaşından küçük çocuklara yönelik değildir ve onlardan bilerek veri toplamayız.
[[KONTROL: Play Console'da seçilen hedef kitleyle aynı olmalı - README'ye bak.]]

## 10. Değişiklikler

Bu politika değişirse yeni sürüm, yeni yürürlük tarihiyle {{POLICY_URL}} adresinde yayımlanır ve oyun ona bağlantı
verir. Önemli bir değişikliği oyunun içinde de duyururuz.

## 11. İletişim

{{DEVELOPER_NAME}}
{{POSTAL_ADDRESS}}
{{CONTACT_EMAIL}}
