namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Aylik Checklist - Sofor, Arac, Guzergah icin aylik kontrol listesi
/// </summary>
public class AylikChecklist : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int Yil { get; set; }
    public int Ay { get; set; }
    public ChecklistTipi ChecklistTipi { get; set; }

    public int? SoforId { get; set; }
    public int? AracId { get; set; }
    public int? GuzergahId { get; set; }

    public DateTime? KontrolTarihi { get; set; }
    public string? KontrolEden { get; set; }
    public ChecklistDurum GenelDurum { get; set; } = ChecklistDurum.Bekliyor;
    public string? Notlar { get; set; }

    public virtual Sofor? Sofor { get; set; }
    public virtual Arac? Arac { get; set; }
    public virtual Guzergah? Guzergah { get; set; }
    public virtual ICollection<ChecklistKalem> Kalemler { get; set; } = new List<ChecklistKalem>();
}

/// <summary>
/// Checklist Kalemi - Her bir kontrol maddesi
/// </summary>
public class ChecklistKalem : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int AylikChecklistId { get; set; }
    public string KalemAdi { get; set; } = string.Empty;
    public ChecklistDurum Durum { get; set; } = ChecklistDurum.Bekliyor;
    public DateTime? SonGecerlilikTarihi { get; set; }
    public DateTime? KontrolTarihi { get; set; }
    public string? Aciklama { get; set; }
    public int SiraNo { get; set; }

    public virtual AylikChecklist AylikChecklist { get; set; } = null!;
}

public enum ChecklistTipi
{
    Sofor = 1,
    Arac = 2,
    Guzergah = 3
}

public enum ChecklistDurum
{
    Bekliyor = 0,
    Gecti = 1,
    Kaldi = 2,
    Muaf = 3
}


