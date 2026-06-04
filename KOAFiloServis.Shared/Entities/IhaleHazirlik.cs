using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// İhale Hazırlık Projesi - Proje bazlı maliyet analizi
/// </summary>
public class IhaleProje : BaseEntity, IFirmaTenant
{
    [Required]
    [StringLength(50)]
    public string ProjeKodu { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string ProjeAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    // Müşteri/Kurum bilgisi
    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }

    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    // Sözleşme süresi
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public int SozlesmeSuresiAy { get; set; } // Ay cinsinden

    // Proje durumu
    public IhaleProjeDurum Durum { get; set; } = IhaleProjeDurum.Taslak;

    // Enflasyon parametreleri
    public decimal EnflasyonOrani { get; set; } = 25; // Yıllık %
    public decimal YakitZamOrani { get; set; } = 30; // Yıllık yakıt zam oranı %

    // Genel parametreler
    public int AylikCalismGunu { get; set; } = 22;
    public int GunlukCalismaSaati { get; set; } = 8;

    // AI Notları
    public string? AIAnaliz { get; set; }
    public DateTime? AIAnalizTarihi { get; set; }

    public string? Notlar { get; set; }

    // Toplamlar (hesaplanan)
    [NotMapped]
    public decimal ToplamAylikMaliyet => Kalemler?.Sum(k => k.AylikMaliyet) ?? 0;
    [NotMapped]
    public decimal ToplamProjemaliyeti => Kalemler?.Sum(k => k.ToplamMaliyet) ?? 0;

    // Navigation
    public virtual ICollection<IhaleGuzergahKalem> Kalemler { get; set; } = new List<IhaleGuzergahKalem>();
    public virtual ICollection<IhaleTeklifVersiyon> TeklifVersiyonlari { get; set; } = new List<IhaleTeklifVersiyon>();
    public virtual ICollection<IhaleSozlesmeRevizyon> SozlesmeRevizyonlari { get; set; } = new List<IhaleSozlesmeRevizyon>();
    public virtual ICollection<IhaleRakipBenchmark> RakipBenchmarklar { get; set; } = new List<IhaleRakipBenchmark>();
}

