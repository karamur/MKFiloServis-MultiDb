# A-08 — doğrudan SQL ve toplu yazım kapanış envanteri

**Tarih:** 2026-10-05. Kaynak taramasıdır; runtime/DB testi değildir.

**Sonuç: 🟢 A-08 kaynak/altyapı ve izole kontrol kapanışı tamamlandı.** Ortak DB tetikleyicileri doğrudan SQL, EF, binary COPY, migration ve firmasız sistem yazımlarını denetler; restore kalıcı operasyon makbuzu taşır. [Sözleşme](A-08-VERITABANI-AUDIT-SOZLESMESI.md) ve [izole kanıt](A-08-IZOLE-DOGRULAMA-2026-10-05.md) mevcut. Müşteri/üretim kabulü ayrı A-04/A-09/A-18/A-19 görevleridir.

## Tamamlanan kaynak düzeltmeleri

| Alan | Durum | İşlem/audit sınırı |
|---|---|---|
| Bütçe | 🟢 | Kalan 5 toplu güncelleme tracked kayıt ve SaveChanges üzerinden; önceki geri alma düzeltmesi tek ortak kayıttır. Banka/mahsup/kredi kartı alt servislerinin ayrı context sınırları için uçtan uca mali kabul A-09'da açıktır. |
| Hakediş fatura bağlantısı | 🟢 | Tracked SaveChanges; fatura üretimi ayrı servis/context'tedir, tüm finans zincirinin atomik olduğu iddia edilmez. |
| Hakediş snapshot artışı | 🟢 | SnapshotTransaction ve tutarlar Serializable üst transaction, ExecutionStrategy'de yeni context ve ortak SaveChanges/audit. Negatif tutar normalizasyonu aynı kapsamda. |
| Rebuild | 🟢/🟡 | 6 toplu güncelleme tracked SaveChanges; mevcut üst transaction korunur. Alt finans servislerinin dış context yazımı, lock ve retry kabulü açık. |
| Lisans | 🟢/🟡 | 3 pasife alma tracked SaveChanges; aktivasyonun mevcut transaction'ı korunur. EF firma audit’i üretmese de firmasız sistem yazımı DB günlüğünde denetlenir. |
| Evrak arşiv backfill | 🟢/🟡 | 2 dosya yolu yazımı tracked SaveChanges, satır başına işlem. Dosya/DB telafi ve commit belirsizliği A-10/A-11'de açık. |
| CRM / WhatsApp / eski test rollback | 🟢/🟡 | 1 / 1 / 4 toplu güncelleme tracked SaveChanges. Mevcut kısmi işlem ve sistem kayıtlarının firma/audit kapsamı ayrıca kabul gerektirir. |
| Maaş snapshot / güzergâh ana kayıt-sefer | 🟢 | Önceki eklerdeki hedeflenen SQL bypass'ları kaldırıldı; ortak transaction kayıtları son durum raporundadır. |

TrackedWriteExtensions normal EF sorgusu + AsTracking + SaveChanges kullanır; audit bypass eden bir SQL wrapper değildir. Güncellenen kayıtların işlem öncesi değerleri hata halinde geri yüklenir; yakalanmış satır hatasının sonraki yazımda tekrar kaydedilmesi engellenir. Bu yardımcı metot context'teki diğer bekleyen değişiklikleri de SaveChanges kapsamında kaydeder. Büyük küme bellek/yük kabulü yapılmadı; toplu eşzamanlı increment için atomik SQL yerine kullanılmamalıdır.

## Kapsam ve denetim kararları

