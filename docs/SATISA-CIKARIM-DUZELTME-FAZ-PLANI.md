# MKFiloServis satışa çıkarım düzeltmeleri — uygulama faz planı

**Tarih:** 2026-10-02
**Kaynaklar:** [İlk analiz](SATISA-CIKARIM-ANALIZ-RAPORU.md), [kodla doğrulama ve düzeltme önerileri](SATISA-CIKARIM-RAPORU-DEGERLENDIRME.md)
**Amaç:** Satış öncesi riskleri sıralı, geri dönüşü planlanmış ve kabul ölçütleri tanımlı iş paketlerine bölmek.

## Planlama ilkeleri

- Önce mevcut müşteri verisi, hesapları, lisansları ve yapılandırmaları envanterlenir. Güvenlik düzeltmesi adıyla müşteri verisi silinmez.
- Sırları koddan/metinlerden çıkarmak tek başına yeterli değildir; daha önce dağıtılmış değerler döndürülür. Git geçmişini yeniden yazmak ayrıca ekip ve repo sahibiyle koordine edilir.
- Blazor circuit kimliği, HTTP API kimliği, firma bağlamı ve arka plan işi kimliği birbirinden ayrı ele alınır.
- Geri yükleme ve şema geçişleri üretim verisi üzerinde denenmez; izole kopya kullanılır.
- Her fazın kod teslimi ve doğrulama sonucu kaydedilmeden sonraki faza geçilmez.
- Süreler bir veya iki geliştirici için kaba mühendislik tahminidir; müşteri verisi geçişi, dış sağlayıcı erişimi, güvenlik incelemesi ve hukuk onayı süreye dahil değildir.

## Faz özeti

| Faz | Hedef | Kapsam | Tahmini süre | Çıkış kararı |
|---|---|---|---:|---|
| 0. Hazırlık ve risk sınırlama | Değişiklik öncesi envanter ve geri dönüş planı | Sırlar, hesaplar, lisanslar, yedekler | 1–3 gün | Envanter ve sorumlular onaylandı |
| 1. Kimlik, erişim ve lisans | Yetkisiz giriş ve lisans taklit riskini kapatmak | K-1…K-6 ve ek test hesabı riski | 1–3 hafta | Erişim ve lisans kabul senaryoları geçti |
| 2. Firma izolasyonu ve denetim | İşlemleri doğru firma/kullanıcıyla ilişkilendirmek | O-2…O-5, O-3, Y-4 ve Y-3 | 1–2 hafta | Firma izolasyonu ve audit kanıtı tamam |
| 3. Veritabanı, başlangıç ve kurtarma | Eksik şema/seed riskini ve kurtarma açığını gidermek | Y-6, O-1, O-4, O-6, O-7, D-5…D-6 | 1–3 hafta | Temiz kurulum, yükseltme ve geri yükleme provası geçti |
| 4. Kod kalitesi ve otomatik teslim | Kırık CI ve sessiz hata yollarını düzeltmek | Y-1, Y-2, Y-7…Y-11, O-8…O-10, O-12 | 1–3 hafta | CI ve yayın derlemesi yeşil |
| 5. Ürün ve dağıtım hazırlığı | Desteklenen sağlayıcıları ve paket içeriğini netleştirmek | O-1, O-11, O-13…O-14, D-1…D-4, D-7…D-8 | 3–10 gün | Paket, doküman ve çevrimdışı davranış kabul edildi |
| 6. Yayın kapısı | Üretime yakın ortamda uçtan uca kanıt toplamak | Yayımlanacak tüm değişiklikler | 1–2 hafta | Satışa çıkar / beklet kararı |

## Faz 0 — Hazırlık ve risk sınırlama

**Amaç:** Güvenlik düzeltmeleri mevcut müşterileri kilitlemeden veya veri kaybettirmeden planlansın.

