# MKFiloServis — Derleme ve Test Doğrulaması

**Tarih:** 2026-10-08 (Europe/Istanbul)  
**Kod:** `0aecc4f1` tabanı + mevcut çalışma alanı değişiklikleri; bu sonuç commit edilmiş sürüm sonucu değildir.  
**Ortam:** Windows, .NET SDK 10.0.401, Release.

## Sonuçlar

| Kontrol | Durum | Kanıt / kapsam |
|---|---|---|
| Çözüm Release derlemesi | 🟢 Geçti | 0 hata / 0 uyarı; Web, Shared, Tests, DataSync, LisansDesktop, Client ve PlaywrightSmoke projeleri |
| Otomatik test paketi | 🟢 Geçti | Son koşu: 102 başarılı / 0 başarısız / 0 atlanan; yaklaşık 21 saniye |
| Yeni SQLite mali regresyon testleri | 🟢 Geçti | Sayaç/rollback 1, işlem anahtarı migration 5, güncel izin 4, maaş servis yazımı 3, banka transaction 5, otomatik mahsup 1, banka iptal 6, personel geri ödeme 9, banka tekrar istek 7: toplam 41 yeni senaryo |
| Tarayıcı kabul/smoke testi | ⚪ Çalıştırılmadı | CRMFILO_TEST_USER ve CRMFILO_TEST_PASSWORD yapılandırılmamış; çalıştırılmadı, geçer sayılmadı |
| İzole PostgreSQL sayaç ve yeni işlem anahtarı migration kontrolleri | 🟢 Geçti | PostgreSQL 17, ayrı geçici cluster, port 55439; gerçek uygulama sayacı, rollback, 20 eşzamanlı numara ve dört tablo migration/benzersiz indeks kontrolü |
| PostgreSQL tam mali servis ve tam migration zinciri | ⚪ Çalıştırılmadı | Yukarıdaki sınırlı kontrol bu kabulün yerine geçmez |
| Gerçek müşteri restore, S3, çok sunucu ve hedef hacim | ⚪ Çalıştırılmadı | Test verisi/ortamı ve ilgili kabul senaryoları bu koşuda kullanılmadı |

## Çalıştırılan komutlar

```powershell
dotnet build MKFiloServis.slnx -c Release --nologo -v minimal
dotnet build MKFiloServis.slnx -c Release --no-restore --nologo -v minimal
dotnet test MKFiloServis.Tests/MKFiloServis.Tests.csproj -c Release --nologo --logger "trx;LogFileName=finance-validation.trx" --results-directory TestResults/final-validation -v minimal
pg_isready -h 127.0.0.1 -p 5432
```

[Derleme çıktısı](../build-final-validation.log), [test çıktısı](../test-final-validation.log), [TRX sonucu](../TestResults/final-validation/finance-validation.trx). Log dosyaları yerel çalışma kanıtıdır; depoya eklenmiş oldukları varsayılmaz.

## Yeni testlerin kapsadığı davranış

- `FinancePersistenceSqliteTests`: gerçek SQLite bağlantısında uygulamanın sayaç metodu, transaction rollback sonrası sayacın geri dönmesi, firma/prefix/ay ayrımı. İki yeni işlem anahtarı migration'ının gerçek EF SQL üreticisiyle dört minimal eski tabloya uygulanması; eski NULL anahtarların korunması; soft-delete sonrasında aynı anahtarın benzersiz indeksçe reddi.
- `CurrentPermissionGuardSqliteTests`: mevcut oturumla yetki, aktif kullanıcı, rol ve Admin statüsü DB'den kaldırılınca sonraki çağrının reddi; token içindeki Admin rolü DB iznini aşamaz.
- `PayrollWriteSqliteTests`: gerçek tam EF SQLite modelinde maaş servis çağrıları; eski UpdatedAt ile kesintiyi ezme girişimi; aynı ödeme tarihi/açıklamasını tekrar gönderme; farklı ödeme içeriğini reddetme; ödenmiş maaşı değiştirme, kaldırma ve yeniden hesaplama retleri; ödenmemiş maaşın fiziksel satırı korunarak soft-delete edilmesi.

İlk ek maaş testleri test fixture'ındaki eksik Organizasyon FK'sı ve async fixture'dan taşınmayan HttpContext nedeniyle kurulamadı. Fixture'a organizasyon eklendi ve test HttpContext erişimi doğrudan fixture nesnesine bağlandı. Bu test kurulum düzeltmeleri sonrası paket tekrar çalıştırıldı; son sonuç 54/54'tür. Üretim kodu sırf test geçsin diye gevşetilmedi.

