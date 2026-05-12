using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services;

public interface IBelgeUyariService
{
    Task<BelgeUyariOzet> GetBelgeUyarilarAsync(int yaklasanGunSayisi = 30);
    Task<List<PersonelBelgeTabloKalemi>> GetPersonelBelgeTablosuAsync();
    Task<PersonelBelgeTabloKalemi?> GetTekPersonelBelgeAsync(int soforId);
    Task<bool> PersonelBelgeTarihGuncelleAsync(int soforId, string belgeAlani, DateTime? tarih);
    Task<byte[]> SeciliPersonelBelgelerZipAsync(List<int> soforIdler, List<string>? seciliDosyaYollari = null);
    Task<byte[]> PersonelBelgePdfAsync(int soforId);

    // Araç Belge Tablosu
    Task<List<AracBelgeTabloKalemi>> GetAracBelgeTablosuAsync();
    Task<AracBelgeTabloKalemi?> GetTekAracBelgeAsync(int aracId);
    Task<bool> AracBelgeTarihGuncelleAsync(int aracId, string belgeAlani, DateTime? bitisTarihi);
    Task<bool> AracBelgeDosyaYukleAsync(int aracId, string belgeAlani, string dosyaAdi, byte[] icerik);
    Task<byte[]> SeciliAracBelgelerZipAsync(List<int> aracIdler, List<string>? seciliDosyaYollari = null);

    // Tedarikçi Belge Tabloları
    Task<List<TedarikciEvrakTabloKalemi>> GetTedarikciEvrakTablosuAsync();
}

public class BelgeUyariOzet
{
    public int ToplamKritikUyari { get; set; }
    public int ToplamUyari { get; set; }

    // Personel Belgeleri
    public List<BelgeUyari> EhliyetUyarilari { get; set; } = new();
    public List<BelgeUyari> SrcUyarilari { get; set; } = new();
    public List<BelgeUyari> MykBelgesiUyarilari { get; set; } = new();
    public List<BelgeUyari> YayginEgitimUyarilari { get; set; } = new();
    public List<BelgeUyari> PsikoteknikUyarilari { get; set; } = new();
    public List<BelgeUyari> SaglikRaporuUyarilari { get; set; } = new();
    public List<BelgeUyari> DigerPersonelEvrakUyarilari { get; set; } = new();

    /// <summary>Diger kategorisindeki TUM ozluk evraklari (eksik, gecerli, suresi gecmis)</summary>
    public List<PersonelBelgeDetay> DigerTumPersonelBelgeler { get; set; } = new();

    // Arac Belgeleri
    public List<BelgeUyari> MuayeneUyarilari { get; set; } = new();
    public List<BelgeUyari> KaskoUyarilari { get; set; } = new();
    public List<BelgeUyari> TrafikSigortasiUyarilari { get; set; } = new();
    public List<BelgeUyari> DigerAracEvrakUyarilari { get; set; } = new();

    // Taşıma Tedarikçi Sözleşmeleri
    public List<BelgeUyari> TedarikciSozlesmeUyarilari { get; set; } = new();

    // Kiralık C Plaka sözleşme bitiş uyarıları
    public List<BelgeUyari> KiralikPlakaUyarilari { get; set; } = new();

    public List<BelgeUyari> TumUyarilar =>
        EhliyetUyarilari
         .Concat(SrcUyarilari)
         .Concat(MykBelgesiUyarilari)
        .Concat(YayginEgitimUyarilari)
        .Concat(PsikoteknikUyarilari)
        .Concat(SaglikRaporuUyarilari)
        .Concat(DigerPersonelEvrakUyarilari)
        .Concat(MuayeneUyarilari)
        .Concat(KaskoUyarilari)
        .Concat(TrafikSigortasiUyarilari)
        .Concat(DigerAracEvrakUyarilari)
        .Concat(TedarikciSozlesmeUyarilari)
        .Concat(KiralikPlakaUyarilari)
        .OrderBy(u => u.KalanGun)
        .ToList();
}

public class BelgeUyari
{
    public int Id { get; set; }
    public string Kaynak { get; set; } = string.Empty;
    public string Baslik { get; set; } = string.Empty;
    public string BelgeTuru { get; set; } = string.Empty;
    public DateTime BitisTarihi { get; set; }
    public string DetayUrl { get; set; } = string.Empty;

