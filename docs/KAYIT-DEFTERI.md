# 📒 KOAFiloServis — Kayıt Defteri

> Bu dosya, geliştirme sürecinde alınan kararları, yapılan tartışmaları ve hazırlanan raporları
> kronolojik olarak kayıt altına alır. Her oturum sonunda güncellenir.

---

## 📅 20.05.2026 — Database-Per-Firma Faz 1 Başlangıç Oturumu

### Yapılanlar

| # | İş | Detay |
|---|-----|-------|
| 1 | `AIAsistan.razor` kaldırıldı | Derleme hatası olan sayfa tamamen silindi, `NavMenu.razor`'daki link kaldırıldı |
| 2 | Build doğrulandı | `dotnet build` → 0 hata, 5 uyarı (önceden mevcut) |
| 3 | Mimari keşif tamamlandı | ApplicationDbContext, TenantAwareDbContextFactory, IAktifFirmaProvider, IFirmaTenant, Firma entity, Program.cs DI, appsettings.json incelendi |
| 4 | Faz planı hazırlandı | 6 fazlı Database-Per-Firma geçiş planı (Faz 0-6) oluşturuldu |
| 5 | Kayıt defteri formatı belirlendi | Tarih+amaç, commit, değişen dosyalar, değişiklik özeti, test/build sonucu, risk/açık işler, faz durumu, karar+gerekçe |

### 🏗️ MİMARİ KARAR — Faz 1 Yaklaşımı

**Karar:** Hybrid model ile kademeli geçiş.

**Gerekçe:**
- `Firma.DatabaseName == null` → eski shared DB modu (mevcut çalışan sistem)
- `Firma.DatabaseName != null` → yeni tenant DB modu (tam izolasyon)
- Aynı `ApplicationDbContext` her iki modda da çalışır — tenant query filter shared modda izolasyon sağlar, dedicated modda zararsız no-op olur
- `MasterDbContext` sadece global tabloları (Firmalar, Kullanicilar, Lisans) yönetir

### 📋 Faz 1 Uygulama Planı (Özet)

| Sıra | Adım | Dosya |
|:----:|------|-------|
| 1 | `Firma` entity'sine `DatabaseName` ekle | `Shared/Entities/Firma.cs` |
| 2 | `ITenantConnectionStringProvider` arayüzü | `Web/Services/` (YENİ) |
| 3 | `TenantConnectionStringProvider` implementasyonu | `Web/Services/` (YENİ) |
| 4 | `MasterDbContext` oluştur | `Web/Data/` (YENİ) |
| 5 | `ApplicationDbContext`'ten master tabloları çıkar | `Web/Data/ApplicationDbContext.cs` |
| 6 | `TenantDbContextFactory` (eskisini replace et) | `Web/Data/` (YENİ + SİL) |
| 7 | `appsettings.json`'a `MasterConnection` ekle | `Web/appsettings.json` |
| 8 | `Program.cs` DI kayıtlarını güncelle | `Web/Program.cs` |
| 9 | `FirmaService` + master tablo servislerini güncelle | `Web/Services/` |
| 10 | Migration klasörlerini düzenle | `Data/Migrations/` |
| 11 | `DbInitializer` güncelle | `Web/Data/DbInitializer.cs` |
| 12 | Build + smoke test | — |

### ⚠️ Riskler / Açık İşler

| Risk | Durum |
|------|:-----:|
| V1 (70+ entity) vs V2 (24 entity) kararı — V2 öneriliyor | 🔴 Karar bekliyor |
| `LisansService` singleton ama MasterDbContext scoped | Çözüm: `IDbContextFactory<MasterDbContext>` |
| Cross-tenant (TumFirmalar) sorgular dedicated DB'de çalışmaz | Faz 1'de sadece shared-DB firmalar için destek |
| Pooling per-tenant DB'ler için optimize değil | Faz 2'de `ConcurrentDictionary` cache |

### 🎯 Sonraki Adım

Faz 1 Adım 1: `Firma` entity'sine `DatabaseName` alanı eklenmesi.

---

## 📅 20.05.2026 — Faz 1 Uygulama Oturumu (İkinci Kısım)

### Commit: `cba5d90`
```
feat(multi-db): Faz 1 altyapi - Database-Per-Firma hybrid mimari kurulumu
```

### ✅ Faz 1 — Tamamlanan Adımlar

