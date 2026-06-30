using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Net.Mail;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Fatura şablon yönetim servisi - Özelleştirilebilir fatura PDF oluşturma
/// </summary>
public class FaturaSablonService : IFaturaSablonService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IFirmaService _firmaService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FaturaSablonService> _logger;

    public FaturaSablonService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IFirmaService firmaService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<FaturaSablonService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _firmaService = firmaService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;

        // QuestPDF Community License
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #region Şablon CRUD İşlemleri

    public async Task<List<FaturaSablon>> TumSablonlariGetirAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var aktifFirma = _firmaService.GetAktifFirma();

        return await context.FaturaSablonlari
            .Include(s => s.Firma)
            .Where(s => aktifFirma.TumFirmalar || s.FirmaId == aktifFirma.FirmaId)
            .OrderByDescending(s => s.Varsayilan)
            .ThenBy(s => s.SablonAdi)
            .ToListAsync();
    }

    public async Task<FaturaSablon?> SablonGetirAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.FaturaSablonlari
            .Include(s => s.Firma)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<FaturaSablon?> VarsayilanSablonGetirAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var aktifFirma = _firmaService.GetAktifFirma();

        // Önce firmaya özel varsayılan şablon ara
        var sablon = await context.FaturaSablonlari
            .Include(s => s.Firma)
            .Where(s => s.FirmaId == aktifFirma.FirmaId && s.Varsayilan && s.Aktif)
            .FirstOrDefaultAsync();

        // Bulunamazsa firmanın herhangi aktif şablonu
        if (sablon == null)
        {
            sablon = await context.FaturaSablonlari
                .Include(s => s.Firma)
                .Where(s => s.FirmaId == aktifFirma.FirmaId && s.Aktif)
                .FirstOrDefaultAsync();
        }

        return sablon;
    }

    public async Task<FaturaSablon> SablonEkleAsync(FaturaSablon sablon)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var aktifFirma = _firmaService.GetAktifFirma();

        sablon.FirmaId = aktifFirma.FirmaId;
        sablon.CreatedAt = DateTime.Now;

        // İlk şablon ise varsayılan yap
        var mevcutSayisi = await context.FaturaSablonlari
            .CountAsync(s => s.FirmaId == aktifFirma.FirmaId);
        if (mevcutSayisi == 0)
            sablon.Varsayilan = true;

        // Varsayılan yapılıyorsa diğerlerini kaldır
        if (sablon.Varsayilan)
        {
            await context.FaturaSablonlari
                .Where(s => s.FirmaId == aktifFirma.FirmaId && s.Varsayilan)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Varsayilan, false));
        }

        context.FaturaSablonlari.Add(sablon);
        await context.SaveChangesAsync();

        _logger.LogInformation("Fatura şablonu eklendi: {SablonAdi}, Firma: {FirmaId}", sablon.SablonAdi, sablon.FirmaId);
        return sablon;
    }

    public async Task<bool> SablonGuncelleAsync(FaturaSablon sablon)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var mevcut = await context.FaturaSablonlari.FindAsync(sablon.Id);
        if (mevcut == null) return false;

        // Varsayılan değişiyorsa diğerlerini kaldır
        if (sablon.Varsayilan && !mevcut.Varsayilan)
        {
            await context.FaturaSablonlari
                .Where(s => s.FirmaId == mevcut.FirmaId && s.Varsayilan && s.Id != sablon.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Varsayilan, false));
        }

        // Alanları güncelle
        mevcut.SablonAdi = sablon.SablonAdi;
        mevcut.Varsayilan = sablon.Varsayilan;
        mevcut.Aktif = sablon.Aktif;

        // Sayfa Ayarları
        mevcut.SayfaBoyutu = sablon.SayfaBoyutu;
        mevcut.SayfaYonelimi = sablon.SayfaYonelimi;
        mevcut.SayfaKenarBoslukSol = sablon.SayfaKenarBoslukSol;
        mevcut.SayfaKenarBoslukSag = sablon.SayfaKenarBoslukSag;
        mevcut.SayfaKenarBoslukUst = sablon.SayfaKenarBoslukUst;
        mevcut.SayfaKenarBoslukAlt = sablon.SayfaKenarBoslukAlt;

        // Logo ve Başlık
        mevcut.LogoGoster = sablon.LogoGoster;
        mevcut.LogoKonumu = sablon.LogoKonumu;
        mevcut.LogoGenislik = sablon.LogoGenislik;
        mevcut.LogoYukseklik = sablon.LogoYukseklik;
        mevcut.OzelLogo = sablon.OzelLogo;
        mevcut.FirmaAdiGoster = sablon.FirmaAdiGoster;
        mevcut.FirmaAdiFontBoyutu = sablon.FirmaAdiFontBoyutu;
        mevcut.FirmaAdresGoster = sablon.FirmaAdresGoster;
        mevcut.FirmaTelefonGoster = sablon.FirmaTelefonGoster;
        mevcut.FirmaEmailGoster = sablon.FirmaEmailGoster;
        mevcut.FirmaVergiGoster = sablon.FirmaVergiGoster;

        // Renkler
        mevcut.AnaPrimaryRenk = sablon.AnaPrimaryRenk;
        mevcut.AnaSecondaryRenk = sablon.AnaSecondaryRenk;
        mevcut.TabloBaslikArkaplanRenk = sablon.TabloBaslikArkaplanRenk;
        mevcut.TabloBaslikYaziRenk = sablon.TabloBaslikYaziRenk;
        mevcut.TabloSatirCizgiRenk = sablon.TabloSatirCizgiRenk;
        mevcut.ToplamArkaplanRenk = sablon.ToplamArkaplanRenk;

        // Font
        mevcut.FontAdi = sablon.FontAdi;
        mevcut.VarsayilanFontBoyutu = sablon.VarsayilanFontBoyutu;
        mevcut.BaslikFontBoyutu = sablon.BaslikFontBoyutu;

        // Fatura Başlığı
        mevcut.FaturaBaslikMetni = sablon.FaturaBaslikMetni;
        mevcut.FaturaBaslikKonumu = sablon.FaturaBaslikKonumu;

        // Bilgi Kutuları
        mevcut.FaturaBilgiKutusuGoster = sablon.FaturaBilgiKutusuGoster;
        mevcut.CariBilgiKutusuGoster = sablon.CariBilgiKutusuGoster;
        mevcut.KutuCercevesiGoster = sablon.KutuCercevesiGoster;
        mevcut.KutuPadding = sablon.KutuPadding;

        // Tablo
        mevcut.TabloSiraNoGoster = sablon.TabloSiraNoGoster;
        mevcut.TabloKdvSutunuGoster = sablon.TabloKdvSutunuGoster;
        mevcut.TabloIskontoSutunuGoster = sablon.TabloIskontoSutunuGoster;
        mevcut.TabloZebraDeseni = sablon.TabloZebraDeseni;
        mevcut.TabloZebraRenk = sablon.TabloZebraRenk;

        // Toplam
        mevcut.ToplamKonumu = sablon.ToplamKonumu;
        mevcut.ToplamBolumGenislik = sablon.ToplamBolumGenislik;
        mevcut.AraToplamGoster = sablon.AraToplamGoster;
        mevcut.KdvToplamGoster = sablon.KdvToplamGoster;
        mevcut.OdenenGoster = sablon.OdenenGoster;
        mevcut.KalanGoster = sablon.KalanGoster;

        // Banka
        mevcut.BankaBilgileriGoster = sablon.BankaBilgileriGoster;
        mevcut.BankaBilgileri = sablon.BankaBilgileri;

        // Alt Bilgi
        mevcut.NotlarGoster = sablon.NotlarGoster;
        mevcut.AltBilgiMetni = sablon.AltBilgiMetni;
        mevcut.SayfaNumarasiGoster = sablon.SayfaNumarasiGoster;

        // Kaşe ve İmza
        mevcut.KaseAlaniGoster = sablon.KaseAlaniGoster;
        mevcut.ImzaAlaniGoster = sablon.ImzaAlaniGoster;
        mevcut.ImzaMetni = sablon.ImzaMetni;
        mevcut.KaseResmi = sablon.KaseResmi;

        // QR Kod
        mevcut.QrKodGoster = sablon.QrKodGoster;
        mevcut.QrKodIcerik = sablon.QrKodIcerik;

        mevcut.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        _logger.LogInformation("Fatura şablonu güncellendi: {SablonId}", sablon.Id);
        return true;
    }

    public async Task<bool> SablonSilAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sablon = await context.FaturaSablonlari.FindAsync(id);
        if (sablon == null) return false;

        // Varsayılan şablon siliniyorsa başka birini varsayılan yap
        if (sablon.Varsayilan)
        {
            var digerSablon = await context.FaturaSablonlari
                .Where(s => s.FirmaId == sablon.FirmaId && s.Id != id && s.Aktif)
                .FirstOrDefaultAsync();
            if (digerSablon != null)
            {
                digerSablon.Varsayilan = true;
            }
        }

        context.FaturaSablonlari.Remove(sablon);
        await context.SaveChangesAsync();

        _logger.LogInformation("Fatura şablonu silindi: {SablonId}", id);
        return true;
    }

    public async Task<bool> VarsayilanYapAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sablon = await context.FaturaSablonlari.FindAsync(id);
        if (sablon == null) return false;

        // Mevcut varsayılanları kaldır
        await context.FaturaSablonlari
            .Where(s => s.FirmaId == sablon.FirmaId && s.Varsayilan)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Varsayilan, false));

        // Yenisini varsayılan yap
        sablon.Varsayilan = true;
        sablon.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();

        _logger.LogInformation("Fatura şablonu varsayılan yapıldı: {SablonId}", id);
        return true;
    }

    public async Task<FaturaSablon> SablonKopyalaAsync(int id, string yeniAd)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var kaynak = await context.FaturaSablonlari.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (kaynak == null)
            throw new InvalidOperationException("Kaynak şablon bulunamadı.");

        var yeni = new FaturaSablon
        {
            FirmaId = kaynak.FirmaId,
            SablonAdi = yeniAd,
            Varsayilan = false,
            Aktif = true,
            SayfaBoyutu = kaynak.SayfaBoyutu,
            SayfaYonelimi = kaynak.SayfaYonelimi,
            SayfaKenarBoslukSol = kaynak.SayfaKenarBoslukSol,
            SayfaKenarBoslukSag = kaynak.SayfaKenarBoslukSag,
            SayfaKenarBoslukUst = kaynak.SayfaKenarBoslukUst,
            SayfaKenarBoslukAlt = kaynak.SayfaKenarBoslukAlt,
            LogoGoster = kaynak.LogoGoster,
            LogoKonumu = kaynak.LogoKonumu,
            LogoGenislik = kaynak.LogoGenislik,
            LogoYukseklik = kaynak.LogoYukseklik,
            OzelLogo = kaynak.OzelLogo,
            FirmaAdiGoster = kaynak.FirmaAdiGoster,
            FirmaAdiFontBoyutu = kaynak.FirmaAdiFontBoyutu,
            FirmaAdresGoster = kaynak.FirmaAdresGoster,
            FirmaTelefonGoster = kaynak.FirmaTelefonGoster,
            FirmaEmailGoster = kaynak.FirmaEmailGoster,
            FirmaVergiGoster = kaynak.FirmaVergiGoster,
            AnaPrimaryRenk = kaynak.AnaPrimaryRenk,
            AnaSecondaryRenk = kaynak.AnaSecondaryRenk,
            TabloBaslikArkaplanRenk = kaynak.TabloBaslikArkaplanRenk,
            TabloBaslikYaziRenk = kaynak.TabloBaslikYaziRenk,
            TabloSatirCizgiRenk = kaynak.TabloSatirCizgiRenk,
            ToplamArkaplanRenk = kaynak.ToplamArkaplanRenk,
            FontAdi = kaynak.FontAdi,
            VarsayilanFontBoyutu = kaynak.VarsayilanFontBoyutu,
            BaslikFontBoyutu = kaynak.BaslikFontBoyutu,
            FaturaBaslikMetni = kaynak.FaturaBaslikMetni,
            FaturaBaslikKonumu = kaynak.FaturaBaslikKonumu,
            FaturaBilgiKutusuGoster = kaynak.FaturaBilgiKutusuGoster,
            CariBilgiKutusuGoster = kaynak.CariBilgiKutusuGoster,
            KutuCercevesiGoster = kaynak.KutuCercevesiGoster,
            KutuPadding = kaynak.KutuPadding,
            TabloSiraNoGoster = kaynak.TabloSiraNoGoster,
            TabloKdvSutunuGoster = kaynak.TabloKdvSutunuGoster,
            TabloIskontoSutunuGoster = kaynak.TabloIskontoSutunuGoster,
            TabloZebraDeseni = kaynak.TabloZebraDeseni,
            TabloZebraRenk = kaynak.TabloZebraRenk,
            ToplamKonumu = kaynak.ToplamKonumu,
            ToplamBolumGenislik = kaynak.ToplamBolumGenislik,
            AraToplamGoster = kaynak.AraToplamGoster,
            KdvToplamGoster = kaynak.KdvToplamGoster,
            OdenenGoster = kaynak.OdenenGoster,
            KalanGoster = kaynak.KalanGoster,
            BankaBilgileriGoster = kaynak.BankaBilgileriGoster,
            BankaBilgileri = kaynak.BankaBilgileri,
            NotlarGoster = kaynak.NotlarGoster,
            AltBilgiMetni = kaynak.AltBilgiMetni,
            SayfaNumarasiGoster = kaynak.SayfaNumarasiGoster,
            KaseAlaniGoster = kaynak.KaseAlaniGoster,
            ImzaAlaniGoster = kaynak.ImzaAlaniGoster,
            ImzaMetni = kaynak.ImzaMetni,
            KaseResmi = kaynak.KaseResmi,
            QrKodGoster = kaynak.QrKodGoster,
            QrKodIcerik = kaynak.QrKodIcerik,
            CreatedAt = DateTime.Now
        };

        context.FaturaSablonlari.Add(yeni);
        await context.SaveChangesAsync();

        _logger.LogInformation("Fatura şablonu kopyalandı: {KaynakId} -> {YeniId}", id, yeni.Id);
        return yeni;
    }

    #endregion

    #region PDF Oluşturma

    public async Task<FaturaPdfResult> FaturaPdfOlusturAsync(int faturaId, int? sablonId = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var fatura = await context.Faturalar
            .Include(f => f.Cari)
            .Include(f => f.Firma)
            .Include(f => f.FaturaKalemleri)
            .FirstOrDefaultAsync(f => f.Id == faturaId);

        if (fatura == null)
        {
            return new FaturaPdfResult
            {
                Basarili = false,
                Mesaj = "Fatura bulunamadı."
            };
        }

        FaturaSablon? sablon = null;
        if (sablonId.HasValue)
        {
            sablon = await SablonGetirAsync(sablonId.Value);
        }
        sablon ??= await VarsayilanSablonGetirAsync();

        return await FaturaPdfOlusturAsync(fatura, sablon);
    }

    public async Task<FaturaPdfResult> FaturaPdfOlusturAsync(Fatura fatura, FaturaSablon? sablon = null)
    {
        try
        {
            // Varsayılan şablon yoksa temel ayarlarla oluştur
            sablon ??= new FaturaSablon();

            // Firma bilgisini al
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var firma = fatura.Firma ?? await context.Firmalar.FindAsync(fatura.FirmaId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Sayfa boyutu ve yönelimi
                    var pageSize = sablon.SayfaBoyutu switch
                    {
                        SayfaBoyutu.A5 => PageSizes.A5,
                        SayfaBoyutu.Letter => PageSizes.Letter,
                        _ => PageSizes.A4
                    };

                    if (sablon.SayfaYonelimi == SayfaYonelimi.Yatay)
                        pageSize = pageSize.Landscape();

                    page.Size(pageSize);
                    page.MarginLeft(sablon.SayfaKenarBoslukSol);
                    page.MarginRight(sablon.SayfaKenarBoslukSag);
                    page.MarginTop(sablon.SayfaKenarBoslukUst);
                    page.MarginBottom(sablon.SayfaKenarBoslukAlt);
                    page.DefaultTextStyle(x => x.FontSize(sablon.VarsayilanFontBoyutu));

                    page.Header().Element(c => ComposeHeader(c, fatura, firma, sablon));
                    page.Content().Element(c => ComposeContent(c, fatura, sablon));
                    page.Footer().Element(c => ComposeFooter(c, sablon));
                });
            });

            var pdfData = document.GeneratePdf();
            var dosyaAdi = $"Fatura_{fatura.FaturaNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            _logger.LogInformation("Fatura PDF oluşturuldu: {FaturaNo}", fatura.FaturaNo);

            return new FaturaPdfResult
            {
                Basarili = true,
                PdfData = pdfData,
                DosyaAdi = dosyaAdi,
                Base64Data = Convert.ToBase64String(pdfData)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura PDF oluşturma hatası: {FaturaId}", fatura.Id);
            return new FaturaPdfResult
            {
                Basarili = false,
                Mesaj = $"PDF oluşturulurken hata: {ex.Message}"
            };
        }
    }

    public async Task<byte[]> OnizlemePdfOlusturAsync(FaturaSablon sablon)
    {
        // Örnek fatura verisi ile önizleme oluştur
        var ornekFatura = new Fatura
        {
            FaturaNo = "FTR-2025-0001",
            FaturaTarihi = DateTime.Today,
            VadeTarihi = DateTime.Today.AddDays(30),
            AraToplam = 10000m,
            KdvOrani = 20,
            KdvTutar = 2000m,
            GenelToplam = 12000m,
            OdenenTutar = 5000m,
            Notlar = "Örnek fatura notları...",
            FaturaKalemleri = new List<FaturaKalem>
            {
                new() { Aciklama = "Servis Hizmeti - Mart 2025", Miktar = 10, BirimFiyat = 500, ToplamTutar = 5000 },
                new() { Aciklama = "Transfer Hizmeti", Miktar = 20, BirimFiyat = 250, ToplamTutar = 5000 }
            },
            Cari = new Cari
            {
                Unvan = "Örnek Müşteri A.Ş.",
                VergiNo = "1234567890",
                VergiDairesi = "Beyoğlu V.D.",
                Adres = "Örnek Mahallesi, Test Caddesi No:123, İstanbul"
            }
        };

        // Firma bilgisi
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var aktifFirma = _firmaService.GetAktifFirma();
        var firma = await context.Firmalar.FindAsync(aktifFirma.FirmaId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                var pageSize = sablon.SayfaBoyutu switch
                {
                    SayfaBoyutu.A5 => PageSizes.A5,
                    SayfaBoyutu.Letter => PageSizes.Letter,
                    _ => PageSizes.A4
                };

                if (sablon.SayfaYonelimi == SayfaYonelimi.Yatay)
                    pageSize = pageSize.Landscape();

                page.Size(pageSize);
                page.MarginLeft(sablon.SayfaKenarBoslukSol);
                page.MarginRight(sablon.SayfaKenarBoslukSag);
                page.MarginTop(sablon.SayfaKenarBoslukUst);
                page.MarginBottom(sablon.SayfaKenarBoslukAlt);
                page.DefaultTextStyle(x => x.FontSize(sablon.VarsayilanFontBoyutu));

                page.Header().Element(c => ComposeHeader(c, ornekFatura, firma, sablon));
                page.Content().Element(c => ComposeContent(c, ornekFatura, sablon));
                page.Footer().Element(c => ComposeFooter(c, sablon));
            });
        });

        return document.GeneratePdf();
    }

    #endregion

    #region Email Gönderimi

    public async Task<bool> FaturaEmailGonderAsync(FaturaYazdirRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EmailAdresi))
            {
                _logger.LogWarning("Email adresi belirtilmedi: FaturaId={FaturaId}", request.FaturaId);
                return false;
            }

            var pdfResult = await FaturaPdfOlusturAsync(request.FaturaId, request.SablonId);
            if (!pdfResult.Basarili || pdfResult.PdfData == null)
            {
                _logger.LogWarning("PDF oluşturulamadı: FaturaId={FaturaId}", request.FaturaId);
                return false;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var fatura = await context.Faturalar
                .Include(f => f.Cari)
                .FirstOrDefaultAsync(f => f.Id == request.FaturaId);

            var konu = request.EmailKonu ?? $"Fatura: {fatura?.FaturaNo}";
            var mesaj = request.EmailMesaj ?? $"""
                Sayın Yetkili,

                {fatura?.FaturaNo} numaralı faturanız ekte gönderilmektedir.

                Fatura Tarihi: {fatura?.FaturaTarihi:dd.MM.yyyy}
                Vade Tarihi: {fatura?.VadeTarihi:dd.MM.yyyy}
                Toplam Tutar: {fatura?.GenelToplam:N2} TL

                Saygılarımızla,
                """;

            var sonuc = await SendEmailWithAttachmentAsync(
                request.EmailAdresi,
                konu,
                mesaj,
                pdfResult.PdfData,
                pdfResult.DosyaAdi ?? $"Fatura_{fatura?.FaturaNo}.pdf"
            );

            if (sonuc)
            {
                _logger.LogInformation("Fatura email gönderildi: FaturaId={FaturaId}, Email={Email}",
                    request.FaturaId, request.EmailAdresi);
            }

            return sonuc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura email gönderme hatası: FaturaId={FaturaId}", request.FaturaId);
            return false;
        }
    }

    public async Task<bool> TopluFaturaEmailGonderAsync(List<int> faturaIds, int? sablonId = null, string? emailKonu = null, string? emailMesaj = null)
    {
        var basariliSayisi = 0;

        foreach (var faturaId in faturaIds)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var fatura = await context.Faturalar
                .Include(f => f.Cari)
                .FirstOrDefaultAsync(f => f.Id == faturaId);

            if (fatura?.Cari?.Email == null) continue;

            var sonuc = await FaturaEmailGonderAsync(new FaturaYazdirRequest
            {
                FaturaId = faturaId,
                SablonId = sablonId,
                EmailGonder = true,
                EmailAdresi = fatura.Cari.Email,
                EmailKonu = emailKonu,
                EmailMesaj = emailMesaj
            });

            if (sonuc) basariliSayisi++;
        }

        _logger.LogInformation("Toplu fatura email gönderildi: {Basarili}/{Toplam}", basariliSayisi, faturaIds.Count);
        return basariliSayisi > 0;
    }

    #endregion

    #region Logo ve Kaşe İşlemleri

    public async Task<bool> LogoYukleAsync(int sablonId, byte[] logoData, string dosyaAdi)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var sablon = await context.FaturaSablonlari.FindAsync(sablonId);
        if (sablon == null) return false;

        sablon.OzelLogo = Convert.ToBase64String(logoData);
        sablon.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();

        _logger.LogInformation("Şablon logosu yüklendi: {SablonId}", sablonId);
        return true;
    }

    public async Task<bool> LogoSilAsync(int sablonId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var sablon = await context.FaturaSablonlari.FindAsync(sablonId);
        if (sablon == null) return false;

        sablon.OzelLogo = null;
        sablon.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();

        _logger.LogInformation("Şablon logosu silindi: {SablonId}", sablonId);
        return true;
    }

    public async Task<bool> KaseYukleAsync(int sablonId, byte[] kaseData, string dosyaAdi)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var sablon = await context.FaturaSablonlari.FindAsync(sablonId);
        if (sablon == null) return false;

        sablon.KaseResmi = Convert.ToBase64String(kaseData);
        sablon.KaseAlaniGoster = true;
        sablon.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();

        _logger.LogInformation("Şablon kaşesi yüklendi: {SablonId}", sablonId);
        return true;
    }

    public async Task<bool> KaseSilAsync(int sablonId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var sablon = await context.FaturaSablonlari.FindAsync(sablonId);
        if (sablon == null) return false;

        sablon.KaseResmi = null;
        sablon.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();

        _logger.LogInformation("Şablon kaşesi silindi: {SablonId}", sablonId);
        return true;
    }

    #endregion

    #region PDF Compose Metodları

    private void ComposeHeader(IContainer container, Fatura fatura, Firma? firma, FaturaSablon sablon)
    {
        var primaryColor = ParseColor(sablon.AnaPrimaryRenk);
        var secondaryColor = ParseColor(sablon.AnaSecondaryRenk);

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Sol taraf: Logo ve Firma bilgileri
                row.RelativeItem().Column(firmaCol =>
                {
                    // Logo
                    if (sablon.LogoGoster)
                    {
                        var logoData = sablon.OzelLogo ?? firma?.Logo;
                        if (!string.IsNullOrEmpty(logoData))
                        {
                            try
                            {
                                var logoBytes = Convert.FromBase64String(logoData);
#pragma warning disable CS0618
                                firmaCol.Item()
                                    .Width(sablon.LogoGenislik)
                                    .Height(sablon.LogoYukseklik)
                                    .Image(logoBytes, ImageScaling.FitArea);
#pragma warning restore CS0618
                            }
                            catch
                            {
                                // Logo yüklenemezse devam et
                            }
                        }
                    }

                    // Firma Adı
                    if (sablon.FirmaAdiGoster && firma != null)
                    {
                        firmaCol.Item().PaddingTop(5)
                            .Text(firma.FirmaAdi)
                            .Bold()
                            .FontSize(sablon.FirmaAdiFontBoyutu)
                            .FontColor(primaryColor);

                        if (!string.IsNullOrEmpty(firma.UnvanTam))
                        {
                            firmaCol.Item().Text(firma.UnvanTam).FontSize(9).FontColor(secondaryColor);
                        }
                    }

                    // Firma Adres
                    if (sablon.FirmaAdresGoster && !string.IsNullOrEmpty(firma?.Adres))
                    {
                        firmaCol.Item().PaddingTop(3).Text(firma.Adres).FontSize(8);
                    }

                    // Firma İletişim
                    if (sablon.FirmaTelefonGoster && !string.IsNullOrEmpty(firma?.Telefon))
                    {
                        firmaCol.Item().Text($"Tel: {firma.Telefon}").FontSize(8);
                    }
                    if (sablon.FirmaEmailGoster && !string.IsNullOrEmpty(firma?.Email))
                    {
                        firmaCol.Item().Text($"E-posta: {firma.Email}").FontSize(8);
                    }

                    // Vergi Bilgisi
                    if (sablon.FirmaVergiGoster && !string.IsNullOrEmpty(firma?.VergiNo))
                    {
                        firmaCol.Item().PaddingTop(2)
                            .Text($"V.D.: {firma.VergiDairesi} / {firma.VergiNo}")
                            .FontSize(8);
                    }
                });

                // Sağ taraf: Fatura başlığı ve bilgileri
                row.RelativeItem().AlignRight().Column(faturaCol =>
                {
                    // Fatura Başlığı
                    faturaCol.Item()
                        .Text(sablon.FaturaBaslikMetni)
                        .Bold()
                        .FontSize(sablon.BaslikFontBoyutu)
                        .FontColor(primaryColor);

                    faturaCol.Item().PaddingTop(10);

                    // Fatura Bilgileri
                    if (sablon.FaturaBilgiKutusuGoster)
                    {
                        var bilgiKutusu = faturaCol.Item();
                        if (sablon.KutuCercevesiGoster)
                            bilgiKutusu = bilgiKutusu.Border(1).BorderColor(Colors.Grey.Lighten1);

                        bilgiKutusu.Padding(sablon.KutuPadding).Column(infoCol =>
                        {
                            infoCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Fatura No:").Bold().FontSize(9);
                                r.RelativeItem().AlignRight().Text(fatura.FaturaNo).FontSize(9);
                            });
                            infoCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Tarih:").Bold().FontSize(9);
                                r.RelativeItem().AlignRight().Text(fatura.FaturaTarihi.ToString("dd.MM.yyyy")).FontSize(9);
                            });
                            if (fatura.VadeTarihi.HasValue)
                            {
                                infoCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Vade:").Bold().FontSize(9);
                                    r.RelativeItem().AlignRight().Text(fatura.VadeTarihi.Value.ToString("dd.MM.yyyy")).FontSize(9);
                                });
                            }
                        });
                    }
                });
            });

            col.Item().PaddingVertical(10);

            // Cari Bilgileri
            if (sablon.CariBilgiKutusuGoster && fatura.Cari != null)
            {
                var cariBilgi = col.Item();
                if (sablon.KutuCercevesiGoster)
                    cariBilgi = cariBilgi.Border(1).BorderColor(Colors.Grey.Lighten1);

                cariBilgi.Padding(sablon.KutuPadding).Column(cariCol =>
                {
                    cariCol.Item().Text("MÜŞTERİ BİLGİLERİ").Bold().FontSize(10).FontColor(primaryColor);
                    cariCol.Item().PaddingTop(5);
                    cariCol.Item().Text(fatura.Cari.Unvan).Bold();
                    if (!string.IsNullOrEmpty(fatura.Cari.VergiNo))
                        cariCol.Item().Text($"V.D.: {fatura.Cari.VergiDairesi} / {fatura.Cari.VergiNo}").FontSize(9);
                    if (!string.IsNullOrEmpty(fatura.Cari.Adres))
                        cariCol.Item().Text(fatura.Cari.Adres).FontSize(9);
                });
            }

            col.Item().PaddingVertical(10);
        });
    }

    private void ComposeContent(IContainer container, Fatura fatura, FaturaSablon sablon)
    {
        var primaryColor = ParseColor(sablon.AnaPrimaryRenk);
        var headerBgColor = ParseColor(sablon.TabloBaslikArkaplanRenk);
        var headerTextColor = ParseColor(sablon.TabloBaslikYaziRenk);
        var lineColor = ParseColor(sablon.TabloSatirCizgiRenk);
        var zebraColor = ParseColor(sablon.TabloZebraRenk);
        var totalBgColor = ParseColor(sablon.ToplamArkaplanRenk);

        container.Column(col =>
        {
            // Fatura Kalemleri Tablosu
            if (fatura.FaturaKalemleri?.Any() == true)
            {
                col.Item().Table(table =>
                {
                    // Sütun tanımları
                    table.ColumnsDefinition(columns =>
                    {
                        if (sablon.TabloSiraNoGoster)
                            columns.ConstantColumn(30);
                        columns.RelativeColumn(3); // Açıklama
                        columns.RelativeColumn(1); // Miktar
                        columns.RelativeColumn(1); // Birim Fiyat
                        if (sablon.TabloKdvSutunuGoster)
                            columns.RelativeColumn(1); // KDV
                        if (sablon.TabloIskontoSutunuGoster)
                            columns.RelativeColumn(1); // İskonto
                        columns.RelativeColumn(1); // Tutar
                    });

                    // Başlık satırı
                    table.Header(header =>
                    {
                        if (sablon.TabloSiraNoGoster)
                            header.Cell().Background(headerBgColor).Padding(5).Text("#").Bold().FontColor(headerTextColor);
                        header.Cell().Background(headerBgColor).Padding(5).Text("Açıklama").Bold().FontColor(headerTextColor);
                        header.Cell().Background(headerBgColor).Padding(5).AlignRight().Text("Miktar").Bold().FontColor(headerTextColor);
                        header.Cell().Background(headerBgColor).Padding(5).AlignRight().Text("B.Fiyat").Bold().FontColor(headerTextColor);
                        if (sablon.TabloKdvSutunuGoster)
                            header.Cell().Background(headerBgColor).Padding(5).AlignRight().Text("KDV").Bold().FontColor(headerTextColor);
                        if (sablon.TabloIskontoSutunuGoster)
                            header.Cell().Background(headerBgColor).Padding(5).AlignRight().Text("İsk.").Bold().FontColor(headerTextColor);
                        header.Cell().Background(headerBgColor).Padding(5).AlignRight().Text("Tutar").Bold().FontColor(headerTextColor);
                    });

                    // Veri satırları
                    int sira = 1;
                    foreach (var kalem in fatura.FaturaKalemleri)
                    {
                        var rowBg = sablon.TabloZebraDeseni && sira % 2 == 0 ? zebraColor : Colors.White;

                        if (sablon.TabloSiraNoGoster)
                            table.Cell().Background(rowBg).BorderBottom(1).BorderColor(lineColor).Padding(5).Text(sira.ToString());
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor(lineColor).Padding(5).Text(kalem.Aciklama);
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor(lineColor).Padding(5).AlignRight().Text(kalem.Miktar.ToString("N2"));
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor(lineColor).Padding(5).AlignRight().Text($"{kalem.BirimFiyat:N2} ₺");
                        if (sablon.TabloKdvSutunuGoster)
                            table.Cell().Background(rowBg).BorderBottom(1).BorderColor(lineColor).Padding(5).AlignRight().Text($"%{kalem.KdvOrani}");
                        if (sablon.TabloIskontoSutunuGoster)
                            table.Cell().Background(rowBg).BorderBottom(1).BorderColor(lineColor).Padding(5).AlignRight().Text($"{kalem.IskontoTutar:N2} ₺");
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor(lineColor).Padding(5).AlignRight().Text($"{kalem.ToplamTutar:N2} ₺");

                        sira++;
                    }
                });
            }

            col.Item().PaddingVertical(10);

            // Toplam Bölümü
            var toplamContainer = sablon.ToplamKonumu switch
            {
                ToplamKonumu.Sol => col.Item().AlignLeft(),
                ToplamKonumu.Orta => col.Item().AlignCenter(),
                _ => col.Item().AlignRight()
            };

            toplamContainer.Width(sablon.ToplamBolumGenislik).Background(totalBgColor).Padding(10).Column(totals =>
            {
                if (sablon.AraToplamGoster)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Ara Toplam:");
                        r.RelativeItem().AlignRight().Text($"{fatura.AraToplam:N2} ₺");
                    });
                }
                if (sablon.KdvToplamGoster)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"KDV (%{fatura.KdvOrani}):");
                        r.RelativeItem().AlignRight().Text($"{fatura.KdvTutar:N2} ₺");
                    });
                }
                totals.Item().PaddingTop(5).BorderTop(1).Row(r =>
                {
                    r.RelativeItem().Text("GENEL TOPLAM:").Bold().FontColor(primaryColor);
                    r.RelativeItem().AlignRight().Text($"{fatura.GenelToplam:N2} ₺").Bold().FontColor(primaryColor);
                });
                if (sablon.OdenenGoster && fatura.OdenenTutar > 0)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Ödenen:");
                        r.RelativeItem().AlignRight().Text($"{fatura.OdenenTutar:N2} ₺").FontColor(Colors.Green.Medium);
                    });
                }
                if (sablon.KalanGoster && fatura.KalanTutar > 0)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Kalan:").Bold();
                        r.RelativeItem().AlignRight().Text($"{fatura.KalanTutar:N2} ₺").Bold().FontColor(Colors.Red.Medium);
                    });
                }
            });

            // Banka Bilgileri
            if (sablon.BankaBilgileriGoster && !string.IsNullOrEmpty(sablon.BankaBilgileri))
            {
                col.Item().PaddingTop(15).Column(bankaCol =>
                {
                    bankaCol.Item().Text("BANKA BİLGİLERİ").Bold().FontSize(10).FontColor(primaryColor);
                    bankaCol.Item().PaddingTop(5).Text(sablon.BankaBilgileri).FontSize(9);
                });
            }

            // Notlar
            if (sablon.NotlarGoster && !string.IsNullOrEmpty(fatura.Notlar))
            {
                col.Item().PaddingTop(15).Column(notCol =>
                {
                    notCol.Item().Text("NOTLAR").Bold().FontSize(10);
                    notCol.Item().PaddingTop(3).Text(fatura.Notlar).FontSize(9);
                });
            }

            // Alt Bilgi Metni
            if (!string.IsNullOrEmpty(sablon.AltBilgiMetni))
            {
                col.Item().PaddingTop(15).Text(sablon.AltBilgiMetni).FontSize(8).FontColor(Colors.Grey.Medium);
            }

            // Kaşe ve İmza Alanları
            if (sablon.KaseAlaniGoster || sablon.ImzaAlaniGoster)
            {
                col.Item().PaddingTop(30).Row(row =>
                {
                    if (sablon.KaseAlaniGoster)
                    {
                        row.RelativeItem().Column(kaseCol =>
                        {
                            if (!string.IsNullOrEmpty(sablon.KaseResmi))
                            {
                                try
                                {
                                    var kaseBytes = Convert.FromBase64String(sablon.KaseResmi);
#pragma warning disable CS0618
                                    kaseCol.Item().Width(100).Height(60).Image(kaseBytes, ImageScaling.FitArea);
#pragma warning restore CS0618
                                }
                                catch { }
                            }
                            kaseCol.Item().Text("Kaşe").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    }

                    if (sablon.ImzaAlaniGoster)
                    {
                        row.RelativeItem().AlignRight().Column(imzaCol =>
                        {
                            imzaCol.Item().Width(150).BorderBottom(1).BorderColor(Colors.Grey.Medium).Height(40);
                            imzaCol.Item().PaddingTop(5).AlignCenter().Text(sablon.ImzaMetni ?? "İmza").FontSize(8);
                        });
                    }
                });
            }
        });
    }

    private void ComposeFooter(IContainer container, FaturaSablon sablon)
    {
        if (sablon.SayfaNumarasiGoster)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span("Sayfa ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        }
    }

    private Color ParseColor(string hexColor)
    {
        try
        {
            if (string.IsNullOrEmpty(hexColor)) return Colors.Black;

            hexColor = hexColor.TrimStart('#');
            if (hexColor.Length == 6)
            {
                var r = Convert.ToByte(hexColor.Substring(0, 2), 16);
                var g = Convert.ToByte(hexColor.Substring(2, 2), 16);
                var b = Convert.ToByte(hexColor.Substring(4, 2), 16);
                return Color.FromRGB(r, g, b);
            }
        }
        catch { }

        return Colors.Black;
    }

    /// <summary>
    /// PDF eki ile e-posta gönderir
    /// </summary>
    private async Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] pdfBytes, string pdfFileName)
    {
        var enabled = _configuration.GetValue("Email:Enabled", false);
        if (!enabled)
        {
            _logger.LogWarning("E-posta servisi devre dışı. Mesaj gönderilmedi: {Subject}", subject);
            return false;
        }

        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = _configuration.GetValue("Email:SmtpPort", 587);
        var smtpUser = _configuration["Email:SmtpUser"] ?? "";
        var smtpPassword = _configuration["Email:SmtpPassword"] ?? "";
        var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
        var fromName = _configuration["Email:FromName"] ?? "CRM Filo Servis";
        var enableSsl = _configuration.GetValue("Email:EnableSsl", true);

        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            _logger.LogError("SMTP ayarları yapılandırılmamış");
            return false;
        }

        try
        {
            using var message = new System.Net.Mail.MailMessage();
            message.From = new System.Net.Mail.MailAddress(fromEmail, fromName);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            // PDF eki ekle
            using var pdfStream = new MemoryStream(pdfBytes);
            var attachment = new System.Net.Mail.Attachment(pdfStream, pdfFileName, "application/pdf");
            message.Attachments.Add(attachment);

            using var smtp = new System.Net.Mail.SmtpClient(smtpHost, smtpPort);
            smtp.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword);
            smtp.EnableSsl = enableSsl;

            await smtp.SendMailAsync(message);
            _logger.LogInformation("Fatura e-postası gönderildi: {To}, {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilirken hata oluştu: {To}", to);
            return false;
        }
    }

    #endregion
}



