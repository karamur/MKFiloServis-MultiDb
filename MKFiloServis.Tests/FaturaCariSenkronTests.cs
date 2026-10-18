using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Calculation;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

public sealed class CariBakiyeHesaplayiciTests
{
    [Fact]
    public void Musteri_carisine_gelen_fatura_da_borc_tarafina_girer()
    {
        // Müşteri olarak açılmış cariden araç alımı faturası alınmış: eskiden Cari Listesi bunu yok sayıyordu.
        var (borc, alacak) = CariBakiyeHesaplayici.Hesapla(CariTipi.Musteri, gidenFatura: 1000m, gelenFatura: 400m, cikisOdemeleri: 0m, girisTahsilatlari: 250m);

        Assert.Equal(650m, borc);
        Assert.Equal(1000m, alacak);
        Assert.Equal(350m, alacak - borc);
    }

    [Fact]
    public void Tedarikci_carisine_kesilen_fatura_alacak_tarafina_girer()
    {
        var (borc, alacak) = CariBakiyeHesaplayici.Hesapla(CariTipi.Tedarikci, gidenFatura: 100m, gelenFatura: 500m, cikisOdemeleri: 200m, girisTahsilatlari: 0m);

        Assert.Equal(500m, borc);
        Assert.Equal(300m, alacak);
    }

    [Theory]
    [InlineData(CariTipi.Musteri)]
    [InlineData(CariTipi.Tedarikci)]
    [InlineData(CariTipi.MusteriTedarikci)]
    [InlineData((CariTipi)0)]
    public void Ayni_veriler_cari_tipinden_bagimsiz_ayni_bakiyeyi_verir(CariTipi tip)
    {
        var (borc, alacak) = CariBakiyeHesaplayici.Hesapla(tip, 1850000m, 4500000m, 75000m, 125000m);

        Assert.Equal(4625000m, borc);
        Assert.Equal(1925000m, alacak);
    }

    [Fact]
    public void Personel_carisinde_yalniz_alinan_belge_ve_odeme_izlenir()
    {
        var (borc, alacak) = CariBakiyeHesaplayici.Hesapla(CariTipi.Personel, gidenFatura: 10m, gelenFatura: 700m, cikisOdemeleri: 300m, girisTahsilatlari: 20m);

        Assert.Equal(700m, borc);
        Assert.Equal(300m, alacak);
    }

    [Fact]
    public void Uygula_cari_uzerindeki_borc_ve_alacagi_yazar()
    {
        var cari = new Cari { CariTipi = CariTipi.MusteriTedarikci };

        CariBakiyeHesaplayici.Uygula(cari, 1000m, 600m, 0m, 0m);

        Assert.Equal((600m, 1000m), (cari.Borc, cari.Alacak));
    }
}

public sealed class FaturaCariKurallariTests
{
    [Theory]
    [InlineData(CariTipi.Musteri, true, false, CariTipi.Musteri)]
    [InlineData(CariTipi.Musteri, true, true, CariTipi.MusteriTedarikci)]
    [InlineData(CariTipi.Musteri, false, true, CariTipi.MusteriTedarikci)]
    [InlineData(CariTipi.Tedarikci, false, true, CariTipi.Tedarikci)]
    [InlineData(CariTipi.Tedarikci, true, false, CariTipi.MusteriTedarikci)]
    [InlineData(CariTipi.MusteriTedarikci, true, false, CariTipi.MusteriTedarikci)]
    [InlineData(CariTipi.MusteriTedarikci, false, false, CariTipi.MusteriTedarikci)]
    [InlineData(CariTipi.Personel, true, true, CariTipi.Personel)]
    [InlineData((CariTipi)0, true, false, CariTipi.Musteri)]
    [InlineData((CariTipi)0, false, true, CariTipi.Tedarikci)]
    [InlineData((CariTipi)0, true, true, CariTipi.MusteriTedarikci)]
    [InlineData((CariTipi)0, false, false, (CariTipi)0)]
    public void Tip_yalnizca_genisler(CariTipi mevcut, bool giden, bool gelen, CariTipi beklenen)
        => Assert.Equal(beklenen, FaturaCariKurallari.GerekenTip(mevcut, giden, gelen));
}

public sealed class FaturaCariSenkronServiceSqliteTests
{
    private static readonly DateTime Ekim = new(2026, 10, 1);

