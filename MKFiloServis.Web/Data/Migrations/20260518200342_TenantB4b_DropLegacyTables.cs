using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantB4b_DropLegacyTables : Migration
    {
        // Faz 5.3-B4b: Faz 5.3-B3-i'de RENAME ile geçici saklanan legacy tabloları DROP eder.
        // - _LEGACY_Sirketler (eski "Sirketler")
        // - _LEGACY_SirketTransferLoglari (eski "SirketTransferLoglari")
        // VERİ KAYBI: Bu iki tablonun tüm içeriği kalıcı olarak silinir.
        // Geri dönüş: Down() boş bırakıldı çünkü orijinal şema CompositeMigrationsHistory'den
        // yeniden inşa edilemez (FK ve seed kayıtları kaybolur). Sadece backup'tan restore yolu var.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
               WHERE table_name = '_LEGACY_SirketTransferLoglari') THEN
        DROP TABLE ""_LEGACY_SirketTransferLoglari"";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables
               WHERE table_name = '_LEGACY_Sirketler') THEN
        DROP TABLE ""_LEGACY_Sirketler"";
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri dönüş yok: drop edilen tablolar yalnızca DB backup'tan restore edilebilir.
            // Bu migration'ı geri almak için pg_dump dosyasından selective restore kullanın.
        }
    }
}



