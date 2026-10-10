# A-18 — temiz kurulum baseline/parity incelemesi

**Tarih:** 2026-10-09
**Kapsam:** `MKFiloServis.Web/Data/Migrations` kaynaklarının salt okunur statik taraması. Müşteri veritabanı kullanılmadı ve migration çalıştırılmadı.

## Bulgular

- Kaynakta Designer dosyaları, snapshot ve helper'lar hariç **144 migration sınıfı** var. Önceki EF çalışma zamanı raporunda 141 migration listelenmişti; bu sayım sonradan eklenen migration kaynaklarıyla güncel kaynak sayımından farklıdır.
- İlk `20260324175248_Init` migration'ının `Up` ve `Down` metotları boş. Ayrıca üç sonraki migration'ın `Up` metodu da boş.
- 144 `Up` gövdesinin **33'ü ham SQL** içeriyor. Model işlemlerinden ayrı bu SQL adımları yeni kurulum parity'sinde ayrıca ele alınmalı.
- `Up` gövdelerinde **5 tablo düşürme**, **8 kolon düşürme** ve **17 constraint düşürme** adımı görüldü. Bunların bazıları dönüşüm/temizlik adımı; başlangıç baseline'ında eski migration'ları körlemesine çalıştırmak doğru değil.
- `20260604204018_NihaiMimari_OrganizasyonSubeHolding` varsayılan organizasyon satırlarını ekliyor ve mevcut `Firmalar.OrganizasyonId` değerlerini dönüştürüyor. `EnsureCreated` yalnızca son model şemasını kurar; bu veri işlemini kendiliğinden yerine getirmez.
- `InsertData/UpdateData/DeleteData` MigrationBuilder işlemi bulunmadı; veri etkileri ham SQL ve uygulama başlangıç seed/helper'ları içinde de aranmalı.

## Bulunan ve düzeltilen başlangıç sırası hatası

`Program.cs` migration'dan önce `LegacyDataTransferService.EnsureSchemaAsync()` çağırıyordu; bu çağrı `LegacyTransfer:Enabled=false` iken bile çalışıyor, güncel modelden DDL üretip `EnsureCreatedAsync()` ile hedef PostgreSQL'i önceden oluşturuyordu. Böylece gerçekten boş DB, eski migration zincirine girmeden kısmen/ tamamen güncel şemaya dönüşebiliyor ve migration geçmişi olmadan `MigrateAsync()` aşamasına ulaşıyordu. Bu, boş kurulum ile migration akışının birbirini atlatmasına yol açan bir kök sıra hatasıydı.

2026-10-09'da bu yan yol kaldırıldı: LegacyDataTransfer artık hedef şema üretmiyor; ana `DbInitializer` migration/seed adımından sonra çalışıyor. İlgili kullanılmayan `GenerateCreateScript`/`EnsureCreated` yardımcıları silindi. Web Release derlemesi **0 uyarı / 0 hata** verdi. Bu değişiklik tek başına baseline üretmiyordu; sonraki bölümde eklenen boş kurulum yolu ayrı olarak ele alındı.

## Boş kurulum baseline düzeltmesi ve kanıtı

Migration sırası hatasından sonra boş kurulum için ayrı bir yol eklendi. Başlangıç yalnızca uygulama tablosu bulunmayan ve migration geçmişi boş olan veritabanını yeni kurulum kabul ediyor; güncel EF modeliyle tabloları oluşturuyor, geçerli migration ID'lerini history tablosuna yazıyor ve `NihaiMimari_OrganizasyonSubeHolding` adımının boş DB için gereken dört varsayılan organizasyon kaydını ekliyor. Kısmi/mevcut şema eski migration yolunda kalıyor. Program bu kontrolü SQLite audit günlüğü kurulmadan önce çağırıyor; doğrudan initializer çağrısı da boş DB'de aynı yolu çalıştırıyor.

**2026-10-09 izole kabulü:** Boş SQLite `:memory:` fixture'ında tam `DbInitializer.InitializeAsync` başarılı; bekleyen migration kalmadı ve dört organizasyon bulundu. Boş, geçici PostgreSQL 17 cluster'ında aynı tam initializer başarılı; migration beklemedi ve dört organizasyon bulundu. Kısmi SQLite ve PostgreSQL tablo fixture'ları baseline olarak işaretlenmedi, satırlar korundu ve migration history oluşturulmadı. Geçici PostgreSQL cluster'ı durduruldu. Tam test paketi **139 geçti / 2 PostgreSQL-özel test ortam değişkeni olmadığı için atlandı**; PostgreSQL özel boş/kısmi şema testleri izole cluster'da ayrıca **2/2 geçti**. Tam Release çözüm derlemesi **0 uyarı / 0 hata**.

