# Puantaj Sonrası Fatura Hazırlık ve Eşleştirme Analiz Raporu — B1 Only

**Tarih**: 2026-06-19 (revizyon 2)
**Durum**: KOD İNCELEMESİ TAMAMLANDI — B2 (HakedisPuantaj) tamamen çıkarıldı, sadece B1 analizi

---

## 1. B1 Veri Akışı — OperasyonKaydi → PuantajKayit → PuantajDetay

### 1.1 Mimari

```
┌─────────────────────────────────────────────────────────────────┐
│                        B1 ANA HAT (TEK KAYNAK)                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  OperasyonKaydi (günlük)                                        │
│    │  Her gün, her araç+güzergah+slot için 1 satır              │
│    │  Kaynak: Manuel giriş, Excel import, şablon oluşturma      │
│    │  Alanlar: Tarih, GuzergahId, AracId, SoforId, Slot,       │
│    │           KurumId, SeferSayisi, PuantajCarpani,            │
│    │           KaynakTipi, FinansYonu, SoforOdemeTipi,          │
│    │           OdemeYapilacakCariId, FaturaKesiciCariId         │
│    │                                                             │
│    ▼                                                            │
│  PuantajEngineService.ProcessDonemAsync()                       │
│    │  Aylık toplu işlem (Quartz job veya manuel tetik)          │
│    │  Gruplama: (GuzergahId, AracId, Slot)                      │
│    │  Her OperasyonKaydi.Gün → ilgili GunNN sütununa yazılır    │
│    │  BirimGelir/BirimGider: Güzergah varsayılanı veya          │
│    │      FiloGuzergahEslestirme üzerinden override              │
│    │  TransactionScope ile tam atomic                          │
│    │                                                             │
│    ├──► PuantajHesapDonemi                                      │
│    │      Yil, Ay, KurumId, Versiyon                            │
│    │      Durum: Taslak → Aktif → Superseded                    │
│    │      OnayDurum: Bekliyor → FinansOnaylandi →               │
│    │                 MuhasebeOnaylandi → Kilitli                │
│    │                                                             │
│    ├──► PuantajKayit (aylık, grup başına 1 satır)              │
│    │      Gun01..Gun31 (int), BirimGelir, ToplamGelir           │
│    │      KDV %10 + %20 ayrı ayrı (gelir ve gider)              │
│    │      Kesinti, Alinacak, Odenecek                            │
│    │      FaturaKesiciCariId, OdemeYapilacakCariId              │
│    │      OnayDurum, HesapDonemiId, Versiyon                    │
│    │                                                             │
│    └──► PuantajDetay (junction — izlenebilirlik)                │
│           OperasyonKaydiId → OperasyonKaydi                     │
│           PuantajKayitId → PuantajKayit                         │
│           HesapDonemiId → PuantajHesapDonemi                     │
│           BirimGelir, BirimGider, SeferSayisi, HesaplananTutar  │
│                                                                  │
│    ▼                                                            │
│  PuantajFaturaRaporService (readonly)                           │
│    │  Kaynak: PuantajKayit (OnayDurum == Onaylandi)            │
│    │  Tek kaynak, merge yok, dedup riski yok                    │
│    │                                                             │
│    ├──► GetOzetAsync — özet KPI kartları                        │
│    ├──► GetSatirlarAsync — sayfalı liste                        │
│    ├──► GetAgacAsync — hiyerarşik ağaç                          │
│    └──► ExportExcelAsync — 6 sayfa Excel                        │
│                                                                  │
│    ▼                                                            │
│  PuantajFinansService                                           │
│    │  PuantajKayit → PuantajFinansalKayit → Fatura              │
│    │  GelirFaturasiUretAsync / GiderFaturasiUretAsync           │
│    │  TopluFaturaUretAsync (hesap dönemi bazlı)                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Kod Referansları

| Bileşen | Dosya | Ana Metot |
|---------|-------|-----------|
| OperasyonKaydi entity | `MKFiloServis.Shared/Entities/OperasyonKaydi.cs` | Tarih, GuzergahId, AracId, Slot, SeferSayisi, PuantajCarpani |
| PuantajKayit entity | `MKFiloServis.Shared/Entities/PuantajKayit.cs` | Gun01..Gun31, BirimGelir/Gider, KDV10/20, Kesinti, Alinacak/Odenecek |
| PuantajDetay entity | `MKFiloServis.Shared/Entities/PuantajDetay.cs` | OperasyonKaydiId, PuantajKayitId, HesapDonemiId |
| PuantajEngineService | `MKFiloServis.Web/Services/PuantajEngineService.cs` | `ProcessDonemAsync()` satır 22 |
| PuantajEngineJob | `MKFiloServis.Web/Jobs/PuantajEngineJob.cs` | Aylık otomatik Quartz job |
| PuantajFaturaRaporService | `MKFiloServis.Web/Services/PuantajFaturaRaporService.cs` | 5 public metot |
| PuantajFaturaRaporController | `MKFiloServis.Web/Controllers/PuantajFaturaRaporController.cs` | 4 API endpoint |
| PuantajFinansService | `MKFiloServis.Web/Services/PuantajFinansService.cs` | FinansalKayit + Fatura üretimi |

---

## 2. PuantajKayit Entity — Tüm Fatura Rapor Alanları

### 2.1 Tam Alan Listesi

| Grup | Alanlar | Fatura rapor için? |
|------|---------|-------------------|
| **Dönem** | `Yil`, `Ay` | ✅ |
| **Kurum** | `KurumId`, `KurumAdi`, `KurumCariId`, `IsverenFirmaId` | ✅ |
| **Güzergah** | `GuzergahId`, `GuzergahAdi` | ✅ |
| **Araç** | `AracId`, `Plaka` | ✅ |
| **Şoför** | `SoforId`, `SoforAdi`, `SoforTelefon`, `SoforOdemeTipi` | ✅ |
| **Cari/Fatura** | `OdemeYapilacakCariId`, `FaturaKesiciCariId`, `FaturaKesiciAdi`, `FaturaKesiciTelefon` | ✅ |
| **Slot/Yön** | `Slot`, `SlotAdi`, `Yon` | ✅ |
| **Kaynak** | `KaynakTipi` (Kendi/Tedarikci), `FinansYonu` (Gelen/Giden), `BelgeNo` | ✅ |
| **31 Gün** | `Gun01`..`Gun31` (int) + `GetGunDeger()` / `SetGunDeger()` | ✅ |
| **Gün toplam** | `Gun` (Gun01..Gun31 toplamı) | ✅ |
| **Gelir** | `BirimGelir`, `ToplamGelir`, `GelirKdvOrani`, `GelirKdvTutari`, `GelirToplam` | ✅ |
| **Gelir KDV/20** | `GelirKdvOrani20`, `GelirKdv20Tutari` | ✅ |
| **Gelir KDV/10** | `GelirKdvOrani10`, `GelirKdv10Tutari` | ✅ |
| **Gelir Kesinti** | `GelirKesinti` | ✅ |
| **Alınacak** | `Alinacak` | ✅ |
| **Gider** | `BirimGider`, `ToplamGider` | ✅ |
| **Gider KDV/20** | `GiderKdvOrani20`, `GiderKdv20Tutari` | ✅ |
| **Gider KDV/10** | `GiderKdvOrani10`, `GiderKdv10Tutari` | ✅ |
| **Gider Kesinti** | `GiderKesinti` | ✅ |
| **Ödenecek** | `Odenecek` | ✅ |
| **Fark** | `FarkTutari` (Alinacak - Odenecek, computed) | ✅ |
| **Fatura durumu** | `GelirFaturaKesildi`, `GelirFaturaNo`, `GelirFaturaTarihi`, `GelirFaturaId`, `GiderFaturaAlindi`, `GiderFaturaNo`, `GiderFaturaTarihi`, `GiderFaturaId` | ✅ |
| **Ödeme durumu** | `GelirOdemeDurumu`, `GelirOdemeTarihi`, `GelirOdenenTutar`, `GiderOdemeDurumu`, `GiderOdemeTarihi`, `GiderOdenenTutar` | Finansal |
| **Onay** | `OnayDurum`, `OnaylayanKullanici`, `OnayTarihi` | ✅ (filtre) |
| **Versiyon** | `HesapDonemiId`, `OncekiVersiyonId`, `Versiyon` | Audit |
| **Detay** | `PuantajDetaylari` (ICollection) | Audit trail |
| **Sıra** | `SiraNo`, `Bolge` | ✅ |
| **Hesaplama** | `HesaplaGelir()`, `HesaplaGider()`, `HesaplaPuantajToplam()` | Built-in |

**Sonuç**: PuantajKayit fatura raporlaması için gereken **tüm alanlara eksiksiz sahip.** KDV %10 + %20 ayrımı dahil.

### 2.2 Günlük Değerlerin Yapısı

PuantajKayit günlük veriyi **31 integer sütun** (`Gun01`..`Gun31`) ile depolar:

```csharp
// PuantajKayit.cs
public int Gun01 { get; set; }  // Ayın 1. günü sefer sayısı
public int Gun02 { get; set; }  // Ayın 2. günü sefer sayısı
// ...
public int Gun31 { get; set; }  // Ayın 31. günü sefer sayısı

