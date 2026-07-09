# Hızlı Referans: Personel Taşıması Puantaj Sistemi

**Belge**: Özet & Kontrol Listesi  
**Tarih**: 2025-01-23

---

## 📌 KIS ÖZET

Personel taşıması yapan bir firmada puantaj sistemi **TP bağımsız** ele alma sistemi vardır:

### 1️⃣ **Operasyonel Puantaj** (İnvoicing/Gelir-Gider)
- **Ne**: Her günün araç-şoför-rota hizmetinin kaydı
- **Kimin**: Operasyon ekibi
- **Sonuç**: Müşteri faturası + Tedarikçi ödeme
- **Sistem Entity**: `FiloGunlukPuantaj` (günlük), `HakedisPuantaj` (aylık), `Hakedis` (müşteri toplam)

### 2️⃣ **Personel Puantajı** (Maaş Hesabı)
- **Ne**: Şoför/operasyon personelinin çalışma bilgileri
- **Kimin**: İnsan Kaynakları
- **Sonuç**: Aylık maaş = Net Ödeme
- **Sistem Entity**: `PersonelPuantaj` (aylık), `Bordro` (ödeme listesi)

---

## 🔄 Veri Akışı (30 Sekunde)

```
GÜN 1-31: FiloGunlukPuantaj kaydı (Araç-Şoför-Müşteri)
    ↓
GÜN 28: HakedisPuantaj otomatik oluştur (Topla)
    ↓
GÜN 28: PersonelPuantaj finaliz (İK kontrol)
    ↓
GÜN 29: Hakedis onay (Operasyon)
    ↓
GÜN 29: Fatura oluştur (Muhasebe)
    ↓
GÜN 30-31: Banka Transfer (Müşteri, Tedarikçi, Personel)
```

---

## 💡 Önemli Farklar

| Özellik | Operasyonel Puantaj | Personel Puantajı |
|---------|---------------------|-------------------|
| **Kaynak** | Şoför GPS/QR | İK sisteminden |
| **Amaç** | Fatura & KDV | Maaş & Stopaj |
| **Frekans** | Günlük | Aylık |
| **KDS Oran** | %20 (ingrid) | SGK %14 + Vergi %15 |
| **Ana İlişki** | Guzergah × Araç × Şoför × Müşteri | Personel |
| **Entity Tipi** | FiloGunlukPuantaj + HakedisPuantaj | PersonelPuantaj |

---

## 📊 Aylık Sayısal Örnek

**Senaryo**: TRT-ANKARA, Ali Demir, Araç 34CX8, Ocak 2025

### Operasyonel:
```
─ Çalışılması Gün: 20 gün (1 izin, 1 arıza)
─ Sefer Sayısı: 20 × 2 = 40 sefer (Sabah+Akşam)
─ Tarife: 150 TL/gün = 40 × 150 = 6.000 TL (Müşteriye)
─ Gider: 40 × 80 = 3.200 TL + %20 KDV = 3.840 TL (Tedarikçiye)
─ Kâr: 6.000 - 3.840 = 2.160 TL
```

### Personel:
```
─ Çalışılan Gün: 20 gün
─ Brüt: 8.500 TL (Base) + 750 TL (Yemek) + 200 TL (Yol) = 9.450 TL
─ Kesinti: 1.393 TL (SGK) + 1.418 TL (Vergi) = 2.811 TL
─ Net: 9.450 - 2.811 = 6.639 TL (Ödeme)
```

---

## 🎯 Key Metrikleri

### Operasyonel KPI'lar
- **Sefer Gerçekleşme Oranı**: (Tamamlandı / Planlandı) × 100
- **Kâr Marjı**: (Gelir - Gider) / Gelir × 100
- **Rota Verimliliği**: Toplam Sefer / Araç Sayısı / Gün

### Personel KPI'ları
- **Devam Oranı**: (Çalışılan Gün / 22) × 100
- **Fazla Mesai Saati**: > 160 saat (22 gün × 8 saat)
- **Maaş/Sefer Oranı**: Net Ödeme / Çalışılan Gün

---

## ⚙️ Teknik Kontrol Listesi

### Günlük ✅
- [ ] FiloGunlukPuantaj kaydı tam mı? (Tüm araçlar)
- [ ] Durum: Tamamlandı / İptal / Arıza?
- [ ] KM ve Saat kontrolü OK?

### Haftalık ✅
- [ ] Anomali raporunu gözden geçir
- [ ] Eksik kayıtlar? (FiloGunlukPuantaj)
- [ ] Çift yazılı kayıtlar? (Unique constraint?)

