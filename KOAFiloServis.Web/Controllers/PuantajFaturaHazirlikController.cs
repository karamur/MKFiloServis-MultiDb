using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Puantaj → Fatura Hazırlık CRUD API.
/// PuantajKayit (B1) verisinden fatura hazırlık kayıtları üretir, yönetir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PuantajFaturaHazirlikController : ControllerBase
{
    private readonly IPuantajFaturaHazirlikService _hazirlikService;
    private readonly IFirmaService _firmaService;

    public PuantajFaturaHazirlikController(IPuantajFaturaHazirlikService hazirlikService, IFirmaService firmaService)
    {
        _hazirlikService = hazirlikService;
        _firmaService = firmaService;
    }

    /// <summary>GET /api/puantaj-fatura-hazirlik?yil=2026&ay=6</summary>
    [HttpGet]
    public async Task<IActionResult> GetByDonem([FromQuery] int yil, [FromQuery] int ay, [FromQuery] int? kurumId = null)
    {
        var list = await _hazirlikService.GetByDonemAsync(yil, ay, kurumId);
        return Ok(list);
    }

    /// <summary>GET /api/puantaj-fatura-hazirlik/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var hazirlik = await _hazirlikService.GetByIdAsync(id);
        if (hazirlik == null) return NotFound();
        return Ok(hazirlik);
    }

    /// <summary>POST /api/puantaj-fatura-hazirlik — PuantajKayit'tan yeni hazırlık oluştur</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PuantajFaturaHazirlikRequest request)
    {
        var hazirlik = new PuantajFaturaHazirlik
        {
            FirmaId = _firmaService.GetAktifFirma().FirmaId,
            Yil = request.Yil,
            Ay = request.Ay,
            KurumId = request.KurumId,
            AgacYapisi = request.AgacYapisi,
            FaturaYonu = request.FaturaYonu,
            Aciklama = request.Aciklama,
            CreatedBy = User.Identity?.Name,
        };
        var created = await _hazirlikService.CreateAsync(hazirlik);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>DELETE /api/puantaj-fatura-hazirlik/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _hazirlikService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    /// <summary>GET /api/puantaj-fatura-hazirlik/{id}/satirlar</summary>
    [HttpGet("{id:int}/satirlar")]
    public async Task<IActionResult> GetSatirlar(int id)
    {
        var satirlar = await _hazirlikService.GetSatirlarAsync(id);
        return Ok(satirlar);
    }

    /// <summary>POST /api/puantaj-fatura-hazirlik/{id}/satirlar — manuel satır ekle</summary>
    [HttpPost("{id:int}/satirlar")]
    public async Task<IActionResult> ManuelSatirEkle(int id, [FromBody] PuantajFaturaHazirlikSatir satir)
    {
        var created = await _hazirlikService.ManuelSatirEkleAsync(id, satir);
        return Ok(created);
    }

    /// <summary>PUT /api/puantaj-fatura-hazirlik/{id}/satirlar/{satirId}</summary>
    [HttpPut("{id:int}/satirlar/{satirId:int}")]
    public async Task<IActionResult> SatirGuncelle(int id, int satirId, [FromBody] PuantajFaturaHazirlikSatir satir)
    {
        satir.Id = satirId;
        satir.HazirlikId = id;
        var result = await _hazirlikService.SatirGuncelleAsync(satir);
        return result ? Ok() : NotFound();
    }

    /// <summary>DELETE /api/puantaj-fatura-hazirlik/{id}/satirlar/{satirId}</summary>
    [HttpDelete("{id:int}/satirlar/{satirId:int}")]
    public async Task<IActionResult> SatirSil(int id, int satirId)
    {
        var deleted = await _hazirlikService.SatirSilAsync(satirId);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>POST /api/puantaj-fatura-hazirlik/{id}/onayla</summary>
    [HttpPost("{id:int}/onayla")]
    public async Task<IActionResult> Onayla(int id)
    {
        var kullanici = User.Identity?.Name ?? "Sistem";
        var result = await _hazirlikService.OnaylaAsync(id, kullanici);
        return Ok(result);
    }

    /// <summary>POST /api/puantaj-fatura-hazirlik/{id}/faturalasti</summary>
    [HttpPost("{id:int}/faturalasti")]
    public async Task<IActionResult> Faturalasti(int id, [FromQuery] int? faturaId = null)
    {
        var result = await _hazirlikService.FaturalastiAsync(id, faturaId);
        return Ok(result);
    }
}

/// <summary>Hazırlık oluşturma request modeli.</summary>
public class PuantajFaturaHazirlikRequest
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public int? KurumId { get; set; }
    public PuantajFaturaAgacYapisi AgacYapisi { get; set; } = PuantajFaturaAgacYapisi.CariAracGuzergah;
    public PuantajFaturaYonu FaturaYonu { get; set; } = PuantajFaturaYonu.Gelir;
    public string? Aciklama { get; set; }
}
