using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace KOAFiloServis.Web.Components.Pages.Operasyon;

public partial class OperasyonImport : ComponentBase
{
    [Inject] private IOperasyonKaydiService OperasyonService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private IBrowserFile? importFile;
    private List<OperasyonImportSonuc> sonuclar = new();
    private bool isliyor;
    private bool kaydedildi;
    private string? hataMesaji;
    private Stream? cachedStream;

    private int gecerliSayi => sonuclar.Count(s => s.Basarili);
    private int hataliSayi => sonuclar.Count(s => !s.Basarili && !s.Atlandi);
    private int atlananSayi => sonuclar.Count(s => s.Atlandi);

    private void DosyaSecildi(InputFileChangeEventArgs e)
    {
        importFile = e.File;
        sonuclar.Clear();
        kaydedildi = false;
        hataMesaji = null;
        cachedStream = null;
    }

    // ── Preview (dryRun = parse only, no save) ─────────────────────────

    private async Task VeriOnizle()
    {
        if (importFile == null) return;
        isliyor = true; hataMesaji = null; sonuclar.Clear(); kaydedildi = false;

        try
        {
            // Stream'i cache'le — save için tekrar kullanacağız
            var ms = new MemoryStream();
            await importFile.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms);
            ms.Position = 0;
            cachedStream = ms;

            sonuclar = await OperasyonService.ImportFromExcelAsync(ms, dryRun: true);
        }
        catch (Exception ex)
        {
            hataMesaji = ex.Message;
        }
        finally
        {
            isliyor = false;
        }
    }

    // ── Save (dryRun = false, DB'ye yazar) ─────────────────────────────

    private async Task TopluKaydet()
    {
        if (cachedStream == null || gecerliSayi == 0) return;
        isliyor = true; hataMesaji = null;

        try
        {
            cachedStream.Position = 0;
            sonuclar = await OperasyonService.ImportFromExcelAsync(cachedStream, dryRun: false);
            kaydedildi = true;
            ToastService.ShowSuccess($"{gecerliSayi} operasyon kaydedildi. {hataliSayi} hatalı, {atlananSayi} atlandı.");
        }
        catch (Exception ex)
        {
            hataMesaji = ex.Message;
            ToastService.ShowError("Kaydetme hatası: " + ex.Message);
        }
        finally
        {
            isliyor = false;
        }
    }

    // ── Şablon ─────────────────────────────────────────────────────────

    private async Task SablonIndir()
    {
        var bytes = OperasyonService.ExcelSablonUret();
        await JS.InvokeVoidAsync("downloadFileFromBytes", "Operasyon_Sablon.xlsx",
            Convert.ToBase64String(bytes));
    }
}
