using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Destek Talebi (Ticket) - osTicket benzeri
/// </summary>
public class DestekTalebi : BaseEntity
{
    /// <summary>
    /// Benzersiz talep numarası (örn: TKT-2025-000001)
    /// </summary>
    public string TalepNo { get; set; } = string.Empty;
    
    /// <summary>
    /// Talep konusu/başlığı
    /// </summary>
    public string Konu { get; set; } = string.Empty;
    
    /// <summary>
    /// Detaylı açıklama
    /// </summary>
    public string Aciklama { get; set; } = string.Empty;
    
    /// <summary>
    /// Talep durumu
    /// </summary>
    public DestekDurum Durum { get; set; } = DestekDurum.Yeni;
    
    /// <summary>
    /// Öncelik seviyesi
    /// </summary>
    public DestekOncelik Oncelik { get; set; } = DestekOncelik.Normal;
    
    /// <summary>
    /// Talep kaynağı
    /// </summary>
    public DestekKaynak Kaynak { get; set; } = DestekKaynak.Web;
    
    /// <summary>
    /// SLA süresi (saat cinsinden)
    /// </summary>
    public int? SlaSuresi { get; set; }
    
    /// <summary>
    /// SLA bitiş zamanı
    /// </summary>
    public DateTime? SlaBitisTarihi { get; set; }
    
    /// <summary>
    /// SLA aşıldı mı?
    /// </summary>
    public bool SlaAsildi { get; set; } = false;
    
    /// <summary>
    /// Son aktivite tarihi
    /// </summary>
    public DateTime SonAktiviteTarihi { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Kapatılma tarihi
    /// </summary>
    public DateTime? KapatilmaTarihi { get; set; }
    
    /// <summary>
    /// Çözüm süresi (dakika)
    /// </summary>
    public int? CozumSuresiDakika { get; set; }
    
    /// <summary>
    /// İlk yanıt süresi (dakika)
    /// </summary>
    public int? IlkYanitSuresiDakika { get; set; }
    
    /// <summary>
    /// Müşteri memnuniyet puanı (1-5)
    /// </summary>
    public int? MemnuniyetPuani { get; set; }
    
    /// <summary>
    /// Memnuniyet yorumu
    /// </summary>
    public string? MemnuniyetYorumu { get; set; }
    
    /// <summary>
    /// Dahili notlar (müşteriye görünmez)
    /// </summary>
    public string? DahiliNotlar { get; set; }
    
    /// <summary>
    /// Etiketler (virgülle ayrılmış)
    /// </summary>
    public string? Etiketler { get; set; }
    
    // === İlişkiler ===
    
    /// <summary>
    /// Departman ID
    /// </summary>
    public int DepartmanId { get; set; }
    
    /// <summary>
    /// Departman
    /// </summary>
    public virtual DestekDepartman Departman { get; set; } = null!;
    
    /// <summary>
    /// Kategori ID
    /// </summary>
    public int? KategoriId { get; set; }
    
    /// <summary>
    /// Kategori
    /// </summary>
    public virtual DestekKategori? Kategori { get; set; }
    
    /// <summary>
    /// Atanan kullanıcı ID
    /// </summary>
    public int? AtananKullaniciId { get; set; }
    
    /// <summary>
    /// Atanan kullanıcı
    /// </summary>
    public virtual Kullanici? AtananKullanici { get; set; }
    
    /// <summary>
    /// Oluşturan kullanıcı ID (null ise müşteri tarafından oluşturuldu)
    /// </summary>
    public int? OlusturanKullaniciId { get; set; }
    
    /// <summary>
    /// Oluşturan kullanıcı
    /// </summary>
    public virtual Kullanici? OlusturanKullanici { get; set; }
    
    // === Müşteri Bilgileri (Kayıtlı olmayan müşteriler için) ===
    
    /// <summary>
    /// İlişkili cari ID
    /// </summary>
    public int? CariId { get; set; }
    
    /// <summary>
    /// İlişkili cari
    /// </summary>
    public virtual Cari? Cari { get; set; }
    
    /// <summary>
    /// Müşteri adı
    /// </summary>
    public string MusteriAdi { get; set; } = string.Empty;
    
    /// <summary>
    /// Müşteri e-posta
    /// </summary>
    public string MusteriEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Müşteri telefon
    /// </summary>
    public string? MusteriTelefon { get; set; }
    
    // === Navigation Properties ===
    
    /// <summary>
    /// Yanıtlar
    /// </summary>
    public virtual ICollection<DestekTalebiYanit> Yanitlar { get; set; } = new List<DestekTalebiYanit>();
    
    /// <summary>
    /// Ekler
    /// </summary>
    public virtual ICollection<DestekTalebiEk> Ekler { get; set; } = new List<DestekTalebiEk>();
    
    /// <summary>
    /// Aktivite geçmişi
    /// </summary>
    public virtual ICollection<DestekTalebiAktivite> Aktiviteler { get; set; } = new List<DestekTalebiAktivite>();
    
    /// <summary>
    /// İlişkili talepler (bağlı ticketlar)
    /// </summary>
    public virtual ICollection<DestekTalebiIliski> IliskiliTalepler { get; set; } = new List<DestekTalebiIliski>();
    
    // === Hesaplanan Özellikler ===
    
    /// <summary>
    /// Yanıt sayısı
    /// </summary>
    [NotMapped]
    public int YanitSayisi => Yanitlar?.Count ?? 0;

    /// <summary>
    /// Açık mı?
    /// </summary>
    [NotMapped]
    public bool Acik => Durum != DestekDurum.Kapali && Durum != DestekDurum.Cozuldu;

    /// <summary>
    /// Bekleyen SLA zamanı
    /// </summary>
    [NotMapped]
    public TimeSpan? KalanSlaSuresi => SlaBitisTarihi.HasValue ? SlaBitisTarihi.Value - DateTime.UtcNow : null;
}

/// <summary>
/// Destek talebi yanıtı
/// </summary>
public class DestekTalebiYanit : BaseEntity
{
    /// <summary>
    /// Talep ID
    /// </summary>
    public int DestekTalebiId { get; set; }
    
