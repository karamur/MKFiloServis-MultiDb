using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

public class IhaleTeklifVersiyon : BaseEntity
{
    public int IhaleProjeId { get; set; }
    public virtual IhaleProje IhaleProje { get; set; } = null!;

    public int VersiyonNo { get; set; }

    [Required]
    [StringLength(50)]
    public string RevizyonKodu { get; set; } = string.Empty;

    public IhaleTeklifVersiyonDurum Durum { get; set; } = IhaleTeklifVersiyonDurum.Taslak;

    [StringLength(2000)]
    public string? RevizyonNotu { get; set; }

    [StringLength(2000)]
    public string? KararNotu { get; set; }

    public int? HazirlayanKullaniciId { get; set; }
    public virtual Kullanici? HazirlayanKullanici { get; set; }

    public int? OnaylayanKullaniciId { get; set; }
    public virtual Kullanici? OnaylayanKullanici { get; set; }

    public DateTime HazirlamaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? OnayTarihi { get; set; }
    public bool AktifVersiyon { get; set; }

    public decimal ToplamMaliyet { get; set; }
    public decimal TeklifTutari { get; set; }
    public decimal KarMarjiTutari { get; set; }
    public decimal KarMarjiOrani { get; set; }

    public virtual ICollection<IhaleTeklifKararLog> KararLoglari { get; set; } = new List<IhaleTeklifKararLog>();
}

public class IhaleTeklifKararLog : BaseEntity
{
    public int IhaleTeklifVersiyonId { get; set; }
    public virtual IhaleTeklifVersiyon IhaleTeklifVersiyon { get; set; } = null!;

    public IhaleTeklifIslemTipi IslemTipi { get; set; }
    public IhaleTeklifVersiyonDurum? OncekiDurum { get; set; }
    public IhaleTeklifVersiyonDurum YeniDurum { get; set; }

    [StringLength(2000)]
    public string? Not { get; set; }

    public int? IslemYapanKullaniciId { get; set; }
    public virtual Kullanici? IslemYapanKullanici { get; set; }

    public DateTime IslemTarihi { get; set; } = DateTime.UtcNow;
}

public enum IhaleTeklifVersiyonDurum
{
    Taslak = 0,
    Incelemede = 1,
    Onaylandi = 2,
    Reddedildi = 3
}

public enum IhaleTeklifIslemTipi
{
    Olustur = 0,
    RevizyonOlustur = 1,
    IncelemeyeGonder = 2,
    Onayla = 3,
    Reddet = 4,
    AktifVersiyonDegisti = 5
}


