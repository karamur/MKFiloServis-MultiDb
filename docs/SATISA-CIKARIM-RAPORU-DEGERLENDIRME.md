# Satışa çıkarım raporunun değerlendirmesi ve çözüm planı

**Tarih:** 2026-10-02
**İncelenen belge:** [SATISA-CIKARIM-ANALIZ-RAPORU.md](SATISA-CIKARIM-ANALIZ-RAPORU.md)
**Yöntem:** Mevcut çalışma ağacındaki kaynak kod, yapılandırma, kurulum betikleri ve Git izleme durumunun statik incelemesi. Çalışan uygulamada yetkisiz erişim denemesi, veritabanı geçişi, geri yükleme veya test çalıştırılmadı.

## Sonuç

Özgün raporun satış öncesi güvenlik ve işletim risklerine ilişkin ana yönü yerindedir. Ancak **“6 kritik / 11 yüksek / 14 orta / 8 düşük” sayıları doğrulanmış nihai envanter değildir**: bazı maddeler fazla kesin ifade edilmiş, bazıları artık geçerli değil, raporda bulunmayan önemli bir yönetici hesabı riski var. Müşteriye kurulum için karar verilmeden önce aşağıdaki P0 maddeleri kapatılmalı ve üretim benzeri ortamda doğrulanmalıdır.

| Öncelik | Bulgular | Karar |
|---|---|---|
| P0 | Sabit parolalı yönetici ve test hesapları; lisans imza sırrı; eksik sayfa/API yetki sınırı; JWT sırrı | Satış/kurulum öncesi giderilmeli |
| P1 | Kaydı başarısız olsa da devam eden kritik başlatma işleri; firma `1` varsayılanı; kaybolabilen denetim kayıtları; düz metin Luca parolası | İlk müşteri kurulumundan önce giderilmeli |
| P2 | Geri yükleme eksikliği, kırık CI, şema geçiş stratejisi, üretim Swagger, tamamlanmamış PDF | Yayın kabul ölçütlerine göre giderilmeli |

## Doğrulanan sorunlar ve çözüm tasarımı

### 1. Sabit parolalı yönetici erişimi — P0, özgün rapordan daha geniş

[DbSeeder.cs](../MKFiloServis.Web/Data/DbSeeder.cs) yönetici kaydını düz metin varsayılan parola ile oluşturuyor. [KullaniciService.cs](../MKFiloServis.Web/Services/KullaniciService.cs) ayrıca yönetici ve **yönetici rolünde test kullanıcısı** oluşturuyor; mevcut test hesabı pasif/kilitli ise başlangıçta yeniden açıyor. [Program.cs](../MKFiloServis.Web/Program.cs) iki seed yolunu her başlangıçta çağırıyor. Bu ikinci hesap özgün raporda yok ve en acil bulgu.

**Çözüm:** Üretimde test kullanıcısı oluşturulmasını ve otomatik yeniden etkinleştirilmesini kaldırın. Tek bir başlangıç yöneticisi oluşturma akışı bırakın. Kurulum başına rastgele üretilmiş, tek seferlik başlangıç bilgisi güvenli kanaldan verilsin ve ilk girişte değiştirilmesi zorunlu olsun. Mevcut kurulumlardaki test/yönetici hesaplarını otomatik silmeden önce envanterleyin; kullanım ve müşteri erişimini kontrol ederek parolaları değiştirin veya hesapları devre dışı bırakın. Kabul ölçütü: yeni üretim kurulumunda bilinen parola ile giriş yapılamaması ve pasifleştirilen test hesabının yeniden açılmaması.

### 2. Lisans imzasının gizli anahtarı dağıtılabilir kodda — P0

[MainForm.cs](../MKFiloServis.LisansDesktop/MainForm.cs) ve [LicenseService.cs](../MKFiloServis.Web/Services/LicenseService.cs) aynı gömülü gizli değeri kullanıyor. SHA-256 ile anahtar eklenerek üretilen imza, doğrulayıcı uygulama ele geçirilirse yeniden üretilebilir. `VerifyLicenseHash` hash dosyası yoksa yeniden yazıyor, okuma hatasında başarı döndürüyor; bu ek dosya güvenilir bütünlük kanıtı sağlamıyor. `setup/Setup.iss` ve `setup/GuncelleSetup.iss` lisans aracını paketliyor; müşteri paketi olan `setup/MusteriSetup.iss` için aynı sonuç varsayılmamalı.

