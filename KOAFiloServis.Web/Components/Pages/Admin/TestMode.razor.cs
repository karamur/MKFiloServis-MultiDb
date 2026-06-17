using KOAFiloServis.Web.Services;

namespace KOAFiloServis.Web.Components.Pages.Admin;

public partial class TestMode
{
    private bool _testAktif;
    private bool _calisiyor;
    private string _testTag = $"TEST_{DateTime.UtcNow:yyyyMMdd_HHmm}";
    private TestBaslatSonuc? _baslatSonuc;
    private TestRollbackSonuc? _rollbackSonuc;

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
}
