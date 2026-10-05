# Web şifreli belge yedeği ve kurtarma kapsamı

## Programın oluşturduğu yeni yedek

Web dosya yedeği ve otomatik PostgreSQL DB ZIP yedeği aynı `RecoveryArchive` kapsamını kullanır. Dosya yedeği DB dump içermez; otomatik DB ZIP yedeği kökte `database.backup` içerir.

| ZIP yolu | Kaynak ve kurtarma hedefi |
|---|---|
| `storage/uploads`, `storage/Arsiv`, `storage/Depo` | Uygulamanın `AppStoragePaths.GetStorageRoot` dizinindeki aynı adlı alt dizinler |
| `storage/keys` | DataProtection key ring; varsa eski `master.key` ve diğer legacy anahtar dosyaları |
| `storage/data`, `storage/logs` | Depolama kökündeki veri ve loglar |
| `application/Data/LucaSettings` | Content root altındaki firma Luca ayarları |
| `application/wwwroot/belgeler`, `application/wwwroot/uploads` | Content root altındaki belge ve eski kurulum eki dizinleri |
| `application/*.json` | Mevcut appsettings, Production ayarları, DB/portal/yedek yapılandırması; yeni makinenin ayarlarıyla karşılaştırılmalı |
| `recovery-manifest.json` | Dosya adları, boyutlar, SHA-256 değerleri, zaman ve DataProtection kurtarma probu |

Kaynakta bulunmayan opsiyonel dizin/dosyalar eklenmez. DataProtection key ring alınamazsa başarı bildirilmez. Yeni düzende legacy `master.key` zorunlu değildir; varsa korunur. ZIP önce geçici dosyada oluşturulur, kapatılır ve her dosyanın manifest boyut/hash değeriyle doğrulanır; ardından nihai dosya adına taşınır. İptal/hatada geçici ZIP kaldırılır. Sembolik bağlantı kaynakları ve kaynak klasörü içindeki yedek hedefi reddedilir.

## Kurtarma sınırları

- ZIP şifreli bir kasa değildir: key ring, legacy anahtarlar, yapılandırma ve kimi düz metin ekler içerebilir. Yedek depolaması yetkili yöneticilerle sınırlandırılmalı ve şirketin şifreli yedek ortamında tutulmalıdır. Manifest hash'leri bozulma/eksik dosya kontrolüdür; ayrı imza veya saldırgana karşı özgünlük kanıtı değildir.
- DataProtection anahtarları sertifika veya Windows DPAPI ile korunmuşsa XML kopyası tek başına yeni makinede yeterli olmayabilir; ilgili sertifika/özel anahtar veya özgün DPAPI hesabı/makinesi gerekir. Mevcut kurulumun anahtar koruma türü ayrıca doğrulanmalıdır.
- Legacy `master.key` DPAPI'ye bağlı olabilir. Yeni makinede eski belgelerin açılması için uygulamanın mevcut legacy anahtar geçiş/kurtarma akışı ve orijinal anahtar gerekir.
- LisansDesktop `.mkkey` yedeği Web belge anahtarlarını kapsamaz.
- Canlı DB dump ve dosya kopyası tek bir ortak snapshot değildir. Kesin DB-belge tutarlılığı gereken tam yedek bakım penceresinde yazma durdurularak alınmalıdır. S3 nesneleri bu yerel dosya ZIP'inde değildir; nesne depolaması ayrıca yedeklenmelidir.

## İzole kurtarma kabulü

1. Mevcut üretime dokunmadan ayrı kurulum ve boş hedef depolama hazırlayın; aynı uygulama sürümü ve `SetApplicationName("MKFiloServis")` kullanılmalı.
2. Güvenilir ZIP'in manifesti, tekil dosya adları ve hash'leri doğrulansın. ZIP girdileri doğrudan keyfi hedeflere açılmamalı; dizin geçişi ve sembolik bağlantılar reddedilmeli.
3. Uygulama kapalıyken `storage/` içeriğini seçilen depolama köküne, `application/` içeriğini uygun kurulum dizinine yerleştirin. Üretim bağlantıları ve ortama özel ayarlar yeni hedefe göre düzenlenmeli; mevcut anahtarlar otomatik üzerine yazılmamalı.
4. PostgreSQL DB dump'ını yalnız izole hedefe yükleyin. Yeni ZIP biçiminde mevcut DB restore yolu, DB'ye dokunmadan manifest/hash kontrolü yapar; **dosyaları/ayarları/anahtarları otomatik geri yüklemez**. Eski manifestsiz DB ZIP'leri eski doğrulama yolunu kullanır.
5. Gerekli sertifika/legacy kurtarma malzemesini sağlayın. DataProtection probu, ilgili `MKFiloServis.RecoveryArchive.Probe.v1` purpose ile açıldığında `MKFiloServis-RecoveryArchive-v1` vermelidir. Bu, belge kabulünün yerine geçmez.
6. Yeni/legacy şifreli belge örneklerini açın, firma Luca ayarlarının çözülebildiğini ve DB-dosya ilişkilerinin doğru olduğunu doğrulayın. Kopyalanan XML varlığı tek başına başarı sayılmaz.

