using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Hakedis/Puantaj kayıt istatistikleri
/// </summary>
public class HakedisIstatistikleri
{
    public int ToplamKayit { get; set; }
    public int TaslakKayit { get; set; }
    public int OnayliKayit { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal ToplamAlinacak { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal ToplamOdenecek { get; set; }
    public decimal ToplamFark { get; set; }
    public int FaturasizGelir { get; set; }
    public int FaturasizGider { get; set; }
    public int OdenmemisGelir { get; set; }
    public int OdenmemisGider { get; set; }
}

/// <summary>
/// Excel import önizleme satırı
/// </summary>
public class HakedisExcelSatiri
{
    public int SatirNo { get; set; }
    public string? KurumAdi { get; set; }
    public string? GuzergahAdi { get; set; }
    public string? Yon { get; set; }
    public string? Plaka { get; set; }
    public string? SoforAdi { get; set; }
    public string? SoforTelefon { get; set; }
    public string? FaturaKesiciAdi { get; set; }
    public string? FaturaKesiciTelefon { get; set; }
    public decimal Gun { get; set; }

    // Gelir alanları
    public decimal Gelir { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal GelirKdv20 { get; set; }
    public decimal GelirKdv10 { get; set; }
    public decimal GelirKesinti { get; set; }
    public decimal Alinacak { get; set; }

    // Gider alanları
    public decimal Gider { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal GiderKdv20 { get; set; }
    public decimal GiderKdv10 { get; set; }
    public decimal GiderKesinti { get; set; }
    public decimal Odenecek { get; set; }

    // Fark
    public decimal Fark => Alinacak - Odenecek;

    // Eşleştirme durumları
    public int? KurumCariId { get; set; }
    public int? GuzergahId { get; set; }
    public int? AracId { get; set; }
    public int? SoforId { get; set; }
    public int? FaturaKesiciCariId { get; set; }

    public bool KurumEslesti { get; set; }
    public bool GuzergahEslesti { get; set; }
    public bool AracEslesti { get; set; }
    public bool SoforEslesti { get; set; }
    public bool FaturaKesiciEslesti { get; set; }

    public bool Gecerli => !string.IsNullOrEmpty(KurumAdi) && !string.IsNullOrEmpty(GuzergahAdi);
    public string? HataMesaji { get; set; }
}

/// <summary>
/// Puantaj Excel şablon satırı (günlük puantajlı format)
/// Bölge | SıraNo | Semt(Güzergah) | Sefer Fiyatı | S/A | Plaka | Şoför | Şoför Tel | Firma Adı | Gün1...Gün31 | Sefer Günü Top | Toplam | KDV20 | KDV10 | Kesinti | Ödenecek
/// </summary>
public class PuantajSablonSatiri
{
    public int SatirNo { get; set; }
    public string? Bolge { get; set; }
    public int SiraNo { get; set; }
    public string? Semt { get; set; } // Güzergah adı
    public decimal SeferFiyati { get; set; }
    public string? ServisTipi { get; set; } // S, A, S/A
    public string? Plaka { get; set; }
    public string? SoforAdi { get; set; }
    public string? SoforTelefon { get; set; }
    public string? FirmaAdi { get; set; } // Ait yada kiralayan firma adı

    // Günlük puantaj (1-31)
    public int[] Gunler { get; set; } = new int[31];

    // Hesaplanan toplamlar
    public int SeferGunuToplami { get; set; }
    public decimal Toplam { get; set; } // SeferFiyati * SeferGunuToplami
    public decimal Kdv20 { get; set; } // Toplam * 1.20
    public decimal Kdv10 { get; set; } // Toplam * 1.10
    public decimal Kesinti { get; set; }
    public decimal Odenecek { get; set; } // Kdv10 - Kesinti

    // Eşleştirme
    public int? GuzergahId { get; set; }
    public int? AracId { get; set; }
    public int? SoforId { get; set; }
    public int? KurumCariId { get; set; }
    public string? SahiplikTipi { get; set; } // Öz mal / Kiralama

    public bool GuzergahEslesti { get; set; }
    public bool AracEslesti { get; set; }
    public bool SoforEslesti { get; set; }

    public bool Gecerli => !string.IsNullOrEmpty(Semt) || !string.IsNullOrEmpty(Plaka);
    public string? HataMesaji { get; set; }
}

/// <summary>
/// Excel kolon eşleştirme
/// </summary>
public class HakedisKolonEslestirme
{
    public int KurumKolonu { get; set; } = 1;
    public int GuzergahKolonu { get; set; } = 2;
    public int GelirKolonu { get; set; } = 3;
    public int GiderKolonu { get; set; } = 4;
    public int YonKolonu { get; set; } = 5;
    public int PlakaKolonu { get; set; } = 6;
    public int SoforKolonu { get; set; } = 7;
    public int SoforTelefonKolonu { get; set; } = 8;
    public int FaturaKesiciKolonu { get; set; } = 9;
    public int FaturaKesiciTelefonKolonu { get; set; } = 10;
    public int GunKolonu { get; set; } = 11;

    // Gider detay kolonları
    public int ToplamGiderKolonu { get; set; } = 12;
    public int GiderKdv20Kolonu { get; set; } = 13;
    public int GiderKdv10Kolonu { get; set; } = 14;
    public int GiderKesintiKolonu { get; set; } = 15;
    public int OdenecekKolonu { get; set; } = 16;

    // Gelir detay kolonları
    public int ToplamGelirKolonu { get; set; } = 17;
    public int GelirKdv20Kolonu { get; set; } = 18;
    public int GelirKdv10Kolonu { get; set; } = 19;
    public int GelirKesintiKolonu { get; set; } = 20;
    public int AlinacakKolonu { get; set; } = 21;

    // Fark kolonu (opsiyonel)
    public int FarkKolonu { get; set; } = 22;

    public int BaslangicSatiri { get; set; } = 2;
}

public interface IHakedisService
{
    // Puantaj/Hakedis kayıtları
    Task<List<PuantajKayit>> GetHakedislerAsync(int yil, int ay, int? kurumId = null, int? guzergahId = null);
    Task<PuantajKayit?> GetHakedisByIdAsync(int id);
    Task<PuantajKayit> CreateHakedisAsync(PuantajKayit hakedis);
    Task<PuantajKayit> UpdateHakedisAsync(PuantajKayit hakedis);
    Task DeleteHakedisAsync(int id);
    Task<HakedisIstatistikleri> GetIstatistiklerAsync(int yil, int ay);

    // Toplu işlemler
    Task TopluOnaylaAsync(List<int> hakedisIdleri, string onaylayanKullanici);
    Task TopluFaturaIsaretle(List<int> hakedisIdleri, bool gelir, string faturaNo);
    Task TopluOdemeIsaretle(List<int> hakedisIdleri, bool gelir, decimal tutar);

    // Excel import (hakedis formatı)
    Task<List<HakedisExcelSatiri>> ExcelOnizlemeAsync(Stream excelStream, HakedisKolonEslestirme eslestirme);
    Task<PuantajExcelImport> ExcelImportAsync(
        List<HakedisExcelSatiri> satirlar, 
        int yil, 
        int ay, 
        string dosyaAdi,
        string kullanici,
        bool otomatikOlustur = true);
    Task<List<PuantajExcelImport>> GetImportGecmisiAsync(int? yil = null, int? ay = null);

    // Puantaj şablon Excel import (günlük puantajlı format)
    Task<List<PuantajSablonSatiri>> PuantajSablonOnizlemeAsync(Stream excelStream, int yil, int ay, int baslangicSatiri = 2);
    Task<PuantajExcelImport> PuantajSablonImportAsync(
        List<PuantajSablonSatiri> satirlar,
        int yil, int ay,
        string dosyaAdi, string kullanici,
        int? kurumCariId = null,
        bool otomatikOlustur = true);
    Task<byte[]> PuantajSablonIndirAsync(int yil, int ay);

    // Eşleştirme yardımcıları
    Task<List<Cari>> AraKurumAsync(string arama);
    Task<List<Guzergah>> AraGuzergahAsync(string arama, int? kurumId = null);
    Task<List<Sofor>> AraSoforAsync(string arama);
    Task<List<Arac>> AraAracAsync(string arama);
}

public class HakedisService : IHakedisService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public HakedisService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PuantajKayit>> GetHakedislerAsync(int yil, int ay, int? kurumId = null, int? guzergahId = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.PuantajKayitlar
            .Include(p => p.KurumCari)
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .Include(p => p.OdemeYapilacakCari)
            .Include(p => p.FaturaKesiciCari)
            .Where(p => p.Yil == yil && p.Ay == ay);

        if (kurumId.HasValue)
            query = query.Where(p => p.KurumCariId == kurumId);

        if (guzergahId.HasValue)
            query = query.Where(p => p.GuzergahId == guzergahId);

        return await query.OrderBy(p => p.KurumAdi).ThenBy(p => p.GuzergahAdi).ToListAsync();
    }

    public async Task<PuantajKayit?> GetHakedisByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PuantajKayitlar
            .Include(p => p.KurumCari)
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .Include(p => p.OdemeYapilacakCari)
            .Include(p => p.FaturaKesiciCari)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PuantajKayit> CreateHakedisAsync(PuantajKayit hakedis)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        hakedis.HesaplaGelir();
        hakedis.HesaplaGider();

        // Savunma: caller-provided entity detached navigation taşıyabilir
        hakedis.Guzergah = null;
        hakedis.Arac = null;
        hakedis.Sofor = null;
        hakedis.KurumCari = null;
        hakedis.OdemeYapilacakCari = null;
        hakedis.FaturaKesiciCari = null;
        hakedis.Kurum = null;
        hakedis.IsverenFirma = null;
        hakedis.HesapDonemi = null;

        context.PuantajKayitlar.Add(hakedis);
        await context.SaveChangesAsync();
        return hakedis;
    }

    public async Task<PuantajKayit> UpdateHakedisAsync(PuantajKayit hakedis)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PuantajKayitlar.FindAsync(hakedis.Id);
        if (existing == null) throw new Exception("Hakedis kaydı bulunamadı.");

        // Tüm alanları güncelle
        existing.Yil = hakedis.Yil;
        existing.Ay = hakedis.Ay;
        existing.KurumCariId = hakedis.KurumCariId;
        existing.KurumAdi = hakedis.KurumAdi;
        existing.GuzergahId = hakedis.GuzergahId;
        existing.GuzergahAdi = hakedis.GuzergahAdi;
        existing.Yon = hakedis.Yon;
        existing.AracId = hakedis.AracId;
        existing.Plaka = hakedis.Plaka;
        existing.SoforId = hakedis.SoforId;
        existing.SoforAdi = hakedis.SoforAdi;
        existing.SoforTelefon = hakedis.SoforTelefon;
        existing.SoforOdemeTipi = hakedis.SoforOdemeTipi;
        existing.OdemeYapilacakCariId = hakedis.OdemeYapilacakCariId;
        existing.FaturaKesiciCariId = hakedis.FaturaKesiciCariId;
        existing.FaturaKesiciAdi = hakedis.FaturaKesiciAdi;
        existing.FaturaKesiciTelefon = hakedis.FaturaKesiciTelefon;
        existing.Gun = hakedis.Gun;
        existing.SeferSayisi = hakedis.SeferSayisi;

        // Gelir alanları
        existing.BirimGelir = hakedis.BirimGelir;
        existing.ToplamGelir = hakedis.ToplamGelir;
        existing.GelirKdvOrani = hakedis.GelirKdvOrani;
        existing.GelirKdvOrani20 = hakedis.GelirKdvOrani20;
        existing.GelirKdv20Tutari = hakedis.GelirKdv20Tutari;
        existing.GelirKdvOrani10 = hakedis.GelirKdvOrani10;
        existing.GelirKdv10Tutari = hakedis.GelirKdv10Tutari;
        existing.GelirKesinti = hakedis.GelirKesinti;
        existing.Alinacak = hakedis.Alinacak;

        // Gider alanları
        existing.BirimGider = hakedis.BirimGider;
        existing.GiderKdvOrani20 = hakedis.GiderKdvOrani20;
        existing.GiderKdv20Tutari = hakedis.GiderKdv20Tutari;
        existing.GiderKdvOrani10 = hakedis.GiderKdvOrani10;
        existing.GiderKdv10Tutari = hakedis.GiderKdv10Tutari;
        existing.GiderKesinti = hakedis.GiderKesinti;
        existing.Odenecek = hakedis.Odenecek;

        // Fatura durumları
        existing.GelirFaturaKesildi = hakedis.GelirFaturaKesildi;
        existing.GelirFaturaNo = hakedis.GelirFaturaNo;
        existing.GelirFaturaTarihi = hakedis.GelirFaturaTarihi;
        existing.GiderFaturaAlindi = hakedis.GiderFaturaAlindi;
        existing.GiderFaturaNo = hakedis.GiderFaturaNo;
        existing.GiderFaturaTarihi = hakedis.GiderFaturaTarihi;

        // Ödeme durumları
        existing.GelirOdemeDurumu = hakedis.GelirOdemeDurumu;
        existing.GelirOdemeTarihi = hakedis.GelirOdemeTarihi;
        existing.GelirOdenenTutar = hakedis.GelirOdenenTutar;
        existing.GiderOdemeDurumu = hakedis.GiderOdemeDurumu;
        existing.GiderOdemeTarihi = hakedis.GiderOdemeTarihi;
        existing.GiderOdenenTutar = hakedis.GiderOdenenTutar;

        existing.OnayDurum = hakedis.OnayDurum;
        existing.Notlar = hakedis.Notlar;

        existing.HesaplaGelir();
        existing.HesaplaGider();
        
        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteHakedisAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var hakedis = await context.PuantajKayitlar.FindAsync(id);
        if (hakedis != null)
        {
            hakedis.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<HakedisIstatistikleri> GetIstatistiklerAsync(int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await context.PuantajKayitlar
            .Where(p => p.Yil == yil && p.Ay == ay)
            .ToListAsync();

        return new HakedisIstatistikleri
        {
            ToplamKayit = kayitlar.Count,
            TaslakKayit = kayitlar.Count(k => k.OnayDurum == PuantajOnayDurum.Taslak),
            OnayliKayit = kayitlar.Count(k => k.OnayDurum == PuantajOnayDurum.Onaylandi),
            ToplamGelir = kayitlar.Sum(k => k.GelirToplam),
            ToplamAlinacak = kayitlar.Sum(k => k.Alinacak),
            ToplamGider = kayitlar.Sum(k => k.ToplamGider),
            ToplamOdenecek = kayitlar.Sum(k => k.Odenecek),
            ToplamFark = kayitlar.Sum(k => k.FarkTutari),
            FaturasizGelir = kayitlar.Count(k => !k.GelirFaturaKesildi && k.GelirToplam > 0),
            FaturasizGider = kayitlar.Count(k => !k.GiderFaturaAlindi && k.ToplamGider > 0),
            OdenmemisGelir = kayitlar.Count(k => k.GelirOdemeDurumu != PuantajOdemeDurum.Odendi && k.GelirToplam > 0),
            OdenmemisGider = kayitlar.Count(k => k.GiderOdemeDurumu != PuantajOdemeDurum.Odendi && k.Odenecek > 0)
        };
    }

    public async Task TopluOnaylaAsync(List<int> hakedisIdleri, string onaylayanKullanici)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await context.PuantajKayitlar
            .Where(p => hakedisIdleri.Contains(p.Id))
            .ToListAsync();

        foreach (var kayit in kayitlar)
        {
            kayit.OnayDurum = PuantajOnayDurum.Onaylandi;
            kayit.OnaylayanKullanici = onaylayanKullanici;
            kayit.OnayTarihi = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public async Task TopluFaturaIsaretle(List<int> hakedisIdleri, bool gelir, string faturaNo)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await context.PuantajKayitlar
            .Where(p => hakedisIdleri.Contains(p.Id))
            .ToListAsync();

        foreach (var kayit in kayitlar)
        {
            if (gelir)
            {
                kayit.GelirFaturaKesildi = true;
                kayit.GelirFaturaNo = faturaNo;
                kayit.GelirFaturaTarihi = DateTime.UtcNow;
            }
            else
            {
                kayit.GiderFaturaAlindi = true;
                kayit.GiderFaturaNo = faturaNo;
                kayit.GiderFaturaTarihi = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task TopluOdemeIsaretle(List<int> hakedisIdleri, bool gelir, decimal tutar)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await context.PuantajKayitlar
            .Where(p => hakedisIdleri.Contains(p.Id))
            .ToListAsync();

        foreach (var kayit in kayitlar)
        {
            if (gelir)
            {
                kayit.GelirOdemeDurumu = PuantajOdemeDurum.Odendi;
                kayit.GelirOdemeTarihi = DateTime.UtcNow;
                kayit.GelirOdenenTutar = tutar > 0 ? tutar : kayit.GelirToplam;
            }
            else
            {
                kayit.GiderOdemeDurumu = PuantajOdemeDurum.Odendi;
                kayit.GiderOdemeTarihi = DateTime.UtcNow;
                kayit.GiderOdenenTutar = tutar > 0 ? tutar : kayit.Odenecek;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<HakedisExcelSatiri>> ExcelOnizlemeAsync(Stream excelStream, HakedisKolonEslestirme eslestirme)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var satirlar = new List<HakedisExcelSatiri>();
        
        using var package = new ExcelPackage(excelStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return satirlar;

        var rowCount = worksheet.Dimension?.Rows ?? 0;
        
        // Tüm cari, güzergah, araç ve şoförleri cache'le
        var cariler = await context.Cariler.Where(c => c.Aktif).ToListAsync();
        var guzergahlar = await context.Guzergahlar.Where(g => g.Aktif).ToListAsync();
        var araclar = await context.Araclar.Where(a => a.Aktif).Include(a => a.PlakaGecmisi).ToListAsync();
        var soforler = await context.Soforler.Where(s => s.Aktif).ToListAsync();

        for (int row = eslestirme.BaslangicSatiri; row <= rowCount; row++)
        {
            var satir = new HakedisExcelSatiri
            {
                SatirNo = row,
                KurumAdi = GetCellValue(worksheet, row, eslestirme.KurumKolonu),
                GuzergahAdi = GetCellValue(worksheet, row, eslestirme.GuzergahKolonu),
                Yon = GetCellValue(worksheet, row, eslestirme.YonKolonu),
                Plaka = GetCellValue(worksheet, row, eslestirme.PlakaKolonu),
                SoforAdi = GetCellValue(worksheet, row, eslestirme.SoforKolonu),
                SoforTelefon = GetCellValue(worksheet, row, eslestirme.SoforTelefonKolonu),
                FaturaKesiciAdi = GetCellValue(worksheet, row, eslestirme.FaturaKesiciKolonu),
                FaturaKesiciTelefon = GetCellValue(worksheet, row, eslestirme.FaturaKesiciTelefonKolonu),
                Gun = GetCellDecimal(worksheet, row, eslestirme.GunKolonu),

                // Gelir alanları
                Gelir = GetCellDecimal(worksheet, row, eslestirme.GelirKolonu),
                ToplamGelir = GetCellDecimal(worksheet, row, eslestirme.ToplamGelirKolonu),
                GelirKdv20 = GetCellDecimal(worksheet, row, eslestirme.GelirKdv20Kolonu),
                GelirKdv10 = GetCellDecimal(worksheet, row, eslestirme.GelirKdv10Kolonu),
                GelirKesinti = GetCellDecimal(worksheet, row, eslestirme.GelirKesintiKolonu),
                Alinacak = GetCellDecimal(worksheet, row, eslestirme.AlinacakKolonu),

                // Gider alanları
                Gider = GetCellDecimal(worksheet, row, eslestirme.GiderKolonu),
                ToplamGider = GetCellDecimal(worksheet, row, eslestirme.ToplamGiderKolonu),
                GiderKdv20 = GetCellDecimal(worksheet, row, eslestirme.GiderKdv20Kolonu),
                GiderKdv10 = GetCellDecimal(worksheet, row, eslestirme.GiderKdv10Kolonu),
                GiderKesinti = GetCellDecimal(worksheet, row, eslestirme.GiderKesintiKolonu),
                Odenecek = GetCellDecimal(worksheet, row, eslestirme.OdenecekKolonu)
            };

            // Boş satırları atla
            if (string.IsNullOrWhiteSpace(satir.KurumAdi) && string.IsNullOrWhiteSpace(satir.GuzergahAdi))
                continue;

            // Kurum eşleştirme
            if (!string.IsNullOrEmpty(satir.KurumAdi))
            {
                var kurum = cariler.FirstOrDefault(c => 
                    c.Unvan.Equals(satir.KurumAdi, StringComparison.OrdinalIgnoreCase) ||
                    c.Unvan.Contains(satir.KurumAdi, StringComparison.OrdinalIgnoreCase) ||
                    satir.KurumAdi.Contains(c.Unvan, StringComparison.OrdinalIgnoreCase));
                if (kurum != null)
                {
                    satir.KurumCariId = kurum.Id;
                    satir.KurumEslesti = true;
                }
            }

            // Güzergah eşleştirme
            if (!string.IsNullOrEmpty(satir.GuzergahAdi))
            {
                var guzergah = guzergahlar.FirstOrDefault(g =>
                    g.GuzergahAdi.Equals(satir.GuzergahAdi, StringComparison.OrdinalIgnoreCase) ||
                    g.GuzergahKodu.Equals(satir.GuzergahAdi, StringComparison.OrdinalIgnoreCase) ||
                    g.GuzergahAdi.Contains(satir.GuzergahAdi, StringComparison.OrdinalIgnoreCase));
                if (guzergah != null)
                {
                    satir.GuzergahId = guzergah.Id;
                    satir.GuzergahEslesti = true;
                    // Güzergahtan kurum al
                    if (!satir.KurumEslesti && guzergah.CariId > 0)
                    {
                        satir.KurumCariId = guzergah.CariId;
                        satir.KurumEslesti = true;
                    }
                }
            }

            // Araç eşleştirme (plaka ile)
            if (!string.IsNullOrEmpty(satir.Plaka))
            {
                var plakaNormalize = NormalizePlaka(satir.Plaka);
                var arac = araclar.FirstOrDefault(a =>
                    NormalizePlaka(a.AktifPlaka) == plakaNormalize ||
                    a.PlakaGecmisi.Any(p => NormalizePlaka(p.Plaka) == plakaNormalize && !p.IsDeleted));
                if (arac != null)
                {
                    satir.AracId = arac.Id;
                    satir.AracEslesti = true;
                }
            }

            // Şoför eşleştirme
            if (!string.IsNullOrEmpty(satir.SoforAdi))
            {
                var sofor = soforler.FirstOrDefault(s =>
                    s.TamAd.Equals(satir.SoforAdi, StringComparison.OrdinalIgnoreCase) ||
                    s.TamAd.Contains(satir.SoforAdi, StringComparison.OrdinalIgnoreCase) ||
                    satir.SoforAdi.Contains(s.TamAd, StringComparison.OrdinalIgnoreCase));
                if (sofor != null)
                {
                    satir.SoforId = sofor.Id;
                    satir.SoforEslesti = true;
                }
            }

            // Fatura kesici eşleştirme
            if (!string.IsNullOrEmpty(satir.FaturaKesiciAdi))
            {
                var faturaKesici = cariler.FirstOrDefault(c =>
                    c.Unvan.Equals(satir.FaturaKesiciAdi, StringComparison.OrdinalIgnoreCase) ||
                    c.Unvan.Contains(satir.FaturaKesiciAdi, StringComparison.OrdinalIgnoreCase));
                if (faturaKesici != null)
                {
                    satir.FaturaKesiciCariId = faturaKesici.Id;
                    satir.FaturaKesiciEslesti = true;
                }
            }

            satirlar.Add(satir);
        }

        return satirlar;
    }

    public async Task<PuantajExcelImport> ExcelImportAsync(
        List<HakedisExcelSatiri> satirlar, 
        int yil, 
        int ay, 
        string dosyaAdi,
        string kullanici,
        bool otomatikOlustur = true)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var import = new PuantajExcelImport
        {
            DosyaAdi = dosyaAdi,
            ImportTarihi = DateTime.UtcNow,
            ImportEdenKullanici = kullanici,
            Yil = yil,
            Ay = ay,
            ToplamSatir = satirlar.Count,
            Durum = ImportDurum.Isleniyor
        };
        context.PuantajExcelImportlar.Add(import);
        await context.SaveChangesAsync();

        int basarili = 0, hatali = 0, olusturulanFirma = 0, olusturulanGuzergah = 0, olusturulanSofor = 0;

        foreach (var satir in satirlar)
        {
            try
            {
                // Otomatik oluşturma açıksa eksik kayıtları oluştur
                if (otomatikOlustur)
                {
                    // Kurum oluştur
                    if (!satir.KurumEslesti && !string.IsNullOrEmpty(satir.KurumAdi))
                    {
                        var yeniCari = new Cari
                        {
                            CariKodu = await GenerateCariKodu(context),
                            Unvan = satir.KurumAdi,
                            CariTipi = CariTipi.Musteri,
                            Aktif = true
                        };
                        context.Cariler.Add(yeniCari);
                        await context.SaveChangesAsync();
                        satir.KurumCariId = yeniCari.Id;
                        satir.KurumEslesti = true;
                        olusturulanFirma++;
                    }

                    // Güzergah oluştur
                    if (!satir.GuzergahEslesti && !string.IsNullOrEmpty(satir.GuzergahAdi) && satir.KurumCariId.HasValue)
                    {
                        var yeniGuzergah = new Guzergah
                        {
                            GuzergahKodu = await GenerateGuzergahKodu(context),
                            GuzergahAdi = satir.GuzergahAdi,
                            CariId = satir.KurumCariId.Value,
                            BirimFiyat = satir.Gelir,
                            Aktif = true,
                            SeferTipi = ParseYonToSeferTipi(satir.Yon)
                        };
                        context.Guzergahlar.Add(yeniGuzergah);
                        await context.SaveChangesAsync();
                        satir.GuzergahId = yeniGuzergah.Id;
                        satir.GuzergahEslesti = true;
                        olusturulanGuzergah++;
                    }

                    // Şoför oluştur
                    if (!satir.SoforEslesti && !string.IsNullOrEmpty(satir.SoforAdi))
                    {
                        var adSoyad = ParseAdSoyad(satir.SoforAdi);
                        var yeniSofor = new Sofor
                        {
                            SoforKodu = await GenerateSoforKodu(context),
                            Ad = adSoyad.Item1,
                            Soyad = adSoyad.Item2,
                            Telefon = satir.SoforTelefon,
                            Gorev = PersonelGorev.Sofor,
                            Aktif = true
                        };
                        context.Soforler.Add(yeniSofor);
                        await context.SaveChangesAsync();
                        satir.SoforId = yeniSofor.Id;
                        satir.SoforEslesti = true;
                        olusturulanSofor++;
                    }

                    // Fatura kesici oluştur
                    if (!satir.FaturaKesiciEslesti && !string.IsNullOrEmpty(satir.FaturaKesiciAdi))
                    {
                        var yeniCari = new Cari
                        {
                            CariKodu = await GenerateCariKodu(context),
                            Unvan = satir.FaturaKesiciAdi,
                            CariTipi = CariTipi.Tedarikci,
                            Telefon = satir.FaturaKesiciTelefon,
                            Aktif = true
                        };
                        context.Cariler.Add(yeniCari);
                        await context.SaveChangesAsync();
                        satir.FaturaKesiciCariId = yeniCari.Id;
                        satir.FaturaKesiciEslesti = true;
                        olusturulanFirma++;
                    }
                }

                // Puantaj kaydı oluştur
                var puantaj = new PuantajKayit
                {
                    Yil = yil,
                    Ay = ay,
                    KurumCariId = satir.KurumCariId,
                    KurumAdi = satir.KurumAdi,
                    GuzergahId = satir.GuzergahId,
                    GuzergahAdi = satir.GuzergahAdi,
                    Yon = ParseYonToPuantajYon(satir.Yon),
                    AracId = satir.AracId,
                    Plaka = satir.Plaka,
                    SoforId = satir.SoforId,
                    SoforAdi = satir.SoforAdi,
                    SoforTelefon = satir.SoforTelefon,
                    FaturaKesiciCariId = satir.FaturaKesiciCariId,
                    FaturaKesiciAdi = satir.FaturaKesiciAdi,
                    FaturaKesiciTelefon = satir.FaturaKesiciTelefon,
                    Gun = satir.Gun > 0 ? satir.Gun : 1,

                    // Gelir alanları
                    BirimGelir = satir.Gelir,
                    ToplamGelir = satir.ToplamGelir > 0 ? satir.ToplamGelir : satir.Gelir * (satir.Gun > 0 ? satir.Gun : 1),
                    GelirKdv20Tutari = satir.GelirKdv20,
                    GelirKdv10Tutari = satir.GelirKdv10,
                    GelirKesinti = satir.GelirKesinti,
                    Alinacak = satir.Alinacak,

                    // Gider alanları
                    BirimGider = satir.Gider,
                    ToplamGider = satir.ToplamGider > 0 ? satir.ToplamGider : satir.Gider * (satir.Gun > 0 ? satir.Gun : 1),
                    GiderKdv20Tutari = satir.GiderKdv20,
                    GiderKdv10Tutari = satir.GiderKdv10,
                    GiderKesinti = satir.GiderKesinti,
                    Odenecek = satir.Odenecek,

                    Kaynak = PuantajKaynak.ExcelImport,
                    ExcelImportId = import.Id,
                    ExcelSatirNo = satir.SatirNo,
                    OnayDurum = PuantajOnayDurum.Taslak
                };
                puantaj.HesaplaGelir();
                // Gider hesaplaması Excel'den geldiği için override etme
                if (satir.Odenecek <= 0)
                    puantaj.HesaplaGider();

                context.PuantajKayitlar.Add(puantaj);
                basarili++;
            }
            catch (Exception ex)
            {
                satir.HataMesaji = ex.Message;
                hatali++;
            }
        }

        await context.SaveChangesAsync();

        import.BasariliSatir = basarili;
        import.HataliSatir = hatali;
        import.OtoOlusturulanFirma = olusturulanFirma;
        import.OtoOlusturulanGuzergah = olusturulanGuzergah;
        import.OtoOlusturulanSofor = olusturulanSofor;
        import.Durum = hatali > 0 ? ImportDurum.Hata : ImportDurum.Tamamlandi;
        await context.SaveChangesAsync();

        return import;
    }

    public async Task<List<PuantajExcelImport>> GetImportGecmisiAsync(int? yil = null, int? ay = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.PuantajExcelImportlar.AsQueryable();
        
        if (yil.HasValue)
            query = query.Where(i => i.Yil == yil);
        if (ay.HasValue)
            query = query.Where(i => i.Ay == ay);

        return await query.OrderByDescending(i => i.ImportTarihi).ToListAsync();
    }

    public async Task<List<Cari>> AraKurumAsync(string arama)
    {
        if (string.IsNullOrEmpty(arama)) return new List<Cari>();
        
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Cariler
            .Where(c => c.Aktif && (c.CariTipi == CariTipi.Musteri || c.CariTipi == CariTipi.MusteriTedarikci))
            .Where(c => c.Unvan.ToLower().Contains(arama.ToLower()) || c.CariKodu.ToLower().Contains(arama.ToLower()))
            .Take(20)
            .ToListAsync();
    }

    public async Task<List<Guzergah>> AraGuzergahAsync(string arama, int? kurumId = null)
    {
        if (string.IsNullOrEmpty(arama)) return new List<Guzergah>();
        
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Guzergahlar.Where(g => g.Aktif);
        
        if (kurumId.HasValue)
            query = query.Where(g => g.CariId == kurumId);
        
        return await query
            .Where(g => g.GuzergahAdi.ToLower().Contains(arama.ToLower()) || g.GuzergahKodu.ToLower().Contains(arama.ToLower()))
            .Take(20)
            .ToListAsync();
    }

    public async Task<List<Sofor>> AraSoforAsync(string arama)
    {
        if (string.IsNullOrEmpty(arama)) return new List<Sofor>();
        
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Soforler
            .Where(s => s.Aktif && s.Gorev == PersonelGorev.Sofor)
            .Where(s => (s.Ad + " " + s.Soyad).ToLower().Contains(arama.ToLower()) || s.SoforKodu.ToLower().Contains(arama.ToLower()))
            .Take(20)
            .ToListAsync();
    }

    public async Task<List<Arac>> AraAracAsync(string arama)
    {
        if (string.IsNullOrEmpty(arama)) return new List<Arac>();
        
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .Include(a => a.PlakaGecmisi)
            .Where(a => a.Aktif)
            .Where(a => a.AktifPlaka != null && a.AktifPlaka.ToLower().Contains(arama.ToLower()))
            .Take(20)
            .ToListAsync();
    }

    #region Puantaj Şablon Import

    public async Task<List<PuantajSablonSatiri>> PuantajSablonOnizlemeAsync(Stream excelStream, int yil, int ay, int baslangicSatiri = 2)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var satirlar = new List<PuantajSablonSatiri>();

        using var package = new ExcelPackage(excelStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return satirlar;

        var rowCount = worksheet.Dimension?.Rows ?? 0;
        var gunSayisi = DateTime.DaysInMonth(yil, ay);

        // Cache
        var guzergahlar = await context.Guzergahlar.Where(g => g.Aktif).ToListAsync();
        var araclar = await context.Araclar.Where(a => a.Aktif).Include(a => a.PlakaGecmisi).ToListAsync();
        var soforler = await context.Soforler.Where(s => s.Aktif).ToListAsync();

        // Excel kolon düzeni: Bölge(1) | SıraNo(2) | Semt/Güzergah(3) | Sefer Fiyatı(4) | S/A(5) | Plaka(6) | Şoför(7) | Şoför Tel(8) | Firma Adı(9) | Gün1(10)...Gün31(40) | SeferGünüTop(41) | Toplam(42) | KDV20(43) | KDV10(44) | Kesinti(45) | Ödenecek(46)
        int gunBaslangicKol = 10;
        int seferTopKol = gunBaslangicKol + gunSayisi; // Ay günü kadar kolon sonra
        int toplamKol = seferTopKol + 1;
        int kdv20Kol = toplamKol + 1;
        int kdv10Kol = kdv20Kol + 1;
        int kesintiKol = kdv10Kol + 1;
        int odenecekKol = kesintiKol + 1;

        for (int row = baslangicSatiri; row <= rowCount; row++)
        {
            var semt = GetCellValue(worksheet, row, 3);
            var plaka = GetCellValue(worksheet, row, 6);

            // Boş satır atla
            if (string.IsNullOrWhiteSpace(semt) && string.IsNullOrWhiteSpace(plaka))
                continue;

            var satir = new PuantajSablonSatiri
            {
                SatirNo = row,
                Bolge = GetCellValue(worksheet, row, 1),
                SiraNo = (int)GetCellDecimal(worksheet, row, 2),
                Semt = semt,
                SeferFiyati = GetCellDecimal(worksheet, row, 4),
                ServisTipi = GetCellValue(worksheet, row, 5),
                Plaka = plaka,
                SoforAdi = GetCellValue(worksheet, row, 7),
                SoforTelefon = GetCellValue(worksheet, row, 8),
                FirmaAdi = GetCellValue(worksheet, row, 9)
            };

            // Günlük puantaj oku
            for (int g = 1; g <= gunSayisi; g++)
            {
                var val = GetCellDecimal(worksheet, row, gunBaslangicKol + g - 1);
                satir.Gunler[g - 1] = (int)val;
            }

            // Toplam kolonları oku
            satir.SeferGunuToplami = (int)GetCellDecimal(worksheet, row, seferTopKol);
            satir.Toplam = GetCellDecimal(worksheet, row, toplamKol);
            satir.Kdv20 = GetCellDecimal(worksheet, row, kdv20Kol);
            satir.Kdv10 = GetCellDecimal(worksheet, row, kdv10Kol);
            satir.Kesinti = GetCellDecimal(worksheet, row, kesintiKol);
            satir.Odenecek = GetCellDecimal(worksheet, row, odenecekKol);

            // Sefer günü toplamı Excel'de yoksa hesapla
            if (satir.SeferGunuToplami == 0)
                satir.SeferGunuToplami = satir.Gunler.Take(gunSayisi).Sum();

            // Toplam yoksa hesapla
            if (satir.Toplam == 0 && satir.SeferFiyati > 0)
                satir.Toplam = satir.SeferFiyati * satir.SeferGunuToplami;

            // Güzergah eşleştirme
            if (!string.IsNullOrEmpty(satir.Semt))
            {
                var guzergah = guzergahlar.FirstOrDefault(g =>
                    g.GuzergahAdi.Equals(satir.Semt, StringComparison.OrdinalIgnoreCase) ||
                    g.GuzergahKodu.Equals(satir.Semt, StringComparison.OrdinalIgnoreCase) ||
                    g.GuzergahAdi.Contains(satir.Semt, StringComparison.OrdinalIgnoreCase) ||
                    satir.Semt.Contains(g.GuzergahAdi, StringComparison.OrdinalIgnoreCase));
                if (guzergah != null)
                {
                    satir.GuzergahId = guzergah.Id;
                    satir.GuzergahEslesti = true;
                    if (guzergah.CariId > 0)
                        satir.KurumCariId = guzergah.CariId;
                }
            }

            // Araç eşleştirme (plaka)
            if (!string.IsNullOrEmpty(satir.Plaka))
            {
                var plakaNormalize = NormalizePlaka(satir.Plaka);
                var arac = araclar.FirstOrDefault(a =>
                    NormalizePlaka(a.AktifPlaka) == plakaNormalize ||
                    a.PlakaGecmisi.Any(p => NormalizePlaka(p.Plaka) == plakaNormalize && !p.IsDeleted));
                if (arac != null)
                {
                    satir.AracId = arac.Id;
                    satir.AracEslesti = true;
                    satir.SahiplikTipi = arac.SahiplikTipi switch
                    {
                        AracSahiplikTipi.Ozmal => "Öz Mal",
                        AracSahiplikTipi.Kiralik => "Kiralama",
                        AracSahiplikTipi.Komisyon => "Komisyon",
                        _ => "Diğer"
                    };
                }
            }

            // Şoför eşleştirme
            if (!string.IsNullOrEmpty(satir.SoforAdi))
            {
                var sofor = soforler.FirstOrDefault(s =>
                    s.TamAd.Equals(satir.SoforAdi, StringComparison.OrdinalIgnoreCase) ||
                    s.TamAd.Contains(satir.SoforAdi, StringComparison.OrdinalIgnoreCase) ||
                    satir.SoforAdi.Contains(s.TamAd, StringComparison.OrdinalIgnoreCase));
                if (sofor != null)
                {
                    satir.SoforId = sofor.Id;
                    satir.SoforEslesti = true;
                }
            }

            satirlar.Add(satir);
        }

        return satirlar;
    }

    public async Task<PuantajExcelImport> PuantajSablonImportAsync(
        List<PuantajSablonSatiri> satirlar,
        int yil, int ay,
        string dosyaAdi, string kullanici,
        int? kurumCariId = null,
        bool otomatikOlustur = true)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var import = new PuantajExcelImport
        {
            DosyaAdi = dosyaAdi,
            ImportTarihi = DateTime.UtcNow,
            ImportEdenKullanici = kullanici,
            Yil = yil,
            Ay = ay,
            ToplamSatir = satirlar.Count,
            Durum = ImportDurum.Isleniyor
        };
        context.PuantajExcelImportlar.Add(import);
        await context.SaveChangesAsync();

        int basarili = 0, hatali = 0, olusturulanGuzergah = 0, olusturulanSofor = 0;
        var gunSayisi = DateTime.DaysInMonth(yil, ay);

        foreach (var satir in satirlar)
        {
            try
            {
                if (otomatikOlustur)
                {
                    // Güzergah oluştur
                    if (!satir.GuzergahEslesti && !string.IsNullOrEmpty(satir.Semt))
                    {
                        var yeniGuzergah = new Guzergah
                        {
                            GuzergahKodu = await GenerateGuzergahKodu(context),
                            GuzergahAdi = satir.Semt,
                            CariId = kurumCariId ?? 0,
                            BirimFiyat = satir.SeferFiyati,
                            Aktif = true,
                            SeferTipi = ParseServisTipiToSeferTipi(satir.ServisTipi)
                        };
                        context.Guzergahlar.Add(yeniGuzergah);
                        await context.SaveChangesAsync();
                        satir.GuzergahId = yeniGuzergah.Id;
                        satir.GuzergahEslesti = true;
                        olusturulanGuzergah++;
                    }

                    // Şoför oluştur (SGK'sız)
                    if (!satir.SoforEslesti && !string.IsNullOrEmpty(satir.SoforAdi))
                    {
                        var adSoyad = ParseAdSoyad(satir.SoforAdi);
                        var yeniSofor = new Sofor
                        {
                            SoforKodu = await GenerateSoforKodu(context),
                            Ad = adSoyad.Item1,
                            Soyad = adSoyad.Item2,
                            Telefon = satir.SoforTelefon,
                            Gorev = PersonelGorev.Sofor,
                            SGKBordroDahilMi = false,
                            Aktif = true
                        };
                        context.Soforler.Add(yeniSofor);
                        await context.SaveChangesAsync();
                        satir.SoforId = yeniSofor.Id;
                        satir.SoforEslesti = true;
                        olusturulanSofor++;
                    }
                }

                // PuantajKayit oluştur
                var puantaj = new PuantajKayit
                {
                    Yil = yil,
                    Ay = ay,
                    Bolge = satir.Bolge,
                    SiraNo = satir.SiraNo,
                    KurumCariId = satir.KurumCariId ?? kurumCariId,
                    GuzergahId = satir.GuzergahId,
                    GuzergahAdi = satir.Semt,
                    Yon = ParseServisTipiToPuantajYon(satir.ServisTipi),
                    AracId = satir.AracId,
                    Plaka = satir.Plaka,
                    SoforId = satir.SoforId,
                    SoforAdi = satir.SoforAdi,
                    SoforTelefon = satir.SoforTelefon,
                    AitFirmaAdi = satir.FirmaAdi,
                    SoforOdemeTipi = satir.SahiplikTipi == "Kiralama" ? SoforOdemeTipi.Kiralik : SoforOdemeTipi.Ozmal,

                    // Sefer fiyatı → BirimGider
                    BirimGider = satir.SeferFiyati,
                    Gun = satir.SeferGunuToplami,
                    ToplamGider = satir.Toplam > 0 ? satir.Toplam : satir.SeferFiyati * satir.SeferGunuToplami,
                    GiderKdv20Tutari = satir.Kdv20,
                    GiderKdv10Tutari = satir.Kdv10,
                    GiderKesinti = satir.Kesinti,
                    Odenecek = satir.Odenecek,

                    Kaynak = PuantajKaynak.ExcelImport,
                    ExcelImportId = import.Id,
                    ExcelSatirNo = satir.SatirNo,
                    OnayDurum = PuantajOnayDurum.Taslak
                };

                // Günlük puantaj değerlerini ata
                for (int g = 1; g <= gunSayisi; g++)
                    puantaj.SetGunDeger(g, satir.Gunler[g - 1]);

                // Ödenecek yoksa hesapla
                if (puantaj.Odenecek == 0 && puantaj.ToplamGider > 0)
                    puantaj.HesaplaPuantajToplam();

                context.PuantajKayitlar.Add(puantaj);
                basarili++;
            }
            catch (Exception ex)
            {
                satir.HataMesaji = ex.Message;
                hatali++;
            }
        }

        await context.SaveChangesAsync();

        import.BasariliSatir = basarili;
        import.HataliSatir = hatali;
        import.OtoOlusturulanGuzergah = olusturulanGuzergah;
        import.OtoOlusturulanSofor = olusturulanSofor;
        import.Durum = hatali > 0 ? ImportDurum.Hata : ImportDurum.Tamamlandi;
        await context.SaveChangesAsync();

        return import;
    }

    public async Task<byte[]> PuantajSablonIndirAsync(int yil, int ay)
    {
        var gunSayisi = DateTime.DaysInMonth(yil, ay);

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add($"Puantaj {yil}-{ay:D2}");

        // Başlıklar
        var headers = new List<string>
        {
            "Bölge", "Sıra No", "Semt (Güzergah)", "Sefer Fiyatı",
            "S/A", "Plaka", "Şoför", "Şoför Tel", "Firma Adı"
        };

        // Gün başlıkları
        for (int g = 1; g <= gunSayisi; g++)
        {
            var tarih = new DateTime(yil, ay, g);
            headers.Add($"{g}\n{tarih:ddd}");
        }

        headers.AddRange(["Sefer Günü Top.", "Toplam", "KDV %20", "KDV %10", "Kesinti", "Ödenecek"]);

        // Başlıkları yaz
        for (int i = 0; i < headers.Count; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
            ws.Cells[1, i + 1].Style.Font.Bold = true;
            ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
            ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            ws.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            ws.Cells[1, i + 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ws.Cells[1, i + 1].Style.WrapText = true;
        }

        // Hafta sonu günlerini kırmızı yap
        for (int g = 1; g <= gunSayisi; g++)
        {
            var tarih = new DateTime(yil, ay, g);
            if (tarih.DayOfWeek == DayOfWeek.Saturday || tarih.DayOfWeek == DayOfWeek.Sunday)
            {
                ws.Cells[1, 9 + g].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(192, 0, 0));
            }
        }

        // Kolon genişlikleri
        ws.Column(1).Width = 12; // Bölge
        ws.Column(2).Width = 8;  // SıraNo
        ws.Column(3).Width = 25; // Semt
        ws.Column(4).Width = 12; // Sefer Fiyatı
        ws.Column(5).Width = 6;  // S/A
        ws.Column(6).Width = 14; // Plaka
        ws.Column(7).Width = 20; // Şoför
        ws.Column(8).Width = 14; // Şoför Tel
        ws.Column(9).Width = 20; // Firma Adı
        for (int g = 1; g <= gunSayisi; g++)
            ws.Column(9 + g).Width = 5; // Gün kolonları
        int sonKolBaslangic = 10 + gunSayisi;
        ws.Column(sonKolBaslangic).Width = 10;     // Sefer Günü Top
        ws.Column(sonKolBaslangic + 1).Width = 14; // Toplam
        ws.Column(sonKolBaslangic + 2).Width = 14; // KDV20
        ws.Column(sonKolBaslangic + 3).Width = 14; // KDV10
        ws.Column(sonKolBaslangic + 4).Width = 14; // Kesinti
        ws.Column(sonKolBaslangic + 5).Width = 14; // Ödenecek

        // Satır yüksekliği
        ws.Row(1).Height = 35;

        // Mevcut güzergahları örnek veri olarak ekle (opsiyonel)
        using var context = await _contextFactory.CreateDbContextAsync();
        var mevcutGuzergahlar = await context.Guzergahlar
            .Where(g => g.Aktif)
            .Include(g => g.VarsayilanSofor)
            .Include(g => g.VarsayilanArac).ThenInclude(a => a!.PlakaGecmisi)
            .OrderBy(g => g.GuzergahAdi)
            .Take(50)
            .ToListAsync();

        int satir = 2;
        foreach (var guz in mevcutGuzergahlar)
        {
            ws.Cells[satir, 2].Value = satir - 1; // SıraNo
            ws.Cells[satir, 3].Value = guz.GuzergahAdi;
            ws.Cells[satir, 4].Value = (double)guz.BirimFiyat;
            ws.Cells[satir, 5].Value = guz.SeferTipi switch
            {
                SeferTipi.Sabah => "S",
                SeferTipi.Aksam => "A",
                SeferTipi.SabahAksam => "S/A",
                SeferTipi.Mesai => "M",
                _ => "S/A"
            };
            if (guz.VarsayilanArac != null)
            {
                ws.Cells[satir, 6].Value = guz.VarsayilanArac.AktifPlaka;
            }
            if (guz.VarsayilanSofor != null)
            {
                ws.Cells[satir, 7].Value = guz.VarsayilanSofor.TamAd;
                ws.Cells[satir, 8].Value = guz.VarsayilanSofor.Telefon;
            }

            // Formüller: Sefer Günü Toplamı, Toplam, KDV20, KDV10, Kesinti, Ödenecek
            var gunRange = $"{ws.Cells[satir, 10].Address}:{ws.Cells[satir, 9 + gunSayisi].Address}";
            ws.Cells[satir, sonKolBaslangic].Formula = $"SUM({gunRange})";
            ws.Cells[satir, sonKolBaslangic + 1].Formula = $"{ws.Cells[satir, 4].Address}*{ws.Cells[satir, sonKolBaslangic].Address}";
            ws.Cells[satir, sonKolBaslangic + 2].Formula = $"{ws.Cells[satir, sonKolBaslangic + 1].Address}*1.2";
            ws.Cells[satir, sonKolBaslangic + 3].Formula = $"{ws.Cells[satir, sonKolBaslangic + 1].Address}*1.1";
            // Kesinti boş
            ws.Cells[satir, sonKolBaslangic + 5].Formula = $"{ws.Cells[satir, sonKolBaslangic + 3].Address}-{ws.Cells[satir, sonKolBaslangic + 4].Address}";

            satir++;
        }

        // Para formatı
        var paraFormat = "#,##0.00";
        for (int r = 2; r < satir; r++)
        {
            ws.Cells[r, 4].Style.Numberformat.Format = paraFormat;
            ws.Cells[r, sonKolBaslangic + 1].Style.Numberformat.Format = paraFormat;
            ws.Cells[r, sonKolBaslangic + 2].Style.Numberformat.Format = paraFormat;
            ws.Cells[r, sonKolBaslangic + 3].Style.Numberformat.Format = paraFormat;
            ws.Cells[r, sonKolBaslangic + 4].Style.Numberformat.Format = paraFormat;
            ws.Cells[r, sonKolBaslangic + 5].Style.Numberformat.Format = paraFormat;
        }

        // Sayfa koruma (başlıklar)
        ws.Cells[1, 1, 1, headers.Count].Style.Locked = true;

        return package.GetAsByteArray();
    }

    #endregion

    #region Helper Methods

    private string? GetCellValue(ExcelWorksheet ws, int row, int col)
    {
        if (col <= 0) return null;
        var cell = ws.Cells[row, col];
        return cell?.Value?.ToString()?.Trim();
    }

    private decimal GetCellDecimal(ExcelWorksheet ws, int row, int col)
    {
        if (col <= 0) return 0;
        var cell = ws.Cells[row, col];
        if (cell?.Value == null) return 0;
        if (decimal.TryParse(cell.Value.ToString(), out var result))
            return result;
        return 0;
    }

    private string NormalizePlaka(string? plaka)
    {
        if (string.IsNullOrEmpty(plaka)) return "";
        return plaka.Replace(" ", "").Replace("-", "").ToUpperInvariant();
    }

    private PuantajYon ParseYonToPuantajYon(string? yon)
    {
        if (string.IsNullOrEmpty(yon)) return PuantajYon.SabahAksam;
        
        var lower = yon.ToLowerInvariant();
        if (lower.Contains("sabah") && lower.Contains("akşam")) return PuantajYon.SabahAksam;
        if (lower.Contains("sabah") && lower.Contains("aksam")) return PuantajYon.SabahAksam;
        if (lower.Contains("sabah")) return PuantajYon.Sabah;
        if (lower.Contains("akşam") || lower.Contains("aksam")) return PuantajYon.Aksam;
        return PuantajYon.Diger;
    }

    private SeferTipi ParseYonToSeferTipi(string? yon)
    {
        if (string.IsNullOrEmpty(yon)) return SeferTipi.SabahAksam;

        var lower = yon.ToLowerInvariant();
        if (lower.Contains("sabah") && lower.Contains("akşam")) return SeferTipi.SabahAksam;
        if (lower.Contains("sabah") && lower.Contains("aksam")) return SeferTipi.SabahAksam;
        if (lower.Contains("sabah")) return SeferTipi.Sabah;
        if (lower.Contains("akşam") || lower.Contains("aksam")) return SeferTipi.Aksam;
        return SeferTipi.Saatlik;
    }

    private PuantajYon ParseServisTipiToPuantajYon(string? tip)
    {
        if (string.IsNullOrEmpty(tip)) return PuantajYon.SabahAksam;
        var t = tip.Trim().ToUpperInvariant();
        if (t == "S/A" || t == "SA" || t == "S-A") return PuantajYon.SabahAksam;
        if (t == "S") return PuantajYon.Sabah;
        if (t == "A") return PuantajYon.Aksam;
        return ParseYonToPuantajYon(tip);
    }

    private SeferTipi ParseServisTipiToSeferTipi(string? tip)
    {
        if (string.IsNullOrEmpty(tip)) return SeferTipi.SabahAksam;
        var t = tip.Trim().ToUpperInvariant();
        if (t == "S/A" || t == "SA" || t == "S-A") return SeferTipi.SabahAksam;
        if (t == "S") return SeferTipi.Sabah;
        if (t == "A") return SeferTipi.Aksam;
        return ParseYonToSeferTipi(tip);
    }

    private (string, string) ParseAdSoyad(string tamAd)
    {
        var parcalar = tamAd.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parcalar.Length == 0) return ("", "");
        if (parcalar.Length == 1) return (parcalar[0], "");
        return (string.Join(" ", parcalar.Take(parcalar.Length - 1)), parcalar.Last());
    }

    private async Task<string> GenerateCariKodu(ApplicationDbContext context)
    {
        var sonKayit = await context.Cariler
            .IgnoreQueryFilters()
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();
        var sonId = sonKayit?.Id ?? 0;
        return $"C{(sonId + 1):D5}";
    }

    private async Task<string> GenerateGuzergahKodu(ApplicationDbContext context)
    {
        var sonKayit = await context.Guzergahlar
            .IgnoreQueryFilters()
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync();
        var sonId = sonKayit?.Id ?? 0;
        return $"G{(sonId + 1):D5}";
    }

    private async Task<string> GenerateSoforKodu(ApplicationDbContext context)
    {
        var sonKayit = await context.Soforler
            .IgnoreQueryFilters()
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();
        var sonId = sonKayit?.Id ?? 0;
        return $"P{(sonId + 1):D5}";
    }

    #endregion
}



