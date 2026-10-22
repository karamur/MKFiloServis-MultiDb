using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Middleware;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Destek;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

public sealed class DestekTalebiSqliteTests
{
    [Fact]
    public async Task Taslak_ekipten_gizlidir_gonderilince_departman_uyesi_bildirim_alir()
    {
        await using var f = await DestekFixture.CreateAsync();

        f.OturumAc(f.TalepEdenId);
        var taslak = await f.Servis.TalepOlusturAsync(f.YeniTalep("Yazıcı çıktı vermiyor"), taslak: true);
        Assert.Equal(DestekDurum.Taslak, taslak.Durum);

        f.OturumAc(f.PersonelId);
        Assert.Null(await f.Servis.GetDetayAsync(taslak.Id));
        Assert.DoesNotContain((await f.Servis.GetPagedAsync(new DestekTalebiFilterParams())).Items, t => t.Id == taslak.Id);
        Assert.Equal(0, await f.BildirimSayisiAsync(f.PersonelId));

        f.OturumAc(f.TalepEdenId);
        taslak.Konu = "Yazıcı çıktı vermiyor (2. kat)";
        var gonderilen = await f.Servis.TaslakGuncelleAsync(taslak, gonder: true);

        Assert.Equal(DestekDurum.Yeni, gonderilen.Durum);
        Assert.NotNull(gonderilen.SlaBitisTarihi);
        var bildirim = Assert.Single(await f.BildirimlerAsync(f.PersonelId));
        Assert.Equal(BildirimTipi.DestekTalebi, bildirim.Tip);
        Assert.Equal($"/destek-talepleri/{gonderilen.Id}", bildirim.Link);
    }

    [Fact]
    public async Task Tam_akis_bilgi_isteme_yazilim_ekibi_cozum_ve_onay()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        var talep = await f.Servis.TalepOlusturAsync(f.YeniTalep("Fatura ekranı açılmıyor"), taslak: false);

