using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Web.Helpers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class AracService : IAracService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;
    private readonly IEvrakArsivService _evrakArsivService;
    private readonly ICacheService _cache;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public AracService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISecureFileService secureFileService,
        IEvrakArsivService evrakArsivService,
        ICacheService cache,
        IAktifFirmaProvider aktifFirmaProvider)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
        _evrakArsivService = evrakArsivService;
        _cache = cache;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    /// <summary>
    /// Cache key'lerini aktif firmaya göre scope'lar. Firma seçili değil veya "Tüm Firmalar"
    /// modu açıksa ortak bir key kullanır. Bu, EF global query filter ile birlikte çalışarak
    /// firmalar arası veri sızmasını önler.
    /// </summary>
    private string ScopeKey(string baseKey)
    {
        if (_aktifFirmaProvider.TumFirmalar) return baseKey + ":Tum";
        var fid = _aktifFirmaProvider.AktifFirmaId ?? 0;
        return baseKey + ":F" + fid.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    #region Araç CRUD İşlemleri

    public Task<List<Arac>> GetAllAsync() =>
        _cache.GetOrSetAsync(ScopeKey(CacheKeys.AracListesi), async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var araclar = await context.Araclar
                .AsNoTracking()
                .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
                .Include(a => a.Firma)
                .Where(a => !a.IsDeleted)
                .ToListAsync();

            foreach (var arac in araclar)
            {
                var aktifPlaka = arac.PlakaGecmisi
                    .Where(p => p.CikisTarihi == null || p.CikisTarihi > DateTime.Today)
                    .OrderByDescending(p => p.GirisTarihi)
                    .FirstOrDefault();

                if (aktifPlaka != null && arac.AktifPlaka != aktifPlaka.Plaka)
                {
                    arac.AktifPlaka = aktifPlaka.Plaka;
                    arac.Plaka = aktifPlaka.Plaka;
                }
            }

            return araclar.OrderBy(a => a.AktifPlaka ?? a.SaseNo).ToList();
        }, CacheDurations.Medium);

    public Task<List<Arac>> GetActiveAsync() =>
        _cache.GetOrSetAsync(ScopeKey(CacheKeys.AracAktif), async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var araclar = await context.Araclar
                .AsNoTracking()
                .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
                .Include(a => a.Firma)
                .Where(a => a.Aktif && !a.IsDeleted)
                .ToListAsync();
            
            // Aktif plakaları güncelle
            foreach (var arac in araclar)
            {
                var aktifPlaka = arac.PlakaGecmisi
                    .Where(p => p.CikisTarihi == null || p.CikisTarihi > DateTime.Today)
                    .OrderByDescending(p => p.GirisTarihi)
                    .FirstOrDefault();
            
                if (aktifPlaka != null && arac.AktifPlaka != aktifPlaka.Plaka)
                {
                    arac.AktifPlaka = aktifPlaka.Plaka;
                    arac.Plaka = aktifPlaka.Plaka;
                }
            }
            
            return araclar.OrderBy(a => a.AktifPlaka ?? a.SaseNo).ToList();
        }, CacheDurations.Medium);
    public async Task<int> GetActiveCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .Where(a => a.Aktif && !a.IsDeleted)
            .CountAsync();
    }

    public async Task<Arac?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.Araclar
            .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted).OrderByDescending(p => p.GirisTarihi))
            .Include(a => a.KiralikCari)
            .Include(a => a.KomisyoncuCari)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            
        if (arac != null)
        {
            // Aktif plakayı güncelle
            var aktifPlaka = arac.PlakaGecmisi
                .Where(p => p.CikisTarihi == null || p.CikisTarihi > DateTime.Today)
                .OrderByDescending(p => p.GirisTarihi)
                .FirstOrDefault();
            
            if (aktifPlaka != null)
            {
                arac.AktifPlaka = aktifPlaka.Plaka;
                arac.Plaka = aktifPlaka.Plaka;
            }
        }
        
        return arac;
    }

    public async Task<Arac?> GetByPlakaAsync(string plaka)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Aktif plakaya göre bul (CikisTarihi null veya gelecek tarihli)
        var aracPlaka = await context.AracPlakalar
            .Include(ap => ap.Arac)
            .FirstOrDefaultAsync(ap => ap.Plaka == plaka && 
                                       !ap.IsDeleted &&
                                       (ap.CikisTarihi == null || ap.CikisTarihi > DateTime.Today));
            
        return aracPlaka?.Arac;
    }
    
    public async Task<Arac?> GetBySaseNoAsync(string saseNo)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(a => a.SaseNo == saseNo && !a.IsDeleted);
    }
    
    public async Task<bool> SaseNoMevcutMu(string saseNo, int? haricAracId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .AnyAsync(a => a.SaseNo == saseNo && 
                          !a.IsDeleted &&
                          (!haricAracId.HasValue || a.Id != haricAracId.Value));
    }
    
    public async Task<bool> PlakaMevcutMu(string plaka, int? haricAracPlakaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.UtcNow.Date;

        // Aktif plaka kontrolü (CikisTarihi null veya gelecek tarihli)
        return await context.AracPlakalar
            .AnyAsync(ap => ap.Plaka == plaka &&
                           !ap.IsDeleted &&
                           ap.AracId > 0 &&
                           (ap.CikisTarihi == null || ap.CikisTarihi > bugun) &&
                           (!haricAracPlakaId.HasValue || ap.Id != haricAracPlakaId.Value) &&
                           context.Araclar.Any(a => a.Id == ap.AracId && !a.IsDeleted));
    }

    public async Task<Arac> CreateAsync(Arac arac, string plaka, PlakaIslemTipi islemTipi = PlakaIslemTipi.Alis, 
        decimal? islemTutari = null, int? cariId = null, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Şase no kontrolü
        if (await SaseNoMevcutMu(arac.SaseNo))
            throw new InvalidOperationException($"Bu şase numarası ({arac.SaseNo}) sistemde zaten kayıtlı.");
            
        // Plaka kontrolü
        if (await PlakaMevcutMu(plaka))
            throw new InvalidOperationException($"Bu plaka ({plaka}) başka bir araçta aktif olarak kullanılıyor.");

        // FirmaId=0 FK hatasina yol acmasin diye null'a cevir
        if (arac.FirmaId <= 0) arac.FirmaId = null;

        try
        {
            // ExecutionStrategy ile transaction sarmalama (NpgsqlRetryingExecutionStrategy uyumluluğu)
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                // Navigation property'leri temizle (tracking sorununu önle)
                arac.PlakaGecmisi = new List<AracPlaka>();
            arac.Masraflar = new List<AracMasraf>();
            arac.ServisCalismalari = new List<ServisCalisma>();
            arac.KiralikCari = null;
            arac.KomisyoncuCari = null;
            arac.KiralikCariId = arac.KiralikCariId <= 0 ? null : arac.KiralikCariId;
            arac.KomisyoncuCariId = arac.KomisyoncuCariId <= 0 ? null : arac.KomisyoncuCariId;
            arac.SaseNo = arac.SaseNo.Trim().ToUpperInvariant();
            plaka = plaka.Trim().ToUpperInvariant();
            arac.TrafikSigortaBitisTarihi = arac.TrafikSigortaBitisTarihi?.Date;
            arac.KaskoBitisTarihi = arac.KaskoBitisTarihi?.Date;
            arac.MuayeneBitisTarihi = arac.MuayeneBitisTarihi?.Date;
            arac.SatisaAcilmaTarihi = arac.SatisaAcilmaTarihi?.Date;
            
            // Araç oluştur
            arac.AktifPlaka = plaka;
            arac.Plaka = plaka;
            arac.CreatedAt = DateTime.UtcNow;
            context.Araclar.Add(arac);

            // İlk plaka kaydını oluştur
            var aracPlaka = new AracPlaka
            {
                Plaka = plaka,
                GirisTarihi = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc),
                IslemTipi = islemTipi,
                IslemTutari = islemTutari,
                CariId = cariId,
                Aciklama = aciklama ?? $"Araç ilk kayıt - {islemTipi}",
                CreatedAt = DateTime.UtcNow
            };

            arac.PlakaGecmisi.Add(aracPlaka);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _cache.RemoveByPrefixAsync(CacheKeys.AracPrefix);

            return arac;
            }); // ExecutionStrategy lambda sonu
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException($"Araç kayıt hatası: {innerMessage}", ex);
        }
    }

    public async Task<Arac> UpdateAsync(Arac arac)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Şase no kontrolü (kendi hariç)
        if (await SaseNoMevcutMu(arac.SaseNo, arac.Id))
            throw new InvalidOperationException($"Bu şase numarası ({arac.SaseNo}) sistemde zaten kayıtlı.");
        
        try
        {
            // Mevcut kaydı veritabanından al
            var existing = await context.Araclar.FirstOrDefaultAsync(a => a.Id == arac.Id && !a.IsDeleted);
            if (existing == null)
                throw new InvalidOperationException("Araç bulunamadı.");

            existing.KiralikCariId = arac.KiralikCariId <= 0 ? null : arac.KiralikCariId;
            existing.KomisyoncuCariId = arac.KomisyoncuCariId <= 0 ? null : arac.KomisyoncuCariId;
            existing.TrafikSigortaBitisTarihi = arac.TrafikSigortaBitisTarihi?.Date;
            existing.KaskoBitisTarihi = arac.KaskoBitisTarihi?.Date;
            existing.MuayeneBitisTarihi = arac.MuayeneBitisTarihi?.Date;
            existing.KoltukSigortasiBaslangiçTarihi = arac.KoltukSigortasiBaslangiçTarihi?.Date;
            existing.KoltukSigortasiBitisTarihi = arac.KoltukSigortasiBitisTarihi?.Date;
            existing.SatisaAcilmaTarihi = arac.SatisaAcilmaTarihi?.Date;
            
            // Sadece değiştirilebilir alanları güncelle
            existing.SaseNo = arac.SaseNo.Trim().ToUpperInvariant();
            existing.Marka = arac.Marka;
            existing.Model = arac.Model;
            existing.ModelYili = arac.ModelYili;
            existing.MotorNo = arac.MotorNo;
            existing.Renk = arac.Renk;
            existing.KoltukSayisi = arac.KoltukSayisi;
            existing.AracTipi = arac.AracTipi;
            existing.AracSinifi = arac.AracSinifi;
            existing.SahiplikTipi = arac.SahiplikTipi;
            existing.GunlukKiraBedeli = arac.GunlukKiraBedeli;
            existing.AylikKiraBedeli = arac.AylikKiraBedeli;
            existing.SeferBasinaKiraBedeli = arac.SeferBasinaKiraBedeli;
            existing.KiraHesaplamaTipi = arac.KiraHesaplamaTipi;
            existing.KomisyonVar = arac.KomisyonVar;
            existing.KomisyonOrani = arac.KomisyonOrani;
            existing.SabitKomisyonTutari = arac.SabitKomisyonTutari;
            existing.KomisyonHesaplamaTipi = arac.KomisyonHesaplamaTipi;
            existing.KmDurumu = arac.KmDurumu;
            existing.Durumu = arac.Durumu;
            existing.Aktif = arac.Aktif;
            existing.Notlar = arac.Notlar;
            existing.SatisaAcik = arac.SatisaAcik;
            existing.SatisFiyati = arac.SatisFiyati;
            existing.SatisAciklamasi = arac.SatisAciklamasi;
            // Tenant (Firma) güncellemesi: sadece açıkça seçilmiş ve mevcut DB'de var ise değiştir
            if (arac.FirmaId.HasValue && arac.FirmaId.Value > 0)
            {
                var firmaVar = await context.Firmalar.AnyAsync(f => f.Id == arac.FirmaId.Value);
                existing.FirmaId = firmaVar ? arac.FirmaId : existing.FirmaId;
            }
            existing.UpdatedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync();
            
            // Aktif plakayı güncelle
            await GuncelleAktifPlaka(context, existing.Id);
            
            return existing;
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException($"Araç güncelleme hatası: {innerMessage}", ex);
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.Araclar
            .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (arac != null)
        {
            foreach (var aktifPlaka in arac.PlakaGecmisi.Where(p => p.CikisTarihi == null || p.CikisTarihi > DateTime.Today))
            {
                aktifPlaka.CikisTarihi = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
                aktifPlaka.UpdatedAt = DateTime.UtcNow;
            }

            arac.AktifPlaka = null;
            arac.IsDeleted = true;
            arac.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// FirmaId değeri null olan araçları verilen firmaya atar.
    /// Eski (multi-tenant öncesi) kayıtların puantaj/fatura akışlarında hata vermemesi için kullanılır.
    /// Global query filter'ı by-pass ederek tüm firmasız araçları yakalar.
    /// </summary>
    public async Task<int> BackfillFirmaIdAsync(int firmaId)
    {
        if (firmaId <= 0)
            throw new ArgumentException("Geçerli bir firma seçilmedi.", nameof(firmaId));

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Verilen firmayı doğrula
        var firmaVar = await context.Firmalar.IgnoreQueryFilters().AnyAsync(f => f.Id == firmaId && !f.IsDeleted);
        if (!firmaVar)
            throw new InvalidOperationException($"Id={firmaId} olan firma bulunamadı.");

        var firmasizlar = await context.Araclar
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted && a.FirmaId == null)
            .ToListAsync();

        if (firmasizlar.Count == 0) return 0;

        foreach (var a in firmasizlar)
        {
            a.FirmaId = firmaId;
            a.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync(CacheKeys.AracPrefix);
        return firmasizlar.Count;
    }

    /// <summary>FirmaId değeri null olan araç sayısı.</summary>
    public async Task<int> GetFirmaIdYokSayisiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .IgnoreQueryFilters()
            .CountAsync(a => !a.IsDeleted && a.FirmaId == null);
    }
    
    #endregion
    
    #region Plaka İşlemleri
    
    public async Task<List<AracPlaka>> GetPlakaGecmisiAsync(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracPlakalar
            .Include(ap => ap.Cari)
            .Where(ap => ap.AracId == aracId && !ap.IsDeleted)
            .OrderByDescending(ap => ap.GirisTarihi)
            .ToListAsync();
    }
    
    public async Task<AracPlaka> PlakaEkle(int aracId, string yeniPlaka, PlakaIslemTipi islemTipi, 
        decimal? islemTutari = null, int? cariId = null, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Plaka kontrolü
        if (await PlakaMevcutMu(yeniPlaka))
            throw new InvalidOperationException($"Bu plaka ({yeniPlaka}) başka bir araçta aktif olarak kullanılıyor.");
        
        // Mevcut aktif plakayı kapat
        var mevcutAktif = await context.AracPlakalar
            .FirstOrDefaultAsync(ap => ap.AracId == aracId && !ap.IsDeleted && ap.CikisTarihi == null);
            
        if (mevcutAktif != null)
        {
            mevcutAktif.CikisTarihi = DateTime.UtcNow;
            mevcutAktif.UpdatedAt = DateTime.UtcNow;
        }
        
        // Yeni plaka ekle
        var yeniPlakaKaydi = new AracPlaka
        {
            AracId = aracId,
            Plaka = yeniPlaka,
            GirisTarihi = DateTime.UtcNow,
            IslemTipi = islemTipi,
            IslemTutari = islemTutari,
            CariId = cariId,
            Aciklama = aciklama,
            CreatedAt = DateTime.UtcNow
        };
        context.AracPlakalar.Add(yeniPlakaKaydi);
        
        // Araçtaki aktif plakayı güncelle
        var arac = await context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
        if (arac != null)
        {
            arac.AktifPlaka = yeniPlaka;
            arac.UpdatedAt = DateTime.UtcNow;
        }
        
        await context.SaveChangesAsync();
        return yeniPlakaKaydi;
    }

    public async Task<bool> AddPlakaToAracAsync(AracPlaka yeniPlaka)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (yeniPlaka.AracId <= 0 || string.IsNullOrWhiteSpace(yeniPlaka.Plaka))
            return false;

        var plakaText = yeniPlaka.Plaka.Trim().ToUpperInvariant();
        if (await PlakaMevcutMu(plakaText, yeniPlaka.Id > 0 ? yeniPlaka.Id : null))
            throw new InvalidOperationException($"Bu plaka ({plakaText}) başka bir araçta aktif olarak kullanılıyor.");

        var arac = await context.Araclar.FirstOrDefaultAsync(a => a.Id == yeniPlaka.AracId && !a.IsDeleted);
        if (arac == null)
            throw new InvalidOperationException("Araç bulunamadı.");

        var girisTarihi = yeniPlaka.GirisTarihi == default ? DateTime.Today : yeniPlaka.GirisTarihi;
        var yeniKayit = new AracPlaka
        {
            AracId = yeniPlaka.AracId,
            Plaka = plakaText,
            GirisTarihi = girisTarihi,
            CikisTarihi = yeniPlaka.CikisTarihi,
            IslemTipi = yeniPlaka.IslemTipi,
            IslemTutari = yeniPlaka.IslemTutari,
            CariId = yeniPlaka.CariId,
            Aciklama = yeniPlaka.Aciklama,
            CreatedAt = DateTime.UtcNow
        };

        context.AracPlakalar.Add(yeniKayit);
        await context.SaveChangesAsync();

        await GuncelleAktifPlaka(context, yeniPlaka.AracId);
        return true;
    }

    public async Task<bool> DeletePlakaFromAracAsync(int aracPlakaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var plakaKaydi = await context.AracPlakalar.FirstOrDefaultAsync(ap => ap.Id == aracPlakaId && !ap.IsDeleted);
        if (plakaKaydi == null)
            return false;

        plakaKaydi.IsDeleted = true;
        plakaKaydi.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        await GuncelleAktifPlaka(context, plakaKaydi.AracId);
        return true;
    }

    public async Task ClosePlakaAsync(int aracPlakaId, DateTime cikisTarihi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var plakaKaydi = await context.AracPlakalar.FirstOrDefaultAsync(ap => ap.Id == aracPlakaId && !ap.IsDeleted);
        if (plakaKaydi == null)
            throw new InvalidOperationException("Plaka kaydı bulunamadı.");

        plakaKaydi.CikisTarihi = cikisTarihi;
        plakaKaydi.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        await GuncelleAktifPlaka(context, plakaKaydi.AracId);
    }
    
    public async Task PlakaCikis(int aracPlakaId, PlakaIslemTipi cikisIslemTipi, 
        decimal? islemTutari = null, int? cariId = null, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var plakaKaydi = await context.AracPlakalar
            .Include(ap => ap.Arac)
            .FirstOrDefaultAsync(ap => ap.Id == aracPlakaId && !ap.IsDeleted);
            
        if (plakaKaydi == null)
            throw new InvalidOperationException("Plaka kaydı bulunamadı.");
            
        if (plakaKaydi.CikisTarihi.HasValue)
            throw new InvalidOperationException("Bu plaka zaten kapatılmış.");
        
        plakaKaydi.CikisTarihi = DateTime.UtcNow;
        plakaKaydi.IslemTipi = cikisIslemTipi;
        if (islemTutari.HasValue) plakaKaydi.IslemTutari = islemTutari;
        if (cariId.HasValue) plakaKaydi.CariId = cariId;
        if (!string.IsNullOrEmpty(aciklama)) plakaKaydi.Aciklama = aciklama;
        plakaKaydi.UpdatedAt = DateTime.UtcNow;
        
        // Araçtaki aktif plakayı temizle
        if (plakaKaydi.Arac != null)
        {
            plakaKaydi.Arac.AktifPlaka = null;
            plakaKaydi.Arac.UpdatedAt = DateTime.UtcNow;
        }
        
        await context.SaveChangesAsync();
    }
    
    private async Task GuncelleAktifPlaka(ApplicationDbContext context, int aracId)
    {
        var arac = await context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
        if (arac == null) return;
        
        // CikisTarihi null olan veya CikisTarihi bugünden sonra olan plakalardan en son eklenen
        var aktifPlaka = await context.AracPlakalar
            .Where(ap => ap.AracId == aracId && 
                        !ap.IsDeleted &&
                        (ap.CikisTarihi == null || ap.CikisTarihi > DateTime.Today))
            .OrderByDescending(ap => ap.GirisTarihi)
            .FirstOrDefaultAsync();
            
        arac.AktifPlaka = aktifPlaka?.Plaka;
        await context.SaveChangesAsync();
    }
    
    #endregion
    
    #region Satışa Açık Araçlar
    
    public async Task<List<Arac>> GetSatisaAcikAraclarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
            .Where(a => a.SatisaAcik && a.Aktif && !a.IsDeleted)
            .OrderBy(a => a.SatisaAcilmaTarihi)
            .ToListAsync();
    }
    
    public async Task AracSatisaAc(int aracId, decimal satisFiyati, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
        if (arac == null)
            throw new InvalidOperationException("Araç bulunamadı.");
            
        arac.SatisaAcik = true;
        arac.SatisFiyati = satisFiyati;
        arac.SatisaAcilmaTarihi = DateTime.UtcNow;
        arac.SatisAciklamasi = aciklama;
        arac.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }
    
    public async Task AracSatisKapat(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
        if (arac == null)
            throw new InvalidOperationException("Araç bulunamadı.");
            
        arac.SatisaAcik = false;
        arac.SatisFiyati = null;
        arac.SatisaAcilmaTarihi = null;
        arac.SatisAciklamasi = null;
        arac.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }
    
    #endregion

    #region Arac Evrak Islemleri

    public async Task<List<AracEvrak>> GetAracEvraklariAsync(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracEvraklari
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .Where(e => e.AracId == aracId && !e.IsDeleted)
            .OrderBy(e => e.EvrakKategorisi)
            .ThenByDescending(e => e.BitisTarihi)
            .ToListAsync();
    }

    public async Task<AracEvrak?> GetAracEvrakByIdAsync(int evrakId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracEvraklari
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == evrakId && !e.IsDeleted);
    }

    public async Task<AracEvrak> CreateAracEvrakAsync(AracEvrak evrak)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (evrak.BaslangicTarihi.HasValue)
            evrak.BaslangicTarihi = DateTime.SpecifyKind(evrak.BaslangicTarihi.Value, DateTimeKind.Utc);
        if (evrak.BitisTarihi.HasValue)
            evrak.BitisTarihi = DateTime.SpecifyKind(evrak.BitisTarihi.Value, DateTimeKind.Utc);

        evrak.CreatedAt = DateTime.UtcNow;
        context.AracEvraklari.Add(evrak);
        await context.SaveChangesAsync();
        await SenkronizeAracBelgeTarihleriAsync(context, evrak.AracId);
        return evrak;
    }

    public async Task<AracEvrak> UpdateAracEvrakAsync(AracEvrak evrak)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (evrak.BaslangicTarihi.HasValue)
            evrak.BaslangicTarihi = DateTime.SpecifyKind(evrak.BaslangicTarihi.Value, DateTimeKind.Utc);
        if (evrak.BitisTarihi.HasValue)
            evrak.BitisTarihi = DateTime.SpecifyKind(evrak.BitisTarihi.Value, DateTimeKind.Utc);

        evrak.UpdatedAt = DateTime.UtcNow;
        context.AracEvraklari.Update(evrak);
        await context.SaveChangesAsync();
        await SenkronizeAracBelgeTarihleriAsync(context, evrak.AracId);
        return evrak;
    }

    public async Task DeleteAracEvrakAsync(int evrakId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var evrak = await context.AracEvraklari
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == evrakId && !e.IsDeleted);

        if (evrak != null)
        {
            // Dosyaları sil
            foreach (var dosya in evrak.Dosyalar)
            {
                await _secureFileService.DeleteAsync(dosya.DosyaYolu);
                dosya.IsDeleted = true;
            }

            evrak.IsDeleted = true;
            await context.SaveChangesAsync();
            await SenkronizeAracBelgeTarihleriAsync(context, evrak.AracId);
        }
    }

    /// <summary>
    /// AracEvrak tablosundaki en güncel (bitiş tarihi en yüksek, aktif, silinmemiş) kayıtlardan
    /// Arac tablosundaki geriye dönük belge bitiş tarihlerini (Muayene/Trafik/Kasko/Koltuk) tekilleştirir.
    /// Böylece evrak güncellendiğinde uyarı/rapor/listelerde tek kaynaktan tutarlı tarih görünür.
    /// </summary>
    private static async Task SenkronizeAracBelgeTarihleriAsync(ApplicationDbContext context, int aracId)
    {
        if (aracId <= 0) return;
        var arac = await context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
        if (arac == null) return;

        var aktifEvraklar = await context.AracEvraklari
            .Where(e => e.AracId == aracId
                     && !e.IsDeleted
                     && e.Durum != EvrakDurum.Pasif
                     && e.BitisTarihi.HasValue)
            .Select(e => new { e.EvrakKategorisi, e.BitisTarihi })
            .ToListAsync();

        static DateTime? NormalizeUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            return value.Value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        }

        DateTime? EnYakin(string kategori) => NormalizeUtc(aktifEvraklar
            .Where(x => x.EvrakKategorisi == kategori)
            .OrderByDescending(x => x.BitisTarihi)
            .Select(x => x.BitisTarihi)
            .FirstOrDefault());

        var yeniMuayene = EnYakin(EvrakKategorileri.Muayene);
        var yeniTrafik = EnYakin(EvrakKategorileri.TrafikSigortasi);
        var yeniKasko = EnYakin(EvrakKategorileri.Kasko);
        var yeniKoltuk = EnYakin(EvrakKategorileri.KoltukSigortasi);

        var degisti = false;
        if (arac.MuayeneBitisTarihi != yeniMuayene) { arac.MuayeneBitisTarihi = yeniMuayene; degisti = true; }
        if (arac.TrafikSigortaBitisTarihi != yeniTrafik) { arac.TrafikSigortaBitisTarihi = yeniTrafik; degisti = true; }
        if (arac.KaskoBitisTarihi != yeniKasko) { arac.KaskoBitisTarihi = yeniKasko; degisti = true; }
        if (arac.KoltukSigortasiBitisTarihi != yeniKoltuk) { arac.KoltukSigortasiBitisTarihi = yeniKoltuk; degisti = true; }

        if (degisti)
        {
            arac.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<AracEvrakDosya> UploadEvrakDosyaAsync(int evrakId, IBrowserFile file)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var evrak = await context.AracEvraklari
            .Include(e => e.Arac)
                .ThenInclude(a => a!.Firma)
            .FirstOrDefaultAsync(e => e.Id == evrakId && !e.IsDeleted);
        if (evrak == null)
            throw new Exception("Evrak bulunamadi");

        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var icerik = memoryStream.ToArray();
        var uzanti = Path.GetExtension(file.Name);

        // Araç arşiv klasörü: {Plaka} - {FirmaAdi}
        var plaka = evrak.Arac?.AktifPlaka ?? evrak.Arac?.SaseNo ?? evrak.AracId.ToString();
        var aracKlasoru = AppStoragePaths.BuildAracArsivKlasoru(plaka, evrak.Arac?.Firma?.FirmaAdi);

        // Dosya adı: {PLAKA}{EvrakTipi}_{yyyyMMdd_HHmmss}.uzanti (boşluksuz)
        var normPlaka = AppStoragePaths.NormalizeFolderName(plaka).Replace(" ", "").Replace("-", "");
        var normEvrak = AppStoragePaths.NormalizeFolderName(evrak.EvrakKategorisi ?? "Evrak").Replace(" ", "").Replace("-", "");
        var arsivDosyaAdi = $"{normPlaka}{normEvrak}_{DateTime.Now:yyyyMMdd_HHmmss}{uzanti}";
        string? storedPath = null;
        try
        {
            storedPath = await _secureFileService.SaveEncryptedAsync(
                $"{AppStoragePaths.AracEvrakRelativeRoot}/{aracKlasoru}",
                arsivDosyaAdi,
                icerik);

            // Arşiv kopyaları (şifreli + şifresiz) - BelgeUyariService akışıyla aynı.
            var sasiNo = evrak.Arac?.SaseNo ?? evrak.AracId.ToString();
            try
            {
                await _evrakArsivService.ArsivleAracEvrakAsync(plaka, sasiNo, evrak.EvrakKategorisi ?? "Evrak", icerik, uzanti);
            }
            catch
            {
                // Arşivleme hatası ana upload akışını kesmemeli.
            }

            var evrakDosya = new AracEvrakDosya
            {
                AracEvrakId = evrakId,
                DosyaAdi = arsivDosyaAdi,
                DosyaYolu = storedPath,
                DosyaTipi = uzanti.TrimStart('.').ToLower(),
                DosyaBoyutu = icerik.LongLength,
                CreatedAt = DateTime.UtcNow
            };

            context.AracEvrakDosyalari.Add(evrakDosya);
            await context.SaveChangesAsync();
            return evrakDosya;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(storedPath))
            {
                try { await _secureFileService.DeleteAsync(storedPath); } catch { }
            }

            throw;
        }
    }

    public async Task<byte[]> GetEvrakDosyaAsync(int dosyaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var dosya = await context.AracEvrakDosyalari
            .Include(d => d.AracEvrak)
            .FirstOrDefaultAsync(d => d.Id == dosyaId && !d.IsDeleted && d.AracEvrak != null && !d.AracEvrak.IsDeleted);
        if (dosya == null)
            throw new Exception("Dosya bulunamadi");

        var content = await _secureFileService.ReadDecryptedAsync(dosya.DosyaYolu);
        if (content == null)
            throw new Exception("Dosya diskte bulunamadi");

        return content;
    }

    public async Task DeleteEvrakDosyaAsync(int dosyaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var dosya = await context.AracEvrakDosyalari
            .Include(d => d.AracEvrak)
            .FirstOrDefaultAsync(d => d.Id == dosyaId && !d.IsDeleted && d.AracEvrak != null && !d.AracEvrak.IsDeleted);
        if (dosya != null)
        {
            await _secureFileService.DeleteAsync(dosya.DosyaYolu);

            context.AracEvrakDosyalari.Remove(dosya);
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Evrak Uyarilari

    public async Task<List<AracEvrak>> GetSuresiDolacakEvraklarAsync(int gunSayisi = 30)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.UtcNow.Date;
        var bitisTarihi = bugun.AddDays(gunSayisi);

        return await context.AracEvraklari
            .Include(e => e.Arac)
            .Where(e => e.Durum == EvrakDurum.Aktif && 
                        e.BitisTarihi.HasValue && 
                        e.BitisTarihi.Value <= bitisTarihi)
            .OrderBy(e => e.BitisTarihi)
            .ToListAsync();
    }

    #endregion

    #region Excel Import/Export

    public async Task<byte[]> GetExcelSablonAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Araclar");
        
        var headers = new[]
        {
            "Şase No *", "Plaka", "Marka", "Model", "Model Yılı", "Motor No", "Renk", "Koltuk Sayısı",
            "Araç Tipi", "Sahiplik Tipi", "KM", "Muayene Bitiş Tarihi", "Trafik Sigortası Bitiş Tarihi",
            "Kasko Bitiş Tarihi", "Aktif", "Notlar"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
        }
        
        ws.Cell(2, 1).Value = "WVWZZZ3CZWE123456";
        ws.Cell(2, 2).Value = "34ABC123";
        ws.Cell(2, 3).Value = "VOLKSWAGEN";
        ws.Cell(2, 4).Value = "CARAVELLE";
        ws.Cell(2, 5).Value = 2023;
        ws.Cell(2, 6).Value = "DFG123456";
        ws.Cell(2, 7).Value = "BEYAZ";
        ws.Cell(2, 8).Value = 9;
        ws.Cell(2, 9).Value = "Minibüs";
        ws.Cell(2, 10).Value = "Özmal";
        ws.Cell(2, 11).Value = 15000;
        ws.Cell(2, 12).Value = DateTime.Today.AddYears(1);
        ws.Cell(2, 13).Value = DateTime.Today.AddYears(1);
        ws.Cell(2, 14).Value = DateTime.Today.AddYears(1);
        ws.Cell(2, 15).Value = "Evet";
        ws.Cell(2, 16).Value = "Excel şablon örnek kaydı";

        ws.Range(2, 12, 2, 14).Style.DateFormat.Format = "dd.MM.yyyy";
        
        ws.Cell(5, 1).Value = "AÇIKLAMALAR:";
        ws.Cell(5, 1).Style.Font.Bold = true;
        ws.Cell(6, 1).Value = "* Şase No: Zorunlu, benzersiz olmalı (17 karakter)";
        ws.Cell(7, 1).Value = "* Araç Tipi: Minibüs, Midibüs, Otobüs, Otomobil, Panelvan";
        ws.Cell(8, 1).Value = "* Sahiplik Tipi: Özmal, Kiralık, Komisyon, Diğer";
        ws.Cell(9, 1).Value = "* Tarih alanları: GG.AA.YYYY formatında";
        ws.Cell(10, 1).Value = "* Aktif: Evet/Hayır, Aktif/Pasif, True/False";
        ws.Cell(11, 1).Value = "* Plaka opsiyoneldir, varsa aktif plaka olarak kaydedilir";
        
        ws.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<AracImportResult> ImportFromExcelAsync(byte[] fileContent)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new AracImportResult();
        
        try
        {
            using var stream = new MemoryStream(fileContent);
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var ws = workbook.Worksheets.First();
            
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            var lastColumn = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            var kolonlar = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Debug: Başlıkları logla
            for (int col = 1; col <= lastColumn; col++)
            {
                var rawHeader = ws.Cell(1, col).GetString();
                var header = NormalizeExcelHeader(rawHeader);
                if (!string.IsNullOrWhiteSpace(header) && !kolonlar.ContainsKey(header))
                {
                    kolonlar[header] = col;
                }
            }

            // Şase No kolonunu bul - birden fazla varyant dene
            int? saseNoKolon = null;
            var saseNoVariants = new[] { "SASE NO", "SASENO", "ŞASE NO", "ŞASİ NO" };
            foreach (var variant in saseNoVariants)
            {
                var normalizedVariant = NormalizeExcelHeader(variant);
                if (kolonlar.TryGetValue(normalizedVariant, out var col))
                {
                    saseNoKolon = col;
                    break;
                }
            }

            if (!saseNoKolon.HasValue)
            {
                result.Errors.Add("Şase No kolonu bulunamadı. Lütfen şablonu kontrol edin.");
                result.Success = false;
                return result;
            }

            var mevcutSaseNolar = await context.Araclar.Where(a => !a.IsDeleted).Select(a => a.SaseNo.ToUpper()).ToListAsync();
            var aktifPlakalar = await context.AracPlakalar
                .Include(ap => ap.Arac)
                .Where(ap => !ap.IsDeleted &&
                             ap.Arac != null &&
                             !ap.Arac.IsDeleted &&
                             (ap.CikisTarihi == null || ap.CikisTarihi > DateTime.Today))
                .Select(ap => new { ap.Plaka, ap.AracId })
                .ToListAsync();
            var aktifPlakaAracMap = aktifPlakalar
                .GroupBy(ap => ap.Plaka.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().AracId, StringComparer.OrdinalIgnoreCase);
            var excelPlakaSaseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            for (int row = 2; row <= lastRow; row++)
            {
                try
                {
                    var saseNo = ws.Cell(row, saseNoKolon.Value).GetString()?.Trim().ToUpper();

                    if (string.IsNullOrWhiteSpace(saseNo))
                        continue;

                    // Açıklama satırlarını atla
                    if (saseNo.StartsWith("*") || saseNo.StartsWith("AÇIKLAMA") || saseNo.Length < 5)
                        continue;

                    var plaka = GetCellValue(ws, row, kolonlar, "PLAKA")?.Trim().ToUpperInvariant();
                    var marka = GetCellValue(ws, row, kolonlar, "MARKA");
                    var model = GetCellValue(ws, row, kolonlar, "MODEL");
                    var modelYiliStr = GetCellValue(ws, row, kolonlar, "MODEL YILI");
                    var motorNo = GetCellValue(ws, row, kolonlar, "MOTOR NO");
                    var renk = GetCellValue(ws, row, kolonlar, "RENK");
                    var koltukSayisiStr = GetCellValue(ws, row, kolonlar, "KOLTUK SAYISI");
                    var aracTipiStr = GetCellValue(ws, row, kolonlar, "ARAC TIPI");
                    var sahiplikTipiStr = GetCellValue(ws, row, kolonlar, "SAHIPLIK TIPI");
                    var kmStr = GetCellValue(ws, row, kolonlar, "KM");
                    var notlar = GetCellValue(ws, row, kolonlar, "NOTLAR");

                    int? modelYili = null;
                    if (int.TryParse(modelYiliStr, out var y)) modelYili = y;

                    int koltukSayisi = 0;
                    if (int.TryParse(koltukSayisiStr, out var k)) koltukSayisi = k;

                    int? km = null;
                    if (int.TryParse(kmStr?.Replace(".", "").Replace(",", ""), out var kmVal)) km = kmVal;

                    var aracTipi = ParseAracTipi(aracTipiStr);
                    var sahiplikTipi = ParseAracSahiplikTipi(sahiplikTipiStr);
                    var muayeneBitis = GetCellDateValue(ws, row, kolonlar, "MUAYENE BITIS TARIHI");
                    var trafikSigortaBitis = GetCellDateValue(ws, row, kolonlar, "TRAFIK SIGORTASI BITIS TARIHI");
                    var kaskoBitis = GetCellDateValue(ws, row, kolonlar, "KASKO BITIS TARIHI");
                    var aktif = GetCellBoolValue(ws, row, kolonlar, "AKTIF");

                    // İşlemin güncelleme mi yeni kayıt mı olduğunu baştan belirle
                    var isUpdate = mevcutSaseNolar.Contains(saseNo);
                    var mevcutAracOzet = isUpdate
                        ? await context.Araclar
                            .Where(a => a.SaseNo.ToUpper() == saseNo && !a.IsDeleted)
                            .Select(a => new { a.Id, a.AktifPlaka })
                            .FirstOrDefaultAsync()
                        : null;

                    if (!string.IsNullOrWhiteSpace(plaka))
                    {
                        if (excelPlakaSaseMap.TryGetValue(plaka, out var oncekiSaseNo) && !string.Equals(oncekiSaseNo, saseNo, StringComparison.OrdinalIgnoreCase))
                        {
                            result.SkippedRecords.Add($"Satır {row} ({saseNo} / {plaka}): Aynı plaka Excel içinde daha önce {oncekiSaseNo} için işlendi, bu kayıt atlandı.");
                            result.SkippedCount++;
                            continue;
                        }

                        if (aktifPlakaAracMap.TryGetValue(plaka, out var plakaSahibiAracId) && (!isUpdate || mevcutAracOzet == null || plakaSahibiAracId != mevcutAracOzet.Id))
                        {
                            result.SkippedRecords.Add($"Satır {row} ({saseNo} / {plaka}): Plaka sistemde başka bir araçta aktif olduğu için kayıt atlandı.");
                            result.SkippedCount++;
                            continue;
                        }
                    }

                    // ExecutionStrategy ile transaction sarmalama (NpgsqlRetryingExecutionStrategy uyumluluğu)
                    var strategy = context.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        await using var transaction = await context.Database.BeginTransactionAsync();

                        if (isUpdate)
                    {
                        var mevcutArac = await context.Araclar
                            .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
                            .FirstOrDefaultAsync(a => a.SaseNo.ToUpper() == saseNo && !a.IsDeleted);

                        if (mevcutArac != null)
                        {
                            if (!string.IsNullOrWhiteSpace(marka)) mevcutArac.Marka = marka;
                            if (!string.IsNullOrWhiteSpace(model)) mevcutArac.Model = model;
                            if (modelYili.HasValue) mevcutArac.ModelYili = modelYili;
                            if (!string.IsNullOrWhiteSpace(motorNo)) mevcutArac.MotorNo = motorNo;
                            if (!string.IsNullOrWhiteSpace(renk)) mevcutArac.Renk = renk;
                            if (koltukSayisi > 0) mevcutArac.KoltukSayisi = koltukSayisi;
                            if (km.HasValue) mevcutArac.KmDurumu = km;
                            if (!string.IsNullOrWhiteSpace(aracTipiStr)) mevcutArac.AracTipi = aracTipi;
                            if (!string.IsNullOrWhiteSpace(sahiplikTipiStr)) mevcutArac.SahiplikTipi = sahiplikTipi;
                            if (muayeneBitis.HasValue) mevcutArac.MuayeneBitisTarihi = DateTime.SpecifyKind(muayeneBitis.Value.Date, DateTimeKind.Utc);
                            if (trafikSigortaBitis.HasValue) mevcutArac.TrafikSigortaBitisTarihi = DateTime.SpecifyKind(trafikSigortaBitis.Value.Date, DateTimeKind.Utc);
                            if (kaskoBitis.HasValue) mevcutArac.KaskoBitisTarihi = DateTime.SpecifyKind(kaskoBitis.Value.Date, DateTimeKind.Utc);
                            if (aktif.HasValue) mevcutArac.Aktif = aktif.Value;
                            if (!string.IsNullOrWhiteSpace(notlar)) mevcutArac.Notlar = notlar;

                            if (!string.IsNullOrWhiteSpace(plaka) && !string.Equals(mevcutArac.AktifPlaka, plaka, StringComparison.OrdinalIgnoreCase))
                            {
                                var plakaKullanimda = await context.AracPlakalar
                                    .Include(ap => ap.Arac)
                                    .AnyAsync(ap => ap.Plaka == plaka &&
                                                    !ap.IsDeleted &&
                                                    ap.Arac != null &&
                                                    !ap.Arac.IsDeleted &&
                                                    (ap.CikisTarihi == null || ap.CikisTarihi > DateTime.Today) &&
                                                    ap.AracId != mevcutArac.Id);

                                if (plakaKullanimda)
                                    throw new InvalidOperationException($"Plaka başka bir araçta aktif: {plaka}");

                                foreach (var aktifPlakaKaydi in mevcutArac.PlakaGecmisi.Where(p => p.CikisTarihi == null))
                                {
                                    aktifPlakaKaydi.CikisTarihi = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
                                }

                                mevcutArac.AktifPlaka = plaka;
                                mevcutArac.Plaka = plaka;
                                mevcutArac.PlakaGecmisi.Add(new AracPlaka
                                {
                                    Plaka = plaka,
                                    GirisTarihi = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc),
                                    IslemTipi = PlakaIslemTipi.PlakaDevir,
                                    Aciklama = "Excel'den güncellendi",
                                    CreatedAt = DateTime.UtcNow
                                });
                            }

                            mevcutArac.UpdatedAt = DateTime.UtcNow;
                            await context.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(plaka))
                        {
                            var plakaKullanimda = await context.AracPlakalar
                                .Include(ap => ap.Arac)
                                .AnyAsync(ap => ap.Plaka == plaka &&
                                                !ap.IsDeleted &&
                                                ap.Arac != null &&
                                                !ap.Arac.IsDeleted &&
                                                (ap.CikisTarihi == null || ap.CikisTarihi > DateTime.Today));

                            if (plakaKullanimda)
                                throw new InvalidOperationException($"Plaka başka bir araçta aktif: {plaka}");
                        }

                        var yeniArac = new Arac
                        {
                            SaseNo = saseNo,
                            Marka = marka,
                            Model = model,
                            ModelYili = modelYili,
                            MotorNo = motorNo,
                            Renk = renk,
                            KoltukSayisi = koltukSayisi,
                            AracTipi = aracTipi,
                            SahiplikTipi = sahiplikTipi,
                            KmDurumu = km,
                            MuayeneBitisTarihi = muayeneBitis.HasValue ? DateTime.SpecifyKind(muayeneBitis.Value.Date, DateTimeKind.Utc) : null,
                            TrafikSigortaBitisTarihi = trafikSigortaBitis.HasValue ? DateTime.SpecifyKind(trafikSigortaBitis.Value.Date, DateTimeKind.Utc) : null,
                            KaskoBitisTarihi = kaskoBitis.HasValue ? DateTime.SpecifyKind(kaskoBitis.Value.Date, DateTimeKind.Utc) : null,
                            Aktif = aktif ?? true,
                            Notlar = notlar,
                            CreatedAt = DateTime.UtcNow
                        };

                        if (!string.IsNullOrWhiteSpace(plaka))
                        {
                            yeniArac.AktifPlaka = plaka;
                            yeniArac.Plaka = plaka;
                            yeniArac.PlakaGecmisi.Add(new AracPlaka
                            {
                                Plaka = plaka,
                                GirisTarihi = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc),
                                IslemTipi = PlakaIslemTipi.Alis,
                                Aciklama = "Excel'den aktarıldı",
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        context.Araclar.Add(yeniArac);
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    }); // ExecutionStrategy lambda sonu

                    // İşlem başarılı - sayaç güncelle
                    if (!string.IsNullOrWhiteSpace(plaka))
                    {
                        excelPlakaSaseMap[plaka] = saseNo;
                        if (isUpdate && mevcutAracOzet != null && !string.IsNullOrWhiteSpace(mevcutAracOzet.AktifPlaka) && !string.Equals(mevcutAracOzet.AktifPlaka, plaka, StringComparison.OrdinalIgnoreCase))
                        {
                            aktifPlakaAracMap.Remove(mevcutAracOzet.AktifPlaka.ToUpper());
                        }

                        if (isUpdate && mevcutAracOzet != null)
                            aktifPlakaAracMap[plaka] = mevcutAracOzet.Id;
                    }

                    if (isUpdate)
                        result.UpdatedCount++;
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(plaka) && mevcutSaseNolar.Count >= 0)
                        {
                            // Yeni kayıtta araç id SaveChanges sonrası set edilir; aynı import içinde plaka tekrarını önlemek için işaretliyoruz.
                            aktifPlakaAracMap[plaka] = int.MinValue;
                        }

                        mevcutSaseNolar.Add(saseNo); // Yeni eklenen şase numarasını listeye ekle
                        result.ImportedCount++;
                    }
                }
                catch (Exception ex)
                {
                    context.ChangeTracker.Clear();
                    // Daha açıklayıcı hata mesajı
                    var saseNoHata = ws.Cell(row, saseNoKolon.Value).GetString()?.Trim() ?? "?";
                    var plakaHata = GetCellValue(ws, row, kolonlar, "PLAKA") ?? "";
                    var hataMesaji = ex.InnerException?.Message ?? ex.Message;
                    result.Errors.Add($"Satır {row} ({saseNoHata} / {plakaHata}): {hataMesaji}");
                    result.ErrorCount++;
                }
            }
            
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Excel okuma hatası: {ex.Message}");
            result.Success = false;
        }
        
        return result;
    }

    private static string? GetCellValue(ClosedXML.Excel.IXLWorksheet ws, int row, Dictionary<string, int> kolonlar, string baslik)
    {
        if (kolonlar.TryGetValue(baslik, out var col))
        {
            return ws.Cell(row, col).GetString()?.Trim();
        }
        return null;
    }

    private static DateTime? GetCellDateValue(ClosedXML.Excel.IXLWorksheet ws, int row, Dictionary<string, int> kolonlar, string baslik)
    {
        if (!kolonlar.TryGetValue(baslik, out var col))
            return null;

        var cell = ws.Cell(row, col);
        if (cell.IsEmpty())
            return null;

        if (cell.DataType == ClosedXML.Excel.XLDataType.DateTime)
            return cell.GetDateTime();

        if (cell.DataType == ClosedXML.Excel.XLDataType.Number)
            return DateTime.FromOADate(cell.GetDouble());

        if (DateTime.TryParse(cell.GetString(), new System.Globalization.CultureInfo("tr-TR"), out var tarih))
            return tarih;

        return null;
    }

    private static bool? GetCellBoolValue(ClosedXML.Excel.IXLWorksheet ws, int row, Dictionary<string, int> kolonlar, string baslik)
    {
        var value = GetCellValue(ws, row, kolonlar, baslik);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.ToUpperInvariant().Trim();
        return normalized switch
        {
            "EVET" or "E" or "TRUE" or "1" or "AKTIF" or "AKTİF" => true,
            "HAYIR" or "H" or "FALSE" or "0" or "PASIF" or "PASİF" => false,
            _ => null
        };
    }

    private AracTipi ParseAracTipi(string? tip)
    {
        if (string.IsNullOrWhiteSpace(tip)) return AracTipi.Minibus;
        
        var tipUpper = tip.ToUpperInvariant().Replace("İ", "I").Replace("Ü", "U").Replace("Ö", "O");
        
        return tipUpper switch
        {
            "MINIBUS" or "MİNİBÜS" => AracTipi.Minibus,
            "MIDIBUS" or "MİDİBÜS" => AracTipi.Midibus,
            "OTOBUS" or "OTOBÜS" => AracTipi.Otobus,
            "OTOMOBIL" or "OTOMOBİL" => AracTipi.Otomobil,
            "PANELVAN" => AracTipi.Panelvan,
            _ => AracTipi.Minibus
        };
    }

    private AracSahiplikTipi ParseAracSahiplikTipi(string? tip)
    {
        if (string.IsNullOrWhiteSpace(tip)) return AracSahiplikTipi.Ozmal;

        var tipUpper = NormalizeExcelHeader(tip);
        return tipUpper switch
        {
            "OZMAL" => AracSahiplikTipi.Ozmal,
            "KIRALIK" => AracSahiplikTipi.Kiralik,
            "KOMISYON" => AracSahiplikTipi.Komisyon,
            "DIGER" => AracSahiplikTipi.Diger,
            _ => AracSahiplikTipi.Ozmal
        };
    }

    private static string NormalizeExcelHeader(string? value)
    {
        return string.Join(" ", (value ?? string.Empty)
            .Replace("*", string.Empty)
            .Replace("İ", "I")
            .Replace("I", "I")
            .Replace("ı", "i")
            .Replace("Ş", "S")
            .Replace("ş", "s")
            .Replace("Ğ", "G")
            .Replace("ğ", "g")
            .Replace("Ü", "U")
            .Replace("ü", "u")
            .Replace("Ö", "O")
            .Replace("ö", "o")
            .Replace("Ç", "C")
            .Replace("ç", "c")
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private static string GetCellString(ClosedXML.Excel.IXLWorksheet ws, int row, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (kolonlar.TryGetValue(NormalizeExcelHeader(baslik), out var col))
            {
                return ws.Cell(row, col).GetString()?.Trim() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static DateTime? GetCellDate(ClosedXML.Excel.IXLWorksheet ws, int row, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!kolonlar.TryGetValue(NormalizeExcelHeader(baslik), out var col))
                continue;

            var cell = ws.Cell(row, col);
            if (cell.IsEmpty())
                return null;

            if (cell.DataType == ClosedXML.Excel.XLDataType.DateTime)
                return cell.GetDateTime();

            if (cell.DataType == ClosedXML.Excel.XLDataType.Number)
                return DateTime.FromOADate(cell.GetDouble());

            if (DateTime.TryParse(cell.GetString(), new System.Globalization.CultureInfo("tr-TR"), out var tarih))
                return tarih;
        }

        return null;
    }

    private static bool? GetCellBool(ClosedXML.Excel.IXLWorksheet ws, int row, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        var value = GetCellString(ws, row, kolonlar, basliklar);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = NormalizeExcelHeader(value);
        return normalized switch
        {
            "EVET" or "E" or "TRUE" or "1" or "AKTIF" => true,
            "HAYIR" or "H" or "FALSE" or "0" or "PASIF" => false,
            _ => null
        };
    }

    #endregion
}


