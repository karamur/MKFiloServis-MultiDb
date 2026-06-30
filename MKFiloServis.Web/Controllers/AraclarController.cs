using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Araç yönetimi API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class AraclarController : ControllerBase
{
    private readonly IAracService _aracService;

    public AraclarController(IAracService aracService)
    {
        _aracService = aracService;
    }

    /// <summary>
    /// Tüm araçları listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? aktif = null, [FromQuery] string? sahiplikTipi = null)
    {
        var araclar = await _aracService.GetAllAsync();
        
        if (aktif.HasValue)
        {
            araclar = araclar.Where(a => a.Aktif == aktif.Value).ToList();
        }

        if (!string.IsNullOrEmpty(sahiplikTipi) && Enum.TryParse<AracSahiplikTipi>(sahiplikTipi, true, out var tip))
        {
            araclar = araclar.Where(a => a.SahiplikTipi == tip).ToList();
        }

        var result = araclar.Select(a => new AracDto
        {
            Id = a.Id,
            AktifPlaka = a.AktifPlaka,
            Marka = a.Marka,
            Model = a.Model,
            ModelYili = a.ModelYili,
            SaseNo = a.SaseNo,
            MotorNo = a.MotorNo,
            Renk = a.Renk,
            KoltukSayisi = a.KoltukSayisi,
            AracTipi = a.AracTipi.ToString(),
            KmDurumu = a.KmDurumu ?? 0,
            SahiplikTipi = a.SahiplikTipi.ToString(),
            TrafikSigortaBitisTarihi = a.TrafikSigortaBitisTarihi,
            KaskoBitisTarihi = a.KaskoBitisTarihi,
            MuayeneBitisTarihi = a.MuayeneBitisTarihi,
            Aktif = a.Aktif
        });

        return Ok(result);
    }

    /// <summary>
    /// Belirli bir aracı getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var arac = await _aracService.GetByIdAsync(id);
        if (arac == null)
            return NotFound(new { Error = "Araç bulunamadı" });

        return Ok(new AracDto
        {
            Id = arac.Id,
            AktifPlaka = arac.AktifPlaka,
            Marka = arac.Marka,
            Model = arac.Model,
            ModelYili = arac.ModelYili,
            SaseNo = arac.SaseNo,
            MotorNo = arac.MotorNo,
            Renk = arac.Renk,
            KoltukSayisi = arac.KoltukSayisi,
            AracTipi = arac.AracTipi.ToString(),
            KmDurumu = arac.KmDurumu ?? 0,
            SahiplikTipi = arac.SahiplikTipi.ToString(),
            TrafikSigortaBitisTarihi = arac.TrafikSigortaBitisTarihi,
            KaskoBitisTarihi = arac.KaskoBitisTarihi,
            MuayeneBitisTarihi = arac.MuayeneBitisTarihi,
            Aktif = arac.Aktif
        });
    }

    /// <summary>
    /// Plaka ile araç arar
    /// </summary>
    [HttpGet("plaka/{plaka}")]
    public async Task<IActionResult> GetByPlaka(string plaka)
    {
        var arac = await _aracService.GetByPlakaAsync(plaka);
        if (arac == null)
            return NotFound(new { Error = "Araç bulunamadı" });

        return Ok(new AracDto
        {
            Id = arac.Id,
            AktifPlaka = arac.AktifPlaka,
            Marka = arac.Marka,
            Model = arac.Model,
            ModelYili = arac.ModelYili,
            SaseNo = arac.SaseNo,
            MotorNo = arac.MotorNo,
            Renk = arac.Renk,
            KoltukSayisi = arac.KoltukSayisi,
            AracTipi = arac.AracTipi.ToString(),
            KmDurumu = arac.KmDurumu ?? 0,
            SahiplikTipi = arac.SahiplikTipi.ToString(),
            TrafikSigortaBitisTarihi = arac.TrafikSigortaBitisTarihi,
            KaskoBitisTarihi = arac.KaskoBitisTarihi,
            MuayeneBitisTarihi = arac.MuayeneBitisTarihi,
            Aktif = arac.Aktif
        });
    }

    /// <summary>
    /// Yeni araç oluşturur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AracCreateDto dto)
    {
        if (string.IsNullOrEmpty(dto.AktifPlaka) && string.IsNullOrEmpty(dto.SaseNo))
            return BadRequest(new { Error = "Plaka veya şase numarası gereklidir" });

        Enum.TryParse<AracSahiplikTipi>(dto.SahiplikTipi, true, out var sahiplikTipi);
        Enum.TryParse<AracTipi>(dto.AracTipi, true, out var aracTipi);

        var arac = new Arac
        {
            AktifPlaka = dto.AktifPlaka,
            Marka = dto.Marka,
            Model = dto.Model,
            ModelYili = dto.ModelYili,
            SaseNo = dto.SaseNo ?? Guid.NewGuid().ToString("N")[..17].ToUpper(), // Geçici şase no
            MotorNo = dto.MotorNo,
            Renk = dto.Renk,
            KoltukSayisi = dto.KoltukSayisi ?? 4,
            AracTipi = aracTipi,
            KmDurumu = dto.KmDurumu,
            SahiplikTipi = sahiplikTipi,
            TrafikSigortaBitisTarihi = dto.TrafikSigortaBitisTarihi,
            KaskoBitisTarihi = dto.KaskoBitisTarihi,
            MuayeneBitisTarihi = dto.MuayeneBitisTarihi,
            Aktif = dto.Aktif ?? true
        };

        await _aracService.CreateAsync(arac, dto.AktifPlaka ?? "", PlakaIslemTipi.Alis);

        return CreatedAtAction(nameof(GetById), new { id = arac.Id }, new AracDto
        {
            Id = arac.Id,
            AktifPlaka = arac.AktifPlaka,
            Marka = arac.Marka,
            Model = arac.Model,
            ModelYili = arac.ModelYili,
            SaseNo = arac.SaseNo,
            MotorNo = arac.MotorNo,
            Renk = arac.Renk,
            KoltukSayisi = arac.KoltukSayisi,
            AracTipi = arac.AracTipi.ToString(),
            KmDurumu = arac.KmDurumu ?? 0,
            SahiplikTipi = arac.SahiplikTipi.ToString(),
            TrafikSigortaBitisTarihi = arac.TrafikSigortaBitisTarihi,
            KaskoBitisTarihi = arac.KaskoBitisTarihi,
            MuayeneBitisTarihi = arac.MuayeneBitisTarihi,
            Aktif = arac.Aktif
        });
    }

    /// <summary>
    /// Araç bilgilerini günceller
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AracUpdateDto dto)
    {
        var arac = await _aracService.GetByIdAsync(id);
        if (arac == null)
            return NotFound(new { Error = "Araç bulunamadı" });

        if (dto.Marka != null)
            arac.Marka = dto.Marka;

        if (dto.Model != null)
            arac.Model = dto.Model;

        if (dto.ModelYili.HasValue)
            arac.ModelYili = dto.ModelYili;

        if (dto.SaseNo != null)
            arac.SaseNo = dto.SaseNo;

        if (dto.MotorNo != null)
            arac.MotorNo = dto.MotorNo;

        if (dto.Renk != null)
            arac.Renk = dto.Renk;

        if (dto.KoltukSayisi.HasValue)
            arac.KoltukSayisi = dto.KoltukSayisi.Value;

        if (!string.IsNullOrEmpty(dto.AracTipi) && Enum.TryParse<AracTipi>(dto.AracTipi, true, out var aracTipi))
            arac.AracTipi = aracTipi;

        if (dto.KmDurumu.HasValue)
            arac.KmDurumu = dto.KmDurumu;

        if (!string.IsNullOrEmpty(dto.SahiplikTipi) && Enum.TryParse<AracSahiplikTipi>(dto.SahiplikTipi, true, out var sahiplikTipi))
            arac.SahiplikTipi = sahiplikTipi;

        if (dto.TrafikSigortaBitisTarihi.HasValue)
            arac.TrafikSigortaBitisTarihi = dto.TrafikSigortaBitisTarihi;

        if (dto.KaskoBitisTarihi.HasValue)
            arac.KaskoBitisTarihi = dto.KaskoBitisTarihi;

        if (dto.MuayeneBitisTarihi.HasValue)
            arac.MuayeneBitisTarihi = dto.MuayeneBitisTarihi;

        if (dto.Aktif.HasValue)
            arac.Aktif = dto.Aktif.Value;

        await _aracService.UpdateAsync(arac);

        return Ok(new AracDto
        {
            Id = arac.Id,
            AktifPlaka = arac.AktifPlaka,
            Marka = arac.Marka,
            Model = arac.Model,
            ModelYili = arac.ModelYili,
            SaseNo = arac.SaseNo,
            MotorNo = arac.MotorNo,
            Renk = arac.Renk,
            KoltukSayisi = arac.KoltukSayisi,
            AracTipi = arac.AracTipi.ToString(),
            KmDurumu = arac.KmDurumu ?? 0,
            SahiplikTipi = arac.SahiplikTipi.ToString(),
            TrafikSigortaBitisTarihi = arac.TrafikSigortaBitisTarihi,
            KaskoBitisTarihi = arac.KaskoBitisTarihi,
            MuayeneBitisTarihi = arac.MuayeneBitisTarihi,
            Aktif = arac.Aktif
        });
    }

    /// <summary>
    /// Araç siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var arac = await _aracService.GetByIdAsync(id);
        if (arac == null)
            return NotFound(new { Error = "Araç bulunamadı" });

        await _aracService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Araç km günceller
    /// </summary>
    [HttpPatch("{id}/km")]
    public async Task<IActionResult> UpdateKm(int id, [FromBody] KmUpdateDto dto)
    {
        var arac = await _aracService.GetByIdAsync(id);
        if (arac == null)
            return NotFound(new { Error = "Araç bulunamadı" });

        if (arac.KmDurumu.HasValue && dto.KmDurumu < arac.KmDurumu)
            return BadRequest(new { Error = "Yeni km değeri mevcut değerden küçük olamaz" });

        arac.KmDurumu = dto.KmDurumu;
        await _aracService.UpdateAsync(arac);

        return Ok(new { AracId = id, KmDurumu = arac.KmDurumu });
    }

    /// <summary>
    /// Belge süresi dolan araçları listeler
    /// </summary>
    [HttpGet("belge-uyari")]
    public async Task<IActionResult> GetBelgeUyarilari([FromQuery] int gun = 30)
    {
        var simdi = DateTime.UtcNow;
        var sinirTarih = simdi.AddDays(gun);

        var araclar = await _aracService.GetAllAsync();
        var uyarilar = araclar
            .Where(a => a.Aktif)
            .SelectMany(a => new[]
            {
                new { Arac = a, BelgeTipi = "Trafik Sigortası", BitisTarihi = a.TrafikSigortaBitisTarihi },
                new { Arac = a, BelgeTipi = "Kasko", BitisTarihi = a.KaskoBitisTarihi },
                new { Arac = a, BelgeTipi = "Muayene", BitisTarihi = a.MuayeneBitisTarihi }
            })
            .Where(x => x.BitisTarihi.HasValue && x.BitisTarihi.Value <= sinirTarih)
            .Select(x => new
            {
                AracId = x.Arac.Id,
                Plaka = x.Arac.AktifPlaka,
                x.BelgeTipi,
                BitisTarihi = x.BitisTarihi!.Value,
                KalanGun = (x.BitisTarihi!.Value.Date - simdi.Date).Days,
                SuresiDoldu = x.BitisTarihi!.Value.Date < simdi.Date
            })
            .OrderBy(x => x.BitisTarihi)
            .ToList();

        return Ok(uyarilar);
    }
}

