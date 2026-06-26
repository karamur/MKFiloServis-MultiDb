using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Globalization;

namespace KOAFiloServis.Web.Services;

public class KiralikPlakaTakipService : IKiralikPlakaTakipService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public KiralikPlakaTakipService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<KiralikPlakaTakip>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.KiralikPlakaTakipler
            .Include(x => x.Arac)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task<KiralikPlakaTakip?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.KiralikPlakaTakipler
            .Include(x => x.Arac)
            .Include(x => x.FaturaDetaylari)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity != null)
        {
            entity.FaturaDetaylari = entity.FaturaDetaylari
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Yil)
                .ThenBy(x => x.Ay)
                .ThenBy(x => x.Sira)
                .ToList();
        }

        return entity;
    }

    public async Task<KiralikPlakaTakip> CreateAsync(KiralikPlakaTakip entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await EnsureNoDuplicateDonemAsync(context, entity);

        var now = DateTime.UtcNow;
        entity.CreatedAt = now;
        entity.AracId ??= await TryFindUniqueActiveAracIdByPlakaAsync(context, entity.Plaka);

        if (entity.FaturaDetaylari != null)
        {
            foreach (var detay in entity.FaturaDetaylari)
            {
                detay.Id = 0;
                detay.KiralikPlakaTakipId = 0;
                detay.CreatedAt = now;
                detay.UpdatedAt = null;
                detay.IsDeleted = false;
                detay.BazPlanTutari = detay.BazPlanTutari;
                detay.EkOdemeTutari = detay.EkOdemeTutari;
                detay.PlanTutari = detay.PlanTutari;
                detay.FaturaTarihi = detay.FaturaTarihi?.Date;
            }
        }

        OzetAlanlariFaturaDetaylarindanUret(entity);
        ApplyCalculatedFields(entity);

        context.KiralikPlakaTakipler.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<KiralikPlakaTakip> UpdateAsync(KiralikPlakaTakip entity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.KiralikPlakaTakipler
            .Include(x => x.FaturaDetaylari)
            .AsTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == entity.Id);
        if (existing == null) throw new InvalidOperationException("Kayıt bulunamadı.");

        existing.Plaka = entity.Plaka;
        // Manuel seçim öncelikli: kullanıcı eşleşmeyi kaldırdıysa AracId=null korunur.
        existing.AracId = entity.AracId;
        existing.IsimSoyisim = entity.IsimSoyisim;
        existing.BaslamaTarihi = entity.BaslamaTarihi;
        existing.BitisTarihi = entity.BitisTarihi;
        existing.Durum = entity.Durum;
        existing.KasaDurumu = entity.KasaDurumu;
        existing.FaturaOdemesi = entity.FaturaOdemesi;
        existing.Periyot = entity.Periyot;
        existing.OdemeSayisi = Math.Max(1, entity.OdemeSayisi);
        existing.AylikVeyaYillikTutar = entity.AylikVeyaYillikTutar;
        existing.EkTutar = entity.EkTutar;
        existing.KesilenFaturaNo = entity.KesilenFaturaNo;
        existing.KesilenFaturaTarih = entity.KesilenFaturaTarih?.Date;
        existing.KesilenFaturaTutar = entity.KesilenFaturaTutar;
        existing.KalanFaturaTutar = entity.KalanFaturaTutar;
        existing.GelenFaturaId = entity.GelenFaturaId <= 0 ? null : entity.GelenFaturaId;
        existing.ToplamOdeme = entity.ToplamOdeme;
        existing.OdenenTutar = entity.OdenenTutar;
        existing.SonOdemeTarihi = entity.SonOdemeTarihi?.Date;

        var now = DateTime.UtcNow;
        foreach (var eskiDetay in existing.FaturaDetaylari.Where(x => !x.IsDeleted))
        {
            eskiDetay.IsDeleted = true;
            eskiDetay.UpdatedAt = now;
        }

        if (entity.FaturaDetaylari != null)
        {
            foreach (var detay in entity.FaturaDetaylari.Where(x => !x.IsDeleted))
            {
                existing.FaturaDetaylari.Add(new KiralikPlakaTakipFatura
                {
                    Sira = detay.Sira,
                    Yil = detay.Yil,
                    Ay = detay.Ay,
                    BazPlanTutari = detay.BazPlanTutari,
                    EkOdemeTutari = detay.EkOdemeTutari,
                    PlanTutari = detay.PlanTutari,
                    FaturaNo = detay.FaturaNo,
                    FaturaTarihi = detay.FaturaTarihi?.Date,
                    FaturaTutari = detay.FaturaTutari,
                    BuAyOdenen = detay.BuAyOdenen,
                    CreatedAt = now
                });
            }
        }

        OzetAlanlariFaturaDetaylarindanUret(existing);
        ApplyCalculatedFields(existing);
        existing.UpdatedAt = now;

        await context.SaveChangesAsync();
        return existing;
    }


    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.KiralikPlakaTakipler.FindAsync(id);
        if (existing != null)
        {
            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
    public async Task<byte[]> GetExcelSablonAsync()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("KiralikCPlakaTakip");

        var headers = new[] { "PLAKA", "İSİM SOYİSİM", "BAŞLAMA TARİHİ", "BİTİŞ TARİHİ", "DURUM", "KASA DURUMU", "FATURA", "AYLIK / YILLIK", "TUTAR", "EK TUTAR", "TOPLAM" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        }

        ws.Cell(2, 1).Value = "06 C 0640";
        ws.Cell(2, 2).Value = "HAKAN YILDIZ";
        ws.Cell(2, 3).Value = new DateTime(2025, 8, 1);
        ws.Cell(2, 4).Value = new DateTime(2026, 7, 31);
        ws.Cell(2, 5).Value = "ÖNÜ AÇIK";
        ws.Cell(2, 6).Value = "PLAKA";
        ws.Cell(2, 7).Value = 66000;
        ws.Cell(2, 8).Value = "AYLIK";
        ws.Cell(2, 9).Value = 66000;
        ws.Cell(2, 10).Value = 0;
        // TOPLAM = FATURA + EK TUTAR (formül)
        ws.Cell(2, 11).FormulaA1 = "=G2+J2";

        // Açıklama satırı (bilgilendirme)
        ws.Cell(4, 1).Value = "NOT: TOPLAM otomatik hesaplanır = FATURA + EK TUTAR";
        ws.Cell(4, 1).Style.Font.Italic = true;
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(4, 1, 4, 11).Merge();

        ws.Range(2, 3, 200, 4).Style.DateFormat.Format = "dd.MM.yyyy";
        ws.Range(2, 7, 200, 7).Style.NumberFormat.Format = "#,##0.00";
        ws.Range(2, 9, 200, 11).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<KiralikPlakaImportResult> ImportFromExcelAsync(byte[] fileContent)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new KiralikPlakaImportResult();

        try
        {
            using var stream = new MemoryStream(fileContent);
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            var excelPlakaSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int row = 2; row <= lastRow; row++)
            {
                try
                {
                    var plaka = ws.Cell(row, 1).GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(plaka))
                        continue;

                    if (!excelPlakaSet.Add(plaka))
                    {
                        result.SkippedRecords.Add($"Satır {row}: Excel içinde mükerrer plaka ({plaka}) atlandı.");
                        result.SkippedCount++;
                        continue;
                    }

                    var isimSoyisim = ws.Cell(row, 2).GetString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(isimSoyisim))
                    {
                        result.Errors.Add($"Satır {row}: İsim Soyisim boş olamaz.");
                        continue;
                    }

                    DateTime baslama = ParseDate(ws.Cell(row, 3));
                    DateTime bitis = ParseDate(ws.Cell(row, 4));
                    var durum = ws.Cell(row, 5).GetString()?.Trim();
                    var kasaDurumu = ws.Cell(row, 6).GetString()?.Trim();
                    decimal fatura = ParseDecimal(ws.Cell(row, 7));
                    var periyotHam = ws.Cell(row, 8).GetString()?.Trim();
                    var periyot = NormalizePeriyot(periyotHam);
                    if (periyot is null)
                    {
                        result.Errors.Add($"Satır {row}: Geçersiz periyot değeri ({periyotHam}). Sadece AYLIK/YILLIK kullanılabilir.");
                        continue;
                    }
                    decimal tutar = ParseDecimal(ws.Cell(row, 9));
                    decimal ekTutar = ParseDecimal(ws.Cell(row, 10));

                    var normalizedPlaka = NormalizePlaka(plaka);
                    var existing = await FindByNormalizedPlakaAsync(context, normalizedPlaka);
                    if (existing == null)
                    {
                        var entity = new KiralikPlakaTakip
                        {
                            Plaka = plaka,
                            IsimSoyisim = isimSoyisim,
                            BaslamaTarihi = baslama,
                            BitisTarihi = bitis,
                            Durum = string.IsNullOrWhiteSpace(durum) ? "ÖNÜ AÇIK" : durum,
                            KasaDurumu = string.IsNullOrWhiteSpace(kasaDurumu) ? "PLAKA" : kasaDurumu,
                            FaturaOdemesi = fatura,
                            Periyot = periyot,
                            OdemeSayisi = HesaplaOdemeSayisi(baslama, bitis, periyot),
                            AylikVeyaYillikTutar = tutar,
                            EkTutar = ekTutar,
                            AracId = await TryFindUniqueActiveAracIdByPlakaAsync(context, plaka),
                            CreatedAt = DateTime.UtcNow
                        };
                        await EnsureNoDuplicateDonemAsync(context, entity);
                        ApplyCalculatedFields(entity);
                        context.KiralikPlakaTakipler.Add(entity);
                        result.ImportedCount++;
                    }
                    else
                    {
                        existing.IsimSoyisim = isimSoyisim;
                        existing.BaslamaTarihi = baslama;
                        existing.BitisTarihi = bitis;
                        existing.Durum = string.IsNullOrWhiteSpace(durum) ? existing.Durum : durum;
                        existing.KasaDurumu = string.IsNullOrWhiteSpace(kasaDurumu) ? existing.KasaDurumu : kasaDurumu;
                        existing.FaturaOdemesi = fatura;
                        existing.Periyot = periyot;
                        existing.OdemeSayisi = HesaplaOdemeSayisi(baslama, bitis, periyot);
                        existing.AylikVeyaYillikTutar = tutar;
                        existing.EkTutar = ekTutar;
                        existing.AracId ??= await TryFindUniqueActiveAracIdByPlakaAsync(context, plaka);
                        existing.UpdatedAt = DateTime.UtcNow;
                        result.UpdatedCount++;
                    }
                }
                catch (Exception exRow)
                {
                    result.Errors.Add($"Satır {row}: {exRow.Message}");
                }
            }

            await context.SaveChangesAsync();
            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Excel import hatası: {ex.Message}");
            result.Success = false;
        }

        return result;
    }

    private static DateTime ParseDate(IXLCell cell)
    {
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime();

        var text = cell.GetString();
        if (DateTime.TryParseExact(text, new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd" }, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(text, out var genericDt))
            return genericDt;

        throw new InvalidOperationException($"Geçersiz tarih: {text}");
    }

    private static string NormalizePlaka(string? plaka)
    {
        if (string.IsNullOrWhiteSpace(plaka)) return string.Empty;
        // Boşluk, tire ve diğer ayraçları temizle, büyük harfe çevir
        return new string(plaka.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private static async Task<int?> TryFindUniqueActiveAracIdByPlakaAsync(ApplicationDbContext context, string? plaka)
    {
        var p = NormalizePlaka(plaka);
        if (string.IsNullOrEmpty(p)) return null;

        // Aktif ve silinmemiş araçlardan normalize aktif plaka eşleşmeleri
        var aktifPlakaAdaylari = await context.Araclar
            .Where(a => !a.IsDeleted && a.Aktif && a.AktifPlaka != null
                && a.AktifPlaka.ToUpper().Replace(" ", "").Replace("-", "") == p)
            .Select(a => a.Id)
            .ToListAsync();

        if (aktifPlakaAdaylari.Count == 1)
            return aktifPlakaAdaylari[0];

        if (aktifPlakaAdaylari.Count > 1)
            return null;

        // Aktif araçların plaka geçmişinde eşleşme ara
        var gecmisAdaylari = await context.AracPlakalar
            .Where(ap => !ap.IsDeleted && ap.Plaka != null
                && ap.Plaka.ToUpper().Replace(" ", "").Replace("-", "") == p
                && ap.Arac != null && !ap.Arac.IsDeleted && ap.Arac.Aktif)
            .OrderByDescending(ap => ap.GirisTarihi)
            .Select(ap => ap.AracId)
            .Distinct()
            .ToListAsync();

        return gecmisAdaylari.Count == 1 ? gecmisAdaylari[0] : null;
    }

    private static async Task<(int? AracId, bool CokluAday, bool PasifAdayVar)> EvaluateAracMatchByPlakaAsync(ApplicationDbContext context, string? plaka)
    {
        var p = NormalizePlaka(plaka);
        if (string.IsNullOrEmpty(p)) return (null, false, false);

        var tumAktifPlakaAdaylari = await context.Araclar
            .Where(a => !a.IsDeleted && a.AktifPlaka != null
                && a.AktifPlaka.ToUpper().Replace(" ", "").Replace("-", "") == p)
            .Select(a => new { a.Id, a.Aktif })
            .ToListAsync();

        var aktifPlakaAdaylari = tumAktifPlakaAdaylari.Where(x => x.Aktif).Select(x => x.Id).Distinct().ToList();
        var pasifAdayVar = tumAktifPlakaAdaylari.Any(x => !x.Aktif);

        if (aktifPlakaAdaylari.Count > 1)
            return (null, true, pasifAdayVar);

        if (aktifPlakaAdaylari.Count == 1)
            return (aktifPlakaAdaylari[0], false, pasifAdayVar);

        var tumGecmisAdaylari = await context.AracPlakalar
            .Where(ap => !ap.IsDeleted && ap.Plaka != null
                && ap.Plaka.ToUpper().Replace(" ", "").Replace("-", "") == p
                && ap.Arac != null && !ap.Arac.IsDeleted)
            .OrderByDescending(ap => ap.GirisTarihi)
            .Select(ap => new { ap.AracId, ap.Arac!.Aktif })
            .ToListAsync();

        var aktifGecmisAdaylari = tumGecmisAdaylari.Where(x => x.Aktif).Select(x => x.AracId).Distinct().ToList();
        pasifAdayVar = pasifAdayVar || tumGecmisAdaylari.Any(x => !x.Aktif);

        if (aktifGecmisAdaylari.Count > 1)
            return (null, true, pasifAdayVar);

        if (aktifGecmisAdaylari.Count == 1)
            return (aktifGecmisAdaylari[0], false, pasifAdayVar);

        return (null, false, pasifAdayVar);
    }

    public async Task<AracEslestirmeSonuc> EslestirmeYapAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var eslesmeyenler = await context.KiralikPlakaTakipler
            .Where(x => !x.IsDeleted && x.AracId == null)
            .ToListAsync();

        var sonuc = new AracEslestirmeSonuc();

        foreach (var kayit in eslesmeyenler)
        {
            var degerlendirme = await EvaluateAracMatchByPlakaAsync(context, kayit.Plaka);
            if (degerlendirme.AracId.HasValue)
            {
                kayit.AracId = degerlendirme.AracId.Value;
                kayit.UpdatedAt = DateTime.UtcNow;
                sonuc.Eslesen++;
            }
            else if (degerlendirme.CokluAday)
            {
                sonuc.CokluAdayNedeniyleAtlanan++;
            }
            else if (degerlendirme.PasifAdayVar)
            {
                sonuc.PasifAracNedeniyleAtlanan++;
            }
            else
            {
                sonuc.EslesmeBulunamayan++;
            }
        }

        if (sonuc.Eslesen > 0) await context.SaveChangesAsync();
        return sonuc;
    }

    public async Task<int> FaturaEslestirmeYapAsync()
    {
        // FAZ: Manuel fatura girişi. Otomatik fatura eşleştirme devre dışı.
        await Task.CompletedTask;
        return 0;
    }


    private static string? NormalizePeriyot(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "AYLIK";
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized is "AYLIK" or "AY") return "AYLIK";
        if (normalized is "YILLIK" or "YIL") return "YILLIK";
        return null;
    }

    private static void OzetAlanlariFaturaDetaylarindanUret(KiralikPlakaTakip entity)
    {
        if (entity.FaturaDetaylari == null || entity.FaturaDetaylari.Count == 0)
            return;

        var aktifDetaylar = entity.FaturaDetaylari
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Yil)
            .ThenBy(x => x.Ay)
            .ThenBy(x => x.Sira)
            .ToList();

        if (aktifDetaylar.Count == 0)
            return;

        var birlesikFaturaNo = string.Join("; ", aktifDetaylar
            .Select(x => x.FaturaNo?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        if (birlesikFaturaNo.Length > 50)
            birlesikFaturaNo = birlesikFaturaNo[..50];

        var tarihliDetaylar = aktifDetaylar
            .Where(x => x.FaturaTarihi.HasValue)
            .Select(x => x.FaturaTarihi!.Value.Date)
            .Distinct()
            .ToList();

        entity.KesilenFaturaNo = string.IsNullOrWhiteSpace(birlesikFaturaNo) ? null : birlesikFaturaNo;
        entity.KesilenFaturaTutar = aktifDetaylar.Sum(x => x.FaturaTutari);
        entity.ToplamOdeme = aktifDetaylar.Sum(x => x.PlanTutari > 0 ? x.PlanTutari : x.FaturaTutari);
        entity.EkTutar = aktifDetaylar.Sum(x => x.EkOdemeTutari);
        entity.OdenenTutar = aktifDetaylar.Sum(x => x.BuAyOdenen);
        entity.KesilenFaturaTarih = tarihliDetaylar.Count == 1 ? tarihliDetaylar[0] : null;
        entity.FaturaOdemesi = aktifDetaylar.Sum(x => x.BazPlanTutari > 0 ? x.BazPlanTutari : x.FaturaTutari);
        entity.KalanFaturaTutar = Math.Max(0, entity.ToplamOdeme - entity.OdenenTutar);
        entity.GelenFaturaId = null;
    }

    private static void ApplyCalculatedFields(KiralikPlakaTakip entity)
    {
        if (entity.ToplamOdeme <= 0)
            entity.ToplamOdeme = entity.Toplam;

        if (entity.OdemeSayisi <= 0)
            entity.OdemeSayisi = HesaplaOdemeSayisi(entity.BaslamaTarihi, entity.BitisTarihi, entity.Periyot);

        entity.OdemeSayisi = Math.Max(1, entity.OdemeSayisi);
        entity.EkTutar = Math.Max(0, entity.EkTutar);
        entity.KalanFaturaTutar = Math.Max(0, entity.ToplamOdeme - entity.OdenenTutar);
    }

    private static int HesaplaOdemeSayisi(DateTime baslangic, DateTime bitis, string? periyot)
    {
        if (baslangic == default || bitis == default || bitis < baslangic)
            return 1;

        return string.Equals(periyot, "YILLIK", StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, (bitis.Year - baslangic.Year) + 1)
            : Math.Max(1, ((bitis.Year - baslangic.Year) * 12) + bitis.Month - baslangic.Month + 1);
    }

    private static async Task EnsureNoDuplicateDonemAsync(ApplicationDbContext context, KiralikPlakaTakip entity)
    {
        var p = NormalizePlaka(entity.Plaka);
        if (string.IsNullOrEmpty(p))
            throw new InvalidOperationException("Plaka boş olamaz.");

        var sameDonemCandidates = await context.KiralikPlakaTakipler
            .Where(x => !x.IsDeleted && x.Id != entity.Id)
            .Where(x =>
                x.BaslamaTarihi.Date == entity.BaslamaTarihi.Date &&
                x.BitisTarihi.Date == entity.BitisTarihi.Date &&
                x.Periyot == entity.Periyot)
            .Select(x => x.Plaka)
            .ToListAsync();

        var hasDuplicate = sameDonemCandidates.Any(existingPlaka => NormalizePlaka(existingPlaka) == p);

        if (hasDuplicate)
            throw new InvalidOperationException("Aynı plaka ve ödeme dönemi için kayıt zaten mevcut.");
    }

    private static async Task<KiralikPlakaTakip?> FindByNormalizedPlakaAsync(ApplicationDbContext context, string normalizedPlaka)
    {
        if (string.IsNullOrWhiteSpace(normalizedPlaka)) return null;

        var sqlNormalized = normalizedPlaka.Replace(" ", "").Replace("-", "");
        var candidates = await context.KiralikPlakaTakipler
            .Where(x => !x.IsDeleted && x.Plaka != null
                && x.Plaka.ToUpper().Replace(" ", "").Replace("-", "") == sqlNormalized)
            .OrderByDescending(x => x.Id)
            .Take(20)
            .ToListAsync();

        return candidates.FirstOrDefault(x => NormalizePlaka(x.Plaka) == normalizedPlaka)
               ?? candidates.FirstOrDefault();
    }

    private static decimal ParseDecimal(IXLCell cell)
    {
        if (cell.DataType == XLDataType.Number)
            return (decimal)cell.GetDouble();

        var text = cell.GetString()?.Trim() ?? "0";
        text = text.Replace("₺", "").Replace(" ", "");

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out var trValue))
            return trValue;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var invValue))
            return invValue;

        return 0;
    }
}







