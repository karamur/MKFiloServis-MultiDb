using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Services.Destek;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>İstatistikler, raporlar ve zamanlanmış işler (SLA kontrolü, otomatik kapatma).</summary>
public partial class DestekTalebiService
{
    private static readonly TimeZoneInfo IsGunuSaatDilimi = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public async Task<DestekDashboardStats> GetDashboardStatsAsync()
    {
        var aktor = await EkipUyesiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kapsam = EkipKapsami(context.DestekTalepleri.AsNoTracking(), aktor, await YazilimDepartmanIdAsync(context));

        var acik = DestekKurallari.AcikDurumlar.ToArray();
        var (gunBasi, gunSonu) = GunAraligiUtc(BusinessTime.Today);
        var stats = new DestekDashboardStats
        {
            ToplamTalepSayisi = await kapsam.CountAsync(),
            AcikTalepSayisi = await kapsam.CountAsync(x => acik.Contains(x.Durum)),
            OnayBekleyenTalepSayisi = await kapsam.CountAsync(x => x.Durum == DestekDurum.Cozuldu),
            BugunAcilanTalepSayisi = await kapsam.CountAsync(x => x.CreatedAt >= gunBasi && x.CreatedAt < gunSonu),
            BugunKapatilanTalepSayisi = await kapsam.CountAsync(x => x.KapatilmaTarihi >= gunBasi && x.KapatilmaTarihi < gunSonu),
            SlaAsildiSayisi = await kapsam.CountAsync(x => x.SlaAsildi && acik.Contains(x.Durum))
        };

        stats.OncelikDagilimi = (await kapsam.Where(x => acik.Contains(x.Durum))
                .GroupBy(x => x.Oncelik).Select(g => new { g.Key, Sayi = g.Count() }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.Sayi);
        stats.DurumDagilimi = (await kapsam
                .GroupBy(x => x.Durum).Select(g => new { g.Key, Sayi = g.Count() }).ToListAsync())
            .GroupBy(x => DestekKurallari.Normallestir(x.Key))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Sayi));
        stats.DepartmanDagilimi = (await kapsam.Where(x => acik.Contains(x.Durum))
                .GroupBy(x => x.Departman.Ad).Select(g => new { g.Key, Sayi = g.Count() }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.Sayi);
        stats.KaynakDagilimi = (await kapsam
                .GroupBy(x => x.Kaynak).Select(g => new { g.Key, Sayi = g.Count() }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.Sayi);

        stats.SonTalepler = await kapsam
            .Include(x => x.Departman)
            .Include(x => x.AtananKullanici)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();
        Normallestir(stats.SonTalepler);

        // Ortalamalar yalnız değeri olan kayıtlardan; hiç kayıt yoksa null (kart "—" gösterir).
        var sureler = await kapsam
            .Where(x => x.IlkYanitSuresiDakika != null || x.CozumSuresiDakika != null || x.MemnuniyetPuani != null)
            .Select(x => new { x.IlkYanitSuresiDakika, x.CozumSuresiDakika, x.MemnuniyetPuani })
            .ToListAsync();
        stats.OrtalamaIlkYanitSuresiSaat = Ortalama(sureler.Select(x => x.IlkYanitSuresiDakika)) / 60.0;
        stats.OrtalamaCozumSuresiSaat = Ortalama(sureler.Select(x => x.CozumSuresiDakika)) / 60.0;
        stats.OrtalamaMemuniyetPuani = Ortalama(sureler.Select(x => x.MemnuniyetPuani));

        return stats;
    }

    public async Task<DestekRaporStats> GetRaporStatsAsync(DateTime baslangic, DateTime bitis, int? departmanId = null)
    {
        var aktor = await OturumGerekliAsync();
        if (!aktor.Personel) throw YetkiYok();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.DestekTalepleri.AsNoTracking().Where(x => x.Durum != DestekDurum.Taslak);
        if (departmanId.HasValue)
            query = query.Where(x => x.DepartmanId == departmanId.Value);

        // Tek sorgu: aralıkta açılan ya da kapanan talepler; gün kırılımı bellekte (iş günü saatine göre).
        var satirlar = await query
            .Where(x => (x.CreatedAt >= baslangic && x.CreatedAt <= bitis) ||
                        (x.KapatilmaTarihi >= baslangic && x.KapatilmaTarihi <= bitis))
            .Select(x => new { x.CreatedAt, x.KapatilmaTarihi, x.SlaAsildi, x.IlkYanitSuresiDakika, x.CozumSuresiDakika, x.MemnuniyetPuani })
            .ToListAsync();

        var acilan = satirlar.Where(x => x.CreatedAt >= baslangic && x.CreatedAt <= bitis).ToList();
        var kapanan = satirlar.Where(x => x.KapatilmaTarihi >= baslangic && x.KapatilmaTarihi <= bitis).ToList();
        var stats = new DestekRaporStats
        {
            BaslangicTarihi = baslangic,
            BitisTarihi = bitis,
            ToplamAcilanTalep = acilan.Count,
            ToplamKapatilanTalep = kapanan.Count,
            ToplamSlaAsimi = acilan.Count(x => x.SlaAsildi),
            OrtalamaIlkYanitSuresiDakika = Ortalama(kapanan.Select(x => x.IlkYanitSuresiDakika)) ?? 0,
            OrtalamaCozumSuresiDakika = Ortalama(kapanan.Select(x => x.CozumSuresiDakika)) ?? 0,
            OrtalamaMemuniyetPuani = Ortalama(kapanan.Select(x => x.MemnuniyetPuani)) ?? 0
        };

        var acilanGun = acilan.GroupBy(x => BusinessTime.DateAt(x.CreatedAt)).ToDictionary(g => g.Key, g => g.Count());
        var kapananGun = kapanan.GroupBy(x => BusinessTime.DateAt(x.KapatilmaTarihi!.Value)).ToDictionary(g => g.Key, g => g.Count());
        for (var gun = BusinessTime.DateAt(baslangic); gun <= BusinessTime.DateAt(bitis); gun = gun.AddDays(1))
        {
            stats.GunlukTrend.Add(new GunlukTalepTrend
            {
                Tarih = gun,
                AcilanTalep = acilanGun.GetValueOrDefault(gun),
                KapatilanTalep = kapananGun.GetValueOrDefault(gun)
            });
        }

        return stats;
    }

    public async Task<List<DestekPerformansRapor>> GetPersonelPerformansRaporuAsync(DateTime baslangic, DateTime bitis, int? departmanId = null)
    {
        var aktor = await OturumGerekliAsync();
        if (!aktor.Personel) throw YetkiYok();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.DestekTalepleri
            .AsNoTracking()
            .Where(x => x.AtananKullaniciId.HasValue && x.CreatedAt >= baslangic && x.CreatedAt <= bitis);
        if (departmanId.HasValue)
            query = query.Where(x => x.DepartmanId == departmanId.Value);

        var satirlar = await query
            .Select(x => new
            {
                KullaniciId = x.AtananKullaniciId!.Value,
                KullaniciAdi = x.AtananKullanici!.AdSoyad,
                x.Durum,
                x.SlaAsildi,
                x.IlkYanitSuresiDakika,
                x.CozumSuresiDakika,
                x.MemnuniyetPuani
            })
            .ToListAsync();

        var yanitSayilari = await context.DestekTalebiYanitlari
            .AsNoTracking()
            .Where(x => x.KullaniciId.HasValue && x.CreatedAt >= baslangic && x.CreatedAt <= bitis && !x.MusteriYaniti && !x.DahiliNot)
            .GroupBy(x => x.KullaniciId!.Value)
            .Select(g => new { KullaniciId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.KullaniciId, x => x.Sayi);

        return satirlar
            .GroupBy(x => new { x.KullaniciId, x.KullaniciAdi })
            .Select(g => new DestekPerformansRapor
            {
                KullaniciId = g.Key.KullaniciId,
                KullaniciAdi = g.Key.KullaniciAdi,
                AtananTalepSayisi = g.Count(),
                CozulenTalepSayisi = g.Count(x => DestekKurallari.Normallestir(x.Durum) is DestekDurum.Cozuldu or DestekDurum.Kapali),
                SlaAsimSayisi = g.Count(x => x.SlaAsildi),
                OrtalamaYanitSuresiDakika = Ortalama(g.Select(x => x.IlkYanitSuresiDakika)) ?? 0,
                OrtalamaCozumSuresiDakika = Ortalama(g.Select(x => x.CozumSuresiDakika)) ?? 0,
                OrtalamaMemuniyetPuani = Ortalama(g.Select(x => x.MemnuniyetPuani)) ?? 0,
                ToplamYanitSayisi = yanitSayilari.GetValueOrDefault(g.Key.KullaniciId)
            })
            .OrderByDescending(x => x.AtananTalepSayisi)
            .ToList();
    }

    public async Task<int> CheckAndUpdateSlaViolationsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (!(await AyarlariOkuAsync(context)).OtomatikSlaKontrolu) return 0;

        var simdi = DateTime.UtcNow;
        var durumlar = DestekKurallari.SlaIsleyenDurumlar.ToArray();
        var asilanlar = await context.DestekTalepleri
            .Where(x => !x.SlaAsildi && x.SlaBitisTarihi != null && x.SlaBitisTarihi < simdi && durumlar.Contains(x.Durum))
            .ToListAsync();
        if (asilanlar.Count == 0) return 0;

        foreach (var talep in asilanlar)
        {
            talep.SlaAsildi = true;
            talep.UpdatedAt = simdi;
            Aktivite(context, talep, AktiviteTuru.SlaBilgilendirme, "SLA çözüm süresi aşıldı", null);
        }
        await context.SaveChangesAsync();

        await BildirAsync(context, async () =>
        {
            foreach (var talep in asilanlar)
                await DestekBildirimci.EkleAsync(context, await EkipIlgilileriAsync(context, talep), null, talep,
                    "SLA süresi aşıldı", talep.Konu, ekipLinki: true, BildirimOncelik.Yuksek);
        });

        return asilanlar.Count;
    }

    public async Task<int> CozulenTalepleriOtomatikKapatAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var gun = (await AyarlariOkuAsync(context)).OtomatikKapatmaGun;
        if (gun <= 0) return 0;

        var simdi = DateTime.UtcNow;
        var sinir = simdi.AddDays(-gun);
        var talepler = await context.DestekTalepleri
            .Where(x => x.Durum == DestekDurum.Cozuldu && x.SonAktiviteTarihi < sinir)
            .ToListAsync();
        if (talepler.Count == 0) return 0;

        var mesaj = $"Talep, çözüldükten sonra {gun} gün içinde yanıt gelmediği için otomatik kapatıldı.";
        foreach (var talep in talepler)
        {
            talep.Durum = DestekDurum.Kapali;
            talep.KapatilmaTarihi = simdi;
            talep.SonAktiviteTarihi = simdi;
            talep.UpdatedAt = simdi;
            context.DestekTalebiYanitlari.Add(new DestekTalebiYanit
            {
                DestekTalebi = talep,
                Icerik = mesaj,
                YanitTuru = YanitTuru.SistemMesaji,
                CreatedAt = simdi
            });
            Aktivite(context, talep, AktiviteTuru.Kapandi, "Talep otomatik kapatıldı", null);
            Aktivite(context, talep, AktiviteTuru.DurumDegisti, $"Durum: {DestekKurallari.Etiket(DestekDurum.Cozuldu)} → {DestekKurallari.Etiket(DestekDurum.Kapali)}",
                null, DestekKurallari.Etiket(DestekDurum.Cozuldu), DestekKurallari.Etiket(DestekDurum.Kapali));
        }
        await context.SaveChangesAsync();

        await BildirAsync(context, async () =>
        {
            foreach (var talep in talepler)
                await DestekBildirimci.EkleAsync(context, [talep.OlusturanKullaniciId], null, talep,
                    "Talebiniz kapatıldı", mesaj, ekipLinki: false);
        });

        return talepler.Count;
    }

    private static double? Ortalama(IEnumerable<int?> degerler)
    {
        var liste = degerler.Where(d => d.HasValue).Select(d => (double)d!.Value).ToList();
        return liste.Count == 0 ? null : liste.Average();
    }

    /// <summary>İş günü (Türkiye saati) takvim gününün UTC karşılığı: [başlangıç, bitiş).</summary>
    private static (DateTime Baslangic, DateTime Bitis) GunAraligiUtc(DateTime yerelGun)
    {
        var gun = DateTime.SpecifyKind(yerelGun.Date, DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(gun, IsGunuSaatDilimi), TimeZoneInfo.ConvertTimeToUtc(gun.AddDays(1), IsGunuSaatDilimi));
    }
}
