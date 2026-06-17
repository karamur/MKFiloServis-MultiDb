using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Components.Pages.Admin;

public partial class Denetim
{
    private List<Firma> _firmalar = [];
    private int _firmaId = 1;
    private int _yil = DateTime.Today.Year;
    private int _ay = DateTime.Today.Month;
    private bool _yukleniyor;
    private DenetimRaporu? _rapor;

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        _firmalar = await db.Firmalar.AsNoTracking().Where(f => !f.IsDeleted).OrderBy(f => f.FirmaAdi).ToListAsync();
        if (_firmalar.Any()) _firmaId = _firmalar.First().Id;
        await Denetle();
    }

    private async Task Denetle()
    {
        _yukleniyor = true; StateHasChanged();
        try { _rapor = await DenetimService.DenetleAsync(_firmaId, _yil, _ay); }
        finally { _yukleniyor = false; StateHasChanged(); }
    }

    private async Task IncidentLogla()
    {
        if (_rapor == null || _rapor.Temiz) return;
        await using var db = await DbFactory.CreateDbContextAsync();

        foreach (var k in _rapor.Kontroller.Where(k => !k.Gecti))
        {
            db.IncidentLogs.Add(new IncidentLog
            {
                FirmaId = _firmaId,
                Level = "Error",
                Message = $"Denetim tutarsızlığı: {k.Ad}",
                Entity = "Denetim",
                BeklenenDeger = k.Beklenen,
                GerceklesenDeger = k.Gerceklesen,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }
}
