using System.Net;
using System.Net.Mail;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// E-posta bildirim servisi
/// </summary>
public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task<bool> SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true);
    Task<bool> SendBelgeUyariEmailAsync(string to, List<BelgeUyariEmail> uyarilar);
    Task<bool> SendFaturaEmailAsync(string to, string faturaNo, decimal tutar, DateTime vadeTarihi);

    // === Destek Talepleri E-posta Bildirimleri ===
    Task<bool> SendDestekYeniTalepEmailAsync(string musteriEmail, string musteriAdi, string talepNo, string konu, string oncelik);
    Task<bool> SendDestekYanitEmailAsync(string musteriEmail, string musteriAdi, string talepNo, string konu, string yanitOzet);
    Task<bool> SendDestekDurumEmailAsync(string musteriEmail, string musteriAdi, string talepNo, string konu, string eskiDurum, string yeniDurum);
    Task<bool> SendDestekAtamaEmailAsync(string temsilciEmail, string temsilciAdi, string talepNo, string konu, string musteriAdi, string oncelik);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _enabled;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _enabled = configuration.GetValue("Email:Enabled", false);
        _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = configuration.GetValue("Email:SmtpPort", 587);
        _smtpUser = configuration["Email:SmtpUser"] ?? "";
        _smtpPassword = configuration["Email:SmtpPassword"] ?? "";
        _fromEmail = configuration["Email:FromEmail"] ?? _smtpUser;
        _fromName = configuration["Email:FromName"] ?? "CRM Filo Servis";
        _enableSsl = configuration.GetValue("Email:EnableSsl", true);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        return await SendEmailAsync(new List<string> { to }, subject, body, isHtml);
    }

    public async Task<bool> SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true)
    {
        if (!_enabled)
        {
            _logger.LogWarning("E-posta servisi devre dışı. Mesaj gönderilmedi: {Subject}", subject);
            return false;
        }

        if (string.IsNullOrEmpty(_smtpUser) || string.IsNullOrEmpty(_smtpPassword))
        {
            _logger.LogError("SMTP ayarları yapılandırılmamış");
            return false;
        }

        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                EnableSsl = _enableSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (var recipient in to.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                message.To.Add(recipient);
            }

            await client.SendMailAsync(message);

            _logger.LogInformation("E-posta gönderildi: {Subject} -> {To}", subject, string.Join(", ", to));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilemedi: {Subject}", subject);
            return false;
        }
    }

    public async Task<bool> SendBelgeUyariEmailAsync(string to, List<BelgeUyariEmail> uyarilar)
    {
        if (!uyarilar.Any()) return true;

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffc107; padding: 15px; margin: 10px 0; border-radius: 5px; }}
        .critical {{ background-color: #f8d7da; border: 1px solid #dc3545; padding: 15px; margin: 10px 0; border-radius: 5px; }}
        table {{ width: 100%; border-collapse: collapse; }}
        th, td {{ padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }}
        th {{ background-color: #f8f9fa; }}
        .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>⚠️ Belge Uyarıları</h2>
        </div>
        <div class='content'>
            <p>Aşağıdaki belgelerin süresi dolmuş veya dolmak üzere:</p>
            <table>
                <thead>
                    <tr>
                        <th>Araç/Şoför</th>
                        <th>Belge</th>
                        <th>Bitiş Tarihi</th>
                        <th>Durum</th>
                    </tr>
                </thead>
                <tbody>
                    {string.Join("", uyarilar.Select(u => $@"
                    <tr class='{(u.GunKaldi <= 0 ? "critical" : "warning")}'>
                        <td>{u.SahipAdi}</td>
                        <td>{u.BelgeAdi}</td>
                        <td>{u.BitisTarihi:dd.MM.yyyy}</td>
                        <td>{(u.GunKaldi <= 0 ? "❌ SÜRESİ GEÇMİŞ" : $"⚠️ {u.GunKaldi} gün kaldı")}</td>
                    </tr>"))}
                </tbody>
            </table>
        </div>
        <div class='footer'>
            <p>Bu e-posta CRM Filo Servis sistemi tarafından otomatik olarak gönderilmiştir.</p>
            <p>© {DateTime.Now.Year} CRM Filo Servis</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, $"⚠️ {uyarilar.Count} Belge Uyarısı - CRM Filo Servis", body);
    }

    public async Task<bool> SendFaturaEmailAsync(string to, string faturaNo, decimal tutar, DateTime vadeTarihi)
    {
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: #0d6efd; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #198754; text-align: center; padding: 20px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📄 Fatura Bildirimi</h2>
        </div>
        <div class='content'>
            <div class='amount'>
                {tutar:N2} ₺
            </div>
            <div class='details'>
                <p><strong>Fatura No:</strong> {faturaNo}</p>
                <p><strong>Vade Tarihi:</strong> {vadeTarihi:dd.MM.yyyy}</p>
            </div>
            <p style='margin-top: 20px;'>Faturanız oluşturulmuştur. Detaylar için sisteme giriş yapabilirsiniz.</p>
        </div>
        <div class='footer'>
            <p>Bu e-posta CRM Filo Servis sistemi tarafından otomatik olarak gönderilmiştir.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, $"Fatura: {faturaNo} - CRM Filo Servis", body);
    }

    #region Destek Talepleri E-posta Bildirimleri

    public async Task<bool> SendDestekYeniTalepEmailAsync(string musteriEmail, string musteriAdi, string talepNo, string konu, string oncelik)
    {
        var body = BuildDestekEmailBody(
            "✅ Destek Talebiniz Alındı",
            "#198754",
            $@"<p>Sayın <strong>{musteriAdi}</strong>,</p>
               <p>Destek talebiniz başarıyla oluşturulmuştur. En kısa sürede ekibimiz tarafından incelenecektir.</p>
               <div class='details'>
                   <p><strong>Talep No:</strong> {talepNo}</p>
                   <p><strong>Konu:</strong> {konu}</p>
                   <p><strong>Öncelik:</strong> {oncelik}</p>
                   <p><strong>Tarih:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
               </div>
               <p>Talebinizin durumunu sistem üzerinden takip edebilirsiniz.</p>");

        return await SendEmailAsync(musteriEmail, $"[{talepNo}] Destek Talebiniz Alındı - {konu}", body);
    }

    public async Task<bool> SendDestekYanitEmailAsync(string musteriEmail, string musteriAdi, string talepNo, string konu, string yanitOzet)
    {
        var body = BuildDestekEmailBody(
            "💬 Destek Talebinize Yanıt",
            "#0d6efd",
            $@"<p>Sayın <strong>{musteriAdi}</strong>,</p>
               <p><strong>{talepNo}</strong> numaralı destek talebinize yeni bir yanıt eklenmiştir.</p>
               <div class='details'>
                   <p><strong>Konu:</strong> {konu}</p>
                   <p><strong>Yanıt Özeti:</strong></p>
                   <div style='background:#f8f9fa;padding:12px;border-radius:4px;margin-top:8px;'>
                       {yanitOzet}
                   </div>
               </div>
               <p>Detayları görmek ve yanıt vermek için sisteme giriş yapabilirsiniz.</p>");

        return await SendEmailAsync(musteriEmail, $"[{talepNo}] Yeni Yanıt - {konu}", body);
    }

    public async Task<bool> SendDestekDurumEmailAsync(string musteriEmail, string musteriAdi, string talepNo, string konu, string eskiDurum, string yeniDurum)
    {
        var durumRenk = yeniDurum switch
        {
            "Çözüldü" or "Kapalı" => "#198754",
            "Yanıt Bekleniyor" => "#ffc107",
            _ => "#0d6efd"
        };

        var body = BuildDestekEmailBody(
            "🔄 Talep Durumu Güncellendi",
            durumRenk,
            $@"<p>Sayın <strong>{musteriAdi}</strong>,</p>
               <p><strong>{talepNo}</strong> numaralı destek talebinizin durumu güncellenmiştir.</p>
               <div class='details'>
                   <p><strong>Konu:</strong> {konu}</p>
                   <p><strong>Önceki Durum:</strong> {eskiDurum}</p>
                   <p><strong>Yeni Durum:</strong> <span style='color:{durumRenk};font-weight:bold;'>{yeniDurum}</span></p>
               </div>");

        return await SendEmailAsync(musteriEmail, $"[{talepNo}] Durum Güncellendi: {yeniDurum} - {konu}", body);
    }

    public async Task<bool> SendDestekAtamaEmailAsync(string temsilciEmail, string temsilciAdi, string talepNo, string konu, string musteriAdi, string oncelik)
    {
        var body = BuildDestekEmailBody(
            "📋 Yeni Talep Ataması",
            "#6f42c1",
            $@"<p>Merhaba <strong>{temsilciAdi}</strong>,</p>
               <p>Aşağıdaki destek talebi size atanmıştır:</p>
               <div class='details'>
                   <p><strong>Talep No:</strong> {talepNo}</p>
                   <p><strong>Konu:</strong> {konu}</p>
                   <p><strong>Müşteri:</strong> {musteriAdi}</p>
                   <p><strong>Öncelik:</strong> {oncelik}</p>
               </div>
               <p>Lütfen en kısa sürede talebi inceleyin ve yanıtlayın.</p>");

        return await SendEmailAsync(temsilciEmail, $"[{talepNo}] Size Atanan Talep - {konu}", body);
    }

    private static string BuildDestekEmailBody(string baslik, string headerRenk, string icerik)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: {headerRenk}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 20px; background: #fff; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid {headerRenk}; }}
        .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; background: #f8f9fa; border-radius: 0 0 8px 8px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{baslik}</h2>
        </div>
        <div class='content'>
            {icerik}
        </div>
        <div class='footer'>
            <p>Bu e-posta CRM Filo Servis sistemi tarafından otomatik olarak gönderilmiştir.</p>
            <p>© {DateTime.Now.Year} CRM Filo Servis</p>
        </div>
    </div>
</body>
</html>";
    }

    #endregion
}

public class BelgeUyariEmail
{
    public string SahipAdi { get; set; } = "";
    public string BelgeAdi { get; set; } = "";
    public DateTime BitisTarihi { get; set; }
    public int GunKaldi { get; set; }
}



