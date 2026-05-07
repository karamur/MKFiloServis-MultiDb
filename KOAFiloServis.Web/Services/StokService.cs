using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class StokService : IStokService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<StokService> _logger;

    public StokService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<StokService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    #region Stok Karti

    public async Task<List<StokKarti>> GetStokKartlariAsync(StokTipi? tip = null, int? kategoriId = null, bool? aktif = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();


        var query = context.StokKartlari
            .AsNoTracking()
            .Include(s => s.Kategori)
            .Include(s => s.VarsayilanTedarikci)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        if (tip.HasValue)
            query = query.Where(s => s.StokTipi == tip.Value);

        if (kategoriId.HasValue)
            query = query.Where(s => s.KategoriId == kategoriId.Value);

        if (aktif.HasValue)
            query = query.Where(s => s.Aktif == aktif.Value);

        return await query.OrderBy(s => s.StokKodu).ToListAsync();
    }

    public async Task<StokKarti?> GetStokKartiByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.StokKartlari
            .Include(s => s.Kategori)
            .Include(s => s.VarsayilanTedarikci)
            .Include(s => s.MuhasebeHesap)
            .Include(s => s.Hareketler.OrderByDescending(h => h.IslemTarihi).Take(10))
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<StokKarti?> GetStokKartiByKodAsync(string kod)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.StokKartlari
            .Include(s => s.Kategori)
            .FirstOrDefaultAsync(s => s.StokKodu == kod && !s.IsDeleted);
    }

    public async Task<StokKarti> CreateStokKartiAsync(StokKarti stok)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrEmpty(stok.StokKodu))
        {
            stok.StokKodu = await GetNextStokKoduAsync(stok.StokTipi);
        }

        stok.CreatedAt = DateTime.UtcNow;
        context.StokKartlari.Add(stok);
        await context.SaveChangesAsync();

        _logger.LogInformation("Stok karti olusturuldu: {StokKodu} - {StokAdi}", stok.StokKodu, stok.StokAdi);
        return stok;
    }

    public async Task<StokKarti> UpdateStokKartiAsync(StokKarti stok)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.StokKartlari.FindAsync(stok.Id);
        if (existing == null)
            throw new Exception("Stok karti bulunamadi");

        existing.StokAdi = stok.StokAdi;
        existing.Aciklama = stok.Aciklama;
        existing.Barkod = stok.Barkod;
        existing.StokTipi = stok.StokTipi;
        existing.AltTipi = stok.AltTipi;
        existing.KategoriId = stok.KategoriId;
        existing.Birim = stok.Birim;
        existing.AlisFiyati = stok.AlisFiyati;
        existing.SatisFiyati = stok.SatisFiyati;
        existing.KdvOrani = stok.KdvOrani;
        existing.StokTakibiYapilsin = stok.StokTakibiYapilsin;
        existing.MinStokMiktari = stok.MinStokMiktari;
        existing.MaksStokMiktari = stok.MaksStokMiktari;
        existing.VarsayilanTedarikciId = stok.VarsayilanTedarikciId;
        existing.MuhasebeHesapId = stok.MuhasebeHesapId;
        existing.Aktif = stok.Aktif;
        existing.ResimUrl = stok.ResimUrl;
        existing.Notlar = stok.Notlar;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteStokKartiAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var stok = await context.StokKartlari.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (stok != null)
        {
            stok.IsDeleted = true;
            stok.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation("Stok karti silindi (soft delete): {StokKodu} - {StokAdi}", stok.StokKodu, stok.StokAdi);
        }
    }

    public async Task<string> GetNextStokKoduAsync(StokTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var prefix = tip switch
        {
            StokTipi.Mal => "MAL",
            StokTipi.Hizmet => "HZM",
            StokTipi.Arac => "ARC",
            StokTipi.YedekParca => "YDP",
            StokTipi.SarfMalzeme => "SRF",
            StokTipi.Demirbas => "DMR",
            _ => "STK"
        };

        var lastStok = await context.StokKartlari
            .IgnoreQueryFilters()
            .Where(s => s.StokKodu.StartsWith(prefix))
            .OrderByDescending(s => s.StokKodu)
            .FirstOrDefaultAsync();

        int nextNum = 1;
        if (lastStok != null)
        {
            var numPart = lastStok.StokKodu.Replace(prefix, "");
            if (int.TryParse(numPart, out var num))
                nextNum = num + 1;
        }

        return $"{prefix}{nextNum:D5}";
    }

    #endregion

    #region Stok Kategori

    public async Task<List<StokKategori>> GetKategorilerAsync(bool? aktif = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.StokKategoriler
            .Include(k => k.UstKategori)
            .Include(k => k.AltKategoriler)
            .AsQueryable();

        if (aktif.HasValue)
            query = query.Where(k => k.Aktif == aktif.Value);

        return await query.OrderBy(k => k.Sira).ThenBy(k => k.KategoriAdi).ToListAsync();
    }

    public async Task<StokKategori?> GetKategoriByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.StokKategoriler
            .Include(k => k.UstKategori)
            .Include(k => k.AltKategoriler)
            .Include(k => k.StokKartlari)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<StokKategori> CreateKategoriAsync(StokKategori kategori)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        kategori.CreatedAt = DateTime.UtcNow;
        context.StokKategoriler.Add(kategori);
        await context.SaveChangesAsync();
        return kategori;
    }

    public async Task<StokKategori> UpdateKategoriAsync(StokKategori kategori)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.StokKategoriler.FindAsync(kategori.Id);
        if (existing == null)
            throw new Exception("Kategori bulunamadi");

        existing.KategoriAdi = kategori.KategoriAdi;
        existing.Aciklama = kategori.Aciklama;
        existing.UstKategoriId = kategori.UstKategoriId;
        existing.Renk = kategori.Renk;
        existing.Icon = kategori.Icon;
        existing.Sira = kategori.Sira;
        existing.Aktif = kategori.Aktif;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteKategoriAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kategori = await context.StokKategoriler.FindAsync(id);
        if (kategori != null)
        {
            kategori.IsDeleted = true;
            kategori.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Stok Hareket

    public async Task<List<StokHareket>> GetStokHareketleriAsync(int? stokKartiId = null, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.StokHareketler
            .Include(h => h.StokKarti)
            .Include(h => h.Cari)
            .Include(h => h.Arac)
            .AsQueryable();

        if (stokKartiId.HasValue)
            query = query.Where(h => h.StokKartiId == stokKartiId.Value);

        if (baslangic.HasValue)
            query = query.Where(h => h.IslemTarihi >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(h => h.IslemTarihi <= bitis.Value);

        return await query.OrderByDescending(h => h.IslemTarihi).ToListAsync();
    }

    public async Task<StokHareket> CreateStokHareketAsync(StokHareket hareket)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        hareket.Miktar = NormalizeMiktar(hareket.HareketTipi, hareket.Miktar);
        hareket.IslemTarihi = DateTime.SpecifyKind(hareket.IslemTarihi, DateTimeKind.Utc);
        hareket.CreatedAt = DateTime.UtcNow;
        context.StokHareketler.Add(hareket);
        await context.SaveChangesAsync();

        // Stok miktarini guncelle
        await UpdateStokMiktariAsync(hareket.StokKartiId);

        await CreateMuhasebeFisiForStokHareketAsync(context, hareket);

        return hareket;
    }

    public async Task<StokHareket> CreateStokOperasyonAsync(StokOperasyonModel operasyon)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var stok = await context.StokKartlari.FirstOrDefaultAsync(s => s.Id == operasyon.StokKartiId && !s.IsDeleted);
        if (stok == null)
            throw new Exception("Stok kartı bulunamadı");

        decimal hareketMiktari = operasyon.HareketTipi switch
        {
            StokHareketTipi.SayimFazlasi => operasyon.Miktar - stok.MevcutStok,
            StokHareketTipi.SayimNoksani => stok.MevcutStok - operasyon.Miktar,
            _ => operasyon.Miktar
        };

        if (hareketMiktari <= 0)
            throw new Exception("İşlem miktarı 0'dan büyük olmalıdır.");

        var hareket = new StokHareket
        {
            StokKartiId = operasyon.StokKartiId,
            IslemTarihi = operasyon.IslemTarihi,
            HareketTipi = operasyon.HareketTipi,
            Miktar = hareketMiktari,
            BirimFiyat = operasyon.BirimFiyat > 0 ? operasyon.BirimFiyat : stok.AlisFiyati,
            BelgeNo = operasyon.BelgeNo,
            Aciklama = operasyon.Aciklama,
            CariId = operasyon.CariId
        };

        return await CreateStokHareketAsync(hareket);
    }

    public async Task CreateUretimRecetesiAsync(UretimReceteModel recete)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (recete.Kalemler == null || !recete.Kalemler.Any())
            throw new Exception("Üretim reçetesi için en az bir bileşen seçilmelidir.");

        if (recete.MamulMiktari <= 0)
            throw new Exception("Mamul miktarı 0'dan büyük olmalıdır.");

        // ExecutionStrategy ile transaction sarmalama (NpgsqlRetryingExecutionStrategy uyumluluğu)
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var hareketler = new List<StokHareket>();
                decimal toplamMaliyet = 0;

                foreach (var kalem in recete.Kalemler.Where(k => k.StokKartiId > 0 && k.Miktar > 0))
                {
                    var stok = await context.StokKartlari.FirstOrDefaultAsync(s => s.Id == kalem.StokKartiId && !s.IsDeleted);
                    if (stok == null)
                        throw new Exception($"Bileşen stok bulunamadı: {kalem.StokKartiId}");

                    var birimFiyat = kalem.BirimFiyat > 0 ? kalem.BirimFiyat : stok.AlisFiyati;
                    toplamMaliyet += kalem.Miktar * birimFiyat;

                    hareketler.Add(new StokHareket
                    {
                        StokKartiId = kalem.StokKartiId,
                        HareketTipi = StokHareketTipi.UretimCikis,
                        IslemTarihi = DateTime.SpecifyKind(recete.IslemTarihi, DateTimeKind.Utc),
                        Miktar = -Math.Abs(kalem.Miktar),
                        BirimFiyat = birimFiyat,
                        BelgeNo = recete.BelgeNo,
                        Aciklama = $"Üretim Reçetesi Çıkışı - {recete.Aciklama}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                var mamulStok = await context.StokKartlari.FirstOrDefaultAsync(s => s.Id == recete.MamulStokKartiId && !s.IsDeleted);
                if (mamulStok == null)
                    throw new Exception("Mamul stok kartı bulunamadı");

                var mamulBirimMaliyet = recete.MamulBirimMaliyeti > 0
                    ? recete.MamulBirimMaliyeti
                    : (toplamMaliyet / recete.MamulMiktari);

                hareketler.Add(new StokHareket
                {
                    StokKartiId = recete.MamulStokKartiId,
                    HareketTipi = StokHareketTipi.UretimGiris,
                    IslemTarihi = DateTime.SpecifyKind(recete.IslemTarihi, DateTimeKind.Utc),
                    Miktar = Math.Abs(recete.MamulMiktari),
                    BirimFiyat = mamulBirimMaliyet,
                    BelgeNo = recete.BelgeNo,
                    Aciklama = $"Üretim Reçetesi Girişi - {recete.Aciklama}",
                    CreatedAt = DateTime.UtcNow
                });

                context.StokHareketler.AddRange(hareketler);
                await context.SaveChangesAsync();

                var stokIds = hareketler.Select(h => h.StokKartiId).Distinct().ToList();
                foreach (var stokId in stokIds)
                    await UpdateStokMiktariAsync(stokId);

                await CreateMuhasebeFisiForUretimAsync(context, recete, hareketler, toplamMaliyet);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task UpdateStokMiktariAsync(int stokKartiId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var stok = await context.StokKartlari.FindAsync(stokKartiId);
        if (stok == null) return;

        var toplamMiktar = await context.StokHareketler
            .Where(h => h.StokKartiId == stokKartiId)
            .SumAsync(h => h.Miktar);

        stok.MevcutStok = toplamMiktar;
        stok.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<decimal> GetMevcutStokAsync(int stokKartiId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.StokHareketler
            .Where(h => h.StokKartiId == stokKartiId)
            .SumAsync(h => h.Miktar);
    }

    private static decimal NormalizeMiktar(StokHareketTipi hareketTipi, decimal miktar)
    {
        var abs = Math.Abs(miktar);
        return hareketTipi switch
        {
            StokHareketTipi.Giris or
            StokHareketTipi.SatisIade or
            StokHareketTipi.SayimFazlasi or
            StokHareketTipi.AracAlis or
            StokHareketTipi.ServisGiris or
            StokHareketTipi.UretimGiris => abs,
            _ => -abs
        };
    }

    private async Task CreateMuhasebeFisiForStokHareketAsync(ApplicationDbContext context, StokHareket hareket)
    {
        var stok = await context.StokKartlari
            .Include(s => s.MuhasebeHesap)
            .FirstOrDefaultAsync(s => s.Id == hareket.StokKartiId);

        if (stok == null)
            return;

        var tutar = Math.Abs(hareket.Miktar) * hareket.BirimFiyat;
        if (tutar <= 0)
            return;

        var stokHesap = await GetOrCreateMuhasebeHesapAsync(context, stok.MuhasebeHesap?.HesapKodu ?? "153", stok.StokAdi, HesapTuru.Aktif, HesapGrubu.DonenVarliklar, "15");

        var karsiKod = hareket.HareketTipi switch
        {
            StokHareketTipi.SayimFazlasi => "397.99.999",
            StokHareketTipi.SayimNoksani => "689.99.001",
            StokHareketTipi.FireZayiat => "689.99.002",
            StokHareketTipi.StokZarari => "689.99.003",
            StokHareketTipi.Diger => "689.99.003",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(karsiKod))
            return;

        var karsiHesap = await GetOrCreateMuhasebeHesapAsync(context, 
            karsiKod,
            GetKarsiHesapAdi(hareket.HareketTipi),
            hareket.HareketTipi == StokHareketTipi.SayimFazlasi ? HesapTuru.Pasif : HesapTuru.Gider,
            hareket.HareketTipi == StokHareketTipi.SayimFazlasi ? HesapGrubu.KisaVadeliYabanciKaynaklar : HesapGrubu.MaliyetHesaplari,
            hareket.HareketTipi == StokHareketTipi.SayimFazlasi ? "39" : "68");

        var fis = new MuhasebeFis
        {
            FisNo = string.Empty,
            FisTarihi = hareket.IslemTarihi,
            FisTipi = FisTipi.Mahsup,
            Aciklama = $"Stok Operasyonu - {stok.StokAdi} - {hareket.HareketTipi}",
            ToplamBorc = tutar,
            ToplamAlacak = tutar,
            Durum = FisDurum.Onaylandi,
            Kaynak = FisKaynak.Otomatik,
            KaynakId = hareket.Id,
            KaynakTip = "StokHareket",
            CreatedAt = DateTime.UtcNow
        };

        var girisIslemi = hareket.Miktar > 0;
        fis.Kalemler.Add(new MuhasebeFisKalem
        {
            HesapId = girisIslemi ? stokHesap.Id : karsiHesap.Id,
            SiraNo = 1,
            Borc = tutar,
            Alacak = 0,
            Tarih = hareket.IslemTarihi,
            Aciklama = hareket.Aciklama,
            CariId = hareket.CariId,
            CreatedAt = DateTime.UtcNow
        });
        fis.Kalemler.Add(new MuhasebeFisKalem
        {
            HesapId = girisIslemi ? karsiHesap.Id : stokHesap.Id,
            SiraNo = 2,
            Borc = 0,
            Alacak = tutar,
            Tarih = hareket.IslemTarihi,
            Aciklama = hareket.Aciklama,
            CariId = hareket.CariId,
            CreatedAt = DateTime.UtcNow
        });

        fis.FisNo = await GenerateNextStokFisNoAsync(context);
        context.MuhasebeFisleri.Add(fis);
        await context.SaveChangesAsync();
    }

    private async Task CreateMuhasebeFisiForUretimAsync(ApplicationDbContext context, UretimReceteModel recete, List<StokHareket> hareketler, decimal toplamMaliyet)
    {
        if (toplamMaliyet <= 0)
            return;

        var mamul = await context.StokKartlari.Include(s => s.MuhasebeHesap).FirstOrDefaultAsync(s => s.Id == recete.MamulStokKartiId);
        if (mamul == null)
            return;

        var mamulHesap = await GetOrCreateMuhasebeHesapAsync(context, mamul.MuhasebeHesap?.HesapKodu ?? "152", mamul.StokAdi, HesapTuru.Aktif, HesapGrubu.DonenVarliklar, "15");
        var uretimKarsi = await GetOrCreateMuhasebeHesapAsync(context, "711.99.999", "Üretimden Mamule Aktarım", HesapTuru.Maliyet, HesapGrubu.MaliyetHesaplari, "71");

        var fis = new MuhasebeFis
        {
            FisNo = string.Empty,
            FisTarihi = DateTime.SpecifyKind(recete.IslemTarihi, DateTimeKind.Utc),
            FisTipi = FisTipi.Mahsup,
            Aciklama = $"Üretim Reçetesi - {mamul.StokAdi}",
            ToplamBorc = toplamMaliyet,
            ToplamAlacak = toplamMaliyet,
            Durum = FisDurum.Onaylandi,
            Kaynak = FisKaynak.Otomatik,
            KaynakTip = "UretimRecete",
            CreatedAt = DateTime.UtcNow
        };

        fis.Kalemler.Add(new MuhasebeFisKalem
        {
            HesapId = mamulHesap.Id,
            SiraNo = 1,
            Borc = toplamMaliyet,
            Alacak = 0,
            Tarih = recete.IslemTarihi,
            Aciklama = recete.Aciklama,
            CreatedAt = DateTime.UtcNow
        });

        fis.Kalemler.Add(new MuhasebeFisKalem
        {
            HesapId = uretimKarsi.Id,
            SiraNo = 2,
            Borc = 0,
            Alacak = toplamMaliyet,
            Tarih = recete.IslemTarihi,
            Aciklama = recete.Aciklama,
            CreatedAt = DateTime.UtcNow
        });

        fis.FisNo = await GenerateNextStokFisNoAsync(context);
        context.MuhasebeFisleri.Add(fis);
        await context.SaveChangesAsync();
    }

    private async Task<MuhasebeHesap> GetOrCreateMuhasebeHesapAsync(ApplicationDbContext context, string hesapKodu, string hesapAdi, HesapTuru hesapTuru, HesapGrubu hesapGrubu, string ustKod)
    {
        var hesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == hesapKodu);
        if (hesap != null)
            return hesap;

        var ustHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == ustKod);
        hesap = new MuhasebeHesap
        {
            HesapKodu = hesapKodu,
            HesapAdi = hesapAdi,
            HesapTuru = hesapTuru,
            HesapGrubu = hesapGrubu,
            UstHesapId = ustHesap?.Id,
            Aktif = true,
            CreatedAt = DateTime.UtcNow
        };
        context.MuhasebeHesaplari.Add(hesap);
        await context.SaveChangesAsync();
        return hesap;
    }

    private static async Task<string> GenerateNextStokFisNoAsync(ApplicationDbContext context)
    {
        var yilAy = $"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month:D2}";
        var sonNo = await MuhasebeService.NextFisNoCounterAsync(context, "STK", yilAy);
        return $"STK-{yilAy}-{sonNo:D4}";
    }

    private static string GetKarsiHesapAdi(StokHareketTipi hareketTipi) => hareketTipi switch
    {
        StokHareketTipi.SayimFazlasi => "Sayım Fazlası Karşılık Hesabı",
        StokHareketTipi.SayimNoksani => "Sayım Noksanı Gideri",
        StokHareketTipi.FireZayiat => "Fire ve Zayiat Gideri",
        StokHareketTipi.StokZarari => "Stok Zarar Gideri",
        _ => "Stok Zarar Gideri"
    };

    #endregion

    #region Arac Islem (Alis/Satis)

    public async Task<List<AracIslem>> GetAracIslemleriAsync(int? aracId = null, AracIslemTipi? tip = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AracIslemler
            .Include(i => i.Arac)
            .Include(i => i.Cari)
            .Include(i => i.Fatura)
            .AsQueryable();

        if (aracId.HasValue)
            query = query.Where(i => i.AracId == aracId.Value);

        if (tip.HasValue)
            query = query.Where(i => i.IslemTipi == tip.Value);

        return await query.OrderByDescending(i => i.IslemTarihi).ToListAsync();
    }

    public async Task<AracIslem?> GetAracIslemByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracIslemler
            .Include(i => i.Arac)
            .Include(i => i.Cari)
            .Include(i => i.Fatura)
            .Include(i => i.StokHareket)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<AracIslem> CreateAracIslemAsync(AracIslem islem)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // KDV hesapla
        islem.KdvTutar = islem.Tutar * islem.KdvOrani / 100;
        islem.ToplamTutar = islem.Tutar + islem.KdvTutar;
        islem.CreatedAt = DateTime.UtcNow;

        context.AracIslemler.Add(islem);
        await context.SaveChangesAsync();

        // Arac durumunu guncelle
        var arac = await context.Araclar.FindAsync(islem.AracId);
        if (arac != null)
        {
            if (islem.IslemTipi == AracIslemTipi.Satis)
            {
                arac.Aktif = false;
                arac.SatisaAcik = false;
            }
            else if (islem.IslemTipi == AracIslemTipi.Alis)
            {
                arac.Aktif = true;
            }

            if (islem.Kilometre.HasValue)
            {
                arac.KmDurumu = islem.Kilometre;
            }

            await context.SaveChangesAsync();
        }

        _logger.LogInformation("Arac islemi olusturuldu: {IslemTipi} - Arac ID: {AracId}", islem.IslemTipi, islem.AracId);
        return islem;
    }

    public async Task<AracIslem> UpdateAracIslemAsync(AracIslem islem)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.AracIslemler.FindAsync(islem.Id);
        if (existing == null)
            throw new Exception("Arac islemi bulunamadi");

        existing.IslemTipi = islem.IslemTipi;
        existing.IslemTarihi = islem.IslemTarihi;
        existing.CariId = islem.CariId;
        existing.Tutar = islem.Tutar;
        existing.KdvOrani = islem.KdvOrani;
        existing.KdvTutar = islem.Tutar * islem.KdvOrani / 100;
        existing.ToplamTutar = islem.Tutar + existing.KdvTutar;
        existing.FaturaId = islem.FaturaId;
        existing.Aciklama = islem.Aciklama;
        existing.Notlar = islem.Notlar;
        existing.Kilometre = islem.Kilometre;
        existing.NoterId = islem.NoterId;
        existing.NoterTarihi = islem.NoterTarihi;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAracIslemAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var islem = await context.AracIslemler.FindAsync(id);
        if (islem != null)
        {
            islem.IsDeleted = true;
            islem.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Servis Kaydi

    public async Task<List<ServisKaydi>> GetServisKayitlariAsync(int? aracId = null, ServisTipi? tip = null, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.ServisKayitlari
            .Include(s => s.Arac)
            .Include(s => s.ServisciCari)
            .Include(s => s.Parcalar)
            .Include(s => s.Fatura)
            .Include(s => s.AracMasraf)
            .AsQueryable();

        if (aracId.HasValue)
            query = query.Where(s => s.AracId == aracId.Value);

        if (tip.HasValue)
            query = query.Where(s => s.ServisTipi == tip.Value);

        if (baslangic.HasValue)
            query = query.Where(s => s.ServisTarihi >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(s => s.ServisTarihi <= bitis.Value);

        return await query.OrderByDescending(s => s.ServisTarihi).ToListAsync();
    }

    public async Task<ServisKaydi?> GetServisKaydiByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ServisKayitlari
            .Include(s => s.Arac)
            .Include(s => s.ServisciCari)
            .Include(s => s.Parcalar)
                .ThenInclude(p => p.StokKarti)
            .Include(s => s.Fatura)
            .Include(s => s.AracMasraf)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<ServisKaydi> CreateServisKaydiAsync(ServisKaydi servis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Kalemleri normalize et ve toplamları hesapla
        servis.Parcalar = NormalizeServisParcalari(servis.Parcalar);
        var (parcaToplami, iscilikToplami, kdvToplami) = HesaplaServisKalemToplamlari(servis.Parcalar);

        servis.ParcaTutari = parcaToplami;
        servis.IscilikTutari = iscilikToplami;

        var toplamNet = servis.IscilikTutari + servis.ParcaTutari;
        if (!servis.KdvManuelMi)
            servis.KdvTutar = kdvToplami;

        servis.KdvOrani = toplamNet > 0 ? Math.Round(servis.KdvTutar / toplamNet * 100, 2) : 0;
        servis.ToplamTutar = toplamNet + servis.KdvTutar;
        servis.CreatedAt = DateTime.UtcNow;

        context.ServisKayitlari.Add(servis);
        await context.SaveChangesAsync();

        await StogaKaydetParcalariAsync(context, servis);
        await context.SaveChangesAsync();

        // Arac km guncelle
        if (servis.Kilometre.HasValue)
        {
            var arac = await context.Araclar.FindAsync(servis.AracId);
            if (arac != null)
            {
                arac.KmDurumu = servis.Kilometre;
                await context.SaveChangesAsync();
            }
        }

        _logger.LogInformation("Servis kaydi olusturuldu: {ServisAdi} - Arac ID: {AracId}", servis.ServisAdi, servis.AracId);
        return servis;
    }

    public async Task<ServisKaydi> UpdateServisKaydiAsync(ServisKaydi servis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ServisKayitlari
            .Include(s => s.Parcalar)
            .FirstOrDefaultAsync(s => s.Id == servis.Id);

        if (existing == null)
            throw new Exception("Servis kaydi bulunamadi");

        existing.ServisTarihi = servis.ServisTarihi;
        existing.ServisciCariId = servis.ServisciCariId;
        existing.ServisTipi = servis.ServisTipi;
        existing.ServisAdi = servis.ServisAdi;
        existing.Aciklama = servis.Aciklama;
        existing.KdvManuelMi = servis.KdvManuelMi;
        existing.KdvOrani = servis.KdvOrani;
        existing.Kilometre = servis.Kilometre;
        existing.Durum = servis.Durum;
        existing.GarantiKapsaminda = servis.GarantiKapsaminda;
        existing.GarantiBitisTarihi = servis.GarantiBitisTarihi;
        existing.Notlar = servis.Notlar;

        // Parcalari guncelle
        context.ServisParcalar.RemoveRange(existing.Parcalar);
        var normalizedParcalar = NormalizeServisParcalari(servis.Parcalar);
        if (normalizedParcalar.Any())
        {
            foreach (var parca in normalizedParcalar)
            {
                parca.ServisKaydiId = existing.Id;
                context.ServisParcalar.Add(parca);
            }
        }

        // Toplam tutar hesapla
        var (parcaToplami, iscilikToplami, kdvToplami) = HesaplaServisKalemToplamlari(normalizedParcalar);
        existing.ParcaTutari = parcaToplami;
        existing.IscilikTutari = iscilikToplami;
        var toplamNet = existing.IscilikTutari + existing.ParcaTutari;
        existing.KdvTutar = existing.KdvManuelMi ? servis.KdvTutar : kdvToplami;
        existing.KdvOrani = toplamNet > 0 ? Math.Round(existing.KdvTutar / toplamNet * 100, 2) : 0;
        existing.ToplamTutar = toplamNet + existing.KdvTutar;
        existing.UpdatedAt = DateTime.UtcNow;

        // Güncellemede aynı servis için daha önce oluşturulmuş stoğa kaydet girişlerini temizle
        await GeriAlServisStokGirisleriAsync(context, existing.Id);
        existing.Parcalar = normalizedParcalar;
        await StogaKaydetParcalariAsync(context, existing);

        await context.SaveChangesAsync();
        return existing;
    }

    private static List<ServisParca> NormalizeServisParcalari(IEnumerable<ServisParca>? parcalar)
    {
        if (parcalar == null)
            return new List<ServisParca>();

        return parcalar
            .Where(p => !string.IsNullOrWhiteSpace(p.ParcaAdi))
            .Select(p => new ServisParca
            {
                ServisKaydiId = p.ServisKaydiId,
                StokKartiId = p.StokKartiId,
                KalemTipi = p.KalemTipi,
                ParcaAdi = p.ParcaAdi.Trim(),
                Miktar = p.Miktar <= 0 ? 1 : p.Miktar,
                Birim = string.IsNullOrWhiteSpace(p.Birim) ? "Adet" : p.Birim.Trim(),
                BirimFiyat = p.BirimFiyat,
                KdvOrani = p.KdvOrani,
                StogaKaydet = p.KalemTipi == ServisKalemTipi.Malzeme && p.StogaKaydet,
                Aciklama = p.Aciklama
            })
            .ToList();
    }

    private static (decimal ParcaToplami, decimal IscilikToplami, decimal KdvToplami) HesaplaServisKalemToplamlari(IEnumerable<ServisParca> parcalar)
    {
        decimal parcaToplami = 0;
        decimal iscilikToplami = 0;
        decimal kdvToplami = 0;

        foreach (var kalem in parcalar)
        {
            var kalemNet = kalem.Miktar * kalem.BirimFiyat;
            kdvToplami += kalemNet * kalem.KdvOrani / 100;

            if (kalem.KalemTipi == ServisKalemTipi.Iscilik)
                iscilikToplami += kalemNet;
            else
                parcaToplami += kalemNet;
        }

        return (parcaToplami, iscilikToplami, kdvToplami);
    }

    private async Task StogaKaydetParcalariAsync(ApplicationDbContext context, ServisKaydi servis)
    {
        if (servis.Parcalar == null || !servis.Parcalar.Any())
            return;

        var belgeNo = $"SRV-{servis.Id}";
        var stokParcalari = servis.Parcalar
            .Where(p => p.KalemTipi == ServisKalemTipi.Malzeme && p.StogaKaydet && p.Miktar > 0)
            .ToList();

        foreach (var parca in stokParcalari)
        {
            var stokKarti = await context.StokKartlari
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.StokAdi == parca.ParcaAdi);

            if (stokKarti == null)
            {
                stokKarti = new StokKarti
                {
                    StokKodu = await GetNextStokKoduInternalAsync(context, StokTipi.YedekParca),
                    StokAdi = parca.ParcaAdi,
                    Aciklama = "Yeni Servis ekranından otomatik oluşturuldu",
                    StokTipi = StokTipi.YedekParca,
                    AltTipi = StokAltTipi.Diger,
                    Birim = string.IsNullOrWhiteSpace(parca.Birim) ? "Adet" : parca.Birim,
                    AlisFiyati = parca.BirimFiyat,
                    SatisFiyati = parca.BirimFiyat,
                    KdvOrani = parca.KdvOrani,
                    StokTakibiYapilsin = true,
                    Aktif = true,
                    VarsayilanTedarikciId = servis.ServisciCariId,
                    CreatedAt = DateTime.UtcNow
                };
                context.StokKartlari.Add(stokKarti);
                await context.SaveChangesAsync();
            }

            var hareket = new StokHareket
            {
                StokKartiId = stokKarti.Id,
                IslemTarihi = DateTime.SpecifyKind(servis.ServisTarihi, DateTimeKind.Utc),
                BelgeNo = belgeNo,
                HareketTipi = StokHareketTipi.ServisGiris,
                Miktar = parca.Miktar,
                BirimFiyat = parca.BirimFiyat,
                CariId = servis.ServisciCariId,
                AracId = servis.AracId,
                AracMasrafId = servis.AracMasrafId,
                Aciklama = $"Servis kaydı stoğa kayıt: {servis.ServisAdi} - {parca.ParcaAdi}",
                CreatedAt = DateTime.UtcNow
            };

            context.StokHareketler.Add(hareket);
            stokKarti.MevcutStok += parca.Miktar;
            stokKarti.AlisFiyati = parca.BirimFiyat;
            stokKarti.KdvOrani = parca.KdvOrani;
            stokKarti.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task GeriAlServisStokGirisleriAsync(ApplicationDbContext context, int servisKaydiId)
    {
        var belgeNo = $"SRV-{servisKaydiId}";
        var hareketler = await context.StokHareketler
            .Where(h => !h.IsDeleted && h.BelgeNo == belgeNo && h.HareketTipi == StokHareketTipi.ServisGiris)
            .ToListAsync();

        if (!hareketler.Any())
            return;

        var stokIds = hareketler.Select(h => h.StokKartiId).Distinct().ToList();
        var stoklar = await context.StokKartlari
            .Where(s => stokIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        foreach (var hareket in hareketler)
        {
            if (stoklar.TryGetValue(hareket.StokKartiId, out var stok))
            {
                stok.MevcutStok -= hareket.Miktar;
                stok.UpdatedAt = DateTime.UtcNow;
            }

            hareket.IsDeleted = true;
            hareket.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static async Task<string> GetNextStokKoduInternalAsync(ApplicationDbContext context, StokTipi tip)
    {
        var prefix = tip switch
        {
            StokTipi.Mal => "MAL",
            StokTipi.Hizmet => "HZM",
            StokTipi.Arac => "ARC",
            StokTipi.YedekParca => "YDP",
            StokTipi.SarfMalzeme => "SRF",
            StokTipi.Demirbas => "DMR",
            _ => "STK"
        };

        var lastStok = await context.StokKartlari
            .IgnoreQueryFilters()
            .Where(s => s.StokKodu.StartsWith(prefix))
            .OrderByDescending(s => s.StokKodu)
            .FirstOrDefaultAsync();

        int nextNum = 1;
        if (lastStok != null)
        {
            var numPart = lastStok.StokKodu.Replace(prefix, "");
            if (int.TryParse(numPart, out var num))
                nextNum = num + 1;
        }

        return $"{prefix}{nextNum:D5}";
    }

    public async Task DeleteServisKaydiAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var servis = await context.ServisKayitlari.FindAsync(id);
        if (servis != null)
        {
            servis.IsDeleted = true;
            servis.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Dashboard

    public async Task<StokDashboard> GetDashboardAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var dashboard = new StokDashboard();

        var buAyBaslangic = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var buAyBitis = buAyBaslangic.AddMonths(1).AddDays(-1);

        dashboard.ToplamStokKarti = await context.StokKartlari.CountAsync();
        dashboard.AktifStokKarti = await context.StokKartlari.CountAsync(s => s.Aktif);

        dashboard.DusukStoklu = await context.StokKartlari
            .Where(s => s.StokTakibiYapilsin && s.MevcutStok <= s.MinStokMiktari)
            .CountAsync();

        dashboard.ToplamStokDegeri = await context.StokKartlari
            .Where(s => s.StokTakibiYapilsin)
            .SumAsync(s => s.MevcutStok * s.AlisFiyati);

        dashboard.AylikAracAlis = await context.AracIslemler
            .Where(i => i.IslemTipi == AracIslemTipi.Alis && 
                       i.IslemTarihi >= buAyBaslangic && i.IslemTarihi <= buAyBitis)
            .CountAsync();

        dashboard.AylikAracSatis = await context.AracIslemler
            .Where(i => i.IslemTipi == AracIslemTipi.Satis && 
                       i.IslemTarihi >= buAyBaslangic && i.IslemTarihi <= buAyBitis)
            .CountAsync();

        dashboard.AylikServisKaydi = await context.ServisKayitlari
            .Where(s => s.ServisTarihi >= buAyBaslangic && s.ServisTarihi <= buAyBitis)
            .CountAsync();

        dashboard.AylikServisTutari = await context.ServisKayitlari
            .Where(s => s.ServisTarihi >= buAyBaslangic && s.ServisTarihi <= buAyBitis)
            .SumAsync(s => s.ToplamTutar);

        dashboard.SonHareketler = await context.StokHareketler
            .Include(h => h.StokKarti)
            .OrderByDescending(h => h.IslemTarihi)
            .Take(10)
            .ToListAsync();

        dashboard.SonServisler = await context.ServisKayitlari
            .Include(s => s.Arac)
            .OrderByDescending(s => s.ServisTarihi)
            .Take(10)
            .ToListAsync();

        return dashboard;
    }

    #endregion
}
