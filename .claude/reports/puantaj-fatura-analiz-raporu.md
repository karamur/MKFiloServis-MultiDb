# Puantaj Sonrası Fatura Hazırlık ve Eşleştirme Analiz Raporu

**Tarih**: 2026-06-18
**Durum**: SADECE ANALİZ — kod değişikliği yok, migration yok

---

## 1. Mevcut Durum

### 1.1 Puantaj Verisi Hangi Tablolarda?

Sistemde **3 paralel puantaj domain'i** var:

| Domain | Ana Tablo | Detay Tablo | Amaç |
|--------|-----------|-------------|------|
| **A — Personel Puantaj** | `PersonelPuantaj` | `GunlukPuantaj` | İK/maaş bordrosu |
| **B1 — Operasyon Puantaj (Engine V1)** | `OperasyonKaydi` → `PuantajKayit` | `PuantajDetay` | Günlük operasyon → puantaj hesaplama |
| **B2 — Hakedis Puantaj** | `HakedisPuantaj` | `HakedisPuantajDetay` | Direkt hakedis girişi |
| **B3 — Filo Puantaj (Legacy)** | `FiloGunlukPuantaj` | — | Eski günlük puantaj |

**Fatura hazırlık için kullanılacak asıl kaynak**: `HakedisPuantaj` + `HakedisPuantajDetay`

### 1.2 Günlük Değerler Nerede?

- **PuantajKayit**: `Gun01..Gun31` (31 integer kolon) — her gün için 0/1/2 sefer değeri
- **HakedisPuantajDetay**: Normalleştirilmiş — her gün ayrı satır (`Gun` 1-31, `SeferSayisi` int, `FiyatCarpani` decimal)
- **OperasyonKaydi**: Her gün ayrı kayıt (`Tarih` DateTime)

### 1.3 Gelir/Gider Nerede?

| Kaynak | Entity | Alanlar |
|--------|--------|---------|
| Puantaj Excel | `HakedisPuantaj` | `GelirBirimFiyat`, `GiderBirimFiyat`, `GelirToplam`, `GiderToplam`, `GelirKdvTutari`, `KdvTutari`, `ToplamKesinti`, `TahsilEdilecekTutar`, `OdenecekTutar` |
| Engine V1 | `PuantajKayit` | `BirimGelir`, `ToplamGelir`, `GelirKdvOrani`, `GelirKesinti`, `Alinacak`, `BirimGider`, `ToplamGider`, `Odenecek` |
| Güzergah | `Guzergah` | `BirimFiyat` (gelir), `GiderFiyat` (gider) |
| Cari sefer ücreti | `CariSeferUcreti` | `SeferUcreti` |

### 1.4 Kurum/Araç/Güzergah İlişkisi Nerede?

- **HakedisPuantaj**: `GuzergahId` → Guzergah, `AracId` → Arac, `SoforId` → Sofor, `CariId` → Cari (tedarikçi)
- **PuantajKayit**: `KurumCariId` → Cari (kurum), `KurumId` → Kurum, `GuzergahId` → Guzergah, `AracId` → Arac, `SoforId` → Sofor, `IsverenFirmaId` → Firma
- **FiloGunlukPuantaj**: `KurumFirmaId` → Firma, `GuzergahId` → Guzergah, `AracId` → Arac, `SoforId` → Sofor

---

## 2. Excel Kolon Karşılıkları

