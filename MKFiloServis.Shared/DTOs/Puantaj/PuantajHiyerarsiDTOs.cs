using System;
using System.Collections.Generic;

namespace MKFiloServis.Shared.DTOs.Puantaj
{
    /// <summary>
    /// Cari → Kurum → Eşleştirmeler hiyerarşisini temsil eder
    /// </summary>
    public class CariKurumHiyerarsiDto
    {
        public int CariId { get; set; }
        public string CariTamAdi { get; set; } = "";
        public string CariKodu { get; set; } = "";

        public decimal CariToplam { get; set; }
        public List<KurumBaslıEslestirmeDTO> Kurumlar { get; set; } = new();
    }

    /// <summary>
    /// Kurum altında eşleştirmeler (araç+şoför+güzergah)
    /// </summary>
    public class KurumBaslıEslestirmeDTO
    {
        public int KurumId { get; set; }
        public string KurumAdi { get; set; } = "";

        public List<EslestirmeDetayDTO> Eslestirmeler { get; set; } = new();

        // Kurum bazında toplam
        public decimal KurumToplam { get; set; }

        // Aylık grid verisini cache'lemek için
        public List<KurumPuantajAylikDTO> AylikGridData { get; set; } = new();
    }

    /// <summary>
    /// Tek bir eşleştirme (Araç + Şoför + Güzergah + Kurum kombinasyonu)
    /// </summary>
    public class EslestirmeDetayDTO
    {
        public int EslestirmeId { get; set; }
        public string AracPlakasi { get; set; } = "";
        public string SoforAdi { get; set; } = "";
        public string GuzergahAdi { get; set; } = "";

        public decimal BirimFiyat { get; set; }
        public decimal GiderFiyat { get; set; }

        public bool AktifMi { get; set; } = true;
        public DateTime BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
    }

    /// <summary>
    /// Kurum bazında aylık puantaj grid'i
    /// </summary>
    public class KurumPuantajAylikDTO
    {
        public int KurumId { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }

        // Günlük hücreler (1-31. günleri temsil eder)
        public List<GunlukHucreDTO> GunlukHucreler { get; set; } = new();

        // Özet
        public int ToplamGun { get; set; }
        public int CalisanGun { get; set; }
        public int HizmetVerilmeyanGun { get; set; }

        public decimal AylikToplam { get; set; }
    }

    /// <summary>
    /// Tek günlük hücre (grid satırında bir gün)
    /// </summary>
    public class GunlukHucreDTO
    {
        public int Gun { get; set; }  // 1-31
        public DateTime Tarih { get; set; }
        public string GunAdi { get; set; } = "";  // "Pazartesi", "Salı", vb.

        // Servis detayları
        public int? AracId { get; set; }
        public int? SoforId { get; set; }
        public int? GuzergahId { get; set; }

        public string AracPlakasi { get; set; } = "";
        public string SoforAdi { get; set; } = "";
        public string GuzergahAdi { get; set; } = "";

        // Durum
        public string Durum { get; set; } = "Boş";  // "Hizmet Verildi", "Makzul", "İzin", "Hastalık", vb.
        public string Notlar { get; set; } = "";

        // Maliyetler
        public decimal? BirimFiyat { get; set; }
        public decimal? GiderFiyat { get; set; }
        public decimal? Toplam { get; set; }

        public bool EditlenmeMi { get; set; } = false;
    }

    /// <summary>
    /// Tüm Cariler için kümülatif puantaj özeti (Dashboard)
    /// </summary>
    public class CariPuantajOzetDTO
    {
        public int CariId { get; set; }
        public string CariTamAdi { get; set; } = "";

        public DateTime BaslamaTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }

        public int ToplamKurum { get; set; }
        public int ToplamEslestirme { get; set; }

        public int ToplamGun { get; set; }
        public int CalisanGun { get; set; }

        public decimal ToplamGelir { get; set; }
        public decimal ToplamGider { get; set; }
        public decimal NetKar { get; set; }

        public List<KurumOzetDTO> KurumOzetleri { get; set; } = new();
    }

    /// <summary>
    /// Kurum seviyesi özet
    /// </summary>
    public class KurumOzetDTO
    {
        public int KurumId { get; set; }
        public string KurumAdi { get; set; } = "";

        public int ToplamEslestirme { get; set; }
        public int ToplamGun { get; set; }
        public int CalisanGun { get; set; }

        public decimal ToplamGelir { get; set; }
        public decimal ToplamGider { get; set; }
        public decimal NetKar { get; set; }

        // Verimin yüzdesi
        public decimal VerimYuzdesi { get; set; }  // CalisanGun / ToplamGun * 100
    }

    /// <summary>
    /// Toplu giriş için request model
    /// </summary>
    public class PuantajTopluGirisRequestDTO
    {
        public int KurumId { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }

        // Excel dosyasından parse edilen satırlar
        public List<TopluGirisSatiriDTO> Satirlar { get; set; } = new();
    }

    /// <summary>
    /// Toplu giriş bir satırı
    /// </summary>
    public class TopluGirisSatiriDTO
    {
        public string AracPlakasi { get; set; } = "";
        public string SoforAdi { get; set; } = "";
        public string GuzergahAdi { get; set; } = "";

        public int? AracId { get; set; }
        public int? SoforId { get; set; }
        public int? GuzergahId { get; set; }

        public int SeforSayisi { get; set; }  // Aylık toplam sefer sayısı
        public string DurumNotlari { get; set; } = "";  // "Makzul 2 gün", "İzin 3 gün", vb.
    }

    /// <summary>
    /// Günlük detail güncellemesi (bulk update)
    /// </summary>
    public class GunlukDetayGuncelleRequestDTO
    {
        public int EslestirmeId { get; set; }
        public DateTime Tarih { get; set; }

        public string Durum { get; set; } = "Hizmet Verildi";  // Düşündüğü durum
        public string Notlar { get; set; } = "";
    }

    /// <summary>
    /// Günlük detail toplu güncellemesi
    /// </summary>
    public class GunlukDetayTopluGuncelleRequestDTO
    {
        public int EslestirmeId { get; set; }
        public List<GunlukDetayGuncelleRequestDTO> Gunler { get; set; } = new();
    }

    /// <summary>
    /// Eksik günleri yamlı add için request
    /// </summary>
    public class EksikGunEkleRequestDTO
    {
        public int KurumId { get; set; }
        public DateTime Tarih { get; set; }

        public int AracId { get; set; }
        public int SoforId { get; set; }
        public int GuzergahId { get; set; }

        public string Durum { get; set; } = "Hizmet Verildi";
        public string Notlar { get; set; } = "";
    }
}
