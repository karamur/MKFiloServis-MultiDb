namespace MKFiloServis.Shared.Entities;

public class HoldingVeri
{
    public int Id { get; set; }
    public int FirmaId { get; set; }
    public string FirmaKodu { get; set; } = "";
    public string FirmaAdi { get; set; } = "";
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string Kategori { get; set; } = "";

    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal Kar { get; set; }
    public decimal? ButceHedef { get; set; }
    public decimal? ButceGerceklesen { get; set; }
    public decimal? AraclarMaliyet { get; set; }
    public decimal? PersonelMaliyet { get; set; }
    public decimal? HakedisToplam { get; set; }
    public decimal? OdenmemisFaturaToplam { get; set; }
    public int? AktifAracSayisi { get; set; }
    public int? PersonelSayisi { get; set; }
    public string? JsonDetay { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
}


