# Lisans imzası v3 — Modül seçimi ve LisansDesktop üzerinden yönetim

## Modül lisanslama — 2026-10-02

**Lisanslar → Lisanslı Modüller** listesinden müşterinin satın aldığı modülleri işaretleyin. En az bir modül seçilmelidir; başlangıçta hiçbir modül otomatik seçilmez. Yeni satış, yenileme ve müşteri kurulum paketinde bu seçim imzalanır ve geçmişe kaydedilir. Seçili geçmiş kaydındaki modüller forma yüklenir; eski kayıtta modül bilgisi yoksa seçim boş kalır. **Kaydı Güncelle**, kayıtlı modül/sürüm hakkını değiştirmez; hak değişikliği yeni imzalı lisans gerektirir.

Modüller: Cari, Filo ve Servis, Rent a Car, Muhasebe, Personel, Fatura ve E-Fatura, Banka ve Kasa, Bütçe, CRM, EBYS ve Belgeler, Satış ve Galeri, Stok, Holding, Raporlar, Checklist. Ortak ayarlar, giriş, firma seçimi ve lisans yükleme temel işlevlerdir. Modüle ait ayar sayfaları da modül hakkı gerektirir. Finansın birleşik dashboard alanı Banka/Kasa, Bütçe, Cari ve Filo haklarını birlikte gerektirir. Şirketler arası kart kopyalama Cari, Filo ve Personel haklarını birlikte gerektirir. Tüm alanları dolduran demo veri ekranı tüm modülleri gerektirir.

Ticari haklar **v3 RSA-PSS imzasına** dahil edilir. İmza zarfı `v3:<Base64 kanonik modül listesi>:<RSA imzası>` biçimindedir; Web modülleri imzalı zarftan okur. Değiştirilebilir ayrı bir Web DB sütunu/JSON alanı yetki kaynağı değildir. Web şeması değişikliği gerekmez; Desktop geçmişine `Modules` sütunu program açılışında eklenir, eski kayıtlar boş kalır. Özel anahtar ve açık anahtar aynı kalır.

**Erişim = girişte doğrulanan lisans hakkı + seçili modül + mevcut kullanıcı/rol yetkisi.** Admin rolü modül sınırını aşamaz. Menü, modül sayfaları/doğrudan URL, ilgili API controller'ları, evrak hub'ı, modül ayarları, ana sayfa ve genel arama lisans haklarını önbellekten okur. Süre ve veritabanı denetimi arka planda tekrarlanmaz. Dosya API'si EBYS/Belgeler, personel evrak hub'ı Personel hakkını gerektirir; belge kullanan paketlerde bu hakları ayrıca seçin.

**Eski ticari v2/legacy lisanslarda modül hakkı yoktur; modül erişimi reddedilir.** Yetkili sözleşmeye göre modülleri seçip **Modüllü Yeniden Bas** işlemini kullanın; firma/makine, özgün süre/oluşturma tarihi, bitiş ve sürüm sınırı korunur. Onay penceresi seçilen modülleri gösterir. Mevcut v2 kayıt v3'e geçirilebilir; yeni `V3Reissue` geçmiş kaydı açılır. Müşteri anlaşması bilinmeden modüller topluca verilmez. Demo lisansları mevcut süre sınırı ve kullanıcı yetkileriyle tüm modülleri kapsar.

Web ve LisansDesktop birlikte güncellenmelidir; eski Web sürümü v3 anahtarı doğrulayamaz. Bu çalışma müşteri anahtarı üretmedi veya canlı DB'yi değiştirmedi. Modül dışı URL/API reddi, Admin sınırı, modül listesiyle oynanmış anahtarın reddi, mevcut müşteri yeniden basımı, lisans değişince açık sayfanın kapanması ve yeni EXE ekranı için çalışma zamanı kabulü henüz yapılmadı.


