using System.Diagnostics;
using System.Threading;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Shared.Exceptions;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// OperasyonKaydi → PuantajKayit dönüşüm motoru V1.
/// HesapDonemi + PuantajDetay + revizyon zinciri + finansal audit.
/// </summary>
public sealed class PuantajEngineService : IPuantajEngineService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public PuantajEngineService(IDbContextFactory<ApplicationDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<PuantajEngineSonucV1> ProcessDonemAsync(int yil, int ay, int? kurumId = null, string? hesaplayan = null, string? notlar = null, CancellationToken ct = default)
    {
        // EF Core Connection Resiliency: ExecutionStrategy retry yaptiginda
        // DbContext'in change tracker'i kirlenir. Her retry denemesinde
        // yeni bir DbContext olusturmak icin factory cagrisi delegate ICINDE yapilmali.
        await using var tempDb = await _dbFactory.CreateDbContextAsync();
        var strategy = tempDb.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var baslangic = new DateTime(yil, ay, 1);
            var bitis = baslangic.AddMonths(1).AddDays(-1);

            // ── Transaction: HesapDonemi + PuantajKayit + PuantajDetay tek seferde ──
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            try
            {
                // 1. Önceki Aktif hesap dönemini bul
                var oncekiAktif = await db.PuantajHesapDonemleri
                    .Where(h => !h.IsDeleted && h.Yil == yil && h.Ay == ay
                                && h.KurumId == kurumId && h.Durum == PuantajHesapDurum.Aktif)
                    .OrderByDescending(h => h.Versiyon)
                    .FirstOrDefaultAsync(ct);

                // Kilit kontrolü: Kilitli dönem varsa revizyon yapılamaz
                if (oncekiAktif?.OnayDurum == PuantajDonemOnayDurum.Kilitli)
                    throw new PuantajDonemKilitliException(oncekiAktif.Id, oncekiAktif.Versiyon);

                int yeniVersiyon = (oncekiAktif?.Versiyon ?? 0) + 1;

                // 2. Yeni HesapDonemi oluştur
                var hesapDonemi = new PuantajHesapDonemi
                {
                    FirmaId = oncekiAktif?.FirmaId,
                    Yil = yil, Ay = ay, KurumId = kurumId,
                    Versiyon = yeniVersiyon,
                    Durum = PuantajHesapDurum.Taslak,
                    HesaplayanKullanici = hesaplayan,
                    HesaplamaTarihi = DateTime.UtcNow,
                    OncekiDonemId = oncekiAktif?.Id,
                    Notlar = notlar,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = hesaplayan
                };
                db.PuantajHesapDonemleri.Add(hesapDonemi);
                await db.SaveChangesAsync(ct);

                // 3. OperasyonKaydi'ları topla
                var query = db.OperasyonKayitlari
                    .Include(o => o.Guzergah)
                    .Where(o => !o.IsDeleted && o.Tarih >= baslangic && o.Tarih <= bitis);

                if (kurumId.HasValue && kurumId.Value > 0)
                {
                    var guzergahIds = await db.Guzergahlar
                        .Where(g => !g.IsDeleted && g.KurumId == kurumId.Value)
                        .Select(g => g.Id)
                        .ToListAsync(ct);
                    query = query.Where(o => guzergahIds.Contains(o.GuzergahId));
                }

                var operasyonlar = await query.ToListAsync(ct);
                if (!operasyonlar.Any())
                {
                    hesapDonemi.Durum = PuantajHesapDurum.Iptal;
                    hesapDonemi.Notlar = (hesapDonemi.Notlar ?? "") + " | İşlenecek operasyon bulunamadı.";
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct); // Iptal donem de olsa persist et — yoksa rollback olur, caller stub ID görür
                    return new PuantajEngineSonucV1 { HesapDonemiId = hesapDonemi.Id, Versiyon = yeniVersiyon };
                }

                // 4. Pricing verilerini toplu yükle
                var guzergahIds2 = operasyonlar.Select(o => o.GuzergahId).Distinct().ToList();
                var guzergahlar = await db.Guzergahlar.Where(g => guzergahIds2.Contains(g.Id)).ToDictionaryAsync(g => g.Id, ct);
                var eslestirmeler = await db.FiloGuzergahEslestirmeleri
                    .Where(e => guzergahIds2.Contains(e.GuzergahId) && e.IsActive).ToListAsync(ct);

                // 5. Önceki dönemin PuantajKayit'larını soft-delete ET (yeni insert'lerden ÖNCE).
                // Sebep: partial UNIQUE index WHERE "IsDeleted"=false —
                // eski kayıtlar IsDeleted=false iken yeni insert'ler 23505 alır.
                // Bu işlem yeni insert'lerden ÖNCE yapılmalıdır.
                int superseded = 0;
                if (oncekiAktif != null)
                {
                    var oncekiKayitlar = await db.PuantajKayitlar
                        .Where(p => p.HesapDonemiId == oncekiAktif.Id && !p.IsDeleted)
                        .ToListAsync(ct);
                    foreach (var pk in oncekiKayitlar)
                    {
                        pk.IsDeleted = true;
                        pk.OnayDurum = PuantajOnayDurum.Taslak;
                        pk.UpdatedAt = DateTime.UtcNow;
                        superseded++;
                    }
                    await db.SaveChangesAsync(ct); // soft-delete'leri persist et (unique index için)
                }

                // 6. GuzergahId + AracId + Slot bazında grupla
                var gruplar = operasyonlar.GroupBy(o => new { o.GuzergahId, o.AracId, o.Slot }).ToList();

                int uretilen = 0, detaySayisi = 0;
                var yeniPuantajKayitlar = new List<(PuantajKayit pk, List<OperasyonKaydi> ops)>();

                foreach (var grup in gruplar)
                {
                    var ilk = grup.First();
                    var guzergahId = grup.Key.GuzergahId;
                    var aracId = grup.Key.AracId;
                    var slot = grup.Key.Slot;

                    var pk = new PuantajKayit
                    {
                        Yil = yil, Ay = ay,
                        GuzergahId = guzergahId, AracId = aracId, Slot = slot,
                        HesapDonemiId = hesapDonemi.Id,
                        Versiyon = yeniVersiyon,
                        Kaynak = PuantajKaynak.ServisCalismaOtomatik,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Gun01..Gun31 sıfırla
                    pk.Gun01 = 0; pk.Gun02 = 0; pk.Gun03 = 0; pk.Gun04 = 0; pk.Gun05 = 0;
                    pk.Gun06 = 0; pk.Gun07 = 0; pk.Gun08 = 0; pk.Gun09 = 0; pk.Gun10 = 0;
                    pk.Gun11 = 0; pk.Gun12 = 0; pk.Gun13 = 0; pk.Gun14 = 0; pk.Gun15 = 0;
                    pk.Gun16 = 0; pk.Gun17 = 0; pk.Gun18 = 0; pk.Gun19 = 0; pk.Gun20 = 0;
                    pk.Gun21 = 0; pk.Gun22 = 0; pk.Gun23 = 0; pk.Gun24 = 0; pk.Gun25 = 0;
                    pk.Gun26 = 0; pk.Gun27 = 0; pk.Gun28 = 0; pk.Gun29 = 0; pk.Gun30 = 0;
                    pk.Gun31 = 0;

                    decimal toplamSefer = 0;
                    foreach (var o in grup)
                    {
                        var gun = o.Tarih.Day;
                        if (o.OperasyonDurumu == OperasyonDurumu.Gitti)
                        {
                            var seferDeger = (int)(o.SeferSayisi * o.PuantajCarpani);
                            pk.SetGunDeger(gun, seferDeger);
                            toplamSefer += seferDeger;
                        }
                    }

                    pk.SoforId = ilk.SoforId;
                    pk.SlotAdi = ilk.SlotAdi;
                    pk.Yon = ilk.Yon;
                    pk.KurumId = ilk.KurumId;
                    pk.IsverenFirmaId = ilk.IsverenFirmaId;
                    pk.SeferSayisi = (int)toplamSefer;
                    pk.KaynakTipi = ilk.KaynakTipi;
                    pk.FinansYonu = ilk.FinansYonu;
                    pk.SoforOdemeTipi = ilk.SoforOdemeTipi;
                    pk.OdemeYapilacakCariId = ilk.OdemeYapilacakCariId;
                    pk.FaturaKesiciCariId = ilk.FaturaKesiciCariId;
                    pk.BelgeNo = ilk.BelgeNo;
                    pk.TransferDurum = ilk.TransferDurum;
                    pk.Notlar = ilk.Notlar;
                    pk.UpdatedAt = DateTime.UtcNow;

                    if (guzergahlar.TryGetValue(guzergahId, out var g))
                    {
                        pk.GuzergahAdi = g.GuzergahAdi;
                        pk.BirimGelir = g.GelirFiyat;
                        pk.BirimGider = g.GiderFiyat;
                    }

                    var eslestirme = eslestirmeler.FirstOrDefault(e => e.GuzergahId == guzergahId && e.AracId == aracId);
                    if (eslestirme != null)
                    {
                        pk.BirimGelir = eslestirme.KurumaKesilecekUcret;
                        pk.BirimGider = eslestirme.TaseronaOdenenUcret;
                    }

                    pk.HesaplaPuantajToplam();
                    pk.HesaplaGelir();
                    pk.HesaplaGider();

                    db.PuantajKayitlar.Add(pk);
                    yeniPuantajKayitlar.Add((pk, grup.ToList()));
                    uretilen++;
                }

                await db.SaveChangesAsync(ct); // PuantajKayit Id'leri için

                // 7. PuantajDetay'ları oluştur
                foreach (var (pk, ops) in yeniPuantajKayitlar)
                {
                    foreach (var o in ops)
                    {
                        var detay = new PuantajDetay
                        {
                            FirmaId = o.FirmaId,
                            OperasyonKaydiId = o.Id,
                            PuantajKayitId = pk.Id,
                            HesapDonemiId = hesapDonemi.Id,
                            BirimGelir = pk.BirimGelir,
                            BirimGider = pk.BirimGider,
                            SeferSayisi = o.SeferSayisi,
                            HesaplananTutar = pk.BirimGelir * o.SeferSayisi * o.PuantajCarpani,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.PuantajDetaylari.Add(detay);
                        detaySayisi++;
                    }
                }

                // 8. Önceki Aktif hesabı Superseded yap + PuantajDetay soft-delete
                // PuantajKayit soft-delete'i step 5'te (insert'lerden ÖNCE) yapıldı.
                if (oncekiAktif != null)
                {
                    oncekiAktif.Durum = PuantajHesapDurum.Superseded;
                    oncekiAktif.UpdatedAt = DateTime.UtcNow;

                    // PuantajDetay kayıtlarını soft-delete et
                    var oncekiDetaylar = await db.PuantajDetaylari
                        .Where(d => d.HesapDonemiId == oncekiAktif.Id && !d.IsDeleted)
                        .ToListAsync(ct);
                    foreach (var d in oncekiDetaylar)
                    {
                        d.IsDeleted = true;
                    }
                }

                // 9. HesapDonemi'ni Aktif yap
                hesapDonemi.Durum = PuantajHesapDurum.Aktif;
                hesapDonemi.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new PuantajEngineSonucV1
                {
                    HesapDonemiId = hesapDonemi.Id,
                    Versiyon = yeniVersiyon,
                    IslenenOperasyonSayisi = operasyonlar.Count,
                    UretilenPuantajKayit = uretilen,
                    SupersededKayit = superseded,
                    OlusturulanDetay = detaySayisi
                };
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task IptalEtAsync(int hesapDonemiId, string? iptalEden = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var hesapDonemi = await db.PuantajHesapDonemleri.FindAsync(hesapDonemiId)
            ?? throw new InvalidOperationException("Hesap dönemi bulunamadı.");

        if (hesapDonemi.Durum != PuantajHesapDurum.Aktif)
            throw new InvalidOperationException("Sadece Aktif hesap dönemi iptal edilebilir.");

        // PuantajKayit'ları soft-delete
        var kayitlar = await db.PuantajKayitlar
            .Where(p => p.HesapDonemiId == hesapDonemiId && !p.IsDeleted)
            .ToListAsync(ct);
        foreach (var k in kayitlar)
        {
            k.IsDeleted = true;
            k.UpdatedAt = DateTime.UtcNow;
        }

        // PuantajDetay'ları soft-delete
        var detaylar = await db.PuantajDetaylari
            .Where(d => d.HesapDonemiId == hesapDonemiId && !d.IsDeleted)
            .ToListAsync(ct);
        foreach (var d in detaylar)
        {
            d.IsDeleted = true;
        }

        hesapDonemi.Durum = PuantajHesapDurum.Iptal;
        hesapDonemi.UpdatedAt = DateTime.UtcNow;
        hesapDonemi.UpdatedBy = iptalEden;

        await db.SaveChangesAsync();
    }

    public async Task<List<PuantajEngineDetayDto>> GetDetaylarAsync(int hesapDonemiId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PuantajDetaylari
            .Include(d => d.OperasyonKaydi).ThenInclude(o => o!.Arac)
            .Include(d => d.OperasyonKaydi).ThenInclude(o => o!.Guzergah)
            .Where(d => d.HesapDonemiId == hesapDonemiId && !d.IsDeleted)
            .OrderBy(d => d.OperasyonKaydi!.Tarih)
            .Select(d => new PuantajEngineDetayDto
            {
                Id = d.Id,
                OperasyonKaydiId = d.OperasyonKaydiId,
                Tarih = d.OperasyonKaydi!.Tarih,
                Plaka = d.OperasyonKaydi.Arac!.AktifPlaka ?? d.OperasyonKaydi.Arac.Plaka,
                GuzergahAdi = d.OperasyonKaydi.Guzergah!.GuzergahAdi,
                Slot = d.OperasyonKaydi.Slot.ToString(),
                SeferSayisi = d.SeferSayisi,
                BirimGelir = d.BirimGelir,
                BirimGider = d.BirimGider,
                HesaplananTutar = d.HesaplananTutar
            })
            .ToListAsync(ct);
    }
}