Bu kanıt boş kurulum yolunu kapatır; tarihsel müşteri veritabanında yükseltme, migration yarıda kalması, rollback, timestamp tür farkları ve müşteri verisi parity'sini kapatmaz. Kısmi PostgreSQL müşteri fixture'ı da henüz kullanılmadı. Bu nedenle A-18 genel kabulü hâlâ açık.

## Güvenli kapanış ölçütü

Yeni kurulum için ayrı ve gözden geçirilebilir baseline hazırlanmalı; var olan veritabanları tarihsel migration hattında yükseltilmeye devam etmeli. Baseline, güncel `ApplicationDbContext` şemasını, PostgreSQL ve SQLite sağlayıcı farklarını, gerekli başlangıç verisini ve ham SQL ile yapılan dönüşümlerin yeni kurulum karşılığını açıkça kapsamalı. Mevcut veritabanında migration geçmişini topluca doldurup yükseltme atlatılmamalı.

Baseline ancak şu izole senaryolar geçtikten sonra dağıtıma alınabilir:

1. Boş PostgreSQL'de baseline kurulum, zorunlu indeks/FK/trigger denetimi ve uygulama başlangıcı.
2. Boş SQLite'de aynı modelin sağlayıcıya uygun kurulumu ve uygulama başlangıcı.
3. Gerçek müşteri verisi içermeyen eski şema fixture'ında tarihsel yükseltme; kayıt sayısı, tenant bağları, seed'ler ve mali toplamların önce/sonra karşılaştırması.
4. Yarım migration ve rollback provası; veri kaybı yaratacak adımların yedek gereksinimi açıkça raporlanmalı.
5. İki kurulum yolunun EF model snapshot'ıyla şema parity karşılaştırması.

## Karar ve kalan iş

Boş PostgreSQL/SQLite baseline'ı iki sağlayıcıda çalıştı; A-18'in eski müşteri şeması yükseltme ve geri dönüş kanıtı hâlâ eksik olduğu için görev sarı ve satışa çıkış tarihi belirsiz kalır. Sonraki kapanış adımı, müşteri verisi içermeyen gerçek eski şema fixture'ında kayıt/tenant/mali toplam parity'si ve yarım migration/rollback kabulidir. Bu geçiş için migration geçmişi otomatik doldurulmaz.

Statik tarama sayıları kaynak metni üzerindendir; migration'ların PostgreSQL/SQLite çalışma davranışı veya müşteri uyumluluğu kanıtı değildir.

## 2026-10-10 — legacy SQLite watermark doğrulaması

İnceleme, önceki watermark'ın 2026-10-06 unique-index migration'larına kadar history doldurabildiğini gösterdi. Böylece aynı tarihten sonraki kritik unique index ve firma bütünlüğü migration'ları eski şemada çalıştırılmadan atlanabiliyordu. Watermark son bilinen önceki migration'a (`20260925192810_NormalizeRentACarOdemeEnumColumns`) çekildi. Eski veritabanı geçmişi eklenmeden önce bu migration'ın TargetModel tablo/kolonları SQLite kataloğunda doğrulanır; eksik tablo veya kolon varsa `__EFMigrationsHistory` oluşturulmadan hata verilir. Bu tarihten sonraki migration'lar gerçek DDL olarak uygulanır. Migration hata sonrası recovery yolu da history'ye otomatik kayıt atmaz.

Bu guard bir şema parity ispatı değildir: indeks, trigger, FK ve veri invariant'ları ile gerçek eski DB yükseltme/rollback fixture'ı hâlâ kabul edilmelidir. Kaynak Release derlemesi 0 uyarı/0 hata; test çalıştırılmadı, müşteri veritabanı kullanılmadı.

### 2026-10-10 — A-18 legacy SQLite watermark koruması

- Watermark `20260925192810_NormalizeRentACarOdemeEnumColumns` olarak sınırlandı. Önceki cutoff, 2026-10-06'daki unique index migration'larını mevcutmiş gibi history'ye ekleyip atlayabiliyordu.
- Baseline öncesi hedef migration modeliyle SQLite tablo/kolon kapsamı karşılaştırılır. Uyuşmayan legacy şemada history tablosu oluşturulmadan hata verilir. Watermark sonrasındaki migration'lar normal DDL olarak çalışır; migration başarısızlığından sonra otomatik history düzeltmesi yoktur.
- Bu kontrol indeksi/FK/trigger/veri parity'sini kanıtlamaz. Eski müşteri DB fixture'ı olmadığı için A-18 sarı; Release derleme **0 uyarı / 0 hata**, test çalıştırılmadı.

### 2026-10-10 — PostgreSQL migration history fail-fast

