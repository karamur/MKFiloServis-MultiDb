using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Snapshot → Muhasebe Fişi otomatik oluşturma servisi.
/// </summary>
public class MuhasebeSnapshotService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IAktifFirmaProvider _firmaProvider;

    public MuhasebeSnapshotService(IDbContextFactory<ApplicationDbContext> factory, IAktifFirmaProvider firmaProvider)
    {
        _factory = factory;
        _firmaProvider = firmaProvider;
    }

    /// <summary>
    /// Kilitli snapshot'tan muhasebe fişi oluşturur.
    /// 770 BORÇ / 335 ALACAK (personel bazlı).
    /// Mükerrer fişi engeller.
    /// </summary>
    public Task<MuhasebeFis?> CreateFromSnapshotAsync(int yil, int ay, int firmaId)
        => KayitTransactionAsync(yil, ay, firmaId, context => CreateFromSnapshotAsyncCore(context, yil, ay, firmaId));

    private static async Task<MuhasebeFis?> CreateFromSnapshotAsyncCore(ApplicationDbContext context, int yil, int ay, int firmaId)
    {

        var snapshot = await context.MaasOdemeSnapshotlar
            .AsTracking()
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
                        && x.FisNo.StartsWith($"MAS-{firmaId}-")
                        && x.FisTarihi.Year == yil && x.FisTarihi.Month == ay
                        && !x.IsDeleted);

        if (varMi)
        {
            // Fiş zaten var
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

        // Snapshot bağlantıları da audit ve üst transaction kapsamındadır.
        foreach (var kayit in snapshot)
        {
            kayit.MuhasebeFisId = fis.Id;
            kayit.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();

        // Fiş oluşturuldu

        return fis;
    }

    /// <summary>
    /// Ters fiş oluşturur — maaş tahakkukunu iptal eder.
    /// Asla kayıt silinmez, sadece ters kayıt yapılır.
    /// </summary>
    public Task<MuhasebeFis?> ReverseSnapshotAsync(int yil, int ay, int firmaId)
        => KayitTransactionAsync(yil, ay, firmaId, context => ReverseSnapshotAsyncCore(context, yil, ay, firmaId));

    private static async Task<MuhasebeFis?> ReverseSnapshotAsyncCore(ApplicationDbContext context, int yil, int ay, int firmaId)
    {

        var snapshot = await context.MaasOdemeSnapshotlar
            .AsTracking()
            .Where(x => x.Yil == yil && x.Ay == ay && x.FirmaId == firmaId && !x.IsDeleted)
            .ToListAsync();

        if (!snapshot.Any())
            throw new InvalidOperationException($"Snapshot bulunamadı. Yil={yil} Ay={ay} Firma={firmaId}");

        if (!snapshot.All(x => x.Kilitli))
            throw new InvalidOperationException($"Snapshot kilitli değil. Yil={yil} Ay={ay}");

        if (snapshot.All(x => x.MuhasebeFisId == null))
            throw new InvalidOperationException($"Henüz muhasebe fişi oluşturulmamış. Yil={yil} Ay={ay}");

        if (snapshot.Any(x => x.IptalFisId != null))
        {
            // Zaten iptal edilmiş
            return null;
        }

        if (snapshot.Any(x => !x.MuhasebeFisId.HasValue) || snapshot.Select(x => x.MuhasebeFisId).Distinct().Count() != 1)
            throw new InvalidOperationException("Snapshot muhasebe fişi bağlantıları tutarsız; iptal öncesinde düzeltilmelidir.");
        var orijinalFisId = snapshot.First().MuhasebeFisId!.Value;
        var orijinalFis = await context.MuhasebeFisleri
            .Include(x => x.Kalemler)
            .FirstOrDefaultAsync(x => x.Id == orijinalFisId && !x.IsDeleted && x.KaynakTip == "MaasSnapshot" && x.FisNo.StartsWith($"MAS-{firmaId}-"))
            ?? throw new InvalidOperationException("Snapshot kaynak muhasebe fişi erişilebilir değil veya firma bağlantısı tutarsız.");

        var toplamOdenecek = snapshot.Sum(x => x.Odenecek);
        var fisNo = await GenerateNextMaasFisNoAsync(context, firmaId);

        var reverseFis = new MuhasebeFis
        {
            FisNo = fisNo,
            FisTarihi = MKFiloServis.Shared.Time.BusinessTime.Today,
            FisTipi = FisTipi.Mahsup,
            Aciklama = $"Maaş İptal Fişi — {ay:D2}/{yil} (Ters Kayıt: {orijinalFis.FisNo})",
            Kaynak = FisKaynak.Otomatik,
            KaynakTip = "MaasSnapshotReverse",
            KaynakId = orijinalFisId,
            Durum = FisDurum.Onaylandi,
            ToplamBorc = toplamOdenecek,
            ToplamAlacak = toplamOdenecek,
            CreatedAt = DateTime.UtcNow
        };

        // TERS KAYIT: Borç ↔ Alacak yer değiştirir
        var sira = 1;
        foreach (var kalem in orijinalFis.Kalemler.OrderBy(k => k.SiraNo))
        {
            reverseFis.Kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = kalem.HesapId,
                SiraNo = sira++,
                Borc = kalem.Alacak,      // TERS
                Alacak = kalem.Borc,       // TERS
                Aciklama = $"İptal: {kalem.Aciklama}",
                CreatedAt = DateTime.UtcNow
            });
        }

        context.MuhasebeFisleri.Add(reverseFis);
        await context.SaveChangesAsync();

        // Snapshot bağlantıları da audit ve üst transaction kapsamındadır.
        foreach (var kayit in snapshot)
        {
            kayit.IptalFisId = reverseFis.Id;
            kayit.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();

        // Ters fiş oluşturuldu

        return reverseFis;
    }

    private async Task<MuhasebeFis?> KayitTransactionAsync(int yil, int ay, int firmaId, Func<ApplicationDbContext, Task<MuhasebeFis?>> kaydet)
    {
        if (yil is < 1 or > 9999 || ay is < 1 or > 12 || firmaId <= 0)
            throw new ArgumentException("Geçerli yıl, ay ve firma gerekir.");
        long secimSurumu = 0;
        void FirmaDegisimi() => System.Threading.Interlocked.Increment(ref secimSurumu);
        void SecimiDogrula()
        {
            if (System.Threading.Volatile.Read(ref secimSurumu) != 0 || _firmaProvider.TumFirmalar || _firmaProvider.AktifFirmaId != firmaId)
                throw new InvalidOperationException("Muhasebeleştirme için kaynak firmayı seçin; işlem sırasında firma seçimi değişmemelidir.");
        }
        _firmaProvider.AktifFirmaDegisti += FirmaDegisimi;
        try
        {
            SecimiDogrula();
            await using var strategyContext = await _factory.CreateDbContextAsync();
            var strategy = strategyContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                SecimiDogrula();
                // Retry her denemede yeni tracker ve transaction ile başlar.
                await using var context = await _factory.CreateDbContextAsync();
                if (!await context.Firmalar.AnyAsync(f => f.Id == firmaId && !f.IsDeleted))
                    throw new InvalidOperationException("Kaynak firma erişilebilir değil.");
                await using var transaction = await context.Database.BeginTransactionAsync();
                var sonuc = await kaydet(context);
                SecimiDogrula();
                await transaction.CommitAsync();
                return sonuc;
            });
        }
        finally { _firmaProvider.AktifFirmaDegisti -= FirmaDegisimi; }
    }

    private static async Task<string> GenerateNextMaasFisNoAsync(ApplicationDbContext context, int firmaId)
    {
        var prefix = $"MAS-{firmaId}-";
        var yil = MKFiloServis.Shared.Time.BusinessTime.Today.Year;

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


