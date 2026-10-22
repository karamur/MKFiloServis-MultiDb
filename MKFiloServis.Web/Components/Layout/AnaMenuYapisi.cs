using MKFiloServis.Shared.Entities;
using static MKFiloServis.Web.Components.Layout.MenuKurali;

namespace MKFiloServis.Web.Components.Layout;

/// <summary>
/// Ana menünün tek tanımı. Kurallar:
/// <list type="bullet">
/// <item>Her sayfa menüde bir kez geçer; aynı sayfanın birden çok adresi <see cref="MenuOgesi.EkYollar"/> ile bağlanır.</item>
/// <item>Bir öğenin görünürlüğü = üst grup kuralı VE alt grup kuralı VE kendi kuralı. Birleştirilen eski girişlerde
/// kural, eski yolların VEYA'sıdır; böylece hiçbir kullanıcı eskiden gördüğü bağlantıyı kaybetmez.</item>
/// <item>Görünür öğesi olmayan grup/alt grup çizilmez.</item>
/// <item>Her bağlantının bir sayfası olmalıdır (<c>AnaMenuYapisiTests</c> denetler).</item>
/// </list>
/// </summary>
public static class AnaMenuYapisi
{
    public static IReadOnlyList<IMenuDugumu> Kokler { get; } = Olustur();