| Grup | Durum | Gerekli kapanış |
|---|---|---|
| Destek kullanım/görüntülenme/yararlı sayaçları | 🟢 karar / 🟡 kabul | Atomik SQL increment korunur; mali/tenant iş değişikliği değildir, her görüntülenme için mali audit satırı üretilmez. Eşzamanlı sayaç kabulü bekler. |
| ApplicationDbContext audit EntityId düzeltmesi | 🟢 karar / 🟡 kabul | Audit kendi kendini audit etmez; üretilen audit kimliği mevcut iş/audit transaction/savepoint içinde düzeltilir. Gerçek DB kabulü A-09. |
| Gelen fatura başlangıç onarımı | 🟢 kod / 🟡 kabul | Doğrudan SQL kaldırıldı; AsTracking + SaveChanges/audit. Firma aidiyeti eksik satırlar yazımdan önce reddedilir. Diğer başlangıç onarımları halen açıktır. |
| Demo veri / sınırlı firma temizliği | 🟢 kod / 🟡 kabul | Ortak transaction/operasyon audit, Admin/tek firma ve değişim sürümü kontrolü. Tüm-veritabanı TRUNCATE kaldırıldı; yenileme [TEST] kapsamındadır. FK kontrolleri açık; 11 tablo temizliğinde ortak muhasebe tabloları silinmez. Gerçek DB kabulü bekler. |
| Diğer başlangıç/migration/seed veri UPDATE/INSERT | 🟢 | Ortak DB günlüğü; startup installer, 11 veri yazan PostgreSQL migration sınıfında SQL öncesi bootstrap ve yeni SMS/özlük/maaş onarım kapıları. Migration geçmişi veri denetiminden ayrıdır. Yeni tablo ilk yazımı izole testte geçti. |
| Legacy tablo aktarımı | 🟢 kod / 🟡 kabul | 5 aktarım yolunda tablo başına transaction + aynı transaction’da operasyon audit’i; satır savepoint’leri ve gerçek affected sayısı. Ortak tüm-aktarım rollback’i yok; sequence etkileri ve PostgreSQL kabulü açık. |
| Restore / DataSync | 🟢 | PostgreSQL audit şeması korunur; SQLite eski veri/audit bağımsız geri dönüş kopyasında korunur. Dış SHA-256 başlangıç/başarı/belirsiz sonuç makbuzu. DataSync row/COPY audit hedef transaction içindedir; izole binary COPY ve gerçek dump/restore geçti. |
| Test session tablo snapshot restore | 🟢 kod / 🟡 kabul | GeriAlAsync TRUNCATE/INSERT + operasyon audit’i ortak transaction; eksik backup reddi, RESTRICT ve başarısızlıkta oturum koruması eklendi. Tam FK/kolon/sequence ve gerçek DB kabulü açık. |
| Test session backup / cleanup / BeginSession | 🟢 kod / 🟡 kabul | Admin/firma kontrolü, ortak transaction/operasyon audit’i, eski yedeği koruma, aktif oturumda cleanup reddi, kaynak SHARE/advisory kilitleri ve session marker eklendi. Çok süreçli oturum sahipliği, eski rollback ve gerçek DB kabulü açık. |
| Dış konsol araçları / SQLite geçişi | 🟢 | PostgreSQL/SQLite DB tetikleyicileri; 4 bağımsız veri SQL betiği bootstrap/ilk yazım kapısı; Deploy restore makbuzu; LisansDesktop yerel SQL geçmişi ortak SQLite audit motoru. Audit kendi günlüğünü denetlemez, migration geçmişi ve kimlik sequence metadatası iş satırı değildir. |

## Kalan C# çağrı konumları

Aşağıdaki liste repo C# dosyalarındaki ExecuteUpdate/Delete, ExecuteSql, ExecuteNonQuery ve BulkUpdate/Delete çağrılarını tarar. Üretilmiş bin/obj, .git ve .kilo dışarıda tutulur. Satırların tamamı iş yazımı değildir: DDL, audit iç düzeltmesi, test ve wrapper tanımları da listelenir. Dinamik SQL de kurulu DB tetikleyicilerine tabidir. İş verisi yazan bağımsız .sql betikleri bootstrap/ilk yazım kapısıyla, restore .ps1 betiği ise operasyon makbuzuyla kapsanmıştır. Liste tek başına kanıt değildir; kapanış kanıtı ortak motor, sözleşme ve izole kontrollerdir.

