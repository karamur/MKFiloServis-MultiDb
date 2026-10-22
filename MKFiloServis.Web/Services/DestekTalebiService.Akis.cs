using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Destek;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>Durum işlemleri, atama, aktarma ve ekip listeleri.</summary>
public partial class DestekTalebiService
{
    private sealed record BekleyenBildirim(IReadOnlyList<int?> Alicilar, string Baslik, string Icerik, bool EkipLinki, BildirimOncelik Oncelik);

    public async Task EylemUygulaAsync(int talepId, DestekEylem eylem, DestekEylemIstegi? istek = null)
    {
        istek ??= new DestekEylemIstegi();
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talep = await TalepYukleAsync(context, talepId);
        var yazilimDepartmanId = await YazilimDepartmanIdAsync(context);
        var rol = DestekErisim.Rol(aktor, talep, yazilimDepartmanId);
        if (!DestekErisim.Gorebilir(rol)) throw YetkiYok();

        var tanim = DestekKurallari.Tanim(eylem);
        if (!DestekKurallari.IzinliMi(eylem, talep.Durum, rol))
            throw new InvalidOperationException(
                $"\"{tanim.Etiket}\" işlemi, talep \"{DestekKurallari.Etiket(talep.Durum)}\" durumundayken sizin için kullanılamaz.");

        var not = istek.Not?.Trim();
        if (tanim.NotZorunlu && string.IsNullOrWhiteSpace(not))
            throw new InvalidOperationException($"{tanim.NotBasligi} zorunludur.");

        var simdi = DateTime.UtcNow;
        var eskiDurum = talep.Durum;
        var bildirimler = new List<BekleyenBildirim>();

        switch (eylem)
        {
            case DestekEylem.Gonder:
                talep.CreatedAt = simdi;
                await SlaUygulaAsync(context, talep, simdi);
                break;

            case DestekEylem.Ustlen:
                talep.AtananKullaniciId = aktor.Id;
                Aktivite(context, talep, AktiviteTuru.Atandi, $"Talep {aktor.AdSoyad} tarafından üstlenildi", aktor.Id);
                bildirimler.Add(new([talep.OlusturanKullaniciId], "Talebiniz işleme alındı",
                    $"{aktor.AdSoyad} talebinizle ilgileniyor.", false, BildirimOncelik.Normal));
                break;

            case DestekEylem.IslemeAl:
                bildirimler.Add(new([talep.OlusturanKullaniciId], "Talebiniz yeniden işlemde",
                    "Destek ekibi talebiniz üzerinde çalışmaya devam ediyor.", false, BildirimOncelik.Normal));
                break;

            case DestekEylem.BilgiIste:
                YanitEkle(context, talep, aktor, not!, YanitTuru.Yanit, dahili: false);
                talep.IlkYanitSuresiDakika ??= Dakika(talep.CreatedAt, simdi);
                talep.AtananKullaniciId ??= aktor.Id;
                bildirimler.Add(new([talep.OlusturanKullaniciId], "Destek ekibi sizden bilgi bekliyor", not!, false, BildirimOncelik.Yuksek));
                break;

            case DestekEylem.YazilimaAktar:
                await YazilimaAktarAsync(context, talep, aktor, yazilimDepartmanId, not!, bildirimler);
                break;

            case DestekEylem.DestegeGeriGonder:
            {
                var (oncekiDepartman, oncekiAtanan) = await SonYazilimAktarmasiAsync(context, talep.Id);
                var hedefDepartman = oncekiDepartman ?? await VarsayilanDepartmanAsync(context, await AyarlariOkuAsync(context));
                if (hedefDepartman.HasValue && await context.DestekDepartmanlari.AnyAsync(d => d.Id == hedefDepartman && d.Aktif))
                    talep.DepartmanId = hedefDepartman.Value;
                talep.AtananKullaniciId = oncekiAtanan;
                YanitEkle(context, talep, aktor, $"Yazılım ekibi notu:\n{not}", YanitTuru.DahiliNot, dahili: true);
                Aktivite(context, talep, AktiviteTuru.DestegeGeriGonderildi, "Talep destek ekibine geri gönderildi", aktor.Id);
                var destekEkibi = talep.AtananKullaniciId.HasValue
                    ? [talep.AtananKullaniciId]
                    : await DestekBildirimci.EkipAlicilariAsync(context, talep.DepartmanId, DestekKurallari.PersonelYetkisi);
                bildirimler.Add(new(destekEkibi, "Yazılım ekibi talebi geri gönderdi", not!, true, BildirimOncelik.Normal));
                break;
            }

            case DestekEylem.Coz:
                YanitEkle(context, talep, aktor, not!, YanitTuru.Yanit, dahili: false);
                talep.IlkYanitSuresiDakika ??= Dakika(talep.CreatedAt, simdi);
                talep.CozumSuresiDakika = Dakika(talep.CreatedAt, simdi);
                talep.AtananKullaniciId ??= aktor.Id;
                bildirimler.Add(new([talep.OlusturanKullaniciId], "Talebiniz çözüldü, onayınız bekleniyor", not!, false, BildirimOncelik.Yuksek));
                break;

            case DestekEylem.Onayla:
            {
                talep.KapatilmaTarihi = simdi;
                talep.CozumSuresiDakika ??= Dakika(talep.CreatedAt, simdi);
                if (istek.MemnuniyetPuani is >= 1 and <= 5)
                {
                    talep.MemnuniyetPuani = istek.MemnuniyetPuani;
                    talep.MemnuniyetYorumu = string.IsNullOrWhiteSpace(istek.MemnuniyetYorumu) ? null : Kirp(istek.MemnuniyetYorumu, 1000);
                }
                var puanMetni = talep.MemnuniyetPuani is int puan ? $" (memnuniyet {puan}/5)" : string.Empty;
                Aktivite(context, talep, AktiviteTuru.CozumOnaylandi, $"Çözüm talep eden tarafından onaylandı{puanMetni}", aktor.Id);
                bildirimler.Add(new(await EkipIlgilileriAsync(context, talep), "Talep eden çözümü onayladı",
                    $"{talep.Konu}{puanMetni}", true, BildirimOncelik.Normal));
                break;
            }

            case DestekEylem.ItirazEt:
                talep.CozumSuresiDakika = null;
                YanitEkle(context, talep, aktor, $"Sorun devam ediyor:\n{not}", YanitTuru.Yanit, dahili: false, musteri: true);
                Aktivite(context, talep, AktiviteTuru.CozumeItirazEdildi, "Talep eden çözüme itiraz etti", aktor.Id);
                bildirimler.Add(new(await EkipIlgilileriAsync(context, talep), "Talep eden çözüme itiraz etti", not!, true, BildirimOncelik.Yuksek));
                break;

            case DestekEylem.Kapat:
                talep.KapatilmaTarihi = simdi;
                talep.CozumSuresiDakika ??= Dakika(talep.CreatedAt, simdi);
                YanitEkle(context, talep, aktor, $"Talep kapatıldı: {not}", YanitTuru.SistemMesaji, dahili: false);
                Aktivite(context, talep, AktiviteTuru.Kapandi, "Talep kapatıldı", aktor.Id);
                bildirimler.Add(new([talep.OlusturanKullaniciId], "Talebiniz kapatıldı", not!, false, BildirimOncelik.Normal));
                break;

            case DestekEylem.YenidenAc:
                talep.KapatilmaTarihi = null;
                talep.CozumSuresiDakika = null;
                await SlaUygulaAsync(context, talep, simdi);
                if (!string.IsNullOrWhiteSpace(not))
                    YanitEkle(context, talep, aktor, $"Yeniden açma notu:\n{not}", YanitTuru.DahiliNot, dahili: true);
                Aktivite(context, talep, AktiviteTuru.YenidenAcildi, "Talep yeniden açıldı", aktor.Id);
                bildirimler.Add(new([talep.OlusturanKullaniciId], "Talebiniz yeniden açıldı",
                    "Destek ekibi talebiniz üzerinde yeniden çalışıyor.", false, BildirimOncelik.Normal));
                bildirimler.Add(new([talep.AtananKullaniciId], "Talep yeniden açıldı", talep.Konu, true, BildirimOncelik.Normal));
                break;
        }

        talep.Durum = tanim.Hedef;
        talep.SonAktiviteTarihi = simdi;
        talep.UpdatedAt = simdi;
        if (eskiDurum != talep.Durum)
            DurumAktivitesi(context, talep, eskiDurum, aktor.Id);

        await context.SaveChangesAsync();

        if (eylem == DestekEylem.Gonder)
        {
            await GonderilenTalebiDuyurAsync(context, talep, aktor);
            return;
        }

        await BildirimleriYazAsync(context, talep, aktor.Id, bildirimler);
        if (eskiDurum != talep.Durum && !DestekErisim.YalnizSahip(rol))
            _ = SendDestekEmailSafeAsync(() => SendDurumDegisiklikEmailAsync(talep, eskiDurum, talep.Durum));
    }

