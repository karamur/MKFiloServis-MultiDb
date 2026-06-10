using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KOAFiloServis.Web.Data;

public class ApplicationDbContext : DbContext
{
    private IServiceProvider? _serviceProvider;
    private IAktifFirmaProvider? _aktifFirmaProvider;

    /// <summary>
    /// AsyncLocal IServiceProvider — Blazor circuit scope'unda SetServiceProvider
    /// çağrılmasa bile IAktifFirmaProvider'a erişim sağlar.
    /// PooledDbContextFactory ile oluşturulan context'ler için fallback.
    /// </summary>
    internal static readonly System.Threading.AsyncLocal<IServiceProvider?> AmbientServiceProvider = new();

    /// <summary>
    /// Aktif firma (yeni tenant kavramı). Global query filter ve SaveChanges'te kullanılır.
    /// 0 dönerse "filter pasif" anlamına gelir (henüz firma seçilmemiş / SuperAdmin / TumFirmalar).
    /// </summary>
    private int? FirmaTenantId => ResolveAktifFirmaProvider()?.AktifFirmaId;

    /// <summary>
    /// True ise IFirmaTenant entity'leri üzerindeki firma filter'ı devre dışı bırakılır
    /// (SuperAdmin / TumFirmalar modu).
    /// Provider yoksa veya AktifFirmaId gecerli degilse filter AKTIF kalir — veri sizintisi onlenir (R9 fix).
    /// </summary>
    private bool FirmaTenantDisabled
    {
        get
        {
            var p = ResolveAktifFirmaProvider();
            if (p == null) return false;
            if (p.TumFirmalar) return true;
            if (p.AktifFirmaId is null or 0) return false;
            return false;
        }
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Scoped IServiceProvider'ı ayarlar. IAktifFirmaProvider sorgu zamanında lazy olarak çözümlenir.
    /// </summary>
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _aktifFirmaProvider = null;
        // AsyncLocal fallback: factory context'leri için ambient provider
        AmbientServiceProvider.Value = serviceProvider;
    }

    private IAktifFirmaProvider? ResolveAktifFirmaProvider()
    {
        // 1) SetServiceProvider ile ayarlanmış provider
        if (_aktifFirmaProvider == null && _serviceProvider != null)
        {
            try
            {
                _aktifFirmaProvider = _serviceProvider.GetService<IAktifFirmaProvider>();
            }
            catch (ObjectDisposedException)
            {
                _serviceProvider = null;
            }
        }
        // 2) AsyncLocal fallback — pooled factory context'leri için
        if (_aktifFirmaProvider == null)
        {
            var ambientSp = AmbientServiceProvider.Value;
            if (ambientSp != null && ambientSp != _serviceProvider)
            {
                try
                {
                    _aktifFirmaProvider = ambientSp.GetService<IAktifFirmaProvider>();
                }
                catch (ObjectDisposedException) { }
            }
        }
        return _aktifFirmaProvider;
    }

    // Organizasyon ve Firma (Nihai Mimari Kural 2-3)
    public DbSet<Organizasyon> Organizasyonlar { get; set; }
    public DbSet<Sube> Subeler { get; set; }
    public DbSet<Firma> Firmalar { get; set; }

    // Cari Modulu
    public DbSet<Cari> Cariler { get; set; }
    public DbSet<CariSeferUcreti> CariSeferUcretleri { get; set; }

    // Kapasite Modulu
    public DbSet<Kapasite> Kapasiteler { get; set; }

    // Filo Servis Modulu
    public DbSet<Sofor> Soforler { get; set; }
    public DbSet<Arac> Araclar { get; set; }
    public DbSet<AracPlaka> AracPlakalar { get; set; }
    public DbSet<Guzergah> Guzergahlar { get; set; }
    public DbSet<GuzergahSefer> GuzergahSeferleri { get; set; }
    public DbSet<MasrafKalemi> MasrafKalemleri { get; set; }
    public DbSet<AracMasraf> AracMasraflari { get; set; }
    public DbSet<ServisCalisma> ServisCalismalari { get; set; }
    public DbSet<AracEvrak> AracEvraklari { get; set; }
    public DbSet<AracEvrakDosya> AracEvrakDosyalari { get; set; }

    // Fatura Modulu
    public DbSet<Fatura> Faturalar { get; set; }
    public DbSet<FaturaKalem> FaturaKalemleri { get; set; }

    // Banka/Kasa Modulu
    public DbSet<BankaHesap> BankaHesaplari { get; set; }
    public DbSet<BankaKasaHareket> BankaKasaHareketleri { get; set; }
    public DbSet<FirmalarArasiTransfer> FirmalarArasiTransferler { get; set; }
    public DbSet<OdemeEslestirme> OdemeEslestirmeleri { get; set; }

    // Checklist Modulu
    public DbSet<AylikChecklist> AylikChecklistler { get; set; }
    public DbSet<ChecklistKalem> ChecklistKalemleri { get; set; }

    // Personel Maas/Izin Modulu
    public DbSet<PersonelMaas> PersonelMaaslari { get; set; }
    public DbSet<PersonelIzin> PersonelIzinleri { get; set; }
    public DbSet<PersonelIzinHakki> PersonelIzinHaklari { get; set; }
    public DbSet<PersonelAracAtama> PersonelAracAtamalari { get; set; }

    // Butce Modulu
    public DbSet<BudgetOdeme> BudgetOdemeler { get; set; }
    public DbSet<BudgetMasrafKalemi> BudgetMasrafKalemleri { get; set; }
    public DbSet<TekrarlayanOdeme> TekrarlayanOdemeler { get; set; }
    public DbSet<BudgetHedef> BudgetHedefler { get; set; }

    // Muhasebe Modulu
    public DbSet<MuhasebeHesap> MuhasebeHesaplari { get; set; }
    public DbSet<MuhasebeFis> MuhasebeFisleri { get; set; }
    public DbSet<MuhasebeFisKalem> MuhasebeFisKalemleri { get; set; }
    public DbSet<MuhasebeDonem> MuhasebeDonemleri { get; set; }
    public DbSet<MuhasebeAyar> MuhasebeAyarlari { get; set; }
    public DbSet<KdvHesapEslestirme> KdvHesapEslestirmeleri { get; set; }
    public DbSet<KostMerkezi> KostMerkezleri { get; set; }
    public DbSet<MuhasebeProje> MuhasebeProjeler { get; set; }

    // Kullanici ve Lisans Modulu
    public DbSet<Lisans> Lisanslar { get; set; }
    public DbSet<Kullanici> Kullanicilar { get; set; }
    public DbSet<Rol> Roller { get; set; }
    public DbSet<RolYetki> RolYetkileri { get; set; }

    // Satis Modulu
    public DbSet<SatisPersoneli> SatisPersonelleri { get; set; }
    public DbSet<AracIlan> AracIlanlari { get; set; }
    public DbSet<PiyasaIlan> PiyasaIlanlari { get; set; }
    public DbSet<AracSatis> AracSatislari { get; set; }
    public DbSet<AracMarka> AracMarkalari { get; set; }
    public DbSet<AracModelTanim> AracModelleri { get; set; }

    // Sistem Modulu
    public DbSet<AktiviteLog> AktiviteLoglar { get; set; }

    // Kurumlar Modülü
    public DbSet<Kurum> Kurumlar { get; set; }

    // Aylik Odeme Modulu
    public DbSet<AylikOdemePlani> AylikOdemePlanlari { get; set; }
    public DbSet<AylikOdemeGerceklesen> AylikOdemeGerceklesenler { get; set; }

    // Kiralama ve Servis Takip Modulu
    public DbSet<KiralamaArac> KiralamaAraclar { get; set; }
    public DbSet<ServisCalismaKiralama> ServisCalismaKiralamalar { get; set; }

    // Musteri Kiralama Modulu
    public DbSet<MusteriKiralama> MusteriKiralamalar { get; set; }

    // Puantaj Modulu
    public DbSet<PersonelPuantaj> PersonelPuantajlar { get; set; }
    public DbSet<GunlukPuantaj> GunlukPuantajlar { get; set; }

    // Kiralık Plaka Takip
    public DbSet<KiralikPlakaTakip> KiralikPlakaTakipler { get; set; }

    // Filo Komisyon ve Araç Operasyon Puantaj Modülü
    public DbSet<FiloGuzergahEslestirme> FiloGuzergahEslestirmeleri { get; set; }
    public DbSet<FiloGunlukPuantaj> FiloGunlukPuantajlar { get; set; }

    // Puantaj Kalıcı Eşleştirme Modülü (Firma+Araç+Şoför ve Firma+Güzergah)
    public DbSet<FirmaAracSoforEslestirme> FirmaAracSoforEslestirmeleri { get; set; }
    public DbSet<FirmaGuzergahEslestirme> FirmaGuzergahEslestirmeleri { get; set; }

    // Hakediş ve Araç Maliyet Modülü
    public DbSet<Hakedis> Hakedisler { get; set; }
    public DbSet<HakedisDetay> HakedisDetaylari { get; set; }
    public DbSet<AracMaliyetSnapshot> AracMaliyetSnapshotlari { get; set; }

    // Piyasa Arastirma Modulu
    public DbSet<AracPiyasaArastirma> PiyasaArastirmalar { get; set; }
    public DbSet<PiyasaArastirmaIlan> PiyasaArastirmaIlanlar { get; set; }
    public DbSet<AracMarkaModel> AracMarkaModeller { get; set; }
    public DbSet<PiyasaKaynak> PiyasaKaynaklar { get; set; }

    // Filo Operasyon Modülü (Araç Alım/Satım, Kiralık C Plaka Takip)
    public DbSet<AracAlimSatim> AracAlimSatimlar { get; set; }
    public DbSet<PlakaDonusum> PlakaDonusumler { get; set; }
    public DbSet<AracOperasyonDurum> AracOperasyonDurumlari { get; set; }
    public DbSet<KiralikCPlakaTakip> KiralikCPlakaTakipler { get; set; }

    // CRM Modulu
    public DbSet<Bildirim> Bildirimler { get; set; }
    public DbSet<EpostaBildirimLog> EpostaBildirimLoglari { get; set; }
    public DbSet<Mesaj> Mesajlar { get; set; }
    public DbSet<EmailAyar> EmailAyarlari { get; set; }
    public DbSet<WhatsAppAyar> WhatsAppAyarlari { get; set; }
    public DbSet<SmsAyar> SmsAyarlari { get; set; }
    public DbSet<SmsLog> SmsLoglari { get; set; }
    public DbSet<SmsSablon> SmsSablonlari { get; set; }
    public DbSet<Hatirlatici> Hatirlaticilar { get; set; }
    public DbSet<KullaniciCari> KullaniciCariler { get; set; }
    public DbSet<DashboardWidget> DashboardWidgetlar { get; set; }
    public DbSet<CariIletisimNot> CariIletisimNotlar { get; set; }
    public DbSet<CariHatirlatma> CariHatirlatmalar { get; set; }

    // Webhook Sistemi
    public DbSet<WebhookEndpoint> WebhookEndpointler { get; set; }
    public DbSet<WebhookLog> WebhookLoglar { get; set; }

    // WhatsApp Iletisim Modulu
    public DbSet<WhatsAppKisi> WhatsAppKisiler { get; set; }
    public DbSet<WhatsAppGrup> WhatsAppGruplar { get; set; }
    public DbSet<WhatsAppGrupUye> WhatsAppGrupUyeler { get; set; }
    public DbSet<WhatsAppMesaj> WhatsAppMesajlar { get; set; }
    public DbSet<WhatsAppSablon> WhatsAppSablonlar { get; set; }

    // Stok/Envanter Modulu
    public DbSet<StokKarti> StokKartlari { get; set; }
    public DbSet<StokKategori> StokKategoriler { get; set; }
    public DbSet<StokHareket> StokHareketler { get; set; }
    public DbSet<AracIslem> AracIslemler { get; set; }
    public DbSet<ServisKaydi> ServisKayitlari { get; set; }
    public DbSet<ServisParca> ServisParcalar { get; set; }

    // Personel Özlük Evrak Modülü
    public DbSet<OzlukEvrakTanim> OzlukEvrakTanimlari { get; set; }
    public DbSet<PersonelOzlukEvrak> PersonelOzlukEvraklar { get; set; }

    // Fatura Şablon Modülü
    public DbSet<FaturaSablon> FaturaSablonlari { get; set; }

    // Personel Finans Modülü (Avans ve Borç Takip)
    public DbSet<PersonelAvans> PersonelAvanslar { get; set; }
    public DbSet<PersonelBorc> PersonelBorclar { get; set; }
    public DbSet<PersonelAvansMahsup> PersonelAvansMahsuplar { get; set; }
    public DbSet<PersonelBorcOdeme> PersonelBorcOdemeler { get; set; }
    public DbSet<PersonelFinansAyar> PersonelFinansAyarlar { get; set; }

    // Bordro Modülü
    public DbSet<Bordro> Bordrolar { get; set; }
    public DbSet<BordroDetay> BordroDetaylar { get; set; }
    public DbSet<BordroOdeme> BordroOdemeler { get; set; }
    public DbSet<BordroAyar> BordroAyarlar { get; set; }

    // Araç İlan Yayın ve Kullanıcı Tercihleri Modülü
    public DbSet<IlanPlatformu> IlanPlatformlari { get; set; }
    public DbSet<AracIlanYayin> AracIlanYayinlar { get; set; }
    public DbSet<AracIlanIcerik> AracIlanIcerikleri { get; set; }
    public DbSet<KullaniciTercihi> KullaniciTercihleri { get; set; }
    public DbSet<KullaniciSonIslem> KullaniciSonIslemler { get; set; }

    // Puantaj/Hakedis Modülü (Excel Import destekli)
    public DbSet<PuantajKayit> PuantajKayitlar { get; set; }
    public DbSet<PuantajExcelImport> PuantajExcelImportlar { get; set; }
    public DbSet<PuantajEslestirmeOneri> PuantajEslestirmeOnerileri { get; set; }
    public DbSet<OperasyonKaydi> OperasyonKayitlari { get; set; }
    public DbSet<PuantajHesapDonemi> PuantajHesapDonemleri { get; set; }
    public DbSet<PuantajDetay> PuantajDetaylari { get; set; }
    public DbSet<PuantajAuditLog> PuantajAuditLogs { get; set; }
    public DbSet<PuantajFinansalKayit> PuantajFinansalKayitlar { get; set; }
    public DbSet<PuantajJobExecution> PuantajJobExecutions { get; set; }

    // Hakediş Puantaj Modülü (Operasyonel — Personel maaş puantajından bağımsız)
    public DbSet<HakedisPuantaj> HakedisPuantajlar { get; set; }
    public DbSet<HakedisPuantajDetay> HakedisPuantajDetaylar { get; set; }
    public DbSet<HakedisKesinti> HakedisKesintiler { get; set; }
    public DbSet<HakedisSeferTuru> HakedisSeferTurleri { get; set; }

    // Proforma Fatura Modülü
    public DbSet<ProformaFatura> ProformaFaturalar { get; set; }
    public DbSet<ProformaFaturaKalem> ProformaFaturaKalemler { get; set; }