**Güncel karar (2026-10-02):** Lisans üretimi, anahtar saklama, şifreli yedekleme ve geri yükleme yalnız **MKFiloServis Lisans Yönetim Merkezi** programından yürütülür. Harici yedekleme betiği, ortam değişkeni veya ayrı anahtar yönetim hizmeti gerekmez.

## Programdan kullanım

1. LisansDesktop programını açın.
2. **Anahtar ve Yedek** sekmesindeki **Anahtar Durumu** butonu ile anahtarı kontrol edin.
3. **Lisanslar** sekmesinde modülleri seçip **İmzalı Lisans Oluştur**, **Seçili Lisansı Yenile** veya kaydın sağ tık menüsündeki **Seçili Lisansı Modüllü Yeniden Bas** işlemini kullanın.
4. Üretilen lisansı ilgili müşterinin Web uygulamasındaki lisans aktivasyon ekranına yükleyin.

Lisans üretici yalnız şirket içinde kullanılır. Müşteri kurulumlarında Web yalnız açık anahtarla doğrulama yapar; imzalayıcı ve özel anahtar müşteri paketlerine eklenmez.

## İmzalama anahtarı

Program özel anahtarı Windows kullanıcı hesabına bağlı DPAPI ile şifreli olarak kendi uygulama veri dizininde saklar. Açık metin özel anahtar dışa aktarılmaz.

Önceki kurulumun bilinen anahtar dosyası varsa ilk anahtar kullanımı sırasında program tarafından şifreli depoya aktarılır. Bu geçiş mevcut RSA anahtarını değiştirmez; mevcut müşteri lisanslarının imzası korunur. Eski kaynak dosya ve önceki yedekler otomatik silinmez.

Anahtar yoksa program rastgele başka bir anahtar üretip imzalamaya devam etmez; **Anahtar / Yedek İçe Aktar** kullanılmalıdır. Web sürümünün açık anahtarıyla eşleşmeyen anahtar veya yanlış yedek parolası reddedilir; mevcut anahtarın üzerine yazılmaz.

Web ve LisansDesktop aynı açık anahtar kaynağını kullanır: `MKFiloServis.Shared/Licensing/LicenseSigningPublicKey.cs`. Bu dosyada özel anahtar yoktur.

İmzalı sürüm hakkı iki programda ortak `LicenseVersionPolicy` ile doğrulanır. Sürüm 2–4 sayısal bileşen içerir ve veritabanıyla uyumlu olarak en fazla 20 karakterdir; boş, aşırı uzun, işaretli, harfli veya parse edilemeyen değerler üretimde reddedilir. Mevcut sözleşmede yalnız açık `0.0.0` değeri sınırsız sürüm hakkıdır. Geçersiz değer bu hakka dönüştürülmez.

**Lisanslar** sekmesindeki **En Fazla Sürüm** alanından yeni lisans hakkını belirleyin. `1.0.99` ilk alan değeridir; sabit imza sınırı değildir. Sınırsız hak için **Sınırsız sürüm hakkı** kutusu açıkça işaretlenmelidir; sürüm alanına `0.0.0` yazmak üretim formunda yeterli değildir. Seçili kaydın mevcut hakkı forma yüklenir; yeni satış, yenileme ve müşteri paketinde seçilen hak imzalanıp yeni kayda yazılır. **Kaydı Güncelle** eski kaydın sürüm hakkını değiştirmez; hak değişikliği yeni imzalı lisans gerektirir. **Modüllü Yeniden Bas** kayıtlı eski sürüm sınırını korur; formdaki yeni sürüm sınırını kullanmaz. Modüller ise formda seçilir ve onayda gösterilir.

Müşteri kurulum paketi hazırlanırken paket sürümü seçilen lisans hakkını aşamaz. Güncelleme ZIP'i genel dağıtım dosyasıdır ve seçili müşteri hakkına bağlanmaz; paket sürümü yine sayısal olarak doğrulanır. Web ve Desktop, v2 imza verisini aynı `LicenseSignaturePayload` kaynağından üretir; alan sırası ve mevcut v2 biçimi korunur. Bu değişikliklerin gerçek kurulum/sürüm kabulü henüz yapılmadı.