| # | Excel Kolonu | Mevcut Kaynak | Entity/Tablo | Eksik mi? | Not |
|---|-------------|---------------|-------------|-----------|-----|
| 1 | S.NO | — | Hesaplama | ✅ | Sıra numarası, UI'da üretilir |
| 2 | GÜZERGAH | ✅ | `Guzergah.GuzergahAdi` | ✅ | |
| 3 | GELİR | ✅ | `HakedisPuantaj.GelirToplam` | ✅ | HakedisPuantaj.Hesapla() ile hesaplanır |
| 4 | GİDER | ✅ | `HakedisPuantaj.GiderToplam` | ✅ | |
| 5 | YÖN | ✅ | `HakedisPuantaj.YonTipi` (Sabah/Aksam/SabahAksam) | ✅ | |
| 6 | PLAKA | ✅ | `Arac.AktifPlaka` (computed) | ✅ | |
| 7 | tedarikçi/personel ŞOFÖR | ✅ | `Sofor.Ad + Sofor.Soyad` | ✅ | HakedisPuantaj.SoforId üzerinden |
| 8 | personel ŞOFÖR | ✅ | Aynı — `Sofor.Ad + Soyad` | ✅ | |
| 9 | tedarikçi/firma FATURA | ✅ | `TasimaTedarikci.Unvan` | ✅ | HakedisPuantaj.CariId → Cari → TasimaTedarikci |
| 10 | tedarikçi/firma TELEFON | ✅ | `TasimaTedarikci.Telefon` veya `Cari.Telefon` | ✅ | |
| 11 | 1-31 gün kolonları | ✅ | `HakedisPuantajDetay.SeferSayisi` (Gun bazlı) | ✅ | |
| 12 | GÜN | ✅ | `HakedisPuantaj.ToplamSefer` | ✅ | Toplam sefer sayısı = gün sayısı |
| 13 | TOPLAM | ✅ | `HakedisPuantaj.TahsilEdilecekTutar` veya `OdenecekTutar` | ✅ | Gelir/Gider yönüne göre |
| 14 | KDV/20 | ✅ | `HakedisPuantaj.GelirKdvTutari` | ✅ | %20 KDV |
| 15 | KDV/10 | ✅ | `PuantajKayit.GelirKdv10Tutari` | ⚠️ | HakedisPuantaj'da sadece tek KDV oranı var. 10/20 ayrımı için ek alan gerekebilir |
| 16 | KESİNTİ | ✅ | `HakedisPuantaj.ToplamKesinti` | ✅ | `HakedisKesinti` collection |
| 17 | ÖDENECEK | ✅ | `HakedisPuantaj.OdenecekTutar` (gider) / `TahsilEdilecekTutar` (gelir) | ✅ | |

**Sonuç**: 17 kolonun 16'sı mevcut sistemde karşılanıyor. **Sadece KDV/10 ayrımı** `HakedisPuantaj`'da tek oran olarak tutuluyor — çift KDV oranı (10+20) için `GelirKdv10Tutari` alanı eklenmeli.

---

## 3. Ağaç Yapısı Önerileri

### A) Kurum > Araç > Güzergah

**Avantaj**: Kurum bazlı fatura kesenler için doğal gruplama. Her kurumun araçları ve güzergahları net görünür.

**Dezavantaj**: Aynı araç farklı kurumlarda çalışıyorsa mükerrer görünür. Tedarikçi bazlı ödeme yapanlar için uygun değil.

**Uygun senaryo**: Tek kuruma hizmet veren özmal araçlar.

**Mevcut veride yapılabilir mi?**: ✅ Evet. `PuantajKayit.KurumId + AracId + GuzergahId` gruplaması.

### B) Kurum > Güzergah > Araç

**Avantaj**: Güzergah bazlı fiyatlandırma yapan kurumlar için ideal. Aynı güzergahta çalışan tüm araçlar bir arada.

**Dezavantaj**: Araç plakası üst seviyede görünmez. Plaka takibi zorlaşır.

**Uygun senaryo**: Sabit güzergah fiyatı olan kurumsal müşteriler.

**Mevcut veride yapılabilir mi?**: ✅ Evet.

### C) Fatura Cari > Araç > Güzergah ⭐ (ÖNERİLEN VARSAYILAN)

**Avantaj**: Fatura kesilecek cari bazında gruplama. Her cariye kesilecek fatura net görünür. Cari > Araç > Güzergah hiyerarşisi fatura kalemi oluşturmaya birebir uyar. Fatura başına toplam, KDV, kesinti kolay hesaplanır.

