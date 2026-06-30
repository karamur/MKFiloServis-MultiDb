namespace MKFiloServis.Web.Models;

/// <summary>
/// Mali Analiz Dashboard i�in model
/// </summary>
public class MaliAnalizDashboard
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi => new DateTime(Yil, Ay, 1).ToString("MMMM yyyy");

    // �zet Kartlar
    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal NetKar => ToplamGelir - ToplamGider;
    public decimal KarlilikOrani => ToplamGelir > 0 ? (NetKar / ToplamGelir) * 100 : 0;

    // �nceki ay kar��la�t�rma
    public decimal OncekiAyGelir { get; set; }
    public decimal OncekiAyGider { get; set; }
    public decimal GelirDegisimOrani => OncekiAyGelir > 0 ? ((ToplamGelir - OncekiAyGelir) / OncekiAyGelir) * 100 : 0;
    public decimal GiderDegisimOrani => OncekiAyGider > 0 ? ((ToplamGider - OncekiAyGider) / OncekiAyGider) * 100 : 0;

    // Segment Bazlı Analiz
    public SegmentAnaliz OzmalAracAnaliz { get; set; } = new();
    public SegmentAnaliz KiralikAracAnaliz { get; set; } = new();
    public SegmentAnaliz KomisyonAnaliz { get; set; } = new();
    public SegmentAnaliz TasimaTedarikciAnaliz { get; set; } = new();

    // Grafik Verileri
    public List<GrafikVeri> AylikTrend { get; set; } = new();
    public List<GrafikVeri> GelirDagilimi { get; set; } = new();
    public List<GrafikVeri> GiderDagilimi { get; set; } = new();
    public List<GrafikVeri> EnKarliGuzergahlar { get; set; } = new();
    public List<GrafikVeri> AracBazliKarlilik { get; set; } = new();
}

/// <summary>
/// Segment bazl� analiz (�zmal, Kiral�k, Komisyon)
/// </summary>
public class SegmentAnaliz
{
    public string SegmentAdi { get; set; } = string.Empty;
    public decimal Gelir { get; set; }
    public decimal Gider { get; set; }
    public decimal Kar => Gelir - Gider;
    public decimal KarlilikOrani => Gelir > 0 ? (Kar / Gelir) * 100 : 0;
    public int SeferSayisi { get; set; }
    public int AracSayisi { get; set; }
    public List<SegmentDetay> Detaylar { get; set; } = new();
}

/// <summary>
/// Segment detay�
/// </summary>
public class SegmentDetay
{
    public string Aciklama { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public bool GelirMi { get; set; }
}

/// <summary>
/// Grafik verisi
/// </summary>
public class GrafikVeri
{
    public string Etiket { get; set; } = string.Empty;
    public decimal Deger { get; set; }
    public string? Renk { get; set; }
    public string? EkBilgi { get; set; }
}

/// <summary>
/// �zmal Ara� Rapor Modeli
/// </summary>
public class OzmalAracRaporu
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public List<OzmalAracDetay> AracDetaylari { get; set; } = new();

    public decimal ToplamGelir => AracDetaylari.Sum(x => x.ToplamGelir);
    public decimal ToplamGider => AracDetaylari.Sum(x => x.ToplamGider);
    public decimal NetKar => ToplamGelir - ToplamGider;
    public int ToplamSefer => AracDetaylari.Sum(x => x.SeferSayisi);
}

/// <summary>
/// �zmal ara� detay�
/// </summary>
public class OzmalAracDetay
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string? Marka { get; set; }
    public string? Model { get; set; }

    // �of�r bilgisi
    public string? AtananSofor { get; set; }

    // Gelirler
    public decimal SeferGeliri { get; set; }
    public decimal EkGelirler { get; set; }
    public decimal ToplamGelir => SeferGeliri + EkGelirler;

    // Giderler
    public decimal SoforMasrafi { get; set; }
    public decimal AkaryakitMasrafi { get; set; }
    public decimal BakimMasrafi { get; set; }
    public decimal SigortaMasrafi { get; set; }
    public decimal DigerMasraflar { get; set; }
    public decimal ToplamGider => SoforMasrafi + AkaryakitMasrafi + BakimMasrafi + SigortaMasrafi + DigerMasraflar;

    // Sonu�
    public decimal NetKar => ToplamGelir - ToplamGider;
    public decimal KarlilikOrani => ToplamGelir > 0 ? (NetKar / ToplamGelir) * 100 : 0;
    public int SeferSayisi { get; set; }

    // �al���lan g�zergahlar
    public List<GuzergahOzet> CalistigiGuzergahlar { get; set; } = new();
}

