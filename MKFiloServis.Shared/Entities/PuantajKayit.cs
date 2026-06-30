using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Puantaj/Hakedis Kayıtları - Excel'den içe aktarılan ve manuel düzenlenebilen kayıtlar
/// </summary>
public class PuantajKayit : BaseEntity
{
    // Dönem Bilgisi
    public int Yil { get; set; }
    public int Ay { get; set; }

    // Bölge / Sıra Bilgisi (Excel şablonundan)
    public string? Bolge { get; set; }
    public int SiraNo { get; set; }

    // Kurum/Firma (Müşteri)
    public int? KurumCariId { get; set; }
    public virtual Cari? KurumCari { get; set; }
    public string? KurumAdi { get; set; } // Excel'den gelen ham değer
    public int? KurumId { get; set; } // Kurum entity referansı (Cari'den ayrı)
    public virtual Kurum? Kurum { get; set; }

    // İşveren Firma (hangi firma adına çalışılıyor)
    public int? IsverenFirmaId { get; set; }
    public virtual Firma? IsverenFirma { get; set; }

    // Güzergah (Semt)
    public int? GuzergahId { get; set; }
    public virtual Guzergah? Guzergah { get; set; }
    public string? GuzergahAdi { get; set; } // Excel'den gelen ham değer

    // Sefer Slot (günlük zaman dilimi)
    public SeferSlot Slot { get; set; } = SeferSlot.Sabah;

    // Özel slot adı (Slot enum dışı özel isimlendirme: "Gece Vardiyası", "Öğle" vb.)
    [StringLength(50)]
    public string? SlotAdi { get; set; }

    // Yön (S=Sabah, A=Akşam, S/A=Sabah-Akşam)
    public PuantajYon Yon { get; set; } = PuantajYon.SabahAksam;

