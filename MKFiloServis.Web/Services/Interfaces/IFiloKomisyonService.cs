using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IFiloKomisyonService
{
    // CUD İşlemleri Eşleştirme Şablonu
    Task<List<FiloGuzergahEslestirme>> GetEslestirmelerAsync(int? firmaId = null, bool sadeceAktifler = true);
    Task<FiloGuzergahEslestirme?> GetEslestirmeByIdAsync(int id);
    Task<FiloGuzergahEslestirme> CreateEslestirmeAsync(FiloGuzergahEslestirme eslestirme);
    Task<FiloGuzergahEslestirme> UpdateEslestirmeAsync(FiloGuzergahEslestirme eslestirme);
    Task<bool> DeleteEslestirmeAsync(int id);

    // Puantaj İşlemleri
    /// <summary>
    /// Verilen Yıl ve Ay için o aya ait aktif olan eşleştirmeleri baz alarak puantaj satırlarını oluşturur 
    /// (Eğer o gün için önceden oluşturulmamışsa).
    /// </summary>
    Task TopluPuantajUretAsync(int firmaId, int yil, int ay);

    /// <summary>
    /// Belirli bir tarihteki puantaj dökümünü getirir
    /// </summary>
    Task<List<FiloGunlukPuantaj>> GetGunlukPuantajlarSiraliAsync(int firmaId, DateTime tarih);

    /// <summary>
    /// İki tarih aralığındaki ve (opsiyonel) belirli bir kuruma / araca ait puantaj listesini getirir
    /// </summary>
    Task<List<FiloGunlukPuantaj>> GetPuantajlarByTarihAraligiAsync(int? firmaId, DateTime baslangic, DateTime bitis, int? kurumId = null, int? aracId = null);

    Task<FiloGunlukPuantaj> CreatePuantajAsync(FiloGunlukPuantaj puantaj);
    Task<FiloGunlukPuantaj> UpdateGunlukPuantajAsync(FiloGunlukPuantaj puantaj);
    Task UpdateGunlukPuantajlarAsync(List<FiloGunlukPuantaj> puantajlar);
    Task<int> DeleteGunlukPuantajlarAsync(List<int> puantajIds);

    /// <summary>
    /// Ekrandan Fatura Kes / Tahsil Et (Toplu İşlem)
    /// </summary>
    Task KurumFaturalastirAsync(List<int> puantajIds);
    Task TaseronOdeAsync(List<int> puantajIds);

    Task<List<Arac>> GetAraclarAsync(int firmaId);
    Task<List<Cari>> GetKurumlarAsync(int firmaId);
    Task<List<Sofor>> GetSoforlerAsync(int firmaId = 0);
    Task<List<Guzergah>> GetGuzergahlarAsync();
    Task<List<Kullanici>> GetKullanicilarAsync();
}