    // İhale Hazırlık Modülü
    public DbSet<IhaleProje> IhaleProjeleri { get; set; }
    public DbSet<IhaleGuzergahKalem> IhaleGuzergahKalemleri { get; set; }
    public DbSet<IhaleSozlesmeRevizyon> IhaleSozlesmeRevizyonlari { get; set; }
    public DbSet<IhaleTeklifVersiyon> IhaleTeklifVersiyonlari { get; set; }
    public DbSet<IhaleTeklifKararLog> IhaleTeklifKararLoglari { get; set; }
    public DbSet<IhaleRakipBenchmark> IhaleRakipBenchmarklar { get; set; }

    // Destek Talebi (Ticket) Modülü - osTicket benzeri
    public DbSet<DestekTalebi> DestekTalepleri { get; set; }
    public DbSet<DestekTalebiYanit> DestekTalebiYanitlari { get; set; }
    public DbSet<DestekTalebiEk> DestekTalebiEkleri { get; set; }
    public DbSet<DestekTalebiAktivite> DestekTalebiAktiviteleri { get; set; }
    public DbSet<DestekTalebiIliski> DestekTalebiIliskileri { get; set; }
    public DbSet<DestekDepartman> DestekDepartmanlari { get; set; }
    public DbSet<DestekDepartmanUye> DestekDepartmanUyeleri { get; set; }
    public DbSet<DestekKategori> DestekKategorileri { get; set; }
    public DbSet<DestekHazirYanit> DestekHazirYanitlari { get; set; }
    public DbSet<DestekBilgiBankasi> DestekBilgiBankasiMakaleleri { get; set; }
    public DbSet<DestekSla> DestekSlaListesi { get; set; }
    public DbSet<DestekAyar> DestekAyarlari { get; set; }

    // EBYS Gelen/Giden Evrak Modülü
    public DbSet<EbysEvrak> EbysEvraklar { get; set; }
    public DbSet<EbysEvrakKategori> EbysEvrakKategoriler { get; set; }
    public DbSet<EbysEvrakDosya> EbysEvrakDosyalar { get; set; }
    public DbSet<EbysEvrakAtama> EbysEvrakAtamalar { get; set; }
    public DbSet<EbysEvrakHareket> EbysEvrakHareketler { get; set; }

    // EBYS Belge Versiyon Modülü
    public DbSet<EbysEvrakDosyaVersiyon> EbysEvrakDosyaVersiyonlar { get; set; }
    public DbSet<AracEvrakDosyaVersiyon> AracEvrakDosyaVersiyonlar { get; set; }
    public DbSet<PersonelOzlukEvrakVersiyon> PersonelOzlukEvrakVersiyonlar { get; set; }

    // EBYS Belge Arama Modülü
    public DbSet<EbysAramaGecmisi> EbysAramaGecmisleri { get; set; }
    public DbSet<EbysKayitliArama> EbysKayitliAramalar { get; set; }

    // EBYS Semantic Search Modülü
    public DbSet<EbysBelgeEmbedding> EbysBelgeEmbeddingler { get; set; }

    // Bildirim Ayarları Modülü
    public DbSet<BildirimAyar> BildirimAyarlari { get; set; }

    // Multi-tenant (Legacy Şirket) - Faz 5.3-B3-i: DbSet kaldırıldı, entity dosyası silinecek

    // Araç Takip (GPS) Modülü
    public DbSet<AracTakipCihaz> AracTakipCihazlar { get; set; }
    public DbSet<AracKonum> AracKonumlar { get; set; }
    public DbSet<AracBolge> AracBolgeler { get; set; }
    public DbSet<AracBolgeAtama> AracBolgeAtamalar { get; set; }
    public DbSet<AracTakipAlarm> AracTakipAlarmlar { get; set; }

    // Şirketler Arası Transfer (Legacy) - Faz 5.3-B3-i: DbSet kaldırıldı, entity dosyası silinecek

    // Audit Log Modülü
    public DbSet<AuditLog> AuditLoglar { get; set; }
    public DbSet<BakimPeriyot> BakimPeriyotlar { get; set; }
    public DbSet<AracBakimUyari> AracBakimUyarilari { get; set; }

    // Lastik Takip Modülü
    public DbSet<LastikDepo> LastikDepolar { get; set; }
    public DbSet<LastikStok> LastikStoklar { get; set; }
    public DbSet<LastikDegisim> LastikDegisimler { get; set; }
    public DbSet<LastikSezonAyar> LastikSezonAyarlari { get; set; }

    public DbSet<AppAyarlari> AppAyarlari { get; set; }

    // Personel Taşıma Tedarikçileri
    public DbSet<TasimaTedarikci> TasimaTedarikciler { get; set; }
    public DbSet<TasimaTedarikciIs> TasimaTedarikciIsler { get; set; }
    public DbSet<TedarikciEvrak> TedarikciEvraklari { get; set; }
    public DbSet<TedarikciEvrakDosya> TedarikciEvrakDosyalari { get; set; }

    // Servis Operasyon (Özmal / Kiralık / Tedarikçi Kontrat + Puantaj + Ödeme/Tahsilat)
    public DbSet<ServisKontrat> ServisKontratlar { get; set; }
    public DbSet<ServisPuantaj> ServisPuantajlar { get; set; }
    public DbSet<ServisOdeme> ServisOdemeler { get; set; }
    public DbSet<ServisTahsilat> ServisTahsilatlar { get; set; }

    // Holding Modulu (Kural 13) — nihai mimari: tek veritabanında konsolide snapshot'lar
    public DbSet<HoldingVeri> HoldingVeriler { get; set; }

    // E-Fatura / E-Arsiv Entegrasyonu
    public DbSet<LucaPortalSettings> LucaPortalAyarlari { get; set; }

