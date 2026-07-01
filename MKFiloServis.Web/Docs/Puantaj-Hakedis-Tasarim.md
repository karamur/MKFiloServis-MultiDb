# Puantaj • Maliyet • Hakediş • Faturalama Tasarımı

> Bu doküman mevcut yapıyı **bozmadan** üzerine inşa edilecek genişletmeyi tanımlar.
> Kaynak: kullanıcı isteği — özmal / kiralık / tedarikçi araç tipleri için maliyet, puantaj
> hakediş ve gelir-gider faturalama akışı.

---

## 1. Sahiplik Modeli (mevcut, korunuyor)

`AracSahiplikTipi` (MKFiloServis.Shared/Entities/Arac.cs):

| Tip | Anlam | Plaka | Araç | Şoför / Personel | Masraf Sahibi |
|---|---|---|---|---|---|
| **Özmal** (1) | Bizim aracımız | Bize ait | Bize ait | Bizim personelimiz | **Biz** |
| **Kiralık** (2) | C-plaka kiralık | Kiralık | Kiralık | Bizim personelimiz | **Biz** (plaka kirası dahil) |
| **Komisyon** (3) | Komisyonla iş | — | — | — | Karma |
| **Diğer** (4) | — | — | — | — | — |
| **Tedarikçi** (5) | Taşeron araç | Sahibinin | Sahibinin | Sahibinin | **Tedarikçi** (biz sadece sefer başı öderiz) |

> Maliyet hesaplama tarafı bu tabloya göre dallanır.
> Mevcut `Arac.SahiplikTipi`, `Arac.TasimaTedarikciId` zaten bu ayrımı taşıyor.

---

## 2. Mevcut Çekirdek Varlıklar (korunuyor)

| Varlık | Rolü |
|---|---|
| `Arac` | Araç ve sahiplik bilgisi |
| `Sofor` | Şoför / personel |
| `Guzergah` | Sefer/güzergah tanımı |
| `Firma` / `Cari (KurumCari)` | İşi aldığımız kurum |
| `TasimaTedarikci` | Taşeron tedarikçi |
| `ServisKontrat` | Kurum × güzergah × araç × şoför sözleşmesi (Özmal/Kiralık/Tedarikçi) |
| `ServisPuantaj` | Aylık dönemsel puantaj toplamı (kontrat bazında) |
| `ServisTahsilat` | Kurumdan tahsilat satırı |
| `ServisOdeme` | Tedarikçi/personel ödeme satırı |
| `FiloGuzergahEslestirme` | Şablon eşleştirme (alternatif yol) |
| `FiloGunlukPuantaj` | Günlük gerçekleşen puantaj satırı |
| `PersonelPuantaj` / `GunlukPuantaj` | Personel maaş bazlı aylık puantaj |
| `AracMasraf` / `MasrafKalemi` | Araç bazlı gider |
| `Fatura` / `FaturaKalem` | Gelir / gider faturaları |

> **Mevcut iki paralel akış var:**
> - `ServisKontrat → ServisPuantaj → ServisTahsilat / ServisOdeme` (kontrat bazlı, dönemsel)
> - `FiloGuzergahEslestirme → FiloGunlukPuantaj` (günlük operasyonel)
>
> Tasarım, **ikisini de yaşatır**: günlük puantaj operasyonun temelidir,
> dönemsel `ServisPuantaj` bunun aylık özetidir ve hakediş/fatura kesimine kaynak olur.

---

## 3. Eklenecek / Genişletilecek Yapılar

### 3.1 `FiloGunlukPuantaj` üzerine eklemeler (yeni alanlar)

| Alan | Tip | Amaç |
|---|---|---|
| `ServisTuru` | `ServisTuru` | Sabah / Akşam / Yarda Mesai / SabahAksam (zaten enum var, kayda yazılmıyor — eklenecek) |
| `SeferSayisi` | `decimal` | O gün o vardiyada gerçekleşen sefer sayısı (varsayılan 1.0) |
| `MaliyetOzmalKiralik` | `decimal?` | Özmal/Kiralık ise hesaplanan birim maliyet snapshot (yakıt+şoför+amortisman) |
| `KurumFaturaId` | `int?` | Kuruma kesilen faturaya bağlantı |
| `TedarikciOdemeFaturaId` | `int?` | Tedarikçiden alınan / tedarikçiye kesilen faturaya bağlantı |

