# A-16 — Eski veri için salt okunur ön envanter

**Tarih:** 2026-10-09 (kapsam kararıyla A-16 kapatıldı)
**Durum:** 🟢 A-16'nın tanımlı teslimi tamamlandı: salt okunur raporlama aracı.

**Kapanış kararı:** Kullanıcı, A-16'yı salt okunur eski veri/tenant risk raporlama aracının teslimi olarak kapatma kararı verdi. Gerçek müşteri verisi tarama, bulguyu onaylama veya düzeltme bu kod teslim görevinin parçası değildir; müşteri geçiş/dağıtım operasyonunda ayrıca yürütülür. Bu görev kapanışı müşteri DB'sinin tarandığını ya da temiz bulunduğunu ifade etmez.

## Amaç ve kapsam

`MKFiloServis.DataSync inventory` veritabanını değiştirmeden 18 sabit kontrolü ve şemadan keşfedilen tenant ilişkisi adaylarını sayar; her kontrol için en fazla 20 kayıt kimliği verir:

| Kod | Denetim |
|---|---|
| A16-01 | Aktif araçta eksik firma bağı |
| A16-02 | Firma içinde yinelenen aktif şase |
| A16-03 | Firma içinde yinelenen güncel plaka |
| A16-04 | Araçtaki kiralık/komisyoncu carisinin farklı firmada olması veya bağlı carinin bulunamaması |
| A16-05 | Banka hareketi hesabının boş/geçersiz olması, bulunamaması veya firma uyuşmazlığı |
| A16-06 | Fatura ve carisinin firma uyuşmazlığı veya carinin yokluğu |
| A16-07 | Firma/personel/dönem maaş snapshot tekrarı |
| A16-08 | Araç/dönem maliyet snapshot tekrarı |
| A16-09 | Firmada birden çok aktif varsayılan fatura şablonu |
| A16-10 | Firma/kullanıcı için birden çok varsayılan fatura grup şablonu |
| A16-11 | Banka hareketi ile personelin firma uyuşmazlığı veya personelin yokluğu |
| A16-12 | Banka hareketi ile aracın firma uyuşmazlığı veya aracın yokluğu |
| A16-13 | Banka hareketi ile araç masrafının firma uyuşmazlığı veya masrafın yokluğu |
| A16-14 | Banka hareketi ile mahsup/geri ödeme hareketinin firma uyuşmazlığı veya hareketin yokluğu |
| A16-15 | Fatura ödeme eşleştirmesinde fatura veya banka hareketi yok/firmasız ya da firmalar uyuşmuyor |
| A16-16 | Banka hareketi carisi geçersiz, bulunamıyor veya firma uyuşmazlığı var |
| A16-17 | Banka hareketinin personel ödeme hesabı geçersiz, bulunamıyor veya firma uyuşmazlığı var |
| A16-18 | Maaş snapshot personeli eksik veya firma bağı uyuşmuyor |

Ek olarak araç, SQLite'ta tanımlı foreign key'leri her kaynak tablo için `pragma_foreign_key_check` ile tarar; PostgreSQL'de `information_schema` üzerinden tanımlı tekli/bileşik FK'leri çıkarıp salt okunur sorguyla sahipsiz kayıtları bulur. İhlaller sırasıyla `A16-SQLITE-FK-*` ve `A16-PG-FK-*` kodlarıyla çıkar; `SampleIds`, satır kimliği bulunmayan tablolar için `-1` kullanır. Her iki sağlayıcıda ayrıca kaynak ve hedefte `FirmaId` olan FK'lerin tüm bileşenleri eşleştirilerek tenant uyuşmazlığı `A16-TENANT-*` kodlu **inceleme adayı** olarak raporlanır. Composite firma uyuşmazlığı hem SQLite hem PostgreSQL sentetik şemasında doğrulanmıştır; gerçek müşteri şemasında kabul edilmemiştir. FK'siz uygulama ilişkileri, genel muhasebe fişlerinin iş kuralı toplamları veya dosya referansları kapsanmaz. Bulgu otomatik düzeltme talimatı değildir; ilişkilerin çapraz firma kullanımı geçerli olabilir ve iş sahibi tarafından doğrulanmalıdır.

