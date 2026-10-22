namespace MKFiloServis.Web.Services;

/// <summary>Genel bakış (dashboard) sayfasının tüm verisi.</summary>
public sealed record HoldingGenelBakis(
    HoldingAnaliz Analiz,
    HoldingButceOzeti? Butce,
    HoldingVadeOzeti? Vade,
    IReadOnlyList<string> Cumleler,
    IReadOnlyList<HoldingBulgu> Bulgular);

/// <summary>Bütçe konsolidasyonu sayfasının verisi.</summary>
public sealed record HoldingButceSayfasi(HoldingAnaliz Analiz, HoldingButceOzeti Butce);

/// <summary>Ödeme planı sayfasının verisi; vade bilgisi alınamazsa <see cref="VadeHatasi"/> doludur.</summary>
public sealed record HoldingOdemeSayfasi(HoldingAnaliz Analiz, HoldingVadeOzeti? Vade, string? VadeHatasi);

/// <summary>
/// Holding sayfalarının verisini servisten çekip analiz nesnelerine çevirir. Sayfalar bu sınıfı çağırır; böylece
/// sayfa kodu ince kalır ve veri hazırlığı sahte servisle test edilebilir.
/// </summary>
public static class HoldingSayfaVerisi
{
    /// <summary>Seçilen yılın ve (ocak/tüm yıl karşılaştırması için) bir önceki yılın anlık görüntüleriyle analiz.</summary>
    public static async Task<HoldingAnaliz> AnalizAsync(IHoldingService servis, int yil, int ay)
    {
        var bu = await servis.GetFirmaKarsilastirmaAsync(yil);
        var onceki = await servis.GetFirmaKarsilastirmaAsync(yil - 1);
        return HoldingAnalizi.Olustur(bu, onceki, yil, ay);
    }

    /// <summary>Bütçenin hesaplandığı ay: seçili ay; "tüm yıl"da verisi olan son ay (ya da içinde bulunulan/son ay).</summary>
    public static int ButceAyi(HoldingAnaliz analiz, int yil, int ay, DateTime bugun)
    {
        if (ay is >= 1 and <= 12)
            return ay;

        return analiz.SonVeriAyi ?? (yil == bugun.Year ? bugun.Month : 12);
    }

    public static async Task<HoldingButceOzeti> ButceAsync(IHoldingService servis, HoldingAnaliz analiz, int yil, int ay, DateTime bugun)
    {
        var satirlar = await servis.GetButceKonsolidasyonAsync(yil);
        return HoldingButceAnalizi.Olustur(satirlar, yil, ButceAyi(analiz, yil, ay, bugun));
    }

    /// <summary>Canlı vade analizi; veri alınamazsa vade null, hata mesajı dolu döner (hata <paramref name="gunluk"/>'e iletilir).</summary>
    public static async Task<(HoldingVadeOzeti? Vade, string? Hata)> VadeAsync(IHoldingService servis, DateTime bugun, Action<Exception>? gunluk = null)
    {
        try
        {
            var kalemler = await servis.GetAcikFaturalarAsync();
            return (HoldingVadeAnalizi.Olustur(kalemler, bugun), null);
        }
        catch (Exception ex)
        {
            gunluk?.Invoke(ex);
            return (null, "Vade bilgisi şu anda alınamadı.");
        }
    }

    /// <summary>
    /// Genel bakış: analiz + bütçe + vade + yönetici cümleleri + dikkat gerektirenler. Bütçe ve vade yardımcı bilgidir;
    /// birinin hatası sayfanın geri kalanını engellemez.
    /// </summary>
    public static async Task<HoldingGenelBakis> GenelBakisAsync(IHoldingService servis, int yil, int ay, DateTime bugun, Action<Exception, string>? gunluk = null)
    {
        var analiz = await AnalizAsync(servis, yil, ay);

        HoldingButceOzeti? butce = null;
        try
        {
            butce = await ButceAsync(servis, analiz, yil, ay, bugun);
        }
        catch (Exception ex)
        {
            gunluk?.Invoke(ex, "Holding bütçe özeti hesaplanamadı");
        }

        var (vade, _) = await VadeAsync(servis, bugun, ex => gunluk?.Invoke(ex, "Holding vade özeti alınamadı"));

        return new HoldingGenelBakis(
            analiz,
            butce,
            vade,
            HoldingAnalizi.OzetCumleleri(analiz, butce),
            HoldingAnalizi.Bulgular(analiz, butce, vade));
    }

    public static async Task<HoldingButceSayfasi> ButceSayfasiAsync(IHoldingService servis, int yil, int ay, DateTime bugun)
    {
        var analiz = await AnalizAsync(servis, yil, ay);
        var butce = await ButceAsync(servis, analiz, yil, ay, bugun);
        return new HoldingButceSayfasi(analiz, butce);
    }

    public static async Task<HoldingOdemeSayfasi> OdemeSayfasiAsync(IHoldingService servis, int yil, int ay, DateTime bugun, Action<Exception>? gunluk = null)
    {
        var analiz = await AnalizAsync(servis, yil, ay);
        var (vade, hata) = await VadeAsync(servis, bugun, gunluk);
        return new HoldingOdemeSayfasi(analiz, vade, hata);
    }
}
