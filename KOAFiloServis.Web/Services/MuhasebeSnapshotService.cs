using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Snapshot → Muhasebe Fişi otomatik oluşturma servisi.
/// </summary>
public class MuhasebeSnapshotService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public MuhasebeSnapshotService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Kilitli snapshot'tan muhasebe fişi oluşturur.
    /// 770 BORÇ / 335 ALACAK (personel bazlı).
    /// Mükerrer fişi engeller.
    /// </summary>
    public async Task<MuhasebeFis?> CreateFromSnapshotAsync(int yil, int ay, int firmaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var snapshot = await context.MaasOdemeSnapshotlar
            .AsNoTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync();

        if (!snapshot.Any())
            throw new InvalidOperationException($"Snapshot bulunamadı. Yil={yil} Ay={ay} Firma={firmaId}");

        if (snapshot.Any(x => !x.Kilitli))
            throw new InvalidOperationException($"Snapshot kilitli değil, önce kilitleyin. Yil={yil} Ay={ay}");

        // Mükerrer fiş engelle
        var varMi = await context.MuhasebeFisleri
            .AsNoTracking()
            .AnyAsync(x => x.Kaynak == FisKaynak.Otomatik
                        && x.KaynakTip == "MaasSnapshot"
                        && x.FisTarihi.Year == yil && x.FisTarihi.Month == ay
                        && !x.IsDeleted);

        if (varMi)
        {
            Console.WriteLine($"[MuhasebeSnapshot] Fiş zaten var. Yil={yil} Ay={ay} Firma={firmaId}");
            return null;
        }

        var toplamOdenecek = snapshot.Sum(x => x.Odenecek);

        // 335 hesaplarını personelId bazlı çöz
        var personelHesapMap = new Dictionary<int, MuhasebeHesap>();
        foreach (var s in snapshot)
        {
            var hesapKodu = $"335.01.{s.PersonelId:D4}";
            var hesap = await context.MuhasebeHesaplari
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HesapKodu == hesapKodu && !h.IsDeleted);

            if (hesap == null)
            {
                // Deterministik olarak oluştur
                hesap = new MuhasebeHesap
                {
                    HesapKodu = hesapKodu,
                    HesapAdi = s.PersonelAdSoyad,
                    HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar,
                    HesapTuru = HesapTuru.Pasif,
                    AltHesapVar = false,
                    SistemHesabi = false,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeHesaplari.Add(hesap);
                await context.SaveChangesAsync();
            }

            personelHesapMap[s.PersonelId] = hesap;
        }

        // 770 hesap
        var hesap770 = await context.MuhasebeHesaplari
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HesapKodu == "770.01" && !h.IsDeleted);

        if (hesap770 == null)
        {
            hesap770 = new MuhasebeHesap
            {
                HesapKodu = "770.01",
                HesapAdi = "Genel Yönetim Giderleri",
                HesapGrubu = HesapGrubu.MaliyetHesaplari,
                HesapTuru = HesapTuru.Gider,
                AltHesapVar = false,
                SistemHesabi = true,
                Aktif = true,
                CreatedAt = DateTime.UtcNow
            };
            context.MuhasebeHesaplari.Add(hesap770);
            await context.SaveChangesAsync();
        }

        var fisNo = await GenerateNextMaasFisNoAsync(context, firmaId);

        var fis = new MuhasebeFis
        {
            FisNo = fisNo,
            FisTarihi = new DateTime(yil, ay, DateTime.DaysInMonth(yil, ay)),
            FisTipi = FisTipi.Mahsup,
            Aciklama = $"Maaş Ödeme Fişi — {ay:D2}/{yil}",
            Kaynak = FisKaynak.Otomatik,
            KaynakTip = "MaasSnapshot",
            Durum = FisDurum.Onaylandi,
            ToplamBorc = toplamOdenecek,
            ToplamAlacak = toplamOdenecek,
            CreatedAt = DateTime.UtcNow
        };

        // 770 BORÇ
        fis.Kalemler.Add(new MuhasebeFisKalem
        {
            HesapId = hesap770.Id,
            SiraNo = 1,
            Borc = toplamOdenecek,
            Alacak = 0,
            CreatedAt = DateTime.UtcNow
        });

        // 335 ALACAK — personel bazlı
        var sira = 2;
        foreach (var s in snapshot.OrderBy(x => x.PersonelAdSoyad))
        {
            if (!personelHesapMap.TryGetValue(s.PersonelId, out var hesap335))
                continue;

            fis.Kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = hesap335.Id,
                SiraNo = sira++,
                Borc = 0,
                Alacak = s.Odenecek,
                Aciklama = $"{s.PersonelAdSoyad} ({s.PersonelKodu})",
                CreatedAt = DateTime.UtcNow
            });
        }

        context.MuhasebeFisleri.Add(fis);
        await context.SaveChangesAsync();

        Console.WriteLine($"[MuhasebeSnapshot] Fiş oluşturuldu: {fisNo} Yil={yil} Ay={ay} Tutar={toplamOdenecek:N2} Personel={snapshot.Count}");

        return fis;
    }

    private static async Task<string> GenerateNextMaasFisNoAsync(ApplicationDbContext context, int firmaId)
    {
        var prefix = $"MAS-{firmaId}-";
        var yil = DateTime.Now.Year;

        var sonFis = await context.MuhasebeFisleri
            .AsNoTracking()
            .Where(x => x.FisNo.StartsWith(prefix))
            .OrderByDescending(x => x.FisNo)
            .Select(x => x.FisNo)
            .FirstOrDefaultAsync();

        if (sonFis == null)
            return $"{prefix}{yil}-0001";

        var parts = sonFis.Split('-');
        if (parts.Length == 4 && int.TryParse(parts[3], out var numara))
            return $"{prefix}{yil}-{numara + 1:D4}";

        return $"{prefix}{yil}-0001";
    }
}
