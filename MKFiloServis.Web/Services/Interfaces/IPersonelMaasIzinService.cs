using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IPersonelMaasIzinService
{
    // Maa� ��lemleri
    Task<List<PersonelMaas>> GetMaaslarAsync(int yil, int ay);
    Task<PersonelMaas?> GetMaasByIdAsync(int id);
    Task<PersonelMaas?> GetMaasBySoforAsync(int soforId, int yil, int ay);
    Task<PersonelMaas> CreateMaasAsync(PersonelMaas maas);
    Task<PersonelMaas> UpdateMaasAsync(PersonelMaas maas);
    Task DeleteMaasAsync(int id);
    Task<int> DeleteMaaslarAsync(List<int> maasIds);
    Task<int> RecalculateMaaslarAsync(List<int> maasIds);
    Task<MaasOlusturmaSonuc> CreateMaasForPersonellerAsync(int yil, int ay, List<int> soforIds);
    Task<List<PersonelMaas>> GetSoforMaasGecmisiAsync(int soforId);
    Task MaasOdemeYapAsync(int maasId, DateTime odemeTarihi);
    Task TopluMaasOlusturAsync(int yil, int ay);

    // �zin ��lemleri
    Task<List<PersonelIzin>> GetIzinlerAsync(int? soforId = null, DateTime? baslangic = null, DateTime? bitis = null);
    Task<PersonelIzin?> GetIzinByIdAsync(int id);
    Task<PersonelIzin> CreateIzinAsync(PersonelIzin izin);
    Task<PersonelIzin> UpdateIzinAsync(PersonelIzin izin);
    Task DeleteIzinAsync(int id);
    Task IzinOnaylaAsync(int izinId, string onaylayanKisi);
    Task IzinReddetAsync(int izinId, string redNedeni);

    // �zin Hakk� ��lemleri
    Task<PersonelIzinHakki?> GetIzinHakkiAsync(int soforId, int yil);
    Task<PersonelIzinHakki> CreateOrUpdateIzinHakkiAsync(PersonelIzinHakki izinHakki);
    Task YillikIzinHaklariOlusturAsync(int yil);

    // Raporlar
    Task<MaasRaporOzet> GetMaasRaporuAsync(int yil, int ay);
    Task<IzinRaporOzet> GetIzinRaporuAsync(int yil);
    Task<List<PersonelOzet>> GetPersonelOzetListesiAsync();
}

public class MaasRaporOzet
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public int PersonelSayisi { get; set; }
    public decimal ToplamBrutMaas { get; set; }
    public decimal ToplamNetMaas { get; set; }
    public decimal ToplamSGKIsci { get; set; }
    public decimal ToplamSGKIsveren { get; set; }
    public decimal ToplamGelirVergisi { get; set; }
    public decimal ToplamOdeme { get; set; }
    public int OdenmeyenSayisi { get; set; }
    public List<MaasDetay> Detaylar { get; set; } = new();
}

public class MaasDetay
{
    public int MaasId { get; set; }
    public int SoforId { get; set; }
    public string SoforAdSoyad { get; set; } = string.Empty;
    public decimal BrutMaas { get; set; }
    public decimal NetMaas { get; set; }
    public decimal SGKIsciPayi { get; set; }
    public decimal GelirVergisi { get; set; }
    public decimal DamgaVergisi { get; set; }
    public decimal ToplamEklemeler { get; set; }
    public decimal DigerEklemeler { get; set; }
    public decimal Avans { get; set; }
    public decimal ToplamKesintiler { get; set; }
    public decimal OdenecekTutar { get; set; }
    public string OdemeDurum { get; set; } = string.Empty;
    public DateTime? OdemeTarihi { get; set; }
}

public class IzinRaporOzet
{
    public int Yil { get; set; }
    public int ToplamPersonel { get; set; }
    public int ToplamKullanilanIzin { get; set; }
    public int ToplamKalanIzin { get; set; }
    public List<IzinDetay> Detaylar { get; set; } = new();
    public List<IzinTipiOzet> TipBazliOzet { get; set; } = new();
}

public class IzinDetay
{
    public int SoforId { get; set; }
    public string SoforAdSoyad { get; set; } = string.Empty;
    public int YillikHak { get; set; }
    public int DevirenIzin { get; set; }
    public int KullanilanIzin { get; set; }
    public int KalanIzin { get; set; }
    public List<PersonelIzin> IzinKayitlari { get; set; } = new();
}

public class IzinTipiOzet
{
    public IzinTipi IzinTipi { get; set; }
    public string IzinTipiAdi { get; set; } = string.Empty;
    public int Adet { get; set; }
    public int ToplamGun { get; set; }
}

public class PersonelOzet
{
    public int SoforId { get; set; }
    public string SoforKodu { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public DateTime? IseBaslamaTarihi { get; set; }
    public decimal BrutMaas { get; set; }
    public decimal NetMaas { get; set; }
    public int KalanIzin { get; set; }
    public int BuAySeferSayisi { get; set; }
    public bool Aktif { get; set; }
}

public class MaasOlusturmaSonuc
{
    public int OlusturulanSayisi { get; set; }
    public int ZatenVarSayisi { get; set; }
}



