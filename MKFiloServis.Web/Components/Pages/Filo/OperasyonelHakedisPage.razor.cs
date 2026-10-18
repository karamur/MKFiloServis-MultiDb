using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Components.Shared;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Components.Pages.Filo;

public partial class OperasyonelHakedisPage
{
    private enum Sekme { Hazirla, Hakedisler }

    private sealed record TurBilgisi(HakedisTipi Tip, string Baslik, string Aciklama, string Ikon);

    private static readonly TurBilgisi[] TurSecenekleri =
    {
        new(HakedisTipi.Kurum, "Kurum hakedişi", "Müşteri kuruma kesilecek gelir faturası", "bi-building"),
        new(HakedisTipi.Tedarikci, "Tedarikçi hakedişi", "Tedarikçi araçları için ödenecek gider", "bi-truck"),
        new(HakedisTipi.AracSahibi, "Araç sahibi hakedişi", "Komisyoncu ve kiralık plaka sahibine ödenecek gider", "bi-person-badge"),
        new(HakedisTipi.Arac, "Araç hakedişi", "Araç bazında eşleştirme ücretiyle tutar (faturalanmaz)", "bi-car-front")
    };

    private static readonly string[] AyAdlari =
    {
        "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    };

    private static readonly HakedisGruplama[] GruplamaSecenekleri =
    {
        HakedisGruplama.Cari, HakedisGruplama.AracSahibi, HakedisGruplama.Guzergah,
        HakedisGruplama.Arac, HakedisGruplama.Sofor, HakedisGruplama.Eslestirme, HakedisGruplama.Yok
    };

    private Sekme aktifSekme = Sekme.Hazirla;
    private bool isBusy;
    private bool onizlemeYukleniyor;
    private int onizlemeSurumu;
    private string? hataMesaji;
    private string? bilgiMesaji;
    private TopluHakedisSonuc? sonUretimSonucu;

    private int yil = BusinessTime.Today.Year;
    private int ay = BusinessTime.Today.Month;
    private HakedisTipi seciliTip = HakedisTipi.Kurum;

    // ── Kapsam (filtre) seçimleri ──────────────────────────────────────────
    private readonly HashSet<string> secSahip = new(StringComparer.Ordinal);
    private readonly HashSet<string> secGuzergah = new(StringComparer.Ordinal);
    private readonly HashSet<string> secEslestirme = new(StringComparer.Ordinal);
    private readonly HashSet<string> secCari = new(StringComparer.Ordinal);
    private readonly HashSet<string> secArac = new(StringComparer.Ordinal);
    private readonly HashSet<string> secSofor = new(StringComparer.Ordinal);
    private readonly HashSet<string> secServis = new(StringComparer.Ordinal);
    private readonly HashSet<AracSahiplikTipi> secSahiplik = new();
    private DateTime? gunBaslangic;
    private DateTime? gunBitis;

    // Hesaplama biçimi (filtre değil): müşteri kurum eşleştirmeden mi alınsın, araç hakedişinin birim fiyatı nereden gelsin.
    private HakedisMusteriKaynagi musteriKaynagi = HakedisMusteriKaynagi.Eslestirme;
    private HakedisBirimFiyatKaynagi birimFiyatKaynagi = HakedisBirimFiyatKaynagi.EslestirmeOdenen;

    // ── Önizleme görünümü ──────────────────────────────────────────────────
    private HakedisKapsamOnizleme? onizleme;
    private HakedisGruplama gruplama = HakedisGruplama.Cari;
    private bool karlilikKolonlari;
    private bool hakedistekileriGoster;
    private bool onizlemeTumuAcik = true;
    private readonly HashSet<string> tersOnizlemeGruplari = new(StringComparer.Ordinal);

    // ── Oluşturulan hakedişler ─────────────────────────────────────────────
    private string _selectedGrouping = "cari";
    private string selectedGrouping
    {
        get => _selectedGrouping;
        set
        {
            if (_selectedGrouping != value)
            {
                _selectedGrouping = value;
                tersListeGruplari.Clear();
            }
        }
    }

    private bool listeTumuAcik = true;
    private readonly HashSet<string> tersListeGruplari = new(StringComparer.Ordinal);
    private bool mutabakatAcik;

    private readonly Dictionary<int, string> kurumAdlari = new();
    private readonly Dictionary<int, string> tedarikciAdlari = new();
    private readonly Dictionary<int, string> aracAdlari = new();
    private readonly Dictionary<int, string> aracSahibiAdlari = new();
    private readonly Dictionary<int, string> sahipCariAdlari = new();
    private readonly Dictionary<int, Cari> cariDetaylari = new();
    private readonly HashSet<int> seciliHakedisIds = new();
    private readonly HashSet<int> acikHakedisDetaylari = new();

    private List<Hakedis> hakedisler = new();
    private PuantajDonemOzetDto? puantajDonemOzeti;

    private string durumFiltre = "";
    private string aramaMetni = "";

    private int EtkinFirmaId => AktifFirmaProvider.AktifFirmaId ?? AktifFirmaProvider.Mevcut.FirmaId;
    private bool DonemGecerli => yil is >= 2000 and <= 2100 && ay is >= 1 and <= 12;

    // ── Yaşam döngüsü ──────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        if (!AktifFirmaProvider.HasAktifFirma)
        {
            Navigation.NavigateTo("/firma-sec?returnUrl=%2Foperasyonel-hakedis", forceLoad: false);
            return;
        }

        await TumunuYukleAsync();

        // Dönemde zaten hakediş varsa kullanıcı çoğunlukla onay / fatura için gelir; listeyi açarak başla.
        if (hakedisler.Count > 0)
            aktifSekme = Sekme.Hakedisler;
    }

    // ── Dönem ve tür ───────────────────────────────────────────────────────

    private async Task AyGitAsync(int fark)
    {
        if (!DonemGecerli)
            return;

        var hedef = new DateTime(yil, ay, 1).AddMonths(fark);
        yil = hedef.Year;
        ay = hedef.Month;
        await DonemDegistiAsync();
    }

    private async Task BuAyaGitAsync()
    {
        yil = BusinessTime.Today.Year;
        ay = BusinessTime.Today.Month;
        await DonemDegistiAsync();
    }

    private async Task DonemDegistiAsync()
    {
        gunBaslangic = null;
        gunBitis = null;
        seciliHakedisIds.Clear();
        acikHakedisDetaylari.Clear();
        sonUretimSonucu = null;
        bilgiMesaji = null;
        await TumunuYukleAsync();
    }

