using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services;

namespace KOAFiloServis.Web.Components.Pages.Admin;

public partial class LicensePage
{
    private LicenseInfo? _license;
    private bool _yukleniyor = true;
    private bool _calisiyor;
    private bool _validationIsValid;
    private string _validationMessage = "";
    private string _firmaKodu = "";
    private string _machineId = LicenseService.GetMachineId();
    private DateTime _expireDate = DateTime.UtcNow.AddYears(1);

    protected override async Task OnInitializedAsync()
    {
        _license = await LicenseService.GetCurrentLicenseAsync();
        if (_license != null)
        {
            var v = await LicenseService.ValidateAsync();
            _validationIsValid = v.IsValid;
            _validationMessage = v.Message;
        }
        _yukleniyor = false;
    }

    private async Task LisansYukle()
    {
        _calisiyor = true; StateHasChanged();
        try
        {
            if (string.IsNullOrWhiteSpace(_firmaKodu)) return;
            var lic = new LicenseInfo
            {
                FirmaKodu = _firmaKodu,
                MachineId = _machineId,
                ExpireDate = _expireDate,
                Signature = LicenseService.GenerateSignature(_firmaKodu, _machineId, _expireDate),
                CreatedAt = DateTime.UtcNow
            };
            await LicenseService.SaveLicenseAsync(lic);
            _license = lic;
            _validationIsValid = true;
        }
        finally { _calisiyor = false; StateHasChanged(); }
    }
}