    /// <summary>
    /// Talep
    /// </summary>
    public virtual DestekTalebi DestekTalebi { get; set; } = null!;
    
    /// <summary>
    /// Yanıt içeriği (HTML destekli)
    /// </summary>
    public string Icerik { get; set; } = string.Empty;
    
    /// <summary>
    /// Dahili not mu? (müşteriye görünmez)
    /// </summary>
    public bool DahiliNot { get; set; } = false;
    
    /// <summary>
    /// Yanıt türü
    /// </summary>
    public YanitTuru YanitTuru { get; set; } = YanitTuru.Yanit;
    
    /// <summary>
    /// Yanıtlayan kullanıcı ID (null ise müşteri)
    /// </summary>
    public int? KullaniciId { get; set; }
    
    /// <summary>
    /// Yanıtlayan kullanıcı
    /// </summary>
    public virtual Kullanici? Kullanici { get; set; }
    
    /// <summary>
    /// Müşteri tarafından mı gönderildi?
    /// </summary>
    public bool MusteriYaniti { get; set; } = false;
    
    /// <summary>
    /// Müşteri adı (müşteri yanıtı ise)
    /// </summary>
    public string? MusteriAdi { get; set; }
    
    /// <summary>
    /// Hazır yanıt kullanıldı mı?
    /// </summary>
    public int? HazirYanitId { get; set; }
    
    /// <summary>
    /// Ekler
    /// </summary>
    public virtual ICollection<DestekTalebiEk> Ekler { get; set; } = new List<DestekTalebiEk>();
}

/// <summary>
/// Destek talebi dosya eki
/// </summary>
public class DestekTalebiEk : BaseEntity
{
    /// <summary>
    /// Talep ID
    /// </summary>
    public int? DestekTalebiId { get; set; }
    
    /// <summary>
    /// Talep
    /// </summary>
    public virtual DestekTalebi? DestekTalebi { get; set; }
    
    /// <summary>
    /// Yanıt ID
    /// </summary>
    public int? YanitId { get; set; }
    
    /// <summary>
    /// Yanıt
    /// </summary>
    public virtual DestekTalebiYanit? Yanit { get; set; }
    
    /// <summary>
    /// Dosya adı
    /// </summary>
    public string DosyaAdi { get; set; } = string.Empty;
    
    /// <summary>
    /// Orijinal dosya adı
    /// </summary>
    public string OrijinalDosyaAdi { get; set; } = string.Empty;
    
    /// <summary>
    /// Dosya yolu
    /// </summary>
    public string DosyaYolu { get; set; } = string.Empty;
    
    /// <summary>
    /// Dosya boyutu (byte)
    /// </summary>
    public long DosyaBoyutu { get; set; }
    
    /// <summary>
    /// MIME tipi
    /// </summary>
    public string MimeTipi { get; set; } = string.Empty;
    
    /// <summary>
    /// Yükleyen kullanıcı ID
    /// </summary>
    public int? YukleyenKullaniciId { get; set; }
    
