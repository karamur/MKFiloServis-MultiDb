using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class MaliAnalizService : IMaliAnalizService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public MaliAnalizService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<MaliAnalizDashboard> GetDashboardAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var dashboard = new MaliAnalizDashboard { Yil = yil, Ay = ay };

        var ayBaslangic = new DateTime(yil, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);
        var oncekiAyBaslangic = ayBaslangic.AddMonths(-1);
        var oncekiAyBitis = ayBaslangic.AddDays(-1);

        // Özmal Araç Analizi
        dashboard.OzmalAracAnaliz = await GetOzmalSegmentAnalizAsync(context, ayBaslangic, ayBitis);

        // Kiralık Araç Analizi
        dashboard.KiralikAracAnaliz = await GetKiralikSegmentAnalizAsync(context, ayBaslangic, ayBitis);

        // Komisyon Analizi
        dashboard.KomisyonAnaliz = await GetKomisyonSegmentAnalizAsync(context, ayBaslangic, ayBitis);

        // Taşıma Tedarikçisi (Alt Yüklenici) Analizi
        dashboard.TasimaTedarikciAnaliz = await GetTasimaTedarikciSegmentAnalizAsync(context, ayBaslangic, ayBitis);

        // Toplam hesaplamalar
        // NOT: Tedarikçi araçları zaten Arac.SahiplikTipi üzerinden Ozmal/Kiralik/Komisyon
        // segmentlerinden birinde sayılıyor. Çift sayımı önlemek için toplama eklenmez;
        // tedarikçi kartı/grafiği bilgi amacıyla ayrı gösterilir.
        dashboard.ToplamGelir = dashboard.OzmalAracAnaliz.Gelir + 
                                dashboard.KiralikAracAnaliz.Gelir + 
                                dashboard.KomisyonAnaliz.Gelir;

        dashboard.ToplamGider = dashboard.OzmalAracAnaliz.Gider + 
                                dashboard.KiralikAracAnaliz.Gider + 
                                dashboard.KomisyonAnaliz.Gider;

        // Önceki ay karşılaştırma
        var oncekiOzmal = await GetOzmalSegmentAnalizAsync(context, oncekiAyBaslangic, oncekiAyBitis);
        var oncekiKiralik = await GetKiralikSegmentAnalizAsync(context, oncekiAyBaslangic, oncekiAyBitis);
        var oncekiKomisyon = await GetKomisyonSegmentAnalizAsync(context, oncekiAyBaslangic, oncekiAyBitis);

        dashboard.OncekiAyGelir = oncekiOzmal.Gelir + oncekiKiralik.Gelir + oncekiKomisyon.Gelir;
        dashboard.OncekiAyGider = oncekiOzmal.Gider + oncekiKiralik.Gider + oncekiKomisyon.Gider;

        // Grafik verileri
        dashboard.GelirDagilimi = new List<GrafikVeri>
        {
            new() { Etiket = "Özmal Araçlar", Deger = dashboard.OzmalAracAnaliz.Gelir, Renk = "#28a745" },
            new() { Etiket = "Kiralık Araçlar", Deger = dashboard.KiralikAracAnaliz.Gelir, Renk = "#ffc107" },
            new() { Etiket = "Komisyon İşleri", Deger = dashboard.KomisyonAnaliz.Gelir, Renk = "#17a2b8" },
            new() { Etiket = "Taşıma Tedarikçileri", Deger = dashboard.TasimaTedarikciAnaliz.Gelir, Renk = "#dc3545" }
        };

        dashboard.GiderDagilimi = await GetGiderDagilimiAsync(context, ayBaslangic, ayBitis);
        dashboard.EnKarliGuzergahlar = await GetEnKarliGuzergahlarAsync(context, ayBaslangic, ayBitis, 5);
        dashboard.AracBazliKarlilik = await GetAracBazliKarlilikAsync(context, ayBaslangic, ayBitis, 5);
        dashboard.AylikTrend = await GetYillikTrendAsync(yil);

        return dashboard;
    }

    public async Task<OzmalAracRaporu> GetOzmalAracRaporuAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rapor = new OzmalAracRaporu { Yil = yil, Ay = ay };
        var ayBaslangic = new DateTime(yil, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        // TEK KAYNAK: Filo Servis - Araçlar bölümündeki aktif özmal araçlar
        var ozmalAraclar = await GetSegmentAraclariAsync(context, AracSahiplikTipi.Ozmal);

        foreach (var arac in ozmalAraclar)
        {
            var detay = new OzmalAracDetay
            {
                AracId = arac.Id,
                Plaka = arac.AktifPlaka ?? string.Empty,
                Marka = arac.Marka,
                Model = arac.Model
            };

            // Sefer gelirleri
            var seferler = await context.ServisCalismalari
                .Include(s => s.Guzergah)
                    .ThenInclude(g => g.Cari)
                .Include(s => s.Sofor)
                .Where(s => s.AracId == arac.Id && 
                           s.CalismaTarihi >= ayBaslangic && 
                           s.CalismaTarihi <= ayBitis &&
                           !s.IsDeleted &&
                           s.Durum == CalismaDurum.Tamamlandi)
                .ToListAsync();

            detay.SeferSayisi = seferler.Count;
            detay.SeferGeliri = seferler.Sum(s => s.Fiyat ?? s.Guzergah.BirimFiyat);

            // En çok çalışan şoförü bul
            var soforGrup = seferler.GroupBy(s => s.SoforId)
                                    .OrderByDescending(g => g.Count())
                                    .FirstOrDefault();
            if (soforGrup != null)
            {
                var sofor = soforGrup.First().Sofor;
                detay.AtananSofor = $"{sofor.Ad} {sofor.Soyad}";
            }

            // Çalışılan güzergahlar
            detay.CalistigiGuzergahlar = seferler
                .GroupBy(s => s.GuzergahId)
                .Select(g => new GuzergahOzet
                {
                    GuzergahId = g.Key,
                    GuzergahAdi = g.First().Guzergah.GuzergahAdi,
                    MusteriUnvan = g.First().Guzergah.Cari.Unvan,
                    SeferSayisi = g.Count(),
                    Gelir = g.Sum(s => s.Fiyat ?? s.Guzergah.BirimFiyat)
                })
                .OrderByDescending(g => g.Gelir)
                .ToList();

            // Masraflar
            var masraflar = await context.AracMasraflari
                .Include(m => m.MasrafKalemi)
                .Where(m => m.AracId == arac.Id &&
                           m.MasrafTarihi >= ayBaslangic &&
                           m.MasrafTarihi <= ayBitis &&
                           !m.IsDeleted)
                .ToListAsync();

            foreach (var masraf in masraflar)
            {
                var kategori = masraf.MasrafKalemi?.MasrafKodu?.ToUpper() ?? "";

                if (kategori.Contains("YAKIT") || kategori.Contains("AKARYA"))
                    detay.AkaryakitMasrafi += masraf.Tutar;
                else if (kategori.Contains("BAKIM") || kategori.Contains("SERVIS") || kategori.Contains("ONARIM"))
                    detay.BakimMasrafi += masraf.Tutar;
                else if (kategori.Contains("SIGORTA") || kategori.Contains("KASKO"))
                    detay.SigortaMasrafi += masraf.Tutar;
                else
                    detay.DigerMasraflar += masraf.Tutar;
            }

            rapor.AracDetaylari.Add(detay);
        }

        return rapor;
    }

    public async Task<KiralikAracRaporu> GetKiralikAracRaporuAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rapor = new KiralikAracRaporu { Yil = yil, Ay = ay };
        var ayBaslangic = new DateTime(yil, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        // TEK KAYNAK: Filo Servis - Araçlar bölümündeki aktif kiralık araçlar.
        // Sefer olsun olmasın, kayıtlı tüm kiralık araç+firma listelenir; veri akışı
        // tek noktadan beslenir, raporlar Filo Servis ile birebir tutarlı olur.
        var kiralikAraclar = (await GetSegmentAraclariAsync(context, AracSahiplikTipi.Kiralik))
            .Where(a => a.KiralikCariId.HasValue)
            .ToList();

        if (kiralikAraclar.Count == 0)
            return rapor;

        var aracIdler = kiralikAraclar.Select(a => a.Id).ToList();

        var calismalar = await context.ServisCalismalari
            .Include(s => s.Sofor)
            .Include(s => s.Guzergah)
                .ThenInclude(g => g.Cari)
            .Where(s => aracIdler.Contains(s.AracId) &&
                       s.CalismaTarihi >= ayBaslangic &&
                       s.CalismaTarihi <= ayBitis &&
                       !s.IsDeleted &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .ToListAsync();

        var masrafToplamlari = await context.AracMasraflari
            .Where(m => !m.IsDeleted &&
                       m.MasrafTarihi >= ayBaslangic &&
                       m.MasrafTarihi <= ayBitis &&
                       aracIdler.Contains(m.AracId))
            .GroupBy(m => m.AracId)
            .Select(g => new { AracId = g.Key, Toplam = g.Sum(m => m.Tutar) })
            .ToDictionaryAsync(x => x.AracId, x => x.Toplam);

        // Firma bazında grupla (kiralık aracın bağlı olduğu firma)
        var firmaGruplari = kiralikAraclar
            .GroupBy(a => a.KiralikCari!)
            .OrderBy(g => g.Key.Unvan);

        foreach (var firmaGrup in firmaGruplari)
        {
            var firma = firmaGrup.Key;
            var firmaDetay = new KiralikFirmaDetay
            {
                FirmaId = firma.Id,
                FirmaUnvan = firma.Unvan,
                FirmaKodu = firma.CariKodu
            };

            foreach (var arac in firmaGrup)
            {
                var aracCalismalari = calismalar.Where(c => c.AracId == arac.Id).ToList();
                var sofor = aracCalismalari.FirstOrDefault()?.Sofor;

                var aracDetay = new KiralikAracDetay
                {
                    Plaka = arac.AktifPlaka ?? arac.SaseNo ?? "Bilinmeyen",
                    SoforAdSoyad = sofor != null ? $"{sofor.Ad} {sofor.Soyad}" : null
                };

                var toplamAracMasrafi = masrafToplamlari.GetValueOrDefault(arac.Id);
                var toplamAracSeferi = aracCalismalari.Count;

                var guzergahGruplari = aracCalismalari.GroupBy(c => c.GuzergahId);
                foreach (var guzergahGrup in guzergahGruplari)
                {
                    var guzergah = guzergahGrup.First().Guzergah;
                    var seferSayisi = guzergahGrup.Count();
                    var birimFiyat = guzergah?.BirimFiyat ?? 0;
                    var seferGeliri = guzergahGrup.Sum(c => c.Fiyat ?? birimFiyat);
                    var kiraBedeli = seferSayisi * (arac.SeferBasinaKiraBedeli ?? 0);
                    var masrafPayi = toplamAracSeferi > 0
                        ? toplamAracMasrafi * seferSayisi / toplamAracSeferi
                        : 0;

                    aracDetay.GuzergahDetaylari.Add(new KiralikGuzergahDetay
                    {
                        GuzergahAdi = guzergah?.GuzergahAdi ?? string.Empty,
                        MusteriUnvan = guzergah?.Cari?.Unvan ?? string.Empty,
                        SeferSayisi = seferSayisi,
                        BirimFiyat = birimFiyat,
                        KiraBedeli = arac.SeferBasinaKiraBedeli ?? 0,
                        MusteridenAlinacak = seferGeliri,
                        FirmayaOdenecek = kiraBedeli + masrafPayi
                    });
                }

                firmaDetay.AracDetaylari.Add(aracDetay);
            }

            rapor.FirmaDetaylari.Add(firmaDetay);
        }

        return rapor;
    }

    public async Task<TasimaTedarikciRaporu> GetTasimaTedarikciRaporuAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rapor = new TasimaTedarikciRaporu { Yil = yil, Ay = ay };
        var ayBaslangic = new DateTime(yil, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        // TEK KAYNAK: Aktif tedarikçiler + Sofor.TasimaTedarikciId / Arac.TasimaTedarikciId
        var tedarikciler = await context.TasimaTedarikciler
            .Where(t => !t.IsDeleted && t.Aktif)
            .OrderBy(t => t.Unvan)
            .ToListAsync();

        if (tedarikciler.Count == 0)
            return rapor;

        var tedarikciIdler = tedarikciler.Select(t => t.Id).ToList();

        // Tedarikçi başına personel/araç sayısı (tek kaynak)
        var personelSayilari = await context.Soforler
            .Where(s => !s.IsDeleted && s.Aktif &&
                        s.TasimaTedarikciId.HasValue &&
                        tedarikciIdler.Contains(s.TasimaTedarikciId.Value))
            .GroupBy(s => s.TasimaTedarikciId!.Value)
            .Select(g => new { TedarikciId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.TedarikciId, x => x.Sayi);

        var aracSayilari = await context.Araclar
            .Where(a => !a.IsDeleted && a.Aktif &&
                        a.TasimaTedarikciId.HasValue &&
                        tedarikciIdler.Contains(a.TasimaTedarikciId.Value))
            .GroupBy(a => a.TasimaTedarikciId!.Value)
            .Select(g => new { TedarikciId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.TedarikciId, x => x.Sayi);

        // Aktif iş atamaları (ay aralığını kesenler)
        var isler = await context.TasimaTedarikciIsler
            .Include(i => i.Guzergah)
                .ThenInclude(g => g.Cari)
            .Include(i => i.Arac)
            .Include(i => i.Sofor)
            .Where(i => !i.IsDeleted &&
                        tedarikciIdler.Contains(i.TasimaTedarikciId) &&
                        i.BaslangicTarihi <= ayBitis &&
                        (i.BitisTarihi == null || i.BitisTarihi >= ayBaslangic))
            .ToListAsync();

        // Tedarikçinin araçlarına ait gerçekleşen seferler (mali hesaplama için)
        var tedarikciAracIdler = await context.Araclar
            .Where(a => !a.IsDeleted && a.TasimaTedarikciId.HasValue &&
                        tedarikciIdler.Contains(a.TasimaTedarikciId.Value))
            .Select(a => new { a.Id, TedarikciId = a.TasimaTedarikciId!.Value })
            .ToListAsync();

        var aracTedarikciMap = tedarikciAracIdler.ToDictionary(x => x.Id, x => x.TedarikciId);
        var aracIdSet = aracTedarikciMap.Keys.ToList();

        var calismalar = aracIdSet.Count == 0
            ? new List<ServisCalisma>()
            : await context.ServisCalismalari
                .Include(s => s.Arac)
                .Include(s => s.Sofor)
                .Include(s => s.Guzergah)
                    .ThenInclude(g => g.Cari)
                .Where(s => !s.IsDeleted &&
                            aracIdSet.Contains(s.AracId) &&
                            s.CalismaTarihi >= ayBaslangic &&
                            s.CalismaTarihi <= ayBitis &&
                            s.Durum == CalismaDurum.Tamamlandi)
                .ToListAsync();

        foreach (var tedarikci in tedarikciler)
        {
            var detay = new TasimaTedarikciDetay
            {
                TedarikciId = tedarikci.Id,
                TedarikciKodu = tedarikci.TedarikciKodu,
                Unvan = tedarikci.Unvan,
                CariId = tedarikci.CariId,
                AracSayisi = aracSayilari.GetValueOrDefault(tedarikci.Id),
                PersonelSayisi = personelSayilari.GetValueOrDefault(tedarikci.Id),
                AktifIsSayisi = isler.Count(i => i.TasimaTedarikciId == tedarikci.Id &&
                                                  i.Durum == TasimaTedarikciIsDurum.Aktif)
            };

            // Tedarikçinin iş atamaları üzerinden satır oluştur (sözleşme bazlı)
            var tedarikciIsleri = isler.Where(i => i.TasimaTedarikciId == tedarikci.Id).ToList();
            var tedarikciCalismalari = calismalar
                .Where(c => aracTedarikciMap.GetValueOrDefault(c.AracId) == tedarikci.Id)
                .ToList();

            // Önce iş bazlı (güzergah eşleşmesi olanlar)
            var islenmisGuzergahIdler = new HashSet<int>();
            foreach (var isAtama in tedarikciIsleri)
            {
                var isCalismalari = tedarikciCalismalari
                    .Where(c => c.GuzergahId == isAtama.GuzergahId &&
                                (isAtama.AracId == null || c.AracId == isAtama.AracId))
                    .ToList();

                var seferSayisi = isCalismalari.Count;
                var birimFiyat = isAtama.Guzergah?.BirimFiyat ?? 0m;
                var seferUcreti = isAtama.SeferUcreti ?? tedarikci.VarsayilanSeferUcreti ?? 0m;
                var aylikUcret = isAtama.AylikUcret ?? 0m;

                var musteridenAlinacak = isCalismalari.Sum(c => c.Fiyat ?? birimFiyat);
                var tedarikciyeOdenecek = (seferSayisi * seferUcreti) + aylikUcret;

                detay.IsDetaylari.Add(new TasimaTedarikciIsDetay
                {
                    IsId = isAtama.Id,
                    GuzergahAdi = isAtama.Guzergah?.GuzergahAdi ?? string.Empty,
                    MusteriUnvan = isAtama.Guzergah?.Cari?.Unvan ?? string.Empty,
                    AracPlaka = isAtama.Arac?.AktifPlaka,
                    SoforAdSoyad = isAtama.Sofor != null ? $"{isAtama.Sofor.Ad} {isAtama.Sofor.Soyad}" : null,
                    SeferSayisi = seferSayisi,
                    BirimFiyat = birimFiyat,
                    SeferUcreti = seferUcreti,
                    MusteridenAlinacak = musteridenAlinacak,
                    TedarikciyeOdenecek = tedarikciyeOdenecek
                });

                islenmisGuzergahIdler.Add(isAtama.GuzergahId);
            }

            // İş ataması olmayan ama yine de seferi yapılan güzergahlar (default sefer ücretiyle)
            var ekGuzergahGruplari = tedarikciCalismalari
                .Where(c => !islenmisGuzergahIdler.Contains(c.GuzergahId))
                .GroupBy(c => c.GuzergahId);

            foreach (var grup in ekGuzergahGruplari)
            {
                var ilk = grup.First();
                var seferSayisi = grup.Count();
                var birimFiyat = ilk.Guzergah?.BirimFiyat ?? 0m;
                var seferUcreti = tedarikci.VarsayilanSeferUcreti ?? 0m;
                var musteridenAlinacak = grup.Sum(c => c.Fiyat ?? birimFiyat);
                var tedarikciyeOdenecek = seferSayisi * seferUcreti;

                detay.IsDetaylari.Add(new TasimaTedarikciIsDetay
                {
                    GuzergahAdi = ilk.Guzergah?.GuzergahAdi ?? string.Empty,
                    MusteriUnvan = ilk.Guzergah?.Cari?.Unvan ?? string.Empty,
                    AracPlaka = ilk.Arac?.AktifPlaka,
                    SoforAdSoyad = ilk.Sofor != null ? $"{ilk.Sofor.Ad} {ilk.Sofor.Soyad}" : null,
                    SeferSayisi = seferSayisi,
                    BirimFiyat = birimFiyat,
                    SeferUcreti = seferUcreti,
                    MusteridenAlinacak = musteridenAlinacak,
                    TedarikciyeOdenecek = tedarikciyeOdenecek
                });
            }

            rapor.TedarikciDetaylari.Add(detay);
        }

        return rapor;
    }

    public async Task<ChecklistOzet> GetChecklistOzetAsync(int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ozet = new ChecklistOzet { Yil = yil, Ay = ay };
        var bugun = DateTime.Today;
        var uyariGunSayisi = 30; // 30 gün kala uyarı

        // Şoför Checklist
        var soforler = await context.Soforler.Where(s => s.Aktif).ToListAsync();
        foreach (var sofor in soforler)
        {
            var soforChecklist = new SoforChecklistOzet
            {
                SoforId = sofor.Id,
                AdSoyad = $"{sofor.Ad} {sofor.Soyad}",
                SoforKodu = sofor.SoforKodu
            };

            // Ehliyet kontrolü (şimdilik entity'de bu alanlar yok, ileride eklenebilir)
            soforChecklist.EhliyetDurum = "Tamam";
            soforChecklist.SrcDurum = "Tamam";
            soforChecklist.PsikoteknikDurum = "Tamam";
            soforChecklist.SaglikDurum = "Tamam";
            soforChecklist.GenelDurum = "Tamam";

            ozet.SoforChecklists.Add(soforChecklist);
        }

        // Araç Checklist
        var araclar = await context.Araclar.Where(a => a.Aktif && !a.IsDeleted).ToListAsync();
        foreach (var arac in araclar)
        {
            var aracChecklist = new AracChecklistOzet
            {
                AracId = arac.Id,
                Plaka = arac.AktifPlaka ?? arac.SaseNo,
                MarkaModel = $"{arac.Marka} {arac.Model}"
            };

            // Muayene
            aracChecklist.MuayeneBitisTarihi = arac.MuayeneBitisTarihi;
            aracChecklist.MuayeneDurum = GetTarihDurum(arac.MuayeneBitisTarihi, bugun, uyariGunSayisi);

            // Sigorta
            aracChecklist.SigortaBitisTarihi = arac.TrafikSigortaBitisTarihi;
            aracChecklist.SigortaDurum = GetTarihDurum(arac.TrafikSigortaBitisTarihi, bugun, uyariGunSayisi);

            // Kasko
            aracChecklist.KaskoBitisTarihi = arac.KaskoBitisTarihi;
            aracChecklist.KaskoDurum = GetTarihDurum(arac.KaskoBitisTarihi, bugun, uyariGunSayisi);

            // Bakım
            aracChecklist.BakimDurum = "Tamam";

            // Genel durum
            var durumlar = new[] { aracChecklist.MuayeneDurum, aracChecklist.SigortaDurum, 
                                   aracChecklist.KaskoDurum, aracChecklist.BakimDurum };

            if (durumlar.Any(d => d == "Kritik"))
                aracChecklist.GenelDurum = "Kritik";
            else if (durumlar.Any(d => d == "Uyari"))
                aracChecklist.GenelDurum = "Uyari";
            else
                aracChecklist.GenelDurum = "Tamam";

            ozet.AracChecklists.Add(aracChecklist);
        }

        // Güzergah Checklist
        var guzergahlar = await context.Guzergahlar
            .Include(g => g.Cari)
            .Where(g => g.Aktif && !g.IsDeleted)
            .ToListAsync();

        var ayBaslangic = new DateTime(yil, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        foreach (var guzergah in guzergahlar)
        {
            var guzergahChecklist = new GuzergahChecklistOzet
            {
                GuzergahId = guzergah.Id,
                GuzergahAdi = guzergah.GuzergahAdi,
                MusteriUnvan = guzergah.Cari.Unvan
            };

            // Sözleşme durumu (şimdilik entity'de yok)
            guzergahChecklist.SozlesmeDurum = "Tamam";
            guzergahChecklist.FiyatDurum = "Tamam";

            // Sefer durumu
            var seferSayisi = await context.ServisCalismalari
                .Where(s => s.GuzergahId == guzergah.Id &&
                           s.CalismaTarihi >= ayBaslangic &&
                           s.CalismaTarihi <= ayBitis &&
                           s.Durum == CalismaDurum.Tamamlandi)
                .CountAsync();

            guzergahChecklist.GerceklesenSefer = seferSayisi;
            guzergahChecklist.SeferDurum = "Tamam";

            // Ödeme durumu
            // Not: Fatura.KalanTutar = GenelToplam - OdenenTutar (computed property, DB'ye map'lenmez).
            // Bu yüzden sorguda doğrudan kolonlar üzerinden ifade ediyoruz.
            var bekleyenFaturalar = await context.Faturalar
                .Where(f => f.CariId == guzergah.CariId &&
                           f.GenelToplam - f.OdenenTutar > 0 &&
                           f.Durum != FaturaDurum.IptalEdildi)
                .SumAsync(f => f.GenelToplam - f.OdenenTutar);

            guzergahChecklist.BekleyenOdeme = bekleyenFaturalar;
            guzergahChecklist.OdemeDurum = bekleyenFaturalar > 0 ? "Uyari" : "Tamam";

            guzergahChecklist.GenelDurum = guzergahChecklist.OdemeDurum;

            ozet.GuzergahChecklists.Add(guzergahChecklist);
        }

        return ozet;
    }

    public async Task<List<GrafikVeri>> GetYillikTrendAsync(int yil)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var trend = new List<GrafikVeri>();

        for (int ay = 1; ay <= 12; ay++)
        {
            var ayBaslangic = new DateTime(yil, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            if (ayBaslangic > DateTime.Today)
                break;

            var gelir = await context.ServisCalismalari
                .Include(s => s.Guzergah)
                .Where(s => s.CalismaTarihi >= ayBaslangic &&
                           s.CalismaTarihi <= ayBitis &&
                           !s.IsDeleted &&
                           s.Durum == CalismaDurum.Tamamlandi)
                .SumAsync(s => s.Fiyat ?? s.Guzergah.BirimFiyat);

            var ozmal = await GetOzmalSegmentAnalizAsync(context, ayBaslangic, ayBitis);
            var kiralik = await GetKiralikSegmentAnalizAsync(context, ayBaslangic, ayBitis);
            var komisyon = await GetKomisyonSegmentAnalizAsync(context, ayBaslangic, ayBitis);
            var gider = ozmal.Gider + kiralik.Gider + komisyon.Gider;

            trend.Add(new GrafikVeri
            {
                Etiket = ayBaslangic.ToString("MMM"),
                Deger = gelir - gider,
                EkBilgi = $"Gelir: {gelir:N0}?, Gider: {gider:N0}?"
            });
        }

        return trend;
    }

    #region Private Methods

    /// <summary>
    /// TEK KAYNAK helper'ı: Filo Servis - Araçlar bölümündeki, verilen sahiplik tipine ait
    /// aktif (silinmemiş) araçları getirir. Tüm Mali Analiz hesapları bu havuzu kullanır,
    /// böylece dashboard, özmal raporu, kiralık raporu ve komisyon raporu birbiriyle
    /// tutarlı (aynı araç sayısı/listesi) çalışır.
    /// </summary>
    private static async Task<List<Arac>> GetSegmentAraclariAsync(ApplicationDbContext context, AracSahiplikTipi tip)
    {
        var araclar = await context.Araclar
            .Include(a => a.KiralikCari)
            .Include(a => a.KomisyoncuCari)
            .Where(a => a.SahiplikTipi == tip && a.Aktif && !a.IsDeleted)
            .ToListAsync();
        return araclar.DistinctBy(a => a.Id).ToList();
    }

    private async Task<SegmentAnaliz> GetOzmalSegmentAnalizAsync(ApplicationDbContext context, DateTime baslangic, DateTime bitis)
    {
        var analiz = new SegmentAnaliz { SegmentAdi = "Özmal Araçlar" };

        // TEK KAYNAK: Filo Servis - Araçlar (özmal aktif araçlar)
        var ozmalAracIds = (await GetSegmentAraclariAsync(context, AracSahiplikTipi.Ozmal))
            .Select(a => a.Id)
            .ToList();

        // Gelirler
        analiz.Gelir = await context.ServisCalismalari
            .Include(s => s.Guzergah)
            .Where(s => ozmalAracIds.Contains(s.AracId) &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .SumAsync(s => s.Fiyat ?? s.Guzergah.BirimFiyat);

        // Giderler
        analiz.Gider = await context.AracMasraflari
            .Where(m => ozmalAracIds.Contains(m.AracId) &&
                       m.MasrafTarihi >= baslangic &&
                       m.MasrafTarihi <= bitis &&
                       !m.IsDeleted)
            .SumAsync(m => m.Tutar);

        analiz.SeferSayisi = await context.ServisCalismalari
            .Where(s => ozmalAracIds.Contains(s.AracId) &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .CountAsync();

        analiz.AracSayisi = ozmalAracIds.Count;

        return analiz;
    }

    private async Task<SegmentAnaliz> GetKiralikSegmentAnalizAsync(ApplicationDbContext context, DateTime baslangic, DateTime bitis)
    {
        var analiz = new SegmentAnaliz { SegmentAdi = "Kiralık Araçlar" };

        // TEK KAYNAK: Filo Servis - Araçlar (kiralık aktif araçlar)
        var kiralikAraclar = await GetSegmentAraclariAsync(context, AracSahiplikTipi.Kiralik);
        analiz.AracSayisi = kiralikAraclar.Count;
        if (kiralikAraclar.Count == 0)
            return analiz;

        var kiralikAracIds = kiralikAraclar.Select(a => a.Id).ToList();
        var kiraBedelleri = kiralikAraclar.ToDictionary(a => a.Id, a => a.SeferBasinaKiraBedeli ?? 0);

        var kiralikCalismalar = await context.ServisCalismalari
            .Include(s => s.Guzergah)
            .Where(s => kiralikAracIds.Contains(s.AracId) &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .ToListAsync();

        analiz.Gelir = kiralikCalismalar.Sum(s => s.Fiyat ?? s.Guzergah.BirimFiyat);

        var kiralikAracMasraflari = await context.AracMasraflari
            .Where(m => !m.IsDeleted &&
                       m.MasrafTarihi >= baslangic &&
                       m.MasrafTarihi <= bitis &&
                       kiralikAracIds.Contains(m.AracId))
            .SumAsync(m => m.Tutar);

        analiz.Gider = kiralikCalismalar.Sum(s => kiraBedelleri.GetValueOrDefault(s.AracId)) + kiralikAracMasraflari;
        analiz.SeferSayisi = kiralikCalismalar.Count;

        return analiz;
    }

    private async Task<SegmentAnaliz> GetKomisyonSegmentAnalizAsync(ApplicationDbContext context, DateTime baslangic, DateTime bitis)
    {
        var analiz = new SegmentAnaliz { SegmentAdi = "Komisyon İşleri" };

        // TEK KAYNAK: Filo Servis - Araçlar (komisyon aktif araçlar)
        var komisyonAraclar = await GetSegmentAraclariAsync(context, AracSahiplikTipi.Komisyon);
        analiz.AracSayisi = komisyonAraclar.Count;
        if (komisyonAraclar.Count == 0)
            return analiz;

        var komisyonAracIds = komisyonAraclar.Select(a => a.Id).ToList();

        var komisyonluCalismalar = await context.ServisCalismalari
            .Include(s => s.Arac)
            .Include(s => s.Guzergah)
            .Where(s => komisyonAracIds.Contains(s.AracId) &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .ToListAsync();

        foreach (var calisma in komisyonluCalismalar)
        {
            var seferFiyat = calisma.Fiyat ?? calisma.Guzergah.BirimFiyat;
            analiz.Gelir += seferFiyat;

            var komisyon = calisma.Arac.KomisyonHesaplamaTipi switch
            {
                KomisyonHesaplamaTipi.YuzdeOrani => seferFiyat * (calisma.Arac.KomisyonOrani ?? 0) / 100,
                KomisyonHesaplamaTipi.SabitTutar => calisma.Arac.SabitKomisyonTutari ?? 0,
                _ => 0
            };
            analiz.Gider += komisyon;
        }

        analiz.SeferSayisi = komisyonluCalismalar.Count;

        return analiz;
    }

    private async Task<SegmentAnaliz> GetTasimaTedarikciSegmentAnalizAsync(ApplicationDbContext context, DateTime baslangic, DateTime bitis)
    {
        var analiz = new SegmentAnaliz { SegmentAdi = "Taşıma Tedarikçileri" };

        // TEK KAYNAK: Aktif tedarikçiler + Arac.TasimaTedarikciId üzerinden bağlı araçlar
        var tedarikciler = await context.TasimaTedarikciler
            .Where(t => !t.IsDeleted && t.Aktif)
            .ToListAsync();

        if (tedarikciler.Count == 0)
            return analiz;

        var tedarikciIdler = tedarikciler.Select(t => t.Id).ToList();

        var araclar = await context.Araclar
            .Where(a => !a.IsDeleted && a.Aktif &&
                        a.TasimaTedarikciId.HasValue &&
                        tedarikciIdler.Contains(a.TasimaTedarikciId.Value))
            .Select(a => new { a.Id, TedarikciId = a.TasimaTedarikciId!.Value })
            .ToListAsync();

        analiz.AracSayisi = araclar.Count;
        if (araclar.Count == 0)
            return analiz;

        var aracIdSet = araclar.Select(a => a.Id).ToList();
        var aracTedarikciMap = araclar.ToDictionary(a => a.Id, a => a.TedarikciId);

        var calismalar = await context.ServisCalismalari
            .Include(s => s.Guzergah)
            .Where(s => !s.IsDeleted &&
                        aracIdSet.Contains(s.AracId) &&
                        s.CalismaTarihi >= baslangic &&
                        s.CalismaTarihi <= bitis &&
                        s.Durum == CalismaDurum.Tamamlandi)
            .ToListAsync();

        // İş atamaları (sözleşme sefer/aylık ücret kaynağı)
        var isler = await context.TasimaTedarikciIsler
            .Where(i => !i.IsDeleted &&
                        tedarikciIdler.Contains(i.TasimaTedarikciId) &&
                        i.BaslangicTarihi <= bitis &&
                        (i.BitisTarihi == null || i.BitisTarihi >= baslangic))
            .ToListAsync();

        var tedarikciVarsayilanUcret = tedarikciler.ToDictionary(t => t.Id, t => t.VarsayilanSeferUcreti ?? 0m);

        // Gelir: müşteriden alınacak (sefer fiyatı veya birim fiyat)
        foreach (var c in calismalar)
        {
            analiz.Gelir += c.Fiyat ?? c.Guzergah.BirimFiyat;
        }

        // Gider: tedarikçiye ödenecek (iş ataması varsa onun ücreti, yoksa varsayılan)
        foreach (var c in calismalar)
        {
            var tedarikciId = aracTedarikciMap[c.AracId];
            var isAtama = isler.FirstOrDefault(i => i.TasimaTedarikciId == tedarikciId &&
                                                     i.GuzergahId == c.GuzergahId &&
                                                     (i.AracId == null || i.AracId == c.AracId));
            var seferUcreti = isAtama?.SeferUcreti
                              ?? tedarikciVarsayilanUcret.GetValueOrDefault(tedarikciId);
            analiz.Gider += seferUcreti;
        }

        // Aylık sabit ücretler (ay aralığını kesen iş atamaları için bir kez)
        analiz.Gider += isler.Where(i => i.AylikUcret.HasValue).Sum(i => i.AylikUcret!.Value);

        analiz.SeferSayisi = calismalar.Count;

        return analiz;
    }

    private async Task<List<GrafikVeri>> GetGiderDagilimiAsync(ApplicationDbContext context, DateTime baslangic, DateTime bitis)
    {
        var masraflar = await context.AracMasraflari
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Arac)
            .Where(m => !m.IsDeleted &&
                       m.MasrafTarihi >= baslangic &&
                       m.MasrafTarihi <= bitis &&
                       m.Arac.SahiplikTipi != AracSahiplikTipi.Komisyon)
            .ToListAsync();

        var gruplar = masraflar
            .GroupBy(m => m.MasrafKalemi?.MasrafAdi ?? "Diğer")
            .Select(g => new GrafikVeri
            {
                Etiket = g.Key,
                Deger = g.Sum(m => m.Tutar)
            })
            .OrderByDescending(g => g.Deger)
            .Take(5)
            .ToList();

        var kiralikKiraGideri = await context.ServisCalismalari
            .Include(s => s.Arac)
            .Where(s => !s.IsDeleted &&
                       !s.Arac.IsDeleted &&
                       s.Arac.SahiplikTipi == AracSahiplikTipi.Kiralik &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .SumAsync(s => s.Arac.SeferBasinaKiraBedeli ?? 0);

        var komisyonGideri = await context.ServisCalismalari
            .Include(s => s.Arac)
            .Include(s => s.Guzergah)
            .Where(s => !s.IsDeleted &&
                       !s.Arac.IsDeleted &&
                       s.Arac.SahiplikTipi == AracSahiplikTipi.Komisyon &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .ToListAsync();

        if (kiralikKiraGideri > 0)
        {
            gruplar.Add(new GrafikVeri { Etiket = "Kiralık Kira Gideri", Deger = kiralikKiraGideri });
        }

        var toplamKomisyonGideri = komisyonGideri.Sum(c => HesaplaKomisyonTutari(c.Arac, c.Fiyat ?? c.Guzergah.BirimFiyat));
        if (toplamKomisyonGideri > 0)
        {
            gruplar.Add(new GrafikVeri { Etiket = "Komisyon Ödemesi", Deger = toplamKomisyonGideri });
        }

        return gruplar.OrderByDescending(g => g.Deger).Take(5).ToList();
    }

    private async Task<List<GrafikVeri>> GetEnKarliGuzergahlarAsync(ApplicationDbContext context, DateTime baslangic, DateTime bitis, int adet)
    {
        var calismalar = await context.ServisCalismalari
            .Include(s => s.Guzergah)
            .Where(s => s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .ToListAsync();

        var gruplar = calismalar
            .GroupBy(s => s.GuzergahId)
            .Select(g => new GrafikVeri
            {
                Etiket = g.First().Guzergah.GuzergahAdi,
                Deger = g.Sum(s => s.Fiyat ?? s.Guzergah.BirimFiyat)
            })
            .OrderByDescending(g => g.Deger)
            .Take(adet)
            .ToList();

        return gruplar;
    }

    private async Task<List<GrafikVeri>> GetAracBazliKarlilikAsync(ApplicationDbContext context, DateTime baslangic, DateTime bitis, int adet)
    {
        var calismalar = await context.ServisCalismalari
            .Include(s => s.Arac)
            .Include(s => s.Guzergah)
            .Where(s => s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted &&
                       !s.Arac.IsDeleted &&
                       s.Durum == CalismaDurum.Tamamlandi)
            .ToListAsync();

        var masraflar = await context.AracMasraflari
            .Include(m => m.Arac)
            .Where(m => !m.IsDeleted &&
                       m.MasrafTarihi >= baslangic &&
                       m.MasrafTarihi <= bitis &&
                       m.Arac.SahiplikTipi != AracSahiplikTipi.Komisyon)
            .GroupBy(m => m.AracId)
            .Select(g => new { AracId = g.Key, Toplam = g.Sum(m => m.Tutar) })
            .ToListAsync();

        var gruplar = calismalar
            .GroupBy(s => s.AracId)
            .Select(g => 
            {
                var gelir = g.Sum(s => s.Fiyat ?? s.Guzergah?.BirimFiyat ?? 0);
                var ilkArac = g.First().Arac;
                var masrafGideri = masraflar.FirstOrDefault(m => m.AracId == g.Key)?.Toplam ?? 0;
                var gider = ilkArac?.SahiplikTipi switch
                {
                    AracSahiplikTipi.Kiralik => g.Sum(s => s.Arac?.SeferBasinaKiraBedeli ?? 0) + masrafGideri,
                    AracSahiplikTipi.Komisyon => g.Sum(s => HesaplaKomisyonTutari(s.Arac, s.Fiyat ?? s.Guzergah?.BirimFiyat ?? 0)),
                    _ => masrafGideri
                };
                return new GrafikVeri
                {
                    Etiket = ilkArac?.AktifPlaka ?? ilkArac?.SaseNo ?? "Bilinmeyen",
                    Deger = gelir - gider,
                    EkBilgi = $"Gelir: {gelir:N0}₺, Gider: {gider:N0}₺"
                };
            })
            .OrderByDescending(g => g.Deger)
            .Take(adet)
            .ToList();

        return gruplar;
    }

    private string GetTarihDurum(DateTime? tarih, DateTime bugun, int uyariGunSayisi)
    {
        if (!tarih.HasValue)
            return "Bekliyor";

        var kalanGun = (tarih.Value - bugun).Days;

        if (kalanGun < 0)
            return "Kritik";
        else if (kalanGun <= uyariGunSayisi)
            return "Uyari";
        else
            return "Tamam";
    }

    private static decimal HesaplaKomisyonTutari(Arac? arac, decimal seferGeliri)
    {
        if (arac == null)
            return 0;

        return arac.KomisyonHesaplamaTipi switch
        {
            KomisyonHesaplamaTipi.YuzdeOrani => seferGeliri * (arac.KomisyonOrani ?? 0) / 100,
            KomisyonHesaplamaTipi.SabitTutar => arac.SabitKomisyonTutari ?? 0,
            _ => 0
        };
    }

    #endregion
}
