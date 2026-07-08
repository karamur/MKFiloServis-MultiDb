# MKFiloServis — Operasyonel Puantaj Devam Dosyası

> **Amaç:** Bu dosya, operasyonel puantaj sayfası çalışmasına kaldığımız yerden devam edebilmek için
> tüm bağlamı, mimari kararları ve açık noktaları tek belgede saklar.
> Son güncelleme: Temmuz 2026

---

## 1. İŞLETME MODELİ (Değişmez Referans)

Firma; **kurumsal müşterilere personel taşımacılığı / personel servis hizmeti** vermektedir.

### Araç Sahiplik Tipleri

| Tip | Enum Değeri | Açıklama | Maliyet Sahibi | Fatura |
|-----|-------------|----------|----------------|--------|
| **Özmal** | `AracSahiplikTipi.Ozmal (1)` | Bize ait araç, bize ait şoför | Biz (yakıt + bakım + maaş + amortisman) | Kuruma 1 gelir fatura |
| **Kiralık** | `AracSahiplikTipi.Kiralik (2)` | C-plaka uzun dönem kira, bize ait şoför | Biz (plaka kirası + yakıt + maaş) | Kuruma 1 gelir fatura |
| **Tedarikçi** | `AracSahiplikTipi.Tedarikci (5)` | Taşeronun aracı + şoförü | Tedarikçi | Kuruma 1 gelir fatura + Tedarikçiye 1 gider fatura |

### İş Hiyerarşisi

```
Kurum (Müşteri Firma/Kurum)
  └─ Güzergah (örn: Organize Sanayi ↔ Şehir Merkezi)
       └─ ServisKontrat (Güzergah × Araç × Şoför × Tedarikçi × birim fiyat)
            └─ FiloGunlukPuantaj (her güne 1 kayıt — operasyonun kalbi)
                 └─ Hakediş (aylık toplama — kurum / tedarikçi / araç bazında)
                      └─ Fatura (gelir veya gider)
```

---

## 2. MEVCUT KOD DURUMU (Temmuz 2026 itibarı)

### 2.1 Entity Katmanı — Durum

| Entity | Dosya | Durum | Notlar |
|--------|-------|-------|--------|
| `FiloGunlukPuantaj` | `FiloKomisyonPuantaj.cs` | ✅ Tam | `ServisTuru`, `SeferSayisi`, `TahakkukEdenKurumUcreti`, `TahakkukEdenTaseronUcreti`, `MaliyetOzmalKiralik`, `TaksiKullanildiMi` var |
| `ServisKontrat` | `ServisKontrat.cs` | ✅ Tam | `ServisKontratTip` (Ozmal/Kiralik/Tedarikci), `GelirBirimFiyat`, `GiderBirimFiyat` |
| `Hakedis` + `HakedisDetay` | Shared/Entities | ✅ Tam | `HakedisTipi` (Kurum/Tedarikci/Arac), `HakedisDurum`, migration var |
| `HakedisPuantaj` | `HakedisPuantaj.cs` | ✅ Var | ESKİ hakediş modeli — yeni `Hakedis` ile AYRI (bkz. §3.1 Sorun) |
| `AracMaliyetSnapshot` | Shared/Entities | ✅ Tam | Migration eklendi (önceki oturum), servis çalışıyor |
| `PersonelPuantaj` | `PersonelPuantaj.cs` | ✅ Tam | Maaş bazlı — operasyonel puantajdan AYRI |
| `OperasyonKaydi` | `OperasyonKaydi.cs` | ✅ Var | PuantajEngine ile oluşturulan kayıt — `FiloGunlukPuantaj` ile FARKLI tablo |
| `PuantajKayit` | `PuantajKayit.cs` | ✅ Var | Engine çıktısı |
| `PuantajAnomali` | `PuantajAnomali.cs` | ✅ Var | Job ile tespit ediliyor |

### 2.2 Servis Katmanı — Durum