    private static IReadOnlyList<IMenuDugumu> Olustur() =>
    [
        Oge("Belge Uyarıları", "/belge-uyarilari", "bi-exclamation-triangle-fill", "text-warning", Yetki("belgeuyari.oku")),
        Oge("Ana Sayfa", "/dashboard", "bi-house-door-fill"),

        // ── CARİ ────────────────────────────────────────────────────────────────
        Grup("cari", "CARİ YÖNETİMİ", "bi-people-fill", kural: Menu("menu.cari"),
            altlar:
            [
                Alt(null, null,
                [
                    Oge("Cari Listesi", "/cariler", "bi-person-lines-fill", "text-primary", Yetki("cariler.oku")),
                    Oge("Borç/Alacak Takip", "/cari-hareket-takip", "bi-cash-stack", "text-success", Yetki("cariler.oku")),
                    Oge("Risk Analizi", "/cari/risk-analizi", "bi-shield-exclamation", "text-danger", Yetki("cariler.oku")),
                ]),
            ]),

        // ── FATURALAR (eskiden Cari Yönetimi + Fatura İşlemleri) ──────────────────
        Grup("fatura", "FATURALAR", "bi-receipt",
            altlar:
            [
                Alt("Listeler", "bi-list-ul",
                [
                    Oge("Tüm Faturalar", "/faturalar", "bi-file-earmark-text-fill", "text-primary",
                        Ve(Menu("menu.fatura"), Yetki("faturalar.oku"))),
                    Oge("Kesilen Faturalar (Satış)", "/efatura/giden", "bi-file-earmark-arrow-up", "text-success",
                        Ve(Menu("menu.fatura"), Yetki("kesilenfatura.oku"))),
                    Oge("Gelen Faturalar (Alış)", "/efatura/gelen", "bi-file-earmark-arrow-down", "text-danger",
                        Ve(Menu("menu.fatura"), Yetki("gelenfatura.oku"))),
                ], acik: true),
                Alt("Hazırlık", "bi-pencil-square",
                [
                    Oge("Proforma Fatura", "/proforma-faturalar", "bi-file-earmark-text", "text-info",
                        Ve(Menu("menu.fatura"), Yetki("kesilenfatura.oku")), ekYollar: ["/proforma-fatura"]),
                    Oge("Toplu Fatura", "/faturalar/toplu-fatura", "bi-files", "text-success",
                        Ve(Menu("menu.fatura"), Yetki("kesilenfatura.oku"))),
                ]),
                Alt("E-Fatura ve Entegrasyon", "bi-cloud-arrow-up",
                [
                    Oge("E-Fatura XML", "/faturalar/efatura-xml", "bi-file-earmark-code", "text-warning",
                        Ve(Menu("menu.fatura"), Yetki("kesilenfatura.oku"))),
                    Oge("Luca Portal (Belgeler)", "/faturalar/luca-portal", "bi-cloud-download", "text-info",
                        Ve(Menu("menu.ayarlar"), Yetki("ayarlar.oku"))),
                    Oge("Stok Türü Eşleştir", "/efatura/stok-turu-eslestir", "bi-tags", "text-info",
                        Ve(Menu("menu.fatura"), Herhangi("gelenfatura.oku", "kesilenfatura.oku"))),
                ]),
            ]),

        // ── FİNANS (eskiden Bütçe + Banka/Kasa + Muhasebe) ───────────────────────
        Grup("finans", "FİNANS", "bi-cash-coin",
            altlar:
            [
                Alt("Banka / Kasa", "bi-bank",
                [
                    Oge("Banka Hesapları", "/banka-hesaplari", "bi-bank2", null, Yetki("bankahesap.oku")),
                    Oge("Hareketler", "/banka-hareketleri", "bi-arrow-left-right", null, Yetki("bankahareket.oku")),
                    Oge("Gelir/Gider İşlem", "/kasa-banka/gelir-gider-islem", "bi-cash-stack", null, Yetki("bankahareket.oku")),
                    Oge("Finans Dashboard", "/finans/dashboard", "bi-graph-up", "text-primary"),
                    Oge("Finans Denetim", "/denetim", "bi-shield-check", "text-info", Menu("menu.admin")),
                ], kural: Menu("menu.bankakasa"), acik: true),
                Alt("Bütçe", "bi-wallet2",
                [
                    Oge("Bütçe Analiz", "/butce", "bi-graph-up", "text-primary", Yetki("butceanaliz.oku")),
                    Oge("Ödeme Yönetimi", "/butce/odemeler", "bi-credit-card", "text-warning", Yetki("odemeyonetim.oku")),
                    Oge("Kredi/Taksitler", "/butce/kredi-taksitler", "bi-calendar-plus", "text-danger", Yetki("tekrarlayanodem.oku")),
                    Oge("Bütçe Risk Analizi", "/butce/risk-analiz-aktar", "bi-shield-exclamation", "text-danger", Yetki("butceanaliz.oku")),
                ], kural: Menu("menu.butce")),
                Alt("Muhasebe", "bi-journal-text",
                [
                    Oge("Muhasebe Dashboard", "/muhasebe", "bi-speedometer2", null, Yetki("muhasebedash.oku")),
                    Oge("Hesap Planı", "/muhasebe/hesap-plani", "bi-list-nested", null, Yetki("hesapplani.oku")),
                    Oge("Muhasebe Fişleri", "/muhasebe/fisler", "bi-receipt", null, Yetki("muhasebefis.oku")),
                    Oge("Muhasebeleştirme", "/muhasebe/muhasbelestirme", "bi-journal-check", null, Yetki("muhasebefis.oku")),
                    Oge("Muhasebe Raporları", "/muhasebe/raporlar", "bi-file-earmark-bar-graph", null, Yetki("muhaseberapor.oku")),
                    Oge("Mali Analiz", "/muhasebe/analiz", "bi-graph-up", null, Yetki("malianaliz.oku")),
                ], kural: Menu("menu.muhasebe")),
            ]),

        // ── FİLO VE SERVİS OPERASYONU (eskiden Servis Operasyon; en çok 2 seviye) ──
        Grup("filo", "FİLO VE SERVİS OPERASYONU", "bi-truck", kural: Menu("menu.filoservis"), htmlKimligi: "nav-section-filo-toggle",
            altlar:
            [
                Alt("Servis Operasyonu", "bi-diagram-3",
                [
                    Oge("Operasyon Merkezi", "/operasyon-merkezi", "bi-grid-1x2", "text-success", OperasyonKurali),
                    Oge("Güzergahlar", "/guzergahlar", "bi-signpost-split", "text-primary",
                        Ve(OperasyonKurali, Herhangi("guzergahlar.oku", "guzergah.oku", "serviscalisma.oku", "raporlar.oku"))),
                    Oge("Kurumlar / Müşteri Kartları", "/servis-operasyon/kurumlar", "bi-building", "text-primary"),
                ], acik: true),
                Alt("Tedarikçi Operasyonu", "bi-truck",
                [
                    Oge("Servis Operasyon", "/personel-tasima/tedarikciler", "bi-truck", "text-success",
                        Herhangi("tedarikciservis.oku", "serviscalisma.oku")),
                    Oge("Araçlar", "/tedarikci-servis/araclar", "bi-truck-front", "text-primary",
                        Herhangi("tedarikciarac.oku", "araclar.oku")),
                    Oge("Personel", "/tedarikci-servis/personel", "bi-people", "text-info",
                        Herhangi("tedarikcipersonel.oku", "personel.oku")),
                    Oge("Araç Evrakları", "/tedarikci-servis/arac-evraklari", "bi-folder2-open", "text-warning",
                        Herhangi("tedarikciaracevrak.oku", "araclar.oku")),
                    Oge("İşler", "/tedarikci-servis/isler", "bi-briefcase", "text-secondary",
                        Herhangi("tedarikciservis.oku", "serviscalisma.oku")),
                ], kural: Herhangi("tedarikciservis.oku", "tedarikciarac.oku", "tedarikcipersonel.oku", "tedarikciaracevrak.oku", "serviscalisma.oku")),
                Alt("Araç Yönetimi", "bi-car-front-fill",
                [
                    Oge("Özmal/Kiralık Araçlar", "/araclar", "bi-truck-front-fill", null, Yetki("araclar.oku"), htmlKimligi: "nav-link-araclar"),
                    Oge("Araç Evrakları", "/arac-evraklari", "bi-folder2-open", "text-warning", Yetki("araclar.oku")),
                    Oge("Bakım-Onarım Kayıtları", "/bakim-onarim", "bi-wrench-adjustable-circle", "text-danger", Yetki("araclar.oku")),
                    // Eskiden iki ayrı menü girişiydi (Araçlar altında ve Satış/Kiralama altında); tek sayfa, 3 adres.
                    Oge("Kiralık C Plaka Takip", "/araclar/plaka-takip", "bi-card-text", "text-primary",
                        Veya(Yetki("araclar.oku"), Ve(Menu("menu.satis"), Yetki("raporlar.oku"))),
                        ekYollar: ["/filo-operasyon/kiralik-c-plaka-takip", "/filo-operasyon/kiralık-c-plaka-takip"]),
                    Oge("Araç Maliyet Snapshot", "/arac-maliyet-snapshot", "bi-fuel-pump", "text-info"),
                ]),
                Alt("Lastik Takip", "bi-circle-half",
                [
                    Oge("Lastik Değişimleri", "/lastik/degisimler", "bi-arrow-repeat", "text-warning"),
                    Oge("Lastik Stok", "/lastik/stok", "bi-stack", "text-info"),
                    Oge("Lastik Depoları", "/lastik/depolar", "bi-building"),
                    Oge("Sezon Takibi", "/lastik/sezon-takip", "bi-calendar2-range", "text-success"),
                ], kural: Yetki("araclar.oku")),
            ]),

        // ── RENT A CAR ───────────────────────────────────────────────────────────
        Grup("rentacar", "RENT A CAR", "bi-car-front-fill", kural: Yetki("rentacar.oku"),
            altlar:
            [
                Alt(null, null,
                [
                    // Dört sekme bağlantısı aynı sayfanın sekmelerine kısa yoldur (kopya değil).
                    Oge("Rezervasyon", "/rent-a-car?sekme=rezervasyon", "bi-calendar-plus", varsayilanSekme: true),
                    Oge("Araçlar / Müsaitlik", "/rent-a-car?sekme=araclar", "bi-car-front"),
                    Oge("Kiralamalar", "/rent-a-car?sekme=kiralamalar", "bi-key"),
                    Oge("Takvim", "/rent-a-car/takvim", "bi-calendar3"),
                    Oge("Teslim / İade", "/rent-a-car?sekme=teslim-iade", "bi-arrow-left-right"),
                    Oge("Ödeme Takibi", "/rent-a-car/odemeler", "bi-cash-coin"),
                    Oge("Kara Liste", "/rent-a-car/kara-liste", "bi-slash-circle", null, Rol("Admin")),
                ]),
            ]),

        // ── SATIŞ VE STOK (eskiden Satış/Kiralama + Stok/Envanter) ───────────────
        Grup("satis", "SATIŞ VE STOK", "bi-cart-plus",
            altlar:
            [
                Alt("Satış", "bi-speedometer2",
                [
                    Oge("Satış Dashboard", "/satis", "bi-speedometer2", "text-primary", Yetki("satis.oku")),
                ], kural: Menu("menu.satis"), acik: true),
                // Aynı iş iki modülde iki ayrı veri modeliyle yazılmış; birleştirme ayrı karar (veri taşıma gerektirir).
                Alt("Araç Alım/Satım", "bi-cart-plus",
                [
                    Oge("Alım/Satım (Noter/Fatura)", "/filo-operasyon/alim-satim", "bi-cart-plus", "text-warning",
                        Ve(Menu("menu.satis"), Yetki("raporlar.oku"))),
                    Oge("Alış/Satış (Stok)", "/stok/arac-islem", "bi-truck", "text-success",
                        Ve(Menu("menu.stok"), Yetki("stok.oku"))),
                ]),
                Alt("Piyasa ve İlan", "bi-megaphone",
                [
                    Oge("Piyasa Araştırma", "/satis/piyasa-karsilastirma", "bi-bar-chart-line-fill", "text-info", Yetki("piyasa.oku")),
                    Oge("İlan Platformları", "/ilan-platformlari", "bi-broadcast", "text-danger", Yetki("piyasa.oku")),
                    Oge("Araç İlan Yayınları", "/arac-ilan-yayinlari", "bi-megaphone", "text-info", Yetki("piyasa.oku")),
                ], kural: Menu("menu.satis")),
                Alt("Stok", "bi-box-seam",
                [
                    Oge("Stok Dashboard", "/stok", "bi-speedometer2", "text-primary", Yetki("stok.oku")),
                    Oge("Stok Kartları", "/stok/kartlar", "bi-box-seam", "text-info", Yetki("stok.oku")),
                ], kural: Menu("menu.stok")),
            ]),

        // ── PERSONEL (puantaj eskiden "Resmi Evrak / Özlük" başlığı altındaydı) ──
        Grup("personel", "PERSONEL", "bi-person-badge", kural: Menu("menu.personel"),
            altlar:
            [
                Alt("Kayıt", "bi-people",
                [
                    Oge("Personel Listesi", "/personel", "bi-people", "text-primary", Yetki("personel.oku"), ekYollar: ["/soforler"]),
                    Oge("Personel Excel Import", "/personel/excel-import", "bi-file-earmark-excel", "text-success", Yetki("personel.oku")),
                ], acik: true),
                Alt("Puantaj", "bi-calendar-range",
                [
                    Oge("Çalışma Puantajı", "/personel/puantaj", "bi-calendar-range", "text-secondary", Yetki("maas.oku")),
                    Oge("Puantaj Excel Grid", "/personel/puantaj-grid", "bi-grid-3x3-gap", "text-primary", Yetki("maas.oku")),
                ]),
                Alt("Maaş ve Ödeme", "bi-cash-coin",
                [
                    Oge("Maaş / Ödeme Yönetimi", "/personel/maas", "bi-cash-stack", "text-success", Yetki("maas.oku")),
                    Oge("Banka Ödeme Listesi", "/personel/banka-odeme-listesi", "bi-bank", "text-primary", Yetki("maas.oku")),
                ]),
                Alt("Özlük ve İzin", "bi-folder2-open",
                [
                    Oge("Özlük Evrak Checklist", "/personel/ozluk-evrak", "bi-clipboard-check", "text-warning", Yetki("personel.oku")),
                    Oge("İzin Yönetimi", "/personel/izin", "bi-calendar-check", "text-info", Yetki("izin.oku")),
                ]),
            ]),

        // ── BELGE YÖNETİMİ (Belge Uyarıları eskiden grubun dışında, üstte tek başınaydı) ──
        Grup("ebys", "BELGE YÖNETİMİ (EBYS)", "bi-folder2-open",
            altlar:
            [
                Alt(null, null,
                [
                    Oge("Belge Merkezi", "/ebys", "bi-folder2-open", "text-info", EbysKurali),
                    Oge("Belge Arama", "/ebys/arama", "bi-search", "text-primary", EbysKurali),
                    Oge("Gelen Evraklar", "/ebys/gelen", "bi-inbox-fill", "text-primary", EbysKurali),
                    Oge("Giden Evraklar", "/ebys/giden", "bi-send-fill", "text-success", EbysKurali),
                    Oge("Evrak Takip", "/ebys/takip", "bi-kanban-fill", "text-warning", EbysKurali),
                    Oge("Evrak Kategorileri", "/ebys/kategoriler", "bi-folder2", "text-secondary", EbysKurali),
                ]),
            ]),

        Oge("İhale Projeleri", "/ihale-hazirlik", "bi-briefcase-fill", "text-warning", Herhangi("ihale.oku", "raporlar.oku")),

        // ── RAPORLAR (eskiden yorum satırındaydı; şimdilik yalnız tekilleştirilen rapor) ──
        Grup("raporlar", "RAPORLAR", "bi-bar-chart-fill",
            altlar:
            [
                Alt(null, null,
                [
                    // Eskiden Rent a Car ve Muhasebe altında iki kez geçiyordu (tek sayfa, 3 adres); iki eski yetkiden biri yeterli.
                    Oge("Araç Kâr/Zarar Raporu", "/raporlar/arac-karlilik", "bi-graph-up-arrow", "text-success",
                        Veya(
                            Ve(Yetki("rentacar.oku"), Yetki(Yetkiler.RentACarKarlilikOku)),
                            Ve(Menu("menu.muhasebe"), Yetki(Yetkiler.MuhasebeAracKarlilikOku))),
                        ekYollar: ["/rent-a-car/arac-karlilik", "/muhasebe/arac-karlilik"]),
                ]),
            ]),

        // ── HOLDİNG ──────────────────────────────────────────────────────────────
        Grup("holding", "HOLDİNG YÖNETİMİ", "bi-buildings", kural: Menu(Yetkiler.MenuHolding),
            altlar:
            [
                Alt(null, null,
                [
                    Oge("Holding Dashboard", "/holding", "bi-speedometer2", "text-primary", Yetki(Yetkiler.HoldingDashboardOku)),
                    Oge("Firma Karşılaştırma", "/holding/karsilastirma", "bi-bar-chart", "text-info", Yetki(Yetkiler.HoldingKarsilastirmaOku)),
                    Oge("Bütçe Konsolidasyonu", "/holding/butce", "bi-calculator", "text-success", Yetki(Yetkiler.HoldingButceOku)),
                    Oge("Ödeme Planı", "/holding/odemeler", "bi-cash-stack", "text-warning", Yetki(Yetkiler.HoldingOdemelerOku)),
                    Oge("Araç Maliyet Özeti", "/holding/arac-maliyet", "bi-truck", "text-secondary", Yetki(Yetkiler.HoldingAracMaliyetOku)),
                    Oge("Personel Gider Özeti", "/holding/personel", "bi-people", "text-info", Yetki(Yetkiler.HoldingPersonelGiderOku)),
                    Oge("Hakediş Özeti", "/holding/hakedis", "bi-clipboard-check", "text-success", Yetki(Yetkiler.HoldingHakedisOku)),
                ]),
            ]),

        // ── AYARLAR (33 → 27 bağlantı; Destek ayrı grup, Luca Portal Faturalar'a taşındı) ──
        Grup("ayarlar", "AYARLAR", "bi-gear-fill", kural: Menu("menu.ayarlar"),
            altlar:
            [
                Alt("Kişisel", "bi-person-circle",
                [
                    Oge("Profilim", "/ayarlar/profil", "bi-person-circle", "text-success"),
                    Oge("Bildirimlerim", "/ayarlar/bildirimler", "bi-bell", "text-warning"),
                    Oge("Kullanıcı Tercihleri", "/kullanici-tercihleri", "bi-person-gear", "text-info"),
                ], acik: true),
                Alt("Tanımlar", "bi-sliders",
                [
                    Oge("Firma Yönetimi", "/ayarlar/firmalar", "bi-building", "text-info", Yetki("firmayonetim.oku")),
                    Oge("Cari / Muhasebe Ayarları", "/ayarlar/muhasebe-cari", "bi-journal-text", "text-primary", Yetki(Yetkiler.CariMuhasebeAyarOku)),
                    Oge("Masraf Kalemleri", "/masraf-kalemleri", "bi-list-task", "text-secondary", Yetki("masrafkalem.oku")),
                    Oge("Araç Şase Yönetimi", "/ayarlar/arac-sase-yonetimi", "bi-upc-scan", "text-secondary", Yetki("aracsase.oku")),
                    Oge("Belge Uyarı Ayarları", "/ayarlar/belge-uyari", "bi-shield-exclamation", "text-danger", Yetki(Yetkiler.BelgeUyariAyarOku)),
                    Oge("Piyasa Kaynakları", "/ayarlar/piyasa-kaynaklari", "bi-globe", "text-primary", Yetki("piyasakaynak.oku")),
                    Oge("Web Sayfası Yönetimi", "/ayarlar/web-sayfasi", "bi-window", "text-primary", Yetki(Yetkiler.WebSayfasiOku)),
                ]),
                Alt("Kullanıcı ve Yetki", "bi-shield-lock",
                [
                    Oge("Kullanıcı Yönetimi", "/ayarlar/kullanicilar", "bi-person-badge", "text-info", Yetki("kullanici.oku")),
                    Oge("Rol Yönetimi", "/ayarlar/roller", "bi-shield-check", "text-danger", Yetki("rol.oku")),
                    Oge("KVKK Yönetimi", "/ayarlar/kvkk", "bi-shield-lock-fill", "text-success", Yetki("ayarlar.oku")),
                ]),
                Alt("Sistem ve Bakım", "bi-cpu",
                [
                    Oge("Veritabanı Ayarları", "/ayarlar/veritabani", "bi-database-gear", null, Yetki("veritabani.oku")),
                    Oge("Veritabanı Yedekleme", "/ayarlar/yedekleme", "bi-database-down", null, Yetki("yedekleme.oku")),
                    Oge("Sistem Durumu", "/sistem-durumu", "bi-heart-pulse", null, Yetki("sistemdurumu.oku")),
                    Oge("Aktivite Logu", "/aktivite-log", "bi-clock-history", null, Yetki("aktivitelog.oku")),
                    Oge("Uygulama Güncelleme", "/ayarlar/guncelleme", "bi-cloud-arrow-down", "text-success", Yetki("guncelleme.oku")),
                ]),
                Alt("Dosya ve Araçlar", "bi-folder-fill",
                [
                    Oge("Dosya Şifreleme", "/ayarlar/dosya-migrasyonu", "bi-shield-lock", null, Yetki("ayarlar.oku")),
                    Oge("Dizin Yönetimi", "/ayarlar/dizin-yonetimi", "bi-folder-fill", "text-warning", Yetki("guncelleme.oku")),
                    Oge("Arşiv Dosya Görüntüleyici", "/ayarlar/arsiv-goruntuleyici", "bi-archive", "text-warning", Yetki(Yetkiler.ArsivGoruntuleyiciOku)),
                    Oge("Demo Veri", "/ayarlar/demo-veri", "bi-database-fill-gear", "text-warning", Yetki("ayarlar.oku")),
                    Oge("Test Modu", "/admin/test", "bi-shield-shaded", "text-info", Menu("menu.admin")),
                ]),
                Alt("Entegrasyon", "bi-plug-fill",
                [
                    Oge("Luca Ayarları", "/ayarlar/luca-portal", "bi-gear", "text-secondary", Yetki("ayarlar.oku")),
                    Oge("Webhook Yönetimi", "/ayarlar/webhooks", "bi-broadcast", "text-primary", Yetki("ayarlar.oku")),
                    Oge("SMS Ayarları", "/ayarlar/sms", "bi-chat-dots", "text-success", Yetki("ayarlar.oku")),
                    Oge("Entegrasyon Rehberi", "/ayarlar/entegrasyon-rehberi", "bi-diagram-3-fill", "text-warning", Yetki("ayarlar.oku")),
                ]),
            ]),

        // ── DESTEK: sol menü yalnız destek/yazılım ekibine açık. Kullanıcı kendi taleplerine üst çubuktan gider. ──
        Grup("destek", "DESTEK", "bi-headset",
            altlar:
            [
                Alt(null, null,
                [
                    Oge("Destek Dashboard", "/destek-talepleri/dashboard", "bi-speedometer2", "text-primary", Yetki("destektalebi.yaz")),
                    Oge("Yeni Talep", "/destek-talepleri/talep-giris", "bi-plus-circle", "text-success", HerKullanici("destektalebi.oku")),
                    Oge("Taleplerim", "/destek-talepleri/taleplerim", "bi-ticket-detailed", "text-info", HerKullanici("destektalebi.oku")),
                    Oge("Bilgi Bankası", "/destek-talepleri/bilgi-bankasi", "bi-book-fill", "text-info", HerKullanici("destektalebi.oku")),
                    Oge("Tüm Talepler", "/destek-talepleri", "bi-ticket-detailed-fill", "text-primary", Herhangi("destektalebi.yaz", "destektalebi.yazilim")),
                    Oge("Talep Yönetimi", "/destek-talepleri/yonetim", "bi-kanban", "text-warning", Herhangi("destektalebi.yaz", "destektalebi.yazilim")),
                    Oge("Destek Ayarları", "/destek-talepleri/ayarlar", "bi-sliders", "text-secondary", Yetki("destektalebi.duzenle")),
                ]),
            ], kural: Herhangi("destektalebi.yaz", "destektalebi.yazilim", "destektalebi.duzenle")),
    ];

