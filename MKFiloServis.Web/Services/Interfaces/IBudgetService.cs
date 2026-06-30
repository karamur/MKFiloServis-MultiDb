using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IBudgetService
{
    // Odeme Islemleri
    Task<List<BudgetOdeme>> GetOdemelerAsync(int yil, int? ay = null, int? firmaId = null);
    Task<List<BudgetOdeme>> GetBekleyenOdemelerAsync(int yil, int? ay = null);
    Task<List<BudgetOdeme>> GetDevirBekleyenOdemelerAsync(DateTime donemBaslangic, int? firmaId = null);
    Task<List<BudgetOdeme>> GetOdemelerByDateRangeAsync(DateTime baslangic, DateTime bitis);
    Task<BudgetOdeme?> GetOdemeByIdAsync(int id);
    Task<BudgetOdeme?> GetOdemeByHareketIdAsync(int bankaKasaHareketId);
    Task<BudgetOdeme> CreateOdemeAsync(BudgetOdeme odeme);
    Task<BudgetOdeme> UpdateOdemeAsync(BudgetOdeme odeme);
    Task DeleteOdemeAsync(int id); // Soft delete
    Task HardDeleteOdemeAsync(int id); // Kalici silme
    Task<BudgetOdeme> OdemeYapAsync(int odemeId, OdemeYapRequest request); // Kasa=Borc, Odeme=Alacak
    Task<BudgetOdeme> OdemeGeriAlAsync(int odemeId); // Odeme geri alma - hareket siler

    // Fatura ile kapatma
    Task<BudgetOdeme> FaturaIleKapatAsync(int odemeId, int faturaId);

    // Taksitli Odeme Islemleri
    Task<List<BudgetOdeme>> CreateTaksitliOdemeAsync(TaksitliOdemeRequest request);
    Task<List<BudgetOdeme>> GetTaksitGrubuAsync(Guid taksitGrupId);
    Task UpdateTaksitGrubuAsync(List<BudgetOdeme> taksitler);
    
    // Excel Islemleri
    Task<byte[]> GetExcelSablonAsync(List<Firma> firmalar);
    Task<int> ImportFromExcelAsync(byte[] fileContent);

    // Masraf Kalemleri
    Task<List<BudgetMasrafKalemi>> GetMasrafKalemleriAsync();
    Task<BudgetMasrafKalemi> CreateMasrafKalemiAsync(BudgetMasrafKalemi kalem);
    Task<BudgetMasrafKalemi> UpdateMasrafKalemiAsync(BudgetMasrafKalemi kalem);
    Task DeleteMasrafKalemiAsync(int id);
    Task SeedMasrafKalemleriAsync();

    // Raporlar
    Task<BudgetOzet> GetAylikOzetAsync(int yil, int ay, int? firmaId = null);
    Task<BudgetOzet> GetPeriyodOzetAsync(DateTime baslangic, DateTime bitis);
    Task<BudgetYillikOzet> GetYillikOzetAsync(int yil, int? firmaId = null);
    Task<List<BudgetGunlukOzet>> GetTakvimDataAsync(int yil, int ay, int? firmaId = null);
    Task<List<BudgetKategoriOzet>> GetKategoriOzetAsync(int yil, int? ay = null);
    Task<List<BudgetKategoriOzet>> GetKategoriOzetByDateRangeAsync(DateTime baslangic, DateTime bitis);
    Task<List<BudgetTrendData>> GetTrendDataAsync(DateTime baslangic, DateTime bitis, string periyod);
    
    // Kredi/Taksit Raporlari
    Task<List<KrediOzet>> GetAktifKredilerAsync(int? firmaId = null);
    Task<List<KrediOzet>> GetKrediOzetleriAsync(int? yil = null, int? firmaId = null);
    Task<List<KrediTaksitDetay>> GetKrediTaksitDetaylariAsync(Guid taksitGrupId);
    Task<BudgetOdeme?> GetTaksitOdemeAsync(Guid taksitGrupId, int taksitNo);
    Task<List<AylikKrediTaksitRapor>> GetAylikKrediTaksitRaporuAsync(int yil);
    Task OdemeYapAsync(int odemeId, int bankaHesapId, DateTime odemeTarihi);

    // Kredi Karti Islemleri
    Task AddKrediKartiBorcAsync(int bankaHesapId, decimal tutar, int ay, int yil, string aciklama);
    Task<List<BudgetOdeme>> GetKrediKartiHareketleriAsync(int bankaHesapId, int? yil = null);

    // Tekrarlayan Odeme Islemleri
    Task<List<TekrarlayanOdeme>> GetTekrarlayanOdemelerAsync(int? firmaId = null);
    Task<List<TekrarlayanOdeme>> GetAktifTekrarlayanOdemelerAsync(int? firmaId = null);
    Task<TekrarlayanOdeme?> GetTekrarlayanOdemeByIdAsync(int id);
    Task<TekrarlayanOdeme> CreateTekrarlayanOdemeAsync(TekrarlayanOdeme odeme);
    Task<TekrarlayanOdeme> UpdateTekrarlayanOdemeAsync(TekrarlayanOdeme odeme);
    Task DeleteTekrarlayanOdemeAsync(int id);
    Task<int> TekrarlayanOdemelerdenKayitOlusturAsync(int yil, int ay, int? firmaId = null);

    // Hedef Yönetimi
    Task<List<BudgetHedef>> GetHedeflerAsync(int yil, int? ay = null, int? firmaId = null);
    Task<BudgetHedef?> GetHedefByIdAsync(int id);
    Task<BudgetHedef> CreateHedefAsync(BudgetHedef hedef);
    Task<BudgetHedef> UpdateHedefAsync(BudgetHedef hedef);
    Task DeleteHedefAsync(int id);
    Task<int> KopyalaHedeflerAsync(int kaynakYil, int hedefYil, decimal artisOrani = 0);

    // Hedef vs Gerçekleşen Karşılaştırma
    Task<List<BudgetHedefGerceklesen>> GetHedefGerceklesenAsync(int yil, int? ay = null, int? firmaId = null);
    Task<BudgetYillikHedefOzet> GetYillikHedefOzetAsync(int yil, int? firmaId = null);

    // Kısmi Ödeme İşlemleri
    Task<BudgetOdeme> KismiOdemeYapAsync(int odemeId, KismiOdemeRequest request);
    Task<BudgetOdeme?> KalanTutariSonrakiDonemeAktarAsync(int odemeId, int? hedefAy = null, int? hedefYil = null);
    Task<List<BudgetOdeme>> GetKismiOdenmislerAsync(int yil, int? ay = null, int? firmaId = null);

    // Risk Analizi
    Task<BudgetRiskAnalizi> GetRiskAnaliziAsync(int yil, int? ay = null, int? firmaId = null);

    // Veri Tutarlılık
    Task<int> TemizleMukerrerKrediKartiBorclariAsync();
}

