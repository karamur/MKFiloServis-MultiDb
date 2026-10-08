# A-08 — Veritabanı yazım denetimi sözleşmesi

**Tarih:** 2026-10-05. Bu belge uygulama ve operasyon denetiminin sınırını tanımlar.

## 1. Ortak yazım kanıtı

- PostgreSQL: `mk_audit.write_journal`, public iş tablolarında INSERT/UPDATE/DELETE satır tetikleyicisi; TRUNCATE öncesi tablo adı ve satır sayısı. Tetikleyiciler `ENABLE ALWAYS` olduğundan DataSync'in replica oturumunda da çalışır.
- SQLite: `__MKWriteJournal`, iş tablolarında INSERT/UPDATE/DELETE tetikleyicileri. `sqlite_*` ve EF migration geçmişi dışarıda tutulur. Mevcut uygulama audit tablolarının yazım/silmesi de denetlenir; günlük kendi kendisini denetlemez.
- Günlük başka bir firmanın adına uydurulmaz. `FirmaId` veya `IsverenFirmaId` yoksa sistem kapsamıdır. PostgreSQL transaction kimliği, zaman, DB aktörü, uygulama adı ve gerçek satır kimliği saklanır; SQLite aynı transaction içinde tablo/satır/zaman/değer kaydı saklar.
- Sırlar ve sır taşıyabilecek serbest payload/değer/dosya alanları maskelenir. EF AktiviteLog ve iş günlüğü birbirinin yerine geçmez: EF uygulama kullanıcı bağlamını, DB günlüğü EF dışı yazımı da kapsar.
- Veri ve tetikleyici kaydı aynı transaction'ın parçasıdır. Audit hatası iş yazımını durdurur; rollback denetim satırını da geri alır. INSERT kimliği DB'den alındığından geçici EF kimliği sorunu yoktur.
- Günlük UPDATE/DELETE ile değiştirilemez. PostgreSQL TRUNCATE de reddedilir. DB sahibi/superuser'ın DDL veya tetikleyici kapatma yetkisine karşı kriptografik değişmezlik iddia edilmez; bu yetki uygulama dışı DB yönetim yetkisidir.

## 2. Şema ve araçlar

Ortak installer Shared assembly içinde gömülüdür. Web başlangıcında veri onarımlarından önce, şema görevlerinden sonra ve veri yazan 11 tarihsel PostgreSQL migration sınıfının SQL komutlarından önce kurulur. Yeni SMS tablosu seed’i ve özlük/maaş onarımları da installer’dan geçer. Kurulum hatası zorunlu başlangıç hatasıdır. Yeni tablo ekleyen bakım/migration kodu **ilk iş verisi yazımından önce installer'ı çağırmalıdır**; yalnız sonradan çalıştırmak ilk yazımı kanıtlamaz.

DataSync hedefinde installer aktarım öncesi çalışır. SQLite iş tablosu listelerinden `__MKWriteJournal` çıkarılır; PostgreSQL günlüğü public dışında olduğundan kopyalanan firma/kullanıcı/audit geçmişinden bağımsız kalır. Legacy hedef bağlantısı da installer'dan geçer. Dış SQL bakım araçları kurulu iş tablolarına yaptıkları yazımlarda bu DB tetikleyicilerine tabidir. Dört iş verisi yazan bağımsız SQL betiğine kanonik installer başlığı, psql ON_ERROR_STOP ve ilk veri yazımından önce yeni tablo installer kapısı eklendi. LisansDesktop yerel satış/yenileme DB’si aynı SQLite motorunu kaynak bağlantısıyla kullanır; yeni şema hazırlayan dış araçlarda aynı kurulum SQL'i zorunludur.

## 3. Restore kanıtı

- PostgreSQL uygulama yedekleri `mk_audit` şemasını iş yedeğine katmaz; restore yalnız `public` şemasını değiştirir. Audit şeması öncesinde oluşturulur, restore sonrası public tetikleyiciler yeniden kurulur. Kaynak DB yüklemesinden sonra audit doğrulaması başarısız olursa Web ZIP restore yolu önceki DB yedeğinin `database.backup` içeriğini `--single-transaction` ile geri uygular; başarılı rollback ayrı `rolled-back.json` makbuzuyla işaretlenir, rollback de başarısızsa işlem unconfirmed kalır.
- PostgreSQL restore custom dump ile `--single-transaction --exit-on-error` kullanır. Denetim şeması korunamayacak düz SQL restore desteklenmez; başarı gibi gösterilmez.
- Veritabanı dışında `%LOCALAPPDATA%/MKFiloServis/OperationJournal/<operation-id>` altında source SHA-256, hedef, OS aktörü, başlangıç ve ayrı tamamlanma makbuzu saklanır. Başlangıç makbuzu diske flush edilmeden restore başlamaz. Başarı makbuzu olmayan işlem **FailedOrUnconfirmed** kabul edilir; belirsiz commit/sonuçta rollback iddia edilmez.
- SQLite açık dosyayı `File.Copy` ile ezmez; SQLite backup API kullanır. Değiştirmeden önce bağımsız `before-restore.db` alınır; önceki DB audit geçmişi bu geri dönüş kopyasında korunur. Hedef yüklendikten sonra tetikleyiciler kurulur.
- Deploy restore betiği mevcut DB'yi DROP etmez; `mk_audit` hariç geri dönüş yedeği ve operasyon makbuzu üretir, audit şemasını korur, hata kodu 1'i başarı kabul etmez. Kaynak restore hata verirse eski public şemayı tek transaction ile geri yükler ve audit installer'ı doğrular; hedef DB bu işlemde yeni oluşturulmuşsa yalnız o yeni DB'yi kaldırır. Rollback kanıtı makbuzda, rollback başarısızlığı ayrı kayıtta tutulur. Ortam parolası finally ile önceki haline döner.
- Bu işlem DB restore kanıtıdır. Dosya/ayar/key ring'i kapsayan tam kurtarma A-03/A-04; gerçek mali zincir, üretim veri büyüklüğü, yetki ve çok süreç kabulü A-09/A-18/A-19 kapsamındadır.

## 4. Kontrol kanıtı

İzole SQLite kontrolünde doğrudan SQL, firmasız sistem kaydı, sır maskeleme, veri/audit rollback, değişmez günlük ve audit kaydı yazılamadığında iş UPDATE'inin durması geçti. PostgreSQL doğrulaması, binary COPY ve gerçek izole custom dump/restore da geçti. [Ayrıntılı izole kanıt](A-08-IZOLE-DOGRULAMA-2026-10-05.md) kaynak SHA-256 ve kapsamı kaydeder; son derlemeler son durum raporundadır. Üretim/müşteri verisiyle restore yapılmış sayılmaz.
