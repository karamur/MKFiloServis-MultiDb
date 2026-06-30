using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Models;

/// <summary>
/// İhale hazırlık AI maliyet tahmin istek modeli
/// </summary>
public class IhaleMaliyetTahminIstek
{
    public string AracModel { get; set; } = string.Empty;
    public int AracModelYili { get; set; }
    public int KoltukSayisi { get; set; }
    public decimal MesafeKm { get; set; }
    public int GunlukSeferSayisi { get; set; }
    public int AylikCalismGunu { get; set; }
    public AracSahiplikKalem SahiplikDurumu { get; set; }
    public decimal YakitTuketimi { get; set; } // lt/100km
    public decimal YakitFiyati { get; set; }
    public int SozlesmeSuresiAy { get; set; }
    public decimal EnflasyonOrani { get; set; }
}

/// <summary>
/// AI maliyet tahmin sonucu
/// </summary>
public class IhaleMaliyetTahminSonuc
{
    public decimal TahminiAylikBakim { get; set; }
    public decimal TahminiAylikLastik { get; set; }
    public decimal TahminiAylikSigorta { get; set; }
    public decimal TahminiAylikKasko { get; set; }
    public decimal TahminiAylikMuayene { get; set; }
    public decimal TahminiAylikYedekParca { get; set; }
    public decimal TahminiAylikDigerMasraf { get; set; }
    public string? AIAciklama { get; set; }
    public bool Basarili { get; set; }
}

/// <summary>
/// AI şoför maaş tahmin sonucu
/// </summary>
public class IhaleSoforMaasTahmin
{
    public decimal TahminiBrutMaas { get; set; }
    public decimal TahminiNetMaas { get; set; }
    public decimal TahminiSGKIsverenPay { get; set; }
    public decimal TahminiToplamMaliyet { get; set; }
    public decimal EnflasyonluBrutMaas { get; set; } // Proje sonu tahmini
    public string? AIAciklama { get; set; }
    public bool Basarili { get; set; }
}

/// <summary>
/// İhale proje özet rapor
/// </summary>
public class IhaleProjeOzet
{
    public int ProjeId { get; set; }
    public string ProjeKodu { get; set; } = string.Empty;
    public string ProjeAdi { get; set; } = string.Empty;
    public string? MusteriFirma { get; set; }
    public int SozlesmeSuresiAy { get; set; }
    public int GuzergahSayisi { get; set; }
    public int AracSayisi { get; set; }

    // Maliyet özet
    public decimal ToplamAylikYakit { get; set; }
    public decimal ToplamAylikAracMasraf { get; set; }
    public decimal ToplamAylikSoforMaliyet { get; set; }
    public decimal ToplamAylikKiraKomisyon { get; set; }
    public decimal ToplamAylikAmortisman { get; set; }
    public decimal ToplamAylikMaliyet { get; set; }

    // Kar/Zarar
    public decimal ToplamAylikKar { get; set; }
    public decimal ToplamAylikTeklifFiyati { get; set; }
    public decimal KarMarjiOrtalama { get; set; }

    // Proje toplam
    public decimal ToplamProjeMaliyeti { get; set; }
    public decimal ToplamProjeKar { get; set; }
    public decimal ToplamProjeTeklif { get; set; }

    // Birim fiyatlar
    public decimal OrtalamaSeferBasiMaliyet { get; set; }
    public decimal OrtalamaSaatlikMaliyet { get; set; }
    public decimal OrtalamaKmBasiMaliyet { get; set; }

    // Hat detayları
    public List<IhaleKalemOzet> KalemOzetleri { get; set; } = [];

    // Enflasyonlu projeksiyon
    public List<AylikProjeksiyonOzet> AylikProjeksiyonlar { get; set; } = [];
}

/// <summary>
/// Hat bazlı özet
/// </summary>
public class IhaleKalemOzet
{
    public int KalemId { get; set; }
    public string HatAdi { get; set; } = string.Empty;
    public decimal MesafeKm { get; set; }
    public string SahiplikDurumu { get; set; } = string.Empty;
    public string AracBilgi { get; set; } = string.Empty;
    public decimal AylikMaliyet { get; set; }
    public decimal AylikTeklifFiyati { get; set; }
    public decimal KarMarji { get; set; }
    public decimal SeferBasiTeklif { get; set; }
}

/// <summary>
/// Aylık projeksiyon özet
/// </summary>
public class AylikProjeksiyonOzet
{
    public int Ay { get; set; }
    public string DonemAdi { get; set; } = string.Empty; // "Ocak 2025"
    public decimal ToplamMaliyet { get; set; }
    public decimal ToplamKar { get; set; }
    public decimal ToplamTeklif { get; set; }
    public decimal KumulatifMaliyet { get; set; }
    public decimal KumulatifKar { get; set; }
    public decimal EnflasyonEtkisi { get; set; } // Enflasyon farkı
}

public class IhaleTeklifKarsilastirmaDto
{
    public int SolVersiyonId { get; set; }
    public string SolRevizyonKodu { get; set; } = string.Empty;
    public int SagVersiyonId { get; set; }
    public string SagRevizyonKodu { get; set; } = string.Empty;

