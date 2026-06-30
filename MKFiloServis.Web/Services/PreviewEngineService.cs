using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Dry-run puantaj hesaplama motoru.
/// DB'ye KESİNLİKLE yazmaz — sadece Okuma yapar, sonucu memory'de üretir.
/// </summary>
public sealed class PreviewEngineService : IPreviewEngineService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public PreviewEngineService(IDbContextFactory<ApplicationDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<PreviewResult> PreviewAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var uyarilar = new List<string>();

        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        // 1. Operasyonları yükle (read-only)
        var query = db.OperasyonKayitlari
            .Include(o => o.Guzergah)
            .Include(o => o.Arac)
            .Include(o => o.Sofor)
            .Where(o => !o.IsDeleted && o.Tarih >= baslangic && o.Tarih <= bitis)
            .AsNoTracking();

        if (kurumId.HasValue && kurumId.Value > 0)
        {
            var guzergahIds = await db.Guzergahlar
                .Where(g => !g.IsDeleted && g.KurumId == kurumId.Value)
                .Select(g => g.Id)
                .ToListAsync(ct);
            query = query.Where(o => guzergahIds.Contains(o.GuzergahId));
        }

        var operasyonlar = await query.OrderBy(o => o.Tarih).ToListAsync(ct);

        if (!operasyonlar.Any())
        {
            uyarilar.Add("Bu dönemde işlenecek operasyon kaydı bulunamadı.");
            return new PreviewResult { UyariMesajlari = uyarilar };
        }

        // 2. Önceki aktif hesap dönemini kontrol et
        var oncekiAktif = await db.PuantajHesapDonemleri
            .Where(h => !h.IsDeleted && h.Yil == yil && h.Ay == ay
                        && h.KurumId == kurumId && h.Durum == PuantajHesapDurum.Aktif)
            .OrderByDescending(h => h.Versiyon)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        int yeniVersiyon = (oncekiAktif?.Versiyon ?? 0) + 1;

        if (oncekiAktif != null)
            uyarilar.Add($"Bu dönem için Versiyon {oncekiAktif.Versiyon} aktif hesap mevcut. Yeni hesaplama revizyon olacak (V{yeniVersiyon}).");

        // 3. Fiyat referanslarını yükle (read-only)
        var guzergahIds2 = operasyonlar.Select(o => o.GuzergahId).Distinct().ToList();
        var guzergahlar = await db.Guzergahlar
            .Where(g => guzergahIds2.Contains(g.Id))
            .AsNoTracking()
            .ToDictionaryAsync(g => g.Id);

        var eslestirmeler = await db.FiloGuzergahEslestirmeleri
            .Where(e => guzergahIds2.Contains(e.GuzergahId) && e.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        // 4. Grupla ve hesapla (tamamen memory'de)
        var gruplar = operasyonlar
            .GroupBy(o => new { o.GuzergahId, o.AracId, o.Slot })
            .Select(g =>
            {
                var ilk = g.First();
                var toplamSefer = g.Where(o => o.OperasyonDurumu == OperasyonDurumu.Gitti)
                                   .Sum(o => (int)(o.SeferSayisi * o.PuantajCarpani));

                // Fiyat belirleme (snapshot)
                decimal birimGelir = 0, birimGider = 0;
                var eslestirme = eslestirmeler.FirstOrDefault(e => e.GuzergahId == g.Key.GuzergahId && e.AracId == g.Key.AracId);
                if (eslestirme != null)
                {
                    birimGelir = eslestirme.KurumaKesilecekUcret;
                    birimGider = eslestirme.TaseronaOdenenUcret;
                }
                else if (guzergahlar.TryGetValue(g.Key.GuzergahId, out var guz))
                {
                    birimGelir = guz.GelirFiyat;
                    birimGider = guz.GiderFiyat;
                }

                return new PreviewGrupDetay
                {
                    GuzergahId = g.Key.GuzergahId,
                    GuzergahAdi = ilk.Guzergah?.GuzergahAdi ?? "",
                    AracId = g.Key.AracId,
                    Plaka = ilk.Arac?.AktifPlaka ?? ilk.Arac?.Plaka ?? "",
                    SoforAdi = ilk.Sofor != null ? $"{ilk.Sofor.Ad} {ilk.Sofor.Soyad}" : null,
                    Slot = ilk.Slot.ToString(),
                    SeferGunuToplami = toplamSefer,
                    BirimGelir = birimGelir,
                    BirimGider = birimGider,
                    ToplamGelir = birimGelir * toplamSefer,
                    ToplamGider = birimGider * toplamSefer,
                    OperasyonSayisi = g.Count()
                };
            })
            .OrderBy(g => g.GuzergahAdi).ThenBy(g => g.Plaka).ThenBy(g => g.Slot)
            .ToList();

        // 5. Uyarı kontrolleri
        var sifirSeferli = gruplar.Where(g => g.SeferGunuToplami == 0).ToList();
        if (sifirSeferli.Any())
            uyarilar.Add($"{sifirSeferli.Count} grupta sefer günü toplamı 0 (tüm operasyonlar iptal/durmuş).");

        return new PreviewResult
        {
            OperasyonSayisi = operasyonlar.Count,
            GrupSayisi = gruplar.Count,
            UretilecekPuantajKayit = gruplar.Count,
            OncekiVersiyon = oncekiAktif?.Versiyon ?? 0,
            YeniVersiyon = yeniVersiyon,
            OncekiHesapDonemiId = oncekiAktif?.Id,
            OncekiHesaplayan = oncekiAktif?.HesaplayanKullanici,
            OncekiHesaplamaTarihi = oncekiAktif?.HesaplamaTarihi,
            ToplamGelir = gruplar.Sum(g => g.ToplamGelir),
            ToplamGider = gruplar.Sum(g => g.ToplamGider),
            ToplamSeferGunu = gruplar.Sum(g => g.SeferGunuToplami),
            OrtalamaBirimGelir = gruplar.Any(g => g.SeferGunuToplami > 0)
                ? gruplar.Where(g => g.SeferGunuToplami > 0).Average(g => g.BirimGelir)
                : 0,
            Gruplar = gruplar,
            UyariMesajlari = uyarilar
        };
    }

    public async Task<ComparisonResult> CompareAsync(int hesapDonemiId1, int hesapDonemiId2, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var h1 = await db.PuantajHesapDonemleri.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hesapDonemiId1)
            ?? throw new InvalidOperationException($"Hesap dönemi bulunamadı: {hesapDonemiId1}");
        var h2 = await db.PuantajHesapDonemleri.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hesapDonemiId2)
            ?? throw new InvalidOperationException($"Hesap dönemi bulunamadı: {hesapDonemiId2}");

        var detay1 = await db.PuantajDetaylari
            .Include(d => d.PuantajKayit).ThenInclude(p => p!.Guzergah)
            .Include(d => d.PuantajKayit).ThenInclude(p => p!.Arac)
            .Include(d => d.OperasyonKaydi).ThenInclude(o => o!.Arac)
            .Include(d => d.OperasyonKaydi).ThenInclude(o => o!.Sofor)
            .Where(d => d.HesapDonemiId == hesapDonemiId1 && !d.IsDeleted)
            .AsNoTracking().ToListAsync(ct);

        var detay2 = await db.PuantajDetaylari
            .Include(d => d.PuantajKayit).ThenInclude(p => p!.Guzergah)
            .Include(d => d.PuantajKayit).ThenInclude(p => p!.Arac)
            .Include(d => d.OperasyonKaydi).ThenInclude(o => o!.Arac)
            .Include(d => d.OperasyonKaydi).ThenInclude(o => o!.Sofor)
            .Where(d => d.HesapDonemiId == hesapDonemiId2 && !d.IsDeleted)
            .AsNoTracking().ToListAsync(ct);

        var map1 = detay1.GroupBy(d => new { d.PuantajKayit!.GuzergahId, d.PuantajKayit.AracId, d.PuantajKayit.Slot })
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SeferSayisi));
        var map2 = detay2.GroupBy(d => new { d.PuantajKayit!.GuzergahId, d.PuantajKayit.AracId, d.PuantajKayit.Slot })
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SeferSayisi));

        var tumKeys = map1.Keys.Union(map2.Keys).Distinct();

        var farklar = tumKeys.Select(k =>
        {
            var sefer1 = map1.GetValueOrDefault(k);
            var sefer2 = map2.GetValueOrDefault(k);
            var pk = detay1.FirstOrDefault(d => d.PuantajKayit!.GuzergahId == k.GuzergahId && d.PuantajKayit.AracId == k.AracId && d.PuantajKayit.Slot == k.Slot)?.PuantajKayit
                  ?? detay2.FirstOrDefault(d => d.PuantajKayit!.GuzergahId == k.GuzergahId && d.PuantajKayit.AracId == k.AracId && d.PuantajKayit.Slot == k.Slot)?.PuantajKayit;

            return new ComparisonDelta
            {
                GuzergahAdi = pk?.GuzergahAdi ?? "",
                Plaka = pk?.Arac?.AktifPlaka ?? pk?.Arac?.Plaka ?? "",
                Slot = k.Slot.ToString(),
                SeferOnceki = sefer1,
                SeferYeni = sefer2,
                GelirOnceki = (pk?.BirimGelir ?? 0) * sefer1,
                GelirYeni = (pk?.BirimGelir ?? 0) * sefer2
            };
        }).OrderBy(f => f.GuzergahAdi).ThenBy(f => f.Plaka).ToList();

        // Operasyon bazlı delta
        var opIds1 = detay1.Select(d => d.OperasyonKaydiId).ToHashSet();
        var opIds2 = detay2.Select(d => d.OperasyonKaydiId).ToHashSet();
        var eklenen = opIds2.Except(opIds1).ToList();
        var cikarilan = opIds1.Except(opIds2).ToList();
        var ortak = opIds1.Intersect(opIds2).ToList();

        var opDeltas = new List<OperasyonDelta>();

        // Yeni eklenenler
        foreach (var id in eklenen)
        {
            var d = detay2.First(x => x.OperasyonKaydiId == id);
            opDeltas.Add(new OperasyonDelta
            {
                Tarih = d.OperasyonKaydi!.Tarih, Plaka = d.OperasyonKaydi.Arac?.AktifPlaka ?? d.OperasyonKaydi.Arac?.Plaka ?? "",
                GuzergahAdi = d.PuantajKayit?.GuzergahAdi ?? "", Slot = d.OperasyonKaydi.Slot.ToString(),
                SoforAdi = d.OperasyonKaydi.Sofor != null ? $"{d.OperasyonKaydi.Sofor.Ad} {d.OperasyonKaydi.Sofor.Soyad}" : null,
                SeferYeni = d.SeferSayisi, DegisimTipi = "Eklendi"
            });
        }

        // Çıkarılanlar
        foreach (var id in cikarilan)
        {
            var d = detay1.First(x => x.OperasyonKaydiId == id);
            opDeltas.Add(new OperasyonDelta
            {
                Tarih = d.OperasyonKaydi!.Tarih, Plaka = d.OperasyonKaydi.Arac?.AktifPlaka ?? d.OperasyonKaydi.Arac?.Plaka ?? "",
                GuzergahAdi = d.PuantajKayit?.GuzergahAdi ?? "", Slot = d.OperasyonKaydi.Slot.ToString(),
                SoforAdi = d.OperasyonKaydi.Sofor != null ? $"{d.OperasyonKaydi.Sofor.Ad} {d.OperasyonKaydi.Sofor.Soyad}" : null,
                SeferOnceki = d.SeferSayisi, DegisimTipi = "Çıkarıldı"
            });
        }

        // Değişenler (aynı operasyon, farklı sefer sayısı veya fiyat)
        foreach (var id in ortak)
        {
            var d1 = detay1.First(x => x.OperasyonKaydiId == id);
            var d2 = detay2.First(x => x.OperasyonKaydiId == id);
            if (d1.SeferSayisi != d2.SeferSayisi || d1.BirimGelir != d2.BirimGelir || d1.BirimGider != d2.BirimGider)
            {
                opDeltas.Add(new OperasyonDelta
                {
                    Tarih = d1.OperasyonKaydi!.Tarih, Plaka = d1.OperasyonKaydi.Arac?.AktifPlaka ?? d1.OperasyonKaydi.Arac?.Plaka ?? "",
                    GuzergahAdi = d1.PuantajKayit?.GuzergahAdi ?? "", Slot = d1.OperasyonKaydi.Slot.ToString(),
                    SoforAdi = d1.OperasyonKaydi.Sofor != null ? $"{d1.OperasyonKaydi.Sofor.Ad} {d1.OperasyonKaydi.Sofor.Soyad}" : null,
                    SeferOnceki = d1.SeferSayisi, SeferYeni = d2.SeferSayisi,
                    GelirOnceki = d1.BirimGelir, GelirYeni = d2.BirimGelir, DegisimTipi = "Değişti"
                });
            }
        }

        return new ComparisonResult
        {
            Hesap1Id = h1.Id, Hesap1Versiyon = h1.Versiyon, Hesap1Tarih = h1.HesaplamaTarihi.ToString("dd.MM HH:mm"),
            Hesap2Id = h2.Id, Hesap2Versiyon = h2.Versiyon, Hesap2Tarih = h2.HesaplamaTarihi.ToString("dd.MM HH:mm"),
            Gelir1 = detay1.Sum(d => d.HesaplananTutar), Gelir2 = detay2.Sum(d => d.HesaplananTutar),
            Gider1 = detay1.Sum(d => d.BirimGider * d.SeferSayisi), Gider2 = detay2.Sum(d => d.BirimGider * d.SeferSayisi),
            Sefer1 = detay1.Sum(d => d.SeferSayisi), Sefer2 = detay2.Sum(d => d.SeferSayisi),
            Farklar = farklar,
            EklenenOperasyon = eklenen.Count, CikarilanOperasyon = cikarilan.Count,
            DegisenOperasyon = opDeltas.Count(d => d.DegisimTipi == "Değişti"), AyniOperasyon = ortak.Count - opDeltas.Count(d => d.DegisimTipi == "Değişti"),
            OperasyonFarklari = opDeltas.OrderBy(d => d.Tarih).ToList()
        };
    }

    public async Task<List<DrillDownOperasyon>> DrillDownAsync(int guzergahId, int aracId, int slot, int yil, int ay, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        return await db.OperasyonKayitlari
            .Include(o => o.Arac)
            .Include(o => o.Sofor)
            .Where(o => !o.IsDeleted && o.Tarih >= baslangic && o.Tarih <= bitis
                        && o.GuzergahId == guzergahId && o.AracId == aracId && (int)o.Slot == slot)
            .OrderBy(o => o.Tarih)
            .AsNoTracking()
            .Select(o => new DrillDownOperasyon
            {
                Id = o.Id,
                Tarih = o.Tarih,
                Plaka = o.Arac!.AktifPlaka ?? o.Arac.Plaka ?? "",
                SoforAdi = o.Sofor != null ? $"{o.Sofor.Ad} {o.Sofor.Soyad}" : null,
                Slot = o.Slot.ToString(),
                SeferSayisi = o.SeferSayisi,
                Durum = o.OperasyonDurumu.ToString()
            })
            .ToListAsync(ct);
    }
}


