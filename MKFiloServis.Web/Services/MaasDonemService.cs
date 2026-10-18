using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Calculation;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class MaasDonemService : IMaasDonemService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public MaasDonemService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Dictionary<int, DonemFinansSonuc>> GetDonemFinansAsync(
        IReadOnlyCollection<int> personelIds,
        int yil,
        int ay,
        bool tumFirmalar = false,
        IReadOnlyCollection<int>? firmaIds = null,
        CancellationToken cancellationToken = default)
    {
        if (yil is < 1 or > 9998)
            throw new ArgumentOutOfRangeException(nameof(yil), "Yıl 1 ile 9998 arasında olmalıdır.");
        if (ay is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(ay), "Ay 1 ile 12 arasında olmalıdır.");
        if (tumFirmalar && (firmaIds is null || firmaIds.Count == 0))
            throw new ArgumentException("Tüm firmalar kapsamında firma listesi verilmelidir.", nameof(firmaIds));

        var ids = personelIds.Where(id => id > 0).Distinct().ToArray();
        var sonuc = new Dictionary<int, DonemFinansSonuc>(ids.Length);
        if (ids.Length == 0)
            return sonuc;

        // Ay sonundan sonraki kayıtlar bu ayı etkilemez; saat bilgisi taşıyan tarihler için üst sınır dışlayıcıdır.
        var donemSonrasi = new DateTime(yil, ay, 1).AddMonths(1);
        var firmalar = tumFirmalar ? firmaIds!.ToArray() : Array.Empty<int>();

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var avansSorgu = context.PersonelAvanslar.AsNoTracking().AsQueryable();
        var borcSorgu = context.PersonelBorclar.AsNoTracking().AsQueryable();
        var masrafSorgu = context.AracMasraflari.AsNoTracking().AsQueryable();
        var hareketSorgu = context.BankaKasaHareketleri.AsNoTracking().AsQueryable();
        var mahsupSorgu = context.PersonelAvansMahsuplar.AsNoTracking().AsQueryable();
        var borcOdemeSorgu = context.PersonelBorcOdemeler.AsNoTracking().AsQueryable();

        if (tumFirmalar)
        {
            // Firma filtresi kaldırıldığında silinmiş kayıtlar da görünür olur; hem silinmişleri hem firma dışını açıkça ele.
            avansSorgu = avansSorgu.IgnoreQueryFilters().Where(a => !a.IsDeleted && a.FirmaId != null && firmalar.Contains(a.FirmaId.Value));
            borcSorgu = borcSorgu.IgnoreQueryFilters().Where(b => !b.IsDeleted && b.FirmaId != null && firmalar.Contains(b.FirmaId.Value));
            masrafSorgu = masrafSorgu.IgnoreQueryFilters().Where(m => !m.IsDeleted && m.FirmaId != null && firmalar.Contains(m.FirmaId.Value));
            hareketSorgu = hareketSorgu.IgnoreQueryFilters().Where(h => !h.IsDeleted && h.FirmaId != null && firmalar.Contains(h.FirmaId.Value));
            mahsupSorgu = mahsupSorgu.IgnoreQueryFilters().Where(m => !m.IsDeleted);
            borcOdemeSorgu = borcOdemeSorgu.IgnoreQueryFilters().Where(o => !o.IsDeleted);
        }
        else
        {
            avansSorgu = avansSorgu.Where(a => !a.IsDeleted);
            borcSorgu = borcSorgu.Where(b => !b.IsDeleted);
            masrafSorgu = masrafSorgu.Where(m => !m.IsDeleted);
            hareketSorgu = hareketSorgu.Where(h => !h.IsDeleted);
            mahsupSorgu = mahsupSorgu.Where(m => !m.IsDeleted);
            borcOdemeSorgu = borcOdemeSorgu.Where(o => !o.IsDeleted);
        }

        var avanslar = await avansSorgu
            .Where(a => ids.Contains(a.PersonelId) && a.Durum != AvansDurum.IptalEdildi && a.AvansTarihi < donemSonrasi)
            .Select(a => new { a.Id, a.PersonelId, a.AvansTarihi, a.Tutar, a.MahsupEdilen, a.MahsupTarihi })
            .ToListAsync(cancellationToken);
        var avansIds = avanslar.Select(a => a.Id).ToArray();
        var mahsuplar = avansIds.Length == 0
            ? []
            : await mahsupSorgu
                .Where(m => avansIds.Contains(m.AvansId))
                .Select(m => new { m.AvansId, m.MahsupTarihi, m.MahsupTutari, m.MahsupSekli })
                .ToListAsync(cancellationToken);
        var mahsupMap = mahsuplar
            .GroupBy(m => m.AvansId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DonemMahsup>)g
                .Select(m => new DonemMahsup(m.MahsupTarihi, m.MahsupTutari, m.MahsupSekli == MahsupSekli.MaastanKesinti))
                .ToList());

        var borclar = await borcSorgu
            .Where(b => ids.Contains(b.PersonelId) && b.OdemeDurum != BorcOdemeDurum.IptalEdildi && b.BorcTarihi < donemSonrasi)
            .Select(b => new { b.Id, b.PersonelId, b.BorcTarihi, b.Tutar, b.OdenenTutar, b.GerceklesenOdemeTarihi })
            .ToListAsync(cancellationToken);
        var borcIds = borclar.Select(b => b.Id).ToArray();
        var borcOdemeleri = borcIds.Length == 0
            ? []
            : await borcOdemeSorgu
                .Where(o => borcIds.Contains(o.BorcId))
                .Select(o => new { o.BorcId, o.OdemeTarihi, o.OdemeTutari })
                .ToListAsync(cancellationToken);
        var borcOdemeMap = borcOdemeleri
            .GroupBy(o => o.BorcId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DonemBorcOdeme>)g
                .Select(o => new DonemBorcOdeme(o.OdemeTarihi, o.OdemeTutari))
                .ToList());

        var masraflar = await masrafSorgu
            .Where(m => m.PersonelCebindenId != null && ids.Contains(m.PersonelCebindenId.Value) && m.MasrafTarihi < donemSonrasi)
            .Select(m => new { m.Id, PersonelId = m.PersonelCebindenId!.Value, Tarih = m.MasrafTarihi, m.Tutar, m.PersoneleOdendi, m.PersonelOdemeTarihi })
            .ToListAsync(cancellationToken);
        var hareketler = await hareketSorgu
            .Where(h => h.PersonelCebindenId != null && ids.Contains(h.PersonelCebindenId.Value) && h.IslemTarihi < donemSonrasi)
            .Select(h => new { h.AracMasrafId, PersonelId = h.PersonelCebindenId!.Value, Tarih = h.IslemTarihi, h.Tutar, h.PersoneleOdendi, h.PersonelOdemeTarihi })
            .ToListAsync(cancellationToken);

        // Araç masrafı personel cebinden girildiğinde banka/kasa hareketi otomatik açılır ve geri ödeme bu hareket üzerinden
        // yapılır; aynı harcama iki kez sayılmasın diye hareketi olan masraf satırı atlanır.
        var hareketliMasrafIds = hareketler.Where(h => h.AracMasrafId != null).Select(h => h.AracMasrafId!.Value).ToHashSet();
        masraflar = masraflar.Where(m => !hareketliMasrafIds.Contains(m.Id)).ToList();

        // Geri ödenmiş ama tarihi girilmemiş eski kayıt, kendi tarihinde kapanmış sayılır; tarihsiz kalıp sonsuza dek devretmesin.
        static DateTime? OdemeTarihi(bool odendi, DateTime? odemeTarihi, DateTime kayitTarihi) =>
            odendi ? (odemeTarihi ?? kayitTarihi) : null;

        var harcamaMap = masraflar
            .Select(m => (m.PersonelId, Kayit: new DonemHarcama(m.Tarih, m.Tutar, OdemeTarihi(m.PersoneleOdendi, m.PersonelOdemeTarihi, m.Tarih))))
            .Concat(hareketler.Select(h => (h.PersonelId, Kayit: new DonemHarcama(h.Tarih, h.Tutar, OdemeTarihi(h.PersoneleOdendi, h.PersonelOdemeTarihi, h.Tarih)))))
            .ToLookup(x => x.PersonelId, x => x.Kayit);

        // Mahsup/ödeme satırı olmadan işaretlenmiş (eski/aktarılmış) kapanışlar: toplam, satırlardan fazlaysa fark
        // kapanış tarihinde (yoksa kaydın kendi tarihinde) kapanmış sayılır; böylece mevcut açık bakiyeyle tutarlı kalır.
        var avansMap = avanslar
            .ToLookup(a => a.PersonelId, a =>
            {
                var satirlar = mahsupMap.TryGetValue(a.Id, out var liste) ? liste : [];
                var eksik = a.MahsupEdilen - satirlar.Sum(m => m.Tutar);
                return new DonemAvans(a.AvansTarihi, a.Tutar, eksik > 0
                    ? [.. satirlar, new DonemMahsup(a.MahsupTarihi ?? a.AvansTarihi, eksik, MaastanKesinti: true)]
                    : satirlar);
            });
        var borcMap = borclar
            .ToLookup(b => b.PersonelId, b =>
            {
                var satirlar = borcOdemeMap.TryGetValue(b.Id, out var liste) ? liste : [];
                var eksik = b.OdenenTutar - satirlar.Sum(o => o.Tutar);
                return new DonemBorc(b.BorcTarihi, b.Tutar, eksik > 0
                    ? [.. satirlar, new DonemBorcOdeme(b.GerceklesenOdemeTarihi ?? b.BorcTarihi, eksik)]
                    : satirlar);
            });

        foreach (var id in ids)
            sonuc[id] = MaasDonemHesaplayici.Hesapla(yil, ay, avansMap[id], borcMap[id], harcamaMap[id]);

        return sonuc;
    }
}