    public async Task AtaAsync(int talepId, int kullaniciId, string? not = null)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talep = await TalepYukleAsync(context, talepId);
        var rol = DestekErisim.Rol(aktor, talep, await YazilimDepartmanIdAsync(context));
        if (!DestekErisim.Isleyebilir(rol)) throw YetkiYok();
        if (talep.Durum is DestekDurum.Kapali or DestekDurum.Taslak)
            throw new InvalidOperationException("Kapanmış ya da taslak talep atanamaz.");

        var hedef = await context.Kullanicilar.AsNoTracking()
            .Where(k => k.Id == kullaniciId && k.Aktif)
            .Select(k => new { k.Id, k.AdSoyad, k.Email })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Atanacak kullanıcı bulunamadı ya da pasif.");

        var simdi = DateTime.UtcNow;
        var eskiDurum = talep.Durum;
        talep.AtananKullaniciId = hedef.Id;
        if (talep.Durum == DestekDurum.Yeni)
            talep.Durum = DestekDurum.Islemde;
        talep.SonAktiviteTarihi = simdi;
        talep.UpdatedAt = simdi;

        Aktivite(context, talep, AktiviteTuru.Atandi, $"Talep {hedef.AdSoyad} kullanıcısına atandı", aktor.Id);
        if (!string.IsNullOrWhiteSpace(not))
            YanitEkle(context, talep, aktor, $"Atama notu:\n{not.Trim()}", YanitTuru.DahiliNot, dahili: true);
        if (eskiDurum != talep.Durum)
            DurumAktivitesi(context, talep, eskiDurum, aktor.Id);

