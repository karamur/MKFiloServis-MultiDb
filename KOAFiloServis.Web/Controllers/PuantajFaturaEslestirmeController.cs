using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Puantaj ↔ Fatura eşleştirme ve fark raporu API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PuantajFaturaEslestirmeController : ControllerBase
{
    private readonly IPuantajFaturaEslestirmeService _eslestirmeService;

    public PuantajFaturaEslestirmeController(IPuantajFaturaEslestirmeService eslestirmeService)
    {
        _eslestirmeService = eslestirmeService;
    }

    /// <summary>GET /api/puantaj-fatura-eslestirme/analiz?yil=2026&ay=6 — otomatik eşleştirme analizi</summary>
    [HttpGet("analiz")]
    public async Task<IActionResult> EslesmeAnalizi([FromQuery] int yil, [FromQuery] int ay, [FromQuery] int? kurumId = null)
    {
        var rapor = await _eslestirmeService.EslesmeAnaliziYapAsync(yil, ay, kurumId);
        return Ok(rapor);
    }

    /// <summary>GET /api/puantaj-fatura-eslestirme/fark-raporu?yil=2026&ay=6 — sadece eşleşmeyen/farklı olanlar</summary>
    [HttpGet("fark-raporu")]
    public async Task<IActionResult> FarkRaporu([FromQuery] int yil, [FromQuery] int ay, [FromQuery] int? kurumId = null)
    {
        var farklar = await _eslestirmeService.FarkRaporuGetirAsync(yil, ay, kurumId);
        return Ok(farklar);
    }

    /// <summary>POST /api/puantaj-fatura-eslestirme/manuel — manuel eşleştir</summary>
    [HttpPost("manuel")]
    public async Task<IActionResult> ManuelEslestir([FromBody] ManuelEslesmeRequest request)
    {
        var result = await _eslestirmeService.ManuelEslestirAsync(request.PuantajKayitId, request.FaturaId);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>DELETE /api/puantaj-fatura-eslestirme/{puantajKayitId} — eşleştirmeyi kaldır</summary>
    [HttpDelete("{puantajKayitId:int}")]
    public async Task<IActionResult> EslesmeKaldir(int puantajKayitId)
    {
        var result = await _eslestirmeService.EslesmeKaldirAsync(puantajKayitId);
        if (!result) return NotFound();
        return Ok();
    }
}

/// <summary>Manuel eşleştirme request.</summary>
public class ManuelEslesmeRequest
{
    public int PuantajKayitId { get; set; }
    public int FaturaId { get; set; }
}
