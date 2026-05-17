using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Stok Karti - Urun/Hizmet/Arac/Servis tanimi
/// </summary>
public class StokKarti : BaseEntity, IFirmaTenant
{
    // Aşama C3 (K4): firma bazlı izolasyon. Nullable; C2 deseni gereği backfill sonrası NOT NULL'a alınacak.
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    [StringLength(50)]
    public string StokKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string StokAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    [StringLength(50)]
    public string? Barkod { get; set; }

    // Stok tipi
    public StokTipi StokTipi { get; set; } = StokTipi.Mal;
    public StokAltTipi? AltTipi { get; set; }

    // Kategori
    public int? KategoriId { get; set; }
    public virtual StokKategori? Kategori { get; set; }

    // Birim
    [StringLength(20)]
    public string Birim { get; set; } = "Adet";

    // Fiyatlar
    public decimal AlisFiyati { get; set; } = 0;
    public decimal SatisFiyati { get; set; } = 0;
    public decimal KdvOrani { get; set; } = 20;

    // Stok takibi
    public bool StokTakibiYapilsin { get; set; } = true;
    public decimal MinStokMiktari { get; set; } = 0;
    public decimal MaksStokMiktari { get; set; } = 0;

    // Mevcut stok miktari (hesaplanan)
    public decimal MevcutStok { get; set; } = 0;

    // Tedarikci
    public int? VarsayilanTedarikciId { get; set; }
    public virtual Cari? VarsayilanTedarikci { get; set; }

    // Muhasebe hesabi
    public int? MuhasebeHesapId { get; set; }
    public virtual MuhasebeHesap? MuhasebeHesap { get; set; }

    // Durum
    public bool Aktif { get; set; } = true;

    // Resim
    public string? ResimUrl { get; set; }

    // Notlar
    public string? Notlar { get; set; }

    // Navigation
    public virtual ICollection<StokHareket> Hareketler { get; set; } = new List<StokHareket>();
}

/// <summary>
/// Stok Kategorisi
/// </summary>
public class StokKategori : BaseEntity, IFirmaTenant
{
    // Aşama C3 (K4): firma bazlı izolasyon.
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    [StringLength(100)]
    public string KategoriAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public int? UstKategoriId { get; set; }
    public virtual StokKategori? UstKategori { get; set; }

    [StringLength(20)]
    public string? Renk { get; set; }

    [StringLength(50)]
    public string? Icon { get; set; }

    public int Sira { get; set; } = 0;
    public bool Aktif { get; set; } = true;

    // Navigation
    public virtual ICollection<StokKarti> StokKartlari { get; set; } = new List<StokKarti>();
    public virtual ICollection<StokKategori> AltKategoriler { get; set; } = new List<StokKategori>();
}

/// <summary>
/// Stok Hareketi - Giris/Cikis kayitlari
/// </summary>
public class StokHareket : BaseEntity, IFirmaTenant
{
    // Aşama C3 (K4): firma bazlı izolasyon.
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int StokKartiId { get; set; }
    public virtual StokKarti StokKarti { get; set; } = null!;

    public DateTime IslemTarihi { get; set; } = DateTime.Today;

    [StringLength(50)]
    public string? BelgeNo { get; set; }

    public StokHareketTipi HareketTipi { get; set; }

    // Miktar (pozitif: giris, negatif: cikis)
    public decimal Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    [NotMapped]
    public decimal ToplamTutar => Math.Abs(Miktar) * BirimFiyat;

    // Iliskili kayitlar
    public int? FaturaId { get; set; }
    public virtual Fatura? Fatura { get; set; }

    public int? FaturaKalemId { get; set; }
    public virtual FaturaKalem? FaturaKalem { get; set; }

    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }

    // Arac iliskisi (arac alim/satim veya servis icin)
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    // Arac masrafi iliskisi (servis girislerinde masraf karti olusturulur)
    public int? AracMasrafId { get; set; }
    public virtual AracMasraf? AracMasraf { get; set; }

    public string? Aciklama { get; set; }

    // Depo (ileride coklu depo destegi icin)
    public int? DepoId { get; set; }
}

/// <summary>
/// Arac Alim/Satim Kaydi
/// </summary>
public class AracIslem : BaseEntity
{
    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;

    public AracIslemTipi IslemTipi { get; set; }
    public DateTime IslemTarihi { get; set; } = DateTime.Today;

    // Cari (Kimden alindi / Kime satildi)
    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }

    // Fiyat bilgileri
    public decimal Tutar { get; set; }
    public decimal KdvOrani { get; set; } = 20;
    public decimal KdvTutar { get; set; }
    public decimal ToplamTutar { get; set; }

    // Fatura iliskisi
    public int? FaturaId { get; set; }
    public virtual Fatura? Fatura { get; set; }

    // Stok hareketi iliskisi
    public int? StokHareketId { get; set; }
    public virtual StokHareket? StokHareket { get; set; }

    // Ek bilgiler
    public string? Aciklama { get; set; }
    public string? Notlar { get; set; }

    // Kilometre
    public int? Kilometre { get; set; }

    // Belgeler
    public string? NoterId { get; set; }
    public DateTime? NoterTarihi { get; set; }
}

/// <summary>
/// Servis Kaydi - Alinan servis hizmetleri
/// </summary>
public class ServisKaydi : BaseEntity
{
    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;

    public DateTime ServisTarihi { get; set; } = DateTime.Today;

    // Servis veren firma
    public int? ServisciCariId { get; set; }
    public virtual Cari? ServisciCari { get; set; }

    public ServisTipi ServisTipi { get; set; }