1. Üretim, demo ve geliştirme ortamlarındaki yönetici/test hesaplarını; aktif lisans sürümlerini; JWT/DB/Luca sırlarını envanterleyin. Sır değerlerini rapora veya issue'ya kopyalamayın.
2. Mevcut lisans anahtarı ve lisans üretim aracı nerede dağıtılmış, hangi müşteriler hangi sürümde belirleyin.
3. Her müşteri veritabanı için doğrulanmış tam yedek alın; yedekten geri yüklenebilirlik durumunu kaydedin.
4. Üretim ve demo için ayrı yapılandırma/dağıtım profillerini belirleyin.
5. Kaynak kod ve dokümanlardaki sırların Git geçmişi ve geçmiş paketlerdeki yayılımını değerlendirin; sızmış olabilecek değerler için döndürme planı oluşturun.

**Kabul ölçütleri:** Envanter sahibi atanmış; geri dönüş adımları kayıtlı; sırlar güvenli kanaldan paylaşılmış; mevcut kullanıcı ve lisansları etkileyebilecek değişiklikler listelenmiş.

## Faz 1 — Kimlik, erişim ve lisans (satış bloklayıcısı)

**Kapsam:** K-1, K-2, K-3, K-4, K-5, K-6; değerlendirme raporundaki yönetici rolündeki `test` hesabı ek bulgusu ve HTTP API/hub erişimi.

1. **Seed akışını tekilleştirin.** Üretimde bilinen parolalı admin/test hesabı oluşturulmasını ve test hesabının otomatik açılmasını kaldırın. İlk admin kurulumu tek seferlik parola/kurulum token'ı ile yapılsın; ilk girişte parola değişimi zorunlu olsun. Geliştirme test verisi yalnızca açık geliştirme profiliyle üretilebilsin.
2. **Kimlik doğrulamayı ayırın.** API için Bearer şemasını açıkça seçin. Blazor için sunucu tarafında doğrulanabilir oturum modeli belirleyin ve mevcut ProtectedSessionStorage akışını bu tasarıma uygun hale getirin. Kayıtsız `Cookies` varsayılanını kaldırın veya gerçek handler kaydedin; ikisini aynı anda varsaymayın.
3. **Yetki envanterini uygulayın.** Razor sayfaları, API controller/endpoint'leri, SignalR hub'ları, indirme/yükleme ve servis yazma işlemleri için erişim matrisi çıkarın. Anonim login/kurulum/sağlık sayfalarını açıkça belirleyin. Sayfa düzeyindeki policy/permission kontrollerini, hassas servis işlemlerinde de uygulatın.
4. **Lisans imzasını sürümlü ve asimetrik hale getirin.** Özel anahtar yalnızca şirket içi imzalayıcıda saklansın; Web yalnızca public key ile doğrulasın. Eski lisansları desteklemek için süreli geçiş sürümü yayımlayın ve eski anahtarın iptal zamanını belirleyin. Lisans aracının hangi setup türlerine dahil olduğunu ayrıca kontrol edin.
5. **Fail-open yolları kapatın.** Lisans doğrulaması hash dosyası yok/okunamıyor diye başarı vermesin; kurtarma yolu yöneticinin erişimini sağlayacak şekilde belgeli ve denetlenebilir olsun. İmza ve hash karşılaştırmalarını sabit zamanda yapın.
6. **Sırları döndürün.** JWT anahtarını, DB parolalarını ve açığa çıkmış diğer sırları kurulum başına yenileyin. Üretim, örnek/development değerleriyle başlamayı reddetsin. Tarihsel Git temizliği ayrı koordinasyon ve ekip onayıyla yürütülsün.
7. **Parola yaşam döngüsü.** Yeni parolalar için güçlü minimum uzunluk ve güvenli deneme kilidi uygulayın. Legacy SHA-256 doğrulaması, başarılı girişte hash'in gerçekten Argon2/Identity hasher'a çevrilip DB'ye kaydedildiği ve eski hesapların durumu ölçüldükten sonra kaldırılmalı. MFA, özellikle yönetici ve muhasebe rollerinde etkinleştirilmelidir.