    [Fact]
    public async Task Onizleme_hicbir_sey_yazmaz_uygulama_cari_tipini_faturalara_gore_genisletir()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (musteri, tedarikci, belirsiz, personel) = await SeedAsync(fixture);
        var servis = new FaturaCariSenkronService(fixture.Factory, MuhasebeSahtesi.Olustur(out _), fixture.Guard);

        var onizleme = await servis.GuncelleAsync(new FaturaCariSenkronIstegi
        {
            Baslangic = Ekim,
            Bitis = new DateTime(2026, 10, 31),
            Uygula = false
        });

        Assert.False(onizleme.Uygulandi);
        Assert.Equal(3, onizleme.CariTipiGuncellenen); // müşteri(+gelen), tedarikçi(+giden), belirsiz(0 → tedarikçi)
        Assert.Equal(CariTipi.Musteri, await TipAsync(fixture, musteri));
        Assert.Equal((CariTipi)0, await TipAsync(fixture, belirsiz));

        var uygulama = await servis.GuncelleAsync(new FaturaCariSenkronIstegi
        {
            Baslangic = Ekim,
            Bitis = new DateTime(2026, 10, 31),
            Uygula = true
        });

        Assert.True(uygulama.Uygulandi);
        Assert.Equal(3, uygulama.CariTipiGuncellenen);
        Assert.Equal(CariTipi.MusteriTedarikci, await TipAsync(fixture, musteri));
        Assert.Equal(CariTipi.MusteriTedarikci, await TipAsync(fixture, tedarikci));
        Assert.Equal(CariTipi.Tedarikci, await TipAsync(fixture, belirsiz));
        Assert.Equal(CariTipi.Personel, await TipAsync(fixture, personel));

