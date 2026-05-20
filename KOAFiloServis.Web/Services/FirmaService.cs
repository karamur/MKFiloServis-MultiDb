using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Firma CRUD + aktif firma cephesi (facade).
/// <para>
/// Aktif firma state'i artık <see cref="IAktifFirmaProvider"/> üzerinden tutulur;
/// böylece her circuit / kullanıcı kendi firmasına sahiptir. Eski <c>static</c>
/// yaklaşım Blazor Server'da multi-user veri sızıntısına yol açıyordu.
/// Mevcut UI çağrıları kırılmasın diye eski API imzaları korunup içerik
/// provider'a delege edilmiştir.
/// </para>
/// </summary>
public class FirmaService : IFirmaService
{
    private readonly IDbContextFactory<MasterDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public FirmaService(
        IDbContextFactory<MasterDbContext> contextFactory,
        IAktifFirmaProvider aktifFirmaProvider)
    {
        _contextFactory = contextFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    #region CRUD

    public async Task<List<Firma>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Firmalar
            .OrderBy(f => f.SiraNo)
            .ThenBy(f => f.FirmaAdi)
            .ToListAsync();
    }

    public async Task<List<Firma>> GetAktifFirmalarAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Firmalar
            .Where(f => f.Aktif)
            .OrderBy(f => f.SiraNo)
            .ThenBy(f => f.FirmaAdi)
            .ToListAsync();
    }

    public async Task<Firma?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Firmalar.FindAsync(id);
    }

    public async Task<Firma?> GetVarsayilanFirmaAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Firmalar
            .FirstOrDefaultAsync(f => f.VarsayilanFirma && f.Aktif);
    }

    public async Task<Firma> CreateAsync(Firma firma)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        firma.CreatedAt = DateTime.UtcNow;
        context.Firmalar.Add(firma);
        await context.SaveChangesAsync();
        return firma;
    }

    public async Task<Firma> UpdateAsync(Firma firma)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Entity'yi attach et ve modified olarak isaretle
        context.Firmalar.Attach(firma);
        context.Entry(firma).State = EntityState.Modified;
        
        firma.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return firma;
    }

    public async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var firma = await context.Firmalar.FindAsync(id);
        if (firma == null) return;

        firma.IsDeleted = true;
        firma.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task SetVarsayilanAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Onceki varsayilani kaldir
        var eskiVarsayilan = await context.Firmalar.Where(f => f.VarsayilanFirma).ToListAsync();
        foreach (var f in eskiVarsayilan)
        {
            f.VarsayilanFirma = false;
        }

        // Yeni varsayilan
        var firma = await context.Firmalar.FindAsync(firmaId);
        if (firma != null)
        {
            firma.VarsayilanFirma = true;
        }

        await context.SaveChangesAsync();
    }

    #endregion

    #region Aktif Firma Yonetimi

    public AktifFirmaBilgisi GetAktifFirma()
    {
        var mevcut = _aktifFirmaProvider.Mevcut;
        // Kullanıcı zaten bir firma seçtiyse veya bilinçli olarak "Tüm Firmalar" modundaysa
        // tekrar varsayılana düşürme.
        if (mevcut.FirmaId != 0 || mevcut.TumFirmalar)
            return mevcut;

        // İlk erişim: önce VarsayilanFirma=true olanı, yoksa ilk aktif firmayı seç.
        // "Tüm Firmalar" otomatik default DEĞİL — kullanıcı tek bir firma ile başlasın,
        // istediğinde üst menüden "Hepsi" diyebilsin.
        using var context = _contextFactory.CreateDbContext();
        var varsayilan = context.Firmalar.FirstOrDefault(f => f.VarsayilanFirma && f.Aktif)
                         ?? context.Firmalar.Where(f => f.Aktif).OrderBy(f => f.SiraNo).ThenBy(f => f.FirmaAdi).FirstOrDefault();

        var bilgi = varsayilan != null
            ? new AktifFirmaBilgisi
            {
                FirmaId = varsayilan.Id,
                FirmaKodu = varsayilan.FirmaKodu,
                FirmaAdi = varsayilan.FirmaAdi,
                AktifDonemYil = varsayilan.AktifDonemYil,
                AktifDonemAy = varsayilan.AktifDonemAy,
                DatabaseName = varsayilan.DatabaseName,
                TumFirmalar = false
            }
            : new AktifFirmaBilgisi
            {
                FirmaId = 0,
                FirmaKodu = "VARSAYILAN",
                FirmaAdi = "Firma Yok",
                AktifDonemYil = DateTime.Today.Year,
                AktifDonemAy = DateTime.Today.Month,
                DatabaseName = null,
                TumFirmalar = false
            };

        _aktifFirmaProvider.Set(bilgi);
        return bilgi;
    }

    public void SetAktifFirma(int firmaId)
    {
        using var context = _contextFactory.CreateDbContext();
        var firma = context.Firmalar.Find(firmaId);
        if (firma == null) return;

        _aktifFirmaProvider.Set(new AktifFirmaBilgisi
        {
            FirmaId = firma.Id,
            FirmaKodu = firma.FirmaKodu,
            FirmaAdi = firma.FirmaAdi,
            AktifDonemYil = firma.AktifDonemYil,
            AktifDonemAy = firma.AktifDonemAy,
            DatabaseName = firma.DatabaseName,
            TumFirmalar = false
        });
    }

    public void SetAktifFirma(AktifFirmaBilgisi firma)
        => _aktifFirmaProvider.Set(firma);

    public void SetTumFirmalar(bool tumFirmalar)
        => _aktifFirmaProvider.SetTumFirmalar(tumFirmalar);

    public void SetAktifDonem(int yil, int ay)
    {
        _aktifFirmaProvider.SetDonem(yil, ay);

        var mevcut = _aktifFirmaProvider.Mevcut;
        if (mevcut.FirmaId > 0)
        {
            using var context = _contextFactory.CreateDbContext();
            var firma = context.Firmalar.Find(mevcut.FirmaId);
            if (firma != null)
            {
                firma.AktifDonemYil = yil;
                firma.AktifDonemAy = ay;
                context.SaveChanges();
            }
        }
    }

    #endregion

    #region Seed

    public async Task SeedVarsayilanFirmaAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        if (await context.Firmalar.AnyAsync()) return;

        var firma = new Firma
        {
            FirmaKodu = "ANA",
            FirmaAdi = "Ana Firma",
            UnvanTam = "Ana Firma Ltd. Sti.",
            Aktif = true,
            VarsayilanFirma = true,
            AktifDonemYil = DateTime.Today.Year,
            AktifDonemAy = DateTime.Today.Month,
            CreatedAt = DateTime.UtcNow
        };

        context.Firmalar.Add(firma);
        await context.SaveChangesAsync();

        // Aktif firma olarak ayarla
        SetAktifFirma(firma.Id);
    }

    #endregion
}
