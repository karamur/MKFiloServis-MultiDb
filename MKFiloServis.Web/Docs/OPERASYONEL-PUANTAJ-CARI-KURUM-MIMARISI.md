# Operasyonel Puantajda Cari Tarafından Kurum Alt Kırılımı Mimarisi

**Tarih**: 2025-01-23 (Taslak)  
**Durum**: Analiz & Tasarım Aşaması  
**Alan**: Operasyonel Puantaj / Raporlama / Veri Modellemesi

---

## 1. Mevcut Durum Analizi

### 1.1 İş Senaryosu
- **MKFiloServis**: Servis taşımacılık firmalarına operasyonel puantaj takibi ve hakediş yönetimi sunmaktadır.
- **Müşteri Yapısı**: Her firma, birden fazla müşteri kurumuyla (hükümet dairesi, özel şirket vb.) iş yapar.
  - *Örnek*: TRT Ankara (ana daire), TRT Ankara (Çiftçiler Pazarı şubesi), TRT Ankara (Keçiören Şubesi) → hepsi ayrı "Kurum" olabilir.
  - Ancak muhasebe sistemi için bir "Cari" olabilir: **TRT-ANKARA** (ana Cari).

### 1.2 Mevcut Entity İlişkileri
```
┌─────────────┐
│   Firma     │  (Sahibi olan firmaların bilgisi)
└──────┬──────┘
       │ 1-to-Many
       │
┌──────▼──────┐
│  Cari       │  (Müşteri/Tedarikçi/Personel hesapları)
└──────┬──────┘
       │ ?  (Şu anda Cari → Kurum y.ö. YOK)
       │
┌──────▼─────────────┐
│  Kurum (Müşteri)   │  (Spesifik kurum/daire)
└────────────────────┘
       │
       │ 1-to-Many
       │
┌──────▼──────────────────────┐
│ FiloGuzergahEslestirme       │  (Araç-Şoför-Güzergah template)
├────────────────────────────┤
│ - KurumFirmaId (FK)         │
│ - GuzergahId                │
│ - AracId                    │
│ - SoforId                   │
└────────────────┬────────────┘
                 │ 1-to-Many
                 │
        ┌────────▼───────────────────┐
        │ FiloGunlukPuantajlar (Daily)│
        ├─────────────────────────────┤
        │ - Tarih                     │
        │ - KurumFirmaId              │
        │ - GuzergahId                │
        │ - AracId                    │
        │ - SoforId                   │
        │ - ServisTuru                │
        │ - Durum                     │
        └─────────────────────────────┘
```

**Mevcut sorun**: Cari → Kurum ilişkisi DB seviyesinde explicit değil. İlişki yorumsal olarak yapılıyor (Cari'nin TamAdı="TRT ANKARA" ise Kurum'larda da benzer prefix olabilir).

### 1.3 Sayısal Tahmin (Veri Boyutu)
- Ön tahmin (orta ölçekli işletme): 
  - Cariler: 50-200 adet
  - Kurumlar: 150-500 adet (Cari başına ortalama 2-3)
  - Eşleştirmeler: 100-300 adet
  - Günlük Puantajlar: 10K-100K adet (yıllık; gün başına 30-300)

---

## 2. Hedef Yapı (Yeni Mimari)

### 2.1 Cari-Kurum-Puantaj Hiyerarşisi
```
BAŞLA: Cari (Müşteri) Filtresi
   │
   ├─ Cari: TRT-ANKARA (Id: 42)
   │   └─ Kurum Alt Kırılımları:
   │       ├─ TRT Ankara Merkez (KurumId: 150)
   │       │   ├─ Eşleştirmeler: 5 adet
   │       │   ├─ Aylık Puantaj: [Hücre Grid]
   │       │   └─ Toplam Gelir (Kurum): 15,000 TL
   │       │
   │       ├─ TRT Ankara Çiftçiler Pazarı (KurumId: 151)
   │       │   ├─ Eşleştirmeler: 3 adet
   │       │   ├─ Aylık Puantaj: [Hücre Grid]
   │       │   └─ Toplam Gelir (Kurum): 8,000 TL
   │       │
   │       └─ [Diğer Kurumlar...]
   │
   ├─ Cari: İSKİ (Id: 43)
   │   └─ Kurum Alt Kırılımları: ...
   │
   └─ [Diğer Cariler...]
```

### 2.2 Database Model Değişiklikleri

#### A. Cari Entity'sine Ekleme
```csharp
public class Cari
{
    // ... existing properties ...

    // Yeni: Inverse navigation for Kurum
    public virtual ICollection<Kurum> KurumListesi { get; set; } = new List<Kurum>();
}
```

