# MKFiloServis — Satışa Çıkarım Güncel Durum Raporu

> **Birleştirilmiş son durum (2026-10-05):** [Son durum ve renkli açık görev listesi](SATISA-CIKARIM-SON-DURUM-2026-10-05.md). Bu dosya tarihsel uygulama ve kanıt eklerini korur.

**Rapor tarihi:** 2026-10-02  
**Son güncelleme:** 2026-10-05  
**Kapsam:** Web, Shared, LisansDesktop, DataSync, Client, CI ve müşteri paketleme akışları.  
**Esas alınan sürüm:** Yerel çalışma ağacı; commit ve müşteri dağıtımı tamamlanmış sayılmaz.  
**Yöntem:** Kaynak incelemesi; rapordaki sonraki düzeltmeler, kayıtlı derleme/publish sonuçları ve izole çalışma zamanı kanıtlarıyla tüm sarı maddelerin karşılaştırılması. Bu renk güncellemesinde yeni test, derleme veya gerçek restore çalıştırılmadı.

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
| Fiziksel dosya silme hata bildirimi | 🟢 Kod; özlük ekranı uyarısı, yol/kimlik logu ve yerel/S3 hata iletimi | 🟡 Gerçek silme kabulü, kalıcı yeniden deneme ve yetim dosya temizliği |
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

İlk yeniden analiz sırasında kaynak düzeltmesi yapılmadı; sonraki devam turlarında kaynaklar değiştirildi ve derlendi. 2026-10-03/04 eklerinde izole geçici harness ve minimal SQLite kontrolleri kayıtlıdır; dolayısıyla raporun tümü için “otomatik/çalışma zamanı test yapılmadı” ifadesi geçerli değildir. Gerçek müşteri yedek/restore, migration veya müşteri kurulumu kabulü yapılmadı. Yeni tam bağımlılık zafiyet taraması ve geçmiş sır taraması yapılmadı; XML paket uyarısına yönelik sınırlı düzeltme aşağıda kayıtlıdır. İzole testler müşteri veritabanına yazım veya müşteri lisansı üretimi değildir.

## 7. Ayrıntılı kaynaklar

**Tarihsel uygulama notları:** Aşağıdaki ilk devam notları kendi yazıldıkları turu anlatır. N-1/N-2 için sonraki izole kontroller 2026-10-03 ekinde kayıtlıdır; güncel tamamlanma/kabul ayrımı üstteki tablo ve ilgili eklerde gösterilir.

**N-2 devamı:** `RecoveryArchive.cs`, `BackupService.cs` ve `DatabaseBackupService.cs` kaynaklarında yedekleme düzenlemeleri yapıldı. Son Web Debug derlemesi **0 uyarı, 0 hata** ile başarılı oldu. Mevcut `.mkkey` lisans yedekleri ve müşteri verileri değiştirilmedi. Gerçek yedek/restore veya otomatik test çalıştırılmadı; kod uygulaması bağımsız kurtarma kabulü değildir.

**N-1/N-3 devamı:** Ortak sürüm doğrulaması eklendi ve CI dosya filtreleri kaldırıldı. LisansDesktop Release/win-x64 çıktısı yeniden yayımlandı. Kaynak düzeltmesi sırasında müşteri lisansı üretilmedi, lisans hakları değiştirilmedi veya GitHub workflow'u çalıştırılmadı. Otomatik/çalışma zamanı kabulü henüz yapılmadı.

**O-14/O-8 devamı:** Lisanslar sekmesine **En Fazla Sürüm** ve **Sınırsız sürüm hakkı** eklendi. Yeni imzalama/geçmiş kayıtları seçilen hakkı kullanır; yeniden basım kayıtlı hakkı korur. Müşteri paketinin sürümü hakkı aşarsa işlem reddedilir. Web/Desktop v2 imza alanlarını ortak `LicenseSignaturePayload` oluşturur. Eski v2 biçimi korunur; diğer normalizasyon/introspeksiyon/seed tekrarları ve uyumluluk kabulü tamamlanmış sayılmaz.

- [39 maddenin ayrı değerlendirmesi, kaynak konumları ve yeni bulgular](SATISA-CIKARIM-YENIDEN-ANALIZ-2026-10-02.md)
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
- 🟡 **Kabul bekliyor:** Bu turda runtime/UI testi yapılmadı. Hızlı firma/dönem/yenileme geçişleri, cache hit/miss, ana/yardımcı sorgu arızası, yeniden deneme ve Dispose kabulü bekler. Başlamış sorgular iptal edilmez. Plaka geçmişi modalının ekleme/silme/kapatma sonuç koruması sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Araç silme sonuç koruması sonraki ekte kod olarak tamamlandı; runtime kabulü bekler. Import sonuçlarının seçim değişimi sırasında ekrana aktarılması ve backfill yetki/firma kabulü ayrı işlerdir. Bu değişiklik ana liste yükleme akışını kapsar.

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
