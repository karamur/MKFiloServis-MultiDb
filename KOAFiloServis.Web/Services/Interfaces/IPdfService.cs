using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services;

public interface IPdfService
{
    byte[] GenerateFaturaPdf(Fatura fatura);
    byte[] GenerateServisCalismaRaporuPdf(List<ServisCalisma> calismalar, DateTime baslangic, DateTime bitis);
    byte[] GenerateBelgeUyariRaporuPdf(List<BelgeUyari> uyarilar);
    byte[] GenerateCariEkstresPdf(Cari cari, List<Fatura> faturalar, List<BankaKasaHareket> hareketler);
    byte[] GenerateMutabakatPdf(MutabakatPdfModel model);
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