    [StringLength(200)]
    public string ServisAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    // Tutarlar
    public decimal IscilikTutari { get; set; } = 0;
    public decimal ParcaTutari { get; set; } = 0;
    public decimal KdvOrani { get; set; } = 20;
    public decimal KdvTutar { get; set; }
    public decimal ToplamTutar { get; set; }

    // Kilometre
    public int? Kilometre { get; set; }

    // Fatura iliskisi
    public int? FaturaId { get; set; }
    public virtual Fatura? Fatura { get; set; }

    // Arac Masraf iliskisi (otomatik olusturulur)
    public int? AracMasrafId { get; set; }
    public virtual AracMasraf? AracMasraf { get; set; }

    // Stok Hareket iliskisi
    public int? StokHareketId { get; set; }
    public virtual StokHareket? StokHareket { get; set; }

    // Durum
    public ServisDurum Durum { get; set; } = ServisDurum.Tamamlandi;

    // Garanti
    public bool GarantiKapsaminda { get; set; } = false;
    public DateTime? GarantiBitisTarihi { get; set; }

    // Notlar
    public string? Notlar { get; set; }

    // KDV manuel düzeltme
    public bool KdvManuelMi { get; set; } = false;

    // Navigation - Servis parcalari
    public virtual ICollection<ServisParca> Parcalar { get; set; } = new List<ServisParca>();
}

/// <summary>
/// Servis Parcasi - Serviste kullanilan parcalar / iscilik kalemleri
/// </summary>
public class ServisParca : BaseEntity
{
    public int ServisKaydiId { get; set; }
    public virtual ServisKaydi ServisKaydi { get; set; } = null!;

    // Stok karti (varsa)
    public int? StokKartiId { get; set; }
    public virtual StokKarti? StokKarti { get; set; }

    // Kalem tipi: Malzeme veya Iscilik
    public ServisKalemTipi KalemTipi { get; set; } = ServisKalemTipi.Malzeme;

    [StringLength(200)]
    public string ParcaAdi { get; set; } = string.Empty;

    public decimal Miktar { get; set; } = 1;

    [StringLength(20)]
    public string Birim { get; set; } = "Adet";

    public decimal BirimFiyat { get; set; }
    [NotMapped]
    public decimal ToplamTutar => Miktar * BirimFiyat;

    // KDV orani (kalem bazli)
    public decimal KdvOrani { get; set; } = 20;
    [NotMapped]
    public decimal KdvTutar => ToplamTutar * KdvOrani / 100;

    // Stoğa kaydet (sadece malzeme kalemleri icin)
    public bool StogaKaydet { get; set; } = false;

    public string? Aciklama { get; set; }
}

#region Enums

public enum StokTipi
{
    Mal = 1,            // Ticari mal
    Hizmet = 2,         // Hizmet
    Arac = 3,           // Arac (sase bazli)
    YedekParca = 4,     // Yedek parca
    SarfMalzeme = 5,    // Sarf malzeme
    Demirbas = 6,       // Demirbas
    Diger = 99
}

public enum StokAltTipi
{
    // Mal
    TicariMal = 101,
    Hammadde = 102,
    YariMamul = 103,
    Mamul = 104,

    // Hizmet
    TasimaHizmeti = 201,
    ServisHizmeti = 202,
    DanismanlikHizmeti = 203,
    KiralamaHizmeti = 204,

    // Arac
    AracMinibus = 301,
    AracMidibus = 302,
    AracOtobus = 303,
    AracOtomobil = 304,
    AracPanelvan = 305,

    // Yedek Parca
    MotorParcasi = 401,
    FrenParcasi = 402,
    ElektrikParcasi = 403,
    KaporParcasi = 404,
    LastikJant = 405,

    // Sarf Malzeme
    Yakit = 501,
    Yag = 502,
    Antifriz = 503,
    CamSuyu = 504,

    Diger = 999
}

public enum StokHareketTipi
{
    Giris = 1,              // Stok girisi
    Cikis = 2,              // Stok cikisi
    SatisIade = 3,          // Satis iadesi (giris)
    AlisIade = 4,           // Alis iadesi (cikis)
    SayimFazlasi = 5,       // Sayim fazlasi (giris)
    SayimNoksani = 6,       // Sayim noksani (cikis)
    FireZayiat = 7,         // Fire/Zayiat (cikis)
    StokZarari = 8,         // Stok zarari (cikis)
    DepoTransferi = 9,      // Depo transferi
    AracAlis = 10,          // Arac alimi (giris)
    AracSatis = 11,         // Arac satisi (cikis)
    ServisGiris = 20,       // Servis hizmeti alindi
    UretimGiris = 30,       // Uretim girisi
    UretimCikis = 31,       // Uretim cikisi (hammadde)
    Diger = 99
}

public enum AracIslemTipi
{
    Alis = 1,
    Satis = 2,
    Takas = 3,
    TakasGiris = 4,
    TakasCikis = 5,
    Devir = 6
}

public enum ServisTipi
{
    PeriyodikBakim = 1,
    Onarim = 2,
    Kasko = 3,
    TrafikSigortasi = 4,
    Muayene = 5,
    LastikDegisimi = 6,
    YagDegisimi = 7,
    FrenBakimi = 8,
    KlimaBakimi = 9,
    MotorBakimi = 10,
    KaportaBoya = 11,
    CamDegisimi = 12,
    AkuDegisimi = 13,
    Cekici = 14,
    Diger = 99
}

public enum ServisDurum
{
    Beklemede = 1,
    Devam = 2,
    Tamamlandi = 3,
    IptalEdildi = 4
}

public enum ServisKalemTipi
{
    Malzeme = 1,
    Iscilik = 2
}

#endregion
