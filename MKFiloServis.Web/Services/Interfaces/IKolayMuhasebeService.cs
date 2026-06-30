using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IKolayMuhasebeService
{
    /// <summary>
    /// İşlem türüne göre muhasebe kaydı önizlemesi oluşturur
    /// </summary>
    Task<MuhasebeOnizleme> OnizlemeOlusturAsync(KolayMuhasebeGiris giris);

    /// <summary>
    /// Girişi ve muhasebe kaydını kaydeder
    /// </summary>
    Task<KolayMuhasebeSonuc> KaydetAsync(KolayMuhasebeGiris giris, MuhasebeOnizleme? manuelOnizleme = null);

    /// <summary>
    /// Cari listesini getirir (hızlı seçim için)
    /// </summary>
    Task<List<Cari>> GetCarilerAsync(CariTipi? tip = null, string? arama = null);

    /// <summary>
    /// Masraf kalemlerini getirir
    /// </summary>
    Task<List<MasrafKalemiBasit>> GetMasrafKalemleriAsync();

    /// <summary>
    /// Banka hesaplarını getirir
    /// </summary>
    Task<List<BankaHesapBasit>> GetBankaHesaplariAsync();

    /// <summary>
    /// Araç listesini getirir (masraf için)
    /// </summary>
    Task<List<Arac>> GetAraclarAsync();

    /// <summary>
    /// Yeni cari oluşturur (hızlı oluşturma)
    /// </summary>
    Task<Cari> HizliCariOlusturAsync(string unvan, CariTipi tip);

    /// <summary>
    /// Yeni cari oluşturur (detaylı bilgilerle)
    /// </summary>
    Task<Cari> HizliCariOlusturDetayliAsync(HizliCariModel model);

    /// <summary>
    /// Muhasebe hesap listesi (manuel düzenleme için)
    /// </summary>
    Task<List<MuhasebeHesap>> GetMuhasebeHesaplariAsync();

    /// <summary>
    /// Stok listesini getirir (kalem girişi için)
    /// </summary>
    Task<List<StokBasit>> GetStoklarAsync(string? arama = null);

    /// <summary>
    /// Varsayılan muhasebe ayarlarını getirir
    /// </summary>
    Task<MuhasebeAyar> GetMuhasebeAyarAsync();

    /// <summary>
    /// Hızlı stok kalemi oluşturur
    /// </summary>
    Task<StokBasit> HizliStokOlusturAsync(string stokAdi, string birim, decimal kdvOrani);
}

/// <summary>
/// Hızlı cari ekleme modeli
/// </summary>
public class HizliCariModel
{
    public string Unvan { get; set; } = "";
    public CariTipi CariTipi { get; set; } = CariTipi.Musteri;
    public string? VergiNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Adres { get; set; }
}



