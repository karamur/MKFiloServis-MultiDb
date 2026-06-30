using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Middleware;

/// <summary>
/// Yakalanmayan tüm exception'ları AppErrorLog tablosuna kaydeder.
/// Program.cs'de app.UseMiddleware&lt;ErrorLoggingMiddleware&gt;() ile eklenir.
/// </summary>
public class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorLoggingMiddleware> _logger;

    public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yakalanmayan hata: {Path}", context.Request.Path);

            try
            {
                var dbFactory = context.RequestServices.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                await using var db = await dbFactory.CreateDbContextAsync();

                var userId = context.User?.Identity?.Name ?? context.User?.FindFirst("sub")?.Value;

                db.AppErrorLogs.Add(new AppErrorLog
                {
                    Message = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message,
                    StackTrace = ex.StackTrace,
                    Path = $"{context.Request.Method} {context.Request.Path}",
                    UserId = userId,
                    Severity = "Critical",
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Hata loglanırken ikincil hata oluştu");
            }

            // Kullanıcıya 500 döndür
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Beklenmeyen bir hata oluştu. Sistem yöneticisi bilgilendirildi.");
        }
    }
}