    /// <summary>
    /// Yükleyen kullanıcı
    /// </summary>
    public virtual Kullanici? YukleyenKullanici { get; set; }
}

/// <summary>
/// Destek talebi aktivite kaydı
/// </summary>
public class DestekTalebiAktivite : BaseEntity
{
    /// <summary>
    /// Talep ID
    /// </summary>
    public int DestekTalebiId { get; set; }
    
    /// <summary>
    /// Talep
    /// </summary>
    public virtual DestekTalebi DestekTalebi { get; set; } = null!;
    
    /// <summary>
    /// Aktivite türü
    /// </summary>
    public AktiviteTuru AktiviteTuru { get; set; }
    
    /// <summary>
    /// Aktivite açıklaması
    /// </summary>
    public string Aciklama { get; set; } = string.Empty;
    
    /// <summary>
    /// Eski değer
    /// </summary>
    public string? EskiDeger { get; set; }
    
    /// <summary>
    /// Yeni değer
    /// </summary>
    public string? YeniDeger { get; set; }
    
    /// <summary>
    /// Kullanıcı ID
    /// </summary>
    public int? KullaniciId { get; set; }
    
    /// <summary>
    /// Kullanıcı
    /// </summary>
    public virtual Kullanici? Kullanici { get; set; }
}

/// <summary>
/// Destek talebi ilişkileri (bağlı ticketlar)
/// </summary>
public class DestekTalebiIliski : BaseEntity
{
    /// <summary>
    /// Ana talep ID
    /// </summary>
    public int AnaTalepId { get; set; }
    
    /// <summary>
    /// Ana talep
    /// </summary>
    public virtual DestekTalebi AnaTalep { get; set; } = null!;
    
    /// <summary>
    /// İlişkili talep ID
    /// </summary>
    public int IliskiliTalepId { get; set; }
    
    /// <summary>
    /// İlişkili talep
    /// </summary>
    public virtual DestekTalebi IliskiliTalep { get; set; } = null!;
    
    /// <summary>
    /// İlişki türü
    /// </summary>
    public IliskiTuru IliskiTuru { get; set; }
}

/// <summary>
/// Destek departmanı
/// </summary>
public class DestekDepartman : BaseEntity
{
    /// <summary>
    /// Departman adı
    /// </summary>
    public string Ad { get; set; } = string.Empty;
    
    /// <summary>
    /// Departman açıklaması
    /// </summary>
    public string? Aciklama { get; set; }
    
    /// <summary>
    /// E-posta adresi
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Otomatik atama yapılsın mı?
    /// </summary>
    public bool OtomatikAtama { get; set; } = false;
    
    /// <summary>
    /// Varsayılan SLA süresi (saat)
    /// </summary>
    public int? VarsayilanSlaSuresi { get; set; }
    
    /// <summary>
    /// Sıra numarası
    /// </summary>
    public int SiraNo { get; set; } = 0;
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Üst departman ID
    /// </summary>
    public int? UstDepartmanId { get; set; }
    
    /// <summary>
    /// Üst departman
    /// </summary>
    public virtual DestekDepartman? UstDepartman { get; set; }
    
    /// <summary>
    /// Alt departmanlar
    /// </summary>
    public virtual ICollection<DestekDepartman> AltDepartmanlar { get; set; } = new List<DestekDepartman>();
    
    /// <summary>
    /// Kategoriler
    /// </summary>
    public virtual ICollection<DestekKategori> Kategoriler { get; set; } = new List<DestekKategori>();
    
    /// <summary>
    /// Talepler
    /// </summary>
    public virtual ICollection<DestekTalebi> Talepler { get; set; } = new List<DestekTalebi>();
    
    /// <summary>
    /// Departman üyeleri
    /// </summary>
    public virtual ICollection<DestekDepartmanUye> Uyeler { get; set; } = new List<DestekDepartmanUye>();
}

/// <summary>
/// Departman üyeleri
/// </summary>
public class DestekDepartmanUye : BaseEntity
{
    /// <summary>
    /// Departman ID
    /// </summary>
    public int DepartmanId { get; set; }
    
    /// <summary>
    /// Departman
    /// </summary>
    public virtual DestekDepartman Departman { get; set; } = null!;
    
    /// <summary>
    /// Kullanıcı ID
    /// </summary>
    public int KullaniciId { get; set; }
    
    /// <summary>
    /// Kullanıcı
    /// </summary>
    public virtual Kullanici Kullanici { get; set; } = null!;
    
    /// <summary>
    /// Departman yöneticisi mi?
    /// </summary>
    public bool Yonetici { get; set; } = false;
    
