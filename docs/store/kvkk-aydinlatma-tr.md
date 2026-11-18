<!--
TASLAK - sahibin yayımlaması için. Hukuki görüş değildir; yayımlanmadan önce bir hukukçuya okutulur.

Neden ayrı bir belge: KVKK m.10 aydınlatmayı, m.5/1 açık rızayı düzenler. Sahibin raporu "Aydınlatma ile açık rızayı
ayır" diyor (docs/research/2026-11-18_release_path_owner_report.md, bölüm 3, "Türkiye ve global kullanıcı için onay").
Raporda olmayan, daha yeni bir kaynak aynı yönde: Kişisel Verileri Koruma Kurulu'nun 18/02/2026 tarihli ve 2026/347
sayılı İlke Kararı (Resmî Gazete 24/03/2026, sayı 33203) - aydınlatma metni ile açık rıza metni ayrı düzenlenir, iç içe
ya da tek onayla sunulamaz; aydınlatma metninin sonunda "okudum, kabul ediyorum" gibi bir onay istenmez (en fazla
"okudum, anladım"); işleme açık rıza dışındaki bir şarta dayanıyorsa ayrıca açık rıza istenmez.
(Kaynak: https://www.alomaliye.com/2026/03/24/kvkkdan-yeni-ilke-karari-acik-riza-metni-ile-aydinlatma-metni-artik-ayri-duzenlenmeli/
- kararın Resmî Gazete metninden doğrulanması hukukçunun işidir.)

Bu yüzden: bu metin hiçbir onay kutusu ya da "kabul" düğmesiyle sunulmaz. Kişiselleştirilmiş reklam için rıza
gerekiyorsa o, oyundaki ayrı rıza ekranında (UMP mesajı ya da kendi ekranımız) istenir; README'deki "Türkiye'de reklam
rızası" sorusuna bak.

Kanun metni (m.5, 9, 10, 11, 13, 14) mevzuat.gov.tr'deki 6698 sayılı Kanun'dan (1.5.6698) okundu; 9. madde 7499 sayılı
Kanun'la değişmiş hâliyle (01/06/2024'ten beri yürürlükte).

Yayımlamadan önce: her {{YER_TUTUCU}} doldurulur, her [[KONTROL: ...]] hukukçuyla çözülüp silinir. Hitap docs/60'ın
kuralıyla "sen"dir; hukukçu "siz" isterse metnin tamamı birlikte çevrilir.
-->

# Cryptforge - Kişisel Verilerin İşlenmesine İlişkin Aydınlatma Metni

**Son güncelleme:** {{EFFECTIVE_DATE}}

Bu metin, 6698 sayılı Kişisel Verilerin Korunması Kanunu'nun ("KVKK") 10. maddesi ve Aydınlatma Yükümlülüğünün Yerine
Getirilmesinde Uyulacak Usul ve Esaslar Hakkında Tebliğ uyarınca, **Cryptforge** oyununu oynarken kişisel verilerinin
nasıl işlendiğini anlatmak için hazırlandı. Bir rıza metni değildir; seni bilgilendirir.

## 1. Veri sorumlusu

{{DEVELOPER_NAME}}
Adres: {{POSTAL_ADDRESS}}
E-posta: {{CONTACT_EMAIL}}

## 2. Hangi kişisel verilerin işlenir

Oyunda hesap yoktur; adın, T.C. kimlik numaran, telefon numaran ve şifre istenmez. Özel nitelikli kişisel veri
(sağlık, biyometrik veri vb.) işlenmez.

