using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Puantaj istisna yönetimi CRUD API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PuantajIstisnaController : ControllerBase
{
    private readonly IPuantajIstisnaService _istisnaService;

    public PuantajIstisnaController(IPuantajIstisnaService istisnaService)
    {
        _istisnaService = istisnaService;
    }

    /// <summary>GET /api/puantaj-istisna/puantaj/{puantajKayitId} — puantaja bağlı istisnalar</summary>
    [HttpGet("puantaj/{puantajKayitId:int}")]
    public async Task<IActionResult> GetByPuantajKayit(int puantajKayitId)
    {
        var list = await _istisnaService.GetByPuantajKayitAsync(puantajKayitId);
        return Ok(list);
    }

    /// <summary>GET /api/puantaj-istisna/donem?yil=2026&ay=6 — dönem bazlı tüm istisnalar</summary>
    [HttpGet("donem")]
    public async Task<IActionResult> GetByDonem([FromQuery] int yil, [FromQuery] int ay, [FromQuery] int? kurumId = null)
    {
        var list = await _istisnaService.GetByDonemAsync(yil, ay, kurumId);
        return Ok(list);
    }

    /// <summary>GET /api/puantaj-istisna/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var istisna = await _istisnaService.GetByIdAsync(id);
        if (istisna == null) return NotFound();
        return Ok(istisna);
    }

    /// <summary>POST /api/puantaj-istisna</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PuantajIstisna istisna)
    {
        var created = await _istisnaService.CreateAsync(istisna);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/puantaj-istisna/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PuantajIstisna istisna)
    {
        istisna.Id = id;
        var updated = await _istisnaService.UpdateAsync(istisna);
        return Ok(updated);
    }

    /// <summary>DELETE /api/puantaj-istisna/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _istisnaService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
