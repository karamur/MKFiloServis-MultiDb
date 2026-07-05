using MKFiloServis.Web.Services;
using MKFiloServis.Web.Data;
using MKFiloServis.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Components.Pages.Admin;

public partial class TestMode
{
    private bool _testAktif;
    private bool _calisiyor;
    private string _testTag = $"TEST_{DateTime.UtcNow:yyyyMMdd_HHmm}";
    private TestBaslatSonuc? _baslatSonuc;
    private TestRollbackSonuc? _rollbackSonuc;
    private DemoDataResult? _demoEkleSonuc;
    private DemoDataResult? _demoKaldirSonuc;
    private DemoDataResult? _firmaTemizlenmeSonuc;

    private List<Firma> _firmalar = [];
    private int _secilenFirmaId;

    protected override async Task OnInitializedAsync()
    {
        await LoadFirmalarAsync();
    }

    private async Task LoadFirmalarAsync()
    {
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            _firmalar = await db.Firmalar.AsNoTracking()
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.FirmaAdi)
                .ToListAsync();

            if (_firmalar.Any())
                _secilenFirmaId = _firmalar.First().Id;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Firmalar yükleme hatası");
        }
    }

    private async Task TestBaslat()
    {
        _calisiyor = true; _baslatSonuc = null; StateHasChanged();
        try
        {
            _baslatSonuc = await TestService.BaslatAsync(_testTag);
            _testAktif = _baslatSonuc.Basarili && TestService.IsTestActive;
        }
        finally { _calisiyor = false; StateHasChanged(); }
    }

    private async Task TestGeriAl()
    {
        _calisiyor = true; _rollbackSonuc = null; StateHasChanged();
        try
        {
            _rollbackSonuc = await TestService.GeriAlAsync(_testTag);
            _testAktif = TestService.IsTestActive;
        }
        finally { _calisiyor = false; StateHasChanged(); }
    }

    private async Task DemoVerilerEkle()
    {
        _calisiyor = true; _demoEkleSonuc = null; StateHasChanged();
        try
        {
            _demoEkleSonuc = await DemoService.ResetAndSeedAsync();
        }
        finally { _calisiyor = false; StateHasChanged(); }
    }

    private async Task DemoVerilerKaldir()
    {
        _calisiyor = true; _demoKaldirSonuc = null; StateHasChanged();
        try
        {
            _demoKaldirSonuc = await DemoService.RemoveDemoDataAsync();
        }
        finally { _calisiyor = false; StateHasChanged(); }
    }

    private async Task FirmaVerileriniTemizle()
    {
        if (_secilenFirmaId <= 0)
        {
            _firmaTemizlenmeSonuc = new DemoDataResult 
            { 
                Basarili = false, 
                Mesaj = "Lütfen bir firma seçin" 
            };
            return;
        }

        _calisiyor = true; _firmaTemizlenmeSonuc = null; StateHasChanged();
        try
        {
            _firmaTemizlenmeSonuc = await DemoService.ClearFirmaDataAsync(_secilenFirmaId);
        }
        finally { _calisiyor = false; StateHasChanged(); }
    }
}



