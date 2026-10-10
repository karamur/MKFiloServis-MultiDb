# A-15 — Banka hareketi firma bağı izole doğrulaması

**Tarih:** 2026-10-06  
**Kaynak:** `20261006200000_GuardBankMovementTenantLinks` migration'ındaki PostgreSQL ve SQLite SQL metinleri.  
**Ortam:** SQLite bellek veritabanı ve yalnız `127.0.0.1:55437` üzerinde çalışan ayrı PostgreSQL 17 geçici kümesi. Müşteri veritabanına bağlanılmadı. PostgreSQL sunucusu doğrulamadan sonra durduruldu; küme dosyaları Git tarafından yok sayılan `setup/output/a15-pg-cluster` altında kaldı.

## Sonuç

| Senaryo | SQLite | PostgreSQL |
|---|---|---|
| Migration öncesi farklı firma hesabına bağlı eski hareketi reddetme | 🟢 | 🟢 |
| Aynı firma hesabı ve carisiyle hareket ekleme | 🟢 | 🟢 |
| Farklı firma hesabı, carisi veya geri ödeme hesabıyla hareket eklemeyi reddetme | 🟢 | 🟢 |
| Mevcut hareketin hesabını başka firma hesabına değiştirmeyi reddetme | 🟢 | 🟢 |
| Hareketi hesap/cariyle birlikte başka firmaya taşımayı reddetme | 🟢 | 🟢 |
| Hareketle ilişkili banka hesabı veya carinin firmasını değiştirmeyi reddetme | 🟢 | 🟢 |

Her sağlayıcıda bir geçerli yazım, bir eski veri ön kontrol reddi ve yedi tetikleyici reddi görüldü. Doğrulama programı teslim dışı `setup/output/a15-db-trigger-probe` altındadır. Son migration değişikliğinden sonra Web Debug derlemesi `setup/output/a15-db-tenant-guard-build` konumuna **0 uyarı, 0 hata** ile tamamlandı.

SQLite senaryoları ayrıca `MKFiloServis.Tests/BankMovementTenantMigrationTests.cs` içinde kalıcılaştırıldı; Release test çalışmasında **9/9** geçti. PostgreSQL kanıtı yukarıdaki geçici küme çalışmasıyla sınırlıdır.

## Sınır

Bu çalışma migration SQL'ini **asgari tablo şeması** üzerinde uyguladı. Gerçek uygulamanın tüm migration zinciri, mevcut müşteri verisi, audit tetikleyicileriyle birlikte çalışma, yüksek hacim ve eşzamanlı işlem yarışı bu kanıtın kapsamı dışındadır. Bu belge 2026-10-06 tarihli ara kanıttır; 2026-10-09 kullanıcı kapsam kararıyla A-15 kod teslimi kapatılmıştır. Müşteri DB ve canlı eşzamanlılık kabulü dağıtım operasyonunda kalır, görev rengini açık tutmaz.
