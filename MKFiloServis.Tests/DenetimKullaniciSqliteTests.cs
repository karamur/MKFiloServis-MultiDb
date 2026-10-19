using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

/// <summary>
/// Otomatik denetim (aktivite günlüğü) kayıtları kullanıcıyı Blazor devresinin oturumundan almalı:
/// girişte AsyncLocal'a yazılan kullanıcı sonraki UI olaylarına akmadığı için eskiden her kayıt "Sistem" adına düşüyordu.
/// </summary>
public sealed class DenetimKullaniciSqliteTests
{
    [Fact]
    public async Task Devre_oturumundaki_kullanici_denetim_kaydina_adi_ve_kimligiyle_yazilir()
    {
        await using var ortam = await Ortam.KurAsync(Kullanici(7, "ayse", "Ayşe Yılmaz"));

        var log = await ortam.CariEkleVeLogunuOkuAsync();

        Assert.Equal("Ayşe Yılmaz", log.KullaniciAdi);
        Assert.Equal(7, log.KullaniciId);
    }

    [Fact]
    public async Task Oturum_yoksa_sistem_adina_yazilir_ve_kullanici_kimligi_bos_kalir()
    {
        await using var ortam = await Ortam.KurAsync(oturum: null);

        var log = await ortam.CariEkleVeLogunuOkuAsync();

        Assert.Equal("Sistem", log.KullaniciAdi);
        Assert.Null(log.KullaniciId);
    }

    [Fact]
    public async Task Ad_soyad_bossa_kullanici_adi_yazilir()
    {
        await using var ortam = await Ortam.KurAsync(Kullanici(9, "mehmet", ""));

        var log = await ortam.CariEkleVeLogunuOkuAsync();

        Assert.Equal("mehmet", log.KullaniciAdi);
        Assert.Equal(9, log.KullaniciId);
    }

    [Fact]
    public async Task Erisimcide_ad_varsa_korunur_kimlik_oturumdan_tamamlanir()
    {
        await using var ortam = await Ortam.KurAsync(Kullanici(7, "ayse", "Ayşe Yılmaz"));
        // AsyncLocal çağıranın bağlamına yazılır; bu yüzden eşzamanlı çağrı ve ardından beklenen işlem aynı akışta kalır.
        ortam.ErisimciAdiAyarla("Giriş Akışı");

        var log = await ortam.CariEkleVeLogunuOkuAsync();

        Assert.Equal("Giriş Akışı", log.KullaniciAdi);
        Assert.Equal(7, log.KullaniciId);
    }

    [Fact]
    public async Task Oturum_hizmeti_cozumlenemese_bile_kayit_yazilir()
    {
        // Kapsamda AppAuthenticationStateProvider hiç kayıtlı değil (arka plan işi): denetim yazımı bozulmaz.
        await using var ortam = await Ortam.KurAsync(oturum: null, oturumHizmetiKayitli: false);

        var log = await ortam.CariEkleVeLogunuOkuAsync();

        Assert.Equal("Sistem", log.KullaniciAdi);
    }

    private static Kullanici Kullanici(int id, string kullaniciAdi, string adSoyad)
        => new() { Id = id, KullaniciAdi = kullaniciAdi, AdSoyad = adSoyad };

    private sealed class Ortam : IAsyncDisposable
    {
        private readonly SqliteConnection _baglanti = new("Data Source=:memory:");
        private ServiceProvider _hizmetler = null!;
        private PayrollWriteSqliteTests.Factory _fabrika = null!;
        private bool _erisimciKuruldu;

        public static async Task<Ortam> KurAsync(Kullanici? oturum, bool oturumHizmetiKayitli = true)
        {
            var ortam = new Ortam();
            await ortam._baglanti.OpenAsync();

            var hizmetler = new ServiceCollection()
                .AddSingleton<IAktifFirmaProvider>(new SabitFirma())
                .AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();
            if (oturumHizmetiKayitli)
            {
                // Gerçek sınıfın bağımlılıkları (oturum deposu, lisans) bu test için gereksiz; yalnızca aktif kullanıcı alanı gerekir.
                var oturumSaglayici = (AppAuthenticationStateProvider)RuntimeHelpers.GetUninitializedObject(typeof(AppAuthenticationStateProvider));
                typeof(AppAuthenticationStateProvider)
                    .GetField("_aktifKullanici", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .SetValue(oturumSaglayici, oturum);
                hizmetler.AddSingleton(oturumSaglayici);
            }

            ortam._hizmetler = hizmetler.BuildServiceProvider();
            var secenekler = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(ortam._baglanti).Options;
            ortam._fabrika = new PayrollWriteSqliteTests.Factory(secenekler, ortam._hizmetler);

            await using var db = ortam._fabrika.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            db.Firmalar.Add(new Firma { Id = 1, FirmaKodu = "T", FirmaAdi = "Test", Organizasyon = new Organizasyon { Id = 1, Adi = "Org" } });
            await db.SaveChangesAsync();
            return ortam;
        }

        public void ErisimciAdiAyarla(string ad)
        {
            new CurrentUserAccessor().SetCurrentUser(ad, null);
            _erisimciKuruldu = true;
        }

        public async Task<AktiviteLog> CariEkleVeLogunuOkuAsync()
        {
            await using var db = _fabrika.CreateDbContext();
            db.Cariler.Add(new Cari { CariKodu = "C1", Unvan = "Cari 1", CariTipi = CariTipi.Musteri, FirmaId = 1 });
            await db.SaveChangesAsync();
            return await db.AktiviteLoglar.AsNoTracking().SingleAsync(l => l.EntityTipi == nameof(Cari));
        }

        public async ValueTask DisposeAsync()
        {
            if (_erisimciKuruldu)
                new CurrentUserAccessor().ClearCurrentUser();
            ApplicationDbContext.AmbientServiceProvider.Value = null;
            await _baglanti.DisposeAsync();
            await _hizmetler.DisposeAsync();
        }
    }

    private sealed class SabitFirma : IAktifFirmaProvider
    {
        public int? AktifFirmaId => 1;
        public bool HasAktifFirma => true;
        public bool TumFirmalar => false;
        public AktifFirmaBilgisi Mevcut => new();
        public event Action? AktifFirmaDegisti { add { } remove { } }
        public void Set(AktifFirmaBilgisi firma) => throw new NotSupportedException();
        public void SetTumFirmalar(bool tumFirmalar) => throw new NotSupportedException();
        public void SetDonem(int yil, int ay) => throw new NotSupportedException();
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }
}