SQLite `ReadOnly` modunda açılır; PostgreSQL `REPEATABLE READ` ve `READ ONLY` transaction içinde taranır. Rapor kaynak bağlantı dizisini, parolayı veya müşteri adlarını yazmaz. `Count` sorunlu kayıt sayısıdır; yinelenme kontrollerinde ilk kayıt değil, fazladan kayıtlar sayılır. `SampleIds` elle inceleme başlangıcıdır, otomatik silme önerisi değildir.

## Çalıştırma

Release derlemesinden sonra müşteri DB'sinin **yedek veya kontrollü kopyasında** çalıştırın. Raporu uygulama deposu dışında, erişimi sınırlandırılmış bir konuma kaydedin.

```powershell
dotnet build .\MKFiloServis.DataSync\MKFiloServis.DataSync.csproj -c Release

.\MKFiloServis.DataSync\bin\Release\net10.0-windows\MKFiloServis.DataSync.exe inventory `
  --provider sqlite --source 'D:\IzoleKopya\filo.db' `
  --output 'D:\GuvenliRaporlar\a16-sqlite.json'
```

PostgreSQL bağlantısını komut satırına düz parola olarak yazmak yerine yetkili sır kaynağından bir ortam değişkenine sağlayın; komut yalnız değişkenin adını alır:

```powershell
.\MKFiloServis.DataSync\bin\Release\net10.0-windows\MKFiloServis.DataSync.exe inventory `
  --provider postgresql --source-env MKF_A16_PG_SOURCE `
  --output 'D:\GuvenliRaporlar\a16-postgresql.json'
```

`Complete=true`, 18 sabit sorgunun, ilgili sağlayıcının foreign key ihlal kontrollerinin ve keşfedilebilen basit tenant FK kontrollerinin çalıştığını gösterir; `FindingCount=0` yalnız raporda yer alan kontroller için temiz sonuçtur. Tablo/kolon eksikse ilgili kontrol `schema_missing`, SQL hatası varsa `scan_error` olur, rapor `Complete=false` döner ve komut başarısız çıkar. Çıktı yolu SQLite kaynak dosyasıyla aynı olamaz. Rapor geçici dosyada hazırlanıp nihai ada taşınır.

## Doğrulama ve kapanış sınırı

2026-10-07'de sentetik SQLite ve ayrı geçici PostgreSQL 17 kümesinde sabit 10 örnek sorun üretildi. Her iki rapor `Complete=true`, `FindingCount=10` verdi. Eksik şemalı SQLite örneği `Complete=false` verdi. Kaynak DB ile aynı rapor yolu reddedildi ve SQLite dosya başlığı korundu. 2026-10-08'de Release derlemesi **0 uyarı/0 hata** ile geçti. Sentetik SQLite kopyasında tekli/bileşik `FirmaId` uyuşmazlıkları `A16-TENANT-*` ile, sahipsiz tekli/bileşik FK'ler `A16-SQLITE-FK-*` ile bulundu. Geçici PostgreSQL 17 kümesinde sahipsiz tekli/bileşik FK kayıtları `A16-PG-FK-*` kontrollerinde **1'er**, composite tenant uyuşmazlığı da `A16-TENANT-*` ile **1** bulgu verdi. Deneme şemasında uygulamanın sabit 10 tablosu bulunmadığı için rapor `Complete=false` ve komut çıkışı 3 verdi; sentetik FK kontrolü başarılı olsa da tam envanter kabulü sayılmaz.