        f.OturumAc(f.PersonelId);
        await f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.Ustlen);
        await f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.BilgiIste, new DestekEylemIstegi { Not = "Ekran görüntüsü atar mısınız?" });
        Assert.Equal(DestekDurum.BilgiBekleniyor, (await f.TalepAsync(talep.Id)).Durum);
        Assert.Contains(await f.BildirimlerAsync(f.TalepEdenId), b => b.Baslik.Contains("bilgi bekliyor") && b.Oncelik == BildirimOncelik.Yuksek);

        f.OturumAc(f.TalepEdenId);
        await f.Servis.YanitEkleAsync(talep.Id, new DestekYanitIstegi { Icerik = "Ekledim." });
        Assert.Equal(DestekDurum.Islemde, (await f.TalepAsync(talep.Id)).Durum);
        Assert.Contains(await f.BildirimlerAsync(f.PersonelId), b => b.Baslik.Contains("mesaj yazdı"));

        f.OturumAc(f.PersonelId);
        await f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.YazilimaAktar, new DestekEylemIstegi { Not = "NullReferenceException, kod incelemesi gerek" });
        var aktarilan = await f.TalepAsync(talep.Id);
        Assert.Equal(DestekDurum.YazilimEkibinde, aktarilan.Durum);
        Assert.Equal(f.YazilimDepartmanId, aktarilan.DepartmanId);
        Assert.Null(aktarilan.AtananKullaniciId);
        Assert.Contains(await f.BildirimlerAsync(f.YazilimciId), b => b.Baslik.Contains("Yazılım ekibine"));
        Assert.Contains(await f.BildirimlerAsync(f.TalepEdenId), b => b.Baslik.Contains("yazılım ekibine iletildi"));

        f.OturumAc(f.YazilimciId);
        var yazilimGorunumu = await f.Servis.GetDetayAsync(talep.Id);
        Assert.True(yazilimGorunumu!.Isleyebilir);
        Assert.Contains((await f.Servis.GetPagedAsync(new DestekTalebiFilterParams())).Items, t => t.Id == talep.Id);
        await f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.DestegeGeriGonder, new DestekEylemIstegi { Not = "1.0.48 sürümünde düzeltildi" });
        var donen = await f.TalepAsync(talep.Id);
        Assert.Equal(DestekDurum.Islemde, donen.Durum);
        Assert.Equal(f.TeknikDepartmanId, donen.DepartmanId);
        Assert.Equal(f.PersonelId, donen.AtananKullaniciId);

        f.OturumAc(f.PersonelId);
        await f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.Coz, new DestekEylemIstegi { Not = "Güncellemeyle çözüldü." });
        var cozulen = await f.TalepAsync(talep.Id);
        Assert.Equal(DestekDurum.Cozuldu, cozulen.Durum);
        Assert.NotNull(cozulen.CozumSuresiDakika);
        Assert.Null(cozulen.KapatilmaTarihi);

        f.OturumAc(f.TalepEdenId);
        var sahipGorunumu = (await f.Servis.GetDetayAsync(talep.Id))!;
        Assert.Equal([DestekEylem.Onayla, DestekEylem.ItirazEt], sahipGorunumu.Eylemler.Select(e => e.Eylem));
        Assert.DoesNotContain(sahipGorunumu.Yanitlar, y => y.DahiliNot);
        Assert.Contains(sahipGorunumu.Yanitlar, y => y.Icerik == "Güncellemeyle çözüldü.");
        await f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.Onayla, new DestekEylemIstegi { MemnuniyetPuani = 5, MemnuniyetYorumu = "Hızlıydı" });

        var kapanan = await f.TalepAsync(talep.Id);
        Assert.Equal(DestekDurum.Kapali, kapanan.Durum);
        Assert.NotNull(kapanan.KapatilmaTarihi);
        Assert.Equal(5, kapanan.MemnuniyetPuani);
        Assert.Contains(await f.BildirimlerAsync(f.PersonelId), b => b.Baslik.Contains("onayladı"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Servis.YanitEkleAsync(talep.Id, new DestekYanitIstegi { Icerik = "Bir şey daha" }));
    }

    [Fact]
    public async Task Itiraz_talebi_yeniden_isleme_alir()
    {
        await using var f = await DestekFixture.CreateAsync();
        var talep = await f.CozulmusTalepAsync();

        f.OturumAc(f.TalepEdenId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.ItirazEt));   // gerekçe zorunlu
        await f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.ItirazEt, new DestekEylemIstegi { Not = "Hâlâ açılmıyor" });

        var sonuc = await f.TalepAsync(talep.Id);
        Assert.Equal(DestekDurum.Islemde, sonuc.Durum);
        Assert.Null(sonuc.CozumSuresiDakika);
        Assert.Contains(await f.BildirimlerAsync(f.PersonelId), b => b.Baslik.Contains("itiraz"));
    }

    [Fact]
    public async Task Yetkisiz_kullanici_baskasinin_talebine_ve_ekip_ekranlarina_erisemez()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        var talep = await f.Servis.TalepOlusturAsync(f.YeniTalep("Gizli talep"), taslak: false);

        f.OturumAc(f.BaskaId);
        Assert.Null(await f.Servis.GetDetayAsync(talep.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.YanitEkleAsync(talep.Id, new DestekYanitIstegi { Icerik = "merhaba" }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.GetPagedAsync(new DestekTalebiFilterParams()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.GetPanoAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.SilAsync(talep.Id));

        f.OturumAc(f.TalepEdenId);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.Coz, new DestekEylemIstegi { Not = "kendi kendime" }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.AtaAsync(talep.Id, f.TalepEdenId));

        f.OturumAc(null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.TalepOlusturAsync(f.YeniTalep("oturumsuz"), taslak: false));
    }

    [Fact]
    public async Task Talep_eden_dahili_notu_ve_dahili_nota_ekli_dosyayi_goremez()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        var talep = await f.Servis.TalepOlusturAsync(f.YeniTalep("Ekran donuyor"), taslak: false);

        f.OturumAc(f.PersonelId);
        var not = await f.Servis.YanitEkleAsync(talep.Id, new DestekYanitIstegi { Icerik = "Müşteri eski sürümde", DahiliNot = true });
        int dahiliEkId;
        await using (var db = f.Factory.CreateDbContext())
        {
            var ek = new DestekTalebiEk { DestekTalebiId = talep.Id, YanitId = not.Id, DosyaAdi = "log.txt", OrijinalDosyaAdi = "log.txt", DosyaYolu = "destek/x/log.txt", MimeTipi = "text/plain", DosyaBoyutu = 3 };
            db.DestekTalebiEkleri.Add(ek);
            await db.SaveChangesAsync();
            dahiliEkId = ek.Id;
        }

        f.OturumAc(f.TalepEdenId);
        var gorunum = (await f.Servis.GetDetayAsync(talep.Id))!;
        Assert.Empty(gorunum.Yanitlar);
        Assert.DoesNotContain(gorunum.Ekler, e => e.Id == dahiliEkId);
        Assert.DoesNotContain(gorunum.Aktiviteler, a => a.AktiviteTuru == AktiviteTuru.YanitEklendi);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.EkIndirAsync(dahiliEkId));
        Assert.Equal(0, await f.BildirimSayisiAsync(f.TalepEdenId));   // dahili not talep edene bildirilmez
    }

    [Fact]
    public async Task Ek_yuklenir_ve_yetkiliye_indirilir_izinsiz_tur_reddedilir()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        var talep = await f.Servis.TalepOlusturAsync(f.YeniTalep("Ek denemesi"), taslak: false);

        var icerik = "hata ayrıntısı"u8.ToArray();
        var ek = await f.Servis.EkYukleAsync(talep.Id, "kayit.txt", "text/plain", new MemoryStream(icerik), icerik.Length);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Servis.EkYukleAsync(talep.Id, "virus.exe", "application/octet-stream", new MemoryStream([1, 2]), 2));

        f.OturumAc(f.PersonelId);
        var indirilen = await f.Servis.EkIndirAsync(ek.Id);
        Assert.Equal(icerik, indirilen!.Icerik);
        Assert.Equal("kayit.txt", indirilen.DosyaAdi);

        f.OturumAc(f.BaskaId);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.EkIndirAsync(ek.Id));
    }

    [Fact]
    public async Task Turkce_harfli_arama_buyuk_kucuk_ve_noktali_i_duyarsizdir()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        await f.Servis.TalepOlusturAsync(f.YeniTalep("YAZICI ARIZASI"), taslak: false);
        await f.Servis.TalepOlusturAsync(f.YeniTalep("İŞ TAKİP EKRANI"), taslak: false);
        await f.Servis.TalepOlusturAsync(f.YeniTalep("Şifre sıfırlama"), taslak: false);

        f.OturumAc(f.PersonelId);
        foreach (var arama in new[] { "arıza", "ARIZA", "yazıcı", "iş takip", "ekranı", "ŞİFRE", "sifre" })
        {
            var sonuc = await f.Servis.GetPagedAsync(new DestekTalebiFilterParams { AramaMetni = arama });
            Assert.True(sonuc.TotalCount == 1, $"'{arama}' araması {sonuc.TotalCount} kayıt buldu");
        }
    }

    [Fact]
    public async Task Cikarilan_departman_uyesi_yeniden_eklenebilir()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.YoneticiId);

        await f.Servis.DepartmanUyesiCikarAsync(f.TeknikDepartmanId, f.PersonelId);
        Assert.DoesNotContain(await f.Servis.GetDepartmanUyeleriAsync(f.TeknikDepartmanId), u => u.KullaniciId == f.PersonelId);

        await f.Servis.DepartmanUyesiEkleAsync(f.TeknikDepartmanId, f.PersonelId, yonetici: true);
        var uye = Assert.Single(await f.Servis.GetDepartmanUyeleriAsync(f.TeknikDepartmanId), u => u.KullaniciId == f.PersonelId);
        Assert.True(uye.Yonetici);

        f.OturumAc(f.PersonelId);   // tanım yetkisi yok
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.DepartmanUyesiCikarAsync(f.TeknikDepartmanId, f.PersonelId));
    }

    [Fact]
    public async Task Talebi_olan_departman_silinmez_pasiflesir_ve_talepler_gorunur_kalir()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.YoneticiId);
        var ikinci = await f.Servis.DepartmanKaydetAsync(new DestekDepartman { Ad = "Muhasebe", Aktif = true });

        f.OturumAc(f.TalepEdenId);
        var talep = await f.Servis.TalepOlusturAsync(f.YeniTalep("Fatura sorusu", ikinci.Id), taslak: false);

        f.OturumAc(f.YoneticiId);
        var mesaj = await f.Servis.DepartmanSilAsync(ikinci.Id);
        Assert.Contains("pasifleştirildi", mesaj);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Servis.DepartmanSilAsync(f.YazilimDepartmanId));   // ayarlarda seçili

        Assert.DoesNotContain(await f.Servis.GetDepartmanlarAsync(), d => d.Id == ikinci.Id);
        Assert.NotNull(await f.Servis.GetDetayAsync(talep.Id));
        Assert.Contains((await f.Servis.GetPagedAsync(new DestekTalebiFilterParams())).Items, t => t.Id == talep.Id);
    }

    [Fact]
    public async Task Ayni_baslikli_makaleler_ayri_adres_alir_ve_taslak_kullaniciya_gorunmez()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.PersonelId);
        var birinci = await f.Servis.BilgiBankasiKaydetAsync(new DestekBilgiBankasi { Baslik = "Şifre nasıl sıfırlanır?", Icerik = "a", Durum = BilgiBankasiDurum.Yayinda });
        var ikinci = await f.Servis.BilgiBankasiKaydetAsync(new DestekBilgiBankasi { Baslik = "Şifre nasıl sıfırlanır?", Icerik = "b", Durum = BilgiBankasiDurum.Taslak });

        Assert.Equal("sifre-nasil-sifirlanir", birinci.Slug);
        Assert.Equal("sifre-nasil-sifirlanir-2", ikinci.Slug);

        f.OturumAc(f.TalepEdenId);
        var gorunen = await f.Servis.GetBilgiBankasiPagedAsync(new BilgiBankasiFilterParams());
        Assert.Equal([birinci.Id], gorunen.Items.Select(m => m.Id));
        Assert.Null(await f.Servis.GetBilgiBankasiByIdAsync(ikinci.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Servis.BilgiBankasiKaydetAsync(new DestekBilgiBankasi { Baslik = "x", Icerik = "y" }));
    }

    [Fact]
    public async Task Yanitsiz_kapatilan_talep_istatistikleri_bozmaz()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        var talep = await f.Servis.TalepOlusturAsync(f.YeniTalep("Yanlışlıkla açıldı"), taslak: false);

        f.OturumAc(f.PersonelId);
        await f.Servis.EylemUygulaAsync(talep.Id, DestekEylem.Kapat, new DestekEylemIstegi { Not = "Mükerrer talep" });
        var stats = await f.Servis.GetDashboardStatsAsync();

        Assert.Equal(1, stats.ToplamTalepSayisi);
        Assert.Equal(0, stats.AcikTalepSayisi);
        Assert.Equal(1, stats.BugunKapatilanTalepSayisi);
        Assert.Null(stats.OrtalamaIlkYanitSuresiSaat);
        Assert.NotNull(stats.OrtalamaCozumSuresiSaat);
        Assert.Null(stats.OrtalamaMemuniyetPuani);
    }

    [Fact]
    public async Task Sla_kontrolu_yalniz_isleyen_talepleri_isaretler_ve_ilgiliye_bildirir()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        var islemde = await f.Servis.TalepOlusturAsync(f.YeniTalep("Süresi geçen"), taslak: false);
        var bilgiBekleyen = await f.Servis.TalepOlusturAsync(f.YeniTalep("Bilgi bekleyen"), taslak: false);
        var cozulen = await f.Servis.TalepOlusturAsync(f.YeniTalep("Çözülen"), taslak: false);

        f.OturumAc(f.PersonelId);
        await f.Servis.EylemUygulaAsync(islemde.Id, DestekEylem.Ustlen);
        await f.Servis.EylemUygulaAsync(bilgiBekleyen.Id, DestekEylem.BilgiIste, new DestekEylemIstegi { Not = "?" });
        await f.Servis.EylemUygulaAsync(cozulen.Id, DestekEylem.Coz, new DestekEylemIstegi { Not = "Tamam" });
        await f.GecmiseCekAsync(x => x.SlaBitisTarihi, DateTime.UtcNow.AddHours(-1));
        var oncekiBildirim = await f.BildirimSayisiAsync(f.PersonelId);

        f.OturumAc(null);   // zamanlanmış iş: oturum yok
        Assert.Equal(1, await f.Servis.CheckAndUpdateSlaViolationsAsync());
        Assert.True((await f.TalepAsync(islemde.Id)).SlaAsildi);
        Assert.False((await f.TalepAsync(bilgiBekleyen.Id)).SlaAsildi);
        Assert.False((await f.TalepAsync(cozulen.Id)).SlaAsildi);
        Assert.Equal(oncekiBildirim + 1, await f.BildirimSayisiAsync(f.PersonelId));
        Assert.Equal(0, await f.Servis.CheckAndUpdateSlaViolationsAsync());
    }

    [Fact]
    public async Task Cozulen_talep_ayarlanan_gun_sonra_otomatik_kapanir()
    {
        await using var f = await DestekFixture.CreateAsync();
        var talep = await f.CozulmusTalepAsync();
        await f.GecmiseCekAsync(x => x.SonAktiviteTarihi, DateTime.UtcNow.AddDays(-8));

        f.OturumAc(null);
        Assert.Equal(1, await f.Servis.CozulenTalepleriOtomatikKapatAsync());

        var sonuc = await f.TalepAsync(talep.Id);
        Assert.Equal(DestekDurum.Kapali, sonuc.Durum);
        Assert.NotNull(sonuc.KapatilmaTarihi);
        Assert.Contains(await f.BildirimlerAsync(f.TalepEdenId), b => b.Baslik.Contains("kapatıldı"));
    }

    [Fact]
    public async Task Hata_bildirimi_teknik_notu_yalniz_ekibe_gosterir_ve_ekran_goruntusunu_ekler()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        var talep = await f.Servis.HataBildirimiOlusturAsync(new SorunBildirimi
        {
            Aciklama = "Kaydet deyince hata verdi.",
            Sayfa = "/faturalar/yeni",
            HataMesaji = "Object reference not set to an instance of an object.",
            HataTuru = "System.NullReferenceException",
            YiginIzi = "at Sayfa.<KaydetAsync>d__12.MoveNext()",
            HataZamaniUtc = DateTime.UtcNow,
            Tarayici = "Edge",
            EkDosyaAdi = "ekran.png",
            EkMimeTipi = "image/png",
            EkIcerik = [137, 80, 78, 71]
        });

        Assert.Equal(DestekKaynak.HataBildirimi, talep.Kaynak);
        Assert.Equal(f.TeknikDepartmanId, talep.DepartmanId);   // varsayılan: "Teknik Destek"
        Assert.Contains("/faturalar/yeni", talep.Aciklama);
        Assert.StartsWith("Hata: Object reference", talep.Konu);

        var sahip = (await f.Servis.GetDetayAsync(talep.Id))!;
        Assert.Empty(sahip.Yanitlar);   // teknik not dahilidir
        Assert.Single(sahip.Ekler);

        f.OturumAc(f.PersonelId);
        var ekip = (await f.Servis.GetDetayAsync(talep.Id))!;
        var teknik = Assert.Single(ekip.Yanitlar);
        Assert.True(teknik.DahiliNot);
        Assert.Equal(YanitTuru.SistemMesaji, teknik.YanitTuru);
        Assert.Contains("<KaydetAsync>", teknik.Icerik);
        Assert.Contains(await f.BildirimlerAsync(f.PersonelId), b => b.Baslik.Contains("hata bildirimi"));
    }

    [Fact]
    public async Task Eski_durum_kodlari_acilista_yeni_akisa_cevrilir()
    {
        await using var f = await DestekFixture.CreateAsync();
        f.OturumAc(f.TalepEdenId);
        var gonderilmis = await f.Servis.TalepOlusturAsync(f.YeniTalep("eski gönderildi"), taslak: false);
        var onaylanmis = await f.Servis.TalepOlusturAsync(f.YeniTalep("eski onaylandı"), taslak: false);
        await using (var db = f.Factory.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync($"UPDATE \"DestekTalepleri\" SET \"Durum\" = 7 WHERE \"Id\" = {gonderilmis.Id}");
            await db.Database.ExecuteSqlRawAsync($"UPDATE \"DestekTalepleri\" SET \"Durum\" = 10, \"KapatilmaTarihi\" = NULL WHERE \"Id\" = {onaylanmis.Id}");
        }

        await using (var db = f.Factory.CreateDbContext())
        {
            var onar = typeof(DbInitializer).GetMethod("DestekVerileriniOnarAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
            await (Task)onar.Invoke(null, [db])!;
        }

        Assert.Equal(DestekDurum.Yeni, (await f.TalepAsync(gonderilmis.Id)).Durum);
        var kapali = await f.TalepAsync(onaylanmis.Id);
        Assert.Equal(DestekDurum.Kapali, kapali.Durum);
        Assert.NotNull(kapali.KapatilmaTarihi);
    }

    [Fact]
    public async Task Bos_veritabaninda_Destek_varsayilanlari_masraf_seedi_basarisiz_olsa_da_olusur()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var context = new ApplicationDbContext(options);

        await DbInitializer.InitializeAsync(context, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

        var departmanlar = await context.DestekDepartmanlari.Select(d => d.Ad).ToListAsync();
        Assert.Contains("Teknik Destek", departmanlar);
        Assert.Contains("Yazılım Ekibi", departmanlar);
        Assert.True(await context.DestekSlaListesi.AnyAsync());
        Assert.True(await context.DestekAyarlari.AnyAsync(a => a.Anahtar == "YazilimEkibiDepartmanId"));
    }

    [Fact]
    public async Task Firma_baglami_olmayan_istekteki_hata_varsayilan_firmaya_kaydedilir()
    {
        await using var f = await DestekFixture.CreateAsync();
        var hizmetler = new ServiceCollection()
            .AddSingleton<IAktifFirmaProvider>(new DestekFixture.BosFirma())
            .AddSingleton<IDbContextFactory<ApplicationDbContext>>(sp => new ScopedDbContextFactory(f.Secenekler, sp))
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = hizmetler };
        http.Request.Method = "GET";
        http.Request.Path = "/patlayan";
        http.Response.Body = new MemoryStream();

        var ara = new ErrorLoggingMiddleware(_ => throw new InvalidOperationException("deneme hatası"), NullLogger<ErrorLoggingMiddleware>.Instance);
        await ara.InvokeAsync(http);

        Assert.Equal(500, http.Response.StatusCode);
        await using var db = f.Factory.CreateDbContext();
        var kayit = Assert.Single(await db.AppErrorLogs.IgnoreQueryFilters().ToListAsync());
        Assert.Equal("deneme hatası", kayit.Message);
        Assert.Equal(1, kayit.FirmaId);
    }
}

