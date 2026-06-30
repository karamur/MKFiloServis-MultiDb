using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services.Interfaces;
using System.Text.Json;

namespace MKFiloServis.Web.Services;

public class LastikService : ILastikService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAracMasrafService _aracMasrafService;

    public LastikService(IDbContextFactory<ApplicationDbContext> contextFactory, IAracMasrafService aracMasrafService)
    {
        _contextFactory = contextFactory;
        _aracMasrafService = aracMasrafService;
    }

    // ================================================================
    //  DEPO
    // ================================================================

    public async Task<List<LastikDepo>> GetDepoListAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikDepolar
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.DepoAdi)
            .ToListAsync();
    }

    public async Task<LastikDepo?> GetDepoByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikDepolar
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<LastikDepo> CreateDepoAsync(LastikDepo depo)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.LastikDepolar.Add(depo);
        await ctx.SaveChangesAsync();
        return depo;
    }

    public async Task<LastikDepo> UpdateDepoAsync(LastikDepo depo)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        depo.UpdatedAt = DateTime.UtcNow;
        ctx.LastikDepolar.Update(depo);
        await ctx.SaveChangesAsync();
        return depo;
    }

    public async Task DeleteDepoAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var depo = await ctx.LastikDepolar.FindAsync(id);
        if (depo != null)
        {
            depo.IsDeleted = true;
            depo.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    // ================================================================
    //  STOK
    // ================================================================

    public async Task<List<LastikStok>> GetStokListAsync(int? depoId = null, bool? aktif = true)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var query = ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Include(s => s.Arac)
            .Include(s => s.KaynakArac)
            .Where(s => !s.IsDeleted);

        if (depoId.HasValue)
            query = query.Where(s => s.DepoId == depoId.Value);

        if (aktif.HasValue)
            query = query.Where(s => s.Aktif == aktif.Value);

        return await query
            .OrderBy(s => s.Depo != null ? s.Depo.DepoAdi : "")
            .ThenBy(s => s.Marka)
            .ThenBy(s => s.Ebat)
            .ToListAsync();
    }

    public async Task<LastikStok?> GetStokByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Include(s => s.Arac)
            .Include(s => s.KaynakArac)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<LastikStok> CreateStokAsync(LastikStok stok)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.LastikStoklar.Add(stok);
        await ctx.SaveChangesAsync();
        return stok;
    }

    public async Task<List<LastikStok>> CreateStokToplualAsync(LastikStok sablon, int adet)
    {
        if (adet < 1) adet = 1;
        if (adet > 20) adet = 20;

        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var liste = new List<LastikStok>(adet);
        for (int i = 0; i < adet; i++)
        {
            var yeni = new LastikStok
            {
                DepoId = sablon.DepoId,
                AracId = sablon.AracId,
                KaynakAracId = sablon.KaynakAracId,
                YedekMi = sablon.YedekMi,
                Marka = sablon.Marka,
                Ebat = sablon.Ebat,
                Sezon = sablon.Sezon,
                SeriNo = sablon.SeriNo,
                Durum = sablon.Durum,
                Aktif = sablon.Aktif,
                Notlar = sablon.Notlar
            };
            ctx.LastikStoklar.Add(yeni);
            liste.Add(yeni);
        }
        await ctx.SaveChangesAsync();
        return liste;
    }

    public async Task<LastikStok> UpdateStokAsync(LastikStok stok)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        stok.UpdatedAt = DateTime.UtcNow;
        ctx.LastikStoklar.Update(stok);
        await ctx.SaveChangesAsync();
        return stok;
    }

    public async Task DeleteStokAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var stok = await ctx.LastikStoklar.FindAsync(id);
        if (stok != null)
        {
            stok.IsDeleted = true;
            stok.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    public async Task PasifAlAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var stok = await ctx.LastikStoklar.FindAsync(id);
        if (stok != null)
        {
            stok.Aktif = false;
            stok.Durum = LastikDurum.Hurda;
            stok.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    // ================================================================
    //  DEĞİŞİM
    // ================================================================

    public async Task<List<LastikDegisim>> GetDegisimListAsync(int? aracId = null, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var query = ctx.LastikDegisimler
            .AsNoTracking()
            .Include(d => d.Arac)
            .Include(d => d.TakilanStok).ThenInclude(s => s!.Depo)
            .Include(d => d.SokulenStok).ThenInclude(s => s!.Depo)
            .Include(d => d.HedefDepo)
            .Include(d => d.KaynakDepo)
            .Where(d => !d.IsDeleted);

        if (aracId.HasValue)
            query = query.Where(d => d.AracId == aracId.Value);
        if (baslangic.HasValue)
            query = query.Where(d => d.DegisimTarihi >= baslangic.Value);
        if (bitis.HasValue)
            query = query.Where(d => d.DegisimTarihi <= bitis.Value);

        return await query
            .OrderByDescending(d => d.DegisimTarihi)
            .ToListAsync();
    }

    public async Task<LastikDegisim?> GetDegisimByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikDegisimler
            .AsNoTracking()
            .Include(d => d.Arac)
            .Include(d => d.TakilanStok).ThenInclude(s => s!.Depo)
            .Include(d => d.SokulenStok).ThenInclude(s => s!.Depo)
            .Include(d => d.HedefDepo)
            .Include(d => d.KaynakDepo)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<LastikDegisim> CreateDegisimAsync(LastikDegisim degisim)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.LastikDegisimler.Add(degisim);
        await ctx.SaveChangesAsync(); // Id alabilmek için önce kaydet

        var payload = ParseDegisimPayload(degisim.Notlar);
        var satirlar = SatirlariCikar(degisim, payload);
        await SenkronizeStoklarAsync(ctx, degisim, satirlar, eskiSatirlar: null);

        if (payload != null)
        {
            degisim.Notlar = JsonSerializer.Serialize(payload);
            ctx.LastikDegisimler.Update(degisim);
        }

        await ctx.SaveChangesAsync();
        await SenkronizeLastikMasrafiAsync(degisim, payload);
        return degisim;
    }

    public async Task<LastikDegisim> UpdateDegisimAsync(LastikDegisim degisim)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        // Eski hâli oku (stok geri alma için)
        var mevcut = await ctx.LastikDegisimler
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == degisim.Id);

        var eskiPayload = mevcut != null ? ParseDegisimPayload(mevcut.Notlar) : null;
        var eskiSatirlar = mevcut != null ? SatirlariCikar(mevcut, eskiPayload) : new List<LastikDegisimNotSatiri>();

        degisim.UpdatedAt = DateTime.UtcNow;
        ctx.LastikDegisimler.Update(degisim);
        await ctx.SaveChangesAsync();

        var yeniPayload = ParseDegisimPayload(degisim.Notlar);
        var yeniSatirlar = SatirlariCikar(degisim, yeniPayload);
        await SenkronizeStoklarAsync(ctx, degisim, yeniSatirlar, eskiSatirlar);

        if (yeniPayload != null)
            degisim.Notlar = JsonSerializer.Serialize(yeniPayload);

        await ctx.SaveChangesAsync();
        await SenkronizeLastikMasrafiAsync(degisim, yeniPayload);

        return degisim;
    }

    public async Task DeleteDegisimAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var degisim = await ctx.LastikDegisimler.FindAsync(id);
        if (degisim != null)
        {
            // Bu değişimin yarattığı stok hareketlerini geri al
            var satirlar = SatirlariCikar(degisim);
            await GeriAlSenkronizasyonAsync(ctx, degisim, satirlar);

            degisim.IsDeleted = true;
            degisim.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();

            var payload = ParseDegisimPayload(degisim.Notlar);
            if (payload?.SatinAlma?.MasrafId.HasValue == true)
                await _aracMasrafService.DeleteAsync(payload.SatinAlma.MasrafId.Value);
        }
    }

    private async Task SenkronizeLastikMasrafiAsync(LastikDegisim degisim, LastikDegisimNotPayload? payload)
    {
        var satinAlma = payload?.SatinAlma;
        if (satinAlma?.ToplamTutar == null || satinAlma.ToplamTutar <= 0)
        {
            if (satinAlma?.MasrafId.HasValue == true)
            {
                await _aracMasrafService.DeleteAsync(satinAlma.MasrafId.Value);
                satinAlma.MasrafId = null;
                degisim.Notlar = JsonSerializer.Serialize(payload);

                await using var ctx = await _contextFactory.CreateDbContextAsync();
                ctx.LastikDegisimler.Update(degisim);
                await ctx.SaveChangesAsync();
            }
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var masrafKalemi = await context.MasrafKalemleri
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.Aktif && m.Kategori == MasrafKategori.Lastik)
            .OrderBy(m => m.Id)
            .FirstOrDefaultAsync();

        if (masrafKalemi == null)
            throw new InvalidOperationException("Lastik masraf kalemi bulunamadı.");

        var degisenAdet = payload?.Satirlar?.Count(s => s.IslemYapildiMi != false) ?? 0;
        var satinAlinanAdet = payload?.Satirlar?.Count(s => s.IslemYapildiMi != false && string.Equals(s.TeminSekli, LastikTeminSekilleri.SatinAlma, StringComparison.OrdinalIgnoreCase)) ?? 0;
        var aciklama = $"Lastik değişimi {degisim.DegisimTarihi:dd.MM.yyyy} - {degisenAdet}/4 pozisyon, satın alma {satinAlinanAdet} adet";
        if (!string.IsNullOrWhiteSpace(satinAlma.TedarikciAdi))
            aciklama += $" | Tedarikçi: {satinAlma.TedarikciAdi}";

        var aracMasraf = new AracMasraf
        {
            Id = satinAlma.MasrafId ?? 0,
            AracId = degisim.AracId,
            MasrafKalemiId = masrafKalemi.Id,
            MasrafTarihi = satinAlma.FaturaTarihi ?? satinAlma.OdemeTarihi ?? degisim.DegisimTarihi,
            Tutar = satinAlma.ToplamTutar.Value,
            Aciklama = aciklama,
            BelgeNo = satinAlma.FaturasizMi ? "FATURASIZ" : satinAlma.FaturaNo,
            OdemeKaynak = GetMasrafOdemeKaynak(satinAlma),
            BankaHesapId = satinAlma.OdemeHesapId,
            CariId = null,
            SoforId = null
        };

        AracMasraf kaydedilenMasraf;
        if (satinAlma.MasrafId.HasValue)
            kaydedilenMasraf = await _aracMasrafService.UpdateAsync(aracMasraf, muhasebeFisiOlustur: satinAlma.OdemeYapildi);
        else
            kaydedilenMasraf = await _aracMasrafService.CreateAsync(aracMasraf, muhasebeFisiOlustur: satinAlma.OdemeYapildi);

        satinAlma.MasrafId = kaydedilenMasraf.Id;
        degisim.Notlar = JsonSerializer.Serialize(payload);

        await using var updateContext = await _contextFactory.CreateDbContextAsync();
        updateContext.LastikDegisimler.Update(degisim);
        await updateContext.SaveChangesAsync();
    }

    private static MasrafOdemeKaynak GetMasrafOdemeKaynak(LastikDegisimFinansNotu satinAlma)
    {
        if (!satinAlma.OdemeYapildi)
            return satinAlma.OdemeHesapId.HasValue ? MasrafOdemeKaynak.Banka : MasrafOdemeKaynak.Kasa;

        return satinAlma.OdemeHesapTipi == HesapTipi.Kasa
            ? MasrafOdemeKaynak.Kasa
            : MasrafOdemeKaynak.Banka;
    }

    // ----------------------------------------------------------------
    //  Stok senkronizasyonu (lastik takip güncellemesi + kayıp üretimi)
    // ----------------------------------------------------------------

    private static List<LastikDegisimNotSatiri> SatirlariCikar(LastikDegisim degisim, LastikDegisimNotPayload? payload = null)
    {
        payload ??= ParseDegisimPayload(degisim.Notlar);
        if (payload?.Satirlar != null && payload.Satirlar.Count > 0)
            return payload.Satirlar
                .Where(SatirIslemGerektirir)
                .ToList();

        // Eski tek-satır kayıtları için fallback
        return new List<LastikDegisimNotSatiri>
        {
            new()
            {
                DegisimYapildi = true,
                IslemYapildiMi = true,
                Pozisyon = degisim.TakilanPozisyon ?? degisim.SokulenPozisyon,
                TakilanStokId = degisim.TakilanStokId,
                KaynakDepoId = degisim.KaynakDepoId,
                SokulenStokId = degisim.SokulenStokId,
                HedefDepoId = degisim.HedefDepoId
            }
        };
    }

    private static bool SatirIslemGerektirir(LastikDegisimNotSatiri satir)
    {
        if (satir.IslemYapildiMi != false || satir.DegisimYapildi)
            return true;

        if (satir.TakilanStokId.HasValue || satir.SokulenStokId.HasValue)
            return true;

        return string.Equals(satir.TeminSekli, LastikTeminSekilleri.SatinAlma, StringComparison.OrdinalIgnoreCase);
    }

    private async Task SenkronizeStoklarAsync(
        ApplicationDbContext ctx,
        LastikDegisim degisim,
        List<LastikDegisimNotSatiri> yeniSatirlar,
        List<LastikDegisimNotSatiri>? eskiSatirlar)
    {
        // 1) Eski hareketleri geri al (varsa)
        if (eskiSatirlar != null && eskiSatirlar.Count > 0)
            await GeriAlSenkronizasyonAsync(ctx, degisim, eskiSatirlar);

        // 2) Yeni hareketleri uygula
        foreach (var s in yeniSatirlar)
        {
            if (!s.TakilanStokId.HasValue && string.Equals(s.TeminSekli, LastikTeminSekilleri.SatinAlma, StringComparison.OrdinalIgnoreCase))
            {
                if (!s.KaynakDepoId.HasValue)
                    throw new InvalidOperationException($"{s.Pozisyon ?? "Pozisyon"} için satın alma deposu seçilmelidir.");

                if (string.IsNullOrWhiteSpace(s.SatinAlmaMarka) || string.IsNullOrWhiteSpace(s.SatinAlmaEbat))
                    throw new InvalidOperationException($"{s.Pozisyon ?? "Pozisyon"} için satın alma marka ve ebat bilgisi zorunludur.");

                var satinAlinanStok = new LastikStok
                {
                    DepoId = s.KaynakDepoId,
                    AracId = null,
                    YedekMi = false,
                    Marka = s.SatinAlmaMarka,
                    Ebat = s.SatinAlmaEbat,
                    Sezon = s.SatinAlmaSezon ?? LastikSezon.YazLastigi,
                    SeriNo = s.SatinAlmaSeriNo,
                    Durum = LastikDurum.Kullanilabilir,
                    Aktif = true,
                    Notlar = BuildSatinAlmaStokNotu(degisim, s)
                };

                ctx.LastikStoklar.Add(satinAlinanStok);
                await ctx.SaveChangesAsync();

                s.TakilanStokId = satinAlinanStok.Id;
                s.TakilanEtiket = $"{satinAlinanStok.Ebat} {satinAlinanStok.Marka}".Trim();
            }

            // Sökülen lastik: araçtan çıkar
            if (s.SokulenStokId.HasValue)
            {
                var sokulen = await ctx.LastikStoklar.FindAsync(s.SokulenStokId.Value);
                if (sokulen != null)
                {
                    sokulen.AracId = null;
                    sokulen.KaynakAracId = degisim.AracId > 0 ? degisim.AracId : null;

                    var akibet = s.SokulenAkibet?.ToLowerInvariant();

                    if (akibet == "cop")
                    {
                        // Hurdaya çıkar: depoya alınmaz, aktif değil
                        sokulen.DepoId = s.HedefDepoId;
                        sokulen.Durum = LastikDurum.Hurda;
                        sokulen.Aktif = false;
                    }
                    else if (akibet == "tamir")
                    {
                        // Tamir: depoya al, durum Tamir
                        sokulen.DepoId = s.HedefDepoId;
                        sokulen.Durum = LastikDurum.Tamir;
                        sokulen.Aktif = true;
                    }
                    else if (s.HedefDepoId.HasValue)
                    {
                        // Normal depoya teslim
                        sokulen.DepoId = s.HedefDepoId;
                        sokulen.Durum = LastikDurum.Kullanilabilir;
                        sokulen.Aktif = true;
                    }
                    else
                    {
                        // Sökülen lastik depoya teslim edilmedi → KAYIP
                        sokulen.DepoId = null;
                        sokulen.Durum = LastikDurum.Kayip;
                        sokulen.Aktif = true;
                    }
                    sokulen.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (degisim.AracId > 0)
            {
                // Sökülen stok seçilmedi → kayıp olarak yeni bir stok kaydı üret
                var kayip = new LastikStok
                {
                    AracId = null,
                    DepoId = null,
                    YedekMi = false,
                    Marka = "(Bilinmeyen)",
                    Ebat = "-",
                    Sezon = LastikSezon.YazLastigi,
                    Durum = LastikDurum.Kayip,
                    Aktif = true,
                    Notlar = $"Otomatik kayıp kaydı. Değişim Id={degisim.Id}, Pozisyon={s.Pozisyon}"
                };
                ctx.LastikStoklar.Add(kayip);
            }

            // Takılan lastik: araca bağla, depodan çıkar
            if (s.TakilanStokId.HasValue)
            {
                var takilan = await ctx.LastikStoklar.FindAsync(s.TakilanStokId.Value);
                if (takilan != null)
                {
                    takilan.AracId = degisim.AracId;
                    takilan.DepoId = null;
                    takilan.YedekMi = false;
                    if (takilan.Durum == LastikDurum.Kayip)
                        takilan.Durum = LastikDurum.Kullanilabilir;
                    takilan.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }

    private async Task GeriAlSenkronizasyonAsync(
        ApplicationDbContext ctx,
        LastikDegisim degisim,
        List<LastikDegisimNotSatiri> satirlar)
    {
        foreach (var s in satirlar)
        {
            // Takılan lastiği araçtan çıkar, kaynak depoya geri koy
            if (s.TakilanStokId.HasValue)
            {
                var takilan = await ctx.LastikStoklar.FindAsync(s.TakilanStokId.Value);
                if (takilan != null && takilan.AracId == degisim.AracId)
                {
                    takilan.AracId = null;
                    takilan.DepoId = s.KaynakDepoId;
                    takilan.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Sökülen lastiği tekrar araca tak (depodan çıkar)
            if (s.SokulenStokId.HasValue)
            {
                var sokulen = await ctx.LastikStoklar.FindAsync(s.SokulenStokId.Value);
                if (sokulen != null)
                {
                    sokulen.AracId = degisim.AracId;
                    sokulen.DepoId = null;
                    sokulen.KaynakAracId = null;
                    sokulen.Aktif = true;
                    if (sokulen.Durum == LastikDurum.Kayip || sokulen.Durum == LastikDurum.Hurda
                        || sokulen.Durum == LastikDurum.Tamir)
                        sokulen.Durum = LastikDurum.Kullanilabilir;
                    sokulen.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }

    public async Task<List<LastikAracDonemOzet>> GetAracDonemOzetListAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var araclar = await ctx.Araclar
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.AktifPlaka,
                a.Marka,
                a.Model,
                a.ModelYili
            })
            .OrderBy(a => a.AktifPlaka)
            .ToListAsync();

        var degisimQuery = ctx.LastikDegisimler
            .AsNoTracking()
            .Where(d => !d.IsDeleted);

        if (baslangic.HasValue)
            degisimQuery = degisimQuery.Where(d => d.DegisimTarihi >= baslangic.Value);

        if (bitis.HasValue)
            degisimQuery = degisimQuery.Where(d => d.DegisimTarihi <= bitis.Value);

        var donemDegisimSayilari = await degisimQuery
            .GroupBy(d => d.AracId)
            .Select(g => new { AracId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.AracId, x => x.Sayi);

        var takiliLastikler = await ctx.LastikStoklar
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Aktif && s.AracId != null)
            .Select(s => new LastikAracaTakiliSatir
            {
                AracId = s.AracId!.Value,
                Marka = s.Marka,
                Ebat = s.Ebat,
                Sezon = s.Sezon
            })
            .ToListAsync();

        var takiliByArac = takiliLastikler
            .GroupBy(x => x.AracId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Aktif sezon ayarını belirle (Dönem sütunu için)
        var bugun = DateTime.Today;
        var sezonAyarlari = await ctx.LastikSezonAyarlari
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.Aktif)
            .ToListAsync();
        LastikSezonAyar? aktifSezon = null;
        foreach (var ayar in sezonAyarlari)
        {
            var (bas, bit) = SezonTarihAralik(ayar, bugun.Year);
            if (bugun >= bas && bugun <= bit) { aktifSezon = ayar; break; }
        }
        LastikSezon? beklenenSezon = aktifSezon?.SezonTipi == LastikSezonTipi.Yaz ? LastikSezon.YazLastigi
            : aktifSezon?.SezonTipi == LastikSezonTipi.Kis ? LastikSezon.KisLastigi
            : null;

        var sonuc = new List<LastikAracDonemOzet>(araclar.Count);

        foreach (var a in araclar)
        {
            var donemDegisimSayisi = donemDegisimSayilari.TryGetValue(a.Id, out var sayi) ? sayi : 0;
            var aracaTakili = takiliByArac.TryGetValue(a.Id, out var liste) ? liste : new List<LastikAracaTakiliSatir>();

            var takiliSayisi = aracaTakili.Count;
            var dortLastikAyni = takiliSayisi == 4 && aracaTakili
                .Select(x => $"{x.Marka}|{x.Ebat}|{(int)x.Sezon}")
                .Distinct()
                .Count() == 1;

            var takiliOzet = takiliSayisi == 0
                ? "Takili lastik kaydi yok"
                : string.Join(" | ", aracaTakili
                    .GroupBy(x => new { x.Marka, x.Ebat, x.Sezon })
                    .Select(g => $"{g.Key.Marka} {g.Key.Ebat} {GetSezonText(g.Key.Sezon)} x{g.Count()}"));

            bool? buSezonDegisimYapildi = beklenenSezon.HasValue
                ? aracaTakili.Any(x => x.Sezon == beklenenSezon.Value || x.Sezon == LastikSezon.DortMevsim)
                : null;

            sonuc.Add(new LastikAracDonemOzet
            {
                AracId = a.Id,
                Plaka = a.AktifPlaka ?? "-",
                AracBilgisi = $"{a.Marka} {a.Model} {a.ModelYili}".Trim(),
                DonemDegisimSayisi = donemDegisimSayisi,
                DonemdeDegisti = donemDegisimSayisi > 0,
                TakiliLastikSayisi = takiliSayisi,
                DortLastikAyniMi = dortLastikAyni,
                TakiliLastikOzeti = takiliOzet,
                BuSezonDegisimYapildi = buSezonDegisimYapildi,
                AktifSezonAdi = aktifSezon?.Ad
            });
        }

        return sonuc;
    }

    public async Task<LastikAracDetay?> GetAracDetayAsync(int aracId, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var arac = await ctx.Araclar
            .AsNoTracking()
            .Include(a => a.PlakaGecmisi)
            .FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);

        if (arac == null)
            return null;

        var takiliLastikler = await ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Where(s => !s.IsDeleted && s.Aktif && s.AracId == aracId)
            .OrderBy(s => s.Marka)
            .ThenBy(s => s.Ebat)
            .ToListAsync();

        var depoLastikler = await ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Where(s => !s.IsDeleted && s.Aktif && s.AracId == null && s.KaynakAracId == aracId)
            .OrderBy(s => s.Durum)
            .ThenBy(s => s.Marka)
            .ThenBy(s => s.Ebat)
            .ToListAsync();

        var hareketQuery = ctx.LastikDegisimler
            .AsNoTracking()
            .Include(d => d.TakilanStok)
            .Include(d => d.SokulenStok)
            .Include(d => d.KaynakDepo)
            .Include(d => d.HedefDepo)
            .Where(d => !d.IsDeleted && d.AracId == aracId);

        if (baslangic.HasValue)
            hareketQuery = hareketQuery.Where(d => d.DegisimTarihi >= baslangic.Value);

        if (bitis.HasValue)
            hareketQuery = hareketQuery.Where(d => d.DegisimTarihi <= bitis.Value);

        var hareketKayitlari = await hareketQuery
            .OrderByDescending(d => d.DegisimTarihi)
            .ToListAsync();

        var hareketler = hareketKayitlari
            .Select(d => new LastikAracHareketSatiri
            {
                DegisimId = d.Id,
                Tarih = d.DegisimTarihi,
                DegisimTipi = d.DegisimTipi,
                KmDurumu = d.KmDurumu,
                TakilanAciklama = FormatTakilanHareket(d),
                SokulenAciklama = FormatSokulenHareket(d),
                YapilanYer = d.YapilanYer ?? string.Empty,
                Ucret = d.Ucret
            })
            .ToList();

        var plakaGecmisi = arac.PlakaGecmisi
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.GirisTarihi)
            .Select(p => new LastikAracPlakaSatiri
            {
                Plaka = p.Plaka,
                GirisTarihi = p.GirisTarihi,
                CikisTarihi = p.CikisTarihi,
                Aktif = p.Aktif
            })
            .ToList();

        return new LastikAracDetay
        {
            AracId = arac.Id,
            Plaka = arac.AktifPlaka ?? "-",
            AracBilgisi = $"{arac.Marka} {arac.Model} {arac.ModelYili}".Trim(),
            PlakaGecmisi = plakaGecmisi,
            TakiliLastikler = takiliLastikler,
            DepoLastikler = depoLastikler,
            Hareketler = hareketler
        };
    }

    public async Task<List<LastikPlakaEnvanteri>> GetPlakaBazliEnvanterAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var sezonDurumByArac = await GetPlakaSezonDurumMapAsync(ctx);

        var araclar = await ctx.Araclar
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.AktifPlaka,
                a.Marka,
                a.Model,
                a.ModelYili
            })
            .OrderBy(a => a.AktifPlaka)
            .ToListAsync();

        var stoklar = await ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Where(s => !s.IsDeleted && s.Aktif && (s.AracId != null || s.KaynakAracId != null))
            .ToListAsync();

        var stokByArac = stoklar
            .Where(s => (s.AracId ?? s.KaynakAracId) != null)
            .GroupBy(s => (s.AracId ?? s.KaynakAracId)!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sonuc = new List<LastikPlakaEnvanteri>(araclar.Count);
        foreach (var a in araclar)
        {
            stokByArac.TryGetValue(a.Id, out var liste);
            liste ??= new();

            var satirlar = liste
                .OrderBy(s => s.YedekMi)
                .ThenBy(s => s.Sezon)
                .ThenBy(s => s.Marka)
                .Select(s => new LastikPlakaEnvanterSatiri
                {
                    StokId = s.Id,
                    Marka = s.Marka,
                    Ebat = s.Ebat,
                    Sezon = s.Sezon,
                    Durum = s.Durum,
                    SeriNo = s.SeriNo,
                    // Konum bilgisi AracId/KaynakAracId üzerinden belirlenir.
                    Yedek = s.AracId == null,
                    Takili = s.AracId != null,
                    DepoAdi = s.Depo?.DepoAdi
                })
                .ToList();

            sezonDurumByArac.TryGetValue(a.Id, out var durum);

            sonuc.Add(new LastikPlakaEnvanteri
            {
                AracId = a.Id,
                Plaka = a.AktifPlaka ?? "-",
                AracBilgisi = $"{a.Marka} {a.Model} {a.ModelYili}".Trim(),
                SonDegisimTarihi = durum?.SonDegisimTarihi,
                Lastikler = satirlar,
                TakiliSayisi = satirlar.Count(x => x.Takili),
                YedekSayisi = satirlar.Count(x => x.Yedek),
                YazVar = satirlar.Any(x => x.Sezon == LastikSezon.YazLastigi || x.Sezon == LastikSezon.DortMevsim),
                KisVar = satirlar.Any(x => x.Sezon == LastikSezon.KisLastigi || x.Sezon == LastikSezon.DortMevsim)
            });
        }

        return sonuc
            .OrderBy(x => x.SonDegisimTarihi ?? DateTime.MinValue)
            .ThenBy(x => x.Plaka)
            .ToList();
    }

    public async Task<List<LastikEksikSezonSatiri>> GetEksikSezonRaporuAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var araclar = await ctx.Araclar
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.AktifPlaka,
                a.Marka,
                a.Model,
                a.ModelYili
            })
            .OrderBy(a => a.AktifPlaka)
            .ToListAsync();

        var sezonDurumByArac = await GetPlakaSezonDurumMapAsync(ctx);

        var sonuc = new List<LastikEksikSezonSatiri>();
        foreach (var a in araclar)
        {
            var dur = sezonDurumByArac.TryGetValue(a.Id, out var d) ? d : null;
            var yazVar = dur?.YazVar ?? false;
            var kisVar = dur?.KisVar ?? false;

            // Tüm araçları listele (filtreler UI tarafında uygulanır)
            sonuc.Add(new LastikEksikSezonSatiri
            {
                AracId = a.Id,
                Plaka = a.AktifPlaka ?? "-",
                AracBilgisi = $"{a.Marka} {a.Model} {a.ModelYili}".Trim(),
                SonDegisimTarihi = dur?.SonDegisimTarihi,
                YazEksik = !yazVar,
                KisEksik = !kisVar,
                ToplamLastikSayisi = dur?.ToplamLastikSayisi ?? 0
            });
        }
        return sonuc
            .OrderBy(x => x.SonDegisimTarihi ?? DateTime.MinValue)
            .ThenBy(x => x.Plaka)
            .ToList();
    }

    public async Task<List<LastikKayipSatiri>> GetKayipLastikRaporuAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var kayiplar = await ctx.LastikStoklar
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Aktif && s.Durum == LastikDurum.Kayip)
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync();

        if (kayiplar.Count == 0)
            return new List<LastikKayipSatiri>();

        // İlişkili son değişim kaydını sökülen stok üzerinden eşle
        var stokIds = kayiplar.Select(k => k.Id).ToList();
        var degisimler = await ctx.LastikDegisimler
            .AsNoTracking()
            .Include(d => d.Arac)
            .Where(d => !d.IsDeleted && d.SokulenStokId != null && stokIds.Contains(d.SokulenStokId!.Value))
            .OrderByDescending(d => d.DegisimTarihi)
            .ToListAsync();

        var degisimByStok = degisimler
            .GroupBy(d => d.SokulenStokId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // Otomatik üretilen kayıplar için Notlar içinden Değişim Id'sini çek
        return kayiplar.Select(k =>
        {
            degisimByStok.TryGetValue(k.Id, out var deg);
            int? otoDegisimId = null;
            if (deg == null && !string.IsNullOrWhiteSpace(k.Notlar))
            {
                var idx = k.Notlar.IndexOf("Id=", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var rest = k.Notlar.Substring(idx + 3);
                    var num = new string(rest.TakeWhile(char.IsDigit).ToArray());
                    if (int.TryParse(num, out var parsed))
                        otoDegisimId = parsed;
                }
            }

            LastikDegisim? otoDeg = null;
            if (otoDegisimId.HasValue)
                otoDeg = degisimler.FirstOrDefault(d => d.Id == otoDegisimId.Value)
                         ?? ctx.LastikDegisimler.AsNoTracking().Include(x => x.Arac).FirstOrDefault(d => d.Id == otoDegisimId.Value);

            var kaynakDegisim = deg ?? otoDeg;

            return new LastikKayipSatiri
            {
                StokId = k.Id,
                Marka = k.Marka,
                Ebat = k.Ebat,
                Sezon = k.Sezon,
                SeriNo = k.SeriNo,
                KayipTarihi = kaynakDegisim?.DegisimTarihi ?? k.UpdatedAt ?? k.CreatedAt,
                KaynaklandigiAracId = kaynakDegisim?.AracId,
                KaynaklandigiPlaka = kaynakDegisim?.Arac?.AktifPlaka,
                DegisimId = kaynakDegisim?.Id,
                Notlar = k.Notlar
            };
        }).ToList();
    }

    public async Task<LastikStok?> KayipLastigiDepoyaAlAsync(int stokId, int depoId, string? not = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var stok = await ctx.LastikStoklar
            .FirstOrDefaultAsync(s => s.Id == stokId && !s.IsDeleted);

        if (stok == null)
            return null;

        if (stok.Durum != LastikDurum.Kayip)
            return await ctx.LastikStoklar
                .AsNoTracking()
                .Include(s => s.Depo)
                .Include(s => s.Arac)
                .FirstOrDefaultAsync(s => s.Id == stokId && !s.IsDeleted);

        stok.Durum = LastikDurum.Kullanilabilir;
        stok.DepoId = depoId;
        stok.AracId = null;
        stok.YedekMi = true;
        stok.UpdatedAt = DateTime.UtcNow;

        var ekNot = string.IsNullOrWhiteSpace(not) ? "Bulundu: Depoya alındı" : $"Bulundu: {not}";
        stok.Notlar = string.IsNullOrWhiteSpace(stok.Notlar) ? ekNot : $"{stok.Notlar} | {ekNot}";

        await ctx.SaveChangesAsync();

        return await ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Include(s => s.Arac)
            .FirstOrDefaultAsync(s => s.Id == stokId && !s.IsDeleted);
    }

    public async Task KayipLastigiCopeyAtAsync(int stokId, string? not, string? plaka, string? soforAdi)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var stok = await ctx.LastikStoklar
            .FirstOrDefaultAsync(s => s.Id == stokId && !s.IsDeleted);

        if (stok == null || stok.Durum != LastikDurum.Kayip)
            return;

        var tarih = DateTime.UtcNow;
        // Geçmiş kaydını Notlar'a işle – plaka/şoför değişse bile bu snapshot korunur
        var gecmis = $"##COPEAT##{tarih:O}|{plaka ?? "-"}|{soforAdi ?? "-"}|{not ?? ""}";
        stok.Notlar = string.IsNullOrWhiteSpace(stok.Notlar) ? gecmis : $"{stok.Notlar} | {gecmis}";
        stok.Durum = LastikDurum.Hurda;
        stok.Aktif = false;
        stok.AracId = null;
        stok.UpdatedAt = tarih;

        await ctx.SaveChangesAsync();
    }

    public async Task<List<LastikCopeyAtilanSatiri>> GetCopeyAtilanLastiklerAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var hurdalar = await ctx.LastikStoklar
            .AsNoTracking()
            .Where(s => !s.IsDeleted && !s.Aktif && s.Durum == LastikDurum.Hurda
                        && s.Notlar != null && s.Notlar.Contains("##COPEAT##"))
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync();

        return hurdalar.Select(s =>
        {
            // ##COPEAT##tarih|plaka|sofor|not
            DateTime? atilmaTarihi = null;
            string? plaka = null;
            string? soforAdi = null;
            string? ekNot = null;
            try
            {
                var idx = s.Notlar!.IndexOf("##COPEAT##", StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var parca = s.Notlar[(idx + 10)..];
                    // Sonraki | ayırıcısından önceki blok bu kaydın verisi
                    var son = parca.IndexOf(" | ##COPEAT##", StringComparison.Ordinal);
                    if (son > 0) parca = parca[..son];
                    var bolumler = parca.Split('|');
                    if (bolumler.Length >= 1) DateTime.TryParse(bolumler[0], out var dt);
                    if (bolumler.Length >= 1 && DateTime.TryParse(bolumler[0], out var dt2)) atilmaTarihi = dt2;
                    if (bolumler.Length >= 2) plaka = bolumler[1] == "-" ? null : bolumler[1];
                    if (bolumler.Length >= 3) soforAdi = bolumler[2] == "-" ? null : bolumler[2];
                    if (bolumler.Length >= 4) ekNot = bolumler[3];
                }
            }
            catch { /* parse hatası – alanlar boş kalır */ }

            return new LastikCopeyAtilanSatiri
            {
                StokId = s.Id,
                Marka = s.Marka,
                Ebat = s.Ebat,
                Sezon = s.Sezon,
                SeriNo = s.SeriNo,
                CopeyAtmaTarihi = atilmaTarihi ?? s.UpdatedAt ?? s.CreatedAt,
                Plaka = plaka,
                SoforAdi = soforAdi,
                Notlar = ekNot
            };
        }).ToList();
    }
    private static string FormatTakilanHareket(LastikDegisim degisim)
    {
        var payload = ParseDegisimPayload(degisim.Notlar);
        if (payload?.Satirlar != null && payload.Satirlar.Count > 0)
        {
            var satirlar = payload.Satirlar
                .Select(s => FormatSatirTakilan(s))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (satirlar.Count > 0)
                return string.Join(" || ", satirlar);
        }

        return FormatLastikHareket(degisim.TakilanStok, degisim.KaynakDepo?.DepoAdi, degisim.TakilanPozisyon);
    }

    private static string FormatSokulenHareket(LastikDegisim degisim)
    {
        var payload = ParseDegisimPayload(degisim.Notlar);
        if (payload?.Satirlar != null && payload.Satirlar.Count > 0)
        {
            var satirlar = payload.Satirlar
                .Select(s => FormatSatirSokulen(s))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (satirlar.Count > 0)
                return string.Join(" || ", satirlar);
        }

        return FormatLastikHareket(degisim.SokulenStok, degisim.HedefDepo?.DepoAdi, degisim.SokulenPozisyon);
    }

    private static string FormatSatirTakilan(LastikDegisimNotSatiri satir)
    {
        if (string.IsNullOrWhiteSpace(satir.TakilanEtiket))
            return string.Empty;

        var pozisyon = string.IsNullOrWhiteSpace(satir.Pozisyon) ? "Pozisyon?" : satir.Pozisyon;
        var depo = string.IsNullOrWhiteSpace(satir.KaynakDepoAdi) ? string.Empty : $" | {satir.KaynakDepoAdi}";
        return $"{pozisyon}: {satir.TakilanEtiket}{depo}";
    }

    private static string FormatSatirSokulen(LastikDegisimNotSatiri satir)
    {
        if (string.IsNullOrWhiteSpace(satir.SokulenEtiket))
            return string.Empty;

        var pozisyon = string.IsNullOrWhiteSpace(satir.Pozisyon) ? "Pozisyon?" : satir.Pozisyon;
        var depo = string.IsNullOrWhiteSpace(satir.HedefDepoAdi) ? string.Empty : $" | {satir.HedefDepoAdi}";
        return $"{pozisyon}: {satir.SokulenEtiket}{depo}";
    }

    private static LastikDegisimNotPayload? ParseDegisimPayload(string? notlar)
    {
        if (string.IsNullOrWhiteSpace(notlar))
            return null;

        try
        {
            return JsonSerializer.Deserialize<LastikDegisimNotPayload>(notlar);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatLastikHareket(LastikStok? stok, string? depoAdi, string? pozisyon)
    {
        if (stok == null)
            return "-";

        var lastik = $"{stok.Marka} {stok.Ebat} {GetSezonText(stok.Sezon)}".Trim();
        var depo = string.IsNullOrWhiteSpace(depoAdi) ? string.Empty : $" | {depoAdi}";
        var poz = string.IsNullOrWhiteSpace(pozisyon) ? string.Empty : $" | {pozisyon}";
        return $"{lastik}{depo}{poz}";
    }

    private static string GetSezonText(LastikSezon sezon) => sezon switch
    {
        LastikSezon.YazLastigi => "Yaz",
        LastikSezon.KisLastigi => "Kis",
        LastikSezon.DortMevsim => "4 Mevsim",
        _ => sezon.ToString()
    };

    private static string BuildSatinAlmaStokNotu(LastikDegisim degisim, LastikDegisimNotSatiri satir)
    {
        var parcalar = new List<string>
        {
            $"Lastik değişiminden otomatik oluşturuldu. DegisimId={degisim.Id}"
        };

        if (!string.IsNullOrWhiteSpace(satir.Pozisyon))
            parcalar.Add($"Pozisyon={satir.Pozisyon}");

        if (!string.IsNullOrWhiteSpace(satir.SatinAlmaTedarikciAdi))
            parcalar.Add($"Tedarikci={satir.SatinAlmaTedarikciAdi}");

        if (!string.IsNullOrWhiteSpace(satir.SatinAlmaNotu))
            parcalar.Add(satir.SatinAlmaNotu);

        return string.Join(" | ", parcalar);
    }

    // ─── Sezon Ayarları ──────────────────────────────────────────────────────

    public async Task<List<LastikSezonAyar>> GetSezonAyarlarAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikSezonAyarlari
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.SezonTipi)
            .ToListAsync();
    }

    public async Task<LastikSezonAyar> UpsertSezonAyarAsync(LastikSezonAyar ayar)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        if (ayar.Id == 0)
        {
            ayar.CreatedAt = DateTime.UtcNow;
            ctx.LastikSezonAyarlari.Add(ayar);
        }
        else
        {
            ayar.UpdatedAt = DateTime.UtcNow;
            ctx.LastikSezonAyarlari.Update(ayar);
        }
        await ctx.SaveChangesAsync();
        return ayar;
    }

    public async Task<LastikSezonDurum> GetSezonDurumAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var ayarlar = await ctx.LastikSezonAyarlari
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.Aktif)
            .ToListAsync();

        var bugun = DateTime.Today;
        LastikSezonAyar? aktif = null;
        LastikSezonAyar? sonraki = null;
        int? kalanGun = null;

        foreach (var ayar in ayarlar)
        {
            var (bas, bit) = SezonTarihAralik(ayar, bugun.Year);
            if (bugun >= bas && bugun <= bit)
            {
                aktif = ayar;
                break;
            }
        }

        // Sonraki sezonu ve kalan günü bul
        var enYakinGun = int.MaxValue;
        foreach (var ayar in ayarlar)
        {
            if (aktif != null && ayar.Id == aktif.Id) continue;
            // Bu yıl veya gelecek yıl başlangıcını dene
            foreach (var yilOffset in new[] { 0, 1 })
            {
                var (bas, _) = SezonTarihAralik(ayar, bugun.Year + yilOffset);
                if (bas >= bugun)
                {
                    var kalan = (int)(bas - bugun).TotalDays;
                    if (kalan < enYakinGun)
                    {
                        enYakinGun = kalan;
                        sonraki = ayar;
                        kalanGun = kalan;
                    }
                    break;
                }
            }
        }

        var uyariAktif = sonraki != null && kalanGun.HasValue && kalanGun.Value <= sonraki.UyariOncesiGun;

        // Değişim gereken araçları hesapla
        var degisimGerekenler = new List<LastikSezonDegisimGereken>();
        if (aktif != null)
        {
            var beklenenSezon = aktif.SezonTipi == LastikSezonTipi.Yaz ? LastikSezon.YazLastigi : LastikSezon.KisLastigi;

            var araclar = await ctx.Araclar
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .Select(a => new { a.Id, a.AktifPlaka, a.Marka, a.Model, a.ModelYili, a.SahiplikTipi })
                .OrderBy(a => a.AktifPlaka)
                .ToListAsync();

            var takiliLastikler = await ctx.LastikStoklar
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.Aktif && s.AracId != null)
                .Select(s => new { s.AracId, s.Sezon })
                .ToListAsync();

            var takiliByArac = takiliLastikler
                .GroupBy(s => s.AracId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var (sezonBas, _) = SezonTarihAralik(aktif, bugun.Year);
            var mevsimlikDegisimler = await ctx.LastikDegisimler
                .AsNoTracking()
                .Where(d => !d.IsDeleted
                    && d.DegisimTipi == LastikDegisimTipi.Mevsimlik)
                .Select(d => new { d.AracId, d.DegisimTarihi })
                .ToListAsync();

            var sonDegisimByArac = mevsimlikDegisimler
                .GroupBy(d => d.AracId)
                .ToDictionary(g => g.Key, g => (DateTime?)g.Max(x => x.DegisimTarihi));

            // CreatedAt fallback: değişim kaydı yoksa stok ekleme tarihini kullan
            var stokCreatedAtByArac = await ctx.LastikStoklar
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.Aktif && s.AracId != null)
                .GroupBy(s => s.AracId!.Value)
                .Select(g => new { AracId = g.Key, MaxCreatedAt = g.Max(x => x.CreatedAt) })
                .ToListAsync();
            var createdAtByArac = stokCreatedAtByArac.ToDictionary(x => x.AracId, x => (DateTime?)x.MaxCreatedAt);

            foreach (var a in araclar)
            {
                takiliByArac.TryGetValue(a.Id, out var liste);
                liste ??= new();

                var dogruSezonVar = liste.Any(x => x.Sezon == beklenenSezon || x.Sezon == LastikSezon.DortMevsim);

                sonDegisimByArac.TryGetValue(a.Id, out var sonDegisim);
                // Değişim tarihi yoksa stok ekleme tarihini kullan
                var sonDegisimTarihi = sonDegisim ?? createdAtByArac.GetValueOrDefault(a.Id);

                var sezonOzeti = liste.Count == 0
                    ? "Lastik yok"
                    : string.Join(", ", liste
                        .GroupBy(x => x.Sezon)
                        .Select(g => $"{GetSezonText2(g.Key)} x{g.Count()}"));

                degisimGerekenler.Add(new LastikSezonDegisimGereken
                {
                    AracId = a.Id,
                    Plaka = a.AktifPlaka ?? "-",
                    AracBilgisi = $"{a.Marka} {a.Model} {a.ModelYili}".Trim(),
                    TakiliSezonOzeti = sezonOzeti,
                    BuSezonDegisimYapildi = dogruSezonVar,
                    SonDegisimTarihi = sonDegisimTarihi,
                    SahiplikTipi = a.SahiplikTipi
                });
            }

            degisimGerekenler = degisimGerekenler
                .OrderBy(x => x.SonDegisimTarihi ?? DateTime.MinValue)
                .ThenBy(x => x.Plaka)
                .ToList();
        }

        return new LastikSezonDurum
        {
            AktifSezon = aktif,
            SonrakiSezon = sonraki,
            SonrakiSezonKalanGun = kalanGun,
            UyariAktif = uyariAktif,
            DegisimGerekenler = degisimGerekenler
        };
    }

    /// <summary>Bir sezon ayarının bu yıl içindeki başlangıç-bitiş tarih çiftini döner.</summary>
    private static (DateTime Baslangic, DateTime Bitis) SezonTarihAralik(LastikSezonAyar ayar, int yil)
    {
        var bas = new DateTime(yil, ayar.BaslangicAyi, Math.Min(ayar.BaslangicGunu, DateTime.DaysInMonth(yil, ayar.BaslangicAyi)));
        var bitisYil = ayar.BitisAyi < ayar.BaslangicAyi ? yil + 1 : yil;
        var bit = new DateTime(bitisYil, ayar.BitisAyi, Math.Min(ayar.BitisGunu, DateTime.DaysInMonth(bitisYil, ayar.BitisAyi)));
        return (bas, bit);
    }

    private static string GetSezonText2(LastikSezon sezon) => sezon switch
    {
        LastikSezon.YazLastigi => "Yaz",
        LastikSezon.KisLastigi => "Kış",
        LastikSezon.DortMevsim => "Dört Mevsim",
        _ => sezon.ToString()
    };

    private static async Task<Dictionary<int, PlakaSezonDurumOzet>> GetPlakaSezonDurumMapAsync(ApplicationDbContext ctx)
    {
        var stoklar = await ctx.LastikStoklar
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Aktif && (s.AracId != null || s.KaynakAracId != null))
            .Select(s => new { AracId = s.AracId ?? s.KaynakAracId, s.Sezon, s.CreatedAt })
            .ToListAsync();

        // Stok ekleme tarihini fallback olarak kullan (gerçek değişim yoksa)
        var createdAtByArac = stoklar
            .GroupBy(s => s.AracId!.Value)
            .ToDictionary(g => g.Key, g => (DateTime?)g.Max(x => x.CreatedAt));

        var map = stoklar
            .GroupBy(s => s.AracId!.Value)
            .ToDictionary(
                g => g.Key,
                g => new PlakaSezonDurumOzet
                {
                    ToplamLastikSayisi = g.Count(),
                    YazVar = g.Any(x => x.Sezon == LastikSezon.YazLastigi || x.Sezon == LastikSezon.DortMevsim),
                    KisVar = g.Any(x => x.Sezon == LastikSezon.KisLastigi || x.Sezon == LastikSezon.DortMevsim),
                    SonDegisimTarihi = createdAtByArac.GetValueOrDefault(g.Key)
                });

        var sonDegisimler = await ctx.LastikDegisimler
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .GroupBy(d => d.AracId)
            .Select(g => new { AracId = g.Key, SonDegisimTarihi = g.Max(x => x.DegisimTarihi) })
            .ToListAsync();

        foreach (var d in sonDegisimler)
        {
            if (map.TryGetValue(d.AracId, out var mevcut))
            {
                // Gerçek değişim tarihi varsa onu kullan (ekleme tarihini ezer)
                mevcut.SonDegisimTarihi = d.SonDegisimTarihi;
            }
            else
            {
                map[d.AracId] = new PlakaSezonDurumOzet
                {
                    ToplamLastikSayisi = 0,
                    YazVar = false,
                    KisVar = false,
                    SonDegisimTarihi = d.SonDegisimTarihi
                };
            }
        }

        return map;
    }
}

