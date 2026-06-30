# Playwright Test Procedures

## Amaç
Bu prosedürler, `CRMFiloServis.Web` uygulamasında kullanıcı girişini ve kritik ekran açılışlarını `Playwright` ile doğrulamak için hazırlanmıştır.

## Kapsam
`CRMFiloServis.PlaywrightSmoke` altındaki smoke testler şu akışları kontrol eder:

1. Anonim kullanıcı `destek-talepleri` açınca `login` sayfasına yönlenir.
2. Geçerli kullanıcı ile giriş yapılabilir.
3. `Destek Talepleri` listesi açılabilir.
4. Listede kayıt varsa ilk `Destek Talebi Detay` ekranı açılabilir.
5. `Bilgi Bankası` ekranı açılabilir.
6. `Destek Ayarları` ekranı açılabilir.

## Varsayılan test kullanıcısı
- Kullanıcı adı: `admin`
- Şifre: `admin123`

## Çalıştırma
Uygulama çalışırken aşağıdaki komut kullanılabilir:

```powershell
dotnet run --project CRMFiloServis.Web\Tests\PlaywrightSmoke\CRMFiloServis.PlaywrightSmoke.csproj -- http://127.0.0.1:5190
```

## Ortam değişkenleri
İstenirse test kullanıcı bilgileri ortam değişkenleriyle verilebilir:

- `CRMFILO_BASE_URL`
- `CRMFILO_TEST_USER`
- `CRMFILO_TEST_PASSWORD`

## Playwright kurulumu
İlk çalıştırmadan önce gerekirse tarayıcı paketlerini yükleyin:

```powershell
pwsh CRMFiloServis.Web\Tests\PlaywrightSmoke\bin\Debug\net10.0\playwright.ps1 install
```

veya paket geri yükleme sonrasında Playwright aracı üzerinden kurulum yapın.

## Not
- Detay ekranı testi veri bağımlıdır; listede talep yoksa bu adım `skip` olarak işaretlenir.
- Eski `Selenium` prosedürleri yerine bu akış kullanılmalıdır.