**Çözüm:** Sürümlenmiş lisans formatına geçin; imzalama özel anahtarı yalnızca şirket içindeki araçta kalsın, Web yalnızca açık anahtarla doğrulasın. Eski lisanslar için süreli ve denetlenebilir geçiş planı oluşturup ardından eski imza anahtarını kaldırın. Lisans aracının müşteri ve güncelleme paketlerine dahil edilip edilmediğini her paket türü için ayrı denetleyin. Yerel hash dosyasının davranışını fail-closed yaparken disk arızasında destek/kurtarma yolunu belirleyin. Özgün rapordaki gömülü anahtar değerini yeni raporlara, loglara veya ekran görüntülerine tekrar yazmayın. Kabul ölçütü: Web paketinden özel imza üretilememesi; değiştirilmiş/eksik lisansın kabul edilmemesi; geçerli eski lisanslar için geçiş senaryosunun belgelenmesi.

### 3. Sayfa ve API yetki sınırı — P0

`Components/Pages` altında **255** Razor sayfasının **63** tanesinde doğrudan `@attribute [Authorize]` var; **192** tanesinde yok. [Routes.razor](../MKFiloServis.Web/Components/Routes.razor) `AuthorizeRouteView` kullanıyor, ancak sayfada yetki meta verisi yoksa bu tek başına erişim engeli değildir. [MainLayout.razor](../MKFiloServis.Web/Components/Layout/MainLayout.razor) ilk interaktif render sonrasında oturum geri yükleme/başarısızsa girişe yönlendirme yapıyor. Bu yüzden “192 sayfanın tamamı anonim kullanıcıya veri gösterir” iddiası statik incelemeyle **kanıtlanmış değildir**; ilk render ve servis çağrısı sırasında hassas verinin sunulup sunulmadığı ayrıca incelenmelidir. [Program.cs](../MKFiloServis.Web/Program.cs) içinde endpoint düzeyinde genel bir fallback policy görünmüyor. Bazı controller'larda yalın `[Authorize]` var; bunlar aşağıdaki eksik varsayılan şemadan etkilenebilir.

**Çözüm:** Web sayfaları, API/controller uçları, SignalR hub'ları ve dosya indirme yolları için ayrı erişim matrisi çıkarın. Blazor circuit kimliği ile HTTP endpoint kimliğini karıştırmadan güvenli varsayılan yetki politikası kurun; anonim login/kurulum/sağlık yollarını açıkça tanımlayın. Sayfalarda rol veya mevcut `Yetkiler` kodlarına dayalı politika kullanın; aynı yetkiyi servis katmanındaki okuma/yazma işlemlerinde de denetleyin. İki kullanıcı ve iki firma ile doğrudan URL, API, ilk render ve circuit yeniden bağlanma senaryolarını doğrulayın. Menüde gizleme erişim kontrolü sayılmamalı.

### 4. Kayıtsız varsayılan kimlik doğrulama şeması — P0/P1

[Program.cs](../MKFiloServis.Web/Program.cs) `DefaultScheme = "Cookies"` ayarlıyor, fakat `AddCookie` çağrısı yok; yalnızca JWT bearer kaydediliyor. Özgün raporun bu yapılandırma çelişkisi doğru. Bununla birlikte Blazor sayfaları özel [AppAuthenticationStateProvider.cs](../MKFiloServis.Web/Services/AppAuthenticationStateProvider.cs) ile çalıştığından **“tüm Blazor sayfaları patlar”** sonucu fazla geniştir. HTTP düzeyindeki yalın `[Authorize]` controller/endpoint akışları özellikle incelenmeli.

**Çözüm:** API için açık `Bearer` şeması kullanın. Blazor'un oturum modelini kalıcı ve sunucu tarafından doğrulanabilir hâle getirecek tasarımı ayrıca seçin; cookie eklemek tek başına mevcut `ProtectedSessionStorage` akışını düzeltmez. Seçilen tasarımdan sonra varsayılan şemayı gerçekten kayıtlı handler'a bağlayın ve yalın `[Authorize]` yollarını doğrulayın.

### 5. Uygulama ve kurulum sırları — P0

[appsettings.json](../MKFiloServis.Web/appsettings.json) dağıtılan varsayılan JWT sırrını içeriyor; [docker-compose.yml](../docker-compose.yml) güçlü değer verilmediğinde tahmin edilebilir veritabanı/JWT varsayılanlarına düşüyor. Özgün rapordaki risk doğrulandı. Sırların Git geçmişinde veya eski paketlerde bulunma durumu ayrıca taranmalı; yalnızca dosyayı değiştirmek eski anahtarı geçersiz kılmaz.

**Çözüm:** Üretimde boş veya örnek sırla başlatmayı reddedin; kurulum başına sır üretip dosya izinleri sınırlı bir sır deposundan yükleyin. Eski JWT anahtarlarını ve veritabanı parolalarını kontrollü döndürün. Docker üretim profili gerekli değişkenler olmadan açılmamalı. Kabul ölçütü: varsayılanlarla üretim başlatılamaması ve eski JWT'lerin artık kabul edilmemesi.

### 6. Firma ve denetim kaydı bütünlüğü — P1

