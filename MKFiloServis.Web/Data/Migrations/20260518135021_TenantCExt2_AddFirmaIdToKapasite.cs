using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <summary>
    /// HOTFİX: Kapasiteler tablosuna FirmaId kolonu eksikti.
    /// Faz C-extend sırasında snapshot zaten Kapasite.FirmaId içerdiği için (entity önceki
    /// oturumda hazırlanmış, ama kolon migration'ı üretilmemiş) EF "fark yok" diyerek
    /// TenantCExt migration'ına Kapasiteler'i dahil etmedi.
    /// Sonuç: runtime'da "column k.FirmaId does not exist" (Npgsql 42703).
    /// Bu migration eksikliği kapatır. PL/pgSQL idempotent.
    /// </summary>
    public partial class TenantCExt2_AddFirmaIdToKapasite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // The ordered history reaches this one-time hotfix with the missing
                // Kapasiteler.FirmaId column. Use EF operations so SQLite rebuilds the
                // table for the FK while preserving existing capacity rows.
                migrationBuilder.AddColumn<int>(
                    name: "FirmaId",
                    table: "Kapasiteler",
                    type: "INTEGER",
                    nullable: true);

                migrationBuilder.CreateIndex(
                    name: "IX_Kapasiteler_FirmaId_KapasiteAdi",
                    table: "Kapasiteler",
                    columns: new[] { "FirmaId", "KapasiteAdi" });

                migrationBuilder.AddForeignKey(
                    name: "FK_Kapasiteler_Firmalar_FirmaId",
                    table: "Kapasiteler",
                    column: "FirmaId",
                    principalTable: "Firmalar",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.Sql(@"
                    UPDATE ""Kapasiteler""
                    SET ""FirmaId"" = COALESCE(
                        (SELECT ""Id"" FROM ""Firmalar""
                         WHERE COALESCE(""VarsayilanFirma"", 0) = 1
                           AND COALESCE(""Aktif"", 1) = 1
                         ORDER BY ""Id"" LIMIT 1),
                        (SELECT ""Id"" FROM ""Firmalar""
                         WHERE COALESCE(""Aktif"", 1) = 1
                         ORDER BY ""Id"" LIMIT 1))
                    WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                ");
                return;
            }

            // 1) FirmaId kolonu (nullable, K9)
            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Kapasiteler' AND column_name = 'FirmaId'
                    ) THEN
                        ALTER TABLE ""Kapasiteler"" ADD COLUMN ""FirmaId"" integer NULL;
                    END IF;
                END $$;
            ");

            // 2) Index (FirmaId, KapasiteAdi) — snapshot ile uyumlu
            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = current_schema()
                          AND tablename = 'Kapasiteler'
                          AND indexname = 'IX_Kapasiteler_FirmaId_KapasiteAdi'
                    ) THEN
                        CREATE INDEX ""IX_Kapasiteler_FirmaId_KapasiteAdi""
                            ON ""Kapasiteler"" (""FirmaId"", ""KapasiteAdi"");
                    END IF;
                END $$;
            ");

            // 3) FK Kapasiteler -> Firmalar (Restrict)
            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE table_name = 'Kapasiteler'
                          AND constraint_name = 'FK_Kapasiteler_Firmalar_FirmaId'
                          AND constraint_type = 'FOREIGN KEY'
                    ) THEN
                        ALTER TABLE ""Kapasiteler""
                            ADD CONSTRAINT ""FK_Kapasiteler_Firmalar_FirmaId""
                            FOREIGN KEY (""FirmaId"") REFERENCES ""Firmalar"" (""Id"") ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");

            // 4) Backfill: NULL/0 satırları varsayılan firma ile doldur
            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"
                DO $$
                DECLARE
                    v_firma_id integer;
                BEGIN
                    SELECT ""Id"" INTO v_firma_id
                    FROM ""Firmalar""
                    WHERE COALESCE(""VarsayilanFirma"", false) = true AND COALESCE(""Aktif"", true) = true
                    ORDER BY ""Id"" LIMIT 1;

                    IF v_firma_id IS NULL THEN
                        SELECT ""Id"" INTO v_firma_id
                        FROM ""Firmalar""
                        WHERE COALESCE(""Aktif"", true) = true
                        ORDER BY ""Id"" LIMIT 1;
                    END IF;

                    IF v_firma_id IS NOT NULL THEN
                        UPDATE ""Kapasiteler""
                        SET ""FirmaId"" = v_firma_id
                        WHERE ""FirmaId"" IS NULL OR ""FirmaId"" = 0;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_Kapasiteler_Firmalar_FirmaId",
                    table: "Kapasiteler");
                migrationBuilder.DropIndex(
                    name: "IX_Kapasiteler_FirmaId_KapasiteAdi",
                    table: "Kapasiteler");
                migrationBuilder.DropColumn(name: "FirmaId", table: "Kapasiteler");
                return;
            }

            migrationBuilder.Sql((migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL" ? MKFiloServis.Shared.Auditing.DatabaseWriteAudit.PostgreSqlInstallSql : "") + "\n" + @"
                ALTER TABLE ""Kapasiteler"" DROP CONSTRAINT IF EXISTS ""FK_Kapasiteler_Firmalar_FirmaId"";
                DROP INDEX IF EXISTS ""IX_Kapasiteler_FirmaId_KapasiteAdi"";
                ALTER TABLE ""Kapasiteler"" DROP COLUMN IF EXISTS ""FirmaId"";
            ");
        }
    }
}


