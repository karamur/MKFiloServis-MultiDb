using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Web.Helpers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace MKFiloServis.Web.Services;

public class AracService : IAracService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;
    private readonly IEvrakArsivService _evrakArsivService;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly ILogger<AracService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AracService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISecureFileService secureFileService,
        IEvrakArsivService evrakArsivService,
        IAktifFirmaProvider aktifFirmaProvider,
        ILogger<AracService> logger,
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
        _evrakArsivService = evrakArsivService;
        _aktifFirmaProvider = aktifFirmaProvider;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
    }


    #region Araç CRUD İşlemleri

    public Task<List<Arac>> GetAllAsync() => AraclariGetirAsync(false);

    public Task<List<Arac>> GetActiveAsync() => AraclariGetirAsync(true);

    private sealed class FirmaSecimiDegistiException : Exception { }

    private async Task<List<Arac>> AraclariGetirAsync(bool sadeceAktif)
    {
        long secimSurumu = 0;
        void SecimDegisti() => Interlocked.Increment(ref secimSurumu);
        _aktifFirmaProvider.AktifFirmaDegisti += SecimDegisti;
        try
        {
            for (var deneme = 0; deneme < 3; deneme++)
            {
                var surum = Volatile.Read(ref secimSurumu);
                var secim = _aktifFirmaProvider.Mevcut;
                var tumFirmalar = secim.TumFirmalar;
                int? firmaId = secim.FirmaId > 0 ? secim.FirmaId : null;
                void SecimiDogrula()
                {
                    if (surum != Volatile.Read(ref secimSurumu) ||
                        _aktifFirmaProvider.TumFirmalar != tumFirmalar ||
                        _aktifFirmaProvider.AktifFirmaId != firmaId)
                        throw new FirmaSecimiDegistiException();
                }
                try
                {
                    SecimiDogrula();
                    if (!tumFirmalar && firmaId is not > 0)
                        return new List<Arac>(); // Seçimsiz F0 önbelleği oluşturma.
                    SecimiDogrula();
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    SecimiDogrula();
                    var query = context.Araclar.AsNoTracking()
                        .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
                        .Include(a => a.Firma)
                        .Where(a => !a.IsDeleted && (!sadeceAktif || a.Aktif));
                    if (!tumFirmalar)
                        query = query.Where(a => a.FirmaId == firmaId);
                    var araclar = await query.ToListAsync();
                    SecimiDogrula();
                    var bugun = DateTime.Today;
                    foreach (var arac in araclar)
                    {
                        var aktifPlaka = arac.PlakaGecmisi
                            .Where(p => p.CikisTarihi == null || p.CikisTarihi > bugun)
                            .OrderByDescending(p => p.GirisTarihi).ThenByDescending(p => p.Id)
                            .FirstOrDefault();
                        if (aktifPlaka != null && arac.AktifPlaka != aktifPlaka.Plaka)
                        {
                            arac.AktifPlaka = aktifPlaka.Plaka;
                            arac.Plaka = aktifPlaka.Plaka;
                        }
                    }
                    SecimiDogrula();
                    return araclar.OrderBy(a => a.AktifPlaka ?? a.SaseNo).ThenBy(a => a.Id).ToList();
                }
                catch (FirmaSecimiDegistiException)
                {
                    _logger.LogDebug("Araç liste yüklemesi firma değişimi nedeniyle yeniden deneniyor.");
                }
            }
            throw new InvalidOperationException("Firma seçimi yükleme sırasında değişti. Araç listesini yeniden yükleyin.");
        }
        finally
        {
            _aktifFirmaProvider.AktifFirmaDegisti -= SecimDegisti;
        }
    }

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
        // Sase no tum firmalarda tekildir; tenant filtresinden bagimsiz kontrol edilir.
        return await context.Araclar
            .IgnoreQueryFilters()
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
        ArgumentNullException.ThrowIfNull(arac);
        if (arac.Id <= 0 || string.IsNullOrWhiteSpace(arac.SaseNo))
            throw new ArgumentException("Geçerli araç kimliği ve şase numarası gerekir.", nameof(arac));
        var firmaId = _aktifFirmaProvider.AktifFirmaId;
        if (_aktifFirmaProvider.TumFirmalar || firmaId is not > 0)
            throw new InvalidOperationException("Araç güncellemek için tek bir firma seçin.");
        var hedefFirmaId = arac.FirmaId ?? firmaId.Value;
        if (hedefFirmaId <= 0) throw new ArgumentException("Geçerli hedef firma gerekir.", nameof(arac));
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var principal = _httpContextAccessor.HttpContext is { } http
                ? http.User
                : (await _authenticationStateProvider.GetAuthenticationStateAsync()).User;
            if (principal.Identity?.IsAuthenticated != true ||
                !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("KullaniciId")?.Value, out var userId) ||
                userId <= 0 || !await context.Kullanicilar.AnyAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted && !k.Rol.IsDeleted))
                throw new UnauthorizedAccessException("Aktif kullanıcı oturumu gerekir.");
            var admin = await FirmaBakimYoneticisiMiAsync(context);
            if (hedefFirmaId != firmaId.Value)
                throw new InvalidOperationException("Firma değişikliği için araç taşıma akışını kullanın.");
            if (!await context.Firmalar.AnyAsync(f => f.Id == firmaId.Value && !f.IsDeleted))
                throw new InvalidOperationException("Seçili firma erişilebilir değil.");

            var existing = await context.Araclar.AsTracking()
                .FirstOrDefaultAsync(a => a.Id == arac.Id && a.FirmaId == firmaId.Value && !a.IsDeleted);
            if (existing == null) throw new InvalidOperationException("Araç bulunamadı veya seçili firmada erişilebilir değil.");
            var firmalar = admin ? context.Firmalar.IgnoreQueryFilters() : context.Firmalar.AsQueryable();
            if (!await firmalar.AnyAsync(f => f.Id == hedefFirmaId && !f.IsDeleted))
                throw new InvalidOperationException("Hedef firma bulunamadı veya erişilebilir değil.");
            var cariler = admin ? context.Cariler.IgnoreQueryFilters() : context.Cariler.AsQueryable();
            foreach (var cariId in new[] { arac.KiralikCariId, arac.KomisyoncuCariId })
            {
                if (cariId.HasValue && (cariId.Value <= 0 || !await cariler.AnyAsync(c => c.Id == cariId.Value && c.FirmaId == hedefFirmaId && !c.IsDeleted)))
                    throw new InvalidOperationException("Araç carileri hedef firmaya ait ve erişilebilir olmalıdır.");
            }
            var saseNo = arac.SaseNo.Trim().ToUpperInvariant();
            if (await context.Araclar.IgnoreQueryFilters().AnyAsync(a => a.Id != arac.Id && !a.IsDeleted && a.SaseNo.ToUpper() == saseNo))
                throw new InvalidOperationException("Şase numarası kullanımda.");
            if (_aktifFirmaProvider.TumFirmalar || _aktifFirmaProvider.AktifFirmaId != firmaId)
                throw new InvalidOperationException("Firma seçimi değişti; aracı yeniden açın.");

            GuncelleAracAlanlari(existing, arac, hedefFirmaId);

            // Aktif plakayı araç değişiklikleriyle birlikte kaydet.
            if (hedefFirmaId == firmaId.Value)
                await GuncelleAktifPlaka(context, existing.Id, firmaId.Value);
            if (_aktifFirmaProvider.TumFirmalar || _aktifFirmaProvider.AktifFirmaId != firmaId)
                throw new InvalidOperationException("Firma seçimi değişti; aracı yeniden açın.");
            await context.SaveChangesAsync();

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç güncelleme başarısız. Araç: {AracId}, Firma: {FirmaId}, Hedef: {HedefFirmaId}", arac.Id, firmaId, hedefFirmaId);
            if (ex is UnauthorizedAccessException || ex is InvalidOperationException && ex.InnerException == null) throw;
            throw new InvalidOperationException("Araç güncellenemedi. Kaydı ve firma seçimini kontrol edin.", ex);
        }
    }

    internal static void GuncelleAracAlanlari(Arac existing, Arac arac, int hedefFirmaId)
    {
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
        existing.FirmaId = hedefFirmaId;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    public async Task DeleteAsync(int id)
    {
        if (id <= 0) throw new ArgumentException("Geçerli bir araç seçin.", nameof(id));
        var firmaId = _aktifFirmaProvider.AktifFirmaId;
        if (_aktifFirmaProvider.TumFirmalar || firmaId is not > 0)
            throw new InvalidOperationException("Araç silmek için tek bir firma seçin.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        if (!await context.Firmalar.AnyAsync(f => f.Id == firmaId.Value && !f.IsDeleted))
            throw new InvalidOperationException("Seçili firma bulunamadı veya erişilebilir değil.");
        var arac = await context.Araclar.AsTracking()
            .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == id && a.FirmaId == firmaId.Value && !a.IsDeleted)
            ?? throw new InvalidOperationException("Araç bulunamadı veya seçili firmaya ait değil.");

        var simdi = DateTime.UtcNow;
        foreach (var aktifPlaka in arac.PlakaGecmisi.Where(p => p.CikisTarihi == null || p.CikisTarihi > DateTime.Today))
        {
            aktifPlaka.CikisTarihi = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
            aktifPlaka.UpdatedAt = simdi;
        }
        arac.AktifPlaka = null;
        arac.IsDeleted = true;
        arac.DeletedAt = simdi;
        arac.UpdatedAt = simdi;
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// FirmaId değeri null olan araçları verilen firmaya atar.
    /// Eski (multi-tenant öncesi) kayıtların puantaj/fatura akışlarında hata vermemesi için kullanılır.
    /// Global query filter'ı by-pass ederek tüm firmasız araçları yakalar.
    /// </summary>
    public async Task<int> BackfillFirmaIdAsync(int firmaId)
    {
        if (firmaId <= 0) throw new ArgumentException("Geçerli bir firma seçin.", nameof(firmaId));
        if (_aktifFirmaProvider.TumFirmalar || _aktifFirmaProvider.AktifFirmaId != firmaId)
            throw new InvalidOperationException("Atama hedefi tek seçili firma olmalıdır.");
        long secimSurumu = 0;
        void FirmaDegisimi() => System.Threading.Interlocked.Increment(ref secimSurumu);
        void SecimiDogrula()
        {
            if (System.Threading.Volatile.Read(ref secimSurumu) != 0 ||
                _aktifFirmaProvider.TumFirmalar || _aktifFirmaProvider.AktifFirmaId != firmaId)
                throw new InvalidOperationException("Firma seçimi değişti; atamayı yeniden başlatın.");
        }
        _aktifFirmaProvider.AktifFirmaDegisti += FirmaDegisimi;
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (!await FirmaBakimYoneticisiMiAsync(context))
                throw new UnauthorizedAccessException("Firmasız araç ataması yalnız aktif Admin kullanıcısı tarafından yapılabilir.");
            SecimiDogrula();
            if (!await context.Firmalar.AnyAsync(f => f.Id == firmaId && !f.IsDeleted))
                throw new InvalidOperationException("Seçili firma bulunamadı veya erişilebilir değil.");
            SecimiDogrula();
            // Yalnız bu Admin bakım yolunda, yalnız firmasız/silinmemiş araçlar için filtre atlanır.
            var firmasizlar = await context.Araclar.IgnoreQueryFilters().AsTracking()
                .Where(a => !a.IsDeleted && a.FirmaId == null).ToListAsync();
            SecimiDogrula();
            if (firmasizlar.Count == 0) return 0;
            var simdi = DateTime.UtcNow;
            foreach (var arac in firmasizlar)
            {
                arac.FirmaId = firmaId;
                arac.UpdatedAt = simdi;
            }
            // Bekleme sırasında kaldırılan Admin yetkisini kayıt öncesinde tekrar kontrol et.
            if (!await FirmaBakimYoneticisiMiAsync(context))
                throw new UnauthorizedAccessException("Firma bakım yetkisi artık geçerli değil.");
            SecimiDogrula();
            await context.SaveChangesAsync();
            return firmasizlar.Count;
        }
        finally { _aktifFirmaProvider.AktifFirmaDegisti -= FirmaDegisimi; }
    }

    private async Task<bool> FirmaBakimYoneticisiMiAsync(ApplicationDbContext context)
    {
        var principal = _httpContextAccessor.HttpContext is { } http
            ? http.User
            : (await _authenticationStateProvider.GetAuthenticationStateAsync()).User;
        if (principal.Identity?.IsAuthenticated != true ||
            !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("KullaniciId")?.Value, out var userId) || userId <= 0)
            return false;
        return await context.Kullanicilar.AnyAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted &&
            !k.Rol.IsDeleted && k.Rol.RolAdi == SistemRolleri.Admin);
    }

    /// <summary>Yetkili Admin için firmasız araç sayısı; diğer kullanıcılar için sıfır.</summary>
    public async Task<int> GetFirmaIdYokSayisiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (!await FirmaBakimYoneticisiMiAsync(context)) return 0;
        return await context.Araclar.IgnoreQueryFilters()
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
        var firmaId = await PlakaYazimFirmasiniGetirAsync(context);
        if (aracId <= 0 || string.IsNullOrWhiteSpace(yeniPlaka))
            throw new InvalidOperationException("Geçerli araç ve plaka gereklidir.");
        yeniPlaka = yeniPlaka.Trim().ToUpperInvariant();
        var arac = await context.Araclar.AsTracking().FirstOrDefaultAsync(a => a.Id == aracId && a.FirmaId == firmaId && !a.IsDeleted)
            ?? throw new InvalidOperationException("Araç bulunamadı veya erişilebilir değil.");
        // Plaka kontrolü
        if (await PlakaKullaniminiKontrolEtAsync(context, yeniPlaka, firmaId))
            throw new InvalidOperationException($"Bu plaka ({yeniPlaka}) başka bir araçta aktif olarak kullanılıyor.");

        await PlakaCarisiniDogrulaAsync(context, cariId, firmaId);
        // Mevcut aktif plakayı kapat
        var mevcutAktif = await context.AracPlakalar.AsTracking()
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

        await GuncelleAktifPlaka(context, aracId, firmaId);

        await context.SaveChangesAsync();
        return yeniPlakaKaydi;
    }

    public async Task<bool> AddPlakaToAracAsync(AracPlaka yeniPlaka)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var firmaId = await PlakaYazimFirmasiniGetirAsync(context);
        if (yeniPlaka.Id != 0)
            throw new InvalidOperationException("Yeni plaka kaydının kimliği sıfır olmalıdır.");
        if (yeniPlaka.AracId <= 0 || string.IsNullOrWhiteSpace(yeniPlaka.Plaka))
            return false;

        var plakaText = yeniPlaka.Plaka.Trim().ToUpperInvariant();
        if (await PlakaKullaniminiKontrolEtAsync(context, plakaText, firmaId))
            throw new InvalidOperationException($"Bu plaka ({plakaText}) başka bir araçta aktif olarak kullanılıyor.");

        var arac = await context.Araclar.AsTracking().FirstOrDefaultAsync(a => a.Id == yeniPlaka.AracId && a.FirmaId == firmaId && !a.IsDeleted);
        if (arac == null)
            throw new InvalidOperationException("Araç bulunamadı.");

        await PlakaCarisiniDogrulaAsync(context, yeniPlaka.CariId, firmaId);
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
        await GuncelleAktifPlaka(context, yeniPlaka.AracId, firmaId);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePlakaFromAracAsync(int aracPlakaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var firmaId = await PlakaYazimFirmasiniGetirAsync(context);
        var plakaKaydi = await context.AracPlakalar.AsTracking().FirstOrDefaultAsync(ap => ap.Id == aracPlakaId && !ap.IsDeleted &&
                context.Araclar.Any(a => a.Id == ap.AracId && a.FirmaId == firmaId && !a.IsDeleted));
        if (plakaKaydi == null)
            return false;

        plakaKaydi.IsDeleted = true;
        plakaKaydi.DeletedAt = DateTime.UtcNow;
        plakaKaydi.UpdatedAt = plakaKaydi.DeletedAt;
        await GuncelleAktifPlaka(context, plakaKaydi.AracId, firmaId);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task ClosePlakaAsync(int aracPlakaId, DateTime cikisTarihi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var firmaId = await PlakaYazimFirmasiniGetirAsync(context);
        var plakaKaydi = await context.AracPlakalar.AsTracking().FirstOrDefaultAsync(ap => ap.Id == aracPlakaId && !ap.IsDeleted &&
                context.Araclar.Any(a => a.Id == ap.AracId && a.FirmaId == firmaId && !a.IsDeleted));
        if (plakaKaydi == null)
            throw new InvalidOperationException("Plaka kaydı bulunamadı.");

        plakaKaydi.CikisTarihi = cikisTarihi;
        plakaKaydi.UpdatedAt = DateTime.UtcNow;
        await GuncelleAktifPlaka(context, plakaKaydi.AracId, firmaId);
        await context.SaveChangesAsync();
    }

    public async Task PlakaCikis(int aracPlakaId, PlakaIslemTipi cikisIslemTipi,
        decimal? islemTutari = null, int? cariId = null, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var firmaId = await PlakaYazimFirmasiniGetirAsync(context);
        var plakaKaydi = await context.AracPlakalar.AsTracking()
            .Include(ap => ap.Arac)
            .FirstOrDefaultAsync(ap => ap.Id == aracPlakaId && !ap.IsDeleted &&
                context.Araclar.Any(a => a.Id == ap.AracId && a.FirmaId == firmaId && !a.IsDeleted));

        if (plakaKaydi == null)
            throw new InvalidOperationException("Plaka kaydı bulunamadı.");

        if (plakaKaydi.CikisTarihi.HasValue)
            throw new InvalidOperationException("Bu plaka zaten kapatılmış.");

        await PlakaCarisiniDogrulaAsync(context, cariId ?? plakaKaydi.CariId, firmaId);
        plakaKaydi.CikisTarihi = DateTime.UtcNow;
        plakaKaydi.IslemTipi = cikisIslemTipi;
        if (islemTutari.HasValue) plakaKaydi.IslemTutari = islemTutari;
        if (cariId.HasValue) plakaKaydi.CariId = cariId;
        if (!string.IsNullOrEmpty(aciklama)) plakaKaydi.Aciklama = aciklama;
        plakaKaydi.UpdatedAt = DateTime.UtcNow;

        // Başka aktif geçmiş kaydı varsa onu seç; araç değerini koşulsuz boşaltma.
        await GuncelleAktifPlaka(context, plakaKaydi.AracId, firmaId, plakaKaydi.Id);

        await context.SaveChangesAsync();
    }

    private async Task<int> PlakaYazimFirmasiniGetirAsync(ApplicationDbContext context)
    {
        var firmaId = _aktifFirmaProvider.AktifFirmaId;
        if (_aktifFirmaProvider.TumFirmalar || firmaId is not > 0)
            throw new InvalidOperationException("Plaka işlemi için tek bir firma seçin.");
        if (!await context.Firmalar.AnyAsync(f => f.Id == firmaId.Value && !f.IsDeleted))
            throw new InvalidOperationException("Seçili firma bulunamadı veya erişilebilir değil.");
        return firmaId.Value;
    }

    private static async Task PlakaCarisiniDogrulaAsync(ApplicationDbContext context, int? cariId, int firmaId)
    {
        if (!cariId.HasValue) return;
        if (cariId.Value <= 0 || !await context.Cariler.AnyAsync(c => c.Id == cariId.Value && c.FirmaId == firmaId && !c.IsDeleted))
            throw new InvalidOperationException("Plaka işlemindeki cari seçili firmaya ait ve erişilebilir olmalıdır.");
    }

    private static Task<bool> PlakaKullaniminiKontrolEtAsync(ApplicationDbContext context, string plaka, int firmaId)
    {
        var bugun = DateTime.UtcNow.Date;
        return context.AracPlakalar.AnyAsync(ap => ap.Plaka == plaka && !ap.IsDeleted &&
            (ap.CikisTarihi == null || ap.CikisTarihi > bugun) &&
            context.Araclar.Any(a => a.Id == ap.AracId && a.FirmaId == firmaId && !a.IsDeleted));
    }

    private static async Task GuncelleAktifPlaka(ApplicationDbContext context, int aracId, int? firmaId = null, int? kapatilanPlakaId = null)
    {
        var arac = await context.Araclar.AsTracking().FirstOrDefaultAsync(a => a.Id == aracId && (!firmaId.HasValue || a.FirmaId == firmaId) && !a.IsDeleted)
            ?? throw new InvalidOperationException("Araç bulunamadı veya erişilebilir değil.");
        // SQL'de çıkış filtresi kullanma: bekleyen kapatma/silme ve yeni kayıtları da gör.
        var kayitlar = await context.AracPlakalar.AsTracking()
            .Where(ap => ap.AracId == aracId && !ap.IsDeleted).ToListAsync();
        var adaylar = new HashSet<AracPlaka>(kayitlar, ReferenceEqualityComparer.Instance);
        foreach (var entry in context.ChangeTracker.Entries<AracPlaka>())
        {
            if (entry.Entity.AracId == aracId && entry.State == EntityState.Added)
                adaylar.Add(entry.Entity);
            if (entry.State == EntityState.Deleted)
                adaylar.Remove(entry.Entity);
        }
        var bugun = DateTime.Today;
        var aktifPlaka = adaylar
            .Where(ap => !ap.IsDeleted && ap.Id != kapatilanPlakaId && (ap.CikisTarihi == null || ap.CikisTarihi > bugun))
            .OrderByDescending(ap => ap.GirisTarihi)
            .ThenByDescending(ap => context.Entry(ap).State == EntityState.Added)
            .ThenByDescending(ap => ap.Id)
            .FirstOrDefault();
        var plaka = aktifPlaka?.Plaka;
        if (arac.AktifPlaka != plaka)
        {
            arac.AktifPlaka = plaka;
            arac.UpdatedAt = DateTime.UtcNow;
        }
        // Çağıran plaka geçmişi ve araç değişikliğini tek SaveChanges ile kaydeder.
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
        var arac = await context.Araclar.AsTracking().FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
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
        var arac = await context.Araclar.AsTracking().FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
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
        await SenkronizeAracBelgeTarihleriAsync(context, evrak.AracId);
        await context.SaveChangesAsync();
        return evrak;
    }

    public async Task<AracEvrak> UpdateAracEvrakAsync(AracEvrak evrak)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Mevcut kaydi yukleyip sadece duzenlenen alanlari aktar;
        // aksi halde kopya entity'deki bos FirmaId/CreatedAt degerleri
        // (Kind=Unspecified) veritabanina yazilirken hataya yol acar.
        var mevcut = await context.AracEvraklari.AsTracking()
            .FirstOrDefaultAsync(e => e.Id == evrak.Id && !e.IsDeleted)
            ?? throw new InvalidOperationException("Guncellenecek evrak bulunamadi.");

        mevcut.EvrakKategorisi = evrak.EvrakKategorisi;
        mevcut.EvrakAdi = evrak.EvrakAdi;
        mevcut.Aciklama = evrak.Aciklama;
        mevcut.BaslangicTarihi = evrak.BaslangicTarihi.HasValue
            ? DateTime.SpecifyKind(evrak.BaslangicTarihi.Value, DateTimeKind.Utc)
            : null;
        mevcut.BitisTarihi = evrak.BitisTarihi.HasValue
            ? DateTime.SpecifyKind(evrak.BitisTarihi.Value, DateTimeKind.Utc)
            : null;
        mevcut.Tutar = evrak.Tutar;
        mevcut.SigortaSirketi = evrak.SigortaSirketi;
        mevcut.PoliceNo = evrak.PoliceNo;
        mevcut.Durum = evrak.Durum;
        mevcut.HatirlatmaAktif = evrak.HatirlatmaAktif;
        mevcut.HatirlatmaGunOnce = evrak.HatirlatmaGunOnce;
        mevcut.UpdatedAt = DateTime.UtcNow;

        await SenkronizeAracBelgeTarihleriAsync(context, mevcut.AracId);
        await context.SaveChangesAsync();
        return mevcut;
    }

    public async Task DeleteAracEvrakAsync(int evrakId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var evrak = await context.AracEvraklari.AsTracking()
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == evrakId && !e.IsDeleted);
        if (evrak == null) return;

        var dosyalar = evrak.Dosyalar.Select(d => (d.Id, d.DosyaYolu)).ToArray();
        var simdi = DateTime.UtcNow;
        foreach (var dosya in evrak.Dosyalar)
        {
            dosya.IsDeleted = true;
            dosya.DeletedAt = simdi;
            dosya.UpdatedAt = simdi;
        }
        evrak.IsDeleted = true;
        evrak.DeletedAt = simdi;
        evrak.UpdatedAt = simdi;
        await SenkronizeAracBelgeTarihleriAsync(context, evrak.AracId);
        await context.SaveChangesAsync();
        await SilinenEvrakDosyalariniTemizleAsync(evrakId, dosyalar);
    }

    /// <summary>
    /// AracEvrak tablosundaki en güncel (bitiş tarihi en yüksek, aktif, silinmemiş) kayıtlardan
    /// Arac tablosundaki geriye dönük belge bitiş tarihlerini (Muayene/Trafik/Kasko/Koltuk) tekilleştirir.
    /// Bekleyen belge değişikliklerini de hesaba katar; kaydı çağıran tek SaveChanges ile yapar.
    /// </summary>
    private static async Task SenkronizeAracBelgeTarihleriAsync(ApplicationDbContext context, int aracId)
    {
        if (aracId <= 0)
            throw new InvalidOperationException("Belge işlemi için geçerli bir araç gereklidir.");
        var arac = await context.Araclar.AsTracking().FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
        if (arac == null)
            throw new InvalidOperationException("Belgenin aracı bulunamadı veya erişilebilir değil.");

        // DB'deki değerlerden önce identity resolution ile bekleyen güncellemeleri kullan.
        // Aktif/tarihli filtreyi SQL'de uygulama: henüz kaydedilmemiş aktifleşme/kategori
        // değişiklikleri ve silmeler de sonuç tarihini etkilemelidir.
        var kayitliEvraklar = await context.AracEvraklari.AsTracking()
            .Where(e => e.AracId == aracId && !e.IsDeleted)
            .ToListAsync();
        var adaylar = new HashSet<AracEvrak>(kayitliEvraklar, ReferenceEqualityComparer.Instance);
        foreach (var entry in context.ChangeTracker.Entries<AracEvrak>())
        {
            if (entry.Entity.AracId == aracId && entry.State == EntityState.Added)
                adaylar.Add(entry.Entity);
            if (entry.State == EntityState.Deleted)
                adaylar.Remove(entry.Entity);
        }
        var aktifEvraklar = adaylar
            .Where(e => !e.IsDeleted && e.Durum != EvrakDurum.Pasif && e.BitisTarihi.HasValue)
            .ToList();

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

        // Tek arşiv sistemi: C:\MKFiloServis_yedekleme\Arsiv\Sifreli\Araclar\{PLAKA - FIRMA}
        // ve evrak tipi bazında tekil dosya adı.
        var plaka = evrak.Arac?.AktifPlaka ?? evrak.Arac?.SaseNo ?? evrak.AracId.ToString();
        var firmaAdi = evrak.Arac?.Firma?.FirmaAdi ?? "FIRMA_YOK";
        string? storedPath = null;
        try
        {
            storedPath = await _evrakArsivService.ArsivleAracEvrakAsync(
                plaka,
                firmaAdi,
                evrak.EvrakKategorisi ?? "Evrak",
                icerik,
                uzanti);

            var evrakDosya = new AracEvrakDosya
            {
                AracEvrakId = evrakId,
                DosyaAdi = Path.GetFileName(file.Name),
                DosyaYolu = storedPath,
                DosyaTipi = uzanti.TrimStart('.').ToLower(),
                DosyaBoyutu = icerik.LongLength,
                CreatedAt = DateTime.UtcNow
            };

            context.AracEvrakDosyalari.Add(evrakDosya);
            await context.SaveChangesAsync();
            return evrakDosya;
        }
        catch (Exception uploadException)
        {
            if (!string.IsNullOrWhiteSpace(storedPath))
            {
                try
                {
                    await using var verify = await _contextFactory.CreateDbContextAsync();
                    var isReferenced = await verify.AracEvrakDosyalari.AsNoTracking()
                        .AnyAsync(x => x.DosyaYolu == storedPath) ||
                        await verify.AracEvrakDosyaVersiyonlar.AsNoTracking()
                            .AnyAsync(x => x.DosyaYolu == storedPath);
                    if (!isReferenced)
                        await _secureFileService.DeleteAsync(storedPath);
                    else
                        _logger.LogWarning(
                            "Araç evrak upload sonrası hata oluştu ancak dosya DB'de referanslı; fiziksel dosya korunuyor. EvrakId={EvrakId}, DosyaYolu={DosyaYolu}",
                            evrakId, storedPath);
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException(
                        "Araç evrak yükleme başarısız veya belirsiz; yeni dosyanın DB başvurusu doğrulanamadı ve dosya güvenlik için korunuyor.",
                        uploadException, cleanupException);
                }
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
        var dosya = await context.AracEvrakDosyalari.AsTracking()
            .Include(d => d.AracEvrak)
            .FirstOrDefaultAsync(d => d.Id == dosyaId && !d.IsDeleted && d.AracEvrak != null && !d.AracEvrak.IsDeleted);
        if (dosya == null) return;

        dosya.IsDeleted = true;
        dosya.DeletedAt = DateTime.UtcNow;
        dosya.UpdatedAt = dosya.DeletedAt;
        await context.SaveChangesAsync();
        await SilinenEvrakDosyalariniTemizleAsync(dosya.AracEvrakId, new[] { (dosya.Id, dosya.DosyaYolu) });
    }

    private async Task SilinenEvrakDosyalariniTemizleAsync(int evrakId, IEnumerable<(int Id, string DosyaYolu)> dosyalar)
    {
        foreach (var dosya in dosyalar)
        {
            if (string.IsNullOrWhiteSpace(dosya.DosyaYolu)) continue;
            // IsDeleted geri alınabilir bir işarettir. Dosya DB satırıyla birlikte
            // restore/migrasyon için korunur; yalnız kalıcı purge ayrı iş olmalıdır.
            _logger.LogInformation(
                "Araç evrakı soft-delete edildi; fiziksel dosya geri alma için korundu. EvrakId={EvrakId}, DosyaId={DosyaId}, DosyaYolu={DosyaYolu}",
                evrakId, dosya.Id, dosya.DosyaYolu);
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
        if (fileContent == null || fileContent.Length == 0 || fileContent.Length > 10 * 1024 * 1024)
            throw new ArgumentException("Excel dosyası boş veya 10 MB sınırını aşıyor.", nameof(fileContent));
        var firmaId = _aktifFirmaProvider.AktifFirmaId;
        if (_aktifFirmaProvider.TumFirmalar || firmaId is not > 0)
            throw new InvalidOperationException("Araç aktarımı için tek bir firma seçin.");
        long secimSurumu = 0;
        void FirmaDegisimi() => System.Threading.Interlocked.Increment(ref secimSurumu);
        void SecimiDogrula()
        {
            if (System.Threading.Volatile.Read(ref secimSurumu) != 0 ||
                _aktifFirmaProvider.AktifFirmaId != firmaId || _aktifFirmaProvider.TumFirmalar)
                throw new InvalidOperationException("Aktarım sırasında firma seçimi değişti.");
        }
        _aktifFirmaProvider.AktifFirmaDegisti += FirmaDegisimi;
        try
        {
        SecimiDogrula();
        await using var context = await _contextFactory.CreateDbContextAsync();
        SecimiDogrula();
        if (!await context.Firmalar.AnyAsync(f => f.Id == firmaId.Value && !f.IsDeleted))
            throw new InvalidOperationException("Seçili firma bulunamadı veya erişilebilir değil.");
        SecimiDogrula();
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

            var mevcutSaseNolar = await context.Araclar.Where(a => a.FirmaId == firmaId.Value && !a.IsDeleted).Select(a => a.SaseNo.ToUpper()).ToListAsync();
            var aktifPlakalar = await context.AracPlakalar
                .Include(ap => ap.Arac)
                .Where(ap => !ap.IsDeleted &&
                             ap.Arac != null &&
                             !ap.Arac.IsDeleted && ap.Arac.FirmaId == firmaId.Value &&
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
                    SecimiDogrula();
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
                            .Where(a => a.FirmaId == firmaId.Value && a.SaseNo.ToUpper() == saseNo && !a.IsDeleted)
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
                        SecimiDogrula();
                        context.ChangeTracker.Clear();
                        await using var transaction = await context.Database.BeginTransactionAsync();
                        SecimiDogrula();

                        if (isUpdate)
                    {
                        var mevcutArac = await context.Araclar.AsTracking()
                            .Include(a => a.PlakaGecmisi.Where(p => !p.IsDeleted))
                            .FirstOrDefaultAsync(a => a.FirmaId == firmaId.Value && a.SaseNo.ToUpper() == saseNo && !a.IsDeleted);
                        SecimiDogrula();
                        if (mevcutArac == null)
                            throw new InvalidOperationException("Güncellenecek araç artık erişilebilir değil.");

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
                                                    !ap.Arac.IsDeleted && ap.Arac.FirmaId == firmaId.Value &&
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
                            SecimiDogrula();
                            await context.SaveChangesAsync();
                            SecimiDogrula();
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
                                                !ap.Arac.IsDeleted && ap.Arac.FirmaId == firmaId.Value &&
                                                (ap.CikisTarihi == null || ap.CikisTarihi > DateTime.Today));

                            if (plakaKullanimda)
                                throw new InvalidOperationException($"Plaka başka bir araçta aktif: {plaka}");
                        }

                        var yeniArac = new Arac
                        {
                            FirmaId = firmaId.Value,
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
                        SecimiDogrula();
                            await context.SaveChangesAsync();
                            SecimiDogrula();
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
                    _logger.LogError(ex, "Araç Excel satırı kaydedilemedi. Satır: {Row}, Firma: {FirmaId}", row, firmaId);
                    var secimDegisti = System.Threading.Volatile.Read(ref secimSurumu) != 0 ||
                        _aktifFirmaProvider.AktifFirmaId != firmaId || _aktifFirmaProvider.TumFirmalar;
                    result.Errors.Add($"Satır {row} ({saseNoHata} / {plakaHata}): " +
                        (secimDegisti ? "Firma seçimi değişti; kalan aktarım durduruldu." : "Kayıt tamamlanamadı; satır verilerini kontrol edin."));
                    result.ErrorCount++;
                    if (secimDegisti) break;
                }
            }

            SecimiDogrula();
            result.Success = result.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç Excel aktarımı tamamlanamadı. Firma: {FirmaId}", firmaId);
            result.Errors.Add("Aktarım tamamlanamadı. Dosya biçimini ve firma seçimini kontrol edin; önceki başarılı satırlar kaydedilmiş olabilir.");
            result.ErrorCount++;
            result.Success = false;
        }

        return result;
        }
        finally
        {
            _aktifFirmaProvider.AktifFirmaDegisti -= FirmaDegisimi;
        }
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


