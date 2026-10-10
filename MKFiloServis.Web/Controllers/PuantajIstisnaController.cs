using MKFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Puantaj istisna yönetimi CRUD API.
/// </summary>
[Authorize(Policy = "Licensed:filoservis")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PuantajIstisnaController : ControllerBase
{
    private readonly IPuantajIstisnaService _istisnaService;
    private readonly CurrentPermissionGuard _permissionGuard;

    public PuantajIstisnaController(IPuantajIstisnaService istisnaService, CurrentPermissionGuard permissionGuard)
    {
        _istisnaService = istisnaService;
        _permissionGuard = permissionGuard;
    }

    /// <summary>GET /api/puantaj-istisna/puantaj/{puantajKayitId} — puantaja bağlı istisnalar</summary>
    [HttpGet("puantaj/{puantajKayitId:int}")]
    public async Task<IActionResult> GetByPuantajKayit(int puantajKayitId)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.MaasOku)) return Forbid();
        var list = await _istisnaService.GetByPuantajKayitAsync(puantajKayitId);
        return Ok(list);
    }

    /// <summary>GET /api/puantaj-istisna/donem?yil=2026&amp;ay=6 — dönem bazlı tüm istisnalar</summary>
    [HttpGet("donem")]
    public async Task<IActionResult> GetByDonem([FromQuery] int yil, [FromQuery] int ay, [FromQuery] int? kurumId = null)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.MaasOku)) return Forbid();
        var list = await _istisnaService.GetByDonemAsync(yil, ay, kurumId);
        return Ok(list);
    }

    /// <summary>GET /api/puantaj-istisna/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.MaasOku)) return Forbid();
        var istisna = await _istisnaService.GetByIdAsync(id);
        if (istisna == null) return NotFound();
        return Ok(istisna);
    }

    /// <summary>POST /api/puantaj-istisna</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PuantajIstisna istisna)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.MaasYaz)) return Forbid();
        var created = await _istisnaService.CreateAsync(istisna);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/puantaj-istisna/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PuantajIstisna istisna)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.MaasDuzenle)) return Forbid();
        istisna.Id = id;
        var updated = await _istisnaService.UpdateAsync(istisna);
        return Ok(updated);
    }

    /// <summary>DELETE /api/puantaj-istisna/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.MaasSil)) return Forbid();
        var deleted = await _istisnaService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}



