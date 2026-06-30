using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

public class KiralikPlakaTakipFatura : BaseEntity
{
    public int KiralikPlakaTakipId { get; set; }
    public virtual KiralikPlakaTakip? KiralikPlakaTakip { get; set; }

    public int Sira { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }

    [StringLength(50)]
    public string? FaturaNo { get; set; }

    public DateTime? FaturaTarihi { get; set; }
    public decimal BazPlanTutari { get; set; }
    public decimal EkOdemeTutari { get; set; }
    public decimal PlanTutari { get; set; }
    public decimal FaturaTutari { get; set; }
    public decimal BuAyOdenen { get; set; }
}


