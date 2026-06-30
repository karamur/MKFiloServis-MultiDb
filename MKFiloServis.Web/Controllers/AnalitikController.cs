using MKFiloServis.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Power BI / Grafana / n8n / Zapier için veri erişim endpoint'leri.
/// Power BI: OData-benzeri JSON array, Grafana: Prometheus-metrikleri ve JSON, n8n/Zapier: webhook tetikleyici verisi.
/// </summary>
[ApiController]
[Route("api/analitik")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class AnalitikController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public AnalitikController(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Power BI / OData-uyumlu endpoint'ler
    // Power BI Desktop'ta "Web" veri kaynağı olarak şu URL'leri kullanın:
    //   GET /api/analitik/odata/faturalar
    //   GET /api/analitik/odata/cariler
    //   GET /api/analitik/odata/araclar
    // ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Fatura özet verisi — Power BI / Grafana için</summary>
    [HttpGet("odata/faturalar")]
    public async Task<IActionResult> OdataFaturalar(
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null,
        [FromQuery] int? top = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var q = ctx.Faturalar
            .Where(f => !f.IsDeleted)
            .AsNoTracking();

        if (baslangic.HasValue) q = q.Where(f => f.FaturaTarihi >= baslangic.Value);
        if (bitis.HasValue) q = q.Where(f => f.FaturaTarihi <= bitis.Value);

        var liste = await q
            .OrderByDescending(f => f.FaturaTarihi)
            .Take(top ?? 10000)
            .Select(f => new
            {
                f.Id,
                f.FaturaNo,
                f.FaturaTarihi,
                f.VadeTarihi,
                Tutar = f.GenelToplam,
                Durum = f.Durum.ToString(),
                Tip = f.FaturaTipi.ToString(),
                f.CariId,
                CariAdi = f.Cari != null ? f.Cari.Unvan : null
            })
            .ToListAsync();

        return Ok(new { odataContext = "faturalar", value = liste, count = liste.Count });
    }

    /// <summary>Cari özet verisi — Power BI / Grafana için</summary>
    [HttpGet("odata/cariler")]
    public async Task<IActionResult> OdataCariler([FromQuery] int? top = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var liste = await ctx.Cariler
            .Where(c => !c.IsDeleted)
            .AsNoTracking()
            .OrderBy(c => c.Unvan)
            .Take(top ?? 5000)
            .Select(c => new
            {
                c.Id,
                c.Unvan,
                c.Telefon,
                c.Email,
                CariTipi = c.CariTipi.ToString(),
                c.Adres,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(new { odataContext = "cariler", value = liste, count = liste.Count });
    }

    /// <summary>Araç verisi — Power BI / Grafana için</summary>
    [HttpGet("odata/araclar")]
    public async Task<IActionResult> OdataAraclar([FromQuery] int? top = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var liste = await ctx.Araclar
            .Where(a => !a.IsDeleted)
            .AsNoTracking()
            .OrderBy(a => a.Plaka)
            .Take(top ?? 2000)
            .Select(a => new
            {
                a.Id,
                a.Plaka,
                a.Marka,
                a.Model,
                a.ModelYili,
                SahiplikTipi = a.SahiplikTipi.ToString()
            })
            .ToListAsync();

        return Ok(new { odataContext = "araclar", value = liste, count = liste.Count });
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Grafana JSON Datasource (SimpleJSON protokolü)
    // Grafana'da "JSON" plugin datasource olarak yapılandırın.
    // ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Grafana search endpoint — kullanılabilir metrik listesi</summary>
    [HttpPost("grafana/search")]
    [HttpGet("grafana/search")]
    [AllowAnonymous]
    public IActionResult GrafanaSearch()
    {
        return Ok(new[]
        {
            "fatura_sayisi", "cari_sayisi", "arac_sayisi", "sofor_sayisi",
            "bu_ay_gelir", "bu_ay_gider", "vadesi_gecmis_sayisi"
        });
    }

    /// <summary>Grafana query endpoint — metrik verilerini döner</summary>
    [HttpPost("grafana/query")]
    public async Task<IActionResult> GrafanaQuery()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);

        var faturaSayisi = await ctx.Faturalar.CountAsync(f => !f.IsDeleted);
        var cariSayisi = await ctx.Cariler.CountAsync(c => !c.IsDeleted);
        var aracSayisi = await ctx.Araclar.CountAsync(a => !a.IsDeleted);
        var soforSayisi = await ctx.Soforler.CountAsync(s => !s.IsDeleted);
        var vadesiGecmis = await ctx.Faturalar.CountAsync(f => !f.IsDeleted &&
            f.VadeTarihi < bugun && f.Durum != KOAFiloServis.Shared.Entities.FaturaDurum.Odendi);

        var buAyGelir = await ctx.BankaKasaHareketleri
            .Where(h => !h.IsDeleted && h.IslemTarihi >= ayBasi && h.HareketTipi == KOAFiloServis.Shared.Entities.HareketTipi.Giris)
            .SumAsync(h => (decimal?)h.Tutar) ?? 0;

        var buAyGider = await ctx.BankaKasaHareketleri
            .Where(h => !h.IsDeleted && h.IslemTarihi >= ayBasi && h.HareketTipi == KOAFiloServis.Shared.Entities.HareketTipi.Cikis)
            .SumAsync(h => (decimal?)h.Tutar) ?? 0;

        return Ok(new[]
        {
            new { target = "fatura_sayisi", datapoints = new[] { new object[] { faturaSayisi, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } } },
            new { target = "cari_sayisi", datapoints = new[] { new object[] { cariSayisi, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } } },
            new { target = "arac_sayisi", datapoints = new[] { new object[] { aracSayisi, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } } },
            new { target = "sofor_sayisi", datapoints = new[] { new object[] { soforSayisi, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } } },
            new { target = "vadesi_gecmis_sayisi", datapoints = new[] { new object[] { vadesiGecmis, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } } },
            new { target = "bu_ay_gelir", datapoints = new[] { new object[] { (double)buAyGelir, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } } },
            new { target = "bu_ay_gider", datapoints = new[] { new object[] { (double)buAyGider, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } } },
        });
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Prometheus metrik endpoint'i (Grafana'nın Prometheus datasource için)
    // ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Prometheus scrape endpoint — /api/analitik/metrics</summary>
    [HttpGet("metrics")]
    [AllowAnonymous]
    public async Task<IActionResult> Metrics()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);

        var faturaSayisi = await ctx.Faturalar.CountAsync(f => !f.IsDeleted);
        var cariSayisi = await ctx.Cariler.CountAsync(c => !c.IsDeleted);
        var aracSayisi = await ctx.Araclar.CountAsync(a => !a.IsDeleted);
        var soforSayisi = await ctx.Soforler.CountAsync(s => !s.IsDeleted);
        var vadesiGecmis = await ctx.Faturalar.CountAsync(f => !f.IsDeleted &&
            f.VadeTarihi < bugun && f.Durum != KOAFiloServis.Shared.Entities.FaturaDurum.Odendi);

        var metin = $@"# HELP koa_fatura_toplam Toplam fatura sayisi
# TYPE koa_fatura_toplam gauge
koa_fatura_toplam {faturaSayisi}
# HELP koa_cari_toplam Toplam cari sayisi
# TYPE koa_cari_toplam gauge
koa_cari_toplam {cariSayisi}
# HELP koa_arac_toplam Toplam arac sayisi
# TYPE koa_arac_toplam gauge
koa_arac_toplam {aracSayisi}
# HELP koa_sofor_toplam Toplam sofor sayisi
# TYPE koa_sofor_toplam gauge
koa_sofor_toplam {soforSayisi}
# HELP koa_fatura_vadesi_gecmis Vadesi gecmis fatura sayisi
# TYPE koa_fatura_vadesi_gecmis gauge
koa_fatura_vadesi_gecmis {vadesiGecmis}
";
        return Content(metin, "text/plain; charset=utf-8");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // n8n / Zapier trigger poll endpoint'i
    // n8n'de "HTTP Request" node ile GET /api/analitik/n8n/ozet kullanın.
    // Zapier'de "Webhook (Polling)" trigger ile aynı endpoint'i kullanın.
    // ──────────────────────────────────────────────────────────────────────────────

    /// <summary>n8n / Zapier polling trigger için anlık sistem özeti</summary>
    [HttpGet("n8n/ozet")]
    public async Task<IActionResult> N8nOzet()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);

        var vadesiGecmisFaturalar = await ctx.Faturalar
            .Where(f => !f.IsDeleted && f.VadeTarihi < bugun && f.Durum != KOAFiloServis.Shared.Entities.FaturaDurum.Odendi)
            .Select(f => new { f.Id, f.FaturaNo, f.VadeTarihi, Tutar = f.GenelToplam, CariAdi = f.Cari != null ? f.Cari.Unvan : null })
            .Take(50)
            .ToListAsync();

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            ozet = new
            {
                fatura_sayisi = await ctx.Faturalar.CountAsync(f => !f.IsDeleted),
                cari_sayisi = await ctx.Cariler.CountAsync(c => !c.IsDeleted),
                arac_sayisi = await ctx.Araclar.CountAsync(a => !a.IsDeleted),
                vadesi_gecmis_fatura_sayisi = vadesiGecmisFaturalar.Count,
            },
            vadesi_gecmis_faturalar = vadesiGecmisFaturalar,
            id = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
        });
    }

    /// <summary>n8n / Zapier için belirli bir tarihten sonra oluşturulan faturaları getirir</summary>
    [HttpGet("n8n/faturalar/yeni")]
    public async Task<IActionResult> N8nYeniFaturalar([FromQuery] DateTime? sonTarihten = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var baslangic = sonTarihten ?? DateTime.UtcNow.AddHours(-1);

        var liste = await ctx.Faturalar
            .Where(f => !f.IsDeleted && f.CreatedAt >= baslangic)
            .OrderByDescending(f => f.CreatedAt)
            .Take(100)
            .Select(f => new
            {
                f.Id,
                f.FaturaNo,
                f.FaturaTarihi,
                Tutar = f.GenelToplam,
                Durum = f.Durum.ToString(),
                CariAdi = f.Cari != null ? f.Cari.Unvan : null,
                f.CreatedAt
            })
            .ToListAsync();

        return Ok(new { count = liste.Count, items = liste });
    }
}



