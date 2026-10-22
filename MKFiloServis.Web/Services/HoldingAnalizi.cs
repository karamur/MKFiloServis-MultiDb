using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Holding ekranlarının saf hesapları: kâr/zarar anlık görüntülerinden dönem toplamı, firma özetleri, 12 aylık seri,
/// önceki dönemle karşılaştırma, yönetici cümleleri ve dikkat gerektirenler. Veritabanına dokunmaz.
/// </summary>
public static class HoldingAnalizi
{
    /// <summary>Kâr marjı bu yüzdenin altındaki firmalar "düşük marj" uyarısı alır.</summary>
    public const decimal DusukMarj = 10m;

    /// <summary>Gelir önceki döneme göre en az bu kadar (%) azalırsa uyarılır.</summary>
    public const int GelirDususEsigi = 15;

    /// <summary>Gider en az bu kadar (%) artar ve gelirden hızlı artarsa uyarılır.</summary>
    public const int GiderArtisEsigi = 20;

    /// <summary>Giderlerin en az bu kadarı (%) ödenmemişse uyarılır.</summary>
    public const decimal OdenmemisOranEsigi = 50m;

    private const int AzamiBulgu = 8;

    public static IReadOnlyList<HoldingVeri> KarZarar(IEnumerable<HoldingVeri> satirlar)
        => satirlar.Where(v => v.Kategori == "KARZARAR" && v.Ay is >= 1 and <= 12).ToList();

    /// <param name="yilSatirlari">Seçilen yılın "KARZARAR" anlık görüntüleri (tüm aylar, tüm firmalar).</param>
    /// <param name="oncekiYilSatirlari">Bir önceki yılın anlık görüntüleri (ocak ayı ve "tüm yıl" karşılaştırması için).</param>
    /// <param name="yil">Seçilen yıl.</param>
    /// <param name="ay">1–12: ay; 0: yılın verisi olan tüm ayları (yıl başından son verili aya).</param>
    public static HoldingAnaliz Olustur(IEnumerable<HoldingVeri> yilSatirlari, IEnumerable<HoldingVeri> oncekiYilSatirlari, int yil, int ay)
    {
        if (ay is < 0 or > 12)
            ay = 0;

        var satirlar = KarZarar(yilSatirlari).Where(v => v.Yil == yil).ToList();
        var oncekiYil = KarZarar(oncekiYilSatirlari).Where(v => v.Yil == yil - 1).ToList();
        var sonAy = satirlar.Count == 0 ? (int?)null : satirlar.Max(v => v.Ay);
        var secili = ay == 0 ? satirlar : satirlar.Where(v => v.Ay == ay).ToList();

        List<HoldingVeri> karsilastirma;
        string oncekiEtiket;
        if (ay == 0)
        {
            // Aynı dönem: geçen yılın, bu yıl verisi olan son aya kadarki kısmı.
            var kesim = sonAy ?? 12;
            karsilastirma = oncekiYil.Where(v => v.Ay <= kesim).ToList();
            oncekiEtiket = $"{yil - 1} yılının aynı dönemi";
        }
        else if (ay > 1)
        {
            karsilastirma = satirlar.Where(v => v.Ay == ay - 1).ToList();
            oncekiEtiket = HoldingBicim.Donem(yil, ay - 1);
        }
        else
        {
            karsilastirma = oncekiYil.Where(v => v.Ay == 12).ToList();
            oncekiEtiket = HoldingBicim.Donem(yil - 1, 12);
        }

        // Firma kimlikleri ve renk sırası: tüm sayfalarda aynı firma aynı renkte görünür.
        var tumu = satirlar.Concat(oncekiYil).ToList();
        var siraBul = tumu.Select(v => v.FirmaId).Distinct().OrderBy(id => id)
            .Select((id, i) => (id, i))
            .ToDictionary(x => x.id, x => x.i);
        var bilgi = tumu.GroupBy(v => v.FirmaId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.Yil).ThenByDescending(v => v.Ay).First());

        var firmaIdleri = satirlar.Select(v => v.FirmaId).Union(karsilastirma.Select(v => v.FirmaId)).Distinct().ToList();
        var firmalar = new List<HoldingFirmaOzeti>();
        foreach (var id in firmaIdleri)
        {
            var ozet = FirmaOzetiOlustur(id, bilgi[id], siraBul[id], secili.Where(v => v.FirmaId == id).ToList());
            var oncekiFirma = karsilastirma.Where(v => v.FirmaId == id).ToList();
            ozet.Onceki = oncekiFirma.Count == 0 ? null : FirmaOzetiOlustur(id, bilgi[id], siraBul[id], oncekiFirma);
            firmalar.Add(ozet);
        }

