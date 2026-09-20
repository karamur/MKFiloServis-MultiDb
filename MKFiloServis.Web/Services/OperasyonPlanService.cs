using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// İzole operasyonel puantaj çekirdeği servisi.
/// Sadece yeni puantaj tablolarına yazar; araç/personel/muhasebe/bütçe modüllerine dokunmaz.
/// Mevcut FiloGuzergahEslestirme ve FiloGunlukPuantaj yalnızca okuma/teyit amaçlı kullanılır.
/// </summary>
public class OperasyonPlanService : IOperasyonPlanService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<OperasyonPlanService> _logger;

    public OperasyonPlanService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ILogger<OperasyonPlanService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public Task<(int Olusan, int Atlanan)> PlanUretAsync(DateTime tarih)
        => PlanUretAsync(tarih, haftaSonunuDahilEt: false);

    private async Task<(int Olusan, int Atlanan)> PlanUretAsync(DateTime tarih, bool haftaSonunuDahilEt)
    {
        var gun = tarih.Date;
        await using var db = await _dbFactory.CreateDbContextAsync();

        var takvim = await db.OperasyonTakvimGunleri
            .FirstOrDefaultAsync(t => t.Tarih == gun);
        var carpan = takvim?.PuantajCarpani ?? 1.0m;

        if (takvim?.GunTipi == OperasyonGunTipi.Tatil)
        {
            _logger.LogInformation("PLAN_URET: {Tarih} tatil günü, plan üretilmedi.", gun);
            return (0, 0);
        }

        if (!haftaSonunuDahilEt && takvim is null && gun.DayOfWeek is (DayOfWeek.Saturday or DayOfWeek.Sunday))
        {
            _logger.LogInformation("PLAN_URET: {Tarih} hafta sonu, plan üretilmedi.", gun);
            return (0, 0);
        }

        var eslestirmeler = await db.Set<FiloGuzergahEslestirme>()
            .Where(e => e.IsActive)
            .AsNoTracking()
            .ToListAsync();

        var mevcutEslestirmeIdleri = await db.OperasyonPlanSatirlari
            .Where(p => p.Tarih == gun)
            .Select(p => p.FiloGuzergahEslestirmeId)
            .ToListAsync();
        var mevcutSet = mevcutEslestirmeIdleri.ToHashSet();

        int olusan = 0, atlanan = 0;
        foreach (var e in eslestirmeler)
        {
            if (mevcutSet.Contains(e.Id)) { atlanan++; continue; }

            db.OperasyonPlanSatirlari.Add(new OperasyonPlanSatiri
            {
                Tarih = gun,
                FiloGuzergahEslestirmeId = e.Id,
                KurumFirmaId = e.KurumFirmaId,
                GuzergahId = e.GuzergahId,
                AracId = e.AracId,
                SoforId = e.SoforId,
                ServisTuru = e.ServisTuru,
                PlanlananSefer = 1m,
                PuantajCarpani = carpan,
                KurumSeferUcretiSnapshot = e.KurumaKesilecekUcret,
                TaseronSeferUcretiSnapshot = e.TaseronaOdenenUcret,
                Durum = carpan > 0m ? OperasyonPlanDurumu.Planlandi : OperasyonPlanDurumu.EksikGiris
            });
            olusan++;
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("PLAN_URET: {Tarih} için {Olusan} plan oluştu, {Atlanan} atlandı.", gun, olusan, atlanan);
        return (olusan, atlanan);
    }

    public async Task<(int Olusan, int Atlanan, int IslenmeyenGun)> AyPlaniUretAsync(int yil, int ay)
    {
        if (ay is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(ay));

        var ayBaslangic = new DateTime(yil, ay, 1);
        var aySonu = ayBaslangic.AddMonths(1);
        var toplamOlusan = 0;
        var toplamAtlanan = 0;
        var islenmeyenGun = 0;

        for (var tarih = ayBaslangic; tarih < aySonu; tarih = tarih.AddDays(1))
        {
            var sonuc = await PlanUretAsync(tarih);
            if (sonuc.Olusan == 0 && sonuc.Atlanan == 0)
                islenmeyenGun++;

            toplamOlusan += sonuc.Olusan;
            toplamAtlanan += sonuc.Atlanan;
        }

        _logger.LogInformation(
            "AYLIK_PLAN_URET: {Yil}/{Ay} için {Olusan} plan oluştu, {Atlanan} atlandı, {IslenmeyenGun} gün işlenmedi.",
            yil, ay, toplamOlusan, toplamAtlanan, islenmeyenGun);

        return (toplamOlusan, toplamAtlanan, islenmeyenGun);
    }

    public async Task<(int Olusan, int Atlanan)> HaftaSonuPlanlariniUretAsync(int yil, int ay)
    {
        var ayBaslangic = new DateTime(yil, ay, 1);
        var gunSayisi = DateTime.DaysInMonth(yil, ay);
        int olusan = 0, atlanan = 0;

        for (var gun = 0; gun < gunSayisi; gun++)
        {
            var tarih = ayBaslangic.AddDays(gun);
            if (tarih.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                continue;

            // Yalnızca varsayılan hafta sonu kuralını aş; tatil ve çarpan kurallarını koru.
            var sonuc = await PlanUretAsync(tarih, haftaSonunuDahilEt: true);
            olusan += sonuc.Olusan;
            atlanan += sonuc.Atlanan;
        }

        return (olusan, atlanan);
    }

    public async Task<List<OperasyonPlanSatiri>> GetPlanlarAsync(DateTime tarih)
    {
        var gun = tarih.Date;
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.OperasyonPlanSatirlari
            .Where(p => p.Tarih == gun)
            .OrderBy(p => p.GuzergahId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<OperasyonPlanSatiri> EkSeferPlanSatiriEkleAsync(
        DateTime tarih,
        int guzergahId,
        int aracId,
        int soforId,
        decimal seferSayisi,
        decimal? kurumSeferUcreti,
        ServisTuru servisTuru = ServisTuru.SabahAksam)
    {
        if (seferSayisi <= 0m)
            throw new ArgumentOutOfRangeException(nameof(seferSayisi), "Sefer sayısı sıfırdan büyük olmalıdır.");

        var gun = tarih.Date;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var guzergah = await db.Set<Guzergah>()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guzergahId);

        if (guzergah is null)
            throw new InvalidOperationException("Seçilen güzergâh bulunamadı.");

        var takvim = await db.OperasyonTakvimGunleri
            .FirstOrDefaultAsync(t => t.Tarih == gun);
        if (takvim?.GunTipi == OperasyonGunTipi.Tatil)
            throw new InvalidOperationException("Tatil gününe ek sefer planı eklenemez.");

        var plan = new OperasyonPlanSatiri
        {
            Tarih = gun,
            // Ek sefer, güzergâh-plaka eşleştirmesi olmadan da oluşturulabilir.
            // Mevcut şema bu alanı zorunlu tuttuğu için 0, serbest ek sefer bağlantısıdır.
            FiloGuzergahEslestirmeId = 0,
            KurumFirmaId = guzergah.CariId,
            GuzergahId = guzergahId,
            AracId = aracId,
            SoforId = soforId,
            ServisTuru = servisTuru,
            PlanlananSefer = seferSayisi,
            PuantajCarpani = takvim?.PuantajCarpani ?? 1.0m,
            KurumSeferUcretiSnapshot = kurumSeferUcreti ?? guzergah.BirimFiyat,
            TaseronSeferUcretiSnapshot = guzergah.GiderFiyat,
            Durum = (takvim?.PuantajCarpani ?? 1.0m) > 0m
                ? OperasyonPlanDurumu.Planlandi
                : OperasyonPlanDurumu.EksikGiris
        };

        db.OperasyonPlanSatirlari.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<int> PlanlariGuncelleAsync(List<OperasyonPlanSatiri> planlar)
    {
        if (planlar.Count == 0)
            return 0;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var planIdleri = planlar.Select(p => p.Id).ToList();
        var kayitlar = await db.OperasyonPlanSatirlari
            .Where(p => planIdleri.Contains(p.Id) && p.Durum == OperasyonPlanDurumu.Planlandi)
            .ToListAsync();

        var guncellenecekler = planlar.ToDictionary(p => p.Id);
        foreach (var kayit in kayitlar)
        {
            if (!guncellenecekler.TryGetValue(kayit.Id, out var kaynak))
                continue;

            kayit.PlanlananSefer = kaynak.PlanlananSefer < 0 ? 0 : kaynak.PlanlananSefer;
            kayit.ServisTuru = kaynak.ServisTuru;
        }

        await db.SaveChangesAsync();
        return kayitlar.Count;
    }

    public async Task DuzenlemeIcinGeriAlAsync(int planId)
    {
        if (planId <= 0)
            throw new ArgumentOutOfRangeException(nameof(planId));

        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var plan = await db.OperasyonPlanSatirlari
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null)
            throw new InvalidOperationException("Düzenlenecek plan bulunamadı.");

        if (!plan.FiloGunlukPuantajId.HasValue)
        {
            plan.Durum = OperasyonPlanDurumu.Planlandi;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return;
        }

        var puantaj = await db.FiloGunlukPuantajlar
            .FirstOrDefaultAsync(p => p.Id == plan.FiloGunlukPuantajId.Value && !p.IsDeleted);
        if (puantaj is null)
            throw new InvalidOperationException("Plana bağlı puantaj kaydı bulunamadı.");

        if (puantaj.Onaylandi || puantaj.KurumFaturaKesildiMi || puantaj.TaseronOdemeYapildiMi ||
            puantaj.KurumFaturaId.HasValue || puantaj.TedarikciOdemeFaturaId.HasValue)
        {
            throw new InvalidOperationException("Onaylanmış, faturalanmış veya ödemesi yapılmış puantaj düzenlenemez.");
        }

        var hakedisDetaylari = await db.HakedisDetaylari
            .Include(d => d.Hakedis)
            .Where(d => d.FiloGunlukPuantajId == puantaj.Id)
            .ToListAsync();

        if (hakedisDetaylari.Any(d => d.Hakedis is not null &&
            d.Hakedis.Durum is not (HakedisDurum.Taslak or HakedisDurum.Iptal)))
        {
            throw new InvalidOperationException("Onaylanmış veya kapanmış hakedişe bağlı puantaj düzenlenemez.");
        }

        var hakedisler = hakedisDetaylari
            .Select(d => d.Hakedis)
            .Where(h => h is not null)
            .Cast<Hakedis>()
            .DistinctBy(h => h.Id)
            .ToList();

        if (hakedisDetaylari.Count > 0)
            db.HakedisDetaylari.RemoveRange(hakedisDetaylari);
        if (hakedisler.Count > 0)
            db.Hakedisler.RemoveRange(hakedisler);

        db.FiloGunlukPuantajlar.Remove(puantaj);
        plan.FiloGunlukPuantajId = null;
        plan.Durum = OperasyonPlanDurumu.Planlandi;
        plan.TeyitTarihi = null;
        plan.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        _logger.LogWarning("PLAN_DUZENLEME_GERI_AL: {PlanId} bekleyen duruma alındı.", planId);
    }

    public async Task SilTekGunlukPlanAsync(int planId)
    {
        if (planId <= 0)
            throw new ArgumentOutOfRangeException(nameof(planId));

        await using var db = await _dbFactory.CreateDbContextAsync();
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var plan = await db.OperasyonPlanSatirlari
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan is null)
                throw new InvalidOperationException("Silinecek günlük plan bulunamadı.");

            if (plan.Durum != OperasyonPlanDurumu.Planlandi || plan.FiloGunlukPuantajId.HasValue)
                throw new InvalidOperationException("Bu günlük plan teyit edildiği veya puantaja aktarıldığı için silinemez.");

            db.OperasyonPlanSatirlari.Remove(plan);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        _logger.LogWarning("TEK_GUN_PLAN_SIL: {PlanId} numaralı günlük plan silindi.", planId);
    }

    public async Task<(int Plan, int Puantaj, int Hakedis)> AylikPlaniTemizleAsync(int yil, int ay)
    {
        if (ay is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(ay));

        var ayBaslangic = new DateTime(yil, ay, 1);
        var aySonu = ayBaslangic.AddMonths(1);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var strategy = db.Database.CreateExecutionStrategy();
        var sonuc = (Plan: 0, Puantaj: 0, Hakedis: 0);

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var planlar = await db.OperasyonPlanSatirlari
                .Where(p => p.Tarih >= ayBaslangic && p.Tarih < aySonu)
                .ToListAsync();
            var puantajIdleri = planlar
                .Where(p => p.FiloGunlukPuantajId.HasValue)
                .Select(p => p.FiloGunlukPuantajId!.Value)
                .Distinct()
                .ToList();
            var puantajlar = await db.Set<FiloGunlukPuantaj>()
                .Where(p => (puantajIdleri.Contains(p.Id) ||
                             (p.Tarih >= ayBaslangic && p.Tarih < aySonu &&
                              p.FiloGuzergahEslestirmeId.HasValue &&
                              planlar.Select(x => x.FiloGuzergahEslestirmeId)
                                  .Where(id => id > 0)
                                  .Contains(p.FiloGuzergahEslestirmeId.Value))) &&
                            !p.IsDeleted)
                .ToListAsync();

            if (puantajlar.Any(p => p.Onaylandi || p.KurumFaturaKesildiMi || p.TaseronOdemeYapildiMi || p.KurumFaturaId.HasValue))
                throw new InvalidOperationException("Seçili ayda onaylanmış, faturalanmış veya ödemesi yapılmış puantaj bulunduğu için plan temizlenemedi.");

            var puantajIds = puantajlar.Select(p => p.Id).ToList();
            var hakedisDetaylari = await db.Set<HakedisDetay>()
                .Where(d => puantajIds.Contains(d.FiloGunlukPuantajId ?? 0))
                .Include(d => d.Hakedis)
                .ToListAsync();

            if (hakedisDetaylari.Any(d => d.Hakedis is not null && d.Hakedis.Durum != HakedisDurum.Taslak && d.Hakedis.Durum != HakedisDurum.Iptal))
                throw new InvalidOperationException("Seçili ayda onaylanmış veya kapanmış hakediş bulunduğu için plan temizlenemedi.");

            var temizlenecekHakedisler = hakedisDetaylari
                .Select(d => d.Hakedis)
                .Where(h => h is not null)
                .Cast<Hakedis>()
                .DistinctBy(h => h.Id)
                .ToList();

            if (hakedisDetaylari.Count > 0)
                db.Set<HakedisDetay>().RemoveRange(hakedisDetaylari);
            if (temizlenecekHakedisler.Count > 0)
                db.Set<Hakedis>().RemoveRange(temizlenecekHakedisler);

            if (puantajlar.Count > 0)
                db.Set<FiloGunlukPuantaj>().RemoveRange(puantajlar);
            if (planlar.Count > 0)
                db.OperasyonPlanSatirlari.RemoveRange(planlar);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            sonuc = (planlar.Count, puantajlar.Count, temizlenecekHakedisler.Count);
        });

        _logger.LogWarning("AYLIK_PLAN_TEMIZLE: {Yil}/{Ay} plan={Plan}, puantaj={Puantaj}, hakediş={Hakedis} temizlendi.",
            yil, ay, sonuc.Plan, sonuc.Puantaj, sonuc.Hakedis);
        return sonuc;
    }

    public async Task<List<OperasyonPlanSatiri>> GetAylikPlanlarAsync(int yil, int ay)
    {
        if (ay is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(ay));

        var ayBaslangic = new DateTime(yil, ay, 1);
        var aySonu = ayBaslangic.AddMonths(1);
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.OperasyonPlanSatirlari
            .Where(p => p.Tarih >= ayBaslangic && p.Tarih < aySonu)
            .OrderBy(p => p.Tarih)
            .ThenBy(p => p.GuzergahId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<OperasyonTakvimGunu?> GetTakvimGunuAsync(DateTime tarih)
    {
        var gun = tarih.Date;
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.OperasyonTakvimGunleri
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Tarih == gun);
    }

    public async Task<int> PlanTeyitEtAsync(List<int> planIdleri)
    {
        if (planIdleri.Count == 0) return 0;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var strategy = db.Database.CreateExecutionStrategy();
        var teyit = 0;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var planlar = await db.OperasyonPlanSatirlari
                .Where(p => planIdleri.Contains(p.Id) && p.Durum == OperasyonPlanDurumu.Planlandi)
                .ToListAsync();

            teyit = 0;
            foreach (var plan in planlar)
            {
                var mevcutPuantaj = await db.Set<FiloGunlukPuantaj>()
                    .FirstOrDefaultAsync(p => p.Tarih.Date == plan.Tarih.Date &&
                                              plan.FiloGuzergahEslestirmeId > 0 &&
                                              p.FiloGuzergahEslestirmeId == plan.FiloGuzergahEslestirmeId &&
                                              !p.IsDeleted);

                if (mevcutPuantaj is not null)
                {
                    plan.Durum = OperasyonPlanDurumu.TeyitEdildi;
                    plan.FiloGunlukPuantajId = mevcutPuantaj.Id;
                    plan.TeyitTarihi = DateTime.UtcNow;
                    teyit++;
                    continue;
                }

                var puantaj = new FiloGunlukPuantaj
                {
                    Tarih = plan.Tarih,
                    FiloGuzergahEslestirmeId = plan.FiloGuzergahEslestirmeId > 0
                        ? plan.FiloGuzergahEslestirmeId
                        : null,
                    KurumFirmaId = plan.KurumFirmaId,
                    GuzergahId = plan.GuzergahId,
                    AracId = plan.AracId,
                    SoforId = plan.SoforId,
                    Durum = OperasyonDurumu.Gitti,
                    ServisTuru = plan.ServisTuru,
                    SeferSayisi = plan.PlanlananSefer,
                    PuantajCarpani = plan.PuantajCarpani,
                    TahakkukEdenKurumUcreti = plan.PlanlananSefer * plan.PuantajCarpani * plan.KurumSeferUcretiSnapshot,
                    TahakkukEdenTaseronUcreti = plan.PlanlananSefer * plan.PuantajCarpani * plan.TaseronSeferUcretiSnapshot,
                    FirmaId = plan.FirmaId
                };
                db.Set<FiloGunlukPuantaj>().Add(puantaj);
                await db.SaveChangesAsync();

                plan.Durum = OperasyonPlanDurumu.TeyitEdildi;
                plan.FiloGunlukPuantajId = puantaj.Id;
                plan.TeyitTarihi = DateTime.UtcNow;
                teyit++;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        _logger.LogInformation("PLAN_TEYIT: {Adet} plan teyit edilip günlük puantaja aktarıldı.", teyit);
        return teyit;
    }

    public async Task<List<OperasyonKontrat>> GetKontratlarAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.OperasyonKontratlar
            .Include(k => k.Fiyatlar)
            .AsNoTracking()
            .OrderByDescending(k => k.BaslangicTarihi)
            .ToListAsync();
    }

    public async Task KontratKaydetAsync(OperasyonKontrat kontrat)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (kontrat.Id == 0) db.OperasyonKontratlar.Add(kontrat);
        else { kontrat.UpdatedAt = DateTime.UtcNow; db.OperasyonKontratlar.Update(kontrat); }
        await db.SaveChangesAsync();
    }

    public async Task TakvimGunuKaydetAsync(OperasyonTakvimGunu gun)
    {
        gun.Tarih = gun.Tarih.Date;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var mevcut = await db.OperasyonTakvimGunleri.FirstOrDefaultAsync(t => t.Tarih == gun.Tarih);
        if (mevcut is null) db.OperasyonTakvimGunleri.Add(gun);
        else
        {
            mevcut.GunTipi = gun.GunTipi;
            mevcut.PuantajCarpani = gun.PuantajCarpani;
            mevcut.Aciklama = gun.Aciklama;
            mevcut.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }
}