public int Gun => Gun01 + Gun02 + ... + Gun31;  // Toplam

// Indexer benzeri erişim
public int GetGunDeger(int gun) => gun switch { 1 => Gun01, ... };
public void SetGunDeger(int gun, int deger) { ... }
```

Her gün değeri `OperasyonKaydi.SeferSayisi * OperasyonKaydi.PuantajCarpani` olarak hesaplanır.

---

## 3. Excel Kolon Karşılıkları (B1)

| # | Excel Kolonu | PuantajKayit Alanı | Durum |
|---|-------------|-------------------|-------|
| 1 | S.NO | `SiraNo` | ✅ |
| 2 | GÜZERGAH | `Guzergah.GuzergahAdi` | ✅ |
| 3 | GELİR | `ToplamGelir` | ✅ |
| 4 | GİDER | `ToplamGider` | ✅ |
| 5 | YÖN | `Slot` (Sabah/Aksam/SabahAksam) | ✅ |
| 6 | PLAKA | `Arac.AktifPlaka` veya `Plaka` (string) | ✅ |
| 7 | ŞOFÖR (tedarikçi) | `Sofor.Ad + Sofor.Soyad` | ✅ |
| 8 | ŞOFÖR (personel) | `Sofor.Ad + Sofor.Soyad` | ✅ |
| 9 | FATURA KESİLECEK CARİ | `FaturaKesiciCariId → Cari.Unvan` | ✅ |
| 10 | TELEFON | `FaturaKesiciTelefon` veya `Cari.Telefon` | ✅ |
| 11 | 1-31 GÜN | `Gun01`..`Gun31` | ✅ |
| 12 | TOPLAM GÜN | `Gun` | ✅ |
| 13 | TOPLAM TUTAR | `Alinacak` (gelir) / `Odenecek` (gider) | ✅ |
| 14 | KDV/20 | `GelirKdv20Tutari` / `GiderKdv20Tutari` | ✅ |
| 15 | KDV/10 | `GelirKdv10Tutari` / `GiderKdv10Tutari` | ✅ |
| 16 | KESİNTİ | `GelirKesinti` / `GiderKesinti` | ✅ |
| 17 | ÖDENECEK / TAHSİL EDİLECEK | `Alinacak` / `Odenecek` | ✅ |

**17/17 kolon B1'de eksiksiz karşılanıyor.**

---

## 4. PuantajFaturaRaporService — B1-Only Durum

### 4.1 Mevcut Durum

Servis şu anda **çift kaynaklı** çalışıyor: `PuantajKayit` + `HakedisPuantaj` → `Concat` merge. B2 çıkarıldığında yapılması gerekenler:

| Metot | Şu an | B1-Only sonrası |
|-------|-------|-----------------|
| `GetOzetAsync` | PK sorgusu + HP sorgusu → topla | **Sadece PK sorgusu** |
| `GetSatirlarAsync` | PK + HP → iki mapper → Concat → sırala → sayfala | **Sadece PK → mapper → sayfala** |
| `GetAgacAsync` | PK + HP → Concat → BuildAgac | **Sadece PK → BuildAgac** |
| `GetCountAsync` | pkCount + hpCount | **Sadece pkCount** |
| `ExportExcelAsync` | merge edilmiş listeden 6 sayfa | **Değişiklik yok** (zaten satırları işliyor) |

### 4.2 Çıkarılacak Kod

```
KALDIRILACAK:
- BuildHakedisPuantajQuery() metodu (~16 satır)
- ApplyYonFilterHp() metodu (~9 satır)
- MapHakedisPuantajToDto() metodu (~56 satır)
- Tüm metodlardaki HP sorgu çağrıları ve .Concat() birleştirmeleri
- HP .Include() zincirleri

