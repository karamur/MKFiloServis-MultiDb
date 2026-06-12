using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class SoforService : ISoforService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;
    private readonly ICacheService _cache;
    private readonly NumaraSerisiService _numaraSerisi;

    public SoforService(IDbContextFactory<ApplicationDbContext> contextFactory, IMuhasebeService muhasebeService, ICacheService cache, NumaraSerisiService numaraSerisi)
    {
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
        _cache = cache;
        _numaraSerisi = numaraSerisi;
    }

    public Task<List<Sofor>> GetAllAsync() =>
        _cache.GetOrSetAsync(CacheKeys.SoforListesi, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var personeller = await QuerySoforler(context)
                .OrderBy(s => s.Ad)
                .ThenBy(s => s.Soyad)
                .ToListAsync();
            personeller.ForEach(NormalizeMaasBilgileri);
            return personeller;
        }, CacheDurations.Medium);

    public Task<List<Sofor>> GetActiveAsync() =>
        _cache.GetOrSetAsync(CacheKeys.SoforAktif, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var personeller = await QuerySoforler(context)
                .Where(s => s.Aktif)
                .OrderBy(s => s.Ad)
                .ThenBy(s => s.Soyad)
                .ToListAsync();
            personeller.ForEach(NormalizeMaasBilgileri);
            return personeller;
        }, CacheDurations.Medium);

    public async Task<int> GetActiveCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QuerySoforler(context)
            .Where(s => s.Aktif)
            .CountAsync();
    }

    public async Task<Sofor?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sofor = await QuerySoforler(context)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sofor != null)
            NormalizeMaasBilgileri(sofor);

        return sofor;
    }

    public async Task<Sofor> CreateAsync(Sofor sofor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        NormalizeSofor(sofor);
        ApplyMaasHesaplama(sofor);
        SyncBordroFlags(sofor);
        await ValidateSoforAsync(context, sofor);

        context.Soforler.Add(sofor);
        await context.SaveChangesAsync();

        // Personel için Cari kaydı oluştur (muhasebe akışının ana kaynağı Personeller tablosudur)
        await EnsurePersonelCariKaydiAsync(context, sofor);

        // 335/195 hesap bağlantılarını personel satırında garanti et
        await EnsurePersonelMuhasebeBaglantilariAsync(context, sofor.Id);

        var kaydedilen = await context.Soforler
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sofor.Id && !s.IsDeleted);

        if (kaydedilen != null)
        {
            sofor.MuhasebeHesapId = kaydedilen.MuhasebeHesapId;
            sofor.PersonelAvansHesapId = kaydedilen.PersonelAvansHesapId;
        }

        return sofor;
    }

    public async Task<Sofor> UpdateAsync(Sofor sofor, DateTime? expectedUpdatedAt = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        NormalizeSofor(sofor);
        ApplyMaasHesaplama(sofor);
        SyncBordroFlags(sofor);

        // Önce mevcut tracking'i temizle
        var trackedEntity = context.ChangeTracker.Entries<Sofor>()
            .FirstOrDefault(e => e.Entity.Id == sofor.Id);
        if (trackedEntity != null)
        {
            trackedEntity.State = EntityState.Detached;
        }

        // Mevcut kaydı oku (tracking ile)
        var existing = await QuerySoforler(context, asNoTracking: false)
            .FirstOrDefaultAsync(s => s.Id == sofor.Id);

        if (existing == null)
            throw new InvalidOperationException($"Personel bulunamadı. Id: {sofor.Id}");

        // Optimistic concurrency: expectedUpdatedAt verildiyse çakışma kontrolü yap
        if (expectedUpdatedAt.HasValue && existing.UpdatedAt.HasValue
            && !existing.UpdatedAt.Value.Equals(expectedUpdatedAt.Value))
        {
            throw new InvalidOperationException(
                $"Personel kaydı başka bir kullanıcı tarafından güncellenmiş. " +
                $"Lütfen sayfayı yenileyip tekrar deneyin. (Id: {sofor.Id})");
        }

        var existingTcKimlikNo = NormalizeTcKimlikNo(existing.TcKimlikNo);
        var existingSoforKodu = NormalizeSoforKodu(existing.SoforKodu);

        if (string.Equals(existingTcKimlikNo, sofor.TcKimlikNo, StringComparison.Ordinal))
        {
            sofor.TcKimlikNo = existingTcKimlikNo;
        }

        if (string.Equals(existingSoforKodu, sofor.SoforKodu, StringComparison.Ordinal))
        {
            sofor.SoforKodu = existingSoforKodu;
        }

        await ValidateSoforAsync(context, sofor, existing);

        var createdAt = existing.CreatedAt;
        var firmaDegisti = sofor.FirmaId.HasValue && sofor.FirmaId.Value > 0 && sofor.FirmaId != existing.FirmaId;

        context.Entry(existing).CurrentValues.SetValues(sofor);

        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // FK değişikliklerini direkt SQL ile garantile (EF SetValues FK'ları atlayabiliyor)
        if (firmaDegisti)
        {
            await context.Database.ExecuteSqlAsync(
                $"UPDATE \"Personeller\" SET \"FirmaId\" = {sofor.FirmaId!.Value}, \"UpdatedAt\" = {DateTime.UtcNow} WHERE \"Id\" = {sofor.Id}");
        }

        // Personel borç/avans hesaplarını personel satırında garanti et
        await EnsurePersonelMuhasebeBaglantilariAsync(context, existing.Id);

        var guncel = await context.Soforler
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == existing.Id && !s.IsDeleted);

        if (guncel != null)
        {
            existing.MuhasebeHesapId = guncel.MuhasebeHesapId;
            existing.PersonelAvansHesapId = guncel.PersonelAvansHesapId;
        }

        await _cache.RemoveByPrefixAsync(CacheKeys.SoforPrefix);
        return existing;
    }

    public async Task DeleteAsync(int id, int? deletedBy = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sofor = await QuerySoforler(context, asNoTracking: false)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sofor == null)
            throw new InvalidOperationException($"Personel bulunamadı. Id: {id}");

        var now = DateTime.UtcNow;
        sofor.Aktif = false;
        sofor.IsDeleted = true;
        sofor.DeletedAt = now;
        sofor.DeletedBy = deletedBy;
        sofor.UpdatedAt = now;
        await context.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync(CacheKeys.SoforPrefix);
    }

    /// <summary>
    /// Sadece seçili personelin SiralamaNo alanını günceller.
    /// Diğer alanlara (PersonelKodu, MuhasebeHesapId, PersonelAvansHesapId, araç eşleştirme, özlük evrak vb.) dokunmaz.
    /// </summary>
    public async Task<bool> UpdateSiraNoAsync(int personelId, int siraNo, CancellationToken cancellationToken = default)
    {
        if (personelId <= 0)
            throw new ArgumentException("Geçersiz personel Id.", nameof(personelId));

        if (siraNo < 0)
            throw new ArgumentException("Sıra No negatif olamaz.", nameof(siraNo));

        await using var context = await _contextFactory.CreateDbContextAsync();
        var affected = await context.Soforler
            .Where(x => x.Id == personelId && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.SiralamaNo, siraNo)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (affected != 1)
        {
            throw new InvalidOperationException(
                $"Sıra No güncellenemedi. PersonelId={personelId}, Affected={affected}. " +
                "Kayıt bulunamadı veya soft-delete edilmiş olabilir.");
        }

        await _cache.RemoveByPrefixAsync(CacheKeys.SoforPrefix);
        return true;
    }

    public async Task<Sofor> RestoreAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Soft delete filter'ı atlayarak silinmiş kaydı bul
        var sofor = await context.Soforler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted);

        if (sofor == null)
            throw new InvalidOperationException($"Silinmiş personel kaydı bulunamadı. Id: {id}");

        sofor.IsDeleted = false;
        sofor.DeletedAt = null;
        sofor.DeletedBy = null;
        sofor.Aktif = true;
        sofor.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        await _cache.RemoveByPrefixAsync(CacheKeys.SoforPrefix);
        return sofor;
    }

    public async Task<string> GenerateNextKodAsync()
    {
        return await GenerateNextKodAsync(PersonelGorev.Sofor);
    }

    public async Task<string> GenerateNextKodAsync(PersonelGorev gorev)
    {
        // Kural 15: Atomik numara üretimi (global — personel kodları firmalar arası benzersiz)
        var prefix = GetKodPrefix(gorev);
        var nextNumber = await _numaraSerisi.GenerateNextAsync(prefix, 0, "GLOBAL");
        return $"{prefix}-{nextNumber:D4}";
    }

    public static string GetKodPrefix(PersonelGorev gorev)
    {
        return gorev switch
        {
            PersonelGorev.Sofor => "SFR",
            PersonelGorev.Muhasebe => "MHS",
            PersonelGorev.Yonetici => "YNT",
            PersonelGorev.OfisCalisani => "PRS",
            PersonelGorev.Teknik => "TKN",
            PersonelGorev.Diger => "DGR",
            _ => "PRS"
        };
    }

    // Görev bazlı filtreleme metodları
    public async Task<List<Sofor>> GetByGorevAsync(PersonelGorev gorev)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personeller = await QuerySoforler(context)
            .Where(s => s.Gorev == gorev)
            .OrderBy(s => s.Ad)
            .ThenBy(s => s.Soyad)
            .ToListAsync();

        personeller.ForEach(NormalizeMaasBilgileri);
        return personeller;
    }

    public async Task<List<Sofor>> GetActiveSoforlerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personeller = await QuerySoforler(context)
            .Where(s => s.Aktif && s.Gorev == PersonelGorev.Sofor)
            .OrderBy(s => s.Ad)
            .ThenBy(s => s.Soyad)
            .ToListAsync();

        personeller.ForEach(NormalizeMaasBilgileri);
        return personeller;
    }

    public async Task<List<Sofor>> GetActiveByGorevAsync(PersonelGorev gorev)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personeller = await QuerySoforler(context)
            .Where(s => s.Aktif && s.Gorev == gorev)
            .OrderBy(s => s.Ad)
            .ThenBy(s => s.Soyad)
            .ToListAsync();

        personeller.ForEach(NormalizeMaasBilgileri);
        return personeller;
    }

    public async Task<int> GetActiveByGorevCountAsync(PersonelGorev gorev)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Soforler
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Aktif && s.Gorev == gorev)
            .CountAsync();
    }

    private static IQueryable<Sofor> QuerySoforler(ApplicationDbContext context, bool asNoTracking = true)
    {
        var query = context.Soforler
            .Include(s => s.Firma)
            .Include(s => s.AracAtamalari.Where(a => a.Aktif))
                .ThenInclude(a => a.Arac)
            .Where(s => !s.IsDeleted);
        return asNoTracking ? query.AsNoTracking() : query;
    }

    private async Task ValidateSoforAsync(ApplicationDbContext context, Sofor sofor, Sofor? existing = null)
    {
        var currentId = sofor.Id;
        var normalizedTcKimlikNo = NormalizeTcKimlikNo(sofor.TcKimlikNo);
        var normalizedSoforKodu = NormalizeSoforKodu(sofor.SoforKodu);
        var existingTcKimlikNo = NormalizeTcKimlikNo(existing?.TcKimlikNo);
        var existingSoforKodu = NormalizeSoforKodu(existing?.SoforKodu);

        if (string.IsNullOrWhiteSpace(sofor.SoforKodu))
            throw new InvalidOperationException("Personel kodu zorunludur.");

        if (string.IsNullOrWhiteSpace(sofor.Ad) || string.IsNullOrWhiteSpace(sofor.Soyad))
            throw new InvalidOperationException("Ad ve Soyad zorunludur.");

        if (sofor.IseBaslamaTarihi.HasValue && sofor.IstenAyrilmaTarihi.HasValue && sofor.IstenAyrilmaTarihi < sofor.IseBaslamaTarihi)
            throw new InvalidOperationException("İşten çıkış tarihi işe başlama tarihinden önce olamaz.");

        if (sofor.SGKBordroDahilMi && sofor.BordroTipiPersonel == PersonelBordroTipi.Yok)
            throw new InvalidOperationException("SGK bordroya dahil personel için bordro tipi seçilmelidir.");

        if (!string.IsNullOrWhiteSpace(normalizedTcKimlikNo))
        {
            if (normalizedTcKimlikNo.Length != 11 || !normalizedTcKimlikNo.All(char.IsDigit))
                throw new InvalidOperationException("TC Kimlik No 11 haneli olmalıdır.");

            var tcKimlikDegisti = existing == null || !string.Equals(existingTcKimlikNo, normalizedTcKimlikNo, StringComparison.Ordinal);

            // Kural 6 (FirmaId izolasyonu): TcKimlikNo firmaya özel unique olmalı
            var tcKimlikCakisanKayit = tcKimlikDegisti
                ? await context.Soforler
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id != currentId
                        && s.TcKimlikNo == normalizedTcKimlikNo
                        && s.FirmaId == sofor.FirmaId)
                : null;

            if (tcKimlikCakisanKayit != null)
            {
                var durum = tcKimlikCakisanKayit.IsDeleted ? "silinmiş" : "aktif";
                throw new InvalidOperationException($"'{normalizedTcKimlikNo}' TC Kimlik No zaten kullanımda. Çakışan kayıt Id: {tcKimlikCakisanKayit.Id} ({durum}).");
            }
        }

        var kodDegisti = existing == null || !string.Equals(existingSoforKodu, normalizedSoforKodu, StringComparison.Ordinal);

        var kodCakisanKayit = kodDegisti
            ? await context.Soforler
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id != currentId && s.SoforKodu == normalizedSoforKodu)
            : null;

        if (kodCakisanKayit != null)
        {
            var durum = kodCakisanKayit.IsDeleted ? "silinmiş" : "aktif";
            throw new InvalidOperationException($"'{normalizedSoforKodu}' personel kodu zaten kullanımda. Çakışan kayıt Id: {kodCakisanKayit.Id} ({durum}).");
        }
    }

    private static void NormalizeSofor(Sofor sofor)
    {
        sofor.SoforKodu = NormalizeSoforKodu(sofor.SoforKodu);
        sofor.Ad = string.IsNullOrWhiteSpace(sofor.Ad) ? string.Empty : sofor.Ad.Trim();
        sofor.Soyad = string.IsNullOrWhiteSpace(sofor.Soyad) ? string.Empty : sofor.Soyad.Trim();
        sofor.TcKimlikNo = NormalizeTcKimlikNo(sofor.TcKimlikNo);
        sofor.Telefon = NormalizeNullableText(sofor.Telefon)?.Replace(" ", string.Empty);
        sofor.Email = NormalizeNullableText(sofor.Email);
        sofor.Adres = NormalizeNullableText(sofor.Adres);
        sofor.Departman = NormalizeNullableText(sofor.Departman);
        sofor.Pozisyon = NormalizeNullableText(sofor.Pozisyon);
        sofor.EhliyetNo = NormalizeNullableText(sofor.EhliyetNo);
        sofor.BankaAdi = NormalizeNullableText(sofor.BankaAdi);
        sofor.IBAN = NormalizeNullableText(sofor.IBAN)?.Replace(" ", string.Empty).ToUpperInvariant();
        sofor.Notlar = NormalizeNullableText(sofor.Notlar);
    }

    private static string? NormalizeNullableText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeSoforKodu(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    private static string? NormalizeTcKimlikNo(string? value)
    {
        var normalized = NormalizeNullableText(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digitsOnly) ? null : digitsOnly;
    }

    private static void NormalizeMaasBilgileri(Sofor sofor)
    {
        // Orijinal NetMaas değerini sakla (ApplyMaasHesaplama sıfırlayabilir)
        var originalNetMaas = sofor.NetMaas;

        ApplyMaasHesaplama(sofor);

        // Eski kayıt: ResmiNetMaas ve DigerMaas hiç girilmemiş, sadece NetMaas var
        if (sofor.ResmiNetMaas == 0 && sofor.DigerMaas == 0 && originalNetMaas > 0)
        {
            sofor.ResmiNetMaas = originalNetMaas;
        }

        sofor.NetMaas = RoundCurrency(sofor.ResmiNetMaas + sofor.DigerMaas);
    }

    private static void ApplyMaasHesaplama(Sofor sofor)
    {
        sofor.CalismaMiktari = RoundCurrency(sofor.CalismaMiktari);
        sofor.BirimUcret = RoundCurrency(sofor.BirimUcret);

        if (sofor.BrutMaasHesaplamaTipi != BrutMaasHesaplamaTipi.Manuel)
        {
            sofor.BrutMaas = RoundCurrency(sofor.CalismaMiktari * sofor.BirimUcret);
        }
        else
        {
            sofor.BrutMaas = RoundCurrency(sofor.BrutMaas);
        }

        sofor.ResmiNetMaas = RoundCurrency(sofor.ResmiNetMaas);
        sofor.DigerMaas = RoundCurrency(sofor.DigerMaas);
        sofor.NetMaas = RoundCurrency(sofor.ResmiNetMaas + sofor.DigerMaas);
    }

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static void SyncBordroFlags(Sofor sofor)
    {
        // ArgePersoneli geriye dönük uyumluluk
        sofor.ArgePersoneli = sofor.SGKBordroDahilMi && sofor.BordroTipiPersonel == PersonelBordroTipi.Arge;
    }

    #region Excel Import/Export

    /// <summary>
    /// Personel import şablonu oluşturur (Excel)
    /// </summary>
    public Task<byte[]> GetImportSablonAsync()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Personel Import");

        // Başlık satırı
        var headers = new[]
        {
            "Ad*", "Soyad*", "TC Kimlik No", "Telefon", "Email", "Adres",
            "Görev (Sofor/OfisCalisani/Muhasebe/Yonetici/Teknik/Diger)", "Departman", "Pozisyon",
            "İşe Başlama (GG.AA.YYYY)", "Brüt Maaş", "Net Maaş",
            "SGK Bordrolu (Evet/Hayır)", "Bordro Tipi (Normal/Arge)",
            "Banka Adı", "IBAN", "Notlar"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Zorunlu alanları vurgula
            if (headers[i].EndsWith("*"))
            {
                cell.Style.Fill.BackgroundColor = XLColor.LightCoral;
            }
        }

        // Örnek veri satırı
        ws.Cell(2, 1).Value = "Ahmet";
        ws.Cell(2, 2).Value = "Yılmaz";
        ws.Cell(2, 3).Value = "12345678901";
        ws.Cell(2, 4).Value = "0532 123 4567";
        ws.Cell(2, 5).Value = "ahmet@firma.com";
        ws.Cell(2, 6).Value = "İstanbul";
        ws.Cell(2, 7).Value = "Sofor";
        ws.Cell(2, 8).Value = "Operasyon";
        ws.Cell(2, 9).Value = "Şoför";
        ws.Cell(2, 10).Value = DateTime.Now.ToString("dd.MM.yyyy");
        ws.Cell(2, 11).Value = 50000;
        ws.Cell(2, 12).Value = 35000;
        ws.Cell(2, 13).Value = "Evet";
        ws.Cell(2, 14).Value = "Normal";
        ws.Cell(2, 15).Value = "Ziraat Bankası";
        ws.Cell(2, 16).Value = "TR00 0000 0000 0000 0000 0000 00";
        ws.Cell(2, 17).Value = "Örnek personel";

        // Sütun genişlikleri
        ws.Columns().AdjustToContents();

        // Açıklama sayfası
        var helpWs = workbook.Worksheets.Add("Açıklamalar");
        helpWs.Cell(1, 1).Value = "PERSONEL IMPORT ŞABLONU AÇIKLAMALARI";
        helpWs.Cell(1, 1).Style.Font.Bold = true;
        helpWs.Cell(1, 1).Style.Font.FontSize = 14;

        helpWs.Cell(3, 1).Value = "Zorunlu Alanlar:";
        helpWs.Cell(3, 1).Style.Font.Bold = true;
        helpWs.Cell(4, 1).Value = "• Ad ve Soyad zorunludur (kırmızı arka plan)";

        helpWs.Cell(6, 1).Value = "Görev Değerleri:";
        helpWs.Cell(6, 1).Style.Font.Bold = true;
        helpWs.Cell(7, 1).Value = "• Sofor, OfisCalisani, Muhasebe, Yonetici, Teknik, Diger";

        helpWs.Cell(9, 1).Value = "Tarih Formatı:";
        helpWs.Cell(9, 1).Style.Font.Bold = true;
        helpWs.Cell(10, 1).Value = "• GG.AA.YYYY (örn: 15.03.2024)";

        helpWs.Cell(12, 1).Value = "SGK/Bordro:";
        helpWs.Cell(12, 1).Style.Font.Bold = true;
        helpWs.Cell(13, 1).Value = "• SGK Bordrolu: Evet/Hayır veya 1/0";
        helpWs.Cell(14, 1).Value = "• Bordro Tipi: Normal veya Arge";

        helpWs.Cell(16, 1).Value = "Önemli Not:";
        helpWs.Cell(16, 1).Style.Font.Bold = true;
        helpWs.Cell(17, 1).Value = "• TC Kimlik No ile mevcut personel kontrolü yapılır";
        helpWs.Cell(18, 1).Value = "• Aynı TC'li personel varsa güncelleme seçeneğine göre işlem yapılır";

        helpWs.Column(1).Width = 60;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    /// <summary>
    /// Excel dosyasından personel import eder
    /// </summary>
    public async Task<PersonelImportSonuc> ImportFromExcelAsync(byte[] excelData, bool mevcutGuncelle = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuc = new PersonelImportSonuc();

        using var stream = new MemoryStream(excelData);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        sonuc.ToplamSatir = Math.Max(0, lastRow - 1); // Başlık hariç

        // Mevcut personelleri TC'ye göre indexle
        var mevcutPersoneller = await context.Soforler
            .AsNoTracking()
            .ToDictionaryAsync(p => p.TcKimlikNo ?? $"ID_{p.Id}", p => p);

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var ad = ws.Cell(row, 1).GetString().Trim();
                var soyad = ws.Cell(row, 2).GetString().Trim();

                // Zorunlu alan kontrolü
                if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(soyad))
                {
                    sonuc.Hatalar.Add(new PersonelImportHata
                    {
                        SatirNo = row,
                        Kolon = "Ad/Soyad",
                        Mesaj = "Ad ve Soyad zorunludur",
                        Kritik = false
                    });
                    sonuc.Atlanan++;
                    continue;
                }

                var tcKimlik = ws.Cell(row, 3).GetString().Trim();

                // Mevcut personel kontrolü (TC ile)
                if (!string.IsNullOrWhiteSpace(tcKimlik) && mevcutPersoneller.TryGetValue(tcKimlik, out var mevcut))
                {
                    if (!mevcutGuncelle)
                    {
                        sonuc.Hatalar.Add(new PersonelImportHata
                        {
                            SatirNo = row,
                            Kolon = "TC Kimlik",
                            Mesaj = $"{tcKimlik} TC'li personel zaten mevcut: {mevcut.TamAd}",
                            Kritik = false
                        });
                        sonuc.Atlanan++;
                        continue;
                    }

                    // Mevcut personeli güncelle
                    await GuncellePersonelFromRow(context, mevcut.Id, ws, row);
                    sonuc.BasariliGuncellenen++;
                    continue;
                }

                // Yeni personel oluştur
                var personel = await OlusturPersonelFromRow(context, ws, row);
                sonuc.BasariliEklenen++;
            }
            catch (Exception ex)
            {
                sonuc.Hatalar.Add(new PersonelImportHata
                {
                    SatirNo = row,
                    Kolon = "Genel",
                    Mesaj = ex.Message,
                    Kritik = false
                });
                sonuc.Atlanan++;
            }
        }

        return sonuc;
    }

    private async Task<Sofor> OlusturPersonelFromRow(ApplicationDbContext context, IXLWorksheet ws, int row)
    {
        var gorevStr = ws.Cell(row, 7).GetString().Trim();
        var gorev = ParseGorev(gorevStr);

        var personel = new Sofor
        {
            SoforKodu = await GenerateNextKodAsync(gorev),
            Ad = ws.Cell(row, 1).GetString().Trim(),
            Soyad = ws.Cell(row, 2).GetString().Trim(),
            TcKimlikNo = ws.Cell(row, 3).GetString().Trim().NullIfEmpty(),
            Telefon = ws.Cell(row, 4).GetString().Replace(" ", string.Empty).Trim().NullIfEmpty(),
            Email = ws.Cell(row, 5).GetString().Trim().NullIfEmpty(),
            Adres = ws.Cell(row, 6).GetString().Trim().NullIfEmpty(),
            Gorev = gorev,
            Departman = ws.Cell(row, 8).GetString().Trim().NullIfEmpty(),
            Pozisyon = ws.Cell(row, 9).GetString().Trim().NullIfEmpty(),
            IseBaslamaTarihi = ParseTarih(ws.Cell(row, 10)),
            BrutMaas = ParseDecimal(ws.Cell(row, 11)),
            NetMaas = ParseDecimal(ws.Cell(row, 12)),
            SGKBordroDahilMi = ParseBool(ws.Cell(row, 13)),
            BordroTipiPersonel = ParseBordroTipi(ws.Cell(row, 14)),
            BankaAdi = ws.Cell(row, 15).GetString().Trim().NullIfEmpty(),
            IBAN = ws.Cell(row, 16).GetString().Trim().NullIfEmpty(),
            Notlar = ws.Cell(row, 17).GetString().Trim().NullIfEmpty(),
            Aktif = true
        };

        // SGK'lı ise bordro tipine göre AR-GE flag'i set et
        if (personel.SGKBordroDahilMi)
        {
            personel.ArgePersoneli = personel.BordroTipiPersonel == PersonelBordroTipi.Arge;
        }

        return await CreateAsync(personel);
    }

    private async Task GuncellePersonelFromRow(ApplicationDbContext context, int personelId, IXLWorksheet ws, int row)
    {
        var existing = await context.Soforler.FirstOrDefaultAsync(p => p.Id == personelId);
        if (existing == null) return;

        existing.Ad = ws.Cell(row, 1).GetString().Trim();
        existing.Soyad = ws.Cell(row, 2).GetString().Trim();
        existing.Telefon = ws.Cell(row, 4).GetString().Replace(" ", string.Empty).Trim().NullIfEmpty() ?? existing.Telefon;
        existing.Email = ws.Cell(row, 5).GetString().Trim().NullIfEmpty() ?? existing.Email;
        existing.Adres = ws.Cell(row, 6).GetString().Trim().NullIfEmpty() ?? existing.Adres;
        existing.Departman = ws.Cell(row, 8).GetString().Trim().NullIfEmpty() ?? existing.Departman;
        existing.Pozisyon = ws.Cell(row, 9).GetString().Trim().NullIfEmpty() ?? existing.Pozisyon;

        var yeniIseBaslama = ParseTarih(ws.Cell(row, 10));
        if (yeniIseBaslama.HasValue) existing.IseBaslamaTarihi = yeniIseBaslama;

        var brutMaas = ParseDecimal(ws.Cell(row, 11));
        if (brutMaas > 0) existing.BrutMaas = brutMaas;

        var netMaas = ParseDecimal(ws.Cell(row, 12));
        if (netMaas > 0) existing.NetMaas = netMaas;

        existing.BankaAdi = ws.Cell(row, 15).GetString().Trim().NullIfEmpty() ?? existing.BankaAdi;
        existing.IBAN = ws.Cell(row, 16).GetString().Trim().NullIfEmpty() ?? existing.IBAN;
        existing.Notlar = ws.Cell(row, 17).GetString().Trim().NullIfEmpty() ?? existing.Notlar;

        existing.UpdatedAt = DateTime.UtcNow;
        context.Soforler.Update(existing);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Mevcut personelleri Excel'e export eder
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personeller = await GetAllAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Personel Listesi");

        // Başlık
        ws.Cell(1, 1).Value = "PERSONEL LİSTESİ";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 15).Merge();

        ws.Cell(2, 1).Value = $"Oluşturma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}";
        ws.Range(2, 1, 2, 15).Merge();

        // Tablo başlıkları
        var headers = new[]
        {
            "Personel Kodu", "Ad", "Soyad", "TC Kimlik", "Telefon", "Email",
            "Görev", "Departman", "Pozisyon", "İşe Başlama", "Durum",
            "Brüt Maaş", "Net Maaş", "SGK Bordrolu", "Bordro Tipi"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Veri satırları
        int row = 5;
        foreach (var p in personeller.OrderBy(x => x.SiralamaNo == 0 ? int.MaxValue : x.SiralamaNo).ThenBy(x => x.Ad))
        {
            ws.Cell(row, 1).Value = p.SoforKodu;
            ws.Cell(row, 2).Value = p.Ad;
            ws.Cell(row, 3).Value = p.Soyad;
            ws.Cell(row, 4).Value = p.TcKimlikNo;
            ws.Cell(row, 5).Value = p.Telefon;
            ws.Cell(row, 6).Value = p.Email;
            ws.Cell(row, 7).Value = GetGorevAdi(p.Gorev);
            ws.Cell(row, 8).Value = p.Departman;
            ws.Cell(row, 9).Value = p.Pozisyon;
            ws.Cell(row, 10).Value = p.IseBaslamaTarihi?.ToString("dd.MM.yyyy");
            ws.Cell(row, 11).Value = p.Aktif ? "Aktif" : "Pasif";
            ws.Cell(row, 12).Value = p.BrutMaas;
            ws.Cell(row, 13).Value = p.NetMaas;
            ws.Cell(row, 14).Value = p.SGKBordroDahilMi ? "Evet" : "Hayır";
            ws.Cell(row, 15).Value = GetBordroTipiAdi(p.BordroTipiPersonel);

            ws.Cell(row, 12).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 13).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        // Özet
        row++;
        ws.Cell(row, 1).Value = "ÖZET";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = $"Toplam Personel: {personeller.Count}";
        row++;
        ws.Cell(row, 1).Value = $"Aktif Personel: {personeller.Count(p => p.Aktif)}";
        row++;
        ws.Cell(row, 1).Value = $"Toplam Net Maaş: {personeller.Where(p => p.Aktif).Sum(p => p.NetMaas):C0}";

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Helper methods
    private static PersonelGorev ParseGorev(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return PersonelGorev.Sofor;

        return value.ToLowerInvariant() switch
        {
            "sofor" or "şoför" => PersonelGorev.Sofor,
            "ofiscalisani" or "ofis çalışanı" or "ofis" => PersonelGorev.OfisCalisani,
            "muhasebe" => PersonelGorev.Muhasebe,
            "yonetici" or "yönetici" => PersonelGorev.Yonetici,
            "teknik" => PersonelGorev.Teknik,
            _ => PersonelGorev.Diger
        };
    }

    private static PersonelBordroTipi ParseBordroTipi(IXLCell cell)
    {
        var value = cell.GetString().Trim().ToLowerInvariant();
        return value switch
        {
            "arge" or "ar-ge" => PersonelBordroTipi.Arge,
            "normal" => PersonelBordroTipi.Normal,
            _ => PersonelBordroTipi.Yok
        };
    }

    private static DateTime? ParseTarih(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var dt))
            return dt;

        var str = cell.GetString().Trim();
        if (DateTime.TryParseExact(str, new[] { "dd.MM.yyyy", "dd/MM/yyyy", "yyyy-MM-dd" },
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;

        return null;
    }

    private static decimal ParseDecimal(IXLCell cell)
    {
        if (cell.TryGetValue<decimal>(out var d))
            return d;

        var str = cell.GetString().Trim().Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(str, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return 0;
    }

    private static bool ParseBool(IXLCell cell)
    {
        var value = cell.GetString().Trim().ToLowerInvariant();
        return value is "evet" or "yes" or "1" or "true" or "e";
    }

    private static string GetGorevAdi(PersonelGorev gorev) => gorev switch
    {
        PersonelGorev.Sofor => "Şoför",
        PersonelGorev.OfisCalisani => "Ofis Çalışanı",
        PersonelGorev.Muhasebe => "Muhasebe",
        PersonelGorev.Yonetici => "Yönetici",
        PersonelGorev.Teknik => "Teknik",
        _ => "Diğer"
    };

    private static string GetBordroTipiAdi(PersonelBordroTipi tip) => tip switch
    {
        PersonelBordroTipi.Normal => "Normal",
        PersonelBordroTipi.Arge => "AR-GE",
        _ => "Yok"
    };

    #endregion

    #region Muhasebe Hesap Otomasyonu

    /// <summary>
    /// Muhasebe hesabı olmayan tüm mevcut personellere toplu hesap oluşturur.
    /// </summary>
    public async Task<PersonelMuhasebeOnarimSonucu> TopluMuhasebeHesabiOlusturAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personeller = await context.Soforler
            .Where(s => !s.IsDeleted)
            .ToListAsync();

        var sonuc = new PersonelMuhasebeOnarimSonucu
        {
            ToplamPersonel = personeller.Count
        };

        foreach (var personel in personeller)
        {
            var borcHesapKodu = $"335.01.{personel.Id:D4}";
            var avansHesapKodu = $"195.01.{personel.Id:D4}";

            var onceBorcHesap = await context.MuhasebeHesaplari
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HesapKodu == borcHesapKodu && !h.IsDeleted);
            var onceAvansHesap = await context.MuhasebeHesaplari
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HesapKodu == avansHesapKodu && !h.IsDeleted);

            await EnsurePersonelBorcHesabiAsync(context, personel);
            await EnsurePersonelAvansHesabiAsync(context, personel);

            var sonraPersonel = await context.Soforler
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == personel.Id && !s.IsDeleted);
            var sonraCari = await context.Cariler
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SoforId == personel.Id && !c.IsDeleted);

            if (onceBorcHesap == null)
                sonuc.Olusturulan335Hesap++;
            if (onceAvansHesap == null)
                sonuc.Olusturulan195Hesap++;

            if (sonraPersonel?.MuhasebeHesapId.HasValue == true)
                sonuc.Baglanan335Personel++;

            if (sonraPersonel?.PersonelAvansHesapId.HasValue == true)
                sonuc.Baglanan195Cari++;
        }

        sonuc.NullKalanPersonelMuhasebeHesapId = await context.Soforler
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.MuhasebeHesapId == null);

        sonuc.NullKalanCariPersonelAvansHesapId = await context.Soforler
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.PersonelAvansHesapId == null);

        return sonuc;
    }

    /// <summary>
    /// Personelin muhasebe hesaplarını listeler (335 altındaki hesaplar)
    /// </summary>
    public async Task<List<MuhasebeHesap>> GetPersonelMuhasebeHesaplariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await context.MuhasebeAyarlari.AsNoTracking().FirstOrDefaultAsync();
        var prefix = string.IsNullOrWhiteSpace(ayar?.PersonelPrefix) ? "335.01" : ayar!.PersonelPrefix.Trim();

        return await context.MuhasebeHesaplari
            .Where(h => h.HesapKodu.StartsWith(prefix + ".") && !h.IsDeleted && h.Aktif)
            .OrderBy(h => h.HesapKodu)
            .ToListAsync();
    }

    /// <summary>
    /// Personel avans hesaplarını listeler (195.01 altındaki hesaplar)
    /// </summary>
    public async Task<List<MuhasebeHesap>> GetPersonelAvansHesaplariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await context.MuhasebeAyarlari.AsNoTracking().FirstOrDefaultAsync();
        var prefix = string.IsNullOrWhiteSpace(ayar?.PersonelAvansPrefix) ? "195.01" : ayar!.PersonelAvansPrefix.Trim();

        return await context.MuhasebeHesaplari
            .Where(h => h.HesapKodu.StartsWith(prefix + ".") && !h.IsDeleted && h.Aktif)
            .OrderBy(h => h.HesapKodu)
            .ToListAsync();
    }

    /// <summary>
    /// Belirli bir personelin avans hesabını getirir (Personeller.PersonelAvansHesap üzerinden)
    /// Önce PersonelAvansHesapId'ye bakar, yoksa deterministik kodla (195.01.{soforId:D4}) arar.
    /// </summary>
    public async Task<MuhasebeHesap?> GetPersonelAvansHesabiAsync(int soforId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var personel = await context.Soforler
            .Include(s => s.PersonelAvansHesap)
            .FirstOrDefaultAsync(s => s.Id == soforId && !s.IsDeleted);

        if (personel == null) return null;

        if (personel.PersonelAvansHesap != null) return personel.PersonelAvansHesap;

        // PersonelAvansHesapId null ise, deterministik kodla ara
        var deterministikKod = $"195.01.{soforId:D4}";

        var avansHesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu == deterministikKod && !h.IsDeleted);

        if (avansHesap != null)
        {
            // ── Personel tablosuna yaz (195 hesap Id) ──
            personel.PersonelAvansHesapId = avansHesap.Id;
            personel.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return avansHesap;
    }

    /// <summary>
    /// Belirli bir personelin borç hesabını getirir (Personeller.MuhasebeHesap - 335.xx.xxx)
    /// Önce Personel.MuhasebeHesapId'ye bakar, yoksa deterministik kodla (335.01.{soforId:D4}) arar.
    /// </summary>
    public async Task<MuhasebeHesap?> GetPersonelBorcHesabiAsync(int soforId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var personel = await context.Soforler
            .Include(s => s.MuhasebeHesap)
            .FirstOrDefaultAsync(s => s.Id == soforId && !s.IsDeleted);

        if (personel == null) return null;

        if (personel.MuhasebeHesap != null) return personel.MuhasebeHesap;

        var deterministikKod = $"335.01.{soforId:D4}";

        var borcHesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu == deterministikKod && !h.IsDeleted);

        if (borcHesap != null)
        {
            personel.MuhasebeHesapId = borcHesap.Id;
            personel.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return borcHesap;
    }

    /// <summary>
    /// Personel için Cari kaydı yoksa oluşturur. Avans hesabı ve muhasebe entegrasyonu için gereklidir.
    /// </summary>
    public async Task EnsurePersonelCariKaydiAsync(Sofor sofor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await EnsurePersonelCariKaydiAsync(context, sofor);
    }

    private async Task EnsurePersonelCariKaydiAsync(ApplicationDbContext context, Sofor sofor)
    {
        // Zaten var mı kontrol et
        var mevcutCari = await context.Cariler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.SoforId == sofor.Id);

        if (mevcutCari != null)
        {
            // Silinmişse geri getir
            if (mevcutCari.IsDeleted)
            {
                mevcutCari.IsDeleted = false;
                mevcutCari.DeletedAt = null;
                mevcutCari.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            return;
        }

        // Cari kodu üret (SFR-0001 gibi personel kodundan)
        var cariKodu = $"PRS-{sofor.SoforKodu}";
        // Aynı kodda Cari var mı?
        var kodVar = await context.Cariler.IgnoreQueryFilters()
            .AnyAsync(c => c.CariKodu == cariKodu);
        if (kodVar)
            cariKodu = $"{cariKodu}-{sofor.Id}";

        var yeniCari = new Cari
        {
            CariKodu = cariKodu,
            Unvan = sofor.TamAd,
            Telefon = sofor.Telefon,
            Email = sofor.Email,
            Adres = sofor.Adres,
            SoforId = sofor.Id,
            FirmaId = sofor.FirmaId,
            Aktif = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Cariler.Add(yeniCari);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Personel için otomatik borç hesabı oluşturur (335 prefix).
    /// Prefix ayarlardan, suffix otomatik artan sayaçtan alınır.
    /// Mevcut MuhasebeHesapId varsa ve adı personelle eşleşiyorsa dokunmaz,
    /// eşleşmiyorsa yeni hesap açar.
    /// </summary>
    public async Task EnsurePersonelBorcHesabiAsync(Sofor sofor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await EnsurePersonelBorcHesabiAsync(context, sofor);
    }

    private async Task EnsurePersonelBorcHesabiAsync(ApplicationDbContext context, Sofor sofor)
    {
        var dbSofor = await context.Soforler
            .FirstOrDefaultAsync(s => s.Id == sofor.Id && !s.IsDeleted);
        if (dbSofor == null)
            return;

        var tamAd = $"{dbSofor.Ad} {dbSofor.Soyad}";
        var hesapKodu = $"335.01.{dbSofor.Id:D4}";

        var hedefHesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu == hesapKodu && !h.IsDeleted);

        if (hedefHesap == null)
        {
            hedefHesap = new MuhasebeHesap
            {
                HesapKodu = hesapKodu,
                HesapAdi = tamAd,
                HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar,
                HesapTuru = HesapTuru.Pasif,
                AltHesapVar = false,
                SistemHesabi = false,
                Aktif = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            context.MuhasebeHesaplari.Add(hedefHesap);
            await context.SaveChangesAsync();
        }

        // ── Personel tablosuna yaz (335 hesap Id) ──
        dbSofor.MuhasebeHesapId = hedefHesap.Id;
        dbSofor.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var dogrulananPersonel = await context.Soforler
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dbSofor.Id && !s.IsDeleted);

        if (dogrulananPersonel?.MuhasebeHesapId != hedefHesap.Id)
            throw new InvalidOperationException($"335 hesap bağlantısı DB'ye yazılamadı. PersonelId={dbSofor.Id}, HesapId={hedefHesap.Id}");

        sofor.MuhasebeHesapId = hedefHesap.Id;
    }

    /// <summary>
    /// Personel için otomatik avans hesabı oluşturur (195 prefix).
    /// Prefix ayarlardan, suffix otomatik artan sayaçtan alınır.
    /// Önce isme göre arar, bulursa seçer, bulamazsa yeni oluşturur.
    /// </summary>
    public async Task EnsurePersonelAvansHesabiAsync(Sofor sofor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await EnsurePersonelAvansHesabiAsync(context, sofor);
    }

    private async Task EnsurePersonelAvansHesabiAsync(ApplicationDbContext context, Sofor sofor)
    {
        var tamAd = $"{sofor.Ad} {sofor.Soyad}";

        var hesapKodu = $"195.01.{sofor.Id:D4}";

        var hedefHesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu == hesapKodu && !h.IsDeleted);

        if (hedefHesap == null)
        {
            hedefHesap = new MuhasebeHesap
            {
                HesapKodu = hesapKodu,
                HesapAdi = tamAd,
                HesapGrubu = HesapGrubu.DonenVarliklar,
                HesapTuru = HesapTuru.Aktif,
                AltHesapVar = false,
                SistemHesabi = false,
                Aktif = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            context.MuhasebeHesaplari.Add(hedefHesap);
            await context.SaveChangesAsync();
        }

        // ── Personel tablosuna yaz (195 hesap Id) ──
        var dbSofor = await context.Soforler
            .FirstOrDefaultAsync(s => s.Id == sofor.Id && !s.IsDeleted);

        if (dbSofor == null)
            throw new InvalidOperationException($"Personel bulunamadı. Id={sofor.Id}");

        dbSofor.PersonelAvansHesapId = hedefHesap.Id;
        dbSofor.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var dogrulananPersonel = await context.Soforler
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sofor.Id && !s.IsDeleted);

        if (dogrulananPersonel?.PersonelAvansHesapId != hedefHesap.Id)
            throw new InvalidOperationException($"195 hesap bağlantısı DB'ye yazılamadı. PersonelId={sofor.Id}, HesapId={hedefHesap.Id}");
    }

    /// <summary>
    /// Personelin manuel seçilen avans hesabını personel kartına atar.
    /// </summary>
    public async Task AvansHesabiAtaAsync(int soforId, int avansHesapId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var personel = await context.Soforler
            .FirstOrDefaultAsync(s => s.Id == soforId && !s.IsDeleted);

        if (personel == null)
            throw new InvalidOperationException($"Personel bulunamadı. Id: {soforId}");

        personel.PersonelAvansHesapId = avansHesapId;
        personel.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Personel 335/195 muhasebe hesap bağlantılarını ExecuteUpdateAsync ile doğrudan DB'ye yazar.
    /// Entity tracking'e güvenmez; UI modelinden gelen null FK değerlerinden etkilenmez.
    /// </summary>
    private async Task EnsurePersonelMuhasebeBaglantilariAsync(ApplicationDbContext context, int personelId)
    {
        // ── 1. Personeli DB'den (tracking olmadan) oku ──
        var dbPersonel = await context.Soforler
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == personelId && !x.IsDeleted);

        if (dbPersonel == null)
            throw new InvalidOperationException($"Personel bulunamadı. PersonelId={personelId}");

        var adSoyad = $"{dbPersonel.Ad} {dbPersonel.Soyad}".Trim();

        var hesap335Kodu = $"335.01.{personelId:D4}";
        var hesap195Kodu = $"195.01.{personelId:D4}";

        // ── 2. Hesapları bul veya oluştur ──
        var hesap335 = await GetOrCreatePersonelMuhasebeHesabiAsync(
            context,
            hesap335Kodu,
            adSoyad,
            HesapGrubu.KisaVadeliYabanciKaynaklar,
            HesapTuru.Pasif);

        var hesap195 = await GetOrCreatePersonelMuhasebeHesabiAsync(
            context,
            hesap195Kodu,
            adSoyad,
            HesapGrubu.DonenVarliklar,
            HesapTuru.Aktif);

        // ── 3. ExecuteUpdateAsync ile DOĞRUDAN DB'ye yaz ──
        // Bu yöntem entity tracking'i bypass eder; SetValues ile null'a çekilmiş
        // olsa bile DB'deki değeri garanti olarak günceller.
        var updated = await context.Soforler
            .IgnoreQueryFilters()
            .Where(x => x.Id == personelId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.MuhasebeHesapId, hesap335.Id)
                .SetProperty(x => x.PersonelAvansHesapId, hesap195.Id)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

        if (updated != 1)
            throw new InvalidOperationException(
                $"Personel 335/195 ExecuteUpdate başarısız. PersonelId={personelId}, UpdatedRows={updated}");

        // ── 4. DB'den tekrar oku ve doğrula ──
        var kontrol = await context.Soforler
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == personelId);

        if (kontrol == null)
            throw new InvalidOperationException($"Personel doğrulama kaydı bulunamadı. PersonelId={personelId}");

        if (kontrol.MuhasebeHesapId != hesap335.Id)
        {
            throw new InvalidOperationException(
                $"335 hesap bağlantısı DB'ye yazılamadı. PersonelId={personelId}, BeklenenHesapId={hesap335.Id}, DbMuhasebeHesapId={kontrol.MuhasebeHesapId}");
        }

        if (kontrol.PersonelAvansHesapId != hesap195.Id)
        {
            throw new InvalidOperationException(
                $"195 hesap bağlantısı DB'ye yazılamadı. PersonelId={personelId}, BeklenenHesapId={hesap195.Id}, DbPersonelAvansHesapId={kontrol.PersonelAvansHesapId}");
        }
    }

    private static async Task<MuhasebeHesap> GetOrCreatePersonelMuhasebeHesabiAsync(
        ApplicationDbContext context,
        string hesapKodu,
        string adSoyad,
        HesapGrubu hesapGrubu,
        HesapTuru hesapTuru)
    {
        var hesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(x => x.HesapKodu == hesapKodu && !x.IsDeleted);

        if (hesap != null)
            return hesap;

        hesap = new MuhasebeHesap
        {
            HesapKodu = hesapKodu,
            HesapAdi = adSoyad,
            HesapGrubu = hesapGrubu,
            HesapTuru = hesapTuru,
            AltHesapVar = false,
            SistemHesabi = false,
            Aktif = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.MuhasebeHesaplari.Add(hesap);
        await context.SaveChangesAsync();

        if (hesap.Id <= 0)
            throw new InvalidOperationException($"Muhasebe hesabı oluşturulamadı. Kod={hesapKodu}");

        return hesap;
    }

    #endregion
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
