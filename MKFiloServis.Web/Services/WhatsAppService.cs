using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly ICRMService _crmService;

    public WhatsAppService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<WhatsAppService> logger, ICRMService crmService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _crmService = crmService;
    }

    #region Kişiler

    public async Task<List<WhatsAppKisi>> GetKisilerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppKisiler
            .Include(k => k.Cari)
            .OrderBy(k => k.AdSoyad)
            .ToListAsync();
    }

    public async Task<WhatsAppKisi?> GetKisiByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppKisiler
            .Include(k => k.Gruplari)
            .ThenInclude(gu => gu.Grup)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<WhatsAppKisi> CreateKisiAsync(WhatsAppKisi kisi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.WhatsAppKisiler.FirstOrDefaultAsync(k => k.Telefon == kisi.Telefon);
        if (mevcut != null)
            throw new Exception("Bu telefon numarası ile kayıtlı bir kişi zaten var.");

        context.WhatsAppKisiler.Add(kisi);
        await context.SaveChangesAsync();
        return kisi;
    }

    public async Task<WhatsAppKisi> UpdateKisiAsync(WhatsAppKisi kisi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.WhatsAppKisiler.Update(kisi);
        await context.SaveChangesAsync();
        return kisi;
    }

    public async Task DeleteKisiAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kisi = await context.WhatsAppKisiler.FindAsync(id);
        if (kisi != null)
        {
            kisi.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task SeciliCarilerdenKisiOlustur(List<int> cariIds)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cariler = await context.Cariler.Where(c => cariIds.Contains(c.Id)).ToListAsync();
        foreach (var cari in cariler)
        {
            if (string.IsNullOrWhiteSpace(cari.Telefon)) continue;

            var mevcut = await context.WhatsAppKisiler.FirstOrDefaultAsync(k => k.Telefon == cari.Telefon);
            if (mevcut == null)
            {
                context.WhatsAppKisiler.Add(new WhatsAppKisi
                {
                    AdSoyad = cari.Unvan,
                    Telefon = cari.Telefon,
                    CariId = cari.Id
                });
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task<int> KisileriSenkronizeEtAsync(List<WhatsAppKisi> kisiler)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (kisiler.Count == 0)
        {
            return 0;
        }

        var normalizedTelefonlar = kisiler
            .Select(k => k.Telefon?.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var mevcutTelefonlar = await context.WhatsAppKisiler
            .Where(k => normalizedTelefonlar.Contains(k.Telefon))
            .Select(k => k.Telefon)
            .ToListAsync();

        var mevcutSet = new HashSet<string>(mevcutTelefonlar.Where(t => !string.IsNullOrWhiteSpace(t))!, StringComparer.OrdinalIgnoreCase);
        var eklenecekler = new List<WhatsAppKisi>();

        foreach (var kisi in kisiler)
        {
            var telefon = kisi.Telefon?.Trim();
            if (string.IsNullOrWhiteSpace(telefon) || mevcutSet.Contains(telefon))
            {
                continue;
            }

            kisi.Telefon = telefon;
            eklenecekler.Add(kisi);
            mevcutSet.Add(telefon);
        }

        if (eklenecekler.Count > 0)
        {
            context.WhatsAppKisiler.AddRange(eklenecekler);
            await context.SaveChangesAsync();
        }

        return eklenecekler.Count;
    }

    #endregion

    #region Gruplar

    public async Task<List<WhatsAppGrup>> GetGruplarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppGruplar
            .Include(g => g.Uyeler)
            .OrderBy(g => g.GrupAdi)
            .ToListAsync();
    }

    public async Task<WhatsAppGrup?> GetGrupByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppGruplar
            .Include(g => g.Uyeler)
            .ThenInclude(gu => gu.Kisi)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<WhatsAppGrup> CreateGrupAsync(WhatsAppGrup grup)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.WhatsAppGruplar.Add(grup);
        await context.SaveChangesAsync();
        return grup;
    }

    public async Task<WhatsAppGrup> UpdateGrupAsync(WhatsAppGrup grup)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.WhatsAppGruplar.Update(grup);
        await context.SaveChangesAsync();
        return grup;
    }

    public async Task DeleteGrupAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var grup = await context.WhatsAppGruplar.FindAsync(id);
        if (grup != null)
        {
            grup.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task GrubaKisiEkleAsync(int grupId, int kisiId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.WhatsAppGrupUyeler.AnyAsync(gu => gu.GrupId == grupId && gu.KisiId == kisiId);
        if (!mevcut)
        {
            context.WhatsAppGrupUyeler.Add(new WhatsAppGrupUye { GrupId = grupId, KisiId = kisiId });
            await context.SaveChangesAsync();
        }
    }

    public async Task GruptanKisiCikarAsync(int grupId, int kisiId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var uye = await context.WhatsAppGrupUyeler.FirstOrDefaultAsync(gu => gu.GrupId == grupId && gu.KisiId == kisiId);
        if (uye != null)
        {
            context.WhatsAppGrupUyeler.Remove(uye);
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Sablonlar

    public async Task<List<WhatsAppSablon>> GetSablonlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppSablonlar.OrderBy(s => s.Baslik).ToListAsync();
    }

    public async Task<WhatsAppSablon?> GetSablonByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppSablonlar.FindAsync(id);
    }

    public async Task<WhatsAppSablon> CreateSablonAsync(WhatsAppSablon sablon)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.WhatsAppSablonlar.Add(sablon);
        await context.SaveChangesAsync();
        return sablon;
    }

    public async Task<WhatsAppSablon> UpdateSablonAsync(WhatsAppSablon sablon)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.WhatsAppSablonlar.Update(sablon);
        await context.SaveChangesAsync();
        return sablon;
    }

    public async Task DeleteSablonAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sablon = await context.WhatsAppSablonlar.FindAsync(id);
        if (sablon != null)
        {
            sablon.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Mesajlar

    public async Task<List<WhatsAppMesaj>> GetMesajlarByKisiAsync(int kisiId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppMesajlar
            .Include(m => m.Gonderen)
            .Where(m => m.KisiId == kisiId)
            .OrderBy(m => m.MesajTarihi)
            .ToListAsync();
    }

    public async Task<List<WhatsAppMesaj>> GetMesajlarByGrupAsync(int grupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppMesajlar
            .Include(m => m.Gonderen)
            .Where(m => m.GrupId == grupId)
            .OrderBy(m => m.MesajTarihi)
            .ToListAsync();
    }

    public async Task<WhatsAppMesaj> SendMesajToKisiAsync(int kisiId, string icerik, int? gonderenId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kisi = await context.WhatsAppKisiler.FindAsync(kisiId);
        if (kisi == null) throw new Exception("Kişi bulunamadı.");

        var mesaj = new WhatsAppMesaj
        {
            KisiId = kisiId,
            Icerik = icerik,
            Tipi = WhatsAppMesaj.Yon.Giden,
            GonderenId = gonderenId,
            MesajTarihi = DateTime.UtcNow,
            Durum = MesajDurum.Gonderildi,
            Okundu = true
        };

        context.WhatsAppMesajlar.Add(mesaj);
        await context.SaveChangesAsync();

        // Arka planda gerçek gönderim yapılabilir
        await _crmService.SendWhatsAppAsync(gonderenId ?? 0, kisi.Telefon, icerik);

        return mesaj;
    }

    public async Task<WhatsAppMesaj> SendMesajToGrupAsync(int grupId, string icerik, int? gonderenId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var grup = await context.WhatsAppGruplar
            .Include(g => g.Uyeler).ThenInclude(gu => gu.Kisi)
            .FirstOrDefaultAsync(g => g.Id == grupId);

        if (grup == null) throw new Exception("Grup bulunamadı.");

        var mesaj = new WhatsAppMesaj
        {
            GrupId = grupId,
            Icerik = icerik,
            Tipi = WhatsAppMesaj.Yon.Giden,
            GonderenId = gonderenId,
            MesajTarihi = DateTime.UtcNow,
            Durum = MesajDurum.Gonderildi,
            Okundu = true
        };

        context.WhatsAppMesajlar.Add(mesaj);
        await context.SaveChangesAsync();

        // Her üyeye mesajı gönder
        foreach (var uye in grup.Uyeler)
        {
            await _crmService.SendWhatsAppAsync(gonderenId ?? 0, uye.Kisi.Telefon, icerik);
        }

        return mesaj;
    }

    public async Task<int> GetOkunmamisMesajSayisiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WhatsAppMesajlar
            .CountAsync(m => m.Tipi == WhatsAppMesaj.Yon.Gelen && !m.Okundu);
    }

    public async Task MesajlariOkunduIsaretleAsync(int? kisiId = null, int? grupId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.WhatsAppMesajlar.Where(m => m.Tipi == WhatsAppMesaj.Yon.Gelen && !m.Okundu);

        if (kisiId.HasValue)
            query = query.Where(m => m.KisiId == kisiId.Value);

        if (grupId.HasValue)
            query = query.Where(m => m.GrupId == grupId.Value);

        await query.ExecuteUpdateAsync(s => s.SetProperty(m => m.Okundu, true));
    }

    #endregion
}


