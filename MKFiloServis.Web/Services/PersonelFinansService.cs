using System.Security.Cryptography;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace MKFiloServis.Web.Services;

public class PersonelFinansService : IPersonelFinansService
{
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly CurrentPermissionGuard _permissionGuard;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;

    public PersonelFinansService(IDbContextFactory<ApplicationDbContext> contextFactory, IMuhasebeService muhasebeService, CurrentPermissionGuard permissionGuard, IAktifFirmaProvider aktifFirmaProvider)
    {
        _aktifFirmaProvider = aktifFirmaProvider;
        _permissionGuard = permissionGuard;
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
    }

    private async Task SoftDeleteDraftPostingAsync(ApplicationDbContext context, int fisId)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MuhasebeFisleriSil);
        var fis = await context.MuhasebeFisleri.FirstOrDefaultAsync(f => f.Id == fisId)
            ?? throw new InvalidOperationException("Bağlı muhasebe fişi aktif firma kapsamında bulunamadı.");
        if (fis.Durum != FisDurum.Taslak)
            throw new InvalidOperationException("Onaylı veya iptal edilmiş muhasebe fişi silinemez; ters kayıt akışını kullanın.");
        fis.IsDeleted = true;
        fis.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<TResult> ExecuteFinanceWriteAsync<TResult>(string permission,
        Func<ApplicationDbContext, Task<TResult>> write, Action resetInput,
        Func<ApplicationDbContext, TResult, Task<bool>> verifyCommitted)
        where TResult : BaseEntity
    {
        await using var strategyContext = await _contextFactory.CreateDbContextAsync();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        var commitStarted = false;
        var writeCompleted = false;
        TResult attemptedResult = default!;
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                // Commit hatası geçici olsa bile aynı mali yazımı yeniden yürütme.
                if (commitStarted)
                    throw new InvalidOperationException("Önceki commit sonucu doğrulanmalıdır.");
                resetInput();
                await _permissionGuard.RequireAnyAsync(permission);
                await using var context = await _contextFactory.CreateDbContextAsync();
                await using var transaction = await context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);
                attemptedResult = await write(context);
                writeCompleted = true;
                commitStarted = true;
                await transaction.CommitAsync();
                return attemptedResult;
            });
        }
        catch (Exception failure)
        {
            if (commitStarted && writeCompleted)
            {
                // Yeni context ile görünür kayıt doğrulanır; başarısız doğrulama yeniden yazma izni değildir.
                try
                {
                    await using var verificationContext = await _contextFactory.CreateDbContextAsync();
                    if (await verifyCommitted(verificationContext, attemptedResult))
                        return attemptedResult;
                }
                catch
                {
                    // Depo hâlâ erişilemiyorsa sonucu bilinmiyor olarak bildir.
                }
                // Üretilmiş kimliği koru: aynı nesne tekrar oluşturma çağrısına verilemez.
                throw new InvalidOperationException(
                    $"İşlemin kaydedilip kaydedilmediği doğrulanamadı (kayıt #{attemptedResult.Id}). Aynı işlemi yeniden göndermeyin; listeyi yenileyip bu kaydı kontrol edin.", failure);
            }
            resetInput();
            throw;
        }
    }

    private static string NormalizeOperationKey(string? key)
    {
        if (!Guid.TryParse(key, out var operationId) || operationId == Guid.Empty)
            throw new InvalidOperationException("Geçerli bir işlem kimliği gereklidir.");
        return operationId.ToString("N");
    }

    private static string OperationFingerprint<T>(T request)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request)));

    private static async Task<T> ReplayFinanceWriteAsync<T>(Func<Task<T>> write, Func<Task<T?>> lookup)
        where T : BaseEntity
    {
        var previous = await lookup();
        if (previous != null) return previous;
        try
        {
            return await write();
        }
        catch
        {
            // Benzersiz indeks, başka sunucunun kazandığı yarışı da engeller.
            // Yalnız eşleşen kalıcı kayıt başarı sayılır; lookup hata verirse başarı varsayılmaz.
            previous = await lookup();
            if (previous != null) return previous;
            throw;
        }
    }

    public async Task<PersonelAvansMahsup> MahsupEtAvansAsync(int avansId, PersonelAvansMahsup mahsup)
    {
        mahsup.IslemKimligi = NormalizeOperationKey(mahsup.IslemKimligi);
        var fingerprint = OperationFingerprint(new { avansId, mahsup.MahsupTutari, mahsup.MahsupTarihi,
            mahsup.MahsupSekli, mahsup.MaasId, mahsup.BankaHesapId, mahsup.Aciklama });
        mahsup.IslemOzeti = fingerprint;
        async Task<PersonelAvansMahsup?> LookupAsync()
        {
            await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (!await context.PersonelAvanslar.AnyAsync(a => a.Id == avansId))
                throw new InvalidOperationException("Avans aktif firma kapsamında bulunamadı.");
            var previous = await context.PersonelAvansMahsuplar.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(m => m.AvansId == avansId && m.IslemKimligi == mahsup.IslemKimligi);
            if (previous == null) return null;
            if (previous.IsDeleted || previous.IslemOzeti != fingerprint)
                throw new InvalidOperationException("İşlem kimliği daha önce kaldırılmış veya farklı içerikli bir mahsup için kullanılmış.");
            return previous;
        }
        return await ReplayFinanceWriteAsync(() => MahsupEtAvansCoreAsync(avansId, mahsup), LookupAsync);
    }

    public async Task<PersonelBorcOdeme> OdemeYapBorcAsync(int borcId, PersonelBorcOdeme odeme, bool muhasebeKaydiOlustur = true)
    {
        odeme.IslemKimligi = NormalizeOperationKey(odeme.IslemKimligi);
        var fingerprint = OperationFingerprint(new { borcId, odeme.OdemeTutari, odeme.OdemeTarihi,
            odeme.OdemeSekli, odeme.BankaHesapId, odeme.Aciklama, muhasebeKaydiOlustur });
        odeme.IslemOzeti = fingerprint;
        async Task<PersonelBorcOdeme?> LookupAsync()
        {
            await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (!await context.PersonelBorclar.AnyAsync(b => b.Id == borcId))
                throw new InvalidOperationException("Borç aktif firma kapsamında bulunamadı.");
            var previous = await context.PersonelBorcOdemeler.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(o => o.BorcId == borcId && o.IslemKimligi == odeme.IslemKimligi);
            if (previous == null) return null;
            if (previous.IsDeleted || previous.IslemOzeti != fingerprint)
                throw new InvalidOperationException("İşlem kimliği daha önce kaldırılmış veya farklı içerikli bir ödeme için kullanılmış.");
            return previous;
        }
        return await ReplayFinanceWriteAsync(() => OdemeYapBorcCoreAsync(borcId, odeme, muhasebeKaydiOlustur), LookupAsync);
    }

    private int ResolveCreationFirma(int? requestedFirmaId)
    {
        var activeFirmaId = _aktifFirmaProvider.AktifFirmaId;
        if (_aktifFirmaProvider.TumFirmalar || activeFirmaId is null or <= 0)
            throw new InvalidOperationException("Yeni personel finans kaydı için tek bir firma seçin.");
        if (requestedFirmaId.HasValue && requestedFirmaId.Value != activeFirmaId.Value)
            throw new UnauthorizedAccessException("Kayıt firması seçili firmayla eşleşmiyor.");
        return activeFirmaId.Value;
    }

    public async Task<PersonelAvans> CreateAvansAsync(PersonelAvans avans, bool muhasebeKaydiOlustur = true)
    {
        avans.FirmaId = ResolveCreationFirma(avans.FirmaId);
        avans.IslemKimligi = NormalizeOperationKey(avans.IslemKimligi);
        var fingerprint = OperationFingerprint(new { avans.FirmaId, avans.PersonelId, avans.AvansTarihi,
            avans.Tutar, avans.OdemeSekli, avans.BankaHesapId, avans.Aciklama, muhasebeKaydiOlustur });
        avans.IslemOzeti = fingerprint;
        async Task<PersonelAvans?> LookupAsync()
        {
            await _permissionGuard.RequireAnyAsync(Yetkiler.MaasYaz);
            var firmaId = ResolveCreationFirma(avans.FirmaId);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var previous = await context.PersonelAvanslar.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(a => a.FirmaId == firmaId && a.IslemKimligi == avans.IslemKimligi);
            if (previous == null) return null;
            if (previous.IsDeleted || previous.Durum == AvansDurum.IptalEdildi || previous.IslemOzeti != fingerprint)
                throw new InvalidOperationException("İşlem kimliği kaldırılmış, iptal edilmiş veya farklı içerikli bir avans için kullanılmış.");
            return previous;
        }
        return await ReplayFinanceWriteAsync(() => CreateAvansCoreAsync(avans, muhasebeKaydiOlustur), LookupAsync);
    }

    public async Task<PersonelBorc> CreateBorcAsync(PersonelBorc borc, bool muhasebeKaydiOlustur = true)
    {
        borc.FirmaId = ResolveCreationFirma(borc.FirmaId);
        borc.IslemKimligi = NormalizeOperationKey(borc.IslemKimligi);
        var fingerprint = OperationFingerprint(new { borc.FirmaId, borc.PersonelId, borc.BorcTarihi, borc.Tutar,
            borc.BorcNedeni, borc.BorcTipi, borc.PlanlananOdemeTarihi, borc.Aciklama, muhasebeKaydiOlustur });
        borc.IslemOzeti = fingerprint;
        async Task<PersonelBorc?> LookupAsync()
        {
            await _permissionGuard.RequireAnyAsync(Yetkiler.MaasYaz);
            var firmaId = ResolveCreationFirma(borc.FirmaId);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var previous = await context.PersonelBorclar.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(b => b.FirmaId == firmaId && b.IslemKimligi == borc.IslemKimligi);
            if (previous == null) return null;
            if (previous.IsDeleted || previous.OdemeDurum == BorcOdemeDurum.IptalEdildi || previous.IslemOzeti != fingerprint)
                throw new InvalidOperationException("İşlem kimliği kaldırılmış, iptal edilmiş veya farklı içerikli bir borç için kullanılmış.");
            return previous;
        }
        return await ReplayFinanceWriteAsync(() => CreateBorcCoreAsync(borc, muhasebeKaydiOlustur), LookupAsync);
    }

    #region Avans İşlemleri

    public async Task<List<PersonelAvans>> GetAvanslarAsync(int? firmaId = null, int? personelId = null, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<PersonelAvans>()
            .Include(a => a.Personel)
            .Include(a => a.Firma)
            .Include(a => a.BankaHesap)
            .Include(a => a.Mahsuplasmalar)
            .AsNoTracking()
            .AsQueryable();

        if (firmaId.HasValue)
            query = query.Where(a => a.FirmaId == firmaId);

        if (personelId.HasValue)
            query = query.Where(a => a.PersonelId == personelId);

        if (baslangic.HasValue)
            query = query.Where(a => a.AvansTarihi >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(a => a.AvansTarihi <= bitis.Value);

        return await query.OrderByDescending(a => a.AvansTarihi).ToListAsync();
    }

    public async Task<PersonelAvans?> GetAvansByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<PersonelAvans>()
            .Include(a => a.Personel)
            .Include(a => a.Firma)
            .Include(a => a.BankaHesap)
            .Include(a => a.Mahsuplasmalar)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    private async Task<PersonelAvans> CreateAvansCoreAsync(PersonelAvans avans, bool muhasebeKaydiOlustur)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasYaz);
        if (avans.Tutar <= 0)
            throw new InvalidOperationException("Avans/borç tutarı sıfırdan büyük olmalıdır.");
        if (avans.Id != 0)
            throw new InvalidOperationException("Kimliği bulunan kayıt yeniden oluşturulamaz; mevcut kaydı listeden kontrol edin.");
        var originalId = avans.Id;
        var originalFisId = avans.MuhasebeFisId;
        var originalFis = avans.MuhasebeFis;
        return await ExecuteFinanceWriteAsync(Yetkiler.MaasYaz, async context =>
        {
            ResolveCreationFirma(avans.FirmaId);
            avans.Durum = AvansDurum.Verildi;
            avans.MahsupEdilen = 0;
            avans.CreatedAt = DateTime.UtcNow;

            context.Set<PersonelAvans>().Add(avans);
            await context.SaveChangesAsync();

            // Muhasebe kaydı oluştur
            if (muhasebeKaydiOlustur)
            {
                await CreateAvansMuhasebeFisiAsync(context, avans);
            }

            return avans;
        }, () =>
        {
            avans.Id = originalId;
            avans.MuhasebeFisId = originalFisId;
            avans.MuhasebeFis = originalFis;
        }, async (verificationContext, result) =>
            await verificationContext.Set<PersonelAvans>().AsNoTracking()
                .AnyAsync(e => e.Id == result.Id && e.CreatedAt == result.CreatedAt && e.PersonelId == result.PersonelId && e.FirmaId == result.FirmaId && e.Tutar == result.Tutar && e.MuhasebeFisId == result.MuhasebeFisId));
    }

    public async Task<PersonelAvans> UpdateAvansAsync(PersonelAvans avans)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        if (avans.Tutar <= 0)
            throw new InvalidOperationException("Avans/borç tutarı sıfırdan büyük olmalıdır.");
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Set<PersonelAvans>().FindAsync(avans.Id);
        if (existing == null)
            throw new InvalidOperationException($"Avans bulunamadı. Id: {avans.Id}");

        if (existing.Durum == AvansDurum.IptalEdildi)
            throw new InvalidOperationException("İptal edilmiş avans düzenlenemez.");
        if (avans.Tutar < existing.MahsupEdilen)
            throw new InvalidOperationException("Avans tutarı mahsup edilmiş tutardan küçük olamaz.");
        var maliAlanDegisti = existing.Tutar != avans.Tutar || existing.AvansTarihi != avans.AvansTarihi
            || existing.OdemeSekli != avans.OdemeSekli || existing.BankaHesapId != avans.BankaHesapId;
        if (maliAlanDegisti && (existing.MuhasebeFisId.HasValue || existing.MahsupEdilen > 0))
            throw new InvalidOperationException("Muhasebe fişi veya mahsup geçmişi bulunan avansın mali alanları değiştirilemez. Düzeltme için kayıt/fiş geri alma veya ters kayıt işlemi gerekir.");

        existing.AvansTarihi = avans.AvansTarihi;
        existing.Tutar = avans.Tutar;
        existing.Aciklama = avans.Aciklama;
        existing.OdemeSekli = avans.OdemeSekli;
        existing.BankaHesapId = avans.BankaHesapId;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAvansAsync(int id)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasSil);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var avans = await context.Set<PersonelAvans>()
            .Include(a => a.Mahsuplasmalar)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (avans == null)
            throw new InvalidOperationException($"Avans bulunamadı. Id: {id}");

        if (avans.Mahsuplasmalar.Any())
            throw new InvalidOperationException("Mahsuplaşması olan avans silinemez!");

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            if (avans.MuhasebeFisId.HasValue)
            {
                await SoftDeleteDraftPostingAsync(context, avans.MuhasebeFisId.Value);
            }

            avans.IsDeleted = true;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<PersonelAvans> IptalEtAvansAsync(int id, string iptalNedeni)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var avans = await context.Set<PersonelAvans>()
            .Include(e => e.Mahsuplasmalar)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (avans == null)
            throw new InvalidOperationException($"Avans bulunamadı. Id: {id}");

        if (avans.Durum == AvansDurum.IptalEdildi)
            return avans;
        if (avans.Mahsuplasmalar.Any() || avans.MahsupEdilen > 0)
            throw new InvalidOperationException("Mahsup geçmişi bulunan avans iptal edilemez; önce mahsup kayıtlarını geri alın.");
        if (avans.MuhasebeFisId.HasValue)
            await SoftDeleteDraftPostingAsync(context, avans.MuhasebeFisId.Value);

        avans.Durum = AvansDurum.IptalEdildi;
        avans.Aciklama = (avans.Aciklama ?? "") + $" [İPTAL: {iptalNedeni}]";
        avans.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return avans;
    }

    #endregion

    #region Avans Mahsup

    private async Task<PersonelAvansMahsup> MahsupEtAvansCoreAsync(int avansId, PersonelAvansMahsup mahsup)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        if (mahsup.MahsupTutari <= 0)
            throw new InvalidOperationException("Ödeme/mahsup tutarı sıfırdan büyük olmalıdır.");
        if (mahsup.Id != 0)
            throw new InvalidOperationException("Kimliği bulunan kayıt yeniden oluşturulamaz; mevcut kaydı listeden kontrol edin.");
        var originalId = mahsup.Id;
        return await ExecuteFinanceWriteAsync(Yetkiler.MaasDuzenle, async context =>
        {
            var avans = await context.Set<PersonelAvans>().FindAsync(avansId);
            if (avans == null)
                throw new InvalidOperationException($"Avans bulunamadı. Id: {avansId}");

            if (avans.Durum == AvansDurum.IptalEdildi)
                throw new InvalidOperationException("İptal edilmiş avans mahsup edilemez.");

            if (mahsup.MahsupTutari > avans.Kalan)
                throw new InvalidOperationException("Mahsup tutarı kalan avanstan fazla olamaz!");

            if (mahsup.MaasId.HasValue)
            {
                var maas = await context.PersonelMaaslari.FirstOrDefaultAsync(m => m.Id == mahsup.MaasId.Value)
                    ?? throw new InvalidOperationException("Maaş aktif firma kapsamında bulunamadı.");
                if (mahsup.MahsupSekli != MahsupSekli.MaastanKesinti || maas.SoforId != avans.PersonelId || maas.FirmaId != avans.FirmaId)
                    throw new InvalidOperationException("Mahsup, aynı firma ve personelin maaş kesintisi olmalıdır.");
                if (maas.OdemeDurum == MaasOdemeDurum.Odendi || mahsup.MahsupTutari > maas.OdenecekTutar)
                    throw new InvalidOperationException("Ödenmiş veya yeterli ödeme tutarı olmayan maaşa mahsup uygulanamaz.");
                maas.Avans += mahsup.MahsupTutari;
                maas.UpdatedAt = DateTime.UtcNow;
            }

            mahsup.AvansId = avansId;
            mahsup.CreatedAt = DateTime.UtcNow;

            mahsup.Avans = avans;
            context.Set<PersonelAvansMahsup>().Add(mahsup);

            // Avans mahsup bilgisini güncelle
            avans.MahsupEdilen += mahsup.MahsupTutari;
            if (avans.Kalan <= 0)
            {
                avans.Durum = AvansDurum.TamamenMahsup;
                avans.MahsupTarihi = mahsup.MahsupTarihi;
            }
            else
            {
                avans.Durum = AvansDurum.KismenMahsup;
            }
            avans.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Muhasebe kaydı oluştur
            var avansWithPersonel = await context.Set<PersonelAvans>()
                .Include(a => a.Personel)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == avansId);
            if (avansWithPersonel != null)
                await CreateMahsupMuhasebeFisiAsync(context, mahsup, avansWithPersonel);

            return mahsup;
        }, () =>
        {
            mahsup.Id = originalId;
        }, async (verificationContext, result) =>
            await verificationContext.Set<PersonelAvansMahsup>().AsNoTracking()
                .AnyAsync(e => e.Id == result.Id && e.CreatedAt == result.CreatedAt && e.AvansId == result.AvansId && e.MahsupTutari == result.MahsupTutari));
    }

    public async Task<List<PersonelAvansMahsup>> GetAvansMahsuplasmalarAsync(int avansId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<PersonelAvansMahsup>()
            .Include(m => m.BankaHesap)
            .Include(m => m.Maas)
            .Where(m => m.AvansId == avansId)
            .OrderByDescending(m => m.MahsupTarihi)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<decimal> MaasaAcikAvansMahsupEtAsync(int maasId, DateTime? mahsupTarihi = null, string? aciklama = null)
    {
        // Bir maaş için tek otomatik parti. Tarih/açıklama değiştirmek yeni parti açmaz.
        var batchSummary = OperationFingerprint(new { Operation = "AutomaticPayrollAdvance:v1", maasId });
        var toplamMahsup = 0m;
        async Task<PersonelMaas?> LookupAsync()
        {
            await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
            await using var lookup = await _contextFactory.CreateDbContextAsync();
            var salary = await lookup.PersonelMaaslari.AsNoTracking().FirstOrDefaultAsync(m => m.Id == maasId)
                ?? throw new InvalidOperationException("Maaş aktif firma kapsamında bulunamadı.");
            var previous = await lookup.PersonelAvansMahsuplar.IgnoreQueryFilters().AsNoTracking()
                .Where(m => m.MaasId == salary.Id && m.IslemOzeti == batchSummary).ToListAsync();
            if (previous.Count == 0) return null;
            if (previous.Any(m => m.IsDeleted))
                throw new InvalidOperationException("Otomatik mahsup partisi geri alınmış. Yeni kesinti gerekiyorsa tekil mahsup işlemini kullanın.");
            toplamMahsup = previous.Sum(m => m.MahsupTutari);
            return salary;
        }

        await ReplayFinanceWriteAsync(() => ExecuteFinanceWriteAsync(Yetkiler.MaasDuzenle, async context =>
        {
            var maas = await context.PersonelMaaslari
                .FirstOrDefaultAsync(m => m.Id == maasId && !m.IsDeleted);

            if (maas == null)
                throw new InvalidOperationException($"Maaş kaydı bulunamadı. Id: {maasId}");

            if (maas.OdemeDurum == MaasOdemeDurum.Odendi)
                throw new InvalidOperationException("Ödenmiş maaşa mahsup uygulanamaz.");

            var previousBatch = await context.PersonelAvansMahsuplar.IgnoreQueryFilters()
                .Where(m => m.MaasId == maas.Id && m.IslemOzeti == batchSummary).ToListAsync();
            if (previousBatch.Count > 0)
            {
                if (previousBatch.Any(m => m.IsDeleted))
                    throw new InvalidOperationException("Geri alınmış otomatik mahsup yeniden uygulanamaz; tekil mahsup kullanın.");
                toplamMahsup = previousBatch.Sum(m => m.MahsupTutari);
                return maas;
            }

            var mahsupEdilebilirTutar = Math.Max(0, maas.OdenecekTutar);
            if (await context.PersonelAvansMahsuplar.IgnoreQueryFilters().AnyAsync(m => m.MaasId == maas.Id))
                throw new InvalidOperationException("Bu maaşta önceki mahsup geçmişi var; ikinci otomatik parti yerine tekil mahsup kullanın.");
            if (mahsupEdilebilirTutar <= 0)
                throw new InvalidOperationException("Maaş üzerinde mahsup edilebilecek tutar bulunmuyor.");

            var acikAvanslar = await context.Set<PersonelAvans>()
                .Where(a => !a.IsDeleted &&
                            a.PersonelId == maas.SoforId &&
                            a.FirmaId == maas.FirmaId &&
                            a.Durum != AvansDurum.IptalEdildi &&
                            a.MahsupEdilen < a.Tutar)
                .OrderBy(a => a.AvansTarihi)
                .ThenBy(a => a.Id)
                .ToListAsync();

            if (!acikAvanslar.Any())
                throw new InvalidOperationException("Mahsup edilecek açık avans bulunamadı.");

            toplamMahsup = 0m;
            var islemTarihi = mahsupTarihi?.Date ?? MKFiloServis.Shared.Time.BusinessTime.Today;

            foreach (var avans in acikAvanslar)
            {
                var kalanKapasite = mahsupEdilebilirTutar - toplamMahsup;
                if (kalanKapasite <= 0)
                    break;

                var mahsupTutari = Math.Min(avans.Kalan, kalanKapasite);
                if (mahsupTutari <= 0)
                    continue;

                var mahsup = new PersonelAvansMahsup
                {
                    AvansId = avans.Id,
                    IslemKimligi = OperationFingerprint(new { batchSummary, avans.Id })[..32].ToLowerInvariant(),
                    IslemOzeti = batchSummary,
                    MaasId = maas.Id,
                    MahsupTarihi = islemTarihi,
                    MahsupTutari = mahsupTutari,
                    Aciklama = aciklama,
                    MahsupSekli = MahsupSekli.MaastanKesinti,
                    CreatedAt = DateTime.UtcNow
                };

                context.Set<PersonelAvansMahsup>().Add(mahsup);

                avans.MahsupEdilen += mahsupTutari;
                avans.MahsupTarihi = islemTarihi;
                avans.MahsupAciklamasi = aciklama;
                avans.Durum = avans.Kalan <= 0 ? AvansDurum.TamamenMahsup : AvansDurum.KismenMahsup;
                avans.UpdatedAt = DateTime.UtcNow;

                toplamMahsup += mahsupTutari;
            }

            if (toplamMahsup <= 0)
                throw new InvalidOperationException("Maaşa uygulanabilecek avans mahsubu bulunamadı.");

            maas.Avans += toplamMahsup;
            maas.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(aciklama))
            {
                var yeniNot = $"{islemTarihi:dd.MM.yyyy} maaş mahsubu: {toplamMahsup:N2} ₺ - {aciklama}";
                maas.Notlar = string.IsNullOrWhiteSpace(maas.Notlar)
                    ? yeniNot
                    : $"{maas.Notlar}{Environment.NewLine}{yeniNot}";
            }

            await context.SaveChangesAsync();
            return maas;
        }, () => toplamMahsup = 0m, async (verification, result) =>
            await verification.PersonelAvansMahsuplar.AsNoTracking()
                .AnyAsync(m => m.MaasId == result.Id && m.IslemOzeti == batchSummary)), LookupAsync);
        return toplamMahsup;
    }

    public async Task DeleteMahsupAsync(int mahsupId)
    {
        await ExecuteFinanceWriteAsync(Yetkiler.MaasSil, async context =>
        {
            var mahsup = await context.Set<PersonelAvansMahsup>()
                .Include(m => m.Avans)
                .FirstOrDefaultAsync(m => m.Id == mahsupId);

            if (mahsup == null)
                throw new InvalidOperationException($"Mahsup kaydı bulunamadı. Id: {mahsupId}");

            if (mahsup.MaasId.HasValue)
            {
                var maas = await context.PersonelMaaslari.FirstOrDefaultAsync(m => m.Id == mahsup.MaasId.Value)
                    ?? throw new InvalidOperationException("Bağlı maaş aktif firma kapsamında bulunamadı.");
                if (maas.OdemeDurum == MaasOdemeDurum.Odendi)
                    throw new InvalidOperationException("Ödenmiş maaşın mahsubu kaldırılamaz; ters kayıt akışını kullanın.");
                if (maas.SoforId != mahsup.Avans.PersonelId || maas.FirmaId != mahsup.Avans.FirmaId || maas.Avans < mahsup.MahsupTutari)
                    throw new InvalidOperationException("Maaş kesintisi ve mahsup tutarsız; düzeltme yapılmadan geri alınamaz.");
                maas.Avans -= mahsup.MahsupTutari;
                maas.UpdatedAt = DateTime.UtcNow;
            }
            if (mahsup.Avans.MahsupEdilen < mahsup.MahsupTutari)
                throw new InvalidOperationException("Avans bakiyesi ve mahsup tutarsız; geri alma reddedildi.");

            // Mahsup kaydında fiş FK'sı yok; kaynak kimliğiyle bağlı taslakları bul.
            var fisIdleri = await context.MuhasebeFisleri
                .Where(f => f.KaynakTip == "PersonelAvansMahsup" && f.KaynakId == mahsup.Id)
                .Select(f => f.Id).ToListAsync();
            foreach (var fisId in fisIdleri)
                await SoftDeleteDraftPostingAsync(context, fisId);

            // Avans mahsup bilgisini güncelle
            var avans = mahsup.Avans;
            avans.MahsupEdilen -= mahsup.MahsupTutari;
            if (avans.MahsupEdilen <= 0)
            {
                avans.MahsupEdilen = 0;
                avans.Durum = AvansDurum.Verildi;
            }
            else
            {
                avans.Durum = AvansDurum.KismenMahsup;
            }
            avans.UpdatedAt = DateTime.UtcNow;

            mahsup.IsDeleted = true;
            mahsup.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return mahsup;
        }, () => { }, async (verification, result) =>
            await verification.PersonelAvansMahsuplar.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(m => m.Id == result.Id && m.IsDeleted && m.UpdatedAt == result.UpdatedAt));
    }

    #endregion

    #region Borç İşlemleri

    public async Task<List<PersonelBorc>> GetBorclarAsync(int? firmaId = null, int? personelId = null, BorcOdemeDurum? durum = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<PersonelBorc>()
            .Include(b => b.Personel)
            .Include(b => b.Firma)
            .Include(b => b.BankaHesap)
            .Include(b => b.Odemeler)
            .AsNoTracking()
            .AsQueryable();

        if (firmaId.HasValue)
            query = query.Where(b => b.FirmaId == firmaId);

        if (personelId.HasValue)
            query = query.Where(b => b.PersonelId == personelId);

        if (durum.HasValue)
            query = query.Where(b => b.OdemeDurum == durum);

        return await query.OrderByDescending(b => b.BorcTarihi).ToListAsync();
    }

    public async Task<PersonelBorc?> GetBorcByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<PersonelBorc>()
            .Include(b => b.Personel)
            .Include(b => b.Firma)
            .Include(b => b.BankaHesap)
            .Include(b => b.Odemeler)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    private async Task<PersonelBorc> CreateBorcCoreAsync(PersonelBorc borc, bool muhasebeKaydiOlustur)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasYaz);
        if (borc.Tutar <= 0)
            throw new InvalidOperationException("Avans/borç tutarı sıfırdan büyük olmalıdır.");
        if (borc.Id != 0)
            throw new InvalidOperationException("Kimliği bulunan kayıt yeniden oluşturulamaz; mevcut kaydı listeden kontrol edin.");
        var originalId = borc.Id;
        var originalFisId = borc.MuhasebeFisId;
        var originalFis = borc.MuhasebeFis;
        return await ExecuteFinanceWriteAsync(Yetkiler.MaasYaz, async context =>
        {
            ResolveCreationFirma(borc.FirmaId);
            borc.OdemeDurum = BorcOdemeDurum.Bekliyor;
            borc.OdenenTutar = 0;
            borc.CreatedAt = DateTime.UtcNow;

            context.Set<PersonelBorc>().Add(borc);
            await context.SaveChangesAsync();

            // Muhasebe kaydı oluştur
            if (muhasebeKaydiOlustur)
            {
                await CreateBorcMuhasebeFisiAsync(context, borc);
            }

            return borc;
        }, () =>
        {
            borc.Id = originalId;
            borc.MuhasebeFisId = originalFisId;
            borc.MuhasebeFis = originalFis;
        }, async (verificationContext, result) =>
            await verificationContext.Set<PersonelBorc>().AsNoTracking()
                .AnyAsync(e => e.Id == result.Id && e.CreatedAt == result.CreatedAt && e.PersonelId == result.PersonelId && e.FirmaId == result.FirmaId && e.Tutar == result.Tutar && e.MuhasebeFisId == result.MuhasebeFisId));
    }

    public async Task<PersonelBorc> UpdateBorcAsync(PersonelBorc borc)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        if (borc.Tutar <= 0)
            throw new InvalidOperationException("Avans/borç tutarı sıfırdan büyük olmalıdır.");
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Set<PersonelBorc>().FindAsync(borc.Id);
        if (existing == null)
            throw new InvalidOperationException($"Borç bulunamadı. Id: {borc.Id}");

        if (existing.OdemeDurum == BorcOdemeDurum.IptalEdildi)
            throw new InvalidOperationException("İptal edilmiş borç düzenlenemez.");
        if (borc.Tutar < existing.OdenenTutar)
            throw new InvalidOperationException("Borç tutarı ödenmiş tutardan küçük olamaz.");
        var maliAlanDegisti = existing.Tutar != borc.Tutar || existing.BorcTarihi != borc.BorcTarihi
            || existing.BorcTipi != borc.BorcTipi;
        if (maliAlanDegisti && (existing.MuhasebeFisId.HasValue || existing.OdenenTutar > 0))
            throw new InvalidOperationException("Muhasebe fişi veya ödeme geçmişi bulunan borcun mali alanları değiştirilemez. Düzeltme için kayıt/fiş geri alma veya ters kayıt işlemi gerekir.");

        existing.BorcTarihi = borc.BorcTarihi;
        existing.Tutar = borc.Tutar;
        existing.BorcNedeni = borc.BorcNedeni;
        existing.Aciklama = borc.Aciklama;
        existing.BorcTipi = borc.BorcTipi;
        existing.PlanlananOdemeTarihi = borc.PlanlananOdemeTarihi;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteBorcAsync(int id)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.PersonelBorcSil);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var borc = await context.Set<PersonelBorc>()
                .Include(b => b.Odemeler)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borc == null)
                throw new InvalidOperationException($"Borç bulunamadı. Id: {id}");

            // Silinmiş ödemelerin işlem anahtarları da tekrar gönderim korumasıdır.
            // Ana borcu fiziksel silmek cascade ile bu geçmişi kaybettirebilir.
            if (await context.PersonelBorcOdemeler.IgnoreQueryFilters()
                .AnyAsync(o => o.BorcId == borc.Id))
                throw new InvalidOperationException("Ödeme geçmişi bulunan borç kalıcı silinemez; ödeme ve işlem kimliği geçmişi korunmalıdır.");

            var fisIdleri = new HashSet<int>();
            if (borc.MuhasebeFisId.HasValue)
                fisIdleri.Add(borc.MuhasebeFisId.Value);

            foreach (var odeme in borc.Odemeler)
            {
                if (odeme.MuhasebeFisId.HasValue)
                    fisIdleri.Add(odeme.MuhasebeFisId.Value);
            }

            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Set<PersonelBorcOdeme>().RemoveRange(borc.Odemeler);
            if (borc.IslemKimligi != null)
            {
                borc.IsDeleted = true;
                borc.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Eski anahtarsız ve ödeme geçmişsiz kayıtta mevcut kalıcı kaldırma sözleşmesi.
                context.Set<PersonelBorc>().Remove(borc);
            }

            if (fisIdleri.Count > 0)
            {
                foreach (var fisId in fisIdleri)
                    await SoftDeleteDraftPostingAsync(context, fisId);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<PersonelBorc> IptalEtBorcAsync(int id, string iptalNedeni)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var borc = await context.Set<PersonelBorc>()
            .Include(e => e.Odemeler)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (borc == null)
            throw new InvalidOperationException($"Borç bulunamadı. Id: {id}");

        if (borc.OdemeDurum == BorcOdemeDurum.IptalEdildi)
            return borc;
        if (borc.Odemeler.Any() || borc.OdenenTutar > 0)
            throw new InvalidOperationException("Ödeme geçmişi bulunan borç iptal edilemez; önce ödeme kayıtlarını geri alın.");
        if (borc.MuhasebeFisId.HasValue)
            await SoftDeleteDraftPostingAsync(context, borc.MuhasebeFisId.Value);

        borc.OdemeDurum = BorcOdemeDurum.IptalEdildi;
        borc.Aciklama = (borc.Aciklama ?? "") + $" [İPTAL: {iptalNedeni}]";
        borc.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return borc;
    }

    #endregion

    #region Borç Ödeme

    private async Task<PersonelBorcOdeme> OdemeYapBorcCoreAsync(int borcId, PersonelBorcOdeme odeme, bool muhasebeKaydiOlustur)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        if (odeme.OdemeTutari <= 0)
            throw new InvalidOperationException("Ödeme/mahsup tutarı sıfırdan büyük olmalıdır.");
        if (odeme.Id != 0)
            throw new InvalidOperationException("Kimliği bulunan kayıt yeniden oluşturulamaz; mevcut kaydı listeden kontrol edin.");
        var originalId = odeme.Id;
        var originalFisId = odeme.MuhasebeFisId;
        var originalFis = odeme.MuhasebeFis;
        return await ExecuteFinanceWriteAsync(Yetkiler.MaasDuzenle, async context =>
        {
            var borc = await context.Set<PersonelBorc>().FindAsync(borcId);
            if (borc == null)
                throw new InvalidOperationException($"Borç bulunamadı. Id: {borcId}");

            if (borc.OdemeDurum == BorcOdemeDurum.IptalEdildi)
                throw new InvalidOperationException("İptal edilmiş borç için ödeme yapılamaz.");

            if (odeme.OdemeTutari > borc.KalanBorc)
                throw new InvalidOperationException("Ödeme tutarı kalan borçtan fazla olamaz!");

            odeme.BorcId = borcId;
            odeme.CreatedAt = DateTime.UtcNow;

            odeme.Borc = borc;
            context.Set<PersonelBorcOdeme>().Add(odeme);

            // Borç ödeme bilgisini güncelle
            borc.OdenenTutar += odeme.OdemeTutari;
            if (borc.KalanBorc <= 0)
            {
                borc.OdemeDurum = BorcOdemeDurum.TamamenOdendi;
                borc.GerceklesenOdemeTarihi = odeme.OdemeTarihi;
            }
            else
            {
                borc.OdemeDurum = BorcOdemeDurum.KismenOdendi;
            }
            borc.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Muhasebe kaydı oluştur
            if (muhasebeKaydiOlustur)
            {
                await CreateBorcOdemeMuhasebeFisiAsync(context, odeme, borc);
            }

            return odeme;
        }, () =>
        {
            odeme.Id = originalId;
            odeme.MuhasebeFisId = originalFisId;
            odeme.MuhasebeFis = originalFis;
        }, async (verificationContext, result) =>
            await verificationContext.Set<PersonelBorcOdeme>().AsNoTracking()
                .AnyAsync(e => e.Id == result.Id && e.CreatedAt == result.CreatedAt && e.BorcId == result.BorcId && e.OdemeTutari == result.OdemeTutari && e.MuhasebeFisId == result.MuhasebeFisId));
    }

    public async Task<List<PersonelBorcOdeme>> GetBorcOdemelerAsync(int borcId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<PersonelBorcOdeme>()
            .Include(o => o.BankaHesap)
            .Where(o => o.BorcId == borcId)
            .OrderByDescending(o => o.OdemeTarihi)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task DeleteBorcOdemeAsync(int odemeId)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasSil);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var odeme = await context.Set<PersonelBorcOdeme>()
            .Include(o => o.Borc)
            .FirstOrDefaultAsync(o => o.Id == odemeId);

        if (odeme == null)
            throw new InvalidOperationException($"Ödeme kaydı bulunamadı. Id: {odemeId}");

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            // Muhasebe fişini sil
            if (odeme.MuhasebeFisId.HasValue)
            {
                await SoftDeleteDraftPostingAsync(context, odeme.MuhasebeFisId.Value);
            }

            // Borç ödeme bilgisini güncelle
            var borc = odeme.Borc;
            borc.OdenenTutar -= odeme.OdemeTutari;
            if (borc.OdenenTutar <= 0)
            {
                borc.OdenenTutar = 0;
                borc.OdemeDurum = BorcOdemeDurum.Bekliyor;
            }
            else
            {
                borc.OdemeDurum = BorcOdemeDurum.KismenOdendi;
            }
            borc.UpdatedAt = DateTime.UtcNow;

            odeme.IsDeleted = true;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    #endregion

    #region Personel Özet

    public async Task<PersonelFinansOzet> GetPersonelFinansOzetAsync(int personelId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personel = await context.Soforler.AsNoTracking().FirstOrDefaultAsync(p => p.Id == personelId);
        if (personel == null)
            throw new InvalidOperationException($"Personel bulunamadı. Id: {personelId}");

        var avanslar = await context.Set<PersonelAvans>()
            .Where(a => a.PersonelId == personelId && a.Durum != AvansDurum.IptalEdildi)
            .AsNoTracking()
            .ToListAsync();

        var borclar = await context.Set<PersonelBorc>()
            .Where(b => b.PersonelId == personelId && b.OdemeDurum != BorcOdemeDurum.IptalEdildi)
            .AsNoTracking()
            .ToListAsync();

        // Personelin cebinden yaptığı ödenmemiş masraflar
        var aracMasrafToplam = await context.AracMasraflari
            .Where(m => m.PersonelCebindenId == personelId && !m.PersoneleOdendi)
            .AsNoTracking()
            .SumAsync(m => (decimal?)m.Tutar) ?? 0;

        var aracMasrafAdet = await context.AracMasraflari
            .Where(m => m.PersonelCebindenId == personelId && !m.PersoneleOdendi)
            .AsNoTracking()
            .CountAsync();

        var bankaHareketToplam = await context.BankaKasaHareketleri
            .Where(h => h.PersonelCebindenId == personelId && !h.PersoneleOdendi)
            .AsNoTracking()
            .SumAsync(h => (decimal?)h.Tutar) ?? 0;

        var bankaHareketAdet = await context.BankaKasaHareketleri
            .Where(h => h.PersonelCebindenId == personelId && !h.PersoneleOdendi)
            .AsNoTracking()
            .CountAsync();

        return new PersonelFinansOzet
        {
            PersonelId = personel.Id,
            PersonelKodu = personel.SoforKodu,
            PersonelAdSoyad = personel.TamAd,
            Departman = personel.Departman,
            Aktif = personel.Aktif,

            ToplamAvansSayisi = avanslar.Count,
            ToplamAvans = avanslar.Sum(a => a.Tutar),
            MahsupEdilenAvans = avanslar.Sum(a => a.MahsupEdilen),
            AcikAvansSayisi = avanslar.Count(a => a.Kalan > 0),

            ToplamBorcSayisi = borclar.Count,
            ToplamBorc = borclar.Sum(b => b.Tutar),
            OdenenBorc = borclar.Sum(b => b.OdenenTutar),
            OdenmemişBorcSayisi = borclar.Count(b => b.KalanBorc > 0),

            ToplamHarcama = aracMasrafToplam + bankaHareketToplam,
            HarcamaAdet = aracMasrafAdet + bankaHareketAdet
        };
    }

    public async Task<List<PersonelCebindenHarcamaItem>> GetPersonelCebindenHarcamalarAsync(int personelId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var aracMasraflar = await context.AracMasraflari
            .Include(m => m.Arac)
            .Where(m => m.PersonelCebindenId == personelId)
            .AsNoTracking()
            .ToListAsync();

        var bankaHareketler = await context.BankaKasaHareketleri
            .Where(h => h.PersonelCebindenId == personelId)
            .AsNoTracking()
            .ToListAsync();

        var liste = new List<PersonelCebindenHarcamaItem>();

        foreach (var m in aracMasraflar)
        {
            var plakaAciklama = m.Arac?.AktifPlaka != null ? $" [{m.Arac.AktifPlaka}]" : "";
            liste.Add(new PersonelCebindenHarcamaItem
            {
                Tarih = m.MasrafTarihi,
                Aciklama = (m.Aciklama ?? "Araç Masrafı") + plakaAciklama,
                Tutar = m.Tutar,
                Kaynak = "AracMasraf",
                KaynakId = m.Id,
                PersoneleOdendi = m.PersoneleOdendi
            });
        }

        foreach (var h in bankaHareketler)
        {
            liste.Add(new PersonelCebindenHarcamaItem
            {
                Tarih = h.IslemTarihi,
                Aciklama = h.Aciklama ?? "Banka/Kasa Hareketi",
                Tutar = h.Tutar,
                Kaynak = "BankaHareket",
                KaynakId = h.Id,
                PersoneleOdendi = h.PersoneleOdendi
            });
        }

        return liste.OrderByDescending(x => x.Tarih).ToList();
    }

    public async Task<List<PersonelFinansOzet>> GetTumPersonelFinansOzetAsync(int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personeller = await context.Soforler
            .Where(p => p.Aktif)
            .AsNoTracking()
            .ToListAsync();

        var ozetler = new List<PersonelFinansOzet>();

        foreach (var personel in personeller)
        {
            var query1 = context.Set<PersonelAvans>().Where(a => a.PersonelId == personel.Id && a.Durum != AvansDurum.IptalEdildi);
            var query2 = context.Set<PersonelBorc>().Where(b => b.PersonelId == personel.Id && b.OdemeDurum != BorcOdemeDurum.IptalEdildi);

            if (firmaId.HasValue)
            {
                query1 = query1.Where(a => a.FirmaId == firmaId);
                query2 = query2.Where(b => b.FirmaId == firmaId);
            }

            var avanslar = await query1.AsNoTracking().ToListAsync();
            var borclar = await query2.AsNoTracking().ToListAsync();

            var ozet = new PersonelFinansOzet
            {
                PersonelId = personel.Id,
                PersonelKodu = personel.SoforKodu,
                PersonelAdSoyad = personel.TamAd,
                Departman = personel.Departman,
                Aktif = personel.Aktif,

                ToplamAvansSayisi = avanslar.Count,
                ToplamAvans = avanslar.Sum(a => a.Tutar),
                MahsupEdilenAvans = avanslar.Sum(a => a.MahsupEdilen),
                AcikAvansSayisi = avanslar.Count(a => a.Kalan > 0),

                ToplamBorcSayisi = borclar.Count,
                ToplamBorc = borclar.Sum(b => b.Tutar),
                OdenenBorc = borclar.Sum(b => b.OdenenTutar),
                OdenmemişBorcSayisi = borclar.Count(b => b.KalanBorc > 0)
            };

            if (ozet.ToplamAvansSayisi > 0 || ozet.ToplamBorcSayisi > 0)
                ozetler.Add(ozet);
        }

        return ozetler.OrderByDescending(o => Math.Abs(o.NetDurum)).ToList();
    }

    #endregion

    #region Ayarlar

    public async Task<PersonelFinansAyar?> GetAyarlarAsync(int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await context.Set<PersonelFinansAyar>()
            .Include(a => a.PersonelAvanslariHesap)
            .Include(a => a.PersoneleBorclarHesap)
            .Include(a => a.KasaHesap)
            .Include(a => a.BankaHesap)
            .FirstOrDefaultAsync(a => a.FirmaId == firmaId);

        if (ayar == null)
        {
            // Varsayılan ayarları oluştur
            ayar = new PersonelFinansAyar
            {
                FirmaId = firmaId,
                OtomatikFisOlustur = true,
                AvansVerildigindeFisOlustur = true,
                AvansMahsupFisOlustur = true,
                BorcOdendigindeFisOlustur = true
            };
        }

        return ayar;
    }

    public async Task<PersonelFinansAyar> SaveAyarlarAsync(PersonelFinansAyar ayar)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Set<PersonelFinansAyar>()
            .FirstOrDefaultAsync(a => a.FirmaId == ayar.FirmaId);

        if (existing == null)
        {
            context.Set<PersonelFinansAyar>().Add(ayar);
        }
        else
        {
            existing.PersonelAvanslariHesapId = ayar.PersonelAvanslariHesapId;
            existing.PersoneleBorclarHesapId = ayar.PersoneleBorclarHesapId;
            existing.KasaHesapId = ayar.KasaHesapId;
            existing.BankaHesapId = ayar.BankaHesapId;
            existing.OtomatikFisOlustur = ayar.OtomatikFisOlustur;
            existing.AvansVerildigindeFisOlustur = ayar.AvansVerildigindeFisOlustur;
            existing.AvansMahsupFisOlustur = ayar.AvansMahsupFisOlustur;
            existing.BorcOdendigindeFisOlustur = ayar.BorcOdendigindeFisOlustur;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return ayar;
    }

    #endregion

    #region Muhasebe Entegrasyonu

    private async Task CreateAvansMuhasebeFisiAsync(ApplicationDbContext context, PersonelAvans avans)
    {
        var ayar = await context.Set<PersonelFinansAyar>().FirstOrDefaultAsync(a => a.FirmaId == avans.FirmaId);
        if (ayar == null || !ayar.OtomatikFisOlustur || !ayar.AvansVerildigindeFisOlustur)
            return;
        if (!ayar.PersonelAvanslariHesapId.HasValue || !ayar.KasaHesapId.HasValue)
            return;

        var fis = new MuhasebeFis
        {
            FisTarihi = DateTime.SpecifyKind(avans.AvansTarihi.Date, DateTimeKind.Utc),
            FisTipi = FisTipi.Tediye,
            Kaynak = FisKaynak.Otomatik,
            KaynakId = avans.Id,
            KaynakTip = "PersonelAvans",
            Aciklama = $"Personel avansı - {avans.Personel?.TamAd ?? avans.PersonelId.ToString()} ({avans.Tutar:N2} ₺)",
            Durum = FisDurum.Taslak,
            Kalemler = new List<MuhasebeFisKalem>
            {
                new() { HesapId = ayar.PersonelAvanslariHesapId.Value, Borc = avans.Tutar, Alacak = 0, SiraNo = 1, Aciklama = "Verilen avans" },
                new() { HesapId = ayar.KasaHesapId.Value, Borc = 0, Alacak = avans.Tutar, SiraNo = 2, Aciklama = "Kasa çıkışı" }
            }
        };

        var kaydedilenFis = await _muhasebeService.CreateFisAtomicAsync(fis, context);

        var entity = await context.Set<PersonelAvans>().FindAsync(avans.Id);
        if (entity != null)
        {
            entity.MuhasebeFisId = kaydedilenFis.Id;
            await context.SaveChangesAsync();
        }
        avans.MuhasebeFisId = kaydedilenFis.Id;
    }

    private async Task CreateBorcMuhasebeFisiAsync(ApplicationDbContext context, PersonelBorc borc)
    {
        var ayar = await context.Set<PersonelFinansAyar>().FirstOrDefaultAsync(a => a.FirmaId == borc.FirmaId);
        if (ayar == null || !ayar.OtomatikFisOlustur)
            return;
        if (!ayar.PersoneleBorclarHesapId.HasValue || !ayar.KasaHesapId.HasValue)
            return;

        var fis = new MuhasebeFis
        {
            FisTarihi = DateTime.SpecifyKind(borc.BorcTarihi.Date, DateTimeKind.Utc),
            FisTipi = FisTipi.Mahsup,
            Kaynak = FisKaynak.Otomatik,
            KaynakId = borc.Id,
            KaynakTip = "PersonelBorc",
            Aciklama = $"Personel borç kaydı - {borc.Personel?.TamAd ?? borc.PersonelId.ToString()} ({borc.BorcNedeni})",
            Durum = FisDurum.Taslak,
            Kalemler = new List<MuhasebeFisKalem>
            {
                new() { HesapId = ayar.KasaHesapId.Value, Borc = borc.Tutar, Alacak = 0, SiraNo = 1, Aciklama = "Kasa girişi" },
                new() { HesapId = ayar.PersoneleBorclarHesapId.Value, Borc = 0, Alacak = borc.Tutar, SiraNo = 2, Aciklama = "Personele borç" }
            }
        };

        var kaydedilenFis = await _muhasebeService.CreateFisAtomicAsync(fis, context);

        var entity = await context.Set<PersonelBorc>().FindAsync(borc.Id);
        if (entity != null)
        {
            entity.MuhasebeFisId = kaydedilenFis.Id;
            await context.SaveChangesAsync();
        }
        borc.MuhasebeFisId = kaydedilenFis.Id;
    }

    private async Task CreateBorcOdemeMuhasebeFisiAsync(ApplicationDbContext context, PersonelBorcOdeme odeme, PersonelBorc borc)
    {
        var ayar = await context.Set<PersonelFinansAyar>().FirstOrDefaultAsync(a => a.FirmaId == borc.FirmaId);
        if (ayar == null || !ayar.OtomatikFisOlustur || !ayar.BorcOdendigindeFisOlustur)
            return;
        if (!ayar.PersoneleBorclarHesapId.HasValue || !ayar.KasaHesapId.HasValue)
            return;

        var kasaAlacakHesapId = odeme.BankaHesapId.HasValue && ayar.BankaHesapId.HasValue
            ? ayar.BankaHesapId.Value
            : ayar.KasaHesapId.Value;

        var fis = new MuhasebeFis
        {
            FisTarihi = DateTime.SpecifyKind(odeme.OdemeTarihi.Date, DateTimeKind.Utc),
            FisTipi = FisTipi.Tediye,
            Kaynak = FisKaynak.Otomatik,
            KaynakId = odeme.Id,
            KaynakTip = "PersonelBorcOdeme",
            Aciklama = $"Personel borç ödemesi - {borc.Personel?.TamAd ?? borc.PersonelId.ToString()} ({odeme.OdemeTutari:N2} ₺)",
            Durum = FisDurum.Taslak,
            Kalemler = new List<MuhasebeFisKalem>
            {
                new() { HesapId = ayar.PersoneleBorclarHesapId.Value, Borc = odeme.OdemeTutari, Alacak = 0, SiraNo = 1, Aciklama = "Borç kapatma" },
                new() { HesapId = kasaAlacakHesapId, Borc = 0, Alacak = odeme.OdemeTutari, SiraNo = 2, Aciklama = "Kasa/banka çıkışı" }
            }
        };

        var kaydedilenFis = await _muhasebeService.CreateFisAtomicAsync(fis, context);

        var entity = await context.Set<PersonelBorcOdeme>().FindAsync(odeme.Id);
        if (entity != null)
        {
            entity.MuhasebeFisId = kaydedilenFis.Id;
            await context.SaveChangesAsync();
        }
        odeme.MuhasebeFisId = kaydedilenFis.Id;
    }

    private async Task CreateMahsupMuhasebeFisiAsync(ApplicationDbContext context, PersonelAvansMahsup mahsup, PersonelAvans avans)
    {
        var ayar = await context.Set<PersonelFinansAyar>().FirstOrDefaultAsync(a => a.FirmaId == avans.FirmaId);
        if (ayar == null || !ayar.OtomatikFisOlustur || !ayar.AvansMahsupFisOlustur)
            return;
        if (!ayar.PersonelAvanslariHesapId.HasValue || !ayar.KasaHesapId.HasValue)
            return;

        var fis = new MuhasebeFis
        {
            FisTarihi = DateTime.SpecifyKind(mahsup.MahsupTarihi.Date, DateTimeKind.Utc),
            FisTipi = FisTipi.Mahsup,
            Kaynak = FisKaynak.Otomatik,
            KaynakId = mahsup.Id,
            KaynakTip = "PersonelAvansMahsup",
            Aciklama = $"Avans mahsubu - {avans.Personel?.TamAd ?? avans.PersonelId.ToString()} ({mahsup.MahsupTutari:N2} ₺)",
            Durum = FisDurum.Taslak,
            Kalemler = new List<MuhasebeFisKalem>
            {
                new() { HesapId = ayar.KasaHesapId.Value, Borc = mahsup.MahsupTutari, Alacak = 0, SiraNo = 1, Aciklama = "Mahsup tahsilat" },
                new() { HesapId = ayar.PersonelAvanslariHesapId.Value, Borc = 0, Alacak = mahsup.MahsupTutari, SiraNo = 2, Aciklama = "Avans kapatma" }
            }
        };

        await _muhasebeService.CreateFisAtomicAsync(fis, context);
    }

    #endregion

    #region Raporlama

    public async Task<byte[]> ExportAvansRaporAsync(List<PersonelAvans> avanslar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Personel Avansları");

        // Başlıklar
        worksheet.Cell(1, 1).Value = "Tarih";
        worksheet.Cell(1, 2).Value = "Personel";
        worksheet.Cell(1, 3).Value = "Firma";
        worksheet.Cell(1, 4).Value = "Tutar";
        worksheet.Cell(1, 5).Value = "Ödeme Şekli";
        worksheet.Cell(1, 6).Value = "Mahsup Edilen";
        worksheet.Cell(1, 7).Value = "Kalan";
        worksheet.Cell(1, 8).Value = "Durum";
        worksheet.Cell(1, 9).Value = "Açıklama";

        var headerRange = worksheet.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Veriler
        int row = 2;
        foreach (var avans in avanslar)
        {
            worksheet.Cell(row, 1).Value = avans.AvansTarihi.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 2).Value = avans.Personel?.TamAd ?? "";
            worksheet.Cell(row, 3).Value = avans.Firma?.FirmaAdi ?? "-";
            worksheet.Cell(row, 4).Value = avans.Tutar;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 5).Value = avans.OdemeSekli.ToString();
            worksheet.Cell(row, 6).Value = avans.MahsupEdilen;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 7).Value = avans.Kalan;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 8).Value = avans.Durum.ToString();
            worksheet.Cell(row, 9).Value = avans.Aciklama ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportBorcRaporAsync(List<PersonelBorc> borclar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Personel Borçları");

        // Başlıklar
        worksheet.Cell(1, 1).Value = "Tarih";
        worksheet.Cell(1, 2).Value = "Personel";
        worksheet.Cell(1, 3).Value = "Firma";
        worksheet.Cell(1, 4).Value = "Borç Nedeni";
        worksheet.Cell(1, 5).Value = "Borç Tipi";
        worksheet.Cell(1, 6).Value = "Tutar";
        worksheet.Cell(1, 7).Value = "Ödenen";
        worksheet.Cell(1, 8).Value = "Kalan";
        worksheet.Cell(1, 9).Value = "Durum";
        worksheet.Cell(1, 10).Value = "Planlanan Ödeme";
        worksheet.Cell(1, 11).Value = "Açıklama";

        var headerRange = worksheet.Range(1, 1, 1, 11);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Veriler
        int row = 2;
        foreach (var borc in borclar)
        {
            worksheet.Cell(row, 1).Value = borc.BorcTarihi.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 2).Value = borc.Personel?.TamAd ?? "";
            worksheet.Cell(row, 3).Value = borc.Firma?.FirmaAdi ?? "-";
            worksheet.Cell(row, 4).Value = borc.BorcNedeni;
            worksheet.Cell(row, 5).Value = borc.BorcTipi.ToString();
            worksheet.Cell(row, 6).Value = borc.Tutar;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 7).Value = borc.OdenenTutar;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 8).Value = borc.KalanBorc;
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 9).Value = borc.OdemeDurum.ToString();
            worksheet.Cell(row, 10).Value = borc.PlanlananOdemeTarihi?.ToString("dd.MM.yyyy") ?? "-";
            worksheet.Cell(row, 11).Value = borc.Aciklama ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportPersonelOzetRaporAsync(List<PersonelFinansOzet> ozetler)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Personel Finans Özeti");

        // Başlıklar
        worksheet.Cell(1, 1).Value = "Personel Kodu";
        worksheet.Cell(1, 2).Value = "Ad Soyad";
        worksheet.Cell(1, 3).Value = "Departman";
        worksheet.Cell(1, 4).Value = "Toplam Avans";
        worksheet.Cell(1, 5).Value = "Mahsup Edilen";
        worksheet.Cell(1, 6).Value = "Kalan Avans";
        worksheet.Cell(1, 7).Value = "Toplam Borç";
        worksheet.Cell(1, 8).Value = "Ödenen Borç";
        worksheet.Cell(1, 9).Value = "Kalan Borç";
        worksheet.Cell(1, 10).Value = "Net Durum";
        worksheet.Cell(1, 11).Value = "Açıklama";

        var headerRange = worksheet.Range(1, 1, 1, 11);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Veriler
        int row = 2;
        foreach (var ozet in ozetler)
        {
            worksheet.Cell(row, 1).Value = ozet.PersonelKodu;
            worksheet.Cell(row, 2).Value = ozet.PersonelAdSoyad;
            worksheet.Cell(row, 3).Value = ozet.Departman ?? "-";
            worksheet.Cell(row, 4).Value = ozet.ToplamAvans;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 5).Value = ozet.MahsupEdilenAvans;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 6).Value = ozet.KalanAvans;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 7).Value = ozet.ToplamBorc;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 8).Value = ozet.OdenenBorc;
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 9).Value = ozet.KalanBorc;
            worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 10).Value = ozet.NetDurum;
            worksheet.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
            
            if (ozet.NetDurum > 0)
                worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Red;
            else if (ozet.NetDurum < 0)
                worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Green;
            
            worksheet.Cell(row, 11).Value = ozet.NetDurumAciklama;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region Toplu İşlemler

    public async Task<int> TopluAvansMahsupAsync(List<int> avansIdler, DateTime mahsupTarihi, string aciklama)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        await using var context = await _contextFactory.CreateDbContextAsync();
        int sayac = 0;
        foreach (var avansId in avansIdler)
        {
            var avans = await context.Set<PersonelAvans>().FindAsync(avansId);
            if (avans == null || avans.Kalan <= 0) continue;

            var mahsup = new PersonelAvansMahsup
            {
                AvansId = avansId,
                MahsupTarihi = mahsupTarihi,
                MahsupTutari = avans.Kalan,
                MahsupSekli = MahsupSekli.Diger,
                Aciklama = aciklama
            };

            await MahsupEtAvansAsync(avansId, mahsup);
            sayac++;
        }

        return sayac;
    }

    public async Task<int> TopluBorcOdemeAsync(List<int> borcIdler, DateTime odemeTarihi, BorcOdemeSekli odemeSekli, int? bankaHesapId)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        await using var context = await _contextFactory.CreateDbContextAsync();
        int sayac = 0;
        foreach (var borcId in borcIdler)
        {
            var borc = await context.Set<PersonelBorc>().FindAsync(borcId);
            if (borc == null || borc.KalanBorc <= 0) continue;

            var odeme = new PersonelBorcOdeme
            {
                BorcId = borcId,
                OdemeTarihi = odemeTarihi,
                OdemeTutari = borc.KalanBorc,
                OdemeSekli = odemeSekli,
                BankaHesapId = bankaHesapId,
                Aciklama = "Toplu ödeme"
            };

            await OdemeYapBorcAsync(borcId, odeme);
            sayac++;
        }

        return sayac;
    }

    #endregion
}