**Dezavantaj**: Tedarikçi gider tarafı için ayrı gruplama gerekir.

**Uygun senaryo**: Müşteriye kesilen satış faturası.

**Mevcut veride yapılabilir mi?**: ✅ Evet. `PuantajKayit.FaturaKesiciCariId + AracId + GuzergahId` veya `HakedisPuantaj.CariId + AracId + GuzergahId`.

### D) Tedarikçi > Araç > Güzergah

**Avantaj**: Tedarikçiden gelen fatura kontrolü için ideal. Her tedarikçinin araç ve güzergah detayı görünür.

**Dezavantaj**: Sadece gider tarafı için geçerli, gelir tarafını kapsamaz.

**Uygun senaryo**: Tedarikçi performans değerlendirmesi, gelen fatura kontrolü.

**Mevcut veride yapılabilir mi?**: ✅ Evet. `Arac.TasimaTedarikciId + GuzergahId` veya `HakedisPuantaj.CariId` (tedarikçi tipindeki cari).

### E) Kurum > Tedarikçi > Araç > Güzergah

**Avantaj**: En kapsamlı hiyerarşi. Kurum → tedarikçi → araç → güzergah zinciri tam görünür.

**Dezavantaj**: 4 seviye derinlik UI'da karmaşık olabilir. Veri yoksa boş seviyeler oluşur.

**Uygun senaryo**: Karmaşık operasyonlar (hem özmal hem tedarikçi araçları olan kurumlar).

**Mevcut veride yapılabilir mi?**: ⚠️ Kısmen. `PuantajKayit`'ta hem `KurumId` hem `IsverenFirmaId` var. Ama Araç-Tedarikçi bağı `Arac.TasimaTedarikciId` üzerinden.

### Önerilen Varsayılan Yapı

**Fatura Cari > Araç > Güzergah** (gelir tarafı) + **Tedarikçi > Araç > Güzergah** (gider tarafı) çift ağaç yapısı.

---

## 4. Tüm Kurumlar Listeleme

### Mevcut Ekran Tek Kurum Mu?

- **PuantajHesaplama**: `KurumId` parametresi alır, tek kurum işler
- **PuantajExcelGrid**: FirmaId bazlı, kurum filtresi var
- **HakedisPuantaj**: CariId bazlı, kurum filtresi opsiyonel

### Tüm Kurumlar Seçeneği Nasıl Olmalı?

`KurumId = null` → tüm kurumlar. Mevcut `PuantajKayit` ve `HakedisPuantaj` servisleri `KurumId` nullable zaten. Filtre kaldırıldığında tüm veri gelir.

### FirmaId/Global Filter Riski

- `IFirmaTenant` tüm entity'lerde `FirmaId` zorunlu → multi-tenant koruması otomatik
- Global query filter `FirmaId == aktifFirma` her sorguya eklenir
- Tüm kurumlar listelense bile **sadece aktif firmanın verisi** görünür ✅

### Yetki Riski

- Tüm kurumları görebilmek için `Yetkiler.BelgeUyarilariOku` veya `*` yetkisi gerekir
- Mevcut yetki sistemi değişmez ✅

---

## 5. Fatura Hazırlık Modeli Önerisi (TASLAK — Kod yok)

### Ana Kayıt
```
PuantajFaturaHazirlik
  - Id, FirmaId (tenant)
  - Yil, Ay
  - KurumId? (null = tüm kurumlar)
  - AgacYapisi (enum: KurumAracGuzergah, CariAracGuzergah, TedarikciAracGuzergah, ...)
  - FaturaYonu (Gelir/Gider)
  - Durum (Taslak, Onaylandi, Faturalasti)
  - ToplamTutar, ToplamKdv, ToplamKesinti, NetTutar
  - CreatedAt, CreatedBy
```

