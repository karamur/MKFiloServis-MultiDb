using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Destek;

/// <summary>Talep detayı: kullanıcının rolüne göre süzülmüş yazışma, zaman çizelgesi, ekler ve yapılabilecek işlemler.</summary>
public sealed class DestekTalepDetayi
{
    public required DestekTalebi Talep { get; init; }

    /// <summary>Yazışma; talep eden olarak bakılıyorsa dahili notlar çıkarılmıştır.</summary>
    public required IReadOnlyList<DestekTalebiYanit> Yanitlar { get; init; }

    /// <summary>Zaman çizelgesi (yeniden eskiye); talep eden olarak bakılıyorsa iç kayıtlar çıkarılmıştır.</summary>
    public required IReadOnlyList<DestekTalebiAktivite> Aktiviteler { get; init; }

    /// <summary>Ekler; talep eden olarak bakılıyorsa dahili notlara ait ekler çıkarılmıştır.</summary>
    public required IReadOnlyList<DestekTalebiEk> Ekler { get; init; }

    public required DestekRol Rol { get; init; }

    /// <summary>Kullanıcının şu anki durumda yapabileceği durum işlemleri.</summary>
    public required IReadOnlyList<DestekEylemTanimi> Eylemler { get; init; }

    public int? YazilimDepartmanId { get; init; }

    public bool Isleyebilir => DestekErisim.Isleyebilir(Rol);

    public bool Sahip => Rol.HasFlag(DestekRol.Sahip);

    /// <summary>Mesaj yazılabilir mi (kapanmış ve taslak taleplere yazılmaz).</summary>
    public bool MesajYazilabilir => Talep.Durum is not (DestekDurum.Kapali or DestekDurum.Taslak);
}

/// <summary>Yazışmaya eklenecek mesaj.</summary>
public sealed class DestekYanitIstegi
{
    public string Icerik { get; set; } = string.Empty;

    /// <summary>Yalnız ekip görür (talep eden için yok sayılır).</summary>
    public bool DahiliNot { get; set; }

    /// <summary>Mesajla birlikte talebi "Bilgi Bekleniyor" durumuna al (ekip için).</summary>
    public bool BilgiIste { get; set; }

    public int? HazirYanitId { get; set; }
}

/// <summary>Durum işlemi ayrıntısı.</summary>
public sealed class DestekEylemIstegi
{
    /// <summary>Gerekçe, çözüm açıklaması ya da not.</summary>
    public string? Not { get; set; }

    /// <summary>Çözüm onaylanırken 1–5 memnuniyet puanı (isteğe bağlı).</summary>
    public int? MemnuniyetPuani { get; set; }

    public string? MemnuniyetYorumu { get; set; }
}

/// <summary>"Sorun Bildir" ile açılan talep: kullanıcının yazdıkları ve otomatik toplanan bağlam.</summary>
public sealed class SorunBildirimi
{
    public string? Konu { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public DestekOncelik Oncelik { get; set; } = DestekOncelik.Normal;

    public string? Sayfa { get; set; }
    public string? OncekiSayfa { get; set; }
    public string? HataMesaji { get; set; }
    public string? HataTuru { get; set; }
    public string? YiginIzi { get; set; }
    public DateTime? HataZamaniUtc { get; set; }
    public string? Tarayici { get; set; }
    public string? UygulamaSurumu { get; set; }

    public string? EkDosyaAdi { get; set; }
    public string? EkMimeTipi { get; set; }
    public byte[]? EkIcerik { get; set; }
}

/// <summary>İndirilecek ek.</summary>
public sealed record DestekEkIcerigi(string DosyaAdi, string MimeTipi, byte[] Icerik);

/// <summary>Atama listesindeki kullanıcı.</summary>
public sealed record DestekAtanabilirKullanici(int Id, string AdSoyad, string? RolAdi, bool DepartmanUyesi, bool YazilimEkibi);

/// <summary>Destek modülünün genel ayarları (DestekAyarlari tablosunda anahtar–değer olarak saklanır).</summary>
public sealed class DestekGenelAyarlar
{
    public const string DestekEmailAnahtari = "DestekEmail";
    public const string VarsayilanDepartmanAnahtari = "VarsayilanDepartmanId";
    public const string YazilimDepartmaniAnahtari = "YazilimEkibiDepartmanId";
    public const string EmailBildirimAnahtari = "EmailBildirimAktif";
    public const string OtomatikSlaAnahtari = "OtomatikSlaKontrolu";
    public const string BilgiBankasiAnahtari = "BilgiBankasiAcik";
    public const string OtomatikKapatmaAnahtari = "OtomatikKapatmaGun";
    public const string DosyaBoyutuAnahtari = "MaksimumDosyaBoyutuMB";
    public const string DosyaTipleriAnahtari = "IzinliDosyaTipleri";

    public const string VarsayilanDosyaTipleri = ".jpg,.jpeg,.png,.gif,.webp,.pdf,.doc,.docx,.xls,.xlsx,.txt,.log,.zip";

    public string? DestekEmail { get; set; }

    /// <summary>"Sorun Bildir" talepleri ve departman seçilmeyen talepler buraya gelir.</summary>
    public int? VarsayilanDepartmanId { get; set; }

    /// <summary>"Yazılım Ekibine Aktar" ile taleplerin taşındığı departman.</summary>
    public int? YazilimEkibiDepartmanId { get; set; }

    public bool EmailBildirimAktif { get; set; }
    public bool OtomatikSlaKontrolu { get; set; } = true;
    public bool BilgiBankasiAcik { get; set; } = true;

    /// <summary>Çözüldü durumunda bu kadar gün yanıt gelmezse talep kendiliğinden kapanır (0 = kapatma).</summary>
    public int OtomatikKapatmaGun { get; set; } = 7;

    public int MaksimumDosyaBoyutuMB { get; set; } = 10;
    public string IzinliDosyaTipleri { get; set; } = VarsayilanDosyaTipleri;

    public IReadOnlyList<string> IzinliUzantilar() => IzinliDosyaTipleri
        .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(u => u.StartsWith('.') ? u.ToLowerInvariant() : "." + u.ToLowerInvariant())
        .Distinct()
        .ToList();
}

/// <summary>Bilgi bankası üst sayaçları.</summary>
public sealed record BilgiBankasiOzeti(int Toplam, int Yayinda, int Taslak, int ToplamGoruntulenme);
