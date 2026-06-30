using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IBordroService
{
    // Bordro Listeleme
    Task<List<Bordro>> GetBordrolarAsync(int? firmaId = null, int? yil = null, BordroTipi? tip = null);
    Task<Bordro?> GetBordroByIdAsync(int id);
    Task<Bordro?> GetBordroByDönemAsync(int yil, int ay, int? firmaId, BordroTipi tip);
    
    // Bordro Oluşturma ve Hesaplama
    Task<Bordro> CreateBordroAsync(int yil, int ay, int? firmaId, BordroTipi tip);
    Task<bool> HesaplaBordroAsync(int bordroId);
    Task<bool> OnaylaBordroAsync(int bordroId, string onaylayanKullanici);
    Task<bool> OnayIptalEtAsync(int bordroId);
    Task<bool> DeleteBordroAsync(int bordroId);
    
    // Bordro Detay İşlemleri
    Task<List<BordroDetay>> GetBordroDetaylarAsync(int bordroId);
    Task<BordroDetay?> GetBordroDetayByIdAsync(int id);
    Task UpdateBordroDetayAsync(BordroDetay detay);
    Task<bool> SilBordroDetayAsync(int detayId);
    
    // Ödeme İşlemleri
    Task<bool> BankaOdemesiYapAsync(List<int> detayIds, DateTime odemeTarihi, int? bankaHesapId, string? aciklama);
    Task<bool> EkOdemeYapAsync(List<int> detayIds, DateTime odemeTarihi, OdemeSekli odemeSekli, int? bankaHesapId, string? aciklama);
    Task<List<BordroOdeme>> GetOdemelerAsync(int bordroDetayId);
    
    // Raporlama
    Task<byte[]> ExportBankaOdemeListesiAsync(int bordroId);
    Task<byte[]> ExportEkOdemeListesiAsync(int bordroId);
    Task<byte[]> ExportTumBordroAsync(int bordroId);
    Task<List<BordroKalanOdemeSatir>> GetKalanOdemeRaporuAsync(int bordroId);
    Task<byte[]> ExportKalanOdemeRaporuAsync(int bordroId);
    Task<byte[]> ExportBordroOzetAsync(int? firmaId, int? yil);

    /// <summary>
    /// Tek personel için detaylı ücret bordrosu PDF (resmi format)
    /// </summary>
    Task<byte[]> ExportUcretBordrosuAsync(int bordroDetayId);

    /// <summary>
    /// Seçili personeller için toplu ücret bordrosu PDF
    /// </summary>
    Task<byte[]> ExportTopluUcretBordrosuAsync(List<int> bordroDetayIds);

    /// <summary>
    /// Tüm bordro için ücret bordroları PDF
    /// </summary>
    Task<byte[]> ExportTumUcretBordrolariAsync(int bordroId);

    // Netten Brüte Hesaplama
    /// <summary>
    /// Net maaştan brüt maaşı hesaplar (ters hesaplama)
    /// </summary>
    Task<NettenBruteHesapSonucu> NettenBruteHesaplaAsync(decimal netMaas, int? firmaId, decimal kumulatifVergiMatrahi = 0);

    // Ayarlar
    Task<BordroAyar> GetBordroAyarAsync(int? firmaId);
    Task SaveBordroAyarAsync(BordroAyar ayar);

    // Özet Bilgiler
    Task<BordroOzet> GetBordroOzetAsync(int? firmaId, int? yil, int? ay);
}

public class BordroOzet
{
    public int ToplamPersonel { get; set; }
    public int NormalPersonel { get; set; }
    public int ArgePersonel { get; set; }
    public decimal ToplamBrutMaas { get; set; }
    public decimal ToplamNetMaas { get; set; }
    public decimal ToplamSgkMaasi { get; set; }
    public decimal ToplamEkOdeme { get; set; }
    public decimal GenelToplam => ToplamNetMaas + ToplamEkOdeme;
    public int OnayliDönemSayisi { get; set; }
    public int BekleyenDönemSayisi { get; set; }
}

public class BordroKalanOdemeSatir
{
    public int BordroId { get; set; }
    public int BordroDetayId { get; set; }
    public int PersonelId { get; set; }
    public string PersonelKodu { get; set; } = string.Empty;
    public string PersonelAdSoyad { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public decimal NetMaas { get; set; }
    public decimal BordrodaEleGecen { get; set; }
    public decimal KalanMaas { get; set; }
    public decimal PersonelHarcamalari { get; set; }
    public decimal PersonelAvansAlacaklari { get; set; }
    public decimal AvansVeOdemeler { get; set; }
    public decimal OdenecekMiktar { get; set; }
    public bool BankaOdemesiYapildi { get; set; }
    public bool EkOdemeYapildi { get; set; }
    public decimal KalanOdeme => KalanMaas + PersonelHarcamalari + PersonelAvansAlacaklari;
}




