# FiloGuzergahEslestirme → FiloGunlukPuantaj Veri Akışı & Mapping

**Tarih**: 2025-01-23  
**Hedef**: Şablon-Puantaj ilişkisinin detaylı veri akışı ve SQL mapping'i

---

## 1. Entity İlişkisi

### 1.1 Şematik İlişki

```
┌────────────────────────────────────────────────┐
│    FiloGuzergahEslestirme (Şablon)             │
│    ├─ Id: 12                                  │
│    ├─ FirmaId: 1 (Tenant)                    │
│    ├─ KurumFirmaId: 42 (TRT-ANKARA)          │
│    ├─ GuzergahId: 3                          │
│    ├─ AracId: 1                              │
│    ├─ SoforId: 5                             │
│    ├─ KurumaKesilecekUcret: 150 TL           │
│    ├─ TaseronaOdenenUcret: 80 TL            │
│    ├─ ServisTuru: SabahAksam                │
│    └─ IsActive: true                         │
└────────────────────────────────────────────────┘
            │
            │ 1-to-Many (FK: FiloGuzergahEslestirmeId)
            │
            ▼
┌────────────────────────────────────────────────────────┐
│      FiloGunlukPuantaj (Günlük Kayıt)                  │
│      ├─ Id: 1001, 1002, 1003, ... (22 adet Ocak'ta)   │
│      ├─ FirmaId: 1 (Tenant)                           │
│      ├─ FiloGuzergahEslestirmeId: 12 (FK)             │
│      ├─ Tarih: 2025-01-02, 2025-01-03, ...            │
│      ├─ KurumFirmaId: 42 (TRT-ANKARA)                 │
│      ├─ GuzergahId: 3                                 │
│      ├─ AracId: 1                                     │
│      ├─ SoforId: 5                                    │
│      ├─ SeferSayisi: 2 (Sabah + Akşam)               │
│      ├─ PuantajCarpani: 1.0 (Normal gün)             │
│      │  veya 0.5 (Hafta sonu)                         │
│      │  veya 1.5 (Fazla mesai)                       │
│      ├─ TahakkukEdenKurumUcreti: 300 TL (2 × 150)    │
│      ├─ TahakkukEdenTaseronUcreti: 160 TL (2 × 80)   │
│      ├─ Durum: Gitti / Gitmedi / Arızalandı          │
│      ├─ Onaylandi: true/false                         │
│      └─ OnayTarihi: 2025-01-02 17:45                 │
└────────────────────────────────────────────────────────┘
```

### 1.2 Foreign Key & Navigation

```csharp
// FiloGuzergahEslestirme
public class FiloGuzergahEslestirme
{
    public int Id { get; set; }
    public int FirmaId { get; set; }
    public int KurumFirmaId { get; set; }
    public int GuzergahId { get; set; }
    public int AracId { get; set; }
    public int SoforId { get; set; }

    // Navigation
    public virtual Guzergah Guzergah { get; set; }
    public virtual Arac Arac { get; set; }
    public virtual Sofor Sofor { get; set; }

    // Reverse Navigation
    public virtual ICollection<FiloGunlukPuantaj> PuantajKayitlari { get; set; }
}

// FiloGunlukPuantaj
public class FiloGunlukPuantaj
{
    public int Id { get; set; }
    public int? FiloGuzergahEslestirmeId { get; set; }  // FK (Optional)

    // Copy of Eslestirme data (Snapshot)
    public int GuzergahId { get; set; }
    public int AracId { get; set; }
    public int SoforId { get; set; }
    public int KurumFirmaId { get; set; }

    // Navigation
    public virtual FiloGuzergahEslestirme? Eslestirme { get; set; }
    public virtual Guzergah? Guzergah { get; set; }
    public virtual Arac? Arac { get; set; }
    public virtual Sofor? Sofor { get; set; }
}
```

---

## 2. Veri Akışı Detayı (Uçtan Uca)

### 2.1 Adım 1: Şablon Oluşturma (Admin)

