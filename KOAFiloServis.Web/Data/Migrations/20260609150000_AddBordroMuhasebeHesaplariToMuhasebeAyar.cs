using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBordroMuhasebeHesaplariToMuhasebeAyar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonelGiderHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "770.01");

            migrationBuilder.AddColumn<string>(
                name: "VergiHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "360.01");

            migrationBuilder.AddColumn<string>(
                name: "SGKHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "361.01");

            migrationBuilder.AddColumn<string>(
                name: "IsverenSGKHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "368.01");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsverenSGKHesabi", table: "MuhasebeAyarlari");
            migrationBuilder.DropColumn(name: "SGKHesabi", table: "MuhasebeAyarlari");
            migrationBuilder.DropColumn(name: "VergiHesabi", table: "MuhasebeAyarlari");
            migrationBuilder.DropColumn(name: "PersonelGiderHesabi", table: "MuhasebeAyarlari");
        }
    }
}
