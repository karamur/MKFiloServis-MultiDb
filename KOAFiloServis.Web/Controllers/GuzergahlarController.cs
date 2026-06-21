using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Güzergah yönetimi API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GuzergahlarController : ControllerBase
{
    private readonly IGuzergahService _guzergahService;

    public GuzergahlarController(IGuzergahService guzergahService)
    {
        _guzergahService = guzergahService;
    }

    /// <summary>
    /// Tüm güzergahları listeler
    /// </summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetAll([FromQuery] bool? aktif = null)
    {
        var guzergahlar = await _guzergahService.GetAllAsync();
        
        if (aktif.HasValue)
        {
            guzergahlar = guzergahlar.Where(g => g.Aktif == aktif.Value).ToList();
        }

        var result = guzergahlar.Select(g => new GuzergahDto
        {
            Id = g.Id,
            GuzergahKodu = g.GuzergahKodu,
            GuzergahAdi = g.GuzergahAdi,
            BaslangicNoktasi = g.BaslangicNoktasi,
            BitisNoktasi = g.BitisNoktasi,
            Mesafe = g.Mesafe,
            TahminiSure = g.TahminiSure,
            BirimFiyat = g.BirimFiyat,
            Aktif = g.Aktif,
            SeferTipi = g.SeferTipi,
            PersonelSayisi = g.PersonelSayisi,
            VarsayilanAracId = g.VarsayilanAracId,
            VarsayilanSoforId = g.VarsayilanSoforId,
            FirmaId = g.FirmaId,
            CariId = g.CariId,
            PuantajCarpani = g.PuantajCarpani,
            Notlar = g.Notlar
        });

        return Ok(result);
    }

    /// <summary>
    /// Belirli bir güzergahı getirir
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetById(int id)
    {
        var guzergah = await _guzergahService.GetByIdAsync(id);
        if (guzergah == null)
            return NotFound(new { Error = "Güzergah bulunamadı" });

        return Ok(new GuzergahDto
        {
            Id = guzergah.Id,
            GuzergahKodu = guzergah.GuzergahKodu,
            GuzergahAdi = guzergah.GuzergahAdi,
            BaslangicNoktasi = guzergah.BaslangicNoktasi,
            BitisNoktasi = guzergah.BitisNoktasi,
            Mesafe = guzergah.Mesafe,
            TahminiSure = guzergah.TahminiSure,
            BirimFiyat = guzergah.BirimFiyat,
            Aktif = guzergah.Aktif,
            SeferTipi = guzergah.SeferTipi,
            PersonelSayisi = guzergah.PersonelSayisi,
            VarsayilanAracId = guzergah.VarsayilanAracId,
            VarsayilanSoforId = guzergah.VarsayilanSoforId,
            FirmaId = guzergah.FirmaId,
            CariId = guzergah.CariId,
            PuantajCarpani = guzergah.PuantajCarpani,
            Notlar = guzergah.Notlar
        });
    }

    /// <summary>
    /// Yeni güzergah oluşturur
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Create([FromBody] GuzergahCreateDto dto)
    {
        if (string.IsNullOrEmpty(dto.GuzergahAdi))
            return BadRequest(new { Error = "Güzergah adı gereklidir" });

        var guzergah = new Guzergah
        {
            GuzergahKodu = dto.GuzergahKodu ?? "",
            GuzergahAdi = dto.GuzergahAdi,
            BaslangicNoktasi = dto.BaslangicNoktasi,
            BitisNoktasi = dto.BitisNoktasi,
            Mesafe = dto.Mesafe,
            TahminiSure = dto.TahminiSure,
            BirimFiyat = dto.BirimFiyat ?? 0,
            Aktif = dto.Aktif ?? true,
            SeferTipi = dto.SeferTipi,
            PersonelSayisi = dto.PersonelSayisi,
            VarsayilanAracId = dto.VarsayilanAracId,
            VarsayilanSoforId = dto.VarsayilanSoforId,
            FirmaId = dto.FirmaId,
            CariId = dto.CariId,
            PuantajCarpani = dto.PuantajCarpani,
            Notlar = dto.Notlar
        };

        await _guzergahService.CreateAsync(guzergah);

        return CreatedAtAction(nameof(GetById), new { id = guzergah.Id }, new GuzergahDto
        {
            Id = guzergah.Id,
            GuzergahKodu = guzergah.GuzergahKodu,
            GuzergahAdi = guzergah.GuzergahAdi,
            BaslangicNoktasi = guzergah.BaslangicNoktasi,
            BitisNoktasi = guzergah.BitisNoktasi,
            Mesafe = guzergah.Mesafe,
            TahminiSure = guzergah.TahminiSure,
            BirimFiyat = guzergah.BirimFiyat,
            Aktif = guzergah.Aktif,
            SeferTipi = guzergah.SeferTipi,
            PersonelSayisi = guzergah.PersonelSayisi,
            VarsayilanAracId = guzergah.VarsayilanAracId,
            VarsayilanSoforId = guzergah.VarsayilanSoforId,
            FirmaId = guzergah.FirmaId,
            CariId = guzergah.CariId,
            PuantajCarpani = guzergah.PuantajCarpani,
            Notlar = guzergah.Notlar
        });
    }

    /// <summary>
    /// Güzergah bilgilerini günceller
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Update(int id, [FromBody] GuzergahUpdateDto dto)
    {
        var guzergah = await _guzergahService.GetByIdAsync(id);
        if (guzergah == null)
            return NotFound(new { Error = "Güzergah bulunamadı" });

        if (!string.IsNullOrEmpty(dto.GuzergahKodu))
            guzergah.GuzergahKodu = dto.GuzergahKodu;

        if (!string.IsNullOrEmpty(dto.GuzergahAdi))
            guzergah.GuzergahAdi = dto.GuzergahAdi;

        if (dto.BaslangicNoktasi != null)
            guzergah.BaslangicNoktasi = dto.BaslangicNoktasi;

        if (dto.BitisNoktasi != null)
            guzergah.BitisNoktasi = dto.BitisNoktasi;

        if (dto.Mesafe.HasValue)
            guzergah.Mesafe = dto.Mesafe;

        if (dto.TahminiSure.HasValue)
            guzergah.TahminiSure = dto.TahminiSure;

        if (dto.BirimFiyat.HasValue)
            guzergah.BirimFiyat = dto.BirimFiyat.Value;

        if (dto.Aktif.HasValue)
            guzergah.Aktif = dto.Aktif.Value;

        if (dto.SeferTipi.HasValue)
            guzergah.SeferTipi = dto.SeferTipi.Value;

        if (dto.PersonelSayisi.HasValue)
            guzergah.PersonelSayisi = dto.PersonelSayisi.Value;

        if (dto.VarsayilanAracId.HasValue)
            guzergah.VarsayilanAracId = dto.VarsayilanAracId.Value;

        if (dto.VarsayilanSoforId.HasValue)
            guzergah.VarsayilanSoforId = dto.VarsayilanSoforId.Value;

        if (dto.FirmaId.HasValue)
            guzergah.FirmaId = dto.FirmaId.Value;

        if (dto.CariId.HasValue)
            guzergah.CariId = dto.CariId.Value;

        if (dto.Notlar != null)
            guzergah.Notlar = dto.Notlar;

        if (dto.PuantajCarpani.HasValue)
            guzergah.PuantajCarpani = dto.PuantajCarpani.Value;

        await _guzergahService.UpdateAsync(guzergah);

        return Ok(new GuzergahDto
        {
            Id = guzergah.Id,
            GuzergahKodu = guzergah.GuzergahKodu,
            GuzergahAdi = guzergah.GuzergahAdi,
            BaslangicNoktasi = guzergah.BaslangicNoktasi,
            BitisNoktasi = guzergah.BitisNoktasi,
            Mesafe = guzergah.Mesafe,
            TahminiSure = guzergah.TahminiSure,
            BirimFiyat = guzergah.BirimFiyat,
            Aktif = guzergah.Aktif,
            SeferTipi = guzergah.SeferTipi,
            PersonelSayisi = guzergah.PersonelSayisi,
            VarsayilanAracId = guzergah.VarsayilanAracId,
            VarsayilanSoforId = guzergah.VarsayilanSoforId,
            FirmaId = guzergah.FirmaId,
            CariId = guzergah.CariId,
            PuantajCarpani = guzergah.PuantajCarpani,
            Notlar = guzergah.Notlar
        });
    }

    /// <summary>
    /// Excel'den toplu güzergah import eder
    /// </summary>
    [HttpPost("import-excel")]
    [AllowAnonymous]
    public async Task<IActionResult> ImportExcel(IFormFile file, [FromQuery] int firmaId = 1)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Error = "Excel dosyası gereklidir." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
            return BadRequest(new { Error = "Sadece .xlsx veya .xls dosyaları kabul edilir." });

        using var stream = file.OpenReadStream();
        var sonuc = await _guzergahService.ImportFromExcelAsync(stream, firmaId);
        return Ok(sonuc);
    }

    /// <summary>
    /// Güzergah siler
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Delete(int id)
    {
        var guzergah = await _guzergahService.GetByIdAsync(id);
        if (guzergah == null)
            return NotFound(new { Error = "Güzergah bulunamadı" });

        await _guzergahService.DeleteAsync(id);
        return NoContent();
    }
}