/// <summary>Geçici dosya SQLite veritabanı üzerinde Destek servisi; ayrı bağlantılar e-posta gibi arka plan görevlerine dayanıklıdır.</summary>
internal sealed class DestekFixture : IAsyncDisposable
{
    private readonly string _dizin = Path.Combine(Path.GetTempPath(), "mkfilo-destek-testleri", Guid.NewGuid().ToString("N"));
    private readonly SahteDestekKullanicisi _aktor = new();
    private readonly Dictionary<int, DestekKullanici> _kullanicilar = new();
    private ServiceProvider _hizmetler = null!;

    public DbContextOptions<ApplicationDbContext> Secenekler { get; private set; } = null!;
    public ScopedDbContextFactory Factory { get; private set; } = null!;
    public DestekTalebiService Servis { get; private set; } = null!;
    public int TalepEdenId { get; private set; }
    public int BaskaId { get; private set; }
    public int PersonelId { get; private set; }
    public int YazilimciId { get; private set; }
    public int YoneticiId { get; private set; }
    public int TeknikDepartmanId { get; private set; }
    public int YazilimDepartmanId { get; private set; }

    public static async Task<DestekFixture> CreateAsync()
    {
        var f = new DestekFixture();
        Directory.CreateDirectory(f._dizin);
        f.Secenekler = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(f._dizin, "destek.db")};Pooling=False")
            .Options;
        f._hizmetler = new ServiceCollection().AddSingleton<IAktifFirmaProvider>(new TekFirma()).BuildServiceProvider();
        f.Factory = new ScopedDbContextFactory(f.Secenekler, f._hizmetler);