| Veri kategorisi | Neler | Kim işler |
|---|---|---|
| Cihaz ve kurulum tanımlayıcıları | Android reklam kimliği, uygulama seti kimliği (app set ID), kurulum için üretilen rastgele analiz kimliği | Google (AdMob), GameAnalytics, Unity |
| Ağ bilgisi ve yaklaşık konum | IP adresi; ondan çıkarılan ülke, bölge, şehir | Google (AdMob, UMP), GameAnalytics, Unity |
| Oyun içi etkileşim verisi | oturumlar; seferlerin başlaması, bitişi ve sonucu; ilk ölüm, ilk basma; ödül teklifi ve cevabın; uygulamanın açılması, dokunuşlar, video izlemeleri | GameAnalytics, Google (AdMob) |
| Teknik ve tanılama verisi | cihaz modeli, işletim sistemi ve oyun sürümü, dil; hata, çökme ve performans kayıtları | GameAnalytics, Google (AdMob), Unity |
| Satın alma verisi | ürün, sipariş numarası, satın alma zamanı (kart ya da banka bilgisi değil) | Google Play, Unity, GameAnalytics |
| Rıza tercihi | reklam rızası konusundaki seçimin | Google (UMP); telefonunda saklanır |
| İletişim verisi | bize yazarsan e-posta adresin ve mesajın | {{DEVELOPER_NAME}} |

[[KONTROL: GameAnalytics'e gönderilecek olayların ve SDK'nın topladığı tanımlayıcıların son listesi, entegrasyondan
sonra.]]

**Telefonunda kalanlar:** oyunun kaydı (altın, eşyalar, ilerleme), arena kaydı, dil tercihin ve satın almanın yerel
kopyası telefonunda tutulur; oyun bunları bize göndermez. Oyunu kaldırınca ya da depolama alanını temizleyince silinir.

## 3. İşleme amaçları

1. İstediğin ödül videosunu göstermek ve sefer bonusunu bir kez vermek.
2. Rıza verdiysen kişiselleştirilmiş reklam göstermek.
3. Reklam sahtekârlığını ve kötüye kullanımı önlemek.
4. Oyunun nasıl oynandığını ölçmek; hataları bulmak, dengeyi ve oyunu iyileştirmek.
5. "Ödül videolarını atla" ürününü satmak, satın almayı doğrulamak ve yeni cihazda geri yüklemek.
6. Bize yazdığında cevap vermek ve KVKK kapsamındaki başvurularını sonuçlandırmak.

## 4. Hukuki sebepler

[[KONTROL: aşağıdaki eşleştirme bir öneridir ve hukukçuyla kesinleştirilir. Kanun metni: KVKK m.5/1 (açık rıza),
m.5/2-c (sözleşmenin kurulması veya ifasıyla doğrudan ilgili olması), m.5/2-ç (hukuki yükümlülük), m.5/2-f (ilgili
kişinin temel hak ve özgürlüklerine zarar vermemek kaydıyla meşru menfaat).]]

