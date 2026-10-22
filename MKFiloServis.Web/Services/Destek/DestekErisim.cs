using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Destek;

/// <summary>
/// İşlemi yapan kullanıcı ve Destek yetkileri. Talep açmak ve kendi taleplerini izlemek için yetki gerekmez;
/// ekip işlemleri <see cref="DestekKurallari.PersonelYetkisi"/> / <see cref="DestekKurallari.YazilimYetkisi"/> ister.
/// </summary>
public sealed record DestekKullanici(int Id, string AdSoyad, string? Email, bool Yonetici, IReadOnlySet<string> Yetkiler)
{
    /// <summary>Destek personeli: tüm talepleri görür ve işler.</summary>
    public bool Personel => Yonetici || Yetkiler.Contains(DestekKurallari.PersonelYetkisi);

    /// <summary>Yazılım ekibi: yazılım ekibine aktarılan talepleri görür ve işler.</summary>
    public bool YazilimEkibi => Yonetici || Yetkiler.Contains(DestekKurallari.YazilimYetkisi);

    /// <summary>Departman, kategori, SLA, hazır yanıt ve ayarları yönetir.</summary>
    public bool TanimYonetir => Yonetici || Yetkiler.Contains(DestekKurallari.TanimYetkisi);

    /// <summary>Talep siler.</summary>
    public bool Silebilir => Yonetici || Yetkiler.Contains(DestekKurallari.SilmeYetkisi);

    /// <summary>Ekip ekranlarını (Tüm Talepler, Talep Yönetimi) açabilir.</summary>
    public bool EkipUyesi => Personel || YazilimEkibi;

    /// <summary>Bilgi bankası makalesi yazabilir.</summary>
    public bool MakaleYazar => Personel || TanimYonetir;
}

/// <summary>Talebe erişim kuralları (saf mantık; servis ve testler kullanır).</summary>
public static class DestekErisim
{
    /// <summary>Kullanıcının bu talepteki rolü. Taslaklar yalnız sahibine görünür.</summary>
    public static DestekRol Rol(DestekKullanici? kullanici, DestekTalebi talep, int? yazilimDepartmanId)
    {
        if (kullanici is null) return DestekRol.Yok;

        var rol = DestekRol.Yok;
        if (talep.OlusturanKullaniciId == kullanici.Id) rol |= DestekRol.Sahip;
        if (talep.Durum == DestekDurum.Taslak) return rol;

        if (kullanici.Personel || talep.AtananKullaniciId == kullanici.Id) rol |= DestekRol.Personel;
        if (kullanici.YazilimEkibi && YazilimEkibinde(talep, yazilimDepartmanId)) rol |= DestekRol.YazilimEkibi;
        return rol;
    }

    /// <summary>Talep yazılım ekibinin kapsamında mı (durum ya da departman).</summary>
    public static bool YazilimEkibinde(DestekTalebi talep, int? yazilimDepartmanId) =>
        talep.Durum == DestekDurum.YazilimEkibinde
        || (yazilimDepartmanId.HasValue && talep.DepartmanId == yazilimDepartmanId.Value);

    public static bool Gorebilir(DestekRol rol) => rol != DestekRol.Yok;

    /// <summary>Ekip işlemleri (yanıt, dahili not, atama, durum) yapabilir.</summary>
    public static bool Isleyebilir(DestekRol rol) => (rol & (DestekRol.Personel | DestekRol.YazilimEkibi)) != 0;

    /// <summary>Talep eden olarak mı görüyor (ekip yetkisi olmadan).</summary>
    public static bool YalnizSahip(DestekRol rol) => rol == DestekRol.Sahip;
}
