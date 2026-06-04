using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Personel Avans İşlemleri
/// </summary>
public class PersonelAvans : BaseEntity, IFirmaTenant
{
    public int PersonelId { get; set; }
    public virtual Sofor Personel { get; set; } = null!;

    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public DateTime AvansTarihi { get; set; } = DateTime.Today;
    public decimal Tutar { get; set; }
    public string? Aciklama { get; set; }
    
    public AvansOdemeSekli OdemeSekli { get; set; } = AvansOdemeSekli.Nakit;
    
    // Banka/Kasa hesap bilgisi
    public int? BankaHesapId { get; set; }
    public virtual BankaHesap? BankaHesap { get; set; }

    // Muhasebe entegrasyonu
    public int? MuhasebeFisId { get; set; }
    public virtual MuhasebeFis? MuhasebeFis { get; set; }

    public AvansDurum Durum { get; set; } = AvansDurum.Verildi;
    
    // Mahsup/İade bilgileri
    public decimal MahsupEdilen { get; set; } = 0;
    [NotMapped]
    public decimal Kalan => Tutar - MahsupEdilen;
    [NotMapped]
    public bool TamamenMahsupEdildi => Kalan <= 0;
    
    public DateTime? MahsupTarihi { get; set; }
    public string? MahsupAciklamasi { get; set; }

    // Navigation
    public virtual ICollection<PersonelAvansMahsup> Mahsuplasmalar { get; set; } = new List<PersonelAvansMahsup>();
}

/// <summary>
/// Personele Borçlar İşlemleri
/// </summary>
public class PersonelBorc : BaseEntity, IFirmaTenant
{
    public int PersonelId { get; set; }
    public virtual Sofor Personel { get; set; } = null!;

    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public DateTime BorcTarihi { get; set; } = DateTime.Today;
    public decimal Tutar { get; set; }
    public string BorcNedeni { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    
    public BorcTipi BorcTipi { get; set; } = BorcTipi.MaasAlacagi;
    
    // Ödeme bilgileri
    public BorcOdemeDurum OdemeDurum { get; set; } = BorcOdemeDurum.Bekliyor;
    public DateTime? PlanlananOdemeTarihi { get; set; }
    public DateTime? GerceklesenOdemeTarihi { get; set; }
    
    // Ödeme detayları
    public decimal OdenenTutar { get; set; } = 0;
    [NotMapped]
    public decimal KalanBorc => Tutar - OdenenTutar;
    [NotMapped]
    public bool TamamenOdendi => KalanBorc <= 0;
    
    public BorcOdemeSekli? OdemeSekli { get; set; }
    
    // Banka/Kasa hesap bilgisi (ödeme yapıldığında)
    public int? BankaHesapId { get; set; }
    public virtual BankaHesap? BankaHesap { get; set; }

    // Muhasebe entegrasyonu
    public int? MuhasebeFisId { get; set; }
    public virtual MuhasebeFis? MuhasebeFis { get; set; }

    // Navigation
    public virtual ICollection<PersonelBorcOdeme> Odemeler { get; set; } = new List<PersonelBorcOdeme>();
}

/// <summary>
/// Avans Mahsuplaşma Kayıtları
/// </summary>
public class PersonelAvansMahsup : BaseEntity
{
    public int AvansId { get; set; }
    public virtual PersonelAvans Avans { get; set; } = null!;

    public DateTime MahsupTarihi { get; set; } = DateTime.Today;
    public decimal MahsupTutari { get; set; }
    public string? Aciklama { get; set; }
    
    public MahsupSekli MahsupSekli { get; set; } = MahsupSekli.MaastanKesinti;
    
    // Maaş kesintisi ise
    public int? MaasId { get; set; }
    public virtual PersonelMaas? Maas { get; set; }
    
