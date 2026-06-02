using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class CariService : ICariService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _firmaProvider;

    public CariService(IDbContextFactory<ApplicationDbContext> contextFactory, IAktifFirmaProvider firmaProvider)
    {
        _contextFactory = contextFactory;
        _firmaProvider = firmaProvider;
    }

    public async Task<List<Cari>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cariler = await context.Cariler
            .AsNoTracking()
            .Include(c => c.MuhasebeHesap)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Unvan)
            .ToListAsync();

        foreach (var cari in cariler)
        {
            await FillMuhasebeBilgisiAsync(context, cari);
        }

        return cariler;
    }

    public async Task<List<Cari>> GetAllWithBakiyeAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cariler = await context.Cariler
            .AsNoTracking()
            .Include(c => c.MuhasebeHesap)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Unvan)
            .ToListAsync();

        // Toplu bakiye hesaplama (N+1 sorunu çözümü)
        var cariIds = cariler.Select(c => c.Id).ToList();
        var bakiyeVerileri = await GetBulkBakiyeVerileriAsync(context, cariIds);

        foreach (var cari in cariler)
        {
            await FillMuhasebeBilgisiAsync(context, cari);
            ApplyBakiyeFromBulkData(cari, bakiyeVerileri);
        }

        return cariler;
    }

    public async Task<int> GetCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Cariler
            .Where(c => !c.IsDeleted)
            .CountAsync();
    }

    public async Task<PagedResult<Cari>> GetPagedAsync(CariFilterParams filter)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Cariler
            .AsNoTracking()
            .Include(c => c.MuhasebeHesap)
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(c =>
                (c.CariKodu != null && c.CariKodu.ToLower().Contains(searchLower)) ||
                (c.Unvan != null && c.Unvan.ToLower().Contains(searchLower)) ||
                (c.YetkiliKisi != null && c.YetkiliKisi.ToLower().Contains(searchLower)) ||
                (c.Telefon != null && c.Telefon.Contains(searchLower)));
        }

        // Tip filtresi
        if (filter.CariTipi.HasValue)
        {
            query = query.Where(c => c.CariTipi == filter.CariTipi.Value);
        }

        // Aktif/Pasif filtresi
        if (filter.Aktif.HasValue)
        {
            query = query.Where(c => c.Aktif == filter.Aktif.Value);
        }

        // Toplam kayıt sayısı (filtrelenmiş)
        var totalCount = await query.CountAsync();

        // Sayfalama uygula
        var items = await query
            .OrderBy(c => c.Unvan)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync();

        // Toplu bakiye hesaplama (N+1 sorunu çözümü)
        var cariIds = items.Select(c => c.Id).ToList();
        var bakiyeVerileri = await GetBulkBakiyeVerileriAsync(context, cariIds);

        foreach (var cari in items)
        {
            await FillMuhasebeBilgisiAsync(context, cari);
            ApplyBakiyeFromBulkData(cari, bakiyeVerileri);
        }

        // Durum filtresi (bakiye hesaplaması sonrası)
        if (!string.IsNullOrEmpty(filter.DurumFiltre))
        {
            items = filter.DurumFiltre switch
            {
                "borclu" => items.Where(c => c.Borc > c.Alacak).ToList(),
                "alacakli" => items.Where(c => c.Alacak > c.Borc).ToList(),
                "sifir" => items.Where(c => c.Borc == c.Alacak).ToList(),
                "islemsiz" => items.Where(c => c.Borc == 0 && c.Alacak == 0).ToList(),
                _ => items
            };
            // Not: Durum filtresinde toplam sayı yeniden hesaplanmaz (performans için)
        }

        return new PagedResult<Cari>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    /// <summary>
    /// Toplu bakiye verilerini tek sorguda çeker (N+1 sorunu çözümü)
    /// </summary>
    private async Task<BulkBakiyeData> GetBulkBakiyeVerileriAsync(ApplicationDbContext context, List<int> cariIds)
    {
        if (!cariIds.Any())
            return new BulkBakiyeData();

        // Fatura toplamları - tek sorgu ile tüm carilerin fatura verilerini al
        var faturaTotals = await context.Faturalar
            .AsNoTracking()
            .Where(f => cariIds.Contains(f.CariId))
            .GroupBy(f => new { f.CariId, f.FaturaYonu })
            .Select(g => new
            {
                g.Key.CariId,
                g.Key.FaturaYonu,
                Toplam = g.Sum(f => f.GenelToplam)
            })
            .ToListAsync();

        // Banka hareket toplamları - tek sorgu ile tüm carilerin hareket verilerini al
        var hareketTotals = await context.BankaKasaHareketleri
            .AsNoTracking()
            .Where(h => h.CariId.HasValue && cariIds.Contains(h.CariId.Value))
            .GroupBy(h => new { CariId = h.CariId!.Value, h.HareketTipi })
            .Select(g => new
            {
                g.Key.CariId,
                g.Key.HareketTipi,
                Toplam = g.Sum(h => h.Tutar)
            })
            .ToListAsync();

        return new BulkBakiyeData
        {
            GelenFaturalar = faturaTotals
                .Where(f => f.FaturaYonu == FaturaYonu.Gelen)
                .ToDictionary(f => f.CariId, f => f.Toplam),
            GidenFaturalar = faturaTotals
                .Where(f => f.FaturaYonu == FaturaYonu.Giden)
                .ToDictionary(f => f.CariId, f => f.Toplam),
            Odemeler = hareketTotals
                .Where(h => h.HareketTipi == HareketTipi.Cikis)
                .ToDictionary(h => h.CariId, h => h.Toplam),
            Tahsilatlar = hareketTotals
                .Where(h => h.HareketTipi == HareketTipi.Giris)
                .ToDictionary(h => h.CariId, h => h.Toplam)
        };
    }

    /// <summary>
    /// Toplu veri kullanarak cari bakiyesini hesaplar
    /// </summary>
    private static void ApplyBakiyeFromBulkData(Cari cari, BulkBakiyeData data)
    {
        var gelenFaturalar = data.GelenFaturalar.GetValueOrDefault(cari.Id, 0);
        var gidenFaturalar = data.GidenFaturalar.GetValueOrDefault(cari.Id, 0);
        var odemeler = data.Odemeler.GetValueOrDefault(cari.Id, 0);
        var tahsilatlar = data.Tahsilatlar.GetValueOrDefault(cari.Id, 0);

        if (cari.CariTipi == CariTipi.Musteri)
        {
            cari.Alacak = gidenFaturalar;
            cari.Borc = tahsilatlar;
        }
        else if (cari.CariTipi == CariTipi.Tedarikci)
        {
            cari.Borc = gelenFaturalar;
            cari.Alacak = odemeler;
        }
        else if (cari.CariTipi == CariTipi.Personel)
        {
            cari.Borc = gelenFaturalar;
            cari.Alacak = odemeler;
        }
        else // MusteriTedarikci
        {
            cari.Alacak = gidenFaturalar + odemeler;
            cari.Borc = gelenFaturalar + tahsilatlar;
        }
    }

    /// <summary>
    /// Toplu bakiye verisi için yardımcı sınıf
    /// </summary>
    private class BulkBakiyeData
    {
        public Dictionary<int, decimal> GelenFaturalar { get; set; } = new();
        public Dictionary<int, decimal> GidenFaturalar { get; set; } = new();
        public Dictionary<int, decimal> Odemeler { get; set; } = new();
        public Dictionary<int, decimal> Tahsilatlar { get; set; } = new();
    }

    private async Task CalculateBakiyeAsync(ApplicationDbContext context, Cari cari)
    {
        // Gelen faturalar (Alis) = Borcumuz
        var gelenFaturalar = await context.Faturalar
            .Where(f => f.CariId == cari.Id && f.FaturaYonu == FaturaYonu.Gelen)
            .SumAsync(f => (decimal?)f.GenelToplam) ?? 0;

        // Giden faturalar (Satis) = Alacagimiz
        var gidenFaturalar = await context.Faturalar
            .Where(f => f.CariId == cari.Id && f.FaturaYonu == FaturaYonu.Giden)
            .SumAsync(f => (decimal?)f.GenelToplam) ?? 0;

        // Banka hareketlerinden odeme/tahsilat
        var odemeler = await context.BankaKasaHareketleri
            .Where(h => h.CariId == cari.Id && h.HareketTipi == HareketTipi.Cikis)
            .SumAsync(h => (decimal?)h.Tutar) ?? 0;

        var tahsilatlar = await context.BankaKasaHareketleri
            .Where(h => h.CariId == cari.Id && h.HareketTipi == HareketTipi.Giris)
            .SumAsync(h => (decimal?)h.Tutar) ?? 0;

        if (cari.CariTipi == CariTipi.Musteri)
        {
            cari.Alacak = gidenFaturalar;
            cari.Borc = tahsilatlar;
        }
        else if (cari.CariTipi == CariTipi.Tedarikci)
        {
            cari.Borc = gelenFaturalar;
            cari.Alacak = odemeler;
        }
        else if (cari.CariTipi == CariTipi.Personel)
        {
            cari.Borc = gelenFaturalar;
            cari.Alacak = odemeler;
        }
        else // MusteriTedarikci
        {
            cari.Alacak = gidenFaturalar + odemeler;
            cari.Borc = gelenFaturalar + tahsilatlar;
        }
    }

    public async Task<Cari?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cari = await context.Cariler
            .Include(c => c.Guzergahlar)
            .Include(c => c.MuhasebeHesap)
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cari != null)
        {
            await FillMuhasebeBilgisiAsync(context, cari);
        }

        return cari;
    }

    public async Task<Cari?> GetByKodAsync(string cariKodu)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cari = await context.Cariler
            .Include(c => c.MuhasebeHesap)
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.CariKodu == cariKodu);

        if (cari != null)
        {
            await FillMuhasebeBilgisiAsync(context, cari);
        }

        return cari;
    }

    public async Task<List<Cari>> GetByTipAsync(CariTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Cariler
            .Where(c => !c.IsDeleted)
            .Where(c => c.CariTipi == tip || c.CariTipi == CariTipi.MusteriTedarikci)
            .OrderBy(c => c.Unvan)
            .ToListAsync();
    }

    public async Task<Cari> CreateAsync(Cari cari)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var girilenCariKodu = cari.CariKodu?.Trim();

        if (!cari.MuhasebeHesapId.HasValue && IsMuhasebeHesapKodu(girilenCariKodu))
        {
            var existingHesap = await FindMuhasebeHesapByKodAsync(context, girilenCariKodu);
            if (existingHesap != null)
            {
                cari.MuhasebeHesapId = existingHesap.Id;
            }
            else
            {
                var ozelHesap = await CreateMuhasebeHesapAsync(context, cari, girilenCariKodu);
                if (ozelHesap != null)
                {
                    cari.MuhasebeHesapId = ozelHesap.Id;
                }
            }
        }

        if (!cari.MuhasebeHesapId.HasValue)
        {
            var muhasebeHesap = await CreateMuhasebeHesapAsync(context, cari);
            if (muhasebeHesap != null)
            {
                cari.MuhasebeHesapId = muhasebeHesap.Id;
            }
        }

        if (!string.IsNullOrWhiteSpace(girilenCariKodu))
        {
            cari.CariKodu = girilenCariKodu;
        }

        cari.IsDeleted = false;
        cari.CreatedAt = DateTime.UtcNow;
        context.Cariler.Add(cari);
        await context.SaveChangesAsync();
        return cari;
    }

    public async Task<Cari> UpdateAsync(Cari cari)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Cariler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cari.Id && !c.IsDeleted && c.FirmaId == _firmaProvider.AktifFirmaId);
            
        if (existing == null) throw new Exception("Cari bulunamadi");

        var girilenCariKodu = cari.CariKodu?.Trim();
        var kodDegisti = !string.Equals(girilenCariKodu, existing.CariKodu, StringComparison.OrdinalIgnoreCase);

        if (!cari.MuhasebeHesapId.HasValue)
        {
            var eslesecekKod = IsMuhasebeHesapKodu(girilenCariKodu)
                ? girilenCariKodu
                : (IsMuhasebeHesapKodu(existing.CariKodu) ? existing.CariKodu : null);

            if (!string.IsNullOrWhiteSpace(eslesecekKod))
            {
                var existingHesap = await FindMuhasebeHesapByKodAsync(context, eslesecekKod);
                if (existingHesap != null)
                {
                    cari.MuhasebeHesapId = existingHesap.Id;
                }
                else if (IsMuhasebeHesapKodu(girilenCariKodu) && kodDegisti)
                {
                    var ozelHesap = await CreateMuhasebeHesapAsync(context, cari, girilenCariKodu);
                    if (ozelHesap != null)
                    {
                        cari.MuhasebeHesapId = ozelHesap.Id;
                    }
                }
            }
        }

        if (!cari.MuhasebeHesapId.HasValue && !existing.MuhasebeHesapId.HasValue)
        {
            var muhasebeHesap = await CreateMuhasebeHesapAsync(context, cari);
            if (muhasebeHesap != null)
            {
                cari.MuhasebeHesapId = muhasebeHesap.Id;
            }
        }
        else if ((existing.MuhasebeHesapId ?? cari.MuhasebeHesapId).HasValue && existing.Unvan != cari.Unvan)
        {
            var hesapId = cari.MuhasebeHesapId ?? existing.MuhasebeHesapId;
            var mHesap = await context.MuhasebeHesaplari.FindAsync(hesapId!.Value);
            if (mHesap != null)
            {
                mHesap.HesapAdi = cari.Unvan;
                mHesap.UpdatedAt = DateTime.UtcNow;
                context.MuhasebeHesaplari.Update(mHesap);
            }

            // Avans hesabının adını da güncelle
            var avansHesapId = cari.PersonelAvansHesapId ?? existing.PersonelAvansHesapId;
            if (avansHesapId.HasValue)
            {
                var avansHesap = await context.MuhasebeHesaplari.FindAsync(avansHesapId.Value);
                if (avansHesap != null)
                {
                    avansHesap.HesapAdi = cari.Unvan;
                    avansHesap.UpdatedAt = DateTime.UtcNow;
                    context.MuhasebeHesaplari.Update(avansHesap);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(girilenCariKodu))
        {
            cari.CariKodu = girilenCariKodu;
        }

        existing.CariKodu = cari.CariKodu ?? string.Empty;
        existing.Unvan = cari.Unvan;
        existing.CariTipi = cari.CariTipi;
        existing.VergiDairesi = cari.VergiDairesi;
        existing.VergiNo = cari.VergiNo;
        existing.TcKimlikNo = cari.TcKimlikNo;
        existing.Adres = cari.Adres;
        existing.Telefon = cari.Telefon;
        existing.Email = cari.Email;
        existing.YetkiliKisi = cari.YetkiliKisi;
        existing.Notlar = cari.Notlar;
        existing.Aktif = cari.Aktif;
        existing.MuhasebeHesapId = cari.MuhasebeHesapId;
        existing.PersonelAvansHesapId = cari.PersonelAvansHesapId ?? existing.PersonelAvansHesapId;
        existing.FirmaId = cari.FirmaId;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task<Cari> MatchMuhasebeHesapByKodAsync(int cariId, string hesapKodu)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cari = await context.Cariler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cariId && !c.IsDeleted && c.FirmaId == _firmaProvider.AktifFirmaId);

        if (cari == null)
        {
            throw new Exception("Cari bulunamadi");
        }

        if (string.IsNullOrWhiteSpace(hesapKodu))
        {
            throw new Exception("Hesap kodu bos olamaz");
        }

        var muhasebeHesap = await FindMuhasebeHesapByKodAsync(context, hesapKodu);
        if (muhasebeHesap == null)
        {
            throw new Exception("Girilen hesap kodu hesap planinda bulunamadi");
        }

        cari.MuhasebeHesapId = muhasebeHesap.Id;
        cari.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return cari;
    }

    public async Task<Cari> EnsureMuhasebeHesapAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cari = await context.Cariler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cariId && !c.IsDeleted && c.FirmaId == _firmaProvider.AktifFirmaId);

        if (cari == null)
        {
            throw new Exception("Cari bulunamadi");
        }

        if (cari.MuhasebeHesapId.HasValue)
        {
            var bagliHesap = await context.MuhasebeHesaplari.FindAsync(cari.MuhasebeHesapId.Value);
            if (bagliHesap != null)
            {
                return cari;
            }
        }

        if (IsMuhasebeHesapKodu(cari.CariKodu))
        {
            var existingHesap = await FindMuhasebeHesapByKodAsync(context, cari.CariKodu);
            if (existingHesap != null)
            {
                cari.MuhasebeHesapId = existingHesap.Id;
                cari.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return cari;
            }
        }

        var olusanHesap = await CreateMuhasebeHesapAsync(context, cari);
        if (olusanHesap == null)
        {
            throw new Exception("Muhasebe hesap kodu olusturulamadi");
        }

        cari.MuhasebeHesapId = olusanHesap.Id;
        cari.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return cari;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // IgnoreQueryFilters ile bul, FirmaId kontrolü ile cross-tenant koruması
        var cari = await context.Cariler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && c.FirmaId == _firmaProvider.AktifFirmaId);

        if (cari != null && !cari.IsDeleted)
        {
            cari.IsDeleted = true;
            cari.Aktif = false;
            cari.UpdatedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<string> GenerateNextKodAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // EF tarafında Substring/Convert kombinasyonu PostgreSQL'e güvenli çevrilemediği için
        // kodları belleğe alıp sayısal kısmı burada parse ediyoruz.
        var cariKodlari = await context.Cariler
            .IgnoreQueryFilters()
            .Where(c => c.CariKodu != null && c.CariKodu.StartsWith("CRI-"))
            .Select(c => c.CariKodu!)
            .ToListAsync();

        var maxNumber = 0;
        foreach (var cariKodu in cariKodlari)
        {
            if (string.IsNullOrWhiteSpace(cariKodu) || cariKodu.Length <= 4)
            {
                continue;
            }

            var numericPart = cariKodu[4..];
            if (numericPart.Length == 5 && int.TryParse(numericPart, out var parsed) && parsed > maxNumber)
            {
                maxNumber = parsed;
            }
        }

        // Timestamp ile ek benzersizlik garantisi
        var timestamp = DateTime.UtcNow.Ticks % 1000;
        var nextNumber = maxNumber + 1 + (int)timestamp;

        // Eğer bu kod mevcutsa, daha yüksek bir sayı bul
        while (await context.Cariler.IgnoreQueryFilters().AnyAsync(c => c.CariKodu == $"CRI-{nextNumber:D5}"))
        {
            nextNumber++;
        }

        return $"CRI-{nextNumber:D5}";
    }

    /// <summary>
    /// Cari icin muhasebe hesabi olusturur
    /// Musteri: 120.01.xxx (Alicilar)
    /// Tedarikci: 320.01.xxx (Saticilar)
    /// Personel: 335.XX.PRSXXXXX (Personel Borclari - XX = FirmaId)
    /// </summary>
    private async Task<MuhasebeHesap?> CreateMuhasebeHesapAsync(ApplicationDbContext context, Cari cari, string? ozelHesapKodu = null)
    {
        try
        {
            // Ayarları al (Eğer DB'de yoksa default ayarları kullan)
            var ayar = await context.MuhasebeAyarlari.FirstOrDefaultAsync() ?? new MuhasebeAyar();

            if (!ayar.OtomatikHesapDuzenlensin)
            {
                return null; // Otomatik hesap açılması kapalıysa işlem yapma
            }

            string anaHesapKodu;
            string anaHesapAdi;
            HesapGrubu hesapGrubu;

            if (cari.CariTipi == CariTipi.Personel)
            {
                // Personel icin ayardaki prefix'i kullan
                // Ornegin ayar "335.01" ise onu kullan, "335" ise "335.FirmaId" de yapilabilir,
                // Ama prefix genelde mutlaktir. Formatini ayardan al.
                anaHesapKodu = ayar.PersonelPrefix;
                anaHesapAdi = "Personel Borclari";
                hesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar;
            }
            else if (cari.CariTipi == CariTipi.Musteri)
            {
                anaHesapKodu = ayar.MusteriPrefix;
                anaHesapAdi = "Alicilar";
                hesapGrubu = HesapGrubu.DonenVarliklar;
            }
            else if (cari.CariTipi == CariTipi.Tedarikci)
            {
                anaHesapKodu = ayar.TedarikciPrefix;
                anaHesapAdi = "Saticilar";
                hesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar;
            }
            else // MusteriTedarikci
            {
                anaHesapKodu = ayar.MusteriPrefix;
                anaHesapAdi = "Alicilar";
                hesapGrubu = HesapGrubu.DonenVarliklar;
            }

            // 335 ana hesabini kontrol et (personel icin) - opsiyonel, sadece 335 ise ana kontrol gerekir
            var prefixBasKisim = anaHesapKodu.Split('.')[0];
            if (cari.CariTipi == CariTipi.Personel)
            {
                var anaPersonelHesap = await context.MuhasebeHesaplari
                    .FirstOrDefaultAsync(h => h.HesapKodu == prefixBasKisim);
                
                if (anaPersonelHesap == null)
                {
                    anaPersonelHesap = new MuhasebeHesap
                    {
                        HesapKodu = prefixBasKisim,
                        HesapAdi = "Personel Borclari Ana Hesap",
                        HesapTuru = HesapTuru.Pasif,
                        HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar,
                        AltHesapVar = true,
                        Aktif = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.MuhasebeHesaplari.Add(anaPersonelHesap);
                    await context.SaveChangesAsync();
                }
            }

            // Ana hesabi bul veya olustur
            var anaHesap = await context.MuhasebeHesaplari
                .FirstOrDefaultAsync(h => h.HesapKodu == anaHesapKodu);

            if (anaHesap == null)
            {
                // Ust hesabi bul
                var ustHesapKodu = anaHesapKodu.Split('.')[0];
                var ustHesap = await context.MuhasebeHesaplari
                    .FirstOrDefaultAsync(h => h.HesapKodu == ustHesapKodu);

                anaHesap = new MuhasebeHesap
                {
                    HesapKodu = anaHesapKodu,
                    HesapAdi = anaHesapAdi,
                    HesapTuru = cari.CariTipi == CariTipi.Personel ? HesapTuru.Pasif : HesapTuru.Aktif,
                    HesapGrubu = hesapGrubu,
                    UstHesapId = ustHesap?.Id,
                    AltHesapVar = true,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeHesaplari.Add(anaHesap);
                await context.SaveChangesAsync();
            }

            // Ayni unvana ve ana hesaba sahip mevcut bir kayit var mi kontrol et
            var ayniUnvanliHesap = await context.MuhasebeHesaplari
                .FirstOrDefaultAsync(h => h.HesapKodu.StartsWith(anaHesapKodu + ".") && h.HesapAdi == cari.Unvan);

            if (ayniUnvanliHesap != null && string.IsNullOrEmpty(ozelHesapKodu))
            {
                // Unvan tutuyorsa ve ozel kod zorlamiyorsa onu dondur
                return ayniUnvanliHesap;
            }

            // Alt hesap numarasini bul
            string yeniHesapKodu;
            
            if (!string.IsNullOrWhiteSpace(ozelHesapKodu))
            {
                // Kullanici spesifik bir kod istedi e.g "120.01.077", direkt onu kullan (zaten yukarida varligi test edildi)
                yeniHesapKodu = ozelHesapKodu;
            }
            else
            {
                // Tum cariler icin sayisal format (Prefix.001) otomatik uretim
                var sonAltHesap = await context.MuhasebeHesaplari
                    .Where(h => h.HesapKodu.StartsWith(anaHesapKodu + "."))
                    .OrderByDescending(h => h.HesapKodu)
                    .FirstOrDefaultAsync();

                int nextNum = 1;
                if (sonAltHesap != null)
                {
                    // Son '.' dan sonrasini al
                    var parts = sonAltHesap.HesapKodu.Split('.');
                    var sonKisim = parts[parts.Length - 1]; // "001", "PRS00001" vs.

                    // Eger icinde PRS vb. harf varsa temizle, sadece sayilari al
                    var sadeceSayi = new string(sonKisim.Where(char.IsDigit).ToArray());

                    if (int.TryParse(sadeceSayi, out var lastNum))
                    {
                        nextNum = lastNum + 1;
                    }
                }

                // Hesabi olustururken Kac haneli olacak? Personelse ornegin 5 haneli yapabiliriz, standartsa 3
                if (cari.CariTipi == CariTipi.Personel)
                {
                    yeniHesapKodu = $"{anaHesapKodu}.{nextNum:D3}"; // Artik PRS ismini kaldirip sadece sayisal yapiyoruz (3 haneli)
                }
                else
                {
                    yeniHesapKodu = $"{anaHesapKodu}.{nextNum:D3}";
                }
            }

            // Cari icin alt hesap olustur
            var cariHesap = new MuhasebeHesap
            {
                HesapKodu = yeniHesapKodu,
                HesapAdi = cari.Unvan,
                HesapTuru = cari.CariTipi == CariTipi.Personel ? HesapTuru.Pasif : HesapTuru.Aktif,
                HesapGrubu = hesapGrubu,
                UstHesapId = anaHesap.Id,
                AltHesapVar = false,
                Aktif = true,
                CreatedAt = DateTime.UtcNow
            };
            context.MuhasebeHesaplari.Add(cariHesap);
            await context.SaveChangesAsync();

            // Personel ise 195.01.XXX (İş Avansları) hesabını da otomatik aç
            if (cari.CariTipi == CariTipi.Personel && !cari.PersonelAvansHesapId.HasValue)
            {
                var avansPrefix = ayar.PersonelAvansPrefix;
                if (string.IsNullOrWhiteSpace(avansPrefix)) avansPrefix = "195.01";

                // 195 ana hesabını kontrol et
                var avansAnaKodBasKisim = avansPrefix.Split('.')[0];
                var avansAnaHesap195 = await context.MuhasebeHesaplari
                    .FirstOrDefaultAsync(h => h.HesapKodu == avansAnaKodBasKisim);
                if (avansAnaHesap195 == null)
                {
                    avansAnaHesap195 = new MuhasebeHesap
                    {
                        HesapKodu = avansAnaKodBasKisim,
                        HesapAdi = "IS AVANSLARI",
                        HesapTuru = HesapTuru.Aktif,
                        HesapGrubu = HesapGrubu.DonenVarliklar,
                        AltHesapVar = true,
                        SistemHesabi = true,
                        Aktif = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.MuhasebeHesaplari.Add(avansAnaHesap195);
                    await context.SaveChangesAsync();
                }

                // 195.01 alt hesabını kontrol et
                var avansUstHesap = await context.MuhasebeHesaplari
                    .FirstOrDefaultAsync(h => h.HesapKodu == avansPrefix);
                if (avansUstHesap == null)
                {
                    avansUstHesap = new MuhasebeHesap
                    {
                        HesapKodu = avansPrefix,
                        HesapAdi = "Personel Avanslari",
                        HesapTuru = HesapTuru.Aktif,
                        HesapGrubu = HesapGrubu.DonenVarliklar,
                        UstHesapId = avansAnaHesap195.Id,
                        AltHesapVar = true,
                        SistemHesabi = true,
                        Aktif = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.MuhasebeHesaplari.Add(avansUstHesap);
                    await context.SaveChangesAsync();
                }

                // Aynı ünvanlı avans hesabı var mı?
                var mevcutAvansHesap = await context.MuhasebeHesaplari
                    .FirstOrDefaultAsync(h => h.HesapKodu.StartsWith(avansPrefix + ".") && h.HesapAdi == cari.Unvan);

                if (mevcutAvansHesap == null)
                {
                    // Sıradaki numarayı bul
                    var sonAvansAltHesap = await context.MuhasebeHesaplari
                        .Where(h => h.HesapKodu.StartsWith(avansPrefix + "."))
                        .OrderByDescending(h => h.HesapKodu)
                        .FirstOrDefaultAsync();
                    int avansNextNum = 1;
                    if (sonAvansAltHesap != null)
                    {
                        var parts = sonAvansAltHesap.HesapKodu.Split('.');
                        var sadeceSayi = new string(parts[parts.Length - 1].Where(char.IsDigit).ToArray());
                        if (int.TryParse(sadeceSayi, out var lastNum)) avansNextNum = lastNum + 1;
                    }
                    var avansHesapKodu = $"{avansPrefix}.{avansNextNum:D3}";

                    mevcutAvansHesap = new MuhasebeHesap
                    {
                        HesapKodu = avansHesapKodu,
                        HesapAdi = cari.Unvan,
                        HesapTuru = HesapTuru.Aktif,
                        HesapGrubu = HesapGrubu.DonenVarliklar,
                        UstHesapId = avansUstHesap.Id,
                        AltHesapVar = false,
                        Aktif = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.MuhasebeHesaplari.Add(mevcutAvansHesap);
                    await context.SaveChangesAsync();
                }

                cari.PersonelAvansHesapId = mevcutAvansHesap.Id;
            }

            return cariHesap;
        }
        catch
        {
            return null;
        }
    }

    private async Task<MuhasebeHesap?> FindMuhasebeHesapByKodAsync(ApplicationDbContext context, string? hesapKodu)
    {
        if (string.IsNullOrWhiteSpace(hesapKodu))
        {
            return null;
        }

        var normalizedKod = hesapKodu.Trim();
        return await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu == normalizedKod);
    }

    private async Task FillMuhasebeBilgisiAsync(ApplicationDbContext context, Cari cari)
    {
        var muhasebeHesap = cari.MuhasebeHesap;

        if (muhasebeHesap == null && cari.MuhasebeHesapId.HasValue)
        {
            muhasebeHesap = await context.MuhasebeHesaplari.FindAsync(cari.MuhasebeHesapId.Value);
        }

        if (muhasebeHesap == null && IsMuhasebeHesapKodu(cari.CariKodu))
        {
            muhasebeHesap = await FindMuhasebeHesapByKodAsync(context, cari.CariKodu);
        }

        if (muhasebeHesap == null && !string.IsNullOrWhiteSpace(cari.Unvan))
        {
            var unvan = cari.Unvan.Trim();

            muhasebeHesap = await context.MuhasebeHesaplari
                .FirstOrDefaultAsync(h => h.HesapAdi == unvan)
                ?? await context.MuhasebeHesaplari
                    .FirstOrDefaultAsync(h => !string.IsNullOrWhiteSpace(h.HesapAdi) &&
                                              (h.HesapAdi.Contains(unvan) || unvan.Contains(h.HesapAdi)));
        }

        if (muhasebeHesap != null)
        {
            cari.MuhasebeHesapId = muhasebeHesap.Id;
            cari.MuhasebeHesap = muhasebeHesap;
        }
    }

    private static bool IsMuhasebeHesapKodu(string? kod)
    {
        if (string.IsNullOrWhiteSpace(kod))
        {
            return false;
        }

        return kod.Contains('.') &&
               (kod.StartsWith("120.", StringComparison.OrdinalIgnoreCase) ||
                kod.StartsWith("320.", StringComparison.OrdinalIgnoreCase) ||
                kod.StartsWith("335.", StringComparison.OrdinalIgnoreCase));
    }

    // ===== İletişim Geçmişi =====

    public async Task<List<CariIletisimNot>> GetIletisimNotlariAsync(int cariId, int? adet = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.CariIletisimNotlar
            .Where(n => n.CariId == cariId && !n.IsDeleted)
            .OrderByDescending(n => n.IletisimTarihi)
            .AsQueryable();

        if (adet.HasValue)
            query = query.Take(adet.Value);

        return await query.ToListAsync();
    }

    public async Task<CariIletisimNot> AddIletisimNotuAsync(CariIletisimNot not)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.CariIletisimNotlar.Add(not);
        await context.SaveChangesAsync();
        return not;
    }

    public async Task<CariIletisimNot> UpdateIletisimNotuAsync(CariIletisimNot not)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.CariIletisimNotlar.FindAsync(not.Id);
        if (mevcut == null) throw new Exception("İletişim notu bulunamadı.");

        mevcut.Konu = not.Konu;
        mevcut.Notlar = not.Notlar;
        mevcut.IletisimTipi = not.IletisimTipi;
        mevcut.IletisimTarihi = not.IletisimTarihi;
        mevcut.SonrakiAksiyon = not.SonrakiAksiyon;
        mevcut.SonrakiAksiyonTarihi = not.SonrakiAksiyonTarihi;
        mevcut.AksiyonTamamlandi = not.AksiyonTamamlandi;
        mevcut.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return mevcut;
    }

    public async Task<bool> DeleteIletisimNotuAsync(int notId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var not = await context.CariIletisimNotlar.FindAsync(notId);
        if (not == null) return false;

        not.IsDeleted = true;
        not.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    // ===== Hatırlatıcılar =====

    public async Task<List<Hatirlatici>> GetCariHatirlaticilariAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Hatirlaticilar
            .Where(h => h.CariId == cariId && !h.IsDeleted)
            .OrderByDescending(h => h.BaslangicTarihi)
            .ToListAsync();
    }

    public async Task<Hatirlatici> AddCariHatirlaticiAsync(Hatirlatici hatirlatici)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Hatirlaticilar.Add(hatirlatici);
        await context.SaveChangesAsync();
        return hatirlatici;
    }

    // ===== Vade Uyarıları =====

    public async Task<List<CariVadeUyari>> GetVadeUyarilariAsync(int? cariId = null, int yaklasmaSuresiGun = 7)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var uyarilar = new List<CariVadeUyari>();

        var query = context.Faturalar
            .Include(f => f.Cari)
            .Where(f => !f.IsDeleted && f.VadeTarihi.HasValue && f.Durum != FaturaDurum.Odendi && f.Durum != FaturaDurum.IptalEdildi);

        if (cariId.HasValue)
            query = query.Where(f => f.CariId == cariId.Value);

        var faturalar = await query.ToListAsync();

        foreach (var f in faturalar)
        {
            var kalanTutar = f.GenelToplam - f.OdenenTutar;
            if (kalanTutar <= 0) continue;

            var vade = f.VadeTarihi!.Value;
            var kalanGun = (vade - bugun).Days;

            // Kritik: 30+ gün gecikmiş, Gecikmiş: vadesi geçmiş, Bugün: bugün vadeli, Yaklaşan: yaklasmaSuresiGun içinde
            VadeUyariSeviye seviye;
            if (kalanGun < -30)
                seviye = VadeUyariSeviye.VadesiGecmisKritik;
            else if (kalanGun < 0)
                seviye = VadeUyariSeviye.VadesiGecmis;
            else if (kalanGun == 0)
                seviye = VadeUyariSeviye.BugunVadeli;
            else if (kalanGun <= yaklasmaSuresiGun)
                seviye = VadeUyariSeviye.YaklasanVade;
            else
                continue;

            uyarilar.Add(new CariVadeUyari
            {
                CariId = f.CariId,
                CariUnvan = f.Cari?.Unvan ?? "",
                CariKodu = f.Cari?.CariKodu ?? "",
                FaturaId = f.Id,
                FaturaNo = f.FaturaNo,
                FaturaTarihi = f.FaturaTarihi,
                VadeTarihi = vade,
                KalanGun = kalanGun,
                KalanTutar = kalanTutar,
                Seviye = seviye
            });
        }

        return uyarilar.OrderBy(u => u.KalanGun).ToList();
    }

    // ---- Sefer Ücretleri ----
    public async Task<List<CariSeferUcreti>> GetSeferUcretleriAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CariSeferUcretleri
            .AsNoTracking()
            .Include(x => x.Guzergah)
            .Where(x => x.CariId == cariId && !x.IsDeleted)
            .OrderByDescending(x => x.Aktif)
            .ThenByDescending(x => x.GecerlilikBaslangic)
            .ToListAsync();
    }

    public async Task<CariSeferUcreti> AddSeferUcretiAsync(CariSeferUcreti ucret)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.CariSeferUcretleri.Add(ucret);
        await context.SaveChangesAsync();
        return ucret;
    }

    public async Task<CariSeferUcreti> UpdateSeferUcretiAsync(CariSeferUcreti ucret)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.CariSeferUcretleri.Update(ucret);
        await context.SaveChangesAsync();
        return ucret;
    }

    public async Task<bool> DeleteSeferUcretiAsync(int ucretId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.CariSeferUcretleri.FirstOrDefaultAsync(x => x.Id == ucretId);
        if (entity == null) return false;
        entity.IsDeleted = true;
        await context.SaveChangesAsync();
        return true;
    }
}





