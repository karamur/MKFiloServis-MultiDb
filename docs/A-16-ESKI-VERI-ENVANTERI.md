# A-16 — Eski veri için salt okunur ön envanter

**Tarih:** 2026-10-09
**Durum:** 🟡 Alt araç hazır; A-16 genel görevi 🔴 açık.

## Amaç ve kapsam

`MKFiloServis.DataSync inventory` veritabanını değiştirmeden 15 sabit kontrolü ve şemadan keşfedilen tenant ilişkisi adaylarını sayar; her kontrol için en fazla 20 kayıt kimliği verir:

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

`Complete=true`, 15 sabit sorgunun, ilgili sağlayıcının foreign key ihlal kontrollerinin ve keşfedilebilen basit tenant FK kontrollerinin çalıştığını gösterir; `FindingCount=0` yalnız raporda yer alan kontroller için temiz sonuçtur. Tablo/kolon eksikse ilgili kontrol `schema_missing`, SQL hatası varsa `scan_error` olur, rapor `Complete=false` döner ve komut başarısız çıkar. Çıktı yolu SQLite kaynak dosyasıyla aynı olamaz. Rapor geçici dosyada hazırlanıp nihai ada taşınır.

## Doğrulama ve kapanış sınırı

2026-10-07'de sentetik SQLite ve ayrı geçici PostgreSQL 17 kümesinde sabit 10 örnek sorun üretildi. Her iki rapor `Complete=true`, `FindingCount=10` verdi. Eksik şemalı SQLite örneği `Complete=false` verdi. Kaynak DB ile aynı rapor yolu reddedildi ve SQLite dosya başlığı korundu. 2026-10-08'de Release derlemesi **0 uyarı/0 hata** ile geçti. Sentetik SQLite kopyasında tekli/bileşik `FirmaId` uyuşmazlıkları `A16-TENANT-*` ile, sahipsiz tekli/bileşik FK'ler `A16-SQLITE-FK-*` ile bulundu. Geçici PostgreSQL 17 kümesinde sahipsiz tekli/bileşik FK kayıtları `A16-PG-FK-*` kontrollerinde **1'er**, composite tenant uyuşmazlığı da `A16-TENANT-*` ile **1** bulgu verdi. Deneme şemasında uygulamanın sabit 10 tablosu bulunmadığı için rapor `Complete=false` ve komut çıkışı 3 verdi; sentetik FK kontrolü başarılı olsa da tam envanter kabulü sayılmaz.

2026-10-09'da A16-11–A16-15 kontrolleri geçici sentetik SQLite veritabanında uçtan uca çalıştırıldı. İlk fixture `Complete=true` ve toplam **5** bulgu verdi; her yeni kontrol tam olarak bir beklenen örnek kimliğini buldu. A16-04 sınır düzeltmesinden sonraki genişletilmiş fixture `Complete=true`, toplam **7** bulgu verdi: A16-04 üç hatalı cari bağlantısı olan iki aracı tekil kimliklerle raporladı; A16-11–A16-15'in her biri beklenen bir örneği buldu. Diğer sabit sorgularda bulgu yoktu. Kaynak sentetik DB `ReadOnly` açıldı ve SHA-256 özeti tarama öncesi/sonrası aynı kaldı; gerçek müşteri verisine bağlanılmadı veya müdahale edilmedi.

Aynı gün sınır durumu incelemesinde A16-15'in banka hareketindeki `FirmaId IS NULL` değerini SQL `<>` karşılaştırmasının `UNKNOWN` sonucu nedeniyle kaçırabileceği bulundu. Sorgu, banka hareketi firması boş veya sıfır/negatifse de bulgu verecek şekilde düzeltildi. Diğer zorunlu tabloları içermeyen minimal sentetik SQLite şemasında A16-15 beklenen eşleştirme kimliğini buldu; bu sınırlı koşuda tüm envanterin `Complete=false` dönmesi beklenen sonuçtu.

A16-04'te de eski `INNER JOIN` carisi eksik olan araç bağlarını tamamen gizleyebiliyordu; ayrıca kiralık ve komisyoncu cari bağlantılarından ikisi birden sorunluysa tek araç birden fazla kez sayılabilirdi. Denetim ayrı `LEFT JOIN` ile her iki bağı da kontrol eder, sahipsiz/firmasız/geçersiz cari bağlantılarını bulur ve araç kimliğini tekilleştirir.

A16-05'te banka hareketinin `BankaHesapId` değeri boş, sıfır veya negatifse eski `> 0` filtresi bu hareketi sessizce atlayabiliyordu. Denetim artık geçersiz hesap kimliğini, bulunmayan hesabı ve boş/geçersiz firma bağlarını da raporlar.

2026-10-09 Release CLI kabulinde sentetik tam şemada hesap kimliği boş, `0`, `-4` ve mevcut olmayan `999` olan dört hareketin tamamı A16-05'te bulundu; geçerli hesaplı beşinci hareket temiz kaldı. Rapor `Complete=true`, `FindingCount=4`, A16-05 `Count=4`, kimlikler `101,102,103,104` ve çıkış kodu `0` verdi. Bu yalnız sentetik sınır testi olup müşteri verisi kabulü değildir.

Bu araç foreign key olarak tanımlanmamış tenant ilişkilerini, FK taraflarından birinde `FirmaId` bulunmayan ilişkileri, eski finans fişi toplamlarını, tarih semantiğini veya dosya referanslarını kapsamaz. **Gerçek müşteri verisi taranmadı ve onarım yapılmadı.** A-16'yı kapatmadan önce tüm iş ilişkilerinin kapsandığının şema ve kodla uzlaştırılması, bulguların yetkili kişiyle tek tek değerlendirilmesi, onarım/geri dönüş planı ve öncesi/sonrası raporları gerekir. A-15 migration ve eşzamanlılık kabulü de ayrıdır.