        await using (var db = f.Factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
            await MKFiloServis.Web.Data.Migrations.SchemaSyncHelper.EnsureFisNoCountersSchemaAsync(db);

            db.Firmalar.Add(new Firma { Id = 1, FirmaKodu = "TEST", FirmaAdi = "Test", VarsayilanFirma = true, Organizasyon = new Organizasyon { Id = 1, Adi = "Test Org", Kod = "ORG" } });
            var kullaniciRol = new Rol { RolAdi = "Kullanici" };
            var destekRol = new Rol { RolAdi = "Destek", Yetkiler = [new RolYetki { YetkiKodu = DestekKurallari.PersonelYetkisi, Izin = true }] };
            var yazilimRol = new Rol { RolAdi = "Yazilim", Yetkiler = [new RolYetki { YetkiKodu = DestekKurallari.YazilimYetkisi, Izin = true }] };
            var adminRol = new Rol { RolAdi = "Admin" };
            Kullanici Yeni(string ad, Rol rol) => new() { KullaniciAdi = ad, SifreHash = "x", AdSoyad = ad, Email = $"{ad}@example.test", Rol = rol, Aktif = true };
            var talepEden = Yeni("Ayşe", kullaniciRol);
            var baska = Yeni("Burak", kullaniciRol);
            var personel = Yeni("Mehmet", destekRol);
            var yazilimci = Yeni("Zeynep", yazilimRol);
            var yonetici = Yeni("Yönetici", adminRol);
            db.Kullanicilar.AddRange(talepEden, baska, personel, yazilimci, yonetici);

            var teknik = new DestekDepartman { Ad = "Teknik Destek", Aktif = true, SiraNo = 1 };
            var yazilim = new DestekDepartman { Ad = "Yazılım Ekibi", Aktif = true, SiraNo = 2 };
            db.DestekDepartmanlari.AddRange(teknik, yazilim);
            db.DestekDepartmanUyeleri.AddRange(
                new DestekDepartmanUye { Departman = teknik, Kullanici = personel },
                new DestekDepartmanUye { Departman = yazilim, Kullanici = yazilimci });
            db.DestekSlaListesi.Add(new DestekSla { Ad = "Normal", Oncelik = DestekOncelik.Normal, IlkYanitSuresi = 8, CozumSuresi = 48, Aktif = true });
            await db.SaveChangesAsync();

            db.DestekAyarlari.Add(new DestekAyar { Anahtar = DestekGenelAyarlar.YazilimDepartmaniAnahtari, Deger = yazilim.Id.ToString(), Grup = "Destek" });
            await db.SaveChangesAsync();

            f.TalepEdenId = talepEden.Id;
            f.BaskaId = baska.Id;
            f.PersonelId = personel.Id;
            f.YazilimciId = yazilimci.Id;
            f.YoneticiId = yonetici.Id;
            f.TeknikDepartmanId = teknik.Id;
            f.YazilimDepartmanId = yazilim.Id;

            f._kullanicilar[talepEden.Id] = new DestekKullanici(talepEden.Id, talepEden.AdSoyad, talepEden.Email, false, new HashSet<string>());
            f._kullanicilar[baska.Id] = new DestekKullanici(baska.Id, baska.AdSoyad, baska.Email, false, new HashSet<string>());
            f._kullanicilar[personel.Id] = new DestekKullanici(personel.Id, personel.AdSoyad, personel.Email, false, new HashSet<string> { DestekKurallari.PersonelYetkisi });
            f._kullanicilar[yazilimci.Id] = new DestekKullanici(yazilimci.Id, yazilimci.AdSoyad, yazilimci.Email, false, new HashSet<string> { DestekKurallari.YazilimYetkisi });
            f._kullanicilar[yonetici.Id] = new DestekKullanici(yonetici.Id, yonetici.AdSoyad, yonetici.Email, true, new HashSet<string>());
        }

