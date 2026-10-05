using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MKFiloServis.Web.Controllers;

/// <summary>Commercial licenses are signed only by the internal offline issuer.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class LicenseController : ControllerBase
{
    [HttpPost("generate")]
    public IActionResult Generate() => StatusCode(StatusCodes.Status410Gone, new
    {
        error = "Web uygulamasında lisans üretimi kapatıldı. Lisansı şirket içi imzalama aracıyla oluşturun."
    });
}
