---
title: "Puantaj Veri Yönetimi - Kurulum & Veri Giriş Rehberi"
author: "Claude Copilot"
date: 2025-01-14
locale: "tr-TR"
version: "1.0"
---

# 📊 Puantaj Veri Yönetimi - Kurulum & Veri Giriş Rehberi

## 🎯 Genel Durum

| Bileşen | Durum | Not |
|---------|-------|-----|
| Service Interface | ✅ Hazır | `ICariKurumPuantajService` |
| Service Implementation | ⚠️ Mock'ta | TODO: DB query'ler eklenecek |
| Blazor UI | ✅ Hazır | Tabs, Accordion, Grid placeholder |
| Database Tables | ✅ Mevcut | Cari, Kurum, FiloGuzergahEslestirme, FiloGunlukPuantaj |
| **Veri (Data)** | ❌ YOK | **👈 ŞU ANDA BURASI** |

---

## 🗄️ Veritabanında Hangi Tablolar Var?

```
ApplicationDbContext'te:

1. Cariler              → Müşteri/Kurumların sahibi
2. Kurumlar            → Müşteri alt birimleri
3. Araclar             → Şirket araçları
4. Soforler            → Şoför bilgileri
5. Guzergahlar         → Rota/Güzergah tanımları
6. FiloGuzergahEslestirme  → Araç+Şoför ↔ Güzergah eşleştirmesi
7. FiloGunlukPuantaj       → Günlük puantaj kayıtları
```

**Problemi**: İlişkili tablolarda **veri yok** veya veri **doğru ilişkilendirilmemiş**.

---

## 📝 Veri Giriş Yöntemleri

### **Yöntem 1: Admin Paneli Üzerinden (Kolay) ⭐**

```
Adım 1: http://localhost:5190 'ye gir
Adım 2: Menü → Müşteri Yönetimi (veya Cari)
Adım 3: + Yeni Cari oluştur
        - Ad: "TRT Ankara"
        - Kod: "TR-001"
        - Kaydet

Adım 4: Menü → Kurum Yönetimi
Adım 5: + Yeni Kurum oluştur
        - Adı: "TRT Ankara Merkez"
        - Karşılık Cari: TRT Ankara (seç)
        - Kaydet

Adım 6: Menü → Araç Yönetimi
Adım 7: + Yeni araç ekle
        - Plaka: "06 AAA 45"
        - Marka: "Mercedes Sprinter"
        - Kaydet

Adım 8: Menü → Şoför Yönetimi
Adım 9: + Yeni şoför ekle
        - Ad: "Ali Veli"
        - TC No: "12345678901"
        - Kaydet

Adım 10: Menü → Güzergah Yönetimi
Adım 11: + Yeni güzergah ekle
         - Adı: "Yenişehir → Merkez"
         - Kaydet

Adım 12: Menü → Araç-Şoför Eşleştirmesi (veya Filo)
Adım 13: + Yeni eşleştirme
         - Araç: "06 AAA 45"
         - Şoför: "Ali Veli"
         - Güzergah: "Yenişehir → Merkez"
         - Kurum: "TRT Ankara Merkez"
         - Kaydet
```

✅ **Sonuç**: Veriler kaydedilirse Service otomatik çeker!

---

### **Yöntem 2: SQL Script (Hızlı) ⚡**

Veritabanına direkt SQL ile veri eklemek:

```sql
-- 1. Cari ekle
INSERT INTO "Cariler" ("FirmaId", "CariTamAdi", "CariKodu", "IsActif", "InsertedAt")
VALUES (1, 'TRT Ankara', 'TR-001', true, NOW());

-- CariId'yi al (sonra kullan):
-- SELECT "CariId" FROM "Cariler" WHERE "CariKodu" = 'TR-001';
-- Varsayalım: CariId = 100

-- 2. Kurum ekle
INSERT INTO "Kurumlar" ("FirmaId", "CariId", "KurumAdi", "Aciklama", "IsActif", "InsertedAt")
VALUES (1, 100, 'TRT Ankara Merkez', '', true, NOW());

-- KurumId'yi al:
-- SELECT "KurumId" FROM "Kurumlar" WHERE "CariId" = 100;
-- Varsayalım: KurumId = 10

-- 3. Araç ekle
INSERT INTO "Araclar" ("FirmaId", "AracPlakasi", "AracMarka", "AracModel", "IsActif", "InsertedAt")
VALUES (1, '06 AAA 45', 'Mercedes', 'Sprinter', true, NOW());

-- AracId'yi al:
-- SELECT "AracId" FROM "Araclar" WHERE "AracPlakasi" = '06 AAA 45';
-- Varsayalım: AracId = 50

-- 4. Şoför ekle
INSERT INTO "Soforler" ("FirmaId", "SoforAdi", "TCKimlikNo", "IsActif", "InsertedAt")
VALUES (1, 'Ali Veli', '12345678901', true, NOW());

-- SoforId'yi al:
-- SELECT "SoforId" FROM "Soforler" WHERE "SoforAdi" = 'Ali Veli';
-- Varsayalım: SoforId = 75

-- 5. Güzergah ekle
INSERT INTO "Guzergahlar" ("FirmaId", "GuzergahAdi", "BaslangicNokta", "BitiNokta", "IsActif", "InsertedAt")
VALUES (1, 'Yenişehir → Merkez', 'Yenişehir', 'Merkez', true, NOW());

-- GuzergahId'yi al:
-- SELECT "GuzergahId" FROM "Guzergahlar" WHERE "GuzergahAdi" = 'Yenişehir → Merkez';
-- Varsayalım: GuzergahId = 25

-- 6. FiloGuzergahEslestirme ekle
INSERT INTO "FiloGuzergahEslestirmeleri" 
("FirmaId", "KurumId", "AracId", "SoforId", "GuzergahId", "BirimFiyat", "GiderFiyat", "IsActif", "BaslangicTarihi", "InsertedAt")
VALUES (1, 10, 50, 75, 25, 150.00, 50.00, true, NOW(), NOW());

-- EslestirmeId'yi al:
-- SELECT "FiloGuzergahEslestirmeId" FROM "FiloGuzergahEslestirmeleri" WHERE "KurumId" = 10;
-- Varsayalım: EslestirmeId = 1

-- 7. FiloGunlukPuantaj ekle (örnek: Bu ayın 1.'i)
INSERT INTO "FiloGunlukPuantajlar" 
("FirmaId", "FiloGuzergahEslestirmeId", "Tarih", "Durum", "BirimFiyat", "GiderFiyat", "Toplam", "Notlar", "InsertedAt")
VALUES 
(1, 1, '2025-01-01', 'Hizmet Verildi', 150.00, 50.00, 200.00, 'Normal iş', NOW()),
(1, 1, '2025-01-02', 'Hizmet Verildi', 150.00, 50.00, 200.00, 'Normal iş', NOW()),
(1, 1, '2025-01-03', 'Hizmet Verildi', 150.00, 50.00, 200.00, 'Normal iş', NOW());
```

