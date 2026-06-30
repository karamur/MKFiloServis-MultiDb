namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Belge süresi uyarı ayarları — belge-uyari-ayarlar.json dosyasında saklanır
/// </summary>
public class BelgeUyariAyarlar
{
    /// <summary>E-posta bildirimi etkin mi?</summary>
    public bool EmailEnabled { get; set; } = false;

    /// <summary>WhatsApp bildirimi etkin mi?</summary>
    public bool WhatsAppEnabled { get; set; } = false;

    /// <summary>Kaç gün kala uyarı gönderilsin (örn. [30, 15, 7, 3, 1])</summary>
    public int[] UyariGunleri { get; set; } = [30, 15, 7, 3, 1];

    /// <summary>WhatsApp kişi ID (kişiye gönderim için; grup yoksa kullanılır)</summary>
    public int WhatsAppKisiId { get; set; } = 0;

    /// <summary>WhatsApp grup ID (gruba gönderim için; varsa öncelikli)</summary>
    public int WhatsAppGrupId { get; set; } = 0;

    /// <summary>Son otomatik çalışma tarihi</summary>
    public DateTime? SonCalisma { get; set; }

    /// <summary>Son çalışmada tespit edilen uyarı sayısı</summary>
    public int SonCalismaUyariSayisi { get; set; }
}


