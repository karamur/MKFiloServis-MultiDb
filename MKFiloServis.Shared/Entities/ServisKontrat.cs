namespace MKFiloServis.Shared.Entities;

// ─── Enums ────────────────────────────────────────────────────────────────────

/// <summary>Servis sözleşmesi türü</summary>
public enum ServisKontratTip
{
    /// <summary>Özmal araç + kendi personelimiz</summary>
    Ozmal = 0,
    /// <summary>C-plakalı kiralık araç + kendi personelimiz</summary>
    Kiralik = 1,
    /// <summary>Dış tedarikçi (alt yüklenici) üzerinden</summary>
    Tedarikci = 2
}

/// <summary>Fatura/ödeme hesaplama yöntemi</summary>
public enum ServisFiyatTip
{
    Aylik = 0,
    SeferBazi = 1,
    Gunluk = 2
}

/// <summary>Sözleşme durumu</summary>
public enum ServisKontratDurum
{
    Aktif = 0,
    Tamamlandi = 1,
    IptalEdildi = 2
}

/// <summary>Puantaj kapanma durumu</summary>
public enum ServisPuantajDurum
{
    Taslak = 0,
    Onaylandi = 1,
    Kapandi = 2
}

/// <summary>Ödeme/tahsilat şekli</summary>
public enum OdemeVeTahsilatSekli
{
    Nakit = 0,
    BankaHavalesi = 1,
    EFT = 2,
    Cek = 3,
    Diger = 4
}

// ─── ServisKontrat ────────────────────────────────────────────────────────────

/// <summary>
/// Servis operasyon sözleşmesi.
/// Özmal/Kiralık: Kendi araç+personelimiz ile kurum/firmaya hizmet.
/// Tedarikçi: Alt yüklenicinin araç+personeli ile güzergahta çalışma.
/// Her iki tipte de kuruma fatura kesilir; tedarikçi tipinde ayrıca tedarikçiye ödeme yapılır.
/// </summary>
public class ServisKontrat : BaseEntity
{
    /// <summary>Otomatik kod (KNT-00001 vb.)</summary>
    public string KontratKodu { get; set; } = string.Empty;

    /// <summary>Fatura kesilen kurum/firma (Cari)</summary>
    public int? KurumCariId { get; set; }
    public virtual Cari? KurumCari { get; set; }

    /// <summary>Güzergah</summary>
    public int GuzergahId { get; set; }
    public virtual Guzergah? Guzergah { get; set; }

    /// <summary>Sözleşme türü</summary>
    public ServisKontratTip Tip { get; set; } = ServisKontratTip.Ozmal;

    // ── Özmal / Kiralık alanları ──
    /// <summary>Özmal veya kiralık araç (Kiralik tipinde bizim aracımız)</summary>
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    /// <summary>Atanan şoför/personel (kendi personelimiz)</summary>
    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }

    // ── Tedarikçi alanları ──
    /// <summary>Tedarikçi firma (tip=Tedarikci ise)</summary>
    public int? TasimaTedarikciId { get; set; }
    public virtual TasimaTedarikci? TasimaTedarikci { get; set; }

    /// <summary>Tedarikçi iş kaydına opsiyonel bağlantı</summary>
    public int? TasimaTedarikciIsId { get; set; }
    public virtual TasimaTedarikciIs? TasimaTedarikciIs { get; set; }

    // ── Sözleşme tarihleri ──
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;
    public DateTime? BitisTarihi { get; set; }

    // ── Tahsilat (kurumdan alınacak) ──
    public ServisFiyatTip TahsilatTip { get; set; } = ServisFiyatTip.Aylik;
    public decimal? TahsilatBirimFiyat { get; set; }

    // ── Ödeme (tedarikçiye ödenecek) ──
    public ServisFiyatTip? OdemeTip { get; set; }
    public decimal? OdemeBirimFiyat { get; set; }

    public ServisKontratDurum Durum { get; set; } = ServisKontratDurum.Aktif;
    public string? Aciklama { get; set; }
    public string? Notlar { get; set; }

    // ── Navigation ──
    public virtual ICollection<ServisPuantaj> Puantajlar { get; set; } = new List<ServisPuantaj>();
}

// ─── ServisPuantaj ────────────────────────────────────────────────────────────

/// <summary>
/// Aylık/dönemsel puantaj kaydı.
/// Bir kontrata ait dönemde kaç gün/sefer çalışıldığını, tahsilat ve ödeme
/// tutarlarını ve ödeme/tahsilat belgelerini barındırır.
/// </summary>
public class ServisPuantaj : BaseEntity
{
    public int ServisKontratId { get; set; }
    public virtual ServisKontrat? ServisKontrat { get; set; }

    public int Yil { get; set; }
    public int Ay { get; set; }

    /// <summary>Dönemdeki çalışma günü veya sefer sayısı</summary>
    public decimal CalismaSayisi { get; set; }

    // ── Tahsilat ──
    public decimal TahsilatBirimFiyat { get; set; }
    /// <summary>Kurumdan kesilecek fatura tutarı (CalismaSayisi × TahsilatBirimFiyat veya manuel)</summary>
    public decimal TahsilatToplam { get; set; }

    // ── Ödeme (tedarikçi tipinde) ──
    public decimal? OdemeBirimFiyat { get; set; }
    /// <summary>Tedarikçiye ödenecek toplam (CalismaSayisi × OdemeBirimFiyat veya manuel)</summary>
    public decimal? OdemeToplam { get; set; }

    public ServisPuantajDurum Durum { get; set; } = ServisPuantajDurum.Taslak;
    public string? OnayanKisi { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? Notlar { get; set; }

    // ── Navigation ──
    public virtual ICollection<ServisOdeme> Odemeler { get; set; } = new List<ServisOdeme>();
    public virtual ICollection<ServisTahsilat> Tahsilatlar { get; set; } = new List<ServisTahsilat>();
}

// ─── ServisOdeme ─────────────────────────────────────────────────────────────

/// <summary>
/// Tedarikçiye veya serbest personele yapılan ödeme kaydı.
/// Her ödeme bir puantaja bağlıdır; puantaj onaylandığında ödeme tetiklenebilir.
/// </summary>
public class ServisOdeme : BaseEntity
{
    public int ServisPuantajId { get; set; }
    public virtual ServisPuantaj? ServisPuantaj { get; set; }

    public DateTime OdemeTarihi { get; set; } = DateTime.Today;
    public decimal Tutar { get; set; }
    public OdemeVeTahsilatSekli OdemeSekli { get; set; }
    public string? BelgeNo { get; set; }
    public string? Aciklama { get; set; }
    public bool Odendi { get; set; } = false;
}

// ─── ServisTahsilat ───────────────────────────────────────────────────────────

/// <summary>
/// Kurumdan/firmadan yapılan tahsilat kaydı.
/// Her tahsilat bir puantaja bağlıdır; fatura kesildikten sonra tahsilat izlenir.
/// </summary>
public class ServisTahsilat : BaseEntity
{
    public int ServisPuantajId { get; set; }
    public virtual ServisPuantaj? ServisPuantaj { get; set; }

    public DateTime TahsilatTarihi { get; set; } = DateTime.Today;
    public decimal Tutar { get; set; }
    public OdemeVeTahsilatSekli TahsilatSekli { get; set; }
    public string? BelgeNo { get; set; }
    public string? Aciklama { get; set; }
    public bool Tahsil { get; set; } = false;
}


