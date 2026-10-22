using System.Text;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Destek;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Destek talebi servisi. Akış kuralları <see cref="DestekKurallari"/>, erişim kuralları <see cref="DestekErisim"/>'dedir;
/// her işlem oturumdaki kullanıcıyı <see cref="IDestekKullaniciSaglayici"/> ile yeniden doğrular.
/// Bu dosya: talep açma, detay, yazışma ve ekler. Diğer parçalar: <c>.Akis</c>, <c>.Tanimlar</c>, <c>.Rapor</c>.
/// </summary>
public partial class DestekTalebiService : IDestekTalebiService
{
    private const int AramaAdayLimiti = 1000;
    private const int TeknikMetinLimiti = 6000;

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<DestekTalebiService> _logger;
    private readonly IEmailService _emailService;
    private readonly NumaraSerisiService _numaraSerisi;
    private readonly ISecureFileService _secureFileService;
    private readonly IDestekKullaniciSaglayici _kullaniciSaglayici;
    private readonly string _uploadPath;

    public DestekTalebiService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<DestekTalebiService> logger,
        IWebHostEnvironment env,
        IEmailService emailService,
        NumaraSerisiService numaraSerisi,
        ISecureFileService secureFileService,
        IDestekKullaniciSaglayici kullaniciSaglayici)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _emailService = emailService;
        _numaraSerisi = numaraSerisi;
        _secureFileService = secureFileService;
        _kullaniciSaglayici = kullaniciSaglayici;
        _uploadPath = Path.Combine(env.WebRootPath, "uploads", "destek");

        // Eski düz dosya ekleri bu klasördedir (yeni ekler şifreli depoya yazılır).
        if (!Directory.Exists(_uploadPath))
            Directory.CreateDirectory(_uploadPath);
    }

    public Task<DestekKullanici?> GetAktifKullaniciAsync() => _kullaniciSaglayici.GetirAsync();

    #region Talep eden

    public async Task<DestekTalebi> TalepOlusturAsync(DestekTalebi girdi, bool taslak)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talep = new DestekTalebi
        {
            Konu = girdi.Konu,
            Aciklama = girdi.Aciklama,
            DepartmanId = girdi.DepartmanId,
            KategoriId = girdi.KategoriId,
            Oncelik = girdi.Oncelik,
            Kaynak = DestekKaynak.Web,
            OlusturanKullaniciId = aktor.Id,
            MusteriAdi = string.IsNullOrWhiteSpace(girdi.MusteriAdi) ? aktor.AdSoyad : girdi.MusteriAdi,
            MusteriEmail = string.IsNullOrWhiteSpace(girdi.MusteriEmail) ? aktor.Email ?? string.Empty : girdi.MusteriEmail,
            MusteriTelefon = girdi.MusteriTelefon,
            Durum = taslak ? DestekDurum.Taslak : DestekDurum.Yeni
        };

        await TalepAlanlariniDogrulaAsync(context, talep);
        await YeniTalepKaydetAsync(context, talep, aktor, taslak ? "Talep taslak olarak kaydedildi" : "Talep gönderildi");
        return talep;
    }

    public async Task<DestekTalebi> TaslakGuncelleAsync(DestekTalebi girdi, bool gonder)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talep = await TalepYukleAsync(context, girdi.Id);
        if (talep.OlusturanKullaniciId != aktor.Id)
            throw YetkiYok("Yalnız kendi taslağınızı düzenleyebilirsiniz.");
        if (talep.Durum != DestekDurum.Taslak)
            throw new InvalidOperationException("Gönderilmiş talep düzenlenemez; bilgi eklemek için talebe mesaj yazın.");

        talep.Konu = girdi.Konu;
        talep.Aciklama = girdi.Aciklama;
        talep.DepartmanId = girdi.DepartmanId;
        talep.KategoriId = girdi.KategoriId;
        talep.Oncelik = girdi.Oncelik;
        talep.MusteriEmail = string.IsNullOrWhiteSpace(girdi.MusteriEmail) ? aktor.Email ?? string.Empty : girdi.MusteriEmail;
        talep.MusteriTelefon = girdi.MusteriTelefon;
        await TalepAlanlariniDogrulaAsync(context, talep);

        var simdi = DateTime.UtcNow;
        talep.UpdatedAt = simdi;
        talep.SonAktiviteTarihi = simdi;

        if (gonder)
        {
            // Süreler (ilk yanıt, çözüm, SLA) talebin gönderildiği andan ölçülür, taslağın açıldığı andan değil.
            talep.CreatedAt = simdi;
            talep.Durum = DestekDurum.Yeni;
            await SlaUygulaAsync(context, talep, simdi);
            Aktivite(context, talep, AktiviteTuru.DurumDegisti, "Talep gönderildi", aktor.Id,
                DestekKurallari.Etiket(DestekDurum.Taslak), DestekKurallari.Etiket(DestekDurum.Yeni));
        }
        else
        {
            Aktivite(context, talep, AktiviteTuru.Guncellendi, "Taslak güncellendi", aktor.Id);
        }

        await context.SaveChangesAsync();

        if (gonder)
            await GonderilenTalebiDuyurAsync(context, talep, aktor);

        return talep;
    }

    public async Task TaslakSilAsync(int talepId)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talep = await TalepYukleAsync(context, talepId);
        if (talep.OlusturanKullaniciId != aktor.Id && !aktor.Silebilir)
            throw YetkiYok("Yalnız kendi taslağınızı silebilirsiniz.");
        if (talep.Durum != DestekDurum.Taslak && !aktor.Silebilir)
            throw new InvalidOperationException("Yalnız taslak talepler silinebilir.");

        talep.IsDeleted = true;
        talep.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task YeniTalebiIptalEtAsync(int talepId)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var talep = await TalepYukleAsync(context, talepId);
        if (talep.OlusturanKullaniciId != aktor.Id)
            throw YetkiYok("Yalnız kendi talebinizi iptal edebilirsiniz.");
        if (talep.Durum != DestekDurum.Yeni)
            throw new InvalidOperationException("Yalnız yeni durumundaki talepler iptal edilebilir.");

        var simdi = DateTime.UtcNow;
        talep.Durum = DestekDurum.IptalEdildi;
        talep.KapatilmaTarihi = simdi;
        talep.SonAktiviteTarihi = simdi;
        talep.UpdatedAt = simdi;
        Aktivite(context, talep, AktiviteTuru.IptalEdildi, "Talep sahibi talebi iptal etti", aktor.Id);
        await context.SaveChangesAsync();
        await BildirAsync(context, async () =>
            await DestekBildirimci.EkleAsync(context, await EkipIlgilileriAsync(context, talep), aktor.Id, talep,
                "Talep sahibi talebi iptal etti", talep.Konu, ekipLinki: true));
    }

    public async Task<DestekTalebi> HataBildirimiOlusturAsync(SorunBildirimi bildirim)
    {
        var aktor = await OturumGerekliAsync();
        if (string.IsNullOrWhiteSpace(bildirim.Aciklama))
            throw new InvalidOperationException("Lütfen ne yapmaya çalıştığınızı ve ne olduğunu kısaca yazın.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayarlar = await AyarlariOkuAsync(context);

        // Ek, talep açılmadan önce denetlenir: uygunsuz dosya yüzünden yarım talep oluşmasın.
        if (bildirim.EkIcerik is { Length: > 0 })
            EkDosyasiniDenetle(bildirim.EkDosyaAdi ?? "ekran-goruntusu.png", bildirim.EkIcerik.LongLength, ayarlar);

        var departmanId = await VarsayilanDepartmanAsync(context, ayarlar)
            ?? throw new InvalidOperationException("Destek departmanı tanımlı değil; lütfen sistem yöneticinize başvurun.");

        var konu = !string.IsNullOrWhiteSpace(bildirim.Konu)
            ? bildirim.Konu
            : !string.IsNullOrWhiteSpace(bildirim.HataMesaji)
                ? $"Hata: {DestekBildirimci.Kisalt(bildirim.HataMesaji, 150)}"
                : $"Sorun bildirimi: {bildirim.Sayfa ?? "program"}";

        var talep = new DestekTalebi
        {
            Konu = konu,
            Aciklama = HataAciklamasi(bildirim),
            DepartmanId = departmanId,
            Oncelik = bildirim.Oncelik,
            Kaynak = DestekKaynak.HataBildirimi,
            Durum = DestekDurum.Yeni,
            OlusturanKullaniciId = aktor.Id,
            MusteriAdi = aktor.AdSoyad,
            MusteriEmail = aktor.Email ?? string.Empty
        };
        await TalepAlanlariniDogrulaAsync(context, talep);

        var teknikNot = new DestekTalebiYanit
        {
            Icerik = TeknikNot(bildirim),
            DahiliNot = true,
            YanitTuru = YanitTuru.SistemMesaji
        };

        await YeniTalepKaydetAsync(context, talep, aktor, "Program içinden sorun bildirildi", [teknikNot]);

        if (bildirim.EkIcerik is { Length: > 0 })
        {
            try
            {
                await EkKaydetAsync(context, talep, aktor, bildirim.EkDosyaAdi ?? "ekran-goruntusu.png",
                    bildirim.EkMimeTipi ?? "image/png", bildirim.EkIcerik, ayarlar);
            }
            catch (Exception ex)
            {
                // Talep açıldı; ek yazılamadıysa kullanıcı talebe sonradan dosya ekleyebilir.
                _logger.LogWarning(ex, "Sorun bildiriminin eki kaydedilemedi. TalepId={TalepId}", talep.Id);
            }
        }

        return talep;
    }

    public async Task<List<DestekTalebi>> GetTaleplerimAsync()
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talepler = await context.DestekTalepleri
            .AsNoTracking()
            .Where(x => x.OlusturanKullaniciId == aktor.Id)
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .Select(x => new DestekTalebi
            {
                Id = x.Id,
                TalepNo = x.TalepNo,
                Konu = x.Konu,
                Durum = x.Durum,
                Kaynak = x.Kaynak,
                SonAktiviteTarihi = x.SonAktiviteTarihi
            })
            .ToListAsync();
        Normallestir(talepler);
        return talepler;
    }

    #endregion

    #region Detay, yazışma ve ekler

    public async Task<DestekTalepDetayi?> GetDetayAsync(int talepId)
    {
        var aktor = await _kullaniciSaglayici.GetirAsync();
        if (aktor == null) return null;

        return await DetayGetirAsync(talepId, aktor, ekipEkrani: false);
    }

    public async Task<DestekTalepDetayi?> GetEkipDetayAsync(int talepId)
    {
        var aktor = await EkipUyesiGerekliAsync();
        return await DetayGetirAsync(talepId, aktor, ekipEkrani: true);
    }

    private async Task<DestekTalepDetayi?> DetayGetirAsync(int talepId, DestekKullanici aktor, bool ekipEkrani)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var talep = await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Include(x => x.AtananKullanici)
            .Include(x => x.OlusturanKullanici)
            .Include(x => x.Cari)
            .FirstOrDefaultAsync(x => x.Id == talepId);
        if (talep == null) return null;
        talep.Durum = DestekKurallari.Normallestir(talep.Durum);
        if (!ekipEkrani && talep.OlusturanKullaniciId != aktor.Id) return null;

        var yazilimDepartmanId = await YazilimDepartmanIdAsync(context);
        var rol = DestekErisim.Rol(aktor, talep, yazilimDepartmanId);
        if (!DestekErisim.Gorebilir(rol)) return null;
        var ekipGorunumu = ekipEkrani && DestekErisim.Isleyebilir(rol);

        var yanitlar = await context.DestekTalebiYanitlari
            .AsNoTracking()
            .Include(y => y.Kullanici)
            .Where(y => y.DestekTalebiId == talepId && (ekipGorunumu || !y.DahiliNot))
            .OrderBy(y => y.CreatedAt).ThenBy(y => y.Id)
            .ToListAsync();

        var aktiviteler = await context.DestekTalebiAktiviteleri
            .AsNoTracking()
            .Include(a => a.Kullanici)
            .Where(a => a.DestekTalebiId == talepId)
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .ToListAsync();
        if (!ekipGorunumu)
            aktiviteler = aktiviteler.Where(a => DestekKurallari.TalepEdenAktiviteMetni(a) != null).ToList();

        var ekler = await context.DestekTalebiEkleri
            .AsNoTracking()
            .Include(e => e.YukleyenKullanici)
            .Where(e => e.DestekTalebiId == talepId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
        if (!ekipGorunumu)
        {
            var dahiliYanitlar = await context.DestekTalebiYanitlari.AsNoTracking()
                .Where(y => y.DestekTalebiId == talepId && y.DahiliNot)
                .Select(y => y.Id)
                .ToListAsync();
            ekler = ekler.Where(e => e.YanitId == null || !dahiliYanitlar.Contains(e.YanitId.Value)).ToList();

            // Müşteri ekranına yalnız talebini takip etmesi için gereken alanları bırak.
            talep.Departman = null!;
            talep.Kategori = null;
            talep.AtananKullanici = null;
            talep.OlusturanKullanici = null;
            talep.Cari = null;
            talep.DepartmanId = 0;
            talep.KategoriId = null;
            talep.AtananKullaniciId = null;
            talep.OlusturanKullaniciId = null;
            talep.CariId = null;
            talep.MusteriEmail = string.Empty;
            talep.MusteriTelefon = null;
            talep.SlaSuresi = null;
            talep.SlaBitisTarihi = null;
            talep.SlaAsildi = false;
            talep.CozumSuresiDakika = null;
            talep.IlkYanitSuresiDakika = null;
            talep.DahiliNotlar = null;
            talep.Etiketler = null;
        }

        return new DestekTalepDetayi
        {
            Talep = talep,
            Yanitlar = yanitlar,
            Aktiviteler = aktiviteler,
            Ekler = ekler,
            Rol = rol,
            Eylemler = DestekKurallari.IzinliEylemler(talep.Durum, rol),
            YazilimDepartmanId = yazilimDepartmanId
        };
    }

    public async Task<DestekTalebiYanit> YanitEkleAsync(int talepId, DestekYanitIstegi istek)
    {
        var aktor = await OturumGerekliAsync();
        var icerik = istek.Icerik?.Trim();
        if (string.IsNullOrWhiteSpace(icerik))
            throw new InvalidOperationException("Mesaj boş olamaz.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var talep = await TalepYukleAsync(context, talepId);
        var rol = DestekErisim.Rol(aktor, talep, await YazilimDepartmanIdAsync(context));
        if (!DestekErisim.Gorebilir(rol)) throw YetkiYok();
        if (talep.Durum is DestekDurum.Kapali or DestekDurum.IptalEdildi)
            throw new InvalidOperationException("Kapanmış talebe mesaj yazılamaz. Sorun sürüyorsa yeni bir talep açın.");
        if (talep.Durum == DestekDurum.Taslak)
            throw new InvalidOperationException("Taslağa mesaj yazılamaz; önce talebi gönderin.");

        var ekip = DestekErisim.Isleyebilir(rol);
        var simdi = DateTime.UtcNow;
        var eskiDurum = talep.Durum;
        var eskiAtanan = talep.AtananKullaniciId;
        var yanit = new DestekTalebiYanit
        {
            DestekTalebiId = talep.Id,
            Icerik = icerik,
            KullaniciId = aktor.Id,
            CreatedAt = simdi
        };

        if (ekip)
        {
            yanit.DahiliNot = istek.DahiliNot;
            yanit.YanitTuru = istek.DahiliNot ? YanitTuru.DahiliNot : YanitTuru.Yanit;
            yanit.HazirYanitId = istek.HazirYanitId;
            if (!istek.DahiliNot)
            {
                talep.IlkYanitSuresiDakika ??= Dakika(talep.CreatedAt, simdi);
                if (istek.BilgiIste && DestekKurallari.IzinliMi(DestekEylem.BilgiIste, talep.Durum, rol))
                {
                    talep.Durum = DestekDurum.BilgiBekleniyor;
                }
                else if (talep.Durum == DestekDurum.Yeni)
                {
                    // Yeni talebe ilk yanıtı yazan talebi üstlenmiş olur.
                    talep.Durum = DestekDurum.Islemde;
                    talep.AtananKullaniciId ??= aktor.Id;
                }
            }
        }
        else
        {
            yanit.MusteriYaniti = true;
            yanit.MusteriAdi = DestekBildirimci.Kisalt(aktor.AdSoyad, 200);
            yanit.YanitTuru = YanitTuru.Yanit;
            if (talep.Durum == DestekDurum.BilgiBekleniyor)
                talep.Durum = DestekDurum.Islemde;
        }

        context.DestekTalebiYanitlari.Add(yanit);
        talep.SonAktiviteTarihi = simdi;
        talep.UpdatedAt = simdi;

        Aktivite(context, talep, AktiviteTuru.YanitEklendi,
            yanit.DahiliNot ? "Dahili not eklendi" : ekip ? "Destek ekibi yanıt verdi" : "Talep eden mesaj yazdı", aktor.Id);
        if (eskiAtanan != talep.AtananKullaniciId)
            Aktivite(context, talep, AktiviteTuru.Atandi, $"Talep {aktor.AdSoyad} tarafından üstlenildi", aktor.Id);
        if (eskiDurum != talep.Durum)
            DurumAktivitesi(context, talep, eskiDurum, aktor.Id);

        await context.SaveChangesAsync();

        if (yanit.HazirYanitId.HasValue)
            await HazirYanitKullanimiArttirAsync(context, yanit.HazirYanitId.Value);

        await BildirAsync(context, async () =>
        {
            if (yanit.DahiliNot)
            {
                await DestekBildirimci.EkleAsync(context, [talep.AtananKullaniciId], aktor.Id, talep,
                    "Dahili not eklendi", icerik, ekipLinki: true);
            }
            else if (ekip)
            {
                var bilgiIsteniyor = talep.Durum == DestekDurum.BilgiBekleniyor;
                await DestekBildirimci.EkleAsync(context, [talep.OlusturanKullaniciId], aktor.Id, talep,
                    bilgiIsteniyor ? "Destek ekibi sizden bilgi bekliyor" : "Talebinize yanıt verildi", icerik,
                    ekipLinki: false, bilgiIsteniyor ? BildirimOncelik.Yuksek : BildirimOncelik.Normal);
            }
            else
            {
                await DestekBildirimci.EkleAsync(context, await EkipIlgilileriAsync(context, talep), aktor.Id, talep,
                    "Talep eden mesaj yazdı", icerik, ekipLinki: true);
            }
        });

        if (ekip && !yanit.DahiliNot)
            _ = SendDestekEmailSafeAsync(() => SendYanitEmailAsync(talep, icerik));

        return yanit;
    }

    public async Task<DestekTalebiEk> EkYukleAsync(int talepId, string dosyaAdi, string mimeTipi, Stream icerik, long boyut)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talep = await TalepYukleAsync(context, talepId);
        var rol = DestekErisim.Rol(aktor, talep, await YazilimDepartmanIdAsync(context));
        if (!DestekErisim.Gorebilir(rol)) throw YetkiYok();
        if (talep.Durum is DestekDurum.Kapali or DestekDurum.IptalEdildi)
            throw new InvalidOperationException("Kapanmış talebe dosya eklenemez.");

        var ayarlar = await AyarlariOkuAsync(context);
        EkDosyasiniDenetle(dosyaAdi, boyut, ayarlar);

        using var bellek = new MemoryStream();
        await icerik.CopyToAsync(bellek);
        var ek = await EkKaydetAsync(context, talep, aktor, dosyaAdi, mimeTipi, bellek.ToArray(), ayarlar);

        var ekip = DestekErisim.Isleyebilir(rol);
        await BildirAsync(context, async () =>
        {
            if (ekip)
                await DestekBildirimci.EkleAsync(context, [talep.OlusturanKullaniciId], aktor.Id, talep,
                    "Talebinize dosya eklendi", ek.OrijinalDosyaAdi, ekipLinki: false);
            else
                await DestekBildirimci.EkleAsync(context, await EkipIlgilileriAsync(context, talep), aktor.Id, talep,
                    "Talep eden dosya ekledi", ek.OrijinalDosyaAdi, ekipLinki: true);
        });

        return ek;
    }

    public async Task<DestekEkIcerigi?> EkIndirAsync(int ekId)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var ek = await context.DestekTalebiEkleri.AsNoTracking().FirstOrDefaultAsync(e => e.Id == ekId);
        if (ek?.DestekTalebiId == null) return null;

        var talep = await context.DestekTalepleri.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ek.DestekTalebiId);
        if (talep == null) return null;
        talep.Durum = DestekKurallari.Normallestir(talep.Durum);

        var rol = DestekErisim.Rol(aktor, talep, await YazilimDepartmanIdAsync(context));
        if (!DestekErisim.Gorebilir(rol)) throw YetkiYok();
        if (!DestekErisim.Isleyebilir(rol) && ek.YanitId.HasValue &&
            await context.DestekTalebiYanitlari.AnyAsync(y => y.Id == ek.YanitId && y.DahiliNot))
            throw YetkiYok();

        byte[]? icerik;
        if (string.IsNullOrWhiteSpace(ek.DosyaYolu))
        {
            icerik = null;
        }
        else if (Path.IsPathRooted(ek.DosyaYolu))
        {
            var eskiYol = ResolveLegacyAttachmentPath(ek.DosyaYolu);
            icerik = File.Exists(eskiYol) ? await File.ReadAllBytesAsync(eskiYol) : null;
        }
        else
        {
            icerik = await _secureFileService.ReadDecryptedAsync(ek.DosyaYolu);
        }

        if (icerik == null) return null;
        var mime = string.IsNullOrWhiteSpace(ek.MimeTipi) ? "application/octet-stream" : ek.MimeTipi;
        return new DestekEkIcerigi(ek.OrijinalDosyaAdi, mime, icerik);
    }

    #endregion

    #region Ortak yardımcılar

    private async Task<DestekKullanici> OturumGerekliAsync() =>
        await _kullaniciSaglayici.GetirAsync() ?? throw new UnauthorizedAccessException("Bu işlem için oturum açmalısınız.");

    private static UnauthorizedAccessException YetkiYok(string? mesaj = null) =>
        new(mesaj ?? "Bu talep üzerinde bu işlem için yetkiniz yok.");

    /// <summary>Talebi değiştirmek üzere (izlenen) yükler; eski durum kodunu yeni akışa çevirir.</summary>
    private static async Task<DestekTalebi> TalepYukleAsync(ApplicationDbContext context, int talepId)
    {
        var talep = await context.DestekTalepleri.FirstOrDefaultAsync(t => t.Id == talepId)
            ?? throw new InvalidOperationException("Talep bulunamadı.");
        talep.Durum = DestekKurallari.Normallestir(talep.Durum);
        return talep;
    }

    private static void Normallestir(IEnumerable<DestekTalebi> talepler)
    {
        foreach (var talep in talepler)
            talep.Durum = DestekKurallari.Normallestir(talep.Durum);
    }

    private static int Dakika(DateTime baslangic, DateTime bitis) => Math.Max(0, (int)(bitis - baslangic).TotalMinutes);

    private static string Kirp(string? metin, int uzunluk)
    {
        var temiz = metin?.Trim() ?? string.Empty;
        return temiz.Length <= uzunluk ? temiz : temiz[..uzunluk];
    }

    private static async Task TalepAlanlariniDogrulaAsync(ApplicationDbContext context, DestekTalebi talep)
    {
        talep.Konu = Kirp(talep.Konu, 500);
        talep.Aciklama = talep.Aciklama?.Trim() ?? string.Empty;
        talep.MusteriAdi = Kirp(talep.MusteriAdi, 200);
        talep.MusteriEmail = Kirp(talep.MusteriEmail, 200);
        talep.MusteriTelefon = string.IsNullOrWhiteSpace(talep.MusteriTelefon) ? null : Kirp(talep.MusteriTelefon, 20);
        talep.Etiketler = EtiketleriDuzenle(talep.Etiketler);

        if (string.IsNullOrWhiteSpace(talep.Konu))
            throw new InvalidOperationException("Konu zorunludur.");
        if (string.IsNullOrWhiteSpace(talep.Aciklama))
            throw new InvalidOperationException("Açıklama zorunludur.");
        if (!await context.DestekDepartmanlari.AnyAsync(d => d.Id == talep.DepartmanId && d.Aktif))
            throw new InvalidOperationException("Geçerli bir departman seçin.");

        if (talep.KategoriId.HasValue &&
            !await context.DestekKategorileri.AnyAsync(k => k.Id == talep.KategoriId && k.Aktif &&
                                                           (k.DepartmanId == null || k.DepartmanId == talep.DepartmanId)))
            talep.KategoriId = null;
    }

    private static string? EtiketleriDuzenle(string? etiketler)
    {
        if (string.IsNullOrWhiteSpace(etiketler)) return null;
        var liste = etiketler
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => Kirp(e, 40))
            .Where(e => e.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var birlesik = string.Join(", ", liste);
        return birlesik.Length == 0 ? null : Kirp(birlesik, 500);
    }

    /// <summary>Yeni talebi (ve varsa sistem notlarını) kaydeder; gönderilmişse ekibe duyurur.</summary>
    private async Task YeniTalepKaydetAsync(
        ApplicationDbContext context, DestekTalebi talep, DestekKullanici? aktor, string aktiviteMetni,
        IEnumerable<DestekTalebiYanit>? ekYanitlar = null)
    {
        var yilOnEki = $"TKT-{DateTime.UtcNow.Year}-";
        var mevcutTalepNolari = await context.DestekTalepleri.AsNoTracking()
            .Where(x => x.TalepNo.StartsWith(yilOnEki))
            .Select(x => x.TalepNo)
            .ToListAsync();
        var enBuyukTalepNo = mevcutTalepNolari
            .Select(no => int.TryParse(no.AsSpan(yilOnEki.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var sira) ? sira : 0)
            .DefaultIfEmpty(0)
            .Max();

        // FisNoCounters tablosu eski/elle açılmış taleplerin gerisinde kalmış olabilir.
        talep.TalepNo = await _numaraSerisi.GenerateFormattedAtLeastAsync("TKT", 0, 6, enBuyukTalepNo + 1);
        var simdi = DateTime.UtcNow;
        talep.CreatedAt = simdi;
        talep.SonAktiviteTarihi = simdi;
        if (talep.Durum != DestekDurum.Taslak)
            await SlaUygulaAsync(context, talep, simdi);

        context.DestekTalepleri.Add(talep);
        Aktivite(context, talep, AktiviteTuru.Olusturuldu, aktiviteMetni, aktor?.Id, null, DestekKurallari.Etiket(talep.Durum));
        foreach (var yanit in ekYanitlar ?? [])
        {
            yanit.DestekTalebi = talep;
            yanit.CreatedAt = simdi;
            context.DestekTalebiYanitlari.Add(yanit);
        }

        await context.SaveChangesAsync();

        if (talep.Durum == DestekDurum.Yeni)
            await GonderilenTalebiDuyurAsync(context, talep, aktor);
    }

    /// <summary>Gönderilen talebi departman ekibine bildirir, talep edene onay e-postası atar.</summary>
    private async Task GonderilenTalebiDuyurAsync(ApplicationDbContext context, DestekTalebi talep, DestekKullanici? aktor)
    {
        await BildirAsync(context, async () =>
        {
            var alicilar = await DestekBildirimci.EkipAlicilariAsync(context, talep.DepartmanId, DestekKurallari.PersonelYetkisi);
            await DestekBildirimci.EkleAsync(context, alicilar, aktor?.Id, talep,
                talep.Kaynak == DestekKaynak.HataBildirimi ? "Yeni hata bildirimi" : "Yeni destek talebi",
                $"{talep.MusteriAdi}: {talep.Konu}", ekipLinki: true,
                talep.Oncelik >= DestekOncelik.Acil ? BildirimOncelik.Yuksek : BildirimOncelik.Normal);
        });

        _ = SendDestekEmailSafeAsync(() => SendYeniTalepEmailAsync(talep));
    }

    private static async Task SlaUygulaAsync(ApplicationDbContext context, DestekTalebi talep, DateTime baslangic)
    {
        var saat = await context.DestekSlaListesi.AsNoTracking()
            .Where(x => x.Oncelik == talep.Oncelik && x.Aktif)
            .OrderBy(x => x.Id)
            .Select(x => (int?)x.CozumSuresi)
            .FirstOrDefaultAsync();
        if (saat is null or <= 0)
            saat = await context.DestekDepartmanlari.AsNoTracking()
                .Where(d => d.Id == talep.DepartmanId)
                .Select(d => d.VarsayilanSlaSuresi)
                .FirstOrDefaultAsync();

        if (saat is > 0)
        {
            talep.SlaSuresi = saat;
            talep.SlaBitisTarihi = baslangic.AddHours(saat.Value);
        }
        else
        {
            talep.SlaSuresi = null;
            talep.SlaBitisTarihi = null;
        }
        talep.SlaAsildi = false;
    }

    private static void Aktivite(
        ApplicationDbContext context, DestekTalebi talep, AktiviteTuru tur, string aciklama, int? kullaniciId,
        string? eskiDeger = null, string? yeniDeger = null)
    {
        context.DestekTalebiAktiviteleri.Add(new DestekTalebiAktivite
        {
            DestekTalebi = talep,
            AktiviteTuru = tur,
            Aciklama = Kirp(aciklama, 1000),
            KullaniciId = kullaniciId,
            EskiDeger = eskiDeger == null ? null : Kirp(eskiDeger, 500),
            YeniDeger = yeniDeger == null ? null : Kirp(yeniDeger, 500),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static void DurumAktivitesi(ApplicationDbContext context, DestekTalebi talep, DestekDurum eskiDurum, int? kullaniciId)
    {
        var eski = DestekKurallari.Etiket(eskiDurum);
        var yeni = DestekKurallari.Etiket(talep.Durum);
        Aktivite(context, talep, AktiviteTuru.DurumDegisti, $"Durum: {eski} → {yeni}", kullaniciId, eski, yeni);
    }

    /// <summary>Ekipten bu talepten haberdar edilecekler: atanan kişi, yoksa departman ekibi.</summary>
    private static async Task<List<int?>> EkipIlgilileriAsync(ApplicationDbContext context, DestekTalebi talep)
    {
        if (talep.AtananKullaniciId.HasValue) return [talep.AtananKullaniciId];
        var yetki = talep.Durum == DestekDurum.YazilimEkibinde ? DestekKurallari.YazilimYetkisi : DestekKurallari.PersonelYetkisi;
        return await DestekBildirimci.EkipAlicilariAsync(context, talep.DepartmanId, yetki);
    }

    /// <summary>Bildirimleri yazar; bildirim hatası işlemi bozmaz.</summary>
    private async Task BildirAsync(ApplicationDbContext context, Func<Task> ekle)
    {
        try
        {
            await ekle();
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Destek bildirimi yazılamadı");
            foreach (var giris in context.ChangeTracker.Entries<Bildirim>().Where(g => g.State == EntityState.Added).ToList())
                giris.State = EntityState.Detached;
        }
    }

    private static string HataAciklamasi(SorunBildirimi bildirim)
    {
        var metin = new StringBuilder(bildirim.Aciklama.Trim());
        metin.AppendLine().AppendLine().AppendLine("— Otomatik eklenen bilgiler —");
        if (!string.IsNullOrWhiteSpace(bildirim.Sayfa))
            metin.AppendLine($"Sayfa: {bildirim.Sayfa}");
        if (!string.IsNullOrWhiteSpace(bildirim.HataMesaji))
            metin.AppendLine($"Görülen hata: {DestekBildirimci.Kisalt(bildirim.HataMesaji, 500)}");
        if (bildirim.HataZamaniUtc.HasValue)
            metin.AppendLine($"Hata zamanı: {BusinessTime.LocalAt(bildirim.HataZamaniUtc.Value):dd.MM.yyyy HH:mm:ss}");
        metin.Append($"Bildirim zamanı: {BusinessTime.Now:dd.MM.yyyy HH:mm:ss}");
        return metin.ToString();
    }

    private static string TeknikNot(SorunBildirimi bildirim)
    {
        var metin = new StringBuilder("Otomatik teknik bilgiler (yalnız ekip görür)");
        void Satir(string baslik, string? deger)
        {
            if (!string.IsNullOrWhiteSpace(deger)) metin.Append('\n').Append(baslik).Append(": ").Append(deger.Trim());
        }

        Satir("Sayfa", bildirim.Sayfa);
        Satir("Önceki sayfa", bildirim.OncekiSayfa);
        Satir("Hata zamanı", bildirim.HataZamaniUtc.HasValue ? BusinessTime.LocalAt(bildirim.HataZamaniUtc.Value).ToString("dd.MM.yyyy HH:mm:ss") : null);
        Satir("Hata türü", bildirim.HataTuru);
        Satir("Hata mesajı", bildirim.HataMesaji);
        Satir("Tarayıcı", bildirim.Tarayici);
        Satir("Uygulama sürümü", bildirim.UygulamaSurumu);
        if (!string.IsNullOrWhiteSpace(bildirim.YiginIzi))
            metin.Append("\nYığın izi:\n").Append(bildirim.YiginIzi.Trim());

        var sonuc = metin.ToString();
        return sonuc.Length <= TeknikMetinLimiti ? sonuc : sonuc[..TeknikMetinLimiti] + "\n…(kısaltıldı)";
    }

    private static void EkDosyasiniDenetle(string dosyaAdi, long boyut, DestekGenelAyarlar ayarlar)
    {
        var uzanti = Path.GetExtension(Path.GetFileName(dosyaAdi) ?? string.Empty).ToLowerInvariant();
        var izinli = ayarlar.IzinliUzantilar();
        if (string.IsNullOrEmpty(uzanti) || !izinli.Contains(uzanti))
            throw new InvalidOperationException($"Bu dosya türü eklenemez ({(string.IsNullOrEmpty(uzanti) ? "uzantısız" : uzanti)}). İzin verilenler: {string.Join(", ", izinli)}");

        var sinirMb = Math.Clamp(ayarlar.MaksimumDosyaBoyutuMB, 1, 50);
        if (boyut <= 0)
            throw new InvalidOperationException("Dosya boş.");
        if (boyut > sinirMb * 1024L * 1024L)
            throw new InvalidOperationException($"Dosya en fazla {sinirMb} MB olabilir.");
    }

    private async Task<DestekTalebiEk> EkKaydetAsync(
        ApplicationDbContext context, DestekTalebi talep, DestekKullanici aktor, string dosyaAdi, string mimeTipi,
        byte[] icerik, DestekGenelAyarlar ayarlar)
    {
        var ad = Path.GetFileName(dosyaAdi);
        if (string.IsNullOrWhiteSpace(ad)) ad = "dosya";
        EkDosyasiniDenetle(ad, icerik.LongLength, ayarlar);

        var depoAdi = Kirp($"{Guid.NewGuid()}_{ad}", 255);
        var yol = await _secureFileService.SaveEncryptedAsync($"destek/{talep.Id}", depoAdi, icerik);

        var simdi = DateTime.UtcNow;
        var ek = new DestekTalebiEk
        {
            DestekTalebiId = talep.Id,
            DosyaAdi = depoAdi,
            OrijinalDosyaAdi = Kirp(ad, 255),
            DosyaYolu = yol,
            DosyaBoyutu = icerik.LongLength,
            MimeTipi = Kirp(string.IsNullOrWhiteSpace(mimeTipi) ? "application/octet-stream" : mimeTipi, 100),
            YukleyenKullaniciId = aktor.Id,
            CreatedAt = simdi
        };
        context.DestekTalebiEkleri.Add(ek);
        talep.SonAktiviteTarihi = simdi;
        talep.UpdatedAt = simdi;
        Aktivite(context, talep, AktiviteTuru.DosyaEklendi, $"Dosya eklendi: {ek.OrijinalDosyaAdi}", aktor.Id);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception saveException)
        {
            await CompensateAttachmentSaveAsync(yol, saveException);
            throw;
        }

        return ek;
    }

    private async Task CompensateAttachmentSaveAsync(string path, Exception saveException)
    {
        try
        {
            await using var verify = await _contextFactory.CreateDbContextAsync();
            var isReferenced = await verify.DestekTalebiEkleri.AsNoTracking()
                .AnyAsync(x => x.DosyaYolu == path);
            if (!isReferenced)
                await _secureFileService.DeleteAsync(path);
        }
        catch (Exception compensationException)
        {
            throw new AggregateException(
                "Destek eki DB kaydı başarısız veya belirsiz; yeni dosyanın DB başvurusu doğrulanamadı ve dosya güvenlik için korunuyor.",
                saveException, compensationException);
        }
    }

    private string ResolveLegacyAttachmentPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!StorageFilePath.IsWithinRoot(_uploadPath, fullPath, allowRoot: false))
            throw new InvalidOperationException("Destek eki yolu izin verilen yükleme dizininin dışında.");
        _ = StorageFilePath.Resolve(_uploadPath, Path.GetRelativePath(_uploadPath, fullPath));
        return fullPath;
    }

    #endregion

    #region E-posta bildirimleri (Destek Ayarları'ndan açılır)

    private async Task SendDestekEmailSafeAsync(Func<Task> emailAction)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (!(await AyarlariOkuAsync(context)).EmailBildirimAktif)
            {
                _logger.LogDebug("Destek e-posta bildirimleri devre dışı");
                return;
            }
            await emailAction();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Destek e-posta bildirimi gönderilirken hata oluştu");
        }
    }

    private async Task SendYeniTalepEmailAsync(DestekTalebi talep)
    {
        if (string.IsNullOrWhiteSpace(talep.MusteriEmail)) return;

        await _emailService.SendDestekYeniTalepEmailAsync(
            talep.MusteriEmail,
            talep.MusteriAdi,
            talep.TalepNo,
            talep.Konu,
            DestekKurallari.OncelikEtiketi(talep.Oncelik));
    }

    private async Task SendYanitEmailAsync(DestekTalebi talep, string yanitIcerik)
    {
        if (string.IsNullOrWhiteSpace(talep.MusteriEmail)) return;

        var ozet = HtmlSanitizerHelper.DuzMetin(yanitIcerik);
        if (ozet.Length > 300) ozet = ozet[..300] + "...";

        await _emailService.SendDestekYanitEmailAsync(
            talep.MusteriEmail,
            talep.MusteriAdi,
            talep.TalepNo,
            talep.Konu,
            ozet);
    }

    private async Task SendDurumDegisiklikEmailAsync(DestekTalebi talep, DestekDurum eskiDurum, DestekDurum yeniDurum)
    {
        if (string.IsNullOrWhiteSpace(talep.MusteriEmail)) return;

        await _emailService.SendDestekDurumEmailAsync(
            talep.MusteriEmail,
            talep.MusteriAdi,
            talep.TalepNo,
            talep.Konu,
            DestekKurallari.Etiket(eskiDurum),
            DestekKurallari.Etiket(yeniDurum));
    }

    private async Task SendAtamaEmailAsync(DestekTalebi talep, string? email, string adSoyad)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        await _emailService.SendDestekAtamaEmailAsync(
            email,
            adSoyad,
            talep.TalepNo,
            talep.Konu,
            talep.MusteriAdi,
            DestekKurallari.OncelikEtiketi(talep.Oncelik));
    }

    #endregion
}
