# Cari→Kurum Hiyerarşi Migration Planı ve Uygulanış Rehberi

**Tarih**: 2025-01-23  
**Sürüm**: 1.0 (Code-First Migration Stratejisi)  
**Kapsam**: Database Schema Güncellemesi & Veri Migrasyonu

---

## 1. Mevcut Durum Snapshot'ı

### 1.1 Mevcut Schema (Basitleştirilmiş)
```sql
-- Mevcut Tablolar
CREATE TABLE Cari (
    Id INT PRIMARY KEY,
    Unvan NVARCHAR(250),
    CariKodu NVARCHAR(50),
    FirmaId INT NULL,
    -- YOK: KurumListesi Relationship
);

CREATE TABLE Kurum (
    Id INT PRIMARY KEY,
    KurumKodu NVARCHAR(50),
    KurumAdi NVARCHAR(250),
    FirmaId INT NULL,
    -- YOK: CariId FK
    -- YOK: FiloGuzergahEslestirmeleri Collection
);

CREATE TABLE FiloGuzergahEslestirmeleri (
    Id INT PRIMARY KEY,
    FirmaId INT,
    KurumFirmaId INT FK→Cari.Id,
    GuzergahId INT,
    AracId INT,
    SoforId INT,
    -- YOK: KurumId FK
    -- YOK: Kurum Navigation
);

CREATE TABLE FiloGunlukPuantajlar (
    Id INT PRIMARY KEY,
    FirmaId INT,
    Tarih DATETIME,
    KurumFirmaId INT FK→Cari.Id,
    GuzergahId INT,
    AracId INT,
    SoforId INT,
    -- YOK: KurumId FK
    -- YOK: Kurum Navigation
);
```