| # | Adım | Değişen Dosyalar | Özet |
|---|------|-----------------|------|
| 1 | `Firma` + `AktifFirmaBilgisi` güncelle | `Shared/Entities/Firma.cs`, `Web/Services/FirmaService.cs` | `DatabaseName` alanı eklendi, `AktifFirmaBilgisi`'ne taşındı |
| 2 | `ITenantConnectionStringProvider` + implementasyon | `Web/Services/ITenantConnectionStringProvider.cs` (YENİ), `Web/Services/TenantConnectionStringProvider.cs` (YENİ) | Dinamik connection string çözümleyici: `DatabaseName == null` → shared DB, `!= null` → tenant DB |
| 3 | `MasterDbContext` oluştur | `Web/Data/MasterDbContext.cs` (YENİ) | 6 çekirdek global tablo: Firmalar, Kullanicilar, Lisanslar, Roller, RolYetkileri, AppAyarlari. Navigation property temizliği yapıldı |
| 4 | ApplicationDbContext temizliği | — | **ERTELENDİ:** FK kırılma riski nedeniyle tüm entity'ler korundu. Faz 4'te yapılacak |
| 5 | `TenantDbContextFactory` (eskisini replace) | `Web/Data/TenantDbContextFactory.cs` (YENİ), `TenantAwareDbContextFactory.cs` (SİL) | `ITenantConnectionStringProvider` ile dinamik DB bağlantısı, pooling Faz 2'de |
| 6-7 | `appsettings.json` + `Program.cs` DI | `Web/appsettings.json`, `Web/Program.cs` | `MasterConnection` eklendi (şimdilik shared DB ile aynı), `TenantDatabase` bölümü eklendi, DI kayıtları güncellendi, `PooledDbContextFactoryHolder` kaldırıldı |
| 8 | Master tablo servisleri güncelle | `Web/Services/LisansService.cs`, `Web/Services/FirmaService.cs` | `LisansService`: `PooledDbContextFactoryHolder` → `IDbContextFactory<MasterDbContext>`, `FirmaService`: `IDbContextFactory<ApplicationDbContext>` → `IDbContextFactory<MasterDbContext>` |
| 9 | Migration | `Web/Migrations/..._MultiDbFaz1_AddFirmaDatabaseName.cs` (YENİ) | `Firmalar` tablosuna `DatabaseName` kolonu eklendi (varchar(100), nullable) |
| 10 | Build doğrulama | — | `dotnet build` → **0 hata, 5 uyarı** ✅ |

### 🏗️ MİMARİ KARAR — MasterDbContext Migration Ertelendi

**Karar:** MasterDbContext migration'ı Faz 2'ye ertelendi. Faz 1'de Master DB fiziksel olarak ayrılmadı, `MasterConnection` şimdilik DefaultConnection ile aynı shared DB'yi gösteriyor.

**Gerekçe:** MasterDbContext entity keşfi (navigation property cascade) 41+ tablo oluşturmaya çalıştı. MasterDbContext'in sadece 6 core tabloyla sınırlanması için kapsamlı `Ignore<>()` konfigürasyonu gerekiyor. Bu iş Faz 2'de Master DB fiziksel ayrımıyla birlikte yapılacak.

### 📊 Faz 1 Durumu: 🟡 KISMEN TAMAMLANDI

**Tamamlanan:** Altyapı (entity, provider, factory, DI, migration) kuruldu, build temiz.
**Ertelenen:** ApplicationDbContext master tablo temizliği, MasterDbContext migration'ı, DbInitializer güncellemesi.

### 🧪 Runtime Smoke Test (20.05.2026 - 3. Kısım)

| Kontrol | Sonuç |
|---------|:-----:|
| `dotnet ef database update` — migration uygulama | ✅ `MultiDbFaz1_AddFirmaDatabaseName` başarıyla uygulandı |
| `dotnet run` — uygulama başlatma | ✅ `Now listening on: http://0.0.0.0:5190` |
| Startup görevleri (Seed, Quartz, GPS) | ✅ Tümü başarılı |
| Uygulama log'larında hata/exception | ✅ **0 hata** |
| `GET /` — giriş sayfası | ✅ HTTP 200, "Giris - Koa Filo Servis" |
| `GET /login` — login sayfası | ✅ HTTP 200 |

**Sonuç:** ✅ Uygulama sorunsuz başlıyor, login sayfası açılıyor, hiçbir hata yok.

### ⚠️ Güncel Riskler

| Risk | Durum |
|------|:-----:|
| V1 (70+ entity) vs V2 (24 entity) kararı | 🔴 Karar bekliyor |
| MasterDbContext migration cascade | 🟡 Faz 2'de çözülecek |
| Runtime smoke test | ✅ **TAMAMLANDI** — uygulama başlıyor, login açılıyor, 0 hata |
| `KullaniciService` + auth servisleri hala ApplicationDbContext kullanıyor | 🟡 Faz 2'de güncellenecek |
| Cross-tenant (TumFirmalar) dedicated DB'de çalışmaz | 🟡 Faz 2'de ele alınacak |

---

## 📅 14.05.2026 — AI Asistan + Mimari Karar Oturumu

### Commit: `952a546`
```
feat(ai-asistan): DeepSeek V3/R1 model katalogu + docs guncelleme
```

