using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Destek Talebi (Ticket) Servisi - osTicket benzeri implementasyon
/// </summary>
public class DestekTalebiService : IDestekTalebiService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<DestekTalebiService> _logger;
    private readonly IEmailService _emailService;
    private readonly NumaraSerisiService _numaraSerisi;
    private readonly string _uploadPath;

    public DestekTalebiService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<DestekTalebiService> logger,
        IWebHostEnvironment env,
        IEmailService emailService,
        NumaraSerisiService numaraSerisi)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _emailService = emailService;
        _numaraSerisi = numaraSerisi;
        _uploadPath = Path.Combine(env.WebRootPath, "uploads", "destek");
        
        // Upload klasörünü oluştur
        if (!Directory.Exists(_uploadPath))
            Directory.CreateDirectory(_uploadPath);
    }

    #region Talep CRUD

    public async Task<List<DestekTalebi>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Include(x => x.AtananKullanici)
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .ToListAsync();
    }

    public async Task<PagedResult<DestekTalebi>> GetPagedAsync(DestekTalebiFilterParams filter)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Include(x => x.AtananKullanici)
            .Include(x => x.Cari)
            .AsQueryable();

        // Filtreler
        if (!string.IsNullOrWhiteSpace(filter.AramaMetni))
        {
            var arama = filter.AramaMetni.ToLower();
            query = query.Where(x => 
                x.TalepNo.ToLower().Contains(arama) ||
                x.Konu.ToLower().Contains(arama) ||
                x.MusteriAdi.ToLower().Contains(arama) ||
                x.MusteriEmail.ToLower().Contains(arama));
        }

        if (filter.Durum.HasValue)
            query = query.Where(x => x.Durum == filter.Durum.Value);

        if (filter.Oncelik.HasValue)
            query = query.Where(x => x.Oncelik == filter.Oncelik.Value);

        if (filter.Kaynak.HasValue)
            query = query.Where(x => x.Kaynak == filter.Kaynak.Value);

        if (filter.DepartmanId.HasValue)
            query = query.Where(x => x.DepartmanId == filter.DepartmanId.Value);

        if (filter.KategoriId.HasValue)
            query = query.Where(x => x.KategoriId == filter.KategoriId.Value);

        if (filter.AtananKullaniciId.HasValue)
            query = query.Where(x => x.AtananKullaniciId == filter.AtananKullaniciId.Value);

        if (filter.CariId.HasValue)
            query = query.Where(x => x.CariId == filter.CariId.Value);

        if (filter.BaslangicTarihi.HasValue)
            query = query.Where(x => x.CreatedAt >= filter.BaslangicTarihi.Value);

        if (filter.BitisTarihi.HasValue)
            query = query.Where(x => x.CreatedAt <= filter.BitisTarihi.Value);

        if (filter.SlaAsildi.HasValue)
            query = query.Where(x => x.SlaAsildi == filter.SlaAsildi.Value);

        if (filter.SadeceAcik == true)
            query = query.Where(x => x.Durum != DestekDurum.Kapali && x.Durum != DestekDurum.Cozuldu);

        // Sıralama
        query = filter.SortBy?.ToLower() switch
        {
            "talepno" => filter.SortDesc ? query.OrderByDescending(x => x.TalepNo) : query.OrderBy(x => x.TalepNo),
            "konu" => filter.SortDesc ? query.OrderByDescending(x => x.Konu) : query.OrderBy(x => x.Konu),
            "durum" => filter.SortDesc ? query.OrderByDescending(x => x.Durum) : query.OrderBy(x => x.Durum),
            "oncelik" => filter.SortDesc ? query.OrderByDescending(x => x.Oncelik) : query.OrderBy(x => x.Oncelik),
            "createdat" => filter.SortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => filter.SortDesc ? query.OrderByDescending(x => x.SonAktiviteTarihi) : query.OrderBy(x => x.SonAktiviteTarihi)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<DestekTalebi>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DestekTalebi?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Include(x => x.AtananKullanici)
            .Include(x => x.OlusturanKullanici)
            .Include(x => x.Cari)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DestekTalebi?> GetByIdWithDetailsAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Include(x => x.AtananKullanici)
            .Include(x => x.OlusturanKullanici)
            .Include(x => x.Cari)
            .Include(x => x.Yanitlar.OrderBy(y => y.CreatedAt))
                .ThenInclude(y => y.Kullanici)
            .Include(x => x.Yanitlar)
                .ThenInclude(y => y.Ekler)
            .Include(x => x.Ekler)
            .Include(x => x.Aktiviteler.OrderByDescending(a => a.CreatedAt))
                .ThenInclude(a => a.Kullanici)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DestekTalebi?> GetByTalepNoAsync(string talepNo)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .FirstOrDefaultAsync(x => x.TalepNo == talepNo);
    }

    public async Task<DestekTalebi> CreateAsync(DestekTalebi talep)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        if (string.IsNullOrEmpty(talep.TalepNo))
            talep.TalepNo = await GenerateNextTalepNoAsync();
        
        talep.CreatedAt = DateTime.UtcNow;
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        
        // SLA hesapla
        if (!talep.SlaSuresi.HasValue)
        {
            var sla = await context.DestekSlaListesi
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Oncelik == talep.Oncelik && x.Aktif);
            
            if (sla != null)
            {
                talep.SlaSuresi = sla.CozumSuresi;
                talep.SlaBitisTarihi = DateTime.UtcNow.AddHours(sla.CozumSuresi);
            }
        }
        
        context.DestekTalepleri.Add(talep);
        await context.SaveChangesAsync();

        // Aktivite kaydı
        await LogAktiviteInternalAsync(context, talep.Id, AktiviteTuru.Olusturuldu, 
            $"Talep oluşturuldu: {talep.TalepNo}", talep.OlusturanKullaniciId);

        // E-posta bildirimi: Yeni talep → müşteriye onay
        _ = SendDestekEmailSafeAsync(() => SendYeniTalepEmailAsync(talep));

        return talep;
    }

    public async Task<DestekTalebi> UpdateAsync(DestekTalebi talep)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        talep.UpdatedAt = DateTime.UtcNow;
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        
        context.DestekTalepleri.Update(talep);
        await context.SaveChangesAsync();
        
        return talep;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(id);
        if (talep != null)
        {
            talep.IsDeleted = true;
            talep.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<string> GenerateNextTalepNoAsync()
    {
        // Kural 15: Atomik numara üretimi
        return await _numaraSerisi.GenerateFormattedAsync("TKT", 0, 6);
    }

    #endregion

    #region Durum ve Atama

    public async Task<bool> UpdateDurumAsync(int talepId, DestekDurum yeniDurum, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(talepId);
        if (talep == null) return false;

        var eskiDurum = talep.Durum;
        talep.Durum = yeniDurum;
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        
        if (yeniDurum == DestekDurum.Kapali || yeniDurum == DestekDurum.Cozuldu)
        {
            talep.KapatilmaTarihi = DateTime.UtcNow;
            talep.CozumSuresiDakika = (int)(DateTime.UtcNow - talep.CreatedAt).TotalMinutes;
        }
        
        await context.SaveChangesAsync();

        await LogAktiviteInternalAsync(context, talepId, AktiviteTuru.DurumDegisti, 
            $"Durum değiştirildi", kullaniciId, eskiDurum.ToString(), yeniDurum.ToString());

        // E-posta bildirimi: Durum değişikliği → müşteriye
        _ = SendDestekEmailSafeAsync(() => SendDurumDegisiklikEmailAsync(talep.Id, eskiDurum, yeniDurum));

        return true;
    }

    public async Task<bool> UpdateOncelikAsync(int talepId, DestekOncelik yeniOncelik, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(talepId);
        if (talep == null) return false;

        var eskiOncelik = talep.Oncelik;
        talep.Oncelik = yeniOncelik;
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        
        // Yeni SLA hesapla
        var sla = await context.DestekSlaListesi
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Oncelik == yeniOncelik && x.Aktif);
        
        if (sla != null)
        {
            talep.SlaSuresi = sla.CozumSuresi;
            talep.SlaBitisTarihi = DateTime.UtcNow.AddHours(sla.CozumSuresi);
        }
        
        await context.SaveChangesAsync();
        
        await LogAktiviteInternalAsync(context, talepId, AktiviteTuru.OncelikDegisti, 
            $"Öncelik değiştirildi", kullaniciId, eskiOncelik.ToString(), yeniOncelik.ToString());
        
        return true;
    }

    public async Task<bool> AtaAsync(int talepId, int atananKullaniciId, int? atayanKullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(talepId);
        if (talep == null) return false;

        var kullanici = await context.Kullanicilar.AsNoTracking().FirstOrDefaultAsync(x => x.Id == atananKullaniciId);
        if (kullanici == null) return false;

        var eskiAtanan = talep.AtananKullaniciId;
        talep.AtananKullaniciId = atananKullaniciId;
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        
        if (talep.Durum == DestekDurum.Yeni)
            talep.Durum = DestekDurum.Acik;
        
        await context.SaveChangesAsync();

        await LogAktiviteInternalAsync(context, talepId, AktiviteTuru.Atandi, 
            $"Talep {kullanici.AdSoyad} kullanıcısına atandı", atayanKullaniciId);

        // E-posta bildirimi: Atama → temsilciye
        _ = SendDestekEmailSafeAsync(() => SendAtamaEmailAsync(talep.Id, kullanici));

        return true;
    }

    public async Task<bool> TransferEtAsync(int talepId, int yeniDepartmanId, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(talepId);
        if (talep == null) return false;

        var departman = await context.DestekDepartmanlari.AsNoTracking().FirstOrDefaultAsync(x => x.Id == yeniDepartmanId);
        if (departman == null) return false;

        var eskiDepartmanId = talep.DepartmanId;
        talep.DepartmanId = yeniDepartmanId;
        talep.AtananKullaniciId = null; // Atamayı sıfırla
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        
        await LogAktiviteInternalAsync(context, talepId, AktiviteTuru.Transferedildi, 
            $"Talep '{departman.Ad}' departmanına transfer edildi", kullaniciId);
        
        return true;
    }

    public async Task<bool> KapatAsync(int talepId, int? kullaniciId = null, string? kapatmaNotu = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(talepId);
        if (talep == null) return false;

        talep.Durum = DestekDurum.Kapali;
        talep.KapatilmaTarihi = DateTime.UtcNow;
        talep.CozumSuresiDakika = (int)(DateTime.UtcNow - talep.CreatedAt).TotalMinutes;
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(kapatmaNotu))
            talep.DahiliNotlar = (talep.DahiliNotlar ?? "") + $"\n[Kapatma Notu - {DateTime.Now:dd.MM.yyyy HH:mm}]: {kapatmaNotu}";
        
        await context.SaveChangesAsync();

        await LogAktiviteInternalAsync(context, talepId, AktiviteTuru.Kapandi, 
            $"Talep kapatıldı{(string.IsNullOrEmpty(kapatmaNotu) ? "" : $": {kapatmaNotu}")}", kullaniciId);

        // E-posta bildirimi: Kapatma → müşteriye
        _ = SendDestekEmailSafeAsync(() => SendDurumDegisiklikEmailAsync(talep.Id, DestekDurum.Acik, DestekDurum.Kapali));

        return true;
    }

    public async Task<bool> YenidenAcAsync(int talepId, int? kullaniciId = null, string? aciklamaNotu = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(talepId);
        if (talep == null) return false;

        talep.Durum = DestekDurum.Acik;
        talep.KapatilmaTarihi = null;
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        
        await LogAktiviteInternalAsync(context, talepId, AktiviteTuru.YenidenAcildi, 
            $"Talep yeniden açıldı{(string.IsNullOrEmpty(aciklamaNotu) ? "" : $": {aciklamaNotu}")}", kullaniciId);
        
        return true;
    }

    public async Task<bool> BirlestirAsync(int anaTalepId, int birlestirilecekTalepId, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var anaTalep = await context.DestekTalepleri.FindAsync(anaTalepId);
        var birlestirilecekTalep = await context.DestekTalepleri.FindAsync(birlestirilecekTalepId);
        
        if (anaTalep == null || birlestirilecekTalep == null) return false;

        // İlişki oluştur
        var iliski = new DestekTalebiIliski
        {
            AnaTalepId = anaTalepId,
            IliskiliTalepId = birlestirilecekTalepId,
            IliskiTuru = IliskiTuru.Birlestirildi,
            CreatedAt = DateTime.UtcNow
        };
        context.DestekTalebiIliskileri.Add(iliski);
        
        // Birleştirilen talebi kapat
        birlestirilecekTalep.Durum = DestekDurum.Kapali;
        birlestirilecekTalep.KapatilmaTarihi = DateTime.UtcNow;
        birlestirilecekTalep.DahiliNotlar = (birlestirilecekTalep.DahiliNotlar ?? "") + 
            $"\n[Birleştirildi - {DateTime.Now:dd.MM.yyyy HH:mm}]: #{anaTalep.TalepNo} ile birleştirildi";
        birlestirilecekTalep.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        
        await LogAktiviteInternalAsync(context, anaTalepId, AktiviteTuru.Birlestirildi, 
            $"#{birlestirilecekTalep.TalepNo} talebi bu talep ile birleştirildi", kullaniciId);
        
        await LogAktiviteInternalAsync(context, birlestirilecekTalepId, AktiviteTuru.Birlestirildi, 
            $"#{anaTalep.TalepNo} talebi ile birleştirildi ve kapatıldı", kullaniciId);
        
        return true;
    }

    #endregion

    #region Yanıt İşlemleri

    public async Task<DestekTalebiYanit> AddYanitAsync(int talepId, DestekTalebiYanit yanit)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(talepId);
        if (talep == null)
            throw new InvalidOperationException("Talep bulunamadı");

        yanit.DestekTalebiId = talepId;
        yanit.CreatedAt = DateTime.UtcNow;
        
        context.DestekTalebiYanitlari.Add(yanit);
        
        // Talep güncelle
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        
        // İlk yanıt süresi hesapla
        if (!talep.IlkYanitSuresiDakika.HasValue && yanit.KullaniciId.HasValue && !yanit.MusteriYaniti)
        {
            talep.IlkYanitSuresiDakika = (int)(DateTime.UtcNow - talep.CreatedAt).TotalMinutes;
        }
        
        // Müşteri yanıtı ise durumu değiştir
        if (yanit.MusteriYaniti && talep.Durum == DestekDurum.YanitBekleniyor)
        {
            talep.Durum = DestekDurum.Acik;
        }
        // Temsilci yanıtı ise
        else if (!yanit.MusteriYaniti && !yanit.DahiliNot)
        {
            talep.Durum = DestekDurum.YanitBekleniyor;
        }
        
        await context.SaveChangesAsync();
        
        // Hazır yanıt kullanım sayacı
        if (yanit.HazirYanitId.HasValue)
        {
            await IncrementHazirYanitKullanimAsync(yanit.HazirYanitId.Value);
        }
        
        // Aktivite kaydı
        var aktiviteTuru = yanit.DahiliNot ? AktiviteTuru.YanitEklendi : AktiviteTuru.YanitEklendi;
        var aciklama = yanit.MusteriYaniti ? "Müşteri yanıtı eklendi" : 
                       yanit.DahiliNot ? "Dahili not eklendi" : "Yanıt eklendi";
        await LogAktiviteInternalAsync(context, talepId, aktiviteTuru, aciklama, yanit.KullaniciId);

        // E-posta bildirimi: Yanıt → müşteriye (temsilci yanıtıysa ve dahili not değilse)
        if (!yanit.DahiliNot && !yanit.MusteriYaniti)
        {
            _ = SendDestekEmailSafeAsync(() => SendYanitEmailAsync(talep, yanit.Icerik));
        }

        return yanit;
    }

    public async Task<List<DestekTalebiYanit>> GetYanitlarAsync(int talepId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalebiYanitlari
            .AsNoTracking()
            .Include(x => x.Kullanici)
            .Include(x => x.Ekler)
            .Where(x => x.DestekTalebiId == talepId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteYanitAsync(int yanitId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var yanit = await context.DestekTalebiYanitlari.FindAsync(yanitId);
        if (yanit == null) return false;

        yanit.IsDeleted = true;
        yanit.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        
        return true;
    }

    #endregion

    #region Dosya Ekleri

    public async Task<DestekTalebiEk> AddEkAsync(int talepId, DestekTalebiEk ek, Stream fileStream)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var talep = await context.DestekTalepleri.FindAsync(talepId);
        if (talep == null)
            throw new InvalidOperationException("Talep bulunamadı");

        // Dosyayı kaydet
        var dosyaAdi = $"{Guid.NewGuid()}_{ek.OrijinalDosyaAdi}";
        var talepKlasor = Path.Combine(_uploadPath, talepId.ToString());
        
        if (!Directory.Exists(talepKlasor))
            Directory.CreateDirectory(talepKlasor);
        
        var dosyaYolu = Path.Combine(talepKlasor, dosyaAdi);
        
        using (var fs = new FileStream(dosyaYolu, FileMode.Create))
        {
            await fileStream.CopyToAsync(fs);
        }
        
        ek.DestekTalebiId = talepId;
        ek.DosyaAdi = dosyaAdi;
        ek.DosyaYolu = dosyaYolu;
        ek.CreatedAt = DateTime.UtcNow;
        
        context.DestekTalebiEkleri.Add(ek);
        
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        
        await LogAktiviteInternalAsync(context, talepId, AktiviteTuru.DosyaEklendi, 
            $"Dosya eklendi: {ek.OrijinalDosyaAdi}", ek.YukleyenKullaniciId);
        
        return ek;
    }

    public async Task<DestekTalebiEk> AddYanitEkAsync(int yanitId, DestekTalebiEk ek, Stream fileStream)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var yanit = await context.DestekTalebiYanitlari.FindAsync(yanitId);
        if (yanit == null)
            throw new InvalidOperationException("Yanıt bulunamadı");

        // Dosyayı kaydet
        var dosyaAdi = $"{Guid.NewGuid()}_{ek.OrijinalDosyaAdi}";
        var talepKlasor = Path.Combine(_uploadPath, yanit.DestekTalebiId.ToString());
        
        if (!Directory.Exists(talepKlasor))
            Directory.CreateDirectory(talepKlasor);
        
        var dosyaYolu = Path.Combine(talepKlasor, dosyaAdi);
        
        using (var fs = new FileStream(dosyaYolu, FileMode.Create))
        {
            await fileStream.CopyToAsync(fs);
        }
        
        ek.YanitId = yanitId;
        ek.DestekTalebiId = yanit.DestekTalebiId;
        ek.DosyaAdi = dosyaAdi;
        ek.DosyaYolu = dosyaYolu;
        ek.CreatedAt = DateTime.UtcNow;
        
        context.DestekTalebiEkleri.Add(ek);
        await context.SaveChangesAsync();
        
        return ek;
    }

    public async Task<List<DestekTalebiEk>> GetEklerAsync(int talepId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalebiEkleri
            .AsNoTracking()
            .Include(x => x.YukleyenKullanici)
            .Where(x => x.DestekTalebiId == talepId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<DestekTalebiEk?> GetEkByIdAsync(int ekId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalebiEkleri
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ekId);
    }

    public async Task<Stream?> GetEkDosyaStreamAsync(int ekId)
    {
        var ek = await GetEkByIdAsync(ekId);
        if (ek == null || !File.Exists(ek.DosyaYolu))
            return null;

        return new FileStream(ek.DosyaYolu, FileMode.Open, FileAccess.Read);
    }

    public async Task<bool> DeleteEkAsync(int ekId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var ek = await context.DestekTalebiEkleri.FindAsync(ekId);
        if (ek == null) return false;

        // Dosyayı sil
        if (File.Exists(ek.DosyaYolu))
        {
            try { File.Delete(ek.DosyaYolu); } catch { }
        }

        ek.IsDeleted = true;
        ek.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        
        return true;
    }

    #endregion

    #region Filtre ve Arama

    public async Task<List<DestekTalebi>> GetByDurumAsync(DestekDurum durum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.AtananKullanici)
            .Where(x => x.Durum == durum)
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .ToListAsync();
    }

    public async Task<List<DestekTalebi>> GetByDepartmanAsync(int departmanId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Kategori)
            .Include(x => x.AtananKullanici)
            .Where(x => x.DepartmanId == departmanId)
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .ToListAsync();
    }

    public async Task<List<DestekTalebi>> GetByKategoriAsync(int kategoriId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.AtananKullanici)
            .Where(x => x.KategoriId == kategoriId)
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .ToListAsync();
    }

    public async Task<List<DestekTalebi>> GetByAtananKullaniciAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Where(x => x.AtananKullaniciId == kullaniciId)
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .ToListAsync();
    }

    public async Task<List<DestekTalebi>> GetByCariAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Where(x => x.CariId == cariId)
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .ToListAsync();
    }

    public async Task<List<DestekTalebi>> AramaAsync(string aramaMetni)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arama = aramaMetni.ToLower();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Where(x => 
                x.TalepNo.ToLower().Contains(arama) ||
                x.Konu.ToLower().Contains(arama) ||
                x.Aciklama.ToLower().Contains(arama) ||
                x.MusteriAdi.ToLower().Contains(arama) ||
                x.MusteriEmail.ToLower().Contains(arama) ||
                (x.Etiketler != null && x.Etiketler.ToLower().Contains(arama)))
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .Take(50)
            .ToListAsync();
    }

    public async Task<List<DestekTalebi>> GetSlaAsildiBekleyenlerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.AtananKullanici)
            .Where(x => x.SlaAsildi && x.Durum != DestekDurum.Kapali && x.Durum != DestekDurum.Cozuldu)
            .OrderByDescending(x => x.SonAktiviteTarihi)
            .ToListAsync();
    }

    #endregion

    #region Departman CRUD

    public async Task<List<DestekDepartman>> GetDepartmanlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekDepartmanlari
            .AsNoTracking()
            .Include(x => x.Kategoriler.Where(k => !k.IsDeleted && k.Aktif))
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<DestekDepartman?> GetDepartmanByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekDepartmanlari
            .AsNoTracking()
            .Include(x => x.Kategoriler)
            .Include(x => x.Uyeler)
                .ThenInclude(u => u.Kullanici)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DestekDepartman> CreateDepartmanAsync(DestekDepartman departman)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        departman.CreatedAt = DateTime.UtcNow;
        context.DestekDepartmanlari.Add(departman);
        await context.SaveChangesAsync();
        return departman;
    }

    public async Task<DestekDepartman> UpdateDepartmanAsync(DestekDepartman departman)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        departman.UpdatedAt = DateTime.UtcNow;
        context.DestekDepartmanlari.Update(departman);
        await context.SaveChangesAsync();
        return departman;
    }

    public async Task DeleteDepartmanAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var departman = await context.DestekDepartmanlari.FindAsync(id);
        if (departman != null)
        {
            departman.IsDeleted = true;
            departman.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<DestekDepartmanUye>> GetDepartmanUyeleriAsync(int departmanId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekDepartmanUyeleri
            .AsNoTracking()
            .Include(x => x.Kullanici)
            .Where(x => x.DepartmanId == departmanId && !x.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> AddDepartmanUyeAsync(int departmanId, int kullaniciId, bool yonetici = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var mevcut = await context.DestekDepartmanUyeleri
            .FirstOrDefaultAsync(x => x.DepartmanId == departmanId && x.KullaniciId == kullaniciId);
        
        if (mevcut != null)
        {
            if (mevcut.IsDeleted)
            {
                mevcut.IsDeleted = false;
                mevcut.Yonetici = yonetici;
                mevcut.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            return true;
        }

        var uye = new DestekDepartmanUye
        {
            DepartmanId = departmanId,
            KullaniciId = kullaniciId,
            Yonetici = yonetici,
            CreatedAt = DateTime.UtcNow
        };
        context.DestekDepartmanUyeleri.Add(uye);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveDepartmanUyeAsync(int departmanId, int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var uye = await context.DestekDepartmanUyeleri
            .FirstOrDefaultAsync(x => x.DepartmanId == departmanId && x.KullaniciId == kullaniciId);
        
        if (uye == null) return false;

        uye.IsDeleted = true;
        uye.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Kategori CRUD

    public async Task<List<DestekKategori>> GetKategorilerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekKategorileri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<List<DestekKategori>> GetKategorilerByDepartmanAsync(int departmanId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekKategorileri
            .AsNoTracking()
            .Where(x => x.DepartmanId == departmanId && !x.IsDeleted && x.Aktif)
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<DestekKategori?> GetKategoriByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekKategorileri
            .AsNoTracking()
            .Include(x => x.Departman)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DestekKategori> CreateKategoriAsync(DestekKategori kategori)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        kategori.CreatedAt = DateTime.UtcNow;
        context.DestekKategorileri.Add(kategori);
        await context.SaveChangesAsync();
        return kategori;
    }

    public async Task<DestekKategori> UpdateKategoriAsync(DestekKategori kategori)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        kategori.UpdatedAt = DateTime.UtcNow;
        context.DestekKategorileri.Update(kategori);
        await context.SaveChangesAsync();
        return kategori;
    }

    public async Task DeleteKategoriAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kategori = await context.DestekKategorileri.FindAsync(id);
        if (kategori != null)
        {
            kategori.IsDeleted = true;
            kategori.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Hazır Yanıtlar

    public async Task<List<DestekHazirYanit>> GetHazirYanitlarAsync(int? departmanId = null, int? kategoriId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.DestekHazirYanitlari
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Where(x => !x.IsDeleted && x.Aktif);

        if (departmanId.HasValue)
            query = query.Where(x => x.DepartmanId == null || x.DepartmanId == departmanId.Value);

        if (kategoriId.HasValue)
            query = query.Where(x => x.KategoriId == null || x.KategoriId == kategoriId.Value);

        return await query
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<DestekHazirYanit?> GetHazirYanitByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekHazirYanitlari
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DestekHazirYanit> CreateHazirYanitAsync(DestekHazirYanit hazirYanit)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        hazirYanit.CreatedAt = DateTime.UtcNow;
        context.DestekHazirYanitlari.Add(hazirYanit);
        await context.SaveChangesAsync();
        return hazirYanit;
    }

    public async Task<DestekHazirYanit> UpdateHazirYanitAsync(DestekHazirYanit hazirYanit)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        hazirYanit.UpdatedAt = DateTime.UtcNow;
        context.DestekHazirYanitlari.Update(hazirYanit);
        await context.SaveChangesAsync();
        return hazirYanit;
    }

    public async Task DeleteHazirYanitAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hazirYanit = await context.DestekHazirYanitlari.FindAsync(id);
        if (hazirYanit != null)
        {
            hazirYanit.IsDeleted = true;
            hazirYanit.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task IncrementHazirYanitKullanimAsync(int hazirYanitId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.DestekHazirYanitlari
            .Where(x => x.Id == hazirYanitId)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.KullanimSayisi, p => p.KullanimSayisi + 1));
    }

    #endregion

    #region Bilgi Bankası

    public async Task<PagedResult<DestekBilgiBankasi>> GetBilgiBankasiPagedAsync(BilgiBankasiFilterParams filter)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.DestekBilgiBankasiMakaleleri
            .AsNoTracking()
            .Include(x => x.Kategori)
            .Include(x => x.Yazar)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.AramaMetni))
        {
            var arama = filter.AramaMetni.ToLower();
            query = query.Where(x => 
                x.Baslik.ToLower().Contains(arama) ||
                x.Icerik.ToLower().Contains(arama) ||
                (x.Etiketler != null && x.Etiketler.ToLower().Contains(arama)));
        }

        if (filter.KategoriId.HasValue)
            query = query.Where(x => x.KategoriId == filter.KategoriId.Value);

        if (filter.Durum.HasValue)
            query = query.Where(x => x.Durum == filter.Durum.Value);

        if (!string.IsNullOrWhiteSpace(filter.Etiket))
            query = query.Where(x => x.Etiketler != null && x.Etiketler.Contains(filter.Etiket));

        query = filter.SortBy?.ToLower() switch
        {
            "baslik" => filter.SortDesc ? query.OrderByDescending(x => x.Baslik) : query.OrderBy(x => x.Baslik),
            "goruntulemesayisi" => filter.SortDesc ? query.OrderByDescending(x => x.GoruntulemeSayisi) : query.OrderBy(x => x.GoruntulemeSayisi),
            _ => filter.SortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<DestekBilgiBankasi>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<DestekBilgiBankasi>> GetBilgiBankasiAramaAsync(string aramaMetni)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arama = aramaMetni.ToLower();
        return await context.DestekBilgiBankasiMakaleleri
            .AsNoTracking()
            .Include(x => x.Kategori)
            .Where(x => !x.IsDeleted && x.Durum == BilgiBankasiDurum.Yayinda &&
                (x.Baslik.ToLower().Contains(arama) ||
                 x.Icerik.ToLower().Contains(arama) ||
                 (x.Etiketler != null && x.Etiketler.ToLower().Contains(arama))))
            .OrderByDescending(x => x.GoruntulemeSayisi)
            .Take(20)
            .ToListAsync();
    }

    public async Task<DestekBilgiBankasi?> GetBilgiBankasiByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekBilgiBankasiMakaleleri
            .AsNoTracking()
            .Include(x => x.Kategori)
            .Include(x => x.Yazar)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DestekBilgiBankasi?> GetBilgiBankasiBySlugAsync(string slug)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekBilgiBankasiMakaleleri
            .AsNoTracking()
            .Include(x => x.Kategori)
            .Include(x => x.Yazar)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Durum == BilgiBankasiDurum.Yayinda);
    }

    public async Task<DestekBilgiBankasi> CreateBilgiBankasiAsync(DestekBilgiBankasi makale)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Slug oluştur
        if (string.IsNullOrEmpty(makale.Slug))
            makale.Slug = GenerateSlug(makale.Baslik);
        
        makale.CreatedAt = DateTime.UtcNow;
        
        if (makale.Durum == BilgiBankasiDurum.Yayinda && !makale.YayinlanmaTarihi.HasValue)
            makale.YayinlanmaTarihi = DateTime.UtcNow;
        
        context.DestekBilgiBankasiMakaleleri.Add(makale);
        await context.SaveChangesAsync();
        return makale;
    }

    public async Task<DestekBilgiBankasi> UpdateBilgiBankasiAsync(DestekBilgiBankasi makale)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        makale.UpdatedAt = DateTime.UtcNow;
        
        if (makale.Durum == BilgiBankasiDurum.Yayinda && !makale.YayinlanmaTarihi.HasValue)
            makale.YayinlanmaTarihi = DateTime.UtcNow;
        
        context.DestekBilgiBankasiMakaleleri.Update(makale);
        await context.SaveChangesAsync();
        return makale;
    }

    public async Task DeleteBilgiBankasiAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var makale = await context.DestekBilgiBankasiMakaleleri.FindAsync(id);
        if (makale != null)
        {
            makale.IsDeleted = true;
            makale.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task IncrementBilgiBankasiGoruntulenmeAsync(int makaleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.DestekBilgiBankasiMakaleleri
            .Where(x => x.Id == makaleId)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.GoruntulemeSayisi, p => p.GoruntulemeSayisi + 1));
    }

    public async Task<bool> YararliBulAsync(int makaleId, bool yararli)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        if (yararli)
        {
            await context.DestekBilgiBankasiMakaleleri
                .Where(x => x.Id == makaleId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.YararliBulmaSayisi, p => p.YararliBulmaSayisi + 1));
        }
        else
        {
            await context.DestekBilgiBankasiMakaleleri
                .Where(x => x.Id == makaleId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.YararsizBulmaSayisi, p => p.YararsizBulmaSayisi + 1));
        }
        
        return true;
    }

    private static string GenerateSlug(string text)
    {
        var slug = text.ToLower()
            .Replace("ş", "s").Replace("ı", "i").Replace("ğ", "g")
            .Replace("ü", "u").Replace("ö", "o").Replace("ç", "c")
            .Replace(" ", "-");
        
        // Sadece alfanumerik ve tire
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        
        return slug.Trim('-');
    }

    #endregion

    #region SLA

    public async Task<List<DestekSla>> GetSlaListesiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekSlaListesi
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Oncelik)
            .ToListAsync();
    }

    public async Task<DestekSla?> GetSlaByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekSlaListesi
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DestekSla?> GetSlaByOncelikAsync(DestekOncelik oncelik)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekSlaListesi
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Oncelik == oncelik && x.Aktif && !x.IsDeleted);
    }

    public async Task<DestekSla> CreateSlaAsync(DestekSla sla)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        sla.CreatedAt = DateTime.UtcNow;
        context.DestekSlaListesi.Add(sla);
        await context.SaveChangesAsync();
        return sla;
    }

    public async Task<DestekSla> UpdateSlaAsync(DestekSla sla)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        sla.UpdatedAt = DateTime.UtcNow;
        context.DestekSlaListesi.Update(sla);
        await context.SaveChangesAsync();
        return sla;
    }

    public async Task DeleteSlaAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sla = await context.DestekSlaListesi.FindAsync(id);
        if (sla != null)
        {
            sla.IsDeleted = true;
            sla.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<int> CheckAndUpdateSlaViolationsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var now = DateTime.UtcNow;
        var asilan = await context.DestekTalepleri
            .Where(x => !x.SlaAsildi && 
                       x.SlaBitisTarihi.HasValue && 
                       x.SlaBitisTarihi < now &&
                       x.Durum != DestekDurum.Kapali && 
                       x.Durum != DestekDurum.Cozuldu)
            .ToListAsync();

        foreach (var talep in asilan)
        {
            talep.SlaAsildi = true;
            talep.UpdatedAt = now;
            
            await LogAktiviteInternalAsync(context, talep.Id, AktiviteTuru.SlaBilgilendirme, 
                "SLA süresi aşıldı!");
        }

        await context.SaveChangesAsync();
        return asilan.Count;
    }

    #endregion

    #region Aktivite ve Raporlama

    public async Task<List<DestekTalebiAktivite>> GetAktivitelerAsync(int talepId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekTalebiAktiviteleri
            .AsNoTracking()
            .Include(x => x.Kullanici)
            .Where(x => x.DestekTalebiId == talepId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task LogAktiviteAsync(int talepId, AktiviteTuru aktiviteTuru, string aciklama, 
        int? kullaniciId = null, string? eskiDeger = null, string? yeniDeger = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await LogAktiviteInternalAsync(context, talepId, aktiviteTuru, aciklama, kullaniciId, eskiDeger, yeniDeger);
    }

    private async Task LogAktiviteInternalAsync(ApplicationDbContext context, int talepId, 
        AktiviteTuru aktiviteTuru, string aciklama, int? kullaniciId = null, 
        string? eskiDeger = null, string? yeniDeger = null)
    {
        var aktivite = new DestekTalebiAktivite
        {
            DestekTalebiId = talepId,
            AktiviteTuru = aktiviteTuru,
            Aciklama = aciklama,
            KullaniciId = kullaniciId,
            EskiDeger = eskiDeger,
            YeniDeger = yeniDeger,
            CreatedAt = DateTime.UtcNow
        };
        
        context.DestekTalebiAktiviteleri.Add(aktivite);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Dashboard ve İstatistikler

    public async Task<DestekDashboardStats> GetDashboardStatsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var bugun = DateTime.UtcNow.Date;
        var stats = new DestekDashboardStats();

        // Genel sayılar
        stats.ToplamTalepSayisi = await context.DestekTalepleri.CountAsync();
        stats.AcikTalepSayisi = await context.DestekTalepleri
            .CountAsync(x => x.Durum != DestekDurum.Kapali && x.Durum != DestekDurum.Cozuldu);
        stats.BugunAcilanTalepSayisi = await context.DestekTalepleri
            .CountAsync(x => x.CreatedAt.Date == bugun);
        stats.BugunKapatilanTalepSayisi = await context.DestekTalepleri
            .CountAsync(x => x.KapatilmaTarihi.HasValue && x.KapatilmaTarihi.Value.Date == bugun);
        stats.SlaAsildiSayisi = await context.DestekTalepleri
            .CountAsync(x => x.SlaAsildi && x.Durum != DestekDurum.Kapali && x.Durum != DestekDurum.Cozuldu);

        // Öncelik dağılımı
        var oncelikGruplari = await context.DestekTalepleri
            .Where(x => x.Durum != DestekDurum.Kapali && x.Durum != DestekDurum.Cozuldu)
            .GroupBy(x => x.Oncelik)
            .Select(g => new { Oncelik = g.Key, Sayi = g.Count() })
            .ToListAsync();
        stats.OncelikDagilimi = oncelikGruplari.ToDictionary(x => x.Oncelik, x => x.Sayi);

        // Durum dağılımı
        var durumGruplari = await context.DestekTalepleri
            .GroupBy(x => x.Durum)
            .Select(g => new { Durum = g.Key, Sayi = g.Count() })
            .ToListAsync();
        stats.DurumDagilimi = durumGruplari.ToDictionary(x => x.Durum, x => x.Sayi);

        // Departman dağılımı
        var departmanGruplari = await context.DestekTalepleri
            .Where(x => x.Durum != DestekDurum.Kapali && x.Durum != DestekDurum.Cozuldu)
            .Include(x => x.Departman)
            .GroupBy(x => x.Departman.Ad)
            .Select(g => new { DepartmanAdi = g.Key, Sayi = g.Count() })
            .ToListAsync();
        stats.DepartmanDagilimi = departmanGruplari.ToDictionary(x => x.DepartmanAdi, x => x.Sayi);

        // Kaynak dağılımı
        var kaynakGruplari = await context.DestekTalepleri
            .GroupBy(x => x.Kaynak)
            .Select(g => new { Kaynak = g.Key, Sayi = g.Count() })
            .ToListAsync();
        stats.KaynakDagilimi = kaynakGruplari.ToDictionary(x => x.Kaynak, x => x.Sayi);

        // Son talepler
        stats.SonTalepler = await context.DestekTalepleri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.AtananKullanici)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Performans metrikleri
        var kapananTalepler = await context.DestekTalepleri
            .Where(x => x.KapatilmaTarihi.HasValue)
            .ToListAsync();

        if (kapananTalepler.Any())
        {
            stats.OrtalamaIlkYanitSuresiSaat = kapananTalepler
                .Where(x => x.IlkYanitSuresiDakika.HasValue)
                .Average(x => x.IlkYanitSuresiDakika!.Value) / 60.0;
            
            stats.OrtalamaCozumSuresiSaat = kapananTalepler
                .Where(x => x.CozumSuresiDakika.HasValue)
                .Average(x => x.CozumSuresiDakika!.Value) / 60.0;
            
            stats.OrtalamaMemuniyetPuani = kapananTalepler
                .Where(x => x.MemnuniyetPuani.HasValue)
                .DefaultIfEmpty()
                .Average(x => x?.MemnuniyetPuani ?? 0);
        }

        return stats;
    }

    public async Task<DestekRaporStats> GetRaporStatsAsync(DateTime baslangic, DateTime bitis, int? departmanId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.DestekTalepleri.AsNoTracking().AsQueryable();
        
        if (departmanId.HasValue)
            query = query.Where(x => x.DepartmanId == departmanId.Value);

        var stats = new DestekRaporStats
        {
            BaslangicTarihi = baslangic,
            BitisTarihi = bitis
        };

        stats.ToplamAcilanTalep = await query
            .CountAsync(x => x.CreatedAt >= baslangic && x.CreatedAt <= bitis);
        
        stats.ToplamKapatilanTalep = await query
            .CountAsync(x => x.KapatilmaTarihi.HasValue && 
                            x.KapatilmaTarihi >= baslangic && 
                            x.KapatilmaTarihi <= bitis);
        
        stats.ToplamSlaAsimi = await query
            .CountAsync(x => x.SlaAsildi && 
                            x.CreatedAt >= baslangic && 
                            x.CreatedAt <= bitis);

        var kapananlar = await query
            .Where(x => x.KapatilmaTarihi.HasValue && 
                       x.KapatilmaTarihi >= baslangic && 
                       x.KapatilmaTarihi <= bitis)
            .ToListAsync();

        if (kapananlar.Any())
        {
            stats.OrtalamaIlkYanitSuresiDakika = kapananlar
                .Where(x => x.IlkYanitSuresiDakika.HasValue)
                .DefaultIfEmpty()
                .Average(x => x?.IlkYanitSuresiDakika ?? 0);
            
            stats.OrtalamaCozumSuresiDakika = kapananlar
                .Where(x => x.CozumSuresiDakika.HasValue)
                .DefaultIfEmpty()
                .Average(x => x?.CozumSuresiDakika ?? 0);
            
            stats.OrtalamaMemuniyetPuani = kapananlar
                .Where(x => x.MemnuniyetPuani.HasValue)
                .DefaultIfEmpty()
                .Average(x => x?.MemnuniyetPuani ?? 0);
        }

        // Günlük trend
        var gunler = (bitis - baslangic).Days + 1;
        stats.GunlukTrend = new List<GunlukTalepTrend>();
        
        for (int i = 0; i < gunler; i++)
        {
            var gun = baslangic.AddDays(i).Date;
            stats.GunlukTrend.Add(new GunlukTalepTrend
            {
                Tarih = gun,
                AcilanTalep = await query.CountAsync(x => x.CreatedAt.Date == gun),
                KapatilanTalep = await query.CountAsync(x => x.KapatilmaTarihi.HasValue && x.KapatilmaTarihi.Value.Date == gun)
            });
        }

        return stats;
    }

    public async Task<List<DestekPerformansRapor>> GetPersonelPerformansRaporuAsync(DateTime baslangic, DateTime bitis, int? departmanId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.DestekTalepleri
            .AsNoTracking()
            .Where(x => x.AtananKullaniciId.HasValue &&
                       x.CreatedAt >= baslangic && 
                       x.CreatedAt <= bitis);

        if (departmanId.HasValue)
            query = query.Where(x => x.DepartmanId == departmanId.Value);

        var gruplar = await query
            .Include(x => x.AtananKullanici)
            .GroupBy(x => new { x.AtananKullaniciId, x.AtananKullanici!.AdSoyad })
            .Select(g => new
            {
                KullaniciId = g.Key.AtananKullaniciId!.Value,
                KullaniciAdi = g.Key.AdSoyad,
                AtananTalepSayisi = g.Count(),
                CozulenTalepSayisi = g.Count(x => x.Durum == DestekDurum.Cozuldu || x.Durum == DestekDurum.Kapali),
                SlaAsimSayisi = g.Count(x => x.SlaAsildi),
                ToplamYanitSuresi = g.Sum(x => x.IlkYanitSuresiDakika ?? 0),
                YanitSayisi = g.Count(x => x.IlkYanitSuresiDakika.HasValue),
                ToplamCozumSuresi = g.Sum(x => x.CozumSuresiDakika ?? 0),
                CozumSayisi = g.Count(x => x.CozumSuresiDakika.HasValue),
                ToplamMemnuniyet = g.Sum(x => x.MemnuniyetPuani ?? 0),
                MemnuniyetSayisi = g.Count(x => x.MemnuniyetPuani.HasValue)
            })
            .ToListAsync();

        // Yanıt sayılarını al
        var yanitSayilari = await context.DestekTalebiYanitlari
            .Where(x => x.KullaniciId.HasValue &&
                       x.CreatedAt >= baslangic &&
                       x.CreatedAt <= bitis &&
                       !x.MusteriYaniti)
            .GroupBy(x => x.KullaniciId)
            .Select(g => new { KullaniciId = g.Key!.Value, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.KullaniciId, x => x.Sayi);

        return gruplar.Select(g => new DestekPerformansRapor
        {
            KullaniciId = g.KullaniciId,
            KullaniciAdi = g.KullaniciAdi,
            AtananTalepSayisi = g.AtananTalepSayisi,
            CozulenTalepSayisi = g.CozulenTalepSayisi,
            SlaAsimSayisi = g.SlaAsimSayisi,
            OrtalamaYanitSuresiDakika = g.YanitSayisi > 0 ? (double)g.ToplamYanitSuresi / g.YanitSayisi : 0,
            OrtalamaCozumSuresiDakika = g.CozumSayisi > 0 ? (double)g.ToplamCozumSuresi / g.CozumSayisi : 0,
            OrtalamaMemuniyetPuani = g.MemnuniyetSayisi > 0 ? (double)g.ToplamMemnuniyet / g.MemnuniyetSayisi : 0,
            ToplamYanitSayisi = yanitSayilari.GetValueOrDefault(g.KullaniciId, 0)
        }).ToList();
    }

    #endregion

    #region Ayarlar

    public async Task<string?> GetAyarAsync(string anahtar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await context.DestekAyarlari
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Anahtar == anahtar);
        return ayar?.Deger;
    }

    public async Task SetAyarAsync(string anahtar, string deger, string? aciklama = null, string? grup = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var ayar = await context.DestekAyarlari.FirstOrDefaultAsync(x => x.Anahtar == anahtar);
        
        if (ayar != null)
        {
            ayar.Deger = deger;
            ayar.UpdatedAt = DateTime.UtcNow;
            if (aciklama != null) ayar.Aciklama = aciklama;
            if (grup != null) ayar.Grup = grup;
        }
        else
        {
            ayar = new DestekAyar
            {
                Anahtar = anahtar,
                Deger = deger,
                Aciklama = aciklama,
                Grup = grup,
                CreatedAt = DateTime.UtcNow
            };
            context.DestekAyarlari.Add(ayar);
        }
        
        await context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetAyarlarByGrupAsync(string grup)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekAyarlari
            .AsNoTracking()
            .Where(x => x.Grup == grup)
            .ToDictionaryAsync(x => x.Anahtar, x => x.Deger);
    }

    #endregion

    #region E-posta Bildirim Yardımcıları

    private async Task<bool> IsEmailBildirimAktifAsync()
    {
        try
        {
            var ayar = await GetAyarAsync("EmailBildirimAktif");
            return bool.TryParse(ayar, out var aktif) && aktif;
        }
        catch
        {
            return false;
        }
    }

    private async Task SendDestekEmailSafeAsync(Func<Task> emailAction)
    {
        try
        {
            if (!await IsEmailBildirimAktifAsync())
            {
                _logger.LogDebug("Destek e-posta bildirimleri devre dışı");
                return;
            }
            await emailAction();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Destek e-posta bildirimi gönderilirken hata oluştu");
        }
    }

    private async Task SendYeniTalepEmailAsync(DestekTalebi talep)
    {
        if (string.IsNullOrWhiteSpace(talep.MusteriEmail)) return;

        await _emailService.SendDestekYeniTalepEmailAsync(
            talep.MusteriEmail,
            talep.MusteriAdi,
            talep.TalepNo,
            talep.Konu,
            talep.Oncelik.ToString());
    }

    private async Task SendYanitEmailAsync(DestekTalebi talep, string yanitIcerik)
    {
        if (string.IsNullOrWhiteSpace(talep.MusteriEmail)) return;

        // HTML etiketlerini temizle - özet için
        var ozet = System.Text.RegularExpressions.Regex.Replace(yanitIcerik, "<[^>]+>", " ");
        if (ozet.Length > 300) ozet = ozet[..300] + "...";

        await _emailService.SendDestekYanitEmailAsync(
            talep.MusteriEmail,
            talep.MusteriAdi,
            talep.TalepNo,
            talep.Konu,
            ozet);
    }

    private async Task SendDurumDegisiklikEmailAsync(int talepId, DestekDurum eskiDurum, DestekDurum yeniDurum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var talep = await context.DestekTalepleri.AsNoTracking().FirstOrDefaultAsync(x => x.Id == talepId);
        if (talep == null || string.IsNullOrWhiteSpace(talep.MusteriEmail)) return;

        await _emailService.SendDestekDurumEmailAsync(
            talep.MusteriEmail,
            talep.MusteriAdi,
            talep.TalepNo,
            talep.Konu,
            GetDurumText(eskiDurum),
            GetDurumText(yeniDurum));
    }

    private async Task SendAtamaEmailAsync(int talepId, Kullanici kullanici)
    {
        if (string.IsNullOrWhiteSpace(kullanici.Email)) return;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var talep = await context.DestekTalepleri.AsNoTracking().FirstOrDefaultAsync(x => x.Id == talepId);
        if (talep == null) return;

        await _emailService.SendDestekAtamaEmailAsync(
            kullanici.Email,
            kullanici.AdSoyad,
            talep.TalepNo,
            talep.Konu,
            talep.MusteriAdi,
            talep.Oncelik.ToString());
    }

    private static string GetDurumText(DestekDurum durum) => durum switch
    {
        DestekDurum.Yeni => "Yeni",
        DestekDurum.Acik => "Açık",
        DestekDurum.YanitBekleniyor => "Yanıt Bekleniyor",
        DestekDurum.Beklemede => "Beklemede",
        DestekDurum.Cozuldu => "Çözüldü",
        DestekDurum.Kapali => "Kapalı",
        _ => durum.ToString()
    };

    #endregion
}



