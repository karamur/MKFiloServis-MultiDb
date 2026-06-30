using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Text.RegularExpressions;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface ICRMService
{
    // Bildirimler
    Task<List<Bildirim>> GetBildirimlerAsync(int kullaniciId, bool sadeceokunmamis = false);
    Task<int> GetOkunmamisBildirimSayisiAsync(int kullaniciId);
    Task<Bildirim> CreateBildirimAsync(Bildirim bildirim);
    Task BildirimOkunduIsaretle(int bildirimId);
    Task TumBildirimleriOkunduIsaretle(int kullaniciId);
    Task DeleteBildirimAsync(int bildirimId);

    // Mesajlar
    Task<List<Mesaj>> GetGelenMesajlarAsync(int kullaniciId);
    Task<List<Mesaj>> GetGonderilenMesajlarAsync(int kullaniciId);
    Task<int> GetOkunmamisMesajSayisiAsync(int kullaniciId);
    Task<Mesaj> SendMesajAsync(Mesaj mesaj);
    Task MesajOkunduIsaretle(int mesajId);
    Task DeleteMesajAsync(int mesajId);

    // Email
    Task<EmailAyar?> GetEmailAyarAsync(int? kullaniciId = null);
    Task<EmailAyar> SaveEmailAyarAsync(EmailAyar ayar);
    Task<bool> SendEmailAsync(int gonderenId, string aliciEmail, string konu, string icerik);
    Task<List<EmailListeItem>> GetGonderilenEmailListAsync(int kullaniciId, int maxCount = 50);
    Task<List<EmailListeItem>> GetGelenEmailListAsync(int kullaniciId, int maxCount = 50);

    // WhatsApp
    Task<WhatsAppAyar?> GetWhatsAppAyarAsync(int? kullaniciId = null);
    Task<WhatsAppAyar> SaveWhatsAppAyarAsync(WhatsAppAyar ayar);
    Task<bool> SendWhatsAppAsync(int gonderenId, string telefon, string mesaj);

    // Hatırlatıcılar
    Task<List<Hatirlatici>> GetHatirlaticilarAsync(int kullaniciId, DateTime? baslangic = null, DateTime? bitis = null);
    Task<List<Hatirlatici>> GetBugunkuHatirlaticilarAsync(int kullaniciId);
    Task<Hatirlatici> CreateHatirlaticiAsync(Hatirlatici hatirlatici);
    Task<Hatirlatici> UpdateHatirlaticiAsync(Hatirlatici hatirlatici);
    Task DeleteHatirlaticiAsync(int hatirlaticiId);
    Task HatirlaticiTamamlaAsync(int hatirlaticiId);

    // Kullanıcı-Cari Eşleştirme
    Task<List<KullaniciCari>> GetKullaniciBagliCarilerAsync(int kullaniciId);
    Task<KullaniciCari> AddKullaniciCariAsync(KullaniciCari kullaniciCari);
    Task<KullaniciCari> UpdateKullaniciCariAsync(KullaniciCari kullaniciCari);
    Task DeleteKullaniciCariAsync(int id);
    Task<bool> KullaniciBuCariyeErisebilirMi(int kullaniciId, int cariId);

    // Dashboard Widget
    Task<List<DashboardWidget>> GetDashboardWidgetlarAsync(int kullaniciId);
    Task SaveDashboardWidgetlarAsync(int kullaniciId, List<DashboardWidget> widgets);
}

public class EmailListeItem
{
    public string Baslik { get; set; } = string.Empty;
    public string Kimden { get; set; } = string.Empty;
    public string Kime { get; set; } = string.Empty;
    public string Ozet { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public bool Okundu { get; set; }
    public MesajDurum Durum { get; set; } = MesajDurum.Gonderildi;
    public bool GelenMi { get; set; }
}

public class CRMService : ICRMService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<CRMService> _logger;

    public CRMService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<CRMService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    #region Bildirimler