## Sonucun sınırı ve kalan doğrulama

Bu koşu tüm satış kabul maddelerini kapatmaz. SQLite migration testleri yalnız yeni dört işlem anahtarı tablosunun yükseltilmesini sınar; uygulamanın tam geçmiş migration zincirini veya gerçek müşteri verisini sınamaz. Maaş testleri eşzamanlı çok sunucu/commit yanıt kaybı senaryolarını kapsamaz. Transfer/cari mahsup fişlerinin tüm hata aşamaları, mahsup iptali ve personel geri ödeme zinciri için servis entegrasyon testleri ayrıca gereklidir.

Sonraki doğrulama: izole PostgreSQL ve tam migration zinciri; transfer/cari mahsup hata/rollback ve eşzamanlı bakiye; otomatik/tekil mahsup ve geri alma; test hesabıyla tarayıcı senaryoları; hedef müşteri restore/S3/çok sunucu/hacim kabulü. A-09/A-29 ve diğer açık görevlere bu eksik kanıtlar verilmeden yeşil kapanış uygulanmaz.

## Altıncı doğrulama — SQLite migration başlangıcı ve dashboard finans şeması

🟢 2026-10-08 tarihli Release çözüm derlemesi **0 hata / 0 uyarı**; son tam test paketi **102/102 başarılı, 0 atlanan**. Bu koşu önceki 96/96 sonucunu günceller.

### Hata ve kök neden

Dashboard finans sorgusu `BankaKasaHareketleri.IslemKimligi` sütununu okurken SQLite `no such column` veriyordu. Kök neden, migration geçmişi bulunmayan eski SQLite veritabanlarında açılış kodunun kullanıcı tablolarını gördükten sonra **tüm** bekleyen migration'ları DDL çalıştırmadan `__EFMigrationsHistory` tablosuna yazmasıydı. Dolayısıyla yeni banka işlem anahtarı migration'ı uygulanmış gibi görünüyordu.

### Düzeltme ve regresyon koruması

- Eski SQLite migration baseline'ı bilinen eski şema watermark'ına kadar sınırlandı; bu eşiğin üstündeki yeni migration'lar artık geçmişe yazılmıyor, EF tarafından çalıştırılıyor.
- Daha önce yanlışlıkla geçmişe yazılmış kurulumlar için açılışta `BankaKasaHareketleri.IslemKimligi` ve `IslemOzeti` eksikse nullable sütunlar ekleniyor ve benzersiz indeks idempotent biçimde tamamlanıyor. Mevcut hareket satırları değiştirilmez.
- `SqliteMigrationBootstrapTests`: yeni migration'ın baseline dışı kaldığını ve eksik sütun/indeks onarımının iki kez çağrıldığında da çalıştığını; eski verinin aynen kaldığını denetleyen testler. Üçüncü senaryo eski şemada dashboard son hareket sorgusunun hata verdiğini, açılış onarımı sonrası sorgunun çalıştığını doğrular.

Bu doğrulama izole bellek SQLite şemasında yapılmıştır. Gerçek müşteri DB'si açılmadı/değiştirilmedi; düzeltmenin müşteri dağıtımında uygulanması ve dashboard kabulü ayrıca gereklidir. [Son Release derlemesi](../build-a15-final.log), [son tam test paketi](../test-a15-final.log).

## A-15 ek düzeltme — fatura/ödeme eşleştirme firma bağı

`OdemeEslestirme` satırı tenant entity'si olmadığından, bir firmaya ait faturayla başka firmaya ait banka hareketi EF kaydetme sınırında eşleştirilebiliyordu. Sync ve async `SaveChanges` öncesi her iki kaydın gerçek FirmaId'si doğrulanır; bulunamayan veya firması olmayan uçlar reddedilir. Dört SQLite senaryosu aynı firma eşleşmesinin kabulünü ve çapraz-firma eşleşmesinin her iki SaveChanges yolunda reddini doğruladı.

Ek olarak `20261008130000_GuardInvoicePaymentMatchFirm` migration'ı SQLite ve PostgreSQL için DB trigger koruması kurar. Migration ön kontrolü eski çapraz-firma/eksik uç eşleşmelerini sessizce geçirmez. Eşleştirme ekleme/güncelleme ve eşleştirilmiş fatura/banka hareketinin firma değişikliği DB seviyesinde reddedilir. Altı izole SQLite migration SQL testi; kirli eski verinin reddi, aynı firma eşleştirmesi ve dört geçersiz doğrudan SQL değişikliğini doğruladı. Ayrı PostgreSQL 17 geçici cluster'ında EF migration SQL'i uygulandı ve aşağıdaki sonuçlar alındı:

