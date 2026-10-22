using System.Collections;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

/// <summary>Holding bileşen/sayfa testlerinin ortak yardımcıları: statik çizim ve sahte servis.</summary>
internal static class HoldingTestAltyapisi
{
    /// <summary>
    /// Bileşeni statik HTML'e çizer. Dinamik metinler HTML kodlanır (ü → &#xFC;) ve kapsamlı CSS her öğeye b-xxxx özniteliği
    /// ekler; karşılaştırmayı okunur tutmak için ikisi de temizlenir. <paramref name="sonra"/> ilk çizimden sonra bileşende
    /// bir işlem yapıp yeniden çizdirir.
    /// </summary>
    public static async Task<string> CizAsync<T>(
        Dictionary<string, object?>? parametreler = null,
        Action<IServiceCollection>? servisler = null,
        Action<T>? sonra = null,
        Func<T, Task>? sonraAsync = null)
        where T : IComponent
    {
        var yakalayici = new BilesenYakalayici<T>();
        var koleksiyon = new ServiceCollection().AddLogging().AddSingleton<IComponentActivator>(yakalayici);
        servisler?.Invoke(koleksiyon);
        await using var services = koleksiyon.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var kok = await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parametreler ?? new Dictionary<string, object?>()));
            if (sonra != null || sonraAsync != null)
            {
                sonra?.Invoke(yakalayici.Bilesen!);
                if (sonraAsync != null)
                    await sonraAsync(yakalayici.Bilesen!);
                typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(yakalayici.Bilesen, null);
            }

            return Temizle(kok.ToHtmlString());
        });
    }

    public static string Temizle(string html)
        => Regex.Replace(WebUtility.HtmlDecode(html), @" b-[a-z0-9]+(?=[\s>/])", string.Empty);

    /// <summary>
    /// Sayfayı (InteractiveServer render modlu) statik çizer. HtmlRenderer render modlu bileşeni desteklemediği için
    /// önbellekteki render modu temizlenir; .NET iç yapısı değişirse test bilerek yüksek sesle düşer.
    /// </summary>
    public static Task<string> SayfaCizAsync<T>(IHoldingService servis) where T : IComponent
    {
        RenderModuKaldir(typeof(T));
        return CizAsync<T>(servisler: s => s.AddSingleton(servis).AddSingleton<IToastService, ToastService>());
    }

    private static void RenderModuKaldir(Type tip)
    {
        var fabrika = typeof(ComponentBase).Assembly.GetType("Microsoft.AspNetCore.Components.ComponentFactory")
            ?? throw new InvalidOperationException("ComponentFactory bulunamadı: .NET iç yapısı değişmiş olabilir.");
        var onbellekAlani = fabrika.GetField("_cachedComponentTypeInfo", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_cachedComponentTypeInfo alanı bulunamadı: .NET iç yapısı değişmiş olabilir.");
        var onbellek = (IDictionary)onbellekAlani.GetValue(null)!;

        object? girdi = onbellek.Contains(tip) ? onbellek[tip] : null;
        if (girdi == null)
        {
            var getir = fabrika.GetMethod("GetComponentTypeInfo", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            girdi = getir?.Invoke(null, [tip]) ?? (onbellek.Contains(tip) ? onbellek[tip] : null);
        }

        var alan = girdi?.GetType().GetField("<ComponentTypeRenderMode>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{tip.Name} için render modu alanı bulunamadı: .NET iç yapısı değişmiş olabilir.");
        alan.SetValue(girdi, null);
    }

    private sealed class BilesenYakalayici<T> : IComponentActivator where T : IComponent
    {
        public T? Bilesen;

        public IComponent CreateInstance(Type componentType)
        {
            var c = (IComponent)Activator.CreateInstance(componentType)!;
            if (c is T t)
                Bilesen = t;
            return c;
        }
    }

    /// <summary>Özel (private/protected) bir asenkron/senkron metodu çağırır.</summary>
    public static async Task CagirAsync(object hedef, string metot, params object?[] argumanlar)
    {
        var m = hedef.GetType().GetMethod(metot, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{hedef.GetType().Name}.{metot} bulunamadı");
        var sonuc = m.Invoke(hedef, argumanlar);
        if (sonuc is Task t)
            await t;
    }

    public static void Cagir(object hedef, string metot, params object?[] argumanlar)
        => (hedef.GetType().GetMethod(metot, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{hedef.GetType().Name}.{metot} bulunamadı"))
            .Invoke(hedef, argumanlar);

    /// <summary>HTML'de <paramref name="desen"/> kaç kez geçer.</summary>
    public static int Say(string html, string desen) => Regex.Matches(html, desen).Count;

    // ---- tohum ----

    public static HoldingVeri Satir(
        int firma, int yil, int ay, decimal gelir, decimal gider, decimal arac = 0m, decimal personel = 0m, decimal hakedis = 0m,
        decimal hakedisGider = 0m, int aktifArac = 0, int personelSayisi = 0, decimal? odenmemis = null, bool eski = false, string? ad = null) => new()
    {
        FirmaId = firma, FirmaKodu = "F" + firma, FirmaAdi = ad ?? (firma == 1 ? "Birinci Firma" : "İkinci Firma"),
        Yil = yil, Ay = ay, Kategori = "KARZARAR",
        ToplamGelir = gelir, ToplamGider = gider, Kar = gelir - gider, AraclarMaliyet = arac, PersonelMaliyet = personel,
        HakedisToplam = hakedis, AktifAracSayisi = aktifArac, PersonelSayisi = personelSayisi, OdenmemisFaturaToplam = odenmemis,
        JsonDetay = eski ? null : HoldingDetay.Yaz(new HoldingDetay { HakedisGider = hakedisGider }),
        OlusturmaTarihi = new DateTime(2026, 10, 21, 10, 20, 0, DateTimeKind.Utc)
    };

    public static HoldingVeri ButceSatiri(int firma, int yil, int ay, decimal hedef, decimal aylik, decimal kumulatif) => new()
    {
        FirmaId = firma, FirmaKodu = "F" + firma, FirmaAdi = firma == 1 ? "Birinci Firma" : "İkinci Firma", Yil = yil, Ay = ay,
        Kategori = "BUTCE", ButceHedef = hedef, ButceGerceklesen = aylik, ToplamGelir = kumulatif,
        OlusturmaTarihi = new DateTime(2026, 10, 21, 10, 20, 0, DateTimeKind.Utc)
    };

    /// <summary>İki firmalı, içinde bulunulan yılın ilk üç ayı dolu küçük örnek veri.</summary>
    public static SahteHoldingServisi OrnekServis()
    {
        var yil = BusinessTime.Today.Year;
        var s = new SahteHoldingServisi { SonDonem = new HoldingDonemSecimi(yil, 3) };

        // geçen yıl (karşılaştırma için)
        s.Kar.Add(Satir(1, yil - 1, 12, 900, 700, arac: 40, personel: 80, hakedis: 600, hakedisGider: 200, aktifArac: 9, personelSayisi: 6, odenmemis: 100));
        s.Kar.Add(Satir(2, yil - 1, 12, 300, 250, aktifArac: 3, personelSayisi: 2, odenmemis: 50));

        decimal[] g1 = [1000, 1100, 1200];
        decimal[] x1 = [700, 800, 1300];       // mart: zarar
        for (var i = 0; i < 3; i++)
        {
            s.Kar.Add(Satir(1, yil, i + 1, g1[i], x1[i], arac: 50, personel: 100, hakedis: 800, hakedisGider: 300, aktifArac: 10, personelSayisi: 7, odenmemis: x1[i] / 4));
            s.Kar.Add(Satir(2, yil, i + 1, 400, 300, arac: 20, personel: 40, hakedis: 200, hakedisGider: 50, aktifArac: 4, personelSayisi: 3, odenmemis: 60));
            s.Butce.Add(ButceSatiri(1, yil, i + 1, 12000, 800, 800 * (i + 1)));
            s.Butce.Add(ButceSatiri(2, yil, i + 1, 3000, 400, 400 * (i + 1)));
        }

        var bugun = BusinessTime.Today;
        s.Faturalar.Add(new HoldingVadeKalemi(1, "Birinci Firma", true, bugun.AddDays(-5), 500m));
        s.Faturalar.Add(new HoldingVadeKalemi(1, "Birinci Firma", true, bugun.AddDays(3), 300m));
        s.Faturalar.Add(new HoldingVadeKalemi(2, "İkinci Firma", false, bugun.AddDays(40), 900m));
        return s;
    }
}

/// <summary>Sahte holding servisi: anlık görüntüler ve açık faturalar bellekte; hata/çağrı gözlemi ayarlanabilir.</summary>
internal sealed class SahteHoldingServisi : IHoldingService
{
    public List<HoldingVeri> Kar { get; } = new();
    public List<HoldingVeri> Butce { get; } = new();
    public List<HoldingVadeKalemi> Faturalar { get; } = new();
    public HoldingDonemSecimi? SonDonem { get; set; }

    public bool KarHatasi { get; set; }
    public bool ButceHatasi { get; set; }
    public bool VadeHatasi { get; set; }
    public bool SonDonemHatasi { get; set; }
    public bool ToplamaHatasi { get; set; }

    public List<(int Yil, int Ay)> Toplamalar { get; } = new();

    public Task ToplaVeKaydetAsync(int yil, int ay)
    {
        if (ToplamaHatasi)
            throw new InvalidOperationException("sahte toplama hatası");
        Toplamalar.Add((yil, ay));
        return Task.CompletedTask;
    }

    public Task<List<HoldingVeri>> GetFirmaKarsilastirmaAsync(int yil, int? ay = null)
    {
        if (KarHatasi)
            throw new InvalidOperationException("sahte kâr/zarar hatası");
        return Task.FromResult(Kar.Where(v => v.Yil == yil && (ay == null || v.Ay == ay)).ToList());
    }

    public Task<List<HoldingVeri>> GetButceKonsolidasyonAsync(int yil)
    {
        if (ButceHatasi)
            throw new InvalidOperationException("sahte bütçe hatası");
        return Task.FromResult(Butce.Where(v => v.Yil == yil).ToList());
    }

    public Task<List<HoldingVeri>> GetAracMaliyetOzetiAsync(int yil, int? ay = null) => GetFirmaKarsilastirmaAsync(yil, ay);
    public Task<List<HoldingVeri>> GetPersonelGiderOzetiAsync(int yil, int? ay = null) => GetFirmaKarsilastirmaAsync(yil, ay);
    public Task<List<HoldingVeri>> GetHakedisOzetiAsync(int yil, int? ay = null) => GetFirmaKarsilastirmaAsync(yil, ay);
    public Task<List<HoldingRapor>> GetKayitliRaporlarAsync() => Task.FromResult(new List<HoldingRapor>());
    public Task<HoldingRapor> RaporKaydetAsync(HoldingRapor rapor) => Task.FromResult(rapor);

    public Task<HoldingDonemSecimi?> GetSonVeriDonemiAsync()
    {
        if (SonDonemHatasi)
            throw new InvalidOperationException("sahte son dönem hatası");
        return Task.FromResult(SonDonem);
    }

    public Task<List<HoldingVadeKalemi>> GetAcikFaturalarAsync()
    {
        if (VadeHatasi)
            throw new InvalidOperationException("sahte vade hatası");
        return Task.FromResult(Faturalar.ToList());
    }
}