---

## 🔗 Veri Akışı (Boş → Dolu)

```
1. Cari oluştur
      ↓
2. Kurum oluştur (← Cari FK)
      ↓
3. Araç oluştur
      ↓
4. Şoför oluştur
      ↓
5. Güzergah oluştur
      ↓
6. FiloGuzergahEslestirme oluştur (← Araç, Şoför, Güzergah, Kurum FK'ları)
      ↓
7. FiloGunlukPuantaj oluştur (← FiloGuzergahEslestirme FK)
      ↓
✅ Service veri gösteriyor!
```

---

## 🚀 Hemen Ne Yapmak Lazım?

### **Seçenek A: Test Verisi Başlat (Önerilen)**
```powershell
# 1. DB'ye bağlan (pgAdmin veya psql)
# 2. Yukarıdaki SQL script'ini çalıştır
# 3. http://localhost:5190/puantaj/cari-hiyerarsi 'ye gir
# 4. Veri görünüyor mu kontrol et
```

### **Seçenek B: Admin Paneli ile Gir**
```
1. http://localhost:5190 açık
2. Menü'nde Cari/Kurum/Araç/Şoför yönetimi ara
3. Yeni kayıtlar ekle
4. Puantaj sayfasına geri dön
```

### **Seçenek C: Service'i Real DB'ye Bağla**
```csharp
// TODO: Program.cs'te Service registration:
// builder.Services.AddScoped<ICariKurumPuantajService>(sp =>
// {
//     var dbContext = sp.GetRequiredService<ApplicationDbContext>();
//     return new CariKurumPuantajServiceImpl(dbContext);
// });

// Service'de GetCariKurumHiyerarsiAsync():
// var carilar = await dbContext.Cariler
//     .Include(c => c.Kurumlar)
//     .ThenInclude(k => k.FiloGuzergahEslestirmeleri)
//     .Where(c => c.CariId == cariId)
//     .FirstOrDefaultAsync();
```

---

## ⚠️ Sık Sorulan Sorular

### **S: Veriler neden gözükmüyor?**
```
Olası Sebepler:
1. DB'de veri yok            → SQL script çalıştır veya Admin'den ekle
2. Service mock döndürüyor   → Cevap: Evet, henüz DB'ye bağlanmıyor
3. Kurum FK'sı boş           → Cari → Kurum ilişkisi yokmi kontrol et
4. Permission sorunu         → [Authorize] var, giriş yaptığını kontrol et
```

### **S: Hangi FirmaId kullanmalıyım?**
```
Admin'le giriş yap → Aktif firma otomatik seçilir
SQL'de FirmaId = 1 koy (varsayılan)
```

### **S: Real data nasıl alırız hiç?**
```
Şu anda:
✅ UI hazır
✅ Service contract hazır  
❌ Service → DB connection başında

Sonraki: Service'i real DbContext ile implement etmek
```

---

## 📋 Kontrol Listesi

- [ ] DB'de Cari kaydı var mı?
- [ ] Cariye ait Kurum kaydı var mı?
- [ ] Kurum'a ait Araç var mı?
- [ ] Araç'a ait Şoför var mı?
- [ ] FiloGuzergahEslestirme oluşturuldu mu?
- [ ] FiloGunlukPuantaj'da günler var mı?
- [ ] Sayfada Cari listesi görünüyor mu?
- [ ] Accordion açılıyor mu?
- [ ] Grid yükleme butonu çalışıyor mu?

---

## 📞 Sonraki Adımlar

1. **Hemen**: SQL script'i çalıştır veya Admin'den veri ekle
2. **Sonra**: Service'i real DB'ye bağla
3. **Nihai**: GridComponent + batch operations

Başarılar! 🎉
