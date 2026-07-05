using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

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
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public FirmaService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
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

        firma.FirmaKodu = await ResolveUniqueFirmaCodeAsync(context, firma.FirmaKodu);
        firma.FirmaAdi = firma.FirmaAdi.Trim();
        firma.UnvanTam = NormalizeOptional(firma.UnvanTam);
        firma.VergiNo = NormalizeOptional(firma.VergiNo);
        firma.VergiDairesi = NormalizeOptional(firma.VergiDairesi);
        firma.Adres = NormalizeOptional(firma.Adres);
        firma.Il = NormalizeOptional(firma.Il);
        firma.Ilce = NormalizeOptional(firma.Ilce);
        firma.Telefon = NormalizeOptional(firma.Telefon);
        firma.Email = NormalizeOptional(firma.Email);
        firma.WebSite = NormalizeOptional(firma.WebSite);
        firma.OrganizasyonId = await EnsureOrganizasyonIdAsync(context, firma.OrganizasyonId);
        firma.CreatedAt = DateTime.UtcNow;

        if (!await context.Firmalar.IgnoreQueryFilters().AnyAsync(f => !f.IsDeleted))
        {
            firma.VarsayilanFirma = true;
        }

        context.Firmalar.Add(firma);
        await context.SaveChangesAsync();
        return firma;
    }

    public async Task<Firma> UpdateAsync(Firma firma)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.Firmalar.FindAsync(firma.Id);
        if (existing == null)
            throw new InvalidOperationException($"Firma bulunamadi: {firma.Id}");

        existing.FirmaKodu = firma.FirmaKodu;
        existing.FirmaAdi = firma.FirmaAdi;
        existing.UnvanTam = firma.UnvanTam;
        existing.VergiNo = firma.VergiNo;
        existing.VergiDairesi = firma.VergiDairesi;
        existing.Adres = firma.Adres;
        existing.Il = firma.Il;
        existing.Ilce = firma.Ilce;
        existing.Telefon = firma.Telefon;
        existing.Email = firma.Email;
        existing.WebSite = firma.WebSite;
        existing.Logo = firma.Logo;
        existing.Aktif = firma.Aktif;
        existing.CariId = firma.CariId;
        existing.UpdatedAt = DateTime.UtcNow;
        // DatabaseName LEGACY — Tenant DB terk edildi, kolon geriye dönük uyumluluk için korunur

        await context.SaveChangesAsync();
        return existing;
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

    private async Task<int> EnsureOrganizasyonIdAsync(ApplicationDbContext context, int organizasyonId)
    {
        if (organizasyonId > 0)
        {
            var mevcutOrganizasyon = await context.Organizasyonlar
                .IgnoreQueryFilters()
                .AnyAsync(o => o.Id == organizasyonId && !o.IsDeleted);

            if (mevcutOrganizasyon)
                return organizasyonId;
        }

        var varsayilanOrganizasyon = await context.Organizasyonlar
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted)
            .OrderBy(o => o.Id)
            .FirstOrDefaultAsync();

        if (varsayilanOrganizasyon != null)
            return varsayilanOrganizasyon.Id;

        var yeniOrganizasyon = new Organizasyon
        {
            Adi = "Ustun Holding",
            Kod = "USTUNHOLDING",
            Aciklama = "Otomatik olusturulan varsayilan organizasyon",
            CreatedAt = DateTime.UtcNow
        };

        context.Organizasyonlar.Add(yeniOrganizasyon);
        await context.SaveChangesAsync();
        return yeniOrganizasyon.Id;
    }

    private async Task<string> ResolveUniqueFirmaCodeAsync(ApplicationDbContext context, string? requestedCode)
    {
        var normalizedCode = NormalizeFirmaCode(requestedCode);
        var codeExists = await context.Firmalar
            .IgnoreQueryFilters()
            .AnyAsync(f => f.FirmaKodu == normalizedCode);

        if (!codeExists)
            return normalizedCode;

        var nextNumber = 1;
        var existingNumbers = await context.Firmalar
            .IgnoreQueryFilters()
            .Select(f => f.FirmaKodu)
            .ToListAsync();

        foreach (var code in existingNumbers)
        {
            if (string.IsNullOrWhiteSpace(code) || !code.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(code[1..], out var parsed) && parsed >= nextNumber)
            {
                nextNumber = parsed + 1;
            }
        }

        var candidate = $"F{nextNumber:000}";
        while (existingNumbers.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            nextNumber++;
            candidate = $"F{nextNumber:000}";
        }

        return candidate;
    }

    private static string NormalizeFirmaCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "F001";

        return code.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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



