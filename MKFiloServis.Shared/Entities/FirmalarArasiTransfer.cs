using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Şirketler (firmalar) arası kasa/banka transferi başlığı. (Karar K6)
/// <para>
/// Bir transfer kaydı oluşturulduğunda <see cref="BankaKasaHareket"/> tablosunda iki kayıt üretilir:
/// </para>
/// <list type="number">
///   <item>Kaynakta <b>çıkış</b> hareketi (FirmaId = <see cref="KaynakFirmaId"/>, BankaHesapId = <see cref="KaynakHesapId"/>).</item>
///   <item>Hedefte <b>giriş</b> hareketi (FirmaId = <see cref="HedefFirmaId"/>, BankaHesapId = <see cref="HedefHesapId"/>).</item>
/// </list>
/// <para>
/// Bu kayıt sayesinde transferin iki tarafı tek bir audit/iz üzerinden takip edilebilir
/// ve mutabakat yapılabilir.
/// </para>
/// <para>
/// <b>Tenant filter:</b> Bu entity <see cref="TenantFilterIgnoreAttribute"/> ile işaretlidir;
/// çünkü transfer iki firmayı aynı anda ilgilendirir ve raporlama her iki firmadan görünmelidir.
/// Filtreleme servis katmanında <c>KaynakFirmaId == aktif || HedefFirmaId == aktif</c> ile yapılır.
/// </para>
/// </summary>
[TenantFilterIgnore]
public class FirmalarArasiTransfer : BaseEntity, IFirmaTenant
{
    /// <summary>
    /// IFirmaTenant kontratı: transferi başlatan firma. <see cref="KaynakFirmaId"/> ile aynıdır.
    /// </summary>
    public int? FirmaId
    {
        get => KaynakFirmaId;
        set => KaynakFirmaId = value ?? 0;
    }

    /// <summary>Transferi başlatan (parayı çıkaran) firma.</summary>
    [Required]
    public int KaynakFirmaId { get; set; }
    public virtual Firma? KaynakFirma { get; set; }

    /// <summary>Transferi alan (parayı tahsil eden) firma.</summary>
    [Required]
    public int HedefFirmaId { get; set; }
    public virtual Firma? HedefFirma { get; set; }

    /// <summary>Kaynak banka/kasa hesabı (FirmaId = KaynakFirmaId olmalı).</summary>
    [Required]
    public int KaynakHesapId { get; set; }
    public virtual BankaHesap? KaynakHesap { get; set; }

    /// <summary>Hedef banka/kasa hesabı (FirmaId = HedefFirmaId olmalı).</summary>
    [Required]
    public int HedefHesapId { get; set; }
    public virtual BankaHesap? HedefHesap { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalı.")]
    public decimal Tutar { get; set; }

    [Required]
    public DateTime TransferTarihi { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [StringLength(50)]
    public string? BelgeNo { get; set; }

    /// <summary>Transfer ile üretilen kaynak (çıkış) hareketi.</summary>
    public int? KaynakHareketId { get; set; }
    public virtual BankaKasaHareket? KaynakHareket { get; set; }

    /// <summary>Transfer ile üretilen hedef (giriş) hareketi.</summary>
    public int? HedefHareketId { get; set; }
    public virtual BankaKasaHareket? HedefHareket { get; set; }
}


