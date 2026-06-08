using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

public class BudgetService : IBudgetService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IBankaKasaHareketService _bankaKasaHareketService;
    private readonly IMuhasebeService _muhasebeService;
    private readonly ILogger<BudgetService> _logger;
    private static readonly string[] AyAdlari = { "", "Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", 
                                                   "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik" };

    public BudgetService(IDbContextFactory<ApplicationDbContext> contextFactory, IBankaKasaHareketService bankaKasaHareketService, IMuhasebeService muhasebeService, ILogger<BudgetService> logger)
    {
        _contextFactory = contextFactory;
        _bankaKasaHareketService = bankaKasaHareketService;
        _muhasebeService = muhasebeService;
        _logger = logger;
    }

    #region Odeme Islemleri

    /// <summary>
    /// Dashboard ve BudgetAnaliz için ödeme listesi. AsNoTracking — salt okunur,
    /// navigation property yüklemez (UI entity'leri DTO gibi kullanır).
    /// </summary>
    public async Task<List<BudgetOdeme>> GetOdemelerAsync(int yil, int? ay = null, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.BudgetOdemeler
            .AsNoTracking()
            .Where(o => o.OdemeYil == yil);

        if (ay.HasValue)
            query = query.Where(o => o.OdemeAy == ay.Value);

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value || o.FirmaId == null);

        var odemeler = await query
            .OrderBy(o => o.OdemeAy)
            .ThenBy(o => o.OdemeTarihi)
            .ToListAsync();

        await DoldurOdemeHareketIzleriAsync(context, odemeler);
        return odemeler;
    }

    /// <summary>
    /// Sadece bekleyen odemeleri getirir (Odenmis ve fatura ile kapatilmis olanlar haric)
    /// </summary>
    public async Task<List<BudgetOdeme>> GetBekleyenOdemelerAsync(int yil, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.BudgetOdemeler
            .Where(o => o.OdemeYil == yil && 
                        (o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi));

        if (ay.HasValue)
            query = query.Where(o => o.OdemeAy == ay.Value);

        return await query
            .OrderBy(o => o.OdemeAy)
            .ThenBy(o => o.OdemeTarihi)
            .ToListAsync();
    }

    public async Task<List<BudgetOdeme>> GetDevirBekleyenOdemelerAsync(DateTime donemBaslangic, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var baslangicUtc = DateTime.SpecifyKind(donemBaslangic.Date, DateTimeKind.Utc);

        var query = context.BudgetOdemeler
            .Where(o => (o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi)
                        && o.OdemeTarihi < baslangicUtc
                        && !o.KalanSonrakiDonemeAktarilsin); // Zaten aktarılmış olanları gösterme

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value);

        return await query
            .AsNoTracking()
            .OrderBy(o => o.OdemeTarihi)
            .ThenBy(o => o.MasrafKalemi)
            .ToListAsync();
    }

    public async Task<List<BudgetOdeme>> GetOdemelerByDateRangeAsync(DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var baslangicUtc = DateTime.SpecifyKind(baslangic.Date, DateTimeKind.Utc);
        var bitisUtc = DateTime.SpecifyKind(bitis.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        return await context.BudgetOdemeler
            .Where(o => o.OdemeTarihi >= baslangicUtc && o.OdemeTarihi <= bitisUtc)
            .OrderBy(o => o.OdemeTarihi)
            .ToListAsync();
    }

    public async Task<BudgetOdeme?> GetOdemeByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BudgetOdemeler.FindAsync(id);
    }

    public async Task<BudgetOdeme?> GetOdemeByHareketIdAsync(int bankaKasaHareketId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BudgetOdemeler
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.BankaKasaHareketId == bankaKasaHareketId);
    }

    private async Task DoldurOdemeHareketIzleriAsync(ApplicationDbContext context, List<BudgetOdeme> odemeler)
    {
        var hareketIds = odemeler
            .Where(o => o.BankaKasaHareketId.HasValue)
            .Select(o => o.BankaKasaHareketId!.Value)
            .Distinct()
            .ToList();

        if (!hareketIds.Any())
            return;

        var hareketler = await context.BankaKasaHareketleri
            .AsNoTracking()
            .Include(h => h.Cari) // Cari?.Unvan için gerekli
            .Where(h => hareketIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id);

        foreach (var odeme in odemeler)
        {
            if (!odeme.BankaKasaHareketId.HasValue || !hareketler.TryGetValue(odeme.BankaKasaHareketId.Value, out var hareket))
                continue;

            odeme.HareketKaynakGorunumu = hareket.IslemKaynak switch
            {
                IslemKaynak.CariMahsup => "Cari Mahsup",
                IslemKaynak.Mahsup => "Hesap Mahsup",
                IslemKaynak.Butce => "Bütçe Ödemesi",
                _ => null
            };

            if (hareket.IslemKaynak == IslemKaynak.CariMahsup)
            {
                odeme.HareketCariUnvani = hareket.Cari?.Unvan;
                odeme.HareketYonGorunumu = hareket.HareketTipi == HareketTipi.Giris
                    ? "Tahsilat (Cari → Hesap)"
                    : "Ödeme (Hesap → Cari)";
                odeme.HareketBelgeNo = hareket.BelgeNo;
            }
        }
    }

    public async Task<BudgetOdeme> CreateOdemeAsync(BudgetOdeme odeme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // DateTime'i UTC olarak ayarla
        odeme.OdemeTarihi = DateTime.SpecifyKind(odeme.OdemeTarihi, DateTimeKind.Utc);
        odeme.Miktar = Math.Abs(odeme.Miktar);

        // FirmaId=0 FK hatasina yol acmasin diye null'a cevir
        if (odeme.FirmaId <= 0) odeme.FirmaId = null;

        // Varsayilan degerler
        odeme.OdemeAy = odeme.OdemeTarihi.Month;
        odeme.OdemeYil = odeme.OdemeTarihi.Year;

        if (!odeme.TaksitliMi)
        {
            odeme.ToplamTaksitSayisi = 1;
            odeme.KacinciTaksit = 1;
        }

        odeme.CreatedAt = DateTime.UtcNow;

        context.BudgetOdemeler.Add(odeme);
        await context.SaveChangesAsync();
        return odeme;
    }

    public async Task<BudgetOdeme> UpdateOdemeAsync(BudgetOdeme odeme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // DateTime'i UTC olarak ayarla
        var odemeTarihi = DateTime.SpecifyKind(odeme.OdemeTarihi, DateTimeKind.Utc);
        var miktar = Math.Abs(odeme.Miktar);
        var updatedAt = DateTime.UtcNow;

        // FirmaId=0 FK hatasina yol acmasin diye null'a cevir
        if (odeme.FirmaId <= 0) odeme.FirmaId = null;

        // Doğrudan veritabanında güncelle (tracking sorunu olmaz)
        await context.BudgetOdemeler
            .Where(o => o.Id == odeme.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.OdemeTarihi, odemeTarihi)
                .SetProperty(o => o.OdemeAy, odemeTarihi.Month)
                .SetProperty(o => o.OdemeYil, odemeTarihi.Year)
                .SetProperty(o => o.MasrafKalemi, odeme.MasrafKalemi)
                .SetProperty(o => o.Aciklama, odeme.Aciklama)
                .SetProperty(o => o.Miktar, miktar)
                .SetProperty(o => o.Durum, odeme.Durum)
                .SetProperty(o => o.FirmaId, odeme.FirmaId)
                .SetProperty(o => o.MasrafKesintisi, odeme.MasrafKesintisi)
                .SetProperty(o => o.CezaKesintisi, odeme.CezaKesintisi)
                .SetProperty(o => o.DigerKesinti, odeme.DigerKesinti)
                .SetProperty(o => o.KesintiAciklamasi, odeme.KesintiAciklamasi)
                .SetProperty(o => o.GercekOdemeTarihi, odeme.GercekOdemeTarihi)
                .SetProperty(o => o.OdemeYapildigiHesapId, odeme.OdemeYapildigiHesapId)
                .SetProperty(o => o.OdenenTutar, odeme.OdenenTutar)
                .SetProperty(o => o.OdemeNotu, odeme.OdemeNotu)
                .SetProperty(o => o.BankaKasaHareketId, odeme.BankaKasaHareketId)
                .SetProperty(o => o.FaturaId, odeme.FaturaId)
                .SetProperty(o => o.FaturaIleKapatildi, odeme.FaturaIleKapatildi)
                .SetProperty(o => o.Notlar, odeme.Notlar)
                .SetProperty(o => o.UpdatedAt, updatedAt));
        
        // Güncellenmiş entity'yi döndür
        odeme.OdemeTarihi = odemeTarihi;
        odeme.OdemeAy = odemeTarihi.Month;
        odeme.OdemeYil = odemeTarihi.Year;
        odeme.Miktar = miktar;
        odeme.UpdatedAt = updatedAt;
        
        return odeme;
    }

    /// <summary>
    /// Soft delete - silinen hicbir yerde gorunmez
    /// </summary>
    public async Task DeleteOdemeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.BudgetOdemeler.FindAsync(id);
        if (odeme != null)
        {
            odeme.IsDeleted = true;
            odeme.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Kalici silme - veritabanindan tamamen siler
    /// </summary>
    public async Task HardDeleteOdemeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.BudgetOdemeler
            .IgnoreQueryFilters() // Soft delete filtresini atla
            .FirstOrDefaultAsync(o => o.Id == id);
            
        if (odeme != null)
        {
            context.BudgetOdemeler.Remove(odeme);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Kasa'dan odeme yapildiginda:
    /// - Kasa = Borc (Cikis)
    /// - Odeme kaydi = Alacak olarak islenir
    /// Kredi Karti odemelerinde kesintiler (masraf, faiz vb.) borca EKLENIR
    /// </summary>
    public async Task<BudgetOdeme> OdemeYapAsync(int odemeId, OdemeYapRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.BudgetOdemeler.AsNoTracking().FirstOrDefaultAsync(o => o.Id == odemeId);
        if (odeme == null)
            throw new Exception("Odeme bulunamadi");

        var odemeTutari = RoundCurrency(Math.Abs(request.KismiOdemeTutari ?? odeme.Miktar));
        var odemeTarihi = DateTime.SpecifyKind(request.OdemeTarihi, DateTimeKind.Utc);

        // Ek masraflar her zaman pozitif olmalı (mutlak değer al)
        var masrafKesintisi = RoundCurrency(Math.Abs(request.MasrafKesintisi));
        var cezaKesintisi = RoundCurrency(Math.Abs(request.CezaKesintisi));
        var digerKesinti = RoundCurrency(Math.Abs(request.DigerKesinti));

        var toplamEkMasraf = masrafKesintisi + cezaKesintisi + digerKesinti;

        // Ek masraflar (ceza, faiz, komisyon) her zaman tutara EKLENIR
        decimal netOdemeTutari = RoundCurrency(odemeTutari + toplamEkMasraf);

        if (netOdemeTutari <= 0)
            throw new Exception("Net ödeme tutarı sıfırdan büyük olmalıdır.");

        var odemeNotu = request.OdemeNotu ?? request.Aciklama;
        var updatedAt = DateTime.UtcNow;
        int? bankaKasaHareketId = null;

        BankaKasaHareket? kaydedilenHareket = null;

        // Cari Mahsup ödeme tipi için özel işlem
        if (request.OdemeTipi == OdemeTipi.CariMahsup)
        {
            if (!request.CariId.HasValue)
                throw new Exception("Cari mahsup için cari seçilmelidir.");
            if (!request.BankaHesapId.HasValue)
                throw new Exception("Cari mahsup için hesap seçilmelidir.");

            var aciklamaBuilder = $"Bütçe Ödemesi: {odeme.MasrafKalemi}";
            if (!string.IsNullOrEmpty(odemeNotu))
                aciklamaBuilder += $" - {odemeNotu}";
            if (toplamEkMasraf != 0)
            {
                aciklamaBuilder += $" (Ek Masraf: {toplamEkMasraf:N2} ₺)";
            }

            // CariMahsup: Biz cariye borçluyuz, ödeme yapıyoruz (caridenHesaba = false)
            // veya Cari bize borçlu, tahsil ediyoruz (caridenHesaba = true = CaridenTahsilat)
            var sonuc = await _bankaKasaHareketService.CariMahsupAsync(
                request.CariId.Value,
                request.BankaHesapId.Value,
                netOdemeTutari,
                odemeTarihi,
                aciklamaBuilder,
                request.CaridenTahsilat,
                null,
                request.MuhasebeHesapKodu,
                request.KostMerkeziKodu,
                request.ProjeKodu
            );

            if (!sonuc.Basarili)
                throw new Exception($"Cari mahsup işlemi başarısız: {sonuc.Hata}");

            // Hareket ID'sini kaydet
            if (sonuc.KaynakHareket != null)
                bankaKasaHareketId = sonuc.KaynakHareket.Id;

            // Ödeme durumunu güncelle
            await context.BudgetOdemeler
                .Where(o => o.Id == odemeId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Durum, OdemeDurum.Odendi)
                    .SetProperty(o => o.GercekOdemeTarihi, odemeTarihi)
                    .SetProperty(o => o.OdenenTutar, netOdemeTutari)
                    .SetProperty(o => o.OdemeYapildigiHesapId, request.BankaHesapId)
                    .SetProperty(o => o.OdemeNotu, odemeNotu)
                    .SetProperty(o => o.MasrafKesintisi, masrafKesintisi)
                    .SetProperty(o => o.CezaKesintisi, cezaKesintisi)
                    .SetProperty(o => o.DigerKesinti, digerKesinti)
                    .SetProperty(o => o.KesintiAciklamasi, request.KesintiAciklamasi)
                    .SetProperty(o => o.BankaKasaHareketId, bankaKasaHareketId)
                    .SetProperty(o => o.UpdatedAt, updatedAt));
        }
        // Kasa/Banka/KrediKarti hareketi olustur (Mahsup ve CariMahsup disinda)
        else if (request.OdemeTipi != OdemeTipi.Mahsup && request.BankaHesapId.HasValue)
        {
            var aciklamaBuilder = $"Bütçe Ödemesi: {odeme.MasrafKalemi}";
            if (!string.IsNullOrEmpty(odemeNotu))
                aciklamaBuilder += $" - {odemeNotu}";
            if (toplamEkMasraf != 0)
            {
                aciklamaBuilder += $" (Ek Masraf: {toplamEkMasraf:N2} ₺)";
            }

            var hareket = new BankaKasaHareket
            {
                IslemNo = $"BORC-{odeme.Id}-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                IslemTarihi = odemeTarihi,
                HareketTipi = HareketTipi.Cikis, // Kasa = Borc (para cikiyor)
                BankaHesapId = request.BankaHesapId.Value,
                Tutar = netOdemeTutari,
                Aciklama = aciklamaBuilder,
                IslemKaynak = IslemKaynak.Butce,
                CreatedAt = DateTime.UtcNow
            };

            // Npgsql retry stratejisiyle uyumlu transaction kullanımı
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await context.Database.BeginTransactionAsync();
                try
                {
                    context.BankaKasaHareketleri.Add(hareket);
                    await context.SaveChangesAsync();
                    bankaKasaHareketId = hareket.Id;
                    kaydedilenHareket = hareket;

                    await context.BudgetOdemeler
                        .Where(o => o.Id == odemeId)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(o => o.Durum, OdemeDurum.Odendi)
                            .SetProperty(o => o.GercekOdemeTarihi, odemeTarihi)
                            .SetProperty(o => o.OdenenTutar, netOdemeTutari)
                            .SetProperty(o => o.OdemeYapildigiHesapId, request.BankaHesapId)
                            .SetProperty(o => o.OdemeNotu, odemeNotu)
                            .SetProperty(o => o.MasrafKesintisi, masrafKesintisi)
                            .SetProperty(o => o.CezaKesintisi, cezaKesintisi)
                            .SetProperty(o => o.DigerKesinti, digerKesinti)
                            .SetProperty(o => o.KesintiAciklamasi, request.KesintiAciklamasi)
                            .SetProperty(o => o.BankaKasaHareketId, bankaKasaHareketId)
                            .SetProperty(o => o.UpdatedAt, updatedAt));

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            if (ShouldCreateKrediKartiBorcu(odeme, request.OdemeTipi))
            {
                var donemAy = request.KrediKartiOdemeAy ?? request.OdemeTarihi.Month;
                var donemYil = request.KrediKartiOdemeYil ?? request.OdemeTarihi.Year;
                await AddKrediKartiBorcAsync(request.BankaHesapId.Value, netOdemeTutari, donemAy, donemYil, aciklamaBuilder, odeme.Id);
            }

            // Muhasebe fişi oluşturma — başarısız olsa bile ödeme ve hareket tamamlanmış sayılır
            try
            {
                await CreateBudgetMuhasebeFisiAsync(context, odeme, request, hareket, odemeTutari, masrafKesintisi, cezaKesintisi, digerKesinti);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bütçe ödeme muhasebe fişi oluşturulamadı (OdemeId={OdemeId}). Ödeme ve hareket başarıyla kaydedildi.", odemeId);
            }
        }
        else
        {
            // Mahsup tipi — hareket oluşturulmaz, sadece ödeme durumu güncellenir
            await context.BudgetOdemeler
                .Where(o => o.Id == odemeId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Durum, OdemeDurum.Odendi)
                    .SetProperty(o => o.GercekOdemeTarihi, odemeTarihi)
                    .SetProperty(o => o.OdenenTutar, netOdemeTutari)
                    .SetProperty(o => o.OdemeYapildigiHesapId, request.BankaHesapId)
                    .SetProperty(o => o.OdemeNotu, odemeNotu)
                    .SetProperty(o => o.MasrafKesintisi, masrafKesintisi)
                    .SetProperty(o => o.CezaKesintisi, cezaKesintisi)
                    .SetProperty(o => o.DigerKesinti, digerKesinti)
                    .SetProperty(o => o.KesintiAciklamasi, request.KesintiAciklamasi)
                    .SetProperty(o => o.BankaKasaHareketId, bankaKasaHareketId)
                    .SetProperty(o => o.UpdatedAt, updatedAt));
        }

        // Güncellenmiş entity'yi döndür
        odeme.Durum = OdemeDurum.Odendi;
        odeme.GercekOdemeTarihi = odemeTarihi;
        odeme.OdenenTutar = netOdemeTutari;
        odeme.OdemeYapildigiHesapId = request.BankaHesapId;
        odeme.OdemeNotu = odemeNotu;
        odeme.MasrafKesintisi = masrafKesintisi;
        odeme.CezaKesintisi = cezaKesintisi;
        odeme.DigerKesinti = digerKesinti;
        odeme.KesintiAciklamasi = request.KesintiAciklamasi;
        odeme.BankaKasaHareketId = bankaKasaHareketId;
        odeme.UpdatedAt = updatedAt;

        return odeme;
    }

    /// <summary>
    /// Fatura ile kapatma - fatura girildiginde hesaplari kapatir
    /// </summary>
    public async Task<BudgetOdeme> FaturaIleKapatAsync(int odemeId, int faturaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.BudgetOdemeler.FindAsync(odemeId);
        if (odeme == null)
            throw new Exception("Odeme bulunamadi");

        var fatura = await context.Faturalar.FindAsync(faturaId);
        if (fatura == null)
            throw new Exception("Fatura bulunamadi");

        // Odeme fatura ile kapatildi
        odeme.FaturaId = faturaId;
        odeme.FaturaIleKapatildi = true;
        odeme.Durum = OdemeDurum.Odendi;
        odeme.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return odeme;
    }

    /// <summary>
    /// Ödeme geri alma - Ödeme durumunu Bekliyor yapar ve ilişkili BankaKasaHareket kaydını siler
    /// </summary>
    public async Task<BudgetOdeme> OdemeGeriAlAsync(int odemeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.BudgetOdemeler.FindAsync(odemeId);
        if (odeme == null)
            throw new Exception("Odeme bulunamadi");

        // İlişkili BankaKasaHareket kaydını sil
        if (odeme.BankaKasaHareketId.HasValue)
        {
            var hareket = await context.BankaKasaHareketleri
                .Include(h => h.OdemeEslestirmeleri)
                .FirstOrDefaultAsync(h => h.Id == odeme.BankaKasaHareketId.Value);

            if (hareket != null)
            {
                // Önce ilişkili OdemeEslestirmeleri sil
                if (hareket.OdemeEslestirmeleri.Any())
                {
                    context.OdemeEslestirmeleri.RemoveRange(hareket.OdemeEslestirmeleri);
                }
                context.BankaKasaHareketleri.Remove(hareket); // Hard delete
            }
        }

        // Ödeme durumunu geri al
        odeme.Durum = OdemeDurum.Bekliyor;
        odeme.GercekOdemeTarihi = null;
        odeme.OdenenTutar = null;
        odeme.BankaKasaHareketId = null;
        odeme.OdemeYapildigiHesapId = null;
        odeme.OdemeNotu = null;
        odeme.MasrafKesintisi = 0;
        odeme.CezaKesintisi = 0;
        odeme.DigerKesinti = 0;
        odeme.KesintiAciklamasi = null;
        odeme.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return odeme;
    }

    #endregion

    #region Taksitli Odeme Islemleri

    public async Task<List<BudgetOdeme>> CreateTaksitliOdemeAsync(TaksitliOdemeRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // FirmaId=0 FK hatasina yol acmasin diye null'a cevir
        var firmaId = (request.FirmaId.HasValue && request.FirmaId.Value > 0) ? request.FirmaId : null;

        var taksitGrupId = Guid.NewGuid();
        var taksitler = new List<BudgetOdeme>();

        // Baslangic tarihini UTC olarak ayarla
        var baslangicUtc = DateTime.SpecifyKind(request.BaslangicTarihi, DateTimeKind.Utc);

        if (request.TaksitPlani != null && request.TaksitPlani.Any())
        {
            // Kullanicinin ozel taksit plani varsa onu kullan
            foreach (var plan in request.TaksitPlani.OrderBy(x => x.Sira))
            {
                var taksitTarihi = DateTime.SpecifyKind(plan.Tarih, DateTimeKind.Utc);
                
                var odeme = new BudgetOdeme
                {
                    OdemeTarihi = taksitTarihi,
                    OdemeAy = taksitTarihi.Month,
                    OdemeYil = taksitTarihi.Year,
                    MasrafKalemi = request.MasrafKalemi,
                    Aciklama = request.Aciklama,
                    Miktar = plan.Tutar,
                    TaksitliMi = true,
                    ToplamTaksitSayisi = request.TaksitSayisi,
                    KacinciTaksit = plan.Sira,
                    TaksitGrupId = taksitGrupId,
                    TaksitBaslangicAy = baslangicUtc,
                    TaksitBitisAy = baslangicUtc.AddMonths(request.TaksitSayisi - 1), // Yaklasik bitis tarihi
                    Notlar = request.Notlar,
                    FirmaId = firmaId,
                    Durum = OdemeDurum.Bekliyor,
                    CreatedAt = DateTime.UtcNow
                };

                taksitler.Add(odeme);
            }
        }
        else
        {
            // Otomatik hesaplama (Eski yontem - fallback olarak birakiyorum)
            var taksitTutari = Math.Round(request.ToplamTutar / request.TaksitSayisi, 2);
            var toplamHesaplanan = taksitTutari * request.TaksitSayisi;
            var fark = request.ToplamTutar - toplamHesaplanan;

            for (int i = 0; i < request.TaksitSayisi; i++)
            {
                var taksitTarihi = baslangicUtc.AddMonths(i);
                var tutar = i == request.TaksitSayisi - 1 ? taksitTutari + fark : taksitTutari;

                var odeme = new BudgetOdeme
                {
                    OdemeTarihi = taksitTarihi,
                    OdemeAy = taksitTarihi.Month,
                    OdemeYil = taksitTarihi.Year,
                    MasrafKalemi = request.MasrafKalemi,
                    Aciklama = request.Aciklama,
                    Miktar = tutar,
                    TaksitliMi = true,
                    ToplamTaksitSayisi = request.TaksitSayisi,
                    KacinciTaksit = i + 1,
                    TaksitGrupId = taksitGrupId,
                    TaksitBaslangicAy = baslangicUtc,
                    TaksitBitisAy = baslangicUtc.AddMonths(request.TaksitSayisi - 1),
                    Notlar = request.Notlar,
                    FirmaId = firmaId,
                    Durum = OdemeDurum.Bekliyor,
                    CreatedAt = DateTime.UtcNow
                };

                taksitler.Add(odeme);
            }
        }

        context.BudgetOdemeler.AddRange(taksitler);
        await context.SaveChangesAsync();
        return taksitler;
    }

    public async Task<List<BudgetOdeme>> GetTaksitGrubuAsync(Guid taksitGrupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BudgetOdemeler
            .Where(o => o.TaksitGrupId == taksitGrupId)
            .OrderBy(o => o.KacinciTaksit)
            .ToListAsync();
    }

    public async Task UpdateTaksitGrubuAsync(List<BudgetOdeme> taksitler)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        foreach (var taksit in taksitler)
        {
            var existing = await context.BudgetOdemeler.FindAsync(taksit.Id);
            if (existing != null)
            {
                // DateTime'i UTC olarak ayarla
                existing.OdemeTarihi = DateTime.SpecifyKind(taksit.OdemeTarihi, DateTimeKind.Utc);
                existing.OdemeAy = taksit.OdemeTarihi.Month;
                existing.OdemeYil = taksit.OdemeTarihi.Year;
                existing.Miktar = taksit.Miktar;
                existing.Durum = taksit.Durum;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }
        await context.SaveChangesAsync();
    }

    #endregion

    #region Excel Islemleri

    public async Task<byte[]> GetExcelSablonAsync(List<Firma> firmalar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Odeme Sablonu");

        // Basliklar
        var headers = new[] { "Odeme Tarihi*", "Masraf Kalemi*", "Aciklama", "Miktar*", "Durum", "Firma", "Notlar" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
        }

        // Ornek satirlar
        worksheet.Cell(2, 1).Value = DateTime.Today.ToString("dd.MM.yyyy");
        worksheet.Cell(2, 2).Value = "Kira";
        worksheet.Cell(2, 3).Value = "Ocak ayi kirasi";
        worksheet.Cell(2, 4).Value = 5000;
        worksheet.Cell(2, 5).Value = "Bekliyor";
        worksheet.Cell(2, 6).Value = firmalar.FirstOrDefault()?.FirmaAdi ?? "";
        worksheet.Cell(2, 7).Value = "";

        // Aciklama
        worksheet.Cell(5, 1).Value = "ACIKLAMALAR:";
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(6, 1).Value = "* Odeme Tarihi: GG.AA.YYYY formatinda";
        worksheet.Cell(7, 1).Value = "* Durum: Bekliyor, Odendi, Ertelendi, Iptal";
        
        // Firma listesi
        worksheet.Cell(9, 1).Value = "FIRMALAR:";
        worksheet.Cell(9, 1).Style.Font.Bold = true;
        int row = 10;
        foreach (var firma in firmalar)
        {
            worksheet.Cell(row++, 1).Value = firma.FirmaAdi;
        }

        // Masraf kalemleri
        row += 2;
        worksheet.Cell(row, 1).Value = "MASRAF KALEMLERI:";
        worksheet.Cell(row++, 1).Style.Font.Bold = true;
        
        var masrafKalemleri = await context.BudgetMasrafKalemleri.Where(m => m.Aktif).OrderBy(m => m.KalemAdi).ToListAsync();
        foreach (var kalem in masrafKalemleri)
        {
            worksheet.Cell(row++, 1).Value = kalem.KalemAdi;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<int> ImportFromExcelAsync(byte[] fileContent)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odemeler = new List<BudgetOdeme>();

        using var stream = new MemoryStream(fileContent);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);

        var rows = worksheet.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            var tarihStr = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(tarihStr) || tarihStr.StartsWith("ACIKLAMA") || tarihStr.StartsWith("FIRMA") || tarihStr.StartsWith("MASRAF") || tarihStr.StartsWith("*"))
                continue;

            DateTime tarih;
            if (row.Cell(1).DataType == ClosedXML.Excel.XLDataType.DateTime)
                tarih = row.Cell(1).GetDateTime();
            else if (row.Cell(1).DataType == ClosedXML.Excel.XLDataType.Number)
                tarih = DateTime.FromOADate(row.Cell(1).GetDouble());
            else if (!DateTime.TryParse(tarihStr, new System.Globalization.CultureInfo("tr-TR"), out tarih))
                continue;

            var masrafKalemi = row.Cell(2).GetString().Trim();
            if (string.IsNullOrEmpty(masrafKalemi)) continue;

            decimal miktar;
            if (row.Cell(4).DataType == ClosedXML.Excel.XLDataType.Number)
                miktar = (decimal)row.Cell(4).GetDouble();
            else
            {
                var miktarStr = row.Cell(4).GetString().Replace(".", "").Replace(",", ".");
                if (!decimal.TryParse(miktarStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out miktar))
                    continue;
            }

            if (miktar <= 0) continue;

            var durumStr = row.Cell(5).GetString().Trim().ToLower();
            var durum = durumStr switch
            {
                "odendi" => OdemeDurum.Odendi,
                "ertelendi" => OdemeDurum.Ertelendi,
                "iptal" => OdemeDurum.Iptal,
                _ => OdemeDurum.Bekliyor
            };

            // Firma bul
            var firmaAdi = row.Cell(6).GetString().Trim();
            int? firmaId = null;
            if (!string.IsNullOrEmpty(firmaAdi))
            {
                var firma = await context.Firmalar.FirstOrDefaultAsync(f => f.FirmaAdi == firmaAdi);
                firmaId = firma?.Id;
            }

            var tarihUtc = DateTime.SpecifyKind(tarih, DateTimeKind.Utc);

            var odeme = new BudgetOdeme
            {
                OdemeTarihi = tarihUtc,
                OdemeAy = tarih.Month,
                OdemeYil = tarih.Year,
                MasrafKalemi = masrafKalemi,
                Aciklama = row.Cell(3).GetString().Trim(),
                Miktar = miktar,
                Durum = durum,
                FirmaId = firmaId,
                Notlar = row.Cell(7).GetString().Trim(),
                TaksitliMi = false,
                ToplamTaksitSayisi = 1,
                KacinciTaksit = 1,
                CreatedAt = DateTime.UtcNow
            };

            odemeler.Add(odeme);
        }

        if (odemeler.Any())
        {
            context.BudgetOdemeler.AddRange(odemeler);
            await context.SaveChangesAsync();
        }

        return odemeler.Count;
    }

    #endregion

    #region Masraf Kalemleri

    public async Task<List<BudgetMasrafKalemi>> GetMasrafKalemleriAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BudgetMasrafKalemleri
            .Where(m => m.Aktif)
            .OrderBy(m => m.SiraNo)
            .ThenBy(m => m.KalemAdi)
            .ToListAsync();
    }

    public async Task<BudgetMasrafKalemi> CreateMasrafKalemiAsync(BudgetMasrafKalemi kalem)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.BudgetMasrafKalemleri.Add(kalem);
        await context.SaveChangesAsync();
        return kalem;
    }

    public async Task<BudgetMasrafKalemi> UpdateMasrafKalemiAsync(BudgetMasrafKalemi kalem)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.BudgetMasrafKalemleri.Update(kalem);
        await context.SaveChangesAsync();
        return kalem;
    }

    public async Task DeleteMasrafKalemiAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kalem = await context.BudgetMasrafKalemleri.FindAsync(id);
        if (kalem != null)
        {
            kalem.Aktif = false;
            await context.SaveChangesAsync();
        }
    }

    public async Task SeedMasrafKalemleriAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Varsayılan masraf kalemleri
        var varsayilanKalemler = new List<(string Adi, string Kategori, string Icon, string Renk, int SiraNo)>
        {
            ("Kira", "Sabit Giderler", "bi-house", "#6c757d", 1),
            ("Elektrik", "Faturalar", "bi-lightning", "#ffc107", 2),
            ("Su", "Faturalar", "bi-droplet", "#0dcaf0", 3),
            ("Doğalgaz", "Faturalar", "bi-fire", "#fd7e14", 4),
            ("İnternet", "Faturalar", "bi-wifi", "#6610f2", 5),
            ("Telefon", "Faturalar", "bi-telephone", "#20c997", 6),
            ("Personel Maaş", "Personel", "bi-people", "#0d6efd", 7),
            ("SGK", "Personel", "bi-shield-check", "#198754", 8),
            ("Vergi", "Vergiler", "bi-bank", "#dc3545", 9),
            ("Akaryakıt", "Araç Giderleri", "bi-fuel-pump", "#fd7e14", 10),
            ("Sigorta", "Sigorta", "bi-shield", "#6f42c1", 11),
            ("Bakım/Onarım", "Araç Giderleri", "bi-tools", "#6c757d", 12),
            ("Kredi Kartı", "Finans", "bi-credit-card", "#dc3545", 13),
            ("Banka Kredisi", "Finans", "bi-bank2", "#0d6efd", 14),
            ("Araç Kredisi", "Finans", "bi-car-front", "#198754", 15),
            ("Diğer", "Diğer", "bi-three-dots", "#6c757d", 99)
        };

        foreach (var (adi, kategori, icon, renk, siraNo) in varsayilanKalemler)
        {
            var mevcutMu = await context.BudgetMasrafKalemleri
                .IgnoreQueryFilters()
                .AnyAsync(m => m.KalemAdi == adi);

            if (!mevcutMu)
            {
                context.BudgetMasrafKalemleri.Add(new BudgetMasrafKalemi
                {
                    KalemAdi = adi,
                    Kategori = kategori,
                    Icon = icon,
                    Renk = renk,
                    SiraNo = siraNo,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
    }

    #endregion

    #region Raporlar

    public async Task<BudgetOzet> GetAylikOzetAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.BudgetOdemeler
            .Where(o => o.OdemeYil == yil && o.OdemeAy == ay);

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value || o.FirmaId == null);

        var odemeler = await query.ToListAsync();

        var ozet = new BudgetOzet
        {
            Yil = yil,
            Ay = ay,
            ToplamOdeme = odemeler.Sum(o => o.Miktar),
            OdenenToplam = odemeler.Where(IsGerceklesenDurumu).Sum(GetGerceklesenTutar),
            BekleyenToplam = odemeler.Where(IsBekleyenDurumu).Sum(GetBekleyenTutar),
            ToplamKayit = odemeler.Count,
            OdenenKayit = odemeler.Count(IsGerceklesenDurumu),
            BekleyenKayit = odemeler.Count(IsBekleyenDurumu)
        };

        ozet.KategoriOzetleri = odemeler
            .GroupBy(o => o.MasrafKalemi)
            .Select(g => new BudgetKategoriOzet
            {
                MasrafKalemi = g.Key,
                Toplam = g.Sum(o => o.Miktar),
                Adet = g.Count(),
                Yuzde = ozet.ToplamOdeme > 0 ? Math.Round(g.Sum(o => o.Miktar) / ozet.ToplamOdeme * 100, 1) : 0
            })
            .OrderByDescending(k => k.Toplam)
            .ToList();

        return ozet;
    }

    public async Task<BudgetYillikOzet> GetYillikOzetAsync(int yil, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Add this to ensure payments are automatically generated for all months in the year view
        for (int m = 1; m <= 12; m++)
        {
            await TekrarlayanOdemelerdenKayitOlusturAsync(yil, m, firmaId);
        }

        var query = context.BudgetOdemeler.Where(o => o.OdemeYil == yil);

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value || o.FirmaId == null);

        var odemeler = await query.ToListAsync();

        var ozet = new BudgetYillikOzet
        {
            Yil = yil,
            ToplamOdeme = odemeler.Sum(o => o.Miktar)
        };

        for (int ay = 1; ay <= 12; ay++)
        {
            var aylikOdemeler = odemeler.Where(o => o.OdemeAy == ay).ToList();
            ozet.AylikToplamlar.Add(new BudgetAylikToplam
            {
                Ay = ay,
                AyAdi = AyAdlari[ay],
                Toplam = aylikOdemeler.Sum(o => o.Miktar),
                Odenen = aylikOdemeler.Where(IsGerceklesenDurumu).Sum(GetGerceklesenTutar),
                Bekleyen = aylikOdemeler.Where(IsBekleyenDurumu).Sum(GetBekleyenTutar)
            });
        }

        ozet.KategoriOzetleri = odemeler
            .GroupBy(o => o.MasrafKalemi)
            .Select(g => new BudgetKategoriOzet
            {
                MasrafKalemi = g.Key,
                Toplam = g.Sum(o => o.Miktar),
                Adet = g.Count(),
                Yuzde = ozet.ToplamOdeme > 0 ? Math.Round(g.Sum(o => o.Miktar) / ozet.ToplamOdeme * 100, 1) : 0
            })
            .OrderByDescending(k => k.Toplam)
            .ToList();

        return ozet;
    }

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static bool IsGerceklesenDurumu(BudgetOdeme odeme)
        => odeme.Durum == OdemeDurum.Odendi || odeme.Durum == OdemeDurum.KismiOdendi || odeme.FaturaIleKapatildi;

    private static bool IsBekleyenDurumu(BudgetOdeme odeme)
        => odeme.Durum == OdemeDurum.Bekliyor || odeme.Durum == OdemeDurum.KismiOdendi;

    private static decimal GetGerceklesenTutar(BudgetOdeme odeme)
        => odeme.Durum == OdemeDurum.KismiOdendi
            ? odeme.NetOdenenTutar
            : (odeme.OdenenTutar ?? odeme.NetOdenenTutar);

    private static decimal GetBekleyenTutar(BudgetOdeme odeme)
        => odeme.Durum == OdemeDurum.KismiOdendi ? odeme.KalanTutar : odeme.Miktar;

    private static string? BirlestirNotlar(string? notlar, string? ekNot)
    {
        if (string.IsNullOrWhiteSpace(ekNot))
            return notlar;

        if (string.IsNullOrWhiteSpace(notlar))
            return ekNot;

        return $"{notlar}\n{ekNot}";
    }

    private static string? UpsertBudgetMeta(string? notlar, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return notlar;

        var satir = $"[{key}:{value}]";
        if (string.IsNullOrWhiteSpace(notlar))
            return satir;

        if (notlar.Contains($"[{key}:"))
        {
            var satirlar = notlar.Split('\n').Where(l => !l.StartsWith($"[{key}:", StringComparison.OrdinalIgnoreCase)).ToList();
            satirlar.Add(satir);
            return string.Join("\n", satirlar);
        }

        return $"{notlar}\n{satir}";
    }

    private static string? GetBudgetMeta(string? notlar, string key)
    {
        if (string.IsNullOrWhiteSpace(notlar))
            return null;

        foreach (var satir in notlar.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (satir.StartsWith($"[{key}:", StringComparison.OrdinalIgnoreCase) && satir.EndsWith("]"))
            {
                return satir.Substring(key.Length + 2, satir.Length - key.Length - 3);
            }
        }

        return null;
    }

    private static bool IsKrediKalemi(string? masrafKalemi)
        => !string.IsNullOrWhiteSpace(masrafKalemi)
           && (masrafKalemi.Contains("Banka Kredisi", StringComparison.OrdinalIgnoreCase)
               || masrafKalemi.Contains("Araç Kredisi", StringComparison.OrdinalIgnoreCase)
               || masrafKalemi.Contains("Arac Kredisi", StringComparison.OrdinalIgnoreCase));

    private static bool IsKrediKartiKalemi(string? masrafKalemi)
        => !string.IsNullOrWhiteSpace(masrafKalemi)
           && masrafKalemi.Contains("Kredi Kartı", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldCreateKrediKartiBorcu(BudgetOdeme odeme, OdemeTipi odemeTipi)
        => odemeTipi == OdemeTipi.KrediKarti && !IsKrediKartiKalemi(odeme.MasrafKalemi);

    private static string? BuildFinansMetaNotu(TaksitliOdemeRequest request)
    {
        string? not = null;
        not = UpsertBudgetMeta(not, "FinansKaynakHesapId", request.BagliBankaHesapId?.ToString());
        not = UpsertBudgetMeta(not, "KrediAnaPara", request.KrediAnaParaTutari?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        not = UpsertBudgetMeta(not, "KrediNet", request.KrediNetGecenTutar?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        not = UpsertBudgetMeta(not, "PesinFaiz", request.PesinFaizTutari.ToString(System.Globalization.CultureInfo.InvariantCulture));
        not = UpsertBudgetMeta(not, "PesinMasraf", request.PesinMasrafTutari.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return not;
    }

    private async Task<MuhasebeHesap> ResolveMuhasebeHesabiAsync(ApplicationDbContext context, params string[] adayKodlar)
    {
        foreach (var kod in adayKodlar.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var hesap = await _muhasebeService.GetHesapByKodAsync(kod);
            if (hesap != null)
                return hesap;

            var kokKod = kod.Contains('.') ? kod.Split('.')[0] : kod;
            hesap = await _muhasebeService.GetHesapByKodAsync(kokKod);
            if (hesap != null)
                return hesap;
        }

        throw new InvalidOperationException($"Muhasebe hesabı bulunamadı: {string.Join(", ", adayKodlar)}");
    }

    private async Task<string> GetBankaHesapMuhasebeKoduAsync(ApplicationDbContext context, int bankaHesapId)
    {
        var hesap = await context.BankaHesaplari.AsNoTracking().FirstOrDefaultAsync(h => h.Id == bankaHesapId)
            ?? throw new InvalidOperationException("Banka hesabı bulunamadı.");

        return hesap.VarsayilanMuhasebeKodu ?? GetBankaHesapKodu(hesap.HesapTipi);
    }

    private static string GetBankaHesapKodu(HesapTipi tip)
    {
        return tip switch
        {
            HesapTipi.Kasa => "100.01",
            HesapTipi.VadesizHesap or HesapTipi.VadeliHesap => "102.01",
            HesapTipi.KrediHesabi => "300.01",
            HesapTipi.KrediKarti => "103.01",
            _ => "102.01"
        };
    }

    private string GetBudgetMuhasebeGiderKodu(BudgetOdeme odeme)
    {
        if (IsKrediKalemi(odeme.MasrafKalemi))
            return "300.01";

        if (IsKrediKartiKalemi(odeme.MasrafKalemi))
            return "103.01";

        return odeme.MasrafKalemi switch
        {
            var kalem when kalem.Contains("Yakıt", StringComparison.OrdinalIgnoreCase) => "770.06",
            var kalem when kalem.Contains("AdBlue", StringComparison.OrdinalIgnoreCase) => "770.06",
            var kalem when kalem.Contains("Bakım", StringComparison.OrdinalIgnoreCase) || kalem.Contains("Onarım", StringComparison.OrdinalIgnoreCase) => "770.07",
            var kalem when kalem.Contains("Sigorta", StringComparison.OrdinalIgnoreCase) => "770.08",
            var kalem when kalem.Contains("Kredi Kartı", StringComparison.OrdinalIgnoreCase) => "103.01",
            _ => "770.01"
        };
    }

    private async Task CreateKrediKullanimiKayitlariAsync(ApplicationDbContext context, TaksitliOdemeRequest request, Guid taksitGrupId, BankaHesap bagliHesap)
    {
        var anaPara = RoundCurrency(request.KrediAnaParaTutari ?? 0);
        if (anaPara <= 0)
            return;

        var netTutar = RoundCurrency(request.KrediNetGecenTutar ?? anaPara);
        var pesinFaiz = RoundCurrency(Math.Abs(request.PesinFaizTutari));
        var pesinMasraf = RoundCurrency(Math.Abs(request.PesinMasrafTutari));
        var kullanimTarihi = DateTime.SpecifyKind(request.BaslangicTarihi, DateTimeKind.Utc);

        var hareket = new BankaKasaHareket
        {
            IslemNo = $"KRD-{taksitGrupId.ToString()[..8]}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            IslemTarihi = kullanimTarihi,
            HareketTipi = HareketTipi.Giris,
            BankaHesapId = bagliHesap.Id,
            Tutar = netTutar,
            Aciklama = $"Kredi kullanımı: {request.Aciklama ?? request.MasrafKalemi}",
            IslemKaynak = IslemKaynak.Butce,
            CreatedAt = DateTime.UtcNow
        };

        context.BankaKasaHareketleri.Add(hareket);
        await context.SaveChangesAsync();

        var bankaHesap = await ResolveMuhasebeHesabiAsync(context, bagliHesap.VarsayilanMuhasebeKodu ?? GetBankaHesapKodu(bagliHesap.HesapTipi));
        var krediHesap = await ResolveMuhasebeHesabiAsync(context, "300.01", "300");
        var kalemler = new List<MuhasebeFisKalem>
        {
            new()
            {
                HesapId = bankaHesap.Id,
                Borc = netTutar,
                Alacak = 0,
                SiraNo = 1,
                Aciklama = $"Kredi kullanımı - {bagliHesap.HesapAdi}"
            }
        };

        var siraNo = 2;
        if (pesinFaiz > 0)
        {
            var faizHesap = await ResolveMuhasebeHesabiAsync(context, "780.01", "780", "770.01", "770");
            kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = faizHesap.Id,
                Borc = pesinFaiz,
                Alacak = 0,
                SiraNo = siraNo++,
                Aciklama = "Peşin kredi faizi"
            });

            context.BudgetOdemeler.Add(new BudgetOdeme
            {
                OdemeTarihi = kullanimTarihi,
                OdemeAy = kullanimTarihi.Month,
                OdemeYil = kullanimTarihi.Year,
                MasrafKalemi = "Banka Kredisi Faiz",
                Aciklama = request.Aciklama,
                Miktar = pesinFaiz,
                Durum = OdemeDurum.Odendi,
                OdenenTutar = pesinFaiz,
                GercekOdemeTarihi = kullanimTarihi,
                OdemeYapildigiHesapId = bagliHesap.Id,
                TaksitliMi = false,
                ToplamTaksitSayisi = 1,
                KacinciTaksit = 1,
                TaksitGrupId = taksitGrupId,
                Notlar = UpsertBudgetMeta(request.Notlar, "FinansKaynakHesapId", bagliHesap.Id.ToString()),
                FirmaId = request.FirmaId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (pesinMasraf > 0)
        {
            var masrafHesap = await ResolveMuhasebeHesabiAsync(context, "770.01", "770");
            kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = masrafHesap.Id,
                Borc = pesinMasraf,
                Alacak = 0,
                SiraNo = siraNo++,
                Aciklama = "Peşin kredi masrafı"
            });

            context.BudgetOdemeler.Add(new BudgetOdeme
            {
                OdemeTarihi = kullanimTarihi,
                OdemeAy = kullanimTarihi.Month,
                OdemeYil = kullanimTarihi.Year,
                MasrafKalemi = "Banka Kredisi Masrafı",
                Aciklama = request.Aciklama,
                Miktar = pesinMasraf,
                Durum = OdemeDurum.Odendi,
                OdenenTutar = pesinMasraf,
                GercekOdemeTarihi = kullanimTarihi,
                OdemeYapildigiHesapId = bagliHesap.Id,
                TaksitliMi = false,
                ToplamTaksitSayisi = 1,
                KacinciTaksit = 1,
                TaksitGrupId = taksitGrupId,
                Notlar = UpsertBudgetMeta(request.Notlar, "FinansKaynakHesapId", bagliHesap.Id.ToString()),
                FirmaId = request.FirmaId,
                CreatedAt = DateTime.UtcNow
            });
        }

        kalemler.Add(new MuhasebeFisKalem
        {
            HesapId = krediHesap.Id,
            Borc = 0,
            Alacak = anaPara,
            SiraNo = siraNo,
            Aciklama = $"Kredi anapara tahakkuku - {request.MasrafKalemi}"
        });

        var fis = new MuhasebeFis
        {
            FisNo = string.Empty,
            FisTarihi = kullanimTarihi,
            FisTipi = FisTipi.Mahsup,
            Aciklama = $"Kredi kullanımı: {request.Aciklama ?? request.MasrafKalemi}",
            Kaynak = FisKaynak.Butce,
            KaynakTip = "BudgetKredi",
            KaynakId = hareket.Id,
            Durum = FisDurum.Onaylandi,
            Kalemler = kalemler
        };

        await _muhasebeService.CreateFisAtomicAsync(fis);
        await context.SaveChangesAsync();
    }

    private async Task CreateBudgetMuhasebeFisiAsync(ApplicationDbContext context, BudgetOdeme odeme, OdemeYapRequest request, BankaKasaHareket hareket, decimal anaTutar, decimal masrafKesintisi, decimal cezaKesintisi, decimal digerKesinti)
    {
        if (!request.BankaHesapId.HasValue)
            return;

        var kaynakKod = await GetBankaHesapMuhasebeKoduAsync(context, request.BankaHesapId.Value);
        var kaynakHesap = await ResolveMuhasebeHesabiAsync(context, kaynakKod);
        var kalemler = new List<MuhasebeFisKalem>();
        var siraNo = 1;

        if (IsKrediKartiKalemi(odeme.MasrafKalemi))
        {
            var bagliHesapId = odeme.OdemeYapildigiHesapId;
            if (!bagliHesapId.HasValue)
            {
                var meta = GetBudgetMeta(odeme.Notlar, "FinansKaynakHesapId");
                if (int.TryParse(meta, out var parsedId))
                    bagliHesapId = parsedId;
            }

            var kartKod = bagliHesapId.HasValue ? await GetBankaHesapMuhasebeKoduAsync(context, bagliHesapId.Value) : "103.01";
            var kartHesap = await ResolveMuhasebeHesabiAsync(context, kartKod, "103.01", "103");
            kalemler.Add(new MuhasebeFisKalem { HesapId = kartHesap.Id, Borc = anaTutar, Alacak = 0, SiraNo = siraNo++, Aciklama = "Kredi kartı borç kapama" });
        }
        else if (IsKrediKalemi(odeme.MasrafKalemi))
        {
            var krediHesap = await ResolveMuhasebeHesabiAsync(context, "300.01", "300");
            kalemler.Add(new MuhasebeFisKalem { HesapId = krediHesap.Id, Borc = anaTutar, Alacak = 0, SiraNo = siraNo++, Aciklama = "Kredi taksit/anapara ödemesi" });
        }
        else
        {
            var giderHesap = await ResolveMuhasebeHesabiAsync(context, GetBudgetMuhasebeGiderKodu(odeme), "770.01", "770");
            kalemler.Add(new MuhasebeFisKalem { HesapId = giderHesap.Id, Borc = anaTutar, Alacak = 0, SiraNo = siraNo++, Aciklama = odeme.MasrafKalemi });
        }

        if (masrafKesintisi + digerKesinti > 0)
        {
            var yonetimMasrafHesabi = await ResolveMuhasebeHesabiAsync(context, "770.01", "770");
            kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = yonetimMasrafHesabi.Id,
                Borc = masrafKesintisi + digerKesinti,
                Alacak = 0,
                SiraNo = siraNo++,
                Aciklama = odeme.KesintiAciklamasi ?? "Yönetim masrafı"
            });
        }

        if (cezaKesintisi > 0)
        {
            var finansmanGideri = await ResolveMuhasebeHesabiAsync(context, "780.01", "780", "770.01", "770");
            kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = finansmanGideri.Id,
                Borc = cezaKesintisi,
                Alacak = 0,
                SiraNo = siraNo++,
                Aciklama = "Faiz / finansman gideri"
            });
        }

        kalemler.Add(new MuhasebeFisKalem
        {
            HesapId = kaynakHesap.Id,
            Borc = 0,
            Alacak = hareket.Tutar,
            SiraNo = siraNo,
            Aciklama = $"Ödeme kaynağı - {hareket.BankaHesap?.HesapAdi ?? kaynakHesap.HesapAdi}"
        });

        var fis = new MuhasebeFis
        {
            FisNo = string.Empty,
            FisTarihi = hareket.IslemTarihi,
            FisTipi = FisTipi.Tediye,
            Aciklama = $"Bütçe ödeme muhasebeleştirme: {odeme.MasrafKalemi}",
            Kaynak = FisKaynak.Butce,
            KaynakId = odeme.Id,
            KaynakTip = "BudgetOdeme",
            Durum = FisDurum.Onaylandi,
            Kalemler = kalemler
        };

        await _muhasebeService.CreateFisAtomicAsync(fis);
    }

    public async Task<List<BudgetGunlukOzet>> GetTakvimDataAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Tekrarlayan odemelerden bu ay icin otomatik kayit olustur
        await TekrarlayanOdemelerdenKayitOlusturAsync(yil, ay, firmaId);

        var query = context.BudgetOdemeler
            .Where(o => o.OdemeYil == yil && o.OdemeAy == ay);

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value || o.FirmaId == null);

        var odemeler = await query.OrderBy(o => o.OdemeTarihi).ToListAsync();

        var gunlukOzetler = new List<BudgetGunlukOzet>();
        var gunSayisi = DateTime.DaysInMonth(yil, ay);

        for (int gun = 1; gun <= gunSayisi; gun++)
        {
            var tarih = new DateTime(yil, ay, gun);
            var gunOdemeleri = odemeler.Where(o => o.OdemeTarihi.Day == gun).ToList();
            var bekleyenOdemeler = gunOdemeleri.Where(o => o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi).ToList();

            gunlukOzetler.Add(new BudgetGunlukOzet
            {
                Tarih = tarih,
                Gun = gun,
                ToplamOdeme = gunOdemeleri.Sum(o => o.Miktar),
                OdemeSayisi = gunOdemeleri.Count,
                BekleyenToplamOdeme = bekleyenOdemeler.Sum(GetBekleyenTutar),
                BekleyenOdemeSayisi = bekleyenOdemeler.Count,
                Odemeler = gunOdemeleri
            });
        }

        return gunlukOzetler;
    }

    public async Task<List<BudgetKategoriOzet>> GetKategoriOzetAsync(int yil, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.BudgetOdemeler.Where(o => o.OdemeYil == yil);

        if (ay.HasValue)
            query = query.Where(o => o.OdemeAy == ay.Value);

        var odemeler = await query.ToListAsync();
        var toplam = odemeler.Sum(o => o.Miktar);

        // Masraf kalemlerinin renklerini al
        var masrafKalemleri = await context.BudgetMasrafKalemleri
            .ToDictionaryAsync(m => m.KalemAdi, m => m.Renk);

        return odemeler
            .GroupBy(o => o.MasrafKalemi)
            .Select(g => new BudgetKategoriOzet
            {
                MasrafKalemi = g.Key,
                Renk = masrafKalemleri.TryGetValue(g.Key, out var renk) ? renk : "#6c757d",
                Toplam = g.Sum(o => o.Miktar),
                Adet = g.Count(),
                Yuzde = toplam > 0 ? Math.Round(g.Sum(o => o.Miktar) / toplam * 100, 1) : 0
            })
            .OrderByDescending(k => k.Toplam)
            .ToList();
    }

    #endregion

    #region Kredi/Taksit Raporlari

    public async Task<List<KrediOzet>> GetAktifKredilerAsync(int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.BudgetOdemeler
            .Where(o => o.TaksitliMi && o.TaksitGrupId.HasValue);

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value);

        var taksitliOdemeler = await query.ToListAsync();

        var krediler = taksitliOdemeler
            .GroupBy(o => o.TaksitGrupId!.Value)
            .Select(g =>
            {
                var taksitler = g.OrderBy(t => t.KacinciTaksit).ToList();
                var ilkTaksit = taksitler.First();
                var sonTaksit = taksitler.Last();
                var odenenTaksitler = taksitler.Where(t => t.Durum == OdemeDurum.Odendi).ToList();
                var kismiOdenenTaksitler = taksitler.Where(t => t.Durum == OdemeDurum.KismiOdendi).ToList();
                var bekleyenTaksitler = taksitler.Where(IsBekleyenDurumu).ToList();
                var sonrakiTaksit = bekleyenTaksitler.OrderBy(t => t.OdemeTarihi).FirstOrDefault();

                return new KrediOzet
                {
                    TaksitGrupId = g.Key,
                    MasrafKalemi = ilkTaksit.MasrafKalemi,
                    Aciklama = ilkTaksit.Aciklama,
                    BaslangicTarihi = ilkTaksit.OdemeTarihi,
                    BitisTarihi = sonTaksit.OdemeTarihi,
                    ToplamTaksitSayisi = taksitler.Count,
                    OdenenTaksitSayisi = odenenTaksitler.Count,
                    KalanTaksitSayisi = bekleyenTaksitler.Count,
                    TaksitTutari = taksitler.First().Miktar,
                    ToplamTutar = taksitler.Sum(t => t.Miktar),
                    OdenenTutar = odenenTaksitler.Sum(GetGerceklesenTutar) + kismiOdenenTaksitler.Sum(GetGerceklesenTutar),
                    KalanTutar = bekleyenTaksitler.Sum(GetBekleyenTutar),
                    TamamlanmaYuzdesi = taksitler.Sum(t => t.Miktar) > 0
                        ? Math.Round((odenenTaksitler.Sum(GetGerceklesenTutar) + kismiOdenenTaksitler.Sum(GetGerceklesenTutar)) / taksitler.Sum(t => t.Miktar) * 100, 1)
                        : 0,
                    SonrakiTaksitTarihi = sonrakiTaksit?.OdemeTarihi
                };
            })
            .Where(k => k.KalanTaksitSayisi > 0) // Sadece aktif (kalan taksiti olan) krediler
            .OrderBy(k => k.SonrakiTaksitTarihi)
            .ToList();

        return krediler;
    }

    public async Task<List<AylikKrediTaksitRapor>> GetAylikKrediTaksitRaporuAsync(int yil)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var taksitliOdemeler = await context.BudgetOdemeler
            .Where(o => o.TaksitliMi && o.OdemeYil == yil)
            .OrderBy(o => o.OdemeAy)
            .ThenBy(o => o.OdemeTarihi)
            .ToListAsync();

        var rapor = new List<AylikKrediTaksitRapor>();

        for (int ay = 1; ay <= 12; ay++)
        {
            var aylikTaksitler = taksitliOdemeler.Where(o => o.OdemeAy == ay).ToList();

            rapor.Add(new AylikKrediTaksitRapor
            {
                Ay = ay,
                AyAdi = AyAdlari[ay],
                ToplamTaksitTutari = aylikTaksitler.Sum(t => t.Miktar),
                OdenenTutar = aylikTaksitler.Where(IsGerceklesenDurumu).Sum(GetGerceklesenTutar),
                BekleyenTutar = aylikTaksitler.Where(IsBekleyenDurumu).Sum(GetBekleyenTutar),
                TaksitSayisi = aylikTaksitler.Count,
                Taksitler = aylikTaksitler.Select(t => new KrediTaksitDetay
                {
                    MasrafKalemi = t.MasrafKalemi,
                    Aciklama = t.Aciklama,
                    KacinciTaksit = t.KacinciTaksit,
                    ToplamTaksitSayisi = t.ToplamTaksitSayisi,
                    Tutar = t.Miktar,
                    Durum = t.Durum,
                    OdemeTarihi = t.OdemeTarihi
                }).ToList()
            });
        }

        return rapor;
    }

    #endregion

    #region Periyod Bazli Raporlar

    public async Task<BudgetOzet> GetPeriyodOzetAsync(DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odemeler = await GetOdemelerByDateRangeAsync(baslangic, bitis);

        var ozet = new BudgetOzet
        {
            BaslangicTarihi = baslangic,
            BitisTarihi = bitis,
            ToplamOdeme = odemeler.Sum(o => o.Miktar),
            OdenenToplam = odemeler.Where(IsGerceklesenDurumu).Sum(GetGerceklesenTutar),
            BekleyenToplam = odemeler.Where(IsBekleyenDurumu).Sum(GetBekleyenTutar),
            ToplamKayit = odemeler.Count,
            OdenenKayit = odemeler.Count(IsGerceklesenDurumu),
            BekleyenKayit = odemeler.Count(IsBekleyenDurumu)
        };

        ozet.KategoriOzetleri = odemeler
            .GroupBy(o => o.MasrafKalemi)
            .Select(g => new BudgetKategoriOzet
            {
                MasrafKalemi = g.Key,
                Toplam = g.Sum(o => o.Miktar),
                Adet = g.Count(),
                Yuzde = ozet.ToplamOdeme > 0 ? Math.Round(g.Sum(o => o.Miktar) / ozet.ToplamOdeme * 100, 1) : 0
            })
            .OrderByDescending(k => k.Toplam)
            .ToList();

        return ozet;
    }

    public async Task<List<BudgetKategoriOzet>> GetKategoriOzetByDateRangeAsync(DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odemeler = await GetOdemelerByDateRangeAsync(baslangic, bitis);
        var toplam = odemeler.Sum(o => o.Miktar);

        var masrafKalemleri = await context.BudgetMasrafKalemleri
            .ToDictionaryAsync(m => m.KalemAdi, m => m.Renk);

        return odemeler
            .GroupBy(o => o.MasrafKalemi)
            .Select(g => new BudgetKategoriOzet
            {
                MasrafKalemi = g.Key,
                Renk = masrafKalemleri.TryGetValue(g.Key, out var renk) ? renk : "#6c757d",
                Toplam = g.Sum(o => o.Miktar),
                Adet = g.Count(),
                Yuzde = toplam > 0 ? Math.Round(g.Sum(o => o.Miktar) / toplam * 100, 1) : 0
            })
            .OrderByDescending(k => k.Toplam)
            .ToList();
    }

    public async Task<List<BudgetTrendData>> GetTrendDataAsync(DateTime baslangic, DateTime bitis, string periyod)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odemeler = await GetOdemelerByDateRangeAsync(baslangic, bitis);
        var trendData = new List<BudgetTrendData>();

        if (periyod == "gunluk")
        {
            var gunler = odemeler.GroupBy(o => o.OdemeTarihi.Date);
            foreach (var gun in gunler.OrderBy(g => g.Key))
            {
                trendData.Add(new BudgetTrendData
                {
                    Etiket = gun.Key.ToString("dd.MM"),
                    Tarih = gun.Key,
                    Toplam = gun.Sum(o => o.Miktar),
                    Odenen = gun.Where(IsGerceklesenDurumu).Sum(GetGerceklesenTutar),
                    Bekleyen = gun.Where(IsBekleyenDurumu).Sum(GetBekleyenTutar),
                    OdemeSayisi = gun.Count()
                });
            }
        }
        else
        {
            var aylar = odemeler.GroupBy(o => new { o.OdemeYil, o.OdemeAy });
            foreach (var ay in aylar.OrderBy(a => a.Key.OdemeYil).ThenBy(a => a.Key.OdemeAy))
            {
                trendData.Add(new BudgetTrendData
                {
                    Etiket = AyAdlari[ay.Key.OdemeAy],
                    Tarih = new DateTime(ay.Key.OdemeYil, ay.Key.OdemeAy, 1),
                    Toplam = ay.Sum(o => o.Miktar),
                    Odenen = ay.Where(IsGerceklesenDurumu).Sum(GetGerceklesenTutar),
                    Bekleyen = ay.Where(IsBekleyenDurumu).Sum(GetBekleyenTutar),
                    OdemeSayisi = ay.Count()
                });
            }
        }

        return trendData;
    }

    #endregion

    #region Tekrarlayan Odeme Islemleri

    public async Task<List<TekrarlayanOdeme>> GetTekrarlayanOdemelerAsync(int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.TekrarlayanOdemeler
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (firmaId.HasValue)
            query = query.Where(t => t.FirmaId == firmaId.Value);

        return await query
            .Include(t => t.Firma)
            .OrderBy(t => t.Aktif ? 0 : 1) // Aktifler önce
            .ThenBy(t => t.OdemeGunu)
            .ThenBy(t => t.OdemeAdi)
            .ToListAsync();
    }

    public async Task<List<TekrarlayanOdeme>> GetAktifTekrarlayanOdemelerAsync(int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var query = context.TekrarlayanOdemeler
            .Where(t => !t.IsDeleted && t.Aktif);

        if (firmaId.HasValue)
            query = query.Where(t => t.FirmaId == firmaId.Value);

        return await query
            .Include(t => t.Firma)
            .OrderBy(t => t.OdemeGunu)
            .ThenBy(t => t.OdemeAdi)
            .ToListAsync();
    }

    public async Task<TekrarlayanOdeme?> GetTekrarlayanOdemeByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TekrarlayanOdemeler
            .Include(t => t.Firma)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    public async Task<TekrarlayanOdeme> CreateTekrarlayanOdemeAsync(TekrarlayanOdeme odeme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        odeme.BaslangicTarihi = DateTime.SpecifyKind(odeme.BaslangicTarihi, DateTimeKind.Utc);
        if (odeme.BitisTarihi.HasValue)
            odeme.BitisTarihi = DateTime.SpecifyKind(odeme.BitisTarihi.Value, DateTimeKind.Utc);
        odeme.CreatedAt = DateTime.UtcNow;

        context.TekrarlayanOdemeler.Add(odeme);
        await context.SaveChangesAsync();
        
        // Tracking'den cikar - ayni context uzerinde tekrar islem yapilabilsin
        context.Entry(odeme).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        
        return odeme;
    }

    public async Task<TekrarlayanOdeme> UpdateTekrarlayanOdemeAsync(TekrarlayanOdeme odeme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.TekrarlayanOdemeler.FindAsync(odeme.Id);
        if (existing == null)
            throw new Exception("Tekrarlayan odeme bulunamadi");

        existing.OdemeAdi = odeme.OdemeAdi;
        existing.MasrafKalemi = odeme.MasrafKalemi;
        existing.Aciklama = odeme.Aciklama;
        existing.Tutar = odeme.Tutar;
        existing.Periyod = odeme.Periyod;
        existing.OdemeGunu = odeme.OdemeGunu;
        existing.BaslangicTarihi = DateTime.SpecifyKind(odeme.BaslangicTarihi, DateTimeKind.Utc);
        existing.BitisTarihi = odeme.BitisTarihi.HasValue
            ? DateTime.SpecifyKind(odeme.BitisTarihi.Value, DateTimeKind.Utc)
            : null;
        existing.HatirlatmaGunSayisi = odeme.HatirlatmaGunSayisi;
        existing.FirmaId = odeme.FirmaId;
        existing.Aktif = odeme.Aktif;
        existing.Renk = odeme.Renk;
        existing.Icon = odeme.Icon;
        existing.Notlar = odeme.Notlar;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        
        // Tracking'den cikar
        context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        
        return existing;
    }

    public async Task DeleteTekrarlayanOdemeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.TekrarlayanOdemeler.FindAsync(id);
        if (odeme != null)
        {
            odeme.IsDeleted = true;
            odeme.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            context.Entry(odeme).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }
    }

    /// <summary>
    /// Tekrarlayan odeme tanimlarindan, belirtilen ay icin BudgetOdeme kayitlari olusturur.
    /// Ayni plan + ayni ay icin daha once kayit varsa tekrar olusturmaz.
    /// </summary>
    public async Task<int> TekrarlayanOdemelerdenKayitOlusturAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var aktifPlanlar = await GetAktifTekrarlayanOdemelerAsync(firmaId);
        var olusturulanSayisi = 0;

        foreach (var plan in aktifPlanlar)
        {
            // Periyod kontrolu - bu ay odeme gunu olup olmadigini kontrol et
            if (!PeriyodaUygunMu(plan, yil, ay))
                continue;

            // Odeme gunu - ayin gun sayisindan buyukse son gunu al
            var gunSayisi = DateTime.DaysInMonth(yil, ay);
            var odemeGunu = Math.Min(plan.OdemeGunu, gunSayisi);

            // Bu plan + bu ay icin kayit var mi kontrol et
            var mevcutKayit = await context.BudgetOdemeler
                .AnyAsync(o => o.OdemeYil == yil &&
                               o.OdemeAy == ay &&
                               o.MasrafKalemi == plan.MasrafKalemi &&
                               o.Aciklama != null && o.Aciklama.StartsWith("[Tekrarlayan") &&
                               o.Aciklama.Contains($"#{plan.Id}]"));

            if (!mevcutKayit)
            {
                var odemeTarihi = DateTime.SpecifyKind(new DateTime(yil, ay, odemeGunu), DateTimeKind.Utc);

                var yeniOdeme = new BudgetOdeme
                {
                    OdemeTarihi = odemeTarihi,
                    OdemeAy = ay,
                    OdemeYil = yil,
                    MasrafKalemi = plan.MasrafKalemi,
                    Aciklama = $"[Tekrarlayan#{plan.Id}] {plan.OdemeAdi}",
                    Miktar = plan.Tutar,
                    FirmaId = plan.FirmaId,
                    Durum = OdemeDurum.Bekliyor,
                    TaksitliMi = false,
                    ToplamTaksitSayisi = 1,
                    KacinciTaksit = 1,
                    Notlar = plan.Notlar,
                    CreatedAt = DateTime.UtcNow
                };

                context.BudgetOdemeler.Add(yeniOdeme);
                olusturulanSayisi++;
            }
        }

        if (olusturulanSayisi > 0)
            await context.SaveChangesAsync();

        return olusturulanSayisi;
    }

    /// <summary>
    /// Belirtilen tekrarlayan odeme planinin, verilen yil/ay icin gecerli olup olmadigini kontrol eder.
    /// </summary>
    private bool PeriyodaUygunMu(TekrarlayanOdeme plan, int yil, int ay)
    {
        var kontrolTarihi = new DateTime(yil, ay, 1);
        var baslangic = new DateTime(plan.BaslangicTarihi.Year, plan.BaslangicTarihi.Month, 1);

        if (kontrolTarihi < baslangic)
            return false;

        if (plan.BitisTarihi.HasValue)
        {
            var bitis = new DateTime(plan.BitisTarihi.Value.Year, plan.BitisTarihi.Value.Month, 1);
            if (kontrolTarihi > bitis)
                return false;
        }

        // Periyod kontrolu
        var ayFarki = ((yil - plan.BaslangicTarihi.Year) * 12) + (ay - plan.BaslangicTarihi.Month);
        var periyodAySayisi = (int)plan.Periyod;

        return ayFarki % periyodAySayisi == 0;
    }

    #endregion

    #region Kredi/Taksit Detay Metodları

    public async Task<List<KrediOzet>> GetKrediOzetleriAsync(int? yil = null, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.BudgetOdemeler
            .Where(o => o.TaksitliMi && o.TaksitGrupId.HasValue && !o.IsDeleted);

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value);

        var taksitliOdemeler = await query.ToListAsync();

        var krediler = taksitliOdemeler
            .GroupBy(o => o.TaksitGrupId!.Value)
            .Select(g =>
            {
                var taksitler = g.OrderBy(t => t.KacinciTaksit).ToList();
                var ilkTaksit = taksitler.First();
                var sonTaksit = taksitler.Last();
                var odenenTaksitler = taksitler.Where(t => t.Durum == OdemeDurum.Odendi).ToList();
                var bekleyenTaksitler = taksitler.Where(t => t.Durum == OdemeDurum.Bekliyor).ToList();
                var sonrakiTaksit = bekleyenTaksitler.OrderBy(t => t.OdemeTarihi).FirstOrDefault();

                return new KrediOzet
                {
                    TaksitGrupId = g.Key,
                    MasrafKalemi = ilkTaksit.MasrafKalemi,
                    Aciklama = ilkTaksit.Aciklama,
                    BaslangicTarihi = ilkTaksit.OdemeTarihi,
                    BitisTarihi = sonTaksit.OdemeTarihi,
                    ToplamTaksitSayisi = taksitler.Count,
                    OdenenTaksitSayisi = odenenTaksitler.Count,
                    KalanTaksitSayisi = bekleyenTaksitler.Count,
                    TaksitTutari = taksitler.First().Miktar,
                    ToplamTutar = taksitler.Sum(t => t.Miktar),
                    OdenenTutar = odenenTaksitler.Sum(t => t.Miktar),
                    KalanTutar = bekleyenTaksitler.Sum(t => t.Miktar),
                    TamamlanmaYuzdesi = taksitler.Count > 0 
                        ? Math.Round((decimal)odenenTaksitler.Count / taksitler.Count * 100, 1) 
                        : 0,
                    SonrakiTaksitTarihi = sonrakiTaksit?.OdemeTarihi
                };
            })
            .ToList();

        // Yıl filtresi
        if (yil.HasValue && yil > 0)
        {
            krediler = krediler
                .Where(k => k.BaslangicTarihi.Year <= yil && k.BitisTarihi.Year >= yil)
                .ToList();
        }

        return krediler.OrderBy(k => k.MasrafKalemi).ToList();
    }

    public async Task<List<KrediTaksitDetay>> GetKrediTaksitDetaylariAsync(Guid taksitGrupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var taksitler = await context.BudgetOdemeler
            .Where(o => o.TaksitGrupId == taksitGrupId && !o.IsDeleted)
            .OrderBy(o => o.KacinciTaksit)
            .ToListAsync();

        return taksitler.Select(t => new KrediTaksitDetay
        {
            MasrafKalemi = t.MasrafKalemi,
            Aciklama = t.Aciklama,
            KacinciTaksit = t.KacinciTaksit,
            ToplamTaksitSayisi = t.ToplamTaksitSayisi,
            Tutar = t.Miktar,
            OdenenTutar = t.Durum == OdemeDurum.KismiOdendi ? t.ToplamKismiOdenen : (t.OdenenTutar ?? (t.Durum == OdemeDurum.Odendi ? t.Miktar : 0)),
            KalanTutar = t.Durum == OdemeDurum.KismiOdendi ? t.KalanTutar : (t.Durum == OdemeDurum.Bekliyor ? t.Miktar : 0),
            OdemeYuzdesi = t.Durum == OdemeDurum.KismiOdendi ? t.OdemeYuzdesi : (t.Durum == OdemeDurum.Odendi ? 100 : 0),
            Durum = t.Durum,
            OdemeTarihi = t.OdemeTarihi
        }).ToList();
    }

    public async Task<BudgetOdeme?> GetTaksitOdemeAsync(Guid taksitGrupId, int taksitNo)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BudgetOdemeler
            .FirstOrDefaultAsync(o => o.TaksitGrupId == taksitGrupId && 
                                      o.KacinciTaksit == taksitNo && 
                                      !o.IsDeleted);
    }

    public async Task OdemeYapAsync(int odemeId, int bankaHesapId, DateTime odemeTarihi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.BudgetOdemeler.FindAsync(odemeId);
        if (odeme == null)
            throw new Exception("Ödeme bulunamadı");

        odeme.Durum = OdemeDurum.Odendi;
        odeme.GercekOdemeTarihi = DateTime.SpecifyKind(odemeTarihi, DateTimeKind.Utc);
        odeme.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }

    public Task AddKrediKartiBorcAsync(int bankaHesapId, decimal tutar, int ay, int yil, string aciklama)
        => AddKrediKartiBorcAsync(bankaHesapId, tutar, ay, yil, aciklama, null);

    public async Task AddKrediKartiBorcAsync(int bankaHesapId, decimal tutar, int ay, int yil, string aciklama, int? kaynakOdemeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Kredi kartı hesabını al
        var hesap = await context.BankaHesaplari.FindAsync(bankaHesapId);
        if (hesap == null || hesap.HesapTipi != HesapTipi.KrediKarti)
            throw new Exception("Geçerli bir kredi kartı hesabı seçiniz.");

        // Ekstre tarihi oluştur (ayın son günü)
        var ekstreTarihi = new DateTime(yil, ay, DateTime.DaysInMonth(yil, ay));
        ekstreTarihi = DateTime.SpecifyKind(ekstreTarihi, DateTimeKind.Utc);

        var normalizedAciklama = $"[{hesap.HesapAdi}] {aciklama}";
        var mevcutAdaylar = await context.BudgetOdemeler
            .Where(o => o.MasrafKalemi == "Kredi Kartı"
                && o.OdemeYil == yil
                && o.OdemeAy == ay
                && o.OdemeYapildigiHesapId == bankaHesapId)
            .ToListAsync();

        var mevcutKayit = mevcutAdaylar.FirstOrDefault(o =>
            (kaynakOdemeId.HasValue && GetBudgetMeta(o.Notlar, "KaynakOdemeId") == kaynakOdemeId.Value.ToString())
            || string.Equals(o.Aciklama, normalizedAciklama, StringComparison.Ordinal));

        if (kaynakOdemeId.HasValue)
        {
            foreach (var digerKayit in mevcutAdaylar.Where(o => o.Id != mevcutKayit?.Id && GetBudgetMeta(o.Notlar, "KaynakOdemeId") == kaynakOdemeId.Value.ToString()))
            {
                digerKayit.IsDeleted = true;
                digerKayit.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (mevcutKayit != null)
        {
            mevcutKayit.OdemeTarihi = ekstreTarihi;
            mevcutKayit.OdemeAy = ay;
            mevcutKayit.OdemeYil = yil;
            mevcutKayit.Aciklama = normalizedAciklama;
            mevcutKayit.Miktar = tutar;
            mevcutKayit.Durum = OdemeDurum.Bekliyor;
            mevcutKayit.OdemeYapildigiHesapId = bankaHesapId;
            mevcutKayit.TaksitGrupId = hesap.KrediTaksitGrupId;
            mevcutKayit.Notlar = UpsertBudgetMeta(mevcutKayit.Notlar, "FinansKaynakHesapId", bankaHesapId.ToString());
            mevcutKayit.Notlar = UpsertBudgetMeta(mevcutKayit.Notlar, "KaynakOdemeId", kaynakOdemeId?.ToString());
            mevcutKayit.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return;
        }

        // BudgetOdeme olarak kaydet
        var odeme = new BudgetOdeme
        {
            OdemeTarihi = ekstreTarihi,
            OdemeAy = ay,
            OdemeYil = yil,
            MasrafKalemi = "Kredi Kartı",
            Aciklama = normalizedAciklama,
            Miktar = tutar,
            Durum = OdemeDurum.Bekliyor,
            OdemeYapildigiHesapId = bankaHesapId,
            TaksitGrupId = hesap.KrediTaksitGrupId, // Kredi kartı ile ilişkilendir
            Notlar = UpsertBudgetMeta(
                UpsertBudgetMeta(null, "FinansKaynakHesapId", bankaHesapId.ToString()),
                "KaynakOdemeId",
                kaynakOdemeId?.ToString()),
            CreatedAt = DateTime.UtcNow
        };

        context.BudgetOdemeler.Add(odeme);
        await context.SaveChangesAsync();
    }

    public async Task<List<BudgetOdeme>> GetKrediKartiHareketleriAsync(int bankaHesapId, int? yil = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hesap = await context.BankaHesaplari.FindAsync(bankaHesapId);
        if (hesap == null) return new List<BudgetOdeme>();

        var query = context.BudgetOdemeler
            .Where(o => !o.IsDeleted && o.MasrafKalemi == "Kredi Kartı" && 
                        o.Aciklama != null && o.Aciklama.StartsWith($"[{hesap.HesapAdi}]"));

        if (yil.HasValue)
        {
            query = query.Where(o => o.OdemeYil == yil.Value);
        }

        return await query
            .OrderByDescending(o => o.OdemeTarihi)
            .ToListAsync();
    }

    public async Task TaksitliOdemeOlusturAsync(object request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Request'i dynamic olarak işle
        var requestType = request.GetType();
        var masrafKalemi = requestType.GetProperty("MasrafKalemi")?.GetValue(request)?.ToString() ?? "";
        var aciklama = requestType.GetProperty("Aciklama")?.GetValue(request)?.ToString();
        var baslangicTarihi = (DateTime)(requestType.GetProperty("BaslangicTarihi")?.GetValue(request) ?? DateTime.Today);
        var taksitSayisi = (int)(requestType.GetProperty("TaksitSayisi")?.GetValue(request) ?? 1);
        var toplamTutar = (decimal)(requestType.GetProperty("ToplamTutar")?.GetValue(request) ?? 0);

        var taksitliRequest = new TaksitliOdemeRequest
        {
            MasrafKalemi = masrafKalemi,
            Aciklama = aciklama,
            BaslangicTarihi = baslangicTarihi,
            TaksitSayisi = taksitSayisi,
            ToplamTutar = toplamTutar
        };

        await CreateTaksitliOdemeAsync(taksitliRequest);
    }

    #endregion

    #region Hedef Yönetimi

    public async Task<List<BudgetHedef>> GetHedeflerAsync(int yil, int? ay = null, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.BudgetHedefler
            .Include(h => h.Firma)
            .Where(h => h.Yil == yil);

        if (ay.HasValue)
            query = query.Where(h => h.Ay == ay.Value || h.Ay == 0); // Belirli ay veya yıllık hedefler

        if (firmaId.HasValue)
            query = query.Where(h => h.FirmaId == firmaId.Value || h.FirmaId == null);

        return await query
            .OrderBy(h => h.Ay)
            .ThenBy(h => h.MasrafKalemi)
            .ToListAsync();
    }

    public async Task<BudgetHedef?> GetHedefByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BudgetHedefler
            .Include(h => h.Firma)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<BudgetHedef> CreateHedefAsync(BudgetHedef hedef)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Aynı yıl/ay/masraf kalemi için hedef var mı kontrol et
        var mevcutHedef = await context.BudgetHedefler
            .FirstOrDefaultAsync(h => h.Yil == hedef.Yil && 
                                      h.Ay == hedef.Ay && 
                                      h.MasrafKalemi == hedef.MasrafKalemi &&
                                      h.FirmaId == hedef.FirmaId);

        if (mevcutHedef != null)
        {
            // Mevcut hedefi güncelle
            mevcutHedef.HedefTutar = hedef.HedefTutar;
            mevcutHedef.Aciklama = hedef.Aciklama;
            mevcutHedef.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return mevcutHedef;
        }

        hedef.CreatedAt = DateTime.UtcNow;
        context.BudgetHedefler.Add(hedef);
        await context.SaveChangesAsync();
        return hedef;
    }

    public async Task<BudgetHedef> UpdateHedefAsync(BudgetHedef hedef)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.BudgetHedefler.FindAsync(hedef.Id);
        if (existing == null)
            throw new ArgumentException($"Hedef bulunamadı: {hedef.Id}");

        existing.Yil = hedef.Yil;
        existing.Ay = hedef.Ay;
        existing.MasrafKalemi = hedef.MasrafKalemi;
        existing.HedefTutar = hedef.HedefTutar;
        existing.Aciklama = hedef.Aciklama;
        existing.FirmaId = hedef.FirmaId;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteHedefAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hedef = await context.BudgetHedefler.FindAsync(id);
        if (hedef != null)
        {
            hedef.IsDeleted = true;
            hedef.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<int> KopyalaHedeflerAsync(int kaynakYil, int hedefYil, decimal artisOrani = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kaynakHedefler = await context.BudgetHedefler
            .Where(h => h.Yil == kaynakYil)
            .ToListAsync();

        var kopyalananSayisi = 0;

        foreach (var kaynak in kaynakHedefler)
        {
            // Hedef yılda aynı kalem var mı kontrol et
            var mevcutHedef = await context.BudgetHedefler
                .FirstOrDefaultAsync(h => h.Yil == hedefYil && 
                                          h.Ay == kaynak.Ay && 
                                          h.MasrafKalemi == kaynak.MasrafKalemi &&
                                          h.FirmaId == kaynak.FirmaId);

            if (mevcutHedef == null)
            {
                var yeniHedef = new BudgetHedef
                {
                    Yil = hedefYil,
                    Ay = kaynak.Ay,
                    MasrafKalemi = kaynak.MasrafKalemi,
                    HedefTutar = kaynak.HedefTutar * (1 + artisOrani / 100),
                    Aciklama = $"[{kaynakYil}'den kopyalandı] {kaynak.Aciklama}",
                    FirmaId = kaynak.FirmaId,
                    CreatedAt = DateTime.UtcNow
                };

                context.BudgetHedefler.Add(yeniHedef);
                kopyalananSayisi++;
            }
        }

        if (kopyalananSayisi > 0)
            await context.SaveChangesAsync();

        return kopyalananSayisi;
    }

    #endregion

    #region Hedef vs Gerçekleşen Karşılaştırma

    public async Task<List<BudgetHedefGerceklesen>> GetHedefGerceklesenAsync(int yil, int? ay = null, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Hedefleri al
        var hedefler = await GetHedeflerAsync(yil, ay, firmaId);

        // Gerçekleşen ödemeleri al (kategori bazlı toplam)
        var gerceklesenQuery = context.BudgetOdemeler
            .Where(o => o.OdemeYil == yil && 
                        (o.Durum == OdemeDurum.Odendi || o.FaturaIleKapatildi));

        if (ay.HasValue)
            gerceklesenQuery = gerceklesenQuery.Where(o => o.OdemeAy == ay.Value);

        if (firmaId.HasValue)
            gerceklesenQuery = gerceklesenQuery.Where(o => o.FirmaId == firmaId.Value);

        var gerceklesenGruplu = await gerceklesenQuery
            .GroupBy(o => new { o.MasrafKalemi, Ay = ay.HasValue ? o.OdemeAy : 0 })
            .Select(g => new 
            { 
                g.Key.MasrafKalemi, 
                g.Key.Ay,
                Toplam = g.Sum(o => o.OdenenTutar ?? o.Miktar) 
            })
            .ToListAsync();

        // Masraf kalemlerinin renklerini al
        var masrafKalemleri = await context.BudgetMasrafKalemleri.ToListAsync();

        // Birleştir
        var sonuc = new List<BudgetHedefGerceklesen>();

        // Hedefi olan kalemler
        foreach (var hedef in hedefler.Where(h => h.Ay == (ay ?? 0)))
        {
            var gerceklesen = gerceklesenGruplu
                .FirstOrDefault(g => g.MasrafKalemi == hedef.MasrafKalemi);

            var kalem = masrafKalemleri.FirstOrDefault(k => k.KalemAdi == hedef.MasrafKalemi);

            sonuc.Add(new BudgetHedefGerceklesen
            {
                MasrafKalemi = hedef.MasrafKalemi,
                Ay = hedef.Ay,
                Yil = hedef.Yil,
                HedefTutar = hedef.HedefTutar,
                GerceklesenTutar = gerceklesen?.Toplam ?? 0,
                Renk = kalem?.Renk
            });
        }

        // Hedefi olmayan ama harcama yapılan kalemler
        foreach (var gerceklesen in gerceklesenGruplu)
        {
            if (!sonuc.Any(s => s.MasrafKalemi == gerceklesen.MasrafKalemi))
            {
                var kalem = masrafKalemleri.FirstOrDefault(k => k.KalemAdi == gerceklesen.MasrafKalemi);

                sonuc.Add(new BudgetHedefGerceklesen
                {
                    MasrafKalemi = gerceklesen.MasrafKalemi,
                    Ay = gerceklesen.Ay,
                    Yil = yil,
                    HedefTutar = 0,
                    GerceklesenTutar = gerceklesen.Toplam,
                    Renk = kalem?.Renk
                });
            }
        }

        return sonuc.OrderBy(s => s.MasrafKalemi).ToList();
    }

    public async Task<BudgetYillikHedefOzet> GetYillikHedefOzetAsync(int yil, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ozet = new BudgetYillikHedefOzet { Yil = yil };

        // Kategori detayları (yıllık toplam)
        ozet.KategoriDetaylari = await GetHedefGerceklesenAsync(yil, null, firmaId);

        ozet.ToplamHedef = ozet.KategoriDetaylari.Sum(k => k.HedefTutar);
        ozet.ToplamGerceklesen = ozet.KategoriDetaylari.Sum(k => k.GerceklesenTutar);

        // Aylık detaylar
        for (int ay = 1; ay <= 12; ay++)
        {
            var aylikHedefler = await context.BudgetHedefler
                .Where(h => h.Yil == yil && h.Ay == ay)
                .ToListAsync();

            var aylikGerceklesen = await context.BudgetOdemeler
                .Where(o => o.OdemeYil == yil && o.OdemeAy == ay && 
                            (o.Durum == OdemeDurum.Odendi || o.FaturaIleKapatildi))
                .SumAsync(o => o.OdenenTutar ?? o.Miktar);

            // Yıllık hedeften aylık payı hesapla (hedef tanımlı değilse)
            var aylikHedefToplam = aylikHedefler.Sum(h => h.HedefTutar);
            if (aylikHedefToplam == 0 && ozet.ToplamHedef > 0)
            {
                aylikHedefToplam = ozet.ToplamHedef / 12; // Eşit dağılım
            }

            ozet.AylikDetaylar.Add(new BudgetAylikHedefOzet
            {
                Ay = ay,
                AyAdi = AyAdlari[ay],
                HedefTutar = aylikHedefToplam,
                GerceklesenTutar = aylikGerceklesen
            });
        }

        return ozet;
    }

    #endregion

    #region Kısmi Ödeme İşlemleri

    public async Task<BudgetOdeme> KismiOdemeYapAsync(int odemeId, KismiOdemeRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.BudgetOdemeler
            .Include(o => o.OdemeYapildigiHesap)
            .FirstOrDefaultAsync(o => o.Id == odemeId);

        if (odeme == null)
            throw new Exception("Ödeme kaydı bulunamadı.");

        if (odeme.Durum == OdemeDurum.Odendi)
            throw new Exception("Bu ödeme zaten tamamlanmış.");

        if (request.OdenecekTutar <= 0)
            throw new Exception("Ödenecek tutar 0'dan büyük olmalıdır.");

        var orijinalMiktar = odeme.Miktar;
        var kalanTutar = orijinalMiktar - odeme.ToplamKismiOdenen;
        if (request.OdenecekTutar > kalanTutar)
            throw new Exception($"Ödenecek tutar kalan tutardan ({kalanTutar:N2} TL) fazla olamaz.");

        var odenecekTutar = RoundCurrency(Math.Abs(request.OdenecekTutar));
        var masrafKesintisi = RoundCurrency(Math.Abs(request.MasrafKesintisi));
        var cezaKesintisi = RoundCurrency(Math.Abs(request.CezaKesintisi));
        var digerKesinti = RoundCurrency(Math.Abs(request.DigerKesinti));
        var odemeTarihi = DateTime.SpecifyKind(request.OdemeTarihi, DateTimeKind.Utc);
        int? bankaKasaHareketId = null;

        // Banka/Kasa hareketi oluştur
        if (request.BankaHesapId.HasValue && request.BankaHesapId > 0)
        {
            var islemNo = await _bankaKasaHareketService.GenerateNextIslemNoAsync();
            var hareket = new BankaKasaHareket
            {
                IslemNo = islemNo,
                BankaHesapId = request.BankaHesapId.Value,
                IslemTarihi = odemeTarihi,
                HareketTipi = HareketTipi.Cikis,
                Tutar = odenecekTutar + masrafKesintisi + cezaKesintisi + digerKesinti,
                IslemKaynak = IslemKaynak.Butce,
                Aciklama = $"[Kısmi Ödeme] {odeme.MasrafKalemi} - {odeme.Aciklama}",
                BelgeNo = $"KO-{odeme.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                CariId = request.CariId,
                CreatedAt = DateTime.UtcNow
            };

            var kaydedilenHareket = await _bankaKasaHareketService.CreateAsync(hareket);
            bankaKasaHareketId = kaydedilenHareket.Id;

            if (ShouldCreateKrediKartiBorcu(odeme, request.OdemeTipi))
            {
                var donemAy = request.HedefAy ?? request.OdemeTarihi.Month;
                var donemYil = request.HedefYil ?? request.OdemeTarihi.Year;
                await AddKrediKartiBorcAsync(request.BankaHesapId.Value, hareket.Tutar, donemAy, donemYil, hareket.Aciklama, odeme.Id);
            }
        }

        // Kalan tutarı hesapla (ödeme sonrası)
        var yeniToplamOdenen = RoundCurrency(odeme.ToplamKismiOdenen + odenecekTutar);
        var odemeSonrasiKalan = RoundCurrency(orijinalMiktar - yeniToplamOdenen);

        // Sonraki döneme aktarma: devir kaydını doğrudan burada oluştur
        int? sonrakiDonemOdemeId = null;
        if (request.KalanSonrakiDonemeAktarilsin && odemeSonrasiKalan > 0)
        {
            // Hedef ay/yıl hesapla
            var hedefAy = request.HedefAy ?? (odeme.OdemeAy == 12 ? 1 : odeme.OdemeAy + 1);
            var hedefYil = request.HedefYil ?? (odeme.OdemeAy == 12 ? odeme.OdemeYil + 1 : odeme.OdemeYil);

            // Mevcut devir kaydı var mı kontrol et
            var mevcutDevir = await context.BudgetOdemeler
                .FirstOrDefaultAsync(o => o.OncekiDonemOdemeId == odeme.Id && !o.IsDeleted);

            if (mevcutDevir != null)
            {
                mevcutDevir.Miktar = odemeSonrasiKalan;
                mevcutDevir.OdemeAy = hedefAy;
                mevcutDevir.OdemeYil = hedefYil;
                mevcutDevir.OdemeTarihi = DateTime.SpecifyKind(new DateTime(hedefYil, hedefAy, 1), DateTimeKind.Utc);
                mevcutDevir.Aciklama = BuildDevirAciklama(odeme.Aciklama);
                mevcutDevir.UpdatedAt = DateTime.UtcNow;
                sonrakiDonemOdemeId = mevcutDevir.Id;
            }
            else
            {
                var yeniDevir = new BudgetOdeme
                {
                    OdemeTarihi = DateTime.SpecifyKind(new DateTime(hedefYil, hedefAy, 1), DateTimeKind.Utc),
                    OdemeAy = hedefAy,
                    OdemeYil = hedefYil,
                    MasrafKalemi = odeme.MasrafKalemi,
                    Aciklama = BuildDevirAciklama(odeme.Aciklama),
                    Miktar = odemeSonrasiKalan,
                    FirmaId = odeme.FirmaId,
                    Durum = OdemeDurum.Bekliyor,
                    OncekiDonemOdemeId = odeme.Id,
                    Notlar = $"Önceki dönem ödeme ID: {odeme.Id}, Orijinal tutar: {orijinalMiktar:N2} TL, Ödenen: {yeniToplamOdenen:N2} TL",
                    CreatedAt = DateTime.UtcNow
                };
                context.BudgetOdemeler.Add(yeniDevir);
                // Flush ile ID alalım
                await context.SaveChangesAsync();
                sonrakiDonemOdemeId = yeniDevir.Id;
            }
        }

        // Ana ödemeyi ExecuteUpdateAsync ile doğrudan güncelle (tracking sorunlarını önler)
        var yeniDurum = sonrakiDonemOdemeId.HasValue
            ? OdemeDurum.Odendi   // Kalan aktarıldı → bu kayıt kapansın
            : (yeniToplamOdenen >= orijinalMiktar ? OdemeDurum.Odendi : OdemeDurum.KismiOdendi);

        var yeniMiktar = sonrakiDonemOdemeId.HasValue ? yeniToplamOdenen : orijinalMiktar;
        var updatedAt = DateTime.UtcNow;

        await context.BudgetOdemeler
            .Where(o => o.Id == odemeId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.ToplamKismiOdenen, yeniToplamOdenen)
                .SetProperty(o => o.KismiOdemeMi, true)
                .SetProperty(o => o.OdenenTutar, yeniToplamOdenen)
                .SetProperty(o => o.BankaKasaHareketId, bankaKasaHareketId ?? odeme.BankaKasaHareketId)
                .SetProperty(o => o.MasrafKesintisi, RoundCurrency(odeme.MasrafKesintisi + masrafKesintisi))
                .SetProperty(o => o.CezaKesintisi, RoundCurrency(odeme.CezaKesintisi + cezaKesintisi))
                .SetProperty(o => o.DigerKesinti, RoundCurrency(odeme.DigerKesinti + digerKesinti))
                .SetProperty(o => o.KesintiAciklamasi, request.KesintiAciklamasi)
                .SetProperty(o => o.KalanSonrakiDonemeAktarilsin, request.KalanSonrakiDonemeAktarilsin)
                .SetProperty(o => o.OdemeYapildigiHesapId, request.BankaHesapId)
                .SetProperty(o => o.GercekOdemeTarihi, odemeTarihi)
                .SetProperty(o => o.Durum, yeniDurum)
                .SetProperty(o => o.Miktar, yeniMiktar)
                .SetProperty(o => o.SonrakiDonemOdemeId, sonrakiDonemOdemeId ?? odeme.SonrakiDonemOdemeId)
                .SetProperty(o => o.UpdatedAt, updatedAt));

        // In-memory entity'yi de güncelle (UI'a döndürmek için)
        odeme.ToplamKismiOdenen = yeniToplamOdenen;
        odeme.KismiOdemeMi = true;
        odeme.OdenenTutar = yeniToplamOdenen;
        odeme.Durum = yeniDurum;
        odeme.Miktar = yeniMiktar;
        odeme.SonrakiDonemOdemeId = sonrakiDonemOdemeId ?? odeme.SonrakiDonemOdemeId;
        odeme.KalanSonrakiDonemeAktarilsin = request.KalanSonrakiDonemeAktarilsin;
        odeme.UpdatedAt = updatedAt;

        return odeme;
    }

    public async Task<BudgetOdeme?> KalanTutariSonrakiDonemeAktarAsync(int odemeId, int? hedefAy = null, int? hedefYil = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.BudgetOdemeler.FindAsync(odemeId);
        if (odeme == null) return null;

        var kalanTutar = odeme.Miktar - odeme.ToplamKismiOdenen;
        if (kalanTutar <= 0) return null;

        var mevcutDevirKayitlari = await context.BudgetOdemeler
            .Where(o => o.OncekiDonemOdemeId == odeme.Id && !o.IsDeleted)
            .OrderBy(o => o.Id)
            .ToListAsync();

        if (mevcutDevirKayitlari.Count > 1)
        {
            foreach (var silinecekKayit in mevcutDevirKayitlari.Skip(1))
            {
                silinecekKayit.IsDeleted = true;
                silinecekKayit.UpdatedAt = DateTime.UtcNow;
            }
        }

        var mevcutSonrakiOdeme = odeme.SonrakiDonemOdemeId.HasValue
            ? await context.BudgetOdemeler.FindAsync(odeme.SonrakiDonemOdemeId.Value)
            : mevcutDevirKayitlari.FirstOrDefault();

        if (mevcutSonrakiOdeme != null && mevcutSonrakiOdeme.IsDeleted)
        {
            mevcutSonrakiOdeme = null;
        }

        if (mevcutSonrakiOdeme != null)
        {
            mevcutSonrakiOdeme.Miktar = kalanTutar;
            mevcutSonrakiOdeme.OdemeAy = hedefAy ?? mevcutSonrakiOdeme.OdemeAy;
            mevcutSonrakiOdeme.OdemeYil = hedefYil ?? mevcutSonrakiOdeme.OdemeYil;
            mevcutSonrakiOdeme.OdemeTarihi = DateTime.SpecifyKind(new DateTime(mevcutSonrakiOdeme.OdemeYil, mevcutSonrakiOdeme.OdemeAy, 1), DateTimeKind.Utc);
            mevcutSonrakiOdeme.Aciklama = BuildDevirAciklama(odeme.Aciklama);
            mevcutSonrakiOdeme.UpdatedAt = DateTime.UtcNow;
            odeme.SonrakiDonemOdemeId = mevcutSonrakiOdeme.Id;
            await context.SaveChangesAsync();
            return mevcutSonrakiOdeme;
        }

        // Sonraki ay/yıl hesapla
        var sonrakiAy = hedefAy ?? (odeme.OdemeAy + 1);
        var sonrakiYil = hedefYil ?? odeme.OdemeYil;

        if (!hedefAy.HasValue && sonrakiAy > 12)
        {
            sonrakiAy = 1;
            sonrakiYil++;
        }

        if (sonrakiAy is < 1 or > 12)
            throw new InvalidOperationException("Hedef ay 1-12 arasında olmalıdır.");

        if (hedefYil.HasValue && hedefYil.Value < odeme.OdemeYil)
            throw new InvalidOperationException("Hedef yıl mevcut ödeme yılından küçük olamaz.");

        if (hedefYil == odeme.OdemeYil && hedefAy.HasValue && hedefAy.Value <= odeme.OdemeAy)
            throw new InvalidOperationException("Hedef dönem mevcut ödeme döneminden sonra olmalıdır.");

        // Yeni dönem için kalan tutarla ödeme oluştur
        var yeniOdeme = new BudgetOdeme
        {
            OdemeTarihi = DateTime.SpecifyKind(new DateTime(sonrakiYil, sonrakiAy, 1), DateTimeKind.Utc),
            OdemeAy = sonrakiAy,
            OdemeYil = sonrakiYil,
            MasrafKalemi = odeme.MasrafKalemi,
            Aciklama = BuildDevirAciklama(odeme.Aciklama),
            Miktar = kalanTutar,
            FirmaId = odeme.FirmaId,
            Durum = OdemeDurum.Bekliyor,
            OncekiDonemOdemeId = odeme.Id,
            Notlar = $"Önceki dönem ödeme ID: {odeme.Id}, Orijinal tutar: {odeme.Miktar:N2} TL, Ödenen: {odeme.ToplamKismiOdenen:N2} TL",
            CreatedAt = DateTime.UtcNow
        };

        context.BudgetOdemeler.Add(yeniOdeme);
        await context.SaveChangesAsync();

        // Ana ödemeyi güncelle
        odeme.SonrakiDonemOdemeId = yeniOdeme.Id;
        odeme.KalanSonrakiDonemeAktarilsin = true;
        await context.SaveChangesAsync();

        return yeniOdeme;
    }

    private static string BuildDevirAciklama(string? aciklama)
    {
        var temizAciklama = aciklama?.Trim() ?? string.Empty;

        while (temizAciklama.StartsWith("[Devir]", StringComparison.OrdinalIgnoreCase))
        {
            temizAciklama = temizAciklama.Substring(7).TrimStart();
        }

        return string.IsNullOrWhiteSpace(temizAciklama)
            ? "[Devir] Önceki dönemden kalan"
            : $"[Devir] {temizAciklama} - Önceki dönemden kalan";
    }

    public async Task<List<BudgetOdeme>> GetKismiOdenmislerAsync(int yil, int? ay = null, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.BudgetOdemeler
            .Include(o => o.OdemeYapildigiHesap)
            .Include(o => o.Firma)
            .Where(o => o.OdemeYil == yil && o.KismiOdemeMi);

        if (ay.HasValue)
            query = query.Where(o => o.OdemeAy == ay.Value);

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value);

        return await query
            .OrderBy(o => o.OdemeAy)
            .ThenBy(o => o.OdemeTarihi)
            .ToListAsync();
    }

    #endregion

    #region Risk Analizi

    public async Task<BudgetRiskAnalizi> GetRiskAnaliziAsync(int yil, int? ay = null, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.UtcNow;
        var analiz = new BudgetRiskAnalizi
        {
            Yil = yil,
            Ay = ay,
            AnalizTarihi = bugun
        };

        // Tüm ödemeleri al
        var query = context.BudgetOdemeler
            .Include(o => o.Firma)
            .Where(o => o.OdemeYil == yil && !o.IsDeleted);

        if (ay.HasValue)
            query = query.Where(o => o.OdemeAy == ay.Value);

        if (firmaId.HasValue)
            query = query.Where(o => o.FirmaId == firmaId.Value || o.FirmaId == null);

        var odemeler = await query.ToListAsync();

        // Genel istatistikler
        analiz.ToplamKayit = odemeler.Count;
        analiz.ToplamBekleyen = odemeler.Where(o => o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi).Sum(o => o.Miktar - o.ToplamKismiOdenen);
        analiz.ToplamKismiOdenen = odemeler.Where(o => o.KismiOdemeMi).Sum(o => o.ToplamKismiOdenen);
        analiz.KismiOdenenKayit = odemeler.Count(o => o.KismiOdemeMi);

        // Geciken ödemeler
        var gecikenler = odemeler.Where(o => 
            (o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi) && 
            o.OdemeTarihi < bugun).ToList();

        analiz.GecikenKayit = gecikenler.Count;
        analiz.ToplamGeciken = gecikenler.Sum(o => o.Miktar - o.ToplamKismiOdenen);

        // Riskli ödemeler listesi
        foreach (var odeme in gecikenler.OrderByDescending(o => o.Miktar - o.ToplamKismiOdenen).Take(20))
        {
            var gecikmeGunu = (int)(bugun - odeme.OdemeTarihi).TotalDays;
            var kalanTutar = odeme.Miktar - odeme.ToplamKismiOdenen;

            var riskSeviyesi = gecikmeGunu switch
            {
                <= 7 => "Normal",
                <= 15 => "Orta",
                <= 30 => "Yüksek",
                _ => "Kritik"
            };

            analiz.RiskliOdemeler.Add(new RiskliOdeme
            {
                OdemeId = odeme.Id,
                MasrafKalemi = odeme.MasrafKalemi,
                Aciklama = odeme.Aciklama,
                Tutar = odeme.Miktar,
                KalanTutar = kalanTutar,
                VadeTarihi = odeme.OdemeTarihi,
                GecikmeGunu = gecikmeGunu,
                RiskSeviyesi = riskSeviyesi,
                RiskAciklamasi = $"{gecikmeGunu} gün gecikmiş, {kalanTutar:N2} TL ödenmemiş"
            });
        }

        // Kategori bazlı risk özeti
        var kategoriler = odemeler.GroupBy(o => o.MasrafKalemi);
        foreach (var kategori in kategoriler)
        {
            var bekleyenler = kategori.Where(o => o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi).ToList();
            var gecikenKategori = bekleyenler.Where(o => o.OdemeTarihi < bugun).ToList();

            var toplamTutar = kategori.Sum(o => o.Miktar);
            var bekleyenTutar = bekleyenler.Sum(o => o.Miktar - o.ToplamKismiOdenen);
            var gecikenTutar = gecikenKategori.Sum(o => o.Miktar - o.ToplamKismiOdenen);

            var riskSkoru = toplamTutar > 0 ? Math.Round(gecikenTutar / toplamTutar * 100, 1) : 0;

            analiz.KategoriRiskleri.Add(new KategoriRiskOzeti
            {
                Kategori = kategori.Key,
                ToplamTutar = toplamTutar,
                BekleyenTutar = bekleyenTutar,
                GecikenTutar = gecikenTutar,
                GecikenKayit = gecikenKategori.Count,
                RiskSkoru = riskSkoru
            });

            // Kategori bazlı ödeme detayları
            analiz.KategoriOdemeleri[kategori.Key] = kategori
                .OrderBy(o => o.OdemeTarihi)
                .Select(o => new KategoriOdemeItem
                {
                    OdemeId = o.Id,
                    Aciklama = o.Aciklama,
                    Miktar = o.Miktar,
                    KalanTutar = o.Miktar - o.ToplamKismiOdenen,
                    OdemeTarihi = o.OdemeTarihi,
                    Durum = o.Durum,
                    TaksitliMi = o.TaksitliMi,
                    GecikmeGunu = (o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi) && o.OdemeTarihi < bugun
                        ? (int)(bugun - o.OdemeTarihi).TotalDays
                        : 0
                }).ToList();
        }

        // Aylık trend
        if (!ay.HasValue)
        {
            for (int m = 1; m <= 12; m++)
            {
                var aylikOdemeler = odemeler.Where(o => o.OdemeAy == m).ToList();
                var aylikBeklenen = aylikOdemeler.Sum(o => o.Miktar);
                var aylikGerceklesen = aylikOdemeler.Where(o => o.Durum == OdemeDurum.Odendi).Sum(o => o.OdenenTutar ?? o.Miktar);
                var aylikGeciken = aylikOdemeler.Where(o => (o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi) && o.OdemeTarihi < bugun).Sum(o => o.Miktar - o.ToplamKismiOdenen);

                var aylikRiskSkoru = aylikBeklenen > 0 ? Math.Round(aylikGeciken / aylikBeklenen * 100, 1) : 0;

                analiz.AylikTrendler.Add(new AylikRiskTrendi
                {
                    Ay = m,
                    AyAdi = AyAdlari[m],
                    BeklenenOdeme = aylikBeklenen,
                    GerceklesenOdeme = aylikGerceklesen,
                    GecikenOdeme = aylikGeciken,
                    RiskSkoru = aylikRiskSkoru
                });
            }
        }

        // Risk skorları hesapla
        var toplamOdeme = odemeler.Sum(o => o.Miktar);
        analiz.OdemeGecikmesiRiski = toplamOdeme > 0 ? Math.Round(analiz.ToplamGeciken / toplamOdeme * 100, 1) : 0;
        analiz.LikiditeRiski = toplamOdeme > 0 ? Math.Round(analiz.ToplamBekleyen / toplamOdeme * 100, 1) : 0;

        // Hedeflerden sapma riski
        var hedefler = await GetHedeflerAsync(yil, ay, firmaId);
        var toplamHedef = hedefler.Sum(h => h.HedefTutar);
        var toplamGerceklesen = odemeler.Where(o => o.Durum == OdemeDurum.Odendi).Sum(o => o.OdenenTutar ?? o.Miktar);
        analiz.BudceSapmaRiski = toplamHedef > 0 ? Math.Round(Math.Abs(toplamGerceklesen - toplamHedef) / toplamHedef * 100, 1) : 0;

        // Genel risk skoru (ağırlıklı ortalama)
        analiz.GenelRiskSkoru = Math.Round(
            (analiz.OdemeGecikmesiRiski * 0.5m) + 
            (analiz.LikiditeRiski * 0.3m) + 
            (analiz.BudceSapmaRiski * 0.2m), 1);

        // Uyarılar
        if (analiz.GecikenKayit > 0)
            analiz.Uyarilar.Add($"⚠️ {analiz.GecikenKayit} adet gecikmiş ödeme bulunuyor (Toplam: {analiz.ToplamGeciken:N2} TL)");

        if (analiz.KismiOdenenKayit > 0)
            analiz.Uyarilar.Add($"📊 {analiz.KismiOdenenKayit} adet kısmi ödenmiş kayıt var");

        if (analiz.GenelRiskSkoru > 50)
            analiz.Uyarilar.Add($"🔴 Yüksek risk seviyesi! Genel risk skoru: {analiz.GenelRiskSkoru}");
        else if (analiz.GenelRiskSkoru > 30)
            analiz.Uyarilar.Add($"🟡 Orta risk seviyesi. Genel risk skoru: {analiz.GenelRiskSkoru}");

        // Öneriler
        if (analiz.ToplamGeciken > 0)
            analiz.Oneriler.Add("💡 Geciken ödemeleri öncelikli olarak kapatmayı değerlendirin");

        if (analiz.KismiOdenenKayit > 3)
            analiz.Oneriler.Add("💡 Kısmi ödemelerin tamamlanması için ödeme planı oluşturulabilir");

        var yuksekRiskliKategoriler = analiz.KategoriRiskleri.Where(k => k.RiskSkoru > 30).ToList();
        foreach (var kategori in yuksekRiskliKategoriler)
        {
            analiz.Oneriler.Add($"💡 {kategori.Kategori} kategorisinde risk yüksek (%{kategori.RiskSkoru}). Bu kalem için bütçe revizyonu önerilir.");
        }

        return analiz;
    }

    /// <summary>
    /// Veritabanındaki bozuk kısmi ödeme/devir kayıtlarını tespit edip düzeltir:
    /// 1. KalanSonrakiDonemeAktarilsin=true olup Durum hâlâ KismiOdendi olan ana kayıtları Odendi yapar
    /// 2. Mükerrer kredi kartı borç kayıtlarını soft-delete yapar
    /// 3. Ana kaydı Odendi olan devir kayıtlarının tutarını (kalan tutar) düzeltir
    /// </summary>
    public async Task<int> TemizleMukerrerKrediKartiBorclariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var duzeltmeSayisi = 0;

        // 1. KalanSonrakiDonemeAktarilsin=true ama Durum hâlâ KismiOdendi olan kayıtlar
        var bozukAnaKayitlar = await context.BudgetOdemeler
            .Where(o => o.KalanSonrakiDonemeAktarilsin
                        && o.Durum == OdemeDurum.KismiOdendi
                        && o.SonrakiDonemOdemeId.HasValue)
            .ToListAsync();

        foreach (var kayit in bozukAnaKayitlar)
        {
            kayit.Durum = OdemeDurum.Odendi;
            kayit.Miktar = kayit.ToplamKismiOdenen > 0 ? kayit.ToplamKismiOdenen : kayit.Miktar;
            kayit.OdenenTutar = kayit.ToplamKismiOdenen;
            kayit.UpdatedAt = DateTime.UtcNow;
            duzeltmeSayisi++;
        }

        // 2. Aynı açıklama + aynı ay + aynı hesap ile mükerrer Kredi Kartı borç kayıtları
        var krediKartiKayitlar = await context.BudgetOdemeler
            .Where(o => o.MasrafKalemi == "Kredi Kartı" && !o.IsDeleted)
            .ToListAsync();

        var gruplar = krediKartiKayitlar
            .GroupBy(o => new { o.OdemeAy, o.OdemeYil, o.OdemeYapildigiHesapId, o.Aciklama })
            .Where(g => g.Count() > 1);

        foreach (var grup in gruplar)
        {
            var silinecekler = grup.OrderBy(o => o.Id).Skip(1); // İlk kaydı tut, gerisini sil
            foreach (var kayit in silinecekler)
            {
                kayit.IsDeleted = true;
                kayit.UpdatedAt = DateTime.UtcNow;
                duzeltmeSayisi++;
            }
        }

        // 3. SonrakiDonemOdemeId ile bağlı devir kayıtlarının tutarını kontrol et
        var devirKayitlari = await context.BudgetOdemeler
            .Where(o => o.OncekiDonemOdemeId.HasValue && !o.IsDeleted && o.Durum == OdemeDurum.Bekliyor)
            .ToListAsync();

        foreach (var devir in devirKayitlari)
        {
            var anaKayit = await context.BudgetOdemeler
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == devir.OncekiDonemOdemeId!.Value);

            if (anaKayit == null) continue;

            // Devir tutarı = orijinal miktar - ödenen
            var dogruKalanTutar = RoundCurrency(anaKayit.Miktar - anaKayit.ToplamKismiOdenen);
            if (dogruKalanTutar <= 0)
            {
                // Tamamen ödenmiş, devir kaydı gereksiz
                devir.IsDeleted = true;
                devir.UpdatedAt = DateTime.UtcNow;
                duzeltmeSayisi++;
            }
            else if (devir.Miktar != dogruKalanTutar)
            {
                devir.Miktar = dogruKalanTutar;
                devir.UpdatedAt = DateTime.UtcNow;
                duzeltmeSayisi++;
            }
        }

        if (duzeltmeSayisi > 0)
            await context.SaveChangesAsync();

        return duzeltmeSayisi;
    }

    #endregion
}
