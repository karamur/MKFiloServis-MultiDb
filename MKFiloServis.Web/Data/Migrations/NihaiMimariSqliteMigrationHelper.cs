using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// Applies the optional/idempotent parts of NihaiMimari_OrganizasyonSubeHolding
/// against SQLite's live catalog. SQLite has no procedural DDL, so this runs between
/// the preceding and target EF migrations and records the target only after success.
/// </summary>
internal static class NihaiMimariSqliteMigrationHelper
{
    internal const string MigrationId = "20260604204018_NihaiMimari_OrganizasyonSubeHolding";

    private static readonly string[] DeletedAtTables =
    {
        "AktiviteLoglar", "Araclar", "AracAlimSatimlar", "AracBakimUyarilari",
        "AracBolgeAtamalar", "AracBolgeler", "AracEvrakDosyaVersiyonlar", "AracEvrakDosyalari",
        "AracEvraklari", "AracIlanIcerikleri", "AracIlanYayinlar", "AracIlanlari", "AracIslemler",
        "AracKonumlar", "AracMaliyetSnapshotlari", "AracMarkaModeller", "AracMarkalari",
        "AracMasraflari", "AracModelleri", "AracOperasyonDurumlari", "AracPlakalar", "AracSatislari",
        "AracTakipAlarmlar", "AracTakipCihazlar", "AylikChecklistler", "AylikOdemeGerceklesenler",
        "AylikOdemePlanlari", "BakimPeriyotlar", "BankaHesaplari", "BankaKasaHareketleri",
        "BildirimAyarlari", "Bildirimler", "BordroAyarlar", "BordroDetaylar", "BordroOdemeler",
        "Bordrolar", "BudgetHedefler", "BudgetMasrafKalemleri", "BudgetOdemeler", "CariHatirlatmalar",
        "CariIletisimNotlar", "CariSeferUcretleri", "Cariler", "ChecklistKalemleri", "DashboardWidgetlar",
        "DestekAyarlari", "DestekBilgiBankasiMakaleleri", "DestekDepartmanUyeleri", "DestekDepartmanlari",
        "DestekHazirYanitlari", "DestekKategorileri", "DestekSlaListesi", "DestekTalebiAktiviteleri",
        "DestekTalebiEkleri", "DestekTalebiIliskileri", "DestekTalebiYanitlari", "DestekTalepleri",
        "EbysAramaGecmisleri", "EbysBelgeEmbeddingler", "EbysEvrakAtamalar", "EbysEvrakDosyaVersiyonlar",
        "EbysEvrakDosyalar", "EbysEvrakHareketler", "EbysEvrakKategoriler", "EbysEvraklar",
        "EbysKayitliAramalar", "EmailAyarlari", "EpostaBildirimLoglari", "FaturaKalemleri", "FaturaSablonlari",
        "Faturalar", "FiloGunlukPuantajlar", "FiloGuzergahEslestirmeleri", "FirmaAracSoforEslestirmeleri",
        "FirmaGuzergahEslestirmeleri", "Firmalar", "FirmalarArasiTransferler", "GunlukPuantajlar",
        "GuzergahSeferleri", "Guzergahlar", "HakedisDetaylari", "Hakedisler", "Hatirlaticilar",
        "IhaleGuzergahKalemleri", "IhaleProjeleri", "IhaleRakipBenchmarklar", "IhaleSozlesmeRevizyonlari",
        "IhaleTeklifKararLoglari", "IhaleTeklifVersiyonlari", "IlanPlatformlari", "Kapasiteler",
        "KdvHesapEslestirmeleri", "KiralamaAraclar", "KiralikCPlakaTakipler", "KiralikPlakaTakipler",
        "KostMerkezleri", "KullaniciCariler", "KullaniciSonIslemler", "KullaniciTercihleri", "Kullanicilar",
        "Kurumlar", "LastikDegisimler", "LastikDepolar", "LastikSezonAyarlari", "LastikStoklar", "Lisanslar",
        "MasrafKalemleri", "Mesajlar", "MuhasebeAyarlari", "MuhasebeDonemleri", "MuhasebeFisKalemleri",
        "MuhasebeFisleri", "MuhasebeHesaplari", "MuhasebeProjeler", "MusteriKiralamalar", "OdemeEslestirmeleri",
        "OperasyonKayitlari", "OzlukEvrakTanimlari", "PersonelAracAtamalari", "PersonelAvansMahsuplar",
        "PersonelAvanslar", "PersonelBorcOdemeler", "PersonelBorclar", "PersonelFinansAyarlar",
        "PersonelIzinHaklari", "PersonelIzinleri", "PersonelMaaslari", "PersonelOzlukEvrakVersiyonlar",
        "PersonelOzlukEvraklar", "PersonelPuantajlar", "Personeller", "PiyasaArastirmaIlanlar",
        "PiyasaArastirmalar", "PiyasaIlanlari", "PlakaDonusumler", "ProformaFaturaKalemler",
        "ProformaFaturalar", "PuantajAuditLogs", "PuantajDetaylari", "PuantajEslestirmeOnerileri",
        "PuantajExcelImportlar", "PuantajFinansalKayitlar", "PuantajHesapDonemleri", "PuantajJobExecutions",
        "PuantajKayitlar", "RolYetkileri", "Roller", "SatisPersonelleri", "ServisCalismaKiralamalar",
        "ServisCalismalari", "ServisKayitlari", "ServisKontratlar", "ServisOdemeler", "ServisParcalar",
        "ServisPuantajlar", "ServisTahsilatlar", "SmsAyarlari", "SmsLoglari", "SmsSablonlari",
        "StokHareketler", "StokKartlari", "StokKategoriler", "TasimaTedarikciIsler", "TasimaTedarikciler",
        "TedarikciEvrakDosyalari", "TedarikciEvraklari", "TekrarlayanOdemeler", "WebhookEndpointler",
        "WebhookLoglar", "WhatsAppAyarlari", "WhatsAppGrupUyeler", "WhatsAppGruplar", "WhatsAppKisiler",
        "WhatsAppMesajlar", "WhatsAppSablonlar"
    };

