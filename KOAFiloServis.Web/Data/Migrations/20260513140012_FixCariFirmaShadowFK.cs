using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCariFirmaShadowFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOT: Bu migration, EF model snapshot'ında oluşmuş olan ancak veritabanında
            // hiç var olmamış olabilen "FirmaId1" shadow FK sütununu temizler.
            // DB'ye göre durum farklı olabileceği için tüm DROP işlemleri IF EXISTS ile yapılır.
            migrationBuilder.Sql("ALTER TABLE \"Cariler\" DROP CONSTRAINT IF EXISTS \"FK_Cariler_Firmalar_FirmaId1\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Cariler_FirmaId1\";");
            migrationBuilder.Sql("ALTER TABLE \"Cariler\" DROP COLUMN IF EXISTS \"FirmaId1\";");

            // FirmaId üzerindeki index/FK zaten önceki migration'larda kurulmuş olabilir;
            // duplicate'leri engellemek için yine güvenli yolla ekliyoruz.
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Cariler_FirmaId\" ON \"Cariler\" (\"FirmaId\");");
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Cariler_Firmalar_FirmaId'
                    ) THEN
                        ALTER TABLE ""Cariler""
                        ADD CONSTRAINT ""FK_Cariler_Firmalar_FirmaId""
                        FOREIGN KEY (""FirmaId"") REFERENCES ""Firmalar""(""Id"") ON DELETE SET NULL;
                    END IF;
                END$$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma: shadow FK'yi yeniden oluşturmuyoruz; sadece eklediğimiz FK'yi düşürüyoruz.
            migrationBuilder.Sql("ALTER TABLE \"Cariler\" DROP CONSTRAINT IF EXISTS \"FK_Cariler_Firmalar_FirmaId\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Cariler_FirmaId\";");
        }
    }
}
