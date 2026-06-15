namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Kasa / Banka finans hareketi.
/// Excel import veya manuel giriş ile oluşturulur.
/// Her hareket → 1 muhasebe fişi.
/// </summary>
public class FinansHareket : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }

    public DateTime Tarih { get; set; }

    /// <summary>Kasa veya Banka</summary>
    public string Tip { get; set; } = "Banka";

    public decimal Tutar { get; set; }

    /// <summary>true = Borç, false = Alacak</summary>
    public bool BorcMu { get; set; } = true;

    /// <summary>Hesap ID'si (100 Kasa, 102 Banka vb.)</summary>
    public int HesapId { get; set; }
    public virtual MuhasebeHesap? Hesap { get; set; }

    /// <summary>Karşı hesap ID'si (120 Cari, 320 Satıcı, 770 Gider vb.)</summary>
    public int KarsiHesapId { get; set; }
    public virtual MuhasebeHesap? KarsiHesap { get; set; }

    public string Aciklama { get; set; } = string.Empty;
    public string? ReferansNo { get; set; }

    /// <summary>Muhasebe fişine aktarıldı mı?</summary>
    public bool AktarildiMi { get; set; }

    /// <summary>Oluşturulan muhasebe fişi ID'si.</summary>
    public int? MuhasebeFisId { get; set; }

    /// <summary>İptal ters fiş ID'si.</summary>
    public int? IptalFisId { get; set; }
}
