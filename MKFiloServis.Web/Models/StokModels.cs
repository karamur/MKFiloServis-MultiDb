namespace MKFiloServis.Web.Models;

using MKFiloServis.Shared.Entities;

public class StokDashboard
{
    public int ToplamStokKarti { get; set; }
    public int AktifStokKarti { get; set; }
    public int DusukStoklu { get; set; }
    public decimal ToplamStokDegeri { get; set; }
    public int AylikAracAlis { get; set; }
    public int AylikAracSatis { get; set; }
    public int AylikServisKaydi { get; set; }
    public decimal AylikServisTutari { get; set; }
    public List<StokHareket> SonHareketler { get; set; } = new();
    public List<ServisKaydi> SonServisler { get; set; } = new();
}

public class StokOperasyonModel
{
    public int StokKartiId { get; set; }
    public DateTime IslemTarihi { get; set; } = DateTime.Today;
    public StokHareketTipi HareketTipi { get; set; }
    public decimal Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public string? BelgeNo { get; set; }
    public string? Aciklama { get; set; }
    public int? CariId { get; set; }
}

public class UretimReceteModel
{
    public int ReceteId { get; set; }
    public string? Ad { get; set; }
    public int MamulStokKartiId { get; set; }
    public decimal MamulMiktari { get; set; }
    public decimal MamulBirimMaliyeti { get; set; }
    public DateTime IslemTarihi { get; set; } = DateTime.Today;
    public string? BelgeNo { get; set; }
    public string? Aciklama { get; set; }
    public List<UretimReceteKalemModel> Kalemler { get; set; } = new();
}

public class UretimReceteDetail
{
    public int MalzemeId { get; set; }
    public decimal Miktar { get; set; }
    public string? Birim { get; set; }
}

public class UretimReceteKalemModel
{
    public int StokKartiId { get; set; }
    public decimal Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
}