    // Tedarikçi bilgisi (alt yüklenici personel/aracı için doldurulur; kendi kaynağımızda null kalır)
    public int? TasimaTedarikciId { get; set; }
    public string? TasimaTedarikciUnvan { get; set; }
    public int KalanGun => (BitisTarihi - DateTime.Today).Days;
    public BelgeUyariSeviye Seviye => KalanGun switch
    {
        < 0 => BelgeUyariSeviye.Kritik,
        <= 7 => BelgeUyariSeviye.Acil,
        <= 30 => BelgeUyariSeviye.Uyari,
        _ => BelgeUyariSeviye.Bilgi
    };
    public string SeviyeClass => Seviye switch
    {
        BelgeUyariSeviye.Kritik => "bg-danger",
        BelgeUyariSeviye.Acil => "bg-warning text-dark",
        BelgeUyariSeviye.Uyari => "bg-info",
        _ => "bg-secondary"
    };
    public string Icon => Seviye switch
    {
        BelgeUyariSeviye.Kritik => "bi-exclamation-triangle-fill",
        BelgeUyariSeviye.Acil => "bi-exclamation-circle-fill",
        BelgeUyariSeviye.Uyari => "bi-info-circle-fill",
        _ => "bi-info-circle"
    };
}

public enum BelgeUyariSeviye
{
    Bilgi = 0,
    Uyari = 1,
    Acil = 2,
    Kritik = 3
}

/// <summary>
/// "Diger Onemli Belgeler" bolumu icin tam liste modeli
/// </summary>
public class PersonelBelgeDetay
{
    public int EvrakId { get; set; }
    public int SoforId { get; set; }
    public string PersonelAdi { get; set; } = string.Empty;
    public string PersonelKodu { get; set; } = string.Empty;
    public string EvrakAdi { get; set; } = string.Empty;
    public OzlukEvrakKategori Kategori { get; set; }
    public bool Tamamlandi { get; set; }
    public DateTime? TamamlanmaTarihi { get; set; }
    public DateTime? GecerlilikBitisTarihi { get; set; }
    public bool Zorunlu { get; set; }
    public string? DosyaYolu { get; set; }
    public string DetayUrl { get; set; } = string.Empty;

    public int? KalanGun => GecerlilikBitisTarihi.HasValue
        ? (GecerlilikBitisTarihi.Value - DateTime.Today).Days
        : null;

    public string DurumClass
    {
        get
        {
            if (!Tamamlandi) return "bg-secondary";
            if (GecerlilikBitisTarihi == null) return "bg-success";
            return KalanGun switch
            {
                < 0 => "bg-danger",
                <= 7 => "bg-warning text-dark",
                <= 30 => "bg-info",
                _ => "bg-success"
            };
        }
    }

    public string DurumMetin
    {
        get
        {
            if (!Tamamlandi) return "Eksik";
            if (GecerlilikBitisTarihi == null) return "Mevcut";
            return KalanGun switch
            {
                < 0 => $"{Math.Abs(KalanGun!.Value)} gun gecti",
                <= 30 => $"{KalanGun} gun kaldi",
                _ => "Gecerli"
            };
        }
    }
}

/// <summary>
/// Personel belge takip tablosu – her satır bir personel, sütunlar belge türleri
/// </summary>
public class PersonelBelgeTabloKalemi
{
    public int SoforId { get; set; }
    public string PersonelAdi { get; set; } = string.Empty;
    public string PersonelKodu { get; set; } = string.Empty;
    public string Gorev { get; set; } = string.Empty;
    public bool Aktif { get; set; }
    public bool Secili { get; set; } = false;

    // Özlük evrak dosyaları
    public int ToplamEvrakSayisi { get; set; }
    public int YuklenmisEvrakSayisi { get; set; }
    public List<OzlukEvrakDosyaBilgisi> EvrakDosyalari { get; set; } = new();

    // Belge tarihleri
    public DateTime? EhliyetGecerlilik { get; set; }
    public DateTime? KimlikGecerlilik { get; set; }
    public DateTime? MykBelgesiGecerlilik { get; set; }
    public bool SrcBelgesiVarMi { get; set; }
    public DateTime? YayginEgitimGecerlilik { get; set; }
    public bool YayginEgitimSertifikasiVarMi { get; set; }
    public DateTime? PsikoteknikGecerlilik { get; set; }
    public DateTime? AdliSicilGecerlilik { get; set; }
    public DateTime? SaglikRaporuGecerlilik { get; set; }
    public DateTime? SuruculCezaBarkodGecerlilik { get; set; }

    // Tedarikçi bilgisi (doldurulursa tedarikçiye ait personel)
    public int? TasimaTedarikciId { get; set; }
    public string? TasimaTedarikciUnvan { get; set; }

    // Yardımcı: belge durumu rengi
    public static string BelgeDurumClass(DateTime? tarih) => tarih == null ? "bg-secondary"
        : (tarih.Value - DateTime.Today).Days switch
        {
            < 0 => "bg-danger",
            <= 7 => "bg-warning text-dark",
            <= 30 => "bg-info",
            _ => "bg-success"
        };