    // AI Puantaj Anomali Tespiti
    public DbSet<PuantajAnomali> PuantajAnomaliler { get; set; }
    public DbSet<HoldingRapor> HoldingRaporlar { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        var isSqlite = Database.IsSqlite();

        // Firma (Nihai Mimari Kural 2-3: Organizasyon → Firma → Şube)
        modelBuilder.Entity<Firma>(entity =>
        {
            entity.HasIndex(e => e.FirmaKodu).IsUnique();
            entity.Property(e => e.FirmaKodu).HasMaxLength(50);
            entity.Property(e => e.FirmaAdi).HasMaxLength(250);
            entity.Property(e => e.VergiNo).HasMaxLength(11);
            entity.HasIndex(e => e.CariId);
            // Firma.CariId -> Cari (kurum rolündeki firmanın muhasebe Cari kaydı)
            entity.HasOne<Cari>()
                .WithMany((string?)null)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            // Organizasyon → Firma (Kural 3: Her firma bir organizasyona bağlıdır)
            entity.HasOne(e => e.Organizasyon)
                .WithMany(o => o.Firmalar)
                .HasForeignKey(e => e.OrganizasyonId)
                .OnDelete(DeleteBehavior.Restrict);
            // Subeler (Kural 5) — Firma tarafında navigation yok, Sube tarafında tanımlı
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Organizasyon (Nihai Mimari Kural 2)
        modelBuilder.Entity<Organizasyon>(entity =>
        {
            entity.HasIndex(e => e.Kod).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(e => e.Adi).HasMaxLength(250);
            entity.Property(e => e.Kod).HasMaxLength(20);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Şube (Nihai Mimari Kural 2, Kural 5)
        modelBuilder.Entity<Sube>(entity =>
        {
            entity.HasIndex(e => new { e.FirmaId, e.SubeKodu }).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(e => e.SubeAdi).HasMaxLength(250);
            entity.Property(e => e.SubeKodu).HasMaxLength(20);
            entity.Property(e => e.Adres).HasMaxLength(500);
            entity.Property(e => e.Telefon).HasMaxLength(20);
            // Sube → Firma
            entity.HasOne(e => e.Firma)
                .WithMany(f => f.Subeler)
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Cari
        modelBuilder.Entity<Cari>(entity =>
        {
            // CariKodu unique - SQLite filter desteklemiyor, PostgreSQL destekliyor
            if (isSqlite)
            {
                entity.HasIndex(e => e.CariKodu).IsUnique();
            }
            else
            {
                entity.HasIndex(e => e.CariKodu)
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
            }
            
            entity.Property(e => e.CariKodu).HasMaxLength(50);
            entity.Property(e => e.Unvan).HasMaxLength(250);
            entity.Property(e => e.VergiNo).HasMaxLength(20);
            entity.Property(e => e.TcKimlikNo).HasMaxLength(11);
            entity.Property(e => e.Telefon).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            
            // Muhasebe Hesap ilişkisi
            entity.HasOne(e => e.MuhasebeHesap)
                .WithMany()
                .HasForeignKey(e => e.MuhasebeHesapId)
                .OnDelete(DeleteBehavior.SetNull);

            // Personel İş Avans Hesabı (195.01.xxx)
            entity.HasOne(e => e.PersonelAvansHesap)
                .WithMany()
                .HasForeignKey(e => e.PersonelAvansHesapId)
                .OnDelete(DeleteBehavior.SetNull);

            // Personel (Sofor) iliskisi
            entity.HasOne(e => e.Sofor)
                .WithMany()
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.SetNull);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Teknik Borç #5)

            // Cari -> Firma (FirmaId) ilişkisini EXPLICIT tanımla.
            // Firma.Cariler inverse navigation'ı ile eşlenerek shadow FK üretimi engellenir.
            entity.HasOne(e => e.Firma)
                .WithMany(f => f.Cariler)
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // CariSeferUcreti - Bir cariye birden fazla sefer ücreti tanımlanabilir
        modelBuilder.Entity<CariSeferUcreti>(entity =>
        {
            entity.HasIndex(e => new { e.CariId, e.GuzergahId, e.GecerlilikBaslangic });
            entity.Property(e => e.Tanim).HasMaxLength(150);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.SeferUcreti).HasPrecision(18, 2);

            entity.HasOne(e => e.Cari)
                .WithMany(c => c.SeferUcretleri)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Guzergah)
                .WithMany()
                .HasForeignKey(e => e.GuzergahId)
                .OnDelete(DeleteBehavior.SetNull);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Faz 5.3-B1, Teknik Borç #5)

            // Firma ilişkisi (Tenant)
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Şoför
        modelBuilder.Entity<Sofor>(entity =>
        {
            entity.ToTable("Personeller");
            entity.HasIndex(e => e.SoforKodu).IsUnique();
            entity.Property(e => e.SoforKodu).HasMaxLength(50);
            entity.Property(e => e.Ad).HasMaxLength(100);
            entity.Property(e => e.Soyad).HasMaxLength(100);
            entity.Property(e => e.TcKimlikNo).HasMaxLength(11);
            entity.Property(e => e.Telefon).HasMaxLength(20);
            entity.Property(e => e.BrutMaas).HasPrecision(18, 2);
            entity.Property(e => e.CalismaMiktari).HasPrecision(18, 2);
            entity.Property(e => e.BirimUcret).HasPrecision(18, 2);
            entity.Property(e => e.ResmiNetMaas).HasPrecision(18, 2);
            entity.Property(e => e.DigerMaas).HasPrecision(18, 2);
            entity.Property(e => e.NetMaas).HasPrecision(18, 2);
            entity.Property(e => e.TopluMaas).HasPrecision(18, 2);
            entity.Property(e => e.SgkMaasi).HasPrecision(18, 2);
            entity.Property(e => e.SGKBordroDahilMi).HasDefaultValue(false);
            entity.Property(e => e.BordroTipiPersonel).HasDefaultValue(PersonelBordroTipi.Yok);
            entity.Ignore(e => e.EkOdeme);

            // Muhasebe Hesap ilişkisi
            entity.HasOne(e => e.MuhasebeHesap)
                .WithMany()
                .HasForeignKey(e => e.MuhasebeHesapId)
                .OnDelete(DeleteBehavior.SetNull);

            // Personel Avans Hesap ilişkisi (195)
            entity.HasOne(e => e.PersonelAvansHesap)
                .WithMany()
                .HasForeignKey(e => e.PersonelAvansHesapId)
                .OnDelete(DeleteBehavior.SetNull);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Faz C-extend, Teknik Borç #5)

            // Firma ilişkisi (çalıştığı firma)
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.SgkCalismaTuru).HasDefaultValue(SgkCalismaTuru.TamZamanli).HasSentinel(null);

            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Araç
        modelBuilder.Entity<Arac>(entity =>
        {
            // Şase numarası unique
            if (isSqlite)
            {
                entity.HasIndex(e => e.SaseNo).IsUnique();
            }
            else
            {
                entity.HasIndex(e => e.SaseNo)
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
            }
            
            entity.Property(e => e.SaseNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AktifPlaka).HasMaxLength(15);
            entity.Property(e => e.Marka).HasMaxLength(50);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.MotorNo).HasMaxLength(50);
            entity.Property(e => e.Renk).HasMaxLength(30);
            entity.Property(e => e.GunlukKiraBedeli).HasPrecision(18, 2);
            entity.Property(e => e.AylikKiraBedeli).HasPrecision(18, 2);
            entity.Property(e => e.SeferBasinaKiraBedeli).HasPrecision(18, 2);
            entity.Property(e => e.KomisyonOrani).HasPrecision(5, 2);
            entity.Property(e => e.SabitKomisyonTutari).HasPrecision(18, 2);
            entity.Property(e => e.SatisFiyati).HasPrecision(18, 2);
            
            entity.HasOne(e => e.KiralikCari)
                .WithMany()
                .HasForeignKey(e => e.KiralikCariId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.KomisyoncuCari)
                .WithMany()
                .HasForeignKey(e => e.KomisyoncuCariId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Faz C-extend, Teknik Borç #5)

            // Firma ilişkisi (Tenant)
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            // PlakaGecmisi navigation'ı AracPlaka entity'sinde tanımlanıyor
            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Araç Plaka Geçmişi
        modelBuilder.Entity<AracPlaka>(entity =>
        {
            entity.Property(e => e.Plaka).HasMaxLength(15).IsRequired();
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.IslemTutari).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Arac)
                .WithMany(a => a.PlakaGecmisi)
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Aynı anda aynı plaka farklı araçta aktif olamaz
            entity.HasIndex(e => new { e.Plaka, e.CikisTarihi })
                .HasFilter("\"CikisTarihi\" IS NULL AND \"IsDeleted\" = false");
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Güzergah
        modelBuilder.Entity<Guzergah>(entity =>
        {
            entity.HasIndex(e => e.GuzergahKodu).IsUnique();
            entity.Property(e => e.GuzergahKodu).HasMaxLength(50);
            entity.Property(e => e.GuzergahAdi).HasMaxLength(200);
            entity.Property(e => e.BirimFiyat).HasPrecision(18, 2);
            entity.Property(e => e.GiderFiyat).HasPrecision(18, 2);
            entity.Property(e => e.PuantajCarpani).HasPrecision(10, 2).HasDefaultValue(1.0m);
            entity.Property(e => e.Mesafe).HasPrecision(10, 2);
            entity.HasOne(e => e.Cari)
                .WithMany(c => c.Guzergahlar)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Faz C-extend, Teknik Borç #5)

            // Firma ilişkisi (Tenant)
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Masraf Kalemi
        modelBuilder.Entity<MasrafKalemi>(entity =>
        {
            entity.HasIndex(e => e.MasrafKodu).IsUnique();
            entity.Property(e => e.MasrafKodu).HasMaxLength(50);
            entity.Property(e => e.MasrafAdi).HasMaxLength(200);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Kapasite
        modelBuilder.Entity<Kapasite>(entity =>
        {
            entity.HasIndex(e => new { e.FirmaId, e.KapasiteAdi });
            entity.Property(e => e.KapasiteAdi).HasMaxLength(100);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.Carpan).HasPrecision(18, 2);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Faz C-extend, Teknik Borç #5)

            // Firma ilişkisi
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Araç Masraf
        modelBuilder.Entity<AracMasraf>(entity =>
        {
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.HasOne(e => e.Arac)
                .WithMany(a => a.Masraflar)
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.MasrafKalemi)
                .WithMany(m => m.AracMasraflari)
                .HasForeignKey(e => e.MasrafKalemiId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Guzergah)
                .WithMany(g => g.AracMasraflari)
                .HasForeignKey(e => e.GuzergahId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ServisCalisma)
                .WithMany(s => s.ArizaMasraflari)
                .HasForeignKey(e => e.ServisCalismaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Sofor)
                .WithMany()
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.MuhasebeFis)
                .WithMany()
                .HasForeignKey(e => e.MuhasebeFisId)
                .OnDelete(DeleteBehavior.SetNull);

            // Personel cebinden harcama ilişkisi
            entity.HasOne(e => e.PersonelCebinden)
                .WithMany()
                .HasForeignKey(e => e.PersonelCebindenId)
                .OnDelete(DeleteBehavior.SetNull);

            // Banka hesap ilişkisi
            entity.HasOne(e => e.BankaHesap)
                .WithMany()
                .HasForeignKey(e => e.BankaHesapId)
                .OnDelete(DeleteBehavior.SetNull);

            // Personel cebinden index
            entity.HasIndex(e => new { e.PersonelCebindenId, e.PersoneleOdendi });

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Servis Çalışma
        modelBuilder.Entity<ServisCalisma>(entity =>
        {
            entity.HasIndex(e => e.CalismaTarihi);
            entity.HasIndex(e => new { e.AracId, e.CalismaTarihi });
            entity.HasIndex(e => new { e.SoforId, e.CalismaTarihi });
            entity.HasIndex(e => new { e.GuzergahId, e.CalismaTarihi });
            entity.Property(e => e.Fiyat).HasPrecision(18, 2);
            entity.HasOne(e => e.Arac)
                .WithMany(a => a.ServisCalismalari)
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Sofor)
                .WithMany(s => s.ServisCalismalari)
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Guzergah)
                .WithMany(g => g.ServisCalismalari)
                .HasForeignKey(e => e.GuzergahId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Fatura
        modelBuilder.Entity<Fatura>(entity =>
        {
            if (isSqlite)
            {
                entity.HasIndex(e => new { e.FirmaId, e.FaturaYonu, e.FaturaNo })
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");
            }
            else
            {
                entity.HasIndex(e => new { e.FirmaId, e.FaturaYonu, e.FaturaNo })
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
            }

            entity.HasIndex(e => e.FaturaTarihi);
            entity.HasIndex(e => new { e.CariId, e.FaturaTarihi });
            entity.HasIndex(e => new { e.Durum, e.VadeTarihi });
            entity.HasIndex(e => new { e.FaturaTipi, e.FaturaTarihi });
            entity.Property(e => e.FaturaNo).HasMaxLength(50);
            entity.Property(e => e.AraToplam).HasPrecision(18, 2);
            entity.Property(e => e.KdvOrani).HasPrecision(5, 2);
            entity.Property(e => e.KdvTutar).HasPrecision(18, 2);
            entity.Property(e => e.GenelToplam).HasPrecision(18, 2);
            entity.Property(e => e.OdenenTutar).HasPrecision(18, 2);
            entity.Property(e => e.TevkifatOrani).HasPrecision(5, 2);
            entity.Property(e => e.TevkifatTutar).HasPrecision(18, 2);
            entity.Property(e => e.TevkifatKodu).HasMaxLength(20);
            entity.HasOne(e => e.Cari)
                .WithMany(c => c.Faturalar)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);
            // Firmalar arası fatura - Karşı firma ilişkisi
            entity.HasOne(e => e.KarsiFirma)
                .WithMany()
                .HasForeignKey(e => e.KarsiFirmaId)
                .OnDelete(DeleteBehavior.SetNull);
            // Firmalar arası fatura eşleştirme ilişkisi
            entity.HasOne(e => e.EslesenFatura)
                .WithMany()
                .HasForeignKey(e => e.EslesenFaturaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Teknik Borç #5)

            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Fatura Kalem
        modelBuilder.Entity<FaturaKalem>(entity =>
        {
            entity.Property(e => e.BirimFiyat).HasPrecision(18, 2);
            entity.Property(e => e.KdvOrani).HasPrecision(5, 2);
            entity.Property(e => e.KdvTutar).HasPrecision(18, 2);
            entity.Property(e => e.ToplamTutar).HasPrecision(18, 2);
            entity.Property(e => e.Miktar).HasPrecision(18, 4);
            entity.Property(e => e.IskontoOrani).HasPrecision(5, 2);
            entity.Property(e => e.IskontoTutar).HasPrecision(18, 2);
            entity.Property(e => e.TevkifatOrani).HasPrecision(5, 2);
            entity.Property(e => e.TevkifatTutar).HasPrecision(18, 2);
            entity.Property(e => e.Birim).HasMaxLength(20);
            entity.Property(e => e.UrunKodu).HasMaxLength(50);
            entity.HasOne(e => e.Fatura)
                .WithMany(f => f.FaturaKalemleri)
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.MuhasebeHesap)
                .WithMany()
                .HasForeignKey(e => e.MuhasebeHesapId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Banka Hesap
        modelBuilder.Entity<BankaHesap>(entity =>
        {
            entity.HasIndex(e => e.HesapKodu).IsUnique();
            entity.Property(e => e.HesapKodu).HasMaxLength(50);
            entity.Property(e => e.HesapAdi).HasMaxLength(200);
            entity.Property(e => e.BankaAdi).HasMaxLength(100);
            entity.Property(e => e.ParaBirimi).HasMaxLength(3);
            entity.Property(e => e.AcilisBakiye).HasPrecision(18, 2);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Faz C-extend, Teknik Borç #5)

            // Firma ilişkisi (Tenant)
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Banka/Kasa Hareket
        modelBuilder.Entity<BankaKasaHareket>(entity =>
        {
            entity.HasIndex(e => e.IslemNo).IsUnique();
            entity.HasIndex(e => new { e.BankaHesapId, e.IslemTarihi });
            entity.HasIndex(e => new { e.CariId, e.IslemTarihi });
            entity.HasIndex(e => new { e.HareketTipi, e.IslemTarihi });
            entity.HasIndex(e => new { e.PersonelCebindenId, e.PersoneleOdendi }); // Personel cebinden index
            entity.Property(e => e.IslemNo).HasMaxLength(50);
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.HasOne(e => e.BankaHesap)
                .WithMany(b => b.Hareketler)
                .HasForeignKey(e => e.BankaHesapId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Cari)
                .WithMany(c => c.BankaKasaHareketler)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);

            // Arac iliskisi (ozellikle personel cebinden arac masraflari icin)
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);

            // Personel cebinden harcama ilişkisi
            entity.HasOne(e => e.PersonelCebinden)
                .WithMany()
                .HasForeignKey(e => e.PersonelCebindenId)
                .OnDelete(DeleteBehavior.SetNull);

            // Personel geri ödeme ilişkisi (self-reference)
            entity.HasOne(e => e.PersonelGeriOdemeHareket)
                .WithMany()
                .HasForeignKey(e => e.PersonelGeriOdemeHareketId)
                .OnDelete(DeleteBehavior.SetNull);

            // Muhasebe fişi ilişkisi
            entity.HasOne(e => e.MuhasebeFis)
                .WithMany()
                .HasForeignKey(e => e.MuhasebeFisId)
                .OnDelete(DeleteBehavior.SetNull);

            // Sirket iliskisi (Multi-tenant) - LEGACY drop edildi (Faz C-extend, Teknik Borç #5)

            // Firma ilişkisi (Tenant)
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter: IsDeleted (Tenant izolasyonu IFirmaTenant filter'ı tarafından otomatik ekleniyor)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Firmalar Arası Transfer (K6)
        modelBuilder.Entity<FirmalarArasiTransfer>(entity =>
        {
            entity.HasIndex(e => e.KaynakFirmaId);
            entity.HasIndex(e => e.HedefFirmaId);
            entity.HasIndex(e => e.TransferTarihi);

            entity.HasOne(e => e.KaynakFirma)
                .WithMany()
                .HasForeignKey(e => e.KaynakFirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.HedefFirma)
                .WithMany()
                .HasForeignKey(e => e.HedefFirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.KaynakHesap)
                .WithMany()
                .HasForeignKey(e => e.KaynakHesapId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.HedefHesap)
                .WithMany()
                .HasForeignKey(e => e.HedefHesapId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.KaynakHareket)
                .WithMany()
                .HasForeignKey(e => e.KaynakHareketId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.HedefHareket)
                .WithMany()
                .HasForeignKey(e => e.HedefHareketId)
                .OnDelete(DeleteBehavior.SetNull);

            // FirmaId getter/setter KaynakFirmaId'yi proxy'ler; EF'in ayrı kolon yaratmasını engelle.
            entity.Ignore(e => e.FirmaId);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Ödeme Eşleştirme
        modelBuilder.Entity<OdemeEslestirme>(entity =>
        {
            entity.Property(e => e.EslestirilenTutar).HasPrecision(18, 2);
            entity.HasOne(e => e.Fatura)
                .WithMany(f => f.OdemeEslestirmeleri)
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BankaKasaHareket)
                .WithMany(b => b.OdemeEslestirmeleri)
                .HasForeignKey(e => e.BankaKasaHareketId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AylikChecklist
        modelBuilder.Entity<AylikChecklist>(entity =>
        {
            entity.HasIndex(e => new { e.Yil, e.Ay, e.ChecklistTipi, e.SoforId, e.AracId, e.GuzergahId });
            entity.Property(e => e.KontrolEden).HasMaxLength(100);
            
            entity.HasOne(e => e.Sofor)
                .WithMany()
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Guzergah)
                .WithMany()
                .HasForeignKey(e => e.GuzergahId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Checklist Kalem
        modelBuilder.Entity<ChecklistKalem>(entity =>
        {
            entity.Property(e => e.KalemAdi).HasMaxLength(200);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.HasOne(e => e.AylikChecklist)
                .WithMany(ac => ac.Kalemler)
                .HasForeignKey(e => e.AylikChecklistId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Personel Maaş
        modelBuilder.Entity<PersonelMaas>(entity =>
        {
            entity.HasIndex(e => new { e.SoforId, e.Yil, e.Ay }).IsUnique();
            entity.Property(e => e.BrutMaas).HasPrecision(18, 2);
            entity.Property(e => e.NetMaas).HasPrecision(18, 2);
            entity.Property(e => e.SGKIsciPayi).HasPrecision(18, 2);
            entity.Property(e => e.SGKIsverenPayi).HasPrecision(18, 2);
            entity.Property(e => e.GelirVergisi).HasPrecision(18, 2);
            entity.Property(e => e.DamgaVergisi).HasPrecision(18, 2);
            entity.Property(e => e.IssizlikPrimi).HasPrecision(18, 2);
            entity.Property(e => e.Prim).HasPrecision(18, 2);
            entity.Property(e => e.Ikramiye).HasPrecision(18, 2);
            entity.Property(e => e.Yemek).HasPrecision(18, 2);
            entity.Property(e => e.Yol).HasPrecision(18, 2);
            entity.Property(e => e.Mesai).HasPrecision(18, 2);
            entity.Property(e => e.DigerEklemeler).HasPrecision(18, 2);
            entity.Property(e => e.Avans).HasPrecision(18, 2);
            entity.Property(e => e.IcraTakibi).HasPrecision(18, 2);
            entity.Property(e => e.DigerKesintiler).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Sofor)
                .WithMany(s => s.Maaslar)
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Personel İzin
        modelBuilder.Entity<PersonelIzin>(entity =>
        {
            entity.HasOne(e => e.Sofor)
                .WithMany(s => s.Izinler)
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Personel İzin Hakkı
        modelBuilder.Entity<PersonelIzinHakki>(entity =>
        {
            entity.HasIndex(e => new { e.SoforId, e.Yil }).IsUnique();
            entity.HasOne(e => e.Sofor)
                .WithMany(s => s.IzinHaklari)
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Budget Ödeme
        modelBuilder.Entity<BudgetOdeme>(entity =>
        {
            entity.HasIndex(e => new { e.OdemeYil, e.OdemeAy, e.MasrafKalemi });
            entity.HasIndex(e => e.TaksitGrupId);
            entity.Property(e => e.MasrafKalemi).HasMaxLength(200);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.Notlar).HasMaxLength(1000);
            entity.Property(e => e.Miktar).HasPrecision(18, 2);
            entity.Property(e => e.ToplamKismiOdenen).HasPrecision(18, 2);
            entity.Property(e => e.Durum).HasConversion<int>();
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Self-referencing ilişkiler (Kısmi ödeme dönem aktarımı)
            entity.HasOne(e => e.SonrakiDonemOdeme)
                .WithOne()
                .HasForeignKey<BudgetOdeme>(e => e.SonrakiDonemOdemeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OncekiDonemOdeme)
                .WithOne()
                .HasForeignKey<BudgetOdeme>(e => e.OncekiDonemOdemeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Budget Masraf Kalemi
        modelBuilder.Entity<BudgetMasrafKalemi>(entity =>
        {
            entity.HasIndex(e => e.KalemAdi);
            entity.Property(e => e.KalemAdi).HasMaxLength(200);
            entity.Property(e => e.Kategori).HasMaxLength(100);
            entity.Property(e => e.Renk).HasMaxLength(20);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Tekrarlayan Odeme
        modelBuilder.Entity<TekrarlayanOdeme>(entity =>
        {
            entity.HasIndex(e => e.MasrafKalemi);
            entity.Property(e => e.OdemeAdi).HasMaxLength(200);
            entity.Property(e => e.MasrafKalemi).HasMaxLength(200);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.Notlar).HasMaxLength(1000);
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.Property(e => e.Renk).HasMaxLength(20);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Periyod).HasConversion<int>();
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Muhasebe Hesap
        modelBuilder.Entity<MuhasebeHesap>(entity =>
        {
            entity.HasIndex(e => e.HesapKodu).IsUnique();
            entity.Property(e => e.HesapKodu).HasMaxLength(50);
            entity.Property(e => e.HesapAdi).HasMaxLength(200);
            entity.HasOne(e => e.UstHesap)
                .WithMany()
                .HasForeignKey(e => e.UstHesapId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Muhasebe Fis
        modelBuilder.Entity<MuhasebeFis>(entity =>
        {
            entity.HasIndex(e => e.FisNo).IsUnique();
            entity.Property(e => e.FisNo).HasMaxLength(50);
            entity.Property(e => e.ToplamBorc).HasPrecision(18, 2);
            entity.Property(e => e.ToplamAlacak).HasPrecision(18, 2);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Muhasebe Fis Kalem
        modelBuilder.Entity<MuhasebeFisKalem>(entity =>
        {
            entity.Property(e => e.Borc).HasPrecision(18, 2);
            entity.Property(e => e.Alacak).HasPrecision(18, 2);
            entity.HasOne(e => e.Fis)
                .WithMany(f => f.Kalemler)
                .HasForeignKey(e => e.FisId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Hesap)
                .WithMany(h => h.FisKalemleri)
                .HasForeignKey(e => e.HesapId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Muhasebe Donem
        modelBuilder.Entity<MuhasebeDonem>(entity =>
        {
            entity.HasIndex(e => new { e.Yil, e.Ay }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Aktivite Log
        modelBuilder.Entity<AktiviteLog>(entity =>
        {
            entity.HasIndex(e => e.IslemZamani);
            entity.HasIndex(e => new { e.Modul, e.IslemTipi });
            entity.Property(e => e.IslemTipi).HasMaxLength(50);
            entity.Property(e => e.Modul).HasMaxLength(100);
            entity.Property(e => e.EntityTipi).HasMaxLength(100);
            entity.Property(e => e.EntityAdi).HasMaxLength(500);
            entity.Property(e => e.Aciklama).HasMaxLength(1000);
            entity.Property(e => e.KullaniciAdi).HasMaxLength(100);
            entity.Property(e => e.IpAdresi).HasMaxLength(50);
            entity.Property(e => e.Tarayici).HasMaxLength(500);
            // Log tablosunda soft delete yok
        });

        // Kullanici
        modelBuilder.Entity<Kullanici>(entity =>
        {
            entity.HasIndex(e => e.KullaniciAdi).IsUnique();
            entity.Property(e => e.KullaniciAdi).HasMaxLength(50);
            entity.Property(e => e.AdSoyad).HasMaxLength(100);
            entity.Property(e => e.IkiFaktorSecretKey).HasMaxLength(200);
            entity.HasOne(e => e.Rol)
                .WithMany(r => r.Kullanicilar)
                .HasForeignKey(e => e.RolId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Sofor)
                .WithMany()
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Sirket (Multi-tenant) - Faz 5.3-B3-i: entity konfigürasyonu kaldırıldı, Sirket.cs silinecek

        // Rol
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasIndex(e => e.RolAdi).IsUnique();
            entity.Property(e => e.RolAdi).HasMaxLength(50);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // RolYetki
        modelBuilder.Entity<RolYetki>(entity =>
        {
            entity.HasIndex(e => new { e.RolId, e.YetkiKodu }).IsUnique();
            entity.Property(e => e.YetkiKodu).HasMaxLength(100);
            entity.HasOne(e => e.Rol)
                .WithMany(r => r.Yetkiler)
                .HasForeignKey(e => e.RolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Lisans
        modelBuilder.Entity<Lisans>(entity =>
        {
            entity.HasIndex(e => e.LisansAnahtari).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // SatisPersoneli
        modelBuilder.Entity<SatisPersoneli>(entity =>
        {
            entity.HasIndex(e => e.PersonelKodu).IsUnique();
            entity.Property(e => e.PersonelKodu).HasMaxLength(50);
            entity.Property(e => e.AdSoyad).HasMaxLength(100);
            entity.Property(e => e.KomisyonOrani).HasPrecision(5, 2);
            entity.Property(e => e.SabitKomisyon).HasPrecision(18, 2);
            entity.Property(e => e.AylikSatisHedefi).HasPrecision(18, 2);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracIlan
        modelBuilder.Entity<AracIlan>(entity =>
        {
            entity.HasIndex(e => e.Plaka);
            entity.Property(e => e.Plaka).HasMaxLength(15);
            entity.Property(e => e.Marka).HasMaxLength(50);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.AlisFiyati).HasPrecision(18, 2);
            entity.Property(e => e.SatisFiyati).HasPrecision(18, 2);
            entity.Property(e => e.KaskoDegeri).HasPrecision(18, 2);
            entity.Property(e => e.PiyasaDegeriMin).HasPrecision(18, 2);
            entity.Property(e => e.PiyasaDegeriMax).HasPrecision(18, 2);
            entity.Property(e => e.PiyasaDegeriOrtalama).HasPrecision(18, 2);
            entity.Property(e => e.TramerTutari).HasPrecision(18, 2);
            entity.HasOne(e => e.SahipCari)
                .WithMany()
                .HasForeignKey(e => e.SahipCariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SatisPersoneli)
                .WithMany(p => p.Ilanlar)
                .HasForeignKey(e => e.SatisPersoneliId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PiyasaIlan
        modelBuilder.Entity<PiyasaIlan>(entity =>
        {
            entity.Property(e => e.Fiyat).HasPrecision(18, 2);
            entity.Property(e => e.TramerTutari).HasPrecision(18, 2);
            entity.HasOne(e => e.AracIlan)
                .WithMany(a => a.PiyasaIlanlari)
                .HasForeignKey(e => e.AracIlanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracSatis
        modelBuilder.Entity<AracSatis>(entity =>
        {
            entity.Property(e => e.SatisFiyati).HasPrecision(18, 2);
            entity.Property(e => e.KomisyonTutari).HasPrecision(18, 2);
            entity.HasOne(e => e.AracIlan)
                .WithMany()
                .HasForeignKey(e => e.AracIlanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AliciCari)
                .WithMany()
                .HasForeignKey(e => e.AliciCariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SatisPersoneli)
                .WithMany(p => p.Satislar)
                .HasForeignKey(e => e.SatisPersoneliId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ===== FİLO OPERASYON MODÜLÜ =====

        // AracAlimSatim
        modelBuilder.Entity<AracAlimSatim>(entity =>
        {
            entity.HasIndex(e => new { e.AracId, e.IslemTarihi });
            entity.Property(e => e.KarsiTarafAdSoyad).HasMaxLength(100);
            entity.Property(e => e.KarsiTarafTcKimlik).HasMaxLength(11);
            entity.Property(e => e.KarsiTarafTelefon).HasMaxLength(20);
            entity.Property(e => e.IslemTutari).HasPrecision(18, 2);
            entity.Property(e => e.KDVTutari).HasPrecision(18, 2);
            entity.Property(e => e.ToplamTutar).HasPrecision(18, 2);
            entity.Property(e => e.NoterAdi).HasMaxLength(100);
            entity.Property(e => e.NoterYevmiyeNo).HasMaxLength(50);
            entity.Property(e => e.OdenenTutar).HasPrecision(18, 2);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.KarsiTarafCari)
                .WithMany()
                .HasForeignKey(e => e.KarsiTarafCariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Fatura)
                .WithMany()
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PlakaDonusum
        modelBuilder.Entity<PlakaDonusum>(entity =>
        {
            entity.HasIndex(e => new { e.AracId, e.EskiPlaka });
            entity.Property(e => e.EskiPlaka).HasMaxLength(15);
            entity.Property(e => e.YeniPlaka).HasMaxLength(15);
            entity.Property(e => e.PlakaBedeliMasrafi).HasPrecision(18, 2);
            entity.Property(e => e.EmnivetHarci).HasPrecision(18, 2);
            entity.Property(e => e.NoterMasrafi).HasPrecision(18, 2);
            entity.Property(e => e.DigerMasraflar).HasPrecision(18, 2);
            entity.Property(e => e.PlakaSatisBedeli).HasPrecision(18, 2);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PlakaSatisCarisi)
                .WithMany()
                .HasForeignKey(e => e.PlakaSatisCarisiId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });


        // KiralikPlakaTakip
        modelBuilder.Entity<KiralikPlakaTakip>(entity =>
        {
            entity.Property(e => e.Plaka).HasMaxLength(15).IsRequired();
            entity.Property(e => e.IsimSoyisim).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Durum).HasMaxLength(50);
            entity.Property(e => e.KasaDurumu).HasMaxLength(50);
            entity.Property(e => e.Periyot).HasMaxLength(20);
            entity.Property(e => e.FaturaOdemesi).HasPrecision(18, 2);
            entity.Property(e => e.AylikVeyaYillikTutar).HasPrecision(18, 2);

            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        // AracOperasyonDurum
        modelBuilder.Entity<AracOperasyonDurum>(entity =>
        {
            entity.HasIndex(e => new { e.AracId, e.Yil, e.Ay }).IsUnique();
            entity.Property(e => e.BrutGelir).HasPrecision(18, 2);
            entity.Property(e => e.KomisyonKesintisi).HasPrecision(18, 2);
            entity.Property(e => e.YakitGideri).HasPrecision(18, 2);
            entity.Property(e => e.SoforMaliyeti).HasPrecision(18, 2);
            entity.Property(e => e.KiraBedeli).HasPrecision(18, 2);
            entity.Property(e => e.BakimOnarimGideri).HasPrecision(18, 2);
            entity.Property(e => e.SigortaGideri).HasPrecision(18, 2);
            entity.Property(e => e.VergiGideri).HasPrecision(18, 2);
            entity.Property(e => e.OtoyolGideri).HasPrecision(18, 2);
            entity.Property(e => e.DigerGiderler).HasPrecision(18, 2);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ===== ARAÇ İLAN YAYIN VE KULLANICI TERCİHLERİ MODÜLÜ =====

        // IlanPlatformu (arabam, sahibinden, letgo vb.)
        modelBuilder.Entity<IlanPlatformu>(entity =>
        {
            entity.HasIndex(e => e.PlatformAdi).IsUnique();
            entity.Property(e => e.PlatformAdi).HasMaxLength(50);
            entity.Property(e => e.WebSiteUrl).HasMaxLength(100);
            entity.Property(e => e.ApiUrl).HasMaxLength(100);
            entity.Property(e => e.ApiKey).HasMaxLength(200);
            entity.Property(e => e.ApiSecret).HasMaxLength(100);
            entity.Property(e => e.KullaniciAdi).HasMaxLength(100);
            entity.Property(e => e.Sifre).HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Renk).HasMaxLength(20);
            entity.Property(e => e.Notlar).HasMaxLength(500);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracIlanYayin - hangi araç hangi platformda yayında
        modelBuilder.Entity<AracIlanYayin>(entity =>
        {
            entity.HasIndex(e => new { e.AracId, e.PlatformId }).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(e => e.PlatformIlanNo).HasMaxLength(100);
            entity.Property(e => e.PlatformIlanUrl).HasMaxLength(500);
            entity.Property(e => e.YayinFiyati).HasPrecision(18, 2);
            entity.Property(e => e.FiyatAciklama).HasMaxLength(50);
            entity.Property(e => e.OneCikarmaBedeli).HasPrecision(18, 2);
            entity.Property(e => e.Notlar).HasMaxLength(500);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Platform)
                .WithMany(p => p.Yayinlar)
                .HasForeignKey(e => e.PlatformId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.YayinlayanKullanici)
                .WithMany()
                .HasForeignKey(e => e.YayinlayanKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracIlanIcerik - ilan içeriği (başlık, açıklama, fotoğraflar)
        modelBuilder.Entity<AracIlanIcerik>(entity =>
        {
            entity.HasIndex(e => new { e.AracId, e.PlatformId });
            entity.Property(e => e.IlanBasligi).HasMaxLength(200);
            entity.Property(e => e.MetaBaslik).HasMaxLength(200);
            entity.Property(e => e.MetaAciklama).HasMaxLength(500);
            entity.Property(e => e.AnahtarKelimeler).HasMaxLength(200);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Platform)
                .WithMany()
                .HasForeignKey(e => e.PlatformId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // KullaniciTercihi - varsayılan anasayfa, tema, bildirimler
        modelBuilder.Entity<KullaniciTercihi>(entity =>
        {
            entity.HasIndex(e => e.KullaniciId).IsUnique();
            entity.Property(e => e.VarsayilanAnasayfa).HasMaxLength(100);
            entity.Property(e => e.Tema).HasMaxLength(20);
            entity.Property(e => e.SidebarDurum).HasMaxLength(20);
            entity.Property(e => e.VarsayilanSiralama).HasMaxLength(2000);
            entity.Property(e => e.AnasayfaWidgetSirasi).HasMaxLength(2000);
            entity.Property(e => e.DigerTercihler).HasMaxLength(4000);
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // KullaniciSonIslem - son erişilen sayfalar
        modelBuilder.Entity<KullaniciSonIslem>(entity =>
        {
            entity.HasIndex(e => new { e.KullaniciId, e.SayfaYolu });
            entity.Property(e => e.SayfaYolu).HasMaxLength(200);
            entity.Property(e => e.SayfaBasligi).HasMaxLength(200);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ===== PUANTAJ/HAKEDİS MODÜLÜ KONFIGURASYONLARI =====

        // PuantajKayit - Excel import ve manuel giriş
        modelBuilder.Entity<PuantajKayit>(entity =>
        {
            entity.HasIndex(e => new { e.Yil, e.Ay, e.GuzergahId, e.AracId, e.Slot })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(e => new { e.Yil, e.Ay, e.KurumCariId });
            entity.Property(e => e.KurumAdi).HasMaxLength(200);
            entity.Property(e => e.GuzergahAdi).HasMaxLength(200);
            entity.Property(e => e.Plaka).HasMaxLength(20);
            entity.Property(e => e.SoforAdi).HasMaxLength(100);
            entity.Property(e => e.SoforTelefon).HasMaxLength(20);
            entity.Property(e => e.FaturaKesiciAdi).HasMaxLength(200);
            entity.Property(e => e.FaturaKesiciTelefon).HasMaxLength(20);
            entity.Property(e => e.GelirFaturaNo).HasMaxLength(50);
            entity.Property(e => e.GiderFaturaNo).HasMaxLength(50);
            entity.Property(e => e.OnaylayanKullanici).HasMaxLength(100);
            entity.Property(e => e.Notlar).HasMaxLength(1000);
            entity.Property(e => e.Bolge).HasMaxLength(100);
            entity.Property(e => e.AitFirmaAdi).HasMaxLength(200);
            entity.Property(e => e.BelgeNo).HasMaxLength(50);
            entity.Property(e => e.TransferDurum).HasMaxLength(50);

            // Decimal precision
            entity.Property(e => e.Gun).HasPrecision(10, 2);
            entity.Property(e => e.BirimGelir).HasPrecision(18, 2);
            entity.Property(e => e.ToplamGelir).HasPrecision(18, 2);
            entity.Property(e => e.GelirKdvTutari).HasPrecision(18, 2);
            entity.Property(e => e.GelirToplam).HasPrecision(18, 2);
            entity.Property(e => e.BirimGider).HasPrecision(18, 2);
            entity.Property(e => e.ToplamGider).HasPrecision(18, 2);
            entity.Property(e => e.GiderKdv20Tutari).HasPrecision(18, 2);
            entity.Property(e => e.GiderKdv10Tutari).HasPrecision(18, 2);
            entity.Property(e => e.GiderKesinti).HasPrecision(18, 2);
            entity.Property(e => e.Odenecek).HasPrecision(18, 2);
            entity.Property(e => e.GelirOdenenTutar).HasPrecision(18, 2);
            entity.Property(e => e.GiderOdenenTutar).HasPrecision(18, 2);

            // Enum → string dönüşümleri (DB'de text olarak saklanan enum kolonları)
            // NOT: HasDefaultValue KULLANILMAZ — enum default=0 olduğu için EF Core sentinel
            // uyarısı verir ve her zaman DB default'u kullanır. Değer garantisi servis katmanında
            // EnsurePuantajDefaults() ile sağlanır.
            entity.Property(e => e.Yon).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.OnayDurum).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.SoforOdemeTipi).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Kaynak).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.GelirOdemeDurumu).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.GiderOdemeDurumu).HasConversion<string>().HasMaxLength(20);

            // İlişkiler
            entity.HasOne(e => e.KurumCari)
                .WithMany()
                .HasForeignKey(e => e.KurumCariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Guzergah)
                .WithMany()
                .HasForeignKey(e => e.GuzergahId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Sofor)
                .WithMany()
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.OdemeYapilacakCari)
                .WithMany()
                .HasForeignKey(e => e.OdemeYapilacakCariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.FaturaKesiciCari)
                .WithMany()
                .HasForeignKey(e => e.FaturaKesiciCariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Kurum)
                .WithMany()
                .HasForeignKey(e => e.KurumId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.IsverenFirma)
                .WithMany()
                .HasForeignKey(e => e.IsverenFirmaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.HesapDonemi)
                .WithMany(h => h.PuantajKayitlari)
                .HasForeignKey(e => e.HesapDonemiId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.OncekiVersiyon)
                .WithMany()
                .HasForeignKey(e => e.OncekiVersiyonId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PuantajExcelImport - import batch kaydı
        modelBuilder.Entity<PuantajExcelImport>(entity =>
        {
            entity.HasIndex(e => new { e.Yil, e.Ay });
            entity.Property(e => e.DosyaAdi).HasMaxLength(200);
            entity.Property(e => e.ImportEdenKullanici).HasMaxLength(100);
            entity.Property(e => e.HataMesaji).HasMaxLength(2000);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PuantajEslestirmeOneri - import eşleştirme önerileri
        modelBuilder.Entity<PuantajEslestirmeOneri>(entity =>
        {
            entity.HasIndex(e => new { e.ExcelImportId, e.Tip, e.ExcelDeger });
            entity.Property(e => e.ExcelDeger).HasMaxLength(200);
            entity.Property(e => e.OnerilenAd).HasMaxLength(200);
            entity.HasOne(e => e.ExcelImport)
                .WithMany()
                .HasForeignKey(e => e.ExcelImportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // OperasyonKaydi - Günlük ham operasyon kaydı
        modelBuilder.Entity<OperasyonKaydi>(entity =>
        {
            entity.HasIndex(e => new { e.Tarih, e.GuzergahId, e.AracId, e.Slot }).IsUnique();
            entity.HasIndex(e => e.Tarih);
            entity.HasIndex(e => e.OperasyonDurumu);
            entity.HasIndex(e => e.Slot);
            entity.HasIndex(e => new { e.FirmaId, e.Tarih });
            entity.HasIndex(e => new { e.Tarih, e.KurumId });
            entity.HasIndex(e => new { e.Tarih, e.AracId });

            entity.Property(e => e.SlotAdi).HasMaxLength(50);
            entity.Property(e => e.BelgeNo).HasMaxLength(50);
            entity.Property(e => e.TransferDurum).HasMaxLength(50);
            entity.Property(e => e.Notlar).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.DeletedBy).HasMaxLength(100);
            entity.Property(e => e.PuantajCarpani).HasPrecision(10, 2);

            entity.HasOne(e => e.Guzergah).WithMany().HasForeignKey(e => e.GuzergahId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Arac).WithMany().HasForeignKey(e => e.AracId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Sofor).WithMany().HasForeignKey(e => e.SoforId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Kurum).WithMany().HasForeignKey(e => e.KurumId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.IsverenFirma).WithMany().HasForeignKey(e => e.IsverenFirmaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.OdemeYapilacakCari).WithMany().HasForeignKey(e => e.OdemeYapilacakCariId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.FaturaKesiciCari).WithMany().HasForeignKey(e => e.FaturaKesiciCariId).OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PuantajHesapDonemi - Hesaplama döngüsü / batch
        modelBuilder.Entity<PuantajHesapDonemi>(entity =>
        {
            entity.HasIndex(e => new { e.FirmaId, e.Yil, e.Ay, e.KurumId, e.Versiyon }).IsUnique();
            entity.HasIndex(e => new { e.FirmaId, e.Yil, e.Ay, e.KurumId });
            entity.HasIndex(e => e.OncekiDonemId);
            entity.HasIndex(e => e.Durum);

            entity.Property(e => e.HesaplayanKullanici).HasMaxLength(100);
            entity.Property(e => e.Notlar).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.DeletedBy).HasMaxLength(100);

            entity.HasOne(e => e.OncekiDonem)
                .WithMany()
                .HasForeignKey(e => e.OncekiDonemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PuantajDetay - Operasyon ↔ Puantaj bağlantısı (snapshot + audit)
        modelBuilder.Entity<PuantajDetay>(entity =>
        {
            entity.HasIndex(e => new { e.OperasyonKaydiId, e.HesapDonemiId }).IsUnique();
            entity.HasIndex(e => e.PuantajKayitId);
            entity.HasIndex(e => e.HesapDonemiId);

            entity.Property(e => e.BirimGelir).HasPrecision(18, 2);
            entity.Property(e => e.BirimGider).HasPrecision(18, 2);
            entity.Property(e => e.HesaplananTutar).HasPrecision(18, 2);

            entity.HasOne(e => e.OperasyonKaydi)
                .WithMany()
                .HasForeignKey(e => e.OperasyonKaydiId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PuantajKayit)
                .WithMany(p => p.PuantajDetaylari)
                .HasForeignKey(e => e.PuantajKayitId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.HesapDonemi)
                .WithMany(h => h.Detaylar)
                .HasForeignKey(e => e.HesapDonemiId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PuantajAuditLog - Onay/hesap aksiyon logları
        modelBuilder.Entity<PuantajAuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.HesapDonemiId, e.AksiyonTarihi });
            entity.HasIndex(e => e.FirmaId);

            entity.Property(e => e.Kullanici).HasMaxLength(100);
            entity.Property(e => e.OncekiDurum).HasMaxLength(100);
            entity.Property(e => e.YeniDurum).HasMaxLength(100);
            entity.Property(e => e.Aciklama).HasMaxLength(500);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PuantajFinansalKayit - Onaylanmış puantajın finansal snapshot'ı
        modelBuilder.Entity<PuantajFinansalKayit>(entity =>
        {
            entity.HasIndex(e => new { e.PuantajKayitId, e.HesapDonemiId }).IsUnique();
            entity.HasIndex(e => e.HesapDonemiId);
            entity.HasIndex(e => e.Durum);

            entity.Property(e => e.BirimGelir).HasPrecision(18, 2);
            entity.Property(e => e.BirimGider).HasPrecision(18, 2);
            entity.Property(e => e.ToplamGelir).HasPrecision(18, 2);
            entity.Property(e => e.ToplamGider).HasPrecision(18, 2);
            entity.Property(e => e.KdvTutar).HasPrecision(18, 2);
            entity.Property(e => e.GenelToplam).HasPrecision(18, 2);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            entity.HasOne(e => e.PuantajKayit).WithMany().HasForeignKey(e => e.PuantajKayitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.HesapDonemi).WithMany().HasForeignKey(e => e.HesapDonemiId).OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PuantajJobExecution - Quartz job çalışma kaydı + table-based mutex
        modelBuilder.Entity<PuantajJobExecution>(entity =>
        {
            entity.HasIndex(e => new { e.FirmaId, e.Yil, e.Ay }).IsUnique()
                .HasFilter("\"Durum\" = 0"); // Sadece Running durumunda UNIQUE → mutex

            entity.HasIndex(e => new { e.FirmaId, e.Yil, e.Ay });
            entity.HasIndex(e => e.Durum);

            entity.Property(e => e.Tetikleyen).HasMaxLength(50);
            entity.Property(e => e.HataMesaji).HasMaxLength(1000);
            entity.Property(e => e.Hesaplayan).HasMaxLength(50);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracMarka
        modelBuilder.Entity<AracMarka>(entity =>
        {
            entity.HasIndex(e => e.MarkaAdi).IsUnique();
            entity.Property(e => e.MarkaAdi).HasMaxLength(50);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracModelTanim
        modelBuilder.Entity<AracModelTanim>(entity =>
        {
            entity.Property(e => e.ModelAdi).HasMaxLength(50);
            entity.HasOne(e => e.Marka)
                .WithMany(m => m.Modeller)
                .HasForeignKey(e => e.MarkaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracEvrak
        modelBuilder.Entity<AracEvrak>(entity =>
        {
            entity.HasIndex(e => new { e.AracId, e.EvrakKategorisi });
            entity.Property(e => e.EvrakKategorisi).HasMaxLength(100);
            entity.Property(e => e.EvrakAdi).HasMaxLength(200);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.SigortaSirketi).HasMaxLength(100);
            entity.Property(e => e.PoliceNo).HasMaxLength(100);
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PersonelAracAtama
        modelBuilder.Entity<PersonelAracAtama>(entity =>
        {
            entity.ToTable("PersonelAracAtamalari");
            entity.HasIndex(e => new { e.SoforId, e.AracId, e.BaslangicTarihi });
            entity.Property(e => e.Notlar).HasMaxLength(500);
            entity.HasOne(e => e.Sofor)
                .WithMany(s => s.AracAtamalari)
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracEvrakDosya
        modelBuilder.Entity<AracEvrakDosya>(entity =>
        {
            entity.Property(e => e.DosyaAdi).HasMaxLength(255);
            entity.Property(e => e.DosyaYolu).HasMaxLength(500);
            entity.Property(e => e.DosyaTipi).HasMaxLength(20);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.HasOne(e => e.AracEvrak)
                .WithMany(e => e.Dosyalar)
                .HasForeignKey(e => e.AracEvrakId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ===== CRM MODULU KONFIGURASYONLARI =====

        // Bildirim
        modelBuilder.Entity<Bildirim>(entity =>
        {
            entity.HasIndex(e => new { e.KullaniciId, e.Okundu });
            entity.Property(e => e.Baslik).HasMaxLength(200);
            entity.Property(e => e.Icerik).HasMaxLength(1000);
            entity.Property(e => e.IliskiliTablo).HasMaxLength(50);
            entity.Property(e => e.Link).HasMaxLength(200);
            entity.HasOne(e => e.Kullanici)
                .WithMany(k => k.Bildirimler)
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Mesaj
        modelBuilder.Entity<Mesaj>(entity =>
        {
            entity.HasIndex(e => new { e.AliciId, e.Okundu });
            entity.HasIndex(e => e.GonderenId);
            entity.Property(e => e.Konu).HasMaxLength(200);
            entity.Property(e => e.DisAlici).HasMaxLength(100);
            entity.Property(e => e.DisGonderimId).HasMaxLength(100);
            entity.HasOne(e => e.Gonderen)
                .WithMany(k => k.GonderilenMesajlar)
                .HasForeignKey(e => e.GonderenId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Alici)
                .WithMany(k => k.AlinanMesajlar)
                .HasForeignKey(e => e.AliciId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.UstMesaj)
                .WithMany(m => m.Yanitlar)
                .HasForeignKey(e => e.UstMesajId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // EmailAyar
        modelBuilder.Entity<EmailAyar>(entity =>
        {
            entity.Property(e => e.SmtpSunucu).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Sifre).HasMaxLength(100);
            entity.Property(e => e.GonderenAdi).HasMaxLength(100);
            entity.Property(e => e.ImapSunucu).HasMaxLength(100);
            entity.Property(e => e.GelenKlasoru).HasMaxLength(100);
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // WhatsAppAyar
        modelBuilder.Entity<WhatsAppAyar>(entity =>
        {
            entity.Property(e => e.Telefon).HasMaxLength(20);
            entity.Property(e => e.ApiKey).HasMaxLength(500);
            entity.Property(e => e.WebhookUrl).HasMaxLength(200);
            entity.Property(e => e.HizliSablonlarJson).HasMaxLength(4000);
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Hatirlatici
        modelBuilder.Entity<Hatirlatici>(entity =>
        {
            entity.HasIndex(e => new { e.KullaniciId, e.BaslangicTarihi });
            entity.Property(e => e.Baslik).HasMaxLength(200);
            entity.Property(e => e.Aciklama).HasMaxLength(1000);
            entity.Property(e => e.IliskiliTablo).HasMaxLength(50);
            entity.Property(e => e.Renk).HasMaxLength(20);
            entity.HasOne(e => e.Kullanici)
                .WithMany(k => k.Hatirlaticilar)
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Cari)
                .WithMany(c => c.Hatirlaticilar)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted
                && e.Kullanici != null
                && !e.Kullanici.IsDeleted
                && (e.Cari == null || !e.Cari.IsDeleted));
        });

        // WhatsApp Modelleri
        modelBuilder.Entity<WhatsAppKisi>(entity =>
        {
            entity.HasIndex(e => e.Telefon).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<WhatsAppGrupUye>(entity =>
        {
            entity.HasIndex(e => new { e.GrupId, e.KisiId }).IsUnique().HasFilter("\"IsDeleted\" = false");
            
            entity.HasOne(e => e.Grup)
                .WithMany(g => g.Uyeler)
                .HasForeignKey(e => e.GrupId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Kisi)
                .WithMany(k => k.Gruplari)
                .HasForeignKey(e => e.KisiId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<WhatsAppMesaj>(entity =>
        {
            entity.HasOne(e => e.Gonderen)
                .WithMany()
                .HasForeignKey(e => e.GonderenId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Kisi)
                .WithMany()
                .HasForeignKey(e => e.KisiId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Grup)
                .WithMany()
                .HasForeignKey(e => e.GrupId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ===== STOK/ENVANTER MODULU KONFIGURASYONLARI =====

        // StokKarti
        modelBuilder.Entity<StokKarti>(entity =>
        {
            entity.HasIndex(e => e.StokKodu).IsUnique();
            entity.Property(e => e.StokKodu).HasMaxLength(50);
            entity.Property(e => e.StokAdi).HasMaxLength(200);
            entity.Property(e => e.Barkod).HasMaxLength(50);
            entity.Property(e => e.Birim).HasMaxLength(20);
            entity.Property(e => e.AlisFiyati).HasPrecision(18, 2);
            entity.Property(e => e.SatisFiyati).HasPrecision(18, 2);
            entity.Property(e => e.KdvOrani).HasPrecision(5, 2);
            entity.Property(e => e.MinStokMiktari).HasPrecision(18, 4);
            entity.Property(e => e.MaksStokMiktari).HasPrecision(18, 4);
            entity.Property(e => e.MevcutStok).HasPrecision(18, 4);
            entity.HasOne(e => e.Kategori)
                .WithMany(k => k.StokKartlari)
                .HasForeignKey(e => e.KategoriId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.VarsayilanTedarikci)
                .WithMany()
                .HasForeignKey(e => e.VarsayilanTedarikciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.MuhasebeHesap)
                .WithMany()
                .HasForeignKey(e => e.MuhasebeHesapId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // StokKategori
        modelBuilder.Entity<StokKategori>(entity =>
        {
            entity.Property(e => e.KategoriAdi).HasMaxLength(100);
            entity.Property(e => e.Renk).HasMaxLength(20);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.HasOne(e => e.UstKategori)
                .WithMany(k => k.AltKategoriler)
                .HasForeignKey(e => e.UstKategoriId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // StokHareket
        modelBuilder.Entity<StokHareket>(entity =>
        {
            entity.HasIndex(e => new { e.StokKartiId, e.IslemTarihi });
            entity.Property(e => e.BelgeNo).HasMaxLength(50);
            entity.Property(e => e.Miktar).HasPrecision(18, 4);
            entity.Property(e => e.BirimFiyat).HasPrecision(18, 2);
            entity.HasOne(e => e.StokKarti)
                .WithMany(s => s.Hareketler)
                .HasForeignKey(e => e.StokKartiId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Fatura)
                .WithMany()
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.FaturaKalem)
                .WithMany()
                .HasForeignKey(e => e.FaturaKalemId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AracMasraf)
                .WithMany()
                .HasForeignKey(e => e.AracMasrafId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AracIslem (Araç Alış/Satış)
        modelBuilder.Entity<AracIslem>(entity =>
        {
            entity.HasIndex(e => new { e.AracId, e.IslemTarihi });
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.Property(e => e.KdvOrani).HasPrecision(5, 2);
            entity.Property(e => e.KdvTutar).HasPrecision(18, 2);
            entity.Property(e => e.ToplamTutar).HasPrecision(18, 2);
            entity.Property(e => e.NoterId).HasMaxLength(50);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Fatura)
                .WithMany()
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.StokHareket)
                .WithMany()
                .HasForeignKey(e => e.StokHareketId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ServisKaydi
        modelBuilder.Entity<ServisKaydi>(entity =>
        {
            entity.HasIndex(e => new { e.AracId, e.ServisTarihi });
            entity.Property(e => e.ServisAdi).HasMaxLength(200);
            entity.Property(e => e.IscilikTutari).HasPrecision(18, 2);
            entity.Property(e => e.ParcaTutari).HasPrecision(18, 2);
            entity.Property(e => e.KdvOrani).HasPrecision(5, 2);
            entity.Property(e => e.KdvTutar).HasPrecision(18, 2);
            entity.Property(e => e.ToplamTutar).HasPrecision(18, 2);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ServisciCari)
                .WithMany()
                .HasForeignKey(e => e.ServisciCariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Fatura)
                .WithMany()
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AracMasraf)
                .WithMany()
                .HasForeignKey(e => e.AracMasrafId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.StokHareket)
                .WithMany()
                .HasForeignKey(e => e.StokHareketId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ServisParca
        modelBuilder.Entity<ServisParca>(entity =>
        {
            entity.Property(e => e.ParcaAdi).HasMaxLength(200);
            entity.Property(e => e.Birim).HasMaxLength(20);
            entity.Property(e => e.Miktar).HasPrecision(18, 4);
            entity.Property(e => e.BirimFiyat).HasPrecision(18, 2);
            entity.HasOne(e => e.ServisKaydi)
                .WithMany(s => s.Parcalar)
                .HasForeignKey(e => e.ServisKaydiId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.StokKarti)
                .WithMany()
                .HasForeignKey(e => e.StokKartiId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<AylikOdemePlani>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Firma == null || !e.Firma.IsDeleted));

        modelBuilder.Entity<AylikOdemeGerceklesen>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Firma == null || !e.Firma.IsDeleted));

        modelBuilder.Entity<BordroDetay>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Personel.IsDeleted);

        modelBuilder.Entity<DashboardWidget>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Kullanici.IsDeleted);

        modelBuilder.Entity<FaturaSablon>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Firma == null || !e.Firma.IsDeleted));

        modelBuilder.Entity<FiloGuzergahEslestirme>(entity =>
        {
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.KurumFirma)
                .WithMany()
                .HasForeignKey(e => e.KurumFirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted
                && (e.Arac == null || !e.Arac.IsDeleted)
                && (e.Firma == null || !e.Firma.IsDeleted)
                && (e.KurumFirma == null || !e.KurumFirma.IsDeleted));
        });

        modelBuilder.Entity<FiloGunlukPuantaj>(entity =>
        {
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted && (e.Arac == null || !e.Arac.IsDeleted) && (e.KurumFirma == null || !e.KurumFirma.IsDeleted));
        });

        // FirmaAracSoforEslestirme - Kurum+Araç+Şoför kalıcı eşleştirme
        modelBuilder.Entity<FirmaAracSoforEslestirme>(entity =>
        {
            entity.HasIndex(e => new { e.FirmaId, e.KurumCariId, e.AracId, e.SoforId });
            entity.HasIndex(e => new { e.KurumCariId, e.Aktif });

            entity.HasOne(e => e.KurumCari)
                .WithMany()
                .HasForeignKey(e => e.KurumCariId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Sofor)
                .WithMany()
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // FirmaGuzergahEslestirme - Kurum+Güzergah kalıcı eşleştirme
        modelBuilder.Entity<FirmaGuzergahEslestirme>(entity =>
        {
            entity.HasIndex(e => new { e.FirmaId, e.KurumCariId, e.GuzergahId });
            entity.HasIndex(e => new { e.KurumCariId, e.Aktif });

            entity.HasOne(e => e.KurumCari)
                .WithMany()
                .HasForeignKey(e => e.KurumCariId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Guzergah)
                .WithMany()
                .HasForeignKey(e => e.GuzergahId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Hakediş + Hakediş Detay + Araç Maliyet Snapshot (yeni modül)
        modelBuilder.Entity<Hakedis>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<Hakedis>()
            .HasOne(h => h.Fatura)
            .WithMany()
            .HasForeignKey(h => h.FaturaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HakedisDetay>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<HakedisDetay>()
            .HasOne(d => d.Hakedis)
            .WithMany(h => h.Detaylar)
            .HasForeignKey(d => d.HakedisId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HakedisDetay>()
            .HasOne(d => d.Arac)
            .WithMany()
            .HasForeignKey(d => d.AracId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HakedisDetay>()
            .HasOne(d => d.FiloGunlukPuantaj)
            .WithMany()
            .HasForeignKey(d => d.FiloGunlukPuantajId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AracMaliyetSnapshot>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Arac == null || !e.Arac.IsDeleted));

        modelBuilder.Entity<AracMaliyetSnapshot>()
            .HasOne(s => s.Arac)
            .WithMany()
            .HasForeignKey(s => s.AracId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AracMaliyetSnapshot>()
            .HasIndex(s => new { s.AracId, s.Yil, s.Ay })
            .IsUnique()
            .HasDatabaseName("IX_AracMaliyetSnapshot_Arac_Donem");

        modelBuilder.Entity<Hakedis>()
            .HasIndex(h => new { h.Tip, h.ReferansId, h.Yil, h.Ay })
            .HasDatabaseName("IX_Hakedis_Tip_Ref_Donem");

        modelBuilder.Entity<KiralamaArac>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Firma == null || !e.Firma.IsDeleted));

        modelBuilder.Entity<KullaniciCari>(entity =>
        {
            entity.HasOne(e => e.Kullanici)
                .WithMany(k => k.BagliCariler)
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Cari)
                .WithMany(c => c.KullaniciEslestirmeleri)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted
                && e.Cari != null
                && !e.Cari.IsDeleted
                && e.Kullanici != null
                && !e.Kullanici.IsDeleted);
        });

        modelBuilder.Entity<CariIletisimNot>(entity =>
        {
            entity.HasOne(e => e.Cari)
                .WithMany(c => c.IletisimNotlari)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted && !e.Cari.IsDeleted);
        });

        modelBuilder.Entity<CariHatirlatma>(entity =>
        {
            entity.HasIndex(e => new { e.CariId, e.Tip, e.CreatedAt });
            entity.Property(e => e.Baslik).HasMaxLength(200);
            entity.Property(e => e.Aciklama).HasMaxLength(2000);
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.HasOne(e => e.Cari)
                .WithMany(c => c.CariHatirlatmalar)
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Fatura)
                .WithMany()
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted && !e.Cari.IsDeleted);
        });

        modelBuilder.Entity<PersonelAvans>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Personel.IsDeleted);

        modelBuilder.Entity<PersonelBorc>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Personel.IsDeleted);

        modelBuilder.Entity<PersonelOzlukEvrak>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Sofor.IsDeleted);

        modelBuilder.Entity<PersonelPuantaj>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Firma == null || !e.Firma.IsDeleted));

        modelBuilder.Entity<ServisCalismaKiralama>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Firma == null || !e.Firma.IsDeleted));

        modelBuilder.Entity<BordroOdeme>()
            .HasQueryFilter(e => !e.IsDeleted && !e.BordroDetay.IsDeleted);

        modelBuilder.Entity<GunlukPuantaj>()
            .HasQueryFilter(e => !e.IsDeleted && (e.PersonelPuantaj == null || !e.PersonelPuantaj.IsDeleted));

        modelBuilder.Entity<PersonelAvansMahsup>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Avans.IsDeleted);

        modelBuilder.Entity<PersonelBorcOdeme>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Borc.IsDeleted);

        modelBuilder.Entity<AracBolgeAtama>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Arac.IsDeleted);

        modelBuilder.Entity<BakimPeriyot>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Arac == null || !e.Arac.IsDeleted));

        modelBuilder.Entity<AracBakimUyari>()
            .HasQueryFilter(e => !e.IsDeleted && (e.Arac == null || !e.Arac.IsDeleted));

        modelBuilder.Entity<AracTakipCihaz>()
            .HasQueryFilter(e => !e.IsDeleted && !e.Arac.IsDeleted);

        modelBuilder.Entity<AracKonum>()
            .HasQueryFilter(e => !e.IsDeleted && !e.AracTakipCihaz.IsDeleted);

        modelBuilder.Entity<AracTakipAlarm>()
            .HasQueryFilter(e => !e.IsDeleted && !e.AracTakipCihaz.IsDeleted);

        modelBuilder.Entity<AracEvrakDosyaVersiyon>()
            .HasQueryFilter(e => !e.IsDeleted && e.AracEvrakDosya != null && !e.AracEvrakDosya.IsDeleted);

        modelBuilder.Entity<BildirimAyar>()
            .HasQueryFilter(e => !e.IsDeleted && e.Kullanici != null && !e.Kullanici.IsDeleted);

        modelBuilder.Entity<EbysAramaGecmisi>()
            .HasQueryFilter(e => !e.IsDeleted && e.Kullanici != null && !e.Kullanici.IsDeleted);

        modelBuilder.Entity<EbysEvrakDosyaVersiyon>()
            .HasQueryFilter(e => !e.IsDeleted && e.EvrakDosya != null && !e.EvrakDosya.IsDeleted);

        modelBuilder.Entity<EbysKayitliArama>()
            .HasQueryFilter(e => !e.IsDeleted && e.Kullanici != null && !e.Kullanici.IsDeleted);

        modelBuilder.Entity<EpostaBildirimLog>()
            .HasQueryFilter(e => !e.IsDeleted && e.Kullanici != null && !e.Kullanici.IsDeleted);

        modelBuilder.Entity<PersonelOzlukEvrakVersiyon>()
            .HasQueryFilter(e => !e.IsDeleted && e.PersonelOzlukEvrak != null && !e.PersonelOzlukEvrak.IsDeleted);

        // Proforma Fatura
        modelBuilder.Entity<ProformaFatura>(entity =>
        {
            entity.HasIndex(e => e.ProformaNo).IsUnique();
            entity.Property(e => e.ProformaNo).HasMaxLength(50);
            entity.Property(e => e.AraToplam).HasPrecision(18, 2);
            entity.Property(e => e.IskontoTutar).HasPrecision(18, 2);
            entity.Property(e => e.IskontoOrani).HasPrecision(5, 2);
            entity.Property(e => e.KdvOrani).HasPrecision(5, 2);
            entity.Property(e => e.KdvTutar).HasPrecision(18, 2);
            entity.Property(e => e.GenelToplam).HasPrecision(18, 2);
            entity.Property(e => e.OdemeKosulu).HasMaxLength(100);
            entity.Property(e => e.TeslimKosulu).HasMaxLength(100);
            entity.Property(e => e.IlgiliKisi).HasMaxLength(100);
            entity.Property(e => e.Telefon).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Fatura)
                .WithMany()
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<ProformaFaturaKalem>(entity =>
        {
            entity.Property(e => e.UrunAdi).HasMaxLength(250);
            entity.Property(e => e.UrunKodu).HasMaxLength(50);
            entity.Property(e => e.Birim).HasMaxLength(20);
            entity.Property(e => e.Miktar).HasPrecision(18, 4);
            entity.Property(e => e.BirimFiyat).HasPrecision(18, 2);
            entity.Property(e => e.IskontoOrani).HasPrecision(5, 2);
            entity.Property(e => e.IskontoTutar).HasPrecision(18, 2);
            entity.Property(e => e.KdvOrani).HasPrecision(5, 2);
            entity.Property(e => e.KdvTutar).HasPrecision(18, 2);
            entity.Property(e => e.AraToplam).HasPrecision(18, 2);
            entity.Property(e => e.NetTutar).HasPrecision(18, 2);
            entity.Property(e => e.ToplamTutar).HasPrecision(18, 2);
            entity.HasOne(e => e.ProformaFatura)
                .WithMany(p => p.Kalemler)
                .HasForeignKey(e => e.ProformaFaturaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.StokKarti)
                .WithMany()
                .HasForeignKey(e => e.StokKartiId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // İhale Teklif Versiyonları
        modelBuilder.Entity<IhaleSozlesmeRevizyon>(entity =>
        {
            if (isSqlite)
            {
                entity.HasIndex(e => new { e.IhaleProjeId, e.RevizyonNo }).IsUnique();
            }
            else
            {
                entity.HasIndex(e => new { e.IhaleProjeId, e.RevizyonNo })
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
            }

            entity.Property(e => e.RevizyonNo).HasMaxLength(50);
            entity.Property(e => e.Baslik).HasMaxLength(250);
            entity.Property(e => e.Aciklama).HasMaxLength(2000);
            entity.Property(e => e.BedelFarki).HasPrecision(18, 2);

            entity.HasOne(e => e.IhaleProje)
                .WithMany(p => p.SozlesmeRevizyonlari)
                .HasForeignKey(e => e.IhaleProjeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<IhaleTeklifVersiyon>(entity =>
        {
            if (isSqlite)
            {
                entity.HasIndex(e => new { e.IhaleProjeId, e.VersiyonNo }).IsUnique();
            }
            else
            {
                entity.HasIndex(e => new { e.IhaleProjeId, e.VersiyonNo })
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");

                entity.HasIndex(e => e.IhaleProjeId)
                    .HasDatabaseName("IX_IhaleTeklifVersiyonlari_AktifVersiyon")
                    .IsUnique()
                    .HasFilter("\"AktifVersiyon\" = true AND \"IsDeleted\" = false");
            }

            entity.Property(e => e.RevizyonKodu).HasMaxLength(50);
            entity.Property(e => e.RevizyonNotu).HasMaxLength(2000);
            entity.Property(e => e.KararNotu).HasMaxLength(2000);
            entity.Property(e => e.ToplamMaliyet).HasPrecision(18, 2);
            entity.Property(e => e.TeklifTutari).HasPrecision(18, 2);
            entity.Property(e => e.KarMarjiTutari).HasPrecision(18, 2);
            entity.Property(e => e.KarMarjiOrani).HasPrecision(5, 2);

            entity.HasOne(e => e.IhaleProje)
                .WithMany(p => p.TeklifVersiyonlari)
                .HasForeignKey(e => e.IhaleProjeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.HazirlayanKullanici)
                .WithMany()
                .HasForeignKey(e => e.HazirlayanKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.OnaylayanKullanici)
                .WithMany()
                .HasForeignKey(e => e.OnaylayanKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<IhaleTeklifKararLog>(entity =>
        {
            entity.HasIndex(e => new { e.IhaleTeklifVersiyonId, e.IslemTarihi });
            entity.Property(e => e.Not).HasMaxLength(2000);

            entity.HasOne(e => e.IhaleTeklifVersiyon)
                .WithMany(v => v.KararLoglari)
                .HasForeignKey(e => e.IhaleTeklifVersiyonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.IslemYapanKullanici)
                .WithMany()
                .HasForeignKey(e => e.IslemYapanKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<IhaleRakipBenchmark>(entity =>
        {
            entity.HasIndex(e => e.IhaleProjeId);
            entity.Property(e => e.RakipFirmaAdi).HasMaxLength(200);

            entity.HasOne(e => e.IhaleProje)
                .WithMany(p => p.RakipBenchmarklar)
                .HasForeignKey(e => e.IhaleProjeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ===== DESTEK TALEBİ (TICKET) MODÜLÜ KONFIGURASYONLARI - osTicket benzeri =====

        // DestekTalebi - Ana ticket tablosu
        modelBuilder.Entity<DestekTalebi>(entity =>
        {
            entity.HasIndex(e => e.TalepNo).IsUnique();
            entity.HasIndex(e => new { e.DepartmanId, e.Durum });
            entity.HasIndex(e => new { e.AtananKullaniciId, e.Durum });
            entity.HasIndex(e => e.SonAktiviteTarihi);
            entity.Property(e => e.TalepNo).HasMaxLength(50);
            entity.Property(e => e.Konu).HasMaxLength(500);
            entity.Property(e => e.MusteriAdi).HasMaxLength(200);
            entity.Property(e => e.MusteriEmail).HasMaxLength(200);
            entity.Property(e => e.MusteriTelefon).HasMaxLength(20);
            entity.Property(e => e.Etiketler).HasMaxLength(500);
            entity.HasOne(e => e.Departman)
                .WithMany(d => d.Talepler)
                .HasForeignKey(e => e.DepartmanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Kategori)
                .WithMany(k => k.Talepler)
                .HasForeignKey(e => e.KategoriId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AtananKullanici)
                .WithMany()
                .HasForeignKey(e => e.AtananKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.OlusturanKullanici)
                .WithMany()
                .HasForeignKey(e => e.OlusturanKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekTalebiYanit - Ticket yanıtları/konuşma
        modelBuilder.Entity<DestekTalebiYanit>(entity =>
        {
            entity.HasIndex(e => new { e.DestekTalebiId, e.CreatedAt });
            entity.Property(e => e.MusteriAdi).HasMaxLength(200);
            entity.HasOne(e => e.DestekTalebi)
                .WithMany(t => t.Yanitlar)
                .HasForeignKey(e => e.DestekTalebiId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekTalebiEk - Dosya ekleri
        modelBuilder.Entity<DestekTalebiEk>(entity =>
        {
            entity.Property(e => e.DosyaAdi).HasMaxLength(255);
            entity.Property(e => e.OrijinalDosyaAdi).HasMaxLength(255);
            entity.Property(e => e.DosyaYolu).HasMaxLength(500);
            entity.Property(e => e.MimeTipi).HasMaxLength(100);
            entity.HasOne(e => e.DestekTalebi)
                .WithMany(t => t.Ekler)
                .HasForeignKey(e => e.DestekTalebiId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Yanit)
                .WithMany(y => y.Ekler)
                .HasForeignKey(e => e.YanitId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.YukleyenKullanici)
                .WithMany()
                .HasForeignKey(e => e.YukleyenKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekTalebiAktivite - Aktivite/Tarihçe kaydı
        modelBuilder.Entity<DestekTalebiAktivite>(entity =>
        {
            entity.HasIndex(e => new { e.DestekTalebiId, e.CreatedAt });
            entity.Property(e => e.Aciklama).HasMaxLength(1000);
            entity.Property(e => e.EskiDeger).HasMaxLength(500);
            entity.Property(e => e.YeniDeger).HasMaxLength(500);
            entity.HasOne(e => e.DestekTalebi)
                .WithMany(t => t.Aktiviteler)
                .HasForeignKey(e => e.DestekTalebiId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekTalebiIliski - Bağlı ticketlar
        modelBuilder.Entity<DestekTalebiIliski>(entity =>
        {
            entity.HasIndex(e => new { e.AnaTalepId, e.IliskiliTalepId }).IsUnique();
            entity.HasOne(e => e.AnaTalep)
                .WithMany(t => t.IliskiliTalepler)
                .HasForeignKey(e => e.AnaTalepId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.IliskiliTalep)
                .WithMany()
                .HasForeignKey(e => e.IliskiliTalepId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekDepartman - Departmanlar
        modelBuilder.Entity<DestekDepartman>(entity =>
        {
            entity.HasIndex(e => e.Ad);
            entity.Property(e => e.Ad).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.HasOne(e => e.UstDepartman)
                .WithMany(d => d.AltDepartmanlar)
                .HasForeignKey(e => e.UstDepartmanId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekDepartmanUye - Departman üyeleri
        modelBuilder.Entity<DestekDepartmanUye>(entity =>
        {
            entity.HasIndex(e => new { e.DepartmanId, e.KullaniciId }).IsUnique();
            entity.HasOne(e => e.Departman)
                .WithMany(d => d.Uyeler)
                .HasForeignKey(e => e.DepartmanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekKategori - Kategoriler
        modelBuilder.Entity<DestekKategori>(entity =>
        {
            entity.HasIndex(e => e.Ad);
            entity.Property(e => e.Ad).HasMaxLength(100);
            entity.Property(e => e.Renk).HasMaxLength(20);
            entity.Property(e => e.Simge).HasMaxLength(50);
            entity.HasOne(e => e.Departman)
                .WithMany(d => d.Kategoriler)
                .HasForeignKey(e => e.DepartmanId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.UstKategori)
                .WithMany(k => k.AltKategoriler)
                .HasForeignKey(e => e.UstKategoriId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekHazirYanit - Hazır yanıt şablonları
        modelBuilder.Entity<DestekHazirYanit>(entity =>
        {
            entity.Property(e => e.Ad).HasMaxLength(200);
            entity.Property(e => e.KonuSablonu).HasMaxLength(500);
            entity.HasOne(e => e.Departman)
                .WithMany()
                .HasForeignKey(e => e.DepartmanId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Kategori)
                .WithMany()
                .HasForeignKey(e => e.KategoriId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekBilgiBankasi - Bilgi bankası makaleleri
        modelBuilder.Entity<DestekBilgiBankasi>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Durum);
            entity.Property(e => e.Baslik).HasMaxLength(500);
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.SeoBaslik).HasMaxLength(200);
            entity.Property(e => e.Etiketler).HasMaxLength(500);
            entity.HasOne(e => e.Kategori)
                .WithMany()
                .HasForeignKey(e => e.KategoriId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Yazar)
                .WithMany()
                .HasForeignKey(e => e.YazarKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekSla - SLA tanımları
        modelBuilder.Entity<DestekSla>(entity =>
        {
            entity.HasIndex(e => e.Oncelik);
            entity.Property(e => e.Ad).HasMaxLength(100);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DestekAyar - Sistem ayarları
        modelBuilder.Entity<DestekAyar>(entity =>
        {
            entity.HasIndex(e => e.Anahtar).IsUnique();
            entity.Property(e => e.Anahtar).HasMaxLength(100);
            entity.Property(e => e.Deger).HasMaxLength(2000);
            entity.Property(e => e.Grup).HasMaxLength(50);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ===== EBYS GELEN/GİDEN EVRAK MODÜLÜ =====

        // EbysEvrak - Gelen/Giden evrak ana tablosu
        modelBuilder.Entity<EbysEvrak>(entity =>
        {
            entity.HasIndex(e => e.EvrakNo);
            entity.HasIndex(e => new { e.Yon, e.Durum });
            entity.HasIndex(e => e.EvrakTarihi);
            entity.Property(e => e.EvrakNo).HasMaxLength(50);
            entity.Property(e => e.Konu).HasMaxLength(500);
            entity.Property(e => e.Ozet).HasMaxLength(2000);
            entity.Property(e => e.GonderenKurum).HasMaxLength(250);
            entity.Property(e => e.AliciKurum).HasMaxLength(250);
            entity.Property(e => e.GelisNo).HasMaxLength(50);
            entity.Property(e => e.GidisNo).HasMaxLength(50);
            entity.Property(e => e.Aciklama).HasMaxLength(2000);
            entity.Property(e => e.Notlar).HasMaxLength(4000);
            entity.HasOne(e => e.Kategori)
                .WithMany(k => k.Evraklar)
                .HasForeignKey(e => e.KategoriId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.UstEvrak)
                .WithMany(e => e.AltEvraklar)
                .HasForeignKey(e => e.UstEvrakId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AtananKullanici)
                .WithMany()
                .HasForeignKey(e => e.AtananKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // EbysEvrakKategori
        modelBuilder.Entity<EbysEvrakKategori>(entity =>
        {
            entity.HasIndex(e => e.KategoriAdi);
            entity.Property(e => e.KategoriAdi).HasMaxLength(100);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.Renk).HasMaxLength(20);
            entity.Property(e => e.Ikon).HasMaxLength(50);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // EbysEvrakDosya
        modelBuilder.Entity<EbysEvrakDosya>(entity =>
        {
            entity.Property(e => e.DosyaAdi).HasMaxLength(255);
            entity.Property(e => e.DosyaYolu).HasMaxLength(500);
            entity.Property(e => e.DosyaTipi).HasMaxLength(20);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.HasOne(e => e.Evrak)
                .WithMany(e => e.Dosyalar)
                .HasForeignKey(e => e.EvrakId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // EbysEvrakAtama
        modelBuilder.Entity<EbysEvrakAtama>(entity =>
        {
            entity.HasIndex(e => new { e.EvrakId, e.Durum });
            entity.Property(e => e.Talimat).HasMaxLength(2000);
            entity.Property(e => e.Sonuc).HasMaxLength(2000);
            entity.HasOne(e => e.Evrak)
                .WithMany(e => e.Atamalar)
                .HasForeignKey(e => e.EvrakId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AtananKullanici)
                .WithMany()
                .HasForeignKey(e => e.AtananKullaniciId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AtayanKullanici)
                .WithMany()
                .HasForeignKey(e => e.AtayanKullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // EbysEvrakHareket
        modelBuilder.Entity<EbysEvrakHareket>(entity =>
        {
            entity.HasIndex(e => new { e.EvrakId, e.IslemTarihi });
            entity.Property(e => e.Aciklama).HasMaxLength(1000);
            entity.Property(e => e.EskiDeger).HasMaxLength(500);
            entity.Property(e => e.YeniDeger).HasMaxLength(500);
            entity.HasOne(e => e.Evrak)
                .WithMany(e => e.Hareketler)
                .HasForeignKey(e => e.EvrakId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Kullanici)
                .WithMany()
                .HasForeignKey(e => e.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Şirketler Arası Transfer Konfigürasyonu - Faz 5.3-B3-i: kaldırıldı

        // Lastik Takip Modülü
        modelBuilder.Entity<LastikDepo>(entity =>
        {
            entity.Property(e => e.DepoAdi).HasMaxLength(150);
            entity.Property(e => e.SorumluKisi).HasMaxLength(100);
            entity.Property(e => e.Telefon).HasMaxLength(20);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<LastikStok>(entity =>
        {
            entity.Property(e => e.Marka).HasMaxLength(100);
            entity.Property(e => e.Ebat).HasMaxLength(30);
            entity.Property(e => e.SeriNo).HasMaxLength(50);
            entity.HasOne(e => e.Depo)
                .WithMany(d => d.Stoklar)
                .HasForeignKey(e => e.DepoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<LastikDegisim>(entity =>
        {
            entity.Property(e => e.YapilanYer).HasMaxLength(150);
            entity.Property(e => e.SokulenPozisyon).HasMaxLength(50);
            entity.Property(e => e.TakilanPozisyon).HasMaxLength(50);
            entity.Property(e => e.Ucret).HasPrecision(18, 2);
            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SokulenStok)
                .WithMany()
                .HasForeignKey(e => e.SokulenStokId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.TakilanStok)
                .WithMany(s => s.Degisimler)
                .HasForeignKey(e => e.TakilanStokId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.HedefDepo)
                .WithMany()
                .HasForeignKey(e => e.HedefDepoId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.KaynakDepo)
                .WithMany()
                .HasForeignKey(e => e.KaynakDepoId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // TasimaTedarikci - Personel Taşıma Tedarikçisi (alt yüklenici)
        modelBuilder.Entity<TasimaTedarikci>(entity =>
        {
            entity.HasIndex(e => e.TedarikciKodu).IsUnique();
            entity.Property(e => e.TedarikciKodu).HasMaxLength(50);
            entity.Property(e => e.Unvan).HasMaxLength(250);
            entity.Property(e => e.YetkiliKisi).HasMaxLength(150);
            entity.Property(e => e.Telefon).HasMaxLength(20);
            entity.Property(e => e.Telefon2).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.VergiNo).HasMaxLength(20);
            entity.Property(e => e.VergiDairesi).HasMaxLength(100);
            entity.Property(e => e.SozlesmeNo).HasMaxLength(100);
            entity.Property(e => e.VarsayilanSeferUcreti).HasPrecision(18, 2);

            entity.HasOne(e => e.Cari)
                .WithMany()
                .HasForeignKey(e => e.CariId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // TasimaTedarikciIs - Tedarikçi-Güzergah-İş eşleşmesi
        modelBuilder.Entity<TasimaTedarikciIs>(entity =>
        {
            entity.HasIndex(e => new { e.TasimaTedarikciId, e.GuzergahId, e.BaslangicTarihi });
            entity.Property(e => e.SeferUcreti).HasPrecision(18, 2);
            entity.Property(e => e.AylikUcret).HasPrecision(18, 2);
            entity.Property(e => e.Aciklama).HasMaxLength(1000);

            entity.HasOne(e => e.TasimaTedarikci)
                .WithMany(t => t.Isler)
                .HasForeignKey(e => e.TasimaTedarikciId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Guzergah)
                .WithMany()
                .HasForeignKey(e => e.GuzergahId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Sofor)
                .WithMany()
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Sofor -> TasimaTedarikci (alt yüklenici personeli)
        modelBuilder.Entity<Sofor>()
            .HasOne(s => s.TasimaTedarikci)
            .WithMany(t => t.Personeller)
            .HasForeignKey(s => s.TasimaTedarikciId)
            .OnDelete(DeleteBehavior.SetNull);

        // Arac -> TasimaTedarikci (alt yüklenici aracı)
        modelBuilder.Entity<Arac>()
            .HasOne(a => a.TasimaTedarikci)
            .WithMany(t => t.Araclar)
            .HasForeignKey(a => a.TasimaTedarikciId)
            .OnDelete(DeleteBehavior.SetNull);

        // TedarikciEvrak - Tedarikçi firma evrakları
        modelBuilder.Entity<TedarikciEvrak>(entity =>
        {
            entity.Property(e => e.EvrakKategorisi).HasMaxLength(100);
            entity.Property(e => e.EvrakAdi).HasMaxLength(250);
            entity.Property(e => e.SigortaSirketi).HasMaxLength(200);
            entity.Property(e => e.PoliceNo).HasMaxLength(100);
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.TasimaTedarikciId, e.EvrakKategorisi });

            entity.HasOne(e => e.TasimaTedarikci)
                .WithMany(t => t.Evraklar)
                .HasForeignKey(e => e.TasimaTedarikciId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // TedarikciEvrakDosya - Tedarikçi evrak dosyaları
        modelBuilder.Entity<TedarikciEvrakDosya>(entity =>
        {
            entity.Property(e => e.DosyaAdi).HasMaxLength(500);
            entity.Property(e => e.DosyaYolu).HasMaxLength(1000);
            entity.Property(e => e.DosyaTipi).HasMaxLength(20);

            entity.HasOne(e => e.TedarikciEvrak)
                .WithMany(ev => ev.Dosyalar)
                .HasForeignKey(e => e.TedarikciEvrakId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ── ServisKontrat ──────────────────────────────────────────────────────
        modelBuilder.Entity<ServisKontrat>(entity =>
        {
            entity.HasIndex(e => e.KontratKodu).IsUnique();
            entity.Property(e => e.KontratKodu).HasMaxLength(50);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.Notlar).HasMaxLength(2000);
            entity.Property(e => e.TahsilatBirimFiyat).HasPrecision(18, 2);
            entity.Property(e => e.OdemeBirimFiyat).HasPrecision(18, 2);

            entity.HasOne(e => e.KurumCari)
                .WithMany()
                .HasForeignKey(e => e.KurumCariId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Guzergah)
                .WithMany()
                .HasForeignKey(e => e.GuzergahId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Arac)
                .WithMany()
                .HasForeignKey(e => e.AracId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Sofor)
                .WithMany()
                .HasForeignKey(e => e.SoforId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TasimaTedarikci)
                .WithMany()
                .HasForeignKey(e => e.TasimaTedarikciId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TasimaTedarikciIs)
                .WithMany()
                .HasForeignKey(e => e.TasimaTedarikciIsId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ── ServisPuantaj
        modelBuilder.Entity<ServisPuantaj>(entity =>
        {
            entity.HasIndex(e => new { e.ServisKontratId, e.Yil, e.Ay }).IsUnique();
            entity.Property(e => e.CalismaSayisi).HasPrecision(18, 2);
            entity.Property(e => e.TahsilatBirimFiyat).HasPrecision(18, 2);
            entity.Property(e => e.TahsilatToplam).HasPrecision(18, 2);
            entity.Property(e => e.OdemeBirimFiyat).HasPrecision(18, 2);
            entity.Property(e => e.OdemeToplam).HasPrecision(18, 2);
            entity.Property(e => e.OnayanKisi).HasMaxLength(150);
            entity.Property(e => e.Notlar).HasMaxLength(2000);

            entity.HasOne(e => e.ServisKontrat)
                .WithMany(k => k.Puantajlar)
                .HasForeignKey(e => e.ServisKontratId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ── ServisOdeme
        modelBuilder.Entity<ServisOdeme>(entity =>
        {
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.Property(e => e.BelgeNo).HasMaxLength(100);
            entity.Property(e => e.Aciklama).HasMaxLength(500);

            entity.HasOne(e => e.ServisPuantaj)
                .WithMany(p => p.Odemeler)
                .HasForeignKey(e => e.ServisPuantajId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ── ServisTahsilat
        modelBuilder.Entity<ServisTahsilat>(entity =>
        {
            entity.Property(e => e.Tutar).HasPrecision(18, 2);
            entity.Property(e => e.BelgeNo).HasMaxLength(100);
            entity.Property(e => e.Aciklama).HasMaxLength(500);

            entity.HasOne(e => e.ServisPuantaj)
                .WithMany(p => p.Tahsilatlar)
                .HasForeignKey(e => e.ServisPuantajId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ── HoldingVeri (Kural 13: Konsolide snapshot, Faz 5.3: FK eklendi)
        modelBuilder.Entity<HoldingVeri>(entity =>
        {
            entity.HasIndex(e => new { e.FirmaId, e.Yil, e.Ay, e.Kategori }).IsUnique();
            entity.Property(e => e.FirmaKodu).HasMaxLength(50);
            entity.Property(e => e.FirmaAdi).HasMaxLength(250);
            entity.Property(e => e.Kategori).HasMaxLength(50);
            entity.Property(e => e.JsonDetay).HasColumnType("text");

            entity.HasOne<Firma>()
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── HoldingRapor (Kural 13: Kayıtlı raporlar)
        modelBuilder.Entity<HoldingRapor>(entity =>
        {
            entity.Property(e => e.Ad).HasMaxLength(250);
            entity.Property(e => e.Tip).HasMaxLength(50);
            entity.Property(e => e.OlusturanKullanici).HasMaxLength(100);
            entity.Property(e => e.JsonFiltreler).HasColumnType("text");
            entity.Property(e => e.JsonSonuc).HasColumnType("text");
        });

        // ── LucaPortalSettings (e-Fatura / e-Arsiv entegrasyon ayarlari) ──
        modelBuilder.Entity<LucaPortalSettings>(entity =>
        {
            entity.Property(e => e.KullaniciAdi).HasMaxLength(100);
            entity.Property(e => e.Sifre).HasMaxLength(100);
            entity.Property(e => e.PortalUrl).HasMaxLength(500);
            entity.Property(e => e.AccessToken).HasMaxLength(500);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
            entity.Property(e => e.LucaFirmaKodu).HasMaxLength(50);
            entity.HasIndex(e => e.FirmaId).IsUnique();
            entity.HasOne<Firma>()
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ----------------------------------------------------------------
        // GLOBAL TENANT FILTER (IFirmaTenant)
        // ----------------------------------------------------------------
        // Karar K3 + K4: FirmaId taşıyan tüm entity'lere otomatik firma filtresi.
        // EF Core 10 named query filter API'si kullanılır; bu sayede mevcut soft-delete
        // (IsDeleted) filtreleri ezilmez, ikinci bir bağımsız filter olarak eklenir.
        // K7 muafiyeti: [TenantFilterIgnore] (Bütçe, Muhasebe vs.)
        ApplyFirmaTenantQueryFilter(modelBuilder);
    }

    private void ApplyFirmaTenantQueryFilter(ModelBuilder modelBuilder)
    {
        var tenantInterface = typeof(IFirmaTenant);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType == null) continue;
            if (!tenantInterface.IsAssignableFrom(entityType.ClrType)) continue;
            if (Attribute.IsDefined(entityType.ClrType, typeof(TenantFilterIgnoreAttribute))) continue;

            var builder = modelBuilder.Entity(entityType.ClrType);

            // Nihai mimari (Kural 4): Tüm IFirmaTenant entity'lerinde FirmaId NOT NULL.
            // [TenantNullableFirmaId] attribute'u tüm entity'lerden kaldırıldı.
            builder.Property("FirmaId").IsRequired();

            // EF Core 10: Aynı entity'de hem anonymous hem named filter olamaz.
            // IFirmaTenant entity'leri için OnModelCreating içinde önceden tanımlanmış
            // anonymous filter (örn. !IsDeleted) varsa onu "SoftDelete" adıyla taşı, sonra
            // "Tenant" named filter ekle.
#pragma warning disable CS0618
            var existingAnonymous = entityType.GetQueryFilter();
#pragma warning restore CS0618
            if (existingAnonymous != null)
            {
                // Metadata seviyesinde anonymous filter'ı temizle, sonra "SoftDelete" adıyla geri ekle.
                entityType.SetQueryFilter((System.Linq.Expressions.LambdaExpression?)null);
                builder.HasQueryFilter("SoftDelete", existingAnonymous);
            }

            // e => FirmaTenantDisabled || EF.Property<int?>(e, "FirmaId") == FirmaTenantId
            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var firmaIdProp = System.Linq.Expressions.Expression.Property(parameter, "FirmaId");
            // Entities with non-nullable int FirmaId need conversion for comparison with int? (Kural 4)
            System.Linq.Expressions.Expression firmaIdPropAsNullable = firmaIdProp.Type == typeof(int)
                ? System.Linq.Expressions.Expression.Convert(firmaIdProp, typeof(int?))
                : firmaIdProp;
            var contextConst = System.Linq.Expressions.Expression.Constant(this);
            var disabledProp = System.Linq.Expressions.Expression.Property(
                contextConst,
                typeof(ApplicationDbContext).GetProperty("FirmaTenantDisabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!);
            var tenantIdProp = System.Linq.Expressions.Expression.Property(
                contextConst,
                typeof(ApplicationDbContext).GetProperty("FirmaTenantId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!);
            var equal = System.Linq.Expressions.Expression.Equal(firmaIdPropAsNullable, tenantIdProp);
            var body = System.Linq.Expressions.Expression.OrElse(disabledProp, equal);
            var lambda = System.Linq.Expressions.Expression.Lambda(body, parameter);

            builder.HasQueryFilter("Tenant", lambda);
        }
    }

    public override int SaveChanges()
    {
        ConvertDatesToUtc();
        UpdateTimestamps();
        AssignFirmaTenantId();
        SafeGenerateAuditLogs();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ConvertDatesToUtc();
        UpdateTimestamps();
        AssignFirmaTenantId();
        SafeGenerateAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Yeni eklenen <see cref="IFirmaTenant"/> entity'lerine, henüz FirmaId set edilmemişse
    /// aktif firma id'sini otomatik atar. Karar K3 + K4 gereği servis katmanında elle
    /// FirmaId ataması yapılmasına gerek kalmaz.
    /// </summary>
    private void AssignFirmaTenantId()
    {
        var aktif = ResolveAktifFirmaProvider();
        var firmaId = aktif?.AktifFirmaId ?? 0;

        // Startup / background scope'da IAktifFirmaProvider olmayabilir.
        // Bu durumda varsayılan firma (Id=1) kullanılır — throw etmek startup'ı kırar.
        if (firmaId == 0)
            firmaId = 1; // safe default: ilk firma

        foreach (var entry in ChangeTracker.Entries<IFirmaTenant>())
        {
            if (entry.State != EntityState.Added) continue;
            if (entry.Entity.FirmaId.HasValue && entry.Entity.FirmaId.Value > 0) continue;

            entry.Entity.FirmaId = firmaId;
        }
    }

    private bool? _auditLogTableExists;

    /// <summary>
    /// AktiviteLoglar tablosu henüz oluşmamışsa (pending migration) audit log kaydını sessizce atlar.
    /// Startup seed sırasında tablo yoksa uygulama çökmeye devam eder.
    /// </summary>
    private void SafeGenerateAuditLogs()
    {
        if (_auditLogTableExists == null)
        {
            try
            {
                // Tablo var mı kontrol et (lightweight: LIMIT 0)
                Database.ExecuteSqlRaw("SELECT 1 FROM \"AktiviteLoglar\" LIMIT 0");
                _auditLogTableExists = true;
            }
            catch (PostgresException)
            {
                _auditLogTableExists = false;
            }
        }

        if (_auditLogTableExists == true)
            GenerateAuditLogs();
    }

    private void GenerateAuditLogs()
    {
        var modifiedEntities = ChangeTracker.Entries()
            .Where(x => (x.State == EntityState.Added || x.State == EntityState.Modified || x.State == EntityState.Deleted) && !(x.Entity is AktiviteLog))
            .ToList();

        foreach (var entry in modifiedEntities)
        {
            var entityType = entry.Entity.GetType();
            // Skip logging for system/identity entities if needed, e.g.
            if (entityType.Name.Contains("AktiviteLog") || entityType.Name.Contains("Log")) continue;

            // Kural 14: FirmaId'yi entity'den veya aktif firmadan al
            int? firmaId = null;
            if (entry.Entity is IFirmaTenant tenantEntity)
                firmaId = tenantEntity.FirmaId;
            if (firmaId == null || firmaId == 0)
                firmaId = ResolveAktifFirmaProvider()?.AktifFirmaId;

            var log = new AktiviteLog
            {
                IslemZamani = DateTime.UtcNow,
                IslemTipi = entry.State.ToString(),
                EntityTipi = entityType.Name,
                Modul = "Genel", // Default, could be mapped based on type
                FirmaId = firmaId,
                KullaniciId = null // UI katmanindan set edilebilir
            };

            try
            {
                var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (idProperty != null && idProperty.CurrentValue != null && entry.State != EntityState.Added)
                {
                    log.EntityId = (int?)Convert.ChangeType(idProperty.CurrentValue, typeof(int));
                }

                if (entry.State == EntityState.Modified)
                {
                    var originalValues = new System.Collections.Generic.Dictionary<string, object?>();
                    var currentValues = new System.Collections.Generic.Dictionary<string, object?>();

                    foreach (var property in entry.Properties)
                    {
                        if (property.IsModified)
                        {
                            originalValues[property.Metadata.Name] = property.OriginalValue;
                            currentValues[property.Metadata.Name] = property.CurrentValue;
                        }
                    }

                    log.EskiDeger = System.Text.Json.JsonSerializer.Serialize(originalValues);
                    log.YeniDeger = System.Text.Json.JsonSerializer.Serialize(currentValues);
                    log.Aciklama = $"{entityType.Name} kaydı güncellendi.";
                }
                else if (entry.State == EntityState.Added)
                {
                    log.Aciklama = $"{entityType.Name} kaydı eklendi.";
                    // We don't have the ID yet for Added entities, it will be generated after SaveChanges.
                    // A proper implementation would run a second pass after SaveChanges.
                }
                else if (entry.State == EntityState.Deleted)
                {
                    log.Aciklama = $"{entityType.Name} kaydı silindi.";
                }

                AktiviteLoglar.Add(log);
            }
            catch { /* Ignore logging errors */ }
        }
    }

    // SirketTransferLog Entity Configuration - Faz 5.3-B3-i: kaldırıldı, entity dosyası silinecek

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    private void ConvertDatesToUtc()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dateTime)
                {
                    if (dateTime.Kind != DateTimeKind.Utc)
                    {
                        property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                    }
                }
                else if (property.CurrentValue is DateTime?)
                {
                    var nullableDateTime = (DateTime?)property.CurrentValue;
                    if (nullableDateTime.HasValue && nullableDateTime.Value.Kind != DateTimeKind.Utc)
                    {
                        property.CurrentValue = DateTime.SpecifyKind(nullableDateTime.Value, DateTimeKind.Utc);
                    }
                }
            }
        }
    }
}



