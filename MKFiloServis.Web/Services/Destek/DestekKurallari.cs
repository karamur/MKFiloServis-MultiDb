using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Destek;

/// <summary>Talebe göre kullanıcının rolü; birden fazlası aynı anda geçerli olabilir.</summary>
[Flags]
public enum DestekRol
{
    Yok = 0,
    /// <summary>Talebi açan kullanıcı.</summary>
    Sahip = 1,
    /// <summary>Destek personeli (destektalebi.yaz / Admin) ya da talebe atanmış kişi.</summary>
    Personel = 2,
    /// <summary>Yazılım ekibi (destektalebi.yazilim) ve talep yazılım ekibinde.</summary>
    YazilimEkibi = 4
}

/// <summary>Talep üzerinde yapılabilen durum işlemleri.</summary>
public enum DestekEylem
{
    Gonder,
    Ustlen,
    IslemeAl,
    BilgiIste,
    YazilimaAktar,
    DestegeGeriGonder,
    Coz,
    Onayla,
    ItirazEt,
    Kapat,
    YenidenAc
}

/// <param name="Eylem">İşlem.</param>
/// <param name="Kaynaklar">İşlemin yapılabildiği durumlar.</param>
/// <param name="Hedef">İşlem sonrası durum.</param>
/// <param name="Roller">İşlemi yapabilen roller (herhangi biri yeter).</param>
/// <param name="NotZorunlu">Gerekçe/not girilmeden yapılamaz.</param>
/// <param name="Etiket">Düğme metni.</param>
/// <param name="Ikon">Bootstrap Icons sınıfı.</param>
/// <param name="DugmeSinifi">Bootstrap düğme sınıfı.</param>
/// <param name="NotBasligi">Not alanının başlığı (penceredeki).</param>
public sealed record DestekEylemTanimi(
    DestekEylem Eylem,
    IReadOnlyList<DestekDurum> Kaynaklar,
    DestekDurum Hedef,
    DestekRol Roller,
    bool NotZorunlu,
    string Etiket,
    string Ikon,
    string DugmeSinifi,
    string NotBasligi);

/// <summary>
/// Destek talebinin tek durum akışı: etiketler, açık sayılan durumlar ve kimin hangi durumda hangi işlemi
/// yapabileceği. Servis ve sayfalar aynı tabloyu kullanır.
/// </summary>
public static class DestekKurallari
{
    public const string PersonelYetkisi = "destektalebi.yaz";
    public const string YazilimYetkisi = "destektalebi.yazilim";
    public const string TanimYetkisi = "destektalebi.duzenle";
    public const string SilmeYetkisi = "destektalebi.sil";

    /// <summary>Gönderilmiş ve çözülmemiş talepler.</summary>
    public static readonly IReadOnlyList<DestekDurum> AcikDurumlar =
        [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.BilgiBekleniyor, DestekDurum.YazilimEkibinde];

    /// <summary>SLA süresinin işlediği durumlar; talep edenden bilgi beklenirken süre işlemez.</summary>
    public static readonly IReadOnlyList<DestekDurum> SlaIsleyenDurumlar =
        [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.YazilimEkibinde];

    /// <summary>Talep Yönetimi panosunun sütunları (sırayla).</summary>
    public static readonly IReadOnlyList<DestekDurum> PanoSutunlari =
        [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.BilgiBekleniyor, DestekDurum.YazilimEkibinde, DestekDurum.Cozuldu];

    /// <summary>Listelerde seçilebilen durumlar (taslaklar yalnız sahibine görünür).</summary>
    public static readonly IReadOnlyList<DestekDurum> EkipDurumlari =
        [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.BilgiBekleniyor, DestekDurum.YazilimEkibinde, DestekDurum.Cozuldu, DestekDurum.Kapali, DestekDurum.IptalEdildi];

    /// <summary>
    /// Birleştirmeden önceki durum kodlarının karşılığı (2 Açık, 3 Beklemede, 7 Gönderildi, 9 Bitti, 10 Onaylandı).
    /// </summary>
    public static readonly IReadOnlyDictionary<int, DestekDurum> EskiDurumKarsiliklari = new Dictionary<int, DestekDurum>
    {
        [2] = DestekDurum.Islemde,
        [3] = DestekDurum.Islemde,
        [7] = DestekDurum.Yeni,
        [9] = DestekDurum.Cozuldu,
        [10] = DestekDurum.Kapali
    };

