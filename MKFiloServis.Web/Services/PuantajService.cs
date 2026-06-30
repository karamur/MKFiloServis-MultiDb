using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class PuantajService : IPuantajService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PuantajService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PersonelPuantaj>> GetAylikPuantajAsync(int firmaId, int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelPuantajlar
            .Include(p => p.Personel)
            .Where(p => p.FirmaId == firmaId && p.Yil == yil && p.Ay == ay && !p.IsDeleted)
            .OrderBy(p => p.Personel!.Ad).ThenBy(p => p.Personel!.Soyad)
            .ToListAsync();
    }

    public async Task<PersonelPuantaj?> GetPuantajByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelPuantajlar
            .Include(p => p.Personel)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<PersonelPuantaj?> GetPersonelAylikPuantajAsync(int personelId, int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelPuantajlar
            .Include(p => p.Personel)
            .FirstOrDefaultAsync(p => p.PersonelId == personelId && p.Yil == yil && p.Ay == ay && !p.IsDeleted);
    }

    public async Task<PersonelPuantaj> CreateOrUpdatePuantajAsync(PersonelPuantaj puantaj)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var mevcut = await context.PersonelPuantajlar
            .FirstOrDefaultAsync(p => p.PersonelId == puantaj.PersonelId && 
                                     p.Yil == puantaj.Yil && 
                                     p.Ay == puantaj.Ay);

        if (mevcut != null)
        {
            if (mevcut.OnayDurumu == PersonelPuantajOnayDurumu.Onaylandi)
                throw new InvalidOperationException("Onaylanmış puantaj kaydı güncellenemez.");

            mevcut.CalisilanGun = puantaj.CalisilanGun;
            mevcut.FazlaMesaiSaat = puantaj.FazlaMesaiSaat;
            mevcut.IzinGunu = puantaj.IzinGunu;
            mevcut.MazeretGunu = puantaj.MazeretGunu;
            mevcut.BrutMaas = puantaj.BrutMaas;
            mevcut.YemekUcreti = puantaj.YemekUcreti;
            mevcut.YolUcreti = puantaj.YolUcreti;
            mevcut.Prim = puantaj.Prim;
            mevcut.DigerOdeme = puantaj.DigerOdeme;
            mevcut.SgkKesinti = puantaj.SgkKesinti;
            mevcut.GelirVergisi = puantaj.GelirVergisi;
            mevcut.DamgaVergisi = puantaj.DamgaVergisi;
            mevcut.DigerKesinti = puantaj.DigerKesinti;
            mevcut.NetOdeme = puantaj.NetOdeme;
            mevcut.BankaHesapNo = puantaj.BankaHesapNo;
            mevcut.OnayDurumu = puantaj.OnayDurumu;
            mevcut.OnaylayanKullanici = puantaj.OnaylayanKullanici;
            mevcut.OnayTarihi = puantaj.OnayTarihi;
            mevcut.OnayNotu = puantaj.OnayNotu;
            mevcut.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return mevcut;
        }
        else
        {
            puantaj.CreatedAt = DateTime.UtcNow;
            context.PersonelPuantajlar.Add(puantaj);
            await context.SaveChangesAsync();
            return puantaj;
        }
    }

    public async Task<PersonelPuantaj> OnayaGonderAsync(int id, string? not = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var puantaj = await context.PersonelPuantajlar.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (puantaj == null)
            throw new InvalidOperationException("Puantaj bulunamadı.");

        if (puantaj.OnayDurumu == PersonelPuantajOnayDurumu.Onaylandi)
            throw new InvalidOperationException("Onaylanmış puantaj tekrar onaya gönderilemez.");

        puantaj.OnayDurumu = PersonelPuantajOnayDurumu.OnayBekliyor;
        puantaj.OnaylayanKullanici = null;
        puantaj.OnayTarihi = null;
        puantaj.OnayNotu = not;
        puantaj.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return puantaj;
    }

    public async Task<PersonelPuantaj> OnaylaAsync(int id, string onaylayanKullanici, string? not = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var puantaj = await context.PersonelPuantajlar.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (puantaj == null)
            throw new InvalidOperationException("Puantaj bulunamadı.");

        puantaj.OnayDurumu = PersonelPuantajOnayDurumu.Onaylandi;
        puantaj.OnaylayanKullanici = onaylayanKullanici;
        puantaj.OnayTarihi = DateTime.UtcNow;
        puantaj.OnayNotu = not;
        puantaj.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return puantaj;
    }

    public async Task<PersonelPuantaj> ReddetAsync(int id, string onaylayanKullanici, string? not = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var puantaj = await context.PersonelPuantajlar.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (puantaj == null)
            throw new InvalidOperationException("Puantaj bulunamadı.");

        puantaj.OnayDurumu = PersonelPuantajOnayDurumu.Reddedildi;
        puantaj.OnaylayanKullanici = onaylayanKullanici;
        puantaj.OnayTarihi = DateTime.UtcNow;
        puantaj.OnayNotu = not;
        puantaj.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return puantaj;
    }

    public async Task<PersonelPuantaj> OnayGeriAlAsync(int id, string? not = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var puantaj = await context.PersonelPuantajlar.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (puantaj == null)
            throw new InvalidOperationException("Puantaj bulunamadı.");

        puantaj.OnayDurumu = PersonelPuantajOnayDurumu.Taslak;
        puantaj.OnaylayanKullanici = null;
        puantaj.OnayTarihi = null;
        puantaj.OnayNotu = not;
        puantaj.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return puantaj;
    }

    public async Task DeletePuantajAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var puantaj = await context.PersonelPuantajlar
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (puantaj == null)
            return;

        if (puantaj.OnayDurumu == PersonelPuantajOnayDurumu.Onaylandi)
            throw new InvalidOperationException("Onaylanan puantaj silinemez. Önce onayı geri alın.");

        var gunlukler = await context.GunlukPuantajlar
            .Where(g => g.PersonelPuantajId == id)
            .ToListAsync();

        context.GunlukPuantajlar.RemoveRange(gunlukler);
        context.PersonelPuantajlar.Remove(puantaj);
        await context.SaveChangesAsync();
    }

    public async Task<List<GunlukPuantaj>> GetGunlukPuantajlarAsync(int puantajId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GunlukPuantajlar
            .Include(g => g.ServisCalisma)
            .Where(g => g.PersonelPuantajId == puantajId && !g.IsDeleted)
            .OrderBy(g => g.Tarih)
            .ToListAsync();
    }

    public async Task<GunlukPuantaj> SaveGunlukPuantajAsync(GunlukPuantaj gunluk)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var puantaj = await context.PersonelPuantajlar.FirstOrDefaultAsync(p => p.Id == gunluk.PersonelPuantajId && !p.IsDeleted);
        if (puantaj?.OnayDurumu == PersonelPuantajOnayDurumu.Onaylandi)
            throw new InvalidOperationException("Onaylanmış puantajın günlük kayıtları değiştirilemez.");

        if (gunluk.Id > 0)
        {
            context.GunlukPuantajlar.Update(gunluk);
        }
        else
        {
            gunluk.CreatedAt = DateTime.UtcNow;
            context.GunlukPuantajlar.Add(gunluk);
        }

        await context.SaveChangesAsync();
        return gunluk;
    }

    public async Task OtomatikGunlukPuantajOlusturAsync(int puantajId, int yil, int ay, bool cumartesiCalisir = true, bool pazarCalisir = false, List<DateTime>? resmiTatiller = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var puantaj = await context.PersonelPuantajlar.FindAsync(puantajId);
        if (puantaj == null) return;

        var gunSayisi = DateTime.DaysInMonth(yil, ay);

        for (int gun = 1; gun <= gunSayisi; gun++)
        {
            var tarih = new DateTime(yil, ay, gun);

            var mevcut = await context.GunlukPuantajlar
                .AnyAsync(g => g.PersonelPuantajId == puantajId && g.Gun == gun);

            if (!mevcut)
            {
                var isTatil = false;

                if (tarih.DayOfWeek == DayOfWeek.Sunday && !pazarCalisir)
                    isTatil = true;
                else if (tarih.DayOfWeek == DayOfWeek.Saturday && !cumartesiCalisir)
                    isTatil = true;
                else if (resmiTatiller != null && resmiTatiller.Any(t => t.Date == tarih.Date))
                    isTatil = true;

                var gunluk = new GunlukPuantaj
                {
                    PersonelPuantajId = puantajId,
                    Gun = gun,
                    Tarih = tarih,
                    Durum = isTatil ? 0 : 1,
                    Calisti = !isTatil,
                    FazlaMesaiSaat = 0,
                    CreatedAt = DateTime.UtcNow
                };
                context.GunlukPuantajlar.Add(gunluk);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<PersonelPuantaj> HesaplaAsync(int puantajId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var puantaj = await context.PersonelPuantajlar.FindAsync(puantajId);
        if (puantaj == null) throw new Exception("Puantaj bulunamadı");

        // Toplam brüt gelir
        var toplamBrut = puantaj.BrutMaas + puantaj.YemekUcreti + puantaj.YolUcreti + 
                        puantaj.Prim + puantaj.DigerOdeme;

        // SGK kesintisi (%14)
        puantaj.SgkKesinti = toplamBrut * 0.14m;

        // Gelir vergisi (basit hesaplama - gerçekte dilimli)
        var gelirVergisiMatrahi = toplamBrut - puantaj.SgkKesinti;
        puantaj.GelirVergisi = gelirVergisiMatrahi * 0.15m;

        // Damga vergisi (%0.759)
        puantaj.DamgaVergisi = toplamBrut * 0.00759m;

        // Net ödeme
        puantaj.NetOdeme = toplamBrut - puantaj.SgkKesinti - puantaj.GelirVergisi - 
                          puantaj.DamgaVergisi - puantaj.DigerKesinti;

        puantaj.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return puantaj;
    }

    public async Task<decimal> ToplamBrutMaasHesaplaAsync(int firmaId, int yil, int ay)
    {
        var puantajlar = await GetAylikPuantajAsync(firmaId, yil, ay);
        return puantajlar.Sum(p => p.BrutMaas + p.YemekUcreti + p.YolUcreti + p.Prim + p.DigerOdeme);
    }

    public async Task<decimal> ToplamNetOdemeHesaplaAsync(int firmaId, int yil, int ay)
    {
        var puantajlar = await GetAylikPuantajAsync(firmaId, yil, ay);
        return puantajlar.Sum(p => p.NetOdeme);
    }

    public async Task<byte[]> ExportPuantajListesiAsync(int firmaId, int yil, int ay)
    {
        var puantajlar = await GetAylikPuantajAsync(firmaId, yil, ay);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add($"{ay:00}-{yil} Puantaj");

        // Başlık
        worksheet.Cells["A1"].Value = "PUANTAJ LİSTESİ";
        worksheet.Cells["A1:P1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        worksheet.Cells["A2"].Value = $"{GetAyAdi(ay)} {yil}";
        worksheet.Cells["A2:P2"].Merge = true;
        worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Kolon başlıkları
        int row = 4;
        worksheet.Cells[row, 1].Value = "Sıra";
        worksheet.Cells[row, 2].Value = "Ad Soyad";
        worksheet.Cells[row, 3].Value = "Çalışılan Gün";
        worksheet.Cells[row, 4].Value = "Fazla Mesai";
        worksheet.Cells[row, 5].Value = "İzin";
        worksheet.Cells[row, 6].Value = "Brüt Maaş";
        worksheet.Cells[row, 7].Value = "Yemek";
        worksheet.Cells[row, 8].Value = "Yol";
        worksheet.Cells[row, 9].Value = "Prim";
        worksheet.Cells[row, 10].Value = "Diğer";
        worksheet.Cells[row, 11].Value = "Toplam Brüt";
        worksheet.Cells[row, 12].Value = "SGK Kesinti";
        worksheet.Cells[row, 13].Value = "Gelir Vergisi";
        worksheet.Cells[row, 14].Value = "Damga Vergisi";
        worksheet.Cells[row, 15].Value = "Diğer Kesinti";
        worksheet.Cells[row, 16].Value = "Net Ödeme";

        worksheet.Cells[row, 1, row, 16].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, 16].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, 16].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

        // Veriler
        int sira = 1;
        row = 5;
        foreach (var p in puantajlar)
        {
            var toplamBrut = p.BrutMaas + p.YemekUcreti + p.YolUcreti + p.Prim + p.DigerOdeme;

            worksheet.Cells[row, 1].Value = sira++;
            worksheet.Cells[row, 2].Value = $"{p.Personel?.Ad} {p.Personel?.Soyad}";
            worksheet.Cells[row, 3].Value = p.CalisilanGun;
            worksheet.Cells[row, 4].Value = p.FazlaMesaiSaat;
            worksheet.Cells[row, 5].Value = p.IzinGunu;
            worksheet.Cells[row, 6].Value = p.BrutMaas;
            worksheet.Cells[row, 7].Value = p.YemekUcreti;
            worksheet.Cells[row, 8].Value = p.YolUcreti;
            worksheet.Cells[row, 9].Value = p.Prim;
            worksheet.Cells[row, 10].Value = p.DigerOdeme;
            worksheet.Cells[row, 11].Value = toplamBrut;
            worksheet.Cells[row, 12].Value = p.SgkKesinti;
            worksheet.Cells[row, 13].Value = p.GelirVergisi;
            worksheet.Cells[row, 14].Value = p.DamgaVergisi;
            worksheet.Cells[row, 15].Value = p.DigerKesinti;
            worksheet.Cells[row, 16].Value = p.NetOdeme;

            // Para birimi formatı
            for (int col = 6; col <= 16; col++)
            {
                worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
            }

            row++;
        }

        // Toplam satırı
        worksheet.Cells[row, 1].Value = "TOPLAM";
        worksheet.Cells[row, 1, row, 5].Merge = true;
        worksheet.Cells[row, 6].Formula = $"SUM(F5:F{row-1})";
        worksheet.Cells[row, 7].Formula = $"SUM(G5:G{row-1})";
        worksheet.Cells[row, 8].Formula = $"SUM(H5:H{row-1})";
        worksheet.Cells[row, 9].Formula = $"SUM(I5:I{row-1})";
        worksheet.Cells[row, 10].Formula = $"SUM(J5:J{row-1})";
        worksheet.Cells[row, 11].Formula = $"SUM(K5:K{row-1})";
        worksheet.Cells[row, 12].Formula = $"SUM(L5:L{row-1})";
        worksheet.Cells[row, 13].Formula = $"SUM(M5:M{row-1})";
        worksheet.Cells[row, 14].Formula = $"SUM(N5:N{row-1})";
        worksheet.Cells[row, 15].Formula = $"SUM(O5:O{row-1})";
        worksheet.Cells[row, 16].Formula = $"SUM(P5:P{row-1})";

        worksheet.Cells[row, 1, row, 16].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, 16].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, 16].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

        for (int col = 6; col <= 16; col++)
        {
            worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
        }

        // Otomatik genişlik
        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportVakifbankOdemeListesiAsync(int firmaId, int yil, int ay)
    {
        var puantajlar = await GetAylikPuantajAsync(firmaId, yil, ay);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Vakıfbank Ödeme");

        // Başlık
        worksheet.Cells["A1"].Value = "VakıfBank Toplu Ödeme Listesi";
        worksheet.Cells["A1:E1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;

        // Kolon başlıkları (VakıfBank formatı)
        worksheet.Cells["A3"].Value = "Sıra";
        worksheet.Cells["B3"].Value = "Alıcı Adı Soyadı";
        worksheet.Cells["C3"].Value = "IBAN";
        worksheet.Cells["D3"].Value = "Tutar";
        worksheet.Cells["E3"].Value = "Açıklama";

        worksheet.Cells["A3:E3"].Style.Font.Bold = true;
        worksheet.Cells["A3:E3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A3:E3"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        // Veriler
        int sira = 1;
        int row = 4;
        foreach (var p in puantajlar.Where(x => !string.IsNullOrEmpty(x.BankaHesapNo)))
        {
            worksheet.Cells[row, 1].Value = sira++;
            worksheet.Cells[row, 2].Value = $"{p.Personel?.Ad} {p.Personel?.Soyad}";
            worksheet.Cells[row, 3].Value = p.BankaHesapNo;
            worksheet.Cells[row, 4].Value = p.NetOdeme;
            worksheet.Cells[row, 5].Value = $"{GetAyAdi(ay)} {yil} Maaş Ödemesi";

            worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
            row++;
        }

        // Toplam
        worksheet.Cells[row, 1].Value = "TOPLAM";
        worksheet.Cells[row, 1, row, 3].Merge = true;
        worksheet.Cells[row, 4].Formula = $"SUM(D4:D{row-1})";
        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
        worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<int> GetToplamPersonelSayisiAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Soforler.CountAsync(s => s.Aktif && !s.IsDeleted);
    }

    public async Task<Dictionary<int, decimal>> GetAylikMaasGrafigiAsync(int firmaId, int yil)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var sonuc = new Dictionary<int, decimal>();
        for (int ay = 1; ay <= 12; ay++)
        {
            var toplam = await context.PersonelPuantajlar
                .Where(p => p.FirmaId == firmaId && p.Yil == yil && p.Ay == ay && !p.IsDeleted)
                .SumAsync(p => p.NetOdeme);
            sonuc[ay] = toplam;
        }

        return sonuc;
    }

    private string GetAyAdi(int ay) => ay switch
    {
        1 => "Ocak", 2 => "Şubat", 3 => "Mart", 4 => "Nisan",
        5 => "Mayıs", 6 => "Haziran", 7 => "Temmuz", 8 => "Ağustos",
        9 => "Eylül", 10 => "Ekim", 11 => "Kasım", 12 => "Aralık",
        _ => ""
    };
}



