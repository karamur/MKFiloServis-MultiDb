# A-16 — Eski veri için salt okunur ön envanter

**Tarih:** 2026-10-08  
**Durum:** 🟡 Alt araç hazır; A-16 genel görevi 🔴 açık.

## Amaç ve kapsam

`MKFiloServis.DataSync inventory` veritabanını değiştirmeden 10 sabit kontrolü ve şemadan keşfedilen tenant ilişkisi adaylarını sayar; her kontrol için en fazla 20 kayıt kimliği verir:

| Kod | Denetim |
|---|---|
| A16-01 | Aktif araçta eksik firma bağı |
| A16-02 | Firma içinde yinelenen aktif şase |
| A16-03 | Firma içinde yinelenen güncel plaka |
| A16-04 | Araçtaki kiralık/komisyoncu carisinin farklı firmada olması |
| A16-05 | Banka hareketi ve hesabının firma uyuşmazlığı veya hesabın yokluğu |
| A16-06 | Fatura ve carisinin firma uyuşmazlığı veya carinin yokluğu |
| A16-07 | Firma/personel/dönem maaş snapshot tekrarı |
| A16-08 | Araç/dönem maliyet snapshot tekrarı |
| A16-09 | Firmada birden çok aktif varsayılan fatura şablonu |
| A16-10 | Firma/kullanıcı için birden çok varsayılan fatura grup şablonu |

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

`Complete=true`, 10 sabit sorgunun, ilgili sağlayıcının foreign key ihlal kontrollerinin ve keşfedilebilen basit tenant FK kontrollerinin çalıştığını gösterir; `FindingCount=0` yalnız raporda yer alan kontroller için temiz sonuçtur. Tablo/kolon eksikse ilgili kontrol `schema_missing`, SQL hatası varsa `scan_error` olur, rapor `Complete=false` döner ve komut başarısız çıkar. Çıktı yolu SQLite kaynak dosyasıyla aynı olamaz. Rapor geçici dosyada hazırlanıp nihai ada taşınır.

## Doğrulama ve kapanış sınırı

2026-10-07'de sentetik SQLite ve ayrı geçici PostgreSQL 17 kümesinde sabit 10 örnek sorun üretildi. Her iki rapor `Complete=true`, `FindingCount=10` verdi. Eksik şemalı SQLite örneği `Complete=false` verdi. Kaynak DB ile aynı rapor yolu reddedildi ve SQLite dosya başlığı korundu. 2026-10-08'de Release derlemesi **0 uyarı/0 hata** ile geçti. Sentetik SQLite kopyasında tekli/bileşik `FirmaId` uyuşmazlıkları `A16-TENANT-*` ile, sahipsiz tekli/bileşik FK'ler `A16-SQLITE-FK-*` ile bulundu. Geçici PostgreSQL 17 kümesinde sahipsiz tekli/bileşik FK kayıtları `A16-PG-FK-*` kontrollerinde **1'er**, composite tenant uyuşmazlığı da `A16-TENANT-*` ile **1** bulgu verdi. Deneme şemasında uygulamanın sabit 10 tablosu bulunmadığı için rapor `Complete=false` ve komut çıkışı 3 verdi; sentetik FK kontrolü başarılı olsa da tam envanter kabulü sayılmaz.

Bu araç foreign key olarak tanımlanmamış tenant ilişkilerini, FK taraflarından birinde `FirmaId` bulunmayan ilişkileri, eski finans fişi toplamlarını, tarih semantiğini veya dosya referanslarını kapsamaz. **Gerçek müşteri verisi taranmadı ve onarım yapılmadı.** A-16'yı kapatmadan önce tüm iş ilişkilerinin kapsandığının şema ve kodla uzlaştırılması, bulguların yetkili kişiyle tek tek değerlendirilmesi, onarım/geri dönüş planı ve öncesi/sonrası raporları gerekir. A-15 migration ve eşzamanlılık kabulü de ayrıdır.