**Kabul senaryoları:** anonim doğrudan URL ve API çağrısı reddedilir; izinli sayfa/API çalışır; rol/yetki kaybı etkin oturuma yansır; circuit yenileme ve çıkış doğru çalışır; yanlış şema exception üretmez; yanlış/değiştirilmiş/sona ermiş lisans reddedilir; yeni imzalı lisans kabul edilir; eski lisans geçişi ve iptali belirlenen kurala uyar; üretim seed'inde test hesabı yoktur.

**Geri dönüş:** Eski lisans formatı yalnızca geçiş sürümünde, belirli bitiş tarihiyle desteklenir. Kullanıcı hesaplarını otomatik silmek yerine devre dışı bırakma/parola sıfırlama uygulanır. Önceki JWT anahtarına dönmek gerekiyorsa bunun kısa süreli, kayıtlı bir acil durum prosedürü olmalıdır.

## Faz 2 — Firma izolasyonu, denetim ve gizli bilgiler

**Kapsam:** O-2, O-3, O-5; Y-3, Y-4; Y-9 gizlilik kısmı.

1. Yeni tenant kaydında sessiz `FirmaId = 1` varsayımını kaldırın. UI isteğinde firma kimliği doğrulanmalı; arka plan işi firma kapsamını açıkça almalı. Sistem çapında iş, yalnızca tanımlı ve denetlenen bir sistem bağlamıyla yürümeli.
2. Lisans firma kodu eşleşmediğinde varsayılan firmaya sessiz atama yapmayın; aktivasyonu durdurun ve yöneticiye açık hata verin.
3. Denetim kaydında sabit kullanıcı ID'lerini kaldırın. Kullanıcı, firma ve işlem kimliklerini gerçek çağrı bağlamından alın. Audit tablosu erişilemiyorsa kayıp sessizce geçilmesin; uyarı ve operasyonel alarm üretin.
4. Luca portal parolasını güvenli anahtar yönetimiyle şifreleyin. Mevcut düz metin değerleri tek seferlik şifreli forma dönüştürün; başarılı dönüşüm sonrasında düz metni temizleyin. Anahtar uygulama deposunda açık tutulmamalı.
5. Luca, EBYS, e-fatura ve webhook veri akışlarını sağlayıcı bazında belgelendirin. TLS, timeout, retry/idempotency ve gönderilen kişisel verinin kapsamını doğrulayın. Harici servislerin kapalı olduğu senaryoları tanımlayın.

**Kabul senaryoları:** Firma A kullanıcısı Firma B verisini okuyamaz/yazamaz; firma bağlamı eksik background job yanlış firmaya yazmaz; audit kaydı yapan kullanıcı doğrudur; audit altyapısı hatası alarm üretir; Luca parolası DB/log/backup içinde düz metin görünmez; dış istekler yapılandırılmış TLS ve timeout kurallarına uyar.

## Faz 3 — Başlangıç, şema yönetimi ve kurtarma

**Kapsam:** Y-6, O-1, O-4, O-6, O-7; D-5, D-6.

1. `RunScopedSafeAsync` işlerini kritik ve opsiyonel olarak sınıflandırın. DB bağlantısı, gerekli şema ve ilk yönetici kurulumu hata verirse uygulama sağlıklı görünerek ayağa kalkmasın. Hata health/readiness durumunda görülsün.
2. `PendingModelChangesWarning` susturmasını kaldırmadan önce model/migration farkını bulun. SQLite ve desteklenecek her sağlayıcı için temiz kurulum ve upgrade yolu yazın. SQL Server/MySQL gerçek anlamda desteklenmiyorsa menü/ayar seçeneklerini kaldırma veya destek dışı olduğunu açıkça bildirme kararı verin.
3. `DatabaseBackupService.RestoreBackupAsync` davranışını sağlayıcı bazında tasarlayın. Yedek doğrulama, önceki DB'yi koruma, geçici hedefe restore, bütünlük kontrolü ve atomik devreye alma adımlarını ekleyin.
4. DataSync şema ön koşullarını doğrulasın; FK'leri kapattığı aktarımda sonradan tutarlılık kontrolü yapsın ve hatalı satırları raporlasın. Aktarım tekrar çalıştırılabilir ve transaction sınırları açık olmalı.
5. DataProtection anahtarlarının yedeği/erişim izinleri ile S3 ve yerel dosya depolama seçeneklerinin hangi koşulda kullanılacağını netleştirin.