> `OperasyonDurumu`, `TahakkukEdenKurumUcreti`, `TahakkukEdenTaseronUcreti`, `TaksiFisTutari` zaten var; korunacak.

### 3.2 Yeni varlık: `AracMaliyetSnapshot` (özmal/kiralık aylık)

```
AracId, Yil, Ay
ToplamKm, ToplamSefer
YakitMasraf, BakimMasraf, LastikMasraf, SigortaMasraf, KaskoMasraf,
PlakaKirasi (kiralık ise), SoforMaasOran, AmortismanOran, DigerMasraf
ToplamMaliyet  -> bu araç bu ay bize kaça mâl oldu
SeferBasiMaliyet -> ToplamMaliyet / ToplamSefer
```

> Kaynak: `AracMasraf`, `KiralikPlakaTakip`, `Bordro/PersonelPuantaj` (şoför oranı),
> `Lastik`, `BakimOnarimKayit`. Servis tarafından her ay arka planda hesaplanır.

### 3.3 Yeni varlık: `Hakedis`

```
Yil, Ay
HakedisTipi: Kurum | Tedarikci | Arac
ReferansId: KurumCariId | TasimaTedarikciId | AracId
ToplamSeferSayisi, BirimFiyat (snapshot), Tutar
KdvOran, KdvTutar, GenelToplam
Durum: Taslak | Onaylandi | Faturalandi | Tahsil/Odendi
FaturaId? (üretilen Fatura'ya bağ)
GenerationParams (JSON: dönem, filtreler)
```

> Hakediş, `FiloGunlukPuantaj` + `ServisPuantaj` üzerinden **toplulaştırma** ile üretilir.
> "Araç bazında" / "Kurum bazında" / "Tedarikçi bazında" sadece `HakedisTipi` ile dallanır.

### 3.4 Yeni varlık (opsiyonel): `HakedisDetay`

