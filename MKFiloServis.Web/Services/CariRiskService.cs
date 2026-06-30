using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class CariRiskService : ICariRiskService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IOllamaService _ollamaService;

    public CariRiskService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IOllamaService ollamaService)
    {
        _contextFactory = contextFactory;
        _ollamaService = ollamaService;
    }

    public async Task<CariRiskOzet> GetRiskOzetAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var bugun = DateTime.Today;
        
        // Tüm aktif cariler
        var cariler = await context.Cariler
            .Where(c => !c.IsDeleted && c.Aktif)
            .ToListAsync();
        
        // Vadesi geçmiş faturalar (tahsilat bekleyen) - Giden fatura = kesilen fatura = gelir
        // NOT: KalanTutar hesaplanmış property, LINQ'da (GenelToplam - OdenenTutar) kullanılmalı
        var vadesiGecmisFaturalar = await context.Faturalar
            .Where(f => !f.IsDeleted && 
                       f.FaturaYonu == FaturaYonu.Giden && 
                       f.VadeTarihi.HasValue && f.VadeTarihi < bugun &&
                       (f.GenelToplam - f.OdenenTutar) > 0)
            .ToListAsync();
        
        var vadesiGecmisCariIds = vadesiGecmisFaturalar.Select(f => f.CariId).Distinct().ToList();
        
        // Ödenmiş faturalar için ortalama tahsilat süresi - OdemeTarihi yok, VadeTarihi ile fatura tarihi farkını kullan
        // NOT: KalanTutar hesaplanmış property, LINQ'da (GenelToplam - OdenenTutar) kullanılmalı
        var odenmisGidenFaturalari = await context.Faturalar
            .Where(f => !f.IsDeleted && 
                       f.FaturaYonu == FaturaYonu.Giden && 
                       (f.GenelToplam - f.OdenenTutar) == 0 &&
                       f.VadeTarihi.HasValue)
            .ToListAsync();

        // Basitleştirilmiş: Ortalama vade süresi (fatura tarihi - vade tarihi arası gün)
        var ortalamaTahsilatSuresi = odenmisGidenFaturalari.Any()
            ? odenmisGidenFaturalari.Average(f => (f.VadeTarihi!.Value - f.FaturaTarihi).TotalDays)
            : 0;
        
        // Risk skoru 50'nin üzerinde olanlar
        var riskliCarilar = await GetRiskKartlariAsync(new CariRiskFilterParams { MinGecikmeGunu = 1 });
        
        return new CariRiskOzet
        {
            ToplamCariSayisi = cariler.Count,
            VadesiGecmisCariSayisi = vadesiGecmisCariIds.Count,
            RiskliCariSayisi = riskliCarilar.Count(r => r.RiskSkoru > 50),
            ToplamVadesiGecmisBorc = vadesiGecmisFaturalar.Sum(f => f.KalanTutar),
            OrtalamaTahsilatSuresi = (decimal)ortalamaTahsilatSuresi,
            ToplamAcikAlacak = cariler.Sum(c => c.Borc), // Alacağımız (müşteri borcu bize)
            ToplamAcikBorc = cariler.Sum(c => c.Alacak)  // Borcumuz (bizim borcumuz tedarikçiye)
        };
    }

    public async Task<List<CariRiskKarti>> GetRiskKartlariAsync(CariRiskFilterParams? filtre = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        filtre ??= new CariRiskFilterParams();
        var bugun = DateTime.Today;
        
        // Tüm aktif carileri al
        var cariQuery = context.Cariler
            .Where(c => !c.IsDeleted && c.Aktif);
        
        if (filtre.CariTipi.HasValue)
            cariQuery = cariQuery.Where(c => c.CariTipi == filtre.CariTipi.Value);
        
        if (!string.IsNullOrEmpty(filtre.SearchTerm))
            cariQuery = cariQuery.Where(c => c.Unvan.Contains(filtre.SearchTerm) || 
                                             c.CariKodu.Contains(filtre.SearchTerm));
        
        var cariler = await cariQuery.ToListAsync();
        
        // Tüm giden faturaları (alacaklar - bizim kestiğimiz faturalar)
        var gidenFaturalari = await context.Faturalar
            .Where(f => !f.IsDeleted && f.FaturaYonu == FaturaYonu.Giden)
            .ToListAsync();

        // Ödeme eşleştirmeleri (faturaya yapılan ödemeler)
        var odemeEslestirmeleri = await context.OdemeEslestirmeleri
            .Where(oe => !oe.IsDeleted)
            .ToListAsync();

        // İletişim notları
        var iletisimNotlari = await context.CariIletisimNotlar
            .Where(n => !n.IsDeleted)
            .GroupBy(n => n.CariId)
            .Select(g => new { CariId = g.Key, SonTarih = g.Max(n => n.IletisimTarihi) })
            .ToListAsync();

        var riskKartlari = new List<CariRiskKarti>();

        foreach (var cari in cariler)
        {
            var cariFaturalari = gidenFaturalari.Where(f => f.CariId == cari.Id).ToList();
            var cariFaturaIds = cariFaturalari.Select(f => f.Id).ToList();
            var cariOdemeleri = odemeEslestirmeleri.Where(oe => cariFaturaIds.Contains(oe.FaturaId)).ToList();
            
            // Vadesi geçmiş faturalar (VadeTarihi nullable)
            var vadesiGecmisFaturalar = cariFaturalari
                .Where(f => f.VadeTarihi.HasValue && f.VadeTarihi.Value < bugun && f.KalanTutar > 0)
                .ToList();
            
            // Yaşlandırma hesapla
            var yaslandirma = HesaplaYaslandirma(vadesiGecmisFaturalar, bugun);
            
            // Son 1 yıl ciro
            var birYilOnce = bugun.AddYears(-1);
            var sonBirYilFaturalar = cariFaturalari.Where(f => f.FaturaTarihi >= birYilOnce).ToList();
            
            // Ortalama tahsilat süresi - OdemeTarihi yok, VadeTarihi kullanılıyor
            var odenmisler = cariFaturalari.Where(f => f.KalanTutar == 0 && f.VadeTarihi.HasValue).ToList();
            var ortTahsilatSuresi = odenmisler.Any()
                ? (decimal)odenmisler.Average(f => Math.Abs((f.VadeTarihi!.Value - f.FaturaTarihi).TotalDays))
                : 0;

            // Vade uyum oranı - zamanında ödenen fatura yüzdesi (bugüne kadar vadesi gelmiş ve ödenmiş olanlar)
            var vadesiGelmisOdenmis = odenmisler.Where(f => f.VadeTarihi!.Value <= bugun).ToList();
            var vadedeOdenen = vadesiGelmisOdenmis.Count; // Ödenmiş olanlar zamanında sayılır
            var vadeUyumOrani = vadesiGelmisOdenmis.Any() ? (decimal)vadedeOdenen / vadesiGelmisOdenmis.Count * 100 : 100;
            
            // Risk skoru hesapla
            var riskSkoru = HesaplaRiskSkoru(
                vadesiGecmisFaturalar.Sum(f => f.KalanTutar),
                vadesiGecmisFaturalar.Any() ? (bugun - vadesiGecmisFaturalar.Min(f => f.VadeTarihi!.Value)).Days : 0,
                ortTahsilatSuresi,
                vadeUyumOrani,
                sonBirYilFaturalar.Sum(f => f.GenelToplam)
            );
            
            // Risk faktörleri
            var riskFaktorleri = new List<string>();
            if (vadesiGecmisFaturalar.Sum(f => f.KalanTutar) > 0)
                riskFaktorleri.Add($"Vadesi geçmiş {FormatPara(vadesiGecmisFaturalar.Sum(f => f.KalanTutar))} borç");
            if (yaslandirma.Vade90Plus > 0)
                riskFaktorleri.Add($"90+ gün vadesi geçmiş: {FormatPara(yaslandirma.Vade90Plus)}");
            if (ortTahsilatSuresi > 30)
                riskFaktorleri.Add($"Ortalama tahsilat süresi: {ortTahsilatSuresi:N0} gün");
            if (vadeUyumOrani < 50)
                riskFaktorleri.Add($"Düşük vade uyum oranı: %{vadeUyumOrani:N0}");
            
            var sonIletisim = iletisimNotlari.FirstOrDefault(i => i.CariId == cari.Id);
            
            var kart = new CariRiskKarti
            {
                CariId = cari.Id,
                CariKodu = cari.CariKodu,
                CariUnvan = cari.Unvan,
                CariTipi = cari.CariTipi,
                RiskSkoru = riskSkoru,
                RiskSeviyesi = GetRiskSeviyesi(riskSkoru),
                ToplamBorc = cari.Borc,
                ToplamAlacak = cari.Alacak,
                Bakiye = cari.Borc - cari.Alacak, // Hesaplanmış bakiye
                VadesiGecmisBorc = vadesiGecmisFaturalar.Sum(f => f.KalanTutar),
                VadesiGecmisGunSayisi = vadesiGecmisFaturalar.Any() ? (bugun - vadesiGecmisFaturalar.Min(f => f.VadeTarihi!.Value)).Days : 0,
                VadesiGecmisFaturaSayisi = vadesiGecmisFaturalar.Count,
                Vade0_30 = yaslandirma.Vade0_30,
                Vade31_60 = yaslandirma.Vade31_60,
                Vade61_90 = yaslandirma.Vade61_90,
                Vade90Plus = yaslandirma.Vade90Plus,
                SonBirYilFaturaSayisi = sonBirYilFaturalar.Count,
                SonBirYilToplamCiro = sonBirYilFaturalar.Sum(f => f.GenelToplam),
                OrtalamaTahsilatSuresi = ortTahsilatSuresi,
                OdemeVadeUyumOrani = vadeUyumOrani,
                SonFaturaTarihi = cariFaturalari.Any() ? cariFaturalari.Max(f => f.FaturaTarihi) : null,
                SonOdemeTarihi = cariOdemeleri.Any() ? cariOdemeleri.Max(o => o.EslestirmeTarihi) : null,
                SonIletisimTarihi = sonIletisim?.SonTarih,
                RiskFaktorleri = riskFaktorleri
            };
            
            riskKartlari.Add(kart);
        }
        
        // Filtreleme
        if (filtre.RiskSeviyesi.HasValue)
            riskKartlari = riskKartlari.Where(r => r.RiskSeviyesi == filtre.RiskSeviyesi.Value).ToList();
        
        if (filtre.MinGecikmeGunu.HasValue)
            riskKartlari = riskKartlari.Where(r => r.VadesiGecmisGunSayisi >= filtre.MinGecikmeGunu.Value).ToList();
        
        if (filtre.MinBorcTutari.HasValue)
            riskKartlari = riskKartlari.Where(r => r.VadesiGecmisBorc >= filtre.MinBorcTutari.Value).ToList();
        
        // Sıralama
        riskKartlari = filtre.SortBy switch
        {
            "VadesiGecmisBorc" => filtre.SortDescending 
                ? riskKartlari.OrderByDescending(r => r.VadesiGecmisBorc).ToList()
                : riskKartlari.OrderBy(r => r.VadesiGecmisBorc).ToList(),
            "GecikmeGunu" => filtre.SortDescending
                ? riskKartlari.OrderByDescending(r => r.VadesiGecmisGunSayisi).ToList()
                : riskKartlari.OrderBy(r => r.VadesiGecmisGunSayisi).ToList(),
            _ => filtre.SortDescending
                ? riskKartlari.OrderByDescending(r => r.RiskSkoru).ToList()
                : riskKartlari.OrderBy(r => r.RiskSkoru).ToList()
        };
        
        return riskKartlari;
    }

    public async Task<CariRiskKarti?> GetCariRiskKartiAsync(int cariId)
    {
        var kartlar = await GetRiskKartlariAsync(new CariRiskFilterParams { SearchTerm = cariId.ToString() });
        return kartlar.FirstOrDefault(k => k.CariId == cariId);
    }

    public async Task<List<VadesiGecmisFatura>> GetVadesiGecmisFaturalarAsync(int? cariId = null, int? minGecikmeGunu = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var bugun = DateTime.Today;
        var minVadeTarihi = minGecikmeGunu.HasValue ? bugun.AddDays(-minGecikmeGunu.Value) : bugun;
        
        // NOT: KalanTutar hesaplanmış property, LINQ'da (GenelToplam - OdenenTutar) kullanılmalı
        var query = context.Faturalar
            .Include(f => f.Cari)
            .Where(f => !f.IsDeleted && 
                       f.FaturaYonu == FaturaYonu.Giden && 
                       f.VadeTarihi.HasValue && f.VadeTarihi < bugun &&
                       (f.GenelToplam - f.OdenenTutar) > 0);

        if (cariId.HasValue)
            query = query.Where(f => f.CariId == cariId.Value);

        if (minGecikmeGunu.HasValue)
            query = query.Where(f => f.VadeTarihi <= minVadeTarihi);

        var faturalar = await query.OrderBy(f => f.VadeTarihi).ToListAsync();
        
        return faturalar.Select(f => new VadesiGecmisFatura
        {
            FaturaId = f.Id,
            FaturaNo = f.FaturaNo,
            FaturaTarihi = f.FaturaTarihi,
            VadeTarihi = f.VadeTarihi!.Value,
            Tutar = f.GenelToplam,
            KalanTutar = f.KalanTutar,
            GecikmeGunu = (bugun - f.VadeTarihi!.Value).Days,
            CariId = f.CariId,
            CariUnvan = f.Cari?.Unvan ?? ""
        }).ToList();
    }

    public async Task<decimal> GetToplamVadesiGecmisBorcAsync()
    {
        var faturalar = await GetVadesiGecmisFaturalarAsync();
        return faturalar.Sum(f => f.KalanTutar);
    }

    public async Task<List<RiskTrendItem>> GetRiskTrendAsync(int aylikDonemSayisi = 12)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var trendItems = new List<RiskTrendItem>();
        var bugun = DateTime.Today;
        
        for (int i = aylikDonemSayisi - 1; i >= 0; i--)
        {
            var donemTarih = bugun.AddMonths(-i);
            var donemSonu = new DateTime(donemTarih.Year, donemTarih.Month, DateTime.DaysInMonth(donemTarih.Year, donemTarih.Month));
            
            // O dönemdeki vadesi geçmiş faturalar (OdemeTarihi yok - KalanTutar > 0 ile kontrol)
            // NOT: KalanTutar hesaplanmış property, LINQ'da (GenelToplam - OdenenTutar) kullanılmalı
            var vadesiGecmisler = await context.Faturalar
                .Where(f => !f.IsDeleted && 
                           f.FaturaYonu == FaturaYonu.Giden && 
                           f.VadeTarihi.HasValue && f.VadeTarihi < donemSonu &&
                           (f.GenelToplam - f.OdenenTutar) > 0)
                .ToListAsync();
            
            trendItems.Add(new RiskTrendItem
            {
                Yil = donemTarih.Year,
                Ay = donemTarih.Month,
                Donem = donemTarih.ToString("MMM yy"),
                RiskliCariSayisi = vadesiGecmisler.Select(f => f.CariId).Distinct().Count(),
                VadesiGecmisToplamBorc = vadesiGecmisler.Sum(f => f.KalanTutar),
                OrtalamaTahsilatSuresi = 0 // Basitleştirme için şimdilik 0
            });
        }
        
        return trendItems;
    }

    public async Task<int> HesaplaRiskSkoruAsync(int cariId)
    {
        var kart = await GetCariRiskKartiAsync(cariId);
        return kart?.RiskSkoru ?? 0;
    }

    public Task RecalculateAllRiskScoresAsync()
    {
        // Risk skorları dinamik hesaplandığı için bu metod gereksiz
        return Task.CompletedTask;
    }

    public async Task<string> GetAIRiskAnaliziAsync(int cariId)
    {
        var kart = await GetCariRiskKartiAsync(cariId);
        if (kart == null) return "Cari bulunamadı.";
        
        var prompt = $$"""
        Aşağıdaki müşteri risk verilerini analiz et ve Türkçe olarak kısa öneriler sun:
        
        MÜŞTERİ: {{kart.CariUnvan}}
        - Risk Skoru: {{kart.RiskSkoru}}/100 ({{kart.RiskSeviyesi}})
        - Vadesi Geçmiş Borç: {{kart.VadesiGecmisBorc:N2}} TL
        - En Eski Gecikme: {{kart.VadesiGecmisGunSayisi}} gün
        - Vadesi Geçmiş Fatura: {{kart.VadesiGecmisFaturaSayisi}} adet
        - Yaşlandırma: 0-30 gün: {{kart.Vade0_30:N2}} TL, 31-60 gün: {{kart.Vade31_60:N2}} TL, 61-90 gün: {{kart.Vade61_90:N2}} TL, 90+ gün: {{kart.Vade90Plus:N2}} TL
        - Son 1 Yıl Ciro: {{kart.SonBirYilToplamCiro:N2}} TL
        - Ortalama Tahsilat Süresi: {{kart.OrtalamaTahsilatSuresi:N0}} gün
        - Vade Uyum Oranı: %{{kart.OdemeVadeUyumOrani:N0}}
        
        Lütfen şunları analiz et:
        1. Risk değerlendirmesi (1-2 cümle)
        2. Tahsilat stratejisi önerisi (1-2 cümle)
        3. İzlenecek aksiyonlar (3 madde)
        
        Kısa ve öz cevap ver.
        """;

        try
        {
            var sistemPrompt = "Sen bir finans ve risk yönetimi uzmanısın. Cari hesap risk analizi yapıyorsun.";
            return await _ollamaService.AnalizYapAsync(prompt, sistemPrompt);
        }
        catch
        {
            return "AI analiz yapılamadı. Ollama bağlantısını kontrol edin.";
        }
    }

    public async Task<string> GetTopluAIRiskAnaliziAsync()
    {
        var ozet = await GetRiskOzetAsync();
        var riskliKartlar = await GetRiskKartlariAsync(new CariRiskFilterParams { MinGecikmeGunu = 1 });
        var topKritik = riskliKartlar.Where(r => r.RiskSeviyesi == RiskSeviyesi.KritikRisk).Take(5).ToList();
        
        var prompt = $$"""
        Aşağıdaki firma alacak risk durumunu analiz et ve Türkçe olarak öneriler sun:
        
        GENEL DURUM:
        - Toplam Cari: {{ozet.ToplamCariSayisi}}
        - Riskli Cari: {{ozet.RiskliCariSayisi}}
        - Vadesi Geçmiş Cari: {{ozet.VadesiGecmisCariSayisi}}
        - Toplam Vadesi Geçmiş Borç: {{ozet.ToplamVadesiGecmisBorc:N2}} TL
        - Ortalama Tahsilat Süresi: {{ozet.OrtalamaTahsilatSuresi:N0}} gün
        
        KRİTİK DURUMDA MÜŞTERİLER:
        {{string.Join("\n", topKritik.Select(k => $"- {k.CariUnvan}: {k.VadesiGecmisBorc:N2} TL vadesi geçmiş, {k.VadesiGecmisGunSayisi} gün gecikme"))}}
        
        Lütfen şunları analiz et:
        1. Genel risk değerlendirmesi (2-3 cümle)
        2. Öncelikli tahsilat stratejisi
        3. Risk azaltma önerileri (3-4 madde)
        
        Kısa ve öz cevap ver.
        """;

        try
        {
            var sistemPrompt = "Sen bir finans ve risk yönetimi uzmanısın. Şirket geneli alacak risk analizi yapıyorsun.";
            return await _ollamaService.AnalizYapAsync(prompt, sistemPrompt);
        }
        catch
        {
            return "AI analiz yapılamadı. Ollama bağlantısını kontrol edin.";
        }
    }

    #region Private Helpers

    private int HesaplaRiskSkoru(decimal vadesiGecmisBorc, int gecikmeGunu, decimal ortTahsilatSuresi, decimal vadeUyumOrani, decimal sonBirYilCiro)
    {
        var skor = 0;
        
        // Vadesi geçmiş borç etkisi (0-40 puan)
        if (vadesiGecmisBorc > 0)
        {
            if (vadesiGecmisBorc > 100000) skor += 40;
            else if (vadesiGecmisBorc > 50000) skor += 30;
            else if (vadesiGecmisBorc > 20000) skor += 20;
            else if (vadesiGecmisBorc > 5000) skor += 10;
            else skor += 5;
        }
        
        // Gecikme süresi etkisi (0-30 puan)
        if (gecikmeGunu > 90) skor += 30;
        else if (gecikmeGunu > 60) skor += 20;
        else if (gecikmeGunu > 30) skor += 10;
        else if (gecikmeGunu > 0) skor += 5;
        
        // Ortalama tahsilat süresi etkisi (0-15 puan)
        if (ortTahsilatSuresi > 60) skor += 15;
        else if (ortTahsilatSuresi > 45) skor += 10;
        else if (ortTahsilatSuresi > 30) skor += 5;
        
        // Vade uyum oranı etkisi (0-15 puan)
        if (vadeUyumOrani < 30) skor += 15;
        else if (vadeUyumOrani < 50) skor += 10;
        else if (vadeUyumOrani < 70) skor += 5;
        
        // Ciro düşükse bonus puan (yeni/az işlem yapan müşteri) (-5 puan)
        if (sonBirYilCiro < 10000 && vadesiGecmisBorc > 0) skor += 5;
        
        return Math.Min(skor, 100);
    }

    private RiskSeviyesi GetRiskSeviyesi(int riskSkoru)
    {
        return riskSkoru switch
        {
            <= 25 => RiskSeviyesi.DusukRisk,
            <= 50 => RiskSeviyesi.OrtaRisk,
            <= 75 => RiskSeviyesi.YuksekRisk,
            _ => RiskSeviyesi.KritikRisk
        };
    }

    private (decimal Vade0_30, decimal Vade31_60, decimal Vade61_90, decimal Vade90Plus) HesaplaYaslandirma(
        List<Fatura> vadesiGecmisFaturalar, DateTime bugun)
    {
        decimal v0_30 = 0, v31_60 = 0, v61_90 = 0, v90Plus = 0;

        foreach (var fatura in vadesiGecmisFaturalar)
        {
            if (!fatura.VadeTarihi.HasValue) continue;

            var gecikme = (bugun - fatura.VadeTarihi.Value).Days;
            if (gecikme <= 30) v0_30 += fatura.KalanTutar;
            else if (gecikme <= 60) v31_60 += fatura.KalanTutar;
            else if (gecikme <= 90) v61_90 += fatura.KalanTutar;
            else v90Plus += fatura.KalanTutar;
        }

        return (v0_30, v31_60, v61_90, v90Plus);
    }

    private static string FormatPara(decimal tutar)
    {
        return tutar.ToString("N2") + " ₺";
    }

    #endregion
}