2026-10-09'da A16-11–A16-15 kontrolleri geçici sentetik SQLite veritabanında uçtan uca çalıştırıldı. İlk fixture `Complete=true` ve toplam **5** bulgu verdi; her yeni kontrol tam olarak bir beklenen örnek kimliğini buldu. A16-04 sınır düzeltmesinden sonraki genişletilmiş fixture `Complete=true`, toplam **7** bulgu verdi: A16-04 üç hatalı cari bağlantısı olan iki aracı tekil kimliklerle raporladı; A16-11–A16-15'in her biri beklenen bir örneği buldu. Diğer sabit sorgularda bulgu yoktu. Kaynak sentetik DB `ReadOnly` açıldı ve SHA-256 özeti tarama öncesi/sonrası aynı kaldı; gerçek müşteri verisine bağlanılmadı veya müdahale edilmedi.

Aynı gün sınır durumu incelemesinde A16-15'in banka hareketindeki `FirmaId IS NULL` değerini SQL `<>` karşılaştırmasının `UNKNOWN` sonucu nedeniyle kaçırabileceği bulundu. Sorgu, banka hareketi firması boş veya sıfır/negatifse de bulgu verecek şekilde düzeltildi. Diğer zorunlu tabloları içermeyen minimal sentetik SQLite şemasında A16-15 beklenen eşleştirme kimliğini buldu; bu sınırlı koşuda tüm envanterin `Complete=false` dönmesi beklenen sonuçtu.

A16-04'te de eski `INNER JOIN` carisi eksik olan araç bağlarını tamamen gizleyebiliyordu; ayrıca kiralık ve komisyoncu cari bağlantılarından ikisi birden sorunluysa tek araç birden fazla kez sayılabilirdi. Denetim ayrı `LEFT JOIN` ile her iki bağı da kontrol eder, sahipsiz/firmasız/geçersiz cari bağlantılarını bulur ve araç kimliğini tekilleştirir.

A16-05'te banka hareketinin `BankaHesapId` değeri boş, sıfır veya negatifse eski `> 0` filtresi bu hareketi sessizce atlayabiliyordu. Denetim artık geçersiz hesap kimliğini, bulunmayan hesabı ve boş/geçersiz firma bağlarını da raporlar.

2026-10-09 Release CLI kabulinde sentetik tam şemada hesap kimliği boş, `0`, `-4` ve mevcut olmayan `999` olan dört hareketin tamamı A16-05'te bulundu; geçerli hesaplı beşinci hareket temiz kaldı. Rapor `Complete=true`, `FindingCount=4`, A16-05 `Count=4`, kimlikler `101,102,103,104` ve çıkış kodu `0` verdi. Bu yalnız sentetik sınır testi olup müşteri verisi kabulü değildir.

Bu araç foreign key olarak tanımlanmamış tenant ilişkilerini, FK taraflarından birinde `FirmaId` bulunmayan ilişkileri, eski finans fişi toplamlarını, tarih semantiğini veya dosya referanslarını kapsamaz. **Gerçek müşteri verisi taranmadı ve onarım yapılmadı.** Her müşteri geçişinde rapor kontrollü/yedek DB kopyasında çalıştırılmalı; bulgular yetkili kişi tarafından incelenmeli ve gerekiyorsa onarım/geri dönüş ile öncesi/sonrası kanıtı hazırlanmalıdır. Bu operasyonel sınır A-16 kod teslimini açık tutmaz. A-15 migration ve eşzamanlılık kabulü ayrıdır.

### 2026-10-09 — A16-16/A16-17 kaynak kapsamı eklendi

A-15 banka hareketi guard migration'ı ile A-16 sabit kontrolleri karşılaştırılırken migration'ın ayrıca `BankaKasaHareketleri.CariId` ve `PersonelOdemeHesapId` bağlantılarını firma kapsamına aldığı, envanterin bu bağları ayrı ayrı denetlemediği görüldü. A16-16 ve A16-17 kontrolleri kaynak koda eklendi; boş/negatif kimlik, eksik hedef, geçersiz firma ve çapraz-firma bağlarını raporlayacak şekilde yazıldı. DataSync Release derlemesi **0 uyarı / 0 hata** ile geçti. Sentetik SQLite CLI kabulinde `Complete=true`, `ReadOnly=true`, toplam **6 bulgu** döndü; A16-16 ve A16-17 kontrollerinin her biri beklenen **3** çapraz-firma/boş-geçersiz/sahipsiz örnek kimliği buldu. Kaynak DB SHA-256 değişmedi. İzole PostgreSQL 17 kümesi `127.0.0.1` soket izni (`Permission denied`) nedeniyle başlatılamadı; PostgreSQL sorgu kabulü çalıştırılmadı. Müşteri DB'sine bağlanılmadı/değişiklik yapılmadı.

