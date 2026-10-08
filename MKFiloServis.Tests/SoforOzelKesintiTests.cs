using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services.Calculation;

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

    [Fact]
    public void Ozel_kesinti_toplami_maas_hesabinda_odenecek_tutardan_dusulur()
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

        var sonuc = MaasHesaplamaEngine.Hesapla(new MaasInput
        {
            GercekMaas = 5000,
            BankayaYatan = 4000,
            Avans = 100,
            Kesinti = sofor.OzelKesintiToplami,
            Harcama = 500
        });

        Assert.Equal(2100, sonuc.Kesinti);
        Assert.Equal(-700, sonuc.Odenecek);
    }
}
