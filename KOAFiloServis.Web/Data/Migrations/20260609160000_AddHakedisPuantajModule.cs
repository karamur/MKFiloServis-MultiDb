using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KOAFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHakedisPuantajModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HakedisPuantajlar
            migrationBuilder.CreateTable(
                name: "HakedisPuantajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: true),
                    SubeId = table.Column<int>(type: "integer", nullable: true),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: false),
                    CariId = table.Column<int>(type: "integer", nullable: false),
                    YonTipi = table.Column<int>(type: "integer", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    KdvOrani = table.Column<int>(type: "integer", nullable: false),
                    GunlukSeferSayisi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamSefer = table.Column<int>(type: "integer", nullable: false),
                    ToplamEkSefer = table.Column<int>(type: "integer", nullable: false),
                    HakedisTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamKesinti = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OdenecekTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisPuantajlar", x => x.Id);
                    table.ForeignKey(name: "FK_HakedisPuantajlar_Araclar_AracId", column: x => x.AracId, principalTable: "Araclar", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_HakedisPuantajlar_Cariler_CariId", column: x => x.CariId, principalTable: "Cariler", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_HakedisPuantajlar_Firmalar_FirmaId", column: x => x.FirmaId, principalTable: "Firmalar", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_HakedisPuantajlar_Faturalar_FaturaId", column: x => x.FaturaId, principalTable: "Faturalar", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_HakedisPuantajlar_Guzergahlar_GuzergahId", column: x => x.GuzergahId, principalTable: "Guzergahlar", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_HakedisPuantajlar_Personeller_SoforId", column: x => x.SoforId, principalTable: "Personeller", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_HakedisPuantajlar_AracId", table: "HakedisPuantajlar", column: "AracId");
            migrationBuilder.CreateIndex(name: "IX_HakedisPuantajlar_CariId", table: "HakedisPuantajlar", column: "CariId");
            migrationBuilder.CreateIndex(name: "IX_HakedisPuantajlar_FirmaId", table: "HakedisPuantajlar", column: "FirmaId");
            migrationBuilder.CreateIndex(name: "IX_HakedisPuantajlar_GuzergahId", table: "HakedisPuantajlar", column: "GuzergahId");
            migrationBuilder.CreateIndex(name: "IX_HakedisPuantajlar_SoforId", table: "HakedisPuantajlar", column: "SoforId");

            // HakedisPuantajDetaylar
            migrationBuilder.CreateTable(
                name: "HakedisPuantajDetaylar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HakedisPuantajId = table.Column<int>(type: "integer", nullable: false),
                    Gun = table.Column<int>(type: "integer", nullable: false),
                    SeferSayisi = table.Column<int>(type: "integer", nullable: false),
                    EkSeferMi = table.Column<bool>(type: "boolean", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisPuantajDetaylar", x => x.Id);
                    table.ForeignKey(name: "FK_HakedisPuantajDetaylar_HakedisPuantajlar_HakedisPuantajId",
                        column: x => x.HakedisPuantajId, principalTable: "HakedisPuantajlar", principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_HakedisPuantajDetaylar_HakedisPuantajId", table: "HakedisPuantajDetaylar", column: "HakedisPuantajId");

            // HakedisKesintiler
            migrationBuilder.CreateTable(
                name: "HakedisKesintiler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HakedisPuantajId = table.Column<int>(type: "integer", nullable: false),
                    KesintiAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisKesintiler", x => x.Id);
                    table.ForeignKey(name: "FK_HakedisKesintiler_HakedisPuantajlar_HakedisPuantajId",
                        column: x => x.HakedisPuantajId, principalTable: "HakedisPuantajlar", principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_HakedisKesintiler_HakedisPuantajId", table: "HakedisKesintiler", column: "HakedisPuantajId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HakedisKesintiler");
            migrationBuilder.DropTable(name: "HakedisPuantajDetaylar");
            migrationBuilder.DropTable(name: "HakedisPuantajlar");
        }
    }
}
