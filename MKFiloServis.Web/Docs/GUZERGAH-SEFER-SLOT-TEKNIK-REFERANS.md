# Güzergah Sefer Slot Teknik Referans

**Tarih**: 2025-01-23  
**Hedef**: GuzergahSefer ve Sefer Slot yönetiminin detaylı teknik kılavuzu

---

## 1. GuzergahSefer Mimarisi

### 1.1 Entity Şeması

```
Guzergah (1)
    │
    │ 1-to-Many
    │
    ▼
GuzergahSefer (N)
    └─ Sira: 1, 2, 3, ...
    ├─ SeferTipi: Sabah, Akşam, Mesai, ...
    ├─ Slot: Sabah (06:00), Aksam (17:00), ...
    ├─ KapasiteAdi: "16+1", "27+1"
    ├─ AracId: FK
    ├─ SoforAd: Serbest metin
    └─ FirmaAdiSerbest: Tedarikçi adı
```

### 1.2 Sefer Slot Türleri

```
SeferSlot Enum
├─ Sabah (1)       // 06:00-08:30
├─ Aksam (2)       // 16:00-19:00
├─ Og (3)          // 12:00-13:30
├─ Mesai (4)       // 08:00-17:00 (tam gün)
├─ Diger1 (5)      // Özel slot 1
├─ Diger2 (6)      // Özel slot 2
├─ Diger3 (7)      // Özel slot 3
├─ Diger4 (8)      // Özel slot 4
└─ Diger5 (9)      // Özel slot 5
```

---

## 2. Sefer Slot Yapılandırma Örnekleri

### Senaryo 1: Basit Sabah-Akşam (TRT Ankara)

```
Güzergah: GZR-TRT-MERKEZ
├─ GuzergahSefer [1]
│  ├─ Sira: 1
│  ├─ Slot: Sabah
│  ├─ KapasiteAdi: "16+1"
│  ├─ AracId: 1 (34CX8)
│  └─ SoforAd: "Ali Demir"
│
└─ GuzergahSefer [2]
   ├─ Sira: 2
   ├─ Slot: Aksam
   ├─ KapasiteAdi: "16+1"
   ├─ AracId: 1 (34CX8)
   └─ SoforAd: "Ali Demir"
```

### Senaryo 2: Üç Sefer (Sabah-Öğle-Akşam)

```
Güzergah: GZR-ISKI-ANKARA
├─ GuzergahSefer [1]
│  ├─ Sira: 1
│  ├─ Slot: Sabah (06:00)
│  ├─ KapasiteAdi: "16+1"
│  ├─ AracId: 2 (34DX5)
│  └─ SoforAd: "Mehmet Yılmaz"
│
├─ GuzergahSefer [2]
│  ├─ Sira: 2
│  ├─ Slot: Og (12:00)
│  ├─ KapasiteAdi: "8+1"
│  ├─ AracId: 3 (34EX1)
│  └─ SoforAd: "Veli Kara"
│
└─ GuzergahSefer [3]
   ├─ Sira: 3
   ├─ Slot: Aksam (17:00)
   ├─ KapasiteAdi: "16+1"
   ├─ AracId: 2 (34DX5)
   └─ SoforAd: "Mehmet Yılmaz"
```

### Senaryo 3: Vardiya (Mesai - 24 Saat)

```
Güzergah: GZR-HOSPITAL-24H
├─ GuzergahSefer [1]
│  ├─ Sira: 1
│  ├─ Slot: Mesai (Gündüz 08:00-17:00)
│  ├─ KapasiteAdi: "27+1"
│  ├─ AracId: 10 (Lüks Otobüs)
│  └─ SoforAd: "Ahmet Şahin"
│
└─ GuzergahSefer [2]
   ├─ Sira: 2
   ├─ Slot: Diger1 (Gece 17:00-08:00)
   ├─ KapasiteAdi: "27+1"
   ├─ AracId: 11 (Lüks Otobüs)
   └─ SoforAd: "Hassan Aziz"
```

