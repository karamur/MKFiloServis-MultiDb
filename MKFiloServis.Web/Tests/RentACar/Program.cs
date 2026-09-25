using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.RentACar;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlite(connection)
    .Options;
var firma = new TestFirmaProvider { AktifFirmaId = 1 };
var services = new ServiceCollection().AddSingleton<IAktifFirmaProvider>(firma).BuildServiceProvider();
IDbContextFactory<ApplicationDbContext> factory = new TestContextFactory(options, services);
await using (var context = await factory.CreateDbContextAsync())
{
    await context.Database.EnsureCreatedAsync();
    await context.Database.ExecuteSqlRawAsync("DROP TABLE RentACarKaraListeKayitlari");
    var migration = new MKFiloServis.Web.Data.Migrations.RentACarKaraListe();
    var sqlGenerator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator>(context);
    foreach (var command in sqlGenerator.Generate(migration.UpOperations, context.Model))
        await context.Database.ExecuteSqlRawAsync(command.CommandText);
    Console.WriteLine("PASS: ayrı kara liste migration tablosu oluşturuldu");
    context.Organizasyonlar.Add(new Organizasyon { Id = 1, Adi = "Test organizasyonu", Kod = "RC-ORG" });
    context.Firmalar.AddRange(
        new Firma { Id = 1, OrganizasyonId = 1, FirmaKodu = "RC-F1", FirmaAdi = "Birinci firma" },
        new Firma { Id = 2, OrganizasyonId = 1, FirmaKodu = "RC-F2", FirmaAdi = "İkinci firma" });
    context.Cariler.AddRange(
        new Cari { Id = 101, FirmaId = 1, CariKodu = "RC-101", Unvan = "Birinci müşteri", CariTipi = CariTipi.Musteri },
        new Cari { Id = 102, FirmaId = 2, CariKodu = "RC-102", Unvan = "İkinci müşteri", CariTipi = CariTipi.Musteri });
    context.Araclar.AddRange(
        new Arac { Id = 201, FirmaId = 1, SaseNo = "RC-201", AktifPlaka = "34 ABC 01" },
        new Arac { Id = 202, FirmaId = 2, SaseNo = "RC-202", AktifPlaka = "34 ABC 02" });
    await context.SaveChangesAsync();
}

var service = new RentACarRezervasyonServisi(factory, firma);
var start = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc);
RentACarRezervasyonTalebi Request(int customer, int vehicle, DateTime from, DateTime to) => new()
{
    MusteriId = customer,
    AracId = vehicle,
    BaslangicTarihi = from,
    PlanlananBitisTarihi = to,
    GunlukFiyat = 100
};

void ExpectModelFailure(string name, RentACarRezervasyonTalebi request, string propertyName)
{
    var results = new List<ValidationResult>();
    var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
    if (valid || !results.Any(x => x.MemberNames.Contains(propertyName)))
    {
        throw new Exception($"{name}: beklenen model doğrulama hatası bulunamadı.");
    }

    Console.WriteLine($"PASS: {name}");
}

ExpectModelFailure("müşteri zorunlu doğrulaması", Request(0, 201, start, start.AddDays(1)), nameof(RentACarRezervasyonTalebi.MusteriId));
ExpectModelFailure("araç zorunlu doğrulaması", Request(101, 0, start, start.AddDays(1)), nameof(RentACarRezervasyonTalebi.AracId));
ExpectModelFailure("tarih aralığı doğrulaması", Request(101, 201, start, start), nameof(RentACarRezervasyonTalebi.PlanlananBitisTarihi));
var invalidPrice = Request(101, 201, start, start.AddDays(1));
invalidPrice.GunlukFiyat = 0;
ExpectModelFailure("günlük fiyat pozitif doğrulaması", invalidPrice, nameof(RentACarRezervasyonTalebi.GunlukFiyat));
var negativeDeposit = Request(101, 201, start, start.AddDays(1));
negativeDeposit.Depozito = -1;
ExpectModelFailure("depozito negatif değer doğrulaması", negativeDeposit, nameof(RentACarRezervasyonTalebi.Depozito));
var longNotes = Request(101, 201, start, start.AddDays(1));
longNotes.Notlar = new string('x', 501);
ExpectModelFailure("not uzunluğu doğrulaması", longNotes, nameof(RentACarRezervasyonTalebi.Notlar));

