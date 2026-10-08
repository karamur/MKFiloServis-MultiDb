# MKFiloServis — Satışa Çıkarım Güncel Durum Raporu

> **Güncel görev takibi (2026-10-06):** [Satışa çıkarım görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md). [2026-10-05 son durum](SATISA-CIKARIM-SON-DURUM-2026-10-05.md) ve bu dosya tarihsel uygulama/kanıt eklerini korur.

**Rapor tarihi:** 2026-10-02  
**Son güncelleme:** 2026-10-06
**Kapsam:** Web, Shared, LisansDesktop, DataSync, Client, CI ve müşteri paketleme akışları.  
**Esas alınan sürüm:** Yerel çalışma ağacı; commit ve müşteri dağıtımı tamamlanmış sayılmaz.  
**Yöntem:** Kaynak incelemesi; rapordaki sonraki düzeltmeler, kayıtlı derleme/publish sonuçları ve izole çalışma zamanı kanıtlarıyla tüm sarı maddelerin karşılaştırılması. Bu renk güncellemesinde yeni test, derleme veya gerçek restore çalıştırılmadı.

**Test sırası (2026-10-08):** Kalan kod düzeltmeleri sonrası toplu nihai doğrulama; [son aşama test planı](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md).

## 1. Yönetici özeti

İlk analizdeki önemli güvenlik ve veri bütünlüğü sorunları için kod düzeltmeleri uygulanmıştır. Lisanslama şirket içi LisansDesktop programında yürütülür; Web yalnız açık anahtarla ticari imzayı doğrular. Lisans programı **Lisanslar**, **Anahtar ve Yedek**, **Paketleme** sekmeleriyle sadeleştirilmiştir.

**Satışa hazırlık kararı: 🟡 Kabul ve açık kod işleri tamamlanmalı.** Derleme başarısı mevcut; müşteri kurulumu, lisans geçişi, erişim/tenant izolasyonu ve şifreli belge kurtarma için uçtan uca kabul kanıtı tamamlanmamıştır. Yeniden analizdeki N-1/N-2/N-3 kaynak düzeltmeleri uygulanmıştır; bağımsız kabul sınırları ve diğer açık işler aşağıda belirtilir.

**Renkler:** 🟢 kaynak düzeltmesi/işlem için kanıt mevcut · 🟡 kısmi veya kabul bekliyor · 🔴 açık kod/iş · ⚪ ürün kararı veya düşük öncelikli takip. Yeşil durum, bütün çalışma zamanı senaryolarının geçtiği anlamına gelmez.

### Sarı madde gözden geçirmesi — 2026-10-04

Dosyanın tamamındaki sarı maddeler sonraki ekler ve mevcut kaynakla karşılaştırıldı. Tamamlanan kod/izole kontrol kısımları yeşile ayrıldı; gerçekte yapılmamış kabul veya açık uygulama işleri sarı kaldı. Önceki “henüz yapılmadı” ifadeleri sonraki kanıtlarla güncellendi.

| Önceden sarı maddede yer alan tamamlanmış kısım | Güncel durum | Açık kalan |
|---|---|---|
| N-1 sürüm politikası ve güncelleme sürümü kontrolü | 🟢 Kod + izole harness | 🟡 Gerçek imzalı müşteri lisansı/paket kabulü |
| N-2 arşiv oluşturma, staging ve key ring probu | 🟢 Kod + izole harness | 🟡 Gerçek DB/belge/credential ve farklı makinede tam kurtarma |
| Audit yeni kimlik/rollback/tekrar deneme | 🟢 Minimal SQLite'ta 12 kontrol | 🟡 Tam model/gerçek DB ve diğer toplu SQL yazımları |
| Filo ilişkilerinde firma kontrolü | 🟢 Kod + 57 izole kontrol | 🟡 Eski kayıtlar, DB kısıtları ve gerçek oturum kabulü |
| Banka şablonu oluşturma firma kapsamı | 🟢 Kod + 46 izole kontrol | 🟡 Gerçek import/oturum/PostgreSQL kabulü |
| Banka CSV/Excel kolon okuyucusu | 🟢 Kod + kayıtlı derleme | 🟡 Gerçek dosyalarla runtime kabulü |
| Banka import firma, hatalı satır ve borç/alacak kontrolü | 🟢 Kod + kayıtlı derleme | 🟡 Import runtime kabulü, eşzamanlı tekilleştirme ve referanssız hareket politikası |
| Maaş snapshot aktif firma/personel doğrulaması | 🟢 Kod + kayıtlı derleme | 🟡 Runtime, eski ilişkiler ve eşzamanlı yazımlar |
| Maaş ekranı snapshot hata bildirimi | 🟢 Kod + kayıtlı derleme | 🟡 Etkileşimli ekran kabulü |
| Maaş liste ve detay panelinde eski sonuç koruması | 🟢 Kod + kayıtlı derleme | 🟡 Hızlı seçim değişikliği kabulü ve başlamış servis yazımları |
| Fatura şablonu varsayılan değişiklikleri ve firma kapsamı | 🟢 Kod + kayıtlı derleme | 🟡 Runtime rollback ve eşzamanlı tek varsayılan kabulü |
| Fatura grup şablonu varsayılan yazımları | 🟢 Kod + kayıtlı derleme | 🟡 Runtime, HTTP firma/kullanıcı kapsamı ve DB tekilleştirme kabulü |
| Fatura grup şablonu kullanıcı sahipliği | 🟢 Kod; servis okuma/yazım kapsamı ve API 403 yanıtı | 🟡 Gerçek HTTP/circuit kullanıcı ve ortak yazım yetkisi kabulü |
| Firma geneli fatura grup şablonu yazım yetkisi | 🟢 Mevcut fatura hazırlık düzenleme izniyle servis kontrolü | 🟡 Yetkili/yetkisiz kullanıcı ve rol değişimi runtime kabulü |
| Fatura grup şablonu API giriş/hata yanıtları ve istek iptali | 🟢 Kod; pozitif kimlik, 400/403/404 ve RequestAborted aktarımı | 🟡 Gerçek HTTP yanıtları, iptal ve rollback kabulü |
| Fatura şablonu okuma ve logo/kaşe yazım kapsamı | 🟢 Kod; açık firma, silinmeme, NoTracking okuma ve tracked görsel yazımı | 🟡 Gerçek oturum/audit rollback, PDF/e-posta runtime ve PostgreSQL kabulü |
| Fatura PDF/önizleme/e-posta firma kapsamı | 🟢 Kod; kayıtlı fatura, aynı firma aktif şablon ve tek kaynaklı e-posta hazırlığı | 🟡 Gerçek PDF/SMTP/oturum, eski ilişkiler ve PostgreSQL kabulü |
| Fiziksel dosya silme hata bildirimi | 🟢 Kod; özlük ekranı uyarısı, yol/kimlik logu ve yerel/S3 hata iletimi | 🟡 Gerçek silme kabulü ve yetim dosya temizliği |
| Dosya silme retry günlüğü ve aday envanteri | 🟢 DP ile şifreli atomik günlük, yenilenen worker lease'i; Admin ekranında şifreli orphan ve şifresiz aday raporu; tam model SQLite'ta referanslı dosya korunup referanssız dosya worker ile silindi; Release testleri 30/30 | 🔴 Gerçek müşteri DB/depo kabulü, referanslı eski açık dosya geçişi, DB commit-kuyruk crash penceresi ve referans kontrolü/unlink yarışı |
| Tedarikçi evrak DB/dosya silme sırası | 🟢 Kod; önce DB/audit, sonra tüm dosyaların temizliği ve kısmi hata bildirimi | 🟡 Gerçek DB/disk/ekran kabulü, kalıcı yeniden deneme ve diğer servisler |
| Araç evrak DB/dosya silme sırası | 🟢 Kod; DB önce, tracked hedefler ve ekran temizlik uyarısı | 🟡 Gerçek DB/disk/ekran kabulü, ortak kayıt rollback kabulü ve kalıcı yeniden deneme |
| Araç belge ve tarih senkronizasyonu ortak kaydı | 🟢 Kod; üç yazım tek SaveChanges ve bekleyen belge değişiklikleriyle tarih hesabı | 🟡 Gerçek rollback, eşzamanlı yazım ve NoTracking kabulü |
| Araç plaka geçmişi/aktif plaka ortak kaydı | 🟢 Kod; tek SaveChanges, tracking ve bekleyen kayıtlarla aktif plaka hesabı | 🟡 Gerçek rollback, tarih semantiği, DB tekillik ve oturum kabulü |
| Plaka yazımı firma/cari kontrolü | 🟢 Kod; tek firma, araç üzerinden kapsam, aynı firma cari ve aynı context mükerrer kontrolü | 🟡 Gerçek oturum/rollback, eski ilişkiler ve DB tekillik kabulü |
| Araç yazımı sonrası önbellek temizliği | 🟢 Kod; commit sonrası araç öneki ve kısmi import sonunda tek temizlik | 🟡 Liste/firma/Excel kabulü, backend arızası ve eşzamanlı factory yarışı |
| Cache temizliği sonrası eski factory yayını | 🟢 Kod; süreç içi sürüm kontrolü ve set/remove kilidi | 🟡 Gerçek yarış/backend/iptal/yük ve çok süreçli Redis kabulü |
| Araç cache anahtarı/sorgusu firma seçimi | 🟢 Kod; yakalanmış firma, açık sorgu koşulu ve seçim sürümüyle yeniden deneme | 🟡 Circuit hit/miss/firma geçişi ve ekran sonuç yayınlama kabulü |
| Araç ekranı ana liste yükleme sürümü/hata bildirimi | 🟢 Kod; çağrıya özel sonuçlar, firma olayında yenileme ve kalıcı yeniden deneme uyarısı | 🟡 Hızlı geçiş/hata/Dispose kabulü ve ayrı modal/yazım akışları |
| Araç plaka modalı yazım sonuçlarının seçim kontrolü | 🟢 Kod; ortak işlem akışı, modal/firma sürümü, form kilidi ve hata bildirimi | 🟡 Modal/firma geçişi, hata/Dispose kabulü ve başlamış servis yazımları |

**Tekrar kontrol sonucu:** Dosyanın tüm sarı satırları ikinci denetim raporundaki kanıtlarla ve ilgili servis/ekran kaynaklarıyla yeniden karşılaştırıldı. Banka import ve fatura şablonu düzeltmeleri özet tabloya da yeşil olarak eklendi. Sarı açıklamalardaki tamamlanmış kod/izole kontrol ifadeleri ayrıldı; sarı durum yalnız kalan iş veya kabulü gösterir. Lisans müşteri geçişi, gerçek kurtarma, fiziksel dosya temizliği ve satış kabulü açık kaldı. Bu gözden geçirme yalnız bu raporu günceller.

## 2. Yapılan düzeltmeler

| Alan | Uygulanan düzenleme | Kalan sınır |
|---|---|---|
| 🟢 Lisans imzası | Ticari RSA-PSS, ortak açık anahtar, Web lisans üretiminin kapatılması, legacy doğrulamanın varsayılan reddi | Mevcut müşteri hakları ve modüllü v3 geçişi kabul edilmedi |
| 🟢 Modül lisanslama | Programda 15 modül seçimi; v3 imzalı haklar; sayfa/API/menü/dashboard/arama kısıtları; Admin muafiyeti yok | Eski ticari lisanslar modül seçilerek yeniden üretilmeli; çalışma zamanı kabulü açık |
| 🟢 Lisans programı | Program içi DPAPI anahtar deposu; parola korumalı `.mkkey` yedek, doğrulama ve içe alma; sade sekmeli ekran | Gerçek yeni yedek ve bağımsız geri yükleme kabulü bekliyor |
| 🟢 Erişim koruması | Global Blazor `[Authorize]`, controller Bearer varsayılanı, hub koruması ve uç envanteri | Rol/firma izolasyonu ve oturum iptali kabulü bekliyor |
| 🟢 Parola | Güçlü parola kuralları, PBKDF2, eski SHA-256 hash yükseltmesi, DB'de 15 dk kilit | Tüm giriş/sıfırlama/kilit yolları doğrulanmalı |
| 🟢 Development JWT | Bu bilgisayarda User Secrets ile kalıcı geliştirme anahtarı | Yeniden başlatmalar arasında token kabulü bekliyor; Production sırları ayrıca sağlanmalı |
| 🟢 PDF ve Luca | Proforma PDF ve UBL Invoice okuma; credential şifreleme/geçiş kodu | Görsel PDF ve gerçek portal kabulü bekliyor |
| 🟢 Tenant ve audit | Firma 1 fallback'leri kaldırıldı; audit aynı transaction'da. Yeni ekleme kimliği, rollback ve aynı context ile yeniden deneme izole SQLite'ta doğrulandı | Firma izolasyon kabulü, diğer Raw SQL/ExecuteUpdate yazımları ve gerçek PostgreSQL/SQL Server kabulü açık |
| 🟢 Restore ve DataSync | DB restore kontrolü; ortak yedek kapsamına key ring/dosya/ayar manifesti eklendi; DataSync snapshot, satır sayımı, FK ve sequence denetimleri | Tam dosya/anahtar kurtarma ve iki yönlü aktarım provası açık |
| 🟢 Başlangıç ve HTTP | Zorunlu başlangıç hatalarında durma; taze HTTP istekleriyle sınırlı retry | Eski DB ve gerçek entegrasyon kabulü açık |
| 🟢 Paketleme | Lisans üreticiyi müşteri paketinden ayıran kaynak kuralları; dahili EXE publish | Gerçek müşteri kurulum EXE'sinin içerik/kullanım kabulü açık |
| 🟢 Banka dosyası importu | Firma/şablon kapsamı, hatalı satırda yazımın durması, dosya içi referanslı mükerrer kontrolü, CSV/Excel okuyucu ve yön doğrulaması | Gerçek dosya runtime kabulü, eşzamanlı DB tekilleştirme ve referanssız hareket politikası açık |
| 🟢 Maaş snapshot ve ekranları | Firma/personel doğrulaması, tracked audit yazımları, görünür hata bildirimi ve liste/detay eski sonuç koruması | Runtime/UI, eski kayıtlar, eşzamanlı yazım ve muhasebeleştirilmiş dönem silme kabulü açık |
| 🟢 Fatura şablonları | Varsayılan değişiklikleri tek SaveChanges/audit akışında; tek aktif firma yazımı | Runtime rollback ve DB seviyesinde tek varsayılan kabulü açık; grup şablonu kod düzeltmesi son ekte |

## 3. Yeniden analiz bulgularının güncel durumu

| ID | Öncelik | Kanıt ve etki | Yapılması gereken |
|---|---|---|---|
| N-1 | 🟢 Kod ve izole sürüm kontrolleri tamamlandı | Ortak sürüm politikası boş/geçersiz ve 20 karakterden uzun değeri reddeder. Desktop sürüm hakkı alanları, ortak imza verisi ve UpdateService kontrolü uygulanmıştır. 2026-10-03 izole harness sürüm sınırı/biçimi ve güncelleme adı kontrollerini kapsar. | 🟡 Gerçek imzalı lisans, yeniden basım ve müşteri paketi kabulü. O-8'in diğer kod tekrarları açık. |
| N-2 | 🟢 Kurtarma hazırlığı kodu ve izole arşiv kontrolleri tamamlandı | ZIP manifest/hash/yol doğrulaması, staging'e açma ve yedek key ring probu uygulanmıştır. Gerçek CreateAsync/PrepareAsync metotları geçici kökte çalıştırılmıştır. DB dump testte sentetik işaretleyicidir; canlı restore yalnız DB'yi geri getirir. | 🟡 Gerçek DB + belge/credential kurtarma, farklı makine/profil, DPAPI/sertifika, S3 ve DB-belge tutarlılığı kabulü. [Kurtarma kapsamı](SIFRELI-BELGE-YEDEK-KURTARMA.md). |
| N-3 | 🟢 Tetikleme kodu düzeltildi | Push/PR dosya filtreleri kaldırıldı; `main` kapsamındaki tüm değişiklikler, Razor/ayar/betikler dahil, workflow'u tetikler. Manuel tetikleme korunur. GitHub'da çalışma sonucu henüz alınmadı. | Y-7 kök test projesi ve kritik CI test kapsamı ayrı açık işlerdir; tetikleme düzeltmesi bunları kapatmaz. |

**Anahtar kapsamı:** LisansDesktop `.mkkey` yedeği ticari lisans imzalama anahtarını kapsar. Web'in şifreli belgeleri ve credential dosyaları için kullanılan DataProtection anahtarlarının yedeği değildir.

## 4. Yapılması gereken işler

| Sıra | İş grubu | Tamamlanma ölçütü |
|---|---|---|
| 1 | N-2 / D-5 / O-6 / Y-3 — Şifreli belge kurtarma | Yeni anahtar modeliyle tam yedek; farklı makine/profilde belgeler açılır ve credential dosyaları çözülebilir |
| 2 | K-1 / N-1 / O-14 / O-8 — Lisans hakkı ve geçiş | Geçersiz sürüm reddedilir; programdan yedek/doğrulama/geri yükleme çalışır; yetkili müşteri envanteriyle modüllü v3 kabulü tamamlanır |
| 3 | K-2 / K-3 / K-5 / K-6 / R-4..R-6 — Güvenlik kabulü | Anonim/normal/Admin ve firma A/B erişimleri; bootstrap kapanışı, oturum iptali, hash yükseltme ve kilit senaryoları doğrulanır |
| 4 | R-3 / O-1 / O-4 / O-7 / Y-10 — Kurulum ve veriler | Desteklenen DB kapsamı açık; temiz/eski kurulum, aktarım ve geri dönüş kabulü; tarih semantiği belgelidir |
| 5 | Y-7 / N-3 / Y-1 / O-3 — CI ve iş bütünlüğü | Kök test projesi ve ilgili tetikleyiciler mevcut; muhasebe-stok/audit hataları işlem garantisiyle ele alınır |
| 6 | K-4 / O-12 / O-13 / R-7 / R-8 / Y-11 — Yayın hazırlığı | İfşa olmuş sırlar yenilenir; güncel paket taraması, silme kararları ve teslim commit'i gözden geçirilir |
| 7 | Y-2 / Y-9 ve düşük öncelikli işler | PDF/Luca/HTTP kabulü; depolama ve çevrimdışı ürün kapsamı belgelenir; eski context/render tekrarları değerlendirilir |

**Sağlayıcı sınırı:** SQL Server/MySQL için otomatik şema yükseltme desteği tamamlanmamıştır. Destek geliştirilmeden bu sağlayıcılar için tam kurulum/yükseltme desteği vaat edilmemelidir.

**Tarih sınırı:** PostgreSQL legacy timestamp davranışı açık kalır. Uyarı yalnız PostgreSQL kullanıldığında gösterilir. Kaynak tarihlerin saat dilimi ve sütun semantiği doğrulanmadan veri dönüşümü yapılmamalıdır.

## 5. Denetim raporuyla karşılaştırmadaki düzeltmeler

- **Y-6:** Uyarının loglanması şema uyumunun tamamlandığını kanıtlamaz; kabul açık.
- **Y-7:** Eksik test projesinin CI'de atlanması başarılı derleme sağlar; test kapsamı sağlamaz.
- **O-8/O-9/O-14 ve D-1..D-8:** İlk 39 maddenin eksik takipleri yeniden analizde listelendi.
- **D-2:** “Yalnız arayüz tanımı” ilk tespiti düzeltilmeli; entity uygulamaları ve firma kopyalama kullanımı mevcut.
- **O-13/R-8:** `test_all.txt` ve boş Infrastructure dosyası staged; eski raporlar ve Rent-a-Car analiz belgesi unstaged silinmiş. Silme gerekçeleri ve commit kararı açık.
- **K-1 ekranı:** Güncel anahtar butonları başlık menüsünde değil, **Anahtar ve Yedek** sekmesindedir.

## 6. Kanıt ve doğrulama sınırı

Önceki turlarda LisansDesktop ve Web Debug derlemeleri **0 uyarı, 0 hata** ile tamamlandı. LisansDesktop Release/win-x64 self-contained/single-file publish başarılı oldu; güncel dahili çıktı `setup/payload/LisansDesktop/MKFiloServisLisans.exe` konumundadır. Bu sonuç gerçek müşteri kurulum paketinin kabulü değildir.

İlk yeniden analiz sırasında kaynak düzeltmesi yapılmadı; sonraki devam turlarında kaynaklar değiştirildi ve derlendi. 2026-10-03/04 eklerinde izole geçici harness ve minimal SQLite kontrolleri kayıtlıdır; dolayısıyla raporun tümü için “otomatik/çalışma zamanı test yapılmadı” ifadesi geçerli değildir. Gerçek müşteri yedek/restore, migration veya müşteri kurulumu kabulü yapılmadı. O kesitte yeni tam bağımlılık zafiyet taraması ve geçmiş sır taraması yapılmamıştı; A-22 bağımlılık taraması 2026-10-06 kapanış ekinde tamamlandı. XML paket uyarısına yönelik sınırlı düzeltme aşağıda kayıtlıdır. İzole testler müşteri veritabanına yazım veya müşteri lisansı üretimi değildir.

## 7. Ayrıntılı kaynaklar

**Tarihsel uygulama notları:** Aşağıdaki ilk devam notları kendi yazıldıkları turu anlatır. N-1/N-2 için sonraki izole kontroller 2026-10-03 ekinde kayıtlıdır; güncel tamamlanma/kabul ayrımı üstteki tablo ve ilgili eklerde gösterilir.

**N-2 devamı:** `RecoveryArchive.cs`, `BackupService.cs` ve `DatabaseBackupService.cs` kaynaklarında yedekleme düzenlemeleri yapıldı. Son Web Debug derlemesi **0 uyarı, 0 hata** ile başarılı oldu. Mevcut `.mkkey` lisans yedekleri ve müşteri verileri değiştirilmedi. Gerçek yedek/restore veya otomatik test çalıştırılmadı; kod uygulaması bağımsız kurtarma kabulü değildir.

**N-1/N-3 devamı:** Ortak sürüm doğrulaması eklendi ve CI dosya filtreleri kaldırıldı. LisansDesktop Release/win-x64 çıktısı yeniden yayımlandı. Kaynak düzeltmesi sırasında müşteri lisansı üretilmedi, lisans hakları değiştirilmedi veya GitHub workflow'u çalıştırılmadı. Otomatik/çalışma zamanı kabulü henüz yapılmadı.

**O-14/O-8 devamı:** Lisanslar sekmesine **En Fazla Sürüm** ve **Sınırsız sürüm hakkı** eklendi. Yeni imzalama/geçmiş kayıtları seçilen hakkı kullanır; yeniden basım kayıtlı hakkı korur. Müşteri paketinin sürümü hakkı aşarsa işlem reddedilir. Web/Desktop v2 imza alanlarını ortak `LicenseSignaturePayload` oluşturur. Eski v2 biçimi korunur; diğer normalizasyon/introspeksiyon/seed tekrarları ve uyumluluk kabulü tamamlanmış sayılmaz.

- [Güncel 31 maddelik görev envanteri; 2026-10-02 tarihli ayrı 39 maddelik belge çalışma ağacında yoktur](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md)
- [İkinci düzeltme denetim raporu — güncel karşılaştırma Bölüm 23](DUZELTME-DENETIM-RAPORU-2.md)
- [İlk satışa çıkarım analizi — tarihsel bulgular](SATISA-CIKARIM-ANALIZ-RAPORU.md)
- [Program üzerinden lisans ve anahtar yönetimi](LISANS-IMZA-GECIS.md)

## Modül lisanslama güncellemesi — 2026-10-02

- 🟢 **Programdan seçim:** Yeni satış, yenileme, yeniden basım ve müşteri paketinde modül seçilir. En az bir seçim zorunludur; eski geçmiş kayıtlarına otomatik hak verilmez. Geçmiş/CSV modül bilgisini içerir.
- 🟢 **İmzalı hak:** Ortak `LicenseModules` sözleşmesiyle modül listesi v3 RSA-PSS imzasının parçasıdır. Web hakkı imzalı zarftan okur; Web DB şeması değişmez. v2 imza doğrulaması korunur ancak v2 ticari anahtar modül açmaz.
- 🟢 **Erişim:** Modül sayfaları ve ilgili API'ler `Licensed:<modül>` politikasıyla korunur. Admin dahil kullanıcı yetkileri lisans hakkını aşamaz. Menü, dashboard, genel arama, modül ayarları, dosya API'si, evrak hub'ı ve geliştirme arşiv işlemleri de sınırlandırıldı. Lisans değişiminde açık sayfa yeniden değerlendirilir; modül sayfalarında 30 saniyelik kontrol vardır.
- 🟢 **Modül kapsamı uygulandı:** Dosya API'si EBYS/Belgeler, evrak hub'ı Personel hakkı ister. Birleşik ekranların gerekli modülleri lisanslama sırasında birlikte seçilmelidir.
- 🟡 **Geçiş/kabul:** Sözleşmeye göre seçilecek modüllerle mevcut müşteri v3 yeniden basımı ve teslimi açık. Modül erişiminin çalışma zamanı/otomatik kabulü yapılmadı; hazır müşteri lisansı üretilmedi.
- **Kullanım:** [Modül seçimi ve lisans geçişi](LISANS-IMZA-GECIS.md). Web ve LisansDesktop birlikte güncellenmeli.

- 🟢 **Son derleme/çıktı:** Web Debug (`--no-restore -p:UseAppHost=false`) başarılı: **0 uyarı, 0 hata**. LisansDesktop Release/win-x64 self-contained/single-file publish başarılı; `setup/payload/LisansDesktop/MKFiloServisLisans.exe` yenilendi. Kaynak/belge diff kontrolü whitespace hatası bildirmedi. Bu sonuç çalışma zamanı kabulü değildir.

## Lisans kontrolü ve O-8 devamı — 2026-10-02

- 🟢 **Tam makine eşleşmesi:** Bilgisayar/kullanıcı adı önekiyle kabul kaldırıldı. Donanım parçası dahil tam makine kodu eşleşmelidir; yalnız boşluklar temizlenir, tire ve harf büyüklüğü korunur. Donanım/Windows kullanıcı kimliği değişiminde yeni makine koduna uygun lisans gerekir.
- 🟢 **Doğrulanmış önbellek:** DB kaydını okumak modül hakkı açmaz. Yalnız tam lisans doğrulaması başarılıysa doğrulanmış durum atanır; her doğrulama hatası önceki hakları temizler. Aktivasyon ve demo, hash yazımından sonra yeniden doğrulanır. `SaveLicenseAsync` aynı aktivasyon yolunu kullanır; ayrı doğrulamasız kaydetme akışı kaldırıldı. Gelecekte oluşturulmuş anahtar aktivasyondan önce reddedilir.
- 🟢 **Ortak normalizasyon:** Shared `LicenseIdentity`, Desktop firma/makine/telefon girişlerini ve Web firma kodu/tam makine karşılaştırmasını ortaklaştırır. İmzalı alanlar doğrulama sırasında yeniden yazılmaz. Firma adı için toleranslı eşleştirme, introspeksiyon ve seed tekrarları hâlâ ayrı takiplerdir; O-8 bütünüyle kapanmadı.
- 🟡 **Kabul:** Aynı bilgisayar/kullanıcı adıyla farklı donanım, eski/new makine kodları, bozuk hash, DB okuma sonrası yetki ve aktivasyon başarısızlığı senaryoları çalışma zamanında çalıştırılmadı. Müşteri lisansı veya canlı DB değiştirilmedi; otomatik test eklenmedi/çalıştırılmadı.

- 🟢 **Bu devamın derlemesi:** Son Web Debug derlemesi **0 uyarı, 0 hata** ile başarılı. LisansDesktop Release/win-x64 self-contained/single-file publish başarılı; dahili EXE yenilendi. Değişen kaynak ve belgelerde `git diff --check` whitespace hatası bildirmedi. Çalışma zamanı kabulü ayrı açık kalır.

## N-2 / D-5 / O-6 kurtarma hazırlığı devamı — 2026-10-02

- 🟢 **Kurtarma ekranı:** Admin için Ayarlar → Yedekleme listesine **Kurtarma Hazırla** eklendi. Yeni/harici ZIP listelenir; harici dosya türü/adı kontrol edilir, rastgele adlı geçici dosyada yükleme bitmeden listede yayınlanmaz.
- 🟢 **İzole açma:** Manifest/yol/hash/boyut kontrolünden geçen ZIP, depolama kökünde erişimi sınırlı yeni `RecoveryStaging/recovery-...` klasörüne açılır. Dizin geçişi, aygıt adları, bağlantılar ve boyut/dosya sayısı sınırı ihlalleri reddedilir; hata/iptal geçici dizini temizler. Var olan canlı dosyalar üzerine yazılmaz.
- 🟢 **Anahtar probu:** Yalnız yedekteki key ring ile ayrı sağlayıcı oluşturulur; yeni anahtar üretimi kapalıdır. Prob sonucu ve DB dump varlığı ekranda gösterilir. Prob çözülemezse dosya hazırlığı anahtar kurtarma başarısı olarak sunulmaz.
- 🟢 **Hazırlık doğrulandı:** Sonraki 2026-10-03 ekinde gerçek CreateAsync/PrepareAsync metotları geçici kökte çalıştırıldı; arşiv, staging, içerik ve key ring probu doğrulandı.
- 🟡 **Kalan kabul:** Gerçek DB + belge/credential kurtarma, farklı makine/profil, DPAPI/sertifika, legacy/S3 ve DB-dosya tutarlılığı açık. Canlı tam restore yapılmadı; N-2/D-5/O-6 bütünüyle kapanmadı.
- **Kullanım:** [Programdan kurtarma hazırlığı](SIFRELI-BELGE-YEDEK-KURTARMA.md).

- 🟢 **Derleme:** Son Web Debug (`--no-restore -p:UseAppHost=false`) başarılı: **0 uyarı, 0 hata**. Değişen kaynak ve belgelerde whitespace kontrolü temiz. Web root altında kurtarma hazırlığı reddedilir. Gerçek yedek/restore veya otomatik test çalıştırılmadı.

## N-1/N-2 düzeltme devamı — 2026-10-02

- 🟢 **N-1 alan uyumu:** Sürüm politikasına ortak `MaximumLength = 20` eklendi; bu değer `LicenseInfo.AllowedVersion` EF sınırıyla ve masaüstü giriş kutusuyla uyumludur. Daha önce politika 64 karaktere izin verirken DB alanı 20 karakterdi; geçersiz uzun hak artık paket ve imza aşamasında reddedilir.
- 🟢 **N-2 hazırlığı:** Kısıtlı staging'e açma, izole key ring probu ve dump varlığı kontrolü uygulandı; sonraki izole harness bu hazırlık yolunu doğruladı.
- 🟡 **N-2 tam kabul:** Gerçek dosya/DB restore ve yeni makinede belge/credential açma açık. Canlı geri yükleme otomatik başlatılmadı.
- 🟢 **Derleme/yayın:** Web Debug derlemesi 0 uyarı/0 hata ile başarılı; LisansDesktop Release/win-x64 self-contained/single-file publish başarılı. Değişen dosyalarda `git diff --check` temiz. Sürüm lisanslama ve bağımsız kurtarma çalışma zamanı kabulleri açık.

## N-1/N-2 devamı — güncelleme sürümü kontrolü — 2026-10-02

- 🟢 **N-1 — güncelleme paketleri:** `UpdateService` artık yerel sürüm karşılaştırıcısı ve parse hatasında izin veren fallback yerine ortak `LicenseVersionPolicy` kullanıyor. İzin verilen lisans sürümü veya ZIP adından çıkarılan güncelleme sürümü boş/geçersizse paket listede izinli görünmez ve kurulum reddedilir; `0.0.0` sınırsız lisansında bile paket sürümü biçimi geçerli olmalıdır.
- 🟢 **N-2 — hazırlık kapsamı:** Doğrulanmış arşivin izole alana çıkarılması uygulandı ve sonraki harness'te sınandı.
- 🟡 **N-2 — kalan kapsam:** Canlı dosya/ayar/DB'ye tam uygulama eklenmedi. Farklı makine/profil, DPAPI/sertifika, S3 ve DB-belge tutarlılığı açık.
- 🟢 **Derleme:** Web ve LisansDesktop Debug derlemeleri ayrı ayrı başarılı: her biri **0 uyarı, 0 hata**. Çalışma zamanı kabulü veya gerçek restore yapılmadı.

## N-1/N-2 çalışma zamanı doğrulaması — 2026-10-03

- 🟢 **N-1:** İzole geçici .NET harness ile lisans üst sınırı, bozuk/boş değer reddi, açık `0.0.0`, 20 karakter sınırı ve güncelleme sürüm çıkarımı sınandı. ZIP adındaki 5 bileşenli sürümün içinden son 4 bileşenin yanlışlıkla seçilebildiği bulundu; desen sıkılaştırıldı. Geçerli 2–4 bileşenli sürüm, aşan sürüm, bozuk sürüm ve sınırsız hak senaryoları tekrar çalıştırıldı.
- 🟢 **N-2:** Uygulamanın gerçek `RecoveryArchive.CreateAsync` ve `PrepareAsync` metotları yalnız geçici test kökünde çalıştırıldı. ZIP oluşturma/iç doğrulama, staging’e açma, dosya içeriği, DB dump işareti ve DataProtection probu başarılı. Arşiv içindeki dosya sonradan değiştirilince manifest/hash denetimi reddetti.
- 🟢 **İzole kanıt:** N-1/N-2 harness'inde toplam **18 kontrol geçti**; kapsam yukarıdaki sürüm/arşiv hazırlığı senaryolarıdır.
- 🟡 **Kapsam sınırı:** DB dosyası sentetik işaretleyiciydi; PostgreSQL dump restore edilmedi. Gerçek yedek, müşteri/üretim DB'si, belgeler ve credential'lar kullanılmadı. Ayrı makine/profilde gerçek belge çözme ve tam kurtarma açık.
- 🟢 **Derleme:** Regex düzeltmesinden sonra Web Debug **0 uyarı, 0 hata**. Harness geçici dizinde oluşturuldu; depoda test projesi veya test kodu bırakılmadı.

