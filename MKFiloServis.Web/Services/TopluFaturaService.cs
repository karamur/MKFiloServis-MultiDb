using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class TopluFaturaService : ITopluFaturaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFaturaService _faturaService;

    public TopluFaturaService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IFaturaService faturaService)
    {
        _contextFactory = contextFactory;
        _faturaService = faturaService;
    }

    public async Task<TopluFaturaOzet> GetDonemOzetiAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var ozet = new TopluFaturaOzet
        {
            Yil = yil,
            Ay = ay
        };

        // Puantaj kayıtları - DbSet adı PuantajKayitlar
        var puantajQuery = context.PuantajKayitlar
            .Where(p => p.Yil == yil && p.Ay == ay);

        // GELİR - Fatura kesilmemiş (satış faturaları)
        var gelirKayitlar = await puantajQuery
            .Where(p => !p.GelirFaturaKesildi && p.KurumCariId != null)
            .ToListAsync();

        ozet.GelirFaturaKesilecekKayitSayisi = gelirKayitlar.Count;
        ozet.GelirFaturaKesilecekCariSayisi = gelirKayitlar.Select(p => p.KurumCariId).Distinct().Count();
        ozet.GelirFaturaKesilecekTutar = gelirKayitlar.Sum(p => p.GelirToplam);

        // GELİR - Fatura kesilmiş
        var gelirKesilenKayitlar = await puantajQuery
            .Where(p => p.GelirFaturaKesildi && p.KurumCariId != null)
            .ToListAsync();

        ozet.GelirFaturaKesilenSayisi = gelirKesilenKayitlar.Select(p => p.GelirFaturaId).Distinct().Count();
        ozet.GelirFaturaKesilenTutar = gelirKesilenKayitlar.Sum(p => p.GelirToplam);

        // GİDER - Fatura alınmamış (alış faturaları)
        var giderKayitlar = await puantajQuery
            .Where(p => !p.GiderFaturaAlindi && p.OdemeYapilacakCariId != null)
            .ToListAsync();

        ozet.GiderFaturaAlinacakKayitSayisi = giderKayitlar.Count;
        ozet.GiderFaturaAlinacakCariSayisi = giderKayitlar.Select(p => p.OdemeYapilacakCariId).Distinct().Count();
        ozet.GiderFaturaAlinacakTutar = giderKayitlar.Sum(p => p.Odenecek);

        // GİDER - Fatura alınmış
        var giderAlinanKayitlar = await puantajQuery
            .Where(p => p.GiderFaturaAlindi && p.OdemeYapilacakCariId != null)
            .ToListAsync();

        ozet.GiderFaturaAlinanSayisi = giderAlinanKayitlar.Select(p => p.GiderFaturaId).Distinct().Count();
        ozet.GiderFaturaAlinanTutar = giderAlinanKayitlar.Sum(p => p.Odenecek);

        return ozet;
    }

    public async Task<List<TopluFaturaOnizleme>> GetOnizlemeAsync(TopluFaturaFiltre filtre)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var onizlemeler = new List<TopluFaturaOnizleme>();

        // Puantaj bazlı faturalama
        if (filtre.Kaynak == TopluFaturaKaynak.Puantaj)
        {
            if (filtre.FaturaYonu == FaturaYonu.Giden)
            {
                // Satış faturaları - müşteriye kesilecek
                onizlemeler = await GetGelirFaturaOnizlemeAsync(context, filtre);
            }
            else
            {
                // Alış faturaları - tedarikçiden alınacak
                onizlemeler = await GetGiderFaturaOnizlemeAsync(context, filtre);
            }
        }

        return onizlemeler;
    }

    private async Task<List<TopluFaturaOnizleme>> GetGelirFaturaOnizlemeAsync(
        ApplicationDbContext context, TopluFaturaFiltre filtre)
    {
        var onizlemeler = new List<TopluFaturaOnizleme>();

        // Fatura kesilmemiş puantaj kayıtları
        var query = context.PuantajKayitlar
            .Include(p => p.KurumCari)
            .Include(p => p.Guzergah)
            .Where(p => p.Yil == filtre.Yil && p.Ay == filtre.Ay && p.KurumCariId != null);

        if (filtre.SadeceFaturaKesilmemis)
        {
            query = query.Where(p => !p.GelirFaturaKesildi);
        }

        if (filtre.CariId.HasValue)
        {
            query = query.Where(p => p.KurumCariId == filtre.CariId.Value);
        }

        var kayitlar = await query.ToListAsync();

        if (filtre.CariBazliGrupla)
        {
            // Cari bazlı gruplama
            var gruplar = kayitlar.GroupBy(k => k.KurumCariId);

            foreach (var grup in gruplar)
            {
                var cari = grup.First().KurumCari;
                if (cari == null) continue;

                var onizleme = new TopluFaturaOnizleme
                {
                    CariId = cari.Id,
                    CariUnvan = cari.Unvan ?? "",
                    CariKod = cari.CariKodu ?? "",
                    VergiNo = cari.VergiNo,
                    FaturaTipi = FaturaTipi.SatisFaturasi,
                    EFaturaTipi = DetermineEFaturaTipi(cari),
                    FaturaTarihi = new DateTime(filtre.Yil, filtre.Ay, DateTime.DaysInMonth(filtre.Yil, filtre.Ay)),
                    VadeTarihi = new DateTime(filtre.Yil, filtre.Ay, DateTime.DaysInMonth(filtre.Yil, filtre.Ay)).AddDays(30),
                    PuantajKayitIdleri = grup.Select(k => k.Id).ToList()
                };

                // Kalemleri oluştur
                foreach (var kayit in grup)
                {
                    var kalem = new TopluFaturaKalemOnizleme
                    {
                        PuantajKayitId = kayit.Id,
                        GuzergahAdi = kayit.GuzergahAdi ?? kayit.Guzergah?.GuzergahAdi,
                        Aciklama = OlusturKalemAciklama(kayit, filtre.Yil, filtre.Ay),
                        Miktar = kayit.Gun,
                        Birim = kayit.Gun == 1 ? "Sefer" : "Gün",
                        BirimFiyat = kayit.BirimGelir,
                        KdvOrani = kayit.GelirKdvOrani > 0 ? kayit.GelirKdvOrani : 20
                    };
                    onizleme.Kalemler.Add(kalem);
                }

                // KDV oranını kalemlere göre ayarla (çoğunluk)
                if (onizleme.Kalemler.Any())
                {
                    onizleme.KdvOrani = onizleme.Kalemler
                        .GroupBy(k => k.KdvOrani)
                        .OrderByDescending(g => g.Count())
                        .First().Key;
                }

                // Durum kontrolü
                ValidateOnizleme(onizleme);

                onizlemeler.Add(onizleme);
            }
        }

        return onizlemeler.OrderBy(o => o.CariUnvan).ToList();
    }

    private async Task<List<TopluFaturaOnizleme>> GetGiderFaturaOnizlemeAsync(
        ApplicationDbContext context, TopluFaturaFiltre filtre)
    {
        var onizlemeler = new List<TopluFaturaOnizleme>();

        // Fatura alınmamış puantaj kayıtları (tedarikçi tarafı)
        var query = context.PuantajKayitlar
            .Include(p => p.OdemeYapilacakCari)
            .Include(p => p.Guzergah)
            .Where(p => p.Yil == filtre.Yil && p.Ay == filtre.Ay && p.OdemeYapilacakCariId != null);

        if (filtre.SadeceFaturaKesilmemis)
        {
            query = query.Where(p => !p.GiderFaturaAlindi);
        }

        if (filtre.CariId.HasValue)
        {
            query = query.Where(p => p.OdemeYapilacakCariId == filtre.CariId.Value);
        }

        var kayitlar = await query.ToListAsync();

        if (filtre.CariBazliGrupla)
        {
            var gruplar = kayitlar.GroupBy(k => k.OdemeYapilacakCariId);

            foreach (var grup in gruplar)
            {
                var cari = grup.First().OdemeYapilacakCari;
                if (cari == null) continue;

                var onizleme = new TopluFaturaOnizleme
                {
                    CariId = cari.Id,
                    CariUnvan = cari.Unvan ?? "",
                    CariKod = cari.CariKodu ?? "",
                    VergiNo = cari.VergiNo,
                    FaturaTipi = FaturaTipi.AlisFaturasi,
                    EFaturaTipi = DetermineEFaturaTipi(cari),
                    FaturaTarihi = new DateTime(filtre.Yil, filtre.Ay, DateTime.DaysInMonth(filtre.Yil, filtre.Ay)),
                    VadeTarihi = new DateTime(filtre.Yil, filtre.Ay, DateTime.DaysInMonth(filtre.Yil, filtre.Ay)).AddDays(30),
                    PuantajKayitIdleri = grup.Select(k => k.Id).ToList()
                };

                // Kalemleri oluştur
                foreach (var kayit in grup)
                {
                    var kalem = new TopluFaturaKalemOnizleme
                    {
                        PuantajKayitId = kayit.Id,
                        GuzergahAdi = kayit.GuzergahAdi ?? kayit.Guzergah?.GuzergahAdi,
                        Aciklama = OlusturKalemAciklama(kayit, filtre.Yil, filtre.Ay),
                        Miktar = kayit.Gun,
                        Birim = kayit.Gun == 1 ? "Sefer" : "Gün",
                        BirimFiyat = kayit.BirimGider,
                        KdvOrani = kayit.GiderKdvOrani20 > 0 ? 20 : (kayit.GiderKdvOrani10 > 0 ? 10 : 0)
                    };
                    onizleme.Kalemler.Add(kalem);
                }

                // KDV oranını kalemlere göre ayarla
                if (onizleme.Kalemler.Any())
                {
                    onizleme.KdvOrani = onizleme.Kalemler
                        .GroupBy(k => k.KdvOrani)
                        .OrderByDescending(g => g.Count())
                        .First().Key;
                }

                ValidateOnizleme(onizleme);
                onizlemeler.Add(onizleme);
            }
        }

        return onizlemeler.OrderBy(o => o.CariUnvan).ToList();
    }

    private string OlusturKalemAciklama(PuantajKayit kayit, int yil, int ay)
    {
        var ayAdi = new DateTime(yil, ay, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        var guzergah = kayit.GuzergahAdi ?? kayit.Guzergah?.GuzergahAdi ?? "Servis";
        var yon = kayit.Yon switch
        {
            PuantajYon.Sabah => "Sabah",
            PuantajYon.Aksam => "Akşam",
            PuantajYon.SabahAksam => "Sabah-Akşam",
            _ => ""
        };

        return $"{ayAdi} {guzergah} {yon} Servis Hizmeti".Trim();
    }

    private EFaturaTipi DetermineEFaturaTipi(Cari cari)
    {
        // Vergi no yoksa veya şahıs ise E-Arşiv
        if (string.IsNullOrEmpty(cari.VergiNo) || cari.VergiNo.Length == 11)
            return EFaturaTipi.EArsiv;

        // E-Fatura mükellefi ise E-Fatura (bu bilgi cariden alınabilir)
        // Şimdilik varsayılan E-Arşiv
        return EFaturaTipi.EArsiv;
    }

    private void ValidateOnizleme(TopluFaturaOnizleme onizleme)
    {
        var eksikler = new List<string>();

        if (onizleme.CariId <= 0)
            eksikler.Add("Cari seçilmemiş");

        if (!onizleme.Kalemler.Any())
            eksikler.Add("Fatura kalemi yok");

        if (onizleme.AraToplam <= 0)
            eksikler.Add("Tutar sıfır");

        if (eksikler.Any())
        {
            onizleme.Durum = TopluFaturaDurum.EksikBilgi;
            onizleme.DurumMesaji = string.Join(", ", eksikler);
        }
        else
        {
            onizleme.Durum = TopluFaturaDurum.Hazir;
        }
    }

    public async Task<TopluFaturaSonuc> FaturaOlusturAsync(List<TopluFaturaOnizleme> onizlemeler, int? firmaId = null)
    {
        var sonuc = new TopluFaturaSonuc();

        foreach (var onizleme in onizlemeler.Where(o => o.Secili && o.Durum == TopluFaturaDurum.Hazir))
        {
            try
            {
                var tekSonuc = await TekFaturaOlusturAsync(onizleme, firmaId);
                
                if (tekSonuc.Basarili)
                {
                    sonuc.OlusturulanFaturaSayisi++;
                    sonuc.ToplamTutar += onizleme.GenelToplam;
                    sonuc.OlusturulanFaturalar.AddRange(tekSonuc.OlusturulanFaturalar);
                }
                else
                {
                    sonuc.BasarisizFaturaSayisi++;
                    sonuc.Hatalar.AddRange(tekSonuc.Hatalar);
                }
            }
            catch (Exception ex)
            {
                sonuc.BasarisizFaturaSayisi++;
                sonuc.Hatalar.Add($"{onizleme.CariUnvan}: {ex.Message}");
            }
        }

        sonuc.Basarili = sonuc.OlusturulanFaturaSayisi > 0;
        sonuc.Mesaj = $"{sonuc.OlusturulanFaturaSayisi} fatura oluşturuldu" +
            (sonuc.BasarisizFaturaSayisi > 0 ? $", {sonuc.BasarisizFaturaSayisi} başarısız" : "");

        return sonuc;
    }

    public async Task<TopluFaturaSonuc> TekFaturaOlusturAsync(TopluFaturaOnizleme onizleme, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuc = new TopluFaturaSonuc();

        try
        {
            throw new InvalidOperationException(
                "TopluFatura devre dışı bırakıldı. " +
                "Fatura oluşturmak için lütfen PuantajExcelGrid (/personel/puantaj-grid) üzerinden puantaj girin, " +
                "kaydedin ve HakedisPuantaj → PuantajFinansService.IsleAsync zincirini kullanın.");
        }
        catch (Exception ex)
        {
            sonuc.Basarili = false;
            sonuc.Hatalar.Add($"Fatura oluşturma hatası: {ex.Message}");
        }

        return sonuc;
    }

    public async Task<CariFaturaAyar?> GetCariFaturaAyarAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var cari = await context.Cariler.FindAsync(cariId);
        if (cari == null) return null;

        return new CariFaturaAyar
        {
            CariId = cari.Id,
            CariUnvan = cari.Unvan ?? "",
            VarsayilanKdvOrani = 20, // Varsayılan
            TevkifatliMi = false, // Cari'de bu alan yok, varsayılan false
            TevkifatOrani = null,
            TevkifatKodu = null,
            VadeGunSayisi = 30, // Varsayılan
            EFaturaTipi = DetermineEFaturaTipi(cari)
        };
    }

    public async Task<List<PuantajKayit>> GetFaturaKesilmemisPuantajlarAsync(int yil, int ay, FaturaYonu yon, int? cariId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.PuantajKayitlar
            .Include(p => p.KurumCari)
            .Include(p => p.OdemeYapilacakCari)
            .Include(p => p.Guzergah)
            .Where(p => p.Yil == yil && p.Ay == ay);

        if (yon == FaturaYonu.Giden)
        {
            query = query.Where(p => !p.GelirFaturaKesildi && p.KurumCariId != null);
            if (cariId.HasValue)
                query = query.Where(p => p.KurumCariId == cariId.Value);
        }
        else
        {
            query = query.Where(p => !p.GiderFaturaAlindi && p.OdemeYapilacakCariId != null);
            if (cariId.HasValue)
                query = query.Where(p => p.OdemeYapilacakCariId == cariId.Value);
        }

        return await query.OrderBy(p => p.KurumCari!.Unvan).ToListAsync();
    }

    public async Task<List<(int Yil, int Ay)>> GetMevcutDonemlerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PuantajKayitlar
            .Select(p => new { p.Yil, p.Ay })
            .Distinct()
            .OrderByDescending(p => p.Yil)
            .ThenByDescending(p => p.Ay)
            .Select(p => ValueTuple.Create(p.Yil, p.Ay))
            .ToListAsync();
    }
}



