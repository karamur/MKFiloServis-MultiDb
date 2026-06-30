using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Puantaj onay workflow: Finans → Muhasebe → Kilit.
/// Her durum geçişinde audit log üretir.
/// </summary>
public sealed class PuantajWorkflowService : IPuantajWorkflowService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public PuantajWorkflowService(IDbContextFactory<ApplicationDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task FinansOnaylaAsync(int hesapDonemiId, string onaylayan, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId)
            ?? throw new InvalidOperationException("Hesap dönemi bulunamadı.");

        if (h.Durum != PuantajHesapDurum.Aktif)
            throw new InvalidOperationException("Sadece Aktif hesap dönemi onaylanabilir.");
        if (h.OnayDurum != PuantajDonemOnayDurum.Bekliyor)
            throw new InvalidOperationException($"Finans onayı için OnayDurum Bekliyor olmalı. Mevcut: {h.OnayDurum}");

        var onceki = h.OnayDurum.ToString();
        h.OnayDurum = PuantajDonemOnayDurum.FinansOnaylandi;
        h.FinansOnaylayan = onaylayan;
        h.FinansOnayTarihi = DateTime.UtcNow;
        h.UpdatedAt = DateTime.UtcNow;

        db.PuantajAuditLogs.Add(AuditLog(h, PuantajAuditAksiyon.FinansOnaylandi, onaylayan, onceki, h.OnayDurum.ToString()));
        await db.SaveChangesAsync(ct);
    }

    public async Task MuhasebeOnaylaAsync(int hesapDonemiId, string onaylayan, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId)
            ?? throw new InvalidOperationException("Hesap dönemi bulunamadı.");

        if (h.Durum != PuantajHesapDurum.Aktif)
            throw new InvalidOperationException("Sadece Aktif hesap dönemi onaylanabilir.");
        if (h.OnayDurum != PuantajDonemOnayDurum.FinansOnaylandi)
            throw new InvalidOperationException("Muhasebe onayı için önce Finans onayı gereklidir.");

        var onceki = h.OnayDurum.ToString();
        h.OnayDurum = PuantajDonemOnayDurum.MuhasebeOnaylandi;
        h.MuhasebeOnaylayan = onaylayan;
        h.MuhasebeOnayTarihi = DateTime.UtcNow;
        h.UpdatedAt = DateTime.UtcNow;

        db.PuantajAuditLogs.Add(AuditLog(h, PuantajAuditAksiyon.MuhasebeOnaylandi, onaylayan, onceki, h.OnayDurum.ToString()));
        await db.SaveChangesAsync(ct);
    }

    public async Task KilitleAsync(int hesapDonemiId, string kilitleyen, string? aciklama = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId)
            ?? throw new InvalidOperationException("Hesap dönemi bulunamadı.");

        if (h.Durum != PuantajHesapDurum.Aktif)
            throw new InvalidOperationException("Sadece Aktif hesap dönemi kilitlenebilir.");
        if (h.OnayDurum != PuantajDonemOnayDurum.MuhasebeOnaylandi)
            throw new InvalidOperationException("Kilitleme için önce Muhasebe onayı gereklidir.");

        var onceki = h.OnayDurum.ToString();
        h.OnayDurum = PuantajDonemOnayDurum.Kilitli;
        h.KilitTarihi = DateTime.UtcNow;
        h.KilitAciklama = aciklama;
        h.UpdatedAt = DateTime.UtcNow;

        db.PuantajAuditLogs.Add(AuditLog(h, PuantajAuditAksiyon.Kilitlendi, kilitleyen, onceki, h.OnayDurum.ToString(), aciklama));
        await db.SaveChangesAsync(ct);
    }

    public async Task KilitAcAsync(int hesapDonemiId, string acan, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId)
            ?? throw new InvalidOperationException("Hesap dönemi bulunamadı.");

        if (h.OnayDurum != PuantajDonemOnayDurum.Kilitli)
            throw new InvalidOperationException("Sadece Kilitli dönemin kilidi açılabilir.");

        var onceki = h.OnayDurum.ToString();
        h.OnayDurum = PuantajDonemOnayDurum.MuhasebeOnaylandi;
        h.KilitTarihi = null;
        h.KilitAciklama = null;
        h.UpdatedAt = DateTime.UtcNow;

        db.PuantajAuditLogs.Add(AuditLog(h, PuantajAuditAksiyon.KilitAcildi, acan, onceki, h.OnayDurum.ToString()));
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<PuantajAuditLog>> GetAuditLogsAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PuantajAuditLogs
            .Where(l => l.HesapDonemiId == hesapDonemiId && !l.IsDeleted)
            .OrderByDescending(l => l.AksiyonTarihi)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<bool> FinansOnaylanabilirMiAsync(int hesapDonemiId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId);
        return h is { Durum: PuantajHesapDurum.Aktif, OnayDurum: PuantajDonemOnayDurum.Bekliyor };
    }

    public async Task<bool> MuhasebeOnaylanabilirMiAsync(int hesapDonemiId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId);
        return h is { Durum: PuantajHesapDurum.Aktif, OnayDurum: PuantajDonemOnayDurum.FinansOnaylandi };
    }

    public async Task<bool> KilitlenebilirMiAsync(int hesapDonemiId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId);
        return h is { Durum: PuantajHesapDurum.Aktif, OnayDurum: PuantajDonemOnayDurum.MuhasebeOnaylandi };
    }

    public async Task<bool> RevizyonYapilabilirMiAsync(int hesapDonemiId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId);
        return h is { Durum: PuantajHesapDurum.Aktif, OnayDurum: not PuantajDonemOnayDurum.Kilitli };
    }

    private static PuantajAuditLog AuditLog(PuantajHesapDonemi h, PuantajAuditAksiyon aksiyon,
        string? kullanici, string onceki, string yeni, string? aciklama = null)
        => new()
        {
            FirmaId = h.FirmaId,
            HesapDonemiId = h.Id,
            Aksiyon = aksiyon,
            Kullanici = kullanici,
            AksiyonTarihi = DateTime.UtcNow,
            OncekiDurum = onceki,
            YeniDurum = yeni,
            Aciklama = aciklama,
            CreatedAt = DateTime.UtcNow
        };
}


