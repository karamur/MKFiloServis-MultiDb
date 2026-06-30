# 🔄 Şifrelenmiş Dosya Recovery Kılavuzu

## Problem: Master Key Değişikliği

Uygulamanın **master.key** dosyası regenerate olduğunda, eski anahtarla şifrelenmiş tüm dosyalar artık açılamaz:

```
❌ Dosya decrypt edilemedi: desteklenen KOA1 formatları veya anahtar eşleşmiyor.
```

## Çözüm: Batch Recovery (Re-encrypt Workflow)

Recovery işlemi:
1. **Eski master key'i kaynak olarak sağla** (HEX string)
2. **Eski key ile dosyaları decrypt et**
3. **Yeni key ile yeniden şifrele (re-encrypt)**
4. **Orijinal dosya konumlarında güncelle**

## Adım Adım Kurtarma

### 1. Eski Master Key'i Bul

Eski key genelde backup konumunda saklanır:

```powershell
# Standart backup konumu
Get-Content "C:\MKFiloServis_yedekleme\keys\raw-key.txt.bak" -Raw
```

**Çıktı örneği:**
```
EE4461E47C5DDF6C5750EE9403735642171A5399FD9292F0C7631B2F181B8415
```

Bu 64 karakterlik HEX string'i saklayın.

### 2. Admin Dashboard'a Git

```
http://localhost:5000/admin/system-health
```

Sayfada "Şifrelenmiş Dosya Recovery" kartını bulun.

### 3. Eski Key'i Yapıştır

"Eski Master Key (HEX)" alanına HEX string'i yapıştırın:

```
EE4461E47C5DDF6C5750EE9403735642171A5399FD9292F0C7631B2F181B8415
```

### 4. Recovery Başlat

"🔄 Recovery Başlat" butonuna tıklayın.

**Tahmini Zaman:** 100 dosya = ~10-30 saniye

### 5. Sonuç Raporu

Recovery sonrası:
- ✅ **Başarılı**: Re-encrypt edilen dosya sayısı
- ❌ **Başarısız**: Decrypt edilemeyen dosya sayısı (format hatası vs.)
- ⏭️ **Skip**: Zaten yeni key ile şifrelenmiş dosyalar

Başarılı dosyalar listesi aşağıda gösterilir.

---

## REST API (Programmatik Kullanım)

### Endpoint

```
POST /api/system/recover-encrypted-files
```

### Request Body

```json
{
  "oldMasterKeyHex": "EE4461E47C5DDF6C5750EE9403735642171A5399FD9292F0C7631B2F181B8415",
  "targetDirectory": null
}
```

- **oldMasterKeyHex** (required): 64 char HEX string (32 byte)
- **targetDirectory** (optional): Taranacak dizin (Arsiv/ relative). Null = tüm Personel + Arac

### Response (200 OK)

```json
{
  "successCount": 245,
  "failedCount": 0,
  "skippedCount": 3,
  "recoveredFiles": [
    "Arsiv/Sifreli/Araclar/06C0640Ruhsat_20260429_073253.enc",
    "Arsiv/Sifreli/Personeller/ASDF_20260410_115423.enc",
    ...
  ],
  "failedFiles": [],
  "isSuccess": true
}
```

### PowerShell Örneği

```powershell
$oldKey = 'EE4461E47C5DDF6C5750EE9403735642171A5399FD9292F0C7631B2F181B8415'
$request = @{
    'oldMasterKeyHex' = $oldKey
    'targetDirectory' = $null
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri 'http://localhost:5000/api/system/recover-encrypted-files' `
    -Method POST `
    -Headers @{ 'Content-Type' = 'application/json' } `
    -Body $request

Write-Host "Başarılı: $($response.successCount)"
Write-Host "Başarısız: $($response.failedCount)"
```

---

## Teknik Detaylar

### AES-256-GCM Format Uyumluluğu

**FileRecoveryService** aşağıdaki KOA1 varyantlarını destekler:

1. **Yeni Format (v1)**: `KOA1 | VER(0x01) | NONCE(12B) | TAG(16B) | CIPHER`
2. **Eski Format (Versiyonsuz)**: `KOA1 | NONCE(12B) | TAG(16B) | CIPHER`
3. **Legacy Varyant**: `KOA1 | NONCE(12B) | CIPHER | TAG(16B)`

**DpapiMasterKeyProvider** ayrıca eski raw-key backup dosyalarını otomatik olarak tarar:
- `raw-key.txt`
- `raw-key.txt.bak`
- `master.key.raw`
- `master.key.txt`

Bulunursa, yeni anahtarla DPAPI LocalMachine'de yeniden kaydedilir (self-healing).

### Tarama Dizinleri

Recovery varsayılan olarak şu dizinleri tarar:
- `C:\MKFiloServis_yedekleme\Arsiv\Sifreli\Personeller\` → Personel evrakları
- `C:\MKFiloServis_yedekleme\Arsiv\Sifreli\Araclar\` → Araç evrakları

---

## Sorun Giderme

### "Master key HEX string'i boş olamaz"

**Çözüm**: Eski key dosyasının doğru konumunu kontrol edin.

```powershell
Test-Path "C:\MKFiloServis_yedekleme\keys\raw-key.txt.bak"
```

### "Eski key hex string 64 karakter olmalıdır"

**Çözüm**: Key 32 byte = 64 HEX karakter olmalıdır. Format kontrol edin:

```powershell
$key = Get-Content "C:\MKFiloServis_yedekleme\keys\raw-key.txt.bak"
Write-Host "Uzunluk: $($key.Trim().Length)"  # 64 olmalı
```

### "Recovery başarısız: desteklenen KOA1 formatları veya anahtar eşleşmiyor"

**Çözüm**: Sağlanan eski key, dosyaları şifreleyen key ile eşleşmiyor olabilir:

1. Backup konumlarında başka key dosyası arayın
2. Dosya format varyantını kontrol edin (legacy layout)
3. Dosya bozulmuş olabilir (system crash vs.)

### Dosyalar hala açılamıyor

**Sonraki adımlar**:
1. Recovery log'larını kontrol edin: `C:\MKFiloServis_yedekleme\logs\`
2. Failed files detaylarını kontrol edin (response'de listelenir)
3. FileRecoveryService tanılama çıkışını gözden geçirin

---

## Dosya Yapısı

### Recovery Service Kodu

```
MKFiloServis.Web/
  Services/
    FileRecoveryService.cs          ← Batch recovery engine
    Security/
      DpapiMasterKeyProvider.cs     ← Eski key fallback + re-saving
      AesGcmFileProtector.cs        ← AES-256-GCM şifreleme
    SecureFileService.cs            ← Override-key overload eklendi
  Controllers/
    SystemHealthController.cs       ← POST /api/system/recover-encrypted-files
  Components/Pages/
    AdminSystemHealth.razor         ← Admin UI dashboard
    AdminSystemHealth.razor.cs      ← Recovery logic
```

### DI Registration

```csharp
// Program.cs
builder.Services.AddScoped<FileRecoveryService>();
```

---

## İlgili Belgeler

- MASTER_KEY_STRATEGY.md — Master key yönetim stratejisi
- ENCRYPTION_FORMATS.md — KOA1 format detayları
- SECURITY_ARCHITECTURE.md — Genel güvenlik mimarisi