#### B. Kurum Entity'si (FK zaten var, inverse nav eklenecek)
```csharp
public class Kurum
{
    public int? CariId { get; set; }  // FK to Cari (if not already named differently)
    public virtual Cari? Cari { get; set; }  // Inverse nav (NEW)

    // ... existing properties ...
}
```

**Not**: Kurum'da `FirmaId` var ama Cari ilişkisi eksik. Kurum'un parent Cari'si eksplisit olmalı.

#### C. FiloGuzergahEslestirme (Güçlendirilecek)
```csharp
public class FiloGuzergahEslestirme
{
    public int FirmaId { get; set; }

    // Mevcut: Kurum/Cari (doğrudan)
    public int KurumFirmaId { get; set; }  // FK to Cari
    public virtual Cari? MusteriCari { get; set; }

    // ÖNERİ: Explicit Kurum referansı
    public int? KurumId { get; set; }  // FK to Kurum (NEW)
    public virtual Kurum? Kurum { get; set; }  // (NEW)

    // ... existing properties ...
}
```

#### D. FiloGunlukPuantaj (Güçlendirilecek)
```csharp
public class FiloGunlukPuantaj
{
    public int FirmaId { get; set; }
    public DateTime Tarih { get; set; }

    // Mevcut: Kurum/Cari (doğrudan)
    public int KurumFirmaId { get; set; }  // FK to Cari

    // ÖNERİ: Explicit Kurum referansı
    public int? KurumId { get; set; }  // FK to Kurum (NEW)
    public virtual Kurum? Kurum { get; set; }  // (NEW)

    // ... existing properties ...
}
```

---

## 3. Sorgu & Raporlama Katmanı

### 3.1 IFiloKomisyonService Tarafına Yeni Metotlar

```csharp
public interface IFiloKomisyonService
{
    // Mevcut metotlar...

    /// <summary>
    /// Belirli bir Cari için tüm Kurum'ları ve altında eşleştirmeleri getirir.
    /// Hiyerarşioluş reporting için.
    /// </summary>
    Task<CariKurumHiyerarsiDto> GetCariKurumHiyerarsiAsync(int cariId);

    /// <summary>
    /// Belirli bir Kurum altında puantajları takvim grid'ine uygun hale döndürür.
    /// </summary>
    Task<List<KurumPuantajAylikDto>> GetKurumPuantajAylikAsync(int kurumId, int yil, int ay);

    /// <summary>
    /// Tüm Cariler için kümülâtif puantaj özetini döndürür.
    /// (Raporlama/Dashboard için)
    /// </summary>
    Task<List<CariPuantajOzetDto>> GetCarilarPuantajOzetiAsync(DateTime baslamaTarihi, DateTime bitisTarihi);
}
```

### 3.2 Query Pattern'leri

**Pattern 1: Cari ile başla, Kurum ve Eşleştirmeleri yükle**
```csharp
var query = _dbContext.Cariler
    .Where(c => c.Id == cariId)
    .Include(c => c.KurumListesi)
        .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
            .ThenInclude(e => e.Guzergah)
    .Include(c => c.KurumListesi)
        .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
            .ThenInclude(e => e.Arac)
    .Include(c => c.KurumListesi)
        .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
            .ThenInclude(e => e.Sofor);
```

**Pattern 2: Kurum başına Puantaj Güncelle**
```csharp
var puantajlar = await _dbContext.FiloGunlukPuantajlar
    .Where(p => p.KurumId == kurumId && p.Tarih >= baslama && p.Tarih <= bitis)
    .OrderBy(p => p.Tarih)
    .ToListAsync();
```

---

## 4. UI/UX Katmanı

### 4.1 Eşleştirme Ekranı Tasarımı (EslestirmeTanimlari.razor)

**Yeni Layout**:
```
┌─────────────────────────────────────────────────────┐
│ Araç, Şoför ve Güzergah Eşleme Havuzu (CARİ BAZLI) │
├─────────────────────────────────────────────────────┤
│
│  [Cari Seçimi: Dropdown/Autocomplete]
│
│  CARİ: TRT-ANKARA (Aktif Seçim)
│  ┌──────────────────────────────────┐
│  │ Tab 1: Kurum Kırılımı             │ Tab 2: Eşleştirmeler (Klassik)  │
│  ├──────────────────────────────────┤
│  │
│  │  KURUM: TRT Ankara Merkez
│  │  ├─ Eşleştirme: Plaka45 / Şoför Ali / Rota-A
│  │  ├─ Aylık Puantaj Grid:
│  │  │   Pazartesi | Salı | Çarşamba | ... | TOPLAM (Kurum)
│  │  │   ...
│  │  │   [Gün bazlı hücreler editlenebilir]
│  │  │
│  │  │  [Kaydet] [Sil] [Rapor]
│  │  │
│  │  KURUM: TRT Ankara Çiftçiler Pazarı
│  │  ├─ Eşleştirme: Plaka77 / Şoför Veli / Rota-B
│  │  ├─ Aylık Puantaj Grid:
│  │  │   ... (benzer)
│  │  │
│  │  │  [Kaydet] [Sil] [Rapor]
│  │
│  └──────────────────────────────────┘
│
│  CARİ TOPLAM: 23,000 TL (tüm Kurum'lar toplamı)
│
└──────────────────────────────────────────────────────┘
```