        f.Servis = new DestekTalebiService(
            f.Factory,
            NullLogger<DestekTalebiService>.Instance,
            new SahteOrtam(f._dizin),
            SahteVekil<IEmailService>.Olustur(),
            new NumaraSerisiService(f.Factory),
            BellekDosyaDeposu.Olustur(),
            f._aktor);
        return f;
    }

    public void OturumAc(int? kullaniciId) => _aktor.Mevcut = kullaniciId.HasValue ? _kullanicilar[kullaniciId.Value] : null;

    public DestekTalebi YeniTalep(string konu, int? departmanId = null) => new()
    {
        Konu = konu,
        Aciklama = $"{konu} — ayrıntı",
        DepartmanId = departmanId ?? TeknikDepartmanId,
        Oncelik = DestekOncelik.Normal
    };

    public async Task<DestekTalebi> CozulmusTalepAsync()
    {
        OturumAc(TalepEdenId);
        var talep = await Servis.TalepOlusturAsync(YeniTalep("Çözülecek talep"), taslak: false);
        OturumAc(PersonelId);
        await Servis.EylemUygulaAsync(talep.Id, DestekEylem.Ustlen);
        await Servis.EylemUygulaAsync(talep.Id, DestekEylem.Coz, new DestekEylemIstegi { Not = "Çözüldü" });
        return talep;
    }

    public async Task<DestekTalebi> TalepAsync(int id)
    {
        await using var db = Factory.CreateDbContext();
        return await db.DestekTalepleri.AsNoTracking().SingleAsync(t => t.Id == id);
    }

    public async Task<List<Bildirim>> BildirimlerAsync(int kullaniciId)
    {
        await using var db = Factory.CreateDbContext();
        return await db.Bildirimler.AsNoTracking().Where(b => b.KullaniciId == kullaniciId).OrderBy(b => b.Id).ToListAsync();
    }

    public async Task<int> BildirimSayisiAsync(int kullaniciId) => (await BildirimlerAsync(kullaniciId)).Count;

    public async Task GecmiseCekAsync(System.Linq.Expressions.Expression<Func<DestekTalebi, DateTime?>> alan, DateTime deger)
    {
        await using var db = Factory.CreateDbContext();
        await db.DestekTalepleri.ExecuteUpdateAsync(s => s.SetProperty(alan, deger));
    }

    public async Task GecmiseCekAsync(System.Linq.Expressions.Expression<Func<DestekTalebi, DateTime>> alan, DateTime deger)
    {
        await using var db = Factory.CreateDbContext();
        await db.DestekTalepleri.ExecuteUpdateAsync(s => s.SetProperty(alan, deger));
    }

    public async ValueTask DisposeAsync()
    {
        // Ateşle-unut e-posta görevleri bağlantıyı bırakabilsin.
        await Task.Delay(50);
        ApplicationDbContext.AmbientServiceProvider.Value = null;
        await _hizmetler.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dizin, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private sealed class SahteDestekKullanicisi : IDestekKullaniciSaglayici
    {
        public DestekKullanici? Mevcut { get; set; }
        public Task<DestekKullanici?> GetirAsync(CancellationToken cancellationToken = default) => Task.FromResult(Mevcut);
    }

    private sealed class TekFirma : IAktifFirmaProvider
    {
        public int? AktifFirmaId => 1;
        public bool HasAktifFirma => true;
        public bool TumFirmalar => false;
        public AktifFirmaBilgisi Mevcut => new();
        public event Action? AktifFirmaDegisti { add { } remove { } }
        public void Set(AktifFirmaBilgisi firma) => throw new NotSupportedException();
        public void SetTumFirmalar(bool value) => throw new NotSupportedException();
        public void SetDonem(int yil, int ay) => throw new NotSupportedException();
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }

    /// <summary>Uygulama açılışı / HTTP hattı gibi aktif firması olmayan kapsam.</summary>
    public sealed class BosFirma : IAktifFirmaProvider
    {
        public int? AktifFirmaId => null;
        public bool HasAktifFirma => false;
        public bool TumFirmalar => false;
        public AktifFirmaBilgisi Mevcut => new();
        public event Action? AktifFirmaDegisti { add { } remove { } }
        public void Set(AktifFirmaBilgisi firma) => throw new NotSupportedException();
        public void SetTumFirmalar(bool value) => throw new NotSupportedException();
        public void SetDonem(int yil, int ay) => throw new NotSupportedException();
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }

    private sealed class SahteOrtam(string kok) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(kok, "wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "MKFiloServis.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = kok;
        public string EnvironmentName { get; set; } = "Development";
    }
}

