using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// BudgetOdemeler tablosuna kısmi ödeme alanlarını ekler
/// </summary>
public static class BudgetOdemeKalanMigrationHelper
{
    public static async Task EnsureBudgetOdemeKalanColumnAsync(ApplicationDbContext context)
    {
        if (context.Database.IsNpgsql())
        {
            var sql = @"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'BudgetOdemeler' 
                                   AND column_name = 'KismiOdemeMi') THEN
                        ALTER TABLE ""BudgetOdemeler"" ADD COLUMN ""KismiOdemeMi"" BOOLEAN NOT NULL DEFAULT FALSE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'BudgetOdemeler' 
                                   AND column_name = 'ToplamKismiOdenen') THEN
                        ALTER TABLE ""BudgetOdemeler"" ADD COLUMN ""ToplamKismiOdenen"" numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'BudgetOdemeler' 
                                   AND column_name = 'KalanSonrakiDonemeAktarilsin') THEN
                        ALTER TABLE ""BudgetOdemeler"" ADD COLUMN ""KalanSonrakiDonemeAktarilsin"" BOOLEAN NOT NULL DEFAULT FALSE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'BudgetOdemeler' 
                                   AND column_name = 'SonrakiDonemOdemeId') THEN
                        ALTER TABLE ""BudgetOdemeler"" ADD COLUMN ""SonrakiDonemOdemeId"" INTEGER NULL;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'BudgetOdemeler' 
                                   AND column_name = 'OncekiDonemOdemeId') THEN
                        ALTER TABLE ""BudgetOdemeler"" ADD COLUMN ""OncekiDonemOdemeId"" INTEGER NULL;
                    END IF;
                END $$;
            ";
            await context.Database.ExecuteSqlRawAsync(sql);
            return;
        }

        if (context.Database.IsSqlite())
        {
            // SQLite için kolon kontrolü
            var conn = context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info('BudgetOdemeler')";
            var columns = new List<string>();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(1));
                }
            }

            if (!columns.Contains("KismiOdemeMi"))
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE BudgetOdemeler ADD COLUMN KismiOdemeMi INTEGER NOT NULL DEFAULT 0";
                await alterCmd.ExecuteNonQueryAsync();
            }

            if (!columns.Contains("ToplamKismiOdenen"))
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE BudgetOdemeler ADD COLUMN ToplamKismiOdenen TEXT NOT NULL DEFAULT 0";
                await alterCmd.ExecuteNonQueryAsync();
            }

            if (!columns.Contains("KalanSonrakiDonemeAktarilsin"))
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE BudgetOdemeler ADD COLUMN KalanSonrakiDonemeAktarilsin INTEGER NOT NULL DEFAULT 0";
                await alterCmd.ExecuteNonQueryAsync();
            }

            if (!columns.Contains("SonrakiDonemOdemeId"))
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE BudgetOdemeler ADD COLUMN SonrakiDonemOdemeId INTEGER NULL";
                await alterCmd.ExecuteNonQueryAsync();
            }

            if (!columns.Contains("OncekiDonemOdemeId"))
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE BudgetOdemeler ADD COLUMN OncekiDonemOdemeId INTEGER NULL";
                await alterCmd.ExecuteNonQueryAsync();
            }
        }
    }
}



