using KOAFiloServis.Web.Services;

namespace KOAFiloServis.Web.Components.Pages;

public partial class LicenseBlock
{
    private bool _isDemoExpired;
    private string? _message;
    private string? _licenseKey;
    private string? _uploadError;
    private bool _isActivating;

    protected override async Task OnInitializedAsync()
    {
        var lic = await LicenseService.GetCurrentLicenseAsync();
        if (lic != null)
        {
            _isDemoExpired = lic.IsDemo && DateTime.UtcNow > lic.ExpireDate;
            _message = _isDemoExpired
                ? $"Demo suresi {lic.ExpireDate:dd.MM.yyyy} tarihinde doldu."
                : $"Lisans {lic.ExpireDate:dd.MM.yyyy} tarihinde sona erdi.";
        }
        else
        {
            _message = "Henuz lisans yuklenmemis.";
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
            // Auto-trim
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
}
