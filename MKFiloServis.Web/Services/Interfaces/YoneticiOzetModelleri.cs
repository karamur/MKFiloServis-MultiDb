using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>Yönetici özeti raporunun tamamı. Kapsam dışı (yetkisiz/lisanssız) bölümler null'dır.</summary>
public sealed class YoneticiOzetRapor
{
    public required YoneticiOzetDonemi Donem { get; init; }
    public DateTime UretimZamani { get; init; }

    /// <summary>Aktif firma adı veya "Tüm Firmalar".</summary>
    public string KapsamAdi { get; init; } = string.Empty;

    public ServisOzeti? Servis { get; init; }
    public HakedisOzeti? Hakedis { get; init; }
    public FaturaOzeti? Fatura { get; init; }
    public KasaOzeti? Kasa { get; init; }
    public ButceOzeti? Butce { get; init; }
    public PersonelOzeti? Personel { get; init; }
    public AracOzeti? Arac { get; init; }
    public KayitOzeti? Kayitlar { get; init; }
    public RentACarOzeti? RentACar { get; init; }
    public EkipOzeti? Ekip { get; init; }

    /// <summary>Dönemden bağımsız, şu an ilgilenilmesi gereken kalemler (en kritik önce).</summary>
    public IReadOnlyList<DikkatKalemi> Dikkat { get; init; } = Array.Empty<DikkatKalemi>();

    /// <summary>Yüklenemeyen bölümlerin adları (kullanıcıya ham hata gösterilmez).</summary>
    public IReadOnlyList<string> Hatalar { get; init; } = Array.Empty<string>();
}

/// <summary>Günlük servis puantajı (sefer) özeti. Kaynak: <c>FiloGunlukPuantaj</c>.</summary>
public sealed class ServisOzeti
{
    /// <summary>Bugüne kadar yapılan sefer (gitmedi/iptal hariç; hakedişin saydığı kayıtlarla aynı).</summary>
    public decimal YapilanSefer { get; init; }
    public decimal OncekiYapilanSefer { get; init; }
    public int CalisanAracSayisi { get; init; }
    public int CalisilanGunSayisi { get; init; }

    /// <summary>Mazeretli/mazeretsiz gitmedi veya kurum tarafından iptal edilen kayıtlar.</summary>
    public int YapilamayanKayit { get; init; }
    public int ArizaKaydi { get; init; }

    /// <summary>Dönemin bugünden sonraki günleri için planlı sefer.</summary>
    public decimal PlanliSefer { get; init; }

    public decimal KurumTahakkuku { get; init; }
    public decimal OncekiKurumTahakkuku { get; init; }
    public decimal TaseronTahakkuku { get; init; }
    public decimal BrutFark => KurumTahakkuku - TaseronTahakkuku;

    /// <summary>Onay oranının paydası: bugüne kadar yapılan sefer kayıtları.</summary>
    public int KayitSayisi { get; init; }
    public int OnayliKayitSayisi { get; init; }
    public int OnayYuzdesi => KayitSayisi > 0 ? (int)Math.Round(100m * OnayliKayitSayisi / KayitSayisi) : 0;

    /// <summary>Deger1 = yapılan sefer, Deger2 = planlı sefer.</summary>
    public ZamanSerisi Seri { get; init; } = ZamanSerisi.Bos;

    public bool Bos => YapilanSefer == 0 && PlanliSefer == 0 && YapilamayanKayit == 0 && ArizaKaydi == 0;
}

/// <summary>Operasyonel hakediş üretimi. Kaynak: <c>Hakedis</c> (iptaller hariç).</summary>
public sealed class HakedisOzeti
{
    /// <summary>Dönemde üretilen (oluşturulma tarihi dönemde olan) hakediş sayısı.</summary>
    public int UretilenAdet { get; init; }
    public int OncekiUretilenAdet { get; init; }

    public int GelirAdet { get; init; }
    public decimal GelirTutari { get; init; }
    public decimal OncekiGelirTutari { get; init; }

    public int GiderAdet { get; init; }
    public decimal GiderTutari { get; init; }

    /// <summary>Araç bazlı iç maliyet hakedişleri (faturalanmaz).</summary>
    public int IcAdet { get; init; }
    public decimal IcTutar { get; init; }

    public decimal NetTutar => GelirTutari - GiderTutari;

    /// <summary>Onay tarihi dönemde olan hakediş sayısı.</summary>
    public int OnaylananAdet { get; init; }