    /// <summary>
    /// Otomatik atama için uygun mu?
    /// </summary>
    public bool OtomatikAtamaUygun { get; set; } = true;
}

/// <summary>
/// Destek kategorisi
/// </summary>
public class DestekKategori : BaseEntity
{
    /// <summary>
    /// Kategori adı
    /// </summary>
    public string Ad { get; set; } = string.Empty;
    
    /// <summary>
    /// Kategori açıklaması
    /// </summary>
    public string? Aciklama { get; set; }
    
    /// <summary>
    /// Renk kodu
    /// </summary>
    public string? Renk { get; set; }
    
    /// <summary>
    /// Simge
    /// </summary>
    public string? Simge { get; set; }
    
    /// <summary>
    /// Sıra numarası
    /// </summary>
    public int SiraNo { get; set; } = 0;
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Departman ID
    /// </summary>
    public int? DepartmanId { get; set; }
    
    /// <summary>
    /// Departman
    /// </summary>
    public virtual DestekDepartman? Departman { get; set; }
    
    /// <summary>
    /// Üst kategori ID
    /// </summary>
    public int? UstKategoriId { get; set; }
    
    /// <summary>
    /// Üst kategori
    /// </summary>
    public virtual DestekKategori? UstKategori { get; set; }
    
    /// <summary>
    /// Alt kategoriler
    /// </summary>
    public virtual ICollection<DestekKategori> AltKategoriler { get; set; } = new List<DestekKategori>();
    
    /// <summary>
    /// Talepler
    /// </summary>
    public virtual ICollection<DestekTalebi> Talepler { get; set; } = new List<DestekTalebi>();
}

/// <summary>
/// Hazır yanıt şablonları
/// </summary>
public class DestekHazirYanit : BaseEntity
{
    /// <summary>
    /// Şablon adı
    /// </summary>
    public string Ad { get; set; } = string.Empty;
    
    /// <summary>
    /// Şablon içeriği (HTML destekli)
    /// </summary>
    public string Icerik { get; set; } = string.Empty;
    
    /// <summary>
    /// Konu şablonu
    /// </summary>
    public string? KonuSablonu { get; set; }
    
    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Aciklama { get; set; }
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Sıra numarası
    /// </summary>
    public int SiraNo { get; set; } = 0;
    
    /// <summary>
    /// Departman ID (null ise tüm departmanlar)
    /// </summary>
    public int? DepartmanId { get; set; }
    
    /// <summary>
    /// Departman
    /// </summary>
    public virtual DestekDepartman? Departman { get; set; }
    
    /// <summary>
    /// Kategori ID
    /// </summary>
    public int? KategoriId { get; set; }
    
    /// <summary>
    /// Kategori
    /// </summary>
    public virtual DestekKategori? Kategori { get; set; }
    
    /// <summary>
    /// Kullanım sayısı
    /// </summary>
    public int KullanimSayisi { get; set; } = 0;
}

/// <summary>
/// Bilgi bankası makalesi
/// </summary>
public class DestekBilgiBankasi : BaseEntity
{
    /// <summary>
    /// Makale başlığı
    /// </summary>
    public string Baslik { get; set; } = string.Empty;
    
    /// <summary>
    /// Makale içeriği (HTML)
    /// </summary>
    public string Icerik { get; set; } = string.Empty;
    
    /// <summary>
    /// Özet
    /// </summary>
    public string? Ozet { get; set; }
    
    /// <summary>
    /// Etiketler (virgülle ayrılmış)
    /// </summary>
    public string? Etiketler { get; set; }
    
    /// <summary>
    /// SEO başlık
    /// </summary>
    public string? SeoBaslik { get; set; }
    
    /// <summary>
    /// SEO açıklama
    /// </summary>
    public string? SeoAciklama { get; set; }
    
    /// <summary>
    /// URL slug
    /// </summary>
    public string? Slug { get; set; }
    
    /// <summary>
    /// Görüntülenme sayısı
    /// </summary>
    public int GoruntulemeSayisi { get; set; } = 0;
    
    /// <summary>
    /// Yararlı bulma sayısı
    /// </summary>
    public int YararliBulmaSayisi { get; set; } = 0;
    
    /// <summary>
    /// Yararsız bulma sayısı
    /// </summary>
    public int YararsizBulmaSayisi { get; set; } = 0;
    
    /// <summary>
    /// Durum
    /// </summary>
    public BilgiBankasiDurum Durum { get; set; } = BilgiBankasiDurum.Taslak;
    
    /// <summary>
    /// Yayınlanma tarihi
    /// </summary>
    public DateTime? YayinlanmaTarihi { get; set; }
    
