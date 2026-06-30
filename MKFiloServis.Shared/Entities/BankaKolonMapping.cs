namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Kullanıcı tanımlı banka Excel/CSV kolon eşleme.
/// Firma bazlı saklanır. Her firma kendi banka formatını tanımlayabilir.
/// </summary>
public class BankaKolonMapping : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }

    /// <summary>Kolay ad (örn: "Ziraat Kurumsal", "Garanti BBVA")</summary>
    public string Ad { get; set; } = string.Empty;

    /// <summary>Tarih kolon index (1-bazlı)</summary>
    public int TarihKolon { get; set; } = 1;
    /// <summary>Açıklama kolon index</summary>
    public int AciklamaKolon { get; set; } = 2;
    /// <summary>Tutar kolon index</summary>
    public int TutarKolon { get; set; } = 3;
    /// <summary>Borç/Alacak kolon index (B/A, Borç/Alacak, Giden/Gelen)</summary>
    public int BorcAlacakKolon { get; set; }
    /// <summary>Referans/İşlem no kolon index</summary>
    public int ReferansKolon { get; set; }

    /// <summary>Dosya tipi: Excel, CSV, XML</summary>
    public string DosyaTipi { get; set; } = "CSV";
    /// <summary>Ayraç karakteri (CSV için: ; , tab)</summary>
    public string Ayrac { get; set; } = ";";
    /// <summary>Başlık satırı var mı?</summary>
    public bool BaslikVarMi { get; set; } = true;
    /// <summary>Başlık satırından sonra atlanacak satır sayısı</summary>
    public int AtlanacakSatir { get; set; }

    /// <summary>Borç göstergesi (örn: "B", "BORÇ", "Giden")</summary>
    public string? BorcGostergesi { get; set; } = "B";
    /// <summary>Alacak göstergesi (örn: "A", "ALACAK", "Gelen")</summary>
    public string? AlacakGostergesi { get; set; } = "A";

    /// <summary>Tarih formatı (örn: dd.MM.yyyy, yyyy-MM-dd)</summary>
    public string TarihFormati { get; set; } = "dd.MM.yyyy";
    /// <summary>Sayı formatı (nokta/virgül ayracı)</summary>
    public string SayiAyraci { get; set; } = ",";

    /// <summary>Varsayılan olarak seçilsin mi?</summary>
    public bool Varsayilan { get; set; }
}


