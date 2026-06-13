using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

public sealed class KurumPuantajService : IKurumPuantajService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IPuantajSyncService _syncService;
    private readonly ILogger<KurumPuantajService> _logger;

    public KurumPuantajService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IPuantajSyncService syncService,
        ILogger<KurumPuantajService> logger)
    {
        _dbFactory = dbFactory;
        _syncService = syncService;
        _logger = logger;
    }

    // ── Kurum / Güzergah / Araç ───────────────────────────────────────────────

    public async Task<List<Kurum>> GetAktifKurumlarAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Kurumlar
            .Where(k => !k.IsDeleted && k.Aktif)
            .OrderBy(k => k.KurumAdi)
            .ToListAsync();
    }

    public async Task<List<Guzergah>> GetKurumGuzergahlariAsync(int kurumId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Guzergahlar
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .Where(g => !g.IsDeleted && g.Aktif && g.KurumId == kurumId)
            .OrderBy(g => g.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<List<AracGuzergahSatiri>> GetGuzergahAraclariAsync(int guzergahId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // FiloGuzergahEslestirme tablosundan atanmış araçlar
        var eslestirmeler = await db.FiloGuzergahEslestirmeleri
            .Include(e => e.Arac)
            .Include(e => e.Sofor)
            .Where(e => e.GuzergahId == guzergahId && e.IsActive)
            .ToListAsync();

        var sonuc = eslestirmeler.Select(e => new AracGuzergahSatiri
        {
            AracId                = e.AracId,
            Plaka                 = e.Arac?.AktifPlaka ?? e.Arac?.Plaka ?? "-",
            SoforId               = e.SoforId,
            SoforAdi              = e.Sofor != null ? $"{e.Sofor.Ad} {e.Sofor.Soyad}" : null,
            KurumaKesilecekUcret  = e.KurumaKesilecekUcret,
            TaseronaOdenenUcret   = e.TaseronaOdenenUcret,
            EslestirmeId          = e.Id
        }).ToList();

        // Eşleştirmede yoksa güzergahın varsayılan aracını ekle
        if (!sonuc.Any())
        {
            var guzergah = await db.Guzergahlar
                .Include(g => g.VarsayilanArac)
                .Include(g => g.VarsayilanSofor)
                .FirstOrDefaultAsync(g => g.Id == guzergahId && !g.IsDeleted);

            if (guzergah?.VarsayilanArac != null)
            {
                sonuc.Add(new AracGuzergahSatiri
                {
                    AracId               = guzergah.VarsayilanArac.Id,
                    Plaka                = guzergah.VarsayilanArac.AktifPlaka ?? guzergah.VarsayilanArac.Plaka,
                    SoforId              = guzergah.VarsayilanSoforId,
                    SoforAdi             = guzergah.VarsayilanSofor != null
                                               ? $"{guzergah.VarsayilanSofor.Ad} {guzergah.VarsayilanSofor.Soyad}"
                                               : null,
                    KurumaKesilecekUcret = guzergah.BirimFiyat,
                    TaseronaOdenenUcret  = 0,
                    EslestirmeId         = null
                });
            }
        }

        return sonuc.OrderBy(s => s.Plaka).ToList();
    }

    // ── PuantajKayit CRUD ─────────────────────────────────────────────────────

    public async Task<List<PuantajKayit>> GetPuantajlarAsync(int yil, int ay, int? kurumId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // KurumId → Güzergahların KurumId alanından filtrele (null = tum kurumlar)
        var guzergahQuery = db.Guzergahlar.Where(g => !g.IsDeleted);
        if (kurumId.HasValue && kurumId.Value > 0)
            guzergahQuery = guzergahQuery.Where(g => g.KurumId == kurumId.Value);
        var guzergahIds = await guzergahQuery.Select(g => g.Id).ToListAsync();

        var kayitlar = await db.PuantajKayitlar
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .Where(p => !p.IsDeleted
                        && p.Yil == yil
                        && p.Ay == ay
                        && p.GuzergahId != null
                        && guzergahIds.Contains(p.GuzergahId!.Value))
            .OrderBy(p => p.Guzergah!.GuzergahAdi)
            .ThenBy(p => p.Plaka)
            .ToListAsync();

        // ── SeferSayisi hesaplama ─────────────────────────────────────────────
        // Öncelik 1: OperasyonKayitlari'ndan gerçek sefer sayısını hesapla.
        // Formül: SUM((Yon == SabahAksam ? 2 : 1) * PuantajCarpani)
        //   Sabah=1, Akşam=1, SabahAksam=2
        // Gruplama Slot içermez — merge edilmiş SabahAksam satırları hem Sabah hem
        // Akşam OperasyonKaydi'larını tek PuantajKayit altında toplar.
        var ayBaslangic = new DateTime(yil, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1);
        var opAgg = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted
                        && o.Tarih >= ayBaslangic && o.Tarih < ayBitis
                        && o.OperasyonDurumu == OperasyonDurumu.Gitti
                        && guzergahIds.Contains(o.GuzergahId))
            .GroupBy(o => new { o.GuzergahId, o.AracId })
            .Select(g => new { g.Key.GuzergahId, g.Key.AracId,
                Toplam = (int)g.Sum(o => (o.Yon == PuantajYon.SabahAksam ? 2 : 1) * o.PuantajCarpani) })
            .ToListAsync();

        var aggMap = opAgg.ToDictionary(
            x => (GuzergahId: x.GuzergahId, AracId: x.AracId),
            x => x.Toplam);

        foreach (var k in kayitlar)
        {
            if (k.GuzergahId == null) continue;

            // AracId null veya 0 ise OperasyonKaydi eşleşmesi yapılamaz
            var aracId = k.AracId.GetValueOrDefault();
            if (aracId > 0 && aggMap.TryGetValue((k.GuzergahId.Value, aracId), out var opToplam))
            {
                k.SeferSayisi = opToplam;
            }
            else
            {
                // Öncelik 2: Gun01-Gun31 toplamından hesapla (OperasyonKaydi yoksa)
                if (k.SeferGunuToplami > 0)
                    k.SeferSayisi = k.SeferGunuToplami;
                // else: mevcut DB değeri korunur
            }
        }

        // ── Mükerrer satır temizliği ─────────────────────────────────────────
        // Aynı {GuzergahId, AracId} için birden fazla satır varsa
        // (merge sonrası yetim kalmış Aksam slot'ları), sadece en yüksek
        // öncelikli Yon'a sahip olanı tut, diğerlerini listeden çıkar.
        // Öncelik: SabahAksam > Sabah > Aksam > Diğer
        static int YonOncelik(PuantajYon y) => y switch
        {
            PuantajYon.SabahAksam => 3,
            PuantajYon.Sabah => 2,
            PuantajYon.Aksam => 1,
            _ => 0
        };

        var dupGroups = kayitlar
            .Where(k => k.GuzergahId != null && k.AracId.HasValue && k.AracId > 0)
            .GroupBy(k => (GuzergahId: k.GuzergahId!.Value, AracId: k.AracId!.Value))
            .Where(g => g.Count() > 1)
            .ToList();

        if (dupGroups.Any())
        {
            var toRemove = new List<PuantajKayit>();
            foreach (var g in dupGroups)
            {
                // En yüksek öncelikli Yon'u tut, diğerlerini çıkar
                var best = g.OrderByDescending(k => YonOncelik(k.Yon))
                            .ThenByDescending(k => k.SeferSayisi)
                            .First();
                foreach (var k in g)
                    if (k != best)
                        toRemove.Add(k);
            }
            foreach (var r in toRemove)
                kayitlar.Remove(r);
        }

        return kayitlar;
    }

    public async Task<PuantajKayit?> GetPuantajByIdAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PuantajKayitlar
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<PuantajKayit> SavePuantajAsync(PuantajKayit kayit)
    {
        // Finansal alanlari gun bazli puantajdan hesapla
        kayit.HesaplaPuantajToplam();
        kayit.HesaplaGelir();
        kayit.HesaplaGider();

        // FK hatasi olmasin diye 0 degerleri null yap
        if (kayit.IsverenFirmaId <= 0) kayit.IsverenFirmaId = null;

        // DB NOT NULL enum/text kolonları için varsayılan değerleri garantile
        EnsurePuantajDefaults(kayit);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            // Upsert: Güzergah + Araç + Yıl + Ay + Slot kombinasyonu unique
            var mevcut = await db.PuantajKayitlar
                .FirstOrDefaultAsync(p =>
                    !p.IsDeleted &&
                    p.GuzergahId == kayit.GuzergahId &&
                    p.AracId     == kayit.AracId &&
                    p.Yil        == kayit.Yil &&
                    p.Ay         == kayit.Ay &&
                    p.Slot       == kayit.Slot);

            if (mevcut == null)
            {
                // Savunma: detached navigation'lar Add() ile tüm graph'ı Added yapmasın.
                // FK alanları (GuzergahId, AracId, SoforId, ...) zaten set edilmiş durumda.
                kayit.Guzergah = null;
                kayit.Arac = null;
                kayit.Sofor = null;
                kayit.KurumCari = null;
                kayit.OdemeYapilacakCari = null;
                kayit.FaturaKesiciCari = null;
                kayit.Kurum = null;
                kayit.IsverenFirma = null;
                kayit.HesapDonemi = null;

                // Clone'dan taşınan mevcut Id ile PK çakışmasını önle (insert'te DB identity üretsin)
                kayit.Id = 0;

                db.PuantajKayitlar.Add(kayit);
            }
            else
            {
                // Gün değerlerini kopyala
                for (int g = 1; g <= 31; g++)
                    mevcut.SetGunDeger(g, kayit.GetGunDeger(g));

                mevcut.SoforId      = kayit.SoforId;
                mevcut.SoforAdi     = kayit.SoforAdi;
                mevcut.Plaka        = kayit.Plaka;
                mevcut.Slot         = kayit.Slot;
                mevcut.SlotAdi      = kayit.SlotAdi;
                mevcut.Yon          = kayit.Yon;
                mevcut.Gun          = kayit.Gun;
                mevcut.SeferSayisi  = kayit.SeferSayisi;
                mevcut.BirimGelir   = kayit.BirimGelir;
                mevcut.BirimGider   = kayit.BirimGider;
                mevcut.ToplamGelir  = kayit.ToplamGelir;
                mevcut.ToplamGider  = kayit.ToplamGider;
                mevcut.Alinacak     = kayit.Alinacak;
                mevcut.Odenecek     = kayit.Odenecek;
                mevcut.GelirKdvTutari = kayit.GelirKdvTutari;
                mevcut.GelirToplam  = kayit.GelirToplam;
                mevcut.KaynakTipi   = kayit.KaynakTipi;
                mevcut.FinansYonu   = kayit.FinansYonu;
                mevcut.KurumId      = kayit.KurumId;
                mevcut.IsverenFirmaId = kayit.IsverenFirmaId;
                mevcut.BelgeNo      = kayit.BelgeNo;
                mevcut.TransferDurum = kayit.TransferDurum;
                mevcut.UpdatedAt    = DateTime.UtcNow;
            }

            ValidateRequiredFields(db);
            await db.SaveChangesAsync();

            // ── OperasyonKaydi sync (aynı transaction içinde) ────────────────
            var saved = mevcut ?? kayit;
            await ((PuantajSyncService)_syncService).SyncFromPuantajWithContextAsync(db, saved, PuantajSyncMode.CreateUpdate);

            await tx.CommitAsync();
            return saved;
        });
    }

    public async Task TopluSavePuantajAsync(IEnumerable<PuantajKayit> kayitlar)
    {
        var kayitList = kayitlar.ToList();
        if (!kayitList.Any()) return;

        // Zorunlu alan validasyonu
        var hatali = kayitList.FirstOrDefault(k => k.GuzergahId == null || k.GuzergahId <= 0);
        if (hatali != null)
            throw new InvalidOperationException($"Kayıt yapılamadı: '{hatali.Plaka}' için GüzergahId boş.");
        hatali = kayitList.FirstOrDefault(k => k.AracId == null || k.AracId <= 0);
        if (hatali != null)
            throw new InvalidOperationException($"Kayıt yapılamadı: GüzergahId={hatali.GuzergahId} için AraçId boş.");

        var yil = kayitList[0].Yil;
        var ay = kayitList[0].Ay;

        // DB NOT NULL kolonları için default değerleri garantile
        foreach (var kayit in kayitList)
            EnsurePuantajDefaults(kayit);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                // Ayni donemdeki tum mevcut kayitlari TEK sorguda yukle
                var mevcutKayitlar = await db.PuantajKayitlar
                    .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay)
                    .ToListAsync();

                foreach (var kayit in kayitList)
                {
                    // Finansal alanlari gun bazli puantajdan hesapla
                    kayit.HesaplaPuantajToplam();
                    kayit.HesaplaGelir();
                    kayit.HesaplaGider();

                    // FK hatasi olmasin diye 0 degerleri null yap
                    if (kayit.IsverenFirmaId <= 0) kayit.IsverenFirmaId = null;

                    var mevcut = mevcutKayitlar.FirstOrDefault(m =>
                        m.GuzergahId == kayit.GuzergahId &&
                        m.AracId     == kayit.AracId &&
                        m.Slot       == kayit.Slot);

                    if (mevcut == null)
                    {
                        // Savunma: detached navigation'lar Add() ile tüm graph'ı Added yapmasın.
                        kayit.Guzergah = null;
                        kayit.Arac = null;
                        kayit.Sofor = null;
                        kayit.KurumCari = null;
                        kayit.OdemeYapilacakCari = null;
                        kayit.FaturaKesiciCari = null;
                        kayit.Kurum = null;
                        kayit.IsverenFirma = null;
                        kayit.HesapDonemi = null;

                        // Clone'dan taşınan mevcut Id ile PK çakışmasını önle (insert'te DB identity üretsin)
                        kayit.Id = 0;

                        db.PuantajKayitlar.Add(kayit);
                    }
                    else
                    {
                        for (int g = 1; g <= 31; g++)
                            mevcut.SetGunDeger(g, kayit.GetGunDeger(g));

                        mevcut.SoforId      = kayit.SoforId;
                        mevcut.SoforAdi     = kayit.SoforAdi;
                        mevcut.Plaka        = kayit.Plaka;
                        mevcut.Slot         = kayit.Slot;
                        mevcut.SlotAdi      = kayit.SlotAdi;
                        mevcut.Yon          = kayit.Yon;
                        mevcut.Gun          = kayit.Gun;
                        mevcut.SeferSayisi  = kayit.SeferSayisi;
                        mevcut.BirimGelir   = kayit.BirimGelir;
                        mevcut.BirimGider   = kayit.BirimGider;
                        mevcut.ToplamGelir  = kayit.ToplamGelir;
                        mevcut.ToplamGider  = kayit.ToplamGider;
                        mevcut.Alinacak     = kayit.Alinacak;
                        mevcut.Odenecek     = kayit.Odenecek;
                        mevcut.GelirKdvTutari = kayit.GelirKdvTutari;
                        mevcut.GelirToplam  = kayit.GelirToplam;
                        mevcut.KaynakTipi   = kayit.KaynakTipi;
                        mevcut.FinansYonu   = kayit.FinansYonu;
                        mevcut.KurumId      = kayit.KurumId;
                        mevcut.IsverenFirmaId = kayit.IsverenFirmaId;
                        mevcut.BelgeNo      = kayit.BelgeNo;
                        mevcut.TransferDurum = kayit.TransferDurum;
                        mevcut.UpdatedAt    = DateTime.UtcNow;
                    }
                }

                // Merge edilmiş SabahAksam satırları için eski Aksam slot'lu yetimleri temizle
                var mergedKeys = kayitList
                    .Where(k => k.Yon == PuantajYon.SabahAksam && k.Slot == SeferSlot.Sabah)
                    .Select(k => (GuzergahId: k.GuzergahId, AracId: k.AracId))
                    .ToHashSet();

                foreach (var mevcut in mevcutKayitlar)
                {
                    if (mevcut.Slot == SeferSlot.Aksam
                        && mevcut.Yon != PuantajYon.SabahAksam
                        && mergedKeys.Contains((mevcut.GuzergahId, mevcut.AracId))
                        && !kayitList.Any(k => k.Id == mevcut.Id))
                    {
                        mevcut.IsDeleted = true;
                        mevcut.UpdatedAt = DateTime.UtcNow;
                    }
                }

                ValidateRequiredFields(db);
            await db.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        // ── Toplu OperasyonKaydi sync (transaction dışında, hata durumunda save'i geri almaz) ──
        try
        {
            await _syncService.SyncFromPuantajTopluAsync(yil, ay, kayitList, PuantajSyncMode.CreateUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Toplu Puantaj->Operasyon sync hatasi: {Count} kayit", kayitList.Count);
        }
    }

    // ── Çakışma Kontrolü ─────────────────────────────────────────────────────

    public async Task<ConflictResult> CheckKayitConflictsAsync(PuantajKayit kayit)
        => await CheckConflictsAsync(new List<PuantajKayit> { kayit });

    public async Task<ConflictResult> CheckConflictsAsync(List<PuantajKayit> kayitlar)
    {
        var result = new ConflictResult();
        if (kayitlar.Count == 0) return result;

        var yil = kayitlar[0].Yil;
        var ay = kayitlar[0].Ay;

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Aynı dönemdeki tüm kayıtları al (çakışma karşılaştırması için)
        var mevcutTumKayitlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay)
            .ToListAsync();

        // Tüm kayıtların (mevcut + yeni) birleşik listesi
        var tumKayitlar = mevcutTumKayitlar
            .Where(m => !kayitlar.Any(k => k.Id == m.Id))
            .Concat(kayitlar)
            .ToList();

        // Guzergah kapasite haritası (Kapasite kuralı için)
        var guzergahIds = tumKayitlar
            .Select(k => k.GuzergahId).Where(id => id.HasValue).Select(id => id!.Value)
            .Distinct().ToList();
        var kapasiteMap = await db.Guzergahlar
            .Where(g => guzergahIds.Contains(g.Id) && g.PersonelSayisi > 0)
            .ToDictionaryAsync(g => g.Id, g => g.PersonelSayisi);

        foreach (var kayit in kayitlar)
        {
            if (kayit.GuzergahId == null || kayit.AracId == null) continue;

            for (int gun = 1; gun <= 31; gun++)
            {
                var gunDeger = kayit.GetGunDeger(gun);
                if (gunDeger == 0) continue;

                // Kural 1: Aynı gün + aynı güzergah + aynı slot → tek araç
                var ayniGuzergahSlot = mevcutTumKayitlar
                    .Where(p => p.Id != kayit.Id
                        && p.GuzergahId == kayit.GuzergahId
                        && p.Slot == kayit.Slot
                        && p.AracId != kayit.AracId
                        && p.GetGunDeger(gun) > 0)
                    .ToList();

                foreach (var cakisan in ayniGuzergahSlot)
                {
                    result.Conflicts.Add(new ConflictItem
                    {
                        Severity = ConflictSeverity.Blocking,
                        Kural = "Tek Arac",
                        Mesaj = $"Gun {gun}: {kayit.Slot} slotunda '{cakisan.Guzergah?.GuzergahAdi ?? cakisan.GuzergahAdi}' guzergahinda zaten {cakisan.Plaka ?? cakisan.Arac?.Plaka} araci atanmis.",
                        Gun = gun,
                        Slot = kayit.Slot,
                        EtkilenenKayitId = cakisan.Id,
                        EtkilenenAciklama = $"{cakisan.Plaka} / {cakisan.SoforAdi}"
                    });
                }

                // Kural 2: Aynı gün + aynı slot → aynı araç tek güzergah
                var ayniAracSlot = mevcutTumKayitlar
                    .Where(p => p.Id != kayit.Id
                        && p.AracId == kayit.AracId
                        && p.Slot == kayit.Slot
                        && p.GuzergahId != kayit.GuzergahId
                        && p.GetGunDeger(gun) > 0)
                    .ToList();

                foreach (var cakisan in ayniAracSlot)
                {
                    result.Conflicts.Add(new ConflictItem
                    {
                        Severity = ConflictSeverity.Blocking,
                        Kural = "Tek Guzergah",
                        Mesaj = $"Gun {gun}: {kayit.Slot} slotunda '{cakisan.Plaka}' araci zaten '{cakisan.Guzergah?.GuzergahAdi ?? cakisan.GuzergahAdi}' guzergahinda gorevli.",
                        Gun = gun,
                        Slot = kayit.Slot,
                        EtkilenenKayitId = cakisan.Id,
                        EtkilenenAciklama = cakisan.Guzergah?.GuzergahAdi ?? cakisan.GuzergahAdi
                    });
                }

                // Kural 5: Kapasite — güzergah başına slot başına araç sayısı PersonelSayisi'ni aşamaz
                if (kayit.GuzergahId.HasValue && kapasiteMap.TryGetValue(kayit.GuzergahId.Value, out var kapasite) && kapasite > 0)
                {
                    var ayniGuzergahSlotAracSayisi = tumKayitlar
                        .Count(x => x.Id != kayit.Id && x.GuzergahId == kayit.GuzergahId
                                    && x.Slot == kayit.Slot && x.GetGunDeger(gun) > 0);
                    if (ayniGuzergahSlotAracSayisi >= kapasite)
                    {
                        result.Conflicts.Add(new ConflictItem
                        {
                            Severity = ConflictSeverity.Blocking,
                            Kural = "Kapasite",
                            Mesaj = $"Bu guzergahin kapasitesi ({kapasite} arac) dolu. Gun {gun}, Slot {kayit.Slot}.",
                            Gun = gun,
                            Slot = kayit.Slot
                        });
                    }
                }

                // Kural 4: Tedarikçi izolasyonu - kendi araç + tedarikçi şoför (Warning)
                if (kayit.KaynakTipi == PlanlamaKaynakTipi.Kendi && kayit.SoforOdemeTipi != SoforOdemeTipi.Ozmal)
                {
                    result.Conflicts.Add(new ConflictItem
                    {
                        Severity = ConflictSeverity.Warning,
                        Kural = "Izolasyon",
                        Mesaj = $"Kendi aracla tedarikci sofor ({kayit.SoforOdemeTipi}) eslesmesi onay gerektirir.",
                        Gun = 0,
                        Slot = kayit.Slot
                    });
                }

                // Kural 3: Aynı gün + aynı slot → aynı şoför farklı görev (Warning)
                if (kayit.SoforId.HasValue)
                {
                    var ayniSofor = mevcutTumKayitlar
                        .Where(p => p.Id != kayit.Id
                            && p.SoforId == kayit.SoforId
                            && p.Slot == kayit.Slot
                            && p.GetGunDeger(gun) > 0)
                        .ToList();

                    foreach (var cakisan in ayniSofor)
                    {
                        if (!result.Conflicts.Any(c => c.EtkilenenKayitId == cakisan.Id && c.Kural == "Tek Sofor"))
                        {
                            result.Conflicts.Add(new ConflictItem
                            {
                                Severity = ConflictSeverity.Warning,
                                Kural = "Tek Sofor",
                                Mesaj = $"Gun {gun}: {kayit.Slot} slotunda '{cakisan.SoforAdi}' soforu birden fazla gorevde.",
                                Gun = gun,
                                Slot = kayit.Slot,
                                EtkilenenKayitId = cakisan.Id,
                                EtkilenenAciklama = $"{cakisan.Guzergah?.GuzergahAdi ?? cakisan.GuzergahAdi} / {cakisan.Plaka}"
                            });
                        }
                    }
                }
            }
        }

        return result;
    }

    public async Task DeletePuantajAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var kayit = await db.PuantajKayitlar.FindAsync(id);
        if (kayit == null) return;
        kayit.IsDeleted  = true;
        kayit.UpdatedAt  = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // ── Linked OperasyonKaydi soft-delete ────────────────────────────
        try { await _syncService.DeleteLinkedOpsAsync(id); }
        catch (Exception ex) { _logger.LogWarning(ex, "Delete sync hatasi: PuantajKayitId={Id}", id); }
    }

    // ── Şablon oluşturma ─────────────────────────────────────────────────────

    public async Task<List<PuantajKayit>> SablonOlusturAsync(int kurumId, int yil, int ay)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var guzergahlar = await db.Guzergahlar
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .Where(g => !g.IsDeleted && g.Aktif && g.KurumId == kurumId)
            .ToListAsync();

        var guzergahIds = guzergahlar.Select(g => g.Id).ToList();

        // GuzergahSefer satırlarını yükle (öncelikli araç/şoför kaynağı)
        // SADECE AKTİF seferler — soft-delete edilmiş kayıtlar puantaja GİRMEZ
        var seferler = await db.GuzergahSeferleri
            .Where(s => guzergahIds.Contains(s.GuzergahId) && s.AracId.HasValue && !s.IsDeleted)
            .Include(s => s.Arac)
            .OrderBy(s => s.GuzergahId).ThenBy(s => s.Sira)
            .ToListAsync();
        var seferMap = seferler.GroupBy(s => s.GuzergahId).ToDictionary(g => g.Key, g => g.ToList());

        // Mevcut kayıtları al
        var mevcutlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && guzergahIds.Contains(p.GuzergahId!.Value))
            .ToListAsync();

        // SoforAd → SoforId eşleme tablosu (isimden ID çözümleme, Türkçe karakter normalize)
        var soforAdIdMap = await db.Soforler
            .Where(s => !s.IsDeleted && guzergahIds.Contains(s.FirmaId ?? 0))
            .Select(s => new { s.Ad, s.Soyad, s.Id, AdSoyad = s.TamAd })
            .ToListAsync();
        var soforLookup = soforAdIdMap
            .Select(s => new { s.Id, Normalized = NormalizeSoforAd(s.AdSoyad) })
            .GroupBy(s => s.Normalized)
            .ToDictionary(g => g.Key, g => g.First().Id);

        int? ResolveSoforId(string? soforAd)
        {
            if (string.IsNullOrWhiteSpace(soforAd)) return null;
            var key = NormalizeSoforAd(soforAd.Trim());
            if (!soforLookup.TryGetValue(key, out var id)) return null;
            // Çoklu eşleşme kontrolü: aynı key'de birden fazla ID varsa güvenli değil
            var matches = soforAdIdMap.Count(s => NormalizeSoforAd(s.AdSoyad) == key);
            return matches == 1 ? id : null;
        }

        var sonuc = new List<PuantajKayit>(mevcutlar);

        foreach (var guzergah in guzergahlar)
        {
            // Öncelik 1: GuzergahSefer satırlarından araç/şoför ata
            if (seferMap.TryGetValue(guzergah.Id, out var gSeferler) && gSeferler.Any())
            {
                foreach (var sefer in gSeferler)
                {
                    var resolvedSoforId = ResolveSoforId(sefer.SoforAd);
                    // Her sefer = 1 puantaj satiri, kendi slot'u ile
                    EkleEksikSatir(sonuc, mevcutlar, guzergah, sefer.AracId!.Value,
                        sefer.Arac?.AktifPlaka ?? sefer.Arac?.Plaka, resolvedSoforId, sefer.SoforAd, yil, ay, sefer.Slot, sefer.SeferTipi);
                }
                continue;
            }

            // Öncelik 2: FiloGuzergahEslestirme'den araçlar
            var eslestirmeler = await db.FiloGuzergahEslestirmeleri
                .Include(e => e.Arac)
                .Include(e => e.Sofor)
                .Where(e => e.GuzergahId == guzergah.Id && e.IsActive)
                .ToListAsync();

            if (!eslestirmeler.Any() && guzergah.VarsayilanArac != null)
            {
                // Varsayılan araçla, sadece güzergahın SeferTipi'ne uygun slotlar
                foreach (var slot in SeferTipindenSlotlara(guzergah.SeferTipi))
                {
                    EkleEksikSatir(sonuc, mevcutlar, guzergah, guzergah.VarsayilanArac.Id,
                        guzergah.VarsayilanArac.AktifPlaka ?? guzergah.VarsayilanArac.Plaka,
                        guzergah.VarsayilanSoforId,
                        guzergah.VarsayilanSofor != null ? $"{guzergah.VarsayilanSofor.Ad} {guzergah.VarsayilanSofor.Soyad}" : null,
                        yil, ay, slot);
                }
            }
            else
            {
                foreach (var e in eslestirmeler)
                {
                    var soforAdi = e.Sofor != null ? $"{e.Sofor.Ad} {e.Sofor.Soyad}" : null;
                    foreach (var slot in SeferTipindenSlotlara(guzergah.SeferTipi))
                    {
                        EkleEksikSatir(sonuc, mevcutlar, guzergah, e.AracId,
                            e.Arac?.AktifPlaka ?? e.Arac?.Plaka,
                            e.SoforId, soforAdi, yil, ay, slot);
                    }
                }
            }
        }

        sonuc = ApplyMergeAndPricing(sonuc, guzergahlar);
        return sonuc.OrderBy(p => p.GuzergahId).ThenBy(p => p.Plaka).ThenBy(p => p.Slot).ToList();
    }

    private static List<PuantajKayit> ApplyMergeAndPricing(
        List<PuantajKayit> rows,
        List<Guzergah> guzergahlar)
    {
        var guzergahMap = guzergahlar.ToDictionary(g => g.Id);
        var toRemove = new List<PuantajKayit>();

        foreach (var group in rows.GroupBy(r => r.GuzergahId))
        {
            var guzergahId = group.Key ?? 0;
            if (!guzergahMap.TryGetValue(guzergahId, out var guzergah)) continue;
            if (guzergah.SeferTipi != SeferTipi.SabahAksam) continue;

            var sabahRows = group.Where(r => r.Slot == SeferSlot.Sabah).ToList();
            var aksamRows = group.Where(r => r.Slot == SeferSlot.Aksam).ToList();

            foreach (var sabah in sabahRows)
            {
                var matching = aksamRows.FirstOrDefault(a =>
                    a.AracId == sabah.AracId
                    && string.Equals(a.SoforAdi ?? string.Empty, sabah.SoforAdi ?? string.Empty, StringComparison.OrdinalIgnoreCase));

                if (matching != null)
                {
                    // Merge: Sabah satırını SabahAksam'a yükselt, Aksam'ı sil
                    sabah.Yon = PuantajYon.SabahAksam;
                    sabah.BirimGelir = guzergah.GelirFiyat * 2;
                    sabah.BirimGider = guzergah.GiderFiyat * 2;
                    toRemove.Add(matching);
                }
                else
                {
                    // Farklı araç/şoför: Yon düzelt (merge olmadı)
                    sabah.Yon = PuantajYon.Sabah;
                }
            }

            // Merge olmamış Aksam satırlarının Yon'unu düzelt
            foreach (var aksam in aksamRows.Where(r => !toRemove.Contains(r)))
            {
                aksam.Yon = PuantajYon.Aksam;
            }
        }

        rows.RemoveAll(r => toRemove.Contains(r));
        return rows;
    }

    // ── Excel Import ──────────────────────────────────────────────────────────

    public async Task<List<PuantajImportSonuc>> TopluImportAsync(List<PuantajImportSatiri> satirlar)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sonuclar = new List<PuantajImportSonuc>();

        // Lookup verileri tek seferde yukle
        var guzergahlar = await db.Guzergahlar.Where(g => g.Aktif && !g.IsDeleted).ToListAsync();
        var kurumlar = await db.Kurumlar.Where(k => k.Aktif).ToListAsync();
        var araclar = await db.Araclar.Where(a => a.Aktif && !a.IsDeleted).ToListAsync();

        var donemYil = satirlar.FirstOrDefault()?.Yil ?? DateTime.Today.Year;
        var donemAy = satirlar.FirstOrDefault()?.Ay ?? DateTime.Today.Month;
        var mevcutKayitlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == donemYil && p.Ay == donemAy)
            .ToListAsync();

        foreach (var s in satirlar)
        {
            try
            {
                // Plaka → Araç eşleştirme
                var plaka = s.Plaka.Trim().ToUpperInvariant();
                var arac = araclar.FirstOrDefault(a =>
                    (a.AktifPlaka ?? "").Equals(plaka, StringComparison.OrdinalIgnoreCase));

                // Güzergah eşleştirme
                var guzergah = guzergahlar.FirstOrDefault(g =>
                    (g.GuzergahAdi ?? "").Equals(s.GuzergahAdi, StringComparison.OrdinalIgnoreCase));

                if (guzergah == null)
                {
                    sonuclar.Add(new PuantajImportSonuc { Basarili = false, HataMesaji = $"Güzergah bulunamadı: {s.GuzergahAdi}" });
                    continue;
                }

                // Kurum eşleştirme
                int? kurumId = s.KurumId;
                if (!kurumId.HasValue && !string.IsNullOrWhiteSpace(s.KurumAdi))
                {
                    var kurum = kurumlar.FirstOrDefault(k =>
                        (k.KurumAdi ?? "").Equals(s.KurumAdi, StringComparison.OrdinalIgnoreCase));
                    kurumId = kurum?.Id;
                }

                // Zaten var mı kontrol et (aynı güzergah+araç+slot)
                var zatenVar = mevcutKayitlar.Any(m =>
                    m.GuzergahId == guzergah.Id && m.AracId == arac?.Id && m.Slot == s.Slot);
                if (zatenVar)
                {
                    sonuclar.Add(new PuantajImportSonuc { Atlandi = true });
                    continue;
                }

                var kayit = new PuantajKayit
                {
                    Yil = s.Yil, Ay = s.Ay,
                    Plaka = s.Plaka, GuzergahId = guzergah.Id,
                    GuzergahAdi = guzergah.GuzergahAdi,
                    AracId = arac?.Id, SoforAdi = s.SoforAdi,
                    KurumAdi = s.KurumAdi, KurumId = kurumId ?? guzergah.KurumId,
                    Slot = s.Slot, SlotAdi = s.SlotAdi,
                    Yon = s.Yon, Gun = s.Gun, SeferSayisi = s.SeferSayisi,
                    BirimGelir = s.BirimGelir, BirimGider = s.BirimGider,
                    FaturaKesiciAdi = s.FaturaKesiciAdi, Notlar = s.Notlar,
                    SoforOdemeTipi = SoforOdemeTipi.Ozmal,
                    KaynakTipi = PlanlamaKaynakTipi.Kendi,
                    FinansYonu = PlanlamaFinansYonu.Giden,
                    CreatedAt = DateTime.UtcNow
                };
                // Import: Gun değeri Excel'den gelir, bireysel gün (Gun01..Gun31) set edilmez.
                // HesaplaPuantajToplam() SeferGunuToplami (bireysel günler toplamı = 0) ile
                // Gun'u ezeceği için burada çağrılmaz; finansal alanlar manuel hesaplanır.
                kayit.ToplamGider = kayit.BirimGider * kayit.Gun;
                kayit.Odenecek = kayit.ToplamGider + kayit.GiderKdv20Tutari + kayit.GiderKdv10Tutari - kayit.GiderKesinti;

                db.PuantajKayitlar.Add(kayit);
                mevcutKayitlar.Add(kayit); // sonraki satırlar duplicate kontrolü için
                sonuclar.Add(new PuantajImportSonuc { Basarili = true });
            }
            catch (Exception ex)
            {
                sonuclar.Add(new PuantajImportSonuc { Basarili = false, HataMesaji = ex.Message });
            }
        }

        await db.SaveChangesAsync();

        // ── Import edilen kayitlar icin sync ─────────────────────────────
        try
        {
            var basariliPuantajIds = sonuclar
                .Where(s => s.Basarili && !s.Atlandi)
                .Select(_ => 0).ToList(); // Id'leri takip edemiyoruz
            if (basariliPuantajIds.Any() || sonuclar.Any(s => s.Basarili))
            {
                await _syncService.SyncFromPuantajTopluAsync(donemYil, donemAy, null, PuantajSyncMode.CreateUpdate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Import sync hatasi");
        }

        return sonuclar;
    }

    private static SeferSlot[] SeferTipindenSlotlara(SeferTipi tip) => tip switch
    {
        SeferTipi.Sabah => new[] { SeferSlot.Sabah },
        SeferTipi.Aksam => new[] { SeferSlot.Aksam },
        SeferTipi.SabahAksam => new[] { SeferSlot.Sabah, SeferSlot.Aksam },
        SeferTipi.Mesai => new[] { SeferSlot.Mesai },
        SeferTipi.Vardiya => new[] { SeferSlot.Mesai },
        SeferTipi.Saatlik => new[] { SeferSlot.Sabah, SeferSlot.Aksam, SeferSlot.Mesai, SeferSlot.Diger1, SeferSlot.Diger2, SeferSlot.Diger3 },
        _ => new[] { SeferSlot.Sabah }
    };

    private static void EkleEksikSatir(
        List<PuantajKayit> sonuc,
        List<PuantajKayit> mevcutlar,
        Guzergah guzergah,
        int aracId, string? plaka,
        int? soforId, string? soforAdi,
        int yil, int ay,
        SeferSlot slot = SeferSlot.Sabah,
        SeferTipi seferTipi = SeferTipi.SabahAksam)
    {
        var varMi = mevcutlar.Any(p => p.GuzergahId == guzergah.Id && p.AracId == aracId && p.Slot == slot);
        if (varMi) return;

        sonuc.Add(new PuantajKayit
        {
            GuzergahId  = guzergah.Id,
            Guzergah    = guzergah,
            AracId      = aracId,
            Plaka       = plaka ?? string.Empty,
            SoforId     = soforId,
            SoforAdi    = soforAdi,
            Yil         = yil,
            Ay          = ay,
            Slot        = slot,
            Yon         = seferTipi switch
            {
                SeferTipi.Sabah      => PuantajYon.Sabah,
                SeferTipi.Aksam      => PuantajYon.Aksam,
                SeferTipi.SabahAksam => PuantajYon.SabahAksam,
                SeferTipi.Mesai      => PuantajYon.SabahAksam,
                SeferTipi.Vardiya    => PuantajYon.SabahAksam,
                _                    => PuantajYon.SabahAksam
            },
            BirimGelir  = guzergah.GelirFiyat,
            BirimGider  = guzergah.GiderFiyat,
            SeferSayisi = 1,
            Gun         = 0
        });
    }

    // ── Puantaj Güncelleme ──────────────────────────────────────────────────

    public async Task<PuantajGuncellemeSonuc> GuncellePuantajAsync(int kurumId, int yil, int ay)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var guzergahlar = await db.Guzergahlar
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .Where(g => !g.IsDeleted && g.Aktif && g.KurumId == kurumId)
            .ToListAsync();

        var guzergahIds = guzergahlar.Select(g => g.Id).ToList();

        // Mevcut puantaj kayitlari
        var mevcutlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && guzergahIds.Contains(p.GuzergahId!.Value))
            .ToListAsync();

        // GuzergahSefer satirlari — SADECE AKTİF, soft-delete HARİÇ
        var seferler = await db.GuzergahSeferleri
            .Include(s => s.Arac)
            .Where(s => guzergahIds.Contains(s.GuzergahId) && s.AracId.HasValue && !s.IsDeleted)
            .OrderBy(s => s.GuzergahId).ThenBy(s => s.Sira)
            .ToListAsync();
        var seferMap = seferler.GroupBy(s => s.GuzergahId).ToDictionary(g => g.Key, g => g.ToList());

        int guncellenen = 0, eklenen = 0, degismeyen = 0;

        foreach (var guzergah in guzergahlar)
        {
            if (seferMap.TryGetValue(guzergah.Id, out var gSeferler) && gSeferler.Any())
            {
                foreach (var sefer in gSeferler)
                {
                    var slot = sefer.Slot;
                    {
                        var mevcut = mevcutlar.FirstOrDefault(p =>
                            p.GuzergahId == guzergah.Id && p.Slot == slot);

                        if (mevcut == null)
                        {
                            // Yeni puantaj satiri ekle
                            var yeni = new PuantajKayit
                            {
                                GuzergahId = guzergah.Id,
                                GuzergahAdi = guzergah.GuzergahAdi,
                                AracId = sefer.AracId!.Value,
                                Plaka = sefer.Arac?.AktifPlaka ?? sefer.Arac?.Plaka,
                                SoforAdi = sefer.SoforAd,
                                KurumId = kurumId,
                                Slot = slot,
                                Yil = yil,
                                Ay = ay,
                                SeferSayisi = 1,
                                Gun = 0,
                                Yon = sefer.SeferTipi switch
                                {
                                    SeferTipi.Sabah => PuantajYon.Sabah,
                                    SeferTipi.Aksam => PuantajYon.Aksam,
                                    SeferTipi.SabahAksam => PuantajYon.SabahAksam,
                                    SeferTipi.Mesai => PuantajYon.SabahAksam,
                                    SeferTipi.Vardiya => PuantajYon.SabahAksam,
                                    _ => PuantajYon.SabahAksam
                                },
                                KaynakTipi = PlanlamaKaynakTipi.Kendi,
                                FinansYonu = PlanlamaFinansYonu.Giden,
                                SoforOdemeTipi = SoforOdemeTipi.Ozmal,
                                CreatedAt = DateTime.UtcNow
                            };
                            db.PuantajKayitlar.Add(yeni);
                            eklenen++;
                        }
                        else
                        {
                            // Mevcut kaydi guncelle (arac/sofor degisti mi?)
                            var aracDegisti = mevcut.AracId != sefer.AracId;
                            var soforDegisti = (mevcut.SoforAdi ?? "") != (sefer.SoforAd ?? "");

                            if (aracDegisti || soforDegisti)
                            {
                                mevcut.AracId = sefer.AracId;
                                mevcut.Plaka = sefer.Arac?.AktifPlaka ?? sefer.Arac?.Plaka;
                                mevcut.SoforAdi = sefer.SoforAd;
                                mevcut.UpdatedAt = DateTime.UtcNow;
                                guncellenen++;
                            }
                            else
                            {
                                degismeyen++;
                            }
                        }
                    }
                }
            }
            else if (guzergah.VarsayilanArac != null)
            {
                // GuzergahSefer yoksa varsayilan arac/slot ile ekle
                foreach (var slot in SeferTipindenSlotlara(guzergah.SeferTipi))
                {
                    var mevcut = mevcutlar.FirstOrDefault(p =>
                        p.GuzergahId == guzergah.Id && p.Slot == slot);

                    if (mevcut == null)
                    {
                        var yeni = new PuantajKayit
                        {
                            GuzergahId = guzergah.Id,
                            GuzergahAdi = guzergah.GuzergahAdi,
                            AracId = guzergah.VarsayilanArac.Id,
                            Plaka = guzergah.VarsayilanArac.AktifPlaka ?? guzergah.VarsayilanArac.Plaka,
                            SoforAdi = guzergah.VarsayilanSofor != null
                                ? $"{guzergah.VarsayilanSofor.Ad} {guzergah.VarsayilanSofor.Soyad}"
                                : null,
                            KurumId = kurumId,
                            Slot = slot,
                            Yil = yil,
                            Ay = ay,
                            SeferSayisi = 1,
                            Gun = 0,
                            Yon = guzergah.SeferTipi switch
                            {
                                SeferTipi.Sabah => PuantajYon.Sabah,
                                SeferTipi.Aksam => PuantajYon.Aksam,
                                SeferTipi.SabahAksam => PuantajYon.SabahAksam,
                                SeferTipi.Mesai => PuantajYon.SabahAksam,
                                SeferTipi.Vardiya => PuantajYon.SabahAksam,
                                _ => PuantajYon.SabahAksam
                            },
                            KaynakTipi = PlanlamaKaynakTipi.Kendi,
                            FinansYonu = PlanlamaFinansYonu.Giden,
                            SoforOdemeTipi = SoforOdemeTipi.Ozmal,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.PuantajKayitlar.Add(yeni);
                        eklenen++;
                    }
                    else
                    {
                        var aracDegisti = mevcut.AracId != guzergah.VarsayilanAracId;
                        if (aracDegisti)
                        {
                            mevcut.AracId = guzergah.VarsayilanAracId;
                            mevcut.Plaka = guzergah.VarsayilanArac.AktifPlaka ?? guzergah.VarsayilanArac.Plaka;
                            mevcut.UpdatedAt = DateTime.UtcNow;
                            guncellenen++;
                        }
                        else degismeyen++;
                    }
                }
            }
        }

        // Merge post-processing: Sabah+Aksam aynı araç/şoför ise birleştir
        var tumKayitlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && guzergahIds.Contains(p.GuzergahId!.Value))
            .ToListAsync();
        ApplyMergeAndPricing(tumKayitlar, guzergahlar);

        await db.SaveChangesAsync();
        return new PuantajGuncellemeSonuc { Guncellenen = guncellenen, Eklenen = eklenen, Degismeyen = degismeyen };
    }

    public async Task<int> PuantajKaldirAsync(int kurumId, int yil, int ay)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var guzergahIds = await db.Guzergahlar
            .Where(g => !g.IsDeleted && g.KurumId == kurumId)
            .Select(g => g.Id)
            .ToListAsync();

        var kayitlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && guzergahIds.Contains(p.GuzergahId!.Value))
            .ToListAsync();

        int count = kayitlar.Count;
        foreach (var k in kayitlar)
        {
            k.IsDeleted = true;
            k.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        // ── Tüm linked OperasyonKayitlari soft-delete ────────────────────
        try
        {
            foreach (var k in kayitlar)
                await _syncService.DeleteLinkedOpsAsync(k.Id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "PuantajKaldir sync hatasi: {Count} kayit", count); }

        return count;
    }

    /// <summary>
    /// Slot/Yon bazlı gerçek sefer sayısı: Sabah=1, Akşam=1, SabahAksam=2.
    /// LINQ sorgusunda inline ternary olarak uygulanır; bu metod referans/sunucu-tarafi kullanim icindir.
    /// </summary>
    /// <summary>
    /// DB NOT NULL enum/text kolonları için varsayılan değerleri garantiler.
    /// </summary>
    /// <summary>
    /// SaveChangesAsync öncesi guard: SoforId NULL olan Added/Modified entity varsa exception fırlatır.
    /// PostgreSQL 23502 hatasını önler.
    /// </summary>
    private static void ValidateRequiredFields(ApplicationDbContext db)
    {
        var eksikler = db.ChangeTracker
            .Entries<PuantajKayit>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Where(e => e.Entity.SoforId == null || e.Entity.SoforId == 0)
            .Select(e => new { e.Entity.GuzergahAdi, e.Entity.SoforAdi, e.Entity.Plaka, e.Entity.Yon, e.Entity.Slot })
            .ToList();

        if (eksikler.Count == 0) return;

        var detay = string.Join("; ", eksikler.Select(x =>
            $"{x.GuzergahAdi} / {x.Plaka} / {x.SoforAdi} / {x.Yon} / {x.Slot}"));
        throw new InvalidOperationException(
            "Şoför kartı eşleşmesi olmayan puantaj satırları var. Kayıt yapılmadı. " +
            "Lütfen ilgili güzergah seferinde şoför kartını seçiniz. Detay: " + detay);
    }

    private static void EnsurePuantajDefaults(PuantajKayit kayit)
    {
        if (kayit.OnayDurum == default)
            kayit.OnayDurum = PuantajOnayDurum.Taslak;
        if (kayit.GelirOdemeDurumu == default)
            kayit.GelirOdemeDurumu = PuantajOdemeDurum.Odenmedi;
        if (kayit.GiderOdemeDurumu == default)
            kayit.GiderOdemeDurumu = PuantajOdemeDurum.Odenmedi;
        if (kayit.SoforOdemeTipi == default)
            kayit.SoforOdemeTipi = SoforOdemeTipi.Ozmal;
        if (kayit.Kaynak == default)
            kayit.Kaynak = PuantajKaynak.Manuel;
        if (kayit.Yon == default)
            kayit.Yon = PuantajYon.SabahAksam;
    }

    /// <summary>
    /// Şoför adı normalize: Türkçe karakter → ASCII, fazla boşluk temizle, uppercase.
    /// "HALİL KULELİ" ve "HALIL KULELI" aynı key'e dönüşür.
    /// </summary>
    private static string NormalizeSoforAd(string? adSoyad)
    {
        if (string.IsNullOrWhiteSpace(adSoyad)) return string.Empty;
        var n = adSoyad.Trim().ToUpperInvariant()
            .Replace('İ', 'I').Replace('Ü', 'U').Replace('Ö', 'O')
            .Replace('Ş', 'S').Replace('Ç', 'C').Replace('Ğ', 'G')
            .Replace('ı', 'I').Replace('ü', 'U').Replace('ö', 'O')
            .Replace('ş', 'S').Replace('ç', 'C').Replace('ğ', 'G');
        return string.Join(" ", n.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Yön dropdown seçenekleri (görünen ad → PuantajYon enum eşlemesi).
    /// Mesai/Gece/Yasa/Ek Sefer gibi değerler Diger olarak saklanır.
    /// Tam alt tür bilgisi için ileride ayrı kolon gerekebilir.
    /// </summary>
    public static readonly List<(string Display, PuantajYon Value)> YonDropdownSecenekleri = new()
    {
        ("Sabah", PuantajYon.Sabah),
        ("Akşam", PuantajYon.Aksam),
        ("S+A", PuantajYon.SabahAksam),
        ("Mesai", PuantajYon.Diger),
        ("Gece", PuantajYon.Diger),
        ("Yasa", PuantajYon.Diger),
        ("Ek Sefer", PuantajYon.Diger),
        ("Resmi Tatil", PuantajYon.Diger),
        ("Hafta Sonu", PuantajYon.Diger),
        ("Özel Sefer", PuantajYon.Diger),
    };

    public static string GetYonDisplayText(PuantajYon yon) =>
        YonDropdownSecenekleri.FirstOrDefault(x => x.Value == yon).Display ?? yon.ToString();

    public static PuantajYon ParseYonDisplay(string? display) =>
        YonDropdownSecenekleri.FirstOrDefault(x => string.Equals(x.Display, display, StringComparison.OrdinalIgnoreCase)).Value;

    private static int GercekSefer(OperasyonKaydi o) => o.Yon switch
    {
        PuantajYon.SabahAksam => 2,
        _ => 1
    };

    // ── OperasyonKaydi Varlık Sorgusu (N+1 önlemli) ─────────────────────

    public async Task<Dictionary<int, bool>> GetOperasyonKaydiVarligiAsync(List<int> puantajIds)
    {
        if (!puantajIds.Any()) return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var set = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted && o.KaynakPuantajId != null
                        && puantajIds.Contains(o.KaynakPuantajId.Value))
            .Select(o => o.KaynakPuantajId!.Value)
            .Distinct()
            .ToListAsync();
        return set.ToDictionary(id => id, _ => true);
    }
}