### 4.2 Bileşen Yapısı
- **CariSelector.razor**: Cari seçim dropdown'ı
- **KurumAkkordiyonu.razor**: Kurum bazında collapsible panel
- **PuantajGridByKurum.razor**: Kurum altında gün grid'i
- **CariPuantajOzeti.razor**: Tüm Cariler özeti tablosu

---

## 5. Veri Migrasyonu Stratejisi

### 5.1 Adımlar
1. **Yayın öncesi**: Mevcut Cari-Kurum ilişkisini kontrol et
   - Her Kurum'un parent Cari'si belir (veri temizliği):
     ```sql
     SELECT k.*, c.*
     FROM Kurum k
     LEFT JOIN Cari c ON ...  -- Heuristic: k.KurumAdi LIKE c.Unvan+'%'
     WHERE c.Id IS NULL;  -- Orphan Kurum'lar
     ```

2. **Migration oluştur**:
   - Kurum'a `CariId` FK ekle
   - Mevcut Kurum'ları parent Cari'lerine bağla
   - Opsiyonel: FiloGuzergahEslestirme ve FiloGunlukPuantaj'a `KurumId` FK ekle

3. **Veri temizliği**:
   - Orphan Kurum'ları handle et (ya silinmesi ya da default Cari'ye atanması)
   - Duplike Kurum'ları tespit et

### 5.2 Code-First Migration Örneği
```csharp
public partial class AddCariKurumHierarchy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Kurum'a CariId FK'ı ekle
        migrationBuilder.AddColumn<int?>(
            name: "CariId",
            table: "Kurum",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Kurum_CariId",
            table: "Kurum",
            column: "CariId");

        migrationBuilder.AddForeignKey(
            name: "FK_Kurum_Cari_CariId",
            table: "Kurum",
            column: "CariId",
            principalTable: "Cari",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        // FiloGuzergahEslestirme'ye KurumId FK'ı ekle
        migrationBuilder.AddColumn<int?>(
            name: "KurumId",
            table: "FiloGuzergahEslestirmeleri",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_FiloGuzergahEslestirmeleri_KurumId",
            table: "FiloGuzergahEslestirmeleri",
            column: "KurumId");

        migrationBuilder.AddForeignKey(
            name: "FK_FiloGuzergahEslestirmeleri_Kurum_KurumId",
            table: "FiloGuzergahEslestirmeleri",
            column: "KurumId",
            principalTable: "Kurum",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        // Veri Migrasyonu: KurumFirmaId → CariId eşlemesi
        migrationBuilder.Sql(@"
            UPDATE k
            SET k.CariId = c.Id
            FROM Kurum k
            INNER JOIN Cari c ON c.Id = k.KurumFirmaId OR 
                                 (c.Unvan LIKE k.KurumAdi + '%' AND c.CariTipi = 1)
            WHERE k.CariId IS NULL;
        ");

        // KurumId dolumu (Eşleştirmeler için)
        migrationBuilder.Sql(@"
            UPDATE e
            SET e.KurumId = e.KurumFirmaId
            FROM FiloGuzergahEslestirmeleri e
            WHERE e.KurumId IS NULL;
        ");
    }
}
```

---

## 6. Raporlama & Analytics

### 6.1 Standart Raporlar
1. **Cari Seviyesi Puantaj Özeti**
   - Cari adı, puantaj dönem, toplam sefer, toplam gelir, toplam maliyet

2. **Kurum Bazında Detay**
   - Cari → Kurum → Eşleştirme → Günlük puantaj

3. **Eşleştirme Analizi**
   - Hangi Araç/Şoför kombinasyonu hangi Kurum'a atanmış, verimlilik

4. **İstatistiksel Dashboard**
   - Cari başına ortalama sefer sayısı, günlük verimlilik, taşeron maliyeti yüzde

