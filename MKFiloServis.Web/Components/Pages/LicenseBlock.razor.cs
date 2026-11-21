using MKFiloServis.Web.Services;

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
        var validation = await LicenseService.ValidateAsync(persistValidation: false);
        _isDemoExpired = lic is { IsDemo: true } && validation.Code == MKFiloServis.Shared.Licensing.LicenseStatusCode.DemoExpired;
        _contactPhone = lic?.ContactPhone;

        _message = validation.IsValid
            ? "Lisansınız geçerli."
            : !string.IsNullOrWhiteSpace(validation.Message)
                ? validation.Message
                : "Henüz lisans yüklenmemiş. İsterseniz 15 günlük demo modunu kullanabilirsiniz.";
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
    }




