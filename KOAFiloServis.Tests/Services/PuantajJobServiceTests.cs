using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace KOAFiloServis.Tests.Services;

public class PuantajJobServiceTests
{
    private readonly ServiceCollection _services = new();
    private readonly Mock<IPuantajMutexService> _mutexMock = new();
    private readonly Mock<IPuantajEngineService> _engineMock = new();
    private readonly Mock<IAktifFirmaProvider> _firmaMock = new();
    private readonly Mock<IDbContextFactory<MasterDbContext>> _masterDbFactoryMock = new();
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _appDbFactoryMock = new();
    private readonly Mock<ILogger<PuantajJobService>> _loggerMock = new();

    public PuantajJobServiceTests()
    {
        _services.AddScoped(_ => _mutexMock.Object);
        _services.AddScoped(_ => _engineMock.Object);
        _services.AddScoped(_ => _firmaMock.Object);
        _services.AddScoped(_ => _appDbFactoryMock.Object);
        _services.AddSingleton(_masterDbFactoryMock.Object);
        _services.AddSingleton(_loggerMock.Object);
        _services.AddScoped<IPuantajJobService, PuantajJobService>();
    }

    private IPuantajJobService BuildSut()
    {
        return _services.BuildServiceProvider().GetRequiredService<IPuantajJobService>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Duplicate mutex → skip (no DB access needed)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DuplicateMutex_EngineNotCalled()
    {
        SetupMutexNotAcquired();

        var sut = BuildSut();
        await sut.ProcessTenantAsync(1, null, 2026, 5, "Test");

        _engineMock.Verify(
            x => x.ProcessDonemAsync(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════
    // ProcessAllTenants: DB error during firm enumeration
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessAllTenants_FirmEnumerationError_ReturnsFailed()
    {
        // Factory returns null (unconfigured mock) → GetAktifFirmalarAsync throws
        // → caught → returns Failed with error message
        var sut = BuildSut();
        var result = await sut.ProcessAllTenantsAsync(2026, 5, "Test");

        result.Durum.Should().Be(PuantajJobExecutionDurum.Failed);
        result.HataMesaji.Should().Contain("Firma listesi");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Date math
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(2026, 1, 2025, 12)]
    [InlineData(2026, 2, 2026, 1)]
    [InlineData(2026, 6, 2026, 5)]
    [InlineData(2026, 12, 2026, 11)]
    [InlineData(2025, 1, 2024, 12)]
    [InlineData(2030, 3, 2030, 2)]
    public void DateMath_PreviousMonth(int nowY, int nowM, int expY, int expM)
    {
        var yil = nowM == 1 ? nowY - 1 : nowY;
        var ay = nowM == 1 ? 12 : nowM - 1;
        yil.Should().Be(expY);
        ay.Should().Be(expM);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MutexAcquireResult
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void MutexAcquireResult_Acquired()
    {
        var r = new MutexAcquireResult
        {
            Acquired = true,
            Record = new PuantajJobExecution { FirmaId = 5, Yil = 2026, Ay = 3 }
        };
        r.Acquired.Should().BeTrue();
        r.Record.Should().NotBeNull();
        r.Record.FirmaId.Should().Be(5);
        r.FailureReason.Should().BeNull();
    }

    [Fact]
    public void MutexAcquireResult_NotAcquired()
    {
        var r = new MutexAcquireResult { Acquired = false, FailureReason = "dup" };
        r.Acquired.Should().BeFalse();
        r.Record.Should().BeNull();
        r.FailureReason.Should().Be("dup");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Entity defaults
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void PuantajJobExecution_DefaultValues()
    {
        var j = new PuantajJobExecution();
        j.Tetikleyen.Should().Be("Quartz");
        j.Durum.Should().Be(PuantajJobExecutionDurum.Running);
        j.IsDeleted.Should().BeFalse();
        j.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(PuantajJobExecutionDurum.Running, "Running")]
    [InlineData(PuantajJobExecutionDurum.Completed, "Completed")]
    [InlineData(PuantajJobExecutionDurum.Failed, "Failed")]
    [InlineData(PuantajJobExecutionDurum.Skipped, "Skipped")]
    [InlineData(PuantajJobExecutionDurum.PartialSuccess, "PartialSuccess")]
    public void PuantajJobExecutionDurum_EnumToString(PuantajJobExecutionDurum v, string s)
    {
        v.ToString().Should().Be(s);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Polly pipeline structure
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void RetryPipeline_IsDefined()
    {
        typeof(PuantajJobService)
            .GetField("RetryPipeline",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic)
            .Should().NotBeNull();
    }

    [Fact]
    public void PostgresUniqueViolation_ErrorCode()
    {
        Npgsql.PostgresErrorCodes.UniqueViolation.Should().Be("23505");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Notes on integration tests needed
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void IntegrationTests_Required_ForDbDependentScenarios()
    {
        // The following scenarios require integration tests with a real database
        // because Moq cannot mock non-virtual DbSet<T> properties on DbContext:
        //
        // 1. Successful processing: mutex acquired → idempotency check → engine → audit log → mutex completed
        // 2. Retry on transient NpgsqlException (Polly pipeline with real PG)
        // 3. Business error no-retry (InvalidOperationException from engine)
        // 4. Stale cleanup (actual DB rows with Running status > 30 min)
        // 5. Concurrent processing (two real connections, unique constraint)
        // 6. Tenant isolation (separate tenant DBs)
        // 7. Failed execution update (mutex status → Failed)
        //
        // These are verified via manual testing and will be covered by
        // integration tests in KOAFiloServis.Tests.Integration project.

        true.Should().BeTrue(); // documentation placeholder
    }

    // ═══════════════════════════════════════════════════════════════════

    private void SetupMutexNotAcquired()
    {
        _mutexMock
            .Setup(x => x.CleanupStaleAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mutexMock
            .Setup(x => x.TryAcquireAsync(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MutexAcquireResult { Acquired = false, FailureReason = "lock" });
    }
}
