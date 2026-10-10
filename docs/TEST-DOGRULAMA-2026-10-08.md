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

## A-19 DataSync izole iki sağlayıcı doğrulaması

**Tarih:** 2026-10-09 (Europe/Istanbul)
**Amaç:** A-19 görev satırındaki aktarım bütünlüğü ve rollback ölçütlerini sentetik SQLite ve PostgreSQL 17.5 üzerinde doğrulamak.
**İzolasyon:** Geçici PostgreSQL kümesi yalnız `127.0.0.1:55441` üzerinde dinledi; müşteri verisi kullanılmadı ve küme doğrulama sonunda kapatıldı. Yerel ayrıntılı kanıt/artifact dizini: `TestResults/a19-postgres-smoke-c26b5deb9f63441587abb60404778b88/` (çalışma ağacında ignore edilen yerel çıktı).

| Senaryo | Sonuç |
|---|---|
| PostgreSQL→SQLite | 2 tablo, toplam 4 sentetik satır aktarıldı; var olan hedef satırlar yenilendi. |
| SQLite→PostgreSQL | Ebeveyn ve çocuk tablolarında 3'er satır aktarıldı; FK kontrolü geçti, sequence sonraki değere doğru ilerledi. |
| Eksik hedef şema/kolon | Her iki yönde ön kontrol aktarımı durdurdu; var olan hedef kayıtlar korundu. |
| PostgreSQL COPY kısıt ihlali | Check constraint hatası tüm transaction'ı geri aldı; önceki hedef satırlar değişmeden kaldı. |
| DataSync Release derlemesi | Başarılı, 0 uyarı / 0 hata. |

**Sınır:** Bu, sentetik izole sağlayıcı kabulüdür. Müşteri verisi, üretim hacmi/credential, gerçek geçiş provası veya iş sahibi kabulü değildir. A-19'un tanımlı kaynak ve izole doğrulama teslimi kapatıldı; canlı geçiş ilgili dağıtım kapısında yapılmalıdır.

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

Son Release çözüm derlemesi 0 hata/uyarı; tam test paketi **102/102 geçti, 0 atlandı**. Bu 2026-10-08 tarihli notta A-15'in PostgreSQL/müşteri DB migration'ı ve saha kabulü açık görünüyordu. 2026-10-09 kullanıcı kapsam kararıyla A-15 kod teslimi kapatılmıştır; müşteri migration'ı ve saha kabulü operasyonel takip olarak kalır, görev rengini açık tutmaz.


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

## 2026-10-09 — Puantaj finans snapshot firma sınırı

🟢 Web Release derlemesi **0 hata / 0 uyarı**. `PuantajFinansSnapshotSqliteTests` yabancı firma döneminin reddini, yerel dönem snapshot'ının doğru firmaya yazılmasını ve tekrar çağrıda ikinci kayıt oluşmamasını doğruladı. Ardından tam Release paketi **105/105 geçti, 0 atlandı**.

🟡 Bu koşu puantajdan fatura/kalem/link üretiminin çok bağlantılı atomikliğini, hakedişin fatura/snapshot zincirini veya müşteri verisi kabulünü doğrulamaz. Bu akışlar A-29 kapsamında açık kalır.

## 2026-10-09 — Tam yerel test paketi

🟢 `dotnet test MKFiloServis.Tests/MKFiloServis.Tests.csproj --no-restore -nologo` **134/134 geçti, 0 başarısız, 0 atlandı**. İlk derleme eski servis kurucularına göre kalan iki fixture'ı gösterdi; fixture'lar güncellendi. Koşu A-28 sağlayıcı kapsam regresyonlarını da içerdi. Test paketi SQLite ve yerel kaynak doğrulamasıdır; PostgreSQL/SQLite müşteri kurulum-yükseltme, kimlik/rol saha matrisi, gerçek lisans/restore/S3 ve harici portal kabullerini içermez.

## 2026-10-09 — GPS kapsam değişikliği sonrası son doğrulama

🟢 Güncel çalışma ağacında `dotnet test MKFiloServis.Tests/MKFiloServis.Tests.csproj -c Release --no-restore --nologo -v minimal`: **137/137 geçti, 0 başarısız, 0 atlandı**. A-20 için UTC gece sınırının İstanbul iş gününe çevrildiği regresyon pakete eklendi. `dotnet build MKFiloServis.slnx -c Release --no-restore --nologo -v minimal`: **0 uyarı / 0 hata**; Android Release trimming dahil tamamlandı. `git diff --check` hata vermedi.