**Kabul senaryoları:** boş DB kurulumu; desteklenen mevcut DB yükseltmesi; geçersiz migration'da hazır olma kontrolünün başarısız olması; SQLite ve seçili sunucu sağlayıcısında yedekten ayrı ortama geri yükleme; geri yükleme sırasında mevcut DB'nin korunması; DataSync sonrasında FK/row count tutarlılığı.

## Faz 4 — Kod kalitesi, hata görünürlüğü ve CI

**Kapsam:** Y-1, Y-2, Y-7, Y-8, Y-10, Y-11; O-8, O-9, O-10, O-12.

1. CI'daki var olmayan test projesini gerçek projeye bağlayın veya yeni test projesi oluşturun. `.slnx` değişikliklerini workflow tetikleyicilerine ekleyin. Her PR'da restore/build ve test raporu üretilsin.
2. Muhasebe, fatura ve webhook boş `catch` bloklarında hatayı bağlama uygun şekilde loglayın; kritik kayıtta işlem hatası kullanıcıya görünür olsun. Scraper gibi beklenen başarısızlıklar ayrıştırılsın ve gürültü üretmesin.
3. `NotImplementedException` ile yayımlanan PDF ve Luca TODO yolları için karar verin: uygulayıp kabul ölçütü ekleyin veya menü/endpoint'i kullanılabilir gibi sunmayın.
4. Sync-over-async çağrılarını gerçek async zincire taşıyın; iptal token'ı ve timeout davranışını koruyun.
5. Tarih alanlarını UTC anı mı yerel takvim tarihi mi olarak kullanıldıklarına göre sınıflandırın. Npgsql legacy timestamp switch'ini testli ve aşamalı veri geçişi olmadan kaldırmayın.
6. NuGet audit bastırmalarını advisory ve paket sürümleriyle tek tek inceleyin; güvenli yükseltme mümkünse yapın, istisna kalırsa gerekçe ve bitiş tarihi ekleyin.
7. Lisans imza/normalizasyon, seed ve DB introspection tekrarlarını geçiş tamamlandıktan sonra ortak modüllerde birleştirin. Ölü DbContext/proje dokümanlarını ancak referans taramasından sonra kaldırın.
8. DataSync yardımındaki parola örneğini, raporlardaki sırları ve yapılandırma örneklerini maskeleyin. Gerçek sırın kaynakta bulunması hâlinde dosyadan silmeye ek olarak döndürme yapın.

**Kabul senaryoları:** GitHub Actions temiz checkout'ta çalışır; en azından lisans doğrulama, parola geçişi, firma izolasyonu, DB backup/restore ve ilgili rapor/hesap mantığı otomatik test edilir; test başarısızlığında pipeline kırmızı olur; PDF özelliği ya kullanılabilir ya da kullanıcıya kapalıdır.

## Faz 5 — Ürün, desteklenen sağlayıcılar ve paketleme

**Kapsam:** O-1, O-11, O-13, O-14; D-1…D-4, D-7, D-8; değerlendirme raporundaki CDN bulgusu.