public class TaksitliOdemeRequest
{
    public DateTime BaslangicTarihi { get; set; }
    public string MasrafKalemi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public decimal ToplamTutar { get; set; }
    public int TaksitSayisi { get; set; }
    public string? Notlar { get; set; }
    public int? FirmaId { get; set; }
    public int? BagliBankaHesapId { get; set; }
    public decimal? KrediAnaParaTutari { get; set; }
    public decimal? KrediNetGecenTutar { get; set; }
    public decimal PesinFaizTutari { get; set; }
    public decimal PesinMasrafTutari { get; set; }
    public List<TaksitDetayRequest> TaksitPlani { get; set; } = new();
}

public class TaksitDetayRequest
{
    public int Sira { get; set; }
    public DateTime Tarih { get; set; }
    public decimal Tutar { get; set; }
}

public class OdemeYapRequest
{
    public MKFiloServis.Shared.Entities.BudgetOdemeTipi OdemeTipi { get; set; }
    public int? BankaHesapId { get; set; }
    public string? Aciklama { get; set; }
    public DateTime OdemeTarihi { get; set; } = DateTime.Today;
    public decimal? KismiOdemeTutari { get; set; }
    public Guid? KrediTaksitGrupId { get; set; } // Kredi kartı için ilişkili kredi
    public int? KrediKartiOdemeAy { get; set; }
    public int? KrediKartiOdemeYil { get; set; }