internal sealed class LastikAracaTakiliSatir
{
    public int AracId { get; set; }
    public string? Marka { get; set; }
    public string Ebat { get; set; } = string.Empty;
    public LastikSezon Sezon { get; set; }
}

internal sealed class PlakaSezonDurumOzet
{
    public int ToplamLastikSayisi { get; set; }
    public bool YazVar { get; set; }
    public bool KisVar { get; set; }
    public DateTime? SonDegisimTarihi { get; set; }
}

internal sealed class LastikDegisimNotPayload
{
    public bool DortluSetBazli { get; set; } = true;
    public int BazPozisyonAdedi { get; set; } = 4;
    public string? GirisTipi { get; set; }
    public int Adet { get; set; }
    public int SetAdedi { get; set; } = 4;
    public int IslemGorenAdet { get; set; }
    public string? UserNot { get; set; }
    public LastikDegisimFinansNotu? Finans { get; set; }
    public LastikDegisimFinansNotu? SatinAlma { get; set; }
    public List<LastikDegisimNotSatiri> Satirlar { get; set; } = new();
}

internal sealed class LastikDegisimNotSatiri
{
    public bool DegisimYapildi { get; set; }
    public bool? IslemYapildiMi { get; set; }
    public string? Pozisyon { get; set; }
    public string? TeminSekli { get; set; }

