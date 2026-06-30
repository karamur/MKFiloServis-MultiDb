using System.Data;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

public static class OzlukEvrakMigrationHelper
{
    public static async Task ApplyOzlukEvrakMigrationAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                var sql = @"
CREATE TABLE IF NOT EXISTS ""OzlukEvrakTanimlari"" (
    ""Id"" SERIAL PRIMARY KEY,
    ""EvrakAdi"" VARCHAR(200) NOT NULL,
    ""Aciklama"" VARCHAR(500),
    ""Kategori"" INTEGER NOT NULL DEFAULT 1,
    ""Zorunlu"" BOOLEAN NOT NULL DEFAULT TRUE,
    ""SiraNo"" INTEGER NOT NULL DEFAULT 1,
    ""Aktif"" BOOLEAN NOT NULL DEFAULT TRUE,
    ""GecerliGorevler"" VARCHAR(50),
    ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE,
    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS ""PersonelOzlukEvraklar"" (
    ""Id"" SERIAL PRIMARY KEY,
    ""SoforId"" INTEGER NOT NULL,
    ""EvrakTanimId"" INTEGER NOT NULL,
    ""Tamamlandi"" BOOLEAN NOT NULL DEFAULT FALSE,
    ""TamamlanmaTarihi"" TIMESTAMP WITH TIME ZONE,
    ""DosyaYolu"" VARCHAR(500),
    ""Aciklama"" VARCHAR(500),
    ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE,
    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT ""FK_PersonelOzlukEvraklar_Personeller"" FOREIGN KEY (""SoforId"") REFERENCES ""Personeller"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_PersonelOzlukEvraklar_OzlukEvrakTanimlari"" FOREIGN KEY (""EvrakTanimId"") REFERENCES ""OzlukEvrakTanimlari"" (""Id"") ON DELETE CASCADE
);";

                await context.Database.ExecuteSqlRawAsync(sql);
            }
            else if (context.Database.IsSqlite())
            {
                await EnsureSqliteSchemaAsync(context);
            }