        // Tekrar çalıştırmak güvenli: yeni değişiklik yok.
        var tekrar = await servis.GuncelleAsync(new FaturaCariSenkronIstegi { Baslangic = Ekim, Bitis = new DateTime(2026, 10, 31), Uygula = true });
        Assert.Equal(0, tekrar.CariTipiGuncellenen);
        Assert.False(tekrar.DegisiklikVar);
    }

    [Fact]
    public async Task Tarih_araligi_taranan_faturalari_sinirlar_ama_cari_tipi_tum_faturalara_bakar()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (musteri, _, _, _) = await SeedAsync(fixture);
        var servis = new FaturaCariSenkronService(fixture.Factory, MuhasebeSahtesi.Olustur(out _), fixture.Guard);

        // Eylül'de yalnızca müşterinin 2.000 ₺'lik kesilen faturası var; gelen faturası Ekim'de.
        var eylul = await servis.GuncelleAsync(new FaturaCariSenkronIstegi
        {
            Baslangic = new DateTime(2026, 9, 1),
            Bitis = new DateTime(2026, 9, 30),
            Uygula = true
        });

        Assert.Equal(1, eylul.TaranFatura);
        Assert.Equal(1, eylul.TaranCari);
        Assert.Equal(2000m, eylul.GidenToplam);
        Assert.Equal(0m, eylul.GelenToplam);
        Assert.Equal(CariTipi.MusteriTedarikci, await TipAsync(fixture, musteri)); // Ekim'deki gelen fatura da hesaba katıldı
    }

    [Fact]
    public async Task Yon_filtresi_yalniz_secilen_yondeki_faturalari_tarar()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await SeedAsync(fixture);
        var servis = new FaturaCariSenkronService(fixture.Factory, MuhasebeSahtesi.Olustur(out _), fixture.Guard);

        var gelen = await servis.GuncelleAsync(new FaturaCariSenkronIstegi
        {
            Baslangic = Ekim,
            Bitis = new DateTime(2026, 10, 31),
            Yon = FaturaYonu.Gelen,
            Uygula = false
        });

        Assert.Equal(4, gelen.TaranFatura); // M-GEL, T-GEL, B-GEL, P-GEL
        Assert.Equal(0m, gelen.GidenToplam);
        Assert.Equal(4500m, gelen.GelenToplam);
    }

    [Fact]
    public async Task Iptal_edilen_fatura_cari_tipini_etkilemez()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        int cariId;
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var cari = new Cari { CariKodu = "C-IPT", Unvan = "Iptal Cari", CariTipi = CariTipi.Musteri, FirmaId = 1 };
            db.Cariler.Add(cari);
            await db.SaveChangesAsync();
            cariId = cari.Id;
            db.Faturalar.Add(Fatura(cari.Id, FaturaYonu.Giden, "IPT-1", Ekim.AddDays(1), 100m));
            var iptal = Fatura(cari.Id, FaturaYonu.Gelen, "IPT-2", Ekim.AddDays(2), 900m);
            iptal.Durum = FaturaDurum.IptalEdildi;
            db.Faturalar.Add(iptal);
            await db.SaveChangesAsync();
        }

        var servis = new FaturaCariSenkronService(fixture.Factory, MuhasebeSahtesi.Olustur(out _), fixture.Guard);
        var sonuc = await servis.GuncelleAsync(new FaturaCariSenkronIstegi { Baslangic = Ekim, Bitis = new DateTime(2026, 10, 31) });

        Assert.Equal(1, sonuc.TaranFatura);
        Assert.Equal(0, sonuc.CariTipiGuncellenen);
        Assert.Equal(CariTipi.Musteri, await TipAsync(fixture, cariId));
    }

    [Fact]
    public async Task Aktarma_sonrasi_cari_guncelleme_yalniz_verilen_carilere_dokunur()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (musteri, tedarikci, _, _) = await SeedAsync(fixture);
        var servis = new FaturaCariSenkronService(fixture.Factory, MuhasebeSahtesi.Olustur(out _), fixture.Guard);

        var adlar = new List<string>();
        var guncellenen = await servis.CariTipleriniGuncelleAsync([musteri], adlar);

        Assert.Equal(1, guncellenen);
        Assert.Single(adlar);
        Assert.Contains("Müşteri → Müşteri + Tedarikçi", adlar[0]);
        Assert.Equal(CariTipi.MusteriTedarikci, await TipAsync(fixture, musteri));
        Assert.Equal(CariTipi.Tedarikci, await TipAsync(fixture, tedarikci));
        Assert.Equal(0, await servis.CariTipleriniGuncelleAsync([]));
    }

    [Fact]
    public async Task Eksik_muhasebe_fisi_ayar_aciksa_olusturulur_kapaliysa_uyari_verir()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await SeedAsync(fixture);
        var muhasebe = MuhasebeSahtesi.Olustur(out var cagrilar);
        var servis = new FaturaCariSenkronService(fixture.Factory, muhasebe, fixture.Guard);
        var istek = new FaturaCariSenkronIstegi
        {
            Baslangic = Ekim,
            Bitis = new DateTime(2026, 10, 31),
            CariTipiGuncelle = false,
            EksikMuhasebeFisiOlustur = true
        };

        // Ayar kaydı yokken fiş açılmaz.
        var ayarsiz = await servis.GuncelleAsync(istek);
        Assert.Equal(0, ayarsiz.OlusturulanFis);
        Assert.Empty(cagrilar);
        Assert.Contains(ayarsiz.Uyarilar, u => u.Contains("otomatik fiş kapalı"));
        Assert.True(ayarsiz.EksikFisSayisi > 0);

        int fisliFaturaId;
        await using (var db = fixture.Factory.CreateDbContext())
        {
            db.MuhasebeAyarlari.Add(new MuhasebeAyar { FaturaOtomatikMuhasebeFisi = true });
            // Ekim'deki bir fatura zaten fişli: taranır ama fiş istenmez.
            var fisli = await db.Faturalar.SingleAsync(f => f.FaturaNo == "M-GID");
            fisli.MuhasebeFisiOlusturuldu = true;
            fisliFaturaId = fisli.Id;
            await db.SaveChangesAsync();
        }

        var onizleme = await servis.GuncelleAsync(new FaturaCariSenkronIstegi { Baslangic = Ekim, Bitis = new DateTime(2026, 10, 31), CariTipiGuncelle = false, Uygula = false });
        Assert.Equal(5, onizleme.EksikFisSayisi); // Ekim'de 6 fatura var, biri fişli
        Assert.Empty(cagrilar);

        var sonuc = await servis.GuncelleAsync(istek);

        Assert.Equal(5, sonuc.OlusturulanFis);
        Assert.Equal(5, cagrilar.Count);
        Assert.DoesNotContain(fisliFaturaId, cagrilar);
    }

    [Fact]
    public async Task Bitis_tarihi_baslangictan_once_olamaz()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = new FaturaCariSenkronService(fixture.Factory, MuhasebeSahtesi.Olustur(out _), fixture.Guard);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servis.GuncelleAsync(new FaturaCariSenkronIstegi
        {
            Baslangic = new DateTime(2026, 10, 31),
            Bitis = new DateTime(2026, 10, 1)
        }));
    }

    private static async Task<CariTipi> TipAsync(PayrollWriteSqliteTests.Fixture fixture, int cariId)
    {
        await using var db = fixture.Factory.CreateDbContext();
        return await db.Cariler.AsNoTracking().Where(c => c.Id == cariId).Select(c => c.CariTipi).SingleAsync();
    }

    private static Fatura Fatura(int cariId, FaturaYonu yon, string no, DateTime tarih, decimal tutar) => new()
    {
        FaturaNo = no,
        FaturaTarihi = tarih,
        CariId = cariId,
        FirmaId = 1,
        FaturaYonu = yon,
        FaturaTipi = yon == FaturaYonu.Giden ? FaturaTipi.SatisFaturasi : FaturaTipi.AlisFaturasi,
        AraToplam = tutar,
        GenelToplam = tutar
    };

    /// <summary>
    /// Müşteri: Eylül'de 2.000 kesilen + Ekim'de 1.800 kesilen ve 1.800 gelen. Tedarikçi: Ekim'de 1.200 gelen ve 700 kesilen.
    /// Belirsiz tipli (0): Ekim'de 1.200 gelen. Personel: Ekim'de 300 gelen (tipi değişmez).
    /// </summary>
    private static async Task<(int Musteri, int Tedarikci, int Belirsiz, int Personel)> SeedAsync(PayrollWriteSqliteTests.Fixture fixture)
    {
        await using var db = fixture.Factory.CreateDbContext();
        var musteri = new Cari { CariKodu = "C-M", Unvan = "Yunus Parlak", CariTipi = CariTipi.Musteri, FirmaId = 1 };
        var tedarikci = new Cari { CariKodu = "C-T", Unvan = "Surogullari", CariTipi = CariTipi.Tedarikci, FirmaId = 1 };
        var belirsiz = new Cari { CariKodu = "C-B", Unvan = "Eski Kayit", CariTipi = (CariTipi)0, FirmaId = 1 };
        var personel = new Cari { CariKodu = "C-P", Unvan = "Personel Cari", CariTipi = CariTipi.Personel, FirmaId = 1 };
        db.Cariler.AddRange(musteri, tedarikci, belirsiz, personel);
        await db.SaveChangesAsync();

        db.Faturalar.AddRange(
            Fatura(musteri.Id, FaturaYonu.Giden, "M-EYL", new DateTime(2026, 9, 12), 2000m),
            Fatura(musteri.Id, FaturaYonu.Giden, "M-GID", Ekim.AddDays(4), 1800m),
            Fatura(musteri.Id, FaturaYonu.Gelen, "M-GEL", Ekim.AddDays(9), 1800m),
            Fatura(tedarikci.Id, FaturaYonu.Gelen, "T-GEL", Ekim.AddDays(6), 1200m),
            Fatura(tedarikci.Id, FaturaYonu.Giden, "T-GID", Ekim.AddDays(7), 700m),
            Fatura(belirsiz.Id, FaturaYonu.Gelen, "B-GEL", Ekim.AddDays(10), 1200m),
            Fatura(personel.Id, FaturaYonu.Gelen, "P-GEL", Ekim.AddDays(11), 300m));
        await db.SaveChangesAsync();
        return (musteri.Id, tedarikci.Id, belirsiz.Id, personel.Id);
    }

    /// <summary>Yalnızca CreateFaturaFisiAsync(Fatura) çağrılarını kaydeden IMuhasebeService sahtesi.</summary>
    public class MuhasebeSahtesi : DispatchProxy
    {
        private List<int> _cagrilar = new();

        public static IMuhasebeService Olustur(out List<int> cagrilar)
        {
            var proxy = Create<IMuhasebeService, MuhasebeSahtesi>();
            var sahte = (MuhasebeSahtesi)(object)proxy;
            cagrilar = sahte._cagrilar;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IMuhasebeService.CreateFaturaFisiAsync) && args is [Fatura fatura])
            {
                _cagrilar.Add(fatura.Id);
                return Task.FromResult(new MuhasebeFis { Id = fatura.Id });
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
