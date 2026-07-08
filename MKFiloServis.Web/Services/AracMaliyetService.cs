using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Aylık araç maliyet snapshot üretici servis.
/// Özmal ve Kiralık araçlar için yakıt/bakım/lastik/sigorta/plaka kirası vb. masrafları
/// AracMasraf, LastikDegisim ve KiralikPlakaTakip kayıtlarından toplulaştırır.
/// </summary>
public class AracMaliyetService : IAracMaliyetService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public AracMaliyetService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<AracMaliyetSnapshot> SnapshotUretAsync(int aracId, int yil, int ay)
    {
        if (yil < 2000 || ay < 1 || ay > 12)
            throw new ArgumentException("Geçersiz dönem.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId)
                   ?? throw new InvalidOperationException("Araç bulunamadı.");

        if (arac.SahiplikTipi == AracSahiplikTipi.Tedarikci)
            throw new InvalidOperationException("Tedarikçi araçları için maliyet snapshot üretilmez (maliyet sahibine aittir).");

        var donemBas = new DateTime(yil, ay, 1);
        var donemSon = donemBas.AddMonths(1);

        // Mevcut snapshot varsa üzerine yaz
        var snap = await context.AracMaliyetSnapshotlari
            .FirstOrDefaultAsync(s => s.AracId == aracId && s.Yil == yil && s.Ay == ay);

        if (snap == null)
        {
            snap = new AracMaliyetSnapshot
            {
                AracId = aracId,
                Yil = yil,
                Ay = ay,
                SahiplikTipi = arac.SahiplikTipi,
                CreatedAt = DateTime.UtcNow
            };
            context.AracMaliyetSnapshotlari.Add(snap);
        }
        else
        {
            snap.SahiplikTipi = arac.SahiplikTipi;
            snap.UpdatedAt = DateTime.UtcNow;
        }

        // AracMasraf kategorilerinden topla
        var masraflar = await context.AracMasraflari
            .Include(m => m.MasrafKalemi)
            .Where(m => m.AracId == aracId
                     && m.MasrafTarihi >= donemBas
                     && m.MasrafTarihi < donemSon)
            .ToListAsync();

        decimal yakit = 0m, bakim = 0m, lastik = 0m, sigorta = 0m, kasko = 0m, diger = 0m;
        foreach (var m in masraflar)
        {
            switch (m.MasrafKalemi?.Kategori)
            {
                case MasrafKategori.Yakit: yakit += m.Tutar; break;
                case MasrafKategori.Bakim:
                case MasrafKategori.Tamir:
                case MasrafKategori.YedekParca: bakim += m.Tutar; break;
                case MasrafKategori.Lastik: lastik += m.Tutar; break;
                case MasrafKategori.Sigorta: sigorta += m.Tutar; break;
                case MasrafKategori.Vergi: diger += m.Tutar; break;
                default: diger += m.Tutar; break;
            }
        }

        // ServisKaydi (bakım-onarım) — AracMasrafId null olanları ekle (bağlı olanlar zaten AracMasraflari'nda sayıldı)
        var servisBakim = await context.ServisKayitlari
            .Where(sk => sk.AracId == aracId
                      && sk.ServisTarihi >= donemBas
                      && sk.ServisTarihi < donemSon
                      && sk.AracMasrafId == null
                      && sk.Durum != ServisDurum.IptalEdildi)
            .SumAsync(sk => (decimal?)sk.ToplamTutar) ?? 0m;
        bakim += servisBakim;

        // LastikDegisim ücretlerini ekle (ayrı bir kalem)
        var lastikDegisimUcret = await context.LastikDegisimler
            .Where(l => l.AracId == aracId
                     && l.DegisimTarihi >= donemBas
                     && l.DegisimTarihi < donemSon
                     && l.Ucret != null)
            .SumAsync(l => (decimal?)l.Ucret) ?? 0m;
        lastik += lastikDegisimUcret;

        // Kiralık plaka kirası (kiralık araç için) - sadece ilgili dönem (yil/ay)
        decimal plakaKirasi = 0m;
        if (arac.SahiplikTipi == AracSahiplikTipi.Kiralik)
        {
            // Fatura detay planından döneme ait (Yil+Ay) tutarı al
            var donemFaturaToplam = await context.KiralikPlakaTakipFaturalar
                .Where(f => f.KiralikPlakaTakip!.AracId == aracId
                          && f.Yil == yil
                          && f.Ay == ay)
                .SumAsync(f => (decimal?)(f.FaturaTutari > 0 ? f.FaturaTutari : f.PlanTutari)) ?? 0m;

            if (donemFaturaToplam > 0m)
            {
                plakaKirasi = donemFaturaToplam;
            }
            else
            {
                // Fatura detayı yoksa sözleşme dönemi içindeki kayıtlardan aylık sabit kira al
                plakaKirasi = await context.KiralikPlakaTakipler
                    .Where(k => k.AracId == aracId
                              && k.BaslamaTarihi <= donemSon
                              && k.BitisTarihi >= donemBas)
                    .SumAsync(k => (decimal?)k.FaturaOdemesi) ?? 0m;
            }
        }

        // FiloGunlukPuantaj'tan toplam sefer ve gelir
        var puantajlar = await context.FiloGunlukPuantajlar
            .Where(p => p.AracId == aracId
                     && p.Tarih >= donemBas
                     && p.Tarih < donemSon
                     && !p.IsDeleted)
            .ToListAsync();
        decimal toplamSefer = puantajlar.Sum(p => p.SeferSayisi);
        decimal toplamGelir = puantajlar.Sum(p => p.TahakkukEdenKurumUcreti);

        // Personel puantajdan (net ödeme) araca düşen şoför maaş payını hesapla.
        // Aynı şoför birden fazla araçta çalıştıysa sefer oranına göre dağıtılır.
        decimal soforMaasPayi = 0m;
        var soforIds = puantajlar.Select(p => p.SoforId).Distinct().ToList();
        if (soforIds.Count > 0)
        {
            var personelPuantajlar = await context.PersonelPuantajlar
                .Where(pp => soforIds.Contains(pp.PersonelId)
                          && pp.Yil == yil
                          && pp.Ay == ay
                          && !pp.IsDeleted)
                .ToListAsync();

            var soforAracSeferleri = await context.FiloGunlukPuantajlar
                .Where(p => soforIds.Contains(p.SoforId)
                         && p.Tarih >= donemBas
                         && p.Tarih < donemSon
                         && !p.IsDeleted)
                .GroupBy(p => new { p.SoforId, p.AracId })
                .Select(g => new
                {
                    g.Key.SoforId,
                    g.Key.AracId,
                    ToplamSefer = g.Sum(x => x.SeferSayisi)
                })
                .ToListAsync();

            foreach (var pp in personelPuantajlar)
            {
                var soforAraclari = soforAracSeferleri.Where(x => x.SoforId == pp.PersonelId).ToList();
                var soforToplamSefer = soforAraclari.Sum(x => x.ToplamSefer);
                if (soforToplamSefer <= 0)
                    continue;

                var buAracSefer = soforAraclari
                    .Where(x => x.AracId == aracId)
                    .Sum(x => x.ToplamSefer);

                if (buAracSefer <= 0)
                    continue;

                var maasBaz = pp.NetOdeme > 0 ? pp.NetOdeme : pp.BrutMaas;
                if (maasBaz <= 0)
                    continue;

                soforMaasPayi += maasBaz * (buAracSefer / soforToplamSefer);
            }
        }

        snap.YakitMasraf = yakit;
        snap.BakimMasraf = bakim;
        snap.LastikMasraf = lastik;
        snap.SigortaMasraf = sigorta;
        snap.KaskoMasraf = kasko;
        snap.PlakaKirasi = plakaKirasi;
        snap.SoforMaasPayi = Math.Round(soforMaasPayi, 2, MidpointRounding.AwayFromZero);
        snap.DigerMasraf = diger;
        snap.ToplamSefer = toplamSefer;
        snap.ToplamGelir = toplamGelir;

        await context.SaveChangesAsync();

        // Sefer başı maliyeti puantaj kayıtlarına yansıt (operasyonel hakediş için)
        if (toplamSefer > 0)
        {
            decimal birim = Math.Round(snap.ToplamMaliyet / toplamSefer, 2);
            foreach (var p in puantajlar)
            {
                p.MaliyetOzmalKiralik = birim;
                p.UpdatedAt = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
        }

        return snap;
    }

    public async Task<List<AracMaliyetSnapshot>> TumAraclarIcinUretAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var araclar = await context.Araclar
            .Where(a => a.SahiplikTipi == AracSahiplikTipi.Ozmal || a.SahiplikTipi == AracSahiplikTipi.Kiralik)
            .Select(a => a.Id)
            .ToListAsync();

        var sonuc = new List<AracMaliyetSnapshot>();
        foreach (var aracId in araclar)
        {
            try
            {
                sonuc.Add(await SnapshotUretAsync(aracId, yil, ay));
            }
            catch
            {
                // tek araç hatası tüm akışı durdurmasın
            }
        }
        return sonuc;
    }

    public async Task<List<AracMaliyetSnapshot>> GetSnapshotlarAsync(int? aracId = null, int? yil = null, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var q = context.AracMaliyetSnapshotlari.Include(s => s.Arac).AsQueryable();
        if (aracId.HasValue) q = q.Where(s => s.AracId == aracId.Value);
        if (yil.HasValue) q = q.Where(s => s.Yil == yil.Value);
        if (ay.HasValue) q = q.Where(s => s.Ay == ay.Value);
        return await q.OrderByDescending(s => s.Yil).ThenByDescending(s => s.Ay).ThenBy(s => s.AracId).ToListAsync();
    }

    public async Task<AracMaliyetSnapshot?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracMaliyetSnapshotlari.Include(s => s.Arac).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> SilAsync(int snapshotId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var s = await context.AracMaliyetSnapshotlari.FindAsync(snapshotId);
        if (s == null) return false;
        context.AracMaliyetSnapshotlari.Remove(s);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Fullpet gibi tek fatura / çok araç yakıt dağılımını plakalara kaydeder.
    /// Her araca ayrı bir AracMasraf kaydı oluşturur.
    /// Eğer <paramref name="esitDagit"/> true ise tutarı araç sayısına böler;
    /// false ise <paramref name="aracTutarlari"/> dict'indeki bireysel tutarları kullanır.
    /// </summary>
    public async Task<int> FullpetFaturaDagitAsync(
        DateTime faturaTarihi,
        string? faturaNo,
        string? aciklama,
        List<int> aracIdler,
        decimal toplamTutar,
        bool esitDagit,
        Dictionary<int, decimal>? aracTutarlari,
        int? firmaId = null)
    {
        if (!aracIdler.Any() || toplamTutar <= 0)
            throw new ArgumentException("Araç listesi boş veya tutar sıfır olamaz.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Yakıt kategorili masraf kalemi bul (önce firmaya özel, yoksa genel)
        var yakitKalemi = await context.MasrafKalemleri
            .Where(k => k.Kategori == MasrafKategori.Yakit && k.Aktif
                     && (firmaId == null || k.FirmaId == firmaId || k.FirmaId == null))
            .OrderBy(k => k.FirmaId == null ? 1 : 0) // firma özelini önce al
            .ThenBy(k => k.Id)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Sistemde aktif bir Yakıt masraf kalemi bulunamadı.");

        int kayitSayisi = 0;
        for (int i = 0; i < aracIdler.Count; i++)
        {
            int aracId = aracIdler[i];
            decimal araçTutari;

            if (esitDagit)
            {
                // Son araca kalan kuruşu ver (yuvarlama farkını önlemek için)
                if (i < aracIdler.Count - 1)
                    araçTutari = Math.Round(toplamTutar / aracIdler.Count, 2);
                else
                    araçTutari = toplamTutar - Math.Round(toplamTutar / aracIdler.Count, 2) * (aracIdler.Count - 1);
            }
            else
            {
                if (aracTutarlari == null || !aracTutarlari.TryGetValue(aracId, out araçTutari) || araçTutari <= 0)
                    continue; // tutarı olmayan veya 0 olan araçları atla
            }

            var masraf = new AracMasraf
            {
                AracId = aracId,
                MasrafKalemiId = yakitKalemi.Id,
                MasrafTarihi = faturaTarihi,
                Tutar = araçTutari,
                BelgeNo = faturaNo,
                Aciklama = string.IsNullOrWhiteSpace(aciklama)
                    ? $"Fullpet fatura dağılımı{(faturaNo != null ? $" ({faturaNo})" : "")}"
                    : aciklama,
                OdemeKaynak = MasrafOdemeKaynak.Kasa,
                FirmaId = firmaId,
                CreatedAt = DateTime.UtcNow
            };
            context.AracMasraflari.Add(masraf);
            kayitSayisi++;
        }

        await context.SaveChangesAsync();
        return kayitSayisi;
    }
}


