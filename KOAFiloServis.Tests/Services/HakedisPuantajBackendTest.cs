using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Backend entegrasyon testi: HakedisPuantajService.CreateAsync zincirini
/// gerçek PostgreSQL veritabanında test eder. UI olmadan DB'ye yazıldığını kanıtlar.
/// Bu TEST AMAÇLI geçici bir testtir, kalıcı feature değildir.
/// </summary>
public class HakedisPuantajBackendTest
{
    private sealed class TestAktifFirmaProvider : IAktifFirmaProvider
    {
        public TestAktifFirmaProvider(int firmaId)
        {
            Mevcut = new AktifFirmaBilgisi { FirmaId = firmaId, TumFirmalar = false };
        }

        public int? AktifFirmaId => Mevcut.FirmaId > 0 ? Mevcut.FirmaId : null;
        public bool HasAktifFirma => AktifFirmaId.HasValue || TumFirmalar;
        public bool TumFirmalar => Mevcut.TumFirmalar;
        public AktifFirmaBilgisi Mevcut { get; private set; }
        public event Action? AktifFirmaDegisti;

        public void Set(AktifFirmaBilgisi firma)
        {
            Mevcut = firma;
            AktifFirmaDegisti?.Invoke();
        }

        public void SetTumFirmalar(bool tumFirmalar)
        {
            Mevcut.TumFirmalar = tumFirmalar;
            AktifFirmaDegisti?.Invoke();
        }

        public void SetDonem(int yil, int ay)
        {
            Mevcut.AktifDonemYil = yil;
            Mevcut.AktifDonemAy = ay;
            AktifFirmaDegisti?.Invoke();
        }

        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }

    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";

