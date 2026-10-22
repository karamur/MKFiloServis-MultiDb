using System.Text.Json;
using System.Text.Json.Serialization;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>
/// <see cref="HoldingVeri.JsonDetay"/> alanında saklanan ek rakamlar (veritabanı şeması değişmeden).
/// </summary>
public sealed class HoldingDetay
{
    /// <summary>
    /// Dışarıya ödenen (tedarikçi + araç sahibi) onaylı hakediş tutarı.
    /// <see cref="HoldingVeri.HakedisToplam"/> ise kuruma kesilen (gelir) onaylı hakedişlerin toplamıdır.
    /// </summary>
    [JsonPropertyName("hakedisGider")]
    public decimal HakedisGider { get; set; }

    public static string Yaz(HoldingDetay detay) => JsonSerializer.Serialize(detay);

    /// <summary>Bozuk ya da boş metin sessizce null döner; eski satırlarda alan hiç yoktur.</summary>
    public static HoldingDetay? Oku(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<HoldingDetay>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>Seçilen yıl/ay (Ay = 0: tüm yıl).</summary>
public readonly record struct HoldingDonemSecimi(int Yil, int Ay);

/// <summary>Bir dönemin (ay ya da yıl) konsolide rakamları ve bunlardan türeyen oranlar. Firma özeti de bundan türer.</summary>
public class HoldingToplam
{
    public decimal Gelir { get; init; }
    public decimal Gider { get; init; }
    public decimal Kar { get; init; }
    public decimal AracMaliyet { get; init; }
    public decimal PersonelMaliyet { get; init; }

    /// <summary>Kuruma kesilen onaylı hakediş (gelir hakedişi).</summary>
    public decimal Hakedis { get; init; }

    /// <summary>Tedarikçi / araç sahibine ödenen onaylı hakediş.</summary>
    public decimal HakedisGider { get; init; }

    public decimal OdenmemisFatura { get; init; }

    /// <summary>Ödenmemiş fatura tutarı toplanmış mı (eski anlık görüntülerde alan boştur).</summary>
    public bool OdenmemisBilgisiVar { get; init; }

    public int AktifArac { get; init; }
    public int Personel { get; init; }
    public int FirmaSayisi { get; init; }

    /// <summary>Dönemde gelir/gider/maliyet hareketi olan firma sayısı.</summary>
    public int HareketliFirmaSayisi { get; init; }

    public decimal Marj => Gelir > 0m ? Kar / Gelir * 100m : 0m;
    public decimal GiderOrani => Gelir > 0m ? Gider / Gelir * 100m : 0m;
    public decimal AracMaliyetOrani => Gelir > 0m ? AracMaliyet / Gelir * 100m : 0m;
    public decimal PersonelMaliyetOrani => Gelir > 0m ? PersonelMaliyet / Gelir * 100m : 0m;
    public decimal HakedisOrani => Gelir > 0m ? Hakedis / Gelir * 100m : 0m;
    public decimal AracBasiMaliyet => AktifArac > 0 ? AracMaliyet / AktifArac : 0m;
    public decimal AracBasiGelir => AktifArac > 0 ? Gelir / AktifArac : 0m;
    public decimal KisiBasiMaliyet => Personel > 0 ? PersonelMaliyet / Personel : 0m;

    /// <summary>Giderin ödenen kısmı (%). Ödenmemiş tutar hiç toplanmadıysa bilinmez (null) — yanlışlıkla %100 gösterilmez.</summary>
    public decimal? OdenmeOrani => OdenmemisBilgisiVar && Gider > 0m
        ? Math.Clamp(100m - OdenmemisFatura / Gider * 100m, 0m, 100m)
        : null;

    /// <summary>Gelir, gider, maliyet ve hakedişin hepsi sıfır.</summary>
    public bool Bos => Gelir == 0m && Gider == 0m && AracMaliyet == 0m && PersonelMaliyet == 0m && Hakedis == 0m;
}

/// <summary>Bir firmanın seçilen dönemdeki rakamları.</summary>
public sealed class HoldingFirmaOzeti : HoldingToplam
{
    public int FirmaId { get; init; }
    public string FirmaKodu { get; init; } = string.Empty;
    public string FirmaAdi { get; init; } = string.Empty;

    /// <summary>Firmanın tüm sayfalarda aynı kalan renk sırası (firma numarasına göre).</summary>
    public int Sira { get; init; }

    /// <summary>Firma için seçilen dönemde en az bir anlık görüntü var.</summary>
    public bool VeriVar { get; init; }

    /// <summary>Holding gelirinin bu firmaya düşen payı (%).</summary>
    public decimal GelirPayi { get; internal set; }

    /// <summary>Holding giderinin payı (%).</summary>
    public decimal GiderPayi { get; internal set; }

    /// <summary>Karşılaştırma dönemindeki rakamlar (yoksa null).</summary>
    public HoldingToplam? Onceki { get; internal set; }
}

/// <summary>Seçilen yılın bir ayı için tüm firmaların toplamı.</summary>
public sealed record HoldingAyNoktasi(
    int Ay,
    bool VeriVar,
    decimal Gelir,
    decimal Gider,
    decimal Kar,
    decimal AracMaliyet,
    decimal PersonelMaliyet,
    decimal Hakedis,
    decimal HakedisGider,
    decimal OdenmemisFatura,
    int AktifArac,
    int Personel)
{
    public decimal Marj => Gelir > 0m ? Kar / Gelir * 100m : 0m;
    public decimal AracBasiMaliyet => AktifArac > 0 ? AracMaliyet / AktifArac : 0m;
    public decimal KisiBasiMaliyet => Personel > 0 ? PersonelMaliyet / Personel : 0m;
}

/// <summary>Kâr/zarar anlık görüntülerinden üretilen dönem analizi: toplamlar, firmalar, 12 aylık seri ve karşılaştırma.</summary>
public sealed class HoldingAnaliz
{
    private readonly IReadOnlyList<HoldingVeri> _yilSatirlari;

    internal HoldingAnaliz(IReadOnlyList<HoldingVeri> yilSatirlari)
    {
        _yilSatirlari = yilSatirlari;
    }

    public int Yil { get; init; }

    /// <summary>1–12: ay; 0: tüm yıl (verisi olan aylar).</summary>
    public int Ay { get; init; }

    public HoldingToplam Toplam { get; init; } = new();

    /// <summary>Karşılaştırma dönemi toplamı (karşılaştırılacak veri yoksa null).</summary>
    public HoldingToplam? Onceki { get; init; }

    /// <summary>"Mart 2026" ya da "2025 yılının aynı dönemi".</summary>
    public string OncekiEtiket { get; init; } = string.Empty;

    /// <summary>Gelire göre azalan sırada.</summary>
    public IReadOnlyList<HoldingFirmaOzeti> Firmalar { get; init; } = Array.Empty<HoldingFirmaOzeti>();

    /// <summary>Seçilen yılın 12 ayı (Ocak–Aralık); verisi olmayan ay <see cref="HoldingAyNoktasi.VeriVar"/> false.</summary>
    public IReadOnlyList<HoldingAyNoktasi> Aylar { get; init; } = Array.Empty<HoldingAyNoktasi>();

    /// <summary>Yılda anlık görüntüsü olan en son ay.</summary>
    public int? SonVeriAyi { get; init; }

    /// <summary>Seçilen dönemdeki anlık görüntülerin en yeni toplama zamanı (UTC).</summary>
    public DateTime? SonGuncellemeUtc { get; init; }

    /// <summary>Seçilen dönemde en az bir firmanın anlık görüntüsü var.</summary>
    public bool VeriVar => Firmalar.Any(f => f.VeriVar);

    /// <summary>Yılda verisi olan aylar.</summary>
    public IReadOnlyList<int> VeriliAylar => Aylar.Where(a => a.VeriVar).Select(a => a.Ay).ToList();

    /// <summary>
    /// Yılda eski toplayıcıyla toplanmış aylar. Güncel toplayıcı her satıra ek detay (<see cref="HoldingVeri.JsonDetay"/>) yazar;
    /// detayı boş olan satır, tüm firmaları kapsamayan/farklı dönem alanı kullanan eski yöntemden kalmıştır.
    /// </summary>
    public IReadOnlyList<int> EskiBicimliAylar
        => _yilSatirlari.Where(v => v.JsonDetay == null).Select(v => v.Ay).Distinct().OrderBy(a => a).ToList();

    /// <summary>Seçilen dönemde (ay ya da tüm yıl) yenilenmesi gereken eski biçimli aylar.</summary>
    public IReadOnlyList<int> DonemdekiEskiAylar
        => Ay == 0 ? EskiBicimliAylar : EskiBicimliAylar.Where(m => m == Ay).ToList();

    /// <summary>Bir firmanın 12 aylık serisi; firmanın o ay anlık görüntüsü yoksa null.</summary>
    public IReadOnlyList<decimal?> FirmaAylik(int firmaId, Func<HoldingVeri, decimal> secici)
    {
        var sonuc = new decimal?[12];
        foreach (var v in _yilSatirlari.Where(v => v.FirmaId == firmaId))
            sonuc[v.Ay - 1] = (sonuc[v.Ay - 1] ?? 0m) + secici(v);
        return sonuc;
    }

    /// <summary>Grafik kategorileri: 12 ay; seçili ay vurgulanır, verisi toplanmamış ay işaretlenir.</summary>
    public IReadOnlyList<HoldingKategori> Kategoriler()
        => Aylar.Select(m => new HoldingKategori(HoldingBicim.AyKisa(m.Ay), HoldingBicim.Donem(Yil, m.Ay), m.VeriVar, m.Ay == Ay)).ToList();

    /// <summary>Tüm firmaların 12 aylık toplam serisi; verisi toplanmamış ay null (grafikte boş kalır).</summary>
    public IReadOnlyList<decimal?> AylikSeri(Func<HoldingAyNoktasi, decimal> secici)
        => Aylar.Select(m => m.VeriVar ? secici(m) : (decimal?)null).ToList();

    /// <summary>Önceki döneme göre değişim rozeti; karşılaştırılacak dönem yoksa rozet yok.</summary>
    public HoldingRozet Rozet(Func<HoldingToplam, decimal> secici, bool yuksekIyi)
        => Onceki is null
            ? HoldingRozet.Yok
            : HoldingRozet.Degisim(OzetDegisim.Hesapla(secici(Toplam), secici(Onceki)), yuksekIyi, $"{OncekiEtiket} ile karşılaştırma");

    /// <summary>"Mart 2026: 11,7 Mn TL" gibi karşılaştırma dönemindeki değer; dönem yoksa null.</summary>
    public string? OncekiMetni(Func<HoldingToplam, decimal> secici, Func<decimal, string> bicim)
        => Onceki is null ? null : $"{OncekiEtiket}: {bicim(secici(Onceki))}";
}

public enum HoldingBulguOnemi
{
    Kritik = 0,
    Uyari = 1,
    Bilgi = 2,
    Iyi = 3
}

/// <summary>Yöneticinin dikkatini çekecek tek bir tespit.</summary>
public sealed record HoldingBulgu(HoldingBulguOnemi Onem, string Baslik, string Metin, string? Link = null);
