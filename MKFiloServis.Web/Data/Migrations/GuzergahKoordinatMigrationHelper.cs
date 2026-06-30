using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// Güzergah entity'sine harita koordinatları eklemek için migration helper
/// </summary>
public static class GuzergahKoordinatMigrationHelper
{
    /// <summary>
    /// Güzergah tablosuna koordinat kolonlarını ekler (SQLite için)
    /// </summary>
    public static async Task ApplyGuzergahKoordinatMigrationAsync(DbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            
            // Kolon var mı kontrol et
            command.CommandText = "PRAGMA table_info(Guzergahlar);";
            var columns = new List<string>();
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(1).ToLower());
                }
            }

            // Eksik kolonları ekle
            var columnsToAdd = new Dictionary<string, string>
            {
                { "baslangiclatitude", "REAL NULL" },
                { "baslangiclongitude", "REAL NULL" },
                { "bitislatitude", "REAL NULL" },
                { "bitislongitude", "REAL NULL" },
                { "rotarengi", "TEXT NULL DEFAULT '#3388ff'" }
            };

            foreach (var col in columnsToAdd)
            {
                if (!columns.Contains(col.Key))
                {
                    using var addCommand = connection.CreateCommand();
                    addCommand.CommandText = $"ALTER TABLE Guzergahlar ADD COLUMN {col.Key} {col.Value};";
                    await addCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"Guzergahlar tablosuna '{col.Key}' kolonu eklendi.");
                }
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    /// <summary>
    /// Güzergah tablosuna koordinat kolonlarını ekler (PostgreSQL için)
    /// </summary>
    public static async Task ApplyGuzergahKoordinatMigrationPostgresAsync(DbContext context)
    {
        var sql = @"
            DO $$
            BEGIN
                -- BaslangicLatitude kolonu
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Guzergahlar' AND column_name = 'BaslangicLatitude') THEN
                    ALTER TABLE ""Guzergahlar"" ADD COLUMN ""BaslangicLatitude"" DOUBLE PRECISION NULL;
                END IF;
                
                -- BaslangicLongitude kolonu
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Guzergahlar' AND column_name = 'BaslangicLongitude') THEN
                    ALTER TABLE ""Guzergahlar"" ADD COLUMN ""BaslangicLongitude"" DOUBLE PRECISION NULL;
                END IF;
                
                -- BitisLatitude kolonu
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Guzergahlar' AND column_name = 'BitisLatitude') THEN
                    ALTER TABLE ""Guzergahlar"" ADD COLUMN ""BitisLatitude"" DOUBLE PRECISION NULL;
                END IF;
                
                -- BitisLongitude kolonu
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Guzergahlar' AND column_name = 'BitisLongitude') THEN
                    ALTER TABLE ""Guzergahlar"" ADD COLUMN ""BitisLongitude"" DOUBLE PRECISION NULL;
                END IF;
                
                -- RotaRengi kolonu
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Guzergahlar' AND column_name = 'RotaRengi') THEN
                    ALTER TABLE ""Guzergahlar"" ADD COLUMN ""RotaRengi"" TEXT NULL DEFAULT '#3388ff';
                END IF;
            END $$;
        ";

        await context.Database.ExecuteSqlRawAsync(sql);
        Console.WriteLine("PostgreSQL: Guzergahlar tablosuna koordinat kolonları eklendi.");

        // GuzergahSeferleri tablosuna Slot kolonu ekle
        var slotSql = @"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name = 'GuzergahSeferleri' AND column_name = 'Slot') THEN
                    ALTER TABLE ""GuzergahSeferleri"" ADD COLUMN ""Slot"" integer NOT NULL DEFAULT 1;
                END IF;
            END $$;
        ";
        await context.Database.ExecuteSqlRawAsync(slotSql);
        Console.WriteLine("PostgreSQL: GuzergahSeferleri tablosuna Slot kolonu eklendi.");
    }
}