    // Cari Mahsup için
    public int? CariId { get; set; }
    public bool CaridenTahsilat { get; set; } = false; // true: cariden tahsilat, false: cariye ödeme

    // Ek masraf bilgileri (masraf, ceza, komisyon - tutara eklenir)
    public decimal MasrafKesintisi { get; set; } = 0;
    public decimal CezaKesintisi { get; set; } = 0;
    public decimal DigerKesinti { get; set; } = 0;
    public string? KesintiAciklamasi { get; set; }
    public string? OdemeNotu { get; set; }

    // Muhasebe Eşleştirme
    public string? MuhasebeHesapKodu { get; set; }
    public string? KostMerkeziKodu { get; set; }
    public string? ProjeKodu { get; set; }

    // Hesaplanan (Ek masraflar tutara eklenir - her zaman pozitif)
    public decimal ToplamEkMasraf => Math.Abs(MasrafKesintisi) + Math.Abs(CezaKesintisi) + Math.Abs(DigerKesinti);
}

public class BudgetOzet
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public decimal ToplamOdeme { get; set; }
    public decimal OdenenToplam { get; set; }
    public decimal BekleyenToplam { get; set; }
    public int ToplamKayit { get; set; }
    public int OdenenKayit { get; set; }
    public int BekleyenKayit { get; set; }
    public List<BudgetKategoriOzet> KategoriOzetleri { get; set; } = new();
}

public class BudgetYillikOzet
{
    public int Yil { get; set; }
    public decimal ToplamOdeme { get; set; }
    public List<BudgetAylikToplam> AylikToplamlar { get; set; } = new();
    public List<BudgetKategoriOzet> KategoriOzetleri { get; set; } = new();
}

public class BudgetAylikToplam
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public decimal Toplam { get; set; }
    public decimal Odenen { get; set; }
    public decimal Bekleyen { get; set; }
}

public class BudgetGunlukOzet
{
    public DateTime Tarih { get; set; }
    public int Gun { get; set; }
    public decimal ToplamOdeme { get; set; }
    public int OdemeSayisi { get; set; }
    public decimal BekleyenToplamOdeme { get; set; }
    public int BekleyenOdemeSayisi { get; set; }
    public List<BudgetOdeme> Odemeler { get; set; } = new();
}

public class BudgetKategoriOzet
{
    public string MasrafKalemi { get; set; } = string.Empty;
    public string? Renk { get; set; }
    public decimal Toplam { get; set; }
    public int Adet { get; set; }
    public decimal Yuzde { get; set; }
}

public class BudgetTrendData
{
    public string Etiket { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public decimal Toplam { get; set; }
    public decimal Odenen { get; set; }
    public decimal Bekleyen { get; set; }
    public int OdemeSayisi { get; set; }
}

public class ExcelImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<BudgetOdeme> ImportedItems { get; set; } = new();
}

// Kredi/Taksit Rapor Modelleri
public class KrediOzet
{
    public Guid TaksitGrupId { get; set; }
    public string MasrafKalemi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public int ToplamTaksitSayisi { get; set; }
    public int OdenenTaksitSayisi { get; set; }
    public int KalanTaksitSayisi { get; set; }
    public decimal TaksitTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar { get; set; }
    public decimal TamamlanmaYuzdesi { get; set; }
    public DateTime? SonrakiTaksitTarihi { get; set; }
}

public class AylikKrediTaksitRapor
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public decimal ToplamTaksitTutari { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal BekleyenTutar { get; set; }
    public int TaksitSayisi { get; set; }
    public List<KrediTaksitDetay> Taksitler { get; set; } = new();
}