Hakediş başlığının altındaki gün/sefer satırlarını saklar (PDF'e dökülür).
`FiloGunlukPuantajId`'a bağlanır.

### 3.5 `Fatura` ile bağlantı

`Fatura` zaten var; yeni alanlar:

| Alan | Tip | Amaç |
|---|---|---|
| `HakedisId` | `int?` | Faturanın kaynak hakedişi |
| `FaturaYonu` | `enum (Gelir/Gider)` | Kuruma kesilen = Gelir, tedarikçiden gelen = Gider |

> Mevcutta varsa atlanır; yoksa migration ile eklenir.

---

## 4. Akışlar

### 4.1 Günlük Puantaj Girişi (operasyonun kalbi)

`/filo/puantaj` (mevcut `FiloPuantaj.razor` üzerinde geliştirilecek):

1. Tarih ve vardiya seç (Sabah / Akşam / Yarda Mesai)
2. Kontrat / eşleştirme havuzundan satırlar otomatik gelir
3. Operatör her satır için: Durum, Sefer Sayısı, Notlar girer
4. Kaydet → her satır için `FiloGunlukPuantaj` insert/update
5. Sahipliğe göre tahakkuk hesaplanır:
   - Özmal/Kiralık: sadece `TahakkukEdenKurumUcreti` (gelir)
   - Tedarikçi: hem `TahakkukEdenKurumUcreti` (gelir) hem `TahakkukEdenTaseronUcreti` (gider)

### 4.2 Aylık Maliyet Tablosu (özmal / kiralık)

`/raporlar/arac-maliyet`:

- Filtre: Yıl/Ay, Sahiplik (Özmal/Kiralık), Araç
- Sütunlar: Plaka • Sefer • Yakıt • Bakım • Lastik • Sigorta • Plaka Kirası •
  Şoför Maaş Payı • Toplam Maliyet • Sefer Başı Maliyet • Kuruma Kesilen Gelir • **Kar/Zarar**
- "Snapshot oluştur" butonu → `AracMaliyetSnapshot` üretir

### 4.3 Hakediş Ekranları

`/hakedis` (mevcut `HakedisTablosu.razor` genişletilecek):

- 3 sekme: **Kurum Bazında** • **Araç Bazında** • **Tedarikçi Bazında**
- Filtre: Dönem (Yıl/Ay), Kurum/Tedarikçi/Araç
- "Hakediş Üret" → seçilen dönem + filtre için `Hakedis` + `HakedisDetay` insert
- Liste: Üretilmiş hakedişler — Onayla / Faturaya Dönüştür / PDF
- "Faturaya Dönüştür" → `Fatura` + `FaturaKalem` üretir, `Hakedis.FaturaId` bağlar

### 4.4 Fatura Kesme

| Kaynak | Yön | Karşı Taraf | Kalem |
|---|---|---|---|
| Kurum hakedişi | Gelir | Kurum (Cari) | Sefer × birim fiyat |
| Tedarikçi hakedişi | Gider | Tedarikçi (Cari) | Sefer × tedarikçi birim fiyat |
| Araç hakedişi (iç rapor) | — | — | Faturalanmaz, iç maliyet/karlılık raporu |

---

## 5. Servis Katmanı

Yeni / güncellenen servisler:

| Servis | Sorumluluk |
|---|---|
| `IPuantajService` (yeni) | Günlük puantaj CRUD, vardiya bazlı tahakkuk, çoklu kayıt |
| `IAracMaliyetService` (yeni) | Aylık maliyet snapshot üret/oku |
| `IHakedisService` (yeni) | Hakediş üretimi (Kurum/Araç/Tedarikçi), onay, faturaya dönüştürme |
| `IFaturaService` (mevcut, genişletilecek) | Hakedişten gelir/gider faturası üret |
| `IServisKontratService` (mevcut) | Korunur; günlük puantaj birimi `ServisKontrat`'tan birim fiyat alır |

---

## 6. Migration / DB Etkisi

Mevcut tablolar **bozulmaz**. Yapılacaklar:

1. `FiloGunlukPuantaj` tablosuna kolon ekleme: `ServisTuru`, `SeferSayisi`, `MaliyetOzmalKiralik`, `KurumFaturaId`, `TedarikciOdemeFaturaId`
2. Yeni tablolar: `AracMaliyetSnapshotlar`, `Hakedisler`, `HakedisDetaylari`
3. `Faturalar` tablosuna: `HakedisId`, `FaturaYonu` (yoksa)

> `DbInitializer.cs` içindeki PostgreSQL/SQLite duplicate-table recovery
> kalıbına yeni tabloların migration id'leri eklenir.

---

## 7. Uygulama Adımları (İteratif)

> Mevcut yapı bozulmasın diye tek tek, derleme/test ederek ilerleyeceğiz.

### Faz 1 — Temel veri modeli
- [ ] `FiloGunlukPuantaj`'a yeni kolonlar
- [ ] `AracMaliyetSnapshot` entity + DbSet + migration
- [ ] `Hakedis` + `HakedisDetay` entity + DbSet + migration
- [ ] `Fatura`'ya `HakedisId` + `FaturaYonu` (yoksa)

### Faz 2 — Servis katmanı
- [ ] `IPuantajService` + `PuantajService`
- [ ] `IHakedisService` + `HakedisService`
- [ ] `IAracMaliyetService` + `AracMaliyetService`

### Faz 3 — UI
- [ ] `FiloPuantaj.razor` → vardiya bazlı toplu giriş + sefer sayısı
- [ ] `/raporlar/arac-maliyet` yeni sayfa
- [ ] `HakedisTablosu.razor` → Kurum/Araç/Tedarikçi sekmeli + üretim/onay/faturalama
- [ ] Fatura listesi: kaynak hakediş gösterimi

### Faz 4 — Raporlar / PDF
- [ ] Hakediş PDF (kurum-tedarikçi formatları)
- [ ] Aylık maliyet excel/pdf

---

## 8. Notlar

- Mevcut `ServisKontrat` / `ServisPuantaj` zaten "kontrat bazlı dönemsel" akışı destekliyor.
  Yeni `Hakedis` bunun **toplulaştırma katmanıdır**, mevcut kayıtları **kaynak olarak kullanır**.
- Mevcut `FiloKomisyonPuantaj` ile çakışmamak için yeni günlük puantaj genişletmesi
  `FiloGunlukPuantaj` üzerinde yapılır.
- Yeni hiçbir alanı zorunlu (Required) yapmayalım — eski kayıtlar geçerli kalsın.