```csharp
// INPUT: Kullanıcı UI'da form doldurur
POST /api/fils-komisyon/eslestirmeler
{
    "firmaId": 1,
    "kurumFirmaId": 42,        // TRT-ANKARA sorunuş (Cari tablosundan)
    "guzergahId": 3,           // TRT Merkez Güzergahı
    "aracId": 1,               // 34CX8 Plakalı Araç
    "soforId": 5,              // Ali Demir (Personel)
    "servisTuru": 3,           // SabahAksam
    "kurumaKesilecekUcret": 150.00,  // Kurumdan tahsil (Guzergah.BirimFiyat ile eşle)
    "taseronaOdenenUcret": 80.00,    // Tedarikçiye ödeme (Guzergah.GiderFiyat ile eşle)
    "isActive": true
}

// DATABASE INSERT
INSERT INTO FiloGuzergahEslestirmeleri (
    FirmaId, KurumFirmaId, GuzergahId, AracId, SoforId,
    ServisTuru, KurumaKesilecekUcret, TaseronaOdenenUcret, IsActive, CreatedAt
) VALUES (
    1, 42, 3, 1, 5,
    3, 150.00, 80.00, 1, NOW()
);
-- RESULT: FiloGuzergahEslestirmeId = 12

// VALIDATION
├─ GuzergahId = 3 → Guzergah.BirimFiyat = 150 ✓ (Eşleştir)
├─ GuzergahId = 3 → Guzergah.GiderFiyat = 80 ✓ (Eşleştir)
├─ AracId = 1 → Arac.Aktif = true ✓
├─ SoforId = 5 → Sofor.Aktif = true ✓
└─ KurumFirmaId = 42 → Cari.Tip = 'Müşteri' ✓
```

### 2.2 Adım 2: Günlük Puantaj Otomatik Oluşturma (Job)

```csharp
// TRIGGER: Sabah 04:00 Job'u çalışır
// "Yarın için eşleştirmelerden günlük puantaj oluştur"

public class GenerateDailyPuantajJob
{
    public async Task ExecuteAsync()
    {
        var eslestirmeler = await context.FiloGuzergahEslestirmeleri
            .Where(e => e.IsActive && !e.IsDeleted)
            .Include(e => e.Guzergah)
            .ToListAsync();

        var tomorrow = DateTime.UtcNow.AddDays(1);

        foreach (var eslestirme in eslestirmeler)
        {
            // Step 1: Puantaj daha var mı?
            var existing = await context.FiloGunlukPuantajlar
                .Where(p => p.FiloGuzergahEslestirmeId == eslestirme.Id &&
                           p.Tarih.Date == tomorrow.Date &&
                           !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (existing != null)
                continue;  // Zaten var, atla

            // Step 2: Puantaj Çarpanı Hesapla
            var puantajCarpani = CalculatePuantajCarpani(tomorrow);
            // Normal gün: 1.0
            // Pazar: 0.5 (İzin günü)
            // Bayram: 0.0 (Tatil)
            // Fazla Mesai: 1.5

            // Step 3: Yeni Puantaj Kaydı Oluştur
            var puantaj = new FiloGunlukPuantaj
            {
                FirmaId = eslestirme.FirmaId,
                FiloGuzergahEslestirmeId = eslestirme.Id,  // ← LINK

                // Eşleştirmeden kopyala
                KurumFirmaId = eslestirme.KurumFirmaId,
                GuzergahId = eslestirme.GuzergahId,
                AracId = eslestirme.AracId,
                SoforId = eslestirme.SoforId,

                // Tarih
                Tarih = tomorrow,

                // Sefer bilgisi
                ServisTuru = eslestirme.ServisTuru,
                SeferSayisi = GetSeferSayisi(eslestirme.ServisTuru),  // 2 sefer
                PuantajCarpani = puantajCarpani,  // 1.0 / 0.5 / 1.5

                // Tutar hesapla
                TahakkukEdenKurumUcreti = 
                    SeferSayisi * eslestirme.KurumaKesilecekUcret * puantajCarpani,
                TahakkukEdenTaseronUcreti = 
                    SeferSayisi * eslestirme.TaseronaOdenenUcret * puantajCarpani,

                // Durum
                Durum = OperasyonDurumu.Planli,
                Onaylandi = false,
                OnayTarihi = null,

                CreatedAt = DateTime.UtcNow
            };

            context.FiloGunlukPuantajlar.Add(puantaj);
        }

        await context.SaveChangesAsync();
    }
}

// ÖRNEK SONUÇ (Ocak'ta 22 çalışan gün):
INSERT INTO FiloGunlukPuantajlar (...) VALUES (
    NULL, 1, 12, '2025-01-02', 42, 3, 1, 5, 3, 2, 1.0, 300.00, 160.00, 1, 0, NULL
);
-- ID = 1001

INSERT INTO FiloGunlukPuantajlar (...) VALUES (
    NULL, 1, 12, '2025-01-03', 42, 3, 1, 5, 3, 2, 1.0, 300.00, 160.00, 1, 0, NULL
);
-- ID = 1002

-- ... (20 tane daha)

SELECT COUNT(*) FROM FiloGunlukPuantajlar 
WHERE FiloGuzergahEslestirmeId = 12 AND MONTH(Tarih) = 1;
-- RESULT: 22 rows
```