/// <summary>Arayüzün her metodunu boş/varsayılan sonuçla karşılar (e-posta gönderimi gibi yan etkiler için).</summary>
public class SahteVekil<T> : DispatchProxy where T : class
{
    public static T Olustur() => Create<T, SahteVekil<T>>();

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        var donus = method!.ReturnType;
        if (donus == typeof(void)) return null;
        if (donus == typeof(Task)) return Task.CompletedTask;
        if (donus.IsGenericType && donus.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var tur = donus.GetGenericArguments()[0];
            var deger = tur == typeof(bool) ? true : tur.IsValueType ? Activator.CreateInstance(tur) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(tur).Invoke(null, [deger]);
        }
        return donus.IsValueType ? Activator.CreateInstance(donus) : null;
    }
}

/// <summary>Şifreli dosya deposunun bellek içi karşılığı.</summary>
public class BellekDosyaDeposu : DispatchProxy
{
    private readonly Dictionary<string, byte[]> _dosyalar = new();

    public static ISecureFileService Olustur() => Create<ISecureFileService, BellekDosyaDeposu>();

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        switch (method!.Name)
        {
            case nameof(ISecureFileService.SaveEncryptedAsync):
            {
                var yol = $"{args![0]}/{args[1]}";
                _dosyalar[yol] = (byte[])args[2]!;
                return Task.FromResult(yol);
            }
            case nameof(ISecureFileService.ReadDecryptedAsync):
                return Task.FromResult(args![0] is string okunacak && _dosyalar.TryGetValue(okunacak, out var icerik) ? icerik : null);
            case nameof(ISecureFileService.DeleteAsync):
                if (args![0] is string silinecek) _dosyalar.Remove(silinecek);
                return Task.CompletedTask;
        }

        var donus = method.ReturnType;
        if (donus == typeof(Task)) return Task.CompletedTask;
        if (donus == typeof(Task<bool>)) return Task.FromResult(true);
        return donus.IsValueType ? Activator.CreateInstance(donus) : null;
    }
}
