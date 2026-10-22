using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services.Destek;

namespace MKFiloServis.Tests;

public sealed class DestekKurallariTests
{
    private static List<DestekEylem> Eylemler(DestekDurum durum, DestekRol rol) =>
        DestekKurallari.IzinliEylemler(durum, rol).Select(e => e.Eylem).ToList();

    [Fact]
    public void Talep_eden_yalniz_gonderir_onaylar_ve_itiraz_eder()
    {
        Assert.Equal([DestekEylem.Gonder], Eylemler(DestekDurum.Taslak, DestekRol.Sahip));
        Assert.Equal([DestekEylem.Onayla, DestekEylem.ItirazEt], Eylemler(DestekDurum.Cozuldu, DestekRol.Sahip));
        foreach (var durum in new[] { DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.BilgiBekleniyor, DestekDurum.YazilimEkibinde, DestekDurum.Kapali })
            Assert.Empty(Eylemler(durum, DestekRol.Sahip));
    }

    [Fact]
    public void Destek_personeli_yeni_talebi_ustlenir_bilgi_ister_aktarir_cozer_ve_kapatir()
    {
        var eylemler = Eylemler(DestekDurum.Yeni, DestekRol.Personel);

        Assert.Contains(DestekEylem.Ustlen, eylemler);
        Assert.Contains(DestekEylem.BilgiIste, eylemler);
        Assert.Contains(DestekEylem.YazilimaAktar, eylemler);
        Assert.Contains(DestekEylem.Coz, eylemler);
        Assert.Contains(DestekEylem.Kapat, eylemler);
        Assert.DoesNotContain(DestekEylem.Onayla, eylemler);
        Assert.DoesNotContain(DestekEylem.Gonder, eylemler);
    }

    [Fact]
    public void Yazilim_ekibi_aktarilan_talebi_geri_gonderir_ya_da_cozer_ama_kapatamaz()
    {
        var eylemler = Eylemler(DestekDurum.YazilimEkibinde, DestekRol.YazilimEkibi);

        Assert.Contains(DestekEylem.DestegeGeriGonder, eylemler);
        Assert.Contains(DestekEylem.Coz, eylemler);
        Assert.Contains(DestekEylem.BilgiIste, eylemler);
        Assert.DoesNotContain(DestekEylem.Kapat, eylemler);
        Assert.DoesNotContain(DestekEylem.YazilimaAktar, eylemler);
    }

    [Fact]
    public void Kapanan_talep_yalniz_personelce_yeniden_acilir()
    {
        Assert.Equal([DestekEylem.YenidenAc], Eylemler(DestekDurum.Kapali, DestekRol.Personel));
        Assert.Empty(Eylemler(DestekDurum.Kapali, DestekRol.Sahip | DestekRol.YazilimEkibi));
    }

    [Fact]
    public void Not_zorunlu_islemler_gerekce_ister()
    {
        Assert.True(DestekKurallari.Tanim(DestekEylem.YazilimaAktar).NotZorunlu);
        Assert.True(DestekKurallari.Tanim(DestekEylem.Coz).NotZorunlu);
        Assert.True(DestekKurallari.Tanim(DestekEylem.ItirazEt).NotZorunlu);
        Assert.True(DestekKurallari.Tanim(DestekEylem.Kapat).NotZorunlu);
        Assert.False(DestekKurallari.Tanim(DestekEylem.Onayla).NotZorunlu);
        Assert.False(DestekKurallari.Tanim(DestekEylem.Ustlen).NotZorunlu);
    }

    [Theory]
    [InlineData(7, DestekDurum.Yeni)]
    [InlineData(2, DestekDurum.Islemde)]
    [InlineData(3, DestekDurum.Islemde)]
    [InlineData(9, DestekDurum.Cozuldu)]
    [InlineData(10, DestekDurum.Kapali)]
    [InlineData(4, DestekDurum.BilgiBekleniyor)]
    public void Eski_durum_kodlari_yeni_akisa_cevrilir(int eskiKod, DestekDurum beklenen)
    {
        Assert.Equal(beklenen, DestekKurallari.Normallestir((DestekDurum)eskiKod));
        Assert.Equal(DestekKurallari.Etiket(beklenen), DestekKurallari.Etiket((DestekDurum)eskiKod));
    }

    [Theory]
    [InlineData("Gonderildi", "Yeni")]
    [InlineData("YanitBekleniyor", "Bilgi Bekleniyor")]
    [InlineData("Onaylandi", "Kapandı")]
    [InlineData("Islemde", "İşlemde")]
    [InlineData("Yazılım Ekibinde", "Yazılım Ekibinde")]
    public void Aktivite_kaydindaki_durum_metni_etiketlenir(string deger, string beklenen)
    {
        Assert.Equal(beklenen, DestekKurallari.EtiketMetinden(deger));
    }

    [Fact]
    public void Talep_eden_zaman_cizelgesi_ic_kayitlari_ve_eski_kapatma_notlarini_gostermez()
    {
        Assert.Null(DestekKurallari.TalepEdenAktiviteMetni(new DestekTalebiAktivite { AktiviteTuru = AktiviteTuru.YanitEklendi, Aciklama = "Dahili not eklendi" }));
        Assert.Null(DestekKurallari.TalepEdenAktiviteMetni(new DestekTalebiAktivite { AktiviteTuru = AktiviteTuru.SlaBilgilendirme }));

        // Eski kayıtlarda kapatma notu açıklamaya yazılıyordu; metin türden üretilir, not sızmaz.
        var eski = new DestekTalebiAktivite { AktiviteTuru = AktiviteTuru.Kapandi, Aciklama = "Talep kapatıldı: müşteri borçlu, ilgilenme" };
        Assert.Equal("Talep kapandı", DestekKurallari.TalepEdenAktiviteMetni(eski));

        var durum = new DestekTalebiAktivite { AktiviteTuru = AktiviteTuru.DurumDegisti, YeniDeger = "Onaylandi" };
        Assert.Equal("Durum: Kapandı", DestekKurallari.TalepEdenAktiviteMetni(durum));
    }

    [Fact]
    public void Acik_talep_cozulmemis_gonderilmis_taleptir()
    {
        Assert.True(new DestekTalebi { Durum = DestekDurum.BilgiBekleniyor }.Acik);
        Assert.True(new DestekTalebi { Durum = DestekDurum.YazilimEkibinde }.Acik);
        Assert.False(new DestekTalebi { Durum = DestekDurum.Cozuldu }.Acik);
        Assert.False(new DestekTalebi { Durum = DestekDurum.Taslak }.Acik);
        Assert.False(new DestekTalebi { Durum = DestekDurum.Kapali }.Acik);
    }

    [Fact]
    public void Takip_adimlari_yazilim_asamasini_yalniz_gerektiginde_gosterir()
    {
        Assert.DoesNotContain(DestekDurum.YazilimEkibinde, DestekKurallari.TakipAdimlari(DestekDurum.Islemde));
        Assert.Contains(DestekDurum.YazilimEkibinde, DestekKurallari.TakipAdimlari(DestekDurum.YazilimEkibinde));
        Assert.True(DestekKurallari.AdimGecildi(DestekDurum.BilgiBekleniyor, DestekDurum.Islemde));
        Assert.False(DestekKurallari.AdimGecildi(DestekDurum.Islemde, DestekDurum.Cozuldu));
    }
}
