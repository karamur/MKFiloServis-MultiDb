using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Luca Portal entegrasyon servisi
/// E-Fatura ve E-Arsiv belgeleri icin web scraping ve API kullanimi
/// </summary>
public class LucaPortalService : ILucaPortalService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LucaPortalService> _logger;
    private readonly IFirmaService _firmaService;
    private readonly IWebHostEnvironment _environment;
    
    private static readonly SemaphoreSlim SettingsFileLock = new(1, 1);
    private LucaPortalSettings? _cachedSettings;
    private HttpClient? _authenticatedClient;
    private readonly IDataProtector _sifreProtector;

    private const string DEFAULT_PORTAL_URL = "https://edonusum.lfrms.com.tr";
    /// <summary>Sifreli olarak saklanan portal parolasının dosyadaki öneki.</summary>
    private const string SIFRE_KORUMA_ONEKI = "dp:v1:";

    public LucaPortalService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<LucaPortalService> logger,
        IFirmaService firmaService,
        IWebHostEnvironment environment,
        IDataProtectionProvider dataProtection)
    {
        _contextFactory = contextFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _firmaService = firmaService;
        _environment = environment;
        _sifreProtector = dataProtection.CreateProtector("MKFiloServis.LucaPortal.Credentials.v1");
    }

    #region Ayarlar

    public async Task<LucaPortalSettings?> GetAyarlarAsync(int? firmaId = null)
    {
        try
        {
            // Firma ID belirtilmemisse aktif firmayi al
            if (!firmaId.HasValue)
            {
                var aktifFirma = _firmaService.GetAktifFirma();
                firmaId = aktifFirma?.FirmaId > 0 ? aktifFirma.FirmaId : (int?)null;
            }

            // Once json dosyasindan oku
            var ayarlar = await AyarlariDosyadanOkuAsync(firmaId);

            if (ayarlar != null)
            {
                _cachedSettings = ayarlar;
                return ayarlar;
            }

            // Varsayilan ayarlar dondur
            return new LucaPortalSettings
            {
                FirmaId = firmaId,
                PortalUrl = DEFAULT_PORTAL_URL
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca Portal ayarlari okunamadi");
            return new LucaPortalSettings { PortalUrl = DEFAULT_PORTAL_URL };
        }
    }

    public async Task<bool> AyarKaydetAsync(LucaPortalSettings ayarlar)
    {
        try
        {
            ayarlar.GuncellemeTarihi = DateTime.UtcNow;
            
            if (ayarlar.Id == 0)
            {
                ayarlar.OlusturmaTarihi = DateTime.UtcNow;
            }
            
            // JSON dosyasina kaydet
            await AyarlariDosyayaKaydetAsync(ayarlar);
            
            _cachedSettings = ayarlar;
            _logger.LogInformation("Luca Portal ayarlari kaydedildi. FirmaId: {FirmaId}", ayarlar.FirmaId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca Portal ayarlari kaydedilemedi");
            return false;
        }
    }

    private async Task<LucaPortalSettings?> AyarlariDosyadanOkuAsync(int? firmaId)
    {
        await SettingsFileLock.WaitAsync();
        try
        {
            var dosyaYolu = GetAyarDosyaYolu(firmaId);
            
            if (!File.Exists(dosyaYolu))
                return null;
            
            var json = await File.ReadAllTextAsync(dosyaYolu);
            var ayarlar = JsonSerializer.Deserialize<LucaPortalSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (ayarlar is not null)
            {
                var migrationNeeded = new[] { ayarlar.Sifre, ayarlar.AccessToken, ayarlar.RefreshToken }
                    .Any(value => !string.IsNullOrEmpty(value) && !value.StartsWith(SIFRE_KORUMA_ONEKI, StringComparison.Ordinal));
                ayarlar.Sifre = GizliDegeriCoz(ayarlar.Sifre);
                ayarlar.AccessToken = GizliDegeriCoz(ayarlar.AccessToken);
                ayarlar.RefreshToken = GizliDegeriCoz(ayarlar.RefreshToken);
                if (migrationNeeded) await AyarlariDosyayaKaydetCoreAsync(ayarlar);
            }

            return ayarlar;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Luca Portal ayar dosyasi okunamadi");
            return null;
        }
        finally { SettingsFileLock.Release(); }
    }

    /// <summary>Dosyada şifreli saklanan parolayı bellekte açık metne çevirir.</summary>
    private string GizliDegeriCoz(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (!value.StartsWith(SIFRE_KORUMA_ONEKI, StringComparison.Ordinal)) return value;
        // Çözülemeyen şifreli kaydı boş bir parola ile ezme.
        return _sifreProtector.Unprotect(value[SIFRE_KORUMA_ONEKI.Length..]);
    }

    /// <summary>Parolayı diske yazmadan önce DataProtection ile şifreler.</summary>
    private async Task AyarlariDosyayaKaydetAsync(LucaPortalSettings ayarlar)
    {
        await SettingsFileLock.WaitAsync();
        try { await AyarlariDosyayaKaydetCoreAsync(ayarlar); }
        finally { SettingsFileLock.Release(); }
    }

    private async Task AyarlariDosyayaKaydetCoreAsync(LucaPortalSettings ayarlar)
    {
        var dosyaYolu = GetAyarDosyaYolu(ayarlar.FirmaId);
        var klasor = Path.GetDirectoryName(dosyaYolu);
        
        if (!string.IsNullOrEmpty(klasor) && !Directory.Exists(klasor))
        {
            Directory.CreateDirectory(klasor);
        }

        // Bellekteki nesneyi değiştirmeden, yalnız diske yazılacak kopyayı şifreliyoruz.
        var diskeYazilacak = new LucaPortalSettings
        {
            Id = ayarlar.Id,
            FirmaId = ayarlar.FirmaId,
            PortalUrl = ayarlar.PortalUrl,
            KullaniciAdi = ayarlar.KullaniciAdi,
            Sifre = SifreyiSifrele(ayarlar.Sifre),
            LucaFirmaKodu = ayarlar.LucaFirmaKodu,
            OtomatikSenkron = ayarlar.OtomatikSenkron,
            SenkronAralikSaat = ayarlar.SenkronAralikSaat,
            AccessToken = SifreyiSifrele(ayarlar.AccessToken ?? string.Empty),
            RefreshToken = SifreyiSifrele(ayarlar.RefreshToken ?? string.Empty),
            TokenGecerlilikTarihi = ayarlar.TokenGecerlilikTarihi,
            SonSenkronTarihi = ayarlar.SonSenkronTarihi,
            OlusturmaTarihi = ayarlar.OlusturmaTarihi,
            GuncellemeTarihi = ayarlar.GuncellemeTarihi
        };

        var json = JsonSerializer.Serialize(diskeYazilacak, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        var temp = dosyaYolu + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temp, json);
            File.Move(temp, dosyaYolu, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private string SifreyiSifrele(string sifre)
    {
        if (string.IsNullOrEmpty(sifre))
            return sifre;

        // Zaten şifrelenmiş bir değer geldiyse (ör. ayar dosyası geri yüklenmişse) tekrar sarmalama.
        if (sifre.StartsWith(SIFRE_KORUMA_ONEKI, StringComparison.Ordinal))
            return sifre;

        return SIFRE_KORUMA_ONEKI + _sifreProtector.Protect(sifre);
    }

    private string GetAyarDosyaYolu(int? firmaId)
    {
        var dataKlasor = Path.Combine(_environment.ContentRootPath, "Data", "LucaSettings");
        var dosyaAdi = firmaId.HasValue ? $"lucasettings_{firmaId}.json" : "lucasettings.json";
        return Path.Combine(dataKlasor, dosyaAdi);
    }

    #endregion

    #region Kimlik Dogrulama

    public async Task<LucaLoginSonuc> GirisYapAsync(string kullaniciAdi, string sifre)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            ConfigureHttpClient(client);
            
            var ayarlar = await GetAyarlarAsync();
            var portalUrl = ayarlar?.PortalUrl ?? DEFAULT_PORTAL_URL;

            if (!PortalUrlGuvenliMi(portalUrl, "GirisYapAsync"))
            {
                return new LucaLoginSonuc
                {
                    Basarili = false,
                    HataMesaji = "Portal adresi HTTPS olmadigi icin giris yapilmadi."
                };
            }

            _logger.LogInformation("Luca Portal'a giris yapiliyor: {Url}", portalUrl);
            
            // 1. Login sayfasina git ve CSRF token al
            var loginPageResponse = await client.GetAsync($"{portalUrl}/Account/Login");
            if (!loginPageResponse.IsSuccessStatusCode)
            {
                return new LucaLoginSonuc
                {
                    Basarili = false,
                    HataMesaji = "Login sayfasina erisilemedi"
                };
            }
            
            var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();
            var csrfToken = ExtractCsrfToken(loginPageHtml);
            
            // 2. Login POST istegi
            var loginData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "UserName", kullaniciAdi },
                { "Password", sifre },
                { "__RequestVerificationToken", csrfToken ?? "" },
                { "RememberMe", "true" }
            });
            
            var loginResponse = await client.PostAsync($"{portalUrl}/Account/Login", loginData);
            
            // Cookie'leri kontrol et
            var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
            var hasAuthCookie = cookies.Any(c => c.Contains(".AspNetCore.Cookies") || c.Contains("auth"));
            
            if (loginResponse.IsSuccessStatusCode || loginResponse.StatusCode == System.Net.HttpStatusCode.Redirect)
            {
                // Basarili giris - firma bilgilerini al
                var dashboardResponse = await client.GetAsync($"{portalUrl}/Dashboard");
                var dashboardHtml = await dashboardResponse.Content.ReadAsStringAsync();
                
                var firmaKodu = ExtractFirmaKodu(dashboardHtml);
                var firmaUnvan = ExtractFirmaUnvan(dashboardHtml);
                
                // Ayarlari guncelle
                if (ayarlar != null)
                {
                    ayarlar.KullaniciAdi = kullaniciAdi;
                    ayarlar.Sifre = sifre; // Bellekte açık metin; diske yazarken SifreyiSifrele() ile şifrelenir.
                    ayarlar.LucaFirmaKodu = firmaKodu;
                    ayarlar.AccessToken = Guid.NewGuid().ToString(); // Session token olarak kullan
                    ayarlar.TokenGecerlilikTarihi = DateTime.UtcNow.AddHours(8);
                    await AyarKaydetAsync(ayarlar);
                }
                
                _authenticatedClient = client;
                
                _logger.LogInformation("Luca Portal giris basarili. Firma: {Firma}", firmaUnvan);
                
                return new LucaLoginSonuc
                {
                    Basarili = true,
                    AccessToken = ayarlar?.AccessToken,
                    TokenGecerlilikTarihi = ayarlar?.TokenGecerlilikTarihi,
                    FirmaKodu = firmaKodu,
                    FirmaUnvan = firmaUnvan
                };
            }
            
            // Hata mesajini HTML'den cek
            var hataMesaji = ExtractHataMesaji(await loginResponse.Content.ReadAsStringAsync());
            
            return new LucaLoginSonuc
            {
                Basarili = false,
                HataMesaji = hataMesaji ?? "Giris basarisiz. Kullanici adi veya sifre hatali."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca Portal giris hatasi");
            return new LucaLoginSonuc
            {
                Basarili = false,
                HataMesaji = $"Baglanti hatasi: {ex.Message}"
            };
        }
    }

    public async Task<LucaLoginSonuc> TokenYenileAsync(string refreshToken)
    {
        var ayarlar = await GetAyarlarAsync();
        
        if (ayarlar == null || string.IsNullOrEmpty(ayarlar.KullaniciAdi))
        {
            return new LucaLoginSonuc
            {
                Basarili = false,
                HataMesaji = "Kayitli kullanici bilgisi bulunamadi"
            };
        }
        
        // Yeniden giris yap
        return await GirisYapAsync(ayarlar.KullaniciAdi, ayarlar.Sifre);
    }

    public async Task<bool> CikisYapAsync()
    {
        try
        {
            var ayarlar = await GetAyarlarAsync();
            
            if (ayarlar != null)
            {
                ayarlar.AccessToken = null;
                ayarlar.RefreshToken = null;
                ayarlar.TokenGecerlilikTarihi = null;
                await AyarKaydetAsync(ayarlar);
            }
            
            _authenticatedClient?.Dispose();
            _authenticatedClient = null;
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca Portal cikis hatasi");
            return false;
        }
    }

    public async Task<bool> BaglantiTestiAsync()
    {
        try
        {
            var ayarlar = await GetAyarlarAsync();
            
            if (ayarlar == null || string.IsNullOrEmpty(ayarlar.KullaniciAdi))
            {
                return false;
            }
            
            // Token gecerli mi kontrol et
            if (ayarlar.TokenGecerliMi && _authenticatedClient != null)
            {
                // Basit bir istek yap
                var portalUrl = ayarlar.PortalUrl ?? DEFAULT_PORTAL_URL;
                var response = await _authenticatedClient.GetAsync($"{portalUrl}/Dashboard");
                return response.IsSuccessStatusCode;
            }
            
            // Yeniden giris yap
            var loginSonuc = await GirisYapAsync(ayarlar.KullaniciAdi, ayarlar.Sifre);
            return loginSonuc.Basarili;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luca Portal baglanti testi hatasi");
            return false;
        }
    }

    #endregion

    #region Belge Sorgulama

    public async Task<LucaSorguSonuc> EFaturaListeleAsync(LucaSorguFiltre filtre)
    {
        return await BelgeListeleAsync(filtre, LucaBelgeTipi.EFatura);
    }

    public async Task<LucaSorguSonuc> EArsivListeleAsync(LucaSorguFiltre filtre)
    {
        return await BelgeListeleAsync(filtre, LucaBelgeTipi.EArsiv);
    }

    private async Task<LucaSorguSonuc> BelgeListeleAsync(LucaSorguFiltre filtre, LucaBelgeTipi belgeTipi)
    {
        var sonuc = new LucaSorguSonuc
        {
            Sayfa = filtre.Sayfa,
            SayfaBoyutu = filtre.SayfaBoyutu
        };
        
        try
        {
            // Baglanti kontrolu
            if (!await EnsureAuthenticatedAsync())
            {
                _logger.LogWarning("Luca Portal'a baglanilamadi");
                return sonuc;
            }
            
            var ayarlar = await GetAyarlarAsync();
            var portalUrl = ayarlar?.PortalUrl ?? DEFAULT_PORTAL_URL;
            
            // Belge listesi sayfasini al
            var listePath = belgeTipi == LucaBelgeTipi.EFatura
                ? (filtre.BelgeYonu == LucaBelgeYonu.Gelen ? "/EFatura/GelenFaturalar" : "/EFatura/GidenFaturalar")
                : (filtre.BelgeYonu == LucaBelgeYonu.Gelen ? "/EArsiv/GelenBelgeler" : "/EArsiv/GidenBelgeler");
            
            var queryParams = new List<string>
            {
                $"baslangicTarihi={filtre.BaslangicTarihi:yyyy-MM-dd}",
                $"bitisTarihi={filtre.BitisTarihi:yyyy-MM-dd}",
                $"sayfa={filtre.Sayfa}",
                $"sayfaBoyutu={filtre.SayfaBoyutu}"
            };
            
            if (!string.IsNullOrEmpty(filtre.VknArama))
                queryParams.Add($"vkn={filtre.VknArama}");
            
            if (!string.IsNullOrEmpty(filtre.FaturaNoArama))
                queryParams.Add($"faturaNo={filtre.FaturaNoArama}");
            
            var url = $"{portalUrl}{listePath}?{string.Join("&", queryParams)}";
            
            var response = await _authenticatedClient!.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Belge listesi alinamadi. Status: {Status}", response.StatusCode);
                return sonuc;
            }
            
            var html = await response.Content.ReadAsStringAsync();
            
            // HTML'den belgeleri parse et
            sonuc.Belgeler = ParseBelgeListesi(html, belgeTipi, filtre.BelgeYonu ?? LucaBelgeYonu.Gelen);
            sonuc.ToplamKayit = ExtractToplamKayit(html);
            
            _logger.LogInformation("{Tip} listesi alindi. Toplam: {Toplam}, Sayfa: {Sayfa}", 
                belgeTipi, sonuc.ToplamKayit, sonuc.Sayfa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Belge listesi alinamadi");
        }
        
        return sonuc;
    }

    public async Task<LucaBelge?> BelgeDetayGetirAsync(string belgeId, LucaBelgeTipi belgeTipi)
    {
        try
        {
            if (belgeTipi is not (LucaBelgeTipi.EFatura or LucaBelgeTipi.EArsiv))
                throw new NotSupportedException("Detay aktarımı yalnız e-Fatura ve e-Arşiv UBL belgelerini destekler.");
            var downloaded = await XmlIndirAsync(belgeId, belgeTipi);
            if (!downloaded.Basarili || downloaded.Icerik is not { Length: > 0 })
                throw new InvalidOperationException(downloaded.HataMesaji ?? "Belge XML içeriği alınamadı.");
            var firma = _firmaService.GetAktifFirma();
            if (firma?.FirmaId is not > 0) throw new InvalidOperationException("Aktif firma seçilmelidir.");
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vkn = await context.Firmalar.Where(f => f.Id == firma.FirmaId)
                .Select(f => f.VergiNo).FirstOrDefaultAsync();
            return ParseBelgeDetay(downloaded.Icerik, belgeTipi, belgeId, vkn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Belge detayi alinamadi. BelgeId: {BelgeId}", belgeId);
            return null;
        }
    }

    #endregion

    #region Belge Indirme

    public async Task<LucaBelgeIndirmeSonuc> XmlIndirAsync(string belgeId, LucaBelgeTipi belgeTipi)
    {
        return await BelgeIndirAsync(belgeId, belgeTipi, "xml");
    }

    public async Task<LucaBelgeIndirmeSonuc> PdfIndirAsync(string belgeId, LucaBelgeTipi belgeTipi)
    {
        return await BelgeIndirAsync(belgeId, belgeTipi, "pdf");
    }

    private async Task<LucaBelgeIndirmeSonuc> BelgeIndirAsync(string belgeId, LucaBelgeTipi belgeTipi, string format)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
            {
                return new LucaBelgeIndirmeSonuc
                {
                    Basarili = false,
                    HataMesaji = "Portal baglantisi kurulamadi"
                };
            }
            
            var ayarlar = await GetAyarlarAsync();
            var portalUrl = ayarlar?.PortalUrl ?? DEFAULT_PORTAL_URL;
            
            var indirPath = belgeTipi == LucaBelgeTipi.EFatura
                ? $"/EFatura/Indir/{belgeId}/{format}"
                : $"/EArsiv/Indir/{belgeId}/{format}";
            
            var response = await _authenticatedClient!.GetAsync($"{portalUrl}{indirPath}");
            
            if (!response.IsSuccessStatusCode)
            {
                return new LucaBelgeIndirmeSonuc
                {
                    Basarili = false,
                    HataMesaji = $"Indirme hatasi: {response.StatusCode}"
                };
            }
            
            var content = await response.Content.ReadAsByteArrayAsync();
            var contentDisposition = response.Content.Headers.ContentDisposition;
            var dosyaAdi = contentDisposition?.FileName?.Trim('"') ?? $"{belgeId}.{format}";
            
            return new LucaBelgeIndirmeSonuc
            {
                Basarili = true,
                Icerik = content,
                DosyaAdi = dosyaAdi,
                ContentType = response.Content.Headers.ContentType?.MediaType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Belge indirilemedi. BelgeId: {BelgeId}", belgeId);
            return new LucaBelgeIndirmeSonuc
            {
                Basarili = false,
                HataMesaji = ex.Message
            };
        }
    }

    public async Task<List<LucaBelgeIndirmeSonuc>> TopluXmlIndirAsync(List<string> belgeIdler, LucaBelgeTipi belgeTipi)
    {
        var sonuclar = new List<LucaBelgeIndirmeSonuc>();
        
        foreach (var belgeId in belgeIdler)
        {
            var sonuc = await XmlIndirAsync(belgeId, belgeTipi);
            sonuclar.Add(sonuc);
            
            // Rate limiting
            await Task.Delay(200);
        }
        
        return sonuclar;
    }

    public async Task<List<LucaBelgeIndirmeSonuc>> TopluPdfIndirAsync(List<string> belgeIdler, LucaBelgeTipi belgeTipi)
    {
        var sonuclar = new List<LucaBelgeIndirmeSonuc>();
        
        foreach (var belgeId in belgeIdler)
        {
            var sonuc = await PdfIndirAsync(belgeId, belgeTipi);
            sonuclar.Add(sonuc);
            
            // Rate limiting
            await Task.Delay(200);
        }
        
        return sonuclar;
    }

    #endregion

    #region Sisteme Aktarma

    public async Task<int> BelgeleriSistemeAktarAsync(List<LucaBelge> belgeler, bool xmlIndir = true, bool pdfIndir = true)
    {
        var aktarilanSayi = 0;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var aktifFirma = _firmaService.GetAktifFirma();

            if (aktifFirma == null || aktifFirma.FirmaId == 0)
            {
                _logger.LogWarning("Aktif firma bulunamadi");
                return 0;
            }

            // Dosya kayit klasoru
            var belgelerKlasor = Path.Combine(_environment.ContentRootPath, "wwwroot", "belgeler", "efatura");
            if (!Directory.Exists(belgelerKlasor))
            {
                Directory.CreateDirectory(belgelerKlasor);
            }

            foreach (var belge in belgeler)
            {
                try
                {
                    // ETTN ile mevcut fatura kontrolu
                    var mevcutFatura = await context.Faturalar
                        .FirstOrDefaultAsync(f => f.EttnNo == belge.EttnNo && f.FirmaId == aktifFirma.FirmaId);

                    if (mevcutFatura != null)
                    {
                        _logger.LogDebug("Belge zaten mevcut: {ETTN}", belge.EttnNo);
                        continue;
                    }

                    // Cari bul veya olustur
                    var vkn = belge.BelgeYonu == LucaBelgeYonu.Gelen ? belge.GondericiVkn : belge.AliciVkn;
                    var unvan = belge.BelgeYonu == LucaBelgeYonu.Gelen ? belge.GondericiUnvan : belge.AliciUnvan;

                    var cari = await context.Cariler
                        .FirstOrDefaultAsync(c => c.VergiNo == vkn && c.FirmaId == aktifFirma.FirmaId);

                    if (cari == null)
                    {
                        cari = new Cari
                        {
                            Unvan = unvan,
                            VergiNo = vkn,
                            CariTipi = belge.BelgeYonu == LucaBelgeYonu.Gelen ? CariTipi.Tedarikci : CariTipi.Musteri,
                            FirmaId = aktifFirma.FirmaId,
                            CreatedAt = DateTime.UtcNow
                        };
                        context.Cariler.Add(cari);
                        await context.SaveChangesAsync();
                    }

                    // Fatura olustur
                    var fatura = new Fatura
                    {
                        FaturaNo = belge.FaturaNo,
                        FaturaTarihi = belge.BelgeTarihi,
                        FaturaTipi = belge.BelgeYonu == LucaBelgeYonu.Gelen ? FaturaTipi.AlisFaturasi : FaturaTipi.SatisFaturasi,
                        FaturaYonu = belge.BelgeYonu == LucaBelgeYonu.Gelen ? FaturaYonu.Gelen : FaturaYonu.Giden,
                        EFaturaTipi = belge.BelgeTipi == LucaBelgeTipi.EFatura ? EFaturaTipi.EFatura : EFaturaTipi.EArsiv,
                        EttnNo = belge.EttnNo,
                        AraToplam = belge.AraToplam,
                        KdvTutar = belge.KdvToplam,
                        GenelToplam = belge.GenelToplam,
                        CariId = cari.Id,
                        FirmaId = aktifFirma.FirmaId,
                        ImportKaynak = "Luca",
                        Durum = FaturaDurum.Beklemede,
                        CreatedAt = DateTime.UtcNow
                    };

                    // XML ve PDF indir
                    if (xmlIndir && belge.XmlMevcut)
                    {
                        var xmlSonuc = await XmlIndirAsync(belge.BelgeId, belge.BelgeTipi);
                        if (xmlSonuc.Basarili && xmlSonuc.Icerik != null)
                        {
                            var xmlDosya = Path.Combine(belgelerKlasor, $"{belge.EttnNo}.xml");
                            await File.WriteAllBytesAsync(xmlDosya, xmlSonuc.Icerik);
                            fatura.XmlDosyaYolu = $"/belgeler/efatura/{belge.EttnNo}.xml";
                        }
                    }

                    if (pdfIndir && belge.PdfMevcut)
                    {
                        var pdfSonuc = await PdfIndirAsync(belge.BelgeId, belge.BelgeTipi);
                        if (pdfSonuc.Basarili && pdfSonuc.Icerik != null)
                        {
                            var pdfDosya = Path.Combine(belgelerKlasor, $"{belge.EttnNo}.pdf");
                            await File.WriteAllBytesAsync(pdfDosya, pdfSonuc.Icerik);
                            fatura.PdfDosyaYolu = $"/belgeler/efatura/{belge.EttnNo}.pdf";
                        }
                    }

                    context.Faturalar.Add(fatura);
                    await context.SaveChangesAsync();

                    aktarilanSayi++;
                    _logger.LogInformation("Belge aktarildi: {ETTN} - {FaturaNo}", belge.EttnNo, belge.FaturaNo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Belge aktarilamadi: {ETTN}", belge.EttnNo);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Belgeler sisteme aktarilamadi");
        }
        
        return aktarilanSayi;
    }

    public async Task<int> TumBelgeleriSenkronizeEtAsync(DateTime baslangic, DateTime bitis, IProgress<string>? progress = null)
    {
        var toplamAktarilan = 0;
        
        try
        {
            progress?.Report("Luca Portal'a baglaniliyor...");
            
            if (!await EnsureAuthenticatedAsync())
            {
                progress?.Report("Baglanti kurulamadi!");
                return 0;
            }
            
            var filtre = new LucaSorguFiltre
            {
                BaslangicTarihi = baslangic,
                BitisTarihi = bitis,
                SayfaBoyutu = 100
            };
            
            // E-Fatura Gelen
            progress?.Report("E-Fatura gelen belgeler aliniyor...");
            filtre.BelgeYonu = LucaBelgeYonu.Gelen;
            var gelenEFatura = await EFaturaListeleAsync(filtre);
            if (gelenEFatura.Belgeler.Any())
            {
                var aktarilan = await BelgeleriSistemeAktarAsync(gelenEFatura.Belgeler);
                toplamAktarilan += aktarilan;
                progress?.Report($"E-Fatura gelen: {aktarilan} belge aktarildi");
            }
            
            // E-Fatura Giden
            progress?.Report("E-Fatura giden belgeler aliniyor...");
            filtre.BelgeYonu = LucaBelgeYonu.Giden;
            var gidenEFatura = await EFaturaListeleAsync(filtre);
            if (gidenEFatura.Belgeler.Any())
            {
                var aktarilan = await BelgeleriSistemeAktarAsync(gidenEFatura.Belgeler);
                toplamAktarilan += aktarilan;
                progress?.Report($"E-Fatura giden: {aktarilan} belge aktarildi");
            }
            
            // E-Arsiv Gelen
            progress?.Report("E-Arsiv gelen belgeler aliniyor...");
            filtre.BelgeYonu = LucaBelgeYonu.Gelen;
            var gelenEArsiv = await EArsivListeleAsync(filtre);
            if (gelenEArsiv.Belgeler.Any())
            {
                var aktarilan = await BelgeleriSistemeAktarAsync(gelenEArsiv.Belgeler);
                toplamAktarilan += aktarilan;
                progress?.Report($"E-Arsiv gelen: {aktarilan} belge aktarildi");
            }
            
            // E-Arsiv Giden
            progress?.Report("E-Arsiv giden belgeler aliniyor...");
            filtre.BelgeYonu = LucaBelgeYonu.Giden;
            var gidenEArsiv = await EArsivListeleAsync(filtre);
            if (gidenEArsiv.Belgeler.Any())
            {
                var aktarilan = await BelgeleriSistemeAktarAsync(gidenEArsiv.Belgeler);
                toplamAktarilan += aktarilan;
                progress?.Report($"E-Arsiv giden: {aktarilan} belge aktarildi");
            }
            
            // Son senkron tarihini guncelle
            var ayarlar = await GetAyarlarAsync();
            if (ayarlar != null)
            {
                ayarlar.SonSenkronTarihi = DateTime.UtcNow;
                await AyarKaydetAsync(ayarlar);
            }
            
            progress?.Report($"Senkronizasyon tamamlandi. Toplam {toplamAktarilan} belge aktarildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Senkronizasyon hatasi");
            progress?.Report($"Hata: {ex.Message}");
        }
        
        return toplamAktarilan;
    }

    #endregion

    #region Private Methods

    private void ConfigureHttpClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
    }

    /// <summary>
    /// Portal adresi ayarlardan gelir ve kullanıcı tarafından değiştirilebilir. Bu adrese portal
    /// kullanıcı adı/parolası POST edilir ve imzalı e-fatura belgeleri indirilir; düz HTTP'ye izin
    /// vermek kimlik bilgilerini ve mali belgeleri ağa açık eder. Sertifika doğrulaması .NET
    /// varsayılanında açıktır; burada yalnız şema HTTPS olarak zorlanır.
    /// </summary>
    private bool PortalUrlGuvenliMi(string portalUrl, string islem)
    {
        if (Uri.TryCreate(portalUrl, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        _logger.LogError(
            "Luca Portal adresi HTTPS degil; islem reddedildi. Islem: {Islem}, Url: {Url}",
            islem, portalUrl);
        return false;
    }

    private async Task<bool> EnsureAuthenticatedAsync()
    {
        var ayarlar = await GetAyarlarAsync();
        
        if (ayarlar == null || string.IsNullOrEmpty(ayarlar.KullaniciAdi))
        {
            return false;
        }

        // Belge listeleme/indirme gibi tüm oturumlu çağrılar bu noktadan geçer; düz HTTP'ye
        // izin verilirse imzalı mali belgeler ağ üzerinde açık taşınır.
        if (!PortalUrlGuvenliMi(ayarlar.PortalUrl ?? DEFAULT_PORTAL_URL, "EnsureAuthenticatedAsync"))
        {
            return false;
        }
        
        // Token gecerli ve client mevcut
        if (ayarlar.TokenGecerliMi && _authenticatedClient != null)
        {
            return true;
        }
        
        // Yeniden giris yap
        var loginSonuc = await GirisYapAsync(ayarlar.KullaniciAdi, ayarlar.Sifre);
        return loginSonuc.Basarili;
    }

    private string? ExtractCsrfToken(string html)
    {
        var match = Regex.Match(html, @"name=""__RequestVerificationToken""\s+value=""([^""]+)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    private string? ExtractFirmaKodu(string html)
    {
        var match = Regex.Match(html, @"data-firma-kodu=""([^""]+)""");
        if (!match.Success)
            match = Regex.Match(html, @"firmaKodu['""]?\s*[:=]\s*['""]?(\w+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private string? ExtractFirmaUnvan(string html)
    {
        var match = Regex.Match(html, @"<span[^>]*class=""[^""]*firma-unvan[^""]*""[^>]*>([^<]+)</span>");
        if (!match.Success)
            match = Regex.Match(html, @"Firma:\s*([^<\n]+)");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private string? ExtractHataMesaji(string html)
    {
        var match = Regex.Match(html, @"<div[^>]*class=""[^""]*alert-danger[^""]*""[^>]*>(.*?)</div>", RegexOptions.Singleline);
        if (match.Success)
        {
            return Regex.Replace(match.Groups[1].Value, "<[^>]+>", "").Trim();
        }
        return null;
    }

    private List<LucaBelge> ParseBelgeListesi(string html, LucaBelgeTipi belgeTipi, LucaBelgeYonu belgeYonu)
    {
        var belgeler = new List<LucaBelge>();
        
        try
        {
            // Tablo satirlarini bul
            var trPattern = @"<tr[^>]*data-id=""([^""]+)""[^>]*>(.*?)</tr>";
            var trMatches = Regex.Matches(html, trPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            foreach (Match trMatch in trMatches)
            {
                try
                {
                    var belgeId = trMatch.Groups[1].Value;
                    var trContent = trMatch.Groups[2].Value;
                    
                    var belge = new LucaBelge
                    {
                        BelgeId = belgeId,
                        BelgeTipi = belgeTipi,
                        BelgeYonu = belgeYonu
                    };
                    
                    // ETTN
                    var ettnMatch = Regex.Match(trContent, @"data-ettn=""([^""]+)""");
                    if (ettnMatch.Success) belge.EttnNo = ettnMatch.Groups[1].Value;
                    
                    // Fatura No
                    var faturaNoMatch = Regex.Match(trContent, @"<td[^>]*class=""[^""]*fatura-no[^""]*""[^>]*>([^<]+)</td>");
                    if (faturaNoMatch.Success) belge.FaturaNo = faturaNoMatch.Groups[1].Value.Trim();
                    
                    // Tarih
                    var tarihMatch = Regex.Match(trContent, @"<td[^>]*class=""[^""]*tarih[^""]*""[^>]*>(\d{2}\.\d{2}\.\d{4})</td>");
                    if (tarihMatch.Success && DateTime.TryParse(tarihMatch.Groups[1].Value, out var tarih))
                        belge.BelgeTarihi = tarih;
                    
                    // Gonderici/Alici VKN ve Unvan
                    var vknMatch = Regex.Match(trContent, @"<td[^>]*class=""[^""]*vkn[^""]*""[^>]*>(\d{10,11})</td>");
                    var unvanMatch = Regex.Match(trContent, @"<td[^>]*class=""[^""]*unvan[^""]*""[^>]*>([^<]+)</td>");
                    
                    if (belgeYonu == LucaBelgeYonu.Gelen)
                    {
                        if (vknMatch.Success) belge.GondericiVkn = vknMatch.Groups[1].Value;
                        if (unvanMatch.Success) belge.GondericiUnvan = unvanMatch.Groups[1].Value.Trim();
                    }
                    else
                    {
                        if (vknMatch.Success) belge.AliciVkn = vknMatch.Groups[1].Value;
                        if (unvanMatch.Success) belge.AliciUnvan = unvanMatch.Groups[1].Value.Trim();
                    }
                    
                    // Tutarlar
                    var tutarMatch = Regex.Match(trContent, @"<td[^>]*class=""[^""]*tutar[^""]*""[^>]*>([0-9.,]+)</td>");
                    if (tutarMatch.Success && decimal.TryParse(tutarMatch.Groups[1].Value.Replace(".", "").Replace(",", "."), out var tutar))
                        belge.GenelToplam = tutar;
                    
                    // Durum
                    var durumMatch = Regex.Match(trContent, @"<span[^>]*class=""[^""]*badge[^""]*""[^>]*>([^<]+)</span>");
                    if (durumMatch.Success) belge.Durum = durumMatch.Groups[1].Value.Trim();
                    
                    // XML/PDF varligi
                    belge.XmlMevcut = trContent.Contains("xml-indir") || trContent.Contains("download-xml");
                    belge.PdfMevcut = trContent.Contains("pdf-indir") || trContent.Contains("download-pdf");
                    
                    belgeler.Add(belge);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Belge satiri parse edilemedi");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Belge listesi parse edilemedi");
        }
        
        return belgeler;
    }

    private int ExtractToplamKayit(string html)
    {
        var match = Regex.Match(html, @"Toplam[:\s]*(\d+)\s*kay", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var toplam))
            return toplam;
        return 0;
    }

    private static LucaBelge ParseBelgeDetay(byte[] xml, LucaBelgeTipi belgeTipi, string belgeId, string? firmaVkn)
    {
        using var input = new MemoryStream(xml, writable: false);
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 20_000_000
        });
        var document = XDocument.Load(reader);
        XNamespace invoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        XNamespace basic = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        XNamespace aggregate = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        var root = document.Root;
        if (root?.Name != invoice + "Invoice") throw new InvalidDataException("Geçerli UBL Invoice belgesi bekleniyor.");
        string Required(XElement? element, string field) => !string.IsNullOrWhiteSpace(element?.Value)
            ? element.Value : throw new InvalidDataException($"UBL alanı eksik: {field}");
        decimal Amount(XElement? element, string field) => decimal.Parse(Required(element, field), NumberStyles.Number, CultureInfo.InvariantCulture);
        var supplier = root.Element(aggregate + "AccountingSupplierParty")?.Element(aggregate + "Party");
        var customer = root.Element(aggregate + "AccountingCustomerParty")?.Element(aggregate + "Party");
        string TaxId(XElement? party) => party?.Elements(aggregate + "PartyIdentification")
            .Select(p => p.Element(basic + "ID"))
            .FirstOrDefault(id => (string?)id?.Attribute("schemeID") is "VKN" or "TCKN")?.Value ?? "";
        string PartyName(XElement? party) => party?.Element(aggregate + "PartyName")?.Element(basic + "Name")?.Value
            ?? party?.Element(aggregate + "PartyLegalEntity")?.Element(basic + "RegistrationName")?.Value
            ?? string.Join(" ", new[] { party?.Element(aggregate + "Person")?.Element(basic + "FirstName")?.Value,
                party?.Element(aggregate + "Person")?.Element(basic + "FamilyName")?.Value }.Where(v => !string.IsNullOrWhiteSpace(v)));
        var sender = TaxId(supplier);
        var receiver = TaxId(customer);
        var ownTaxId = firmaVkn?.Trim();
        var direction = !string.IsNullOrEmpty(ownTaxId) && sender == ownTaxId ? LucaBelgeYonu.Giden
            : !string.IsNullOrEmpty(ownTaxId) && receiver == ownTaxId ? LucaBelgeYonu.Gelen
            : throw new InvalidDataException("Belgenin gönderici/alıcı VKN'si aktif firma ile eşleşmiyor.");
        var totals = root.Element(aggregate + "LegalMonetaryTotal");
        // LucaBelge para birimi taşımıyor; dövizli belgeyi TL tutarı gibi aktarma.
        if (Required(root.Element(basic + "DocumentCurrencyCode"), "DocumentCurrencyCode") != "TRY")
            throw new NotSupportedException("Dövizli UBL belgesi için para birimi destekli model gerekir.");
        var taxTotals = root.Elements(aggregate + "TaxTotal").ToList();
        if (taxTotals.Count == 0) throw new InvalidDataException("UBL TaxTotal alanı eksik.");
        return new LucaBelge
        {
            BelgeId = belgeId,
            EttnNo = Required(root.Element(basic + "UUID"), "UUID"),
            FaturaNo = Required(root.Element(basic + "ID"), "ID"),
            BelgeTarihi = DateTime.ParseExact(Required(root.Element(basic + "IssueDate"), "IssueDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            BelgeTipi = belgeTipi, BelgeYonu = direction,
            GondericiVkn = sender, GondericiUnvan = PartyName(supplier),
            AliciVkn = receiver, AliciUnvan = PartyName(customer),
            AraToplam = Amount(totals?.Element(basic + "LineExtensionAmount"), "LineExtensionAmount"),
            KdvToplam = taxTotals.SelectMany(t => t.Elements(aggregate + "TaxSubtotal"))
                .Where(t => t.Element(aggregate + "TaxCategory")?.Element(aggregate + "TaxScheme")?.Element(basic + "TaxTypeCode")?.Value == "0015")
                .Sum(t => Amount(t.Element(basic + "TaxAmount"), "TaxAmount")),
            GenelToplam = Amount(totals?.Element(basic + "PayableAmount"), "PayableAmount"),
            XmlMevcut = true
        };
    }

    #endregion
}


