using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Araç bilgileri - Şase numarasına göre tekil
/// </summary>
public class Arac : BaseEntity
{
    /// <summary>
    /// Multi-tenant: Şirket ID (null = sistem geneli)
    /// </summary>
    public int? SirketId { get; set; }
    public virtual Sirket? Sirket { get; set; }

    // Şase numarası - Tekil (Unique)
    public string SaseNo { get; set; } = string.Empty;
    
    // Eski kod ile uyumluluk için korunan, veritabanına map edilmeyen alan
    [NotMapped]
    public string Plaka
    {
        get => AktifPlaka ?? string.Empty;
        set => AktifPlaka = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    // Aktif plaka - Otomatik hesaplanır
    public string? AktifPlaka { get; set; }
    
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public int? ModelYili { get; set; }
    public string? MotorNo { get; set; }
    public string? Renk { get; set; }
    public int KoltukSayisi { get; set; }
    public AracTipi AracTipi { get; set; }
    public AracSinifi AracSinifi { get; set; } = AracSinifi.PersonelTasiti;
    public AracSahiplikTipi SahiplikTipi { get; set; } = AracSahiplikTipi.Ozmal;
    
    // Kiralık araç bilgileri
    public int? KiralikCariId { get; set; } // Araç sahibi (kiralık ise)
    public decimal? GunlukKiraBedeli { get; set; }
    public decimal? AylikKiraBedeli { get; set; }
    public decimal? SeferBasinaKiraBedeli { get; set; }
    public KiraHesaplamaTipi? KiraHesaplamaTipi { get; set; }
    
    // Komisyon bilgileri
    public bool KomisyonVar { get; set; } = false;
    public int? KomisyoncuCariId { get; set; } // Komisyoncu
    public decimal? KomisyonOrani { get; set; } // Yüzde
    public decimal? SabitKomisyonTutari { get; set; } // Sefer başına sabit tutar
    public KomisyonHesaplamaTipi? KomisyonHesaplamaTipi { get; set; }

    // Personel Taşıma Tedarikçisi (alt yüklenici)
    // Doluysa bu araç bir tedarikçiye ait; ruhsat/sigorta/muayene takibi yine AracEvrak üzerinden tek kaynaktan yapılır.
    public int? TasimaTedarikciId { get; set; }
    public virtual TasimaTedarikci? TasimaTedarikci { get; set; }

    // Geriye dönük uyumluluk için araç üzerindeki belge tarihleri
    public DateTime? TrafikSigortaBitisTarihi { get; set; }
    public DateTime? KaskoBitisTarihi { get; set; }
    public DateTime? MuayeneBitisTarihi { get; set; }
    public DateTime? KoltukSigortasiBaslangiçTarihi { get; set; }
    public DateTime? KoltukSigortasiBitisTarihi { get; set; }

    public int? KmDurumu { get; set; }
    public AracDurumu Durumu { get; set; } = AracDurumu.Bosta;
    public bool Aktif { get; set; } = true;
    public string? Notlar { get; set;}
    
    // Satış durumu
    public bool SatisaAcik { get; set; } = false;
    public decimal? SatisFiyati { get; set; }
    public DateTime? SatisaAcilmaTarihi { get; set; }
    public string? SatisAciklamasi { get; set; }

    // Navigation Properties
    public virtual Cari? KiralikCari { get; set; }
    public virtual Cari? KomisyoncuCari { get; set; }
    public virtual ICollection<AracPlaka> PlakaGecmisi { get; set; } = new List<AracPlaka>();
    public virtual ICollection<AracMasraf> Masraflar { get; set; } = new List<AracMasraf>();
    public virtual ICollection<ServisCalisma> ServisCalismalari { get; set; } = new List<ServisCalisma>();
    public virtual ICollection<BakimPeriyot> BakimPeriyotlari { get; set; } = new List<BakimPeriyot>();
    
    // Hesaplanan Özellik - Aktif plakayı döner (CikisTarihi null veya bugünden sonra)
    public AracPlaka? AktifPlakaKaydi => PlakaGecmisi?
        .Where(p => !p.IsDeleted && (p.CikisTarihi == null || p.CikisTarihi > DateTime.Today))
        .OrderByDescending(p => p.GirisTarihi)
        .FirstOrDefault();
}

/// <summary>
/// Araç plaka geçmişi - Her şase için birden fazla plaka olabilir
/// </summary>
public class AracPlaka : BaseEntity
{
    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;
    
    public string Plaka { get; set; } = string.Empty;
    
    // Plaka dönemi
    public DateTime GirisTarihi { get; set; }
    public DateTime? CikisTarihi { get; set; }
    
    // İşlem tipi
    public PlakaIslemTipi IslemTipi { get; set; }
    
    // Ek bilgiler
    public string? Aciklama { get; set; }
    public decimal? IslemTutari { get; set; } // Alış/Satış fiyatı
    
    // İlişkili kayıtlar
    public int? CariId { get; set; } // Kimden alındı / Kime satıldı
    public virtual Cari? Cari { get; set; }
    
    // Aktif mi? (CikisTarihi null veya gelecek tarihli ise aktif)
    public bool Aktif => CikisTarihi == null || CikisTarihi > DateTime.Today;
}

public enum PlakaIslemTipi
{
    Alis = 1,           // Araç alışı
    Satis = 2,          // Araç satışı
    PlakaDevir = 3,     // Plaka devri (aynı şase, farklı plaka)
    Servis = 4,         // Servis girişi
    Kiralama = 5,       // Kiralamaya verildi
    KiralamaBitis = 6,  // Kiralamadan döndü
    TramerKaydi = 7,    // Tramer kaydı
    Diger = 99
}

public enum AracTipi
{
    Minibus = 1,
    Midibus = 2,
    Otobus = 3,
    Otomobil = 4,
    Panelvan = 5
}

public enum AracSinifi
{
    PersonelTasiti = 1,
    OkulTasiti = 2
}

public enum AracSahiplikTipi
{
    Ozmal = 1,
    Kiralik = 2,
    Komisyon = 3,
    Diger = 4,
    Tedarikci = 5
}

public enum KiraHesaplamaTipi
{
    Gunluk = 1,
    Aylik = 2,
    SeferBasina = 3
}

public enum KomisyonHesaplamaTipi
{
    YuzdeOrani = 1,
    SabitTutar = 2
}

public enum AracDurumu
{
    Bosta = 1,
    Operasyon = 2,
    Yonetim = 3,
    Satis = 4
}