1. Docker üretim profilinde örnek sırlarla devam etmeyi engelleyin; örnek/development profili ile üretim profili ayrıştırılsın.
2. Lisans `AllowedVersion` politikasını ürün sürümleme yaklaşımıyla uyumlu hale getirin; paket sürümü ve lisans geçerliliği için tek kaynak kullanın.
3. README, kurulum komutları, proje listesi ve test talimatlarını diskteki gerçek yapıyla eşitleyin. Olmayan Infrastructure/Service projeleri için ya dokümanı düzeltin ya da projeyi gerçekten oluşturun.
4. `bin/obj`, log, temp ve artifact dosyaları için Git izleme durumunu envanterleyin. Sadece gerçekten izlenen dosyaları depodan çıkarın; kullanıcıya ait `.kilo/` gibi yerel klasörleri topluca temizlemeyin.
5. İnternetsiz çalışma bir ürün gereksinimiyse App.razor/CDN varlıklarını yerelleştirin ve tüm kritik ekranları ağ kapalıyken doğrulayın. PWA offline desteği ayrı, açık bir kapsam kararı olmalı.
6. DTO doğrulama kapsamı, render mode tekrarları ve tenant interface kullanımı için ayrı bakım işleri açın; bu maddeleri güvenlik düzeltmeleriyle aynı anda geniş çaplı yeniden yazmayın.

**Kabul ölçütleri:** Müşteri/upgrade paketleri yalnızca gerekli dosyaları içeriyor; test/demo araçları ve lisans özel anahtarı müşteri paketinde yok; gerçek sır bulunmuyor; offline vaadi varsa ekran/varlıklar internet olmadan açılıyor; README ve paket içeriği eşleşiyor.

## Faz 6 — Yayın kapısı ve üretim kararı

**Kapsam:** Bütün fazların kanıtı; KVKK/hukuk ve müşteri işletim hazırlığı.

- Temiz kurulum ve mevcut sürümden upgrade provası yapılmış.
- Lisans oluşturma/doğrulama/iptal/süre bitimi ve eski lisans geçişi kanıtlanmış.
- İki firma ve farklı rollerle URL, API, hub, dosya, rapor ve yazma erişimi sınanmış.
- DB yedeği izole ortama geri yüklenmiş ve bütünlük kontrolü kaydedilmiş.
- Migration ve DataSync denemesi kopya veride tamamlanmış.
- Kritik CI kontrolleri geçiyor; NuGet uyarıları ve açık istisnalar kayıtlı.
- Kurulum, sır döndürme, yedek geri yükleme, veri saklama ve olay müdahale talimatları teslim edilmiş.
- Kullanıcıya açık rapor/çıktılar ve Rent a Car belgeleri için gerekli iş/hukuk onayları alınmış.

**Go/No-Go:** Herhangi bir P0 kabul ölçütü başarısızsa müşteri kurulumu yapılmaz. P1 başarısızlıkları için yazılı risk sahibi, geçici önlem ve bitiş tarihi olmadan satış onayı verilmez.

## Özgün rapor maddelerinin faz eşlemesi

| Kaynak madde | Faz |
|---|---|
| K-1, K-2, K-3, K-4, K-5, K-6 | 1 |
| Yönetici rolündeki test hesabı (değerlendirme ek bulgusu) | 0–1 |
| Y-3, Y-4, Y-9; O-2, O-3, O-5 | 2 |
| Y-6; O-1, O-4, O-6, O-7; D-5, D-6 | 3 |
| Y-1, Y-2, Y-7, Y-8, Y-10, Y-11; O-8, O-9, O-10, O-12 | 4 |
| O-11, O-13, O-14; D-1, D-2, D-3, D-4, D-7, D-8; CDN offline davranışı | 5 |
| Teslimat öncesi doğrulama ve kalan mevzuat/iş onayları | 6 |

### Düzeltme bekleyen rapor maddeleri

- O-13 ve D-8'deki Git'te izlenen `bin/obj` ve log dosyaları iddiası mevcut incelemede doğrulanmadı; önce güncel `git ls-files` envanteri alınmalı. `test_all.txt` izleniyor görünüyordu.
- K-2'deki 192 sayfa sayısı doğrudur; bu sayfaların hepsinin anonim kullanıcıya veri verdiği kanıtlanmış değildir. Faz 1 erişim testleri sonuç vermeli.
- K-3'te kayıtsız `Cookies` şeması doğrulandı; fakat Blazor özel authentication state provider kullandığı için Blazor devresinin tamamının çökeceği varsayılmamalıdır.
- Y-1/Y-6/O-4 için başlangıç sarmalayıcısı hatayı logluyor ama kritik hatada devam ediyor. Düzeltme “log eklemek” değil, kritik işlerde başlangıcı/ready durumunu fail-closed yapmaktır.