Bu kabul henüz yürütülmedi. Canlı sistemde otomatik tam dosya/anahtar restore uygulanmış değildir; N-2/D-5/O-6 kapanışı bu kanıtlardan sonra değerlendirilir.

## Programdan kurtarma dosyalarını hazırlama — 2026-10-02

1. **Admin** hesabıyla **Ayarlar → Yedekleme** ekranını açın.
2. Listeden güvenilir yeni biçim ZIP yedeğini seçin ve **Kurtarma Hazırla** düğmesine basın. Harici ZIP, **Yedek Dosyası Yükle** ile yüklenebilir (mevcut 500 MB yükleme sınırı). Manifesti olmayan eski ZIP bu işlemde kabul edilmez.
3. Program manifest, tekil adlar, izin verilen yollar, boyutlar ve SHA-256 değerlerini denetler. Dizin geçişi, Windows aygıt adları, sembolik bağlantılar, 100.000 dosyadan veya toplam 100 GiB açılmış boyuttan fazlası reddedilir. Açılan gerçek bayt sayısı da sınırlıdır; ZIP başlığındaki boyuta tek başına güvenilmez.
4. Dosyalar depolama kökünün **RecoveryStaging/recovery-...** altındaki yeni klasöre hazırlanır; mevcut dosyalar üzerine yazılmaz. Windows erişimi uygulama hesabı, SYSTEM ve Administrators ile; Unix dizin erişimi sahibiyle sınırlandırılır. Başarısız/iptal edilmiş geçici klasör temizlenir. Yeni hazırlıklar ayrı klasörler oluşturur; kabul sonrası bu hassas kopyaları yetkili yönetici kaldırmalıdır.
5. **Anahtar probu** yedekteki `storage/keys` üzerinden ayrı bir DataProtection sağlayıcısıyla denenir. Uygulamanın mevcut anahtar halkası kullanılmaz ve yeni anahtar üretilmez. Çözülürse yalnız probun açıldığı bildirilir; belge/credential kabulü ayrıca gereklidir. Çözülemezse klasör hazırlanmış halde kalır ve anahtar kurtarmasının tamamlanmadığı açıkça gösterilir; özgün sertifika/DPAPI malzemesi sağlandıktan sonra hazırlık tekrar yapılabilir.
6. Ekrandaki **DB dump Var/Yok** bilgisini kontrol edin. Hazırlama DB restore çalıştırmaz; `storage/`, `application/` ve varsa `database.backup` ayrı kurulumda önceki kabul adımlarıyla uygulanmalıdır. Mevcut canlı DB, dosyalar, ayarlar ve anahtarlar üzerine otomatik kopyalama yapılmaz.

ZIP hash'leri özgünlük kanıtı değildir; özellikle anahtar XML'leri içeren ZIP yalnız güvenilir kaynaktan alınmalıdır. Yedeklerin varsayılan depolama konumu ve hazırlık klasörü web root dışında tutulmalıdır. Sertifika/DPAPI gereksinimi, legacy anahtarların taşınabilirliği ve S3 kurtarması bu hazırlıkla kapanmaz.

2026-10-03'te uygulamanın `RecoveryArchive.CreateAsync` ve `PrepareAsync` metotları izole geçici kökte sentetik dosya/key ring ile çalıştırıldı: manifest/hash doğrulama, staging, anahtar probu ve değişmiş dosyanın reddi geçti. Test DB dump'ı PostgreSQL biçiminde değildi; gerçek DB restore yapılmadı. Gerçek ZIP, yeni makine/profilde belge/credential çözme ve DB ilişkisi kabulü hâlâ bekliyor.
