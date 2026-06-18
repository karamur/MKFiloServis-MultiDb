using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Puantaj sonrası fatura hazırlık raporu — READONLY API.
/// Faz 1: Test ve veri doğrulama amaçlı. UI yok.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PuantajFaturaRaporController : ControllerBase
{
    private readonly IPuantajFaturaRaporService _raporService;

    public PuantajFaturaRaporController(IPuantajFaturaRaporService raporService)
    {
        _raporService = raporService;
    }

    /// <summary>GET /api/puantaj-fatura-rapor/ozet?yil=2026&ay=6&yon=Gelir</summary>
    [HttpGet("ozet")]
    public async Task<IActionResult> GetOzet(
        [FromQuery] int yil,
        [FromQuery] int ay,
        [FromQuery] PuantajFaturaYonu yon = PuantajFaturaYonu.Gelir,
        [FromQuery] int? kurumId = null,
        [FromQuery] int? cariId = null,
        [FromQuery] int? aracId = null,
        [FromQuery] int? guzergahId = null)
    {
        var request = new PuantajFaturaRaporRequest
        {
            Yil = yil, Ay = ay, Yon = yon,
            KurumId = kurumId, CariId = cariId,
            AracId = aracId, GuzergahId = guzergahId
        };
        var sonuc = await _raporService.GetOzetAsync(request);
        return Ok(sonuc);
    }

    /// <summary>GET /api/puantaj-fatura-rapor/satirlar?yil=2026&ay=6&page=1&pageSize=50</summary>
    [HttpGet("satirlar")]
    public async Task<IActionResult> GetSatirlar(
        [FromQuery] int yil,
        [FromQuery] int ay,
        [FromQuery] PuantajFaturaYonu yon = PuantajFaturaYonu.Gelir,
        [FromQuery] int? kurumId = null,
        [FromQuery] int? cariId = null,
        [FromQuery] int? aracId = null,
        [FromQuery] int? guzergahId = null,
        [FromQuery] string? arama = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var request = new PuantajFaturaRaporRequest
        {
            Yil = yil, Ay = ay, Yon = yon,
            KurumId = kurumId, CariId = cariId,
            AracId = aracId, GuzergahId = guzergahId,
            Arama = arama, Page = page, PageSize = pageSize
        };
        var satirlar = await _raporService.GetSatirlarAsync(request);
        var total = await _raporService.GetCountAsync(request);
        return Ok(new { total, page, pageSize, data = satirlar });
    }

    /// <summary>GET /api/puantaj-fatura-rapor/agac?yil=2026&ay=6&agac=CariAracGuzergah</summary>
    [HttpGet("agac")]
    public async Task<IActionResult> GetAgac(
        [FromQuery] int yil,
        [FromQuery] int ay,
        [FromQuery] PuantajFaturaAgacYapisi agac = PuantajFaturaAgacYapisi.CariAracGuzergah,
        [FromQuery] PuantajFaturaYonu yon = PuantajFaturaYonu.Gelir,
        [FromQuery] int? kurumId = null,
        [FromQuery] int? cariId = null,
        [FromQuery] int? aracId = null,
        [FromQuery] int? guzergahId = null)
    {
        var request = new PuantajFaturaRaporRequest
        {
            Yil = yil, Ay = ay, Agac = agac, Yon = yon,
            KurumId = kurumId, CariId = cariId,
            AracId = aracId, GuzergahId = guzergahId
        };
        var agacSonuc = await _raporService.GetAgacAsync(request);
        return Ok(agacSonuc);
    }
}
