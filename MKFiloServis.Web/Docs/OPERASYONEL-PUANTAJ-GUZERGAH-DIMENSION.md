# Operasyonel Puantaj - Güzergah Boyutu (Derinlemesine Analiz)

**Tarih**: 2025-01-23  
**Kapsam**: Güzergah Veri Modeli, İlişkiler, Akış & Raporlama  
**Hedef**: Personel Taşıması Puantaj Sisteminde Güzergahların Rol ve Uygulanması

---

## İçindekiler

1. [Güzergah Veri Modeli](#1-güzergah-veri-modeli)
2. [Güzergah-Puantaj İlişkisi & Veri Akışı](#2-güzergah-puantaj-ilişkisi--veri-akışı)
3. [Sefer Slot'u ve Kapasiteyi Yönetme](#3-sefer-slotu-ve-kapasiteyi-yönetme)
4. [Program Akışı Iyileştirmeleri](#4-program-akışı-iyileştirmeleri)
5. [Güzergah Bazında Raporlama](#5-güzergah-bazında-raporlama)
6. [Teknik Kontrol Listesi](#6-teknik-kontrol-listesi)

---

## 1. Güzergah Veri Modeli

### 1.1 Guzergah Entity - Tam Yapı

```csharp
public class Guzergah : BaseEntity, IKopyalanabilirTenant, IFirmaTenant
{
    // ===== TENANT =====
    public int? FirmaId { get; set; }              // Kiracı firma
    public virtual Firma? Firma { get; set; }

    // ===== TEMEL BİLGİLER =====
    public string GuzergahKodu { get; set; }       // Örn: "GZR001"
    public string GuzergahAdi { get; set; }        // Örn: "TRT Ankara Merkez → TRT Dış Ticaret"
    public string? BaslangicNoktasi { get; set; }  // Örn: "TRT Paşaköy, Ankara"
    public string? BitisNoktasi { get; set; }      // Örn: "TRT Dış Ticaret Şubesi"

    // ===== KOORDİNATLAR (HARİTA) =====
    public double? BaslangicLatitude { get; set; }
    public double? BaslangicLongitude { get; set; }
    public double? BitisLatitude { get; set; }
    public double? BitisLongitude { get; set; }
    public string? RotaRengi { get; set; } = "#3388ff"  // Harita görünümü için

    // ===== FİYATLANDIRMA =====
    public decimal BirimFiyat { get; set; }        // Gelir fiyatı (Kurumdan tahsil)
    public decimal GiderFiyat { get; set; }        // Gider fiyatı (Şoför/Araç maliyeti)
    public decimal PuantajCarpani { get; set; } = 1.0m  // Hafta sonu/İzin çarpanı

    // ===== LOJİSTİK =====
    public decimal? Mesafe { get; set; }           // km cinsinden
    public int? TahminiSure { get; set; }          // dakika
    public int PersonelSayisi { get; set; }        // Ortalama personel yük
    public string? KapasiteAdi { get; set; }       // Örn: "16+1", "27+1"

    // ===== SEFER TİPİ =====
    public SeferTipi SeferTipi { get; set; } = SeferTipi.SabahAksam;
    public enum SeferTipi
    {
        Sabah = 1,        // Sadece sabah servisi
        Aksam = 2,        // Sadece akşam servisi
        SabahAksam = 3,   // Her gün sabah + akşam
        Saatlik = 4,      // Saatlik şoför vs.
        Mesai = 5,        // Vardiya mesaisi
        Vardiya = 6       // Vardiya süresi
    }

    // ===== VARSAYILAN YAPILANDI (TEMPLATE) =====
    public int? VarsayilanAracId { get; set; }
    public virtual Arac? VarsayilanArac { get; set; }
    public int? VarsayilanSoforId { get; set; }
    public virtual Sofor? VarsayilanSofor { get; set; }

    // ===== MUHASEBE / FATURA =====
    public int? FaturaKalemId { get; set; }        // Hangi fatura kaleminden oluşturuldu
    public int CariId { get; set; }                // Müşteri / Kurum (Eski uyumluluk)
    public int? KurumId { get; set; }              // Kurum tablosundan ilişki (Yeni)
    public virtual Kurum? Kurum { get; set; }

    // ===== DURUM =====
    public bool Aktif { get; set; } = true;
    public string? Notlar { get; set; }

    // ===== NAVIGATION PROPERTIES =====
    public virtual Cari Cari { get; set; } = null!;
    public virtual ICollection<ServisCalisma> ServisCalismalari { get; set; } = new();
    public virtual ICollection<AracMasraf> AracMasraflari { get; set; } = new();
    public virtual ICollection<FiloGuzergahEslestirme> AracEslestirmeleri { get; set; } = new();
}
```

### 1.2 Guzergah Alanlarının Analiz Tablosu

| Alan | Tip | Amaç | Örnek | Önem |
|------|-----|------|-------|------|
| **GuzergahKodu** | string | Sistem tanımlayıcısı | GZR001, GZR-TRT-001 | 🔴 Kritik |
| **GuzergahAdi** | string | İnsan okunabilir ad | "TRT Merkez-Dış Ticaret" | 🔴 Kritik |
| **BirimFiyat** | decimal | Kurumdan tahsil edilen /sefer | 150 TL | 🔴 Kritik |
| **GiderFiyat** | decimal | Şoför/Araç maliyeti /sefer | 80 TL |🔴 Kritik |
| **Mesafe** | decimal? | Rota kilometresi | 12.5 km | 🟡 Opsiyonel |
| **TahminiSure** | int? | Beklenen seyahat süresi | 30 dk | 🟡 Raporlama |
| **SeferTipi** | enum | Sabah/Akşam/Sabah-Akşam | SabahAksam | 🔴 Kritik |
| **PersonelSayisi** | int | Ortalama yolcu sayısı | 12 kişi | 🟡 İstatistik |
| **PuantajCarpani** | decimal | Hafta sonu / İzin çarpanı | 1.0 / 0.5 | 🟡 Hesaplama |
| **Aktif** | bool | Kullanımda mı? | true/false | 🔴 Kritik |

### 1.3 Güzergah Hiyerarşisi

```
┌────────────────────────────────────────┐
│          Firma (Kiracı)                │
│      E.g. MKFiloServis Ltd.            │
└────────────────────────────────────────┘
                    │
                    │ 1-to-Many
                    ▼
        ┌──────────────────────┐
        │  Guzergah (Rota)     │
        │  ├─ GuzergahKodu     │
        │  ├─ GuzergahAdi      │
        │  ├─ BirimFiyat       │
        │  ├─ SeferTipi        │
        │  └─ Koordinatlar     │
        └──────────────────────┘
                    │
        ┌───────────┼───────────┐
        │           │           │
        ▼           ▼           ▼
    (A)         (B)         (C)
  Sefer      Essay      Araç
  Detay      lestirme   Masraf
  (Slot)     (Template) (Anomali)
```

---

## 2. Güzergah-Puantaj İlişkisi & Veri Akışı

### 2.1 Veri Akışı (Uçtan Uca)

```
START (Sabah 06:00)
  │
  ├─ [ADIM 1] Güzergah Seçimi
  │  │
  │  ├─ Guzergah.Id = 3
  │  │  ├─ Kod: GZR-TRT-MERKEZ
  │  │  ├─ Adı: TRT Ankara Merkez
  │  │  ├─ BirimFiyat: 150 TL
  │  │  ├─ SeferTipi: SabahAksam
  │  │  └─ PersonelSayisi: 12
  │  │
  │  └─ Sistem: Guzergah.VarsayilanArac ve VarsayilanSofor yükle
  │     ├─ Araç: 34CX8 (Ford Transit)
  │     └─ Şoför: Ali Demir (ID: 5)
  │
  ├─ [ADIM 2] FiloGuzergahEslestirme (Şablon) Kontrolü
  │  │
  │  └─ SELECT FROM FiloGuzergahEslestirme WHERE
  │       GuzergahId = 3 AND AracId = 34CX8 AND SoforId = 5
  │       AND IsActive = true AND Tarih_bugun_araliginda
  │       RESULT: Eslestirme ID = 12
  │       │
  │       ├─ KurumaKesilecekUcret = 150 TL
  │       ├─ TaseronaOdenenUcret = 80 TL
  │       └─ ServisTuru = SabahAksam
  │
  ├─ [ADIM 3] FiloGunlukPuantaj Kaydı Oluştur
  │  │
  │  └─ INSERT FiloGunlukPuantaj:
  │       Tarih = 2025-01-23
  │       GuzergahId = 3
  │       AracId = 34CX8 (Plaka)
  │       SoforId = 5
  │       KurumFirmaId = 42 (TRT-ANKARA Cari)
  │       FiloGuzergahEslestirmeId = 12
  │       ServisTuru = SabahAksam
  │       SeferSayisi = 2 (sabah + akşam)
  │       PuantajCarpani = 1.0
  │       Durum = Planli
  │       TahakkukEdenKurumUcreti = 2 × 150 = 300 TL
  │       TahakkukEdenTaseronUcreti = 2 × 80 = 160 TL
  │
  ├─ [ADIM 4] Çalışma Anlık Sistem (QR/SMS)
  │  │
  │  ├─ 06:30: Şoför "Başladı" QR kodunu tara
  │  ├─ 17:30: Şoför "Bitti" QR kodunu tara
  │  │
  │  └─ Sistem: Durum = Gitti (Tamamlandı)
  │
  ├─ [ADIM 5] Terminal Kontrolü (Araç Muayene)
  │  │
  │  ├─ Şoför KM sayaçı kontrol: Başlangıç: 15.420 km, Bitiş: 15.432 km ✓
  │  ├─ Yakıt kontrolü: OK ✓
  │  ├─ Temizlik: OK ✓
  │  ├─ Hasar yok ✓
  │  │
  │  └─ Terminal operatörü: "Tamam, ONAYLA" → FiloGunlukPuantaj.Onaylandi = true
  │
  ├─ [ADIM 6] Sistem Finalizasyonu
  │  │
  │  └─ Sistem otomatik:
  │       ├─ FiloGunlukPuantaj.Durum = Gitti (Tamamlandı)
  │       ├─ FiloGunlukPuantaj.OnayTarihi = NOW()
  │       ├─ FiloGunlukPuantaj.TahakkukEdenKurumUcreti = 300 TL (değişmedi)
  │       ├─ FiloGunlukPuantaj.TahakkukEdenTaseronUcreti = 160 TL (değişmedi)
  │       │
  │       └─ MUHASEBE SESİ: "Hakedis daha hesaplanmadı, ay 28'de oto-hesaplanacak"
  │
  └─ END (17:45) ✓

─────────────────────────────────────────────────────
AY SONU (28-29 Ocak) - HakedisPuantaj Oluşturma
─────────────────────────────────────────────────────

START (Job: AutoGenerateHakedisPuantaj)
  │
  ├─ [ADIM 7] Güzergah Bazında Topla
  │  │
  │  └─ SELECT DISTINCT GuzergahId FROM FiloGunlukPuantaj
  │       WHERE Yil = 2025 AND Ay = 1 AND Durum IN ('Gitti', ...)
  │       AND Onaylandi = true
  │       RESULT: Güzergah 3, 5, 7, 9
  │
  ├─ [ADIM 8] Her Güzergahı Temiz
  │  │
  │  ├─ Güzergah 3 (TRT Merkez):
  │  │  │
  │  │  ├─ SELECT SUM(SeferSayisi * PuantajCarpani) FROM FiloGunlukPuantaj
  │  │  │  WHERE GuzergahId = 3 AND Yil = 2025 AND Ay = 1
  │  │  │  RESULT: ToplamSefer = 44 (22 gün × 2 sefer)
  │  │  │
  │  │  ├─ Guzergah birimFiyat = 150 TL (Kurumdan)
  │  │  ├─ Guzergah GiderFiyat = 80 TL (Tedarikçiye)
  │  │  │
  │  │  └─ INSERT HakedisPuantaj:
  │  │      │
  │  │      ├─ Guzergah: GZR-TRT-MERKEZ
  │  │      ├─ ToplamSefer = 44
  │  │      ├─ GelirToplam = 44 × 150 = 6.600 TL
  │  │      ├─ GiderToplam = 44 × 80 = 3.520 TL
  │  │      ├─ KDV = 3.520 × 0.20 = 704 TL
  │  │      ├─ OdenecekTutar = 3.520 + 704 = 4.224 TL
  │  │      └─ Durum = Taslak
  │  │
  │  ├─ Güzergah 5 (TRT Çiftçiler Pazarı):
  │  │  │
  │  │  ├─ ToplamSefer = 38 (19 gün × 2 sefer)
  │  │  ├─ GelirToplam = 38 × 150 = 5.700 TL
  │  │  ├─ GiderToplam = 38 × 80 = 3.040 TL
  │  │  ├─ KDV = 3.040 × 0.20 = 608 TL
  │  │  └─ OdenecekTutar = 3.648 TL
  │  │
  │  └─ [BENZER ŞEKILDE 7, 9 GÜZERGAHLARı İŞLE]
  │
  ├─ [ADIM 9] Hakedis (Müşteri Toplamı) Oluştur
  │  │
  │  ├─ Güzergahlardan CariId'yi topla: 42 (TRT-ANKARA)
  │  │
  │  ├─ SELECT SUM(GelirToplam), SUM(toplamSefer), SUM(KdvTutari)
  │  │  FROM HakedisPuantaj WHERE CariId = 42 AND Yil = 2025 AND Ay = 1
  │  │  RESULT:
  │  │  ├─ ToplamSeferSayisi = 122 (Tüm güzergahlar)
  │  │  ├─ Tutar = 18.140 TL (Gelir)
  │  │  ├─ KdvTutar = 3.712 TL
  │  │  └─ GenelToplam = 21.852 TL
  │  │
  │  └─ INSERT Hakedis:
  │      │
  │      ├─ Yil = 2025, Ay = 1
  │      ├─ Tip = 'Kurum'
  │      ├─ ReferansId = 42 (TRT-ANKARA)
  │      ├─ ToplamSeferSayisi = 122
  │      ├─ Tutar = 18.140 TL
  │      ├─ GenelToplam = 21.852 TL
  │      ├─ Durum = Taslak
  │      └─ Bildirim: "TRT-ANKARA için Ocak Hakedişi oluşturuldu"
  │
  └─ END ✓ (Ay 29'de operasyon müdürü kontrol)

```

### 2.2 Güzergah Bilgileri Puantajda Tutulması

**İlişki Örneği**:

```
FiloGunlukPuantaj (Günlük)
│
├─ GuzergahId = 3 (FK: Guzergah)
│  │
│  └─ NavigationProperty: Guzergah
│     ├─ GuzergahKodu = "GZR-TRT-MERKEZ"
│     ├─ GuzergahAdi = "TRT Merkez → TRT Dış Ticaret"
│     ├─ BirimFiyat = 150 TL (Kurumdan tahsil)
│     ├─ GiderFiyat = 80 TL (Şoför maliyeti)
│     ├─ Mesafe = 12.5 km
│     ├─ TahminiSure = 30 dakika
│     └─ SeferTipi = SabahAksam
│
└─ Hesaplama sonucu:
   ├─ TahakkukEdenKurumUcreti = SeferSayisi × BirimFiyat × PuantajCarpani
   │  = 2 × 150 × 1.0 = 300 TL
   │
   └─ TahakkukEdenTaseronUcreti = SeferSayisi × GiderFiyat × PuantajCarpani
      = 2 × 80 × 1.0 = 160 TL
```

---

## 3. Sefer Slot'u ve Kapasiteyi Yönetme

### 3.1 GuzergahSefer Entity

```csharp
public class GuzergahSefer : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }              // Tenant
    public virtual Firma? Firma { get; set; }

    public int GuzergahId { get; set; }            // FK: Guzergah
    public virtual Guzergah? Guzergah { get; set; }

    // ===== SEFER SLOT DETAY =====
    public int Sira { get; set; }                  // 1. sefer, 2. sefer, 3. sefer ...
    public SeferTipi SeferTipi { get; set; }       // Sabah, Akşam, Mesai ...
    public SeferSlot Slot { get; set; }            // Operasyonel slot: Sabah, Aksam, Mesai, Diger1-5

    public enum SeferSlot
    {
        Sabah = 1,        // 06:00-08:00
        Aksam = 2,        // 17:00-19:00
        Og = 3,           // 12:00-13:00
        Mesai = 4,        // 08:00-17:00
        Diger1 = 5,       // Özel slot 1
        Diger2 = 6,       // Özel slot 2
        Diger3 = 7,       // Özel slot 3
        Diger4 = 8,
        Diger5 = 9
    }

    // ===== KAPASİTE =====
    public string? KapasiteAdi { get; set; }       // Örn: "16+1" (16 yolcu + 1 şoför)

    // ===== ARAÇ VE ŞOFÖR (SLOT SPESİFİK) =====
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    public string? SoforAd { get; set; }            // Serbest metin (opsiyonel)
    public string? SoforTelefon { get; set; }       // İletişim

    // ===== TEDARIKÇI =====
    [Column("Firma")]                              // DB uyumluluk
    public string? FirmaAdiSerbest { get; set; }   // Tedarikçi adı
}
```

### 3.2 Sefer Slot Yönetimi

**Bir güzergahta birden fazla slot olabilir**:

```
Güzergah: GZR-TRT-MERKEZ (ID: 3)
├─ SeferTipi: SabahAksam
│
├─ [SEFER 1] Sabah Servisi
│  ├─ Sira: 1
│  ├─ Slot: Sabah (06:00)
│  ├─ Kapasite: 16+1
│  ├─ Araç: 34CX8 (Ford Transit)
│  └─ Şoför: Ali Demir
│
├─ [SEFER 2] Akşam Servisi
│  ├─ Sira: 2
│  ├─ Slot: Aksam (17:00)
│  ├─ Kapasite: 16+1
│  ├─ Araç: 34CX8 (Ford Transit)
│  └─ Şoför: Ali Demir
│
└─ [SEFER 3 - OPSIYONEL] Öğle Servisi
   ├─ Sira: 3
   ├─ Slot: Og (12:00)
   ├─ Kapasite: 8+1
   ├─ Araç: 34DX5 (Minibüs)
   └─ Şoför: Mehmet Yılmaz
```

**Veri Tabanı Sorgusu**:

```sql
-- Bir güzergahın tüm sefer slotlarını getir
SELECT * FROM GuzergahSefer
WHERE GuzergahId = 3
ORDER BY Sira;

-- Sonuç:
-- | Sira | Slot | KapasiteAdi | AracId | SoforAd |
-- |  1   |  1   |   "16+1"    | 1      | "Ali"   |
-- |  2   |  2   |   "16+1"    | 1      | "Ali"   |
-- |  3   |  3   |    "8+1"    | 2      | "Mehmet"|
```

### 3.3 Kapasite Optimizasyonu

**Senaryolar**:

| Güzergah | Personel | KapasiteAdi | Araç | Şoför | Maliyet/Gün |
|----------|----------|-------------|------|-------|------------|
| TRT Merkez | 22 kişi | 16+1 | Otobüs | Ali | 300 TL |
| TRT Merkez | 22 kişi | 16+1 + 8+1 | 2× Minibüs | Ali+Mehmet | 400 TL |
| İSKİ Ankara | 8 kişi | 8+1 | Minibüs | Veli | 150 TL |
| ESHOT | 30 kişi | 27+1 | Lüks Otobüs | Ahmet | 450 TL |

---

## 4. Program Akışı Iyileştirmeleri

### 4.1 Mevcut Akış (Sorunlar)

```
❌ PROBLEM 1: Güzergah Fiyat Güncellemesi
   └─ Guzergah.BirimFiyat = 150 TL → 160 TL değişirse
      └─ Daha sonra oluşturulan FiloGunlukPuantaj
      │  (yeni) yeni fiyatı alır ✓ (Doğru)
      │
      └─ Ama eski Eşleştirmeler eski fiyatı tutuyor
      │  (Değişmedi) → Raporlama uyumsuzluğu ❌
```

### Solution 1A: Snapshot Pattern (Önerilen)

```csharp
public class FiloGunlukPuantaj : BaseEntity, IFirmaTenant
{
    // Mevcut alanlar...

    // YENİ: Snapshot alanları
    public decimal GuzergahBirimFiyatSnapshot { get; set; }   // 150 TL
    public decimal GuzergahGiderFiyatSnapshot { get; set; }   // 80 TL
    public decimal GuzergahPuantajCarpaniSnapshot { get; set; } = 1.0m

    // Fiyat değişimi izlemesi
    [NotMapped]
    public bool FiyatEklendi => 
        (GuzergahBirimFiyatSnapshot != Guzergah!.BirimFiyat) ||
        (GuzergahGiderFiyatSnapshot != Guzergah!.GiderFiyat);
}
```

**Implementasyon**:

```csharp
// FiloKomisyonService.cs
public async Task CreateDailyPuantajAsync(int guzergahId, int aracId, int soforId, int kurumId)
{
    var guzergah = await context.Guzergahlar.FindAsync(guzergahId);

    var puantaj = new FiloGunlukPuantaj
    {
        GuzergahId = guzergahId,
        AracId = aracId,
        SoforId = soforId,
        KurumFirmaId = kurumId,

        // Snapshot al
        GuzergahBirimFiyatSnapshot = guzergah!.BirimFiyat,
        GuzergahGiderFiyatSnapshot = guzergah.GiderFiyat,
        GuzergahPuantajCarpaniSnapshot = guzergah.PuantajCarpani,

        // Hesapla
        TahakkukEdenKurumUcreti = 2 * guzergah.BirimFiyat * guzergah.PuantajCarpani,
        TahakkukEdenTaseronUcreti = 2 * guzergah.GiderFiyat * guzergah.PuantajCarpani,
    };

    context.FiloGunlukPuantajlar.Add(puantaj);
    await context.SaveChangesAsync();
}
```

### 4.2 Enhancement 2: Güzergah Validasyon

```csharp
public class GuzergahValidasyonService
{
    // ✓ Güzergah active mı?
    // ✓ Fiyat > 0 mı?
    // ✓ Mesafe tanımlandı mı?
    // ✓ Başlangıç/Bitiş noktaları tanımlandı mı?

    public async Task<List<string>> ValidateGuzergahAsync(int guzergahId)
    {
        var errors = new List<string>();
        var gz = await context.Guzergahlar.FindAsync(guzergahId);

        if (gz == null)
            errors.Add("Güzergah bulunamadı");
        else
        {
            if (!gz.Aktif)
                errors.Add("⚠ Güzergah pasif");
            if (gz.BirimFiyat <= 0)
                errors.Add("❌ Kurumdan tahsil fiyatı 0");
            if (gz.GiderFiyat <= 0)
                errors.Add("❌ Gider fiyatı 0");
            if (string.IsNullOrEmpty(gz.BaslangicNoktasi))
                errors.Add("⚠ Başlangıç noktası tanımlanmadı");
            if (string.IsNullOrEmpty(gz.BitisNoktasi))
                errors.Add("⚠ Bitiş noktası tanımlanmadı");
            if (gz.VarsayilanAracId == null)
                errors.Add("❌ Varsayılan araç tanımlanmadı");
        }

        return errors;
    }
}
```

### 4.3 Enhancement 3: Güzergah Değişim Notifikasyonu

```csharp
public class GuzergahChangeNotificationService
{
    // Güzergah fiyatı değişirse
    // → Etkili Eşleştirmeleri (aktif olanları) bulup izle
    // → Operasyon müdürüne bildiri gönder

    public async Task NotifyGuzergahChangeAsync(Guzergah oldGuzergah, Guzergah newGuzergah)
    {
        var changes = new StringBuilder();

        if (oldGuzergah.BirimFiyat != newGuzergah.BirimFiyat)
            changes.AppendLine($"Kurumdan tahsil fiyatı: {oldGuzergah.BirimFiyat} → {newGuzergah.BirimFiyat}");

        if (oldGuzergah.GiderFiyat != newGuzergah.GiderFiyat)
            changes.AppendLine($"Gider fiyatı: {oldGuzergah.GiderFiyat} → {newGuzergah.GiderFiyat}");

        if (changes.Length > 0)
        {
            var eslestirmeler = await context.FiloGuzergahEslestirmeleri
                .Where(e => e.GuzergahId == newGuzergah.Id && e.IsActive)
                .ToListAsync();

            // Email/Notification gönder
            await _notificationService.SendAsync(
                to: "operasyon@firma.com",
                subject: $"Güzergah Güncelleme: {newGuzergah.GuzergahAdi}",
                body: $"Aşağıdaki değişiklikler yapıldı:\n{changes}\n\n" +
                      $"Etkilenen eşleştirmeler: {eslestirmeler.Count} adet"
            );
        }
    }
}
```

---

## 5. Güzergah Bazında Raporlama

### 5.1 Güzergah Performans Raporu

**SQL Sorgusu**:

```sql
-- Güzergah bazında aylık özet
SELECT 
    gz.GuzergahKodu,
    gz.GuzergahAdi,
    gz.BirimFiyat AS KurumFiyat,
    gz.GiderFiyat AS TaseronFiyat,
    COUNT(DISTINCT fgp.Tarih) AS CalisilanGunSayisi,
    SUM(CASE WHEN fgp.Durum = 1 THEN fgp.SeferSayisi ELSE 0 END) AS ToplamSefer,
    SUM(fgp.TahakkukEdenKurumUcreti) AS ToplGelir,
    SUM(fgp.TahakkukEdenTaseronUcreti) AS ToplamGider,
    SUM(fgp.TahakkukEdenKurumUcreti) - SUM(fgp.TahakkukEdenTaseronUcreti) AS Kar
FROM Guzergahlar gz
LEFT JOIN FiloGunlukPuantajlar fgp ON gz.Id = fgp.GuzergahId
WHERE YEAR(fgp.Tarih) = 2025 AND MONTH(fgp.Tarih) = 1
GROUP BY gz.Id, gz.GuzergahKodu, gz.GuzergahAdi, gz.BirimFiyat, gz.GiderFiyat
ORDER BY gz.GuzergahAdi;

-- ÖRNEK SONUÇ:
-- | GuzergahKodu | GuzergahAdi | KurumFiyat | TaseronFiyat | CalisilanGun | ToplamSefer | ToplamGelir | ToplamGider | Kar |
-- |--------- ------|--------|--------|--------|--------|--------|--------|--------|--------|
-- | GZR001 | TRT Merkez | 150 | 80 | 22 | 44 | 6.600 | 3.520 | 3.080 |
-- | GZR002 | TRT Çiftçiler | 150 | 80 | 19 | 38 | 5.700 | 3.040 | 2.660 |
-- | GZR003 | İSKİ Ankara | 120 | 60 | 21 | 42 | 5.040 | 2.520 | 2.520 |
-- |TOPLAM | | | | 62 | 124 | 17.340 | 9.080 | 8.260 |
```

### 5.2 Blazor Raporlama Bileşeni (Tasarım)

```razor
@* GuzergahPerformansRaporu.razor *@
@using MKFiloServis.Shared.Entities

<div class="card">
    <div class="card-header">
        <h5>Güzergah Bazında Aylık Performans</h5>
        <div class="filter-group">
            <InputSelect @bind-Value="SelectedYear" class="form-control">
                <option value="2025">2025</option>
            </InputSelect>
            <InputSelect @bind-Value="SelectedMonth" class="form-control">
                @for (int i = 1; i <= 12; i++)
                {
                    <option value="@i">@System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)</option>
                }
            </InputSelect>
            <button class="btn btn-primary" @onclick="RefreshReport">
                Raporu Güncelle
            </button>
        </div>
    </div>

    <div class="card-body">
        @if (reportData == null)
        {
            <p>Yükleniyor...</p>
        }
        else if (reportData.Count == 0)
        {
            <p class="text-muted">Bu dönem için veri bulunamadı.</p>
        }
        else
        {
            <table class="table table-striped">
                <thead>
                    <tr>
                        <th>Güzergah Kodu</th>
                        <th>Güzergah Adı</th>
                        <th>Çalışılan Gün</th>
                        <th>Toplam Sefer</th>
                        <th class="text-right">Gelir (TL)</th>
                        <th class="text-right">Gider (TL)</th>
                        <th class="text-right">Kar (TL)</th>
                        <th class="text-center">Marj %</th>
                        <th>%</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var row in reportData)
                    {
                        <tr>
                            <td>@row.GuzergahKodu</td>
                            <td>
                                <span class="badge" style="background-color: @row.RotaRengi">●</span>
                                @row.GuzergahAdi
                            </td>
                            <td>@row.CalisilanGunSayisi</td>
                            <td>@row.ToplamSefer</td>
                            <td class="text-right">@row.TotalGelir.ToString("N2")</td>
                            <td class="text-right">@row.TotalGider.ToString("N2")</td>
                            <td class="text-right text-success font-weight-bold">@row.Kar.ToString("N2")</td>
                            <td class="text-center">
                                @if (row.Kar > 0)
                                {
                                    <span class="badge badge-success">
                                        @Math.Round((row.Kar / row.TotalGelir * 100), 1)%
                                    </span>
                                }
                                else
                                {
                                    <span class="badge badge-danger">Zarar</span>
                                }
                            </td>
                            <td>
                                <button class="btn btn-sm btn-info" @onclick="() => ShowDetail(row.GuzergahId)">
                                    Detay
                                </button>
                            </td>
                        </tr>
                    }
                    <tr class="table-active">
                        <th colspan="2">TOPLAM</th>
                        <th>@reportData.Sum(r => r.CalisilanGunSayisi)</th>
                        <th>@reportData.Sum(r => r.ToplamSefer)</th>
                        <th class="text-right">@reportData.Sum(r => r.TotalGelir).ToString("N2")</th>
                        <th class="text-right">@reportData.Sum(r => r.TotalGider).ToString("N2")</th>
                        <th class="text-right text-success font-weight-bold">
                            @reportData.Sum(r => r.Kar).ToString("N2")
                        </th>
                        <th class="text-center">
                            @{
                                var totalGelir = reportData.Sum(r => r.TotalGelir);
                                var totalKar = reportData.Sum(r => r.Kar);
                                <span class="badge badge-success">
                                    @Math.Round((totalKar / totalGelir * 100), 1)%
                                </span>
                            }
                        </th>
                        <th></th>
                    </tr>
                </tbody>
            </table>
        }
    </div>
</div>

@code {
    private List<GuzergahRaporSatiri> reportData = null!;
    private int SelectedYear = DateTime.Now.Year;
    private int SelectedMonth = DateTime.Now.Month;

    protected override async Task OnInitializedAsync()
    {
        await RefreshReport();
    }

    private async Task RefreshReport()
    {
        reportData = await guzergahService.GetGuzergahRaporuAsync(SelectedYear, SelectedMonth);
    }

    private async Task ShowDetail(int guzergahId)
    {
        // Modal açarak günlük detayları göster
    }
}

public class GuzergahRaporSatiri
{
    public int GuzergahId { get; set; }
    public string? GuzergahKodu { get; set; }
    public string? GuzergahAdi { get; set; }
    public string? RotaRengi { get; set; }
    public int CalisilanGunSayisi { get; set; }
    public int ToplamSefer { get; set; }
    public decimal TotalGelir { get; set; }
    public decimal TotalGider { get; set; }
    public decimal Kar => TotalGelir - TotalGider;
}
```

### 5.3 Güzergah Karşılaştırması (Trend Analizi)

```
TRT Merkez Güzergahı - 6 Aylık Trend

Ay       | Sefer | Gelir  | Gider | Kar   | Marj %
---------|-------|--------|-------|-------|--------
Eylül    | 88    | 13.200 | 7.040 | 6.160 | 46,7%
Ekim     | 92    | 13.800 | 7.360 | 6.440 | 46,7%
Kasım    | 90    | 13.500 | 7.200 | 6.300 | 46,7%
Aralık   | 82    | 12.300 | 6.560 | 5.740 | 46,7%
Ocak     | 44    | 6.600  | 3.520 | 3.080 | 46,7%
Şubat*   | -     | -      | -     | -     | -

📊 TREND: Ocak'ta sefer sayısı yarı yarıya düştü
   Muhtemel Sebep: İzin takvimi (Yılbaşı), İş durgunluğu
   Eylem: Müşteriye danışmanlık
```

---

## 6. Teknik Kontrol Listesi

### 6.1 Güzergah Veri Kalitesi Controlü

```
✓ GÜNLÜK CHECKLIST (Terminal Operatörü)
  └─ [ ] Tüm güzergahlar aktif mi? (GuzergahAktif = true)
  └─ [ ] Fiyatlar tanımlanmış mı? (BirimFiyat > 0, GiderFiyat > 0)
  └─ [ ] Başlangıç/Bitiş noktaları uygun mu?
  └─ [ ] Sefer slotları tanımlanmış mı? (GuzergahSefer)
  └─ [ ] Varsayılan araç atanmış mı?

✓ HAFTALIK KONTROL (Operasyon Müdürü)
  └─ [ ] Güzergah fiyatı işletme karlılığını sağlıyor mu?
  └─ [ ] Anomali / İzin tutarlı mı?
  └─ [ ] Puantaj karşılaştırması: Planı vs Gerçek

✓ AYLIK KONTROL (Muhasebe)
  └─ [ ] Hakedis hesaplamada güzergah fiyatları uygunmu?
  └─ [ ] KDV hesaplama doğru mu?
  └─ [ ] Fatura kalemlerine eşleme yapıldı mı?
  └─ [ ] Karşılaştırma: Fatura tutarı = Hakedis tutarı?

✓ YILLIK KONTROL (Üst Yönetim)
  └─ [ ] Güzergah performans trendleri
  └─ [ ] Yeni güzergah ihtiyacı var mı?
  └─ [ ] Fiyat revizyonu gerekli mi?
  └─ [ ] KDV / Vergi ödeme denetimi
```

### 6.2 Güzergah Model Validasyon (Code)

```csharp
public class GuzergahValidator : AbstractValidator<Guzergah>
{
    public GuzergahValidator()
    {
        RuleFor(g => g.GuzergahKodu)
            .NotEmpty().WithMessage("Güzergah kodu gerekli")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Kod formatı uygun değil");

        RuleFor(g => g.GuzergahAdi)
            .NotEmpty().WithMessage("Güzergah adı gerekli")
            .Length(5, 255).WithMessage("Ad uzunluğu 5-255 karakter olmalı");

        RuleFor(g => g.BirimFiyat)
            .GreaterThan(0).WithMessage("Kurumdan tahsil fiyatı > 0 olmalı");

        RuleFor(g => g.GiderFiyat)
            .GreaterThan(0).WithMessage("Gider fiyatı > 0 olmalı")
            .LessThan(g => g.BirimFiyat).WithMessage("Gider fiyatı < Gelir fiyatı olmalı");

        RuleFor(g => g.Mesafe)
            .GreaterThan(0).When(g => g.Mesafe.HasValue)
            .WithMessage("Mesafe > 0 olmalı");

        RuleFor(g => g.VarsayilanAracId)
            .NotNull().WithMessage("Varsayılan araç tanımlanmalı");
    }
}
```

---

## ÖZET

| Bölüm | Bulgu | Uyum |
|-------|-------|------|
| **Güzergah Modeli** | Güzergah tüm operasyonel boyutları taşıyr (fiyat, sefer tipi, koord.) | ✅ İyi |
| **Veri Akışı** | Güzergah → Eşleştirme → Puantaj → Hakedis sıralanması mantıklı | ✅ İyi |
| **Snapshot Pattern** | Fiyat değişim izi için snapshot önerildi | ⚠ Opsiyonel |
| **Sefer Slot** | GuzergahSefer ile multi-slot yönetimi var | ✅ Var |
| **Raporlama** | Güzergah bazlı raporlama tasarlandı | ✅ Tasarlandı |
| **Program Akışı** | Notifikasyon ve validasyon iyileştirmeleri önerildi | ⚠ Opsiyonel |

---

**Tarihçe**  
- v1.0 (2025-01-23): İlk versiyon - Güzergah dimension analiz tamamlandı