    // Dönemde üretilenlerin şu anki durum dağılımı
    public int TaslakAdet { get; init; }
    public int OnayliAdet { get; init; }
    public int FaturaliAdet { get; init; }
    public int TamamlananAdet { get; init; }

    public bool Bos => UretilenAdet == 0 && OnaylananAdet == 0;
}

/// <summary>Kesilen ve alınan faturalar (iptaller hariç, iade faturaları ayrı sayılır).</summary>
public sealed class FaturaOzeti
{
    public int KesilenAdet { get; init; }
    public decimal KesilenTutar { get; init; }
    public decimal KesilenTahsilEdilen { get; init; }
    public int OncekiKesilenAdet { get; init; }
    public decimal OncekiKesilenTutar { get; init; }

    public int AlinanAdet { get; init; }
    public decimal AlinanTutar { get; init; }
    public decimal AlinanOdenen { get; init; }

    public int IadeAdet { get; init; }

    public int TahsilatYuzdesi => KesilenTutar > 0
        ? (int)Math.Clamp(Math.Round(100m * KesilenTahsilEdilen / KesilenTutar), 0, 100)
        : 0;

    public int OdemeYuzdesi => AlinanTutar > 0
        ? (int)Math.Clamp(Math.Round(100m * AlinanOdenen / AlinanTutar), 0, 100)
        : 0;

    public bool Bos => KesilenAdet == 0 && AlinanAdet == 0 && IadeAdet == 0;
}

/// <summary>Banka/kasa hareketleri: hesaplar arası aktarım, cari mahsup ve personel cebinden harcama hariç.</summary>
public sealed class KasaOzeti
{
    public int TahsilatAdet { get; init; }
    public decimal TahsilatTutari { get; init; }
    public decimal OncekiTahsilat { get; init; }

    public int OdemeAdet { get; init; }
    public decimal OdemeTutari { get; init; }
    public decimal OncekiOdeme { get; init; }

    public decimal NetNakit => TahsilatTutari - OdemeTutari;
    public decimal OncekiNetNakit => OncekiTahsilat - OncekiOdeme;

    /// <summary>Deger1 = tahsilat, Deger2 = ödeme.</summary>
    public ZamanSerisi Seri { get; init; } = ZamanSerisi.Bos;

    public bool Bos => TahsilatAdet == 0 && OdemeAdet == 0;
}

/// <summary>Bütçe ödemeleri (ödeme tarihi dönemde olanlar; ay sonuna kadar planlananlar dahil).</summary>
public sealed class ButceOzeti
{
    public decimal Planlanan { get; init; }
    public decimal Odenen { get; init; }
    public decimal Bekleyen { get; init; }
    public int KalemSayisi { get; init; }
    public int OdenenKalem { get; init; }
    public int BekleyenKalem { get; init; }

    public int GerceklesmeYuzdesi => Planlanan > 0
        ? (int)Math.Clamp(Math.Round(100m * Odenen / Planlanan), 0, 999)
        : 0;

    /// <summary>"%35"; ödenen tutar var ama yüzde 0'a yuvarlanıyorsa "%1'den az" (ör. 1.000 / 1.148.233 TL).</summary>
    public string GerceklesmeMetni => GerceklesmeYuzdesi == 0 && Odenen > 0 ? "%1'den az" : $"%{GerceklesmeYuzdesi}";

    /// <summary>Kalem yok ya da hepsi 0 TL (ör. tutarsız "Kredi Kartı" satırları) ise gösterecek bir şey yoktur.</summary>
    public bool Bos => KalemSayisi == 0 || (Planlanan == 0 && Odenen == 0 && Bekleyen == 0);
}

/// <summary>Personel puantajı, maaş dönem kaydı ve avanslar.</summary>
public sealed class PersonelOzeti
{
    public int AktifPersonel { get; init; }

    public int PuantajKaydi { get; init; }
    public int PuantajOnayli { get; init; }
    public int PuantajOnayBekleyen { get; init; }
    public int PuantajTaslak { get; init; }
    public int PuantajReddedilen { get; init; }

    /// <summary>Maaş dönem kaydı (snapshot) alınan personel sayısı.</summary>
    public int MaasKaydiPersonel { get; init; }
    public decimal MaasOdenecek { get; init; }

    public int AvansAdet { get; init; }
    public decimal AvansTutar { get; init; }

    public bool Bos => PuantajKaydi == 0 && MaasKaydiPersonel == 0 && AvansAdet == 0;
}