[ApplicationDbContext.cs](../MKFiloServis.Web/Data/ApplicationDbContext.cs) aktif firma bulunmadığında yeni tenant kaydına `FirmaId = 1` atıyor. Aynı dosya denetim tablosu/üretimi hatalarında işlemi sürdürüp logu atlayabiliyor. [EbysEvrakService.cs](../MKFiloServis.Web/Services/EbysEvrakService.cs) atayan kullanıcıyı sabit `1` yapıyor. Özgün raporun O-2/O-3/Y-4 maddeleri bu bakımdan doğrulandı; bunlar çoklu firma ve denetim izi için yüksek öncelik taşır.

**Çözüm:** Etkileşimli işlemlerde firma ve kullanıcı bağlamı zorunlu olsun; arka plan işlerinde firma açık parametreyle taşınsın. Belirsiz bağlamda sessizce firma `1`e yazmak yerine işlem durdurulsun veya yalnızca açıkça tanımlanmış sistem kaydı yolu kullanılsın. Denetim kaydı için ayrı güvenilir kuyruk/transaction stratejisi seçin; kayıt yapılamadığında görünür alarm üretin. Kabul ölçütü: firma bağlamı olmayan işin başka firmaya veri yazmaması; ekleme/güncelleme/silmede gerçek kullanıcı ve firma kimliğinin izlenmesi.

### 7. Başlatma ve şema geçişi — P1

[Program.cs](../MKFiloServis.Web/Program.cs) içindeki `RunScopedSafeAsync` hatayı loglayıp devam ediyor; aynı sarmalayıcı veritabanı hazırlama ve seed için de kullanılıyor. `PendingModelChangesWarning` bastırılmış. Bu durum “kurulum başarılı” görünürken şemanın veya temel verinin eksik kalmasına yol açabilir. Özgün rapordaki “25 boş catch” anlatımı doğru değil: ortak sarmalayıcı hatayı **logluyor**, ancak kritik görev için uygulamayı durdurmuyor.

**Çözüm:** Görevleri kritik ve opsiyonel olarak sınıflandırın. Veritabanı açılışı, gerekli şema geçişi ve ilk yönetici oluşturma başarısızsa başlatmayı durdurun; opsiyonel işlerde uyarı ile devam edin. Migration ve idempotent SQL yardımcıları için tek kaynaklı şema sürüm planı çıkarın. `PendingModelChangesWarning` bastırmasını kaldırmadan önce boş ve mevcut veritabanlarında model/şema farkını giderin. Geri dönüş için doğrulanmış yedek gerekir.

### 8. Gizli bilgi ve kurtarma eksikleri — P1/P2

[LucaPortalService.cs](../MKFiloServis.Web/Services/LucaPortalService.cs) portal parolasını düz metin saklıyor. [DatabaseBackupService.cs](../MKFiloServis.Web/Services/DatabaseBackupService.cs) geri yükleme metodu uyarı verip `false` dönüyor. Bunlar ayrı iş kalemleri: parolayı uygulama anahtarıyla şifrelemek ve mevcut düz metin kayıtları güvenli biçimde dönüştürmek; ayrıca her etkin DB sağlayıcısı için desteklenen geri yükleme yolunu belirleyip veri kopyası üzerinde prova etmek. Geri yükleme yoksa “yedek var” ifadesi kurtarılabilirlik garantisi değildir.

### 9. Teslimat kalitesi — P2

[tests.yml](../.github/workflows/tests.yml) var olmayan `MKFiloServis.Tests` projesini restore/build/test ediyor. Workflow ayrıca çözüm dosyası değişikliklerinde `.slnx` uzantısını izlemiyor. [ProformaFaturaService.cs](../MKFiloServis.Web/Services/ProformaFaturaService.cs) PDF dışa aktarmasında `NotImplementedException` atıyor; özelliğin kullanıcıya sunulup sunulmadığı ayrıca kontrol edilmeli. Swagger [Program.cs](../MKFiloServis.Web/Program.cs) içinde tüm ortamlarda açık. CI'ı var olan test yaklaşımına bağlayın veya yeni test projesi kurun; PDF'yi tamamlayın ya da görünür eylemi kaldırın; Swagger'ı ortam/yetki politikasıyla sınırlayın.

## Özgün raporda düzeltilmesi gereken yorumlar

