using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public sealed class PuantajFinansService : IPuantajFinansService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IFaturaService _faturaService;
    private readonly IMuhasebeService _muhasebeService;
    private readonly ILogger<PuantajFinansService> _logger;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public PuantajFinansService(IDbContextFactory<ApplicationDbContext> dbFactory, IFaturaService faturaService, IMuhasebeService muhasebeService, ILogger<PuantajFinansService> logger, IAktifFirmaProvider aktifFirmaProvider)
    {
        _dbFactory = dbFactory;
        _faturaService = faturaService;
        _muhasebeService = muhasebeService;
        _logger = logger;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    private int RequireSelectedFirma()
    {
        if (_aktifFirmaProvider.TumFirmalar || _aktifFirmaProvider.AktifFirmaId is not > 0)
            throw new UnauthorizedAccessException("Puantaj finans işlemi için tek bir firma seçilmelidir.");
        return _aktifFirmaProvider.AktifFirmaId.Value;
    }

    public async Task<bool> FinansalKayitOlusturulabilirMiAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        var firmaId = RequireSelectedFirma();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.PuantajHesapDonemleri.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == hesapDonemiId && x.FirmaId == firmaId && !x.IsDeleted, ct);
        return h is { Durum: PuantajHesapDurum.Aktif, OnayDurum: PuantajDonemOnayDurum.Kilitli };
    }

    public async Task FinansalKayitOlusturAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        var firmaId = RequireSelectedFirma();
        await using var strategyDb = await _dbFactory.CreateDbContextAsync();
        var strategy = strategyDb.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var donem = await db.PuantajHesapDonemleri.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == hesapDonemiId && x.FirmaId == firmaId && !x.IsDeleted, ct);
            if (donem is not { Durum: PuantajHesapDurum.Aktif, OnayDurum: PuantajDonemOnayDurum.Kilitli })
                throw new InvalidOperationException("Seçili firmada finansal kayıt için kilitli ve aktif hesap dönemi bulunamadı.");

            var pkList = await db.PuantajKayitlar
                .Where(p => p.HesapDonemiId == hesapDonemiId && !p.IsDeleted &&
                    !db.PuantajFinansalKayitlar.IgnoreQueryFilters()
                        .Any(f => f.PuantajKayitId == p.Id && f.HesapDonemiId == hesapDonemiId))
                .ToListAsync(ct);

            var simdi = DateTime.UtcNow;
            foreach (var pk in pkList)
            {
                db.PuantajFinansalKayitlar.Add(new PuantajFinansalKayit
                {
                    FirmaId = firmaId,
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
            await transaction.CommitAsync(ct);
        });
    }

    public async Task<List<PuantajFinansalKayit>> FinansalKayitlariGetirAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        var firmaId = RequireSelectedFirma();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PuantajFinansalKayitlar
            .Include(f => f.PuantajKayit).ThenInclude(p => p!.Guzergah)
            .Include(f => f.PuantajKayit).ThenInclude(p => p!.Arac)
            .Where(f => f.FirmaId == firmaId && f.HesapDonemiId == hesapDonemiId && !f.IsDeleted)
            .OrderBy(f => f.PuantajKayit!.Guzergah!.GuzergahAdi)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<bool> FaturaUretilebilirMiAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        var firmaId = RequireSelectedFirma();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var kayitVar = await db.PuantajFinansalKayitlar
            .AnyAsync(f => f.FirmaId == firmaId && f.HesapDonemiId == hesapDonemiId && !f.IsDeleted, ct);
        return kayitVar;
    }

    public async Task<Fatura> GelirFaturasiUretAsync(int finansalKayitId, CancellationToken ct = default)
        => await FaturaUretAsync(finansalKayitId, gelir: true, ct);

    public async Task<Fatura> GiderFaturasiUretAsync(int finansalKayitId, CancellationToken ct = default)
        => await FaturaUretAsync(finansalKayitId, gelir: false, ct);

    private async Task<Fatura> FaturaUretAsync(int finansalKayitId, bool gelir, CancellationToken ct)
    {
        var firmaId = RequireSelectedFirma();
        await using var strategyDb = await _dbFactory.CreateDbContextAsync();
        var strategy = strategyDb.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var fk = await db.PuantajFinansalKayitlar.AsTracking()
                .Include(f => f.PuantajKayit).ThenInclude(p => p!.Guzergah)
                .Include(f => f.PuantajKayit).ThenInclude(p => p!.Arac)
                .FirstOrDefaultAsync(f => f.Id == finansalKayitId && f.FirmaId == firmaId && !f.IsDeleted, ct)
                ?? throw new InvalidOperationException("Finansal kayıt bulunamadı.");
            var cariId = gelir ? fk.GelirCariId : fk.GiderCariId;
            if (cariId is not > 0) throw new InvalidOperationException(gelir ? "Gelir CariId tanımlı değil." : "Gider CariId tanımlı değil.");
            if ((gelir ? fk.GelirFaturaId : fk.GiderFaturaId) != null)
                throw new InvalidOperationException(gelir ? "Bu kayıt için gelir faturası zaten üretilmiş." : "Bu kayıt için gider faturası zaten üretilmiş.");

            var pk = fk.PuantajKayit ?? throw new InvalidOperationException("Puantaj kaydı bulunamadı.");
            var yon = gelir ? FaturaYonu.Giden : FaturaYonu.Gelen;
            var tip = gelir ? FaturaTipi.SatisFaturasi : FaturaTipi.AlisFaturasi;
            var kdvOrani = gelir ? pk.GelirKdvOrani : pk.GiderKdvOrani20 + pk.GiderKdvOrani10;
            var kdvTutar = gelir ? fk.KdvTutar : pk.GiderKdv20Tutari + pk.GiderKdv10Tutari;
            var tutar = gelir ? fk.ToplamGelir : fk.ToplamGider;
            var aciklama = gelir
                ? $"{pk.Yil}/{pk.Ay:D2} {pk.GuzergahAdi} / {pk.Arac?.AktifPlaka ?? pk.Arac?.Plaka} Puantaj Gelir Faturası"
                : $"{pk.Yil}/{pk.Ay:D2} {pk.GuzergahAdi} / {pk.Arac?.AktifPlaka ?? pk.Arac?.Plaka} Puantaj Gider Faturası";
            var fatura = new Fatura
            {
                FaturaNo = await _faturaService.GenerateNextFaturaNoAsync(tip, yon, firmaId),
                FaturaTarihi = MKFiloServis.Shared.Time.BusinessTime.Today, FaturaTipi = tip, FaturaYonu = yon,
                Durum = FaturaDurum.Beklemede, EFaturaTipi = EFaturaTipi.EArsiv,
                CariId = cariId.Value, FirmaId = firmaId, AracId = pk.AracId,
                AraToplam = tutar, KdvOrani = kdvOrani, KdvTutar = kdvTutar,
                GenelToplam = gelir ? fk.GenelToplam : tutar + kdvTutar,
                ImportKaynak = "Puantaj", Aciklama = aciklama, CreatedAt = DateTime.UtcNow
            };
            fatura = await _faturaService.CreateAsync(db, fatura, ct, createAutomaticAccounting: false);
            db.FaturaKalemleri.Add(new FaturaKalem
            {
                FaturaId = fatura.Id,
                SiraNo = 1,
                Aciklama = gelir ? $"{pk.GuzergahAdi} / {pk.Arac?.AktifPlaka ?? pk.Arac?.Plaka} / {pk.Slot}" : $"Tedarikçi Ödemesi: {pk.GuzergahAdi} / {pk.Arac?.AktifPlaka ?? pk.Arac?.Plaka}",
                Miktar = fk.SeferGunu, Birim = "Sefer Günü",
                BirimFiyat = gelir ? fk.BirimGelir : fk.BirimGider,
                KdvOrani = kdvOrani, KdvTutar = kdvTutar,
                ToplamTutar = gelir ? fk.GenelToplam : tutar,
                KalemTipi = FaturaKalemTipi.Servis, CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
            var otomatikFisAcik = await db.MuhasebeAyarlari
                .AnyAsync(a => a.FaturaOtomatikMuhasebeFisi, ct);
            if (otomatikFisAcik)
                await _muhasebeService.CreateFaturaFisiAsync(db, fatura);
            if (gelir) fk.GelirFaturaId = fatura.Id; else fk.GiderFaturaId = fatura.Id;
            fk.Durum = fk.GelirFaturaId != null && fk.GiderFaturaId != null
                ? PuantajFinansalDurum.TumFaturalarUretildi
                : gelir ? PuantajFinansalDurum.GelirFaturasiUretildi : PuantajFinansalDurum.GiderFaturasiUretildi;
            fk.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return fatura;
        });
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
        var firmaId = RequireSelectedFirma();
        await using var strategyDb = await _dbFactory.CreateDbContextAsync();
        return await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var kayit = await db.Hakedisler.AsTracking()
                .FirstOrDefaultAsync(x => x.Id == hakedis.Id && x.FirmaId == firmaId && !x.IsDeleted);
            if (kayit == null) return new PuantajFinansSonuc { Mesaj = "Hakediş kaydı bulunamadı." };
            if (kayit.FaturaId != null) return new PuantajFinansSonuc { Mesaj = "Bu hakediş zaten işlenmiş." };
            if (kayit.Tip == HakedisTipi.Arac) return new PuantajFinansSonuc { Mesaj = "Araç tipi hakedişler faturalanmaz (iç raporlama kaydı)." };

            var cari = await ResolveCariForHakedisAsync(db, kayit, firmaId);
            if (cari == null) return new PuantajFinansSonuc { Mesaj = "Hakediş için cari eşleşmesi bulunamadı." };
            var araToplam = kayit.Tutar > 0 ? kayit.Tutar : kayit.GenelToplam;
            if (araToplam <= 0) return new PuantajFinansSonuc { Mesaj = "Hakediş tutarı sıfır veya negatif olduğu için işlenemedi." };

            var kdvOrani = (int)(kayit.KdvOran > 0 ? kayit.KdvOran : 20m);
            var yon = kayit.Tip == HakedisTipi.Kurum ? FaturaYonu.Giden : FaturaYonu.Gelen;
            var fatura = await FaturaOlusturHizliAsync(db, cari, araToplam, yon, kdvOrani, firmaId,
                $"Hakediş {kayit.Tip}: {kayit.Yil}-{kayit.Ay:D2} / #{kayit.Id}");
            kayit.FaturaId = fatura.Id;
            kayit.Durum = HakedisDurum.Faturalandi;
            kayit.UpdatedAt = DateTime.UtcNow;
            await SnapshotHakedisGuncelleAsync(db, kayit, fatura.Id);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            var gelir = kayit.Tip == HakedisTipi.Kurum ? araToplam : 0m;
            var gider = kayit.Tip == HakedisTipi.Tedarikci ? araToplam : 0m;
            return new PuantajFinansSonuc
            {
                Basarili = true, GelirTutar = gelir, GiderTutar = gider, Kar = gelir - gider,
                GelirFaturaId = yon == FaturaYonu.Giden ? fatura.Id : null,
                GiderFaturaId = yon == FaturaYonu.Gelen ? fatura.Id : null
            };
        });
    }

    private static async Task<Cari?> ResolveCariForHakedisAsync(ApplicationDbContext db, Hakedis hakedis, int firmaId)
    {
        if (hakedis.Tip == HakedisTipi.Kurum)
        {
            return await db.Cariler.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == hakedis.ReferansId && c.FirmaId == firmaId && !c.IsDeleted);
        }

        if (hakedis.Tip == HakedisTipi.Tedarikci)
        {
            var tedarikci = await db.TasimaTedarikciler.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == hakedis.ReferansId && !t.IsDeleted);

            if (tedarikci?.CariId is int cariId && cariId > 0)
            {
                var cariById = await db.Cariler.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == cariId && c.FirmaId == firmaId && !c.IsDeleted);
                if (cariById != null) return cariById;
            }

            if (!string.IsNullOrWhiteSpace(tedarikci?.Unvan))
            {
                return await db.Cariler.AsNoTracking()
                    .FirstOrDefaultAsync(c => !c.IsDeleted
                        && c.FirmaId == firmaId
                        && c.CariTipi == CariTipi.Tedarikci
                        && c.Unvan == tedarikci.Unvan);
            }
        }

        return null;
    }


    private async Task SnapshotHakedisGuncelleAsync(ApplicationDbContext db, Hakedis hakedis, int? faturaId = null)
    {
        if (hakedis.FirmaId is not > 0)
            throw new InvalidOperationException("Hakediş snapshot işlemi için geçerli firma gerekir.");
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

    }

    private static Guid DeterministicGuid(int seed, string scope)
    {
        // Aynı seed + scope → hep aynı Guid (idempotency için)
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes($"{scope}:{seed}");
        var hash = md5.ComputeHash(bytes);
        return new Guid(hash);
    }

    private async Task<Fatura> FaturaOlusturHizliAsync(ApplicationDbContext db, Cari cari, decimal tutar, FaturaYonu yon, int kdvOrani, int firmaId, string aciklama)
    {
        var kdvTutar = tutar * kdvOrani / 100;
        var fatura = new Fatura
        {
            FaturaTarihi = DateTime.SpecifyKind(MKFiloServis.Shared.Time.BusinessTime.Today, DateTimeKind.Utc),
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
        fatura.FaturaNo = await _faturaService.GenerateNextFaturaNoAsync(fatura.FaturaTipi, yon, firmaId);
        return await _faturaService.CreateAsync(db, fatura); // Fatura, kalem ve muhasebe aynı üst transaction'da.
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