| Servis | Implementasyon | Durum | Notlar |
|--------|---------------|-------|--------|
| `OperasyonelHakedisService` | `OperasyonelHakedisService.cs` | ✅ **ÇALIŞIYOR** | 632 satır, `FiloGunlukPuantaj` → `Hakedis` üretimi tam |
| `HakedisPuantajService` | `HakedisPuantajService.cs` | ✅ Var | 505 satır, eski `HakedisPuantaj` tablosu için — yeni sistemle çakışıyor |
| `HakedisRaporService` | `HakedisRaporService.cs` | ✅ Var | Rapor üretimi |
| `HakedisMuhasebeService` | `HakedisMuhasebeService.cs` | ✅ Var | Muhasebe entegrasyonu |
| `AracMaliyetService` | `AracMaliyetService.cs` | ✅ **ÇALIŞIYOR** | `FullpetFaturaDagitAsync` dahil |
| `PuantajEngineService` | `PuantajEngineService.cs` | ✅ Var | `OperasyonKaydi` → `PuantajKayit` dönüşümü |
| `PuantajWorkflowService` | `PuantajWorkflowService.cs` | ✅ Var | Finans/Muhasebe/Kilit onay zinciri |
| `ServisKontratService` | `ServisKontratService.cs` | ✅ Var | Kontrat CRUD + birim fiyat |
| `FiloKomisyonService` | `FiloKomisyonService.cs` | ✅ Var | `FiloGunlukPuantaj` CRUD |

### 2.3 UI Sayfaları — Durum

| Sayfa | Rota | Satır | Durum | Notlar |
|-------|------|-------|-------|--------|
| `FiloGunlukPuantajPage.razor` | `/operasyon/filo-gunluk-puantaj` | 1887 | ❌ Silindi | FAZ 0 Adım 0.1 kapsamında kaldırıldı |
| `FiloHakedisPage.razor` | `/filo-hakedis` | 568 | ❌ Silindi | FAZ 0 Adım 0.1 kapsamında kaldırıldı |
| `FiloPuantaj.razor` | `/filo/puantaj` | ? | ❌ Silindi | FAZ 0 Adım 0.1 kapsamında kaldırıldı |
| `PuantajDetay.razor` | `/servis-operasyon/puantaj/{id}` | 681 | ✅ Aktif | Kontrat bazlı detay, onay, fatura taslağı |
| `KontratList.razor` | `/servis-operasyon/kontratlar` | ? | ✅ Aktif | ServisKontrat listesi |
| `KontratForm.razor` | `/servis-operasyon/kontrat-form` | ? | ✅ Aktif | Kontrat oluşturma/düzenleme |
| `AracMaliyetSnapshotPage.razor` | `/araclar/maliyet-snapshot` | ? | ✅ Aktif | Önceki oturumda eklendi |
| `HaftalikPuantajMatrisi.razor` | Raporlar | ? | ✅ Var | Haftalık matris raporu |

---

## 3. KRİTİK SORUNLAR VE ÇÖZÜM DURUMLARI

### 3.1 🔴 ÜÇ PARALEL HAKEDİŞ SİSTEMİ ÇAKIŞMASI

Projede **üç ayrı hakediş yolu** var, bunlar tam entegre değil:

```
YOL A — Klasik kontrat bazlı:
ServisKontrat → ServisPuantaj → HakedisPuantaj (tablo: HakedisPuantajlar)
Servis: HakedisPuantajService (505 satır)
Sayfa: HİÇBİR sayfada kullanılmıyor — Program.cs'de kayıtlı ama inject yok

YOL B — Operasyonel (YENİ, DOĞRU YOL):
FiloGunlukPuantaj → Hakedis + HakedisDetay (tablo: Hakedisler)
Servis: OperasyonelHakedisService (632 satır) ✅ ÇALIŞIYOR
Kullanıldığı yerler: KarlilikRaporu, FiloKpiDashboard, AylikKapanisAsistani,
                    BildirimMerkezi, KomutaMerkezi, OperasyonelOzetBandi

YOL C — PuantajEngine:
OperasyonKaydi → PuantajKayit → (hakediş yok, sadece sync)
```