    public int? TakilanStokId { get; set; }
    public string? TakilanEtiket { get; set; }
    public int? KaynakDepoId { get; set; }
    public string? KaynakDepoAdi { get; set; }

    public int? SokulenStokId { get; set; }
    public string? SokulenEtiket { get; set; }
    public int? HedefDepoId { get; set; }
    public string? HedefDepoAdi { get; set; }

    /// <summary>
    /// Sökülen lastiğin akibeti: "depo" = depoda kullanılabilir, "tamir" = tamire gönderildi, "cop" = hurdaya çıkarıldı.
    /// Null veya boş ise varsayılan davranış (HedefDepoId varsa depoya, yoksa kayıp).
    /// </summary>
    public string? SokulenAkibet { get; set; }

    public string? SatinAlmaTedarikciAdi { get; set; }
    public string? SatinAlmaMarka { get; set; }
    public string? SatinAlmaEbat { get; set; }
    public LastikSezon? SatinAlmaSezon { get; set; }
    public string? SatinAlmaSeriNo { get; set; }
    public string? SatinAlmaNotu { get; set; }
}

internal sealed class LastikDegisimFinansNotu
{
    public int? MasrafId { get; set; }
    public string? TedarikciAdi { get; set; }
    public bool SatinAlmaVar { get; set; }
    public int SatinAlinanAdet { get; set; }
    public decimal? SatinAlmaToplamTutari { get; set; }
    public decimal? ToplamTutar { get; set; }
    public bool FaturasizAlim { get; set; }
    public bool FaturasizMi { get; set; }
    public string? FaturaNo { get; set; }
    public DateTime? FaturaTarihi { get; set; }
    public bool OdemeYapildi { get; set; }
    public DateTime? OdemeTarihi { get; set; }
    public int? OdemeHesapId { get; set; }
    public string? OdemeHesapAdi { get; set; }
    public HesapTipi? OdemeHesapTipi { get; set; }
    public string? OdemeNotu { get; set; }
}

internal static class LastikTeminSekilleri
{
    public const string Stoktan = "stok";
    public const string SatinAlma = "satin_alma";
}