DEĞİŞMEYECEK:
- PuantajFaturaSatirDto — tüm alanlar B1'den karşılanıyor
- PuantajFaturaAgacNodeDto
- PuantajFaturaOzetDto
- PuantajFaturaRaporController (4 API endpoint)
- DTO'daki Kaynak alanı sabit "PuantajKayit" olur
```

### 4.3 Basitleşme Sonucu

B2 çıkarıldıktan sonra servis ~80 satır hafifler, merge mantığı tamamen kalkar, dedup riski sıfırlanır.

---

## 5. Ağaç Yapıları (B1)

B1'de `KurumId`, `FaturaKesiciCariId`, `AracId`, `GuzergahId` doğrudan PuantajKayit üzerinde olduğu için tüm gruplamalar doğrudan yapılabilir:

### A) Kurum > Araç > Güzergah
```
PuantajKayit → KurumId → AracId → GuzergahId
```
**Avantaj**: Kurum bazlı fatura kesenler için doğal. Her kurumun araç ve güzergahları net.

### B) Kurum > Güzergah > Araç
```
PuantajKayit → KurumId → GuzergahId → AracId
```
**Avantaj**: Güzergah bazlı fiyatlandırmada ideal.

### C) Fatura Cari > Araç > Güzergah ⭐ (ÖNERİLEN)
```
PuantajKayit → FaturaKesiciCariId → AracId → GuzergahId
```
**Avantaj**: Fatura muhatabı bazında gruplama. Fatura kalemi oluşturmaya birebir uyar. Her cariye kesilecek fatura, toplam, KDV, kesinti net görünür.

### D) Tedarikçi > Araç > Güzergah
```
PuantajKayit (KaynakTipi == Tedarikci) → AracId → GuzergahId
```
**Avantaj**: Tedarikçiden gelen fatura kontrolü için ideal.

### Önerilen Varsayılan
**Fatura Cari > Araç > Güzergah** (gelir) + **Tedarikçi > Araç > Güzergah** (gider) çift ağaç.

---

## 6. Tüm Kurumlar Listeleme

| Konu | Durum |
|------|-------|
| `KurumId = null` → tüm kurumlar | ✅ `PuantajEngineService.ProcessDonemAsync()` nullable parametre |
| FirmaId tenant koruması | ✅ Global query filter otomatik |
| Yetki kontrolü | ✅ Mevcut yetki sistemi değişmez |
| Raporlama servisi | ✅ KurumId filtresi opsiyonel |

---

## 7. Fatura Hazırlık Modeli Önerisi

### Ana Kayıt
```
PuantajFaturaHazirlik
  - Id, FirmaId (tenant)
  - Yil, Ay
  - KurumId? (null = tüm kurumlar)
  - AgacYapisi (enum)
  - FaturaYonu (Gelir/Gider)
  - Durum (Taslak, Onaylandi, Faturalasti)
  - ToplamTutar, ToplamKdv, ToplamKesinti, NetTutar
  - CreatedAt, CreatedBy
