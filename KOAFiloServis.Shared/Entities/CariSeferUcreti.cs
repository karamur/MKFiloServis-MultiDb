namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Bir cari (özellikle taşıma tedarikçisi) için tanımlı sefer ücreti.
/// Bir cariye birden fazla sefer ücreti tanımlanabilir (farklı güzergah,
/// farklı dönem, farklı araç tipi vb.).
/// </summary>
public class CariSeferUcreti : BaseEntity
{
    public int? SirketId { get; set; }
    public virtual Sirket? Sirket { get; set; }

    public int CariId { get; set; }
    public virtual Cari Cari { get; set; } = null!;

    /// <summary>Opsiyonel güzergah bağlantısı.</summary>
    public int? GuzergahId { get; set; }
    public virtual Guzergah? Guzergah { get; set; }

    /// <summary>Sefer/hizmet tanımı (örn. "Tek yön", "Gidiş-dönüş", "Hafta sonu").</summary>
    public string Tanim { get; set; } = string.Empty;

    public decimal SeferUcreti { get; set; }

    /// <summary>Geçerlilik başlangıcı.</summary>
    public DateTime GecerlilikBaslangic { get; set; } = DateTime.Today;

    /// <summary>Geçerlilik bitişi (null = açık uçlu).</summary>
    public DateTime? GecerlilikBitis { get; set; }

    public bool Aktif { get; set; } = true;
    public string? Aciklama { get; set; }
}