## Bağımlılıklar ve karar gerektiren noktalar

1. Özel lisans imza anahtarını kim, nerede yönetecek? Yeni lisans biçimi hangi tarihte zorunlu olacak?
2. Üretimde desteklenecek DB sağlayıcıları hangileri? Sadece PostgreSQL/SQLite kalacaksa diğer sağlayıcıların kaldırılması ayrıca sürüm kararıdır.
3. Blazor oturumu için mevcut özel AuthenticationStateProvider/ProtectedSessionStorage akışının circuit, yenileme, sunucu render ve çıkış davranışı ayrıca gözden geçirilmeli; route düzeyindeki `[Authorize]` bu yaşam döngüsü doğrulamasının yerine geçmez.
4. Lisans ve admin hesaplarının mevcut müşterilerdeki geçişi kim tarafından ve hangi bakım penceresinde yürütülecek?
5. Tam internet kesintisi ürün gereksinimi ise CDN ve harita sağlayıcısı için yerel alternatif/dağıtım paketi kararı verilmelidir.

## Uygulama günlüğü

**Son güncelleme:** 2026-10-02. Aşağıdaki değişiklikler Faz 1'in ilk uygulama dilimidir; fazın tamamlandığı veya satış kabul ölçütlerinin geçtiği anlamına gelmez.

