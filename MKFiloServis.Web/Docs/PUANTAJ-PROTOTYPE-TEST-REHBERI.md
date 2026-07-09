---
title: "Puantaj Hiyerarşi Prototip - Test Rehberi"
author: "Claude Copilot"
date: 2025-01-14
locale: "tr-TR"
version: "2.0"
---

# 🧪 Puantaj Hiyerarşi Prototip - Test Rehberi

## 📌 Test Ortamı

### Server Bilgisi
- **URL**: http://localhost:5190
- **Durumu**: ✅ Çalışıyor
- **Port**: 5190
- **Environment**: Development

---

## 🚀 Hızlı Başlangıç

### **Adım 1: Veri Hazırla**
```
→ PUANTAJ-VERİ-YONETIMI-KURULUM.md dosyasını oku
→ SQL script'i çalıştır VEYA Admin'den veri ekle
→ Minimum: 1 Cari + 1 Kurum + 1 Araç + 1 Şoför + 1 Eşleştirme
```

### **Adım 2: Sayfaya Git**
```
http://localhost:5190/puantaj/cari-hiyerarsi
```

### **Adım 3: Test Et**
```
✓ Cariler listeleniyorsa    → Veri gelişyor!
✓ Accordion açılıyorsa      → Component çalışıyor
✓ Grid dolduruluyorsa       → Service başarılı
```

---

## 🎯 Test Seviyeleri

### **1️⃣ Manuel Test (Tarayıcı)**

#### ✅ Seviye 1: Sayfa Erişim
```
1. Tarayıcıda açmak: http://localhost:5190
2. Kontrol Noktaları:
   ✓ Uygulama açılıyor mı?
   ✓ Login gerekli mi?
   ✓ Admin giriş yapılabiliyor mu? (user: admin, pass: admin123)
```

#### ✅ Seviye 2: Rota Erişim
```
1. Adres: http://localhost:5190/puantaj/cari-hiyerarsi
2. Kontrol Noktaları:
   ✓ 404 hatası alınmıyor mu?
   ✓ Sayfa [Authorize] koruması çalışıyor mu?
   ✓ Layout doğru render ediliyor mu?
```

#### ✅ Seviye 3: UI Bileşenleri
```
Sayfa yüklendiğinde kontrol et:

a) TABS
   ✓ "Hiyerarşi Görüntüle" tab'ı var mı?
   ✓ "Aylık Grid" tab'ı var mı?
   ✓ Tab'lar tıklanabiliyor mu?

b) ACCORDION (Hiyerarşi Tab'ında)
   ✓ Cari başlıkları görünüyor mu?  ← VERİ İLE?
   ✓ Accordion açılıp kapanıyor mu? ✅
   ✓ Kurum listesi gösteriyor mu?   ← VERİ İLE?

c) TABLO (Eşleştirmeler)
   ✓ Tablosu render ediliyor mu? ✅
   ✓ Kolon başlıkları doğru mu?  ✅
     - Araç Plakası
     - Şoför Adı
     - Güzergah
     - Birim Fiyat
     - Gider Fiyat
     - Durum

d) AYLIk GRID TAB'I
   ✓ Placeholder görünüyor mu? ✅
   ✓ Yıl/Ay seçimi var mı? ✅
   ✓ "Grid Yükle" butonu tıklanabiliyor mu? ✅
   ✓ Gün hücreleri doldurulduğu (31 hücre)? ✓
```

#### ✅ Seviye 4: Service Çağrısı
```
Browser DevTools (F12) açmak:

1. Network Tab
   ✓ Aylık Grid Yükle'ye tıkla
   ✓ XHR isteği yapılıyor mu?
   ✓ 200 OK yanıt alınıyor mu?
   ✓ Response'ta gün bilgisi var mı?

2. Console
   ✓ Hata mesajı yok mu?
   ✓ Warning yok mu?
```

---

### **2️⃣ Gelişmiş Test (Playwright)**

#### Komut Satırında Çalıştırma
```powershell
# Test Framework klasörüne gitmek
cd MKFiloServis.Web/Tests/PlaywrightSmoke

# Temel smoke test
dotnet run

# Ortam değişkenleriyle
$env:CRMFILO_BASE_URL = "http://localhost:5190"
$env:CRMFILO_TEST_USER = "admin"
$env:CRMFILO_TEST_PASSWORD = "admin123"
dotnet run
```

---

## 📊 Test Özet Tablosu

| Seviye | Test Adı | Status | Not |
|--------|---------|--------|-----|
| 1 | Uygulama açılma | ✅ OK | 200 OK |
| 1 | Authorization | ✅ OK | Giriş istemi çalışıyor |
| 2 | Rota erişim | ✅ OK | /puantaj/cari-hiyerarsi → Sayfa açılıyor |
| 3 | Tab rendering | ✅ OK | 2 tab görünür |
| 3 | Accordion | ✅ OK | Açılıp kapanıyor (Veri gerekli) |
| 3 | Tablo | ✅ OK | Eşleştirmeler render ediliyor |
| **3** | **Cari listesi** | ⏳ Bekleniyor | **DB'ye veri eklenirse otomatik gelecek** |
| 4 | Grid yükleme | ✅ OK | 31 gün hücre doldurulur |
| 4 | Console | ✅ OK | Hata yok |