### 1.2 Veri Varsayımları
- **Cariler**: ~50-200 aktif kayıt
- **Kurumlar**: ~150-500 kayıt (Cari başına 2-3 ortalama)
- **Orphan Kurumlar** (parent Cari'si yok): ~5-10% (temizlenecek)
- **FiloGunlukPuantajlar**: ~50K-100K kayıt (yıllık)

---

## 2. Migration Estratejisi

### 2.1 Adımlar (Sequence)

#### **Faz A: Schema Değişiklikleri (Non-Breaking)**
1. Kurum'a `CariId` sütununu ekle (NULLABLE)
2. FiloGuzergahEslestirme'ye `KurumId` sütununu ekle (NULLABLE)
3. FiloGunlukPuantaj'a `KurumId` sütununu ekle (NULLABLE)
4. Index'leri ekle (IX_Kurum_CariId, IX_FiloGuzergahEslestirmeleri_KurumId, vb.)
5. Foreign key constraintleri ekle (NULLABLE, SetNull on delete)

#### **Faz B: Veri Migrasyonu**
1. Orphan Kurum'ları tespit et
2. Kurum'ları parent Cari'lerine bağla (CariId doldur)
3. FiloGuzergahEslestirme'lerin KurumId'lerini doldur
4. FiloGunlukPuantaj'ların KurumId'lerini doldur

#### **Faz C: Veri Temizliği & Validasyon**
1. Consistency check'leri çalıştır
2. Data audit raporu genereate et
3. Rollback vs. commit planı hazırla

---

## 3. Migration Kodu (EF Core Code-First)

### 3.1 Migration Oluştur
```bash
# Visual Studio Package Manager Console veya CLI
Add-Migration AddCariKurumHierarchy
# veya
dotnet ef migrations add AddCariKurumHierarchy
```

### 3.2 Migration Sınıfı

**File**: `MKFiloServis.Web/Data/Migrations/20250123_AddCariKurumHierarchy.cs`

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc/>
    public partial class AddCariKurumHierarchy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // FAZA 1: Schema Değişiklikleri (Non-Breaking)
            // =====================================================

            // Step 1.1: Kurum → CariId FK ekle
            migrationBuilder.AddColumn<int?>(
                name: "CariId",
                table: "Kurum",
                type: "int",
                nullable: true,
                comment: "Parent Cari reference (FK). Kurum alt kırılım hiyerarşi için.");

            // Step 1.2: FiloGuzergahEslestirmeleri → KurumId FK ekle
            migrationBuilder.AddColumn<int?>(
                name: "KurumId",
                table: "FiloGuzergahEslestirmeleri",
                type: "int",
                nullable: true,
                comment: "Explicit Kurum reference. KurumFirmaId'den hareketle doldurulacak.");

            // Step 1.3: FiloGunlukPuantajlar → KurumId FK ekle
            migrationBuilder.AddColumn<int?>(
                name: "KurumId",
                table: "FiloGunlukPuantajlar",
                type: "int",
                nullable: true,
                comment: "Explicit Kurum reference. KurumFirmaId'den hareketle doldurulacak.");

            // =====================================================
            // FAZA 2: Index'ler Ekle (Sorgu Performansı)
            // =====================================================

            // Index 2.1: Kurum.CariId
            migrationBuilder.CreateIndex(
                name: "IX_Kurum_CariId",
                table: "Kurum",
                column: "CariId");

            // Index 2.2: FiloGuzergahEslestirmeleri.KurumId + IsActive
            migrationBuilder.CreateIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KurumId_IsActive",
                table: "FiloGuzergahEslestirmeleri",
                columns: new[] { "KurumId", "IsActive" });

            // Index 2.3: FiloGunlukPuantajlar.KurumId + Tarih
            migrationBuilder.Sql(@"
                CREATE NONCLUSTERED INDEX IX_FiloGunlukPuantajlar_KurumId_Tarih
                ON FiloGunlukPuantajlar (KurumId, Tarih)
                INCLUDE (AracId, SoforId, GuzergahId, Durum);
            ");

            // =====================================================
            // FAZA 3: Veri Migrasyonu (Doldurma)
            // =====================================================

            // Step 3.1: Kurum.CariId'yi doldur
            // Heuristic: KurumFirmaId'den Cari'ye doğrudan link
            migrationBuilder.Sql(@"
                -- Stajı: Orphan Kurum'ları tanımla
                -- KurumFirmaId'nin Cari.Id'sine doğrudan eşlenmesi
                UPDATE k
                SET k.CariId = k.KurumFirmaId
                FROM Kurum k
                WHERE k.CariId IS NULL AND k.KurumFirmaId IS NOT NULL;

                -- Bağlantı kontrol: Eğer hala orphan varsa, log edelim
                -- (Opsiyonel: Exception throw veya manuel cleanup)
            ");

            // Step 3.2: FiloGuzergahEslestirmeleri.KurumId'yi doldur
            // Heuristic: KurumFirmaId'den parent Kurum'u bul
            migrationBuilder.Sql(@"
                -- Her Eşleştirme için, KurumFirmaId'si olan Cari'nin
                -- hangi Kurum'larının olduğunu bul ve birincisini ata.
                -- (Varsayım: GenellikleKurumFirmaId = Cari, Kurum'lar bu Cari'nin altında)
                UPDATE e
                SET e.KurumId = k.Id
                FROM FiloGuzergahEslestirmeleri e
                INNER JOIN Kurum k ON k.CariId = e.KurumFirmaId
                WHERE e.KurumId IS NULL
                AND k.IsActive = 1  -- Aktif Kurum'lar tercih
                AND (SELECT COUNT(*) FROM Kurum WHERE CariId = e.KurumFirmaId AND IsActive = 1) = 1;

                -- Eğer bir Cari'nin birden fazla Kurum'u varsa ve seçilmemişse,
                -- ilk aktif Kurum'u seç.
                UPDATE e
                SET e.KurumId = (
                    SELECT TOP 1 k.Id 
                    FROM Kurum k 
                    WHERE k.CariId = e.KurumFirmaId AND k.IsActive = 1
                    ORDER BY k.Id
                )
                FROM FiloGuzergahEslestirmeleri e
                WHERE e.KurumId IS NULL AND e.KurumFirmaId IS NOT NULL;
            ");

            // Step 3.3: FiloGunlukPuantajlar.KurumId'yi doldur
            migrationBuilder.Sql(@"
                -- Her Puantaj kaydı için, eşleşen Eşleştirme'yi bulup KurumId'ni ata
                UPDATE p
                SET p.KurumId = COALESCE(
                    (SELECT TOP 1 e.KurumId 
                     FROM FiloGuzergahEslestirmeleri e 
                     WHERE e.Id = p.FiloGuzergahEslestirmeId),
                    k.Id
                )
                FROM FiloGunlukPuantajlar p
                LEFT JOIN Kurum k ON k.CariId = p.KurumFirmaId AND k.IsActive = 1
                WHERE p.KurumId IS NULL;
            ");

            // =====================================================
            // FAZA 4: Foreign Key Constraintleri Ekle
            // =====================================================

            // FK 4.1: Kurum.CariId → Cari.Id
            migrationBuilder.AddForeignKey(
                name: "FK_Kurum_Cari_CariId",
                table: "Kurum",
                column: "CariId",
                principalTable: "Cari",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // FK 4.2: FiloGuzergahEslestirmeleri.KurumId → Kurum.Id
            migrationBuilder.AddForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Kurum_KurumId",
                table: "FiloGuzergahEslestirmeleri",
                column: "KurumId",
                principalTable: "Kurum",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // FK 4.3: FiloGunlukPuantajlar.KurumId → Kurum.Id
            migrationBuilder.AddForeignKey(
                name: "FK_FiloGunlukPuantajlar_Kurum_KurumId",
                table: "FiloGunlukPuantajlar",
                column: "KurumId",
                principalTable: "Kurum",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // =====================================================
            // OPSIYONEL: Veri Validasyon & Audit
            // =====================================================

            // Check 5.1: İstatistiksel rapor
            migrationBuilder.Sql(@"
                -- INSERT INTO MigrationAudit (MigrationType, Details)
                -- SELECT 
                --     'AddCariKurumHierarchy',
                --     CONCAT(
                --         'Kurum: ', (SELECT COUNT(*) FROM Kurum WHERE CariId IS NOT NULL),
                --         ', Eslestirmeler: ', (SELECT COUNT(*) FROM FiloGuzergahEslestirmeleri WHERE KurumId IS NOT NULL),
                --         ', Puantajlar: ', (SELECT COUNT(*) FROM FiloGunlukPuantajlar WHERE KurumId IS NOT NULL)
                --     );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // Rollback Sırası (Tersine)
            // =====================================================

            // R1: Foreign Keys Kaldır
            migrationBuilder.DropForeignKey(
                name: "FK_Kurum_Cari_CariId",
                table: "Kurum");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGuzergahEslestirmeleri_Kurum_KurumId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_FiloGunlukPuantajlar_Kurum_KurumId",
                table: "FiloGunlukPuantajlar");

            // R2: Index'ler Kaldır
            migrationBuilder.DropIndex(
                name: "IX_Kurum_CariId",
                table: "Kurum");

            migrationBuilder.DropIndex(
                name: "IX_FiloGuzergahEslestirmeleri_KurumId_IsActive",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_FiloGunlukPuantajlar_KurumId_Tarih ON FiloGunlukPuantajlar;
            ");

            // R3: Sütunlar Kaldır
            migrationBuilder.DropColumn(
                name: "CariId",
                table: "Kurum");

            migrationBuilder.DropColumn(
                name: "KurumId",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropColumn(
                name: "KurumId",
                table: "FiloGunlukPuantajlar");
        }
    }
}
```

---

## 4. EF Core Entity Yapılandırması

### 4.1 ApplicationDbContext Güncellemesi

**File**: `MKFiloServis.Web/Data/ApplicationDbContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ========== CARİ - KURUM İLİŞKİSİ ==========

    // Cari.KurumListesi (1-to-Many)
    modelBuilder.Entity<Cari>()
        .HasMany(c => c.KurumListesi)
        .WithOne(k => k.Cari)
        .HasForeignKey(k => k.CariId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.SetNull)
        .HasConstraintName("FK_Kurum_Cari_CariId");

    // KURUM - FİLO EŞLEŞTIRME İLİŞKİSİ
    modelBuilder.Entity<Kurum>()
        .HasMany(k => k.FiloGuzergahEslestirmeleri)
        .WithOne(e => e.Kurum)
        .HasForeignKey(e => e.KurumId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.SetNull)
        .HasConstraintName("FK_FiloGuzergahEslestirmeleri_Kurum_KurumId");

    // KURUM - GÜNLÜK PUANTAJ İLİŞKİSİ
    modelBuilder.Entity<Kurum>()
        .Property(k => k.KurumAdi)
        .HasMaxLength(250)
        .IsRequired();

    // ========== INDEXLEME STRATEJİSİ ==========

    // Index 1: Kurum lookup
    modelBuilder.Entity<Kurum>()
        .HasIndex(k => k.CariId)
        .HasName("IX_Kurum_CariId");

    // Index 2: Eşleştirmeler (active filter)
    modelBuilder.Entity<FiloGuzergahEslestirme>()
        .HasIndex(e => new { e.KurumId, e.IsActive })
        .HasName("IX_FiloGuzergahEslestirmeleri_KurumId_IsActive");

    // Index 3: Puantaj (tarih range)
    modelBuilder.Entity<FiloGunlukPuantaj>()
        .HasIndex(p => new { p.KurumId, p.Tarih })
        .HasName("IX_FiloGunlukPuantajlar_KurumId_Tarih");

    // ========== DİĞER KONFIGURASYONLAR (DEĞİŞMEYEN) ==========
    // ... (existing configurations)
}
```

---

## 5. Veri Temizliği & Validasyon Scriptleri

### 5.1 Pre-Migration Audit

```sql
-- Script: PRE_MIGRATION_AUDIT.sql
-- Çalıştır: Migration'dan ÖNCE

-- Rapor 1: Orphan Kurum'ları (CariId boş olanlar)
SELECT COUNT(*) as OrphanKurumCount
FROM Kurum k
WHERE k.KurumFirmaId IS NULL OR k.KurumFirmaId NOT IN (SELECT Id FROM Cari);

-- Rapor 2: Cari başına Kurum dağılımı
SELECT c.Unvan, COUNT(k.Id) as KurumSayisi
FROM Cari c
LEFT JOIN Kurum k ON k.KurumFirmaId = c.Id
GROUP BY c.Id, c.Unvan
ORDER BY KurumSayisi DESC;

-- Rapor 3: Eşleştirmeler durumu
SELECT 
    COUNT(*) as TotalEslestirme,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) as AktifEslestirme,
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) as PasifEslestirme
FROM FiloGuzergahEslestirmeleri;

-- Rapor 4: Puantaj veri boyutu
SELECT 
    COUNT(*) as TotalPuantaj,
    MIN(Tarih) as EarliestDate,
    MAX(Tarih) as LatestDate,
    DATEDIFF(DAY, MIN(Tarih), MAX(Tarih)) as DaysSpan
FROM FiloGunlukPuantajlar;
```

### 5.2 Post-Migration Validation

```sql
-- Script: POST_MIGRATION_VALIDATION.sql
-- Çalıştır: Migration'dan SONRA

-- Check 1: Orphan Kurum'lar (hala varsa)
SELECT k.Id, k.KurumAdi, k.KurumFirmaId
FROM Kurum k
WHERE k.CariId IS NULL;

-- Check 2: Eşleştirmeler KurumId doldurulmuş mı
SELECT 
    COUNT(*) as Total,
    SUM(CASE WHEN KurumId IS NOT NULL THEN 1 ELSE 0 END) as WithKurumId,
    SUM(CASE WHEN KurumId IS NULL THEN 1 ELSE 0 END) as WithoutKurumId
FROM FiloGuzergahEslestirmeleri;

-- Check 3: Puantajlar KurumId doldurulmuş mı
SELECT 
    COUNT(*) as Total,
    SUM(CASE WHEN KurumId IS NOT NULL THEN 1 ELSE 0 END) as WithKurumId,
    SUM(CASE WHEN KurumId IS NULL THEN 1 ELSE 0 END) as WithoutKurumId
FROM FiloGunlukPuantajlar;

-- Check 4: Referential Integrity
SELECT 
    'FK Violation: FiloGuzergahEslestirmeleri.KurumId' as Issue,
    COUNT(*) as Count
FROM FiloGuzergahEslestirmeleri e
WHERE e.KurumId IS NOT NULL AND e.KurumId NOT IN (SELECT Id FROM Kurum)
UNION ALL
SELECT 
    'FK Violation: FiloGunlukPuantajlar.KurumId',
    COUNT(*)
FROM FiloGunlukPuantajlar p
WHERE p.KurumId IS NOT NULL AND p.KurumId NOT IN (SELECT Id FROM Kurum);

-- Check 5: İstatistiksel Özet
SELECT 
    'Cari' as Entity,
    COUNT(*) as RecordCount,
    COUNT(DISTINCT FirmaId) as UniqueCompanies
FROM Cari
UNION ALL
SELECT 
    'Kurum',
    COUNT(*),
    COUNT(DISTINCT FirmaId)
FROM Kurum
UNION ALL
SELECT 
    'FiloGuzergahEslestirmeleri',
    COUNT(*),
    COUNT(DISTINCT FirmaId)
FROM FiloGuzergahEslestirmeleri
UNION ALL
SELECT 
    'FiloGunlukPuantajlar',
    COUNT(*),
    COUNT(DISTINCT FirmaId)
FROM FiloGunlukPuantajlar;
```

---

## 6. Uygulanış Adımları (Operasyon)

### 6.1 Production Deployment Çizelgesi

**Zaman**: T+0 (Migration Deploy Saati)

| Saat | Adım | Sorumluluk | Not |
|------|------|-----------|-----|
| T+0 | Veritabanı Backup | DBA | Full backup + transaction log |
| T+5 | Pre-Audit Scripts çalıştır | DBA | Rapor alınır, göreceli numaralar doğrulanır |
| T+10 | Migration çalıştır (UP) | Backend Lead | `Update-Database` veya `dotnet ef database update` |
| T+15 | Post-Validation Scripts çalıştır | DBA | FK violations, orphan check |
| T+20 | Code Deploy (Servis Güncellemesi) | DevOps | Yeni IFiloKomisyonService metotları aktif |
| T+30 | Smoke Tests | QA | Temel operasyonları test et |
| T+45 | Gradual Rollout (Feature Flag) | Backend | UI'lar stage-by-stage aktif |
| T+60+ | Monitoring & Logs | Ops | Hata kontrol, performance tracking |

### 6.2 Rollback Planı

Eğer kritik hata bulunursa:

```powershell
# Command: Rollback Migration
Update-Database -Migration 20250123_AddCariKurumHierarchy -TargetMigration 20250123_AddCariKurumHierarchy -Revert

# veya CLI:
# dotnet ef database update 20250122_PreviousMigration
```

**Rollback Süresi**: ~2-5 dakika (bağımlılık olmayan schema revert)

### 6.3 Monitoring & Alerting

```yaml
# Application Insights / Azure Monitor Kuralları
- Query Warnings: IF IX_Kurum_CariId or IX_FiloGuzergahEslestirmeleri_KurumId_IsActive scan'ler 
  10ms'yi aşarsa alert
- NULL Count: IF KurumId IS NULL örüntüsü % 5'i aşarsa alert
- Performance: IF puantaj query'leri öncesine kıyasla 30% yavaşlarsa alert
```

---

## 7. Kod Değişiklikleri (Entity Definitions)

### 7.1 Cari.cs Güncellemesi
```csharp
public class Cari : BaseEntity, IKopyalanabilirTenant, IFirmaTenant
{
    // ... existing properties ...

    // ✅ YENİ: Navigation property
    /// <summary>
    /// Bu Cari'ye bağlı tüm Kurum'lar (1-to-Many).
    /// Hiyerarşik raporlama ve filtreleme için.
    /// </summary>
    public virtual ICollection<Kurum> KurumListesi { get; set; } = new List<Kurum>();
}
```

### 7.2 Kurum.cs Güncellemesi
```csharp
public class Kurum : BaseEntity, IKopyalanabilirTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    // ✅ YENİ: Parent Cari (FK)
    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }

    [Required]
    [StringLength(50)]
    public string KurumKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string KurumAdi { get; set; } = string.Empty;

    // ... existing properties ...

    // ✅ YENİ: Inverse navigation (Eşleştirmeler)
    public virtual ICollection<FiloGuzergahEslestirme> FiloGuzergahEslestirmeleri { get; set; } 
        = new List<FiloGuzergahEslestirme>();
}
```

### 7.3 FiloGuzergahEslestirme.cs Güncellemesi
```csharp
public class FiloGuzergahEslestirme : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }

    [Required]
    public int KurumFirmaId { get; set; }  // FK to Cari (backward compat)

    // ✅ YENİ: Explicit Kurum FK
    public int? KurumId { get; set; }

    [Required]
    public int GuzergahId { get; set; }

    [Required]
    public int AracId { get; set; }

    [Required]
    public int SoforId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(KurumFirmaId))]
    public virtual Cari? MusteriCari { get; set; }

    [ForeignKey(nameof(KurumId))]
    public virtual Kurum? Kurum { get; set; }  // ✅ YENİ

    // ... existing navigationnav properties ...
}
```

### 7.4 FiloGunlukPuantaj.cs Güncellemesi
```csharp
public class FiloGunlukPuantaj : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }

    [Required]
    public DateTime Tarih { get; set; }

    public int? FiloGuzergahEslestirmeId { get; set; }

    [Required]
    public int KurumFirmaId { get; set; }  // FK to Cari (backward compat)

    // ✅ YENİ: Explicit Kurum FK
    public int? KurumId { get; set; }

    [Required]
    public int GuzergahId { get; set; }

    [Required]
    public int AracId { get; set; }

    [Required]
    public int SoforId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(KurumFirmaId))]
    public virtual Cari? MusteriCari { get; set; }

    [ForeignKey(nameof(KurumId))]
    public virtual Kurum? Kurum { get; set; }  // ✅ YENİ

    // ... existing navigation properties ...
}
```

---

## 8. Veri Migrasyonu Sonrası Validasyon Raporu Şablonu

```markdown
# Post-Migration Validation Report