| İş | Durum | Not |
|---|---|---|
| Bilinen admin/test hesabı seed'ini kaldırma | ✅ Uygulandı | Var olan kullanıcı etkinleştirilmez veya parolası değiştirilmez. Boş DB için sır yapılandırılmış ilk admin kurulumu eklendi. |
| Parola deneme politikası | ✅ Uygulandı | Yeni parolalar en az 12 karakter; beş başarısız denemede 15 dakika kilit. Legacy hash doğrulaması geçiş tamamlanana dek korunuyor. |
| JWT varsayılanı ve sır | ✅ Uygulandı | API varsayılan şeması Bearer. Kaynak ayarlarından sabit sır çıkarıldı; Production ve Compose sır verilmeden başlamaz. Development'da süreç ömürlü rastgele sır üretilir. |
| Sayfa, API ve hub erişimi | ✅ İlk katman uygulandı | Razor route'ları varsayılan olarak `[Authorize]`; açık sayfalar ayrıca anonim. API Bearer varsayılanı; AuthController ve EvrakHub korumalı; dosya recovery ve Development backfill Admin+Bearer, detaylı health Admin gerektiriyor. Anonim güzergah Excel importu kapatıldı, metrik endpoint'i ve detaylı sağlık çıktısı oturum gerektiriyor. Kalan servis yazma yetkileri matrisi ayrıca tamamlanmalı. |
| Güzergâh Excel import yetkisi ve firma seçimi | ✅ Güvenli varsayılan eklendi | Import yalnızca Admin Bearer kabul ediyor; `firmaId` zorunlu ve aktif firma olarak doğrulanıyor. Blazor importu da aktif firma seçili değilse duruyor; `1` varsayılanı kaldırıldı. Kullanıcı-firma üyelik modeli olmadığı için kapsamlı tenant yetkisi Faz 2'de tamamlanmalı. |
| Audit kullanıcı kimliği | ✅ İlk düzeltme uygulandı | HTTP/JWT için `KullaniciId` veya `NameIdentifier`, Blazor circuit için AuthenticationStateProvider kullanılıyor. Aktif firma audit bağlamının arka plan ve sistem işleri için açıkça taşınması Faz 2'de sürüyor. |
| Yeni kayıtlarda firma kapsamı | ✅ Fail-closed kural uygulandı | `ApplicationDbContext` artık firma bağlamı yokken yeni tenant kaydına `FirmaId=1` atamıyor; kaydı açıklayıcı hata ile durduruyor. Background/system yazımları explicit firma bağlamı ile güncellenmeli ve ayrıca doğrulanmalı. |
| Lisans-firma eşleştirmesi | ✅ Sessiz varsayılan kaldırıldı | Lisans kodu mevcut aktif firmayla eşleşmiyorsa aktivasyon duruyor. Yalnızca firma kaydı bulunmayan temiz kurulum lisans kodundan ilk firmayı oluşturabilir. |
| Yedek geri yüklemede eksik FirmaId | ✅ Açık hedef zorunlu | Eski yedekte FirmaId eksik olan satırlar için aktif hedef firma seçilmeden geri yükleme yapılamıyor; otomatik ilk firmaya atama kaldırıldı. |
| İhale örnek verisi firma bağı | ✅ Uygulandı | Örnek güzergâh, şoför, araç, proje ve puantaj aktif firma altında oluşturuluyor; firma seçilmeden örnek veri üretimi reddediliyor. |
| EBYS işlem sahibi | ✅ Sabit kullanıcı kaldırıldı | Atama ve evrak hareketleri Blazor kimlik durumundaki gerçek `NameIdentifier` değerini kullanıyor; kimlik belirlenemezse işlem kaydedilmiyor. |
| Bakım uyarısı alıcısı | ✅ Sabit kullanıcı kaldırıldı | Bildirim `KullaniciId=1` yerine aktif Admin/Yönetici kullanıcılarına gönderiliyor; alıcı yoksa uyarı loglanıyor. Çoklu firma kullanıcı hedeflemesi üyelik modeli sonrası netleştirilmeli. |
| Firma seçimi ve circuit geri yükleme | 🟡 Geçici koruma | MainLayout ve `/firma-sec` yalnız Admin'e firma değiştirme/Tüm Firmalar seçimi veriyor. Diğer kullanıcılar varsayılan aktif firmaya sabitleniyor; çıkışta firma seçimi temizleniyor. Kullanıcı-firma üyelik tablosu yokken bu geçici sınırlama, gerçek üyelik yetkilendirmesinin yerini tutmaz. |
| Lisans hash doğrulaması | ✅ Uygulandı, geçiş notu gerekli | Eksik/okunamayan hash lisansı reddeder; imza/hash karşılaştırmaları sabit zamanlıdır. Lisans hash dosyası kayıp mevcut kurulumda lisansın yeniden yüklenmesi gerekir. |
| Asimetrik lisans imzası | ⏳ Bekliyor | Özel anahtarın şirket içi imzalayıcıya taşınması ve mevcut lisansların geçiş planı belirlenmeden imza biçimi değiştirilmedi. |
| İlk yönetici parolası değiştirme, API/hub/servis erişim matrisi, JWT sır döndürme ve MFA | ⏳ Bekliyor | Faz 1 kabul senaryoları uygulanıp doğrulanmalı. Müşteri kurulumlarındaki sırların döndürülmesi dağıtım sahibinin bakım penceresini gerektirir. |

Faz 1 halen kısmi durumdadır; MFA, asimetrik lisans geçişi, oturum yaşam döngüsü ve tüm servis yetkileri bekliyor. Faz 2 de kısmi durumdadır: yeni kayıtlarda firma fallback'i kapatıldı, ancak background/system yazımlarının explicit firma kapsamıyla gözden geçirilmesi ve kullanıcı-firma üyelik/yetki modelinin tamamlanması gerekir. Program başlangıcındaki eski satır backfill'i mevcut veriyi etkileyebileceğinden envanter ve izole kopya doğrulaması yapılana kadar değiştirilmedi. Kabul senaryoları ve yayın kapısı için ayrı, izole veritabanı kopyalarında doğrulama yapılmalıdır.
