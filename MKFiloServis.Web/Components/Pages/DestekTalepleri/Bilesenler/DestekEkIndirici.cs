using Microsoft.JSInterop;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Components.Pages.DestekTalepleri.Bilesenler;

/// <summary>Destek ekini (yetki denetimiyle) tarayıcıya indirir; ek listesi ve yazışma bileşenleri kullanır.</summary>
internal static class DestekEkIndirici
{
    public static async Task IndirAsync(IDestekTalebiService servis, IJSRuntime js, IToastService toast, int ekId)
    {
        try
        {
            var ek = await servis.EkIndirAsync(ekId);
            if (ek == null)
            {
                toast.ShowWarning("Dosya bulunamadı; silinmiş ya da taşınmış olabilir.");
                return;
            }

            await js.InvokeVoidAsync("downloadFileFromBase64", ek.DosyaAdi, Convert.ToBase64String(ek.Icerik), ek.MimeTipi);
        }
        catch (UnauthorizedAccessException ex)
        {
            toast.ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            toast.ShowError($"Dosya indirilemedi: {ex.GetBaseException().Message}");
        }
    }

    public static string Ikon(string? mimeTipi)
    {
        if (string.IsNullOrEmpty(mimeTipi)) return "bi-file-earmark";
        if (mimeTipi.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "bi-file-earmark-image";
        if (mimeTipi.Contains("pdf", StringComparison.OrdinalIgnoreCase)) return "bi-file-earmark-pdf";
        if (mimeTipi.Contains("word", StringComparison.OrdinalIgnoreCase)) return "bi-file-earmark-word";
        if (mimeTipi.Contains("excel", StringComparison.OrdinalIgnoreCase) || mimeTipi.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase))
            return "bi-file-earmark-excel";
        if (mimeTipi.Contains("zip", StringComparison.OrdinalIgnoreCase) || mimeTipi.Contains("compressed", StringComparison.OrdinalIgnoreCase))
            return "bi-file-earmark-zip";
        return "bi-file-earmark";
    }

    public static string Boyut(long bayt) => bayt switch
    {
        < 1024 => $"{bayt} B",
        < 1024 * 1024 => $"{bayt / 1024} KB",
        _ => $"{bayt / (1024.0 * 1024.0):0.#} MB"
    };
}
