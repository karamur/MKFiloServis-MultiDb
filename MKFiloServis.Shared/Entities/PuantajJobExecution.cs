using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

public class PuantajJobExecution : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int Yil { get; set; }
    public int Ay { get; set; }
    public int? KurumId { get; set; }

    [StringLength(50)]
    public string Tetikleyen { get; set; } = "Quartz";

    public PuantajJobExecutionDurum Durum { get; set; } = PuantajJobExecutionDurum.Running;

    public DateTime? Baslangic { get; set; }
    public DateTime? Bitis { get; set; }

    public int Versiyon { get; set; }
    public int? HesapDonemiId { get; set; }

    public int IslenenOperasyon { get; set; }
    public int UretilenPuantaj { get; set; }

    [StringLength(1000)]
    public string? HataMesaji { get; set; }

    [StringLength(50)]
    public string? Hesaplayan { get; set; }
}

public enum PuantajJobExecutionDurum
{
    Running = 0,
    Completed = 1,
    PartialSuccess = 2,
    Failed = 3,
    Skipped = 4
}


