using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class KolayMuhasebeService : IKolayMuhasebeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;
    private readonly ICariService _cariService;

    public KolayMuhasebeService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMuhasebeService muhasebeService,
        ICariService cariService)
    {
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
        _cariService = cariService;
    }

    #region Önizleme Oluşturma

    public async Task<MuhasebeOnizleme> OnizlemeOlusturAsync(KolayMuhasebeGiris giris)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await GetMuhasebeAyarAsync();
        var fisTipi = GetFisTipi(giris.IslemTuru);
        var fisNo = await _muhasebeService.GenerateNextFisNoAsync(fisTipi);

        var onizleme = new MuhasebeOnizleme
        {
            FisNo = fisNo,
            FisTarihi = giris.IslemTarihi,
            FisTipi = fisTipi,
            Aciklama = OlusturAciklama(giris)
        };

        // İşlem türüne göre kalemler oluştur
        var kalemler = giris.IslemTuru switch
        {
            KolayIslemTuru.GelirFatura => await OlusturGelirFaturaKalemleri(context, giris, ayar),
            KolayIslemTuru.GiderFatura => await OlusturGiderFaturaKalemleri(context, giris, ayar),
            KolayIslemTuru.MasrafGirisi => await OlusturMasrafKalemleri(context, giris, ayar),
            KolayIslemTuru.TahsilatGirisi => await OlusturTahsilatKalemleri(context, giris, ayar),
            KolayIslemTuru.OdemeGirisi => await OlusturOdemeKalemleri(context, giris, ayar),
            KolayIslemTuru.MahsupKaydi => await OlusturMahsupKalemleri(context, giris, ayar),
            KolayIslemTuru.AvansGirisi => await OlusturAvansKalemleri(context, giris, ayar),
            _ => new List<MuhasebeKalemOnizleme>()
        };

        onizleme.Kalemler = kalemler;
        return onizleme;
    }

    private async Task<List<MuhasebeKalemOnizleme>> OlusturGelirFaturaKalemleri(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeAyar ayar)
    {
        var kalemler = new List<MuhasebeKalemOnizleme>();
        var siraNo = 1;

        // Cari hesap bilgisi al
        var cariHesapKodu = "120.01"; // Varsayılan alıcılar
        var cariUnvan = giris.CariUnvan ?? "Bilinmeyen Müşteri";

        if (giris.CariId.HasValue)
        {
            var cari = await context.Cariler.AsNoTracking().Include(c => c.MuhasebeHesap).OrderBy(c => c.Id).FirstOrDefaultAsync(c => c.Id == giris.CariId);
            if (cari != null)
            {
                cariUnvan = cari.Unvan;
                if (cari.MuhasebeHesap != null)
                    cariHesapKodu = cari.MuhasebeHesap.HesapKodu;
            }
        }

        var alicidanAlinacak = giris.TevkifatliMi ? giris.GenelToplam - giris.TevkifatTutar : giris.GenelToplam;

        // 120 Alıcılar BORÇ
        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = cariHesapKodu,
            HesapAdi = $"Alıcılar - {cariUnvan}",
            HesapId = await GetHesapIdAsync(context, cariHesapKodu),
            Borc = alicidanAlinacak,
            Alacak = 0,
            Aciklama = $"Fatura: {giris.BelgeNo}",
            CariId = giris.CariId,
            CariUnvan = cariUnvan
        });

        // Tevkifat varsa - 136 Diğer Çeşitli Alacaklar BORÇ
        if (giris.TevkifatliMi && giris.TevkifatTutar > 0)
        {
            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = ayar.TevkifatAlacakHesabi,
                HesapAdi = "Tevkifat Alacağı",
                HesapId = await GetHesapIdAsync(context, ayar.TevkifatAlacakHesabi),
                Borc = giris.TevkifatTutar,
                Alacak = 0,
                Aciklama = $"Tevkifat ({giris.TevkifatKodu})"
            });
        }

        // 600 Satışlar ALACAK
        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = ayar.SatisGelirHesabi,
            HesapAdi = "Yurtiçi Satışlar",
            HesapId = await GetHesapIdAsync(context, ayar.SatisGelirHesabi),
            Borc = 0,
            Alacak = giris.AraToplam,
            Aciklama = "Satış Geliri"
        });

        // 391 Hesaplanan KDV ALACAK
        if (giris.KdvTutar > 0)
        {
            var eslestirme = ayar.KdvHesapEslestirmeleri.FirstOrDefault(e => e.KdvOrani == (int)giris.KdvOrani);
            var kdvHesapKodu = eslestirme?.HesaplananKdvHesabi ?? ayar.HesaplananKdvHesabi;
            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = kdvHesapKodu,
                HesapAdi = "Hesaplanan KDV",
                HesapId = await GetHesapIdAsync(context, kdvHesapKodu),
                Borc = 0,
                Alacak = giris.KdvTutar,
                Aciklama = $"KDV %{giris.KdvOrani}"
            });
        }

        return kalemler;
    }

    private async Task<List<MuhasebeKalemOnizleme>> OlusturGiderFaturaKalemleri(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeAyar ayar)
    {
        var kalemler = new List<MuhasebeKalemOnizleme>();
        var siraNo = 1;

        // Cari hesap bilgisi al
        var cariHesapKodu = "320.01"; // Varsayılan satıcılar
        var cariUnvan = giris.CariUnvan ?? "Bilinmeyen Tedarikçi";

        if (giris.CariId.HasValue)
        {
            var cari = await context.Cariler.AsNoTracking().Include(c => c.MuhasebeHesap).OrderBy(c => c.Id).FirstOrDefaultAsync(c => c.Id == giris.CariId);
            if (cari != null)
            {
                cariUnvan = cari.Unvan;
                if (cari.MuhasebeHesap != null)
                    cariHesapKodu = cari.MuhasebeHesap.HesapKodu;
            }
        }

        var saticiyaOdenecek = giris.TevkifatliMi ? giris.GenelToplam - giris.TevkifatTutar : giris.GenelToplam;

        // 770 Genel Yönetim Giderleri / 153 Ticari Mallar BORÇ
        var giderHesabi = ayar.AlisGiderHesabi;
        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = giderHesabi,
            HesapAdi = "Gider/Mal Alışı",
            HesapId = await GetHesapIdAsync(context, giderHesabi),
            Borc = giris.AraToplam,
            Alacak = 0,
            Aciklama = giris.Aciklama ?? "Alış"
        });

        // 191 İndirilecek KDV BORÇ
        if (giris.KdvTutar > 0)
        {
            var eslestirme = ayar.KdvHesapEslestirmeleri.FirstOrDefault(e => e.KdvOrani == (int)giris.KdvOrani);
            var kdvHesapKodu = eslestirme?.IndirilecekKdvHesabi ?? ayar.IndirilecekKdvHesabi;
            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = kdvHesapKodu,
                HesapAdi = "İndirilecek KDV",
                HesapId = await GetHesapIdAsync(context, kdvHesapKodu),
                Borc = giris.KdvTutar,
                Alacak = 0,
                Aciklama = $"KDV %{giris.KdvOrani}"
            });
        }

        // 320 Satıcılar ALACAK
        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = cariHesapKodu,
            HesapAdi = $"Satıcılar - {cariUnvan}",
            HesapId = await GetHesapIdAsync(context, cariHesapKodu),
            Borc = 0,
            Alacak = saticiyaOdenecek,
            Aciklama = $"Fatura: {giris.BelgeNo}",
            CariId = giris.CariId,
            CariUnvan = cariUnvan
        });

        // Tevkifat varsa - 360 Sorumlu Sıfatıyla Ödenen KDV ALACAK
        if (giris.TevkifatliMi && giris.TevkifatTutar > 0)
        {
            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = ayar.TevkifatKdvHesabi,
                HesapAdi = "Sorumlu Sıfatıyla Ödenen KDV",
                HesapId = await GetHesapIdAsync(context, ayar.TevkifatKdvHesabi),
                Borc = 0,
                Alacak = giris.TevkifatTutar,
                Aciklama = $"Tevkifat KDV ({giris.TevkifatKodu})"
            });
        }

        return kalemler;
    }

    private async Task<List<MuhasebeKalemOnizleme>> OlusturMasrafKalemleri(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeAyar ayar)
    {
        var kalemler = new List<MuhasebeKalemOnizleme>();
        var siraNo = 1;

        // Masraf kalemi hesap kodu
        var masrafHesapKodu = "770.01"; // Varsayılan genel yönetim giderleri
        var masrafAdi = "Genel Masraf";

        if (giris.MasrafKalemiId.HasValue)
        {
            var masrafKalemi = await context.MasrafKalemleri.AsNoTracking()
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync(m => m.Id == giris.MasrafKalemiId);
            if (masrafKalemi != null)
            {
                masrafAdi = masrafKalemi.MasrafAdi;
                // MasrafKalemi'nde MuhasebeHesapKodu yok, kategori bazlı eşleme yapalım
                masrafHesapKodu = GetMasrafHesapKodu(masrafKalemi.Kategori);
            }
        }

        // 7xx Gider Hesabı BORÇ
        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = masrafHesapKodu,
            HesapAdi = masrafAdi,
            HesapId = await GetHesapIdAsync(context, masrafHesapKodu),
            Borc = giris.AraToplam,
            Alacak = 0,
            Aciklama = giris.Aciklama ?? masrafAdi
        });

        // 191 İndirilecek KDV BORÇ (varsa)
        if (giris.KdvTutar > 0)
        {
            var eslestirme = ayar.KdvHesapEslestirmeleri.FirstOrDefault(e => e.KdvOrani == (int)giris.KdvOrani);
            var kdvHesapKodu = eslestirme?.IndirilecekKdvHesabi ?? ayar.IndirilecekKdvHesabi;
            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = kdvHesapKodu,
                HesapAdi = "İndirilecek KDV",
                HesapId = await GetHesapIdAsync(context, kdvHesapKodu),
                Borc = giris.KdvTutar,
                Alacak = 0,
                Aciklama = $"KDV %{giris.KdvOrani}"
            });
        }

        var (odemeHesapKodu, odemeHesapAdi, odemeCariId, odemeCariUnvan) = await GetMasrafOdemeHesabiAsync(context, giris);

        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = odemeHesapKodu,
            HesapAdi = odemeHesapAdi,
            HesapId = await GetHesapIdAsync(context, odemeHesapKodu),
            Borc = 0,
            Alacak = giris.GenelToplam,
            Aciklama = $"Masraf ödemesi: {giris.BelgeNo}",
            CariId = odemeCariId,
            CariUnvan = odemeCariUnvan
        });

        return kalemler;
    }

    private async Task<List<MuhasebeKalemOnizleme>> OlusturTahsilatKalemleri(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeAyar ayar)
    {
        var kalemler = new List<MuhasebeKalemOnizleme>();
        var siraNo = 1;

        // Banka/Kasa hesabı BORÇ
        var bankaHesapKodu = "100.01"; // Varsayılan kasa
        var bankaHesapAdi = "Kasa";

        if (giris.BankaHesapId.HasValue)
        {
            var bankaHesap = await context.BankaHesaplari.AsNoTracking()
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(b => b.Id == giris.BankaHesapId);
            if (bankaHesap != null)
            {
                bankaHesapAdi = bankaHesap.HesapAdi;
                bankaHesapKodu = bankaHesap.VarsayilanMuhasebeKodu ?? GetBankaHesapKodu(bankaHesap.HesapTipi);
            }
        }

        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = bankaHesapKodu,
            HesapAdi = bankaHesapAdi,
            HesapId = await GetHesapIdAsync(context, bankaHesapKodu),
            Borc = giris.GenelToplam,
            Alacak = 0,
            Aciklama = $"Tahsilat: {giris.BelgeNo}"
        });

        // 120 Alıcılar ALACAK
        var cariHesapKodu = "120.01";
        var cariUnvan = giris.CariUnvan ?? "Müşteri";

        if (giris.CariId.HasValue)
        {
            var cari = await context.Cariler.AsNoTracking().Include(c => c.MuhasebeHesap).OrderBy(c => c.Id).FirstOrDefaultAsync(c => c.Id == giris.CariId);
            if (cari != null)
            {
                cariUnvan = cari.Unvan;
                if (cari.MuhasebeHesap != null)
                    cariHesapKodu = cari.MuhasebeHesap.HesapKodu;
            }
        }

        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = cariHesapKodu,
            HesapAdi = $"Alıcılar - {cariUnvan}",
            HesapId = await GetHesapIdAsync(context, cariHesapKodu),
            Borc = 0,
            Alacak = giris.GenelToplam,
            Aciklama = $"Tahsilat: {giris.BelgeNo}",
            CariId = giris.CariId,
            CariUnvan = cariUnvan
        });

        return kalemler;
    }

    private async Task<List<MuhasebeKalemOnizleme>> OlusturOdemeKalemleri(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeAyar ayar)
    {
        var kalemler = new List<MuhasebeKalemOnizleme>();
        var siraNo = 1;

        // 320 Satıcılar BORÇ
        var cariHesapKodu = "320.01";
        var cariUnvan = giris.CariUnvan ?? "Tedarikçi";

        if (giris.CariId.HasValue)
        {
            var cari = await context.Cariler.AsNoTracking().Include(c => c.MuhasebeHesap).OrderBy(c => c.Id).FirstOrDefaultAsync(c => c.Id == giris.CariId);
            if (cari != null)
            {
                cariUnvan = cari.Unvan;
                if (cari.MuhasebeHesap != null)
                    cariHesapKodu = cari.MuhasebeHesap.HesapKodu;
            }
        }

        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = cariHesapKodu,
            HesapAdi = $"Satıcılar - {cariUnvan}",
            HesapId = await GetHesapIdAsync(context, cariHesapKodu),
            Borc = giris.GenelToplam,
            Alacak = 0,
            Aciklama = $"Ödeme: {giris.BelgeNo}",
            CariId = giris.CariId,
            CariUnvan = cariUnvan
        });

        // Banka/Kasa hesabı ALACAK
        var bankaHesapKodu = "100.01";
        var bankaHesapAdi = "Kasa";

        if (giris.BankaHesapId.HasValue)
        {
            var bankaHesap = await context.BankaHesaplari.AsNoTracking()
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(b => b.Id == giris.BankaHesapId);
            if (bankaHesap != null)
            {
                bankaHesapAdi = bankaHesap.HesapAdi;
                bankaHesapKodu = bankaHesap.VarsayilanMuhasebeKodu ?? GetBankaHesapKodu(bankaHesap.HesapTipi);
            }
        }

        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = bankaHesapKodu,
            HesapAdi = bankaHesapAdi,
            HesapId = await GetHesapIdAsync(context, bankaHesapKodu),
            Borc = 0,
            Alacak = giris.GenelToplam,
            Aciklama = $"Ödeme: {giris.BelgeNo}"
        });

        return kalemler;
    }

    private async Task<List<MuhasebeKalemOnizleme>> OlusturMahsupKalemleri(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeAyar ayar)
    {
        var kalemler = new List<MuhasebeKalemOnizleme>();
        var siraNo = 1;

        // Cari bilgisini al
        var cariHesapKodu = "120.01"; // Varsayılan alıcılar veya satıcılar
        var cariUnvan = giris.CariUnvan ?? "Cari";
        CariTipi? cariTipi = null;

        if (giris.CariId.HasValue)
        {
            var cari = await context.Cariler.AsNoTracking().Include(c => c.MuhasebeHesap).OrderBy(c => c.Id).FirstOrDefaultAsync(c => c.Id == giris.CariId);
            if (cari != null)
            {
                cariUnvan = cari.Unvan;
                cariTipi = cari.CariTipi;
                if (cari.MuhasebeHesap != null)
                    cariHesapKodu = cari.MuhasebeHesap.HesapKodu;
                else
                {
                    // Cari tipine göre varsayılan hesap
                    cariHesapKodu = cari.CariTipi switch
                    {
                        CariTipi.Musteri => "120.01", // Alıcılar
                        CariTipi.Tedarikci => "320.01", // Satıcılar
                        CariTipi.MusteriTedarikci => "120.01", // Varsayılan alıcı
                        CariTipi.Personel => "335.01", // Personele Borçlar
                        _ => "120.01"
                    };
                }
            }
        }

        // Banka/Kasa hesabı bilgisini al
        var bankaHesapKodu = "100.01";
        var bankaHesapAdi = "Kasa";

        if (giris.BankaHesapId.HasValue)
        {
            var bankaHesap = await context.BankaHesaplari.AsNoTracking()
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(b => b.Id == giris.BankaHesapId);
            if (bankaHesap != null)
            {
                bankaHesapAdi = bankaHesap.HesapAdi;
                bankaHesapKodu = bankaHesap.VarsayilanMuhasebeKodu ?? GetBankaHesapKodu(bankaHesap.HesapTipi);
            }
        }

        // Mahsup işlemi:
        // Müşteri ise: Müşteriden tahsilat yapılıyor (Alıcılar azalıyor)
        // Tedarikçi ise: Tedarikçiye ödeme yapılıyor (Satıcılar azalıyor)
        bool musteriMahsup = cariTipi == CariTipi.Musteri || cariTipi == CariTipi.MusteriTedarikci;

        if (musteriMahsup)
        {
            // Müşteri mahsubu: Banka/Kasa BORÇ, Alıcılar ALACAK
            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = bankaHesapKodu,
                HesapAdi = bankaHesapAdi,
                HesapId = await GetHesapIdAsync(context, bankaHesapKodu),
                Borc = giris.GenelToplam,
                Alacak = 0,
                Aciklama = $"Mahsup tahsilat: {giris.BelgeNo}"
            });

            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = cariHesapKodu,
                HesapAdi = $"Alıcılar - {cariUnvan}",
                HesapId = await GetHesapIdAsync(context, cariHesapKodu),
                Borc = 0,
                Alacak = giris.GenelToplam,
                Aciklama = $"Mahsup: {giris.BelgeNo}",
                CariId = giris.CariId,
                CariUnvan = cariUnvan
            });
        }
        else
        {
            // Tedarikçi/Personel mahsubu: Satıcılar BORÇ, Banka/Kasa ALACAK
            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = cariHesapKodu,
                HesapAdi = $"Satıcılar - {cariUnvan}",
                HesapId = await GetHesapIdAsync(context, cariHesapKodu),
                Borc = giris.GenelToplam,
                Alacak = 0,
                Aciklama = $"Mahsup ödeme: {giris.BelgeNo}",
                CariId = giris.CariId,
                CariUnvan = cariUnvan
            });

            kalemler.Add(new MuhasebeKalemOnizleme
            {
                SiraNo = siraNo++,
                HesapKodu = bankaHesapKodu,
                HesapAdi = bankaHesapAdi,
                HesapId = await GetHesapIdAsync(context, bankaHesapKodu),
                Borc = 0,
                Alacak = giris.GenelToplam,
                Aciklama = $"Mahsup: {giris.BelgeNo}"
            });
        }

        return kalemler;
    }

    private async Task<List<MuhasebeKalemOnizleme>> OlusturAvansKalemleri(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeAyar ayar)
    {
        var kalemler = new List<MuhasebeKalemOnizleme>();
        var siraNo = 1;

        // 195 İş Avansları BORÇ — personelin özgü hesabı varsa onu kullan
        string avansHesapKodu = "195.01";
        string avansHesapAdi = "Personel Avansları";
        int? avansHesapId = null;

        if (giris.PersonelAvansHesapId.HasValue)
        {
            var ozguHesap = await context.MuhasebeHesaplari.AsNoTracking()
                .OrderBy(h => h.Id)
                .FirstOrDefaultAsync(h => h.Id == giris.PersonelAvansHesapId.Value && !h.IsDeleted);
            if (ozguHesap != null)
            {
                avansHesapKodu = ozguHesap.HesapKodu;
                avansHesapAdi = ozguHesap.HesapAdi;
                avansHesapId = ozguHesap.Id;
            }
        }

        if (avansHesapId == null)
        {
            avansHesapId = await GetHesapIdAsync(context, avansHesapKodu);
        }

        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = avansHesapKodu,
            HesapAdi = avansHesapAdi,
            HesapId = avansHesapId,
            Borc = giris.GenelToplam,
            Alacak = 0,
            Aciklama = giris.Aciklama ?? "Personel avansı",
            CariId = giris.CariId,
            CariUnvan = giris.CariUnvan
        });

        // Banka/Kasa ALACAK
        var bankaHesapKodu = "100.01";
        var bankaHesapAdi = "Kasa";

        if (giris.BankaHesapId.HasValue)
        {
            var bankaHesap = await context.BankaHesaplari.AsNoTracking()
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(b => b.Id == giris.BankaHesapId);
            if (bankaHesap != null)
            {
                bankaHesapAdi = bankaHesap.HesapAdi;
                bankaHesapKodu = bankaHesap.VarsayilanMuhasebeKodu ?? GetBankaHesapKodu(bankaHesap.HesapTipi);
            }
        }

        kalemler.Add(new MuhasebeKalemOnizleme
        {
            SiraNo = siraNo++,
            HesapKodu = bankaHesapKodu,
            HesapAdi = bankaHesapAdi,
            HesapId = await GetHesapIdAsync(context, bankaHesapKodu),
            Borc = 0,
            Alacak = giris.GenelToplam,
            Aciklama = "Avans ödemesi"
        });

        return kalemler;
    }

    #endregion

    #region Kaydetme

    public async Task<KolayMuhasebeSonuc> KaydetAsync(KolayMuhasebeGiris giris, MuhasebeOnizleme? manuelOnizleme = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuc = new KolayMuhasebeSonuc();

        try
        {
            // Yeni cari oluştur (gerekirse)
            if (!giris.CariId.HasValue && !string.IsNullOrWhiteSpace(giris.CariUnvan) && giris.YeniCariTipi.HasValue)
            {
                var yeniCari = await HizliCariOlusturAsync(giris.CariUnvan, giris.YeniCariTipi.Value);
                giris.CariId = yeniCari.Id;
                sonuc.CariId = yeniCari.Id;
                sonuc.Uyarilar.Add($"Yeni cari oluşturuldu: {yeniCari.Unvan}");
            }

            if (CariMuhasebeEslemeGerekliMi(giris) && giris.CariId.HasValue)
            {
                await _cariService.EnsureMuhasebeHesapAsync(giris.CariId.Value);
            }

            // Önizleme oluştur (verilmemişse)
            var onizleme = await OnizlemeOlusturAsync(giris);

            // Dengeli mi kontrol et
            if (!onizleme.Dengeli)
            {
                sonuc.Basarili = false;
                sonuc.Mesaj = $"Muhasebe kaydı dengeli değil! Borç: {onizleme.ToplamBorc:N2}, Alacak: {onizleme.ToplamAlacak:N2}";
                return sonuc;
            }

            // İşlem türüne göre kayıtları oluştur
            switch (giris.IslemTuru)
            {
                case KolayIslemTuru.GelirFatura:
                    sonuc = await KaydetGelirFatura(context, giris, onizleme);
                    break;
                case KolayIslemTuru.GiderFatura:
                    sonuc = await KaydetGiderFatura(context, giris, onizleme);
                    break;
                case KolayIslemTuru.MasrafGirisi:
                    sonuc = await KaydetMasraf(context, giris, onizleme);
                    break;
                case KolayIslemTuru.TahsilatGirisi:
                    sonuc = await KaydetTahsilat(context, giris, onizleme);
                    break;
                case KolayIslemTuru.OdemeGirisi:
                    sonuc = await KaydetOdeme(context, giris, onizleme);
                    break;
                case KolayIslemTuru.AvansGirisi:
                    sonuc = await KaydetAvans(context, giris, onizleme);
                    break;
                case KolayIslemTuru.MahsupKaydi:
                    sonuc = await KaydetMahsup(context, giris, onizleme);
                    break;
                default:
                    // Sadece muhasebe fişi oluştur
                    sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme);
                    sonuc.Basarili = true;
                    sonuc.Mesaj = "Muhasebe fişi oluşturuldu.";
                    break;
            }
        }
        catch (Exception ex)
        {
            sonuc.Basarili = false;
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { msg += $" → {inner.Message}"; inner = inner.InnerException; }
            sonuc.Mesaj = $"Hata: {msg}";
        }

        return sonuc;
    }

    private async Task<KolayMuhasebeSonuc> KaydetGelirFatura(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeOnizleme onizleme)
    {
        var sonuc = new KolayMuhasebeSonuc();

        // Fatura oluştur
        var fatura = new Fatura
        {
            FaturaNo = giris.BelgeNo ?? await GenerateFaturaNo(context, "SF"),
            FaturaTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
            VadeTarihi = giris.VadeTarihi.HasValue ? DateTime.SpecifyKind(giris.VadeTarihi.Value, DateTimeKind.Utc) : null,
            FaturaTipi = FaturaTipi.SatisFaturasi,
            FaturaYonu = FaturaYonu.Giden,
            EFaturaTipi = EFaturaTipi.EArsiv,
            CariId = giris.CariId ?? 0,
            AraToplam = giris.AraToplam,
            KdvOrani = giris.KdvOrani,
            KdvTutar = giris.KdvTutar,
            GenelToplam = giris.GenelToplam,
            TevkifatliMi = giris.TevkifatliMi,
            TevkifatOrani = giris.TevkifatOrani,
            TevkifatKodu = giris.TevkifatKodu,
            TevkifatTutar = giris.TevkifatTutar,
            Aciklama = giris.Aciklama,
            Notlar = giris.Notlar,
            Durum = FaturaDurum.Beklemede,
            ImportKaynak = "KolayGiris",
            CreatedAt = DateTime.UtcNow
        };

        context.Faturalar.Add(fatura);
        await context.SaveChangesAsync();
        sonuc.FaturaId = fatura.Id;

        // Muhasebe fişi oluştur
        sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.Fatura, fatura.Id);

        // Faturayı güncelle
        fatura.MuhasebeFisiOlusturuldu = true;
        fatura.MuhasebeFisId = sonuc.MuhasebeFisId;
        context.Faturalar.Update(fatura);
        await context.SaveChangesAsync();

        // Stok kalemleri varsa stok çıkış hareketi oluştur
        foreach (var kalem in giris.Kalemler.Where(k => k.StokId.HasValue && k.Miktar > 0))
        {
            try
            {
                context.StokHareketler.Add(new StokHareket
                {
                    StokKartiId = kalem.StokId!.Value,
                    IslemTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
                    HareketTipi = StokHareketTipi.Cikis,
                    Miktar = kalem.Miktar,
                    BirimFiyat = kalem.BirimFiyat,
                    BelgeNo = fatura.FaturaNo,
                    Aciklama = kalem.Aciklama ?? giris.Aciklama,
                    CariId = giris.CariId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch { }
        }
        if (giris.Kalemler.Any(k => k.StokId.HasValue))
            await context.SaveChangesAsync();

        sonuc.Basarili = true;
        sonuc.Mesaj = $"Gelir faturası ve muhasebe kaydı oluşturuldu. Fatura No: {fatura.FaturaNo}";
        return sonuc;
    }

    private async Task<KolayMuhasebeSonuc> KaydetGiderFatura(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeOnizleme onizleme)
    {
        var sonuc = new KolayMuhasebeSonuc();

        // Fatura oluştur
        var fatura = new Fatura
        {
            FaturaNo = giris.BelgeNo ?? await GenerateFaturaNo(context, "AF"),
            FaturaTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
            VadeTarihi = giris.VadeTarihi.HasValue ? DateTime.SpecifyKind(giris.VadeTarihi.Value, DateTimeKind.Utc) : null,
            FaturaTipi = FaturaTipi.AlisFaturasi,
            FaturaYonu = FaturaYonu.Gelen,
            EFaturaTipi = EFaturaTipi.EFatura,
            CariId = giris.CariId ?? 0,
            AraToplam = giris.AraToplam,
            KdvOrani = giris.KdvOrani,
            KdvTutar = giris.KdvTutar,
            GenelToplam = giris.GenelToplam,
            TevkifatliMi = giris.TevkifatliMi,
            TevkifatOrani = giris.TevkifatOrani,
            TevkifatKodu = giris.TevkifatKodu,
            TevkifatTutar = giris.TevkifatTutar,
            Aciklama = giris.Aciklama,
            Notlar = giris.Notlar,
            Durum = FaturaDurum.Beklemede,
            ImportKaynak = "KolayGiris",
            CreatedAt = DateTime.UtcNow
        };

        context.Faturalar.Add(fatura);
        await context.SaveChangesAsync();
        sonuc.FaturaId = fatura.Id;

        // Muhasebe fişi oluştur
        sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.Fatura, fatura.Id);

        // Faturayı güncelle
        fatura.MuhasebeFisiOlusturuldu = true;
        fatura.MuhasebeFisId = sonuc.MuhasebeFisId;
        context.Faturalar.Update(fatura);
        await context.SaveChangesAsync();

        // Stok kalemleri varsa stok giriş hareketi oluştur
        foreach (var kalem in giris.Kalemler.Where(k => k.StokId.HasValue && k.Miktar > 0))
        {
            try
            {
                context.StokHareketler.Add(new StokHareket
                {
                    StokKartiId = kalem.StokId!.Value,
                    IslemTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
                    HareketTipi = StokHareketTipi.Giris,
                    Miktar = kalem.Miktar,
                    BirimFiyat = kalem.BirimFiyat,
                    BelgeNo = fatura.FaturaNo,
                    Aciklama = kalem.Aciklama ?? giris.Aciklama,
                    CariId = giris.CariId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch { }
        }
        if (giris.Kalemler.Any(k => k.StokId.HasValue))
            await context.SaveChangesAsync();

        sonuc.Basarili = true;
        sonuc.Mesaj = $"Gider faturası ve muhasebe kaydı oluşturuldu. Fatura No: {fatura.FaturaNo}";
        return sonuc;
    }

    private async Task<KolayMuhasebeSonuc> KaydetMasraf(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeOnizleme onizleme)
    {
        var sonuc = new KolayMuhasebeSonuc();

        // Araç masrafı mı?
        if (giris.AracId.HasValue && giris.MasrafKalemiId.HasValue)
        {
            var masraf = new AracMasraf
            {
                MasrafTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
                AracId = giris.AracId.Value,
                MasrafKalemiId = giris.MasrafKalemiId.Value,
                Tutar = giris.GenelToplam,
                BelgeNo = giris.BelgeNo,
                Aciklama = giris.Aciklama,
                CariId = giris.CariId,
                CreatedAt = DateTime.UtcNow
            };

            context.AracMasraflari.Add(masraf);
            await context.SaveChangesAsync();
            sonuc.MasrafId = masraf.Id;

            // Muhasebe fişi
            sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.Otomatik, masraf.Id, "AracMasraf");
            masraf.MuhasebeFisId = sonuc.MuhasebeFisId;
            context.AracMasraflari.Update(masraf);
            await context.SaveChangesAsync();
        }
        else
        {
            // Genel masraf - sadece muhasebe fişi
            sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.Manuel, null, "Masraf");
        }

        // Banka hareketi oluştur (ödeme yapıldıysa)
        if (giris.MasrafOdemeKaynagi == MasrafOdemeKaynagi.KasaBanka && giris.BankaHesapId.HasValue)
        {
            var hareket = new BankaKasaHareket
            {
                BankaHesapId = giris.BankaHesapId.Value,
                IslemNo = await GenerateIslemNo(context),
                IslemTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
                Tutar = giris.GenelToplam,
                HareketTipi = HareketTipi.Cikis,
                Aciklama = $"Masraf: {giris.Aciklama ?? giris.BelgeNo}",
                CariId = giris.CariId,
                IslemKaynak = IslemKaynak.Manuel,
                CreatedAt = DateTime.UtcNow
            };

            context.BankaKasaHareketleri.Add(hareket);
            await context.SaveChangesAsync();
            sonuc.BankaHareketId = hareket.Id;

            // Muhasebe fişi ile geri bağlantı - Masraf banka hareketi
            if (sonuc.MuhasebeFisId.HasValue)
            {
                hareket.MuhasebeFisId = sonuc.MuhasebeFisId;
                context.BankaKasaHareketleri.Update(hareket);
                await context.SaveChangesAsync();
            }
        }

        // Stok kalemleri varsa stok hareketleri oluştur
        foreach (var kalem in giris.Kalemler.Where(k => k.StokId.HasValue && k.Miktar > 0))
        {
            try
            {
                context.StokHareketler.Add(new StokHareket
                {
                    StokKartiId = kalem.StokId!.Value,
                    IslemTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
                    HareketTipi = StokHareketTipi.Cikis,
                    Miktar = kalem.Miktar,
                    BirimFiyat = kalem.BirimFiyat,
                    BelgeNo = giris.BelgeNo,
                    Aciklama = kalem.Aciklama ?? giris.Aciklama,
                    CariId = giris.CariId,
                    AracMasrafId = sonuc.MasrafId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch { }
        }
        if (giris.Kalemler.Any(k => k.StokId.HasValue))
            await context.SaveChangesAsync();

        sonuc.Basarili = true;
        sonuc.Mesaj = giris.MasrafOdemeKaynagi switch
        {
            MasrafOdemeKaynagi.Personel => "Masraf personel alacağı olarak muhasebeleştirildi ve personel borçlarına eklendi.",
            MasrafOdemeKaynagi.Cari => "Masraf cari alacağı olarak muhasebeleştirildi.",
            _ => "Masraf ve muhasebe kaydı oluşturuldu."
        };
        return sonuc;
    }

    private async Task<KolayMuhasebeSonuc> KaydetTahsilat(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeOnizleme onizleme)
    {
        var sonuc = new KolayMuhasebeSonuc();

        if (!giris.BankaHesapId.HasValue)
        {
            sonuc.Basarili = false;
            sonuc.Mesaj = "Tahsilat için banka/kasa hesabı seçilmeli.";
            return sonuc;
        }

        // Banka hareketi oluştur
        var hareket = new BankaKasaHareket
        {
            BankaHesapId = giris.BankaHesapId.Value,
            IslemNo = await GenerateIslemNo(context),
            IslemTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
            Tutar = giris.GenelToplam,
            HareketTipi = HareketTipi.Giris,
            Aciklama = $"Tahsilat: {giris.CariUnvan} - {giris.BelgeNo}",
            CariId = giris.CariId,
            IslemKaynak = IslemKaynak.FaturaTahsilat,
            CreatedAt = DateTime.UtcNow
        };

        context.BankaKasaHareketleri.Add(hareket);
        await context.SaveChangesAsync();
        sonuc.BankaHareketId = hareket.Id;

        // Muhasebe fişi
        sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.BankaHareket, hareket.Id);

        // Muhasebe fişi ile geri bağlantı
        hareket.MuhasebeFisId = sonuc.MuhasebeFisId;
        context.BankaKasaHareketleri.Update(hareket);
        await context.SaveChangesAsync();

        sonuc.Basarili = true;
        sonuc.Mesaj = $"Tahsilat kaydedildi. Tutar: {giris.GenelToplam:N2} TL";
        return sonuc;
    }

    private async Task<KolayMuhasebeSonuc> KaydetOdeme(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeOnizleme onizleme)
    {
        var sonuc = new KolayMuhasebeSonuc();

        if (!giris.BankaHesapId.HasValue)
        {
            sonuc.Basarili = false;
            sonuc.Mesaj = "Ödeme için banka/kasa hesabı seçilmeli.";
            return sonuc;
        }

        // Banka hareketi oluştur
        var hareket = new BankaKasaHareket
        {
            BankaHesapId = giris.BankaHesapId.Value,
            IslemNo = await GenerateIslemNo(context),
            IslemTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
            Tutar = giris.GenelToplam,
            HareketTipi = HareketTipi.Cikis,
            Aciklama = $"Ödeme: {giris.CariUnvan} - {giris.BelgeNo}",
            CariId = giris.CariId,
            IslemKaynak = IslemKaynak.FaturaOdeme,
            CreatedAt = DateTime.UtcNow
        };

        context.BankaKasaHareketleri.Add(hareket);
        await context.SaveChangesAsync();
        sonuc.BankaHareketId = hareket.Id;

        // Muhasebe fişi
        sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.BankaHareket, hareket.Id);

        // Muhasebe fişi ile geri bağlantı
        hareket.MuhasebeFisId = sonuc.MuhasebeFisId;
        context.BankaKasaHareketleri.Update(hareket);
        await context.SaveChangesAsync();

        sonuc.Basarili = true;
        sonuc.Mesaj = $"Ödeme kaydedildi. Tutar: {giris.GenelToplam:N2} TL";
        return sonuc;
    }

    private async Task<KolayMuhasebeSonuc> KaydetAvans(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeOnizleme onizleme)
    {
        var sonuc = new KolayMuhasebeSonuc();

        if (!giris.BankaHesapId.HasValue)
        {
            sonuc.Basarili = false;
            sonuc.Mesaj = "Avans için banka/kasa hesabı seçilmeli.";
            return sonuc;
        }

        // Banka hareketi oluştur
        var hareket = new BankaKasaHareket
        {
            BankaHesapId = giris.BankaHesapId.Value,
            IslemNo = await GenerateIslemNo(context),
            IslemTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
            Tutar = giris.GenelToplam,
            HareketTipi = HareketTipi.Cikis,
            Aciklama = $"Personel Avansı: {giris.CariUnvan}",
            CariId = giris.CariId,
            IslemKaynak = IslemKaynak.Manuel,
            CreatedAt = DateTime.UtcNow
        };

        context.BankaKasaHareketleri.Add(hareket);
        await context.SaveChangesAsync();
        sonuc.BankaHareketId = hareket.Id;

        // Muhasebe fişi
        sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.BankaHareket, hareket.Id);

        // Muhasebe fişi ile geri bağlantı
        hareket.MuhasebeFisId = sonuc.MuhasebeFisId;
        context.BankaKasaHareketleri.Update(hareket);
        await context.SaveChangesAsync();

        // Personel seçilmişse PersonelAvans kaydı oluştur
        if (giris.PersonelId.HasValue && giris.PersonelId.Value > 0)
        {
            var bankaHesap = await context.BankaHesaplari.AsNoTracking()
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(b => b.Id == giris.BankaHesapId);
            var odemeSekli = bankaHesap?.HesapTipi == HesapTipi.Kasa
                ? AvansOdemeSekli.Nakit
                : AvansOdemeSekli.BankaTransfer;

            var personelAvans = new PersonelAvans
            {
                PersonelId = giris.PersonelId.Value,
                AvansTarihi = DateTime.SpecifyKind(giris.IslemTarihi.Date, DateTimeKind.Utc),
                Tutar = giris.GenelToplam,
                Aciklama = giris.Aciklama ?? "Kolay giriş - Avans ödemesi",
                OdemeSekli = odemeSekli,
                BankaHesapId = giris.BankaHesapId,
                MuhasebeFisId = sonuc.MuhasebeFisId,
                Durum = AvansDurum.Verildi,
                MahsupEdilen = 0,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<PersonelAvans>().Add(personelAvans);
            await context.SaveChangesAsync();
            sonuc.Mesaj = $"Avans kaydedildi ve personel hesabına eklendi. Tutar: {giris.GenelToplam:N2} TL";
        }
        else
        {
            sonuc.Mesaj = $"Avans kaydedildi. Tutar: {giris.GenelToplam:N2} TL";
        }

        sonuc.Basarili = true;
        return sonuc;
    }

    private async Task<KolayMuhasebeSonuc> KaydetMahsup(ApplicationDbContext context, KolayMuhasebeGiris giris, MuhasebeOnizleme onizleme)
    {
        var sonuc = new KolayMuhasebeSonuc();

        if (!giris.BankaHesapId.HasValue)
        {
            // Banka/kasa yoksa sadece muhasebe fişi
            sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.Manuel, null, "Mahsup");
            sonuc.Basarili = true;
            sonuc.Mesaj = $"Mahsup kaydedildi. Tutar: {giris.GenelToplam:N2} TL";
            return sonuc;
        }

        // Cari tipine göre banka hareketi yönünü belirle
        HareketTipi hareketTipi = HareketTipi.Cikis;
        if (giris.CariId.HasValue)
        {
            var cari = await context.Cariler.AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(c => c.Id == giris.CariId.Value);
            hareketTipi = (cari?.CariTipi == CariTipi.Musteri || cari?.CariTipi == CariTipi.MusteriTedarikci)
                ? HareketTipi.Giris   // Müşteriden mahsup tahsilat
                : HareketTipi.Cikis;  // Tedarikçi/Personele mahsup ödeme
        }

        var hareket = new BankaKasaHareket
        {
            BankaHesapId = giris.BankaHesapId.Value,
            IslemNo = await GenerateIslemNo(context),
            IslemTarihi = DateTime.SpecifyKind(giris.IslemTarihi, DateTimeKind.Utc),
            Tutar = giris.GenelToplam,
            HareketTipi = hareketTipi,
            Aciklama = $"Mahsup: {giris.CariUnvan} - {giris.BelgeNo}",
            CariId = giris.CariId,
            IslemKaynak = IslemKaynak.CariMahsup,
            CreatedAt = DateTime.UtcNow
        };

        context.BankaKasaHareketleri.Add(hareket);
        await context.SaveChangesAsync();
        sonuc.BankaHareketId = hareket.Id;

        sonuc.MuhasebeFisId = await KaydetMuhasebeFisi(context, onizleme, FisKaynak.BankaHareket, hareket.Id);

        // Muhasebe fişi ile geri bağlantı
        hareket.MuhasebeFisId = sonuc.MuhasebeFisId;
        context.BankaKasaHareketleri.Update(hareket);
        await context.SaveChangesAsync();

        sonuc.Basarili = true;
        sonuc.Mesaj = $"Mahsup kaydedildi. Tutar: {giris.GenelToplam:N2} TL";
        return sonuc;
    }

    private async Task<int> KaydetMuhasebeFisi(ApplicationDbContext context, MuhasebeOnizleme onizleme, FisKaynak kaynak = FisKaynak.Manuel, int? kaynakId = null, string? kaynakTip = null)
    {
        // FisNo preview anında değil, kayıt anında üretilir (stale FisNo → duplicate key sorununu önler).
        var fis = new MuhasebeFis
        {
            FisNo = string.Empty,
            FisTarihi = DateTime.SpecifyKind(onizleme.FisTarihi, DateTimeKind.Utc),
            FisTipi = onizleme.FisTipi,
            Aciklama = onizleme.Aciklama,
            ToplamBorc = onizleme.ToplamBorc,
            ToplamAlacak = onizleme.ToplamAlacak,
            Durum = FisDurum.Onaylandi,
            Kaynak = kaynak,
            KaynakId = kaynakId,
            KaynakTip = kaynakTip ?? GetKaynakTip(kaynak),
            CreatedAt = DateTime.UtcNow
        };

        // Atomik olarak FisNo üret + fişi kaydet (SemaphoreSlim koruması altında)
        var savedFis = await _muhasebeService.CreateFisAtomicAsync(fis);

        // Kalemleri mevcut context üzerinden ekle (FK ile bağlı)
        foreach (var kalem in onizleme.Kalemler)
        {
            if (!kalem.HesapId.HasValue || kalem.HesapId == 0)
                throw new InvalidOperationException($"Muhasebe kalemi '{kalem.HesapKodu} - {kalem.HesapAdi}' için hesap ID bulunamadı. Lütfen muhasebe hesaplarının tanımlı olduğunu kontrol edin.");

            var fisKalem = new MuhasebeFisKalem
            {
                FisId = savedFis.Id,
                HesapId = kalem.HesapId.Value,
                SiraNo = kalem.SiraNo,
                Borc = kalem.Borc,
                Alacak = kalem.Alacak,
                Aciklama = kalem.Aciklama,
                CariId = kalem.CariId,
                Tarih = DateTime.SpecifyKind(onizleme.FisTarihi, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow
            };

            context.MuhasebeFisKalemleri.Add(fisKalem);
        }

        await context.SaveChangesAsync();
        return savedFis.Id;
    }

    #endregion

    #region Yardımcı Metodlar

    public async Task<List<Cari>> GetCarilerAsync(CariTipi? tip = null, string? arama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (tip == CariTipi.Personel)
        {
            await EnsurePersonelCariKayitlariAsync(context, arama);
        }

        var query = context.Cariler.AsNoTracking().Where(c => c.Aktif);

        if (tip.HasValue)
        {
            // Müşteri seçilmişse: Müşteri ve MüşteriTedarikçi
            // Tedarikçi seçilmişse: Tedarikçi ve MüşteriTedarikçi
            // Personel seçilmişse: Sadece Personel
            if (tip.Value == CariTipi.Personel)
            {
                query = query.Where(c => c.CariTipi == CariTipi.Personel);
            }
            else
            {
                query = query.Where(c => c.CariTipi == tip.Value || c.CariTipi == CariTipi.MusteriTedarikci);
            }
        }

        if (!string.IsNullOrWhiteSpace(arama))
            query = query.Where(c => c.Unvan.Contains(arama) || c.CariKodu.Contains(arama));

        return await query.OrderBy(c => c.Unvan).Take(50).ToListAsync();
    }

    private async Task EnsurePersonelCariKayitlariAsync(ApplicationDbContext context, string? arama)
    {
        var personelQuery = context.Soforler
            .Where(s => s.Aktif && !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var normalizedSearch = arama.Trim();
            personelQuery = personelQuery.Where(s =>
                ((s.Ad ?? string.Empty) + " " + (s.Soyad ?? string.Empty)).Contains(normalizedSearch) ||
                (s.SoforKodu != null && s.SoforKodu.Contains(normalizedSearch)) ||
                (s.TcKimlikNo != null && s.TcKimlikNo.Contains(normalizedSearch)) ||
                (s.Telefon != null && s.Telefon.Contains(normalizedSearch)));
        }

        var personeller = await personelQuery
            .OrderBy(s => s.Ad)
            .ThenBy(s => s.Soyad)
            .Take(20)
            .ToListAsync();

        if (!personeller.Any())
            return;

        var personelIds = personeller.Select(s => s.Id).ToList();

        // Mevcut cari kayıtlarını bul
        var mevcutCariler = await context.Cariler
            .Where(c => c.CariTipi == CariTipi.Personel && c.SoforId.HasValue && personelIds.Contains(c.SoforId.Value))
            .ToListAsync();

        var mevcutCariSoforIds = mevcutCariler.Select(c => c.SoforId!.Value).ToList();

        // Mevcut carilerin unvanlarını güncelle (Ad Soyad formatına)
        foreach (var cari in mevcutCariler)
        {
            var personel = personeller.FirstOrDefault(p => p.Id == cari.SoforId);
            if (personel != null && cari.Unvan != personel.TamAd)
            {
                cari.Unvan = personel.TamAd;
                cari.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Yeni cariler oluştur
        var yeniCariler = new List<Cari>();

        foreach (var personel in personeller.Where(s => !mevcutCariSoforIds.Contains(s.Id)))
        {
            var cari = new Cari
            {
                CariKodu = await _cariService.GenerateNextKodAsync(),
                Unvan = personel.TamAd,
                CariTipi = CariTipi.Personel,
                TcKimlikNo = personel.TcKimlikNo,
                Telefon = personel.Telefon,
                Email = personel.Email,
                Adres = personel.Adres,
                SoforId = personel.Id,
                Aktif = true,
                CreatedAt = DateTime.UtcNow
            };

            yeniCariler.Add(cari);
            context.Cariler.Add(cari);
        }

        // Değişiklikleri kaydet (hem güncelleme hem yeni eklemeler)
        if (mevcutCariler.Any(c => c.UpdatedAt != null) || yeniCariler.Any())
        {
            await context.SaveChangesAsync();
        }

        // Yeni cariler için muhasebe hesabı oluştur
        foreach (var yeniCari in yeniCariler)
        {
            await _cariService.EnsureMuhasebeHesapAsync(yeniCari.Id);
        }
    }

    public async Task<List<MasrafKalemiBasit>> GetMasrafKalemleriAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MasrafKalemleri
            .AsNoTracking()
            .Where(m => m.Aktif)
            .GroupBy(m => new { m.Id, m.MasrafAdi, m.Kategori })
            .Select(g => new MasrafKalemiBasit
            {
                Id = g.Key.Id,
                Ad = g.Key.MasrafAdi,
                MuhasebeHesapKodu = GetMasrafHesapKodu(g.Key.Kategori)
            })
            .OrderBy(m => m.Ad)
            .ToListAsync();
    }

    public async Task<List<BankaHesapBasit>> GetBankaHesaplariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BankaHesaplari
            .AsNoTracking()
            .Where(b => b.Aktif)
            .OrderBy(b => b.HesapAdi)
            .Select(b => new BankaHesapBasit
            {
                Id = b.Id,
                HesapAdi = b.HesapAdi,
                MuhasebeHesapKodu = b.VarsayilanMuhasebeKodu,
                Bakiye = b.AcilisBakiye // Gerçek bakiye hesaplanmalı
            })
            .ToListAsync();
    }

    public async Task<List<Arac>> GetAraclarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .AsNoTracking()
            .Where(a => a.Aktif)
            .OrderBy(a => a.AktifPlaka)
            .ToListAsync();
    }

    public async Task<List<StokBasit>> GetStoklarAsync(string? arama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.StokKartlari
            .AsNoTracking()
            .Where(s => s.Aktif);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(s => s.StokAdi.Contains(arama) || s.StokKodu.Contains(arama));
        }

        return await query
            .OrderBy(s => s.StokAdi)
            .Take(50)
            .Select(s => new StokBasit
            {
                Id = s.Id,
                StokKodu = s.StokKodu,
                StokAdi = s.StokAdi,
                Birim = s.Birim,
                AlisFiyati = s.AlisFiyati,
                SatisFiyati = s.SatisFiyati,
                KdvOrani = s.KdvOrani,
                MevcutStok = s.MevcutStok
            })
            .ToListAsync();
    }

    public async Task<Cari> HizliCariOlusturAsync(string unvan, CariTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Önce aynı unvan ile mevcut cari var mı kontrol et
        var mevcutCari = await context.Cariler
            .IgnoreQueryFilters()
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(c => c.Unvan == unvan && !c.IsDeleted);

        if (mevcutCari != null)
        {
            // Mevcut cari varsa onu döndür
            return mevcutCari;
        }

        // Benzersiz kod üret (retry mekanizması ile)
        string cariKodu;
        var maxRetries = 5;
        var retryCount = 0;

        do
        {
            cariKodu = await _cariService.GenerateNextKodAsync();
            var kodMevcut = await context.Cariler
                .IgnoreQueryFilters()
                .AnyAsync(c => c.CariKodu == cariKodu);

            if (!kodMevcut)
                break;

            retryCount++;
            await Task.Delay(50); // Kısa bekleme
        } while (retryCount < maxRetries);

        var cari = new Cari
        {
            CariKodu = cariKodu,
            Unvan = unvan,
            CariTipi = tip,
            Aktif = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Cariler.Add(cari);
        await context.SaveChangesAsync();

        if (tip == CariTipi.Personel)
        {
            cari = await _cariService.EnsureMuhasebeHesapAsync(cari.Id);
        }

        return cari;
    }

    private async Task<(string HesapKodu, string HesapAdi, int? CariId, string? CariUnvan)> GetMasrafOdemeHesabiAsync(ApplicationDbContext context, KolayMuhasebeGiris giris)
    {
        if (giris.MasrafOdemeKaynagi == MasrafOdemeKaynagi.KasaBanka)
        {
            var odemeHesapKodu = "100.01";
            var odemeHesapAdi = "Kasa";

            if (giris.BankaHesapId.HasValue)
            {
                var bankaHesap = await context.BankaHesaplari.AsNoTracking()
                    .OrderBy(b => b.Id)
                    .FirstOrDefaultAsync(b => b.Id == giris.BankaHesapId);
                if (bankaHesap != null)
                {
                    odemeHesapAdi = bankaHesap.HesapAdi;
                    odemeHesapKodu = bankaHesap.VarsayilanMuhasebeKodu ?? GetBankaHesapKodu(bankaHesap.HesapTipi);
                }
            }

            return (odemeHesapKodu, odemeHesapAdi, null, null);
        }

        if (!giris.CariId.HasValue)
        {
            return giris.MasrafOdemeKaynagi == MasrafOdemeKaynagi.Personel
                ? ("335.01", "Personele Borçlar", null, null)
                : ("320.01", "Satıcılar", null, null);
        }

        var cari = await context.Cariler
            .AsNoTracking()
            .Include(c => c.MuhasebeHesap)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(c => c.Id == giris.CariId.Value);

        if (cari == null)
        {
            return giris.MasrafOdemeKaynagi == MasrafOdemeKaynagi.Personel
                ? ("335.01", "Personele Borçlar", giris.CariId, giris.CariUnvan)
                : ("320.01", "Satıcılar", giris.CariId, giris.CariUnvan);
        }

        var varsayilanKod = giris.MasrafOdemeKaynagi == MasrafOdemeKaynagi.Personel
            ? "335.01"
            : cari.CariTipi == CariTipi.Personel
                ? "335.01"
                : "320.01";

        var hesapKodu = cari.MuhasebeHesap?.HesapKodu ?? varsayilanKod;
        var hesapAdi = cari.MuhasebeHesap?.HesapAdi
            ?? (giris.MasrafOdemeKaynagi == MasrafOdemeKaynagi.Personel ? $"Personele Borçlar - {cari.Unvan}" : $"Cari Alacak - {cari.Unvan}");

        return (hesapKodu, hesapAdi, cari.Id, cari.Unvan);
    }

    private static bool CariMuhasebeEslemeGerekliMi(KolayMuhasebeGiris giris)
    {
        return giris.CariId.HasValue &&
               (giris.IslemTuru == KolayIslemTuru.AvansGirisi
                || (giris.IslemTuru == KolayIslemTuru.MasrafGirisi && giris.MasrafOdemeKaynagi != MasrafOdemeKaynagi.KasaBanka));
    }

    public async Task<Cari> HizliCariOlusturDetayliAsync(HizliCariModel model)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cari = new Cari
        {
            CariKodu = await _cariService.GenerateNextKodAsync(),
            Unvan = model.Unvan,
            CariTipi = model.CariTipi,
            Aktif = true,
            CreatedAt = DateTime.UtcNow
        };

        // Personel için TC, diğerleri için Vergi No
        if (model.CariTipi == CariTipi.Personel)
        {
            cari.TcKimlikNo = model.VergiNo;
        }
        else
        {
            cari.VergiNo = model.VergiNo;
        }

        cari.Telefon = model.Telefon;
        cari.Email = model.Email;
        cari.Adres = model.Adres;

        context.Cariler.Add(cari);
        await context.SaveChangesAsync();

        if (model.CariTipi == CariTipi.Personel)
        {
            cari = await _cariService.EnsureMuhasebeHesapAsync(cari.Id);
        }

        return cari;
    }

    public async Task<List<MuhasebeHesap>> GetMuhasebeHesaplariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MuhasebeHesaplari
            .AsNoTracking()
            .Where(h => h.Aktif)
            .OrderBy(h => h.HesapKodu)
            .ToListAsync();
    }

    public async Task<MuhasebeAyar> GetMuhasebeAyarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MuhasebeAyarlari
            .Include(a => a.KdvHesapEslestirmeleri)
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync() ?? new MuhasebeAyar();
    }

    private async Task<int?> GetHesapIdAsync(ApplicationDbContext context, string hesapKodu)
    {
        if (string.IsNullOrWhiteSpace(hesapKodu))
            return null;

        // Önce tam eşleşme dene
        var hesap = await context.MuhasebeHesaplari
            .AsNoTracking()
            .OrderBy(h => h.Id)
            .FirstOrDefaultAsync(h => h.HesapKodu == hesapKodu);

        if (hesap != null)
            return hesap.Id;

        // Tam eşleşme yoksa ana hesap koduyla (nokta öncesi) ara
        var anaKod = hesapKodu.Split('.')[0];
        hesap = await context.MuhasebeHesaplari
            .AsNoTracking()
            .OrderBy(h => h.Id)
            .FirstOrDefaultAsync(h => h.HesapKodu == anaKod || h.HesapKodu.StartsWith(anaKod + "."));

        return hesap?.Id;
    }

    private static FisTipi GetFisTipi(KolayIslemTuru islemTuru)
    {
        return islemTuru switch
        {
            KolayIslemTuru.TahsilatGirisi => FisTipi.Tahsilat,
            KolayIslemTuru.OdemeGirisi or KolayIslemTuru.AvansGirisi => FisTipi.Tediye,
            _ => FisTipi.Mahsup
        };
    }

    private static string OlusturAciklama(KolayMuhasebeGiris giris)
    {
        var islemAdi = giris.IslemTuru switch
        {
            KolayIslemTuru.GelirFatura => "Satış Faturası",
            KolayIslemTuru.GiderFatura => "Alış Faturası",
            KolayIslemTuru.MasrafGirisi => "Masraf",
            KolayIslemTuru.TahsilatGirisi => "Tahsilat",
            KolayIslemTuru.OdemeGirisi => "Ödeme",
            KolayIslemTuru.MahsupKaydi => "Mahsup",
            KolayIslemTuru.AvansGirisi => "Avans",
            _ => "İşlem"
        };

        var detaylar = new[] { giris.BelgeNo, giris.CariUnvan, giris.Aciklama }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct()
            .ToList();

        var detay = detaylar.Any() ? string.Join(" - ", detaylar) : "-";
        return $"{islemAdi}: {detay}";
    }

    public async Task GeriAlAsync(int muhasebeFisId)
    {
        await _muhasebeService.DeleteFisAsync(muhasebeFisId);
    }

    private async Task<(int? SoforId, int? CariId, string? PersonelAdi)> ResolveMasrafPersoneliAsync(ApplicationDbContext context, int? cariId, string? fallbackUnvan)
    {
        if (!cariId.HasValue)
            return (null, null, fallbackUnvan);

        var cari = await context.Cariler
            .Include(c => c.Sofor)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(c => c.Id == cariId.Value);

        if (cari == null)
            return (null, cariId, fallbackUnvan);

        if (cari.SoforId.HasValue)
            return (cari.SoforId.Value, null, cari.Sofor?.TamAd ?? cari.Unvan);

        if (cari.CariTipi != CariTipi.Personel)
            return (null, cari.Id, cari.Unvan);

        var sofor = await FindMatchingSoforAsync(context, cari);
        if (sofor == null)
            return (null, cari.Id, cari.Unvan);

        cari.SoforId = sofor.Id;
        await context.SaveChangesAsync();

        return (sofor.Id, null, sofor.TamAd);
    }

    private async Task<Sofor?> FindMatchingSoforAsync(ApplicationDbContext context, Cari cari)
    {
        IQueryable<Sofor> query = context.Soforler.Where(s => s.Aktif && !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(cari.TcKimlikNo))
        {
            var tc = cari.TcKimlikNo.Trim();
            var byTc = await query.OrderBy(s => s.Id).FirstOrDefaultAsync(s => s.TcKimlikNo == tc);
            if (byTc != null)
                return byTc;
        }

        if (!string.IsNullOrWhiteSpace(cari.Email))
        {
            var email = cari.Email.Trim();
            var byEmail = await query.OrderBy(s => s.Id).FirstOrDefaultAsync(s => s.Email == email);
            if (byEmail != null)
                return byEmail;
        }

        if (!string.IsNullOrWhiteSpace(cari.Telefon))
        {
            var telefon = cari.Telefon.Trim();
            var byTelefon = await query.OrderBy(s => s.Id).FirstOrDefaultAsync(s => s.Telefon == telefon);
            if (byTelefon != null)
                return byTelefon;
        }

        if (string.IsNullOrWhiteSpace(cari.Unvan))
            return null;

        var unvan = cari.Unvan.Trim();
        return await query.OrderBy(s => s.Id).FirstOrDefaultAsync(s => ((s.Ad ?? string.Empty) + " " + (s.Soyad ?? string.Empty)) == unvan);
    }

    private static string GetKaynakTip(FisKaynak kaynak)
    {
        return kaynak switch
        {
            FisKaynak.Fatura => "Fatura",
            FisKaynak.BankaHareket => "BankaHareket",
            FisKaynak.Butce => "Butce",
            _ => "Manuel"
        };
    }

    private static string GetMasrafHesapKodu(MasrafKategori kategori)
    {
        return kategori switch
        {
            MasrafKategori.Yakit => "770.01",
            MasrafKategori.Bakim => "770.02",
            MasrafKategori.Tamir => "770.03",
            MasrafKategori.Sigorta => "770.04",
            MasrafKategori.Vergi => "770.05",
            MasrafKategori.Personel => "770.06",
            MasrafKategori.Lastik => "770.07",
            MasrafKategori.YedekParca => "770.08",
            MasrafKategori.Mutfak => "770.09",
            MasrafKategori.Ofis => "770.10",
            MasrafKategori.Temizlik => "770.11",
            MasrafKategori.Kirtasiye => "770.12",
            MasrafKategori.Diger => "770.99",
            _ => "770.99"
        };
    }

    private static string GetBankaHesapKodu(HesapTipi tip)
    {
        return tip switch
        {
            HesapTipi.Kasa => "100.01",
            HesapTipi.VadesizHesap or HesapTipi.VadeliHesap => "102.01",
            HesapTipi.KrediHesabi => "300.01",
            HesapTipi.KrediKarti => "103.01",
            _ => "102.01"
        };
    }

    private async Task<string> GenerateFaturaNo(ApplicationDbContext context, string prefix)
    {
        var yil = DateTime.Now.Year;
        var lastNo = await context.Faturalar
            .Where(f => f.FaturaNo.StartsWith($"{prefix}{yil}"))
            .OrderByDescending(f => f.FaturaNo)
            .Select(f => f.FaturaNo)
            .FirstOrDefaultAsync();

        var nextNum = 1;
        if (lastNo != null && int.TryParse(lastNo.Substring(prefix.Length + 4), out var num))
            nextNum = num + 1;

        return $"{prefix}{yil}{nextNum:D6}";
    }

    private async Task<string> GenerateIslemNo(ApplicationDbContext context)
    {
        var yil = DateTime.Now.Year;
        var ay = DateTime.Now.Month;
        var prefix = $"ISL{yil}{ay:D2}";

        var lastNo = await context.BankaKasaHareketleri
            .Where(h => h.IslemNo.StartsWith(prefix))
            .OrderByDescending(h => h.IslemNo)
            .Select(h => h.IslemNo)
            .FirstOrDefaultAsync();

        var nextNum = 1;
        if (lastNo != null && int.TryParse(lastNo.Substring(prefix.Length), out var num))
            nextNum = num + 1;

        return $"{prefix}{nextNum:D4}";
    }

    public async Task<StokBasit> HizliStokOlusturAsync(string stokAdi, string birim, decimal kdvOrani)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Mevcut stok var mı kontrol et
        var mevcut = await context.StokKartlari
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(s => s.StokAdi == stokAdi && !s.IsDeleted);

        if (mevcut != null)
            return new StokBasit
            {
                Id = mevcut.Id,
                StokKodu = mevcut.StokKodu,
                StokAdi = mevcut.StokAdi,
                Birim = mevcut.Birim,
                AlisFiyati = mevcut.AlisFiyati,
                SatisFiyati = mevcut.SatisFiyati,
                KdvOrani = mevcut.KdvOrani,
                MevcutStok = mevcut.MevcutStok
            };

        // Yeni stok kodu üret
        var yil = DateTime.Now.Year;
        var sayi = await context.StokKartlari.CountAsync() + 1;
        var stokKodu = $"STK{yil % 100:D2}{sayi:D5}";

        var yeniStok = new StokKarti
        {
            StokKodu = stokKodu,
            StokAdi = stokAdi,
            Birim = birim,
            KdvOrani = kdvOrani,
            StokTipi = StokTipi.Hizmet,
            Aktif = true,
            CreatedAt = DateTime.UtcNow
        };

        context.StokKartlari.Add(yeniStok);
        await context.SaveChangesAsync();

        return new StokBasit
        {
            Id = yeniStok.Id,
            StokKodu = yeniStok.StokKodu,
            StokAdi = yeniStok.StokAdi,
            Birim = yeniStok.Birim,
            KdvOrani = yeniStok.KdvOrani
        };
    }

    #endregion
}



