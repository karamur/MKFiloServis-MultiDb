using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public sealed class PuantajFinansService : IPuantajFinansService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IFaturaService _faturaService;
    private readonly ILogger<PuantajFinansService> _logger;

    public PuantajFinansService(IDbContextFactory<ApplicationDbContext> dbFactory, IFaturaService faturaService, ILogger<PuantajFinansService> logger)
    {
        _dbFactory = dbFactory;
        _faturaService = faturaService;
        _logger = logger;
    }

    public async Task<bool> FinansalKayitOlusturulabilirMiAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId);
        return h is { Durum: PuantajHesapDurum.Aktif, OnayDurum: PuantajDonemOnayDurum.Kilitli };
    }

    public async Task FinansalKayitOlusturAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        if (!await FinansalKayitOlusturulabilirMiAsync(hesapDonemiId, ct))
            throw new InvalidOperationException("Finansal kayıt için hesap dönemi Kilitli olmalıdır.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var mevcut = await db.PuantajFinansalKayitlar
            .Where(f => f.HesapDonemiId == hesapDonemiId && !f.IsDeleted)
            .Select(f => f.PuantajKayitId)
            .ToListAsync(ct);

        var pkList = await db.PuantajKayitlar
            .Where(p => p.HesapDonemiId == hesapDonemiId && !p.IsDeleted && !mevcut.Contains(p.Id))
            .ToListAsync(ct);

        var simdi = DateTime.UtcNow;
        foreach (var pk in pkList)
        {
            db.PuantajFinansalKayitlar.Add(new PuantajFinansalKayit
            {
                FirmaId = null,
                PuantajKayitId = pk.Id,
                HesapDonemiId = hesapDonemiId,
                BirimGelir = pk.BirimGelir,
                BirimGider = pk.BirimGider,
                ToplamGelir = pk.ToplamGelir,
                ToplamGider = pk.ToplamGider,
                KdvTutar = pk.GelirKdvTutari,
                GenelToplam = pk.Alinacak,
                SeferGunu = (int)pk.Gun,
                GelirCariId = pk.FaturaKesiciCariId ?? pk.KurumCariId,
                GiderCariId = pk.OdemeYapilacakCariId,
                KayitTarihi = simdi,
                CreatedAt = simdi
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<List<PuantajFinansalKayit>> FinansalKayitlariGetirAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PuantajFinansalKayitlar
            .Include(f => f.PuantajKayit).ThenInclude(p => p!.Guzergah)
            .Include(f => f.PuantajKayit).ThenInclude(p => p!.Arac)
            .Where(f => f.HesapDonemiId == hesapDonemiId && !f.IsDeleted)
            .OrderBy(f => f.PuantajKayit!.Guzergah!.GuzergahAdi)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<bool> FaturaUretilebilirMiAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var kayitVar = await db.PuantajFinansalKayitlar
            .AnyAsync(f => f.HesapDonemiId == hesapDonemiId && !f.IsDeleted, ct);
        return kayitVar;
    }

    public async Task<Fatura> GelirFaturasiUretAsync(int finansalKayitId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fk = await db.PuantajFinansalKayitlar
            .Include(f => f.PuantajKayit).ThenInclude(p => p!.Guzergah)
            .Include(f => f.PuantajKayit).ThenInclude(p => p!.Arac)
            .FirstOrDefaultAsync(f => f.Id == finansalKayitId && !f.IsDeleted, ct)
            ?? throw new InvalidOperationException("Finansal kayıt bulunamadı.");

        if (fk.GelirCariId == null)
            throw new InvalidOperationException("Gelir CariId tanımlı değil.");
        if (fk.GelirFaturaId != null)
            throw new InvalidOperationException("Bu kayıt için gelir faturası zaten üretilmiş.");

        var pk = fk.PuantajKayit!;
        var faturaNo = await _faturaService.GenerateNextFaturaNoAsync(FaturaTipi.SatisFaturasi, FaturaYonu.Giden);

        var fatura = new Fatura
        {
            FaturaNo = faturaNo,
            FaturaTarihi = DateTime.Today,
            FaturaTipi = FaturaTipi.SatisFaturasi,
            FaturaYonu = FaturaYonu.Giden,
            Durum = FaturaDurum.Beklemede,
            EFaturaTipi = EFaturaTipi.EArsiv,
            CariId = fk.GelirCariId.Value,
            AraToplam = fk.ToplamGelir,
            KdvOrani = pk.GelirKdvOrani,
            KdvTutar = fk.KdvTutar,
            GenelToplam = fk.GenelToplam,
            ImportKaynak = "Puantaj",
            Aciklama = $"{pk.Yil}/{pk.Ay:D2} {pk.GuzergahAdi} / {pk.Arac?.AktifPlaka ?? pk.Arac?.Plaka} Puantaj Gelir Faturası",
            CreatedAt = DateTime.UtcNow
        };

        fatura = await _faturaService.CreateAsync(fatura);

        // FaturaKalem ekle
        await using var db2 = await _dbFactory.CreateDbContextAsync();
        db2.FaturaKalemleri.Add(new FaturaKalem
        {
            FaturaId = fatura.Id,
            SiraNo = 1,
            Aciklama = $"{pk.GuzergahAdi} / {(pk.Arac?.AktifPlaka ?? pk.Arac?.Plaka)} / {pk.Slot}",
            Miktar = fk.SeferGunu,
            Birim = "Sefer Günü",
            BirimFiyat = fk.BirimGelir,
            KdvOrani = pk.GelirKdvOrani,
            KdvTutar = fk.KdvTutar,
            ToplamTutar = fk.GenelToplam,
            KalemTipi = FaturaKalemTipi.Servis,
            CreatedAt = DateTime.UtcNow
        });
        await db2.SaveChangesAsync(ct);

        // Finansal kaydı güncelle
        fk.GelirFaturaId = fatura.Id;
        fk.Durum = fk.GiderFaturaId != null ? PuantajFinansalDurum.TumFaturalarUretildi : PuantajFinansalDurum.GelirFaturasiUretildi;
        fk.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return fatura;
    }

    public async Task<Fatura> GiderFaturasiUretAsync(int finansalKayitId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fk = await db.PuantajFinansalKayitlar
            .Include(f => f.PuantajKayit).ThenInclude(p => p!.Guzergah)
            .Include(f => f.PuantajKayit).ThenInclude(p => p!.Arac)
            .FirstOrDefaultAsync(f => f.Id == finansalKayitId && !f.IsDeleted, ct)
            ?? throw new InvalidOperationException("Finansal kayıt bulunamadı.");

        if (fk.GiderCariId == null)
            throw new InvalidOperationException("Gider CariId tanımlı değil.");
        if (fk.GiderFaturaId != null)
            throw new InvalidOperationException("Bu kayıt için gider faturası zaten üretilmiş.");

        var pk = fk.PuantajKayit!;
        var faturaNo = await _faturaService.GenerateNextFaturaNoAsync(FaturaTipi.AlisFaturasi, FaturaYonu.Gelen);

        var fatura = new Fatura
        {
            FaturaNo = faturaNo,
            FaturaTarihi = DateTime.Today,
            FaturaTipi = FaturaTipi.AlisFaturasi,
            FaturaYonu = FaturaYonu.Gelen,
            Durum = FaturaDurum.Beklemede,
            EFaturaTipi = EFaturaTipi.EArsiv,
            CariId = fk.GiderCariId.Value,
            AraToplam = fk.ToplamGider,
            KdvOrani = pk.GiderKdvOrani20 + pk.GiderKdvOrani10,
            KdvTutar = pk.GiderKdv20Tutari + pk.GiderKdv10Tutari,
            GenelToplam = fk.ToplamGider + pk.GiderKdv20Tutari + pk.GiderKdv10Tutari,
            ImportKaynak = "Puantaj",
            Aciklama = $"{pk.Yil}/{pk.Ay:D2} {pk.GuzergahAdi} / {pk.Arac?.AktifPlaka ?? pk.Arac?.Plaka} Puantaj Gider Faturası",
            CreatedAt = DateTime.UtcNow
        };

        fatura = await _faturaService.CreateAsync(fatura);

        await using var db2 = await _dbFactory.CreateDbContextAsync();
        db2.FaturaKalemleri.Add(new FaturaKalem
        {
            FaturaId = fatura.Id,
            SiraNo = 1,
            Aciklama = $"Tedarikçi Ödemesi: {pk.GuzergahAdi} / {(pk.Arac?.AktifPlaka ?? pk.Arac?.Plaka)}",
            Miktar = fk.SeferGunu,
            Birim = "Sefer Günü",
            BirimFiyat = fk.BirimGider,
            KdvOrani = pk.GiderKdvOrani20 + pk.GiderKdvOrani10,
            KdvTutar = pk.GiderKdv20Tutari + pk.GiderKdv10Tutari,
            ToplamTutar = fk.ToplamGider,
            KalemTipi = FaturaKalemTipi.Servis,
            CreatedAt = DateTime.UtcNow
        });
        await db2.SaveChangesAsync(ct);

        fk.GiderFaturaId = fatura.Id;
        fk.Durum = fk.GelirFaturaId != null ? PuantajFinansalDurum.TumFaturalarUretildi : PuantajFinansalDurum.GiderFaturasiUretildi;
        fk.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return fatura;
    }

    // ═══════════════════════════════════════════════════════════════
    // Operasyonel Hakedis → Tam Finans Zinciri
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Yeni operasyonel hakediş modelinden finans zinciri üretir.
    /// Kurum => Giden fatura (gelir), Tedarikçi => Gelen fatura (gider), Araç => finans dışı.
    /// </summary>
    public async Task<PuantajFinansSonuc> IsleAsync(Hakedis hakedis)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var kayit = await db.Hakedisler
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == hakedis.Id && !x.IsDeleted);

        if (kayit == null)
            return new PuantajFinansSonuc { Mesaj = "Hakediş kaydı bulunamadı." };

        if (kayit.FaturaId != null)
            return new PuantajFinansSonuc { Mesaj = "Bu hakediş zaten işlenmiş." };

        if (kayit.Tip == HakedisTipi.Arac)
            return new PuantajFinansSonuc { Mesaj = "Araç tipi hakedişler faturalanmaz (iç raporlama kaydı)." };

        var cari = await ResolveCariForHakedisAsync(db, kayit);
        if (cari == null)
            return new PuantajFinansSonuc { Mesaj = "Hakediş için cari eşleşmesi bulunamadı." };

        var firmaId = kayit.FirmaId ?? 0;
        var araToplam = kayit.Tutar > 0 ? kayit.Tutar : kayit.GenelToplam;
        if (araToplam <= 0)
            return new PuantajFinansSonuc { Mesaj = "Hakediş tutarı sıfır veya negatif olduğu için işlenemedi." };

        var kdvOrani = (int)(kayit.KdvOran > 0 ? kayit.KdvOran : 20m);
        var yon = kayit.Tip == HakedisTipi.Kurum ? FaturaYonu.Giden : FaturaYonu.Gelen;
        var fatura = await FaturaOlusturHizliAsync(
            cari,
            araToplam,
            yon,
            kdvOrani,
            firmaId,
            $"Hakediş {kayit.Tip}: {kayit.Yil}-{kayit.Ay:D2} / #{kayit.Id}");

        await db.Hakedisler
            .Where(x => x.Id == kayit.Id)
            .UpdateTrackedAsync(db, record =>
            {
                record.FaturaId = fatura.Id;
                record.Durum = HakedisDurum.Faturalandi;
                record.UpdatedAt = DateTime.UtcNow;
            }, requireSingleRecord: true);

        try
        {
            await SnapshotHakedisGuncelleAsync(kayit, fatura.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hakediş faturalandı ancak snapshot güncellenemedi. Hakediş: {Id}", kayit.Id);
            throw new InvalidOperationException("Hakediş faturası oluşturuldu ancak snapshot güncellemesi tamamlanamadı; yeniden fatura üretmeden mevcut kaydı kontrol edin.", ex);
        }

        var gelir = kayit.Tip == HakedisTipi.Kurum ? araToplam : 0m;
        var gider = kayit.Tip == HakedisTipi.Tedarikci ? araToplam : 0m;

        return new PuantajFinansSonuc
        {
            Basarili = true,
            GelirTutar = gelir,
            GiderTutar = gider,
            Kar = gelir - gider,
            GelirFaturaId = yon == FaturaYonu.Giden ? fatura.Id : null,
            GiderFaturaId = yon == FaturaYonu.Gelen ? fatura.Id : null
        };
    }

    private static async Task<Cari?> ResolveCariForHakedisAsync(ApplicationDbContext db, Hakedis hakedis)
    {
        if (hakedis.Tip == HakedisTipi.Kurum)
        {
            return await db.Cariler.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == hakedis.ReferansId && !c.IsDeleted);
        }

        if (hakedis.Tip == HakedisTipi.Tedarikci)
        {
            var tedarikci = await db.TasimaTedarikciler.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == hakedis.ReferansId && !t.IsDeleted);

            if (tedarikci?.CariId is int cariId && cariId > 0)
            {
                var cariById = await db.Cariler.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == cariId && !c.IsDeleted);
                if (cariById != null) return cariById;
            }

            if (!string.IsNullOrWhiteSpace(tedarikci?.Unvan))
            {
                return await db.Cariler.AsNoTracking()
                    .FirstOrDefaultAsync(c => !c.IsDeleted
                        && c.CariTipi == CariTipi.Tedarikci
                        && c.Unvan == tedarikci.Unvan);
            }
        }

        return null;
    }


    private async Task SnapshotHakedisGuncelleAsync(Hakedis hakedis, int? faturaId = null)
    {
        if (hakedis.FirmaId is not > 0)
            throw new InvalidOperationException("Hakediş snapshot işlemi için geçerli firma gerekir.");
        await using var strategyDb = await _dbFactory.CreateDbContextAsync();
        var strategy = strategyDb.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var firmaId = hakedis.FirmaId ?? 0;

            var islemId = DeterministicGuid(hakedis.Id, "HakedisFinansV2");
            var exists = await db.SnapshotTransactions
                .AnyAsync(t => t.IslemId == islemId && !t.IsDeleted);
            if (exists) return;

            var hakedisTutar = hakedis.GenelToplam > 0 ? hakedis.GenelToplam : hakedis.Tutar;
            var deltaGelir = hakedis.Tip == HakedisTipi.Kurum ? hakedisTutar : 0m;
            var deltaGider = hakedis.Tip == HakedisTipi.Tedarikci ? hakedisTutar : 0m;

            db.SnapshotTransactions.Add(new SnapshotTransaction
            {
                FirmaId = firmaId,
                IslemId = islemId,
                Yil = hakedis.Yil,
                Ay = hakedis.Ay,
                IslemTipi = "HakedisFinansV2",
                GelirDelta = deltaGelir,
                GiderDelta = deltaGider,
                FaturaId = faturaId,
                Aciklama = $"Hakedis #{hakedis.Id} finans işlemi",
                CreatedAt = DateTime.UtcNow
            });

            var snapshots = await db.MaasOdemeSnapshotlar.AsTracking()
                .Where(s => s.FirmaId == firmaId && s.Yil == hakedis.Yil && s.Ay == hakedis.Ay && !s.IsDeleted)
                .ToListAsync();
            foreach (var snapshot in snapshots)
            {
                if (!snapshot.Kilitli)
                {
                    snapshot.HakedisGelir += deltaGelir;
                    snapshot.HakedisGider += deltaGider;
                    snapshot.UpdatedAt = DateTime.UtcNow;
                }
                if (snapshot.HakedisGelir < 0 || snapshot.HakedisGider < 0)
                {
                    _logger.LogCritical("Snapshot negatif tutarı düzeltiliyor. Snapshot={Id}", snapshot.Id);
                    snapshot.HakedisGelir = Math.Max(snapshot.HakedisGelir, 0m);
                    snapshot.HakedisGider = Math.Max(snapshot.HakedisGider, 0m);
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    private static Guid DeterministicGuid(int seed, string scope)
    {
        // Aynı seed + scope → hep aynı Guid (idempotency için)
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes($"{scope}:{seed}");
        var hash = md5.ComputeHash(bytes);
        return new Guid(hash);
    }

    private async Task<Fatura> FaturaOlusturHizliAsync(Cari cari, decimal tutar, FaturaYonu yon, int kdvOrani, int firmaId, string aciklama)
    {
        var kdvTutar = tutar * kdvOrani / 100;
        var fatura = new Fatura
        {
            FaturaTarihi = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc),
            FaturaYonu = yon,
            FaturaTipi = yon == FaturaYonu.Giden ? FaturaTipi.SatisFaturasi : FaturaTipi.AlisFaturasi,
            CariId = cari.Id, FirmaId = firmaId,
            AraToplam = tutar, KdvOrani = kdvOrani, KdvTutar = kdvTutar, GenelToplam = tutar + kdvTutar,
            Aciklama = aciklama, Durum = FaturaDurum.Odendi, ImportKaynak = "Puantaj", CreatedAt = DateTime.UtcNow
        };
        fatura.FaturaKalemleri.Add(new FaturaKalem
        {
            SiraNo = 1, Aciklama = aciklama, Miktar = 1, Birim = "Adet",
            BirimFiyat = tutar, KdvOrani = kdvOrani, KdvTutar = kdvTutar, ToplamTutar = tutar + kdvTutar, CreatedAt = DateTime.UtcNow
        });
        return await _faturaService.CreateAsync(fatura); // → otomatik muhasebe fişi
    }

    public async Task<int> TopluFaturaUretAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        var kayitlar = await FinansalKayitlariGetirAsync(hesapDonemiId, ct);
        int uretilen = 0;

        foreach (var fk in kayitlar.Where(f => f.Durum == PuantajFinansalDurum.Bekliyor))
        {
            try
            {
                if (fk.GelirCariId != null && fk.GelirFaturaId == null)
                    await GelirFaturasiUretAsync(fk.Id, ct);
                if (fk.GiderCariId != null && fk.GiderFaturaId == null)
                    await GiderFaturasiUretAsync(fk.Id, ct);
                uretilen++;
            }
            catch (Exception ex)
            {
                // Fatura üretimi kaydı tutarsız bırakmaması için tekil hatayı kaydet, diğer kayıtları atlamaya devam et.
                _logger.LogError(ex, "Puantaj finans faturası üretilemedi. PuantajKayitId: {PuanKayitId}, GelirCariId: {GelirCariId}, GiderCariId: {GiderCariId}",
                    fk.Id, fk.GelirCariId, fk.GiderCariId);
            }
        }

        return uretilen;
    }
}


