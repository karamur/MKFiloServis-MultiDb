using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// SMS gönderim servisi - birden fazla SMS provider desteği
/// </summary>
public class SmsService : ISmsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<SmsService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SmsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<SmsService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    #region SMS Ayarları

    public async Task<SmsAyar?> GetAktifAyarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<SmsAyar>()
            .FirstOrDefaultAsync(a => !a.IsDeleted && a.Aktif);
    }

    public async Task<List<SmsAyar>> GetTumAyarlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<SmsAyar>()
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.Aktif)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<SmsAyar> SaveAyarAsync(SmsAyar ayar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (ayar.Aktif)
        {
            // Diğer aktif ayarları pasif yap (tek aktif ayar olsun)
            var digerAktifler = await context.Set<SmsAyar>()
                .Where(a => !a.IsDeleted && a.Aktif && a.Id != ayar.Id)
                .ToListAsync();

            foreach (var diger in digerAktifler)
            {
                diger.Aktif = false;
            }
        }

        if (ayar.Id == 0)
        {
            ayar.CreatedAt = DateTime.Now;
            context.Set<SmsAyar>().Add(ayar);
        }
        else
        {
            ayar.UpdatedAt = DateTime.Now;
            context.Set<SmsAyar>().Update(ayar);
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("SMS ayarı kaydedildi: Provider={Provider}, Aktif={Aktif}", ayar.Provider, ayar.Aktif);
        return ayar;
    }

    public async Task<bool> TestBaglantisiAsync(int ayarId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await context.Set<SmsAyar>().FindAsync(ayarId);
        if (ayar == null) return false;

        try
        {
            var bakiye = await BakiyeSorgulaInternalAsync(context, ayar);
            return bakiye.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS bağlantı testi başarısız: {Provider}", ayar.Provider);
            return false;
        }
    }

    public async Task<decimal?> BakiyeSorgulaAsync(int ayarId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await context.Set<SmsAyar>().FindAsync(ayarId);
        if (ayar == null) return null;

        var bakiye = await BakiyeSorgulaInternalAsync(context, ayar);
        
        if (bakiye.HasValue)
        {
            ayar.Bakiye = bakiye;
            ayar.SonBakiyeSorguTarihi = DateTime.Now;
            await context.SaveChangesAsync();
        }

        return bakiye;
    }

    private async Task<decimal?> BakiyeSorgulaInternalAsync(ApplicationDbContext context, SmsAyar ayar)
    {
        try
        {
            return ayar.Provider switch
            {
                SmsProvider.NetGsm => await NetGsmBakiyeSorgulaAsync(context, ayar),
                SmsProvider.Iletimerkezi => await IletimerkeziBakiyeSorgulaAsync(context, ayar),
                SmsProvider.Mutlucell => await MutlucellBakiyeSorgulaAsync(context, ayar),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bakiye sorgulama hatası: {Provider}", ayar.Provider);
            return null;
        }
    }

    #endregion

    #region SMS Gönderimi

    public async Task<SmsGonderimSonuc> GonderAsync(string telefon, string mesaj, SmsTipi tip = SmsTipi.Bildirim, 
        string? iliskiliTablo = null, int? iliskiliKayitId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await GetAktifAyarAsync();
        if (ayar == null)
        {
            _logger.LogWarning("SMS gönderilemedi: Aktif SMS ayarı bulunamadı");
            return new SmsGonderimSonuc
            {
                Basarili = false,
                HataMesaji = "SMS ayarları yapılandırılmamış"
            };
        }

        // Telefon numarasını formatla
        telefon = FormatTelefon(telefon);
        if (string.IsNullOrEmpty(telefon))
        {
            return new SmsGonderimSonuc
            {
                Basarili = false,
                HataMesaji = "Geçersiz telefon numarası"
            };
        }

        // Log kaydı oluştur
        var log = new SmsLog
        {
            SmsAyarId = ayar.Id,
            Telefon = telefon,
            Mesaj = mesaj,
            Tip = tip,
            IliskiliTablo = iliskiliTablo,
            IliskiliKayitId = iliskiliKayitId,
            Durum = SmsGonderimDurum.Bekliyor,
            CreatedAt = DateTime.Now
        };
        context.Set<SmsLog>().Add(log);
        await context.SaveChangesAsync();

        try
        {
            var sonuc = ayar.Provider switch
            {
                SmsProvider.NetGsm => await NetGsmGonderAsync(context, ayar, telefon, mesaj),
                SmsProvider.Iletimerkezi => await IletimerkeziGonderAsync(context, ayar, telefon, mesaj),
                SmsProvider.Mutlucell => await MutlucellGonderAsync(context, ayar, telefon, mesaj),
                SmsProvider.Twilio => await TwilioGonderAsync(context, ayar, telefon, mesaj),
                _ => new SmsGonderimSonuc { Basarili = false, HataMesaji = "Desteklenmeyen SMS provider" }
            };

            // Log güncelle
            log.Durum = sonuc.Basarili ? SmsGonderimDurum.Gonderildi : SmsGonderimDurum.Basarisiz;
            log.ProviderMesajId = sonuc.MesajId;
            log.HataMesaji = sonuc.HataMesaji;
            log.GonderimTarihi = DateTime.Now;

            // Ayar istatistiklerini güncelle
            if (sonuc.Basarili)
            {
                ayar.ToplamGonderilenSms++;
                ayar.SonGonderimTarihi = DateTime.Now;
            }
            else
            {
                ayar.ToplamBasarisizSms++;
            }

            if (sonuc.KalanBakiye.HasValue)
            {
                ayar.Bakiye = sonuc.KalanBakiye;
            }

            await context.SaveChangesAsync();

            sonuc.LogId = log.Id;
            _logger.LogInformation("SMS gönderildi: Telefon={Telefon}, Basarili={Basarili}", telefon, sonuc.Basarili);
            return sonuc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS gönderim hatası: {Telefon}", telefon);
            log.Durum = SmsGonderimDurum.Basarisiz;
            log.HataMesaji = ex.Message;
            await context.SaveChangesAsync();

            return new SmsGonderimSonuc
            {
                Basarili = false,
                HataMesaji = ex.Message,
                LogId = log.Id
            };
        }
    }

    public async Task<List<SmsGonderimSonuc>> TopluGonderAsync(List<SmsGonderimIstek> istekler)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuclar = new List<SmsGonderimSonuc>();

        foreach (var istek in istekler)
        {
            var sonuc = await GonderAsync(istek.Telefon, istek.Mesaj, istek.Tip, istek.IliskiliTablo, istek.IliskiliKayitId);
            sonuclar.Add(sonuc);

            // Rate limiting - her SMS arasında kısa bekleme
            await Task.Delay(100);
        }

        return sonuclar;
    }

    public async Task<SmsGonderimSonuc> SablonlaGonderAsync(string telefon, int sablonId, 
        Dictionary<string, string> degiskenler, string? iliskiliTablo = null, int? iliskiliKayitId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sablon = await GetSablonByIdAsync(sablonId);
        if (sablon == null)
        {
            return new SmsGonderimSonuc
            {
                Basarili = false,
                HataMesaji = "SMS şablonu bulunamadı"
            };
        }

        // Değişkenleri değiştir
        var mesaj = sablon.Sablon;
        foreach (var degisken in degiskenler)
        {
            mesaj = mesaj.Replace($"{{{degisken.Key}}}", degisken.Value);
        }

        return await GonderAsync(telefon, mesaj, sablon.Tip, iliskiliTablo, iliskiliKayitId);
    }

    #endregion

    #region SMS Logları

    public async Task<List<SmsLog>> GetLoglarAsync(DateTime? baslangic = null, DateTime? bitis = null, 
        SmsGonderimDurum? durum = null, int? limit = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<SmsLog>()
            .Include(l => l.SmsAyar)
            .Include(l => l.GonderenKullanici)
            .Where(l => !l.IsDeleted);

        if (baslangic.HasValue)
            query = query.Where(l => l.CreatedAt >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(l => l.CreatedAt <= bitis.Value);

        if (durum.HasValue)
            query = query.Where(l => l.Durum == durum.Value);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit ?? 100)
            .ToListAsync();
    }

    public async Task<SmsLog?> GetLogByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<SmsLog>()
            .Include(l => l.SmsAyar)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
    }

    public async Task<SmsIstatistik> GetIstatistikAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        baslangic ??= DateTime.Today.AddDays(-30);
        bitis ??= DateTime.Now;

        var loglar = await context.Set<SmsLog>()
            .Where(l => !l.IsDeleted && l.CreatedAt >= baslangic && l.CreatedAt <= bitis)
            .ToListAsync();

        var ayar = await GetAktifAyarAsync();

        var istatistik = new SmsIstatistik
        {
            ToplamGonderen = loglar.Count,
            Basarili = loglar.Count(l => l.Durum == SmsGonderimDurum.Gonderildi || l.Durum == SmsGonderimDurum.Iletildi),
            Basarisiz = loglar.Count(l => l.Durum == SmsGonderimDurum.Basarisiz),
            Bekleyen = loglar.Count(l => l.Durum == SmsGonderimDurum.Bekliyor),
            KalanBakiye = ayar?.Bakiye,
            TipeBazliSayilar = loglar
                .GroupBy(l => l.Tip)
                .ToDictionary(g => g.Key, g => g.Count()),
            GunlukGonderim = loglar
                .Where(l => l.CreatedAt >= DateTime.Today.AddDays(-7))
                .GroupBy(l => l.CreatedAt.Date.ToString("dd.MM"))
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return istatistik;
    }

    #endregion

    #region SMS Şablonları

    public async Task<List<SmsSablon>> GetSablonlarAsync(SmsTipi? tip = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<SmsSablon>()
            .Where(s => !s.IsDeleted);

        if (tip.HasValue)
            query = query.Where(s => s.Tip == tip.Value);

        return await query
            .OrderBy(s => s.Tip)
            .ThenByDescending(s => s.Varsayilan)
            .ThenBy(s => s.Adi)
            .ToListAsync();
    }

    public async Task<SmsSablon?> GetSablonByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<SmsSablon>()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<SmsSablon> SaveSablonAsync(SmsSablon sablon)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (sablon.Varsayilan)
        {
            // Aynı tipteki diğer varsayılanları kaldır
            var digerVarsayilanlar = await context.Set<SmsSablon>()
                .Where(s => !s.IsDeleted && s.Tip == sablon.Tip && s.Varsayilan && s.Id != sablon.Id)
                .ToListAsync();

            foreach (var diger in digerVarsayilanlar)
            {
                diger.Varsayilan = false;
            }
        }

        if (sablon.Id == 0)
        {
            sablon.CreatedAt = DateTime.Now;
            context.Set<SmsSablon>().Add(sablon);
        }
        else
        {
            sablon.UpdatedAt = DateTime.Now;
            context.Set<SmsSablon>().Update(sablon);
        }

        await context.SaveChangesAsync();
        return sablon;
    }

    public async Task DeleteSablonAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sablon = await context.Set<SmsSablon>().FindAsync(id);
        if (sablon != null)
        {
            sablon.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<SmsSablon?> GetVarsayilanSablonAsync(SmsTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<SmsSablon>()
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Tip == tip && s.Varsayilan && s.Aktif);
    }

    #endregion

    #region Bildirim Entegrasyonu

    public async Task<int> BildirimSmsGonderAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var gonderilen = 0;

        // SMS bildirimi alacak kullanıcıları bul (BildirimAyar'da SMS açık olanlar)
        // Bu metod BildirimService'ten çağrılabilir
        
        _logger.LogInformation("Bildirim SMS'leri gönderildi: {Count}", gonderilen);
        return gonderilen;
    }

    #endregion

    #region Provider Implementasyonları

    // === NETGSM ===
    private async Task<SmsGonderimSonuc> NetGsmGonderAsync(ApplicationDbContext context, SmsAyar ayar, string telefon, string mesaj)
    {
        var client = _httpClientFactory.CreateClient();
        
        var parameters = new Dictionary<string, string>
        {
            { "usercode", ayar.KullaniciAdi ?? "" },
            { "password", ayar.ApiKey ?? "" },
            { "gsmno", telefon },
            { "message", mesaj },
            { "msgheader", ayar.GondericiNumara ?? "" },
            { "filter", "0" },
            { "startdate", "" },
            { "stopdate", "" }
        };

        var url = ayar.ApiUrl ?? "https://api.netgsm.com.tr/sms/send/get";
        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));

        var response = await client.GetStringAsync($"{url}?{queryString}");

        // NetGSM yanıt formatı: "00 MESAJ_ID" (başarılı) veya hata kodu
        if (response.StartsWith("00"))
        {
            var parts = response.Split(' ');
            return new SmsGonderimSonuc
            {
                Basarili = true,
                MesajId = parts.Length > 1 ? parts[1] : null
            };
        }

        return new SmsGonderimSonuc
        {
            Basarili = false,
            HataMesaji = NetGsmHataKoduAciklama(response)
        };
    }

    private async Task<decimal?> NetGsmBakiyeSorgulaAsync(ApplicationDbContext context, SmsAyar ayar)
    {
        var client = _httpClientFactory.CreateClient();
        
        var url = $"https://api.netgsm.com.tr/balance/list/get?usercode={ayar.KullaniciAdi}&password={ayar.ApiKey}&stip=2";
        var response = await client.GetStringAsync(url);

        // Yanıt formatı: bakiye değeri veya hata kodu
        if (decimal.TryParse(response.Trim(), out var bakiye))
        {
            return bakiye;
        }

        return null;
    }

    private string NetGsmHataKoduAciklama(string kod)
    {
        return kod switch
        {
            "20" => "Mesaj metninde hata var",
            "30" => "Geçersiz kullanıcı adı/şifre",
            "40" => "Başlık (msgheader) sistemde tanımlı değil",
            "50" => "Abone hesabı aktif değil",
            "51" => "Abone hesabı işlemlere kapalı",
            "60" => "Gönderim yapacak bakiye yok",
            "70" => "Hatalı sorgulama",
            "80" => "Sisteme SMS gönderilemedi",
            "85" => "Mükerrer gönderim yapılamaz",
            _ => $"Bilinmeyen hata: {kod}"
        };
    }

    // === İLETİMERKEZİ ===
    private async Task<SmsGonderimSonuc> IletimerkeziGonderAsync(ApplicationDbContext context, SmsAyar ayar, string telefon, string mesaj)
    {
        var client = _httpClientFactory.CreateClient();

        var requestBody = new
        {
            request = new
            {
                authentication = new
                {
                    key = ayar.ApiKey,
                    hash = "" // İletimerkezi hash hesaplaması gerekebilir
                },
                order = new
                {
                    sender = ayar.GondericiNumara,
                    sendDateTime = Array.Empty<object>(),
                    message = new
                    {
                        text = mesaj,
                        receipents = new
                        {
                            number = new[] { telefon }
                        }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(ayar.ApiUrl ?? "https://api.iletimerkezi.com/v1/send-sms/json", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // İletimerkezi yanıt parsing
        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var statusCode = doc.RootElement.GetProperty("response").GetProperty("status").GetProperty("code").GetInt32();
            
            if (statusCode == 200)
            {
                return new SmsGonderimSonuc { Basarili = true };
            }

            var statusMessage = doc.RootElement.GetProperty("response").GetProperty("status").GetProperty("message").GetString();
            return new SmsGonderimSonuc { Basarili = false, HataMesaji = statusMessage };
        }
        catch
        {
            return new SmsGonderimSonuc { Basarili = false, HataMesaji = "Yanıt parse edilemedi" };
        }
    }

    private async Task<decimal?> IletimerkeziBakiyeSorgulaAsync(ApplicationDbContext context, SmsAyar ayar)
    {
        // İletimerkezi bakiye sorgulama API'si
        return null;
    }

    // === MUTLUCELL ===
    private async Task<SmsGonderimSonuc> MutlucellGonderAsync(ApplicationDbContext context, SmsAyar ayar, string telefon, string mesaj)
    {
        var client = _httpClientFactory.CreateClient();

        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<smspack ka=""{ayar.KullaniciAdi}"" pwd=""{ayar.ApiKey}"" org=""{ayar.GondericiNumara}"">
    <mesaj>
        <metin>{HttpUtility.HtmlEncode(mesaj)}</metin>
        <nums>{telefon}</nums>
    </mesaj>
</smspack>";

        var content = new StringContent(xml, Encoding.UTF8, "application/xml");
        var response = await client.PostAsync(ayar.ApiUrl ?? "https://smsgw.mutlucell.com/smsgw-ws/sndblkex", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Mutlucell yanıt: "$" ile başlıyorsa başarılı
        if (responseContent.StartsWith("$"))
        {
            return new SmsGonderimSonuc { Basarili = true, MesajId = responseContent };
        }

        return new SmsGonderimSonuc { Basarili = false, HataMesaji = MutlucellHataAciklama(responseContent) };
    }

    private async Task<decimal?> MutlucellBakiyeSorgulaAsync(ApplicationDbContext context, SmsAyar ayar)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://smsgw.mutlucell.com/smsgw-ws/gtcrdtex?ka={ayar.KullaniciAdi}&pwd={ayar.ApiKey}";
        var response = await client.GetStringAsync(url);

        if (decimal.TryParse(response.Trim(), out var bakiye))
        {
            return bakiye;
        }
        return null;
    }

    private string MutlucellHataAciklama(string kod)
    {
        return kod switch
        {
            "20" => "Post edilen XML eksik veya hatalı",
            "21" => "Kullanıcı adı ya da şifre hatalı",
            "22" => "Kullanıcı aktif değil",
            "23" => "Originator hatalı ya da tanımlı değil",
            "24" => "Mesaj metni boş",
            "25" => "Gönderilecek numara yok",
            _ => $"Bilinmeyen hata: {kod}"
        };
    }

    // === TWILIO ===
    private async Task<SmsGonderimSonuc> TwilioGonderAsync(ApplicationDbContext context, SmsAyar ayar, string telefon, string mesaj)
    {
        var client = _httpClientFactory.CreateClient();
        
        // Twilio format: KullaniciAdi = Account SID, ApiKey = Auth Token
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ayar.KullaniciAdi}:{ayar.ApiKey}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", telefon.StartsWith("+") ? telefon : $"+90{telefon}"),
            new KeyValuePair<string, string>("From", ayar.GondericiNumara ?? ""),
            new KeyValuePair<string, string>("Body", mesaj)
        });

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{ayar.KullaniciAdi}/Messages.json";
        var response = await client.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(responseContent);
            var sid = doc.RootElement.GetProperty("sid").GetString();
            return new SmsGonderimSonuc { Basarili = true, MesajId = sid };
        }

        return new SmsGonderimSonuc { Basarili = false, HataMesaji = responseContent };
    }

    #endregion

    #region Yardımcı Metodlar

    private string FormatTelefon(string telefon)
    {
        if (string.IsNullOrWhiteSpace(telefon))
            return string.Empty;

        // Sadece rakamları al
        var rakamlar = new string(telefon.Where(char.IsDigit).ToArray());

        // Türkiye formatı kontrolü
        if (rakamlar.StartsWith("90") && rakamlar.Length == 12)
        {
            return rakamlar; // 905xxxxxxxxx formatında
        }
        else if (rakamlar.StartsWith("0") && rakamlar.Length == 11)
        {
            return "9" + rakamlar; // 05xxxxxxxxx -> 905xxxxxxxxx
        }
        else if (rakamlar.Length == 10 && rakamlar.StartsWith("5"))
        {
            return "90" + rakamlar; // 5xxxxxxxxx -> 905xxxxxxxxx
        }

        // Diğer formatlar için olduğu gibi döndür
        return rakamlar;
    }

    #endregion
}