| Madde | Değerlendirme | Düzeltilmiş yaklaşım |
|---|---|---|
| K-2 | `192` sayfa sayımı doğru; “hepsi anonim erişilebilir” kanıtlanmadı. | İlk render, doğrudan URL, servis ve API erişimini ayrı doğrulayın. |
| K-3 | Kayıtsız `Cookies` şeması doğru; Blazor'un özel state provider'ı göz ardı edilmiş. | HTTP ve circuit oturumunu ayrı tasarlayın. |
| K-6 | Legacy SHA-256 yolu gerçekten var ve `SuccessRehashNeeded` dönüyor. | Önce başarılı eski girişte yeniden hash'in kalıcı kaydedildiğini ve kalan eski hesap sayısını doğrulayın; ardından eski yolu kaldırın. Doğrudan kaldırmak kullanıcıları dışarıda bırakabilir. |
| Y-6 / O-4 | Hatalar tümüyle yutulmuyor, ortak sarmalayıcı logluyor. | Kritik görevlerin hatasında başlatmayı durdurun; yalnızca opsiyonel görevler devam etsin. |
| Y-8 | `.Result` yolları var; “ölümcül kilit” her akışta kesin değil. | `async` zincire dönüştürün ve bloklanan çağrıların fiilen hangi yürütme bağlamında olduğunu belirleyin. |
| Y-9 | Dış servisler var; `Ollama` proxy ayarından bunların gizlilik ihlali olduğu sonucu çıkmaz. | Entegrasyon başına veri akışı, açık yapılandırma, TLS ve erişim kontrolü denetimi yapın. |
| Y-10 | Npgsql legacy timestamp anahtarı açık; belirtilen maaş farkı kanıtlanmadı. | Tarih/saat alanlarının anlamını ve sağlayıcılar arası dönüşümü belirleyip geçiş planı çıkarın. |
| O-13 / D-8 | `git ls-files` incelemesinde `bin/obj` veya listelenen build logları izlenmiyor; yalnızca `test_all.txt` izleniyor. | Git'ten çıkarma maddesini mevcut gerçek izleme durumuna göre daraltın. |
| Genel | Raporun satır numaraları zamanla kayabilir ve bazı düzeltmeler geçiş gerektirir. | Dosya/simge ve davranış temelli kabul ölçütü kullanın; raporu kod değiştikçe güncelleyin. |

## Eksik bırakılan ek bulgular

1. **Yönetici rolündeki `test` hesabı:** Sabit parola ile oluşturuluyor ve pasif/kilitli ise yeniden açılıyor. P0 kapsamında ele alınmalı.
2. **Özgün raporda sırların kendisi yazılmış:** Bu belge paylaşılırsa hassas teknik ayrıntılar da yayılır. Paylaşılacak sürümde sır değerleri çıkarılmalı; sırlar ayrıca döndürülmeli.
3. **İnternetsiz kullanım iddiasıyla çelişen istemci CDN'leri:** [App.razor](../MKFiloServis.Web/Components/App.razor) Bootstrap Icons, Leaflet ve Chart.js için dış URL'ler kullanıyor. Bunlar AI veri aktarımı kanıtı değildir, fakat internet olmadan görünüm/harita davranışı bozulabilir. Ürün çevrimdışı vaat ediyorsa statik varlıklar yerelleştirilmeli.
4. **API ve hub yetkileri sayfa sayımı dışında:** [Controllers](../MKFiloServis.Web/Controllers) ve SignalR uçları için ayrı yetki envanteri hazırlanmalı. Özellikle yalın `[Authorize]` ile varsayılan şema etkileşimi incelenmeli.

## Uygulama sırası ve kabul kapıları

1. **Derhal:** Üretimde test hesabı seed'ini kaldırın; mevcut test hesaplarını envanterleyin; başlangıç parolalarını ve dağıtılan sırları döndürme planı hazırlayın. Mevcut müşteri verilerini silmeyin.
2. **Kimlik ve lisans:** Tekil admin bootstrap, lisansın asimetrik imzası, gerçek HTTP kimlik şeması, sayfa/API/servis yetki matrisi. Yeni ve eski kurulum geçişleri ayrı ele alınmalı.
3. **Veri bütünlüğü:** Firma `1` varsayımını ve sabit kullanıcı ID'sini kaldırın; kritik başlatma görevlerini fail-closed yapın; denetim kaybına alarm ekleyin.
4. **Operasyon:** Luca sırlarını dönüştürün; yedek geri yükleme provasını yapın; CI'ı çalışır hâle getirin; Swagger/PDF kararını tamamlayın.
5. **Yayın kapısı:** Temiz kurulum ve mevcut veri yükseltmesi, iki firma/two-role erişim senaryoları, geçerli/geçersiz lisans, yedekten geri dönüş ve çevrimdışı istemci davranışı üretim benzeri ortamda doğrulanmalı. Bu inceleme bunları çalıştırmamıştır.

**Karar:** Mevcut statik kanıtlar özellikle yönetici hesabı, lisans imzası ve yetki sınırı nedeniyle satış öncesi düzeltme gerektiriyor. “Satışa hazır” onayı yalnızca kod değişikliklerine değil, yukarıdaki yayın kapısının uygulamalı sonuçlarına dayanmalıdır.
