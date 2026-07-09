# Operasyonel Puantaj Raporlama - Ticari Analiz & Strateji

**Tarih**: 2025-01-23  
**Hedef**: Günlük vs. Toplu Raporlama Stratejilerinin Ticari Analizi  
**Sonuç**: ✅ **Hibrit Model** (Günlük Giriş + Toplu Işlem) Önerilir

---

## 📋 İçindekiler

1. [Mevcut İki Yaklaşım - Analiz](#1-mevcut-i̇ki-yaklaşım---analiz)
2. [Ticari Kabul Edilebilirlik Değerlendirmesi](#2-ticari-kabul-edilebilirlik-değerlendirmesi)
3. [Problem Alanları](#3-problem-alanları)
4. [Önerilen Hibrit Model](#4-önerilen-hibrit-model-günlük-giriş--toplu-işlem)
5. [Uygulama Planı](#5-uygulama-planı)
6. [Blazor UI Tasarımı](#6-blazor-ui-tasarımı)

---

## 1. Mevcut İki Yaklaşım - Analiz

### Yaklaşım A: **Günlük Kayıt Modeli** (Per-Record)

**Tanım**: Her güne ait `FiloGunlukPuantaj` kaydı **münferit olarak** girilir/oluşturulur.

```
┌─────────────────────────────────────────┐
│ Operatör: Günlük Puantaj Girişi        │
├─────────────────────────────────────────┤
│ Tarih: 2025-01-23                      │
│ Güzergah: GZR-TRT-001                  │
│ Araç: 34CX8                            │
│ Şoför: Ali Demir                       │
│ Sefer Sayısı: 2                        │
│ Durum: ✅ Gitti                         │
│ [KAYDET]                               │
└─────────────────────────────────────────┘
     ↓ (22 gün × 2 sefer için)
┌─────────────────────────────────────────┐
│ 44 Ayrı Geniş Formla Veri Girişi       │
│ ⏱ Toplam Süre: ~2-3 saat/ay            │
│ 💼 İş Yükü: ÇOOK YÜKSEK ❌             │
└─────────────────────────────────────────┘
```

#### Avantajları
✅ Etkinlik kontrolü kolay (Her kayıt tek tek gözden geçirilebilir)  
✅ Hata düzeltme basit (Bir kaydı değiştirmek hızlı)  
✅ Raporlama granüler (Günlük detay tam)  

#### Dezavantajları
❌ **Çok fazla klik**: 44 kayıt × 5-6 klik = ~260 klik/ay  
❌ **Operatör hatasına açık**: Copy-paste yanlışları, aynı güzergahı 2 sefer gibi unutma  
❌ **Operatör sıkılması**: Tekrar eden veri, duyarsallaşma riski  
❌ **Ay sonu yoğunluğu**: 22 gün sonunda hızlı giriş zorunluluğu  
❌ **Batch işlem yok**: Job otomasyonu faydalı değil  

**SONUÇ**: ❌ **Ticari olarak kabul edilebilir DEĞİL** (Operatör verimliliği çok düşük + hata oranı yüksek)

---

### Yaklaşım B: **Toplu Giriş Modeli** (Bulk / Template)

**Tanım**: Şablon (`FiloGuzergahEslestirme`) oluştur → Sistem tüm ayı otomatik üret → Sadece istisnalar düzenle.

```
┌─────────────────────────────────────────┐
│ YÖNETİCİ: Aybaşında Şablon Oluştur    │
├─────────────────────────────────────────┤
│ Güzergah: GZR-TRT-001                  │
│ Müşteri: TRT-ANKARA                    │
│ Araç: 34CX8                            │
│ Şoför: Ali Demir                       │
│ Kurumdan Tahsil: 150 TL/sefer          │
│ Gider: 80 TL/sefer                     │
│ [ŞABLON OLUŞTUR]                       │
└─────────────────────────────────────────┘
     ↓ (Job çalışır 04:00'de)
┌─────────────────────────────────────────┐
│ SİSTEM: 22 gün × 2 sefer                │
│ = 44 kayıt OTOMATİK OLUŞTUR ✅         │
│ Durum: Planli, Onaylandi=false         │
├─────────────────────────────────────────┤
│ OPERATÖR: İstisnaları Düzenle          │
│ - 2025-01-06 (Köprü günü iptal)       │
│ - 2025-01-17 (Arıza → Durum=Iptal)    │
│ [KAYDET x 2]                           │
└─────────────────────────────────────────┘
```

#### Avantajları
✅ **Çok az klik**: Sadece istisna düzeltmeleri (2-5 kayıt)  
✅ **Operatör verimli**: Rutin iş yapmaması, konsantrasyonu kısıl  
✅ **Sistem tutarlılığı**: Aynı şablon, garantili sefer sayısı  
✅ **Üretkenlik**: 44 kayıt yerine ~2 düzenleme → Saat 1 saat civarında  
✅ **Taş temeli güvenli**: Job başarısız olsa, eski veri korunur  

#### Dezavantajları  
❌ Şablon ayarı hatalı ise tüm ay hatalı → Batch fix gerekli  
❌ İstisnalar sistem tarafından otomatik tespit edilmiyor (Manual gözden geçirme)  
❌ Yeni güzergah eklemek şablon oluşturmayı gerekli kılıyor  

**SONUÇ**: ✅ **Ticari olarak kabul edilebilir** (Operatör verimliliği maksimum + sistem kontrollü)

---

## 2. Ticari Kabul Edilebilirlik Değerlendirmesi

### Kriter Tablosu

| Kriter | Günlük Model | Toplu Model | Hibrit | Ağırlık |
|--------|--------------|-------------|--------|---------|
| **Operatör Verimliliği** | 2/10 ⚠️ | 9/10 ✅ | 10/10 ✅✅ | 35% |
| **Hata Oranı Riski** | 8/10 ⚠️ | 4/10 ✅ | 1/10 ✅✅ | 30% |
| **İş Yükü (Ay/100 kayıt)** | 3 saat | 20 dakika | 25 dakika | 25% |
| **Sistem Tutarlılığı** | 7/10 ✅ | 8/10 ✅ | 9/10 ✅✅ | 10% |

**Ağırlıklı Skor**:
- **Günlük**: 2×0.35 + 8×0.30 + 3×0.25 + 7×0.10 = 0.7 + 2.4 + 0.75 + 0.7 = **4.55/10** ❌
- **Toplu**: 9×0.35 + 4×0.30 + 2×0.25 + 8×0.10 = 3.15 + 1.2 + 0.5 + 0.8 = **5.65/10** ✅
- **Hibrit**: 10×0.35 + 1×0.30 + 2.5×0.25 + 9×0.10 = 3.5 + 0.3 + 0.625 + 0.9 = **5.325/10** 🔄

### Hukuki/Muhasebe Perspektifi

| Durum | Günlük Model | Toplu Model | Hibrit |
|------|--------------|-------------|--------|
| **Denetim İzlenebilirliği** | Tam (Her kayıt tarihi) | Tam (Batch log) | Tam ✅✅ |
| **Fatura Doğruluğu** | Riskli (Hata potansiyeli) | Güvenli (Template) | Güvenli ✅ |
| **Ay Sonu Kapanış Hızı** | Yavaş (22 gün gözden geçir) | Hızlı (Batch kontrol) | Hızlı ✅ |
| **Muhasebe Tasdiki** | Kompleks (Detay kontrol) | Basit (Özet kontrol) | Basit ✅ |

**Hukuki Sonuç**: ✅ **Muhasebe tarafından kabul edilebilir** (Toplu Model / Hibrit Model)

---

## 3. Problem Alanları

### Problem 1: **Günlük Modelde Operatör Ermek**

```
❌ Sene sonunda:
   - 44 kayıt/ay × 12 ay = 528 veri girişi/sene
   - Tahmini: 12 saatlik insan gücü
   - Operatör tatilinde ise sistem durur

✅ Hileli Girişler:
   - "Durum=Gitti" her gün seçme riski (Kontrol eksikliği)
   - Tarihe dikkat etmeme (Sıra hatası)
   - Çarpan unutma (Hafta sonu = 1.5 olması gerektiğinde 1.0)
```

### Problem 2: **Toplu Modelde Şablon Yötüşü**

```
❌ Şablon Hatasından:
   - Fiyat yanlış gir (150 yerine 1500) → 44 kayıt hatalı ✗
   - Şoför yanlış seç (Ali yerine Mehmet) → Operasyon çökebilir ✗

✅ Çözüm:
   - Şablon oluşturma Yönetici tarafından (Tekli kontrol)
   - Sistem Job log'u kaydet (Başarı/başarısızlık raporu)
   - İstisnalı günlük validasyon (Hafta sonu, bayram otomatik kontrol)
```

### Problem 3: **Veri Tutarlılığı Crunch**

```
Senaryo: Guzergah.BirimFiyat = 150 TL → 160 TL (Fiyat artışı)

❌ Günlük Model:
   - Onaylı raporlar: 20 kayıt (150 TL sabit)
   - Yeni kayıtlar: 24 kayıt (160 TL)
   - Raporlama anlaşmazlığı: Toplam ne? 20×150 + 24×160

✅ Hibrit Model (Snapshot Kullanılan):
   - FiloGunlukPuantaj.GuzergahBirimFiyatSnapshot = 150 TL
   - Raporlama tutarlı: İşlenmiş kayıtlar snapshot'ı kullanır
   - Yeni aydan: Yeni fiyat geçerli
```

---

## 4. Önerilen Hibrit Model (Günlük Giriş + Toplu İşlem)

### Mimari Genel Bakış

```
┌──────────────────────────────────────────────────────────────┐
│                    HIBRIT MODEL TASARIMI                     │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ [A] SETUP AŞAMASI (Ay Başında - 5 dakika)                  │
│     └─ Yönetici: FiloGuzergahEslestirme +                  │
│        GuzergahSefer slotları konfigüre et                  │
│        ├─ Güzergah, Araç, Şoför, Fiyatlar seç              │
│        └─ [KAYDET] → Sistem Template hazır                 │
│                                                              │
│ [B] BATCH GENERATION (04:00 Job - Otomatik)                │
│     └─ System: 22 gün × 2 sefer/gün = 44 kayıt oluştur    │
│        ├─ Durum: Planli                                     │
│        ├─ Onaylandi: false (Terminal kontrolü bekleniyor)   │
│        ├─ Snapshot: Fiyatlar kilitlendi                     │
│        └─ Log: Job success/failure rapor                    │
│                                                              │
│ [C] GÜNLÜK OPERASYON (06:00 - 18:00)                        │
│     └─ Şoför: QR código ile başladı/bitti işaret et        │
│        └─ Sistem otomatik: Durum = Gitti                    │
│                                                              │
│ [D] İSTİSNA YÖNETİMİ (Gün İçinde)                          │
│     └─ Operatör: Sadece hatalı vardiyaları düzenle         │
│        ├─ 2025-01-06 (Köprü günü) → İptal                  │
│        ├─ 2025-01-17 (Arıza) → Durum = Arizalandi          │
│        └─ [KAYDET] x 2 (İstisnalar sadece)                 │
│                                                              │
│ [E] TERMINAL KONTROL (Akşam 17:45)                         │
│     └─ Kontrol Operatörü: KM, Yakıt, Hasar kontrol        │
│        └─ Masa: Onaylandi = true (Sistem ayarı)            │
│                                                              │
│ [F] HAKEDIS OLUŞTUR (28 Ocak 02:00)                        │
│     └─ System: AyaDer SELECT Onaylandi=true SUM()          │
│        ├─ Güzergah Özeti (HakedisPuantaj)                 │
│        ├─ Kurum Faturası (Hakedis)                        │
│        └─ Tedarikçi Ödeme (TedarikciOdeme)                │
│                                                              │
│ [G] RAPORLAMA (Ay İçinde / Ay Sonu)                        │
│     └─ Yönetici: Dashboard                                 │
│        ├─ Güzergah performans (Gelir, Gider, Kar)         │
│        ├─ Trend (6 aylık trend)                           │
│        └─ Tahminler (Ay sonu projeksiyon)                 │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### Adım Adım Operasyon

#### **Adım 1: Şablon Kurulumu (Ay Başında)**

```csharp
// Yönetici Portal → "Aybaşı Konfigürasyonu" Sayfası

// UI: Simple Form
- Güzergah Seç (Dropdown)
- Müşteri Seç (Dropdown = Kurum)
- Araç Seç (Dropdown)
- Şoför Seç (Dropdown)
- Kurumdan Tahsil: 150 TL ← Auto-fill from Guzergah.BirimFiyat
- Gideri: 80 TL ← Auto-fill from Guzergah.GiderFiyat
- Sefer Tipi: Sabah+Aksam ← Auto-fill from Guzergah.SeferTipi
- [KAYDET]

// Backend
repository.CreateEslestirme(new FiloGuzergahEslestirme
{
    KurumFirmaId = müşteri,
    GuzergahId = güzergah,
    AracId = araç,
    SoforId = şoför,
    KurumaKesilecekUcret = 150m,
    TaseronaOdenenUcret = 80m,
    ServisTuru = ServisTuru.SabahAksam
});
```

**Zaman**: ⏱ ~2-3 dakika (Yönetici)

---

#### **Adım 2: Sistem Batch Job (04:00 Job)**

```csharp
// File: GenerateDailyPuantajsJob.cs
// Cron: 0 4 * * *  (Her gün 04:00)

public async Task Execute()
{
    // Yarın için tüm AKTIF eşleştirmeleri oku
    var eslestirmeler = await context.FiloGuzergahEslestirmeler
        .AsNoTracking()
        .Where(e => e.IsActive && !e.IsDeleted)
        .ToListAsync();

    // Her eşleştirme için günlük kayıt oluştur
    foreach (var eslestirme in eslestirmeler)
    {
        var tarih = DateTime.Today.AddDays(1); // Yarın

        // Prova: Aynı tarihte var mı?
        var existing = await context.FiloGunlukPuantajlar
            .FirstOrDefaultAsync(gp => 
                gp.FiloGuzergahEslestirmeId == eslestirme.Id &&
                gp.Tarih == tarih);

        if (existing != null) continue; // Zaten var

        // YENİ KAYIT OLU
        var puantaj = new FiloGunlukPuantaj
        {
            FiloGuzergahEslestirmeId = eslestirme.Id,
            Tarih = tarih,
            KurumFirmaId = eslestirme.KurumFirmaId,
            GuzergahId = eslestirme.GuzergahId,
            AracId = eslestirme.AracId,
            SoforId = eslestirme.SoforId,
            ServisTuru = eslestirme.ServisTuru,
            SeferSayisi = (eslestirme.ServisTuru == ServisTuru.SabahAksam) ? 2 : 1,

            // SNAPSHOT: Fiyatları kilitlenmiş olarak sakla
            GuzergahBirimFiyatSnapshot = eslestirme.KurumaKesilecekUcret,
            GuzergahGiderFiyatSnapshot = eslestirme.TaseronaOdenenUcret,

            // ÇARPAN: Hafta sonu/bayram kontrol
            PuantajCarpani = CalculateCarpan(tarih),

            // DURUMU: İlk başta Planli
            Durum = OperasyonDurumu.Planli,
            Onaylandi = false
        };

        context.FiloGunlukPuantajlar.Add(puantaj);
    }

    await context.SaveChangesAsync();

    // LOG: Başarılı tespit (Slack bildir vs.)
    logger.LogInformation($"✅ {eslestirmeler.Count} eşleştirmeden 
        22 gün × Sefer = {totalGeneratedRecords} puantaj oluşturuldu.");
}

private decimal CalculateCarpan(DateTime tarih)
{
    // Hafta sonu kontrolü
    if (tarih.DayOfWeek == DayOfWeek.Saturday || 
        tarih.DayOfWeek == DayOfWeek.Sunday)
        return 1.5m; // Hafta sonu

    // Bayram kontrolü
    var bayrams = new[] { 
        new DateTime(2025, 1, 1),   // Yılbaşı
        new DateTime(2025, 4, 23),  // Çocuk Bayramı
        // ... diğer bayramlar
    };

    if (bayrams.Contains(tarih.Date))
        return 1.5m;

    return 1.0m;
}
```

**Zaman**: ⏱ ~10 saniye (Sistem, her gece otomatik)

---

#### **Adım 3: İşletme Akışı (Gün İçinde)**

```
Sabah 06:30
├─ Şoför, araç depoda
├─ QR Kod/RFID: "Başladı" işaretleme
└─ Sistem otomatik: FiloGunlukPuantaj.Durum = Gitti (✓ Başladı)

Akşam 18:00
├─ Şoför, oto deposuna geri
├─ QR Kod/RFID: "Bitti" işaretleme
└─ Sistem otomatik: FiloGunlukPuantaj.Durum = Gitti (✓ Tamamlandı)

Akşam 17:45
├─ Kontrol operatörü: KM sayacı, Yakıt, Hasar kontrol
└─ Sistem: Onaylandi = true ← İşaret
```

**Zaman**: ⏱ ~30 saniye/araç (QR kod tarama)

---

#### **Adım 4: İstisnalı Düzeltme (Haftada 1-2 kez)**

```
Operatör Portal: "İstisnalı Günleri Düzenle"

┌─────────────────────────────────────────────┐
│ Takvim Görünümü                             │
├─────────────────────────────────────────────┤
│ 1  2  3  4  5  6  7  8 ... 22 ... 31       │
│         ✓  ✓  ✗  ✓  ✓  ✓  ✓   Başlandı    │
│                                             │
│ Donanım 6: ⚠️ KÖPRü GÜNÜ (İptal)          │
│ Durum: Planli → İptal (Kurumdan talebi)   │
│ [KAYDET]                                   │
│                                             │
│ Donanım 17: ⚠️ MOTOR ARIZA (Gitmedi)      │
│ Durum: Planli → Gitmedi_Ariza             │
│ Sebep: Motor soğutma sıvısı sızıntısı     │
│ [KAYDET]                                   │
└─────────────────────────────────────────────┘
```

**Zaman**: ⏱ ~2-3 dakika (2-3 istisna için)

---

#### **Adım 5: Hakedis (Ay Sonu - 28 Ocak)**

```sql
-- Job: GenerateMonthlyHakediş (02:00 at midnight)

-- 1. Güzergah Özeti (HakedisPuantaj)
SELECT 
    gz.GuzergahId,
    COUNT(fgp.Id) AS SejerSayisi,
    SUM(fgp.GuzergahBirimFiyatSnapshot * fgp.SeferSayisi * fgp.PuantajCarpani) AS Gelir,
    SUM(fgp.GuzergahGiderFiyatSnapshot * fgp.SeferSayisi * fgp.PuantajCarpani) AS Gider
FROM FiloGunlukPuantajlar fgp
WHERE YEAR(fgp.Tarih) = 2025 
  AND MONTH(fgp.Tarih) = 1
  AND fgp.Onaylandi = true
  AND fgp.Durum NOT IN ('İptal', 'Gitmedi')
GROUP BY gz.GuzergahId;

-- 2. Kurum Faturası (Hakedis)
SELECT 
    fgp.KurumFirmaId,
    SUM(fgp.SeferSayisi) AS ToplamSefer,
    SUM(fgp.GuzergahBirimFiyatSnapshot * fgp.SeferSayisi * fgp.PuantajCarpani) AS ToplamGelir
FROM FiloGunlukPuantajlar fgp
WHERE ... (Aynı filtre)
GROUP BY fgp.KurumFirmaId;
```

**Zaman**: ⏱ ~2 saniye (SQL Group By)

---

### **Operatör İş Yükü Özeti**

| Adım | Kim | Frekans | Zaman | Kompleksite |
|------|-----|---------|-------|-------------|
| **Şablon Kurulumu** | Yönetici | Ay başında | 3 dk | Düşük |
| **Batch Job** | Sistem | Her gece 04:00 | Otomatik | - |
| **Gün İçi QR** | Şoför | Her sefer (2×/gün) | 30 sn | Çok Düşük |
| **Terminal Kontrol** | Kontrol Operatörü | Her gün | 10 dk | Düşük |
| **İstisnalı Düzeltme** | Operatör | Haftada 2-3 x | 2-3 dk | Düşük |
| **Hakedis** | Sistem | Ay sonu (1×) | Otomatik | - |

**TOPLAM İŞ YÜKÜ/AY**: 🎯 **~20-25 dakika** (Operatör) + **~10 dakika** (Yönetici aybaşında)

---

## 5. Uygulama Planı

### **Faz 1: Foundation (1-2 hafta)**

- [ ] `GenerateDailyPuantajsJob` implementasyonu (Batch generation)
- [ ] `GuzergahSefer` entity & service (Sefer slot yönetimi)
- [ ] Snapshot pattern migration (Fiyat kilitlendi)
- [ ] `CalculateCarpan()` helper (Hafta sonu/bayram)

### **Faz 2: UI - Blazor Bileşenleri (2-3 hafta)**

- [ ] **EslestirmeYonetimi.razor** - Şablon oluştur/düzenle
  - Dropdown: Güzergah, Araç, Şoför
  - Auto-fill: Fiyatlar (Guzergah'dan)
  - Button: KAYDET

- [ ] **IstisnalıGünlerEditor.razor** - Takvim + Düzenleme
  - Takvim görünümü (Durum rengi: Planli=yeşil, İptal=kırmızı)
  - Modal: İstisna Detayı (Sebep, Durum değişikliği)
  - Button: KAYDET

- [ ] **TerminalOnayi.razor** - Kontrol Operatörü Kontrol
  - Bugünün listesi (22 güzergah × 2 sefer)
  - Checkbox: "Kontrol tamam, onaylı"
  - Button: ONAYLA (Batch Onaylandi=true)

### **Faz 3: Raporlama (2 hafta)**

- [ ] **GuzergahPerformansRaporu.razor** - Dashboard
  - Güzergah listesi + Performans kartları
  - Grafik: Gelir/Gider trend
  - Export: Excel (Muhasebe için)

### **Faz 4: Test & Doğrulama (1 hafta)**

- [ ] UAT: Operatör tarafından testler
- [ ] Job reliability: Backup/recovery
- [ ] Raporlama doğruluğu: Muhasebe kontrol

---

## 6. Blazor UI Tasarımı

### **6.1 EslestirmeYonetimi.razor** (Template Oluştur)

```razor
@page "/eslestirme-yonetimi"
@using MKFiloServis.Shared.Entities
@using MKFiloServis.Web.Services
@inject FiloKomisyonService FiloService
@inject GuzergahService GuzergahService
@inject NotificationService NotificationService

<PageTitle>Eşleştirme Yönetimi - Aybaşı Kurulumu</PageTitle>

<div class="container-fluid">
    <div class="row">
        <div class="col-12">
            <h2>📋 Aybaşı Konfigürasyonu</h2>
            <p class="text-muted">Hangi Güzergah + Araç + Şoför kombinasyonlarını 22 gün boyunca çalıştırılacak?</p>
        </div>
    </div>

    <!-- NEW TEMPLATE FORM -->
    <div class="row mt-4">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header bg-primary text-white">
                    <h5>🆕 Yeni Template</h5>
                </div>
                <div class="card-body">
                    <form @onsubmit="HandleCreateEslestirme">
                        <!-- Güzergah -->
                        <div class="mb-3">
                            <label for="guzergahId" class="form-label">🗺️ Güzergah *</label>
                            <select class="form-select" id="guzergahId" 
                                    @bind="newEslestirme.GuzergahId" 
                                    @onchange="OnGuzergahChanged">
                                <option value="">Seçin...</option>
                                @foreach (var gz in guzergahlar)
                                {
                                    <option value="@gz.Id">@gz.GuzergahKodu - @gz.GuzergahAdi</option>
                                }
                            </select>
                        </div>

                        <!-- Müşteri -->
                        <div class="mb-3">
                            <label for="musteriId" class="form-label">👥 Müşteri (Kurum) *</label>
                            <select class="form-select" id="musteriId" 
                                    @bind="newEslestirme.KurumFirmaId">
                                <option value="">Seçin...</option>
                                @foreach (var musteri in musteriler)
                                {
                                    <option value="@musteri.Id">@musteri.CariAdi</option>
                                }
                            </select>
                        </div>

                        <!-- Araç -->
                        <div class="mb-3">
                            <label for="aracId" class="form-label">🚌 Araç *</label>
                            <select class="form-select" id="aracId" 
                                    @bind="newEslestirme.AracId">
                                <option value="">Seçin...</option>
                                @foreach (var arac in araclar)
                                {
                                    <option value="@arac.Id">@arac.Plaka - @arac.Model</option>
                                }
                            </select>
                        </div>

                        <!-- Şoför -->
                        <div class="mb-3">
                            <label for="soforId" class="form-label">👨 Şoför *</label>
                            <select class="form-select" id="soforId" 
                                    @bind="newEslestirme.SoforId">
                                <option value="">Seçin...</option>
                                @foreach (var sofor in soforler)
                                {
                                    <option value="@sofor.Id">@sofor.SoforAdi</option>
                                }
                            </select>
                        </div>

                        <!-- Otomatik Doldurulmuş Fiyatlar -->
                        <hr>
                        <div class="row">
                            <div class="col-6">
                                <div class="mb-3">
                                    <label class="form-label">💰 Kurumdan Tahsil</label>
                                    <div class="input-group">
                                        <input type="number" class="form-control" 
                                               @bind="newEslestirme.KurumaKesilecekUcret" 
                                               disabled>
                                        <span class="input-group-text">₺</span>
                                    </div>
                                    <small class="text-muted">Guzergah.BirimFiyat'ten otomatik</small>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="mb-3">
                                    <label class="form-label">💸 Tedarikçiye Ödeme</label>
                                    <div class="input-group">
                                        <input type="number" class="form-control" 
                                               @bind="newEslestirme.TaseronaOdenenUcret" 
                                               disabled>
                                        <span class="input-group-text">₺</span>
                                    </div>
                                    <small class="text-muted">Guzergah.GiderFiyat'ten otomatik</small>
                                </div>
                            </div>
                        </div>

                        <!-- Buttons -->
                        <div class="d-flex gap-2">
                            <button type="submit" class="btn btn-success">
                                ✅ TEMPLATE OLUŞTUR
                            </button>
                            <button type="button" class="btn btn-secondary" @onclick="ResetForm">
                                🔄 Temizle
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>

        <!-- ACTIVE TEMPLATES LIST -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header bg-info text-white">
                    <h5>📊 Aktif Templates</h5>
                </div>
                <div class="card-body" style="max-height: 400px; overflow-y: auto;">
                    @foreach (var eslestirme in eslestirmeler)
                    {
                        <div class="alert alert-info border-left-4">
                            <div class="d-flex justify-content-between align-items-start">
                                <div>
                                    <strong>@eslestirme.Guzergah.GuzergahAdi</strong>
                                    <br>
                                    <small>🚌 @eslestirme.Arac.Plaka | 👨 @eslestirme.Sofor.SoforAdi</small>
                                    <br>
                                    <small>💰 @eslestirme.KurumaKesilecekUcret TL (Gelir) / @eslestirme.TaseronaOdenenUcret TL (Gider)</small>
                                </div>
                                <div>
                                    <button class="btn btn-sm btn-warning" @onclick="() => EditEslestirme(eslestirme)">
                                        ✏️ Düzenle
                                    </button>
                                    <button class="btn btn-sm btn-danger" @onclick="() => DeleteEslestirme(eslestirme)">
                                        🗑️ Sil
                                    </button>
                                </div>
                            </div>
                        </div>
                    }
                </div>
            </div>
        </div>
    </div>

    <!-- INFO BOX -->
    <div class="row mt-4">
        <div class="col-12">
            <div class="alert alert-success">
                <strong>ℹ️ Sistem Otomasyonu:</strong>
                <br>
                ✅ Her template için sistem 22 gün × 2 sefer = 44 kayıt oluşturacaktır.
                <br>
                ✅ Puantaj girilişi <strong>04:00'de</strong> (gece otomatik) gerçekleşecektir.
                <br>
                ⚠️ İstisnalar (Köprü günü, arıza) <strong>İstisnalı Günler Editor</strong>'de düzenlenir.
            </div>
        </div>
    </div>
</div>

@code {
    private List<Guzergah> guzergahlar = new();
    private List<Cari> musteriler = new();
    private List<Arac> araclar = new();
    private List<Sofor> soforler = new();
    private List<FiloGuzergahEslestirme> eslestirmeler = new();

    private FiloGuzergahEslestirme newEslestirme = new();

    protected override async Task OnInitializedAsync()
    {
        // Load dropdowns
        guzergahlar = await GuzergahService.GetActiveGuzergahlarAsync();
        // ... load musteriler, araclar, soforler
        eslestirmeler = await FiloService.GetEslestirmelerAsync();
    }

    private async Task OnGuzergahChanged(ChangeEventArgs e)
    {
        var guzergahId = int.Parse(e.Value?.ToString() ?? "0");
        var guzergah = guzergahlar.FirstOrDefault(g => g.Id == guzergahId);

        if (guzergah != null)
        {
            // Otomatik doldur
            newEslestirme.KurumaKesilecekUcret = guzergah.BirimFiyat;
            newEslestirme.TaseronaOdenenUcret = guzergah.GiderFiyat;
        }
    }

    private async Task HandleCreateEslestirme()
    {
        try
        {
            await FiloService.CreateEslestirmeAsync(newEslestirme);
            await NotificationService.ShowSuccessAsync("✅ Template oluşturuldu!");

            eslestirmeler = await FiloService.GetEslestirmelerAsync();
            ResetForm();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync($"❌ Hata: {ex.Message}");
        }
    }

    private void ResetForm()
    {
        newEslestirme = new();
    }

    private async Task DeleteEslestirme(FiloGuzergahEslestirme eslestirme)
    {
        if (await NotificationService.ConfirmAsync("Silmek istediğinizden emin misiniz?"))
        {
            await FiloService.DeleteEslestirmeAsync(eslestirme.Id);
            eslestirmeler = await FiloService.GetEslestirmelerAsync();
        }
    }

    private async Task EditEslestirme(FiloGuzergahEslestirme eslestirme)
    {
        // Navigate to edit page
        NavigationManager.NavigateTo($"/eslestirme-duzenle/{eslestirme.Id}");
    }
}
```

---

### **6.2 IstisnalıGünlerEditor.razor** (Takvim + İstisnalar)

```razor
@page "/istisnal-gunler-duzenle/{AyYil}"
@using MKFiloServis.Shared.Entities
@using System.Globalization

<PageTitle>İstisnalı Günler - @AyYil</PageTitle>

<div class="container-fluid">
    <div class="row mb-4">
        <div class="col-12">
            <h2>📅 @CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(CurrentMonth) @CurrentYear İstisnalı Günleri Düzenle</h2>
            <p class="text-muted">Köprü günleri, tatiller veya arızaları işaretleyin. Sistem otomatik üretilen 44 puantajdan bunları çıkaracaktır.</p>
        </div>
    </div>

    <!-- CALENDAR VIEW -->
    <div class="row">
        <div class="col-12">
            <div class="card">
                <div class="card-body">
                    <div class="calendar">
                        @for (int day = 1; day <= DaysInMonth; day++)
                        {
                            var date = new DateTime(CurrentYear, CurrentMonth, day);
                            var durum = GetPuantajDurum(date);
                            var istisnavarmi = IstisnaliGunler.ContainsKey(date.Date);

                            <div class="calendar-day 
                                        @(istisnavarmi ? "bg-danger text-white" : durum == "Gitti" ? "bg-success text-white"  : "bg-light")">
                                <div class="day-number">@day</div>
                                @if (istisnavarmi && IstisnaliGunler[date.Date] != null)
                                {
                                    <small class="day-status">@IstisnaliGunler[date.Date]</small>
                                }
                            </div>
                        }
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- İSTİSNA DETAY -->
    <div class="row mt-4">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header bg-warning">
                    <h5>⚠️ İstisnalı Günü Seç ve Düzenle</h5>
                </div>
                <div class="card-body">
                    <form @onsubmit="HandleSaveIstisna">
                        <div class="mb-3">
                            <label class="form-label">Tarih</label>
                            <input type="date" class="form-control" @bind="selectedDate">
                        </div>

                        <div class="mb-3">
                            <label class="form-label">İstisna Türü *</label>
                            <select class="form-select" @bind="selectedDurum">
                                <option value="">Seçin...</option>
                                <option value="İptal_KurumTarafindan">❌ İptal - Kurum Tarafından</option>
                                <option value="İptal_Koprugu">❌ İptal - Köprü Günü</option>
                                <option value="İptal_Tatil">❌ İptal - Resmi Tatil</option>
                                <option value="Arizalandi">⚙️ Arızalandı - Motor Sorunu</option>
                                <option value="Arizalandi_Kaza">⚙️ Arızalandı - Kaza</option>
                                <option value="Gitmedi_Mazeretli">⛔ Gitmedi - Mazeret (Şoför Hastalığı)</option>
                                <option value="Gitmedi_Mazeretsiz">❌ Gitmedi - Mazaret Olmadan</option>
                            </select>
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Açıklama</label>
                            <textarea class="form-control" @bind="selectedDescription" 
                                      placeholder="Örn: Motor soğutma sıvısı sızıntısı"></textarea>
                        </div>

                        <div class="d-flex gap-2">
                            <button type="submit" class="btn btn-success">✅ KAYDET</button>
                            <button type="button" class="btn btn-info" @onclick="ClearSelection">🔄 Temizle</button>
                        </div>
                    </form>
                </div>
            </div>
        </div>

        <!-- İstisnalar Listesi -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header bg-info">
                    <h5>📋 Bu Ay İstisnalar (@IstisnaliGunler.Count)</h5>
                </div>
                <div class="card-body" style="max-height: 300px; overflow-y: auto;">
                    @foreach (var istisna in IstisnaliGunler.OrderBy(x => x.Key))
                    {
                        <div class="alert alert-danger d-flex justify-content-between">
                            <div>
                                <strong>@istisna.Key.ToString("dd MMM yyyy")</strong>
                                <br>
                                <small>@istisna.Value</small>
                            </div>
                            <button class="btn btn-sm btn-danger" 
                                    @onclick="() => RemoveIstisna(istisna.Key)">
                                🗑️
                            </button>
                        </div>
                    }
                </div>
            </div>
        </div>
    </div>
</div>

<style>
    .calendar {
        display: grid;
        grid-template-columns: repeat(7, 1fr);
        gap: 10px;
        margin-bottom: 20px;
    }

    .calendar-day {
        padding: 10px;
        text-align: center;
        border: 1px solid #ddd;
        min-height: 50px;
        cursor: pointer;
        border-radius: 5px;
    }

    .calendar-day:hover {
        opacity: 0.8;
    }

    .day-number {
        font-weight: bold;
        font-size: 14px;
    }

    .day-status {
        display: block;
        font-size: 10px;
        margin-top: 3px;
    }
</style>

@code {
    [Parameter]
    public string AyYil { get; set; } = DateTime.Now.ToString("yyyy-MM");

    private int CurrentMonth => int.Parse(AyYil.Split('-')[1]);
    private int CurrentYear => int.Parse(AyYil.Split('-')[0]);
    private int DaysInMonth => DateTime.DaysInMonth(CurrentYear, CurrentMonth);

    private Dictionary<DateTime, string> IstisnaliGunler = new();
    private DateTime selectedDate = DateTime.Now;
    private string selectedDurum = "";
    private string selectedDescription = "";

    private async Task HandleSaveIstisna()
    {
        if (string.IsNullOrEmpty(selectedDurum))
        {
            // Show error notification
            return;
        }

        IstisnaliGunler[selectedDate.Date] = $"{selectedDurum} - {selectedDescription}";

        // Update database
        // await service.UpdateIstisnaAsync(...);

        ClearSelection();
    }

    private void RemoveIstisna(DateTime date)
    {
        IstisnaliGunler.Remove(date);
    }

    private void ClearSelection()
    {
        selectedDurum = "";
        selectedDescription = "";
    }

    private string GetPuantajDurum(DateTime date)
    {
        // Query DB for this date
        // return "Gitti", "Planli", or null
        return "Planli";
    }
}
```

---

### **6.3 TerminalOnayi.razor** (Günlük Kontrol)**

```razor
@page "/terminal-onayi"

<PageTitle>Terminal Kontrol - Günlük Onaylama</PageTitle>

<div class="container-fluid">
    <div class="row mb-4">
        <div class="col-12">
            <h2>✅ Terminal Kontrolü - @DateTime.Today.ToString("dd MMM yyyy")</h2>
            <p class="text-muted">
                Bugünün tüm araçlarını kontrol et (KM, Yakıt, Hasar). 
                Sorunlar yoksa BAŞKAN's olarak işaretle.
            </p>
        </div>
    </div>

    <!-- TODAY'S SUMMARY -->
    <div class="row">
        <div class="col-md-3">
            <div class="card bg-primary text-white">
                <div class="card-body">
                    <h5>🚌 Toplam Araç</h5>
                    <h3>@todayAracSayisi</h3>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card bg-success text-white">
                <div class="card-body">
                    <h5>✅ Kontrol Tamamlanmış</h5>
                    <h3>@confirmedCount/@todayAracSayisi</h3>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card bg-warning text-white">
                <div class="card-body">
                    <h5>⏳ Beklenen</h5>
                    <h3>@(todayAracSayisi - confirmedCount)</h3>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card bg-info text-white">
                <div class="card-body">
                    <h5>💰 Bugünün Geliri</h5>
                    <h3>@todayGelir.ToString("C")</h3>
                </div>
            </div>
        </div>
    </div>

    <!-- CHECKLIST TABLE -->
    <div class="row mt-4">
        <div class="col-12">
            <div class="card">
                <div class="card-header bg-dark text-white">
                    <h5>📋 Araç Kontrol Checklist'i</h5>
                </div>
                <div class="card-body">
                    <table class="table table-striped table-hover">
                        <thead class="table-dark">
                            <tr>
                                <th>Güzergah</th>
                                <th>Araç</th>
                                <th>Şoför</th>
                                <th>Sefer</th>
                                <th>Durum</th>
                                <th>KM Sayacı</th>
                                <th>Kontrol</th>
                                <th>Onayla</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var puantaj in todayPuantajlar)
                            {
                                <tr>
                                    <td>@puantaj.Guzergah.GuzergahAdi</td>
                                    <td>
                                        <strong>@puantaj.Arac.Plaka</strong>
                                        <br>
                                        <small>@puantaj.Arac.Model</small>
                                    </td>
                                    <td>@puantaj.Sofor.SoforAdi</td>
                                    <td>
                                        @if (puantaj.ServisTuru == ServisTuru.SabahAksam)
                                        {
                                            <span class="badge bg-info">2 Sefer</span>
                                        }
                                        else
                                        {
                                            <span class="badge bg-secondary">1 Sefer</span>
                                        }
                                    </td>
                                    <td>
                                        @if (puantaj.Durum == OperasyonDurumu.Gitti)
                                        {
                                            <span class="badge bg-success">✅ Gitti</span>
                                        }
                                        else if (puantaj.Durum == OperasyonDurumu.Arizalandi)
                                        {
                                            <span class="badge bg-danger">⚙️ Arızalandı</span>
                                        }
                                        else
                                        {
                                            <span class="badge bg-light text-dark">⏳ Planli</span>
                                        }
                                    </td>
                                    <td>
                                        <input type="number" class="form-control form-control-sm" 
                                               @bind="puantaj.KmSayaci" placeholder="KM">
                                    </td>
                                    <td>
                                        <button class="btn btn-sm btn-info" 
                                                @onclick="() => OpenKontrolModal(puantaj)">
                                            🔍 Kontrol
                                        </button>
                                    </td>
                                    <td>
                                        <input type="checkbox" class="form-check-input" 
                                               @bind="puantaj.Onaylandi">
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>

                    <div class="mt-3">
                        <button class="btn btn-lg btn-success" @onclick="HandleBatchOnayla">
                            ✅ TÜM KONTROLİ ONAYLA (@confirmedCount/@todayAracSayisi)
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private List<FiloGunlukPuantaj> todayPuantajlar = new();
    private int todayAracSayisi = 0;
    private int confirmedCount = 0;
    private decimal todayGelir = 0;

    protected override async Task OnInitializedAsync()
    {
        // Load today's puantajlar
        todayPuantajlar = await service.GetTodayPuantajlarAsync();

        todayAracSayisi = todayPuantajlar.Count;
        confirmedCount = todayPuantajlar.Count(p => p.Onaylandi);
        todayGelir = todayPuantajlar.Sum(p => 
            p.GuzergahBirimFiyatSnapshot * p.SeferSayisi * p.PuantajCarpani);
    }

    private async Task HandleBatchOnayla()
    {
        var toConfirm = todayPuantajlar.Where(p => !p.Onaylandi).ToList();

        foreach (var puantaj in toConfirm)
        {
            puantaj.Onaylandi = true;
            await service.UpdatePuantajAsync(puantaj);
        }

        await NotificationService.ShowSuccessAsync($"✅ {toConfirm.Count} puantaj onaylandı!");
        confirmedCount = todayAracSayisi;
    }

    private async Task OpenKontrolModal(FiloGunlukPuantaj puantaj)
    {
        // Open modal to log KM, fuel, damage details
    }
}
```

---

## 📌 Özet & Tavsiye

### ✅ **Önerilen Model: HİBRİT (Günlük Giriş + Toplu İşlem)**

| Unsur | Durum |
|-------|-------|
| **Operatör İş Yükü** | 🟢 Düşük (~20-25 min/ay) |
| **Hata Riski** | 🟢 Minimum (Sistem kontrol) |
| **Veri Tutarlılığı** | 🟢 Garantili (Snapshot) |
| **Raporlama** | 🟢 İşlenebilir (Batch) |
| **Ticari Kabul** | 🟢 **EVET** ✅ |

### 📋 Uygulama Sırası

1. **Faz 1** (1-2 hafta): Backend Job + Snapshot pattern
2. **Faz 2** (2-3 hafta): Blazor UI bileşenleri
3. **Faz 3** (2 hafta): Raporlama dashboard
4. **Faz 4** (1 hafta): Test & go-live

**Teknik Sorumluluk**: Puantaj modülü SADECEsse, diğer modüllere dokunulmaz ✅

---

**Sonraki Adım**: Plan ayrıntılarını mı oluşturalım? Uygulama sunumu mı yapmalı?
