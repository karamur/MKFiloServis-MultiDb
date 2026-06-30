using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

public class WhatsAppKisi : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string AdSoyad { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Telefon { get; set; } = string.Empty;

    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }

    public string? Notlar { get; set; }

    public virtual ICollection<WhatsAppGrupUye> Gruplari { get; set; } = new List<WhatsAppGrupUye>();
}

public class WhatsAppGrup : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string GrupAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public virtual ICollection<WhatsAppGrupUye> Uyeler { get; set; } = new List<WhatsAppGrupUye>();
}

public class WhatsAppGrupUye : BaseEntity
{
    public int GrupId { get; set; }
    public virtual WhatsAppGrup Grup { get; set; } = null!;

    public int KisiId { get; set; }
    public virtual WhatsAppKisi Kisi { get; set; } = null!;
}

public class WhatsAppMesaj : BaseEntity
{
    public int? GonderenId { get; set; } // Bo�sa sistem g�ndermi�tir
    public virtual Kullanici? Gonderen { get; set; }

    public int? KisiId { get; set; }
    public virtual WhatsAppKisi? Kisi { get; set; }

    public int? GrupId { get; set; }
    public virtual WhatsAppGrup? Grup { get; set; }

    [Required]
    public string Icerik { get; set; } = string.Empty;

    public Yon Tipi { get; set; } // Gelen, Giden

    // MesajDurum enum'� halihaz�rda CRMEntities.cs i�inde var
    public MesajDurum Durum { get; set; } = MesajDurum.Gonderildi;

    public DateTime MesajTarihi { get; set; } = DateTime.UtcNow;

    public bool Okundu { get; set; } = false;

    public enum Yon
    {
        Gelen = 0,
        Giden = 1
    }
}

public class WhatsAppSablon : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Baslik { get; set; } = string.Empty;

    [Required]
    public string Icerik { get; set; } = string.Empty;

    public string? Parametreler { get; set; } // �rn: {0}: �sim, {1}: Fatura No
}