## Programdan şifreli yedekleme

Anahtar işlemlerinin tamamı **Anahtar ve Yedek** sekmesindedir. Kurulum ve güncelleme araçları **Paketleme** sekmesinde bulunur.

1. **Şifreli Yedek Oluştur** seçin.
2. En az 16 karakterlik yedek parolasını iki kez girin.
3. `.mkkey` yedeğini saklayacağınız konumu programın dosya penceresinden seçin.
4. Program yedeği yeniden okuyup parola ile açarak açık anahtar/imza eşleşmesini doğrular; ardından başarı bildirir.

Yedek, parola korumalı PKCS#8 biçimindedir (AES-256-CBC, PBKDF2/SHA-256, 600.000 iterasyon). Parolayı yedek dosyasından ayrı saklayın. Parola olmadan yedek geri yüklenemez. Program parolayı kaydetmez, loglamaz veya panoya kopyalamaz. Yedek Git çalışma alanına yazılamaz.

Bu yedek biçimi Windows hesabına bağlı değildir; yedek dosyası ve parolasıyla başka bilgisayardaki aynı açık anahtarı kullanan LisansDesktop sürümünde geri yüklenebilir.

## Programdan geri yükleme

Yedeğin kullanılabilirliğini mevcut anahtarı değiştirmeden kontrol etmek için **Yedeği Doğrula** seçin. `.mkkey` dosyasını seçip parolasını girin. Program yedeği bellekte açar ve bu sürümün açık anahtarıyla imza eşleşmesini denetler; etkin anahtar deposuna veya müşteri lisanslarına yazmaz. Bu kontrol başka bilgisayardaki aynı sürümde de yapılabilir; bilgisayarın mevcut imzalama anahtarı gerekmez.

1. **Anahtar / Yedek İçe Aktar** seçin.
2. `.mkkey` yedeğini seçip parolasını girin.
3. Program özel anahtarla doğrulama verisi imzalayıp ortak açık anahtarla doğrular.
4. Eşleşme başarılıysa anahtarı yeni bilgisayarın kendi şifreli deposuna kaydeder.

Önceki `.dpapi.json` yedekleri de aynı butondan alınabilir; bunların açılması eski Windows hesabının DPAPI anahtarlarını gerektirir. Önceki PEM anahtarını programa ilk kez almak için aynı buton kullanılır; program PEM dışa aktarmaz.

## Mevcut müşterilerin modüllü v3 geçişi

- Yetkili müşteri kaynağından firma, makine, lisans hakkı, sürüm ve bitiş tarihini teyit edin.
- Programdaki lisans geçmişinden ilgili kaydı seçin; sağ tık **Seçili Lisansı Modüllü Yeniden Bas** işlemini kullanın.
- Akış özgün firma/makine/sürüm/bitiş ve oluşturma tarihi/süreyi korur; tutarsız veya süresi dolmuş kaydı reddeder. Daha güncel lisans ya da aynı bitişte modüllü yeniden basım varsa durur.
- İşlem yeni satış/uzatma oluşturmaz; `V3Reissue` geçmiş kaydı yazar. Müşteri teslimi, aktivasyon ve yeniden açılış kabulü ayrıca teyit edilmelidir.
- Yerel satış geçmişi programın kendi `licenses.db` dosyasındadır. Önceki salt okunur incelemede 46 geçmiş kaydı, 9 firma/makine çifti vardı; geçmiş kayıtlar tek başına aktif müşteri hakkı veya teslim/kabul kanıtı sayılmaz.

Legacy ortak sır ile doğrulama varsayılan olarak kapalı kalır. Müşterilerin yeni lisansı hazır ve kabul edilmiş olmadan yeni sürüme geçişi yapılmamalıdır. Ticari lisansların mevcut 14 günlük yenileme toleransı ve demo kuralları bu düzenlemeyle değiştirilmedi.

## Önceki yedeklerin durumu

