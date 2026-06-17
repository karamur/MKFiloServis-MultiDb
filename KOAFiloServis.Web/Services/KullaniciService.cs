using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class KullaniciService : IKullaniciService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly AppAuthenticationStateProvider _authProvider;
    private readonly ILogger<KullaniciService> _logger;
    private readonly IEmailService _emailService;
    private readonly LicenseService _licenseService;
    private readonly UserManager<Kullanici> _userManager;

    public KullaniciService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        AppAuthenticationStateProvider authProvider,
        ILogger<KullaniciService> logger,
        IEmailService emailService,
        LicenseService licenseService,
        UserManager<Kullanici> userManager)
    {
        _contextFactory = contextFactory;
        _authProvider = authProvider;
        _logger = logger;
        _emailService = emailService;
        _licenseService = licenseService;
        _userManager = userManager;
    }

    #region CRUD

    public async Task<List<Kullanici>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Kullanicilar
            .Include(k => k.Rol)
            .OrderBy(k => k.AdSoyad)
            .ToListAsync();
    }

    public async Task<Kullanici?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Kullanicilar
            .Include(k => k.Rol)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<Kullanici?> GetByKullaniciAdiAsync(string kullaniciAdi)
    {
        var normalizedKullaniciAdi = (kullaniciAdi ?? string.Empty).Trim().ToUpperInvariant();
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Kullanicilar
            .Include(k => k.Rol)
            .FirstOrDefaultAsync(k => !k.IsDeleted && k.KullaniciAdi.ToUpper() == normalizedKullaniciAdi);
    }

    public async Task<Kullanici> CreateAsync(Kullanici kullanici, string sifre)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Kullanici adi kontrolu
        if (await context.Kullanicilar.AnyAsync(k => k.KullaniciAdi == kullanici.KullaniciAdi))
            throw new Exception("Bu kullanici adi zaten kayitli!");

        kullanici.CreatedAt = DateTime.UtcNow;

        var result = await _userManager.CreateAsync(kullanici, sifre);
        if (!result.Succeeded)
            throw new Exception(string.Join("; ", result.Errors.Select(e => e.Description)));

        return kullanici;
    }

    public async Task<Kullanici> KayitOlAsync(Kullanici kullanici, string sifre)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        if (!await _licenseService.CheckUserLimitAsync(await context.Kullanicilar.CountAsync(k => k.Aktif)))
            throw new Exception("Lisans kullanıcı limitine ulaşıldı.");

        var kullaniciAdi = (kullanici.KullaniciAdi ?? string.Empty).Trim();
        var email = (kullanici.Email ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(kullanici.AdSoyad))
            throw new Exception("Kullanıcı adı ve ad soyad zorunludur.");

        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("E-posta adresi zorunludur.");

        if (await context.Kullanicilar.AnyAsync(k => !k.IsDeleted && k.KullaniciAdi == kullaniciAdi))
            throw new Exception("Bu kullanıcı adı zaten kayıtlı!");

        if (await context.Kullanicilar.AnyAsync(k => !k.IsDeleted && k.Email != null && k.Email.ToUpper() == email.ToUpper()))
            throw new Exception("Bu e-posta adresi zaten kayıtlı!");

        var defaultRole = await context.Roller.FirstOrDefaultAsync(r => r.RolAdi == SistemRolleri.Kullanici);
        if (defaultRole == null)
            throw new Exception("Varsayılan kullanıcı rolü bulunamadı.");

        kullanici.KullaniciAdi = kullaniciAdi;
        kullanici.Email = email;
        kullanici.RolId = defaultRole.Id;
        kullanici.Aktif = true;

        return await CreateAsync(kullanici, sifre);
    }

    public async Task<Kullanici> UpdateAsync(Kullanici kullanici)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Kullanicilar.FindAsync(kullanici.Id);
        if (existing == null) throw new Exception("Kullanici bulunamadi");

        existing.AdSoyad = kullanici.AdSoyad;
        existing.Email = kullanici.Email;
        existing.Telefon = kullanici.Telefon;
        existing.RolId = kullanici.RolId;
        existing.SoforId = kullanici.SoforId;
        existing.Aktif = kullanici.Aktif;
        existing.Tema = kullanici.Tema;
        existing.KompaktMod = kullanici.KompaktMod;
        existing.UpdatedAt = DateTime.UtcNow;

        context.Kullanicilar.Update(existing);
        await context.SaveChangesAsync();
        return existing;
    }

    public async Task<Kullanici> ToggleAktifAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Kullanicilar.FindAsync(id);
        if (existing == null) throw new Exception("Kullanici bulunamadi");

        existing.Aktif = !existing.Aktif;
        existing.UpdatedAt = DateTime.UtcNow;

        context.Kullanicilar.Update(existing);
        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar.FindAsync(id);
        if (kullanici == null) return;

        kullanici.IsDeleted = true;
        kullanici.UpdatedAt = DateTime.UtcNow;
        context.Kullanicilar.Update(kullanici);
        await context.SaveChangesAsync();
    }

    public async Task<bool> SifremiUnuttumAsync(string kullaniciAdiVeyaEmail)
    {
        var giris = (kullaniciAdiVeyaEmail ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(giris))
            return false;

        using var context = await _contextFactory.CreateDbContextAsync();
        var normalized = giris.ToUpperInvariant();

        var kullanici = await context.Kullanicilar
            .Include(k => k.Rol)
            .FirstOrDefaultAsync(k => !k.IsDeleted &&
                                      (k.KullaniciAdi.ToUpper() == normalized ||
                                       (k.Email != null && k.Email.ToUpper() == normalized)));

        if (kullanici == null || !kullanici.Aktif || string.IsNullOrWhiteSpace(kullanici.Email))
            return false;

        var eskiHash = kullanici.SifreHash;
        var eskiKilitli = kullanici.Kilitli;
        var eskiBasarisizGiris = kullanici.BasarisizGirisSayisi;
        var geciciSifre = GenerateTemporaryPassword();

        try
        {
            kullanici.SifreHash = HashPassword(kullanici, geciciSifre);
            kullanici.Kilitli = false;
            kullanici.BasarisizGirisSayisi = 0;
            kullanici.UpdatedAt = DateTime.UtcNow;
            context.Kullanicilar.Update(kullanici);
            await context.SaveChangesAsync();

            var subject = "Koa Filo Servis - Geçici Şifre";
            var body = $@"
<p>Merhaba {kullanici.AdSoyad},</p>
<p>Hesabınız için geçici şifre oluşturuldu.</p>
<p><strong>Kullanıcı Adı:</strong> {kullanici.KullaniciAdi}<br />
<strong>Geçici Şifre:</strong> {geciciSifre}</p>
<p>Giriş yaptıktan sonra lütfen profil ekranından şifrenizi değiştirin.</p>
<p>Bu işlem size ait değilse sistem yöneticiniz ile iletişime geçin.</p>";

            var emailSent = await _emailService.SendEmailAsync(kullanici.Email, subject, body, true);
            if (!emailSent)
            {
                kullanici.SifreHash = eskiHash;
                kullanici.Kilitli = eskiKilitli;
                kullanici.BasarisizGirisSayisi = eskiBasarisizGiris;
                kullanici.UpdatedAt = DateTime.UtcNow;
                context.Kullanicilar.Update(kullanici);
                await context.SaveChangesAsync();
                return false;
            }

            _logger.LogInformation("Sifre sifirlama e-postasi gonderildi: {KullaniciAdi}", kullanici.KullaniciAdi);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sifre sifirlama islemi basarisiz: {Giris}", giris);
            return false;
        }
    }

    public async Task<IkiFaktorKurulumBilgisi> IkiFaktorKurulumBaslatAsync(int kullaniciId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar.FindAsync(kullaniciId);
        if (kullanici == null)
            throw new Exception("Kullanıcı bulunamadı.");

        if (string.IsNullOrWhiteSpace(kullanici.IkiFaktorSecretKey))
        {
            kullanici.IkiFaktorSecretKey = TwoFactorAuthenticatorHelper.GenerateSecretKey();
            kullanici.UpdatedAt = DateTime.UtcNow;
            context.Kullanicilar.Update(kullanici);
            await context.SaveChangesAsync();
        }

        var accountName = !string.IsNullOrWhiteSpace(kullanici.Email)
            ? kullanici.Email
            : kullanici.KullaniciAdi;

        return new IkiFaktorKurulumBilgisi
        {
            SecretKey = kullanici.IkiFaktorSecretKey,
            ManuelAnahtar = TwoFactorAuthenticatorHelper.FormatManualEntryKey(kullanici.IkiFaktorSecretKey),
            KurulumUri = TwoFactorAuthenticatorHelper.BuildSetupUri("Koa Filo Servis", accountName, kullanici.IkiFaktorSecretKey),
            IkiFaktorAktif = kullanici.IkiFaktorAktif
        };
    }

    public async Task IkiFaktorEtkinlestirAsync(int kullaniciId, string dogrulamaKodu)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar.FindAsync(kullaniciId);
        if (kullanici == null)
            throw new Exception("Kullanıcı bulunamadı.");

        if (string.IsNullOrWhiteSpace(kullanici.IkiFaktorSecretKey))
            throw new Exception("Önce iki faktörlü doğrulama kurulumu başlatılmalıdır.");

        if (!TwoFactorAuthenticatorHelper.ValidateCode(kullanici.IkiFaktorSecretKey, dogrulamaKodu))
            throw new Exception("Doğrulama kodu geçersiz.");

        kullanici.IkiFaktorAktif = true;
        kullanici.IkiFaktorEtkinlestirmeTarihi = DateTime.UtcNow;
        kullanici.UpdatedAt = DateTime.UtcNow;

        context.Kullanicilar.Update(kullanici);
        await context.SaveChangesAsync();
    }

    public async Task IkiFaktorDevreDisiBirakAsync(int kullaniciId, string dogrulamaKodu)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar.FindAsync(kullaniciId);
        if (kullanici == null)
            throw new Exception("Kullanıcı bulunamadı.");

        if (!kullanici.IkiFaktorAktif || string.IsNullOrWhiteSpace(kullanici.IkiFaktorSecretKey))
            throw new Exception("İki faktörlü doğrulama zaten kapalı.");

        if (!TwoFactorAuthenticatorHelper.ValidateCode(kullanici.IkiFaktorSecretKey, dogrulamaKodu))
            throw new Exception("Doğrulama kodu geçersiz.");

        kullanici.IkiFaktorAktif = false;
        kullanici.IkiFaktorSecretKey = null;
        kullanici.IkiFaktorEtkinlestirmeTarihi = null;
        kullanici.UpdatedAt = DateTime.UtcNow;

        context.Kullanicilar.Update(kullanici);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Giris/Cikis

    public async Task<KullaniciGirisSonuc> GirisYapAsync(string kullaniciAdi, string sifre)
    {
        var normalizedKullaniciAdi = (kullaniciAdi ?? string.Empty).Trim().ToUpperInvariant();
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar
            .Include(k => k.Rol)
            .FirstOrDefaultAsync(k => !k.IsDeleted && k.KullaniciAdi.ToUpper() == normalizedKullaniciAdi);

        if (kullanici == null)
        {
            _logger.LogWarning("Giris basarisiz - kullanici bulunamadi: {KullaniciAdi}", kullaniciAdi);
            return new KullaniciGirisSonuc { Basarili = false, Mesaj = "Kullanici bulunamadi" };
        }

        if (!kullanici.Aktif)
        {
            _logger.LogWarning("Giris basarisiz - kullanici aktif degil: {KullaniciAdi}", kullaniciAdi);
            return new KullaniciGirisSonuc { Basarili = false, Mesaj = "Kullanici aktif degil" };
        }

        if (kullanici.Kilitli)
        {
            _logger.LogWarning("Giris basarisiz - kullanici kilitli: {KullaniciAdi}", kullaniciAdi);
            return new KullaniciGirisSonuc { Basarili = false, Mesaj = "Kullanici kilitli. Yoneticiye basvurun." };
        }

        var parolaDogru = await _userManager.CheckPasswordAsync(kullanici, sifre);
        if (!parolaDogru)
        {
            kullanici.BasarisizGirisSayisi++;
            if (kullanici.BasarisizGirisSayisi >= 5)
            {
                kullanici.Kilitli = true;
                _logger.LogWarning("Kullanici kilitlendi (5 basarisiz deneme): {KullaniciAdi}", kullaniciAdi);
            }
            context.Kullanicilar.Update(kullanici);
            await context.SaveChangesAsync();

            _logger.LogWarning("Giris basarisiz - sifre hatali: {KullaniciAdi}", kullaniciAdi);
            return new KullaniciGirisSonuc { Basarili = false, Mesaj = "Sifre hatali" };
        }

        // Basarili giris
        kullanici.SonGirisTarihi = DateTime.UtcNow;
        kullanici.BasarisizGirisSayisi = 0;

        if (kullanici.IkiFaktorAktif && !string.IsNullOrWhiteSpace(kullanici.IkiFaktorSecretKey))
        {
            context.Kullanicilar.Update(kullanici);
            await context.SaveChangesAsync();

            return new KullaniciGirisSonuc
            {
                Basarili = false,
                IkiFaktorGerekli = true,
                BekleyenKullaniciId = kullanici.Id,
                IkiFaktorHedefi = !string.IsNullOrWhiteSpace(kullanici.Email) ? kullanici.Email : kullanici.KullaniciAdi,
                Mesaj = "Doğrulama kodunu girin."
            };
        }

        context.Kullanicilar.Update(kullanici);
        await context.SaveChangesAsync();

        // Authentication state guncelle - async versiyon
        await _authProvider.GirisYapAsync(kullanici);

        _logger.LogInformation("Basarili giris: {KullaniciAdi}, Rol: {Rol}",
            kullaniciAdi, kullanici.Rol?.RolAdi);

        return new KullaniciGirisSonuc { Basarili = true, Kullanici = kullanici };
    }

    public async Task<KullaniciGirisSonuc> IkiFaktorGirisiTamamlaAsync(int kullaniciId, string dogrulamaKodu)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar
            .Include(k => k.Rol)
            .FirstOrDefaultAsync(k => !k.IsDeleted && k.Id == kullaniciId);

        if (kullanici == null)
            return new KullaniciGirisSonuc { Basarili = false, Mesaj = "Kullanıcı bulunamadı." };

        if (!kullanici.IkiFaktorAktif || string.IsNullOrWhiteSpace(kullanici.IkiFaktorSecretKey))
            return new KullaniciGirisSonuc { Basarili = false, Mesaj = "İki faktörlü doğrulama etkin değil." };

        if (!TwoFactorAuthenticatorHelper.ValidateCode(kullanici.IkiFaktorSecretKey, dogrulamaKodu))
            return new KullaniciGirisSonuc { Basarili = false, Mesaj = "Doğrulama kodu geçersiz." };

        context.Kullanicilar.Update(kullanici);
        await context.SaveChangesAsync();

        await _authProvider.GirisYapAsync(kullanici);

        _logger.LogInformation("2FA ile basarili giris: {KullaniciAdi}", kullanici.KullaniciAdi);

        return new KullaniciGirisSonuc { Basarili = true, Kullanici = kullanici };
    }

    public async Task CikisYapAsync()
    {
        var aktifKullanici = _authProvider.GetAktifKullanici();
        var sessionId = _authProvider.GetSessionId();

        _logger.LogInformation("Cikis yapiliyor: {KullaniciAdi}, SessionId: {SessionId}",
            aktifKullanici?.KullaniciAdi, sessionId);

        await _authProvider.CikisYapAsync();
    }

    public Task<Kullanici?> GetAktifKullaniciAsync()
    {
        return Task.FromResult(_authProvider.GetAktifKullanici());
    }

    #endregion

    #region Sifre

    public async Task SifreDegistirAsync(int kullaniciId, string eskiSifre, string yeniSifre)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar.FindAsync(kullaniciId);
        if (kullanici == null) throw new Exception("Kullanici bulunamadi");

        var result = await _userManager.ChangePasswordAsync(kullanici, eskiSifre, yeniSifre);
        if (!result.Succeeded)
            throw new Exception(result.Errors.FirstOrDefault()?.Description ?? "Mevcut sifre hatali");

        kullanici.UpdatedAt = DateTime.UtcNow;
        context.Kullanicilar.Update(kullanici);
        await context.SaveChangesAsync();
    }

    public async Task SifreSifirlaAsync(int kullaniciId, string yeniSifre)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar.FindAsync(kullaniciId);
        if (kullanici == null) throw new Exception("Kullanici bulunamadi");

        kullanici.SifreHash = HashPassword(kullanici, yeniSifre);
        kullanici.Kilitli = false;
        kullanici.BasarisizGirisSayisi = 0;
        kullanici.UpdatedAt = DateTime.UtcNow;
        context.Kullanicilar.Update(kullanici);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Yetki

    public async Task<bool> YetkiVarMiAsync(int kullaniciId, string yetkiKodu)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar
            .Include(k => k.Rol)
            .ThenInclude(r => r.Yetkiler)
            .FirstOrDefaultAsync(k => k.Id == kullaniciId);

        if (kullanici == null) return false;
        if (kullanici.Rol.RolAdi == "Admin") return true; // Admin her seye yetkili

        return kullanici.Rol.Yetkiler.Any(y => y.YetkiKodu == yetkiKodu && y.Izin);
    }

    public async Task<List<string>> GetKullaniciYetkileriAsync(int kullaniciId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await context.Kullanicilar
            .Include(k => k.Rol)
            .ThenInclude(r => r.Yetkiler)
            .FirstOrDefaultAsync(k => k.Id == kullaniciId);

        if (kullanici == null) return new List<string>();
        if (kullanici.Rol.RolAdi == "Admin") return GetTumYetkiler();

        return kullanici.Rol.Yetkiler.Where(y => y.Izin).Select(y => y.YetkiKodu).ToList();
    }
    
    public async Task<HashSet<string>> GetCurrentUserYetkilerAsync()
    {
        try
        {
            var kullanici = await GetAktifKullaniciAsync();
            if (kullanici == null)
                return new HashSet<string>();
                
            // Admin ise tüm yetkiler
            if (kullanici.Rol?.RolAdi == "Admin")
                return new HashSet<string> { "*" };
                
            var yetkiler = await GetKullaniciYetkileriAsync(kullanici.Id);
            return yetkiler.ToHashSet();
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private List<string> GetTumYetkiler()
    {
        return Yetkiler.GetAll();
    }

    #endregion

    #region Roller

    public async Task<List<Rol>> GetRollerAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Roller
            .Include(r => r.Yetkiler)
            .OrderBy(r => r.RolAdi)
            .ToListAsync();
    }

    public async Task<Rol> CreateRolAsync(Rol rol)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        rol.CreatedAt = DateTime.UtcNow;
        context.Roller.Add(rol);
        await context.SaveChangesAsync();
        return rol;
    }

    public async Task<Rol> UpdateRolAsync(Rol rol)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Roller.FindAsync(rol.Id);
        if (existing == null) throw new Exception("Rol bulunamadi");
        if (existing.SistemRolu) throw new Exception("Sistem rolu duzenlenemez");

        existing.RolAdi = rol.RolAdi;
        existing.Aciklama = rol.Aciklama;
        existing.Renk = rol.Renk;
        existing.UpdatedAt = DateTime.UtcNow;

        context.Roller.Update(existing);
        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteRolAsync(int rolId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var rol = await context.Roller.FindAsync(rolId);
        if (rol == null) return;
        if (rol.SistemRolu) throw new Exception("Sistem rolu silinemez");

        // Rolu kullanan kullanici var mi?
        if (await context.Kullanicilar.AnyAsync(k => k.RolId == rolId))
            throw new Exception("Bu role atanmis kullanicilar var");

        rol.IsDeleted = true;
        rol.UpdatedAt = DateTime.UtcNow;
        context.Roller.Update(rol);
        await context.SaveChangesAsync();
    }

    public async Task<Rol> UpdateRolYetkileriAsync(int rolId, List<RolYetki> yetkiler)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var rol = await context.Roller
            .Include(r => r.Yetkiler)
            .FirstOrDefaultAsync(r => r.Id == rolId);

        if (rol == null) throw new Exception("Rol bulunamadi");

        // Mevcut yetkileri sil
        context.RolYetkileri.RemoveRange(rol.Yetkiler);

        // Yeni yetkileri ekle
        foreach (var yetki in yetkiler)
        {
            yetki.RolId = rolId;
            yetki.CreatedAt = DateTime.UtcNow;
            context.RolYetkileri.Add(yetki);
        }

        await context.SaveChangesAsync();
        return rol;
    }

    public async Task SetRolYetkileriAsync(int rolId, List<string> yetkiKodlari)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var rol = await context.Roller
            .Include(r => r.Yetkiler)
            .FirstOrDefaultAsync(r => r.Id == rolId);

        if (rol == null) throw new Exception("Rol bulunamadi");

        // Mevcut yetkileri sil
        context.RolYetkileri.RemoveRange(rol.Yetkiler);

        // Yeni yetkileri ekle
        foreach (var yetkiKodu in yetkiKodlari)
        {
            context.RolYetkileri.Add(new RolYetki
            {
                RolId = rolId,
                YetkiKodu = yetkiKodu,
                Izin = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    #endregion

    #region Seed

    public async Task SeedAdminAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Tum sistem rollerini olustur
        foreach (var rolTanim in SistemRolleri.GetAllRoles())
        {
            var mevcutRol = await context.Roller.FirstOrDefaultAsync(r => r.RolAdi == rolTanim.Name);
            if (mevcutRol == null)
            {
                var yeniRol = new Rol
                {
                    RolAdi = rolTanim.Name,
                    Aciklama = rolTanim.Description,
                    Renk = rolTanim.Color,
                    SistemRolu = rolTanim.Name == SistemRolleri.Admin || rolTanim.Name == SistemRolleri.Kullanici,
                    CreatedAt = DateTime.UtcNow
                };
                context.Roller.Add(yeniRol);
                await context.SaveChangesAsync();

                // Varsayilan yetkileri ata
                var varsayilanYetkiler = SistemRolleri.GetDefaultPermissions(rolTanim.Name);
                foreach (var yetkiKodu in varsayilanYetkiler)
                {
                    context.RolYetkileri.Add(new RolYetki
                    {
                        RolId = yeniRol.Id,
                        YetkiKodu = yetkiKodu,
                        Izin = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await context.SaveChangesAsync();
            }
            else if (string.IsNullOrEmpty(mevcutRol.Renk))
            {
                // Mevcut role renk ekle
                mevcutRol.Renk = rolTanim.Color;
                mevcutRol.Aciklama = rolTanim.Description;
                await context.SaveChangesAsync();
            }
        }

        // Yeni eklenen sistem yetkilerini mevcut rollere eksikse ekle (mevcut ozellestirmeleri ezmeden)
        await EnsureRolePermissionAsync(context, SistemRolleri.Operasyon, Yetkiler.PersonelBorcSil);
        await EnsureRolePermissionAsync(context, SistemRolleri.Operasyon, Yetkiler.TedarikciServisOperasyonOku);
        await EnsureRolePermissionAsync(context, SistemRolleri.Operasyon, Yetkiler.TedarikciAraclariOku);
        await EnsureRolePermissionAsync(context, SistemRolleri.Operasyon, Yetkiler.TedarikciPersonelOku);
        await EnsureRolePermissionAsync(context, SistemRolleri.Operasyon, Yetkiler.TedarikciAracEvraklariOku);

        // Admin kullanici olustur veya sifresini dogrula
        var adminRol = await context.Roller.FirstOrDefaultAsync(r => r.RolAdi == SistemRolleri.Admin);
        if (adminRol != null)
        {
            var adminUser = await context.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciAdi == "admin");
            if (adminUser == null)
            {
                adminUser = new Kullanici
                {
                    KullaniciAdi = "admin",
                    SifreHash = HashPassword(new Kullanici { KullaniciAdi = "admin", AdSoyad = "Sistem Yoneticisi" }, "admin123"),
                    AdSoyad = "Sistem Yoneticisi",
                    RolId = adminRol.Id,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Kullanicilar.Add(adminUser);
                await context.SaveChangesAsync();
            }
            else
            {
                // Admin zaten varsa sadece kilitli/pasif durumunu duzelt.
                // ŞIFREYE DOKUNMA — admin kendi belirledigi sifreyi kullanmaya devam etsin.
                var adminGuncelle = false;
                if (!adminUser.Aktif)
                {
                    adminUser.Aktif = true;
                    adminGuncelle = true;
                }
                if (adminUser.Kilitli)
                {
                    adminUser.Kilitli = false;
                    adminUser.BasarisizGirisSayisi = 0;
                    adminGuncelle = true;
                }
                if (adminGuncelle)
                {
                    adminUser.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
        }

        // TEST kullanici olustur - hizli giris icin
        if (adminRol != null)
        {
            var testUser = await context.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciAdi == "test");
            if (testUser == null)
            {
                testUser = new Kullanici
                {
                    KullaniciAdi = "test",
                    SifreHash = HashPassword(new Kullanici { KullaniciAdi = "test", AdSoyad = "Test Kullanici" }, "test123"),
                    AdSoyad = "Test Kullanici",
                    RolId = adminRol.Id,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Kullanicilar.Add(testUser);
                await context.SaveChangesAsync();
            }
            else
            {
                // Test kullanici zaten varsa sadece kilitli/pasif durumunu duzelt.
                // ŞIFREYE DOKUNMA.
                var testGuncelle = false;
                if (!testUser.Aktif)
                {
                    testUser.Aktif = true;
                    testGuncelle = true;
                }
                if (testUser.Kilitli)
                {
                    testUser.Kilitli = false;
                    testUser.BasarisizGirisSayisi = 0;
                    testGuncelle = true;
                }
                if (testGuncelle)
                {
                    testUser.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
        }
    }

    #endregion

    #region Helpers

    private string HashPassword(Kullanici kullanici, string password)
    {
        return _userManager.PasswordHasher.HashPassword(kullanici, password);
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var bytes = new byte[10];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);

        var passwordChars = bytes
            .Select(b => chars[b % chars.Length])
            .ToArray();

        return new string(passwordChars);
    }

    private static async Task EnsureRolePermissionAsync(ApplicationDbContext context, string roleName, string yetkiKodu)
    {
        var rol = await context.Roller
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RolAdi == roleName);

        if (rol == null)
            return;

        var yetkiVar = await context.RolYetkileri
            .AnyAsync(y => y.RolId == rol.Id && y.YetkiKodu == yetkiKodu && y.Izin);

        if (yetkiVar)
            return;

        context.RolYetkileri.Add(new RolYetki
        {
            RolId = rol.Id,
            YetkiKodu = yetkiKodu,
            Izin = true,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    #endregion
}