### Satır Kayıt
```
PuantajFaturaHazirlikSatir
  - HazirlikId (FK)
  - Seviye1..4 (ağaç seviyeleri — string label)
  - KurumId?, CariId?, TedarikciId?, AracId?, GuzergahId?
  - Plaka, SoforAdi, Telefon (denormalize)
  - Gun01..Gun31 (int) veya GunDegerleri (jsonb)
  - ToplamGun, ToplamSefer
  - BirimGelir, ToplamGelir, BirimGider, ToplamGider
  - KdvOrani, KdvTutar
  - Kesinti, OdenecekTutar
  - FaturaId? (bağlandıysa)
```

### Grup Şablonu
```
FaturaGrupSablonu
  - Id, FirmaId
  - Ad (string)
  - AgacYapisi (enum)
  - VarsayilanMi (bool)
  - KullaniciId? (kullanıcı bazlı ise)
```

### Manuel Düzeltme İhtiyacı

**Evet gerekli.** Puantajdan gelen veri her zaman faturaya birebir uymaz:
- Ek kalem ekleme (özel servis, bekleme ücreti)
- Fiyat düzeltme (anlaşma değişikliği)
- Kalem silme (iptal edilen sefer)

Öneri: Hazırlık satırında `ManuelDuzeltmeMi` (bool) + `OrijinalTutar` (decimal) alanı.

### Onay/Kilit Mekanizması

Puantaj onaylandıktan sonra fatura hazırlık **okuma amaçlı** olmalı. Fatura kesildikten sonra hazırlık kaydı `Faturalasti` durumuna geçmeli ve kilitlenmeli.

---

## 6. Kesilen/Gelen Fatura Eşleştirme

### Mevcut Eşleştirme Altyapısı

Sistemde **zaten 3 seviye eşleştirme var**:

1. **Cari Mutabakat** (`PuantajEslestirmeService.GetCariMutabakatAsync`)
   - `FiloGunlukPuantaj.TahakkukEdenKurumUcreti` vs `Fatura` (Giden)
   - Fark raporu: tahakkuk eden - kesilen

2. **Tedarikçi Mutabakat** (`PuantajEslestirmeService.GetTasimaTedarikciMutabakatAsync`)
   - `FiloGunlukPuantaj.TahakkukEdenTaseronUcreti` vs `Fatura` (Gelen)
   - Fark raporu: tahakkuk eden - gelen

3. **Firmalar Arası Fatura Eşleştirme** (`FaturaService.FaturalariEslestirAsync`)
   - `Fatura.EslesenFaturaId` bağı ile çapraz şirket faturaları

### Eksik Olan

Mevcut mutabakat **eski FiloGunlukPuantaj** üzerinden çalışıyor. **Yeni HakedisPuantaj/PuantajKayit** için eşleştirme yok.

### Önerilen Eşleştirme Anahtarları

| Alan | Gelir Eşleştirme (Kesilen) | Gider Eşleştirme (Gelen) |
|------|---------------------------|-------------------------|
| Dönem | Yil + Ay | Yil + Ay |
| Kurum/Cari | `FaturaKesiciCariId` | `OdemeYapilacakCariId` |
| Araç | `AracId` → Plaka | `AracId` → Plaka |
| Güzergah | `GuzergahId` | `GuzergahId` |
| Gün/Sefer | `Gun` / `ToplamSefer` | `Gun` / `ToplamSefer` |
| Tutar | `ToplamGelir` / `Alinacak` | `ToplamGider` / `Odenecek` |
| KDV | `GelirKdvTutari` | `GiderKdvTutari` |
| Kesinti | `GelirKesinti` | `GiderKesinti` |

### Otomatik Eşleşme Kuralları

1. **Tam eşleşme**: Aynı dönem + aynı cari/tedarikçi + aynı araç + aynı güzergah + tutar farkı ≤ %1
2. **Yakın eşleşme**: Aynı dönem + aynı cari/tedarikçi + tutar farkı ≤ %5 → manuel onay
3. **Eşleşme yok**: Kesilen fatura var / puantaj kaydı yok veya tersi

### Manuel Eşleşme İhtiyacı

