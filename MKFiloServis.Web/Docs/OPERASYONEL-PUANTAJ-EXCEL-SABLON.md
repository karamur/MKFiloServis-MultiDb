# MKFiloServis — Operasyonel Puantaj Excel Şablon Sözleşmesi

> Amaç: Operasyonel puantaj Excel yüklemelerinde tek format standardı kullanmak ve hatalı/mükerrer kayıt riskini azaltmak.

---

## 1) Kapsam

Bu sözleşme aşağıdaki akışlar için geçerlidir:
- Puantaj preview
- Puantaj import/update (idempotent)
- Hata/fark raporu üretimi

Hedef ekran/servis:
- `Personel/PuantajExcelGrid.razor`
- `PuantajExcelService.cs`

---

## 2) Dosya Formatı

- Dosya tipi: `.xlsx`
- İlk satır: **başlık satırı**
- Veri başlangıcı: 2. satır
- Tarih alanı: `yyyy-MM-dd` veya Excel date serial
- Ondalık alanlar: `.` veya `,` kabul edilir (serviste normalize edilir)

---

## 3) Zorunlu Kolonlar

Aşağıdaki kolonlar zorunludur (başlık adları birebir önerilen adlarla gelmelidir):

1. `Tarih`
2. `Guzergah`
3. `AracPlaka`
4. `ServisTuru`
5. `SeferSayisi`

Opsiyonel kolonlar:
- `Sofor`
- `Not`
- `Kaynak`

---

## 4) Alan Kuralları

### 4.1 Tarih
- Boş olamaz
- Geçerli tarih olmalı

### 4.2 Guzergah
- Boş olamaz
- Sistemde aktif güzergah ile eşleşmeli

### 4.3 AracPlaka
- Boş olamaz
- Sistemdeki araç kaydı ile eşleşmeli

### 4.4 ServisTuru
- Boş olamaz
- Sistemde tanımlı enum/değer setine eşleşmeli

### 4.5 SeferSayisi
- Boş olamaz
- `>= 0` olmalı
- Negatif değer kabul edilmez

### 4.6 Sofor (opsiyonel)
- Doluysa sistemdeki şoför kaydı ile eşleşmeli

---

## 5) Idempotent Eşleştirme Anahtarı

Import upsert anahtarı:

`Firma + Tarih + Guzergah + AracPlaka (veya Sofor) + ServisTuru`

Davranış:
- Aynı anahtar tekrar gelirse: **update**
- Yeni anahtar ise: **insert**

---

## 6) Preview Çıktı Sözleşmesi

Preview aşaması şu grupları üretmelidir:

1. **Geçerli Satırlar**
2. **Eksik Kolon Hataları**
3. **Referans Eşleşme Hataları** (güzergah/araç/şoför)
4. **Veri Kural Hataları** (tarih/sefer türü/sefer sayısı)
5. **Diff Satırları** (Eski/Yeni/Durum)

Durum alanı önerisi:
- `Yeni`
- `Güncellenecek`
- `Değişmedi`
- `Hatalı`

---

## 7) Onaylı/Faturalı Dönem Koruması

Eğer satır, `Onaylandi/Faturalandi` hakediş dönemini etkiliyorsa:
- Doğrudan overwrite yapılmaz
- Fark raporu + kullanıcı onayı gerekir
- İşlem audit log'a yazılır

---

## 8) Hata Raporu CSV Formatı

Önerilen kolonlar:
- `SatirNo`
- `Alan`
- `Deger`
- `HataKodu`
- `HataMesaji`
- `Oneri`

Örnek HataKodları:
- `MISSING_REQUIRED_COLUMN`
- `INVALID_DATE`
- `ROUTE_NOT_FOUND`
- `VEHICLE_NOT_FOUND`
- `INVALID_TRIP_COUNT`
- `LOCKED_PERIOD`

---

## 9) Örnek Başlık ve Satır

Örnek başlık:

`Tarih | Guzergah | AracPlaka | ServisTuru | SeferSayisi | Sofor | Not | Kaynak`

Örnek satır:

`2026-07-15 | Organize Sanayi - Merkez | 34ABC123 | PersonelTasima | 2 | Ahmet Yılmaz | Sabah-akşam seferi | ExcelImport`

---

## 10) Kabul Kriterleri

Bu şablon uygulandı sayılması için:
1. Eksik zorunlu kolonlarda import başlamadan kullanıcıya açık hata verilir.
2. Preview aşamasında hatalı satırlar CSV olarak indirilebilir.
3. Aynı dosya ikinci yüklemede mükerrer kayıt üretmez.
4. Onaylı/faturalı dönem etkisinde doğrudan overwrite engellenir.

---

## 11) Sürüm Notu

- Sürüm: `v1`
- Tarih: `Temmuz 2026`
- Bağlı plan: `OPERASYONEL-PUANTAJ-TEK-PLAN-DOSYASI.md`