### Aylık ✅
- [ ] HakedisPuantaj doğru hesaplanmış? (Kontrol sayfı)
- [ ] Hakedis onaylandı? (İmza)
- [ ] PersonelPuantaj sonuçlandı? (Bordro hazır)
- [ ] KDV oranları doğru? (Tarife tablo)
- [ ] Banka transferi tamamlandı?

---

## 🚨 Hata Senaryoları

### Operasyonel Hataları

**Problem**: Şoför hava durumu nedeni ile Sabah seferi yapamadı (sadece Akşam)
```
Çözüm:
1. FiloGunlukPuantaj.ServisTuru = "Aksam" (Sabah+Aksam yerine)
2. Sistem otomatik: Sefer = 1 (2'den azal)
3. HakedisPuantaj yeniden hesaplat: 40 yerine 39 sefer
4. Tutar: 39 × 150 = 5.850 TL (600 TL eksi)
```

**Problem**: Arıza raporu geç alındı (işlem yapıldı fakat belgelenmedı)
```
Çözüm:
1. FiloGunlukPuantaj.Durum = "ArizaNedeniyleYapilamadi" (Düzelt)
2. AracMasraf.ArizaTipi detaylandır
3. Personel: ArizaGunu sayılabilir mi? (İK kontrol)
4. Raporta not: "Arıza Tarihi: 15-Oca, Tamir: 3 saatlik"
```

### Personel Hataları

**Problem**: Ali Demir 2 gün izin aldığında sistem sadece 1 gün belgeledi
```
Çözüm:
1. PersonelPuantaj.CalisilanGun = 21 (22 yerine) ← KONTROL
2. PersonelPuantaj.IzinGunu = 2 ← Arttır
3. BrutMaas proportional azalsın?
   - Yemek: 21 × 37.50 = 787.50 (20'den azal)
4. Net maaş yeniden hesapla
```

**Problem**: SGK kesintisi yanlış hesaplanmış
```
Çözüm:
1. SGK oranı 2025 için: %14 (cek tarif tablosundan)
2. BrutMaas × 0.14 = SGK (otomatik)
3. Örnek: 9.450 × 0.14 = 1.323 TL (1.393 yerine)
4. Muhasebede düeltme fişi (Personel Kesinti Düz.)
```

---

## 📋 Şablon & Formlar

### Excel Hakedis Template
```
Sütun A-H:  Guzergah, Arac, Sofor, Müşteri, Yon, Sefer Sayısı, Tarife
Sütun I-L:  Tutar, KDV.20, KDV.10, Toplam
Sütun M-P:  Gider Birim, Gider Toplam, Gider KDV, Ödenecek
Sütun Q:    Kar
Formül:     =K×L, =I×0.20, =I×0.10, =I+J+İ, vs.
```

### Excel Bordro Template
```
A: Personel Adı
B: Çalışılan Gün
C: Brüt Maaş
D: Yemek (B×37.50)
E: Yol
F: Prim
G: Toplam (C+D+E+F)
H: SGK (G×0.14)
I: Vergi (G×0.15)
J: Net Ödeme (G-H-I)
```

---

## 🔗 Sistem Değişkeni

### İnsan Kaynakları Bakış Açısı
- Personel maaş yapısı: Seferel + Bonus
- Arıza/İzin yönetimi: KVK sistemine bağlı
- Devam raporu: Aylık (otomatik veya manuel)

### Muhasebe Bakış Açısı
- KDV Bildirimi: HakedisPuantaj.Tutar + .KdvTutar
- Gelir Vergisi: PersonelPuantaj.BrutMaas üzerinde stopaj
- E-Fatura: Format UBL, Aylık son gün

### Operasyon Bakış Açısı
- Rota verimliliği: Sefer/Araç/Gün
- Müşteri memnuniyeti: Arızasız Gün %
- Tedarikçi ödemesi: Zamanında & doğru

---

## 📞 İletişim & Sorumluluk

| Rol | Sorumluluk | İletişim |
|-----|-----------|----------|
| **Şoför** | Günlük puantaj input (QR/SMS) | Operasyon |
| **Operasyon** | FiloGunlukPuantaj onay, Hakedis kontrolü | Muhasebe, İK |
| **İK** | PersonelPuantaj finaliz | Muhasebe |
| **Muhasebeci** | KDV, Stopaj, Fatura, Bordro | Yönetim |
| **Yönetim** | Kârlılık analizi, Tarife gözden geçirme | Pazarlama, İK |

---

**Hazırlanma Tarihi**: 2025-01-23  
**Dönem**: 2025 Ocak ve Sonrası  
**Gözden Geçiren**: [Operasyon + Muhasebe Müdürü]
