using MKFiloServis.Web.Services;
using Microsoft.JSInterop;

namespace MKFiloServis.Web.Components.Pages;

public partial class LicenseBlock
{
    private bool _isDemoExpired;
    private string? _message;
    private string? _contactPhone;
    private string? _licenseKey;
    private string? _uploadError;
    private bool _isActivating;
    private bool _isDemoInstalling;
    private string _machineCode = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _machineCode = LicenseService.GetMachineId();

        var lic = await LicenseService.GetCurrentLicenseAsync();
        if (lic != null)
        {
            _isDemoExpired = lic.IsDemo && DateTime.UtcNow > lic.ExpireDate;
            _message = _isDemoExpired
                ? $"Demo suresi {lic.ExpireDate:dd.MM.yyyy} tarihinde doldu."
                : $"Lisans {lic.ExpireDate:dd.MM.yyyy} tarihinde sona erdi.";
            _contactPhone = lic.ContactPhone;
        }
        else
        {
            _message = "Henuz lisans yuklenmemis. Isterseniz 15 gunluk demo modunu kullanabilirsiniz.";
        }
    }

    private async Task LisansAktiveEt()
    {
        _uploadError = null;

        if (string.IsNullOrWhiteSpace(_licenseKey))
        {
            _uploadError = "Lutfen lisans anahtarini yapistirin.";
            return;
        }

        _isActivating = true;
        StateHasChanged();

        try
        {
            _licenseKey = _licenseKey.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");

            await LicenseService.ActivateFromKeyAsync(_licenseKey);
            Nav.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            _uploadError = ex.Message;
        }
        finally
        {
            _isActivating = false;
            StateHasChanged();
        }
    }

    private async Task DemoModunuYukle()
    {
        _uploadError = null;
        _isDemoInstalling = true;
        StateHasChanged();

        try
        {
            await LicenseService.InstallDemoLicenseAsync();
            Nav.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            _uploadError = ex.Message;
        }
        finally
        {
            _isDemoInstalling = false;
            StateHasChanged();
        }
    }

    private async Task MakineKodunuKopyala()
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", _machineCode);
            _message = "Makine kodu panoya kopyalandı.";
            StateHasChanged();
        }
        catch
        {
            _message = $"Makine kodu: {_machineCode}";
            StateHasChanged();
        }
    }
}




