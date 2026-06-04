# KOAFiloServis Mimari Diyagram Metni

Bu doküman, çizime dönüştürülebilecek metinsel mimari diyagram içeriğini sağlar.

---

## 1. Üst Seviye Diyagram

```text
                  +------------------------------+
                  |       KOAFiloServis Web      |
                  |       Blazor Server UI       |
                  +---------------+--------------+
                                  |
                                  |
                                  v
                  +------------------------------+
                  |   Application / Domain Layer |
                  |  Use-case + Tenant Rules     |
                  +---------------+--------------+
                                  |
          +-----------------------+------------------------+
          |                        |                        |
          v                        v                        v
+------------------+   +------------------------+  +----------------------+
| Master DB Access |   | Tenant DB Access       |  | Holding DB Access    |
| Firma/Kullanici  |   | Firma bazlı runtime    |  | Konsolidasyon        |
+------------------+   +------------------------+  +----------------------+
                                  ^
                                  |
                                  |
                  +---------------+--------------+
                  | Tenant Connection Resolver   |
                  | Firma -> DatabaseName -> DB  |
                  +------------------------------+
```

---

## 2. Hibrit Kanal Diyagramı

```text
+----------------------+   +----------------------+   +----------------------+
| Web                  |   | Desktop              |   | Mobile               |
| Blazor Server        |   | WPF / WinUI          |   | .NET MAUI            |
+----------------------+   +----------------------+   +----------------------+
| ERP                  |   | Puantaj Terminali    |   | Sevkiyat             |
| Muhasebe             |   | Barkod Operasyonları |   | Saha İşlemleri       |
| Bütçe                |   | Veri Aktarımı        |   | Mobil Görevler       |
| Stok                 |   | Offline Giriş        |   | Offline Sync         |
| Cari / CRM           |   | Cihaz Entegrasyonu   |   | Konum / Teslim       |
| Fatura / Rapor       |   |                      |   |                      |
+----------------------+   +----------------------+   +----------------------+
```

---

## 3. Veritabanı Diyagramı

```text
+-----------------------------+
| KOAFiloServis_Master        |
|-----------------------------|
| Firmalar                    |
| Kullanicilar                |
| Roller                      |
| Lisanslar                   |
| Sistem ayarlari             |
+-------------+---------------+
              |
              | Firma / DatabaseName eşleme
              v
+-----------------------------+
| Koa_[FirmaKodu]_[Id]        |
|-----------------------------|
| Cari                        |
| Muhasebe                    |
| Bütçe                       |
| Stok                        |
| Araç / Şoför / Güzergah     |
| Puantaj                     |
| Fatura                      |
| Operasyon                   |
+-----------------------------+

+-----------------------------+
| DestekCRMServisBlazorDb     |
|-----------------------------|
| Legacy ortak veri havuzu    |
| Sadece transfer kaynağı     |
+-----------------------------+

+-----------------------------+
| KOAFiloServis_Holding       |
|-----------------------------|
| Konsolidasyon verileri      |
| Çok firma özet raporları    |
+-----------------------------+
```

---

## 4. Tenant Çözümleme Akışı

```text
Login
  ↓
Aktif Firma Seçimi
  ↓
IAktifFirmaProvider
  ↓
FirmaId / FirmaKodu / DatabaseName
  ↓
TenantConnectionStringProvider
  ↓
TenantDbContextFactory
  ↓
ApplicationDbContext
  ↓
Tenant DB CRUD
```

---

## 5. Veri Aktarım Diyagramı

```text
+------------------------------+
| TransferDesktop              |
|------------------------------|
| Firma seçimi                 |
| Dry-run                      |
| Validation                   |
| Conflict log                 |
| Transfer raporu              |
+---------------+--------------+
                |
                | read only
                v
+------------------------------+
| DestekCRMServisBlazorDb      |
| Legacy source                |
+---------------+--------------+
                |
                | map by FirmaKodu
                v
+------------------------------+
| Target Tenant DB             |
| Koa_[FirmaKodu]_[Id]         |
+------------------------------+
```

---

## 6. Desktop Transfer İç Akış

```text
Firma seç
  ↓
Master DB'den hedef tenant bilgisi çöz
  ↓
Legacy DB kaynak satırlarını oku
  ↓
Dry-run raporu üret
  ↓
FK / tablo / bağımlılık doğrula
  ↓
Tenant DB'ye kopyala
  ↓
ON CONFLICT sonuçlarını logla
  ↓
Transfer raporu oluştur
```

---

## 7. Kullanım Notu

Bu doküman draw.io, Excalidraw, Visio veya Mermaid tabanlı çizimlere temel metin olarak kullanılabilir.
