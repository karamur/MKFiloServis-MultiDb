using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Operasyonel hakediş servisi: yeni Hakediş entity'si üzerinden çalışır.
/// FiloGunlukPuantaj kayıtlarından dönemsel hakediş üretir.
/// </summary>
public class OperasyonelHakedisService : IOperasyonelHakedisService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public OperasyonelHakedisService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Hakedis>> GetHakedislerAsync(int? yil = null, int? ay = null, HakedisTipi? tip = null, int? referansId = null, HakedisDurum? durum = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var q = context.Hakedisler
            .Include(h => h.Detaylar)
            .Include(h => h.Fatura)
            .AsQueryable();

        if (yil.HasValue) q = q.Where(h => h.Yil == yil.Value);
        if (ay.HasValue) q = q.Where(h => h.Ay == ay.Value);
        if (tip.HasValue) q = q.Where(h => h.Tip == tip.Value);
        if (referansId.HasValue) q = q.Where(h => h.ReferansId == referansId.Value);
        if (durum.HasValue) q = q.Where(h => h.Durum == durum.Value);

        return await q.OrderByDescending(h => h.Yil)
                      .ThenByDescending(h => h.Ay)
                      .ThenBy(h => h.Tip)
                      .ToListAsync();
    }

    public async Task<Hakedis?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Hakedisler
            .Include(h => h.Detaylar).ThenInclude(d => d.Arac)
            .Include(h => h.Detaylar).ThenInclude(d => d.Sofor)
            .Include(h => h.Detaylar).ThenInclude(d => d.Guzergah)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public Task<Hakedis> KurumHakedisiUretAsync(int kurumFirmaId, int yil, int ay) =>
        UretInternalAsync(HakedisTipi.Kurum, kurumFirmaId, yil, ay);

    public Task<Hakedis> TedarikciHakedisiUretAsync(int tasimaTedarikciId, int yil, int ay) =>
        UretInternalAsync(HakedisTipi.Tedarikci, tasimaTedarikciId, yil, ay);

    public Task<Hakedis> AracHakedisiUretAsync(int aracId, int yil, int ay) =>
        UretInternalAsync(HakedisTipi.Arac, aracId, yil, ay);

    private async Task<Hakedis> UretInternalAsync(HakedisTipi tip, int referansId, int yil, int ay)
    {
        if (yil < 2000 || ay < 1 || ay > 12)
            throw new ArgumentException("Geçersiz dönem (yıl/ay).");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var donemBas = new DateTime(yil, ay, 1);
        var donemSon = donemBas.AddMonths(1);

        // Kaynak puantaj sorgusu
        IQueryable<FiloGunlukPuantaj> q = context.FiloGunlukPuantajlar
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .Include(p => p.Guzergah)
            .Where(p => p.Tarih >= donemBas && p.Tarih < donemSon);

        switch (tip)
        {
            case HakedisTipi.Kurum:
                q = q.Where(p => p.KurumFirmaId == referansId);
                break;
            case HakedisTipi.Tedarikci:
                q = q.Where(p => p.Arac != null
                                 && p.Arac.SahiplikTipi == AracSahiplikTipi.Tedarikci
                                 && p.Arac.TasimaTedarikciId == referansId);
                break;
            case HakedisTipi.Arac:
                q = q.Where(p => p.AracId == referansId);
                break;
        }

        var puantajlar = await q.OrderBy(p => p.Tarih).ToListAsync();

        // 🔧 FIX: Sefer sayısı 0 olan kayıtları filtrele (henüz doldurulmamış puantajlar)
        puantajlar = puantajlar.Where(p => p.SeferSayisi > 0).ToList();

        // Mevcut taslak varsa üzerine yaz, yoksa yeni
        var hakedis = await context.Hakedisler
            .Include(h => h.Detaylar)
            .FirstOrDefaultAsync(h => h.Tip == tip && h.ReferansId == referansId
                                      && h.Yil == yil && h.Ay == ay
                                      && h.Durum == HakedisDurum.Taslak);

        if (hakedis == null)
        {
            hakedis = new Hakedis
            {
                Tip = tip,
                ReferansId = referansId,
                Yil = yil,
                Ay = ay,
                Durum = HakedisDurum.Taslak,
                CreatedAt = DateTime.UtcNow
            };
            context.Hakedisler.Add(hakedis);
        }
        else
        {
            // Eski detayları temizle
            context.HakedisDetaylari.RemoveRange(hakedis.Detaylar);
            hakedis.Detaylar.Clear();
            hakedis.UpdatedAt = DateTime.UtcNow;
        }

        decimal toplamSefer = 0m;
        decimal toplamTutar = 0m;

        foreach (var p in puantajlar)
        {
            decimal birim = tip switch
            {
                HakedisTipi.Kurum => p.TahakkukEdenKurumUcreti > 0 && p.SeferSayisi > 0
                                        ? Math.Round(p.TahakkukEdenKurumUcreti / p.SeferSayisi, 2)
                                        : p.TahakkukEdenKurumUcreti,
                HakedisTipi.Tedarikci => p.TahakkukEdenTaseronUcreti > 0 && p.SeferSayisi > 0
                                        ? Math.Round(p.TahakkukEdenTaseronUcreti / p.SeferSayisi, 2)
                                        : p.TahakkukEdenTaseronUcreti,
                HakedisTipi.Arac => p.MaliyetOzmalKiralik ?? 0m,
                _ => 0m
            };

            decimal tutar = tip switch
            {
                HakedisTipi.Kurum => p.TahakkukEdenKurumUcreti,
                HakedisTipi.Tedarikci => p.TahakkukEdenTaseronUcreti,
                HakedisTipi.Arac => (p.MaliyetOzmalKiralik ?? 0m) * p.SeferSayisi,
                _ => 0m
            };

            var detay = new HakedisDetay
            {
                Hakedis = hakedis,
                Tarih = p.Tarih,
                ServisTuru = p.ServisTuru,
                AracId = p.AracId,
                SoforId = p.SoforId,
                GuzergahId = p.GuzergahId,
                FiloGunlukPuantajId = p.Id,
                SeferSayisi = p.SeferSayisi,
                BirimFiyat = birim,
                Tutar = tutar,
                CreatedAt = DateTime.UtcNow
            };

            hakedis.Detaylar.Add(detay);
            toplamSefer += p.SeferSayisi;
            toplamTutar += tutar;
        }

        hakedis.ToplamSeferSayisi = toplamSefer;
        hakedis.Tutar = toplamTutar;
        hakedis.BirimFiyat = toplamSefer > 0 ? Math.Round(toplamTutar / toplamSefer, 2) : 0m;
        hakedis.KdvTutar = Math.Round(toplamTutar * (hakedis.KdvOran / 100m), 2);
        hakedis.GenelToplam = toplamTutar + hakedis.KdvTutar;

        await context.SaveChangesAsync();
        return hakedis;
    }

    public async Task<Hakedis> OnaylaAsync(int hakedisId, string onaylayanKisi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var h = await context.Hakedisler.FindAsync(hakedisId)
                ?? throw new InvalidOperationException("Hakediş bulunamadı.");

        if (h.Durum != HakedisDurum.Taslak)
            throw new InvalidOperationException("Sadece Taslak hakediş onaylanabilir.");

        h.Durum = HakedisDurum.Onaylandi;
        h.OnaylayanKisi = onaylayanKisi;
        h.OnayTarihi = DateTime.UtcNow;
        h.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return h;
    }

    public async Task<Hakedis> IptalEtAsync(int hakedisId, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var h = await context.Hakedisler.FindAsync(hakedisId)
                ?? throw new InvalidOperationException("Hakediş bulunamadı.");

        if (h.Durum == HakedisDurum.Faturalandi || h.Durum == HakedisDurum.TahsilEdildi || h.Durum == HakedisDurum.Odendi)
            throw new InvalidOperationException("Faturalanmış / kapanmış hakediş iptal edilemez.");

        h.Durum = HakedisDurum.Iptal;
        if (!string.IsNullOrWhiteSpace(aciklama))
            h.Notlar = string.IsNullOrEmpty(h.Notlar) ? aciklama : $"{h.Notlar}\n{aciklama}";
        h.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return h;
    }

    public async Task<Fatura> FaturayaDonustureAsync(int hakedisId, DateTime faturaTarihi, string? faturaNo = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var h = await context.Hakedisler.FirstOrDefaultAsync(x => x.Id == hakedisId)
                ?? throw new InvalidOperationException("Hakediş bulunamadı.");

        if (h.Durum != HakedisDurum.Onaylandi)
            throw new InvalidOperationException("Sadece Onaylanmış hakediş faturaya dönüştürülebilir.");

        if (h.Tip == HakedisTipi.Arac)
            throw new InvalidOperationException("Araç tipi hakediş faturalanmaz (iç maliyet/karlılık raporu).");

        // Cari'yi belirle
        int? cariId = null;
        if (h.Tip == HakedisTipi.Kurum)
        {
            // Kurum = Firma; Firma'nın varsayılan cari kaydını ara
            var firma = await context.Firmalar.FirstOrDefaultAsync(f => f.Id == h.ReferansId);
            if (firma == null) throw new InvalidOperationException("Kurum (Firma) bulunamadı.");
            // İsim üzerinden eşleşen cariyi al (basit varsayım)
            var cari = await context.Cariler.FirstOrDefaultAsync(c => c.Unvan == firma.FirmaAdi || c.Unvan == firma.UnvanTam);
            cariId = cari?.Id;
        }
        else if (h.Tip == HakedisTipi.Tedarikci)
        {
            var tedarikci = await context.TasimaTedarikciler.FirstOrDefaultAsync(t => t.Id == h.ReferansId)
                            ?? throw new InvalidOperationException("Tedarikçi bulunamadı.");
            cariId = tedarikci.CariId;
        }

        if (cariId == null || cariId == 0)
            throw new InvalidOperationException("Hakediş için Cari kaydı bulunamadı; faturadan önce Cari eşlemesi yapılmalıdır.");

        var fatura = new Fatura
        {
            FaturaNo = faturaNo ?? $"HKD-{h.Tip}-{h.Yil}{h.Ay:D2}-{h.Id}",
            FaturaTarihi = faturaTarihi,
            FaturaTipi = h.Tip == HakedisTipi.Kurum ? FaturaTipi.SatisFaturasi : FaturaTipi.AlisFaturasi,
            FaturaYonu = h.Tip == HakedisTipi.Kurum ? FaturaYonu.Giden : FaturaYonu.Gelen,
            Durum = FaturaDurum.Beklemede,
            EFaturaTipi = EFaturaTipi.EArsiv,
            CariId = cariId.Value,
            AraToplam = h.Tutar,
            KdvOrani = h.KdvOran,
            KdvTutar = h.KdvTutar,
            GenelToplam = h.GenelToplam,
            HakedisId = h.Id,
            Aciklama = $"{h.Yil}/{h.Ay:D2} dönemi {h.Tip} hakedişi (#{h.Id})",
            ImportKaynak = "Hakedis",
            CreatedAt = DateTime.UtcNow
        };

        context.Faturalar.Add(fatura);
        await context.SaveChangesAsync();

        h.FaturaId = fatura.Id;
        h.Durum = HakedisDurum.Faturalandi;
        h.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return fatura;
    }

    public async Task<bool> SilAsync(int hakedisId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var h = await context.Hakedisler
            .Include(x => x.Detaylar)
            .FirstOrDefaultAsync(x => x.Id == hakedisId);
        if (h == null) return false;

        if (h.Durum == HakedisDurum.Faturalandi || h.Durum == HakedisDurum.TahsilEdildi || h.Durum == HakedisDurum.Odendi)
            throw new InvalidOperationException("Faturalanmış / kapanmış hakediş silinemez.");

        context.HakedisDetaylari.RemoveRange(h.Detaylar);
        context.Hakedisler.Remove(h);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<HakedisOnizleme> OnizleAsync(HakedisTipi tip, int referansId, int yil, int ay)
    {
        if (yil < 2000 || ay < 1 || ay > 12)
            throw new ArgumentException("Geçersiz dönem (yıl/ay).");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var donemBas = new DateTime(yil, ay, 1);
        var donemSon = donemBas.AddMonths(1);

        IQueryable<FiloGunlukPuantaj> q = context.FiloGunlukPuantajlar
            .Where(p => p.Tarih >= donemBas && p.Tarih < donemSon);

        switch (tip)
        {
            case HakedisTipi.Kurum:
                q = q.Where(p => p.KurumFirmaId == referansId);
                break;
            case HakedisTipi.Tedarikci:
                q = q.Where(p => p.Arac != null
                                 && p.Arac.SahiplikTipi == AracSahiplikTipi.Tedarikci
                                 && p.Arac.TasimaTedarikciId == referansId);
                break;
            case HakedisTipi.Arac:
                q = q.Where(p => p.AracId == referansId);
                break;
        }

        var ozet = await q.GroupBy(_ => 1).Select(g => new
        {
            Adet = g.Count(),
            Sefer = g.Sum(x => (decimal?)x.SeferSayisi) ?? 0m,
            Kurum = g.Sum(x => (decimal?)x.TahakkukEdenKurumUcreti) ?? 0m,
            Taseron = g.Sum(x => (decimal?)x.TahakkukEdenTaseronUcreti) ?? 0m,
            Ozmal = g.Sum(x => (decimal?)((x.MaliyetOzmalKiralik ?? 0m) * x.SeferSayisi)) ?? 0m
        }).FirstOrDefaultAsync();

        decimal tahminiTutar = tip switch
        {
            HakedisTipi.Kurum => ozet?.Kurum ?? 0m,
            HakedisTipi.Tedarikci => ozet?.Taseron ?? 0m,
            HakedisTipi.Arac => ozet?.Ozmal ?? 0m,
            _ => 0m
        };

        var mevcut = await context.Hakedisler
            .Where(h => h.Tip == tip && h.ReferansId == referansId && h.Yil == yil && h.Ay == ay
                        && h.Durum != HakedisDurum.Iptal)
            .OrderByDescending(h => h.Id)
            .Select(h => new { h.Id, h.Durum })
            .FirstOrDefaultAsync();

        return new HakedisOnizleme(
            PuantajSayisi: ozet?.Adet ?? 0,
            ToplamSefer: ozet?.Sefer ?? 0m,
            TahminiTutar: tahminiTutar,
            MevcutTaslakVar: mevcut?.Durum == HakedisDurum.Taslak,
            MevcutOnayliVar: mevcut != null && mevcut.Durum != HakedisDurum.Taslak,
            MevcutHakedisId: mevcut?.Id,
            MevcutHakedisDurum: mevcut?.Durum
        );
    }

    public async Task<TopluHakedisSonuc> TopluUretAsync(HakedisTipi tip, int yil, int ay)
    {
        if (yil < 2000 || ay < 1 || ay > 12)
            throw new ArgumentException("Geçersiz dönem (yıl/ay).");

        var donemBas = new DateTime(yil, ay, 1);
        var donemSon = donemBas.AddMonths(1);

        // 1) Dönem puantajlarından distinct referans id'leri çek
        List<int> referansIds;
        Dictionary<int, string> referansAdlari = new();

        await using (var ctx = await _contextFactory.CreateDbContextAsync())
        {
            switch (tip)
            {
                case HakedisTipi.Kurum:
                    referansIds = await ctx.FiloGunlukPuantajlar
                        .Where(p => p.Tarih >= donemBas && p.Tarih < donemSon)
                        .Select(p => p.KurumFirmaId)
                        .Distinct()
                        .ToListAsync();
                    referansAdlari = await ctx.Firmalar
                        .Where(f => referansIds.Contains(f.Id))
                        .ToDictionaryAsync(f => f.Id, f => f.FirmaAdi);
                    break;

                case HakedisTipi.Tedarikci:
                    referansIds = await ctx.FiloGunlukPuantajlar
                        .Where(p => p.Tarih >= donemBas && p.Tarih < donemSon
                                    && p.Arac != null
                                    && p.Arac.SahiplikTipi == AracSahiplikTipi.Tedarikci
                                    && p.Arac.TasimaTedarikciId != null)
                        .Select(p => p.Arac!.TasimaTedarikciId!.Value)
                        .Distinct()
                        .ToListAsync();
                    referansAdlari = await ctx.TasimaTedarikciler
                        .Where(t => referansIds.Contains(t.Id))
                        .ToDictionaryAsync(t => t.Id, t => t.Unvan);
                    break;

                case HakedisTipi.Arac:
                    referansIds = await ctx.FiloGunlukPuantajlar
                        .Where(p => p.Tarih >= donemBas && p.Tarih < donemSon)
                        .Select(p => p.AracId)
                        .Distinct()
                        .ToListAsync();
                    referansAdlari = await ctx.Araclar
                        .Where(a => referansIds.Contains(a.Id))
                        .ToDictionaryAsync(a => a.Id, a => a.Plaka);
                    break;

                default:
                    referansIds = new List<int>();
                    break;
            }
        }

        var satirlar = new List<TopluHakedisSatir>();
        int uretilen = 0, atlanan = 0, hatali = 0;
        decimal toplamTutar = 0m;

        foreach (var refId in referansIds)
        {
            var ad = referansAdlari.TryGetValue(refId, out var n) ? n : $"#{refId}";

            // Mevcut Onaylı/Faturalı/Tahsil/Ödendi kayıt varsa atla
            await using (var ctx = await _contextFactory.CreateDbContextAsync())
            {
                var mevcut = await ctx.Hakedisler
                    .Where(h => h.Tip == tip && h.ReferansId == refId && h.Yil == yil && h.Ay == ay
                                && h.Durum != HakedisDurum.Iptal && h.Durum != HakedisDurum.Taslak)
                    .Select(h => new { h.Id, h.Durum })
                    .FirstOrDefaultAsync();

                if (mevcut != null)
                {
                    atlanan++;
                    satirlar.Add(new TopluHakedisSatir(
                        refId, ad, "Atlandı",
                        $"Mevcut {mevcut.Durum} hakediş (#{mevcut.Id}) bulunduğu için atlandı.",
                        mevcut.Id, 0m, 0m));
                    continue;
                }
            }

            try
            {
                var h = await UretInternalAsync(tip, refId, yil, ay);
                uretilen++;
                toplamTutar += h.GenelToplam;
                satirlar.Add(new TopluHakedisSatir(
                    refId, ad, "Üretildi", null, h.Id, h.GenelToplam, h.ToplamSeferSayisi));
            }
            catch (Exception ex)
            {
                hatali++;
                satirlar.Add(new TopluHakedisSatir(
                    refId, ad, "Hata", ex.GetBaseException().Message, null, 0m, 0m));
            }
        }

        return new TopluHakedisSonuc(
            ToplamReferans: referansIds.Count,
            UretilenAdet: uretilen,
            AtlananAdet: atlanan,
            HataliAdet: hatali,
            ToplamTutar: toplamTutar,
            Satirlar: satirlar.OrderByDescending(s => s.Tutar).ToList()
        );
    }

    public async Task<TopluIslemSonuc> TopluOnaylaAsync(IEnumerable<int> hakedisIds, string onaylayanKisi)
    {
        var ids = hakedisIds?.Distinct().ToList() ?? new List<int>();
        var satirlar = new List<TopluIslemSatir>();
        int basarili = 0, atlanan = 0, hatali = 0;

        if (ids.Count == 0)
            return new TopluIslemSonuc(0, 0, 0, 0, satirlar);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedisler = await context.Hakedisler
            .Where(h => ids.Contains(h.Id))
            .ToListAsync();

        var bulunanIds = hakedisler.Select(h => h.Id).ToHashSet();
        foreach (var eksikId in ids.Where(i => !bulunanIds.Contains(i)))
        {
            hatali++;
            satirlar.Add(new TopluIslemSatir(eksikId, "Hata", "Hakediş bulunamadı."));
        }

        var simdi = DateTime.UtcNow;
        foreach (var h in hakedisler)
        {
            if (h.Durum != HakedisDurum.Taslak)
            {
                atlanan++;
                satirlar.Add(new TopluIslemSatir(h.Id, "Atlandı", $"Durum {h.Durum}; sadece Taslak onaylanabilir."));
                continue;
            }

            try
            {
                h.Durum = HakedisDurum.Onaylandi;
                h.OnaylayanKisi = onaylayanKisi;
                h.OnayTarihi = simdi;
                h.UpdatedAt = simdi;
                basarili++;
                satirlar.Add(new TopluIslemSatir(h.Id, "Tamam", null));
            }
            catch (Exception ex)
            {
                hatali++;
                satirlar.Add(new TopluIslemSatir(h.Id, "Hata", ex.GetBaseException().Message));
            }
        }

        await context.SaveChangesAsync();

        return new TopluIslemSonuc(ids.Count, basarili, atlanan, hatali,
            satirlar.OrderBy(s => s.HakedisId).ToList());
    }

    public async Task<TopluIslemSonuc> TopluSilAsync(IEnumerable<int> hakedisIds)
    {
        var ids = hakedisIds?.Distinct().ToList() ?? new List<int>();
        var satirlar = new List<TopluIslemSatir>();
        int basarili = 0, atlanan = 0, hatali = 0;

        if (ids.Count == 0)
            return new TopluIslemSonuc(0, 0, 0, 0, satirlar);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedisler = await context.Hakedisler
            .Include(h => h.Detaylar)
            .Where(h => ids.Contains(h.Id))
            .ToListAsync();

        var bulunanIds = hakedisler.Select(h => h.Id).ToHashSet();
        foreach (var eksikId in ids.Where(i => !bulunanIds.Contains(i)))
        {
            hatali++;
            satirlar.Add(new TopluIslemSatir(eksikId, "Hata", "Hakediş bulunamadı."));
        }

        foreach (var h in hakedisler)
        {
            if (h.Durum == HakedisDurum.Faturalandi || h.Durum == HakedisDurum.TahsilEdildi || h.Durum == HakedisDurum.Odendi)
            {
                atlanan++;
                satirlar.Add(new TopluIslemSatir(h.Id, "Atlandı", $"Durum {h.Durum}; faturalanmış/kapanmış hakediş silinemez."));
                continue;
            }

            try
            {
                context.HakedisDetaylari.RemoveRange(h.Detaylar);
                context.Hakedisler.Remove(h);
                basarili++;
                satirlar.Add(new TopluIslemSatir(h.Id, "Tamam", null));
            }
            catch (Exception ex)
            {
                hatali++;
                satirlar.Add(new TopluIslemSatir(h.Id, "Hata", ex.GetBaseException().Message));
            }
        }

        await context.SaveChangesAsync();

        return new TopluIslemSonuc(ids.Count, basarili, atlanan, hatali,
            satirlar.OrderBy(s => s.HakedisId).ToList());
    }

    public async Task<TopluIslemSonuc> TopluFaturalaAsync(IEnumerable<int> hakedisIds, DateTime faturaTarihi)
    {
        var ids = hakedisIds?.Distinct().ToList() ?? new List<int>();
        var satirlar = new List<TopluIslemSatir>();
        int basarili = 0, atlanan = 0, hatali = 0;

        if (ids.Count == 0)
            return new TopluIslemSonuc(0, 0, 0, 0, satirlar);

        // Önce hızlı ön-tarama: bulunmayanları ve uygun olmayanları işaretle
        await using (var ctx = await _contextFactory.CreateDbContextAsync())
        {
            var mevcut = await ctx.Hakedisler
                .Where(h => ids.Contains(h.Id))
                .Select(h => new { h.Id, h.Durum, h.Tip })
                .ToListAsync();

            var mevcutMap = mevcut.ToDictionary(x => x.Id);

            foreach (var id in ids)
            {
                if (!mevcutMap.TryGetValue(id, out var info))
                {
                    hatali++;
                    satirlar.Add(new TopluIslemSatir(id, "Hata", "Hakediş bulunamadı."));
                    continue;
                }

                if (info.Tip == HakedisTipi.Arac)
                {
                    atlanan++;
                    satirlar.Add(new TopluIslemSatir(id, "Atlandı", "Araç tipi hakediş faturalanmaz."));
                    continue;
                }

                if (info.Durum != HakedisDurum.Onaylandi)
                {
                    atlanan++;
                    satirlar.Add(new TopluIslemSatir(id, "Atlandı", $"Durum {info.Durum}; sadece Onaylanmış faturalanabilir."));
                    continue;
                }

                // Faturalanabilir → asıl çağrıyı yap
                try
                {
                    var fatura = await FaturayaDonustureAsync(id, faturaTarihi);
                    basarili++;
                    satirlar.Add(new TopluIslemSatir(id, "Tamam", $"Fatura #{fatura.Id} ({fatura.FaturaNo})", fatura.Id, fatura.FaturaNo));
                }
                catch (Exception ex)
                {
                    hatali++;
                    satirlar.Add(new TopluIslemSatir(id, "Hata", ex.GetBaseException().Message));
                }
            }
        }

        return new TopluIslemSonuc(ids.Count, basarili, atlanan, hatali,
            satirlar.OrderBy(s => s.HakedisId).ToList());
    }
}


