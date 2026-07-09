using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MKFiloServis.Shared.DTOs.Puantaj;

namespace MKFiloServis.Shared.Services.Contracts
{
    /// <summary>
    /// Cari-Kurum-Puantaj hiyerarşik veri yönetimi contract
    /// </summary>
    public interface ICariKurumPuantajService
    {
        /// <summary>
        /// Cari için Kurum hiyerarşisini getirir
        /// </summary>
        Task<CariKurumHiyerarsiDto> GetCariKurumHiyerarsiAsync(int cariId);

        /// <summary>
        /// Kurum bazında aylık puantaj grid'i
        /// </summary>
        Task<KurumPuantajAylikDTO> GetKurumPuantajAylikAsync(int kurumId, int yil, int ay);

        /// <summary>
        /// Tüm Cariler özeti
        /// </summary>
        Task<List<CariPuantajOzetDTO>> GetCarilarPuantajOzetiAsync(DateTime baslamaTarihi, DateTime bitisTarihi);

        /// <summary>
        /// Toplu puantaj giriş
        /// </summary>
        Task<bool> TopluPuantajGirisiAsync(PuantajTopluGirisRequestDTO request, string kullaniciId);

        /// <summary>
        /// Eksik güne araç+şoför ekle
        /// </summary>
        Task<bool> EksikGunuEkleAsync(EksikGunEkleRequestDTO request, string kullaniciId);

        /// <summary>
        /// Günlük detail toplu güncelle
        /// </summary>
        Task<bool> GunlukDetayTopluGuncelleAsync(GunlukDetayTopluGuncelleRequestDTO request, string kullaniciId);
    }
}
