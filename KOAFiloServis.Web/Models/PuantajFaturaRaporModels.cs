namespace KOAFiloServis.Web.Models;

// ══════════════════════════════════════════════
// ENUM'LAR
// ══════════════════════════════════════════════

public enum PuantajFaturaYonu
{
    Gelir = 1,
    Gider = 2
}

public enum PuantajFaturaAgacYapisi
{
    CariAracGuzergah = 1,
    KurumAracGuzergah = 2,
    TedarikciAracGuzergah = 3,
    KurumGuzergahArac = 4
}

// ══════════════════════════════════════════════
// REQUEST
// ══════════════════════════════════════════════

public class PuantajFaturaRaporRequest
{
    public int Yil { get; set; }
    public int Ay { get; set; }

    public int? KurumId { get; set; }
    public int? CariId { get; set; }
    public int? AracId { get; set; }
    public int? GuzergahId { get; set; }

    public PuantajFaturaYonu Yon { get; set; } = PuantajFaturaYonu.Gelir;
    public PuantajFaturaAgacYapisi Agac { get; set; } = PuantajFaturaAgacYapisi.CariAracGuzergah;

    public string? Arama { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// ══════════════════════════════════════════════
// SATIR DTO
// ══════════════════════════════════════════════

public class PuantajFaturaSatirDto
{
    // Kaynak bilgisi
    public int KayitId { get; set; }
    public string Kaynak { get; set; } = "PuantajKayit"; // "PuantajKayit" veya "HakedisPuantaj"

    // Kurum
    public int? KurumId { get; set; }
    public string? KurumAdi { get; set; }

    // Fatura kesilecek / ödenecek Cari
    public int? CariId { get; set; }
    public string? CariUnvan { get; set; }
    public string? Telefon { get; set; }

    // Tedarikçi (gider tarafı için)
    public int? TedarikciId { get; set; }
    public string? TedarikciUnvan { get; set; }

    // Araç
    public int? AracId { get; set; }
    public string? Plaka { get; set; }

    // Şoför
    public int? SoforId { get; set; }
    public string? SoforAdi { get; set; }

    // Güzergah
    public int? GuzergahId { get; set; }
    public string? GuzergahAdi { get; set; }

    // Slot / Yön
    public string? SlotAdi { get; set; }
    public string? YonTipi { get; set; }

    // Finansal
    public decimal BirimGelir { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal BirimGider { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal KdvTutar { get; set; }
    /// <summary>
    /// HakedisPuantaj içinde KDV/10 ve KDV/20 ayrımı ayrı alanlarla tutulmadığı için
    /// şimdilik 0 bırakılmaktadır. Ayrım için ileride migration gerekir.
    /// </summary>
    public decimal Kdv10Tutar { get; set; }
    public decimal Kdv20Tutar { get; set; }
    public decimal KesintiTutar { get; set; }
    public decimal TahsilEdilecek { get; set; }
    public decimal Odenecek { get; set; }

    // Sefer
    public int ToplamSefer { get; set; }
    public decimal Gun { get; set; }

    // 31 gün kolonu
    public int Gun01 { get; set; } public int Gun02 { get; set; } public int Gun03 { get; set; }
    public int Gun04 { get; set; } public int Gun05 { get; set; } public int Gun06 { get; set; }
    public int Gun07 { get; set; } public int Gun08 { get; set; } public int Gun09 { get; set; }
    public int Gun10 { get; set; } public int Gun11 { get; set; } public int Gun12 { get; set; }
    public int Gun13 { get; set; } public int Gun14 { get; set; } public int Gun15 { get; set; }
    public int Gun16 { get; set; } public int Gun17 { get; set; } public int Gun18 { get; set; }
    public int Gun19 { get; set; } public int Gun20 { get; set; } public int Gun21 { get; set; }
    public int Gun22 { get; set; } public int Gun23 { get; set; } public int Gun24 { get; set; }
    public int Gun25 { get; set; } public int Gun26 { get; set; } public int Gun27 { get; set; }
    public int Gun28 { get; set; } public int Gun29 { get; set; } public int Gun30 { get; set; }
    public int Gun31 { get; set; }

    // Fatura durumu
    public bool FaturaKesildi { get; set; }
    public string? FaturaNo { get; set; }
    public DateTime? FaturaTarihi { get; set; }
}

// ══════════════════════════════════════════════
// AĞAÇ NODE DTO
// ══════════════════════════════════════════════

public class PuantajFaturaAgacNodeDto
{
    public string SeviyeAdi { get; set; } = string.Empty;
    public string? Etiket { get; set; }
    public int? ReferansId { get; set; }

    public List<PuantajFaturaSatirDto> Satirlar { get; set; } = new();
    public List<PuantajFaturaAgacNodeDto> Cocuklar { get; set; } = new();

    // Node özeti
    public int SatirSayisi { get; set; }
    public int ToplamSefer { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal ToplamKdv { get; set; }
    public decimal ToplamKesinti { get; set; }
    public decimal NetTutar { get; set; }
}

// ══════════════════════════════════════════════
// ÖZET DTO
// ══════════════════════════════════════════════

public class PuantajFaturaOzetDto
{
    public int ToplamKayit { get; set; }
    public int FaturaKesilen { get; set; }
    public int FaturaKesilmeyen { get; set; }
    public int ToplamSefer { get; set; }
    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal ToplamKdv { get; set; }
    public decimal ToplamKesinti { get; set; }
    public decimal NetGelir { get; set; }
    public decimal NetGider { get; set; }
    public decimal KarZarar { get; set; }
}