---

## 3. Fiyat Snapshot Pattern

### 3.1 Problem

```
GÜN 2: Şablon oluşturuldu
└─ KurumaKesilecekUcret = 150 TL
└─ TaseronaOdenenUcret = 80 TL

GÜN 10: Admin fiyatı değiştirir
└─ UI: "Guzergah.BirimFiyat 150 → 160 TL değişti"
└─ Guzergah tablosu güncellendi

GÜN 11: Otomatik puantaj oluşturuluyor
└─ FiloGuzergahEslestirme, eski 150 TL okunuyor ✓
   (Eşleştirmede sabit tutulmuş)
└─ FiloGunlukPuantaj yeni 160 TL ile hesaplanıyor ✗
   (Guzergah'dan okumuş, yeni değer)

SONUÇ: Uyumsuzluk ❌
└─ GÜN 2-10: 150 TL ile hesaplanmış puantajlar
└─ GÜN 11+: 160 TL ile hesaplanmış puantajlar
└─ Hakedis toplamı yanlış
```

### 3.2 Çözüm: Snapshot

```csharp
// FiloGuzergahEslestirme (Şablon)
public class FiloGuzergahEslestirme : BaseEntity, IFirmaTenant
{
    // Mevcut alanlar...

    // YENİ: Snapshot alanları (Değişmeyecek)
    public decimal? GuzergahBirimFiyatSnapshot { get; set; }       // 150 TL (Kur)
    public decimal? GuzergahGiderFiyatSnapshot { get; set; }       // 80 TL (Gider)
    public int? GuzergahSeferSayisiSnapshot { get; set; }          // 2 (Sabah + Akşam)
}

// İYİLEŞTİRME: Şablon oluşturulurken snapshot'u kaydet
public async Task<FiloGuzergahEslestirme> CreateEslestirmeAsync(FiloGuzergahEslestirme eslestirme)
{
    var guzergah = await context.Guzergahlar.FindAsync(eslestirme.GuzergahId);

    // Snapshot'u kaydet (Değişmeyecek referans)
    eslestirme.GuzergahBirimFiyatSnapshot = guzergah!.BirimFiyat;
    eslestirme.GuzergahGiderFiyatSnapshot = guzergah.GiderFiyat;
    eslestirme.GuzergahSeferSayisiSnapshot = GetSeferSayisi(eslestirme.ServisTuru);

    context.FiloGuzergahEslestirmeleri.Add(eslestirme);
    await context.SaveChangesAsync();
    return eslestirme;
}

// Puantaj oluşturulurken Snapshot'u kullan
public async Task GenerateDailyPuantajAsync(FiloGuzergahEslestirme eslestirme)
{
    var puantaj = new FiloGunlukPuantaj
    {
        FiloGuzergahEslestirmeId = eslestirme.Id,
        // ... diğer alanlar

        // SNAPSHOT'TAN KULLAAAAN
        TahakkukEdenKurumUcreti = 
            (eslestirme.GuzergahSeferSayisiSnapshot ?? 2) * 
            (eslestirme.GuzergahBirimFiyatSnapshot ?? 150m) * 
            puantajCarpani,

        TahakkukEdenTaseronUcreti = 
            (eslestirme.GuzergahSeferSayisiSnapshot ?? 2) * 
            (eslestirme.GuzergahGiderFiyatSnapshot ?? 80m) * 
            puantajCarpani
    };

    context.FiloGunlukPuantajlar.Add(puantaj);
    await context.SaveChangesAsync();
}
```

---

## 4. SQL JOIN/Query Pattern'leri

### 4.1 Pattern 1: Şablondan Puantajları Getir

```sql
-- Belirli eşleştirmenin tüm puantajlarını getir
SELECT 
    fgp.*,
    gz.GuzergahAdi,
    ar.Plaka,
    sf.Ad AS SoforAdi,
    ca.Ad AS KurumAdi
FROM FiloGunlukPuantajlar fgp
LEFT JOIN FiloGuzergahEslestirmeleri fge ON fgp.FiloGuzergahEslestirmeId = fge.Id
LEFT JOIN Guzergahlar gz ON fgp.GuzergahId = gz.Id
LEFT JOIN Araclar ar ON fgp.AracId = ar.Id
LEFT JOIN Soforler sf ON fgp.SoforId = sf.Id
LEFT JOIN Cariler ca ON fgp.KurumFirmaId = ca.Id
WHERE fgp.FiloGuzergahEslestirmeId = 12
  AND YEAR(fgp.Tarih) = 2025
  AND MONTH(fgp.Tarih) = 1
ORDER BY fgp.Tarih;
```