    // Araç (Plaka) - plakaya göre öz mal/kiralama tespiti
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }
    public string? Plaka { get; set; } // Excel'den gelen ham değer

    // Şoför
    public int? SoforId { get; set; }
    public virtual Sofor? Sofor { get; set; }
    public string? SoforAdi { get; set; } // Excel'den gelen ham değer
    public string? SoforTelefon { get; set; }

    // Ait yada kiralayan firma adı
    public string? AitFirmaAdi { get; set; }

    // Kaynak ve Finans
    public PlanlamaKaynakTipi KaynakTipi { get; set; } = PlanlamaKaynakTipi.Kendi;
    public PlanlamaFinansYonu FinansYonu { get; set; } = PlanlamaFinansYonu.Giden;
    public string? BelgeNo { get; set; }
    public string? TransferDurum { get; set; }

    // Şoför Tipi (Özel, Kiralık, Komisyoncu)
    public SoforOdemeTipi SoforOdemeTipi { get; set; } = SoforOdemeTipi.Ozmal;

    // Ödeme Yapılacak Firma/Cari (Kiralık/Komisyoncu için)
    public int? OdemeYapilacakCariId { get; set; }
    public virtual Cari? OdemeYapilacakCari { get; set; }

    // Fatura Kesen Cari (Müşteriye fatura kesen firma)
    public int? FaturaKesiciCariId { get; set; }
    public virtual Cari? FaturaKesiciCari { get; set; }
    public string? FaturaKesiciAdi { get; set; } // Excel'den gelen ham değer
    public string? FaturaKesiciTelefon { get; set; }

    // Gün/Sefer Bilgisi
    public decimal Gun { get; set; } // Çarpan (örn: 1, 0.5, 22 gün vb.)
    public int SeferSayisi { get; set; } = 1;

    // Günlük Puantaj (Ayın 1-31 günleri için sefer sayısı: 0=gitmedi, 1=tek sefer, 2=S+A)
    public int Gun01 { get; set; }
    public int Gun02 { get; set; }
    public int Gun03 { get; set; }
    public int Gun04 { get; set; }
    public int Gun05 { get; set; }
    public int Gun06 { get; set; }
    public int Gun07 { get; set; }
    public int Gun08 { get; set; }
    public int Gun09 { get; set; }
    public int Gun10 { get; set; }
    public int Gun11 { get; set; }
    public int Gun12 { get; set; }
    public int Gun13 { get; set; }
    public int Gun14 { get; set; }
    public int Gun15 { get; set; }
    public int Gun16 { get; set; }
    public int Gun17 { get; set; }
    public int Gun18 { get; set; }
    public int Gun19 { get; set; }
    public int Gun20 { get; set; }
    public int Gun21 { get; set; }
    public int Gun22 { get; set; }
    public int Gun23 { get; set; }
    public int Gun24 { get; set; }
    public int Gun25 { get; set; }
    public int Gun26 { get; set; }
    public int Gun27 { get; set; }
    public int Gun28 { get; set; }
    public int Gun29 { get; set; }
    public int Gun30 { get; set; }
    public int Gun31 { get; set; }

    // Sefer Günü Toplamı (Gun01+Gun02+...+Gun31)
    [NotMapped]
    public int SeferGunuToplami => Gun01 + Gun02 + Gun03 + Gun04 + Gun05 + Gun06 + Gun07 +
        Gun08 + Gun09 + Gun10 + Gun11 + Gun12 + Gun13 + Gun14 + Gun15 + Gun16 + Gun17 +
        Gun18 + Gun19 + Gun20 + Gun21 + Gun22 + Gun23 + Gun24 + Gun25 + Gun26 + Gun27 +
        Gun28 + Gun29 + Gun30 + Gun31;

    /// <summary>
    /// Günlük puantaj değerini index ile al/ata (gun: 1-31)
    /// </summary>
    public int GetGunDeger(int gun) => gun switch
    {
        1 => Gun01, 2 => Gun02, 3 => Gun03, 4 => Gun04, 5 => Gun05,
        6 => Gun06, 7 => Gun07, 8 => Gun08, 9 => Gun09, 10 => Gun10,
        11 => Gun11, 12 => Gun12, 13 => Gun13, 14 => Gun14, 15 => Gun15,
        16 => Gun16, 17 => Gun17, 18 => Gun18, 19 => Gun19, 20 => Gun20,
        21 => Gun21, 22 => Gun22, 23 => Gun23, 24 => Gun24, 25 => Gun25,
        26 => Gun26, 27 => Gun27, 28 => Gun28, 29 => Gun29, 30 => Gun30,
        31 => Gun31, _ => 0
    };

    public void SetGunDeger(int gun, int deger)
    {
        switch (gun)
        {
            case 1: Gun01 = deger; break; case 2: Gun02 = deger; break;
            case 3: Gun03 = deger; break; case 4: Gun04 = deger; break;
            case 5: Gun05 = deger; break; case 6: Gun06 = deger; break;
            case 7: Gun07 = deger; break; case 8: Gun08 = deger; break;
            case 9: Gun09 = deger; break; case 10: Gun10 = deger; break;
            case 11: Gun11 = deger; break; case 12: Gun12 = deger; break;
            case 13: Gun13 = deger; break; case 14: Gun14 = deger; break;
            case 15: Gun15 = deger; break; case 16: Gun16 = deger; break;
            case 17: Gun17 = deger; break; case 18: Gun18 = deger; break;
            case 19: Gun19 = deger; break; case 20: Gun20 = deger; break;
            case 21: Gun21 = deger; break; case 22: Gun22 = deger; break;
            case 23: Gun23 = deger; break; case 24: Gun24 = deger; break;
            case 25: Gun25 = deger; break; case 26: Gun26 = deger; break;
            case 27: Gun27 = deger; break; case 28: Gun28 = deger; break;
            case 29: Gun29 = deger; break; case 30: Gun30 = deger; break;
            case 31: Gun31 = deger; break;
        }
    }
    
    // GELİR (Müşteriden Alınacak)
    [Column(TypeName = "decimal(18,2)")]
    public decimal BirimGelir { get; set; } // Sefer/gün başına

    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamGelir { get; set; } // BirimGelir * Gun

    public int GelirKdvOrani { get; set; } = 20; // %20 veya %10

    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirKdvTutari { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirToplam { get; set; } // KDV dahil toplam gelir

    // Gelir KDV Detayları (Gider tarafı gibi ayrıntılı)
    public int GelirKdvOrani20 { get; set; } = 0; // %20 KDV'li kısım tutarı
    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirKdv20Tutari { get; set; }

    public int GelirKdvOrani10 { get; set; } = 0; // %10 KDV'li kısım tutarı
    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirKdv10Tutari { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirKesinti { get; set; } // Kesintiler

    [Column(TypeName = "decimal(18,2)")]
    public decimal Alinacak { get; set; } // Net alınacak tutar (ToplamGelir + KDVler - Kesintiler)
    
    // GİDER (Şoför/Tedarikçiye Ödenecek)
    [Column(TypeName = "decimal(18,2)")]
    public decimal BirimGider { get; set; } // Sefer/gün başına
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ToplamGider { get; set; } // BirimGider * Gun
    
    // Gider KDV Oranları (Stopaj/Kesinti dahil)
    public int GiderKdvOrani20 { get; set; } = 0; // %20 KDV'li kısım tutarı (yüzde değil tutar)
    [Column(TypeName = "decimal(18,2)")]
    public decimal GiderKdv20Tutari { get; set; }
    
    public int GiderKdvOrani10 { get; set; } = 0; // %10 KDV'li kısım tutarı
    [Column(TypeName = "decimal(18,2)")]
    public decimal GiderKdv10Tutari { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal GiderKesinti { get; set; } // Stopaj veya diğer kesintiler
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Odenecek { get; set; } // Net ödenecek tutar (ToplamGider + KDVler - Kesintiler)

    // FARK (Gelir - Gider)
    [NotMapped]
    public decimal FarkTutari => Alinacak - Odenecek; // Pozitif: Kar, Negatif: Zarar
    
    // Fatura Durumu - GELİR (Müşteriye Kesilen Fatura)
    public bool GelirFaturaKesildi { get; set; } = false;
    public string? GelirFaturaNo { get; set; }
    public DateTime? GelirFaturaTarihi { get; set; }
    public int? GelirFaturaId { get; set; }
    
    // Fatura Durumu - GİDER (Tedarikçiden Alınan Fatura)
    public bool GiderFaturaAlindi { get; set; } = false;
    public string? GiderFaturaNo { get; set; }
    public DateTime? GiderFaturaTarihi { get; set; }
    public int? GiderFaturaId { get; set; }
    
    // Ödeme Durumu - GELİR (Müşteriden Tahsilat)
    public PuantajOdemeDurum GelirOdemeDurumu { get; set; } = PuantajOdemeDurum.Odenmedi;
    public DateTime? GelirOdemeTarihi { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal GelirOdenenTutar { get; set; }

    // Ödeme Durumu - GİDER (Tedarikçiye Ödeme)
    public PuantajOdemeDurum GiderOdemeDurumu { get; set; } = PuantajOdemeDurum.Odenmedi;
    public DateTime? GiderOdemeTarihi { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal GiderOdenenTutar { get; set; }
    
    // Onay Durumu
    public PuantajOnayDurum OnayDurum { get; set; } = PuantajOnayDurum.Taslak;
    public string? OnaylayanKullanici { get; set; }
    public DateTime? OnayTarihi { get; set; }
    
    // Kaynak Bilgisi
    public PuantajKaynak Kaynak { get; set; } = PuantajKaynak.Manuel;
    public int? ExcelImportId { get; set; }
    public int? ExcelSatirNo { get; set; }

    // ── Hesap Dönemi + Revizyon (PuantajEngine V1) ──────────────────────
    public int? HesapDonemiId { get; set; }
    public virtual PuantajHesapDonemi? HesapDonemi { get; set; }

    public int? OncekiVersiyonId { get; set; }
    public virtual PuantajKayit? OncekiVersiyon { get; set; }

    public int Versiyon { get; set; } = 1;

    // ── Detay bağlantısı ─────────────────────────────────────────────────
    public virtual ICollection<PuantajDetay> PuantajDetaylari { get; set; } = new List<PuantajDetay>();

    // Notlar ve Açıklamalar
    public string? Notlar { get; set; }

    // Hesaplama metodları
    public void HesaplaGelir()
    {
        ToplamGelir = BirimGelir * Gun;
        GelirKdvTutari = GelirKdv20Tutari + GelirKdv10Tutari;
        if (GelirKdvTutari == 0 && GelirKdvOrani > 0)
        {
            // Eğer ayrıntılı KDV girilmemişse genel orandan hesapla
            GelirKdvTutari = ToplamGelir * GelirKdvOrani / 100;
        }
        GelirToplam = ToplamGelir + GelirKdvTutari;
        Alinacak = ToplamGelir + GelirKdv20Tutari + GelirKdv10Tutari - GelirKesinti;
    }
    
    public void HesaplaGider()
    {
        ToplamGider = BirimGider * Gun;
        // KDV tutarları zaten Excel'den geliyor veya manuel giriliyor
        Odenecek = ToplamGider + GiderKdv20Tutari + GiderKdv10Tutari - GiderKesinti;
    }

    /// <summary>
    /// Günlük puantajdan Gun (sefer günü toplamı) ve Toplam hesapla
    /// </summary>
    public void HesaplaPuantajToplam()
    {
        Gun = SeferGunuToplami;
        ToplamGider = BirimGider * Gun;
        Odenecek = ToplamGider + GiderKdv20Tutari + GiderKdv10Tutari - GiderKesinti;
    }
}

/// <summary>
/// Excel Import Batch Kaydı
/// </summary>
public class PuantajExcelImport : BaseEntity
{
    public string DosyaAdi { get; set; } = string.Empty;
    public DateTime ImportTarihi { get; set; } = DateTime.UtcNow;
    public string? ImportEdenKullanici { get; set; }
    
    // İstatistikler
    public int ToplamSatir { get; set; }
    public int BasariliSatir { get; set; }
    public int HataliSatir { get; set; }
    public int OtoOlusturulanFirma { get; set; }
    public int OtoOlusturulanGuzergah { get; set; }
    public int OtoOlusturulanSofor { get; set; }
    
    // Dönem
    public int Yil { get; set; }
    public int Ay { get; set; }
    
    // Durum
    public ImportDurum Durum { get; set; } = ImportDurum.Bekliyor;
    public string? HataMesaji { get; set; }
    
    // İlişkili kayıtlar
    public virtual ICollection<PuantajKayit> Kayitlar { get; set; } = new List<PuantajKayit>();
}

/// <summary>
/// Import sırasında oluşturulan eşleştirme önerileri
/// </summary>
public class PuantajEslestirmeOneri : BaseEntity
{
    public int ExcelImportId { get; set; }
    public virtual PuantajExcelImport ExcelImport { get; set; } = null!;
    
    public EslestirmeTipi Tip { get; set; }
    public string ExcelDeger { get; set; } = string.Empty; // Excel'deki değer
    
    // Önerilen eşleştirmeler
    public int? OnerilenId { get; set; } // CariId, GuzergahId, SoforId, AracId
    public string? OnerilenAd { get; set; }
    public int BenzerlikPuani { get; set; } // 0-100
    
    public bool Onaylandi { get; set; } = false;
    public bool YeniOlusturulacak { get; set; } = false;
}

#region Enums

/// <summary>
/// Sefer slot türleri (günlük operasyonel zaman dilimi)
/// </summary>
public enum SeferSlot
{
    Sabah = 1,
    Aksam = 2,
    Mesai = 3,
    Diger1 = 4,
    Diger2 = 5,
    Diger3 = 6,
    Diger4 = 7,
    Diger5 = 8,
    /// <summary>Hem Sabah hem Akşam seferini tek kayıtta temsil eder (2 sefer sayılır).</summary>
    SabahAksam = 9
}

/// <summary>
/// Puantaj yön türleri
/// </summary>
public enum PuantajYon
{
    Sabah = 1,
    Aksam = 2,
    SabahAksam = 3,
    Diger = 4
}

/// <summary>
/// Şoför ödeme tipi
/// </summary>
public enum SoforOdemeTipi
{
    Ozmal = 1,        // Kendi şoförümüz, doğrudan ödeme
    Kiralik = 2,      // Kiralık araç ile gelen şoför, firmaya ödeme
    Komisyoncu = 3    // Komisyoncu üzerinden, komisyoncuya ödeme
}

/// <summary>
/// Puantaj ödeme durumu
/// </summary>
public enum PuantajOdemeDurum
{
    Odenmedi = 0,
    KismiOdendi = 1,
    Odendi = 2,
    Iptal = 3
}

/// <summary>
/// Puantaj onay durumu
/// </summary>
public enum PuantajOnayDurum
{
    Taslak = 0,
    OnayBekliyor = 1,
    Onaylandi = 2,
    Reddedildi = 3
}

/// <summary>
/// Puantaj kaydı kaynağı
/// </summary>
public enum PuantajKaynak
{
    Manuel = 0,
    ExcelImport = 1,
    ServisCalismaOtomatik = 2,
    Puantaj = 3
}

/// <summary>
/// Excel import durumu
/// </summary>
public enum ImportDurum
{
    Bekliyor = 0,
    Eslestiriliyor = 1,
    OnayBekliyor = 2,
    Isleniyor = 3,
    Tamamlandi = 4,
    Hata = 5
}

/// <summary>
/// Eşleştirme tipi
/// </summary>
public enum EslestirmeTipi
{
    Kurum = 1,
    Guzergah = 2,
    Sofor = 3,
    Arac = 4,
    FaturaKesici = 5
}

/// <summary>
/// Planlama finans yönü
/// </summary>
public enum PlanlamaFinansYonu
{
    Gelen = 1,      // Kurumdan bize gelen fatura
    Giden = 2,      // Bizden tedarikçiye giden fatura
    IcDagitim = 3   // 3 firma arası iç dağıtım/yansıtma
}

/// <summary>
/// Planlama kaynak tipi
/// </summary>
public enum PlanlamaKaynakTipi
{
    Kendi = 1,      // Özmal araç/şoför
    Tedarikci = 2   // Dış tedarikçi
}

#endregion