---

## 3. Teknik Implementasyon (Service)

### 3.1 GuzergahSefer Service

```csharp
public interface IGuzergahSeferService
{
    // Bir güzergahın tüm sefer slotlarını getir
    Task<List<GuzergahSefer>> GetGuzergahSefsrAsync(int guzergahId);

    // Slot'a göre sefer detay getir
    Task<GuzergahSefer> GetSeferBySlotAsync(int guzergahId, SeferSlot slot);

    // Yeni sefer slot ekle
    Task<GuzergahSefer> AddSeferAsync(GuzergahSefer sefer);

    // Sefer güncelle
    Task<GuzergahSefer> UpdateSeferAsync(GuzergahSefer sefer);

    // Sefer sil
    Task<bool> DeleteSeferAsync(int seferId);

    // Güzergah sefer slotlarını yeniden sırala
    Task<bool> ReoderSefersAsync(int guzergahId, Dictionary<int, int> siraMapping);
}

public class GuzergahSeferService : IGuzergahSeferService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public async Task<List<GuzergahSefer>> GetGuzergahSefersAsync(int guzergahId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GuzergahSefers
            .Where(gs => gs.GuzergahId == guzergahId && !gs.IsDeleted)
            .Include(gs => gs.Arac)
            .Include(gs => gs.Firma)
            .OrderBy(gs => gs.Sira)
            .ToListAsync();
    }

    public async Task<GuzergahSefer?> GetSeferBySlotAsync(int guzergahId, SeferSlot slot)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GuzergahSefers
            .Where(gs => gs.GuzergahId == guzergahId && gs.Slot == slot && !gs.IsDeleted)
            .Include(gs => gs.Arac)
            .FirstOrDefaultAsync();
    }

    public async Task<GuzergahSefer> AddSeferAsync(GuzergahSefer sefer)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Sıra otomatik hesapla
        var maxSira = await context.GuzergahSefers
            .Where(gs => gs.GuzergahId == sefer.GuzergahId && !gs.IsDeleted)
            .MaxAsync(gs => (int?)gs.Sira) ?? 0;

        sefer.Sira = maxSira + 1;

        context.GuzergahSefers.Add(sefer);
        await context.SaveChangesAsync();
        return sefer;
    }

    public async Task<bool> ReoderSefersAsync(int guzergahId, Dictionary<int, int> siraMapping)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var sefers = await context.GuzergahSefers
            .Where(gs => gs.GuzergahId == guzergahId && !gs.IsDeleted)
            .ToListAsync();

        foreach (var sefer in sefers)
        {
            if (siraMapping.TryGetValue(sefer.Id, out var yeniSira))
            {
                sefer.Sira = yeniSira;
            }
        }

        await context.SaveChangesAsync();
        return true;
    }
}
```

---

## 4. Guzergah & GuzergahSefer Kullanım Adımları

### 4.1 Güzergah Oluşturma

