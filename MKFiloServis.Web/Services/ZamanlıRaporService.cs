using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class ZamanliRaporService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZamanliRaporService> _logger;

    public ZamanliRaporService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ZamanliRaporService> logger)
    {
        _contextFactory = contextFactory;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task GonderGunlukRaporAsync(CancellationToken ct = default)
    {
        var alicilar = _configuration.GetSection("ZamanlıRapor:AliciListesi").Get<string[]>()
                       ?? _configuration.GetSection("ZamanliRapor:AliciListesi").Get<string[]>()
                       ?? Array.Empty<string>();

        if (alicilar.Length == 0)
        {
            _logger.LogInformation("Zamanli rapor: alıcı listesi boş, gönderilmedi.");
            return;
        }

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
            var bugun = DateTime.Today;

            var faturaSayisi = await ctx.Faturalar.CountAsync(f => !f.IsDeleted, ct);
            var aracSayisi = await ctx.Araclar.CountAsync(a => !a.IsDeleted, ct);
            var soforSayisi = await ctx.Soforler.CountAsync(s => !s.IsDeleted, ct);
            var cariSayisi = await ctx.Cariler.CountAsync(c => !c.IsDeleted, ct);

            var vadesiGecmis = await ctx.Faturalar
                .CountAsync(f => !f.IsDeleted && f.VadeTarihi < bugun && f.Durum != MKFiloServis.Shared.Entities.FaturaDurum.Odendi, ct);

            var html = $@"
<html><body style='font-family:Segoe UI,Arial;max-width:600px;margin:auto;'>
<div style='background:#1e3c72;color:white;padding:20px;border-radius:8px 8px 0 0;'>
  <h2 style='margin:0;'>KOA Filo Servis - Günlük Rapor</h2>
  <p style='margin:4px 0 0;opacity:.8;'>{bugun:dd MMMM yyyy, dddd}</p>
</div>
<div style='background:#f8fafc;padding:20px;'>
  <table style='width:100%;border-collapse:collapse;'>
    <tr><td style='padding:12px;background:white;border:1px solid #e2e8f0;border-radius:6px;margin:4px;'>
      <div style='color:#64748b;font-size:12px;'>TOPLAM FATURA</div>
      <div style='color:#0f172a;font-size:24px;font-weight:bold;'>{faturaSayisi:N0}</div>
    </td><td style='width:8px;'></td>
    <td style='padding:12px;background:white;border:1px solid #e2e8f0;border-radius:6px;'>
      <div style='color:#64748b;font-size:12px;'>VADESİ GEÇMİŞ</div>
      <div style='color:#dc2626;font-size:24px;font-weight:bold;'>{vadesiGecmis:N0}</div>
    </td></tr>
    <tr><td style='height:8px;' colspan='3'></td></tr>
    <tr><td style='padding:12px;background:white;border:1px solid #e2e8f0;border-radius:6px;'>
      <div style='color:#64748b;font-size:12px;'>ARAÇ</div>
      <div style='color:#0f172a;font-size:24px;font-weight:bold;'>{aracSayisi:N0}</div>
    </td><td style='width:8px;'></td>
    <td style='padding:12px;background:white;border:1px solid #e2e8f0;border-radius:6px;'>
      <div style='color:#64748b;font-size:12px;'>ŞOFÖR</div>
      <div style='color:#0f172a;font-size:24px;font-weight:bold;'>{soforSayisi:N0}</div>
    </td></tr>
    <tr><td style='height:8px;' colspan='3'></td></tr>
    <tr><td colspan='3' style='padding:12px;background:white;border:1px solid #e2e8f0;border-radius:6px;'>
      <div style='color:#64748b;font-size:12px;'>CARİ HESAP</div>
      <div style='color:#0f172a;font-size:24px;font-weight:bold;'>{cariSayisi:N0}</div>
    </td></tr>
  </table>
</div>
<div style='background:#f1f5f9;padding:12px 20px;border-radius:0 0 8px 8px;text-align:center;'>
  <small style='color:#94a3b8;'>Bu rapor KOA Filo Servis tarafından otomatik gönderilmiştir.</small>
</div>
</body></html>";

            foreach (var alici in alicilar)
            {
                await _emailService.SendEmailAsync(alici, $"KOA Filo Servis - Günlük Rapor ({bugun:dd.MM.yyyy})", html);
            }

            _logger.LogInformation("Zamanli rapor {Count} alıcıya gönderildi.", alicilar.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanli rapor gönderilemedi.");
        }
    }
}



