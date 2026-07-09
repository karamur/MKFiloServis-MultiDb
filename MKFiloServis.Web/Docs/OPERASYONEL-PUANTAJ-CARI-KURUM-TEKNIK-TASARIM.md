# Operasyonel Puantajda Cari→Kurum Hiyerarşi: Teknik Tasarım & Uygulama Detayları

**Tarih**: 2025-01-23  
**Durum**: Teknik Tasarım Dokümanı  
**Hedef Kitle**: Backend/Frontend Developers & Architects

---

## 1. Database Model Değişiklikleri (Entity Framework)

### 1.1 Cari Entity Güncellemesi

**Mevcut Durum**:
```csharp
public class Cari : BaseEntity, IKopyalanabilirTenant, IFirmaTenant
{
    // ... existing properties ...
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }
    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }
}
```

**Hedef Durum** (Yeni Navigasyon Eklemesi):
```csharp
public class Cari : BaseEntity, IKopyalanabilirTenant, IFirmaTenant
{
    // ... existing properties ...
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }
    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }

    // ✅ YENİ: Cari altındaki Kurum'lar
    public virtual ICollection<Kurum> KurumListesi { get; set; } = new List<Kurum>();
}
```

**Fluen API** (ApplicationDbContext içinde):
```csharp
modelBuilder.Entity<Cari>()
    .HasMany(c => c.KurumListesi)
    .WithOne(k => k.Cari)
    .HasForeignKey(k => k.CariId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);
```

---

### 1.2 Kurum Entity Güncellemesi

**Mevcut Durum**:
```csharp
public class Kurum : BaseEntity, IKopyalanabilirTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    [StringLength(50)]
    public string KurumKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string KurumAdi { get; set; } = string.Empty;

    // ... more properties ...
}
```

**Hedef Durum** (Cari FK + Inverse Nav):
```csharp
public class Kurum : BaseEntity, IKopyalanabilirTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    // ✅ YENİ: Parent Cari
    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }

    [Required]
    [StringLength(50)]
    public string KurumKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string KurumAdi { get; set; } = string.Empty;

    // ✅ YENİ: Inverse nav - Eşleştirmeler
    public virtual ICollection<FiloGuzergahEslestirme> FiloGuzergahEslestirmeleri { get; set; } 
        = new List<FiloGuzergahEslestirme>();

    // ... more properties ...
}
```

**Fluent API**:
```csharp
modelBuilder.Entity<Kurum>()
    .HasOne(k => k.Cari)
    .WithMany(c => c.KurumListesi)
    .HasForeignKey(k => k.CariId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);
```

---

### 1.3 FiloGuzergahEslestirme Entity Güncellemesi

**Mevcut Durum**:
```csharp
public class FiloGuzergahEslestirme : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }

    [Required]
    public int KurumFirmaId { get; set; }  // FK to Cari (müşteri)

    [Required]
    public int GuzergahId { get; set; }
    [Required]
    public int AracId { get; set; }
    [Required]
    public int SoforId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(KurumFirmaId))]
    public virtual Cari? MusteriCari { get; set; }
    // ... other navigations ...
}
```

**Hedef Durum** (Explicit Kurum FK):
```csharp
public class FiloGuzergahEslestirme : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }

    [Required]
    public int KurumFirmaId { get; set; }  // FK to Cari (müşteri) - KEEP for backward compat

    // ✅ YENİ: Explicit Kurum referansı
    public int? KurumId { get; set; }  // FK to Kurum

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
    public virtual Kurum? Kurum { get; set; }  // YENİ

    // ... other navigations ...
}
```

**Fluent API**:
```csharp
modelBuilder.Entity<FiloGuzergahEslestirme>()
    .HasOne(e => e.Kurum)
    .WithMany(k => k.FiloGuzergahEslestirmeleri)
    .HasForeignKey(e => e.KurumId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);
```

---

### 1.4 FiloGunlukPuantaj Entity Güncellemesi

**Mevcut Durum**:
```csharp
public class FiloGunlukPuantaj : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }

    [Required]
    public DateTime Tarih { get; set; }

    public int? FiloGuzergahEslestirmeId { get; set; }

    [Required]
    public int KurumFirmaId { get; set; }  // FK to Cari

    [Required]
    public int GuzergahId { get; set; }
    [Required]
    public int AracId { get; set; }
    [Required]
    public int SoforId { get; set; }

    public int? KullaniciId { get; set; }
    public OperasyonDurumu Durum { get; set; }
    public ServisTuru ServisTuru { get; set; }

    // ... other properties ...
}
```