```csharp
// Step 1: Guzergah oluştur
var guzergah = new Guzergah
{
    FirmaId = 1, // Tenant
    GuzergahKodu = "GZR-TRT-001",
    GuzergahAdi = "TRT Ankara - Merkez Servisi",
    BaslangicNoktasi = "TRT Paşaköy, Ankara",
    BitisNoktasi = "TRT Dış Ticaret Şubesi",
    BirimFiyat = 150m,        // Kurumdan tahsil
    GiderFiyat = 80m,         // Gider
    Mesafe = 12.5m,
    TahminiSure = 30,
    PersonelSayisi = 12,
    SeferTipi = SeferTipi.SabahAksam,
    Aktif = true
};

await guzergahService.AddGuzergahAsync(guzergah);
// guzergah.Id = 3 (Otomatik atandı)

// Step 2: Sefer slotlarını ekle
var seferSabah = new GuzergahSefer
{
    GuzergahId = 3,
    FirmaId = 1,
    Sira = 1,
    Slot = SeferSlot.Sabah,
    SeferTipi = SeferTipi.Sabah,
    KapasiteAdi = "16+1",
    AracId = 1,  // 34CX8
    SoforAd = "Ali Demir"
};

var seferAksam = new GuzergahSefer
{
    GuzergahId = 3,
    FirmaId = 1,
    Sira = 2,
    Slot = SeferSlot.Aksam,
    SeferTipi = SeferTipi.Aksam,
    KapasiteAdi = "16+1",
    AracId = 1,  // 34CX8
    SoforAd = "Ali Demir"
};

await guzergahSeferService.AddSeferAsync(seferSabah);
await guzergahSeferService.AddSeferAsync(seferAksam);

// Step 3: Eşleştirme (FiloGuzergahEslestirme) oluştur
var eslestirme = new FiloGuzergahEslestirme
{
    FirmaId = 1,
    KurumFirmaId = 42,  // TRT-ANKARA Cari ID
    GuzergahId = 3,
    AracId = 1,
    SoforId = 5,  // Ali Demir Person ID
    ServisTuru = ServisTuru.SabahAksam,
    KurumaKesilecekUcret = 150m,   // BirimFiyat
    TaseronaOdenenUcret = 80m,     // GiderFiyat
    IsActive = true
};

await filoKomisyonService.CreateEslestirmeAsync(eslestirme);
```

---

## 5. Sefer Slot Kayıtlarında Uyarılar

### ⚠ Validasyon Kural

```csharp
public class GuzergahSeferValidator : AbstractValidator<GuzergahSefer>
{
    public GuzergahSeferValidator()
    {
        RuleFor(gs => gs.Sira)
            .GreaterThan(0).WithMessage("Sıra > 0 olmalı");

        RuleFor(gs => gs.KapasiteAdi)
            .NotEmpty().WithMessage("Kapasite belirtilmeli")
            .Matches(@"^\d+\+\d+$").WithMessage("Format: \"16+1\" (yolcu+şoför)");

        RuleFor(gs => gs.AracId)
            .NotNull().When(gs => gs.Arac == null)
            .WithMessage("Araç seçilmeli");

        // Aynı güzergah + slot kombinasyonu 2 kez olamaz
        RuleFor(gs => gs.Slot)
            .Custom((slot, context) =>
            {
                var existing = _context.GuzergahSefers
                    .Where(gs => gs.GuzergahId == context.Parent.GuzergahId &&
                                 gs.Slot == slot &&
                                 !gs.IsDeleted &&
                                 gs.Id != context.Parent.Id)
                    .FirstOrDefaultAsync()
                    .Result;

                if (existing != null)
                {
                    context.AddFailure($"Bu güzergahta {slot} slotu zaten var");
                }
            });
    }
}
```

---

## 6. Hızlı Referans Tablosu

| Operasyon | Kod | Örnek |
|-----------|-----|-------|
| Güzergahı getir | `guzergahService.Get(id)` | `Get(3)` |
| Güzergah sefsrlerini getir | `seferService.GetGuzergahSefers(gzId)` | `GetGuzergahSefers(3)` |
| Slot bazında sefer | `seferService.GetSeferBySlot(gzId, slot)` | `GetSeferBySlot(3, Sabah)` |
| Sefer ekle | `seferService.AddSefer(sefer)` | `AddSefer(new {Sira:1, ...})` |
| Sıra yenile | `seferService.ReorderSefers(gzId, map)` | `ReorderSefers(3, {1:2, 2:1})` |
| Eşleştirme oluştur | `filoService.CreateEslestirme(e)` | `CreateEslestirme(new {...})` |
| Günlük puantaj oluştur | `filoService.CreateDailyPuantaj(gzId, ...)` | `CreateDailyPuantaj(3, 1, 5, 42)` |

---

**Teknik Referans - v1.0 (2025-01-23)**
