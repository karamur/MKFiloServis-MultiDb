using System.Text.Json;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace KOAFiloServis.Web.Components.Pages;

public partial class LicenseBlock
{
    private bool _isDemoExpired;
    private string? _message;
    private string? _licenseJson;
    private string? _uploadError;

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

    private async Task OnLicenseFileSelected(InputFileChangeEventArgs e)
    {
        _uploadError = null;
        try
        {
            using var reader = new StreamReader(e.File.OpenReadStream(maxAllowedSize: 1024 * 100));
            _licenseJson = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _uploadError = $"Dosya okuma hatasi: {ex.Message}";
        }
    }

    private async Task LisansYukle()
    {
        _uploadError = null;
        try
        {
            if (string.IsNullOrWhiteSpace(_licenseJson))
            {
                _uploadError = "Lutfen bir lisans dosyasi secin.";
                return;
            }

            LicenseInfo? lic = null;
            try { lic = JsonSerializer.Deserialize<LicenseInfo>(_licenseJson); }
            catch { }

            if (lic == null || string.IsNullOrWhiteSpace(lic.Signature))
            {
                _uploadError = "Gecersiz lisans formati.";
                return;
            }

            await LicenseService.SaveLicenseAsync(lic);
            var validation = await LicenseService.ValidateAsync();

            if (validation.IsValid)
            {
                KOAFiloServis.Shared.AppMode.ExitDemoMode(); // Lisans yüklendi → FULL MODE
                Nav.NavigateTo("/", forceLoad: true);
            }
            else
                _uploadError = validation.Message;
        }
        catch (Exception ex)
        {
            _uploadError = $"Hata: {ex.Message}";
        }
    }
}
