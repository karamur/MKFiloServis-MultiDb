using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services.Destek;

namespace MKFiloServis.Tests;

public sealed class DestekErisimTests
{
    private const int YazilimDepartmani = 9;

    private static DestekKullanici Kisi(int id, params string[] yetkiler) =>
        new(id, $"Kişi {id}", null, Yonetici: false, yetkiler.ToHashSet());

    private static DestekTalebi Talep(DestekDurum durum, int? sahip = 1, int? atanan = null, int departman = 1) =>
        new() { Durum = durum, OlusturanKullaniciId = sahip, AtananKullaniciId = atanan, DepartmanId = departman };

    [Fact]
    public void Talep_eden_kendi_talebini_gorur_baskasi_goremez()
    {
        var talep = Talep(DestekDurum.Yeni);

        Assert.Equal(DestekRol.Sahip, DestekErisim.Rol(Kisi(1), talep, YazilimDepartmani));
        Assert.Equal(DestekRol.Yok, DestekErisim.Rol(Kisi(2), talep, YazilimDepartmani));
        Assert.Equal(DestekRol.Yok, DestekErisim.Rol(null, talep, YazilimDepartmani));
    }

    [Fact]
    public void Taslak_destek_personeline_bile_gorunmez()
    {
        var taslak = Talep(DestekDurum.Taslak);

        Assert.Equal(DestekRol.Yok, DestekErisim.Rol(Kisi(5, DestekKurallari.PersonelYetkisi), taslak, YazilimDepartmani));
        Assert.Equal(DestekRol.Yok, DestekErisim.Rol(new DestekKullanici(6, "Yönetici", null, true, new HashSet<string>()), taslak, YazilimDepartmani));
        Assert.Equal(DestekRol.Sahip, DestekErisim.Rol(Kisi(1), taslak, YazilimDepartmani));
    }

    [Fact]
    public void Destek_personeli_ve_atanan_kisi_talebi_isler()
    {
        var talep = Talep(DestekDurum.Islemde, atanan: 7);

        Assert.True(DestekErisim.Isleyebilir(DestekErisim.Rol(Kisi(5, DestekKurallari.PersonelYetkisi), talep, YazilimDepartmani)));
        Assert.True(DestekErisim.Isleyebilir(DestekErisim.Rol(Kisi(7), talep, YazilimDepartmani)));   // yetkisi olmasa da atanan
        Assert.False(DestekErisim.Isleyebilir(DestekErisim.Rol(Kisi(1), talep, YazilimDepartmani)));
        Assert.True(DestekErisim.YalnizSahip(DestekErisim.Rol(Kisi(1), talep, YazilimDepartmani)));
    }

    [Fact]
    public void Yazilim_ekibi_yalniz_kendisine_aktarilan_talepleri_gorur()
    {
        var yazilimci = Kisi(8, DestekKurallari.YazilimYetkisi);

        Assert.Equal(DestekRol.Yok, DestekErisim.Rol(yazilimci, Talep(DestekDurum.Islemde), YazilimDepartmani));
        Assert.Equal(DestekRol.YazilimEkibi, DestekErisim.Rol(yazilimci, Talep(DestekDurum.YazilimEkibinde), YazilimDepartmani));
        Assert.Equal(DestekRol.YazilimEkibi, DestekErisim.Rol(yazilimci, Talep(DestekDurum.Cozuldu, departman: YazilimDepartmani), YazilimDepartmani));
    }

    [Fact]
    public void Yetkiler_rollere_dogru_cevrilir()
    {
        Assert.True(Kisi(1, DestekKurallari.PersonelYetkisi).EkipUyesi);
        Assert.True(Kisi(1, DestekKurallari.YazilimYetkisi).EkipUyesi);
        Assert.False(Kisi(1, "destektalebi.oku").EkipUyesi);
        Assert.True(Kisi(1, DestekKurallari.TanimYetkisi).MakaleYazar);
        Assert.False(Kisi(1, DestekKurallari.YazilimYetkisi).Silebilir);
        var yonetici = new DestekKullanici(1, "Admin", null, true, new HashSet<string>());
        Assert.True(yonetici.Personel && yonetici.YazilimEkibi && yonetici.TanimYonetir && yonetici.Silebilir);
    }
}
