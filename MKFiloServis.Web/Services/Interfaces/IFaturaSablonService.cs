using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Fatura şablon yönetim servisi interface'i
/// </summary>
public interface IFaturaSablonService
{
    // Şablon CRUD işlemleri
    Task<List<FaturaSablon>> TumSablonlariGetirAsync();
    Task<FaturaSablon?> SablonGetirAsync(int id);
    Task<FaturaSablon?> VarsayilanSablonGetirAsync();
    Task<FaturaSablon> SablonEkleAsync(FaturaSablon sablon);
    Task<bool> SablonGuncelleAsync(FaturaSablon sablon);
    Task<bool> SablonSilAsync(int id);
    Task<bool> VarsayilanYapAsync(int id);
    Task<FaturaSablon> SablonKopyalaAsync(int id, string yeniAd);

    // PDF oluşturma işlemleri
    Task<FaturaPdfResult> FaturaPdfOlusturAsync(int faturaId, int? sablonId = null);
    Task<FaturaPdfResult> FaturaPdfOlusturAsync(Fatura fatura, FaturaSablon? sablon = null);
    Task<byte[]> OnizlemePdfOlusturAsync(FaturaSablon sablon);

    // Email gönderimi
    Task<bool> FaturaEmailGonderAsync(FaturaYazdirRequest request);
    Task<bool> TopluFaturaEmailGonderAsync(List<int> faturaIds, int? sablonId = null, string? emailKonu = null, string? emailMesaj = null);

    // Logo işlemleri
    Task<bool> LogoYukleAsync(int sablonId, byte[] logoData, string dosyaAdi);
    Task<bool> LogoSilAsync(int sablonId);
    Task<bool> KaseYukleAsync(int sablonId, byte[] kaseData, string dosyaAdi);
    Task<bool> KaseSilAsync(int sablonId);
}