// DTO Modelleri
public class GuzergahDto
{
    public int Id { get; set; }
    public string? GuzergahKodu { get; set; }
    public string GuzergahAdi { get; set; } = "";
    public string? BaslangicNoktasi { get; set; }
    public string? BitisNoktasi { get; set; }
    public decimal? Mesafe { get; set; }
    public int? TahminiSure { get; set; }
    public decimal? BirimFiyat { get; set; }
    public bool Aktif { get; set; }
    public SeferTipi SeferTipi { get; set; }
    public int PersonelSayisi { get; set; }
    public int? VarsayilanAracId { get; set; }
    public int? VarsayilanSoforId { get; set; }
    public int? FirmaId { get; set; }
    public int CariId { get; set; }
    public decimal PuantajCarpani { get; set; }
    public string? Notlar { get; set; }
}

public class GuzergahCreateDto
{
    public string? GuzergahKodu { get; set; }
    public string GuzergahAdi { get; set; } = "";
    public string? BaslangicNoktasi { get; set; }
    public string? BitisNoktasi { get; set; }
    public decimal? Mesafe { get; set; }
    public int? TahminiSure { get; set; }
    public decimal? BirimFiyat { get; set; }
    public bool? Aktif { get; set; }
    public SeferTipi SeferTipi { get; set; } = SeferTipi.SabahAksam;
    public int PersonelSayisi { get; set; } = 0;
    public int? VarsayilanAracId { get; set; }
    public int? VarsayilanSoforId { get; set; }
    public int? FirmaId { get; set; }
    public decimal PuantajCarpani { get; set; } = 1.0m;
    public int CariId { get; set; }
    public string? Notlar { get; set; }
}

public class GuzergahUpdateDto
{
    public string? GuzergahKodu { get; set; }
    public string? GuzergahAdi { get; set; }
    public string? BaslangicNoktasi { get; set; }
    public string? BitisNoktasi { get; set; }
    public decimal? Mesafe { get; set; }
    public int? TahminiSure { get; set; }
    public decimal? BirimFiyat { get; set; }
    public bool? Aktif { get; set; }
    public SeferTipi? SeferTipi { get; set; }
    public int? PersonelSayisi { get; set; }
    public int? VarsayilanAracId { get; set; }
    public int? VarsayilanSoforId { get; set; }
    public int? FirmaId { get; set; }
    public int? CariId { get; set; }
    public decimal? PuantajCarpani { get; set; }
    public string? Notlar { get; set; }
}