async Task ExpectFailure(string name, Func<Task> action, string expectedMessage)
{
    try
    {
        await action();
        throw new Exception($"{name}: işlem beklenmedik şekilde başarılı oldu.");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains(expectedMessage, StringComparison.Ordinal))
    {
        Console.WriteLine($"PASS: {name}");
    }
}

var reservation = await service.RezervasyonOlusturAsync(Request(101, 201, start, start.AddDays(2)));
Console.WriteLine("PASS: geçerli rezervasyon oluşturuldu");
await ExpectFailure("başka firma müşterisi reddedilir", () => service.RezervasyonOlusturAsync(Request(102, 201, start.AddDays(3), start.AddDays(4))), "müşteri aktif firmaya ait değil");
await ExpectFailure("başka firma aracı reddedilir", () => service.RezervasyonOlusturAsync(Request(101, 202, start.AddDays(3), start.AddDays(4))), "araç aktif firmaya ait değil");
await ExpectFailure("rezervasyon çakışması reddedilir", () => service.RezervasyonOlusturAsync(Request(101, 201, start.AddDays(1), start.AddDays(3))), "müsait değil");

var deliveryInfo = new RentACarAracIslemBilgisi
{
    Kilometre = 10000,
    YakitSeviyesi = "8/8",
    HasarNotlari = "Teslim hasarı yok",
    Aksesuarlar = "Yedek anahtar",
    Notlar = "Teslim edildi"
};
await service.AraciTeslimEtAsync(reservation.Id, deliveryInfo);
await using (var context = await factory.CreateDbContextAsync())
{
    var delivered = await context.MusteriKiralamalar.SingleAsync(x => x.Id == reservation.Id);
    var deliveredVehicle = await context.Araclar.SingleAsync(x => x.Id == reservation.AracId);
    if (delivered.Durum != KiralamaDurumu.Aktif || delivered.BaslangicKm != 10000
        || delivered.TeslimYakitSeviyesi != "8/8" || delivered.TeslimHasarNotlari != "Teslim hasarı yok"
        || deliveredVehicle.Durumu != AracDurumu.Kiralandi)
    {
        throw new Exception("Araç teslim bilgileri kalıcı olarak kaydedilmedi.");
    }
}
Console.WriteLine("PASS: araç teslimi durumu ve tutanak alanları kaydedildi");
await ExpectFailure("aynı rezervasyon ikinci kez teslim edilemez", () => service.AraciTeslimEtAsync(reservation.Id, deliveryInfo), "Yalnızca rezervasyon");
firma.AktifFirmaId = 2;
var foreignDelivery = await service.RezervasyonOlusturAsync(Request(102, 202, start.AddDays(10), start.AddDays(11)));
firma.AktifFirmaId = 1;
await ExpectFailure("başka firma rezervasyonu teslim edilemez", () => service.AraciTeslimEtAsync(foreignDelivery.Id, deliveryInfo), "bulunamadı");

var returnInfo = new RentACarAracIslemBilgisi
{
    Kilometre = 10075,
    YakitSeviyesi = "7/8",
    HasarNotlari = "Yeni hasar yok",
    Aksesuarlar = "Yedek anahtar teslim alındı",
    Notlar = "İade tamamlandı"
};
await service.AraciIadeAlAsync(reservation.Id, returnInfo);
await using (var context = await factory.CreateDbContextAsync())
{
    var returned = await context.MusteriKiralamalar.SingleAsync(x => x.Id == reservation.Id);
    var returnedVehicle = await context.Araclar.SingleAsync(x => x.Id == reservation.AracId);
    if (returned.Durum != KiralamaDurumu.Tamamlandi || returned.BitisKm != 10075
        || returned.IadeYakitSeviyesi != "7/8" || returned.IadeHasarNotlari != "Yeni hasar yok"
        || returned.IadeNotlari != "İade tamamlandı" || returnedVehicle.Durumu != AracDurumu.Bosta)
    {
        throw new Exception("Araç iade bilgileri kalıcı olarak kaydedilmedi.");
    }
}
Console.WriteLine("PASS: araç iadesi durumu ve tutanak alanları kaydedildi");
await ExpectFailure("tamamlanan kiralama ikinci kez iade edilemez", () => service.AraciIadeAlAsync(reservation.Id, returnInfo), "Yalnızca aktif");
await ExpectFailure("teslim kilometresinden düşük iade reddedilir", async () =>
{
    try
    {
        firma.AktifFirmaId = 2;
        var activeReservation = await service.RezervasyonOlusturAsync(Request(102, 202, start.AddDays(20), start.AddDays(22)));
        await service.AraciTeslimEtAsync(activeReservation.Id, deliveryInfo);
        await service.AraciIadeAlAsync(activeReservation.Id, new RentACarAracIslemBilgisi
        {
            Kilometre = deliveryInfo.Kilometre - 1,
            YakitSeviyesi = "8/8"
        });
    }
    finally
    {
        firma.AktifFirmaId = 1;
    }
}, "İade kilometresi");

