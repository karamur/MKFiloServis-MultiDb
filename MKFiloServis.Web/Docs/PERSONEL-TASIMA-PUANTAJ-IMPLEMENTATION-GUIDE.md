# Personel Taşıması Puantaj Sistemi: Implementation & Troubleshooting Guide

**Tarih**: 2025-01-23  
**Hedef**: Operasyonel takip ve sorun giderme için adım-adım rehber

---

## 🎯 OBJECTIVE MAP

```
┌─────────────────────────────────────────────┐
│   PERSONEL TAŞIMA PTS                       │
│   Multi-Tenant + Operasyonel Puantaj +      │
│   Personel Maaş Sistemi                    │
│                                             │
│ Version: 1.0 (Prod)                        │
│ Status: Live                                │
└─────────────────────────────────────────────┘

O-1: Operasyonel Puantaj (FiloGunlukPuantaj)
     └─ O-1.1: Günlük Kayıt
     └─ O-1.2: Haftalık Review
     └─ O-1.3: Anomali Detection

O-2: Hakediş Oluşturma (HakedisPuantaj + Hakedis)
     └─ O-2.1: Aylık Topla
     └─ O-2.2: Onay Workflow
     └─ O-2.3: Fatura Dönüştür

O-3: Personel Puantaj (PersonelPuantaj)
     └─ O-3.1: Ay Başı Hazırlık
     └─ O-3.2: Ay Sonu Finalize
     └─ O-3.3: Bordro Oluştur

O-4: Muhasebe Kapanış
     └─ O-4.1: KDV Hızlı Kontrol
     └─ O-4.2: Stopaj Doğrulama
     └─ O-4.3: Uzlaştırma
```

---

## 📦 PROCESS FLOWS

### Flow 1: Günlük Operasyon (FiloGunlukPuantaj Entry)

```
START
  │
  ├─ [SABAH 06:30] Şoför Başlangıç QR Taraması
  │  │
  │  └─ Sistem: FiloGunlukPuantaj → Başlangıç Zaman Kaydı
  │
  ├─ [GÜN ESNASINDA] Rota Uygulaması
  │  │
  │  └─ Şoför: Normal çalışma (İzleme yapılıyor)
  │
  ├─ [AKŞAM 17:30] Şoför Sonuç QR Taraması
  │  │
  │  └─ Sistem: FiloGunlukPuantaj → Bitiş Zaman Kaydı
  │
  ├─ [TERMINAL] Araç Kontrol
  │  │
  │  ├─ KM sayacı: OK?
  │  ├─ Yakıt: OK?
  │  ├─ Temizlik: OK?
  │  └─ Hasar: OK?
  │
  └─ Sistem UPDATE
     │
     ├─ IF Tüm OK: Durum = "Tamamlandı" ✓
     ├─ ELSE IF Arıza: Durum = "ArizaNedeniyleYapilamadi" + AracMasraf
     ├─ ELSE IF İptal: Durum = "IptalEdildi"
     └─ ELSE: Durum = "Planli" (Tekrar yarın)

END ✓

─────────────────────────────────────────────────
Su, Çarş, Per, Cum, Cts günleri tekrar
(Cuma günü: Cumartesi için kontrol), (Pazar: İzin)
─────────────────────────────────────────────────
```

### Flow 2: Aylık Hakedis Oluşturma (Yapı Yoğunluk Oto-Oluştur Rutin)

