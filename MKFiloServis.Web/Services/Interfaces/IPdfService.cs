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