PostgreSQL initializer’dan, `AylikOdemeGerceklesenler` tablosu/FK eksikliği halinde migration ID’sini migration çalıştırmadan history’ye ekleyen ön-kontrol kaldırıldı. Duplicate column/table hatalarından sonra seçilmiş migration’ları tablo/kolon varlığına bakarak uygulanmış sayan recovery kodu da kaldırıldı. `MigrateAsync` hatası artık başlangıç hatası olarak yukarı taşınır; migration geçmişi otomatik değiştirilmez. Bu yaklaşım uyumsuz legacy DB’de sessiz/yanlış başarıyı önler ancak legacy DDL uyumluluğunu sağlamaz. Eski PostgreSQL/SQLite fixture ve rollback/parity kabulü yok; A-18 sarı kalır. Web Release build 0 uyarı/0 hata; otomatik test çalıştırılmadı.
### 2026-10-10 — legacy SQLite watermark parity guard genişletildi

Watermark migration TargetModel ile yalnızca tablo/kolon değil, beklenen relational index adları ve foreign key principal/from/to kolon eşleşmeleri de SQLite kataloğunda doğrulanır. Herhangi bir eksik öğe varsa `__EFMigrationsHistory` oluşturulmadan başlangıç reddedilir. Bu, eksik indeks/FK içeren yarım legacy şemanın watermark ile uyumlu sanılmasını önler. Guard’ın müşteri verisi/parity kabulinin yerini tutmadığı notu geçerlidir: SQLite/PostgreSQL legacy fixture ve rollback kanıtı yok; A-18 sarı. Web Release **0 uyarı / 0 hata**, otomatik test çalıştırılmadı.
### 2026-10-10 — timestamp şema uyarlaması atomik yapıldı

PostgreSQL’de `timestamp with time zone` audit kolonlarını uygulamanın UTC `timestamp without time zone` sözleşmesine uyarlayan açılış işlemi artık tüm kolonları tek transaction’da değiştirir. Varsayılanı düşürme, tip dönüşümü ve default’u geri koyma adımlarından biri hata verirse transaction commit edilmez; şema kısmi halde bırakılmaz. SQLite legacy watermark kontrolü tablo/kolon, indeks ve FK eşleşmesini de kapsar. Fixture olmadığı için bu statik korumalar eski DB parity/rollback kabulinin yerine geçmez; A-18 sarı kalır.

### 2026-10-10 — migration öncesi genel şema eşitlemesi kaldırıldı

Program başlangıcındaki `SchemaSyncHelper.EnsureAllColumnsExistAsync` çağrısı kaldırıldı. Bu helper EF modelinden kolon DDL'i çıkarıp eski şemaya migration'lardan önce uygulayabiliyordu; migration'ların kendi DDL'iyle çakışma ve gerçek geçiş sırasını bozma riski vardı. Kullanılmayan, pending migration ID'lerini uygulanmış yazan helper da kaldırıldı. `FisNoCounters` legacy uyumluluğu artık `DbInitializer` migration'larından sonra çalışır ve hata halinde startup başarısız olur.

PostgreSQL initializer'ında migration sonrası modelde eksik kolonları tek tek oluşturan eski otomatik DDL yolu da artık çalıştırılmıyor. Migration tamamlandıktan sonra iki sağlayıcıda modelin tablo/kolon/indeks/FK imzaları gerçek katalogla karşılaştırılır; eksik yapı veya kalmış `Araclar.Plaka` legacy kolonu varsa startup durur, kendiliğinden şema yamalanmaz.

Araç `Plaka`→`AktifPlaka`/`AracPlakalar` migration'ında da sıralama/veri kaybı açığı bulundu: `Araclar.Plaka` migration başında siliniyor, initializer'daki kopyalama denemesi ise migration'lardan sonra koşuyordu. Migration artık boş `SaseNo` değerini unique index öncesi eski plakadan doldurur; sonra eski plakayı `AktifPlaka`'ya ve plaka geçmişine aktarır, en son legacy kolonu kaldırır (PostgreSQL ve SQLite SQL yolları). PostgreSQL post-migration uyumluluk yolu aynı dönüşümü yalnız eski kolon gerçekten mevcutsa yapar; migration history tablosunu/ID'sini oluşturmaz veya değiştirmez. Migration'ı daha önce uygulayıp `Plaka` kolonunu kaybetmiş kurulumlarda bu güncel migration geriye dönük çalışmaz; kayıp değerler için eski yedek gerekir.

Web Release derlemesi **0 uyarı / 0 hata** verdi; otomatik test çalıştırılmadı. Eski müşteri PostgreSQL/SQLite şema fixture'ı, kayıt/tenant/mali parity ve yarım migration/rollback kanıtı hâlâ yok. A-18 sarı; bu değişiklik migration sırası/veri taşıma kusurlarını giderir fakat genel eski şema kabulini kapatmaz.

### 2026-10-10 — SQLite watermark veri migration provenance guard