    private static readonly string[] RequiredFirmaTables =
    {
        "BordroAyarlar", "BordroDetaylar", "Bordrolar", "CariHatirlatmalar", "IhaleProjeleri",
        "PersonelAvanslar", "PersonelBorclar", "PersonelFinansAyarlar", "ProformaFaturalar",
        "PuantajAuditLogs", "TekrarlayanOdemeler"
    };

    private static readonly (string Table, string OnDelete)[] FirmaForeignKeys =
    {
        ("BordroAyarlar", "CASCADE"), ("BordroDetaylar", "CASCADE"), ("Bordrolar", "CASCADE"),
        ("CariHatirlatmalar", "CASCADE"), ("FiloGunlukPuantajlar", "RESTRICT"),
        ("FiloGuzergahEslestirmeleri", "RESTRICT"), ("FirmaAracSoforEslestirmeleri", "CASCADE"),
        ("FirmaGuzergahEslestirmeleri", "CASCADE"), ("IhaleProjeleri", "CASCADE"),
        ("MusteriKiralamalar", "CASCADE"), ("OperasyonKayitlari", "CASCADE"),
        ("PersonelAvanslar", "CASCADE"), ("PersonelBorclar", "CASCADE"),
        ("PersonelFinansAyarlar", "CASCADE"), ("PuantajAuditLogs", "CASCADE"),
        ("PuantajDetaylari", "CASCADE"), ("PuantajFinansalKayitlar", "CASCADE"),
        ("PuantajHesapDonemleri", "CASCADE"), ("PuantajJobExecutions", "CASCADE")
    };