**Gerçek durum (Temmuz 2026 analizi):**
`FiloHakedisPage.razor` — adı "Hakediş" ama aslında `FiloGuzergahEslestirme`
listesini gösteriyor. Hakediş üretmiyor, sadece eşleştirme tablosu + puantaj
sayfasına link. Ne `IHakedisPuantajService` ne `IOperasyonelHakedisService`
inject edilmiş.

**Sonuç:** Yol A tamamen ölü kod. Yol B rapor sayfalarında aktif kullanılıyor.
Bir hakediş yönetim sayfası sıfırdan yazılmalı.

### 3.2 🔴 OPERASYONEL PUANTAJ SAYFASI YETERSİZ

`FiloGunlukPuantajPage.razor` (1887 satır) var ama:
- Tek tablo görünümü var, özmal/kiralık/tedarikçi ayrımı UI'da görünmüyor
- Kontrat şablonundan otomatik satır üretme akışı eksik (kontrat → günlük satır)
- Tedarikçi satırı için TahakkukEdenTaseronUcreti otomatik hesaplanmıyor
- `ServisKontrat.GiderBirimFiyat` → `TahakkukEdenTaseronUcreti` bağlantısı yok
- Sayfa çok büyük, bölümlere ayrılmalı (component bazlı)

### 3.3 🟡 HAKEDİŞ SAYFASI YENİ SİSTEME BAĞLI DEĞİL

`FiloHakedisPage.razor`:
- `IOperasyonelHakedisService` inject edilmemiş
- "Hakediş Üret" butonu yok (Kurum / Tedarikçi / Araç bazında)
- Onay → Faturaya Dönüştür akışı eksik
- PDF çıktısı yok

### 3.4 🟡 TAHAKKUK OTOMATİK HESAPLANMIYOR

`FiloGunlukPuantaj` kaydedilirken:
- `TahakkukEdenKurumUcreti` = `ServisKontrat.GelirBirimFiyat` × `SeferSayisi` → **MANUEL** giriliyor
- `TahakkukEdenTaseronUcreti` = `ServisKontrat.GiderBirimFiyat` × `SeferSayisi` → **MANUEL** giriliyor, tedarikçi satırlarında hiç doldurulmuyor

**Çözüm:** Kayıt kaydedilirken ServisKontrat'tan birim fiyat çekilip otomatik hesaplanmalı.

### 3.5 🟡 MALİYET SNAPSHOT → PUANTAJ BAĞLANTISI ZAYİF

`FiloGunlukPuantaj.MaliyetOzmalKiralik` alanı var (`decimal?`) ama:
- Hiçbir yerde otomatik dolduruluyor
- `AracMaliyetSnapshot.SeferBasiMaliyet` değeri buraya aktarılmıyor
- Bu yüzden Araç bazlı Hakediş üretildiğinde (`HakedisTipi.Arac`) değer sıfır çıkıyor

### 3.6 🟢 PERSONel PUANTAJ ENTEGRASYONU (DÜŞÜK ÖNCELİK)

`PersonelPuantaj` (maaş bazlı, aylık) → `AracMaliyetSnapshot.SoforMaasOran` aktarımı yok.
Bu eksiklik maliyet snapshot'unun şoför maaş payını içermemesine neden oluyor.

---

### 3.7 🔴 UI SAYFALARI MENÜDE YOK, DOSYADA DURUYOR

NavMenu incelemesi sonucu:
- `FiloGunlukPuantajPage.razor` → navmenüde **link yok**
- `FiloHakedisPage.razor` → navmenüde **link yok**
- `FiloPuantaj.razor` → navmenüde **link yok**