/// <summary>
/// G�zergah �zeti
/// </summary>
public class GuzergahOzet
{
    public int GuzergahId { get; set; }
    public string GuzergahAdi { get; set; } = string.Empty;
    public string MusteriUnvan { get; set; } = string.Empty;
    public int SeferSayisi { get; set; }
    public decimal Gelir { get; set; }
}

/// <summary>
/// Kiral�k Ara� Rapor Modeli
/// </summary>
public class KiralikAracRaporu
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public List<KiralikFirmaDetay> FirmaDetaylari { get; set; } = new();

    public decimal ToplamMusteriddenAlinacak => FirmaDetaylari.Sum(x => x.MusteriddenAlinacak);
    public decimal ToplamFirmayaOdenecek => FirmaDetaylari.Sum(x => x.FirmayaOdenecek);
    public decimal NetKar => ToplamMusteriddenAlinacak - ToplamFirmayaOdenecek;
    public int ToplamSefer => FirmaDetaylari.Sum(x => x.ToplamSefer);
}

/// <summary>
/// Kiral�k firma detay�
/// </summary>
public class KiralikFirmaDetay
{
    public int FirmaId { get; set; }
    public string FirmaUnvan { get; set; } = string.Empty;
    public string FirmaKodu { get; set; } = string.Empty;

    public List<KiralikAracDetay> AracDetaylari { get; set; } = new();

    public decimal MusteriddenAlinacak => AracDetaylari.Sum(x => x.MusteridenAlinacak);
    public decimal FirmayaOdenecek => AracDetaylari.Sum(x => x.FirmayaOdenecek);
    public decimal Kar => MusteriddenAlinacak - FirmayaOdenecek;
    public int ToplamSefer => AracDetaylari.Sum(x => x.SeferSayisi);
}

/// <summary>
/// Kiral�k ara� detay�
/// </summary>
public class KiralikAracDetay
{
    public string Plaka { get; set; } = string.Empty;
    public string? SoforAdSoyad { get; set; }
    public List<KiralikGuzergahDetay> GuzergahDetaylari { get; set; } = new();

    public decimal MusteridenAlinacak => GuzergahDetaylari.Sum(x => x.MusteridenAlinacak);
    public decimal FirmayaOdenecek => GuzergahDetaylari.Sum(x => x.FirmayaOdenecek);
    public decimal Kar => MusteridenAlinacak - FirmayaOdenecek;
    public int SeferSayisi => GuzergahDetaylari.Sum(x => x.SeferSayisi);
}

/// <summary>
/// Kiral�k g�zergah detay�
/// </summary>
public class KiralikGuzergahDetay
{
    public string GuzergahAdi { get; set; } = string.Empty;
    public string MusteriUnvan { get; set; } = string.Empty;
    public int SeferSayisi { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal KiraBedeli { get; set; }
    public decimal MusteridenAlinacak { get; set; }
    public decimal FirmayaOdenecek { get; set; }
}

/// <summary>
/// Checklist �zet modeli
/// </summary>
public class ChecklistOzet
{
    public int Yil { get; set; }
    public int Ay { get; set; }

    public List<SoforChecklistOzet> SoforChecklists { get; set; } = new();
    public List<AracChecklistOzet> AracChecklists { get; set; } = new();
    public List<GuzergahChecklistOzet> GuzergahChecklists { get; set; } = new();

    public int ToplamKritik => SoforChecklists.Count(x => x.GenelDurum == "Kritik") +
                               AracChecklists.Count(x => x.GenelDurum == "Kritik") +
                               GuzergahChecklists.Count(x => x.GenelDurum == "Kritik");

    public int ToplamUyari => SoforChecklists.Count(x => x.GenelDurum == "Uyari") +
                              AracChecklists.Count(x => x.GenelDurum == "Uyari") +
                              GuzergahChecklists.Count(x => x.GenelDurum == "Uyari");
}

/// <summary>
/// �of�r checklist �zeti
/// </summary>
public class SoforChecklistOzet
{
    public int SoforId { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string SoforKodu { get; set; } = string.Empty;

    public string EhliyetDurum { get; set; } = "Bekliyor";
    public DateTime? EhliyetBitisTarihi { get; set; }

    public string SrcDurum { get; set; } = "Bekliyor";
    public DateTime? SrcBitisTarihi { get; set; }

    public string PsikoteknikDurum { get; set; } = "Bekliyor";
    public DateTime? PsikoteknikBitisTarihi { get; set; }

    public string SaglikDurum { get; set; } = "Bekliyor";
    public DateTime? SaglikRaporuBitisTarihi { get; set; }

    public string GenelDurum { get; set; } = "Bekliyor";
}

/// <summary>
/// Ara� checklist �zeti
/// </summary>
public class AracChecklistOzet
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string? MarkaModel { get; set; }

