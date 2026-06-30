using System.Text;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace MKFiloServis.Web.Components.Pages.Admin;

public partial class LicenseGenerate : ComponentBase
{
    [Inject] private LicenseService LicenseSvc { get; set; } = default!;
    [Inject] private IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string firmaKodu = "";
    private string machineId = "";
    private DateTime expireDate = DateTime.UtcNow.AddYears(1);
    private bool calisiyor;
    private bool otomatikDolduruldu;

    private string uretilenKey = "";
    private DateTime uretilenExpire;
    private string hataMesaji = "";
    private bool kopyalandi;

    private List<LicenseInfo> sonUretilenler = new();

    protected override async Task OnInitializedAsync()
    {
        // PART 4: Query string'den auto-fill
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var qMachineId = query["machineId"];
        if (!string.IsNullOrWhiteSpace(qMachineId))
        {
            machineId = qMachineId;
            otomatikDolduruldu = true;
        }

        var qFirma = query["firma"];
        if (!string.IsNullOrWhiteSpace(qFirma))
        {
            firmaKodu = qFirma;
        }

        await SonLisanslariYukle();
    }

    private async Task LisansUret()
    {
        if (string.IsNullOrWhiteSpace(firmaKodu) || string.IsNullOrWhiteSpace(machineId))
            return;

        calisiyor = true;
        hataMesaji = "";
        uretilenKey = "";
        StateHasChanged();

        try
        {
            var created = DateTime.UtcNow; // 🔥 CRITICAL: UTC
            const string allowedVersion = "1.0.99";

            // AYNI SIGNATURE — LicenseService.GenerateSignature() + Desktop MainForm.Uret() ile birebir
            var signature = LicenseService.GenerateSignature(
                firmaKodu, machineId, expireDate,
                isDemo: false, allowedVersion, created);

            // JSON → Base64 (LicenseInfo entity deserialize edilebilir)
            var json = JsonSerializer.Serialize(new
            {
                FirmaKodu = firmaKodu,
                MachineId = machineId,
                ExpireDate = expireDate,
                DurationDays = 365,
                AllowedVersion = allowedVersion,
                IsDemo = false,
                CreatedAt = created,
                ContactPhone = "",
                Signature = signature
            });

            uretilenKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            uretilenExpire = expireDate;

            // Part 5: Log — DB'ye kaydet
            var lic = new LicenseInfo
            {
                FirmaKodu = firmaKodu,
                MachineId = machineId,
                ExpireDate = expireDate,
                DurationDays = 365,
                AllowedVersion = allowedVersion,
                IsDemo = false,
                CreatedAt = created,
                ContactPhone = "",
                Signature = signature,
                IsActive = false // Admin panelinden uretilen lisanslar log olarak kalir
            };
            await LicenseSvc.SaveGeneratedLogAsync(lic);

            await SonLisanslariYukle();
        }
        catch (Exception ex)
        {
            hataMesaji = $"Lisans üretilemedi: {ex.Message}";
        }
        finally
        {
            calisiyor = false;
            StateHasChanged();
        }
    }

    private async Task Kopyala()
    {
        if (string.IsNullOrEmpty(uretilenKey)) return;

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", uretilenKey);
            kopyalandi = true;
            StateHasChanged();
            await Task.Delay(2500);
            kopyalandi = false;
            StateHasChanged();
        }
        catch
        {
            // Clipboard API desteklenmiyorsa
        }
    }

    private void Sifirla()
    {
        uretilenKey = "";
        hataMesaji = "";
        kopyalandi = false;
    }

    private async Task SonLisanslariYukle()
    {
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            sonUretilenler = await db.LicenseInfos
                .Where(l => !l.IsActive && !l.IsDeleted && !l.IsDemo)
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToListAsync();
        }
        catch
        {
            sonUretilenler = new();
        }
    }
}