    public decimal SolToplamMaliyet { get; set; }
    public decimal SagToplamMaliyet { get; set; }
    public decimal ToplamMaliyetFarki { get; set; }

    public decimal SolTeklifTutari { get; set; }
    public decimal SagTeklifTutari { get; set; }
    public decimal TeklifTutariFarki { get; set; }

    public decimal SolKarMarjiTutari { get; set; }
    public decimal SagKarMarjiTutari { get; set; }
    public decimal KarMarjiTutariFarki { get; set; }

    public decimal SolKarMarjiOrani { get; set; }
    public decimal SagKarMarjiOrani { get; set; }
    public decimal KarMarjiOraniFarki { get; set; }
}

public class IhaleGerceklesenAnalizOzet
{
    public int ProjeId { get; set; }
    public DateTime AnalizBaslangicTarihi { get; set; }
    public DateTime AnalizBitisTarihi { get; set; }
    public int GecenAySayisi { get; set; }
    public int ToplamGerceklesenSeferSayisi { get; set; }

    public decimal PlanlananToplamMaliyet { get; set; }
    public decimal GerceklesenToplamMaliyet { get; set; }
    public decimal MaliyetSapmasi { get; set; }
    public decimal MaliyetSapmaOrani { get; set; }

    public decimal PlanlananToplamTeklif { get; set; }
    public decimal GerceklesenToplamGelir { get; set; }
    public decimal GelirSapmasi { get; set; }
    public decimal GelirSapmaOrani { get; set; }

    public decimal PlanlananToplamKar { get; set; }
    public decimal GerceklesenToplamKar { get; set; }
    public decimal KarSapmasi { get; set; }
    public decimal KarSapmaOrani { get; set; }
    public int AktifSozlesmeRevizyonSayisi { get; set; }
    public decimal ToplamRevizyonBedelFarki { get; set; }
    public int ToplamRevizyonSureFarkiAy { get; set; }
    public decimal RevizyonEtkiliPlanlananTeklif { get; set; }
    public decimal RevizyonEtkiliGelirSapmasi { get; set; }
    public decimal TeklifDogrulukSkoru { get; set; }
    public string RiskSeviyesi { get; set; } = "Dusuk";

    public List<IhaleGerceklesenKalemAnalizi> Kalemler { get; set; } = [];
    public List<IhaleSozlesmeRevizyonEtkisiOzet> AktifRevizyonlar { get; set; } = [];
}

public class IhaleGerceklesenKalemAnalizi
{
    public int KalemId { get; set; }
    public string HatAdi { get; set; } = string.Empty;
    public string SahiplikDurumu { get; set; } = string.Empty;
    public int GecenAySayisi { get; set; }
    public int GerceklesenSeferSayisi { get; set; }

    public decimal PlanlananMaliyet { get; set; }
    public decimal GerceklesenMaliyet { get; set; }
    public decimal MaliyetSapmasi { get; set; }

    public decimal PlanlananTeklif { get; set; }
    public decimal GerceklesenGelir { get; set; }
    public decimal GelirSapmasi { get; set; }

    public decimal PlanlananKar { get; set; }
    public decimal GerceklesenKar { get; set; }
    public decimal KarSapmasi { get; set; }
    public decimal TeklifDogrulukSkoru { get; set; }
    public string RiskSeviyesi { get; set; } = "Dusuk";
}

public class IhaleSozlesmeRevizyonEtkisiOzet
{
    public string RevizyonNo { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
    public string Baslik { get; set; } = string.Empty;
    public DateTime RevizyonTarihi { get; set; }
    public DateTime? YurutmeTarihi { get; set; }
    public decimal BedelFarki { get; set; }
    public int SureFarkiAy { get; set; }
}

public class IhaleOperasyonDashboardOzet
{
    public int KazanilanProjeSayisi { get; set; }
    public int AnalizEdilenProjeSayisi { get; set; }
    public int RiskliProjeSayisi { get; set; }
    public int RevizyonluProjeSayisi { get; set; }
    public int SureUzatimliProjeSayisi { get; set; }
    public decimal OrtalamaTeklifDogrulukSkoru { get; set; }
    public decimal ToplamKarSapmasi { get; set; }
    public decimal EnKotusuKarSapmasi { get; set; }
    public decimal ToplamRevizyonBedelFarki { get; set; }
    public List<IhaleRiskliProjeOzet> RiskliProjeler { get; set; } = [];
}

public class IhaleRiskliProjeOzet
{
    public int ProjeId { get; set; }
    public string ProjeKodu { get; set; } = string.Empty;
    public string ProjeAdi { get; set; } = string.Empty;
    public decimal TeklifDogrulukSkoru { get; set; }
    public decimal KarSapmasi { get; set; }
    public decimal MaliyetSapmaOrani { get; set; }
    public int AktifRevizyonSayisi { get; set; }
    public decimal ToplamRevizyonBedelFarki { get; set; }
    public string RiskSeviyesi { get; set; } = "Dusuk";
}



