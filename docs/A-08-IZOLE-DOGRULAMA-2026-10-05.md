# A-08 — İzole doğrulama kanıtı

**Tarih:** 2026-10-05. Test verisi ile, canlı/müşteri veritabanından bağımsız çalıştırıldı.

## Kaynak kimliği

- PostgreSQL installer SHA-256: `62d00cc9b7dec39294ad686823b07909664c97cb978d300496d128800bd2b22b`
- Ortak C# audit motoru SHA-256: `ac53d0782e8a1a8cc155994a8461540bb161f2fb68769e97c0f24075a4d00e74`
- PostgreSQL 17 geçici cluster: yalnız 127.0.0.1 üzerinde rastgele port; ayrı GUID adlı DB. Test DB'si silindi ve geçici sunucu durduruldu.
- SQLite: bellek ve geçici dosya DB'leri; .NET 10 kontrol programı, Npgsql 10.0.2 / Microsoft.Data.Sqlite 10.0.9 / SQLitePCLRaw.lib.e_sqlite3 2.1.13.
- Kontrol programı çalışma alanına test projesi eklemeden `%TEMP%/MKFiloServis-audit-probe-046e8a00db9e412aa05d9fbdf24cf216` altında çalıştırıldı. Son başarılı çalıştırma çıkış kodu **0**.

## Son başarılı kontrol sonucu

| Kontrol | Sonuç |
|---|---|
| SQLite doğrudan SQL ve firmasız sistem satırı | 🟢 |
| SQLite sır maskeleme, transaction veri/audit rollback | 🟢 |
| SQLite günlük UPDATE/DELETE koruması ve audit yokken UPDATE'in geri alınması | 🟢 |
| SQLite backup API restore, önceki veri/audit geri dönüş kopyası, yeniden kurulan tetikleyici ve dış başarı makbuzu | 🟢 |
| PostgreSQL SQL, firmasız sistem satırı, maskeleme, transaction veri/audit rollback | 🟢 |
| PostgreSQL replica modunda ALWAYS tetikleyici | 🟢 |
| PostgreSQL TRUNCATE satır sayısı ve rollback | 🟢 |
| PostgreSQL günlük silme koruması ve audit yokken UPDATE'in geri alınması | 🟢 |
| PostgreSQL gerçek custom pg_dump/pg_restore: veri geri geldi, mevcut audit geçmişi korundu, sonraki UPDATE denetlendi | 🟢 |
| PostgreSQL yeni migration tablosu / nested DO installer: ilk INSERT denetlendi, ApiKey maskelendi | 🟢 |
| PostgreSQL Npgsql binary COPY, replica transaction içinde audit ve ApiKey maskeleme | 🟢 |
| Dış restore başlangıç/başarı makbuzu üretimi | 🟢 |
| Restore PowerShell betiği parser kontrolü | 🟢 |

```text
PASS SQLite: direct SQL, system scope, redaction, rollback, append-only, audit failure blocks write
PASS SQLite backup API restore: data restored, prior data/audit recovery copy preserved, triggers retained, external completion receipt
PASS PostgreSQL custom dump/restore: data restored, audit history preserved, triggers retained, external completion receipt
PASS PostgreSQL migration/new-table gate: nested DO installer, first write captured, ApiKey masked
PASS PostgreSQL DataSync primitive: binary COPY in replica transaction audited with masked ApiKey
PASS PostgreSQL: direct SQL, system scope, redaction, rollback, replica ALWAYS, truncate, append-only, audit failure blocks write
Isolated PostgreSQL probe database removed
```

## Sınır

Bu kanıt audit/transaction motoru ve restore altyapısı içindir. Web yetki ekranı, gerçek müşterinin tüm migration zinciri, tam DataSync veri kümesi, büyük hacim, mali zincir ve bağımsız makine/key ring kurtarması test edilmiş sayılmaz. Bunlar A-04/A-09/A-18/A-19 kabul görevlerinde kalır. DB sahibi/superuser'ın DDL müdahalesine karşı kriptografik değişmezlik iddia edilmez.
