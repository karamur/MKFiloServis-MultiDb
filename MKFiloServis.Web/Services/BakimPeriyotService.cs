using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class BakimPeriyotService : IBakimPeriyotService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<BakimPeriyotService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public BakimPeriyotService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<BakimPeriyotService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<List<BakimPeriyot>> GetByAracIdAsync(int aracId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.BakimPeriyotlar
            .AsNoTracking()
            .Where(b => b.AracId == aracId && !b.IsDeleted)
            .OrderBy(b => b.BakimAdi)
            .ToListAsync();
    }

    public async Task<List<BakimPeriyot>> GetAllActiveAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.BakimPeriyotlar
            .AsNoTracking()
            .Include(b => b.Arac)
            .Where(b => b.Aktif && !b.IsDeleted && b.Arac.Aktif && !b.Arac.IsDeleted)
            .OrderBy(b => b.Arac.AktifPlaka)
            .ThenBy(b => b.BakimAdi)
            .ToListAsync();
    }

    public async Task<BakimPeriyot?> GetByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.BakimPeriyotlar
            .AsNoTracking()
            .Include(b => b.Arac)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
    }

    public async Task<BakimPeriyot> CreateAsync(BakimPeriyot periyot)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.BakimPeriyotlar.Add(periyot);
        await ctx.SaveChangesAsync();
        return periyot;
    }

    public async Task<BakimPeriyot> UpdateAsync(BakimPeriyot periyot)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        periyot.UpdatedAt = DateTime.UtcNow;
        ctx.BakimPeriyotlar.Update(periyot);
        await ctx.SaveChangesAsync();
        return periyot;
    }

    public async Task DeleteAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var periyot = await ctx.BakimPeriyotlar.FindAsync(id);
        if (periyot != null)
        {
            periyot.IsDeleted = true;
            periyot.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    public async Task KmGuncellemeKontrolAsync(int aracId, int yeniKm)
    {
        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var periyotlar = await ctx.BakimPeriyotlar
                .Where(b => b.AracId == aracId && b.Aktif && !b.IsDeleted && b.PeriyotKm.HasValue)
                .ToListAsync();

            if (!periyotlar.Any()) return;

            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetService<IEmailService>();
            var whatsappService = scope.ServiceProvider.GetService<IWhatsAppService>();
            var bildirimService = scope.ServiceProvider.GetService<IBildirimService>();

            var arac = await ctx.Araclar.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aracId);
            if (arac == null) return;

            foreach (var periyot in periyotlar)
            {
                var kalanKm = periyot.KalanKm(yeniKm);
                if (kalanKm == null) continue;

                BakimUyariTipi? uyariTipi = null;
                if (kalanKm <= 0)
                    uyariTipi = BakimUyariTipi.KmAsildi;
                else if (kalanKm <= periyot.UyariKmEsigi)
                    uyariTipi = BakimUyariTipi.KmYaklasiyor;

                if (uyariTipi == null) continue;

                // Bugün aynı tip uyarı zaten gönderilmişse tekrar gönderme
                var bugunGonderildi = await ctx.AracBakimUyarilari.AnyAsync(u =>
                    u.BakimPeriyotId == periyot.Id &&
                    u.UyariTipi == uyariTipi &&
                    u.GonderimTarihi >= DateTime.UtcNow.Date);
                if (bugunGonderildi) continue;

                await GonderBakimUyarisiAsync(ctx, periyot, arac, uyariTipi.Value,
                    yeniKm, kalanKm, null, emailService, whatsappService, bildirimService);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Km güncelleme bakım kontrolü hatası (AracId: {AracId})", aracId);
        }
    }

    public async Task TumAraclariBakimKontrolAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Bakım periyot kontrolü başlatıldı");
        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var periyotlar = await ctx.BakimPeriyotlar
                .AsNoTracking()
                .Include(b => b.Arac)
                .Where(b => b.Aktif && !b.IsDeleted && b.Arac.Aktif && !b.Arac.IsDeleted)
                .ToListAsync(ct);

            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetService<IEmailService>();
            var whatsappService = scope.ServiceProvider.GetService<IWhatsAppService>();
            var bildirimService = scope.ServiceProvider.GetService<IBildirimService>();

            int uyariSayisi = 0;
            foreach (var periyot in periyotlar)
            {
                ct.ThrowIfCancellationRequested();
                var arac = periyot.Arac;
                var bugun = DateTime.Today;

                // --- Km bazlı kontrol ---
                if (periyot.PeriyotKm.HasValue && periyot.SonrakiBakimKm.HasValue && arac.KmDurumu.HasValue)
                {
                    var kalanKm = periyot.SonrakiBakimKm.Value - arac.KmDurumu.Value;
                    BakimUyariTipi? uyariTipi = kalanKm <= 0
                        ? BakimUyariTipi.KmAsildi
                        : kalanKm <= periyot.UyariKmEsigi
                            ? BakimUyariTipi.KmYaklasiyor
                            : null;

                    if (uyariTipi != null && !await BugunGonderildiMiAsync(ctx, periyot.Id, uyariTipi.Value, ct))
                    {
                        await using var writeCtx = await _contextFactory.CreateDbContextAsync();
                        await GonderBakimUyarisiAsync(writeCtx, periyot, arac, uyariTipi.Value,
                            arac.KmDurumu.Value, kalanKm, null, emailService, whatsappService, bildirimService);
                        uyariSayisi++;
                    }
                }

                // --- Gün bazlı kontrol ---
                if (periyot.PeriyotGun.HasValue && periyot.SonrakiBakimTarihi.HasValue)
                {
                    var kalanGun = (int)(periyot.SonrakiBakimTarihi.Value.Date - bugun).TotalDays;
                    BakimUyariTipi? uyariTipi = kalanGun <= 0
                        ? BakimUyariTipi.GunAsildi
                        : kalanGun <= periyot.UyariGunEsigi
                            ? BakimUyariTipi.GunYaklasiyor
                            : null;

                    if (uyariTipi != null && !await BugunGonderildiMiAsync(ctx, periyot.Id, uyariTipi.Value, ct))
                    {
                        await using var writeCtx = await _contextFactory.CreateDbContextAsync();
                        await GonderBakimUyarisiAsync(writeCtx, periyot, arac, uyariTipi.Value,
                            arac.KmDurumu, null, kalanGun, emailService, whatsappService, bildirimService);
                        uyariSayisi++;
                    }
                }
            }

            _logger.LogInformation("Bakım periyot kontrolü tamamlandı. Gönderilen uyarı: {Sayi}", uyariSayisi);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bakım periyot kontrolü hatası");
        }
    }

    public async Task<List<BakimDurumOzet>> GetBakimDurumOzetAsync(int? aracId = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var query = ctx.BakimPeriyotlar
            .AsNoTracking()
            .Include(b => b.Arac)
            .Where(b => b.Aktif && !b.IsDeleted && b.Arac.Aktif && !b.Arac.IsDeleted);

        if (aracId.HasValue)
            query = query.Where(b => b.AracId == aracId.Value);

        var periyotlar = await query.ToListAsync();
        var bugun = DateTime.Today;
        var ozet = new List<BakimDurumOzet>();

        foreach (var p in periyotlar)
        {
            int? kalanKm = null;
            int? kalanGun = null;
            var seviye = BakimDurumSeviye.Normal;

            if (p.PeriyotKm.HasValue && p.SonrakiBakimKm.HasValue && p.Arac.KmDurumu.HasValue)
            {
                kalanKm = p.SonrakiBakimKm.Value - p.Arac.KmDurumu.Value;
                if (kalanKm <= 0) seviye = BakimDurumSeviye.Kritik;
                else if (kalanKm <= p.UyariKmEsigi) seviye = BakimDurumSeviye.Uyari;
            }

            if (p.PeriyotGun.HasValue && p.SonrakiBakimTarihi.HasValue)
            {
                kalanGun = (int)(p.SonrakiBakimTarihi.Value.Date - bugun).TotalDays;
                var gunSeviye = kalanGun <= 0 ? BakimDurumSeviye.Kritik
                    : kalanGun <= p.UyariGunEsigi ? BakimDurumSeviye.Uyari
                    : BakimDurumSeviye.Normal;
                if (gunSeviye > seviye) seviye = gunSeviye;
            }

            if (seviye > BakimDurumSeviye.Normal || aracId.HasValue)
            {
                ozet.Add(new BakimDurumOzet
                {
                    AracId = p.AracId,
                    Plaka = p.Arac.AktifPlaka ?? p.Arac.SaseNo,
                    BakimPeriyotId = p.Id,
                    BakimAdi = p.BakimAdi,
                    KalanKm = kalanKm,
                    KalanGun = kalanGun,
                    Seviye = seviye
                });
            }
        }

        return ozet.OrderByDescending(o => o.Seviye).ThenBy(o => o.KalanKm).ToList();
    }

    // --- Yardımcı Metotlar ---

    private static async Task<bool> BugunGonderildiMiAsync(
        ApplicationDbContext ctx, int periyotId, BakimUyariTipi tip, CancellationToken ct)
    {
        return await ctx.AracBakimUyarilari.AnyAsync(u =>
            u.BakimPeriyotId == periyotId &&
            u.UyariTipi == tip &&
            u.GonderimTarihi >= DateTime.UtcNow.Date, ct);
    }

    private async Task GonderBakimUyarisiAsync(
        ApplicationDbContext ctx,
        BakimPeriyot periyot,
        Arac arac,
        BakimUyariTipi uyariTipi,
        int? aracKm,
        int? kalanKm,
        int? kalanGun,
        IEmailService? emailService,
        IWhatsAppService? whatsappService,
        IBildirimService? bildirimService)
    {
        var plaka = arac.AktifPlaka ?? arac.SaseNo;
        var log = new AracBakimUyari
        {
            BakimPeriyotId = periyot.Id,
            AracId = arac.Id,
            UyariTipi = uyariTipi,
            AracKm = aracKm,
            KalanKm = kalanKm,
            KalanGun = kalanGun,
            GonderimTarihi = DateTime.UtcNow
        };

        var mesaj = OlusturBakimMesaji(plaka, periyot.BakimAdi, uyariTipi, kalanKm, kalanGun);

        try
        {
            // Uygulama içi bildirim
            if (bildirimService != null)
            {
                await bildirimService.CreateAsync(new Bildirim
                {
                    KullaniciId = 1,
                    Baslik = $"Bakım Uyarısı: {plaka}",
                    Icerik = mesaj,
                    Tip = BildirimTipi.Uyari,
                    Oncelik = BildirimOncelik.Yuksek,
                    Link = "/araclar",
                    IliskiliTablo = "Arac",
                    IliskiliKayitId = arac.Id
                });
            }

            log.WhatsAppGonderildi = true;
            log.EmailGonderildi = true;
        }
        catch (Exception ex)
        {
            log.HataMesaji = ex.Message;
            _logger.LogWarning(ex, "Bakım bildirimi gönderim hatası: {Plaka} - {Bakim}", plaka, periyot.BakimAdi);
        }

        ctx.AracBakimUyarilari.Add(log);
        await ctx.SaveChangesAsync();

        _logger.LogInformation("Bakım uyarısı kaydedildi: {Plaka} - {Bakim} - {Tip}", plaka, periyot.BakimAdi, uyariTipi);
    }

    private static string OlusturBakimMesaji(string plaka, string bakimAdi, BakimUyariTipi tip,
        int? kalanKm, int? kalanGun)
    {
        return tip switch
        {
            BakimUyariTipi.KmYaklasiyor => $"🔧 {plaka} — {bakimAdi} bakımına {kalanKm:N0} km kaldı.",
            BakimUyariTipi.KmAsildi => $"⚠️ {plaka} — {bakimAdi} bakımı {Math.Abs(kalanKm ?? 0):N0} km geçildi!",
            BakimUyariTipi.GunYaklasiyor => $"🔧 {plaka} — {bakimAdi} bakımına {kalanGun} gün kaldı.",
            BakimUyariTipi.GunAsildi => $"⚠️ {plaka} — {bakimAdi} bakımı {Math.Abs(kalanGun ?? 0)} gün geçildi!",
            _ => $"{plaka} — {bakimAdi} bakım uyarısı."
        };
    }
}


