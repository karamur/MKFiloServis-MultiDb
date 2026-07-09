---
title: "Puantaj Prototip - VERİ SORUNU ÇÖZÜM REHBERİ"
author: "Claude Copilot"
date: 2025-01-14
locale: "tr-TR"
---

# 🎯 Puantaj Prototip - VERİ SORUNU ÇÖZÜM

## 🔴 PROBLEM
```
❌ Sayfa açılıyor   ✅
❌ UI görünüyor     ✅
❌ Tabs çalışıyor   ✅
❌ BUT... Cariler listesi BOŞŞ  ← ÖĞRETİSİ BURASI!
```

## 🟢 ÇÖZÜM: 3 ADIM

### **ADIM 1: VERİ HAZIRLA** (5 dakika)

**Seçenek A: SQL Script (En Hızlı)**
```sql
-- pgAdmin, DBeaver, veya psql'de çalıştır

INSERT INTO "Cariler" ("FirmaId", "CariTamAdi", "CariKodu", "IsActif", "InsertedAt")
VALUES (1, 'TRT Ankara', 'TR-001', true, NOW());

-- CariId öğren (sağ tık → Properties veya SELECT sorgusu)
-- Sonra aşağıdaki SQL'lerde bunu kullan (örneğin 100)

INSERT INTO "Kurumlar" ("FirmaId", "CariId", "KurumAdi", "IsActif", "InsertedAt")
VALUES (1, 100, 'TRT Ankara Merkez', true, NOW());

INSERT INTO "Araclar" ("FirmaId", "AracPlakasi", "AracMarka", "IsActif", "InsertedAt")
VALUES (1, '06 AAA 45', 'Mercedes', true, NOW());

INSERT INTO "Soforler" ("FirmaId", "SoforAdi", "TCKimlikNo", "IsActif", "InsertedAt")
VALUES (1, 'Ali Veli', '12345678901', true, NOW());

INSERT INTO "Guzergahlar" ("FirmaId", "GuzergahAdi", "IsActif", "InsertedAt")
VALUES (1, 'Yenişehir → Merkez', true, NOW());

INSERT INTO "FiloGuzergahEslestirmeleri" 
("FirmaId", "KurumId", "AracId", "SoforId", "GuzergahId", "BirimFiyat", "GiderFiyat", "IsActif", "BaslangicTarihi", "InsertedAt")
VALUES (1, 10, 50, 75, 25, 150.00, 50.00, true, NOW(), NOW());

INSERT INTO "FiloGunlukPuantajlar" 
("FirmaId", "FiloGuzergahEslestirmeId", "Tarih", "Durum", "BirimFiyat", "GiderFiyat", "Toplam", "InsertedAt")
VALUES 
(1, 1, '2025-01-01', 'Hizmet Verildi', 150.00, 50.00, 200.00, NOW()),
(1, 1, '2025-01-02', 'Hizmet Verildi', 150.00, 50.00, 200.00, NOW());
```

**Seçenek B: Admin Panelinden (Daha Anlaşılır)**
```
1. http://localhost:5190 açılır
2. Menü → Müşteri Yönetimi
3. + Yeni Cari ekle
   - Ad: "TRT Ankara"
   - Kod: "TR-001"
   - Kaydet ✓

4. Menü → Kurum Yönetimi
5. + Yeni Kurum
   - Ad: "TRT Ankara Merkez"
   - Cari: TRT Ankara (seç)
   - Kaydet ✓

6. Menü → Araç Yönetimi
7. + Yeni Araç
   - Plaka: "06 AAA 45"
   - Kaydet ✓

8. Menü → Şoför Yönetimi
9. + Yeni Şoför
   - Ad: "Ali Veli"
   - Kaydet ✓

10. Menü → Güzergah Yönetimi
11. + Yeni Güzergah
    - Ad: "Yenişehir → Merkez"
    - Kaydet ✓

12. Menü → Filo Eşleştirmeler (veya Araç-Şoför)
13. + Yeni Eşleştirme
    - Araç: Seç
    - Şoför: Seç
    - Güzergah: Seç
    - Kurum: Seç
    - Kaydet ✓
```

### **ADIM 2: SAYFAYı YENİLE**

