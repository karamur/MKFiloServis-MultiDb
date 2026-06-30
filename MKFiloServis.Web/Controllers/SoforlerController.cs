using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Şoför/Personel yönetimi API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class SoforlerController : ControllerBase
{
    private readonly ISoforService _soforService;

    public SoforlerController(ISoforService soforService)
    {
        _soforService = soforService;
    }

    /// <summary>
    /// Tüm şoförleri/personeli listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? aktif = null, [FromQuery] string? gorev = null)
    {
        var soforler = await _soforService.GetAllAsync();

        if (aktif.HasValue)
        {
            soforler = soforler.Where(s => s.Aktif == aktif.Value).ToList();
        }

        if (!string.IsNullOrEmpty(gorev) && Enum.TryParse<PersonelGorev>(gorev, true, out var personelGorev))
        {
            soforler = soforler.Where(s => s.Gorev == personelGorev).ToList();
        }

        var result = soforler.Select(s => new SoforDto
        {
            Id = s.Id,
            SoforKodu = s.SoforKodu,
            Ad = s.Ad,
            Soyad = s.Soyad,
            TcKimlikNo = s.TcKimlikNo,
            Telefon = s.Telefon,
            Email = s.Email,
            Adres = s.Adres,
            Gorev = s.Gorev.ToString(),
            Departman = s.Departman,
            Pozisyon = s.Pozisyon,
            EhliyetNo = s.EhliyetNo,
            EhliyetGecerlilikTarihi = s.EhliyetGecerlilikTarihi,
            MykBelgesiGecerlilikTarihi = s.MykBelgesiGecerlilikTarihi,
            SrcBelgesiVarMi = s.YayginEgitimSertifikasiVarMi || s.SrcBelgesiGecerlilikTarihi.HasValue,
            PsikoteknikGecerlilikTarihi = s.PsikoteknikGecerlilikTarihi,
            SaglikRaporuGecerlilikTarihi = s.SaglikRaporuGecerlilikTarihi,
            IseBaslamaTarihi = s.IseBaslamaTarihi,
            Aktif = s.Aktif
        });

        return Ok(result);
    }

    /// <summary>
    /// Belirli bir şoförü getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sofor = await _soforService.GetByIdAsync(id);
        if (sofor == null)
            return NotFound(new { Error = "Şoför bulunamadı" });

        return Ok(new SoforDto
        {
            Id = sofor.Id,
            SoforKodu = sofor.SoforKodu,
            Ad = sofor.Ad,
            Soyad = sofor.Soyad,
            TcKimlikNo = sofor.TcKimlikNo,
            Telefon = sofor.Telefon,
            Email = sofor.Email,
            Adres = sofor.Adres,
            Gorev = sofor.Gorev.ToString(),
            Departman = sofor.Departman,
            Pozisyon = sofor.Pozisyon,
            EhliyetNo = sofor.EhliyetNo,
            EhliyetGecerlilikTarihi = sofor.EhliyetGecerlilikTarihi,
            MykBelgesiGecerlilikTarihi = sofor.MykBelgesiGecerlilikTarihi,
            SrcBelgesiVarMi = sofor.YayginEgitimSertifikasiVarMi || sofor.SrcBelgesiGecerlilikTarihi.HasValue,
            PsikoteknikGecerlilikTarihi = sofor.PsikoteknikGecerlilikTarihi,
            SaglikRaporuGecerlilikTarihi = sofor.SaglikRaporuGecerlilikTarihi,
            IseBaslamaTarihi = sofor.IseBaslamaTarihi,
            Aktif = sofor.Aktif
        });
    }

    /// <summary>
    /// Yeni şoför oluşturur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SoforCreateDto dto)
    {
        if (string.IsNullOrEmpty(dto.Ad) || string.IsNullOrEmpty(dto.Soyad))
            return BadRequest(new { Error = "Ad ve soyad gereklidir" });

        Enum.TryParse<PersonelGorev>(dto.Gorev, true, out var gorev);

        var sofor = new Sofor
        {
            SoforKodu = dto.SoforKodu ?? "",
            Ad = dto.Ad,
            Soyad = dto.Soyad,
            TcKimlikNo = dto.TcKimlikNo,
            Telefon = dto.Telefon,
            Email = dto.Email,
            Adres = dto.Adres,
            Gorev = gorev,
            Departman = dto.Departman,
            Pozisyon = dto.Pozisyon,
            EhliyetNo = dto.EhliyetNo,
            EhliyetGecerlilikTarihi = dto.EhliyetGecerlilikTarihi,
            MykBelgesiGecerlilikTarihi = dto.MykBelgesiGecerlilikTarihi,
            SrcBelgesiGecerlilikTarihi = null,
            YayginEgitimSertifikasiVarMi = dto.SrcBelgesiVarMi,
            PsikoteknikGecerlilikTarihi = dto.PsikoteknikGecerlilikTarihi,
            SaglikRaporuGecerlilikTarihi = dto.SaglikRaporuGecerlilikTarihi,
            IseBaslamaTarihi = dto.IseBaslamaTarihi,
            Aktif = dto.Aktif ?? true
        };

        await _soforService.CreateAsync(sofor);

        return CreatedAtAction(nameof(GetById), new { id = sofor.Id }, new SoforDto
        {
            Id = sofor.Id,
            SoforKodu = sofor.SoforKodu,
            Ad = sofor.Ad,
            Soyad = sofor.Soyad,
            TcKimlikNo = sofor.TcKimlikNo,
            Telefon = sofor.Telefon,
            Email = sofor.Email,
            Adres = sofor.Adres,
            Gorev = sofor.Gorev.ToString(),
            Departman = sofor.Departman,
            Pozisyon = sofor.Pozisyon,
            EhliyetNo = sofor.EhliyetNo,
            EhliyetGecerlilikTarihi = sofor.EhliyetGecerlilikTarihi,
            MykBelgesiGecerlilikTarihi = sofor.MykBelgesiGecerlilikTarihi,
            SrcBelgesiVarMi = sofor.YayginEgitimSertifikasiVarMi || sofor.SrcBelgesiGecerlilikTarihi.HasValue,
            PsikoteknikGecerlilikTarihi = sofor.PsikoteknikGecerlilikTarihi,
            SaglikRaporuGecerlilikTarihi = sofor.SaglikRaporuGecerlilikTarihi,
            IseBaslamaTarihi = sofor.IseBaslamaTarihi,
            Aktif = sofor.Aktif
        });
    }

    /// <summary>
    /// Şoför bilgilerini günceller
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SoforUpdateDto dto)
    {
        var sofor = await _soforService.GetByIdAsync(id);
        if (sofor == null)
            return NotFound(new { Error = "Şoför bulunamadı" });

        if (!string.IsNullOrEmpty(dto.SoforKodu))
            sofor.SoforKodu = dto.SoforKodu;

        if (!string.IsNullOrEmpty(dto.Ad))
            sofor.Ad = dto.Ad;

        if (!string.IsNullOrEmpty(dto.Soyad))
            sofor.Soyad = dto.Soyad;

        if (dto.TcKimlikNo != null)
            sofor.TcKimlikNo = dto.TcKimlikNo;

        if (dto.Telefon != null)
            sofor.Telefon = dto.Telefon;

        if (dto.Email != null)
            sofor.Email = dto.Email;

        if (dto.Adres != null)
            sofor.Adres = dto.Adres;

        if (!string.IsNullOrEmpty(dto.Gorev) && Enum.TryParse<PersonelGorev>(dto.Gorev, true, out var gorev))
            sofor.Gorev = gorev;

        if (dto.Departman != null)
            sofor.Departman = dto.Departman;

        if (dto.Pozisyon != null)
            sofor.Pozisyon = dto.Pozisyon;

        if (dto.EhliyetNo != null)
            sofor.EhliyetNo = dto.EhliyetNo;

        if (dto.EhliyetGecerlilikTarihi.HasValue)
            sofor.EhliyetGecerlilikTarihi = dto.EhliyetGecerlilikTarihi;

        if (dto.MykBelgesiGecerlilikTarihi.HasValue)
            sofor.MykBelgesiGecerlilikTarihi = dto.MykBelgesiGecerlilikTarihi;

        if (dto.SrcBelgesiVarMi.HasValue)
            sofor.YayginEgitimSertifikasiVarMi = dto.SrcBelgesiVarMi.Value;

        sofor.SrcBelgesiGecerlilikTarihi = null;

        if (dto.PsikoteknikGecerlilikTarihi.HasValue)
            sofor.PsikoteknikGecerlilikTarihi = dto.PsikoteknikGecerlilikTarihi;

        if (dto.SaglikRaporuGecerlilikTarihi.HasValue)
            sofor.SaglikRaporuGecerlilikTarihi = dto.SaglikRaporuGecerlilikTarihi;

        if (dto.IseBaslamaTarihi.HasValue)
            sofor.IseBaslamaTarihi = dto.IseBaslamaTarihi;

        if (dto.Aktif.HasValue)
            sofor.Aktif = dto.Aktif.Value;

        await _soforService.UpdateAsync(sofor);

        return Ok(new SoforDto
        {
            Id = sofor.Id,
            SoforKodu = sofor.SoforKodu,
            Ad = sofor.Ad,
            Soyad = sofor.Soyad,
            TcKimlikNo = sofor.TcKimlikNo,
            Telefon = sofor.Telefon,
            Email = sofor.Email,
            Adres = sofor.Adres,
            Gorev = sofor.Gorev.ToString(),
            Departman = sofor.Departman,
            Pozisyon = sofor.Pozisyon,
            EhliyetNo = sofor.EhliyetNo,
            EhliyetGecerlilikTarihi = sofor.EhliyetGecerlilikTarihi,
            MykBelgesiGecerlilikTarihi = sofor.MykBelgesiGecerlilikTarihi,
            SrcBelgesiVarMi = sofor.YayginEgitimSertifikasiVarMi || sofor.SrcBelgesiGecerlilikTarihi.HasValue,
            PsikoteknikGecerlilikTarihi = sofor.PsikoteknikGecerlilikTarihi,
            SaglikRaporuGecerlilikTarihi = sofor.SaglikRaporuGecerlilikTarihi,
            IseBaslamaTarihi = sofor.IseBaslamaTarihi,
            Aktif = sofor.Aktif
        });
    }

    /// <summary>
    /// Şoför siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var sofor = await _soforService.GetByIdAsync(id);
        if (sofor == null)
            return NotFound(new { Error = "Şoför bulunamadı" });

        await _soforService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Belge süresi dolan şoförleri listeler
    /// </summary>
    [HttpGet("belge-uyari")]
    public async Task<IActionResult> GetBelgeUyarilari([FromQuery] int gun = 30)
    {
        var simdi = DateTime.UtcNow;
        var sinirTarih = simdi.AddDays(gun);

        var soforler = await _soforService.GetAllAsync();
        var uyarilar = soforler
            .Where(s => s.Aktif && s.Gorev == PersonelGorev.Sofor)
            .SelectMany(s => new[]
            {
                new { Sofor = s, BelgeTipi = "Ehliyet", BitisTarihi = s.EhliyetGecerlilikTarihi },
                new { Sofor = s, BelgeTipi = "MYK Belgesi", BitisTarihi = s.MykBelgesiGecerlilikTarihi },
                new { Sofor = s, BelgeTipi = "Psikoteknik", BitisTarihi = s.PsikoteknikGecerlilikTarihi },
                new { Sofor = s, BelgeTipi = "Sağlık Raporu", BitisTarihi = s.SaglikRaporuGecerlilikTarihi }
            })
            .Where(x => x.BitisTarihi.HasValue && x.BitisTarihi.Value <= sinirTarih)
            .Select(x => new
            {
                SoforId = x.Sofor.Id,
                AdSoyad = $"{x.Sofor.Ad} {x.Sofor.Soyad}",
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
public class SoforDto
{
    public int Id { get; set; }
    public string? SoforKodu { get; set; }
    public string Ad { get; set; } = "";
    public string Soyad { get; set; } = "";
    public string? TcKimlikNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }
    public string Gorev { get; set; } = "";
    public string? Departman { get; set; }
    public string? Pozisyon { get; set; }
    public string? EhliyetNo { get; set; }
    public DateTime? EhliyetGecerlilikTarihi { get; set; }
    public DateTime? MykBelgesiGecerlilikTarihi { get; set; }
    public bool SrcBelgesiVarMi { get; set; }
    public DateTime? PsikoteknikGecerlilikTarihi { get; set; }
    public DateTime? SaglikRaporuGecerlilikTarihi { get; set; }
    public DateTime? IseBaslamaTarihi { get; set; }
    public bool Aktif { get; set; }
}

public class SoforCreateDto
{
    public string? SoforKodu { get; set; }
    public string Ad { get; set; } = "";
    public string Soyad { get; set; } = "";
    public string? TcKimlikNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }
    public string? Gorev { get; set; }
    public string? Departman { get; set; }
    public string? Pozisyon { get; set; }
    public string? EhliyetNo { get; set; }
    public DateTime? EhliyetGecerlilikTarihi { get; set; }
    public DateTime? MykBelgesiGecerlilikTarihi { get; set; }
    public bool SrcBelgesiVarMi { get; set; }
    public DateTime? PsikoteknikGecerlilikTarihi { get; set; }
    public DateTime? SaglikRaporuGecerlilikTarihi { get; set; }
    public DateTime? IseBaslamaTarihi { get; set; }
    public bool? Aktif { get; set; }
}

public class SoforUpdateDto
{
    public string? SoforKodu { get; set; }
    public string? Ad { get; set; }
    public string? Soyad { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }
    public string? Gorev { get; set; }
    public string? Departman { get; set; }
    public string? Pozisyon { get; set; }
    public string? EhliyetNo { get; set; }
    public DateTime? EhliyetGecerlilikTarihi { get; set; }
    public DateTime? MykBelgesiGecerlilikTarihi { get; set; }
    public bool? SrcBelgesiVarMi { get; set; }
    public DateTime? PsikoteknikGecerlilikTarihi { get; set; }
    public DateTime? SaglikRaporuGecerlilikTarihi { get; set; }
    public DateTime? IseBaslamaTarihi { get; set; }
    public bool? Aktif { get; set; }
}



