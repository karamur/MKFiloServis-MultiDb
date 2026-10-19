using System.Globalization;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public enum DegisimYonu
{
    /// <summary>İki dönemde de değer yok.</summary>
    Yok = 0,
    Ayni = 1,
    Artti = 2,
    Azaldi = 3,

    /// <summary>Önceki dönemde değer yoktu, şimdi var.</summary>
    Yeni = 4
}

/// <summary>Önceki döneme göre değişim (yüzde, tam sayıya yuvarlanır).</summary>
public readonly record struct OzetDegisim(DegisimYonu Yon, int Yuzde)
{
    public static OzetDegisim Hesapla(decimal simdi, decimal onceki)
    {
        if (simdi == 0 && onceki == 0)
            return new OzetDegisim(DegisimYonu.Yok, 0);
        if (onceki == 0)
            return new OzetDegisim(DegisimYonu.Yeni, 0);
        if (simdi == onceki)
            return new OzetDegisim(DegisimYonu.Ayni, 0);

        var yuzde = (int)Math.Round((simdi - onceki) / Math.Abs(onceki) * 100m, 0, MidpointRounding.AwayFromZero);
        if (yuzde == 0)
            return new OzetDegisim(DegisimYonu.Ayni, 0);
        return new OzetDegisim(yuzde > 0 ? DegisimYonu.Artti : DegisimYonu.Azaldi, Math.Abs(yuzde));
    }
}