    // Aktivite kayıtlarında eski enum adları geçebilir.
    private static readonly IReadOnlyDictionary<string, DestekDurum> EskiDurumAdlari = new Dictionary<string, DestekDurum>(StringComparer.OrdinalIgnoreCase)
    {
        ["Acik"] = DestekDurum.Islemde,
        ["Beklemede"] = DestekDurum.Islemde,
        ["Gonderildi"] = DestekDurum.Yeni,
        ["Bitti"] = DestekDurum.Cozuldu,
        ["Onaylandi"] = DestekDurum.Kapali,
        ["YanitBekleniyor"] = DestekDurum.BilgiBekleniyor
    };

    private static readonly DestekDurum[] Acik = [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.BilgiBekleniyor, DestekDurum.YazilimEkibinde];

    public static readonly IReadOnlyList<DestekEylemTanimi> Eylemler =
    [
        new(DestekEylem.Gonder, [DestekDurum.Taslak], DestekDurum.Yeni, DestekRol.Sahip, false,
            "Gönder", "bi-send", "btn-success", ""),
        new(DestekEylem.Ustlen, [DestekDurum.Yeni], DestekDurum.Islemde, DestekRol.Personel, false,
            "Üstlen", "bi-person-check", "btn-primary", ""),
        new(DestekEylem.IslemeAl, [DestekDurum.BilgiBekleniyor], DestekDurum.Islemde, DestekRol.Personel | DestekRol.YazilimEkibi, false,
            "İşleme Al", "bi-play-circle", "btn-outline-primary", ""),
        new(DestekEylem.BilgiIste, [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.YazilimEkibinde], DestekDurum.BilgiBekleniyor,
            DestekRol.Personel | DestekRol.YazilimEkibi, true,
            "Bilgi İste", "bi-question-circle", "btn-outline-info", "Talep edene sorulacak soru"),
        new(DestekEylem.YazilimaAktar, [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.BilgiBekleniyor], DestekDurum.YazilimEkibinde,
            DestekRol.Personel, true,
            "Yazılım Ekibine Aktar", "bi-code-slash", "btn-outline-dark", "Aktarma gerekçesi (yazılım ekibi görür)"),
        new(DestekEylem.DestegeGeriGonder, [DestekDurum.YazilimEkibinde], DestekDurum.Islemde, DestekRol.Personel | DestekRol.YazilimEkibi, true,
            "Destek Ekibine Geri Gönder", "bi-arrow-return-left", "btn-outline-secondary", "Destek ekibine not"),
        new(DestekEylem.Coz, Acik, DestekDurum.Cozuldu, DestekRol.Personel | DestekRol.YazilimEkibi, true,
            "Çözüldü", "bi-check-circle", "btn-success", "Çözüm açıklaması (talep eden görür)"),
        new(DestekEylem.Onayla, [DestekDurum.Cozuldu], DestekDurum.Kapali, DestekRol.Sahip, false,
            "Çözümü Onayla", "bi-check2-all", "btn-success", ""),
        new(DestekEylem.ItirazEt, [DestekDurum.Cozuldu], DestekDurum.Islemde, DestekRol.Sahip, true,
            "Sorun Devam Ediyor", "bi-arrow-counterclockwise", "btn-outline-danger", "Sorun neden devam ediyor?"),
        new(DestekEylem.Kapat, [.. Acik, DestekDurum.Cozuldu], DestekDurum.Kapali, DestekRol.Personel, true,
            "Kapat", "bi-x-circle", "btn-outline-secondary", "Kapatma nedeni (talep eden görür)"),
        new(DestekEylem.YenidenAc, [DestekDurum.Cozuldu, DestekDurum.Kapali], DestekDurum.Islemde, DestekRol.Personel, false,
            "Yeniden Aç", "bi-arrow-repeat", "btn-outline-warning", "Not (isteğe bağlı)")
    ];

    public static bool AcikMi(DestekDurum durum) => AcikDurumlar.Contains(durum);

    public static DestekEylemTanimi Tanim(DestekEylem eylem) => Eylemler.First(e => e.Eylem == eylem);