### 6.2 DTO Örnekleri
```csharp
public class CariKurumHiyerarsiDto
{
    public int CariId { get; set; }
    public string CariUnvan { get; set; }
    public List<KurumDetayDto> Kurumlar { get; set; }
    public decimal CariToplami { get; set; }
}

public class KurumDetayDto
{
    public int KurumId { get; set; }
    public string KurumAdi { get; set; }
    public List<EslestirmeDetayDto> Eslestirmeler { get; set; }
    public decimal KurumToplami { get; set; }
}

public class EslestirmeDetayDto
{
    public int EslestirmeId { get; set; }
    public string Plaka { get; set; }
    public string SoforAdi { get; set; }
    public string GuzerigahAdi { get; set; }
    public List<PuantajGunuDto> GunlerHavuzu { get; set; }
}
```

---

## 7. Performans & Optimizasyon

### 7.1 Index Strategy
```csharp
// Kurum
modelBuilder.Entity<Kurum>()
    .HasIndex(k => k.CariId)
    .HasName("IX_Kurum_CariId");

// FiloGuzergahEslestirme
modelBuilder.Entity<FiloGuzergahEslestirme>()
    .HasIndex(e => new { e.KurumId, e.IsActive })
    .HasName("IX_FiloGuzergahEslestirmeleri_KurumId_IsActive");

// FiloGunlukPuantaj (Cari+Kurum+Tarih range)
modelBuilder.Entity<FiloGunlukPuantaj>()
    .HasIndex(p => new { p.KurumId, p.Tarih })
    .HasName("IX_FiloGunlukPuantajlar_KurumId_Tarih");
```

### 7.2 Caching Önerisi
- Cari → Kurum listi (CAR_{cariId}_KURUMLAR): TTL 1 gün
- Kurum → Eşleştirmeler (KUR_{kurumId}_ESLESTIRMELER): TTL 1 gün
- Aylık Puantaj Özeti (PUA_{kurumId}_{yil}{ay}): TTL 3 gün

---

## 8. Uyarlama Yol Haritası

### Faz 1: Database Model (1-2 gün)
- [ ] Cari & Kurum entity'lerine navigasyon prop ekleme
- [ ] Migration oluşturma (FK'lar, data migration)
- [ ] Veri doğrulama ve temizliği

### Faz 2: Servis & Repository (2-3 gün)
- [ ] IFiloKomisyonService yeni metotlarını tanımla
- [ ] FiloKomisyonService implementasyon (sorgu optimize)
- [ ] Birim testleri

### Faz 3: UI Bileşenleri (3-4 gün)
- [ ] CariSelector.razor
- [ ] KurumAkkordiyonu.razor
- [ ] PuantajGridByKurum.razor
- [ ] Entegre edilmiş EslestirmeTanimlari.razor (yeni layout)

### Faz 4: Raporlama & Iyileştirmeler (2-3 gün)
- [ ] CariPuantajOzeti.razor (dashboard)
- [ ] Rapor generation servis
- [ ] Cache layer entegrasyonu
- [ ] QA & Performans test

**Toplam**: ~2 hafta (parallel çalışmayla 1 hafta mümkün)

---

## 9. Riskler & Dikkat Noktaları

| Risk | Seviyesi | Azaltma Stratejisi |
|------|----------|-------------------|
| Mevcut veri inconsistency | **YÜKSEK** | Veri audit ve temizlik faz1'de yapılmalı |
| Performans (n+1 query) | **ORTA** | EF Include/ThenInclude optimize, test ile doğrula |
| Geriye uyumluluk ve downtime | **ORTA** | Gradual rollout, feature flag veya parallel run |
| Raporlama logic değişimi | **ORTA** | Eski raporlar yanında yeni hiyerarşik rapor |
| Migration rollback gerek | **DÜŞ** | Migration script tersine döne bilir; data backup |

---

## 10. Sonuç & Tavsiyeler

**Sonuç**: Cari → Kurum hiyerarşisinin explicit hale getirilmesi, operasyonel puantajda **raporlama clarity'sini ve business intelligence'ı önemli ölçüde iyileştirmeye** sunmuştur.

**Tavsiyeler**:
1. ✅ **Öneri 1**: Hemen Faz 1 başla; veri audit ile başla.
2. ✅ **Öneri 2**: Feature flag kullanarak faz 3'te gradual UI rollout yap.
3. ✅ **Öneri 3**: Mevcut Excel/raporlama akışlarını bu yeni hiyerarşiye migrate etmek için timeline planla.
4. ✅ **Öneri 4**: Kurum/Cari masterdata kurallarını dokumentasyona ekle (veri giriş kılavuzu).

---

**Hazırlayan**: Assistant  
**Son Güncelleme**: 2025-01-23  
**Versiyon**: 1.0 (Taslak)