```text
PASS: A-15 PostgreSQL cross-firm match rejected
PASS: A-15 PostgreSQL linked invoice firm update rejected
PASS: isolated PostgreSQL migration/trigger probe
```

Son Release çözüm derlemesi 0 hata/uyarı; tam test paketi **102/102 geçti, 0 atlandı**. Müşteri DB'sinde migration, çok sunuculu eşzamanlılık kabulü ve A-15'in diğer ilişkileri açık olduğundan A-15 genel durumu 🔴 kalır.


## İkinci test koşusu — 2026-10-08

Son kod ve testlerle çözüm Release derlemesi yeniden **0 hata / 0 uyarı**; paket **60/60 başarılı, 0 atlanan**. Önceki 54/54 sonucu ilk koşuydu; güncel sonuç 60/60'tır.

### Ek servis senaryoları

- `BankTransactionSqliteTests`: transfer başarı/fiş hatası (2), cari mahsup başarı/fiş hatası (2), para birimi uyuşmazlığı/eksik hesap eşleştirmesi (1). Gerçek tam EF SQLite modeli ve başlangıç sayaç şema yardımcısı kullanılır. Fiş hatası SQLite trigger'ıyla enjekte edilir; başarısız işlem sonrası hareket/fiş bulunmaması, bakiye korunması, cari alt hesap ve üst hesabın AltHesapVar bayrağının geri dönmesi denetlenir. Başarılı transferde iki karşı FK ve ortak MuhasebeFisId doğrulanır. EF trigger hatasını DbUpdateException olarak sarmalar; test bu beklenen kayıt hatasını denetler.
- `PayrollAdvanceSqliteTests`: otomatik parti tekrarında tek mahsup ve tek maaş kesintisi; kaldırmada maaş/avans bakiyelerinin geri dönmesi, mahsup satırının soft-delete kalması ve kaldırılmış partinin tekrar uygulanmaması.

### Testin yakaladığı üretim hatası ve düzeltmesi

`SchemaSyncHelper.EnsureFisNoCountersSchemaAsync` SQLite kontrolünde DbContext'in bağlantısı `await using` ile dispose ediliyordu. Tam bellek veritabanı bağlantısı kapandığında sonraki işlemlerde tablolar kayboluyordu. Bağlantı sahipliği DbContext'te bırakıldı; yalnız kontrol komutu dispose edilir. Gerçek şema yardımcısını çağıran banka servis testleri düzeltme sonrası tekrar geçti.

### İzole PostgreSQL

Mevcut localhost:5432 veritabanına yazılmadı. TestResults altında ayrı cluster `initdb` ile kuruldu, yalnız `127.0.0.1:55439` bağlantısı kabul edildi. Yerel validation kullanıcısı yalnız bu geçici cluster içindir; müşteri hesabı/şifresi kullanılmadı. `TestResults/pg-probe/Probe.csproj` ve Program.cs üzerinden gerçek `SchemaSyncHelper`, `MuhasebeService.NextFisNoCounterAsync` ve EF migration SQL üreticisi çalıştırıldı.

- Sayaç başlangıcı ve aynı transaction'da ikinci numara: geçti.
- Transaction rollback sonrası numaranın geri dönmesi: geçti.
- 20 bağımsız context'ten eşzamanlı istekte 20 farklı ardışık numara (3–22): geçti.
- Firma bazında sayaç ayrımı: geçti.
- İki yeni işlem anahtarı migration'ı dört minimal eski tabloya uygulandı; eski NULL anahtarlar korunarak soft-delete satırının tükettiği anahtarın tekrar INSERT'i dört tabloda da 23505 ile reddedildi.

[PostgreSQL kontrol çıktısı](../TestResults/pg-validation.log), [geçici probe kaynağı](../TestResults/pg-probe/Program.cs). Probe kaynak/logları yerel TestResults çıktılarıdır; sürüm kontrolüne eklenmiş varsayılmaz. Probe derlemesinde sabit tablo adlarıyla SQL üretiminde EF1002 analizör uyarıları oluştu; çözüm derlemesinin 0 uyarı sonucu bu ayrı probe projesini kapsamaz.

Geçici sunucu `pg_ctl stop` ile durduruldu. Test cluster dosyaları kanıt için tutuldu; çalışan geçici servis bırakılmadı. Bu kontrol tam PostgreSQL maaş/transfer zinciri, tam eski migration zinciri, gerçek müşteri verisi veya çok sunucu kabulü değildir.


## Üçüncü doğrulama — mahsup iptali

