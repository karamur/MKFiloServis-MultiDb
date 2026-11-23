# MKFiloServis

![MKFiloServis — Filo, kiralama ve servis yönetimi için çok firmalı ERP](docs/assets/mkfiloservis-cover.svg)

MKFiloServis, birden fazla firmanın araçlarını, kiralama sözleşmelerini, servis operasyonlarını ve mali kayıtlarını tek bir web uygulamasında yönetmek için geliştirilmiş bir ERP sistemidir. Kullanıcının çalışabileceği kayıtlar ve modüller; etkin firma, rol yetkileri ve lisans kapsamına göre belirlenir.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Blazor](https://img.shields.io/badge/Blazor-Interactive%20Server-512BD4?logo=blazor)](https://learn.microsoft.com/aspnet/core/blazor/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Testler](https://github.com/karamur/MKFiloServis-MultiDb/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/karamur/MKFiloServis-MultiDb/actions/workflows/tests.yml)

**[Windows sunucusuna kurulum](setup/README.md)** · **[Geliştirici başlangıcı](#geliştirici-başlangıcı)** · **[Yedekleme ve kurtarma](MKFiloServis.Web/DOSYA_RECOVERY_KILAVUZU.md)** · **[Güvenlik bildirimi](SECURITY.md)**

## Neler yapabilirsiniz?

| İş alanı | Örnek iş akışı |
| --- | --- |
| Filo ve servis | Araç kartından plaka, bakım ve evrak kayıtlarını takip edin. |
| Rent a Car | Tarih aralığına göre müsait araçları bulun; rezervasyonu kiralamaya dönüştürün, sözleşme bazında ödeme ve depozitoyu izleyin. |
| Cari ve finans | Cari hesapları, faturaları, ödemeleri ve borç/alacak hareketlerini firma bazında takip edin. |
| Personel ve operasyon | Personel ve şoför kayıtlarını, operasyon planını ve puantajı yönetin. |
| Belge ve arşiv | Yetkiniz kapsamındaki araç ve personel evraklarını uygulamadan açın, ön izleyin veya indirin. |

Rent a Car sözleşmeleri ve teslim/iade tutanakları **taslaktır**. Yakıt, hasar, aksesuar ve imza bilgileri teslim sırasında ayrıca teyit edilmelidir.

## Windows sunucusuna kurulum

1. 64 bit Windows sunucusunda erişilebilir bir **PostgreSQL veritabanı** hazırlayın. Kurucu PostgreSQL kurmaz, veritabanı oluşturmaz ve yedek yüklemez. Mevcut, boş bir veritabanında uygulama ilk açılışta tabloları oluşturabilir; bağlantı kullanıcısına gerekli şema izinlerini verin.
2. Yeni sunucuda `MKFiloServisKurulum-1.0.48.exe` paketini yönetici olarak çalıştırın. Paket IIS'i yapılandırır ve gereken .NET 10 Hosting Bundle bileşenini kurar. **Yeni sunucuda yalnız Web MSI'ını çalıştırmayın:** ASP.NET Core IIS modülü (ANCM) eksik kalabilir.
3. Kurulum tamamlanınca sunucuda `http://localhost:5050` adresini açın. [İlk yönetici hesabı yönergelerini](setup/README.md#iis-kurulumunda-ilk-giriş) izleyin. Varsayılan hesap oluşturulduysa uygulamayı ağa açmadan önce parolasını değiştirin; dış erişim için HTTPS yapılandırın.

**Mevcut kurulum:** `MKFiloServisGuncelle-1.0.48.exe` uygulama dosyalarını günceller; mevcut veritabanı bağlantısı ve depolama dizini korunur. `MKLisansArac-1.0.48.msi` ise ayrı bir kullanıcı bazlı lisans yönetim aracıdır; web sunucusunu çalıştırmak için gerekli değildir.

## Geliştirici başlangıcı

Windows'ta [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ile aşağıdaki komutları çalıştırın. Yalnız web uygulamasını geliştirmek için MAUI istemcisini derlemeniz gerekmez.

```powershell
git clone https://github.com/karamur/MKFiloServis-MultiDb.git
cd MKFiloServis-MultiDb
dotnet build .\MKFiloServis.Web\MKFiloServis.Web.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start.ps1 -CheckOnly
```

`-CheckOnly`, uygulamayı başlatmadan ortamı ve portu kontrol eder. Geliştirme için **üretimden ayrı** bir SQLite veya PostgreSQL veritabanı yapılandırın, ardından `.\start.bat` komutunu çalıştırın. Başlatıcı varsayılan olarak 5050–5069 aralığında uygun portu seçer ve sağlık kontrolü başarılı olduğunda tarayıcıyı açar.

```powershell
# Birim ve entegrasyon testleri
dotnet test .\MKFiloServis.Tests\MKFiloServis.Tests.csproj

# Rent a Car iş kuralları için ayrı kontroller
dotnet run --project .\MKFiloServis.Web\Tests\RentACar\MKFiloServis.RentACarChecks.csproj
```

### Dağıtım paketlerini üretme

[WiX 6 ön koşullarını](setup/README.md) tamamladıktan sonra depo kökünden çalıştırın:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\setup\build-wix.ps1 -Version 1.0.48
```

Web MSI, kurulum ve güncelleme paketleri (WiX Burn) ile lisans aracı MSI'ı `setup\output\v1.0.48\` altında üretilir. Bu ikili dosyalar kaynak depoda tutulmaz.

## Mimari ve depo haritası

Web katmanı **ASP.NET Core Blazor Interactive Server** kullanır. Uygulama servisleri verilere **Entity Framework Core** ile erişir; arka plan işleri **Quartz.NET** ile yürütülür. Web uygulaması desteklenen tarayıcılarda PWA olarak da kurulabilir; işlem yapmak için sunucu bağlantısı gerekir.

| Bileşen | Sorumluluk |
| --- | --- |
| [Web](MKFiloServis.Web/) | Web arayüzü, API, iş kuralları, veri erişimi ve arka plan işleri |
| [Shared](MKFiloServis.Shared/) | Paylaşılan veri modelleri, sözleşmeler ve lisans doğrulama kuralları |
| [Client](MKFiloServis.Client/) | Windows ve Android için .NET MAUI istemcisi |
| [DataSync](MKFiloServis.DataSync/) | SQLite verilerinin PostgreSQL'e aktarımı; gerektiğinde uygulama şemasının hazırlanması |
| [LisansDesktop](MKFiloServis.LisansDesktop/) | Windows üzerinde lisans oluşturma ve yönetimi |
| [Tests](MKFiloServis.Tests/) | xUnit testleri ve izole veritabanı senaryoları |

## Yedekleme ve kurtarma

**Ayarlar → Yedekleme ve Kurtarma → Kurtarma Dizini Oluştur ve Yedekle** işlemi, PostgreSQL dökümünü, yerel arşivleri, ayarları ve dosya anahtarlarını bir kurtarma ZIP'inde toplar. Veritabanı ve dosyalar art arda kopyalandığından tutarlı bir yedek için işlemi yazımların durdurulduğu bakım penceresinde çalıştırın. PostgreSQL istemci araçları sunucuda bulunmalıdır; `pg_dump` sunucunun ana sürümünden eski olmamalıdır.

Varsayılan kurtarma dizini `C:\MKFiloServis_yedekleme\Kurtarma` yoludur. **Bu dizini ayrı, şifreli ve erişimi sınırlandırılmış bir ortama kopyalayın.** Kurtarma ZIP'i hassas bilgiler içerir ve kendi başına parola korumalı değildir. Anahtar dosyalarının bulunması, başka bir Windows makinesinde şifreli belgelerin açılacağını garanti etmez. Canlı geri yüklemeden önce [kurtarma kılavuzundaki](MKFiloServis.Web/DOSYA_RECOVERY_KILAVUZU.md) anahtar kontrolünü ve örnek belge açma provasını yapın.

Güvenlik açıklarını [güvenlik bildirim politikasına](SECURITY.md) göre bildirin. Hata raporlarına parola, bağlantı dizesi, özel anahtar, kurtarma ZIP'i veya müşteri verisi eklemeyin.

© MK Yazılım. Tüm hakları saklıdır.