| Dosya | Satır |
|---|---|
| [MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs](../MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs#L85) | 85 |
| [MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs](../MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs#L102) | 102 |
| [MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs](../MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs#L130) | 130 |
| [MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs](../MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs#L203) | 203 |
| [MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs](../MKFiloServis.DataSync/Exporters/PostgresToSqliteExporter.cs#L287) | 287 |
| [MKFiloServis.DataSync/Exporters/SqliteToPostgresImporter.cs](../MKFiloServis.DataSync/Exporters/SqliteToPostgresImporter.cs#L93) | 93 |
| [MKFiloServis.DataSync/Exporters/SqliteToPostgresImporter.cs](../MKFiloServis.DataSync/Exporters/SqliteToPostgresImporter.cs#L101) | 101 |
| [MKFiloServis.DataSync/Exporters/SqliteToPostgresImporter.cs](../MKFiloServis.DataSync/Exporters/SqliteToPostgresImporter.cs#L117) | 117 |
| [MKFiloServis.DataSync/Exporters/SqliteToPostgresImporter.cs](../MKFiloServis.DataSync/Exporters/SqliteToPostgresImporter.cs#L424) | 424 |
| [MKFiloServis.LisansDesktop/MainForm.cs](../MKFiloServis.LisansDesktop/MainForm.cs#L473) | 473 |
| [MKFiloServis.LisansDesktop/MainForm.cs](../MKFiloServis.LisansDesktop/MainForm.cs#L489) | 489 |
| [MKFiloServis.LisansDesktop/MainForm.cs](../MKFiloServis.LisansDesktop/MainForm.cs#L508) | 508 |
| [MKFiloServis.LisansDesktop/MainForm.cs](../MKFiloServis.LisansDesktop/MainForm.cs#L1730) | 1730 |
| [MKFiloServis.LisansDesktop/MainForm.cs](../MKFiloServis.LisansDesktop/MainForm.cs#L1780) | 1780 |
| [MKFiloServis.LisansDesktop/MainForm.cs](../MKFiloServis.LisansDesktop/MainForm.cs#L1788) | 1788 |
| [MKFiloServis.Shared/Auditing/DatabaseWriteAudit.cs](../MKFiloServis.Shared/Auditing/DatabaseWriteAudit.cs#L105) | 105 |
| [MKFiloServis.Web/Components/Pages/Admin/ErrorLogs.razor.cs](../MKFiloServis.Web/Components/Pages/Admin/ErrorLogs.razor.cs#L74) | 74 |
| [MKFiloServis.Web/Data/ApplicationDbContext.cs](../MKFiloServis.Web/Data/ApplicationDbContext.cs#L3412) | 3412 |
| [MKFiloServis.Web/Data/ApplicationDbContext.cs](../MKFiloServis.Web/Data/ApplicationDbContext.cs#L3429) | 3429 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L54) | 54 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L186) | 186 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L252) | 252 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L545) | 545 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L652) | 652 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L661) | 661 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L671) | 671 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L716) | 716 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L720) | 720 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L767) | 767 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L778) | 778 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L844) | 844 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L855) | 855 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L905) | 905 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L975) | 975 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L1037) | 1037 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L1070) | 1070 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L1138) | 1138 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L1177) | 1177 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L1217) | 1217 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L1462) | 1462 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L1467) | 1467 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L1985) | 1985 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2017) | 2017 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2032) | 2032 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2112) | 2112 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2138) | 2138 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2151) | 2151 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2177) | 2177 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2194) | 2194 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2207) | 2207 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2218) | 2218 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2231) | 2231 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2246) | 2246 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2259) | 2259 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2287) | 2287 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2307) | 2307 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2351) | 2351 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2404) | 2404 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2419) | 2419 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L2449) | 2449 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3178) | 3178 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3196) | 3196 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3212) | 3212 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3245) | 3245 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3252) | 3252 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3261) | 3261 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3275) | 3275 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3305) | 3305 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3323) | 3323 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3401) | 3401 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3414) | 3414 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3417) | 3417 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3420) | 3420 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3479) | 3479 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3537) | 3537 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3585) | 3585 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3623) | 3623 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3647) | 3647 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3681) | 3681 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3723) | 3723 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3761) | 3761 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3793) | 3793 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3820) | 3820 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3852) | 3852 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3897) | 3897 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3925) | 3925 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3962) | 3962 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L3997) | 3997 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L4024) | 4024 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L4056) | 4056 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L4088) | 4088 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L4116) | 4116 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L4148) | 4148 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L4180) | 4180 |
| [MKFiloServis.Web/Data/DbInitializer.cs](../MKFiloServis.Web/Data/DbInitializer.cs#L4209) | 4209 |
| [MKFiloServis.Web/Data/DemoDataService.cs](../MKFiloServis.Web/Data/DemoDataService.cs#L106) | 106 |
| [MKFiloServis.Web/Data/DemoDataService.cs](../MKFiloServis.Web/Data/DemoDataService.cs#L155) | 155 |
| [MKFiloServis.Web/Data/Migrations/AracMasrafMuhasebeMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/AracMasrafMuhasebeMigrationHelper.cs#L30) | 30 |
| [MKFiloServis.Web/Data/Migrations/AracMasrafMuhasebeMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/AracMasrafMuhasebeMigrationHelper.cs#L75) | 75 |
| [MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs#L58) | 58 |
| [MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs#L68) | 68 |
| [MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs#L112) | 112 |
| [MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs#L125) | 125 |
| [MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs#L170) | 170 |
| [MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BankaHareketPersonelCebindenMigrationHelper.cs#L183) | 183 |
| [MKFiloServis.Web/Data/Migrations/BordroMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BordroMigrationHelper.cs#L100) | 100 |
| [MKFiloServis.Web/Data/Migrations/BordroMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BordroMigrationHelper.cs#L218) | 218 |
| [MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs#L40) | 40 |
| [MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs#L46) | 46 |
| [MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs#L61) | 61 |
| [MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs#L62) | 62 |
| [MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetHedefMigrationHelper.cs#L63) | 63 |
| [MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs#L49) | 49 |
| [MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs#L75) | 75 |
| [MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs#L82) | 82 |
| [MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs#L89) | 89 |
| [MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs#L96) | 96 |
| [MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/BudgetOdemeKalanMigrationHelper.cs#L103) | 103 |
| [MKFiloServis.Web/Data/Migrations/CariMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/CariMigrationHelper.cs#L37) | 37 |
| [MKFiloServis.Web/Data/Migrations/CariMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/CariMigrationHelper.cs#L80) | 80 |
| [MKFiloServis.Web/Data/Migrations/FaturaGibDurumMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/FaturaGibDurumMigrationHelper.cs#L70) | 70 |
| [MKFiloServis.Web/Data/Migrations/FaturaGibDurumMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/FaturaGibDurumMigrationHelper.cs#L134) | 134 |
| [MKFiloServis.Web/Data/Migrations/GuzergahKdvOraniMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/GuzergahKdvOraniMigrationHelper.cs#L30) | 30 |
| [MKFiloServis.Web/Data/Migrations/GuzergahKdvOraniMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/GuzergahKdvOraniMigrationHelper.cs#L46) | 46 |
| [MKFiloServis.Web/Data/Migrations/GuzergahKoordinatMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/GuzergahKoordinatMigrationHelper.cs#L51) | 51 |
| [MKFiloServis.Web/Data/Migrations/GuzergahKoordinatMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/GuzergahKoordinatMigrationHelper.cs#L102) | 102 |
| [MKFiloServis.Web/Data/Migrations/GuzergahKoordinatMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/GuzergahKoordinatMigrationHelper.cs#L115) | 115 |
| [MKFiloServis.Web/Data/Migrations/GuzergahSeferFirmaIdConstraintHelper.cs](../MKFiloServis.Web/Data/Migrations/GuzergahSeferFirmaIdConstraintHelper.cs#L43) | 43 |
| [MKFiloServis.Web/Data/Migrations/KiralikPlakaFaturaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/KiralikPlakaFaturaMigrationHelper.cs#L35) | 35 |
| [MKFiloServis.Web/Data/Migrations/KiralikPlakaTakipFaturaPlanMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/KiralikPlakaTakipFaturaPlanMigrationHelper.cs#L26) | 26 |
| [MKFiloServis.Web/Data/Migrations/KiralikPlakaTakipFaturaPlanMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/KiralikPlakaTakipFaturaPlanMigrationHelper.cs#L45) | 45 |
| [MKFiloServis.Web/Data/Migrations/LastikSezonAyarMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/LastikSezonAyarMigrationHelper.cs#L35) | 35 |
| [MKFiloServis.Web/Data/Migrations/LastikSezonAyarMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/LastikSezonAyarMigrationHelper.cs#L60) | 60 |
| [MKFiloServis.Web/Data/Migrations/MuhasebeAyarMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/MuhasebeAyarMigrationHelper.cs#L32) | 32 |
| [MKFiloServis.Web/Data/Migrations/MuhasebeAyarMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/MuhasebeAyarMigrationHelper.cs#L77) | 77 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L46) | 46 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L56) | 56 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L57) | 57 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L58) | 58 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L78) | 78 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L215) | 215 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L216) | 216 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L219) | 219 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L234) | 234 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L275) | 275 |
| [MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/OzlukEvrakMigrationHelper.cs#L279) | 279 |
| [MKFiloServis.Web/Data/Migrations/PersonelBelgeTarihleriMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelBelgeTarihleriMigrationHelper.cs#L47) | 47 |
| [MKFiloServis.Web/Data/Migrations/PersonelFinansMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelFinansMigrationHelper.cs#L123) | 123 |
| [MKFiloServis.Web/Data/Migrations/PersonelFinansMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelFinansMigrationHelper.cs#L275) | 275 |
| [MKFiloServis.Web/Data/Migrations/PersonelFinansMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelFinansMigrationHelper.cs#L401) | 401 |
| [MKFiloServis.Web/Data/Migrations/PersonelMaasHesaplamaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelMaasHesaplamaMigrationHelper.cs#L29) | 29 |
| [MKFiloServis.Web/Data/Migrations/PersonelMaasHesaplamaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelMaasHesaplamaMigrationHelper.cs#L73) | 73 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajOnayMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajOnayMigrationHelper.cs#L36) | 36 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajOnayMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajOnayMigrationHelper.cs#L82) | 82 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L83) | 83 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L104) | 104 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L136) | 136 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L160) | 160 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L205) | 205 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L212) | 212 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L213) | 213 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L214) | 214 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L237) | 237 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L245) | 245 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L246) | 246 |
| [MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelPuantajTableMigrationHelper.cs#L277) | 277 |
| [MKFiloServis.Web/Data/Migrations/PersonelTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelTableMigrationHelper.cs#L24) | 24 |
| [MKFiloServis.Web/Data/Migrations/PersonelTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelTableMigrationHelper.cs#L48) | 48 |
| [MKFiloServis.Web/Data/Migrations/PersonelTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelTableMigrationHelper.cs#L56) | 56 |
| [MKFiloServis.Web/Data/Migrations/PersonelTableMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PersonelTableMigrationHelper.cs#L208) | 208 |
| [MKFiloServis.Web/Data/Migrations/PuantajCarpaniMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajCarpaniMigrationHelper.cs#L30) | 30 |
| [MKFiloServis.Web/Data/Migrations/PuantajCarpaniMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajCarpaniMigrationHelper.cs#L46) | 46 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L19) | 19 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L26) | 26 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L33) | 33 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L40) | 40 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L47) | 47 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L54) | 54 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L61) | 61 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L68) | 68 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L80) | 80 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L87) | 87 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L95) | 95 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L129) | 129 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L198) | 198 |
| [MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSlotMigrationHelper.cs#L238) | 238 |
| [MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs#L30) | 30 |
| [MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs#L38) | 38 |
| [MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs#L46) | 46 |
| [MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs#L54) | 54 |
| [MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/PuantajSyncMigrationHelper.cs#L98) | 98 |
| [MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs](../MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs#L124) | 124 |
| [MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs](../MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs#L162) | 162 |
| [MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs](../MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs#L186) | 186 |
| [MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs](../MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs#L192) | 192 |
| [MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs](../MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs#L198) | 198 |
| [MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs](../MKFiloServis.Web/Data/Migrations/SchemaSyncHelper.cs#L357) | 357 |
| [MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs#L85) | 85 |
| [MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs#L178) | 178 |
| [MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs#L562) | 562 |
| [MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs#L583) | 583 |
| [MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs#L595) | 595 |
| [MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs#L648) | 648 |
| [MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs#L672) | 672 |
| [MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SirketSchemaFixMigrationHelper.cs#L696) | 696 |
| [MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs#L75) | 75 |
| [MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs#L135) | 135 |
| [MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs#L183) | 183 |
| [MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs#L217) | 217 |
| [MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs#L236) | 236 |
| [MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SmsMigrationHelper.cs#L241) | 241 |
| [MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs#L53) | 53 |
| [MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs#L55) | 55 |
| [MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs#L58) | 58 |
| [MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs#L71) | 71 |
| [MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SoforMaasMigrationHelper.cs#L158) | 158 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L30) | 30 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L68) | 68 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L92) | 92 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L111) | 111 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L129) | 129 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L146) | 146 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L171) | 171 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L183) | 183 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L199) | 199 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L225) | 225 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L251) | 251 |
| [MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/SyncPuantajSchemaMigrationHelper.cs#L265) | 265 |
| [MKFiloServis.Web/Data/Migrations/TenantFirmaIdBackfillMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/TenantFirmaIdBackfillMigrationHelper.cs#L100) | 100 |
| [MKFiloServis.Web/Data/Migrations/TenantFirmaIdBackfillMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/TenantFirmaIdBackfillMigrationHelper.cs#L111) | 111 |
| [MKFiloServis.Web/Data/Migrations/TenantFirmaIdBackfillMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/TenantFirmaIdBackfillMigrationHelper.cs#L117) | 117 |
| [MKFiloServis.Web/Data/Migrations/TwoFactorMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/TwoFactorMigrationHelper.cs#L33) | 33 |
| [MKFiloServis.Web/Data/Migrations/TwoFactorMigrationHelper.cs](../MKFiloServis.Web/Data/Migrations/TwoFactorMigrationHelper.cs#L78) | 78 |
| [MKFiloServis.Web/Program.cs](../MKFiloServis.Web/Program.cs#L1072) | 1072 |
| [MKFiloServis.Web/Program.cs](../MKFiloServis.Web/Program.cs#L1081) | 1081 |
| [MKFiloServis.Web/Program.cs](../MKFiloServis.Web/Program.cs#L1261) | 1261 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L346) | 346 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L1029) | 1029 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L1546) | 1546 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L1577) | 1577 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L1584) | 1584 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L1588) | 1588 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L1725) | 1725 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2077) | 2077 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2078) | 2078 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2079) | 2079 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2080) | 2080 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2081) | 2081 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2082) | 2082 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2083) | 2083 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2084) | 2084 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2085) | 2085 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2086) | 2086 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2087) | 2087 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2088) | 2088 |
| [MKFiloServis.Web/Services/BackupService.cs](../MKFiloServis.Web/Services/BackupService.cs#L2089) | 2089 |
| [MKFiloServis.Web/Services/DestekTalebiService.cs](../MKFiloServis.Web/Services/DestekTalebiService.cs#L1003) | 1003 |
| [MKFiloServis.Web/Services/DestekTalebiService.cs](../MKFiloServis.Web/Services/DestekTalebiService.cs#L1144) | 1144 |
| [MKFiloServis.Web/Services/DestekTalebiService.cs](../MKFiloServis.Web/Services/DestekTalebiService.cs#L1155) | 1155 |
| [MKFiloServis.Web/Services/DestekTalebiService.cs](../MKFiloServis.Web/Services/DestekTalebiService.cs#L1161) | 1161 |
| [MKFiloServis.Web/Services/LegacyDataTransferService.cs](../MKFiloServis.Web/Services/LegacyDataTransferService.cs#L110) | 110 |
| [MKFiloServis.Web/Services/LegacyDataTransferService.cs](../MKFiloServis.Web/Services/LegacyDataTransferService.cs#L424) | 424 |
| [MKFiloServis.Web/Services/LegacyDataTransferService.cs](../MKFiloServis.Web/Services/LegacyDataTransferService.cs#L443) | 443 |
| [MKFiloServis.Web/Services/LegacyDataTransferService.cs](../MKFiloServis.Web/Services/LegacyDataTransferService.cs#L598) | 598 |
| [MKFiloServis.Web/Services/LegacyDataTransferService.cs](../MKFiloServis.Web/Services/LegacyDataTransferService.cs#L630) | 630 |
| [MKFiloServis.Web/Services/LegacyDataTransferService.cs](../MKFiloServis.Web/Services/LegacyDataTransferService.cs#L665) | 665 |
| [MKFiloServis.Web/Services/LegacyDataTransferService.cs](../MKFiloServis.Web/Services/LegacyDataTransferService.cs#L726) | 726 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L61) | 61 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L71) | 71 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L147) | 147 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L221) | 221 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L230) | 230 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L235) | 235 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L279) | 279 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L310) | 310 |
| [MKFiloServis.Web/Services/TestSessionService.cs](../MKFiloServis.Web/Services/TestSessionService.cs#L318) | 318 |
| [MKFiloServis.Web/Tests/RentACar/Program.cs](../MKFiloServis.Web/Tests/RentACar/Program.cs#L21) | 21 |
| [MKFiloServis.Web/Tests/RentACar/Program.cs](../MKFiloServis.Web/Tests/RentACar/Program.cs#L25) | 25 |