    internal static async Task ApplyAsync(ApplicationDbContext context)
    {
        if (!context.Database.IsSqlite()) return;

        await using var transaction = await context.Database.BeginTransactionAsync();
        var connection = context.Database.GetDbConnection();
        var dbTransaction = transaction.GetDbTransaction();

        await ExecuteAsync(connection, dbTransaction, """
            CREATE TABLE IF NOT EXISTS "Organizasyonlar" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "Adi" TEXT NOT NULL,
                "Kod" TEXT NULL,
                "Aciklama" TEXT NULL,
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "UpdatedAt" TEXT NULL,
                "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                "DeletedAt" TEXT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Organizasyonlar_Kod"
                ON "Organizasyonlar" ("Kod") WHERE "IsDeleted" = 0;
            CREATE TABLE IF NOT EXISTS "Subeler" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "FirmaId" INTEGER NOT NULL,
                "SubeAdi" TEXT NOT NULL,
                "SubeKodu" TEXT NULL,
                "Adres" TEXT NULL,
                "Telefon" TEXT NULL,
                "Aktif" INTEGER NOT NULL DEFAULT 1,
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "UpdatedAt" TEXT NULL,
                "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                "DeletedAt" TEXT NULL,
                CONSTRAINT "FK_Subeler_Firmalar_FirmaId"
                    FOREIGN KEY ("FirmaId") REFERENCES "Firmalar" ("Id") ON DELETE RESTRICT);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Subeler_FirmaId_SubeKodu"
                ON "Subeler" ("FirmaId", "SubeKodu") WHERE "IsDeleted" = 0;
            CREATE TABLE IF NOT EXISTS "HoldingVeriler" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "FirmaId" INTEGER NOT NULL, "FirmaKodu" TEXT NOT NULL, "FirmaAdi" TEXT NOT NULL,
                "Yil" INTEGER NOT NULL, "Ay" INTEGER NOT NULL, "Kategori" TEXT NOT NULL,
                "ToplamGelir" NUMERIC NOT NULL DEFAULT 0, "ToplamGider" NUMERIC NOT NULL DEFAULT 0,
                "Kar" NUMERIC NOT NULL DEFAULT 0, "ButceHedef" NUMERIC NULL,
                "ButceGerceklesen" NUMERIC NULL, "AraclarMaliyet" NUMERIC NULL,
                "PersonelMaliyet" NUMERIC NULL, "HakedisToplam" NUMERIC NULL,
                "OdenmemisFaturaToplam" NUMERIC NULL, "AktifAracSayisi" INTEGER NULL,
                "PersonelSayisi" INTEGER NULL, "JsonDetay" TEXT NULL,
                "OlusturmaTarihi" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_HoldingVeriler_FirmaId_Yil_Ay_Kategori"
                ON "HoldingVeriler" ("FirmaId", "Yil", "Ay", "Kategori");
            CREATE TABLE IF NOT EXISTS "HoldingRaporlar" (
                "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                "Ad" TEXT NOT NULL, "Tip" TEXT NOT NULL, "Yil" INTEGER NOT NULL,
                "Ay" INTEGER NULL, "JsonFiltreler" TEXT NULL, "JsonSonuc" TEXT NULL,
                "OlusturmaTarihi" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "OlusturanKullanici" TEXT NULL);
            """);

        if (!await HasForeignKeyAsync(connection, dbTransaction, "Subeler", "FirmaId", "Firmalar", "Id"))
        {
            if (await HasOrphanRowsAsync(connection, dbTransaction, "Subeler", "FirmaId", "Firmalar", "Id"))
                throw new InvalidOperationException("Subeler.FirmaId içinde eşleşmeyen firma var; SQLite FK kurulumu durduruldu.");
            await EnsureForeignKeyTriggersAsync(connection, dbTransaction, "Subeler", "FirmaId", "Firmalar", "Id", "RESTRICT", required: true);
        }

        foreach (var (name, code) in new[]
        {
            ("Ustun Holding", "USTUNHOLDING"), ("Ustun Grup", "USTUNGRUP"),
            ("Ustun Filo", "USTUNFILO"), ("Recep Ustun", "RECEPUSTUN")
        })
        {
            await ExecuteAsync(connection, dbTransaction, """
                INSERT INTO "Organizasyonlar" ("Adi", "Kod", "IsDeleted", "CreatedAt")
                SELECT $name, $code, 0, CURRENT_TIMESTAMP
                WHERE NOT EXISTS (SELECT 1 FROM "Organizasyonlar" WHERE "Kod" = $code);
                """, ("$name", name), ("$code", code));
        }

        if (await TableExistsAsync(connection, dbTransaction, "Firmalar"))
        {
            if (!await ColumnExistsAsync(connection, dbTransaction, "Firmalar", "OrganizasyonId"))
                await ExecuteAsync(connection, dbTransaction,
                    "ALTER TABLE \"Firmalar\" ADD COLUMN \"OrganizasyonId\" INTEGER NOT NULL DEFAULT 1;");
            await ExecuteAsync(connection, dbTransaction, """
                CREATE INDEX IF NOT EXISTS "IX_Firmalar_OrganizasyonId" ON "Firmalar" ("OrganizasyonId");
                UPDATE "Firmalar"
                SET "OrganizasyonId" = (SELECT "Id" FROM "Organizasyonlar" WHERE "Kod" = 'USTUNHOLDING' LIMIT 1)
                WHERE "OrganizasyonId" = 0 OR "OrganizasyonId" IS NULL
                   OR NOT EXISTS (SELECT 1 FROM "Organizasyonlar" o WHERE o."Id" = "Firmalar"."OrganizasyonId");
                """);
            if (await HasOrphanRowsAsync(connection, dbTransaction, "Firmalar", "OrganizasyonId", "Organizasyonlar", "Id"))
                throw new InvalidOperationException("Firmalar.OrganizasyonId içinde eşleşmeyen organizasyon var; SQLite FK kurulumu durduruldu.");
            await EnsureForeignKeyTriggersAsync(connection, dbTransaction, "Firmalar", "OrganizasyonId", "Organizasyonlar", "Id", "RESTRICT", required: true);
        }

        foreach (var table in DeletedAtTables)
        {
            if (await TableExistsAsync(connection, dbTransaction, table) &&
                !await ColumnExistsAsync(connection, dbTransaction, table, "DeletedAt"))
                await ExecuteAsync(connection, dbTransaction,
                    $"ALTER TABLE {Q(table)} ADD COLUMN \"DeletedAt\" TEXT NULL;");
        }

        var firstFirmId = await ScalarAsync(connection, dbTransaction,
            "SELECT \"Id\" FROM \"Firmalar\" WHERE NOT \"IsDeleted\" ORDER BY \"Id\" LIMIT 1;");
        if (firstFirmId is not null and not DBNull)
        {
            foreach (var table in RequiredFirmaTables)
            {
                if (!await TableExistsAsync(connection, dbTransaction, table)) continue;
                if (!await ColumnExistsAsync(connection, dbTransaction, table, "FirmaId"))
                    await ExecuteAsync(connection, dbTransaction,
                        $"ALTER TABLE {Q(table)} ADD COLUMN \"FirmaId\" INTEGER NULL;");
                await ExecuteAsync(connection, dbTransaction,
                    $"UPDATE {Q(table)} SET \"FirmaId\" = $id WHERE \"FirmaId\" IS NULL OR \"FirmaId\" = 0;",
                    ("$id", firstFirmId));
                await EnsureRequiredColumnTriggerAsync(connection, dbTransaction, table, "FirmaId");
            }
        }

        foreach (var (table, onDelete) in FirmaForeignKeys)
        {
            if (!await TableExistsAsync(connection, dbTransaction, table) ||
                !await ColumnExistsAsync(connection, dbTransaction, table, "FirmaId")) continue;
            if (await HasOrphanRowsAsync(connection, dbTransaction, table, "FirmaId", "Firmalar", "Id"))
                throw new InvalidOperationException($"{table}.FirmaId içinde eşleşmeyen firma var; SQLite FK kurulumu durduruldu.");
            if (!await HasForeignKeyAsync(connection, dbTransaction, table, "FirmaId", "Firmalar", "Id"))
                await EnsureForeignKeyTriggersAsync(connection, dbTransaction, table, "FirmaId", "Firmalar", "Id", onDelete,
                    RequiredFirmaTables.Contains(table, StringComparer.OrdinalIgnoreCase));
        }

        foreach (var table in new[] { "PuantajFinansalKayitlar", "PuantajDetaylari", "MusteriKiralamalar", "FiloGuzergahEslestirmeleri", "FiloGunlukPuantajlar" })
        {
            if (await TableExistsAsync(connection, dbTransaction, table) && await ColumnExistsAsync(connection, dbTransaction, table, "FirmaId"))
                await ExecuteAsync(connection, dbTransaction,
                    $"CREATE INDEX IF NOT EXISTS {Q($"IX_{table}_FirmaId")} ON {Q(table)} (\"FirmaId\");");
        }

        await transaction.CommitAsync();
    }

