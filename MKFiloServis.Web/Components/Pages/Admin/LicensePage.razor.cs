using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Components.Pages.Admin;

public partial class LicensePage
{
    private LicenseInfo? _license;
    private bool _yukleniyor = true;
    private bool _calisiyor;
    private bool _validationIsValid;
    private string _validationMessage = "";
    private string _licenseKey = "";

    protected override async Task OnInitializedAsync()
    {
        _license = await LicenseService.GetCurrentLicenseAsync();
        // ValidateAsync SADECE startup'ta (Program.cs) çalışır.
        // Burada cache'teki lisans bilgisi yeterlidir.
        _validationIsValid = LicenseService.HasValidLicense();
        _validationMessage = _validationIsValid ? "Lisans geçerli." : "Lisans bulunamadı veya geçersiz.";
        _yukleniyor = false;
    }

    private async Task LisansYukle()
    {
        _calisiyor = true; StateHasChanged();
        try
        {
            if (string.IsNullOrWhiteSpace(_licenseKey)) return;
            var lic = await LicenseService.ActivateFromKeyAsync(_licenseKey);
            _license = lic;
            _validationIsValid = true;
            _validationMessage = "Lisans geçerli.";
            MKFiloServis.Shared.AppMode.ExitDemoMode(); // Lisans yüklendi → FULL MODE
        }
        catch (Exception ex)
        {
            _validationIsValid = false;
            _validationMessage = ex.Message;
        }
        finally { _calisiyor = false; StateHasChanged(); }
    }
}