Kullanıcı bunlara doğrudan URL yazarak erişebiliyor ama menüde görünmüyor.
Sayfalar build'e giriyor, gereksiz derleme yükü oluşturuyor.

**Karar (Temmuz 2026):** Seçenek 3 — Tam Kaldır uygulanacak.
Entity / Servis / Migration / DB tablolarına DOKUNULMAYACAK.
Sadece 3 UI sayfası dosyası silinecek, ileride sıfırdan temiz yazılacak.

Silinecek dosyalar:
- `MKFiloServis.Web/Components/Pages/Filo/FiloGunlukPuantajPage.razor`
- `MKFiloServis.Web/Components/Pages/Filo/FiloHakedisPage.razor`
- `MKFiloServis.Web/Components/Pages/Filo/FiloPuantaj.razor`

NavMenu'de `menu.filoservis` yetki tanımındaki `filo-gunluk-puantaj` string'i
de temizlenecek (sadece string, sayfanın kendisi değil).

> ⚠️ Bu işlem yapılmadı. Bir sonraki oturumda başlanacak.

---

## 4. MİMARİ KARAR KAYITLARI

| # | Karar | Gerekçe | Tarih |
|---|-------|---------|-------|
| K1 | `FiloGunlukPuantaj` tek kayıt noktası olacak | Çift kayıt riskini ortadan kaldırır | Tasarım dokümanı |
| K2 | `Hakedis` tablosu `HakedisPuantaj` tablosunun yerini alır | Daha temiz; Kurum/Tedarikçi/Araç tipi tek yapıda | Tasarım dokümanı |
| K3 | `OperasyonelHakedisService` geçerli hakediş servisi | 632 satır, tam uygulanmış, `FiloGunlukPuantaj` → `Hakedis` dönüşümü çalışıyor | Temmuz 2026 analizi |
| K4 | `AracMaliyetSnapshot` korunacak ve Araç hakediş için kaynak olacak | `FullpetFaturaDagitAsync` dahil tam çalışıyor | Önceki oturum |
| K5 | Yeni alanlar `Required` yapılmayacak | Eski kayıtlar geçerli kalsın | Tasarım dokümanı |
| K6 | `ServisKontratTip` ile `AracSahiplikTipi` farkı korunacak | Kontrat tipi ≠ araç tipi (aynı araç farklı kontratta farklı tip olabilir) | Temmuz 2026 analizi |
| K7 | Mevcut 3 puantaj UI sayfası silinecek, sıfırdan yazılacak | Sayfalar menüde yok, 1887+568+? satır bakımsız kod, temiz başlamak daha iyi | Temmuz 2026 analizi |
| K8 | `HakedisPuantajService` ve `HakedisPuantaj` tablosu ölü kod | Hiçbir sayfada inject edilmemiş, Yol B (`OperasyonelHakedisService`) aktif kullanılıyor | Temmuz 2026 analizi |

---

## 5. ÖNERİLEN UYGULAMA SIRALAMASI

### FAZ 0 — Temizlik (DEVAM EDİYOR)

**Hedef:** Eski UI sayfalarını sil, build temiz kalsın, yeni sayfa için alan aç.

#### Adım 0.1 — 3 UI sayfasını sil ✅ TAMAMLANDI
- `FiloGunlukPuantajPage.razor` silindi
- `FiloHakedisPage.razor` silindi
- `FiloPuantaj.razor` silindi

#### Adım 0.2 — Kalan referansları temizle ⏳ YAPILACAK
- `GuzergahForm.razor` satır ~1278 → `NavigateTo("/filo-gunluk-puantaj")` kaldırılacak
- `FiloKpiDashboard.razor` satır ~270 → `filo/puantaj` linki kaldırılacak
- `NavMenu.razor` satır ~1340 → `"filo-gunluk-puantaj"` string'i kaldırılacak

#### Adım 0.3 — Build doğrula ⏳ YAPILACAK
- Hedef: 0 warning, 0 error

