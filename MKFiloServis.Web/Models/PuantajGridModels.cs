namespace MKFiloServis.Web.Models;

/// <summary>
/// Puantaj hücre modu — kullanıcı manuel mi girdi, engine mi hesapladı?
/// </summary>
public enum PuantajHucreModu
{
    Auto = 0,
    Manual = 1
}

/// <summary>
/// Excel-benzeri puantaj grid'indeki tek bir hücre.
/// </summary>
public class PuantajHucre
{
    public int Gun { get; set; }
    public int Deger { get; set; }        // 0, 1, 2 (standart sefer)
    public PuantajHucreModu Mod { get; set; } = PuantajHucreModu.Auto;
    public bool Secili { get; set; }

    // Esnek sefer modeli
    public int Mesai { get; set; }         // kaç mesai seferi (0-N)
    public int EkSefer { get; set; }       // kaç ek sefer (0-N)
    public decimal FiyatCarpani { get; set; } = 1m; // 1=normal, 1.5=mesai, 2=tatil

    public bool IsManual => Mod == PuantajHucreModu.Manual;
    public bool IsDirty => Mod == PuantajHucreModu.Manual;
    public int OldValue { get; set; } // Preview için eski değer

    public int ToplamSefer
    {
        get
        {
            var toplam = Deger + Mesai + EkSefer;
            if (toplam < 0) return 0;
            if (toplam > 10) return 10; // 🔴 Üst limit
            return toplam;
        }
    }

    public PuantajHucre Clone() => new()
    {
        Gun = Gun, Deger = Deger, Mod = Mod, Secili = Secili,
        Mesai = Mesai, EkSefer = EkSefer, FiyatCarpani = FiyatCarpani
    };
}

/// <summary>
/// Grid'deki bir satır (bir PuantajKayit'a karşılık gelir).
/// </summary>
public class PuantajGridSatir
{
    public int KayitId { get; set; }
    public string? KurumAdi { get; set; }
    public string? GuzergahAdi { get; set; }
    public string? SoforAdi { get; set; }
    public string? Plaka { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal Kesinti { get; set; }
    public List<PuantajHucre> Hucreler { get; set; } = new();

    public bool IsSelected { get; set; }
    public bool IsDirty => Hucreler.Any(h => h.Mod == PuantajHucreModu.Manual);

    public int ToplamSefer => Hucreler.Sum(h => h.ToplamSefer);
    public decimal ToplamTutar => ToplamSefer * BirimFiyat;
    public decimal Net => ToplamTutar - Kesinti;

    public PuantajGridSatir Clone()
    {
        var c = (PuantajGridSatir)MemberwiseClone();
        c.Hucreler = Hucreler.Select(h => h.Clone()).ToList();
        return c;
    }
}

/// <summary>
/// Undo/Redo için state snapshot'ı.
/// </summary>
public class PuantajGridState
{
    public List<PuantajGridSatir> Satirlar { get; set; } = new();

    public PuantajGridState Clone()
    {
        return new PuantajGridState
        {
            Satirlar = Satirlar.Select(s => s.Clone()).ToList()
        };
    }
}



