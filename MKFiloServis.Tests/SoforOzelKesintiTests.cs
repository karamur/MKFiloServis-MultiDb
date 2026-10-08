using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Tests;

public sealed class SoforOzelKesintiTests
{
    [Fact]
    public void Ozel_kesinti_toplami_personel_kartindaki_tum_kalemleri_kapsar()
    {
        var sofor = new Sofor
        {
            IcraKesintisi = 100,
            BESKesintisi = 200,
            SendikaKesintisi = 300,
            HayatSigortasi = 400,
            BireyselEmeklilik = 500,
            DigerOzelKesinti = 600
        };

        Assert.Equal(2100, sofor.OzelKesintiToplami);
    }
}