    public string MuayeneDurum { get; set; } = "Bekliyor";
    public DateTime? MuayeneBitisTarihi { get; set; }

    public string SigortaDurum { get; set; } = "Bekliyor";
    public DateTime? SigortaBitisTarihi { get; set; }

    public string KaskoDurum { get; set; } = "Bekliyor";
    public DateTime? KaskoBitisTarihi { get; set; }

    public string BakimDurum { get; set; } = "Bekliyor";
    public int? SonrakiBakimKm { get; set; }

    public string GenelDurum { get; set; } = "Bekliyor";
}

/// <summary>
/// G�zergah checklist �zeti
/// </summary>
public class GuzergahChecklistOzet
{
    public int GuzergahId { get; set; }
    public string GuzergahAdi { get; set; } = string.Empty;
    public string MusteriUnvan { get; set; } = string.Empty;

    public string SozlesmeDurum { get; set; } = "Bekliyor";
    public DateTime? SozlesmeBitisTarihi { get; set; }

    public string FiyatDurum { get; set; } = "Bekliyor";
    public DateTime? SonFiyatGuncellemeTarihi { get; set; }

    public string SeferDurum { get; set; } = "Bekliyor";
    public int PlanlananSefer { get; set; }
    public int GerceklesenSefer { get; set; }

    public string OdemeDurum { get; set; } = "Bekliyor";
    public decimal BekleyenOdeme { get; set; }

    public string GenelDurum { get; set; } = "Bekliyor";
}

/// <summary>
/// Taşıma Tedarikçisi (Alt Yüklenici) Rapor Modeli.
/// Kaynak: Sofor.TasimaTedarikciId / Arac.TasimaTedarikciId üzerinden tek kaynak.
/// </summary>
public class TasimaTedarikciRaporu
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public List<TasimaTedarikciDetay> TedarikciDetaylari { get; set; } = new();

    public decimal ToplamMusteridenAlinacak => TedarikciDetaylari.Sum(x => x.MusteridenAlinacak);
    public decimal ToplamTedarikciyeOdenecek => TedarikciDetaylari.Sum(x => x.TedarikciyeOdenecek);
    public decimal NetKar => ToplamMusteridenAlinacak - ToplamTedarikciyeOdenecek;
    public int ToplamSefer => TedarikciDetaylari.Sum(x => x.ToplamSefer);
    public int ToplamArac => TedarikciDetaylari.Sum(x => x.AracSayisi);
    public int ToplamPersonel => TedarikciDetaylari.Sum(x => x.PersonelSayisi);
}

/// <summary>
/// Tek bir tedarikçinin (alt yüklenici) ay bazlı finans özeti.
/// </summary>
public class TasimaTedarikciDetay
{
    public int TedarikciId { get; set; }
    public string TedarikciKodu { get; set; } = string.Empty;
    public string Unvan { get; set; } = string.Empty;
    public int? CariId { get; set; }

    public int AracSayisi { get; set; }
    public int PersonelSayisi { get; set; }
    public int AktifIsSayisi { get; set; }

    public List<TasimaTedarikciIsDetay> IsDetaylari { get; set; } = new();

    public decimal MusteridenAlinacak => IsDetaylari.Sum(x => x.MusteridenAlinacak);
    public decimal TedarikciyeOdenecek => IsDetaylari.Sum(x => x.TedarikciyeOdenecek);
    public decimal Kar => MusteridenAlinacak - TedarikciyeOdenecek;
    public decimal KarlilikOrani => MusteridenAlinacak > 0 ? (Kar / MusteridenAlinacak) * 100 : 0;
    public int ToplamSefer => IsDetaylari.Sum(x => x.SeferSayisi);
}

/// <summary>
/// Tedarikçinin bir güzergah/iş bazındaki ay özeti.
/// </summary>
public class TasimaTedarikciIsDetay
{
    public int? IsId { get; set; }
    public string GuzergahAdi { get; set; } = string.Empty;
    public string MusteriUnvan { get; set; } = string.Empty;
    public string? AracPlaka { get; set; }
    public string? SoforAdSoyad { get; set; }

    public int SeferSayisi { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal SeferUcreti { get; set; }

    /// <summary>Müşteriden tahsil edilecek (güzergah birim fiyatı * sefer).</summary>
    public decimal MusteridenAlinacak { get; set; }

    /// <summary>Tedarikçiye ödenecek (sözleşme sefer ücreti * sefer + aylık ücret).</summary>
    public decimal TedarikciyeOdenecek { get; set; }

    public decimal Kar => MusteridenAlinacak - TedarikciyeOdenecek;
}