**Hedef Durum** (Explicit Kurum FK):
```csharp
public class FiloGunlukPuantaj : BaseEntity, IFirmaTenant
{
    [Required]
    public int FirmaId { get; set; }

    [Required]
    public DateTime Tarih { get; set; }

    public int? FiloGuzergahEslestirmeId { get; set; }

    [Required]
    public int KurumFirmaId { get; set; }  // FK to Cari - KEEP for backward compat

    // ✅ YENİ: Explicit Kurum referansı
    public int? KurumId { get; set; }  // FK to Kurum

    [Required]
    public int GuzergahId { get; set; }
    [Required]
    public int AracId { get; set; }
    [Required]
    public int SoforId { get; set; }

    public int? KullaniciId { get; set; }
    public OperasyonDurumu Durum { get; set; }
    public ServisTuru ServisTuru { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(KurumFirmaId))]
    public virtual Cari? MusteriCari { get; set; }

    [ForeignKey(nameof(KurumId))]
    public virtual Kurum? Kurum { get; set; }  // YENİ

    // ... other navigations ...
}
```

---

## 2. Servis Katmanı (IFiloKomisyonService)

### 2.1 Yeni Interface Metotları

```csharp
public interface IFiloKomisyonService
{
    // EXISTING METHODS (unchanged)...
    Task<List<FiloGuzergahEslestirme>> GetEslestirmelerAsync();
    Task<List<FiloGunlukPuantaj>> CreatePuantajAsync(...);
    // ... etc ...

    // ✅ YENİ METOTLAR: Cari-Kurum Hiyerarşisi

    /// <summary>
    /// Belirtilen Cari için tüm Kurum'ları ve altında eşleştirmeleri hiyerarşik olarak döndürür.
    /// UI: Accordion/Tree view oluşturmak için.
    /// </summary>
    Task<CariKurumHiyerarsiDto> GetCariKurumHiyerarsiAsync(
        int cariId, 
        bool includeEslestirmeler = true,
        bool includeInactiveler = false);

    /// <summary>
    /// Belirtilen Kurum için tüm Eşleştirmeleri ve ilgili Günlük Puantaj kayıtlarını getirir.
    /// Tarih aralığı ile filtreleme yapılabilir.
    /// </summary>
    Task<KurumEslestirmePuantajDto> GetKurumEslestirmePuantajAsync(
        int kurumId,
        DateTime? baslama = null,
        DateTime? bitis = null);

    /// <summary>
    /// Tüm Cariler için kümülâtif puantaj ve operasyon özetini döndürür.
    /// Raporlama/Dashboard kullanımı.
    /// </summary>
    Task<List<CariPuantajOzetDto>> GetCarilarPuantajOzetiAsync(
        DateTime baslama,
        DateTime bitis,
        int? firmaId = null);

    /// <summary>
    /// Belirtilen Kurum altında tüm Eşleştirmelerin aylık puantaj grid'ini oluşturur.
    /// Gün bazlı editlemeler için kulanılır.
    /// </summary>
    Task<List<AylikPuantajGridDto>> GetKurumAylikPuantajGridAsync(
        int kurumId,
        int yil,
        int ay);

    /// <summary>
    /// Cari tarafından Kurum listesini getirir (basık, sadece lookup).
    /// </summary>
    Task<List<KurumBasitDto>> GetCariKurumlarSimpleAsync(int cariId);
}
```

---

### 2.2 Servis İmplementasyonu (Örnek Metotlar)

