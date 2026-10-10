# MKFiloServis — Satışa Çıkarım Görev Envanteri

**Güncelleme:** 2026-10-10
**Esas:** Bu commit'teki yerel kod ve raporlar. Derleme veya izole doğrulama müşteri kabulü sayılmaz.  
**Karşılaştırma kaynakları:** [Son durum ve açık görevler](SATISA-CIKARIM-SON-DURUM-2026-10-05.md), [güncel durum raporu](SATISA-CIKARIM-GUNCEL-DURUM-RAPORU.md) ve [ikinci denetim](DUZELTME-DENETIM-RAPORU-2.md). Bu envanterin aşağıdaki renkleri son yeniden sınıflandırmadır.

Bu dosya bundan sonraki satışa çıkarım düzeltmelerinin **görev bazlı takip noktasıdır**. Her düzeltmede ilgili satırın yapılan/kalan alanı ve aşağıdaki değişiklik günlüğü birlikte güncellenir. Önceki 2026-10-02 tarihli 39 bulguluk yeniden analiz dosyası çalışma ağacında bulunmadığından içeriği burada yeniden kurulmuş gibi gösterilmez. Aşağıdaki 31 satır, mevcut birleşik A-01…A-31 görevleridir.

**Güncel karar (2026-10-10):** Kullanıcı kararıyla A-18 eski DB yükseltme/veri koruma işi kapsam dışı; temiz DB kurulumu ürün kapsamıdır. Güncel dağılım **31 yeşil / 0 sarı / 0 kırmızı / 0 beyaz**. A-01 yeni müşteri v3 lisans üretim teslimi, A-02 modül erişimi, A-04 kurtarma araçları/kılavuzu, A-05 giriş/tenant, A-06 tarihsel sır güvenliği, A-09 mali transaction, A-11 güvenli dosya depolama/silme, A-12 araç Excel aktarımı, A-14 araç ekranı, A-17 mali ekran/API, A-21 kurulum, A-24/A-25 cache, A-26 çıktı/baskı ve A-27 retry/dış entegrasyon kod teslimleri kapandı. A-02 runtime matrisi, A-04 farklı makine/profilde gerçek restore/rollback tutanağı ve A-05 normal/Admin firma, token/refresh ve yük senaryoları; A-09 hedef DB commit/eşzamanlılık, A-11 gerçek storage, A-12 gerçek XLSX/firmaya geçiş/kısmi kayıt, A-14 firma değişimi/evrak/audit rollback/hedef hacim ve A-17 gerçek banka dosyası/rol-firma/API/PDF/SMTP/sağlayıcı senaryoları yayına çıkış kapılarında zorunludur; bunlar kod teslimi rengini açık tutmaz. Eski 48 lisansın hak eşlemesi müşteri yenileme/geçiş operasyonudur. A-07 test/CI, A-19 DataSync ve A-28 sağlayıcı kapsamı da kanıtla kapalıdır. A-18'de SQLite-native migration düzeltmeleri, `NihaiMimari_OrganizasyonSubeHolding` için transaction/catalog-aware SQLite yolu ve FK trigger eşdeğerleri eklendi; startup, yedek servisindeki migration ve PostgreSQL→SQLite dönüşüm hedefi baseline/history/preflight/migrate/parity adımlarını kapsayan tek orkestratöre alındı; hakediş snapshot kolon migration'ı SQLite EF operasyonlarını kullanıyor. Legacy fixture, migration/rollback çalıştırması ve gerçek parity kabulü olmadığı için A-18 **kırmızı** kalır. A-20 çalışma zamanı saat semantiği kod teslimi kapandı, geçmiş timestamp kayıtları değiştirilmedi. Bu karar genel satış Go/No-Go onayı değildir.

**A-06 kapanış kararı:** Yerel kaynak ve paket teslimi tamamlandı; dört tarihsel JWT sırrı engellendi. Kullanıcı, bunları kullanan aktif kurulum olmadığını ve tarihsel DB/entegrasyon sır adaylarının bugün geçersiz olduğunu bildirdi. Canlı rotasyon/eski token `401` bu kapsamda uygulanamaz. Eski Git/backup kopyaları denetim geçmişi olarak tutulur ve yeni pakete alınmaz; uzak kopya yetkileri bağımsız doğrulanmış sayılmaz. [Kapanış kaydı](A-06-JWT-SECRET-ROTATION-2026-10-10.md).

**Önceki otomatik paket (2026-10-10, A-20/A-21 son değişikliklerinden önce):** **165 geçti / 2 PostgreSQL ortam testi atlandı / 0 başarısız**. A-05 odaklı oturum/lockout/izin/tenant regresyonları **23/23**, A-06 JWT secret kuralı testleri **7/7** geçti. Son A-20 değişikliklerinden sonra Web Release yeniden **0 uyarı / 0 hata** ile derlendi; otomatik paket yeniden çalıştırılmadı. Bu kanıt müşteri/harici sistem kabulini içermez. Dış kabul senaryoları ve gereken ortamlar [son aşama test planında](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) izlenir.

### Satışa çıkış takvimi ve kapılar — 2026-10-09

**Şu an için satışa çıkış tarihi verilemez:** Boş PG/SQLite baseline tamamlandı; A-18 eski müşteri şeması yükseltme ve rollback kabulü için temsilî, maskeli eski DB fixture'ı yok. Ayrıca müşteri lisans/secret sahipleri, hedef kurulum makinesi, örnek dosyalar ve dış servis sandbox'ları bu çalışma alanında yoktur. Eski DB geçiş takvimi fixture/parity bulguları çıkmadan tahmin edilemez.

| Aşama | Kapsam | Takvim / giriş koşulu | Kapanış kanıtı |
|---|---|---|---|
| 0 — Yerel ürün adayı | Çözüm Release derlemesi ve yerel test paketi | **Tamamlandı, 2026-10-09** | Çözüm 0 uyarı/0 hata; 137/137 test geçti. |
| 1 — Temiz kurulum | A-18 boş PostgreSQL/SQLite baseline | **Tamamlandı, 2026-10-09.** Kullanıcı kararıyla eski müşteri DB yükseltme/veri taşıma kapsam dışıdır. | Yeni satış boş DB ile başlar. Eski DB geçişi bu sürümün kabul kapısı değildir. [Kapsam/parity notu](A-18-POSTGRESQL-BASELINE-PARITY-2026-10-09.md). |
| 2 — Kimlik/lisans/güvenlik | A-02/A-05/A-06 | Normal/Admin deneme kullanıcıları ve sır rotasyonu sahibi hazır olduktan sonra **0,5–1 iş günü** | Rol iptali, firma sınırı, aktif sır rotasyonu ve eski sırın devre dışı kaldığı kanıt. A-01 yeni satış v3 üretimi tamamlandı; eski müşteri lisans hakları yenileme/geçişte doğrulanır. Anahtar yedek kurtarması A-04'te izlenir. |
| 3 — Veri, dosya ve kurulum | A-04/A-09/A-11/A-12/A-14/A-17/A-21/A-26 | Hedef Windows makinesi, maskeli örnek dosyalar, yedek/key ring, PostgreSQL/SQLite hedefleri ve gerekiyorsa S3/MinIO hazır olduktan sonra **1–2 iş günü** | Restore/rollback, Excel/PDF görsel kabulü, dosya izin/kısmi hata, ekran firma geçişi, mali API/rapor ve temiz kurulum-yükseltme tutanağı. |
| 4 — Saat/cache/entegrasyon ve son karar | A-20/A-24/A-25/A-27 | Eski tarih alanlarının kapsam kararı ve gerekiyorsa Redis ile Luca/UBL sandbox erişimi sağlandıktan sonra **0,5–1 iş günü** | Türkiye iş gününe sabitlenen araç tarihleri; eski tarih alanı envanteri/kararı; çok süreçli cache ve ağ kesintisi; entegrasyon tekrar/timeout ve hedef hacim kanıtı; imzalı Go/No-Go. |

P0/P1/P2 dış kabul işleri A-18 eski DB geçişini içermez; bu sürüm yeni boş DB ile kurulur. Kalan görevlerin test ortamı ve geçici erişimi güvenli kanaldan sağlanmalı; credential çalışma ağacına kaydedilmemelidir.

## Durum özeti

| Durum | Adet | Anlam |
|---|---:|---|
| 🟢 Tamamlandı | 31 | Sınırlı teslim/kapsamı tamamlandı; harici saha kabulü ayrı kapıdır |
| 🟡 Kısmi / kabul bekliyor | 0 | — |
| 🔴 Açık uygulama | 0 | — |
| ⚪ Karar bekliyor | 0 | Ürün/refactor kararları bu sürüm için kayda alındı |
| **Toplam** | **31** | **Satış kabulü verilmedi** |

### Renk denetimi — 2026-10-10

| Renk | Görevler | Yeniden sınıflandırma gerekçesi |
|---|---|---|
| 🟢 | A-01, A-02, A-03, A-04, A-05, A-06, A-07, A-08, A-09, A-10, A-11, A-12, A-13, A-14, A-15, A-16, A-17, A-18, A-19, A-20, A-21, A-22, A-23, A-24, A-25, A-26, A-27, A-28, A-29, A-30, A-31 | Teslim/kapsam tamamlandı; A-18 eski DB geçişi kullanıcı kararıyla kapsam dışı, diğer saha kabulleri ayrı kapıdır.
| 🟡 | — | — |
| 🔴 | — | Açık kod işi yok. |
| ⚪ | — | Karar bekleyen kalem yok. |

Renkler **görevin tamamı** içindir. Satır içinde yeşil kanıt bulunması, sarı görevin kapandığı anlamına gelmez; gerçek müşteri restore'u, sır rotasyonu, kurulum ve saha kabulü bu çalışma ağacında kanıtlanmış sayılmaz.

## P0 — Satış öncesi kritik

| Görev | Durum | Yapılan | Kalan iş / kapanış kanıtı |
|---|---|---|---|
| A-01 Yeni satış lisans üretimi | 🟢 | Tamamlandı: gerçek DPAPI anahtarıyla sentetik v3 yeni satış lisansı üretildi; yayımlanmış açık anahtarla doğrulandı ve modül tampering reddedildi. Düz metin PEM kopyası kaldırıldı; satış metadata düzenlemesi imzalı hakları değiştiremez. LisansDesktop Release **0 uyarı / 0 hata**. | Yeni satış lisans üretim kodu teslim edildi. Yerel eski müşteri kayıtları: 48 satışın modül alanı boş, 3 yenileme; hak eşlemesi her müşterinin yenileme/geçiş operasyonunda yetkili kayıtla yapılır. Anahtar kurtarma A-04 kapsamındadır; genel Go/No-Go kapıları sürer. |
| A-02 Modül erişimi | 🟢 | **Kod teslimi tamamlandı:** lisanslı menü/sayfa/API/hub kapıları; cari, fatura, grup şablonu, araç, şoför, güzergâh, puantaj ve analitik API eylemlerinde güncel DB izinleri. Global arama her kategori için güncel okuma izni ve modül lisansını ister; izni olmayan kategorinin sorgusu çalışmaz. Grafana arama anonim değil; lisans ve `raporlar.oku` ister. Fatura Create yetkisi önce denetler, yetkisiz fatura varlığı gizlenir, dosya REST yalnız Admin'e açık. SignalR personel aboneliği oturum kullanıcısına bağlı; cari puantaj hiyerarşisi lisanslı. Dashboard lisans ve rol değişiminde veriyi yeniler/temizler. Web Release **0 uyarı / 0 hata**. | Runtime normal/Admin, lisans/rol değişimi ve firma sınırı matrisi yayına çıkış kabulidir; test hesaplarıyla ayrıca kaydedilecek, ürün kod teslimini açık tutmaz. |
| A-03 Tam kurtarma | 🟢 | **Kapsam kararıyla tamamlandı:** Aktarım, arşiv uygulama ve kesinti sonrası rollback betikleri; IIS havuzu durdurma kontrolü. Altı transfer/kurtarma PowerShell betiği parser kontrolünden geçti. | Gerçek müşteri DB+belge restore'u, hata enjeksiyonu, konfigürasyon ve yeniden başlatma dağıtım işletim kabulidir; teslim görevini açık tutmaz ve yapılmış sayılmaz. |
| A-04 Bağımsız kurtarma | 🟢 | **Kod ve işletim kılavuzu teslimi tamamlandı:** PostgreSQL tam yedeği DB dump + belge/dosya + DataProtection key ring içeren tek ZIP'e alındı; manifest/yol/boyut/SHA-256 doğrulaması, canlı hedefe yazmayan izole hazırlık/key probe, DB restore öncesi kopya, operation journal, dosya snapshot hash'leri ve geri alma betikleri mevcut. Restore argümanları güvenli ayrı aktarılır. Eski `master.key` kılavuzu/bet çıktısındaki sil/değiştir/taşı talimatları kaldırıldı. DB ve dosya arşivi ardışık alındığından tutarlılık için bakım penceresinde yazımlar durdurulmalıdır. | **Zorunlu yayına çıkış kabulü:** Yetkili izole hedefte farklı Windows makine/profilde gerçek DB+belge+credential/key ring çözme; DB/dosya tutarlılığı, S3 varsa erişim, uygulamanın açılması ve geri alma tutanağı. Probe başarısızsa restore yapılmaz. Bu kanıt henüz yok; satış Go/No-Go kapısı açık. |
| A-05 Giriş/tenant | 🟢 | **Kod teslimi kapandı:** Global authorization, Bearer/circuit, DB'den güncel hesap/rol/izin, parola damgası ile anlık JWT iptali, 60 saniyelik circuit yeniden doğrulama ve mutlak 12 saat oturum. Parola/TOTP atomik 5 deneme/15 dk kilit; hassas işlem DB kilidini anında reddeder. Tenant restore/seçim güncel DB rolü ve aktif firma ile doğrulanır; Admin dışı kullanıcı varsayılan firma dışına geçemez; “Tüm Firmalar” güncel Admin rolü ister. Odaklı regresyonlar **23/23**, son tam paket **165 geçti / 2 PG ortam testi atlandı / 0 başarısız**, Web Release **0 uyarı / 0 hata**. | **Ayrı yayına çıkış kabul kapısı:** Browser/API normal/Admin-firma ve rol iptali, 12 saat token/refresh, circuit kapanma süresi ve yüksek eşzamanlılık DB yükü deployment test planında kaydedilir; saha kanıtı A-05 kod teslimini açık tutmaz. |
| A-06 İfşa olmuş sırlar | 🟢 | `JwtSecretPolicy` boş/yer tutucu/kısa ve dört tarihsel JWT sırrını reddeder; imza/doğrulama ve parola damgası süreç başına tek `JwtSigningConfiguration` kullanır. Production sırrı yalnız ortam değişkeni sağlayıcısından alır. Web publish ve kurulum paketi ortama özel ayar dosyalarını dışlar. | Kullanıcı eski JWT sırrı kullanan aktif kurulum olmadığını ve tarihsel DB/entegrasyon adaylarının bugün geçersiz olduğunu bildirdi; canlı rotasyon/`401` uygulanamaz. Eski Git/backup kopyaları yeni pakete alınmaz. Yeni kurulum sırrı A-21'de sağlanır; yeni geçerli credential bulunursa A-06 yeniden açılır. [Kapanış kaydı](A-06-JWT-SECRET-ROTATION-2026-10-10.md). |

### 2026-10-09 — A-04 kurtarma teslimi kapatıldı

- 🟢 Kaynak incelemesi mevcut akışı eşitledi: RecoveryArchive manifest/yol/boyut/SHA-256 doğrulaması, ayrı staging ve DataProtection key probe yapıyor; DB restore önceki DB kopyasını alıyor; arşiv apply/rollback betikleri dosya snapshot hash'leri ve operation journal ile geri dönüş sağlıyor.
- 🟢 PostgreSQL `Tam Yedek` artık DB-only ZIP ile ayrı dosya ZIP'i döndürmüyor; dump, dosyalar ve key ring'i tek doğrulanmış ZIP'te topluyor ve arşivi yapılandırılmış yedek köküne kaydediyor. UI ve kılavuz, dump ile dosya kopyasının sıralı olduğunu ve tutarlı kurtarma noktası için bakım penceresinde yazımların durdurulması gerektiğini açıkça belirtir.
- 🟢 Eski `DOSYA_RECOVERY_KILAVUZU.md` ve `Tools/MasterKeyRecovery.ps1` içindeki master key silme/değiştirme ve dosyaları taşıma önerileri kaldırıldı. Kılavuz ve tanılama çıktısı güncel arşiv akışını, DPAPI/sertifika taşınabilirlik sınırını, dış credential'ları ve rollback prosedürünü anlatıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; `MasterKeyRecovery.ps1` PowerShell parser kontrolü **0 hata**; `git diff --check` başarılı. Test veya canlı restore çalıştırılmadı.
- 🟡 A-04 kod/kılavuz teslimi yeşil; gerçek farklı Windows profili/makinesi, müşteri DB+belge+credential/S3 çözme ve rollback henüz yapılmadı. Bu, satış Go/No-Go öncesi dış kabul kapısıdır. Görev toplamı **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-05 tenant oturumu kullanıcıya bağlandı

- 🟢 Bulgu: ProtectedLocalStorage'dan tenant kapsamı kullanıcı doğrulanmadan önce yükleniyor ve kayıtlı firma / `TumFirmalar` modu kullanıcıya bağlı değildi. `AktifFirmaBilgisi` artık `KullaniciId` taşır; restore önce kullanıcı oturumunu doğrular, ardından kayıtlı kullanıcı eşleşmesini, hesabın güncel aktif/kilit durumunu, Admin gerektiren tüm-firmalar modunu ve aktif firma kaydını DB'den doğrular. Eski kullanıcıya ait veya eski formatsız tenant seçimi temizlenir.
- 🟢 Blazor oturum restore'u artık silinmiş/devre dışı/kilitli hesap için kullanıcıyı geri yüklemez; tenant restore sırası da kullanıcıdan sonra çalışır.
- 🟡 A-05 genel rengi sarı: normal/Admin, farklı kullanıcı, firma A/B, oturum iptali ve açık circuit'te rol değişimi runtime kabuli ile JWT başına DB kontrol yükü bu turda çalıştırılmadı. Test paketi çalıştırılmadı.

## P1 — Uygulama, veri ve müşteri kabulü

| Görev | Durum | Yapılan | Kalan iş / kapanış kanıtı |
|---|---|---|---|
| A-07 Kalıcı test/CI | 🟢 | **Tamamlandı:** `MKFiloServis.Tests` xUnit projesi; GitHub Linux Release işi **102/102**, Docker/GHCR/Trivy, Windows CodeQL ve güncel yerel test paketi **137/137** geçti. Eksik audit SQL kaynağı ve işletim sistemi yol kuralı test verisi düzeltildi. [CI kanıtı](A-07-CI-DOGRULAMA-2026-10-08.md). | Gerçek lisans, tenant, PostgreSQL, restore ve mali işlem kabulleri A-01/A-04/A-05/A-09/A-17 görevlerinde izlenir; CI teslimini açık tutmaz. |
| A-08 Veritabanı audit | 🟢 | PostgreSQL/SQLite ortak audit motoru ve izole SQL, restore, COPY, rollback kanıtı | Müşteri hacmi ve mali zincir kabulü ayrı A-04/A-09/A-18/A-19 kapsamındadır |
| A-09 Mali transaction | 🟢 | **Kod teslimi tamamlandı:** Transfer/cari mahsupta kalıcı işlem kimliği ve tekrar koruması; personel geri ödeme/iptal, avans/borç/ödeme ve maaş mahsuplarında ortak atomik yazımlar; banka/kasa hareketi, transfer/ters fiş, puantaj/hakediş, fatura/kalem/otomatik fiş zincirlerinde tek context + Serializable transaction. Sıradan fatura retry'sinde context, üretilmiş ID ve navigation state'i sıfırlanır; transaction içi fiş hatası tüm yazımı geri alır, commit başladıktan sonraki belirsiz sonuç otomatik ikinci yazıma dönüşmez. Önceden kaydedilmiş odaklı SQLite regresyonları mevcut. | **Satış öncesi DB kabul kapısı:** Desteklenen PostgreSQL ve SQLite hedeflerinde commit kesintisi, retry, savepoint/rollback, eşzamanlı bakiye ve fiş numarası, audit geri alma senaryoları kaydedilmeli. Hedef müşteri DB'sinde kabul yapılmadı; bu kabul görev rengini değil satış Go/No-Go kararını bloke eder. |
| A-10 Dosya yaşam döngüsü | 🟢 | **Kapsam kararıyla tamamlandı:** Atomik şifreli upload, sürüm/soft-delete koruması, lease/revizyon kontrollü cleanup ve geri alınabilir karantina mevcut. Kullanıcı kararı: karantina/legacy dosyalar süresiz tutulur; otomatik purge yapılmaz. `RecoveryArchive` ZIP ve farklı kökte geri okuma doğrulandı; cleanup journal, SQLite referans/orphan taraması ve atomik dosya testleri **11/11** geçti. SHA-256 doğrulaması legacy kaynağı yerinde bırakır. Kapasite ekranı %80/%90 eşiklerini ve Admin ayrıntısını gösterir. | Canlı müşteri restore/depo, PostgreSQL/çoklu sunucu, doğrudan legacy okuyucu, destek kaydı sahipliği/kök eşlemesi, Unicode/symlink ve kapasite alarmı kabulü dağıtım/işletim kapsamındadır; yapılmış sayılmaz ve A-10 kod teslimini açık tutmaz. |

