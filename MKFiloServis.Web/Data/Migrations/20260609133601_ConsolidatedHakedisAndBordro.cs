using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MKFiloServis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidatedHakedisAndBordro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "WhatsAppSablonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "WhatsAppMesajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "WhatsAppKisiler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "WhatsAppGrupUyeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "WhatsAppGruplar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "WhatsAppAyarlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "WebhookLoglar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "WebhookEndpointler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "TekrarlayanOdemeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "TedarikciEvraklari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "TedarikciEvrakDosyalari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "TasimaTedarikciler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "TasimaTedarikciIsler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Subeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "StokKategoriler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "StokKartlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "StokHareketler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "SmsSablonlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "SmsLoglari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "SmsAyarlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ServisTahsilatlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ServisPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ServisParcalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ServisOdemeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ServisKontratlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ServisKayitlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ServisCalismalari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ServisCalismaKiralamalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "SatisPersonelleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "RolYetkileri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Roller",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PuantajKayitlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PuantajJobExecutions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PuantajFinansalKayitlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PuantajExcelImportlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PuantajEslestirmeOnerileri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PuantajDetaylari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PuantajAuditLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PuantajAnomaliler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ProformaFaturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ProformaFaturaKalemler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PlakaDonusumler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PiyasaIlanlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PiyasaArastirmalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PiyasaArastirmaIlanlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelOzlukEvrakVersiyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelOzlukEvraklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelMaaslari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Personeller",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelIzinleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelIzinHaklari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelFinansAyarlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelBorcOdemeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelBorclar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelAvansMahsuplar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelAvanslar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PersonelAracAtamalari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "OzlukEvrakTanimlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Organizasyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "OdemeEslestirmeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MusteriKiralamalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MuhasebeProjeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MuhasebeHesaplari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MuhasebeFisleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MuhasebeFisKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MuhasebeDonemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MuhasebeAyarlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IsverenSGKHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonelGiderHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SGKHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VergiHesabi",
                table: "MuhasebeAyarlari",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Mesajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MasrafKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Lisanslar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "LastikStoklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "LastikSezonAyarlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "LastikDepolar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "LastikDegisimler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Kurumlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "KullaniciTercihleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "KullaniciSonIslemler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Kullanicilar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "KullaniciCariler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "KostMerkezleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "KiralikPlakaTakipler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "KiralikCPlakaTakipler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "KiralamaAraclar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "KdvHesapEslestirmeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Kapasiteler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "IlanPlatformlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "IhaleTeklifVersiyonlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "IhaleTeklifKararLoglari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "IhaleSozlesmeRevizyonlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "IhaleRakipBenchmarklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "IhaleProjeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "IhaleGuzergahKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Hatirlaticilar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Hakedisler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "HakedisDetaylari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "GuzergahSeferleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Guzergahlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "GunlukPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "FirmalarArasiTransferler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Firmalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "FirmaGuzergahEslestirmeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "FirmaAracSoforEslestirmeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "FiloGuzergahEslestirmeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "FiloGunlukPuantajlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "FaturaSablonlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Faturalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "FaturaKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EpostaBildirimLoglari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EmailAyarlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysKayitliAramalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysEvraklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysEvrakKategoriler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysEvrakHareketler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysEvrakDosyaVersiyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysEvrakDosyalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysEvrakAtamalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysBelgeEmbeddingler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EbysAramaGecmisleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekTalepleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekTalebiYanitlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekTalebiIliskileri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekTalebiEkleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekTalebiAktiviteleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekSlaListesi",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekKategorileri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekHazirYanitlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekDepartmanUyeleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekDepartmanlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekBilgiBankasiMakaleleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DestekAyarlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "DashboardWidgetlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ChecklistKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "CariSeferUcretleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Cariler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "CariIletisimNotlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "CariHatirlatmalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BudgetOdemeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BudgetMasrafKalemleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BudgetHedefler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BordroOdemeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Bordrolar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BordroDetaylar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BordroAyarlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Bildirimler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BildirimAyarlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BankaKasaHareketleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BankaHesaplari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BakimPeriyotlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AylikOdemePlanlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AylikOdemeGerceklesenler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AylikChecklistler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracTakipCihazlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracTakipAlarmlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracSatislari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracPlakalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracOperasyonDurumlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracModelleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracMasraflari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracMarkaModeller",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracMarkalari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracMaliyetSnapshotlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Araclar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracKonumlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracIslemler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracIlanYayinlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracIlanlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracIlanIcerikleri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracEvraklari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracEvrakDosyaVersiyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracEvrakDosyalari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracBolgeler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracBolgeAtamalar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracBakimUyarilari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AracAlimSatimlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AktiviteLoglar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HakedisPuantajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    SubeId = table.Column<int>(type: "integer", nullable: true),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    GuzergahId = table.Column<int>(type: "integer", nullable: false),
                    AracId = table.Column<int>(type: "integer", nullable: false),
                    SoforId = table.Column<int>(type: "integer", nullable: false),
                    CariId = table.Column<int>(type: "integer", nullable: false),
                    YonTipi = table.Column<int>(type: "integer", nullable: false),
                    GelirBirimFiyat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    GiderBirimFiyat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    KdvOrani = table.Column<int>(type: "integer", nullable: false),
                    GunlukSeferSayisi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamSefer = table.Column<int>(type: "integer", nullable: false),
                    GelirToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GiderToplam = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KdvTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GelirKdvTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ToplamKesinti = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OdenecekTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TahsilEdilecekTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    FaturaId = table.Column<int>(type: "integer", nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisPuantajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HakedisPuantajlar_Araclar_AracId",
                        column: x => x.AracId,
                        principalTable: "Araclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HakedisPuantajlar_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HakedisPuantajlar_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HakedisPuantajlar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HakedisPuantajlar_Guzergahlar_GuzergahId",
                        column: x => x.GuzergahId,
                        principalTable: "Guzergahlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HakedisPuantajlar_Personeller_SoforId",
                        column: x => x.SoforId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HakedisSeferTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    Kod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VarsayilanSeferSayisi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FiyatCarpani = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MesaiMi = table.Column<bool>(type: "boolean", nullable: false),
                    EkSeferMi = table.Column<bool>(type: "boolean", nullable: false),
                    SistemTanimliMi = table.Column<bool>(type: "boolean", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisSeferTurleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HakedisSeferTurleri_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisKesintiler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HakedisKesintiler_HakedisPuantajlar_HakedisPuantajId",
                        column: x => x.HakedisPuantajId,
                        principalTable: "HakedisPuantajlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HakedisPuantajDetaylar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HakedisPuantajId = table.Column<int>(type: "integer", nullable: false),
                    Gun = table.Column<int>(type: "integer", nullable: false),
                    SeferSayisi = table.Column<int>(type: "integer", nullable: false),
                    SeferTuruId = table.Column<int>(type: "integer", nullable: true),
                    FiyatCarpani = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MesaiMi = table.Column<bool>(type: "boolean", nullable: false),
                    EkSeferMi = table.Column<bool>(type: "boolean", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakedisPuantajDetaylar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HakedisPuantajDetaylar_HakedisPuantajlar_HakedisPuantajId",
                        column: x => x.HakedisPuantajId,
                        principalTable: "HakedisPuantajlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HakedisPuantajDetaylar_HakedisSeferTurleri_SeferTuruId",
                        column: x => x.SeferTuruId,
                        principalTable: "HakedisSeferTurleri",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HakedisKesintiler_HakedisPuantajId",
                table: "HakedisKesintiler",
                column: "HakedisPuantajId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajDetaylar_HakedisPuantajId",
                table: "HakedisPuantajDetaylar",
                column: "HakedisPuantajId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajDetaylar_SeferTuruId",
                table: "HakedisPuantajDetaylar",
                column: "SeferTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_AracId",
                table: "HakedisPuantajlar",
                column: "AracId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_CariId",
                table: "HakedisPuantajlar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_FaturaId",
                table: "HakedisPuantajlar",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_FirmaId",
                table: "HakedisPuantajlar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_GuzergahId",
                table: "HakedisPuantajlar",
                column: "GuzergahId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisPuantajlar_SoforId",
                table: "HakedisPuantajlar",
                column: "SoforId");

            migrationBuilder.CreateIndex(
                name: "IX_HakedisSeferTurleri_FirmaId",
                table: "HakedisSeferTurleri",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HakedisKesintiler");

            migrationBuilder.DropTable(
                name: "HakedisPuantajDetaylar");

            migrationBuilder.DropTable(
                name: "HakedisPuantajlar");

            migrationBuilder.DropTable(
                name: "HakedisSeferTurleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WhatsAppSablonlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WhatsAppMesajlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WhatsAppKisiler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WhatsAppGrupUyeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WhatsAppGruplar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WhatsAppAyarlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WebhookLoglar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WebhookEndpointler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TekrarlayanOdemeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TedarikciEvraklari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TedarikciEvrakDosyalari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TasimaTedarikciler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TasimaTedarikciIsler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Subeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "StokKategoriler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "StokKartlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "StokHareketler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SmsSablonlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SmsLoglari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SmsAyarlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServisTahsilatlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServisPuantajlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServisParcalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServisOdemeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServisKontratlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServisKayitlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServisCalismalari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServisCalismaKiralamalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SatisPersonelleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "RolYetkileri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Roller");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PuantajKayitlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PuantajJobExecutions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PuantajFinansalKayitlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PuantajExcelImportlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PuantajEslestirmeOnerileri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PuantajDetaylari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PuantajAuditLogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PuantajAnomaliler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProformaFaturalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProformaFaturaKalemler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PlakaDonusumler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PiyasaIlanlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PiyasaArastirmalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PiyasaArastirmaIlanlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelPuantajlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelOzlukEvrakVersiyonlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelOzlukEvraklar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelMaaslari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Personeller");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelIzinleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelIzinHaklari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelFinansAyarlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelBorcOdemeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelBorclar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelAvansMahsuplar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelAvanslar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PersonelAracAtamalari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "OzlukEvrakTanimlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Organizasyonlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "OdemeEslestirmeleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MusteriKiralamalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MuhasebeProjeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MuhasebeHesaplari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MuhasebeFisleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MuhasebeFisKalemleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MuhasebeDonemleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "IsverenSGKHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "PersonelGiderHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "SGKHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "VergiHesabi",
                table: "MuhasebeAyarlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Mesajlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MasrafKalemleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Lisanslar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "LastikStoklar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "LastikSezonAyarlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "LastikDepolar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "LastikDegisimler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Kurumlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "KullaniciTercihleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "KullaniciSonIslemler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "KullaniciCariler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "KostMerkezleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "KiralikPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "KiralikCPlakaTakipler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "KiralamaAraclar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "KdvHesapEslestirmeleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Kapasiteler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "IlanPlatformlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "IhaleTeklifVersiyonlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "IhaleTeklifKararLoglari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "IhaleSozlesmeRevizyonlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "IhaleRakipBenchmarklar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "IhaleProjeleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "IhaleGuzergahKalemleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Hatirlaticilar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Hakedisler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "HakedisDetaylari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "GuzergahSeferleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Guzergahlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "GunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FirmalarArasiTransferler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Firmalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FirmaGuzergahEslestirmeleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FirmaAracSoforEslestirmeleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FiloGuzergahEslestirmeleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FiloGunlukPuantajlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FaturaSablonlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FaturaKalemleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EpostaBildirimLoglari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EmailAyarlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysKayitliAramalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysEvraklar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysEvrakKategoriler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysEvrakHareketler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysEvrakDosyaVersiyonlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysEvrakDosyalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysEvrakAtamalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysBelgeEmbeddingler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EbysAramaGecmisleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekTalepleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekTalebiYanitlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekTalebiIliskileri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekTalebiEkleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekTalebiAktiviteleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekSlaListesi");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekKategorileri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekHazirYanitlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekDepartmanUyeleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekDepartmanlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekBilgiBankasiMakaleleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DestekAyarlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DashboardWidgetlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ChecklistKalemleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CariSeferUcretleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Cariler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CariIletisimNotlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CariHatirlatmalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BudgetOdemeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BudgetMasrafKalemleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BudgetHedefler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BordroOdemeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Bordrolar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BordroDetaylar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BordroAyarlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Bildirimler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BildirimAyarlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BankaKasaHareketleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BankaHesaplari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BakimPeriyotlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AylikOdemePlanlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AylikOdemeGerceklesenler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AylikChecklistler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracTakipCihazlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracTakipAlarmlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracSatislari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracPlakalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracOperasyonDurumlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracModelleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracMasraflari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracMarkaModeller");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracMarkalari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracMaliyetSnapshotlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Araclar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracKonumlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracIslemler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracIlanYayinlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracIlanlari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracIlanIcerikleri");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracEvraklari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracEvrakDosyaVersiyonlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracEvrakDosyalari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracBolgeler");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracBolgeAtamalar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracBakimUyarilari");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AracAlimSatimlar");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AktiviteLoglar");
        }
    }
}


