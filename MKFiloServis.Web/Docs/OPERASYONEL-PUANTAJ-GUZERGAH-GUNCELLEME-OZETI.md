# Operasyonel Puantaj - Güzergah Dimension Güncelleme Özeti

**Tarih**: 23 Ocak 2025  
**Durum**: ✅ Aşama 1-3 Tamamlandı  
**Sonraki**: Blazor UI/Service Uygulaması

---

## 📋 Yapılan Çalışmalar

### Yeni Oluşturulan Dokümanlar

1. **`OPERASYONEL-PUANTAJ-GUZERGAH-DIMENSION.md`** (Derin Analiz)
   - ✅ Güzergah veri modeli - 14 alan detaylı analiz
   - ✅ Güzergah-Puantaj ilişkisi & veri akışı (uçtan uca 9 adım)
   - ✅ Snapshot pattern önerisi (fiyat stabilitesi için)
   - ✅ Program iyileştirmeleri (validasyon, notifikasyon, Blazor rapor)
   - ✅ SQL reporting patterns ve güzergah bazında aylık rapor

2. **`GUZERGAH-SEFER-SLOT-TEKNIK-REFERANS.md`** (Operasyonel Detay)
   - ✅ GuzergahSefer entity mimarisi
   - ✅ Sefer slot türleri (Sabah, Akşam, Öğle, Mesai, Diger1-5)
   - ✅ 3 senaryo (Basit, Üç sefer, 24 Saat vardiya)
   - ✅ IGuzergahSeferService taslağı & implementasyon
   - ✅ Validasyon kuralları ve hızlı referans tablosu

3. **`FILOGUZERGAH-ESLESTIRME-PUANTAJ-VERI-AKISI.md`** (Mapping & Akış)
   - ✅ Entity ilişkisi (1-to-Many FK mapping)
   - ✅ 2.2 adımlı veri akışı (Şablon → Job → Puantaj)
   - ✅ Snapshot pattern detaylı (Fiyat değişim sorunu çözümü)
   - ✅ 3 SQL JOIN pattern'i (Sorgu örnekleri)
   - ✅ Service implementasyon kodu (`GenerateDailyPuantajsForTomorrowAsync`)
   - ✅ Troubleshooting kılavuzu

4. **Ana Rapor Güncellemesi** (`PERSONEL-TASIMA-PUANTAJ-SISTEMI-RAPORU.md`)
   - ✅ İçindekiler genişletildi (3.1, 3.2, 3.3 alt bölümler)
   - ✅ Yeni doküman referansları eklendi
   - ✅ Güzergah boyutu tanıtımı (Sefer Slot yapılandırması ile)
   - ✅ FiloGuzergahEslestirme + FiloGunlukPuantaj veri akışı diyagramı
   - ✅ Snapshot pattern tanıtımı

---

## 🏗 Program Akışı Düzenlemesi

### Operasyonel Puantaj - Yeni Mimarisi

```
┌─────────────────────────────────────────────────┐
│              Güzergah (Rota Dimension)          │
│  ├─ GezrghahKodu, GuzergahAdi                   │
│  ├─ BirimFiyat (Kurumdan tahsil)                │
│  ├─ GiderFiyat (Şoför maliyeti)                 │
│  ├─ SeferTipi (Sabah/Akşam/SabahAksam)          │
│  ├─ PersonelSayisi, Mesafe, Kapasite           │
│  └─ [GuzergahSefer] Slot detayları (1..N)      │
│     ├─ Sira, Slot (Sabah/Akşam/Öğle)            │
│     ├─ KapasiteAdi ("16+1", "8+1")              │
│     ├─ AracId, SoforAd                          │
│     └─ FirmaAdiSerbest (Tedarikçi)              │
└─────────────────────────────────────────────────┘
             │
             │ 1-to-Many (FK)
             ▼
┌─────────────────────────────────────────────────┐
│    FiloGuzergahEslestirme (Şablon)              │
│  ├─ KurumFirmaId (Müşteri)                      │
│  ├─ GuzergahId (FK)                             │
│  ├─ AracId, SoforId                             │
│  ├─ KurumaKesilecekUcret (Güzergah.BirimFiyat) │
│  ├─ TaseronaOdenenUcret (Güzergah.GiderFiyat)  │
│  └─ [SNAPSHOT] Fiyat History                    │
└─────────────────────────────────────────────────┘
             │
      ┌──────│ Otomatik Job (04:00)
      │      │ "22 gün × 2 sefer/gün puantaj üret"
      │      ▼
      │  ┌─────────────────────────────────────┐
      │  │   FiloGunlukPuantaj (Günlük)       │
      │  │   ├─ Tarih (2025-01-02 ... 01-29) │
      │  │   ├─ FiloGuzergahEslestirmeId (FK)│
      │  │   ├─ GuzergahId, AracId, SoforId  │
      │  │   ├─ SeferSayisi = 2              │
      │  │   ├─ PuantajCarpani (1.0/0.5/1.5)│
      │  │   ├─ Tahukkuk = Sefer × Fiyat × Çarpan
      │  │   ├─ Durum (Gitti/Gitmedi/Arıza)│
      │  │   └─ Onaylandi (Terminal Control)│
      │  └─────────────────────────────────────┘
      │
      └──→ [22 kayıt] × 2 sefer = 44 sefer
           Toplam: 44 × 150 = 6.600 TL (Gelir)
                   44 × 80 = 3.520 TL (Gider)
                   Kar = 3.080 TL
```