await using (var context = await factory.CreateDbContextAsync())
{
    var completed = await context.MusteriKiralamalar.SingleAsync(x => x.Id == reservation.Id);
    completed.Durum = KiralamaDurumu.Tamamlandi;
    completed.GercekBitisTarihi = start.AddDays(3);
    await context.SaveChangesAsync();
}

var available = await service.GetMusaitAraclarAsync(start.AddDays(2), start.AddDays(3));
if (available.Any(x => x.Id == 201)) throw new Exception("Tamamlanan kiralamanın gerçek kullanım süresi müsait gösterildi.");
Console.WriteLine("PASS: tamamlanan kiralamanın gerçek süresi müsait değil");
await ExpectFailure("tamamlanan kiralama süresi çakışması reddedilir", () => service.RezervasyonOlusturAsync(Request(101, 201, start.AddDays(2), start.AddDays(4))), "müsait değil");
var later = await service.RezervasyonOlusturAsync(Request(101, 201, start.AddDays(3), start.AddDays(4)));
Console.WriteLine("PASS: gerçek iade sonrası sınırda rezervasyon kabul edildi");
await ExpectFailure("düzenlemede tamamlanan kiralama çakışması reddedilir", () => service.RezervasyonGuncelleAsync(later.Id, Request(101, 201, start.AddDays(2), start.AddDays(4))), "müsait değil");
firma.AktifFirmaId = 2;
var otherTenantReservation = await service.RezervasyonOlusturAsync(Request(102, 202, start, start.AddDays(1)));
firma.AktifFirmaId = 1;
await ExpectFailure("başka firmanın rezervasyonu düzenlenemez", () => service.RezervasyonGuncelleAsync(otherTenantReservation.Id, Request(101, 201, start.AddDays(4), start.AddDays(5))), "bulunamadı");

var blacklistAuth = new TestAuthenticationStateProvider();
var blacklist = new RentACarKaraListeServisi(factory, firma, blacklistAuth);
await ExpectFailure("kara liste gerekçesi zorunlu", () => blacklist.EkleAsync(1, 101, " "), "Gerekçe");
await ExpectFailure("başka firma müşterisi kara listeye eklenemez", () => blacklist.EkleAsync(1, 102, "Test"), "aktif firmaya ait değil");
await blacklist.EkleAsync(1, 101, "Test engeli");
await ExpectFailure("mükerrer aktif engel reddedilir", () => blacklist.EkleAsync(1, 101, "Tekrar"), "zaten kara listede");
await ExpectFailure("kara liste rezervasyon oluşturmayı engeller", () => service.RezervasyonOlusturAsync(Request(101, 201, start.AddDays(40), start.AddDays(41))), "kara listesinde");
await ExpectFailure("kara liste rezervasyon düzenlemeyi engeller", () => service.RezervasyonGuncelleAsync(later.Id, Request(101, 201, start.AddDays(40), start.AddDays(41))), "kara listesinde");
await ExpectFailure("kara liste teslimi engeller", () => service.AraciTeslimEtAsync(later.Id, deliveryInfo), "kara listesinde");
var blocked = (await blacklist.ListeleAsync(1)).Single();
firma.AktifFirmaId = 2;
if ((await blacklist.ListeleAsync(2)).Count != 0) throw new Exception("Kara liste firma izolasyonu bozuldu.");
await ExpectFailure("başka firma engeli kaldırılamaz", () => blacklist.KaldirAsync(2, blocked.Id, "Test"), "bulunamadı");
await service.RezervasyonOlusturAsync(Request(102, 202, start.AddDays(40), start.AddDays(41)));
Console.WriteLine("PASS: kara liste diğer firmayı etkilemedi");
firma.AktifFirmaId = 1;
await blacklist.KaldirAsync(1, blocked.Id, "Kontrol tamamlandı");
var released = (await blacklist.ListeleAsync(1)).Single();
if (released.KaldirmaTarihi is null || released.KaldiranKullaniciId != 7 || released.Neden != "Test engeli"
    || released.KaldirmaNedeni != "Kontrol tamamlandı") throw new Exception("Kara liste işlem izi kayboldu.");