```
START (GÜN 28 SABAHI, Otomatik Job)
  │
  ├─ [STEP 1] Veri Getirme
  │  │
  │  └─ SELECT * FROM FiloGunlukPuantaj
  │     WHERE Yil=2025 AND Ay=1
  │     AND Durum IN ('Tamamlandı', 'IptalEdildi')
  │     GROUP BY Guzergah, Arac, Sofor, Cari
  │
  │   Çıktı (ÖRNEK):
  │   ┌──────────────────────────────────────┐
  │   │ Gz: 1 │ Ar: 34CX8 │ Sf: Ali │ Ca: 42
  │   │ Gz: 2 │ Ar: 06TX2 │ Sf: Veli│ Ca: 42
  │   │ Gz: 3 │ Ar: 24RH4 │ Sf: Mehm│ Ca: 42
  │   └──────────────────────────────────────┘
  │
  ├─ [STEP 2] HakedisPuantaj Oluşturma
  │  │
  │  └─ FOREACHRow IN Rows:
  │        INSERT INTO HakedisPuantaj (
  │          Yil, Ay, GuzergahId, AracId, SoforId, CariId,
  │          ToplamSefer = 44 (SUM Tamamlandı),
  │          GelirBirimFiyat = 150 (Tarif),
  │          GiderBirimFiyat = 80 (Tarif),
  │          GelirToplam = 44 × 150 = 6.600 TL,
  │          GiderToplam = 44 × 80 = 3.520 TL,
  │          KdvTutari = 3.520 × 0.20 = 704 TL,
  │          OdenecekTutar = 3.520 + 704 = 4.224 TL,
  │          Durum = 'Taslak'
  │        )
  │
  ├─ [STEP 3] Hakedis (Müşteri Toplamı) Oluşturma
  │  │
  │  └─ FOREACHCari IN UniqCariler:
  │        SELECT SUM(GelirToplam), SUM(ToplamSefer),
  │               SUM(KdvTutari)
  │        FROM HakedisPuantaj
  │        WHERE CariId = X AND Yil=2025 AND Ay=1
  │
  │        INSERT INTO Hakedis (
  │          Yil, Ay, Tip='Kurum', ReferansId=42,
  │          ToplamSeferSayisi = 122,
  │          Tutar = 16.240 TL,
  │          KdvTutar = 3.248 TL,
  │          GenelToplam = 19.488 TL,
  │          Durum = 'Taslak'
  │        )
  │
  ├─ [STEP 4] Bildirim Gönderme
  │  │
  │  └─ Email Operasyon Müdürü:
  │     "Hakedis oluşturuldu. Kontrol etmediniz belgeleme:
  │      - TRT-ANKARA: 19.488 TL
  │      - İSKİ: 8.900 TL
  │      - ESHOT: 5.200 TL
  │      Toplam: 33.588 TL
  │      
  │      Lütfen GÜN 29'de onaylayınız."
  │
  └─ END ✓ (Log: "Hakedis Job tamamlandı")

─────────────────────────────────────────────────
[GÜN 28-29] Manuel Kontrol & Onay
  │
  ├─ Operasyon müdürü Hakedis rapor inceliyor
  ├─ Anomali varsa: FiloGunlukPuantaj'da düzelt
  ├─ Sonra: HakedisPuantaj & Hakedis yeniden hesapla
  ├─ OK ise: Hakedis.Durum = 'Onaylandı'
  └─ Email muhasebeci: "Hakedis hazır"

[GÜN 29] Fatura Oluşturma
  │
  ├─ Muhasebeci: Hakedis → Fatura dönüştürme
  ├─ Fatura.Tarih = Son gün (31-Oca)
  ├─ Fatura.Vade = Müşteri kontratında (Ör. +45 gün)
  ├─ Hakedis.FaturaId = yeni FaturaId
  ├─ Hakedis.Durum = 'FaturayaDonusturuldu'
  ├─ Fatura.Durum = 'Yayındatılmış'
  └─ Email müşteri: "Fatura bağlantısı"
─────────────────────────────────────────────────
```

### Flow 3: Personel Puantajı Hazırlama & Finalize

