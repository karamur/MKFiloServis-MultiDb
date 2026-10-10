using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Fatura yönetimi API endpoint'leri
/// </summary>
[Authorize(Policy = "Licensed:fatura")]
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class FaturalarController : ControllerBase
{
    private readonly IFaturaService _faturaService;
    private readonly ICariService _cariService;
    private readonly CurrentPermissionGuard _permissionGuard;

    public FaturalarController(IFaturaService faturaService, ICariService cariService, CurrentPermissionGuard permissionGuard)
    {
        _faturaService = faturaService;
        _cariService = cariService;
        _permissionGuard = permissionGuard;
    }

    /// <summary>
    /// Tüm faturaları listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? tip = null,
        [FromQuery] string? yon = null,
        [FromQuery] string? durum = null,
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null,
        [FromQuery] int? cariId = null,
        [FromQuery] int? firmaId = null)
    {
        if (!await HasReadPermissionAsync(yon)) return Forbid();
        var filter = BuildFilter(tip, yon, durum, baslangic, bitis, cariId, firmaId, pageNumber: 1, pageSize: 100);
        var page = await _faturaService.GetPagedAsync(filter);
        if (page.TotalCount > filter.PageSize)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Fatura listesi sayfalama gerektiriyor",
                Detail = "Bu sonuç 100 kaydı aşıyor. GET /api/faturalar/sayfali?PageNumber=1&PageSize=100 uç noktasını kullanın."
            });
        return Ok(page.Items.Select(MapFaturaDto));
    }

    /// <summary>Filtreleri veritabanında uygulayarak fatura listesini sayfalar.</summary>
    [HttpGet("sayfali")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? tip = null,
        [FromQuery] string? yon = null,
        [FromQuery] string? durum = null,
        [FromQuery] DateTime? baslangic = null,
        [FromQuery] DateTime? bitis = null,
        [FromQuery] int? cariId = null,
        [FromQuery] int? firmaId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25)
    {
        if (!await HasReadPermissionAsync(yon)) return Forbid();
        var filter = BuildFilter(tip, yon, durum, baslangic, bitis, cariId, firmaId, pageNumber, pageSize);
        var page = await _faturaService.GetPagedAsync(filter);
        var result = new PagedResult<FaturaDto>(page.Items.Select(MapFaturaDto).ToList(), page.TotalCount,
            page.PageNumber, page.PageSize);
        return Ok(result);
    }

    /// <summary>
    /// Belirli bir faturayı getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var fatura = await _faturaService.GetByIdWithKalemlerAsync(id);
        if (fatura == null)
            return NotFound(new { Error = "Fatura bulunamadı" });
        if (!await HasReadPermissionAsync(fatura.FaturaYonu.ToString()))
            return NotFound(new { Error = "Fatura bulunamadı" });

        return Ok(MapFaturaDetayDto(fatura));
    }

    /// <summary>
    /// Fatura numarasıyla arar
    /// </summary>
    [HttpGet("no/{faturaNo}")]
    public async Task<IActionResult> GetByNo(string faturaNo, [FromQuery] string? yon = null, [FromQuery] int? firmaId = null)
    {
        if (!await HasReadPermissionAsync(yon)) return Forbid();
        var requestedDirection = Enum.TryParse<FaturaYonu>(yon, true, out var parsedDirection)
            ? parsedDirection
            : (FaturaYonu?)null;
        var fatura = await _faturaService.GetByNoAsync(faturaNo, requestedDirection, firmaId);
        
        if (fatura == null)
            return NotFound(new { Error = "Fatura bulunamadı" });
        if (!await HasReadPermissionAsync(fatura.FaturaYonu.ToString()))
            return NotFound(new { Error = "Fatura bulunamadı" });

        return Ok(MapFaturaDto(fatura));
    }

    /// <summary>
    /// Yeni fatura oluşturur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FaturaCreateDto dto)
    {
        if (!Enum.TryParse<FaturaTipi>(dto.FaturaTipi, true, out var faturaTipi))
            faturaTipi = FaturaTipi.SatisFaturasi;

        if (!Enum.TryParse<FaturaYonu>(dto.FaturaYonu, true, out var faturaYonu))
            faturaYonu = faturaTipi == FaturaTipi.AlisFaturasi || faturaTipi == FaturaTipi.AlisIadeFaturasi
                ? FaturaYonu.Gelen
                : FaturaYonu.Giden;

        if (!await HasActionPermissionAsync(faturaYonu, "yaz")) return Forbid();

        if (!dto.FirmaId.HasValue || dto.FirmaId.Value <= 0)
            return BadRequest(new { Error = "Firma seçimi gereklidir" });

        if (dto.CariId <= 0)
            return BadRequest(new { Error = "Cari seçimi gereklidir" });

        var cari = await _cariService.GetByIdAsync(dto.CariId);
        if (cari == null)
            return BadRequest(new { Error = "Geçersiz cari" });

        if (dto.FirmalarArasiFatura && (!dto.KarsiFirmaId.HasValue || dto.KarsiFirmaId == dto.FirmaId))
            return BadRequest(new { Error = "Firmalar arası faturada farklı bir karşı firma seçilmelidir" });

        var fatura = new Fatura
        {
            FaturaNo = dto.FaturaNo ?? $"FTR-{DateTime.UtcNow:yyyyMMddHHmmss}",
            FaturaTarihi = dto.FaturaTarihi ?? DateTime.UtcNow,
            VadeTarihi = dto.VadeTarihi,
            FaturaYonu = faturaYonu,
            FaturaTipi = faturaTipi,
            Durum = Enum.TryParse<FaturaDurum>(dto.Durum, true, out var faturaDurum) ? faturaDurum : FaturaDurum.Beklemede,
            CariId = dto.CariId,
            FirmaId = dto.FirmaId,
            FirmalarArasiFatura = dto.FirmalarArasiFatura,
            KarsiFirmaId = dto.FirmalarArasiFatura ? dto.KarsiFirmaId : null,
            Aciklama = dto.Aciklama,
            FaturaKalemleri = dto.Kalemler?.Select(k => new FaturaKalem
            {
                Aciklama = k.Aciklama ?? "",
                Miktar = k.Miktar,
                BirimFiyat = k.BirimFiyat,
                KdvOrani = k.KdvOrani ?? 20,
                ToplamTutar = k.Miktar * k.BirimFiyat,
                KdvTutar = k.Miktar * k.BirimFiyat * (k.KdvOrani ?? 20) / 100
            }).ToList() ?? []
        };

        fatura.AraToplam = fatura.FaturaKalemleri.Sum(k => k.ToplamTutar);
        fatura.KdvTutar = fatura.FaturaKalemleri.Sum(k => k.KdvTutar);
        fatura.GenelToplam = fatura.AraToplam + fatura.KdvTutar;

        try
        {
            await _faturaService.CreateAsync(fatura);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Error = ex.Message });
        }

        return CreatedAtAction(nameof(GetById), new { id = fatura.Id }, MapFaturaDto(fatura));
    }

    /// <summary>
    /// Fatura durumunu günceller
    /// </summary>
    [HttpPatch("{id}/durum")]
    public async Task<IActionResult> UpdateDurum(int id, [FromBody] FaturaDurumUpdateDto dto)
    {
        var fatura = await _faturaService.GetByIdAsync(id);
        if (fatura == null)
            return NotFound(new { Error = "Fatura bulunamadı" });
        if (!await HasActionPermissionAsync(fatura.FaturaYonu, "duzenle"))
            return NotFound(new { Error = "Fatura bulunamadı" });

        if (!Enum.TryParse<FaturaDurum>(dto.Durum, true, out var yeniDurum))
            return BadRequest(new { Error = "Geçersiz durum" });

        fatura.Durum = yeniDurum;

        try
        {
            await _faturaService.UpdateAsync(fatura);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Error = ex.Message });
        }

        return Ok(new { FaturaId = id, Durum = fatura.Durum.ToString() });
    }

    /// <summary>
    /// Fatura siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var fatura = await _faturaService.GetByIdAsync(id);
        if (fatura == null)
            return NotFound(new { Error = "Fatura bulunamadı" });
        if (!await HasActionPermissionAsync(fatura.FaturaYonu, "sil"))
            return NotFound(new { Error = "Fatura bulunamadı" });

        await _faturaService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Vadesi geçmiş faturaları listeler
    /// </summary>
    [HttpGet("vadesi-gecmis")]
    public async Task<IActionResult> GetVadesiGecmis()
    {
        if (!await HasReadPermissionAsync(yon: null)) return Forbid();
        var simdi = DateTime.UtcNow.Date;
        var faturalar = await _faturaService.GetAllAsync();
        
        var vadesiGecmis = faturalar
            .Where(f => f.VadeTarihi.HasValue && 
                       f.VadeTarihi.Value.Date < simdi && 
                       f.Durum != FaturaDurum.Odendi)
            .Select(f => new
            {
                f.Id,
                f.FaturaNo,
                f.FaturaTarihi,
                f.VadeTarihi,
                FaturaTipi = f.FaturaTipi.ToString(),
                CariUnvan = f.Cari?.Unvan,
                f.GenelToplam,
                f.OdenenTutar,
                KalanTutar = f.GenelToplam - f.OdenenTutar,
                GecenGun = (simdi - f.VadeTarihi!.Value.Date).Days
            })
            .OrderByDescending(f => f.GecenGun)
            .ToList();

        return Ok(vadesiGecmis);
    }

    /// <summary>
    /// Fatura istatistiklerini getirir
    /// </summary>
    [HttpGet("istatistikler")]
    public async Task<IActionResult> GetIstatistikler([FromQuery] int? yil = null, [FromQuery] int? ay = null)
    {
        if (!await HasReadPermissionAsync(yon: null)) return Forbid();
        var hedefYil = yil ?? DateTime.UtcNow.Year;
        var hedefAy = ay ?? DateTime.UtcNow.Month;

        var baslangic = new DateTime(hedefYil, hedefAy, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        var faturalar = await _faturaService.GetAllAsync();
        var aylikFaturalar = faturalar.Where(f => 
            f.FaturaTarihi >= baslangic && f.FaturaTarihi <= bitis).ToList();

        var satisFaturalari = aylikFaturalar.Where(f => f.FaturaTipi == FaturaTipi.SatisFaturasi).ToList();
        var alisFaturalari = aylikFaturalar.Where(f => f.FaturaTipi == FaturaTipi.AlisFaturasi).ToList();

        return Ok(new
        {
            Donem = $"{hedefAy:00}/{hedefYil}",
            ToplamFatura = aylikFaturalar.Count,
            SatisFaturaSayisi = satisFaturalari.Count,
            SatisToplam = satisFaturalari.Sum(f => f.GenelToplam),
            AlisFaturaSayisi = alisFaturalari.Count,
            AlisToplam = alisFaturalari.Sum(f => f.GenelToplam),
            OdenmemisFatura = aylikFaturalar.Count(f => f.Durum != FaturaDurum.Odendi),
            OdenmemisTutar = aylikFaturalar.Where(f => f.Durum != FaturaDurum.Odendi).Sum(f => f.GenelToplam - f.OdenenTutar)
        });
    }

    private async Task<bool> HasReadPermissionAsync(string? yon)
    {
        var permissions = new List<string> { Yetkiler.FaturalarOku };
        if (Enum.TryParse<FaturaYonu>(yon, true, out var direction))
            permissions.Add(direction == FaturaYonu.Giden ? Yetkiler.KesilenFaturalarOku : Yetkiler.GelenFaturalarOku);
        else if (!string.IsNullOrWhiteSpace(yon))
            return false;

        try
        {
            await _permissionGuard.RequireAnyAsync(permissions.ToArray());
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static FaturaFilterParams BuildFilter(string? tip, string? yon, string? durum,
        DateTime? baslangic, DateTime? bitis, int? cariId, int? firmaId, int pageNumber, int pageSize)
    {
        return new FaturaFilterParams
        {
            FaturaTipi = Enum.TryParse<FaturaTipi>(tip, true, out var parsedTip) ? parsedTip : null,
            Yon = Enum.TryParse<FaturaYonu>(yon, true, out var parsedYon) ? parsedYon : null,
            Durum = Enum.TryParse<FaturaDurum>(durum, true, out var parsedDurum) ? parsedDurum : null,
            BaslangicTarih = baslangic,
            BitisTarih = bitis,
            CariId = cariId,
            FirmaId = firmaId,
            PageNumber = Math.Clamp(pageNumber, 1, int.MaxValue / 100),
            PageSize = Math.Clamp(pageSize, 1, 100)
        };
    }

    private Task<bool> HasActionPermissionAsync(FaturaYonu yon, string action)
    {
        var general = action switch
        {
            "yaz" => Yetkiler.FaturalarYaz,
            "sil" => Yetkiler.FaturalarSil,
            _ => Yetkiler.FaturalarDuzenle
        };
        var directional = (yon, action) switch
        {
            (FaturaYonu.Giden, "yaz") => Yetkiler.KesilenFaturalarYaz,
            (FaturaYonu.Giden, "sil") => Yetkiler.KesilenFaturalarSil,
            (FaturaYonu.Giden, _) => Yetkiler.KesilenFaturalarDuzenle,
            (FaturaYonu.Gelen, "yaz") => Yetkiler.GelenFaturalarYaz,
            (FaturaYonu.Gelen, "sil") => Yetkiler.GelenFaturalarSil,
            (FaturaYonu.Gelen, _) => Yetkiler.GelenFaturalarDuzenle,
            _ => (string?)null
        };

        return directional is null
            ? Task.FromResult(false)
            : _permissionGuard.HasAnyAsync(general, directional);
    }

    private static FaturaDto MapFaturaDto(Fatura fatura)
    {
        return new FaturaDto
        {
            Id = fatura.Id,
            FaturaNo = fatura.FaturaNo,
            FaturaTarihi = fatura.FaturaTarihi,
            VadeTarihi = fatura.VadeTarihi,
            FaturaTipi = fatura.FaturaTipi.ToString(),
            FaturaYonu = fatura.FaturaYonu.ToString(),
            Durum = fatura.Durum.ToString(),
            CariId = fatura.CariId,
            CariUnvan = fatura.Cari?.Unvan,
            FirmaId = fatura.FirmaId,
            FirmaAdi = fatura.Firma?.FirmaAdi,
            FirmalarArasiFatura = fatura.FirmalarArasiFatura,
            KarsiFirmaId = fatura.KarsiFirmaId,
            KarsiFirmaAdi = fatura.KarsiFirma?.FirmaAdi,
            AraToplam = fatura.AraToplam,
            KdvToplam = fatura.KdvTutar,
            GenelToplam = fatura.GenelToplam,
            OdenenTutar = fatura.OdenenTutar,
            KalanTutar = fatura.GenelToplam - fatura.OdenenTutar,
            Aciklama = fatura.Aciklama
        };
    }

    private static FaturaDetayDto MapFaturaDetayDto(Fatura fatura)
    {
        var detay = new FaturaDetayDto();
        var ozet = MapFaturaDto(fatura);

        detay.Id = ozet.Id;
        detay.FaturaNo = ozet.FaturaNo;
        detay.FaturaTarihi = ozet.FaturaTarihi;
        detay.VadeTarihi = ozet.VadeTarihi;
        detay.FaturaTipi = ozet.FaturaTipi;
        detay.FaturaYonu = ozet.FaturaYonu;
        detay.Durum = ozet.Durum;
        detay.CariId = ozet.CariId;
        detay.CariUnvan = ozet.CariUnvan;
        detay.FirmaId = ozet.FirmaId;
        detay.FirmaAdi = ozet.FirmaAdi;
        detay.FirmalarArasiFatura = ozet.FirmalarArasiFatura;
        detay.KarsiFirmaId = ozet.KarsiFirmaId;
        detay.KarsiFirmaAdi = ozet.KarsiFirmaAdi;
        detay.AraToplam = ozet.AraToplam;
        detay.KdvToplam = ozet.KdvToplam;
        detay.GenelToplam = ozet.GenelToplam;
        detay.OdenenTutar = ozet.OdenenTutar;
        detay.KalanTutar = ozet.KalanTutar;
        detay.Aciklama = ozet.Aciklama;
        detay.Kalemler = fatura.FaturaKalemleri?.Select(k => new FaturaKalemDto
        {
            Id = k.Id,
            Aciklama = k.Aciklama,
            Miktar = k.Miktar,
            BirimFiyat = k.BirimFiyat,
            KdvOrani = k.KdvOrani,
            Tutar = k.ToplamTutar,
            KdvTutar = k.KdvTutar
        }).ToList() ?? [];

        return detay;
    }
}

// DTO Modelleri
public class FaturaDto
{
    public int Id { get; set; }
    public string? FaturaNo { get; set; }
    public DateTime FaturaTarihi { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public string FaturaTipi { get; set; } = "";
    public string FaturaYonu { get; set; } = "";
    public string Durum { get; set; } = "";
    public int CariId { get; set; }
    public string? CariUnvan { get; set; }
    public int? FirmaId { get; set; }
    public string? FirmaAdi { get; set; }
    public bool FirmalarArasiFatura { get; set; }
    public int? KarsiFirmaId { get; set; }
    public string? KarsiFirmaAdi { get; set; }
    public decimal AraToplam { get; set; }
    public decimal KdvToplam { get; set; }
    public decimal GenelToplam { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar { get; set; }
    public string? Aciklama { get; set; }
}

public class FaturaDetayDto : FaturaDto
{
    public List<FaturaKalemDto> Kalemler { get; set; } = [];
}

public class FaturaKalemDto
{
    public int Id { get; set; }
    public string? Aciklama { get; set; }
    public decimal Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal KdvOrani { get; set; }
    public decimal Tutar { get; set; }
    public decimal KdvTutar { get; set; }
}

public class FaturaCreateDto
{
    public string? FaturaNo { get; set; }
    public DateTime? FaturaTarihi { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public string? FaturaTipi { get; set; }
    public string? FaturaYonu { get; set; }
    public string? Durum { get; set; }
    public int? FirmaId { get; set; }
    public bool FirmalarArasiFatura { get; set; }
    public int? KarsiFirmaId { get; set; }
    public int CariId { get; set; }
    public string? Aciklama { get; set; }
    public List<FaturaKalemCreateDto>? Kalemler { get; set; }
}

public class FaturaKalemCreateDto
{
    public string? Aciklama { get; set; }
    public decimal Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal? KdvOrani { get; set; }
}

public class FaturaDurumUpdateDto
{
    public string Durum { get; set; } = "";
}