    // İade ise
    public int? BankaHesapId { get; set; }
    public virtual BankaHesap? BankaHesap { get; set; }
}

/// <summary>
/// Personel Borç Ödeme Kayıtları
/// </summary>
public class PersonelBorcOdeme : BaseEntity
{
    public int BorcId { get; set; }
    public virtual PersonelBorc Borc { get; set; } = null!;

    public DateTime OdemeTarihi { get; set; } = DateTime.Today;
    public decimal OdemeTutari { get; set; }
    public string? Aciklama { get; set; }
    
    public BorcOdemeSekli OdemeSekli { get; set; } = BorcOdemeSekli.Nakit;
    
    public int? BankaHesapId { get; set; }
    public virtual BankaHesap? BankaHesap { get; set; }
    
    // Muhasebe entegrasyonu
    public int? MuhasebeFisId { get; set; }
    public virtual MuhasebeFis? MuhasebeFis { get; set; }
}

/// <summary>
/// Personel Finans Ayarları (Muhasebe Hesap Kodları Eşleştirme)
/// </summary>
public class PersonelFinansAyar : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    // Avans hesap kodları
    public int? PersonelAvanslariHesapId { get; set; } // 195 - Personel Avansları
    public virtual MuhasebeHesap? PersonelAvanslariHesap { get; set; }
    
    // Borç hesap kodları
    public int? PersoneleBorclarHesapId { get; set; } // 335 - Personele Borçlar
    public virtual MuhasebeHesap? PersoneleBorclarHesap { get; set; }
    
    // Kasa/Banka hesap kodları
    public int? KasaHesapId { get; set; } // 100 - Kasa
    public virtual MuhasebeHesap? KasaHesap { get; set; }
    
    public int? BankaHesapId { get; set; } // 102 - Bankalar
    public virtual MuhasebeHesap? BankaHesap { get; set; }

    // Bordro muhasebe hesap kodları
    public int? OdenecekVergiHesapId { get; set; } // 360 - Ödenecek Vergi ve Fonlar
    public virtual MuhasebeHesap? OdenecekVergiHesap { get; set; }

    public int? OdenecekSgkHesapId { get; set; } // 361 - Ödenecek SGK Primleri
    public virtual MuhasebeHesap? OdenecekSgkHesap { get; set; }

    public int? PersonelGiderHesapId { get; set; } // 770 - Genel Yönetim Giderleri
    public virtual MuhasebeHesap? PersonelGiderHesap { get; set; }

    public int? DigerYukumlulukHesapId { get; set; } // 368 - Ödenecek Diğer Yükümlülükler
    public virtual MuhasebeHesap? DigerYukumlulukHesap { get; set; }

    // Otomatik fiş oluşturma ayarları
    public bool OtomatikFisOlustur { get; set; } = true;
    public bool AvansVerildigindeFisOlustur { get; set; } = true;
    public bool AvansMahsupFisOlustur { get; set; } = true;
    public bool BorcOdendigindeFisOlustur { get; set; } = true;
}

// Enums
public enum AvansOdemeSekli
{
    Nakit = 1,
    BankaTransfer = 2,
    Cek = 3,
    Senet = 4
}

public enum AvansDurum
{
    Verildi = 1,
    KismenMahsup = 2,
    TamamenMahsup = 3,
    IptalEdildi = 4
}

public enum BorcTipi
{
    MaasAlacagi = 1,
    PrimAlacagi = 2,
    MesaiAlacagi = 3,
    IkramiyeAlacagi = 4,
    TazminatAlacagi = 5,
    Diger = 99
}

public enum BorcOdemeDurum
{
    Bekliyor = 1,
    KismenOdendi = 2,
    TamamenOdendi = 3,
    IptalEdildi = 4,
    Ertelendi = 5
}

public enum BorcOdemeSekli
{
    Nakit = 1,
    BankaTransfer = 2,
    Cek = 3,
    MaastanKesinti = 4,
    Mahsup = 5,
    KrediKarti = 6
}

public enum MahsupSekli
{
    MaastanKesinti = 1,
    NakitIade = 2,
    BankaIade = 3,
    Diger = 99
}