Statik migration incelemesi, watermark öncesi veri değiştiren adımlar bulunduğunu doğruladı: FirmaId backfillleri, organizasyon seedleri, HakedisPuantajlar duplicate pasifleştirmesi ve araç plaka/şase veri taşıması. Şema hedef modelinin tablo/kolon/index/FK paritysi bu DML adımlarının çalıştığının kanıtı değildir. EnsureSqliteMigrationHistoryAsync, mevcut uygulama tabloları için history tablosu yoksa veya watermarka kadar migration ID zinciri eksikse otomatik history yazımını keser. Web Release build 0 uyarı / 0 hata; test çalıştırılmadı. Bu fail-fast sessiz veri kaybı riskini giderir ancak historysiz eski SQLite kurulumlarını bloke eder. Yedek/log ile doğrulanmış migration provenance ve parity tabanlı kontrollü geçiş aracı yok; A-18 kırmızı, satış Go/No-Go kapalı.
Tenant FirmaId startup backfill'i de atomik hale getirildi: tüm tablo güncellemeleri tek transaction'da; herhangi bir hata startup'a yükselir ve transaction rollback olur. Bu, mevcut veriyi kısmen firmalara atayıp uygulamanın açılmasını engeller; legacy SQLite migration geçmişi/provenance ihtiyacını ortadan kaldırmaz.### 2026-10-10 — SQLite legacy migration zinciri uyumluluk bulgusu

Static migration taramasında SQLite için sağlayıcı dalı bulunmayan PL/pgSQL DO blokları saptandı. Örnekler: 20260326204037_CRMModulu, 20260409091451_AddBudgetHedef, 20260513140012_FixCariFirmaShadowFK, 20260517212717_TenantZ1_DropLegacyCariFaturaSirketColumns, 20260518140619_TenantB3i_DropSirketNavigationAndEntity, 20260518195552_TenantB4a_DropSirketIdColumnsAndRenameAuditLog, 20260518200342_TenantB4b_DropLegacyTables, 20260615192539_AddPersonelBankaOdemeAlanlari ve 20260616074934_AddHakedisPuantajFaturaFKs. Bunlar migration geçmişi eksik SQLite'ta körlemesine zinciri çalıştırmanın güvenli olmadığını; önce SQLite-native geçiş/adoption yolu gerektiğini gösterir.

SQLite geçmişi eksik veritabanında EF zincirini doğrudan çalıştırmak bu bloklarda başarısız olur. History recovery aracı tek başına yetmez: migration'ların DDL/DML semantiğini SQLite'a taşıyıp tenant bağları ve finans kayıtlarıyla legacy fixture üzerinde doğrulamak gerekir. Böyle bir adoption yolu henüz yok; A-18 kırmızı.

### 2026-10-10 — SQLite history'siz legacy adoption

History tablosu olmayan mevcut SQLite DB için adoption kod yolu kaldırıldı ve config bayrağıyla aşılamaz. Watermark öncesi boş migration geçmişi de otomatik tamamlanmış sayılmaz. Neden: şema parity'si veri dönüşüm migration'larının (tenant/organizasyon backfill, duplicate düzeltme, araç verisi aktarımı dahil) gerçekten çalıştığını ispatlamaz; önceki sınırlı reconciliation tüm tarihsel DML adımlarını karşılamıyordu. Yalnız boş veritabanı fresh baseline'ı desteklenir. Legacy SQLite için her veri dönüşümünü SQLite-native uygulayan, yedek ve transaction/rollback adımları tanımlı operator migration aracı; ardından müşteri benzeri fixture'da kayıt/tenant/mali parity kanıtı gerekir. PostgreSQL eski DB geçişi de fixture ile doğrulanmadı. A-18 sarı kalır.

### 2026-10-10 — A-18 eski watermark şema kıyası kaldırıldı

History zinciri doğrulanmış mevcut SQLite DB, güncel `__EFMigrationsHistory` kayıtları üzerinden ilerler. Önceki kod ayrıca DB'yi 2026-09-25 watermark migration'ının eski `TargetModel`'iyle kıyaslıyordu; daha sonra uygulanmış migration'ların meşru biçimde kaldırdığı/yenilediği kolon ve indeksleri “eksik” sayıp upgrade'i engelleyebilirdi. Watermark `TargetModel` kıyası kaldırıldı. Artık history yok/boş, watermark öncesi history boşluğu veya tanınmayan history ID varsa işlem fail-fast durur; geçerli zincirde pending migration'lar EF ile çalışır ve tamamlandığında güncel model parity kontrol edilir. Web Release derlemesi 0 uyarı / 0 hata; otomatik test ve müşteri DB çalıştırılmadı. Legacy historyless migration/parity/rollback kabulü açık, A-18 sarı.