> ⚠️ Adım 0.1 tamamlandı. Adım 0.2–0.3 bir sonraki oturumda yapılacak.

---

### FAZ 1 — Yeni Operasyonel Puantaj Sayfası (SIFIRDAN)

**Hedef:** Operatör günde 5 dakikada tüm güzergah satırlarını işleyebilsin.

#### Adım 1.1 — Kontrat → Günlük Satır Otomatik Üretimi
- Sayfa açıldığında seçilen tarih için aktif `ServisKontrat`'lar listelenir
- "Bugün için satırları oluştur" butonu ile `FiloGunlukPuantaj` kayıtları taslak olarak oluşturulur
- Her satır için kontrat tipine göre `TahakkukEdenKurumUcreti` otomatik hesaplanır

#### Adım 1.2 — Tahakkuk Otomatik Hesaplama
```csharp
// SaveAsync içinde:
kayit.TahakkukEdenKurumUcreti = kontrat.GelirBirimFiyat * kayit.SeferSayisi * kayit.PuantajCarpani;
if (kontrat.Tip == ServisKontratTip.Tedarikci)
    kayit.TahakkukEdenTaseronUcreti = kontrat.GiderBirimFiyat * kayit.SeferSayisi * kayit.PuantajCarpani;
```

#### Adım 1.3 — UI Araç Tipi Renk Kodlaması
- Özmal satırlar: `table-success` (yeşil)
- Kiralık satırlar: `table-info` (mavi)
- Tedarikçi satırlar: `table-warning` (sarı) + ekstra Tedarikçi sütunu

### FAZ 2 — Hakediş Sayfası Yeni Sisteme Bağlama (SONRA)

**Hedef:** `FiloHakedisPage.razor` → `IOperasyonelHakedisService` kullanacak.

#### Adım 2.1 — Sayfa Inject Güncelleme
```razor
@inject IOperasyonelHakedisService HakedisService
```

#### Adım 2.2 — "Hakediş Üret" Butonu
- Dönem (Yıl/Ay) + Tip (Kurum/Tedarikçi/Araç) seçimi
- `OperasyonelHakedisService.KurumHakedisiUretAsync(...)` çağrısı

#### Adım 2.3 — Onay ve Faturalama Akışı
```
Taslak → Onayla → Faturaya Dönüştür
                     ↓
              Fatura.HakedisId = hakedis.Id
              Fatura.FaturaYonu = Gelir (Kurum) | Gider (Tedarikçi)
```

### FAZ 3 — Maliyet → Hakediş Bağlantısı (EN SON)

#### Adım 3.1 — MaliyetOzmalKiralik Otomatik Doldurma
Kayıt kaydedilirken:
```csharp
var snapshot = await maliyetService.GetSnapshotlarAsync(aracId, tarih.Year, tarih.Month);
if (snapshot.FirstOrDefault() is { } s && s.ToplamSefer > 0)
    kayit.MaliyetOzmalKiralik = s.SeferBasiMaliyet;
```

#### Adım 3.2 — Kar/Zarar Raporu
- `TahakkukEdenKurumUcreti - MaliyetOzmalKiralik` = Günlük Kar/Zarar
- Aylık toplama → `FiloKpiDashboard` veya ayrı sayfa

---

## 6. DOSYA REFERANSLARİ (Kritik Dosyalar)

### Entity'ler
- `MKFiloServis.Shared/Entities/FiloKomisyonPuantaj.cs` → `FiloGunlukPuantaj` sınıfı (satır 68)
- `MKFiloServis.Shared/Entities/ServisKontrat.cs` → `ServisKontratTip`, `ServisKontrat`
- `MKFiloServis.Shared/Entities/HakedisPuantaj.cs` → ESKİ hakediş modeli
- `MKFiloServis.Shared/Entities/Hakedis.cs` → YENİ hakediş modeli (kullanılacak)

