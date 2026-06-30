using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class AracMasrafService : IAracMasrafService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;
    private readonly IBankaKasaHareketService _bankaKasaHareketService;

    public AracMasrafService(IDbContextFactory<ApplicationDbContext> contextFactory, IMuhasebeService muhasebeService, IBankaKasaHareketService bankaKasaHareketService)
    {
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
        _bankaKasaHareketService = bankaKasaHareketService;
    }

    public async Task<List<AracMasraf>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracMasraflari
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Guzergah)
            .Include(m => m.Sofor)
            .Include(m => m.Cari)
            .Include(m => m.MuhasebeFis)
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.MasrafTarihi)
            .ToListAsync();
    }

    public async Task<List<AracMasraf>> GetByAracIdAsync(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracMasraflari
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Guzergah)
            .Include(m => m.Sofor)
            .Include(m => m.Cari)
            .Where(m => !m.IsDeleted && m.AracId == aracId)
            .OrderByDescending(m => m.MasrafTarihi)
            .ToListAsync();
    }

    public async Task<List<AracMasraf>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracMasraflari
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Guzergah)
            .Include(m => m.Sofor)
            .Include(m => m.Cari)
            .Where(m => !m.IsDeleted && m.MasrafTarihi >= startDate && m.MasrafTarihi <= endDate)
            .OrderByDescending(m => m.MasrafTarihi)
            .ToListAsync();
    }

    public async Task<List<AracMasraf>> GetByAracAndDateRangeAsync(int aracId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracMasraflari
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Guzergah)
            .Include(m => m.Sofor)
            .Include(m => m.Cari)
            .Where(m => !m.IsDeleted && m.AracId == aracId && m.MasrafTarihi >= startDate && m.MasrafTarihi <= endDate)
            .OrderByDescending(m => m.MasrafTarihi)
            .ToListAsync();
    }

    public async Task<List<AracMasraf>> GetArizaMasraflariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracMasraflari
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Guzergah)
            .Include(m => m.ServisCalisma)
            .Include(m => m.Sofor)
            .Include(m => m.Cari)
            .Where(m => !m.IsDeleted && m.ArizaKaynaklimi)
            .OrderByDescending(m => m.MasrafTarihi)
            .ToListAsync();
    }

    public async Task<List<AracMasraf>> GetByKategoriAsync(MasrafKategori kategori, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AracMasraflari
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Sofor)
            .Include(m => m.Cari)
            .Where(m => !m.IsDeleted && m.MasrafKalemi.Kategori == kategori);

        if (startDate.HasValue)
            query = query.Where(m => m.MasrafTarihi >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MasrafTarihi <= endDate.Value);

        return await query.OrderByDescending(m => m.MasrafTarihi).ToListAsync();
    }

    public async Task<AracMasraf?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracMasraflari
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Guzergah)
            .Include(m => m.ServisCalisma)
            .Include(m => m.Sofor)
            .Include(m => m.Cari)
            .Include(m => m.MuhasebeFis)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
    }

    public async Task<AracMasraf> CreateAsync(AracMasraf aracMasraf, bool muhasebeFisiOlustur = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await UygulaSahiplikKurallariAsync(context, aracMasraf);
        ValidateMuhtapSecimi(aracMasraf);

        context.AracMasraflari.Add(aracMasraf);
        await context.SaveChangesAsync();

        await SenkronizeMuhasebeDurumuAsync(context, aracMasraf.Id, muhasebeFisiOlustur);

        var savedMasraf = (await GetByIdAsync(aracMasraf.Id))!;
        await AutoSyncBankaHareketAsync(savedMasraf, null);
        return savedMasraf;
    }

    public async Task<AracMasraf> UpdateAsync(AracMasraf aracMasraf, bool muhasebeFisiOlustur = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await UygulaSahiplikKurallariAsync(context, aracMasraf);
        ValidateMuhtapSecimi(aracMasraf);

        var existing = await context.AracMasraflari
            .FirstOrDefaultAsync(m => m.Id == aracMasraf.Id && !m.IsDeleted);

        if (existing == null)
            throw new InvalidOperationException("Masraf kaydı bulunamadı.");

        existing.MasrafTarihi = aracMasraf.MasrafTarihi;
        existing.Tutar = aracMasraf.Tutar;
        existing.Aciklama = aracMasraf.Aciklama;
        existing.BelgeNo = aracMasraf.BelgeNo;
        existing.ArizaKaynaklimi = aracMasraf.ArizaKaynaklimi;
        existing.OdemeKaynak = aracMasraf.OdemeKaynak;
        existing.PersonelCebindenId = aracMasraf.PersonelCebindenId;
        existing.PersoneleOdendi = aracMasraf.PersoneleOdendi;
        existing.PersonelOdemeTarihi = aracMasraf.PersonelOdemeTarihi;
        existing.BankaHesapId = aracMasraf.BankaHesapId;
        existing.AracId = aracMasraf.AracId;
        existing.MasrafKalemiId = aracMasraf.MasrafKalemiId;
        existing.GuzergahId = aracMasraf.GuzergahId;
        existing.ServisCalismaId = aracMasraf.ServisCalismaId;
        existing.SoforId = aracMasraf.SoforId;
        existing.CariId = aracMasraf.CariId;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await SenkronizeMuhasebeDurumuAsync(context, existing.Id, muhasebeFisiOlustur);

        var savedMasraf = (await GetByIdAsync(existing.Id))!;

        // Find existing linked hareket (if any) to update it
        var linkedHareket = await context.BankaKasaHareketleri
            .FirstOrDefaultAsync(h => h.AracMasrafId == existing.Id && !h.IsDeleted);
        await AutoSyncBankaHareketAsync(savedMasraf, linkedHareket?.Id);
        return savedMasraf;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var aracMasraf = await context.AracMasraflari
            .FirstOrDefaultAsync(m => m.Id == id);

        if (aracMasraf == null)
            return;

        if (aracMasraf.MuhasebeFisId.HasValue)
        {
            await _muhasebeService.DeleteFisAsync(aracMasraf.MuhasebeFisId.Value);
            aracMasraf.MuhasebeFisId = null;
        }

        // Linked banka hareketi varsa sil
        var linkedHareket = await context.BankaKasaHareketleri
            .FirstOrDefaultAsync(h => h.AracMasrafId == id && !h.IsDeleted);
        if (linkedHareket != null)
        {
            await _bankaKasaHareketService.DeleteAsync(linkedHareket.Id);
        }

        aracMasraf.IsDeleted = true;
        aracMasraf.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<decimal> GetToplamMasrafByAracAsync(int aracId, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AracMasraflari.Where(m => m.AracId == aracId);
        query = query.Where(m => !m.IsDeleted);

        if (startDate.HasValue)
            query = query.Where(m => m.MasrafTarihi >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MasrafTarihi <= endDate.Value);

        return await query.SumAsync(m => m.Tutar);
    }

    private static void ValidateMuhtapSecimi(AracMasraf aracMasraf)
    {
        if (aracMasraf.SoforId.HasValue && aracMasraf.CariId.HasValue)
            throw new InvalidOperationException("Aynı masraf kaydı için hem personel hem cari seçilemez.");
    }

    private async Task UygulaSahiplikKurallariAsync(ApplicationDbContext context, AracMasraf aracMasraf)
    {
        var arac = await context.Araclar
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == aracMasraf.AracId && !a.IsDeleted);

        if (arac == null)
            throw new InvalidOperationException("Masraf için seçilen araç bulunamadı.");

        if (arac.SahiplikTipi != AracSahiplikTipi.Komisyon)
            return;

        aracMasraf.SoforId = null;
        aracMasraf.CariId ??= arac.KomisyoncuCariId;

        if (!aracMasraf.CariId.HasValue)
            throw new InvalidOperationException("Komisyon araç masraflarında komisyoncu cari tanımlı olmalı.");
    }

    private async Task MuhasebeFisSenkronizeEtAsync(ApplicationDbContext context, int aracMasrafId)
    {
        var aracMasraf = await context.AracMasraflari
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Sofor)
            .Include(m => m.Cari)
            .FirstOrDefaultAsync(m => m.Id == aracMasrafId);

        if (aracMasraf == null || aracMasraf.Tutar <= 0)
            return;

        var giderHesabi = await GetMasrafHesabiAsync(context, aracMasraf.MasrafKalemi?.Kategori ?? MasrafKategori.Diger);
        var karsiHesap = await GetKarsiHesapAsync(context, aracMasraf);
        var mevcutFis = aracMasraf.MuhasebeFisId.HasValue
            ? await _muhasebeService.GetFisByIdAsync(aracMasraf.MuhasebeFisId.Value)
            : null;

        var fis = new MuhasebeFis
        {
            Id = mevcutFis?.Id ?? 0,
            FisNo = mevcutFis?.FisNo ?? string.Empty,
            FisTarihi = aracMasraf.MasrafTarihi,
            FisTipi = FisTipi.Mahsup,
            Aciklama = BuildFisAciklamasi(aracMasraf),
            Kaynak = FisKaynak.Otomatik,
            KaynakId = aracMasraf.Id,
            KaynakTip = "AracMasraf",
            Durum = mevcutFis?.Durum ?? FisDurum.Taslak,
            Kalemler = new List<MuhasebeFisKalem>
            {
                new()
                {
                    HesapId = giderHesabi.Id,
                    Borc = aracMasraf.Tutar,
                    Alacak = 0,
                    SiraNo = 1,
                    Aciklama = BuildFisAciklamasi(aracMasraf),
                    CariId = aracMasraf.CariId
                },
                new()
                {
                    HesapId = karsiHesap.Id,
                    Borc = 0,
                    Alacak = aracMasraf.Tutar,
                    SiraNo = 2,
                    Aciklama = BuildKarsiHesapAciklamasi(aracMasraf),
                    CariId = aracMasraf.CariId
                }
            }
        };

        if (mevcutFis == null)
        {
            await _muhasebeService.CreateFisAtomicAsync(fis);
            aracMasraf.MuhasebeFisId = fis.Id;
            await context.SaveChangesAsync();
            return;
        }

        await _muhasebeService.UpdateFisAsync(fis);
    }

    private async Task SenkronizeMuhasebeDurumuAsync(ApplicationDbContext context, int aracMasrafId, bool muhasebeFisiOlustur)
    {
        var aracMasraf = await context.AracMasraflari
            .AsTracking()
            .FirstOrDefaultAsync(m => m.Id == aracMasrafId && !m.IsDeleted);

        if (aracMasraf == null)
            return;

        if (!muhasebeFisiOlustur)
        {
            if (aracMasraf.MuhasebeFisId.HasValue)
            {
                await _muhasebeService.DeleteFisAsync(aracMasraf.MuhasebeFisId.Value);
                aracMasraf.MuhasebeFisId = null;
                await context.SaveChangesAsync();
            }

            return;
        }

        await MuhasebeFisSenkronizeEtAsync(context, aracMasrafId);
    }

    private async Task<MuhasebeHesap> GetMasrafHesabiAsync(ApplicationDbContext context, MasrafKategori kategori)
    {
        var hesapKodu = kategori switch
        {
            MasrafKategori.Yakit => "770.06",
            MasrafKategori.Bakim => "770.07",
            MasrafKategori.Tamir => "770.07",
            MasrafKategori.Lastik => "770.07",
            MasrafKategori.YedekParca => "770.07",
            MasrafKategori.Sigorta => "770.08",
            MasrafKategori.Personel => "770.09",
            _ => "770"
        };

        return await _muhasebeService.GetHesapByKodAsync(hesapKodu)
            ?? await _muhasebeService.GetHesapByKodAsync("770")
            ?? throw new InvalidOperationException("Masraf için uygun muhasebe hesabı bulunamadı.");
    }

    private async Task<MuhasebeHesap> GetKarsiHesapAsync(ApplicationDbContext context, AracMasraf aracMasraf)
    {
        if (aracMasraf.Arac?.SahiplikTipi == AracSahiplikTipi.Komisyon)
        {
            var komisyonCariId = aracMasraf.CariId ?? aracMasraf.Arac.KomisyoncuCariId;
            if (komisyonCariId.HasValue)
                return await GetOrCreateCariHesapAsync(context, komisyonCariId.Value);
        }

        if (aracMasraf.SoforId.HasValue)
            return await GetOrCreatePersonelHesapAsync(context, aracMasraf.SoforId.Value);

        if (aracMasraf.CariId.HasValue)
            return await GetOrCreateCariHesapAsync(context, aracMasraf.CariId.Value);

        if (aracMasraf.OdemeKaynak == MasrafOdemeKaynak.Banka && aracMasraf.BankaHesapId.HasValue)
        {
            var bankaHesap = await context.BankaHesaplari
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == aracMasraf.BankaHesapId.Value && !h.IsDeleted);

            var hesapKodu = bankaHesap?.VarsayilanMuhasebeKodu;
            if (string.IsNullOrWhiteSpace(hesapKodu))
                hesapKodu = "102";

            return await _muhasebeService.GetHesapByKodAsync(hesapKodu)
                ?? await _muhasebeService.GetHesapByKodAsync("102")
                ?? throw new InvalidOperationException("Banka hesabı için muhasebe karşılığı bulunamadı.");
        }

        if (aracMasraf.OdemeKaynak == MasrafOdemeKaynak.Kasa)
        {
            var hesapKodu = "100.01";
            if (aracMasraf.BankaHesapId.HasValue)
            {
                var kasaHesap = await context.BankaHesaplari
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == aracMasraf.BankaHesapId.Value && !h.IsDeleted);
                if (!string.IsNullOrWhiteSpace(kasaHesap?.VarsayilanMuhasebeKodu))
                    hesapKodu = kasaHesap.VarsayilanMuhasebeKodu!;
            }

            return await _muhasebeService.GetHesapByKodAsync(hesapKodu)
                ?? await _muhasebeService.GetHesapByKodAsync("100")
                ?? throw new InvalidOperationException("Kasa hesabı bulunamadı.");
        }

        return await _muhasebeService.GetHesapByKodAsync("100.01")
            ?? await _muhasebeService.GetHesapByKodAsync("100")
            ?? throw new InvalidOperationException("Kasa hesabı bulunamadı.");
    }

    private async Task<MuhasebeHesap> GetOrCreateCariHesapAsync(ApplicationDbContext context, int cariId)
    {
        var anaHesap = await _muhasebeService.GetHesapByKodAsync("320")
            ?? throw new InvalidOperationException("320 Satıcılar hesabı bulunamadı.");

        var cari = await context.Cariler.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cariId)
            ?? throw new InvalidOperationException("Cari bulunamadı.");

        var hesapKodu = BuildAltHesapKodu("320", cari.Id);
        var mevcut = await _muhasebeService.GetHesapByKodAsync(hesapKodu);
        if (mevcut != null)
            return mevcut;

        mevcut = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.UstHesapId == anaHesap.Id && h.HesapAdi == cari.Unvan && !h.IsDeleted);
        if (mevcut != null)
            return mevcut;

        anaHesap.AltHesapVar = true;
        await context.SaveChangesAsync();

        return await _muhasebeService.CreateHesapAsync(new MuhasebeHesap
        {
            HesapKodu = hesapKodu,
            HesapAdi = cari.Unvan,
            HesapTuru = anaHesap.HesapTuru,
            HesapGrubu = anaHesap.HesapGrubu,
            UstHesapId = anaHesap.Id,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<MuhasebeHesap> GetOrCreatePersonelHesapAsync(ApplicationDbContext context, int soforId)
    {
        var anaHesap = await _muhasebeService.GetHesapByKodAsync("335")
            ?? throw new InvalidOperationException("335 Personellere Borçlar hesabı bulunamadı.");

        var sofor = await context.Soforler.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == soforId)
            ?? throw new InvalidOperationException("Personel bulunamadı.");

        var hesapKodu = BuildAltHesapKodu("335", sofor.Id);
        var mevcut = await _muhasebeService.GetHesapByKodAsync(hesapKodu);
        if (mevcut != null)
            return mevcut;

        mevcut = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.UstHesapId == anaHesap.Id && h.HesapAdi == sofor.TamAd && !h.IsDeleted);
        if (mevcut != null)
            return mevcut;

        anaHesap.AltHesapVar = true;
        await context.SaveChangesAsync();

        return await _muhasebeService.CreateHesapAsync(new MuhasebeHesap
        {
            HesapKodu = hesapKodu,
            HesapAdi = sofor.TamAd,
            HesapTuru = anaHesap.HesapTuru,
            HesapGrubu = anaHesap.HesapGrubu,
            UstHesapId = anaHesap.Id,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string BuildFisAciklamasi(AracMasraf aracMasraf)
    {
        var sahiplik = aracMasraf.Arac?.SahiplikTipi switch
        {
            AracSahiplikTipi.Ozmal => "Özmal",
            AracSahiplikTipi.Kiralik => "Kiralık",
            AracSahiplikTipi.Komisyon => "Komisyon",
            _ => "Araç"
        };
        var muhatap = aracMasraf.Sofor?.TamAd ?? aracMasraf.Cari?.Unvan ?? "Kasa";
        var belge = string.IsNullOrWhiteSpace(aracMasraf.BelgeNo) ? string.Empty : $" / Belge: {aracMasraf.BelgeNo}";
        return $"{sahiplik} araç masrafı - {aracMasraf.Arac?.AktifPlaka} - {aracMasraf.MasrafKalemi?.MasrafAdi} - {muhatap}{belge}";
    }

    private static string BuildKarsiHesapAciklamasi(AracMasraf aracMasraf)
    {
        if (aracMasraf.Arac?.SahiplikTipi == AracSahiplikTipi.Komisyon && aracMasraf.Cari != null)
            return $"Komisyon araç masrafı: {aracMasraf.Cari.Unvan}";

        if (aracMasraf.Sofor != null)
            return $"Personel cebinden ödeme: {aracMasraf.Sofor.TamAd}";

        if (aracMasraf.Cari != null)
            return $"Cari cebinden ödeme: {aracMasraf.Cari.Unvan}";

        return "Kasa karşılığı";
    }

    private static string BuildAltHesapKodu(string anaHesapKodu, int id)
    {
        return $"{anaHesapKodu}.{id:D6}";
    }

    private async Task AutoSyncBankaHareketAsync(AracMasraf masraf, int? existingHareketId)
    {
        // Yalnızca "Personel cebinden" + hesap seçili olduğunda otomatik banka/kasa hareketi oluştur/güncelle
        if (masraf.OdemeKaynak != MasrafOdemeKaynak.PersonelCebinden || !masraf.BankaHesapId.HasValue || !masraf.SoforId.HasValue)
        {
            // Önceden linked hareket varsa ve artık koşul sağlanmıyorsa sil
            if (existingHareketId.HasValue)
                await _bankaKasaHareketService.DeleteAsync(existingHareketId.Value);
            return;
        }

        var personelAd = masraf.Sofor?.TamAd ?? $"Personel#{masraf.SoforId}";
        var aracPlaka = masraf.Arac?.AktifPlaka ?? $"Araç#{masraf.AracId}";
        var aciklama = string.IsNullOrWhiteSpace(masraf.Aciklama)
            ? $"Personel cebinden araç masrafı - {aracPlaka} - {masraf.MasrafKalemi?.MasrafAdi}"
            : masraf.Aciklama;

        if (existingHareketId.HasValue)
        {
            var existing = await _bankaKasaHareketService.GetByIdAsync(existingHareketId.Value);
            if (existing != null)
            {
                existing.IslemTarihi = masraf.MasrafTarihi;
                existing.Tutar = masraf.Tutar;
                existing.BelgeNo = masraf.BelgeNo;
                existing.Aciklama = aciklama;
                existing.BankaHesapId = masraf.BankaHesapId.Value;
                existing.PersonelCebindenId = masraf.SoforId;
                existing.AracId = masraf.AracId;
                await _bankaKasaHareketService.UpdateAsync(existing);
                return;
            }
        }

        // Yeni hareket oluştur
        var islemNo = await _bankaKasaHareketService.GenerateNextIslemNoAsync();
        var hareket = new BankaKasaHareket
        {
            IslemNo = islemNo,
            IslemTarihi = masraf.MasrafTarihi,
            HareketTipi = HareketTipi.Cikis,
            Tutar = masraf.Tutar,
            BelgeNo = masraf.BelgeNo,
            Aciklama = aciklama,
            IslemKaynak = IslemKaynak.PersonelCebinden,
            BankaHesapId = masraf.BankaHesapId.Value,
            PersonelCebindenId = masraf.SoforId,
            AracId = masraf.AracId,
            AracMasrafId = masraf.Id,
            MuhasebeAciklama = $"Personel cebinden: {personelAd} - {aracPlaka}"
        };
        await _bankaKasaHareketService.CreateAsync(hareket);
    }
}