**Evet.** Aşağıdaki durumlarda manuel müdahale gerekir:
- Birden fazla puantaj kaydı tek faturada birleşmişse
- Tek puantaj kaydı birden fazla faturaya bölünmüşse
- Faturada puantaj dışı kalemler varsa
- Dövizli fatura varsa

### Fark Raporu

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

## 7. E-Fatura Entegrasyonu

### Mevcut Durum

**✅ E-Fatura altyapısı MEVCUT.**

**Fatura entity'sinde**:
- `EFaturaTipi` (EFatura/EArsiv)
- `EttnNo` (ETTN/UUID)
- `GibKodu`, `GibOnayTarihi`, `GibDurumu` (Bekliyor → Gonderildi → Kabul/Red)
- `GibGonderimTarihi`, `GibDurumMesaji`
- `XmlDosyaYolu`, `PdfDosyaYolu`

**FaturaService'te**:
- `ImportFromExcelAsync` — Excel'den e-fatura import
- `ImportFromXmlAsync` — XML dosyasından e-fatura import
- `ImportFromXmlWithPdfAsync` — XML + PDF import
- XML parse → `Fatura` entity'sine dönüştürme (Cari eşleştirme dahil)

**Ama**: GİB'e gönderim kodu (entegratör API çağrısı) **henüz yok**. Sadece import/export ve dosya yönetimi var.

---

## 8. Excel Çıktı Önerisi

### Sayfa 1: Fatura Hazırlık Listesi

Kolonlar: S.NO, GÜZERGAH, GELİR, GİDER, YÖN, PLAKA, ŞOFÖR, FATURA KESİLECEK CARİ, TELEFON, 1-31 GÜN, TOPLAM GÜN, TOPLAM TUTAR, KDV/20, KDV/10, KESİNTİ, ÖDENECEK/TAHSİL EDİLECEK

Kaynak: `HakedisPuantaj` + `HakedisPuantajDetay` + `Guzergah` + `Arac` + `Sofor` + `Cari`

### Sayfa 2: Kurum Bazlı Özet

Kolonlar: KURUM, TOPLAM SEFER, TOPLAM GELİR, TOPLAM GİDER, KDV, KESİNTİ, NET, KAR/ZARAR

Kaynak: `HakedisPuantaj` → `KurumId` gruplaması (veya `PuantajKayit.KurumCariId`)

### Sayfa 3: Araç Bazlı Özet

Kolonlar: PLAKA, MARKA/MODEL, SAHİPLİK, TOPLAM SEFER, GELİR, GİDER, KAR/ZARAR

Kaynak: `HakedisPuantaj` → `AracId` gruplaması

### Sayfa 4: Güzergah Bazlı Özet

Kolonlar: GÜZERGAH, TOPLAM SEFER, GELİR, GİDER, ORTALAMA MALİYET, KARLILIK %

Kaynak: `HakedisRaporService.GetGuzergahRaporuAsync()` zaten mevcut

### Sayfa 5: Tedarikçi Bazlı Ödenecek

Kolonlar: TEDARİKÇİ, TOPLAM SEFER, TOPLAM GİDER, KDV, KESİNTİ, ÖDENECEK

Kaynak: `HakedisPuantaj` → `CariId` (Tedarikçi tipi) gruplaması

### Sayfa 6: Fatura Eşleştirme Farkları

Kolonlar: DURUM (Eşleşti/Eşleşmedi/Farklı), PUANTAJ TUTAR, FATURA TUTAR, FARK, AÇIKLAMA

---

## 9. Riskler