```
START (GÜN 1 - Ay Başında)
  │
  ├─ Sistem: PersonelPuantaj kaydı otomatik başlat
  │  │
  │  └─ For Each Personel IN Aktif:
  │       INSERT INTO PersonelPuantaj (
  │         Yil=2025, Ay=1, PersonelId,
  │         CalisilanGun=0 (Henüz bilinmiyor),
  │         FazlaMesaiSaat=0,
  │         Durum='Hazirlanıyor'
  │       )
  │
  ├─ [GÜN 1-21] Ay Esnasında
  │  │
  │  └─ Her gün BATCH:
  │       UPDATE PersonelPuantaj SET
  │       CalisilanGun = CalisilanGun + 1
  │       WHERE PersonelId IN (
  │         SELECT DISTINCT SoforId
  │         FROM FiloGunlukPuantaj
  │         WHERE Tarih='2025-01-02' AND Durum='Tamamlandı'
  │       )
  │
  │  [Mantık]: Eğer şoför o gün "Tamamlandı" kaydı varsa → Gün sayısı +1
  │
  ├─ [GÜN 25-27] Ay Sonu - İK Kontrol
  │  │
  │  └─ İK Yöneticisi:
  │     1. PersonelPuantaj rapor açıyor
  │     2. Her personel için kontrol:
  │        - CalisilanGun doğru? (Sayım eşleşiyor mu?)
  │        - IzinGunu ekleme (İzin belgesi varsa)
  │        - MazeretGunu ekleme (Rapor varsa)
  │        - FazlaMesaiSaat kontrol
  │     3. Düzeltmeler yapılıyor
  │
  ├─ [GÜN 27] Finalize
  │  │
  │  └─ İK: PersonelPuantaj.Durum = 'Onaylı'
  │     Sistem otomatik: Maaş hesaplama
  │     ├─ BrutMaas (Base)
  │     ├─ YemekUcreti (CalisilanGun × 37.50)
  │     ├─ YolUcreti
  │     ├─ Prim (Bonus)
  │     ├─ TOPLAM Gelir
  │     ├─ SGK Kesinti (Gelir × 0.14)
  │     ├─ Vergi Kesinti (Gelir × 0.15)
  │     └─ NET Ödeme
  │
  ├─ [GÜN 28] Bordro Oluşturma
  │  │
  │  └─ Sistem otomatik:
  │     CREATE Bordro FROM PersonelPuantaj
  │     (WHERE Durum='Onaylı')
  │     ├─ Bordro.Ay = Ocak
  │     ├─ Bordro.Durumu = 'Hazirlanıyor'
  │     └─ Bordro satırı = Personel + NetÖdeme
  │
  ├─ [GÜN 28-29] Muhasebe Kontrol
  │  │
  │  └─ Muhasebeci:
  │     1. Bordro toplam kontrol
  │     2. İlişki hesaplamalar:
  │        - SGK topla → İş Yeri Payı = Bordro × 0.165
  │        - Vergi topla → KDV / Stopaj
  │     3. Muhasebeci onayı: Bordro.Durum = 'Onaylı'
  │
  ├─ [GÜN 30-31] Banka Transferi
  │  │
  │  └─ Mali İşler:
  │     1. Bordro → Havale Listesi
  │     2. Her personel için transfer
  │     3. Referans: "Ocak 2025 Maaş"
  │     4. Bordro.Durum = 'Ödendi'
  │
  └─ END ✓ (Log: "Personel Puantaj Tamamlandı")

─────────────────────────────────────────────────
```

---

## 🔧 TROUBLESHOOTING GUIDE

### ❌ PROBLEM: Sefer Sayısı Yanlış

**Belirtiler**:
- HakedisPuantaj.ToplamSefer = 40 fakat beklenen 44
- Muhasebe: "Tutar uyumsuz"

**Teşhis**:

```sql
-- 1. Kontrol: FiloGunlukPuantaj sayısı doğru mu?
SELECT COUNT(*), SUM(CASE WHEN Durum='Tamamlandı' THEN 1 ELSE 0 END)
FROM FiloGunlukPuantaj
WHERE Yil=2025 AND Ay=1 
  AND GuzergahId=3 AND AracId=34CX8 AND SoforId=[Ali ID];
-- Beklenen: 44 (22 gün × 2 sefer)

-- 2. Kontrol: Durum değerleri ne?
SELECT Durum, COUNT(*)
FROM FiloGunlukPuantaj
WHERE Yil=2025 AND Ay=1 
  AND GuzergahId=3 AND AracId=34CX8
GROUP BY Durum;
-- Beklenen: Tamamlandı=40, IptalEdildi=0, ArizaNedeniyle=2

-- 3. Eksik günler?
SELECT DISTINCT Tarih
FROM FiloGunlukPuantaj
WHERE Yil=2025 AND Ay=1 
  AND GuzergahId=3 AND AracId=34CX8
ORDER BY Tarih;
-- Beklenen: 22 farklı gün
```

