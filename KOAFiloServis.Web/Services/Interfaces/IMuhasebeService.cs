using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Models;

namespace KOAFiloServis.Web.Services;

public interface IMuhasebeService
{
    // Hesap Plani
    Task<List<MuhasebeHesap>> GetHesapPlaniAsync();
    Task<List<MuhasebeHesap>> GetHesaplarByGrupAsync(HesapGrubu grup);
    Task<MuhasebeHesap?> GetHesapByKodAsync(string hesapKodu);
    Task<MuhasebeHesap?> GetHesapByIdAsync(int id);
    Task<MuhasebeHesap> CreateHesapAsync(MuhasebeHesap hesap);
    Task<MuhasebeHesap> UpdateHesapAsync(MuhasebeHesap hesap);
    Task DeleteHesapAsync(int id);
    Task SeedVarsayilanHesapPlaniAsync();
    Task<bool> HesapIslemGormusMuAsync(int hesapId);
    Task<HesapPlaniImportResult> ImportHesapPlaniFromExcelAsync(byte[] fileContent);
    Task<byte[]> GetHesapPlaniSablonAsync();

    // Muhasebe Fisleri
    Task<List<MuhasebeFis>> GetFislerAsync(int yil, int? ay = null);
    Task<List<MuhasebeFis>> GetFislerByTipAsync(FisTipi tip, int yil, int? ay = null);
    Task<MuhasebeFis?> GetFisByIdAsync(int id);
    Task<MuhasebeFis> CreateFisAsync(MuhasebeFis fis);
    Task<MuhasebeFis> UpdateFisAsync(MuhasebeFis fis);
    Task DeleteFisAsync(int id);
    Task<string> GenerateNextFisNoAsync(FisTipi tip, int firmaId = 0);
    /// <summary>Kilitli olarak FisNo üretir ve fişi kaydeder. Duplicate key hatasını önler.</summary>
    Task<MuhasebeFis> CreateFisAtomicAsync(MuhasebeFis fis);
    Task OnayliFisAsync(int fisId);
    Task OnayGeriAlFisAsync(int fisId);

    // Otomatik Fis Olusturma
    Task<MuhasebeFis> CreateFaturaFisiAsync(Fatura fatura);
    Task<MuhasebeFis> CreateTahsilatFisiAsync(BankaKasaHareket hareket, int faturaId);
    Task<MuhasebeFis> CreateTediyeFisiAsync(BankaKasaHareket hareket, int? faturaId = null);

    // Mahsup Fişi Oluşturma
    Task<MuhasebeFis?> CreateHesapTransferFisiAsync(BankaKasaHareket cikisHareket, BankaKasaHareket girisHareket, BankaHesap kaynakHesap, BankaHesap hedefHesap);
    Task<MuhasebeFis?> CreateCariMahsupFisiAsync(BankaKasaHareket hareket, Cari cari, BankaHesap hesap, bool tahsilatMi);
    Task IptalFisiOlusturAsync(Guid mahsupGrupId);

    // Donemler
    Task<List<MuhasebeDonem>> GetDonemlerAsync(int yil);
    Task<MuhasebeDonem?> GetAktifDonemAsync();
    Task DonemKapatAsync(int donemId);

    // Raporlar
    Task<MuavinRapor> GetMuavinRaporuAsync(string hesapKodu, DateTime baslangic, DateTime bitis);
    Task<YevmiyeRapor> GetYevmiyeRaporuAsync(DateTime baslangic, DateTime bitis);
    Task<GelirGiderRapor> GetGelirGiderRaporuAsync(int yil, int? ay = null);
    Task<BilancoRapor> GetBilancoRaporuAsync(DateTime tarih);
    Task<MizanRapor> GetMizanRaporuAsync(DateTime baslangic, DateTime bitis);

    // Yevmiye Excel Export
    Task<byte[]> ExportYevmiyeToExcelAsync(DateTime baslangic, DateTime bitis);
    Task<byte[]> GetYevmiyeYazdirDataAsync(DateTime baslangic, DateTime bitis);