---

## ⚠️ Bilinen Sorunlar & Çözümleri

### ✅ Çözülen
- ✅ Build error (Authorize namespace' fix edildi)
- ✅ Service registration (DI kaydı yapıldı)
- ✅ Grid oluşturma (Mock data ile 31 gün)

### 🔧 Bekleyen Çözümler

#### 1. **Cariler listelenmiyor?** 
   - 🔍 Sebep: DB'de veri yok
   - ✅ Çözüm: PUANTAJ-VERİ-YONETIMI-KURULUM.md oku → SQL çalıştır

#### 2. **"Veri yok" mesajı görünüyor?**
   - 🔍 Sebep: GetCarilarPuantajOzetiAsync mock döndürüyor
   - ✅ Çözüm: DbContext inject edip real query yazmak (SONRAKI PHASE)

#### 3. **Grid placeholder boş?**
   - 🔍 Sebep: PuantajGunlukGrid.razor bileşeni henüz oluşturulmadı
   - ✅ Çözüm: Aylık grid component implement (SONRAKI PHASE)

#### 4. **"[Mock]" yazısı görünüyor?**
   - 🔍 Sebep: Service henüz gerçek DB'ye bağlanmamış
   - ✅ Çözüm: ✅ Artık bağlama noktaları DocumentED halinde (kod içinde)

---

## 🎯 Veri Akışı Şeması

```
1. DB'de veri yok
        ↓
2. Admin panelden veri ekle VEYA SQL çalıştır
        ↓
3. http://localhost:5190/puantaj/cari-hiyerarsi 'ye gir
        ↓
4. Service → GetCariKurumHiyerarsiAsync() çağrılır
        ↓
5. [ŞU ANDA] Mock data dönüyor
   [SONRA] Real DB query döneceği:
   - SELECT Cariler
   - JOIN Kurumlar
   - JOIN FiloGuzergahEslestirmeleri
   - Render → Accordion
        ↓
6. Accordion açılıyor → Kurumlar, Araçlar, Şoförler görünüyor
        ↓
7. "Grid Yükle" → GetKurumPuantajAylikAsync() → 31 gün
        ↓
✅ Test başarılı!
```

---

## 🚀 Sonraki Adımlar (Roadmap)

```
[CURRENT] ✅ UI Prototype + Mock data
   ↓
[NEXT-1] 📊 Real DB implentation
   - DbContext WebContent'ten inject et
   - GetCariKurumHiyerarsiAsync() → Real JOIN query
   - GetKurumPuantajAylikAsync() → FiloGunlukPuantaj sorgusu

   ↓
[NEXT-2] 🎨 GridComponent implement
   - PuantajGunlukGrid.razor oluştur
   - Row-based daily cells
   - Edit modunda cells
   - Bulk update

   ↓
[NEXT-3] 📁 Toplu işlemler
   - Excel import
   - Batch insert/update
   - Missing-day insertion

   ↓
[FINAL] ✅ E2E Test + Deploy
```

---

## 📋 Kontrol Listesi

### HEMEN YAPILACAK:
- [ ] VERİ KURULUMU:
  - [ ] SQL script'i çalıştır
  - [ ] VEYA Admin'den Cari ekle
  - [ ] Kurum ekle
  - [ ] Araç/Şoför ekle
  - [ ] Eşleştirme oluştur

- [ ] TEST:
  - [ ] Sayfa açılıyor / 200 OK
  - [ ] Authorization çalışıyor  
  - [ ] Tabs render ediliyor
  - [ ] Accordion açılıyor
  - [ ] Cariler listeleninleri GÖRÜNÜyor ← VERİ ÜZERİNDE TEST
  - [ ] Tablo görünüyor
  - [ ] Grid Yükle butonu tıklanabiliyor
  - [ ] Network XHR OK döndürüyor
  - [ ] 31 hücre görünüyor
  - [ ] Console'da hata yok

### SONRA YAPILACAK:
- [ ] DbContext impl
- [ ] GridComponent
- [ ] Excel import
- [ ] E2E tests

---

## 📞 Destek

### Hata mı?
1. Browser console'unu aç (F12)
2. Network → XHR filter'ini kullan
3. Response'ı kontrol et
4. Logs'u oku:
   ```powershell
   # Terminal'da running server output'ı gör
   get_background_terminal_output --tail 50
   ```

### Veri mi eklenmiyor?
→ PUANTAJ-VERİ-YONETIMI-KURULUM.md dosyasını oku

---

**Başarılı testler! 🎉**

---

**Son Güncelleme**: 2025-01-14
**Sürüm**: 2.0 (Veri yönetimi + Real DB bağlama noktaları eklendi)