    // ── ortak koşullar ──────────────────────────────────────────────────────────

    // Not: bunlar özellikle ifade gövdeli (her çağrıda yeni kural). Statik alan başlatıcısı olsalardı, metin sırası
    // yüzünden Kokler'den sonra çalışır ve menü öğelerine null (= herkese görünür) kural verirdi.

    /// <summary>Operasyon Merkezi ve Güzergahlar için eski ortak koşul.</summary>
    private static MenuKurali OperasyonKurali =>
        Herhangi("serviscalisma.oku", "tedarikciservis.oku", "raporlar.oku", "guzergahlar.oku", "toplucalisma.oku");

    /// <summary>EBYS sayfaları: modül menüsü ve ebys/belge uyarısı yetkisinden biri.</summary>
    private static MenuKurali EbysKurali =>
        Ve(Menu("menu.ebys"), Herhangi("ebys.oku", "belgeuyari.oku"));

    // ── kurucu yardımcılar ──────────────────────────────────────────────────────

    private static MenuOgesi Oge(
        string etiket,
        string href,
        string ikon,
        string? renk = null,
        MenuKurali? kural = null,
        string? htmlKimligi = null,
        string[]? ekYollar = null,
        bool varsayilanSekme = false)
        => new(etiket, href, ikon, renk, kural, htmlKimligi, ekYollar, varsayilanSekme);

    private static MenuAltGrubu Alt(string? baslik, string? ikon, MenuOgesi[] ogeler, MenuKurali? kural = null, bool acik = false)
        => new(baslik, ikon, ogeler, kural, acik);

    private static MenuGrubu Grup(
        string anahtar,
        string baslik,
        string ikon,
        MenuAltGrubu[] altlar,
        MenuKurali? kural = null,
        string? htmlKimligi = null)
        => new(anahtar, baslik, ikon, altlar, kural, htmlKimligi);
}