    public async Task<List<Bildirim>> GetBildirimlerAsync(int kullaniciId, bool sadeceokunmamis = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Bildirimler
            .Where(b => b.KullaniciId == kullaniciId);

        if (sadeceokunmamis)
            query = query.Where(b => !b.Okundu);

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<int> GetOkunmamisBildirimSayisiAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Bildirimler
            .CountAsync(b => b.KullaniciId == kullaniciId && !b.Okundu);
    }

    public async Task<Bildirim> CreateBildirimAsync(Bildirim bildirim)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Bildirimler.Add(bildirim);
        await context.SaveChangesAsync();
        return bildirim;
    }

    public async Task BildirimOkunduIsaretle(int bildirimId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bildirim = await context.Bildirimler.FindAsync(bildirimId);
        if (bildirim != null)
        {
            bildirim.Okundu = true;
            bildirim.OkunmaTarihi = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task TumBildirimleriOkunduIsaretle(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Bildirimler
            .Where(b => b.KullaniciId == kullaniciId && !b.Okundu)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Okundu, true)
                .SetProperty(b => b.OkunmaTarihi, DateTime.UtcNow));
    }

    public async Task DeleteBildirimAsync(int bildirimId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bildirim = await context.Bildirimler.FindAsync(bildirimId);
        if (bildirim != null)
        {
            bildirim.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Mesajlar

    public async Task<List<Mesaj>> GetGelenMesajlarAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Mesajlar
            .Include(m => m.Gonderen)
            .Where(m => m.AliciId == kullaniciId || m.AliciId == null)
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<Mesaj>> GetGonderilenMesajlarAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Mesajlar
            .Include(m => m.Alici)
            .Where(m => m.GonderenId == kullaniciId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<int> GetOkunmamisMesajSayisiAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Mesajlar
            .CountAsync(m => (m.AliciId == kullaniciId || m.AliciId == null) && !m.Okundu);
    }

    public async Task<Mesaj> SendMesajAsync(Mesaj mesaj)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        mesaj.Durum = MesajDurum.Gonderildi;
        context.Mesajlar.Add(mesaj);
        await context.SaveChangesAsync();

        // Alıcıya bildirim oluştur
        if (mesaj.AliciId.HasValue)
        {
            var gonderen = await context.Kullanicilar.FindAsync(mesaj.GonderenId);
            await CreateBildirimAsync(new Bildirim
            {
                KullaniciId = mesaj.AliciId.Value,
                Baslik = $"Yeni mesaj: {mesaj.Konu}",
                Icerik = $"{gonderen?.AdSoyad ?? "Bilinmeyen"} size bir mesaj gönderdi.",
                Tip = BildirimTipi.Mesaj,
                Link = "/crm/mesajlar"
            });
        }

        return mesaj;
    }

    public async Task MesajOkunduIsaretle(int mesajId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mesaj = await context.Mesajlar.FindAsync(mesajId);
        if (mesaj != null)
        {
            mesaj.Okundu = true;
            mesaj.OkunmaTarihi = DateTime.UtcNow;
            mesaj.Durum = MesajDurum.Okundu;
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteMesajAsync(int mesajId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mesaj = await context.Mesajlar.FindAsync(mesajId);
        if (mesaj != null)
        {
            mesaj.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Email

    public async Task<EmailAyar?> GetEmailAyarAsync(int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (kullaniciId.HasValue)
        {
            return await context.EmailAyarlari
                .FirstOrDefaultAsync(e => e.KullaniciId == kullaniciId && e.Aktif);
        }

        return await context.EmailAyarlari
            .FirstOrDefaultAsync(e => e.KullaniciId == null && e.Aktif);
    }

    public async Task<EmailAyar> SaveEmailAyarAsync(EmailAyar ayar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (ayar.Id == 0)
            context.EmailAyarlari.Add(ayar);
        else
            context.EmailAyarlari.Update(ayar);

        await context.SaveChangesAsync();
        return ayar;
    }

    public async Task<bool> SendEmailAsync(int gonderenId, string aliciEmail, string konu, string icerik)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var ayar = await GetEmailAyarAsync(gonderenId) ?? await GetEmailAyarAsync();
            if (ayar == null)
            {
                _logger.LogWarning("Email ayarlari bulunamadi");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(ayar.GonderenAdi ?? ayar.Email, ayar.Email));
            message.To.Add(MailboxAddress.Parse(aliciEmail));
            message.Subject = konu;
            message.Body = new TextPart("plain") { Text = icerik };

            using var smtp = new SmtpClient();
            var secureSocket = ayar.SslKullan ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await smtp.ConnectAsync(ayar.SmtpSunucu, ayar.SmtpPort, secureSocket);

            if (!string.IsNullOrWhiteSpace(ayar.Sifre))
            {
                await smtp.AuthenticateAsync(ayar.Email, ayar.Sifre);
            }

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            await SendMesajAsync(new Mesaj
            {
                GonderenId = gonderenId,
                Konu = konu,
                Icerik = icerik,
                Tip = MesajTipi.Email,
                DisAlici = aliciEmail,
                Durum = MesajDurum.Gonderildi
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email gonderilirken hata");

            try
            {
                await SendMesajAsync(new Mesaj
                {
                    GonderenId = gonderenId,
                    Konu = konu,
                    Icerik = icerik,
                    Tip = MesajTipi.Email,
                    DisAlici = aliciEmail,
                    Durum = MesajDurum.Hata
                });
            }
            catch
            {
            }

            return false;
        }
    }

    public async Task<List<EmailListeItem>> GetGonderilenEmailListAsync(int kullaniciId, int maxCount = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Mesajlar
            .Where(m => m.GonderenId == kullaniciId && m.Tip == MesajTipi.Email)
            .OrderByDescending(m => m.CreatedAt)
            .Take(maxCount)
            .Select(m => new EmailListeItem
            {
                Baslik = m.Konu,
                Kimden = string.Empty,
                Kime = m.DisAlici ?? string.Empty,
                Ozet = m.Icerik.Length > 120 ? m.Icerik.Substring(0, 120) + "..." : m.Icerik,
                Tarih = m.CreatedAt,
                Okundu = m.Durum == MesajDurum.Okundu,
                Durum = m.Durum,
                GelenMi = false
            })
            .ToListAsync();
    }

    public async Task<List<EmailListeItem>> GetGelenEmailListAsync(int kullaniciId, int maxCount = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ayar = await GetEmailAyarAsync(kullaniciId) ?? await GetEmailAyarAsync();
        if (ayar == null || !ayar.GelenKutusuAktif || string.IsNullOrWhiteSpace(ayar.ImapSunucu) || string.IsNullOrWhiteSpace(ayar.Email))
        {
            return new List<EmailListeItem>();
        }

        try
        {
            using var client = new ImapClient();
            var secureSocket = ayar.ImapSslKullan ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(ayar.ImapSunucu, ayar.ImapPort, secureSocket);

            if (!string.IsNullOrWhiteSpace(ayar.Sifre))
            {
                await client.AuthenticateAsync(ayar.Email, ayar.Sifre);
            }

            var klasor = await client.GetFolderAsync(ayar.GelenKlasoru ?? "INBOX");
            await klasor.OpenAsync(MailKit.FolderAccess.ReadOnly);

            var sonuc = new List<EmailListeItem>();
            var baslangic = Math.Max(0, klasor.Count - maxCount);

            for (var i = klasor.Count - 1; i >= baslangic; i--)
            {
                var mail = await klasor.GetMessageAsync(i);
                sonuc.Add(new EmailListeItem
                {
                    Baslik = mail.Subject ?? "(Konu yok)",
                    Kimden = mail.From.ToString(),
                    Kime = string.Join(", ", mail.To.Select(t => t.ToString())),
                    Ozet = GetOzetMetin(mail),
                    Tarih = mail.Date != DateTimeOffset.MinValue ? mail.Date.UtcDateTime : DateTime.UtcNow,
                    Okundu = true,
                    Durum = MesajDurum.Gonderildi,
                    GelenMi = true
                });
            }

            await client.DisconnectAsync(true);
            return sonuc.OrderByDescending(x => x.Tarih).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gelen emailler okunurken hata");
            return new List<EmailListeItem>();
        }
    }

    private static string GetOzetMetin(MimeMessage mail)
    {
        var icerik = mail.TextBody;

        if (string.IsNullOrWhiteSpace(icerik) && !string.IsNullOrWhiteSpace(mail.HtmlBody))
        {
            icerik = Regex.Replace(mail.HtmlBody, "<.*?>", " ");
        }

        if (string.IsNullOrWhiteSpace(icerik))
        {
            return string.Empty;
        }

        icerik = Regex.Replace(icerik, "\\s+", " ").Trim();
        return icerik.Length > 140 ? icerik[..140] + "..." : icerik;
    }

    #endregion

    #region WhatsApp

    public async Task<WhatsAppAyar?> GetWhatsAppAyarAsync(int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (kullaniciId.HasValue)
        {
            return await context.WhatsAppAyarlari
                .FirstOrDefaultAsync(e => e.KullaniciId == kullaniciId && e.Aktif);
        }

        return await context.WhatsAppAyarlari
            .FirstOrDefaultAsync(e => e.KullaniciId == null && e.Aktif);
    }

    public async Task<WhatsAppAyar> SaveWhatsAppAyarAsync(WhatsAppAyar ayar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (ayar.Id == 0)
            context.WhatsAppAyarlari.Add(ayar);
        else
            context.WhatsAppAyarlari.Update(ayar);

        await context.SaveChangesAsync();
        return ayar;
    }

    public async Task<bool> SendWhatsAppAsync(int gonderenId, string telefon, string mesaj)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var ayar = await GetWhatsAppAyarAsync(gonderenId) ?? await GetWhatsAppAyarAsync();
            if (ayar == null || string.IsNullOrEmpty(ayar.ApiKey))
            {
                _logger.LogWarning("WhatsApp ayarlari bulunamadi");
                return false;
            }

            // TODO: WhatsApp API implementasyonu
            // Twilio veya WhatsApp Business API ile mesaj gönder

            // Mesaj kaydı oluştur
            await SendMesajAsync(new Mesaj
            {
                GonderenId = gonderenId,
                Konu = "WhatsApp Mesajı",
                Icerik = mesaj,
                Tip = MesajTipi.WhatsApp,
                DisAlici = telefon,
                Durum = MesajDurum.Gonderildi
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp mesajı gönderilirken hata");
            return false;
        }
    }

    #endregion

    #region Hatırlatıcılar

    public async Task<List<Hatirlatici>> GetHatirlaticilarAsync(int kullaniciId, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Hatirlaticilar
            .Include(h => h.Cari)
            .Where(h => h.KullaniciId == kullaniciId);

        if (baslangic.HasValue)
            query = query.Where(h => h.BaslangicTarihi >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(h => h.BaslangicTarihi <= bitis.Value);

        return await query
            .OrderBy(h => h.BaslangicTarihi)
            .ToListAsync();
    }

    public async Task<List<Hatirlatici>> GetBugunkuHatirlaticilarAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var yarin = bugun.AddDays(1);

        return await context.Hatirlaticilar
            .Include(h => h.Cari)
            .Where(h => h.KullaniciId == kullaniciId 
                && h.BaslangicTarihi >= bugun 
                && h.BaslangicTarihi < yarin
                && h.Durum == HatirlaticiDurum.Bekliyor)
            .OrderBy(h => h.BaslangicTarihi)
            .ToListAsync();
    }

    public async Task<Hatirlatici> CreateHatirlaticiAsync(Hatirlatici hatirlatici)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Hatirlaticilar.Add(hatirlatici);
        await context.SaveChangesAsync();
        return hatirlatici;
    }

    public async Task<Hatirlatici> UpdateHatirlaticiAsync(Hatirlatici hatirlatici)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Hatirlaticilar.Update(hatirlatici);
        await context.SaveChangesAsync();
        return hatirlatici;
    }

    public async Task DeleteHatirlaticiAsync(int hatirlaticiId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hatirlatici = await context.Hatirlaticilar.FindAsync(hatirlaticiId);
        if (hatirlatici != null)
        {
            hatirlatici.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task HatirlaticiTamamlaAsync(int hatirlaticiId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hatirlatici = await context.Hatirlaticilar.FindAsync(hatirlaticiId);
        if (hatirlatici != null)
        {
            hatirlatici.Durum = HatirlaticiDurum.Tamamlandi;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Kullanıcı-Cari Eşleştirme

    public async Task<List<KullaniciCari>> GetKullaniciBagliCarilerAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.KullaniciCariler
            .Include(kc => kc.Cari)
            .Where(kc => kc.KullaniciId == kullaniciId)
            .OrderBy(kc => kc.Cari.Unvan)
            .ToListAsync();
    }

    public async Task<KullaniciCari> AddKullaniciCariAsync(KullaniciCari kullaniciCari)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Coka-cok iliski oldugu icin duplicate kontrolu KALDIRILDI
        // Ayni kullanici-cari cifti birden fazla kez eklenebilir (farkli izinlerle)
        context.KullaniciCariler.Add(kullaniciCari);
        await context.SaveChangesAsync();
        return kullaniciCari;
    }

    public async Task<KullaniciCari> UpdateKullaniciCariAsync(KullaniciCari kullaniciCari)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.KullaniciCariler.Update(kullaniciCari);
        await context.SaveChangesAsync();
        return kullaniciCari;
    }

    public async Task DeleteKullaniciCariAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kullaniciCari = await context.KullaniciCariler.FindAsync(id);
        if (kullaniciCari != null)
        {
            kullaniciCari.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> KullaniciBuCariyeErisebilirMi(int kullaniciId, int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Admin her cariye erişebilir
        var kullanici = await context.Kullanicilar
            .Include(k => k.Rol)
            .FirstOrDefaultAsync(k => k.Id == kullaniciId);

        if (kullanici?.Rol?.RolAdi == "Admin")
            return true;

        // Kullanıcının bağlı carileri kontrol et
        return await context.KullaniciCariler
            .AnyAsync(kc => kc.KullaniciId == kullaniciId && kc.CariId == cariId);
    }

    #endregion

    #region Dashboard Widget

    public async Task<List<DashboardWidget>> GetDashboardWidgetlarAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var widgets = await context.DashboardWidgetlar
            .Where(w => w.KullaniciId == kullaniciId)
            .OrderBy(w => w.Sira)
            .ToListAsync();

        // Varsayılan widget'ları yoksa oluştur
        if (!widgets.Any())
        {
            widgets = GetVarsayilanWidgetlar(kullaniciId);
            context.DashboardWidgetlar.AddRange(widgets);
            await context.SaveChangesAsync();
        }

        return widgets;
    }

    public async Task SaveDashboardWidgetlarAsync(int kullaniciId, List<DashboardWidget> widgets)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcutlar = await context.DashboardWidgetlar
            .Where(w => w.KullaniciId == kullaniciId)
            .ToListAsync();

        context.DashboardWidgetlar.RemoveRange(mevcutlar);

        foreach (var widget in widgets)
        {
            widget.KullaniciId = kullaniciId;
            context.DashboardWidgetlar.Add(widget);
        }

        await context.SaveChangesAsync();
    }

    private List<DashboardWidget> GetVarsayilanWidgetlar(int kullaniciId)
    {
        return new List<DashboardWidget>
        {
            new() { KullaniciId = kullaniciId, WidgetKodu = "bildirimler", Sira = 0, Kolon = 0, Genislik = 4, Gorunur = true },
            new() { KullaniciId = kullaniciId, WidgetKodu = "mesajlar", Sira = 1, Kolon = 4, Genislik = 4, Gorunur = true },
            new() { KullaniciId = kullaniciId, WidgetKodu = "randevular", Sira = 2, Kolon = 8, Genislik = 4, Gorunur = true },
            new() { KullaniciId = kullaniciId, WidgetKodu = "belgeler", Sira = 3, Kolon = 0, Genislik = 6, Gorunur = true },
            new() { KullaniciId = kullaniciId, WidgetKodu = "odemeler", Sira = 4, Kolon = 6, Genislik = 6, Gorunur = true },
        };
    }

    #endregion
}



