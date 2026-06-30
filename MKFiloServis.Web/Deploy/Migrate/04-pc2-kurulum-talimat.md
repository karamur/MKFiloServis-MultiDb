# KOA Filo Servis — 2. PC Kurulum Talimatı

> **Hazırlayan:** Otomatik oluşturuldu (`03-pc2-publish.ps1`)  
> **Versiyon:** .NET 10 · PostgreSQL 16 · IIS (in-process)

---

## Gereksinimler (2. PC'de kurulu olmalı)

| Bileşen | Minimum Sürüm | İndirme |
|---------|--------------|---------|
| Windows | 10/Server 2019+ | — |
| .NET 10 Hosting Bundle | 10.0.x | https://aka.ms/dotnet/download |
| PostgreSQL | 14+ (önerilen 16) | https://www.postgresql.org/download/windows/ |
| IIS + ASP.NET Core Module | — | `dism /online /enable-feature /featurename:IIS-WebServer` |
| PowerShell 7+ | 7.x | https://aka.ms/powershell |

---

## ADIM 1 — Dosyaları 2. PC'ye Taşıma

1. 1. PC'de oluşturulan ZIP paketini 2. PC'ye kopyalayın:
   ```
   KOAFiloServis-PC2-YYYYMMDD_HHmm.zip
   ```
2. ZIP'i `C:\KOAFiloServis\IIS` klasörüne **çıkartın**.

---

## ADIM 2 — Şifreli Evrakları Taşıma

1. PC'den eski şifreli evrakları kopyalayın:

   | Kaynak (1. PC) | Hedef (2. PC) |
   |---|---|
   | `C:\KOAFiloServis_yedekleme\uploads\` | `C:\KOAFiloServis_yedekleme\uploads\` |
   | `C:\KOAFiloServis_yedekleme\keys\key-*.xml` | `C:\KOAFiloServis_yedekleme\keys\` |
   | `C:\KOAFiloServis_yedekleme\keys\master.key` | `C:\KOAFiloServis_yedekleme\keys\` |

> **ÖNEMLİ:** Data protection `key-*.xml` dosyaları **şifreli evrakları çözmek için zorunludur**.  
> Bu dosyalar olmadan arşivdeki belgeler **açılamaz**.

---

## ADIM 3 — Veritabanı Restore

2. PC'de PostgreSQL kurulu ve çalışıyor olmalıdır.

```powershell
# Paket içindeki veya 1. PC'den kopyalanan script:
pwsh -ExecutionPolicy Bypass -File "C:\KOAFiloServis\IIS\01-db-restore.ps1" `
    -BackupFile   "BACKUP_DOSYASI_YOLU.backup" `
    -PgHost       "localhost" `
    -PgPort       "5432" `
    -PgUser       "postgres" `
    -PgPassword   "SİFRENİZ" `
    -NewDbName    "KOAFiloServis"
```

**Backup dosyası:** 1. PC'de `C:\KOAFiloServis_yedekleme\database\` altındaki en güncel `.backup` dosyası.

---

## ADIM 4 — appsettings Yapılandırması

`C:\KOAFiloServis\IIS\appsettings.PC2.json` dosyasını açın ve düzenleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=SIFRESINIZ;"
  },
  "Jwt": {
    "Secret": "EN_AZ_32_KARAKTER_GIZLI_ANAHTAR_YAZIN_BURAYA"
  },
  "Backup": {
    "Path": "C:\\KOAFiloServis_yedekleme\\database"
  }
}
```

Sonra **`appsettings.Production.json`** olarak kaydedin (varsa üzerine yazın).

---

## ADIM 5 — IIS Kurulumu

IIS'de yeni site oluşturmak için **Yönetici olarak** çalıştırın:

```batch
C:\KOAFiloServis\IIS\kur.bat "C:\KOAFiloServis\IIS" "C:\KOAFiloServis_yedekleme\deploy" "" "Install"
```

veya IIS Manager'dan:
- **Site Adı:** KOAFiloServis
- **Fiziksel Yol:** `C:\KOAFiloServis\IIS`
- **Port:** 80 (veya boş 443 + sertifika)
- **Uygulama Havuzu:** `No Managed Code`, 64-bit

---

## ADIM 6 — Lisans Aktivasyonu

1. Tarayıcıdan `http://localhost` adresine gidin.
2. Lisans ekranında lisans anahtarınızı girin.
3. **Bu PC için yeni bir makine kodu oluşur** — lisansınız bu koda bağlıdır.

> Her PC için ayrı lisans anahtarı gerekebilir. `Lisans/KOAFiloServisLisans.exe` uygulamasını kullanarak makine kodunu alın ve yeni anahtar oluşturun.

---

## ADIM 7 — Doğrulama

| Kontrol | Beklenen Sonuç |
|---------|----------------|
| `http://localhost` | Giriş sayfası açılır |
| Giriş yapılabilir | ✅ |
| Personel listesi dolu | ✅ (DB restore başarılı) |
| Evrak görüntüleme | ✅ (key'ler doğru konumda) |
| Sistem > Lisans | Aktif gösterir |

---

## Sorun Giderme

### Evraklar açılmıyor / şifre çözme hatası
- `C:\KOAFiloServis_yedekleme\keys\key-*.xml` dosyalarının mevcut olduğunu kontrol edin.
- Dosya izinleri: IIS uygulama havuzu kullanıcısı (`IIS AppPool\KOAFiloServis` veya `NETWORK SERVICE`) bu klasörü okuyabilmeli.

### Veritabanı bağlantı hatası
- PostgreSQL servisinin çalıştığını kontrol edin: `Get-Service postgresql*`
- `appsettings.Production.json` dosyasındaki şifreyi kontrol edin.

### Lisans makine kodu uyuşmuyor
- 2. PC için `Lisans/KOAFiloServisLisans.exe` ile yeni makine kodunu alın.
- Mevcut lisans anahtarı başka PC'ye bağlıysa yeni anahtar üretmeniz gerekir.

---

## Klasör Yapısı Özeti (2. PC)

```
C:\
├── KOAFiloServis\
│   └── IIS\                       ← Uygulama dosyaları (kur.bat ile kurulur)
│       ├── KOAFiloServis.Web.dll
│       ├── appsettings.json
│       ├── appsettings.Production.json  ← Siz düzenlediniz
│       └── web.config
│
└── KOAFiloServis_yedekleme\
    ├── uploads\                    ← Şifreli evraklar (1. PC'den kopyalandı)
    │   ├── evraklar\{id}\*.enc
    │   └── personel-ozluk\{id}\*.enc
    ├── keys\                       ← Data protection key'leri (1. PC'den kopyalandı!)
    │   ├── key-*.xml
    │   └── master.key
    └── database\                   ← DB yedekleri (uygulama otomatik oluşturur)
```
