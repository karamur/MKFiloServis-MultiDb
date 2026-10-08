using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MKFiloServis.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261007100000_AddFilePathLookupIndexes")]
public sealed class AddFilePathLookupIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex("IX_AracEvrakDosyalari_DosyaYolu", "AracEvrakDosyalari", "DosyaYolu");
        migrationBuilder.CreateIndex("IX_AracEvrakDosyaVersiyonlar_DosyaYolu", "AracEvrakDosyaVersiyonlar", "DosyaYolu");
        migrationBuilder.CreateIndex("IX_DestekTalebiEkleri_DosyaYolu", "DestekTalebiEkleri", "DosyaYolu");
        migrationBuilder.CreateIndex("IX_EbysEvrakDosyalar_DosyaYolu", "EbysEvrakDosyalar", "DosyaYolu");
        migrationBuilder.CreateIndex("IX_EbysEvrakDosyaVersiyonlar_DosyaYolu", "EbysEvrakDosyaVersiyonlar", "DosyaYolu");
        migrationBuilder.CreateIndex("IX_EvrakDosyalari_DosyaYolu", "EvrakDosyalari", "DosyaYolu");
        migrationBuilder.CreateIndex("IX_Faturalar_PdfDosyaYolu", "Faturalar", "PdfDosyaYolu");
        migrationBuilder.CreateIndex("IX_Faturalar_XmlDosyaYolu", "Faturalar", "XmlDosyaYolu");
        migrationBuilder.CreateIndex("IX_PersonelOzlukEvraklar_DosyaYolu", "PersonelOzlukEvraklar", "DosyaYolu");
        migrationBuilder.CreateIndex("IX_PersonelOzlukEvrakVersiyonlar_DosyaYolu", "PersonelOzlukEvrakVersiyonlar", "DosyaYolu");
        migrationBuilder.CreateIndex("IX_ProformaFaturalar_PdfDosyaYolu", "ProformaFaturalar", "PdfDosyaYolu");
        migrationBuilder.CreateIndex("IX_TedarikciEvrakDosyalari_DosyaYolu", "TedarikciEvrakDosyalari", "DosyaYolu");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_AracEvrakDosyalari_DosyaYolu", "AracEvrakDosyalari");
        migrationBuilder.DropIndex("IX_AracEvrakDosyaVersiyonlar_DosyaYolu", "AracEvrakDosyaVersiyonlar");
        migrationBuilder.DropIndex("IX_DestekTalebiEkleri_DosyaYolu", "DestekTalebiEkleri");
        migrationBuilder.DropIndex("IX_EbysEvrakDosyalar_DosyaYolu", "EbysEvrakDosyalar");
        migrationBuilder.DropIndex("IX_EbysEvrakDosyaVersiyonlar_DosyaYolu", "EbysEvrakDosyaVersiyonlar");
        migrationBuilder.DropIndex("IX_EvrakDosyalari_DosyaYolu", "EvrakDosyalari");
        migrationBuilder.DropIndex("IX_Faturalar_PdfDosyaYolu", "Faturalar");
        migrationBuilder.DropIndex("IX_Faturalar_XmlDosyaYolu", "Faturalar");
        migrationBuilder.DropIndex("IX_PersonelOzlukEvraklar_DosyaYolu", "PersonelOzlukEvraklar");
        migrationBuilder.DropIndex("IX_PersonelOzlukEvrakVersiyonlar_DosyaYolu", "PersonelOzlukEvrakVersiyonlar");
        migrationBuilder.DropIndex("IX_ProformaFaturalar_PdfDosyaYolu", "ProformaFaturalar");
        migrationBuilder.DropIndex("IX_TedarikciEvrakDosyalari_DosyaYolu", "TedarikciEvrakDosyalari");
    }
}
