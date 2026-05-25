# ADR-0005: Multi-Tenant Database-Per-Firma Mimarisi

## Status
Accepted (2026-05-25)

## Context
Proje 3 firma ile çalışıyor. Önceki "Shared Database + FirmaId row-level isolation" mimarisinde kullanıcıların hatalı firma seçimi veya filter kaçağı durumunda firmaların verileri birbirine karışabiliyordu.

İş ihtiyacı:
- Firma verileri fiziksel olarak izole olmalı
- Bir firmanın verisi diğer firmadan görülmemeli
- Holding konsolidasyonu için çapraz firma raporlama yapılabilmeli
- Yedekleme/restore firma bazlı olmalı
- KVKK uyumluluğu sağlanmalı

## Decision
**Hybrid model: Database-Per-Firma + Shared Master DB**

1. **Master DB** (`KOAFiloServis_Master`): Sadece global tablolar (Firmalar, Kullanicilar, Roller, Lisanslar)
2. **Tenant DB'ler** (`Koa_[FirmaKodu]_[ID]`): Her firmanın kendi DB'si, tam fiziksel izolasyon
3. **Holding DB** (`KOAFiloServis_Holding`): Konsolide raporlama (read model, eventual consistency)
4. **Geçiş stratejisi**: `Firma.DatabaseName == null` → shared DB, `!= null` → tenant DB (kademeli)

**Interface**: `IFirmaTenant` marker interface. `ApplyFirmaTenantQueryFilter` ile otomatik global query filter.

**DbContext Factory**: `TenantDbContextFactory` → `IDbContextFactory<ApplicationDbContext>`. Connection string'i `Firma.DatabaseName`'e göre dinamik çözer.

## Consequences

**Avantajlar:**
- Fiziksel izolasyon: bir firmanın verisi diğer firmanın DB'sinde yok
- KVKK uyumlu: firma verisi ayrı DB'de
- Firma bazlı yedekleme/restore
- Kademeli geçiş: eski shared DB firmaları etkilenmez

**Riskler:**
- Cross-tenant sorgu (TumFirmalar) tenant DB'de çalışmaz → Holding DB ile çözüldü
- Migration yönetimi: her tenant DB'ye ayrı migration → `TenantDatabaseService.BaselineMigrationsAsync`
- Connection pooling: her tenant DB için ayrı pool → `ConcurrentDictionary` cache

## Alternatives Considered
1. **Shared DB + row-level security**: Reddedildi — fiziksel izolasyon yok, KVKK riski
2. **Schema-per-tenant**: Reddedildi — PostgreSQL'de schema yönetimi karmaşık
3. **Tamamen ayrı DB (Master DB yok)**: Reddedildi — kullanıcı/lisans yönetimi için ortak katman gerekli

## Related Components
- `IFirmaTenant.cs` (Shared/Entities)
- `Firma.cs` (+DatabaseName alanı)
- `MasterDbContext.cs` (Web/Data)
- `TenantDbContextFactory.cs` (Web/Data)
- `TenantDatabaseService.cs` (Web/Services)
- `HoldingDbContext.cs` (Web/Data)
- `ApplicationDbContext.cs` (ApplyFirmaTenantQueryFilter)
- Tüm entity'ler: IFirmaTenant implementasyonu

## Migration Impact
- `Firmalar` tablosu: `DatabaseName` kolonu eklendi
- Master DB: 6 çekirdek tablo, raw SQL ile oluşturuldu
- Tenant DB: EnsureCreated + BaselineMigrationsAsync
- Entity'ler: IFirmaTenant implementasyonu (FirmaId alanı)

## Verification Checklist
- [x] Tenant DB'de sadece ilgili firmanın verisi görünür
- [x] Firma geçişi: UI'da firma değişince veri değişir
- [x] Master DB: kullanıcı girişi, lisans kontrolü çalışır
- [x] Holding DB: konsolide raporlar
- [x] Migration: tüm tenant DB'lere otomatik uygulanır
- [x] Build: 0 hata, 0 uyarı
- [x] Test: 305/305 başarılı