**Çözüm Adımları**:

```
IF Toplam Sayı = 40 (beklenen 44):
  ├─ STEP 1: Eksik 4 sefer hangi gün?
  │  Tarih aralığını kontrol: Ör. 13-14-15 Ocak eksik
  │
  ├─ STEP 2: Neden eksik?
  │  ├─ İzin mi? (Dosyada var mı?)
  │  ├─ Arıza mı? (AracMasraf var mı?)
  │  └─ Sayı hatası mı?
  │
  ├─ STEP 3: İzin ise
  │  │
  │  └─ PersonelPuantaj.IzinGunu +=1
  │     (Zaten oto-hesaplansa da doğrula)
  │
  ├─ STEP 4: Arıza ise
  │  │
  │  └─ AracMasraf danışmana bakma
  │     (Tamir süresi / Parça bekleme)
  │     Kaç gün duracak?
  │
  └─ STEP 5: Sayı hatası ise
     └─ FiloGunlukPuantaj'da manuel kaydı yeniden gir
```

**Örnek Düzeltme**:

```sql
-- Eksik 4 sefer: 13-14 Ocak (2 gün × 2 sefer)
-- Neden: Ali izinli ama kaide yazılmadı

-- Çözüm 1: PersonelPuantaj.IzinGunu +=2
UPDATE PersonelPuantaj
SET IzinGunu = IzinGunu + 2
WHERE PersonelId=[Ali ID] 
  AND Yil=2025 AND Ay=1;

-- Çözüm 2: HakedisPuantaj manuel düzelt (geçici)
UPDATE HakedisPuantaj
SET ToplamSefer = 40,  -- 44 yerine (çalışan gün başına)
    GelirToplam = 40 * 150 = 6.000,
    GiderToplam = 40 * 80 = 3.200,
    KdvTutari = 3.200 * 0.20 = 640,
    OdenecekTutar = 3.840
WHERE Yil=2025 AND Ay=1 
  AND GuzergahId=3 AND AracId=34CX8;

-- Sonra: Hakedis otomatik yeniden hesapla
```

---

### ❌ PROBLEM: Personel Maaş Hesaplaması Yanlış

**Belirtiler**:
- Ali Demir Net: 7.064 TL fakat beklenen 7.500 TL
- SGK hesaplaması yanlış?

**Teşhis**:

```sql
-- 1. PersonelPuantaj kaydı
SELECT * FROM PersonelPuantaj
WHERE PersonelId=[Ali ID] AND Yil=2025 AND Ay=1;

-- Beklenen Sonuç:
CalisilanGun=20, BrutMaas=8500, YemekUcreti=750, 
YolUcreti=200, Prim=0, DigerOdeme=0
TotalGelir = 9450

-- 2. Kesintiler
SgkKesinti = 9450 × 0.14 = 1323
GelirVergisi = 9450 × 0.15 = 1417.50
TotalKesinti = 2740.50
NetOdeme = 9450 - 2740.50 = 6709.50 (7.064 DEĞİL!)

-- 3. Problem: BrutMaas alanı kontrol
```

**Çözüm**:

```
Problem Senaryosu 1: BrutMaas hatalı
  └─ UPDATE PersonelPuantaj
       SET BrutMaas = 8500 (Kontrat üzerinden)
       WHERE PersonelId=[Ali ID]

Problem Senaryosu 2: YemekUcreti yanlış
  └─ Hesaplandı mı: 20 × 37.50 = 750?
     Eğer 750 yerine 600 yazılıysa:
     UPDATE PersonelPuantaj
       SET YemekUcreti = 750

Problem Senaryosu 3: SGK oranı 2025 için salalı
  └─ 2025 SGK = %14 (Uygun)
     Eğer %13 olarak hesaplanırsa:
     Sistem kodu kontrol et: Services/PersonelPuantajService.cs

Problem Senaryosu 4: Fazla Mesai Saati Yanlış
  └─ Eğer FazlaMesaiSaat=16 yazılıysa
     Fazla = 16 × 53.75 = 860 TL (ek)
     Bu durumda maaş yükselir
```

---

### ❌ PROBLEM: KDV Fatura Tutarında Hata

**Belirtiler**:
- Fatura Net: 16.240 TL
- KDV hesaplaması: 3.248 TL (% 20)
- Beklenen: (Müşteri 3 Kurum × KDV farklı?)

**Teşhis**:

```sql
-- 1. HakedisPuantaj kontrol
SELECT GuzergahId, GelirToplam, KdvOrani, KdvTutari, GelirToplam * (KdvOrani/100) AS BeklenenKdv
FROM HakedisPuantaj
WHERE CariId=42 AND Yil=2025 AND Ay=1;

-- Örnek:
-- | Gz1 | 6600   | 20 | 1320 | Doğru
-- | Gz2 | 5040   | 20 | 1008 | Doğru
-- | Gz3 | 5200   | 20 | 1040 | Doğru (Toplam = 3368, değil 3248!)

-- 2. Hakedis kontrol
SELECT * FROM Hakedis
WHERE CariId=42 AND Yil=2025 AND Ay=1;

-- Hesaplama:
TotalTutar = 6600 + 5040 + 5200 = 16.840 (BURADAn 16.240 mi yazılı?)
TotalKdv = 1320 + 1008 + 1040 = 3.368 (BURADAn 3.248 mi yazılı?)
```

**Çözüm**:

```
IF KdvTutar != (Tutar × KdvOrani / 100):
  └─ Problem: Hakedis hesaplaması yanlış

  Çözüm:
  UPDATE Hakedis
  SET Tutar = 16.840,       -- Doğru toplam
      KdvTutar = 3.368,      -- Doğru KDV
      GenelToplam = 20.208   -- Tutar + KdvTutar
  WHERE CariId=42 AND Yil=2025 AND Ay=1;

  Muhasebe Fişi:
  Belge: "Hakedis KDV Düzeltme (Ocak 2025)"
  Tutar: 120 TL (Fark)
  Açıklama: "HakedisPuantaj oto-hasplaması yanlış alınmış, düzeltildi"
```

---

## 📋 DAİLY/WEEKLY/MONTHLY CHECKLIST

### DÜN MORNING (Operasyon)

```
⬜ FiloGunlukPuantaj SAYISI
   Beklenen: Araç sayısı × 2 sefer
   ├─ 3 Araç, Her gün 2 sefer = 6 gün
   ├─ Pazarları harioç = 5-6 gün/hafta
   └─ Anomali varsa, Durum düzeltildi mi?

⬜ AracMasraf Raporu
   ├─ Yeni arıza var mı?
   ├─ Tamir süresi: 1-2 gün / 1 hafta?
   └─ Sefer kayıpları bekleniyor mi?
```

### HER CUMA (Operasyon Sonu Hafta)

```
⬜ Haftalık Puantaj Özeti
   ├─ Tüm sefer sayısı
   ├─ Anomali kaydı tamamlandı mı?
   └─ Maaş standart: CalisilanGun × 5 gün OK?

⬜ Sonraki Haftayı Planlama
   ├─ Izin takvimi kontrol
   ├─ Araç bakım planlı mı?
   └─ Yeni rota/araç lansman var mı?
```

### AYI 28 (Muhasebe Start)

