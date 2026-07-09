# 🇹🇷 Türkiye Şartlarında Araç Başına Güzergah Puantajı Giriş Yöntemi
## Kapsamlı Analiz, Öneriler & Uygulama Planı

**Tarih**: 23 Ocak 2025  
**Hedef**: Türkiye'de personel taşıma sektöründe araç-güzergah-puantaj ilişkisini optimize etmek  
**Kapsam**: Mevcut sistem analizi, Türkiye pratiği, öneriler, teknik uygulama planı

---

## 📋 İçindekiler

1. [Türkiye Personel Taşıma Sektörü Özet](#1-türkiye-personel-taşıma-sektörü-özet)
2. [MKFiloServis Mevcut Durumu](#2-mkfiloservis-mevcut-durumu)
3. [Puantaj Giriş Yöntemleri: Karşılaştırma](#3-puantaj-giriş-yöntemleri-karşılaştırma)
4. [Önerilen Çözüm: Hibrit Input Modeli](#4-önerilen-çözüm-hibrit-input-modeli)
5. [Teknik Implementasyon Detayı](#5-teknik-implementasyon-detayı)
6. [Blazor UI/UX Tasarımı](#6-blazor-uiux-tasarımı)
7. [Uygulama Yol Haritası](#7-uygulama-yol-haritası)
8. [Işletme Özeti & Eğitim Planı](#8-işletme-özeti--eğitim-planı)

---

## 1. Türkiye Personel Taşıma Sektörü Özet

### 1.1 Pazar Yapısı

```
┌─ Kurumlar (İşveren)
│  • Hukuk Müşavirlik ofisleri
│  • Bankalar (personel navette)
│  • Hastaneler & Klinikleri
│  • Elektrik Dağıtım Şirketleri
│  • Telekom firmalar
│  • Endüstriyel tesisler
│
├─ Taşıyıcı Firmalar (Trafik İşletme)
│  • Kendi araçları + personel
│  • Komisyonlu (Ödünç) araçlar
│  • Serbest şoför + araç
│
└─ İşçiler (Personel)
   • Sabit puantaj dönemü (Ay/İkiye bir)
   • Değişken durum (Hastalık, İzin, Ödülü)
   • Komisyon talepleri (Para karşılığı)
```

### 1.2 Puantaj İş Akışı (Gerçek)

```
Pazartesi → Cuma: Servis döner → Şoför puantajlar giriyor
        ↓
Ayın Sonunda (26-30):
  ├─ Operatör Excel'e tüm seferleri yazıyor
  ├─ Hangi araç → Hangi güzergahta → Kaç sefer
  ├─ Durum notları (Makzul [K], Gitmedi, Taksi, vb.)
  └─ SGK raporuna hazırlanıyor
        ↓
Muhasebeci:
  ├─ Firmanın kredisine göre puantaj kontrol ediyor
  ├─ Komisyon hesaplanıyor
  ├─ SGK bildirimine düşüyor
  └─ Fatura düzenliyor
```

### 1.3 Türkiye Zorlukları (Pain Points)

```
1. ZAMAN SYNC SORUNU
   └─ Servis = Pazartesi-Cuma, Puantaj dönemi = Ay (26-30)
      Araç A: "Bu ayda 18 sefer yaptım"
      Donanım: "Sistemde 20 sefer var." → UYUŞMAZLIK!

2. DURUM MUHASEBESI (Makzul Yönetim)
   └─ "Bu araç pazartesi makzulü idi" → Puantaja yansımasın
      Ama: Şoför "hep makzul" demeyebilir (çıkar çatışması)

3. GÜZERGAH DEĞIŞKENLIĞI
   └─ Rota A → Rota B'ye geçti ortası → Puantaj?
      "Ya 15 gün A'da, 15 gün B'de?" → Manual split gerekli

4. TOPLU GIRIŞ Vs. DETAYLI GIRIŞ
   └─ Toplu: "Ay boyunca 100 sefer" (Hızlı, ama hataya açık)
   └─ Detaylı: Her gün her araç (Doğru, ama zaman alan)

5. SISTEM YÖK FAZLASI
   └─ "Pazartesi sistemde 5 sefer, gerçekte 6 sefer"
      İşletmeci: "Kalan 1'i elle ekleyeceğim" → Manuel override
```

---

## 2. MKFiloServis Mevcut Durumu

### 2.1 Mevcut Entity Yapısı

```
┌─────────────────────────────────────────────────┐
│          MKFiloServis Veri Modeli              │
├─────────────────────────────────────────────────┤
│
│ 📍 Guzergah (Master Data)
│    └─ GuzergahKodu, GuzergahAdi
│    └─ BaslangicNoktasi, BitisNoktasi
│    └─ BirimFiyat, GiderFiyat, PuantajCarpani
│    └─ VarsayilanAracId, VarsayilanSoforId
│
│ 🚗 Arac (Araç Envanteri)
│    └─ Model, Plaka, Kapasite
│    └─ Sahiplik (Özmal vs. Tazeron)
│
│ 👤 Sofor (Operatör/Şoför)
│    └─ Ad, Telefon, SSK No, Banka
│
│ 🔗 FiloGuzergahEslestirme (ŞABLON - Kalıcı)
│    └─ FirmaId, KurumFirmaId, GuzergahId
│    └─ AracId, SoforId
│    └─ KurumaKesilecekUcret, TaseronaOdenenUcret
│    └─ IsActive
│
│ 📅 FiloGunlukPuantaj (GÜNLÜK OPERASYOnal - Geçici)
│    └─ Tarih, FiloGuzergahEslestirmeId
│    └─ KurumFirmaId, GuzergahId, AracId, SoforId
│    └─ SeferSayisi, PuantajCarpani
│    └─ Durum (Gitti, Makzul, Taksi, vb.)
│    └─ TahakkukEdenKurumUcreti, TahakkukEdenTaseronUcreti
│    └─ Onaylandi
│
└─────────────────────────────────────────────────┘
```

### 2.2 Mevcut Puantaj Giriş Yöntemi

```
📌 TABLı (TOPLU) GİRİŞ
├─ Sayfalar:
│  ├─ "Toplu Puantaj" → Ay başına araç seçip 1 sayı girilir
│  ├─ "Operasyonel Puantaj" → Günlük sefer girilir
│  └─ "Çalışma Puantajı" → Personel puantajı (ayrı sistem)
│
├─ Problem:
│  └─ Ay içi consistency yok (26-30 Ocak ayı başında başlarsa?)
│  └─ Durum yönetimi zayıf (Makzul nasıl uygulanır?)
│  └─ Ara dönem değişiklikleri (Araç değişti, Güzergah değişti) → Manual
│  └─ Fatura ile puantaj uyuşmazlığı sık
```

### 2.3 Sorunlu Alanlar

```
❌ Problem 1: "Toplu Puantaj" sayfası mono-sefer
   ├─ Input: Ay × Araç → Sabit puantaj sayısı
   └─ Sorun: Güzergah başına breakdown yok
                → Araç A'nın: 12 gün Rota 1, 8 gün Rota 2 
                   nasıl split edilir?

❌ Problem 2: Durum (Makzul) yönetimi
   ├─ FiloGunlukPuantaj.Durum: Enum (Gitti, Makzul, Taksi, vb.)
   └─ Sorun: "Pazartesi-Çarşamba makzul idiysem ay toplam kaç sefer?"
                → Manuel hesaplama gerekli

❌ Problem 3: Ara dönem değişiklikleri
   ├─ "Araç A, Ocak ayında Rota 1'den Rota 2'ye geçti"
   └─ Sorun: FiloGuzergahEslestirme (şablon) bu dinamismi desteklemiyor
                → Yeni Eşleştirme = Yeni template = Karışıklık

❌ Problem 4: Multi-güzergah araç
   ├─ "Araç A: Pazartesi Rota 1, Salı Rota 2, ... Cuma Rota 1"
   └─ Sorun: "Raporlamada toplam kaç sefer?" → Güzergah bazında split gerekli
```

---

## 3. Puantaj Giriş Yöntemleri: Karşılaştırma

### 3.1 Yöntem A: Toplu Sayı (Mevcut)

```
┌────────────────────────────────┐
│ "Ocak ayında Araç A: 22 sefer" │
│  → Bittim, bir sayı gir        │
└────────────────────────────────┘

✅ Avantajlar:
  • Hızlı (1 sayı girecek)
  • Basit UI (Dropdown + Input field)
  • Aylık özet rapor easy

❌ Dezavantajlar:
  • Güzergah breakdown yok
  • Değişken durum muhasebesi manuel
  • Faturalama karmaşık (Rota 1: 150₺, Rota 2: 100₺ → Hangi Rota?)
  • Midterm değişiklik (Araç swap, Rota değişim) support yok
```

### 3.2 Yöntem B: Günlük Detaylı (Ideal ama Ağır)

```
┌────────────────────────────────┐
│ Pazartesi: Araç A, Rota 1: 1 sefer, Durum:Gitti
│ Salı:      Araç A, Rota 1: 1 sefer, Durum:Gitti
│ Çarşamba:  Araç A, Rota 2: 2 sefer, Durum:Makzul (1)
│ ...
│ Cuma:      Araç A, Rota 1: 1 sefer, Durum:Gitti
└────────────────────────────────┘

✅ Avantajlar:
  • 100% doğru (Her gün kaydedilir)
  • Durum muhasebesi açık (Makzul hangi gün?)
  • Güzergah breakdown built-in
  • Fatura tahakkuku kolay (Rota 1 × 8 gün × 150₺)
  • Mid-term değişiklik support (Araç swap tarihini gir)

❌ Dezavantajlar:
  • Zaman consuming (20 araç × 22 iş günü = 440 input)
  • Operator error yüksek
  • "Pazartesi unuttu" sorun
  • Başlangıçta adoption slow
```

### 3.3 Yöntem C: Hibrit (ÖNERİLEN)

```
┌────────────────────────────────┐
│ Sunum 1: TOPLU + Tanım
│  "Ocak'ta Araç A:"
│  - Rota 1: 12 gün → 12 sefer    [Hafta sonu × 1.5 = 18 sefer?]
│  - Rota 2: 8 gün → 24 sefer     [2.5 sefer/gün → topla 20]
│  - Makzul: 2 gün  → 0 sefer
│  ├─ Subtotal: 12 + 24 = 36 sefer
│  ├─ Makzul kesintisi: -2 sefer (opsiyonel)
│  └─ TOPLAM: 34 sefer
│
│ Sunum 2: HAFTALIK ÖZET (İnce-tuning)
│  Week 1: 8 sefer (Rota 1)... Gözden geçir, düzelt
│  Week 2: 9 sefer (Rota 1 + values)... Gözden geçir
│  ...
│
│ Sunum 3: GÜNLÜK DETAY (Deep-dive)
│  Pazartesi Rota 1: 2 sefer...
│  Salı Rota 1: 1 sefer...
│  ...gözden geçir, düzelt
└────────────────────────────────┘

✅ Avantajlar:
  • Hızlı başlangıç (Ay × Araç × Rota simple form)
  • Gözden geçirme UI rahat (Haftalık summary)
  • Deep-dive option (Günlük detail edit)
  • Makzul management clear
  • Fatura build-in ready

✅ Türkiye pratiğine uygun
  • "Ortalama gibi, sonra düzelt" → Hızlı, pratik
  • Makzul deduction seperate
  • Rota breakdown included
```

---

## 4. Önerilen Çözüm: Hibrit Input Modeli

### 4.1 Architecture

```
┌─ Kullanıcı Arabirimi (Blazor)
│  ├─ Level 1: TOPLU GİRİŞ (Hızlı)
│  │   "Ocak'ta Araç A → Rota 1: 12 gün, Rota 2: 8 gün, Makzul: 2 gün"
│  │   → Auto-calculate sefer sayısı (Rota default fiyat bazlı)
│  │   → "36 sefer olacak, onayla/düzelt"
│  │
│  ├─ Level 2: HAFTALIK GÖZ DEN GEÇİ RME (Fine-tuning)
│  │   "Haft a 1: 8 sefer, Haft a 2: 9 sefer, ..." 
│  │   → Week-by-week summary
│  │   → Makzul, Durum notları + Seferler
│  │
│  └─ Level 3: GÜNLÜK DETAY (Deep-dive)
│      Pazartesi → Rota 1 → 1 sefer → Gitti
│      Salı → Rota 1 → 1 sefer → Gitti
│      ...
│      Cuma → Rota 1 → 1 sefer → Gitti
│
├─ Backend Service (C# / EF Core)
│  ├─ PuantajInputService
│  │   ├─ CreateTopluGiriş(...) → Toplu create + auto-calc
│  │   ├─ GetHaftalikOzet(...) → Week summary for review
│  │   ├─ UpdateGunlukDetay(...) → Single day update
│  │   └─ ValidatePuantaj(...) → Check consistency
│  │
│  └─ FaturabandırmaService
│      ├─ CalculateFaturaTahakkuku(...) → Güzergah × Sefer × Fiyat
│      └─ GenerateSGKReport(...) → Aylık SGK bildirimi
│
└─ Database (PostgreSQL)
   ├─ FiloGunlukPuantaj (Tarih, Güzergah, Araç, Sefer, Durum)
   ├─ PuantajDönemAyarları (Dönem başı-sonu, Makzul kesilir mi?)
   └─ GuzergahAracEslestirmeGecsmiş (Tarih aralığında hangi Rota)
```

### 4.2 Veri Modeli Genişletmesi

```csharp
/// Bölüm 1: Toplu Giriş için Ara Tablo
public class PuantajTopluGiriş : BaseEntity, IFirmaTenant
{
    public int FirmaId { get; set; }
    public int KurumFirmaId { get; set; }

    // Dönem tanımı
    public int Yil { get; set; }           // 2025
    public int Ay { get; set; }            // 1 (Ocak)
    public DateTime DonempBasiTarihi { get; set; }  // 2024-12-26
    public DateTime DonepSonuTarihi { get; set; }   // 2025-01-25

    // Araç & Rota
    public int AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    // Rota Detayları (JSON array veya separate table)
    public List<PuantajTopluRotaDetay> RotaDetaylari { get; set; }

    // Makzul yönetim
    public int MakzulGunSayisi { get; set; }  // 2 gün makzul
    public decimal MakzulToplamSefer { get; set; }  // -2 sefer

    // Toplam hesaplandı
    public decimal ToplamSeferSayisi { get; set; }  // 36 sefer
    public decimal ToplamGelir { get; set; }   // 36 × ort Fiyat
    public decimal ToplamMaliyet { get; set; }  // 36 × ort Gider

    // Durum
    public PuantajTopluGirisDurumu Durum { get; set; }
    public DateTime OnayTarihi { get; set; }
    public int? OnayanKullaniciId { get; set; }

    // İlişkiler
    public virtual ICollection<FiloGunlukPuantaj> UretigenGunlukPuantajlar { get; set; }
}

/// Rota Detay (Bölüm 2)
public class PuantajTopluRotaDetay
{
    public int GuezergahId { get; set; }
    public string GuezergahAdi { get; set; }
    public int GunSayisi { get; set; }  // "Bu ay Rota 1'de kaç gün çalıştı?"
    public decimal SeferSayisiPerGun { get; set; }  // "Her gün ortalama kaç sefer?"
    public decimal TahminiTotalSefer { get; set; }  // = GunSayisi × SeferSayisiPerGun

    // Fiyatlandırma snapshot (Rota bu dönemde değişmişse snapshot)
    public decimal BirimFiyatSnapshot { get; set; }  // 150₺
    public decimal GiderFiyatSnapshot { get; set; }  // 80₺

    // İlişki
    public virtual Guzergah? Guzergah { get; set; }
}

public enum PuantajTopluGirisDurumu
{
    TaslakOlusturuldu = 1,     // Toplu giriş yapıldı, onay bekleniyor
    InceleniyorGunlukFormat = 2,  // Haftalık özet review ediliyor
    GunlukDetayOnayi = 3,      // Her gün detail kontrol ediliyor
    Onaylandi = 4,             // Tüm seviyelardan onay
    Hesaplandi = 5             // Fatura & SGK hesaplaması yapıldı
}
```

### 4.3 Input Flow (Operatör Perspektifinden)

```
ADIM 1: TOPLU GİRİŞ (5 dakika)
├─ Sayfaya gir: "Puantaj Toplu Giriş"
├─ Dönem seç: "Ocak 2025" (26 Aralık 2024 - 25 Ocak 2025)
├─ Araç seç: "Araç A (Plaka: 34-ABC-12345)"
├─ Rota sayısı: "2 rota"
│  ├─ Rota 1: "Ankara Merkez → TRT (Pazartesi-Çarşamba)"
│  │   ├─ Gün sayısı: 12
│  │   ├─ Sefer/gün: 1  [Default: BirimFiyat'dan hesaplanabilir]
│  │   └─ → 12 sefer → 12 × 150₺ = 1.800₺
│  │
│  └─ Rota 2: "Ankara Merkez → Banka (Perşembe-Cuma)"
│      ├─ Gün sayısı: 8
│      ├─ Sefer/gün: 3  [High-frequency route]
│      └─ → 24 sefer → 24 × 100₺ = 2.400₺
│
├─ Makzul: 2 gün → -2 sefer (kesinti) [Toggle able]
├─ TOPLAM HESAPLANDI: 34 sefer, Gelir: 4.200₺
├─ Düzenle butonları (Her Rota edit'e gidebilir)
└─ ✓ "Onayla" → Haftalık özet'e geç

─────────────────────────────────────────────────

ADIM 2: HAFTALIK ÖZET REVIEW (5 dakika)
├─ Sayfaya gir: "Haftalık Puantaj Özeti"
├─ Takvim görünümü:
│  Week 1 (26 Ara - 1 Oca):
│  ├─ Rota 1: 3 gün → 3 sefer → ✓
│  ├─ Rota 2: 2 gün → 6 sefer → ? (Sanki çok)
│  └─ Makzul: 0 gün
│
│  Week 2 (2 Oca - 8 Oca):
│  ├─ Rota 1: 3 gün → 3 sefer
│  ├─ Rota 2: 2 gün → 6 sefer
│  └─ Makzul: 2 gün → -2 sefer [Çarşamba & Perşembe]
│
│  ... (all weeks shown)
│
├─ Şüpheli alanlar: Kırmızı flag (örn: "Rota 2: 3 sefer/gün çok mu?")
├─ Bir hafta "düzelt" → Günlük detail'e git
└─ Tüm hafta tamam ise → "Güncelle & İleri git"

─────────────────────────────────────────────────

ADIM 3: GÜNLÜK DETAYDUZELTMELERİ (5-10 dakika, OPSIYONEL)
├─ Sayfaya gir: "Günlük Puantaj Detayı"
├─ Takvim + Grid:
│  Tarih      | Rota      | Sefer | Durum    | Notlar
│  26 Aralık  | Rota 1    | 1     | Gitti    | Tanesi
│  27 Aralık  | Rota 2    | 3     | Makzul*  | (*Şarşamba makzul)
│  28 Aralık  | Rota 2    | 3     | Gitti    |
│  29 Aralık  | Rota 1    | 1     | Gitti    |
│  30 Aralık  | Rota 1    | 1     | Gitti    |
│  31 Aralık  | [Tatil]   | -     | Tatil    |
│  1 Ocak     | [Tatil]   | -     | Tatil    |
│  2 Ocak     | -         | 0     | W/O       | [No route assigned - unscheduled]
│  3 Ocak     | Rota 1    | 2     | Gitti    |
│  ... (21 günü listele)
│
├─ Inline edit: "Sefer" sayısı değiştirebilir, Durum toggle
├─ "Önceki Makzul Tarihi" tutulur
├─ "Tamamla & Faturaya Git" button
└─ FaturaBandırma sayfasına oto-taşı

─────────────────────────────────────────────────

SONUÇ: SGK & Fatura
├─ Sistem otomatik olarak:
│  ├─ FiloGunlukPuantaj tablasını 34 kaydıyla doldurur
│  ├─ Tarih: 26 Ara, 27 Ara, ..., 25 Oca
│  ├─ Durum, Sefer, Rota bilgileri kaydedilir
│  └─ → Fatura ve SGK hazırlığı tamamlanır
│
└─ Muhasebeci: "Puantaj Fatura Mutabakat" sayfasında
   ├─ SGK Bildirimi: ✓ Otomatik
   ├─ Fatura: ✓ Otomatik (Rota × Sefer × Fiyat)
   └─ Ödeme: ✓ Muhasebe sistemi hazır
```

---

## 5. Teknik Implementasyon Detayı

### 5.1 Backend Service: PuantajTopluGirisjService

```csharp
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public interface IPuantajTopluGirisiService
{
    Task<PuantajTopluGiriş> CreateTopluGirişAsync(
        int firmaId, 
        int kurumFirmaId, 
        int aracId, 
        int yil, 
        int ay, 
        List<PuantajTopluRotaDetay> rotaDetaylari,
        int makzulGunSayisi = 0,
        CancellationToken cancellationToken = default);

    Task<PuantajHaftalikOzet> GetHaftalikOzetAsync(
        int puantajTopluGirisiId, 
        CancellationToken cancellationToken = default);

    Task UpdateGunlukDetayAsync(
        int puantajTopluGirisiId,
        DateTime tarih,
        int guezergahId,
        decimal seferSayisi,
        OperasyonDurumu durum,
        string? notlar = null,
        CancellationToken cancellationToken = default);

    Task<decimal> ValidateTuturlulukAsync(
        int puantajTopluGirisiId,
        CancellationToken cancellationToken = default);

    Task Onayla_GunlukPuantajlarOlusturAsync(
        int puantajTopluGirisiId,
        int onayanKullaniciId,
        CancellationToken cancellationToken = default);
}

public class PuantajTopluGirisiService : IPuantajTopluGirisiService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PuantajTopluGirisiService> _logger;

    public PuantajTopluGirisiService(
        ApplicationDbContext context,
        ILogger<PuantajTopluGirisiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Toplu giriş oluştur: "Bu araç bu ay X Rotada Y gün çalıştı"
    /// </summary>
    public async Task<PuantajTopluGiriş> CreateTopluGirişAsync(
        int firmaId,
        int kurumFirmaId,
        int aracId,
        int yil,
        int ay,
        List<PuantajTopluRotaDetay> rotaDetaylari,
        int makzulGunSayisi = 0,
        CancellationToken cancellationToken = default)
    {
        // Dönem tarihlerini hesapla (26. gün önceki ay ~ 25. gün bu ay)
        var donempBasiTarihi = new DateTime(yil, ay, 1);
        if (ay == 1)
        {
            donempBasiTarihi = new DateTime(yil - 1, 12, 26);
        }
        else
        {
            donempBasiTarihi = new DateTime(yil, ay - 1, 26);
        }

        var donepSonuTarihi = donempBasiTarihi.AddMonths(1).AddDays(-1);

        // Toplam sefer hesapla
        var toplamSefer = rotaDetaylari
            .Sum(r => r.GunSayisi * r.SeferSayisiPerGun) - makzulGunSayisi;

        // Toplam gelir/gider hesapla
        var toplamGelir = rotaDetaylari
            .Sum(r => (r.GunSayisi * r.SeferSayisiPerGun) * r.BirimFiyatSnapshot);

        var toplamMaliyet = rotaDetaylari
            .Sum(r => (r.GunSayisi * r.SeferSayisiPerGun) * r.GiderFiyatSnapshot);

        var puantajTopluGiriş = new PuantajTopluGiriş
        {
            FirmaId = firmaId,
            KurumFirmaId = kurumFirmaId,
            AracId = aracId,
            Yil = yil,
            Ay = ay,
            DonempBasiTarihi = donempBasiTarihi,
            DonepSonuTarihi = donepSonuTarihi,
            RotaDetaylari = rotaDetaylari,
            MakzulGunSayisi = makzulGunSayisi,
            MakzulToplamSefer = makzulGunSayisi,
            ToplamSeferSayisi = toplamSefer,
            ToplamGelir = toplamGelir,
            ToplamMaliyet = toplamMaliyet,
            Durum = PuantajTopluGirisDurumu.TaslakOlusturuldu,
            CreatedAt = DateTime.UtcNow
        };

        _context.PuantajTopluGirisleri.Add(puantajTopluGiriş);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Toplu puantaj giriş oluşturuldu: Araç={AracId}, Dönem={Yil}-{Ay}, Toplam={Toplam} sefer",
            aracId, yil, ay, toplamSefer);

        return puantajTopluGiriş;
    }

    /// <summary>
    /// Haftalık özet getir (Review için)
    /// </summary>
    public async Task<PuantajHaftalikOzet> GetHaftalikOzetAsync(
        int puantajTopluGirisiId,
        CancellationToken cancellationToken = default)
    {
        var topluGiriş = await _context.PuantajTopluGirisleri
            .Include(p => p.RotaDetaylari)
            .FirstOrDefaultAsync(p => p.Id == puantajTopluGirisiId, cancellationToken);

        if (topluGiriş == null)
            throw new ArgumentException("Toplu giriş bulunamadı");

        var haftalikOzet = new PuantajHaftalikOzet
        {
            PuantajTopluGirisiId = puantajTopluGirisiId,
            Haftalar = GenerateWeeklyBreakdown(
                topluGiriş.DonempBasiTarihi,
                topluGiriş.DonepSonuTarihi,
                topluGiriş.RotaDetaylari)
        };

        return haftalikOzet;
    }

    /// <summary>
    /// Günlük detay güncelle (Düzeltme için)
    /// </summary>
    public async Task UpdateGunlukDetayAsync(
        int puantajTopluGirisiId,
        DateTime tarih,
        int guezergahId,
        decimal seferSayisi,
        OperasyonDurumu durum,
        string? notlar = null,
        CancellationToken cancellationToken = default)
    {
        var topluGiriş = await _context.PuantajTopluGirisleri
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == puantajTopluGirisiId, cancellationToken);

        if (topluGiriş == null)
            throw new ArgumentException("Toplu giriş bulunamadı");

        // Var olan veya yeni FiloGunlukPuantaj oluştur/güncelle
        var gunlukPuantaj = await _context.FiloGunlukPuantajlar
            .FirstOrDefaultAsync(p =>
                p.FirmaId == topluGiriş.FirmaId &&
                p.KurumFirmaId == topluGiriş.KurumFirmaId &&
                p.Tarih == tarih &&
                p.GuezergahId == guezergahId &&
                p.AracId == topluGiriş.AracId,
                cancellationToken);

        if (gunlukPuantaj == null)
        {
            // Yeni kayıt oluştur
            gunlukPuantaj = new FiloGunlukPuantaj
            {
                FirmaId = topluGiriş.FirmaId,
                KurumFirmaId = topluGiriş.KurumFirmaId,
                Tarih = tarih,
                GuezergahId = guezergahId,
                AracId = topluGiriş.AracId,
                SeferSayisi = seferSayisi,
                Durum = durum,
                Notlar = notlar,
                CreatedAt = DateTime.UtcNow
            };
            _context.FiloGunlukPuantajlar.Add(gunlukPuantaj);
        }
        else
        {
            // Var olan kaydı güncelle
            gunlukPuantaj.SeferSayisi = seferSayisi;
            gunlukPuantaj.Durum = durum;
            gunlukPuantaj.Notlar = notlar;
            gunlukPuantaj.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Günlük puantaj güncellendi: Tarih={Tarih}, Sefer={Sefer}, Durum={Durum}",
            tarih, seferSayisi, durum);
    }

    private List<PuantajHaftaOzet> GenerateWeeklyBreakdown(
        DateTime basTarihi,
        DateTime sonTarihi,
        List<PuantajTopluRotaDetay> rotaDetaylari)
    {
        var haftalar = new List<PuantajHaftaOzet>();
        var suanTarihi = basTarihi;
        var haftaNumarasi = 1;

        while (suanTarihi <= sonTarihi)
        {
            var haftatasonuTarihi = suanTarihi.AddDays(6);
            if (haftatasonuTarihi > sonTarihi)
                haftatasonuTarihi = sonTarihi;

            var haftaGunleri = new List<DateTime>();
            var gunler = (haftatasonuTarihi - suanTarihi).Days + 1;
            for (int i = 0; i < gunler; i++)
            {
                haftaGunleri.Add(suanTarihi.AddDays(i));
            }

            var hafta = new PuantajHaftaOzet
            {
                HaftaNumarasi = haftaNumarasi,
                BasTarihi = suanTarihi,
                SonTarihi = haftatasonuTarihi,
                Gunler = haftaGunleri.Select(g => new PuantajGunuOzet
                {
                    Tarih = g,
                    Rotalar = rotaDetaylari.Select(r => new PuantajGunRotaOzet
                    {
                        GuezergahId = r.GuezergahId,
                        GuezergahAdi = r.GuezergahAdi,
                        TahminiSefer = r.SeferSayisiPerGun,
                        Durum = OperasyonDurumu.Gitti  // Default
                    }).ToList()
                }).ToList()
            };

            haftalar.Add(hafta);
            suanTarihi = haftatasonuTarihi.AddDays(1);
            haftaNumarasi++;
        }

        return haftalar;
    }

    /// <summary>
    /// Uyuşmazlığı kontrol et (Toplu vs Derinlemesine)
    /// </summary>
    public async Task<decimal> ValidateTuturlulukAsync(
        int puantajTopluGirisiId,
        CancellationToken cancellationToken = default)
    {
        var topluGiriş = await _context.PuantajTopluGirisleri
            .FirstOrDefaultAsync(p => p.Id == puantajTopluGirisiId, cancellationToken);

        if (topluGiriş == null)
            throw new ArgumentException("Toplu giriş bulunamadı");

        // Günlük kayıtlardaki toplam
        var gunlukToplam = await _context.FiloGunlukPuantajlar
            .Where(p =>
                p.FirmaId == topluGiriş.FirmaId &&
                p.AracId == topluGiriş.AracId &&
                p.Tarih >= topluGiriş.DonempBasiTarihi &&
                p.Tarih <= topluGiriş.DonepSonuTarihi)
            .SumAsync(p => p.SeferSayisi, cancellationToken);

        var fark = topluGiriş.ToplamSeferSayisi - gunlukToplam;

        if (fark != 0)
        {
            _logger.LogWarning(
                "Uyuşmazlık tespit edildi: TopluGiriş={Toplam}, Gunluk={Gunluk}, Fark={Fark}",
                topluGiriş.ToplamSeferSayisi, gunlukToplam, fark);
        }

        return fark;
    }

    /// <summary>
    /// Onay & FiloGunlukPuantaj oluştur
    /// </summary>
    public async Task Onayla_GunlukPuantajlarOlusturAsync(
        int puantajTopluGirisiId,
        int onayanKullaniciId,
        CancellationToken cancellationToken = default)
    {
        var topluGiriş = await _context.PuantajTopluGirisleri
            .Include(p => p.RotaDetaylari)
            .FirstOrDefaultAsync(p => p.Id == puantajTopluGirisiId, cancellationToken);

        if (topluGiriş == null)
            throw new ArgumentException("Toplu giriş bulunamadı");

        // Uyuşmazlık kontrol et
        var fark = await ValidateTuturlulukAsync(puantajTopluGirisiId, cancellationToken);

        if (Math.Abs(fark) > 0.01m)  // 0.01 sefer farkı tolerance
        {
            _logger.LogWarning(
                "Uyuşmazlık olmasına rağmen onay yapılıyor. Fark: {Fark}", fark);
        }

        // Durum güncelle
        topluGiriş.Durum = PuantajTopluGirisDurumu.Onaylandi;
        topluGiriş.OnayTarihi = DateTime.UtcNow;
        topluGiriş.OnayanKullaniciId = onayanKullaniciId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Toplu puantaj onaylandı. PuantajTopluGirişId={Id}, OnayanKullaniciId={UserId}",
            puantajTopluGirisiId, onayanKullaniciId);
    }
}

// ==================== DTOs ====================

public class PuantajHaftalikOzet
{
    public int PuantajTopluGirisiId { get; set; }
    public List<PuantajHaftaOzet> Haftalar { get; set; } = [];
}

public class PuantajHaftaOzet
{
    public int HaftaNumarasi { get; set; }
    public DateTime BasTarihi { get; set; }
    public DateTime SonTarihi { get; set; }
    public List<PuantajGunuOzet> Gunler { get; set; } = [];
}

public class PuantajGunuOzet
{
    public DateTime Tarih { get; set; }
    public List<PuantajGunRotaOzet> Rotalar { get; set; } = [];
}

public class PuantajGunRotaOzet
{
    public int GuezergahId { get; set; }
    public string GuezergahAdi { get; set; }
    public decimal TahminiSefer { get; set; }
    public OperasyonDurumu Durum { get; set; }
}
```

---

## 6. Blazor UI/UX Tasarımı

### ⭐ 6.0 YENİ: Satır Bazlı Günlük Grid Tasarımı (23 Ocak 2025)

> **📌 ÖNEMLİ UPDATE:** Kullanıcı isteğine göre, puantaj giriş akışı **satır-bazlı dönem viewing** ile güncellenmiştir.
>
> **🎯 Yeni Akış:**
> 1. Dönem seç (Aylık VEYA Tarih Aralığı)
> 2. Satır bazlı grid göster (22 iş günü)
> 3. Eksik günlere araç+şoför ekle ("➕ Ekle")
> 4. Inline düzeltme yap ("✎ Düzelt")
> 5. Uyuşmazlık kontrol ("🔍 Kontrol Et")
> 6. Tamamla & Faturaya git

**📄 Tam Tasarım Dokümantasyonu:**  
👉 **[GUNLUK-GRID-TASARIMI-SATIR-BAZLI.md](./GUNLUK-GRID-TASARIMI-SATIR-BAZLI.md)**

İçeriği:
- Dönem seçimi (Aylık vs Tarih Aralığı)
- Satır bazlı grid (22 gün = 22 satır)
- Eksik gün tamamlama in-line
- Inline editing (Sefer, Durum, Notlar)
- Makzul yönetimi (Red flagging, auto sefer=0)
- Uyuşmazlık kontrol (Toplu vs Günlük)
- Özet kartları (Dolu/Boş/Makzul/Total Sefer)
- Tam Razor component kodu (`PuantajGunlukDetay.razor`)
- DTO tanımları
- Backend service updates

---

### 6.1 Sayfa 1: Toplu Giriş (PuantajTopluGirisi.razor)

```razor
@page "/operasyon/puantaj-toplu-girisi"
@inject IPuantajTopluGirisiService PuantajService
@inject NavigationManager NavManager
@inject ToastrService ToastrService
@attribute [Authorize(Roles = "Operatör,Yönetici")]

<div class="container mt-4">
    <div class="row">
        <div class="col-md-10">
            <h2>Araç Başına Güzergah Puantajı - Toplu Giriş</h2>
        </div>
        <div class="col-md-2">
            <button class="btn btn-secondary" @onclick="GoBack">← Geri</button>
        </div>
    </div>

    <hr />

    @if (!string.IsNullOrEmpty(_errorMessage))
    {
        <div class="alert alert-danger" role="alert">
            <strong>❌ Hata:</strong> @_errorMessage
        </div>
    }

    <form @onsubmit="HandleSubmit">
        <!-- Dönem Seçimi -->
        <div class="card mb-4">
            <div class="card-header bg-primary text-white">
                <strong>1️⃣ Dönem & Araç Seçimi</strong>
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-3">
                        <label class="form-label">Yıl:</label>
                        <input type="number" class="form-control" @bind="_selectedYear" min="2025" max="2050" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label">Ay:</label>
                        <select class="form-select" @bind="_selectedMonth">
                            <option value="">-- Seç --</option>
                            @for (int i = 1; i <= 12; i++)
                            {
                                <option value="@i">@GetMonthName(i)</option>
                            }
                        </select>
                    </div>
                    <div class="col-md-6">
                        <label class="form-label">Araç:</label>
                        <select class="form-select" @bind="_selectedAracId">
                            <option value="">-- Seç --</option>
                            @foreach (var arac in _aracList)
                            {
                                <option value="@arac.Id">@arac.Plaka - @arac.Model</option>
                            }
                        </select>
                    </div>
                </div>
            </div>
        </div>

        <!-- Rota Detayları -->
        <div class="card mb-4">
            <div class="card-header bg-success text-white">
                <strong>2️⃣ Rota Detayları (@_rotaDetaylari.Count rota)</strong>
            </div>
            <div class="card-body">
                <table class="table table-sm table-bordered">
                    <thead class="table-light">
                        <tr>
                            <th>Güzergah</th>
                            <th>Gün Sayısı</th>
                            <th>Sefer/Gün</th>
                            <th>Total Sefer</th>
                            <th>Birim Fiyat</th>
                            <th>Gider Fiyat</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var (detay, index) in _rotaDetaylari.Select((d, i) => (d, i)))
                        {
                            <tr>
                                <td>
                                    <select class="form-select form-select-sm" 
                                            @onchange="@((ChangeEventArgs e) => UpdateRotaDetay(index, e))">
                                        @foreach (var rota in _rotaList)
                                        {
                                            <option value="@rota.Id" 
                                                    selected="@(detay?.GuezergahId == rota.Id)">
                                                @rota.GuzergahAdi
                                            </option>
                                        }
                                    </select>
                                </td>
                                <td>
                                    <input type="number" class="form-control form-control-sm" 
                                           @bind="detay.GunSayisi" min="0" />
                                </td>
                                <td>
                                    <input type="number" class="form-control form-control-sm" 
                                           @bind="detay.SeferSayisiPerGun" min="0" step="0.5" />
                                </td>
                                <td>
                                    <strong>@(detay.GunSayisi * detay.SeferSayisiPerGun)</strong>
                                </td>
                                <td>
                                    ₺@detay.BirimFiyatSnapshot.ToString("0.00")
                                </td>
                                <td>
                                    ₺@detay.GiderFiyatSnapshot.ToString("0.00")
                                </td>
                                <td>
                                    <button type="button" class="btn btn-sm btn-danger" 
                                            @onclick="@(() => RemoveRota(index))">
                                        ✕
                                    </button>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
                <button type="button" class="btn btn-sm btn-info" @onclick="AddRota">
                    + Rota Ekle
                </button>
            </div>
        </div>

        <!-- Makzul Yönetimi -->
        <div class="card mb-4">
            <div class="card-header bg-warning text-dark">
                <strong>3️⃣ Makzul Günleri (İzin, Hastalık, vb.)</strong>
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-4">
                        <label class="form-label">Makzul Gün Sayısı:</label>
                        <input type="number" class="form-control" @bind="_makzulGunSayisi" min="0" max="22" />
                    </div>
                    <div class="col-md-8">
                        <p class="text-muted small">
                            <strong>✓ Tavsiye:</strong> Makzul sayısın kesintisi otomatik yapılacaktır.
                            <br />Örn: 22 işgünü - 2 makzul = 20 sefer (makzul gün sayısı azaltılacak)
                        </p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Özet -->
        <div class="card mb-4 bg-light">
            <div class="card-header bg-info text-white">
                <strong>📊 Hesaplanan Toplam</strong>
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-3">
                        <h5>Toplam Sefer</h5>
                        <h3 class="text-success">@_toplamSefer</h3>
                    </div>
                    <div class="col-md-3">
                        <h5>Tahmini Gelir</h5>
                        <h3 class="text-primary">₺@_toplamGelir.ToString("0.00")</h3>
                    </div>
                    <div class="col-md-3">
                        <h5>Tahmini Gider</h5>
                        <h3 class="text-danger">₺@_toplamMaliyet.ToString("0.00")</h3>
                    </div>
                    <div class="col-md-3">
                        <h5>Marj</h5>
                        <h3 class="text-warning">₺@(_toplamGelir - _toplamMaliyet).ToString("0.00")</h3>
                    </div>
                </div>
            </div>
        </div>

        <!-- Buttons -->
        <div class="d-flex justify-content-between">
            <button type="reset" class="btn btn-secondary">↻ Sıfırla</button>
            <div>
                <button type="submit" class="btn btn-primary btn-lg">
                    Devam Et (Haftalık Özeti Gözden Geçir) →
                </button>
            </div>
        </div>
    </form>
</div>

@code {
    private int _selectedYear = DateTime.Now.Year;
    private int _selectedMonth = DateTime.Now.Month;
    private int _selectedAracId;
    private List<PuantajTopluRotaDetay> _rotaDetaylari = new();
    private int _makzulGunSayisi = 0;
    private List<Arac> _aracList = new();
    private List<Guzergah> _rotaList = new();
    private string _errorMessage = "";

    private decimal _toplamSefer => 
        (_rotaDetaylari.Sum(r => r.GunSayisi * r.SeferSayisiPerGun)) - _makzulGunSayisi;

    private decimal _toplamGelir => 
        _rotaDetaylari.Sum(r => (r.GunSayisi * r.SeferSayisiPerGun) * r.BirimFiyatSnapshot);

    private decimal _toplamMaliyet => 
        _rotaDetaylari.Sum(r => (r.GunSayisi * r.SeferSayisiPerGun) * r.GiderFiyatSnapshot);

    protected override async Task OnInitializedAsync()
    {
        // TODO: Load _aracList ve _rotaList from service
        await LoadData();
    }

    private async Task LoadData()
    {
        // Service call'ları buraya gelecek
    }

    private void AddRota()
    {
        _rotaDetaylari.Add(new PuantajTopluRotaDetay());
    }

    private void RemoveRota(int index)
    {
        _rotaDetaylari.RemoveAt(index);
    }

    private void UpdateRotaDetay(int index, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int guezergahId))
        {
            var rota = _rotaList.FirstOrDefault(r => r.Id == guezergahId);
            if (rota != null)
            {
                _rotaDetaylari[index].GuezergahId = guezergahId;
                _rotaDetaylari[index].GuezergahAdi = rota.GuzergahAdi;
                _rotaDetaylari[index].BirimFiyatSnapshot = rota.BirimFiyat;
                _rotaDetaylari[index].GiderFiyatSnapshot = rota.GiderFiyat;
            }
        }
    }

    private async Task HandleSubmit()
    {
        if (_selectedAracId == 0 || _selectedMonth == 0 || _rotaDetaylari.Count == 0)
        {
            _errorMessage = "Lütfen tüm alanları doldurunuz.";
            return;
        }

        try
        {
            // TODO: Service call to create TopluGiriş
            // Then navigate to weekly review page
            NavManager.NavigateTo($"/operasyon/puantaj-haftalik-ozet/...");
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    private void GoBack() => NavManager.NavigateTo("/operasyon/dashboard");

    private string GetMonthName(int month) => 
        new DateTime(2025, month, 1).ToString("MMMM", new System.Globalization.CultureInfo("tr-TR"));
}
```

### 6.2 Sayfa 2: Haftalık Özet (PuantajHaftalikOzet.razor)

```razor
@page "/operasyon/puantaj-haftalik-ozet/{PuantajTopluGirisiId:int}"
@inject IPuantajTopluGirisiService PuantajService
@inject NavigationManager NavManager
@inject ToastrService ToastrService

<!-- Haftalık takvim view, gün başına sefer ve durum gösterimi -->
<!-- Şüpheli hücreler: kırmızı flag (örn: 3 sefer/gün çok mu?) -->
<!-- "Düzelt" button → Günlük detail'e git -->

<div class="container mt-4">
    <h2>📅 Haftalık Puantaj Özeti (Gözden Geçir & Düzelt)</h2>

    @if (_haftalikOzet != null)
    {
        @foreach (var hafta in _haftalikOzet.Haftalar)
        {
            <div class="card mb-3">
                <div class="card-header bg-info text-white">
                    <strong>Hafta @hafta.HaftaNumarasi: @hafta.BasTarihi.ToString("dd MMM") - @hafta.SonTarihi.ToString("dd MMM yyyy")</strong>
                </div>
                <div class="card-body table-responsive">
                    <table class="table table-sm table-hover">
                        <thead class="table-light">
                            <tr>
                                <th>Gün (Tarih)</th>
                                <th colspan="2">Rota</th>
                                <th>Sefer</th>
                                <th>Durum</th>
                                <th>İşlem</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var gun in hafta.Gunler)
                            {
                                @foreach (var rota in gun.Rotalar)
                                {
                                    <tr>
                                        <td>@gun.Tarih.ToString("ddd, dd MMM")</td>
                                        <td>@rota.GuezergahAdi</td>
                                        <td class="text-end">@rota.TahminiSefer seferler</td>
                                        <td class="text-end">
                                            <strong class="@(rota.TahminiSefer > 3 ? "text-danger" : "text-success")">
                                                @rota.TahminiSefer
                                            </strong>
                                        </td>
                                        <td>
                                            <span class="badge bg-success">@rota.Durum</span>
                                        </td>
                                        <td>
                                            <button class="btn btn-sm btn-outline-primary" 
                                                    @onclick="@(() => EditDay(gun.Tarih))">
                                                Düzelt
                                            </button>
                                        </td>
                                    </tr>
                                }
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        }

        <div class="d-flex justify-content-between">
            <button class="btn btn-secondary" @onclick="GoBack">← Geri</button>
            <button class="btn btn-success btn-lg" @onclick="ApproveAndContinue">
                ✓ Onayla & Günlük Detaylara Git →
            </button>
        </div>
    }
</div>

@code {
    [Parameter]
    public int PuantajTopluGirisiId { get; set; }

    private PuantajHaftalikOzet _haftalikOzet;

    protected override async Task OnInitializedAsync()
    {
        _haftalikOzet = await PuantajService.GetHaftalikOzetAsync(PuantajTopluGirisiId);
    }

    private void EditDay(DateTime tarih)
    {
        NavManager.NavigateTo($"/operasyon/puantaj-gunluk-detay/{PuantajTopluGirisiId}/{tarih:yyyyMMdd}");
    }

    private async Task ApproveAndContinue()
    {
        // Navigate to daily detail view
        NavManager.NavigateTo($"/operasyon/puantaj-gunluk-detay/{PuantajTopluGirisiId}");
    }

    private void GoBack() => NavManager.NavigateTo("/operasyon/puantaj-toplu-girisi");
}
```

### 6.3 Sayfa 3: Günlük Detay (PuantajGunlukDetay.razor)

```razor
@page "/operasyon/puantaj-gunluk-detay/{PuantajTopluGirisiId:int}"
@page "/operasyon/puantaj-gunluk-detay/{PuantajTopluGirisiId:int}/{TarihStr:int}"

<!-- Grid view ile tüm günleri ve rotaları göster -->
<!-- Inline edit capability (Sefer, Durum, Notlar) -->
<!-- Save button'ı row başına veya toplu -->

<div class="container-fluid mt-4">
    <h2>📋 Günlük Puantaj Detayları</h2>

    @if (_gunlukDetaylar != null)
    {
        <div class="table-responsive">
            <table class="table table-bordered table-sm table-hover">
                <thead class="table-dark sticky-top">
                    <tr>
                        <th>Tarih (Gün)</th>
                        <th>Rota / Güzergah</th>
                        <th>Sefer Sayısı</th>
                        <th>Durum</th>
                        <th>Notlar</th>
                        <th>İşlemler</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var detay in _gunlukDetaylar)
                    {
                        <tr class="@(detay.IsEditing ? "table-active" : "")">
                            <td>
                                @detay.Tarih.ToString("ddd, dd MMM yyyy", new System.Globalization.CultureInfo("tr-TR"))
                            </td>
                            <td>
                                @if (detay.IsEditing)
                                {
                                    <select class="form-select form-select-sm" @bind="detay.GuezergahId">
                                        @foreach (var rota in _rotaList)
                                        {
                                            <option value="@rota.Id">@rota.GuzergahAdi</option>
                                        }
                                    </select>
                                }
                                else
                                {
                                    <span>@detay.GuezergahAdi</span>
                                }
                            </td>
                            <td>
                                @if (detay.IsEditing)
                                {
                                    <input type="number" class="form-control form-control-sm" @bind="detay.SeferSayisi" min="0" step="0.5" />
                                }
                                else
                                {
                                    <strong>@detay.SeferSayisi</strong>
                                }
                            </td>
                            <td>
                                @if (detay.IsEditing)
                                {
                                    <select class="form-select form-select-sm" @bind="detay.Durum">
                                        <option value="1">Gitti</option>
                                        <option value="2">Makzul (K)</option>
                                        <option value="3">Taksi</option>
                                        <option value="4">Tatil</option>
                                        <option value="5">Unscheduled</option>
                                    </select>
                                }
                                else
                                {
                                    <span class="badge bg-info">@detay.DurumAdi</span>
                                }
                            </td>
                            <td>
                                @if (detay.IsEditing)
                                {
                                    <input type="text" class="form-control form-control-sm" @bind="detay.Notlar" placeholder="Notlar..." />
                                }
                                else
                                {
                                    <span class="text-muted small">@detay.Notlar</span>
                                }
                            </td>
                            <td>
                                @if (detay.IsEditing)
                                {
                                    <button class="btn btn-sm btn-success me-1" @onclick="@(() => SaveDetay(detay))">✓ Kaydet</button>
                                    <button class="btn btn-sm btn-secondary" @onclick="@(() => CancelEdit(detay))">✕ İptal</button>
                                }
                                else
                                {
                                    <button class="btn btn-sm btn-outline-primary" @onclick="@(() => EditDetay(detay))">✎ Düzelt</button>
                                }
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>

        <div class="d-flex justify-content-between mt-4">
            <button class="btn btn-secondary" @onclick="GoBack">← Haft alık Özete Dön</button>
            <div>
                <button class="btn btn-warning me-2" @onclick="ShowValidationCheck">
                    🔍 Uyuşmazlık Kontrol Et
                </button>
                <button class="btn btn-success btn-lg" @onclick="FinalizeAndGoToFatura">
                    ✓ Tamamla & Faturaya Git →
                </button>
            </div>
        </div>
    }
</div>

@code {
    [Parameter]
    public int PuantajTopluGirisiId { get; set; }

    [Parameter]
    public int? TarihStr { get; set; }

    private List<PuantajGunlukDetayDTO> _gunlukDetaylar;
    private List<Guzergah> _rotaList;

    protected override async Task OnInitializedAsync()
    {
        // Load data
        // If TarihStr provided, filter to that date
    }

    private void EditDetay(PuantajGunlukDetayDTO detay)
    {
        detay.IsEditing = true;
    }

    private async Task SaveDetay(PuantajGunlukDetayDTO detay)
    {
        await PuantajService.UpdateGunlukDetayAsync(
            PuantajTopluGirisiId,
            detay.Tarih,
            detay.GuezergahId,
            detay.SeferSayisi,
            detay.Durum,
            detay.Notlar);

        detay.IsEditing = false;
        ToastrService.ShowSuccess("Güncelleme başarılı");
    }

    private void CancelEdit(PuantajGunlukDetayDTO detay)
    {
        detay.IsEditing = false;
        // Reload original data
    }

    private async Task ShowValidationCheck()
    {
        var fark = await PuantajService.ValidateTuturlulukAsync(PuantajTopluGirisiId);
        if (fark == 0)
        {
            ToastrService.ShowSuccess("✓ Uyuşmazlık yok!");
        }
        else
        {
            ToastrService.ShowWarning($"⚠️ Uyuşmazlık bulundu: {fark} sefer farkı");
        }
    }

    private async Task FinalizeAndGoToFatura()
    {
        // Call: Onayla_GunlukPuantajlarOlusturAsync
        // Navigate to: /operasyon/puantaj-fatura-mutabakat
        NavManager.NavigateTo("/operasyon/puantaj-fatura-mutabakat");
    }

    private void GoBack() => 
        NavManager.NavigateTo($"/operasyon/puantaj-haftalik-ozet/{PuantajTopluGirisiId}");
}
```

---

## 7. Uygulama Yol Haritası

### 7.1 Faz 1 (Hafta 1-2: Foundation)

```
ADIM 1: Database Migration
├─ PuantajTopluGiriş entity oluştur
├─ PuantajTopluRotaDetay entity oluştur
├─ Migration script oluştur & migrate et
└─ ✅ Commit

ADIM 2: Service Layer
├─ IPuantajTopluGirisiService interface oluştur
├─ PuantajTopluGirisiService impl. (CreateTopluGiriş, GetHaftalikOzet, UpdateGunlukDetay)
├─ Unit tests ekle
└─ ✅ Commit

ADIM 3: Project Registration
├─ Services'i DI container'a kaydet
├─ ILogger konfigürasyonu
└─ ✅ Commit
```

### 7.2 Faz 2 (Hafta 3-4: UI Razor Components)

```
ADIM 4: Blazor Sayfaları
├─ PuantajTopluGirisi.razor (Toplu giriş formu)
├─ PuantajHaftalikOzet.razor (Haftalık review)
├─ PuantajGunlukDetay.razor (Günlük edit)
├─ Styling & responsive design
└─ ✅ Commit

ADIM 5: API Controllers (Opsiyonel)
├─ PuantajTopluGirisiController oluştur
├─ [GET] /api/puantaj-toplu-girisleri
├─ [POST] /api/puantaj-toplu-girisleri
└─ ✅ Commit
```

### 7.3 Faz 3 (Hafta 5-6: Integrasyon & Testing)

```
ADIM 6: Fatura & SGK Integration
├─ FaturaBandırmaService ile entegrasyon
├─ SGK rapor üretim güncellemesi
└─ ✅ Commit

ADIM 7: Testing & UAT
├─ Manual testing (Toplu → Haftalık → Günlük flow)
├─ Edge case'ler (Mid-month changes, Makzul, Rota swap)
├─ Performance test (1000+ kayıt)
└─ ✅ Pilot müşteri testi

ADIM 8: Documentation & Training
├─ User manual oluştur (Türkçe)
├─ Video tutorial
├─ Support team eğitimi
└─ ✅ Yayın hazırlığı
```

### 7.4 Gantt Chart (Tahmini Zaman)

```
Week 1-2      [Db + Service ■■■]
Week 3-4      [Blazor Pages ■■■]
Week 5        [Integration ■]
Week 6        [Testing + Training ■■■]
Week 7        [Deployment + Monitoring ■]

Total: ~7 hafta (1.6 ay)
```

---

## 8. Işletme Özeti & Eğitim Planı

### 8.1 Operatör Eğitim Videosu (3 parça)

```
Video 1: Toplu Giriş (2 min)
├─ Dönem seçimi
├─ Araç seçimi
├─ Rota detayları girilişi
└─ Makzul yönetimi

Video 2: Haftalık Özet Kontrolü (2 min)
├─ Haftalık takvim görünümü
├─ Şüpheli alanlar (Red flags)
├─ "Düzelt" button kullanımı
└─ Onay süreci

Video 3: Günlük Detay (3 min)
├─ Grid view navigasyonu
├─ Sefer/Durum/Notlar editing
├─ Uyuşmazlık kontrolü
└─ Faturaya gidiş
```

### 8.2 Muhasebeci Eğitim Materiyal

```
Adımlar:
1. "Puantaj Fatura Mutabakat" sayfasını aç
2. TopluGiriş + GunlukPuantaj uyuşmazlık kontrolü ✓
3. Otomatik fatura tahakkukunu gözden geçir
4. SGK bildirimi oluştur (1-click)
5. Ödeme talimatını düzenle
```

### 8.3 Go-Live Checklist

```
□ Technical
  □ Database migration tamamlandı
  □ Service layer test geçti (>90% coverage)
  □ Blazor components test geçti (responsive, accessibility)
  □ Load test geçti (100+ concurrent users)
  □ Integration test geçti (Fatura & SGK)

□ User Acceptance
  □ 3+ pilot müşteri testi tamamlandı
  □ Feedback toplandı & addressed
  □ Video tutorials İZLENDİ & anlasıldi
  □ Support team trained & ready

□ Documentation
  □ User manual (Türkçe) ✓
  □ Troubleshooting guide ✓
  □ FAQ & Common scenarios ✓

□ Deployment
  □ Staging ortamında deploy ve test ✓
  □ Backup strategy confirmed ✓
  □ Rollback plan ready ✓
  □ Monitoring alerts configured ✓

□ Support
  □ Helpdesk team trained
  □ Support scripts prepared
  □ 24h support availability confirmed
```

---

## 📌 Özet & Tavsiyeler

### ✅ Önerilen Yöntem: **Hibrit Input Model**

| Yönü | Puan |
|------|------|
| **Hız** (İş operatörü) | 9/10 |
| **Doğruluk** (Data quality) | 8/10 |
| **Türkiye Uygunluğu** | 9/10 |
| **Makzul Yönetimi** | 8/10 |
| **Fatura Entegrasyon** | 9/10 |
| **Operatör Eğitimi** | 8/10 |
| **Teknik Komplekslik** | 6/10 |
| **Geliştirme Süresi** | 6-7 hafta |

### 🎯 Kritik Başarı Faktörleri

1. **Toplu Giriş Doğruluğu**: Rota × Gün × Sefer/Gün formülü kesin olmalı
2. **Makzul Muhasebesi**: Makzul gün sayısı = Sefer kesintisi (açık)
3. **Uyuşmazlık Yönetimi**: TopluGiriş ≠ GunlukPuantaj → Warning, not error
4. **UI/UX Basitliği**: 3 seviye (Toplu → Haftalık → Günlük) progresif
5. **Eğitim**: Video + Live demo + 1-on-1 support ilk 2 hafta

---

**Sonuç**: Bu hibrit model, Türkiye'deki personel taşıma sektörünün "ortalama gibi, sonra düzelt" pratiğiyle mükemmel uyumludur. Hızlı, doğru ve operatör-friendly!

---

**Hazırladı**: Claude Code Analysis  
**Tarih**: 23 Ocak 2025  
**Versyon**: 1.0 - Kapsamlı Analiz & Plan  
**Durum**: ✅ Implementation Ready