#### A. GetCariKurumHiyerarsiAsync
```csharp
public async Task<CariKurumHiyerarsiDto> GetCariKurumHiyerarsiAsync(
    int cariId,
    bool includeEslestirmeler = true,
    bool includeInactiveler = false)
{
    var query = _dbContext.Cariler
        .Where(c => c.Id == cariId)
        .Include(c => c.KurumListesi);

    if (!includeInactiveler)
    {
        query = query
            .Select(c => new Cari
            {
                Id = c.Id,
                Unvan = c.Unvan,
                CariKodu = c.CariKodu,
                Aktif = c.Aktif,
                KurumListesi = c.KurumListesi
                    .Where(k => k.IsActive.HasValue && k.IsActive.Value) // Kurum'da IsActive check
                    .ToList()
            });
    }

    if (includeEslestirmeler)
    {
        query = query
            .Include(c => c.KurumListesi)
                .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
                    .ThenInclude(e => e.Guzergah)
            .Include(c => c.KurumListesi)
                .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
                    .ThenInclude(e => e.Arac)
            .Include(c => c.KurumListesi)
                .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
                    .ThenInclude(e => e.Sofor);
    }

    var cari = await query.FirstOrDefaultAsync();

    if (cari == null)
        throw new ArgumentException($"Cari with ID {cariId} not found.");

    // DTO Mapping
    return new CariKurumHiyerarsiDto
    {
        CariId = cari.Id,
        CariUnvan = cari.Unvan,
        CariKodu = cari.CariKodu,
        Kurumlar = cari.KurumListesi.Select(k => new KurumDetayDto
        {
            KurumId = k.Id,
            KurumAdi = k.KurumAdi,
            KurumKodu = k.KurumKodu,
            Eslestirmeler = k.FiloGuzergahEslestirmeleri?.Select(e => new EslestirmeDetayDto
            {
                EslestirmeId = e.Id,
                Plaka = e.Arac?.AktifPlaka,
                SoforAdi = e.Sofor?.TamAd,
                GuzerigahAdi = e.Guzergah?.GuzergahAdi,
                IsActive = e.IsActive,
                KurumaUcret = e.KurumaKesilecekUcret,
                HedefeUcret = e.TaseronaOdenenUcret
            }).ToList() ?? new List<EslestirmeDetayDto>()
        }).ToList()
    };
}
```

#### B. GetCarilarPuantajOzetiAsync (Dashboard Raporu)
```csharp
public async Task<List<CariPuantajOzetDto>> GetCarilarPuantajOzetiAsync(
    DateTime baslama,
    DateTime bitis,
    int? firmaId = null)
{
    var query = _dbContext.Cariler.AsQueryable();

    if (firmaId.HasValue)
        query = query.Where(c => c.FirmaId == firmaId);

    var carilar = await query
        .Include(c => c.KurumListesi)
        .ToListAsync();

    var puantajlar = await _dbContext.FiloGunlukPuantajlar
        .Where(p => p.Tarih >= baslama && p.Tarih <= bitis)
        .Where(p => !firmaId.HasValue || p.FirmaId == firmaId)
        .ToListAsync();

    var result = carilar.Select(c => new CariPuantajOzetDto
    {
        CariId = c.Id,
        CariUnvan = c.Unvan,
        CariKodu = c.CariKodu,
        KurumSayisi = c.KurumListesi.Count,
        ToplamSefer = puantajlar
            .Where(p => p.KurumFirmaId == c.Id) // KurumFirmaId = CariId
            .Count(),
        ToplamGelir = puantajlar
            .Where(p => p.KurumFirmaId == c.Id)
            .Sum(p => p.BirimFiyat * p.SeferSayisi),
        ToplamMaliyet = puantajlar
            .Where(p => p.KurumFirmaId == c.Id)
            .Sum(p => p.TaseronaOdenen),
        BaslamaTarihi = baslama,
        BitisTarihi = bitis
    }).ToList();

    return result;
}
```

#### C. GetKurumAylikPuantajGridAsync (Gün-Bazlı Edit Grid)
```csharp
public async Task<List<AylikPuantajGridDto>> GetKurumAylikPuantajGridAsync(
    int kurumId,
    int yil,
    int ay)
{
    var puantajlar = await _dbContext.FiloGunlukPuantajlar
        .Where(p => p.KurumId == kurumId && 
                    p.Tarih.Year == yil && 
                    p.Tarih.Month == ay)
        .Include(p => p.Arac)
        .Include(p => p.Sofor)
        .Include(p => p.Guzergah)
        .ToListAsync();

    // Gruplanmış sonuç
    var grouped = puantajlar
        .GroupBy(p => new { p.AracId, p.SoforId })
        .Select(g => new AylikPuantajGridDto
        {
            SiradakiAracId = g.Key.AracId,
            SiradakiSoforId = g.Key.SoforId,
            Plaka = g.First().Arac?.AktifPlaka,
            SoforAdi = g.First().Sofor?.TamAd,
            OylukHucreler = g
                .OrderBy(p => p.Tarih)
                .Select((p, idx) => new GunHucreDto
                {
                    GunNo = p.Tarih.Day,
                    DayOfWeek = p.Tarih.DayOfWeek.ToString(),
                    PuantajId = p.Id,
                    Deger = p.SeferSayisi,
                    ServisTuru = p.ServisTuru,
                    Durum = p.Durum
                }).ToList()
        }).ToList();

    return grouped;
}
```

