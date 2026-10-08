using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Firma değişikliğinde entity ve bağlı verilerin taşınması/kopyalanması.
/// </summary>
public class FirmaTransferService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly Interfaces.ICacheService _cache;
    private readonly IAktifFirmaProvider _firmaProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _auth;
    private readonly ILogger<FirmaTransferService> _logger;

    public FirmaTransferService(IDbContextFactory<ApplicationDbContext> dbFactory, Interfaces.ICacheService cache, IAktifFirmaProvider firmaProvider, IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider auth, ILogger<FirmaTransferService> logger)
    {
        _dbFactory = dbFactory;
        _cache = cache;
        _firmaProvider = firmaProvider;
        _httpContextAccessor = httpContextAccessor;
        _auth = auth;
        _logger = logger;
    }

    // ── Güzergah Transfer ──────────────────────────────────────────────────

    public async Task<List<TransferableItem>> GetGuzergahTransferItemsAsync(int guzergahId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var items = new List<TransferableItem>();

        var seferSayisi = await db.GuzergahSeferleri.CountAsync(s => s.GuzergahId == guzergahId);
        if (seferSayisi > 0)
            items.Add(new TransferableItem { Key = "GuzergahSefer", Label = $"Seferler ({seferSayisi} adet)", Count = seferSayisi, Selected = true });

        var puantajSayisi = await db.PuantajKayitlar.CountAsync(p => p.GuzergahId == guzergahId && !p.IsDeleted);
        if (puantajSayisi > 0)
            items.Add(new TransferableItem { Key = "PuantajKayit", Label = $"Puantaj Kayıtları ({puantajSayisi} adet)", Count = puantajSayisi, Selected = true });

        return items;
    }

    public async Task<(int moved, List<string> errors)> MoveGuzergahToFirmaAsync(int guzergahId, int targetFirmaId, List<string> selectedKeys)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        int moved = 0;
        var errors = new List<string>();

        if (selectedKeys.Contains("GuzergahSefer"))
        {
            try
            {
                var seferler = await db.GuzergahSeferleri.Where(s => s.GuzergahId == guzergahId).ToListAsync();
                foreach (var s in seferler) s.FirmaId = targetFirmaId;
                moved += seferler.Count;
            }
            catch (Exception ex) { errors.Add($"Seferler: {ex.Message}"); }
        }

        if (selectedKeys.Contains("PuantajKayit"))
        {
            try
            {
                var puantajlar = await db.PuantajKayitlar.Where(p => p.GuzergahId == guzergahId && !p.IsDeleted).ToListAsync();
                foreach (var p in puantajlar) p.IsverenFirmaId = targetFirmaId;
                moved += puantajlar.Count;
            }
            catch (Exception ex) { errors.Add($"Puantaj: {ex.Message}"); }
        }

        var guzergah = await db.Guzergahlar.FindAsync(guzergahId);
        if (guzergah != null) guzergah.FirmaId = targetFirmaId;

        await db.SaveChangesAsync();
        return (moved, errors);
    }

    // ── Araç Transfer ──────────────────────────────────────────────────────

    public async Task<List<TransferableItem>> GetAracTransferItemsAsync(int aracId)
    {
        if (aracId <= 0) throw new ArgumentException("Geçerli araç kimliği gerekir.", nameof(aracId));
        var kaynakFirmaId = _firmaProvider.AktifFirmaId;
        if (_firmaProvider.TumFirmalar || kaynakFirmaId is not > 0)
            throw new InvalidOperationException("Taşıma için aracın kaynak firmasını seçin.");
        long secimSurumu = 0;
        void FirmaDegisimi() => System.Threading.Interlocked.Increment(ref secimSurumu);
        void SecimiDogrula()
        {
            if (System.Threading.Volatile.Read(ref secimSurumu) != 0 || _firmaProvider.TumFirmalar || _firmaProvider.AktifFirmaId != kaynakFirmaId)
                throw new InvalidOperationException("Kaynak firma seçimi değişti; taşımayı yeniden başlatın.");
        }
        _firmaProvider.AktifFirmaDegisti += FirmaDegisimi;
        try
        {
            SecimiDogrula();
            await using var db = await _dbFactory.CreateDbContextAsync();
            var principal = _httpContextAccessor.HttpContext is { } http ? http.User : (await _auth.GetAuthenticationStateAsync()).User;
            if (principal.Identity?.IsAuthenticated != true ||
                !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("KullaniciId")?.Value, out var userId) || userId <= 0 ||
                !await db.Kullanicilar.AnyAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted && !k.Rol.IsDeleted && k.Rol.RolAdi == SistemRolleri.Admin))
                throw new UnauthorizedAccessException("Araç taşıma envanteri için aktif Admin yetkisi gerekir.");
            if (!await db.Araclar.AnyAsync(a => a.Id == aracId && a.FirmaId == kaynakFirmaId && !a.IsDeleted))
                throw new InvalidOperationException("Araç kaynak firmada bulunamadı veya erişilebilir değil.");
            SecimiDogrula();
            var items = new List<TransferableItem>();

            // Filtre atlama yalnız yetkilendirilmiş kaynak aracın ilişki envanteri içindir.
            var evrakSayisi = await db.AracEvraklari.IgnoreQueryFilters().CountAsync(e => e.AracId == aracId && !e.IsDeleted);
            if (evrakSayisi > 0)
                items.Add(new TransferableItem { Key = "AracEvrak", Label = $"Evraklar ve dosyaları ({evrakSayisi} evrak) - araçla birlikte zorunlu", Count = evrakSayisi, Selected = true, Required = true });

            var plakaSayisi = await db.AracPlakalar.IgnoreQueryFilters().CountAsync(p => p.AracId == aracId && !p.IsDeleted);
            if (plakaSayisi > 0)
                items.Add(new TransferableItem { Key = "AracPlaka", Label = $"Plaka Geçmişi ({plakaSayisi} adet) - bilgi amaçlı", Count = plakaSayisi, Selected = false });

            var puantajSayisi = await db.PuantajKayitlar.IgnoreQueryFilters().CountAsync(p => p.AracId == aracId && !p.IsDeleted);
            if (puantajSayisi > 0)
                items.Add(new TransferableItem { Key = "PuantajKayit", Label = $"Puantaj Kayıtları ({puantajSayisi} adet)", Count = puantajSayisi, Selected = true });

            var servisSayisi = await db.ServisCalismalari.IgnoreQueryFilters().CountAsync(s => s.AracId == aracId && !s.IsDeleted);
            if (servisSayisi > 0)
                items.Add(new TransferableItem { Key = "ServisCalisma", Label = $"Servis çalışma kayıtları ({servisSayisi} adet)", Count = servisSayisi, Selected = true });

            var plakaTakipSayisi = await db.KiralikPlakaTakipler.IgnoreQueryFilters().CountAsync(k => k.AracId == aracId && !k.IsDeleted);
            if (plakaTakipSayisi > 0)
                items.Add(new TransferableItem { Key = "KiralikPlakaTakip", Label = $"Plaka Takip ({plakaTakipSayisi} adet)", Count = plakaTakipSayisi, Selected = false });

            SecimiDogrula();
            return items;
        }
        finally { _firmaProvider.AktifFirmaDegisti -= FirmaDegisimi; }
    }

    public async Task<(int moved, List<string> errors)> MoveAracToFirmaAsync(int aracId, int targetFirmaId, List<string> selectedKeys, Arac? guncelAlanlar = null)
    {
        if (aracId <= 0 || targetFirmaId <= 0 || selectedKeys == null)
            throw new ArgumentException("Geçerli araç, hedef firma ve taşıma seçimleri gerekir.");
        Arac? alanlar = null;
        if (guncelAlanlar != null)
        {
            if (guncelAlanlar.Id != aracId || guncelAlanlar.FirmaId != targetFirmaId || string.IsNullOrWhiteSpace(guncelAlanlar.SaseNo))
                throw new ArgumentException("Taşıma ve form araç/firma kimlikleri eşleşmelidir.", nameof(guncelAlanlar));
            if (new[] { guncelAlanlar.KiralikCariId, guncelAlanlar.KomisyoncuCariId }.Any(id => id.HasValue && id.Value <= 0))
                throw new ArgumentException("Kira/komisyon cari kimlikleri pozitif veya boş olmalıdır.", nameof(guncelAlanlar));
            alanlar = new Arac { Id = aracId };
            AracService.GuncelleAracAlanlari(alanlar, guncelAlanlar, targetFirmaId);
        }
        selectedKeys = selectedKeys.Distinct(StringComparer.Ordinal).ToList();
        var kaynakFirmaId = _firmaProvider.AktifFirmaId;
        if (_firmaProvider.TumFirmalar || kaynakFirmaId is not > 0)
            throw new InvalidOperationException("Taşıma için aracın kaynak firmasını seçin.");
        long secimSurumu = 0;
        void FirmaDegisimi() => System.Threading.Interlocked.Increment(ref secimSurumu);
        void SecimiDogrula()
        {
            if (System.Threading.Volatile.Read(ref secimSurumu) != 0 || _firmaProvider.TumFirmalar || _firmaProvider.AktifFirmaId != kaynakFirmaId)
                throw new InvalidOperationException("Kaynak firma seçimi değişti; taşımayı yeniden başlatın.");
        }
        _firmaProvider.AktifFirmaDegisti += FirmaDegisimi;
        try
        {
            SecimiDogrula();
            await using var db = await _dbFactory.CreateDbContextAsync();
            SecimiDogrula();
            var principal = _httpContextAccessor.HttpContext is { } http ? http.User : (await _auth.GetAuthenticationStateAsync()).User;
            if (principal.Identity?.IsAuthenticated != true ||
                !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("KullaniciId")?.Value, out var userId) || userId <= 0 ||
                !await db.Kullanicilar.AnyAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted && !k.Rol.IsDeleted && k.Rol.RolAdi == SistemRolleri.Admin))
                throw new UnauthorizedAccessException("Araç firma taşıması için aktif Admin yetkisi gerekir.");
            var arac = await db.Araclar.AsTracking().FirstOrDefaultAsync(a => a.Id == aracId && a.FirmaId == kaynakFirmaId && !a.IsDeleted)
                ?? throw new InvalidOperationException("Araç kaynak firmada bulunamadı veya erişilebilir değil.");
            if (!await db.Firmalar.IgnoreQueryFilters().AnyAsync(f => f.Id == targetFirmaId && !f.IsDeleted))
                throw new InvalidOperationException("Hedef firma bulunamadı.");
            if (targetFirmaId == kaynakFirmaId) throw new InvalidOperationException("Kaynak ve hedef firma aynı olamaz.");
            foreach (var cariId in new[] { (alanlar ?? arac).KiralikCariId, (alanlar ?? arac).KomisyoncuCariId })
                if (cariId.HasValue && (cariId.Value <= 0 || !await db.Cariler.IgnoreQueryFilters().AnyAsync(c => c.Id == cariId.Value && c.FirmaId == targetFirmaId && !c.IsDeleted)))
                    throw new InvalidOperationException("Taşıma öncesinde kira/komisyon cari bağlantıları hedef firmaya uygun olarak düzenlenmelidir.");
            if (selectedKeys.Any(k => k is not ("AracEvrak" or "PuantajKayit" or "ServisCalisma" or "KiralikPlakaTakip")))
                throw new ArgumentException("Geçersiz taşıma seçimi.", nameof(selectedKeys));
            if (alanlar != null && await db.Araclar.IgnoreQueryFilters().AnyAsync(a => a.Id != aracId && !a.IsDeleted && a.SaseNo.ToUpper() == alanlar.SaseNo))
                throw new InvalidOperationException("Şase numarası kullanımda.");
            SecimiDogrula();
            int moved = 0;
            var errors = new List<string>();

            // Evrak ve dosyalar bağımsız tenant alanı taşır; aracı izleyen bu kayıtlar birlikte taşınmalıdır.
            var evraklar = await db.AracEvraklari.IgnoreQueryFilters().AsTracking()
                .Where(e => e.AracId == aracId && !e.IsDeleted).ToListAsync();
            var dosyalar = await db.AracEvrakDosyalari.IgnoreQueryFilters().AsTracking()
                .Where(d => !d.IsDeleted && db.AracEvraklari.IgnoreQueryFilters()
                    .Any(e => e.Id == d.AracEvrakId && e.AracId == aracId && !e.IsDeleted)).ToListAsync();
            if (evraklar.Any(e => e.FirmaId != kaynakFirmaId) || dosyalar.Any(d => d.FirmaId != kaynakFirmaId))
                throw new InvalidOperationException("Araç evrak/dosya firma bağlantıları tutarsız; taşıma öncesinde düzeltilmelidir.");
            if (evraklar.Count > 0 && !selectedKeys.Contains("AracEvrak"))
                throw new InvalidOperationException("Araçla birlikte evrakların da taşınması gerekir; Evraklar seçimini işaretleyin.");
            foreach (var evrak in evraklar) { evrak.FirmaId = targetFirmaId; evrak.UpdatedAt = DateTime.UtcNow; }
            foreach (var dosya in dosyalar) { dosya.FirmaId = targetFirmaId; dosya.UpdatedAt = DateTime.UtcNow; }
            moved += evraklar.Count + dosyalar.Count;

            if (selectedKeys.Contains("PuantajKayit"))
            {
                var puantajlar = await db.PuantajKayitlar.IgnoreQueryFilters().AsTracking()
                    .Where(p => p.AracId == aracId && !p.IsDeleted).ToListAsync();
                if (puantajlar.Any(p => p.IsverenFirmaId != kaynakFirmaId))
                    throw new InvalidOperationException("Puantaj kaynak firma bağlantıları tutarsız; taşıma öncesinde düzeltilmelidir.");
                foreach (var p in puantajlar)
                {
                    foreach (var cariId in new[] { p.KurumCariId, p.OdemeYapilacakCariId, p.FaturaKesiciCariId })
                        if (cariId.HasValue && (cariId.Value <= 0 || !await db.Cariler.IgnoreQueryFilters()
                            .AnyAsync(c => c.Id == cariId.Value && c.FirmaId == targetFirmaId && !c.IsDeleted)))
                            throw new InvalidOperationException("Puantaj cari bağlantıları hedef firmaya uygun olmalıdır.");
                    if (p.KurumId.HasValue && !await db.Kurumlar.IgnoreQueryFilters()
                        .AnyAsync(k => k.Id == p.KurumId.Value && k.FirmaId == targetFirmaId && !k.IsDeleted))
                        throw new InvalidOperationException("Puantaj kurum bağlantısı hedef firmaya uygun olmalıdır.");
                    if (p.GuzergahId.HasValue && !await db.Guzergahlar.IgnoreQueryFilters()
                        .AnyAsync(g => g.Id == p.GuzergahId.Value && g.FirmaId == targetFirmaId && !g.IsDeleted))
                        throw new InvalidOperationException("Puantaj güzergâh bağlantısı hedef firmaya uygun olmalıdır.");
                    if (p.SoforId.HasValue && !await db.Soforler.IgnoreQueryFilters()
                        .AnyAsync(g => g.Id == p.SoforId.Value && g.FirmaId == targetFirmaId && !g.IsDeleted))
                        throw new InvalidOperationException("Puantaj şoför bağlantısı hedef firmaya uygun olmalıdır.");
                    if (p.GelirFaturaId.HasValue || p.GiderFaturaId.HasValue || p.HesapDonemiId.HasValue || p.OncekiVersiyonId.HasValue)
                        throw new InvalidOperationException("Fatura, hesap dönemi veya önceki sürüm bağlantılı puantajlar bu akışta taşınamaz; bağlantıların ayrıca eşlenmesi gerekir.");
                }
                foreach (var p in puantajlar) { p.IsverenFirmaId = targetFirmaId; p.UpdatedAt = DateTime.UtcNow; }
                moved += puantajlar.Count;
            }

            if (selectedKeys.Contains("ServisCalisma"))
            {
                var servisler = await db.ServisCalismalari.IgnoreQueryFilters().AsTracking()
                    .Where(s => s.AracId == aracId && !s.IsDeleted).ToListAsync();
                if (servisler.Any(s => s.FirmaId != kaynakFirmaId))
                    throw new InvalidOperationException("Servis çalışma kaynak firma bağlantıları tutarsız; taşıma öncesinde düzeltilmelidir.");
                foreach (var servis in servisler)
                {
                    if (!await db.Guzergahlar.IgnoreQueryFilters().AnyAsync(g => g.Id == servis.GuzergahId && g.FirmaId == targetFirmaId && !g.IsDeleted) ||
                        !await db.Soforler.IgnoreQueryFilters().AnyAsync(g => g.Id == servis.SoforId && g.FirmaId == targetFirmaId && !g.IsDeleted))
                        throw new InvalidOperationException("Servis çalışma güzergâh ve şoför bağlantıları hedef firmaya uygun olmalıdır.");
                    if (await db.AracMasraflari.IgnoreQueryFilters().AnyAsync(m => m.ServisCalismaId == servis.Id && !m.IsDeleted))
                        throw new InvalidOperationException("Masraf bağlantılı servis çalışmaları bu akışta taşınamaz; masraf bağlantılarının ayrıca eşlenmesi gerekir.");
                }
                foreach (var servis in servisler) { servis.FirmaId = targetFirmaId; servis.UpdatedAt = DateTime.UtcNow; }
                moved += servisler.Count;
            }

            if (selectedKeys.Contains("KiralikPlakaTakip"))
            {
                try
                {
                    var takipler = await db.KiralikPlakaTakipler.IgnoreQueryFilters().Where(k => k.AracId == aracId && !k.IsDeleted).ToListAsync();
                    foreach (var t in takipler) { /* KiralikPlakaTakip'te FirmaId yok, skip */ }
                }
                catch (Exception ex) { errors.Add($"Plaka Takip: {ex.Message}"); }
            }

            if (errors.Any()) return (0, errors); // Hazırlık hatasında tracked değişiklikler kaydedilmez.
            SecimiDogrula();
            if (arac != null)
            {
                // Form alanları ve taşıma izi, ilişkilerle birlikte tek kayıtta hazırlanır.
                var oncekiFirmaId = arac.FirmaId;
                if (alanlar != null) AracService.GuncelleAracAlanlari(arac, alanlar, targetFirmaId);
                arac.KaynakFirmaId = oncekiFirmaId;
                arac.KaynakKayitId = arac.Id;
                arac.FirmaId = targetFirmaId;
                arac.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                errors.Add("Araç kaydı bulunamadı; firma değişikliği uygulanamadı.");
            }

            if (!await db.Kullanicilar.AnyAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted && !k.Rol.IsDeleted && k.Rol.RolAdi == SistemRolleri.Admin))
                throw new UnauthorizedAccessException("Taşıma yetkisi artık geçerli değil.");
            SecimiDogrula();
            // SaveChanges iş ve audit değişikliklerini aynı transaction'da kaydeder.
            await db.SaveChangesAsync();

            // Arac listeleri firma bazli cache'leniyor; tasima sonrasi eski firmada
            // gorunmemesi icin tum arac cache'leri temizlenir.
            try { await _cache.RemoveByPrefixAsync(Interfaces.CacheKeys.AracPrefix); }
            catch (Exception ex) { _logger.LogWarning(ex, "Araç taşıması kaydedildi ancak önbellek temizlenemedi. Araç: {AracId}", aracId); }

            return (moved, errors);
        }
        finally { _firmaProvider.AktifFirmaDegisti -= FirmaDegisimi; }
    }
}

public class TransferableItem
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool Required { get; set; }
    public bool Selected { get; set; }
}