    /// <summary>Durum ve role göre yapılabilecek işlemler (düğme sırası tablodaki sıradır).</summary>
    public static IReadOnlyList<DestekEylemTanimi> IzinliEylemler(DestekDurum durum, DestekRol rol) =>
        Eylemler.Where(e => e.Kaynaklar.Contains(durum) && (e.Roller & rol) != 0).ToList();

    public static bool IzinliMi(DestekEylem eylem, DestekDurum durum, DestekRol rol)
    {
        var tanim = Tanim(eylem);
        return tanim.Kaynaklar.Contains(durum) && (tanim.Roller & rol) != 0;
    }

    /// <summary>Veritabanında kalmış eski bir durum değerini yeni akışa çevirir; bilinmeyen değer aynen döner.</summary>
    public static DestekDurum Normallestir(DestekDurum durum) =>
        EskiDurumKarsiliklari.TryGetValue((int)durum, out var yeni) ? yeni : durum;

    public static string Etiket(DestekDurum durum) => Normallestir(durum) switch
    {
        DestekDurum.Taslak => "Taslak",
        DestekDurum.Yeni => "Yeni",
        DestekDurum.Islemde => "İşlemde",
        DestekDurum.BilgiBekleniyor => "Bilgi Bekleniyor",
        DestekDurum.YazilimEkibinde => "Yazılım Ekibinde",
        DestekDurum.Cozuldu => "Çözüldü",
        DestekDurum.Kapali => "Kapandı",
        DestekDurum.IptalEdildi => "İptal Edildi",
        _ => "Bilinmiyor"
    };

    /// <summary>Aktivite kaydındaki durum metni (enum adı ya da yeni etiket) için görünen ad.</summary>
    public static string EtiketMetinden(string? deger)
    {
        if (string.IsNullOrWhiteSpace(deger)) return "-";
        if (EskiDurumAdlari.TryGetValue(deger, out var eski)) return Etiket(eski);
        return Enum.TryParse<DestekDurum>(deger, ignoreCase: true, out var durum) && Enum.IsDefined(durum) ? Etiket(durum) : deger;
    }

    /// <summary>Talep eden için durumun ne anlama geldiği.</summary>
    public static string TalepEdenAciklamasi(DestekDurum durum) => Normallestir(durum) switch
    {
        DestekDurum.Taslak => "Henüz gönderilmedi; düzenleyip gönderebilirsiniz.",
        DestekDurum.Yeni => "Talebiniz alındı; destek ekibi inceleyecek.",
        DestekDurum.Islemde => "Destek ekibi talebiniz üzerinde çalışıyor.",
        DestekDurum.BilgiBekleniyor => "Destek ekibi sizden bilgi bekliyor; lütfen mesaj yazın.",
        DestekDurum.YazilimEkibinde => "Talebiniz yazılım ekibine iletildi; inceleniyor.",
        DestekDurum.Cozuldu => "Talebiniz çözüldü. Lütfen onaylayın; sorun sürüyorsa \"Sorun Devam Ediyor\" deyin.",
        DestekDurum.Kapali => "Talep kapandı.",
        DestekDurum.IptalEdildi => "Talebi iptal ettiniz. Kayıt iptal edilenlerde saklanır.",
        _ => string.Empty
    };