/// <summary>
/// Rakip/piyasa teklif benchmark kaydı
/// </summary>
public class IhaleRakipBenchmark : BaseEntity
{
    public int IhaleProjeId { get; set; }
    public virtual IhaleProje IhaleProje { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string RakipFirmaAdi { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Aciklama { get; set; }

    // Teklif tutarları
    public decimal? AylikTeklifTutari { get; set; }
    public decimal? ToplamTeklifTutari { get; set; }

    // Piyasa verileri
    public decimal? PiyasaOrtalamaFiyati { get; set; }
    public decimal? MinPiyasaFiyati { get; set; }
    public decimal? MaxPiyasaFiyati { get; set; }

    // Kaynak bilgisi
    [StringLength(200)]
    public string? VeriKaynagi { get; set; } // "Geçmiş ihale", "Piyasa araştırması" vb.

    public DateTime? VeritarihiTarihi { get; set; }

    public string? Notlar { get; set; }

    // Bizim teklifimizle karşılaştırma (NotMapped)
    [NotMapped]
    public decimal? FarkYuzdesi { get; set; }
}

public class IhaleSozlesmeRevizyon : BaseEntity
{
    public int IhaleProjeId { get; set; }
    public virtual IhaleProje IhaleProje { get; set; } = null!;

    public IhaleSozlesmeRevizyonTipi RevizyonTipi { get; set; } = IhaleSozlesmeRevizyonTipi.SozlesmeRevizyonu;

    [Required]
    [StringLength(50)]
    public string RevizyonNo { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string Baslik { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Aciklama { get; set; }

    public DateTime RevizyonTarihi { get; set; } = DateTime.Today;
    public DateTime? YurutmeTarihi { get; set; }
    public decimal BedelFarki { get; set; }
    public int SureFarkiAy { get; set; }
    public bool Aktif { get; set; } = true;
}

/// <summary>
/// İhale güzergah/hat kalemi - Her hat için maliyet detayı
/// </summary>
public class IhaleGuzergahKalem : BaseEntity
{
    public int IhaleProjeId { get; set; }
    public virtual IhaleProje IhaleProje { get; set; } = null!;

    // Güzergah bilgisi
    public int? GuzergahId { get; set; }
    public virtual Guzergah? Guzergah { get; set; }

    [Required]
    [StringLength(300)]
    public string HatAdi { get; set; } = string.Empty;

    public string? BaslangicNoktasi { get; set; }
    public string? BitisNoktasi { get; set; }

    public decimal MesafeKm { get; set; }
    public int TahminiSureDakika { get; set; }

    // Sefer bilgisi
    public SeferTipi SeferTipi { get; set; } = SeferTipi.SabahAksam;
    public int GunlukSeferSayisi { get; set; } = 2; // Sabah-Akşam = 2
    public int AylikSeferGunu { get; set; } = 22;
    public int PersonelSayisi { get; set; }

    // Araç bilgisi
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    public AracSahiplikKalem SahiplikDurumu { get; set; } = AracSahiplikKalem.Ozmal;
    public string? AracModelBilgi { get; set; } // örn: "2022 Mercedes Sprinter"
    public int AracKoltukSayisi { get; set; } = 27;
    public decimal YakitTuketimi { get; set; } = 18; // lt/100km

    // Yakıt maliyeti
    public decimal YakitFiyati { get; set; } = 42; // TL/lt
    public decimal GunlukYakitMaliyeti { get; set; }
    public decimal AylikYakitMaliyeti { get; set; }

    // Araç masrafları (AI tahmin veya kullanıcı giriş)
    public decimal AylikBakimMasrafi { get; set; }
    public decimal AylikLastikMasrafi { get; set; }
    public decimal AylikSigortaMasrafi { get; set; }
    public decimal AylikKaskoMasrafi { get; set; }
    public decimal AylikMuayeneMasrafi { get; set; }
    public decimal AylikYedekParcaMasrafi { get; set; }
    public decimal AylikDigerMasraf { get; set; }

    // Kira/Komisyon
    public decimal AylikKiraBedeli { get; set; }
    public decimal SeferBasiKomisyon { get; set; }
    public decimal AylikKomisyonToplam { get; set; }

    // Şoför maaş
    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }
    public decimal SoforBrutMaas { get; set; }
    public decimal SoforNetMaas { get; set; }
    public decimal SoforSGKIsverenPay { get; set; }
    public decimal SoforToplamMaliyet { get; set; } // Brüt + SGK İşveren

    // Amortisman (özmal için)
    public decimal AracDegeri { get; set; }
    public int AmortismanYili { get; set; } = 5;
    public decimal AylikAmortisman { get; set; }

    // Birim fiyatlar (hesaplanan)
    public decimal AylikMaliyet { get; set; }
    public decimal ToplamMaliyet { get; set; } // Proje süresi boyunca
    public decimal SeferBasiMaliyet { get; set; }
    public decimal SaatlikMaliyet { get; set; }
    public decimal KmBasiMaliyet { get; set; }

    // Kâr marjı
    public decimal KarMarjiOrani { get; set; } = 15; // %
    public decimal AylikKarTutari { get; set; }

    // Teklif fiyatı
    public decimal AylikTeklifFiyati { get; set; }
    public decimal SeferBasiTeklifFiyati { get; set; }
    public decimal SaatlikTeklifFiyati { get; set; }

    // AI tahmin sonuçları
    public bool AITahminiKullanildi { get; set; }
    public string? AITahminDetay { get; set; }

    // Enflasyonlu projeksiyonlar (NotMapped - hesaplanacak)
    [NotMapped]
    public List<AylikProjeksiyon>? EnflasyonluProjeksiyonlar { get; set; }
}

/// <summary>
/// Aylık projeksiyon (enflasyonlu)
/// </summary>
public class AylikProjeksiyon
{
    public int Ay { get; set; }
    public int Yil { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public decimal YakitMaliyeti { get; set; }
    public decimal AracMasrafi { get; set; }
    public decimal SoforMaliyeti { get; set; }
    public decimal KiraKomisyon { get; set; }
    public decimal Amortisman { get; set; }
    public decimal ToplamMaliyet { get; set; }
    public decimal KarTutari { get; set; }
    public decimal TeklifFiyati { get; set; }
    public decimal EnflasyonCarpani { get; set; } = 1;
}

public enum IhaleProjeDurum
{
    Taslak = 0,
    Hazirlaniyor = 1,
    TeklifVerildi = 2,
    Kazanildi = 3,
    Kaybedildi = 4,
    IptalEdildi = 5
}

public enum AracSahiplikKalem
{
    Ozmal = 1,
    Kiralik = 2,
    Komisyon = 3
}

public enum IhaleSozlesmeRevizyonTipi
{
    SozlesmeRevizyonu = 1,
    EkProtokol = 2,
    FiyatFarki = 3,
    SureUzatimi = 4
}