    private async Task TipSecAsync(HakedisTipi tip)
    {
        if (seciliTip == tip)
            return;

        seciliTip = tip;
        seciliHakedisIds.Clear();
        acikHakedisDetaylari.Clear();
        sonUretimSonucu = null;
        bilgiMesaji = null;
        await TumunuYukleAsync();
    }

    private async Task TumunuYukleAsync()
    {
        hataMesaji = null;

        if (!DonemGecerli)
        {
            hataMesaji = "Geçersiz dönem seçimi.";
            return;
        }

        isBusy = true;
        try
        {
            await YukleHakedisVeMutabakatAsync();
        }
        catch (Exception ex)
        {
            hataMesaji = ex.GetBaseException().Message;
        }
        finally
        {
            isBusy = false;
        }

        await OnizlemeYenileAsync();
    }

    // ── Kapsam / önizleme ──────────────────────────────────────────────────

    private HakedisKapsami KapsamOlustur()
    {
        static List<int> Idler(IEnumerable<string> anahtarlar) => anahtarlar
            .Select(a => int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        return new HakedisKapsami
        {
            Yil = yil,
            Ay = ay,
            BaslangicTarihi = gunBaslangic,
            BitisTarihi = gunBitis,
            CariIdleri = Idler(secCari),
            GuzergahIdleri = Idler(secGuzergah),
            EslestirmeIdleri = Idler(secEslestirme),
            AracIdleri = Idler(secArac),
            SoforIdleri = Idler(secSofor),
            SahiplikTipleri = secSahiplik.ToList(),
            SahipAnahtarlari = secSahip.ToList(),
            ServisTurleri = Idler(secServis).Select(i => (ServisTuru)i).ToList(),
            MusteriKaynagi = musteriKaynagi,
            BirimFiyatKaynagi = birimFiyatKaynagi
        };
    }

    private async Task MusteriKaynagiSecAsync(HakedisMusteriKaynagi kaynak)
    {
        if (musteriKaynagi == kaynak)
            return;

        musteriKaynagi = kaynak;
        // Cari seçenekleri kaynağa göre değişir; eski seçimler yeni listede yoksa temizlenir.
        await OnizlemeYenileAsync();
    }

    private async Task OnizlemeYenileAsync()
    {
        if (!DonemGecerli)
            return;

        var surum = ++onizlemeSurumu;
        onizlemeYukleniyor = true;

        try
        {
            HakedisKapsamOnizleme? sonuc = null;

            // Dönem / tür değişince artık listede olmayan seçimler temizlenir; bu durumda bir kez daha sorgulanır.
            for (var deneme = 0; deneme < 2; deneme++)
            {
                sonuc = await HakedisService.KapsamOnizleAsync(seciliTip, KapsamOlustur());
                if (surum != onizlemeSurumu)
                    return;

                if (!GecersizSecimleriTemizle(sonuc.Secenekler))
                    break;
            }

            onizleme = sonuc;
        }
        catch (Exception ex)
        {
            if (surum == onizlemeSurumu)
                hataMesaji = ex.GetBaseException().Message;
        }
        finally
        {
            if (surum == onizlemeSurumu)
                onizlemeYukleniyor = false;
        }
    }

    /// <summary>Seçili değerlerden yeni seçenek listesinde bulunmayanları kaldırır; bir şey değiştiyse true döner.</summary>
    private bool GecersizSecimleriTemizle(HakedisFiltreSecenekleri secenekler)
    {
        static bool Buda(HashSet<string> secili, IEnumerable<string> gecerli)
        {
            if (secili.Count == 0)
                return false;

            var kume = gecerli.ToHashSet(StringComparer.Ordinal);
            return secili.RemoveWhere(x => !kume.Contains(x)) > 0;
        }

        var degisti = false;
        degisti |= Buda(secSahip, secenekler.Sahipler.Select(s => s.Anahtar));
        degisti |= Buda(secGuzergah, secenekler.Guzergahlar.Select(s => s.Anahtar));
        degisti |= Buda(secEslestirme, secenekler.Eslestirmeler.Select(s => s.Anahtar));
        degisti |= Buda(secCari, secenekler.Cariler.Select(s => s.Anahtar));
        degisti |= Buda(secArac, secenekler.Araclar.Select(s => s.Anahtar));
        degisti |= Buda(secSofor, secenekler.Soforler.Select(s => s.Anahtar));
        degisti |= Buda(secServis, secenekler.ServisTurleri.Select(t => ((int)t).ToString(CultureInfo.InvariantCulture)));
        degisti |= secSahiplik.RemoveWhere(t => !secenekler.SahiplikTipleri.Contains(t)) > 0;
        return degisti;
    }

    private async Task FiltreDegistiAsync()
    {
        // Güzergâh seçenekleri seçili carilere göre daralır; artık görünmeyen güzergâh seçimi düşürülür.
        if (onizleme is not null && secGuzergah.Count > 0)
        {
            var gecerli = GuzergahSecenekleri.Select(g => g.Anahtar).ToHashSet(StringComparer.Ordinal);
            secGuzergah.RemoveWhere(x => !gecerli.Contains(x));
        }

        await OnizlemeYenileAsync();
    }

    private async Task SahiplikDegistirAsync(AracSahiplikTipi tip)
    {
        if (!secSahiplik.Add(tip))
            secSahiplik.Remove(tip);

        // Seçilen sahiplik tiplerine uymayan sahip seçimleri düşürülür.
        if (onizleme is not null && secSahip.Count > 0 && secSahiplik.Count > 0)
        {
            var gecerli = onizleme.Secenekler.Sahipler
                .Where(s => s.SahiplikTipi.HasValue && secSahiplik.Contains(s.SahiplikTipi.Value))
                .Select(s => s.Anahtar)
                .ToHashSet(StringComparer.Ordinal);
            secSahip.RemoveWhere(x => !gecerli.Contains(x));
        }

        await OnizlemeYenileAsync();
    }

    private async Task FiltreleriTemizleAsync()
    {
        secSahip.Clear();
        secGuzergah.Clear();
        secEslestirme.Clear();
        secCari.Clear();
        secArac.Clear();
        secSofor.Clear();
        secServis.Clear();
        secSahiplik.Clear();
        gunBaslangic = null;
        gunBitis = null;
        await OnizlemeYenileAsync();
    }

    private async Task GunDegistiAsync(ChangeEventArgs e, bool baslangic)
    {
        DateTime? deger = DateTime.TryParse(e.Value?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.Date
            : null;

        if (baslangic)
            gunBaslangic = deger;
        else
            gunBitis = deger;

        await OnizlemeYenileAsync();
    }

    private async Task HazirGunAraligiAsync(int? ilkGun, int? sonGun)
    {
        var gunSayisi = DateTime.DaysInMonth(yil, ay);
        gunBaslangic = ilkGun.HasValue ? new DateTime(yil, ay, Math.Min(ilkGun.Value, gunSayisi)) : null;
        gunBitis = sonGun.HasValue ? new DateTime(yil, ay, Math.Min(sonGun.Value, gunSayisi)) : null;
        await OnizlemeYenileAsync();
    }

    private IReadOnlyList<CokluSecim.CokluSecimOgesi> Ogeler(IEnumerable<HakedisSecenek> kaynak) =>
        kaynak.Select(s => new CokluSecim.CokluSecimOgesi(s.Anahtar, s.Ad, s.Alt)).ToList();

    private IEnumerable<HakedisSecenek> SahipSecenekleri => onizleme is null
        ? Enumerable.Empty<HakedisSecenek>()
        : onizleme.Secenekler.Sahipler.Where(s => secSahiplik.Count == 0
            || (s.SahiplikTipi.HasValue && secSahiplik.Contains(s.SahiplikTipi.Value)));

    private IEnumerable<HakedisSecenek> AracSecenekleri => onizleme is null
        ? Enumerable.Empty<HakedisSecenek>()
        : onizleme.Secenekler.Araclar.Where(a => secSahiplik.Count == 0
            || (a.SahiplikTipi.HasValue && secSahiplik.Contains(a.SahiplikTipi.Value)));

    private IEnumerable<HakedisSecenek> GuzergahSecenekleri => onizleme is null
        ? Enumerable.Empty<HakedisSecenek>()
        : onizleme.Secenekler.Guzergahlar.Where(g => secCari.Count == 0
            || (g.UstAnahtar is not null && secCari.Contains(g.UstAnahtar)));

    private IEnumerable<HakedisSecenek> CariSecenekleri => onizleme?.Secenekler.Cariler ?? new List<HakedisSecenek>();
    private IEnumerable<HakedisSecenek> EslestirmeSecenekleri => onizleme?.Secenekler.Eslestirmeler ?? new List<HakedisSecenek>();
    private IEnumerable<HakedisSecenek> SoforSecenekleri => onizleme?.Secenekler.Soforler ?? new List<HakedisSecenek>();

    private IEnumerable<AracSahiplikTipi> SahiplikSecenekleri => onizleme?.Secenekler.SahiplikTipleri ?? new List<AracSahiplikTipi>();

    private IReadOnlyList<CokluSecim.CokluSecimOgesi> ServisOgeleri => (onizleme?.Secenekler.ServisTurleri ?? new List<ServisTuru>())
        .Select(t => new CokluSecim.CokluSecimOgesi(((int)t).ToString(CultureInfo.InvariantCulture), HakedisKapsamAciklayici.ServisTuruMetni(t)))
        .ToList();

    private bool FiltreVar => KapsamOlustur().FiltreVarMi;

    private static string? TarihDegeri(DateTime? tarih) => tarih?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string DonemIlkGun => DonemGecerli ? new DateTime(yil, ay, 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
    private string DonemSonGun => DonemGecerli ? new DateTime(yil, ay, DateTime.DaysInMonth(yil, ay)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

    private string KapsamOzetiMetni => onizleme is null
        ? "Tüm dönem"
        : HakedisKapsamAciklayici.Ozet(KapsamOlustur(), onizleme.Secenekler);

    // ── Önizleme tablosu ───────────────────────────────────────────────────

    // Her çizimde birden çok kez okunduğu için, kaynak veya görünüm seçenekleri değişmedikçe önbellekten döner.
    private HakedisKapsamOnizleme? gorunenKaynak;
    private bool gorunenHakedistekilerle;
    private bool gorunenEslestirmeAyri;
    private IReadOnlyList<HakedisKapsamSatiri> gorunenSatirlar = Array.Empty<HakedisKapsamSatiri>();

    private IReadOnlyList<HakedisKapsamSatiri> GorunenSatirlar
    {
        get
        {
            if (onizleme is null)
                return Array.Empty<HakedisKapsamSatiri>();

            var eslestirmeAyri = gruplama == HakedisGruplama.Eslestirme;
            if (!ReferenceEquals(gorunenKaynak, onizleme)
                || gorunenHakedistekilerle != hakedistekileriGoster
                || gorunenEslestirmeAyri != eslestirmeAyri)
            {
                gorunenSatirlar = HakedisRaporOlusturucu.Birlestir(
                    onizleme.Satirlar.Where(s => hakedistekileriGoster || !s.HakedisteMi), eslestirmeAyri);
                gorunenKaynak = onizleme;
                gorunenHakedistekilerle = hakedistekileriGoster;
                gorunenEslestirmeAyri = eslestirmeAyri;
            }

            return gorunenSatirlar;
        }
    }

    private List<HakedisSatirGrubu> GorunenGruplar => HakedisRaporOlusturucu.Grupla(GorunenSatirlar, gruplama);

    private IEnumerable<HakedisKapsamSatiri> OlusacakSatirlar => onizleme is null
        ? Enumerable.Empty<HakedisKapsamSatiri>()
        : onizleme.Satirlar.Where(s => s.Uygun && !s.HakedisteMi);

    private decimal OlusacakTutar => OlusacakSatirlar.Sum(s => s.HakedisTutari);
    private int HakedistekiPuantaj => onizleme?.Satirlar.Where(s => s.HakedisteMi).Sum(s => s.PuantajSayisi) ?? 0;
    private decimal HakedistekiSefer => onizleme?.Satirlar.Where(s => s.HakedisteMi).Sum(s => s.Sefer) ?? 0m;
    private int UygunsuzPuantaj => onizleme?.Satirlar.Where(s => !s.Uygun).Sum(s => s.PuantajSayisi) ?? 0;

    private IEnumerable<string> UygunsuzNedenleri => onizleme is null
        ? Enumerable.Empty<string>()
        : onizleme.Satirlar.Where(s => !s.Uygun && !string.IsNullOrWhiteSpace(s.UygunsuzlukNedeni))
            .Select(s => s.UygunsuzlukNedeni!)
            .Distinct();

    /// <summary>Seçili türün hangi araçları kapsadığını anlatır; tür dışı kalan satırların nedenini açıklar.</summary>
    private string TurKapsamiAciklamasi => seciliTip switch
    {
        HakedisTipi.Tedarikci => "Tedarikçi hakedişi yalnızca tedarikçi araçlarını kapsar.",
        HakedisTipi.AracSahibi => "Araç sahibi hakedişi yalnızca komisyon ve kiralık araçları kapsar (özmal ve tedarikçi araçları hariç).",
        _ => string.Empty
    };
    private bool GorunenHakedisteVar => GorunenSatirlar.Any(s => s.HakedisteMi || !s.Uygun);

    private bool GosterCari => gruplama != HakedisGruplama.Cari;
    private bool GosterGuzergah => gruplama != HakedisGruplama.Guzergah;
    private bool GosterPlaka => gruplama != HakedisGruplama.Arac;
    private bool GosterSahip => gruplama != HakedisGruplama.AracSahibi;
    private bool GosterSofor => gruplama != HakedisGruplama.Sofor;

    private int GrupEtiketKolonSayisi =>
        (GosterCari ? 1 : 0) + (GosterGuzergah ? 1 : 0) + (GosterPlaka ? 1 : 0) + (GosterSahip ? 1 : 0) + (GosterSofor ? 1 : 0);

    private bool OnizlemeGrubuAcik(string anahtar) => onizlemeTumuAcik != tersOnizlemeGruplari.Contains(anahtar);

    private void OnizlemeGrubuDegistir(string anahtar)
    {
        if (!tersOnizlemeGruplari.Add(anahtar))
            tersOnizlemeGruplari.Remove(anahtar);
    }

    private void OnizlemeGruplariniAcKapat()
    {
        onizlemeTumuAcik = !onizlemeTumuAcik;
        tersOnizlemeGruplari.Clear();
    }

    private void GruplamaDegisti()
    {
        tersOnizlemeGruplari.Clear();
    }

    // ── Hakediş oluşturma ──────────────────────────────────────────────────

    private async Task HakedisOlusturAsync()
    {
        if (onizleme is null || onizleme.OlusacakHakedisSayisi == 0)
        {
            bilgiMesaji = "Seçili kapsamda yeni hakedişe girecek satır yok.";
            return;
        }

        hataMesaji = null;
        bilgiMesaji = null;
        isBusy = true;
        try
        {
            // Önce dönem puantaj tahakkuklarını güncel güzergâh fiyatlarına göre yeniden hesapla
            // (eski çarpanlı değerleri temizler); sonra kapsama göre hakedişleri üret.
            if (EtkinFirmaId > 0)
                await FiloKomisyonService.TahakkuklariYenidenHesaplaAsync(EtkinFirmaId, yil, ay);

            var sonuc = await HakedisService.KapsamliUretAsync(seciliTip, KapsamOlustur());
            sonUretimSonucu = sonuc;
            bilgiMesaji = $"{sonuc.UretilenAdet} hakediş taslağı oluşturuldu / yenilendi (KDV dahil {sonuc.ToplamTutar:N2} ₺)"
                + (sonuc.AtlananAdet > 0 ? $", {sonuc.AtlananAdet} kayıt atlandı" : string.Empty)
                + (sonuc.HataliAdet > 0 ? $", {sonuc.HataliAdet} hata" : string.Empty) + ".";

            seciliHakedisIds.Clear();
            await YukleHakedisVeMutabakatAsync();
            aktifSekme = Sekme.Hakedisler;
        }
        catch (Exception ex)
        {
            hataMesaji = ex.GetBaseException().Message;
        }
        finally
        {
            isBusy = false;
        }

        await OnizlemeYenileAsync();
    }

    // ── Dışa aktarma (önizleme) ────────────────────────────────────────────

    private HakedisRaporTablosu? RaporTablosu()
    {
        if (onizleme is null || GorunenSatirlar.Count == 0)
            return null;

        return HakedisRaporOlusturucu.TabloOlustur(
            GorunenSatirlar, seciliTip, gruplama, karlilikKolonlari, yil, ay,
            HakedisKapsamAciklayici.Ozet(onizleme.Kapsam, onizleme.Secenekler));
    }

    private string RaporDosyaAdi(string uzanti) => $"Hakedis_{GetTipText(seciliTip)}_{yil}-{ay:D2}.{uzanti}";

    private async Task OnizlemeExceleAktarAsync()
    {
        try
        {
            var tablo = RaporTablosu();
            if (tablo is null)
                return;

            var bytes = HakedisRaporOlusturucu.ExcelOlustur(tablo);
            await JS.InvokeVoidAsync("downloadFile", RaporDosyaAdi("xlsx"), Convert.ToBase64String(bytes),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            hataMesaji = $"Excel oluşturulurken hata: {ex.GetBaseException().Message}";
        }
    }

    private async Task OnizlemePdfAktarAsync()
    {
        try
        {
            var tablo = RaporTablosu();
            if (tablo is null)
                return;

            var bytes = PdfService.GenerateHakedisRaporTablosuPdf(tablo);
            await JS.InvokeVoidAsync("downloadFile", RaporDosyaAdi("pdf"), Convert.ToBase64String(bytes), "application/pdf");
        }
        catch (Exception ex)
        {
            hataMesaji = $"PDF oluşturulurken hata: {ex.GetBaseException().Message}";
        }
    }

    // ── Oluşturulan hakedişler ─────────────────────────────────────────────

    private async Task YukleAsync()
    {
        hataMesaji = null;
        bilgiMesaji = null;

        if (!DonemGecerli)
        {
            hataMesaji = "Geçersiz dönem seçimi.";
            return;
        }

        isBusy = true;
        try
        {
            await YukleHakedisVeMutabakatAsync();
        }
        catch (Exception ex)
        {
            hataMesaji = ex.GetBaseException().Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task YukleHakedisVeMutabakatAsync()
    {
        hakedisler = await HakedisService.GetHakedislerAsync(yil, ay, seciliTip);
        listeTumuAcik = hakedisler.Count <= 30;
        tersListeGruplari.Clear();
        await ReferansAdlariniYukleAsync();

        puantajDonemOzeti = EtkinFirmaId > 0
            ? await FiloKomisyonService.GetPuantajDonemOzetiAsync(EtkinFirmaId, yil, ay)
            : null;
    }

    private IEnumerable<Hakedis> DurumaGoreHakedisler(HakedisDurum? durum) =>
        durum is null ? hakedisler : hakedisler.Where(h => h.Durum == durum);

    private int DurumAdedi(HakedisDurum? durum) => DurumaGoreHakedisler(durum).Count();

    private void DurumSec(string durum) => durumFiltre = durum;

    private List<Hakedis> FiltreliHakedisler
    {
        get
        {
            IEnumerable<Hakedis> sonuc = hakedisler;

            if (!string.IsNullOrWhiteSpace(durumFiltre) && Enum.TryParse<HakedisDurum>(durumFiltre, out var durum))
                sonuc = sonuc.Where(h => h.Durum == durum);

            if (!string.IsNullOrWhiteSpace(aramaMetni))
            {
                var kelimeler = aramaMetni.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                sonuc = sonuc.Where(h =>
                {
                    var metin = $"{GetReferansAd(h)} {GetGuzergahOzet(h)} {GetKapsamOzeti(h)}";
                    return kelimeler.All(k => metin.Contains(k, StringComparison.CurrentCultureIgnoreCase));
                });
            }

            return sonuc.ToList();
        }
    }

    private void ListeFiltreleriniTemizle()
    {
        durumFiltre = "";
        aramaMetni = "";
    }

    private bool TumSeciliMi => FiltreliHakedisler.Any() && FiltreliHakedisler.All(h => seciliHakedisIds.Contains(h.Id));
    private List<int> SeciliTaslakIdler => hakedisler.Where(h => seciliHakedisIds.Contains(h.Id) && h.Durum == HakedisDurum.Taslak).Select(h => h.Id).ToList();
    private List<int> SeciliFaturalanabilirIdler => hakedisler.Where(h => seciliHakedisIds.Contains(h.Id) && h.Durum == HakedisDurum.Onaylandi && h.Tip != HakedisTipi.Arac).Select(h => h.Id).ToList();
    private List<int> SeciliSilinebilirIdler => hakedisler.Where(h => seciliHakedisIds.Contains(h.Id) && h.Durum is not (HakedisDurum.Faturalandi or HakedisDurum.TahsilEdildi or HakedisDurum.Odendi)).Select(h => h.Id).ToList();

    // Gruplama sonuçları
    private Dictionary<string, List<Hakedis>> GetGroupedData()
    {
        var kaynak = FiltreliHakedisler;
        return selectedGrouping switch
        {
            "referans" => kaynak
                .GroupBy(GetReferansAd)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList()),

            _ => // "cari" (default)
                kaynak
                    .GroupBy(GetCariGrupAnahtari)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.ToList()),
        };
    }

    private bool ListeGrubuAcik(string anahtar) => listeTumuAcik != tersListeGruplari.Contains(anahtar);

    private void ToggleGroup(string anahtar)
    {
        if (!tersListeGruplari.Add(anahtar))
            tersListeGruplari.Remove(anahtar);
    }

    private void ListeGruplariniAcKapat()
    {
        listeTumuAcik = !listeTumuAcik;
        tersListeGruplari.Clear();
    }

    private void SecimiDegistir(int hakedisId)
    {
        if (!seciliHakedisIds.Add(hakedisId))
            seciliHakedisIds.Remove(hakedisId);
    }

    private void TumunuSecKaldir()
    {
        if (TumSeciliMi)
            seciliHakedisIds.Clear();
        else
            seciliHakedisIds.UnionWith(FiltreliHakedisler.Select(h => h.Id));
    }

    private void ToggleHakedisDetay(int hakedisId)
    {
        if (!acikHakedisDetaylari.Add(hakedisId))
            acikHakedisDetaylari.Remove(hakedisId);
    }

    /// <summary>Tek bir işlem çalıştırır: meşgul bayrağı, hata/bilgi mesajı ve liste yenileme ortak.</summary>
    private async Task IslemYapAsync(Func<Task<string?>> islem, Action? sonra = null)
    {
        hataMesaji = null;
        bilgiMesaji = null;
        isBusy = true;
        try
        {
            bilgiMesaji = await islem();
            sonra?.Invoke();
            await YukleHakedisVeMutabakatAsync();
        }
        catch (Exception ex)
        {
            hataMesaji = ex.GetBaseException().Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private Task OnaylaAsync(int hakedisId) => IslemYapAsync(async () =>
    {
        await HakedisService.OnaylaAsync(hakedisId, "Operasyon");
        seciliHakedisIds.Remove(hakedisId);
        return "Hakediş onaylandı.";
    });

    private Task IptalAsync(int hakedisId) => IslemYapAsync(async () =>
    {
        await HakedisService.IptalEtAsync(hakedisId, "UI üzerinden iptal");
        seciliHakedisIds.Remove(hakedisId);
        return "Hakediş iptal edildi.";
    });

    private Task FaturalaAsync(int hakedisId) => IslemYapAsync(async () =>
    {
        var fatura = await HakedisService.FaturayaDonustureAsync(hakedisId, BusinessTime.Today);
        seciliHakedisIds.Remove(hakedisId);
        return $"Hakediş faturaya dönüştürüldü. Fatura No: {fatura.FaturaNo}";
    });

    private Task SilAsync(int hakedisId) => IslemYapAsync(async () =>
    {
        await HakedisService.SilAsync(hakedisId);
        seciliHakedisIds.Remove(hakedisId);
        return "Hakediş silindi.";
    });

    private async Task PdfIndirAsync(int hakedisId)
    {
        hataMesaji = null;
        bilgiMesaji = null;

        isBusy = true;
        try
        {
            var hakedis = await HakedisService.GetByIdAsync(hakedisId)
                ?? throw new InvalidOperationException("Hakediş bulunamadı.");

            var listedeki = hakedisler.FirstOrDefault(x => x.Id == hakedisId);
            var referansAd = listedeki is null ? null : GetReferansAd(listedeki);
            var bytes = PdfService.GenerateHakedisPdf(hakedis, referansAd);

            var dosyaAdi = $"Hakedis_{GetTipText(hakedis.Tip)}_{hakedis.Yil}{hakedis.Ay:D2}_{hakedis.Id}.pdf";
            await JS.InvokeVoidAsync("downloadFileFromBytes", dosyaAdi, "application/pdf", bytes);
            bilgiMesaji = "Hakediş PDF indirildi.";
        }
        catch (Exception ex)
        {
            hataMesaji = ex.GetBaseException().Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private Task TopluOnaylaSecilenAsync()
    {
        var ids = SeciliTaslakIdler;
        if (!ids.Any())
        {
            bilgiMesaji = "Toplu onay için uygun (Taslak) seçili kayıt yok.";
            return Task.CompletedTask;
        }

        return IslemYapAsync(async () =>
        {
            var sonuc = await HakedisService.TopluOnaylaAsync(ids, "Operasyon");
            return IslemOzeti("Toplu onay", sonuc);
        }, seciliHakedisIds.Clear);
    }

    private Task TopluFaturalaSecilenAsync()
    {
        var ids = SeciliFaturalanabilirIdler;
        if (!ids.Any())
        {
            bilgiMesaji = "Toplu faturalama için uygun (Onaylı ve Araç dışı) seçili kayıt yok.";
            return Task.CompletedTask;
        }

        return IslemYapAsync(async () =>
        {
            var sonuc = await HakedisService.TopluFaturalaAsync(ids, BusinessTime.Today);
            return IslemOzeti("Toplu faturalama", sonuc);
        }, seciliHakedisIds.Clear);
    }

    private Task TopluSilSecilenAsync()
    {
        var ids = SeciliSilinebilirIdler;
        if (!ids.Any())
        {
            bilgiMesaji = "Toplu silme için uygun seçili kayıt yok.";
            return Task.CompletedTask;
        }

        return IslemYapAsync(async () =>
        {
            var sonuc = await HakedisService.TopluSilAsync(ids);
            return IslemOzeti("Toplu silme", sonuc);
        }, seciliHakedisIds.Clear);
    }

    private Task TaslaklariYenileSecilenAsync()
    {
        var ids = SeciliTaslakIdler;
        if (!ids.Any())
        {
            bilgiMesaji = "Yenilemek için seçili Taslak hakediş yok.";
            return Task.CompletedTask;
        }

        return IslemYapAsync(async () =>
        {
            // Üretimdeki gibi önce tahakkuklar güncel güzergâh fiyatlarına göre yeniden hesaplanır.
            if (EtkinFirmaId > 0)
                await FiloKomisyonService.TahakkuklariYenidenHesaplaAsync(EtkinFirmaId, yil, ay);

            var sonuc = await HakedisService.TaslaklariYenileAsync(ids);
            return IslemOzeti("Taslak yenileme", sonuc);
        }, seciliHakedisIds.Clear);
    }

    private static string IslemOzeti(string baslik, TopluIslemSonuc sonuc)
    {
        var ozet = $"{baslik} tamamlandı. Başarılı: {sonuc.BasariliAdet}, Atlanan: {sonuc.AtlananAdet}, Hatalı: {sonuc.HataliAdet}";
        var neden = sonuc.Satirlar
            .Where(s => s.Sonuc != "Tamam" && !string.IsNullOrWhiteSpace(s.Mesaj))
            .Select(s => $"#{s.HakedisId}: {s.Mesaj}")
            .FirstOrDefault();
        return neden is null ? ozet : $"{ozet}. İlk neden — {neden}";
    }

    // ── Liste dışa aktarma ─────────────────────────────────────────────────

    private async Task ExceleAktarAsync()
    {
        try
        {
            if (!FiltreliHakedisler.Any()) return;

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Hakediş");

            var basliklar = new[] { "#", "Tip", "Referans", "Kapsam", "Güzergah", "Toplam Sefer", "Birim Fiyat", "Tutar", "KDV Oran %", "KDV Tutar", "Genel Toplam", "Durum" };
            for (var i = 0; i < basliklar.Length; i++)
            {
                var hucre = ws.Cell(1, i + 1);
                hucre.Value = basliklar[i];
                hucre.Style.Font.Bold = true;
                hucre.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            var satir = 2;
            foreach (var h in FiltreliHakedisler.OrderBy(x => GetReferansAd(x)))
            {
                ws.Cell(satir, 1).Value = h.Id;
                ws.Cell(satir, 2).Value = GetTipText(h.Tip);
                ws.Cell(satir, 3).Value = GetReferansAd(h);
                ws.Cell(satir, 4).Value = GetKapsamOzeti(h);
                ws.Cell(satir, 5).Value = GetGuzergahOzet(h);
                ws.Cell(satir, 6).Value = h.ToplamSeferSayisi;
                ws.Cell(satir, 7).Value = h.BirimFiyat;
                ws.Cell(satir, 8).Value = h.Tutar;
                ws.Cell(satir, 9).Value = h.KdvOran;
                ws.Cell(satir, 10).Value = h.KdvTutar;
                ws.Cell(satir, 11).Value = h.GenelToplam;
                ws.Cell(satir, 12).Value = h.Durum.ToString();
                satir++;
            }

            // Toplam satırı
            ws.Cell(satir, 5).Value = "TOPLAM";
            ws.Cell(satir, 5).Style.Font.Bold = true;
            foreach (var col in new[] { 6, 8, 10, 11 })
            {
                var hucre = ws.Cell(satir, col);
                hucre.FormulaA1 = $"SUM({ws.Cell(2, col).Address}:{ws.Cell(satir - 1, col).Address})";
                hucre.Style.Font.Bold = true;
            }

            ws.Range(2, 6, satir, 11).Style.NumberFormat.Format = "#,##0.00";
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var dosyaAdi = $"OperasyonelHakedis_{yil}-{ay:D2}_{GetTipText(seciliTip)}.xlsx";
            var base64 = Convert.ToBase64String(stream.ToArray());
            await JS.InvokeVoidAsync("downloadFile", dosyaAdi, base64, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            hataMesaji = $"Excel oluşturulurken hata: {ex.Message}";
        }
    }

    // ── Referans adları ────────────────────────────────────────────────────

    private async Task ReferansAdlariniYukleAsync()
    {
        var referansIdler = hakedisler
            .Select(h => h.ReferansId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (!referansIdler.Any())
            return;

        switch (seciliTip)
        {
            case HakedisTipi.Kurum:
                var guzergahCariIdleri = hakedisler
                    .SelectMany(h => h.Detaylar)
                    .Where(d => d.Guzergah != null && d.Guzergah.CariId > 0)
                    .Select(d => d.Guzergah!.CariId);
                var kurumCariIdleri = referansIdler.Concat(guzergahCariIdleri).Distinct().ToHashSet();
                var kurumCariler = await CariService.GetAllAsync();
                kurumAdlari.Clear();
                cariDetaylari.Clear();
                foreach (var c in kurumCariler.Where(c => kurumCariIdleri.Contains(c.Id)
                    && (EtkinFirmaId <= 0 || c.FirmaId == EtkinFirmaId)))
                {
                    kurumAdlari[c.Id] = c.Unvan;
                    cariDetaylari[c.Id] = c;
                }

                break;

            case HakedisTipi.Tedarikci:
                var tedarikciler = await TedarikciService.GetAllAsync(false);
                tedarikciAdlari.Clear();
                foreach (var t in tedarikciler.Where(t => referansIdler.Contains(t.Id)))
                    tedarikciAdlari[t.Id] = t.Unvan;
                break;

            case HakedisTipi.AracSahibi:
                // Araç sahibi hakedişinin referansı, komisyoncu / kiralık plaka sahibinin cari kartıdır.
                var sahipCariler = await CariService.GetAllAsync();
                sahipCariAdlari.Clear();
                cariDetaylari.Clear();
                foreach (var c in sahipCariler.Where(c => referansIdler.Contains(c.Id)))
                {
                    sahipCariAdlari[c.Id] = c.Unvan;
                    cariDetaylari[c.Id] = c;
                }

                break;

            case HakedisTipi.Arac:
                var araclar = await AracService.GetAllAsync();
                var cariler = await CariService.GetAllAsync();
                var tumTedarikciler = await TedarikciService.GetAllAsync(false);

                aracAdlari.Clear();
                aracSahibiAdlari.Clear();
                cariDetaylari.Clear();

                var cariById = cariler.ToDictionary(c => c.Id, c => c);
                var tedarikciById = tumTedarikciler.ToDictionary(t => t.Id, t => t);

                foreach (var a in araclar.Where(a => referansIdler.Contains(a.Id)))
                {
                    aracAdlari[a.Id] = !string.IsNullOrWhiteSpace(a.AktifPlaka) ? a.AktifPlaka : a.Plaka;

                    string sahipAd = "Bilinmeyen Sahip";
                    Cari? sahipCari = null;

                    if (a.SahiplikTipi == AracSahiplikTipi.Kiralik && a.KiralikCariId.HasValue && cariById.TryGetValue(a.KiralikCariId.Value, out var kiralikCari))
                    {
                        sahipAd = kiralikCari.Unvan;
                        sahipCari = kiralikCari;
                    }
                    else if (a.SahiplikTipi == AracSahiplikTipi.Komisyon && a.KomisyoncuCariId.HasValue && cariById.TryGetValue(a.KomisyoncuCariId.Value, out var komisyonCari))
                    {
                        sahipAd = komisyonCari.Unvan;
                        sahipCari = komisyonCari;
                    }
                    else if (a.SahiplikTipi == AracSahiplikTipi.Tedarikci && a.TasimaTedarikciId.HasValue && tedarikciById.TryGetValue(a.TasimaTedarikciId.Value, out var t))
                    {
                        sahipAd = t.Unvan;
                        if (t.CariId.HasValue && cariById.TryGetValue(t.CariId.Value, out var tCari))
                            sahipCari = tCari;
                    }
                    else if (!string.IsNullOrWhiteSpace(a.Firma?.FirmaAdi))
                    {
                        sahipAd = a.Firma.FirmaAdi;
                    }

                    aracSahibiAdlari[a.Id] = sahipAd;
                    if (sahipCari != null)
                        cariDetaylari[a.Id] = sahipCari;
                }
                break;
        }
    }

    private string GetCariGrupAnahtari(Hakedis h)
    {
        return h.Tip switch
        {
            HakedisTipi.Kurum => $"Cari: {GetKurumCariOzeti(h)}",
            HakedisTipi.Tedarikci => tedarikciAdlari.TryGetValue(h.ReferansId, out var tedarikciAd) ? $"Cari: {tedarikciAd}" : $"Tedarikçi #{h.ReferansId}",
            HakedisTipi.AracSahibi => sahipCariAdlari.TryGetValue(h.ReferansId, out var sahipCariAd) ? $"Cari: {sahipCariAd}" : $"Cari #{h.ReferansId}",
            HakedisTipi.Arac => aracSahibiAdlari.TryGetValue(h.ReferansId, out var sahipAd) ? $"Cari: {sahipAd}" : "Cari: Bilinmeyen Sahip",
            _ => h.ReferansId.ToString()
        };
    }

    private string GetReferansAd(Hakedis h) => h.Tip switch
    {
        HakedisTipi.Kurum => GetKurumCariOzeti(h),
        HakedisTipi.Tedarikci => tedarikciAdlari.TryGetValue(h.ReferansId, out var tedarikciAd)
            ? tedarikciAd
            : $"Tedarikçi #{h.ReferansId}",
        HakedisTipi.AracSahibi => sahipCariAdlari.TryGetValue(h.ReferansId, out var sahipCariAdi)
            ? sahipCariAdi
            : $"Cari #{h.ReferansId}",
        HakedisTipi.Arac => aracAdlari.TryGetValue(h.ReferansId, out var aracAd)
            ? (aracSahibiAdlari.TryGetValue(h.ReferansId, out var sahipAd) ? $"{aracAd} • {sahipAd}" : aracAd)
            : $"Araç #{h.ReferansId}",
        _ => h.ReferansId.ToString()
    };

    private string GetCariTooltip(Hakedis h)
    {
        Cari? cari = null;

        if (h.Tip == HakedisTipi.Kurum && !MusteriEslestirmedenMi(h))
        {
            var routeCariId = h.Detaylar
                .Select(d => d.Guzergah?.CariId ?? 0)
                .FirstOrDefault(id => id > 0);
            cariDetaylari.TryGetValue(routeCariId, out cari);
        }
        else if (h.Tip is HakedisTipi.Arac or HakedisTipi.AracSahibi or HakedisTipi.Kurum)
            cariDetaylari.TryGetValue(h.ReferansId, out cari);

        if (cari is null)
            return GetReferansAd(h);

        return $"Cari: {cari.Unvan} | VergiNo: {cari.VergiNo ?? "-"} | Telefon: {cari.Telefon ?? "-"} | İl/İlçe: {cari.Il ?? "-"}/{cari.Ilce ?? "-"}";
    }

    /// <summary>Hakedişin müşteri kurumu eşleştirmeden alınarak mı hazırlandığı (ReferansId doğrudan cari kartı).</summary>
    private static bool MusteriEslestirmedenMi(Hakedis h) => HakedisUretimParametreleri.MusteriEslestirmedenMi(h.GenerationParams);

    private string GetKurumCariOzeti(Hakedis h)
    {
        if (MusteriEslestirmedenMi(h))
            return kurumAdlari.TryGetValue(h.ReferansId, out var eslestirmeCari) ? eslestirmeCari : $"Cari #{h.ReferansId}";

        var cariAdlari = h.Detaylar
            .Where(d => d.Guzergah != null && d.Guzergah.CariId > 0)
            .Select(d => CariAdiniGetir(d.Guzergah!.CariId))
            .Where(ad => !string.IsNullOrWhiteSpace(ad))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(ad => ad, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return cariAdlari.Count == 0
            ? kurumAdlari.TryGetValue(h.ReferansId, out var legacyAd) ? legacyAd : $"Cari #{h.ReferansId}"
            : string.Join(", ", cariAdlari);
    }

    private string CariAdiniGetir(int cariId) =>
        cariDetaylari.TryGetValue(cariId, out var cari) ? cari.Unvan : $"Cari #{cariId}";

    private static string GetKapsamOzeti(Hakedis h) =>
        HakedisUretimParametreleri.Coz(h.GenerationParams)?.Ozet is { Length: > 0 } ozet ? ozet : "Tüm dönem";

    private static bool KapsamliMi(Hakedis h) =>
        HakedisUretimParametreleri.ImzaOku(h.GenerationParams).Length > 0;

    private sealed record GuzergahDetaySatiri(string Ad, decimal Sefer, decimal GelirBirim, decimal GelirToplam, decimal GiderBirim, decimal GiderToplam);

    private static List<GuzergahDetaySatiri> GetGuzergahDetaylari(Hakedis h)
    {
        return h.Detaylar
            .Where(d => d.Guzergah != null && !string.IsNullOrWhiteSpace(d.Guzergah!.GuzergahAdi))
            .GroupBy(d => d.Guzergah!.GuzergahAdi.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var sefer = g.Sum(x => x.SeferSayisi);
                var guzergah = g.First().Guzergah!;

                // Sefer fiyatı doğrudan güzergah kartındaki fiyatlardan alınır.
                var gelirBirim = guzergah.GelirFiyat;
                var gelirToplam = sefer * gelirBirim;

                var giderBirim = guzergah.GiderFiyat;
                var giderToplam = sefer * giderBirim;

                return new GuzergahDetaySatiri(
                    g.Key,
                    sefer,
                    gelirBirim,
                    gelirToplam,
                    giderBirim,
                    giderToplam);
            })
            .ToList();
    }

    private decimal ToplamSeferGelir => FiltreliHakedisler.SelectMany(h => GetGuzergahDetaylari(h)).Sum(d => d.GelirToplam);
    private decimal ToplamSeferGider => FiltreliHakedisler.SelectMany(h => GetGuzergahDetaylari(h)).Sum(d => d.GiderToplam);

    private decimal HakedisToplamSefer => FiltreliHakedisler.Sum(h => h.ToplamSeferSayisi);
    private decimal HakedisToplamGelir => FiltreliHakedisler.Sum(h => h.Tutar);
    private decimal HakedisToplamGider => ToplamSeferGider;
    private decimal SeferFarki => (puantajDonemOzeti?.ToplamSefer ?? 0m) - HakedisToplamSefer;
    private decimal GelirFarki => (puantajDonemOzeti?.ToplamGelir ?? 0m) - HakedisToplamGelir;
    private decimal GiderFarki => (puantajDonemOzeti?.ToplamGider ?? 0m) - HakedisToplamGider;
    private bool MutabakatSorunuVar => seciliTip switch
    {
        HakedisTipi.Kurum => Math.Abs(SeferFarki) > 0.01m || Math.Abs(GelirFarki) > 0.01m,
        HakedisTipi.Tedarikci or HakedisTipi.AracSahibi => Math.Abs(SeferFarki) > 0.01m || Math.Abs(GiderFarki) > 0.01m,
        HakedisTipi.Arac => Math.Abs(SeferFarki) > 0.01m,
        _ => Math.Abs(SeferFarki) > 0.01m || Math.Abs(GelirFarki) > 0.01m || Math.Abs(GiderFarki) > 0.01m
    };

    private static string GetGuzergahOzet(Hakedis h, bool kisalt = false)
    {
        var guzergahlar = h.Detaylar
            .Where(d => d.Guzergah != null)
            .Select(d => d.Guzergah!.GuzergahAdi)
            .Where(ad => !string.IsNullOrWhiteSpace(ad))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(ad => ad)
            .ToList();

        if (!guzergahlar.Any()) return "-";

        if (kisalt && guzergahlar.Count > 2)
            return $"{guzergahlar[0]}, {guzergahlar[1]} (+{guzergahlar.Count - 2})";

        return string.Join(", ", guzergahlar);
    }

    // ── Görünüm yardımcıları ───────────────────────────────────────────────

    private static string Tl(decimal tutar) => $"{tutar:N2} ₺";

    private static string Sayi(decimal deger) => deger.ToString("N2");

    private static string GetTipText(HakedisTipi tip) => tip.Metin();

    private static string GetTipBadge(HakedisTipi tip) => tip switch
    {
        HakedisTipi.Kurum => "bg-primary",
        HakedisTipi.Tedarikci => "bg-success",
        HakedisTipi.AracSahibi => "bg-warning text-dark",
        HakedisTipi.Arac => "bg-info text-dark",
        _ => "bg-secondary"
    };

    private static string GetDurumBadge(HakedisDurum durum) => durum switch
    {
        HakedisDurum.Taslak => "bg-secondary",
        HakedisDurum.Onaylandi => "bg-warning text-dark",
        HakedisDurum.Faturalandi => "bg-primary",
        HakedisDurum.TahsilEdildi => "bg-success",
        HakedisDurum.Odendi => "bg-success",
        HakedisDurum.Iptal => "bg-danger",
        _ => "bg-light text-dark"
    };

    private static string GetDurumText(HakedisDurum durum) => durum switch
    {
        HakedisDurum.Taslak => "Taslak",
        HakedisDurum.Onaylandi => "Onaylı",
        HakedisDurum.Faturalandi => "Faturalı",
        HakedisDurum.TahsilEdildi => "Tahsil edildi",
        HakedisDurum.Odendi => "Ödendi",
        HakedisDurum.Iptal => "İptal",
        _ => durum.ToString()
    };

    private string TutarBasligi => HakedisRaporOlusturucu.TutarBasligi(seciliTip);
}
