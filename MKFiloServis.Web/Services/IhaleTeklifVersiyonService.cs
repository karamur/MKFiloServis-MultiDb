using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class IhaleTeklifVersiyonService : IIhaleTeklifVersiyonService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IIhaleHazirlikService _ihaleHazirlikService;
    private readonly IKullaniciService _kullaniciService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<IhaleTeklifVersiyonService> _logger;

    public IhaleTeklifVersiyonService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IIhaleHazirlikService ihaleHazirlikService,
        IKullaniciService kullaniciService,
        IAuditLogService auditLogService,
        ILogger<IhaleTeklifVersiyonService> logger)
    {
        _contextFactory = contextFactory;
        _ihaleHazirlikService = ihaleHazirlikService;
        _kullaniciService = kullaniciService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<IhaleTeklifVersiyon?> GetByIdAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IhaleTeklifVersiyonlari
            .Include(v => v.IhaleProje)
            .Include(v => v.HazirlayanKullanici)
            .Include(v => v.OnaylayanKullanici)
            .FirstOrDefaultAsync(v => v.Id == versiyonId);
    }

    public async Task<List<IhaleTeklifVersiyon>> GetListByIhaleProjeIdAsync(int ihaleProjeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IhaleTeklifVersiyonlari
            .Include(v => v.HazirlayanKullanici)
            .Include(v => v.OnaylayanKullanici)
            .Where(v => v.IhaleProjeId == ihaleProjeId)
            .OrderByDescending(v => v.VersiyonNo)
            .ToListAsync();
    }

    public async Task<IhaleTeklifVersiyon?> GetAktifVersiyonAsync(int ihaleProjeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IhaleTeklifVersiyonlari
            .Include(v => v.HazirlayanKullanici)
            .Include(v => v.OnaylayanKullanici)
            .FirstOrDefaultAsync(v => v.IhaleProjeId == ihaleProjeId && v.AktifVersiyon);
    }

    public async Task<List<IhaleTeklifKararLog>> GetKararLoglariAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IhaleTeklifKararLoglari
            .Include(l => l.IslemYapanKullanici)
            .Where(l => l.IhaleTeklifVersiyonId == versiyonId)
            .OrderByDescending(l => l.IslemTarihi)
            .ToListAsync();
    }

    public async Task<IhaleTeklifVersiyon> CreateInitialAsync(int ihaleProjeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await GetCurrentUserOrThrowAsync(context);
        EnsureCanManageDrafts(kullanici);

        var proje = await GetProjeAsync(context, ihaleProjeId);
        var mevcutVersiyonVar = await context.IhaleTeklifVersiyonlari
            .AnyAsync(v => v.IhaleProjeId == ihaleProjeId);

        if (mevcutVersiyonVar)
            throw new InvalidOperationException("Bu ihale için zaten teklif versiyonu oluşturulmuş.");

        var versiyon = new IhaleTeklifVersiyon
        {
            IhaleProjeId = proje.Id,
            VersiyonNo = 1,
            RevizyonKodu = "V1",
            Durum = IhaleTeklifVersiyonDurum.Taslak,
            HazirlayanKullaniciId = kullanici?.Id,
            HazirlamaTarihi = DateTime.UtcNow,
            AktifVersiyon = true
        };

        await SnapshotDoldurAsync(context, versiyon);

        context.IhaleTeklifVersiyonlari.Add(versiyon);
        await context.SaveChangesAsync();

        await KararLogEkleAsync(context, 
            versiyon.Id,
            IhaleTeklifIslemTipi.Olustur,
            null,
            versiyon.Durum,
            "İlk teklif versiyonu oluşturuldu.",
            kullanici?.Id);

        await context.SaveChangesAsync();
        await TryAuditAsync(context, "IhaleTeklifVersiyonOlustur", versiyon.Id, $"{proje.ProjeKodu} için ilk teklif versiyonu oluşturuldu.");

        return versiyon;
    }

    public async Task<IhaleTeklifVersiyon> CreateRevisionAsync(int kaynakVersiyonId, string? revizyonNotu)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await GetCurrentUserOrThrowAsync(context);
        EnsureCanManageDrafts(kullanici);

        var kaynakVersiyon = await context.IhaleTeklifVersiyonlari
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == kaynakVersiyonId);

        if (kaynakVersiyon == null)
            throw new InvalidOperationException("Kaynak teklif versiyonu bulunamadı.");

        var proje = await GetProjeAsync(context, kaynakVersiyon.IhaleProjeId);
        var yeniVersiyonNo = await GetSonrakiVersiyonNoAsync(context, kaynakVersiyon.IhaleProjeId);

        await PasiflestirDigerAktifVersiyonlarAsync(context, kaynakVersiyon.IhaleProjeId, null);

        var yeniVersiyon = new IhaleTeklifVersiyon
        {
            IhaleProjeId = kaynakVersiyon.IhaleProjeId,
            VersiyonNo = yeniVersiyonNo,
            RevizyonKodu = $"V{yeniVersiyonNo}",
            Durum = IhaleTeklifVersiyonDurum.Taslak,
            RevizyonNotu = revizyonNotu,
            HazirlayanKullaniciId = kullanici?.Id,
            HazirlamaTarihi = DateTime.UtcNow,
            AktifVersiyon = true,
            ToplamMaliyet = kaynakVersiyon.ToplamMaliyet,
            TeklifTutari = kaynakVersiyon.TeklifTutari,
            KarMarjiTutari = kaynakVersiyon.KarMarjiTutari,
            KarMarjiOrani = kaynakVersiyon.KarMarjiOrani
        };

        await SnapshotDoldurAsync(context, yeniVersiyon);

        context.IhaleTeklifVersiyonlari.Add(yeniVersiyon);
        await context.SaveChangesAsync();

        await KararLogEkleAsync(context, 
            yeniVersiyon.Id,
            IhaleTeklifIslemTipi.RevizyonOlustur,
            null,
            yeniVersiyon.Durum,
            revizyonNotu ?? $"{kaynakVersiyon.RevizyonKodu} versiyonundan revizyon oluşturuldu.",
            kullanici?.Id);

        await context.SaveChangesAsync();
        await TryAuditAsync(context, "IhaleTeklifRevizyonOlustur", yeniVersiyon.Id, $"{proje.ProjeKodu} için {yeniVersiyon.RevizyonKodu} revizyonu oluşturuldu.");

        return yeniVersiyon;
    }

    public async Task<IhaleTeklifVersiyon> SetActiveAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await GetCurrentUserOrThrowAsync(context);
        EnsureCanManageDrafts(kullanici);

        var versiyon = await context.IhaleTeklifVersiyonlari
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == versiyonId);

        if (versiyon == null)
            throw new InvalidOperationException("Teklif versiyonu bulunamadı.");

        if (!versiyon.AktifVersiyon)
        {
            await PasiflestirDigerAktifVersiyonlarAsync(context, versiyon.IhaleProjeId, versiyon.Id);
            versiyon.AktifVersiyon = true;
            versiyon.UpdatedAt = DateTime.UtcNow;
            await KararLogEkleAsync(context, 
                versiyon.Id,
                IhaleTeklifIslemTipi.AktifVersiyonDegisti,
                versiyon.Durum,
                versiyon.Durum,
                $"{versiyon.RevizyonKodu} aktif versiyon olarak işaretlendi.",
                kullanici?.Id);

            await context.SaveChangesAsync();
            await TryAuditAsync(context, "IhaleTeklifAktifVersiyon", versiyon.Id, $"{versiyon.RevizyonKodu} aktif versiyon yapıldı.");
        }

        return versiyon;
    }

    public async Task<IhaleTeklifVersiyon> SendToReviewAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await GetCurrentUserOrThrowAsync(context);
        EnsureCanManageDrafts(kullanici);

        var versiyon = await context.IhaleTeklifVersiyonlari
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == versiyonId);

        if (versiyon == null)
            throw new InvalidOperationException("Teklif versiyonu bulunamadı.");

        if (versiyon.Durum != IhaleTeklifVersiyonDurum.Taslak)
            throw new InvalidOperationException("Sadece taslak durumundaki teklif incelemeye gönderilebilir.");

        var oncekiDurum = versiyon.Durum;

        versiyon.Durum = IhaleTeklifVersiyonDurum.Incelemede;
        versiyon.UpdatedAt = DateTime.UtcNow;

        await KararLogEkleAsync(context, 
            versiyon.Id,
            IhaleTeklifIslemTipi.IncelemeyeGonder,
            oncekiDurum,
            versiyon.Durum,
            "Teklif incelemeye gönderildi.",
            kullanici?.Id);

        await context.SaveChangesAsync();
        await TryAuditAsync(context, "IhaleTeklifInceleme", versiyon.Id, $"{versiyon.RevizyonKodu} incelemeye gönderildi.");

        return versiyon;
    }

    public async Task<IhaleTeklifVersiyon> ApproveAsync(int versiyonId, string? kararNotu)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await GetCurrentUserOrThrowAsync(context);
        EnsureCanApprove(kullanici);

        var versiyon = await context.IhaleTeklifVersiyonlari
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == versiyonId);

        if (versiyon == null)
            throw new InvalidOperationException("Teklif versiyonu bulunamadı.");

        if (versiyon.Durum != IhaleTeklifVersiyonDurum.Incelemede)
            throw new InvalidOperationException("Sadece incelemedeki teklifler onaylanabilir.");

        var oncekiDurum = versiyon.Durum;

        versiyon.Durum = IhaleTeklifVersiyonDurum.Onaylandi;
        versiyon.KararNotu = kararNotu;
        versiyon.OnaylayanKullaniciId = kullanici?.Id;
        versiyon.OnayTarihi = DateTime.UtcNow;
        versiyon.UpdatedAt = DateTime.UtcNow;

        await KararLogEkleAsync(context, 
            versiyon.Id,
            IhaleTeklifIslemTipi.Onayla,
            oncekiDurum,
            versiyon.Durum,
            kararNotu ?? "Teklif onaylandı.",
            kullanici?.Id);

        await context.SaveChangesAsync();
        await TryAuditAsync(context, "IhaleTeklifOnay", versiyon.Id, $"{versiyon.RevizyonKodu} onaylandı.");

        return versiyon;
    }

    public async Task<IhaleTeklifVersiyon> RejectAsync(int versiyonId, string kararNotu)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kullanici = await GetCurrentUserOrThrowAsync(context);
        EnsureCanApprove(kullanici);

        if (string.IsNullOrWhiteSpace(kararNotu))
            throw new InvalidOperationException("Reddedilen teklifler için karar notu zorunludur.");

        var versiyon = await context.IhaleTeklifVersiyonlari
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == versiyonId);

        if (versiyon == null)
            throw new InvalidOperationException("Teklif versiyonu bulunamadı.");

        if (versiyon.Durum != IhaleTeklifVersiyonDurum.Incelemede)
            throw new InvalidOperationException("Sadece incelemedeki teklifler reddedilebilir.");

        var oncekiDurum = versiyon.Durum;

        versiyon.Durum = IhaleTeklifVersiyonDurum.Reddedildi;
        versiyon.KararNotu = kararNotu;
        versiyon.OnaylayanKullaniciId = kullanici?.Id;
        versiyon.OnayTarihi = DateTime.UtcNow;
        versiyon.UpdatedAt = DateTime.UtcNow;

        await KararLogEkleAsync(context, 
            versiyon.Id,
            IhaleTeklifIslemTipi.Reddet,
            oncekiDurum,
            versiyon.Durum,
            kararNotu,
            kullanici?.Id);

        await context.SaveChangesAsync();
        await TryAuditAsync(context, "IhaleTeklifRed", versiyon.Id, $"{versiyon.RevizyonKodu} reddedildi.");

        return versiyon;
    }

    private async Task<IhaleProje> GetProjeAsync(ApplicationDbContext context, int ihaleProjeId)
    {
        var proje = await context.IhaleProjeleri
            .FirstOrDefaultAsync(p => p.Id == ihaleProjeId);

        if (proje == null)
            throw new InvalidOperationException("İhale projesi bulunamadı.");

        return proje;
    }

    private async Task<int> GetSonrakiVersiyonNoAsync(ApplicationDbContext context, int ihaleProjeId)
    {
        var sonVersiyonNo = await context.IhaleTeklifVersiyonlari
            .Where(v => v.IhaleProjeId == ihaleProjeId)
            .MaxAsync(v => (int?)v.VersiyonNo) ?? 0;

        return sonVersiyonNo + 1;
    }

    private async Task<Kullanici> GetCurrentUserOrThrowAsync(ApplicationDbContext context)
    {
        var kullanici = await _kullaniciService.GetAktifKullaniciAsync();
        if (kullanici == null)
            throw new InvalidOperationException("Bu işlem için oturum açmış kullanıcı gereklidir.");

        return kullanici;
    }

    private static void EnsureCanManageDrafts(Kullanici kullanici)
    {
        if (!HasAnyRole(kullanici, "Admin", "Operasyon"))
            throw new InvalidOperationException("Bu işlem için Admin veya Operasyon rolü gereklidir.");
    }

    private static void EnsureCanApprove(Kullanici kullanici)
    {
        if (!HasAnyRole(kullanici, "Admin", "Yönetici", "Yonetici"))
            throw new InvalidOperationException("Bu işlem için Admin veya Yönetici rolü gereklidir.");
    }

    private static bool HasAnyRole(Kullanici kullanici, params string[] roller)
    {
        var rolAdi = kullanici.Rol?.RolAdi;
        if (string.IsNullOrWhiteSpace(rolAdi))
            return false;

        return roller.Any(rol => string.Equals(rolAdi, rol, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SnapshotDoldurAsync(ApplicationDbContext context, IhaleTeklifVersiyon versiyon)
    {
        var ozet = await _ihaleHazirlikService.GetProjeOzetAsync(versiyon.IhaleProjeId);
        versiyon.ToplamMaliyet = ozet.ToplamProjeMaliyeti;
        versiyon.TeklifTutari = ozet.ToplamProjeTeklif;
        versiyon.KarMarjiTutari = ozet.ToplamProjeKar;
        versiyon.KarMarjiOrani = ozet.KarMarjiOrtalama;
    }

    private async Task PasiflestirDigerAktifVersiyonlarAsync(ApplicationDbContext context, int ihaleProjeId, int? haricVersiyonId)
    {
        var aktifVersiyonlar = await context.IhaleTeklifVersiyonlari
            .AsTracking()
            .Where(v => v.IhaleProjeId == ihaleProjeId && v.AktifVersiyon && (!haricVersiyonId.HasValue || v.Id != haricVersiyonId.Value))
            .ToListAsync();

        foreach (var aktifVersiyon in aktifVersiyonlar)
        {
            aktifVersiyon.AktifVersiyon = false;
            aktifVersiyon.UpdatedAt = DateTime.UtcNow;
        }
    }

    private Task KararLogEkleAsync(ApplicationDbContext context, 
        int versiyonId,
        IhaleTeklifIslemTipi islemTipi,
        IhaleTeklifVersiyonDurum? oncekiDurum,
        IhaleTeklifVersiyonDurum yeniDurum,
        string? not,
        int? kullaniciId)
    {
        context.IhaleTeklifKararLoglari.Add(new IhaleTeklifKararLog
        {
            IhaleTeklifVersiyonId = versiyonId,
            IslemTipi = islemTipi,
            OncekiDurum = oncekiDurum,
            YeniDurum = yeniDurum,
            Not = not,
            IslemYapanKullaniciId = kullaniciId,
            IslemTarihi = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    private async Task TryAuditAsync(ApplicationDbContext context, string islemTipi, int entityId, string aciklama)
    {
        try
        {
            await _auditLogService.LogCustomAsync(islemTipi, nameof(IhaleTeklifVersiyon), entityId, aciklama, AuditKategorileri.Sistem);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "İhale teklif versiyon audit kaydı oluşturulamadı. EntityId: {EntityId}", entityId);
        }
    }
}


