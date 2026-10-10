using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class InvoiceStartupIndexTests
{
    [Fact]
    public async Task Startup_replaces_legacy_index_only_after_new_unique_index_is_created()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await CreateLegacyInvoiceTableAsync(connection, duplicateRows: false);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await RunIndexSetupAsync(context);

        Assert.True(await IndexExistsAsync(connection, "IX_Faturalar_FirmaId_FaturaYonu_FaturaNo"));
        Assert.False(await IndexExistsAsync(connection, "IX_Faturalar_FaturaNo"));
    }

    [Fact]
    public async Task Startup_keeps_legacy_index_and_rolls_back_when_old_rows_violate_new_scope()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await CreateLegacyInvoiceTableAsync(connection, duplicateRows: true);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);

        await Assert.ThrowsAsync<SqliteException>(() => RunIndexSetupAsync(context));

        Assert.True(await IndexExistsAsync(connection, "IX_Faturalar_FaturaNo"));
        Assert.False(await IndexExistsAsync(connection, "IX_Faturalar_FirmaId_FaturaYonu_FaturaNo"));
    }

    [Fact]
    public void Retry_reset_clears_generated_ids_and_detached_navigation_graph()
    {
        var fatura = new Fatura
        {
            Id = 42,
            FaturaNo = "INV-42",
            FaturaTarihi = DateTime.Today,
            FirmaId = 7,
            Firma = new Firma { Id = 7, FirmaAdi = "Firma A" },
            CariId = 9,
            Cari = new Cari { Id = 9, Unvan = "Cari A" },
            KarsiFirma = new Firma { Id = 11, FirmaAdi = "Karsi Firma" },
            MuhasebeFisId = 99,
            MuhasebeFisiOlusturuldu = true,
            FaturaKalemleri = new List<FaturaKalem>()
        };

        var kalem = new FaturaKalem
        {
            Id = 17,
            FaturaId = 42,
            Fatura = fatura,
            Firma = new Firma { Id = 7, FirmaAdi = "Firma A" },
            MuhasebeHesap = new MuhasebeHesap { Id = 3, HesapKodu = "600" },
            Arac = new Arac { Id = 101, Plaka = "34 ABC 123" },
            Aciklama = "Kalem"
        };
        fatura.FaturaKalemleri.Add(kalem);

        var method = typeof(FaturaService).GetMethod("ResetNewInvoiceGraphForRetry", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(FaturaService), "ResetNewInvoiceGraphForRetry");

        method.Invoke(null, [fatura]);

        Assert.Equal(0, fatura.Id);
        Assert.Null(fatura.MuhasebeFisId);
        Assert.False(fatura.MuhasebeFisiOlusturuldu);
        Assert.Null(fatura.Cari);
        Assert.Null(fatura.Firma);
        Assert.Null(fatura.KarsiFirma);

        Assert.Single(fatura.FaturaKalemleri);
        var resetKalem = Assert.IsType<FaturaKalem>(fatura.FaturaKalemleri.Single());
        Assert.Equal(0, resetKalem.Id);
        Assert.Equal(0, resetKalem.FaturaId);
        Assert.Null(resetKalem.Fatura);
        Assert.Null(resetKalem.Firma);
        Assert.Null(resetKalem.MuhasebeHesap);
        Assert.Null(resetKalem.Arac);
    }

    private static async Task RunIndexSetupAsync(ApplicationDbContext context)
    {
        var method = typeof(DbInitializer).GetMethod(
            "EnsureFaturaFirmaYonUniqueIndexAsync",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(DbInitializer), "EnsureFaturaFirmaYonUniqueIndexAsync");

        var task = (Task?)method.Invoke(null,
            [context, "SQLite", new ConfigurationBuilder().Build()])
            ?? throw new InvalidOperationException("İndeks kurulum görevi oluşturulamadı.");
        await task;
    }

    private static async Task CreateLegacyInvoiceTableAsync(SqliteConnection connection, bool duplicateRows)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Faturalar (
                Id INTEGER PRIMARY KEY,
                FirmaId INTEGER NOT NULL,
                FaturaYonu INTEGER NOT NULL,
                FaturaNo TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            CREATE UNIQUE INDEX IX_Faturalar_FaturaNo ON Faturalar (FaturaNo);
            """;
        if (duplicateRows)
        {
            command.CommandText += """
                DROP INDEX IX_Faturalar_FaturaNo;
                CREATE INDEX IX_Faturalar_FaturaNo ON Faturalar (FaturaNo);
                INSERT INTO Faturalar (Id, FirmaId, FaturaYonu, FaturaNo) VALUES
                    (1, 1, 0, 'INV-1'), (2, 1, 0, 'INV-1');
                """;
        }
        else
        {
            command.CommandText += """
                INSERT INTO Faturalar (Id, FirmaId, FaturaYonu, FaturaNo) VALUES
                    (1, 1, 0, 'INV-1');
                """;
        }
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> IndexExistsAsync(SqliteConnection connection, string indexName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'index' AND name = $name";
        command.Parameters.AddWithValue("$name", indexName);
        return await command.ExecuteScalarAsync() is not null;
    }
}
