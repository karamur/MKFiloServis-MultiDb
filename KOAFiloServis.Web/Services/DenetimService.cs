using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Finans zinciri denetim motoru.
/// Fatura=Muhasebe, Snapshot=Dashboard tutarlılığını kontrol eder.
/// </summary>
public class DenetimService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public DenetimService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DenetimRaporu> DenetleAsync(int firmaId, int yil, int ay)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rapor = new DenetimRaporu { FirmaId = firmaId, Yil = yil, Ay = ay };

        var hakedisler = await db.HakedisPuantajlar
            .Where(h => h.FirmaId == firmaId && h.Yil == yil && h.Ay == ay && !h.IsDeleted)
            .ToListAsync();

        var snapshot = await db.MaasOdemeSnapshotlar
            .Where(s => s.FirmaId == firmaId && s.Yil == yil && s.Ay == ay && !s.IsDeleted)
            .ToListAsync();

        var snapshotTx = await db.SnapshotTransactions
            .Where(t => t.FirmaId == firmaId && t.Yil == yil && t.Ay == ay && !t.IsDeleted)
            .ToListAsync();

        // Kontrol 1: HakedisPuantaj toplamı == Snapshot
        var hakedisGelir = hakedisler.Sum(h => h.TahsilEdilecekTutar);
        var hakedisGider = hakedisler.Sum(h => h.OdenecekTutar);
        var snapshotGelir = snapshot.Sum(s => s.HakedisGelir);
        var snapshotGider = snapshot.Sum(s => s.HakedisGider);

        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "Hakedis Gelir == Snapshot HakedisGelir",
            Gecti = hakedisGelir == snapshotGelir,
            Beklenen = hakedisGelir,
            Gerceklesen = snapshotGelir,
            Fark = hakedisGelir - snapshotGelir
        });
        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "Hakedis Gider == Snapshot HakedisGider",
            Gecti = hakedisGider == snapshotGider,
            Beklenen = hakedisGider,
            Gerceklesen = snapshotGider,
            Fark = hakedisGider - snapshotGider
        });

        // Kontrol 2: SnapshotTransaction toplamı == Snapshot
        var txGelir = snapshotTx.Sum(t => t.GelirDelta);
        var txGider = snapshotTx.Sum(t => t.GiderDelta);
        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "SnapshotTransaction GelirDelta == Snapshot HakedisGelir",
            Gecti = txGelir == snapshotGelir,
            Beklenen = snapshotGelir,
            Gerceklesen = txGelir,
            Fark = snapshotGelir - txGelir
        });
        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "SnapshotTransaction GiderDelta == Snapshot HakedisGider",
            Gecti = txGider == snapshotGider,
            Beklenen = snapshotGider,
            Gerceklesen = txGider,
            Fark = snapshotGider - txGider
        });

        // Kontrol 3: Yarım zincir (Hakedis var ama fatura yok)
        var yarimZincir = hakedisler.Where(h => h.GelirFaturaId == null && h.GiderFaturaId == null).ToList();
        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "Yarım zincir (faturasız hakedis)",
            Gecti = !yarimZincir.Any(),
            Beklenen = 0,
            Gerceklesen = yarimZincir.Count,
            Fark = yarimZincir.Count,
            Detay = yarimZincir.Any() ? string.Join(", ", yarimZincir.Select(h => $"#{h.Id}")) : null
        });

        // Kontrol 4: Eksik muhasebe fişi (Fatura var, Muhasebe yok)
        var faturalar = await db.Faturalar
            .Where(f => f.FirmaId == firmaId && f.FaturaTarihi.Year == yil && f.FaturaTarihi.Month == ay && !f.IsDeleted)
            .ToListAsync();
        var eksikMuhasebe = faturalar.Where(f => f.MuhasebeFisId == null).ToList();
        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "Faturası olan ama muhasebe fişi olmayan",
            Gecti = !eksikMuhasebe.Any(),
            Beklenen = 0,
            Gerceklesen = eksikMuhasebe.Count,
            Fark = eksikMuhasebe.Count,
            Detay = eksikMuhasebe.Any() ? string.Join(", ", eksikMuhasebe.Select(f => $"#{f.Id} {f.FaturaNo}")) : null
        });

        // Kontrol 5: Yetim fatura (HakedisPuantaj silinmiş ama fatura duruyor)
        var hakedisFaturaIds = hakedisler
            .SelectMany(h => new[] { h.GelirFaturaId, h.GiderFaturaId })
            .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        var yetimFaturalar = faturalar.Where(f => !hakedisFaturaIds.Contains(f.Id)).ToList();
        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "Yetim fatura (HakedisPuantaj bağlantısı kopuk)",
            Gecti = !yetimFaturalar.Any(),
            Beklenen = 0,
            Gerceklesen = yetimFaturalar.Count,
            Fark = yetimFaturalar.Count
        });

        // Kontrol 6: Snapshot sapması (tolerans 1₺)
        var gelirFark = Math.Abs(snapshotGelir - hakedisGelir);
        var giderFark = Math.Abs(snapshotGider - hakedisGider);
        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "Snapshot-Hakedis sapması (tolerans ≤1₺)",
            Gecti = gelirFark <= 1 && giderFark <= 1,
            Beklenen = 0,
            Gerceklesen = gelirFark + giderFark,
            Fark = gelirFark + giderFark
        });
        var islenmisler = hakedisler.Where(h => h.GelirFaturaId != null || h.GiderFaturaId != null).ToList();
        var txHakedisIds = snapshotTx.Select(t => t.HakedisPuantajId).ToHashSet();
        var eksikTx = islenmisler.Where(h => !txHakedisIds.Contains(h.Id)).ToList();
        rapor.Kontroller.Add(new DenetimKontrol
        {
            Ad = "Her işlenmiş hakediş için SnapshotTransaction var",
            Gecti = !eksikTx.Any(),
            Beklenen = islenmisler.Count,
            Gerceklesen = islenmisler.Count - eksikTx.Count,
            Fark = eksikTx.Count
        });

        rapor.GecenKontrol = rapor.Kontroller.Count(k => k.Gecti);
        rapor.KalanKontrol = rapor.Kontroller.Count(k => !k.Gecti);
        rapor.Temiz = rapor.KalanKontrol == 0;

        // 🔴 Denetim Skoru (0-100)
        rapor.Skor = 100;
        if (rapor.KalanKontrol > 0)
        {
            // Her başarısız kontrol -15 puan
            rapor.Skor = Math.Max(0, 100 - rapor.KalanKontrol * 15);
            // Snapshot mismatch varsa ekstra -25
            if (gelirFark > 1 || giderFark > 1) rapor.Skor = Math.Max(0, rapor.Skor - 25);
        }

        // SystemHealth kaydını güncelle
        var health = await db.SystemHealths
            .FirstOrDefaultAsync(h => h.FirmaId == firmaId && h.Yil == yil && h.Ay == ay && !h.IsDeleted);
        if (health == null)
        {
            health = new SystemHealth { FirmaId = firmaId, Yil = yil, Ay = ay, CreatedAt = DateTime.UtcNow };
            db.SystemHealths.Add(health);
        }
        health.IsHealthy = rapor.Temiz;
        health.DenetimSkoru = rapor.Skor;
        health.IncidentCount = rapor.KalanKontrol;
        health.CriticalCount = gelirFark > 1 || giderFark > 1 ? 1 : 0;
        health.LastError = rapor.Temiz ? null : $"Skor: {rapor.Skor} — {rapor.KalanKontrol} kontrol başarısız";
        health.LastCheck = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // 🔴 Kritik hata → Incident oluştur
        if (rapor.Skor < 70)
        {
            db.IncidentLogs.Add(new IncidentLog
            {
                FirmaId = firmaId,
                Level = "Critical",
                Message = $"Denetim KRİTİK (Skor: {rapor.Skor}) — {rapor.KalanKontrol} kontrol başarısız",
                Entity = "DenetimRaporu",
                BeklenenDeger = 100,
                GerceklesenDeger = rapor.Skor,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        return rapor;
    }
}

public class DenetimRaporu
{
    public int FirmaId { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }
    public List<DenetimKontrol> Kontroller { get; set; } = [];
    public int GecenKontrol { get; set; }
    public int KalanKontrol { get; set; }
    public bool Temiz { get; set; }
    public int Skor { get; set; } = 100; // 0-100: 100=Sağlıklı, 70-99=Riskli, <70=Kritik
    public string SkorDurum => Skor >= 100 ? "✅ Sağlıklı" : Skor >= 70 ? "⚠️ Riskli" : "❌ Kritik";
}

public class DenetimKontrol
{
    public string Ad { get; set; } = string.Empty;
    public bool Gecti { get; set; }
    public decimal Beklenen { get; set; }
    public decimal Gerceklesen { get; set; }
    public decimal Fark { get; set; }
    public string? Detay { get; set; }
}
