namespace MKFiloServis.Web.Services.Interfaces;

public sealed class DuplicateGrup
{
    public int GuzergahId { get; set; }
    public string GuzergahAdi { get; set; } = "";
    public int AracId { get; set; }
    public string Plaka { get; set; } = "";
    public List<DuplicateSatir> Satirlar { get; set; } = [];
    public string MergeOnerisi { get; set; } = "";
}

public sealed class DuplicateSatir
{
    public int PuantajKayitId { get; set; }
    public string Yon { get; set; } = "";
    public string Slot { get; set; } = "";
    public int SeferSayisi { get; set; }
    public string SoforAdi { get; set; } = "";
}

public interface IDuplicateDetectionService
{
    Task<List<DuplicateGrup>> TespitRaporuAsync(int yil, int ay, int? kurumId = null);
}