---

## 3. Query Pattern'leri & EF Core Optimizasyonları

### 3.1 Pattern: Cari-driven Loading
```csharp
// ✅ EFFİCİENT: Single query with proper includes
var cari = await _dbContext.Cariler
    .AsNoTracking()  // Sadece okuma ise
    .Where(c => c.Id == cariId)
    .Include(c => c.KurumListesi)
        .ThenInclude(k => k.FiloGuzergahEslestirmeleri
            .Where(e => e.IsActive))  // Filtered include
            .ThenInclude(e => e.Guzergah)
    .Include(c => c.KurumListesi)
        .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
            .ThenInclude(e => e.Arac)
                .ThenInclude(a => a.AktifPlakaKaydi)
    .Include(c => c.KurumListesi)
        .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
            .ThenInclude(e => e.Sofor)
    .FirstOrDefaultAsync();

// ❌ KÖTÜ: Multiple queries (n+1 problem)
var cari = await _dbContext.Cariler.FindAsync(cariId);
foreach (var kurum in cari.KurumListesi)
{
    var eslestirmeler = await _dbContext.FiloGuzergahEslestirmeleri
        .Where(e => e.KurumId == kurum.Id)
        .ToListAsync();  // Extra query per Kurum!
}
```

### 3.2 Pattern: Tarih Aralığı Filtrelemesi
```csharp
// ✅ Parametrize ve indexed sorgu
var basla = new DateTime(2025, 1, 1);
var bitis = new DateTime(2025, 1, 31).AddDays(1).AddSeconds(-1);

var puantajlar = await _dbContext.FiloGunlukPuantajlar
    .Where(p => p.KurumId == kurumId &&
                p.Tarih >= basla &&
                p.Tarih < bitis)  // Use < bitis + 1 day for better indexing
    .OrderBy(p => p.Tarih)
    .ThenBy(p => p.AracId)
    .ToListAsync();
```

### 3.3 Pattern: Aggregation (Dashboard)
```csharp
// ✅ DB'de sum/count yap (n öğe yerine 1 sonuç)
var ozetler = await _dbContext.FiloGunlukPuantajlar
    .Where(p => p.Tarih >= baslama && p.Tarih <= bitis)
    .GroupBy(p => p.KurumId)
    .Select(g => new
    {
        KurumId = g.Key,
        ToplamSefer = g.Count(),
        ToplamGelir = g.Sum(p => p.BirimFiyat * p.SeferSayisi),
        MinTarih = g.Min(p => p.Tarih),
        MaxTarih = g.Max(p => p.Tarih)
    })
    .ToListAsync();

// ❌ KÖTÜ: Tüm verileri yükle, LINQ-to-Objects'de sum
var puantajlar = await _dbContext.FiloGunlukPuantajlar.ToListAsync();
var ozetler = puantajlar
    .GroupBy(p => p.KurumId)
    .Select(g => new
    {
        KurumId = g.Key,
        ToplamSefer = g.Count(),  // Memory'de hesaplanıyor
        ToplamGelir = g.Sum(p => p.BirimFiyat * p.SeferSayisi)
    })
    .ToList();
```

---

## 4. UI/UX Bileşen Mimarisi

### 4.1 Blazor Bileşen Hiyerarşisi
```
EslestirmeTanimlari.razor (Master Page)
├── CariSelector.razor (Cari dropdown)
├── KurumAkkordiyonu.razor (Her Kurum için collapsible)
│   ├── PuantajGridByKurum.razor (Gün-bazlı grid)
│   │   ├── EditablePuantajHucre.razor (Gün hücresi)
│   │   └── PuantajIslemler.razor (Kaydet/Sil butonları)
│   └── EslestirmeListesi.razor (Araç/Şoför/Güzergah)
└── CariToplam.razor (Cari-level özet)

CariPuantajDashboard.razor (Dashboard/Rapor Sayfası)
├── BaslamaBitisTarih Picker
├── CariCarousel.razor (Cariler listesi)
└── CariDetayPaneli.razor (Seçili Cari detayı)
```

