using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

public enum HoldingButceDurumu
{
    /// <summary>Firmanın bu yıl için bütçe hedefi tanımlı değil.</summary>
    HedefYok = 0,

    /// <summary>Harcama, geçen süreye göre beklenen düzeyde ya da altında.</summary>
    Normal = 1,

    /// <summary>Henüz aşılmadı ama harcama geçen süreden belirgin hızlı.</summary>
    Hizli = 2,

    /// <summary>Yıllık hedef aşıldı.</summary>
    Asim = 3
}

/// <summary>Bir firmanın bütçesi: yıllık hedef, seçilen aya kadar gerçekleşen ve aylık dağılım.</summary>
public sealed class HoldingButceFirma
{
    public int FirmaId { get; init; }
    public string FirmaAdi { get; init; } = string.Empty;
    public int Sira { get; init; }

    /// <summary>Anlık görüntüsü seçilen aya kadar olan aylarda var.</summary>
    public bool VeriVar { get; init; }

    public decimal Hedef { get; init; }

    /// <summary>Yıl başından seçilen aya kadar ödenen toplam.</summary>
    public decimal Gerceklesen { get; init; }

    /// <summary>Aylık ödenen tutar (Ocak–Aralık); anlık görüntüsü olmayan ay null.</summary>
    public IReadOnlyList<decimal?> Aylik { get; init; } = new decimal?[12];

    public HoldingButceDurumu Durum { get; init; }

    public decimal Kalan => Hedef - Gerceklesen;

    /// <summary>Hedefin yüzde kaçı harcandı (hedef yoksa 0).</summary>
    public decimal Kullanim => Hedef > 0m ? Gerceklesen / Hedef * 100m : 0m;
}

/// <summary>
/// Holding bütçe konsolidasyonu. Bütçe modülündeki gibi hedef bir harcama sınırıdır: kullanım %100'ü aşarsa
/// bütçe aşılmıştır. "Beklenen" kullanım, hedefin geçen aya orantılı payıdır (Mart sonu = %25).
/// </summary>
public sealed class HoldingButceOzeti
{
    public int Yil { get; init; }

    /// <summary>Gerçekleşenin hesaplandığı ay (1–12).</summary>
    public int Ay { get; init; }

    public IReadOnlyList<HoldingButceFirma> Firmalar { get; init; } = Array.Empty<HoldingButceFirma>();
    public decimal Hedef { get; init; }
    public decimal Gerceklesen { get; init; }

    /// <summary>12 aylık toplam ödeme (hiçbir firmada anlık görüntüsü olmayan ay null).</summary>
    public IReadOnlyList<decimal?> Aylik { get; init; } = new decimal?[12];

    public decimal BeklenenYuzde { get; init; }
    public HoldingButceDurumu Durum { get; init; }

    /// <summary>Bütçe anlık görüntüsü hiç yok.</summary>
    public bool VeriVar => Firmalar.Any(f => f.VeriVar);

    /// <summary>Anlık görüntüsü toplanmış aylar (1–12).</summary>
    public IReadOnlyList<int> VeriliAylar => Enumerable.Range(1, 12).Where(m => Aylik[m - 1].HasValue).ToList();

    public bool HedefVar => Hedef > 0m;
    public decimal Kalan => Hedef - Gerceklesen;
    public decimal Kullanim => Hedef > 0m ? Gerceklesen / Hedef * 100m : 0m;

    /// <summary>Hedefi tanımlı firmaların aylık eşit payı (grafikte "beklenen seviye" çizgisi).</summary>
    public decimal AylikBeklenen => Hedef / 12m;
}

public static class HoldingButceAnalizi
{
    /// <summary>Harcama beklenen düzeyin bu kadar puan üstüne çıkınca "hızlı" uyarısı verilir.</summary>
    public const decimal HizliFarki = 10m;

    /// <param name="butceSatirlari"><c>Kategori = "BUTCE"</c> anlık görüntüleri (yılın tüm ayları).</param>
    /// <param name="yil">Bütçe yılı.</param>
    /// <param name="ay">1–12: yıl başından bu aya kadar gerçekleşen hesaplanır.</param>
    public static HoldingButceOzeti Olustur(IEnumerable<HoldingVeri> butceSatirlari, int yil, int ay)
    {
        ay = Math.Clamp(ay, 1, 12);
        var satirlar = butceSatirlari
            .Where(v => v.Kategori == "BUTCE" && v.Yil == yil && v.Ay is >= 1 and <= 12)
            .ToList();

        var beklenen = ay / 12m * 100m;
        var firmaIdleri = satirlar.Select(v => v.FirmaId).Distinct().OrderBy(id => id).ToList();

        var firmalar = new List<HoldingButceFirma>();
        for (var sira = 0; sira < firmaIdleri.Count; sira++)
        {
            var id = firmaIdleri[sira];
            var firmaSatirlari = satirlar.Where(v => v.FirmaId == id).ToList();
            var ad = firmaSatirlari.OrderByDescending(v => v.Ay).First().FirmaAdi;

            // Yıllık hedef her aylık satırda aynıdır; en güncel satırdan alınır.
            var hedef = firmaSatirlari.OrderByDescending(v => v.Ay).Select(v => v.ButceHedef ?? 0m).FirstOrDefault();

            // Kümülatif tutar (ToplamGelir alanı) o ayın satırında Ocak'tan itibaren hesaplanmıştır; seçilen ay toplanmamışsa
            // en yakın önceki ayınki kullanılır.
            var referans = firmaSatirlari.Where(v => v.Ay <= ay).OrderByDescending(v => v.Ay).FirstOrDefault();
            var gerceklesen = referans?.ToplamGelir ?? 0m;

            var aylik = new decimal?[12];
            foreach (var s in firmaSatirlari)
                aylik[s.Ay - 1] = s.ButceGerceklesen ?? 0m;

            firmalar.Add(new HoldingButceFirma
            {
                FirmaId = id,
                FirmaAdi = ad,
                Sira = sira,
                VeriVar = referans != null,
                Hedef = hedef,
                Gerceklesen = gerceklesen,
                Aylik = aylik,
                Durum = DurumBul(hedef, gerceklesen, beklenen)
            });
        }

        var toplamHedef = firmalar.Sum(f => f.Hedef);
        var toplamGerceklesen = firmalar.Sum(f => f.Gerceklesen);
        var aylikToplam = new decimal?[12];
        for (var i = 0; i < 12; i++)
        {
            var degerler = firmalar.Select(f => f.Aylik[i]).Where(d => d.HasValue).ToList();
            aylikToplam[i] = degerler.Count == 0 ? null : degerler.Sum();
        }

        return new HoldingButceOzeti
        {
            Yil = yil,
            Ay = ay,
            Firmalar = firmalar.OrderByDescending(f => f.Hedef).ThenBy(f => f.FirmaAdi, StringComparer.CurrentCultureIgnoreCase).ToList(),
            Hedef = toplamHedef,
            Gerceklesen = toplamGerceklesen,
            Aylik = aylikToplam,
            BeklenenYuzde = beklenen,
            Durum = DurumBul(toplamHedef, toplamGerceklesen, beklenen)
        };
    }

    public static HoldingButceDurumu DurumBul(decimal hedef, decimal gerceklesen, decimal beklenenYuzde)
    {
        if (hedef <= 0m)
            return HoldingButceDurumu.HedefYok;

        var kullanim = gerceklesen / hedef * 100m;
        if (kullanim > 100m)
            return HoldingButceDurumu.Asim;

        return kullanim > beklenenYuzde + HizliFarki ? HoldingButceDurumu.Hizli : HoldingButceDurumu.Normal;
    }
}