---

### 🤖 AI Asistan Model Kataloğu

**Konu:** DeepSeek V4 yapay zeka listesine eklenebilir mi?

**Araştırma Sonucu:**
- Ollama public registry'de `deepseek-v4` tag'i **mevcut değil** (14.05.2026 tarihi itibarıyla)
- Mevcut resmi DeepSeek sürümleri:
  - `deepseek-v3` — Genel amaçlı, güçlü model
  - `deepseek-r1` — Reasoning (akıl yürütme) modeli
  - `deepseek-coder-v2` — Kod odaklı model

**Yapılan Değişiklik:**
- `AIAsistan.razor` → `GetBirlesikModelListesi()` metoduna `deepseek-v3` ve `deepseek-r1` eklendi
- Dropdown artık iki grup gösteriyor:
  - **Yerel (Ollama):** Makinede `ollama pull` ile yüklenmiş modeller
  - **Önerilen (yüklü değil):** Katalogdaki ama henüz indirilmemiş modeller
- Yüklü olmayan model seçilince `ollama pull <model>` komutu ipucu olarak gösteriliyor

**Dosya:** `KOAFiloServis.Web/Components/Pages/Ayarlar/AIAsistan.razor`

---

### 🏗️ MİMARİ KARAR — Database Per Firma

#### Sorun Tanımı
Müşteri 3 firma ile çalışıyor. Mevcut "Shared Database + FirmaId row-level isolation"
mimarisinde kullanıcıların hatalı firma seçimi veya filter kaçağı durumunda firmaların
verileri birbirine karışabiliyor. Zirve Müşavirlik gibi referans sistemlerde her firma
ayrı veritabanında çalışıyor.

#### Mevcut Mimari (Shared DB)
```
PostgreSQL: DestekCRMServisBlazorDb (TEK DB)
  Araclar   → FirmaId=1, FirmaId=2, FirmaId=3  (hepsi aynı tabloda)
  Cariler   → FirmaId=1, FirmaId=2, FirmaId=3
  Faturalar → FirmaId=1, FirmaId=2, FirmaId=3

Koruma mekanizması: HasQueryFilter("FirmaId == aktifFirma")
Zayıf nokta: Kullanıcı hatalı firma seçimi → yanlış veri görme/yazma riski
```

#### Hedef Mimari (Database Per Firma)
```
PostgreSQL Server
  db_global   → Kullanıcılar, Lisans, Firma katalogu
  db_firma_1  → Firma A'nın TÜM verileri (tam izolasyon)
  db_firma_2  → Firma B'nın TÜM verileri (tam izolasyon)
  db_firma_3  → Firma C'nın TÜM verileri (tam izolasyon)
  db_holding  → Ortak/Holding konsolidasyon DB
```

#### Gerekçe
1. **Veri güvenliği:** DB seviyesinde fiziksel izolasyon — filter bypass imkansız
2. **Müşteri talebi:** Zirve Müşavirlik benzeri yapı isteniyor
3. **Holding ihtiyacı:** 3 firmayı birleştiren ortak raporlama / bütçe konsolidasyonu
4. **Yedekleme:** Firma bazlı backup/restore kolaylaşır
5. **KVKK/Hukuki:** Firma verisi fiziksel olarak ayrı

---

### 🏢 HOLDİNG / ORTAK FİRMA MODÜLÜ

#### Kavram
3 (veya daha fazla) operasyonel firmanın finansal verilerini **özetleyerek** tek bir
"Holding" veritabanında konsolide eden yeni modül.

#### Holding'e Ne Aktarılır?
| Veri Türü | Aktarılır | Not |
|-----------|:---------:|-----|
| Bütçe gerçekleşmesi | ✅ | Gelir/gider toplam |
| Fatura toplamları | ✅ | KDV dahil/hariç |
| Banka/Kasa bakiyesi | ✅ | Dönem sonu snapshot |
| Personel gider özeti | ✅ | Bordro toplamı |
| Araç maliyet özeti | ✅ | Bakım+yakıt toplam |
| Hakediş ödemeleri | ✅ | Tedarikçi toplamı |
| Tekil fatura detayı | ❌ | Gizlilik/boyut |
| Personel özlük | ❌ | KVKK |
| Cari kart detayı | ❌ | Firma içi bilgi |

#### Holding Rapor Türleri
- Firma Karşılaştırma (Gelir/Gider/Kâr yan yana)
- Bütçe Konsolidasyonu (tüm firmalar toplam)
- Ödeme Planı (tüm firmaların vadesi gelenler)
- Araç Maliyet Özeti (firma bazlı)
- Personel Gider Özeti (firma bazlı bordro toplam)
- Hakediş Özeti (tedarikçi ödemeleri)

---

