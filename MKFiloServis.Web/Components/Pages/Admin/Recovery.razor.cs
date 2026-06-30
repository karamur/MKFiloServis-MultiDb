using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Components.Pages.Admin;

public partial class Recovery
{
    private List<Firma> _firmalar = [];
    private int _firmaId = 1;
    private int _yil = DateTime.Today.Year;
    private int _ay = DateTime.Today.Month;
    private bool _calisiyor;
    private string? _durumMesaji;
    private RebuildOnizleme? _onizleme;
    private RebuildSonuc? _sonuc;

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        _firmalar = await db.Firmalar.AsNoTracking().Where(f => !f.IsDeleted).OrderBy(f => f.FirmaAdi).ToListAsync();
        if (_firmalar.Any()) _firmaId = _firmalar.First().Id;
    }

    private async Task OnizlemeYap()
    {
        _calisiyor = true;
        _sonuc = null;
        _durumMesaji = "Önizleme yapılıyor...";
        StateHasChanged();

        try { _onizleme = await RebuildService.PreviewAsync(_firmaId, _yil, _ay); }
        finally { _calisiyor = false; StateHasChanged(); }
    }

    private bool _rebuildOnay;

    private async Task RebuildCalistir()
    {
        if (!_rebuildOnay)
        {
            _rebuildOnay = true;
            _durumMesaji = "Tekrar tıklayarak onaylayın...";
            StateHasChanged();
            return;
        }

        _rebuildOnay = false;
        _calisiyor = true;
        _durumMesaji = "Rebuild çalışıyor...";
        StateHasChanged();

        try { _sonuc = await RebuildService.RebuildAsync(_firmaId, _yil, _ay); }
        finally { _calisiyor = false; StateHasChanged(); }
    }
}