public class KrediTaksitDetay
{
    public string MasrafKalemi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public int KacinciTaksit { get; set; }
    public int ToplamTaksitSayisi { get; set; }
    public decimal Tutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar { get; set; }
    public decimal OdemeYuzdesi { get; set; }
    public OdemeDurum Durum { get; set; }
    public DateTime OdemeTarihi { get; set; }
}

// Kısmi Ödeme Request
public class KismiOdemeRequest
{
    public decimal OdenecekTutar { get; set; }
    public DateTime OdemeTarihi { get; set; } = DateTime.Today;
    public int? BankaHesapId { get; set; }
    public MKFiloServis.Shared.Entities.BudgetOdemeTipi OdemeTipi { get; set; } = MKFiloServis.Shared.Entities.BudgetOdemeTipi.Kasa;
    public string? Aciklama { get; set; }
    public bool KalanSonrakiDonemeAktarilsin { get; set; } = false;
    public int? HedefAy { get; set; }
    public int? HedefYil { get; set; }

    // Cari Mahsup için
    public int? CariId { get; set; }

    // Ek masraflar
    public decimal MasrafKesintisi { get; set; } = 0;
    public decimal CezaKesintisi { get; set; } = 0;
    public decimal DigerKesinti { get; set; } = 0;
    public string? KesintiAciklamasi { get; set; }
}

// Risk Analizi Modeli
public class BudgetRiskAnalizi
{
    public int Yil { get; set; }
    public int? Ay { get; set; }
    public DateTime AnalizTarihi { get; set; } = DateTime.Now;

    // Genel Özet
    public decimal ToplamBekleyen { get; set; }
    public decimal ToplamGeciken { get; set; }
    public decimal ToplamKismiOdenen { get; set; }
    public int ToplamKayit { get; set; }
    public int GecikenKayit { get; set; }
    public int KismiOdenenKayit { get; set; }

    // Risk Skorları (0-100)
    public decimal GenelRiskSkoru { get; set; }
    public decimal LikiditeRiski { get; set; }
    public decimal OdemeGecikmesiRiski { get; set; }
    public decimal BudceSapmaRiski { get; set; }

    // Detaylar
    public List<RiskliOdeme> RiskliOdemeler { get; set; } = new();
    public List<KategoriRiskOzeti> KategoriRiskleri { get; set; } = new();
    public Dictionary<string, List<KategoriOdemeItem>> KategoriOdemeleri { get; set; } = new();
    public List<AylikRiskTrendi> AylikTrendler { get; set; } = new();
    public List<string> Uyarilar { get; set; } = new();
    public List<string> Oneriler { get; set; } = new();
}

public class RiskliOdeme
{
    public int OdemeId { get; set; }
    public string MasrafKalemi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public decimal Tutar { get; set; }
    public decimal KalanTutar { get; set; }
    public DateTime VadeTarihi { get; set; }
    public int GecikmeGunu { get; set; }
    public string RiskSeviyesi { get; set; } = "Normal"; // Normal, Orta, Yüksek, Kritik
    public string? RiskAciklamasi { get; set; }
}

public class KategoriOdemeItem
{
    public int OdemeId { get; set; }
    public string? Aciklama { get; set; }
    public decimal Miktar { get; set; }
    public decimal KalanTutar { get; set; }
    public DateTime OdemeTarihi { get; set; }
    public OdemeDurum Durum { get; set; }
    public bool TaksitliMi { get; set; }
    public int GecikmeGunu { get; set; }
}

public class KategoriRiskOzeti
{
    public string Kategori { get; set; } = string.Empty;
    public decimal ToplamTutar { get; set; }
    public decimal BekleyenTutar { get; set; }
    public decimal GecikenTutar { get; set; }
    public int GecikenKayit { get; set; }
    public decimal RiskSkoru { get; set; }
}

public class AylikRiskTrendi
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public decimal BeklenenOdeme { get; set; }
    public decimal GerceklesenOdeme { get; set; }
    public decimal GecikenOdeme { get; set; }
    public decimal RiskSkoru { get; set; }
}