        var toplam = Birlestir(firmalar);
        foreach (var f in firmalar)
        {
            f.GelirPayi = toplam.Gelir > 0m ? f.Gelir / toplam.Gelir * 100m : 0m;
            f.GiderPayi = toplam.Gider > 0m ? f.Gider / toplam.Gider * 100m : 0m;
        }

        var oncekiToplam = karsilastirma.Count == 0
            ? null
            : Birlestir(karsilastirma.GroupBy(v => v.FirmaId)
                .Select(g => (HoldingToplam)FirmaOzetiOlustur(g.Key, bilgi[g.Key], siraBul[g.Key], g.ToList()))
                .ToList());

        var aylar = Enumerable.Range(1, 12).Select(a =>
        {
            var r = satirlar.Where(v => v.Ay == a).ToList();
            return new HoldingAyNoktasi(
                a,
                r.Count > 0,
                r.Sum(v => v.ToplamGelir),
                r.Sum(v => v.ToplamGider),
                r.Sum(v => v.Kar),
                r.Sum(v => v.AraclarMaliyet ?? 0m),
                r.Sum(v => v.PersonelMaliyet ?? 0m),
                r.Sum(v => v.HakedisToplam ?? 0m),
                r.Sum(v => HoldingDetay.Oku(v.JsonDetay)?.HakedisGider ?? 0m),
                r.Sum(v => v.OdenmemisFaturaToplam ?? 0m),
                r.Sum(v => v.AktifAracSayisi ?? 0),
                r.Sum(v => v.PersonelSayisi ?? 0));
        }).ToList();

        return new HoldingAnaliz(satirlar)
        {
            Yil = yil,
            Ay = ay,
            Toplam = toplam,
            Onceki = oncekiToplam,
            OncekiEtiket = oncekiEtiket,
            Firmalar = firmalar
                .OrderByDescending(f => f.Gelir)
                .ThenByDescending(f => f.Kar)
                .ThenBy(f => f.FirmaAdi, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            Aylar = aylar,
            SonVeriAyi = sonAy,
            SonGuncellemeUtc = secili.Count == 0 ? null : secili.Max(v => v.OlusturmaTarihi)
        };
    }

    private static HoldingFirmaOzeti FirmaOzetiOlustur(int firmaId, HoldingVeri bilgi, int sira, IReadOnlyCollection<HoldingVeri> satirlar)
    {
        // Araç/personel adedi aylara bölünmez; bir firmanın en son ayındaki anlık görüntüden alınır.
        var son = satirlar.OrderByDescending(v => v.Ay).FirstOrDefault();
        var hareketli = satirlar.Any(v =>
            v.ToplamGelir != 0m || v.ToplamGider != 0m || (v.AraclarMaliyet ?? 0m) != 0m
            || (v.PersonelMaliyet ?? 0m) != 0m || (v.HakedisToplam ?? 0m) != 0m);

        return new HoldingFirmaOzeti
        {
            FirmaId = firmaId,
            FirmaKodu = bilgi.FirmaKodu,
            FirmaAdi = bilgi.FirmaAdi,
            Sira = sira,
            VeriVar = satirlar.Count > 0,
            Gelir = satirlar.Sum(v => v.ToplamGelir),
            Gider = satirlar.Sum(v => v.ToplamGider),
            Kar = satirlar.Sum(v => v.Kar),
            AracMaliyet = satirlar.Sum(v => v.AraclarMaliyet ?? 0m),
            PersonelMaliyet = satirlar.Sum(v => v.PersonelMaliyet ?? 0m),
            Hakedis = satirlar.Sum(v => v.HakedisToplam ?? 0m),
            HakedisGider = satirlar.Sum(v => HoldingDetay.Oku(v.JsonDetay)?.HakedisGider ?? 0m),
            OdenmemisFatura = satirlar.Sum(v => v.OdenmemisFaturaToplam ?? 0m),
            OdenmemisBilgisiVar = satirlar.Any(v => v.OdenmemisFaturaToplam.HasValue),
            AktifArac = son?.AktifAracSayisi ?? 0,
            Personel = son?.PersonelSayisi ?? 0,
            FirmaSayisi = 1,
            HareketliFirmaSayisi = hareketli ? 1 : 0
        };
    }