### 2026-10-09 — yerel `App_Data/test.db` salt okunur ön taraması

Güncel 17 sabit kontrolle Release CLI çalıştırıldı; rapor ayrı `temp` yoluna yazıldı. Çıkış kodu **3**, `Complete=false`, `ReadOnly=true`, **248** toplam kontrol (247 `clean`, bir `schema_missing`) ve `FindingCount=0` döndü. A16-11, test DB şemasında `Soforler` tablosu bulunmadığı için çalışmadı; bu nedenle sıfır bulgu temiz/tam sonuç değildir. A16-16 ve A16-17 mevcut şemada çalışıp bulgu vermedi. Kaynak `test.db` SHA-256 özeti tarama öncesi/sonrası aynı kaldı. Bu yerel test dosyasıdır; gerçek müşteri DB'si taranmadı.

### 2026-10-09 — A16-16/A16-17 SQLite sınır durumları genişletildi

Ayrı sentetik tam şema fixture'ında her yeni kontrol için beşer beklenen örnek oluşturuldu: çapraz firma, sıfır, negatif, bulunmayan hedef ve boş/geçersiz hareket firması. Ayrıca iki isteğe bağlı ilişkinin NULL olduğu geçerli hareket eklendi ve bulgu vermedi. CLI `Complete=true`, `ReadOnly=true`, toplam **12** bulgu ve çıkış kodu **0** döndürdü; A16-16 `Count=5` (`501,503,505,507,509`), A16-17 `Count=5` (`502,504,506,508,510`) verdi. Sentetik kaynak DB SHA-256 özeti değişmedi. Bu SQLite kapsamıdır; PostgreSQL ve müşteri verisi kabulü değildir.

### 2026-10-09 — A16 sabit firma kontrollerinde sıfır firma sınırı

A16-06 ile A16-11–A16-14 karşılaştırmalı kontrollerinin `FirmaId IS NULL` durumunu bulduğu, ancak her iki uçta da `FirmaId=0` olduğunda `<>` karşılaştırmasının uyuşmazlık üretmediği görüldü. Sorgular sıfır/negatif firma kimliklerini de bulacak şekilde düzeltildi. DataSync Release derlemesi **0 uyarı / 0 hata**. Sentetik SQLite CLI kabulinde `Complete=true`, `ReadOnly=true`, çıkış kodu **0**; bu beş kontrolün her biri kendi beklenen bir örnek kaydı buldu (`601`, `611`, `612`, `613`, `614`); kaynak DB SHA-256 değişmedi. PostgreSQL ve gerçek müşteri verisi kabulü yapılmadı.

### 2026-10-09 — A16-18 maaş snapshot personel/firma bağı

`MaasOdemeSnapshotlar.PersonelId` ile `FirmaId` bağlantısı uygulama ilişki taramasında FK ile güvence altında değildi. A16-18 aktif snapshot'ta boş/sıfır/negatif personel kimliği, bulunmayan şoför/personel, geçersiz firma ve çapraz-firma bağını raporlar. DataSync Release derlemesi **0 uyarı / 0 hata**. Sentetik SQLite kabulinde `Complete=true`, `ReadOnly=true`, A16-18 `Count=3`, beklenen örnek kimlikleri `801,802,803`; geçerli aynı firma satırı ile silinmiş uyumsuz satır bulgu üretmedi. Kaynak SHA-256 değişmedi. PostgreSQL ve müşteri verisi kabul edilmedi.