### 4.2 Pattern 2: Şablon Bilgisiyle Puantaj Topla

```sql
-- Her güzergah-araç-şoför kombinasyonu için aylık özet
SELECT 
    fge.Id AS EslestirmeId,
    fge.GuzergahId,
    fge.AracId,
    fge.SoforId,
    fge.KurumFirmaId,
    fge.KurumaKesilecekUcret AS BirimFiyat,
    fge.TaseronaOdenenUcret AS GiderFiyat,
    COUNT(DISTINCT fgp.Tarih) AS CalisilanGunSayisi,
    SUM(CASE WHEN fgp.Durum = 1 THEN fgp.SeferSayisi ELSE 0 END) AS ToplamSefer,
    SUM(fgp.TahakkukEdenKurumUcreti) AS ToplamGelir,
    SUM(fgp.TahakkukEdenTaseronUcreti) AS ToplamGider,
    SUM(fgp.TahakkukEdenKurumUcreti) - SUM(fgp.TahakkukEdenTaseronUcreti) AS Kar
FROM FiloGuzergahEslestirmeleri fge
LEFT JOIN FiloGunlukPuantajlar fgp ON fge.Id = fgp.FiloGuzergahEslestirmeId
WHERE YEAR(fgp.Tarih) = 2025
  AND MONTH(fgp.Tarih) = 1
  AND fge.IsActive = 1
  AND fge.IsDeleted = 0
GROUP BY fge.Id, fge.GuzergahId, fge.AracId, fge.SoforId, fge.KurumFirmaId,
         fge.KurumaKesilecekUcret, fge.TaseronaOdenenUcret
ORDER BY fge.KurumFirmaId, fge.GuzergahId;

-- SONUÇ:
-- | EslestirmeId | GuzergahId | AracId | SoforId | KurumFirmaId | CalisilanGun | ToplamSefer | ToplamGelir | ToplamGider | Kar |
-- |    12        |     3      |   1    |   5     |     42       |     22       |     44      |   6.600     |   3.520     | 3.080 |
-- |    13        |     5      |   1    |   5     |     42       |     19       |     38      |   5.700     |   3.040     | 2.660 |
```

### 4.3 Pattern 3: Haftalık Özet (Raporlama)

```sql
-- Hafta bazında eşleştirme performansı
SELECT 
    fge.Id AS EslestirmeId,
    gz.GuzergahAdi,
    DATEPART(WEEK, fgp.Tarih) AS HaftaSayisi,
    COUNT(*) AS BitirileKayitlari,
    SUM(CASE WHEN fgp.Durum = 1 THEN 1 ELSE 0 END) AS TamamlananSefer,
    SUM(fgp.TahakkukEdenKurumUcreti) AS HaftalikGelir,
    SUM(fgp.TahakkukEdenTaseronUcreti) AS HaftalikGider
FROM FiloGuzergahEslestirmeleri fge
LEFT JOIN FiloGunlukPuantajlar fgp ON fge.Id = fgp.FiloGuzergahEslestirmeId
LEFT JOIN Guzergahlar gz ON fge.GuzergahId = gz.Id
WHERE YEAR(fgp.Tarih) = 2025
  AND MONTH(fgp.Tarih) = 1
GROUP BY fge.Id, gz.GuzergahAdi, DATEPART(WEEK, fgp.Tarih)
ORDER BY fge.Id, DATEPART(WEEK, fgp.Tarih);
```

---

## 5. Service Implementasyonu

### 5.1 Puantaj Oluşturma Service

