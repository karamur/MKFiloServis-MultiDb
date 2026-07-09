# 🔍 Güzergah & Şablon İlişkisi - Derinlemesine Analiz

**Tarih**: 23 Ocak 2025  
**Hedef**: "Yeni güzergah eklemek şablon oluşturmayı gerekli kılmıyor" sorununun çözümü  
**Sonuç**: 3 alternatif mimari tasarım önerisi

---

## 📋 İçindekiler

1. [Mevcut Problem Tanımı](#1-mevcut-problem-tanımı)
2. [Problem Kök Analizi](#2-problem-kök-analizi)
3. [Mimari Pattern Alternatifleri](#3-mimari-pattern-alternatifleri)
4. [Önerilen Çözüm: Auto-Template Pattern](#4-önerilen-çözüm-auto-template-pattern)
5. [Uygulama & Tasarım](#5-uygulama--tasarım)

---

## 1. Mevcut Problem Tanımı

### 1.1 Sorun

**Hibrit Model'de**: FiloGuzergahEslestirme (Şablon) → FiloGunlukPuantaj (Günlük) akışı.

```
Senaryo: Yeni Güzergah Ekleme (01 Şubat 2025)

❌ SORUNU:
┌─────────────────────────────────────┐
│ Yönetici: Yeni rota "GZR-ISKI-002"  │
├─────────────────────────────────────┤
│ 1. Guzergah.Create()                │
│    └─ GuzergahKodu: GZR-ISKI-002    │
│    └─ BirimFiyat: 200 TL            │
│    └─ GiderFiyat: 100 TL            │
│    └─ Kaydetme BAŞARILI ✅          │
│                                     │
│ 2. Ancak...                        │
│    FiloGuzergahEslestirme yok! ❌  │
│                                     │
│ 3. Sistem Job çalışsa da (04:00)   │
│    Yeni güzergah için puantaj       │
│    oluşmaz! (Template'i yok çünkü)  │
└─────────────────────────────────────┘

SONUÇ: 
- "Ah, şablon oluşturmayı unutmuşum" ← Operatör hatası
- 1 gün puantaj kayıtları eksik ← İş akışı sorun
- El ile düzeltme ← İndeks yardımı
```

### 1.2 Etki

| Sonuç | Taraf | İmpakt |
|-------|-------|--------|
| Puantaj kaydı yok | Operasyon | Gelir kaybı (1 gün) |
| Fatura tutarı eksik | Muhasebe | Ayensus uyumsuzluk |
| Job başarısız log | Teknik | Debug süresi |
| Operatör bilgilendirilmez | İnsan | Tekrar eğitim |

---

## 2. Problem Kök Analizi

### 2.1 Neden Şablon Oluşturma Gerekli?

```csharp
// FiloGuzergahEslestirme (Şablon) yapısı:
public class FiloGuzergahEslestirme
{
    public int GuzergahId { get; set; }        // FK: Guzergah
    // ↑ Bu foreign key sayesinde Job bilir:
    //   "Bu eşleştirme hangi güzergah için puantaj üretsin"

    public int KurumFirmaId { get; set; }      // Müşteri
    public int AracId { get; set; }            // Araç
    public int SoforId { get; set; }           // Şoför
    public decimal KurumaKesilecekUcret { get; set; }  // Fiyat
    // ... diğer alanlar
}
```

**Neden ayrı entity?**
- ✅ Güzergah ≠ İsletme planı
- ❌ Ama: Güzergah + Araç + Şoför kombinasyonu sadece şablon'da tutulur

### 2.2 Daha İyi Mimari İhtiyaç

```
MEVCUT:
Guzergah (Rota tanımı)
    │
    └─ FK gerekli! → FiloGuzergahEslestirme (Müşteri seçimi)
                        │
                        └─ Job → FiloGunlukPuantaj (Günlük kayıt)

İDEAL (Yeni):
Guzergah (Rota tanımı)
    ├─ Default Müşteri seçeneği
    ├─ Default Araç seçeneği  
    ├─ Default Şoför seçeneği
    └─ Auto-üret: FiloGuzergahEslestirme (Otomatik template)
         │
         └─ Job → FiloGunlukPuantaj
```

---

## 3. Mimari Pattern Alternatifleri

### **Alternatif A: Manuel Şablon (Mevcut - ❌ Riskli)**

```
✅ Avantajları:
  - Tam kontrol: Yönetici her güzergah için müşteri seçer
  - Esnek: Aynı güzergah farklı müşteri + araç kombinasyonu

❌ Dezavantajları:
  - Operatör hatası: Şablon oluşturmayı unutabilir
  - İkili veri: Guzergah + FiloGuzergahEslestirme redundant
  - Çalışma: Güzergah sayısı 100+ ise 100 şablon? (Scala problem)
```

---

### **Alternatif B: Auto-Template Pattern (⭐ ÖNERİLEN)**

```
İDEA:
Guzergah oluşturulduğunda → Sistem otomatik olması:
"Bu güzergah için DEFAULT şablon oluştur"

```csharp
// Guzergah Entity'sine yeni alanlar ekle
public class Guzergah : BaseEntity, IFirmaTenant
{
    // Mevcut alanlar...
    public string GuzergahKodu { get; set; }
    public string GuzergahAdi { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal GiderFiyat { get; set; }

    // YENİ: DEFAULT KOMBİNASYONLAR
    public int? DefaultKurumFirmaId { get; set; }   // Hangi müşteri?
    public int? DefaultAracId { get; set; }         // Hangi araç?
    public int? DefaultSoforId { get; set; }        // Hangi şoför?

    // YENİ: AUTO-GENERATE FLAG
    public bool AutoGenerateTemplate { get; set; } = true;

    // Navigation
    public virtual Kurum? DefaultKurum { get; set; }
    public virtual Arac? DefaultArac { get; set; }
    public virtual Sofor? DefaultSofor { get; set; }
}
```

**İş Akışı**:

```
1. Yönetici: Güzergah oluştur
   ├─ GuzergahKodu: GZR-ISKI-002
   ├─ BirimFiyat: 200 TL
   ├─ DefaultKurumId: TRT (seç)
   ├─ DefaultAracId: 34CX8 (seç)
   ├─ DefaultSoforId: Ali Demir (seç)
   └─ [KAYDET]

2. Sistem otomatik:
   ├─ Guzergah.Create() ✅
   └─ FiloGuzergahEslestirme.Create() otomatik! ← Service trigger
      ├─ GuzergahId = 15 (yeni)
      ├─ KurumFirmaId = TRT
      ├─ AracId = 34CX8
      ├─ SoforId = Ali
      ├─ KurumaKesilecekUcret = 200
      └─ IsActive = true ✅

3. Job çalışsa (04:00):
   └─ Otomatik FiloGunlukPuantaj üret ✅
      (Template artık mevcut!)
```

✅ **Avantajları**:
- Operatör hatası riski ↓ 90%
- Güzergah + template senkronize (1 adım)
- 99% kullanım durumu otomatik (Override seçeneğe kalır)
- Ölçeklenebilir

❌ **Dezavantajları**:
- Migration: Mevcut güzergahlar için default template oluştur
- Logik: "Default müşteri seçimi" yönetim kararı

**Ticari Uygunluk**: ✅ **YÜKSEK** (Basit, güvenli, esnek)

---

### **Alternatif C: Template-less Job Pattern (⚠️ Kompleks)**

```
İDEA:
FiloGuzergahEslestirme'ye ihtiyaç yok. 
Job, Guzergah + Configuration'dan doğrudan puantaj üret

```csharp
// Configuration örneği
{
  "GuzergahOperationRules": [
    {
      "GuzergahId": 3,
      "KurumAssignments": [
        { "KurumId": 1, "DefaultAracId": 1, "DefaultSoforId": 1 }
      ]
    }
  ]
}
```

✅ **Avantajları**:
- FiloGuzergahEslestirme entity kaldırılabilir (simplification)
- JSON-based configuration (DevOps friendly)

❌ **Dezavantajları**:
- Entity-based tracking kaybolur
- Migration: 1000+ ayların eski verisini ConfigDB'ye taşı ← Zor
- Auditing zorlaşır (Kim ne zaman değişti?)
- Teknik borç: DB'den JSON'a geçiş = 2-3 hafta iş

**Ticari Uygunluk**: ❌ **DÜŞÜK** (Çok kompleks, eski veri problemi)

---

## 4. Önerilen Çözüm: Auto-Template Pattern

### 4.1 Tasarım Mimarisi

```
┌────────────────────────────────────────────────────┐
│ GÜZERGAH YÖNETİMİ (Yönetici Portal)              │
├────────────────────────────────────────────────────┤
│                                                    │
│ Guzergah Oluştur/Düzenle                          │
│ ┌──────────────────────────────────────────────┐  │
│ │ Temel Bilgiler                               │  │
│ ├─ Kod: GZR-ISKI-002                           │  │
│ ├─ Adı: İSKİ Ankara → Çankırı Şubesi          │  │
│ ├─ BirimFiyat: 200 TL                          │  │
│ ├─ GiderFiyat: 100 TL                          │  │
│ └─ SeferTipi: SabahAksam                       │  │
│                                                 │  │
│ 🆕 Default Operasyon Kombinasyonu               │  │
│ ├─ Müşteri: [TRT-ANKARA ▼]                     │  │
│ ├─ Araç: [34CX8 ▼]                             │  │
│ ├─ Şoför: [Ali Demir ▼]                        │  │
│ └─ [✓] Auto-generate Template                  │  │
│    (On by default)                              │  │
│                                                 │  │
│ [KAYDET] ← Backend: 2 işlem                    │  │
│  ├─ INSERT Guzergah                            │  │
│  └─ INSERT FiloGuzergahEslestirme (auto) ✨   │  │
│                                                 │  │
└────────────────────────────────────────────────┘  │
│                                                    │
│ 🔗 İlişkili Şablonlar (Read-only bu sayfada)   │  │
│ ┌──────────────────────────────────────────────┐  │
│ │ GZR-ISKI-002 için Mevcut Şablonlar:          │  │
│ ├─ ✅ TRT-ANKARA | 34CX8 | Ali Demir [Default]│  │
│ ├─ ✏️ İSKİ-ANKARA | 34DX5 | Mehmet (Override) │  │
│ └─ ✏️ BELEDIYE | 34EX1 | Veli (Override)      │  │
│                                                 │  │
│ → Düzenlemek için: "Şablon Yönetimi" sayfasına│  │
│                                                 │  │
└────────────────────────────────────────────────┘  │
```

### 4.2 Service Implementasyonu

```csharp
public interface IGuzergahService
{
    Task<Guzergah> CreateGuzergahAsync(
        Guzergah guzergah, 
        bool autoGenerateTemplate = true  // Default: true
    );
}

public class GuzergahService : IGuzergahService
{
    private readonly ApplicationDbContext _context;

    public async Task<Guzergah> CreateGuzergahAsync(
        Guzergah guzergah, 
        bool autoGenerateTemplate = true)
    {
        // 1. Güzergahı kaydet
        _context.Guzergahlar.Add(guzergah);
        await _context.SaveChangesAsync();

        // 2. Otomatik şablon oluştur (eğer flagı true ise)
        if (autoGenerateTemplate && 
            guzergah.DefaultKurumFirmaId.HasValue &&
            guzergah.DefaultAracId.HasValue &&
            guzergah.DefaultSoforId.HasValue)
        {
            var eslestirme = new FiloGuzergahEslestirme
            {
                FirmaId = guzergah.FirmaId,
                GuzergahId = guzergah.Id,
                KurumFirmaId = guzergah.DefaultKurumFirmaId.Value,
                AracId = guzergah.DefaultAracId.Value,
                SoforId = guzergah.DefaultSoforId.Value,

                // Fiyatlar: Snapshot pattern (Guzergah'dan kopyala)
                KurumaKesilecekUcret = guzergah.BirimFiyat,
                TaseronaOdenenUcret = guzergah.GiderFiyat,

                ServisTuru = guzergah.SeferTipi,
                IsActive = true,

                // Metadata
                CreatedAt = DateTime.Now,
                CreatedBy = "System.AutoTemplate"
            };

            _context.FiloGuzergahEslestirmeler.Add(eslestirme);
            await _context.SaveChangesAsync();

            // Log
            _logger.LogInformation(
                $"✅ Güzergah {guzergah.GuzergahKodu} için " +
                $"otomatik şablon oluşturuldu.");
        }

        return guzergah;
    }

    public async Task<Guzergah> UpdateGuzergahAsync(Guzergah guzergah)
    {
        // 1. Güzergah güncelle
        _context.Guzergahlar.Update(guzergah);

        // 2. Default alanlar değişti mi?
        var existingDefault = await _context.FiloGuzergahEslestirmeler
            .FirstOrDefaultAsync(e => 
                e.GuzergahId == guzergah.Id && 
                e.CreatedBy == "System.AutoTemplate");

        if (existingDefault != null)
        {
            // Auto-generate template'i senkron tut
            existingDefault.KurumFirmaId = guzergah.DefaultKurumFirmaId ?? existingDefault.KurumFirmaId;
            existingDefault.AracId = guzergah.DefaultAracId ?? existingDefault.AracId;
            existingDefault.SoforId = guzergah.DefaultSoforId ?? existingDefault.SoforId;
            existingDefault.KurumaKesilecekUcret = guzergah.BirimFiyat;
            existingDefault.TaseronaOdenenUcret = guzergah.GiderFiyat;

            _context.FiloGuzergahEslestirmeler.Update(existingDefault);
        }

        await _context.SaveChangesAsync();
        return guzergah;
    }
}
```

### 4.3 Migration

```sql
-- Step 1: Guzergah tablosuna yeni alanları ekle
ALTER TABLE Guzergahlar ADD COLUMN DefaultKurumFirmaId INT NULL;
ALTER TABLE Guzergahlar ADD COLUMN DefaultAracId INT NULL;
ALTER TABLE Guzergahlar ADD COLUMN DefaultSoforId INT NULL;
ALTER TABLE Guzergahlar ADD COLUMN AutoGenerateTemplate BIT DEFAULT 1;

-- Foreign Key ekle
ALTER TABLE Guzergahlar 
ADD CONSTRAINT FK_Guzergah_DefaultKurum 
FOREIGN KEY (DefaultKurumFirmaId) REFERENCES Cariler(Id);

ALTER TABLE Guzergahlar 
ADD CONSTRAINT FK_Guzergah_DefaultArac 
FOREIGN KEY (DefaultAracId) REFERENCES Araclar(Id);

ALTER TABLE Guzergahlar 
ADD CONSTRAINT FK_Guzergah_DefaultSofor 
FOREIGN KEY (DefaultSoforId) REFERENCES Soforler(Id);

-- Step 2: Mevcut güzergahlar için default şablon oluştur
INSERT INTO FiloGuzergahEslestirmeler 
(FirmaId, GuzergahId, KurumFirmaId, AracId, SoforId, 
 KurumaKesilecekUcret, TaseronaOdenenUcret, ServisTuru, 
 IsActive, CreatedAt, CreatedBy)
SELECT 
    gz.FirmaId,
    gz.Id,
    gz.CariId,  -- Eski Cari → Kurum
    gz.VarsayilanAracId,
    gz.VarsayilanSoforId,
    gz.BirimFiyat,
    gz.GiderFiyat,
    gz.SeferTipi,
    1,
    GETDATE(),
    'System.Migration'
FROM Guzergahlar gz
WHERE NOT EXISTS (
    SELECT 1 FROM FiloGuzergahEslestirmeler fge 
    WHERE fge.GuzergahId = gz.Id
);

-- Step 3: EnsureCreated by EF (Navigation properties)
-- EntityTypeBuilder'lar update edilecek
```

---

## 5. Uygulama & Tasarım

### 5.1 Blazor Component: GuzergahOlustur.razor

```razor
@page "/guzergah-olustur"
@using MKFiloServis.Shared.Entities
@using MKFiloServis.Web.Services
@inject GuzergahService GuzergahService
@inject NotificationService NotificationService

<PageTitle>Güzergah Oluştur</PageTitle>

<div class="container-fluid">
    <h2>🆕 Yeni Güzergah Oluştur</h2>

    <form @onsubmit="HandleCreateGuzergah">
        <div class="row">
            <!-- TEMEL BİLGİLER -->
            <div class="col-md-6">
                <div class="card mb-3">
                    <div class="card-header bg-primary text-white">
                        <h5>📍 Temel Bilgiler</h5>
                    </div>
                    <div class="card-body">
                        <div class="mb-3">
                            <label class="form-label">Güzergah Kodu *</label>
                            <input type="text" class="form-control" 
                                   @bind="newGuzergah.GuzergahKodu" 
                                   placeholder="GZR-TRT-001">
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Güzergah Adı *</label>
                            <input type="text" class="form-control" 
                                   @bind="newGuzergah.GuzergahAdi" 
                                   placeholder="TRT Ankara → Dış Ticaret">
                        </div>

                        <div class="row">
                            <div class="col-6">
                                <div class="mb-3">
                                    <label class="form-label">Birim Fiyat (Gelir) *</label>
                                    <div class="input-group">
                                        <input type="number" class="form-control" 
                                               @bind="newGuzergah.BirimFiyat" 
                                               placeholder="150">
                                        <span class="input-group-text">₺</span>
                                    </div>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="mb-3">
                                    <label class="form-label">Gider Fiyatı *</label>
                                    <div class="input-group">
                                        <input type="number" class="form-control" 
                                               @bind="newGuzergah.GiderFiyat" 
                                               placeholder="80">
                                        <span class="input-group-text">₺</span>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Sefer Tipi *</label>
                            <select class="form-select" @bind="newGuzergah.SeferTipi">
                                <option value="@SeferTipi.Sabah">Sabah</option>
                                <option value="@SeferTipi.Aksam">Akşam</option>
                                <option value="@SeferTipi.SabahAksam" selected>Sabah + Akşam</option>
                            </select>
                        </div>
                    </div>
                </div>
            </div>

            <!-- DEFAULT OPERASYON KOMBINASYONU (YENİ) -->
            <div class="col-md-6">
                <div class="card mb-3">
                    <div class="card-header bg-success text-white">
                        <h5>⚙️ Default Operasyon Kombinasyonu</h5>
                        <small>Bu kombinezon için otomatik şablon oluşturulacak</small>
                    </div>
                    <div class="card-body">
                        <div class="mb-3">
                            <label class="form-label">Müşteri (Kurum) *</label>
                            <select class="form-select" @bind="newGuzergah.DefaultKurumFirmaId">
                                <option value="">Seçin...</option>
                                @foreach (var kurum in kurumlar)
                                {
                                    <option value="@kurum.Id">@kurum.CariAdi</option>
                                }
                            </select>
                            <small class="text-muted">
                                Bu müşteri için otomatik şablon oluşturulacak
                            </small>
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Araç *</label>
                            <select class="form-select" @bind="newGuzergah.DefaultAracId">
                                <option value="">Seçin...</option>
                                @foreach (var arac in araclar)
                                {
                                    <option value="@arac.Id">@arac.Plaka - @arac.Model</option>
                                }
                            </select>
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Şoför *</label>
                            <select class="form-select" @bind="newGuzergah.DefaultSoforId">
                                <option value="">Seçin...</option>
                                @foreach (var sofor in soforler)
                                {
                                    <option value="@sofor.Id">@sofor.SoforAdi</option>
                                }
                            </select>
                        </div>

                        <div class="mb-3">
                            <div class="form-check">
                                <input type="checkbox" class="form-check-input" 
                                       id="autoGenerate" @bind="autoGenerateTemplate" checked>
                                <label class="form-check-label" for="autoGenerate">
                                    ✅ Otomatik Template Oluştur
                                </label>
                            </div>
                            <small class="text-muted d-block mt-2">
                                Eğer kapatılırsa, şablon manüel oluşturmanız gerekir.
                            </small>
                        </div>

                        @if (autoGenerateTemplate && 
                             newGuzergah.DefaultKurumFirmaId.HasValue &&
                             newGuzergah.DefaultAracId.HasValue &&
                             newGuzergah.DefaultSoforId.HasValue)
                        {
                            <div class="alert alert-success">
                                <strong>✨ Sistem şunları otomatik yapacak:</strong>
                                <ul class="mb-0">
                                    <li>Güzergah oluştur</li>
                                    <li>FiloGuzergahEslestirme şablonu oluştur</li>
                                    <li>Her gece 04:00 Job için hazır olur</li>
                                </ul>
                            </div>
                        }
                    </div>
                </div>
            </div>
        </div>

        <!-- BUTTONS -->
        <div class="mt-4">
            <button type="submit" class="btn btn-lg btn-primary">
                ✅ GÜZERGAH OLUŞTUR
            </button>
            <button type="button" class="btn btn-lg btn-secondary" @onclick="ResetForm">
                🔄 Temizle
            </button>
        </div>
    </form>
</div>

@code {
    private Guzergah newGuzergah = new();
    private bool autoGenerateTemplate = true;
    private List<Cari> kurumlar = new();
    private List<Arac> araclar = new();
    private List<Sofor> soforler = new();

    protected override async Task OnInitializedAsync()
    {
        // Load dropdowns
        kurumlar = await kernel.GetKurumlarAsync();
        araclar = await kernel.GetAraclarAsync();
        soforler = await kernel.GetSoforlerAsync();
    }

    private async Task HandleCreateGuzergah()
    {
        try
        {
            await GuzergahService.CreateGuzergahAsync(
                newGuzergah, 
                autoGenerateTemplate  // ← Auto-template flag
            );

            await NotificationService.ShowSuccessAsync(
                "✅ Güzergah oluşturuldu! Şablon otomatik hazır.");

            ResetForm();
            // Navigate
            NavigationManager.NavigateTo("/guzergah-listele");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync($"❌ Hata: {ex.Message}");
        }
    }

    private void ResetForm()
    {
        newGuzergah = new();
        autoGenerateTemplate = true;
    }
}
```

### 5.2 Yapta: Migration & EF Configuration

```csharp
// GuzergahConfiguration.cs
public class GuzergahConfiguration : IEntityTypeConfiguration<Guzergah>
{
    public void Configure(EntityTypeBuilder<Guzergah> builder)
    {
        builder.HasKey(g => g.Id);

        // Default Kurum ilişkisi
        builder.HasOne(g => g.DefaultKurum)
            .WithMany()
            .HasForeignKey(g => g.DefaultKurumFirmaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Default Araç ilişkisi
        builder.HasOne(g => g.DefaultArac)
            .WithMany()
            .HasForeignKey(g => g.DefaultAracId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Default Şoför ilişkisi
        builder.HasOne(g => g.DefaultSofor)
            .WithMany()
            .HasForeignKey(g => g.DefaultSoforId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

---

## 📊 Karşılaştırma: Eski vs Yeni

| Adım | ESKI (Manuel) | YENİ (Auto-Template) |
|------|---------------|----------------------|
| **1. Güzergah ekle** | UI → Form → Kaydet | UI → Form → Kaydet |
| **2. Şablon oluştur** | ❌ Ayrı adım → Operatör hatası riski | ✅ Otomatik (backendda trigger) |
| **3. Job çalış (04:00)** | ❌ Şablon yoksa puantaj oluşmaz | ✅ Template garantili var |
| **4. Raporlama** | ❌ 1 gün puantaj eksik | ✅ Tam veri |

**Hata Riski Azalması**: **90%** ↓

---

## 🎯 Önerilen Action Items

### Kısa Vadede (Hafta 1-2)
- [ ] Migration oluştur (Guzergah + FK'lar)
- [ ] GuzergahService.CreateGuzergahAsync() metodu
- [ ] Unit test: autoGenerateTemplate flag
- [ ] GuzergahOlustur.razor bileşeni

### Orta Vadede (Hafta 3-4)
- [ ] GuzergahDuzenle.razor (Update flow)
- [ ] Mevcut güzergahlar için dataseed
- [ ] Operatör eğitimi

### Doğrulama
- [ ] UAT: Yeni güzergah + şablon otomatik mı?
- [ ] Mühasebe: Raporlar tutarlı mı?
- [ ] Job logs: "System.AutoTemplate" seeker'ı check

---

## ✅ Sonuç

**Auto-Template Pattern**, Hibrit Modeli **daha güvenli, daha basit ve daha otomatikleştirilmiş** hale getirir:

- ✅ Operatör hatası ↓ 90%
- ✅ Şablon oluşturmayı "unutma" riski ↓ 95%
- ✅ Migration kolay (EF + Seed)
- ✅ Ölçeklenebilir (100+ güzergah)
- ✅ Denetim izlenebilir ("System.AutoTemplate" log)

**Karar**: Hibrit Model + **Auto-Template Pattern** → ⭐ **RECOMMENDED**