### 🔀 GITHUB — YENİ REPO KARARI

#### Soru
> "GitHub'da bu yapıya dokunmadan yeni proje gibi açıp projeyi oraya kopyalayıp
> oradan devam etsek olur mu?"

#### Yanıt ve Değerlendirme
**Evet, tamamen uygulanabilir.** İki yöntem var:

---

**Yöntem 1 — Fork (Önerilen)**
```bash
# GitHub web arayüzünde:
# 1. https://github.com/karamur/KOAFiloServis → "Fork" butonu
# 2. Yeni repo adı: KOAFiloServis-v2  (veya KOAFiloServis-MultiDb)
# 3. Sadece main branch'i fork et

# Yerel:
git clone https://github.com/karamur/KOAFiloServis-v2
cd KOAFiloServis-v2
# Upstream'i orijinal repo olarak ekle (gelecekte senkronizasyon için)
git remote add upstream https://github.com/karamur/KOAFiloServis
```
✅ Orijinal repo **aynen korunur** (production backup)
✅ Yeni repoda Database-Per-Firma geçişi yapılır
✅ İleride orijinale patch geri alınabilir (`git cherry-pick`)

---

**Yöntem 2 — Yeni Boş Repo + Kopyalama**
```bash
# GitHub'da yeni repo oluştur: KOAFiloServis-v2

# Yerel — mevcut kodu yeni remote'a bağla:
cd "C:\Users\muratk\Desktop\d yedek\calisma\Claude-Code\KOAFiloServis"
git remote add v2 https://github.com/karamur/KOAFiloServis-v2
git push v2 main

# Yeni çalışma klasörü:
cd C:\Users\muratk\Desktop\d yedek\calisma\
git clone https://github.com/karamur/KOAFiloServis-v2 KOAFiloServis-v2
```
✅ Temiz başlangıç
⚠️ Commit geçmişi taşınır (arzu edilmezse `--depth 1` veya squash)

---

**Önerilen Akış:**
```
karamur/KOAFiloServis        → Mevcut production kodu (dokunulmaz, korunur)
karamur/KOAFiloServis-MultiDb → Yeni Database-Per-Firma mimarisi geliştirme
```

Geçiş tamamlanıp test edilince `KOAFiloServis-MultiDb` → `KOAFiloServis`'e merge edilir
veya doğrudan production'a alınır.

---

### 📋 Uygulama Fazları (Özet)

| Faz | İçerik | Tahmini Süre |
|-----|--------|:---:|
| **Faz 1** | GlobalDbContext + TenantDbContext ayrımı + ITenantDbResolver | 3–4 gün |
| **Faz 2** | Mevcut veri göçü (3 firma DB'sine taşıma) | 1–2 gün |
| **Faz 3** | Holding modülü + konsolidasyon raporları | 3–4 gün |
| **Faz 4** | IFirmaTenant + FirmaId temizliği | 1–2 gün |
| **Test** | Stabilizasyon | 2–3 gün |
| **TOPLAM** | | **~10–15 gün** |

---

### ⚠️ Riskler

| Risk | Önlem |
|------|-------|
| Veri göçü sırasında kayıp | Tam backup → row count doğrulama |
| FirmaKopyalama çoklu DB'de kırılma | Önce refactor, sonra göç |
| Migration yönetimi karmaşıklığı | Tek TenantDbContext migration path |
| Holding raporu performansı | Task.WhenAll ile paralel DB sorgusu |

---

### 🎯 Sonraki Adım (Onay Bekliyor)

Yeni repo açma ve Faz 1'e başlama kararı alınırsa:
1. GitHub'da `KOAFiloServis-MultiDb` reposu oluştur
2. Mevcut kodu oraya kopyala (`git push v2 main`)
3. `GlobalDbContext` ve `TenantDbContext` dosyalarını oluştur
4. `ITenantDbResolver` interface + implementasyonunu yaz
5. `appsettings.json`'a `TenantDb:Template` bölümü ekle

---

## 📚 İlgili Dosyalar

| Dosya | Konu |
|-------|------|
| `docs/TENANT_MIGRATION_PLAN.md` | Mevcut tenant migrasyonu (tamamlanmış) |
| `docs/CALISMA-NOTLARI-2026-05-13.md` | Önceki oturum notları |
| `docs/CALISMA-NOTLARI-2026-05-14.md` | Bu oturum notları |
| `docs/OTURUM_NOTLARI_2026-05-19.md` | Tenant v1.0.21 tamamlandı notu |
| `KOAFiloServis.Shared/Entities/IFirmaTenant.cs` | Mevcut tenant interface |
| `KOAFiloServis.Web/Data/TenantAwareDbContextFactory.cs` | Mevcut factory |
| `KOAFiloServis.Web/Services/IAktifFirmaProvider.cs` | Aktif firma state servisi |