    // Zirve Muhasebe Programı Export
    Task<byte[]> ExportZirveFormatAsync(DateTime baslangic, DateTime bitis);
    Task<byte[]> ExportMuhasebeKontrolListesiAsync(DateTime baslangic, DateTime bitis);

    // KDV Beyanname Raporları
    Task<KdvBeyanRapor> GetKdvBeyanRaporuAsync(int yil, int ay);
    Task<List<KdvAylikOzet>> GetYillikKdvOzetiAsync(int yil);

    // Nakit Akış Raporu
    Task<NakitAkisRapor> GetNakitAkisRaporuAsync(int yil, int? ay = null);

    // Hesap Bakiyeleri
    Task<decimal> GetHesapBakiyeAsync(string hesapKodu, DateTime? tarih = null);
    Task<List<HesapBakiye>> GetHesapBakiyeleriAsync(HesapGrubu grup, DateTime? tarih = null);

    // Toplu Muhasebeleştirme
    Task<MuhasbelestirmeDurum> GetMuhasbelestirmeDurumuAsync();
    Task<List<MuhasebeFaturaOzet>> GetMuhasbelestirilmemisFaturalarAsync(DateTime? baslangic = null, DateTime? bitis = null, FaturaYonu? faturaYonu = null);
    Task<List<MuhasebeMasrafOzet>> GetMuhasbelestirilmemisMasraflarAsync(DateTime? baslangic = null, DateTime? bitis = null);
    Task<MuhasbelestirmeSonuc> TopluFaturaMuhasbelestirAsync(List<int> faturaIdleri);
    Task<MuhasbelestirmeSonuc> TopluMasrafMuhasbelestirAsync(List<int> masrafIdleri);

    // Muhasebeleştirme Kontrol & Gelişmiş
    Task<MuhasbelestirmeKontrol> KontrolYapAsync(List<int>? faturaIdleri = null, List<int>? masrafIdleri = null);
    Task<List<MuhasbelestirilmisKayit>> GetMuhasbelestirilmisKayitlarAsync(DateTime? baslangic = null, DateTime? bitis = null, string? kaynakTip = null);
    Task<MuhasbelestirmeSonuc> TopluGeriAlAsync(List<int> fisIdleri);
    Task<byte[]> ExportMuhasbelestirmeKontrolExcelAsync(List<int>? faturaIdleri = null, List<int>? masrafIdleri = null);
}

// Hesap Plani Import Result
public class HesapPlaniImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

#region Rapor Modelleri

public class MuavinRapor
{
    public string HesapKodu { get; set; } = "";
    public string HesapAdi { get; set; } = "";
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public decimal DevirBorc { get; set; }
    public decimal DevirAlacak { get; set; }
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal Bakiye { get; set; }
    public List<MuavinSatir> Satirlar { get; set; } = new();
}

public class MuavinSatir
{
    public DateTime Tarih { get; set; }
    public string FisNo { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; }
}

public class YevmiyeRapor
{
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public List<YevmiyeSatir> Satirlar { get; set; } = new();
}

public class YevmiyeSatir
{
    public int SiraNo { get; set; }
    public DateTime Tarih { get; set; }
    public string FisNo { get; set; } = "";
    public string HesapKodu { get; set; } = "";
    public string HesapAdi { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
}

public class GelirGiderRapor
{
    public int Yil { get; set; }
    public int? Ay { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal NetKar { get; set; }
    public List<GelirGiderKalem> Gelirler { get; set; } = new();
    public List<GelirGiderKalem> Giderler { get; set; } = new();
    public List<AylikGelirGider> AylikDetay { get; set; } = new();
}

public class GelirGiderKalem
{
    public string HesapKodu { get; set; } = "";
    public string HesapAdi { get; set; } = "";
    public decimal Tutar { get; set; }
    public decimal Yuzde { get; set; }
}

public class AylikGelirGider
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = "";
    public decimal Gelir { get; set; }
    public decimal Gider { get; set; }
    public decimal Net { get; set; }
}

public class BilancoRapor
{
    public DateTime Tarih { get; set; }
    public decimal ToplamAktif { get; set; }
    public decimal ToplamPasif { get; set; }

