using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Fatura hazırlık raporu ağaç gruplama şablonu CRUD API.
/// </summary>
[Authorize(Policy = "Licensed:fatura")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
[TypeFilter(typeof(FaturaGrupSablonuAccessExceptionFilter))]
public class FaturaGrupSablonuController : ControllerBase
{
    private readonly IFaturaGrupSablonuService _sablonService;
    private readonly IFirmaService _firmaService;
    private readonly CurrentPermissionGuard _permissionGuard;

    public FaturaGrupSablonuController(
        IFaturaGrupSablonuService sablonService,
        IFirmaService firmaService,
        CurrentPermissionGuard permissionGuard)
    {
        _sablonService = sablonService;
        _firmaService = firmaService;
        _permissionGuard = permissionGuard;
    }

    private int AktifFirmaId => _firmaService.GetAktifFirma().FirmaId;

    /// <summary>GET /api/fatura-grup-sablonu — tüm şablonlar</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery, Range(1, int.MaxValue)] int? kullaniciId = null)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.FaturaHazirlikOku)) return Forbid();
        var sablonlar = await _sablonService.GetByFirmaAsync(AktifFirmaId, kullaniciId, HttpContext.RequestAborted);
        return Ok(sablonlar);
    }

    /// <summary>GET /api/fatura-grup-sablonu/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([Range(1, int.MaxValue)] int id)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.FaturaHazirlikOku)) return Forbid();
        var sablon = await _sablonService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (sablon == null) return NotFound();
        return Ok(sablon);
    }

    /// <summary>GET /api/fatura-grup-sablonu/varsayilan — kullanıcının varsayılan şablonu</summary>
    [HttpGet("varsayilan")]
    public async Task<IActionResult> GetVarsayilan([FromQuery, Range(1, int.MaxValue)] int? kullaniciId = null)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.FaturaHazirlikOku)) return Forbid();
        var sablon = await _sablonService.GetVarsayilanAsync(AktifFirmaId, kullaniciId, HttpContext.RequestAborted);
        if (sablon == null) return NoContent();
        return Ok(sablon);
    }

    /// <summary>POST /api/fatura-grup-sablonu</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FaturaGrupSablonuRequest request)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.FaturaHazirlikYaz)) return Forbid();
        var sablon = new FaturaGrupSablonu
        {
            FirmaId = AktifFirmaId,
            Ad = request.Ad,
            AgacYapisi = request.AgacYapisi,
            VarsayilanMi = request.VarsayilanMi,
            KullaniciId = request.KullaniciId,
        };
        var created = await _sablonService.CreateAsync(sablon, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/fatura-grup-sablonu/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([Range(1, int.MaxValue)] int id, [FromBody] FaturaGrupSablonuRequest request)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.FaturaHazirlikDuzenle)) return Forbid();
        var existing = await _sablonService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (existing == null) return NotFound();

        existing.Ad = request.Ad;
        existing.AgacYapisi = request.AgacYapisi;
        existing.VarsayilanMi = request.VarsayilanMi;

        var updated = await _sablonService.UpdateAsync(existing, HttpContext.RequestAborted);
        return Ok(updated);
    }

    /// <summary>DELETE /api/fatura-grup-sablonu/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int id)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.FaturaHazirlikDuzenle)) return Forbid();
        var deleted = await _sablonService.DeleteAsync(id, HttpContext.RequestAborted);
        if (!deleted) return NotFound();
        return NoContent();
    }

    /// <summary>POST /api/fatura-grup-sablonu/{id}/varsayilan-yap</summary>
    [HttpPost("{id:int}/varsayilan-yap")]
    public async Task<IActionResult> SetVarsayilan([Range(1, int.MaxValue)] int id)
    {
        if (!await _permissionGuard.HasAnyAsync(Yetkiler.FaturaHazirlikDuzenle)) return Forbid();
        var result = await _sablonService.SetVarsayilanAsync(id, HttpContext.RequestAborted);
        if (!result) return NotFound();
        return Ok();
    }
}

/// <summary>Şablon CRUD request modeli.</summary>
public class FaturaGrupSablonuRequest
{
    [Required, StringLength(150)]
    public string Ad { get; set; } = null!;
    [EnumDataType(typeof(PuantajFaturaAgacYapisi))]
    public PuantajFaturaAgacYapisi AgacYapisi { get; set; } = PuantajFaturaAgacYapisi.CariAracGuzergah;
    public bool VarsayilanMi { get; set; }
    [Range(1, int.MaxValue)]
    public int? KullaniciId { get; set; }
}

/// <summary>Beklenen giriş/kapsam ve erişim hatalarını API yanıtına dönüştürür.</summary>
public sealed class FaturaGrupSablonuAccessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var problem = context.Exception switch
        {
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Şablon erişimi reddedildi",
                Detail = "Oturumunuzu ve firma/kullanıcı kapsamını kontrol edin."
            },
            FaturaGrupSablonuException { Hata: FaturaGrupSablonuHata.GecersizIstek } hata => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Geçersiz şablon isteği",
                Detail = hata.Message
            },
            FaturaGrupSablonuException { Hata: FaturaGrupSablonuHata.Bulunamadi } hata => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Şablon bulunamadı",
                Detail = hata.Message
            },
            _ => null
        };
        if (problem is null) return;
        problem.Instance = context.HttpContext.Request.Path;
        context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
        context.ExceptionHandled = true;
    }
}