## Y-1 devamı — hızlı muhasebe kayıt bütünlüğü — 2026-10-03

- 🟢 `KolayMuhasebeService` içindeki fatura/masraf ve bağlı fiş, banka, stok değişiklikleri transaction'a alındı. Stok hareketi ekleme hataları artık yutulmaz; işlem başarısızsa ana yazımlar commit edilmez.
- 🟢 Hata dış catch'te işlem türü/cari/belge bağlamıyla loglanır ve başarısız sonuç olarak döner.
- 🟢 Muhasebe fişi aynı `ApplicationDbContext` üzerinden yazılır. PostgreSQL fiş sayacı komutu etkin EF transaction'ını kullanır; fiş ve sayaç artışı fatura/masraf işlemiyle beraber geri alınabilir.
- 🟡 Kapsam dışında kalan cari/hesap hazırlığı ayrı servis transaction'larında; diğer kritik muhasebe yolları ve gerçek PostgreSQL rollback provası açık.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata** ile geçti. Otomatik test veya DB yazımı yapılmadı.

## O-3 devamı — personel araç ve muhasebe bağlantı denetimi — 2026-10-03

- 🟢 Personel–araç eşleştirmesini kapatma/silme ve personel 335/195 muhasebe bağlantısı doğrudan `ExecuteUpdate` yerine tracked `SaveChanges` kullanıyor. Değişen kayıt kimliği/alanları denetim kaydına girer; personel işlemleri seçili personel kimliğiyle sınırlandırılır.
- 🟢 `ApplicationDbContext` yeni kayıtların DB tarafından verilen PK değerini aynı transaction içindeki ikinci adımda ilgili audit kaydına yazar. Hata durumunda açılan transaction rollback edilir; senkron/asenkron SaveChanges yolları kapsanır.
- 🟢 Audit kimlik yazımı/rollback/tekrar deneme, sonraki ekin minimal SQLite modelinde **12 kontrolle** doğrulandı.
- 🟡 Repo genelindeki diğer `ExecuteUpdate`/Raw SQL yazımları ve tam model/PostgreSQL/SQL Server kabulü açık.

## O-3 devamı — personel sıra numarası audit kapsamı — 2026-10-04

- 🟢 `SoforService.UpdateSiraNoAsync` tracked personel kaydı ve `SaveChangesAsync` kullanıyor; firma query filter korunuyor ve sıra numarası değişikliği audit'e giriyor.
- 🟢 **İzole audit kanıtı:** Sonraki audit ekinde minimal SQLite kontrolleri kayıtlıdır.
- 🟡 Diğer `ExecuteUpdate`/Raw SQL yazımları, tam model/PostgreSQL/SQL Server kabulü ve O-3'ün tüm kapsamı açık.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata**.

## O-3 devamı — personel özlük belge metadata temizliği — 2026-10-04

- 🟢 Belge silme akışında dosya metadata alanları artık tracked `PersonelOzlukEvrak` kaydında temizleniyor ve `SaveChangesAsync` ile audit ediliyor. Güncelleme personel/evrak/kayıt kimliğiyle sınırlandırılıyor.
- 🟡 Özlük fiziksel silme hatasının log/ekran bildirimi sonraki ekte kod olarak tamamlandı; yetim dosya temizliği, yeniden deneme ve diğer doğrudan SQL yazımları açık. Runtime DB kabulü yapılmadı.

## O-3 çalışma zamanı düzeltmesi ve depolama güvenliği — 2026-10-04