### 4.2 EslestirmeTanimlari.razor (Ana Ekran - Yeni Layout)

**Pseudo Code**:
```razor
@page "/filo/komisyon-eslestirme"
@using MKFiloServis.Web.Services.Interfaces

@inject IFiloKomisyonService FiloKomisyonService
@inject IToastService ToastService

<PageTitle>Araç, Şoför ve Güzergah Eşleme Havuzu (CARİ BAZLI)</PageTitle>

<div class="container-fluid">
    <h3><i class="bi bi-link-45deg me-2 text-primary"></i>Araç, Şoför ve Güzergah Eşleme Havuzu (Cari Kırılımlı)</h3>

    <!-- CARİ SEÇİMİ -->
    <div class="mb-3">
        <label>Cari Seçiniz:</label>
        <input type="text" 
               placeholder="Cari adı veya kod..." 
               @oninput="async (e) => await SearcimCariAsync((string)e.Value)" 
               class="form-control">

        @if (aranmisCarilar.Any())
        {
            <ul class="list-group">
                @foreach (var c in aranmisCarilar)
                {
                    <li class="list-group-item" @onclick="() => SecilmisCariDegisAsync(c.Id)">
                        @c.Unvan (@c.CariKodu)
                    </li>
                }
            </ul>
        }
    </div>

    <!-- SEÇİLİ CARİ: KURUM AKKORDIYONU -->
    @if (secilmisCari != null)
    {
        <div class="card">
            <div class="card-header">
                <strong>@secilmisCari.CariUnvan</strong> 
                <span class="text-muted">(@secilmisCari.Kurumlar.Count Kurum)</span>
            </div>
            <div class="card-body">

                @foreach (var kurum in secilmisCari.Kurumlar)
                {
                    <div class="card mb-2">
                        <div class="card-header" @onclick="() => ToggleKurumAsync(kurum.KurumId)" 
                             style="cursor: pointer;">
                            <strong>@kurum.KurumAdi</strong>
                            <span class="badge bg-info">@kurum.Eslestirmeler.Count Eşleştirme</span>
                            @if (expandedKurumId == kurum.KurumId)
                            {
                                <i class="bi bi-chevron-up float-end"></i>
                            }
                            else
                            {
                                <i class="bi bi-chevron-down float-end"></i>
                            }
                        </div>

                        @if (expandedKurumId == kurum.KurumId)
                        {
                            <div class="card-body">
                                <!-- PuantajGridByKurum Bileşeni Burada -->
                                <PuantajGridByKurum KurumId="@kurum.KurumId" 
                                                     Yil="@DateTime.Now.Year" 
                                                     Ay="@DateTime.Now.Month"
                                                     OnSave="@(async () => await RefreshYield())"
                                                     OnDelete="@(async () => await RefreshYield())" />

                                <div class="text-end mt-2">
                                    <strong>Kurum Toplam: @kurum.Eslestirmeler.Sum(e => e.ToplamTL) TL</strong>
                                </div>
                            </div>
                        }
                    </div>
                }

                <div class="mt-3 alert alert-info">
                    <strong>Cari Toplam (@DateTime.Now.Year.@DateTime.Now.Month):</strong> 
                    @secilmisCari.Kurumlar.Sum(k => k.Eslestirmeler.Sum(e => e.ToplamTL)) TL
                </div>
            </div>
        </div>
    }
</div>

@code {
    private CariKurumHiyerarsiDto? secilmisCari;
    private List<CariBasitDto> aranmisCarilar = new();
    private int? expandedKurumId;

    private async Task SecilmisCariDegisAsync(int cariId)
    {
        secilmisCari = await FiloKomisyonService.GetCariKurumHiyerarsiAsync(cariId);
        aranmisCarilar.Clear();
    }

    private async Task SearcimCariAsync(string aranan)
    {
        if (string.IsNullOrWhiteSpace(aranan)) 
        { 
            aranmisCarilar.Clear(); 
            return; 
        }

        // Search carileri yükle (TODO: Implement)
        // aranmisCarilar = await FiloKomisyonService.SearchCarilarAsync(aranan);
    }

    private void ToggleKurumAsync(int kurumId)
    {
        expandedKurumId = expandedKurumId == kurumId ? null : kurumId;
    }

    private async Task RefreshYield()
    {
        if (secilmisCari != null)
        {
            secilmisCari = await FiloKomisyonService.GetCariKurumHiyerarsiAsync(secilmisCari.CariId);
            StateHasChanged();
        }
    }
}
```