```

### Satır Kayıt
```
PuantajFaturaHazirlikSatir
  - HazirlikId (FK)
  - PuantajKayitId (FK → kaynağa geri bağlantı)
  - Seviye1..4 (ağaç seviyeleri — string label)
  - KurumId?, CariId?, AracId?, GuzergahId?
  - Plaka, SoforAdi, Telefon (denormalize)
  - Gun01..Gun31 (int) — PuantajKayit'tan kopya
  - ToplamGun, ToplamSefer
  - BirimGelir, ToplamGelir, BirimGider, ToplamGider
  - KdvOrani20, Kdv20Tutar, KdvOrani10, Kdv10Tutar
  - Kesinti, Alinacak/Odenecek
  - FaturaId? (bağlandıysa)
  - ManuelDuzeltmeMi (bool), OrijinalTutar (decimal?)
```

### Grup Şablonu
```
FaturaGrupSablonu
  - Id, FirmaId
  - Ad
  - AgacYapisi (enum)
  - VarsayilanMi (bool)
  - KullaniciId? (kullanıcı bazlı)
```

### Manuel Düzeltme
**Gerekli.** Puantaj her zaman faturaya birebir uymaz:
- Ek kalem (özel servis, bekleme)
- Fiyat düzeltme (anlaşma değişikliği)
- Kalem silme (iptal sefer)

### Onay/Kilit
Puantaj onaylandıktan sonra fatura hazırlık **readonly**. Fatura kesildikten sonra `Faturalasti` → kilit.

---

## 8. E-Fatura ve Finans Zinciri

### 8.1 PuantajFinansService (B1 Hattı)

`PuantajFinansService` B1 tarafında zaten çalışan bir zincire sahip:

```
PuantajKayit (Onaylandi)
    │
    ▼
PuantajFinansalKayit (ara katman)
    │  FinansalKayitOlusturAsync(hesapDonemiId)
    │
    ├──► GelirFaturasiUretAsync → Fatura (Giden/Satış)
    │
    └──► GiderFaturasiUretAsync → Fatura (Gelen/Alış)
    