🟡 Bu yerel doğrulama GPS kaldırma migration'ını müşteri DB'sinde çalıştırmaz ve A-01/A-02/A-04/A-05/A-06/A-09/A-11/A-12/A-14/A-17/A-18/A-20/A-21/A-24/A-25/A-26/A-27 dış kabulini kapatmaz. A-18 temiz PostgreSQL baseline eforu/parity kapsamı belli olmadığından Go/No-Go tarihi henüz belirlenmedi; önkoşullar [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md#satışa-çıkış-takvimi-ve-kapılar--2026-10-09).

## 2026-10-09 — A-18 fatura indeksi ve güncel tam test paketi

🟢 `InvoiceStartupIndexTests` iki SQLite başlangıç senaryosunu doğruluyor: indeks geçişi başarılıysa yeni indeks kurulur ve eski kaldırılır; eski kayıtlardaki yinelenme unique indeks kurulumunu bozarsa transaction rollback olur ve eski indeks korunur. Hedef PostgreSQL startup ve gerçek eski müşteri verisi kapsam dışıdır.

🟢 Ardından tüm yerel paket yeniden çalıştırıldı: **136/136 geçti, 0 başarısız, 0 atlandı**. A-18 genel görevi temiz/eski kurulum migration kabulü nedeniyle sarı kalır.

## A-18 PostgreSQL indeks ve temiz başlangıç provası

**Tarih:** 2026-10-09 (Europe/Istanbul)
**Sağlayıcı:** İzole PostgreSQL 17.5; yalnız sentetik invoice satırları. Geçici cluster localhost'ta çalıştı ve sonunda kapatıldı. Harness ve PostgreSQL logları yerel/ignore edilen `TestResults/a18-postgres-clean-7923367ad46d42debb1089b82514235e/` altındadır.

| Senaryo | Sonuç |
|---|---|
| Gerçek başlangıç indeks rutini, uyumlu eski veri | Yeni `FirmaId/FaturaYonu/FaturaNo` unique indeksi kuruldu; eski `IX_Faturalar_FaturaNo` kaldırıldı. |
| Gerçek başlangıç indeks rutini, yinelenen eski veri | Unique indeks hatası transaction'ı geri aldı; eski indeks korundu ve yeni indeks kurulmadı. |
| Tam boş PostgreSQL `DbInitializer.InitializeAsync` | Başarısız. Legacy migration atlama yolu mevcut olmayan `__EFMigrationsHistory` tablosuna insert ediyor. Bu atlama geçici devre dışı bırakıldığında migration `AylikOdemeGerceklesenler` tablosunu varsayarak başarısız oldu. Deneysel değişiklik geri alındı. |

**Karar:** İndeks taşıma/rollback davranışı iki sağlayıcıda kanıtlandı; A-18 kapanmadı. Temiz ve eski PostgreSQL şema başlangıcının destek sözleşmesi ve migration zinciri düzeltilmeden satış öncesi kurulum kabulü verilemez. PostgreSQL müşteri verisi kullanılmadı.

## 2026-10-09 — Legacy aktarımın migration öncesi şema üretme yolu kaldırıldı

- 🔴 Kaynak izinde `LegacyDataTransferService.EnsureSchemaAsync()` çağrısının `DbInitializer` öncesinde ve aktarım kapalı olsa bile çalıştığı bulundu. PostgreSQL model DDL'ini ve `EnsureCreatedAsync()` yolunu kullanarak boş hedefi EF migration zincirinin dışında önceden oluşturabiliyordu.
- 🟢 Yan yol ve kullanılmayan şema üretim metotları kaldırıldı. Legacy aktarım `DbInitializer` migration/seed adımından sonra çalışıyor.
- 🟢 `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj -c Release --no-restore --nologo -v minimal`: **0 uyarı / 0 hata**.
- 🟡 Bu değişiklik migration baseline üretmez. İzole boş PostgreSQL/SQLite başlangıç testi ve eski şema yükseltme kabulü bu düzenlemeden sonra henüz çalıştırılmadı; A-18 açık kalır.

### 2026-10-09 takip — boş PostgreSQL için fail-fast ve history helper

🟢 `DbInitializer` boş PostgreSQL'i core `Firmalar`/`Kullanicilar` tabloları üzerinden legacy DB'den ayırır; boş kurulum legacy migration'ı geçmişe kaydetmeden açıkça reddedilir. Eski, mevcut şemalı DB'de history helper `__EFMigrationsHistory` tablosunu transaction içinde idempotent oluşturup `ON CONFLICT DO NOTHING` ile yazar.

🟢 Ayrı geçici PostgreSQL 17 kümesinde gerçek boş DB ile initializer çalıştırıldı. Önceki history-table exception yerine açık preflight reddi alındı; reddedilen DB'de public tablo sayısı **0** kaldı. Test cluster kapatıldı; müşteri verisi kullanılmadı.

🟡 Bu A-18 temiz PostgreSQL kurulumunu çözmez: desteklenen başlangıç/migration baseline zinciri hâlâ eksik ve eski müşteri şemasında history recovery/yükseltme kabulü ayrıca yapılmadı.

### 2026-10-09 takip — model snapshot karşılaştırması ve GPS kapsamı

🔴 `ApplicationDbContext` için `Database.HasPendingModelChanges()` **true** verdi; migration assembly **141** migration listeliyor. Otomatik scaffold beş araç GPS tablosunu ve çok sayıda timestamp tür dönüşümünü bir araya getirmişti. Timestamp dönüşümleri kabul edilmedi ve ayrı incelenecek.

🟢 Ürün kapsam kararıyla araç takip GPS kaldırıldı. Snapshot'tan beş GPS entity/ilişkisi silindi; `20261009200000_RemoveVehicleGpsTracking` migration'ı bu tabloları child-first sırayla düşürür. Bu migration müşteri veritabanlarına henüz uygulanmadı; çalıştırıldığında GPS tablolarındaki veri silinir ve `Down` tarafından geri yüklenmez.

🟡 Snapshot'ta GPS farkı giderildi; diğer model/snapshot farkları ve boş PostgreSQL başlangıç zinciri hâlâ incelenmelidir. Temiz kurulum kabulü verilmedi.

## 2026-10-09 — A-02/A-17 analitik API rol kontrolü derlemesi

🔴 Kaynak incelemesinde rapor lisanslı OData, Grafana, Prometheus ve n8n uçlarında güncel rol izni kontrolü bulunmadı. Veri döndüren eylemlere `CurrentPermissionGuard` üzerinden `raporlar.oku` denetimi eklendi; metrik adı listesi döndüren anonim Grafana search istisna olarak kaldı.

🟢 `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj -c Release --no-restore -nologo`: **başarılı, 0 uyarı / 0 hata**. Bu düzeltme için rol/tenant runtime testi çalıştırılmadı; bu kayıt yalnız derleme ve kaynak kapsamını kanıtlar.

## 2026-10-09 — A-02/A-17 fatura grup şablonu izinleri

🔴 Fatura grup şablonu REST endpoint'leri kullanıcı/firma sahiplik kapsamını uyguluyor, ancak modül rol izinlerini istemiyordu. GET uçlarına `faturahazirlik.oku`, Create'e `faturahazirlik.yaz`, Update/Delete/SetVarsayilan'a `faturahazirlik.duzenle` güncel DB kontrolü eklendi.

🟢 `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj -c Release --no-restore -nologo`: **başarılı, 0 uyarı / 0 hata**. Test ve normal/Admin runtime matrisi çalıştırılmadı.

## 2026-10-09 — A-06/A-21 PC2 kurulum betiği kaynak denetimi

🟢 Eski PC2 publish betiği `appsettings.PC2.json` üretmeyecek ve üretim sırlarını pakete yazmayacak şekilde değiştirildi. `03-pc2-publish.ps1` PowerShell parser kontrolü geçti. PC2 talimatları ve setup README güncel paket akışı, PostgreSQL/SQLite kapsamı, ACL korumalı `dbsettings.json` ve harici `Jwt__Secret` ayarıyla eşitlendi.

🟢 `setup/Setup.iss` ve `setup/MusteriSetup.iss` kaynaklarında sağlayıcı seçenekleri statik olarak tarandı: PostgreSQL ve SQLite var; MSSQL seçeneği ve eski seçim dalı yok. `git diff --check` hata vermedi (yalnızca mevcut dosyalar için LF/CRLF normalizasyon uyarıları gösterildi).

🟡 Inno Setup EXE üretimi, hedef Windows/IIS kurulum-yükseltme, yedek restore, sır yükleme ve lisans aktivasyonu çalıştırılmadı. Bu kaynak düzenlemesi A-06/A-21'in operasyonel kabulini kapatmaz.

## 2026-10-09 — A-02/A-17 fatura API yazma izin sırası

🟢 `FaturalarController` Create eylemi fatura yönüne bağlı genel ve gelen/kesilen yazma izinlerini cari doğrulamasından önce denetler. Get/Update/Delete eylemleri yöne uygun izinleri kontrol eder ve yetkisiz faturayı 404 ile gizler; servis katmanındaki izin kontrolü de kalır.

🟢 `dotnet build MKFiloServis.Web/MKFiloServis.Web.csproj -c Release --no-restore -nologo` başarılı: **0 uyarı / 0 hata**. `git diff --check` hata vermedi; çalışma ağacındaki satır sonu normalizasyon uyarıları devam ediyor.

🟡 HTTP normal/Admin/yön matrisi çalıştırılmadı; A-02/A-17 saha kabulinin kalan kanıtı budur.

## 2026-10-09 — A-18 boş PG/SQLite baseline ve kısmi şema koruması

- 🟢 Boş SQLite `:memory:` veritabanında tam `DbInitializer.InitializeAsync` geçti; migration beklemedi ve dört varsayılan organizasyon oluşturuldu.
- 🟢 Ayrı, geçici PostgreSQL 17 cluster'ında boş DB için tam initializer geçti; migration beklemedi ve dört organizasyon bulundu. Kısmi PG şema fixture'ında baseline reddedildi, satır korundu ve migration history yaratılmadı. İki koşullu PG testi ayrı çalıştırmada **2/2 geçti**; cluster kapatıldı.
- 🟢 Kısmi SQLite eski şema fixture'ında baseline reddedildi, mevcut satır korundu ve migration history tablosu yaratılmadı.
- 🟢 Kalıcı test paketi **139 geçti / 2 PostgreSQL özel ortamı olmadığı için atlandı**. Release çözüm derlemesi **0 uyarı / 0 hata**.
- 🟡 Mevcut müşteri şema yükseltmesi ve rollback fixture'ı bu testlerin kapsamı dışındadır; A-18 genel görevi sarı kalır.
