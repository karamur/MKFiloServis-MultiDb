using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// <see cref="IFirmaKopyalamaService"/> default implementasyonu.
/// <para>
/// Tenant filter "TumFirmalar" modunda değilken kaynak firmaya doğrudan erişemediğimiz için
/// tüm sorgular <c>IgnoreQueryFilters()</c> ile çalışır ve manuel <c>FirmaId</c> filtresi uygulanır.
/// Yeni kayıtlar eklenirken de SaveChanges'in otomatik tenant ataması yanlış firmaya yazmasın diye
/// her entity'nin <c>FirmaId</c> alanı elle <b>hedef firma</b>'ya set edilir.
/// </para>
/// </summary>
public sealed class FirmaKopyalamaService : IFirmaKopyalamaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public FirmaKopyalamaService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<FirmaKopyalamaKayitOzeti>> ListeleAsync(
        FirmaKopyalamaModulu modul,
        int kaynakFirmaId,
        int hedefFirmaId)
    {
        if (kaynakFirmaId <= 0) throw new ArgumentException("Kaynak firma seçilmelidir.", nameof(kaynakFirmaId));
        if (hedefFirmaId <= 0) throw new ArgumentException("Hedef firma seçilmelidir.", nameof(hedefFirmaId));

        using var ctx = await _contextFactory.CreateDbContextAsync();

        return modul switch
        {
            FirmaKopyalamaModulu.Cari => await ListeleCariAsync(ctx, kaynakFirmaId, hedefFirmaId),
            FirmaKopyalamaModulu.Kurum => await ListeleKurumAsync(ctx, kaynakFirmaId, hedefFirmaId),
            FirmaKopyalamaModulu.Guzergah => await ListeleGuzergahAsync(ctx, kaynakFirmaId, hedefFirmaId),
            FirmaKopyalamaModulu.Arac => await ListeleAracAsync(ctx, kaynakFirmaId, hedefFirmaId),
            FirmaKopyalamaModulu.Sofor => await ListeleSoforAsync(ctx, kaynakFirmaId, hedefFirmaId),
            FirmaKopyalamaModulu.MasrafKalemi => throw new NotSupportedException(
                "MasrafKalemi global tanım kümesidir; firma bazlı kopyalama gerekmez."),
            _ => throw new NotSupportedException($"Modül desteklenmiyor: {modul}")
        };
    }

    public async Task<FirmaKopyalamaSonucu> KopyalaAsync(
        FirmaKopyalamaModulu modul,
        int kaynakFirmaId,
        int hedefFirmaId,
        IEnumerable<int> kayitIds)
    {
        if (kaynakFirmaId <= 0) throw new ArgumentException("Kaynak firma seçilmelidir.", nameof(kaynakFirmaId));
        if (hedefFirmaId <= 0) throw new ArgumentException("Hedef firma seçilmelidir.", nameof(hedefFirmaId));
        if (kaynakFirmaId == hedefFirmaId)
            throw new InvalidOperationException("Kaynak ve hedef firma aynı olamaz.");

        var ids = kayitIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0)
            return new FirmaKopyalamaSonucu();

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var strategy = ctx.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            await using var tx = await ctx.Database.BeginTransactionAsync();

            var sonuc = modul switch
            {
                FirmaKopyalamaModulu.Cari => await KopyalaCariAsync(ctx, kaynakFirmaId, hedefFirmaId, ids),
                FirmaKopyalamaModulu.Kurum => await KopyalaKurumAsync(ctx, kaynakFirmaId, hedefFirmaId, ids),
                FirmaKopyalamaModulu.Guzergah => await KopyalaGuzergahAsync(ctx, kaynakFirmaId, hedefFirmaId, ids),
                FirmaKopyalamaModulu.Arac => await KopyalaAracAsync(ctx, kaynakFirmaId, hedefFirmaId, ids),
                FirmaKopyalamaModulu.Sofor => await KopyalaSoforAsync(ctx, kaynakFirmaId, hedefFirmaId, ids),
                FirmaKopyalamaModulu.MasrafKalemi => throw new NotSupportedException(
                    "MasrafKalemi global tanım kümesidir; firma bazlı kopyalama gerekmez."),
                _ => throw new NotSupportedException($"Modül desteklenmiyor: {modul}")
            };

            await tx.CommitAsync();
            return sonuc;
        });
    }

    // ---------------- CARI ----------------
    private static async Task<List<FirmaKopyalamaKayitOzeti>> ListeleCariAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId)
    {
        var hedefKodlar = await ctx.Cariler.IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.FirmaId == hedefFirmaId)
            .Select(c => c.CariKodu)
            .ToListAsync();
        var hedefSet = hedefKodlar.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var list = await ctx.Cariler.IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.FirmaId == kaynakFirmaId)
            .OrderBy(c => c.CariKodu)
            .Select(c => new { c.Id, c.CariKodu, c.Unvan, c.CariTipi })
            .ToListAsync();

        return list.Select(c => new FirmaKopyalamaKayitOzeti
        {
            Id = c.Id,
            Kod = c.CariKodu,
            Ad = c.Unvan,
            EkBilgi = c.CariTipi.ToString(),
            HedefteVarMi = hedefSet.Contains(c.CariKodu)
        }).ToList();
    }

    private static async Task<FirmaKopyalamaSonucu> KopyalaCariAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId, List<int> ids)
    {
        var hedefKodlar = (await ctx.Cariler.IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.FirmaId == hedefFirmaId)
            .Select(c => c.CariKodu)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var kaynak = await ctx.Cariler.IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.FirmaId == kaynakFirmaId && ids.Contains(c.Id))
            .ToListAsync();

        var sonuc = new FirmaKopyalamaSonucu();
        int kopyalanan = 0;
        foreach (var src in kaynak)
        {
            if (hedefSetContains(hedefKodlar, src.CariKodu, sonuc, src.CariKodu)) continue;

            var clone = new Cari
            {
                CariKodu = src.CariKodu,
                Unvan = src.Unvan,
                CariTipi = src.CariTipi,
                VergiDairesi = src.VergiDairesi,
                VergiNo = src.VergiNo,
                TcKimlikNo = src.TcKimlikNo,
                Adres = src.Adres,
                Il = src.Il,
                Ilce = src.Ilce,
                PostaKodu = src.PostaKodu,
                Telefon = src.Telefon,
                Telefon2 = src.Telefon2,
                Fax = src.Fax,
                Email = src.Email,
                WebSitesi = src.WebSitesi,
                YetkiliKisi = src.YetkiliKisi,
                Notlar = src.Notlar,
                Aktif = src.Aktif,
                // Her firmanın kendi hesap planı vardır → muhasebe eşleşmeleri kopyalanmaz
                MuhasebeHesapId = null,
                PersonelAvansHesapId = null,
                SoforId = null,
                SozlesmeNo = src.SozlesmeNo,
                SozlesmeBaslangicTarihi = src.SozlesmeBaslangicTarihi,
                SozlesmeBitisTarihi = src.SozlesmeBitisTarihi,
                FirmaId = hedefFirmaId,
                KaynakFirmaId = kaynakFirmaId,
                KaynakKayitId = src.Id,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Cariler.Add(clone);
            hedefKodlar.Add(src.CariKodu);
            kopyalanan++;
        }
        await ctx.SaveChangesAsync();
        return WithCount(sonuc, kopyalanan);
    }

    // ---------------- KURUM ----------------
    private static async Task<List<FirmaKopyalamaKayitOzeti>> ListeleKurumAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId)
    {
        var hedefSet = (await ctx.Kurumlar.IgnoreQueryFilters()
            .Where(k => !k.IsDeleted && k.FirmaId == hedefFirmaId)
            .Select(k => k.KurumKodu)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var list = await ctx.Kurumlar.IgnoreQueryFilters()
            .Where(k => !k.IsDeleted && k.FirmaId == kaynakFirmaId)
            .OrderBy(k => k.KurumKodu)
            .Select(k => new { k.Id, k.KurumKodu, k.KurumAdi, k.Il })
            .ToListAsync();

        return list.Select(k => new FirmaKopyalamaKayitOzeti
        {
            Id = k.Id,
            Kod = k.KurumKodu,
            Ad = k.KurumAdi,
            EkBilgi = k.Il,
            HedefteVarMi = hedefSet.Contains(k.KurumKodu)
        }).ToList();
    }

    private static async Task<FirmaKopyalamaSonucu> KopyalaKurumAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId, List<int> ids)
    {
        var hedefSet = (await ctx.Kurumlar.IgnoreQueryFilters()
            .Where(k => !k.IsDeleted && k.FirmaId == hedefFirmaId)
            .Select(k => k.KurumKodu)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var kaynak = await ctx.Kurumlar.IgnoreQueryFilters()
            .Where(k => !k.IsDeleted && k.FirmaId == kaynakFirmaId && ids.Contains(k.Id))
            .ToListAsync();

        var sonuc = new FirmaKopyalamaSonucu();
        int kopyalanan = 0;
        foreach (var src in kaynak)
        {
            if (hedefSetContains(hedefSet, src.KurumKodu, sonuc, src.KurumKodu)) continue;

            var clone = new Kurum
            {
                KurumKodu = src.KurumKodu,
                KurumAdi = src.KurumAdi,
                UnvanTam = src.UnvanTam,
                VergiNo = src.VergiNo,
                VergiDairesi = src.VergiDairesi,
                Adres = src.Adres,
                Il = src.Il,
                Ilce = src.Ilce,
                Telefon = src.Telefon,
                Telefon2 = src.Telefon2,
                Email = src.Email,
                WebSite = src.WebSite,
                YetkiliKisi = src.YetkiliKisi,
                YetkiliTelefon = src.YetkiliTelefon,
                YetkiliEmail = src.YetkiliEmail,
                Notlar = src.Notlar,
                Aktif = src.Aktif,
                CariId = null, // her firmanın kendi cari kayıtları
                FirmaId = hedefFirmaId,
                KaynakFirmaId = kaynakFirmaId,
                KaynakKayitId = src.Id,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Kurumlar.Add(clone);
            hedefSet.Add(src.KurumKodu);
            kopyalanan++;
        }
        await ctx.SaveChangesAsync();
        return WithCount(sonuc, kopyalanan);
    }

    // ---------------- GUZERGAH ----------------
    private static async Task<List<FirmaKopyalamaKayitOzeti>> ListeleGuzergahAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId)
    {
        var hedefSet = (await ctx.Guzergahlar.IgnoreQueryFilters()
            .Where(g => !g.IsDeleted && g.FirmaId == hedefFirmaId)
            .Select(g => g.GuzergahKodu)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var list = await ctx.Guzergahlar.IgnoreQueryFilters()
            .Where(g => !g.IsDeleted && g.FirmaId == kaynakFirmaId)
            .OrderBy(g => g.GuzergahKodu)
            .Select(g => new { g.Id, g.GuzergahKodu, g.GuzergahAdi, g.BaslangicNoktasi, g.BitisNoktasi })
            .ToListAsync();

        return list.Select(g => new FirmaKopyalamaKayitOzeti
        {
            Id = g.Id,
            Kod = g.GuzergahKodu,
            Ad = g.GuzergahAdi,
            EkBilgi = $"{g.BaslangicNoktasi} → {g.BitisNoktasi}",
            HedefteVarMi = hedefSet.Contains(g.GuzergahKodu)
        }).ToList();
    }

    private static async Task<FirmaKopyalamaSonucu> KopyalaGuzergahAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId, List<int> ids)
    {
        var hedefSet = (await ctx.Guzergahlar.IgnoreQueryFilters()
            .Where(g => !g.IsDeleted && g.FirmaId == hedefFirmaId)
            .Select(g => g.GuzergahKodu)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var kaynak = await ctx.Guzergahlar.IgnoreQueryFilters()
            .Where(g => !g.IsDeleted && g.FirmaId == kaynakFirmaId && ids.Contains(g.Id))
            .ToListAsync();

        var sonuc = new FirmaKopyalamaSonucu();
        int kopyalanan = 0;
        foreach (var src in kaynak)
        {
            if (hedefSetContains(hedefSet, src.GuzergahKodu, sonuc, src.GuzergahKodu)) continue;

            var clone = new Guzergah
            {
                GuzergahKodu = src.GuzergahKodu,
                GuzergahAdi = src.GuzergahAdi,
                BaslangicNoktasi = src.BaslangicNoktasi,
                BitisNoktasi = src.BitisNoktasi,
                BaslangicLatitude = src.BaslangicLatitude,
                BaslangicLongitude = src.BaslangicLongitude,
                BitisLatitude = src.BitisLatitude,
                BitisLongitude = src.BitisLongitude,
                RotaRengi = src.RotaRengi,
                BirimFiyat = src.BirimFiyat,
                GiderFiyat = src.GiderFiyat,
                Mesafe = src.Mesafe,
                TahminiSure = src.TahminiSure,
                Aktif = src.Aktif,
                Notlar = src.Notlar,
                SeferTipi = src.SeferTipi,
                PersonelSayisi = src.PersonelSayisi,
                KapasiteAdi = src.KapasiteAdi,
                // Araç/şoför/kurum/cari referansları her firmada farklı; null bırakılır
                VarsayilanAracId = null,
                VarsayilanSoforId = null,
                KurumId = null,
                CariId = 0,
                FaturaKalemId = null,
                FirmaId = hedefFirmaId,
                KaynakFirmaId = kaynakFirmaId,
                KaynakKayitId = src.Id,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Guzergahlar.Add(clone);
            hedefSet.Add(src.GuzergahKodu);
            kopyalanan++;
        }
        await ctx.SaveChangesAsync();
        return WithCount(sonuc, kopyalanan);
    }

    // ---------------- ARAC ----------------
    private static async Task<List<FirmaKopyalamaKayitOzeti>> ListeleAracAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId)
    {
        var hedefSet = (await ctx.Araclar.IgnoreQueryFilters()
            .Where(a => !a.IsDeleted && a.FirmaId == hedefFirmaId)
            .Select(a => a.SaseNo)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var list = await ctx.Araclar.IgnoreQueryFilters()
            .Where(a => !a.IsDeleted && a.FirmaId == kaynakFirmaId)
            .OrderBy(a => a.AktifPlaka)
            .Select(a => new { a.Id, a.SaseNo, a.AktifPlaka, a.Marka, a.Model, a.SahiplikTipi })
            .ToListAsync();

        return list.Select(a => new FirmaKopyalamaKayitOzeti
        {
            Id = a.Id,
            Kod = a.AktifPlaka ?? a.SaseNo,
            Ad = $"{a.Marka} {a.Model}".Trim(),
            EkBilgi = a.SahiplikTipi.ToString(),
            HedefteVarMi = hedefSet.Contains(a.SaseNo)
        }).ToList();
    }

    private static async Task<FirmaKopyalamaSonucu> KopyalaAracAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId, List<int> ids)
    {
        var hedefSet = (await ctx.Araclar.IgnoreQueryFilters()
            .Where(a => !a.IsDeleted && a.FirmaId == hedefFirmaId)
            .Select(a => a.SaseNo)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var kaynak = await ctx.Araclar.IgnoreQueryFilters()
            .Where(a => !a.IsDeleted && a.FirmaId == kaynakFirmaId && ids.Contains(a.Id))
            .ToListAsync();

        var sonuc = new FirmaKopyalamaSonucu();
        int kopyalanan = 0;
        foreach (var src in kaynak)
        {
            if (hedefSetContains(hedefSet, src.SaseNo, sonuc, src.SaseNo)) continue;

            var clone = new Arac
            {
                SaseNo = src.SaseNo,
                AktifPlaka = src.AktifPlaka,
                Marka = src.Marka,
                Model = src.Model,
                ModelYili = src.ModelYili,
                MotorNo = src.MotorNo,
                Renk = src.Renk,
                KoltukSayisi = src.KoltukSayisi,
                AracTipi = src.AracTipi,
                AracSinifi = src.AracSinifi,
                SahiplikTipi = src.SahiplikTipi,
                // Cari/tedarikçi referansları null'lanır (hedef firmanın kendi carileri olacak)
                KiralikCariId = null,
                GunlukKiraBedeli = src.GunlukKiraBedeli,
                AylikKiraBedeli = src.AylikKiraBedeli,
                SeferBasinaKiraBedeli = src.SeferBasinaKiraBedeli,
                KiraHesaplamaTipi = src.KiraHesaplamaTipi,
                KomisyonVar = src.KomisyonVar,
                KomisyoncuCariId = null,
                KomisyonOrani = src.KomisyonOrani,
                SabitKomisyonTutari = src.SabitKomisyonTutari,
                KomisyonHesaplamaTipi = src.KomisyonHesaplamaTipi,
                TasimaTedarikciId = null,
                TrafikSigortaBitisTarihi = src.TrafikSigortaBitisTarihi,
                KaskoBitisTarihi = src.KaskoBitisTarihi,
                MuayeneBitisTarihi = src.MuayeneBitisTarihi,
                KoltukSigortasiBaslangiçTarihi = src.KoltukSigortasiBaslangiçTarihi,
                KoltukSigortasiBitisTarihi = src.KoltukSigortasiBitisTarihi,
                KmDurumu = src.KmDurumu,
                Durumu = src.Durumu,
                Aktif = src.Aktif,
                Notlar = src.Notlar,
                FirmaId = hedefFirmaId,
                KaynakFirmaId = kaynakFirmaId,
                KaynakKayitId = src.Id,
                CreatedAt = DateTime.UtcNow
                // PlakaGecmisi / AracEvrak / AracMasraf / KiralikPlakaTakip kopyalanmaz (hareketsel)
            };
            ctx.Araclar.Add(clone);
            hedefSet.Add(src.SaseNo);
            kopyalanan++;
        }
        await ctx.SaveChangesAsync();
        return WithCount(sonuc, kopyalanan);
    }

    // ---------------- SOFOR ----------------
    private static async Task<List<FirmaKopyalamaKayitOzeti>> ListeleSoforAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId)
    {
        var hedefSet = (await ctx.Soforler.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.FirmaId == hedefFirmaId)
            .Select(s => s.SoforKodu)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var list = await ctx.Soforler.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.FirmaId == kaynakFirmaId)
            .OrderBy(s => s.SoforKodu)
            .Select(s => new { s.Id, s.SoforKodu, s.Ad, s.Soyad, s.Gorev })
            .ToListAsync();

        return list.Select(s => new FirmaKopyalamaKayitOzeti
        {
            Id = s.Id,
            Kod = s.SoforKodu,
            Ad = $"{s.Ad} {s.Soyad}".Trim(),
            EkBilgi = s.Gorev.ToString(),
            HedefteVarMi = hedefSet.Contains(s.SoforKodu)
        }).ToList();
    }

    private static async Task<FirmaKopyalamaSonucu> KopyalaSoforAsync(
        ApplicationDbContext ctx, int kaynakFirmaId, int hedefFirmaId, List<int> ids)
    {
        var hedefSet = (await ctx.Soforler.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.FirmaId == hedefFirmaId)
            .Select(s => s.SoforKodu)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var kaynak = await ctx.Soforler.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.FirmaId == kaynakFirmaId && ids.Contains(s.Id))
            .ToListAsync();

        var sonuc = new FirmaKopyalamaSonucu();
        int kopyalanan = 0;
        foreach (var src in kaynak)
        {
            if (hedefSetContains(hedefSet, src.SoforKodu, sonuc, src.SoforKodu)) continue;

            var clone = new Sofor
            {
                SoforKodu = src.SoforKodu,
                Ad = src.Ad,
                Soyad = src.Soyad,
                TcKimlikNo = src.TcKimlikNo,
                Telefon = src.Telefon,
                Email = src.Email,
                Adres = src.Adres,
                SiralamaNo = src.SiralamaNo,
                Gorev = src.Gorev,
                Departman = src.Departman,
                Pozisyon = src.Pozisyon,
                EhliyetNo = src.EhliyetNo,
                EhliyetGecerlilikTarihi = src.EhliyetGecerlilikTarihi,
                MykBelgesiGecerlilikTarihi = src.MykBelgesiGecerlilikTarihi,
                YayginEgitimSertifikasiVarMi = src.YayginEgitimSertifikasiVarMi,
                SrcBelgesiGecerlilikTarihi = src.SrcBelgesiGecerlilikTarihi,
                PsikoteknikGecerlilikTarihi = src.PsikoteknikGecerlilikTarihi,
                SaglikRaporuGecerlilikTarihi = src.SaglikRaporuGecerlilikTarihi,
                KimlikGecerlilikTarihi = src.KimlikGecerlilikTarihi,
                AdliSicilGecerlilikTarihi = src.AdliSicilGecerlilikTarihi,
                SuruculCezaBarkodluBelgeTarihi = src.SuruculCezaBarkodluBelgeTarihi,
                IseBaslamaTarihi = src.IseBaslamaTarihi,
                IstenAyrilmaTarihi = src.IstenAyrilmaTarihi,
                SgkCikisTarihi = src.SgkCikisTarihi,
                Aktif = src.Aktif,
                Notlar = src.Notlar,
                BrutMaasHesaplamaTipi = src.BrutMaasHesaplamaTipi,
                CalismaMiktari = src.CalismaMiktari,
                BirimUcret = src.BirimUcret,
                BrutMaas = src.BrutMaas,
                ResmiNetMaas = src.ResmiNetMaas,
                DigerMaas = src.DigerMaas,
                NetMaas = src.NetMaas,
                TasimaTedarikciId = null,
                SGKBordroDahilMi = src.SGKBordroDahilMi,
                BordroTipiPersonel = src.BordroTipiPersonel,
                SgkCalismaTuru = src.SgkCalismaTuru,
                ArgePersoneli = src.ArgePersoneli,
                TopluMaas = src.TopluMaas,
                SgkMaasi = src.SgkMaasi,
                FirmaId = hedefFirmaId,
                KaynakFirmaId = kaynakFirmaId,
                KaynakKayitId = src.Id,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Soforler.Add(clone);
            hedefSet.Add(src.SoforKodu);
            kopyalanan++;
        }
        await ctx.SaveChangesAsync();
        return WithCount(sonuc, kopyalanan);
    }

    // ---------------- Helpers ----------------
    private static bool hedefSetContains(HashSet<string> set, string kod, FirmaKopyalamaSonucu sonuc, string atlananKod)
    {
        if (string.IsNullOrWhiteSpace(kod)) return false;
        if (set.Contains(kod))
        {
            sonuc.AtlananKodlar.Add(atlananKod);
            return true;
        }
        return false;
    }

    private static FirmaKopyalamaSonucu WithCount(FirmaKopyalamaSonucu sonuc, int kopyalanan)
    {
        return new FirmaKopyalamaSonucu
        {
            KopyalananSayisi = kopyalanan,
            AtlananSayisi = sonuc.AtlananKodlar.Count,
            AtlananKodlar = sonuc.AtlananKodlar,
            Hatalar = sonuc.Hatalar
        };
    }
}



