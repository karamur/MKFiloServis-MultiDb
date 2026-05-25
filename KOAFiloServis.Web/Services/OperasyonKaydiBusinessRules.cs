using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// OperasyonKaydi domain kuralları (çakışma kontrolü, operasyonel kısıtlar).
/// </summary>
public sealed class OperasyonKaydiBusinessRules
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public OperasyonKaydiBusinessRules(IDbContextFactory<ApplicationDbContext> dbFactory)
        => _dbFactory = dbFactory;

    /// <summary>
    /// Aynı Tarih + Guzergah + Slot için farklı araç atanmış mı kontrol eder.
    /// </summary>
    public async Task<List<string>> CheckConflictsAsync(OperasyonKaydi kayit)
    {
        var errors = new List<string>();

        await using var db = await _dbFactory.CreateDbContextAsync();

        var conflict = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted
                        && o.Id != kayit.Id
                        && o.Tarih == kayit.Tarih
                        && o.GuzergahId == kayit.GuzergahId
                        && o.Slot == kayit.Slot
                        && o.AracId != kayit.AracId)
            .Select(o => o.Arac!.AktifPlaka ?? o.Arac!.Plaka)
            .FirstOrDefaultAsync();

        if (conflict != null)
            errors.Add($"Bu gün/slot için '{conflict}' plakalı araç zaten atanmış.");

        return errors;
    }

    /// <summary>
    /// Operasyonel kural: Gitti durumunda sefer sayısı 0 olamaz.
    /// </summary>
    public static List<string> CheckOperationalRules(OperasyonKaydi kayit)
    {
        var errors = new List<string>();

        if (kayit.OperasyonDurumu == OperasyonDurumu.Gitti && kayit.SeferSayisi <= 0)
            errors.Add("'Gitti' durumunda sefer sayısı en az 1 olmalıdır.");

        return errors;
    }
}