| Risk | Seviye | Açıklama | Önlem |
|------|--------|----------|-------|
| Puantajı bozma | 🔴 YÜKSEK | Fatura hazırlık kodunun puantaj kaydetme akışına müdahale etmesi | **Sadece okuma**. Puantaj servislerine dokunulmaz |
| Duplicate satır | 🟡 ORTA | Aynı puantajdan birden fazla fatura hazırlık kaydı oluşması | Unique constraint: `(HesapDonemiId, PuantajKayitId, FaturaYonu)` |
| Aynı araç farklı kurum | 🟡 ORTA | Araç günde birden fazla kurumda çalışabilir | Gruplama seviyesinde araç plakası tekrar eder — bu beklenen davranış |
| KDV oranı satır/cari bazlı | 🟡 ORTA | %10 ve %20 aynı faturada olabilir | `HakedisPuantaj`'da çift KDV alanı yok → `GelirKdv10Tutari` eklenmeli |
| Kesinti otomatik/manuel | 🟢 DÜŞÜK | Mevcut sistemde `HakedisKesinti` manuel giriliyor | Faturaya aktarımda kesinti kalemi olarak eklenir |
| Multi-tenant FirmaId | 🟢 DÜŞÜK | Global query filter otomatik korur | İlave önlem gerekmez |
| Gelen fatura otomatik eşleşme | 🟡 ORTA | XML import var ama UUID bazlı eşleştirme yok | `EttnNo` alanı kullanılarak otomatik eşleşme yapılabilir |

---

## 10. Kesin Dokunulmayacak Alanlar ✅

- Mevcut Kurum Puantaj kaydetme (`KurumPuantajService`, `OperasyonKaydiService`)
- Şablon oluşturma (`PuantajExcelService`)
- Operasyon girişi (`OperasyonGiris.razor`)
- Araç belgeleri (`AracEvrak`, `AracForm`)
- Personel belgeleri (`OzlukEvrakChecklist`, `EvrakYonetimi`)
- Maaş (`PersonelPuantaj`, `MaasYonetimi`, `MaasHesaplamaEngine`)
- Banka ödeme (`BankaKasaHareket`, `BankaOdemeListesi`)
- Muhasebe fişi üretimi (`MuhasebeService`, `MuhasebeSnapshotService`)
- E-fatura gönderim/çekim kodu
- Migration
- Menü/route/sayfa

---

## 11. Uygulama Faz Önerisi

### Faz 1: Sadece Okuma Amaçlı Rapor (Güvenli)

- Yeni `PuantajFaturaRaporService` (readonly)
- `HakedisPuantaj` + `HakedisPuantajDetay`'dan okuma
- Ağaç gruplama seçeneği (UI dropdown)
- Sayfalama/filtreleme
- Puantaja **hiç dokunmaz**

### Faz 2: Excel Export + Gruplama Kaydetme

- Excel çıktı (6 sayfa)
- Grup şablonu kaydetme (`FaturaGrupSablonu`)
- Kullanıcı tercihi saklama

### Faz 3: Fatura Hazırlık Kayıtları

- `PuantajFaturaHazirlik` + `PuantajFaturaHazirlikSatir` tabloları
- Manuel düzeltme imkanı
- Onay/kilit mekanizması

### Faz 4: Kesilen/Gelen Fatura Eşleştirme

- Yeni `PuantajFaturaEslestirmeService`
- Otomatik eşleşme kuralları
- Manuel eşleşme UI'ı
- `HakedisPuantaj` ↔ `Fatura` bağlantısı

### Faz 5: Eşleştirme Fark Raporu

- Fark raporu (Excel + Dashboard widget)
- Eksik/yanlış eşleşmeleri işaretleme
- Toplu düzeltme

---

## 12. Kod Değişikliği Yapıldı mı?

**Hayır.** Hiçbir dosya değiştirilmedi. Migration açılmadı.

---

## 13. Karar

**Mevcut puantaj bozulmadan bu yapı kurulabilir mi?** → **EVET.**

Puantaj verisi (HakedisPuantaj + PuantajKayit) readonly olarak okunur. Yeni tablolar (FaturaHazirlik, Eslestirme) tamamen ayrıdır. Puantaj servislerine dokunulmaz.

**İlk faz için en güvenli adım**: Readonly rapor servisi + UI sayfası. Puantaja dokunmadan, sadece mevcut veriden okuyarak fatura hazırlık listesi oluşturma.