2026-10-02'de oluşturulan yerel ve USB DPAPI yedekleri korunmuştur. Doğrulanmış eski dosya adı: `license-signing-20261002T175221Z-91da1d3180204406a3672ed5806b2eec.dpapi.json`.

Bunlar önceki aşamanın geri dönüş kopyalarıdır. Harici yedekleme betiği kaldırılmıştır; yeni yedekleme ve geri yükleme işlemleri programdan yapılır. Bu çalışma sırasında yeni yedek parolası belirlenmedi, taşınabilir `.mkkey` dosyası oluşturulmadı, eski özel anahtar/yedek dosyaları silinmedi ve müşteri lisansı üretilmedi.

## Doğrulama kapsamı

LisansDesktop kaynak değişiklikleri derlendi. Anahtar yönetimi pencereleri, parola ile yedekleme/geri yükleme ve farklı bilgisayarda kurtarma henüz etkileşimli kabulden geçmedi. Müşteri envanteri ve müşteri lisans geçişi açık kalır; K-1 bütünüyle kapanmış değildir.

## Makine kimliği ve doğrulama — 2026-11-22

Yeni FP2 makine kimliği bilgisayar ve Windows kullanıcı adından bağımsızdır. Dört bileşenden tek bir donanım değişimi toleranslıdır; eksik bileşenler eşleşme sayılmaz. Eski makine kimlikleri birebir karşılaştırılarak kabul edilir. Kökten farklı donanım için yeni imzalı lisans gerekir. Ana ekran, aktivasyon ve modül kontrolleri aynı politikayı kullanır; ticari bitiş günü ve 14 günlük yenileme toleransı demo süresiyle karıştırılmaz.

DB'deki lisansı görüntülemek doğrulanmış modül hakkı açmaz. Kesin lisans reddinde haklar temizlenir; geçici DB erişim hatasında önceki doğrulanmış oturum hakları korunur, yeni giriş reddedilir. Ticari aktivasyon önce tam doğrulanır, seri transaction içinde yazılıp yeniden okunarak doğrulanır ve yalnız commit sonrası önbelleğe yayımlanır. Ret veya kayıt hatasında eski lisans korunur. Aynı anahtarın yeniden yüklenmesi mevcut satırı kullanır. Yerel saat koruma dosyası yazılamazsa uyarı kaydedilir; DB doğrulama zamanı korunur.

Mevcut lisans, kullanıcı girişinde veya kayıtlı oturum geri yüklenirken tam doğrulanır. Sunucu başlangıcında ve zamanlayıcıyla doğrulama yapılmaz. Yeni anahtar veya demo aktivasyonu kendi doğrulamasını yapar; kurulumdaki `license.auto.key` bu aktivasyon akışını kullanır. Süre dolumu ve veritabanındaki lisans değişiklikleri sonraki giriş doğrulamasında görülür. Doğrulama sonucu değişirse ortak önbellek güncellenir ve açık ekranlar yeni modül haklarına göre yenilenir; devam eden işlemler geri alınmaz. Development varsayılanı tüm modülleri açar; `Licensing:EnforceInDevelopment=true` gerçek lisans politikasını uygular. Gerçek müşteri v3 anahtarı, IIS ve PostgreSQL teslim kabulü ayrıca yapılmalıdır.

Önceki 2026-11-22 başlangıç doğrulamasında geçici SQLite ve ayrı depolama diziniyle Production süreci açıldı ve yeniden açıldı; `/healthz` ile `/readyz` HTTP 200 döndürdü. O testte kullanılan periyodik lisans doğrulaması artık kaldırılmıştır.

## N-1 alan sınırı — 2026-10-02

Ortak sürüm politikası `LicenseInfo.AllowedVersion` için EF modelindeki 20 karakter sınırını paylaşır. Lisans ekranı da bu uzunlukta giriş kabul eder. Böylece imzalama ve müşteri paketi oluşturma, veritabanının saklayamayacağı sürüm hakkı üretmez.