/// <summary>Araç masrafları ve filo hareketi.</summary>
public sealed class AracOzeti
{
    public int AktifArac { get; init; }
    public int YeniArac { get; init; }
    public int MasrafAdet { get; init; }
    public decimal MasrafTutari { get; init; }
    public decimal OncekiMasrafTutari { get; init; }

    public bool Bos => YeniArac == 0 && MasrafAdet == 0;
}

/// <summary>Yeni kayıtlar: yalnızca ilgili modül yetkisi olan satırlar dolu (null = gösterme).</summary>
public sealed class KayitOzeti
{
    public int? YeniCari { get; init; }
    public int? YeniPersonel { get; init; }
    public int? GelenEvrak { get; init; }
    public int? GidenEvrak { get; init; }

    public bool Bos => (YeniCari ?? 0) == 0 && (YeniPersonel ?? 0) == 0 && (GelenEvrak ?? 0) == 0 && (GidenEvrak ?? 0) == 0;
}

public sealed class RentACarOzeti
{
    public int YeniKiralama { get; init; }
    public int TamamlananKiralama { get; init; }
    public decimal TamamlananCiro { get; init; }
    public int AktifKiralama { get; init; }

    public bool Bos => YeniKiralama == 0 && TamamlananKiralama == 0 && AktifKiralama == 0;
}

public enum DikkatSeviyesi
{
    Bilgi = 1,
    Uyari = 2,
    Kritik = 3
}

/// <summary>"Dikkat gerektirenler" listesindeki tek satır.</summary>
public sealed class DikkatKalemi
{
    public string Anahtar { get; init; } = string.Empty;
    public string Baslik { get; init; } = string.Empty;
    public string? Aciklama { get; init; }
    public int Adet { get; init; }
    public decimal? Tutar { get; init; }
    public DikkatSeviyesi Seviye { get; init; } = DikkatSeviyesi.Uyari;
    public string Ikon { get; init; } = "bi-exclamation-circle";
    public string Link { get; init; } = "/";
}

/// <summary>Kullanıcıların sistemde yaptığı kayıt işlemleri (aktivite günlüğünden; yalnızca ana kayıt türleri).</summary>
public sealed class EkipOzeti
{
    public int ToplamIslem { get; init; }
    public int AktifKullanici { get; init; }
    public IReadOnlyList<EkipKullanici> Kullanicilar { get; init; } = Array.Empty<EkipKullanici>();
    public IReadOnlyList<EkipIsTuru> IsTurleri { get; init; } = Array.Empty<EkipIsTuru>();
    public IReadOnlyList<EkipIslem> SonIslemler { get; init; } = Array.Empty<EkipIslem>();

    public bool Bos => ToplamIslem == 0;
}

public sealed class EkipKullanici
{
    public string KullaniciAdi { get; init; } = string.Empty;
    public int Toplam { get; init; }
    public int Eklenen { get; init; }
    public int Guncellenen { get; init; }
    public int Silinen { get; init; }

    /// <summary>İş günü saatiyle son işlem zamanı.</summary>
    public DateTime SonIslem { get; init; }
}

public sealed class EkipIsTuru
{
    public string Etiket { get; init; } = string.Empty;
    public int Adet { get; init; }
}

public enum EkipIslemTuru
{
    Eklendi = 1,
    Guncellendi = 2,
    Silindi = 3
}

public sealed class EkipIslem
{
    /// <summary>İş günü saatiyle işlem zamanı.</summary>
    public DateTime Zaman { get; init; }
    public string Kullanici { get; init; } = string.Empty;
    public string Etiket { get; init; } = string.Empty;
    public EkipIslemTuru Tur { get; init; }
    public string? Ayrinti { get; init; }
}

/// <summary>Grafik için zaman serisi: günlük veya aylık kovalar, iki karşılaştırmalı değer.</summary>
public sealed class ZamanSerisi
{
    public static ZamanSerisi Bos { get; } = new();

    public IReadOnlyList<SeriNoktasi> Noktalar { get; init; } = Array.Empty<SeriNoktasi>();
    public bool Aylik { get; init; }
    public string Etiket1 { get; init; } = string.Empty;
    public string Etiket2 { get; init; } = string.Empty;

    public decimal Azami => Noktalar.Count == 0 ? 0 : Noktalar.Max(n => Math.Max(n.Deger1, n.Deger2));
    public bool VeriVar => Noktalar.Any(n => n.Deger1 != 0 || n.Deger2 != 0);
}

public sealed record SeriNoktasi(DateTime Baslangic, string Etiket, string Aciklama, decimal Deger1, decimal Deger2, bool Gelecek);
