using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Cari (Müşteri/Tedarikçi) yönetimi API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class CarilerController : ControllerBase
{
    private readonly ICariService _cariService;

    public CarilerController(ICariService cariService)
    {
        _cariService = cariService;
    }

    /// <summary>
    /// Tüm carileri listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? tip = null, [FromQuery] bool? aktif = null)
    {
        var cariler = await _cariService.GetAllAsync();
        
        if (!string.IsNullOrEmpty(tip) && Enum.TryParse<CariTipi>(tip, true, out var cariTipi))
        {
            cariler = cariler.Where(c => c.CariTipi == cariTipi).ToList();
        }
        
        if (aktif.HasValue)
        {
            cariler = cariler.Where(c => c.Aktif == aktif.Value).ToList();
        }

        var result = cariler.Select(c => new CariDto
        {
            Id = c.Id,
            CariKodu = c.CariKodu,
            Unvan = c.Unvan,
            CariTipi = c.CariTipi.ToString(),
            VergiDairesi = c.VergiDairesi,
            VergiNo = c.VergiNo,
            Telefon = c.Telefon,
            Email = c.Email,
            Adres = c.Adres,
            Il = c.Il,
            Ilce = c.Ilce,
            Aktif = c.Aktif
        });

        return Ok(result);
    }

    /// <summary>
    /// Belirli bir cariyi getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cari = await _cariService.GetByIdAsync(id);
        if (cari == null)
            return NotFound(new { Error = "Cari bulunamadı" });

        return Ok(new CariDto
        {
            Id = cari.Id,
            CariKodu = cari.CariKodu,
            Unvan = cari.Unvan,
            CariTipi = cari.CariTipi.ToString(),
            VergiDairesi = cari.VergiDairesi,
            VergiNo = cari.VergiNo,
            Telefon = cari.Telefon,
            Email = cari.Email,
            Adres = cari.Adres,
            Il = cari.Il,
            Ilce = cari.Ilce,
            Aktif = cari.Aktif
        });
    }

    /// <summary>
    /// Yeni cari oluşturur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CariCreateDto dto)
    {
        if (string.IsNullOrEmpty(dto.Unvan))
            return BadRequest(new { Error = "Ünvan gereklidir" });

        if (!Enum.TryParse<CariTipi>(dto.CariTipi, true, out var cariTipi))
            cariTipi = CariTipi.Musteri;

        var cari = new Cari
        {
            CariKodu = dto.CariKodu ?? "",
            Unvan = dto.Unvan,
            CariTipi = cariTipi,
            VergiDairesi = dto.VergiDairesi,
            VergiNo = dto.VergiNo,
            Telefon = dto.Telefon,
            Email = dto.Email,
            Adres = dto.Adres,
            Il = dto.Il,
            Ilce = dto.Ilce,
            Aktif = dto.Aktif ?? true
        };

        await _cariService.CreateAsync(cari);

        return CreatedAtAction(nameof(GetById), new { id = cari.Id }, new CariDto
        {
            Id = cari.Id,
            CariKodu = cari.CariKodu,
            Unvan = cari.Unvan,
            CariTipi = cari.CariTipi.ToString(),
            VergiDairesi = cari.VergiDairesi,
            VergiNo = cari.VergiNo,
            Telefon = cari.Telefon,
            Email = cari.Email,
            Adres = cari.Adres,
            Il = cari.Il,
            Ilce = cari.Ilce,
            Aktif = cari.Aktif
        });
    }

    /// <summary>
    /// Cari bilgilerini günceller
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CariUpdateDto dto)
    {
        var cari = await _cariService.GetByIdAsync(id);
        if (cari == null)
            return NotFound(new { Error = "Cari bulunamadı" });

        if (!string.IsNullOrEmpty(dto.Unvan))
            cari.Unvan = dto.Unvan;

        if (!string.IsNullOrEmpty(dto.CariKodu))
            cari.CariKodu = dto.CariKodu;

        if (!string.IsNullOrEmpty(dto.CariTipi) && Enum.TryParse<CariTipi>(dto.CariTipi, true, out var cariTipi))
            cari.CariTipi = cariTipi;

        if (dto.VergiDairesi != null)
            cari.VergiDairesi = dto.VergiDairesi;

        if (dto.VergiNo != null)
            cari.VergiNo = dto.VergiNo;

        if (dto.Telefon != null)
            cari.Telefon = dto.Telefon;

        if (dto.Email != null)
            cari.Email = dto.Email;

        if (dto.Adres != null)
            cari.Adres = dto.Adres;

        if (dto.Il != null)
            cari.Il = dto.Il;

        if (dto.Ilce != null)
            cari.Ilce = dto.Ilce;

        if (dto.Aktif.HasValue)
            cari.Aktif = dto.Aktif.Value;

        await _cariService.UpdateAsync(cari);

        return Ok(new CariDto
        {
            Id = cari.Id,
            CariKodu = cari.CariKodu,
            Unvan = cari.Unvan,
            CariTipi = cari.CariTipi.ToString(),
            VergiDairesi = cari.VergiDairesi,
            VergiNo = cari.VergiNo,
            Telefon = cari.Telefon,
            Email = cari.Email,
            Adres = cari.Adres,
            Il = cari.Il,
            Ilce = cari.Ilce,
            Aktif = cari.Aktif
        });
    }

    /// <summary>
    /// Cari siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cari = await _cariService.GetByIdAsync(id);
        if (cari == null)
            return NotFound(new { Error = "Cari bulunamadı" });

        await _cariService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Cari bakiyesini getirir
    /// </summary>
    [HttpGet("{id}/bakiye")]
    public async Task<IActionResult> GetBakiye(int id)
    {
        // GetAllWithBakiyeAsync ile bakiye hesaplanmış cariyi getir
        var cariler = await _cariService.GetAllWithBakiyeAsync();
        var cari = cariler.FirstOrDefault(c => c.Id == id);

        if (cari == null)
            return NotFound(new { Error = "Cari bulunamadı" });

        return Ok(new
        {
            CariId = id,
            Unvan = cari.Unvan,
            Borc = cari.Borc,
            Alacak = cari.Alacak,
            Bakiye = cari.Alacak - cari.Borc
        });
    }
}

// DTO Modelleri
public class CariDto
{
    public int Id { get; set; }
    public string? CariKodu { get; set; }
    public string Unvan { get; set; } = "";
    public string CariTipi { get; set; } = "";
    public string? VergiDairesi { get; set; }
    public string? VergiNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }
    public string? Il { get; set; }
    public string? Ilce { get; set; }
    public bool Aktif { get; set; }
}

public class CariCreateDto
{
    public string? CariKodu { get; set; }
    public string Unvan { get; set; } = "";
    public string? CariTipi { get; set; }
    public string? VergiDairesi { get; set; }
    public string? VergiNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }
    public string? Il { get; set; }
    public string? Ilce { get; set; }
    public bool? Aktif { get; set; }
}

public class CariUpdateDto
{
    public string? CariKodu { get; set; }
    public string? Unvan { get; set; }
    public string? CariTipi { get; set; }
    public string? VergiDairesi { get; set; }
    public string? VergiNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }
    public string? Il { get; set; }
    public string? Ilce { get; set; }
    public bool? Aktif { get; set; }
}