    internal static string TriggerName(string table, string column, string principal) => $"__ef_fk_{table}_{column}_{principal}";

    private static async Task EnsureForeignKeyTriggersAsync(DbConnection connection, DbTransaction transaction,
        string table, string column, string principal, string principalColumn, string onDelete, bool required)
    {
        var trigger = TriggerName(table, column, principal);
        var invalidReference = required
            ? $"NEW.{Q(column)} IS NULL OR NOT EXISTS (SELECT 1 FROM {Q(principal)} WHERE {Q(principalColumn)} = NEW.{Q(column)})"
            : $"NEW.{Q(column)} IS NOT NULL AND NOT EXISTS (SELECT 1 FROM {Q(principal)} WHERE {Q(principalColumn)} = NEW.{Q(column)})";
        await ExecuteAsync(connection, transaction, $"""
            CREATE TRIGGER IF NOT EXISTS {Q(trigger + "_ins")}
            BEFORE INSERT ON {Q(table)}
            WHEN {invalidReference}
            BEGIN SELECT RAISE(ABORT, 'Foreign key {table}.{column} is invalid'); END;
            CREATE TRIGGER IF NOT EXISTS {Q(trigger + "_upd")}
            BEFORE UPDATE OF {Q(column)} ON {Q(table)}
            WHEN {invalidReference}
            BEGIN SELECT RAISE(ABORT, 'Foreign key {table}.{column} is invalid'); END;
            """);

        if (string.Equals(onDelete, "CASCADE", StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteAsync(connection, transaction, $"""
                CREATE TRIGGER IF NOT EXISTS {Q(trigger + "_del")}
                AFTER DELETE ON {Q(principal)}
                BEGIN DELETE FROM {Q(table)} WHERE {Q(column)} = OLD.{Q(principalColumn)}; END;
                """);
        }
        else
        {
            await ExecuteAsync(connection, transaction, $"""
                CREATE TRIGGER IF NOT EXISTS {Q(trigger + "_del")}
                BEFORE DELETE ON {Q(principal)}
                WHEN EXISTS (SELECT 1 FROM {Q(table)} WHERE {Q(column)} = OLD.{Q(principalColumn)})
                BEGIN SELECT RAISE(ABORT, 'Delete restricted by {table}.{column}'); END;
                """);
        }
    }

    private static async Task EnsureRequiredColumnTriggerAsync(DbConnection connection, DbTransaction transaction, string table, string column)
    {
        var name = Q($"__required_{table}_{column}");
        await ExecuteAsync(connection, transaction, $"""
            CREATE TRIGGER IF NOT EXISTS {name}_ins BEFORE INSERT ON {Q(table)}
            WHEN NEW.{Q(column)} IS NULL BEGIN SELECT RAISE(ABORT, '{table}.{column} is required'); END;
            CREATE TRIGGER IF NOT EXISTS {name}_upd BEFORE UPDATE OF {Q(column)} ON {Q(table)}
            WHEN NEW.{Q(column)} IS NULL BEGIN SELECT RAISE(ABORT, '{table}.{column} is required'); END;
            """);
    }

    private static async Task<bool> HasForeignKeyAsync(DbConnection connection, DbTransaction transaction,
        string table, string from, string principal, string to)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA foreign_key_list({Q(table)})";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            if (string.Equals(reader.GetString(2), principal, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(reader.GetString(3), from, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(reader.GetString(4), to, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static async Task<bool> HasOrphanRowsAsync(DbConnection connection, DbTransaction transaction,
        string table, string from, string principal, string to)
    {
        var result = await ScalarAsync(connection, transaction, $"""
            SELECT 1 FROM {Q(table)} child
            LEFT JOIN {Q(principal)} parent ON parent.{Q(to)} = child.{Q(from)}
            WHERE child.{Q(from)} IS NOT NULL AND parent.{Q(to)} IS NULL
            LIMIT 1;
            """);
        return result is not null and not DBNull;
    }

    private static async Task<bool> TableExistsAsync(DbConnection c, DbTransaction t, string table) =>
        await ScalarAsync(c, t, "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;", ("$name", table)) is not null;

    private static async Task<bool> ColumnExistsAsync(DbConnection c, DbTransaction t, string table, string column)
    {
        await using var command = c.CreateCommand();
        command.Transaction = t;
        command.CommandText = $"PRAGMA table_info({Q(table)})";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static async Task<object?> ScalarAsync(DbConnection c, DbTransaction t, string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = c.CreateCommand();
        command.Transaction = t;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
        return await command.ExecuteScalarAsync();
    }

    private static async Task ExecuteAsync(DbConnection c, DbTransaction t, string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = c.CreateCommand();
        command.Transaction = t;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
        await command.ExecuteNonQueryAsync();
    }

    private static string Q(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
