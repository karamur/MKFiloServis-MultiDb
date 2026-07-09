# Personel Taşıması Yapan Bir Firmada Puantaj Sistemi

**Tarih**: 2025-01-23  
**Kapsamı**: Operasyonel & Personel Puantaj Sistemi Mimarisi  
**Okuyucu**: İnsan Kaynakları, Operasyon, Muhasebe, Teknoloji Ekipleri  

---

## İçindekiler

1. [İş Modeli Özeti](#1-iş-modeli-özeti)
2. [Puantaj Çeşitleri](#2-puantaj-çeşitleri)
3. [Operasyonel Puantaj (Filo/Taşıma)](#3-operasyonel-puantaj-filoTaşıma)
   - 3.1 [Güzergah Boyutu (Rota Dimension)](#31-güzergah-boyutu-rota-dimension)
   - 3.2 [Sefer Slot Yönetimi](#32-sefer-slot-yönetimi)
   - 3.3 [FiloGuzergahEslestirme & FiloGunlukPuantaj Akışı](#33-filoguzergaheslestirme--filogunlukpuantaj-akışı)
4. [Personel Puantajı (Maaş)](#4-personel-puantajı-maaş)
5. [Hakediş ve Raporlama](#5-hakediş-ve-raporlama)
6. [Veri Akışı ve Entegrasyon](#6-veri-akışı-ve-entegrasyon)
7. [Sistem Komponenleri](#7-sistem-komponenleri)
8. [Örnekler ve Say Senaryoları](#8-örnekler-ve-say-senaryoları)
9. [Riskler ve Best Practices](#9-riskler-ve-best-practices)

---

> **📌 YENİ**: Bu rapor Ocak 2025'te operasyonel puantajda **güzergah boyutu**nu içerecek şekilde güncellenmiştir. Detaylı teknik referanslar için bkz:
> - `OPERASYONEL-PUANTAJ-GUZERGAH-DIMENSION.md` (Derin analiz)
> - `GUZERGAH-SEFER-SLOT-TEKNIK-REFERANS.md` (Sefer slot kılavuzu)
> - `FILOGUZERGAH-ESLESTIRME-PUANTAJ-VERI-AKISI.md` (Veri akışı mapping)

---

## 1. İş Modeli Özeti

### 1.1 Personel Taşıması Nedir?

**Personel taşıması**, bir kurumun (örn. Büyükelçilik, Bakanlık, Belediye) çalışanlarını, ziyaretçilerini veya misyonlarını sabit rotalar üzerinde periyodik olarak taşıyabilmesidir.

**Tipik Senaryolar**:
- 🏛️ **Hükümet Dairesi**: TRT personeli Ankara'da 5 farklı şube arasında sabah/akşam taşıması
- 🏢 **Özel Şirket**: İstanbul'daki bir finans şirketi, şubeleri arası personel servisi
- 🚐 **Vali Konaği**: Vali ziyareti, yönetici toplantısı taşıması
- 🏫 **Okul Otobüsü**: Devlet okulunun öğrenci taşıması
- 🏥 **Hastane**: Hasta ziyareti servisi, personel navettesi

### 1.2 Firmada Taşıma Hizmetinin Yapısı

```
┌──────────────────────────────────────────┐
│   Personel Taşıma Hizmeti (PTS Firması)  │
─ Müşteri: Kurum (TRT, İSKİ, ESHOT, vb.)
─ Tarif: Sabit güzergah + Sefer başı ücret
─ Frekans: Günlük (sabah + akşam = 2 sefer)
│                                          │
├─ Araç (Minibüs/Otobüs)                  │
│  ├─ Plaka: 34CX8 / Owner: Özmal         │
│  ├─ Model: 2020 Ford Transit            │
│  └─ Kapasite: 15 kişi                   │
│                                          │
├─ Şoför (Personel)                       │
│  ├─ Ad: Ali Demir                       │
│  ├─ Sözleşme: Tam zamanlı               │
│  ├─ Maaş: 8.500 TL/ay                   │
│  └─ Çalışma Saati: 06:00 - 18:00        │
│                                          │
├─ Güzergah (Rota)                        │
│  ├─ Adı: TRT Ankara Merkez Servisi      │
│  ├─ Başlangıç: TRT Paşaköy              │
│  ├─ Bitiş: TRT Dış Ticaret Şubesi       │
│  ├─ Mesafe: 12 km                       │
│  ├─ Sefer Saati: 30 dakika              │
│  └─ Sefer Fiyatı: 150 TL/gün (Sabah+Akşam)
│     (= 75 TL sefer, 2 sefer/gün)        │
│                                          │
└──────────────────────────────────────────┘
```

### 1.3 Ana Aktor'lar

| Aktor | Rol | Örnek |
|-------|-----|-------|
| **Kurum Müşteri** | Hizmet talep eden, faturalandırılan | TRT-ANKARA |
| **Taşıma Firması** | Hizmeti sunan, operasyon yapan | PTS Ltd. Şti. |
| **Şoför** | Operasyon yapan personel | Ali Demir |
| **Araç Sahibi** | Araç maliklikleri yönetim | Özmal / Taşeron |
| **Muhasebe** | Gelir/Gider raporlama | Müdürlük |

---

## 2. Puantaj Çeşitleri

Personel taşıması yapan bir firmada **iki bağımsız puantaj sistemi** vardır:

### 2.1 İlişki Matrisi

```
┌─────────────────────────┬──────────────────┬─────────────────────┐
│ Puantaj Türü             │ Amacı             │ Sorumluluk           │
├─────────────────────────┼──────────────────┼─────────────────────┤
│ OPERASYONELPUANTAJı     │ Gelir/Gider      │ Operasyon Müdürü    │
│ (FiloGunlukPuantaj)     │ Raporlama        │                     │
│ PERSONEL PUANTAJI       │ Maaş Hesaplama   │ İnsan Kaynakları    │
│ (PersonelPuantaj)       │ & Ödeme          │                     │
└─────────────────────────┴──────────────────┴─────────────────────┘
```

---

## 3. Operasyonel Puantaj (Filo/Taşıma)

### 3.1 Tanım

**Operasyonel Puantaj**, şoför ve araç tarafından gerçekleştirilen her bir servis çalışmasının (taşıma görevidir) günlük kaydıdır.

- **Amaç**: Müşteriye (Kurum) fatura kesilmesi ve Şoför/Araç Sahibine ödeme hesaplanması
- **Frekans**: Günlük (sabit rotalar, sabit fiyat)
- **Sorucu**: Araç Sahibi/Taşeron Firma
- **Saklayıcı**: Operasyon Yöneticisi veya Sistem

### 3.2 Güzergah Boyutu (Rota Dimension)

**Yeni Yaklaşım (Ocak 2025)**: Operasyonel puantaj artık sadece günlük kayıt değil, **Güzergah**'ı birinci sınıf boyutu (dimension) olarak içerir.

#### Güzergah (Guzergah Entity) - Ana Yapı

```csharp
public class Guzergah : BaseEntity, IFirmaTenant
{
    // TEMEL
    public int? FirmaId { get; set; }              // Kiracı firma
    public string GuzergahKodu { get; set; }       // Örn: "GZR-TRT-001"
    public string GuzergahAdi { get; set; }        // Örn: "TRT Merkez → Dış Ticaret"

    // KONUM
    public string? BaslangicNoktasi { get; set; }  // Örn: "TRT Paşaköy, Ankara"
    public string? BitisNoktasi { get; set; }      // Örn: "TRT Dış Ticaret Şubesi"
    public double? BaslangicLatitude { get; set; }
    public double? BaslangicLongitude { get; set; }
    public double? BitisLatitude { get; set; }
    public double? BitisLongitude { get; set; }
    public string? RotaRengi { get; set; }         // Harita görünümü: "#3388ff"

    // FİYATLANDIRMA (Çok Önemli!)
    public decimal BirimFiyat { get; set; }        // Kurumdan tahsil (Gelir) → 150 TL/sefer
    public decimal GiderFiyat { get; set; }        // Şoför/Araç maliyeti (Gider) → 80 TL/sefer
    public decimal PuantajCarpani { get; set; } = 1.0m;  // Hafta sonu/İzin çarpanı

    // LOJİSTİK
    public decimal? Mesafe { get; set; }           // km
    public int? TahminiSure { get; set; }          // dakika
    public int PersonelSayisi { get; set; }        // Ortalama yolcu
    public string? KapasiteAdi { get; set; }       // "16+1", "27+1"

    // SEFER TİPİ
    public SeferTipi SeferTipi { get; set; }       // Sabah, Akşam, SabahAksam, Mesai, vb.

    // REFERANSLAR
    public int? VarsayilanAracId { get; set; }     // Standart araç
    public int? VarsayilanSoforId { get; set; }    // Standart şoför
    public int? KurumId { get; set; }              // Müşteri (Kurum)
    public int CariId { get; set; }                // Müşteri (Cari - Eski uyumluluk)

    // DURUM
    public bool Aktif { get; set; }
    public string? Notlar { get; set; }
}

public enum SeferTipi
{
    Sabah = 1,        // 06:00-08:30
    Aksam = 2,        // 16:00-19:00
    SabahAksam = 3,   // Hem sabah hem akşam (2 sefer/gün)
    Saatlik = 4,      // Saatlik
    Mesai = 5,        // Vardiya (08:00-17:00)
    Vardiya = 6       // Gece vardiyası
}
```

**Güzergah Analiz Tablosu**:

| Alan | Örnek | Önem |
|------|-------|------|
| GuzergahKodu | GZR-TRT-001 | 🔴 Kritik |
| GuzergahAdi | TRT Ankara Merkez | 🔴 Kritik |
| BirimFiyat (Gelir) | 150 TL/sefer | 🔴 **Kritik** |
| GiderFiyat (Gider) | 80 TL/sefer | 🔴 **Kritik** |
| SeferTipi | SabahAksam | 🔴 Kritik |
| Mesafe | 12.5 km | 🟡 İstatistik |
| PersonelSayisi | 12 kişi | 🟡 İstatistik |
| PuantajCarpani | 1.0 / 0.5 / 1.5 | 🟡 Hafta sonu/Fazla |
| Aktif | true/false | 🔴 Kritik |

#### Sefer Slot (Operasyonel Detay)

Bir güzergahta birden fazla sefer slotu (sabah, akşam, öğle) tanımlanabilir:

```csharp
public class GuzergahSefer : BaseEntity, IFirmaTenant
{
    public int GuzergahId { get; set; }
    public int Sira { get; set; }                  // 1., 2., 3. sefer
    public SeferSlot Slot { get; set; }            // Sabah, Aksam, Og, Mesai, ...
    public string? KapasiteAdi { get; set; }       // "16+1", "8+1"
    public int? AracId { get; set; }
    public string? SoforAd { get; set; }
    public string? FirmaAdiSerbest { get; set; }   // Tedarikçi
}

public enum SeferSlot
{
    Sabah = 1,       // 06:00
    Aksam = 2,       // 17:00
    Og = 3,          // 12:00
    Mesai = 4,       // 08:00-17:00 (Tam gün)
    Diger1 = 5       // Özel slot
}
```

**Örnek Sefer Yapılandırması**:

```
Güzergah: GZR-TRT-MERKEZ
├─ Sefer 1: Sabah (06:00) → 16+1 kapasite → Ali Demir (34CX8)
├─ Sefer 2: Akşam (17:00) → 16+1 kapasite → Ali Demir (34CX8)
└─ [Opsiyonel Sefer 3: Öğle (12:00) → 8+1 kapasite → Veli Kara (34DX5)]
```

---

### 3.3 FiloGuzergahEslestirme & FiloGunlukPuantaj Akışı

Operasyonel puantaj **iki seviyede** çalışır:

#### Seviye 1: Şablon (FiloGuzergahEslestirme)

Bir kurum için **sabit** bir rota-araç-şoför kombinasyonu:

```
FiloGuzergahEslestirme (Şablon)
├─ Id: 12
├─ KurumFirmaId: 42 (TRT-ANKARA)
├─ GuzergahId: 3 (TRT Merkez)
├─ AracId: 1 (34CX8)
├─ SoforId: 5 (Ali Demir)
├─ ServisTuru: SabahAksam (3)
├─ KurumaKesilecekUcret: 150 TL  ← Guzergah.BirimFiyat
├─ TaseronaOdenenUcret: 80 TL    ← Guzergah.GiderFiyat
└─ IsActive: true
```

#### Seviye 2: Günlük Kayıt (FiloGunlukPuantaj)

Her gün için otomatik oluşturulan gerçek kaydı:

```
FiloGunlukPuantaj
├─ Id: 1001-1022 (22 gece, Ocak'ta)
├─ FiloGuzergahEslestirmeId: 12 (FK - Şablona link)
├─ Tarih: 2025-01-02, 2025-01-03, ...
├─ GuzergahId: 3
├─ AracId: 1
├─ SoforId: 5
├─ KurumFirmaId: 42
├─ SeferSayisi: 2 (Sabah + Akşam)
├─ PuantajCarpani: 1.0 (Normal gün) / 0.5 (Pazar) / 1.5 (Fazla)
├─ TahakkukEdenKurumUcreti: 2 × 150 × 1.0 = 300 TL
├─ TahakkukEdenTaseronUcreti: 2 × 80 × 1.0 = 160 TL
├─ Durum: Gitti / Gitmedi / Arızalandı
└─ Onaylandi: true (Terminal kontrolünden sonra)
```

**Veri Akışı Diyagramı**:

```
ADMIN                             SİSTEM                          GÜN
│                                  │                               │
├─ Şablon Oluştur ──────────────→ FiloGuzergahEslestirme:12       │
│  (KurumId, GuzergahId, AracId)  ├─ BirimFiyat=150               │
│                                  └─ GiderFiyat=80               │
│                                                                  │
│                                  ├─ Otomatik Job (04:00)        │
│                                  │  "Yarın için puantaj üret"  │
│                                  │                              ▼
│                                  └─→ INSERT FiloGunlukPuantaj:1001
│                                     (Tarih=2025-01-02, 
│                                      SeferSayisi=2, 
│                                      TahakkukEdenKurumUcreti=300)
│                                                                  │
│                                                            ✓ Sabah 06:30
│                                                            ✓ Rota işlemiyor
│                                                            ✓ Akşam 18:00
│                                                                  │
│                                                   ✓ Terminal kontrolü
│                                                   ✓ Onaylandi=true
│                                                   ✓ Durum=Gitti
│                                                                  │
│  ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← ← AY SONU JOB (28 Ocak) ← ← ← ← ←
│
├─ Hakedis Oluş tur
│  "GuzergahId 3 → 22 gün × 2 sefer × 150 = 6.600 TL"
│
└─→ HAKEDIS FATURASI
    └─ TRT-ANKARA'ya 6.600 TL fatura tutuşturuldu
```

---

### 3.4 Snapshot Pattern (Fiyat Stabilitesi)

#### Entity: `FiloGunlukPuantaj`

```csharp
public class FiloGunlukPuantaj
{
    public int Id { get; set; }

    // Ana Bilgiler
    public int FirmaId { get; set; }
    public DateTime Tarih { get; set; }  // Hizmetin yapıldığı gün

    // Referanslar
    public int GuzergahId { get; set; }  // Rota
    public int AracId { get; set; }      // Araç (Plaka)
    public int SoforId { get; set; }     // Şoför
    public int KurumFirmaId { get; set; } // Müşteri (Kurum/Cari)

    // Servis Bilgisi
    public ServisTuru ServisTuru { get; set; }  // Sabah/Akşam/Sabah+Akşam

    // Durum
    public enum CalismaDurum
    {
        Planli = 1,              // Plan aşamasında
        Tamamlandi = 2,          // Hizmet tamamlandı
        IptalEdildi = 3,         // Hizmet iptal
        ArizaNedeniyleYapilamadi = 4  // Araç arızası nedeni
    }
    public CalismaDurum Durum { get; set; }

    // Zoom İnceleme
    public bool PuantajAlinmismi { get; set; }
    public DateTime? PuantajAlmaTarihi { get; set; }
}
```

### 3.3 Operasyonel Puantaj Kaydedilme Akışı

```
┌─────────────────────────────────────────────────────────┐
│ OPERASYON ETSİP                                          │
├─────────────────────────────────────────────────────────┤
│
│  Gün Başında:
│  ┌─ Planlama Ekibi                                  ┐
│  │  - Günün atanacak Araç/Şoför seçimi             │
│  │  - Rota ataması (Güzergah)                      │
│  │  - Kurum (Müşteri) doğrulaması                  │
│  │  - Sistem'de FiloGunlukPuantaj → Taslak        │
│  └─────────────────────────────────────────────────┘
│
│  Gün Esnasında:
│  ┌─ Şoför (Araç'ta)                               ┐
│  │  - Sabah 06:30'de Başlangıç Noktası'nda top.  │
│  │  - Rota boyunca personel alım/bırakım         │
│  │  - Akşam 18:00'de Bitiş Noktası'na varış      │
│  │  - Araç anomali yok → ServisTamamlandi        │
│  └─────────────────────────────────────────────────┘
│
│  Gün Sonunda:
│  ┌─ Terminalde Kontrol                            ┐
│  │  - Şoför KM sayaçı kontrolü                    │
│  │  - Anoğraf/Emniyet belgeleri kontrol          │
│  │  - Sistem'de FiloGunlukPuantaj → Tamamlandı   │
│  │  - Eğer arıza varsa → Anomali kaydı            │
│  └─────────────────────────────────────────────────┘
│
│  Haftalık Özet:
│  ┌─ Operasyon Müdürü                              ┐
│  │  - Hafta bitiminde tüm puantaj kaydı review  │
│  │  - Anomali varsa: İnceleme, düzeltme          │
│  │  - Raporlama: Filo Puantaj Özeti              │
│  │  - Excel Export: Muhasebe'ye gönderme         │
│  └─────────────────────────────────────────────────┘
│
└─────────────────────────────────────────────────────────┘
```

### 3.4 Operasyonel Puantaj Aylık Özeti

**Aylık Toplaması**:

| Kriter | Hesaplama | Örnek |
|--------|-----------|-------|
| **Ayda Çalışan Gün Sayısı** | (Çalışma Günü - İzin - Arıza) | 22 gün |
| **Toplam Sefer Sayısı** | Ayda Çalışan Gün × Sefer/Gün | 22 × 2 = 44 sefer |
| **Gelir (Kuruma Faturalanacak)** | Toplam Sefer × Sefer Fiyatı | 44 × 150 = 6,600 TL |
| **Gider (Şoför/Araç Sahibine)** | Toplam Sefer × Gider Birim Fiyatı | 44 × 80 = 3,520 TL |
| **KDV (Gelire eklenir)** | Gelir × %20 | 6,600 × 0.20 = 1,320 TL |
| **Kâr (Firma)** | Gelir - Gider | 6,600 - 3,520 = 3,080 TL |

---

## 4. Personel Puantajı (Maaş)

### 4.1 Tanım

**Personel Puantajı**, şoför/operasyon personelinin ayda kaç gün çalıştığı, ne kadar fazla mesai yaptığı vb. bilgilerin kaydıdır.

- **Amaç**: Aylık maaş hesaplamak ve ödeme yapmak
- **Frekans**: Aylık (ayın son günü finalize edilir)
- **Sorucu**: İnsan Kaynakları
- **Kullanıcı**: Muhasebe (Bordro yazılımı input'u)

### 4.2 Personel Puantajı Kaydı Yapısı

#### Entity: `PersonelPuantaj`

```csharp
public class PersonelPuantaj : BaseEntity, IFirmaTenant
{
    // Tenant
    public int FirmaId { get; set; }

    // Referans
    public int PersonelId { get; set; }  // Şoför, Mekanik, vs.
    public int Yil { get; set; }
    public int Ay { get; set; }   // 1-12

    // Çalışma Bilgisi
    public int CalisilanGun { get; set; }       // Fiilen çalışılan gün (operative)
    public decimal FazlaMesaiSaat { get; set; } // Fazla mesai saati
    public int IzinGunu { get; set; }           // Resmi/Ara izin
    public int MazeretGunu { get; set; }        // Rapor/Mazeret izni

    // Maaş Bileşenleri (Gelir)
    public decimal BrutMaas { get; set; }   // Taban maaş
    public decimal YemekUcreti { get; set; }
    public decimal YolUcreti { get; set; }
    public decimal Prim { get; set; }       // Performans bonusu
    public decimal DigerOdeme { get; set; } // Borç silme, hediye, vb.

    // Kesintiler
    public decimal SgkKesinti { get; set; }   // Sigorta priminin personel payı
    public decimal GelirVergisi { get; set; } // KDV/Stopaj
    public decimal DamgaVergisi { get; set; }
    public decimal DigerKesinti { get; set; }

    // Sonuç
    public decimal NetOdeme { get; set; }  // Ödenen miktar
    public DateTime? OdemeTarihi { get; set; }

    // Durum
    public enum OdemeDurumu
    {
        Hazirlanıyor = 1,
        Onaylı = 2,
        Ödendi = 3,
        İptal = 4
    }
    public OdemeDurumu Durum { get; set; }
}
```

### 4.3 Personel Puantajı Hazırlama Akışı

```
┌─────────────────────────────────────────────────────────┐
│ PERSONEL PUANTAJI HAZIRLAMA                              │
├─────────────────────────────────────────────────────────┤
│
│  Haftada 5 Gün İdeal Çalışma:
│  ┌─ İnsan Kaynakları                                ┐
│  │  - FiloGunlukPuantaj'dan otomattik al           │
│  │  - Günlük: "Tamamlandı" kaydı = Çalışılan gün  │
│  │  - Eksik: "İptal/Arıza" = İzin/Mazeret         │
│  │  - Çalışılan Gün = Tamamlandı Kayıtları        │
│  │  - Aylık Özet: Sum(Tamamlandı)                 │
│  └─────────────────────────────────────────────────┘
│
│  Ay Sonu (25-27. gün):
│  ┌─ PersonelPuantaj Finalize                      ┐
│  │  1. Çalışılan Gün Sayısı: 20 gün              │
│  │  2. Fazla Mesai: (20 gün × 8 saat) = 160 saat│
│  │     Gerekirse: fazla mesai saati manuel gir   │
│  │  3. İzin/Mazeret: Yönetici onayı              │
│  │  4. Prim: Performans bazlı (isteğe bağlı)     │
│  └─────────────────────────────────────────────────┘
│
│  Maaş Hesaplama:
│  ┌─ Muhasebe/İK                                    ┐
│  │  BrutMaas      = 8.500 TL                      │
│  │  YemekUcreti   =   750 TL (20 gün × 37.5)     │
│  │  YolUcreti     =   200 TL                      │
│  │  FazlaMesai    = 160 saat × 53 TL = 8.480 TL │
│  │  Prim          = Bonuslar = 1.000 TL          │
│  │  ──────────────────────────────────────────────│
│  │  TOPLAM GELİR  = 18.930 TL                     │
│  │                                                 │
│  │  SGK (-) %14   = 18.930 × 0.14 = -2.650 TL   │
│  │  GV (-)  %15   = 18.930 × 0.15 = -2.840 TL   │
│  │  ──────────────────────────────────────────────│
│  │  NET ÖDEME     = 13.440 TL                     │
│  │                                                 │
│  │  Durum: Onaylı ✓                              │
│  │  Ödeme Tarihi: 2025-02-05                     │
│  └─────────────────────────────────────────────────┘
│
└─────────────────────────────────────────────────────────┘
```

### 4.4 Örnek: Şoför Ali Demir - Ocak 2025

```
Personel Puantaj Formu
═════════════════════════════════════════════════════════
Adı Soyadı: Ali Demir
Görevü: Şoför
Sözleşme Türü: Tam Zamanlı
Aylık Brüt: 8.500 TL

Çalışma Bilgisi:
─────────────────────────────────────────────────────────
Çalışılan Gün         : 20 gün (Pazarlar hariç, 2 izin)
Fazla Mesai           : 160 saat (20 × 8)
Resmi İzin            : 2 gün
Rapor (Mazeret)       : 0 gün

Gelir Bileşenleri:
─────────────────────────────────────────────────────────
Brüt Maaş             : 8.500,00 TL
Yemek Ücreti          :   750,00 TL (20 gün × 37,50)
Yol Ücreti            :   200,00 TL
Fazla Mesai (160st @53): 8.480,00 TL
Performans Bonusu     : 1.000,00 TL
                       ─────────────
TOPLAM GELİR          : 18.930,00 TL

Kesintiler:
─────────────────────────────────────────────────────────
SGK Kesintisi (%14)   : -2.650,20 TL
Gelir Vergisi (%15)   : -2.839,50 TL
Damga Vergisi         :    -0,00 TL
Diğer Kesintiler      :    -0,00 TL
                       ─────────────
TOPLAM KESİNTİ        : -5.489,70 TL

NET ÖDEME             : 13.440,30 TL
─────────────────────────────────────────────────────────

Durum                 : Onaylı ✓
Ödeme Tarihi          : 2025-02-08
Durum Bildirimi       : Banka Transferi Tamamlandı
Havale Referans       : TR97001006000050053000000043

Onay İmzası          : Muhasebe Müdürü (E-İşaretli)
Tarih                : 2025-02-08 14:30
```

---

## 5. Hakediş ve Raporlama

### 5.1 Hakediş (İnvoicing) Nedir?

**Hakediş**, operasyonel puantajlardan aylık toplamın alınarak müşteriye (Kurum) fatura kesilmesi işlemidir.

#### Entity: `HakedisPuantaj` ve `Hakedis`

**HakedisPuantaj**: Aylık toplamı (Ana Kayıt)

```csharp
public class HakedisPuantaj : BaseEntity, IFirmaTenant
{
    // Dönem
    public int Yil { get; set; }
    public int Ay { get; set; }

    // Referans (İlişkiler)
    public int GuzergahId { get; set; }
    public int AracId { get; set; }
    public int SoforId { get; set; }
    public int CariId { get; set; }  // Müşteri

    // Fiyatlandırma
    public decimal GelirBirimFiyat { get; set; }  // Müşteriye ücret
    public decimal GiderBirimFiyat { get; set; }  // Tedarikçiye ücret
    public decimal GunlukSeferSayisi { get; set; } // 2 (Sabah+Akşam)

    // Toplamlar
    public int ToplamSefer { get; set; }           // 44 sefer (22 çalışma günü)
    public decimal GelirToplam { get; set; }        // 44 × 150 = 6.600 TL
    public decimal GiderToplam { get; set; }        // 44 × 80 = 3.520 TL
    public decimal KdvTutari { get; set; }          // 3.520 × 0.20 = 704 TL
    public decimal OdenecekTutar { get; set; }      // 3.520 + 704 = 4.224 TL

    // Durum
    public HakedisDurumu Durum { get; set; }    // Taslak / Onaylandı
}
```

**Hakedis**: Müşteri bazında aylık toplam (Raporlama)

```csharp
public class Hakedis : BaseEntity, IFirmaTenant
{
    // Dönem
    public int Yil { get; set; }
    public int Ay { get; set; }

    // Tip
    public HakedisTipi Tip { get; set; }  // Kurum / Tedarikçi / Araç
    public int ReferansId { get; set; }   // CariId / AracId / vb

    // Toplamlar
    public decimal ToplamSeferSayisi { get; set; }  // Tüm sefers
    public decimal Tutar { get; set; }              // $TOPLAM (KDV hariç)
    public decimal KdvTutar { get; set; }           // KDV
    public decimal GenelToplam { get; set; }        // $TOPLAM (KDV dahil)

    // Durum
    public HakedisDurum Durum { get; set; }  // Taslak / Onaylandı
    public int? FaturaId { get; set; }       // Bağlı Fatura (varsa)
}
```

### 5.2 Hakediş Oluşturma Adımları

```
┌─────────────────────────────────────────────────────────┐
│ AYLIK HAKEDİŞ OLUŞTURMA (Ay Sonu 3-5. Gün)             │
├─────────────────────────────────────────────────────────┤
│
│  ADIM 1: Operasyonel Veriler Finalize
│  ┌─ FiloGunlukPuantaj Özetleme                     ┐
│  │  - SELECT * FROM FiloGunlukPuantaj               │
│  │    WHERE Yil=2025 AND Ay=1                      │
│  │    AND Durum='Tamamlandı'                       │
│  │                                                  │
│  │  - Örnek: 44 sefer kaydı elde edilir           │
│  └──────────────────────────────────────────────────┘
│
│  ADIM 2: Aylık HakedisPuantaj Oluşturmak
│  ┌─ Her Güzergah/Araç/Şoför/Cari için satır       ┐
│  │  1. SELECT SUM(Sefer) for (Guzergah, Arac, Sofor, Cari)
│  │  2. INSERT INTO HakedisPuantaj:                 │
│  │     - Yil, Ay, Guzergah, Arac, Sofor, Cari     │
│  │     - GelirBirimFiyat = 150 TL/gün (tariften)  │
│  │     - GiderBirimFiyat = 80 TL/gün (tariften)   │
│  │     - ToplamSefer = 44                          │
│  │     - GelirToplam = 44 × 150 = 6.600 TL        │
│  │     - GiderToplam = 44 × 80 = 3.520 TL         │
│  │     - KdvTutari = 3.520 × 0.20 = 704 TL        │
│  │     - OdenecekTutar = 4.224 TL                 │
│  │     - Durum = "Taslak"                          │
│  └──────────────────────────────────────────────────┘
│
│  ADIM 3: Müşteri (Cari) Bazında Topla
│  ┌─ Hakedis Kaydı Oluşturmak                       ┐
│  │  INSERT INTO Hakedis:                           │
│  │  - Yil=2025, Ay=1                              │
│  │  - Tip = "Kurum"                               │
│  │  - ReferansId = 42 (TRT-ANKARA CariId)         │
│  │  - SELECT SUM(GelirToplam) from HakedisPuantaj │
│  │    WHERE Cari=42 AND Yil=2025 AND Ay=1         │
│  │  - Örnek TRT-ANKARA için 3 Kurum vardır:       │
│  │    * TRT Merkez: 6.600 TL                      │
│  │    * TRT Çiftçiler: 4.500 TL                   │
│  │    * TRT Dış Ticaret: 3.300 TL                 │
│  │  - ToplamSeferSayisi = 66                      │
│  │  - Tutar = 14.400 TL (KDV hariç)               │
│  │  - KdvTutar = 2.880 TL                         │
│  │  - GenelToplam = 17.280 TL                     │
│  │  - Durum = "Taslak"                            │
│  └──────────────────────────────────────────────────┘
│
│  ADIM 4: Onay ve Raporlama
│  ┌─ Operasyon Müdürü Kontrol                      ┐
│  │  - Hakedis raporu gözden geçir                  │
│  │  - Anomali varsa: Düzeltme                     │
│  │  - Onay: Durum = "Onaylandı"                   │
│  │  - Export: Excel / PDF                         │
│  └──────────────────────────────────────────────────┘
│
│  ADIM 5: Faturaya Dönüştürmek
│  ┌─ Fatura Oluştur                                │
│  │  - Hakedis.Durum = "Onaylandı"                 │
│  │  - CREATE Fatura FROM Hakedis                  │
│  │  - İnşaat.FaturaId = Hakedis.Id               │
│  │  - Fatura Tarihi: Aya ait son gün              │
│  │  - Vade: Kuruma göre (30-60 gün)              │
│  │  - Fatura Durum = "Yayındatılmış"             │
│  └──────────────────────────────────────────────────┘
│
└─────────────────────────────────────────────────────────┘
```

### 5.3 Hakediş Rapor Örneği: TRT-ANKARA (Ocak 2025)

```
╔══════════════════════════════════════════════════════════════╗
║            AYLIK HAKEDİŞ RAPORU                             ║
║────────────────────────────────────────────────────────────────║
║ Müşteri: TRT-ANKARA (Cari ID: 42)                           ║
║ Dönem: Ocak 2025 (2025-01-01 s.d. 2025-01-31)              ║
║ Rapor Tarihi: 2025-02-03                                   ║
╚══════════════════════════════════════════════════════════════╝

A. KURUMLAR İTİBARİYLA DETAY
──────────────────────────────────────────────────────────────

┌─ Kurum 1: TRT Ankara Merkez Servisi
│  Araç: 34CX8 (Ford Transit)
│  Şoför: Ali Demir
│  Güzergah: Paşaköy → Dış Ticaret Şubesi (12 km)
│  Tarifler: 150 TL/gün (S+A)
│
│  İstatistik:
│  ├─ Çalışma Günü: 22 gün
│  ├─ Günlük Sefer: 2 (Sabah + Akşam)
│  ├─ Toplam Sefer: 44
│  ├─ Arıza Günü: 0
│  ├─ İzin Günü: 1 (Tatil)
│  └─ Mazeret Günü: 1 (Mazeret Raporu)
│
│  Gelir (Kuruma Faturalanacak):
│  ├─ Sefer Fiyatı: 150 TL
│  ├─ Sefer Sayısı: 44
│  ├─ Toplam Gelir: 6.600,00 TL
│  ├─ KDV %20: 1.320,00 TL
│  └─ Toplam (KDV dahil): 7.920,00 TL
│
│  Gider (Tedarikçiye Ödenecek):
│  ├─ Tedarikçi Ücret: 80 TL/gün (sefer başı 40 TL)
│  ├─ Sefer Sayısı: 44
│  ├─ Toplam Gider: 3.520,00 TL
│  ├─ KDV %20: 704,00 TL
│  └─ Ödenecek (KDV dahil): 4.224,00 TL
│
│  KAR MARJ: 7.920 - 4.224 = 3.696 TL (%46,6)
│
│  Durum: Onaylandı ✓
└─────────────────────────────────────────────────────────────

┌─ Kurum 2: TRT Ankara Çiftçiler Pazarı Şubesi
│  Araç: 06TX2 (Hyundai H350)
│  Şoför: Veli Kaya
│  Güzergah: Kızılay → Çiftçiler Pazarı (8 km)
│  Tarifler: 120 TL/gün (S+A)
│
│  İstatistik:
│  ├─ Çalışma Günü: 20 gün
│  ├─ Günlük Sefer: 2
│  ├─ Toplam Sefer: 40
│  ├─ Arıza Günü: 1
│  ├─ İzin Günü: 1
│  └─ Mazeret Günü: 0
│
│  Gelir: 40 × 120 = 4.800,00 TL + KDV = 5.760,00 TL
│  Gider: 40 × 70 = 2.800,00 TL + KDV = 3.360,00 TL
│  KAR MARJ: 5.760 - 3.360 = 2.400 TL (%41,7)
│
│  Durum: Onaylandı ✓
└─────────────────────────────────────────────────────────────

┌─ Kurum 3: TRT Ankara Dış Ticaret Şubesi
│  Araç: 24RH4 (Mercedes Sprinter)
│  Şoför: Mehmet Yıldız
│  Güzergah: Kavaklidere → Ziraat Bankası (10 km)
│  Tarifler: 130 TL/gün (S+A)
│
│  İstatistik:
│  ├─ Çalışma Günü: 18 gün
│  ├─ Günlük Sefer: 2
│  ├─ Toplam Sefer: 36
│  ├─ Arıza Günü: 2
│  ├─ İzin Günü: 2
│  └─ Mazeret Günü: 0
│
│  Gelir: 36 × 130 = 4.680,00 TL + KDV = 5.616,00 TL
│  Gider: 36 × 75 = 2.700,00 TL + KDV = 3.240,00 TL
│  KAR MARJ: 5.616 - 3.240 = 2.376 TL (%42,3)
│
│  Durum: Onaylandı ✓
└─────────────────────────────────────────────────────────────

B. MÜŞTERİ (CARİ) TOPLAM
──────────────────────────────────────────────────────────────

Müşteri: TRT-ANKARA

Tüm Kurumlar Toplamı:
├─ Toplam Sefer: 44 + 40 + 36 = 120 sefer
├─ Toplam Gelir (KDV hariç): 6.600 + 4.800 + 4.680 = 16.080 TL
├─ Toplam Gelir (KDV dahil): 7.920 + 5.760 + 5.616 = 19.296 TL
├─ Toplam Gider (KDV dahil): 4.224 + 3.360 + 3.240 = 10.824 TL
└─ Net Kar: 19.296 - 10.824 = 8.472 TL

Hakedis İstatistikleri:
├─ Toplam Rota Sayısı: 3
├─ Toplam Araç: 3
├─ Toplam Şoför: 3
├─ Ortalama Seçi/Gün: 120 / 60 çalışma günü = 2 sefer
└─ Ortalama Kar Marjı: 8.472 / 19.296 * 100 = %43,9

C. YAKLAŞAN KÖLEMLERİ
──────────────────────────────────────────────────────────────

Adı: Ocak 2025 Hakediş - TRT-ANKARA
Fatura No: FTR-2025-00142
Fatura Tarihi: 2025-02-01
Müşteri: TRT-ANKARA
Vade: 45 gün (Vade Sonu: 2025-03-18)

Net Toplam (KDV hariç): 16.080,00 TL
Vergi Tutarı (%20): 3.216,00 TL
Toplam (KDV dahil): 19.296,00 TL

Durum: Onaylandı ✓
Fatura Durumu: Yayındatılıyor (Bekleme)

Muhasebeci Onayı: Zeynep Tanış (E-İşaretli)
Tarih: 2025-02-03 16:45
```

---

## 6. Veri Akışı ve Entegrasyon

### 6.1 Sistem Mimarisi

```
┌────────────────────────────────────────────────────────────┐
│                  PERSONEL TAŞIMA PTS                       │
│              (Muhasebe + Operasyon Sistemi)                │
├────────────────────────────────────────────────────────────┤
│
│  A. OPERASYON REGİSTRA
│  ┌───────────────────────────────────────────────────┐
│  │ • FiloGunlukPuantaj                                │
│  │   (Günlük: Araç-Şoför-Rota-Müşteri-Durum)        │
│  │ • ServisCalisma                                    │
│  │   (Hizmet detayı: Saat, KM, Ücret vs.)           │
│  │ • AracMasraf                                       │
│  │   (Arıza, Bakım vb.)                              │
│  │ • Anomali Detection Engine                        │
│  │   (Bozuk veriler, çift kayıt vs.)                │
│  └───────────────────────────────────────────────────┘
│
│  ↓ (Aylık Topla)
│
│  B. PUANTAJ TOPLAMASI
│  ┌───────────────────────────────────────────────────┐
│  │ • HakedisPuantaj                                   │
│  │   (Aylık: Guzergah × Arac × Sofor × Cari)        │
│  │ • HakedisPuantajToplamı                           │
│  │   (Aylık: Cari × Guzergah × Arac)                │
│  │ • PersonelPuantaj (bağımsız)                      │
│  │   (Aylık: Personel maaş hesapları)               │
│  └───────────────────────────────────────────────────┘
│
│  ↓ (Onay)
│
│  C. HAKEDİŞ VE RAPORLAMA
│  ┌───────────────────────────────────────────────────┐
│  │ • Hakedis (Fatura öncesi)                         │
│  │ • HakedisRapor (Aylık özeti)                     │
│  │ • KarAnaliz (Kârlılık raporu)                    │
│  │ • AracMaliyetSnapshotla (Araç ana yer tabı)     │
│  └───────────────────────────────────────────────────┘
│
│  ↓ (Muhasebeci Onayı)
│
│  D. FAKTURACİLİK VE ÖDEME
│  ┌───────────────────────────────────────────────────┐
│  │ • Fatura (Kuruma vergi faturası)                 │
│  │ • Tahsil (Kurumdan tahsil)                       │
│  │ • Ödeme Emri (Tedarikçiye ödeme)                │
│  │ • Bordro (Personel ödeme listesi)               │
│  └───────────────────────────────────────────────────┘
│
│  ↓ (Banka Transferi)
│
│  E. MUHASEBE KAPATIŞI
│  ┌───────────────────────────────────────────────────┐
│  │ • Aylik Muhasebe Uzlaşması                       │
│  │ • KDV Bildirgesi Hazırlığı                       │
│  │ • Vergi Stopaj Hesaplamak                        │
│  │ • Nakit Akış Raporu                             │
│  └───────────────────────────────────────────────────┘
│
└────────────────────────────────────────────────────────────┘
```

### 6.2 Veri Akış Grafiği (Timing)

```
GÜN 1-31: Operasyon
   ↓
   FiloGunlukPuantaj günlük kaydı (44 kayıt)
        + ServisCalisma detayları
        + AracMasraf anomalisı (varsa)

GÜN 1 (Ay Başında): Plan
   ↓
   Sistem PersonelPuantaj → "Hazırlanıyor" durumunda başlat

GÜN 25-27 (Ay Sonu): Personel Finalize
   ↓
   İK: PersonelPuantaj → Onay
        - CalisilanGun = 20 gün
        - BrutMaas, KesintI vs. enter
        - Durum: Onaylı

GÜN 28 (Muhasebe Başı): Hakedis Oluştur
   ↓
   Sistem Otomatik:
        1. FiloGunlukPuantaj'dan topla (44 kayıt)
        2. HakedisPuantaj oluştur + hesapla
        3. Hakedis oluştur (Cari toplamı)
        4. Durum: Taslak

GÜN 28-29: Operasyon Kontrol
   ↓
   Operasyon Müdürü:
        - Hakedis raporunu gözden geçir
        - Anomali varsa: Düzelt (FiloGunlukPuantaj'da)
        - Sonra yeniden hesapla
        - Durum: Onaylandı

GÜN 29: Muhasebe Kontrol
   ↓
   Muhasebeci:
        - Hakedis raporu denetim
        - Fatura oluştur
        - Hakedis.Durum: "Faturaya Dönüştürüldü"
        - Fatura.Durum: "Yayındatılmış"

GÜN 30: Ödeme
   ↓
   Banka Transferi:
        - Fatura: Kuruma gönder
        - Ödeme Emri: Tedarikçi havale
        - Bordro: Personel transfer

GÜN 31 + 1-5: Muhasebe Kapanış
   ↓
   - Aylik Uzlaşması tamamlandı
   - KDV hesaplanmış
   - Vergi Stopaj hesaplanmış
   - Nakit Akış güncellenmiş
```

---

## 7. Sistem Komponenleri

### 7.1 Veritabanı Şeması (Basit Görünüm)

```sql
┌─────────────────────────────┐
│         Firma               │
│ (Şirket Merkezî)            │
├─────────────────────────────┤
│ Id (PK)                     │
│ Ad, VergiNo, Adres         │
│ AktivTarihi, PasifTarihi   │
└──────────────┬──────────────┘
               │ (1-to-N)
               ↓
┌─────────────────────────────┐
│      Cari                   │
│ (Müşteri/Tedarik/Personel) │
├─────────────────────────────┤
│ Id (PK)                     │
│ CariKodu, Unvan            │
│ CariTipi (M/T/P/O)         │
│ FirmaId (FK)               │
└──────────────┬──────────────┘
               │ (1-to-N)
               ├→ Kurum (Alt alt)
               ├→ Sofor (Tahmin)
               └→ TasımaTedarıkçı

┌─────────────────────────────┐
│       Guzergah              │
│ (Rota)                      │
├─────────────────────────────┤
│ Id (PK)                     │
│ Ad (Paşaköy → DışŞ.)       │
│ Mesafe (km)                │
│ BirimFiyat (150 TL)        │
│ FirmaId (FK)               │
└──────────────┬──────────────┘
               │ (1-to-N) ←┐
               ↓            │
┌──────────────────────────────────┐
│    FiloGuzergahEslestirme       │
│ (Araç-Şoför taklasi)            │
├──────────────────────────────────┤
│ Id (PK)                          │
│ GuzergahId (FK) ←────────────────┤
│ AracId (FK)                      │
│ SoforId (FK)                     │
│ KurumFirmaId (FK → Cari)        │
│ ServisTuru, KorumaÜcreti        │
│ Durum (Aktif/Pasif)            │
│ FirmaId (FK)                     │
└──────────────┬──────────────────┘
               │ (1-to-N)
               ↓
┌──────────────────────────────────┐
│    FiloGunlukPuantaj             │
│ (Günlük Hizmet Kaydı)            │
├──────────────────────────────────┤
│ Id (PK)                          │
│ Tarih                            │
│ GuzergahId (FK)                  │
│ AracId (FK)                      │
│ SoforId (FK)                     │
│ KurumFirmaId (FK → Cari)        │
│ ServisTuru (Sabah/Akşam)        │
│ Durum (Planlandı/Tamamlandı)   │
│ FirmaId (FK)                     │
└──────────────┬──────────────────┘
               │ (1-to-N)
               ↓
┌──────────────────────────────────┐
│    HakedisPuantaj                │
│ (Aylık Toplam - Guzergah×Arac)  │
├──────────────────────────────────┤
│ Id (PK)                          │
│ Yil, Ay                          │
│ GuzergahId (FK)                  │
│ AracId (FK)                      │
│ SoforId (FK)                     │
│ CariId (FK)                      │
│ ToplamSefer (44)                │
│ GelirBirimFiyat (150)            │
│ GiderBirimFiyat (80)             │
│ GelirToplam, GiderToplam        │
│ KdvTutari, Durum                │
│ FirmaId (FK)                     │
└──────────────┬──────────────────┘
               │ (1-to-N)
               ↓
┌──────────────────────────────────┐
│    Hakedis                       │
│ (Müşteri Bazında Aylık Toplam)  │
├──────────────────────────────────┤
│ Id (PK)                          │
│ Yil, Ay                          │
│ Tip (Kurum/Tedarik)             │
│ ReferansId (CariId)              │
│ ToplamSeferSayisi, Tutar        │
│ KdvTutar, GenelToplam           │
│ Durum, OnayTarihi               │
│ FaturaId (FK)                    │
│ FirmaId (FK)                     │
└──────────────┬──────────────────┘
               │ (1-to-1)
               ↓
┌──────────────────────────────────┐
│    Fatura                        │
│ (Muşteri'ye kesilecek)          │
├──────────────────────────────────┤
│ Id (PK)                          │
│ FaturaNo, FaturaTarihi           │
│ CariId (FK)                      │
│ Tutar, KdvTutar                  │
│ Durum (Taslak/Yayındatılmış)    │
│ FirmaId (FK)                     │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│    PersonelPuantaj               │
│ (Maaş Hesap Bilgileri) [BAĞIMSIZ]│
├──────────────────────────────────┤
│ Id (PK)                          │
│ PersonelId (FK → Cari/Sofor)    │
│ Yil, Ay                          │
│ CalisilanGun (20)                │
│ FazlaMesaiSaat (160)             │
│ BrutMaas, YemekÜcreti            │
│ SgkKesinti, GelirVergisi         │
│ NetOdeme, OdemeTarihi            │
│ Durum (Onay/Ödendi)              │
│ FirmaId (FK)                     │
└──────────────────────────────────┘
```

### 7.2 API Endpoints

```
OPERASYONEL PUANTAJ
───────────────────────────────────────────────────────────

GET    /api/filo-puantaj
       - Tüm günlük puantaj kayıtları

GET    /api/filo-puantaj/{yil}/{ay}
       - Belirli ay puantajları

GET    /api/filo-puantaj/{id}
       - Tek kayıt detayı

POST   /api/filo-puantaj
       - Yeni puantaj kaydı

PUT    /api/filo-puantaj/{id}
       - Puantaj düzenleme

DELETE /api/filo-puantaj/{id}
       - Puantaj silme


HAKEDIŞ ve RAPORLAMA
───────────────────────────────────────────────────────────

POST   /api/hakedis/generate
       - Aylık hakedis oluştur
       - Param: Yil, Ay, CariId (opsiyonel)

GET    /api/hakedis/{yil}/{ay}
       - Aylık hakedis listesi

GET    /api/hakedis/{id}
       - Hakedis detayı (PDF/JSON)

PUT    /api/hakedis/{id}/onay
       - Hakedis onay kütlüğü

GET    /api/hakedis-rapor/{yil}/{ay}
       - Cari bazında özet rapor

GET    /api/kar-analiz/{yil}/{ay}
       - Guzergah/Arac/Cari bazında kâr analizi


PERSONEL PUANTAJ & BORDRO
───────────────────────────────────────────────────────────

GET    /api/personel-puantaj/{personelId}/{yil}/{ay}
       - Personel aylık puantajı

PUT    /api/personel-puantaj/{id}
       - Puantaj veya cesitli bilgisi

POST   /api/bordro/generate
       - Aylık bordro oluştur

GET    /api/bordro/{yil}/{ay}
       - Bordro listesi


EXCEL IMPORT/EXPORT
───────────────────────────────────────────────────────────

POST   /api/hakedis/import-excel
       - Excel hakediş import

GET    /api/hakedis/export-excel/{yil}/{ay}
       - Excel rapor indirme
```

---

## 8. Örnekler ve Say Senaryoları

### 8.1 Tam Senaryo: TRT-ANKARA Ocak 2025

#### 📋 Başlangıç Verileri

```
Müşteri: TRT-ANKARA (CariId: 42)
  ├─ Kurum 1: TRT Merkez (Araç: 34CX8, Şoför: Ali Demir)
  ├─ Kurum 2: TRT Çiftçiler (Araç: 06TX2, Şoför: Veli Kaya)
  └─ Kurum 3: TRT Dış Ticaret (Araç: 24RH4, Şoför: Mehmet Y.)

Ocak 2025: 31 gün (5 Çarşamba, 4 Pazartesi, vb.)
   ├─ İş Günü: 22 gün (Cuma-4, Cumartesi-4, Pazar-5)
   ├─ Resmi Tatil: 1 gün (Yeni Yıl = 1 Ocak = Çarşamba)
   ├─ Çalışma Günü Beklentisi: 21 gün/kişi
   └─ Aylık Toplam: 21 gün × 3 Şoför = 63 personel-gün

Sabit Tarifeler:
   ├─ TRT Merkez: 150 TL/gün (75 TL sabah, 75 TL akşam)
   ├─ TRT Çiftçiler: 120 TL/gün
   └─ TRT Dış Ticaret: 130 TL/gün

Tedarikçi Ücretleri:
   ├─ TRT Merkez: 80 TL/gün
   ├─ TRT Çiftçiler: 70 TL/gün
   └─ TRT Dış Ticaret: 75 TL/gün
```

#### 📊 Günlük Puantaj Kaydı İşlemi

```
Ocak 1, 2025 (Çarşamba) - Resmi Tatil
   └─ FiloGunlukPuantaj: YOK (Tüm araçlar)

Ocak 2, 2025 (Perşembe) - Normal Gün
   ├─ Araç 34CX8 (Ali Demir)
   │  ├─ Durum: Tamamlandı ✓
   │  ├─ Sabah: 06:30-08:00 / Akşam: 16:00-17:30
   │  └─ Kaydedilen Sefer: 2 (S+A)
   │
   ├─ Araç 06TX2 (Veli Kaya)
   │  ├─ Durum: Tamamlandı ✓
   │  └─ Kaydedilen Sefer: 2
   │
   └─ Araç 24RH4 (Mehmet Y.)
      ├─ Durum: Tamamlandı ✓
      └─ Kaydedilen Sefer: 2

   Günlük Toplam: 6 sefer

[... (Ocak 3-31 = benzer şekilde) ...]

Ocak 8, 2025 (Salı) - İzin Günü (Ali Demir'in İzni)
   ├─ Araç 34CX8: Durum = "IptalEdildi" (Ali yok)
   ├─ Araç 06TX2: Durum = "Tamamlandı" (2 sefer)
   └─ Araç 24RH4: Durum = "Tamamlandı" (2 sefer)

   Günlük Toplam: 4 sefer (1 araç eksik)

Ocak 15, 2025 (Salı) - Araç Arızası
   ├─ Araç 34CX8: Durum = "ArizaNedeniyleYapilamadi"
   ├─ Araç 06TX2: Durum = "Tamamlandı" (2 sefer)
   └─ Araç 24RH4: Durum = "Tamamlandı" (2 sefer)

   Günlük Toplam: 4 sefer

[... (Ocak 16-29 = Normal) ...]

Ocak 29, 2025 (Çarşamba) - Son İş Günü
   ├─ Araç 34CX8: Durum = "Tamamlandı" (2 sefer)
   ├─ Araç 06TX2: Durum = "Tamamlandı" (2 sefer)
   └─ Araç 24RH4: Durum = "Tamamlandı" (2 sefer)

   Günlük Toplam: 6 sefer
```

#### 🧮 Aylık Özet Hesaplama

```
Araç 34CX8 (Ali Demir) - TRT Merkez
   ├─ Toplam Çalışma Günü: 20 gün
   │  (22 iş günü - 1 izin - 1 arıza)
   ├─ Toplam Sefer: 20 × 2 = 40 sefer
   ├─ Gelir: 40 × 150 TL = 6.000 TL
   ├─ Gider: 40 × 80 TL = 3.200 TL
   ├─ KDV (%20): 3.200 × 0.20 = 640 TL
   ├─ Ödenecek: 3.200 + 640 = 3.840 TL
   └─ Kar: 6.000 - 3.840 = 2.160 TL

Araç 06TX2 (Veli Kaya) - TRT Çiftçiler
   ├─ Toplam Çalışma Günü: 21 gün (arıza yok)
   ├─ Toplam Sefer: 21 × 2 = 42 sefer
   ├─ Gelir: 42 × 120 TL = 5.040 TL
   ├─ Gider: 42 × 70 TL = 2.940 TL
   ├─ KDV (%20): 2.940 × 0.20 = 588 TL
   ├─ Ödenecek: 2.940 + 588 = 3.528 TL
   └─ Kar: 5.040 - 3.528 = 1.512 TL

Araç 24RH4 (Mehmet Y.) - TRT Dış Ticaret
   ├─ Toplam Çalışma Günü: 20 gün
   │  (22 iş günü - 2 mazeret)
   ├─ Toplam Sefer: 20 × 2 = 40 sefer
   ├─ Gelir: 40 × 130 TL = 5.200 TL
   ├─ Gider: 40 × 75 TL = 3.000 TL
   ├─ KDV (%20): 3.000 × 0.20 = 600 TL
   ├─ Ödenecek: 3.000 + 600 = 3.600 TL
   └─ Kar: 5.200 - 3.600 = 1.600 TL

═════════════════════════════════════════════════════════════
TRT-ANKARA TOPLAM (OCT 2025)
═════════════════════════════════════════════════════════════
Toplam Sefer: 40 + 42 + 40 = 122 sefer
Toplam Gelir (KDV hariç): 6.000 + 5.040 + 5.200 = 16.240 TL
Toplam Gelir (KDV dahil): (hesapla farklı KDV oranı varsa)
Toplam Gider (KDV dahil): 3.840 + 3.528 + 3.600 = 10.968 TL
NET KAR: 16.240 - 10.968 = 5.272 TL (KDV hariç gelirden)
```

#### 📋 Personel Puantajı (Şoför Maaş)

```
─────────────────────────────────────────────────────────
Ali Demir - Ocak 2025

Çalışma Bilgisi:
├─ Çalışılan Gün: 20 gün
├─ Izin Günü: 1 gün
├─ Arıza Günü: 1 gün
├─ Çalışılan Saat: 20 × 8 = 160 saat
└─ Fazla Mesai: 0 saat (Taşıma görevinde standard)

Gelir Bileşenleri:
├─ Brüt Maaş: 8.500 TL
├─ Yemek Ücreti: 20 × 37.50 = 750 TL
├─ Yol Ücreti: 200 TL
├─ Taşıma Bonusu: 500 TL (Serf başı)
└─ TOPLAM: 9.950 TL

Kesintiler:
├─ SGK (%14): 1.393 TL
├─ Gelir Vergisi: 1.493 TL
└─ Damga: 0 TL

NET ÖDEME: 9.950 - 1.393 - 1.493 = 7.064 TL
─────────────────────────────────────────────────────────

Veli Kaya - Ocak 2025

Çalışma Bilgisi:
├─ Çalışılan Gün: 21 gün (tüm ay)
├─ Izin Günü: 0 gün
├─ Arıza Günü: 0 gün
└─ Toplam Saat: 21 × 8 = 168 saat

Gelir:
├─ Brüt Maaş: 8.000 TL
├─ Yemek: 21 × 37.50 = 787.50 TL
├─ Yol: 200 TL
├─ Bonus: 650 TL
└─ TOPLAM: 9.637.50 TL

Kesintiler:
├─ SGK (%14): 1.349 TL
├─ Gelir V.: 1.446 TL
└─ Damga: 0 TL

NET ÖDEME: 9.637.50 - 1.349 - 1.446 = 6.842.50 TL
─────────────────────────────────────────────────────────

Mehmet Yıldız - Ocak 2025

Çalışma Bilgisi:
├─ Çalışılan Gün: 20 gün
├─ Izin/Mazeret: 2 gün rapor
├─ Çalışılan Saat: 160
└─ Fazla Mesai: 16 saat

Gelir:
├─ Brüt Maaş: 9.000 TL
├─ Yemek: 20 × 37.50 = 750 TL
├─ Yol: 200 TL
├─ Fazla Mesai (16@53): 848 TL
├─ Bonus: 500 TL
└─ TOPLAM: 11.298 TL

Kesintiler:
├─ SGK (%14): 1.581.72 TL
├─ Gelir V.: 1.695 TL
└─ Damga: 0 TL

NET ÖDEME: 11.298 - 1.581.72 - 1.695 = 8.021.28 TL
─────────────────────────────────────────────────────────

OCAK 2025 BORDRO TOPLAM:
├─ Ali Demir: 7.064 TL
├─ Veli Kaya: 6.842.50 TL
├─ Mehmet Y.: 8.021.28 TL
└─ TOTAL PERSONEL ÖDEMESİ: 21.927.78 TL
```

#### 💰 Aylık Genel Özet

```
╔═════════════════════════════════════════════════════════╗
║         OCAK 2025 - FİNANSAL RAPOR                     ║
╚═════════════════════════════════════════════════════════╝

A. KURUMA YAPILACAK FATURALANDIRMA (GELIR)
──────────────────────────────────────────────────
   TRT-ANKARA → Fatura
   ├─ Net Tutar: 16.240 TL
   ├─ KDV (% 20): 3.248 TL
   └─ Toplam: 19.488 TL

B. TEDARIKÇILERE ÖDENECEKLER (GIDER)
──────────────────────────────────────────────────
   Benim Şirketim → Taşeron/Tedarikçi
   ├─ Merkez (34CX8 sahibi): 3.840 TL
   ├─ Çiftçiler (06TX2 sahibi): 3.528 TL
   ├─ Dış T. (24RH4 sahibi): 3.600 TL
   └─ Toplam: 10.968 TL

C. PERSONEL ÖDEME
──────────────────────────────────────────────────
   Personel → Maaş
   ├─ Ali Demir: 7.064 TL
   ├─ Veli Kaya: 6.842.50 TL
   ├─ Mehmet Y.: 8.021.28 TL
   └─ Toplam: 21.927.78 TL

D. FINANSAL ANALIZ
──────────────────────────────────────────────────
   Brüt Gelir (KDV dahil): 19.488 TL
   ├─ Tedarikçi Gideri: 10.968 TL
   ├─ Personel Gideri: 21.927.78 TL
   ├─ Operasyon Gideri (~5%): 975 TL
   └─ Toplam Gideri: ~33.870.78 TL

   NET KAR/ZARAR: 19.488 - 33.870.78 = -14.382.78 TL !!!

   ⚠️ SORUN #1: Personel gideri çok yüksek!
      Şoför maaşı aylık 8.000-9.000 TL ama hizmet değeri
      sadece 3-4 bin TL. Gider fazlası modelde...

   ⚠️ SORUN #2: KDV farklılığı
      - Tedarikçi gideri için KDV geri alınamıyor

E. KONTROL PERSPEKTİFİ
──────────────────────────────────────────────────
   1. Kur için çalışan sayısını artırmak?
      → Aynı teşekkür'de 4-5 rota yerine sadece 3

   2. Maaş yapısını revize etmek?
      → Taşıma hizmetine özel "sefer temelli ücret"

   3. Fiyat tarifini artırmak?
      → TRT-ANKARA sabit fiyatı 150 TL yerine 200 TL+
```

---

## 9. Riskler ve Best Practices

### 9.1 Operasyonel Riskler

| Risk | Etki | Çözüm |
|------|------|-------|
| **Eksik Puantaj** | Şoför maaş hesaplanamıyor, Fatura tutarı yanlış | Günlük SMS/QR-code doğrulaması |
| **Çift Kayıt** | Aynı sefer 2x faturalanıyor | Unique constraint + anomali detection |
| **Arıza/Gecikme** | Müşteri memnuniyeti ↓ | Alternatif araç sistemi, bonus/ceza mekanizması |
| **Şoför Devamsızlığı** | Sefer iptal veya başka şoför bulma | İzin planlama, backup şoför tabanı |
| **KDV Hataları** | Vergi cezası | Excel şablonu standart dosya + oto-hesaplama |
| **Ödeme Gecikmesi** | Tedarikçi memnuniyeti ↓ | Oto-bordro, sabit ödeme günü |

### 9.2 Muhasebe Riskler

| Risk | Etki | Çözüm |
|------|------|-------|
| **Yanlış KDV Kategori** | Şeffaf vergi stopajı | KDV oranı = Tarif tablosunda |
| **Stopaj Hesap Hatası** | Vergi müfettişliği uyası | Otomattik stopaj formülü |
| **Faktura Numarasız Ödeme** | Gümrük Muhäsebesi reddi | E-fatura sistemi mandatory |
| **Personel Kesintisi Yanlış** | Net maaş tutarsızlığı | SGK/Vergi tablolarının yıllık güncellenmesi |
| **Hazırlık Gecikmesi** | Bordrо vs. Hakedis sinkronizasyon sorunu | Ay 28'nde otofinaliz rutin |

### 9.3 Best Practices

#### 🎯 Operasyon Tarafı

1. **Sabit Rota Tarifesi**
   - Her rota için tarife belgelendirme
   - Müşteri ile yazılı sözleşme
   - Yıl başında revizyon

2. **Günlük Kayıt Sistemi**
   - Şoför mobile app (QR-code check-in)
   - GPS tracking (konum doğrulama)
   - Fotoğraf/video proof (varsa anomali)

3. **Haftası Kontrol**
   - Pazartesi: Önceki hafta puantajı finalize
   - Perşembe: Sonraki haftanın planı
   - Cuma: Aylık özet preview

4. **Arıza Protokolü**
   - Şoför raporu 2 saat içinde
   - Alternatif araç veya rota değişikliği
   - Sistem notasyon + fotoğraf

#### 📊 Muhasebe Tarafı

1. **Ay Sonu Checklist (26. Güne kadar)**
   - [ ] Tüm FiloGunlukPuantaj kaydedildi
   - [ ] Anomali raporu incelenmldi
   - [ ] HakedisPuantaj oluşturuldu
   - [ ] KDV oranları doğru
   - [ ] Hakedis onaylayıcı imzaladı

2. **Fatura Standardı**
   - E-Fatura sistemi (UBL formatı)
   - Tarih: Ayın son günü
   - Vade: Müşteri kontratına göre (30-45-60 gün)
   - Açı Kalemi: Tafsil (Rota×Sefer×Fiyat)

3. **KDV Stopaj Tablosu** (örnek)
   ```
   Tedarikçi Tipi    | KDV Oran | Stopaj Oranı
   ─────────────────────────────────────
   Gerçek Kişi       | %20      | %3 (Stopaj)
   Şirket (Kayıtlı) | %20      | %0 (Fatura)
   Gerçek Kişi (KDV)| %20      | %0 ( Fatura)
   ```

4. **Bordro Yapısı**
   - Taşıma Bonusu = Sefer Say × Bonus/Sefer
   - Maaş = Base + Yemek + Yol + Bonus - Kesinti
   - Düzeltme: +İzin veya -Arıza (ayda maksimum 2-3)

#### 🔒 İç Kontrol

1. **Puantaj Doğrulama**
   - Gün başında veya sonunda SMS/Email
   - Şoför onayı: "Bugün İzin" veya "Çalıştım"
   - Sistem: Oto-kaydı veya manuel
   - Haftalık: Manager kontrol

2. **VERGİ Denetçiliği**
   - KDV Beyannamesi = HakedisPuantaj toplama
   - Gelir Vergisi = PersonelPuantaj üzerine stopaj
   - Muhasebe Müdürü imzası: Tüm belge

3. **Bağımsız Denetim**
   - Aylik finans raporunun muhasebeci tarafından onaylanması
   - KDV hesaplama audit
   - Aylık kar analizi

---

## Kaynakça &

Sistem Çerçevesi: MKFiloServis (Multi-Tenant Operasyonel + Hakediş + Puantaj)

- **Entity Reference**: FiloGunlukPuantaj, HakedisPuantaj, Hakedis, PersonelPuantaj
- **Servis**: HakedisService, FiloKomisyonService, PuantajHakedisSyncService, OperasyonelHakedisService
- **UI**: EslestirmeTanimlari.razor, OperasyonelHakedisPage.razor, HakedisOzeti.razor

---

**Versiyon**: 1.0  
**Son Güncelleme**: 2025-01-23  
**Hazırlayan**: Sistem Mimarı (MKFiloServis Proje Ekibi)  
**Onaylayan**: [İmza Alanı] - İnsan Kaynakları Müdürü / Muhasebe Müdürü

---
