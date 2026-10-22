using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services.Destek;

/// <summary>
/// Destek olaylarını üst çubuktaki bildirim zilinde (Bildirimler tablosu) gösterir. Kayıtlar, olayın kaydıyla aynı
/// SaveChanges içinde yazılır. İşlemi yapan kişiye bildirim gitmez; pasif kullanıcılar ve "Destek talebi uyarısı"nı
/// kapatanlar atlanır.
/// </summary>
internal static class DestekBildirimci
{
    public static string TalepEdenLinki(int talepId) => $"/destek-talepleri/talep-takip/{talepId}";

    public static string EkipLinki(int talepId) => $"/destek-talepleri/{talepId}";

    public static async Task EkleAsync(
        ApplicationDbContext context,
        IEnumerable<int?> alicilar,
        int? islemYapanId,
        DestekTalebi talep,
        string baslik,
        string icerik,
        bool ekipLinki,
        BildirimOncelik oncelik = BildirimOncelik.Normal,
        CancellationToken cancellationToken = default)
    {
        var adaylar = alicilar
            .Where(id => id is > 0 && id != islemYapanId)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        if (adaylar.Count == 0) return;

        var aktifler = await context.Kullanicilar.AsNoTracking()
            .Where(k => adaylar.Contains(k.Id) && k.Aktif)
            .Select(k => k.Id)
            .ToListAsync(cancellationToken);
        var kapatanlar = await context.BildirimAyarlari.AsNoTracking()
            .Where(a => adaylar.Contains(a.KullaniciId) && !a.DestekTalebiUyarisi)
            .Select(a => a.KullaniciId)
            .ToListAsync(cancellationToken);

        var link = ekipLinki ? EkipLinki(talep.Id) : TalepEdenLinki(talep.Id);
        foreach (var kullaniciId in aktifler.Except(kapatanlar))
        {
            context.Bildirimler.Add(new Bildirim
            {
                KullaniciId = kullaniciId,
                Baslik = Kisalt($"{talep.TalepNo} · {baslik}", 200),
                Icerik = Kisalt(icerik, 1000),
                Tip = BildirimTipi.DestekTalebi,
                Oncelik = oncelik,
                IliskiliTablo = nameof(DestekTalebi),
                IliskiliKayitId = talep.Id,
                Link = link,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Ekipten haber verilecek kişiler: departman üyeleri; üye yoksa yetkiyi taşıyanlar; o da yoksa yöneticiler.
    /// </summary>
    public static async Task<List<int?>> EkipAlicilariAsync(
        ApplicationDbContext context, int? departmanId, string yetki, CancellationToken cancellationToken = default)
    {
        if (departmanId is > 0)
        {
            var uyeler = await context.DestekDepartmanUyeleri.AsNoTracking()
                .Where(u => u.DepartmanId == departmanId.Value && u.Kullanici.Aktif)
                .Select(u => (int?)u.KullaniciId)
                .ToListAsync(cancellationToken);
            if (uyeler.Count > 0) return uyeler;
        }

        var yetkililer = await context.Kullanicilar.AsNoTracking()
            .Where(k => k.Aktif && k.Rol.RolAdi != "Admin" &&
                        k.Rol.Yetkiler.Any(y => !y.IsDeleted && y.Izin && y.YetkiKodu == yetki))
            .Select(k => (int?)k.Id)
            .ToListAsync(cancellationToken);
        if (yetkililer.Count > 0) return yetkililer;

        return await context.Kullanicilar.AsNoTracking()
            .Where(k => k.Aktif && k.Rol.RolAdi == "Admin")
            .Select(k => (int?)k.Id)
            .ToListAsync(cancellationToken);
    }

    public static string Kisalt(string? metin, int uzunluk)
    {
        if (string.IsNullOrEmpty(metin)) return string.Empty;
        var tek = metin.Replace("\r", " ").Replace("\n", " ").Trim();
        return tek.Length <= uzunluk ? tek : tek[..(uzunluk - 1)] + "…";
    }
}