    // Aktif Kalemler
    public List<BilancoKalem> DonenVarliklar { get; set; } = new();
    public List<BilancoKalem> DuranVarliklar { get; set; } = new();

    // Pasif Kalemler
    public List<BilancoKalem> KisaVadeliYabanciKaynaklar { get; set; } = new();
    public List<BilancoKalem> UzunVadeliYabanciKaynaklar { get; set; } = new();
    public List<BilancoKalem> Ozkaynaklar { get; set; } = new();
}

public class BilancoKalem
{
    public string HesapKodu { get; set; } = "";
    public string HesapAdi { get; set; } = "";
    public decimal Tutar { get; set; }
    public bool AltHesapVar { get; set; }
    public List<BilancoKalem> AltKalemler { get; set; } = new();
}

public class MizanRapor
{
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal ToplamBorcBakiye { get; set; }
    public decimal ToplamAlacakBakiye { get; set; }
    public List<MizanSatir> Satirlar { get; set; } = new();
}

public class MizanSatir
{
    public string HesapKodu { get; set; } = "";
    public string HesapAdi { get; set; } = "";
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal BorcBakiye { get; set; }
    public decimal AlacakBakiye { get; set; }
}

public class HesapBakiye
{
    public string HesapKodu { get; set; } = "";
    public string HesapAdi { get; set; } = "";
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; }
}

// KDV Beyanname Rapor Modelleri
public class KdvBeyanRapor
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi { get; set; } = "";

    // Satış (Hesaplanan KDV)
    public decimal SatisTutari { get; set; }
    public decimal HesaplananKdv { get; set; }
    public List<KdvOranDetay> HesaplananKdvDetay { get; set; } = new();

    // Alış (İndirilecek KDV)
    public decimal AlisTutari { get; set; }
    public decimal IndirilecekKdv { get; set; }
    public List<KdvOranDetay> IndirilecekKdvDetay { get; set; } = new();

    // Tevkifat
    public decimal TevkifatKdv { get; set; }
    public decimal TevkifatliSatisKdv { get; set; }

    // Devreden KDV
    public decimal DevredenKdv { get; set; }

    // Hesaplama
    public decimal ToplamIndirimler { get; set; }
    public decimal FarkKdv { get; set; }
    public decimal OdenecekKdv { get; set; }
    public decimal SonrakiAyaDevredenKdv { get; set; }
}

public class KdvOranDetay
{
    public decimal KdvOrani { get; set; }
    public decimal Matrah { get; set; }
    public decimal KdvTutar { get; set; }
}

public class KdvAylikOzet
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = "";
    public decimal HesaplananKdv { get; set; }
    public decimal IndirilecekKdv { get; set; }
    public decimal TevkifatKdv { get; set; }
    public decimal DevredenKdv { get; set; }
    public decimal OdenecekKdv { get; set; }
    public decimal SonrakiAyaDevreden { get; set; }
}

// Nakit Akış Rapor Modelleri
public class NakitAkisRapor
{
    public int Yil { get; set; }
    public int? Ay { get; set; }
    public decimal DonemBasiBakiye { get; set; }
    public decimal ToplamGiris { get; set; }
    public decimal ToplamCikis { get; set; }
    public decimal DonemSonuBakiye { get; set; }
    public List<NakitHareketDetay> GirisDetay { get; set; } = new();
    public List<NakitHareketDetay> CikisDetay { get; set; } = new();
    public List<NakitAylikOzet> AylikDetay { get; set; } = new();
}

public class NakitHareketDetay
{
    public string Tur { get; set; } = "";
    public decimal Tutar { get; set; }
}

public class NakitAylikOzet
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = "";
    public decimal BaslangicBakiye { get; set; }
    public decimal Giris { get; set; }
    public decimal Cikis { get; set; }
    public decimal SonBakiye { get; set; }
}

#endregion