### Operasyonel Puantaj Yaşam Döngüsü

```
[1 PLANLAMA AŞAMASI] (Sabah 04:00 - Sistem Job)
    ↓
    FiloGuzergahEslestirme'den oku
    ↓
    Yarın için tüm eşleştirmeler taraması
    ↓
    Çarpan hesapla (Hafta sonu? Bayram?)
    ↓
    INSERT 22 × FiloGunlukPuantaj (Durum=Planli)

[2 OPERASYON AŞAMASI] (Gün boyunca)
    ↓
    Sabah 06:30: Şoför "başladı" QR'sı
    ↓
    Akşam 18:00: Şoför "bitti" QR'sı
    ↓
    UPDATE FiloGunlukPuantaj.Durum = Gitti

[3 TERMINAL KONTROLÜ] (Akşam 17:45)
    ↓
    Operatör kontrol: KM, Yakıt, Temizlik, Hasar
    ↓
    Onaylandi = true
    ↓
    OnayTarihi = NOW()

[4 AY SONU HESAPLAMA] (28 Ocak, 02:00)
    ↓
    SELECT * FROM FiloGunlukPuantaj
    WHERE Yil=2025, Ay=1, Onaylandi=true
    ↓
    GROUP BY GuzergahId
    ↓
    INSERT HakedisPuantaj (Güzergah Özeti)
    ├─ 44 sefer
    ├─ 6.600 TL gelir
    └─ 3.520 TL gider
    ↓
    GROUP BY KurumFirmaId
    ↓
    INSERT Hakedis (Kurum Faturası)
    ├─ 122 sefer (tüm güzergahlar)
    ├─ 18.140 TL
    └─ Durum = Taslak (Onay bekliyor)

[5 RAPORLAMA] (Ay boyunca / Ay sonu)
    ↓
    Güzergah Performans Raporu
    ├─ Çalışılan gün sayısı
    ├─ Toplam sefer sayısı
    ├─ Gelir / Gider
    ├─ Kar Marjı %
    └─ Trend (6 ay)
```

---

## 📊 Raporlama Kapasitesi

### 1. Güzergah Bazında Aylık Özet

```sql
SELECT 
    gz.GuzergahKodu,
    gz.GuzergahAdi,
    COUNT(DISTINCT fgp.Tarih) AS CalisilanGun,
    SUM(CASE WHEN fgp.Durum=1 THEN fgp.SeferSayisi ELSE 0 END) AS ToplamSefer,
    SUM(fgp.TahakkukEdenKurumUcreti) AS Gelir,
    SUM(fgp.TahakkukEdenTaseronUcreti) AS Gider,
    SUM(fgp.TahakkukEdenKurumUcreti) - SUM(fgp.TahakkukEdenTaseronUcreti) AS Kar
FROM FiloGunlukPuantajlar fgp
JOIN Guzergahlar gz ON fgp.GuzergahId = gz.Id
WHERE YEAR(fgp.Tarih) = 2025 AND MONTH(fgp.Tarih) = 1
GROUP BY gz.Id, gz.GuzergahKodu, gz.GuzergahAdi
ORDER BY gz.GuzergahAdi;
```

**Örnek Sonuç**:

| Kod | Rota Adı | Gün | Sefer | Gelir | Gider | Kar |
|-----|----------|-----|-------|-------|-------|-----|
| GZR001 | TRT Merkez | 22 | 44 | 6.600 | 3.520 | 3.080 |
| GZR002 | TRT Çiftçiler | 19 | 38 | 5.700 | 3.040 | 2.660 |
| GZR003 | İSKİ Ankara | 21 | 42 | 5.040 | 2.520 | 2.520 |
| **TOPLAM** | | **62** | **124** | **17.340** | **9.080** | **8.260** |

### 2. Blazor Raporlama Bileşeni (Tasarım)

