using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class PuantajKartKontroluTests
{
    private static Sofor Saatlik(decimal kartSaat) => new() { BrutMaasHesaplamaTipi = BrutMaasHesaplamaTipi.Saatlik, CalismaMiktari = kartSaat, BirimUcret = 150 };
    private static Sofor Gunluk(decimal kartGun) => new() { BrutMaasHesaplamaTipi = BrutMaasHesaplamaTipi.Gunluk, CalismaMiktari = kartGun, BirimUcret = 1200 };

    private static List<GunlukPuantaj> Gunler(int adet, decimal saat, decimal fazlaMesai = 0)
        => Enumerable.Range(1, adet).Select(g => new GunlukPuantaj { Gun = g, Calisti = true, CalismaSaati = saat, FazlaMesaiSaat = fazlaMesai }).ToList();

    [Fact]
    public void Saatlik_puantaj_kart_saatiyle_ayniysa_uyumludur()
    {
        var sonuc = PuantajKartKontrolu.Hesapla(Saatlik(195), Gunler(26, 7.5m));

        Assert.Equal(KartKontrolDurumu.Uyumlu, sonuc.Durum);
        Assert.Equal(195m, sonuc.PuantajDegeri);
        Assert.False(sonuc.Uyarili);
        Assert.Equal("195 / 195 s", sonuc.Ozet);
    }

    [Fact]
    public void Saatlik_puantaj_kart_saatinden_azsa_eksik_uyarisi_verir()
    {
        var sonuc = PuantajKartKontrolu.Hesapla(Saatlik(195), Gunler(22, 7.5m));

        Assert.Equal(KartKontrolDurumu.Az, sonuc.Durum);
        Assert.True(sonuc.Uyarili);
        Assert.Equal(-30m, sonuc.Fark);
        Assert.Equal("Kartta 195 saat yazıyor, puantajda 165 saat var (30 saat eksik).", sonuc.Mesaj);
    }

    [Fact]
    public void Saatlik_puantaj_kart_saatinden_fazlaysa_fazla_uyarisi_verir_ve_fazla_mesaiyi_belirtir()
    {
        // 26 gün × (7,5 normal + 1,5 fazla mesai) = 234 saat; kartta 195
        var sonuc = PuantajKartKontrolu.Hesapla(Saatlik(195), Gunler(26, 7.5m, 1.5m));

        Assert.Equal(KartKontrolDurumu.Fazla, sonuc.Durum);
        Assert.Equal(234m, sonuc.PuantajDegeri);
        Assert.Equal(39m, sonuc.FazlaMesai);
        Assert.Equal("Kartta 195 saat yazıyor, puantajda 234 saat var (39 saat fazla; 39 saati fazla mesai).", sonuc.Mesaj);
    }

    [Fact]
    public void Izinli_mazeretli_ve_calismayan_gunlerin_saati_sayilmaz()
    {
        var gunler = Gunler(20, 7.5m);
        gunler.Add(new GunlukPuantaj { Gun = 21, Izinli = true, CalismaSaati = 7.5m });
        gunler.Add(new GunlukPuantaj { Gun = 22, Mazeret = true, CalismaSaati = 7.5m });
        gunler.Add(new GunlukPuantaj { Gun = 23, Calisti = false, CalismaSaati = 7.5m, FazlaMesaiSaat = 2 });

        var sonuc = PuantajKartKontrolu.Hesapla(Saatlik(150), gunler);

        Assert.Equal(150m, sonuc.PuantajDegeri);
        Assert.Equal(KartKontrolDurumu.Uyumlu, sonuc.Durum);
    }

    [Fact]
    public void Kucuk_ondalik_farklar_uyarilmaz()
    {
        var gunler = Gunler(26, 7.5m);
        gunler[0].CalismaSaati = 7.495m;

        Assert.Equal(KartKontrolDurumu.Uyumlu, PuantajKartKontrolu.Hesapla(Saatlik(195), gunler).Durum);
    }

    [Fact]
    public void Gunluk_personelde_calisilan_gun_sayisi_kartla_karsilastirilir()
    {
        var sonuc = PuantajKartKontrolu.Hesapla(Gunluk(26), Gunler(24, 0));

        Assert.Equal(KartKontrolDurumu.Az, sonuc.Durum);
        Assert.Equal(KartKontrolBirimi.Gun, sonuc.Birim);
        Assert.Equal("Kartta 26 gün yazıyor, puantajda 24 gün var (2 gün eksik).", sonuc.Mesaj);
        Assert.Equal(KartKontrolDurumu.Fazla, PuantajKartKontrolu.Hesapla(Gunluk(26), Gunler(27, 0)).Durum);
        Assert.Equal(KartKontrolDurumu.Uyumlu, PuantajKartKontrolu.Hesapla(Gunluk(26), Gunler(26, 0)).Durum);
    }

    [Theory]
    [InlineData(BrutMaasHesaplamaTipi.Manuel, 195)]
    [InlineData(BrutMaasHesaplamaTipi.Aylik, 1)]
    [InlineData(BrutMaasHesaplamaTipi.Saatlik, 0)]
    [InlineData(BrutMaasHesaplamaTipi.Gunluk, 0)]
    public void Kartta_saat_gun_tanimi_olmayan_personel_karsilastirilmaz(BrutMaasHesaplamaTipi tip, decimal miktar)
    {
        var personel = new Sofor { BrutMaasHesaplamaTipi = tip, CalismaMiktari = miktar };

        var sonuc = PuantajKartKontrolu.Hesapla(personel, Gunler(10, 7.5m));

        Assert.Equal(KartKontrolDurumu.Tanimsiz, sonuc.Durum);
        Assert.False(sonuc.Uyarili);
        Assert.Equal("-", sonuc.Ozet);
        Assert.Contains("kontrol yapılmadı", sonuc.Mesaj);
    }

    [Fact]
    public void Personel_bilinmiyorsa_tanimsiz_doner()
    {
        Assert.Equal(KartKontrolDurumu.Tanimsiz, PuantajKartKontrolu.Hesapla(null, Gunler(5, 8)).Durum);
    }

    [Fact]
    public void Calisma_duzeni_plani_secili_gun_ve_gunluk_saatle_degerlendirilir()
    {
        // 26 gün × 7,5 = 195 → uyumlu; 27 gün × 7,5 = 202,5 → fazla; 24 gün → eksik
        Assert.Equal(KartKontrolDurumu.Uyumlu, PuantajKartKontrolu.Plan(Saatlik(195), 26, 7.5m).Durum);
        Assert.Equal(KartKontrolDurumu.Fazla, PuantajKartKontrolu.Plan(Saatlik(195), 27, 7.5m).Durum);
        Assert.Equal(KartKontrolDurumu.Az, PuantajKartKontrolu.Plan(Saatlik(195), 24, 7.5m).Durum);
        // Günlük saat 9 → 1,5 saat/gün fazla mesai planlanır
        var fazlaMesaili = PuantajKartKontrolu.Plan(Saatlik(195), 26, 9m);
        Assert.Equal(234m, fazlaMesaili.PuantajDegeri);
        Assert.Equal(39m, fazlaMesaili.FazlaMesai);
        // Günlük personelde yalnız gün sayısı önemlidir
        Assert.Equal(KartKontrolDurumu.Uyumlu, PuantajKartKontrolu.Plan(Gunluk(26), 26, 0).Durum);
    }

    [Theory]
    [InlineData(195, 7.5)]   // kart aylık toplam: günlük öneri standart süre
    [InlineData(225, 7.5)]
    [InlineData(8, 8)]       // 24 saatin altı eski kayıtlarda günlük saat olarak girilmiş olabilir
    [InlineData(0, 7.5)]
    public void Varsayilan_gunluk_saat_aylik_toplami_gunluk_saat_sanmaz(decimal kart, decimal beklenen)
    {
        Assert.Equal(beklenen, PuantajKartKontrolu.VarsayilanGunlukSaat(Saatlik(kart)));
    }

    [Theory]
    [InlineData(7.5, 7.5, 0)]
    [InlineData(9, 7.5, 1.5)]
    [InlineData(4, 4, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(-3, 0, 0)]
    public void Gunluk_saat_standart_sureye_gore_bolunur(decimal saat, decimal normal, decimal fazlaMesai)
    {
        Assert.Equal((normal, fazlaMesai), PuantajKartKontrolu.Bol(saat));
    }
}