**Migration**: AddCariKurumHierarchy  
**Executed At**: [TIMESTAMP]  
**Executed By**: [USER]

## Summary
- Total Cari: [N]
- Total Kurum: [N] (Orphan: [M])
- Total Eslestirmeler: [N] (with KurumId: [M])
- Total Puantajlar: [N] (with KurumId: [M])

## Validations Passed
- [ ] No orphan Kurum (CariId NULL)
- [ ] All FiloGuzergahEslestirmeleri linked to Kurum
- [ ] All FiloGunlukPuantajlar have KurumId (where applicable)
- [ ] No referential integrity violations
- [ ] Indexes created successfully
- [ ] Foreign keys enforced

## Performance Baseline
- Query: `SELECT * FROM Kurum WHERE CariId = ?` → [TIME]ms
- Query: `SELECT * FROM FiloGuzergahEslestirmeleri WHERE KurumId = ?` → [TIME]ms
- Query: `SELECT * FROM FiloGunlukPuantajlar WHERE KurumId = ? AND Tarih BETWEEN ? AND ?` → [TIME]ms

## Issues (if any)
- [None recorded]

## Sign-off
- DBA: _________________________ Date: _________
- Backend Lead: _________________________ Date: _________
- DevOps: _________________________ Date: _________
```

---

## Özet Checklist

- [ ] Migration dosyası oluştur ve test et (local DB'de)
- [ ] Pre-migration audit script'ini çalıştır (staging/prod)
- [ ] Cari.cs, Kurum.cs, FiloGuzergahEslestirme.cs, FiloGunlukPuantaj.cs güncelle
- [ ] ApplicationDbContext.cs Fluent API ekle
- [ ] Migration Up() ve Down() metotları test et
- [ ] Post-migration validation script'ini hazırla
- [ ] Production rollback planını onaylat
- [ ] Monitoring/alerting kurallarını ayarla
- [ ] Deployment görev listesini döküm et
- [ ] QA smoke test plan'ı hazırla

---

**Versiyon**: 1.0 (Code-First Migration Plan)  
**Son Güncelleme**: 2025-01-23  
**Kategori**: Database | Schema Versioning | EF Core Migrations
