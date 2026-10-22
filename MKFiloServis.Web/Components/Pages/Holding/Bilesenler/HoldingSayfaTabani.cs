using Microsoft.AspNetCore.Components;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Components.Pages.Holding.Bilesenler;

/// <summary>
/// Holding sayfalarının ortak davranışı: açılış dönemi (verisi olan en son ay), dönem değişince yeniden yükleme,
/// hızlı dönem değiştirmede eski yanıtı atma ve hata durumu. Sayfa yalnız <see cref="VeriGetirAsync"/> ve çizimi yazar.
/// </summary>
public abstract class HoldingSayfaTabani<TVeri> : ComponentBase where TVeri : class
{
    private int _istekNo;

    [Inject] protected IHoldingService HoldingService { get; set; } = default!;

    [Inject] protected ILogger<HoldingSayfaTabani<TVeri>> Gunluk { get; set; } = default!;

    protected int Yil { get; private set; }

    /// <summary>1–12: ay; 0: tüm yıl.</summary>
    protected int Ay { get; private set; }

    protected TVeri? Veri { get; private set; }

    protected bool Yukleniyor { get; private set; } = true;

    protected string? Hata { get; private set; }

    /// <summary>Sayfa "Tüm Yıl" seçeneğini destekler mi.</summary>
    protected virtual bool TumYilDestekli => true;

    /// <summary>İlk açılışta (verisi olan son yılın) tüm yıl mı seçilsin; aksi halde verisi olan son ay.</summary>
    protected virtual bool VarsayilanTumYil => false;

    protected static DateTime Bugun => BusinessTime.Today;

    protected static int EnErkenYil => Bugun.Year - 3;

    protected static int EnGecYil => Bugun.Year;

    protected abstract Task<TVeri> VeriGetirAsync(int yil, int ay);

    protected override async Task OnInitializedAsync()
    {
        var son = await AcilisDonemiAsync();
        Yil = Math.Clamp(son.Yil, EnErkenYil, EnGecYil);
        Ay = VarsayilanTumYil && TumYilDestekli ? 0 : son.Ay;
        await YukleAsync();
    }

    private async Task<HoldingDonemSecimi> AcilisDonemiAsync()
    {
        try
        {
            return HoldingAnalizi.AcilisDonemi(await HoldingService.GetSonVeriDonemiAsync(), Bugun);
        }
        catch (Exception ex)
        {
            Gunluk.LogWarning(ex, "Holding açılış dönemi okunamadı; içinde bulunulan ay kullanılıyor");
            return new HoldingDonemSecimi(Bugun.Year, Bugun.Month);
        }
    }

    protected async Task DonemDegistiAsync(HoldingDonemSecimi secim)
    {
        Yil = secim.Yil;
        Ay = secim.Ay;
        await YukleAsync();
    }

    protected Task YenidenYukleAsync() => YukleAsync();

    private async Task YukleAsync()
    {
        var no = ++_istekNo;
        Yukleniyor = true;
        Hata = null;

        try
        {
            var sonuc = await VeriGetirAsync(Yil, Ay);

            // Daha yeni bir istek başladıysa (hızlı dönem değiştirme) eski sonuç atılır.
            if (no != _istekNo)
                return;

            Veri = sonuc;
        }
        catch (Exception ex)
        {
            if (no != _istekNo)
                return;

            Gunluk.LogError(ex, "Holding sayfası verisi yüklenemedi");
            Hata = "Veriler şu anda yüklenemedi. Lütfen birkaç saniye sonra yeniden deneyin.";
        }
        finally
        {
            if (no == _istekNo)
                Yukleniyor = false;
        }
    }
}