    public static string BelgeDurumMetin(DateTime? tarih) => tarih == null ? "Yok"
        : (tarih.Value - DateTime.Today).Days switch
        {
            var d when d < 0 => $"{Math.Abs(d)}g geçti",
            var d when d <= 30 => $"{d}g kaldı",
            _ => tarih.Value.ToString("dd.MM.yy")
        };
}

public class OzlukEvrakDosyaBilgisi
{
    public int EvrakTanimId { get; set; }
    public string EvrakAdi { get; set; } = string.Empty;
    public string? DosyaYolu { get; set; }
    public string? DosyaAdi { get; set; }
    public bool DosyaVar => !string.IsNullOrEmpty(DosyaYolu);
    public bool Secili { get; set; } = false;
}

/// <summary>
/// Araç belge takip tablosu – her satır bir araç, sütunlar belge türleri (Sigorta, Muayene, Uygunluk, Koltuk Sigortası, Kasko)
/// </summary>
public class AracBelgeTabloKalemi
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string SaseNo { get; set; } = string.Empty;
    public string MarkaModel { get; set; } = string.Empty;
    public AracTipi AracTipi { get; set; }
    public AracSahiplikTipi SahiplikTipi { get; set; }
    public bool Aktif { get; set; }
    public bool Secili { get; set; } = false;

    // Belge dosyaları (AracEvrak'tan)
    public int ToplamEvrakSayisi { get; set; }
    public int YuklenmisEvrakSayisi { get; set; }
    public List<AracEvrakDosyaBilgisi> EvrakDosyalari { get; set; } = new();

    // Belge tarihleri
    public bool RuhsatVarMi { get; set; }
    public DateTime? RuhsatGecerlilik { get; set; }
    public DateTime? SigortaGecerlilik { get; set; }   // Trafik Sigortası
    public DateTime? MuayeneGecerlilik { get; set; }
    public DateTime? UygunlukGecerlilik { get; set; }
    public DateTime? KoltukSigortasiGecerlilik { get; set; }
    public DateTime? KaskoGecerlilik { get; set; }

    // Tedarikçi bilgisi (doldurulursa tedarikçiye ait araç)
    public int? TasimaTedarikciId { get; set; }
    public string? TasimaTedarikciUnvan { get; set; }

    public static string BelgeDurumClass(DateTime? tarih) => tarih == null ? "bg-secondary"
        : (tarih.Value - DateTime.Today).Days switch
        {
            < 0 => "bg-danger",
            <= 7 => "bg-warning text-dark",
            <= 30 => "bg-info",
            _ => "bg-success"
        };

    public static string BelgeDurumMetin(DateTime? tarih) => tarih == null ? "Yok"
        : (tarih.Value - DateTime.Today).Days switch
        {
            var d when d < 0 => $"{Math.Abs(d)}g geçti",
            var d when d <= 30 => $"{d}g kaldı",
            _ => tarih.Value.ToString("dd.MM.yy")
        };
}

public class AracEvrakDosyaBilgisi
{
    public int AracEvrakId { get; set; }
    public string EvrakKategorisi { get; set; } = string.Empty;
    public string? EvrakAdi { get; set; }
    public string? DosyaYolu { get; set; }
    public string? DosyaAdi { get; set; }
    public bool DosyaVar => !string.IsNullOrEmpty(DosyaYolu);
    public bool Secili { get; set; } = false;
}

/// <summary>
/// Tedarikçi firma belge takip tablosu – her satır bir tedarikçi, sütunlar TedarikciEvrakKategorileri
/// </summary>
public class TedarikciEvrakTabloKalemi
{
    public int TedarikciId { get; set; }
    public string TedarikciUnvan { get; set; } = string.Empty;
    public bool Aktif { get; set; }

    /// <summary>Kategori adı → bitiş tarihi eşlemesi (TedarikciEvrakKategorileri sabitleri)</summary>
    public Dictionary<string, DateTime?> Belgeler { get; set; } = new();

    public static string BelgeDurumClass(DateTime? tarih) => tarih == null ? "bg-secondary"
        : (tarih.Value - DateTime.Today).Days switch
        {
            < 0 => "bg-danger",
            <= 7 => "bg-warning text-dark",
            <= 30 => "bg-info",
            _ => "bg-success"
        };

    public static string BelgeDurumMetin(DateTime? tarih) => tarih == null ? "Yok"
        : (tarih.Value - DateTime.Today).Days switch
        {
            var d when d < 0 => $"{Math.Abs(d)}g geçti",
            var d when d <= 30 => $"{d}g kaldı",
            _ => tarih.Value.ToString("dd.MM.yy")
        };
}