```
⬜ Aylık Paparçe Finalizasyonu
   ├─ FiloGunlukPuantaj son kayıtları (31-Ocak)
   ├─ Tüm anomaliler çözüldü mü?
   └─ Hakedis oluşturma öncesi: FINAL CHECK

⬜ HakedisPuantaj Kontrol
   ├─ Sayı hesaplands mı? (Aylık sefer)
   ├─ Tarifler doğru alındı mı?
   ├─ KDV oranları doğru alındı mı?
   └─ Durum: Taslak → Hazır Onaya
```

### AYI 29 (Muhasebe Onay)

```
⬜ Hakedis Onay Rutin
   ├─ Operasyon müdürü kontrol
   ├─ Anomali raporu mü?
   └─ Onay: Durum = "Onaylandı"

⬜ Personel Puantaj Finalize (İK)
   ├─ CalisilanGun doğru?
   ├─ İzin/Mazeret günleri gerçek mi?
   └─ Maaş hesaplama oto doğru alındı mı?

⬜ Fatura Oluşturma (Muhasebe)
   ├─ Fatura no. atandı mı?
   ├─ E-Fatura şekli kontrol
   └─ Vade tarihi: 45 gün?
```

### AYI 30-31 (Ödeme)

```
⬜ Banka Transferleri
   ├─ Müşteri Faturası
   │  ├─ PDF gönderildi
   │  ├─ Portal gönderildi
   │  └─ Tahsil takibi başladı
   │
   ├─ Tedarikçi Ödeme
   │  ├─ Ödeme Emri oluşturuldu
   │  ├─ Havale sırada
   │  └─ Status: "Ödendi"
   │
   └─ Personel Ödeme
      ├─ Bordro finalize
      ├─ GÜMRÜK KAYIT günceleme
      └─ Status: "Hesaba Yatırıldı"
```

---

## 🎓 TRAINING POINTS

### Operasyon Ekibi (Şoför + Denetçi)

1. **QR/SMS Puantaj**: "Her gün başında QR tara"
2. **Hata Bildir**: "Araç arızası 2 saat içinde email"
3. **İzin Prosedürü**: "İzin 3 gün önceden İK'ye"
4. **Maaş Kontrol**: "Ayın 30'nda personel puantaj kontrolü"

### Muhasebe Ekibi

1. **HakedisPuantaj Doğrulama**: "28'de otomatik oluşur, 29'da kontrol"
2. **KDV Hesaplama**: "Tutar × 0.20 = KDV (Gider için %20, Gelir için %20)"
3. **Fatura Oluşturma**: "Hakedis → Fatura, Tarih = Ay Sonu"
4. **Stopaj Hesaplama**: "PersonelPuantaj Brut × 0.15 = Stopaj"

### İnsan Kaynakları

1. **Personel Puantaj**: "Ay 27'de finalize et, bütün hesaplar doğru?"
2. **İzin Takvimi**: "İzin gözden geçir, SystemPersonelPuantaj.IzinGunu"
3. **Maaş Bileşenleri**: "Yemek = Çalışılan Gün × 37.50"
4. **Bordro Finalize**: "Ay 28'de oluşturulur, 29'da kontrol"

---

## 📞 Support & Escalation

### Level 1: Operasyon (İlk Yanıt)
- **Sorumlu**: Operasyon Müdürü
- **Yanıt Zamanı**: < 2 saat
- **Kapsam**: Günlük puantaj anomalisiyasi, rota planı
- **Çözüm**: Sistem ekibine yönlendirme / Manuel düzeltme

### Level 2: Muhasebe (Kontrol)
- **Sorumlu**: Muhasebe Müdürü
- **Yanıt Zamanı**: < 4 saat
- **Kapsam**: KDV, Stopaj, Fatura
- **Çözüm**: Teknik SQL düzeltmesi veya yeniden hesaplama

### Level 3: Teknoloji (Dev)
- **Sorumlu**: Developer
- **Yanıt Zamanı**: < 24 saat
- **Kapsam**: Sistem bug, automating calculation error
- **Çözüm**: Code patch, deployment

---

**Son Güncelleme**: 2025-01-23
**Versiyon**: 1.0
**Hazırlayan**: Sistem Mimarı & Operasyon Danışmanı