    /// <summary>
    /// Creates an IServiceProvider that provides TestAktifFirmaProvider for tenant filter resolution.
    /// Without this, the global query filter (FirmaTenantId == FirmaId) resolves FirmaTenantId as null
    /// and filters OUT all records with non-null FirmaId — causing "kayıt DB'de bulunamadı" errors.
    /// </summary>
    private IServiceProvider CreateServiceProvider(int firmaId = 1)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAktifFirmaProvider>(new TestAktifFirmaProvider(firmaId));
        return services.BuildServiceProvider();
    }

    private IDbContextFactory<ApplicationDbContext> CreateFactory(int firmaId = 1)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        var sp = CreateServiceProvider(firmaId);
        return new ScopedDbContextFactory(options, sp);
    }

    /// <summary>
    /// TEST 1: CreateAsync → DB'ye yazıldığını kanıtla
    /// </summary>
    [Fact]
    public async Task Test1_CreateAsync_WritesToDatabase()
    {
        // Arrange
        var factory = CreateFactory();
        var service = new HakedisPuantajService(factory, new TestAktifFirmaProvider(1), NullLogger<HakedisPuantajService>.Instance);

        await using var context = await factory.CreateDbContextAsync();

        // Aynı FirmaId=1 için test verisi bul
        var arac = await context.Araclar
            .FirstOrDefaultAsync(a => !a.IsDeleted && a.FirmaId == 1);
        var sofor = await context.Soforler
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.FirmaId == 1);
        var guzergah = await context.Guzergahlar
            .FirstOrDefaultAsync(g => !g.IsDeleted && g.FirmaId == 1 && g.Aktif);
        var cari = await context.Cariler
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);

        arac.Should().NotBeNull("test için en az bir araç olmalı");
        sofor.Should().NotBeNull("test için en az bir personel olmalı");
        guzergah.Should().NotBeNull("test için en az bir güzergah olmalı");
        cari.Should().NotBeNull("test için en az bir cari olmalı");

        // Önce bu kombinasyonda eski test kaydı varsa soft-delete yap
        var eskiKayit = await context.HakedisPuantajlar
            .Where(h => h.Yil == 2026 && h.Ay == 6
                && h.GuzergahId == guzergah!.Id
                && h.AracId == arac!.Id
                && h.SoforId == sofor!.Id)
            .ToListAsync();
        foreach (var ek in eskiKayit)
        {
            ek.IsDeleted = true;
            ek.DeletedAt = DateTime.UtcNow;
        }
        if (eskiKayit.Any())
            await context.SaveChangesAsync();

        var hakedis = new HakedisPuantaj
        {
            FirmaId = 1,
            Yil = 2026,
            Ay = 6,
            AracId = arac!.Id,
            SoforId = sofor!.Id,
            GuzergahId = guzergah!.Id,
            CariId = cari!.Id,
            GelirBirimFiyat = 1000,
            GiderBirimFiyat = 800,
            KdvOrani = 20,
            Durum = HakedisDurumu.Taslak,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        // Act
        var sonuc = await service.CreateAsync(hakedis);

        // Assert
        sonuc.Should().NotBeNull();
        sonuc.Id.Should().BeGreaterThan(0, "SaveChanges sonrası Id atanmış olmalı");

        // DB'den tekrar oku
        var dbKayit = await context.HakedisPuantajlar
            .FirstOrDefaultAsync(h => h.Id == sonuc.Id && !h.IsDeleted);
        dbKayit.Should().NotBeNull("kayıt DB'de bulunmalı");
        dbKayit!.Yil.Should().Be(2026);
        dbKayit.Ay.Should().Be(6);
        dbKayit.FirmaId.Should().Be(1);
        dbKayit.Durum.Should().Be(HakedisDurumu.Taslak);

        // Cleanup: test kaydını soft-delete yap
        dbKayit.IsDeleted = true;
        dbKayit.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// TEST 2: CreateAsync → GunlukDetayOlustur → Detaylar tablosuna yazıldığını kanıtla
    /// </summary>
    [Fact]
    public async Task Test2_GunlukDetayOlustur_WritesDetails()
    {
        // Arrange
        var factory = CreateFactory();
        var service = new HakedisPuantajService(factory, new TestAktifFirmaProvider(1), NullLogger<HakedisPuantajService>.Instance);
        await using var context = await factory.CreateDbContextAsync();

        var arac = await context.Araclar
            .FirstOrDefaultAsync(a => !a.IsDeleted && a.FirmaId == 1);
        var sofor = await context.Soforler
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.FirmaId == 1);
        var guzergah = await context.Guzergahlar
            .FirstOrDefaultAsync(g => !g.IsDeleted && g.FirmaId == 1 && g.Aktif);
        var cari = await context.Cariler
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.FirmaId == 1);

        arac.Should().NotBeNull();
        sofor.Should().NotBeNull();
        guzergah.Should().NotBeNull();
        cari.Should().NotBeNull();

        // Eski test kaydı varsa temizle
        var eskiKayit = await context.HakedisPuantajlar
            .Where(h => h.Yil == 2026 && h.Ay == 6
                && h.GuzergahId == guzergah!.Id
                && h.AracId == arac!.Id
                && h.SoforId == sofor!.Id)
            .ToListAsync();
        foreach (var ek in eskiKayit)
        {
            ek.IsDeleted = true;
            ek.DeletedAt = DateTime.UtcNow;
        }
        if (eskiKayit.Any())
            await context.SaveChangesAsync();

        var hakedis = new HakedisPuantaj
        {
            FirmaId = 1,
            Yil = 2026,
            Ay = 6,
            AracId = arac!.Id,
            SoforId = sofor!.Id,
            GuzergahId = guzergah!.Id,
            CariId = cari!.Id,
            GelirBirimFiyat = 1000,
            GiderBirimFiyat = 800,
            KdvOrani = 20,
            Durum = HakedisDurumu.Taslak,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        // Act: CreateAsync otomatik olarak GunlukDetayOlusturAsync çağırır
        var sonuc = await service.CreateAsync(hakedis);
        sonuc.Id.Should().BeGreaterThan(0);

        // GunlukSeferGuncelleAsync ile bir güne sefer sayısı ata
        // İmza: (int hakedisId, int gun, int seferSayisi, bool ekSeferMi, string? aciklama = null)
        await service.GunlukSeferGuncelleAsync(sonuc.Id, gun: 15, seferSayisi: 3, seferTuruId: null, fiyatCarpani: 1m, mesaiMi: false, ekSeferMi: false);

        // Assert: Detaylar tablosunda kayıt var mı?
        var detaylar = await context.HakedisPuantajDetaylar
            .Where(d => d.HakedisPuantajId == sonuc.Id && !d.IsDeleted)
            .OrderBy(d => d.Gun)
            .ToListAsync();

        detaylar.Should().NotBeEmpty("GunlukDetayOlustur tüm günler için detay oluşturmalı");
        // Haziran 30 gün → 30 detay kaydı
        detaylar.Count.Should().Be(30, "Haziran ayı için 30 günlük detay oluşturulmalı");

        // 15. gün detayı kontrol et
        var detay15 = detaylar.FirstOrDefault(d => d.Gun == 15);
        detay15.Should().NotBeNull("15. gün detayı oluşturulmalı");
        detay15!.SeferSayisi.Should().Be(3, "GunlukSeferGuncelle ile 3 sefer atandı");
        detay15.EkSeferMi.Should().BeFalse();

        // Diğer günler 0 sefer olmalı
        var detay1 = detaylar.FirstOrDefault(d => d.Gun == 1);
        detay1.Should().NotBeNull();
        detay1!.SeferSayisi.Should().Be(0, "atanmamış günler 0 olmalı");

        // Cleanup: test kaydını ve detaylarını soft-delete yap
        var dbKayit = await context.HakedisPuantajlar
            .FirstOrDefaultAsync(h => h.Id == sonuc.Id);
        if (dbKayit != null)
        {
            dbKayit.IsDeleted = true;
            dbKayit.DeletedAt = DateTime.UtcNow;
        }
        foreach (var d in detaylar)
        {
            d.IsDeleted = true;
            d.DeletedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// TEST 3: Validation — zorunlu alanlar kontrolü
    /// </summary>
    [Fact]
    public async Task Test3_Validation_RejectsInvalidData()
    {
        var factory = CreateFactory();
        var service = new HakedisPuantajService(factory, new TestAktifFirmaProvider(1), NullLogger<HakedisPuantajService>.Instance);

        // GuzergahId=0 → hata vermeli
        var invalid = new HakedisPuantaj
        {
            FirmaId = 1,
            Yil = 2026, Ay = 6,
            GuzergahId = 0,
            AracId = 0,
            SoforId = 0,
            CariId = 0,
            Durum = HakedisDurumu.Taslak
        };

        Func<Task> act = async () => await service.CreateAsync(invalid);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Güzergah*");
    }
}
