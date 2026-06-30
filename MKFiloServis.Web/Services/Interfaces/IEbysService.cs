using MKFiloServis.Shared.Entities;
using Microsoft.AspNetCore.Components.Forms;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IEbysService
{
    Task<List<EbysBelgeKaydi>> GetBelgeKayitlariAsync(EbysBelgeListeFiltre filtre);
    Task<List<string>> GetKategorilerAsync();
    Task<List<EbysKategoriOzet>> GetKategoriOzetleriAsync();
    Task<EbysBelgeOlusturmaSecenekleri> GetBelgeOlusturmaSecenekleriAsync();
    Task<EbysBelgeDosya?> GetBelgeDosyasiAsync(EbysBelgeKaynak kaynak, int belgeId, int? dosyaId = null);
    Task<EbysBelgeDuzenlemeModeli?> GetBelgeDuzenlemeModeliAsync(EbysBelgeKaynak kaynak, int belgeId);
    Task BelgeGuncelleAsync(EbysBelgeDuzenlemeModeli model);
    Task BelgeDosyasiYukleAsync(EbysBelgeKaynak kaynak, int belgeId, IBrowserFile file);
    Task<EbysBelgeKaydi> BelgeOlusturAsync(EbysBelgeOlusturmaModeli model);
}

public class EbysBelgeListeFiltre
{
    public string? AramaMetni { get; set; }
    public EbysBelgeKaynakFiltre Kaynak { get; set; } = EbysBelgeKaynakFiltre.Tumu;
    public string? Kategori { get; set; }
    public EbysBelgeRiskFiltre Risk { get; set; } = EbysBelgeRiskFiltre.Tumu;
    public bool SadeceDosyasiOlanlar { get; set; }
}

public enum EbysBelgeKaynakFiltre
{
    Tumu = 0,
    Personel = 1,
    Arac = 2
}

public enum EbysBelgeKaynak
{
    Personel = 1,
    Arac = 2
}

public enum EbysBelgeRiskFiltre
{
    Tumu = 0,
    Risksiz = 1,
    Yaklasan = 2,
    SuresiDolmus = 3,
    DosyaEksik = 4
}

public class EbysBelgeKaydi
{
    public EbysBelgeKaynak Kaynak { get; set; }
    public int BelgeId { get; set; }
    public int? DosyaId { get; set; }
    public string BelgeAdi { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string IlgiliKayitAdi { get; set; } = string.Empty;
    public string IlgiliKayitKodu { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public bool DosyaVar { get; set; }
    public string? DosyaAdi { get; set; }
    public DateTime? BelgeTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public string RiskDurumu { get; set; } = string.Empty;
    public bool YaklasanMi { get; set; }
    public bool SuresiDolmusMu { get; set; }
    public string KaynakDetayUrl { get; set; } = string.Empty;
}

public class EbysKategoriOzet
{
    public string Kategori { get; set; } = string.Empty;
    public int ToplamKayit { get; set; }
    public int PersonelSayisi { get; set; }
    public int AracSayisi { get; set; }
    public int DosyaliKayit { get; set; }
    public int RiskliKayit { get; set; }
}

public class EbysBelgeDosya
{
    public string DosyaAdi { get; set; } = string.Empty;
    public string MimeTipi { get; set; } = "application/octet-stream";
    public byte[] Icerik { get; set; } = Array.Empty<byte>();
}

public class EbysBelgeDuzenlemeModeli
{
    public EbysBelgeKaynak Kaynak { get; set; }
    public int BelgeId { get; set; }
    public string BelgeAdi { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public DateTime? BelgeTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public bool Tamamlandi { get; set; }
    public string Durum { get; set; } = string.Empty;
    public bool DosyaVar { get; set; }
    public bool BelgeAdiDuzenlenebilir { get; set; }
    public bool KategoriDuzenlenebilir { get; set; }
    public bool DurumDuzenlenebilir { get; set; }
    public bool TarihDuzenlenebilir { get; set; }
    public bool BitisTarihiDuzenlenebilir { get; set; }
}

public class EbysBelgeOlusturmaSecenekleri
{
    public List<EbysSecimItem> Personeller { get; set; } = new();
    public List<EbysSecimItem> Araclar { get; set; } = new();
    public List<EbysSecimItem> PersonelEvrakTanimlari { get; set; } = new();
    public List<string> AracKategorileri { get; set; } = new();
}

public class EbysSecimItem
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string? Kod { get; set; }
}

public class EbysBelgeOlusturmaModeli
{
    public EbysBelgeKaynak Kaynak { get; set; } = EbysBelgeKaynak.Personel;
    public int? IlgiliKayitId { get; set; }
    public int? EvrakTanimId { get; set; }
    public string? BelgeAdi { get; set; }
    public string? Kategori { get; set; }
    public string? Aciklama { get; set; }
    public bool Tamamlandi { get; set; }
    public DateTime? BelgeTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public string Durum { get; set; } = "Aktif";
}