    private static HoldingToplam Birlestir(IReadOnlyCollection<HoldingToplam> parcalar) => new()
    {
        Gelir = parcalar.Sum(p => p.Gelir),
        Gider = parcalar.Sum(p => p.Gider),
        Kar = parcalar.Sum(p => p.Kar),
        AracMaliyet = parcalar.Sum(p => p.AracMaliyet),
        PersonelMaliyet = parcalar.Sum(p => p.PersonelMaliyet),
        Hakedis = parcalar.Sum(p => p.Hakedis),
        HakedisGider = parcalar.Sum(p => p.HakedisGider),
        OdenmemisFatura = parcalar.Sum(p => p.OdenmemisFatura),
        OdenmemisBilgisiVar = parcalar.Any(p => p.OdenmemisBilgisiVar),
        AktifArac = parcalar.Sum(p => p.AktifArac),
        Personel = parcalar.Sum(p => p.Personel),
        FirmaSayisi = parcalar.Count,
        HareketliFirmaSayisi = parcalar.Count(p => !p.Bos)
    };

    /// <summary>
    /// Seçili ayın bulunmadığı ya da boş kaldığı durumlarda açılış dönemi: yılda verisi olan en son ay.
    /// Hiç veri yoksa içinde bulunulan ay.
    /// </summary>
    public static HoldingDonemSecimi AcilisDonemi(HoldingDonemSecimi? sonVeri, DateTime bugun)
        => sonVeri ?? new HoldingDonemSecimi(bugun.Year, bugun.Month);

    // ---- yönetici özeti ----

    /// <summary>Dönemi birkaç düz cümleyle anlatır ("Nisan 2026'da 3 firmanın toplam geliri…").</summary>
    public static IReadOnlyList<string> OzetCumleleri(HoldingAnaliz a, HoldingButceOzeti? butce = null)
    {
        var donem = HoldingBicim.Donem(a.Yil, a.Ay);
        if (!a.VeriVar)
            return [$"{donem} için henüz holding verisi toplanmamış."];

        var t = a.Toplam;
        var cumleler = new List<string>();

        var sonucMetni = t.Kar >= 0m ? "net kâr" : "net zarar";
        cumleler.Add(
            $"{donem}: {t.FirmaSayisi} firmanın toplam geliri {HoldingBicim.ParaKisa(t.Gelir)}, gideri {HoldingBicim.ParaKisa(t.Gider)}; " +
            $"{sonucMetni} {HoldingBicim.ParaKisa(Math.Abs(t.Kar))}" + (t.Gelir > 0m && t.Kar >= 0m ? $" (kâr marjı {HoldingBicim.Yuzde(t.Marj)})." : "."));

        if (a.Onceki is { } o && !o.Bos)
        {
            var gelir = OzetDegisim.Hesapla(t.Gelir, o.Gelir);
            var kar = OzetDegisim.Hesapla(t.Kar, o.Kar);
            var parcalar = new List<string>();
            if (DegisimMetni("Gelir", gelir) is { } g) parcalar.Add(g);
            if (DegisimMetni("Net kâr", kar) is { } k) parcalar.Add(k);
            if (parcalar.Count > 0)
                cumleler.Add($"{a.OncekiEtiket} ile karşılaştırınca: {string.Join("; ", parcalar)}.");
        }

        var hareketli = a.Firmalar.Where(f => f.VeriVar && !f.Bos).ToList();
        if (hareketli.Count > 0)
        {
            var enKarli = hareketli.OrderByDescending(f => f.Kar).First();
            var enBuyuk = hareketli.OrderByDescending(f => f.Gelir).First();
            if (hareketli.Count > 1)
            {
                cumleler.Add(
                    $"En yüksek kâr: {enKarli.FirmaAdi} ({HoldingBicim.ParaKisa(enKarli.Kar)}); " +
                    $"en büyük gelir: {enBuyuk.FirmaAdi} ({HoldingBicim.ParaKisa(enBuyuk.Gelir)}, holding gelirinin {HoldingBicim.Yuzde(enBuyuk.GelirPayi, 0)}).");
            }
        }

        if (t.AracMaliyet > 0m || t.PersonelMaliyet > 0m || t.AktifArac > 0 || t.Personel > 0)
        {
            cumleler.Add(
                $"Araç maliyeti {HoldingBicim.ParaKisa(t.AracMaliyet)}, personel maliyeti {HoldingBicim.ParaKisa(t.PersonelMaliyet)}; " +
                $"{HoldingBicim.Sayi(t.AktifArac)} aktif araç, {HoldingBicim.Sayi(t.Personel)} personel.");
        }

        if (butce is { VeriVar: true, HedefVar: true })
        {
            cumleler.Add(
                $"Bütçe: yıllık harcama hedefinin {HoldingBicim.Yuzde(butce.Kullanim, 0)} kadarı kullanıldı " +
                $"({HoldingBicim.Donem(butce.Yil, butce.Ay)} sonunda beklenen {HoldingBicim.Yuzde(butce.BeklenenYuzde, 0)}).");
        }

        return cumleler;
    }

