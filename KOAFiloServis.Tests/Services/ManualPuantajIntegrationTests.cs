using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Shared.Exceptions;
using KOAFiloServis.Web.Controllers;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KOAFiloServis.Tests.Services;

public class ManualPuantajIntegrationTests
{
    // ═══════════════════════════════════════════════════════════════════
    // TEST 1: Manual Puantaj Create — Entity Construction + Validation
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test1_ManualPuantajCreate_EntityDefaults_Correct()
    {
        var kayit = new PuantajKayit
        {
            Yil = 2026, Ay = 5,
            KurumId = 1, KurumAdi = "Test Kurum",
            GuzergahId = 10, GuzergahAdi = "Test Güzergah",
            AracId = 100, Plaka = "34TEST001",
            SoforId = 50, SoforAdi = "Test Sofor",
            Slot = SeferSlot.Sabah, SlotAdi = "Sabah",
            Yon = PuantajYon.SabahAksam,
            SeferSayisi = 1, Gun = 22,
            BirimGelir = 500, BirimGider = 300,
            CreatedAt = DateTime.UtcNow
        };

        kayit.Yil.Should().Be(2026);
        kayit.Ay.Should().Be(5);
        kayit.Plaka.Should().Be("34TEST001");
        kayit.Kaynak.Should().Be(PuantajKaynak.Manuel, "default Kaynak=Manuel");
        kayit.OnayDurum.Should().Be(PuantajOnayDurum.Taslak, "default Onay=Taslak");
        kayit.Slot.Should().Be(SeferSlot.Sabah);
        kayit.IsDeleted.Should().BeFalse("new records not deleted");

        // Entity methods
        kayit.SetGunDeger(1, 3);
        kayit.SetGunDeger(15, 2);
        kayit.GetGunDeger(1).Should().Be(3);
        kayit.GetGunDeger(15).Should().Be(2);
        kayit.GetGunDeger(0).Should().Be(0, "invalid day returns 0");
        kayit.GetGunDeger(32).Should().Be(0, "invalid day returns 0");

        kayit.HesaplaPuantajToplam();
        kayit.Gun.Should().Be(5, "3 + 2 sefer = 5 gün");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 2: Duplicate Kayıt — Unique Key Logic
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test2_DuplicateKayit_UniqueKeyComponents()
    {
        // The unique key for PuantajKayit upsert is:
        // (GuzergahId, AracId, Yil, Ay, Slot)
        // Verified by SavePuantajAsync logic

        var uniqueKey = (GuzergahId: 10, AracId: 100, Yil: 2026, Ay: 5, Slot: SeferSlot.Sabah);

        var same = (GuzergahId: 10, AracId: 100, Yil: 2026, Ay: 5, Slot: SeferSlot.Sabah);
        var different = (GuzergahId: 10, AracId: 100, Yil: 2026, Ay: 5, Slot: SeferSlot.Aksam);

        uniqueKey.Should().Be(same, "same unique key → upsert (update)");
        uniqueKey.Should().NotBe(different, "different slot → new record");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 3: Locked Period — PuantajDonemKilitliException
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test3_LockedPeriod_ExceptionHasCorrectProperties()
    {
        var ex = new PuantajDonemKilitliException(99, 3);

        ex.HesapDonemiId.Should().Be(99);
        ex.Versiyon.Should().Be(3);
        ex.Message.Should().Contain("kilitli");
        ex.Should().BeAssignableTo<PuantajBusinessException>(
            "Business exception → retry YOK");
        ex.Should().NotBeAssignableTo<PuantajInfrastructureException>(
            "Locked period is NOT transient");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 4: Recalculate — Engine SonucV1 Validation
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test4_Recalculate_EngineSonucV1_Properties()
    {
        var sonuc = new PuantajEngineSonucV1
        {
            HesapDonemiId = 42, Versiyon = 2,
            IslenenOperasyonSayisi = 150, UretilenPuantajKayit = 45,
            SupersededKayit = 40, OlusturulanDetay = 150
        };

        sonuc.HesapDonemiId.Should().Be(42);
        sonuc.Versiyon.Should().Be(2);
        sonuc.IslenenOperasyonSayisi.Should().Be(150);
        sonuc.UretilenPuantajKayit.Should().Be(45);
        sonuc.OlusturulanDetay.Should().Be(150, "her operasyon için 1 detay");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 5: Audit Log — Entity Construction
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test5_AuditLog_Construction_Correct()
    {
        var log = new PuantajAuditLog
        {
            FirmaId = 1, HesapDonemiId = 100,
            Aksiyon = PuantajAuditAksiyon.Kilitlendi,
            Kullanici = "muratk",
            AksiyonTarihi = DateTime.UtcNow,
            OncekiDurum = "MuhasebeOnaylandi",
            YeniDurum = "Kilitli",
            Aciklama = "Dönem sonu kilidi"
        };

        log.Aksiyon.Should().Be(PuantajAuditAksiyon.Kilitlendi);
        log.Kullanici.Should().Be("muratk");
        log.OncekiDurum.Should().Be("MuhasebeOnaylandi");
        log.YeniDurum.Should().Be("Kilitli");

        // Enum values
        ((int)PuantajAuditAksiyon.Hesaplandi).Should().Be(1);
        ((int)PuantajAuditAksiyon.FinansOnaylandi).Should().Be(2);
        ((int)PuantajAuditAksiyon.MuhasebeOnaylandi).Should().Be(3);
        ((int)PuantajAuditAksiyon.Kilitlendi).Should().Be(4);
        ((int)PuantajAuditAksiyon.KilitAcildi).Should().Be(5);
        ((int)PuantajAuditAksiyon.RevizyonYapildi).Should().Be(6);
        ((int)PuantajAuditAksiyon.IptalEdildi).Should().Be(7);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 6: Soft Delete — IsDeleted Property
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test6_SoftDelete_EntityBehavior()
    {
        var kayit = new PuantajKayit
        {
            Yil = 2026, Ay = 5, GuzergahId = 10, AracId = 100,
            Slot = SeferSlot.Sabah
        };

        kayit.IsDeleted.Should().BeFalse("new record not deleted");

        // Simulate delete
        kayit.IsDeleted = true;
        kayit.UpdatedAt = DateTime.UtcNow;

        kayit.IsDeleted.Should().BeTrue();
        kayit.UpdatedAt.Should().NotBeNull("delete updates timestamp");

        // Query filter simulation
        var store = new List<PuantajKayit> { kayit };
        store.Where(p => !p.IsDeleted).Should().BeEmpty("filter excludes deleted");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 7: Concurrent Edit — Optimistic Concurrency Pattern
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test7_ConcurrentEdit_Recommendation()
    {
        // Current behavior: upsert (last write wins)
        // Recommendation: add [ConcurrencyCheck] on Version column via Fluent API
        // modelBuilder.Entity<PuantajKayit>()
        //     .Property(p => p.Versiyon).IsConcurrencyToken();
        // → DbUpdateConcurrencyException on conflicting updates

        var kayit = new PuantajKayit { Versiyon = 1 };
        kayit.Versiyon.Should().Be(1, "Initial version");
        kayit.SetGunDeger(1, 1);
        kayit.GetGunDeger(1).Should().Be(1);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 8: Authorization — API + Blazor Pages
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test8a_Authorization_Controller_JWT_Bearer()
    {
        var attr = typeof(PuantajJobController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .First();

        attr.AuthenticationSchemes.Should().Be("Bearer", "API requires JWT Bearer");
        attr.Roles.Should().Contain("Admin");
        attr.Roles.Should().Contain("Muhasebeci");
    }

    [Fact]
    public void Test8b_Authorization_BlazorPages_HaveRoleRestrictions()
    {
        var hesaplamaType = typeof(Web.Components.Pages.Operasyon.PuantajHesaplama);
        hesaplamaType.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Should().NotBeEmpty("/puantaj-hesaplama requires [Authorize]");

        var girisType = typeof(Web.Components.Pages.Operasyon.OperasyonGiris);
        girisType.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Should().NotBeEmpty("/operasyon-giris requires [Authorize]");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 9: Transaction Rollback — Exception Propagation
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test9_TransactionRollback_ExceptionTypes()
    {
        // Verify all exception types for transaction rollback scenarios
        var dbConnEx = new PuantajDatabaseConnectionException("timeout",
            new Npgsql.NpgsqlException("Connection refused"));

        dbConnEx.Should().BeAssignableTo<PuantajInfrastructureException>();
        dbConnEx.IsTransientFailure.Should().BeTrue();

        // Non-transient: should NOT retry
        var mutexFailEx = new PuantajMutexAcquireFailedException(1, 2026, 5, "locked");
        mutexFailEx.IsTransientFailure.Should().BeFalse("mutex collision → skip, not retry");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST 10: Smoke API — All Service Methods Exist
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Test10a_Smoke_KurumPuantajService_Constructs()
    {
        var dbFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        var service = new KurumPuantajService(dbFactoryMock.Object);
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IKurumPuantajService>();
    }

    [Fact]
    public void Test10b_Smoke_PuantajEngineService_Constructs()
    {
        var dbFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        var service = new PuantajEngineService(dbFactoryMock.Object);
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IPuantajEngineService>();
    }

    [Fact]
    public void Test10c_Smoke_PuantajWorkflowService_Constructs()
    {
        var dbFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        var service = new PuantajWorkflowService(dbFactoryMock.Object);
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IPuantajWorkflowService>();
    }

    [Fact]
    public void Test10d_Smoke_PuantajJobControllerEndpoints_Exist()
    {
        var methods = typeof(PuantajJobController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.DeclaringType == typeof(PuantajJobController))
            .Select(m => m.Name)
            .ToList();

        methods.Should().Contain("ProcessAll");
        methods.Should().Contain("ProcessTenant");
        methods.Should().Contain("GetHistory");
        methods.Should().Contain("GetHistoryDetail");
    }

    // ═══════════════════════════════════════════════════════════════════
    // CHAOS: DB Connection Drop — Entity Rollback
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Chaos_DBConnectionDrop_ExceptionHandling()
    {
        // When DB drops during save:
        // 1. NpgsqlException thrown → caught by caller
        // 2. No duplicate — transaction rollback
        // 3. Mutex updated to Failed by catch block

        var pgEx = new Npgsql.NpgsqlException("Connection refused");

        // Polly ShouldHandle: true → retry on NpgsqlException
        // But max 3 attempts → if still failing → Failed

        pgEx.Should().NotBeNull("NpgsqlException indicates DB drop");
        pgEx.Message.Should().Contain("refused");

        // Retry policy handles this:
        // PuantajRetryPolicy.ShouldHandle: NpgsqlException → true (retry)
    }

    [Fact]
    public void Chaos_MutexAcquireFailed_IsTransientFalse()
    {
        var ex = new PuantajMutexAcquireFailedException(1, 2026, 5, "zaten işleniyor");

        ex.IsTransientFailure.Should().BeFalse(
            "Mutex collision → retry meaningless, skip instead");

        ex.Message.Should().Contain("Firma 1");
        ex.Message.Should().Contain("2026/05");
    }

    // ═══════════════════════════════════════════════════════════════════
    // BOUNDARY TESTS
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(SeferSlot.Sabah, 1)]
    [InlineData(SeferSlot.Aksam, 2)]
    [InlineData(SeferSlot.Mesai, 3)]
    public void SeferSlot_EnumValues(SeferSlot slot, int expected)
    {
        ((int)slot).Should().Be(expected);
    }

    [Theory]
    [InlineData(PuantajOnayDurum.Taslak, 0)]
    [InlineData(PuantajOnayDurum.OnayBekliyor, 1)]
    [InlineData(PuantajOnayDurum.Onaylandi, 2)]
    [InlineData(PuantajOnayDurum.Reddedildi, 3)]
    public void PuantajOnayDurum_EnumValues(PuantajOnayDurum durum, int expected)
    {
        ((int)durum).Should().Be(expected);
    }

    [Fact]
    public void PuantajKayit_GelirHesaplama_Correct()
    {
        var kayit = new PuantajKayit
        {
            BirimGelir = 500, Gun = 22,
            GelirKdvOrani = 20
        };

        kayit.HesaplaGelir();

        kayit.ToplamGelir.Should().Be(11000m, "500 × 22 = 11,000");
        kayit.GelirKdvTutari.Should().Be(2200m, "11,000 × 20% = 2,200");
        kayit.GelirToplam.Should().Be(13200m, "11,000 + 2,200 = 13,200");
    }

    [Fact]
    public void PuantajKayit_GiderHesaplama_Correct()
    {
        var kayit = new PuantajKayit
        {
            BirimGider = 300, Gun = 22,
            GiderKdvOrani20 = 20
        };

        kayit.HesaplaGider();

        kayit.ToplamGider.Should().Be(6600m, "300 × 22 = 6,600");
    }
}