/// <summary>
/// Yönetici özeti raporundan okunaklı Türkçe cümleler üretir ("Bu ay 52 sefer yapıldı…").
/// Saf metin üretimi; veritabanına dokunmaz.
/// </summary>
public static class YoneticiOzetMetni
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public const string BosMesaj = "Seçilen dönemde kayıtlı bir iş bulunmuyor.";

    /// <summary>Hiçbir bölümde dönem içi hareket yok mu.</summary>
    public static bool Bos(YoneticiOzetRapor r)
        => (r.Servis?.Bos ?? true)
           && (r.Hakedis?.Bos ?? true)
           && (r.Fatura?.Bos ?? true)
           && (r.Kasa?.Bos ?? true)
           && (r.Butce?.Bos ?? true)
           && (r.Personel?.Bos ?? true)
           && (r.Arac?.Bos ?? true)
           && (r.Kayitlar?.Bos ?? true)
           && (r.RentACar?.Bos ?? true)
           && (r.Ekip?.Bos ?? true);

    public static IReadOnlyList<string> Olustur(YoneticiOzetRapor r)
    {
        var giris = r.Donem.CumleGirisi;
        var cumleler = new List<string>();

        if (r.Servis is { } s)
        {
            if (s.YapilanSefer > 0)
            {
                var cumle = $"{giris} {Sayi(s.YapilanSefer)} sefer yapıldı";
                if (s.CalisanAracSayisi > 0)
                    cumle += $" ({s.CalisanAracSayisi} araç)";
                if (s.KurumTahakkuku > 0)
                    cumle += $"; kurum tahakkuku {ParaFormat.FormatKisa(s.KurumTahakkuku)}";
                cumleler.Add(cumle + ".");
            }
            else if (s.PlanliSefer > 0)
            {
                cumleler.Add($"{giris} henüz tamamlanan sefer yok; {Sayi(s.PlanliSefer)} sefer planlı.");
            }
        }

        if (r.Hakedis is { UretilenAdet: > 0 } h)
        {
            var cumle = $"{giris} {h.UretilenAdet} hakediş hazırlandı";
            var parcalar = new List<string>();
            if (h.GelirTutari > 0)
                parcalar.Add($"{ParaFormat.FormatKisa(h.GelirTutari)} gelir");
            if (h.GiderTutari > 0)
                parcalar.Add($"{ParaFormat.FormatKisa(h.GiderTutari)} gider");
            if (parcalar.Count > 0)
                cumle += $" ({string.Join(", ", parcalar)})";
            if (h.OnaylananAdet > 0)
                cumle += $"; {h.OnaylananAdet} hakediş onaylandı";
            cumleler.Add(cumle + ".");
        }

        if (r.Fatura is { Bos: false } f)
        {
            var parcalar = new List<string>();
            if (f.KesilenAdet > 0)
                parcalar.Add($"{f.KesilenAdet} fatura kesildi ({ParaFormat.FormatKisa(f.KesilenTutar)})");
            if (f.AlinanAdet > 0)
                parcalar.Add($"{f.AlinanAdet} fatura alındı ({ParaFormat.FormatKisa(f.AlinanTutar)})");
            if (parcalar.Count > 0)
                cumleler.Add($"{giris} {string.Join(", ", parcalar)}.");
        }

        if (r.Kasa is { Bos: false } k)
        {
            cumleler.Add(
                $"{giris} {ParaFormat.FormatKisa(k.TahsilatTutari)} tahsilat, {ParaFormat.FormatKisa(k.OdemeTutari)} ödeme gerçekleşti; net nakit {Isaretli(k.NetNakit)}.");
        }

        // Tutarı sıfır olan kalemler ("Kredi Kartı 0 TL") cümle üretmez; "0 TL / 0 TL gerçekleşti" anlamsız olur.
        if (r.Butce is { Planlanan: > 0 } b)
        {
            cumleler.Add(
                $"Bütçe ödemeleri: {ParaFormat.FormatKisa(b.Odenen)} / {ParaFormat.FormatKisa(b.Planlanan)} gerçekleşti ({b.GerceklesmeMetni}).");
        }

        if (r.Personel is { Bos: false } p)
        {
            if (p.PuantajKaydi > 0)
                cumleler.Add($"{p.PuantajKaydi} personel puantajı girildi ({p.PuantajOnayli} onaylı).");
            if (p.MaasKaydiPersonel > 0)
                cumleler.Add($"{p.MaasKaydiPersonel} personel için maaş dönem kaydı alındı.");
            if (p.AvansAdet > 0)
                cumleler.Add($"{p.AvansAdet} avans verildi ({ParaFormat.FormatKisa(p.AvansTutar)}).");
        }

        if (r.Arac is { MasrafAdet: > 0 } a)
            cumleler.Add($"{a.MasrafAdet} araç masrafı işlendi ({ParaFormat.FormatKisa(a.MasrafTutari)}).");

        if (r.Ekip is { Bos: false } e)
            cumleler.Add($"Ekip {e.ToplamIslem} kayıt işlemi yaptı ({e.AktifKullanici} kullanıcı).");

        if (cumleler.Count == 0)
            cumleler.Add(BosMesaj);

        return cumleler;
    }

    /// <summary>Sefer sayıları tam sayıysa ondalıksız, değilse tek ondalıkla ("52", "52,5").</summary>
    public static string Sayi(decimal deger)
        => deger == Math.Truncate(deger) ? deger.ToString("N0", Tr) : deger.ToString("N1", Tr);

    /// <summary>Büyük tutarları kartlara sığacak şekilde kısaltır ("1,24 Mn TL"); küçükleri olduğu gibi bırakır.</summary>
    public static string ParaKisalt(decimal tutar)
    {
        var mutlak = Math.Abs(tutar);
        if (mutlak >= 1_000_000_000m)
            return $"{(tutar / 1_000_000_000m).ToString("0.##", Tr)} Mr TL";
        if (mutlak >= 1_000_000m)
            return $"{(tutar / 1_000_000m).ToString("0.##", Tr)} Mn TL";
        return ParaFormat.FormatKisa(tutar);
    }

    /// <summary>Net tutarı işaretiyle gösterir ("+140.000 TL", "-20.000 TL").</summary>
    public static string Isaretli(decimal tutar)
        => tutar > 0 ? $"+{ParaFormat.FormatKisa(tutar)}" : ParaFormat.FormatKisa(tutar);
}