- 🟢 **Audit kimliği:** Önceki ikinci UPDATE, `SaveChanges(false)` sonrası henüz CLR nesnesine aktarılmamış `log.Id` değerini kullanıyordu. Yeni kimlik EF entry'den okunuyor; UPDATE tam bir satırı etkilemezse işlem başarısız olur.
- 🟢 **Transaction:** Context'in açtığı transaction execution strategy içinde yürütülür; değişiklikler commit'ten sonra kabul edilir. Hata halinde geçici kimlikler geri yüklenir. Mevcut transaction savepoint destekliyorsa iki audit aşaması aynı savepoint ile geri alınır; dış transaction commit edilmez. Savepoint desteklemeyen dış transaction'da rollback sorumluluğu çağırandadır.
- 🟢 **Runtime kanıtı:** Gerçek `ApplicationDbContext.SaveChanges` kodunu çağıran geçici harness ve minimal modelle SQLite bellek veritabanında **12 kontrol geçti**: senkron/asenkron kimlik yazımı, `false` overload, yapay audit UPDATE hatası, iki tablonun rollback'i, aynı context ile tekrar deneme, dış transaction rollback'i ve savepoint ile önceki yazımların korunması.
- 🟢 **Dosya yolu:** `SecureFileService` artık yalnız klasör adı önekiyle izin vermiyor. Ortak `StorageFilePath` uploads/Arsiv/Depo için seçilen kökün dışına çıkan yolları reddediyor. Gerçek yardımcı kaynakla **11 yol sınırı kontrolü geçti**; komşu klasör, üst klasör ve mutlak yol denendi.
- 🟢 **Paket:** Test restore'unda `System.Security.Cryptography.Xml 10.0.0` için NU1903 uyarıları görüldü; Web'de sürüm `10.0.12` olarak sabitlendi. Tekrar restore/testte bu uyarılar görülmedi. Kasıtlı sürüm sabitlemesi için yalnız NU1510 framework-tekrarı uyarısı paket üzerinde bastırılır; güvenlik uyarıları bastırılmaz. [NuGet sürüm kaydı](https://www.nuget.org/packages/System.Security.Cryptography.Xml/10.0.12).
- 🟡 **Kalan:** PostgreSQL/SQL Server retry ve commit hata kabulü, tam uygulama modeli, diğer toplu SQL yazımları ve yetim dosya temizliği açık. Testler müşteri/üretim veritabanına veya gerçek belgelere erişmedi; geçici projeler depo dışında tutuldu.
- 🟢 **Son doğrulama:** Web Debug restore/build **0 uyarı, 0 hata** ile başarılı; değişen dosyalarda `git diff --check` temiz. Toplam **23 izole kontrol** geçti.

## O-3 devamı — filo güzergâh eşleştirme güncellemesi — 2026-10-04

- 🟢 `FiloKomisyonService.UpdateEslestirmeAsync` artık `IgnoreQueryFilters` + toplu SQL UPDATE kullanmıyor. Filtreli SELECT ile mevcut kayıt yükleniyor; ücret, araç, şoför, güzergâh, kurum, kullanıcı, servis türü ve aktiflik tracked `SaveChangesAsync` ile audit'e giriyor. Mevcut firma kimliği ve oluşturma tarihi gelen nesneden kopyalanmıyor.
- 🟢 Gerçek servis ve `ApplicationDbContext.SaveChanges` koduyla, minimal ilişkisel model ve firma/soft-delete filtreleri kullanan SQLite bellek veritabanında **11 kontrol geçti**. Alanların yazılması, ücret için eski/yeni audit değerleri, firma/tarih korunması, başka firma, silinmiş kayıt/araç/firma ve bulunamayan kayıt reddi doğrulandı. Audit INSERT hatasında ücret güncellemesi rollback edildi.
- 🟢 İlişkili FK firma doğrulaması sonraki ekte tamamlandı; oluşturma/güncelleme servislerinde **57 izole SQLite kontrolü** kayıtlıdır.
- 🟡 Minimal model kontrolleri tam uygulama/circuit/PostgreSQL kabulünün yerini tutmaz. Diğer doğrudan SQL yazımları açık; müşteri verisi kullanılmadı.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.

## Filo eşleştirme ilişkilerinde firma doğrulaması — 2026-10-04

- 🟢 Oluşturma ve güncellemede kurum (`Cari`), güzergâh, araç ve personelin eşleştirmenin firmasına ait, silinmemiş ve mevcut query filter kapsamında erişilebilir olduğu servis içinde doğrulanır. Güncelleme firma kapsamını mevcut kayıttan alır; istemcinin FirmaId değeri kapsamı değiştirmez. Kullanıcı modeli global olduğundan isteğe bağlı kullanıcı için yalnız varlık/silinme kontrolü yapılır.
- 🟢 Oluşturma yalnız doğrulanmış scalar alanlarla yeni entity üretir; gönderilen PK, navigation nesneleri ve firma grafiği ekleme/yabancı anahtar değerlerini değiştiremez. İki çağıran ekran oluşturma sonucuna bağlı eski nesne kimliği kullanmıyor; listeyi yeniden yüklüyor.
- 🟢 Seferler ekranında `KurumFirmaId` için yanlış Cari → Firma kimlik dönüşümü kaldırıldı. Bu alan modelde `Cari` FK'sidir; kurum seçimi kaydetme ve düzenleme sırasında doğrudan cari kimliğiyle çalışır.
- 🟢 Gerçek oluşturma/güncelleme servisi ve SaveChanges koduyla minimal ilişkisel modelde **57 izole SQLite kontrolü geçti**. Dört ilişki için başka firma/silinmiş/bulunamayan/0 kimlikler, hem oluşturma hem güncellemede reddedildi; tenant filtresi kapalı modelde de açık FirmaId eşitliği korundu. Kullanıcı ve firma kontrolleri, gönderilen navigation grafiğinin yok sayılması, geçerli yazım/audit ve reddedilen işlemlerin iş/audit kaydı üretmemesi doğrulandı.
- 🟡 Tam uygulama/circuit/PostgreSQL kabulü, eski kayıtların ilişki tutarlılığı taraması ve veritabanı seviyesinde firma ilişkilerini koruyan kısıtlar açık. Bu değişiklik servis düzeyindeki yeni yazımları korur; müşteri verisi taşınmadı veya yeniden eşlenmedi. Diğer toplu SQL/audit işleri açık.
- 🟢 Son kaynak için Web Debug `--no-incremental` derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz. Seferler ekranının etkileşimli kullanıcı kabulü yapılmadı.

## O-3 devamı — banka kolon eşleme şablonları — 2026-10-04

- 🟢 `BankaImportService.SilAsync` filtreli tracked şablonu yükler, soft-delete ve silinme tarihini `SaveChangesAsync` ile kaydeder. Değişiklik audit'e girer; erişilemeyen, bulunamayan veya silinmiş şablonda başarısızlık açıkça bildirilir.
- 🟢 `KaydetAsync` mevcut şablonda firma filtresini ve silinmeme koşulunu korur. Bulunamayan güncelleme artık yeni kayıt eklemeye düşmez; istemcinin FirmaId değeri mevcut kayda kopyalanmaz. Negatif kayıt kimliği ve zorunlu tarih/tutar kolonlarının geçersiz değerleri reddedilir. İki yazımda `AsTracking` açıkça kullanılır.
- 🟢 Gerçek servis/SaveChanges koduyla minimal model, yalnız tenant query filter ve varsayılan `NoTracking` kullanan SQLite bellek veritabanında **16 kontrol geçti**. Geçerli güncelleme, eski/yeni değer audit'i, firma korunması, gizli/silinmiş/bulunamayan kayıt reddi, kolon/kimlik doğrulaması, soft-delete audit'i, başarısız çağrıların yeni kayıt üretmemesi ve audit hatasında silmenin rollback'i doğrulandı.
- 🟢 Yeni banka şablonunun aktif firma kapsamı sonraki ekte tamamlandı; oluşturma dahil **46 izole SQLite kontrolü** kayıtlıdır.
- 🟡 Gerçek dosya import/mükerrer kayıt, tam model/PostgreSQL kabulü ve diğer toplu SQL işleri açık; müşteri verisi kullanılmadı.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.

## Banka şablonlarında aktif firma yazım kapsamı — 2026-10-04

- 🟢 `BankaImportService` artık zorunlu scoped `IAktifFirmaProvider` bağımlılığı alır. Oluşturma/güncelleme/silme öncesinde tek pozitif firma seçimi gerekir; firma seçilmemiş veya Tüm firmalar modundaki yazımlar reddedilir. Güncelleme ve silme sorguları global filtreye ek olarak açık FirmaId eşitliği kullanır.
- 🟢 Oluşturmada null/0 FirmaId, sunucunun seçili firma kimliğiyle doldurulur; başka/negatif firma kimliği reddedilir. Yeni şablon için seçili firmanın veritabanında mevcut ve silinmemiş olması gerekir. Yalnız şablon alanları yeni entity'ye kopyalanır; istemcinin oluşturma/silinme metadata alanları kullanılmaz.
- 🟢 Gerçek servis/SaveChanges ile minimal model, **global tenant filtresi olmayan** ve varsayılan `NoTracking` kullanan SQLite bellek DB'sinde **46 kontrol geçti**. Önceki 16 kontrol tekrar kapsandı; aktif firma eksikliği/Tüm firmalar, diğer firmaya oluşturma, null/0 doğru firma ataması, bulunamayan/silinmiş firma reddi, firma 2 seçiliyken meşru oluşturma, metadata korunması ve yeni kayıt audit hatasında rollback doğrulandı.
- 🟡 Testte firma sağlayıcısı sentetiktir; gerçek oturum/circuit firma seçimi ve yetkilerinin tam kabulü, gerçek banka dosyası import/mükerrer kayıt akışı ve PostgreSQL kabulü açık. Diğer toplu SQL/audit işleri devam eder. Gerçek müşteri verisi veya kalıcı test projesi kullanılmadı.
- 🟢 Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.

## Banka dosyası içe aktarma kapsamı ve hatalı dosya koruması — 2026-10-04

- 🟢 **Kod düzeltildi:** Önizleme ve içe aktarma tek aktif firma gerektirir. İstenen hedef firma ve şablonun firma değeri aktif firmayla uyumlu olmalıdır; firma mevcut/silinmemiş olmalıdır. Kimliği olan şablon ayrıca aynı firma ve silinmeme koşuluyla veritabanında kontrol edilir. Kaydedilmemiş şablonlarla kolon denemesi korunur.
- 🟢 **Kod düzeltildi:** Negatif isteğe bağlı kolonlar, geçersiz atlanacak satır sayısı, boş ayraç/tarih formatı ve eksik kolonlu satırlar reddedilir. Satır ayrıştırma hatası varsa hiçbir finans hareketi eklenmez; önizleme hatalı veya boş sonucu başarılı göstermez. Finans hareketleri ve audit mevcut tek `SaveChangesAsync` akışını kullanır.
- 🟢 **Kod düzeltildi:** Referanslı hareketlerde tarih, kırpılmış referans, tutar ve borç/alacak yönü birlikte karşılaştırılır. HashSet hem mevcut kayıtları hem aynı dosyada kabul edilen satırları kapsar. Hiç yeni kayıt bulunmaması açıkça bildirilir.
- 🟢 **Kod düzeltildi:** Boş maaş veri listesiyle yapılan snapshot çağrısı kaldırıldı. Önceki çağrı maaş tutarlarını güncellemiyordu; banka importu artık maaş güncellemesi yapılmış izlenimi vermiyor.
- 🟢 **Sonradan tamamlanan:** CSV tırnak/ayraç ve Excel hücre/kolon uyumu sonraki ekte düzeltildi; mevcut kaynak aynı kolon okuyucusunu kullanır.
- 🟡 **Kalan kabul:** Yeni import davranışlarının runtime testi yapılmadı. Gerçek banka dosyaları, tam model/oturum/PostgreSQL, eşzamanlı yüklemede DB tekilleştirme ve referanssız hareket politikası açık. Önceki 46 şablon kontrolü import davranışlarının kanıtı değildir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen üç dosyada `git diff --check` temiz.

## Banka CSV/Excel kolon ayrıştırması — 2026-10-04

- 🟢 **Kod düzeltildi:** CSV/TXT okuyucusu `TextFieldParser` kullanır; seçili ayraç, tırnak içinde ayraç, çift tırnak kaçışı ve çok satırlı alanlar aynı kolon dizisine ayrıştırılır. Hatalı tırnak yapısında satır numarasıyla hata bildirilir ve finans yazımına geçilmez. Önizleme ve import aynı okuyucuyu kullanır.
- 🟢 **Kod düzeltildi:** Excel hücreleri artık `;` ile birleştirilmez; doğrudan kolon dizisine alınır. İlk kolon, aradaki boş hücreler ve boş satırların konumları korunur. Excel tarihleri ISO tarih metnine, sayısal değerleri şablondaki sayı ayracıyla uyumlu metne çevrilir. Hücre içindeki ayraç kolon kaydırmaz.
- 🟢 **Kod düzeltildi:** Başlık okuma aynı ayrıştırmayı kullanır; isteğe bağlı ayraç parametresi eklendi. Parametre yoksa CSV başlığında `;`, `,` ve tab adaylarından en çok kolon üreten seçilir. `.xls` destekleniyor izlenimi kaldırıldı; kullanıcıdan `.xlsx`/CSV istenir. Ayraçta satır sonu veya çift tırnak reddedilir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.
- 🟢 **CSV/Excel kod işi tamamlandı:** Önceki ekteki okuyucu ve kolon uyumu eksikleri giderildi; mevcut kaynakta TextFieldParser ve doğrudan Excel hücre dizileri kullanılır.
- **Hata numarası kapsamı:** Çok satırlı CSV'de alan dönüşüm hatası mantıksal kayıt sırasını, CSV yapı hatası fiziksel satır numarasını kullanır.
- 🟡 **Kalan:** Gerçek banka örnekleri, Excel tarih/sayı çeşitleri, belirsiz başlık ayracı ve tam oturum/PostgreSQL runtime kabulü açık. Eşzamanlı yükleme tekilliği ve referanssız kayıt politikası açık; bu okuyucu düzeltmesinde runtime testi yapılmadı.

## Banka borç/alacak göstergelerinde belirsizlik kontrolü — 2026-10-04

- 🟢 **Kod düzeltildi:** Yön kolonu seçiliyse boş veya tanınmayan gösterge artık varsayılan borç sayılmaz; satır hatası oluşturur. Mevcut import koruması bu hatada tüm dosyanın kaydını durdurur.
- 🟢 **Kod düzeltildi:** Özel göstergeler `|` ile ayrılır, kırpılır, boş seçenekler yok sayılır ve büyük/küçük harf duyarsız tam değer eşleşmesiyle kontrol edilir. `Contains` kaldırıldı; örneğin içinde `B` geçen bir metin sırf bu harf nedeniyle borç sayılmaz. Eksiyle başlayan herhangi bir metni alacak kabul eden yol kaldırıldı; tek `-` göstergesi korunur.
- 🟢 **Kod düzeltildi:** Standart ve özel göstergelerin birleşiminde borç/alacak kümeleri kesişiyorsa şablon kaydetme, önizleme ve import öncesinde reddedilir. Standart göstergeler özel değerlerle ters yöne çevrilemez.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.
- 🟡 **Kalan:** Bu turda runtime testi çalıştırılmadı. Yön kolonu seçilmemiş şablonda önceki varsayılan borç davranışı korunur; imzalı tutarlardan yön türetme ve tutarı mutlak değere dönüştürme politikası banka örnekleriyle ayrıca belirlenmeli. Eşzamanlı yükleme, referanssız kayıt ve tam model/PostgreSQL kabulü açık.

## O-3 devamı — maaş snapshot kilitleme ve silme — 2026-10-04

- 🟢 **Kod düzeltildi:** `MaasSnapshotService.KilitleAsync` ve `SilAsync` doğrudan `ExecuteUpdateAsync` yerine firma/dönem/silinmeme koşullarıyla tracked kayıtları yükler ve tek `SaveChangesAsync` kullanır. Kilit ve soft-delete değişiklikleri ortak audit akışına girer; global query filter kaldırılmaz.
- 🟢 **Kod düzeltildi:** Kilitleme yalnız kilitsiz kayıtları değiştirir. Silme yalnız silinmemiş kayıtları değiştirir; silinme ve güncelleme zamanı aynı UTC değeriyle atanır. Tekrarlanan çağrılar mevcut kilitleme/silinme tarihlerini yeniden yazmaz; eşleşmeyen dönem önceki gibi yazım yapmadan döner.
- 🟢 **Kod düzeltildi:** Snapshot güncellemede `AsTracking` açıkça kullanılır. Oluşturma, güncelleme, kilitleme ve silme öncesinde pozitif firma kimliği, geçerli yıl ve 1–12 ay aralığı doğrulanır.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.
- 🟢 **Sonradan tamamlanan:** Bağımsız aktif firma ve personel ilişki kontrolü sonraki ekte eklendi; dört snapshot yazımı tek seçili firma gerektirir.
- 🟡 **Kalan:** Servisin audit rollback/NoTracking runtime kabulü, gerçek oturum yetkileri, eşzamanlı yazımlar, muhasebeleştirilmiş dönem silme politikası ve tam model/PostgreSQL kabulü açık. O-3 bütünüyle kapanmadı.

## Maaş snapshot firma ve personel kapsamı — 2026-10-04

- 🟢 **Kod düzeltildi:** `MaasSnapshotService` zorunlu scoped `IAktifFirmaProvider` alır. Oluşturma, güncelleme, kilitleme ve silme için hedef firma tek seçili aktif firma olmalıdır; firma seçimsizliği, farklı hedef firma ve Tüm firmalar modundaki yazımlar reddedilir. Firma ayrıca mevcut/silinmemiş ve sorgu filtresi kapsamında erişilebilir olmalıdır.
- 🟢 **Kod düzeltildi:** Oluşturma/güncellemede tüm gönderilen personel kimlikleri pozitif ve benzersiz olmalıdır. Personel (`Sofor`) kaydının açık FirmaId eşitliğiyle aynı firmaya ait, silinmemiş ve mevcut query filter kapsamında erişilebilir olduğu doğrulanır. 500 kimliklik gruplarla kontrol edilen listenin tamamı geçmeden snapshot yazımı yapılmaz. İşten ayrılmış fakat silinmemiş personel geçmiş dönem için reddedilmez.
- 🟢 **Kod düzeltildi:** Mevcut dönemde snapshot varsa, güncelleme listesinde dönemin snapshot kayıtlarında bulunmayan personel reddedilir. Oluşturmanın mevcut dönem kontrolü ayrı servis/context çağrıları yerine aynı context üzerinden yapılır; mevcut dönem tekrar oluşturulmaz. Snapshot bulunmayan güncelleme önceki gibi boş liste döndürür.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` temiz.
- 🟢 **Sonradan tamamlanan:** İki maaş ekranının snapshot hataları kalıcı uyarıyla gösterilir; teknik ayrıntı logger'a gider. Tüm firma/filtreli liste yazımı için yönerge eklendi; konsola yazıp sessizce devam eden yol kaldırıldı.
- 🟡 **Kalan:** Gerçek oturum/UI kabulü, eski snapshot ilişki tutarlılığı, DB kısıtları, eşzamanlı yazımlar ve tam model/PostgreSQL açık. Okuma metotlarının mevcut filtre davranışı korunur; yeni kontrollerin runtime testi yapılmadı.

## Maaş ekranlarında dönem kayıt sonucu bildirimi — 2026-10-04

- 🟢 **Kod düzeltildi:** Banka Ödeme Listesi ve Maaş / Ödeme Yönetimi ekranlarında snapshot oluşturmanın beklenmeyen `Task.Run` çağrıları kaldırıldı. Oluşturma liste yükleme akışı içinde `await` edilir; hata yalnız konsola yazılmaz. Liste gösterilmeye devam ederken dönem kaydı oluşturulamadığı kalıcı `role="alert"` uyarısıyla açıklanır. Teknik hata ayrıntıları kullanıcı metnine eklenmez; sunucu logger'ına kaydedilir.
- 🟢 **Kod düzeltildi:** Hedef firma personel listesinin ilk kaydından türetilmez. Tek aktif firma seçimsizliği/Tüm firmalar ve Maaş ekranının tüm firma görünümünde snapshot yazımı denenmez; seçim yönergesi gösterilir. Banka ekranında görev/SGK bordro filtresi varsa eksik liste dönem kaydı olarak yazılmaz; filtre kaldırma yönergesi verilir.
- 🟢 **Kod düzeltildi:** Yükleme başında dönem ve firma değerleri, banka ekranında filtre değerleri yerel değişkenlere alınır. Snapshot varlık kontrolünü beklemeden önce ilgili personel satırları diziye alınır. Yeni yüklemede eski dönem uyarısı temizlenir; mevcut snapshot tekrar oluşturulmaz.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dört dosyada `git diff --check` temiz.
- 🟢 **Sonradan tamamlanan:** Liste yükleme sürümü, çağrıya özel listeler ve seçim güncelliği koruması sonraki ekte eklendi; eski sonuçlar yeni listeyi değiştiremez.
- 🟡 **Kalan:** Hızlı yenileme/firma/dönem değişiminin etkileşimli kabulü yapılmadı. Başlamış servis yazımı, çok kullanıcılı snapshot tekilleştirme, eski eksik dönemlerin onarımı ve PostgreSQL açık.

## Maaş ekranlarında eski yükleme sonuçlarının engellenmesi — 2026-10-04

- 🟢 **Kod düzeltildi:** İki ekranın liste yüklemeleri artan sürüm numarası kullanır. Personel ve hesaplanan satır/özet listeleri çağrıya özel yerel değişkenlerde hazırlanır. Yalnız en son yükleme, aynı dönem/firma/görünüm ve banka filtreleri hâlâ seçiliyken sonucu ekran alanlarına aktarır; eski çağrılar listelere satır ekleyemez veya yeni çağrının yükleme göstergesini kapatamaz.
- 🟢 **Kod düzeltildi:** Snapshot varlık kontrolü sonrasında ve oluşturma çağrısından önce yüklemenin hâlâ güncel olduğu doğrulanır. Eski çağrının hata/uyarısı yeni dönemin bildirimi üzerine yazılmaz. Maaş ekranının firma görünümü yükleme başında alınır ve aynı çağrının sorgu/yazım kararlarında kullanılır.
- 🟢 **Kod düzeltildi:** Banka ekranı aktif firma değişikliğinde eski listeyi temizleyerek yeniden yükler; abonelik Dispose'da kaldırılır. İki ekran Dispose sırasında yükleme sürümünü geçersizleştirir; bekleyen liste sonucunun ekran alanlarına aktarılması engellenir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dört dosyada `git diff --check` temiz.
- 🟢 **Sonradan tamamlanan:** Detay paneli için ayrı sürüm, seçim/dönem/firma kontrolü, yerel sonuçlar ve panel kapatma/Dispose koruması sonraki ekte eklendi.
- 🟡 **Kalan:** Liste/detay için runtime/UI kabulü yapılmadı. Başlamış snapshot yazımı iptal edilmez; DB tekilleştirme, diğer asenkron mali yazımlar, eski eksik dönemler ve PostgreSQL açık.

## Maaş personel detay panelinde eski sonuç koruması — 2026-10-04

- 🟢 **Kod düzeltildi:** `DurumPersonelDetayYukle` ayrı yükleme sürümü ve ana liste sürümü kullanır; personel/sekme/yıl/ay/firma/görünüm değerlerini başlangıçta alır. Finans özeti, avans, borç, harcama, maaş geçmişi ve bordro sonuçları yerel değişkenlerde hazırlanır. Yalnız güncel seçim aynıysa tek aşamada panel alanlarına aktarılır; ara sonuçlar farklı personelin bilgileriyle birleşmez.
- 🟢 **Kod düzeltildi:** Panel kapatma, firma değişikliği, ana liste yenileme ve Dispose eski detay çağrılarını geçersizleştirir. Yeni yükleme eski detay alanlarını temizler. Eski hata bildirimi bastırılır; eski finally bloğu yeni detayın yükleme göstergesini kapatamaz. Güncel hatanın teknik ayrıntısı logger'a gider; kullanıcıya tekrar deneme yönergesi gösterilir.
- 🟢 **Kod düzeltildi:** Personel yalnız mevcut listede bulunuyorsa seçilir. Ana liste yenilendiğinde seçili personel nesnesi yeni listeden alınır; artık listede yoksa panel kapatılır. Bordro araması yükleme başındaki dönem ve personeli kullanır.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen üç dosyada `git diff --check` temiz.
- 🟢 **Detay paneli kod işi tamamlandı:** Önceki ekteki yükleme yarışı eksikliği giderildi; eski çağrıların sonuç yayınlaması engellenir.
- 🟡 **Kalan:** Hızlı personel/sekme/dönem değiştirme runtime/UI kabulü yapılmadı. Başlamış okuma sorgularının iptali, diğer asenkron mali yazımlar/modallar, çok kullanıcılı snapshot tekilleştirme ve tam model/PostgreSQL kabulü açık.

## O-3 devamı — fatura şablonu varsayılan değişiklikleri — 2026-10-04

- 🟢 **Kod düzeltildi:** `FaturaSablonService` ekleme, güncelleme ve varsayılan seçme yollarındaki üç `ExecuteUpdateAsync` kaldırıldı. Diğer varsayılanlar aynı context'te açık tracked sorguyla yüklenir, işaretleri kaldırılır ve yeni/yenilenen şablonla birlikte tek `SaveChangesAsync` ile kaydedilir. Böylece varsayılan kaldırma ortak audit ve transaction akışına girer; kayıt öncesi ayrı SQL yazımı yapılmaz.
- 🟢 **Kod düzeltildi:** Ekleme/güncelleme/silme/varsayılan seçme/kopyalama tek aktif firma gerektirir. Firma seçimsizliği ve Tüm firmalar modundaki yazımlar reddedilir; firma mevcut/silinmemiş ve filtre kapsamında erişilebilir olmalıdır. Mevcut şablon işlemleri açık FirmaId ve silinmeme koşuluyla çalışır. Güncelleme/silme/varsayılan seçimi ve silmede yedek varsayılan sorgusu `AsTracking` kullanır.
- 🟢 **Kod düzeltildi:** Eklemede sıfır olmayan kayıt kimliği reddedilir; gelen Firma navigation grafiği kullanılmaz. Firma kimliği ve oluşturma/silinme metadata alanları sunucudan atanır. Değişiklik zamanları UTC kullanır; varsayılan silindiğinde yalnız aktif/silinmemiş aynı firma şablonu sabit kimlik sırasıyla seçilir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen üç dosyada `git diff --check` boşluk hatası bildirmedi. Git yalnız servis dosyası için LF → CRLF normalizasyon uyarısı verdi.
- 🟡 **Kalan:** Bu turda runtime testi yapılmadı. Varsayılan/audit hata rollback'i, varsayılan NoTracking, gerçek firma oturumu ve PostgreSQL kabulü bekler. DB seviyesinde eşzamanlı tek varsayılan kısıtı, eski çoklu varsayılan kayıtlarının onarımı, PDF/e-posta kapsamının runtime kabulü açık; şablon okuma ve logo/kaşe firma kontrolü sonraki ekte kod olarak tamamlandı. Grup şablonu varsayılan yazımının kod düzeltmesi aşağıdaki ekte tamamlandı; runtime kabulü bekler. O-3 tümüyle kapanmış değildir.

## O-3 devamı — fatura grup şablonu varsayılan yazımları — 2026-10-04

- 🟢 **Kod düzeltildi:** `UnsetVarsayilanAsync` toplu SQL UPDATE yerine aynı context'te tracked kayıtları değiştirir. Oluşturma/güncelleme/varsayılan seçme mevcut ve yeni varsayılanı tek SaveChanges ile audit/transaction akışında kaydeder. Firma + KullaniciId kapsamı korunur; firma geneli (null kullanıcı) ile kullanıcıya özel varsayılanlar birbirini kaldırmaz.
- 🟢 **Kod düzeltildi:** Dört yazım metodu scoped aktif firma sağlayıcısı alır; tek pozitif aktif firma ve mevcut/silinmemiş firma gerekir. Güncelleme/silme/varsayılan seçme açık FirmaId/silinmeme ve AsTracking kullanır. Oluşturmada farklı firma reddedilir; null/0 firma seçili firmaya atanır. Yalnız şablon alanlarıyla yeni entity üretilir; gelen navigation ve silinme metadata alanları kullanılmaz.
- 🟢 **Kod düzeltildi:** Yeni kayıt kimliği sıfır olmalı; ad boş olamaz ve kırpıldıktan sonra en fazla 150 karakter olabilir. Gruplama enum değeri ve isteğe bağlı pozitif kullanıcı kimliği doğrulanır. Silmede UTC silinme/güncelleme zamanı birlikte atanır. Controller oluşturma sonucundaki yeni kimliği kullanır; API sözleşmesi değişmedi.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**. Runtime testi yapılmadı.
- 🟡 **Kalan:** NoTracking/audit rollback, HTTP aktif firma/oturum ve ortak yazım yetkisi kabulü, 400/403/404 API yanıtlarının runtime kabulü, DB seviyesinde eşzamanlı tek varsayılan, eski bozuk kayıtlar ve PostgreSQL kabulü açık. Kullanıcı sahipliği/varlığı, özel şablon okuma kapsamı ve sahiplik reddinin 403 yanıtı sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. O-3 bütünüyle kapanmadı.

## Fatura grup şablonlarında kullanıcı sahipliği — 2026-10-04

- 🟢 **Kod düzeltildi:** Servis kullanıcı kimliğini HTTP principal'dan; HTTP context yoksa Blazor AuthenticationStateProvider'dan alır. HTTP anonimse circuit kimliğine düşmez. Kimlik pozitif ve DB kullanıcısı aktif/silinmemiş olmalıdır; istemciden gelen farklı KullaniciId reddedilir.
- 🟢 **Kod düzeltildi:** Listeleme null kullanıcı parametresinde dahi yalnız oturum kullanıcısının özel şablonları ve firma geneli şablonlarını döndürür. Kimlikle okuma/güncelleme/silme/varsayılan seçme aynı sahiplik ve açık aktif firma koşullarını kullanır. Admin dahil başka kullanıcının özel şablonuna sahiplik muafiyeti eklenmedi. Güncelleme kayıtlı kullanıcı sahipliğini değiştirmez.
- 🟢 **Kod düzeltildi:** Varsayılan okumada null kullanıcı yalnız firma genelini, kullanıcı belirtilmişse yalnız doğrulanmış oturum kullanıcısını seçer. Başka kullanıcı adına özel kayıt oluşturma engellenir. Firma geneli (null kullanıcı) şablonlarının okuma davranışı korunur; ortak yazım yetkisi aşağıdaki devam ekinde sınırlandırıldı.
- 🟢 **Kod düzeltildi:** Controller'a sahiplik reddini sade ProblemDetails ile 403'e dönüştüren filtre eklendi. İstek modelinde ad uzunluğu/zorunluluğu, enum ve pozitif isteğe bağlı kullanıcı kimliği doğrulaması bulunur; ApiController model hatasını 400 olarak döndürür. Beklenmeyen servis/DB hataları bu filtreyle başarılı sonuç gibi gösterilmez.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi. Kaynak dosyalarında yalnız LF → CRLF normalizasyon uyarısı görüldü.
- 🟡 **Kalan:** Runtime testi yapılmadı. Gerçek Bearer/Blazor oturumu, aktif firma sağlayıcısının HTTP isteğinde kurulması, kullanıcı A/B ve pasif kullanıcı kabulü, ortak şablon rol/yazım yetkisinin çalışma zamanı kabulü, 400/403/404 API yanıtlarının runtime kabulü, DB tekilleştirme ve PostgreSQL kabulü açık. Önceki ekteki sahiplik/okuma kod eksikleri giderildi; tüm API kabulü kapanmış değildir.

## Firma geneli fatura grup şablonu yazım yetkisi — 2026-10-04

- 🟢 **Kod düzeltildi:** Firma geneli (KullaniciId null) şablonu oluşturma/güncelleme/silme/varsayılan seçme için `Yetkiler.FaturaHazirlikDuzenle` (`faturahazirlik.duzenle`) kontrolü eklendi. Var olan fatura hazırlık raporu izni kullanılır; yeni izin veya otomatik rol hakkı atanmadı. Kişisel şablonlarda oturum kullanıcısı sahiplik kontrolü korunur.
- 🟢 **Kod düzeltildi:** İzin aynı context üzerinden aktif/silinmemiş kullanıcı ve silinmemiş rol/yetki kayıtlarından okunur. Rol Admin ise mevcut sistem yetki kuralına göre ortak yazım kabul edilir; diğer rolde izin kaydının `Izin` değeri true olmalıdır. Claim'deki rol veya önbellek sonucu ortak yazım izni yerine kullanılmaz. Güncelleme kontrolü gönderilen kullanıcı alanına değil mevcut şablon sahibine dayanır.
- 🟢 **Kod düzeltildi:** Yetki reddi tracked alan değişikliği, varsayılan kaldırma veya SaveChanges öncesindedir. UnauthorizedAccessException mevcut API filtresiyle 403 olur. Ortak şablonların okunması ve kullanıcıların kendi özel şablonları korunur; Admin başka kullanıcının özel şablonuna erişemez. Lisans politikası controller üzerinde ayrıca korunur.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi. Servis dosyasında LF → CRLF normalizasyon uyarısı görüldü.
- 🟡 **Kalan:** Bu turda runtime testi yapılmadı. Yetkisiz/yetkili/Admin kullanıcı, rol/yetki kaldırma, HTTP/circuit firma kurulumu ve PostgreSQL kabulü açık. Kontrolden sonra başka işlemde değişen rol/yetkinin yazım anında tekrar doğrulanması ve DB tekilleştirme ayrı işlerdir. Önceki ekteki ortak yazım yetkisi kod eksikliği giderildi; gerçek API kabulü bekler.

## Fatura grup şablonu API giriş ve hata yanıtları — 2026-10-04

- 🟢 **Kod düzeltildi:** Beklenen şablon doğrulama/firma seçimi hataları `FaturaGrupSablonuException` ile ayrıldı. Boş/geçersiz ad, gruplama değeri, yeni kayıt kimliği, kullanıcı kimliği ve tek firma seçimi sorunları API filtresinde 400 ProblemDetails yanıtına dönüştürülür. Güncellemenin servis sorgusunda artık bulunamayan/erişilemeyen hedefi 404 döner; mevcut sahiplik/yetki reddi 403 olarak kalır.
- 🟢 **Kod düzeltildi:** Route şablon kimliği ve isteğe bağlı query kullanıcı kimliği için pozitif aralık doğrulaması eklendi. Serviste kimlikle okuma/güncelleme/silme/varsayılan seçme de pozitif kayıt kimliği ister. Controller'ın tüm servis çağrıları `HttpContext.RequestAborted` aktarır; güncellemenin ön okuması da aynı iptal sinyalini kullanır.
- 🟢 **Kod düzeltildi:** Filtre yalnız tanımlı kullanıcı hataları ve erişim reddini işler; beklenmeyen EF/altyapı hataları ile iptal istisnaları 400'e çevrilmez. Kullanıcıya altyapı istisnası mesajı açılmaz. Servis arayüzü açıklamaları mevcut özel/ortak okuma ve sahiplik koruma davranışıyla güncellendi.
- 🟢 **Derleme:** Son kaynakla `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj --no-restore -v:minimal` başarılı: **0 uyarı, 0 hata**.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek HTTP'de 400/403/404, seçili firma/oturum, istek iptali ve hata rollback'i kabulü açık. İptal sinyali tamamlanmış bir kaydı geri almaz. Eşzamanlı DB tek varsayılan kısıtı ve PostgreSQL kabulü de bekler; O-3 bütünüyle kapanmadı.

## Fatura şablonu okuma ve logo/kaşe yazım kapsamı — 2026-10-04

- 🟢 **Kod düzeltildi:** Logo yükleme/silme ve kaşe yükleme/silme artık `FindAsync` kullanmaz. Ortak yardımcı tek pozitif seçili firma ve erişilebilir/silinmemiş firma doğrular; şablonu açık FirmaId/silinmeme koşuluyla `AsTracking` yükler. Tüm firmalar modunda yazım reddedilir; başka firma veya silinmiş şablonda işlem false döner. Dört yol aynı context'te SaveChanges/audit akışını kullanır; güncelleme zamanları UTC oldu.
- 🟢 **Kod düzeltildi:** Kimlikle şablon okuma pozitif kayıt kimliği ve tek firma gerektirir; yalnız aynı firmadaki silinmemiş kaydı `AsNoTracking` getirir. Varsayılan okuma aynı firma kapsamındaki aktif/silinmemiş kayıtları kullanır; önce varsayılan, ardından kimlik sırası seçilir. Eski çoklu varsayılanlarda seçim sabittir; bu değişiklik eski kayıtları onarmaz.
- 🟢 **Kod düzeltildi:** Liste sorgusu salt okunur ve açık silinmeme koşulludur; tek firma modunda firma varlığı/kapsamı doğrulanır. Tüm firmalar liste görünümü mevcut query filter kapsamında korunur. Aynı adlı şablonlar kimlik sırasıyla kararlı listelenir. Arayüz açıklamaları yeni tek firma okuma/görsel yazım kuralını belirtir.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek firma seçimi, Tüm firmalar reddi, varsayılan NoTracking altında görsel güncelleme/audit rollback ve PostgreSQL kabulü açık. PDF fatura/şablon eşleşmesi, nesne overload'ı, önizleme ve e-posta firma kapsamı sonraki ekte kod olarak tamamlandı; çalışma zamanı kabulü bekler. DB tek varsayılan kısıtı ve eski kayıt onarımı bekler; O-3 bütünüyle kapanmadı.

## Fatura PDF, önizleme ve e-posta firma kapsamı — 2026-10-04

- 🟢 **Kod düzeltildi:** Gerçek PDF üretimi tek seçili firma gerektirir. Fatura aynı context'te açık FirmaId/silinmeme koşuluyla ve `AsNoTracking` yüklenir. Yüklenen firma/cari silinmiş veya cari/kalem firma ilişkisi uyumsuzsa PDF hazırlanmaz. Şablon yalnız aynı firmada aktif/silinmemiş olabilir. Açık şablon kimliği geçersiz/bulunamayan/pasif ise varsayılana sessizce geçilmez; kimlik verilmezse firma varsayılanı, sonra kimlik sırasıyla aktif şablon, hiç yoksa temel tasarım kullanılır.
- 🟢 **Kod düzeltildi:** Fatura/şablon nesnesi alan overload gelen tutar/cari/firma navigation ve tasarım içeriğini gerçek belgeye taşımaz; yalnız kayıt kimlikleriyle DB'den yeniden yükler. Kaydedilmemiş fatura/şablon reddedilir. Önizleme örnek fatura ve düzenlenen ayarlarla çalışmaya devam eder; tek firma, gelen FirmaId ve kayıtlı şablonun erişilebilir/silinmemiş kapsamı doğrulanır. Firma başlığı DB'den alınır; `FindAsync` kaldırıldı.
- 🟢 **Kod düzeltildi:** Tekli e-postanın konusu/gövdesi ve PDF eki aynı doğrulanmış fatura nesnesinden hazırlanır; farklı context'te ikinci fatura okuması kaldırıldı. Toplu gönderimde firma ve aktif şablon başta sabitlenir, yinelenen fatura kimlikleri tekilleştirilir; yalnız o firmada erişilebilir faturanın cari adresi kullanılır. Pozitif olmayan kimlik listesi gönderim başlamadan reddedilir; erişilemeyen/adresi olmayan faturalar atlanır ve dönüş mevcut gibi en az bir başarıyı bildirir.
- 🟢 **Kod düzeltildi:** PDF hazırlama/render hatalarının teknik mesajları kullanıcı sonucuna eklenmez; ayrıntı logger'a yazılır. Başarısız PDF için e-posta gönderimi denenmez.
- 🟢 **Derleme:** `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj --no-restore -v:minimal` başarılı: **0 uyarı, 0 hata**. Değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi veya gerçek e-posta gönderimi yapılmadı. Gerçek oturum/Tüm firmalar, farklı firma ve pasif şablon reddi, PDF görsel kabulü, önizleme, SMTP ve PostgreSQL kabulü bekler. Eski null/bozuk firma ilişkilerinin onarımı, global filtrelerin gizlediği ilişkilerin DB denetimi, eşzamanlı firma/şablon değişimi ve DB tek varsayılan kısıtı açık. Toplu gönderim kısmi başarılı olabilir; gönderilmiş e-postalar geri alınmaz. O-3 bütünüyle kapanmadı.

## Fiziksel dosya silme hatalarının bildirilmesi — 2026-10-04

- 🟢 **Kod düzeltildi:** Özlük dosyası silmede fiziksel silme hatasını yutan boş catch kaldırıldı. DB commit'i tamamlandıktan sonra silme başarısızsa `FileCleanupPendingException` döner; kayıt kimlikleri ve dosya yolu teknik hatayla birlikte sunucu logunda tutulur. Kullanıcıya fiziksel yol/teknik ayrıntı gösterilmez. Bu metoda ILogger eklendi; DB hedef sorguları `AsTracking` kullanır.
- 🟢 **Kod düzeltildi:** Şoför/personel detay dosya ekranı bu ayrı durumu “Dosya kaydı kaldırıldı; fiziksel dosya temizliği tamamlanamadı” uyarısıyla bildirir ve DB'den durum/dosya listesini yeniden yükler. Fiziksel silme hatasında “Dosya silindi” başarı bildirimi verilmez; DB değişikliği geri alınmış gibi gösterilmez.
- 🟢 **Kod düzeltildi:** `SecureFileService.DeleteAsync` iptal sinyalini başlangıçta kontrol eder. `File.Exists` ön kontrolü kaldırıldı; erişim/IO hatasının false sonucu nedeniyle silmenin sessizce atlanması engellendi. Güvenli yol çözümünden sonra doğrudan `File.Delete` kullanılır; bulunmayan dosya mevcut .NET davranışıyla işlem gerektirmez, hata logger'a yazılıp çağırana iletilir.
- 🟢 **Kod düzeltildi:** S3 silme metodu 404'ü işlem gerektirmeyen durum saymaya devam eder; diğer HTTP/iletişim/iptal hataları loglandıktan sonra çağırana iletilir. İstek ve yanıt dispose edilir. Başarısız silme artık başarılı tamamlanmış Task gibi bildirilmez.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kalan:** Bu turda runtime testi veya gerçek dosya/S3 silmesi yapılmadı. Disk erişim/kilit hatası, DB commit sonrası ekran yenileme, iptal ve S3 404/403/5xx kabulü açık. Kalıcı temizlik kuyruğu/yeniden deneme, mevcut yetim dosyaların taranması, log saklama süresi ve diğer servislerde dosya-DB sıralaması/upload telafisi ayrı işlerdir. Tedarikçi evrak silme sırası ve upload telafisi hata logu sonraki ekte kod olarak düzeltildi; kabulü bekler. Log bildirimi otomatik dosya temizliği değildir; bu dosya yaşam döngüsü işi bütünüyle kapanmadı.

## Tedarikçi evraklarında DB ve dosya silme sırası — 2026-10-04

- 🟢 **Kod düzeltildi:** Evrakın tüm dosyalarıyla silinmesi ve tek dosya silinmesi fiziksel silmeden önce DB değişikliğini kaydeder. Hedefler `AsTracking` yüklenir; UTC IsDeleted/DeletedAt/UpdatedAt alanları atanır. Evrak ve bağlı dosyaların değişiklikleri tek SaveChanges ile audit akışına girer; SaveChanges başarısızsa bu silme yolları fiziksel temizliğe başlamaz.
- 🟢 **Kod düzeltildi:** DB silme kaydı tamamlandıktan sonra ortak temizlik yardımcısı tüm dosyaları dener. Bir dosyanın hatası sonraki dosyaları durdurmaz. Her başarısız yol evrak/dosya kimliğiyle sunucu loguna yazılır; başarısızlıklar `FileCleanupPendingException` içinde toplanır. Soft delete edilen dosyaların yolları DB kaydında korunur.
- 🟢 **Kod düzeltildi:** Tedarikçi evrak ekranındaki tek dosya silme bu özel durumu uyarıyla gösterir ve evrak listesini/seçili dosya panelini yeniden yükler; fiziksel silme başarısızken başarı bildirimi vermez. Yükleme telafisindeki boş catch de log bildirimiyle değiştirildi; temizlenemeyen dosya yolu kaydedilir ve asıl yükleme istisnası korunur.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kalan:** Bu turda runtime testi veya gerçek dosya silmesi yapılmadı. Gerçek DB/audit rollback, varsayılan NoTracking, çok dosyalı kısmi temizlik ve ekran kabulü açık. Kalıcı temizlik kuyruğu/yeniden deneme, eski yetim dosyalar, diğer servisler, belirsiz upload commit sonucunda telafi güvenliği ve tam firma/yetki kabulü bekler. DB commit'i ile fiziksel silme tek atomik işlem değildir; tüm dosya yaşam döngüsü işi kapanmadı.

## Araç evraklarında DB ve dosya silme sırası — 2026-10-04

- 🟢 **Kod düzeltildi:** Araç belgesinin bağlı dosyalarıyla silinmesi ve tek dosya silinmesi önce DB/audit değişikliğini kaydeder. Hedefler `AsTracking` yüklenir; IsDeleted ve UTC DeletedAt/UpdatedAt alanları atanır. Belge ve bağlı dosyalar tek SaveChanges ile kaldırılır; ilk DB kaydı başarısızsa fiziksel silme başlamaz. Soft delete edilen dosyanın yolu kayıtta korunur.
- 🟢 **Kod düzeltildi:** Ortak temizlik yardımcısı her dosyayı ayrı dener; hata sonraki dosyaları durdurmaz. Başarısız yollar evrak/dosya kimliğiyle logger'a yazılır ve `FileCleanupPendingException` ile toplanarak bildirilir. Yükleme telafisinde temizlenemeyen dosya da loglanır; asıl yükleme hatası korunur.
- 🟢 **Kod düzeltildi:** Araç formunun belge/tek dosya silme ve araç evrak ekranının tek dosya silme yolları bu özel durumu uyarıyla gösterip DB listesini yeniler. Tek dosya paneli mevcut evrak listesinden güncellenir; fiziksel temizlik hatasında başarı bildirimi verilmez.
- 🟢 **Kod düzeltildi:** Belge tarihlerini senkronize eden araç hedefi `AsTracking` kullanır. Aktif plaka ve satış açma/kapatma yollarındaki aynı araç hedef sorguları da açık tracking kullanır; varsayılan NoTracking altında bu hedeflerin alan değişiklikleri kaydedilebilir. Bu değişiklik plaka/satış yollarının tüm kabulünü kapatmaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kalan:** Bu turda runtime testi veya gerçek dosya silmesi yapılmadı. DB/audit rollback, varsayılan NoTracking, disk kilidi/izin hatası, çok dosyalı temizlik ve üç ekran akışının kabulü açık. Belge değişikliği ile araç tarih senkronizasyonunun tek SaveChanges kullanması sonraki ekte kod olarak tamamlandı; rollback ve eşzamanlı yazım kabulü bekler. Yeniden deneme, kalıcı temizlik kuyruğu, eski yetim dosyalar, upload belirsiz commit telafisi, diğer servisler ve tam firma/yetki kabulü bekler.

## Araç belge değişikliği ve tarih senkronizasyonunun ortak kaydı — 2026-10-04

- 🟢 **Kod düzeltildi:** Araç evrak ekleme/güncelleme/silme önce belge değişikliğini ve araç üzerindeki muayene/trafik/kasko/koltuk tarihlerini aynı context'te hazırlar; ardından tek SaveChanges kullanır. Senkronizasyon yardımcısının ikinci SaveChanges çağrısı kaldırıldı. Böylece belge ve araç tarih değişiklikleri ortak DB/audit kayıt akışına girer; belge silmede fiziksel temizlik bu kaydın ardından başlar.
- 🟢 **Kod düzeltildi:** Tarih hesabı tüm erişilebilir/silinmemiş araç belgelerini tracking ile yükler, henüz kaydedilmemiş Added belgeleri referans kimliğiyle dahil eder. Pasif/tarihsiz/silinmiş filtreleri bellek üzerinde uygulanır; böylece güncellenen kategori/tarih, pasife alma/aktifleşme ve bekleyen soft delete sonucu doğru hesaba katılır. Silme durumundaki tracked kayıtlar hariç tutulur. Geçerli/erişilebilir araç yoksa kayıttan önce işlem reddedilir.
- 🟢 **Kod düzeltildi:** Evrak güncelleme hedefi açık AsTracking kullanır; varsayılan NoTracking altında alan değişiklikleri korunur. Eklemede henüz DB kimliği olmayan belgeler mevcut kayıtlara kimlik sıfır olduğu için karışmaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek DB/audit rollback, belge kategori/tarih/pasiflik değişimleri, son belge silindiğinde tarih temizlenmesi ve varsayılan NoTracking kabulü açık. Bu tek kayıt düzenlemesi eşzamanlı iki context'in tarih hesabını tekilleştirmez; PostgreSQL/SQL Server, tam firma/yetki kabulü, eski tutarsız veriler ve kalıcı dosya temizliği/yeniden deneme bekler. Dosya silme DB commit'inin dışındadır.

## Araç plaka geçmişi ve aktif plakanın ortak kaydı — 2026-10-04

- 🟢 **Kod düzeltildi:** Plaka nesnesiyle ekleme, silme ve tarihli kapatma yollarındaki kayıt öncesi/sonrası iki SaveChanges kaldırıldı; plaka geçmişi ve araç AktifPlaka tek SaveChanges ile audit/transaction akışına girer. PlakaEkle ve PlakaCikis de aynı hesap yardımcısını kullanır. Yazım hedefleri açık AsTracking yüklenir; yardımcı kendi başına kayıt yapmaz.
- 🟢 **Kod düzeltildi:** Aktif plaka hesabı tracked geçmiş ve henüz kaydedilmemiş Added kayıtları referans kimliğiyle birleştirir. Silinmiş/deleted kayıtları çıkarır; çıkış tarihi filtresini bellek üzerinde uygular. Giriş tarihi, eklenen kayıt önceliği ve kimlik ile seçim sabittir. PlakaCikis'in o işlemde kapattığı kayıt ayrıca hariç tutulur; başka uygun aktif geçmiş varsa seçilir, yoksa araç plakası temizlenir.
- 🟢 **Kod düzeltildi:** PlakaEkle geçerli/erişilebilir araç ve boş olmayan plaka ister; plaka kırpılıp büyük harfe çevrilir. Nesneyle eklemede sıfır olmayan yeni kayıt kimliği reddedilir; gönderilen kimlikle mükerrer kontrolünü atlama yolu kaldırıldı. Silmede UTC DeletedAt/UpdatedAt birlikte atanır. Aktif araç plaka alanı değiştiğinde UpdatedAt güncellenir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Ekleme/silme/kapatma/çıkış, NoTracking ve ortak audit rollback kabulü açık. Gün bazlı aktiflik ile saat içeren çıkış tarihlerinin semantiği, ileri giriş tarihleri, UTC/yerel gün farkı, eski çoklu aktif kayıtlar, cari/firma doğrulamasının runtime kabulü ve cache runtime kabulü ayrıca ele alınmalı; AracService yazım sonrası temizliği sonraki ekte kod olarak tamamlandı. Beş özel plaka yazımının firma/cari kontrolü ve iki eklemenin aynı context mükerrer sorgusu sonraki ekte kod olarak tamamlandı; eşzamanlı yazım ve DB tekillik kısıtı tamamlanmadı. Tam oturum ve PostgreSQL/SQL Server kabulü bekler.

## Plaka yazımlarında seçili firma ve cari doğrulaması — 2026-10-04

- 🟢 **Kod düzeltildi:** PlakaEkle, AddPlakaToAracAsync, DeletePlakaFromAracAsync, ClosePlakaAsync ve PlakaCikis tek pozitif seçili firma gerektirir. Tüm firmalar/seçimsizlik reddedilir; firma mevcut/silinmemiş ve query filter kapsamında erişilebilir olmalıdır. Firma kimliği işlem başında alınır; araç hedefleri açık FirmaId, plaka hedefleri aynı firmadaki silinmemiş araç ilişkisiyle sınırlandırılır. Query filter devre dışı bırakılmaz.
- 🟢 **Kod düzeltildi:** Yeni plaka ekleme ve çıkış işlemlerinde isteğe bağlı cari kimliği pozitif, aynı firmaya ait ve silinmemiş/erişilebilir olmalıdır. Çıkışta yeni cari verilmezse mevcut cari ilişkisi de doğrulanır; geçersiz ilişkiyle yeni işlem alanları kaydedilmez. Null cari desteklenir.
- 🟢 **Kod düzeltildi:** İki plaka ekleme yolunun mükerrer kontrolü ayrı servis/context çağrısı yerine aynı context ve yakalanmış firma kimliğiyle çalışır. Mevcut firmaya ait silinmemiş araçların aktif plaka kayıtları kontrol edilir. Diğer genel plaka okuma/araç oluşturma yollarının politikası bu özel yazım kontrolünün kapsamı değildir.
- 🟢 **Kod düzeltildi:** Genel araç güncellemesinde aktif plaka yardımcısının çağrısı araç SaveChanges öncesine taşındı; hesaplanan plaka alanı aynı araç kaydında saklanır. Bu çağrının mevcut query filter kapsamı korunur; beş özel plaka yazımı açık firma parametresi verir.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek HTTP/circuit firma seçimi, farklı firma araç/cari reddi, silinmiş kayıtlar, null cari ve rollback kabulü açık. Eski plaka/cari ilişkilerinin onarımı, genel araç firma değiştirme politikası, tarih semantiği, cache tazeliğinin runtime kabulü, eşzamanlı DB tekillik ve PostgreSQL/SQL Server kabulü bekler. Aynı context'teki mükerrer sorgu DB seviyesinde tekillik garantisi değildir.

## Araç yazımlarından sonra önbellek temizliği — 2026-10-04

- 🟢 **Kod düzeltildi:** AracService içindeki başarılı araç/plaka/satış/belge/dosya DB yazımları sonrası ortak araç önbelleği temizleme çağrısı eklendi. Araç oluşturma ve firmasız araç backfill'inin mevcut temizliği de aynı yardımcıyı kullanır. `CacheKeys.AracPrefix` tüm firma ve Tüm firmalar araç liste/aktif anahtarlarını kapsar; değişiklik yalnız o anda seçili ekran anahtarıyla sınırlanmaz.
- 🟢 **Kod düzeltildi:** Temizlik SaveChanges sonrasında, açık transaction kullanılan araç oluşturma yolunda commit sonrasında çalışır. Belge/dosya silme yollarında DB kaydı sonrası önbellek temizliği fiziksel silmeden öncedir; disk hatası DB'deki kaldırma durumunun önbellek temizliğini atlatmaz. Helper hatası logger'a yazılır; tamamlanan DB kaydı cache hatası yüzünden başarısız yazım gibi gösterilmez.
- 🟢 **Kod düzeltildi:** Excel araç aktarımı tamamlanan satır commit'lerini işaretler ve işlem sonunda bir kez temizlik yapar. Diğer satırlar başarısız veya dış okuma hatası oluşmuş olsa da önceki başarılı satırlar varsa temizlik denenir. Hiç commit yoksa temizlik çağrılmaz; satır başına önbellek silme yükü eklenmedi.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek liste/aktif görünüm, firma değişimi, kısmi Excel aktarımı ve disk hatası sonrası liste kabulü bekler. CacheService backend hatasını loglayıp yutabilir; bu çağrılar başarısız depoda tazelik garantisi vermez. Aynı süreçte temizlik sonrası eski factory yayınlama koruması sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Süreçler arası Redis anahtar takibi/sürümü, yeniden deneme ve başka servislerden araç yazımları açık.

## Önbellek temizliği sonrası eski sorgu sonucunun yayınlanması — 2026-10-05

- 🟢 **Kod düzeltildi:** CacheService aynı süreçte tüm scope'lar arasında ortak temizlik sürümü kullanır. GetOrSetAsync sürümü cache okumasından önce alır; temizlik araya girerse cache hit kullanılmaz. Factory sonucu yayınlanırken sürüm tekrar kontrol edilir; arada RemoveAsync/RemoveByPrefixAsync çalışmışsa eski sonuç cache'e yazılmaz. Tracker'da henüz bulunmayan ilk anahtar hesaplaması da sürüm değişiminden etkilenir.
- 🟢 **Kod düzeltildi:** Cache set ve temizlik işlemleri ortak SemaphoreSlim ile sıralanır. Sürüm kontrolü, backend set ve anahtar takibi aynı kilitte tamamlanır; kontrol ile set arasına temizlik giremez. Factory ve serileştirme kilit dışında çalışır. Doğrudan SetAsync/sliding set de aynı yayınlama kilidini kullanır; GetOrSet factory sürüm koruması ayrı olarak uygulanır.
- 🟢 **Kod düzeltildi:** Prefix temizliğinde bir anahtarın backend hatası sonraki anahtarların temizliğini durdurmaz. Başarısız anahtar tracker'da tutulur ve logger'a yazılır; yeniden temizlikte tekrar denenebilir. Set/remove kilitleri finally ile bırakılır. Bu yollar ve GetOrSet iptal edilen çağrıyı başarılı yayınlama gibi tamamlamaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Factory/temizlik/set yarışının gerçek backend ve farklı scope kabulü, iptal ve cache kesintisi kabulü açık. Başlamış factory sonucu kendi çağıranına dönebilir; bu değişiklik ekran yükleme sürüm kontrolünün yerine geçmez. Sürüm süreç geneline aittir; ilgisiz anahtar temizliği de devam eden factory'nin cache yayınını atlatabilir. Backend yazımları ortak kilitte sıralandığından yük/Redis gecikmesi kabulü gerekir. Çok süreçli Redis sürümü/anahtar takibi, mevcut stale backend verisi ve otomatik yeniden deneme açık.

## Araç liste önbelleğinde firma seçiminin sabitlenmesi — 2026-10-05

- 🟢 **Kod düzeltildi:** GetAllAsync/GetActiveAsync ortak yükleme yardımcısını kullanır. Firma kimliği ve Tüm firmalar modu çağrı denemesi başında yerel değerlere alınır; cache anahtarı bu değerlerden üretilir. Tek firma sorgusunda açık FirmaId koşulu aynı yakalanmış kimliği kullanır; EF global filtreleri korunur. Firma seçilmemişse boş liste döner, F0 cache kaydı oluşturulmaz.
- 🟢 **Kod düzeltildi:** Yükleme boyunca geçici firma değişim aboneliği seçim sürümünü artırır. Context oluşturma öncesi/sonrası, DB sorgusu sonrası, factory sonucu öncesi ve cache hit dönüşünde seçim kontrol edilir. Değişen seçimde factory tamamlanmaz ve sonuç cache'e yazılmaz; çağrı en fazla üç deneme ile yeni seçimi yükler. Aynı firmaya dönülmüş olsa da olay sürümü aradaki değişimi yakalar. Abonelik finally ile kaldırılır.
- 🟢 **Kod düzeltildi:** Anahtarlar araç öneki altında Scope2 sürümünü kullanır; önceki kapsam yarışıyla doldurulmuş eski anahtarlar bu okumalarda kullanılmaz. Tüm firmalar görünümü mevcut query filter politikasını kullanır. Aktif plaka ve araç liste sıralamasına kimlik eşitlik sırası eklendi; iki okuma aynı hesap akışını paylaşır.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime testi yapılmadı. Gerçek circuit/firma/Tüm firmalar geçişleri, cache hit/miss sırasında A→B→A değişimi ve farklı scope kabulü bekler. Üç denemede seçim sabitlenmezse yeniden yükleme yönergesi içeren hata döner; çağıran ekranların hata ve yükleme sürümü yönetimi ayrıca kabul edilmeli. Ana araç listesinde servis sonrası seçim/sürüm ve hata bildirimi sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Diğer ekran sonuç yayınları, provider'ın olay dışı doğrudan mutasyonu, çok süreçli Redis, backend arızası ve diğer cache kullanan servisler açık.

## Araç listesinde eski yükleme sonuçları ve hata bildirimi — 2026-10-05

- 🟢 **Kod düzeltildi:** Araç listesi her yüklemede artan sürüm ve başlangıç firma/Tüm firmalar değerlerini kullanır. Araçlar, firmasız araç sayısı ve firma seçenekleri çağrıya özel yerel alanlarda hazırlanır; yalnız güncel yükleme sonucu ekran alanlarına aktarır. Kontrol her bekleme sonrasında yapılır; eski çağrı yeni listenin yükleme göstergesini kapatamaz.
- 🟢 **Kod düzeltildi:** Firma değişimi eski çağrıyı olay anında geçersizleştirir; UI dispatcher üzerinden firma filtresi, eski liste ve silme/plaka geçmişi seçimleri temizlenerek yeniden yükleme yapılır. Başlayan yükleme eski veriyi temizler ve yükleme durumunu render eder. Dispose sürümü geçersizleştirir ve firma olay aboneliğini kaldırır; bekleyen liste sonucu kapatılmış ekrana aktarılmaz.
- 🟢 **Kod düzeltildi:** Ana liste hatası try/catch/finally ile ele alınır; teknik ayrıntı logger'a gider, güncel kullanıcı hatası kalıcı role=alert uyarısı ve Yeniden Dene düğmesiyle bildirilir. Yardımcı firma seçenekleri/firmasız sayısı hataları da logger ve uyarıyla açıklanır; araçlar yüklenmişse liste gösterilmeye devam eder. Teknik istisna mesajı kullanıcı uyarısına eklenmez.
- 🟢 **Derleme:** Son kaynakla Web Debug derlemesi **0 uyarı, 0 hata**; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime/UI testi yapılmadı. Hızlı firma/dönem/yenileme geçişleri, cache hit/miss, ana/yardımcı sorgu arızası, yeniden deneme ve Dispose kabulü bekler. Başlamış sorgular iptal edilmez. Plaka geçmişi modalının ekleme/silme/kapatma sonuç koruması sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Araç silme sonuç koruması sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Import sonuç koruması son ekte kod olarak tamamlandı; runtime kabulü ve import servisinin firma kapsamı denetimi bekler. Backfill yetki/firma kabulü ayrı iştir. Bu değişiklik ana liste yükleme akışını kapsar.

## Araç plaka geçmişi modalında bekleyen yazım sonuçları — 2026-10-05

- 🟢 **Kod düzeltildi:** Plaka ekleme/silme/kapatma ortak işlem yardımcısını kullanır. Araç kimliği, modal sürümü ve firma/Tüm firmalar seçimi işlem başında yakalanır; servis ve liste yenilemesi sonrasında güncellik kontrol edilir. Modal kapatma, yeniden açma, firma olayı ve Dispose eski sonucu geçersizleştirir. Eski çağrı yeni modalın verisini, formunu veya işlem göstergesini değiştiremez.
- 🟢 **Kod düzeltildi:** Yeni plaka isteği form nesnesinden ayrı hazırlanır. Silme/kapatma hedefinin seçili aracın geçmişinde olması kontrol edilir; tek seçili firma ve araç firma eşleşmesi gerekir. Kayıt sürerken form ve satır işlemleri kilitlenir; modal kapatılabilir. Kapatma öncesinde ekrandaki plaka nesnesinin çıkış tarihi değiştirilmez.
- 🟢 **Kod düzeltildi:** Yenileme sonrası araç yakalanmış kimlikle bulunur; yenileme başka yüklemeyle geçersizleşmişse veya araç bulunamazsa modal kapanır. Silme dahil üç yolun hatası logger'a yazılır; yalnız güncel modal genel hata bildirimi gösterir, teknik istisna ayrıntısı kullanıcıya aktarılmaz.
- 🟢 **Derleme:** Web Debug derlemesi **0 uyarı, 0 hata** ile tamamlandı. Değişen üç dosyanın `git diff --check` kontrolü boşluk hatası bildirmedi.
- 🟡 **Kabul bekliyor:** Bu turda runtime/UI testi yapılmadı. Bekleyen ekleme/silme/kapatma sırasında modal kapatma/başka araç açma, A→B→A firma geçişi, servis/liste arızası, çift tıklama ve Dispose kabulü açık. Başlamış servis yazımı iptal edilmez veya geri alınmaz; modal kapansa da DB kaydı tamamlanabilir. Koruma bu modalın sonuç yayınına aittir; diğer ekranların yazımları, araç import/silme, backfill yetkisi ve eşzamanlı DB tekillik ayrıca bekler.

## Personel Banka Ödeme Listesi PDF/yazdırma düzeni — 2026-10-05

- 🟢 **Kod düzeltildi:** PDF düğmesinin kullandığı tarayıcı yazdırmasına bu sayfaya özel, yalnız print ortamında yüklenen stil eklendi. A4 yatay ve 8 mm kenar boşluğu tanımlandı; menü, üst çubuk, filtre/işlem düğmeleri, modal ve toast çıktıda gizlenir. Ana yerleşimin flex/genişlik/padding ve tablo kapsayıcısının taşma kısıtları yazdırmada kaldırılır.
- 🟢 **Kod düzeltildi:** On beş sütun sabit yüzde genişlikleriyle sayfaya dağıtılır; uzun metinler satıra sarılır. Başlık satırı sonraki sayfalarda tekrarlanır, satırlar sayfa sınırında bölünmez; genel toplam tekrar eden footer yerine listenin sonunda basılır. Görev dağılımı ve ödeme özeti raporda korunur.
- 🟢 **Kod düzeltildi:** Basılı başlık seçili ay/yıl, görev ve bordro filtresini gösterir. Düğme PDF / Yazdır olarak adlandırıldı; yükleme veya boş liste sırasında çalışmaz. Yazdırma hatası teknik ayrıntısı logger'a, genel bildirim kullanıcıya gider. Ödeme hesapları ve liste verisi değiştirilmedi.
- 🟢 **Derleme:** Ayrı geçici çıktı klasörüne `UseAppHost=false` ile Web Debug derlemesi **0 uyarı, 0 hata** tamamlandı; `git diff --check` boşluk hatası bildirmedi. İlk normal derleme çalışan Web EXE kilidi nedeniyle MSB3027/MSB3021 ile durdu. Çalışan uygulama durdurulmadı; yeni Razor çıktısının kullanılması için uygulama yeniden derlenip başlatılmalı.
- 🟡 **Kabul bekliyor:** Bu turda tarayıcı yazdırma önizlemesi veya gerçek PDF çıktısı alınmadı. Çok sayfalı liste, uzun ad/adres, büyük tutarlar, SGK ayrı/birleşik görünüm ve Chrome/Firefox kabulü bekler. PDF kaydı tarayıcının hedef seçiminden yapılır; yönlendirme kullanıcı tarafından elle değiştirilebilir.

## İhale raporlarında profesyonel Excel ve PDF çıktısı — 2026-10-05

- 🟢 **Kod düzeltildi:** Enflasyonlu projeksiyon kartına Excel ve PDF düğmeleri eklendi. Çıktılar ekranda yüklenmiş aynı özet verisini kullanır; seçili proje ile rapor kimliği eşleşmeden aktarım yapılmaz. Proje adı/kodu, müşteri, sözleşme süresi, hat sayısı ve oluşturma zamanı raporda yer alır.
- 🟢 **Kod düzeltildi:** Projeksiyon çıktısı Proje Özeti, Aylık Projeksiyon ve Hat Detayları bölümlerini içerir. İlk/son ay maliyeti, süre sonu artış tutarı/oranı ve proje toplamları korunur. Kümülatif toplamlar aylık satırlardan tekrar toplanmaz; son ayın kümülatif değerleri kullanılır. Hesaplama servisi değiştirilmedi.
- 🟢 **Kod düzeltildi:** Ortak Excel sunumu ayrı çalışma sayfaları, başlık/rapor bilgileri, şeritli filtreli tablolar, sabit üst satırlar, metin sarma, sınırlı sütun genişlikleri, sayısal hücreler ve iki ondalıklı negatif sayı formatı kullanır. A4 yatay, bir sayfa genişliği ve tekrarlanan başlık satırları yazdırma ayarına kaydedilir. Metin hücreleri formül olarak atanmaz.
- 🟢 **Kod düzeltildi:** PDF gerçek PDF dosyası olarak QuestPDF ile oluşturulup indirilir. Bölüm başlıkları, tekrarlanan tablo başlıkları, dönüşümlü satır rengi, vurgulu toplamlar, Türkçe sayı biçimi ve sayfa numaraları kullanılır. Normal tablolar A4 yatay, dokuzdan fazla sütunlu gerçekleşen analiz A3 yatay hazırlanır; uygulama menüsü PDF'ye girmez.
- 🟢 **Kod düzeltildi:** Gerçekleşen/Tekliflenen Analiz ve İhale Sonrası Operasyon Özeti de Excel/PDF düğmeleriyle aynı sunumu kullanır. Operasyon çıktısında göstergeler ve riskli projeler ayrı bölümlere ayrılır. Aktarım sırasında düğmeler kilitlenir; teknik hata logger'a, genel hata kullanıcıya gider. Mevcut teklif versiyonu export servisinin onay/izin kontrolleri korunur; bu ek ekran analizlerini kapsar.
- 🟢 **Derleme:** Son kaynak ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi. `git diff --check` boşluk hatası bildirmedi. Çalışan Web uygulaması durdurulmadı; yeni ekranı kullanmak için normal çıktı yeniden derlenip uygulama başlatılmalı.
- 🟡 **Kabul bekliyor:** Bu turda gerçek XLSX/PDF açma veya görsel çıktı kabulü yapılmadı. Uzun proje/hat adları, boş risk listesi, çok sayfalı projeksiyon, büyük/negatif tutarlar, Excel baskısı ve ekran/çıktı değerlerinin kullanıcı kabulü bekler. Çıktı anında DB yeniden sorgulanmaz; rapor ekranda yüklü veriyi sunar. Müşteriyle paylaşmadan önce raporun güncelliği kontrol edilmelidir.

## Araç silmede firma kapsamı, tracking ve bekleyen sonuçlar — 2026-10-05

- 🟢 **Kod düzeltildi:** AracService.DeleteAsync pozitif araç kimliği ve tek seçili firma ister; firma kimliği context oluşturulmadan önce yakalanır. Firma ve araç global filtreler içinde erişilebilir/silinmemiş olmalıdır; araç açık FirmaId koşuluyla yüklenir. Tüm firmalar/seçimsizlik ve başka firmaya ait araç reddedilir. Bulunmayan hedef sessiz başarı gibi tamamlanmaz.
- 🟢 **Kod düzeltildi:** Araç ve dahil edilen plaka geçmişi AsTracking yüklenir. Araç soft delete, UTC DeletedAt/UpdatedAt, aktif plaka temizliği ve geçmişte açık plaka çıkışı ortak SaveChanges akışında kaydedilir. Başarılı kayıt sonrası mevcut araç önbelleği temizliği korunur. Plaka tarih semantiği bu ekte değiştirilmedi.
- 🟢 **Kod düzeltildi:** Araç listesinin silme onayı kendi modal sürümünü kullanır. Kimlik ve firma seçimi işlem başında yakalanır; servis/liste yenilemesi sonrası güncellik kontrol edilir. Modal kapatma/başka araç açma, firma olayı ve Dispose eski sonucu geçersizleştirir; eski çağrı yeni modalı kapatamaz veya yükleme göstergesini değiştiremez. Aynı açık modalda ikinci silme engellenir.
- 🟢 **Kod düzeltildi:** Silme hatası try/catch/finally ile ele alınır; teknik ayrıntı logger'a, genel bildirim yalnız güncel modalın kullanıcısına gider. Başarılı kayıt sonrası liste yenilenir; yenileme hatası ana listenin mevcut uyarı/yeniden deneme akışında kalır.
- 🟢 **Derleme:** Son kaynak ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul bekliyor:** Bu turda runtime veya gerçek araç silme yapılmadı. NoTracking varsayılanı, DB/audit rollback, farklı firma/Tüm firmalar reddi, bulunmayan araç, çift tıklama, modal/firma geçişi ve Dispose kabulü açık. Başlamış silme iptal edilmez; modal kapansa da DB yazımı tamamlanabilir. İlişkili operasyonların araç soft delete politikasının kabulü, HTTP/yetki kabulü, Excel aktarım sonuç koruması ve backfill yetkisi ayrı işlerdir.

## Araç Excel aktarımında modal ve seçim sonuç koruması — 2026-10-05

- 🟢 **Kod düzeltildi:** Dosya nesnesi, modal sürümü ve firma/Tüm firmalar seçimi işlem başında yakalanır. Dosya okuması ve servis çağrısı sonrasında güncellik kontrol edilir; dosya okunurken seçim değişmişse servis yazımı başlatılmaz. Modal açma/kapatma, yeni dosya, firma olayı ve Dispose eski sonucu geçersizleştirir.
- 🟢 **Kod düzeltildi:** Ekran genelinde aktarım kilidi modal kapansa da işlem bitene kadar korunur; çift başlatma ve bekleyen işlem sırasında dosya değiştirme engellenir. Açılan modal işlem sürdüğünü ve kapatmanın kayıtları geri almadığını açıklar. Yalnız kilidi tutan aktarım finally içinde kilidi bırakır; Dispose sonrası render yapılmaz.
- 🟢 **Kod düzeltildi:** Sonuç yerel değişkende tutulur ve yalnız güncel modalda yayınlanır. Hatalı/kısmi sonuç başarı bildirimi yerine uyarı verir; eklenmiş/güncellenmiş satır varsa Success=false olsa da liste yenilenir. UI istisnası logger'a, genel hata güncel modala gider.
- 🟢 **Derleme:** Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** tamamlandı; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul bekliyor:** Runtime/UI testi veya gerçek Excel aktarımı yapılmadı. Dosya okurken/modal veya firma A→B→A geçişi, iki tıklama, kısmi commit, okuma hatası, modalı yeniden açma ve Dispose kabulü açık. Başlamış servis yazımı iptal edilmez; DB kaydı sürebilir. Import servisinin firma kimliğini işlem boyunca sabitlemesi, sorgular/yeni kayıtlar için açık firma koşulu ve servis hata ayrıntılarının kullanıcı sunumu ayrıca denetlenmeli; bu ek UI sonucu kapsamındadır.

## A-13 devamı — araç import servisinde sabit firma ve tracked yazım — 2026-10-05

- 🟢 **Kod düzeltildi:** ImportFromExcelAsync boş/10 MB üzeri veriyi ve tek pozitif firma seçimi olmayan çağrıyı reddeder. Firma kimliği context oluşturulmadan önce yakalanır; firma erişilebilir/silinmemiş olmalıdır. Şase, güncelleme ve aktif plaka sorguları açık firma koşuluyla çalışır; yeni araç FirmaId alanı yakalanmış kimlikle atanır. Global filtreler kaldırılmaz.
- 🟢 **Kod düzeltildi:** İşlem boyunca geçici firma olayı aboneliği seçim sürümünü artırır; A→B→A değişimi de yakalanır. Satır/transaction başında, kayıt öncesi ve SaveChanges sonrası commit öncesinde seçim doğrulanır. Değişim fark edilince mevcut transaction commit edilmez, kalan satırlar durur; daha önce commit edilmiş satırlar geri alınmaz. Abonelik finally ile kaldırılır.
- 🟢 **Kod düzeltildi:** Güncelleme hedefi ve plaka geçmişi AsTracking yüklenir; hedef kaybolmuş/gizlenmişse başarılı güncelleme sayılmaz. ExecutionStrategy her denemede tracker'ı temizleyip hedefi tekrar yükler. Satır hata ayrıntısı logger'a gider; kullanıcıya satır kimliği ve genel hata/firma değişimi yönergesi verilir. Satır hatası varsa Success=false döner; önceki kısmi kayıtları UI yenilemesi kapsar.
- 🟢 **Derleme:** Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** tamamlandı; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul bekliyor:** Gerçek Excel/DB/circuit testi yapılmadı. Firma değişimi ile SaveChanges/commit'in aynı anda gerçekleşmesi, audit tenant bilgisi, NoTracking, retry/commit belirsizliği, eski şase/plaka ilişkileri ve eşzamanlı DB tekillik kabulü açık. Kontroller başlamış commit'i iptal veya geri alma garantisi değildir; sağlayıcı/global filtre/audit bütünlüğünün gerçek kabulü gerekir. Backfill yetkisi ve genel araç firma değiştirme denetimi A-13 altında açık kalır.

## A-13 devamı — firmasız araç atamasında Admin ve hedef firma kontrolü — 2026-10-05

- 🟢 **Kod düzeltildi:** BackfillFirmaIdAsync HTTP varsa HTTP kimliğini, circuit çağrısında AuthenticationStateProvider kimliğini kullanır. Pozitif kullanıcı kimliği ve DB'de aktif/silinmemiş kullanıcı, silinmemiş Admin rolü gerekir; yalnız claim'deki rol yeterli değildir. Yetki kayıt öncesinde tekrar doğrulanır. Diğer kullanıcılar firmasız araç sayısını da alamaz; sıfır döndüğü için bakım düğmesi gösterilmez.
- 🟢 **Kod düzeltildi:** Hedef kimlik tek seçili firma ile aynı olmalıdır; hedef firma global filtre kapsamında mevcut/silinmemiş olmalıdır. Filtre atlama yalnız yetkili bakım yolunda, FirmaId=null ve silinmemiş araç sorgusuna uygulanır. Araçlar AsTracking yüklenir; firma/UpdatedAt ortak SaveChanges/audit akışında kaydedilir ve ardından cache temizlenir.
- 🟢 **Kod düzeltildi:** Geçici firma olayı aboneliği A→B→A geçişini de geçersizleştirir; seçim kayıt öncesi doğrulanır, abonelik finally ile kaldırılır. UI işlem kilidi onaydan önce alınır; onay ve servis sonucu liste sürümü/firma seçimiyle kontrol edilir. Eski sonuç başka firma ekranında başarı/yenileme başlatmaz. Teknik hatalar logger'a, genel bildirim güncel seçime gider.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Atama düğmesi işlem/yükleme sırasında devre dışıdır. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul ve kapsam:** Runtime testi veya gerçek backfill yapılmadı. HTTP/circuit kimliği, normal/Admin/pasif kullanıcı, rol kaldırma, NoTracking, audit rollback ve eşzamanlı firma seçimi kabulü açık. Başlamış kayıt iptal edilmez. İlişkili mevcut tenant verilerinin tutarlılığı ve eşzamanlı atama kabulü ayrıca yapılmalı; genel UpdateAsync firma değiştirme yolu bu ekte değiştirilmedi ve A-13'te açık kalır.

## A-13 devamı — genel araç güncelleme ve taşıma giriş kontrolleri — 2026-10-05

- 🟢 **Kod düzeltildi:** UpdateAsync pozitif araç kimliği, boş olmayan şase, tek seçili firma ve aktif/silinmemiş DB kullanıcısı ister. Genel güncelleme seçili firmadaki tracked araçla sınırlıdır. Bu ekteki Admin için hedef kaydı filtresiz yeniden açma yolu aşağıdaki ortak kayıt düzeltmesinde kaldırıldı; firma değişikliği taşıma servisinden yürütülür.
- 🟢 **Kod düzeltildi:** Hedef firma mevcut/silinmemiş olmalıdır; kira/komisyon carileri pozitif, hedef firmaya ait ve silinmemiş olmalıdır. Null cari desteklenir; geçersiz cari sessizce null yapılmaz. Şase mükerrer sorgusu aynı context'te yapılır, yanlış firma sessizce yok sayılmaz. Kayıt öncesi seçili firma tekrar kontrol edilir. Teknik DB hatası logger'a gider; kullanıcıya genel hata verilir.
- 🟢 **Kod düzeltildi:** MoveAracToFirmaAsync öncesinde aktif DB Admin yetkisi, tek seçili kaynak firma, kaynak firmada erişilebilir tracked araç, mevcut/silinmemiş ve farklı hedef firma ile izinli taşıma anahtarları kontrol edilir. Kira/komisyon bağlantıları hedefe uygun değilse taşıma başlamadan reddedilir; otomatik cari silme/eşleme yapılmaz. Puantaj ve servis çalışma hedefleri AsTracking yüklenir. Hazırlık hataları varsa SaveChanges yapılmaz; form da bu durumda ayrı UpdateAsync çağrısını başlatmaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kalan uygulama/kabul:** Runtime veya gerçek taşıma yapılmadı. Bu ekte iki ayrı SaveChanges olarak kaydedilen taşıma/form güncellemesi, aşağıdaki ortak kayıt düzeltmesinde birleştirildi. İlişkili cari/plaka/puantaj/personel/operasyon kayıtlarının hedef firma tutarlılığı ve seçilmeyen kayıtların politikası ayrıca tamamlanmalı. Güzergâh taşıma yolu bu ekte değiştirilmedi. Rol/firma değişiminin yazımla yarışı, A→B→A, eski bozuk ilişkiler, tam HTTP/circuit ve DB/audit rollback kabulü bekler. Başlamış commit bu kontrollerle geri alınmaz; A-13 bütünüyle kapanmadı.

## A-13 devamı — araç taşıma ve form alanlarının ortak kaydı — 2026-10-05

- 🟢 **Kod düzeltildi:** Araç formundaki iki taşıma onayı güncel form alanlarını MoveAracToFirmaAsync çağrısına verir; taşıma sonrasında ikinci UpdateAsync çağrısı kaldırıldı. Form alanları, kaynak firma/kayıt izi ve seçilen ilişkili değişiklikler aynı context içinde hazırlanıp tek SaveChanges/audit akışında kaydedilir. Hazırlık hatasında hiçbir değişiklik kaydedilmez.
- 🟢 **Kod düzeltildi:** Form kimliği ve hedef firma eşleşmesi kontrol edilir; değiştirilebilir alanların kopyası ilk await öncesinde alınır. Pozitif olmayan cari kimlikleri reddedilir; kira/komisyon carileri hedef firmada mevcut ve silinmemiş olmalıdır. Şase mükerrerliği kayıt öncesinde kontrol edilir. Genel UpdateAsync farklı firmaya yazımı reddeder ve taşıma akışına yönlendirir.
- 🟢 **Kod düzeltildi:** Firma olayı sürümü A→B→A seçimini de geçersizleştirir; geçici abonelik finally ile kaldırılır. Aktif DB Admin yetkisi kayıt öncesinde yeniden doğrulanır. Kayıt sonrası cache temizleme hatası logger'a gider; tamamlanmış DB kaydı başarısız taşıma olarak bildirilmez.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi. git diff --check boşluk hatası bildirmedi.
- 🟡 **Açık kapsam/kabul:** Runtime testi veya gerçek taşıma yapılmadı; çalışan uygulama yeniden başlatılmadı. İlişkili ve seçilmeyen kayıtların tenant politikası, eski cari/plaka/personel/operasyon bağlantıları, eşzamanlı taşıma, rol/firma değişiminin commit ile yarışı ve DB/audit rollback kabulü açık. Güzergâh taşıma yolu bu ekte değiştirilmedi. Başlamış commit'i geri alma garantisi verilmez; A-13 genel durumu sarıdır.

## A-13 devamı — araç evrak/dosya firma bütünlüğü ve envanter yetkisi — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşıma kodundaki eski “AracEvrak FirmaId taşımaz” varsayımı kaldırıldı. Silinmemiş araç evrakları ve bu evrakların silinmemiş dosyaları AsTracking yüklenir; her iki kaydın FirmaId ve UpdatedAt alanları araçla aynı SaveChanges/audit akışında hedefe aktarılır. Taşınan kayıt sayısı dosyaları da kapsar; fiziksel dosya yolu değiştirilmez.
- 🟢 **Kod düzeltildi:** Evrak/dosya kaynak firma kimliği araçla eşleşmiyorsa (boş kimlik dahil) işlem kayıt öncesinde reddedilir. Mevcut evraklar seçilmeden araç taşınamaz; UI bu seçimi zorunlu ve devre dışı onay kutusuyla gösterir. Servis aynı kuralı doğrudan çağrıda da uygular.
- 🟢 **Kod düzeltildi:** GetAracTransferItemsAsync pozitif araç kimliği, tek seçili kaynak firma, aktif/silinmemiş DB Admin kullanıcısı ve kaynak firmada erişilebilir araç kontrolünden sonra ilişki sayımlarını açar. Firma olayı sürümü liste hazırlığı boyunca A→B→A değişimini de yakalar; abonelik finally ile kaldırılır. Listeleme hatası UI'de yakalanır, logger'a kaydedilir ve genel bildirim gösterilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Gerçek taşıma veya runtime testi yapılmadı; çalışan uygulama yeniden başlatılmadı. Puantaj/bakım ve diğer ilişkilerin kaynak/hedef bağlantıları, seçilmeyen kayıt politikası, silinmiş evrak/dosyaların yeniden etkinleştirilmesi, dosya sürümü erişimi, eşzamanlı yazım ve rollback kabulü açık. Eski firma uyuşmazlıkları bu akışta otomatik onarılmaz. A-13 genel durumu sarı kalır.

## A-13 devamı — seçilen puantaj ve servis çalışma bağlantıları — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşınması seçilen aktif puantajların IsverenFirmaId, servis çalışmalarının FirmaId alanı kaynak araç firmasıyla eşleşmelidir; boş/başka firma bağlantısı kayıt öncesinde reddedilir. Uygun kayıtların firma ve UpdatedAt alanları ortak SaveChanges akışında güncellenir.
- 🟢 **Kod düzeltildi:** Seçilen puantajın dolu cari, kurum, güzergâh ve şoför bağlantıları hedef firmada mevcut ve silinmemiş olmalıdır. Servis çalışmasının zorunlu güzergâh ve şoför bağlantıları da hedef firmaya göre kontrol edilir. Bağlantılar otomatik silinmez veya başka kimliğe çevrilmez; uyumsuzlukta taşıma kaydedilmez.
- 🟢 **Kod düzeltildi:** Gelir/gider faturası, hesap dönemi veya önceki sürüm bağlantılı puantajlar ile aktif masraf bağlantılı servis çalışmaları eşleme tamamlanana kadar bu taşıma akışında reddedilir. Önceden değiştirilen tracked araç/evrak alanları da bu ret halinde SaveChanges yapılmadığı için kaydedilmez. ServisCalisma envanter etiketi gerçek kapsamına uygun “Servis çalışma kayıtları” olarak düzeltildi.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Gerçek taşıma veya runtime testi yapılmadı. Seçilmeyen kayıtların tarihsel firma politikası, hedef kayıt eşleme ekranı, Excel import ve diğer dolaylı ilişkiler, şoför/personel atamaları, plaka takibi/faturaları, silinmiş kayıtlar ve eşzamanlı yazım/rollback kabulü açıktır. Bu korumalar tam ilişki geçişi veya satış kabulü değildir; A-13 sarı kalır.

## A-13 devamı — araç taşıma ekranında işlem ve sonuç koruması — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşıma envanteri ve onay işlemi ortak işlem kilidi kullanır; çift listeleme/taşıma ve işlem sürerken Kaydet üzerinden yeni taşıma penceresi açma engellenir. Onay düğmeleri ve seçim kutuları işlem boyunca devre dışıdır; yazım sürerken kapatma/iptal engellenir ve bekleme bilgisi gösterilir. Envanter yüklenmeden taşıma başlatılmaz.
- 🟢 **Kod düzeltildi:** Modal sürümü, araç kimliği, kaynak ve hedef firma işlem başında yakalanır. İptal, yeni pencere, route parametresi, firma olayı (A→B→A dahil) ve Dispose eski sonucu geçersizleştirir. Envanter sonucu, hata/başarı bildirimi ve yönlendirme yalnız güncel pencere için uygulanır; firma olayı/Dispose aboneliği kaldırma kapsamındadır. İşlem kilidi finally ile bırakılır.
- 🟢 **Kod düzeltildi:** Taşıma hazırlık ve istisna ayrıntıları logger'a gider; kullanıcıya genel yetki/firma/ilişki kontrolü yönergesi gösterilir. Hata halinde pencere açık kalır; başarı sonrası kapanır. Başlamış DB kaydı, pencere veya firma değişikliğiyle geri alınmış sayılmaz.
- 🟢 **Ekran düzeltildi:** “Bugünden Sonra Kopyala” düğmesi aynı MoveAracToFirmaAsync çağrısını yaptığı ve tarih bazlı kopya üretmediği için kaldırıldı. Ekran yalnız gerçekten uygulanan taşıma işlemini sunar; kopyalama özelliği eklenmedi.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime/UI veya gerçek taşıma testi yapılmadı. Çift tıklama, envanter beklerken iptal/yeniden açma, firma/route değişimi, Dispose, başarısız taşıma ve commit yarışı kabulü açıktır. Diğer form yükleme/kayıt/evrak işlemleri bu sürüm koruması kapsamında değildir. Seçilmeyen ilişkilerin politikası ve hedef eşleme işleri nedeniyle A-13 sarı kalır.

## A-08 devamı — maaş snapshot muhasebe bağlantısı ve transaction — 2026-10-05

- 🟢 **Kod düzeltildi:** MuhasebeSnapshotService içindeki fiş/iptal fişi bağlantısını yazan iki ExecuteUpdateAsync kaldırıldı. Snapshotlar AsTracking yüklenir; MuhasebeFisId/IptalFisId ve UpdatedAt değişiklikleri normal SaveChanges/audit akışından geçer.
- 🟢 **Kod düzeltildi:** Hesap ön hazırlığı, normal/ters fiş ve snapshot bağlantıları tek üst transaction içinde çalışır. ExecutionStrategy her denemede yeni context/tracker ve transaction açar; hata halinde commit yapılmaz ve transaction dispose edilir. Audit savepoint akışı mevcut transaction içinde kalır.
- 🟢 **Kod düzeltildi:** Yıl/ay/pozitif firma ve tek seçili kaynak firma doğrulanır. Firma olayı sürümü A→B→A değişimini yakalar; commit öncesi tekrar kontrol edilir, abonelik finally ile kaldırılır. Mükerrer fiş sorgusu MAS-firma önekiyle daraltıldı; iptal için tüm snapshotların aynı mevcut kaynak fişe bağlı olması ve fişin firma öneki/kaynak tipi kontrol edilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul:** Gerçek DB/audit rollback veya runtime testi yapılmadı. İki eşzamanlı muhasebeleştirme, fiş numarası/hesap tekillikleri, commit sonucu belirsizliği, eski tutarsız snapshotlar ve kaynak fiş toplam/kalem uyumu A-09/A-15/A-16 altında açıktır. MuhasebeFis üzerinde doğrudan FirmaId olmadığından önek kontrolü kalıcı tenant ilişki kısıtının yerine geçmez.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Bu ek yalnız MuhasebeSnapshotService içindeki iki doğrudan yazımı kapatır. Hedefli servis aramasında BudgetService, PuantajFinansService, GuzergahService/GuzergahSeferService, RebuildService, LicenseService ve bakım/restore servislerinde başka doğrudan SQL/toplu yazım yolları vardır; tam repo envanteri ve her yolun audit/transaction kararı henüz tamamlanmadı. A-08 genel durumu kırmızı kalır.

## A-08 devamı — güzergâh sefer yenilemede audit ve transaction sınırı — 2026-10-05

- 🟢 **Kod düzeltildi:** GuzergahSeferService.ReplaceAllInCurrentDbAsync içindeki ExecuteUpdateAsync kaldırıldı. Aktif eski seferler AsTracking yüklenir; IsDeleted, DeletedAt ve UpdatedAt normal SaveChanges/audit akışında yazılır. Zaten silinmiş seferlerin geçmiş zaman damgaları değiştirilmez.
- 🟢 **Kod düzeltildi:** Metot açık transaction gerektirir; eski seferleri kapatma ve yenilerini ekleme aynı üst transaction içinde kalır. Mevcut iki çağıran transaction açmaktadır. Hata/sayı uyuşmazlığında çağıranın rollback akışı çalışır. ChangeTracker.Clear kaldırıldı; üst işlemin takip ettiği güzergâh ve diğer kayıtlar yardımcı metot tarafından detache edilmez.
- 🟢 **Kod düzeltildi:** Yazım için pozitif güzergâh ve tek seçili kaynak firma gerekir; parent ve aktif eski seferlerin firma kimlikleri eşleşmelidir. Seçili firma kayıtlar öncesinde ve metot sonunda tekrar kontrol edilir. Tutarsız sefer firması sessizce düzeltilmez; işlem reddedilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime veya gerçek DB rollback/audit testi yapılmadı. Eşzamanlı replace, retry/commit belirsizliği, üst transaction savepoint davranışı ve eski ilişki kabulü açık. Buradaki seçim kontrolü anlık kimlik eşitliğidir; A→B→A olayı ve üst çağıranın commit anı için işlem boyunca sürüm koruması ayrıca tamamlanmalıdır. GuzergahService'in diğer doğrudan yazımları bu ekte değiştirilmedi; A-08 kırmızı kalır.

## A-08 devamı — güzergâh ana kayıt yazımlarında audit — 2026-10-05

- 🟢 **Kod düzeltildi:** GuzergahService.UpdateAsync ve UpdateWithSeferlerAsync içindeki iki ExecuteUpdateAsync kaldırıldı. Ana güzergâh AsTracking ve silinmemiş kayıt koşuluyla yüklenir; cari/kurum bağlantıları, gelir/gider fiyatı ve KDV aynı tracked kaydın SaveChanges/audit akışında yazılır. GelirFiyat mevcut BirimFiyat alanına bağlıdır; ayrı SQL fiyat yazımı gerekmez.
- 🟢 **Kod düzeltildi:** Normal UpdateAsync içindeki ikinci bağımsız SQL yazımı kaldırıldı; ana alanlar ve cari/kurum tek SaveChanges çağrısında kaydedilir. Mevcut 0/null girişte eski cari/kurum bağlantısını koruma davranışı değişmedi. Sonuç doğrulaması global filtre kapsamında okunur.
- 🟢 **Kod düzeltildi:** Seferli güncellemede ana kayıt, sefer kapatma/ekleme ve audit mevcut üst transaction içinde kalır. Sefer yardımcı metodundaki takip temizleme çağrısı önceki ekte kaldırılmıştır; bu ekte ana kayıt için ayrıca SQL bypass yapılmaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Runtime/gerçek DB veya rollback testi yapılmadı. Güzergâh genel güncelleme/create/delete/transfer yetkisi ve firma ilişkileri, 0/null ile ilişki temizleme ürün davranışı, firma değişiminde sonuç doğrulaması, cache hatası sonrası kullanıcı bildirimi, eşzamanlı yazım ve retry/commit belirsizliği ayrıca denetlenmelidir. Bu ek tam güzergâh tenant kabulü değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Güzergâh ana kayıt/sefer servislerindeki hedeflenen üç ExecuteUpdate yolu kapandı; BudgetService, PuantajFinansService ve diğer bakım/restore/toplu yazım yolları için repo envanteri ve audit/transaction kararları devam eder. A-08 genel durumu kırmızı kalır.

## A-08 devamı — bütçe ödeme geri almada ortak kayıt/audit — 2026-10-05

- 🟢 **Kod düzeltildi:** BudgetService.OdemeGeriAlAsync içindeki ExecuteUpdateAsync kaldırıldı. Aktif ödeme AsTracking yüklenir; ödeme durum/tarih/tutar/hesap/kesinti alanları normal SaveChanges/audit akışında sıfırlanır. Banka hareketinin silinmesi için ayrı erken SaveChanges kaldırıldı; banka silme ve ödeme güncelleme tek SaveChanges transaction'ında kaydedilir.
- 🟢 **Kod düzeltildi:** Bağlı banka hareketi erişilebilir/silinmemiş ve ödeme ile aynı pozitif firmaya ait olmalıdır. Eksik/gizli hareket varsa ödeme bağlantısı sessizce sıfırlanmaz. Fatura, muhasebe fişi, mahsup, personel geri ödeme, araç masrafı veya aktif fatura eşlemesi olan hareketler kendi iptal akışına yönlendirilir; eşlemeler otomatik silinmez.
- 🟢 **Kod düzeltildi:** Başka aktif bütçe ödemesinin veya mahsup/personel geri ödeme hareketinin kullandığı banka kaydı silinmez. Filtre atlama yalnız bağlı hareket kimliğiyle çakışma kontrolüdür. Kısmi ödeme veya sonraki döneme aktarılmış ödeme, tüm ilişkiler birlikte geri alınmadan bu basit iptal akışında reddedilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime veya gerçek DB rollback/audit testi yapılmadı. Mevcut fiziksel banka hareketi silme davranışı korunmuştur; ters kayıt/soft delete politikası ayrıca değerlendirilmelidir. Tam rol/firma yetkisi, kredi kartı ve diğer dolaylı ilişkiler, kısmi/devir ödeme iptal uygulaması, eşzamanlı bağlantı ekleme ve commit belirsizliği açıktır. Ön sorgular eşzamanlı DB ilişki kısıtı değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Bu ek yalnız ödeme geri almadaki bir SQL bypass'ını kapatır. BudgetService'in kısmi ödeme, normal ödeme ve diğer toplu yazımları ile repo genelindeki SQL/audit envanteri devam eder; A-08 kırmızı kalır.

## A-08 kapanış çalışması — kalan toplu servis yazımları ve repo envanteri — 2026-10-05

- 🟢 **Kod düzeltildi:** BudgetService (5), CRMService (1), PuantajFinansService hakediş bağlantısı (1), RebuildService (6), LicenseService (3), eski TestSessionService satır rollback (4), WhatsAppService (1) ve EvrakArsivBackfillService (2) olmak üzere **23 toplu çağrı** tracked sorgu/UpdateTrackedAsync/SaveChanges akışına taşındı. Ortak yardımcı doğrudan SQL çalıştırmaz. Tek kayıt beklenen bütçe, hakediş ve evrak güncellemesi eşleşme yoksa başarısız olur; başarı gibi dönmez. Hata halinde bu sorgunun kayıt değerleri ve değişiklik işaretleri işlem öncesine döndürülür.
- 🟢 **Kod düzeltildi:** Hakediş snapshotındaki iki raw SQL yazımı kaldırıldı. SnapshotTransaction işaretçisi ve tracked tutar artışı/negatif tutar normalizasyonu tek Serializable transaction/SaveChanges kapsamındadır; ExecutionStrategy her denemede yeni context açar. Snapshot hatası sessizce yutulmaz; mevcut faturanın oluşmuş olduğu açık hata mesajıyla belirtilir. Bu, fatura alt servisinin ayrı transaction'ını geri alma garantisi değildir.
- 🟢 **Envanter oluşturuldu:** [A-08 SQL/audit kapanış envanteri](A-08-SQL-AUDIT-KAPANIS-ENVANTERI.md) repo C# kaynaklarındaki kalan çağrı konumlarını ve audit/transaction kararlarını listeler. Çağrı listesi DDL, sayaç, wrapper tanımı, audit iç düzeltmesi ve test kodunu da içerir; tamamı açık iş yazımı sayılmadı. Script/dinamik SQL semantik incelemesi ayrıca gereklidir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kabul sınırı:** Runtime/test veya gerçek veri değişikliği yapılmadı. Tracked batch büyük kümeyi belleğe alır ve context'teki diğer bekleyen değişiklikleri de kaydeder; yük, eşzamanlılık, commit belirsizliği ve tam mali zincir rollback kabulü açık. Firma bağlamı olmayan sistem kayıtlarında mevcut audit resolver satır üretmeyebilir; normal SaveChanges kullanılması her sistem kaydının audit kanıtı değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kapanış engeli:** Başlangıç/migration/seed veri onarımları, restore/legacy transfer/DataSync/demo işlemleri ve test tablo restore yollarında operasyon audit/rollback sözleşmesi henüz tamamlanmadı. Destek sayaçlarının atomik increment kararı ve audit EntityId iç düzeltmesi ayrı belgelenmiştir. Bu açıklar yalnız rapor rengini değiştirerek kapatılmaz; A-08 genel durumu kırmızı kalır.

## A-08 devamı — legacy aktarımda tablo transaction ve operasyon audit'i — 2026-10-05

- 🟢 **Kod düzeltildi:** LegacyDataTransferService içindeki roller, kullanıcılar, rol yetkileri, muhasebe hesapları ve generic tablo aktarımı tablo başına Npgsql transaction açar. Veri yazımı ile parametreli AktiviteLoglar özet kaydı aynı transaction'dadır; audit kaydı başarısızsa tablo commit edilmez. Başarı logu commit sonrasında yazılır. Satır bazlı eski/yeni değer yerine açıkça operasyon özeti tutulur.
- 🟢 **Kod düzeltildi:** Özet kaydı işlem kimliği, tablo, gerçek etkilenen satır sayısı, muhasebe üst hesap güncelleme sayısı, kaynak/hedef DB adı ve yapılandırılmış hedef firma kimliği içerir. Connection string, parola, kaynak satır verisi veya SQL parametreleri günlüğe kopyalanmaz. ON CONFLICT DO NOTHING satırları başarı sayısını artırmaz.
- 🟢 **Kod düzeltildi:** Satır INSERT/upsert işlemleri savepoint kullanır. Beklenen unique çakışmasında savepoint geri alınarak transaction kullanılabilir tutulur; muhasebe hesap Id fallback'i aynı transaction içinde devam eder. Generic kolon uyuşmazlığı döngüyü kesip kısmi başarı vermek yerine tablo işlemini hatayla durdurur. Hata/erken çıkışta transaction disposal commit edilmemiş tablo değişikliklerini geri alır.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek legacy veritabanı değiştirilmedi.
- 🟡 **Kalan kapsam/kabul:** İşlem sınırı tüm aktarım değil, tek tablodur; daha önce commit edilen tablolar sonraki tablo hatasında geri alınmaz. Source bağlantıları salt okunur kalır fakat tüm kaynak tablolar ortak snapshot içinde okunmaz. PostgreSQL sequence setval etkileri transaction rollback garantisine dahil değildir. Audit tablosu/kolonları yoksa veri commit'i reddedilir. Eski firma/rol/kimlik eşleme, idempotency, aynı anda iki aktarım, audit PK/sequence ve gerçek PostgreSQL rollback kabulü bekler.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan kapanış:** Legacy tablo yazımı için operasyon audit'i eklendi; schema hazırlığı, başlangıç veri onarımları, diğer restore/aktarım/demo yolları ve firma bağlamı olmayan sistem audit'i açık. A-08 genel durumu kırmızıdır.

## A-08 devamı — test tablo geri yüklemede ortak transaction ve audit — 2026-10-05

- 🟢 **Kod düzeltildi:** TestSessionService.GeriAlAsync yalnız PostgreSQL'de, aktif test etiketi eşleştiğinde ve DB'de aktif/silinmemiş Admin yetkisi doğrulandığında çalışır. Tek pozitif firma seçimi gereklidir; işlem boyunca firma olay sürümü A→B→A değişimini de yakalar. Geçici abonelik finally ile kaldırılır.
- 🟢 **Kod düzeltildi:** Gerekli altı backup tablosu yazım öncesinde kontrol edilir; eksik tablo sessizce atlanmaz. Bütün hedef tablolar tek TRUNCATE RESTRICT komutuyla hazırlanır; yedeklenmemiş bağımlı tabloları silen CASCADE kaldırıldı. Dış FK bağımlılığı varsa işlem reddedilir; bu tablolar otomatik silinmez veya yedeklenmiş kabul edilmez.
- 🟢 **Kod düzeltildi:** TRUNCATE/INSERT ve TestSnapshotRestore AktiviteLog kaydı aynı üst transaction içindedir. Raw ADO komutları EF CurrentTransaction'a açıkça bağlanır. Audit kaydı işlem kimliği, tag, tablo kümesi, session ve WholeDatabaseTables kapsamını belirtir; satır audit'i yerine operasyon özeti üretir. ExecutionStrategy denemeleri yeni context/transaction ile başlar.
- 🟢 **Kod düzeltildi:** Test oturumu yalnız başarılı commit sonrasında kapatılır. Hata halinde işlem başarılı gösterilmez; oturum/yedekler korunur ve teknik hata logger'a gider. Kullanıcıya genel yedek/bağımlılık/yetki kontrolü bildirimi verilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek test verisi veya yedek geri yüklenmedi.
- 🟡 **Kalan kabul/kapsam:** Gerçek PostgreSQL restore/rollback/audit testi yapılmadı. Mevcut altı tablo kümesi bütün FK bağımlılıklarını kapsamaz; bu durumda RESTRICT işlemi durdurur. Tam bağımlılık yedeği, INSERT sırası/kolon uyumu, sequence/kimlik eşlemesi ve commit belirsizliği kabulü açık. Seçilen firma audit aidiyetidir; tablo restore tüm firma satırlarını kapsar. Test başlatma/backup oluşturma, cleanup ve basit BeginSession yollarının yetki/transaction/audit sözleşmesi bu ekte tamamlanmadı. A-08 genel durumu kırmızı kalır.

## A-08 devamı — test backup/cleanup/session bakım sözleşmesi — 2026-10-05

- 🟢 **Kod düzeltildi:** BaslatAsync, BeginSessionAsync ve TemizleAsync PostgreSQL bakım yardımcısında aktif DB Admin yetkisi, tek pozitif firma ve işlem boyunca firma olay sürümü kontrolü kullanır. Bakım değişiklikleri, session rezervasyonu ve açık kapsamlı AktiviteLog operasyon kaydı aynı transaction/SaveChanges içindedir; yalnız commit sonrası AppMode/başarı durumu yayınlanır.
- 🟢 **Kod düzeltildi:** Backup öncesinde aynı tag için mevcut tablo varsa işlem reddedilir; DROP ile eski yedek silinmez. Altı kaynak tablo SHARE kilidiyle korunur; CREATE TABLE AS SELECT işlemleri ve audit ortak transaction'dadır. Hata halinde dönülen backup tablosu listesi temizlenir ve başarısız işlem yeni oturum başlatmaz.
- 🟢 **Kod düzeltildi:** Cleanup aktif test varken reddedilir. Süreç içi ortak semaphore bakım çağrılarını sıraya alır; PostgreSQL transaction advisory lock backup/cleanup/begin/restore DB işlemlerini aynı anahtarla sıralar. ReadCommitted altında session maksimumu silinmiş loglar dahil okunur; session Begin marker'ı audit ile birlikte kaydedilir. Başka süreçlerdeki AppMode durumunu bu kilit tek başına doğrulamaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek backup/restore veya test oturumu çalıştırılmadı.
- 🟡 **Kalan kabul:** Farklı süreçlerde aktif test oturumu sahipliği, bağlantı/commit belirsizliği, session marker ve eski rollback uyumu, tag normalizasyon çakışması, DDL/SHARE lock gecikmesi ve gerçek PostgreSQL rollback kabulü açık. SQL backup yalnız altı tabloyu kapsar; tam bağımlılık kurtarması değildir. Eski satır rollback yolunun ayrı yetki ve kısmi sonuç sözleşmesi ayrıca denetlenmelidir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kapanış sınırı:** Test backup/cleanup/begin kaynak kontrolleri tamamlandı; başlangıç/migration veri onarımı, diğer restore/DataSync/demo/dış araç operasyon kayıtları ve firmasız sistem audit bağlamı halen açıktır. Bu işler tamamlanmadan A-08 yeşile çevrilmez.

## A-08 devamı — başlangıç fatura onarımı ve demo bakım sınırı — 2026-10-05

- 🟢 **Gelen fatura onarımı:** PostgreSQL doğrudan UPDATE kaldırıldı; bütün sağlayıcılarda AsTracking + SaveChanges ve UTC zaman damgası kullanılır. Firma aidiyeti bulunmayan kayıtlar yazımdan önce reddedilir; hata artık başarı gibi yutulmaz.
- 🟢 **Demo bakım kaydı:** Ekleme, [TEST] temizliği ve yenileme aktif Admin/tek firma kontrolü, firma değişim sürümü, ortak PostgreSQL transaction/advisory kilidi ve aynı transaction'daki operasyon audit'i kapsamındadır. Retry denemesinde yeni context/seeder oluşturulur; seed başarısız sonucu commit edilmez. İki ekran aynı servisi çağırır.
- 🟢 **Tehlikeli sıfırlama kaldırıldı:** Demo yenileme yalnız seçili firmanın [TEST] kayıtlarını temizleyip yeniden üretir. Eski tüm-veritabanı TRUNCATE API'si yazım yapmadan açık hata döndürür; kullanıcı/lisans/audit geçmişini silen CASCADE ve session_replication_role yolları kaldırılmıştır. Ekran açıklamaları kapsamı belirtir.
- 🟢 **Sınırlı firma temizliği:** Tanımlı 11 tabloda parametreli FirmaId silme + operasyon audit'i ortak transaction'dadır. FK kontrolleri açık kalır; tablo hatası atlanmaz. FirmaId taşımayan ortak MuhasebeFisleri/MuhasebeHesaplari bu işlemden çıkarılmıştır. Bu yol tüm firma verisinin eksiksiz silindiğini iddia etmez.
- 🟡 **Kabul açık:** Gerçek PostgreSQL demo/temizlik/audit/rollback testi yapılmadı. İlişkili kayıtlar fiziksel silmeyi engelleyebilir; işlem başarısız döner. Seed'in eski demo veri aidiyeti, FK/kayıt üretimi ve commit belirsizliği kabulü bekler. Sadece derleme kontrolü bu kabulü kapatmaz.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan:** Diğer başlangıç/migration veri onarımları, restore/DataSync/dış araç yazımları ve firmasız sistem audit sözleşmesi açıktır. A-08 genel durumu kırmızı kalır.

- 🟢 **Derleme kontrolü:** Son kaynaklarla Web projesi izole çıktı klasörüne `--no-restore -p:UseAppHost=false` ile derlendi: **0 uyarı, 0 hata**. `git diff --check` temiz. Runtime/gerçek veri temizliği çalıştırılmadı.

## A-08 ortak altyapı — SQL ve sistem yazımlarının veritabanında denetimi — 2026-10-05

- 🟢 **Ortak altyapı eklendi:** PostgreSQL/SQLite iş tablosu tetikleyicileri EF dışı INSERT/UPDATE/DELETE'yi aynı transaction içinde denetler; PostgreSQL TRUNCATE kapsam/satır sayısını kaydeder. Firma bağlamı olmayan kayıtlar ayrı DB günlüğünde sistem kapsamındadır. Sır alanları maskelenir; audit hatası iş yazımını engeller.
- 🟢 **Başlangıç/aktarım:** Şema kurulumundan sonra installer, legacy hedef bağlantısında installer, DataSync hedefinde installer eklendi. Master başlangıç kopyası kaynak snapshot ve ortak hedef transaction'a alındı; satır hatası artık atlanmaz. SQLite journal iş tablosu aktarım/sıfırlama listesinden çıkarıldı.
- 🟢 **Restore:** Kalıcı dış operasyon makbuzu ve source SHA-256; PostgreSQL audit şemasını yedek/restore iş kapsamından ayırma; SQLite açık dosyayı ezmek yerine backup API ve öncesi geri dönüş kopyası. Deploy betiğinde DB DROP kaldırıldı; atomik restore ve sıfır dışı hata kodunun reddi eklendi.
- 🟢 **SQLite izole kontrol:** Doğrudan SQL, sistem kapsamı, sır maskeleme, rollback, günlük değişmezliği ve audit hatasında veri yazımının durması geçti.
- **Sözleşme:** [A-08 veritabanı audit sözleşmesi](A-08-VERITABANI-AUDIT-SOZLESMESI.md). PostgreSQL izole kontrolü ve son kaynak derlemeleri tamamlanınca görev satırı güncellenecek. Müşteri verisi restore edilmedi.

## 🟢 A-08 kapanışı — ortak denetim motoru ve izole doğrulama — 2026-10-05

**A-08 tamamlandı.** Bu ek önceki A-08 “açık/kırmızı” kayıtlarının güncel durumunu değiştirir; önceki ekler tarihsel çalışma kanıtıdır.

- 🟢 **SQL bypass kökten kapatıldı:** Ortak PostgreSQL/SQLite tetikleyicisi EF, doğrudan SQL, toplu yazım, migration ve binary COPY'yi veriyle aynı transaction içinde kaydeder. Audit hatasında veri yazımı gerçekleşmez; rollback günlüğü de geri alır. Firma bilgisi bulunmayan sistem kayıtları başka firmaya mal edilmez.
- 🟢 **Kapsam tamamlandı:** Web startup + 11 veri migration sınıfı; maaş/özlük/SMS onarım kapıları; master kopyada kaynak snapshot + ortak hedef transaction; legacy/DataSync hedef kurulumları; demo bakımı; LisansDesktop yerel satış/yenileme SQL geçmişi; 4 bağımsız veri SQL betiği ve Deploy restore. Günlük kendi kendini audit etmez; migration geçmişi/kimlik sequence metadatası iş satırı değildir.
- 🟢 **Sır maskeleme ortaklaştırıldı:** DbContext otomatik audit, AuditLogService ve DB motoru API/key/password/credential/payload/değer alanlarını maskeler. Yerel/üretim sırları test çıktısına yazılmadı.
- 🟢 **Restore kanıtı kalıcı:** PostgreSQL public restore mevcut `mk_audit` geçmişini korur. SQLite eski veri/audit bağımsız before-restore kopyasında korunur. SHA-256 başlangıç ve ayrı başarı/belirsiz sonuç makbuzu, restore edilen DB'nin dışında saklanır. Düz SQL restore ve mevcut DB'yi düşürme kaldırıldı; hata kodu 1 başarı sayılmaz.
- 🟢 **Gerçek izole çalışma zamanı kontrolü:** SQLite ve PostgreSQL 17 üzerinde doğrudan SQL, sistem kapsamı, sır maskeleme, rollback, audit hatasında yazımın reddi, ALWAYS/replica, TRUNCATE, yeni migration tablosunun ilk INSERT'i ve binary COPY geçti. Gerçek custom pg_dump/pg_restore ile veri/audit koruması ve SQLite backup API ile geri dönüş kopyası doğrulandı. Geçici PG test DB'si silindi, sunucu durduruldu; müşteri DB'sine dokunulmadı.
- 🟢 **Son kaynak derlemeleri:** Web, DataSync ve LisansDesktop projeleri izole çıktı klasörlerinde `--no-restore -p:UseAppHost=false` ile derlendi: her biri **0 uyarı / 0 hata**. Restore PowerShell parser kontrolü geçti.
- **Kanıtlar:** [Veritabanı audit sözleşmesi](A-08-VERITABANI-AUDIT-SOZLESMESI.md), [izole kontrol ve kaynak SHA-256](A-08-IZOLE-DOGRULAMA-2026-10-05.md), [çağrı/kapsam envanteri](A-08-SQL-AUDIT-KAPANIS-ENVANTERI.md).
- 🟡 **Ayrı ürün kabulü:** Gerçek müşteri migration/DataSync verisi, yüksek hacim, mali zincir, bağımsız makine ve dosya/key ring kurtarması A-04/A-09/A-18/A-19 kapsamında açıktır. Bunlar A-08'in açık kod işi olarak tekrar sayılmaz.

## A-03 devamı — DB-only geri yükleme kapsamının ekranda belirtilmesi — 2026-10-05

- 🟢 **Kod düzeltildi:** Yedekleme ekranındaki restore onayında işlemin yalnızca PostgreSQL verisini değiştirdiği; ZIP içindeki belge/dosya, ayar ve DataProtection anahtarlarının uygulanmadığı açıkça gösterilir. Eylem düğmesi “Yalnızca DB'yi Geri Yükle” olarak adlandırıldı.
- 🟢 **Dokümantasyon:** `SIFRELI-BELGE-YEDEK-KURTARMA.md` ekran davranışı ve sınırla eşitlendi.
- 🔴 **A-03 açık:** DB+dosya+anahtarları kapalı bakım penceresinde birlikte uygulayan, ayarları hedefe göre koruyan ve her aşamada önceki duruma dönen otomasyon henüz eklenmedi. Bu UI düzeltmesi tam kurtarma değildir; A-03 kırmızı kalır.

## A-03 devamı — deploy DB restore hata geri dönüşü — 2026-10-05

- 🟢 **Kod düzeltildi:** `01-db-restore.ps1`, mevcut DB için `mk_audit` hariç custom geri dönüş dump'ını restore öncesinde üretip boyutunu kontrol eder. Kaynak restore girişiminden sonra herhangi bir hata olursa eski `public` şemayı tek transaction'lı `pg_restore` ile geri yükler, audit installer'ı yeniden uygular ve `rolled-back.json` makbuzu yazar.
- 🟢 **Yeni hedef DB:** DB önceden yoksa ve kaynak restore hata verirse yalnız bu çağrıda oluşturulan hedef DB bağlantıları kapatılıp DB kaldırılır. Eski bir DB hiçbir hata kolunda DROP edilmez.
- 🟡 **Doğrulama sınırı:** PowerShell parser/derleme ve diff kontrolü yapılacak; bu turda canlı veya izole PostgreSQL hata enjeksiyonu çalıştırılmadı. Makbuz gerçek rollback kabulünün yerine geçmez.
- 🔴 **A-03 açık:** Dosya/ayar/key ring ile DB'yi aynı tam kurtarma operasyonunda uygulama ve birlikte geri alma otomasyonu yoktur. Mevcut değişiklik yalnız deploy DB restore betiğinin rollback açığını kapatır.

## A-03 devamı — Web ZIP DB restore rollback — 2026-10-05

- 🟢 **Kod düzeltildi:** Web ZIP restore başarılı DB restore sonrasında audit doğrulaması veya başarı makbuzu başarısızsa, önceden alınmış tam ZIP yedeğindeki tek PostgreSQL custom dump ayrı staging'e çıkarılır ve `--single-transaction` ile önceki `public` şeması geri kurulur; ardından audit installer doğrulanır.
- 🟢 **Sonuç kaydı:** Geri dönüş doğrulanırsa `rolled-back.json` yazılır. Kaynak/rollback işlemi belirsiz kalırsa `unconfirmed.json` korunur; rollback hatası Critical loglanır. Başarısız `pg_restore` da commit/bağlantı belirsizliği ihtimaline karşı önceki yedekten geri döndürülür.
- 🟢 **Derleme:** Web + Shared Debug ayrı geçici çıktı klasörüne `--no-restore -p:UseAppHost=false` ile **0 uyarı / 0 hata** derlendi.
- 🟡 **Kabul sınırı:** PostgreSQL hata enjeksiyonu/gerçek restore bu turda çalıştırılmadı; uygulama yolu kod derlemesiyle kabul edilmiş sayılmaz.
- 🔴 **A-03 açık:** Web/deploy DB rollback uygulandı; dosya, ayar ve key ring'i aynı tam kurtarma operasyonunda atomik uygulama/rollback aracı hâlâ yoktur.

## Güvenli parola istemi — restore betikleri — 2026-10-05

- 🟢 `01-db-restore.ps1` artık parola parametresi veya `MKFILO_PG_PASSWORD` desteği sunmaz. PostgreSQL parolası `Read-Host -AsSecureString` ile istenir; komut satırı argümanına aktarılmaz. PowerShell süreç ortamındaki `PGPASSWORD` yalnız işlem boyunca ayarlanır, finally bloğunda önceki değer geri yüklenir; BSTR ve SecureString temizlenir.
- 🟢 `00-aktar-baslat.ps1` parola değerini yapılandırmada tutmaz ve alt sürece parametre olarak geçmez. `04-pc2-kurulum-talimat.md` yeni istem akışını anlatır.
- 🟢 İki betiğin PowerShell parser doğrulaması ve `git diff --check` geçti; eski açık parola parametresi/metin araması hedef betik ve talimatta eşleşme vermedi. 🟡 Gerçek müşteri bağlantısı ve sır rotasyonu kanıtı yoktur; A-06 açık kalır.

## A-03 devamı — DB restore öncesi dosya/anahtar ön kontrolü — 2026-10-05

- 🟢 **Kod düzeltildi:** `02-dosya-aktar.ps1 -PreflightOnly` kaynak `.enc` başlıklarını sınıflandırır. MKD1 dosyalarında `key-*.xml`; legacy/AES dosyalarında DPAPI ile çözülebilir `master.key` veya 32 baytlık geçerli import/raw key arar. Ön kontrol hedefe yazmaz.
- 🟢 **Sıra düzeltildi:** `00-aktar-baslat.ps1` DB restore'dan önce dosya/anahtar preflight çalıştırır. Anahtarlar eksik/bozuksa DB restore başlatılmaz. Gerçek hedef kopyasında aynı kontroller yeniden yapılır.
- 🟢 **Sözdizimi:** İki betiğin PowerShell parse kontrolü ve `git diff --check` geçti. 🟡 DataProtection key XML'in hedefte gerçekten çözülebildiği farklı makine kabulü A-04'tedir. Dosya kopyası sonrasındaki I/O hatasında ortak DB+dosya rollback henüz yoktur.
- 🔴 **A-03 açık:** Bu düzeltme anahtar eksikliğiyle DB'yi tek başına restore etme riskini kapatır; tam operasyon transaction/rollback aracı değildir.

## A-03 devamı — legacy DB+dosya aktarım rollback bağlama — 2026-10-05

- 🟢 **Önceki durum snapshot'ı:** `00-aktar-baslat.ps1` dosya aktarımından önce `uploads`, `keys`, `database` klasörlerinin mevcut olup olmadığını kaydeder ve mevcut içeriği LocalAppData operasyon klasörüne kopyalar. Kopya eksikse DB restore başlamaz.
- 🟢 **DB makbuzu:** `01-db-restore.ps1`, önceden var olan DB'nin ayrı SHA-256 doğrulamalı custom dump'ını parent operasyon klasörüne kopyalar. Yeni DB oluşturduysa `pg_database.oid` değerini operasyon makbuzuna yazar.
- 🟢 **Dosya hatası geri dönüşü:** `02-dosya-aktar.ps1` başarısız olursa parent önce önceki DB dump'ını tek transaction ile yükler; DB başlangıçta yoksa yalnız aynı operasyonda oluşturulan ve OID'si eşleşen hedefi kaldırır. Sonra üç depolama klasörünü başlangıç var/yok durumuna döndürür. Geri dönüş başarı/başarısız makbuzu tutulur; hata halinde snapshot silinmez.
- 🟢 **Statik doğrulama:** İlgili üç PowerShell betiğinin parser kontrolü ve `git diff --check` geçti.
- 🟡 **Kabul sınırı:** Gerçek DB/file hata enjeksiyonu, müşteri verisi veya elektrik kesintisi testi yapılmadı. Bu, yakalanan süreç hatası rollback akışıdır; ani süreç/host kesintisinde otomatik devam garantisi değildir.
- 🟢 **Kesinti sonrası elle kurtarma:** `03-full-transfer-recover.ps1`, LocalAppData altındaki tam aktarım snapshot/makbuzunu doğrular; önceden var olan DB'yi SHA-256 doğrulamalı dump'tan geri yükler veya makbuzdaki OID eşleşen yeni DB'yi kaldırır, ardından üç depolama klasörünü snapshot'tan geri alır. Önceki otomatik rollback başarısızlık makbuzu elle kurtarmayı engellemez; yeni DB zaten kaldırılmışsa adım idempotent tamamlanır. IIS havuzunun durduğu onaylanır ve `recovered.json` yazılır.
- 🟢 **Güvenlik kontrolleri:** Operasyon klasörü doğrudan izinli journal kökü altında olmalı; dump yolu beklenen operasyon dosyasına sabitlenir, hash doğrulanır, junction/symlink hedefleri reddedilir.
- 🟢 **Kapsam belgeleri eşitlendi:** Şifreli belge yedek rehberi legacy journal geri dönüşünü RecoveryArchive ZIP uygulamasından ayrı açıklar.
- 🟢 **RecoveryArchive apply aracı:** `05-recovery-archive-apply.ps1` staging manifestinin boyut/hash'lerini doğrular; izinli storage/Luca/belge köklerini ve varsa DB dump'ını ayrı hedefe uygular. appsettings JSON dosyalarını atlar, önceki DB/dosya durumunu apply journal'ında saklar ve yakalanan hatada rollback dener.
- 🟢 **DB hata dalı:** DB alt betiği hata kodu döndürürse üst apply akışı apply journal'daki önceki DB dump'ıyla rollback'i ayrıca dener.
- 🟢 **Kesinti kurtarması:** `06-recovery-archive-rollback.ps1` tamamlanmamış apply journal'ındaki DB ve dosya snapshot'larını geri yükler. Apply/rollback belirtilen IIS havuzunu appcmd ile durdurup doğrular; yeniden başlatma kabul sonrası operatördedir. 🟡 Key XML/`KEY-HAZIR` onayı hedef kimliğinde belge çözümünü kanıtlamaz; PostgreSQL hata enjeksiyonu ve farklı makine key ring/credential kabulü yapılmadı.
- 🟢 **Statik doğrulama:** Altı aktarım/kurtarma PowerShell betiğinin parser kontrolü geçti; `git diff --check` temiz. 🟡 Gerçek PostgreSQL, hata enjeksiyonu veya elektrik kesintisi kabulü yapılmadı.
- 🔴 **A-03 açık:** Başarı sonrası IIS yeniden başlatma ve gerçek hata/kesinti/DB+belge kabulü tamamlanmadı. Ortama özel appsettings otomatik uygulanmaz.

## A-03 devamı — RecoveryArchive journal yolunun doğrulanması — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, DB/dosya hedeflerine dokunmadan önce `%LOCALAPPDATA%\MKFiloServis\OperationJournal` yolunun üst bileşenlerinde junction/symlink bulunmadığını ve yeni operasyon klasörünün normal dizin olduğunu doğrular. Beklenmeyen yönlendirmede işlem durur.
- 🟢 PowerShell parser: `05-recovery-archive-apply.ps1`, `06-recovery-archive-rollback.ps1`, `01-db-restore.ps1` başarılı. `git diff --check` temiz; yalnız doküman satır sonu uyarıları var.
- 🔴 **A-03 açık:** Bu statik kontrol gerçek IIS/NTFS/DB hata enjeksiyonu, kesinti kurtarma veya farklı makine belge çözme kabulü yerine geçmez.

## A-03 devamı — dosya rollback snapshot SHA-256 makbuzu — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, her mevcut hedef dosya kökü için snapshot kopyası sonrası dosya yolu/boyut/SHA-256 manifesti üretir. Apply sırasında hata yakalanırsa snapshot hash'lerini yeniden doğrulamadan kopyalamaz.
- 🟢 `06-recovery-archive-rollback.ps1`, kesinti sonrası geri dönüşe başlamadan önce ilgili snapshot'ların dosya sayısı, yolları, boyutları ve SHA-256 değerlerini doğrular; uyuşmazlıkta DB veya hedef dosyaları değiştirmeden durur.
- 🟢 İki PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz (dokümanlarda satır sonu uyarısı dışında).
- 🔴 **A-03 açık:** Hash makbuzu snapshot bozulmasını saptar, imza/yerel yönetici müdahalesine karşı özgünlük sağlamaz. Gerçek PostgreSQL, kesinti, NTFS ve farklı makine belge kabulü yapılmadı.

## A-03 devamı — apply hazırlık makbuzlarının atomik yazımı — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, dosya öncesi durumu, snapshot SHA-256 listesini ve `ApplyStarted` makbuzunu önce aynı journal klasöründeki benzersiz geçici dosyaya yazar, diske flush eder ve hedef makbuz adına atomik taşır. Kesinti sırasında yarım JSON'un tamamlanmış makbuz gibi görünme olasılığı azaltıldı.
- 🟢 PowerShell parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Dosya taşıma atomikliği hedef dosya sistemine bağlıdır; gerçek NTFS/ani güç kesintisi, PostgreSQL ve uygulama kabulü yapılmadı.

## A-03 devamı — snapshot doğrulamasını hedef silmeden önce yapma — 2026-10-05

- 🟢 Apply hata geri dönüşü ve kesinti sonrası rollback artık ilgili snapshot SHA-256 kontrolünü hedef klasörü silmeden önce yapar. Snapshot eksik/değişmişse o hedef korunur ve geri dönüş başarısızlığı açıkça kaydedilir; doğrulanmamış kopya kullanılmaz.
- 🟢 `00`, `01`, `02`, `03`, `05`, `06` PowerShell betiklerinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek hata enjeksiyonu ve PostgreSQL/NTFS geri dönüş kabulü yapılmadı.

## A-03 devamı — IIS durdurmadan önce apply journal alanı — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, LocalAppData journal üst yolunu doğrulayıp yeni operasyon klasörünü oluşturmadan IIS havuzunu durdurmaz. Journal yolu/oluşturma hatası artık uygulama havuzunu gereksiz yere kapalı bırakmaz. Havuz yine dosya ve DB snapshot'larından önce durdurulur.
- 🟢 Altı migration PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek IIS servis durumu geçişi, DB/NTFS hata enjeksiyonu ve hedef makine kurtarma kabulü yapılmadı.

## A-03 devamı — DB rollback makbuz ve dump doğrulaması — 2026-10-05

- 🟢 `01-db-restore.ps1` operasyon makbuzuna kaynak DB dump SHA-256 değerini yazar.
- 🟢 `05` ve `06` rollback yolları mevcut DB için `PreExistingDatabaseSnapshotReady`, hedef adı, beklenen journal dump yolu ve rollback dump hash'ini doğrular. Yeni DB durumunda `DatabaseCreatedByOperation`, sayısal DB OID'si, journal içindeki sabit kaynak dump yolu ve kaynak dump hash'i doğrulanmadan DB kaldırma adımına geçmez.
- 🟢 Üç PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** PostgreSQL üzerinde gerçek yeni/mevcut DB geri dönüşü ve kesinti/hata enjeksiyonu kabulü henüz yapılmadı.

## A-03 devamı — terminal apply/rollback makbuzlarını atomik yazma — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1` artık `applied.json` ve `rollback-result.json` sonuçlarını geçici dosyaya flush edip atomik taşır. Başarı makbuzu tamamlanmadan oluşan yazma hatası apply hata/rollback akışına düşer.
- 🟢 `06-recovery-archive-rollback.ps1` `recovered.json` ve `recovery-failed.json` makbuzlarını aynı yöntemle yazar; yarım terminal makbuzun sonraki kurtarma denemesini yanlış engelleme riski azaltıldı.
- 🟢 İki PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Atomik dosya taşıma ve gerçek süreç/güç kesintisi davranışı hedef Windows/NTFS üzerinde denenmedi; DB ve belge kabulü de bekliyor.

## A-03 devamı — hedef yol üst bileşenlerini yeniden doğrulama — 2026-10-05

- 🟢 Apply ve rollback betikleri hedefleri snapshot alma, uygulama ve geri yükleme öncesinde üst dizinleri yeniden junction/symlink açısından denetler. Başlangıç kontrolünden sonra yol bileşeni değişmişse hedef üzerinde işlem yapmaz.
- 🟢 İlgili PowerShell parser kontrolleri başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Dosya sistemi yarış koşulları, PostgreSQL arızası ve gerçek hedef kurulum kabulü henüz çalıştırılmadı.

## A-03 devamı — eski apply journal'larıyla kurtarma uyumluluğu — 2026-10-05

- 🟢 `06-recovery-archive-rollback.ps1`, yeni apply journal'larında snapshot SHA-256 doğrulamasını zorunlu tutar. Hash makbuzu olmayan eski journal'larda snapshot'ı junction/symlink açısından denetler ve DB/dosya değişikliğinden önce `ESKI-SNAPSHOT-ONAY` operatör onayı ister; onay yoksa değişiklik yapmadan durur.
- 🟢 Eski yeni-DB makbuzlarında kaynak dump hash alanı yoksa, hedef OID ve beklenen journal dump yolu doğrulanarak DB kaldırma kurtarması sürdürülebilir; yeni hash alanı varsa doğrulama zorunludur.
- 🟢 `06-recovery-archive-rollback.ps1` parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Eski journal'larda eksik hash özgünlük doğrulaması operatör onayıyla sınırlıdır; gerçek DB/kesinti kabulü yapılmadı.

## A-03 devamı — 6 Ekim journal tekrar deneme güvenliği

- 🟢 `06-recovery-archive-rollback.ps1`, önceki başarısız denemeler için her seferinde benzersiz `recovery-failed-{guid}.json` makbuzu yazar; var olan hata kaydı yeni rollback denemesini engellemez veya hata ayrıntısını ezmez.
- 🟢 `files-before-hashes.json` yolu varsa normal dosya olması zorunludur. Aynı adla klasör/özel yol bulunursa eski hash'siz journal olarak yorumlanmaz; işlem durur.
- 🟢 PowerShell parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek Windows kesinti ve PostgreSQL rollback kabulü yapılmadı.

## A-10 devamı — EBYS belge silme ve güncelleme sırası — 2026-10-06

- 🟢 EBYS dosya silme artık önce kaydı soft-delete edip DB değişikliğini kaydeder, ardından fiziksel dosyayı temizler. Fiziksel silme başarısızlığı `FileCleanupPendingException` olarak bildirilir.
- 🟢 EBYS dosya güncelleme eski dosyayı DB değişikliğinden önce silmez. Yeni şifreli dosya oluşturulur, DB'deki yol güncellenir ve commit sonucu hata verirse yeni yol taze context ile kontrol edilir; başvuru olup olmadığı doğrulanamazsa olası yetim dosya güvenlik için korunur. Commit başarılıysa eski dosya sonradan temizlenir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test veya gerçek dosya silme çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı temizleme kuyruğu/yeniden deneme, mevcut yetim dosya envanteri ve diğer servislerin DB/dosya sırası bu düzeltme kapsamında tamamlanmadı.

## A-10 devamı — EBYS işlem hareketini dosya temizliğinden önce kaydetme — 2026-10-06

- 🟢 EBYS silme/güncellemede iş kaydı DB'ye yazıldıktan sonra EBYS hareket kaydı ekleniyor, fiziksel eski dosya en son temizleniyor. Temizlik hatası olsa da DB hareket kaydı yazılmış olur; fiziksel silme DB kaydından önce çalışmaz.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test/gerçek dosya silme yapılmadı.
- 🔴 **A-10 açık:** Hareket/audit kaydı ve ana DB kaydı ayrı SaveChanges bağlamlarındadır; kalıcı cleanup kuyruğu, yetim envanteri ve diğer servislerin telafi akışı hâlâ gereklidir.

## A-10 devamı — araç uyarı ekranı upload telafisi — 2026-10-06

- 🟢 `BelgeUyariService.AracBelgeDosyaYukleAsync`, dosya DB kaydı hata döndürdüğünde artık yeni dosyayı körlemesine silmez. Taze context ile `AracEvrakDosyalari` içinde dosya yoluna başvuru aranır; başvuru varsa dosya korunur, yoksa telafi silmesi yapılır. DB doğrulaması veya telafi silmesi başarısızsa asıl hata ve telafi hatası birlikte yükseltilir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test/gerçek upload çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı temizlik kuyruğu/yeniden deneme, yetim dosya envanteri ve diğer dosya yazan servislerin belirsiz commit telafisi tamamlanmadı.

## A-10 devamı — EBYS sürüm arşivini ana dosyadan ayırma — 2026-10-06

- 🟢 `BelgeVersiyonService.ArsivleEbysEvrakDosyaAsync`, eski ana dosyanın aynı yolunu sürüm kaydına kopyalamak yerine şifreli baytları `ebys/versions/{id}` altında bağımsız dosyaya kopyalar ve sürüm kaydını bu yola bağlar. Ana dosya güncellendiğinde eski sürüm artık ana dosya temizliğiyle kaybolmaz.
- 🟢 Arşiv DB kaydı başarısız/belirsizse taze context sürüm tablosunda bu yola başvuru arar; başvuru doğrulanmadan kopyayı silmez. Sürüm içeriği okuyucusu yeni şifreli depoyu açar, eski webroot kayıtları için kök dışına çıkmayan geriye uyumlu okuma sağlar.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime/sürüm geri yükleme testi yapılmadı.
- 🔴 **A-10 açık:** Araç/personel sürüm arşivleri, kalıcı temizleme kuyruğu/yeniden deneme, eski yetim dosya envanteri ve servisler arası telafi hâlâ bekliyor.

## A-10 devamı — araç/personel sürüm arşivlerinin bağımsız dosyalanması — 2026-10-06

- 🟢 Araç ve personel sürüm arşivleri artık mevcut dosyanın aynı path'ini paylaşmaz; `CopyVersionFileAsync` ile bağımsız şifreli sürüm dosyası oluşturulur. SecureFileService dışındaki eski webroot dosyaları güvenli kök altında okunup yeni korumalı depoya alınır.
- 🟢 Sürüm içeriği okuyucuları şifreli depoyu açar, legacy webroot yolunu kök dışına çıkış denetimiyle destekler. (Sonraki 2026-10-06 düzeltmesiyle sürüm soft-delete artık fiziksel dosyayı silmiyor; aşağıdaki güncel not geçerlidir.)
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime test/gerçek dosya işlemi yapılmadı.
- 🔴 **A-10 açık:** Dosya silme ile sürüm referans kontrolü eşzamanlı DB transaction/row lock altında değildir; kalıcı cleanup kuyruğu ve eski yetim dosya envanteri hâlâ gereklidir. Diğer servis akışlarının tamamı taranıp düzeltilmedi.

## A-10 devamı — sürüm silme yarışı ve personel evrakı yenileme sırası — 2026-10-06

- 🟢 Sürüm soft-delete işlemleri artık dosyayı fiziksel olarak silmiyor. Geri yükleme ile referans kontrolü/silme arasındaki yarış, kalıcı ve serileştirilmiş cleanup mekanizması gelene kadar dosyayı koruyarak önlendi.
- 🟢 EBYS personel evrakı yenilemesinde mevcut dosya yeni upload ve DB güncellemesinden önce silinmiyor. Yeni dosya önce şifreli depoya yazılıyor; DB kaydı hata verirse taze context ile ana kayıt ve sürüm tablosu kontrol ediliyor. Başvuru doğrulanamıyorsa dosya korunup hata görünür kılınıyor.
- 🟡 Eski personel dosyası bu akışta otomatik temizlenmiyor; geçmiş sürüm ilişkisi net olmadığı için tutuluyor. Kalıcı cleanup kuyruğu/yeniden deneme ve yetim envanteri gereklidir.
- 🔴 **A-10 açık:** Genel servis taraması, belirsiz commit telafileri ve kalıcı dosya temizleme altyapısı tamamlanmadı.

## A-10 devamı — fatura PDF/XML dosya değiştirme sırası — 2026-10-06

- 🟢 Fatura PDF ve XML yenilemesinde eski dosya artık yeni dosya ve DB yolu kaydedilmeden silinmiyor. Yeni içerik şifreli depoya yazılıyor, DB path'i sonra güncelleniyor.
- 🟢 DB kaydı hata verirse taze context ile PDF/XML yolunun fatura kayıtlarında bulunup bulunmadığı doğrulanıyor. Başvuru varsa PDF yüklemesi başarılı kabul ediliyor; başvuru yoksa yeni dosya telafi ediliyor. Doğrulama/temizlik hatasında AggregateException ile belirsizlik görünür kalıyor.
- 🟡 Eski dosya otomatik kaldırılmıyor; firma içi kopya faturalar aynı yolu paylaşabildiği için güvenli, referans kontrollü kalıcı cleanup kuyruğu bekleniyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime fatura dosyası işlemi yapılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup/yeniden deneme kuyruğu, yetim dosya envanteri ve diğer tüm dosya yazan servislerin incelemesi gereklidir.

## A-10 devamı — destek eki şifreli depolama ve upload telafisi — 2026-10-06

- 🟢 Yeni destek talebi ve yanıt ekleri artık webroot içine düz metin yazılmıyor; `ISecureFileService` ile şifreli depoya kaydediliyor. Okuma akışı şifreli yeni yolları açıyor ve eski webroot eklerini kontrollü kök altında geriye dönük destekliyor.
- 🟢 DB kaydı başarısız/belirsiz kaldığında taze context ile ek tablosu kontrol ediliyor; DB başvurusu yoksa yeni dosya telafi ediliyor, doğrulama/temizlik hatasında dosya korunup hata görünür kılınıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek ek yükleme/indirme/silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Eski düz metin eklerin toplu şifreli depoya taşınması, kalıcı cleanup kuyruğu/yeniden deneme ve yetim envanteri henüz yapılmadı.

## A-10 devamı — ortak evrak ekranı dosya sırası ve yol doğrulaması — 2026-10-06

- 🟢 Ortak evrak ekranında yenileme öncesi eski dosya silme kaldırıldı. Yeni dosya DB kaydından önce yazılıyor; DB başarısız/belirsiz kalırsa taze context ile referans doğrulanıp yalnız başvurulmayan yeni dosya telafi ediliyor.
- 🟢 Evrak silme akışında önce DB soft-delete kaydediliyor; fiziksel silmeden önce diğer aktif kayıtlar denetleniyor. Temizlik hatası artık sessizce yutulmuyor.
- 🟢 `FileService` okuma/silme/yol çözümlemesi, depolama kökü dışına taşan veya mutlak yol içeren dosya adlarını reddediyor.
- 🟡 Eski evrak dosyaları, kalıcı referans kontrollü temizleme kuyruğu kurulana kadar tutuluyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime dosya senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Eski düz metin dosyaların şifreli depoya taşınması, kalıcı cleanup/yeniden deneme ve yetim envanteri devam ediyor.

## A-10 devamı — personel özlük ekranı upload telafisi ve sürüm referansları — 2026-10-06

- 🟢 Personel özlük ekranında upload sonrası bir UI/tarih yenileme hatası yeni dosyayı silip DB'de bozuk başvuru bırakabiliyordu. Telafi artık yeni yolu ana kayıt, sürüm geçmişi ve ortak evrak kayıtlarında kontrol ediyor; başvuru varsa dosyayı koruyor, kontrol başarısızsa da silmiyor.
- 🟢 Başarılı yenilemede eski fiziksel dosya otomatik silinmiyor. Hızlı silme de ana kayıt temizlendikten sonra sürüm ve ortak evrak referanslarını kontrol ediyor; geçmişte kullanılan dosyanın silinmesini erteliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek upload/geri yükleme/silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup kuyruğu/yeniden deneme ve yetim dosya envanteri; ayrıca eski dosyaların kontrollü temizliği gerekiyor.

## A-10 devamı — araç/personel arşiv dosyalarının sürüm başına benzersizleştirilmesi — 2026-10-06

- 🟢 Arşiv servisi aynı araç/personel evrak tipi için sabit şifreli dosya adını tekrar kullanıyordu; yeni yükleme geçmiş sürüm içeriğinin üstüne yazabiliyordu. Her yeni arşiv yazımı artık GUID tabanlı benzersiz dosya yoluna kaydediliyor.
- 🟢 Araç upload telafisi artık DB kayıt işleminden sonra çalışan cache temizliği gibi bir adım hata verdiğinde taze context ile dosya yolu ve sürüm referanslarını denetliyor; başvurulan dosyayı silmiyor. Doğrulama/temizlik hatasında belirsizlik AggregateException ile korunuyor.
- 🟢 Personel arşiv yüklemesi kaynak dosya adı/türü/boyutunu kayıt metoduna iletiyor; dosya adı benzersiz saklama adıyla karışmıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek arşiv sürümleme testi yapılmadı.
- 🔴 **A-10 açık:** Mevcut sabit isimli arşivlerin geriye dönük taraması, cleanup kuyruğu/yeniden deneme ve yetim envanteri gereklidir.

## A-10 devamı — araç evrak upload telafisi ve silme referans kontrolü — 2026-10-06

- 🟢 Araç upload telafisi, yeni dosya DB’de veya sürüm tablosunda referanslıysa artık silmiyor. Cache invalidation gibi DB commit sonrası hata oluşsa da içerik korunuyor; referans denetimi başarısızsa AggregateException ile işlem belirsizliği bildiriliyor.
- 🟢 Araç evrak dosyası silme DB soft-delete sonrasında taze context ile diğer aktif dosya ve sürüm referanslarını kontrol ediyor; bilinen paylaşımlı yol fiziksel olarak kaldırılmıyor.
- 🟡 Referans kontrolü ile fiziksel silme halen tek bir DB transaction/lock içinde değil. Kalıcı serialized cleanup kuyruğu kurulana kadar A-10 kırmızı kalıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime eşzamanlı upload/restore testi yapılmadı.

## A-10 devamı — Şoför formu upload telafisi — 2026-10-06

- 🟢 Şoför formunda post-save içerik doğrulaması veya UI yenilemesi hata verdiğinde `catch` artık yeni dosyayı körlemesine silmiyor. Taze DB context ile özlük ana kayıtları, sürüm geçmişi ve ortak evrak yolları denetleniyor.
- 🟢 DB başvurusu varsa dosya korunuyor; referans sorgusu veya cleanup başarısızsa hata loglanıp belirsizlik AggregateException ile bildiriliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek personel upload senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup kuyruğu/yeniden deneme, yetim envanteri ve tüm upload/silme yollarının çalışma zamanı kabulü gereklidir.

## A-10 devamı — taşıma tedarikçisi eki upload telafisi ve referans kontrolü — 2026-10-06

- 🟢 Tedarikçi eki upload hatasında yeni şifreli dosya silinmeden önce taze context ile dosya yolu DB'de aranıyor. Belirsiz committe referans varsa dosya korunuyor; sorgu/temizlik hatası AggregateException ile görünür.
- 🟢 Tedarikçi eki silme DB soft-delete sonrasında diğer aktif eklerde aynı yol aranarak fiziksel temizlik yapıyor; ortak kullanılan dosya silinmiyor.
- 🟡 Kontrol ve fiziksel silme arasında çoklu süreç kilidi yok; A-10'un kalıcı cleanup kuyruğu/serialized işleyicisi hâlâ açık.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime upload/silme senaryosu çalıştırılmadı.

## A-10 devamı — destek talebi eki silme sırası — 2026-10-06

- 🟢 Destek talebi eki silmede önce DB soft-delete kaydediliyor. Sonra taze context ile başka aktif ek kaydının aynı dosya yoluna başvurup başvurmadığı kontrol ediliyor; fiziksel dosya yalnız başvuru yoksa siliniyor.
- 🟢 Fiziksel yolun destek yükleme kök dizini altında olduğu doğrulanıyor. Silme/konum hataları artık boş catch ile yutulmuyor; ek kimliği loglanıp `FileCleanupPendingException` ile bildiriliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Destek eki runtime silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup/yeniden deneme kuyruğu, yetim dosya envanteri ve diğer tüm dosya yazan servislerin incelemesi gereklidir.


## Kurulumda veritabanı seçimi — 2026-10-06

- 🟢 Ana (Setup.iss) ve müşteri (MusteriSetup.iss) Inno kurulumlarına PostgreSQL / SQLite / MSSQL seçim adımı eklendi. PostgreSQL bağlantı alanları; SQLite dosya yolu alınır ve seçime uygun dbsettings.json kurulumda yazılır. Güncelleme paketi mevcut ayarı korur.
- 🟢 IIS kur.ps1 -Mode Install kurulumunda da sağlayıcı sorulur; PostgreSQL parolası maskeli alınır, yapılandırma geçici dosyadan atomik taşınır. Kurulum modu mevcut SQLite dosyasını artık silmez.
- 🟡 **MSSQL açık:** Seçenek sihirbazda görünür, seçildiğinde neden ilerlenemediği bildirilir. DbInitializer.InitializeAsync yalnız PostgreSQL/SQLite desteklediğinden SQL Server otomatik migration/audit desteği eklenmeden MSSQL kurulumu tamamlanamaz; A-28 kırmızı kalır.
- 🟢 Güncel Web/DataSync publish ile ana, güncelleme ve müşteri EXE paketleri v1.0.37 üretildi. Ana IIS kurulumunda dbsettings.json okuması yöneticiler ve yalnız ilgili uygulama havuzuyla sınırlandı; SQLite App_Data yazma izni uygulama havuzuna verilir. 🟡 Etkileşimli hedef makine kurulumu ve gerçek DB bağlantı kabulü yapılmadı.

## A-15 devamı — aktif araç plakası DB tekilliği — 2026-10-06

- 🟢 Aktif ve silinmemiş `AracPlakalar` kayıtları için filtreli benzersiz indeks; model, snapshot ve migration ile eklendi. Migration öncesi yinelenen aktif plakalar denetleniyor ve varsa veri değiştirmeden duruyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Gerçek PostgreSQL/SQLite migration ve eşzamanlı kayıt kabulü yapılmadı. A-15'in banka referansı, dönem snapshot, varsayılan şablon ve kalan firma ilişkileri açık; ana son durum raporunda 🔴 olarak izlenir.

## A-15 devamı — banka importu eşzamanlı tekrar koruması — 2026-10-06

- 🟢 Referans numaralı import satırlarına firma/tarih/referans/tutar/yön bileşiminden deterministik hash ekleniyor; aktif kayıtlar firma kapsamında benzersiz indeksle korunuyor. Soft-delete kayıtlar yeniden importu engellemiyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Gerçek migration/eşzamanlı import kabulü ve geçmiş satırlar için geriye dönük anahtar doldurma yapılmadı. Dönem snapshot/varsayılan şablonlar A-15 içinde açık.

## A-15 devamı — aylık personel maaş snapshot tekilliği — 2026-10-06

- 🟢 Firma/yıl/ay/personel doğal anahtarında aktif maaş snapshotlarını benzersiz tutan filtreli indeks ve yinelenen kayıt ön kontrolü eklendi.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Gerçek migration/eşzamanlı yazım kabulü yapılmadı. Araç maliyet snapshotı soft-delete davranışı ve varsayılan şablon kısıtları açık.

## A-15 devamı — araç maliyet snapshotı yeniden üretim uyumu — 2026-10-06

- 🟢 Araç/yıl/ay tekil indeksi artık yalnız aktif snapshotlara uygulanıyor. Silinen dönem snapshotı yeniden üretilebilir; migration mevcut indeksi filtreli indeksle değiştirir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Migration ve silme/yeniden üretme runtime kabulü yapılmadı. Varsayılan fatura/grup şablonları A-15 içinde açık.

## A-15 devamı — varsayılan fatura ve grup şablonu tekilliği — 2026-10-06

- 🟢 Aktif fatura varsayılanı firma başına, grup şablonu varsayılanı ise firma geneli ve kullanıcı kapsamlarında ayrı filtreli benzersiz indekslerle korunuyor.
- 🟢 Migration öncesi yinelenen varsayılan kontrolü eklendi; çakışma varsa mevcut satırlar değiştirilmeden migration reddediliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Gerçek migration ve eşzamanlı şablon değiştirme kabulü yapılmadı; A-15’in diğer tenant ilişkileri açık.
- 🟢 Temiz kurulumda henüz migration edilmemiş fatura/grup şablonu tabloları için yinelenen veri ön kontrolü güvenli biçimde atlanıyor.

## A-15 devamı — banka hareketi hesap/cari firma kapsamı — 2026-10-06

- 🟢 Banka/Kasa hareketi servisinde oluşturma/güncelleme seçilen hesabı ve cariyi hareket firmasıyla aynı kapsamda doğruluyor; güncelleme mevcut kaydın firma kapsamını değiştiremiyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata.
- 🟡 Diğer doğrudan yazım yolları ve DB composite FK kapsamı tamamlanmadı; gerçek firma A/B kabulü yapılmadı.

## A-15 devamı — ortak SaveChanges ilişki denetimi — 2026-10-06

- 🟢 Banka/Kasa hareketi ekleyen/değiştiren EF yazımları hesap, cari ve personel geri ödeme hesabı firma eşleşmesini ortak SaveChanges sınırında denetler; aynı context'te yeni eklenen cari ve mevcut hareketin firma taşıması da kapsanır.
- 🟢 Web Debug derlemesi 0 uyarı/0 hata. Geçici SQLite bellekiçi doğrulamada yanlış firma hesabı, cari, yeni cari, geri ödeme hesabı ve firma taşıma reddedildi; aynı firma hareketi kaydedildi.
- 🟢 Audit `EntityId` iç düzeltmesi tenant sorgu filtresinden bağımsız yapılıyor; açık firma sağlayıcısı bulunmayan izole kayıtta iki yeni firma audit kimliği doğrulandı.
- 🟡 Raw SQL/dış yazımlar, DB composite FK, diğer tenant bağlantıları ve gerçek PostgreSQL/SQLite migration/kabul açık. A-15 🔴 kalır.

## A-15 devamı — banka hareketi veritabanı firma denetimi — 2026-10-06

- 🟢 PostgreSQL/SQLite migration'ı banka hareketinin hesap/cari/geri ödeme hesabı firma bağını tetikleyicilerle denetler; bağlı hesap veya carinin firma değiştirmesini de engeller. Ön kontrolde eski uyuşmazlık varsa veri değiştirmeden migration durur.
- 🟢 Web Debug derlemesi 0 uyarı/0 hata. 🟡 Gerçek DB migration, eski veri onarımı, diğer ilişkiler ve eşzamanlılık kabulü açık; A-15 🔴 kalır. Ayrıntı [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md).

## 🟢 A-22 kapanışı — yedi projede güncel NuGet taraması — 2026-10-06

- LisansDesktop'un geçişli native SQLite kütüphanesindeki yüksek önem dereceli bildirim saptandı; kütüphane 2.1.13'e yükseltildi ve dahili Release/win-x64 EXE yenilendi.
- Çözümdeki altı proje ve çözüm dışı Rent-a-Car kontrol projesinin doğrudan/geçişli son NuGet taramasında bilinen açık raporlanmadı. CI audit işi tarama komutu hatasında başarısız olacak ve iki proje kümesini de tarayacak şekilde düzenlendi. [Kanıt ve sınır](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md).
- A-22 🟢; gerçek müşteri paketinin kurulum ve güncelleme kabulü A-21 🟡 olarak izlenir.

## A-15 devamı — iki sağlayıcıda izole firma bağı denetimi — 2026-10-06

- 🟢 SQLite bellek DB ve geçici PostgreSQL 17 kümesinde migration ön kontrolü, geçerli yazım ve yedi ret senaryosu geçti. Hareketin aynı anda hesap/cari değiştirerek firma değiştirmesi de DB'de reddediliyor.
- 🟢 Web Debug derlemesi 0 uyarı/0 hata. [İzole kanıt ve sınır](A-15-IZOLE-FIRMA-BAGI-DOGRULAMA-2026-10-06.md). 🟡 Tam model/müşteri migration ve eşzamanlılık kabulü açık; A-15 🔴.

## A-07 devamı — kalıcı test altyapısı — 2026-10-06

- 🟢 `MKFiloServis.Tests` xUnit projesi çözüme eklendi. A-15 SQLite migration için 9 test Release olarak yerelde geçti. CI test projesini zorunlu derleyip çalıştırır, TRX sonucunu saklar.
- 🟡 GitHub CI sonucu ve diğer kritik lisans/tenant/audit/restore/mali testler açık; A-07 🟡. Ayrıntı [güncel görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md).

## A-07 devamı — lisans protokolü regresyonu — 2026-10-06

- 🟢 Ortak v3 modül zarfı, RSA-PSS modül bağı ve sürüm hakkı testleri kalıcı projeye eklendi; toplam Release sonucu **21/21 başarılı**.
- 🟡 Gerçek üretim imzası, yetkili müşteri geçişi ve modül UI/API kabulü yapılmadı. A-01/A-02/A-07 🟡 kalır; [güncel görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) izlenir.

## A-10 devamı — şifreli yüklemenin atomik tamamlanması — 2026-10-06

- 🟢 Şifreli upload artık son yola doğrudan yazılmaz; aynı klasörde tamamlanan geçici dosya son ada taşınır. İptalde/hata durumunda geçici dosyanın temizlenmesi denenir.
- 🟢 Başarılı upload ve önceden iptal testleri geçti; Release test toplamı **23/23**.
- 🔴 Kalıcı temizlik/yeniden deneme kuyruğu, yetim envanteri ve diğer servislerin kapsamı açık; A-10 kırmızı kalır.

## A-10 devamı — özlük dosyası referans koruması — 2026-10-06

- 🟢 Personel özlükte dosya veya sürüm satırı kaldırılınca, fiziksel dosyayı silmeden önce mevcut ve geçmiş özlük yolları kontrol edilir; ortak eski yol kullanılıyorsa dosya tutulur.
- 🟢 Release build ve mevcut test paketi başarılı (**23/23**); özel eski paylaşım senaryosu ayrıca DB entegrasyon testine alınmadı.
- 🔴 Çok süreçli yarış, kalıcı temizleme kuyruğu ve diğer varlık yollarının envanteri açık.

## A-10 devamı — yerel nesne deposu — 2026-10-06

- 🟢 Depo anahtarları kök dışına çıkamaz; yerel upload geçici dosyadan atomik yayımlanır ve silme `File.Exists` kaynaklı hata gizlemesini yapmaz.
- 🟢 İki yeni regresyonla Release test paketi **25/25 geçti**.
- 🔴 Sembolik bağlantılar, kalıcı cleanup kuyruğu ve gerçek storage hata kabulü açık.

## A-10 devamı — depolama sembolik bağlantı denetimi — 2026-10-06

- 🟢 Ortak `StorageFilePath` çözümleyicisi ve yerel nesne deposu mevcut sembolik bağlantı bileşenlerinden geçen yolları reddedecek şekilde güncellendi.
- 🟢 Release test paketi **25/25 başarılı**; `git diff --check` temiz.
- 🟡 Windows test kullanıcısının sembolik bağlantı oluşturma ayrıcalığı bulunmadığından saldırı senaryosu çalıştırılamadı. Kontrol ile dosya işlemi arasındaki TOCTOU, kalıcı temizleme kuyruğu, yetim envanteri ve gerçek storage kabulü açık; A-10 🔴 kalır.

## A-10 devamı — eksik üst klasörlü yerel dosya silme — 2026-10-06

- 🟢 Yerel nesne deposu, silinecek anahtarın üst klasörü yoksa bu durumu eksik dosya olarak kabul eder. Erişim veya diğer IO hataları yine çağırana iletilir.
- 🟢 Yeni regresyon testi eklendi; Release paketi **26/26 başarılı**, `git diff --check` temiz.
- 🔴 Kalıcı hata kuyruğu/yeniden deneme, çok süreçli yarış/TOCTOU, yetim envanteri ve gerçek storage hata kabulü açık. A-10 kapanmadı.

## A-10 devamı — ortak silme davranışını şifreli depoya uygulama — 2026-10-06

- 🟢 Eksik üst klasörü güvenle ele alan `StorageFilePath.DeleteIdempotently`, hem `SecureFileService` hem `LocalObjectStorageService` tarafından kullanılıyor. Var olmayan üst klasörde başarılı no-op; erişim/IO hatalarında hata iletimi korunuyor.
- 🟢 Release test paketi **26/26 başarılı**; `git diff --check` temiz.
- 🔴 Şifreli depo için ayrı servis entegrasyonu testi, kalıcı retry kuyruğu, TOCTOU ve çok süreçli yarış, yetim envanteri ve gerçek storage hata kabulü açık; A-10 kırmızı kalır.

## A-10 legacy açık dosya migration sırası — 2026-10-06

- 🟢 Eski wwwroot upload dosyası artık yeni şifreli yol DB'ye kaydedilip taze bağlamda doğrulanmadan silinmiyor. Commit hatası/sonucu belirsizse eski dosya korunuyor; aynı eski yol başka bir DB kaydında kullanılıyorsa silinmiyor.
- 🟢 Legacy yol çözümlemesi uploads köküne hapsedildi ve mevcut sembolik bağlantı bileşenleri reddediliyor. Release testleri 26/26 geçti; `git diff --check` temiz.
- 🔴 Eski açık dosya temizliğini uygulama yeniden başlasa da sürdüren kalıcı kuyruk yok; gerçek eski DB/dosya üzerinde migration kabulü, orphan taraması ve TOCTOU yarış önlemi açık.

- 🟢 Eski yolun paylaşılmış olup olmadığı kontrolü şifreli dosya tablolarının yanında ortak evrak, özlük, destek, tedarikçi eki ve fatura/proforma dosya alanlarını da `IgnoreQueryFilters()` ile tarar.

## A-10 dosya migration aracının erişim sınırı ve kapsamı — 2026-10-06

- 🟢 Migration ekranı artık hem `Admin` rolü hem mevcut EBYS lisans politikasını ister.
- 🟢 UI/servis açıklaması kapsamı açık söyler: aktif firma filtresindeki EBYS ve araç ana/sürüm kayıtları. “Tüm düz metin dosyaları” iddiası kaldırıldı. Release test paketi 26/26 geçti, diff kontrolü temiz.
- 🔴 Soft-delete kayıtları ve diğer dosya türlerinin envanteri/migration'ı, gerçek rol/tenant kabulü, kalıcı cleanup kuyruğu ve orphan taraması açık.

## A-10 referanssız şifreli dosya envanteri — 2026-10-06

- 🟢 Admin evrak bakım raporu artık uploads, Arsiv ve Depo altındaki `.enc` dosyalarını DB'deki bilinen dosya yolu kolonlarıyla karşılaştırıp referanssız dosyaları listeler. Soft-delete kayıtları referans sayımına dahildir.
- 🟢 Rapor salt okunurdur; otomatik silme yapmaz. Scanner regresyon testi dahil Release test paketi **27/27 başarılı**, `git diff --check` temiz.
- 🔴 Envanter eski açık dosyaları ve listelenen kolonlar dışındaki dosya alanlarını kapsamıyor; gerçek DB ile müşteri kabulü, yarış güvenliği ve kalıcı cleanup/retry açık. A-10 kapanmadı.

## A-10 güncel durum — geri alınabilir dosyaların korunması — 2026-10-06

- 🟢 Kullanıcı kararıyla soft-delete kayıtlarının fiziksel dosyaları geri alma için korunuyor. `SecureFileService` doğrudan ve worker üzerinden gelen silme isteklerinde DB referansını kontrol eder; dosya hâlâ referanslıysa journal isteğini kapatır.
- 🟢 Tam model SQLite testi doğrudan çağrı, worker çağrısı ve referanssız dosyanın silinmesini doğruladı. DB referans sorgusu arızasında dosya ve journal isteği korunuyor. Release paketi **31/31 başarılı**.
- 🔴 Gerçek müşteri DB/deposu kabulü, commit-journal crash penceresi, referans sorgusu-unlink yarışı ve eski dosya alanlarının tam kapsamı açık; A-10 tamamlandı olarak işaretlenmedi. Ayrıntılı güncel liste: `SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md`.

- 🟢 Devam düzeltmesi: ortak evrak ve destek eki soft-delete akışlarında eski düz dosyanın doğrudan fiziksel silinmesi kaldırıldı; geri alma dosyayı korur.
- 🟢 Ortak evrak yeni dosyaları şifreli depoya yazar; geçmiş düz dosyalar görüntüleme/indirmede ve bakım ekranında desteklenir. Web Release derlemesi 0 uyarı/0 hata.
- 🟡 Geçmiş düz dosyaların toplu şifreli geçişi ve müşteri kabulü açık.

- 🟢 Yönetici geçiş aracı artık aktif ortak evrakın eski düz dosyalarını da önizleyip şifreli depoya taşır; yeni DB yolu doğrulanır, başka bir silinmiş kayıt eski yolu kullanıyorsa dosya korunur. Web Release derlemesi 0 uyarı/0 hata.
- 🔴 Silinmiş kayıtların kendi geçişi ve gerçek veriyle çalışma zamanı kabulü açık.

- 🟢 Geçiş ekranı tek aktif firma ister; ortak evrak bağlı personel/araç firma kimliğiyle sınırlandırılır, sembolik bağlantı içeren eski dosya yolu reddedilir.

- 🟢 Silinmiş ortak evrakın eski düz dosyası da aynı firma bağıyla şifreli yola taşınır; önizleme aktif ve silinmiş sayıları ayrı gösterir. Web Release derlemesi 0 uyarı/0 hata.
- 🔴 Bağı kopuk eski kayıtlar ve EBYS/araç silinmiş geçmişi o tarihte açıktı; aşağıdaki sonraki değişiklikle geçiş kapsamına alındı. Gerçek veri kabulü açık.

- 🟢 Araç ana dosya ve sürüm geçişi açık `FirmaId` filtresi kullanır. EBYS kayıtlarının firma bağı olmadığı ekran metninde belirtilir; EBYS sahipliği açık kalır.

- 🟢 Şifreli geçişte yeni dosya DB yolu değiştirilmeden geri okunup kaynakla byte düzeyinde karşılaştırılır; doğrulanmayan kopya temizlenmeye çalışılır, eski dosya korunur. Web Release derlemesi 0 uyarı/0 hata; gerçek veri kabulü açık.

- 🟢 Yeni yol DB'de kayıtlıyken eski dosya temizlenemezse ekran `Temizlik Bekliyor` gösterir. Bakım raporu eski ortak evrak ve wwwroot yüklemelerindeki referanssız düz dosya adaylarını da listeler; otomatik silmez.
- 🔴 Bu satırdaki eski köklerde kalıcı retry eksikliği aşağıdaki sonraki düzeltmeyle giderildi; gerçek depolama kabulü açık.

### A-10 eski düz dosya temizliğinde kalıcı yeniden deneme — 2026-10-06

- 🟢 Dosya geçişi yeni şifreli yolu DB'ye yazdıktan sonra eski ortak evrak ve `wwwroot/uploads` temizleme isteğini DP korumalı günlüğe alır. Worker, başarısız işlemi tekrar dener ve her seferinde aktif/silinmiş DB referanslarını kontrol eder; geri alma için kullanılan dosyayı silmez.
- 🟢 Web Release derlemesi 0 uyarı/0 hata. Bu ek için çalışma zamanı testi yapılmadı.
- 🔴 Bu satırdaki eski dosya geçişi commit-kuyruk kesintisi aşağıdaki sonraki düzeltmeyle giderildi. Referans sorgusu ile silme yarışı, büyük DB tarama süresi ve müşteri DB/depo kabulü açık. A-10 🔴 kalır; güncel görev kaydı `SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md` dosyasındadır.

### A-10 geçiş temizliği için journal önceliği — 2026-10-06

- 🟢 Yeni şifreli kopya doğrulandıktan sonra eski/yeni yol çifti kalıcı journal'a DB yol değişiminden önce yazılır. Worker yeni yol DB'de doğrulanana kadar dosyayı koruyup isteği erteler; commit sonrası yeniden değerlendirir. Önceki v1 istekleri okunabilir.
- 🟢 Web Release derlemesi 0 uyarı/0 hata. Son değişiklik için çalışma zamanı testi yapılmadı.
- 🔴 Başarısız DB yazımından kalan güvenli bekleme girdileri, diğer silme akışlarının commit-kuyruk aralığı, sorgu-silme yarışı ve müşteri kabulü açık. A-10 🔴.

### A-10 geri alınabilir EBYS ve araç belge geçmişi — 2026-10-06

- 🟢 Yönetici geçişi soft-delete filtrelerini yok sayarak EBYS ana/sürüm dosyalarını taşır. Araç ana/sürüm dosyaları da silinmiş satırlar dahil bulunur; açık firma filtresi dosya kaydını ve bağlı araç/evrakı birlikte doğrular.
- 🟢 Önizleme aktif ve silinmiş kayıtları ayrı sayar; ekran metni EBYS'nin yönetici genel kapsamını belirtir. Web Release derlemesi 0 uyarı/0 hata; müşteri verisiyle geçiş testi yapılmadı.
- 🔴 EBYS kayıtlarının firma sahipliği, bozuk/bağı kopuk eski araç satırlarının onarımı, büyük veri performansı, diğer belge alanları ve gerçek müşteri kabulü açık; A-10 🔴.

### A-10 personel özlük ve fatura dosyalarını ekleme — 2026-10-06

- 🟢 Seçili firmadaki personel özlük dosyaları ve sürümleri ile fatura PDF/XML ekleri geçişe eklendi. Silinmiş kayıtlar da taşınabilir; personel sürücüsü ve fatura `FirmaId` bağı doğrulanır.
- 🟢 Legacy upload yollarında baştaki/ters eğik çizgi biçimleri ve personel ortak yükleme kökünde eski tek dosya adı destekleniyor. Ekran modül bazında gruplanmış adetleri gösteriyor. Web Release derlemesi 0 uyarı/0 hata; gerçek veri geçişi yapılmadı.
- 🔴 Destek/tedarikçi dosya sahipliği ve fiziksel kök eşlemesi, EBYS firma sahipliği, diğer dosya yazım/silme akışları, büyük veri performansı ve müşteri kabulü açık. A-10 🔴.

### A-10 migration kaynak kökü doğrulaması — 2026-10-06

Fatura eski PDF/XML dosyalarının uygulamadaki gerçek okuyucusu `AppStoragePaths.GetUploadsRoot(...)` kullandığı için geçiş ve kalıcı cleanup journal'ı bu sabit depolama köküne bağlandı. EBYS/araç `wwwroot/uploads`, özlük tek dosya adları ortak dosya servisi kökü altında ayrı işlendi. Release derlemesi 0 uyarı/0 hata. Çalışma zamanı/müşteri kabulü yapılmadı; destek/tedarikçi sahiplik-kök eşlemesi, EBYS firma bağı ve diğer A-10 kabul maddeleri açık.

### A-10 tedarikçi dosyalarında tenant doğrulaması — 2026-10-06

Tedarikçi eski ekleri için `TedarikciEvrakDosya → TedarikciEvrak → TasimaTedarikci → Cari → Cari.FirmaId` ilişki zinciriyle aktif firma sınırı eklendi; `IgnoreQueryFilters` sayesinde geri alınabilir soft-delete ekler de kapsama giriyor. Eski tekil dosya adları sadece ortak yükleme kökünde aranıyor. UI sayaçları eklendi; Web Release derlemesi 0 uyarı/0 hata. Destek eki kaydında firma bağı kesin değilse ve eski mutlak dosya yolu kökü ayrıca doğrulanamıyorsa kayıtlar atlanıyor. Gerçek müşteri kabulü yok, A-10 açık.

### A-10 destek eki tenant ve yol sınırı — 2026-10-06

Destek taleplerinin doğrudan firma kolonu bulunmadığından migrasyon yalnızca `CariId → Cari.FirmaId` bağı seçili firmayı doğruladığında ilerler; ticket/yanıt ekleri soft-delete filtreleri yok sayılarak bulunur. Legacy mutlak yollar `wwwroot/uploads/destek` dizini içinde canonical path kontrolünden geçer ve cleanup journal ayrı bir destek kök anahtarı kullanır. Cari bağı olmayan ve destek klasörü dışındaki yollar taşınmaz. Release derlemesi 0 uyarı/0 hata; gerçek müşteri/runtime kabulü yapılmadı, A-10 açık.

### A-10 personel hard-delete temizliğini commit öncesi günlüğe alma — 2026-10-06

Personel özlük dosya yolu değişikliği ve sürüm satırı silme akışında kalıcı cleanup isteği DB `SaveChanges` çağrısından önce yazılır. Journal girdisindeki `WaitForReferenceRemoval` worker'a DB yolu halen görünüyorsa isteği tamamlamak yerine ertelemesini söyler; satır sonradan kaldırılırsa tekrar deneyip fiziksel temizliği yapar. Hâlâ başvurulan/soft-delete geçmişte tutulan dosyalar korunur. Release derlemesi 0 uyarı/0 hata; runtime testi yapılmadı; diğer modüllerin commit-journal aralığı açık.

### A-10 EBYS eski dosya yolunu DB değişiminden önce günlüğe alma — 2026-10-06

`EbysEvrakService.DosyaGuncelleAsync` yeni şifreli kopyayı hazırladıktan sonra eski yol için kalıcı bekleyen-temizleme girdisi oluşturur; DB yolu ancak sonra kaydedilir. Worker eski yol DB'de referanslıyken girdiyi silip kaybetmez; erteler. SaveChanges istisnasında yeniden okunmuş güncel DB yolu, eski/yeni kopyanın güvenli telafisini belirler. Release build 0 uyarı/0 hata; runtime/müşteri kabulü yok. Personel özlük ve EBYS dışında kalan path-update akışları açık.

### 2026-10-06 — A-10 fatura PDF/XML değiştirmede eski dosyayı güvenli temizleme

- 🟢 Fatura PDF ve XML değişiminde eski yol, DB `SaveChanges` öncesi kalıcı temizleme günlüğüne eklenir. Şifreli yollar DB referansı kalkana kadar yeniden denenir; legacy `/uploads/...` dosyaları doğru storage köküyle, yeni DB yolu doğrulandıktan sonra işlenir.
- 🟢 DB yazımı başarısız/belirsizse mevcut fatura yolu yeniden okunur. Eski yol hâlâ kayıtlıysa yeni dosya telafi edilir ve eski yolun isteği kaldırılır; yeni yol commit olduysa eski temizleme isteği korunur. Web Release derlemesi **0 uyarı / 0 hata**; çalışma zamanı/fatura verisiyle doğrulama yapılmadı.
- 🔴 A-10 kırmızı kalır: diğer dosya yolu değiştirme/silme akışlarının tamamı, gerçek müşteri DB/storage worker kabulü, referans sorgusu-unlink yarışı ve büyük envanter performansı açıktır.

### 2026-10-06 — A-10 yönetici arşiv geçişinde belirsiz commit telafisi

- 🟢 Yönetici arşiv geçişinde personel/araç dosyasının kopyalanması veya DB transaction sonucu hata verirse, yeni dosya silinmeden önce taze DB bağlamında hedef satırın yolu doğrulanır. DB yeni yolu gösteriyorsa işlem `Copied` sayılır; eski dosya kopya-güvenli strateji gereği korunur.
- 🟢 DB hedef yolu göstermiyorsa yeni kopya temizlenir. Doğrulama hatasında şifreli dosya silme servisi kalıcı cleanup günlüğüne başvuruyu ekler; referans kontrolü yapılamazsa dosya korunur ve worker tekrar dener. Web Release derlemesi **0 uyarı / 0 hata**; yönetici ekranında gerçek geçiş/commit hata enjeksiyonu yapılmadı.
- 🔴 A-10 kırmızı kalır: diğer dosya akışlarının tam envanteri, gerçek DB/storage worker kabulü, referans kontrolü-unlink yarışı ve büyük envanter performansı açık.

### 2026-10-06 — A-10 personel/araç arşiv dosyalarının atomik yazımı

- 🟢 `EvrakArsivService` şifreli personel/araç arşivini benzersiz geçici `.tmp.enc` dosyasına yazar ve tamamlanınca aynı klasörde son `.enc` adına taşır. İptal/yazım hatasında geçici dosya temizlenmeye çalışılır; tamamlanmamış içerik DB’ye döndürülen yol olarak yayımlanmaz.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Gerçek disk kesintisi/iptal çalıştırma testi yapılmadı.
- 🔴 A-10 kırmızı kalır: arşiv kopyası ile DB kaydı arasındaki süreç kesintisi yetim bırakabilir (yetim envanteriyle görünür), bütün dosya akışlarının runtime kabulü, TOCTOU ve büyük envanter performansı açık.

### 2026-10-06 — A-10 e-Fatura/Luca dosyalarında yeni yazımları şifreleme

- 🟢 GİB e-Fatura XML üretimi artık `wwwroot/efatura` yerine atomik şifreli depoya yazar; XML okuma şifreli yolu açar ve eski e-Fatura webroot yollarını sınırlandırılmış geriye dönük okuma olarak destekler. Luca’dan gelen yeni XML/PDF dosyaları da `wwwroot/belgeler/efatura` yerine şifreli depoya yazılır; kaydetme hatasında DB referansı denetlenerek cleanup kuyruğu kullanılır.
- 🟢 Fatura indirme yolu eski `/belgeler/efatura/...` ve `efatura/...` kayıtlarını güvenli `wwwroot` çözümlemesiyle okuyabilir. Yönetici dosya geçişi artık bu iki eski webroot yol ailesini de önizler, şifreli kopyayı doğrular, DB yolunu günceller ve yeni yol doğrulanınca referanssız eski açık dosya için dayanıklı temizlik isteği oluşturur. Web Release derlemesi **0 uyarı / 0 hata**; gerçek Luca/GİB bağlantısı veya eski müşteri dosyası geçişi çalıştırılmadı.
- 🔴 Var olan müşteri webroot dosyaları otomatik toplu taşınmadı; yönetici geçişi seçili firma ile çalıştırılmalı. ETTN/fatura ilişkilendirme ve canlı entegrasyon kabulü, hata enjeksiyonu, TOCTOU ve büyük veri performansı açık; A-10 🔴 kalır.

### 2026-10-07 — A-10 kalan yaşam döngüsü açıklarının kök düzeltmeleri

- 🟢 Eski düz e-Fatura/Luca çıktıları artık anonim statik dosya olarak sunulmaz; yeni GİB XML ve Luca XML/PDF üretimleri şifreli depodadır. Eski `wwwroot/efatura` ve `wwwroot/belgeler/efatura` yolları seçili firma migrasyonuna alındı; legacy `FileService` düz metin yazma ve doğrudan silme API'leri kaldırılıp salt-okunur yapıldı.
- 🟢 Dosya migrasyonu DB commit'inin gerçekleşmediğini taze bağlamla doğrularsa spekülatif şifreli kopyayı güvenli temizleme servisine verir ve eski-yeni yol bekleme isteğini tamamlar; böylece hiçbir zaman yazılmayacak yeni yolu bekleyen kalıcı kuyruk birikmez. Eski anahtar dosya kurtarması orijinali ezmeden geçici şifreli kopya üretip atomik değiştirir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` temiz. Bu turda test veya canlı müşteri geçişi çalıştırılmadı.
- 🔴 A-10 henüz kapanmaz: mevcut müşteri legacy dosyalarının firma bazlı geçişi/restore kabulü, aynı dosya yoluna eşzamanlı yeni DB referansı ile fiziksel silme arasındaki TOCTOU yarışı ve büyük DB referans taraması yükü saha kanıtı ister. Müşteri geçişi yapılmadan legacy açık dosyalar diskte korunur.

### 2026-10-07 — A-10 personel evrak sürümleme ve kaldırma tutarlılığı

- 🟢 Yeniden yüklemede eski dosya yolu ve metadata doğru sürüm satırında korunuyor. Hızlı kaldırma DB’de aktif dosya başvurusunu temizliyor ve eski dosyayı geri alınabilir sürüm geçmişine bağlı tutuyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; canlı personel verisiyle geri alma kabulü yapılmadı.
- 🔴 A-10 açık: müşteri DB/storage geçiş/restore kabulü, genel TOCTOU, büyük DB tarama maliyeti ve kalan modül silme akışları.

### 2026-10-07 — A-10 soft-delete dosyalarının geri alınabilir tutulması

- 🟢 Araç evrakı, tedarikçi eki ve EBYS dosyası soft-delete sırasında diskten silinmiyor. Geri alınabilir DB satırının dosyası da restore için korunuyor; kalıcı purge ayrı operasyon olmalı.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Canlı DB’de restore kabulü yapılmadı.
- 🔴 A-10 açık: müşteri geçiş/restore kabulü, eşzamanlı referans-silme yarışı, büyük DB tarama maliyeti ve kalan kalıcı silme akışları.

### 2026-10-07 — A-10 dosya yolu referans kontrolü ve indeksleme

- 🟢 12 dosya yolu sütunu indekslendi. Silme başına bütün dosya yolları uygulama belleğine taşınmıyor; DB sorgusu aliasları ve Windows path normalizasyonunu kontrol ediyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test çalıştırılmadı.
- 🔴 A-10 açık: hedef hacim migration/performans kabulü, çoklu sunucu silme yarışı, müşteri dosyası geçiş/restore kabulü ve firma bağı olmayan legacy destek ekleri.

### 2026-10-07 — A-10 personel dosya yolu değişikliğini amaca özel hale getirme

- 🟢 Genel personel evrak güncellemesi artık dosya yolunu detached form verisinden yazamaz. Bozuk dosya başvurusunu temizleme ayrı, kimlik ile sınırlı DB servisine taşındı.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🔴 A-10 müşteri restore/geçiş ve hedef DB hacim performans kabulünü bekliyor.

### 2026-10-07 — A-10 izole restore ve çoklu worker kanıtı

- 🟢 Release SQLite kabulinde soft-delete dosyası cleanup sonrası kaldı; kayıt geri açılınca DB referansı ve fiziksel dosya varlığı korundu.
- 🟢 Bağımsız iki cleanup worker aynı journal girdisini birlikte claim edemedi. Release test paketi **32/32 başarılı**.
- 🟡 Bu, aynı host paylaşımlı dizin testidir; müşteri DB/storage geçişi, hedef hacim ölçümü ve iki ayrı sunucu/SMB üzerinde kabul henüz yapılmamıştır. Saha verisi kullanılmadı.
### 2026-10-07 — A-10 kuyruk sahipliği ve geçiş güvenliği

- 🟢 Worker tamamlaması lease/revizyon kontrolüne bağlandı; eski işlem yeniden kuyruğa alınan veya bekleme türü değişen isteği kaldıramaz. Doğrudan silme, DB başvurusunun kalkmasını bekleyen isteği korur.
- 🟢 Legacy kaynak silinmeden önce yeni şifreli dosya tekrar çözülüp SHA-256 ile karşılaştırılır. Eksik/uyuşmayan hedefte eski dosya ve kuyruk korunur. Referans sorgusunda boşluk, ters ayraç ve Türkçe harf koruması tamamlandı; tam yol listesi belleğe alınmaz.
- 🟢 Tam model SQLite ile gerçek geçiş servisi, soft-delete geri alma ve tekrar çalıştırma kabulü geçti. 100.000 sentetik satırda yerel sorgu ölçümleri 2,7 / 28,0 / 37,5 ms; Release paketi **34/34 başarılı**.
- 🔴 Bu kanıt genel referans ekleme–silme yarışını, tüm firma sahipliği açıklarını veya müşteri/çoklu sunucu kabulünü kapatmaz. Güncel kapsam ve ölçüm sınırları [görev envanterinin son ekinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) kayıtlıdır.
### 2026-10-07 — A-24/A-25 süreçler arası cache nesli

- 🟢 CacheService ortak depodaki nesil belirteciyle çalışıyor. Geçersizleştirme, başka servisin başlattığı eski factory sonucunun yeniden görünmesini engeller; süreç içi anahtar listesine bağımlılık kaldırıldı. Prefix temizliği tüm uygulama cache'ini geçersizleştirdiği için DB yükü artabilir.
- 🟢 İptal yutulmaz; okuma arızasında veri kaynağı kullanılır, invalidation arızası çağırana bildirilir. Dört yeni cache regresyonuyla Release paketi **38/38 başarılı**.
- 🔴 Gerçek Redis/çok süreçli yük, diğer araç yazımları ve backend kesintisinde kalıcı invalidation retry kapsamı açık. Docker daemon erişilemedi. Detaylar [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md); A-24 🔴 ve A-25 🟡.


### 2026-10-08 — A-29 fatura servisinde güncel yetki denetimi

- 🟢 `CurrentPermissionGuard` servis sınırında oturum kimliğini HTTP/circuit üzerinden alır; etkin kullanıcı, güncel rol ve izinleri her çağrıda DB'den okur. Oturumsuz, pasif/silinmiş hesap veya silinmiş rol/izin ile yazım reddedilir; DB sorgu hatası yazımı durdurur. Oturumdaki Admin rol iddiası yetki kaynağı değildir.
- 🟢 `FaturaService` oluşturma, düzenleme, silme, Excel/XML/XML+PDF içe aktarma, PDF değiştirme, kalem güncelleme, stok kartlı kalem güncelleme, fatura eşleştirme, mahsup kapatma ve açık muhasebeleştirme girişlerinde güncel izin aranır. Genel fatura izni veya ilgili gelen/kesilen yön izni kabul edilir. Yön değiştirmede hem eski hem yeni yön, eşleştirme/mahsupta iki kayıt da denetlenir; kalemlerde izin DB'deki ana faturadan alınır.
- 🟢 XML+PDF içe aktarmada yeni faturanın PDF'i yazma izniyle eklenir; mevcut faturanın PDF'ini değiştiren dış servis çağrısı düzenleme izni ister. Açık muhasebeleştirme ayrıca `MuhasebeFisleriYaz` ister. `MuhasebeService.CreateFisAsync` doğrudan fiş oluşturmayı aynı güncel servis denetimiyle korur.
- 🟡 Türetilmiş ödeme toplamı (`UpdateOdenenTutarAsync`), otomatik/atomik muhasebe üretimi, hesap planı ve iç servislerin fiş düzenleme/silme çağrıları henüz bu ortak denetime taşınmadı. Bunların banka/masraf/hakediş iş yetkileriyle birlikte ele alınması gerekir; yalnız manuel muhasebe izni eklenerek bu iş akışları kapatılmış sayılmadı. Puantaj ve diğer mali servis/API girişleri de açık; A-29 sarı kalır.
- 🟡 Bu değişiklik için çalışma zamanı rol kaldırma veya müşteri kabul testi çalıştırılmadı. Önceki 42/42 sonucu bu yeni servis değişikliğinin test kanıtı olarak kullanılmaz.
- 🟢 Son kodla Web Release derlemesi başarılı: **0 uyarı / 0 hata**. İlgili değişikliklerde `git diff --check` temiz. Bu sonuç çalışma zamanı kabulü değildir.


### 2026-10-08 — A-29 muhasebe düzenleme/silme ve masraf servisleri

- 🟢 Muhasebe fişi düzenleme, silme, onaylama ve onayı geri alma servis girişleri artık güncel DB izni arar. Düzenleme/onay işlemleri `MuhasebeFisleriDuzenle`, silme `MuhasebeFisleriSil` gerektirir. Hesap planı düzenleme/silme de kendi izinleriyle korunur.
- 🟢 Araç masrafı oluşturma/düzenleme/silme servis girişleri `AracMasraflariYaz/Duzenle/Sil` izni arar. Muhasebe fişi üretimi seçilmişse ayrıca `MuhasebeFisleriYaz`, bağlı fiş güncellenecekse `MuhasebeFisleriDuzenle`, bağlı fiş kaldırılacaksa `MuhasebeFisleriSil` gerekir. Oluşturma/düzenlemede bu izinler ilk masraf yazımından önce kontrol edilir; fiş düzenleme/silme servisi de izni yeniden denetler. Mevcut rollerde bu işlemleri yapacak kullanıcılara ilgili muhasebe izinleri verilmelidir.
- 🟢 Kolay muhasebe kaydetme servisi cari oluşturma dahil ilk yazımdan önce `MuhasebeFisleriYaz` iznini doğrular. Geri alma akışı korunan fiş silme servisini kullanır.
- 🟢 Manuel fiş oluşturma, fiş düzenleme ve `CreateFisAtomicAsync` ile fiş üretme borç/alacak toplamlarını kalemlerden yeniden hesaplar; fark 0,01'den büyükse kayıt reddedilir. Düzenlemede kontrol eski kalemler kaldırılmadan önce, atomik girişte fiş numarası üretilmeden önce yapılır.
- 🟡 Kalan kapsam: hesap planı oluşturmanın iç servis sözleşmesi, doğrudan atomik/otomatik muhasebe çağrılarının tümü, türetilmiş fatura ödeme toplamı, puantaj ve diğer mali servis/API girişleri. Çok servisli masraf/banka/muhasebe işlemleri tek transaction altında değildir; izin değişimi veya sonraki servis hatası nedeniyle oluşabilecek kısmi kayıt riski bu izin kontrolleriyle bütünüyle kapanmaz. A-29 sarı kalır.
- 🟡 Çalışma zamanı veya müşteri kabul testi bu değişiklik için çalıştırılmadı.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata** ile tamamlandı.


### 2026-10-08 — A-29 toplu muhasebe geri alma ve fiş bütünlüğü

- 🟢 `TopluGeriAlAsync` artık yalnız taslak fişi geri alır; onaylı veya iptal edilmiş fiş işlem görmeden reddedilir. Fiş ve kalemlerin fiziksel silinmesi kaldırıldı: taslak fiş soft-delete olur, kalemler inceleme/audit için korunur. Fatura/masraf bağlantısının kaldırılması ve fişin silinmiş işaretlenmesi aynı `SaveChanges` çağrısındadır.
- 🟢 Toplu fatura/masraf muhasebeleştirme `MuhasebeFisleriYaz`, toplu geri alma `MuhasebeFisleriSil` iznini hem başlangıçta hem her satır öncesinde DB'den denetler. İzin sorgusu hata verirse veya izin kaldırılmışsa döngü durur; daha önce başarıyla kaydedilmiş satırlar geri alınmış sayılmaz.
- 🟢 Üç toplu akışta başarısız satır sonrası EF tracking temizlenir. Böylece başarısız satırın bekleyen değişiklikleri sonraki satırın `SaveChanges` çağrısına taşınmaz. Bu, daha önce başka context'te commit olmuş işlemleri geri almaz; çok servisli transaction kapsamı A-09'da açıktır.
- 🟢 Otomatik fişlerin ortak özel kayıt yardımcısı da kalemlerden borç/alacak dengesini doğrular; fiş onaylama kaydı kalemleriyle yükler ve dengesiz fişi onaylamaz.
- 🟡 A-29'un doğrudan atomik/otomatik muhasebe servis yetkileri, hesap oluşturma/import, puantaj ve diğer mali girişleri ile çalışma zamanı doğrulaması halen açık. Bu değişiklik için test veya gerçek müşteri kabulü çalıştırılmadı; görev renkleri **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz** olarak korunur.
- 🟢 Web Release derlemesi: **0 uyarı / 0 hata**.


### 2026-10-08 — Test sırası ve hesap/cari/stok servis girişleri

- 🟢 Kullanıcı kararıyla test çalıştırmaları kod düzeltmeleri sonrasındaki son aşamaya taşındı. [Son aşama test planı](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) uygulama sırasını ve nihai kapsamı tanımlar.
- 🟢 Ekrandan varsayılan hesap planı oluşturma `HesapPlaniYaz` izni arar. Başlangıç hazırlığı, kullanıcı servis arayüzünde bulunmayan iç metoda taşındı; oturumsuz çağrı için ortak yetki atlama eklenmedi. DI'da somut servis ve arayüz aynı scoped örneğe bağlandı.
- 🟢 Hesap planı Excel yükleme hem ekleme hem güncelleme yapabildiği için `HesapPlaniYaz` ve `HesapPlaniDuzenle` izinlerini girişte ve kaydetmeden hemen önce denetler.
- 🟢 Kolay muhasebenin hızlı cari oluşturma girişleri `CarilerYaz`, hızlı stok oluşturma `StokKartlariYaz` izni ister. Aynı unvanlı mevcut cari aramasında firma filtresini kaldıran `IgnoreQueryFilters` kaldırıldı; global kod çakışması kontrolü ayrı kaldı.
- 🟡 Bu tur derleme/test çalıştırılmadı; yeni kodun nihai doğrulaması son aşama planındadır. A-29 diğer mali girişler, A-15 tüm ilişkiler ve A-07 test/CI kapsamı henüz kapanmadı. Görev renkleri değişmedi.


### 2026-10-08 — A-29 personel finans servis yazımları

- 🟢 `PersonelFinansService` avans/borç oluşturma, düzenleme, silme/iptal, mahsup, maaşa avans mahsup, borç ödeme, ödeme/mahsup kaldırma, toplu mahsup/ödeme ve ayar kaydetme girişlerinde güncel DB izni denetler. Oluşturma `MaasYaz`, düzenleme/ödeme/mahsup/ayar `MaasDuzenle`, avans ve ödeme/mahsup silme `MaasSil`; kalıcı borç silme `PersonelBorcSil` gerektirir. Toplu işlerin her gerçek yazımı da alt servis girişinden denetlenir.
- 🟢 Avans ve borç ödeme kaldırılırken bağlı muhasebe fişi fiziksel silinmez. Güncel `MuhasebeFisleriSil` izni ve taslak fiş durumu gerekir; fiş soft-delete edilir, kalemler korunur. Kalıcı borç silmenin bağlı fişlerinde de aynı kural geçerlidir. Onaylı/iptal edilmiş veya aktif firma kapsamında bulunamayan fiş işlemi durdurur. Borç silmenin ana kayıt sorgusundaki filtre atlaması kaldırıldı.
- 🟢 Tekil mahsup ve borç ödemesinde sıfır/negatif tutar reddedilir; bu girdilerle kalan avans/borç ters yönde artırılamaz.
- 🟡 Mevcut çok context'li otomatik fiş üretme/linkleme tek transaction değildir. Bağlı fişli kayıt düzenleme/iptalinin mali tutarlılığı ve silinmiş alt ödeme kayıtlarının kalıcı silme kapsamı ayrıca denetlenmeli. Doğrudan atomik muhasebe girişleri ve diğer maaş/puantaj/bütçe servisleri henüz bütünüyle korunmuş sayılmaz. A-29/A-09/A-15 genel renkleri değişmedi.
- 🟡 Kullanıcı planına uygun olarak bu tur derleme veya test çalıştırılmadı. Son aşamada personel finans izinleri, taslak/onaylı fiş kaldırma, negatif/sıfır tutar ve firma kapsamı senaryoları doğrulanacak.


### 2026-10-08 — A-29 bağlı fişli personel kayıtlarında tutarlılık

- 🟢 Avans/borç oluşturma ve düzenleme pozitif tutar ister. Düzenlemede avans tutarı mahsup edilenden, borç tutarı ödenenden küçük olamaz. Bağlı fiş veya işlenmiş bakiye varsa tutar/tarih ve ilgili mali alanlar değiştirilemez; açıklama gibi mali olmayan alanların düzenlenmesi korunur. İptal edilmiş kayıt düzenlenemez.
- 🟢 Avans/borç iptalinde aktif mahsup/ödeme geçmişi veya işlenmiş bakiye varsa işlem reddedilir. İşlem görmemiş kayıt varsa bağlı taslak fiş, muhasebe silme izniyle soft-delete edilir; kayıt iptali ve fiş değişikliği tek `SaveChanges` çağrısında kaydedilir. Onaylı/iptal fiş iptali engeller. Zaten iptal edilmiş kayda tekrar iptal notu eklenmez.
- 🟢 Mahsup geri alma, `PersonelAvansMahsup` kaynak kimliğine bağlı fişleri bulur. Taslak fişleri kalemleri korunarak soft-delete eder; avans bakiyesi ve mahsup kaydıyla aynı transaction'da kaydeder. Onaylı fiş veya eksik silme yetkisi bütün işlemi durdurur.
- 🟢 İptal edilmiş avansa yeni mahsup, iptal edilmiş borca yeni ödeme eklenemez.
- 🟡 Eşzamanlı düzenleme/ödeme/iptal için kilit veya concurrency sözleşmesi, otomatik kayıt+fiş+bağlantı üretiminin ortak transaction'ı ve eski tutarsız kayıt onarımı halen açık. Bu kaynak kontrolleri geçmiş müşteri verisinin düzeldiği anlamına gelmez. A-09/A-15/A-16/A-29 genel renkleri korunur.
- 🟡 Derleme ve test çalıştırılmadı; kullanıcı kararıyla son aşama planına bırakıldı.


### 2026-10-08 — A-09/A-29 personel kayıt ve otomatik fişte ortak transaction

- 🟢 Avans oluşturma, borç oluşturma, borç ödeme ve tekil avans mahsubu ortak `ExecuteFinanceWriteAsync` akışına taşındı. Her girişimde yeni context ve `Serializable` transaction açılır; kayıt/bakiye, fiş üretimi ve varsa fiş bağlantısı aynı context üzerinden kaydedilir. İşlem tamamlanmadan commit yapılmaz; fiş veya bağlantı yazımı hata verirse transaction dispose ile geri alınır.
- 🟢 Dört özel muhasebe yardımcısı ayarları da çağıranın context'inden okur; fiş için `CreateFisAtomicAsync(fis, context)` kullanır. Ayrı context'te fiş üretimi ve ayrı context'te bağlantı kaydı kaldırıldı. Mahsup fişi kaynak kimliğiyle aynı transaction'a dahildir; yeni bir fiş FK'sı eklenmedi.
- 🟢 Execution strategy'nin her girişiminde güncel yetki tekrar aranır. Başarısız girişimin ürettiği kayıt/fiş kimliği ve fiş navigation'ı yeniden girişten önce sıfırlanır/eski girdiye döner. Ödeme ve mahsup yeni context'ten yüklenen ana kayda bağlanır. Nihai hata halinde girdi kimlikleri de eski durumuna alınır.
- 🟡 Bu kod transaction sınırını düzeltir; bağlantı kopmasında commit sonucunun belirsizliği/idempotency, execution strategy'nin hedef DB ayarları ve eşzamanlı işlem sonuçları henüz doğrulanmadı. Diğer düzenleme/silme/maaşa toplu mahsup/bütçe/banka servisleri bu yeni ortak transaction'a taşınmış sayılmaz. A-09/A-29 genel durumları sarı kalır.
- 🟡 Kullanıcı planına uygun olarak derleme/test çalıştırılmadı. Son aşamada ilk kayıt, fiş yazımı, link yazımı ve commit kesintisi noktalarına hata enjeksiyonu ve eşzamanlı ödeme/mahsup senaryoları uygulanmalı.


### 2026-10-08 — A-09 personel commit belirsizliğinde tekrar yazma koruması

- 🟢 Ortak personel transaction yardımcısı commit başladığını kaydeder. Commit sonrası hata geçici sayılsa bile execution strategy yeni mali yazım başlatamaz; yeniden giriş kontrolü yazımdan önce durur.
- 🟢 Commit hatasından sonra bağımsız context ile ana kayıt kimliği, oluşturulma zamanı, ana ilişki/tutar ve varsa fiş bağlantısı denetlenir. Kayıt bu ölçütlerle görünüyorsa daha önce oluşturulan sonuç döndürülür. Kayıt doğrulanamıyorsa veya DB erişilemiyorsa işlem sonucu belirsiz olarak bildirilir; sessiz tekrar INSERT yapılmaz.
- 🟢 Belirsiz sonuçta üretilen kimlik korunur ve hata metninde kayıt numarası verilir. Dört oluşturma/ödeme/mahsup girişine kimliği bulunan nesneyi yeniden oluşturma koruması eklendi. Commit öncesi başarısız girişimler önceki reset/new-context retry akışını kullanır.
- 🟡 Bu koruma aynı servis çağrısı ve aynı girdi nesnesi içindir. Yeni nesne/yeni HTTP isteği, yeniden başlatma veya farklı sunucu üzerinden yeniden gönderime kalıcı idempotency anahtarı sağlamaz. DB zaman hassasiyeti ve sorgu filtreleri doğrulamayı engellerse sonuç başarı varsayılmaz. Hedef SQLite/PostgreSQL commit hata enjeksiyonu son aşamada yapılmalı. A-09/A-29 sarı kalır.
- 🟡 Kullanıcı planına uygun olarak derleme ve test çalıştırılmadı.


### 2026-10-08 — A-09/A-29 ödeme ve mahsupta kalıcı işlem kimliği

- 🟢 `PersonelBorcOdeme` ve `PersonelAvansMahsup` için `IslemKimligi` (32 karakter GUID) ve `IslemOzeti` (SHA-256) eklendi. Yeni form nesnesi kimliği bir kez üretir; aynı mantıksal isteğin yeniden gönderiminde kimlik korunmalıdır. Servis boş/geçersiz kimliği reddeder, özeti kendisi hesaplar.
- 🟢 Aynı kimlik ve aynı ebeveyn/istek içeriğinde mevcut kalıcı kayıt döner; bakiye tekrar değiştirilmez ve ikinci fiş üretilmez. Aynı kimlik farklı içerik veya kaldırılmış işlem için kullanılırsa reddedilir. Tekrar sonuç döndürme de güncel izin ve aktif firma kapsamındaki ana kayıt kontrolüne tabidir.
- 🟢 İki tabloya filtrelenmemiş benzersiz işlem kimliği indeksi eklendi; farklı süreç/sunucu aynı DB üzerinde aynı anahtarı ikinci kez yazamaz. Yarış/hata sonrası servis eşleşen kaydı tekrar sorgular; bulunmadığında başarı varsaymaz. İşlem özeti tutar, tarih, ödeme/mahsup şekli, banka, açıklama ve ilgili ana kaydı; borç ödemesinde muhasebe üretim tercihini kapsar.
- 🟢 Ödeme geçmişi bulunan borcun kalıcı silinmesi engellendi; silinmiş ödeme geçmişi de kapsanır. Böylece cascade silme tüketilmiş ödeme anahtarını kaybettiremez. Ödeme/mahsup soft-delete sonrası indeks anahtarı saklanır.
- 🟡 `20261008120000_AddPersonnelPaymentOperationKeys` migration'ı ve model snapshot güncellendi; migration hiçbir gerçek DB'ye uygulanmadı. Eski satırların yeni sütunları NULL kalır; geçmiş işlemler için sahte kimlik/kanıt üretilmez. Hedef PostgreSQL/SQLite migration ve eski şema hazırlık zinciri son aşamada doğrulanmalı. Şema yükseltilmeden bu yeni model mevcut DB üzerinde çalıştırılmamalı.
- 🟡 Kalıcı koruma aynı anahtarla ödeme ve mahsup tekrarına aittir. Farklı anahtar gönderimi, avans/borç oluşturma, maaşa otomatik toplu mahsup ve diğer mali servisler bu kapsamda kapanmaz. Yeni istek/profil/sunucu aynı mantıksal işlem için anahtarı korumalıdır. A-09/A-29 sarı; derleme/test kullanıcı kararıyla son aşamada.


### 2026-10-08 — A-09/A-29 avans ve borç oluşturmada kalıcı tekrar koruması

- 🟢 Avans ve borç modellerine işlem kimliği/istek özeti ile filtrelenmemiş benzersiz indeks eklendi. Oluşturma servisleri aynı kimlik ve içerikte mevcut kaydı döndürür; yeni kayıt/bakiye/fiş üretilmez. Farklı içerik, silinmiş veya iptal edilmiş kayıt kimliği reddedilir. Tekrar sonucu döndürürken `MaasYaz` güncel DB izni denetlenir.
- 🟢 Oluşturma tek seçili firma gerektirir. FirmaId boşsa seçili firma işlemden önce atanır; farklı firma veya tüm firmalar rapor modu oluşturmayı reddeder. Firma bilgisi istek özetindedir ve transaction girişinde tekrar denetlenir. Filtre atlanan tekrar sorgusu açıkça seçili FirmaId ve işlem kimliğiyle sınırlıdır.
- 🟢 İşlem kimliği taşıyan, ödeme geçmişsiz borç kaldırıldığında ana kayıt soft-delete edilir; tekrar koruması ve fiş kalemleri korunur. Eski anahtarsız ve ödeme geçmişsiz borçta mevcut fiziksel kaldırma akışı korunur. Maaş ekranı kaldırma onayı artık ödeme hareketlerinin kalıcı silineceğini söylemez.
- 🟡 `20261008121000_AddPersonnelCreationOperationKeys` migration ve model snapshot hazırlandı; gerçek DB'ye uygulanmadı. Önceki ödeme/mahsup migration'ından sonra çalışmalıdır. Eski satırların anahtarı/özeti NULL kalır. Derleme, migration provası ve test kullanıcı planıyla son aşamada.
- 🟡 Koruma aynı mantıksal istekte aynı işlem kimliği tutulduğu sürece geçerlidir. Yeni kimlik yeni işlem sayılır; maaşa otomatik toplu mahsup, bütçe/banka ve diğer mali zincirler bu kapsama alınmış sayılmaz. A-09/A-29 genel renkleri sarı kalır.


### 2026-10-08 — A-09/A-29 maaşa otomatik mahsup ve geri alma

- 🟢 Otomatik avans mahsubu maaş/avans bakiyeleri ve mahsup satırlarıyla aynı Serializable transaction içinde yürütülür. Maaş kimliğine bağlı kalıcı parti özeti ve avans bazında benzersiz işlem anahtarı kullanılır; tekrar çağrı mevcut parti toplamını döndürür. Yeni açıklama/tarih veya yeni açık avans ikinci otomatik parti açmaz. Avanslar aynı firma/personelle sınırlı, tarih ve kimlik sırasıyla işlenir.
- 🟢 Maaşa bağlı tekil mahsup firma/personel/ödeme durumu ve maaş kapasitesini denetler; maaşın Avans kesintisini aynı transaction içinde artırır. Mahsup kaldırma maaş kesintisini, avans bakiyesini, soft-delete kaydını ve varsa bağlı taslak fiş kaldırmayı aynı transaction içinde yürütür. Ödenmiş maaş, onaylı fiş veya tutarsız bakiye geri almayı reddeder.
- 🟢 Otomatik parti kısmen/tamamen geri alınırsa tekrar uygulanmaz; kayıtlar ve tüketilmiş anahtarlar korunur. Önceden başka/eski mahsup geçmişi olan maaşta yeni otomatik parti reddedilir. Gerekli yeni kesinti tekil mahsup akışından yapılmalıdır. Ekran bildirimi mevcut parti toplamını gösterir; tekrar çağrıyı yeni kesinti gibi sunmaz.
- 🟡 Önceki iki işlem anahtarı migration'ı gereklidir; bu oturumda uygulanmadı. Eski tekil mahsuplar maaş kesintisini güncellememiş olabilir; tutarsız eski kayıtlar otomatik düzeltilmez. Otomatik maaş mahsubunun muhasebe hesabı sözleşmesi ayrıca değerlendirilmelidir; nakit tahsilat fişi bu akışa eklenmedi.
- 🟡 Derleme/test/migration provası son aşamada; eşzamanlı iki sunucu, yanıt kaybı, geri alma, eski veri ve hedef sağlayıcı senaryoları henüz çalıştırılmadı. A-09/A-29 genel durumları sarı kalır; satış onayı verilmiş değildir.


### 2026-10-08 — A-09/A-29 maaş servis sınırının korunması

- 🟢 Maaş oluşturma/toplu oluşturma, düzenleme/ödeme/yeniden hesaplama ve kaldırma servisleri sırasıyla güncel `MaasYaz`, `MaasDuzenle`, `MaasSil` izinlerini denetler. Mali yazımlar taze context ve Serializable transaction içinde yürütülür; commit girişinden sonra otomatik yeniden yazma yapılmaz.
- 🟢 Maaş güncellemede eski `UpdatedAt` reddedilir; firma/personel/dönem ve silinme bilgisi değiştirilemez. Oluşturma/audit alanları istemci nesnesinden ezilmez. Mahsup geçmişi varsa Avans kesintisi doğrudan düzenlenemez; ödenmiş/kısmi ödenmiş maaşın hesap ve kesinti alanları korunur. Negatif ödeme tutarı reddedilir.
- 🟢 Maaş kaldırma soft-delete oldu. Ödeme işareti/tarihi veya aktif/silinmiş mahsup geçmişi bulunan maaş kaldırılamaz. Toplu kaldırma ve yeniden hesaplama seçilen tüm maaşların kapsamını kontrol eder; uygun olmayan kayıt varsa işlem topluca reddedilir. Yeniden hesaplama ödeme/mahsup geçmişini değiştirmez.
- 🟢 Maaş ödeme tarihi, durumu ve açıklaması tek servis çağrısı ve transaction içinde kaydedilir. Aynı tarih/açıklama tekrarında ikinci durum değişikliği yapılmaz; zaten ödenmiş maaşa farklı ödeme bilgisi reddedilir. Düzenleme üzerinden yeni ödeme işareti verilemez; mevcut ödeme iptali hesap alanları değiştirilmeden yapılabilir. Bu akış ödeme durumunu işaretler; banka/kasa hareketi veya ters muhasebe fişi ürettiği iddia edilmez.
- 🟢 Yeni maaşın FirmaId'si doğrulanan personelden atanır. Toplu oluşturmada farklı firma/eksik personel reddedilir; dönem mevcut kayıt kontrolü ve oluşturma aynı transaction içindedir.
- 🟡 Kod incelemesine göre düzenlendi; derleme/test son aşamada. Sağlayıcıda eşzamanlı maaş/mahsup/ödeme, commit belirsizliği, eski timestamp ve toplu rollback senaryoları henüz çalıştırılmadı. Yeni şema migration'ları bu oturumda uygulanmadı. Tüm mali servisler ve gerçek kabul kapsamı kapanmadığından A-09/A-29 sarı kalır.


### 2026-10-08 — A-09/A-29 banka/kasa yazım ve bağlantı koruması

- 🟢 Hareket oluşturma, transfer, cari mahsup ve personel geri ödeme girişlerinde güncel `BankaHareketleriYaz`; genel düzenlemede `BankaHareketleriDuzenle`; kaldırma/mahsup iptali/geri ödeme iptalinde `BankaHareketleriSil` servis düzeyinde denetlenir. Banka hesabı oluşturma/düzenleme/kaldırma ve firma atamasında ilgili hesap izinleri eklenmiştir. Hesap servisindeki kontrol, bu servise yönlendiren wrapper'ları da kapsar.
- 🟢 Genel hareket düzenleme ve kaldırma taze context ve Serializable transaction içinde yapılır. Eski UpdatedAt reddedilir; kaynak, mahsup bağlantısı, muhasebe fişi, geri ödeme durumu ve silinme bilgisi genel düzenlemeden değiştirilemez. Commit başladıktan sonra otomatik tekrar yazma yapılmaz; belirsiz sonuç başarı sayılmaz.
- 🟢 Muhasebe fişine, fatura eşleştirmesine, bütçe ödemesine, transfer/mahsuba, araç masrafına veya personel geri ödemesine bağlı hareket genel düzenleme/kaldırmadan korunur. Bağlantısız hareket kaldırma soft-delete oldu; önceki fiziksel eşleştirme/kayıt silme ve bütçe ödeme alanlarını sıfırlama genel kaldırma yolundan çıkarıldı. Kaynak işlem akışları ayrıca değerlendirilmelidir. Rent a Car için mevcut senkron güncelleme/kaldırma helper'ları transaction içinde korunur; diğer bağlantı kontrolleri bu akışta da geçerlidir.
- 🟢 Yeni harekette işlenmiş/silinmiş kayıt veya bağlı işlem kimliği taşınması reddedilir. Geçersiz yön/kaynak enum'u ve aktif olmayan banka hesabı reddedilir. Banka hesabı düzenleme/kaldırma/firma ataması Serializable transaction içindedir; hareket geçmişli hesabın para birimi, tipi ve açılış bakiyesi değiştirilemez. Firma ataması yalnız seçili firmaya ve firmasız hesaba yapılabilir; mevcut firmalı hesap başka firmaya taşınamaz, hareket firması uyuşmazlığı reddedilir.
- 🟡 Transfer/cari mahsup fişlerinin ayrı context'te üretimi ve hata yutma; mahsup iptalinin fişle atomikliği; personel geri ödemede kısmi seçim ve ortak ödeme iptali sözleşmesi henüz bu düzeltmeyle kapanmaz. Bu özel zincirler genel hareket korumasından ayrı açık iş olarak sürer.
- 🟡 Derleme/test kullanıcı planıyla son aşamada; gerçek DB transaction/yarış/rol değişimi ve kaynak akış uyumluluğu henüz çalıştırılmadı. A-09/A-29 genel renkleri sarı; 4 yeşil / 21 sarı / 6 kırmızı dağılımı değişmedi.


### 2026-10-08 — A-09/A-29 transfer ve cari mahsup atomik kaydı

- 🟢 Hesaplar arası transferde kaynak/hedef hesap sorgusu, kaynak bakiye kontrolü, iki hareket, karşı hareket bağlantısı, muhasebe fişi ve iki hareketin MuhasebeFisId bağlantıları tek context/Serializable transaction içinde yürütülür. Cari mahsupta hareket, cari alt hesap hazırlığı, muhasebe fişi ve fiş bağlantısı aynı transaction içindedir. Ayrı context'te fiş yazma ve fiş hatasını yutup başarı dönme kaldırıldı.
- 🟢 Transfer hesapları aktif, aynı firma ve para biriminde olmalıdır. Cari mahsupta cari/hesap firması eşleşir; iki işlem de tek seçili firma gerektirir. Eksik muhasebe eşleştirmesinde işlem reddedilir ve hareketler kaydedilmez. Bu nedenle muhasebe hesabı hazırlanmamış kurulumda transfer/cari mahsup kullanımı için önce hesap planı/eşleştirme tamamlanmalıdır.
- 🟢 Hareket numarası aynı transaction bağlantısında üretilir; SQLite yazım transaction'ı içinden ayrı bağlantıyla ikinci numara yazımı yapılmaz. Ortak fiş sayacı PostgreSQL/SQLite için sağlayıcı komutu ve mevcut transaction üzerinden çalışacak şekilde düzenlendi; desteklenmeyen sağlayıcı reddedilir. Mevcut muhasebe modelinde fiş/hesap planı FirmaId taşımaz; muhasebe fiş sayacının mevcut ortak kapsamı burada değiştirilmedi. Hareket numaraları firma bazındadır.
- 🟢 Transfer/cari mahsup fişi yardımcıları güncel banka yazım iznini denetler, kalıcı hareket kimliği/yön/tutar/hesap uyuşmasını kontrol eder ve mevcut kaynak fişi varsa yeniden üretmez. Dışarıdan transaction verilmezse yardımcı kendi Serializable transaction'ını açar. Cari hesap hazırlığı sorguları aynı context'e taşındı; üst hesap ve alt hesap değişikliği aynı işlemde kaydedilir.
- 🟡 Yeni bir transfer/cari mahsup isteğinin yeniden gönderiminde kalıcı istek kimliği henüz yoktur; aynı hareketin fiş tekrarını önlemek, yeni istekten ikinci hareketi engellemek anlamına gelmez. Commit sonrası belirsiz sonuç otomatik yeniden yazılmaz; kullanıcı yeni istek göndermeden listeden kontrol etmelidir. Mahsup iptali/ters fiş atomikliği ve ortak personel geri ödeme iptali ayrıca açıktır.
- 🟡 Derleme ve test son aşamada; PostgreSQL/SQLite sayaç, eşzamanlı bakiye, fiş hata/rollback ve karşı hareket bağlantı senaryoları henüz çalıştırılmadı. A-09/A-29 sarı ve genel görev renk dağılımı korunur.


### 2026-10-08 — Ertelenen derleme ve otomatik testlerin çalıştırılması

Kullanıcının bu oturumda verdiği test talebiyle son aşama kontrolleri başlatıldı. 🟢 Çözüm Release derlemesi **0 hata / 0 uyarı**; 🟢 genişletilmiş otomatik paket **54/54 başarılı, 0 atlanan**. Yeni 12 regresyon senaryosu SQLite sayaç/transaction rollback, dört işlem anahtarı migration tablosu, güncel DB yetkisi ve maaş servis yazımlarını kapsar. Test fixture ilişki/HttpContext hataları düzeltilerek son paket yeniden çalıştırıldı. [Komut, kanıt ve kapsam raporu](TEST-DOGRULAMA-2026-10-08.md).

⚪ Tarayıcı testi için yapılandırılmış test hesabı yok; PostgreSQL hazır olma kontrolü kabul testi sayılmadı. Tam migration zinciri, gerçek müşteri restore, S3, çok sunucu, hedef hacim, transfer/iptal/geri ödeme zinciri ve mali hata enjeksiyonu açık. Geçmiş bölümlerdeki “derleme/test ertelendi” ifadeleri o çalışmanın tarihsel durumudur; bu koşunun kapsadığı kontroller artık yukarıdaki sonuçla günceldir. A-09/A-29 genel durumu 🟡 kalır; genel dağılım 4 yeşil / 21 sarı / 6 kırmızıdır.


### 2026-10-08 — İkinci doğrulama: banka transaction ve otomatik mahsup

🟢 Güncel Release çözüm derlemesi **0 hata / 0 uyarı**; otomatik paket **60/60 geçti, 0 atlandı**. Transfer/cari mahsupta başarı ve fiş hata enjeksiyonu sonrası tam rollback; karşı hareket/fiş FK'ları, bakiye, cari alt hesap rollback'i; otomatik maaş mahsubunda tekrar/geri alma doğrulandı.

🟢 Testin yakaladığı SQLite şema yardımcısının DbContext bağlantısını dispose etmesi düzeltildi. Şema yardımcısını gerçekten çalıştıran servis testleri tekrar geçti. 🟢 Ayrı PostgreSQL 17 cluster'ında uygulamanın numara sayacı, transaction rollback, 20 eşzamanlı numara ve dört işlem anahtarı tablosunun yeni migration/benzersiz indeks kontrolleri geçti. Geçici sunucu durduruldu; mevcut müşteri DB'sine yazılmadı. [Güncel kapsam ve kanıt](TEST-DOGRULAMA-2026-10-08.md).

⚪ Tarayıcı hesabı, tam PostgreSQL mali servis ve geçmiş migration zinciri, gerçek restore/S3/çok sunucu/hedef hacim; 🟡 iptal/ters fiş ve personel geri ödeme zinciri hâlâ açık. A-09/A-29 genel renkleri sarı; önceki 54/54 ilk koşu sonucu olup son paket sonucu 60/60'tır.

### 2026-10-08 — Mahsup iptali ve ters fiş bütünlüğü

- 🟢 Transfer/cari mahsup iptalinde ters fiş ve banka hareketlerinin soft-delete işlemi aynı Serializable transaction/context içinde kaydedilir. Hata yutma ve finansal geçmişin fiziksel silinmesi kaldırıldı; ters fiş yazılamazsa banka bakiyesi ve hareketler korunur.
- 🟢 Eski onaylı fiş defterde korunur; onaylı ters fişle net etki sıfırlanır. Eski fişi rapor dışında bırakıp yalnız ters fişi hesaba katma hatası giderildi. Bu kaynak fişleri ve ters kayıtlarının manuel düzenleme, silme, onay ve onay geri alma girişleri reddedilir.
- 🟢 Güncel silme izni ve seçili firma kontrolü; karşı hareket bağlantısı, grup bütünlüğü, tutar/fiş uyuşması ve bağlı ödeme koruması eklendi. Tekrar iptal yeni ters fiş üretmez; kullanıcıya grubun aktif olmadığı bildirilir. Eksik fiş bağlantılı eski kayıtlar otomatik tahminle iptal edilmez, veri onarımı gerektirir.
- 🟢 Altı yeni SQLite regresyon senaryosu: transfer/cari iptal başarısı, her iki akışta ters fiş hata enjeksiyonu/rollback, eksik transfer bağlantısı ve tutar uyuşmazlığı. Başarı senaryoları tekrar iptal reddini, geçmiş satırlarının korunmasını, banka bakiyesini, hesap bazında net sıfırı ve manuel fiş değişikliği retlerini de denetler.
- 🟢 Release çözüm derlemesi **0 hata / 0 uyarı**; otomatik paket **66/66 başarılı, 0 atlanan**. [Kanıt ve kapsam](TEST-DOGRULAMA-2026-10-08.md).
- 🟡 A-09/A-29 genel durumu korunur: yeni transfer isteği için kalıcı istek kimliği, ortak personel geri ödeme iptali ve tam PostgreSQL mali servis kabulü açık. Gerçek müşteri restore/S3/çok sunucu/hedef hacim kabulü bu SQLite sonucu ile kapatılmadı. Önceki 54/54 ve 60/60 sonuçları tarihsel koşulardır; bu koşunun son sonucu 66/66'dır.

### 2026-10-08 — Personel geri ödeme ve ortak iptal bütünlüğü

- 🟢 Personel geri ödeme oluşturma ve iptal yazımları ortak `WriteBankAsync` sınırına taşındı: her denemede yeni context, güncel izin, Serializable transaction ve commit başladıktan sonra otomatik tekrar yazmama koruması. Hareket numarası ödeme transaction'ında üretilir; ayrı hesap/numara context'i kaldırıldı.
- 🟢 Seçilen masrafların tamamı aynı firma/personelin geçerli ve ödenmemiş kayıtları olmalıdır. Eksik/geçersiz seçimde yalnız geçerli alt küme ödenmez. Ödeme hesabı aynı firmada aktif olmalı; masraf/ödeme para birimleri eşleşmelidir. Kaynak kayıtların kapanışı ve banka çıkışı birlikte kaydedilir; aynı ödenmiş kayıtları yeniden ödeme reddedilir.
- 🟢 Ortak banka ödemesindeki bir masrafın iptali, ödemeye bağlı tüm masrafları birlikte yeniden ödenecek duruma alır ve banka çıkışını soft-delete eder. Tek kaydı açıp ortak çıkışı tam tutarla bırakma hatası giderildi. Ödeme tutarı ve geçmiş banka satırı korunur. Hesapsız ödeme işareti yalnız seçili kayıtta geri alınır.
- 🟢 Tutarsız toplam/bağlantı, gizli veya silinmiş bağlı kayıt ve fiş/eşleştirme/bütçe ödemesi olan banka çıkışı iptal edilmez. Fişe bağlanmış ödeme için otomatik ters fiş oluşturulmuş sayılmaz; kaynak akıştan çözülmesi gerekir.
- 🟢 Ekranın iptal izni servisle eşleştirildi; ortak iptalin kapsamı onay mesajında açık. Genel düzenlemede ödeme durumu/tarihi alanı kaldırıldı, personel seçimi salt okunur oldu; ödenmiş veya kaynak masraf/fiş bağlantılı kayıt için düzenleme kapalı. Düzenleme nesnesine `UpdatedAt` ve korunan bağlantılar taşındı.
- 🟢 Dokuz yeni SQLite regresyon senaryosu geçti: ödeme başarısı/hata rollback'i, ortak iptal başarısı/hata rollback'i, eksik seçim reddi, fişli ödeme iptal reddi, hesapsız işaret/iptal, para birimi ve ortak toplam uyuşmazlığı. Başarı kontrolleri tekrar ödeme/iptal reddini, ödeme bağlantılarını, banka bakiyesini ve fiziksel satırların korunmasını da içerir.
- 🟢 Release çözüm derlemesi **0 hata / 0 uyarı**; otomatik paket **75/75 başarılı, 0 atlanan**. [Kanıt ve kapsam](TEST-DOGRULAMA-2026-10-08.md).
- 🟡 A-09/A-29 genel renkleri korunur. Yeni transfer isteği için kalıcı istek kimliği, tam PostgreSQL mali servis/commit belirsizliği/eşzamanlılık doğrulaması ve saha kabulleri açık. Tarayıcı etkileşimi bu koşuda çalıştırılmadı. Önceki günlükte açık olan ortak personel geri ödeme iptalinin yukarıdaki teknik kapsamı artık tamamlandı; önceki 66/66 sonucu tarihsel koşudur.

### 2026-10-08 — Transfer/cari mahsup tekrar istek koruması

- 🟢 Transfer ve cari mahsupta geçerli, boş olmayan GUID işlem kimliği zorunlu; kimliksiz çağrı yeni kayıt üretmez. Kimlik normalize edilir; firma, işlem türü, hesap/cari, yön, tutar, tarih ve metin alanları SHA-256 içerik özetiyle bağlanır.
- 🟢 Aynı kimlik ve aynı içerik mevcut hareket/grup/fişi döndürür; farklı içerik, iptal veya tutarsız fiş/karşı hareket bağlantısı yeni kayıt üretmeden reddedilir. Transferde yalnız ana hareket kimliği tüketir; karşı hareket tekrar anahtar taşımaz. Silinmiş kayıt kimliğini korur.
- 🟢 `BankaKasaHareketleri` için nullable `IslemKimligi`/`IslemOzeti`, benzersiz kimlik indeksi, model snapshot ve `20261008122000_AddBankOperationKeys` migration'ı eklendi. Eski NULL kimlikli satırlar değiştirilmez. Genel hareket oluşturma/düzenlemeden kimlik değiştirme engellenir.
- 🟢 Mahsup ekranı bekleyen kimliği firma/işlem türüne göre `sessionStorage` içinde saklar; form açma ve sekmede yenileme sonrası aynı kimlik korunur, başarılı cevap alındıktan sonra temizlenir. Kaydetme sürerken ikinci çağrı engellenir. Tarayıcı saklama alanı erişilemezse işlem başlatılmaz. Kapanmış sekme/yeni tarayıcı oturumu aynı kimliği kendiliğinden taşımaz; yeniden ödeme öncesi liste kontrolü gerekir.
- 🟢 Bütçe servisinin cari mahsup çağrısı bütçe kaydı ve firmadan türetilen sabit kimliği gönderir. Böylece aynı bütçe satırının cari mahsup tekrarında ikinci banka hareketi üretilmez. Bütçe satırını banka hareketine bağlayan yazım hâlâ ayrı context/işlem sınırındadır; bu değişiklik o bütünlük riskini kapatmaz.
- 🟢 Sekiz yeni SQLite senaryosu geçti: transfer/cari aynı kimlik tekrarı, farklı içerik/tür reddi, iptal edilmiş kimliğin tüketilmiş kalması, fiş hatası/rollback sonrası aynı kimlikle başarı; kimliksiz/geçersiz kimlik reddi ve yeni migration'ın eski NULL satır/soft-delete benzersizlik kontrolü. Release çözüm derlemesi **0 hata / 0 uyarı**, paket **83/83 başarılı, 0 atlanan**. [Kanıt ve kapsam](TEST-DOGRULAMA-2026-10-08.md).
- 🟡 Yeni migration müşteri DB'sine uygulanmadı; dağıtımda bu migration'ın uygulanması gerekir. PostgreSQL yeni banka migration'ı/mali servis/eşzamanlılık, tarayıcı sessionStorage etkileşimi ve bütçe kayıt+hareket atomikliği ayrıca doğrulanmalıdır. A-09/A-29 genel durumu sarı; yukarıdaki tekrar istek teknik kapsamı yeşildir. Önceki 75/75 sonucu tarihsel koşudur.

### 2026-10-08 — Dashboard SQLite şema hatası: `IslemKimligi`

Dashboard finans verileri SQLite'ta `no such column: b.IslemKimligi` hatası veriyordu. Kök neden `DbInitializer` içindeki eski SQLite geçmiş oluşturma yoluydu: migration geçmişi olmayan mevcut DB'de tüm bekleyen migration'lar çalıştırılmış gibi kaydediliyordu.

🟢 Baseline artık eski şema watermark'ıyla sınırlı. Daha yeni migration'lar uygulanmadan “geçmişe yazılamaz”. Daha önce hatalı işaretlenmiş kurulumlarda eksik banka işlem anahtarı sütunları ve benzersiz indeks, mevcut hareket kayıtlarına dokunmadan açılışta tamamlanır. Üç izole SQLite regresyon testi, dashboard son hareket sorgusunun onarım sonrası çalışması dahil; A-15 için uygulama katmanında dört ve doğrudan SQL/DB trigger migration'ında altı firma bağı senaryosu eklendi. Tam Release doğrulaması **102/102** geçti, build 0 hata/uyarı. [Doğrulama ayrıntısı](TEST-DOGRULAMA-2026-10-08.md).

🟡 Gerçek müşteri DB'sinde migration/onarım ve dashboard kabulü yapılmadı; bu kod doğrulaması saha kabulinin yerine geçmez. Genel satış görev renkleri değişmedi.

### 2026-10-08 — A-15 fatura/ödeme bağı DB koruması

🟢 `20261008130000_GuardInvoicePaymentMatchFirm` migration'ı SQLite ve PostgreSQL'de `OdemeEslestirmeleri` yazımlarını ve bağlı fatura/banka hareketi firma değişikliklerini DB seviyesinde korur. Altı SQLite migration SQL testi ve dört SaveChanges testi geçti. Ayrıca PostgreSQL 17 izole cluster'ında migration uygulandı ve çapraz-firma eşleştirme ile bağlı fatura taşıma denemeleri reddedildi. Release build **0 hata/uyarı**, tam test paketi **102/102**. Ayrıntı ve PASS kanıtı [test raporunda](TEST-DOGRULAMA-2026-10-08.md).

🟡 PostgreSQL sunucusunda migration, eski müşteri verisi ve çoklu bağlantı eşzamanlılık kabulü yapılmadı; A-15'in diğer tenant ilişkileri de açık. A-15 kırmızı kalır.
