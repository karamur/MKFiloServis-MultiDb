using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IPdfService
{
    byte[] GenerateFaturaPdf(Fatura fatura);
    byte[] GenerateServisCalismaRaporuPdf(List<ServisCalisma> calismalar, DateTime baslangic, DateTime bitis);
    byte[] GenerateBelgeUyariRaporuPdf(List<BelgeUyari> uyarilar);
    byte[] GenerateCariEkstresPdf(Cari cari, List<Fatura> faturalar, List<BankaKasaHareket> hareketler);
    byte[] GenerateMutabakatPdf(MutabakatPdfModel model);
    byte[] GenerateHakedisPdf(Hakedis hakedis, string? referansAd = null);
    byte[] GenerateHakedisDetayRaporPdf(HakedisDetayRaporModel model);
    byte[] GenerateRentACarKiralamaRaporPdf(RentACarKiralamaRaporModel model);
}

public sealed class RentACarKiralamaRaporModel
{
    public string FirmaUnvan { get; set; } = string.Empty;
    public string FirmaVergiDairesi { get; set; } = string.Empty;
    public string FirmaVergiNo { get; set; } = string.Empty;
    public string FirmaAdres { get; set; } = string.Empty;
    public string FirmaTelefon { get; set; } = string.Empty;
    public string RaporTarihAraligi { get; set; } = string.Empty;
    public string DurumFiltresi { get; set; } = string.Empty;
    public string AramaFiltresi { get; set; } = string.Empty;
    public DateTime RaporOlusturmaTarihi { get; set; } = DateTime.Now;
    public List<RentACarPdfKiralamaSatiri> Satirlar { get; set; } = new();
}

public sealed class RentACarPdfKiralamaSatiri
{
    public string? SozlesmeNo { get; set; }
    public string Musteri { get; set; } = string.Empty;
    public string Plaka { get; set; } = string.Empty;
    public string Arac { get; set; } = string.Empty;
    public DateTime Baslangic { get; set; }
    public DateTime PlanlananIade { get; set; }
    public DateTime? GercekIade { get; set; }
    public string Durum { get; set; } = string.Empty;
    public decimal PlanlananTutar { get; set; }
    public string OdemeDurumu { get; set; } = string.Empty;
}

public sealed class HakedisDetayRaporModel
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public bool FirmalariGrupla { get; set; }
    public List<HakedisDetayRaporSatiri> Satirlar { get; set; } = new();
}

public sealed class HakedisDetayRaporSatiri
{
    public string Firma { get; set; } = string.Empty;
    public string Guzergah { get; set; } = string.Empty;
    public string Plaka { get; set; } = string.Empty;
    public string Sofor { get; set; } = string.Empty;
    public decimal SeferSayisi { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal FaturaTutari { get; set; }
}

public class MutabakatPdfModel
{
    public string CariKodu { get; set; } = string.Empty;
    public string CariUnvan { get; set; } = string.Empty;
    public string VergiNo { get; set; } = string.Empty;
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public decimal DonemBasiBakiye { get; set; }
    public decimal ToplamBorc { get; set; }
    public decimal ToplamAlacak { get; set; }
    public decimal DonemSonuBakiye { get; set; }
    public List<MutabakatPdfHareket> Hareketler { get; set; } = new();
}

public class MutabakatPdfHareket
{
    public DateTime Tarih { get; set; }
    public string BelgeNo { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; }
}