    /// <summary>
    /// Kategori ID
    /// </summary>
    public int? KategoriId { get; set; }
    
    /// <summary>
    /// Kategori
    /// </summary>
    public virtual DestekKategori? Kategori { get; set; }
    
    /// <summary>
    /// Yazar kullanıcı ID
    /// </summary>
    public int? YazarKullaniciId { get; set; }
    
    /// <summary>
    /// Yazar
    /// </summary>
    public virtual Kullanici? Yazar { get; set; }
}

/// <summary>
/// SLA tanımları
/// </summary>
public class DestekSla : BaseEntity
{
    /// <summary>
    /// SLA adı
    /// </summary>
    public string Ad { get; set; } = string.Empty;
    
    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Aciklama { get; set; }
    
    /// <summary>
    /// İlk yanıt süresi (saat)
    /// </summary>
    public int IlkYanitSuresi { get; set; }
    
    /// <summary>
    /// Çözüm süresi (saat)
    /// </summary>
    public int CozumSuresi { get; set; }
    
    /// <summary>
    /// Öncelik
    /// </summary>
    public DestekOncelik Oncelik { get; set; }
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Mesai saatlerinde mi hesaplansın?
    /// </summary>
    public bool SadeceMesaiSaatleri { get; set; } = true;
    
    /// <summary>
    /// Hafta içi mi hesaplansın?
    /// </summary>
    public bool SadeceHaftaIci { get; set; } = true;
}

/// <summary>
/// Destek sistemi ayarları
/// </summary>
public class DestekAyar : BaseEntity
{
    /// <summary>
    /// Ayar anahtarı
    /// </summary>
    public string Anahtar { get; set; } = string.Empty;
    
    /// <summary>
    /// Ayar değeri
    /// </summary>
    public string Deger { get; set; } = string.Empty;
    
    /// <summary>
    /// Ayar açıklaması
    /// </summary>
    public string? Aciklama { get; set; }
    
    /// <summary>
    /// Ayar grubu
    /// </summary>
    public string? Grup { get; set; }
}

// === ENUM'lar ===

/// <summary>
/// Destek talebi durumu
/// </summary>
public enum DestekDurum
{
    Taslak = 0,
    Yeni = 1,
    Acik = 2,
    Beklemede = 3,
    YanitBekleniyor = 4,
    Cozuldu = 5,
    Kapali = 6,
    /// <summary>
    /// Gönderildi - Atama bekliyor (osTicket benzeri)
    /// </summary>
    Gonderildi = 7,
    /// <summary>
    /// İşlemde - Yetkili üzerinde çalışıyor
    /// </summary>
    Islemde = 8,
    /// <summary>
    /// Bitti - Tamamlandı, onay bekliyor
    /// </summary>
    Bitti = 9,
    /// <summary>
    /// Onaylandi - Kullanıcı tarafından onaylandı
    /// </summary>
    Onaylandi = 10
}

/// <summary>
/// Öncelik seviyesi
/// </summary>
public enum DestekOncelik
{
    Dusuk = 1,
    Normal = 2,
    Yuksek = 3,
    Acil = 4,
    Kritik = 5
}

/// <summary>
/// Talep kaynağı
/// </summary>
public enum DestekKaynak
{
    Web = 1,
    Email = 2,
    Telefon = 3,
    Api = 4,
    WhatsApp = 5,
    MobilUygulama = 6
}

/// <summary>
/// Yanıt türü
/// </summary>
public enum YanitTuru
{
    Yanit = 1,
    DahiliNot = 2,
    OtomatikYanit = 3,
    SistemMesaji = 4
}

/// <summary>
/// Aktivite türü
/// </summary>
public enum AktiviteTuru
{
    Olusturuldu = 1,
    Guncellendi = 2,
    DurumDegisti = 3,
    OncelikDegisti = 4,
    Atandi = 5,
    Transferedildi = 6,
    YanitEklendi = 7,
    DosyaEklendi = 8,
    Kapandi = 9,
    YenidenAcildi = 10,
    SlaBilgilendirme = 11,
    Birlestirildi = 12
}

/// <summary>
/// İlişki türü
/// </summary>
public enum IliskiTuru
{
    Bagli = 1,
    Cift = 2,
    AnaAlt = 3,
    Birlestirildi = 4
}

/// <summary>
/// Bilgi bankası durumu
/// </summary>
public enum BilgiBankasiDurum
{
    Taslak = 1,
    IncelemeBekliyor = 2,
    Yayinda = 3,
    Arsiv = 4
}


