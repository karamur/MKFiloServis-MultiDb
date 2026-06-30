namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Uygulama genel ayarları (dizinler, güncelleme vb.)
/// </summary>
public class AppAyarlari
{
    public int Id { get; set; }
    public string Anahtar { get; set; } = "";
    public string Deger { get; set; } = "";
    public string? Aciklama { get; set; }
    public string Kategori { get; set; } = "Genel";
    public DateTime GuncellenmeTarihi { get; set; } = DateTime.UtcNow;
}