```
http://localhost:5190/puantaj/cari-hiyerarsi

❌ F5 basma (Browser cache)
✅ Ctrl+Shift+R basma (Hard refresh)
```

### **ADIM 3: KONTROL ET**

```
✓ Cariler listesi görünüyor mu?
  - TRT Ankara - accordion başlığı

✓ Accordion açılıyor mu?
  - Kurumlar alt listesi görünüyor

✓ Tablo doldurulmuş mu?
  - Araç, Şoför, Güzergah satırları
```

---

## 🏗️ TEKNIK BACKGROUND (Merak edenler için)

### Neden Veri Yok?

```
Service → MockData dönüyor:

// Eski (Şu anda)
GetCariKurumHiyerarsiAsync(1)
  → Cariler.Count = 0 ← MOCK!

// Yeni (Gelecek - Real DB)
GetCariKurumHiyerarsiAsync(1)
  → SELECT ... FROM Cariler
  → WHERE CariId = 1
  → JOIN Kurumlar
  → return Cariler + Kurumlar + Araçlar
```

### Service Bağlama Noktası (Real DB)

```csharp
// Şu anda Shared/Services/Implementations/CariKurumPuantajService.cs'te:
// ✅ BAĞLAMA NOKTASI yorumları var
//
// Orada yazıyor:
// "var carilar = await dbContext.Cariler
//                .Include(c => c.Kurumlar)
//                .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
//                .FirstOrDefaultAsync(c => c.CariId == cariId);"

// Bu kod SONRA yazılacak (Service'i DbContext ile inject edelim)
```

### Veritabanı Şeması

```
Cari
  ├─ CariId ⭐
  ├─ CariTamAdi
  └─ CariKodu

Kurum (← CariId FK)
  ├─ KurumId ⭐
  ├─ CariId (FK)
  └─ KurumAdi

Arac
  ├─ AracId ⭐
  └─ AracPlakasi

Sofor
  ├─ SoforId ⭐
  └─ SoforAdi

Guzergah
  ├─ GuzergahId ⭐
  └─ GuzergahAdi

FiloGuzergahEslestirme (← Kurum, Arac, Sofor, Guzergah FK)
  ├─ FiloGuzergahEslestirmeId ⭐
  ├─ KurumId (FK)
  ├─ AracId (FK)
  ├─ SoforId (FK)
  ├─ GuzergahId (FK)
  ├─ BirimFiyat
  └─ GiderFiyat

FiloGunlukPuantaj (← FiloGuzergahEslestirme FK)
  ├─ FiloGunlukPuantajId ⭐
  ├─ FiloGuzergahEslestirmeId (FK)
  ├─ Tarih
  ├─ Durum
  └─ BirimFiyat
```

---

## ✅ YAPILACAKLAR (Roadmap)

| Faz | Yapılacak | Durum | Neden |
|-----|-----------|-------|-------|
| **Faz 0** | Veri hazırlama | ⏳ BUGÜN | Prototip test için |
| **Faz 1** | Service'i Real DB'ye bağla | ⏳ SONRA | Mock → Real query |
| **Faz 2** | GridComponent implement | ⏳ SONRA | Row-based daily grid |
| **Faz 3** | Excel import | ⏳ SONRA | Toplu veri giriş |
| **Faz 4** | E2E Test + Deploy | ⏳ SONRA | Production ready |

---

## 📊 Başarı Kriterleri

```
✅ Faz 0 Tamamlanmış:
   → Cari görünüyor
   → Accordion açılıyor
   → Grid 31 hücre gösteriyor
   → Veri tablosunda satirlar var

❌ Şu Anda:
   → Veri mock (bağlama noktası DocumentED)
   → GridComponent placeholder
   → Toplu işlemler stub
```

---

## 📞 HIZLI BAŞLA

```
1. SQL Kopyala → pgAdmin'de Çalıştır (2 dakika)
2. Ctrl+Shift+R → Tarayıcı Yenile (1 dakika)
3. Kontrol → "Cariler görünüyor mu?" (1 dakika)

Toplam: 4 dakika! ⚡
```

---

**Yazılı BU rehber yardımcı oldu mu? Soru var mı? Söyle!** 🎉