    private static string? DegisimMetni(string ad, OzetDegisim d) => d.Yon switch
    {
        DegisimYonu.Artti => $"{ad} %{d.Yuzde} arttı",
        DegisimYonu.Azaldi => $"{ad} %{d.Yuzde} azaldı",
        DegisimYonu.Ayni => $"{ad} değişmedi",
        _ => null
    };

    // ---- dikkat gerektirenler ----

    /// <summary>
    /// Yöneticinin bakması gereken durumlar, önem sırasıyla (kritik → uyarı → bilgi → iyi haber).
    /// Eşikler bu sınıfın sabitleridir; hiçbiri bir iş hedefi değil, "bakmaya değer" ölçütüdür.
    /// </summary>
    public static IReadOnlyList<HoldingBulgu> Bulgular(HoldingAnaliz a, HoldingButceOzeti? butce = null, HoldingVadeOzeti? vade = null)
    {
        var liste = new List<HoldingBulgu>();
        var donem = HoldingBicim.Donem(a.Yil, a.Ay);

        if (!a.VeriVar)
        {
            liste.Add(new HoldingBulgu(HoldingBulguOnemi.Bilgi, "Bu dönem için veri yok",
                $"{donem} için holding verisi toplanmamış. \"Güncelle\" düğmesiyle bu dönemin verisini toplayabilirsiniz."));
        }
        else
        {
            var t = a.Toplam;

            if (t.Kar < 0m)
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Kritik, "Holding zararda",
                    $"{donem}: toplam net zarar {HoldingBicim.ParaKisa(-t.Kar)} (gelir {HoldingBicim.ParaKisa(t.Gelir)}, gider {HoldingBicim.ParaKisa(t.Gider)}).",
                    "/holding/karsilastirma"));
            }

            foreach (var f in a.Firmalar.Where(f => f.VeriVar && f.Kar < 0m))
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Kritik, $"{f.FirmaAdi}: zarar",
                    $"Net zarar {HoldingBicim.ParaKisa(-f.Kar)}; gelir {HoldingBicim.ParaKisa(f.Gelir)}, gider {HoldingBicim.ParaKisa(f.Gider)}.",
                    "/holding/karsilastirma"));
            }

            foreach (var f in a.Firmalar.Where(f => f.VeriVar && f.Gelir > 0m && f.Kar >= 0m && f.Marj < DusukMarj))
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Uyari, $"{f.FirmaAdi}: düşük kâr marjı",
                    $"Kâr marjı {HoldingBicim.Yuzde(f.Marj)}; gelirin {HoldingBicim.Yuzde(f.GiderOrani, 0)} kadarı giderle gidiyor.",
                    "/holding/karsilastirma"));
            }

            if (a.Onceki is { Bos: false } o)
            {
                var gelir = OzetDegisim.Hesapla(t.Gelir, o.Gelir);
                var gider = OzetDegisim.Hesapla(t.Gider, o.Gider);
                var kar = OzetDegisim.Hesapla(t.Kar, o.Kar);

                if (gelir.Yon == DegisimYonu.Azaldi && gelir.Yuzde >= GelirDususEsigi)
                {
                    liste.Add(new HoldingBulgu(HoldingBulguOnemi.Uyari, "Gelir düştü",
                        $"Toplam gelir {a.OncekiEtiket} dönemine göre %{gelir.Yuzde} azaldı ({HoldingBicim.ParaKisa(o.Gelir)} → {HoldingBicim.ParaKisa(t.Gelir)}).",
                        "/holding/karsilastirma"));
                }

                var gelirArtisi = gelir.Yon == DegisimYonu.Artti ? gelir.Yuzde : 0;
                if (gider.Yon == DegisimYonu.Artti && gider.Yuzde >= GiderArtisEsigi && gider.Yuzde > gelirArtisi)
                {
                    liste.Add(new HoldingBulgu(HoldingBulguOnemi.Uyari, "Gider hızlı arttı",
                        $"Toplam gider {a.OncekiEtiket} dönemine göre %{gider.Yuzde} arttı ({HoldingBicim.ParaKisa(o.Gider)} → {HoldingBicim.ParaKisa(t.Gider)}).",
                        "/holding/karsilastirma"));
                }

                if (kar.Yon == DegisimYonu.Artti && kar.Yuzde >= 10 && t.Kar > 0m)
                {
                    liste.Add(new HoldingBulgu(HoldingBulguOnemi.Iyi, "Kâr arttı",
                        $"Net kâr {a.OncekiEtiket} dönemine göre %{kar.Yuzde} arttı ({HoldingBicim.ParaKisa(o.Kar)} → {HoldingBicim.ParaKisa(t.Kar)})."));
                }
            }

            if (t.OdenmeOrani is { } odenme && 100m - odenme >= OdenmemisOranEsigi)
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Uyari, "Giderlerin büyük kısmı ödenmemiş",
                    $"Dönem giderinin {HoldingBicim.Yuzde(100m - odenme, 0)} kadarı ({HoldingBicim.ParaKisa(t.OdenmemisFatura)}) henüz ödenmemiş.",
                    "/holding/odemeler"));
            }

            foreach (var f in a.Firmalar.Where(f => f.VeriVar && f.Bos))
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Bilgi, $"{f.FirmaAdi}: kayıt yok",
                    "Bu dönemde fatura, araç masrafı, maaş ya da hakediş kaydı bulunmuyor."));
            }

            foreach (var f in a.Firmalar.Where(f => !f.VeriVar))
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Bilgi, $"{f.FirmaAdi}: veri toplanmamış",
                    $"{donem} için bu firmanın verisi henüz toplanmamış."));
            }

            var eksik = EksikAylar(a);
            if (eksik.Count > 0)
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Bilgi, "Toplanmamış aylar var",
                    $"{string.Join(", ", eksik.Select(HoldingBicim.AyAdi))} ayının verisi toplanmamış; aylık grafiklerde boş görünür."));
            }
        }

        if (butce is { VeriVar: true, HedefVar: true })
        {
            if (butce.Durum == HoldingButceDurumu.Asim)
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Kritik, "Bütçe aşıldı",
                    $"Yıllık bütçe kullanımı {HoldingBicim.Yuzde(butce.Kullanim)} ({HoldingBicim.ParaKisa(butce.Gerceklesen)} / {HoldingBicim.ParaKisa(butce.Hedef)}).",
                    "/holding/butce"));
            }
            else if (butce.Durum == HoldingButceDurumu.Hizli)
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Uyari, "Bütçe beklenenden hızlı harcanıyor",
                    $"Kullanım {HoldingBicim.Yuzde(butce.Kullanim)}; {HoldingBicim.AyAdi(butce.Ay)} sonu için beklenen {HoldingBicim.Yuzde(butce.BeklenenYuzde)}.",
                    "/holding/butce"));
            }

            foreach (var f in butce.Firmalar.Where(f => f.Durum == HoldingButceDurumu.Asim))
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Kritik, $"{f.FirmaAdi}: bütçe aşımı",
                    $"Kullanım {HoldingBicim.Yuzde(f.Kullanim)}; hedefin {HoldingBicim.ParaKisa(-f.Kalan)} üzerinde harcandı.",
                    "/holding/butce"));
            }
        }

        if (vade is { Bos: false })
        {
            if (vade.Borc.Gecikmis > 0m)
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Kritik, "Vadesi geçmiş ödemeler",
                    $"{vade.Borc.GecikmisAdet} faturada toplam {HoldingBicim.ParaKisa(vade.Borc.Gecikmis)} ödeme gecikmiş.",
                    "/holding/odemeler"));
            }

            if (vade.Borc.YediGunIcinde > 0m)
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Bilgi, "Yaklaşan ödemeler",
                    $"Önümüzdeki 7 gün içinde {HoldingBicim.ParaKisa(vade.Borc.YediGunIcinde)} ödeme vadesi geliyor.",
                    "/holding/odemeler"));
            }

            if (vade.Alacak.Gecikmis > 0m)
            {
                liste.Add(new HoldingBulgu(HoldingBulguOnemi.Uyari, "Vadesi geçmiş alacaklar",
                    $"{vade.Alacak.GecikmisAdet} faturada toplam {HoldingBicim.ParaKisa(vade.Alacak.Gecikmis)} tahsilat gecikmiş.",
                    "/holding/odemeler"));
            }
        }

        return liste
            .OrderBy(b => b.Onem)
            .Take(AzamiBulgu)
            .ToList();
    }

    /// <summary>İlk ve son verili ay arasında anlık görüntüsü olmayan aylar.</summary>
    public static IReadOnlyList<int> EksikAylar(HoldingAnaliz a)
    {
        var verili = a.VeriliAylar;
        if (verili.Count < 2)
            return Array.Empty<int>();

        var ilk = verili.Min();
        var son = verili.Max();
        return Enumerable.Range(ilk, son - ilk + 1).Where(m => !verili.Contains(m)).ToList();
    }
}
