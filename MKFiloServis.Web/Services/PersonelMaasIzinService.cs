using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class PersonelMaasIzinService : IPersonelMaasIzinService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PersonelMaasIzinService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Maa� ��lemleri

    public async Task<List<PersonelMaas>> GetMaaslarAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelMaaslari
            .Include(m => m.Sofor)
            .Where(m => m.Yil == yil && m.Ay == ay && !m.IsDeleted)
            .OrderBy(m => m.Sofor.Ad)
            .ToListAsync();
    }

    public async Task<PersonelMaas?> GetMaasByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelMaaslari
            .Include(m => m.Sofor)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<PersonelMaas?> GetMaasBySoforAsync(int soforId, int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelMaaslari
            .Include(m => m.Sofor)
            .FirstOrDefaultAsync(m => m.SoforId == soforId && m.Yil == yil && m.Ay == ay && !m.IsDeleted);
    }

    public async Task<PersonelMaas> CreateMaasAsync(PersonelMaas maas)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.PersonelMaaslari.Add(maas);
        await context.SaveChangesAsync();
        return maas;
    }

    public async Task<PersonelMaas> UpdateMaasAsync(PersonelMaas maas)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PersonelMaaslari.FindAsync(maas.Id);
        if (existing == null)
            throw new InvalidOperationException($"PersonelMaas bulunamadı: {maas.Id}");

        // 🔴 Fetch + map + SaveChanges — Attach/Modified KULLANMA
        context.Entry(existing).CurrentValues.SetValues(maas);
        existing.UpdatedAt = DateTime.UtcNow;
        // Navigation property'leri etkileme
        context.Entry(existing).Reference(x => x.Sofor).IsModified = false;
        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteMaasAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var maas = await context.PersonelMaaslari.FindAsync(id);
        if (maas != null)
        {
            context.PersonelMaaslari.Remove(maas);
            await context.SaveChangesAsync();
        }
    }

    public async Task<int> DeleteMaaslarAsync(List<int> maasIds)
    {
        if (maasIds == null || !maasIds.Any()) return 0;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await context.PersonelMaaslari
            .Where(m => maasIds.Contains(m.Id))
            .ToListAsync();

        if (!kayitlar.Any()) return 0;

        context.PersonelMaaslari.RemoveRange(kayitlar);
        await context.SaveChangesAsync();
        return kayitlar.Count;
    }

    public async Task<int> RecalculateMaaslarAsync(List<int> maasIds)
    {
        if (maasIds == null || !maasIds.Any()) return 0;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await context.PersonelMaaslari
            .Include(m => m.Sofor)
            .Where(m => maasIds.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync();

        foreach (var maas in kayitlar)
        {
            var sofor = maas.Sofor;
            if (sofor == null) continue;

            maas.BrutMaas = sofor.BrutMaas;
            maas.NetMaas = sofor.ResmiNetMaas > 0 ? sofor.ResmiNetMaas : sofor.NetMaas;

            // Eğer kayıtta başka ekleme yoksa personel kartındaki diğer maaşı taşı.
            if (maas.ToplamEklemeler <= 0 && sofor.DigerMaas > 0)
            {
                maas.DigerEklemeler = sofor.DigerMaas;
            }

            maas.SGKIsciPayi = maas.BrutMaas * 0.14m;
            maas.SGKIsverenPayi = maas.BrutMaas * 0.205m;
            maas.IssizlikPrimi = maas.BrutMaas * 0.01m;
            maas.DamgaVergisi = maas.BrutMaas * 0.00759m;

            var vergiMatrahi = maas.BrutMaas - maas.SGKIsciPayi - maas.IssizlikPrimi;
            maas.GelirVergisi = vergiMatrahi * 0.15m;
            maas.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return kayitlar.Count;
    }

    public async Task<MaasOlusturmaSonuc> CreateMaasForPersonellerAsync(int yil, int ay, List<int> soforIds)
    {
        var sonuc = new MaasOlusturmaSonuc();
        if (soforIds == null || !soforIds.Any()) return sonuc;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var benzersizSoforIds = soforIds.Distinct().ToList();

        var mevcutSoforIds = await context.PersonelMaaslari
            .Where(m => m.Yil == yil && m.Ay == ay && benzersizSoforIds.Contains(m.SoforId) && !m.IsDeleted)
            .Select(m => m.SoforId)
            .Distinct()
            .ToListAsync();

        sonuc.ZatenVarSayisi = mevcutSoforIds.Count;

        var olusturulacakSoforler = await context.Soforler
            .Where(s => s.Aktif && (s.IstenAyrilmaTarihi == null || s.IstenAyrilmaTarihi >= new DateTime(yil, ay, 1)) && benzersizSoforIds.Contains(s.Id) && !mevcutSoforIds.Contains(s.Id))
            .ToListAsync();

        foreach (var sofor in olusturulacakSoforler)
        {
            var yeniMaas = BuildDefaultMaasFromSofor(sofor, yil, ay);
            context.PersonelMaaslari.Add(yeniMaas);
        }

        await context.SaveChangesAsync();
        sonuc.OlusturulanSayisi = olusturulacakSoforler.Count;
        return sonuc;
    }

    public async Task<List<PersonelMaas>> GetSoforMaasGecmisiAsync(int soforId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelMaaslari
            .Include(m => m.Sofor)
            .Where(m => m.SoforId == soforId && !m.IsDeleted)
            .OrderByDescending(m => m.Yil)
            .ThenByDescending(m => m.Ay)
            .ToListAsync();
    }

    public async Task MaasOdemeYapAsync(int maasId, DateTime odemeTarihi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var maas = await context.PersonelMaaslari.FindAsync(maasId);
        if (maas != null)
        {
            maas.OdemeTarihi = odemeTarihi;
            maas.OdemeDurum = MaasOdemeDurum.Odendi;
            await context.SaveChangesAsync();
        }
    }

    public async Task TopluMaasOlusturAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var donemBaslangic = new DateTime(yil, ay, 1);
        var aktifSoforler = await context.Soforler
            .Where(s => s.Aktif && (s.IstenAyrilmaTarihi == null || s.IstenAyrilmaTarihi >= donemBaslangic))
            .ToListAsync();

        foreach (var sofor in aktifSoforler)
        {
            var mevcutMaas = await GetMaasBySoforAsync(sofor.Id, yil, ay);
            if (mevcutMaas == null)
            {
                var yeniMaas = BuildDefaultMaasFromSofor(sofor, yil, ay);
                context.PersonelMaaslari.Add(yeniMaas);
            }
        }

        await context.SaveChangesAsync();
    }

    private static PersonelMaas BuildDefaultMaasFromSofor(Sofor sofor, int yil, int ay)
    {
        var yeniMaas = new PersonelMaas
        {
            SoforId = sofor.Id,
            Yil = yil,
            Ay = ay,
            BrutMaas = sofor.BrutMaas,
            NetMaas = sofor.ResmiNetMaas > 0 ? sofor.ResmiNetMaas : sofor.NetMaas,
            DigerEklemeler = sofor.DigerMaas,
            CalismaGunu = 26,
            OdemeDurum = MaasOdemeDurum.Bekliyor
        };

        yeniMaas.SGKIsciPayi = yeniMaas.BrutMaas * 0.14m;
        yeniMaas.SGKIsverenPayi = yeniMaas.BrutMaas * 0.205m;
        yeniMaas.IssizlikPrimi = yeniMaas.BrutMaas * 0.01m;
        yeniMaas.DamgaVergisi = yeniMaas.BrutMaas * 0.00759m;

        var vergiMatrahi = yeniMaas.BrutMaas - yeniMaas.SGKIsciPayi - yeniMaas.IssizlikPrimi;
        yeniMaas.GelirVergisi = vergiMatrahi * 0.15m;

        return yeniMaas;
    }

    #endregion

    #region �zin ��lemleri

    public async Task<List<PersonelIzin>> GetIzinlerAsync(int? soforId = null, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.PersonelIzinleri
            .Include(i => i.Sofor)
            .AsQueryable();

        if (soforId.HasValue)
            query = query.Where(i => i.SoforId == soforId.Value);

        if (baslangic.HasValue)
            query = query.Where(i => i.BitisTarihi >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(i => i.BaslangicTarihi <= bitis.Value);

        return await query.OrderByDescending(i => i.BaslangicTarihi).ToListAsync();
    }

    public async Task<PersonelIzin?> GetIzinByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelIzinleri
            .Include(i => i.Sofor)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<PersonelIzin> CreateIzinAsync(PersonelIzin izin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.PersonelIzinleri.Add(izin);
        await context.SaveChangesAsync();

        // Yıllık izinse, izin hakkından düş
        if (izin.IzinTipi == IzinTipi.YillikIzin && izin.Durum == IzinDurum.Onaylandi)
        {
            await UpdateIzinHakkiKullanimAsync(context, izin.SoforId, izin.BaslangicTarihi.Year, izin.ToplamGun);
        }

        return izin;
    }

    public async Task<PersonelIzin> UpdateIzinAsync(PersonelIzin izin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PersonelIzinleri.FindAsync(izin.Id);
        if (existing == null)
            throw new InvalidOperationException($"PersonelIzin bulunamadı: {izin.Id}");

        // 🔴 Fetch + map + SaveChanges — Update() KULLANMA
        context.Entry(existing).CurrentValues.SetValues(izin);
        existing.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteIzinAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var izin = await context.PersonelIzinleri.FindAsync(id);
        if (izin != null)
        {
            izin.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task IzinOnaylaAsync(int izinId, string onaylayanKisi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var izin = await context.PersonelIzinleri.FindAsync(izinId);
        if (izin != null)
        {
            izin.Durum = IzinDurum.Onaylandi;
            izin.OnaylayanKisi = onaylayanKisi;
            izin.OnayTarihi = DateTime.Now;

            // Yıllık izinse kullanımı güncelle
            if (izin.IzinTipi == IzinTipi.YillikIzin)
            {
                await UpdateIzinHakkiKullanimAsync(context, izin.SoforId, izin.BaslangicTarihi.Year, izin.ToplamGun);
            }

            await context.SaveChangesAsync();
        }
    }

    public async Task IzinReddetAsync(int izinId, string redNedeni)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var izin = await context.PersonelIzinleri.FindAsync(izinId);
        if (izin != null)
        {
            izin.Durum = IzinDurum.Reddedildi;
            izin.RedNedeni = redNedeni;
            await context.SaveChangesAsync();
        }
    }

    private async Task UpdateIzinHakkiKullanimAsync(ApplicationDbContext context, int soforId, int yil, int gun)
    {
        var izinHakki = await GetIzinHakkiAsync(soforId, yil);
        if (izinHakki != null)
        {
            izinHakki.KullanilanIzin += gun;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region İzin Hakkı İşlemleri

    public async Task<PersonelIzinHakki?> GetIzinHakkiAsync(int soforId, int yil)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelIzinHaklari
            .Include(h => h.Sofor)
            .FirstOrDefaultAsync(h => h.SoforId == soforId && h.Yil == yil);
    }

    public async Task<PersonelIzinHakki> CreateOrUpdateIzinHakkiAsync(PersonelIzinHakki izinHakki)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await GetIzinHakkiAsync(izinHakki.SoforId, izinHakki.Yil);
        if (mevcut == null)
        {
            context.PersonelIzinHaklari.Add(izinHakki);
        }
        else
        {
            mevcut.YillikIzinHakki = izinHakki.YillikIzinHakki;
            mevcut.DevirenIzin = izinHakki.DevirenIzin;
            mevcut.Notlar = izinHakki.Notlar;
        }
        await context.SaveChangesAsync();
        return izinHakki;
    }

    public async Task YillikIzinHaklariOlusturAsync(int yil)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var donemBaslangic = new DateTime(yil, 1, 1);
        var aktifSoforler = await context.Soforler
            .Where(s => s.Aktif && (s.IstenAyrilmaTarihi == null || s.IstenAyrilmaTarihi >= donemBaslangic))
            .ToListAsync();

        foreach (var sofor in aktifSoforler)
        {
            var mevcutHak = await GetIzinHakkiAsync(sofor.Id, yil);
            if (mevcutHak == null)
            {
                // Önceki yıldan devreden izin
                var oncekiYilHak = await GetIzinHakkiAsync(sofor.Id, yil - 1);
                var devirenIzin = oncekiYilHak?.KalanIzin ?? 0;

                // Kıdem yılına göre izin hakkı hesapla
                var kidemYili = sofor.IseBaslamaTarihi.HasValue 
                    ? (yil - sofor.IseBaslamaTarihi.Value.Year) 
                    : 0;

                var yillikHak = kidemYili switch
                {
                    < 1 => 14,
                    < 5 => 14,
                    < 15 => 20,
                    _ => 26
                };

                var yeniHak = new PersonelIzinHakki
                {
                    SoforId = sofor.Id,
                    Yil = yil,
                    YillikIzinHakki = yillikHak,
                    DevirenIzin = devirenIzin,
                    KullanilanIzin = 0
                };

                context.PersonelIzinHaklari.Add(yeniHak);
            }
        }

        await context.SaveChangesAsync();
    }

    #endregion

    #region Raporlar

    public async Task<MaasRaporOzet> GetMaasRaporuAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var maaslar = await GetMaaslarAsync(yil, ay);

        var ozet = new MaasRaporOzet
        {
            Yil = yil,
            Ay = ay,
            PersonelSayisi = maaslar.Count,
            ToplamBrutMaas = maaslar.Sum(m => m.BrutMaas),
            ToplamNetMaas = maaslar.Sum(m => m.NetMaas),
            ToplamSGKIsci = maaslar.Sum(m => m.SGKIsciPayi),
            ToplamSGKIsveren = maaslar.Sum(m => m.SGKIsverenPayi),
            ToplamGelirVergisi = maaslar.Sum(m => m.GelirVergisi),
            ToplamOdeme = maaslar.Sum(m => m.OdenecekTutar),
            OdenmeyenSayisi = maaslar.Count(m => m.OdemeDurum != MaasOdemeDurum.Odendi),
            Detaylar = maaslar.Select(m => new MaasDetay
            {
                MaasId = m.Id,
                SoforId = m.SoforId,
                SoforAdSoyad = m.Sofor.TamAd,
                BrutMaas = m.BrutMaas,
                NetMaas = m.NetMaas,
                SGKIsciPayi = m.SGKIsciPayi,
                GelirVergisi = m.GelirVergisi,
                DamgaVergisi = m.DamgaVergisi,
                ToplamEklemeler = m.ToplamEklemeler > 0 ? m.ToplamEklemeler : (m.Sofor.DigerMaas > 0 ? m.Sofor.DigerMaas : 0),
                DigerEklemeler = m.DigerEklemeler > 0 ? m.DigerEklemeler : (m.Sofor.DigerMaas > 0 ? m.Sofor.DigerMaas : 0),
                Avans = m.Avans,
                ToplamKesintiler = m.ToplamKesintiler,
                OdenecekTutar = m.OdenecekTutar,
                OdemeDurum = m.OdemeDurum.ToString(),
                OdemeTarihi = m.OdemeTarihi
            }).ToList()
        };

        return ozet;
    }

    public async Task<IzinRaporOzet> GetIzinRaporuAsync(int yil)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var izinHaklari = await context.PersonelIzinHaklari
            .Include(h => h.Sofor)
            .Where(h => h.Yil == yil)
            .ToListAsync();

        var izinler = await context.PersonelIzinleri
            .Include(i => i.Sofor)
            .Where(i => i.BaslangicTarihi.Year == yil && i.Durum == IzinDurum.Onaylandi)
            .ToListAsync();

        var ozet = new IzinRaporOzet
        {
            Yil = yil,
            ToplamPersonel = izinHaklari.Count,
            ToplamKullanilanIzin = izinHaklari.Sum(h => h.KullanilanIzin),
            ToplamKalanIzin = izinHaklari.Sum(h => h.KalanIzin),
            Detaylar = izinHaklari.Select(h => new IzinDetay
            {
                SoforId = h.SoforId,
                SoforAdSoyad = h.Sofor.TamAd,
                YillikHak = h.YillikIzinHakki,
                DevirenIzin = h.DevirenIzin,
                KullanilanIzin = h.KullanilanIzin,
                KalanIzin = h.KalanIzin,
                IzinKayitlari = izinler.Where(i => i.SoforId == h.SoforId).ToList()
            }).ToList(),
            TipBazliOzet = izinler
                .GroupBy(i => i.IzinTipi)
                .Select(g => new IzinTipiOzet
                {
                    IzinTipi = g.Key,
                    IzinTipiAdi = GetIzinTipiAdi(g.Key),
                    Adet = g.Count(),
                    ToplamGun = g.Sum(i => i.ToplamGun)
                }).ToList()
        };

        return ozet;
    }

    public async Task<List<PersonelOzet>> GetPersonelOzetListesiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var buAy = DateTime.Today.Month;
        var buYil = DateTime.Today.Year;

        var donemBaslangic = new DateTime(buYil, buAy, 1);
        var soforler = await context.Soforler
            .Where(s => s.Aktif && (s.IstenAyrilmaTarihi == null || s.IstenAyrilmaTarihi >= donemBaslangic))
            .ToListAsync();

        var izinHaklari = await context.PersonelIzinHaklari
            .Where(h => h.Yil == buYil)
            .ToListAsync();

        var seferler = await context.ServisCalismalari
            .Where(s => s.CalismaTarihi.Month == buAy && s.CalismaTarihi.Year == buYil)
            .GroupBy(s => s.SoforId)
            .Select(g => new { SoforId = g.Key, Sayi = g.Count() })
            .ToListAsync();

        return soforler.Select(s => new PersonelOzet
        {
            SoforId = s.Id,
            SoforKodu = s.SoforKodu,
            AdSoyad = s.TamAd,
            IseBaslamaTarihi = s.IseBaslamaTarihi,
            BrutMaas = s.BrutMaas,
            NetMaas = s.NetMaas,
            KalanIzin = izinHaklari.FirstOrDefault(h => h.SoforId == s.Id)?.KalanIzin ?? 0,
            BuAySeferSayisi = seferler.FirstOrDefault(x => x.SoforId == s.Id)?.Sayi ?? 0,
            Aktif = s.Aktif
        }).ToList();
    }

    private string GetIzinTipiAdi(IzinTipi tip)
    {
        return tip switch
        {
            IzinTipi.YillikIzin => "Yıllık İzin",
            IzinTipi.UcretsizIzin => "Ücretsiz İzin",
            IzinTipi.RaporluIzin => "Raporlu İzin",
            IzinTipi.MazeretIzni => "Mazeret İzni",
            IzinTipi.EvlilikIzni => "Evlilik İzni",
            IzinTipi.DogumIzni => "Doğum İzni",
            IzinTipi.OlumIzni => "Ölüm İzni",
            IzinTipi.IdariIzin => "İdari İzin",
            _ => tip.ToString()
        };
    }

    #endregion
}