| Amaç (bölüm 3) | Önerilen hukuki sebep |
|---|---|
| 1. Ödül videosu ve bonus | m.5/2-c ya da m.5/2-f [[KONTROL]] |
| 2. Kişiselleştirilmiş reklam | m.5/1, açık rıza (ayrı ekranda istenir, her zaman geri alınabilir) [[KONTROL]] |
| 3. Sahtekârlığı önleme | m.5/2-f [[KONTROL]] |
| 4. Oyun analizi | m.5/2-f ya da m.5/1 [[KONTROL: hangisi; açık rızaysa analiz rızadan önce başlamaz]] |
| 5. Satın alma | m.5/2-c [[KONTROL]] |
| 6. İletişim ve başvurular | m.5/2-c, m.5/2-ç (KVKK m.13'teki cevap yükümlülüğü) [[KONTROL]] |

Kişiselleştirilmiş reklama rıza vermemek ya da rızanı geri almak oyunu oynamanı engellemez.

## 5. Toplama yöntemi

Kişisel verilerin, oyunun içine yerleştirilmiş yazılım geliştirme kitleri (Google Mobile Ads SDK, Google UMP,
GameAnalytics SDK, Unity IAP) aracılığıyla, oyunu kullanırken otomatik yollarla ve elektronik ortamda toplanır. İletişim
verin, bize e-posta yazdığında senden toplanır.

## 6. Kimlere ve hangi amaçla aktarılır

| Alıcı | Rolü | Amaç | Ülke |
|---|---|---|---|
| Google (AdMob, UMP) | kendi şartları altında bağımsız veri sorumlusu | reklam, ölçüm, sahtekârlığı önleme, rıza yönetimi | ABD ve diğer ülkeler |
| GameAnalytics ApS | bizim adımıza veri işleyen | oyun analizi | Danimarka; barındırma ABD (Amazon Web Services, Google Cloud) |
| Unity Technologies (Unity IAP) | satın alma altyapısı [[KONTROL: rolü, Unity'nin şartlarına göre]] | satın almanın çalışması, analiz | ABD ve diğer ülkeler |
| Google Play | ödemeyi kendi şartları altında yürüten | ödeme, satın alma kaydı | ABD ve diğer ülkeler |
| Yetkili kamu kurum ve kuruluşları | - | yalnızca hukuki bir yükümlülük doğarsa | Türkiye |

**Yurt dışına aktarım:** bu alıcıların işlediği veriler Türkiye dışına aktarılır. Aktarım KVKK m.9'a göre yapılır.
[[KONTROL: her alıcı için dayanılan güvence - yeterlilik kararı, Kurul'un ilan ettiği standart sözleşme (imzadan
itibaren beş iş günü içinde Kurul'a bildirilir) ya da diğer uygun güvenceler. KVKK'nın kendi açıklamasına göre m.9/6'daki
"arızi" istisnalar (aktarıma açık rıza dahil) yalnızca düzenli olmayan, süreklilik göstermeyen aktarımlar içindir; bir
SDK'nın sürekli aktarımı için uygun değildir: https://www.kvkk.gov.tr/Icerik/2053/Yurtdisina-Aktarim]]

## 7. Hakların (KVKK m.11)

Veri sorumlusuna başvurarak kendinle ilgili;

a) kişisel veri işlenip işlenmediğini öğrenme,
b) kişisel verilerin işlenmişse buna ilişkin bilgi talep etme,
c) kişisel verilerin işlenme amacını ve bunların amacına uygun kullanılıp kullanılmadığını öğrenme,
ç) yurt içinde veya yurt dışında kişisel verilerin aktarıldığı üçüncü kişileri bilme,
d) kişisel verilerin eksik veya yanlış işlenmiş olması hâlinde bunların düzeltilmesini isteme,
e) KVKK m.7'de öngörülen şartlar çerçevesinde kişisel verilerin silinmesini veya yok edilmesini isteme,
f) (d) ve (e) bentleri uyarınca yapılan işlemlerin, kişisel verilerin aktarıldığı üçüncü kişilere bildirilmesini isteme,
g) işlenen verilerin münhasıran otomatik sistemler vasıtasıyla analiz edilmesi suretiyle aleyhine bir sonucun ortaya
çıkmasına itiraz etme,
ğ) kişisel verilerin kanuna aykırı olarak işlenmesi sebebiyle zarara uğraman hâlinde zararın giderilmesini talep etme

haklarına sahipsin.

## 8. Nasıl başvurursun

Taleplerini yazılı olarak ya da {{CONTACT_EMAIL}} adresine e-postayla iletebilirsin.
[[KONTROL: kabul edilecek başvuru yolları, Veri Sorumlusuna Başvuru Usul ve Esasları Hakkında Tebliğ'e göre - bir
hukukçuyla.]]

Elimizde hesabın ya da adın olmadığı için, analiz verini bulabilmemiz adına başvurunda
[[KONTROL: oyunun ayarlarında gösterilecek oyuncu kimliği ya da başka bir tanıtma yolu - README'ye bak]] bilgisini
yazman gerekir.

Başvurunu talebin niteliğine göre en kısa sürede ve **en geç otuz gün içinde, ücretsiz** sonuçlandırırız; işlem ayrıca
bir maliyet gerektirirse Kurul'un belirlediği tarifedeki ücret alınabilir (KVKK m.13). Başvurun reddedilirse, cevabı
yetersiz bulursan ya da süresinde cevap verilmezse, cevabı öğrendiğin tarihten itibaren otuz ve her hâlde başvuru
tarihinden itibaren altmış gün içinde Kişisel Verileri Koruma Kurulu'na şikâyette bulunabilirsin (KVKK m.14).

Reklam kimliğini telefonunun ayarlarından sıfırlayabilir ya da silebilir, reklam tercihlerini oyunda
[[KONTROL: oyundaki gizlilik seçeneklerinin yolu]] üzerinden değiştirebilirsin.

## 9. Daha fazlası

Gizlilik politikamızın tamamı: {{POLICY_URL}}