### 4.3 PuantajGridByKurum.razor (Gün Grid)

**Pseudo Code**:
```razor
@using MKFiloServis.Web.Services.Interfaces

<div class="table-responsive">
    <table class="table table-sm table-bordered">
        <thead>
            <tr>
                <th>Plaka</th>
                <th>Şoför</th>
                @for (int gün = 1; gün <= DateTime.DaysInMonth(yil, ay); gün++)
                {
                    <th class="text-center">@gün</th>
                }
                <th class="text-center">TOPLAM</th>
                <th class="text-center">İşlem</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var row in gridData)
            {
                <tr>
                    <td>@row.Plaka</td>
                    <td>@row.SoforAdi</td>
                    @foreach (var hucre in row.OylukHucreler)
                    {
                        <td class="text-center" @onclick="() => SetEditCell(row.SiradakiAracId, hucre.GunNo)">
                            @if (editCell != null && editCell.GunNo == hucre.GunNo)
                            {
                                <input type="number" @bind="editCell.Deger" class="form-control form-control-sm" />
                            }
                            else
                            {
                                <span>@hucre.Deger</span>
                            }
                        </td>
                    }
                    <td class="text-end">@row.OylukHucreler.Sum(h => h.Deger)</td>
                    <td class="text-center">
                        <button class="btn btn-sm btn-outline-success" @onclick="() => KaydetAsync(row.SiradakiAracId)">
                            <i class="bi bi-save"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" @onclick="() => SilAsync(row.SiradakiAracId)">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@code {
    [Parameter]
    public int KurumId { get; set; }
    [Parameter]
    public int Yil { get; set; }
    [Parameter]
    public int Ay { get; set; }
    [Parameter]
    public EventCallback OnSave { get; set; }
    [Parameter]
    public EventCallback OnDelete { get; set; }

    private List<AylikPuantajGridDto> gridData = new();
    private GunHucreDto? editCell;

    @inject IFiloKomisyonService FiloKomisyonService
    @inject IToastService ToastService

    protected override async Task OnInitializedAsync()
    {
        gridData = await FiloKomisyonService.GetKurumAylikPuantajGridAsync(KurumId, Yil, Ay);
    }

    private async Task KaydetAsync(int aracId)
    {
        try
        {
            // TODO: Update puantajları DB'de güncelle
            // await FiloKomisyonService.UpdateGunlukPuantajlarAsync(...);

            ToastService.ShowSuccess("Puantajlar kaydedildi.");
            await OnSave.InvokeAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Hata: {ex.Message}");
        }
    }

    private async Task SilAsync(int aracId)
    {
        try
        {
            // TODO: Puantajları soft-delete
            // await FiloKomisyonService.DeleteGunlukPuantajlarAsync(...);

            ToastService.ShowSuccess("Puantajlar silindi.");
            await OnDelete.InvokeAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Hata: {ex.Message}");
        }
    }
}
```

---

## 5. DTO Modelleri

