using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Controllers;

[ApiController]
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
            Timestamp = report.CheckedAt,
            Database = new
            {
                report.Database.IsHealthy,
                report.Database.ProviderName,
                report.Database.ResponseTimeMs
            },
            Disk = new
            {
                report.Disk.IsHealthy,
                report.Disk.UsedPercentage,
                report.Disk.WarningMessage
            },
            Memory = new
            {
                report.Memory.IsHealthy,
                report.Memory.WorkingSetBytes,
                report.Memory.WarningMessage
            }
        });
    }

    [HttpGet("details")]
    [AllowAnonymous]
    public async Task<ActionResult<SystemHealthReport>> GetDetails()
    {
        var report = await _healthService.GetHealthReportAsync();
        return report.OverallStatus == HealthStatus.Critical
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, report)
            : Ok(report);
    }
}