await service.AraciTeslimEtAsync(later.Id, deliveryInfo);
await blacklist.EkleAsync(1, 101, "İade testi");
await service.AraciIadeAlAsync(later.Id, returnInfo);
Console.WriteLine("PASS: kara liste iadeyi engellemedi; kaldırma sonrası teslim kabul edildi");
blacklistAuth.Admin = false;
try
{
    await blacklist.EkleAsync(1, 101, "Yetkisiz");
    throw new Exception("Yetkisiz kara liste yönetimi kabul edildi.");
}
catch (UnauthorizedAccessException) { Console.WriteLine("PASS: yetkisiz kara liste yönetimi reddedildi"); }

var dbPath = Path.Combine(Path.GetTempPath(), $"rentacar-check-{Guid.NewGuid():N}.db");
try
{
    var concurrentOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite($"Data Source={dbPath};Default Timeout=10")
        .Options;
    IDbContextFactory<ApplicationDbContext> concurrentFactory = new TestContextFactory(concurrentOptions, services);
    await using (var context = await concurrentFactory.CreateDbContextAsync())
    {
        await context.Database.EnsureCreatedAsync();
        context.Organizasyonlar.Add(new Organizasyon { Id = 1, Adi = "Test organizasyonu", Kod = "RC-ORG" });
        context.Firmalar.Add(new Firma { Id = 1, OrganizasyonId = 1, FirmaKodu = "RC-F1", FirmaAdi = "Birinci firma" });
        context.Cariler.Add(new Cari { Id = 101, FirmaId = 1, CariKodu = "RC-101", Unvan = "Birinci müşteri", CariTipi = CariTipi.Musteri });
        context.Araclar.Add(new Arac { Id = 201, FirmaId = 1, SaseNo = "RC-201", AktifPlaka = "34 ABC 01" });
        await context.SaveChangesAsync();
    }

    var concurrentService = new RentACarRezervasyonServisi(concurrentFactory, firma);
    async Task<bool> TryReserveAsync()
    {
        try
        {
            await concurrentService.RezervasyonOlusturAsync(Request(101, 201, start, start.AddDays(1)));
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException or SqliteException)
        {
            return false;
        }
    }

    var results = await Task.WhenAll(TryReserveAsync(), TryReserveAsync());
    await using (var context = await concurrentFactory.CreateDbContextAsync())
    {
        var count = await context.MusteriKiralamalar.CountAsync();
        if (results.Count(x => x) != 1 || count != 1)
        {
            throw new Exception($"Eşzamanlı rezervasyon sonucu hatalı: başarılı={results.Count(x => x)}, kayıt={count}.");
        }
    }
    Console.WriteLine("PASS: eşzamanlı aynı araç rezervasyonlarından yalnızca biri kaydedildi");
}
finally
{
    SqliteConnection.ClearAllPools();
    File.Delete(dbPath);
}

Console.WriteLine("Rent a Car rezervasyon kontrolleri başarılı.");

sealed class TestContextFactory(DbContextOptions<ApplicationDbContext> options, IServiceProvider services) : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext()
    {
        var context = new ApplicationDbContext(options);
        context.SetServiceProvider(services);
        return context;
    }
}

sealed class TestAuthenticationStateProvider : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
{
    public bool Admin { get; set; } = true;
    public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "7"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, Admin ? SistemRolleri.Admin : "Kullanici")
        }, "Test");
        return Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(new System.Security.Claims.ClaimsPrincipal(identity)));
    }
}

sealed class TestFirmaProvider : IAktifFirmaProvider
{
    public int? AktifFirmaId { get; set; }
    public bool HasAktifFirma => AktifFirmaId is > 0;
    public bool TumFirmalar => false;
    public AktifFirmaBilgisi Mevcut => default!;
    public event Action? AktifFirmaDegisti { add { } remove { } }
    public void Set(AktifFirmaBilgisi firma) => throw new NotSupportedException();
    public void SetTumFirmalar(bool tumFirmalar) => throw new NotSupportedException();
    public void SetDonem(int yil, int ay) => throw new NotSupportedException();
    public Task<bool> TryRestoreAsync() => Task.FromResult(false);
}