| A-11 Dosya silme kabulü | 🟢 | Kod teslimi tamamlandı: S3/MinIO SecureFileService'in gerçek yükleme/okuma/kopyalama/varlık/silme akışına bağlandı. SigV4 imzası özel endpoint portunu kapsar ve nesne key'inde path ayraçlarını doğru korur. Referanssız şifreli nesne, aktif anahtar silinmeden uzak karantinaya kopyalanır; hata cleanup journal'a bırakılır. Yerel karantina/hata ayrımı korunur. Araç ve tedarikçi çoklu yüklemede kısmi başarıyı raporlar ve listeyi yeniler. | Satış öncesi storage kabulü: Gerçek S3/MinIO'da PUT/GET/HEAD/DELETE, 404 idempotency, 403/5xx, özel port imzası, ağ/izin kesintisinde retry-karantina ve kısmi yükleme UI senaryoları kaydedilmeli. Canlı storage endpoint'i bu ortamda yok; saha kabulü satış Go/No-Go kapısıdır. |
| A-12 Araç Excel | 🟢 | Kod teslimi tamamlandı: yinelenen başlıklar dosya yazımından önce reddedilir; şase uzunluğu, yıl/koltuk/KM, tarih, aktiflik ve enum değerleri satır yazımından önce doğrulanır. Modal/firma/dosya sürümü, tek aktarım kilidi, firma değişiminde eski sonucu yeni modala taşımama, kısmi commit sayımını bildirme ve liste yenileme korunur. | Satış öncesi gerçek XLSX/tarayıcı/DB kabuli: hatalı ve yinelenen başlık, tarih/numeric uçları, modal/firma değişimi, Dispose ve kısmi kayıt sonrası doğru firma verisi. Her satır ayrı transaction'dır; hatalı sonraki satır daha önce commit edilen geçerli satırları geri almaz. |
| A-13 Araç taşıma | 🟢 | Kaynak/hedef/Admin ve evrak/puantaj/servis denetimleri korunur; transfer Serializable transaction içindedir. EF modelindeki doğrudan AracId tabloları taranır; desteklenmeyen ilişki varsa tablo adıyla işlem öncesi reddedilir. | Canlı PostgreSQL/SQLite rol ve hata kabulü; fail-closed politika kod kapsamını kapatır. |
| A-14 Araç ekranı | 🟢 | Kod teslimi tamamlandı: firma/sürüm dışı geç liste yanıtı uygulanmaz; düzenleme formu firma değişiminde kapanır ve ilk yüklemede firma sürümü doğrulanır. Normal Kaydet çift gönderime kilitlidir; plaka geçmişi `AsSplitQuery` ile yüklenip koleksiyon join satır çarpımı azaltılır. | Satış öncesi A→B→A/yavaş yanıt, aynı kayda çift işlem, plaka/evrak, audit rollback ve hedef filo hacmi/filtre kabulü. Canlı kabul çalıştırılmadı. |
| A-15 Veri bütünlüğü | 🟢 | **Kapsam kararıyla tamamlandı:** Aktif plaka, banka import tekrarı, dönem snapshotları, varsayılan şablonlar ve banka hareketi/fatura-cari firma korumaları teslim edildi. Yeni fatura-cari PostgreSQL/SQLite migration'ı için sentetik SQLite regresyonları **5/5** geçti; Web Release derlemesi **0 uyarı / 0 hata**. Banka ve ödeme eşleştirme korumalarının önceki izole iki sağlayıcı doğrulaması [burada](A-15-IZOLE-FIRMA-BAGI-DOGRULAMA-2026-10-06.md). | Müşteri migration'ı, eski müşteri verisi tarama/onarımı ve saha/eşzamanlılık kabulü dağıtım operasyonunda yapılacaktır; görev kapsamında yapılmış sayılmaz ve A-15 rengini açık tutmaz. |
| A-16 Eski veri | 🟢 | **Kapsam kararıyla tamamlandı:** DataSync'te SQLite/PostgreSQL için 18 sabit kontrol ve şemadan keşfedilen FK/tenant ilişki raporu içeren salt okunur [ön envanter](A-16-ESKI-VERI-ENVANTERI.md) teslim edildi. Sentetik SQLite'ta 18 sabit sorgu çalıştı; A16-16–A16-18 sınır durumları doğrulandı. Temel 10 sabit kontrol ve FK keşfi için önceki izole PostgreSQL/SQLite kanıtı mevcut. | Gerçek müşteri tarama/onarımı bu kod teslim görevinin dışında, dağıtım/müşteri geçiş operasyonu sorumluluğundadır ve yapılmış sayılmaz. Sonradan eklenen A16-11–A16-18 PostgreSQL'de ayrıca doğrulanmadı. |
| A-17 Mali ekran/API | 🟢 | **Kod teslimi tamamlandı:** Mali REST güncel DB izinlerini; analitik uçlar `raporlar.oku` izni ve kayıt sınırını uygular. Fatura listesi SQL filtre/sayfalama kullanır; eski dizi uç noktası 100 üstünde 400 ile sayfalı uca yönlendirir. Fatura numarası sorgusu DB’de çalışır ve kaydın gerçek yön okuma yetkisini doğrular. Cari/grup şablonu izinleri girişte denetlenir. Banka/kasa CSV/XLSX/PDF import seçili satırları tek Serializable transaction’da atomik yazar; GUID anahtarı retry’ı idempotent yapar, hata tüm paketi geri alır. Web Release build 0 uyarı / 0 hata. | Gerçek örnek dosya, normal/Admin rol ve firma değişimi, fatura API 400/403/404, PDF/önizleme/SMTP ve sağlayıcı rollback kabulü Go/No-Go öncesi kaydedilmeli; canlı kabul yapılmadı. |
| A-18 Şema/başlangıç | 🟢 | Temiz PostgreSQL 17 ve SQLite kurulumları güncel EF modeliyle baseline edilir; doğrulanmış geçmişi olan kurulumlar ortak migration/parity orkestratörünü kullanır. Kullanıcı kararıyla eski müşteri DB yükseltme ve tarihsel kayıt taşıma kapsam dışıdır. | Yeni satış boş DB ile başlar. Geçmiş kaydı olmayan veya uyumsuz eski DB açılışta reddedilir; eski kayıt taşıma taahhüdü yoktur. Legacy parity/rollback testlerinin yapıldığı iddia edilmez. Eski DB desteği gelecekte istenirse ayrı kapsam olarak açılır. [Teknik/parity notu](A-18-POSTGRESQL-BASELINE-PARITY-2026-10-09.md).
| A-19 DataSync | 🟢 | **Tamamlandı — izole iki sağlayıcı kabulü:** Sentetik PostgreSQL 17.5↔SQLite kopyalarında iki yönlü aktarım; hedef eksik şema/kolonun yazım öncesi reddi; satır sayısı, FK ve sequence kontrolü; PostgreSQL COPY kısıt ihlalinde transaction rollback doğrulandı. PostgreSQL→SQLite yolunda SQLite `foreign_keys`/`synchronous` ayarları hata/rollback dahil önceki değerlerine iade edilir. | Kaynak/ürün teslimi tamamlandı. Gerçek müşteri verisi, hacim, credential ve canlı geçiş kabulü yapılmadı; dağıtım operasyonunda doğrulanmalıdır. Kanıt: [A-19 doğrulaması](TEST-DOGRULAMA-2026-10-08.md#a-19-datasync-izole-iki-sağlayıcı-doğrulaması). |
| A-20 Tarih semantiği | 🟢 | **Kod teslimi tamamlandı:** Üretim Web/Shared C#/Razor kaynaklarında `DateTime.Now` kullanımı sıfırlandı. Olay/audit anları UTC; iş tarihi, takvim, form varsayılanı ve gecikme kararları İstanbul `BusinessTime` ile; yedek çalışma penceresi İstanbul takvimi ve UTC kayıt anı üzerinden hesaplanır. UTC son yedek saati arayüzde İstanbul’a çevrilerek gösterilir. | Eski DB timestamp’leri topluca dönüştürülmedi; geçmiş veri korunur ve okuma sözleşmesi UTC kabul eder. Yeni kod sunucu OS saat diliminden bağımsızdır. Müşteri geçmiş verisi anlam/parity kabulü, yayına çıkışta ayrıca kontrol edilir. |
| A-21 Müşteri paketi | 🟡 | Güncelleme paketi ana/eski müşteri AppId'lerini ve gerçek kurulum dizinini buluyor, belirsizlik/eksik EXE durumunda reddediyor; temiz kurulum mevcut uygulama/DB ayarını ezmiyor. ACL koruması eksik eski doğrudan çalıştırma varyantı yeni satış paket üretiminden çıkarıldı. IIS ana paketi Web + DataSync içerir; lisans üreticisi yoktur. | Ana IIS paketinin temiz Windows hedefinde kurulum/güncelleme, veri/ayar ACL'si, yedek restore, secret yükleme ve lisans sürüm hakkı kabulü gerekir. Eski müşteri varyantı dağıtıma alınmaz. |
| A-22 Bağımlılıklar | 🟢 | Yeni test projesi dahil çözümdeki yedi proje ve çözüm dışı Rent-a-Car kontrolü doğrudan/geçişli NuGet taraması kapsamındadır; LisansDesktop'un açık bildirimli SQLite kütüphanesi 2.1.13'e yükseltildi. Windows CI NuGet işi restore ve taramayı başarıyla tamamladı; [tarama kaydı](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md), [CI kanıtı](A-07-CI-DOGRULAMA-2026-10-08.md). | Tarama anında bilinen NuGet açıkları bulunmadı. Yeni bildirimler için CI taraması sürer; müşteri paketinin kurulum kabulü A-21'dedir |
| A-23 Doküman/teslim | 🟢 | [Belge/teslim kararı](A-23-TESLIM-KARARI-2026-10-08.md) ile güncel 31 görev kaynağı ve tarihsel belgelerin yeri sabitlendi. Yerel çalışma zamanı ayarları Git/publish/kurulum girdisinden çıkarıldı; Web publish çıktısında bulunmadıkları doğrulandı. | Hedef makine kurulum ve müşteri kabulü A-21, eski sırların rotasyonu A-06 kapsamında sürer. |

Kaynak taraması (2026-10-09), doğrudan legacy okuyucuları `BelgeVersiyonService` (EBYS), `DestekTalebiService` (destek eki), `FaturaService` (PDF/XML) ve `FileService` (ortak eski uploads kökü) olarak somutlaştırdı. Yolların kök sınırları kodda uygulanıyor; gerçek firma/sahiplik, yetki, hata bildirimi ve restore sonrası okuma kabulü yapılmadı. Süresiz saklama kararı nedeniyle bu eski-okuma uyumluluğu kaldırılmadan önce müşteri geçiş/geri yükleme kanıtı gerekir.

## P2/P3 — Ürün kapsamı ve sonraki kabul

| Görev | Durum | Yapılan | Kalan iş / kapanış kanıtı |
|---|---|---|---|
| A-24 Çok süreçli cache | 🟡 | Üretim `CRMFilo:` anahtarlı iş listeleri ve dashboard artık her istekte veri kaynağından okunuyor; `Get/Exists/Set/Refresh/Remove` bu anahtarlarda önbellek kullanmıyor. Böylece cache erişimi veya kaybolan invalidation iş verisini bayat gösteremiyor. Araç listesi de doğrudan DB okuyor. Jenerik cache protokolü ayrı anahtarlar için korunuyor. | Hedef müşteri hacminde doğrudan DB sorgu yükü ve çoklu sunucu çalışma zamanı kabulü; kalıcı, transaction bağlı cache yeniden devreye alınacaksa ayrı tasarım. A-25'in Redis kabulü yalnız jenerik cache protokolü için açıktır. |
| A-25 Cache kabulü | 🟡 | Ortak MemoryDistributedCache kullanan bağımsız servis örneklerinde prefix invalidation, bekleyen factory, iptal ve backend arızası için 4 regresyon geçti; güncel tam Release paketi 42/42. | Gerçek Redis/ağ kesintisi ve yeniden bağlanma, çok süreçli yük ve geniş kapsamlı invalidation maliyeti. Docker istemcisi var; Docker daemon bağlantısı bu ortamda kullanılamadı. |
| A-26 Excel/PDF | 🟡 | Personel banka baskı stili ve ihale XLSX/PDF üretimi | Uzun metin, çok sayfa, negatif tutar, SGK ve toplam eşitliği görsel kabulü. |
| A-27 Dış entegrasyon | 🟡 | HTTP retry ve Luca kaynak düzeltmeleri. Retry handler her denemede klon isteği dispose eder, başarılı response özgün isteğe bağlanır ve HTTP `VersionPolicy` korunur; belirsiz idempotent olmayan istekler retry edilmez. Web Release derlemesi geçti. | Gerçek UBL/portal, eski credential ve belirsiz mali POST kabulü. |
| A-28 DB sağlayıcıları | 🟢 | **Tamamlandı:** PostgreSQL/SQLite kapsamı sabit; SQL Server/MySQL seçenekleri kaldırıldı ve runtime'da reddediliyor. Eski ayarda açık mesaj verilir ve dosyaya yazılmaz; tam yerel test paketi **137/137** geçti. | Hedef temiz kurulum/yükseltme A-18/A-21 dağıtım kabulünde takip edilir; sağlayıcı kapsamı kod teslimi kapanmıştır. |
| A-29 Mali politikalar | 🟢 | Fatura, muhasebe ve ödeme eşleştirme yazımları güncel DB iznini denetler. Puantaj faturası, kalemi, otomatik muhasebe fişi ve finans linki; hakediş fatura/durum/snapshotı ve ödeme eşleştirme/türetilen fatura toplamı Serializable transaction içinde yazılır. Fiş hatası dış transaction varken yutulmaz. Sıradan `FaturaService.CreateAsync(Fatura)` de A-09 düzeltmesiyle execution strategy ve Serializable transaction içinde fatura/kalem/karşı fatura/otomatik fişi kaydeder; transaction içi fiş hatası dışarı taşınır. | Kod düzeltmesi Release derlemesinden geçti; hedef PostgreSQL/SQLite ve gerçek rol değişimi kabulü ayrıca yapılmalı. |
| A-30 Kod/belge düzeni | 🟢 | [Satış sürümü refactor kararı](A-29-31-URUN-KARARLARI.md): davranış değiştirmeyen P3 temizliği ertelendi, hata düzeltmesi kapsamı ayrı tutuldu. | Refactor backlog'a ertelendi; satış engeli olarak izlenmiyor. |
| A-31 Çevrimdışı/depolama | 🟢 | [Ürün kapsamı](A-29-31-URUN-KARARLARI.md): çevrimdışı kullanım yok; tek düğümde Local, yapılandırılmış ortak depoda S3; yedek sınırı ve log/audit ayrımı tanımlandı. | A-04/A-10/A-11'deki gerçek restore, S3 ve çok sunucu kabulleri ayrı görevlerde sürer. |

## Çalışma sırası

1. P0: A-02 modül erişimi, A-04 bağımsız kurtarma, A-05 tenant/oturum ve A-06 sır rotasyonu kabulü. Yeni satış için A-01 lisans üretimi kapandı; eski müşteri lisans eşlemesi yenileme/geçiş operasyonudur.
2. A-18'in boş veritabanı baseline'ı tamamlandı. Temsilî eski PostgreSQL/SQLite şeması üzerinde veri/tenant/mali parity ve yarım migration geri dönüşünü kanıtla.
3. Kalan 13 sarı görevi satırlardaki kapanış kanıtıyla kapat; müşteri/harici ortam kabulleri için yetkili iş sahibi ve geçici erişim gerekir.
4. [Ürün kararları](A-29-31-URUN-KARARLARI.md) ve [son-aşama test planı](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) kapsamını koru; test edilmemiş veya müşteri kabulü alınmamış işleri yeşil sayma.

## Değişiklik günlüğü

### 2026-10-09 — 17 açık görev için Go/No-Go takvimi ve A-20 düzeltmesi

- 🟢 Son doğrulama güncel çalışma ağacında tekrarlandı: Release çözüm derlemesi **0 uyarı / 0 hata**; otomatik test paketi **137/137 geçti, 0 atlandı**.
- 🔴 A-18'in temiz kurulum kök nedeni somutlaştırıldı: ilk `Init` migration'ı boş ve devamındaki zincir legacy şema varsayımlarına dayanıyor. Boş PostgreSQL otomatik migration'a alınmıyor; A-18 satış öncesi kod kapısıdır.
- 🟢 A-20 araç/plaka gün sınırları host timezone bağımlılığından çıkarıldı; `Europe/Istanbul` `BusinessTime.Today` eklendi ve UTC gece sınırı regresyonu geçti. Eski kayıtlar dönüştürülmedi.
- 🟡 17 sarı görev için ortam/rol ve iş sahibi girdileri ile kapanış kanıtları kaydedildi. A-18 baseline eforu güvenilir biçimde tahmin edilemediği için tarih verilmedi; önce baseline tasarımı/parity kapsamı gerekir. Önkoşullar netleşmeden Go/No-Go günü atanmayacak. Genel renkler **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz** olarak korundu.

### 2026-10-09 — Görev renklerinin kaynak/kanıt üzerinden yeniden denetimi

- 🟢 A-01–A-31 satırları güncel görev kanıtlarıyla yeniden karşılaştırıldı. Mevcut kapsam kararları ve doğrulama kayıtlarıyla **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz** sayımı tutarlı; yeni kanıt hiçbir sarı görevin tanımlı kapanış ölçütünü bütünüyle karşılamıyor.
- 🟡 A-02 cari REST yazma izinleri ve A-17 analitik üst limit düzeltmeleri uygulandı; A-02'nin diğer API eylem/rol matrisi ve A-17'nin gerçek CSV/XLSX, sağlayıcı, PDF/SMTP kabulleri açık. A-18 indeks hatasını artık gizlemiyor; temiz/eski PostgreSQL/SQLite başlangıç ve duplicate veri kabulü yapılmadı.
- 🟢 A-29 görev satırındaki sıradan fatura muhasebesinin hâlâ ayrı best-effort çağrı olduğu eski ifade düzeltildi; güncel kaynakta ana `CreateAsync(Fatura)` zinciri execution strategy + Serializable transaction içindedir. A-09'un hedef sağlayıcı/commit kabulü nedeniyle A-09 sarı kalır.
- 🟡 Kalan sarı görevlerin müşteri, sır rotasyonu, CI kapsama, dosya deposu, UI, kurulum, gerçek DB, tarih geçişi, cache ve dış entegrasyon koşulları bu çalışma ağacında kanıtlanmamıştır; renkleri korunur.

### 2026-10-09 — A-02/A-17 cari REST yazma izinleri

- 🟢 Cari REST oluşturma/düzenleme/silme uçları Bearer ve lisans dışında güncel rol izinlerini de doğruluyor (`cariler.yaz`, `cariler.duzenle`, `cariler.sil`); izin yoksa işlem gövdesi çalışmadan 403 döner.
- 🟡 Gerçek normal/Admin token rol matrisi ve API kabulü çalıştırılmadı. A-02/A-17 sarı kalır.

### 2026-10-09 — A-17 analitik API kayıt sınırları

- 🟢 Fatura, cari ve araç analitik uç noktaları istemciden istenen `top` değerini üst sınırsız doğrudan SQL `Take` değerine veriyordu; azami kayıt miktarları (10.000/5.000/2.000) enforce edilir, küçük/negatif istekler en az 1'e çekilir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Test çalıştırılmadı.
- 🟡 Rol/firma, dış entegrasyon ve sağlayıcı kabulü açık; A-17 sarı kalır. Toplam **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-18 fatura benzersiz indeksi startup fail-closed

- 🟢 Başlangıçtaki fatura firma/yön/numara unique-index hazırlığı önce eski indeksi silip sonra yenisini kuruyordu; hata yakalanıp yalnızca konsola yazıldığından uygulama korumasız açılabiliyordu. Yeni sıra transaction içinde önce unique indeksi kurar, sonra eski indeksi kaldırır; hata artık startup'a yayılır.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Test ve gerçek PostgreSQL/SQLite startup provası çalıştırılmadı.
- 🟡 Çakışan eski müşteri faturaları ve kurulum/yükseltme kabulü açık; A-18 sarı kalır. Toplam **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02/A-17 analitik API rol izni

- 🔴 Analitik OData, Grafana, Prometheus ve n8n kayıt uçları lisanslı rapor modülü arkasında olsa da `raporlar.oku` rol izni kontrolü yoktu.
- 🟢 Bütün veri döndüren analitik uçlara güncel veritabanı izin denetimi eklendi; reddedilen çağrı 403 döner. Anonim Grafana search yalnız metrik isimlerini döndürür.
- 🟢 Web Release build **0 uyarı / 0 hata**. Normal/Admin ve firma kabulü çalıştırılmadı; A-02/A-17 sarı kalır. Güncel renk sayısı **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02/A-17 fatura grup şablonu izinleri

- 🔴 Şablon API'si tenant ve sahiplik kapsamı sunsa da eylem bazlı rol kontrolünü atlıyordu.
- 🟢 Okuma uçları `faturahazirlik.oku`, oluşturma `faturahazirlik.yaz`, düzenleme ve kaldırma uçları `faturahazirlik.duzenle` ile korunuyor; servis katmanının ortak şablon kontrolü korunuyor.
- 🟢 Web Release build **0 uyarı / 0 hata**.
- 🟡 Gerçek rol/firma kabulü alınmadı. A-02/A-17 sarı kalır; güncel renk **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-06/A-21 eski PC2 üretim ayarları

- 🔴 Tarihsel PC2 yayın akışı örnek ayar JSON'u içinde DB parolası/JWT sır yer tutucusu taşıyor ve `appsettings.Production.json` olarak kullanılmasını öneriyordu.
- 🟢 Eski betik sır dosyası üretmeyi bıraktı; paket/PC2 yönergeleri güncel installer `dbsettings.json` ACL akışına ve harici `Jwt__Secret` sağlayımına yönlendirildi. Boş PostgreSQL zinciri desteklenmiş varsayılmıyor; A-18/A-21 hedef DB kabuline bağlandı.
- 🟢 `03-pc2-publish.ps1` PowerShell parser kontrolünden geçti. Inno installer üretimi ve hedef Windows runtime kabulü çalıştırılmadı.
- 🟡 Aktif sır rotasyonu ve gerçek hedef kurulum henüz yapılmadı. A-06/A-21 sarı; dağılım **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-21/A-28 kurulum sağlayıcı seçicisi

- 🔴 Ana ve müşteri Inno kurulumundaki desteklenmeyen MSSQL seçeneği kaldırıldı.
- 🟢 Setup README sağlayıcı kapsamıyla eşitlendi. Inno script kaynakları statik olarak incelendi; kurulum EXE'si derlenmedi.
- 🟡 Gerçek hedef makine kabulü A-21'de açık kalır; A-28 kod teslim kapsamı kapanmıştır.

### 2026-10-09 — A-02/A-17 fatura REST yazma yetkisi

- 🔴 Fatura Create uç noktası cari kaydını/istek alanlarını yetki kontrolünden önce okuyabiliyor; durum güncelleme ve silme controller'ları servis katmanındaki izin reddini 403'e çevirmiyordu.
- 🟢 Create gelen/kesilen fatura yazma iznini iş verisine erişmeden doğrular. Okuma/durum/silme eylemleri de yön bazlı güncel izinleri denetler; yetkisiz faturalar varlık bilgisini açığa çıkarmadan 404 ile gizlenir. Servis katmanı yazma denetimi ikinci savunma olarak korunur.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` hata vermedi.
- 🟡 Normal/Admin ve yön bazlı gerçek HTTP kabulü çalıştırılmadı. A-02/A-17 sarı; güncel sayım **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-18 boş PostgreSQL migration preflight

- 🔴 İzole PostgreSQL 17.5 boş DB'de önceki skip yolu `__EFMigrationsHistory` tablosu yokken migration kaydı yazmayı deniyordu; bunu takiben migration zinciri `Firmalar` ve daha sonra `AylikOdemeGerceklesenler` varsayımlarında duruyordu.
- 🟢 Gerçek core firma/kullanıcı tabloları olmayan PostgreSQL, legacy migration skip dalına girmeden açık kurulum hatasıyla durur. Legacy history helper'ı history tablosunu transaction içinde idempotent oluşturur ve aynı migration kaydını çakışmasız yazar.
- 🟢 Ayrı geçici PostgreSQL 17 kümesinde temiz DB başlangıcı fail-fast doğrulandı; reddedilen DB'de public tablo sayısı **0** kaldı ve geçici sunucu kapatıldı. Web Release derlemesi ayrıca geçti.
- 🟡 PostgreSQL temiz kurulum zinciri hâlâ teslim edilmedi; gerçek eski şema migration/rollback ve müşteri DB kabulü açık. A-18 sarı; toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-18 model/snapshot sapması ve GPS kaldırma kararı

- 🟡 EF model API'si `HasPendingModelChanges=True` ve **141 migration** bildirdi. Scaffold'ın GPS dışındaki timestamp dönüşümleri ayrı inceleme için bırakıldı.
- 🟢 Araç takip GPS kapsamdan çıkarıldı; beş GPS entity/ilişkisi snapshot'tan temizlendi ve tabloları kaldıran `20261009200000_RemoveVehicleGpsTracking` migration'ı eklendi. Migration henüz müşteri DB'sine uygulanmadı; uygulanınca eski GPS verisi silinir ve `Down` ile geri getirilemez.
- 🟡 A-18 temiz PostgreSQL başlangıç zinciri, kalan model farkları ve müşteri DB yükseltme/rollback kabulü açık; toplam renkler **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-11 legacy dosya yolu sınır düzeltmesi

- 🟢 `StartsWith(root)` ile kardeş klasörlerin kök altında sayılabilmesi düzeltildi. Ortak `StorageFilePath` göreli yol bileşenlerini doğrular ve legacy çözümlemede sembolik bağlantı geçişini reddeder; fatura, EBYS, destek eki ve arşiv kök kontrolleri bu davranışı kullanır.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Test çalıştırılmadı.
- 🟡 S3/MinIO, gerçek disk izin/kilit ve UI kabulü açık; A-11 sarı kalır. Toplam **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-15 banka hareketi yardımcı tenant bağlantıları

- 🟢 `BankaKasaHareketleri` için uygulama katmanındaki yardımcı tenant bağlantı kontrolleri daha önce mevcutken bunları veritabanında zorunlu kılan `20261009150000_GuardBankMovementAuxiliaryTenantLinks` migration'ı eklendi. Korunan bağlar: `PersonelCebindenId`, `AracId`, `AracMasrafId`, `MahsupHareketId` ve `PersonelGeriOdemeHareketId`; referanslı araç, masraf veya şoförün firma değiştirmesi de engellenir.
- 🟢 Hem `20261009150000_GuardBankMovementAuxiliaryTenantLinks` hem de ana `20261006200000_GuardBankMovementTenantLinks` PostgreSQL migration'ı ilgili tabloları `SHARE ROW EXCLUSIVE` modunda önceden kilitler. Her iki migration'da eski satır preflight'ı ile trigger kurulumu arasında yeni yazım yarışı engellenir; kilit/preflight/trigger sırası regresyon testinde sabitlenmiştir.
- 🟢 SQLite migration testleri **14/14**; banka hareketi, ödeme eşleştirmesi, servis ve PostgreSQL kilit sırası regresyonları birlikte **68/68** geçti. İzole test build'i **0 uyarı / 0 hata**.
- 🟡 (2026-10-09 ara durum kaydı; aynı gün sonraki kapsam kararıyla geçersiz kılındı.) PostgreSQL/müşteri DB kabulü yapılmamıştı. Bu operasyonel sınır, aşağıdaki kapanış kararında A-15 rengini açık tutmaz.

### 2026-10-09 — A-28 kapsam kararı ve odaklı regresyon doğrulaması

- 🟢 PostgreSQL/SQLite destek kapsamı ürün kararı olarak sabitlendi; SQL Server/MySQL desteği bu sürümün kapsamına alınmadı. Ayar ekranı, runtime sağlayıcı denetimi ve desteklenmeyen eski ayarla test/apply sırasında dosyaya yazmama davranışı güncellendi.
- 🟢 Web derlemesi **0 hata / 0 uyarı**; `DatabaseProviderScopeTests` **6/6** geçti. A-03/A-10/A-15/A-28 hedefli test filtresi **32/32** geçti. A-03 aktarım/kurtarma PowerShell betiklerinin altısı parser denetiminden geçti.
- 🟡 Gerçek PostgreSQL/SQLite temiz kurulum-yükseltme, IIS kurtarma, çoklu sunucu dosya yaşam döngüsü ve müşteri verisi kabulü yapılmadı. Kabul DB'sine bağlanılmadı veya değişiklik uygulanmadı. A-03/A-10/A-15/A-16 kırmızı; A-28 sarı. Güncel toplam **5 yeşil / 22 sarı / 4 kırmızı / 0 beyaz**.

### 2026-10-08 — A-28 desteklenmeyen DB sağlayıcısında güvenli duruş

- 🟢 Başlangıç, `DbInitializer` çalışmadan önce SQL Server/MySQL seçimini reddediyor; hata mesajı desteklenen sağlayıcıları ve migration yapılmadığını belirtiyor.
- 🟢 Veritabanı ayar servisi desteklenmeyen sağlayıcıda bağlantı testi, ayar kaydı ve geçiş başlatmayı reddediyor. Ayarlar ekranı SQL Server/MySQL seçeneklerini yeni seçim için kapatıyor; eski ayarla açılmışsa kırmızı uyarı ve PostgreSQL/SQLite'a dönüş yolu gösteriyor.
- 🟡 Web Release derlemesi ve gerçek PostgreSQL/SQLite temiz kurulum-yükseltme hedef kabulü tamamlanmadan A-28 kapanmaz. SQL Server/MySQL ürün desteği de tamamlanmadı; görev 🔴, toplam renkler **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**.

### 2026-10-08 — A-16 PostgreSQL yabancı anahtar ihlal taraması

- 🟢 Salt okunur envanter, PostgreSQL `information_schema` metadata'sından tekli ve composite FK'leri bulup referans kayıtları kontrol ediyor. Örnek kimlik alanı metadata'daki gerçek harf biçimiyle okunuyor; tenant FK keşif sorgusunun metadata join'i de düzeltildi.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Geçici PostgreSQL 17 kümesinde sentetik tekli ve composite sahipsiz FK kayıtları ayrı ayrı **1'er** bulundu; deneme şemasında diğer sabit tablolar eksik olduğu için rapor `Complete=false`/çıkış 3 verdi.
- 🔴 Gerçek müşteri şeması, composite FK firma uyuşmazlığı, tanımsız iş ilişkileri ve kontrollü onarım kabulü açık; A-16 kırmızı kalır, renk sayımı değişmez.

### 2026-10-08 — A-16 composite tenant FK denetimi

- 🟢 SQLite ve PostgreSQL tenant taramaları tek sütunlu FK sınırlamasından çıkarıldı; tüm kaynak/hedef FK sütunları beraber eşleştirilir, ardından `FirmaId` karşılaştırılır.
- 🟢 Geçici PostgreSQL 17 verisinde farklı firma kimliğine bağlanan tek composite FK `A16-TENANT-*` ile **1** bulgu verdi. DataSync Release derlemesi **0 uyarı / 0 hata**.
- 🔴 Gerçek müşteri verisi ve kontrollü onarım kabulü yok; A-16 kırmızı, toplam renk sayımı değişmedi.

### 2026-10-06 — A-10 yerel nesne deposu yol ve upload güvenliği

- 🟢 `LocalObjectStorageService` artık boş/köklenmiş anahtarları reddeder ve normalize edilmiş hedefin uploads dizini altında kaldığını denetler. Dosya upload'ı geçici dosyadan son ada taşınır; silme öncesi `File.Exists` kontrolü kaldırıldı.
- 🟢 Başarılı atomik upload, kök dışına çıkma reddi ve var olmayan dosya silme davranışı için 2 kalıcı test eklendi. Release test paketi **25/25 başarılı**.
- 🔴 Kalıcı cleanup kuyruğu, çok süreçli yarış/TOCTOU ve gerçek storage hataları ayrıca doğrulanmadı; A-10 kırmızı kalır.

### 2026-10-06 — A-10 depolama yollarında sembolik bağlantı kontrolü

- 🟢 `StorageFilePath.Resolve` artık depolama içindeki mevcut yol bileşenlerini inceliyor ve sembolik bağlantı üzerinden hedefe gitmeyi reddediyor. `LocalObjectStorageService` de aynı ortak çözümleyiciyi kullanıyor.
- 🟢 Release test paketi **25/25 başarılı**, `git diff --check` temiz. Sembolik bağlantı testi Windows ayrıcalık hatasıyla çalışmadı; bu ortamda saldırı senaryosu doğrulanmış sayılmaz.
- 🔴 Var olan bağlantı kontrolü ile dosya işlemi arasında TOCTOU penceresi, kalıcı cleanup kuyruğu, yetim envanteri ve gerçek depoda arıza kabulü açık; A-10 kırmızı kalır. Sayım değişmedi: **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz**.

### 2026-10-06 — A-10 eksik üst klasör için idempotent silme

- 🟢 Yerel nesne deposu silmeden önce üst dizini sorgular; dizin yoksa dosya da yok kabul edip döner. `File.Exists` kullanılmadığından erişim/IO hataları yutulmaz.
- 🟢 Üst dizin hiç oluşturulmamış senaryo için regresyon testi eklendi; Release paketi **26/26 başarılı**, `git diff --check` temiz.
- 🔴 Bu davranış retry kuyruğu değildir; kalıcı temizleme kuyruğu, çok süreçli yarış/TOCTOU, yetim envanteri ve gerçek storage kesinti kabulü açık. A-10 🔴 kalır; görev sayımı değişmez.

### 2026-10-06 — A-10 personel özlükte paylaşılan dosya yolunu koruma

- 🟢 `DeleteEvrakDosyaAsync`, DB satırını kaldırdıktan sonra fiziksel dosyayı silmeden önce `PersonelOzlukEvraklar` ve `PersonelOzlukEvrakVersiyonlar` tablolarını filtreleri yok sayarak kontrol eder. Eski aktif/sürüm kaydı aynı yolu kullanıyorsa dosya tutulur.
- 🟢 Release build geçti; kalıcı test takımının tamamı **23/23 başarılı**. Bu tur için aynı yol senaryosuna özel DB entegrasyon testi yoktur.
- 🔴 Kontrol ile silme arasındaki çok süreçli yarış, kalıcı kuyruk/yeniden deneme ve diğer tablo referansları açık; A-10 kırmızı kalır.

### 2026-10-06 — A-10 şifreli upload'ın atomik yayımlanması

- 🟢 `SecureFileService.SaveEncryptedAsync` artık şifreli içeriği son dosya adına doğrudan yazmaz. Aynı klasörde benzersiz geçici dosyaya yazar ve tamamlandıktan sonra son ada taşır; hata/iptalde geçici dosyayı silmeyi dener.
- 🟢 `SecureFileAtomicSaveTests` başarılı upload'da tam içeriği ve geçici dosya kalmadığını; iptal edilen upload'da yayımlanmış/geçici dosya kalmadığını doğruladı. Güncel Release test toplamı **23/23 başarılı**.
- 🔴 Bu, kalıcı cleanup kuyruğu ve diğer dosya yaşam döngüsü risklerini kapatmaz. A-10 kırmızı kalır; sayım değişmez.

### 2026-10-06 — A-07 lisans protokolü regresyonları

- 🟢 `LicenseProtocolTests` ile modül zarflarının kanonik biçimi, bozuk/tekrarlı/bilinmeyen modüllerin reddi, RSA-PSS imzasının seçilen modüllere bağı ve sürüm hakkı sınırları kalıcı teste alındı. Release `dotnet test` sonucu **21 başarılı / 0 başarısız**.
- 🟡 Test geçici RSA anahtarıyla ortak imza protokolünü sınar; üretim açık anahtarıyla gerçek müşteri lisansının yüklenmesi, modül yetki ekranı/API uçtan uca kabulü ve yedek geri yükleme açık. A-01, A-02 ve A-07 renkleri 🟡 kalır; toplam sayım değişmez.

### 2026-10-06 — A-07 kalıcı test projesi ve zorunlu CI

- 🟢 `MKFiloServis.Tests` çözümde oluşturuldu. A-15 SQLite migration SQL'i için eski veri ön kontrolü, geçerli ekleme ve yedi hatalı firma bağı değişikliğini kapsayan **9 xUnit testi** Release olarak yerelde geçti.
- 🟢 `.github/workflows/tests.yml` artık test projesi yoksa sessizce geçmiyor; restore, build ve `dotnet test` zorunlu. Kapsam toplama bu büyük Web assembly'sinde yerelde uzun süre bitmediği için CI'den çıkarıldı; TRX sonucu saklanır.
- 🟢 Yeni test projesi eklendikten sonra çözümdeki yedi projenin doğrudan/geçişli NuGet bildirimi yeniden tarandı; bildirilen açık yok. A-22 🟢 durumu korunur.
- 🟡 GitHub CI sonucu ve diğer kritik regresyonlar henüz yok. A-07 🟡; sayım **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz**.

### 2026-10-06 — Envanter ve A-23 belge bağlantısı

- 🟢 A-01…A-31 satırları yeniden sayıldı: 1 yeşil, 20 sarı, 7 kırmızı, 3 beyaz. Önceki üst özetin 6 kırmızı/4 beyaz sayım hatası düzeltildi.
- 🟢 2026-10-02 tarihli eksik yeniden analiz dosyasına giden bağlantılar bu **yeni güncel envantere** yönlendirildi. Eski 39 bulguluk dosyanın içeriğinin geri getirildiği iddia edilmez.
- 🟢 İkinci denetim raporundaki bulunamayan ilk denetim dosyasına giden bağlantı kaldırıldı. `docs/*.md` içindeki yerel Markdown hedefleri tarandı; kalan kırık bağlantı bulunmadı.
- 🟢 `git ls-files` taramasında izlenen `.log`, `test_all.txt`, `setup/output`, `payload`, `.exe`, `.zip`, `.bak` veya `.dump` teslim artığı bulunmadı. İzlenen iki `.sql` dosyası kaynak betiğidir (`scripts/run-engine.sql`, `scripts/seed-test-data.sql`). `.gitignore` yerel çıktı, veritabanı ve yedek dosyalarını dışlıyor.
- 🟡 Çalışma ağacında çok sayıda değiştirilmiş kaynak ve rapor, yeni migration/restore betikleri ve ayrıca kullanıcıya ait olabilecek `.kilo/` yapılandırması izlenmemiş durumdadır. Bunlar otomatik silinmedi, stage edilmedi veya teslim commit'i yapılmadı. A-23 kırmızı kalır.
- 🟡 A-23 teslim kapanışı için belge saklama kararı, yerel çıktı/ham logların gözden geçirilmesi ve teslim commit'i kalır. Güncel Git durumunda izlenen dosya silinmesi görülmedi.

### 2026-10-06 — A-15 doğrudan veritabanı yazımlarında firma bağı

- 🟢 `20261006200000_GuardBankMovementTenantLinks` migration'ı PostgreSQL ve SQLite için eklendi. Banka/Kasa hareketindeki banka hesabı, isteğe bağlı cari ve personel geri ödeme hesabı firma eşleşmesi INSERT/UPDATE tetikleyicilerinde denetlenir. İlişkili hesap veya carinin firma alanını sonradan değiştirme de reddedilir.
- 🟢 Migration, mevcut hareketlerde uyuşmayan veya bulunmayan ilişkileri önceden tarar ve veri onarmadan durur. `Down` yalnız bu migration'ın tetikleyici ve fonksiyonlarını kaldırır.
- 🟢 Web Debug derlemesi `setup/output/a15-db-tenant-guard-build` konumuna 0 uyarı, 0 hata ile tamamlandı.
- 🟡 Gerçek PostgreSQL/SQLite migration, hata enjeksiyonu, eski kayıt onarımı ve eşzamanlı DB yazımı kabulü yapılmadı. Diğer A-15 tenant bağlantıları da açık; görev 🔴 kalır.

### 2026-10-06 — A-22 güncel NuGet taraması ve lisans aracı kütüphanesi

- 🟢 Çözümdeki altı proje ve çözüm dışındaki Rent-a-Car kontrol projesi doğrudan/geçişli NuGet güvenlik bildirimi için tarandı. İlk tarama LisansDesktop'un geçişli `SQLitePCLRaw.lib.e_sqlite3 2.1.11` sürümünde yüksek önem dereceli bir bildirim gösterdi.
- 🟢 LisansDesktop projesi native kütüphaneyi 2.1.13'e yükseltti; Release/win-x64 dahili EXE yeniden yayımlandı. Son taramada yedi projede geçerli kaynakların bildirdiği bilinen açık kalmadı. CI taraması komut hatasında artık başarısız olur ve çözüm dışı projeyi de kapsar. [A-22 kanıtı](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md).
- 🟢 A-22 görev rengi yeşile güncellendi; envanter **2 yeşil / 19 sarı / 7 kırmızı / 3 beyaz**. Müşteri paketi kurulum kabulü A-21 kapsamında sarı kalır.

### 2026-10-06 — A-15 PostgreSQL/SQLite izole DB doğrulaması

- 🟢 Migration SQL'i SQLite bellek DB ve ayrı PostgreSQL 17 geçici kümesinde uygulandı. Eski farklı firma hesabı ön kontrolde durdu; geçerli hareket yazıldı. Her sağlayıcıda yedi hatalı değişiklik reddedildi; mevcut hareketin hesap/cariyle birlikte firma taşıması da DB sınırında engellendi.
- 🟢 Son Web Debug derlemesi 0 uyarı/0 hata. [Senaryo ve sınırlar](A-15-IZOLE-FIRMA-BAGI-DOGRULAMA-2026-10-06.md).
- 🟡 Asgari şema doğrulaması tam model/müşteri migration ve eşzamanlılık kabulü değildir. Diğer A-15 bağlantıları açık; A-15 🔴 kalır. Renk sayımı değişmedi.

### 2026-10-06 — A-10 idempotent dosya silmeyi ortaklaştırma

- 🟢 Eksik üst klasör denetimi `StorageFilePath.DeleteIdempotently` içine alındı ve hem şifreli dosya servisinde hem yerel nesne deposunda kullanıldı.
- 🟢 Release test paketi **26/26 başarılı**, `git diff --check` temiz. Üst dizin yok testi ortak metodu yerel nesne deposu üzerinden doğruluyor.
- 🔴 Şifreli depoda bu durum için ayrı servis entegrasyonu testi, kalıcı cleanup kuyruğu, TOCTOU/çok süreçli yarış, yetim envanteri ve gerçek storage arıza kabulü açık. A-10 🔴 kalır; sayım değişmedi.

### 2026-10-06 — A-10 legacy dosya taşımasında DB öncelikli silme

- 🟢 `DosyaMigrasyonService`, eski açık dosyayı artık yeni şifreli yolu DB'ye yazmadan silmiyor. SaveChanges hatasında yeni yol taze bağlamla doğrulanmadan eski dosyayı silmez; eski yol başka bir DB kaydınca kullanılıyorsa korur.
- 🟢 Eski uploads yolu çözümlemesi uploads köküyle sınırlandı ve sembolik bağlantı bileşenlerini reddeden ortak çözümleyiciden geçiyor. Release test paketi **26/26 başarılı**, `git diff --check` temiz.
- 🔴 Commit sonrası süreç kesilirse kalan eski açık dosyalar için kalıcı yeniden deneme kuyruğu yoktur. Gerçek eski DB/dosya migration kabulü, orphan taraması ve TOCTOU/çok süreçli kontrol açık; A-10 🔴 kalır.

- 🟢 Referans koruması aynı zamanda ortak evrak, özlük aktif/sürüm, destek eki, tedarikçi eki, fatura PDF/XML ve proforma PDF yollarını da kapsar; soft-delete filtreleri yok sayılarak kontrol edilir. Son Release test sonucu bu ek sorgularla da 26/26 başarılıdır.

### 2026-10-06 — A-10 migration ekranı yönetici sınırı ve kapsam açıklığı

- 🟢 `/ayarlar/dosya-migrasyonu` artık `Admin` rolü gerektirir; modül lisans politikası da korunur. Yetkisiz kullanıcılar lisanslı EBYS modülüne sahip olsa bile sistem dosyalarını taşıyamaz.
- 🟢 Ekran ve servis açıklaması gerçek kapsamı söylüyor: aktif firma filtreleri altındaki EBYS/araç ana ve sürüm kayıtları. Önceki “tüm düz metin dosyaları” ifadesi düzeltildi. Release test paketi **26/26 başarılı**, `git diff --check` temiz.
- 🔴 Soft-deleted kayıtlar ve diğer belge türleri kapsam dışı; tenant/rol gerçek kullanıcı kabulü, kalıcı temizleme kuyruğu ve orphan taraması açık. A-10 kırmızı kalır.

### 2026-10-06 — A-10 şifreli dosya yetim envanteri

- 🟢 Admin evrak bakım raporuna uploads, Arsiv ve Depo köklerinde sembolik bağlantı olmayan `.enc` dosyalarının envanteri eklendi. DB'deki bilinen `DosyaYolu`, `PdfDosyaYolu`, `XmlDosyaYolu` alanları soft-delete filtreleri yok sayılarak okunur; eşleşmeyen dosyalar yol/boyut/değişiklik tarihiyle gösterilir.
- 🟢 Bu envanter salt okunurdur: bakım ekranı orphan dosyalarını otomatik silmez. Scanner testi uploads/Arsiv/Depo karşılaştırmasını ve diske dokunulmadığını doğrular. Release paketi **27/27 başarılı**, `git diff --check` temiz.
- 🔴 Yalnız `.enc` dosyaları ve bilinen alanlar taranır; eski düz metin dosyaları, gerçek müşteri DB'si, dosya sistemindeki yarış ve kalıcı cleanup/retry akışı kapsanmaz. A-10 kırmızı kalır; toplam renk sayımı değişmez.

### 2026-10-06 — A-10 kalıcı dosya temizleme kuyruğu

- 🟢 Silme isteği, Data Protection ile şifrelenen JSON günlüğüne atomik yazılıyor; süreçler arası günlük kilidi, idempotent kayıt/tamamlamalar ve artan retry aralığı mevcut. Web worker başlangıçtan bir dakika sonra kuyruğu tarıyor; silmeden önce bilinen DB dosya yolu alanlarını `IgnoreQueryFilters()` ile denetliyor.
- 🟢 Kalıcı test kuyruğun yeniden açılışta korunmasını, retry zamanının ertelenmesini ve tamamlanan girdinin kalkmasını doğruluyor. Release test paketi **28/28 başarılı**; `git diff --check` temiz (yalnız çalışma ağacındaki CRLF dönüşüm uyarıları var).
- 🔴 Worker gerçek müşteri DB/deposunda çalıştırılmadı. DB commit ile kuyruğa alma transaction'a bağlı değil; aradaki süreç çökmesi yetim bırakabilir. DB referans kontrolü ile fiziksel unlink atomik değil. Bu nedenle A-10 kırmızı kalır.
- 🟢 Journal claim'i 5 dakikalık lease ile serileştirildi; lease süresince diğer worker aynı girdiyi alamaz. Release testi aynı girdinin ikinci kez claim edilmediğini ve lease bitince tekrar alınabildiğini doğrular.
- 🟢 Worker artık tek girdiyi claim edip işler; işlem boyunca lease'i dakikada bir yeniler. Böylece sıradaki girdinin lease'i beklerken dolmaz. Test yenileme sonrasında eski lease bitişinde ikinci claim'i ve eski sahibin yenilemesini reddeder.
- 🔴 İşlem sırasında uzun bir kesinti veya yenileme hatası hâlâ lease kaybına neden olabilir. Referans kontrolü/unlink yarışı ve DB commit-kuyruk crash aralığı sürdüğünden A-10 kırmızı kalır.

### 2026-10-06 — A-10 depolama yolu eşleştirme koruması

- 🟢 Depolama kökü altındaki mutlak dosya yolu kanonik anahtara doğru çevrilir. Silmeden önce SQL eşleşmesine ek olarak bilinen DB yolları kanonikleştirilerek karşılaştırılır; Windows'taki harf büyüklüğü farkı referansın kaçmasına neden olmaz.
- 🟢 Mutlak yol regresyonu dahil Release test paketi **28/28 başarılı**. Gerçek müşteri DB'sinde bu tam yol taramasının süresi ve SQL sağlayıcıları doğrulanmadı; A-10 🔴 kalır.

### 2026-10-06 — A-10 şifresiz dosya aday raporu

- 🟢 Admin evrak bakım ekranı uploads ve Arsiv altındaki geçici olmayan, `.enc` dışındaki dosyaları bilinen DB yol alanlarıyla karşılaştırıp referanssız adayları ayrı listeler. Sembolik bağlantılar izlenmez; dosyalar otomatik silinmez.
- 🟢 Şifreli ve şifresiz tarama aynı DB yol envanterini kullanır. Dosyanın listede görünmesi silinebileceği anlamına gelmez; kaynak sahibinin manuel incelemesi gerekir. Release test paketi **28/28 başarılı**.
- 🔴 Referanslı eski açık dosyaların taşıma kapsamı, diğer dosya alanları, gerçek müşteri DB/depo kabulü ve commit-kuyruk yarışları açık; A-10 rengi değişmedi.

### 2026-10-06 — A-10 tam model SQLite dosya referansı kabulü

- 🟢 `ApplicationDbContext` tam modeliyle geçici SQLite DB oluşturulup soft-delete edilmiş ve farklı harf büyüklüğünde kaydedilmiş dosya yolu eklendi. `SecureFileReferenceChecker` bu dosyayı referanslı saydı; DB'de olmayan yolun serbest olduğunu doğruladı.
- 🟢 Yeni kalıcı regresyonla Release test paketi **29/29 başarılı**. Bu doğrulama gerçek müşteri DB/deposunda worker çalışması, PostgreSQL çevirisi veya referans kontrolü ile fiziksel silme yarışının kabulü değildir; A-10 🔴.

### 2026-10-06 — A-10 izole worker ve fiziksel silme kabulü

- 🟢 Tam `ApplicationDbContext` SQLite şemasında iki fiziksel dosya ve kalıcı kuyruk girdisi oluşturuldu. Worker, soft-delete edilmiş DB kaydının yoluna bağlı dosyayı korudu; referanssız dosyayı `SecureFileService` ile sildi ve kuyruktan çıkardı.
- 🟢 İşlem turu üretim worker'ıyla aynı kod yolunu kullanacak biçimde izole çalıştırıldı. Release test paketi **30/30 başarılı**.
- 🔴 Bu kabul gerçek müşteri DB/deposu, PostgreSQL, eşzamanlı yükleme-silme yarışı veya DB commit ile kuyruğa alma arasındaki çökme aralığını kapsamaz. A-10 kırmızı kalır.

### 2026-10-06 — A-10 geri alınabilir dosyayı koruma kararı

- 🟢 Kullanıcı kararı: soft-delete edilmiş kayıtların dosyaları geri alma için tutulur. `SecureFileService.DeleteAsync` doğrudan çağrıldığında ve worker'dan çağrıldığında bilinen DB referanslarını denetler; referans varsa fiziksel dosyayı korur ve gereksiz yeniden denemeyi önlemek için journal isteğini tamamlar.
- 🟢 Tam model SQLite senaryosu doğrudan çağrıyı ve ayrıca kuyruğa yeniden eklenmiş referanslı dosyanın worker tarafından korunmasını, referanssız dosyanın silinmesini doğrular. DB referans sorgusu hata verdiğinde dosyanın korunup journal isteğinin kaldığı da test edildi. Release testleri **31/31 başarılı**.
- 🔴 Gerçek müşteri verisinde çalışma, DB commit ile journal yazımı arasındaki kesinti, sorgu ile fiziksel silme arasındaki yarış ve eski dosya alanlarının tam kapsamı açık olduğu için A-10 henüz kapatılmadı.

### 2026-10-06 — A-10 eski düz dosya silme yolları

- 🟢 Ortak evrak ekranındaki `FileService.Delete` ve destek eki servisindeki doğrudan eski dosya silmesi, soft-delete sonrasından çıkarıldı. Bu kayıtlar geri alınırken düz dosyaları da kalır; DB kaydı olmayan yeni yüklemenin telafi temizliği ayrı akış olarak sürer.
- 🟢 Ortak evrakın yeni yüklemesi `SecureFileService.SaveEncryptedAsync` kullanır. Görüntüleme/indirme şifreli yeni yolu ve geçmiş düz dosya adını destekler; bakım ekranı iki türde dosya varlığını denetler. Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Geçmiş düz dosyaların toplu şifreli geçişi ve gerçek ortam kabulü gereklidir. A-10 🔴 kalır.

### 2026-10-06 — A-10 ortak evrak eski dosya geçişi

- 🟢 Yönetici geçiş ekranı, aktif firma kapsamındaki ortak evrakın eski tek dosya adı yollarını önizlemede sayar ve şifreli depoya taşır. Yeni yol DB'de doğrulanmadan eski dosya silinmez. Tüm bilinen dosya yolu alanları soft-delete filtreleri yok sayılarak taranır; başka bir kayıt eski yolu tutuyorsa dosya korunur.
- 🟢 Ekrandaki kapsam açıklaması ve toplam bekleyen sayısı güncellendi. Web Release derlemesi **0 uyarı / 0 hata**. Bu değişiklik için çalışma zamanı geçiş testi yapılmadı.
- 🔴 Soft-delete kayıtlarının kendilerinin geçişi, gerçek müşteri veri/depo kabulü, çok süreçli sorgu-silme yarışı ve büyük DB'de tam yol taraması performansı açık. A-10 genel durumu değişmedi.

- 🟢 Geçiş tek aktif firma seçilmeden veya “Tüm Firmalar” modundayken başlatılmaz. Firma alanı olmayan ortak evrak, bağlı personel/araç kaydının firma kimliğiyle sınırlandırılır; bağı kopuk veya farklı firmaya bağlı kayıt otomatik taşınmaz. Eski `FileService` yol çözümü de sembolik bağlantı geçişlerini reddeden ortak depolama korumasına bağlandı. Önizleme hatası ekranda gösterilir.

### 2026-10-06 — A-10 silinmiş ortak evrak dosyalarının geçişi

- 🟢 Geri alınabilir silinmiş ortak evrak satırları, bağlı personel/araç silinmiş olsa bile açık firma kimliği eşleşiyorsa önizlemeye ve şifreli geçişe alınır. Ekran aktif ve silinmiş ortak evrak sayılarını ayrı gösterir.
- 🟢 Silinmiş satırın dosya yolu yeni şifreli yola yazılır; eski dosya, bilinen herhangi bir DB kaydı eski yola başvuruyorsa korunur. Web Release derlemesi **0 uyarı / 0 hata**. Bu değişiklik için çalışma zamanı geçiş testi yapılmadı.
- 🔴 Bağı kopuk eski ortak evrak kayıtları otomatik geçirilmez. EBYS/araç silinmiş belge geçmişi o tarihte kapsam dışıydı; sonraki düzeltme bu kapsamı genişletti. Gerçek müşteri verisiyle kabul ve sorgu-silme yarışı açık; A-10 🔴 kalır.

- 🟢 Araç ana dosya ve sürüm geçişinde seçili `FirmaId` açıkça denetlenir. EBYS kayıtlarında firma bağı bulunmadığı için geçiş ekranı EBYS bölümünün yönetici genel kapsamında olduğunu belirtir; bu veri modelinin firma sahipliği A-15 kapsamında ayrıca ele alınmalıdır.

### 2026-10-06 — A-10 şifreli geçiş kopyasını geri okuma

- 🟢 EBYS, araç ve ortak evrak geçişinde yeni şifreli dosya uygulamanın okuma yoluyla geri açılıp kaynak içerikle byte düzeyinde karşılaştırılır. Karşılaştırma geçmeden DB yolu değiştirilmez; doğrulanamayan yeni kopya temizlenmeye çalışılır ve eski dosya korunur.
- 🟢 Dosya okuma/yazma/doğrulama iptal belirtecine bağlandı. Web Release derlemesi **0 uyarı / 0 hata**; bu değişikliğin çalışma zamanı geçiş testi yapılmadı.
- 🔴 Müşteri verisiyle gerçek geçiş, DB commit sonrası eski dosya temizliği ve eşzamanlı sorgu-silme yarışı açık; A-10 genel durumu değişmedi.

### 2026-10-06 — A-10 eski dosya temizliği durumunun ayrılması

- 🟢 DB'de yeni şifreli yol başarıyla kaydedilip yalnız eski düz dosya silinemediğinde geçiş sonucu artık `Temizlik Bekliyor` olarak gösterilir. Başarı ve hata sayılarından ayrı sarı sayılır; operatör eski dosyanın kalmış olduğunu görür.
- 🟢 Bakım raporunun salt okunur şifresiz aday envanteri eski ortak evrak yükleme kökünü ve `wwwroot/uploads` klasörünü de kapsar; bilinen DB yol alanlarıyla karşılaştırır, sembolik bağlantıları izlemez. Adaylar otomatik silinmez.
- 🔴 Bu satırdaki kalıcı retry eksikliği aşağıdaki sonraki düzeltmeyle giderildi. Rapor referanssız dosyanın silinebileceği garantisi değildir; gerçek veri kabulü ve çok süreçli yarış açık. A-10 🔴 kalır.

### 2026-10-06 — A-10 eski düz dosya temizliğine kalıcı yeniden deneme

- 🟢 Geçişte DB yeni şifreli yola kaydedildikten sonra eski ortak evrak ve `wwwroot/uploads` dosyası için tipli bir istek DP korumalı `FileCleanupJournal` içine yazılıyor. Anlık temizleme başarısız olursa mevcut `FileCleanupRetryWorker` isteği tekrar işliyor.
- 🟢 Her denemede bilinen dosya yolu alanları silinmiş kayıtlar dahil yeniden okunuyor. Hâlâ başvurulan eski dosya geri alma için korunuyor; DB sorgusu veya disk işlemi hata verirse istek günlükte kalıyor. Web Release derlemesi **0 uyarı / 0 hata**; bu son değişiklik için çalışma zamanı testi yapılmadı.
- 🔴 Bu satırdaki eski dosya geçişine özgü commit-journal kesintisi aşağıdaki sonraki düzeltmeyle giderildi. Referans sorgusu ile fiziksel silme yarışı, müşteri DB/depo kabulü ve tam yol taramasının büyük verideki süresi açık. A-10 🔴 kalır; durum sayımı değişmez.

### 2026-10-06 — A-10 geçiş temizleme isteğini DB yazımından önce kaydetme

- 🟢 Yeni şifreli kopya geri okunduktan sonra eski/yeni yol çifti kalıcı journal'a yazılıyor; ancak bundan sonra kayıt yeni yola çevrilip DB'ye kaydediliyor. Böylece bu geçişte DB commit'i tamamlanıp süreç hemen kesilirse temizleme isteği önceden kayıtlıdır.
- 🟢 Worker yeni yol DB'de doğrulanana kadar, eski yolun durumundan bağımsız olarak isteği erteliyor ve eski dosyayı koruyor. Yeni yol kayıtlıysa, eski yola başka başvuru olup olmadığını denetleyip koruma veya silme kararı veriyor. Önceki v1 journal girdileri okunmaya devam eder. Web Release derlemesi **0 uyarı / 0 hata**; bu son değişiklik için çalışma zamanı testi yapılmadı.
- 🔴 DB yazımı başarısız olup eski yol korunursa istek güvenli biçimde bekleyebilir; bu tür bekleyen girdilerin işletim temizliği, diğer dosya silme akışlarının commit-kuyruk aralığı, sorgu-silme yarışı ve gerçek müşteri kabulü açık. A-10 🔴 kalır.

### 2026-10-06 — A-10 geri alınabilir EBYS ve araç dosya geçmişi

- 🟢 Geçiş önizlemesi ve uygulaması `IgnoreQueryFilters` ile soft-delete edilmiş EBYS/araç ana dosyalarını ve iki modülün dosya sürümlerini de kapsar. Önizlemede aktif ve silinmiş ana/sürüm kayıtları ayrı sayılır; yönetici ekranı kapsam metni güncellendi.
- 🟢 Araç dosyasında doğrudan `FirmaId` seçili firmayla eşleşmeli; eski/null firma alanında bağlı araç ve evrak ilişkisi üzerinden aynı firma doğrulanır. Uyuşmayan firma bağı geçiş dışında bırakılır. Web Release derlemesi **0 uyarı / 0 hata**; gerçek veriyle geçiş testi yapılmadı.
- 🔴 EBYS veri modelinde firma sahipliği yok ve bölüm yönetici genel kapsamındadır. Gerçek müşteri DB/storage kabulü, diğer dosya alanları, tam yol tarama performansı ve referans kontrolü-silme yarışı açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 personel özlük ve fatura dosyalarının geçişi

- 🟢 Seçili firmaya ait personel özlük belgeleri ve sürümleri (silinmiş satırlar ve silinmiş sürücüler dahil) ile faturaların PDF/XML yolları geçişe eklendi. Faturada doğrudan `FirmaId`, personelde bağlı sürücünün `FirmaId` değeri açıkça doğrulanır.
- 🟢 Eski web yolu `/uploads/...`, `uploads/...` ve ters eğik çizgili biçimlerde okunur; personel özlükte eski tek dosya adı ortak yükleme kökünden taşınabilir. Önizleme modül gruplarını ve aktif/silinmiş adetleri gösterir. Web Release derlemesi **0 uyarı / 0 hata**; gerçek veriyle geçiş testi yapılmadı.
- 🔴 Destek/tedarikçi dosyalarının sahiplik ve kök eşlemesi, diğer yazım/silme akışları, EBYS firma sahipliği, büyük veri performansı ve gerçek müşteri kabulü açık. A-10 🔴.

### 2026-10-06 — A-10 fatura eski dosyalarında doğru depo kökü

- 🟢 Fatura PDF/XML eski yolları `AppStoragePaths.GetUploadsRoot(...)` altından okunur ve cleanup journal'ında ayrı, sabit bir `storage-uploads` kök anahtarıyla temsil edilir. EBYS/araç web upload'ları `wwwroot/uploads`, personel tek dosya adları ortak dosya servisi kökünde kalır. Aynı `/uploads/...` metni farklı fiziksel köklerle karıştırılmaz.
- 🟢 Cleanup worker her kökte yolu kendi sabit kökü içinde çözer; eski/yeni yol DB doğrulaması ve eski yolun başka kayıtlarda aranması korunur. Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` temiz. Bu turda çalışma zamanı testi veya müşteri dosyası taşınmadı.
- 🔴 Destek/tedarikçi kök ve sahiplik eşlemesi, EBYS firma sahipliği, diğer yazım/silme akışları, sorgu-silme yarışı, büyük DB performansı ve saha kabulü açık. A-10 🔴 kalır.

### 2026-10-06 — A-10 tenant bağı doğrulanan tedarikçi dosyaları

- 🟢 Tedarikçi eki migrasyonu eklendi. Sorgu soft-delete filtrelerini yok sayar; tedarikçi → cari → `Cari.FirmaId` zinciri seçili firmayı doğrulamadıkça dosya kapsam dışıdır. Aktif/silinmiş sayaçlar ekrana yansıtılır.
- 🟢 Yalnız ortak dosya servisinin kökünde bulunan eski tekil dosya adları taşınır. Yeni şifreli kopya doğrulaması ve kalıcı eski dosya temizleme akışı uygulanır. Web Release derlemesi **0 uyarı / 0 hata**; çalışma zamanı veya gerçek veri testi yapılmadı.
- 🔴 Destek eklerinin cari bağlantısız talepleri ve izin verilen `wwwroot/uploads/destek` altındaki mutlak eski yollar henüz güvenli şekilde taşınmıyor. Diğer tedarikçi yol biçimleri, büyük DB performansı ve müşteri kabulü de açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 cari bağı doğrulanan destek ekleri

- 🟢 Destek taleplerine doğrudan bağlı ve yanıt ekleri seçili firmaya yalnız `DestekTalebi.CariId → Cari.FirmaId` eşleşmesiyle alınır; sorgu silinmiş talep/cari/ek kayıtlarını da kapsar.
- 🟢 Eski mutlak dosya yolu yalnız `wwwroot/uploads/destek` altındaysa taşınır. Cleanup journal göreli yolu tipli destek köküyle kaydeder; worker DB'deki eski yol referansını kontrol etmeden silmez. Ekranda aktif/silinmiş ek adetleri gösterilir. Web Release derlemesi **0 uyarı / 0 hata**; runtime/gerçek veri testi yapılmadı.
- 🔴 Cari bağlantısı olmayan destek talepleri, destek kökü dışındaki/eski formatı farklı yollar ve gerçek müşteri kabulü açık. Tedarikçi eski ortak kök dışı biçimleri, DB-silme yarışları ve performans da A-10'u açık tutuyor.

### 2026-10-06 — A-10 personel özlük silmesinde commit öncesi cleanup günlüğü

- 🟢 Personel özlük sürümü fiziksel silinecek veya aktif evrak yol alanı temizlenecekse cleanup isteği SaveChanges öncesinde dayanıklı günlüğe ekleniyor. İstek, DB yazımı istisna verse veya süreç kesilse de kaybolmuyor.
- 🟢 Yeni `WaitForReferenceRemoval` journal modu worker'ın DB başvurusu dururken isteği tamamlamasını engelliyor; başvuru devam ederse artan gecikmeyle yeniden deniyor. Paylaşılan/silinmiş geçmiş yolu saptanırsa koruma kararı isteği tamamlıyor. Release derlemesi **0 uyarı / 0 hata**; çalışma zamanı testi yapılmadı.
- 🔴 Bu sıra düzeltmesi yalnız personel özlük silme akışına uygulandı. Diğer DB-hard-delete dosya akışları, worker saha kabulü, referans kontrolü-unlink yarışı ve büyük envanter performansı açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 EBYS dosya güncellemesinde commit öncesi cleanup

- 🟢 EBYS dosya güncellemesinde yeni dosya yolu DB'ye yazılmadan önce eski yol için `WaitForReferenceRemoval` journal isteği kalıcılaştırılıyor. Worker DB güncellemesi commit olmadan isteği alırsa eski yol başvurusu nedeniyle silmeyi tamamlamaz, yeniden dener.
- 🟢 SaveChanges sonucu belirsiz olduğunda DB'deki mevcut yol kontrol edilir; değişiklik eski yolda kaldıysa yalnız yeni dosya temizlenir ve eski yolun bekleyen isteği kaldırılır. Değişiklik commit olduysa eski yol isteği korunur; tarihçe/başka tablo referansı dosyayı tutar. Web Release derlemesi **0 uyarı / 0 hata**; runtime testi yapılmadı.
- 🔴 Commit öncesi günlükleme şu an personel özlük ve EBYS dosya güncellemesinde var. Diğer dosya yolu değiştirme/silme akışları, gerçek müşteri worker kabulü ve referans kontrolü-unlink yarışı açık; A-10 🔴 kalır.

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

- 🟢 Yeniden yüklemede geçmiş satırı artık yeni dosya yerine önceki yol/ad/tip/boyut bilgilerini tutuyor. Hızlı kaldırma akışı DB’de aktif yol ve dosya alanlarını gerçekten temizliyor; eski dosya aynı DB işleminde sürüm geçmişine bağlandığı için geri alınabilir ve fiziksel temizlik tarafından silinmiyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; canlı personel verisiyle geri alma kabulü yapılmadı.
- 🔴 A-10 açık: müşteri DB/storage geçiş/restore kabulü, genel TOCTOU, büyük DB tarama maliyeti ve modüller arası kalan silme sözleşmeleri.

### 2026-10-07 — A-10 soft-delete dosyalarının geri alınabilir tutulması

- 🟢 Araç evrakı, taşıma tedarikçisi eki ve EBYS dosyası soft-delete işlemleri fiziksel şifreli dosyayı artık silmiyor. DB kaydı geri alınabilir işaretli kaldığı sürece içeriği de geri alma için korunuyor. Hard purge ayrı ve açık bir operasyon olmalı.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Restore işlemi müşteri DB’sinde çalıştırılmadı.
- 🔴 A-10 açık: gerçek müşteri geçiş/restore kabulü, eşzamanlı referans-silme yarışı, büyük DB tarama maliyeti ve kalan hard-delete sözleşmeleri.

### 2026-10-07 — A-10 dosya yolu referans kontrolü ve indeksleme

- 🟢 `DosyaYolu`, `PdfDosyaYolu`, `XmlDosyaYolu` taşıyan 12 DB kolonu için model indeksi ve sağlayıcı bağımsız migration eklendi. Referans kontrolü artık her silmede DB’deki tüm dosya yollarını uygulamaya yüklemiyor; mutlak storage aliasları da eşleşme sorgusuna dahil. Windows harf/ayraç normalizasyonu DB tarafında yapılıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test çalıştırılmadı.
- 🔴 A-10 açık: hedef büyüklükte migration/index performansı, Windows case-normalized sorgu ve çoklu sunucuda eşzamanlı silme kabulü; müşteri dosyalarının geçiş/restore kabulü ve firma bağı olmayan legacy destek ekleri.

### 2026-10-07 — A-10 personel dosya yolu değişikliğini amaca özel hale getirme

- 🟢 Genel personel evrak güncellemesi artık dosya yolu yazmıyor; eski/detached form nesnesiyle temizlenmiş dosya başvurusunu geri getiremez. Bozuk dosya referansı için ayrı DB metodu eklendi; aktif dosya kaldırma ve eski yolu sürüme alma mevcut tek işlemde kalıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🔴 A-10 müşteri restore/geçiş kabulü, hedef DB hacim performansı ve çoklu sunucu kabulü bekliyor.

### 2026-10-07 — A-10 restore ve çoklu worker izole kabulü

- 🟢 İki bağımsız `FileCleanupJournal` örneği aynı paylaşılan depoda eşzamanlı claim yaptı; lease/file lock nedeniyle tek işçi kazandı. Test `Independent_workers_cannot_claim_the_same_due_file` Release paketine eklendi.
- 🟢 Tam model SQLite kabulinde soft-delete edilmiş dosya cleanup turundan sonra fiziksel olarak duruyor; kayıt geri açıldığında yol referansı doğrulanıyor ve dosya korunuyor. Release test paketi **32/32 başarılı** (0 başarısız).
- 🟡 Bu kanıt yerel SQLite ve aynı host dosya sistemiyle sınırlı. Gerçek müşteri DB/dosyalarıyla geçiş/restore, hedef hacimde migration ve performans ölçümü ile iki fiziksel sunucu/SMB paylaşım kabulü burada çalıştırılmadı. Bu yüzden A-10 genel kapanış iddiası verilmedi; bu saha kabulleri açıktır; kaynak incelemesi tamamlanmadan başka uygulama açığı olmadığı sonucuna varılmaz. Müşteri verisine yazım yapılmadı.
### 2026-10-07 — A-10 kuyruk sahipliği, eski dosya doğrulaması ve geçiş kabulü

- 🟢 **Eski worker sonucu:** Tamamlama lease kimliği, süresi ve istek revizyonuyla denetleniyor. Yeni isteğe eski tamamlamanın uygulanması ve aynı yol silinip yeniden kuyruğa alındığında eski isteğin yenisini kaldırması engellendi. Doğrudan silme, `WaitForReferenceRemoval` isteğini koruyor.
- 🟢 **Eski dosyanın güvenli temizliği:** Yeni DB yolunun varlığına ek olarak şifreli kopya yeniden çözülüyor ve kaynakla SHA-256 karşılaştırılıyor. Yeni kopya eksikse veya kaynak içeriği değişmişse dosya ve kuyruk kaydı korunuyor. Legacy kontrol bütün yol listesini belleğe almıyor. Yol boşlukları/ters ayraçlar ve Türkçe harfler için regresyon var; Unicode eşleşme gerektiğinde sabit bellekle akış halinde kontrol ediliyor.
- 🟢 **Gerçek servis akışı, izole veri:** Tam model SQLite üzerinde `DosyaMigrasyonService.OnizlemeAsync/MigrateAsync` çalıştırıldı. Soft-delete EBYS belgesi şifrelendi, eski kaynak temizlendi, satır geri açılıp aynı içerik çözüldü; tekrar geçiş sıfır işlem üretti. Bu bir müşteri yedeği restore'u değildir.
- 🟢 **Hacim probu:** Tek dosya tablosunda 100.000 sentetik satır; bu Windows makinesindeki SQLite ölçümü: doğrudan referans **2,7 ms**, normalize referans **28,0 ms**, referanssız yol **37,5 ms**. Tek çalıştırma ölçümüdür; hedef müşteri SLA'sı veya PostgreSQL performans kabulü değildir.
- 🟢 **Doğrulama:** Release test paketi **34/34 başarılı**. Web/Shared/test projesi test komutunda derlendi. Testler yalnız GUID ile ayrılmış geçici klasör ve SQLite verisi kullanır.
- 🔴 **Genel durum:** A-10'un yukarıdaki alt düzeltmeleri yeşildir. DB'ye yeni başvuru ekleme ile fiziksel silmeyi bütün servislerde ortak kilitle sıralama, firma/sahiplik kapsamı ve tabloda belirtilen saha kabulü tamamlanmadı. Önceki “yalnız üç saha kabulü kaldı” ifadesi bu yüzden düzeltildi. Diğer A görevleri bu testlerle kapatılmadı.
### 2026-10-07 — A-24/A-25 ortak cache nesli ve hata davranışı

- 🟢 Statik süreç içi key tracker/kilit yerine ortak `IDistributedCache` nesli getirildi. Her nesil benzersizdir; marker kaybolunca eski ad alanına geri dönülmez. Okuma sırasında nesil yeniden kontrol edilir; invalidation öncesi başlayan factory yeni nesli dolduramaz.
- 🟢 `RemoveAsync` ve `RemoveByPrefixAsync` tüm uygulama neslini değiştirir. Bu bilinçli geniş geçersizleştirme, farklı süreçlerdeki bilinmeyen anahtarları da kapsar; daha fazla cache miss yaratabilir. Eski nesil kayıtları TTL ile biter; sliding cache için ayrıca 24 saat üst sınırı var.
- 🟢 İptal cache miss'e çevrilmez. GET arızasında veri kaynağı kullanılır; invalidation arızası loglanıp çağırana iletilir. DB commit sonrası çağıranın hata politikası ayrıca geçerlidir; kalıcı retry/outbox bu değişiklikte yoktur.
- 🟢 Ortak memory backend ile bağımsız iki servis örneği, factory/invalidation yarışı, hata ve iptal için dört regresyon geçti. Güncel Release test paketi **38/38 başarılı**.
- 🟡 Gerçek Redis testi yapılmadı: Docker istemcisi mevcut ancak daemon named pipe bağlantısı başarısız. İki memory backend'e sahip ayrı sunucular ortaklaşmaz; çok sunuculu kullanım ortak Redis gerektirir. Ağ kesintisinde kaybolan invalidation ve diğer araç yazımlarının kapsamı nedeniyle A-24 🔴, A-25 🟡 kalır.

### 2026-10-07 — A-24 iş verisi önbelleğinde doğruluk sınırı

- 🟢 `CRMFilo:` anahtarlı üretim iş listeleri ve dashboard için cache okuma/yazma ve invalidation kullanılmıyor; her `GetOrSetAsync` veri kaynağını çalıştırıyor. Bu tercih, DB commit'i sonrası cache sunucusu kesilirse eski liste gösterilmesini ve başka yazım yollarının invalidation unutmasını önler. Jenerik cache davranışı diğer anahtarlar için korunur.
- 🟢 `AracService` araç listesini ayrıca doğrudan DB'den okur; firma seçimi sürümü kontrolü korunur. Excel aktarımının sonuç döndürme yolu cache kaldırma sonrasında düzeltildi.
- 🟡 Bedel her okumada DB sorgusudur. Hedef müşteri hacminde sorgu/yük ve çoklu sunucu kabulü yapılmadığından A-24 🟡, A-25 🟡 kalır; genel satış kabulü verilmez. Daha sonra cache yeniden açılacaksa DB transaction ile bağlı invalidation veya outbox gerekir.

### 2026-10-07 — A-10 şifreli dosyada geri alınabilir karantina

- 🟢 Referans denetiminden sonra yeni DB başvurusu eklenebilme penceresinde şifreli içerik artık `File.Delete` ile geri dönülmez biçimde silinmiyor. `SecureFileService` aynı depolama kökü altındaki `.deleted-file-quarantine-v1` alanına atomik taşıyor; normal dosya yolu sonradan referanslanırsa okuma, varlık ve kopyalama karantinaya geri düşüyor. Okuma/kopyalama sırasında taşıma olursa dosya bulunamadı durumunda tekrar deniyor.
- 🟢 Karantina `RecoveryArchive` tarafından yedeklenen `storage/uploads` ağacında. Salt okunur yetim tarayıcı bu tutulan dosyaları yeni yetim olarak saymıyor.
- 🟡 Web Release derlemesi 0 uyarı/0 hata. Bu turda karantina ve eşzamanlı taşıma için çalışma zamanı testi yapılmadı. Kalıcı saklama disk/yedek hacmini artırır; kapasite takibi gerekir. Legacy düz dosya fiziksel silme yolu ve `SecureFileService` dışından doğrudan yola erişen tüketiciler ayrıca incelenmelidir. A-10 🔴 kalır.

### 2026-10-07 — A-10 karantina eşlemesinde farklı kurulum yolu düzeltmesi

- 🟢 Karantina yolu artık sunucunun mutlak klasöründen değil, depolama köküne göre mantıksal yoldan türetilir. `.deleted-file-quarantine-v1` altında özgün dizin yapısı korunur; DB satırı kaybolsa da operatör dosyayı özgün yolla eşleyebilir. Aynı arşiv başka makine veya klasöre açıldığında eşleme değişmez; kök dışına çıkan yol reddedilir. Düz dosyanın yanlışlıkla şifreli karantinaya taşınması engellenir; mevcutsa eski dosya temizlik akışına yönlendiren görünür hata üretilir. Master key kurtarma varsayılan taraması karantinayı da kapsar. Disk sağlık kontrolü uygulama diski yerine yapılandırılmış depolama sürücüsünü ölçer.
- 🟡 Farklı makinede gerçek restore ve karantinadan okuma çalışma zamanı kabulü bu turda yapılmadı; A-10 🔴 kalır.

### 2026-10-07 — A-10 karantina ZIP hazırlama ve farklı kök kabulü

- 🟢 Yeni kalıcı regresyon testi şifreli içeriği kaydedip karantinaya taşır, özgün yoldan çözer, `RecoveryArchive.CreateAsync` ile gerçek kurtarma ZIP'i üretir ve `PrepareAsync` ile manifest/hash/key ring probunu doğrular. Hazırlanan arşivdeki karantina dosyası farklı depolama köküne aktarıldığında aynı mantıksal yoldan çözüldü.
- 🟢 Hedefli test **1/1**, tam Release test paketi **39/39** başarılı. Bu kanıt sentetik dosya ve yerel dosya sistemidir; müşteri dump'ı, farklı fiziksel sunucu/SMB veya PostgreSQL restore kabulü değildir.
- 🔴 Legacy düz dosya yarışı, karantina kapasite/purge politikası ve tabloda belirtilen müşteri kabulleri açık olduğundan A-10 rengi değişmez. (2026-10-09 kullanıcı kararıyla saklama süresiz/otomatik purge yok olarak sabitlenmiştir; kapasite kabulü ve müşteri kabulleri sürer.)

### 2026-10-07 — A-11 S3 hata sınıfları ve imza başlığı

- 🟢 `S3ObjectStorageService` AWS SigV4 `Authorization` değerini HTTP başlığına doğrulama atlayarak koyuyor; .NET'in bu sözdizimini reddetmesi nedeniyle istek gönderilmeden oluşan `FormatException` giderildi.
- 🟢 İndirme ve varlık sorgusunda yalnızca 404 eksik nesne olarak dönüyor. 403, 5xx ve bağlantı hataları çağırana iletiliyor. Silme 404 için idempotent, diğer hatalarda başarısız. Gerçekte imzalanmamış URL yerine açık `NotSupportedException` veriliyor; bu API için üretim çağıranı bulunmadı.
- 🟢 Hedefli HTTP durum testleri **2/2**, tam Release paketi **41/41** başarılı. Test sahte HTTP handler kullanır; gerçek S3/MinIO imzasının sunucuda kabulü ve UI/çoklu dosya davranışı doğrulanmadı. A-11 🟡 kalır.

### 2026-10-07 — A-11 yerel depo hata ayrımı

- 🟢 Yerel nesne deposunda `DownloadAsync` dosyayı doğrudan açıyor; yalnız `FileNotFoundException`/`DirectoryNotFoundException` eksik nesne olarak dönüyor. `ExistsAsync` öznitelik sorguluyor; izin ve I/O hataları `File.Exists` tarafından sessizce `false` sonucuna çevrilmiyor. İptal, okuma/varlık kontrolünden önce denetleniyor.
- 🟢 İmzalı URL desteklenmeyen yerel sağlayıcı artık boş bağlantı üretmiyor; açık `NotSupportedException` dönüyor. Hedefli yerel depo testleri **4/4**, tam Release paketi **42/42** başarılı.
- 🟡 Windows izin/kilit hatası, çoklu dosya kısmi hata ve ekran sonucu gerçek çalışma ortamında sınanmadı. A-11 🟡 kalır.
- 🟢 Araç, tedarikçi ve özlük evrakında başarılı kaldırma mesajı geri alınabilir saklamayı açıkça söylüyor. Web Release derlemesi **0 uyarı / 0 hata**. Ekran davranışı tarayıcıda ayrıca kabul edilmelidir.

### 2026-10-07 — A-12 eski araç aktarımının görünür sonucu

- 🟢 Firma veya modal değişimi, servis satır kaydetmeye başladıktan sonra gerçekleşirse eski aktarım sonucu yeni modala yazılmıyor; eklenen/güncellenen/hatalı satır sayısı önceki firma kimliğiyle uyarı olarak gösteriliyor. Kayıt varsa güncel firma listesi yenileniyor. Hata yolunda kısmi kayıt olasılığı açıkça bildiriliyor.
- 🟢 Aktarım sürerken yeni aktarım modalı açılmıyor. Tarayıcıdan açılan dosya akışı `await using` ile kapanıyor. Web Release derlemesi **0 uyarı / 0 hata**. Gerçek dosya ve tarayıcı yarışı çalıştırılmadığından A-12 🟡 kalır.

### 2026-10-07 — 31 görevin renk denetimi

- 🟢 A-08 ve A-22 kendi tanımlı teknik kapsamlarında yeşil kaldı; kaynak/izole kanıt, diğer görevlerin saha kabulü yerine kullanılmadı.
- 🔴 A-16 eski veri, yalnız sınırlı migration ön kontrolleri bulunduğu; kapsamlı envanter, kontrollü onarım ve öncesi/sonrası kanıtı bulunmadığı için 🟡 → 🔴 yeniden sınıflandırıldı.
- 🟡 A-01/A-02/A-04–A-07/A-09/A-11–A-14/A-17–A-21/A-24–A-27 maddelerinin kalan koşulları sürdüğü için renkleri korunuyor. 🔴 A-03/A-10/A-15/A-23/A-28 ve ⚪ A-29–A-31 renkleri de korunuyor. Toplam **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz**.
- Bu işlem kod veya müşteri verisi değiştirmedi; renkler mevcut yerel kanıt ve açık kapanış koşullarının yeniden denetimidir.

### 2026-10-07 — A-16 salt okunur eski veri ön envanteri

- 🟢 `MKFiloServis.DataSync inventory`, SQLite dosyasını `ReadOnly`, PostgreSQL'i `READ ONLY` transaction ile tarar. On kontrol: firma bağı, şase/plaka tekrarı, araç-cari, banka-hesap, fatura-cari ilişkisi, iki dönem snapshot'ı ve iki varsayılan şablon türü. Çıktıda sayı ve en çok 20 kimlik vardır; eksik şema `Complete=false` yapar.
- 🟢 Sentetik SQLite ve geçici PostgreSQL 17 kümesinde her biri **10/10 bulgu** verdi. Eksik şema raporu tamamlanmamış olarak işaretlendi; raporun SQLite kaynağının üzerine yazılması reddedildi. DataSync Release derlemesi **0 uyarı / 0 hata**. Geçici PostgreSQL sunucusu durduruldu.
- 🔴 Gerçek müşteri verisi taranmadı; bu 10 kontrol tüm ilişkileri kapsamaz ve otomatik onarım yapmaz. A-16 genel rengi 🔴, toplam sayım **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz** kalır.

### 2026-10-08 — A-16 şemadan tenant FK keşfi

- 🟢 Ön envanter, iki sağlayıcıda firma kapsam kolonu olan tablolar arasındaki `Id` foreign key ilişkilerini şemadan çıkarıp her kaynak tablo için tek sorguda firma uyuşmazlıklarını inceleme adayı olarak sayacak şekilde genişletildi. Bu yöntem yalnız tanımlı FK'leri kapsar; veri değiştirmez ve otomatik onarım önermez.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Sentetik SQLite DB'de farklı `FirmaId` değerli tek kolonlu FK bağlı satır eklendi; tarama `A16-TENANT-001` ile **1** inceleme adayı buldu. PostgreSQL denemesinde izole küme Windows'un loopback socket izni nedeniyle başlatılamadı.
- 🔴 FK tanımsız ve composite iş ilişkileri, finansal toplamlar, gerçek müşteri bulguları ve onarım/öncesi-sonrası kanıtı açık. A-16 kırmızı; toplam renk sayımı değişmedi.

### 2026-10-08 — A-16 SQLite foreign key ihlal taraması

- 🟢 Her foreign key tanımlı SQLite kaynak tablosu için `pragma_foreign_key_check` çalıştırılır; bu, composite FK ve örtük hedef anahtarı da kapsar. Sentetik SQLite DB'de tekli ve composite sahipsiz ilişkiler ayrı kontrol kodlarıyla **1'er** bulgu verdi.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Tarama salt okunurdur; `WITHOUT ROWID` tablolar için örnek kimliğin `-1` olabileceği raporda açıklanır.
- 🔴 Bu SQLite özelliği PostgreSQL için sahipsiz kayıt denetimi sağlamaz. Tenant kapsamı olmayan iş ilişkileri, gerçek müşteri taraması, finansal uzlaştırma ve kontrollü onarım/öncesi-sonrası kanıtı açık; A-16 🔴, toplam **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz** kalır.

### 2026-10-08 — A-29 mali kararlar ve A-30/A-31 kapsam kapanışı

- 🟢 A-29 kararları yazıldı; Excel/CSV banka içe aktarımında hem giriş hem çıkış doluysa ya da tutar/yön uyuşmuyorsa satır reddedilir. Kilitli veya muhasebe fişine bağlı maaş snapshot silme engellendi. Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Güvenli mali yazımlarda rol değişikliğinin sunucu sınırında yeniden yetkilendirilmesi ve çalışma zamanı kabulü halen açık; A-29 sarı.
- 🟢 A-30 davranış değiştirmeyen P3 refactor'ı satış kapsamından erteleme kararıyla; A-31 çevrimdışı yok/Local/S3/backup sınırlarını belirleme kararıyla kapatıldı. Ayrıntı [A-29–A-31 karar belgesinde](A-29-31-URUN-KARARLARI.md).
- 🟢 Sayım **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**. A-03/A-10/A-15/A-16/A-23/A-28 kırmızıları dış kabul veya temel iş kalemi içerdiğinden bu kararlarla kapanmış sayılmadı.

### 2026-10-08 — A-29 yetki sorgularında circuit eskimesi

- 🟢 `KullaniciService` yetki sorguları artık oturumda saklanan rolü Admin kararı için kullanmıyor; kullanıcı etkinliği, güncel rol ve izinler her çağrıda DB'den okunuyor. Pasif/silinmiş kullanıcı, rol ve izin kayıtları yetki alamıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Bu ortak sorguyu kullanmayan mali servis yazımları merkezi olarak korunmuş sayılmaz; yazım sınırlarının envanteri ve rol değişikliği çalışma zamanı kabulü tamamlanmalı. A-29 sarı kalır.
- 🟢 2026-10-08 regresyon doğrulaması: mevcut Release test paketi **42/42 geçti**; A-29 rol değiştirme senaryosu pakette henüz yok, dolayısıyla bu sonuç tek başına yetki kabulünü kapatmaz.

### 2026-10-08 — Çözüm geneli Release kabul/doğrulama koşusu

- 🟢 `dotnet test MKFiloServis.slnx -c Release --no-restore` çıkış kodu **0**. İçindeki `MKFiloServis.Tests` paketi **42/42 geçti**; çözümdeki Web, DataSync, LisansDesktop, Shared ve MAUI Client projeleri derlendi. Client Android ve Windows Release link/AOT adımları da hata vermeden tamamlandı.
- 🟢 Ayrı `MKFiloServis.PlaywrightSmoke` aracı Release'de **0 uyarı / 0 hata** ile derlendi.
- 🟡 Tarayıcı smoke akışı çalıştırılmadı: `CRMFILO_TEST_USER` ve `CRMFILO_TEST_PASSWORD` test kimlik bilgileri bu oturumda tanımlı değil. Gerçek test kullanıcısı olmadan uygulama oturumuna dair PASS sonucu üretilemez.
- 🟡 Bu koşu müşteri PostgreSQL/SQLite kurulumunu, IIS installer kurulumunu, gerçek S3/SMTP/Luca bağlantılarını veya gerçek DB ve dosya kurtarmasını çalıştırmadı. Bunlar ayrı test ortamı/credential gerektirdiğinden A-01–A-29 kabul durumu yalnızca bu çözüm koşusuyla değişmez.

### 2026-10-08 — A-29 banka hareketi UI yazım kapıları

- 🟢 Banka hareketi formu oluşturma/düzenleme sırasında sırasıyla `BankaHareketleriYaz`/`BankaHareketleriDuzenle`, silme sırasında `BankaHareketleriSil`, içe aktarma yansıtmasında `BankaHareketleriYaz` güncel DB yetkisiyle tekrar sorgulanıyor. Rol kaldırılmış, hesap pasif/silinmiş veya oturum kapanmışsa yazım servisi çağrılmadan işlem reddedilir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; regresyon paketi **42/42 başarılı**.

### 2026-10-09 — A-16 banka yardımcı firma bağlantısı envanteri

- 🟢 Salt okunur DataSync envanterine beş sabit sorgu eklendi: banka hareketi–personel, araç, araç masrafı, mahsup/geri ödeme ve fatura ödeme eşleştirmesi–banka hareketi firma bağları.
- 🟢 Genişletilmiş geçici SQLite fixture `Complete=true`, toplam **7** bulgu verdi: A16-04 üç hatalı cari bağlantısını iki benzersiz araç olarak; A16-11–A16-15'in her birini de beklenen bir kayıt olarak buldu. Diğer sabit kontroller temiz kaldı; kaynak SHA-256 değişmedi.
- 🟢 Sınır durumunda A16-15 banka hareketi `FirmaId` boş/geçersizse de eşleşmeyi bulacak şekilde düzeltildi; minimal sentetik SQLite taramasında beklenen eşleştirme bulundu. Diğer zorunlu tablolar eksik olduğundan bu hedefli koşunun genel raporu `Complete=false` döndü.
- 🟢 A16-04, eski `INNER JOIN` nedeniyle kaçabilen sahipsiz kiralık/komisyoncu cari bağlantılarını `LEFT JOIN` ile buluyor; iki hatalı bağlantısı olan aynı araç tek bulgu sayılıyor.
- 🔴 Envanter 15 sabit kontrolü kapsasa da FK'siz/FirmaId'siz iş ilişkileri, gerçek müşteri bulgu değerlendirmesi ve yetkili kontrollü onarım/öncesi-sonrası kanıtı açık; A-16 🔴 kalır, görev renkleri/sayımı değişmez.
- 🟡 Bu düzeltme banka UI yollarını kapsar. Maaş, puantaj, fatura ve muhasebe yazımlarının tamamında ortak servis sınırı ve açık oturum rol değişimi UI kabul testi sürüyor; A-29 sarı.

### 2026-10-08 — A-29 maaş ve personel finans UI yazımları

- 🟢 Maaş oluşturma/düzenleme/silme/yeniden hesaplama, ödeme onay/iptali, avans ve borç ekleme/düzenleme/silme/ödeme işlemleri her olayda güncel DB izin sorgusu yapıyor. Veritabanı yetki sorgusu hata verirse işlem fail-closed reddedilir. İlk yüklemede otomatik üretilen kilitli maaş snapshot'ı için `MaasYaz` izni gerekir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; mevcut Release regresyon paketi **42/42 başarılı**.
- 🟡 Bu UI kontrolleri merkezi servis sınırı değildir. Puantaj/fatura/muhasebe dış çağrıları ve rol değiştirme tarayıcı kabul testi kapsam dışı kaldığından A-29 sarıdır.

### 2026-10-08 — A-29 banka/personel ödeme mali ekranları

- 🟢 Güncel izin kontrolü banka hareketi formu/listesi/aktarımına ek olarak personele geri ödeme/iptal ve gelir-gider kayıt/düzenleme/silme olaylarına eklendi. Gelir-gider işleminde muhasebe fişi seçilmişse `MuhasebeFisleriYaz` ayrıca aranıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**, otomatik Release test paketi **42/42 başarılı**.
- 🟡 Kontroller kullanıcı UI olaylarında. Puantaj ve kalan fatura/muhasebe servis yolları ile oturumda rol değişimi tarayıcı kabul testi henüz tamamlanmadı; A-29 sarı.


### 2026-10-08 — A-29 fatura servisinde güncel yetki denetimi

- 🟢 `CurrentPermissionGuard` servis sınırında oturum kimliğini HTTP/circuit üzerinden alır; etkin kullanıcı, güncel rol ve izinleri her çağrıda DB'den okur. Oturumsuz, pasif/silinmiş hesap veya silinmiş rol/izin ile yazım reddedilir; DB sorgu hatası yazımı durdurur. Oturumdaki Admin rol iddiası yetki kaynağı değildir.
- 🟢 `FaturaService` oluşturma, düzenleme, silme, Excel/XML/XML+PDF içe aktarma, PDF değiştirme, kalem güncelleme, stok kartlı kalem güncelleme, fatura eşleştirme, mahsup kapatma ve açık muhasebeleştirme girişlerinde güncel izin aranır. Genel fatura izni veya ilgili gelen/kesilen yön izni kabul edilir. Yön değiştirmede hem eski hem yeni yön, eşleştirme/mahsupta iki kayıt da denetlenir; kalemlerde izin DB'deki ana faturadan alınır.
- 🟢 XML+PDF içe aktarmada yeni faturanın PDF'i yazma izniyle eklenir; mevcut faturanın PDF'ini değiştiren dış servis çağrısı düzenleme izni ister. Açık muhasebeleştirme ayrıca `MuhasebeFisleriYaz` ister. `MuhasebeService.CreateFisAsync` doğrudan fiş oluşturmayı aynı güncel servis denetimiyle korur.
- 🟡 Türetilmiş ödeme toplamı (`UpdateOdenenTutarAsync`), otomatik/atomik muhasebe üretimi, hesap planı ve iç servislerin fiş düzenleme/silme çağrıları henüz bu ortak denetime taşınmadı. Bunların banka/masraf/hakediş iş yetkileriyle birlikte ele alınması gerekir; yalnız manuel muhasebe izni eklenerek bu iş akışları kapatılmış sayılmadı. Puantaj ve diğer mali servis/API girişleri de açık; A-29 sarı kalır.

### 2026-10-09 — A-16 banka hesap kimliği sınır kontrolü

- 🟢 A16-05'te eski `BankaHesapId > 0` filtresi boş, sıfır ve negatif hesap kimliklerini yok sayabiliyordu. Sorgu boş/geçersiz kimlik, sahipsiz hesap ve boş/geçersiz firma bağlarını bulacak şekilde genişletildi.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Release CLI, 15 sabit kontrolün tamamını içeren sentetik SQLite fixture'ında boş, `0`, `-4` ve bulunmayan `999` hesap kimlikli dört kaydı buldu; geçerli beşinci kayıt temiz kaldı. Rapor `Complete=true`, A16-05 `Count=4`, örnek kimlikler `101,102,103,104`, çıkış kodu `0`.
- 🔴 Bu sentetik test gerçek müşteri verisi taraması/onarımı veya PostgreSQL migration kabulü değildir; A-16 ve A-15 kırmızı, görev renk dağılımı değişmedi.
- 🟡 Bu değişiklik için çalışma zamanı rol kaldırma veya müşteri kabul testi çalıştırılmadı. Önceki 42/42 sonucu bu yeni servis değişikliğinin test kanıtı olarak kullanılmaz.
- 🟢 Son kodla Web Release derlemesi başarılı: **0 uyarı / 0 hata**. İlgili değişikliklerde `git diff --check` temiz. Bu sonuç çalışma zamanı kabulü değildir.

### 2026-10-09 — A-16 banka hareketi cari ve ödeme hesabı bağları

- 🟢 A-15 `GuardBankMovementTenantLinks` migration'ındaki kontrollerle sabit envanter uzlaştırıldı. Envanterin kaçırdığı `BankaKasaHareketleri.CariId` ve `PersonelOdemeHesapId` için A16-16/A16-17 eklenerek boş/negatif kimlik, sahipsiz hedef ve firma uyuşmazlığı kapsandı.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata** ile geçti.
- 🟢 Sentetik SQLite CLI kabulinde `Complete=true`, `ReadOnly=true`, toplam **6 bulgu**; A16-16 ve A16-17'nin her biri beklenen **3** örneği (çapraz firma, sıfır/negatif veya bulunmayan hedef) buldu. Kaynak DB SHA-256 değişmedi.
- 🟡 PostgreSQL 17 izole kümesi `127.0.0.1` dinleme soketi `Permission denied` nedeniyle başlamadı; yeni sorgular PostgreSQL'de çalıştırılmadı. Eski test sonuçları yeni sorgular için PostgreSQL kanıtı değildir. A-16 kırmızı; görev renk dağılımı değişmedi.
- 🔴 A-16'nın gerçek müşteri verisi, diğer FK'siz iş ilişkileri, yetkili değerlendirme ve kontrollü onarım/öncesi-sonrası kabuli açık.

### 2026-10-09 — A-16 yerel test DB ön taraması

- 🟡 `MKFiloServis.Web\App_Data\test.db` güncel Release CLI ile salt okunur tarandı; rapor ayrı `temp` dosyasına yazıldı. Rapor `Complete=false`, çıkış kodu **3**, **248** kontrol (247 temiz, bir şema eksiği), `FindingCount=0` verdi. A16-11 `Soforler` tablosu bulunmadığından çalışmadı; sıfır bulgu temiz tam tarama değildir. Yeni A16-16/A16-17 mevcut şemada temiz kaldı.
- 🟢 Kaynak dosyanın SHA-256 özeti değişmedi. Bu yerel test DB'sidir, müşteri DB'si değildir.
- 🔴 A-16 genel durumu değişmez; tam envanter, gerçek veri incelemesi ve kontrollü onarım kabulü açık.

### 2026-10-09 — A16-16/A16-17 SQLite sınır durumları

- 🟢 Ayrı sentetik tam şema fixture'ında çapraz firma, `0`, negatif, bulunmayan hedef ve geçersiz hareket firması durumları için her sorgu **5/5** beklenen kaydı buldu. İsteğe bağlı NULL ilişkiler bulgu vermedi. CLI `Complete=true`, `ReadOnly=true`, toplam **12** bulgu; kaynak SHA-256 değişmedi.
- 🟡 Bu koşu SQLite'tır. PostgreSQL kümesi soket izni nedeniyle başlatılamadı; müşteri verisi kullanılmadı.
- 🔴 A-16'nın genel eski veri kapsamı, müşteri bulgularının yetkili incelemesi ve onarım/öncesi-sonrası kabulü açık kalır.

### 2026-10-09 — A16 sabit kontrollerinde sıfır FirmaId sınırı

- 🟢 A16-06 ve A16-11–A16-14'te iki uçta da `FirmaId=0` olduğunda kaçabilen ilişki uyuşmazlığı düzeltildi; sorgular artık boş, sıfır ve negatif firma kimliklerini raporluyor. DataSync Release derlemesi **0 uyarı / 0 hata**.
- 🟢 Sentetik SQLite CLI kabulinde `Complete=true`, `ReadOnly=true`; A16-06/A16-11/A16-12/A16-13/A16-14 beklenen birer bulguyu buldu. Kaynak fixture SHA-256 değişmedi.
- 🟡 PostgreSQL ve gerçek müşteri verisi kabul edilmedi; A-16 kırmızı kalır.

### 2026-10-09 — A16-18 maaş snapshot personel/firma ilişkisi

- 🟢 FK ile güvence altında olmayan `MaasOdemeSnapshotlar.PersonelId`–`FirmaId` bağı için A16-18 eklendi. Aktif snapshot'ta eksik/geçersiz personel, bulunmayan personel, boş/geçersiz firma ve çapraz-firma durumu aranıyor.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Sentetik SQLite CLI `Complete=true`, `ReadOnly=true`; A16-18 beklenen **3** uyumsuz kaydı buldu, geçerli aynı-firma ve silinmiş satırı dışarıda bıraktı; kaynak SHA-256 değişmedi.
- 🟡 PostgreSQL/müşteri verisi doğrulanmadı. A-16 genel kapsamı ve kontrollü onarım kabulü açık; görev kırmızı kalır.

### 2026-10-09 — A-16 salt okunur araç teslimi olarak kapatıldı

- 🟢 Kullanıcı kapsam kararı: A-16'nın teslimi, eski veri/tenant risklerini salt okunur raporlayan DataSync aracıdır. 18 sabit kontrol ve şemadan keşfedilen FK/tenant denetimi bu kapsamı karşılıyor; görev yeşile alındı.
- 🟡 Gerçek müşteri verisinin taranması, bulguların yetkili onayı ve olası onarım müşteri geçiş operasyonudur; yapılmış sayılmaz. Sonradan eklenen A16-11–A16-18 PostgreSQL'de ayrıca çalıştırılmamıştır; rapor sınırı açıkça belgelenmiştir.
- 🟢 Güncel sayım **6 yeşil / 22 sarı / 3 kırmızı / 0 beyaz**. Satışa çıkarım kararı kalan A-03/A-10/A-15 ve müşteri kabul işleri nedeniyle kırmızı kalır.


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

### A-15 / SQLite dashboard şeması — 2026-10-08

🟢 Finans hareketleri için banka/karşı hareket/geri ödeme kaynak bağlarının firma eşleşmesi EF kaydetme sınırında doğrulanır; altı sync/async çapraz-firma regresyon senaryosu eklendi. Fatura ve banka hareketini bağlayan `OdemeEslestirme` kayıtlarında aynı firma zorunluluğu hem sync hem async kayıtta kontrol edilir; dört pozitif/negatif SQLite senaryosu geçti. SQLite dashboard hatasının kök nedeni olan tüm bekleyen migration'ları uygulandı diye kaydetme davranışı kaldırıldı. Eksik `BankaKasaHareketleri.IslemKimligi` ve `IslemOzeti` sütunları ile benzersiz indeks idempotent ve veri koruyucu biçimde açılışta onarılır. Üçüncü SQLite regresyon testi dashboard son hareket sorgusunun onarım sonrası çalıştığını doğrular. Son Release çözüm derlemesi 0 hata/uyarı; paket 96/96 başarılı. İzole SQLite test kanıtı `TEST-DOGRULAMA-2026-10-08.md` dosyasındadır.

🟡 A-15 genel görev durumu değişmedi: tüm varlık ilişkilerinin envanteri, müşteri eski verilerinin denetimi ve hedef DB/migration kabulü hâlâ tamamlanmalıdır. Gerçek müşteri verisine müdahale yapılmadı.

### 2026-10-08 — A-23 kaynak ve kurulum teslim temizliği

- 🟢 [A-23 teslim kararı](A-23-TESLIM-KARARI-2026-10-08.md) geçerli raporları ve tarihsel belgelerin Git geçmişindeki yerini sabitler. Mevcut satış durumu 31 görevlik envanterle izlenir.
- 🟢 Yerel `dbsettings.json`, `portalsettings.json` ve `backup_settings.json` korunarak Git takibinden çıkarıldı. Web publish ve ana/müşteri/güncelleme kurulum girdileri bu dosyaları dışlar; `setup/build.ps1` eski payload'da bulursa durur. Release Web publish çıktısında bu dosyalar, çerez ve `.db` bulunmadı. PowerShell betiği parser denetiminden geçti.
- 🟡 Gerçek Inno EXE ve hedef kurulum A-21; eski kimlik bilgisi rotasyonu A-06 kapsamında. A-23 🟢 kapandı. Güncel sayım **5 yeşil / 21 sarı / 5 kırmızı / 0 beyaz**; satış kabulü verilmedi.

### 2026-10-08 — GitHub CI kök düzeltmeleri

- 🟢 Gömülü PostgreSQL audit SQL dosyası genel `*.sql` ignore kuralından dar istisnayla çıkarılıp Git'e eklendi. Linux Tests/Docker ve Windows CodeQL'in önceki eksik kaynak derleme hatası giderildi.
- 🟢 NuGet audit Windows/MAUI hedeflerine uygun Windows 2025 çalıştırıcısında restore ve doğrudan/geçişli taramayı başarıyla tamamladı; bilinen açık raporlanmadı. GitHub Linux Tests Release çalışması **102/102 geçti, 0 atlanan**. [Ayrıntılı CI kanıtı](A-07-CI-DOGRULAMA-2026-10-08.md).
- 🟢 Docker imajı oluşturuldu, GHCR'ye gönderildi, aynı digest Trivy ile tarandı ve SARIF GitHub Security'ye yüklendi.
- 🟢 Windows tam çözüm derlemesi ve CodeQL C# analizi GitHub'da başarıyla tamamlandı.
- 🟡 Gerçek lisans, müşteri DB/restore ve saha kabulü açık; A-07 🟡, A-22 🟢 ve genel renk dağılımı değişmedi.

### 2026-10-08 — A-29 banka hesabı oluşturma firma/izin sınırı

- 🟢 Banka hesabı oluşturma, güncel yazma iznini tekrar denetleyen Serializable `WriteAccountAsync` sınırına alındı. Seçili firma olmadan veya çağrıda başka firmanın kimliği verilerek hesap açılması reddedilir; firma boşsa seçili firma atanır.
- 🟢 İki odaklı SQLite regresyonu geçti: yabancı firmaya kayıt/yarım satır oluşmaz, seçili firmaya kayıt yapılır; veritabanında Admin rolü kaldırılan oturum yeni hesap açamaz. Release hedefli test derlemesi başarılıydı.
- 🟡 Puantaj ve diğer otomatik mali yazımların atomiklik/yetki incelemesi ile gerçek rol değişimi ve müşteri veritabanı kabulü açık. A-29 genel rengi değişmedi.

### 2026-10-09 — A-29 puantaj finans snapshot firma ve transaction sınırı

- 🟢 Finansal snapshot üretimi seçili firma zorunluluğu ile sınırlandı; dönem uygunluğu ve yeni satırlar aynı Serializable transaction içinde işleniyor. Üretilen satır açık `FirmaId` taşıyor. Önceden silinmiş satır da benzersiz anahtar çakışmasına yol açmaması için tekrar oluşturulmuyor.
- 🟢 Finansal kayıt listesi, fatura uygunluğu, tekil fatura üretimi ve hakediş işleme çağrılarında seçili firma kapsamı açıkça uygulanıyor; hakediş cari eşleşmesi firma bazında sınırlandı. SQLite regresyonu yabancı dönemi reddetme, yerel firma atama ve tekrar çağrı davranışını doğruladı. Release Web derlemesi 0 hata/0 uyarı; test paketi **105/105 geçti**.
- 🟡 Fatura oluşturma/kalem/link ve hakediş fatura/snapshot zinciri hâlâ birden çok DbContext/commit kullanıyor. Bu atomiklik, diğer servis yetki sınırları ve gerçek müşteri kabulü açık; A-29 rengi sarı kalır.

### 2026-10-09 — A-10 depolama kapasitesi göstergesi eşik uyumu

- 🟢 Sistem Sağlığı ekranı artık servis eşiklerini tutarlı gösterir: %80 üzeri uyarı, %90 üzeri kritik; erişilemeyen depolama sürücüsü de yeşil gösterilmez ve hata mesajı görünür.
- 🟡 Harici alarm teslimi ve kapasite artışı/müdahale prosedürü bu kod değişikliğinin kapsamında değildir; gerçek depolama hacminde kabul edilmelidir. A-10 kırmızı kalır.

### 2026-10-09 — A-15 kapsam kararıyla kapatıldı

- 🟢 Kullanıcı kararıyla A-15'in kapanış ölçütü tanımlı tenant korumalarının ve yerel doğrulamanın teslimi olarak sabitlendi. Aktif plaka, banka import tekrarı, dönem snapshotları, varsayılan şablonlar, banka hareketi ilişkileri ve fatura-cari firma bağı için korumalar kaynakta mevcut.
- 🟢 Yeni fatura-cari PostgreSQL/SQLite migration'ı için sentetik SQLite regresyonları **5/5** geçti; Web Release derlemesi **0 uyarı / 0 hata**. Banka/ödeme firması korumalarının önceki izole iki sağlayıcı doğrulaması kayıtlı.
- 🟡 Müşteri DB migration'ı, eski müşteri verisi tarama/onarımı ve canlı eşzamanlılık kabulü dağıtım operasyonudur; bu repoda yapılmış sayılmaz. Bu sınır A-15 kod teslimini açık tutmaz.
- 🟢 Güncel sayım **7 yeşil / 22 sarı / 2 kırmızı / 0 beyaz**. Açık kırmızı görevler A-03 ve A-10'dur.

### 2026-10-09 — A-03 ve A-10 kapsam kararıyla kapatıldı

- 🟢 Kullanıcı kararıyla A-03'ün kurtarma betiği teslimi ve A-10'un şifreli dosya yaşam döngüsü teslimi bu görevlerin kapanış kapsamı olarak sabitlendi. A-03 altı betiğin parser kontrolü; A-10 odaklı testler **11/11** ve karantina arşivi geri okuma kanıtı kayıtlıdır.
- 🟡 Müşteri restore/kurulum, canlı depo/çoklu sunucu, legacy okuyucu ve kapasite alarmı kabulleri saha/işletim planına aittir; yapılmış sayılmaz ve A-03/A-10 görev durumunu açmaz.
- 🟢 Güncel dağılım **9 yeşil / 22 sarı / 0 kırmızı / 0 beyaz**. Kalan 22 sarı görev kendi müşteri/çalışma zamanı kabul koşullarıyla izlenir.

### 2026-10-09 — Önceki toplu kapanışın yeniden analizi

- 🟡 Önceki toplu kapanış kararı kaynak ve kanıt üzerinden yeniden değerlendirildi; renkler görev satırlarına göre düzeltildi.
- 🟡 Müşteri lisans/sır geçişi, gerçek DB ve restore, rol/tenant/API, dosya depolama, kurulum, entegrasyon, mali ve görsel saha kabulleri yapılmış sayılmaz; bunlar görev listesi dışındaki dağıtım/işletim kabulleridir.
- 🟢 A-13 fail-closed transfer ve A-29 mali yazım atomiklik/yetki kodları kapatıldı. Güncel dağılım **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz**; saha kabulü ve yayına çıkış onayı ayrıca verilmemiştir.

### 2026-10-09 — A-13/A-29 kod düzeltmeleri

- 🟢 A-13 araç firma taşıma ön kontrolü ve kayıt güncellemeleri tek Serializable transaction'a alındı. Eşlemesi olmayan masraf, banka/kasa, maliyet snapshotı, checklist ve fatura ilişkileri varsa taşıma sessiz veri ayrışması yerine hata ile durur. Kiralık plaka takip kayıtları FK ile aracı izlediği için artık boş/no-op döngüyle sayılmaz.
- 🟢 A-29 hakediş muhasebeye aktarımında güncel MuhasebeFisleriYaz izni, mükerrer fiş kontrolü ve aynı transaction içinde yazım sağlandı.
- 🔴 A-13 kapsamındaki kalan doğrudan AracId ilişkilerinin eşlemesi, A-29 puantaj fatura/kalem/link zincirinin tek transaction'a taşınması ve diğer mali yazımlarda ortak yetki/atomiklik henüz tamamlanmadı. İki görev de bu nedenle açık kaldı. Yeni test/derleme çalıştırılmadı.
### 2026-10-09 — A-13 ve A-29 kod tesliminin kapanışı

- 🟢 A-13 EF modelindeki doğrudan AracId tablolarını tarar. Desteklenen ilişkileri araçla aynı Serializable işlemde taşır; eşlenmeyen ilişki varsa işlem öncesi tablo adını vererek reddeder. Böylece seçilmeyen/doğrudan bağlı kayıtlar eski firmada sessizce bırakılmaz.
- 🟢 A-29 puantaj fatura/kalem/finans bağlantısı, hakediş fatura/durum/snapshot ve ödeme eşleştirme/türetilmiş fatura toplamı aynı Serializable transaction içinde kaydedilir. Fatura, muhasebe fişi ve eşleştirme yazımları güncel DB izin kontrolü yapar.
- 🟢 Web Release build 0 uyarı / 0 hata ile tamamlandı. Bu turda test çalıştırılmadı; gerçek müşteri ve PostgreSQL/SQLite çalışma zamanı kabulü dağıtımda yapılacak.

### 2026-10-09 — A-13/A-29 takip maddelerinin güncel duruma eşitlenmesi

- 🟢 Görev tablosu ve güncel değişiklik günlüğü esas alınarak önceki “kalan uygulama tamamlanmadı” kaydı tarihsel durum olarak bırakıldı; güncel satırlarda A-13 ve A-29 kod kapsamı kapalıdır.
- 🟢 Çalışma sırası düzeltildi: bu iki görevi yeniden açıp aynı kod işlerini tekrarlamak yerine 20 sarı görevin kabul kanıtı izlenir. A-13/A-29 için müşteri rol ve hedef sağlayıcı çalışma zamanı kabulü yapılmış gibi gösterilmez.
- 🟡 Son doğrulama: Web Release build başarılı, 0 uyarı / 0 hata. Bu güncellemede test çalıştırılmadı.


### 2026-10-09 — A-09/A-29 puantaj otomatik fatura muhasebe zinciri

- 🟢 Kaynak incelemesinde puantaj faturasının kalemi ayrı kaydedildiği, otomatik muhasebe fişinin ise ayrı DbContext ile kalem eklenmeden üretilebildiği ve hatanın yutulduğu bulundu.
- 🟢 Fatura yazımı mevcut context/transaction overload'u kullanıyor; puantaj kalemi fiş üretilmeden önce aynı transaction'da kaydediliyor. Otomatik fiş de paylaşılan context'e yazılıyor. Transaction içindeyken fiş hatası artık çağırana iletilip tüm zincirin rollback olmasını sağlıyor.
- 🟢 Web Release build **0 uyarı / 0 hata**. Bu turda test çalıştırılmadı. A-09 sarı (genel fatura zinciri ve hedef sağlayıcı/commit kabulü), A-29 yeşil (puantaj kapsamındaki kod düzeltmesi); görev toplamı **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz** kaldı.


### 2026-10-09 — A-19 DataSync SQLite bağlantı ayarlarının hata yolunda iadesi

- 🟢 PostgreSQL→SQLite aktarımı `foreign_keys` ve `synchronous` PRAGMA ayarlarını geçici değiştiriyordu. Önceki kodda hata halinde foreign key kontrolü kapalı kalabilir, başarılı yolda da bağlantı havuzuna NORMAL synchronous değeri bırakılabilirdi.
- 🟢 Başlangıç değerleri saklanıp aktarım/rollback sonrasında `finally` içinde geri yükleniyor. DataSync Release build **0 uyarı / 0 hata**.
- 🟡 Sentetik/gerçek SQLite/PostgreSQL hata enjeksiyonu ve sequence/FK eşitliği çalıştırılmadı; A-19 kabul durumu sarı kalır.

### 2026-10-09 — A-27 HTTP retry kaynak/istek yaşam döngüsü

- 🟢 Retry denemelerinin klon istekleri transient yanıt, ağ hatası, timeout ve başarı yollarında dispose ediliyor. Başarılı yanıt çağıranın özgün isteğine bağlanıyor; HTTP `VersionPolicy` deneme isteğine taşınıyor. Idempotent olmayan metotlarda belirsiz timeout/bağlantı kopması retry edilmez.
- 🟢 Web Release build **0 uyarı / 0 hata**; gerçek dış servis/UBL portal kabulü çalıştırılmadı. A-27 sarı kalır.

### 2026-10-09 — A-07/A-18/A-28 doğrulama ve renk güncellemesi

- 🟢 Tam yerel test paketi **136/136** geçti; Release Web ve test projeleri **0 uyarı / 0 hata** ile derlendi. İki eski test fixture güncellendi.
- 🟢 A-18 fatura başlangıç indeksinin başarılı geçişini ve duplicate eski satır halinde transaction rollback/eski indeks korumasını doğrulayan iki SQLite testi eklendi (**2/2**).
- 🟢 A-07 test/CI teslimi ve A-28 sağlayıcı kapsam/UI teslimi kapatıldı. A-18 PostgreSQL/temiz kurulum ve diğer müşteri kabulleri açık olduğundan genel renk dağılımı **13 yeşil / 18 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-19 DataSync iki sağlayıcı doğrulaması

- 🟢 Sentetik verili, yalnız localhost'ta çalışan PostgreSQL 17.5 kümesinde PostgreSQL→SQLite ve SQLite→PostgreSQL CLI aktarımı başarıyla doğrulandı; tablo/satır sonuçları, foreign key ve sequence durumu kontrol edildi.
- 🟢 İki yönde eksik hedef şema/kolon ön kontrolü veri değiştirmeden reddedildi. PostgreSQL COPY check constraint hatası transaction'ı geri aldı ve önceki hedef satırları korudu.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**; geçici PostgreSQL kümesi kapatıldı. Gerçek müşteri verisi veya üretim geçişi kullanılmadı.
- 🟢 A-19 tanımlı izole ürün kabulü tamamlandı; saha/hacim/credential kabulü dağıtım kapısına taşındı. Yeni güncel toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**; A-19 artık sarı listesinde değil.

### 2026-10-09 — A-18 PostgreSQL indeks ve temiz startup provası

- 🟢 PostgreSQL 17.5'te üretim indeks rutini iki minimal fixture üzerinde çalıştırıldı. Normal eski indeks geçişi scoped unique index'i kurup legacy index'i transaction içinde kaldırdı. Aynı firma/yön/numara için duplicate eski satırlarda unique index oluşturulamadı; transaction rollback oldu, legacy index kaldı ve yeni index oluşmadı.
- 🟡 İzole boş PostgreSQL'de tam `DbInitializer.InitializeAsync` başarısız oldu: mevcut legacy skip yolu, migration history tablosu henüz yokken ona insert yapıyor. Skip koşulu geçici olarak aşıldığında sonraki migration mevcut `AylikOdemeGerceklesenler` şemasını arıyor ve duruyor. Deneysel bypass kaynakta tutulmadı; veri dönüşümünü varsayarak migration atlanmadı.
- 🟡 A-18 sarı kalır. Kapanış için PostgreSQL/SQLite temiz ve eski şema başlangıç sözleşmesi, uygulanacak migration zinciri ve duplicate müşteri verisi kararı belirlenmelidir. Toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz** değişmedi.

### 2026-10-09 — A-01 anahtar yedeği hedef koruması

- 🟢 `ExportBackup` hedefi etkin DPAPI anahtar deposu veya legacy PEM anahtar dosyasıyla aynıysa işlem artık yazma başlamadan reddediliyor. Önceden bu hedef seçilirse şifreli yedek, çalışma anındaki imza anahtarı dosyasının üzerine yazıp sonraki lisans imzalama işlemlerini bozabilirdi.
- 🟢 LisansDesktop Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Bağımsız Windows profili geri yükleme, yetkili müşteri/lisans envanteri ve gerçek v3 lisans teslim kabulü hâlâ yapılmadı; A-01 sarı, dağılım **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-01 gerçek profil anahtar ve lisans envanteri incelemesi

- 🟢 Yerel DPAPI anahtarı ve legacy PEM özel anahtarı gizli içerik göstermeden bellekte açılıp uygulamanın yayımlanmış açık anahtarına karşı kontrol edildi; ikisi de aynı parmak izine sahipti. Release LisansDesktop içindeki `OpenSigningKey` gerçek mevcut profil anahtarıyla çalıştırıldı; eşleşmeyi doğruladı ve düz metin PEM kopyasını kaldırdı. Son durumda DPAPI etkin anahtar açılıyor, legacy PEM kalmadı.
- 🟢 Lisans SQLite DB salt okunur sayıldı: **51 kayıt (48 Sale, 3 Renewal)**; firma/makine/bitiş/süre/sürüm alanları dolu, 48 satış kaydında modül alanı boş, `V3Reissue` kaydı **0**. Kişisel/müşteri alanları rapora alınmadı ve DB değiştirilmedi.
- 🟢 Uygulama temizliği sonrasında LisansDesktop Release derlemesi **0 uyarı / 0 hata**; `git diff --check` whitespace hatası vermedi.
- 🟡 Eski 48 satışın modülleri yetkili sözleşme/iş sahibi olmadan atanamaz. Bağımsız profilde `.mkkey` geri yükleme ve v3 teslim kabulü de yapılmadı; A-01 sarı, toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-01 lisans hakları ile satış kaydı düzenlemesinin ayrılması

- 🟢 Kök kod incelemesinde satış kaydı düzenleme formu modül/sürüm haklarını gösteriyor, fakat DB update sorgusu bu hakları kaydetmiyordu. Böylece ekranda yapılan hak değişikliği imzalı lisansa yansımadığı halde kullanıcıya düzenleme tamamlanmış gibi görünebilirdi.
- 🟢 Düzenleme akışı şimdi kayıtlı ve formdaki modül/sürüm hakları farklıysa işlemi reddedip imzalı v3 yeniden basıma yönlendiriyor. Hakları değişmemiş eski kayıtta modül sütununun boş olması metadata düzeltmesini engellemiyor. LisansDesktop Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Hiçbir müşteri modül hakkı otomatik atanmadı; 48 eski satışın sözleşme envanteri, bağımsız `.mkkey` geri yüklemesi ve v3 teslim kabulü açık. A-01 sarı; toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-01 yeni satış lisansı uçtan uca sentetik imza doğrulaması

- 🟢 LisansDesktop'ın gerçek `BuildLicenseKey` yolu mevcut DPAPI anahtarıyla sentetik firma/makine için v3 imzalı anahtar üretti. Yayımlanmış açık anahtar imzayı doğruladı; imza içindeki modül hakkı değiştirilince doğrulama başarısız oldu. Sentetik lisans satış DB'sine yazılmadı, anahtar metni/özel anahtar çıktılanmadı.
- 🟢 Mevcut gerçek anahtar da eşleşme kontrolünden geçti; daha önce bulunan düz metin PEM yedeği güvenli eşleşme sonrası kaldırıldı. LisansDesktop Release **0 uyarı / 0 hata**.
- 🟡 Bu kanıt yeni müşteri v3 lisans üretimini doğrular; 48 eski satışın yetkili hak envanteri/gerçek v3 teslimi ve bağımsız profil yedek geri yüklemesi kapsamı ayrı ve açık kalır. A-01 sarı; genel **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.


### 2026-10-09 — A-01 kapsam kararı: yeni satış üretimi kapandı

- 🟢 A-01 yeni müşteri satışında v3 lisans üretim kodu olarak sınırlandı ve kapatıldı: mevcut DPAPI anahtarıyla sentetik anahtar üretildi, yayımlanmış açık anahtar doğruladı, modül hakları imzaya bağlı ve değiştirme denemesi reddedildi. LisansDesktop Release **0 uyarı / 0 hata**.
- 🟡 Önceki 48 satışın modül hakları boş. Bunların yeniden basımı yalnızca sözleşme/iş sahibiyle müşteri yenileme ya da geçiş operasyonunda yapılır; bu karar eski müşteri geçişinin tamamlandığı anlamına gelmez. Bağımsız anahtar kurtarma A-04; normal/Admin modül erişim kabulü A-02 kapsamındadır.
- 🟡 Güncel görev sayımı **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**. Bu kapsam kararı A-02/A-04/A-05/A-06 veya genel Go/No-Go kararını kapatmaz.

### 2026-10-09 — A-02 SignalR ve puantaj sayfa erişim düzeltmesi

- 🟢 `EvrakHub.SubscribePersonel` istemcinin verdiği personel kimliğine göre grup aboneliği veriyordu. Abonelik artık oturumdaki kullanıcı ID'sini aktif/kilitsiz kullanıcı kaydına bağlar, yalnızca o kullanıcının `SoforId` değerine izin verir ve personelin aktif/silinmemiş olduğunu doğrular. İstemcinin başka personelin evrak bildirim grubuna katılması engellendi.
- 🟢 `/puantaj/cari-hiyerarsi` rotasında yalnız `[Authorize]` vardı; personel puantaj içeriği lisans denetimi olmadan açılabiliyordu. Rota `Licensed:personel` politikasına bağlandı. Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 A-02 uçtan uca normal/Admin, lisans iptali/değişimi, rol revokasyonu ve firma sınırı kabulini bekliyor; bu nedenle görev rengi ve toplam **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz** kaldı. Genel satış onayı değildir.

### 2026-10-09 — A-02 dashboard hakediş özeti lisans kapısı

- 🟢 Dashboard'da `OperasyonelOzetBandi` filo hakediş tutarlarını lisans kontrolü olmadan sorgulayıp gösteriyordu. Bileşen artık yalnız `filoservis` modül hakkı etkin olduğunda oluşturuluyor; lisanssız durumda sorgular da çalışmıyor.
- 🟡 A-02 normal/Admin, lisans değişimi/iptali, rol iptali ve firma sınırı çalışma zamanı matrisi bekliyor; toplam **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02 açık dashboard lisans yenilemesi

- 🟢 Dashboard lisans cache değişikliklerini dinler ve 30 saniyede bir lisans ile güncel kullanıcı izinlerini doğrular. Modül/rol hakkı kalkar veya lisans sona ererse yetkisiz bölümlerin verisini sıfırlayıp yalnız erişilebilir bölümleri yeniden yükler; hak eklenirse yeni bölümü açar. Derleme kanıtı kaynak uygulamasını doğrular; canlı erişim matrisi ayrıca kabul edilmelidir.
- 🟡 A-02 runtime kabulü bekliyor; toplam **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02 anonim Grafana arama istisnası kaldırıldı

- 🟢 `AnalitikController.GrafanaSearch` üzerindeki `[AllowAnonymous]`, controller seviyesindeki Bearer ve `Licensed:raporlar` denetimlerini atlıyordu. Arama endpoint'i artık Bearer + rapor modül lisansı altında çalışır ve `raporlar.oku` iznini güncel DB'den denetler.
- 🟡 A-02 tam runtime lisans/rol/tenant matrisi bekliyor; toplam **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02 kod teslim kapsamı kapatıldı

- 🟢 Kaynak taramasında bulunan açıklar giderildi: personel SignalR IDOR'u, lisanssız puantaj hiyerarşi rotası, lisanssız dashboard hakediş özeti/açık oturum eski verisi ve anonim Grafana arama istisnası. MVC API'leri Bearer ve modül politikalarıyla, modül sayfaları `Licensed:*` ile korunur; Web Release **0 uyarı / 0 hata**.
- 🟢 A-02'nin **kod teslimi** kapatıldı; durum dağılımı **16 yeşil / 15 sarı / 0 kırmızı / 0 beyaz**. A-02 normal/Admin, lisans ve rol iptali, firma sınırı runtime matrisi aşama 2 yayına çıkış kabulinde ayrıca zorunludur; bu kapı çalıştırılmadan satış onayı verilmez.

### 2026-10-09 — A-02 global arama rol izni

- 🟢 Global arama her cari/araç/personel/fatura/güzergâh sorgusundan önce kategoriye ait güncel okuma rol iznini ve modül lisansını kontrol ediyor. İzin yoksa o kategori için DB sorgusu açılmıyor; bu, lisanslı ama rol izni olmayan oturumların arama önerilerinden kayıt bilgisi almasını engelliyor. Web Release **0 uyarı / 0 hata**.
- 🟢 A-02 kod teslimi yeşil; normal/Admin ve firma runtime matrisi ayrı yayına çıkış kabulidir. Toplam **16 yeşil / 15 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-05 açık circuit yetki iptali

- 🟢 Girişte hesap/rol/izinler DB'den tazelenir; kullanıcı pasif, silinmiş, kilitli veya rolü geçersizse oturum açma reddedilir. Açık circuit dakikada bir hesap etkinliği ile rol/izin parmak izini doğrular; değişiklikte oturumu sonlandırır.
- 🟢 Oturum sonlanınca kullanıcı firma kapsamı ve tüm-firmalar seçimi temizlenir, arayüz login'e yönlenir.
- 🟡 A-05 runtime kabulü ve yüksek eşzamanlı oturumlarda dakikalık sorgu etkisi ölçülmediğinden genel durum sarı; satış onayı değildir. Web Release derlemesi 0 uyarı/0 hata; `git diff --check` temiz. Test paketi çalıştırılmadı.

### 2026-10-09 — A-05 2FA deneme kilidi

- 🟢 Kaynak taramasında hatalı TOTP kodlarının başarısız giriş sayacına eklenmediği, ayrıca parola doğru olduğunda sayaç 2FA doğrulaması tamamlanmadan sıfırlandığı bulundu. Beş hatalı parola/TOTP doğrulaması artık ortak 15 dakikalık kilidi başlatır; sayaç yalnız tam girişten sonra sıfırlanır.
- 🟢 2FA bekleme aşaması hesabın güncel aktif/silinmiş/kilit durumunu tekrar denetler. Hesap durumu doğrulama sırasında değişirse provider oturum açmayı reddeder ve giriş sonucu başarısız döner.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test paketi çalıştırılmadı.
- 🟡 TOTP brute-force/kilit kabulü, normal/Admin ve tenant matrisi, circuit/token DB yükü runtime'da doğrulanmadı. A-05 sarı kalır; toplam **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-05 parola değişiminde oturum iptali

- 🟢 JWT'ye parola hash'ini açığa çıkarmayan, uygulama imza sırrıyla üretilen HMAC oturum damgası eklendi. API her token doğrulamasında damgayı mevcut parola hash'iyle sabit-zamanlı karşılaştırır; parola değişince/sıfırlanınca mevcut JWT ve refresh zinciri anında reddedilir.
- 🟢 Blazor circuit yetki parmak izine parola sürümü de eklendi; değişiklik dakikalık doğrulamada oturumu kapatıp tenant bağlamını temizler. Güncel hesap doğrulanmadan giriş sonucu başarılı dönmez.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test paketi çalıştırılmadı. Mevcut eski JWT'ler damga taşımadığı için dağıtım sonrasında yeniden giriş gerekir.
- 🟡 Normal/Admin-firma, JWT iptal ve token/circuit DB yükü çalışma zamanı kabulü yapılmadı. A-05 sarı; toplam **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-05 süresiz oturum ve refresh uzatması kapatıldı

- 🟢 Blazor `sessionStorage` kaydı artık kullanıcı kimliği ile ilk giriş zamanını birlikte taşır. Eski biçim, gelecek zamanlı veya 12 saati aşmış oturum geri yüklenmez; açık circuit zaman aşımında en geç bir dakika içinde kapatılır.
- 🟢 JWT `auth_started` ilk girişte atanır ve refresh sırasında korunur; token süresi ilk girişten itibaren 12 saati aşamaz. Her token doğrulaması mutlak yaşı kontrol eder; refresh oturumu uzatamaz. Süre dolunca parola/2FA ile yeniden giriş gerekir.
- 🟢 Önceki değişikliklerle birlikte parola değişimi JWT damgasını anında iptal eder; TOTP başarısızlığı ortak 15 dk kilide dahildir.
- 🟡 A-05 runtime zaman aşımı/refresh ve normal/Admin-firma matrisi ile JWT/circuit DB yükü sahada kabul edilmedi. Web Release derlemesi 0 uyarı / 0 hata; git diff --check temiz. Bu turda test çalıştırılmadı. Renkler **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-05 tenant restore ve paralel kilit denemesi

- 🟢 Tenant restore sırasında güncel rol Admin değilse eski tarayıcıdaki farklı firma seçimi reddedilip temizlenir; yalnız varsayılan aktif firma kabul edilir. Admin dışı kullanıcı için `SetTumFirmalar(true)` provider katmanında da engellenir.
- 🟢 Hatalı parola/TOTP deneme sayısı atomik DB güncellemesiyle artırılır; eşzamanlı istekler önceki sayaç değerini ezerek 5 deneme kilidini atlayamaz. Hesap kilitlenince eşzamanlı kalan denemeler güncelleme koşulundan düşer.
- 🟢 Web Release derlemesi 0 uyarı / 0 hata; git diff --check temiz. Bu turda test paketi çalıştırılmadı.
- 🟡 Tenant rol değişimi yarış penceresi, eşzamanlı 2FA kilidi, normal/Admin ve 12 saat oturum kabulleri runtime ortamında doğrulanmadı. A-05 sarı, görev sayımı **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-05 hesap kilidi anında uygulama ve tam paket

- 🟢 `CurrentPermissionGuard` her hassas işlemde güncel DB hesap kilidini kontrol eder; açık circuit yeniden doğrulama periyodunu beklemeden korunan işlemler reddedilir.
- 🟢 Oturum süresi/firma restore/atomik lockout için 12 politika testi ve DB yetki revokasyonu/kilit guard için 5 regresyon testi geçti (**17/17**).
- 🟢 Tam test paketi **152 geçti / 2 PostgreSQL ortam testi atlandı / 0 başarısız**. Web Release derlemesi 0 uyarı / 0 hata; `git diff --check` temiz.
- 🟡 Normal/Admin-firma akışının browser/API üzerinden gerçek kullanıcılarla kabulü, 12 saat sonunda canlı token/refresh sınırı ve JWT/circuit DB yükü ölçümü açık. A-05 sarı; toplam **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-05 firma seçimi provider katmanında sınırlandı

- Firma değiştirme servisi artık seçimi provider'da güncel DB hesabı ve rolüyle doğrular; normal kullanıcı yalnız etkin varsayılan firmaya geçebilir, Admin yalnız etkin firmaları seçebilir. Etkin olmayan/yok firma ve oturumsuz firma seçimi reddedilir. “Tüm Firmalar” geçişi de güncel DB Admin rolüne bağlıdır.
- Firma restore/seçim matrisi ve önceki oturum/lockout/izin guard regresyonları **23/23 geçti**. Tam test paketi **158 geçti / 2 PostgreSQL ortam testi atlandı / 0 başarısız**. Web Release derlemesi **0 uyarı / 0 hata**, `git diff --check` temiz.
- 🟢 A-05 kod teslimi kapandı; tenant firma seçme bypass'ı provider katmanında giderildi. Odaklı testler **23/23**, tam paket **158 geçti / 2 PG ortam testi atlandı / 0 başarısız**, Web Release **0 uyarı / 0 hata**. Güncel görev sayımı **18 yeşil / 13 sarı / 0 kırmızı / 0 beyaz**.
- Yayına çıkış için browser/API normal/Admin firma matrisi, gerçek token/refresh 12 saat sınırı, circuit iptali ve DB yükü ayrıca ölçülüp [son aşama test planına](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) kaydedilir; bu saha kanıtı A-05 kod teslimini yeniden açmaz.

### 2026-10-10 — A-06 JWT secret kuralı ve Git geçmişi denetimi

- 🟢 `JwtSecretPolicy` host başlangıcı ve token üretiminde ortak doğrulama yapar: boş, `REPLACE_`, 32 UTF-8 bayttan kısa ve bilinen engelli ifşa edilmiş anahtar reddedilir. Güncel kaynak ağacında 24+ karakterli olası sır literal'i bulunmadı.
- 🟡 Git geçmişi taramasında eski `appsettings.json` içindeki bilinen engelli JWT anahtarı fingerprint'i doğrulandı. Eski preproduction/publish/build ve lisans aracı geçmişinde başka secret alanı adayları bulundu; bunlar sahiplerince sınıflandırılmalı. Secret değerleri tarama çıktısına alınmadı. Geçmiş/uzak kopya silme veya koruma kararı verilmedi.
- 🟢 JWT secret politika testleri **7/7**, Web Release **0 uyarı / 0 hata**, tam test paketi **165 geçti / 2 PG ortam testi atlandı / 0 başarısız**. `git diff --check` temiz.
- 🟡 A-06 aktif Production secret rotasyonu ve eski JWT'nin `401` kanıtı olmadan kapanmaz. [Rotasyon prosedürü](A-06-JWT-SECRET-ROTATION-2026-10-10.md); güncel toplam **18 yeşil / 13 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-20/A-21 ürün düzeltmesi ve 13 sarı kapanış denetimi

- A-20 üretim C#/Razor takvim ifadeleri İstanbul iş gününe taşındı; statik kalan `Today`/`Now.Year/Month/Day/Date` sayısı **0**, Web Release **0 uyarı / 0 hata**. Kalan 320 `DateTime.Now` kullanımının an/veri semantiği ve eski müşteri verisi kararı açık.
- A-21 ana IIS ve güncelleme paketleri güncel publish çıktısından derlendi. ACL'si eksik eski doğrudan müşteri paketi yeni satış `build.ps1` akışından çıkarıldı. Derlenen doğrulama EXE'leri imzasız; hedef Windows kurulum/restore/lisans kabulü yapılmadı.
- A-09/A-18 satırlarının eski sağlayıcı ve boş PostgreSQL durumu düzeltildi. Diğer dokuz sarı görevin dış ortam/veri kabul kanıtı değişmedi; renkler **18/13/0/0**. Ayrıntılı değişiklik ve kanıt sınırı [ana durum kaydında](SATISA-CIKARIM-SON-DURUM-2026-10-05.md#a-20-iş-günü-ve-a-21-kurulum-kök-düzeltmeleri).

### 2026-10-10 — A-06 uygulama ve paket güncellemesi

- Token imzası, doğrulama anahtarı ve parola damgası aynı süreç sabitinden üretilir; Production sırrı yalnız ortam değişkeni sağlayıcısından alır ve JSON/argümanda gölgelenmiş eski sır varsa başlangıcı reddeder. Güncel Web Release **0 uyarı / 0 hata** ile derlendi; yeni otomatik test çalıştırılmadı.
- Güncel Web çıktısı ana IIS ve güncelleme doğrulama paketlerine alındı. SHA-256 kayıtları `setup/output/validation-2026-10-10/SHA256SUMS.txt` içindedir. EXE'ler imzasızdır; hedef kurulum kabulü A-21 kapsamında açık.
- Aktif Production vault, eski JWT ve düğüm erişimi olmadan canlı rotasyon veya eski token `401` doğrulaması yapılamadı. A-06 **🟡** kalır; toplam **18 yeşil / 13 sarı / 0 kırmızı / 0 beyaz**. [Kapanış adımları](A-06-JWT-SECRET-ROTATION-2026-10-10.md).

### 2026-10-10 — A-06 tarihsel JWT envanteri

- Yerel Git geçmişindeki 170 JSON blobu ve JWT secret ataması içeren betikler değerler gösterilmeden tarandı. Dört ayrı tarihsel JWT sırrının SHA-256 parmak izleri uygulama engel listesine alındı; güncel Web ile ana IIS/güncelleme doğrulama paketleri yeniden üretildi.
- Aktif vault, düğüm ve eski JWT erişimi yok. Gerçek rotasyon, eski token `401` kanıtı, tarihsel diğer secret adaylarının sahipleriyle sınıflandırılması ve uzak kopya kararı açık. A-06 **🟡**; genel dağılım **18/13/0/0**. [A-06 kaydı](A-06-JWT-SECRET-ROTATION-2026-10-10.md).

### 2026-10-10 — A-06 aktif kurulum beyanı

- Kullanıcı, dört eski JWT sırrını kullanan aktif müşteri/Production kurulumu olmadığını bildirdi. `401` ve canlı JWT rotasyonu bu kapsamda uygulanamaz. Tarihsel DB/API/lisans adaylarının geçerlilik/iptal ve uzak kopya kararı açık olduğundan A-06 **🟡** kalır. [Koşullu kabul](A-06-JWT-SECRET-ROTATION-2026-10-10.md).

### 2026-10-10 — A-09 mali transaction kod kapanışı

- 🟢 A-09 kod kapsamı kapatıldı. Fatura, kalem, karşı fatura ve otomatik muhasebe fişi ana fatura akışında aynı execution strategy ve Serializable transaction içinde yazılır. Transaction içindeki fiş hatası faturayı da geri alır. Retry yeni context kullanır ve önceki denemede üretilen kimlik/navigation değerlerini yeniden kullanmaz; commit başladıktan sonra sonucu belirsiz bir işlem otomatik tekrar mali yazıma çevrilmez.
- 🟢 Personel avans/borç/ödeme/mahsup/maaş, banka-kasa hareketleri, transfer/ters fiş, puantaj ve hakediş zincirlerinde ortak context/transaction ve kalıcı tekrar korumaları mevcut. Önceki odaklı SQLite regresyon kanıtları görev satırında referans alınmıştır.
- 🟡 Bu çalışma alanında hedef müşteri PostgreSQL/SQLite bağlantısı yok. Kesinti anında commit sonucu, sağlayıcı retry davranışı, iki süreçli bakiye/fiş numarası ve audit rollback saha kabulinde çalıştırılmalıdır; çalıştırılmış gibi kaydedilmez.
- A-09 🟢 kod teslimi; hedef DB kabulü satış Go/No-Go kapısıdır. Güncel toplam **20 yeşil / 11 sarı / 0 kırmızı / 0 beyaz**. Satışa çıkış onayı verilmedi.


### 2026-10-10 — A-11 uzak dosya deposu ve kısmi yükleme kapanışı

- S3/MinIO SecureFileService'in şifreli upload/download/copy/exists/delete akışında etkinleştirildi; silme aktif nesneyi kaldırmadan önce geri alınabilir uzak karantina kopyasını oluşturur. Hata/izin reddi cleanup günlüğünde kalır ve yeniden denenir.
- SigV4 canonical Host alanı özel portu içerir; nesne anahtarı klasör ayraçları segment kodlamasıyla korunur. Araç ve tedarikçi çoklu yüklemelerinde başarılı/hatalı/sonucu belirsiz dosyalar ayrıştırılır, liste tazelenir.
- Web Release derlemesi 0 uyarı / 0 hata, diff check temiz. Bu turda test çalıştırılmadı. Gerçek S3/MinIO ve Windows disk izin/kilit kabulü yapılmadı.
- A-11 yeşil kod teslimi; dış depolama kabulü satış Go/No-Go kapısıdır. Güncel toplam 21 yeşil / 10 sarı / 0 kırmızı / 0 beyaz.

### 2026-10-10 — A-12 Excel değer doğrulama kod kapanışı

- Yinelenen normalize başlıklar mutasyondan önce reddedilir. Satır bazında şase numarası uzunluğu, model yılı, koltuk sayısı, KM, tarih, aktiflik, araç tipi ve sahiplik tipi kayıttan önce doğrulanır; bozuk değerler varsayılanlara sessizce düşmez.
- Firma/modal sürüm denetimi ve tek aktarım kilidi korunur. Satır başına transaction ve kısmi sonuç sayımı sürer; hata alan satır yazılmaz, önceden commit edilmiş satırlar sonuçta görünür ve liste tazelenir.
- Web Release derlemesi 0 uyarı / 0 hata; diff check temiz. Otomatik test çalıştırılmadı, canlı XLSX/tarayıcı/DB kabulü yapılmadı.
- A-12 yeşil kod teslimi; gerçek XLSX, firma değişimi, Dispose ve kısmi kayıt doğrulaması satış Go/No-Go kapısıdır. Güncel toplam 22 yeşil / 9 sarı / 0 kırmızı / 0 beyaz.

### 2026-10-10 — A-14 araç listesi firma ve çift işlem kapanışı

- Düzenleme formu firma seçimi değişince kapatılır; bekleyen yükleme sürüm ve firma eşleşmesini doğrular. Normal araç kaydı çift gönderime kapatılır ve işlem sürerken düğme durumu gösterilir. Servis güncellemesi güncel firma kapsamını kayıt öncesi kontrol eder.
- Araç/plaka geçmişi listesi split query ile yüklenir; collection join satır çarpımı azaltılır. Araç listesinin firma/sürüm koruması ile silme ve plaka modallarının tek işlem sürümü kontrolleri sürer.
- Web Release derlemesi 0 uyarı / 0 hata; diff check temiz. Otomatik test ve canlı UI/DB kabul çalıştırılmadı.
- A-14 yeşil kod teslimi; A→B→A/yavaş yanıt, çift işlem, evrak/audit rollback ve hedef filo hacmi satış Go/No-Go kapısıdır. Güncel toplam 23 yeşil / 8 sarı / 0 kırmızı / 0 beyaz.


### 2026-10-10 — A-17 mali import atomikliği ve kod teslimi

- 🟢 Banka/kasa CSV/XLSX/PDF importunda seçilen satırlar önceden satır satır commit edildiğinden aktarım kısmen kalabiliyordu. `CreateImportedBatchAsync` ile izin/firma doğrulaması, satır doğrulama, kayıt ekleme ve tek `SaveChanges` tek Serializable transaction’a alındı. Her staged GUID kalıcı işlem anahtarıdır; aynı içerikle retry mevcut kaydı döndürür, farklı içerik/silinmiş kimlik reddedilir. Başarısız paket rollback olur ve stage yeniden deneme için korunur.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test çalıştırılmadı. A-17 kod teslimi yeşildir.
- 🟡 Gerçek banka dosyaları, normal/Admin ve firma geçişi, fatura API, PDF/SMTP ve sağlayıcı rollback kabulü yayın öncesi Go/No-Go kapısıdır; canlı kabul yapılmış sayılmaz. Güncel renk **24 yeşil / 7 sarı / 0 kırmızı / 0 beyaz**.


### 2026-10-10 — A-17 fatura API sorgu sınırı

- 🟢 Fatura liste filtresi DB sorgusuna taşındı ve sayfalama eklendi; legacy array API 100 üzerindeki sonuçlarda belleğe bütün kayıtları almak yerine 400 ile yeni sayfalı route’u bildiriyor. Fatura numarası araması tek SQL sorgusu; bulunan kaydın yön bazlı izin denetimi de uygulanıyor.
- 🟢 Web Release derlemesi 0 uyarı / 0 hata; test çalıştırılmadı. A-17 kod teslimi 🟢, canlı ve harici kabul koşulları satış öncesi Go/No-Go adımında. Renk sayısı 24 yeşil / 7 sarı / 0 kırmızı / 0 beyaz.

### 2026-10-10 — A-18/A-20/A-21/A-24–A-27 kök düzeltmeleri

- A-18'de SQLite migration exception sonrasında migration history'sini otomatik doldurup hatayı bastıran kurtarma kaldırıldı; hatalı/eksik şema artık başarılı başlangıç sayılmaz. Eski müşteri DB yükseltme/parity/rollback fixture'ı yok, görev sarı.
- A-20 EF kaydında Local tarih gerçek UTC'ye çevrilir; Unspecified mevcut UTC sözleşmesine göre ele alınır. 320 `DateTime.Now` kullanımı ve tarihsel kolon anlamları sınıflandırılmadı; görev sarı.
- A-21 Inno komut başlatma/çıkış hataları kurulum başarısızlığı verir; config ve SQLite ACL IIS başlatılmadan uygulanır. Windows kurulum kabulü dış kapıda.
- A-24/A-25 cache kaynak teslimi, A-26 XLSX/PDF/baskı kaynak teslimi ve A-27 sınırlandırılmış HTTP retry kod teslimi tamamlandı. Redis/yük, görsel çıktı, Luca/UBL gerçek kabulü Go/No-Go adımlarıdır.
- Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` temiz. Bu turda test, Inno EXE derlemesi veya dış kabul çalıştırılmadı.
- Güncel görev renkleri **29 yeşil / 2 sarı / 0 kırmızı / 0 beyaz**. Müşteri/üretim kabulü yapılmış sayılmaz ve genel satış onayı verilmez.

### 2026-10-10 — A-18 migration watermark ve A-20 tarih envanteri

- A-18 legacy SQLite watermark 2026-09-25 öncesine alındı; Ekim unique-index/tenant migration’ları history’ye topluca yazılıp atlanmaz. Baseline öncesi TargetModel tablo/kolonları, indeks adları ve FK tablo/kolon eşleşmeleri SQLite kataloğuyla karşılaştırılır. Eksik bulguda history kaydı oluşturulmaz; PostgreSQL migration hatası da history recovery başlatmaz. Bu statik koruma tam parity değildir; eski müşteri fixture, trigger/veri invariant’ı, migration/rollback kabulü açık.
- A-20 kaynak taraması 321 `DateTime.Now` ifadesi buldu; 28'i `TestDataSeeder` içinde. Diğer kullanımlar timestamp yazımı, form varsayılanı, süre eşiği, rapor tarihi ve dosya adı/görsel metin karışımıdır. Bunları körlemesine UTC veya İstanbul'a çevirmek saklanan zamanı ya da kullanıcıya gösterilen saati kaydırabilir. Local EF değerinin UTC dönüşümü ve takvim referanslarının İstanbul günü kullanması uygulandı; alan semantiği/legacy değer kararı hâlâ açık.
- Görev renkleri **29 yeşil / 2 sarı**; A-18 ve A-20 sarı. Web Release build **0 uyarı / 0 hata**; otomatik test ve müşteri DB kabulü yapılmadı.

### 2026-10-10 — A-20 tarih kullanım sınıflandırması

- 128 saf çıktı biçimlendirme ifadesi `BusinessTime.Now` kullanacak şekilde güncellendi; rapor saati ve saatli dosya adları artık sunucunun yerel saat dilimine bağlı değil.
- Statik taramada 193 `DateTime.Now` kaldı: 28 test verisi, 165 çalışma kodu. Persist edilen olay zamanları, form varsayılanları ve eşik sorguları alan anlamına göre ayrıştırılmadan otomatik değiştirilmedi. Eski kayıtların UTC/İstanbul semantiği ve müşteri verisi kabulü açık; A-20 sarı.
- Web Release build **0 uyarı / 0 hata**; test çalıştırılmadı. Genel renk **29 yeşil / 2 sarı**.

### 2026-10-10 — A-18 migration fail-fast ve A-20 saat semantiği kapanışı

- **A-18:** PostgreSQL’in migration çalıştırmadan history tablosuna ID ekleyen legacy FK atlaması ve duplicate tablo/kolon sonrası seçilmiş migration’ları uygulanmış sayan recovery yolları kaldırıldı. Şema uyuşmazlığı initialization hatası verir; migration history otomatik değiştirilmez. Eski müşteri PostgreSQL/SQLite fixture, veri/tenant/mali parity ve rollback kanıtı olmadığı için A-18 **🟡** kaldı.
- **A-20:** Web/Shared üretim C#/Razor kaynaklarında `DateTime.Now` taraması **0** (yalnız test veri seeder’ında 28 sentetik kullanım kaldı). Olay/audit zamanları UTC, iş günü ve yerel iş kuralları Istanbul `BusinessTime`; yedekleme UTC anını saklar, İstanbul planına göre çalışır ve UI’da İstanbul saati gösterir. Geçmiş DB kayıtlarına saat farkı uygulayan dönüşüm yapılmadı. A-20 **🟢 kod teslimi** olarak kapatıldı.
- Web Release build **0 uyarı / 0 hata**; `git diff --check` temiz. Test çalıştırılmadı. Görev dağılımı **30 yeşil / 1 sarı / 0 kırmızı / 0 beyaz**; A-18 eski şema kabul kapısı ve satış Go/No-Go açık.
### 2026-10-10 — A-18/A-20 teknik kapanış güncellemesi

- A-18’in legacy SQLite watermark guard’ı tablo/kolon, EF indeks adı ve FK principal/from/to eşleşmelerini kontrol eder. Uyumsuz DB’de migration geçmişi yazılmadan başlangıç kesilir. Gerçek eski PostgreSQL/SQLite fixture, mali/tenant parity ve rollback yok; A-18 sarı.
- A-20 üretim Web/Shared C#/Razor kaynaklarında `DateTime.Now` sıfır; UTC olay zamanı, İstanbul iş takvimi ve saat dilimi bağımsız yedek planı uygulandı. A-20 kod teslimi yeşil. Görev dağılımı **30 yeşil / 1 sarı / 0 kırmızı / 0 beyaz**.
### 2026-10-10 — A-18 transaction ve A-20 yerel saat gösterimi

- A-18 PostgreSQL timestamptz şema uyarlaması tüm kolonlar için tek transaction’da çalışır ve yarım DDL bırakmaz. SQLite legacy watermark kontrolü tablo/kolon, indeks ve FK eşleşmelerini kapsar. Eski PostgreSQL/SQLite fixture ve parity/rollback kabulü olmadığı için görev sarı.
- A-20 statik taraması production Web/Shared kaynaklarında `DateTime.Now`, `DateTime.Today` ve `ToLocalTime()` bulmadı. UTC anlar İstanbul’a çevrilir; muhasebe ve rezervasyon tarih/saat değerleri iş duvar saati olarak korunur. A-20 yeşil. Görev dağılımı **30 yeşil / 1 sarı**.

### 2026-10-10 — A-18 migration öncesi DDL bypass kaldırıldı

- Program başlangıcından genel model-kolon eşitlemesi kaldırıldı; eski şema artık EF migration'larından önce hedef modele yaklaştırılmıyor. Pending migration ID'lerini history'ye yazan kullanılmayan yol `SchemaSyncHelper`'dan çıkarıldı.
- `FisNoCounters` legacy uyarlaması migrations sonrasına taşındı ve hatası artık fatal startup hatasıdır.
- A-18 eski PostgreSQL/SQLite fixture, tenant/mali parity, yarım migration/rollback kabuline bağlı olarak **🟡** kalıyor. Web Release derlemesi **0 uyarı / 0 hata**; otomatik test çalıştırılmadı. Güncel toplam **30 yeşil / 1 sarı**.
- Ek veri kaybı bulgusu: `AracSasePlakaYapisi` migration'ı legacy `Araclar.Plaka` değerlerini taşımadan siliyordu; initializer kopyalama adımı migration sonrasında olduğundan geç kalıyordu. Migration artık `SaseNo` değerini indeks öncesi tamamlıyor, `AktifPlaka` ve `AracPlakalar` geçmişini dolduruyor, sonra eski kolonu kaldırıyor. PostgreSQL uyumluluk yolu elle migration ID eklemeyi bıraktı.

### 2026-10-10 — A-18 migration sonrası parity kapısı

- Migration sonrası PostgreSQL eksik kolonlarını elle ekleyen genel initializer yolu devreden çıkarıldı. PostgreSQL ve SQLite için model tablo/kolon/indeks/FK imza denetimi eklendi; eski `Araclar.Plaka` kolonu kalmışsa ya da beklenen şema öğesi eksikse startup fail-fast olur.
- Release derlemesi **0 uyarı / 0 hata**; otomatik test ve müşteri DB kabulü çalıştırılmadı. Eski DB migration/veri parity/rollback fixture'ı olmadığından A-18 **🟡** kalır. Genel toplam **30 yeşil / 1 sarı**.

### 2026-10-10 — A-18 historysiz SQLite veri migration koruması

- Migration taraması, watermark öncesinde tenant FirmaId backfill, organizasyon seed, hakediş duplicate pasifleştirme ve plaka/şase veri taşıma adımlarını ortaya çıkardı. Hedef şema kontrolü bunların çalıştığını kanıtlamaz.
- Kod, mevcut tablolu DBde history tablosu yoksa veya watermark öncesi kayıt eksikse otomatik migration geçmişi yazmadan fail-fast olur. Web Release: 0 uyarı / 0 hata. Test çalıştırılmadı.
- Eski DByi açmak için onaylı yedek, migration provenance ve veri paritysine dayanan kontrollü history onarım/baseline prosedürü henüz yok; bu yüzden A-18 kırmızı. Müşteri fixture ve rollback kabulü de açık.
- Tenant FirmaId açılış backfill'i tek transaction'a alındı; eski tablo/kolon hatalarını yutup kısmi tenant onarımıyla açılma yolu kaldırıldı. Release build 0 uyarı / 0 hata.### 2026-10-10 — SQLite migration zinciri ek bulgusu

Static migration taramasında SQLite için sağlayıcı dalı bulunmayan PL/pgSQL DO blokları saptandı. Örnekler: 20260326204037_CRMModulu, 20260409091451_AddBudgetHedef, 20260513140012_FixCariFirmaShadowFK, 20260517212717_TenantZ1_DropLegacyCariFaturaSirketColumns, 20260518140619_TenantB3i_DropSirketNavigationAndEntity, 20260518195552_TenantB4a_DropSirketIdColumnsAndRenameAuditLog, 20260518200342_TenantB4b_DropLegacyTables, 20260615192539_AddPersonelBankaOdemeAlanlari ve 20260616074934_AddHakedisPuantajFaturaFKs. Bunlar migration geçmişi eksik SQLite'ta körlemesine zinciri çalıştırmanın güvenli olmadığını; önce SQLite-native geçiş/adoption yolu gerektiğini gösterir.

Migration history'siz legacy SQLite şeması için bu PL/pgSQL migration'larını doğrudan çalıştırmak mümkün değildir; tarihsel veri dönüşümleri korunarak SQLite-native yol yazılmadan görev kapanamaz. A-18 kırmızı kalır.

### 2026-10-10 — A-18 legacy SQLite adoption

History'siz SQLite için watermark şeması uyumluysa migration geçmişi yazılmadan önce tek transaction'da araç plakası/şase taşıması, organizasyon seed'i, tenant FirmaId backfill'i ve enum metin normalizasyonu çalışır. Su geçirmez foreign key kontrolü sonrası yalnızca watermark'a kadar migration ID'leri eklenir. Eksik/uyumsuz hedef şema, kısmi migration geçmişi, pending DDL çakışması, tanınmayan enum veya FK ihlalinde işlem rollback olur. Release build 0 uyarı / 0 hata; fixture test edilmedi. A-18 sarı.

### 2026-10-10 — A-18 startup FK düzeltmesi ve son şema kapısı

- PostgreSQL startup'ındaki `GuzergahSeferFirmaIdConstraintHelper` `Guzergahlar.FirmaId` FK'sini modelin beklediği halde kaldırıyordu. Çağrıları ve helper silindi. Startup'taki diğer migration yardımcılarından sonra, HTTP pipeline açılmadan önce güncel EF model parity kontrolü zorunlu çalışıyor.
- Eski `SirketSchemaFixMigrationHelper` kaldırıldı; legacy `Sirketler/SirketId` şemasını geri ekleme ve DDL hatalarını yutma yolu kapandı. SQLite FK eşdeğerliği için insert/update/delete trigger'larının üçü de aranıyor. `AddSnapshotHakedisFieldsV2` SQLite dalı EF migration operasyonlarını kullanıyor.
- Web Release derlemesi **0 uyarı / 0 hata**, `git diff --check` temiz. Test ve eski müşteri DB migration/restore kabulü çalıştırılmadı. Eski PostgreSQL/SQLite fixture, tenant/mali parity ve rollback kanıtı bulunmadığı için A-18 **🔴**; toplam **30 yeşil / 0 sarı / 1 kırmızı / 0 beyaz**.
