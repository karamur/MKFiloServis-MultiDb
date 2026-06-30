using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Her sabah yöneticiye gönderilecek günlük özeti hazırlar.
/// Bugünkü seferler, vadesi geçmiş/yaklaşan faturalar, süresi dolan belgeler özetlenir.
/// </summary>
public class GunlukOzetService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWhatsAppService _whatsappService;
    private readonly ILogger<GunlukOzetService> _logger;
    private readonly IConfiguration _configuration;

    public GunlukOzetService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IWhatsAppService whatsappService,
        ILogger<GunlukOzetService> logger,
        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _whatsappService = whatsappService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task GonderGunlukOzetAsync(CancellationToken cancellationToken = default)
    {
        var whatsappKisiId = _configuration.GetValue("GunlukOzet:WhatsAppKisiId", 0);
        var whatsappGrupId = _configuration.GetValue("GunlukOzet:WhatsAppGrupId", 0);

        if (whatsappKisiId == 0 && whatsappGrupId == 0)
        {
            _logger.LogWarning("Gunluk ozet: GunlukOzet:WhatsAppKisiId veya GunlukOzet:WhatsAppGrupId yapilandirilmamis, gonderim atlaniyor");
            return;
        }

        try
        {
            var ozet = await HazirlaOzetAsync();
            var mesaj = MesajOlustur(ozet);

            if (whatsappGrupId > 0)
            {
                await _whatsappService.SendMesajToGrupAsync(whatsappGrupId, mesaj);
                _logger.LogInformation("Gunluk ozet WhatsApp mesaji gruba gonderildi (GrupId: {GrupId})", whatsappGrupId);
            }
            else
            {
                await _whatsappService.SendMesajToKisiAsync(whatsappKisiId, mesaj);
                _logger.LogInformation("Gunluk ozet WhatsApp mesaji kisiye gonderildi (KisiId: {KisiId})", whatsappKisiId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gunluk ozet gonderim hatasi");
            throw;
        }
    }

    private async Task<GunlukOzetDto> HazirlaOzetAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var yedi = bugun.AddDays(7);

        var bugunSeferSayisi = await context.ServisCalismalari
            .CountAsync(s => !s.IsDeleted && s.CalismaTarihi.Date == bugun);

        var tamamlananSeferSayisi = await context.ServisCalismalari
            .CountAsync(s => !s.IsDeleted && s.CalismaTarihi.Date == bugun
                          && s.Durum == CalismaDurum.Tamamlandi);

        var vadesiGecmisFatura = await context.Faturalar
            .CountAsync(f => !f.IsDeleted
                          && f.Durum != FaturaDurum.Odendi
                          && f.VadeTarihi.HasValue && f.VadeTarihi.Value < bugun);

        var buHaftaVadelenecek = await context.Faturalar
            .CountAsync(f => !f.IsDeleted
                          && f.Durum != FaturaDurum.Odendi
                          && f.VadeTarihi.HasValue
                          && f.VadeTarihi.Value >= bugun && f.VadeTarihi.Value <= yedi);

        var aracBelgeSayisi = await context.AracEvraklari
            .CountAsync(b => !b.IsDeleted
                          && b.BitisTarihi.HasValue
                          && b.BitisTarihi.Value >= bugun && b.BitisTarihi.Value <= yedi);

        var soforBelgeSayisi = await context.Soforler
            .CountAsync(s => s.Aktif && !s.IsDeleted &&
                ((s.EhliyetGecerlilikTarihi.HasValue && s.EhliyetGecerlilikTarihi.Value >= bugun && s.EhliyetGecerlilikTarihi.Value <= yedi) ||
                 (s.SrcBelgesiGecerlilikTarihi.HasValue && s.SrcBelgesiGecerlilikTarihi.Value >= bugun && s.SrcBelgesiGecerlilikTarihi.Value <= yedi) ||
                 (s.PsikoteknikGecerlilikTarihi.HasValue && s.PsikoteknikGecerlilikTarihi.Value >= bugun && s.PsikoteknikGecerlilikTarihi.Value <= yedi)));

        var aktifAracSayisi = await context.Araclar.CountAsync(a => a.Aktif && !a.IsDeleted);

        var bugunYeniFatura = await context.Faturalar
            .CountAsync(f => !f.IsDeleted && f.CreatedAt.Date == bugun);

        return new GunlukOzetDto
        {
            Tarih = bugun,
            BugunSeferSayisi = bugunSeferSayisi,
            TamamlananSeferSayisi = tamamlananSeferSayisi,
            VadesiGecmisFaturaSayisi = vadesiGecmisFatura,
            BuHaftaVadelenecekFaturaSayisi = buHaftaVadelenecek,
            YaklasanBelgeSayisi = aracBelgeSayisi + soforBelgeSayisi,
            AktifAracSayisi = aktifAracSayisi,
            BugunYeniFaturaSayisi = bugunYeniFatura,
        };
    }

    private static string MesajOlustur(GunlukOzetDto o)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Gunluk Filo Ozeti - {o.Tarih:dd.MM.yyyy dddd}");
        sb.AppendLine();

        sb.AppendLine("Seferler");
        sb.AppendLine($"  Bugun planlanan: {o.BugunSeferSayisi} sefer");
        if (o.TamamlananSeferSayisi > 0)
            sb.AppendLine($"  Tamamlanan: {o.TamamlananSeferSayisi} sefer");
        sb.AppendLine();

        sb.AppendLine("Faturalar");
        if (o.VadesiGecmisFaturaSayisi > 0)
            sb.AppendLine($"  Vadesi gecmis: {o.VadesiGecmisFaturaSayisi} fatura");
        if (o.BuHaftaVadelenecekFaturaSayisi > 0)
            sb.AppendLine($"  Bu hafta vadesi dolacak: {o.BuHaftaVadelenecekFaturaSayisi} fatura");
        if (o.BugunYeniFaturaSayisi > 0)
            sb.AppendLine($"  Bugun eklenen: {o.BugunYeniFaturaSayisi} fatura");
        if (o.VadesiGecmisFaturaSayisi == 0 && o.BuHaftaVadelenecekFaturaSayisi == 0)
            sb.AppendLine("  Acil fatura yok");
        sb.AppendLine();

        if (o.YaklasanBelgeSayisi > 0)
        {
            sb.AppendLine("Belgeler");
            sb.AppendLine($"  7 gun icinde dolacak: {o.YaklasanBelgeSayisi} belge");
            sb.AppendLine();
        }

        sb.AppendLine($"Filo: {o.AktifAracSayisi} aktif arac");
        sb.AppendLine();
        sb.Append("CRM Filo Servis - Otomatik Ozet");

        return sb.ToString();
    }
}

public class GunlukOzetDto
{
    public DateTime Tarih { get; set; }
    public int BugunSeferSayisi { get; set; }
    public int TamamlananSeferSayisi { get; set; }
    public int VadesiGecmisFaturaSayisi { get; set; }
    public int BuHaftaVadelenecekFaturaSayisi { get; set; }
    public int YaklasanBelgeSayisi { get; set; }
    public int AktifAracSayisi { get; set; }
    public int BugunYeniFaturaSayisi { get; set; }
}