// DTO Modelleri
public class AracDto
{
    public int Id { get; set; }
    public string? AktifPlaka { get; set; }
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public int? ModelYili { get; set; }
    public string? SaseNo { get; set; }
    public string? MotorNo { get; set; }
    public string? Renk { get; set; }
    public int KoltukSayisi { get; set; }
    public string AracTipi { get; set; } = "";
    public int KmDurumu { get; set; }
    public string SahiplikTipi { get; set; } = "";
    public DateTime? TrafikSigortaBitisTarihi { get; set; }
    public DateTime? KaskoBitisTarihi { get; set; }
    public DateTime? MuayeneBitisTarihi { get; set; }
    public bool Aktif { get; set; }
}

public class AracCreateDto
{
    public string? AktifPlaka { get; set; }
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public int? ModelYili { get; set; }
    public string? SaseNo { get; set; }
    public string? MotorNo { get; set; }
    public string? Renk { get; set; }
    public int? KoltukSayisi { get; set; }
    public string? AracTipi { get; set; }
    public int? KmDurumu { get; set; }
    public string? SahiplikTipi { get; set; }
    public DateTime? TrafikSigortaBitisTarihi { get; set; }
    public DateTime? KaskoBitisTarihi { get; set; }
    public DateTime? MuayeneBitisTarihi { get; set; }
    public bool? Aktif { get; set; }
}

public class AracUpdateDto
{
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public int? ModelYili { get; set; }
    public string? SaseNo { get; set; }
    public string? MotorNo { get; set; }
    public string? Renk { get; set; }
    public int? KoltukSayisi { get; set; }
    public string? AracTipi { get; set; }
    public int? KmDurumu { get; set; }
    public string? SahiplikTipi { get; set; }
    public DateTime? TrafikSigortaBitisTarihi { get; set; }
    public DateTime? KaskoBitisTarihi { get; set; }
    public DateTime? MuayeneBitisTarihi { get; set; }
    public bool? Aktif { get; set; }
}

public class KmUpdateDto
{
    public int KmDurumu { get; set; }
}