        await context.SaveChangesAsync();

        await BildirimleriYazAsync(context, talep, aktor.Id,
        [
            new([hedef.Id], "Size bir destek talebi atandı", string.IsNullOrWhiteSpace(not) ? talep.Konu : $"{talep.Konu} — {not.Trim()}", true,
                talep.Oncelik >= DestekOncelik.Acil ? BildirimOncelik.Yuksek : BildirimOncelik.Normal),
            new([talep.OlusturanKullaniciId], "Talebiniz işleme alındı", $"{hedef.AdSoyad} talebinizle ilgileniyor.", false, BildirimOncelik.Normal)
        ]);

        if (hedef.Id != aktor.Id)
            _ = SendDestekEmailSafeAsync(() => SendAtamaEmailAsync(talep, hedef.Email, hedef.AdSoyad));
    }

    public async Task TransferEtAsync(int talepId, int departmanId, string gerekce)
    {
        var aktor = await OturumGerekliAsync();
        gerekce = gerekce?.Trim() ?? string.Empty;
        if (gerekce.Length == 0)
            throw new InvalidOperationException("Aktarma gerekçesi zorunludur.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var talep = await TalepYukleAsync(context, talepId);
        var yazilimDepartmanId = await YazilimDepartmanIdAsync(context);
        var rol = DestekErisim.Rol(aktor, talep, yazilimDepartmanId);
        if (!DestekErisim.Isleyebilir(rol)) throw YetkiYok();
        if (!DestekKurallari.AcikMi(talep.Durum))
            throw new InvalidOperationException("Yalnız açık talepler aktarılabilir.");

        var hedef = await context.DestekDepartmanlari.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmanId && d.Aktif)
            ?? throw new InvalidOperationException("Hedef departman bulunamadı ya da pasif.");
        if (hedef.Id == talep.DepartmanId)
            throw new InvalidOperationException("Talep zaten bu departmanda.");

        var bildirimler = new List<BekleyenBildirim>();
        var simdi = DateTime.UtcNow;
        var eskiDurum = talep.Durum;

        if (hedef.Id == yazilimDepartmanId && DestekKurallari.IzinliMi(DestekEylem.YazilimaAktar, talep.Durum, rol))
        {
            await YazilimaAktarAsync(context, talep, aktor, yazilimDepartmanId, gerekce, bildirimler);
            talep.Durum = DestekDurum.YazilimEkibinde;
        }
        else
        {
            talep.DepartmanId = hedef.Id;
            talep.AtananKullaniciId = null;
            if (talep.Durum == DestekDurum.YazilimEkibinde)
                talep.Durum = DestekDurum.Islemde;
            YanitEkle(context, talep, aktor, $"Departman aktarma gerekçesi:\n{gerekce}", YanitTuru.DahiliNot, dahili: true);
            Aktivite(context, talep, AktiviteTuru.Transferedildi, $"Talep '{hedef.Ad}' departmanına aktarıldı", aktor.Id);
            bildirimler.Add(new(await DestekBildirimci.EkipAlicilariAsync(context, hedef.Id, DestekKurallari.PersonelYetkisi),
                $"Talep '{hedef.Ad}' departmanına aktarıldı", gerekce, true, BildirimOncelik.Normal));
        }

        talep.SonAktiviteTarihi = simdi;
        talep.UpdatedAt = simdi;
        if (eskiDurum != talep.Durum)
            DurumAktivitesi(context, talep, eskiDurum, aktor.Id);

        await context.SaveChangesAsync();
        await BildirimleriYazAsync(context, talep, aktor.Id, bildirimler);
    }

    public async Task OncelikDegistirAsync(int talepId, DestekOncelik oncelik)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talep = await TalepYukleAsync(context, talepId);
        if (!DestekErisim.Isleyebilir(DestekErisim.Rol(aktor, talep, await YazilimDepartmanIdAsync(context)))) throw YetkiYok();
        if (talep.Oncelik == oncelik) return;

        var eski = talep.Oncelik;
        talep.Oncelik = oncelik;
        if (DestekKurallari.AcikMi(talep.Durum))
            await SlaUygulaAsync(context, talep, DateTime.UtcNow);
        talep.SonAktiviteTarihi = DateTime.UtcNow;
        talep.UpdatedAt = DateTime.UtcNow;
        Aktivite(context, talep, AktiviteTuru.OncelikDegisti, "Öncelik değiştirildi", aktor.Id,
            DestekKurallari.OncelikEtiketi(eski), DestekKurallari.OncelikEtiketi(oncelik));
        await context.SaveChangesAsync();
    }

    public async Task EtiketleriGuncelleAsync(int talepId, string? etiketler)
    {
        var aktor = await OturumGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var talep = await TalepYukleAsync(context, talepId);
        if (!DestekErisim.Isleyebilir(DestekErisim.Rol(aktor, talep, await YazilimDepartmanIdAsync(context)))) throw YetkiYok();

        var yeni = EtiketleriDuzenle(etiketler);
        if (string.Equals(yeni, talep.Etiketler, StringComparison.Ordinal)) return;

        var eski = talep.Etiketler;
        talep.Etiketler = yeni;
        talep.UpdatedAt = DateTime.UtcNow;
        Aktivite(context, talep, AktiviteTuru.Guncellendi, "Etiketler güncellendi", aktor.Id, eski, yeni);
        await context.SaveChangesAsync();
    }

    public async Task SilAsync(int talepId)
    {
        var aktor = await OturumGerekliAsync();
        if (!aktor.Silebilir) throw YetkiYok("Talep silmek için destektalebi.sil yetkisi gerekir.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var talep = await TalepYukleAsync(context, talepId);
        talep.IsDeleted = true;
        talep.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<DestekTalebi> PersonelTalepOlusturAsync(DestekTalebi girdi)
    {
        var aktor = await OturumGerekliAsync();
        if (!aktor.Personel) throw YetkiYok("Müşteri adına talep açmak için destek personeli yetkisi gerekir.");
        if (string.IsNullOrWhiteSpace(girdi.MusteriAdi))
            throw new InvalidOperationException("Müşteri adı zorunludur.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        int? cariId = null;
        if (girdi.CariId.HasValue && await context.Cariler.AnyAsync(c => c.Id == girdi.CariId.Value))
            cariId = girdi.CariId;

        var talep = new DestekTalebi
        {
            Konu = girdi.Konu,
            Aciklama = girdi.Aciklama,
            DepartmanId = girdi.DepartmanId,
            KategoriId = girdi.KategoriId,
            Oncelik = girdi.Oncelik,
            Kaynak = girdi.Kaynak == DestekKaynak.HataBildirimi ? DestekKaynak.Web : girdi.Kaynak,
            CariId = cariId,
            MusteriAdi = girdi.MusteriAdi,
            MusteriEmail = girdi.MusteriEmail ?? string.Empty,
            MusteriTelefon = girdi.MusteriTelefon,
            Etiketler = girdi.Etiketler,
            Durum = DestekDurum.Yeni
        };
        await TalepAlanlariniDogrulaAsync(context, talep);
        await YeniTalepKaydetAsync(context, talep, aktor, $"Talep {aktor.AdSoyad} tarafından müşteri adına açıldı");
        return talep;
    }

    public async Task<PagedResult<DestekTalebi>> GetPagedAsync(DestekTalebiFilterParams filter)
    {
        var aktor = await EkipUyesiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var yazilimDepartmanId = await YazilimDepartmanIdAsync(context);

        var query = EkipKapsami(context.DestekTalepleri.AsNoTracking(), aktor, yazilimDepartmanId)
            .Include(x => x.Departman)
            .Include(x => x.Kategori)
            .Include(x => x.AtananKullanici)
            .Include(x => x.Cari)
            .AsQueryable();

        if (filter.Durum.HasValue)
            query = query.Where(x => x.Durum == filter.Durum.Value);
        if (filter.Oncelik.HasValue)
            query = query.Where(x => x.Oncelik == filter.Oncelik.Value);
        if (filter.Kaynak.HasValue)
            query = query.Where(x => x.Kaynak == filter.Kaynak.Value);
        if (filter.DepartmanId.HasValue)
            query = query.Where(x => x.DepartmanId == filter.DepartmanId.Value);
        if (filter.KategoriId.HasValue)
            query = query.Where(x => x.KategoriId == filter.KategoriId.Value);
        if (filter.AtananKullaniciId == 0)
            query = query.Where(x => x.AtananKullaniciId == null);
        else if (filter.AtananKullaniciId.HasValue)
            query = query.Where(x => x.AtananKullaniciId == filter.AtananKullaniciId.Value);
        if (filter.CariId.HasValue)
            query = query.Where(x => x.CariId == filter.CariId.Value);
        if (filter.BaslangicTarihi.HasValue)
            query = query.Where(x => x.CreatedAt >= filter.BaslangicTarihi.Value);
        if (filter.BitisTarihi.HasValue)
            query = query.Where(x => x.CreatedAt <= filter.BitisTarihi.Value);
        if (filter.SlaAsildi.HasValue)
            query = query.Where(x => x.SlaAsildi == filter.SlaAsildi.Value);
        if (filter.SadeceAcik == true)
        {
            var acik = DestekKurallari.AcikDurumlar.ToArray();
            query = query.Where(x => acik.Contains(x.Durum));
        }

        var sayfa = Math.Max(1, filter.Page);
        var boyut = Math.Clamp(filter.PageSize, 1, 200);
        var kelimeler = AramaMetni.Kelimeler(filter.AramaMetni);

        List<DestekTalebi> kayitlar;
        int toplam;
        if (kelimeler.Count > 0)
        {
            // Türkçe harfler için kaba LIKE ile aday çekilir, kesin eşleşme bellekte yapılır (bkz. AramaMetni).
            var desen = AramaMetni.KabaDesen(AramaMetni.EnUzun(kelimeler));
            var adaylar = await query
                .Where(x => EF.Functions.Like(x.TalepNo.ToLower(), desen, AramaMetni.KacisKarakteri)
                         || EF.Functions.Like(x.Konu.ToLower(), desen, AramaMetni.KacisKarakteri)
                         || EF.Functions.Like(x.MusteriAdi.ToLower(), desen, AramaMetni.KacisKarakteri)
                         || EF.Functions.Like(x.MusteriEmail.ToLower(), desen, AramaMetni.KacisKarakteri)
                         || (x.Etiketler != null && EF.Functions.Like(x.Etiketler.ToLower(), desen, AramaMetni.KacisKarakteri)))
                .OrderByDescending(x => x.SonAktiviteTarihi)
                .Take(AramaAdayLimiti)
                .ToListAsync();
            var eslesenler = Sirala(adaylar
                    .Where(x => AramaMetni.HepsiEslesir(kelimeler, x.TalepNo, x.Konu, x.MusteriAdi, x.MusteriEmail, x.Etiketler))
                    .AsQueryable(), filter)
                .ToList();
            toplam = eslesenler.Count;
            kayitlar = eslesenler.Skip((sayfa - 1) * boyut).Take(boyut).ToList();
        }
        else
        {
            query = Sirala(query, filter);
            toplam = await query.CountAsync();
            kayitlar = await query.Skip((sayfa - 1) * boyut).Take(boyut).ToListAsync();
        }

        Normallestir(kayitlar);
        return new PagedResult<DestekTalebi>
        {
            Items = kayitlar,
            TotalCount = toplam,
            PageNumber = sayfa,
            PageSize = boyut
        };
    }

    public async Task<List<DestekTalebi>> GetPanoAsync()
    {
        var aktor = await EkipUyesiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var durumlar = DestekKurallari.PanoSutunlari.ToArray();

        var talepler = await EkipKapsami(context.DestekTalepleri.AsNoTracking(), aktor, await YazilimDepartmanIdAsync(context))
            .Include(x => x.Departman)
            .Include(x => x.AtananKullanici)
            .Where(x => durumlar.Contains(x.Durum))
            .OrderByDescending(x => x.Oncelik)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync();
        Normallestir(talepler);
        return talepler;
    }

    public async Task<List<DestekAtanabilirKullanici>> GetAtanabilirKullanicilarAsync(int? departmanId)
    {
        await EkipUyesiGerekliAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var uyeler = departmanId is > 0
            ? await context.DestekDepartmanUyeleri.AsNoTracking()
                .Where(u => u.DepartmanId == departmanId.Value)
                .Select(u => u.KullaniciId)
                .ToListAsync()
            : [];

        var adaylar = await context.Kullanicilar.AsNoTracking()
            .Where(k => k.Aktif && (uyeler.Contains(k.Id)
                || k.Rol.RolAdi == "Admin"
                || k.Rol.Yetkiler.Any(y => !y.IsDeleted && y.Izin &&
                    (y.YetkiKodu == DestekKurallari.PersonelYetkisi || y.YetkiKodu == DestekKurallari.YazilimYetkisi))))
            .Select(k => new
            {
                k.Id,
                k.AdSoyad,
                RolAdi = k.Rol.RolAdi,
                Yazilim = k.Rol.Yetkiler.Any(y => !y.IsDeleted && y.Izin && y.YetkiKodu == DestekKurallari.YazilimYetkisi)
            })
            .ToListAsync();

        return adaylar
            .Select(k => new DestekAtanabilirKullanici(k.Id, k.AdSoyad, k.RolAdi, uyeler.Contains(k.Id), k.Yazilim))
            .OrderByDescending(k => k.DepartmanUyesi)
            .ThenBy(k => k.AdSoyad, StringComparer.CurrentCulture)
            .ToList();
    }

    #region Akış yardımcıları

    private async Task<DestekKullanici> EkipUyesiGerekliAsync()
    {
        var aktor = await OturumGerekliAsync();
        return aktor.EkipUyesi ? aktor : throw YetkiYok("Bu ekran destek ekibi (destektalebi.yaz) ya da yazılım ekibi (destektalebi.yazilim) yetkisi ister.");
    }

    /// <summary>Ekip listelerinde görünen talepler: personel hepsini, yazılım ekibi kendi kapsamını ve kendisine atananları görür.</summary>
    private static IQueryable<DestekTalebi> EkipKapsami(IQueryable<DestekTalebi> query, DestekKullanici aktor, int? yazilimDepartmanId)
    {
        query = query.Where(x => x.Durum != DestekDurum.Taslak);
        if (aktor.Personel) return query;

        var kullaniciId = aktor.Id;
        return query.Where(x => x.Durum == DestekDurum.YazilimEkibinde
                             || (yazilimDepartmanId != null && x.DepartmanId == yazilimDepartmanId)
                             || x.AtananKullaniciId == kullaniciId);
    }

    private static IQueryable<DestekTalebi> Sirala(IQueryable<DestekTalebi> query, DestekTalebiFilterParams filter) =>
        filter.SortBy?.ToLowerInvariant() switch
        {
            "talepno" => filter.SortDesc ? query.OrderByDescending(x => x.TalepNo) : query.OrderBy(x => x.TalepNo),
            "konu" => filter.SortDesc ? query.OrderByDescending(x => x.Konu) : query.OrderBy(x => x.Konu),
            "durum" => filter.SortDesc ? query.OrderByDescending(x => x.Durum) : query.OrderBy(x => x.Durum),
            "oncelik" => filter.SortDesc ? query.OrderByDescending(x => x.Oncelik) : query.OrderBy(x => x.Oncelik),
            "createdat" => filter.SortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => filter.SortDesc ? query.OrderByDescending(x => x.SonAktiviteTarihi) : query.OrderBy(x => x.SonAktiviteTarihi)
        };

    private static void YanitEkle(
        ApplicationDbContext context, DestekTalebi talep, DestekKullanici aktor, string icerik, YanitTuru tur, bool dahili, bool musteri = false)
    {
        context.DestekTalebiYanitlari.Add(new DestekTalebiYanit
        {
            DestekTalebi = talep,
            Icerik = icerik,
            DahiliNot = dahili,
            YanitTuru = tur,
            KullaniciId = aktor.Id,
            MusteriYaniti = musteri,
            MusteriAdi = musteri ? DestekBildirimci.Kisalt(aktor.AdSoyad, 200) : null,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>Talebi yazılım ekibine taşır; dönüş için önceki departman ve atanan aktivitede saklanır.</summary>
    private async Task YazilimaAktarAsync(
        ApplicationDbContext context, DestekTalebi talep, DestekKullanici aktor, int? yazilimDepartmanId, string gerekce,
        List<BekleyenBildirim> bildirimler)
    {
        var oncekiDepartman = talep.DepartmanId;
        var oncekiAtanan = talep.AtananKullaniciId;
        if (yazilimDepartmanId.HasValue && await context.DestekDepartmanlari.AnyAsync(d => d.Id == yazilimDepartmanId && d.Aktif))
            talep.DepartmanId = yazilimDepartmanId.Value;
        talep.AtananKullaniciId = null;

        YanitEkle(context, talep, aktor, $"Yazılım ekibine aktarma gerekçesi:\n{gerekce}", YanitTuru.DahiliNot, dahili: true);
        Aktivite(context, talep, AktiviteTuru.YazilimEkibineAktarildi, "Talep yazılım ekibine aktarıldı", aktor.Id,
            $"{oncekiDepartman}|{oncekiAtanan}", talep.DepartmanId.ToString());

        bildirimler.Add(new(await DestekBildirimci.EkipAlicilariAsync(context, yazilimDepartmanId, DestekKurallari.YazilimYetkisi),
            "Yazılım ekibine talep aktarıldı", gerekce, true, BildirimOncelik.Yuksek));
        bildirimler.Add(new([talep.OlusturanKullaniciId], "Talebiniz yazılım ekibine iletildi",
            "Yazılım ekibi inceliyor; gelişmeleri buradan bildireceğiz.", false, BildirimOncelik.Normal));
    }

    /// <summary>En son "yazılım ekibine aktarıldı" kaydındaki departman ve atanan.</summary>
    private static async Task<(int? Departman, int? Atanan)> SonYazilimAktarmasiAsync(ApplicationDbContext context, int talepId)
    {
        var deger = await context.DestekTalebiAktiviteleri.AsNoTracking()
            .Where(a => a.DestekTalebiId == talepId && a.AktiviteTuru == AktiviteTuru.YazilimEkibineAktarildi)
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Select(a => a.EskiDeger)
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(deger)) return (null, null);

        var parcalar = deger.Split('|');
        int? Oku(int i) => parcalar.Length > i && int.TryParse(parcalar[i], out var v) && v > 0 ? v : null;
        return (Oku(0), Oku(1));
    }

    private async Task BildirimleriYazAsync(ApplicationDbContext context, DestekTalebi talep, int islemYapanId, IEnumerable<BekleyenBildirim> bildirimler)
    {
        var liste = bildirimler.ToList();
        if (liste.Count == 0) return;

        await BildirAsync(context, async () =>
        {
            foreach (var b in liste)
                await DestekBildirimci.EkleAsync(context, b.Alicilar, islemYapanId, talep, b.Baslik, b.Icerik, b.EkipLinki, b.Oncelik);
        });
    }

    #endregion
}
