using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Destek;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>Departman, üye, kategori, SLA, hazır yanıt, genel ayarlar ve bilgi bankası.</summary>
public partial class DestekTalebiService
{
    #region Departmanlar ve üyeler

    public async Task<List<DestekDepartman>> GetDepartmanlarAsync(bool pasiflerDahil = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekDepartmanlari
            .AsNoTracking()
            .Include(x => x.Kategoriler.Where(k => k.Aktif))
            .Include(x => x.Uyeler)
            .Where(x => pasiflerDahil || x.Aktif)
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<DestekDepartman> DepartmanKaydetAsync(DestekDepartman girdi)
    {
        await TanimYetkisiGerekliAsync();
        var ad = Kirp(girdi.Ad, 100);
        if (ad.Length == 0) throw new InvalidOperationException("Departman adı zorunludur.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var adlar = await context.DestekDepartmanlari.AsNoTracking()
            .Where(d => d.Id != girdi.Id)
            .Select(d => d.Ad)
            .ToListAsync();
        if (adlar.Any(a => string.Equals(AramaMetni.Katla(a), AramaMetni.Katla(ad), StringComparison.Ordinal)))
            throw new InvalidOperationException($"'{ad}' adında bir departman zaten var.");

        DestekDepartman departman;
        if (girdi.Id > 0)
        {
            departman = await context.DestekDepartmanlari.FirstOrDefaultAsync(d => d.Id == girdi.Id)
                ?? throw new InvalidOperationException("Departman bulunamadı.");
            if (departman.Aktif && !girdi.Aktif)
                await AyarlardaKullanilmiyorAsync(context, departman.Id, "pasifleştirmeden");
            departman.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            departman = new DestekDepartman { CreatedAt = DateTime.UtcNow };
            context.DestekDepartmanlari.Add(departman);
        }

        departman.Ad = ad;
        departman.Aciklama = string.IsNullOrWhiteSpace(girdi.Aciklama) ? null : girdi.Aciklama.Trim();
        departman.Email = string.IsNullOrWhiteSpace(girdi.Email) ? null : Kirp(girdi.Email, 200);
        departman.VarsayilanSlaSuresi = girdi.VarsayilanSlaSuresi is > 0 ? girdi.VarsayilanSlaSuresi : null;
        departman.SiraNo = girdi.SiraNo;
        departman.Aktif = girdi.Aktif;
        departman.OtomatikAtama = girdi.OtomatikAtama;

        await context.SaveChangesAsync();
        return departman;
    }

    public async Task<string> DepartmanSilAsync(int departmanId)
    {
        await TanimYetkisiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var departman = await context.DestekDepartmanlari.FirstOrDefaultAsync(d => d.Id == departmanId)
            ?? throw new InvalidOperationException("Departman bulunamadı.");
        await AyarlardaKullanilmiyorAsync(context, departman.Id, "silmeden");

        // Talebi olan departman silinirse talepleri hiçbir listede görünmez (zorunlu ilişki + silinmiş filtresi);
        // bu yüzden yalnız pasifleştirilir.
        if (await context.DestekTalepleri.AnyAsync(t => t.DepartmanId == departman.Id))
        {
            departman.Aktif = false;
            departman.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return $"'{departman.Ad}' departmanında talep olduğu için silinmedi, pasifleştirildi (yeni taleplerde seçilemez).";
        }

        departman.IsDeleted = true;
        departman.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return $"'{departman.Ad}' departmanı silindi.";
    }

    public async Task<List<DestekDepartmanUye>> GetDepartmanUyeleriAsync(int departmanId)
    {
        await TanimYetkisiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekDepartmanUyeleri
            .AsNoTracking()
            .Include(x => x.Kullanici)
            .Where(x => x.DepartmanId == departmanId)
            .OrderByDescending(x => x.Yonetici)
            .ThenBy(x => x.Kullanici.AdSoyad)
            .ToListAsync();
    }

    public async Task DepartmanUyesiEkleAsync(int departmanId, int kullaniciId, bool yonetici)
    {
        await TanimYetkisiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (!await context.DestekDepartmanlari.AnyAsync(d => d.Id == departmanId))
            throw new InvalidOperationException("Departman bulunamadı.");
        if (!await context.Kullanicilar.AnyAsync(k => k.Id == kullaniciId && k.Aktif))
            throw new InvalidOperationException("Kullanıcı bulunamadı ya da pasif.");

        // (DepartmanId, KullaniciId) benzersiz indeksi silinmiş satırları da kapsar: önce eski satırı canlandır.
        var mevcut = await context.DestekDepartmanUyeleri
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.DepartmanId == departmanId && x.KullaniciId == kullaniciId);

        if (mevcut != null)
        {
            mevcut.IsDeleted = false;
            mevcut.Yonetici = yonetici;
            mevcut.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            context.DestekDepartmanUyeleri.Add(new DestekDepartmanUye
            {
                DepartmanId = departmanId,
                KullaniciId = kullaniciId,
                Yonetici = yonetici,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task DepartmanUyesiCikarAsync(int departmanId, int kullaniciId)
    {
        await TanimYetkisiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var uye = await context.DestekDepartmanUyeleri
            .FirstOrDefaultAsync(x => x.DepartmanId == departmanId && x.KullaniciId == kullaniciId);
        if (uye == null) return;

        uye.IsDeleted = true;
        uye.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<List<Kullanici>> GetAktifKullanicilarAsync()
    {
        var aktor = await OturumGerekliAsync();
        if (!aktor.TanimYonetir && !aktor.EkipUyesi) throw YetkiYok();

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Kullanicilar
            .AsNoTracking()
            .Include(k => k.Rol)
            .Where(k => k.Aktif)
            .OrderBy(k => k.AdSoyad)
            .ToListAsync();
    }

    private async Task<DestekKullanici> TanimYetkisiGerekliAsync()
    {
        var aktor = await OturumGerekliAsync();
        return aktor.TanimYonetir ? aktor : throw YetkiYok("Destek tanımlarını değiştirmek için destektalebi.duzenle yetkisi gerekir.");
    }

    /// <summary>
    /// Ayarlarda varsayılan ya da yazılım ekibi olarak seçili departman silinemez/pasifleştirilemez.
    /// <paramref name="islem"/> uyarıdaki fiildir ("silmeden", "pasifleştirmeden").
    /// </summary>
    private static async Task AyarlardaKullanilmiyorAsync(ApplicationDbContext context, int departmanId, string islem)
    {
        var ayarlar = await AyarlariOkuAsync(context);
        string? rol = ayarlar.VarsayilanDepartmanId == departmanId ? "sorun bildirimlerinin varsayılan departmanı"
            : ayarlar.YazilimEkibiDepartmanId == departmanId ? "yazılım ekibi departmanı"
            : null;
        if (rol != null)
            throw new InvalidOperationException($"Bu departman Destek Ayarları'nda {rol} olarak seçili; {islem} önce ayarlardan başka bir departman seçin.");
    }

    #endregion

    #region Kategoriler

    public async Task<List<DestekKategori>> GetKategorilerAsync(bool pasiflerDahil = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekKategorileri
            .AsNoTracking()
            .Include(x => x.Departman)
            .Where(x => pasiflerDahil || x.Aktif)
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<DestekKategori> KategoriKaydetAsync(DestekKategori girdi)
    {
        await TanimYetkisiGerekliAsync();
        var ad = Kirp(girdi.Ad, 100);
        if (ad.Length == 0) throw new InvalidOperationException("Kategori adı zorunludur.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        if (girdi.DepartmanId.HasValue && !await context.DestekDepartmanlari.AnyAsync(d => d.Id == girdi.DepartmanId))
            throw new InvalidOperationException("Departman bulunamadı.");

        DestekKategori kategori;
        if (girdi.Id > 0)
        {
            kategori = await context.DestekKategorileri.FirstOrDefaultAsync(k => k.Id == girdi.Id)
                ?? throw new InvalidOperationException("Kategori bulunamadı.");
            kategori.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            kategori = new DestekKategori { CreatedAt = DateTime.UtcNow };
            context.DestekKategorileri.Add(kategori);
        }

        kategori.Ad = ad;
        kategori.Aciklama = string.IsNullOrWhiteSpace(girdi.Aciklama) ? null : girdi.Aciklama.Trim();
        kategori.Renk = RenkKodu(girdi.Renk);
        kategori.Simge = SimgeSinifi(girdi.Simge);
        kategori.SiraNo = girdi.SiraNo;
        kategori.Aktif = girdi.Aktif;
        kategori.DepartmanId = girdi.DepartmanId;

        await context.SaveChangesAsync();
        return kategori;
    }

    public async Task KategoriSilAsync(int kategoriId)
    {
        await TanimYetkisiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kategori = await context.DestekKategorileri.FirstOrDefaultAsync(k => k.Id == kategoriId);
        if (kategori == null) return;

        kategori.IsDeleted = true;
        kategori.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    // Kategori rengi ve simgesi stil özniteliğine/sınıfa yazılır: yalnız güvenli biçimler kabul edilir.
    private static string? RenkKodu(string? renk) =>
        !string.IsNullOrWhiteSpace(renk) && Regex.IsMatch(renk.Trim(), "^#[0-9a-fA-F]{3,8}$") ? renk.Trim() : null;

    private static string? SimgeSinifi(string? simge) =>
        !string.IsNullOrWhiteSpace(simge) && Regex.IsMatch(simge.Trim(), "^bi-[a-z0-9-]{1,45}$") ? simge.Trim() : null;

    #endregion

    #region SLA

    public async Task<List<DestekSla>> GetSlaListesiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestekSlaListesi
            .AsNoTracking()
            .OrderByDescending(x => x.Oncelik)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<DestekSla> SlaKaydetAsync(DestekSla girdi)
    {
        await TanimYetkisiGerekliAsync();
        var ad = Kirp(girdi.Ad, 100);
        if (ad.Length == 0) throw new InvalidOperationException("SLA adı zorunludur.");
        if (girdi.CozumSuresi <= 0) throw new InvalidOperationException("Çözüm süresi en az 1 saat olmalıdır.");
        if (girdi.IlkYanitSuresi < 0) throw new InvalidOperationException("İlk yanıt süresi negatif olamaz.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        if (girdi.Aktif && await context.DestekSlaListesi.AnyAsync(s => s.Id != girdi.Id && s.Aktif && s.Oncelik == girdi.Oncelik))
            throw new InvalidOperationException($"{DestekKurallari.OncelikEtiketi(girdi.Oncelik)} öncelik için zaten aktif bir SLA var; önce onu pasifleştirin.");

        DestekSla sla;
        if (girdi.Id > 0)
        {
            sla = await context.DestekSlaListesi.FirstOrDefaultAsync(s => s.Id == girdi.Id)
                ?? throw new InvalidOperationException("SLA bulunamadı.");
            sla.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            sla = new DestekSla { CreatedAt = DateTime.UtcNow };
            context.DestekSlaListesi.Add(sla);
        }

        sla.Ad = ad;
        sla.Aciklama = string.IsNullOrWhiteSpace(girdi.Aciklama) ? null : girdi.Aciklama.Trim();
        sla.Oncelik = girdi.Oncelik;
        sla.IlkYanitSuresi = girdi.IlkYanitSuresi;
        sla.CozumSuresi = girdi.CozumSuresi;
        sla.Aktif = girdi.Aktif;
        sla.SadeceMesaiSaatleri = girdi.SadeceMesaiSaatleri;
        sla.SadeceHaftaIci = girdi.SadeceHaftaIci;

        await context.SaveChangesAsync();
        return sla;
    }

    public async Task SlaSilAsync(int slaId)
    {
        await TanimYetkisiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sla = await context.DestekSlaListesi.FirstOrDefaultAsync(s => s.Id == slaId);
        if (sla == null) return;

        sla.IsDeleted = true;
        sla.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    #endregion

    #region Hazır yanıtlar

    public async Task<List<DestekHazirYanit>> GetHazirYanitlarAsync(int? departmanId = null, int? kategoriId = null, bool pasiflerDahil = false)
    {
        // Hazır yanıtlar yalnız ekip içindir; talep edene boş liste döner.
        var aktor = await OturumGerekliAsync();
        if (!aktor.EkipUyesi && !aktor.TanimYonetir) return [];

        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.DestekHazirYanitlari
            .AsNoTracking()
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Where(x => pasiflerDahil || x.Aktif);

        if (departmanId.HasValue)
            query = query.Where(x => x.DepartmanId == null || x.DepartmanId == departmanId.Value);
        if (kategoriId.HasValue)
            query = query.Where(x => x.KategoriId == null || x.KategoriId == kategoriId.Value);

        return await query
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<DestekHazirYanit> HazirYanitKaydetAsync(DestekHazirYanit girdi)
    {
        await TanimYetkisiGerekliAsync();
        var ad = Kirp(girdi.Ad, 200);
        if (ad.Length == 0) throw new InvalidOperationException("Şablon adı zorunludur.");
        if (string.IsNullOrWhiteSpace(girdi.Icerik)) throw new InvalidOperationException("Şablon içeriği zorunludur.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        DestekHazirYanit hazirYanit;
        if (girdi.Id > 0)
        {
            hazirYanit = await context.DestekHazirYanitlari.FirstOrDefaultAsync(h => h.Id == girdi.Id)
                ?? throw new InvalidOperationException("Hazır yanıt bulunamadı.");
            hazirYanit.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            hazirYanit = new DestekHazirYanit { CreatedAt = DateTime.UtcNow };
            context.DestekHazirYanitlari.Add(hazirYanit);
        }

        hazirYanit.Ad = ad;
        hazirYanit.Icerik = girdi.Icerik.Trim();
        hazirYanit.KonuSablonu = string.IsNullOrWhiteSpace(girdi.KonuSablonu) ? null : Kirp(girdi.KonuSablonu, 500);
        hazirYanit.Aciklama = string.IsNullOrWhiteSpace(girdi.Aciklama) ? null : girdi.Aciklama.Trim();
        hazirYanit.DepartmanId = girdi.DepartmanId;
        hazirYanit.KategoriId = girdi.KategoriId;
        hazirYanit.SiraNo = girdi.SiraNo;
        hazirYanit.Aktif = girdi.Aktif;

        await context.SaveChangesAsync();
        return hazirYanit;
    }

    public async Task HazirYanitSilAsync(int hazirYanitId)
    {
        await TanimYetkisiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hazirYanit = await context.DestekHazirYanitlari.FirstOrDefaultAsync(h => h.Id == hazirYanitId);
        if (hazirYanit == null) return;

        hazirYanit.IsDeleted = true;
        hazirYanit.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private static async Task HazirYanitKullanimiArttirAsync(ApplicationDbContext context, int hazirYanitId)
    {
        await context.DestekHazirYanitlari
            .Where(x => x.Id == hazirYanitId)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.KullanimSayisi, p => p.KullanimSayisi + 1));
    }

    #endregion

    #region Genel ayarlar

    public async Task<DestekGenelAyarlar> GetGenelAyarlarAsync()
    {
        await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await AyarlariOkuAsync(context);
    }

    public async Task GenelAyarlariKaydetAsync(DestekGenelAyarlar ayarlar)
    {
        await TanimYetkisiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        async Task<int?> AktifDepartman(int? id, string ad)
        {
            if (id is not > 0) return null;
            return await context.DestekDepartmanlari.AnyAsync(d => d.Id == id && d.Aktif)
                ? id
                : throw new InvalidOperationException($"{ad} için seçilen departman bulunamadı ya da pasif.");
        }

        var varsayilan = await AktifDepartman(ayarlar.VarsayilanDepartmanId, "Varsayılan departman");
        var yazilim = await AktifDepartman(ayarlar.YazilimEkibiDepartmanId, "Yazılım ekibi");
        var kapatma = Math.Clamp(ayarlar.OtomatikKapatmaGun, 0, 90);
        var boyut = Math.Clamp(ayarlar.MaksimumDosyaBoyutuMB, 1, 50);
        var tipler = string.Join(",", new DestekGenelAyarlar { IzinliDosyaTipleri = ayarlar.IzinliDosyaTipleri ?? string.Empty }.IzinliUzantilar());
        if (tipler.Length == 0) tipler = DestekGenelAyarlar.VarsayilanDosyaTipleri;

        await AyarYazAsync(context, DestekGenelAyarlar.DestekEmailAnahtari, ayarlar.DestekEmail?.Trim() ?? string.Empty, "Destek modülü e-posta adresi");
        await AyarYazAsync(context, DestekGenelAyarlar.VarsayilanDepartmanAnahtari, varsayilan?.ToString() ?? string.Empty, "Sorun bildirimlerinin düştüğü departman");
        await AyarYazAsync(context, DestekGenelAyarlar.YazilimDepartmaniAnahtari, yazilim?.ToString() ?? string.Empty, "Yazılım ekibine aktarılan taleplerin departmanı");
        await AyarYazAsync(context, DestekGenelAyarlar.EmailBildirimAnahtari, ayarlar.EmailBildirimAktif.ToString(), "Destek modülü e-posta bildirimleri aktif mi");
        await AyarYazAsync(context, DestekGenelAyarlar.OtomatikSlaAnahtari, ayarlar.OtomatikSlaKontrolu.ToString(), "SLA kontrolü otomatik çalışsın mı");
        await AyarYazAsync(context, DestekGenelAyarlar.BilgiBankasiAnahtari, ayarlar.BilgiBankasiAcik.ToString(), "Bilgi bankası kullanıcılara açık mı");
        await AyarYazAsync(context, DestekGenelAyarlar.OtomatikKapatmaAnahtari, kapatma.ToString(), "Çözüldü durumundaki talep kaç gün sonra kapansın (0 = kapanmasın)");
        await AyarYazAsync(context, DestekGenelAyarlar.DosyaBoyutuAnahtari, boyut.ToString(), "Maksimum dosya boyutu (MB)");
        await AyarYazAsync(context, DestekGenelAyarlar.DosyaTipleriAnahtari, tipler, "İzin verilen dosya uzantıları");

        await context.SaveChangesAsync();
    }

    private static readonly string[] AyarAnahtarlari =
    [
        DestekGenelAyarlar.DestekEmailAnahtari, DestekGenelAyarlar.VarsayilanDepartmanAnahtari, DestekGenelAyarlar.YazilimDepartmaniAnahtari,
        DestekGenelAyarlar.EmailBildirimAnahtari, DestekGenelAyarlar.OtomatikSlaAnahtari, DestekGenelAyarlar.BilgiBankasiAnahtari,
        DestekGenelAyarlar.OtomatikKapatmaAnahtari, DestekGenelAyarlar.DosyaBoyutuAnahtari, DestekGenelAyarlar.DosyaTipleriAnahtari
    ];

    /// <summary>Ayarları anahtarla okur (eski kurulumlarda bazıları farklı gruplarda kayıtlıdır); olmayanlar varsayılan değeri alır.</summary>
    private static async Task<DestekGenelAyarlar> AyarlariOkuAsync(ApplicationDbContext context)
    {
        var degerler = await context.DestekAyarlari.AsNoTracking()
            .Where(a => AyarAnahtarlari.Contains(a.Anahtar))
            .Select(a => new { a.Anahtar, a.Deger })
            .ToListAsync();
        var sozluk = degerler
            .GroupBy(a => a.Anahtar)
            .ToDictionary(g => g.Key, g => g.First().Deger);

        string? Metin(string anahtar) => sozluk.TryGetValue(anahtar, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
        int? Sayi(string anahtar) => int.TryParse(Metin(anahtar), out var v) ? v : null;
        bool Evet(string anahtar, bool varsayilan) => bool.TryParse(Metin(anahtar), out var v) ? v : varsayilan;

        return new DestekGenelAyarlar
        {
            DestekEmail = Metin(DestekGenelAyarlar.DestekEmailAnahtari),
            VarsayilanDepartmanId = Sayi(DestekGenelAyarlar.VarsayilanDepartmanAnahtari) is > 0 and var v ? v : null,
            YazilimEkibiDepartmanId = Sayi(DestekGenelAyarlar.YazilimDepartmaniAnahtari) is > 0 and var y ? y : null,
            EmailBildirimAktif = Evet(DestekGenelAyarlar.EmailBildirimAnahtari, false),
            OtomatikSlaKontrolu = Evet(DestekGenelAyarlar.OtomatikSlaAnahtari, true),
            BilgiBankasiAcik = Evet(DestekGenelAyarlar.BilgiBankasiAnahtari, true),
            OtomatikKapatmaGun = Math.Clamp(Sayi(DestekGenelAyarlar.OtomatikKapatmaAnahtari) ?? 7, 0, 90),
            MaksimumDosyaBoyutuMB = Math.Clamp(Sayi(DestekGenelAyarlar.DosyaBoyutuAnahtari) ?? 10, 1, 50),
            IzinliDosyaTipleri = Metin(DestekGenelAyarlar.DosyaTipleriAnahtari) ?? DestekGenelAyarlar.VarsayilanDosyaTipleri
        };
    }

    /// <summary>Ayarı yazar; (benzersiz anahtarlı) silinmiş satırı canlandırır.</summary>
    private static async Task AyarYazAsync(ApplicationDbContext context, string anahtar, string deger, string aciklama)
    {
        var ayar = await context.DestekAyarlari.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Anahtar == anahtar);
        if (ayar == null)
        {
            context.DestekAyarlari.Add(new DestekAyar
            {
                Anahtar = anahtar,
                Deger = Kirp(deger, 2000),
                Aciklama = aciklama,
                Grup = "Destek",
                CreatedAt = DateTime.UtcNow
            });
            return;
        }

        ayar.IsDeleted = false;
        ayar.Deger = Kirp(deger, 2000);
        ayar.Aciklama = aciklama;
        ayar.Grup = "Destek";
        ayar.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Yazılım ekibi departmanı (ayarlı ve aktifse).</summary>
    private static async Task<int?> YazilimDepartmanIdAsync(ApplicationDbContext context)
    {
        var id = (await AyarlariOkuAsync(context)).YazilimEkibiDepartmanId;
        return id.HasValue && await context.DestekDepartmanlari.AnyAsync(d => d.Id == id && d.Aktif) ? id : null;
    }

    /// <summary>Sorun bildirimlerinin düştüğü departman: ayar → "Teknik Destek" → ilk aktif departman.</summary>
    private static async Task<int?> VarsayilanDepartmanAsync(ApplicationDbContext context, DestekGenelAyarlar ayarlar)
    {
        if (ayarlar.VarsayilanDepartmanId.HasValue &&
            await context.DestekDepartmanlari.AnyAsync(d => d.Id == ayarlar.VarsayilanDepartmanId && d.Aktif))
            return ayarlar.VarsayilanDepartmanId;

        var aktifler = await context.DestekDepartmanlari.AsNoTracking()
            .Where(d => d.Aktif && d.Id != (ayarlar.YazilimEkibiDepartmanId ?? 0))
            .OrderBy(d => d.SiraNo).ThenBy(d => d.Id)
            .Select(d => new { d.Id, d.Ad })
            .ToListAsync();
        return aktifler.FirstOrDefault(d => AramaMetni.Katla(d.Ad) == "teknik destek")?.Id ?? aktifler.FirstOrDefault()?.Id;
    }

    #endregion

    #region Bilgi bankası

    public async Task<PagedResult<DestekBilgiBankasi>> GetBilgiBankasiPagedAsync(BilgiBankasiFilterParams filter)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.DestekBilgiBankasiMakaleleri
            .AsNoTracking()
            .Include(x => x.Kategori)
            .Include(x => x.Yazar)
            .AsQueryable();

        if (!aktor.MakaleYazar)
            query = query.Where(x => x.Durum == BilgiBankasiDurum.Yayinda);
        else if (filter.Durum.HasValue)
            query = query.Where(x => x.Durum == filter.Durum.Value);
        if (filter.KategoriId.HasValue)
            query = query.Where(x => x.KategoriId == filter.KategoriId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Etiket))
            query = query.Where(x => x.Etiketler != null && x.Etiketler.Contains(filter.Etiket));

        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "baslik" => filter.SortDesc ? query.OrderByDescending(x => x.Baslik) : query.OrderBy(x => x.Baslik),
            "goruntulemesayisi" => filter.SortDesc ? query.OrderByDescending(x => x.GoruntulemeSayisi) : query.OrderBy(x => x.GoruntulemeSayisi),
            _ => filter.SortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };

        var sayfa = Math.Max(1, filter.Page);
        var boyut = Math.Clamp(filter.PageSize, 1, 200);
        var kelimeler = AramaMetni.Kelimeler(filter.AramaMetni);
        if (kelimeler.Count > 0)
        {
            var desen = AramaMetni.KabaDesen(AramaMetni.EnUzun(kelimeler));
            var adaylar = await query
                .Where(x => EF.Functions.Like(x.Baslik.ToLower(), desen, AramaMetni.KacisKarakteri)
                         || EF.Functions.Like(x.Icerik.ToLower(), desen, AramaMetni.KacisKarakteri)
                         || (x.Ozet != null && EF.Functions.Like(x.Ozet.ToLower(), desen, AramaMetni.KacisKarakteri))
                         || (x.Etiketler != null && EF.Functions.Like(x.Etiketler.ToLower(), desen, AramaMetni.KacisKarakteri)))
                .Take(AramaAdayLimiti)
                .ToListAsync();
            var eslesenler = adaylar
                .Where(x => AramaMetni.HepsiEslesir(kelimeler, x.Baslik, x.Ozet, x.Etiketler, x.Icerik))
                .ToList();
            return new PagedResult<DestekBilgiBankasi>
            {
                Items = eslesenler.Skip((sayfa - 1) * boyut).Take(boyut).ToList(),
                TotalCount = eslesenler.Count,
                PageNumber = sayfa,
                PageSize = boyut
            };
        }

        var toplam = await query.CountAsync();
        return new PagedResult<DestekBilgiBankasi>
        {
            Items = await query.Skip((sayfa - 1) * boyut).Take(boyut).ToListAsync(),
            TotalCount = toplam,
            PageNumber = sayfa,
            PageSize = boyut
        };
    }

    public async Task<DestekBilgiBankasi?> GetBilgiBankasiByIdAsync(int makaleId)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var makale = await context.DestekBilgiBankasiMakaleleri
            .AsNoTracking()
            .Include(x => x.Kategori)
            .Include(x => x.Yazar)
            .FirstOrDefaultAsync(x => x.Id == makaleId);
        return makale != null && (aktor.MakaleYazar || makale.Durum == BilgiBankasiDurum.Yayinda) ? makale : null;
    }

    public async Task<DestekBilgiBankasi> BilgiBankasiKaydetAsync(DestekBilgiBankasi girdi)
    {
        var aktor = await OturumGerekliAsync();
        if (!aktor.MakaleYazar) throw YetkiYok("Makale yazmak için destek personeli ya da destektalebi.duzenle yetkisi gerekir.");

        var baslik = Kirp(girdi.Baslik, 500);
        if (baslik.Length == 0) throw new InvalidOperationException("Başlık zorunludur.");
        if (string.IsNullOrWhiteSpace(girdi.Icerik)) throw new InvalidOperationException("İçerik zorunludur.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        DestekBilgiBankasi makale;
        if (girdi.Id > 0)
        {
            makale = await context.DestekBilgiBankasiMakaleleri.FirstOrDefaultAsync(m => m.Id == girdi.Id)
                ?? throw new InvalidOperationException("Makale bulunamadı.");
            makale.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            makale = new DestekBilgiBankasi { CreatedAt = DateTime.UtcNow, YazarKullaniciId = aktor.Id };
            context.DestekBilgiBankasiMakaleleri.Add(makale);
        }

        makale.Baslik = baslik;
        makale.Icerik = girdi.Icerik.Trim();
        makale.Ozet = string.IsNullOrWhiteSpace(girdi.Ozet) ? null : girdi.Ozet.Trim();
        makale.Etiketler = EtiketleriDuzenle(girdi.Etiketler);
        makale.KategoriId = girdi.KategoriId;
        makale.Durum = girdi.Durum;
        makale.SeoBaslik = string.IsNullOrWhiteSpace(girdi.SeoBaslik) ? null : Kirp(girdi.SeoBaslik, 200);
        makale.SeoAciklama = string.IsNullOrWhiteSpace(girdi.SeoAciklama) ? null : girdi.SeoAciklama.Trim();
        if (string.IsNullOrWhiteSpace(makale.Slug))
            makale.Slug = await BenzersizSlugAsync(context, baslik, makale.Id);
        if (makale.Durum == BilgiBankasiDurum.Yayinda && !makale.YayinlanmaTarihi.HasValue)
            makale.YayinlanmaTarihi = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return makale;
    }

    public async Task BilgiBankasiSilAsync(int makaleId)
    {
        var aktor = await OturumGerekliAsync();
        if (!aktor.MakaleYazar) throw YetkiYok();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var makale = await context.DestekBilgiBankasiMakaleleri.FirstOrDefaultAsync(m => m.Id == makaleId);
        if (makale == null) return;

        makale.IsDeleted = true;
        makale.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task IncrementBilgiBankasiGoruntulenmeAsync(int makaleId)
    {
        await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.DestekBilgiBankasiMakaleleri
            .Where(x => x.Id == makaleId)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.GoruntulemeSayisi, p => p.GoruntulemeSayisi + 1));
    }

    public async Task YararliBulAsync(int makaleId, bool yararli)
    {
        await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var makaleler = context.DestekBilgiBankasiMakaleleri.Where(x => x.Id == makaleId);
        if (yararli)
            await makaleler.ExecuteUpdateAsync(x => x.SetProperty(p => p.YararliBulmaSayisi, p => p.YararliBulmaSayisi + 1));
        else
            await makaleler.ExecuteUpdateAsync(x => x.SetProperty(p => p.YararsizBulmaSayisi, p => p.YararsizBulmaSayisi + 1));
    }

    public async Task<BilgiBankasiOzeti> GetBilgiBankasiOzetiAsync()
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.DestekBilgiBankasiMakaleleri.AsNoTracking();
        if (!aktor.MakaleYazar)
            query = query.Where(x => x.Durum == BilgiBankasiDurum.Yayinda);

        var satirlar = await query.Select(x => new { x.Durum, x.GoruntulemeSayisi }).ToListAsync();
        return new BilgiBankasiOzeti(
            satirlar.Count,
            satirlar.Count(x => x.Durum == BilgiBankasiDurum.Yayinda),
            satirlar.Count(x => x.Durum == BilgiBankasiDurum.Taslak),
            satirlar.Sum(x => x.GoruntulemeSayisi));
    }

    /// <summary>Başlıktan URL adı üretir; (silinmiş dahil) çakışırsa sonuna -2, -3 … ekler.</summary>
    private static async Task<string> BenzersizSlugAsync(ApplicationDbContext context, string baslik, int haricId)
    {
        var temel = Regex.Replace(AramaMetni.Katla(baslik), "[^a-z0-9]+", "-").Trim('-');
        if (temel.Length == 0) temel = "makale";
        if (temel.Length > 180) temel = temel[..180].Trim('-');

        var aday = temel;
        for (var sira = 2; await context.DestekBilgiBankasiMakaleleri.IgnoreQueryFilters()
                 .AnyAsync(m => m.Slug == aday && m.Id != haricId); sira++)
            aday = $"{temel}-{sira}";
        return aday;
    }

    #endregion
}