            // Index'leri ayrı oluştur (hata vermemesi için try-catch)
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_PersonelOzlukEvraklar_SoforId"" ON ""PersonelOzlukEvraklar"" (""SoforId"")");
                await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_PersonelOzlukEvraklar_EvrakTanimId"" ON ""PersonelOzlukEvraklar"" (""EvrakTanimId"")");
                await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_OzlukEvrakTanimlari_Kategori"" ON ""OzlukEvrakTanimlari"" (""Kategori"")");
            }
            catch { /* Index zaten varsa hata vermesini engelle */ }

            // "Nüfus Cüzdanı Fotokopisi" -> "Kimlik Fotokopisi" tekilleştirmesi
            try
            {
                await ConsolidateKimlikFotokopisiAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kimlik fotokopisi tekilleştirme hatası: {ex.Message}");
            }

            // GecerliGorevler kısıtını kaldır: Tüm personeller için aynı belgeler.
            try
            {
                if (context.Database.IsNpgsql() || context.Database.IsSqlite())
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"UPDATE ""OzlukEvrakTanimlari"" SET ""GecerliGorevler"" = NULL WHERE ""GecerliGorevler"" IS NOT NULL AND ""GecerliGorevler"" <> ''");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GecerliGorevler temizleme hatası: {ex.Message}");
            }

            // "Yaygın Eğitim Sertifikası" tanımını ekle (mevcut değilse)
            try
            {
                await EnsureYayginEgitimTanimAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Yaygın Eğitim Sertifikası tanım ekleme hatası: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Özlük evrak migration hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// "Yaygın Eğitim Sertifikası" özlük evrak tanımını, yoksa ekler (idempotent).
    /// SRC Belgesi'nin hemen ardına eklenir.
    /// </summary>
    private static async Task EnsureYayginEgitimTanimAsync(ApplicationDbContext context)
    {
        var exists = await context.OzlukEvrakTanimlari
            .AnyAsync(t => t.EvrakAdi.Contains("Yaygın Eğitim") && !t.IsDeleted);
        if (exists) return;

        // SRC Belgesi'nin SiraNo'sunu bul; yoksa 2 varsay
        var srcSiraNo = await context.OzlukEvrakTanimlari
            .Where(t => t.EvrakAdi == "SRC Belgesi" && !t.IsDeleted)
            .Select(t => (int?)t.SiraNo)
            .FirstOrDefaultAsync() ?? 2;

        // SRC'den sonra gelenlerin SiraNo'larını 1 arttır
        var sonrakiler = await context.OzlukEvrakTanimlari
            .Where(t => t.Kategori == OzlukEvrakKategori.SoforBelgeleri && t.SiraNo > srcSiraNo && !t.IsDeleted)
            .ToListAsync();
        foreach (var t in sonrakiler)
            t.SiraNo++;

        context.OzlukEvrakTanimlari.Add(new OzlukEvrakTanim
        {
            EvrakAdi = "Yaygın Eğitim Sertifikası",
            Aciklama = "Yaygın eğitim katılım sertifikası",
            Kategori = OzlukEvrakKategori.SoforBelgeleri,
            Zorunlu = false,
            SiraNo = srcSiraNo + 1,
            Aktif = true,
            GecerliGorevler = null,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// "Nüfus Cüzdanı Fotokopisi" ile "Kimlik Fotokopisi" aynı belgedir.
    /// Kimlik Fotokopisi varsa, eski "Nüfus Cüzdanı Fotokopisi" kayıtlarını ona taşır ve siler.
    /// Yoksa "Nüfus Cüzdanı Fotokopisi" tanımını "Kimlik Fotokopisi" olarak günceller.
    /// </summary>
    private static async Task ConsolidateKimlikFotokopisiAsync(ApplicationDbContext context)
    {
        var nufusTanim = await context.OzlukEvrakTanimlari
            .FirstOrDefaultAsync(t => t.EvrakAdi == "Nüfus Cüzdanı Fotokopisi");
        if (nufusTanim == null) return;

        var kimlikTanim = await context.OzlukEvrakTanimlari
            .FirstOrDefaultAsync(t => t.EvrakAdi == "Kimlik Fotokopisi" && t.Id != nufusTanim.Id);

        if (kimlikTanim == null)
        {
            // Sadece adı değiştir.
            nufusTanim.EvrakAdi = "Kimlik Fotokopisi";
            nufusTanim.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return;
        }

        // İki tanım da varsa: nüfus altındaki personel evraklarını kimlik tanımına taşı.
        var nufusEvraklar = await context.PersonelOzlukEvraklar
            .Where(e => e.EvrakTanimId == nufusTanim.Id)
            .ToListAsync();

        foreach (var ev in nufusEvraklar)
        {
            var mevcutKimlik = await context.PersonelOzlukEvraklar
                .FirstOrDefaultAsync(x => x.SoforId == ev.SoforId && x.EvrakTanimId == kimlikTanim.Id);

            if (mevcutKimlik == null)
            {
                ev.EvrakTanimId = kimlikTanim.Id;
                ev.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Kimlik tanımına ait kayıt boşsa, nüfus kaydındaki bilgileri taşı.
                if (string.IsNullOrEmpty(mevcutKimlik.DosyaYolu) && !string.IsNullOrEmpty(ev.DosyaYolu))
                    mevcutKimlik.DosyaYolu = ev.DosyaYolu;
                if (!mevcutKimlik.Tamamlandi && ev.Tamamlandi)
                {
                    mevcutKimlik.Tamamlandi = true;
                    mevcutKimlik.TamamlanmaTarihi = ev.TamamlanmaTarihi ?? mevcutKimlik.TamamlanmaTarihi;
                }
                if (!mevcutKimlik.GecerlilikBitisTarihi.HasValue && ev.GecerlilikBitisTarihi.HasValue)
                    mevcutKimlik.GecerlilikBitisTarihi = ev.GecerlilikBitisTarihi;

                mevcutKimlik.UpdatedAt = DateTime.UtcNow;
                context.PersonelOzlukEvraklar.Remove(ev);
            }
        }

        // Eski tanımı sil
        context.OzlukEvrakTanimlari.Remove(nufusTanim);
        await context.SaveChangesAsync();
    }

    private static async Task EnsureSqliteSchemaAsync(ApplicationDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            if (await RequiresSqliteTableRebuildAsync(connection, "OzlukEvrakTanimlari"))
            {
                await ExecuteSqliteNonQueryAsync(connection, "DROP TABLE IF EXISTS \"PersonelOzlukEvraklar\"");
                await ExecuteSqliteNonQueryAsync(connection, "DROP TABLE IF EXISTS \"OzlukEvrakTanimlari\"");
            }

            await ExecuteSqliteNonQueryAsync(connection, @"
CREATE TABLE IF NOT EXISTS ""OzlukEvrakTanimlari"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_OzlukEvrakTanimlari"" PRIMARY KEY AUTOINCREMENT,
    ""EvrakAdi"" TEXT NOT NULL,
    ""Aciklama"" TEXT NULL,
    ""Kategori"" INTEGER NOT NULL DEFAULT 1,
    ""Zorunlu"" INTEGER NOT NULL DEFAULT 1,
    ""SiraNo"" INTEGER NOT NULL DEFAULT 1,
    ""Aktif"" INTEGER NOT NULL DEFAULT 1,
    ""GecerliGorevler"" TEXT NULL,
    ""CreatedAt"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedAt"" TEXT NULL,
    ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
)");

            await ExecuteSqliteNonQueryAsync(connection, @"
CREATE TABLE IF NOT EXISTS ""PersonelOzlukEvraklar"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PersonelOzlukEvraklar"" PRIMARY KEY AUTOINCREMENT,
    ""SoforId"" INTEGER NOT NULL,
    ""EvrakTanimId"" INTEGER NOT NULL,
    ""Tamamlandi"" INTEGER NOT NULL DEFAULT 0,
    ""TamamlanmaTarihi"" TEXT NULL,
    ""DosyaYolu"" TEXT NULL,
    ""Aciklama"" TEXT NULL,
    ""CreatedAt"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedAt"" TEXT NULL,
    ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT ""FK_PersonelOzlukEvraklar_Personeller"" FOREIGN KEY (""SoforId"") REFERENCES ""Personeller"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_PersonelOzlukEvraklar_OzlukEvrakTanimlari"" FOREIGN KEY (""EvrakTanimId"") REFERENCES ""OzlukEvrakTanimlari"" (""Id"") ON DELETE CASCADE
)");
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> RequiresSqliteTableRebuildAsync(System.Data.Common.DbConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT type, pk FROM pragma_table_info('{tableName}') WHERE name = 'Id' LIMIT 1";

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return false;
        }

        var type = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
        var pk = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
        return pk != 1 || !string.Equals(type, "INTEGER", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ExecuteSqliteNonQueryAsync(System.Data.Common.DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}



