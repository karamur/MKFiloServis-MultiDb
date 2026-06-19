using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Fatura hazırlık raporu ağaç gruplama şablonu CRUD API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FaturaGrupSablonuController : ControllerBase
{
    private readonly IFaturaGrupSablonuService _sablonService;
    private readonly IFirmaService _firmaService;

    public FaturaGrupSablonuController(IFaturaGrupSablonuService sablonService, IFirmaService firmaService)
    {
        _sablonService = sablonService;
        _firmaService = firmaService;
    }

    private int AktifFirmaId => _firmaService.GetAktifFirma().FirmaId;

    /// <summary>GET /api/fatura-grup-sablonu — tüm şablonlar</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? kullaniciId = null)
    {
        var sablonlar = await _sablonService.GetByFirmaAsync(AktifFirmaId, kullaniciId);
        return Ok(sablonlar);
    }

    /// <summary>GET /api/fatura-grup-sablonu/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sablon = await _sablonService.GetByIdAsync(id);
        if (sablon == null) return NotFound();
        return Ok(sablon);
    }

    /// <summary>GET /api/fatura-grup-sablonu/varsayilan — kullanıcının varsayılan şablonu</summary>
    [HttpGet("varsayilan")]
    public async Task<IActionResult> GetVarsayilan([FromQuery] int? kullaniciId = null)
    {
        var sablon = await _sablonService.GetVarsayilanAsync(AktifFirmaId, kullaniciId);
        if (sablon == null) return NoContent();
        return Ok(sablon);
    }

    /// <summary>POST /api/fatura-grup-sablonu</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FaturaGrupSablonuRequest request)
    {
        var sablon = new FaturaGrupSablonu
        {
            FirmaId = AktifFirmaId,
            Ad = request.Ad,
            AgacYapisi = request.AgacYapisi,
            VarsayilanMi = request.VarsayilanMi,
            KullaniciId = request.KullaniciId,
        };
        var created = await _sablonService.CreateAsync(sablon);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/fatura-grup-sablonu/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] FaturaGrupSablonuRequest request)
    {
        var existing = await _sablonService.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Ad = request.Ad;
        existing.AgacYapisi = request.AgacYapisi;
        existing.VarsayilanMi = request.VarsayilanMi;

        var updated = await _sablonService.UpdateAsync(existing);
        return Ok(updated);
    }

    /// <summary>DELETE /api/fatura-grup-sablonu/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _sablonService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    /// <summary>POST /api/fatura-grup-sablonu/{id}/varsayilan-yap</summary>
    [HttpPost("{id:int}/varsayilan-yap")]
    public async Task<IActionResult> SetVarsayilan(int id)
    {
        var result = await _sablonService.SetVarsayilanAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}

/// <summary>Şablon CRUD request modeli.</summary>
public class FaturaGrupSablonuRequest
{
    public string Ad { get; set; } = null!;
    public PuantajFaturaAgacYapisi AgacYapisi { get; set; } = PuantajFaturaAgacYapisi.CariAracGuzergah;
    public bool VarsayilanMi { get; set; }
    public int? KullaniciId { get; set; }
}