    public static string RozetSinifi(DestekDurum durum) => Normallestir(durum) switch
    {
        DestekDurum.Taslak => "bg-secondary",
        DestekDurum.Yeni => "bg-primary",
        DestekDurum.Islemde => "bg-warning text-dark",
        DestekDurum.BilgiBekleniyor => "bg-info text-dark",
        DestekDurum.YazilimEkibinde => "bg-dark",
        DestekDurum.Cozuldu => "bg-success",
        DestekDurum.Kapali => "bg-light text-dark border",
        DestekDurum.IptalEdildi => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string Ikon(DestekDurum durum) => Normallestir(durum) switch
    {
        DestekDurum.Taslak => "bi-file-earmark",
        DestekDurum.Yeni => "bi-inbox",
        DestekDurum.Islemde => "bi-gear",
        DestekDurum.BilgiBekleniyor => "bi-question-circle",
        DestekDurum.YazilimEkibinde => "bi-code-slash",
        DestekDurum.Cozuldu => "bi-check-circle",
        DestekDurum.Kapali => "bi-check2-all",
        DestekDurum.IptalEdildi => "bi-x-circle",
        _ => "bi-circle"
    };

    public static string OncelikEtiketi(DestekOncelik oncelik) => oncelik switch
    {
        DestekOncelik.Dusuk => "Düşük",
        DestekOncelik.Normal => "Normal",
        DestekOncelik.Yuksek => "Yüksek",
        DestekOncelik.Acil => "Acil",
        DestekOncelik.Kritik => "Kritik",
        _ => oncelik.ToString()
    };

    public static string OncelikRozetSinifi(DestekOncelik oncelik) => oncelik switch
    {
        DestekOncelik.Dusuk => "bg-secondary",
        DestekOncelik.Normal => "bg-info text-dark",
        DestekOncelik.Yuksek => "bg-warning text-dark",
        DestekOncelik.Acil => "bg-danger",
        DestekOncelik.Kritik => "bg-danger",
        _ => "bg-secondary"
    };

    public static string KaynakEtiketi(DestekKaynak kaynak) => kaynak switch
    {
        DestekKaynak.Web => "Web",
        DestekKaynak.Email => "E-posta",
        DestekKaynak.Telefon => "Telefon",
        DestekKaynak.Api => "API",
        DestekKaynak.WhatsApp => "WhatsApp",
        DestekKaynak.MobilUygulama => "Mobil Uygulama",
        DestekKaynak.HataBildirimi => "Program içi hata bildirimi",
        _ => kaynak.ToString()
    };

    /// <summary>
    /// Talep edenin zaman çizelgesinde gösterilecek metin; iç yazışma içerebilecek aktiviteler için null döner.
    /// Eski kayıtların açıklamasında dahili not geçebildiği için metin türden ve durum değerinden üretilir.
    /// </summary>
    public static string? TalepEdenAktiviteMetni(DestekTalebiAktivite aktivite) => aktivite.AktiviteTuru switch
    {
        AktiviteTuru.Olusturuldu => "Talep oluşturuldu",
        AktiviteTuru.DurumDegisti => $"Durum: {EtiketMetinden(aktivite.YeniDeger)}",
        AktiviteTuru.Atandi => "Talep bir destek yetkilisine atandı",
        AktiviteTuru.Transferedildi => "Talep ilgili ekibe aktarıldı",
        AktiviteTuru.YazilimEkibineAktarildi => "Talep yazılım ekibine aktarıldı",
        AktiviteTuru.Kapandi => "Talep kapandı",
        AktiviteTuru.IptalEdildi => "Talep sahibi talebi iptal etti",
        AktiviteTuru.YenidenAcildi => "Talep yeniden açıldı",
        AktiviteTuru.CozumOnaylandi => "Çözüm onaylandı",
        AktiviteTuru.CozumeItirazEdildi => "Çözüme itiraz edildi",
        _ => null
    };

    /// <summary>Talep edenin takip ekranındaki adımlar.</summary>
    public static IReadOnlyList<DestekDurum> TakipAdimlari(DestekDurum durum) =>
        Normallestir(durum) == DestekDurum.IptalEdildi
            ? [DestekDurum.Yeni, DestekDurum.IptalEdildi]
            :
        Normallestir(durum) == DestekDurum.YazilimEkibinde
            ? [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.YazilimEkibinde, DestekDurum.Cozuldu, DestekDurum.Kapali]
            : [DestekDurum.Yeni, DestekDurum.Islemde, DestekDurum.Cozuldu, DestekDurum.Kapali];

    /// <summary>Adımın tamamlanıp tamamlanmadığı (bilgi bekleniyor, işlemde adımında sayılır).</summary>
    public static bool AdimGecildi(DestekDurum mevcut, DestekDurum adim)
    {
        static int Sira(DestekDurum d) => d switch
        {
            DestekDurum.Taslak => 0,
            DestekDurum.Yeni => 1,
            DestekDurum.Islemde or DestekDurum.BilgiBekleniyor => 2,
            DestekDurum.YazilimEkibinde => 3,
            DestekDurum.Cozuldu => 4,
            DestekDurum.Kapali => 5,
            DestekDurum.IptalEdildi => 6,
            _ => 0
        };
        return Sira(Normallestir(mevcut)) >= Sira(adim);
    }
}