### Servisler
- `MKFiloServis.Web/Services/OperasyonelHakedisService.cs` → YENİ, ÇALIŞIYOR (632 satır)
- `MKFiloServis.Web/Services/HakedisPuantajService.cs` → ESKİ (505 satır, göz ardı edilecek)
- `MKFiloServis.Web/Services/FiloKomisyonService.cs` → `FiloGunlukPuantaj` CRUD
- `MKFiloServis.Web/Services/AracMaliyetService.cs` → Snapshot ve Fullpet dağıtım
- `MKFiloServis.Web/Services/ServisKontratService.cs` → Kontrat ve birim fiyat

### Sayfalar
- `MKFiloServis.Web/Components/Pages/Filo/FiloGunlukPuantajPage.razor` → ❌ FAZ 0 Adım 0.1'de silindi
- `MKFiloServis.Web/Components/Pages/Filo/FiloHakedisPage.razor` → ❌ FAZ 0 Adım 0.1'de silindi
- `MKFiloServis.Web/Components/Pages/Filo/FiloPuantaj.razor` → ❌ FAZ 0 Adım 0.1'de silindi
- `MKFiloServis.Web/Components/Pages/ServisOperasyon/PuantajDetay.razor` → Detay/onay/fatura (aktif)

### Dokümanlar
- `MKFiloServis.Web/Docs/Puantaj-Hakedis-Tasarim.md` → Orijinal tasarım dokümanı
- `MKFiloServis.Web/Docs/OPERASYONEL-PUANTAJ-DEVAM-DOSYASI.md` → **BU DOSYA**

---

## 7. DEVAM EDİLDİĞİNDE YAPILACAK İLK ADIM

"Devam edelim" dediğinde şu konudan başlayacağız:

> **FAZ 0 devam — Kalan adımlar:**
> 1. ✅ `FiloGunlukPuantajPage.razor` silindi
> 2. ✅ `FiloHakedisPage.razor` silindi
> 3. ✅ `FiloPuantaj.razor` silindi
> 4. ✅ `GuzergahForm.razor` NavigateTo referansı kaldırıldı
> 5. ✅ `FiloKpiDashboard.razor` filo/puantaj linki kaldırıldı
> 6. ✅ `NavMenu.razor` `"filo-gunluk-puantaj"` string temizlendi
> 7. ✅ Build doğrulandı (başarılı)
>
> Ardından **FAZ 1** ile yeni operasyonel puantaj sayfasını sıfırdan yazmaya başlarız.
> Sayfa adı: `OperasyonelPuantajPage.razor` — rota: `/operasyon/puantaj`
> Veri kaynağı: `ServisKontrat` → `FiloGunlukPuantaj` (mevcut entity'ler korunuyor)

---

## 8. BAĞLAM SORU-CEVAP (Hızlı Referans)

| Soru | Cevap |
|------|-------|
| Günlük puantaj tablosu hangisi? | `FiloGunlukPuantaj` (FiloKomisyonPuantaj.cs içinde) |
| Kontrat tipi enum nerede? | `ServisKontrat.cs` → `ServisKontratTip` (Ozmal/Kiralik/Tedarikci) |
| Araç tipi enum nerede? | `Arac.cs` → `AracSahiplikTipi` (Ozmal=1, Kiralik=2, Tedarikci=5) |
| Çalışan hakediş servisi hangisi? | `OperasyonelHakedisService` (IOperasyonelHakedisService) |
| Eski hakediş servisi hangisi? | `HakedisPuantajService` (IHakedisPuantajService) — göz ardı edilecek |
| Maliyet snapshot nerede? | `AracMaliyetService` + `AracMaliyetSnapshot` entity |
| DB provider? | PostgreSQL (Host=localhost, Port=5432, DB=MKFiloServis) |
| Build durumu? | ✅ 0 warning, 0 error (Temmuz 2026) |
| Fullpet dağıtımı nasıl? | `IAracMaliyetService.FullpetFaturaDagitAsync(...)` |