- `GuzergahPerformansRaporu.razor` (Taslak doküman içinde)
- Aylık/Haftalı filtreleme
- Güzergah rengi gösterimi (#3388ff)
- Kar marjı trending
- Detail page (günlük detaylarını göster)

---

## 🔧 Teknik Uygulanacaklar

### Immediate (1-2 gün)

- [ ] `IGuzergahSeferService` implement et (GuzergahSeferService.cs )
- [ ] `GenerateDailyPuantajsForTomorrowAsync` Job'u uygulamaya koy (Scheduled)
- [ ] Snapshot pattern alanlarını migration ile ekle

### Short-term (1-2 hafta)

- [ ] Blazor UI: Güzergah List yönetim sayfası
- [ ] Blazor UI: GuzergahSefer slot yönetimi
- [ ] Blazor UI: FiloGuzergahEslestirme şablon oluşturma
- [ ] Blazor UI: Öğretici/Onboarding ekranı

### Medium-term (1 ay)

- [ ] Güzergah Performans Raporu (GuzergahPerformansRaporu.razor)
- [ ] Hakedis hesaplama Job'u (tamamlama / hata düzeltme)
- [ ] Trend analizi (6 aylık güzergah karşılaştırması)
- [ ] Fiyat revision önerileri (ML/Analytics)

---

## 📚 Referans Yapısı

```
MKFiloServis.Web/Docs/
├─ PERSONEL-TASIMA-PUANTAJ-SISTEMI-RAPORU.md
│  └─ Ana rapor (✅ Güzergah bölümü eklendi 2025-01-23)
│
├─ OPERASYONEL-PUANTAJ-GUZERGAH-DIMENSION.md (📄 YENİ)
│  └─ Derin analiz: Güzergah modeli, veri akışı, snapshot, raporlama
│
├─ GUZERGAH-SEFER-SLOT-TEKNIK-REFERANS.md (📄 YENİ)
│  └─ Teknik kılavuz: Sefer slot yapılandırması, service, validasyon
│
├─ FILOGUZERGAH-ESLESTIRME-PUANTAJ-VERI-AKISI.md (📄 YENİ)
│  └─ Veri akışı: Şablon → Job → Puantaj mapping, SQL, troubleshooting
│
├─ OPERASYONEL-PUANTAJ-GUZERGAH-GUNCELLEME-OZETI.md (📄 Bu Dosya)
│  └─ Özet: Yapılanlar, program akışı, teknik todo
│
└─ [Gerekirse] İmplementasyon Kılavuzu
   └─ Adım-adım service/UI uygulaması
```

---

## ✅ Doğrulama Kontrol Listesi

```
KOD ENTEGRASYONU
├─ [ ] Snapshot pattern işliyor mu? (GuzergahBirimFiyatSnapshot tutuldu mu?)
├─ [ ] GuzergahSefer entity migration'ı tamam mı?
├─ [ ] FiloGuzergahEslestirme + FiloGunlukPuantaj FK ilişkisi?
└─ [ ] Existing puantaj kayıtları data integrity kaybetmedi mi?

RAPOR DOĞRULAMA
├─ [ ] Güzergah bazında özet sorgusu çalışıyor mu?
├─ [ ] Toplam sefer sayısı = SeferSayisi × Gün sayısı?
├─ [ ] Tutar = Sefer × BirimFiyat × Çarpan?
└─ [ ] Kar = Gelir - Gider?

OPERASYON DOĞRULAMA
├─ [ ] Günlük puantaj Job'u 04:00'de çalışıyor mu?
├─ [ ] Terminal onayı Onaylandi = true yapıyor mu?
├─ [ ] Hakedis ay son'unda otomatik oluşturuluyor mu?
└─ [ ] Gmail/Email notifikasyonları gidiyor mu?

RAPORLAMA DOĞRULAMA
├─ [ ] Güzergah performans raporu doğru tutarları gösteriyor mu?
├─ [ ] Blazor tablosu renk gösterimi (#3388ff) çalışıyor mu?
├─ [ ] Kar marjı yüzdeleri hesaplanıyor mu?
└─ [ ] Detail modal günlük kayıtları gösteriyor mu?
```

---

## 📞 İletişim & Notlar

- **Dokümantasyon Dili**: Türkçe (Kullanıcı tercihi)
- **Kod Dili**: C# (.NET 10/8)
- **UI Framework**: Blazor (Web project)
- **Teknik İnceleme**: Puantaj modülü sadece, diğer modüllere dokunulmuyor

---

**Tarihçe**  
- v1.0 (2025-01-23): Güzergah dimension analiz tamamlandı, ana rapor güncellendi