🟢 Son Release çözüm derlemesi 0 hata / 0 uyarı; paket 66/66 geçti, 0 atlandı, yaklaşık 13 saniye. Son koşu `--no-build --no-restore` ile başarılı çözüm derlemesinin ardından çalıştırıldı; yukarıdaki log/TRX son koşuya aittir.

`BankTransactionSqliteTests` içine altı iptal senaryosu eklendi: transfer/cari iptali, her iki işlemde ters fiş INSERT hatasıyla tam rollback, eksik karşı hareket ve tutar uyuşmazlığı reddi. Onaylı eski fiş ile onaylı ters kaydın hesap bazında net sıfırı; fiziksel geçmiş, tekrar iptal reddi, banka bakiyesi ve manuel onay/onay geri alma/düzenleme/silme retleri denetlendi. İptal yolu artık banka transaction'ını kullanır; ayrı context ve boş catch kaldırıldı.

Bu altı senaryo SQLite'ta çalıştı; PostgreSQL iptal servis yolu bu koşuda çalıştırılmadı. Önceden hatalı iptal edilmiş müşteri fişlerinin otomatik veri onarımı yapılmadı. Kalan kabul sınırları ve ortak personel geri ödeme iptali açık.


## Dördüncü doğrulama — personel geri ödeme

🟢 Son Release çözüm derlemesi 0 hata / 0 uyarı; paket **75/75 geçti, 0 atlandı**, yaklaşık 18 saniye. Dokuz yeni `BankTransactionSqliteTests` senaryosu geri ödeme kapanışı, ödeme/iptal UPDATE hata enjeksiyonu ile rollback, ortak ödemenin tüm bağlı masraflarını geri açma, geçmiş ödeme satırını koruma, tekrar ödeme/iptal reddi, eksik seçim, fişli ödeme, para birimi/toplam tutarsızlığı ve hesapsız ödeme işaretini kapsar. Son koşu başarılı çözüm derlemesinden sonra `--no-build --no-restore` ile çalıştırıldı; yukarıdaki log/TRX son koşuya aittir.

Ekranın ödeme durumu düzenleme alanı kaldırıldı, iptal izni servisle eşleştirildi ve ortak iptal için onay açıklaması düzeltildi. Razor değişiklikleri derlendi; tarayıcı etkileşim testi yapılmadı. Bu dokuz servis senaryosu SQLite'ta çalıştı; PostgreSQL personel geri ödeme yolu ve gerçek müşteri kayıtlarının onarımı çalıştırılmadı. Fiş/eşleştirme/bütçe ödemesine bağlanmış geri ödeme iptali güvenli biçimde reddedilir; otomatik ters fiş desteği varsayılmaz.


## Beşinci doğrulama — banka istek kimlikleri

🟢 Son Release çözüm derlemesi **0 hata / 0 uyarı**; paket **83/83 geçti, 0 atlandı**, yaklaşık 20 saniye. Başarılı çözüm derlemesinin ardından `--no-build --no-restore` ile çalıştırıldı; log/TRX son koşuya aittir.

Yedi yeni `BankTransactionSqliteTests` senaryosu, transfer/cari mahsupta aynı GUID'nin farklı yazımı ve aynı tutarın farklı decimal yazımıyla tekrarın aynı kaydı döndürmesini; farklı tutar/işlem türünün reddini; iptal edilmiş kimliğin tüketilmiş kalmasını; fiş INSERT hatasının kimliği de geri almasını ve aynı kimlikle sonraki başarılı denemeyi; kimliksiz/geçersiz/boş GUID çağrısının reddini denetler. `FinancePersistenceSqliteTests` yeni banka migration'ını EF SQLite SQL üreticisiyle minimal eski tabloya uygular; NULL eski kimlikler ve soft-delete sonrası benzersiz indeks reddi doğrulanır.

Mahsup ekranı bekleyen kimliği sessionStorage'da saklayacak, aynı sekmede yeniden açma/yenilemede kullanacak ve yalnız başarılı cevapta temizleyecek şekilde düzenlendi. Razor derlendi; tarayıcı saklama/yenileme senaryosu çalıştırılmadı. Bütçe çağrısı sabit kaynak kimliği gönderecek şekilde derlendi; tam bütçe ödeme akışı bu koşuda test edilmedi. Bütçe kaydının hareketle atomik bağlanması ayrı açık iştir. Yeni banka migration'ı müşteri DB'sine veya PostgreSQL'e bu koşuda uygulanmadı; dağıtımda yeni kolon/indeksler uygulanmadan bu sürüm kullanılmamalıdır.
