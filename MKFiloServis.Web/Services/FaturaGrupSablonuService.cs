using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Fatura hazırlık raporu ağaç gruplama şablonu CRUD servisi.
/// Her kullanıcı birden fazla şablon kaydedebilir, birini varsayılan yapabilir.
/// </summary>
public class FaturaGrupSablonuService : IFaturaGrupSablonuService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public FaturaGrupSablonuService(IDbContextFactory<ApplicationDbContext> dbFactory, IAktifFirmaProvider aktifFirmaProvider,
        IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider authenticationStateProvider)
    {
        _dbFactory = dbFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<List<FaturaGrupSablonu>> GetByFirmaAsync(int firmaId, int? kullaniciId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userId = await GetUserIdAsync(db, ct);
        KullaniciKapsaminiDogrula(kullaniciId, userId);
        var aktifFirmaId = await GetAktifFirmaIdAsync(db, ct);
        if (firmaId != aktifFirmaId)
            throw new UnauthorizedAccessException("Şablonlara yalnız seçili firma kapsamında erişilebilir.");
        var query = db.FaturaGrupSablonlari
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted && (x.KullaniciId == userId || x.KullaniciId == null));

        return await query.OrderByDescending(x => x.VarsayilanMi).ThenBy(x => x.Ad)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<FaturaGrupSablonu?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        KayitKimliginiDogrula(id);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userId = await GetUserIdAsync(db, ct);
        var firmaId = await GetAktifFirmaIdAsync(db, ct);
        return await db.FaturaGrupSablonlari
            .Where(x => x.Id == id && x.FirmaId == firmaId && !x.IsDeleted && (x.KullaniciId == null || x.KullaniciId == userId))
            .AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public async Task<FaturaGrupSablonu?> GetVarsayilanAsync(int firmaId, int? kullaniciId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userId = await GetUserIdAsync(db, ct);
        KullaniciKapsaminiDogrula(kullaniciId, userId);
        var aktifFirmaId = await GetAktifFirmaIdAsync(db, ct);
        if (firmaId != aktifFirmaId)
            throw new UnauthorizedAccessException("Şablonlara yalnız seçili firma kapsamında erişilebilir.");
        var query = db.FaturaGrupSablonlari
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted && x.VarsayilanMi);

        if (kullaniciId.HasValue)
            query = query.Where(x => x.KullaniciId == userId);
        else
            query = query.Where(x => x.KullaniciId == null);

        return await query.AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public async Task<FaturaGrupSablonu> CreateAsync(FaturaGrupSablonu sablon, CancellationToken ct = default)
    {
        if (sablon.Id != 0)
            throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.GecersizIstek, "Yeni grup şablonunun kayıt kimliği sıfır olmalıdır.");
        SablonuDogrula(sablon);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userId = await GetUserIdAsync(db, ct);
        KullaniciKapsaminiDogrula(sablon.KullaniciId, userId);
        var firmaId = await GetAktifFirmaIdAsync(db, ct);
        if (sablon.FirmaId is not null and not 0 && sablon.FirmaId != firmaId)
            throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.GecersizIstek, "Grup şablonu yalnız seçili firma için oluşturulabilir.");
        await FirmaGeneliYazimYetkisiniDogrulaAsync(db, userId, sablon.KullaniciId, ct);
        var yeni = new FaturaGrupSablonu
        {
            FirmaId = firmaId,
            KullaniciId = sablon.KullaniciId,
            Ad = sablon.Ad.Trim(),
            AgacYapisi = sablon.AgacYapisi,
            VarsayilanMi = sablon.VarsayilanMi
        };

        // Eğer varsayılan ise, mevcut varsayılanları kaldır
        if (yeni.VarsayilanMi)
            await UnsetVarsayilanAsync(db, firmaId, yeni.KullaniciId, ct);

        db.FaturaGrupSablonlari.Add(yeni);
        await db.SaveChangesAsync(ct);
        return yeni;
    }

    public async Task<FaturaGrupSablonu> UpdateAsync(FaturaGrupSablonu sablon, CancellationToken ct = default)
    {
        KayitKimliginiDogrula(sablon.Id);
        SablonuDogrula(sablon);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userId = await GetUserIdAsync(db, ct);
        KullaniciKapsaminiDogrula(sablon.KullaniciId, userId);
        var firmaId = await GetAktifFirmaIdAsync(db, ct);
        var existing = await db.FaturaGrupSablonlari.AsTracking()
            .Where(x => x.Id == sablon.Id && x.FirmaId == firmaId && !x.IsDeleted && (x.KullaniciId == null || x.KullaniciId == userId))
            .FirstOrDefaultAsync(ct)
            ?? throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.Bulunamadi, "Şablon bulunamadı veya erişilebilir değil.");

        await FirmaGeneliYazimYetkisiniDogrulaAsync(db, userId, existing.KullaniciId, ct);
        existing.Ad = sablon.Ad.Trim();
        existing.AgacYapisi = sablon.AgacYapisi;
        existing.UpdatedAt = DateTime.UtcNow;

        // Varsayılan durumu değiştiyse
        if (sablon.VarsayilanMi && !existing.VarsayilanMi)
        {
            await UnsetVarsayilanAsync(db, firmaId, existing.KullaniciId, ct, existing.Id);
            existing.VarsayilanMi = true;
        }
        else if (!sablon.VarsayilanMi)
        {
            existing.VarsayilanMi = false;
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        KayitKimliginiDogrula(id);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userId = await GetUserIdAsync(db, ct);
        var firmaId = await GetAktifFirmaIdAsync(db, ct);
        var sablon = await db.FaturaGrupSablonlari.AsTracking()
            .Where(x => x.Id == id && x.FirmaId == firmaId && !x.IsDeleted && (x.KullaniciId == null || x.KullaniciId == userId))
            .FirstOrDefaultAsync(ct);

        if (sablon == null) return false;

        await FirmaGeneliYazimYetkisiniDogrulaAsync(db, userId, sablon.KullaniciId, ct);
        sablon.IsDeleted = true;
        sablon.DeletedAt = DateTime.UtcNow;
        sablon.UpdatedAt = sablon.DeletedAt;
        sablon.VarsayilanMi = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetVarsayilanAsync(int id, CancellationToken ct = default)
    {
        KayitKimliginiDogrula(id);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userId = await GetUserIdAsync(db, ct);
        var firmaId = await GetAktifFirmaIdAsync(db, ct);
        var sablon = await db.FaturaGrupSablonlari.AsTracking()
            .Where(x => x.Id == id && x.FirmaId == firmaId && !x.IsDeleted && (x.KullaniciId == null || x.KullaniciId == userId))
            .FirstOrDefaultAsync(ct);

        if (sablon == null) return false;

        await FirmaGeneliYazimYetkisiniDogrulaAsync(db, userId, sablon.KullaniciId, ct);
        await UnsetVarsayilanAsync(db, firmaId, sablon.KullaniciId, ct, sablon.Id);
        sablon.VarsayilanMi = true;
        sablon.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Helper ──

    private async Task<int> GetUserIdAsync(ApplicationDbContext db, CancellationToken ct)
    {
        // HTTP isteğinde kimliği HTTP'den al; anonim isteği circuit kimliğiyle yükseltme.
        var principal = _httpContextAccessor.HttpContext is { } http
            ? http.User
            : (await _authenticationStateProvider.GetAuthenticationStateAsync()).User;
        if (principal.Identity?.IsAuthenticated != true ||
            !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("KullaniciId")?.Value, out var userId) || userId <= 0)
            throw new UnauthorizedAccessException("Geçerli bir kullanıcı oturumu gereklidir.");
        if (!await db.Kullanicilar.AnyAsync(x => x.Id == userId && x.Aktif && !x.IsDeleted, ct))
            throw new UnauthorizedAccessException("Kullanıcı aktif değil veya erişilebilir değil.");
        return userId;
    }

    private static void KullaniciKapsaminiDogrula(int? requestedUserId, int userId)
    {
        if (requestedUserId.HasValue && requestedUserId.Value != userId)
            throw new UnauthorizedAccessException("Başka kullanıcının özel şablonuna erişilemez.");
    }

    private static async Task FirmaGeneliYazimYetkisiniDogrulaAsync(
        ApplicationDbContext db, int userId, int? sablonKullaniciId, CancellationToken ct)
    {
        // Kişisel şablonlarda mevcut sahiplik kontrolü; ortak şablonda hazırlık düzenleme izni.
        if (sablonKullaniciId.HasValue) return;
        var izin = await db.Kullanicilar
            .Where(x => x.Id == userId && x.Aktif && !x.IsDeleted && !x.Rol.IsDeleted)
            .AnyAsync(x => x.Rol.RolAdi == SistemRolleri.Admin ||
                x.Rol.Yetkiler.Any(y => y.YetkiKodu == Yetkiler.FaturaHazirlikDuzenle && y.Izin && !y.IsDeleted), ct);
        if (!izin)
            throw new UnauthorizedAccessException("Firma geneli grup şablonlarını yönetmek için fatura hazırlık düzenleme yetkisi gereklidir.");
    }

    private async Task<int> GetAktifFirmaIdAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var firmaId = _aktifFirmaProvider.AktifFirmaId;
        if (_aktifFirmaProvider.TumFirmalar || firmaId is not > 0)
            throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.GecersizIstek, "Fatura grup şablonu işlemi için tek bir firma seçin.");
        if (!await db.Firmalar.AnyAsync(x => x.Id == firmaId.Value && !x.IsDeleted, ct))
            throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.GecersizIstek, "Seçili firma bulunamadı veya erişilebilir değil.");
        return firmaId.Value;
    }

    private static void KayitKimliginiDogrula(int id)
    {
        if (id <= 0)
            throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.GecersizIstek, "Şablon kimliği pozitif olmalıdır.");
    }

    private static void SablonuDogrula(FaturaGrupSablonu sablon)
    {
        if (string.IsNullOrWhiteSpace(sablon.Ad) || sablon.Ad.Trim().Length > 150)
            throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.GecersizIstek, "Şablon adı zorunludur ve en fazla 150 karakter olabilir.");
        if (!Enum.IsDefined(sablon.AgacYapisi))
            throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.GecersizIstek, "Geçersiz fatura gruplama yapısı.");
        if (sablon.KullaniciId is <= 0)
            throw new FaturaGrupSablonuException(FaturaGrupSablonuHata.GecersizIstek, "Kullanıcı kimliği pozitif olmalı veya firma geneli için boş bırakılmalıdır.");
    }

    private static async Task UnsetVarsayilanAsync(ApplicationDbContext db, int firmaId, int? kullaniciId, CancellationToken ct, int? haricId = null)
    {
        var query = db.FaturaGrupSablonlari.AsTracking()
            .Where(x => x.FirmaId == firmaId && x.VarsayilanMi && !x.IsDeleted);

        if (kullaniciId.HasValue)
            query = query.Where(x => x.KullaniciId == kullaniciId.Value);
        else
            query = query.Where(x => x.KullaniciId == null);

        if (haricId.HasValue)
            query = query.Where(x => x.Id != haricId.Value);
        var sablonlar = await query.ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var sablon in sablonlar)
        {
            sablon.VarsayilanMi = false;
            sablon.UpdatedAt = now;
        }
        // Çağıran yeni varsayılanla birlikte tek SaveChanges yapar.
    }
}


