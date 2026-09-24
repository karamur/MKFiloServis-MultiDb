using System.Data;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services.RentACar;

public sealed class RentACarRezervasyonServisi : IRentACarRezervasyonServisi
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public RentACarRezervasyonServisi(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IAktifFirmaProvider aktifFirmaProvider)
    {
        _contextFactory = contextFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    public async Task<IReadOnlyList<Cari>> GetAktifMusterilerAsync(CancellationToken cancellationToken = default)
    {
        var firmaId = AktifFirmaIdAl();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Cariler.AsNoTracking()
            .Where(x => x.FirmaId == firmaId && x.Aktif && !x.IsDeleted
                && (x.CariTipi == CariTipi.Musteri || x.CariTipi == CariTipi.MusteriTedarikci))
            .OrderBy(x => x.Unvan)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Arac>> GetAktifAraclarAsync(CancellationToken cancellationToken = default)
    {
        var firmaId = AktifFirmaIdAl();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Araclar.AsNoTracking()
            .Where(x => x.FirmaId == firmaId && x.Aktif && !x.IsDeleted)
            .OrderBy(x => x.AktifPlaka)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Arac>> GetMusaitAraclarAsync(
        DateTime baslangic,
        DateTime bitis,
        CancellationToken cancellationToken = default)
    {
        TarihleriDogrula(baslangic, bitis);
        var firmaId = AktifFirmaIdAl();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var kiradakiAracIds = await context.MusteriKiralamalar.AsNoTracking()
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted
                && x.Durum != KiralamaDurumu.IptalEdildi
                && x.BaslangicTarihi < bitis
                && (x.GercekBitisTarihi ?? x.PlanlananBitisTarihi) > baslangic)
            .Select(x => x.AracId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await context.Araclar.AsNoTracking()
            .Where(x => x.FirmaId == firmaId && x.Aktif && !x.IsDeleted
                && !kiradakiAracIds.Contains(x.Id))
            .OrderBy(x => x.AktifPlaka)
            .ToListAsync(cancellationToken);
    }

    public async Task<MusteriKiralama> RezervasyonOlusturAsync(
        RentACarRezervasyonTalebi talep,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(talep);
        TarihleriDogrula(talep.BaslangicTarihi, talep.PlanlananBitisTarihi);
        if (talep.MusteriId <= 0 || talep.AracId <= 0)
        {
            throw new InvalidOperationException("Müşteri ve araç seçilmelidir.");
        }
        if (talep.GunlukFiyat <= 0)
        {
            throw new InvalidOperationException("Günlük fiyat sıfırdan büyük olmalıdır.");
        }
        if (talep.Depozito < 0)
        {
            throw new InvalidOperationException("Depozito negatif olamaz.");
        }
        if (talep.Notlar?.Length > 500)
        {
            throw new InvalidOperationException("Not alanı en fazla 500 karakter olabilir.");
        }

        var firmaId = AktifFirmaIdAl();
        return await SeriIslemAsync(async context =>
        {

        var musteriGecerli = await context.Cariler.AnyAsync(x =>
            x.Id == talep.MusteriId && x.FirmaId == firmaId && x.Aktif && !x.IsDeleted
            && (x.CariTipi == CariTipi.Musteri || x.CariTipi == CariTipi.MusteriTedarikci), cancellationToken);
        if (!musteriGecerli)
        {
            throw new InvalidOperationException("Seçilen müşteri aktif firmaya ait değil veya kullanılamıyor.");
        }

        var aracGecerli = await context.Araclar.AnyAsync(x =>
            x.Id == talep.AracId && x.FirmaId == firmaId && x.Aktif && !x.IsDeleted, cancellationToken);
        if (!aracGecerli)
        {
            throw new InvalidOperationException("Seçilen araç aktif firmaya ait değil veya kullanılamıyor.");
        }

        var cakismaVar = await context.MusteriKiralamalar.AnyAsync(x =>
            x.FirmaId == firmaId && x.AracId == talep.AracId && !x.IsDeleted
            && x.Durum != KiralamaDurumu.IptalEdildi
            && x.BaslangicTarihi < talep.PlanlananBitisTarihi
            && (x.GercekBitisTarihi ?? x.PlanlananBitisTarihi) > talep.BaslangicTarihi,
            cancellationToken);
        if (cakismaVar)
        {
            throw new InvalidOperationException("Araç seçilen tarih aralığında müsait değil.");
        }

        var kiralama = new MusteriKiralama
        {
            FirmaId = firmaId,
            MusteriId = talep.MusteriId,
            AracId = talep.AracId,
            BaslangicTarihi = talep.BaslangicTarihi,
            PlanlananBitisTarihi = talep.PlanlananBitisTarihi,
            GunlukFiyat = decimal.Round(talep.GunlukFiyat, 2),
            ToplamTutar = decimal.Round(ToplamTutarHesapla(talep.BaslangicTarihi, talep.PlanlananBitisTarihi, talep.GunlukFiyat), 2),
            Depozito = talep.Depozito.HasValue ? decimal.Round(talep.Depozito.Value, 2) : null,
            Durum = KiralamaDurumu.Rezervasyon,
            OdemeDurumu = KiralamaOdemeDurumu.Beklemede,
            Notlar = string.IsNullOrWhiteSpace(talep.Notlar) ? null : talep.Notlar.Trim(),
            SozlesmeNo = $"KR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..16],
            CreatedAt = DateTime.UtcNow
        };

        context.MusteriKiralamalar.Add(kiralama);
        await context.SaveChangesAsync(cancellationToken);
        return kiralama;
        }, cancellationToken);
    }

    public async Task RezervasyonGuncelleAsync(
        int kiralamaId,
        RentACarRezervasyonTalebi talep,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(talep);
        if (kiralamaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kiralamaId));
        }

        TalebiDogrula(talep);
        var firmaId = AktifFirmaIdAl();
        await SeriIslemAsync(async context =>
        {
        var kiralama = await context.MusteriKiralamalar.FirstOrDefaultAsync(
            x => x.Id == kiralamaId && x.FirmaId == firmaId && !x.IsDeleted,
            cancellationToken);

        if (kiralama is null)
        {
            throw new InvalidOperationException("Kiralama kaydı bulunamadı.");
        }
        if (kiralama.Durum != KiralamaDurumu.Rezervasyon)
        {
            throw new InvalidOperationException("Yalnızca rezervasyon durumundaki kayıtlar düzenlenebilir.");
        }

        var musteriGecerli = await context.Cariler.AnyAsync(x =>
            x.Id == talep.MusteriId && x.FirmaId == firmaId && x.Aktif && !x.IsDeleted
            && (x.CariTipi == CariTipi.Musteri || x.CariTipi == CariTipi.MusteriTedarikci), cancellationToken);
        if (!musteriGecerli)
        {
            throw new InvalidOperationException("Seçilen müşteri aktif firmaya ait değil veya kullanılamıyor.");
        }

        var aracGecerli = await context.Araclar.AnyAsync(x =>
            x.Id == talep.AracId && x.FirmaId == firmaId && x.Aktif && !x.IsDeleted, cancellationToken);
        if (!aracGecerli)
        {
            throw new InvalidOperationException("Seçilen araç aktif firmaya ait değil veya kullanılamıyor.");
        }

        var cakismaVar = await context.MusteriKiralamalar.AnyAsync(x =>
            x.Id != kiralamaId && x.FirmaId == firmaId && x.AracId == talep.AracId && !x.IsDeleted
            && x.Durum != KiralamaDurumu.IptalEdildi
            && x.BaslangicTarihi < talep.PlanlananBitisTarihi
            && (x.GercekBitisTarihi ?? x.PlanlananBitisTarihi) > talep.BaslangicTarihi,
            cancellationToken);
        if (cakismaVar)
        {
            throw new InvalidOperationException("Araç seçilen tarih aralığında müsait değil.");
        }

        kiralama.MusteriId = talep.MusteriId;
        kiralama.AracId = talep.AracId;
        kiralama.BaslangicTarihi = talep.BaslangicTarihi;
        kiralama.PlanlananBitisTarihi = talep.PlanlananBitisTarihi;
        kiralama.GunlukFiyat = decimal.Round(talep.GunlukFiyat, 2);
        kiralama.ToplamTutar = decimal.Round(
            ToplamTutarHesapla(talep.BaslangicTarihi, talep.PlanlananBitisTarihi, talep.GunlukFiyat), 2);
        kiralama.Depozito = talep.Depozito.HasValue ? decimal.Round(talep.Depozito.Value, 2) : null;
        kiralama.Notlar = string.IsNullOrWhiteSpace(talep.Notlar) ? null : talep.Notlar.Trim();
        kiralama.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return true;
        }, cancellationToken);
    }

    private async Task<T> SeriIslemAsync<T>(Func<ApplicationDbContext, Task<T>> islem, CancellationToken cancellationToken)
    {
        await using var strategyContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var sonuc = await islem(context);
            await transaction.CommitAsync(cancellationToken);
            return sonuc;
        });
    }

    public async Task IptalEtAsync(
        int kiralamaId,
        string iptalNedeni,
        CancellationToken cancellationToken = default)
    {
        if (kiralamaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kiralamaId));
        }

        var neden = iptalNedeni?.Trim();
        if (string.IsNullOrWhiteSpace(neden))
        {
            throw new InvalidOperationException("İptal nedeni girilmelidir.");
        }

        var firmaId = AktifFirmaIdAl();
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var kiralama = await context.MusteriKiralamalar
            .FirstOrDefaultAsync(x => x.Id == kiralamaId && x.FirmaId == firmaId && !x.IsDeleted, cancellationToken);

        if (kiralama is null)
        {
            throw new InvalidOperationException("Kiralama kaydı bulunamadı.");
        }

        if (kiralama.Durum != KiralamaDurumu.Rezervasyon)
        {
            throw new InvalidOperationException("Yalnızca rezervasyon durumundaki kiralamalar iptal edilebilir.");
        }

        var iptalNotu = $"İptal nedeni: {neden}";
        var yeniNotlar = string.IsNullOrWhiteSpace(kiralama.Notlar)
            ? iptalNotu
            : $"{kiralama.Notlar.Trim()}\n{iptalNotu}";

        if (yeniNotlar.Length > 500)
        {
            throw new InvalidOperationException("İptal nedeni mevcut notlarla birlikte 500 karakter sınırını aşıyor.");
        }

        kiralama.Durum = KiralamaDurumu.IptalEdildi;
        kiralama.Notlar = yeniNotlar;
        kiralama.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    private int AktifFirmaIdAl()
    {
        var firmaId = _aktifFirmaProvider.AktifFirmaId;
        if (!firmaId.HasValue || firmaId.Value <= 0)
        {
            throw new InvalidOperationException("İşlem için önce aktif firma seçilmelidir.");
        }

        return firmaId.Value;
    }

    private static void TarihleriDogrula(DateTime baslangic, DateTime bitis)
    {
        if (bitis <= baslangic)
        {
            throw new InvalidOperationException("Planlanan iade zamanı başlangıç zamanından sonra olmalıdır.");
        }
    }

    private static void TalebiDogrula(RentACarRezervasyonTalebi talep)
    {
        TarihleriDogrula(talep.BaslangicTarihi, talep.PlanlananBitisTarihi);
        if (talep.MusteriId <= 0 || talep.AracId <= 0)
        {
            throw new InvalidOperationException("Müşteri ve araç seçilmelidir.");
        }
        if (talep.GunlukFiyat <= 0)
        {
            throw new InvalidOperationException("Günlük fiyat sıfırdan büyük olmalıdır.");
        }
        if (talep.Depozito < 0)
        {
            throw new InvalidOperationException("Depozito negatif olamaz.");
        }
        if (talep.Notlar?.Length > 500)
        {
            throw new InvalidOperationException("Not alanı en fazla 500 karakter olabilir.");
        }
    }

    private static decimal ToplamTutarHesapla(DateTime baslangic, DateTime bitis, decimal gunlukFiyat)
    {
        var gunSayisi = Math.Max(1, (int)Math.Ceiling((bitis - baslangic).TotalDays));
        return gunSayisi * gunlukFiyat;
    }
}
