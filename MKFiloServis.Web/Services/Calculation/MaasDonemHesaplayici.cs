namespace MKFiloServis.Web.Services.Calculation;

// Kayıt türleri veritabanından bağımsızdır. MaastanKesinti: true ise tutar o dönemin maaşından kesilerek kapatılmıştır,
// false ise personel nakit/banka ile iade etmiştir. OdemeTarihi: null ise harcama henüz geri ödenmemiştir.

/// <summary>Avans kaydına yapılmış bir mahsup / iade.</summary>
public sealed record DonemMahsup(DateTime Tarih, decimal Tutar, bool MaastanKesinti);

/// <summary>Personele verilmiş avans.</summary>
public sealed record DonemAvans(DateTime Tarih, decimal Tutar, IReadOnlyList<DonemMahsup> Mahsuplar);

/// <summary>Şirketin personele borcuna (maaş/prim/mesai alacağı) yapılmış ödeme.</summary>
public sealed record DonemBorcOdeme(DateTime Tarih, decimal Tutar);

/// <summary>Şirketin personele olan borcu.</summary>
public sealed record DonemBorc(DateTime Tarih, decimal Tutar, IReadOnlyList<DonemBorcOdeme> Odemeler);

/// <summary>Personelin şirket adına cebinden yaptığı harcama.</summary>
public sealed record DonemHarcama(DateTime Tarih, decimal Tutar, DateTime? OdemeTarihi);

/// <summary>Bir personelin seçilen ay için avans / harcama / borç görünümü ve önceki aylardan devreden bakiyesi.</summary>
public sealed class DonemFinansSonuc
{
    /// <summary>O ay içinde verilen avanslar (maaştan düşülecek).</summary>
    public decimal Avans { get; init; }

    /// <summary>O ay içindeki geri ödenmemiş cebinden harcamalar ile şirketin o ay doğan personele borcu (maaşa eklenecek).</summary>
    public decimal Harcama { get; init; }

    /// <summary>Ay başı itibarıyla kapanmamış önceki ay avans bakiyesi (personelin şirkete borcu).</summary>
    public decimal DevirAvans { get; init; }

    /// <summary>Ay başı itibarıyla kapanmamış önceki ay şirket borcu (personelin alacağı).</summary>
    public decimal DevirBorc { get; init; }

    /// <summary>Ay başı itibarıyla geri ödenmemiş önceki ay cebinden harcamaları (personelin alacağı).</summary>
    public decimal DevirHarcama { get; init; }

    /// <summary>
    /// Geçen aydan kalan net bakiye. Pozitif: şirket personele borçlu (ödenecek artar);
    /// negatif: personel şirkete borçlu (ödenecek azalır).
    /// </summary>
    public decimal GecenAydanKalan => DevirBorc + DevirHarcama - DevirAvans;

    public bool DevirVar => DevirAvans != 0 || DevirBorc != 0 || DevirHarcama != 0;
}

/// <summary>
/// Aylık maaş / ödeme listesi için avans, borç ve cebinden harcama kayıtlarını tarihlerine göre
/// seçilen aya böler. UI ve servisler bu sınıfı kullanır; veritabanı veya EF bilgisi taşımaz.
///
/// Kurallar (tarihler yalnızca gün olarak karşılaştırılır):
/// - Bu ay: avans, harcama ve borç kendi ayında gösterilir. Maaştan kesinti yoluyla yapılan mahsup,
///   o ayın kesintisini silmez (kesintinin kendisidir); personelin nakit/banka iadesi ay sonuna kadar düşer.
/// - Devir: ay başından önce doğan, ay başı itibarıyla kapanmamış bakiyelerdir. Kapanış, ilgili kaydın
///   mahsup / ödeme / geri ödeme tarihiyle belirlenir; bu yüzden geçmiş aylar sonradan da aynı sonucu verir.
/// - Ay sonundan sonraki kayıtlar o ayı etkilemez.
/// </summary>
public static class MaasDonemHesaplayici
{
    public static DonemFinansSonuc Hesapla(
        int yil,
        int ay,
        IEnumerable<DonemAvans>? avanslar,
        IEnumerable<DonemBorc>? borclar,
        IEnumerable<DonemHarcama>? harcamalar)
    {
        var donemBasi = new DateTime(yil, ay, 1);
        var donemSonu = donemBasi.AddMonths(1).AddDays(-1);

        decimal buAyAvans = 0, devirAvans = 0;
        foreach (var avans in avanslar ?? [])
        {
            if (avans.Tutar <= 0)
                continue;

            var tarih = avans.Tarih.Date;
            var mahsuplar = avans.Mahsuplar ?? [];
            if (tarih >= donemBasi && tarih <= donemSonu)
            {
                // Ay içinde verilen avans: yalnızca personelin ay sonuna kadar iade ettiği kısım düşer.
                var iade = mahsuplar.Where(m => !m.MaastanKesinti && m.Tarih.Date <= donemSonu).Sum(m => m.Tutar);
                buAyAvans += Math.Max(0, avans.Tutar - iade);
            }
            else if (tarih < donemBasi)
            {
                var kapanan = mahsuplar.Where(m => m.Tarih.Date < donemBasi).Sum(m => m.Tutar);
                devirAvans += Math.Max(0, avans.Tutar - kapanan);
            }
        }

        decimal buAyBorc = 0, devirBorc = 0;
        foreach (var borc in borclar ?? [])
        {
            if (borc.Tutar <= 0)
                continue;

            var tarih = borc.Tarih.Date;
            var odemeler = borc.Odemeler ?? [];
            if (tarih >= donemBasi && tarih <= donemSonu)
            {
                var odenen = odemeler.Where(o => o.Tarih.Date <= donemSonu).Sum(o => o.Tutar);
                buAyBorc += Math.Max(0, borc.Tutar - odenen);
            }
            else if (tarih < donemBasi)
            {
                var odenen = odemeler.Where(o => o.Tarih.Date < donemBasi).Sum(o => o.Tutar);
                devirBorc += Math.Max(0, borc.Tutar - odenen);
            }
        }

        decimal buAyHarcama = 0, devirHarcama = 0;
        foreach (var harcama in harcamalar ?? [])
        {
            if (harcama.Tutar <= 0)
                continue;

            var tarih = harcama.Tarih.Date;
            var odemeGunu = harcama.OdemeTarihi?.Date;
            if (tarih >= donemBasi && tarih <= donemSonu)
            {
                if (odemeGunu is null || odemeGunu > donemSonu)
                    buAyHarcama += harcama.Tutar;
            }
            else if (tarih < donemBasi)
            {
                if (odemeGunu is null || odemeGunu >= donemBasi)
                    devirHarcama += harcama.Tutar;
            }
        }

        return new DonemFinansSonuc
        {
            Avans = buAyAvans,
            Harcama = buAyHarcama + buAyBorc,
            DevirAvans = devirAvans,
            DevirBorc = devirBorc,
            DevirHarcama = devirHarcama
        };
    }
}
