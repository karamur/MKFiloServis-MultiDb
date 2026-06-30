using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Components.Pages.Admin;

public partial class ErrorLogs
{
    private List<AppErrorLog> _errors = [];
    private bool _yukleniyor = true;
    private string _filtreSeverity = "";
    private int _filtreGun = 7;
    private AppErrorLog? _selectedError;

    protected override async Task OnInitializedAsync()
    {
        await Yenile();
    }

    private async Task Yenile()
    {
        _yukleniyor = true;
        StateHasChanged();

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var since = DateTime.UtcNow.AddDays(-_filtreGun);

            var query = db.AppErrorLogs
                .AsNoTracking()
                .Where(e => e.CreatedAt >= since && !e.IsDeleted);

            if (!string.IsNullOrEmpty(_filtreSeverity))
                query = query.Where(e => e.Severity == _filtreSeverity);

            _errors = await query
                .OrderByDescending(e => e.CreatedAt)
                .Take(200)
                .ToListAsync();
        }
        finally
        {
            _yukleniyor = false;
            StateHasChanged();
        }
    }

    private void DetayGoster(AppErrorLog err)
    {
        _selectedError = err;
    }

    private async Task Coz(AppErrorLog err)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var entity = await db.AppErrorLogs.FindAsync(err.Id);
        if (entity != null)
        {
            entity.Cozuldu = true;
            entity.CozulmeTarihi = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        _selectedError = null;
        await Yenile();
    }

    private async Task Temizle()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var since = DateTime.UtcNow.AddDays(-_filtreGun);
        await db.AppErrorLogs
            .Where(e => e.CreatedAt < since && !e.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, DateTime.UtcNow));
        await Yenile();
    }
}




