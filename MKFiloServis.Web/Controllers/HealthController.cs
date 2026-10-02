using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ISystemHealthService _healthService;

    public HealthController(ISystemHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        var report = await _healthService.GetHealthReportAsync();

        var statusCode = report.OverallStatus switch
        {
            HealthStatus.Critical => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status200OK
        };

        return StatusCode(statusCode, new
        {
            Status = report.OverallStatus.ToString(),
            Timestamp = report.CheckedAt
        });
    }

    [HttpGet("details")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SystemHealthReport>> GetDetails()
    {
        var report = await _healthService.GetHealthReportAsync();
        return report.OverallStatus == HealthStatus.Critical
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, report)
            : Ok(report);
    }
}