```csharp
public class FiloKomisyonService : IFiloKomisyonService
{
    public async Task GenerateDailyPuantajsForTomorrowAsync()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var tomorrow = DateTime.UtcNow.AddDays(1).Date;

            // Step 1: Active eşleştirmeleri getir
            var eslestirmeler = await context.FiloGuzergahEslestirmeleri
                .Where(e => e.IsActive && !e.IsDeleted)
                .Include(e => e.Guzergah)
                .ToListAsync();

            foreach (var eslestirme in eslestirmeler)
            {
                // Step 2: Aynı gün kaydı var mı?
                var var existing = await context.FiloGunlukPuantajlar
                    .Where(p => p.FiloGuzergahEslestirmeId == eslestirme.Id &&
                               p.Tarih.Date == tomorrow &&
                               !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existing != null)
                    continue;

                // Step 3: Çarpan hesapla
                var carpani = CalculatePuantajCarpani(tomorrow);

                // Step 4: Sefer sayısı
                var seferSayisi = GetSeferSayisiFromServisTuru(eslestirme.ServisTuru);

                // Step 5: Puantaj oluştur
                var puantaj = new FiloGunlukPuantaj
                {
                    FirmaId = eslestirme.FirmaId,
                    FiloGuzergahEslestirmeId = eslestirme.Id,
                    Tarih = tomorrow,
                    KurumFirmaId = eslestirme.KurumFirmaId,
                    GuzergahId = eslestirme.GuzergahId,
                    AracId = eslestirme.AracId,
                    SoforId = eslestirme.SoforId,
                    ServisTuru = eslestirme.ServisTuru,
                    SeferSayisi = seferSayisi,
                    PuantajCarpani = carpani,
                    TahakkukEdenKurumUcreti = seferSayisi * eslestirme.KurumaKesilecekUcret * carpani,
                    TahakkukEdenTaseronUcreti = seferSayisi * eslestirme.TaseronaOdenenUcret * carpani,
                    Durum = OperasyonDurumu.Planli,
                    Onaylandi = false
                };

                context.FiloGunlukPuantajlar.Add(puantaj);
            }

            await context.SaveChangesAsync();
        }
    }

    private decimal CalculatePuantajCarpani(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Sunday)  // Pazar
            return 0.5m;
        if (IsPublicHoliday(date))  // Bayram
            return 0.0m;
        return 1.0m;  // Normal gün
    }

    private int GetSeferSayisiFromServisTuru(ServisTuru servisTuru)
    {
        return servisTuru switch
        {
            ServisTuru.Sabah => 1,
            ServisTuru.Aksam => 1,
            ServisTuru.SabahAksam => 2,
            ServisTuru.Mesai => 1,
            _ => 1
        };
    }
}
```

### 5.2 Puantaj Sorgulama Service

```csharp
public async Task<List<FiloGunlukPuantaj>> GetPuantajsByEslestirmeAsync(int eslestirmeId, int yil, int ay)
{
    using (var context = await _contextFactory.CreateDbContextAsync())
    {
        return await context.FiloGunlukPuantajlar
            .Where(p => p.FiloGuzergahEslestirmeId == eslestirmeId &&
                       p.Tarih.Year == yil &&
                       p.Tarih.Month == ay &&
                       !p.IsDeleted)
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .OrderBy(p => p.Tarih)
            .ToListAsync();
    }
}

public async Task<decimal> GetAylikToplamGelirAsync(int eslestirmeId, int yil, int ay)
{
    using (var context = await _contextFactory.CreateDbContextAsync())
    {
        return await context.FiloGunlukPuantajlar
            .Where(p => p.FiloGuzergahEslestirmeId == eslestirmeId &&
                       p.Tarih.Year == yil &&
                       p.Tarih.Month == ay &&
                       !p.IsDeleted)
            .SumAsync(p => p.TahakkukEdenKurumUcreti);
    }
}
```

---

## 6. Troubleshooting

### ❌ Problem: Puantaj tutarı yanlış

```
Eşleştirme: Eslestirme.Id = 12
├─ KurumaKesilecekUcret = 150 TL
├─ TaseronaOdenenUcret = 80 TL

Puantaj: FiloGunlukPuantaj.FiloGuzergahEslestirmeId = 12, Tarih = 2025-01-02
├─ SeferSayisi = 2
├─ PuantajCarpani = 1.0
├─ TahakkukEdenKurumUcreti = 280 TL ❌ (Beklenen: 300 TL = 2 × 150)
├─ TahakkukEdenTaseronUcreti = 160 TL ✓ (Doğru: 2 × 80)
```

**Teşhis**:

```sql
-- Eşleştirme kontrol
SELECT * FROM FiloGuzergahEslestirmeleri WHERE Id = 12;
-- KurumaKesilecekUcret = 150 (Görülüyor)

-- Puantaj kontrol
SELECT * FROM FiloGunlukPuantajlar 
WHERE FiloGuzergahEslestirmeId = 12 AND Tarih = '2025-01-02';
-- Sonuç: TahakkukEdenKurumUcreti = 280 (Hatalı)

-- Hesaplama kontrol
SELECT 2 * 150 * 1.0;  -- 300 (Beklenen)
```

**Çözüm**:

```csharp
// Service'de hesaplama hatası var
// Yanlış:
TahakkukEdenKurumUcreti = (seferSayisi * eslestirme.KurumaKesilecekUcret) - 20; // Bug!

// Doğru:
TahakkukEdenKurumUcreti = seferSayisi * eslestirme.KurumaKesilecekUcret * carpani;
```

---

**Teknik Referans - Veri Akışı v1.0 (2025-01-23)**
