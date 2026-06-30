# Selenium Test Procedures

## Amaç
Bu prosedürler, Blazor tabanlı `CRMFiloServis.Web` uygulamasında login, yetkisiz yönlendirme ve temel oturum davranışlarını yerelde otomatik ve manuel olarak doğrulamak için hazırlanmıştır.

## Otomatik smoke test kapsamı
`CRMFiloServis.SeleniumTests` projesi şu akışları otomatik doğrular:

1. Anonim kullanıcı korumalı sayfaya gidince `login` sayfasına yönlenir.
2. Geçerli kullanıcı ile giriş yapılabilir.
3. `Beni Hatırla` işaretliyken çıkış sonrası kullanıcı adı tekrar doldurulur.

## Yerel çalışma notu
Bu testler GitHub'a göndermeden yerelde çalıştırılabilir. Testler uygulama `http://127.0.0.1:5190` üzerinde açık değilse uygulamayı kendisi başlatmayı dener.

## Test kullanıcısı
- Kullanıcı adı: `admin`
- Şifre: `admin123`

## Manuel prosedürler

### MP-01 Menü linkleri login'e düşüyor mu
1. `admin` ile giriş yap.
2. Sol menüde görünür olan modüllere sırayla tıkla.
3. Sağ üst menüden `Lisans Bilgileri` ve `Veritabani Ayarlari` bağlantılarını aç.
4. Her adımda URL ve ekran başlığını kontrol et.

Beklenen sonuç:
- Yetkili kullanıcı login ekranına geri düşmemeli.
- Yetkisiz durumda `Yetkiniz yok` veya ilgili erişim davranışı görülmeli.

### MP-02 Sayfa yenileme sonrası oturum korunuyor mu
1. Giriş yap.
2. Korumalı bir sayfaya git.
3. Tarayıcıda tam yenileme yap.

Beklenen sonuç:
- Uygulama oturumu koruyorsa aynı sayfada kalmalı.
- Login'e düşerse auth state kalıcılığı sorunu vardır.

### MP-03 Çıkış davranışı
1. Giriş yap.
2. `Cikis` butonuna bas.
3. Doğrudan korumalı bir URL açmayı dene.

Beklenen sonuç:
- Kullanıcı `login` ekranına yönlenmeli.

## Çalıştırma
Visual Studio Test Explorer veya `dotnet test CRMFiloServis.SeleniumTests/CRMFiloServis.SeleniumTests.csproj` ile çalıştırılabilir.