```csharp
// DTO'lar (MKFiloServis.Web/Models/CariKurumPuantajDtos.cs)

public class CariKurumHiyerarsiDto
{
    public int CariId { get; set; }
    public string CariUnvan { get; set; } = "";
    public string CariKodu { get; set; } = "";
    public List<KurumDetayDto> Kurumlar { get; set; } = new();
}

public class KurumDetayDto
{
    public int KurumId { get; set; }
    public string KurumAdi { get; set; } = "";
    public string KurumKodu { get; set; } = "";
    public List<EslestirmeDetayDto> Eslestirmeler { get; set; } = new();
}

public class EslestirmeDetayDto
{
    public int EslestirmeId { get; set; }
    public string? Plaka { get; set; }
    public string? SoforAdi { get; set; }
    public string? GuzerigahAdi { get; set; }
    public bool IsActive { get; set; }
    public decimal KurumaUcret { get; set; }
    public decimal HedefeUcret { get; set; }
}

public class CariPuantajOzetDto
{
    public int CariId { get; set; }
    public string CariUnvan { get; set; } = "";
    public string CariKodu { get; set; } = "";
    public int KurumSayisi { get; set; }
    public int ToplamSefer { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal ToplamMaliyet { get; set; }
    public DateTime BaslamaTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
}

public class AylikPuantajGridDto
{
    public int SiradakiAracId { get; set; }
    public int SiradakiSoforId { get; set; }
    public string? Plaka { get; set; }
    public string? SoforAdi { get; set; }
    public List<GunHucreDto> OylukHucreler { get; set; } = new();
}

public class GunHucreDto
{
    public int GunNo { get; set; }
    public string DayOfWeek { get; set; } = "";
    public int PuantajId { get; set; }
    public int Deger { get; set; }
    public ServisTuru ServisTuru { get; set; }
    public OperasyonDurumu Durum { get; set; }
}
```

---

## 6. Index Strategy (Veritabanı Performansı)

```csharp
// ApplicationDbContext.OnModelCreating'de eklenecek:

// Index 1: Kurum → Cari arama
modelBuilder.Entity<Kurum>()
    .HasIndex(k => k.CariId)
    .HasName("IX_Kurum_CariId");

// Index 2: Eşleştirmeler → Kurum
modelBuilder.Entity<FiloGuzergahEslestirme>()
    .HasIndex(e => new { e.KurumId, e.IsActive })
    .HasName("IX_FiloGuzergahEslestirmeleri_KurumId_IsActive");

// Index 3: Puantajlar → Kurum + Tarih (Range query)
modelBuilder.Entity<FiloGunlukPuantaj>()
    .HasIndex(p => new { p.KurumId, p.Tarih })
    .HasName("IX_FiloGunlukPuantajlar_KurumId_Tarih");

// Index 4: Puantajlar → Cari + Tarih (Backward compat)
modelBuilder.Entity<FiloGunlukPuantaj>()
    .HasIndex(p => new { p.KurumFirmaId, p.Tarih })
    .HasName("IX_FiloGunlukPuantajlar_KurumFirmaId_Tarih");
```

---

## 7. Caching Strategy (Redis / In-Memory)

```csharp
// Services/CacheService.cs

public class CariKurumCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IFiloKomisyonService _filoService;

    private const string CacheKeyPrefix = "cari_kurum_";
    private const int CacheTTLHours = 24;

    public async Task<CariKurumHiyerarsiDto> GetCachedCariKurumAsync(int cariId)
    {
        var cacheKey = $"{CacheKeyPrefix}h_{cariId}";

        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<CariKurumHiyerarsiDto>(cached)!;

        var fresh = await _filoService.GetCariKurumHiyerarsiAsync(cariId);
        await _cache.SetStringAsync(cacheKey, 
            JsonSerializer.Serialize(fresh), 
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheTTLHours) 
            });

        return fresh;
    }

    public async Task InvalidateCariKurumCacheAsync(int cariId)
    {
        await _cache.RemoveAsync($"{CacheKeyPrefix}h_{cariId}");
    }
}
```

---

## Özet Checklist

- [ ] Cari.cs: `public virtual ICollection<Kurum> KurumListesi` ekle
- [ ] Kurum.cs: `public int? CariId` ve `public virtual Cari? Cari` ekle
- [ ] Kurum.cs: `public virtual ICollection<FiloGuzergahEslestirme>` ekle
- [ ] FiloGuzergahEslestirme.cs: `public int? KurumId` ve `public virtual Kurum? Kurum` ekle
- [ ] FiloGunlukPuantaj.cs: `public int? KurumId` ve `public virtual Kurum? Kurum` ekle
- [ ] ApplicationDbContext: Fluent API ile tüm relations tanımla
- [ ] IFiloKomisyonService: 5 yeni metot ekle
- [ ] FiloKomisyonService: İmplementasyonlar yaz
- [ ] DTO modelleri oluştur
- [ ] EslestirmeTanimlari.razor: Yeni layout
- [ ] PuantajGridByKurum.razor: Akordiyonlu layout
- [ ] Index'ler ekle
- [ ] Cache service ekle

---

**Versiyon**: 1.0 (Taslak)  
**Son Güncelleme**: 2025-01-23