TopluFaturaUretAsync → tüm dönem için batch
```

### 8.2 E-Fatura Entegrasyonu

✅ **E-Fatura altyapısı mevcut.** `Fatura` entity'sinde:
- `EFaturaTipi` (EFatura/EArsiv)
- `EttnNo`, `GibKodu`, `GibOnayTarihi`, `GibDurumu`
- `XmlDosyaYolu`, `PdfDosyaYolu`

**FaturaService'te:**
- `ImportFromXmlAsync` — XML import + parse + cari eşleştirme
- `ImportFromExcelAsync` — Excel import

**Eksik:** GİB'e gönderim API çağrısı henüz yok.

---

## 9. Excel Çıktısı (6 Sayfa)

Tüm sayfalar **sadece PuantajKayit** üzerinden:

| Sayfa | İçerik | Kaynak |
|-------|--------|--------|
| 1 | Fatura Hazırlık Listesi (17 kolon) | `PuantajKayit` (Onaylandi) |
| 2 | Kurum Bazlı Özet | `PuantajKayit` → `KurumId` gruplama |
| 3 | Araç Bazlı Özet | `PuantajKayit` → `AracId` gruplama |
| 4 | Güzergah Bazlı Özet | `PuantajKayit` → `GuzergahId` gruplama |
| 5 | Tedarikçi Bazlı Ödenecek | `PuantajKayit` → `KaynakTipi == Tedarikci` |
| 6 | Fatura Eşleştirme Farkları | `PuantajKayit` ↔ `Fatura` karşılaştırma |

---

## 10. Kesilen/Gelen Fatura Eşleştirme

### 10.1 Mevcut Altyapı

Sistemde `PuantajEslestirmeService` var ancak **legacy FiloGunlukPuantaj** üzerinden çalışıyor. B1 için yeni eşleştirme gerekli.

### 10.2 Önerilen Eşleştirme Anahtarları (B1)

| Alan | Gelir (Kesilen Fatura) | Gider (Gelen Fatura) |
|------|----------------------|---------------------|
| Dönem | `Yil + Ay` | `Yil + Ay` |
| Muhatap | `FaturaKesiciCariId → Cari` | `OdemeYapilacakCariId → Cari` |
| Araç | `AracId → Plaka` | `AracId → Plaka` |
| Güzergah | `GuzergahId` | `GuzergahId` |
| Toplam Sefer | `Gun` | `Gun` |
| Tutar | `Alinacak` | `Odenecek` |
| KDV/20 | `GelirKdv20Tutari` | `GiderKdv20Tutari` |
| KDV/10 | `GelirKdv10Tutari` | `GiderKdv10Tutari` |
| Kesinti | `GelirKesinti` | `GiderKesinti` |
| Fatura ref | `GelirFaturaId` | `GiderFaturaId` |

### 10.3 Otomatik Eşleşme Kuralları

1. **Tam eşleşme**: Aynı dönem + aynı cari + aynı araç + aynı güzergah + tutar farkı ≤ %1
2. **Yakın eşleşme**: Aynı dönem + aynı cari + tutar farkı ≤ %5 → manuel onay
3. **Eşleşme yok**: Fatura var / Puantaj yok veya tersi

### 10.4 Manuel Eşleşme Gereken Durumlar
- Birden fazla PuantajKayit tek faturada birleşmişse
- Tek PuantajKayit birden fazla faturaya bölünmüşse
- Faturada puantaj dışı kalem varsa
- Dövizli fatura varsa

### 10.5 Fark Raporu

| Fark Tipi | Açıklama |
|-----------|----------|
| Kesilen var / Puantaj yok | Fatura kesilmiş ama puantajda karşılığı yok |
| Puantaj var / Kesilen yok | Puantajda sefer var ama fatura kesilmemiş |
| Tutar farkı | Fatura tutarı ≠ Puantaj tutarı |
| Gün/Sefer farkı | Fatura sefer sayısı ≠ Puantaj sefer sayısı |
| KDV farkı | KDV oranı/tutarı uyuşmuyor |
| Kesinti farkı | Kesinti tutarı uyuşmuyor |
| Plaka/Güzergah farkı | Aynı cari ama farklı araç/güzergah |
| Tam eşleşen | Tüm alanlar eşleşiyor ✅ |
| Manuel eşleşen | Kullanıcı manuel bağladı |

---

## 11. Riskler

| Risk | Seviye | Açıklama | Önlem |
|------|--------|----------|-------|
| Puantajı bozma | 🔴 YÜKSEK | Fatura servisinin PuantajEngineService akışına yazma yapması | **Sadece okuma.** Engine'e dokunulmaz |
| Duplicate fatura hazırlık | 🟡 ORTA | Aynı PuantajKayit'tan birden fazla hazırlık kaydı | Unique: `(HesapDonemiId, PuantajKayitId, FaturaYonu)` |
| Aynı araç farklı kurum | 🟡 ORTA | Araç günde birden fazla kurumda çalışabilir | Gruplamada beklenen davranış |
| Versiyonlu hesaplama dönemi | 🟢 DÜŞÜK | Engine her çalıştığında yeni versiyon oluşturur, eski Superseded | Sadece `Aktif` dönem okunur |
| Multi-tenant FirmaId | 🟢 DÜŞÜK | Global query filter otomatik korur | İlave önlem gerekmez |
| Gelen fatura otomatik eşleşme | 🟡 ORTA | XML import var ama UUID bazlı eşleştirme yok | `EttnNo` kullanılabilir |

---

## 12. Kesin Dokunulmayacak Alanlar ✅

- `PuantajEngineService.ProcessDonemAsync()` — OperasyonKaydi → PuantajKayit motoru
- `PuantajEngineJob` — Aylık Quartz job
- `OperasyonKaydiService` — Günlük operasyon girişi
- `PuantajHesapDonemi` — Versiyonlu hesaplama dönemi
- `PuantajDetay` — İzlenebilirlik junction tablosu
- Personel/Maaş/Banka/Muhasebe modülleri
- Migration, menü, route

---

## 13. Uygulama Faz Planı

### Faz 1: B1-Only Raporlama (Mevcut servisten B2 çıkarma)

**Dosya:** `PuantajFaturaRaporService.cs`

Yapılacaklar:
1. `BuildHakedisPuantajQuery()` metodunu kaldır
2. `ApplyYonFilterHp()` metodunu kaldır
3. `MapHakedisPuantajToDto()` mapper'ı kaldır
4. `GetOzetAsync` — sadece PK sorgusu
5. `GetSatirlarAsync` — PK sorgusu + mapper + doğrudan sayfalama
6. `GetAgacAsync` — PK sorgusu + BuildAgac
7. `GetCountAsync` — sadece pkCount
8. HP `.Include()` zincirlerini kaldır

**Etki:** ~80 satır azalma, merge mantığı yok, dedup riski yok.
**Risk:** Sıfır — salt okuma, engine'e dokunulmaz.

### Faz 2: Excel Export + Grup Şablonu

- 6 sayfa Excel (B1 verisiyle)
- `FaturaGrupSablonu` entity + CRUD
- Kullanıcı tercihi kaydetme

### Faz 3: Fatura Hazırlık Kayıtları

- `PuantajFaturaHazirlik` + `PuantajFaturaHazirlikSatir` entity'leri
- Manuel düzeltme (ek kalem, fiyat değişikliği, silme)
- Onay/kilit mekanizması

### Faz 4: Fatura Eşleştirme

- `PuantajFaturaEslestirmeService` (B1 ↔ Fatura)
- Otomatik eşleşme kuralları
- Manuel eşleşme UI
- `PuantajKayit.GelirFaturaId` / `GiderFaturaId` bağlantısı

### Faz 5: Eşleştirme Fark Raporu

- Fark raporu (Excel + Dashboard widget)
- Eksik/yanlış eşleşmeleri işaretleme
- Toplu düzeltme

---

## 14. Karar

**B1 (OperasyonKaydi → PuantajKayit → PuantajDetay) ana kaynak olarak yeterli mi?** → **EVET.**

- 17 Excel kolonunun tamamı B1'de eksiksiz karşılanıyor
- KDV %10 + %20 ayrımı B1'de mevcut (B2'de yoktu)
- PuantajDetay ile tam izlenebilirlik (OperasyonKaydi ↔ PuantajKayit)
- Versiyonlu hesaplama dönemi ile revizyon güvenli
- Engine otomatik (Quartz job), manuel müdahale gerekmez
- `PuantajFaturaRaporService` B1-only yapıldığında ~80 satır hafifliyor, merge/dedup riski sıfırlanıyor

**PuantajFaturaRaporService B2'den arındırıldığında:** Tek kaynak, tek mapper, tek sorgu. Hiçbir DTO veya API endpoint değişikliği gerekmez.

